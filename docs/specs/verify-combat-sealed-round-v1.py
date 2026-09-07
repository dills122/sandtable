#!/usr/bin/env python3
"""Bounded C3b authority contract oracle; not production or full Snapshot12."""
import copy
import importlib.util
import json
import sys
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('steps', ROOT/'verify-combat-selection-steps-v1.py')
steps = importlib.util.module_from_spec(spec); spec.loader.exec_module(steps)
encode, sha = steps.encode, steps.sha
INVENTORY = json.loads((ROOT/'combat-sealed-round-v1.schema.json').read_text())
SCHEMA = steps.SCHEMA | {k:[tuple(x.split(':')) for x in v.split()] for k,v in INVENTORY['objects'].items()}
FIXTURE = ROOT/'fixtures/combat-sealed-round-v1.json'


def start():
    f = json.loads(steps.FIXTURE.read_text())
    case = next(c for c in f['cases'] if c['name']=='accepted-decline')
    case['boundary'] = json.loads(f['boundaries'][case['boundaryIndex']]['canonicalUtf8'])
    return dict(contractVersion=1, boundary=case['boundary'], steps=steps.replay(case)), case


class Invalid(ValueError):
    def __init__(self, code, path=''):
        self.code, self.path = f'CMB-RND-{code:03}', path
        super().__init__(self.code+' '+(path or '/'))


def require(ok, code, path=''):
    if not ok: raise Invalid(code, path)


def typed(value, kind, path='', depth=0):
    require(depth <= 32, 1, path)
    if kind.endswith('?'):
        if value is not None: typed(value, kind[:-1], path, depth)
    elif kind == 'RoundEffect':
        require(type(value) is dict and type(value.get('kind')) is str and value['kind'] in INVENTORY['effectTags'], 3, path)
        typed(value, INVENTORY['effectTags'][value['kind']], path, depth)
    elif kind in SCHEMA:
        require(type(value) is dict and set(value)=={k for k,_ in SCHEMA[kind]}, 1, path)
        for k,t in SCHEMA[kind]: typed(value[k], t, path+'/'+k, depth+1)
    elif kind.endswith('[]'):
        require(type(value) is list and len(value)<=512, 1, path)
        for i,x in enumerate(value): typed(x,kind[:-2],f'{path}/{i}',depth+1)
    elif kind == 'role': require(type(value) is str and value in ('attacker','defender'), 2, path)
    else:
        try: steps.typed(value,kind,path,depth)
        except steps.Invalid as e: raise Invalid(int(e.code[-3:]),e.path) from e


def canonical(value, kind):
    if kind.endswith('?'): return None if value is None else canonical(value,kind[:-1])
    if kind == 'RoundEffect': return canonical(value,INVENTORY['effectTags'][value['kind']])
    if kind in SCHEMA: return {k:canonical(value[k],t) for k,t in SCHEMA[kind]}
    if kind.endswith('[]'): return [canonical(x,kind[:-2]) for x in value]
    return value


def parse(raw,kind):
    require(type(raw) is bytes and 0<len(raw)<=1048576,1)
    def pairs(items):
        out={}
        for k,v in items:
            require(k not in out,1);out[k]=v
        return out
    try:
        value=json.loads(raw.decode('utf-8'),object_pairs_hook=pairs,
                        parse_constant=lambda _: (_ for _ in ()).throw(ValueError()))
    except (ValueError,UnicodeError,RecursionError) as e: raise Invalid(1) from e
    typed(value,kind);require(raw==encode(canonical(value,kind)),8)
    return value


def digest(kind,value): return steps.digest(INVENTORY['domains'][kind],encode(value))
def base_hash(base): return 'sha256:'+digest('base',canonical(base,'Base'))
def cycle_id(base):
    b=base['boundary'];return sha(steps.seq.identity(b['cycle'],'Authority',b['firstActingSide']))


def read_base(raw, predecessor):
    b=parse(raw,'Base')
    try:
        boundary=steps.boundary(encode(predecessor['boundary']))
        control=steps.replay(predecessor)
    except steps.Invalid as e: raise Invalid(4,'/predecessor') from e
    require(b==dict(contractVersion=1,boundary=boundary,steps=control),4)
    require(control['selectionOutcome']=='selected' and control['declineReceiptId'] is not None
            and control['stepIndex']==3 and len(control['stepReceipts'])==3 and not control['closed'],6)
    require(control['selection']==steps.candidate(boundary),4)
    return b


def initial(base):
    s=base['steps'];b=base['boundary']
    return dict(contractVersion=1,baseHash=base_hash(base),opportunityId=None,roundId=None,
        stateVersion=s['stateVersion'],prefix=s['prefix'],stepIndex=3,status='unopened',openingReceiptId=None,
        timing=None,slots=[],stepReceipts=copy.deepcopy(s['stepReceipts']),world=copy.deepcopy(b['world']),
        randomState=copy.deepcopy(b['randomState']),attackHistory=[],targetUses=[],commitmentId=None,
        cancellationReceiptId=None,receipts=[],closed=False)


def command(base,state,kind,slot=None):
    slot_value=next((x for x in state['slots'] if x['role']==slot),None)
    return dict(contractVersion=1,kind=kind,segmentId=base['steps']['segmentId'],
        roundId=state['roundId'],slotId=slot_value['slotId'] if slot_value else None,
        expectedPriorVersion=state['stateVersion'] if kind in ('open-round','complete-step','commit-attack') else None,
        allocation=copy.deepcopy(slot_value['allocation']) if slot_value else None)


def trusted(cmd,actor='system',now=None,available=True):
    return dict(command=cmd,actor=actor,admittedAt=now,clockAvailable=available)


def transition(base,prior,inp):
    """Only accepts an internally derived prior; persisted state enters through read_state/replay."""
    typed(inp,'RoundInput');typed(prior,'RoundState')
    cmd=inp['command'];kind=cmd['kind'];actor=inp['actor'];b=base['boundary']
    require(cmd['contractVersion']==1 and kind in ('open-round','seal-choice','expire-round','controller-unavailable','complete-step','commit-attack'),3)
    require(cmd['segmentId']==base['steps']['segmentId'] and prior['baseHash']==base_hash(base),4)
    require(actor in ('axis','commonwealth') if kind=='seal-choice' else actor=='system',4)
    require(cmd['allocation'] is not None and cmd['slotId'] is not None if kind=='seal-choice'
            else cmd['allocation'] is None and cmd['slotId'] is None,3)
    require(cmd['expectedPriorVersion'] is not None if kind in ('open-round','complete-step','commit-attack')
            else cmd['expectedPriorVersion'] is None,3)
    require(cmd['roundId'] is None if kind=='open-round' else cmd['roundId'] is not None,3)
    ch=sha(encode(canonical(cmd,'RoundCommand')))
    duplicate=next((r for r in prior['receipts'] if r['commandHash']==ch),None)
    if duplicate:
        require(duplicate['actor']==actor,4)
        return copy.deepcopy(prior),None,duplicate['receiptId']
    if kind in ('expire-round','controller-unavailable') and (cmd['roundId']!=prior['roundId'] or prior['status']!='collecting'):
        return copy.deepcopy(prior),None,None
    require(not prior['closed'] and prior['status']!='committed',7)
    require(prior['world']==b['world'] and prior['randomState']==b['randomState']
            and not prior['attackHistory'] and not prior['targetUses'],4)
    if kind!='open-round':require(cmd['roundId']==prior['roundId'],4)
    if kind in ('open-round','complete-step','commit-attack'):require(cmd['expectedPriorVersion']==prior['stateVersion'],6)
    require(prior['stateVersion']<2**63-1 and len(prior['receipts'])<16,6)
    s=copy.deepcopy(prior);effect=None;author=actor
    if kind=='open-round':
        require(s['status']=='unopened' and s['stepIndex']==3 and not s['receipts'],6)
        require(inp['clockAvailable'] and inp['admittedAt'] is not None
                and inp['admittedAt']>=base['steps']['rbaWindow']['timing']['highWaterUnixMilliseconds'],5)
        try: timing=steps.inputs.make_timing(steps.env.Context().config,'force-assignment',inp['admittedAt'])
        except steps.inputs.Invalid as e: raise Invalid(5,'/admittedAt') from e
        candidate=base['steps']['selection']
        op=dict(baseHash=s['baseHash'],cycleId=cycle_id(base),positionId=steps.edge(b)['combatPositionIds'][3],
                candidate=candidate,declineReceiptId=base['steps']['declineReceiptId'])
        s['opportunityId']='opp.'+digest('opportunity',op)
        opening=dict(baseHash=s['baseHash'],opportunityId=s['opportunityId'],openingAuthorityVersion=s['stateVersion'],
                     openingHistoryPrefix=s['prefix'],timing=timing)
        s['roundId']='rnd.'+digest('round',opening)
        for role in ('attacker','defender'):
            p=candidate[role]
            s['slots'].append(dict(role=role,owner=p['unit']['originalSide'],
                slotId='slt.'+digest('slot',dict(roundId=s['roundId'],role=role)),
                allocation=dict(kind='full-close-assault',unit=copy.deepcopy(p['unit']),componentId=p['componentIds'][0],committedToe=10),
                sealedReceiptId=None,sealedAt=None))
        s['status']='collecting';s['timing']=timing
        effect=dict(kind='round-opened',baseHash=s['baseHash'],opportunityId=s['opportunityId'],timing=copy.deepcopy(timing),slots=copy.deepcopy(s['slots']))
    elif kind=='seal-choice':
        require(s['status']=='collecting' and s['stepIndex']==3,6)
        slot=next((x for x in s['slots'] if x['slotId']==cmd['slotId']),None)
        require(slot is not None and slot['owner']==actor,4)
        require(slot['sealedReceiptId'] is None,6)
        require(cmd['allocation']==slot['allocation'],4)
        gate=steps.inputs.clock_gate(s['timing'],inp['admittedAt'],inp['clockAvailable'])
        require(gate!='expired',5)
        if gate=='unavailable':
            effect=dict(kind='round-cancelled',cause='clock-unavailable',timing=copy.deepcopy(s['timing']));author='system'
        else:
            s['timing']['highWaterUnixMilliseconds']=inp['admittedAt']
            effect=dict(kind='choice-sealed',slotId=slot['slotId'],allocation=copy.deepcopy(slot['allocation']),
                        timing=copy.deepcopy(s['timing']),prepared=any(x['sealedReceiptId'] is not None for x in s['slots']))
    elif kind in ('expire-round','controller-unavailable'):
        require(s['status']=='collecting',6)
        gate=steps.inputs.clock_gate(s['timing'],inp['admittedAt'],inp['clockAvailable'])
        if kind=='expire-round' and gate=='before-deadline':return copy.deepcopy(prior),None,None
        if inp['admittedAt'] is not None:s['timing']['highWaterUnixMilliseconds']=max(inp['admittedAt'],s['timing']['highWaterUnixMilliseconds'])
        cause='controller-unavailable' if kind=='controller-unavailable' else ('deadline' if gate=='expired' else 'clock-unavailable')
        effect=dict(kind='round-cancelled',cause=cause,timing=copy.deepcopy(s['timing']))
    elif kind=='complete-step':
        require(inp['admittedAt'] is None and inp['clockAvailable'],5)
        require(s['status'] in ('prepared','cancelled') and 3<=s['stepIndex']<=5,6)
        require(s['stepIndex']<5 or s['status']=='cancelled',7)
        positions=steps.edge(b)['combatPositionIds'];i=s['stepIndex']
        proofs=[s['cancellationReceiptId']] if s['status']=='cancelled' else [x['sealedReceiptId'] for x in s['slots']]
        require(all(x is not None for x in proofs),6)
        effect=dict(kind='step-completed',fromPositionId=positions[i],
                    toPositionId=positions[i+1] if i<5 else steps.edge(b)['releasePositionId'],
                    previousStepReceiptId=s['stepReceipts'][-1],proofReceipts=proofs)
    else:
        require(inp['admittedAt'] is None and inp['clockAvailable'],5)
        require(s['status']=='prepared' and s['stepIndex']==5 and len(s['stepReceipts'])==5,6)
        require(all(x['sealedReceiptId'] is not None for x in s['slots']),6)
        allocations=[copy.deepcopy(x['allocation']) for x in s['slots']]
        cid='cmt.'+digest('commitment',dict(roundId=s['roundId'],priorVersion=s['stateVersion'],priorPrefix=s['prefix'],allocations=allocations))
        costs=[]
        for slot,cost in zip(s['slots'],(5,3)):
            element=next(x for x in s['world']['elements'] if x['elementId']==slot['allocation']['unit']['elementId'])
            cp=element['operationalState']['capabilityPointsExpended']['numerator']
            require(element['ammunition']['points']==10 and cp+cost<=10,4)
            costs.append(dict(unit=slot['allocation']['unit'],beforeCp=cp,afterCp=cp+cost,beforeAmmo=10,afterAmmo=0))
        effect=dict(kind='attack-committed',commitmentId=cid,allocations=allocations,costs=costs,preResultRandomState=copy.deepcopy(s['randomState']))
    event=dict(contractVersion=1,eventType=INVENTORY['eventTypes'][effect['kind']],author=author,campaignId=b['cycle']['campaignId'],
        rulesetHash=b['cycle']['rulesetHash'],configurationHash=b['cycle']['admittedPolicyBundleDigest'],cycleId=cycle_id(base),
        segmentId=cmd['segmentId'],roundId=s['roundId'],priorVersion=prior['stateVersion'],stateVersion=prior['stateVersion']+1,
        priorPrefix=prior['prefix'],input=canonical(inp,'RoundInput'),effect=effect)
    rid='cmb.'+digest('receipt',event);event['receiptId']=rid
    typed(event,'RoundEvent');raw=encode(canonical(event,'RoundEvent'));require(len(raw)<=1048576,1)
    tag=effect['kind']
    if tag=='round-opened':s['openingReceiptId']=rid
    elif tag=='choice-sealed':
        slot=next(x for x in s['slots'] if x['slotId']==effect['slotId'])
        slot['sealedReceiptId']=rid;slot['sealedAt']=inp['admittedAt']
        s['status']='prepared' if effect['prepared'] else 'collecting'
    elif tag=='round-cancelled':
        s['status']='cancelled';s['cancellationReceiptId']=rid;s['timing']=copy.deepcopy(effect['timing'])
    elif tag=='step-completed':
        s['stepReceipts'].append(rid);s['stepIndex']+=1;s['closed']=s['stepIndex']==6
    elif tag=='attack-committed':
        s['status']='committed';s['commitmentId']=effect['commitmentId']
        for cost in effect['costs']:
            e=next(x for x in s['world']['elements'] if x['elementId']==cost['unit']['elementId'])
            e['operationalState']['capabilityPointsExpended']['numerator']=cost['afterCp'];e['ammunition']['points']=0
        c=base['steps']['selection']
        s['attackHistory'].append(dict(commitmentId=effect['commitmentId'],cycleId=cycle_id(base),segmentId=cmd['segmentId'],
            attacker=copy.deepcopy(c['attacker']['unit']),defender=copy.deepcopy(c['defender']['unit']),
            targetLocationId=c['targetLocationId'],gameTurn=b['cycle']['gameTurn'],operationStage=b['cycle']['operationStage']))
        s['targetUses'].append(dict(commitmentId=effect['commitmentId'],segmentId=cmd['segmentId'],targetLocationId=c['targetLocationId']))
    s['stateVersion']=event['stateVersion'];s['prefix']=steps.seq.prefix_event(prior['prefix'],raw)
    s['receipts'].append(dict(commandHash=ch,eventHash=sha(raw),receiptId=rid,actor=actor,stateVersion=s['stateVersion']))
    typed(s,'RoundState');require(len(encode(canonical(s,'RoundState')))<=1048576,1)
    return s,raw,rid


def read_event(raw,base,prior,inp):
    parse(raw,'RoundEvent');state,expected,_=transition(base,prior,inp)
    require(expected is not None and expected==raw,6)
    return state


def replay(base,inputs,events,length=None):
    require(type(inputs) is list and type(events) is list and len(inputs)==len(events)<=16,1)
    require(length is None or type(length) is int and 0<=length<=len(events),2)
    s=initial(base)
    for inp,raw in zip(inputs[:length],events[:length]):s=read_event(raw,base,s,inp)
    return s


def read_state(raw,base,inputs,events,length=None):
    parse(raw,'RoundState');s=replay(base,inputs,events,length)
    require(raw==encode(canonical(s,'RoundState')),6)
    return s


def trace_case(base,case):
    state=initial(base);inputs=[];events=[]
    def apply(kind,role=None,now=None):
        nonlocal state
        actor=base['steps']['selection'][role]['unit']['originalSide'] if role else 'system'
        inp=trusted(command(base,state,kind,role),actor,now)
        state,raw,_=transition(base,state,inp);inputs.append(inp);events.append(raw)
    apply('open-round',now=3000)
    for i,role in enumerate(case['seals']):apply('seal-choice',role,4000+i)
    if case['terminal']=='cancelled':apply('expire-round',now=33000)
    apply('complete-step');apply('complete-step')
    apply('commit-attack' if case['terminal']=='committed' else 'complete-step')
    return state,inputs,events


def run_case(base,case): return trace_case(base,case)[0]


def rejected(call,code=None):
    try:call()
    except Invalid as e:
        if code is not None:assert e.code==f'CMB-RND-{code:03}',(e.code,code,e.path)
    else:raise AssertionError('invalid round contract accepted')


def behavioral_checks(base):
    s=initial(base);frozen=encode(s)
    opened,_,_=transition(base,s,trusted(command(base,s,'open-round'),now=3000))
    attacker=next(x for x in opened['slots'] if x['role']=='attacker')
    defender=next(x for x in opened['slots'] if x['role']=='defender')
    a=trusted(command(base,opened,'seal-choice','attacker'),attacker['owner'],4000)
    d=trusted(command(base,opened,'seal-choice','defender'),defender['owner'],4001)
    one,_,_=transition(base,opened,a)
    assert one['slots'][1]==opened['slots'][1] and one['world']==opened['world']
    # Both slots have stable authority references; outward projections remain004.
    bad=copy.deepcopy(d);bad['actor']=a['actor'];rejected(lambda:transition(base,one,bad),4)
    bad=copy.deepcopy(d);bad['command']['allocation']['committedToe']=9;rejected(lambda:transition(base,one,bad),4)
    for key in ('roundId','slotId','segmentId'):
        bad=copy.deepcopy(d);bad['command'][key]='other';rejected(lambda:transition(base,one,bad),4)
    bad=copy.deepcopy(d);bad['command']['expectedPriorVersion']=one['stateVersion'];rejected(lambda:transition(base,one,bad),3)
    for when in (33000,33001):
        late=copy.deepcopy(d);late['admittedAt']=when;rejected(lambda:transition(base,one,late),5)
    for when,available in ((3999,True),(None,True),(4001,False)):
        lost=copy.deepcopy(d);lost.update(admittedAt=when,clockAvailable=available)
        cancelled,raw,_=transition(base,one,lost)
        assert cancelled['status']=='cancelled' and cancelled['world']==base['boundary']['world']
        assert cancelled['slots'][1]['sealedReceiptId'] is None and not cancelled['attackHistory']
        assert json.loads(raw)['author']=='system' and cancelled['timing']['deadlineUnixMilliseconds']==33000
    almost=copy.deepcopy(d);almost['admittedAt']=32999
    prepared,_,_=transition(base,one,almost)
    assert prepared['status']=='prepared'
    for state in (prepared,):
        for kind in ('expire-round','controller-unavailable'):
            after,event,rid=transition(base,state,trusted(command(base,state,kind),now=33000,available=False))
            assert after==state and event is None and rid is None
    old=trusted(command(base,one,'expire-round'),now=33000);old['command']['roundId']='old-round'
    assert transition(base,one,old)==(one,None,None)
    assert transition(base,one,trusted(command(base,one,'expire-round'),now=32999))==(one,None,None)
    cancelled,_,_=transition(base,one,trusted(command(base,one,'controller-unavailable'),now=4001))
    assert cancelled['world']==base['boundary']['world'] and cancelled['slots'][0]==one['slots'][0]
    rejected(lambda:transition(base,one,trusted(command(base,one,'complete-step'))),6)
    rejected(lambda:transition(base,prepared,trusted(command(base,prepared,'commit-attack'))),6)
    for state in (opened,one):
        rejected(lambda:transition(base,state,trusted(command(base,state,'commit-attack'))),6)
    for now,available in ((None,True),(1999,True),(3000,False),(253402300799999,True)):
        rejected(lambda:transition(base,s,trusted(command(base,s,'open-round'),now=now,available=available)),5)
    assert encode(s)==frozen
    # Resource tampering cannot be converted into a legitimate cancellation.
    for change in ('world','randomState'):
        tampered=copy.deepcopy(one)
        if change=='world':tampered['world']['elements'][0]['ammunition']['points']=9
        else:tampered['randomState']['seed']+=1
        rejected(lambda:transition(base,tampered,trusted(command(base,tampered,'controller-unavailable'),now=4001)),4)
    # Checked version overflow publishes nothing.
    overflow=copy.deepcopy(prepared);overflow['stateVersion']=2**63-1
    rejected(lambda:transition(base,overflow,trusted(command(base,overflow,'complete-step'))),6)
    assert overflow['stepIndex']==3


def resource_boundary_base(base,predecessor,attacker_cp,defender_cp):
    case=copy.deepcopy(predecessor);b=case['boundary']
    for role,cp in (('attacker',attacker_cp),('defender',defender_cp)):
        unit=base['steps']['selection'][role]['unit']['elementId']
        next(e for e in b['world']['elements'] if e['elementId']==unit)['operationalState']['capabilityPointsExpended']['numerator']=cp
    current=steps.initial(steps.boundary(encode(b)));new_inputs=[];new_events=[]
    for original in case['inputs']:
        inp=copy.deepcopy(original);cmd=inp['command'];cmd['segmentId']=current['segmentId']
        if cmd['decisionId'] is not None:cmd['decisionId']=current['segmentId']+'.'+cmd['decisionId'].rsplit('.',1)[1]
        if cmd['expectedPriorVersion'] is not None:cmd['expectedPriorVersion']=current['stateVersion']
        if cmd['candidate'] is not None:cmd['candidate']=steps.candidate(b)
        current,raw,_=steps.transition(b,current,inp)
        new_inputs.append(inp);new_events.append({'canonicalUtf8':raw.decode()})
    case['inputs']=new_inputs;case['events']=new_events
    result=dict(contractVersion=1,boundary=b,steps=current)
    return read_base(encode(result),case)


def main():
    raw_base,predecessor=start();base=read_base(encode(raw_base),predecessor)
    fixture=json.loads(FIXTURE.read_text());mutations=raw_checks=cuts=0;finals=[]
    assert fixture['contractVersion']==1 and 'baseGolden' in fixture
    assert all('events' in c and 'stateGolden' in c and 'inputs' in c for c in fixture['cases'])
    if 'baseGolden' in fixture:
        g=fixture['baseGolden'];raw=g['canonicalUtf8'].encode()
        assert raw==encode(base) and len(raw)==g['byteCount'] and sha(raw)==g['sha256']
        assert fixture['predecessorSha256']==sha(steps.FIXTURE.read_bytes())
    for case in fixture['cases']:
        final,inputs,events=trace_case(base,case);finals.append(final)
        assert final['status']==case['terminal'] and final['stepIndex']==case['expectedStepIndex']
        assert len(final['attackHistory'])==len(final['targetUses'])==case['expectedHistory']
        expected_world=copy.deepcopy(base['boundary']['world'])
        for i,role in enumerate(('attacker','defender')):
            unit=base['steps']['selection'][role]['unit']['elementId']
            e=next(x for x in expected_world['elements'] if x['elementId']==unit)
            e['operationalState']['capabilityPointsExpended']['numerator']=case['expectedCp'][i]
            e['ammunition']['points']=case['expectedAmmo'][i]
        assert final['world']==expected_world and final['randomState']==base['boundary']['randomState']
        assert final['closed']==(case['terminal']=='cancelled')
        prior=initial(base)
        if 'events' in case:
            assert case['inputs']==inputs
            assert len(case['events'])==len(events)
        for n,(inp,raw) in enumerate(zip(inputs,events)):
            if 'events' in case:
                g=case['events'][n];assert raw==g['canonicalUtf8'].encode() and len(raw)==g['byteCount'] and sha(raw)==g['sha256']
            state=read_event(raw,base,prior,inp)
            assert read_state(encode(state),base,inputs,events,n+1)==state;cuts+=1
            duplicate,event,rid=transition(base,state,inp)
            assert duplicate==state and event is None and rid==json.loads(raw)['receiptId']
            wrong=copy.deepcopy(inp);wrong['actor']='commonwealth' if inp['actor']!='commonwealth' else 'axis'
            rejected(lambda:transition(base,state,wrong))
            for key,value in (('priorVersion',0),('stateVersion',999),('priorPrefix','sha256:'+'0'*64),
                              ('receiptId','other'),('roundId','other'),('configurationHash','sha256:'+'0'*64)):
                bad=json.loads(raw);bad[key]=value;rejected(lambda:read_event(encode(bad),base,prior,inp));mutations+=1
            bad=json.loads(raw);bad['effect']['extra']=1;rejected(lambda:read_event(encode(bad),base,prior,inp));mutations+=1
            if json.loads(raw)['effect']['kind']=='attack-committed':
                bad=json.loads(raw);bad['effect']['costs'][0]['afterCp']+=1
                rejected(lambda:read_event(encode(bad),base,prior,inp));mutations+=1
            if state['slots']:
                tampered=copy.deepcopy(state);tampered['slots'][0]['allocation']['committedToe']=9
                rejected(lambda:read_state(encode(tampered),base,inputs,events,n+1));mutations+=1
            for invalid in (raw+b'\n',b'\xef\xbb\xbf'+raw,raw[:-1],raw.replace(b'"contractVersion":1',b'"contractVersion":1.0',1),
                            raw.replace(b'"contractVersion":1',b'"contractVersion":1,"contractVersion":1',1),
                            encode(dict(reversed(list(json.loads(raw).items()))))):
                rejected(lambda:read_event(invalid,base,prior,inp));raw_checks+=1
            for key,value in (('prefix','sha256:'+'0'*64),('stepIndex',999),('status','committed'),('baseHash','sha256:'+'0'*64)):
                if state[key]==value:continue
                tampered=copy.deepcopy(state);tampered[key]=value
                rejected(lambda:read_state(encode(tampered),base,inputs,events,n+1));mutations+=1
            prior=state
        assert read_state(encode(initial(base)),base,inputs,events,0)==initial(base)
        if 'stateGolden' in case:
            g=case['stateGolden'];raw=g['canonicalUtf8'].encode()
            assert sha(raw)==g['sha256'] and len(raw)==g['byteCount'] and read_state(raw,base,inputs,events)==final
        for cut in (-1,True,len(events)+1):rejected(lambda:replay(base,inputs,events,cut),2)
        rejected(lambda:replay(base,inputs,list(reversed(events))))
        rejected(lambda:replay(base,inputs,events[:-1]))
        if final['status']=='committed':
            assert final['attackHistory'][0]['attacker']==base['steps']['selection']['attacker']['unit']
            assert final['attackHistory'][0]['defender']==base['steps']['selection']['defender']['unit']
            assert final['targetUses'][0]['targetLocationId']==base['steps']['selection']['targetLocationId']
            rejected(lambda:transition(base,final,trusted(command(base,final,'complete-step'))),7)
            for kind in ('expire-round','controller-unavailable'):
                assert transition(base,final,trusted(command(base,final,kind),now=99999))==(final,None,None)
            changed=copy.deepcopy(inputs[-1]);changed['command']['expectedPriorVersion']=final['stateVersion']
            rejected(lambda:transition(base,final,changed),7)
        else:
            rejected(lambda:transition(base,final,trusted(command(base,final,'complete-step'))),7)
    assert finals[0]['world']==finals[1]['world'] and finals[0]['randomState']==finals[1]['randomState']
    assert finals[0]['commitmentId']!=finals[1]['commitmentId']
    assert [s['allocation'] for s in finals[0]['slots']]==[s['allocation'] for s in finals[1]['slots']]
    behavioral_checks(base)
    for acp,dcp in ((0,7),(5,0),(5,7)):
        variant=resource_boundary_base(base,predecessor,acp,dcp)
        end=run_case(variant,fixture['cases'][0])
        for role,wanted in (('attacker',acp+5),('defender',dcp+3)):
            unit=variant['steps']['selection'][role]['unit']['elementId']
            assert next(e for e in end['world']['elements'] if e['elementId']==unit)['operationalState']['capabilityPointsExpended']['numerator']==wanted
        assert end['randomState']==variant['boundary']['randomState']
    for key,value in (('contractVersion',2),):
        bad=copy.deepcopy(base);bad[key]=value;rejected(lambda:read_base(encode(bad),predecessor),4)
    bad=copy.deepcopy(base);bad['steps']['declineReceiptId']=None;rejected(lambda:read_base(encode(bad),predecessor),4)
    bad=copy.deepcopy(base);bad['boundary']['world']['elements'][0]['ammunition']['points']=9
    rejected(lambda:read_base(encode(bad),predecessor),4)
    for invalid in (b'{"kind":NaN}',b'[]',b'null',b'{',b' '*1048577):rejected(lambda:parse(invalid,'RoundEvent'))
    print(f'PASS: 4 traces; {cuts} replay/state cuts; {mutations} event/state mutations; {raw_checks} raw rejections; '
          'seal orders, retries, deadlines, cancellation, CP ceilings and commitment guards. Isolated contract evidence only.')

if __name__ == '__main__': main()
