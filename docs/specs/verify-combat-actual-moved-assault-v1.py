#!/usr/bin/env python3
"""Additive moved assault reference. Draft until bounds/interface freeze; no bypassed admission."""
import copy,hashlib,importlib.util,itertools,json,re,sys
from pathlib import Path
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parent;REPOSITORY=ROOT.parent.parent
NAME='combat-actual-moved-assault-v1'
INVENTORY=json.loads((ROOT/(NAME+'.schema.json')).read_bytes())
def load(name,file):
 s=importlib.util.spec_from_file_location(name,ROOT/file);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
selection_leaves=load('moved_selection_leaves','verify-combat-actual-selection-v1.py')
round_leaves=load('moved_round_leaves','verify-combat-actual-round-entry-v1.py')
paid_leaves=load('moved_paid_leaves','verify-combat-actual-paid-assault-v1.py')
i01=load('moved_actual_entry','verify-combat-actual-reaction-combat-entry-v1.py')
aad=paid_leaves.aad
SCHEMA={k:[tuple(f.split(':')) for f in v.split()] for k,v in INVENTORY['objects'].items()}
LIMITS=INVENTORY['limits'];MAX_UTC=253402300799999
SUPPORTED_HANDLERS=set(INVENTORY['supportHandlers'])
class Invalid(ValueError):
 def __init__(self,code,path=''):
  self.code,self.path=f'CMB-MAS-{code:03}',path;super().__init__(self.code+' '+path)
def require(ok,code,path=''):
 if not ok:raise Invalid(code,path)
def encode(v):return json.dumps(v,ensure_ascii=True,separators=(',',':')).encode('ascii')
def sha(data):return 'sha256:'+hashlib.sha256(data).hexdigest()
def digest(domain,data):return hashlib.sha256(domain.encode('ascii')+b'\0'+data).hexdigest()
def phase_digest(phase,kind,value):return digest(INVENTORY['domains'][phase][kind],encode(value))
def verify_dependencies():
 for path,h in INVENTORY['runtimeSourceHashes'].items():
  try:data=(REPOSITORY/path).read_bytes()
  except OSError as e:raise Invalid(9,path) from e
  require(hashlib.sha256(data).hexdigest()==h,9,path)
def dependency_manifest_hash():return 'sha256:'+phase_digest('paid','dependency-manifest',dict(sorted(INVENTORY['runtimeSourceHashes'].items())))
OWN_DELIVERY={path:sha(path.read_bytes()) for path in (ROOT/(NAME+'.schema.json'),Path(__file__).resolve())}
def verify_delivery():
 for path,expected in OWN_DELIVERY.items():
  require(sha(path.read_bytes())==expected,9,'own-delivery')
def typed(v,kind,path='',depth=0):
 require(depth<=LIMITS['depth'],1,path)
 if kind.endswith('?'):
  if v is not None:typed(v,kind[:-1],path,depth)
 elif kind in INVENTORY['effectTags']:
  require(type(v) is dict and v.get('kind') in INVENTORY['effectTags'][kind],3,path)
  typed(v,INVENTORY['effectTags'][kind][v['kind']],path,depth)
 elif kind in SCHEMA:
  require(type(v) is dict and set(v)=={k for k,_ in SCHEMA[kind]},1,path)
  for f,t in SCHEMA[kind]:typed(v[f],t,path+'/'+f,depth+1)
 elif kind.endswith('[]'):
  require(type(v) is list and len(v)<=512,1,path)
  for i,x in enumerate(v):typed(x,kind[:-2],path+'/'+str(i),depth+1)
 elif kind=='phase':require(v in ('selection','round','paid','reserve-entry'),2,path)
 elif kind=='utf8':require(type(v) is str and v.isascii() and 0<len(v)<=LIMITS['sourceBytes'],1,path)
 else:
  try:paid_leaves.typed(v,kind,path,0)
  except ValueError as e:raise Invalid(1,path) from e
def canonical(v,kind):
 if kind.endswith('?'):return None if v is None else canonical(v,kind[:-1])
 if kind in INVENTORY['effectTags']:return canonical(v,INVENTORY['effectTags'][kind][v['kind']])
 if kind in SCHEMA:return {f:canonical(v[f],t) for f,t in SCHEMA[kind]}
 if kind.endswith('[]'):return [canonical(x,kind[:-2]) for x in v]
 if kind in ('phase','utf8'):return v
 return paid_leaves.canonical(v,kind)
def byte_limit(kind):
 return LIMITS['sourceBytes'] if kind=='Source' else LIMITS['proofBytes'] if kind=='FamilyProof' else LIMITS['eventBytes'] if kind.endswith('Event') else LIMITS['inputBytes'] if kind.endswith('Input') or kind.endswith('Command') else LIMITS['controlBytes']
def raw(v,kind):
 typed(v,kind);data=encode(canonical(v,kind));require(len(data)<=byte_limit(kind),1,'/'+kind);return data
def parse(data,kind):
 require(type(data) is bytes and 0<len(data)<=byte_limit(kind),1)
 def pairs(items):
  obj={}
  for k,v in items:require(k not in obj,1);obj[k]=v
  return obj
 try:v=json.loads(data.decode('ascii'),object_pairs_hook=pairs,parse_constant=lambda _:(_ for _ in ()).throw(Invalid(1)))
 except (ValueError,UnicodeError,RecursionError) as e:raise Invalid(1) from e
 require(raw(v,kind)==data,8);return v
def envelope(phase,inp):return encode(dict(phase=phase,input=inp))
def parse_envelope(data):
 require(type(data) is bytes and 0<len(data)<=4096,1)
 obj=json.loads(data);require(type(obj) is dict and list(obj)==['phase','input'] and obj['phase'] in ['selection','round','paid'],1)
 kind={'selection':'SelectionInput','round':'RoundInput','paid':'PaidInput'}[obj['phase']]
 raw(obj['input'],kind);require(encode(obj)==data,8);return obj['phase'],obj['input']
def combat_edge(b):return selection_leaves.edge(b)
def selection_window(state,b,kind,now):
 timing=selection_leaves.inputs.make_timing(selection_leaves.env.Context().config,kind,now)
 side=b['cycle']['actingSide'];return dict(decisionId=state['segmentId']+'.'+kind,owner=side if kind=='selection' else 'commonwealth' if side=='axis' else 'axis',timing=timing)
def round_prior_selection(b):return b['selectionProof']['control']
def round_boundary(b):return b['selectionProof']['boundary']
def round_positions(b):return combat_edge(round_boundary(b))['combatPositionIds']
def round_base_hash(b):return 'sha256:'+phase_digest('round','base',canonical(b,'RoundBase'))
def round_clock_hash(b):return 'sha256:'+phase_digest('round','configuration',canonical(b['clockConfiguration'],'ClockConfiguration'))
def round_hash_of(kind,v):return 'sha256:'+phase_digest('round',kind,v)
def round_digest(kind,v):return phase_digest('round',kind,v)
def paid_digest(kind,v):return phase_digest('paid',kind,v)
# Compatible pure arithmetic/projection helpers take factual carriers, never legacy admission proofs.
result_facts=paid_leaves.result_facts
project=paid_leaves.project
make_window=paid_leaves.make_window
component=paid_leaves.component
route=paid_leaves.route
cp=paid_leaves.cp
def selection_initial(b):
    return dict(contractVersion=1, boundaryHash=sha(raw(b, 'MovedBoundary')),
        segmentId='asg.'+digest(INVENTORY['domains']['selection']['segment'], raw(b, 'MovedBoundary')),
        stateVersion=b['priorVersion'], prefix=b['priorPrefix'], stepIndex=0, selectionOutcome='unopened', selection=None,
        selectionReceiptId=None, declineReceiptId=None, cancellationReceiptId=None, selectionWindow=None, rbaWindow=None,
        stepReceipts=[], receipts=[], closed=False)


def selection_active_window(s):
    if s['selectionOutcome'] == 'pending': return s['selectionWindow']
    if s['selectionOutcome'] == 'selected' and s['rbaWindow'] is not None and s['declineReceiptId'] is None: return s['rbaWindow']
    return None


def selection_admitted_timing(w, inp):
    t = copy.deepcopy(w['timing']); now = inp['admittedAt']
    if now is not None: t['highWaterUnixMilliseconds'] = max(now, t['highWaterUnixMilliseconds'])
    return t


def selection_transition(b, prior, inp):
    """Immutable contract transition from independently trusted boundary + authenticated input."""
    typed(inp, 'SelectionInput'); cmd = inp['command']; kind = cmd['kind']; actor = inp['actor']
    require(cmd['contractVersion'] == 1 and kind in KINDS, 3)
    allowed = {'open-segment': {'expectedPriorVersion'}, 'close-empty-selection': {'expectedPriorVersion'},
        'choose-selection': {'decisionId', 'choice', 'candidate'}, 'decline-rba': {'decisionId', 'participant'},
        'complete-step': {'fromPositionId', 'expectedPriorVersion'}, 'open-rba': {'fromPositionId', 'expectedPriorVersion'},
        'expire-window': {'decisionId'}, 'controller-unavailable': {'decisionId'}}[kind]
    for k in ('decisionId', 'fromPositionId', 'expectedPriorVersion', 'choice', 'candidate', 'participant'):
        require(k in allowed or cmd[k] is None, 3, '/command/'+k)
    require(cmd['segmentId'] == prior['segmentId'], 4)
    system = kind not in ('choose-selection', 'decline-rba')
    require(actor == 'system' if system else actor in ('axis', 'commonwealth'), 4, '/actor')
    ch = sha(encode(canonical(cmd, 'SelectionCommand')))
    duplicate = next((r for r in prior['receipts'] if r['commandHash'] == ch), None)
    if duplicate:
        require(duplicate['actor'] == actor, 4, '/actor')
        return copy.deepcopy(prior), None, duplicate['receiptId']
    w = selection_active_window(prior)
    before_rba_unavailable = (kind == 'controller-unavailable' and prior['selectionOutcome'] == 'selected'
        and prior['stepIndex'] == 2 and prior['rbaWindow'] is None and cmd['decisionId'] == prior['segmentId']+'.rba')
    if not before_rba_unavailable and kind in ('expire-window', 'controller-unavailable') and (w is None or cmd['decisionId'] != w['decisionId']):
        return copy.deepcopy(prior), None, None
    require(not prior['closed'], 6)
    require(len(prior['receipts']) < 16 and prior['stateVersion'] < 2**63-1, 6)
    s = copy.deepcopy(prior); choices = candidate(b); step = s['stepIndex']; positions = combat_edge(b)['combatPositionIds']
    if kind in ('open-segment', 'close-empty-selection', 'complete-step', 'open-rba'):
        require(cmd['expectedPriorVersion'] == prior['stateVersion'], 6)
    if kind in ('complete-step', 'open-rba'): require(cmd['fromPositionId'] == positions[step], 6)
    if kind == 'open-segment':
        require(s['selectionOutcome'] == 'unopened' and not s['receipts'], 6)
        require(inp['admittedAt'] is not None if choices and inp['clockAvailable'] else True, 5)
        require(choices is not None or inp['admittedAt'] is None, 5)
        opens_window = choices is not None and inp['clockAvailable']
        s['selectionOutcome'] = 'pending' if opens_window else 'system-no-selection'
        s['selectionWindow'] = selection_window(s, b, 'selection', inp['admittedAt']) if opens_window else None
        effect = dict(kind='segment-opened', boundaryHash=s['boundaryHash'], window=s['selectionWindow'])
    elif kind == 'close-empty-selection':
        require(s['selectionOutcome'] == 'system-no-selection' and s['selectionWindow'] is None, 6)
        require(inp['admittedAt'] is None and inp['clockAvailable'], 5)
        effect = dict(kind='selection-closed', outcome='no-selection', candidate=None, timing=None)
    elif kind == 'choose-selection':
        require(s['selectionOutcome'] == 'pending' and w is not None and cmd['decisionId'] == w['decisionId'], 6)
        require(actor == w['owner'], 4)
        require(cmd['choice'] in ('select-close-assault', 'finish-without-attack'), 3)
        chosen = cmd['choice'] == 'select-close-assault'
        require(cmd['candidate'] == choices if chosen else cmd['candidate'] is None, 4)
        require(inputs.clock_gate(w['timing'], inp['admittedAt'], inp['clockAvailable']) == 'before-deadline', 5)
        effect = dict(kind='selection-closed', outcome='selected' if chosen else 'no-selection', candidate=choices if chosen else None,
                      timing=selection_admitted_timing(w, inp))
    elif kind == 'open-rba':
        require(step == 2 and s['selectionOutcome'] == 'selected' and s['rbaWindow'] is None, 6)
        require(inp['clockAvailable'] and inp['admittedAt'] is not None, 5)
        require(inp['admittedAt'] >= s['selectionWindow']['timing']['highWaterUnixMilliseconds'], 5)
        s['rbaWindow'] = selection_window(s, b, 'rba', inp['admittedAt'])
        effect = dict(kind='rba-opened', selectionReceiptId=s['selectionReceiptId'], window=s['rbaWindow'])
    elif kind == 'decline-rba':
        require(step == 2 and s['selectionOutcome'] == 'selected' and w is not None and cmd['decisionId'] == w['decisionId'], 6)
        require(actor == w['owner'] and cmd['participant'] == s['selection']['defender']['unit'], 4)
        require(inputs.clock_gate(w['timing'], inp['admittedAt'], inp['clockAvailable']) == 'before-deadline', 5)
        effect = dict(kind='rba-declined', selectionReceiptId=s['selectionReceiptId'], participant=cmd['participant'], timing=selection_admitted_timing(w, inp))
    elif kind in ('expire-window', 'controller-unavailable'):
        if before_rba_unavailable:
            effect = dict(kind='selection-cancelled', selectionReceiptId=s['selectionReceiptId'], timing=None)
        else:
            require(w is not None, 6)
            gate = inputs.clock_gate(w['timing'], inp['admittedAt'], inp['clockAvailable'])
            if kind == 'expire-window' and gate == 'before-deadline': return copy.deepcopy(prior), None, None
            if s['selectionOutcome'] == 'pending':
                effect = dict(kind='selection-closed', outcome='no-selection', candidate=None, timing=selection_admitted_timing(w, inp))
            else: effect = dict(kind='selection-cancelled', selectionReceiptId=s['selectionReceiptId'], timing=selection_admitted_timing(w, inp))
    else:
        require(s['selectionOutcome'] in ('selected', 'no-selection', 'cancelled'), 6)
        no_attack = s['selectionOutcome'] != 'selected'
        if not no_attack and step == 2: require(s['declineReceiptId'] is not None, 6)
        require(inp['admittedAt'] is None and inp['clockAvailable'], 5)
        require(no_attack or step < 3, 7)  # Prepared/committed gates belong to C3b/c.
        disposition = s['cancellationReceiptId'] or (s['declineReceiptId'] if step >= 2 else None) or s['selectionReceiptId']
        effect = dict(kind='step-completed', fromPositionId=positions[step],
            toPositionId=positions[step+1] if step < 5 else combat_edge(b)['releasePositionId'],
            previousStepReceiptId=s['stepReceipts'][-1] if s['stepReceipts'] else s['receipts'][0]['receiptId'],
            dispositionReceiptId=disposition, proofKind='no-attack' if no_attack else ['no-gun-positions', 'no-barrage-work', 'accepted-decline'][step])
    event = dict(contractVersion=1, eventType='moved-selection-'+effect['kind'], campaignId=b['cycle']['campaignId'], rulesetHash=b['cycle']['rulesetHash'],
        configurationHash=b['cycle']['admittedPolicyBundleDigest'], entrySourceHash=b['entrySourceHash'], cycleId=b['cycleId'],
        segmentId=s['segmentId'], priorVersion=prior['stateVersion'], stateVersion=prior['stateVersion']+1, priorPrefix=prior['prefix'],
        input=canonical(inp, 'SelectionInput'), effect=effect)
    rid = 'asc.'+digest(INVENTORY['domains']['selection']['receipt'], encode(event)); event['receiptId'] = rid
    event_bytes = raw(event, 'SelectionEvent'); require(len(event_bytes) <= 1048576, 1)
    if effect['kind'] == 'selection-closed':
        s.update(selectionOutcome=effect['outcome'], selection=effect['candidate'], selectionReceiptId=rid)
        if s['selectionWindow'] is not None: s['selectionWindow']['timing'] = effect['timing']
    elif effect['kind'] == 'rba-declined': s['declineReceiptId'] = rid; s['rbaWindow']['timing'] = effect['timing']
    elif effect['kind'] == 'selection-cancelled':
        s.update(selectionOutcome='cancelled', cancellationReceiptId=rid)
        if s['rbaWindow'] is not None: s['rbaWindow']['timing'] = effect['timing']
    elif effect['kind'] == 'step-completed':
        s['stepReceipts'].append(rid); s['stepIndex'] += 1; s['closed'] = s['stepIndex'] == 6
    s['stateVersion'] = event['stateVersion']; s['prefix'] = seq.prefix_event(prior['prefix'], event_bytes)
    s['receipts'].append(dict(commandHash=ch, eventHash=sha(event_bytes), receiptId=rid, actor=actor, stateVersion=s['stateVersion']))
    typed(s, 'SelectionControl'); require(len(encode(s)) <= 1048576, 1)
    return s, event_bytes, rid


def round_initial(b):
    s=round_prior_selection(b); a=round_boundary(b)
    return dict(contractVersion=1,baseHash=round_base_hash(b),clockConfigurationHash=round_clock_hash(b),segmentId=s['segmentId'],
        opportunityId=None,roundId=None,stateVersion=s['stateVersion'],prefix=s['prefix'],stepIndex=3,status='unopened',
        openingReceiptId=None,timing=None,slots=[],stepReceipts=copy.deepcopy(s['stepReceipts']),world=copy.deepcopy(a['world']),
        randomState=copy.deepcopy(a['randomState']),attackHistory=[],targetUses=[],commitmentId=None,
        cancellationReceiptId=None,receipts=[],closed=False)


def round_command(b,s,kind,role=None):
    slot=next((x for x in s['slots'] if x['role']==role),None)
    structural=kind in ('open-round','complete-step')
    return dict(contractVersion=1,kind=kind,clockConfigurationHash=round_clock_hash(b),segmentId=s['segmentId'],
        roundId=None if kind=='open-round' else s['roundId'],slotId=slot['slotId'] if kind=='seal-choice' and slot else None,
        expectedPriorVersion=s['stateVersion'] if structural else None,
        fromPositionId=round_positions(b)[s['stepIndex']] if kind=='complete-step' and s['stepIndex']<6 else None,
        allocation=copy.deepcopy(slot['allocation']) if kind=='seal-choice' and slot else None)


def round_gate(timing,now,available):
    if not available or now is None or now<timing['openingFloorUnixMilliseconds']: return 'unavailable'
    return 'expired' if now>=timing['deadlineUnixMilliseconds'] else 'live'


def round_empty_aa(b,s):
    """Joined retained Content + exact actual World + full assigned components; no caller emptiness."""
    path='docs/specs/fixtures/combat-content-v7.canonical.json'
    try: data=(REPOSITORY/path).read_bytes()
    except (OSError,KeyError) as error: raise Invalid(9,path) from error
    require(hashlib.sha256(data).hexdigest()==INVENTORY['runtimeSourceHashes'][path],9,path)
    content=json.loads(data)
    candidate=round_prior_selection(b)['selection']; ids=[]
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
    return round_hash_of('empty-aa',canonical(certificate,'EmptyAaCertificate'))


def round_transition(b,prior,inp,admission_enabled=True):
    """Private kernel; no derived Base/prior admission API. apply/replay always authenticate source."""
    typed(inp,'RoundInput');require(type(admission_enabled) is bool,1,'/admissionEnabled')
    cmd,actor=inp['command'],inp['actor'];kind=cmd['kind']
    require(cmd['contractVersion']==1 and kind in ROUND_SUPPORTED+ROUND_UNSUPPORTED,3,'/command')
    structural=kind in ('open-round','complete-step')
    require((cmd['expectedPriorVersion'] is not None)==structural,3,'/command/expectedPriorVersion')
    require((cmd['fromPositionId'] is not None)==(kind=='complete-step'),3,'/command/fromPositionId')
    require((cmd['slotId'] is not None and cmd['allocation'] is not None) if kind=='seal-choice'
            else cmd['slotId'] is None and cmd['allocation'] is None,3,'/command/allocation')
    require((cmd['roundId'] is None)==(kind=='open-round'),3,'/command/roundId')
    require(cmd['segmentId']==prior['segmentId'] and cmd['clockConfigurationHash']==round_clock_hash(b),4,'/command/context')
    require(actor in ('axis','commonwealth') if kind=='seal-choice' else actor=='system',4,'/actor')
    ch=sha(raw(cmd,'RoundCommand'));duplicate=next((x for x in prior['receipts'] if x['commandHash']==ch),None)
    if duplicate:
        require(actor==duplicate['actor'],4,'/actor');return copy.deepcopy(prior),None,duplicate['receiptId']
    if kind in ('expire-round','controller-unavailable') and (cmd['roundId']!=prior['roundId'] or prior['status']!='collecting'):
        return copy.deepcopy(prior),None,None
    require(not prior['closed'] and prior['stateVersion']<2**63-1 and len(prior['receipts'])<16,6,'/lifecycle')
    if kind!='open-round':require(cmd['roundId']==prior['roundId'],4,'/command/roundId')
    if structural:require(cmd['expectedPriorVersion']==prior['stateVersion'],6,'/command/expectedPriorVersion')
    if kind=='complete-step':require(prior['stepIndex']<6 and cmd['fromPositionId']==round_positions(b)[prior['stepIndex']],6,'/command/fromPositionId')
    s=copy.deepcopy(prior);now=inp['admittedAt'];author=actor
    if kind=='open-round':
        require(s['status']=='unopened' and s['stepIndex']==3 and not s['receipts'],6,'/lifecycle')
        require(admission_enabled,7,'/admissionEnabled')
        require(inp['clockAvailable'] and now is not None and now+30000<=MAX_UTC,5,'/admittedAt')
        timing=dict(contractVersion=1,clockConfigurationHash=round_clock_hash(b),kind='force-assignment',decisionBudgetMilliseconds=30000,
                    openedAtUnixMilliseconds=now,deadlineUnixMilliseconds=now+30000,openingFloorUnixMilliseconds=now)
        identity=dict(baseHash=round_base_hash(b),selectionSourceHash=b['selectionProof']['sourceHash'],
            segmentId=s['segmentId'],cycleId=round_boundary(b)['cycleId'],positionId=round_positions(b)[3],
            candidate=round_prior_selection(b)['selection'],declineReceiptId=round_prior_selection(b)['declineReceiptId'])
        s['opportunityId']='aopp.'+round_digest('opportunity',identity)
        s['roundId']='arnd.'+round_digest('round',dict(baseHash=round_base_hash(b),opportunityId=s['opportunityId'],
                    openingAuthorityVersion=s['stateVersion'],openingHistoryPrefix=s['prefix'],timing=timing))
        for role in ('attacker','defender'):
            p=round_prior_selection(b)['selection'][role]
            s['slots'].append(dict(role=role,owner=p['unit']['originalSide'],slotId='aslt.'+round_digest('slot',dict(roundId=s['roundId'],role=role)),
                allocation=dict(kind='full-close-assault',unit=copy.deepcopy(p['unit']),componentId=p['componentIds'][0],committedToe=10),sealedReceiptId=None,sealedAt=None))
        s['status'],s['timing']='collecting',timing
        effect=dict(kind='round-opened',opportunityId=s['opportunityId'],timing=copy.deepcopy(timing),slots=copy.deepcopy(s['slots']))
    elif kind=='seal-choice':
        require(s['status']=='collecting' and s['stepIndex']==3,6,'/lifecycle')
        slot=next((x for x in s['slots'] if x['slotId']==cmd['slotId']),None)
        require(slot is not None and slot['owner']==actor,4,'/command/slotId')
        require(slot['sealedReceiptId'] is None,6,'/command/slotId')
        require(cmd['allocation']==slot['allocation'],4,'/command/allocation')
        clock=round_gate(s['timing'],now,inp['clockAvailable']);require(clock!='expired',5,'/admittedAt')
        if clock=='unavailable':
            effect=dict(kind='round-cancelled',cause='clock-unavailable',timing=copy.deepcopy(s['timing']));author='system'
        else:
            effect=dict(kind='choice-sealed',slotId=slot['slotId'],allocation=copy.deepcopy(slot['allocation']),
                timing=copy.deepcopy(s['timing']),prepared=any(x['sealedReceiptId'] is not None for x in s['slots']))
    elif kind in ('expire-round','controller-unavailable'):
        clock=round_gate(s['timing'],now,inp['clockAvailable'])
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
        effect=dict(kind='step-completed',fromPositionId=round_positions(b)[index],
            toPositionId=round_positions(b)[index+1] if index<5 else selection.edge(round_boundary(b))['releasePositionId'],
            previousStepReceiptId=s['stepReceipts'][-1],proofKind='no-attack' if cancel else ('prepared-full-assignment' if index==3 else 'certified-empty-aa'),
            proofReceipts=proofs,certificateHash=round_empty_aa(b,s) if index==4 and not cancel else None)
    else:
        require(now is None and inp['clockAvailable'],5,'/admittedAt');raise Invalid(7,'/continuation')
    event=dict(contractVersion=1,eventType='moved-round-'+effect['kind'],author=author,
        campaignId=round_boundary(b)['cycle']['campaignId'],rulesetHash=round_boundary(b)['cycle']['rulesetHash'],
        configurationHash=round_clock_hash(b),predecessorConfigurationHash=b['clockConfiguration']['parentConfigurationHash'],
        selectionSourceHash=b['selectionProof']['sourceHash'],baseHash=round_base_hash(b),cycleId=round_boundary(b)['cycleId'],
        segmentId=s['segmentId'],roundId=s['roundId'],priorVersion=prior['stateVersion'],stateVersion=prior['stateVersion']+1,
        priorPrefix=prior['prefix'],input=canonical(inp,'RoundInput'),effect=canonical(effect,'RoundEffect'))
    receipt='arc.'+round_digest('receipt',event);event['receiptId']=receipt;data=raw(event,'RoundEvent')
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


def paid_initial(ctx):
    p=ctx['prior']
    require(p['randomState']['seed']==1 and p['randomState']['nextByteCursor']==2,4,'/randomState')
    return dict(contractVersion=1,baseHash='sha256:'+paid_digest('base',ctx['base']),roundClockConfigurationHash=p['clockConfigurationHash'],resultClockPolicyId=INVENTORY['resultClockPolicyId'],stateVersion=p['stateVersion'],prefix=p['prefix'],status='prepared',commitmentId=None,result=None,settlementId=None,window=None,acceptedHighWater=p['timing']['openingFloorUnixMilliseconds'],world=copy.deepcopy(p['world']),randomState=copy.deepcopy(p['randomState']),attackHistory=[],targetUses=[],roundClosureReceiptId=None,caCompletionReceiptId=None,receipts=[],closed=False)


def paid_paid_world(ctx):
    p=ctx['prior'];w=copy.deepcopy(p['world']);costs=[]
    require(not p['attackHistory'] and not p['targetUses'] and not w['settlements'] and not w['custodyLots'],7,'/paidBoundary')
    for r,cost in (('attacker',5),('defender',3)):
        allocation=next(x['allocation'] for x in p['slots'] if x['role']==r)
        unit=ctx['selection'][r]['unit'];require(allocation['unit']==unit and allocation['committedToe']==10,7,'/allocation')
        e=next(x for x in w['elements'] if x['elementId']==unit['elementId']);before=e['operationalState']['capabilityPointsExpended']
        require(before['denominator']==1 and before['numerator']==(4 if r=='attacker' else 0) and e['ammunition']['points']==10 and e['components'][0]['currentToe']==10,7,'/resources')
        e['operationalState']['capabilityPointsExpended']=cp(before['numerator']+cost);e['ammunition']['points']=0
        costs.append(dict(unit=copy.deepcopy(unit),beforeCp=before['numerator'],afterCp=before['numerator']+cost,beforeAmmo=10,afterAmmo=0))
    return w,costs


def paid_resolve_result(ctx,s):
    consume('docs/research/verify-combat-rng.py')
    spec=importlib.util.spec_from_file_location('paid_rng',REPOSITORY/'docs/research/verify-combat-rng.py');rng=importlib.util.module_from_spec(spec);spec.loader.exec_module(rng)
    before=copy.deepcopy(s['randomState']);cursor=before['nextByteCursor'];draws=[];dice=[]
    for label in ctx['rules']['procedure']['orderedPurposes']:
        try:die,end,consumed=rng.roll(before['seed'],cursor)
        except (ValueError,OverflowError) as error:raise Invalid(7,'/randomState') from error
        require(len(consumed)<=LIMITS['consumedBytesPerDraw'],7,'/drawCapacity')
        draws.append(dict(purpose=label,beforeCursor=cursor,afterCursor=end,die=die,consumed=list(consumed)));dice.append(die);cursor=end
    require(len(dice)==8,7,'/procedure')
    ac,dc=10*dice[0]+dice[1],10*dice[2]+dice[3]
    morale={x['coordinate']:x['adjustment'] for x in ctx['rules']['morale']};am,dm=morale[ac],morale[dc];diff=am-dm
    effect=next(x for x in ctx['rules']['effects'] if x['differential']==diff)
    a,d=10*dice[4]+dice[5],10*dice[6]+dice[7]
    role='attacker' if a//10+a%10 in effect['attackerCaptureSums'] else 'defender' if d//10+d%10 in effect['defenderCaptureSums'] else None
    capture=None
    if role:
        try:capture,end,consumed=rng.roll(before['seed'],cursor)
        except (ValueError,OverflowError) as error:raise Invalid(7,'/randomState') from error
        require(len(consumed)<=LIMITS['consumedBytesPerDraw'],7,'/drawCapacity')
        draws.append(dict(purpose=role+'.capture.share',beforeCursor=cursor,afterCursor=end,die=capture,consumed=list(consumed)));cursor=end
    facts=result_facts(ctx['rules'],diff,a,d,capture)
    v=dict(commitmentId=s['commitmentId'],rulesInputHash=ctx['config']['rulesInputHash'],procedureId=ctx['rules']['procedure']['procedureId'],beforeRandomState=before,afterRandomState=before|dict(nextByteCursor=cursor),draws=draws,attackerMoraleCoordinate=ac,defenderMoraleCoordinate=dc,attackerMorale=am,defenderMorale=dm,basicDifferential=0,facts=facts)
    result=dict(resultId='res.'+hashlib.sha256(INVENTORY['inheritedResultDomains']['result'].encode()+b'\0'+encode(v)).hexdigest(),**v);raw(result,'AssaultResult');return result


def paid_projected(ctx,s,cut,retreat_choice=None,custody_choice=None):
    st=s['world']['settlements'][0] if s['world']['settlements'] else None
    retreat_choice=retreat_choice or (st['disposition']['kind'] if st and st['disposition'] else None)
    custody_choice=custody_choice or (st['custody']['kind'] if st and st['custody'] else None)
    paid,_=paid_paid_world(ctx)
    return project(ctx,paid,s['result'],cut,retreat_choice,custody_choice)


def paid_certify_support(ctx):
    require(SUPPORTED_HANDLERS==set(INVENTORY['supportHandlers']),7,'/supportHandlers')
    for kind,key in (('PaidSource','sourceBytes'),('PaidProof','proofBytes'),('PaidControl','controlBytes'),('PaidEvent','eventBytes'),('PaidInput','inputBytes'),('PaidLedger','paidLedgerBytes')):
        require(LIMITS[key]>=INVENTORY['encodedUpperBounds'][kind],7,'/supportCapacity/'+kind)
    require(LIMITS['acceptedEvents']>=11 and LIMITS['acceptedReceipts']>=11 and LIMITS['draws']>=9 and LIMITS['consumedBytesPerDraw']>=512 and LIMITS['routes']>=9,7,'/supportCollectionCapacity')
    w,_=paid_paid_world(ctx);summaries={};counts={'morale':0,'assault-cell':0,'capture-refusal':0};coverage={k:set() for k in counts}
    morale={x['coordinate']:x['adjustment'] for x in ctx['rules']['morale']}
    for ordinal,row in enumerate(aad.catalogue_records(ctx['rules'])):
        require(type(row) is dict and row.get('kind') in counts and row.get('ordinal')==ordinal,7,'/supportIndex')
        try:aad.typed(row,aad.KINDS[row['kind']])
        except aad.Invalid as error:raise Invalid(7,'/supportShape') from error
        counts[row['kind']]+=1
        key=(row['attackerMoraleCoordinate'],row['defenderMoraleCoordinate']) if row['kind']=='morale' else (row['differential'],row['attackerCoordinate'],row['defenderCoordinate']) if row['kind']=='assault-cell' else tuple(row['facts'][k] for k in ('differential','attackerCoordinate','defenderCoordinate','captureDie'))+(row['refusalHexes'],)
        require(key not in coverage[row['kind']],7,'/supportDuplicate');coverage[row['kind']].add(key)
        if row['kind']=='morale':
            ac,dc=row['attackerMoraleCoordinate'],row['defenderMoraleCoordinate']
            require(ac in morale and dc in morale and row['attackerAdjustment']==morale[ac] and row['defenderAdjustment']==morale[dc] and row['differential']==morale[ac]-morale[dc],7,'/supportMorale')
            continue
        if row['kind']=='assault-cell':
            effect=next(x for x in ctx['rules']['effects'] if x['differential']==row['differential'])
            ac,dc=row['attackerCoordinate'],row['defenderCoordinate']
            captured=ac//10+ac%10 in effect['attackerCaptureSums'] or dc//10+dc%10 in effect['defenderCaptureSums']
            expected=result_facts(ctx['rules'],row['differential'],ac,dc,1 if captured else None)
            require(all(row[k]==expected[k] for k in ('differential','attackerCoordinate','defenderCoordinate','attackerPercent','defenderPercent','rawEngaged','requiredRetreat','capturedRole')) and row['captureTriggered']==captured,7,'/supportAssaultCell')
            continue
        facts=row['facts']
        require(result_facts(ctx['rules'],facts['differential'],facts['attackerCoordinate'],facts['defenderCoordinate'],facts['captureDie'])==facts,7,'/supportFacts')
        require(0<=row['refusalHexes']<=facts['requiredRetreat'],7,'/supportRefusal')
        a=(10*facts['attackerPercent']+99)//100;d=10*(facts['defenderPercent']+10*row['refusalHexes'])//100
        ca=(a*facts['captureShare']+99)//100 if facts['capturedRole']=='attacker' else 0
        cd=(d*facts['captureShare']+99)//100 if facts['capturedRole']=='defender' else 0
        expected=dict(attackerLossToe=a,defenderLossToe=d,attackerCapturedToe=ca,defenderCapturedToe=cd,attackerOtherLossToe=a-ca,defenderOtherLossToe=d-cd,attackerRemainingToe=10-a,defenderRemainingToe=10-d,attackerLossDp=3 if a>=3 else 0,defenderLossDp=3 if d>=3 else 0)
        require(all(row[k]==v for k,v in expected.items()),7,'/supportLossArithmetic')
        key=tuple(facts[x] for x in ('attackerPercent','defenderPercent','rawEngaged','requiredRetreat','capturedRole','captureShare'))+(row['refusalHexes'],)
        summaries[key]=row
    require(counts=={'morale':1296,'assault-cell':6480,'capture-refusal':8840},7,'/supportCoverage')
    coordinates=set(morale)
    require(coverage['morale']=={(a,d) for a in coordinates for d in coordinates},7,'/supportMoraleCoverage')
    expected_cells={(e['differential'],a,d) for e in ctx['rules']['effects'] for a in coordinates for d in coordinates}
    require(coverage['assault-cell']==expected_cells,7,'/supportCellCoverage')
    expected_variants=set()
    for diff,a,d in expected_cells:
        effect=next(e for e in ctx['rules']['effects'] if e['differential']==diff)
        captured=a//10+a%10 in effect['attackerCaptureSums'] or d//10+d%10 in effect['defenderCaptureSums']
        for die in range(1,7) if captured else (None,):
            facts=result_facts(ctx['rules'],diff,a,d,die)
            for refusal in range(facts['requiredRetreat']+1):expected_variants.add((diff,a,d,die,refusal))
    require(coverage['capture-refusal']==expected_variants,7,'/supportVariantCoverage')
    for row in summaries.values():
        facts=row['facts'];choice='not-required' if not facts['requiredRetreat'] else 'refuse-retreat' if row['refusalHexes'] else 'retreat'
        captured=row['attackerCapturedToe']+row['defenderCapturedToe']>0
        result=dict(resultId='res.'+'f'*64,commitmentId='cmt.'+'f'*64,facts=facts)
        for custody in ('relocate-and-guard','leave-unguarded') if captured else (None,):
            for cut in ('resolved','disposition','losses','retreat','custody','relationships'):
                if cut=='custody' and not captured:continue
                world=project(ctx,w,result,cut,choice,custody);raw(world,'World')
            require(sum(x['components'][0]['currentToe'] for x in world['elements'])+sum(x['toe'] for x in world['guards'])+row['attackerLossToe']+row['defenderLossToe']==20,7,'/supportConservation')
    for kind in ('retreat','custody'):
        fallback=next(x['fallback'] for x in ctx['config']['windows'] if x['kind']==kind)
        require(fallback==('refuse-retreat' if kind=='retreat' else 'leave-unguarded'),7,'/fallback')
        for owner in ('axis','commonwealth'):
            require(make_window(ctx,kind,owner,'set.'+'f'*64,10000,True) is not None,7,'/windowSupport')
            require(make_window(ctx,kind,owner,'set.'+'f'*64,None,True) is None and make_window(ctx,kind,owner,'set.'+'f'*64,MAX_UTC,True) is None,7,'/windowFallbackSupport')
    require(ctx['prior']['randomState']['seed']==1 and ctx['prior']['randomState']['nextByteCursor']==2,7,'/rngCapacity')
    return counts


def paid_transition(ctx,prior,inp,fresh=True):
    verify_delivery();verify_dependencies();typed(prior,'PaidControl');typed(inp,'PaidInput');cmd=inp['command'];kind=cmd['kind'];actor=inp['actor'];now=inp['admittedAt']
    require(kind in paid_leaves.INVENTORY['commands'],3,'/kind')
    require(cmd['contractVersion']==1 and cmd['baseHash']==prior['baseHash'] and cmd['roundId']==ctx['prior']['roundId'],4,'/context')
    structural=kind in ('commit','resolve','advance')
    require((cmd['expectedPriorVersion'] is not None)==structural and (cmd['choice'] is not None)==(kind=='choose'),3,'/command')
    require(actor in ('axis','commonwealth') if kind=='choose' else actor=='system',4,'/actor')
    require(cmd['commitmentId']==(None if kind=='commit' else prior['commitmentId']),4,'/commitmentId')
    require((cmd['decisionId'] is None)==structural,3,'/decisionId')
    ch=sha(raw(cmd,'PaidCommand'));retained=next((r for r in prior['receipts'] if r['commandHash']==ch and r['actor']==actor),None)
    if retained:return copy.deepcopy(prior),None,retained['receiptId']
    window=prior['window']
    if kind in ('expire','unavailable') and (window is None or cmd['decisionId']!=window['decisionId']):return copy.deepcopy(prior),None,None
    require(not prior['closed'] and len(prior['receipts'])<LIMITS['acceptedReceipts'] and prior['stateVersion']<2**63-1,7,'/transitionCapacity')
    require(cmd['expectedPriorVersion']==prior['stateVersion'] if structural else window is not None and cmd['decisionId']==window['decisionId'],6,'/priorVersionOrDecision')
    s=copy.deepcopy(prior);author=actor;effect=None
    def choice_record(choice,reason,timing):
        nonlocal effect
        wk=window['kind'] if window else ('retreat' if s['status']=='resolved' else 'custody')
        cut='disposition' if wk=='retreat' else 'custody'
        s['world']=paid_projected(ctx,s,cut,choice if wk=='retreat' else None,choice if wk=='custody' else None)
        effect=dict(kind='disposition-recorded' if wk=='retreat' else 'custody-settled',reason=reason,timing=timing,payload=s['world']['settlements'][0][cut]);s['status']=cut;s['window']=None
    if kind=='commit':
        require(prior['status']=='prepared' and now is None and inp['clockAvailable'],6,'/commit')
        if fresh:paid_certify_support(ctx)
        paid,costs=paid_paid_world(ctx);allocations=[copy.deepcopy(x['allocation']) for x in ctx['prior']['slots']]
        preimage=dict(baseHash=prior['baseHash'],priorVersion=prior['stateVersion'],priorPrefix=prior['prefix'],roundId=ctx['prior']['roundId'],allocations=allocations,costs=costs,preResultRandomState=copy.deepcopy(prior['randomState']))
        raw(preimage,'CommitmentPreimage');commitment='cmt.'+paid_digest('commitment',preimage)
        effect=dict(kind='attack-committed',commitmentId=commitment,allocations=allocations,costs=costs,preResultRandomState=copy.deepcopy(prior['randomState']))
        s['world']=paid;s['commitmentId']=commitment;s['status']='committed'
        s['attackHistory']=[dict(commitmentId=commitment,cycleId=ctx['boundary']['cycleId'],segmentId=ctx['prior']['segmentId'],attacker=copy.deepcopy(ctx['selection']['attacker']['unit']),defender=copy.deepcopy(ctx['selection']['defender']['unit']),targetLocationId=ctx['selection']['targetLocationId'],gameTurn=ctx['cycle']['gameTurn'],operationStage=ctx['cycle']['operationStage'])]
        s['targetUses']=[dict(commitmentId=commitment,segmentId=ctx['prior']['segmentId'],targetLocationId=ctx['selection']['targetLocationId'])]
    elif kind in ('choose','expire','unavailable'):
        require(window is not None,6,'/window');choices=('retreat','refuse-retreat') if window['kind']=='retreat' else ('relocate-and-guard','leave-unguarded')
        if kind=='choose':require(actor==window['owner'] and cmd['choice'] in choices,4,'/choiceOwner')
        timing=copy.deepcopy(window['timing']);lost=not inp['clockAvailable'] or now is None or now<timing['highWaterUnixMilliseconds'];late=now is not None and now>=timing['deadlineUnixMilliseconds']
        if kind=='expire' and not lost and not late:return copy.deepcopy(prior),None,None
        if kind=='choose' and not lost:require(not late,5,'/deadline')
        fallback=kind!='choose' or lost
        if not lost:s['acceptedHighWater']=max(s['acceptedHighWater'],now);timing['highWaterUnixMilliseconds']=max(timing['highWaterUnixMilliseconds'],now)
        reason=('clock-unavailable' if lost else 'controller-unavailable' if kind=='unavailable' else 'deadline') if fallback else 'owner-choice'
        if fallback:author='system'
        choice_record(choices[-1] if fallback else cmd['choice'],reason,timing)
    elif kind=='resolve':
        require(prior['status']=='committed' and now is None and inp['clockAvailable'],6,'/resolve')
        result=paid_resolve_result(ctx,s);s['result']=result;s['randomState']=copy.deepcopy(result['afterRandomState']);s['world']=paid_projected(ctx,s,'resolved');s['settlementId']=s['world']['settlements'][0]['settlementId'];s['status']='resolved'
        effect=dict(kind='assault-resolved',result=result,settlementId=s['settlementId'])
    else:
        require(window is None,6,'/windowBypass');status=s['status'];needs_retreat=status=='resolved' and s['result']['facts']['requiredRetreat']>0;needs_custody=status=='retreat' and any(x['status']=='pending' for x in s['world']['custodyLots'])
        if needs_retreat or needs_custody:
            wk='retreat' if needs_retreat else 'custody';owner=ctx['selection']['defender']['unit']['originalSide'] if needs_retreat else s['world']['custodyLots'][0]['captor']['originalSide']
            opened=make_window(ctx,wk,owner,s['settlementId'],now,inp['clockAvailable'])
            if opened is None:choice_record('refuse-retreat' if wk=='retreat' else 'leave-unguarded','opening-clock-unavailable',None)
            else:s['acceptedHighWater']=max(s['acceptedHighWater'],now);s['window']=opened;s['status']='waiting-'+wk;effect=dict(kind='choice-opened',window=copy.deepcopy(opened))
        else:
            require(now is None and inp['clockAvailable'],3,'/structuralTiming')
            if status=='resolved':choice_record('not-required','not-required',None)
            elif status in ('disposition','losses','retreat','custody'):
                cut={'disposition':'losses','losses':'retreat','retreat':'relationships','custody':'relationships'}[status]
                s['world']=paid_projected(ctx,s,cut);s['status']=cut;effect=dict(kind={'losses':'losses-settled','retreat':'retreat-settled','relationships':'relationships-settled'}[cut],payload=s['world']['settlements'][0][cut])
            elif status=='relationships':
                st=s['world']['settlements'][0];proof_receipts=[st[k]['receiptId'] for k in ('disposition','losses','retreat','custody','relationships') if st[k]]
                require(all(st[k] for k in ('disposition','losses','retreat','relationships')) and not any(x['status']=='pending' for x in s['world']['custodyLots']),7,'/pendingClosure')
                s['status']='round-closed';effect=dict(kind='round-closed',settlementId=s['settlementId'],proofReceipts=proof_receipts)
            elif status=='round-closed':
                edge=combat_edge(ctx['boundary']);require(s['roundClosureReceiptId'] is not None and len(ctx['prior']['stepReceipts'])==5,7,'/caClosure')
                s['status']='closed';s['closed']=True;effect=dict(kind='ca-completed',fromPositionId=edge['combatPositionIds'][5],toPositionId=edge['releasePositionId'],previousStepReceiptId=ctx['prior']['stepReceipts'][-1],roundClosureReceiptId=s['roundClosureReceiptId'])
            else:raise Invalid(6,'/status')
    cycle=ctx['cycle']
    event=dict(contractVersion=1,eventType='moved-paid-'+effect['kind'],author=author,campaignId=cycle['campaignId'],rulesetHash=cycle['rulesetHash'],configurationHash=cycle['admittedPolicyBundleDigest'],roundClockConfigurationHash=prior['roundClockConfigurationHash'],resultClockPolicyId=INVENTORY['resultClockPolicyId'],preparedSourceHash=ctx['base']['preparedSourceHash'],baseHash=prior['baseHash'],cycleId=ctx['boundary']['cycleId'],segmentId=ctx['prior']['segmentId'],roundId=ctx['prior']['roundId'],priorVersion=prior['stateVersion'],stateVersion=prior['stateVersion']+1,priorPrefix=prior['prefix'],input=canonical(inp,'PaidInput'),effect=canonical(effect,'PaidEffect'))
    receipt='cmb.'+paid_digest('receipt',event);event['receiptId']=receipt;data=raw(event,'PaidEvent')
    if effect['kind']=='round-closed':s['roundClosureReceiptId']=receipt
    if effect['kind']=='ca-completed':s['caCompletionReceiptId']=receipt
    s['stateVersion']=event['stateVersion'];s['prefix']=aad.are.selection.st.seq.prefix_event(prior['prefix'],data)
    s['receipts'].append(dict(commandHash=ch,eventHash=sha(data),receiptId=receipt,actor=actor,stateVersion=s['stateVersion']))
    raw(s,'PaidControl');verify_dependencies();verify_delivery();return s,data,receipt

ROUND_SUPPORTED=round_leaves.SUPPORTED
ROUND_UNSUPPORTED=round_leaves.UNSUPPORTED
KINDS=selection_leaves.KINDS

inputs=selection_leaves.inputs
seq=selection_leaves.seq
selection=selection_leaves
candidate=selection_leaves.candidate
def consume(path):
 require(path in INVENTORY['runtimeSourceHashes'],9,path)
 data=(REPOSITORY/path).read_bytes();require(hashlib.sha256(data).hexdigest()==INVENTORY['runtimeSourceHashes'][path],9,path);return data

def source_bytes(packet):
 data=raw(packet,'Source')
 require(len(packet['reactionEntrySourceCanonicalUtf8'])<=LIMITS['parentSourceBytes'],1)
 for field,cap in [('selectionEventCanonicalUtf8',16),('roundEventCanonicalUtf8',16),('paidEventCanonicalUtf8',11)]:
  require(len(packet[field])<=cap and all(len(x)<=LIMITS['eventBytes'] for x in packet[field]),1,field)
 return data
def own_stage(ledger,count,phase):
 require(type(ledger) is list and len(ledger)==count,4,phase)
 out=[]
 for v in ledger:
  require(type(v) is bytes,1,phase)
  tag,inp=parse_envelope(v);require(tag==phase,4,phase);out.append((bytes(v),copy.deepcopy(inp)))
 return out
def prefix_source(packet,which):
 clone=copy.deepcopy(packet)
 if which=='selection':clone['roundEventCanonicalUtf8']=[];clone['paidEventCanonicalUtf8']=[]
 if which=='round':clone['paidEventCanonicalUtf8']=[]
 return sha(source_bytes(clone))
def derive_boundary(entry,entry_proof,parent_packet):
 weather_events=[json.loads(t) for t in parent_packet['prefixEventCanonicalUtf8'] if json.loads(t)['eventType']=='weather-determined']
 require(len(weather_events)==1,4,'weather')
 we=weather_events[0]
 return dict(contractVersion=1,entrySourceHash=entry_proof['sourceHash'],creationBinding=entry['creationBinding'],creationEventHash=entry['creationEventHash'],
 cycle=copy.deepcopy(entry['cycle']),cycleId=entry['cycleId'],firstActingSide=entry['firstActingSide'],priorVersion=entry['stateVersion'],priorPrefix=entry['prefix'],
 reserveCompletionReceiptId=entry['completionReceiptId'],movementCompletionReceiptId=entry['movementEnd']['completionReceiptId'],
 breakdownCompletionReceiptId=entry['breakdownCompletionReceiptId'],breakdownCompletionEventHash=entry_proof['finalBreakdownEventHash'],
 position=copy.deepcopy(entry['sequencePosition']),world=copy.deepcopy(entry['world']),randomState=copy.deepcopy(entry['randomState']),
 weather=dict(gameTurn=1,operationStage=1,attackerKind='normal',defenderKind='normal',weatherReceiptId=we['receiptId'],weatherEventHash=sha(encode(we))),
 breakdownFlow={'kind':'idle'},reactionWindow=None,movementEnd=copy.deepcopy(entry['movementEnd']),candidate=copy.deepcopy(entry_proof['candidate']))
def replay(packet,request,prefix,lifecycle,breakdown,selection_inputs,round_inputs,paid_inputs):
 verify_dependencies();source_bytes(packet)
 # Independent bytes captured before reference dependency/context work.
 sl=own_stage(selection_inputs,len(packet['selectionEventCanonicalUtf8']),'selection')
 rl=own_stage(round_inputs,len(packet['roundEventCanonicalUtf8']),'round')
 pl=own_stage(paid_inputs,len(packet['paidEventCanonicalUtf8']),'paid')
 parent=packet['reactionEntrySourceCanonicalUtf8'].encode('ascii')
 entry,_,parent_proof,_=i01.replay(parent,request,[bytes(x) for x in prefix],[bytes(x) for x in lifecycle],[bytes(x) for x in breakdown])
 ep=json.loads(parent_proof)
 require(entry['stateVersion']==18 and entry['breakdownCompletionReceiptId'] is not None and ep['candidate'] is not None,4,'entry')
 b=derive_boundary(entry,ep,json.loads(parent));raw(b,'MovedBoundary')
 ss=selection_initial(b);rs=None;ps=None;rb=None;ctx=None
 for i,(owned,inp) in enumerate(sl):
  event=packet['selectionEventCanonicalUtf8'][i].encode('ascii');parse(event,'SelectionEvent')
  after,expected,rid=selection_transition(b,ss,inp);require(expected is not None and event==expected,6,'selection');ss=after
 selected=ss['selectionOutcome']=='selected' and ss['stepIndex']==3 and ss['declineReceiptId'] is not None and ss['cancellationReceiptId'] is None
 if selected:
  sp=dict(contractVersion=1,sourceHash=prefix_source(packet,'selection'),boundary=b,control=ss)
  rb=dict(contractVersion=1,selectionProof=sp,clockConfiguration=round_leaves.clock_configuration())
  rs=round_initial(rb)
 else:require(not rl and not pl,4,'dangling-round-paid')
 for i,(owned,inp) in enumerate(rl):
  event=packet['roundEventCanonicalUtf8'][i].encode('ascii');parse(event,'RoundEvent')
  after,expected,rid=round_transition(rb,rs,inp,admission_enabled=True);require(expected is not None and event==expected,6,'round');rs=after
 prepared=rs is not None and rs['status']=='prepared' and rs['stepIndex']==5 and len(rs['stepReceipts'])==5 and all(x['sealedReceiptId'] for x in rs['slots'])
 if prepared:
  base=dict(contractVersion=1,entrySourceHash=ep['sourceHash'],selectionSourceHash=prefix_source(packet,'selection'),
   preparedSourceHash=prefix_source(packet,'round'),preparedWorldHash=sha(raw(rs['world'],'World')),configurationHash=b['cycle']['admittedPolicyBundleDigest'])
  raw(base,'PreparedBase')
  ctx=dict(base=base,prior=rs,boundary=b,selection=ss['selection'],cycle=b['cycle'],rules=aad.normalized_rules(),
   config=paid_leaves.aad.are.selection.env.Context().config,graph=copy.deepcopy(paid_leaves.aad.are.selection.env.world.GRAPH),anchors=copy.deepcopy(paid_leaves.aad.are.selection.env.world.ANCHORS))
  ps=paid_initial(ctx)
 else:require(not pl,4,'dangling-paid')
 for i,(owned,inp) in enumerate(pl):
  event=packet['paidEventCanonicalUtf8'][i].encode('ascii');parse(event,'PaidEvent')
  after,expected,rid=paid_transition(ctx,ps,inp,fresh=False);require(expected is not None and event==expected,6,'paid');ps=after
 return dict(packet=copy.deepcopy(packet),request=request,entry=entry,parentProof=ep,boundary=b,selection=ss,round=rs,paid=ps,roundBase=rb,paidContext=ctx,ledgers=(sl,rl,pl))
def control(frame):
 entry=frame['entry'];ss=frame['selection'];rs=frame['round'];ps=frame['paid']
 phase='paid' if ps is not None else 'round' if rs is not None else 'selection'
 state=ps if ps is not None else rs if rs is not None else ss
 closed=bool(state['closed'])
 if closed:phase='reserve-entry'
 index=6 if closed or ps is not None and ps['closed'] else 5 if ps is not None else rs['stepIndex'] if rs is not None else ss['stepIndex']
 position_id=combat_edge(frame['boundary'])['releasePositionId'] if index==6 else combat_edge(frame['boundary'])['combatPositionIds'][index]
 position=next(p for p in i01.lc.CATALOG['positions'] if p['positionId']==position_id)
 receipts=copy.deepcopy(entry['receipts'])+copy.deepcopy(ss['receipts'])+(copy.deepcopy(rs['receipts']) if rs is not None else [])+(copy.deepcopy(ps['receipts']) if ps is not None else [])
 progress=copy.deepcopy(entry['actualProgressRefs'])
 for text in frame['packet']['paidEventCanonicalUtf8']:
  ev=json.loads(text)
  if ev['effect']['kind']=='attack-committed':progress.append(dict(eventType=ev['eventType'],receiptId=ev['receiptId'],eventHash=sha(text.encode())))
 value=dict(contractVersion=1,phase=phase,stateVersion=state['stateVersion'],prefix=state['prefix'],sequencePosition=position,cycle=copy.deepcopy(entry['cycle']),cycleId=entry['cycleId'],
 world=copy.deepcopy(ps['world'] if ps is not None else entry['world']),randomState=copy.deepcopy(ps['randomState'] if ps is not None else entry['randomState']),
 movementEnd=copy.deepcopy(entry['movementEnd']),actualProgressRefs=progress,candidate=copy.deepcopy(frame['parentProof']['candidate']),receipts=receipts,
 selection=ss,round=rs,paid=ps,closed=closed)
 require(len(receipts)==value['stateVersion']-1,6,'complete-receipts');raw(value,'FamilyControl');return value
def apply(frame,current,admission_enabled=True):
 phase,inp=parse_envelope(bytes(current));prior=frame[phase];require(prior is not None,4,'phase')
 if phase=='selection':
  if inp['command']['kind']=='open-segment':require(admission_enabled,7,'fresh-open')
  state,event,rid=selection_transition(frame['boundary'],prior,inp)
 elif phase=='round':state,event,rid=round_transition(frame['roundBase'],prior,inp,admission_enabled)
 else:
  if inp['command']['kind']=='commit':
   existing=any(r['commandHash']==sha(raw(inp['command'],'PaidCommand')) and r['actor']==inp['actor'] for r in prior['receipts'])
   if not existing:require(admission_enabled,7,'fresh-commit')
  state,event,rid=paid_transition(frame['paidContext'],prior,inp,fresh=admission_enabled)
 packet=copy.deepcopy(frame['packet'])
 if event is not None:packet[phase+'EventCanonicalUtf8'].append(event.decode('ascii'))
 return packet,event,rid
def source_from_parent(parent):
 return dict(contractVersion=1,reactionEntrySourceCanonicalUtf8=parent,selectionEventCanonicalUtf8=[],roundEventCanonicalUtf8=[],paidEventCanonicalUtf8=[])
def command_selection(state,kind,**kw):
 cmd=dict(contractVersion=1,kind=kind,segmentId=state['segmentId'],decisionId=None,fromPositionId=None,expectedPriorVersion=None,choice=None,candidate=None,participant=None)
 cmd.update(kw);return cmd
def trace(parent_case,first='attacker',selection_variant='selected',round_cancel=None):
 request=json.loads(parent_case['requestCanonicalUtf8']);prefix=[x.encode() for x in parent_case['prefixInputCanonicalUtf8']]
 ancestor=[x.encode() for x in parent_case['inputCanonicalUtf8']]
 packet=source_from_parent(parent_case['cuts'][4]['sourceCanonicalUtf8'])
 ledgers=dict(selection=[],round=[],paid=[]);frames=[];commands=[];events=[];cuts=[]
 def rebuild():return replay(packet,request,prefix,ancestor[:3],ancestor[3:],ledgers['selection'],ledgers['round'],ledgers['paid'])
 frame=rebuild()
 def retain():
  value=control(frame);cuts.append(dict(sourceCanonicalUtf8=source_bytes(packet).decode(),controlCanonicalUtf8=raw(value,'FamilyControl').decode()))
 retain()
 def act(phase,cmd,actor='system',now=None,available=True):
  nonlocal packet,frame
  current=envelope(phase,dict(command=cmd,actor=actor,admittedAt=now,clockAvailable=available))
  packet,event,rid=apply(frame,current)
  require(event is not None,6,'trace-noop');ledgers[phase].append(current);commands.append(current.decode());events.append(event.decode())
  frame=rebuild();retain()
 b=frame['boundary'];route=combat_edge(b)['combatPositionIds'];owner=b['cycle']['actingSide'];other=b['candidate']['defender']['unit']['originalSide']
 act('selection',command_selection(frame['selection'],'open-segment',expectedPriorVersion=18),now=None if selection_variant=='opening-unavailable' else 1000,available=selection_variant!='opening-unavailable')
 state=frame['selection']
 if selection_variant=='opening-unavailable':
  act('selection',command_selection(state,'close-empty-selection',expectedPriorVersion=state['stateVersion']))
 elif selection_variant in ('selection-expired','selection-unavailable'):
  act('selection',command_selection(state,'expire-window' if selection_variant=='selection-expired' else 'controller-unavailable',decisionId=state['selectionWindow']['decisionId']),now=31000)
 else:
  selected=selection_variant!='finish'
  act('selection',command_selection(state,'choose-selection',decisionId=state['selectionWindow']['decisionId'],choice='select-close-assault' if selected else 'finish-without-attack',candidate=b['candidate'] if selected else None),actor=owner,now=1001)
 while frame['selection']['stepIndex']<3 or frame['selection']['selectionOutcome']!='selected' and not frame['selection']['closed']:
  state=frame['selection'];step=state['stepIndex'];selected=state['selectionOutcome']=='selected'
  if step==2 and selected and state['declineReceiptId'] is None:
   if state['rbaWindow'] is None:
    if selection_variant=='rba-before-open-unavailable':
     act('selection',command_selection(state,'controller-unavailable',decisionId=state['segmentId']+'.rba'),now=None);continue
    act('selection',command_selection(state,'open-rba',expectedPriorVersion=state['stateVersion'],fromPositionId=route[2]),now=2000);continue
   if selection_variant in ('rba-expired','rba-unavailable'):
    act('selection',command_selection(state,'expire-window' if selection_variant=='rba-expired' else 'controller-unavailable',decisionId=state['rbaWindow']['decisionId']),now=32000);continue
   act('selection',command_selection(state,'decline-rba',decisionId=state['rbaWindow']['decisionId'],participant=b['candidate']['defender']['unit']),actor=other,now=2001);continue
  act('selection',command_selection(state,'complete-step',expectedPriorVersion=state['stateVersion'],fromPositionId=route[step]))
 if not frame['selection']['closed']:
  act('round',round_command(frame['roundBase'],frame['round'],'open-round'),now=3000)
  if round_cancel:
   partial,cause=round_cancel
   if partial!='empty':
    state=frame['round'];slot=next(x for x in state['slots'] if x['role']==partial)
    act('round',round_command(frame['roundBase'],state,'seal-choice',partial),actor=slot['owner'],now=4000)
   state=frame['round']
   if cause in ('deadline','controller'):
    act('round',round_command(frame['roundBase'],state,'expire-round' if cause=='deadline' else 'controller-unavailable'),now=33000 if cause=='deadline' else None)
   else:
    role=next(x['role'] for x in state['slots'] if x['sealedReceiptId'] is None);slot=next(x for x in state['slots'] if x['role']==role)
    act('round',round_command(frame['roundBase'],state,'seal-choice',role),actor=slot['owner'],now=None if cause=='null' else 2999 if cause=='below-floor' else 3500,available=cause!='confidence')
   for _ in range(3):act('round',round_command(frame['roundBase'],frame['round'],'complete-step'))
  else:
   for role,time in [(first,4000),('defender' if first=='attacker' else 'attacker',3500)]:
    state=frame['round'];slot=next(x for x in state['slots'] if x['role']==role)
    act('round',round_command(frame['roundBase'],state,'seal-choice',role),actor=slot['owner'],now=time)
   for _ in range(2):act('round',round_command(frame['roundBase'],frame['round'],'complete-step'))
   for kind in ['commit','resolve']+['advance']*6:
    act('paid',paid_leaves.command(frame['paidContext'],frame['paid'],kind))
 require(control(frame)['closed'],6,'trace-terminal')
 return dict(name=parent_case['name']+'-'+first+'-'+selection_variant+('-'+round_cancel[0]+'-'+round_cancel[1] if round_cancel else ''),
 parentCase=parent_case['name'],requestCanonicalUtf8=parent_case['requestCanonicalUtf8'],
 prefixInputCanonicalUtf8=parent_case['prefixInputCanonicalUtf8'],lifecycleInputCanonicalUtf8=parent_case['inputCanonicalUtf8'][:3],
 breakdownInputCanonicalUtf8=parent_case['inputCanonicalUtf8'][3:],inputCanonicalUtf8=commands,eventCanonicalUtf8=events,cuts=cuts)
def generate(selection='all'):
 parents=json.loads((ROOT/'fixtures/combat-actual-reaction-combat-entry-v1.json').read_bytes())['cases']
 result=[]
 for p in parents:
  for first in ['attacker','defender']:result.append(trace(p,first))
 if selection=='all':
  variants=['opening-unavailable','finish','selection-expired','selection-unavailable','rba-before-open-unavailable','rba-expired','rba-unavailable']
  for side in ['axis','commonwealth']:
   own=[p for p in parents if p['name'].startswith(side+'-')]
   for i,v in enumerate(variants):result.append(trace(own[i%3],selection_variant=v))
   i=0
   for partial in ['empty','attacker','defender']:
    for cause in ['deadline','controller','confidence','null','below-floor']:
     result.append(trace(own[i%3],round_cancel=(partial,cause)));i+=1
 return dict(contract=NAME,contractVersion=1,cases=result)
def measurements(fixture):
 maxima=dict(sourceBytes=0,controlBytes=0,eventBytes=0,inputBytes=0,controlDepth=0)
 def depth(v):return 1+max([depth(x) for x in (v.values() if isinstance(v,dict) else v)] or [0]) if isinstance(v,(dict,list)) else 0
 for case in fixture['cases']:
  for cut in case['cuts']:
   maxima['sourceBytes']=max(maxima['sourceBytes'],len(cut['sourceCanonicalUtf8']));maxima['controlBytes']=max(maxima['controlBytes'],len(cut['controlCanonicalUtf8']))
   maxima['controlDepth']=max(maxima['controlDepth'],depth(json.loads(cut['controlCanonicalUtf8'])))
  for event in case['eventCanonicalUtf8']:maxima['eventBytes']=max(maxima['eventBytes'],len(event))
  for input in case['inputCanonicalUtf8']:maxima['inputBytes']=max(maxima['inputBytes'],len(input))
 return maxima
if __name__=='__main__':
 if '--measure' in sys.argv:
  fixture=generate();report=dict(status='draft reference capacity/fullbranch measurement, not native acceptance',cases=len(fixture['cases']),maxima=measurements(fixture))
  Path('/private/tmp/n1-maximum-capacity-receipt.json').write_text(json.dumps(report,indent=2))
  Path('/private/tmp/n1-draft-fixture.json').write_bytes(encode(fixture)+b'\n')
  print(json.dumps(report))
 else:raise NotImplementedError('Contract freeze/retained fixture verification pending')
