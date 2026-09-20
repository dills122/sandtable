#!/usr/bin/env python3
"""H0 literal roots from independently replayed family histories; contract evidence only."""
from __future__ import annotations
import copy
import hashlib
import functools
import time
import importlib.util
import json
import sys
import types
from collections import Counter
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent
REPO = ROOT.parent.parent
FIXTURE = ROOT / 'fixtures/combat-inherited-snapshot-v1.json'


def load(stem):
    print('H0 import', stem, flush=True)
    spec = importlib.util.spec_from_file_location('h0_' + stem.replace('-', '_'), ROOT / ('verify-combat-' + stem + '-v1.py'))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


f6 = load('inherited-reaction-movement-completion')
f5, f2 = f6.irs, f6.irl
f1 = f2.irt
im = f1.imv
rd = im.rd
stage = rd.stage
weather = stage.weather
pre = rd.pre
env = pre.env
lc = load('inherited-movement-lifecycle')
g2 = load('inherited-breakdown-completion')
f3 = load('inherited-reaction-closure')
f4 = load('inherited-reaction-active-fallback')
F6_CASES = tuple(json.loads(f6.FIXTURE.read_text())['cases'])
MODULES = dict(B1=pre, C=weather, B2=stage, D=rd, E1=im, E2G1=lc,
    G2=g2, F1=f1, F2=f2, F3=f3, F4=f4, F5=f5, F6=f6)
EXTERNAL = dict(C2=env, Pre=pre, Weather=weather, Reserve=rd, Movement=im,
    Lifecycle=lc, Reaction=f2, Fallback=f4)
SCHEMA_PATH = ROOT / 'combat-inherited-snapshot-v1.schema.json'
INVENTORY = json.loads(SCHEMA_PATH.read_text())
SCHEMA = {k: [tuple(f.split(':', 1)) for f in v.split()] for k, v in INVENTORY['objects'].items()}
encode, sha = env.encode, env.sha


class Invalid(ValueError):
    pass


def require(ok, message):
    if not ok:
        raise Invalid(message)


def typed(value, kind, depth=0):
    require(depth <= 32, 'depth')
    if kind.endswith('?'):
        if value is not None:
            typed(value, kind[:-1], depth)
    elif kind.endswith('[]'):
        require(type(value) is list and len(value) <= 512, 'array')
        for item in value:
            typed(item, kind[:-2], depth + 1)
    elif kind in INVENTORY['aliases']:
        typed(value, INVENTORY['aliases'][kind], depth)
    elif kind in INVENTORY['unions']:
        require(type(value) is dict and value.get('kind') in INVENTORY['unions'][kind], 'union')
        typed(value, INVENTORY['unions'][kind][value['kind']], depth)
    elif kind in SCHEMA:
        require(type(value) is dict and list(value) == [k for k, _ in SCHEMA[kind]], 'ordered fields: ' + kind)
        for key, child in SCHEMA[kind]:
            typed(value[key], child, depth + 1)
    elif '.' in kind:
        alias, child = kind.split('.', 1)
        module = EXTERNAL[alias]
        external_typed(value, module, child, depth)
        require(module.canonical(value, child) == value, 'external canonical value')
        require(encode(module.canonical(value, child)) == encode(value), 'external canonical order')
    elif kind == 'null':
        require(value is None, 'null')
    else:
        pre.typed(value, kind, depth)


def external_typed(value, module, kind, depth, field=''):
    require(depth <= 32, 'depth')
    if kind.endswith('?'):
        if value is not None:
            external_typed(value, module, kind[:-1], depth, field)
    elif kind.endswith('[]'):
        bound = 4096 if kind == 'Cause[]' and field == 'cohesionCauses' else 512
        require(type(value) is list and len(value) <= bound, 'external array')
        for child in value:
            external_typed(child, module, kind[:-2], depth + 1)
    elif kind in module.SCHEMA:
        require(type(value) is dict and list(value) == [key for key, _ in module.SCHEMA[kind]], 'external fields')
        for key, child in module.SCHEMA[kind]:
            external_typed(value[key], module, child, depth + 1, key)
    elif module is env:
        module.typed(value, kind, depth=depth)
    else:
        module.typed(value, kind, depth)


def limits(value, depth=0, field=''):
    require(depth <= 32, 'depth')
    if type(value) is dict:
        for key, child in value.items():
            limits(child, depth + 1, key)
    elif type(value) is list:
        require(len(value) <= (4096 if field == 'cohesionCauses' else 512), 'array')
        for child in value:
            limits(child, depth + 1, field)


def raw(value):
    limits(value)
    typed(value, 'Snapshot')
    require(value['contractVersion'] == 12, 'version')
    data = encode(value)
    require(len(data) <= 1048576, 'root bytes')
    return data


def parse(data):
    require(type(data) is bytes and 0 < len(data) <= 1048576, 'root bytes')
    def pairs(items):
        out = {}
        for key, value in items:
            require(key not in out, 'duplicate')
            out[key] = value
        return out
    try:
        value = json.loads(data.decode('ascii'), object_pairs_hook=pairs,
            parse_constant=lambda _: (_ for _ in ()).throw(Invalid('constant')))
        require(raw(value) == data, 'canonical bytes')
        return value
    except (ValueError, KeyError, TypeError, UnicodeError, RecursionError) as error:
        raise Invalid('invalid root') from error


def bounded_history(args, events):
    # Public H0 evidence context is concrete owned tuples/lists, never a lazy enumerable.
    require(type(args) is tuple and len(args) >= 2 and type(args[0]) is dict, 'context')
    require(type(args[1]) is bytes and 0 < len(args[1]) <= 1048576, 'retained creation required')
    require(type(events) is list and all(type(group) is list for group in args[2:]), 'history collections')
    groups = (*args[2:], events)
    require(sum(len(group) for group in groups) <= 512, 'history count')
    total = 0
    for group in groups:
        for event in group:
            require(type(event) is bytes and 0 < len(event) <= 1048576, 'event bytes')
            total += len(event)
            require(total <= 16 * 1048576, 'history bytes')
    return [event for group in groups for event in group]


def request_bytes(request):
    # Bounded grammar (including strings/arrays/depth) before serialization or ownership copy.
    env.typed(request, 'Request')
    data = encode(request)
    require(len(data) <= 1048576, 'request bytes')
    return data


def replay_context(sample):
    args, events = sample['args'], sample['events']
    request_bytes(args[0])
    history = bounded_history(args, events)
    # Bounds checked before copying; isolate mutable caller request/collection ownership.
    args, events = copy.deepcopy(args), copy.deepcopy(events)
    family = sample['family']
    if family == 'F6':
        # F6's fixture selector is not a trust source: actual F5 transcript is replayed first.
        predecessor = f5.replay(*args)
        expected, _, _ = f6.initial(sample['case'])
        require(encode(predecessor) == encode(expected), 'F6 trusted predecessor')
        state = f6.replay(sample['case'], events)
    else:
        state = MODULES[family].replay(*args, events)
    # Reserve entry exists before a family-specific Reserve cache is requested.
    if state['sequencePosition']['positionId'] == rd.opening.position('entryFromPositionId')['positionId']:
        q, created = args[:2]
        reserve_entry = rd.initial(q, created, history[:4], history[4:5], history[5:9])
        if 'members' not in state:
            require(state['stateVersion'] == 10 and len(history) == 9, 'Reserve entry')
            state = reserve_entry
    return args[0], args[1], history, state


def compose(request, created, history, state):
    # Internal only: caller state is always output from replay_context, never accepted cache.
    root = env.make_snapshot(created, request, pre.CONTEXT)
    require(state['configurationHash'] == sha(encode(env.canonical(root['configuration'], 'Config'))), 'configuration binding')
    require(state['creationBinding'] == root['creationReceipt']['creationBinding']
        and state['creationEventHash'] == sha(created), 'creation binding')
    require(state['campaignId'] == root['campaignId'] and state['rulesetHash'] == root['rulesetHash'], 'identity')
    require(state['stateVersion'] == len(history) + 1 and len(state['receipts']) == len(history), 'complete ledger')
    prefix = env.sequence.prefix_creation(created)
    for index, event in enumerate(history):
        receipt = state['receipts'][index]
        require(receipt['eventHash'] == sha(event) and receipt['stateVersion'] == index + 2, 'receipt history')
        prefix = env.sequence.prefix_event(prefix, event)
    require(prefix == state['prefix'], 'trusted history prefix')
    for key in ('stateVersion', 'world', 'initiativeHolder', 'operationStageOrders', 'randomState'):
        root[key] = copy.deepcopy(state[key])
    if 'operationStageWeather' in state:
        root['operationStageWeather'] = copy.deepcopy(state['operationStageWeather'])
    else:
        require(len(history) <= 4, 'absent Weather before Weather only')
    flow = state.get('breakdownFlow')
    if flow is None:
        # No move/stop can have been committed before these exact pre-route histories.
        require(not state.get('tracks') and not state.get('actualProgressRefs')
            and not state.get('reactionWindow') and not state.get('interruptContext')
            and all(json.loads(event)['eventType'] not in (
                'element-moved', 'reacting-element-moved', 'element-movement-stopped',
                'reaction-participant-completed') for event in history), 'idle proof')
        flow = dict(kind='idle')
    root['breakdownFlow'] = copy.deepcopy(flow)
    if 'currentPosition' in state:
        root['currentPosition'] = copy.deepcopy(state['currentPosition'])
    else:
        tag = 'breakdown-stop' if flow['kind'] == 'phasing-stop' else 'sequence'
        root['currentPosition'] = dict(kind=tag, sequencePosition=copy.deepcopy(state['sequencePosition']))
    root['reactionWindow'] = copy.deepcopy(state.get('reactionWindow'))
    root['chroniclePrefix'] = state['prefix']
    root['commandReceipts'] = copy.deepcopy(state['receipts'])
    if 'members' in state:
        arm = dict(kind='reserve-designation', firstActingSide=state['firstActingSide'], members=copy.deepcopy(state['members']))
        if state['cycle'] is not None:
            arm['kind'] = 'inherited-cycle'
            for key in ('cycle', 'cycleId', 'openingBaseHash', 'completionReceiptId', 'sequencePosition'):
                arm[key] = copy.deepcopy(state[key])
            # These fields did not exist in earlier causal contracts. Their absence is proven
            # by the accepted event transcript; family identity never selects their value.
            for key in ('tracks', 'actualProgressRefs'):
                if key not in state:
                    require(len(history) <= 11 and state['cycle']['openedAuthorityVersion'] == state['stateVersion'], 'absent movement evidence')
                arm[key] = copy.deepcopy(state.get(key, []))
            for key in ('interruptContext', 'movementEnd', 'breakdownCompletionReceiptId'):
                if key not in state:
                    disallowed = {'interruptContext': {'element-movement-stopped'},
                        'movementEnd': {'movement-segment-completed'},
                        'breakdownCompletionReceiptId': {'breakdown-segment-completed'}}[key]
                    require(not any(json.loads(e)['eventType'] in disallowed for e in history),
                        'absent ' + key + ' proof')
                arm[key] = copy.deepcopy(state.get(key))
        root['cycleState'] = arm
    else:
        require(len(history) < 9, 'pre-Reserve proof')
    return root


@functools.lru_cache(maxsize=1024)
def memoized_root(family, request, created, groups, events, case):
    sample = dict(family=family, args=(json.loads(request), created, *[list(g) for g in groups]),
        events=list(events), case=json.loads(case))
    return raw(compose(*replay_context(sample)))


def root_bytes(sample):
    require(sample['family'] in MODULES, 'known evidence interface')
    require(sample['case'] in F6_CASES if sample['family'] == 'F6' else sample['case'] is None,
        'pinned fixture descriptor')
    bounded_history(sample['args'], sample['events'])
    request = request_bytes(sample['args'][0])
    return memoized_root(sample['family'], request, sample['args'][1],
        tuple(tuple(g) for g in sample['args'][2:]), tuple(sample['events']), encode(sample['case']))


def read_root(data, trusted_context):
    parse(data)
    expected = root_bytes(trusted_context)
    require(data == expected, 'trusted root mismatch')
    return expected


def cases(module):
    return json.loads(module.FIXTURE.read_text())['cases']


def inventory():
    out = []
    def add(family, label, args, events, states, case=None):
        print('H0 inventory', family, label, len(states), flush=True)
        for index, state in enumerate(states):
            out.append(dict(family=family, label=label + '/' + str(index), args=tuple(args),
                events=list(events[:index]), case=case, expectedState=state, coverage='fixture-state'))
    for c in cases(pre):
        q, created, _, events, states = pre.trace(c)
        add('B1', c['name'], (q, created), events, states)
    for c in cases(weather):
        for choice in ('act-first', 'act-last'):
            q, created, opening, _, event, before, after = weather.trace(c, choice)
            add('C', str(c['seed']) + '/' + choice, (q, created, opening), [event], [before, after])
    for c in cases(stage):
        for choice in ('act-first', 'act-last'):
            q, created, opening, w, _, events, states = stage.trace(c, choice)
            add('B2', str(c['seed']) + '/' + choice, (q, created, opening, w), events, states)
    for c in cases(rd):
        q, created, opening, w, s, _, events, states = rd.trace(c)
        add('D', c['name'], (q, created, opening, w, s), events, states)
    for family in ('E1', 'E2G1', 'F2', 'F4', 'F5'):
        module = MODULES[family]
        for i, c in enumerate(cases(module)):
            args, _, events, states = module.trace(c)
            add(family, c.get('name', str(i)), args, events, states)
    for i, c in enumerate(cases(g2)):
        args, _, event, before, after = g2.trace(c)
        add('G2', str(i), args, [event], [before, after])
    for c in cases(f1):
        args, _, event, after = f1.trace(c)
        add('F1', c['name'], args, [event], [f1.initial(*args), after])
    for c in cases(f3):
        args, _, event, before, after = f3.trace(c)
        add('F3', c['name'], args, [event], [before, after])
    for c in cases(f6):
        predecessor = next(p for p in cases(f5) if p['name'] == c['predecessorCase'])
        args, _, previous, _ = f5.trace(predecessor)
        _, _, events, states = f6.trace(c)
        add('F6', c['name'], (*args, previous), events, states, c)
    # Close every selected transcript under prefixes. Existing interface cuts remain distinct;
    # supplemental cuts are deduplicated by exact creation plus exact ordered event bytes.
    known = {(sample['args'][1], tuple(bounded_history(sample['args'], sample['events']))) for sample in out}
    supplemental = []
    for sample in out:
        q, created = sample['args'][:2]
        history = bounded_history(sample['args'], sample['events'])
        for count in range(len(history) + 1):
            prefix = history[:count]
            identity = (created, tuple(prefix))
            if identity in known:
                continue
            # Current selected fixtures' only uncovered prefixes are B1 prefixes for Weather seeds.
            # Reject an unexpected coverage gap rather than guessing a new history router here.
            require(count <= 4, 'uncovered later causal prefix needs explicit adapter')
            state = pre.replay(q, created, prefix)
            supplemental.append(dict(family='B1', label='supplemental/' + sha(created)[7:] + '/' +
                str(count) + '/' + sha(b''.join(prefix))[7:], args=(q, created), events=prefix,
                case=None, expectedState=state, coverage='supplemental-prefix'))
            known.add(identity)
    print('H0 supplemental prefixes', len(supplemental), flush=True)
    return out + supplemental


def rejected(call):
    try:
        call()
    except (ValueError, KeyError, TypeError, AssertionError, RecursionError):
        return
    raise AssertionError('invalid input accepted')


def measure(value, depth=0):
    if type(value) is dict:
        children = [measure(v, depth + 1) for v in value.values()]
    elif type(value) is list:
        children = [measure(v, depth + 1) for v in value]
    else:
        children = []
    return (max([depth] + [x[0] for x in children]), max([len(value) if type(value) is list else 0] + [x[1] for x in children]))


def source_pins():
    paths = {SCHEMA_PATH, ROOT / 'combat-snapshot-composition-v1.schema.json',
        ROOT / 'fixtures/combat-snapshot-composition-v1.json', ROOT / 'verify-combat-snapshot-composition-v1.py'}
    seen = set()
    def visit(module):
        if id(module) in seen:
            return
        seen.add(id(module))
        path = Path(getattr(module, '__file__', '')).resolve()
        if path.parent != ROOT or not path.name.startswith('verify-'):
            return
        paths.add(path)
        stem = path.name.removeprefix('verify-').removesuffix('.py')
        for other in (ROOT / (stem + '.schema.json'), ROOT / 'fixtures' / (stem + '.json')):
            if other.is_file():
                paths.add(other)
        for name in getattr(module, 'SOURCE_PATHS', []):
            target = REPO / name
            if not target.is_file():
                target = ROOT / name
            require(target.is_file(), 'declared source path: ' + name)
            paths.add(target)
        for value in vars(module).values():
            if isinstance(value, types.ModuleType):
                visit(value)
    for module in MODULES.values():
        visit(module)
    # Follow normative source declarations, not just imported Python modules.
    pending = list(paths)
    inspected = set()
    while pending:
        path = pending.pop()
        if path in inspected or path.suffix != '.json':
            continue
        inspected.add(path)
        document = json.loads(path.read_text())
        for key in ('sourceHashes', 'sourcePins'):
            pins = document.get(key, {})
            if type(pins) is list:
                pins = {item['path']: item['sha256'] for item in pins}
            require(type(pins) is dict, 'source pin declaration')
            if pins:
                for name, digest in pins.items():
                    target = REPO / name
                    if not target.is_file():
                        target = ROOT / name
                    require(target.is_file(), 'missing source: ' + name)
                    if type(digest) is dict:
                        digest = digest['sha256']
                    require(sha(target.read_bytes()) == digest, 'declared source pin: ' + name)
                    if target not in paths:
                        paths.add(target)
                        pending.append(target)
    return {str(path.relative_to(REPO)): sha(path.read_bytes()) for path in sorted(paths)}


def mutate(value):
    if type(value) is dict and value:
        result = copy.deepcopy(value)
        key = next(iter(value))
        result[key] = mutate(value[key])
        return result
    if type(value) is list:
        return [mutate(value[0]), *copy.deepcopy(value[1:])] if value else ['forged']
    if type(value) is str:
        if value.startswith('sha256:') or len(value) == 64:
            return value[:-1] + ('0' if value[-1] != '0' else '1')
        if value in ('axis', 'commonwealth'):
            return 'commonwealth' if value == 'axis' else 'axis'
        return value + 'x'
    if type(value) is int:
        return value + 1
    if type(value) is bool:
        return not value
    return 'forged'


def falsification(samples):
    counts = Counter()
    terminals = {sample['family']: sample for sample in samples if sample['coverage'] == 'fixture-state'}
    for family, sample in terminals.items():
        print('H0 falsification', family, flush=True)
        data = root_bytes(sample)
        value = json.loads(data)
        # Full nested edits must be rejected against causal authority even after canonical re-encoding.
        for path in (('world', 'elements', 0, 'currentLocationId'),
                     ('randomState', 'nextByteCursor'), ('configuration', 'schemaVersion'),
                     ('creationReceipt', 'creationEventHash'), ('commandReceipts', 0, 'commandHash'),
                     ('commandReceipts', 0, 'actor')):
            changed = copy.deepcopy(value)
            node = changed
            for key in path[:-1]:
                node = node[key]
            node[path[-1]] = mutate(node[path[-1]])
            rejected(lambda changed=changed: read_root(encode(changed), sample))
            counts['nestedRootMutations'] += 1
        # Check each independently supplied trusted predecessor segment as well as local events.
        for index in range(2, len(sample['args']) + 1):
            group = sample['events'] if index == len(sample['args']) else sample['args'][index]
            if not group:
                continue
            for mode in ('omit', 'duplicate', 'reorder', 'raw', 'receipt-tamper'):
                changed = copy.deepcopy(sample)
                target = changed['events'] if index == len(sample['args']) else changed['args'][index]
                if mode == 'omit':
                    target.pop()
                elif mode == 'duplicate':
                    target.append(target[-1])
                elif mode == 'reorder':
                    if len(target) < 2:
                        continue
                    target[0], target[-1] = target[-1], target[0]
                elif mode == 'raw':
                    target[-1] += b' '
                else:
                    event = json.loads(target[-1])
                    event['receiptId'] = 'forged-receipt'
                    target[-1] = encode(event)
                rejected(lambda changed=changed: read_root(data, changed))
                counts['trustedHistoryMutations'] += 1
        for field in ('created', 'request'):
            changed = copy.deepcopy(sample)
            args = list(changed['args'])
            if field == 'created':
                args[1] = b''
            else:
                args[0]['campaignId'] += '-foreign'
            changed['args'] = tuple(args)
            rejected(lambda changed=changed: read_root(data, changed))
            counts['trustedCreationMutations'] += 1
        changed = copy.deepcopy(value)
        changed['chroniclePrefix'] = mutate(changed['chroniclePrefix'])
        changed['creationReceipt']['creationEventHash'] = mutate(changed['creationReceipt']['creationEventHash'])
        for receipt in changed['commandReceipts']:
            receipt['eventHash'] = mutate(receipt['eventHash'])
            receipt['commandHash'] = mutate(receipt['commandHash'])
        rejected(lambda: read_root(encode(changed), sample))
        counts['multipleBindingMutations'] += 1
    # Recompute every affected digest binding after a structurally valid event forgery.
    sample = terminals['F1']
    event = json.loads(sample['events'][-1])
    event['destinationLocationId'] = event['originLocationId']
    event['receiptId'] = f1.receipt(event)
    forged_event = f1.raw(event, 'TriggerEvent')
    require(event['receiptId'] == f1.receipt(json.loads(forged_event)), 'resigned event receipt')
    forged = json.loads(root_bytes(sample))
    before = dict(sample, events=[])
    prior = json.loads(root_bytes(before))
    forged['chroniclePrefix'] = env.sequence.prefix_event(prior['chroniclePrefix'], forged_event)
    forged['commandReceipts'][-1]['receiptId'] = event['receiptId']
    forged['commandReceipts'][-1]['eventHash'] = sha(forged_event)
    forged['commandReceipts'][-1]['commandHash'] = sha(f1.raw(event['input'], 'InheritedInput'))
    forged['cycleState']['actualProgressRefs'][-1]['receiptId'] = event['receiptId']
    forged['cycleState']['actualProgressRefs'][-1]['eventHash'] = sha(forged_event)
    forged_data = raw(forged)
    require(parse(forged_data) == forged, 'forged root remains structurally canonical')
    rejected(lambda: read_root(forged_data, sample))
    rejected(lambda: read_root(forged_data, dict(sample, events=[forged_event])))
    counts['recomputedBindingForgeries'] = 2
    # Same creation/predecessor, different lawful closure with all bindings genuinely recomputed.
    original = next(s for s in samples if s['family'] == 'F3' and s['label'] == 'axis-timeout/1')
    alternate = next(s for s in samples if s['family'] == 'F3' and s['label'] == 'axis-scripted-unavailable/1')
    require(original['args'] == alternate['args'], 'shared trusted predecessor')
    rejected(lambda: read_root(root_bytes(alternate), original))
    counts['lawfulAlternateTail'] = 1
    # Every nested field in the newly composed arms/unions, across distinct shapes and both owners.
    representatives = {}
    for sample in samples:
        value = json.loads(root_bytes(sample))
        cycle = value['cycleState']
        window = value['reactionWindow']
        shape = (sample['expectedState'].get('firstActingSide'),
            None if cycle is None else cycle['kind'], value['currentPosition']['kind'],
            value['breakdownFlow']['kind'], None if window is None else (
                bool(window['activeOpportunityId']), len(window['resolvedOpportunityIds'])))
        representatives.setdefault(shape, (sample, value))
    def leaves(value, path=()):
        if type(value) is dict and value:
            for key, child in value.items():
                yield from leaves(child, (*path, key))
        elif type(value) is list and value:
            for index, child in enumerate(value):
                yield from leaves(child, (*path, index))
        else:
            yield path
    for sample, value in representatives.values():
        for field in ('cycleState', 'currentPosition', 'reactionWindow', 'breakdownFlow'):
            for path in leaves(value[field], (field,)):
                changed = copy.deepcopy(value)
                node = changed
                for key in path[:-1]:
                    node = node[key]
                node[path[-1]] = mutate(node[path[-1]])
                rejected(lambda changed=changed: read_root(encode(changed), sample))
                counts['newArmNestedMutations'] += 1
    counts['newArmShapes'] = len(representatives)
    # A different fully lawful, independently regenerated branch is still wrong trusted authority.
    a = next(s for s in samples if s['family'] == 'D' and s['label'] == 'normal-act-first-none/1')
    b = next(s for s in samples if s['family'] == 'D' and s['label'] == 'normal-act-last-none/1')
    rejected(lambda: read_root(root_bytes(a), b))
    counts['lawfulForeignBranch'] += 1
    sample = samples[0]
    renamed = dict(sample, label='untrusted diagnostic label', expectedState={'forged': True})
    require(root_bytes(sample) == root_bytes(renamed), 'diagnostic cache authority')
    counts['diagnosticIndependence'] = 1
    args = sample['args'][:2]
    bounded_history(args, [b'{}'] * 512)
    rejected(lambda: bounded_history(args, [b'{}'] * 513))
    bounded_history(args, [b'x' * 1048576] * 16)
    rejected(lambda: bounded_history(args, [b'x' * 1048576] * 17))
    rejected(lambda: bounded_history(args, [b'x' * 1048577]))
    limits([0] * 512)
    rejected(lambda: limits([0] * 513))
    limits({'cohesionCauses': [0] * 4096})
    rejected(lambda: limits({'cohesionCauses': [0] * 4097}))
    # Exercise typed World cause allowance with genuine predecessor Cause shape, not just length walker.
    cause_type = im.SCHEMA['Cause']
    def primitive(kind):
        if kind in ('id', 'string', 'text', 'locator'): return 'test'
        if kind == 'hash': return 'sha256:' + '0' * 64
        if kind == 'rawHash': return '0' * 64
        if kind in ('int', 'long', 'ulong'): return 0
        if kind == 'side': return 'axis'
        if kind == 'bool': return False
        if kind.endswith('?') or kind == 'null': return None
        if kind.endswith('[]') or kind == 'empty': return []
        if kind in im.SCHEMA: return {k: primitive(t) for k, t in im.SCHEMA[kind]}
        raise AssertionError('unhandled cause primitive: ' + kind)
    cause = {key: primitive(kind) for key, kind in cause_type}
    external_typed([cause] * 4096, im, 'Cause[]', 1, 'cohesionCauses')
    rejected(lambda: external_typed([cause] * 4097, im, 'Cause[]', 1, 'cohesionCauses'))
    nested = 0
    for _ in range(32): nested = {'x': nested}
    limits(nested)
    rejected(lambda: limits({'x': nested}))
    rejected(lambda: parse(b' ' * 1048577))
    counts['capacityBoundaryChecks'] = 14
    return counts


def verify(samples):
    vectors, shared, stats = [], {}, Counter()
    family_counts = Counter()
    supplemental_counts = Counter()
    for sample in samples:
        print('H0 cut', sample['family'], sample['label'], flush=True)
        data = root_bytes(sample)
        q, created, history, state = replay_context(sample)
        original = sample['expectedState']
        # Only B2's actual Reserve entry adds typed membership during normalization.
        require(all(state[k] == v for k, v in original.items()), 'independent replay state')
        require(read_root(data, sample) == data, 'readback')
        identity = (sha(created), tuple(sha(e) for e in history))
        if identity in shared:
            require(shared[identity][0] == data, 'shared-cut root disagreement')
            shared[identity][1].append(sample['family'] + ':' + sample['label'])
        else:
            shared[identity] = (data, [sample['family'] + ':' + sample['label']])
        value = json.loads(data)
        if not history:
            require(data == encode(env.canonical(env.make_snapshot(created, q, pre.CONTEXT), 'Snapshot')), 'C2 bytes')
            env.read_snapshot(data, created, q, pre.CONTEXT)
        for field in value:
            changed = copy.deepcopy(value)
            changed[field] = mutate(value[field])
            rejected(lambda changed=changed: read_root(encode(changed), sample))
            stats['rootFieldMutations'] += 1
            omitted = copy.deepcopy(value)
            del omitted[field]
            rejected(lambda omitted=omitted: read_root(encode(omitted), sample))
            stats['rootFieldOmissions'] += 1
        for bad in (b' ' + data, data + b'\n', b'\xef\xbb\xbf' + data,
                    data.replace(b'"contractVersion":12', b'"contractVersion":12.0', 1),
                    data.replace(b'"contractVersion":12', b'"contractVersion":12,"contractVersion":12', 1),
                    encode(dict(reversed(list(value.items())))),
                    data.replace(b'"campaignId"', b'"campaign\\u0049d"', 1)):
            rejected(lambda bad=bad: read_root(bad, sample))
            stats['rawNegatives'] += 1
        depth, array = measure(value)
        stats['maxRootBytes'] = max(stats['maxRootBytes'], len(data))
        stats['maxDepth'] = max(stats['maxDepth'], depth)
        stats['maxArrayItems'] = max(stats['maxArrayItems'], array)
        stats['maxHistoryEvents'] = max(stats['maxHistoryEvents'], len(history))
        stats['maxHistoryBytes'] = max(stats['maxHistoryBytes'], sum(map(len, history)))
        if sample['coverage'] == 'fixture-state':
            family_counts[sample['family']] += 1
        else:
            supplemental_counts[sample['family']] += 1
        vectors.append(dict(coverage=sample['coverage'], family=sample['family'], label=sample['label'], stateVersion=value['stateVersion'],
            creationHash=sha(created), eventHashes=[sha(e) for e in history], bytes=len(data), sha256=sha(data), canonicalJson=data.decode('ascii')))
    groups = [labels for _, labels in shared.values() if len(labels) > 1]
    for required in ({'B2', 'D'}, {'F1', 'F2', 'F3'}, {'F2', 'F4', 'F5'}):
        require(any(required <= {label.split(':', 1)[0] for label in labels} for labels in groups), 'required shared cut')
    stats['cuts'] = len(samples)
    stats['fixtureStateCuts'] = sum(family_counts.values())
    stats['supplementalPrefixCuts'] = sum(supplemental_counts.values())
    stats['uniqueCuts'] = len(shared)
    stats['sharedGroups'] = len(groups)
    return dict(contractVersion=1, contract='combat-inherited-snapshot-v1', sourcePins=source_pins(),
        measured=dict(stats), familyCuts=dict(sorted(family_counts.items())),
        supplementalFamilyCuts=dict(sorted(supplemental_counts.items())), sharedCuts=groups, roots=vectors)


def schema_guard():
    return {key: sha(encode([getattr(module, 'SCHEMA', {}), module.INVENTORY]))
        for key, module in (MODULES | EXTERNAL).items()}


def main():
    started = time.monotonic()
    schemas_before = schema_guard()
    frozen = json.loads(FIXTURE.read_text())
    require(frozen['sourcePins'] == source_pins(), 'source pins changed')
    samples = inventory()
    negative = falsification(samples)
    result = verify(samples)
    result['falsification'] = dict(negative)
    require(schema_guard() == schemas_before, 'predecessor schema mutation')
    require(json.loads(FIXTURE.read_text()) == result, 'frozen fixture mismatch')
    print('H0 literal Snapshot12:', json.dumps(result['measured'], sort_keys=True))
    print('Falsification:', json.dumps(result['falsification'], sort_keys=True))
    print('Elapsed seconds:', round(time.monotonic() - started, 3), 'memoization:', memoized_root.cache_info())
    print('Family cuts:', json.dumps(result['familyCuts'], sort_keys=True))
    print('Contract evidence only; runtime router, disabled-admission restore and publication remain open.')


if __name__ == '__main__':
    main()
