#!/usr/bin/env python3
"""Private actual prepared-round contract. Trusted ledgers are separate ingress arguments."""
import copy
import hashlib
import importlib.util
import json
import sys
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent
REPOSITORY = ROOT.parent.parent
INVENTORY = json.loads((ROOT/'combat-actual-round-entry-v1.schema.json').read_text())
DEPENDENCIES = INVENTORY['originalSourceHashes'] | INVENTORY['additionalSourceHashes']

class Invalid(ValueError):
    def __init__(self, code, path=''):
        self.code, self.path = f'CMB-ARE-{code:03}', path
        super().__init__(f'{self.code} {path or "/"}')

def require(ok, code, path=''):
    if not ok: raise Invalid(code, path)

def dependency_bytes(path): return (REPOSITORY/path).read_bytes()

def verify_dependencies():
    for path, expected in DEPENDENCIES.items():
        try: actual = hashlib.sha256(dependency_bytes(path)).hexdigest()
        except (OSError, KeyError) as error: raise Invalid(9, path) from error
        require(actual == expected, 9, path)

verify_dependencies()
spec = importlib.util.spec_from_file_location('actual_selection', ROOT/'verify-combat-actual-selection-v1.py')
selection = importlib.util.module_from_spec(spec); spec.loader.exec_module(selection)
require(INVENTORY['originalSourceHashes'] == selection.DEPENDENCIES, 9, '/originalSourceHashes')
OWN_SCHEMA = {k:[tuple(f.split(':')) for f in v.split()] for k,v in INVENTORY['objects'].items()}
SCHEMA = selection.SCHEMA | OWN_SCHEMA
encode, sha = selection.encode, selection.sha
FIXTURE = ROOT/'fixtures/combat-actual-round-entry-v1.json'
_CACHE = {}
MAX_UTC = 253402300799999
SUPPORTED = ('open-round','seal-choice','expire-round','controller-unavailable','complete-step')
UNSUPPORTED = ('commit-attack','resolve-attack','release-reserves','repeat-cycle','finish-cycle','open-later-stage','consume-lineage','refund','reseed')

def typed(value, kind, path='', depth=0):
    require(depth <= 32, 1, path)
    if kind.endswith('?'):
        if value is not None: typed(value,kind[:-1],path,depth)
    elif kind == 'RoundEffect':
        require(type(value) is dict and type(value.get('kind')) is str and value['kind'] in INVENTORY['effectTags'],3,path)
        typed(value,INVENTORY['effectTags'][value['kind']],path,depth)
    elif kind == 'Effect':
        require(type(value) is dict and type(value.get('kind')) is str and value['kind'] in selection.TAGS,3,path)
        typed(value,selection.TAGS[value['kind']],path,depth)
    elif kind in selection.pe.SCHEMA and kind not in OWN_SCHEMA and kind not in selection.OWN_SCHEMA:
        try: selection.pe.typed(value,kind,depth)
        except ValueError as error: raise Invalid(1,path) from error
    elif kind in SCHEMA:
        require(type(value) is dict and set(value)=={k for k,_ in SCHEMA[kind]},1,path)
        for key, child in SCHEMA[kind]: typed(value[key],child,path+'/'+key,depth+1)
    elif kind.endswith('[]'):
        require(type(value) is list and len(value)<=512,1,path)
        for index, child in enumerate(value): typed(child,kind[:-2],f'{path}/{index}',depth+1)
    elif kind in ('int','long','utc'):
        maximum = {'int':2**31-1,'long':2**63-1,'utc':MAX_UTC}[kind]
        require(type(value) is int,1,path)
        require(0<=value<=maximum,2,path)
    elif kind == 'role':
        require(type(value) is str,1,path);require(value in ('attacker','defender'),2,path)
    elif kind == 'actor':
        require(type(value) is str,1,path);require(value in ('axis','commonwealth','system'),2,path)
    elif kind == 'utf8': require(type(value) is str and 0<len(value)<=1048576 and value.isascii(),1,path)
    else:
        try: selection.st.typed(value,kind,path,depth)
        except selection.st.Invalid as error: raise Invalid(int(error.code[-3:]),error.path) from error

def canonical(value, kind):
    if kind.endswith('?'): return None if value is None else canonical(value,kind[:-1])
    if kind == 'RoundEffect': return canonical(value,INVENTORY['effectTags'][value['kind']])
    if kind == 'Effect': return canonical(value,selection.TAGS[value['kind']])
    if kind in selection.pe.SCHEMA and kind not in OWN_SCHEMA and kind not in selection.OWN_SCHEMA: return selection.pe.canonical(value,kind)
    if kind in SCHEMA: return {k:canonical(value[k],child) for k,child in SCHEMA[kind]}
    if kind.endswith('[]'): return [canonical(child,kind[:-2]) for child in value]
    return value

def raw(value, kind):
    typed(value,kind); data=encode(canonical(value,kind)); require(len(data)<=1048576,1); return data

def parse(data, kind):
    require(type(data) is bytes and 0<len(data)<=1048576,1)
    def pairs(items):
        value={}
        for key, child in items:
            require(key not in value,1); value[key]=child
        return value
    try:
        value=json.loads(data.decode('ascii'),object_pairs_hook=pairs,
                        parse_constant=lambda _: (_ for _ in ()).throw(ValueError()))
    except (ValueError,UnicodeError,RecursionError) as error:
        if isinstance(error,Invalid): raise
        raise Invalid(1) from error
    require(raw(value,kind)==data,8); return value

def digest(domain, value): return selection.digest(INVENTORY['domains'][domain],encode(value))
def hash_of(domain, value): return 'sha256:'+digest(domain,value)

def source_bytes(source):
    data=raw(source,'ActualRoundSource'); require(source['contractVersion']==1,3)
    require(len(source['roundEventCanonicalUtf8'])<=16,1,'/roundEventCanonicalUtf8'); return data

def clock_configuration():
    context=selection.env.Context()
    budget=next(x['decisionBudgetMilliseconds'] for x in context.config['windows'] if x['kind']=='force-assignment')
    return dict(contractVersion=1,parentConfigurationHash=context.config_hash,
                timingPolicyId=INVENTORY['clockPolicyId'],decisionBudgetMilliseconds=budget)

def base(source, selection_ledger):
    """Derived only after full original source replay with independent original ledger."""
    verify_dependencies(); source_bytes(source)
    require(source['clockConfiguration']==clock_configuration(),4,'/clockConfiguration')
    try:
        original=selection.read_source(source['actualSelectionSourceCanonicalUtf8'].encode('ascii'),selection_ledger)
        proof=selection.proof(original,selection_ledger)
    except ValueError as error: raise Invalid(4,'/actualSelectionSourceCanonicalUtf8') from error
    control=proof['control']
    require(control['stateVersion']==20 and control['stepIndex']==3 and control['selectionOutcome']=='selected'
            and control['declineReceiptId'] is not None and control['cancellationReceiptId'] is None
            and len(control['stepReceipts'])==3 and not control['closed']
            and len(original['selectionEventCanonicalUtf8'])==7,4,'/actualSelectionSourceCanonicalUtf8')
    return parse(raw(dict(contractVersion=1,actualSelectionProof=proof,
                         clockConfiguration=source['clockConfiguration']),'Base'),'Base')

def base_hash(b): return hash_of('base',canonical(b,'Base'))
def clock_hash(b): return hash_of('configuration',canonical(b['clockConfiguration'],'ClockConfiguration'))
def prior_selection(b): return b['actualSelectionProof']['control']
def boundary(b): return b['actualSelectionProof']['boundary']
def positions(b): return selection.edge(boundary(b))['combatPositionIds']

def initial(b):
    s=prior_selection(b); a=boundary(b)
    return dict(contractVersion=1,baseHash=base_hash(b),clockConfigurationHash=clock_hash(b),segmentId=s['segmentId'],
        opportunityId=None,roundId=None,stateVersion=s['stateVersion'],prefix=s['prefix'],stepIndex=3,status='unopened',
        openingReceiptId=None,timing=None,slots=[],stepReceipts=copy.deepcopy(s['stepReceipts']),world=copy.deepcopy(a['world']),
        randomState=copy.deepcopy(a['randomState']),attackHistory=[],targetUses=[],commitmentId=None,
        cancellationReceiptId=None,receipts=[],closed=False)

def command(b,s,kind,role=None):
    slot=next((x for x in s['slots'] if x['role']==role),None)
    structural=kind in ('open-round','complete-step')
    return dict(contractVersion=1,kind=kind,clockConfigurationHash=clock_hash(b),segmentId=s['segmentId'],
        roundId=None if kind=='open-round' else s['roundId'],slotId=slot['slotId'] if kind=='seal-choice' and slot else None,
        expectedPriorVersion=s['stateVersion'] if structural else None,
        fromPositionId=positions(b)[s['stepIndex']] if kind=='complete-step' and s['stepIndex']<6 else None,
        allocation=copy.deepcopy(slot['allocation']) if kind=='seal-choice' and slot else None)

def trusted(cmd,actor='system',now=None,available=True):
    return dict(command=copy.deepcopy(cmd),actor=actor,admittedAt=now,clockAvailable=available)

def empty_aa(b,s):
    """Joined retained Content + exact actual World + full assigned components; no caller emptiness."""
    path='docs/specs/fixtures/combat-content-v7.canonical.json'
    try: data=dependency_bytes(path)
    except (OSError,KeyError) as error: raise Invalid(9,path) from error
    require(hashlib.sha256(data).hexdigest()==DEPENDENCIES[path],9,path)
    content=json.loads(data)
    candidate=prior_selection(b)['selection']; ids=[]
    require(len(s['slots'])==2 and all(x['sealedReceiptId'] is not None for x in s['slots']),6,'/slots')
    for slot in s['slots']:
        participant=candidate[slot['role']]; allocation=slot['allocation']
        element=next((x for x in content['elements'] if x['elementId']==participant['unit']['elementId']),None)
        current=next((x for x in s['world']['elements'] if x['elementId']==participant['unit']['elementId']),None)
        require(element is not None and current is not None,4,'/emptyAa')
        require(len(element['components'])==len(current['components'])==1,4,'/emptyAa')
        component=element['components'][0]; live=current['components'][0]
        require(component['componentClassId']=='land.combat-component.infantry' and component['maximumToe']==10
                and live['componentId']==component['componentId']==allocation['componentId']
                and live['currentToe']==allocation['committedToe']==10
                and participant['componentIds']==[component['componentId']]
                and allocation['unit']==participant['unit'],4,'/emptyAa')
        ids.append(component['componentId'])
    certificate=dict(contractVersion=1,contentHash=sha(data),worldHash=sha(raw(s['world'],'World')),
        candidate=copy.deepcopy(candidate),allocations=[copy.deepcopy(x['allocation']) for x in s['slots']],
        assignmentReceiptId=s['stepReceipts'][-1],componentIds=ids)
    return hash_of('empty-aa',canonical(certificate,'EmptyAaCertificate'))

def gate(timing,now,available):
    if not available or now is None or now<timing['openingFloorUnixMilliseconds']: return 'unavailable'
    return 'expired' if now>=timing['deadlineUnixMilliseconds'] else 'live'

def transition(b,prior,inp,admission_enabled=True):
    """Private kernel; no derived Base/prior admission API. apply/replay always authenticate source."""
    typed(inp,'RoundInput');require(type(admission_enabled) is bool,1,'/admissionEnabled')
    cmd,actor=inp['command'],inp['actor'];kind=cmd['kind']
    require(cmd['contractVersion']==1 and kind in SUPPORTED+UNSUPPORTED,3,'/command')
    structural=kind in ('open-round','complete-step')
    require((cmd['expectedPriorVersion'] is not None)==structural,3,'/command/expectedPriorVersion')
    require((cmd['fromPositionId'] is not None)==(kind=='complete-step'),3,'/command/fromPositionId')
    require((cmd['slotId'] is not None and cmd['allocation'] is not None) if kind=='seal-choice'
            else cmd['slotId'] is None and cmd['allocation'] is None,3,'/command/allocation')
    require((cmd['roundId'] is None)==(kind=='open-round'),3,'/command/roundId')
    require(cmd['segmentId']==prior['segmentId'] and cmd['clockConfigurationHash']==clock_hash(b),4,'/command/context')
    require(actor in ('axis','commonwealth') if kind=='seal-choice' else actor=='system',4,'/actor')
    ch=sha(raw(cmd,'RoundCommand'));duplicate=next((x for x in prior['receipts'] if x['commandHash']==ch),None)
    if duplicate:
        require(actor==duplicate['actor'],4,'/actor');return copy.deepcopy(prior),None,duplicate['receiptId']
    if kind in ('expire-round','controller-unavailable') and (cmd['roundId']!=prior['roundId'] or prior['status']!='collecting'):
        return copy.deepcopy(prior),None,None
    require(not prior['closed'] and prior['stateVersion']<2**63-1 and len(prior['receipts'])<16,6,'/lifecycle')
    if kind!='open-round':require(cmd['roundId']==prior['roundId'],4,'/command/roundId')
    if structural:require(cmd['expectedPriorVersion']==prior['stateVersion'],6,'/command/expectedPriorVersion')
    if kind=='complete-step':require(prior['stepIndex']<6 and cmd['fromPositionId']==positions(b)[prior['stepIndex']],6,'/command/fromPositionId')
    s=copy.deepcopy(prior);now=inp['admittedAt'];author=actor
    if kind=='open-round':
        require(s['status']=='unopened' and s['stepIndex']==3 and not s['receipts'],6,'/lifecycle')
        require(admission_enabled,7,'/admissionEnabled')
        require(inp['clockAvailable'] and now is not None and now+30000<=MAX_UTC,5,'/admittedAt')
        timing=dict(contractVersion=1,clockConfigurationHash=clock_hash(b),kind='force-assignment',decisionBudgetMilliseconds=30000,
                    openedAtUnixMilliseconds=now,deadlineUnixMilliseconds=now+30000,openingFloorUnixMilliseconds=now)
        identity=dict(baseHash=base_hash(b),actualSelectionSourceHash=b['actualSelectionProof']['sourceHash'],
            segmentId=s['segmentId'],cycleId=boundary(b)['cycleId'],positionId=positions(b)[3],
            candidate=prior_selection(b)['selection'],declineReceiptId=prior_selection(b)['declineReceiptId'])
        s['opportunityId']='aopp.'+digest('opportunity',identity)
        s['roundId']='arnd.'+digest('round',dict(baseHash=base_hash(b),opportunityId=s['opportunityId'],
                    openingAuthorityVersion=s['stateVersion'],openingHistoryPrefix=s['prefix'],timing=timing))
        for role in ('attacker','defender'):
            p=prior_selection(b)['selection'][role]
            s['slots'].append(dict(role=role,owner=p['unit']['originalSide'],slotId='aslt.'+digest('slot',dict(roundId=s['roundId'],role=role)),
                allocation=dict(kind='full-close-assault',unit=copy.deepcopy(p['unit']),componentId=p['componentIds'][0],committedToe=10),sealedReceiptId=None,sealedAt=None))
        s['status'],s['timing']='collecting',timing
        effect=dict(kind='round-opened',opportunityId=s['opportunityId'],timing=copy.deepcopy(timing),slots=copy.deepcopy(s['slots']))
    elif kind=='seal-choice':
        require(s['status']=='collecting' and s['stepIndex']==3,6,'/lifecycle')
        slot=next((x for x in s['slots'] if x['slotId']==cmd['slotId']),None)
        require(slot is not None and slot['owner']==actor,4,'/command/slotId')
        require(slot['sealedReceiptId'] is None,6,'/command/slotId')
        require(cmd['allocation']==slot['allocation'],4,'/command/allocation')
        clock=gate(s['timing'],now,inp['clockAvailable']);require(clock!='expired',5,'/admittedAt')
        if clock=='unavailable':
            effect=dict(kind='round-cancelled',cause='clock-unavailable',timing=copy.deepcopy(s['timing']));author='system'
        else:
            effect=dict(kind='choice-sealed',slotId=slot['slotId'],allocation=copy.deepcopy(slot['allocation']),
                timing=copy.deepcopy(s['timing']),prepared=any(x['sealedReceiptId'] is not None for x in s['slots']))
    elif kind in ('expire-round','controller-unavailable'):
        clock=gate(s['timing'],now,inp['clockAvailable'])
        if kind=='expire-round' and clock=='live':return copy.deepcopy(prior),None,None
        cause='controller-unavailable' if kind=='controller-unavailable' else ('deadline' if clock=='expired' else 'clock-unavailable')
        effect=dict(kind='round-cancelled',cause=cause,timing=copy.deepcopy(s['timing']))
    elif kind=='complete-step':
        require(s['status'] in ('prepared','cancelled') and 3<=s['stepIndex']<=5,6,'/lifecycle')
        require(now is None and inp['clockAvailable'],5,'/admittedAt')
        require(s['stepIndex']<5 or s['status']=='cancelled',7,'/continuation')
        index=s['stepIndex'];cancel=s['status']=='cancelled'
        proofs=[s['cancellationReceiptId']] if cancel else [x['sealedReceiptId'] for x in s['slots']]
        require(all(x is not None for x in proofs),6,'/proofReceipts')
        effect=dict(kind='step-completed',fromPositionId=positions(b)[index],
            toPositionId=positions(b)[index+1] if index<5 else selection.edge(boundary(b))['releasePositionId'],
            previousStepReceiptId=s['stepReceipts'][-1],proofKind='no-attack' if cancel else ('prepared-full-assignment' if index==3 else 'certified-empty-aa'),
            proofReceipts=proofs,certificateHash=empty_aa(b,s) if index==4 and not cancel else None)
    else:
        require(now is None and inp['clockAvailable'],5,'/admittedAt');raise Invalid(7,'/continuation')
    event=dict(contractVersion=1,eventType='actual-round-'+effect['kind'],author=author,
        campaignId=boundary(b)['cycle']['campaignId'],rulesetHash=boundary(b)['cycle']['rulesetHash'],
        configurationHash=clock_hash(b),predecessorConfigurationHash=b['clockConfiguration']['parentConfigurationHash'],
        actualSelectionSourceHash=b['actualSelectionProof']['sourceHash'],baseHash=base_hash(b),cycleId=boundary(b)['cycleId'],
        segmentId=s['segmentId'],roundId=s['roundId'],priorVersion=prior['stateVersion'],stateVersion=prior['stateVersion']+1,
        priorPrefix=prior['prefix'],input=canonical(inp,'RoundInput'),effect=canonical(effect,'RoundEffect'))
    receipt='arc.'+digest('receipt',event);event['receiptId']=receipt;data=raw(event,'RoundEvent')
    tag=effect['kind']
    if tag=='round-opened':s['openingReceiptId']=receipt
    elif tag=='choice-sealed':
        slot=next(x for x in s['slots'] if x['slotId']==effect['slotId']);slot['sealedReceiptId'],slot['sealedAt']=receipt,now
        s['status']='prepared' if effect['prepared'] else 'collecting'
    elif tag=='round-cancelled':s['status'],s['cancellationReceiptId']='cancelled',receipt
    else:
        s['stepReceipts'].append(receipt);s['stepIndex']+=1;s['closed']=s['stepIndex']==6
    s['stateVersion']=event['stateVersion'];s['prefix']=selection.seq.prefix_event(prior['prefix'],data)
    s['receipts'].append(dict(commandHash=ch,eventHash=sha(data),receiptId=receipt,actor=actor,stateVersion=s['stateVersion']))
    return parse(raw(s,'RoundControl'),'RoundControl'),data,receipt

def owned_ledger(ledger,count):
    require(type(ledger) is list and len(ledger)==count and count<=16,1,'/roundLedger')
    return [parse(raw(x,'RoundInput'),'RoundInput') for x in ledger]

def replay(source,selection_ledger,round_ledger):
    verify_dependencies();source=parse(source_bytes(source),'ActualRoundSource')
    ledger=owned_ledger(round_ledger,len(source['roundEventCanonicalUtf8']))
    # Cache contains only independently authenticated complete source + both complete ledgers.
    try: old=selection.owned_ledger(selection_ledger,len(json.loads(source['actualSelectionSourceCanonicalUtf8'])['selectionEventCanonicalUtf8']))
    except (ValueError,KeyError,TypeError) as error:raise Invalid(4,'/selectionLedger') from error
    key=(source_bytes(source),tuple(selection.raw(x,'Input') for x in old),tuple(raw(x,'RoundInput') for x in ledger))
    if key not in _CACHE:
        b=base(source,old);state=initial(b)
        for text,inp in zip(source['roundEventCanonicalUtf8'],ledger):
            data=text.encode('ascii');parse(data,'RoundEvent');state,expected,_=transition(b,state,inp)
            require(expected is not None and data==expected,6,'/roundEventCanonicalUtf8')
        if len(_CACHE)>=256:_CACHE.clear()
        _CACHE[key]=(raw(b,'Base'),raw(state,'RoundControl'))
    b,s=_CACHE[key];return parse(b,'Base'),parse(s,'RoundControl')

def apply(source,selection_ledger,round_ledger,current_input,admission_enabled=True):
    verify_dependencies();inp=parse(raw(current_input,'RoundInput'),'RoundInput')
    require(type(admission_enabled) is bool,1,'/admissionEnabled')
    b,prior=replay(source,selection_ledger,round_ledger);state,event,receipt=transition(b,prior,inp,admission_enabled)
    if event is None and receipt is not None:
        event=next(x.encode('ascii') for x in source['roundEventCanonicalUtf8'] if json.loads(x)['receiptId']==receipt)
    return state,event,receipt

def proof(source,selection_ledger,round_ledger):
    verify_dependencies();b,state=replay(source,selection_ledger,round_ledger);h=digest('source',json.loads(source_bytes(source)))
    return parse(raw(dict(contractVersion=1,sourceId='arsrc.'+h,sourceHash='sha256:'+h,base=b,control=state),'ActualRoundProof'),'ActualRoundProof')

def read_source(data,selection_ledger,round_ledger):
    verify_dependencies();source=parse(data,'ActualRoundSource');replay(source,selection_ledger,round_ledger);return source

def read_control(data,source,selection_ledger,round_ledger):
    verify_dependencies();value=parse(data,'RoundControl');require(data==raw(replay(source,selection_ledger,round_ledger)[1],'RoundControl'),6,'/control');return value

def read_proof(data,source,selection_ledger,round_ledger):
    verify_dependencies();value=parse(data,'ActualRoundProof');require(data==raw(proof(source,selection_ledger,round_ledger),'ActualRoundProof'),6,'/proof');return value

# Everything below is executable acceptance, not an admission or golden generation API.
def predecessor(side,variant='selected'):
    row=next(x for x in json.loads(selection.FIXTURE.read_text())['cases'] if x['owner']==side and x['variant']==variant)
    return selection.parse(row['source']['canonicalUtf8'].encode('ascii'),'ActualSelectionSource'),[selection.parse(x['canonicalUtf8'].encode('ascii'),'Input') for x in row['trustedInputs']]

def start(side):
    old,ledger=predecessor(side);source=dict(contractVersion=1,actualSelectionSourceCanonicalUtf8=selection.source_bytes(old).decode('ascii'),clockConfiguration=clock_configuration(),roundEventCanonicalUtf8=[])
    return source,ledger

def trace(side,first='attacker',cancel=None):
    source,old=start(side);b,state=replay(source,old,[]);sources=[copy.deepcopy(source)];states=[state];ledger=[];events=[]
    def accept(kind,role=None,now=None,available=True):
        nonlocal state,source
        actor=next(x['owner'] for x in state['slots'] if x['role']==role) if role else 'system'
        inp=trusted(command(b,state,kind,role),actor,now,available);state,event,r=apply(source,old,ledger,inp)
        assert event is not None and r==json.loads(event)['receiptId']
        ledger.append(inp);events.append(event);source['roundEventCanonicalUtf8'].append(event.decode('ascii'));sources.append(copy.deepcopy(source));states.append(copy.deepcopy(state))
    accept('open-round',now=3000)
    if cancel is None:
        accept('seal-choice',first,4000);accept('seal-choice','defender' if first=='attacker' else 'attacker',3500)
    else:
        partial,cause=cancel
        if partial!='empty':accept('seal-choice',partial,4000)
        if cause=='deadline':accept('expire-round',now=33000)
        elif cause=='controller':accept('controller-unavailable',available=False)
        else:
            role='attacker' if partial in ('empty','defender') else 'defender'
            accept('seal-choice',role,now={'confidence':3500,'null':None,'below-floor':2999}[cause],available=cause!='confidence')
    for _ in range(3 if cancel else 2):accept('complete-step')
    return source,old,ledger,sources,states,events

def conservation(b,state):
    a=boundary(b);assert state['world']==a['world'] and state['randomState']==a['randomState']
    assert state['randomState']['seed']==1 and state['randomState']['nextByteCursor']==2
    assert not state['attackHistory'] and not state['targetUses'] and state['commitmentId'] is None
    for element in state['world']['elements']:
        assert element['ammunition']['points']==10 and element['operationalState']['capabilityPointsExpended']=={'numerator':0,'denominator':1}
        assert [x['currentToe'] for x in element['components']]==[10]

def semantic_tests():
    results=[]
    for side in ('axis','commonwealth'):
        for first in ('attacker','defender'):
            result=trace(side,first);source,old,ledger,sources,states,events=result;b,_=replay(sources[0],old,[])
            assert [s['stateVersion'] for s in states]==[20,21,22,23,24,25]
            assert [s['status'] for s in states]==['unopened','collecting','collecting','prepared','prepared','prepared']
            assert [s['stepIndex'] for s in states]==[3,3,3,3,4,5]
            assert len(events)==5 and len(states[-1]['stepReceipts'])==5 and not states[-1]['closed']
            assert states[-1]['slots'][0]['role']=='attacker' and states[-1]['slots'][1]['role']=='defender'
            assert [s['sealedAt'] for s in states[-1]['slots']]==([4000,3500] if first=='attacker' else [3500,4000])
            assert json.loads(events[-1])['effect']['certificateHash']==empty_aa(b,states[-2])
            for s in states:conservation(b,s)
            results.append((side,first,None,result))
        for partial in ('empty','attacker','defender'):
            for cause in ('deadline','controller','confidence','null','below-floor'):
                result=trace(side,cancel=(partial,cause));source,old,ledger,sources,states,events=result;b,_=replay(sources[0],old,[])
                assert states[-1]['stateVersion']==(25 if partial=='empty' else 26) and states[-1]['stepIndex']==6 and states[-1]['closed']
                assert len(states[-1]['stepReceipts'])==6 and states[-1]['status']=='cancelled'
                assert sum(x['sealedReceiptId'] is not None for x in states[-1]['slots'])==(0 if partial=='empty' else 1)
                assert all(json.loads(x)['effect']['proofKind']=='no-attack' for x in events[-3:])
                for s in states:conservation(b,s)
                results.append((side,'attacker',(partial,cause),result))
    print('PASS semantic: 4 Prepared CA25 traces; 30 cancelled no-attack Release traces',flush=True)
    return results

def artifact(data):return dict(canonicalUtf8=data.decode('ascii'),byteCount=len(data),sha256=sha(data))

def fixture_case(side,first,cancel,result):
    source,old,ledger,sources,states,events=result
    return dict(owner=side,firstSeal=(None if cancel[0]=='empty' else cancel[0]) if cancel else first,cancellation=list(cancel) if cancel else None,
        source=artifact(source_bytes(source)),trustedSelectionInputs=[selection.artifact(selection.raw(x,'Input')) for x in old],
        trustedRoundInputs=[artifact(raw(x,'RoundInput')) for x in ledger],events=[artifact(x) for x in events],
        controls=[artifact(raw(x,'RoundControl')) for x in states],proofs=[artifact(raw(proof(s,old,ledger[:i]),'ActualRoundProof')) for i,s in enumerate(sources)])

# The acceptance suite uses predecessor fixtures as retained evidence, never as new admission authority.
def reject(label, call, counts, code=None):
    try: call()
    except Invalid as error:
        if code is not None: assert error.code==f'CMB-ARE-{code:03}',(label,error.code,code,error.path)
        group=label.split('/')[0];counts[group]=counts.get(group,0)+1
        return error.code
    raise AssertionError('unexpected acceptance: '+label)

def signed_event(value):
    value=copy.deepcopy(value)
    unsigned={k:v for k,v in value.items() if k!='receiptId'}
    value['receiptId']='arc.'+digest('receipt',unsigned)
    return encode(value)

def mutated(value): return selection.replacement(value)

def verify_trace(result,counts,mutations=True):
    source,old,ledger,sources,states,events=result
    for cut,packet in enumerate(sources):
        b,state=replay(packet,old,ledger[:cut]);assert state==states[cut]
        assert read_source(source_bytes(packet),old,ledger[:cut])==packet
        assert read_control(raw(state,'RoundControl'),packet,old,ledger[:cut])==state
        p=proof(packet,old,ledger[:cut]);assert read_proof(raw(p,'ActualRoundProof'),packet,old,ledger[:cut])==p
        counts['cuts']=counts.get('cuts',0)+1
        conservation(b,state)
        suffix=copy.deepcopy(packet);remaining=copy.deepcopy(ledger[:cut])
        for i in range(cut,len(events)):
            next_state,event,receipt=apply(suffix,old,remaining,ledger[i],admission_enabled=False if cut else True)
            assert event==events[i] and next_state==states[i+1]
            suffix['roundEventCanonicalUtf8'].append(event.decode('ascii'));remaining.append(ledger[i])
            counts['suffix']=counts.get('suffix',0)+1
        assert source_bytes(suffix)==source_bytes(source)
        for i in range(cut):
            for now,available in ((ledger[i]['admittedAt'],ledger[i]['clockAvailable']),(None,False),(MAX_UTC,True),(ledger[i]['admittedAt'],not ledger[i]['clockAvailable'])):
                retry=copy.deepcopy(ledger[i]);retry.update(admittedAt=now,clockAvailable=available)
                current,event,receipt=apply(packet,old,ledger[:cut],retry,admission_enabled=False)
                assert current==state and event==events[i] and receipt==json.loads(event)['receiptId']
                counts['retries']=counts.get('retries',0)+1
            bad=copy.deepcopy(ledger[i]);bad['admittedAt']=True
            reject('retry-primitive/bool',lambda:apply(packet,old,ledger[:cut],bad),counts,1)
            bad=copy.deepcopy(ledger[i]);bad['actor']='axis' if bad['actor']!='axis' else 'commonwealth'
            reject('retry-actor/changed',lambda:apply(packet,old,ledger[:cut],bad),counts,4)
        if state['status']=='prepared' or state['closed']:
            for kind in ('expire-round','controller-unavailable'):
                current,event,receipt=apply(packet,old,ledger[:cut],trusted(command(b,state,kind),now=None,available=False),False)
                assert current==state
                if receipt is None: assert event is None
                else: assert event==next(e for e in events[:cut] if json.loads(e)['receiptId']==receipt)
                counts['prepared-noop']=counts.get('prepared-noop',0)+1
    if not mutations:return
    pe=selection.pe
    for i,event in enumerate(events):
        value=json.loads(event)
        for path,leaf in pe.leaves(value):
            forged=pe.changed(value,path,mutated(leaf));data=signed_event(forged) if path!=('receiptId',) else encode(forged)
            packet=copy.deepcopy(sources[i+1]);packet['roundEventCanonicalUtf8'][-1]=data.decode('ascii')
            reject('event-leaf/'+str(path),lambda:replay(packet,old,ledger[:i+1]),counts)
        for path,leaf in pe.leaves(ledger[i]):
            forged=pe.changed(ledger[i],path,mutated(leaf));alternate=copy.deepcopy(ledger[:i+1]);alternate[i]=forged
            reject('input-leaf/'+str(path),lambda:replay(sources[i+1],old,alternate),counts)
        for bad in selection.malformed_spellings(event,'RoundEvent'):
            packet=copy.deepcopy(sources[i+1]);packet['roundEventCanonicalUtf8'][-1]=bad.decode('ascii',errors='replace')
            reject('raw-event/spelling',lambda:replay(packet,old,ledger[:i+1]),counts)
    p=proof(source,old,ledger)
    for path,leaf in pe.leaves(p):
        forged=pe.changed(p,path,mutated(leaf))
        # Bind any changed Base or Control bytes locally. The original source and ledgers remain authority.
        if path[:1]==('base',):
            try: raw(forged['base'],'Base')
            except Invalid: forged['control']['baseHash']=hash_of('base',forged['base'])  # Bind malformed ordered JSON locally; admission still rejects shape.
            else: forged['control']['baseHash']=base_hash(forged['base'])
        reject('proof-leaf/'+str(path),lambda:read_proof(encode(forged),source,old,ledger),counts)
    for path,leaf in pe.leaves(states[-1]):
        forged=pe.changed(states[-1],path,mutated(leaf))
        reject('control-leaf/'+str(path),lambda:read_control(encode(forged),source,old,ledger),counts)
    for kind,data,reader in (
        ('ActualRoundSource',source_bytes(source),lambda d:read_source(d,old,ledger)),
        ('ActualRoundProof',raw(p,'ActualRoundProof'),lambda d:read_proof(d,source,old,ledger)),
        ('RoundControl',raw(states[-1],'RoundControl'),lambda d:read_control(d,source,old,ledger))):
        for bad in selection.malformed_spellings(data,kind):reject('raw/spelling',lambda:reader(bad),counts)
    for extra in (ledger[:-1],ledger+[ledger[0]],list(reversed(ledger))):
        reject('ledger/count-order',lambda:replay(source,old,extra),counts)
    for i in range(len(events)):
        for action in ('missing','duplicate','reverse'):
            packet=copy.deepcopy(source)
            if action=='missing':packet['roundEventCanonicalUtf8'].pop(i)
            elif action=='duplicate':packet['roundEventCanonicalUtf8'].append(events[i].decode('ascii'))
            else:packet['roundEventCanonicalUtf8'].reverse()
            reject('history/'+action,lambda:replay(packet,old,ledger),counts)
    frozen=source_bytes(source);frozen_old=copy.deepcopy(old);frozen_ledger=copy.deepcopy(ledger);expected=raw(p,'ActualRoundProof')
    returned=read_source(frozen,old,ledger);returned['roundEventCanonicalUtf8'].clear()
    b,s=replay(source,old,ledger);b['actualSelectionProof']['control']['receipts'].clear();s['slots'].clear()
    p['base']['actualSelectionProof']['boundary']['world']['elements'].clear();p['control']['receipts'].clear()
    assert raw(proof(source,old,ledger),'ActualRoundProof')==expected
    assert source_bytes(source)==frozen and old==frozen_old and ledger==frozen_ledger
    caller=copy.deepcopy(source);call_old=copy.deepcopy(old);call_ledger=copy.deepcopy(ledger)
    replay(caller,call_old,call_ledger);caller['roundEventCanonicalUtf8'].clear();call_old.clear();call_ledger.clear()
    assert raw(proof(source,old,ledger),'ActualRoundProof')==expected
    counts['ownership']=counts.get('ownership',0)+6

def predecessor_checks(counts):
    pe=selection.pe
    for side in ('axis','commonwealth'):
        source,old=start(side);selected=json.loads(source['actualSelectionSourceCanonicalUtf8'])
        for cut in range(8):
            earlier=copy.deepcopy(selected);earlier['selectionEventCanonicalUtf8']=earlier['selectionEventCanonicalUtf8'][:cut]
            state=selection.replay(earlier,old[:cut]);assert state['stateVersion']==13+cut
            packet=copy.deepcopy(source);packet['actualSelectionSourceCanonicalUtf8']=selection.source_bytes(earlier).decode('ascii')
            if cut<7:reject('preselection/premature',lambda:replay(packet,old[:cut],[]),counts,4)
            else:assert replay(packet,old,[])[1]['stateVersion']==20
        for variant in selection.VARIANTS:
            if variant=='selected':continue
            original,inputs=predecessor(side,variant);packet=copy.deepcopy(source)
            packet['actualSelectionSourceCanonicalUtf8']=selection.source_bytes(original).decode('ascii')
            reject('preselection/fallback',lambda:replay(packet,inputs,[]),counts,4)
        for i,text in enumerate(selected['selectionEventCanonicalUtf8']):
            value=json.loads(text)
            for path,leaf in pe.leaves(value):
                forged=pe.changed(value,path,mutated(leaf));packet=copy.deepcopy(selected)
                packet['selectionEventCanonicalUtf8'][i]=selection.signed_event(forged).decode('ascii') if path!=('receiptId',) else encode(forged).decode('ascii')
                candidate=copy.deepcopy(source);candidate['actualSelectionSourceCanonicalUtf8']=selection.source_bytes(packet).decode('ascii')
                reject('selection-leaf/'+str(path),lambda:replay(candidate,old,[]),counts,4)
        entry=json.loads(selected['positiveEntrySourceCanonicalUtf8'])
        for cut in (0,1):
            packet=copy.deepcopy(entry);packet['entryEventCanonicalUtf8']=packet['entryEventCanonicalUtf8'][:cut]
            selection.pe.read_source(pe.packet_bytes(packet))
            original=copy.deepcopy(selected);original['positiveEntrySourceCanonicalUtf8']=pe.packet_bytes(packet).decode('ascii')
            candidate=copy.deepcopy(source);candidate['actualSelectionSourceCanonicalUtf8']=selection.source_bytes(original).decode('ascii')
            reject('entry/premature',lambda:replay(candidate,old,[]),counts,4)
        for key in ('requestCanonicalUtf8','createdCanonicalUtf8')+pe.HISTORY_KEYS+('entryEventCanonicalUtf8',):
            records=[entry[key]] if type(entry[key]) is str else entry[key]
            for i,text in enumerate(records):
                value=json.loads(text)
                for path,leaf in pe.leaves(value):
                    packet=copy.deepcopy(entry);data=encode(pe.changed(value,path,mutated(leaf))).decode('ascii')
                    if type(packet[key]) is str:packet[key]=data
                    else:packet[key][i]=data
                    original=copy.deepcopy(selected);original['positiveEntrySourceCanonicalUtf8']=pe.packet_bytes(packet).decode('ascii')
                    candidate=copy.deepcopy(source);candidate['actualSelectionSourceCanonicalUtf8']=selection.source_bytes(original).decode('ascii')
                    reject('entry-leaf/'+str(path),lambda:replay(candidate,old,[]),counts,4)
        for alternate in (old[:-1],old+[old[0]],list(reversed(old))):
            reject('selection-ledger/count-order',lambda:replay(source,alternate,[]),counts,4)
        for i in range(len(old)):
            for key,val in (('actor','axis' if old[i]['actor']!='axis' else 'commonwealth'),('admittedAt',1999)):
                alternative=copy.deepcopy(old);alternative[i][key]=val
                reject('selection-ledger/forged',lambda:replay(source,alternative,[]),counts,4)

# A test-only declassifier. This is NOT an observation, action, transport or outward admission API.
def live_owner_witness(state,role):
    slot=next(x for x in state['slots'] if x['role']==role)
    return dict(owner=slot['owner'],role=role,allocation=copy.deepcopy(slot['allocation']),sealed=slot['sealedReceiptId'] is not None,
                opening=state['timing']['openedAtUnixMilliseconds'],deadline=state['timing']['deadlineUnixMilliseconds'])

def privacy_checks(counts):
    clocks=[(now,available) for now in (None,0,2999,3000,3500,3999,4000,4001,32999,33000,33001,MAX_UTC) for available in (True,False)]
    for side in ('axis','commonwealth'):
        for role in ('attacker','defender'):
            other='defender' if role=='attacker' else 'attacker'
            result=trace(side,first=other);source,old,ledger,sources,states,events=result;b,_=replay(sources[0],old,[])
            empty,one=states[1],states[2]
            assert live_owner_witness(empty,role)==live_owner_witness(one,role)
            assert set(live_owner_witness(one,role))=={'owner','role','allocation','sealed','opening','deadline'}
            for now,available in clocks:
                outcomes=[]
                for cut in (1,2):
                    state=states[cut];inp=trusted(command(b,state,'seal-choice',role),next(x['owner'] for x in state['slots'] if x['role']==role),now,available)
                    before=raw(state,'RoundControl')
                    try:
                        current,event,receipt=apply(sources[cut],old,ledger[:cut],inp)
                        outcome='cancelled' if current['status']=='cancelled' else 'accepted'
                        conservation(b,current)
                        if outcome=='accepted':assert live_owner_witness(current,role)['sealed']
                    except Invalid as error:assert error.code=='CMB-ARE-005';outcome='rejected'
                    expected='cancelled' if not available or now is None or now<3000 else ('accepted' if now<33000 else 'rejected')
                    assert outcome==expected;outcomes.append(outcome);assert raw(state,'RoundControl')==before
                    counts['privacy-clock']=counts.get('privacy-clock',0)+1
                assert outcomes[0]==outcomes[1]
                for field,val,code in (('slotId','foreign',4),('segmentId','foreign',4),('clockConfigurationHash','sha256:'+'0'*64,4),('expectedPriorVersion',99,3)):
                    for cut in (1,2):
                        state=states[cut];inp=trusted(command(b,state,'seal-choice',role),next(x['owner'] for x in state['slots'] if x['role']==role),now,available)
                        inp['command'][field]=val
                        reject('privacy-invalid/'+field,lambda:apply(sources[cut],old,ledger[:cut],inp),counts,code)
            counts['privacy-witness']=counts.get('privacy-witness',0)+1

def boundary_checks(counts):
    source,old,ledger,sources,states,events=trace('axis');b,_=replay(sources[0],old,[])
    for now in (None,MAX_UTC-29999):
        reject('opening/fault',lambda:apply(sources[0],old,[],trusted(command(b,states[0],'open-round'),now=now)),counts,5)
    for flag in (0,None,'false'):
        reject('primitive/admission-flag',lambda:apply(sources[0],old,[],trusted(command(b,states[0],'open-round'),now=3000),flag),counts,1)
    reject('opening/disabled',lambda:apply(sources[0],old,[],trusted(command(b,states[0],'open-round'),now=3000),False),counts,7)
    reject('opening/unavailable',lambda:apply(sources[0],old,[],trusted(command(b,states[0],'open-round'),now=3000,available=False)),counts,5)
    disabled=trusted(command(b,states[0],'open-round'),now=None,available=False)
    reject('opening/disabled-before-clock',lambda:apply(sources[0],old,[],disabled,False),counts,7)
    for field,value,code in (('contractVersion',2,3),('segmentId','foreign',4),('expectedPriorVersion',19,6)):
        bad=copy.deepcopy(disabled);bad['command'][field]=value
        reject('opening/context-before-disabled',lambda:apply(sources[0],old,[],bad,False),counts,code)

    good,_,_=apply(sources[0],old,[],trusted(command(b,states[0],'open-round'),now=MAX_UTC-30000))
    assert good['timing']['deadlineUnixMilliseconds']==MAX_UTC
    for primitive in ('actor','role'):
        for value in (None,True,1):
            reject('primitive/enum-type',lambda:typed(value,primitive),counts,1)
        reject('primitive/enum-value',lambda:typed('foreign',primitive),counts,2)
    for value in (-1,MAX_UTC+1,2**63,True,1.5,'3000'):
        code=2 if type(value) is int else 1
        reject('primitive/time',lambda:apply(sources[0],old,[],trusted(command(b,states[0],'open-round'),now=value)),counts,code)
    for cut,state in enumerate(states):
        if cut:
            for kind in UNSUPPORTED:
                inp=trusted(command(b,state,kind));reject('unsupported/'+kind,lambda:apply(sources[cut],old,ledger[:cut],inp),counts,7)
                inp['admittedAt']=3000;reject('unsupported/clock-first',lambda:apply(sources[cut],old,ledger[:cut],inp),counts,5)
        if cut==5:
            inp=trusted(command(b,state,'complete-step'));reject('unsupported/positive-ca',lambda:apply(sources[cut],old,ledger[:cut],inp),counts,7)
            inp['admittedAt']=33000;reject('unsupported/positive-ca-clock',lambda:apply(sources[cut],old,ledger[:cut],inp),counts,5)
    for cut in (1,2):
        for kind in ('expire-round','controller-unavailable'):
            inp=trusted(command(b,states[cut],kind),now=33000);inp['command']['roundId']='stale'
            state,event,r=apply(sources[cut],old,ledger[:cut],inp);assert state==states[cut] and event is r is None
            counts['stale-noop']=counts.get('stale-noop',0)+1
        state,event,r=apply(sources[cut],old,ledger[:cut],trusted(command(b,states[cut],'expire-round'),now=32999))
        assert state==states[cut] and event is r is None
    for field in ('publicAction','observation','provider','snapshot','transport','hiddenSeal','result','settlement','refund','reseed'):
        for value,kind in ((source,'ActualRoundSource'),(proof(source,old,ledger),'ActualRoundProof'),(states[-1],'RoundControl'),(json.loads(events[1]),'RoundEvent'),(ledger[1],'RoundInput')):
            bad=copy.deepcopy(value);bad[field]=None
            reject('future/'+field,lambda:parse(encode(bad),kind),counts,1)
    for foreign in (old[0],prior_selection(b),b,{'sourceHash':proof(source,old,ledger)['sourceHash']}):
        reject('family/digest-or-old',lambda:read_source(encode(foreign),old,ledger),counts,1)
    for n in (16,17,513):
        packet=copy.deepcopy(sources[0]);packet['roundEventCanonicalUtf8']=['{}']*n
        reject('capacity/round',lambda:read_source(encode(packet),old,[ledger[0]]*n),counts,1)
    oversized=copy.deepcopy(sources[0]);oversized['actualSelectionSourceCanonicalUtf8']='x'*1048576
    reject('capacity/bytes',lambda:read_source(encode(oversized),old,[]),counts,1)
    for cut in (1,3,5):
        state=copy.deepcopy(states[cut]);state['stateVersion']=2**63-1
        inp=trusted(command(b,state,'seal-choice','attacker' if cut==1 else None),actor='axis',now=None,available=False) if cut==1 else trusted(command(b,state,'complete-step'))
        reject('capacity/version',lambda:transition(b,state,inp),counts,6)
        state=copy.deepcopy(states[cut]);state['receipts']=[{}]*16
        inp=trusted(command(b,state,'complete-step')) if cut>1 else trusted(command(b,state,'seal-choice','attacker'),'axis',3000)
        # A private-kernel capacity probe needs receipt shapes sufficient for duplicate lookup.
        state['receipts']=[dict(commandHash='sha256:'+'0'*64)]*16
        reject('capacity/receipt',lambda:transition(b,state,inp),counts,6)
    # All ordered semantic arrays are retained; full aggregate history is not truncated to 16.
    entry=json.loads(json.loads(source['actualSelectionSourceCanonicalUtf8'])['positiveEntrySourceCanonicalUtf8'])
    assert sum(len(entry[k]) for k in selection.pe.HISTORY_KEYS+('entryEventCanonicalUtf8',))==12
    assert len(old)==7 and len(events)==5 and all(len(source_bytes(s))<1048576 for s in sources)
    for field in ('parentConfigurationHash','timingPolicyId','decisionBudgetMilliseconds','contractVersion'):
        packet=copy.deepcopy(sources[0]);packet['clockConfiguration'][field]=mutated(packet['clockConfiguration'][field])
        reject('configuration/'+field,lambda:replay(packet,old,[]),counts,4)
    # Frozen positive and cancelled traces also cover mixed receipts, role order and certificate leaves.

def precedence_checks(counts):
    result=trace('axis');source,old,ledger,sources,states,events=result;b,_=replay(sources[0],old,[])
    cases=[]
    def add(label,inp,code,cut=1):cases.append((label,inp,code,cut))
    live=trusted(command(b,states[1],'seal-choice','attacker'),'axis',None,False)
    for field,value,code in (('contractVersion',2,3),('expectedPriorVersion',0,3),('segmentId','foreign',4),('slotId','foreign',4)):
        bad=copy.deepcopy(live);bad['command'][field]=value;add(field,bad,code)
    bad=copy.deepcopy(live);bad['command']['segmentId']='foreign';bad['command']['expectedPriorVersion']=20;add('arm-before-context',bad,3)
    bad=copy.deepcopy(live);bad['command']['contractVersion']=2;bad['admittedAt']=True;add('primitive-before-version',bad,1)
    bad=copy.deepcopy(live);bad['actor']='system';add('actor-before-clock',bad,4)
    bad=copy.deepcopy(live);bad['command']['allocation']['committedToe']=9;add('allocation-before-clock',bad,4)
    bad=copy.deepcopy(live);bad['command']['roundId']='foreign';add('round-before-clock',bad,4)
    for cut in (3,4,5):
        bad=trusted(command(b,states[cut],'complete-step'),now=33000);bad['command']['expectedPriorVersion']-=1;add('state-before-clock',bad,6,cut)
        bad=trusted(command(b,states[cut],'complete-step'),now=33000);bad['command']['fromPositionId']='foreign';add('position-before-clock',bad,6,cut)
    bad=trusted(command(b,states[5],'complete-step'),now=33000);add('clock-before-positive-stop',bad,5,5)
    for label,inp,code,cut in cases:reject('precedence/'+label,lambda:apply(sources[cut],old,ledger[:cut],inp),counts,code)
    # Every primitive field overlaid onto each conflict still wins, rather than one special vector.
    for label,inp,code,cut in cases:
        for value in (True,3.5,'3000',MAX_UTC+1,-1):
            bad=copy.deepcopy(inp);bad['admittedAt']=value
            reject('precedence-primitive/'+label,lambda:apply(sources[cut],old,ledger[:cut],bad),counts,2 if type(value) is int else 1)

def dependency_checks(counts):
    global dependency_bytes,_CACHE
    source,old,ledger,sources,states,events=trace('axis');pdata=raw(proof(source,old,ledger),'ActualRoundProof');cdata=raw(states[-1],'RoundControl');data=source_bytes(source)
    original=dependency_bytes;cache=_CACHE
    class Sentinel(dict):
        def __contains__(self,key):raise AssertionError('dependency failure reached warm cache')
    try:
        _CACHE=Sentinel(cache)
        for path in DEPENDENCIES:
            dependency_bytes=lambda candidate,p=path:original(candidate)+(b' ' if candidate==p else b'')
            for call in (lambda:replay(source,old,ledger),lambda:base(source,old),lambda:proof(source,old,ledger),
                         lambda:apply(source,old,ledger,ledger[0],False),lambda:read_source(data,old,ledger),
                         lambda:read_proof(pdata,source,old,ledger),lambda:read_control(cdata,source,old,ledger)):
                reject('dependency/'+path,call,counts,9)
            dependency_bytes=lambda candidate,p=path:(_ for _ in ()).throw(KeyError(p)) if candidate==p else original(candidate)
            reject('dependency-missing/'+path,lambda:read_source(data,old,ledger),counts,9)
    finally:dependency_bytes=original;_CACHE=cache

def lifecycle_matrix(counts):
    """Contract expectation table independent of transition/gate. Covers every lifecycle and command kind."""
    for side in ('axis','commonwealth'):
        positive=trace(side);cancel=trace(side,cancel=('empty','deadline'));other=trace(side,first='defender')
        # Eleven lifecycle representatives, including each independently sealed role.
        rows=[(positive,i) for i in range(6)]+[(other,2)]+[(cancel,i) for i in range(2,6)]
        for result,cut in rows:
            source,old,ledger,sources,states,events=result;state=states[cut];b,_=replay(sources[0],old,[])
            for kind in SUPPORTED+UNSUPPORTED:
                roles=('attacker','defender') if kind=='seal-choice' and state['slots'] else (None,)
                for role in roles:
                    for now in (None,0,2999,3000,3500,32999,33000,33001,MAX_UTC):
                        for available in (True,False):
                            cmd=command(b,state,kind,role)
                            if kind=='complete-step' and state['closed']:cmd['fromPositionId']=positions(b)[5]
                            actor=next(x['owner'] for x in state['slots'] if x['role']==role) if role else 'system'
                            inp=trusted(cmd,actor,now,available)
                            # Closed arms precede lifecycle, including unavailable clocks.
                            if kind!='open-round' and state['roundId'] is None:expected=3
                            elif kind=='seal-choice' and role is None:expected=3
                            elif any(x['commandHash']==sha(raw(cmd,'RoundCommand')) and x['actor']==actor for x in state['receipts']):expected='duplicate'
                            elif kind in ('expire-round','controller-unavailable') and state['status']!='collecting':expected='noop'
                            elif state['closed']:expected=6
                            elif kind=='open-round':
                                expected=6 if state['status']!='unopened' else (5 if not available or now is None or now>MAX_UTC-30000 else 'accepted')
                            elif kind=='seal-choice':
                                if state['status']!='collecting':expected=6
                                elif not available or now is None or now<3000:expected='accepted'
                                elif now>=33000:expected=5
                                else:expected='accepted'
                            elif kind=='expire-round':expected='noop' if available and now is not None and 3000<=now<33000 else 'accepted'
                            elif kind=='controller-unavailable':expected='accepted'
                            elif kind=='complete-step':
                                if state['status'] not in ('prepared','cancelled'):expected=6
                                elif now is not None or not available:expected=5
                                elif state['status']=='prepared' and state['stepIndex']==5:expected=7
                                else:expected='accepted'
                            else:expected=5 if now is not None or not available else 7
                            before=raw(state,'RoundControl')
                            if type(expected) is int:
                                reject('lifecycle-matrix/'+kind,lambda:transition(b,state,inp),counts,expected)
                            else:
                                current,event,receipt=transition(b,state,inp)
                                if expected=='duplicate':assert current==state and event is None and receipt is not None
                                elif expected=='noop':assert current==state and event is receipt is None
                                else:assert current['stateVersion']==state['stateVersion']+1 and event is not None and receipt is not None
                                conservation(b,current);counts['lifecycle-matrix']=counts.get('lifecycle-matrix',0)+1
                            assert raw(state,'RoundControl')==before
                            # Public source admission sample for each kind/lifecycle/role and conflict.
                            if now is None and not available:
                                if type(expected) is int:reject('lifecycle-public/'+kind,lambda:apply(sources[cut],old,ledger[:cut],inp),counts,expected)
                                else:
                                    current,event,r=apply(sources[cut],old,ledger[:cut],inp)
                                    assert (current==state)==(expected in ('duplicate','noop'))
                                    counts['lifecycle-public']=counts.get('lifecycle-public',0)+1

def closure_checks(counts):
    pe=selection.pe
    for side in ('axis','commonwealth'):
        source,old=start(side);original=json.loads(source['actualSelectionSourceCanonicalUtf8']);entry=json.loads(original['positiveEntrySourceCanonicalUtf8'])
        for key in pe.HISTORY_KEYS+('entryEventCanonicalUtf8',):
            for action in ('missing','duplicate','reverse'):
                packet=copy.deepcopy(entry)
                if action=='missing':packet[key].pop()
                elif action=='duplicate':packet[key].append(packet[key][0])
                else:
                    if len(packet[key])<2:continue
                    packet[key].reverse()
                selected=copy.deepcopy(original);selected['positiveEntrySourceCanonicalUtf8']=pe.packet_bytes(packet).decode('ascii')
                bad=copy.deepcopy(source);bad['actualSelectionSourceCanonicalUtf8']=selection.source_bytes(selected).decode('ascii')
                reject('entry-order/'+key+action,lambda:replay(bad,old,[]),counts,4)
        for action in ('missing','duplicate','reverse'):
            packet=copy.deepcopy(original)
            if action=='missing':packet['selectionEventCanonicalUtf8'].pop()
            elif action=='duplicate':packet['selectionEventCanonicalUtf8'].append(packet['selectionEventCanonicalUtf8'][0])
            else:packet['selectionEventCanonicalUtf8'].reverse()
            bad=copy.deepcopy(source);bad['actualSelectionSourceCanonicalUtf8']=selection.source_bytes(packet).decode('ascii')
            reject('selection-order/'+action,lambda:replay(bad,old,[]),counts,4)
        result=trace(side);final,old,ledger,sources,states,events=result
        # Caller can't substitute the other actual original owner while retaining this round history.
        foreign,foreign_old=start('commonwealth' if side=='axis' else 'axis');bad=copy.deepcopy(final)
        bad['actualSelectionSourceCanonicalUtf8']=foreign['actualSelectionSourceCanonicalUtf8']
        reject('owner/mixed-round',lambda:replay(bad,foreign_old,ledger),counts)
        for kind,data in (('ActualRoundSource',source_bytes(final)),('ActualRoundProof',raw(proof(final,old,ledger),'ActualRoundProof')),
                          ('RoundControl',raw(states[-1],'RoundControl')),('RoundEvent',events[-1]),('RoundInput',raw(ledger[0],'RoundInput'))):
            for rawdata in (b' '*1048577,b'['*33+b'0'+b']'*33, b'\xff', data+b' '):
                reject('raw-limits/'+kind,lambda:parse(rawdata,kind),counts)
        for i,inp in enumerate(ledger):
            for bad in selection.malformed_spellings(raw(inp,'RoundInput'),'RoundInput'):
                reject('raw-input/spelling',lambda:parse(bad,'RoundInput'),counts)
        b,_=replay(sources[0],old,[])
        for state in states:
            for leaf in ('baseHash','prefix','stepReceipts','slots','world','randomState','attackHistory','targetUses'):
                forged=copy.deepcopy(state)
                if type(forged[leaf]) is list:forged[leaf]=list(reversed(forged[leaf])) if len(forged[leaf])>1 else [None]
                elif type(forged[leaf]) is dict:
                    forged[leaf]=copy.deepcopy(forged[leaf]);key=next(iter(forged[leaf]));forged[leaf][key]=mutated(forged[leaf][key])
                else:forged[leaf]=mutated(forged[leaf])
                if forged==state:continue
                cut=state['stateVersion']-20
                reject('causal-array/'+leaf,lambda:read_control(encode(forged),sources[cut],old,ledger[:cut]),counts)
        # Every closed command numeric arm is tested at lexical/range endpoints, before context/time.
        inp=trusted(command(b,states[3],'complete-step'))
        for key,maximum in (('contractVersion',2**31-1),('expectedPriorVersion',2**63-1)):
            for value in (-1,maximum+1,True,1.0,'23'):
                bad=copy.deepcopy(inp);bad['command'][key]=value;bad['command']['segmentId']='foreign';bad['clockAvailable']=False
                reject('integer-arm/'+key,lambda:apply(sources[3],old,ledger[:3],bad),counts,2 if type(value) is int else 1)
        for cut in (1,2):
            for role in ('attacker','defender'):
                state=states[cut];inp=trusted(command(b,state,'seal-choice',role),next(x['owner'] for x in state['slots'] if x['role']==role),None,False)
                # A fresh malformed own allocation/foreign seat never becomes a clock cancellation.
                bad=copy.deepcopy(inp);bad['command']['allocation']['committedToe']=9
                reject('proposal/shape-before-clock',lambda:apply(sources[cut],old,ledger[:cut],bad),counts,6 if next(x['sealedReceiptId'] for x in state['slots'] if x['role']==role) is not None else 4)
                bad=copy.deepcopy(inp);bad['actor']='system'
                reject('proposal/actor-before-clock',lambda:apply(sources[cut],old,ledger[:cut],bad),counts,4)
    # Range/capacity helpers are internal probes, not source admission or proof shortcuts.
    for kind in ('RoundInput','ActualRoundSource','ActualRoundProof','RoundControl','RoundEvent'):
        reject('capacity/empty-bytes',lambda:parse(b'',kind),counts,1)

def consumption_checks(counts):
    global dependency_bytes
    source,old,ledger,sources,states,events=trace('axis')
    b,_=replay(sources[4],old,ledger[:4]);inp=trusted(command(b,states[4],'complete-step'))
    original=dependency_bytes;path='docs/specs/fixtures/combat-content-v7.canonical.json'
    for mode in ('changed','missing'):
        calls=[0]
        def lookup(candidate):
            if candidate!=path:return original(candidate)
            calls[0]+=1
            if calls[0]<=2:return original(candidate) # Both public preflight checks pass before warm lookup.
            if mode=='missing':raise KeyError(path)
            return original(candidate)+b' '
        dependency_bytes=lookup
        try:reject('dependency-consumption/'+mode,lambda:apply(sources[4],old,ledger[:4],inp),counts,9)
        finally:dependency_bytes=original
        assert calls[0]==3

def adversarial_tests(results,mutations=True):
    counts={}
    for side,first,cancel,result in results:
        verify_trace(result,counts,mutations)
        print('PASS trace:',side,first,cancel,flush=True)
    print('PASS cuts/retries/ownership:',json.dumps(counts,sort_keys=True),flush=True)
    predecessor_checks(counts);print('PASS predecessor:',json.dumps(counts,sort_keys=True),flush=True)
    privacy_checks(counts);boundary_checks(counts);precedence_checks(counts);lifecycle_matrix(counts);closure_checks(counts);dependency_checks(counts);consumption_checks(counts)
    print('PASS adversarial:',json.dumps(counts,sort_keys=True),flush=True)
    return counts

def verify_literals(results):
    require(FIXTURE.exists(),1,'/fixture')
    fixture=json.loads(FIXTURE.read_text())
    expected=dict(contractVersion=1,contract=INVENTORY['contract'],originalSourceHashes=INVENTORY['originalSourceHashes'],
                  additionalSourceHashes=INVENTORY['additionalSourceHashes'],cases=[fixture_case(*row) for row in results])
    assert fixture==expected,'immutable literal fixture byte/hash/control/proof mismatch'
    for row in fixture['cases']:
        expected_first=None if row['cancellation'] and row['cancellation'][0]=='empty' else row['cancellation'][0] if row['cancellation'] else row['firstSeal']
        assert row['firstSeal']==expected_first
        assert row['firstSeal'] in (None,'attacker','defender')
        first_seal=next((json.loads(e['canonicalUtf8']) for e in row['events'] if json.loads(e['canonicalUtf8'])['effect']['kind']=='choice-sealed'),None)
        actual_role=next(x['role'] for x in json.loads(row['controls'][1]['canonicalUtf8'])['slots'] if x['slotId']==first_seal['effect']['slotId']) if first_seal else None
        assert row['firstSeal']==actual_role,'first seal label must match actual accepted role'
    artifacts=[]
    for row in fixture['cases']:
        artifacts.append(row['source'])
        for key in ('trustedSelectionInputs','trustedRoundInputs','events','controls','proofs'):artifacts.extend(row[key])
    for item in artifacts:
        data=item['canonicalUtf8'].encode('ascii')
        assert len(data)==item['byteCount'] and sha(data)==item['sha256']
    max_source=max(row['source']['byteCount'] for row in fixture['cases'])
    max_proof=max(item['byteCount'] for row in fixture['cases'] for item in row['proofs'])
    print(f'PASS literals: {len(fixture["cases"])} cases; {len(artifacts)} artifacts; max source {max_source}; max proof {max_proof}',flush=True)

if __name__=='__main__':
    if sys.argv[1:]==['--semantic']:semantic_tests()
    elif sys.argv[1:]==['--adversarial']:adversarial_tests(semantic_tests())
    elif not sys.argv[1:]:
        results=semantic_tests();verify_literals(results);adversarial_tests(results)
    else:raise SystemExit('Usage: verify-combat-actual-round-entry-v1.py [--semantic|--adversarial]')
