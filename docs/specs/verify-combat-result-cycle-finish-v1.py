#!/usr/bin/env python3
"""Native Result2 to cycle finish; explicit synthetic-source authentication."""
import copy
import hashlib
import importlib.util
import json
import sys
from functools import lru_cache
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent

def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, ROOT / filename)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module

r2 = load('bridge_result2', 'verify-combat-result-settlement-v2.py')
cyc = load('bridge_cycle', 'verify-combat-cycle-control-v1.py')
rel = cyc.rel
FIXTURE = ROOT / 'fixtures/combat-result-cycle-finish-v1.json'
INVENTORY = json.loads((ROOT / 'combat-result-cycle-finish-v1.schema.json').read_bytes())
SCHEMA = {name: [tuple(field.split(':')) for field in fields.split()]
          for name, fields in INVENTORY['objects'].items()}
POLICY = 'sandtable.combat.native-result2-cycle-finish.v1'
CERTIFICATE = 'sandtable.combat.synthetic-pre-retreat-movement.v1'
encode, sha = r2.encode, r2.sha
SOURCE_PINS = {
    'combat-result-settlement-v2.schema.json': '719634c4720b4a92669b2e83e60df58cd32d0e556ec9ef66c546aed1f9c7e9d6',
    'verify-combat-result-settlement-v2.py': 'a6a781976f26b78a9dc098ebfbb3b516284aa1d5873744d96d8fa23aa50d83a2',
    'fixtures/combat-result-settlement-v2.json': '6a1f6cda74424680669539d73583fa3ba5affc612380b3a3efe2e8986a09804a',
    'combat-sealed-round-v2.schema.json': '365283c1c51b9e155aa1651c253759b271bbda75f613f52f511ceeb0dec12c7c',
    'verify-combat-sealed-round-v2.py': 'd1091b5d1d6fb1d9ac88da45313c88fcbc922f763e62d01af44311abc8656bf9',
    'fixtures/combat-sealed-round-v2.json': '8200354f49bef2fd4a976dd9c24e4e485f8c06ad903d8c3deb06d5a9db509a95',
    'combat-reserve-release-v1.schema.json': '10ba1ca46e05fbed52c6eef5174c2ac84c4282df0957c44eab6e86079342bc32',
    'verify-combat-reserve-release-v1.py': '105ef18d9db6364ce42f9831afd3c33010fccc71b0ae60af78cec7c185da892b',
    'fixtures/combat-reserve-release-v1.json': '70ed683c21f95a511c06362765dbe0971e0bb024bfcee74be70865be70601af9',
    'combat-cycle-control-v1.schema.json': '8c517db6daa734d415bd1d474280c2cf6e65013490c4a2568795d50e6112f7da',
    'verify-combat-cycle-control-v1.py': '9039f9e1067b2cdd2e0a57ae16e711350b4bbdceb500d22bbbd715f39e8ccc9f',
    'fixtures/combat-cycle-control-v1.json': 'a8f81c538c0a70c28dd7e97638ec63c915558ec16cc9be6f6a102a12159b5bb6',
    'combat-ordinary-movement-v1.schema.json': '039d5f3cdb908dc761540b4809bfb9c6e77c9ab6efd8c965e69955fbbcf0728a',
    'verify-combat-ordinary-movement-v1.py': 'cc75e52a6d160d4b0c6c880dc43bc65742068f83ef85cc7fb4babbf8756e0dff',
}

class Invalid(ValueError):
    def __init__(self, code):
        self.code = f'CMB-RCF-{code:03}'
        super().__init__(self.code)

def require(ok, code):
    if not ok:
        raise Invalid(code)

def typed(value, kind, depth=0):
    require(depth <= 32, 1)
    if kind.endswith('?'):
        if value is not None:
            typed(value, kind[:-1], depth)
    elif kind in SCHEMA:
        require(type(value) is dict and set(value) == {k for k, _ in SCHEMA[kind]}, 1)
        for key, item_type in SCHEMA[kind]:
            typed(value[key], item_type, depth + 1)
    elif kind.endswith('[]'):
        require(type(value) is list and len(value) <= 512, 1)
        for item in value:
            typed(item, kind[:-2], depth + 1)
    elif kind == 'native':
        require(type(value) is str and 0 < len(value.encode('utf8')) <= 1048576, 1)
    else:
        try:
            r2.typed(value, kind, depth=depth)
        except r2.Invalid as error:
            raise Invalid(int(error.code[-3:])) from error

def canonical(value, kind):
    if kind.endswith('?'):
        return None if value is None else canonical(value, kind[:-1])
    if kind in SCHEMA:
        return {key: canonical(value[key], item_type) for key, item_type in SCHEMA[kind]}
    if kind.endswith('[]'):
        return [canonical(item, kind[:-2]) for item in value]
    return value

def raw(value, kind):
    typed(value, kind)
    data = encode(canonical(value, kind))
    require(len(data) <= 1048576, 1)
    return data

def parse(data, kind):
    require(type(data) is bytes and 0 < len(data) <= 1048576, 1)
    def pairs(items):
        out = {}
        for key, value in items:
            require(key not in out, 1)
            out[key] = value
        return out
    try:
        value = json.loads(data.decode('utf8'), object_pairs_hook=pairs,
                           parse_constant=lambda _: (_ for _ in ()).throw(ValueError()))
        require(raw(value, kind) == data, 8)
    except (UnicodeError, RecursionError, ValueError) as error:
        if isinstance(error, Invalid):
            raise
        raise Invalid(1) from error
    return value

def native(module, value, kind):
    return module.raw(value, kind).decode('utf8')

def native_read(module, data, kind):
    try:
        return module.parse(data.encode('utf8'), kind)
    except (ValueError, UnicodeError, TypeError) as error:
        raise Invalid(4) from error

@lru_cache(maxsize=32)
def source_bytes(case_data):
    case = json.loads(case_data)
    ctx, state, inputs, events = r2.trace_case(case)
    source = dict(caseId=case['name'], roundBase=native(r2, ctx['base'], 'Base'),
                predecessor=encode(ctx['predecessor']).decode('utf8'),
                roundInputs=[native(r2, inp, 'RoundInput') for inp in ctx['roundInputs']],
                roundEvents=[event.decode('utf8') for event in ctx['roundEvents']],
                committed=native(r2, ctx['committed'], 'RoundState'),
                resultInputs=[native(r2, inp, 'ResultInput') for inp in inputs],
                resultEvents=[event.decode('utf8') for event in events],
                resultState=native(r2, state, 'ResultState'))
    return raw(source, 'Source')

def source_for(case):
    return parse(source_bytes(encode(case)), 'Source')

@lru_cache(maxsize=128)
def _checked_source(data):
    source = parse(data, 'Source')
    case = next((case for case in r2.cases() if case['name'] == source['caseId']), None)
    require(case is not None, 3)
    expected = source_for(case)
    require(all(source[key] == expected[key] for key in (
        'roundBase', 'predecessor', 'roundInputs', 'roundEvents', 'committed')), 4)
    ctx = dict(base=native_read(r2, source['roundBase'], 'Base'),
               predecessor=json.loads(source['predecessor']),
               roundInputs=[native_read(r2, inp, 'RoundInput') for inp in source['roundInputs']],
               roundEvents=[event.encode('utf8') for event in source['roundEvents']],
               committed=native_read(r2, source['committed'], 'RoundState'))
    inputs = [native_read(r2, inp, 'ResultInput') for inp in source['resultInputs']]
    expected_inputs = [json.loads(inp) for inp in expected['resultInputs']]
    require(len(inputs) == len(expected_inputs) <= 32, 1)
    require([(inp['command']['kind'], inp['command']['choice'], inp['actor']) for inp in inputs]
            == [(inp['command']['kind'], inp['command']['choice'], inp['actor']) for inp in expected_inputs], 3)
    try:
        final = r2.read_state(source['resultState'].encode('utf8'), ctx, inputs,
                              [event.encode('utf8') for event in source['resultEvents']])
    except (ValueError, KeyError, TypeError) as error:
        raise Invalid(4) from error
    for inp, data in zip(inputs, source['resultEvents']):
        event = json.loads(data); effect = event['effect']
        require(effect.get('reason') in (None, 'owner-choice', 'not-required'), 3)
        if inp['command']['kind'] == 'choose':
            require(event['author'] == inp['actor'] and effect.get('reason') == 'owner-choice'
                    and effect['kind'] in ('disposition-recorded', 'custody-settled')
                    and effect['payload']['kind'] == inp['command']['choice'], 3)
    require(final['closed'] and final['status'] == 'closed' and final['window'] is None
            and final['caCompletionReceiptId'] is not None and final['roundClosureReceiptId'] is not None, 5)
    require(all(settlement[key] is not None for settlement in final['world']['settlements']
                for key in ('disposition', 'losses', 'retreat', 'relationships')), 5)
    return ctx, final

def checked_source(data):
    # Authentication caches never expose mutable stored authority to callers.
    return copy.deepcopy(_checked_source(data))

def derive(source):
    ctx, final = checked_source(raw(source, 'Source'))
    boundary = ctx['base']['boundary']
    cycle = copy.deepcopy(boundary['cycle'])
    world = final['world']
    own = next(element for element in world['elements']
               if element['elementId'] == ctx['base']['steps']['selection']['attacker']['unit']['elementId'])
    require(own['reserveStatus'] == 'none', 3)
    member = dict(unit=rel.world.unit(own, world['creationBinding']), status='none', baseCpa=10,
                  spentCp=copy.deepcopy(own['operationalState']['capabilityPointsExpended']),
                  history=rel.empty_history(cycle))
    release = dict(contractVersion=1, profile='settled-empty-release', cycle=cycle,
                   firstActingSide=boundary['firstActingSide'], positionId=cyc.edge(cycle)['releasePositionId'],
                   priorVersion=final['stateVersion'], priorPrefix=final['prefix'],
                   combatCompletionReceiptId=final['caCompletionReceiptId'],
                   retainedWorldHash=sha(r2.raw(world, 'World')), randomState=copy.deepcopy(final['randomState']),
                   acceptedHighWater=None, members=[member], attackHistory=copy.deepcopy(ctx['committed']['attackHistory']))
    proof = dict(scope=rel.scope(cycle), ordinal=cycle['ordinal'],
                 completionReceiptId='probe.result2.movement-completed',
                 endLocations=cyc.locations(boundary['world']), excludedBefore=[])
    certificate = dict(policyId=CERTIFICATE, provenance='synthetic',
                       boundaryWorldHash=sha(r2.raw(boundary['world'], 'World')),
                       proof=native(cyc, proof, 'MovementEndProof'))
    return release, certificate

def base_for(case):
    source = source_for(case)
    release, certificate = derive(source)
    return dict(contractVersion=1, sourcePolicyId=POLICY, source=source,
                certificate=certificate, releaseBase=native(rel, release, 'ReleaseBase'))

@lru_cache(maxsize=128)
def checked_base(data):
    base = parse(data, 'BridgeBase')
    require(base['contractVersion'] == 1 and base['sourcePolicyId'] == POLICY, 3)
    release, certificate = derive(base['source'])
    require(base['certificate'] == certificate and base['releaseBase'] == native(rel, release, 'ReleaseBase'), 4)
    try:
        rel.validate_base(release)
    except ValueError as error:
        raise Invalid(4) from error
    return True

def read_base(data):
    checked_base(data)
    return parse(data, 'BridgeBase')

def identity(base):
    return 'rcf.' + r2.steps.digest(INVENTORY['domains']['bridge'], raw(base, 'BridgeBase'))

def cycle_base(base, state):
    ctx, final = checked_source(raw(base['source'], 'Source'))
    commits = [event for event in ctx['roundEvents'] if json.loads(event)['effect']['kind'] == 'attack-committed']
    require(len(commits) == 1, 4)
    event = json.loads(commits[0])
    return dict(contractVersion=1, profile='settled-control', releaseBase=json.loads(base['releaseBase']),
                releaseInputs=[dict(command=json.loads(inp['command']['nativeCommand']),
                                    actor=inp['actor'], admittedAt=inp['admittedAt'], clockAvailable=inp['clockAvailable'])
                               for inp in state['inputs'] if inp['command']['kernel'] == 'release'],
                releaseEvents=[json.loads(event['nativeEvent']) for event in state['events']
                               if event['input']['command']['kernel'] == 'release'],
                world=copy.deepcopy(final['world']), movementEnd=json.loads(base['certificate']['proof']),
                progress=[dict(eventType=event['eventType'], receiptId=event['receiptId'], eventHash=sha(commits[0]))],
                targetUses=copy.deepcopy(ctx['committed']['targetUses']))

def initial(base):
    checked_base(raw(base, 'BridgeBase'))
    state = rel.initial(json.loads(base['releaseBase']))
    return dict(contractVersion=1, bridgeId=identity(base), baseHash=sha(raw(base, 'BridgeBase')),
                phase='release', stateVersion=state['stateVersion'], prefix=state['prefix'],
                kernelState=native(rel, state, 'ReleaseState'), inputs=[], events=[])

def kernel_parts(base, state, kernel):
    require(kernel in ('release', 'cycle'), 3)
    if kernel == 'release':
        require(state['phase'] == 'release', 5)
        return rel, json.loads(base['releaseBase']), native_read(rel, state['kernelState'], 'ReleaseState')
    require(state['phase'] in ('cycle', 'finished'), 5)
    return cyc, cycle_base(base, state), native_read(cyc, state['kernelState'], 'CycleControlState')

def command(base, state, kernel, kind):
    module, derived, current = kernel_parts(base, state, kernel)
    value = rel.command(current, kind) if kernel == 'release' else cyc.command(derived, current, kind)
    return dict(contractVersion=1, bridgeId=identity(base), kernel=kernel,
                nativeCommand=native(module, value, 'ReleaseCommand' if kernel == 'release' else 'CycleControlCommand'))

def trusted(cmd, actor='system', now=None, available=True):
    return dict(command=cmd, actor=actor, admittedAt=now, clockAvailable=available)

def apply(base, prior, inp):
    typed(inp, 'BridgeInput')
    inp = canonical(inp, 'BridgeInput')
    cmd = inp['command']
    require(cmd['contractVersion'] == 1 and cmd['bridgeId'] == identity(base), 4)
    require(cmd['kernel'] in ('release', 'cycle'), 3)
    module = rel if cmd['kernel'] == 'release' else cyc
    native_cmd = native_read(module, cmd['nativeCommand'], 'ReleaseCommand' if module is rel else 'CycleControlCommand')
    allowed = ('open', 'complete') if module is rel else ('open', 'finish', 'expire', 'unavailable', 'fallback-step')
    require(native_cmd['kind'] in allowed, 3)
    if module is rel:
        require(inp['admittedAt'] is None and inp['clockAvailable'], 3)
    for accepted, event in zip(prior['inputs'], prior['events']):
        if raw(accepted['command'], 'BridgeCommand') == raw(cmd, 'BridgeCommand') and accepted['actor'] == inp['actor']:
            return prior, None, event['receiptId']
    module, derived, current = kernel_parts(base, prior, cmd['kernel'])
    native_input = dict(command=native_cmd, actor=inp['actor'], admittedAt=inp['admittedAt'], clockAvailable=inp['clockAvailable'])
    try:
        after, event_data, _ = module.transition(derived, current, native_input)
        if event_data is None:
            return prior, None, None
        require(module.read_event(event_data, derived, current, native_input) == after, 6)
    except ValueError as error:
        raise Invalid(6) from error
    event = dict(contractVersion=1, sourcePolicyId=POLICY, bridgeId=identity(base),
                 baseHash=sha(raw(base, 'BridgeBase')), input=copy.deepcopy(inp), nativeEvent=event_data.decode('utf8'))
    receipt = 'rcfr.' + r2.steps.digest(INVENTORY['domains']['receipt'], encode(event))
    event['receiptId'] = receipt
    state = copy.deepcopy(prior)
    state['inputs'].append(copy.deepcopy(inp)); state['events'].append(event)
    require(len(state['events']) <= 4, 1)
    if module is rel and after['status'] == 'completed':
        derived = cycle_base(base, state)
        try:
            cyc.assess(derived)
            after = cyc.initial(derived)
        except ValueError as error:
            raise Invalid(3) from error
        state['phase'] = 'cycle'
        module = cyc
    elif module is cyc and after['status'] == 'finished':
        state['phase'] = 'finished'
    state.update(stateVersion=after['stateVersion'], prefix=after['prefix'],
                 kernelState=native(module, after, 'ReleaseState' if module is rel else 'CycleControlState'))
    return state, raw(event, 'BridgeEvent'), receipt

def replay(base, inputs, events):
    require(type(inputs) is list and type(events) is list and len(inputs) == len(events) <= 4, 1)
    state = initial(base)
    for inp, event in zip(inputs, events):
        state, expected, _ = apply(base, state, inp)
        require(expected is not None and raw(event, 'BridgeEvent') == expected, 6)
    return state

@lru_cache(maxsize=512)
def checked_state(base_data, data):
    base = read_base(base_data)
    state = parse(data, 'BridgeState')
    require(data == raw(replay(base, state['inputs'], state['events']), 'BridgeState'), 6)
    return True

def read_state(data, base):
    checked_state(raw(base, 'BridgeBase'), data)
    return parse(data, 'BridgeState')

def transition(base, prior, inp):
    read_state(raw(prior, 'BridgeState'), base)
    return apply(base, prior, inp)

def read_event(data, base, prior, inp):
    parse(data, 'BridgeEvent')
    after, expected, _ = transition(base, prior, inp)
    require(data == expected, 6)
    return after

def test_all_lineages():
    cuts = retries = guard_cases = entitlement_cases = 0
    modes = {}
    for case in r2.cases():
        base = base_for(case)
        state = initial(base)
        assert read_base(raw(base, 'BridgeBase')) == base
        assert read_state(raw(state, 'BridgeState'), base) == state
        assert json.loads(base['releaseBase'])['acceptedHighWater'] is None
        cuts += 1
        versions = [state['stateVersion']]
        for kernel, kind, actor, now in (
                ('release', 'open', 'system', None), ('release', 'complete', 'system', None),
                ('cycle', 'open', 'system', 3500), ('cycle', 'finish', case['attackerSide'], 3501)):
            if state['phase'] == 'finished':
                break
            inp = trusted(command(base, state, kernel, kind), actor, now)
            prior = state
            state, event, receipt = transition(base, prior, inp)
            assert event and receipt and read_event(event, base, prior, inp) == state
            assert read_state(raw(state, 'BridgeState'), base) == state
            retry, extra, rid = transition(base, state, inp)
            assert retry == state and extra is None and rid == receipt
            cuts += 1; retries += 1
            versions.append(state['stateVersion'])
        assert state['phase'] == 'finished'
        final = json.loads(state['kernelState'])
        source = json.loads(base['source']['resultState'])
        committed = json.loads(base['source']['committed'])
        cycle = json.loads(base['releaseBase'])['cycle']
        assert final['positionId'] == cyc.edge(cycle)['finishPositionId'] and final['activeCycle'] is None
        assert r2.raw(final['world'], 'World') == r2.raw(source['world'], 'World')
        assert final['randomState'] == source['randomState']
        assert final['attackHistory'] == committed['attackHistory']
        assert final['targetUses'] == committed['targetUses']
        assert final['world']['futureObligations'] == source['world']['futureObligations']
        assert len(state['events']) == 4
        assert [json.loads(event['nativeEvent'])['eventType'] for event in state['events']] == [
            'reserve-release-opened', 'reserve-release-completed',
            'movement-combat-control-opened', 'movement-combat-phase-finished']
        modes[cyc.control_mode(final['assessment'])] = modes.get(cyc.control_mode(final['assessment']), 0) + 1
        guard_cases += bool(final['world']['guards'])
        entitlement_cases += bool(final['world']['replacementEntitlements'])
        assert versions == list(range(versions[0], versions[0] + len(versions)))
    assert guard_cases == entitlement_cases == 8
    assert modes == {'owner-choice': 32}
    return dict(traces=32, cuts=cuts, retries=retries, guards=guard_cases, entitlements=entitlement_cases, modes=modes)

def rejected(call):
    try:
        call()
    except Invalid:
        return
    raise AssertionError('invalid bridge admitted')

def sample():
    return base_for(r2.cases()[0])

def to_cycle(base):
    state = initial(base)
    for kind in ('open', 'complete'):
        state, _, _ = transition(base, state, trusted(command(base, state, 'release', kind)))
    return state

def mutate_native(value, field, mutate):
    obj = json.loads(value[field]); mutate(obj); value[field] = encode(obj).decode('utf8')

def test_authentication():
    base = sample(); checks = 0
    for change in (
            lambda b: b.update(contractVersion=2), lambda b: b.update(sourcePolicyId='unsupported'),
            lambda b: b['source'].update(caseId='unsupported'),
            lambda b: b['certificate'].update(policyId='unsupported'),
            lambda b: b['certificate'].update(provenance='creation-rooted'),
            lambda b: b['certificate'].update(boundaryWorldHash='sha256:' + '0' * 64),
            lambda b: mutate_native(b['certificate'], 'proof', lambda p: p.update(completionReceiptId='forged')),
            lambda b: mutate_native(b['certificate'], 'proof', lambda p: p.update(ordinal=2)),
            lambda b: mutate_native(b['certificate'], 'proof', lambda p: p.update(endLocations=list(reversed(p['endLocations'])))),
            lambda b: mutate_native(b, 'releaseBase', lambda r: r.update(acceptedHighWater=10001)),
            lambda b: mutate_native(b, 'releaseBase', lambda r: r.update(members=[])),
            lambda b: mutate_native(b, 'releaseBase', lambda r: r.update(priorVersion=r['priorVersion'] + 1)),
            lambda b: mutate_native(b['source'], 'resultState', lambda r: r.update(closed=False)),
            lambda b: mutate_native(b['source'], 'resultState', lambda r: r.update(caCompletionReceiptId=None)),
            lambda b: mutate_native(b['source'], 'resultState', lambda r: r['world']['futureObligations'].append({})),
            lambda b: mutate_native(b['source'], 'committed', lambda r: r.update(contractVersion=1)),
            lambda b: b['source']['resultEvents'].pop(),
            lambda b: b['source']['roundEvents'].reverse()):
        changed = copy.deepcopy(base); change(changed)
        rejected(lambda: read_base(raw(changed, 'BridgeBase'))); checks += 1
    # Valid native shape is insufficient: bind every leaf of both derived artifacts.
    obj = json.loads(base['releaseBase'])
    for key in obj:
        changed = copy.deepcopy(base); bad = copy.deepcopy(obj)
        bad[key] = None if bad[key] is not None else 0
        changed['releaseBase'] = encode(bad).decode('utf8')
        rejected(lambda: read_base(raw(changed, 'BridgeBase'))); checks += 1
    return checks

def test_commands_events_states():
    base = sample(); state = initial(base); count = 0
    for kernel, kind, actor, now in (('release', 'open', 'system', None), ('release', 'complete', 'system', None),
                                    ('cycle', 'open', 'system', 3500), ('cycle', 'finish', 'axis', 3501)):
        inp = trusted(command(base, state, kernel, kind), actor, now)
        after, data, _ = transition(base, state, inp)
        for key in after:
            bad = copy.deepcopy(after)
            if key in ('inputs', 'events'):
                bad[key] = bad[key][:-1]
            elif key == 'kernelState':
                mutate_native(bad, key, lambda native_state: native_state.update(stateVersion=native_state['stateVersion'] + 1))
            elif type(bad[key]) is int:
                bad[key] += 1
            else:
                bad[key] = 'forged'
            rejected(lambda: read_state(raw(bad, 'BridgeState'), base)); count += 1
        event = json.loads(data)
        for key in event:
            bad = copy.deepcopy(event)
            if key == 'input':
                bad[key]['actor'] = 'commonwealth'
            elif key == 'nativeEvent':
                mutate_native(bad, key, lambda ev: ev.update(receiptId='forged'))
            elif type(bad[key]) is int:
                bad[key] += 1
            else:
                bad[key] = 'forged'
            rejected(lambda: read_event(raw(bad, 'BridgeEvent'), base, state, inp)); count += 1
        for change in (lambda i: i['command'].update(bridgeId='forged'),
                       lambda i: i['command'].update(contractVersion=2),
                       lambda i: i.update(actor='commonwealth'),
                       lambda i: mutate_native(i['command'], 'nativeCommand', lambda c: c.update(expectedPriorVersion=0))):
            bad = copy.deepcopy(inp); change(bad)
            rejected(lambda: transition(base, state, bad)); count += 1
        state = after
    cycle = to_cycle(base)
    for kind in ('repeat', 'fallback-step', 'finish'):
        rejected(lambda: transition(base, cycle, trusted(command(base, cycle, 'cycle', kind), 'axis', 3500)))
    wrong = trusted(command(base, initial(base), 'release', 'complete'))
    rejected(lambda: transition(base, initial(base), wrong))
    forged = copy.deepcopy(state); forged['events'][0]['receiptId'] = 'forged'
    rejected(lambda: transition(base, forged, inp))
    return count + 5

def test_raw_contracts():
    base = sample(); state = initial(base)
    inp = trusted(command(base, state, 'release', 'open'))
    after, event, _ = transition(base, state, inp)
    checks = 0
    for value, kind in ((base, 'BridgeBase'), (base['source'], 'Source'), (base['certificate'], 'Certificate'),
                        (inp['command'], 'BridgeCommand'), (inp, 'BridgeInput'),
                        (json.loads(event), 'BridgeEvent'), (after, 'BridgeState')):
        data = raw(value, kind)
        assert parse(data, kind) == value
        key = next(iter(value))
        first = json.dumps(key).encode() + b':' + encode(value[key])
        raws = [b'\xef\xbb\xbf' + data, data + b' ', b' ' + data, data[:-1],
                b'{"unexpected":0,' + data[1:], b'{' + first + b',' + data[1:],
                encode(dict(reversed(list(value.items())))), b'\xff', b'{}', b'x' * 1048577]
        if 'contractVersion' in value:
            raws.extend([data.replace(b'"contractVersion":1', b'"contractVersion":1.0', 1),
                         data.replace(b'"contractVersion":1', b'"contractVersion":true', 1)])
        for bad in raws:
            rejected(lambda: parse(bad, kind)); checks += 1
    for bad in (dict(state, inputs=[inp] * 513), dict(state, stateVersion=2**63),
                dict(state, stateVersion=True), dict(state, kernelState='')):
        rejected(lambda: read_state(raw(bad, 'BridgeState'), base)); checks += 1
    oversized = dict(base['source'], resultEvents=['{}'] * 33, resultInputs=['{}'] * 33)
    rejected(lambda: checked_source(raw(oversized, 'Source')))
    rejected(lambda: replay(base, [inp] * 5, [json.loads(event)] * 5))
    return checks + 2

def test_clock_policy():
    base = sample(); cycle = to_cycle(base); checks = 0
    assert json.loads(cycle['kernelState'])['acceptedHighWater'] is None
    for now, available in ((0, True), (3500, True), (None, True), (None, False),
                           (3500, False), (253402300799999, True)):
        opened, _, _ = transition(base, cycle, trusted(command(base, cycle, 'cycle', 'open'), now=now, available=available))
        control = json.loads(opened['kernelState'])
        if control['openingClockFailure']:
            finish = trusted(command(base, opened, 'cycle', 'fallback-step'))
        else:
            assert control['timing']['openedAtUnixMilliseconds'] == now
            finish = trusted(command(base, opened, 'cycle', 'finish'), 'axis', now)
        final, event, _ = transition(base, opened, finish)
        assert final['phase'] == 'finished' and event
        assert json.loads(final['kernelState'])['world'] == json.loads(base['source']['resultState'])['world']
        checks += 1
    opened, _, _ = transition(base, cycle, trusted(command(base, cycle, 'cycle', 'open'), now=3500))
    deadline = json.loads(opened['kernelState'])['timing']['deadlineUnixMilliseconds']
    for kind, actor, now, available, expected in (
            ('expire', 'system', deadline - 1, True, None),
            ('expire', 'system', deadline, True, 'deadline'),
            ('unavailable', 'system', 3501, True, 'controller-unavailable'),
            ('finish', 'axis', 3499, True, 'clock-unavailable'),
            ('finish', 'axis', None, True, 'clock-unavailable'),
            ('finish', 'axis', 3501, False, 'clock-unavailable')):
        final, event, _ = transition(base, opened, trusted(command(base, opened, 'cycle', kind), actor, now, available))
        if expected is None:
            assert final == opened and event is None
        else:
            wrapped = json.loads(event); native_event = json.loads(wrapped['nativeEvent'])
            assert native_event['author'] == 'system' and native_event['effect']['reason'] == expected
            assert final['phase'] == 'finished'
        checks += 1
    rejected(lambda: transition(base, opened, trusted(command(base, opened, 'cycle', 'finish'), 'axis', deadline)))
    for now in (-1, 253402300800000, True, 3500.0):
        rejected(lambda: transition(base, cycle, trusted(command(base, cycle, 'cycle', 'open'), now=now)))
        checks += 1
    return checks + 1

def test_prior_time_isolation():
    checks = changed_audits = 0
    for case in r2.cases():
        base = base_for(case)
        source = copy.deepcopy(base['source'])
        ctx, _ = checked_source(raw(source, 'Source'))
        current = r2.initial(ctx); inputs = []; events = []
        for old in source['resultInputs']:
            inp = json.loads(old)
            if inp['admittedAt'] is not None:
                inp['admittedAt'] += 20000
            current, event, _ = r2.transition(ctx, current, inp)
            inputs.append(native(r2, inp, 'ResultInput')); events.append(event.decode('utf8'))
        source.update(resultInputs=inputs, resultEvents=events, resultState=native(r2, current, 'ResultState'))
        release, certificate = derive(source)
        alternate = dict(base, source=source, certificate=certificate, releaseBase=native(rel, release, 'ReleaseBase'))
        prior = json.loads(base['source']['resultState'])
        # Branches without mandatory windows have no owner time to change.
        if any(json.loads(inp)['admittedAt'] is not None for inp in inputs):
            assert prior['acceptedHighWater'] != current['acceptedHighWater']
            changed_audits += 1
        pair = [(b, to_cycle(b)) for b in (base, alternate)]
        for now, available in ((0, True), (3500, True), (10500, True), (None, True),
                               (3500, False), (253402300799999, True)):
            outcomes = []
            for b, state in pair:
                opened, _, _ = transition(b, state, trusted(command(b, state, 'cycle', 'open'), now=now, available=available))
                control = json.loads(opened['kernelState'])
                outcomes.append(encode({key: control[key] for key in
                                        ('status', 'timing', 'openingClockFailure', 'acceptedHighWater')}))
            assert outcomes[0] == outcomes[1]
            checks += 1
    return dict(comparisons=checks, changedSourceAudits=changed_audits)

def test_frozen_evidence():
    verify_fixture(FIXTURE.read_bytes())
    original = FIXTURE.read_bytes()
    for changed in (original.replace(b'\n', b'\r\n'), original + b' ',
                    original.replace(b'"contractVersion": 1', b'"contractVersion": 1.0', 1),
                    original.replace(b'"contractVersion": 1', b'"contractVersion": true', 1),
                    original.replace(b'"traces":', b'"unexpected": 0, "traces":', 1)):
        rejected(lambda: verify_fixture(changed))
    return dict(pins=len(SOURCE_PINS), tampering=5)

def test_cache_ownership():
    base = sample(); data = raw(base['source'], 'Source')
    ctx, final = checked_source(data)
    ctx['committed']['targetUses'].clear()
    final['world']['elements'].clear()
    authentic_ctx, authentic_final = checked_source(data)
    assert authentic_ctx['committed']['targetUses'] == json.loads(base['source']['committed'])['targetUses']
    assert authentic_final['world'] == json.loads(base['source']['resultState'])['world']
    return 2

def test_native_frames_and_scope():
    base = sample(); count = 0
    for field in ('roundBase', 'predecessor', 'committed', 'resultState'):
        text = base['source'][field]
        obj = json.loads(text); key = next(iter(obj))
        first = json.dumps(key) + ':' + encode(obj[key]).decode('utf8')
        for changed in (text + ' ', '\ufeff' + text, '{' + first + ',' + text[1:],
                        encode(dict(reversed(list(obj.items())))).decode('utf8'),
                        text.replace('"contractVersion":2', '"contractVersion":2.0', 1)
                        if field != 'predecessor' else '{}'):
            altered = copy.deepcopy(base); altered['source'][field] = changed
            rejected(lambda: read_base(raw(altered, 'BridgeBase'))); count += 1
    state = to_cycle(base)
    opened, _, _ = transition(base, state, trusted(command(base, state, 'cycle', 'open'), now=3500))
    finished, _, _ = transition(base, opened, trusted(command(base, opened, 'cycle', 'finish'), 'axis', 3501))
    timer = command(base, opened, 'cycle', 'expire')
    mutate_native(timer, 'nativeCommand', lambda c: c.update(decisionId='stale.decision'))
    stale, event, receipt = transition(base, finished, trusted(timer, now=3502))
    assert stale == finished and event is receipt is None
    retry = copy.deepcopy(finished['inputs'][-1]); retry['admittedAt'] = 4000
    same, event, receipt = transition(base, finished, retry)
    assert same == finished and event is None and receipt == finished['events'][-1]['receiptId']
    rejected(lambda: transition(base, opened, trusted(command(base, opened, 'cycle', 'repeat'), 'axis', 3501)))
    rejected(lambda: replay(base, finished['inputs'][:1] * 2, finished['events'][:1] * 2))
    return count + 4

def test_structured_order():
    base = sample(); state = initial(base)
    inp = trusted(command(base, state, 'release', 'open'))
    after, event, receipt = transition(base, state, inp)
    reordered = dict(reversed(list(inp.items())))
    reordered['command'] = dict(reversed(list(inp['command'].items())))
    actual, actual_event, actual_receipt = transition(base, state, reordered)
    assert actual_event == event and actual_receipt == receipt and actual == after
    assert read_state(raw(actual, 'BridgeState'), base) == actual
    return 2

def test_source_owner_choice_semantics():
    admitted = []; checks = same_world = 0
    for case in r2.cases():
        if not case['name'].startswith(('defender-capture-guard.', 'defender-capture-escape.')):
            continue
        base = base_for(case); source = base['source']
        ctx, original = checked_source(raw(source, 'Source'))
        for mode in ('unavailable', 'backwards', 'opening-unavailable'):
            state = r2.initial(ctx); inputs = []; events = []
            for text in source['resultInputs']:
                inp = json.loads(text)
                if mode == 'opening-unavailable' and inp['command']['kind'] == 'choose' and state['window'] is None:
                    continue
                inp['command'] = r2.command(ctx, state, inp['command']['kind'], inp['command']['choice'])
                if mode == 'opening-unavailable' and state['status'] == 'retreat' and state['world']['custodyLots']:
                    inp.update(admittedAt=None, clockAvailable=False)
                if state['window'] and state['window']['kind'] == 'custody' and inp['command']['kind'] == 'choose':
                    inp['admittedAt'] = None if mode == 'unavailable' else state['window']['timing']['highWaterUnixMilliseconds'] - 1
                    inp['clockAvailable'] = mode != 'unavailable'
                state, event, _ = r2.transition(ctx, state, inp)
                inputs.append(inp); events.append(event)
            assert r2.read_state(r2.raw(state, 'ResultState'), ctx, inputs, events) == state
            custody = next(json.loads(event) for event in events if json.loads(event)['effect']['kind'] == 'custody-settled')
            reason = 'opening-clock-unavailable' if mode == 'opening-unavailable' else 'clock-unavailable'
            assert custody['author'] == 'system' and custody['effect']['reason'] == reason
            assert custody['effect']['payload']['kind'] == 'leave-unguarded'
            if case['name'].startswith('defender-capture-escape.'):
                assert r2.raw(state['world'], 'World') == r2.raw(original['world'], 'World')
                same_world += 1
            else:
                assert len(original['world']['guards']) == 1 and not state['world']['guards']
                assert not original['world']['replacementEntitlements'] and len(state['world']['replacementEntitlements']) == 1
            fork = dict(source, resultInputs=[native(r2, inp, 'ResultInput') for inp in inputs],
                        resultEvents=[event.decode('utf8') for event in events], resultState=native(r2, state, 'ResultState'))
            try:
                checked_source(raw(fork, 'Source'))
            except Invalid:
                pass
            else:
                admitted.append(case['name'] + '.' + mode)
            checks += 1
    assert not admitted, ('source fallback admitted', admitted)
    return dict(rejections=checks, sameWorldFallbacks=same_world)

def retained_vectors():
    require(all(hashlib.sha256((ROOT / name).read_bytes()).hexdigest() == expected
                for name, expected in SOURCE_PINS.items()), 4)
    traces = []
    for case in r2.cases():
        base = base_for(case); state = initial(base)
        cuts = [dict(stateHash=sha(raw(state, 'BridgeState')), bytes=len(raw(state, 'BridgeState')))]
        for kernel, kind, actor, now in (
                ('release', 'open', 'system', None), ('release', 'complete', 'system', None),
                ('cycle', 'open', 'system', 3500), ('cycle', 'finish', case['attackerSide'], 3501)):
            if state['phase'] == 'finished':
                break
            state, _, _ = transition(base, state, trusted(command(base, state, kernel, kind), actor, now))
            cuts.append(dict(stateHash=sha(raw(state, 'BridgeState')), bytes=len(raw(state, 'BridgeState'))))
        final = json.loads(state['kernelState'])
        traces.append(dict(caseId=case['name'], baseHash=sha(raw(base, 'BridgeBase')),
                           sourceHash=sha(raw(base['source'], 'Source')), certificate=base['certificate'],
                           releaseBase=base['releaseBase'], inputs=state['inputs'], events=state['events'], cuts=cuts,
                           sourceStateHash=sha(base['source']['resultState'].encode('utf8')),
                           finalStateHash=sha(state['kernelState'].encode('utf8')),
                           worldHash=sha(r2.raw(final['world'], 'World')),
                           sourceAuditHighWater=json.loads(base['source']['resultState'])['acceptedHighWater'],
                           mode=cyc.control_mode(final['assessment']), witnesses=len(final['assessment']['witnesses']),
                           successorPositionId=final['positionId'], activeCycle=final['activeCycle']))
    return dict(contractVersion=1, contract='combat-result-cycle-finish-v1', sourcePolicyId=POLICY,
                certificatePolicyId=CERTIFICATE,
                schemaSha256=hashlib.sha256((ROOT / 'combat-result-cycle-finish-v1.schema.json').read_bytes()).hexdigest(),
                sourceHashes=SOURCE_PINS, traces=traces)

@lru_cache(maxsize=1)
def fixture_bytes():
    return (json.dumps(retained_vectors(), indent=2) + '\n').encode('utf8')

def verify_fixture(data):
    require(type(data) is bytes and data == fixture_bytes(), 6)

if __name__ == '__main__':
    for test in (test_frozen_evidence, test_all_lineages, test_authentication, test_commands_events_states, test_raw_contracts, test_clock_policy, test_prior_time_isolation, test_cache_ownership, test_native_frames_and_scope, test_structured_order, test_source_owner_choice_semantics):
        print(test.__name__, test(), flush=True)
