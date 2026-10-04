#!/usr/bin/env python3
"""Result2 settled-continuation bridge; synthetic earlier Movement trust."""
import copy
import importlib.util
import json
import sys
from functools import lru_cache
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent

def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    out = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(out)
    return out

res = load('settled_result2', ROOT/'verify-combat-result-settlement-v2.py')
ctl = load('settled_control1', ROOT/'verify-combat-cycle-control-v1.py')
rel, mov, world, steps = ctl.rel, ctl.mov, res.world, res.steps
encode, sha = res.encode, res.sha

INVENTORY = json.loads((ROOT/'combat-settled-continuation-v1.schema.json').read_bytes())
FIXTURE = ROOT/'fixtures/combat-settled-continuation-v1.json'
OBJECTS = {k: [tuple(f.split(':')) for f in v.split()] for k, v in INVENTORY['objects'].items()}
CONTROL_TYPES = {'MovementEndProof', 'ProgressRef', 'ContinuationWitness', 'ReleaseInput', 'ReserveMember', 'AttackHistory', 'TargetUse', 'Authority'}
TRUST = 'synthetic-pre-combat'

class Invalid(ValueError):
    def __init__(self, code):
        self.code = f'CMB-SCT-{code:03}'
        super().__init__(self.code)

def require(ok, code):
    if not ok: raise Invalid(code)

def typed(value, kind, depth=0):
    require(depth <= 32, 1)
    if kind.endswith('?'):
        if value is not None: typed(value, kind[:-1], depth)
    elif kind in OBJECTS:
        require(type(value) is dict and set(value) == {k for k, _ in OBJECTS[kind]}, 1)
        for key, child in OBJECTS[kind]: typed(value[key], child, depth+1)
    elif kind.endswith('[]'):
        require(type(value) is list and len(value) <= 512, 1)
        for item in value: typed(item, kind[:-2], depth+1)
    elif kind == 'utf8':
        require(type(value) is str and 0 < len(value) <= 1048576, 1)
    else:
        try:
            if kind in CONTROL_TYPES: ctl.typed(value, kind, depth)
            else: res.typed(value, kind, depth=depth)
        except ValueError as error: raise Invalid(1) from error

def canonical(value, kind):
    if kind.endswith('?'): return None if value is None else canonical(value, kind[:-1])
    if kind in OBJECTS: return {k: canonical(value[k], t) for k, t in OBJECTS[kind]}
    if kind.endswith('[]'): return [canonical(x, kind[:-2]) for x in value]
    if kind == 'utf8': return value
    return ctl.canonical(value, kind) if kind in CONTROL_TYPES else res.canonical(value, kind)

def raw(value, kind):
    typed(value, kind); data = encode(canonical(value, kind)); require(len(data) <= 1048576, 1)
    return data

def parse(data, kind):
    require(type(data) is bytes and 0 < len(data) <= 1048576, 1)
    def pairs(items):
        out = {}
        for k, v in items: require(k not in out, 1); out[k] = v
        return out
    try: value = json.loads(data.decode('utf8'), object_pairs_hook=pairs,
                            parse_constant=lambda _: (_ for _ in ()).throw(ValueError()))
    except (ValueError, UnicodeError, RecursionError) as error: raise Invalid(1) from error
    require(raw(value, kind) == data, 8)
    return value

def traces():
    # The frozen Result2 fixture is the independent synthetic admission catalogue.
    return json.loads(res.FIXTURE.read_bytes())['traces']

def source_for(name, descriptor='boundary-locations'):
    trace = next((t for t in traces() if t['name'] == name), None)
    require(trace is not None and descriptor in ('boundary-locations', 'distant-original', 'prior-exclusion'), 3)
    b = json.loads(trace['baseCanonicalUtf8'])['boundary']; c = b['cycle']
    locations = ctl.locations(b['world']); excluded = []
    if descriptor == 'distant-original':
        for item in locations: item['locationId'] = world.ANCHORS[item['unit']['originalSide']]
    if descriptor == 'prior-exclusion':
        excluded = [copy.deepcopy(x['unit']) for x in locations if x['unit']['originalSide'] == c['actingSide']]
    proof = dict(scope=rel.scope(c), ordinal=c['ordinal'], completionReceiptId='synthetic.movement-completed',
                 endLocations=locations, excludedBefore=excluded)
    context = res.env.Context(); request = context.request()
    created = res.env.encode(res.env.canonical(res.env.make_created(request, context), 'Created'))
    return dict(contractVersion=1, sourceId=name, trustLabel=TRUST,
                requestCanonicalUtf8=res.env.encode(res.env.canonical(request, 'Request')).decode(),
                createdCanonicalUtf8=created.decode(), baseCanonicalUtf8=trace['baseCanonicalUtf8'],
                predecessorCanonicalUtf8=encode(trace['predecessor']).decode(),
                roundInputsCanonicalUtf8=encode(trace['roundInputs']).decode(),
                roundEventCanonicalUtf8=trace['roundEventCanonicalUtf8'],
                committedCanonicalUtf8=trace['committedCanonicalUtf8'],
                resultInputsCanonicalUtf8=encode(trace['resultInputs']).decode(),
                resultEventCanonicalUtf8=trace['resultEventCanonicalUtf8'], resultStateCanonicalUtf8=trace['stateCanonicalUtf8'],
                movementEnd=dict(trustLabel=TRUST, descriptorId=descriptor,
                                 receiptSource='independently-pinned-synthetic-descriptor', proof=proof))

def replay_source(source):
    typed(source, 'SettledSource')
    require(source['contractVersion'] == 1 and source['trustLabel'] == TRUST and source['movementEnd']['trustLabel'] == TRUST, 3)
    require(len(source['roundEventCanonicalUtf8']) <= 16 and len(source['resultEventCanonicalUtf8']) <= 16, 1)
    try:
        context = res.env.Context()
        request = res.env.read_request(source['requestCanonicalUtf8'].encode(), context)
        res.env.read_created(source['createdCanonicalUtf8'].encode(), request, context)
        predecessor = json.loads(source['predecessorCanonicalUtf8'])
        base = res.rnd.read_base(source['baseCanonicalUtf8'].encode(), predecessor)
        ri = json.loads(source['roundInputsCanonicalUtf8']); re = [e.encode() for e in source['roundEventCanonicalUtf8']]
        committed = res.rnd.read_state(source['committedCanonicalUtf8'].encode(), base, ri, re)
        ctx = dict(base=base, predecessor=predecessor, roundInputs=ri, roundEvents=re, committed=committed)
        inputs = json.loads(source['resultInputsCanonicalUtf8']); events = [e.encode() for e in source['resultEventCanonicalUtf8']]
        result = res.read_state(source['resultStateCanonicalUtf8'].encode(), ctx, inputs, events)
        require(result['closed'] and result['status'] == 'closed' and result['window'] is None
                and result['caCompletionReceiptId'] is not None and result['roundClosureReceiptId'] is not None, 5)
        require(all(all(s[k] is not None for k in ('disposition', 'losses', 'retreat', 'relationships'))
                    for s in result['world']['settlements']), 5)
    except (ValueError, KeyError, TypeError, IndexError, OverflowError, RecursionError) as error: raise Invalid(4) from error
    # Replay does not create trust. Match exact independently retained inputs, source bytes and descriptor.
    admitted = source_for(source['sourceId'], source['movementEnd']['descriptorId'])
    require(raw(source, 'SettledSource') == raw(admitted, 'SettledSource'), 4)
    proof = source['movementEnd']['proof']; c = base['boundary']['cycle']
    ctl.validate_proof(proof, c, [world.unit(e, result['world']['creationBinding']) for e in result['world']['elements']], c['ordinal'])
    return ctx, result

def release_basis(ctx, result):
    b = ctx['base']['boundary']; c = b['cycle']; selected = ctx['base']['steps']['selection']['attacker']['unit']
    own = next(e for e in result['world']['elements'] if e['elementId'] == selected['elementId'])
    require(selected == world.unit(own, result['world']['creationBinding']) and own['reserveStatus'] == 'none', 4)
    member = dict(unit=copy.deepcopy(selected), status='none', baseCpa=10,
                  spentCp=copy.deepcopy(own['operationalState']['capabilityPointsExpended']), history=rel.empty_history(c))
    basis = dict(contractVersion=1, profile='settled-empty-release', cycle=copy.deepcopy(c), firstActingSide=b['firstActingSide'],
                 positionId=ctl.edge(c)['releasePositionId'], priorVersion=result['stateVersion'], priorPrefix=result['prefix'],
                 combatCompletionReceiptId=result['caCompletionReceiptId'], retainedWorldHash=sha(res.raw(result['world'], 'World')),
                 randomState=copy.deepcopy(result['randomState']), acceptedHighWater=None, members=[member],
                 attackHistory=copy.deepcopy(ctx['committed']['attackHistory']))
    rel.validate_base(basis)
    return basis

def packet_for(name, descriptor='boundary-locations'):
    source = source_for(name, descriptor); ctx, result = replay_source(source); basis = release_basis(ctx, result)
    state = rel.initial(basis); inputs = []; events = []
    for kind in ('open', 'complete'):
        inp = rel.trusted(rel.command(state, kind), 'system', None)
        state, event, _ = rel.transition(basis, state, inp); inputs.append(inp); events.append(event.decode())
    return dict(contractVersion=1, source=source, releaseBaseCanonicalUtf8=rel.raw(basis, 'ReleaseBase').decode(),
                releaseInputs=inputs, releaseEventCanonicalUtf8=events)

def commitment_progress(ctx):
    committed = ctx['committed']; b = ctx['base']['boundary']; c = b['cycle']; selected = ctx['base']['steps']['selection']['attacker']['unit']
    matches = [(json.loads(data), data) for data in ctx['roundEvents'] if json.loads(data)['eventType'] == 'combat-attack-committed']
    require(len(matches) == 1, 4); event, data = matches[0]
    require(event['effect']['kind'] == 'attack-committed' and event['effect']['commitmentId'] == committed['commitmentId'], 4)
    history = [h for h in committed['attackHistory'] if h['commitmentId'] == committed['commitmentId'] and h['attacker'] == selected
               and (h['gameTurn'], h['operationStage']) == (c['gameTurn'], c['operationStage'])]
    require(len(history) == 1 and selected['originalSide'] == c['actingSide'] and event['receiptId'] != committed['commitmentId'], 4)
    require(any(r['receiptId'] == event['receiptId'] and r['eventHash'] == sha(data) for r in committed['receipts']), 4)
    return [dict(eventType=event['eventType'], receiptId=event['receiptId'], eventHash=sha(data))]

@lru_cache(maxsize=128)
def _bridge(packet_data):
    """Return owned immutable canonical bytes. Only an admitted complete source may yield proof."""
    packet = parse(packet_data, 'SettledPacket'); source = packet['source']; require(packet['contractVersion'] == 1, 3)
    ctx, result = replay_source(source); basis = release_basis(ctx, result)
    require(packet['releaseBaseCanonicalUtf8'].encode() == rel.raw(basis, 'ReleaseBase'), 4)
    inputs = packet['releaseInputs']; events = packet['releaseEventCanonicalUtf8']; require(len(inputs) == len(events) == 2, 5)
    state = rel.initial(basis)
    try:
        for kind, inp, event in zip(('open', 'complete'), inputs, events):
            cmd = inp['command']
            require(inp['actor'] == 'system' and inp['admittedAt'] is None and inp['clockAvailable'] is True
                    and cmd['kind'] == kind and all(cmd[k] is None for k in ('decisionId', 'unit', 'choice')), 5)
            state = rel.read_event(event.encode(), basis, state, inp)
    except ValueError as error: raise Invalid(5) from error
    require(state['status'] == 'completed' and not state['pending'] and state['completionReceiptId'] is not None, 5)
    require(state['members'] == basis['members'] and state['attackHistory'] == ctx['committed']['attackHistory']
            and state['randomState'] == result['randomState'] and state['retainedWorldHash'] == basis['retainedWorldHash'], 4)
    require(rel.project_world(result['world'], basis, state) == result['world'], 4)
    progress = commitment_progress(ctx)
    assessment_base = dict(contractVersion=1, profile='settled-control', releaseBase=basis, releaseInputs=inputs,
                           releaseEvents=[json.loads(e) for e in events], world=result['world'],
                           movementEnd=source['movementEnd']['proof'], progress=progress, targetUses=ctx['committed']['targetUses'])
    # Supported Result2 catalogue: all 32 share the pure exhausted-ammo infantry capability.
    # Fail unsupported capability explicitly; never translate an assessor error into no legal moves.
    try: assessment = ctl.assess(assessment_base)
    except ctl.Invalid as error: raise Invalid(3) from error
    identity = dict(sourceId=source['sourceId'], sourceHash=sha(raw(source, 'SettledSource')),
                    requestHash=sha(source['requestCanonicalUtf8'].encode()), createdHash=sha(source['createdCanonicalUtf8'].encode()),
                    boundaryHash=sha(encode(ctx['base']['boundary'])), selectionHash=sha(source['predecessorCanonicalUtf8'].encode()),
                    baseHash=sha(source['baseCanonicalUtf8'].encode()), committedHash=sha(source['committedCanonicalUtf8'].encode()),
                    resultHash=sha(source['resultStateCanonicalUtf8'].encode()), releaseBaseHash=sha(rel.raw(basis, 'ReleaseBase')),
                    releaseStateHash=sha(rel.raw(state, 'ReleaseState')), movementDescriptorHash=sha(raw(source['movementEnd'], 'SyntheticMovementDescriptor')),
                    roundClockConfigurationHash=ctx['committed']['clockConfigurationHash'], resultClockPolicyId=result['resultClockPolicyId'])
    proof = dict(contractVersion=1, profile='result2-settled-continuation', trustLabel=TRUST, sourceIdentity=identity,
                 cycle=copy.deepcopy(basis['cycle']), stateVersion=state['stateVersion'], prefix=state['prefix'], positionId=basis['positionId'],
                 releaseCompletionReceiptId=state['completionReceiptId'], movementEnd=copy.deepcopy(source['movementEnd']),
                 world=copy.deepcopy(result['world']), randomState=copy.deepcopy(result['randomState']),
                 attackHistory=copy.deepcopy(state['attackHistory']), targetUses=copy.deepcopy(ctx['committed']['targetUses']),
                 members=copy.deepcopy(state['members']), progress=progress, witnessOutcome='supported', unsupportedReason=None,
                 witnesses=assessment['witnesses'])
    return raw(proof, 'SettledContinuationProof')

def bridge(packet_data):
    require(type(packet_data) is bytes, 1)
    return _bridge(packet_data)

def read_proof(data, packet_data):
    parse(data, 'SettledContinuationProof'); expected = bridge(packet_data); require(data == expected, 6)
    return data

def proof_id(data):
    parse(data, 'SettledContinuationProof')
    return 'sct.'+steps.digest(INVENTORY['domains']['proof'], data)

def test_engaged_and_contact_literals():
    for side in ('axis', 'commonwealth'):
        for case, after, breakoff, dp in (('zero-engaged',11,4,1), ('ordinary',9,2,0)):
            source = dict(name=case+'.'+side+'.attacker')
            proof = json.loads(bridge(raw(packet_for(source['name']), 'SettledPacket')))
            assert any(w['terrainCost']==2 and w['breakOffCost']==breakoff and
                       w['afterCp']==after and w['excessCpDp']==dp for w in proof['witnesses']), source['name']

def test_commitment_receipt_hash():
    source = json.loads(res.FIXTURE.read_bytes())['traces'][0]
    event = next(e for e in source['roundEventCanonicalUtf8'] if json.loads(e)['eventType']=='combat-attack-committed')
    proof = json.loads(bridge(raw(packet_for(source['name']), 'SettledPacket')))
    assert proof['progress'] == [dict(eventType='combat-attack-committed',receiptId=json.loads(event)['receiptId'],eventHash=sha(event.encode()))]
    assert proof['progress'][0]['receiptId'] != json.loads(source['committedCanonicalUtf8'])['commitmentId']


# Independently calculated literals, before regression hashes. Four owner/seal contexts per row.
LITERALS = {
    'ordinary': (1,9,2,0,0,0,0),
    'zero-retreat': (2,7,0,0,0,0,0),
    'refusal-loss-dp': (1,9,2,0,0,0,0),
    'zero-engaged': (1,11,4,1,0,0,0),
    'defender-capture-guard': (1,9,2,0,1,0,1),
    'defender-capture-escape': (1,9,2,0,0,1,1),
    'attacker-capture-guard-cp-limit': (2,12,0,2,1,0,1),
    'attacker-capture-escape': (2,7,0,0,0,1,1),
}

def rejected(call):
    try: call()
    except Invalid: return
    raise AssertionError('invalid settled source/proof accepted')

def test_catalogue_preservation():
    count = 0
    for trace in traces():
        packet = packet_for(trace['name']); packet_data = raw(packet, 'SettledPacket'); before = copy.deepcopy(packet)
        data = bridge(packet_data); proof = json.loads(data); result = json.loads(trace['stateCanonicalUtf8']); w = proof['world']
        assert packet == before and type(data) is bytes
        assert read_proof(data, packet_data) == data and parse(data, 'SettledContinuationProof') == proof
        assert proof['trustLabel'] == TRUST and proof['witnessOutcome'] == 'supported' and proof['unsupportedReason'] is None
        assert w == result['world'] and proof['randomState'] == result['randomState']
        committed = json.loads(trace['committedCanonicalUtf8']); c = committed['base']['boundary']['cycle'] if 'base' in committed else json.loads(trace['baseCanonicalUtf8'])['boundary']['cycle']
        assert proof['cycle'] == c and proof['stateVersion'] == result['stateVersion']+2
        assert proof['attackHistory'] == committed['attackHistory'] and proof['targetUses'] == committed['targetUses']
        expected = LITERALS[trace['name'].rsplit('.',2)[0]]
        n, after, breakoff, dp, guards, entitlements, duties = expected
        assert len(proof['witnesses']) == n, (trace['name'], proof['witnesses'])
        assert all((x['afterCp'],x['breakOffCost'],x['excessCpDp'],x['terrainCost'],x['usesReleaseException']) == (after,breakoff,dp,2,False) for x in proof['witnesses'])
        assert [len(w[k]) for k in ('guards','replacementEntitlements','futureObligations')] == [guards,entitlements,duties]
        for obligation in w['futureObligations']:
            assert obligation['status'] == 'retained-unimplemented' and obligation['earnedScope'] == dict(gameTurn=1,operationStage=1)
            if guards: assert obligation['kind'] == 'guard-priority-upkeep' and obligation['eligibleScope'] is None
            else: assert obligation['kind'] == 'replacement-training-gate' and obligation['eligibleScope'] == dict(gameTurn=5,operationStage=1)
        assert len(proof['progress']) == 1
        assert all(x['eventType'] != 'reserve-release-completed' for x in proof['progress'])
        assert proof['releaseCompletionReceiptId'] == json.loads(packet['releaseEventCanonicalUtf8'][-1])['receiptId']
        assert proof['positionId'] == ctl.edge(c)['releasePositionId']
        assert sorted(x['destinationLocationId'] for x in proof['witnesses']) == [x['destinationLocationId'] for x in proof['witnesses']]
        # Caller projections cannot mutate the owned/cache value.
        proof['world']['elements'].clear(); proof['progress'].clear()
        assert bridge(packet_data) == data
        count += 1
    return count

def test_original_movement_evidence():
    for side in ('axis','commonwealth'):
        for name in ('ordinary','zero-retreat'):
            context = name+'.'+side+'.attacker'
            default = json.loads(bridge(raw(packet_for(context), 'SettledPacket')))
            assert default['witnesses']
            for descriptor in ('distant-original','prior-exclusion'):
                packet = packet_for(context, descriptor); proof = json.loads(bridge(raw(packet,'SettledPacket')))
                assert proof['witnesses'] == [] and proof['witnessOutcome'] == 'supported' and len(proof['progress']) == 1
                assert proof['world'] == default['world'] and proof['randomState'] == default['randomState']
                assert ctl.excluded(packet['source']['movementEnd']['proof'], side)
            if name == 'zero-retreat':
                old = packet_for(context)['source']['movementEnd']['proof']['endLocations']
                assert old != ctl.locations(default['world'])  # Retreat changed current locations, never the original descriptor.
    return 8

def test_independent_arithmetic_probes():
    # Isolated rule probes: no admission, event, World, proof or reachable-history claim.
    assert mov.cost(10,9,['engaged'],None,2) == (15,5)
    mov.rejected(lambda: mov.cost(10,10,['engaged'],None,2),5)
    mov.rejected(lambda: mov.cost(10,2**63-1,['engaged'],None,2),7)
    assert mov.cost(10,11,[],None,2) == (13,2)  # incremental DP, not repeat charge for old overspend
    mov.rejected(lambda: mov.cost(10,5,['engaged'],'I',2),5)
    mov.rejected(lambda: mov.cost(10,5,[],'II',2),5)
    return 6

def replace_json_string(obj, field, change):
    value = json.loads(obj[field]); change(value); obj[field] = encode(value).decode()

def resign(data, oracle, kind):
    obj = json.loads(data); obj.pop('receiptId'); obj['receiptId'] = 'cmb.'+oracle.digest('receipt',obj)
    return oracle.raw(obj,kind).decode()

def test_source_rejections():
    packet = packet_for('zero-engaged.axis.attacker'); mutations = []
    def add(change):
        bad = copy.deepcopy(packet); change(bad); mutations.append(bad)
    add(lambda p: p['source'].update(sourceId='zero-engaged.commonwealth.attacker'))
    add(lambda p: p['source'].update(trustLabel='creation-rooted'))
    add(lambda p: p['source']['movementEnd'].update(trustLabel='creation-rooted'))
    for field in ('completionReceiptId','ordinal','scope','endLocations','excludedBefore'):
        def change(p, field=field):
            proof = p['source']['movementEnd']['proof']
            if field == 'completionReceiptId': proof[field] = json.loads(p['source']['committedCanonicalUtf8'])['commitmentId']
            elif field == 'ordinal': proof[field] += 1
            elif field == 'scope': proof[field]['actingSide'] = 'commonwealth'
            elif field == 'endLocations': proof[field].pop()
            else: proof[field] = [proof['endLocations'][0]['unit']]
        add(change)
    add(lambda p: p['source']['movementEnd'].update(receiptSource='post-retreat-world'))
    for field in ('roundEventCanonicalUtf8','resultEventCanonicalUtf8'):
        add(lambda p, f=field: p['source'][f].pop())
        add(lambda p, f=field: p['source'][f].reverse())
    for field in ('roundInputsCanonicalUtf8','resultInputsCanonicalUtf8'):
        for key, value in (('actor','commonwealth'),('admittedAt',9999),('clockAvailable',False)):
            add(lambda p,f=field,k=key,v=value: replace_json_string(p['source'],f,lambda x: x[0].update({k:v})))
    add(lambda p: replace_json_string(p['source'],'requestCanonicalUtf8',lambda x:x.update(campaignId='foreign-campaign')))
    add(lambda p: replace_json_string(p['source'],'createdCanonicalUtf8',lambda x:x.update(stateVersion=2)))
    add(lambda p: replace_json_string(p['source'],'predecessorCanonicalUtf8',lambda x:x['inputs'].pop()))
    add(lambda p: replace_json_string(p['source'],'baseCanonicalUtf8',lambda x:x['clockConfiguration'].update(clockPolicyId='wrong-clock')))
    add(lambda p: replace_json_string(p['source'],'committedCanonicalUtf8',lambda x:x.update(clockConfigurationHash='sha256:'+'0'*64)))
    add(lambda p: replace_json_string(p['source'],'resultStateCanonicalUtf8',lambda x:x['world']['elements'][0]['operationalState'].update(cohesionLevel=123)))
    # Re-signed canonical forgeries are still rejected by independent semantic replay.
    def forged_round(p):
        event = json.loads(p['source']['roundEventCanonicalUtf8'][-1]); event['effect']['commitmentId'] = 'fake.commitment'
        p['source']['roundEventCanonicalUtf8'][-1] = resign(encode(event),res.rnd,'RoundEvent')
    add(forged_round)
    def forged_result(p):
        event=json.loads(p['source']['resultEventCanonicalUtf8'][0]);event['effect']['result']['draws'][0]['die']=6
        p['source']['resultEventCanonicalUtf8'][0]=resign(encode(event),res,'ResultEvent')
    add(forged_result)
    # Private seal, cancel, timer and retain-only cannot replace attack commitment progress.
    for substitute in ('combat-choice-sealed','combat-selection-cancelled','combat-window-expired','reserve-unit-disposition-recorded'):
        def change(p, substitute=substitute):
            event=json.loads(p['source']['roundEventCanonicalUtf8'][-1]);event['eventType']=substitute
            p['source']['roundEventCanonicalUtf8'][-1]=resign(encode(event),res.rnd,'RoundEvent')
        add(change)
    for bad in mutations: rejected(lambda bad=bad: bridge(raw(bad,'SettledPacket')))
    return len(mutations)

def test_valid_resigned_clock_variant_requires_admission():
    packet=packet_for('zero-retreat.axis.attacker');ctx,_=replay_source(packet['source'])
    state=res.initial(ctx);inputs=[];events=[]
    for old in json.loads(packet['source']['resultInputsCanonicalUtf8']):
        now=None if old['admittedAt'] is None else old['admittedAt']+7
        inp=res.trusted(res.command(ctx,state,old['command']['kind'],old['command']['choice']),old['actor'],now,old['clockAvailable'])
        state,event,_=res.transition(ctx,state,inp);inputs.append(inp);events.append(event)
    # This variant is semantically replayable; self-consistency still supplies no catalogue trust.
    res.read_state(res.raw(state,'ResultState'),ctx,inputs,events)
    packet['source']['resultInputsCanonicalUtf8']=encode(inputs).decode()
    packet['source']['resultEventCanonicalUtf8']=[e.decode() for e in events]
    packet['source']['resultStateCanonicalUtf8']=res.raw(state,'ResultState').decode()
    rejected(lambda:bridge(raw(packet,'SettledPacket')))
    return 1

def test_release_and_proof_rejections():
    packet = packet_for('ordinary.axis.attacker'); original = raw(packet,'SettledPacket'); proof = bridge(original); mutations = []
    def add(change):
        bad=copy.deepcopy(packet);change(bad);mutations.append(bad)
    add(lambda p:p['releaseEventCanonicalUtf8'].pop())
    add(lambda p:p['releaseEventCanonicalUtf8'].reverse())
    add(lambda p:p['releaseInputs'].pop())
    for key,value in (('actor','axis'),('admittedAt',10000),('clockAvailable',False)):
        add(lambda p,k=key,v=value:p['releaseInputs'][0].update({k:v}))
    add(lambda p:p['releaseInputs'][0]['command'].update(kind='expire'))
    add(lambda p:replace_json_string(p,'releaseBaseCanonicalUtf8',lambda x:x['members'].clear()))
    add(lambda p:replace_json_string(p,'releaseBaseCanonicalUtf8',lambda x:x.update(acceptedHighWater=10000)))
    add(lambda p:replace_json_string(p,'releaseBaseCanonicalUtf8',lambda x:x['cycle'].update(ordinal=2)))
    add(lambda p:replace_json_string(p,'releaseBaseCanonicalUtf8',lambda x:x.update(priorVersion=2**63-1)))
    add(lambda p:replace_json_string(p,'releaseBaseCanonicalUtf8',lambda x:x['members'][0]['unit'].update(elementId='foreign-unit')))
    def signed_release(p):
        event=json.loads(p['releaseEventCanonicalUtf8'][0]);event['effect']['openingClockFailure']=True
        event.pop('receiptId');event['receiptId']='rr.'+rel.digest('receipt',event)
        p['releaseEventCanonicalUtf8'][0]=rel.raw(event,'ReleaseEvent').decode()
    add(signed_release)
    for bad in mutations:rejected(lambda bad=bad:bridge(raw(bad,'SettledPacket')))
    obj=json.loads(proof); commitment=json.loads(packet['source']['committedCanonicalUtf8'])['commitmentId']
    changes=[lambda x:x['progress'][0].update(receiptId=commitment),lambda x:x['progress'][0].update(eventHash='sha256:'+'0'*64),
             lambda x:x['progress'].clear(),lambda x:x['progress'].append(dict(eventType='reserve-release-completed',receiptId=x['releaseCompletionReceiptId'],eventHash=sha(packet['releaseEventCanonicalUtf8'][-1].encode()))),
             lambda x:x['members'].clear(),lambda x:x.update(stateVersion=x['stateVersion']+1),lambda x:x['world']['futureObligations'].append({}),
             lambda x:x['witnesses'][0].update(afterCp=10),lambda x:x['sourceIdentity'].update(resultClockPolicyId='historical')]
    for change in changes:
        bad=copy.deepcopy(obj);change(bad);rejected(lambda bad=bad:read_proof(raw(bad,'SettledContinuationProof'),original))
    return len(mutations)+len(changes)

def test_bounds_and_canonical():
    packet=raw(packet_for('ordinary.axis.attacker'),'SettledPacket');proof=bridge(packet)
    for data in (b'',b' '*1048577,packet+b' ',b'\xff',packet.replace(b'"contractVersion":1,',b'"contractVersion":1,"contractVersion":1,',1),b'['*40+b'0'+b']'*40):
        rejected(lambda data=data:bridge(data))
    bad=json.loads(packet);bad['releaseInputs']*=257;rejected(lambda:bridge(raw(bad,'SettledPacket')))
    for field,value in (('contractVersion',True),('stateVersion',2**63),('witnessOutcome','unsupported')):
        bad=json.loads(proof);bad[field]=value;rejected(lambda bad=bad:read_proof(raw(bad,'SettledContinuationProof'),packet))
    rejected(lambda:typed(json.loads(proof),'SettledContinuationProof',33))
    return 11

SOURCE_PATHS = tuple(dict.fromkeys(res.SOURCE_FILES + (
    'verify-combat-result-settlement-v2.py','fixtures/combat-result-settlement-v2.json',
    'combat-reserve-release-v1.schema.json','verify-combat-reserve-release-v1.py','fixtures/combat-reserve-release-v1.json',
    'combat-ordinary-movement-v1.schema.json','verify-combat-ordinary-movement-v1.py','fixtures/combat-ordinary-movement-v1.json',
    'combat-cycle-control-v1.schema.json','verify-combat-cycle-control-v1.py','fixtures/combat-cycle-control-v1.json',
    'combat-authority-envelope-v1.schema.json','verify-combat-authority-envelope-v1.py','fixtures/combat-authority-envelope-v1.json',
    'combat-selection-steps-v1.schema.json','verify-combat-selection-steps-v1.py','fixtures/combat-selection-steps-v1.json',
    'combat-settled-continuation-v1.schema.json',
)))

def source_pins(): return {p:sha((ROOT/p).read_bytes()) for p in SOURCE_PATHS}

def generated_fixture():
    rows=[]
    for trace in traces():
        packet=packet_for(trace['name']);data=raw(packet,'SettledPacket');proof=bridge(data)
        rows.append(dict(sourceId=trace['name'],packetCanonicalUtf8=data.decode(),packetHash=sha(data),
                         proofCanonicalUtf8=proof.decode(),proofHash=sha(proof),proofId=proof_id(proof),
                         witnessOutcome='supported',literal=LITERALS[trace['name'].rsplit('.',2)[0]]))
    # Extra original-distance/exclusion probes remain explicitly synthetic catalogue descriptors.
    descriptors=[]
    for side in ('axis','commonwealth'):
        for descriptor in ('distant-original','prior-exclusion'):
            data=raw(packet_for('zero-retreat.'+side+'.attacker',descriptor),'SettledPacket');proof=bridge(data)
            descriptors.append(dict(packetCanonicalUtf8=data.decode(),proofCanonicalUtf8=proof.decode(),proofHash=sha(proof)))
    return dict(contract='combat-settled-continuation-v1',trustLabel=TRUST,sourcePins=source_pins(),
                compatibility='Result1 control retained unchanged; native Round2/Result2 suffix and untimed empty Release1; no creation-rooted settled admission',
                arithmeticProbes=dict(evidenceClass='independent-rule-probes-not-admitted-history',spent9EngagedClear2=[15,5],spent10EngagedClear2='rejected-ceiling16'),
                traces=rows,movementDescriptorProbes=descriptors)

def test_fixture():
    expected=(json.dumps(generated_fixture(),indent=2)+'\n').encode()
    assert FIXTURE.read_bytes()==expected, 'new frozen fixture drift'
    return 32

def semantic_tests():
    tests=(test_engaged_and_contact_literals,test_commitment_receipt_hash,test_catalogue_preservation,
           test_original_movement_evidence,test_independent_arithmetic_probes,test_source_rejections,
           test_valid_resigned_clock_variant_requires_admission,test_release_and_proof_rejections,test_bounds_and_canonical)
    results={}
    for test in tests:results[test.__name__]=test()
    return results

def main():
    results=semantic_tests();test_fixture()
    print('PASS: settled-continuation: '+str(len(results))+' semantic groups; '+str(results))
    print('PASS: 32 immutable canonical proofs; 4 retained synthetic descriptor vectors; '+str(len(SOURCE_PATHS))+' source pins')

if __name__ == '__main__': main()
