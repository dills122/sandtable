#!/usr/bin/env python3
"""Actual creation-rooted positive entry; private contract oracle, no native activation."""
import copy
import hashlib
import importlib.util
import json
import sys
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent


def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, ROOT / filename)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


bd = load('positive_breakdown', 'verify-combat-inherited-breakdown-completion-v1.py')
lc, rd = bd.lc, bd.lc.im.rd
steps = load('positive_facts', 'verify-combat-selection-steps-v1.py')
encode, sha = lc.encode, lc.sha
INVENTORY = json.loads((ROOT / 'combat-positive-entry-v1.schema.json').read_text())
SCHEMA = lc.SCHEMA | bd.SCHEMA | {
    k: [tuple(f.split(':')) for f in fields.split()] for k, fields in INVENTORY['objects'].items()}
FIXTURE = ROOT / 'fixtures/combat-positive-entry-v1.json'
HISTORY_KEYS = ('preambleEventCanonicalUtf8', 'weatherEventCanonicalUtf8',
                'stageEventCanonicalUtf8', 'reserveEventCanonicalUtf8')
KINDS = ('complete-movement-segment', 'complete-breakdown-segment')


class Invalid(ValueError):
    def __init__(self, code):
        self.code = f'CMB-PEN-{code:03}'
        super().__init__(self.code)


def require(ok, code):
    if not ok:
        raise Invalid(code)


def typed(value, kind, depth=0):
    require(depth <= 32, 1)
    if kind == 'utf8':
        require(type(value) is str and 0 < len(value) <= 1048576 and value.isascii(), 1)
    elif kind.endswith('?'):
        if value is not None:
            typed(value, kind[:-1], depth)
    elif kind in SCHEMA:
        require(type(value) is dict and set(value) == {k for k, _ in SCHEMA[kind]}, 1)
        for k, child in SCHEMA[kind]:
            typed(value[k], child, depth + 1)
    elif kind.endswith('[]'):
        require(type(value) is list and len(value) <= 512, 1)
        for item in value:
            typed(item, kind[:-2], depth + 1)
    else:
        try:
            lc.typed(value, kind, depth)
        except ValueError as error:
            raise Invalid(1) from error


def canonical(value, kind):
    if kind.endswith('?'):
        return None if value is None else canonical(value, kind[:-1])
    if kind in SCHEMA:
        return {k: canonical(value[k], child) for k, child in SCHEMA[kind]}
    if kind.endswith('[]'):
        child = kind[:-2]
        out = [canonical(item, child) for item in value]
        if child in ('UnitKey', 'EndLocation'):
            out.sort(key=lambda x: lc.ctl.rel.key(x if child == 'UnitKey' else x['unit']))
        elif child in rd.pre.env.KEYS:
            out.sort(key=lambda x: tuple(x[k] for k in rd.pre.env.KEYS[child]))
        elif kind == 'id[]':
            out.sort()
        return out
    if kind == 'utf8':
        return value
    return lc.canonical(value, kind)


def raw(value, kind):
    typed(value, kind)
    data = encode(canonical(value, kind))
    require(len(data) <= 1048576, 1)
    return data


def parse(data, kind):
    require(type(data) is bytes and 0 < len(data) <= 1048576, 1)
    def pairs(items):
        out = {}
        for k, v in items:
            require(k not in out, 1)
            out[k] = v
        return out
    try:
        value = json.loads(data.decode('utf8'), object_pairs_hook=pairs,
                           parse_constant=lambda _: (_ for _ in ()).throw(ValueError()))
    except (ValueError, UnicodeError, RecursionError) as error:
        raise Invalid(1) from error
    require(raw(value, kind) == data, 8)
    return value


def packet_bytes(packet):
    # Object entry points apply the same closed inventory/bounds as byte entry points.
    return raw(packet, 'PositiveSource')


def initial(packet):
    packet_bytes(packet)
    require(packet['contractVersion'] == 1, 3)
    counts = (4, 1, 4, 1)
    require(all(len(packet[k]) == n for k, n in zip(HISTORY_KEYS, counts))
            and len(packet['entryEventCanonicalUtf8']) <= 2, 7)
    try:
        q = rd.pre.env.parse(packet['requestCanonicalUtf8'].encode('ascii'), 'Request')
        args = (q, packet['createdCanonicalUtf8'].encode('ascii'),
                *[[x.encode('ascii') for x in packet[k]] for k in HISTORY_KEYS])
        state = rd.replay(*args)
    except ValueError as error:
        raise Invalid(4) from error
    side = state['firstActingSide']
    require(side in INVENTORY['openingHashes'], 4)
    opening = copy.deepcopy(packet)
    opening['entryEventCanonicalUtf8'] = []
    require(sha(packet_bytes(opening)) == INVENTORY['openingHashes'][side], 4)
    require(state['stateVersion'] == 11 and len(state['receipts']) == 10
            and state['sequencePosition'] == lc.MOVEMENT
            and state['cycle']['actingSide'] == side and state['cycle']['ordinal'] == 1
            and state['cycle']['openedAuthorityVersion'] == 11
            and state['cycle']['gameTurn'] == state['cycle']['operationStage'] == 1
            and state['cycle']['playerPhaseSlot'] == 'first-acting-side'
            and state['randomState'] == q['randomState'] | {'nextByteCursor': 2}
            and q['randomState']['seed'] == 1
            and state['operationStageWeather'][0]['kind'] == 'normal'
            and all(e['reserveStatus'] == 'none' for e in state['world']['elements']), 4)
    state.update(tracks=[], actualProgressRefs=[], breakdownFlow={'kind': 'idle'},
                 interruptContext=None, movementEnd=None, breakdownCompletionReceiptId=None)
    return canonical(state, 'CombatEntryState')


def command(state, kind):
    require(kind in KINDS, 3)
    return lc.command(state, kind) if kind == KINDS[0] else bd.command(state)


def input_type(inp):
    require(type(inp) is dict and type(inp.get('command')) is dict, 1)
    kind = inp['command'].get('kind')
    require(kind in KINDS, 3)
    return 'LifecycleInput' if kind == KINDS[0] else 'EntryInput'


def authorize(state, inp):
    kind = input_type(inp)
    typed(inp, kind)
    cmd = inp['command']
    require(cmd['contractVersion'] == 2, 3)
    require(inp['actor'] == (state['cycle']['actingSide'] if kind == 'LifecycleInput' else 'system'), 5)
    require(cmd['creationBinding'] == state['creationBinding']
            and cmd['creationEventHash'] == state['creationEventHash']
            and cmd['cycleId'] == state['cycleId'], 4)
    return kind


def _emit(state, inp):
    # Reuse unchanged event grammar/kernels only after new actual-source admission.
    kind = authorize(state, inp)
    require(inp == command(state, inp['command']['kind']), 6)
    try:
        if kind == 'LifecycleInput':
            require(state['stateVersion'] == 11 and state['breakdownCompletionReceiptId'] is None, 6)
            after, data = lc._emit(state, inp)
            after['breakdownCompletionReceiptId'] = None
        else:
            require(state['stateVersion'] == 12, 6)
            after, data = bd._emit(state, inp)
    except ValueError as error:
        if isinstance(error, Invalid):
            raise
        raise Invalid(6) from error
    return canonical(after, 'CombatEntryState'), data


def read_event(data):
    require(type(data) is bytes and 0 < len(data) <= 1048576, 1)
    try:
        tag = json.loads(data).get('eventType')
        if tag == 'movement-segment-completed':
            event = lc.read_event(data)
        elif tag == 'breakdown-segment-completed':
            event = bd.read_event(data)
        else:
            raise Invalid(3)
    except (ValueError, AttributeError, UnicodeError, RecursionError) as error:
        if isinstance(error, Invalid):
            raise
        raise Invalid(4) from error
    return event


def replay(packet):
    state = initial(packet)
    for text in packet['entryEventCanonicalUtf8']:
        data = text.encode('ascii')
        event = read_event(data)
        state, expected = _emit(state, event['input'])
        require(data == expected, 6)
    return state


def apply(packet, inp):
    state = replay(packet)
    kind = authorize(state, inp)
    for text in packet['entryEventCanonicalUtf8']:
        data = text.encode('ascii')
        accepted = read_event(data)['input']
        if accepted['command']['expectedPriorVersion'] == inp['command']['expectedPriorVersion']:
            require(input_type(accepted) == kind and raw(inp, kind) == raw(accepted, kind), 6)
            return state, data, True
    after, data = _emit(state, inp)
    return after, data, False


def candidate(state):
    if state['breakdownCompletionReceiptId'] is None:
        return None
    require(state['stateVersion'] == 13 and state['sequencePosition'] == bd.TERMINAL
            and state['movementEnd'] is not None, 6)
    weather = state['operationStageWeather'][0]['kind']
    facts = dict(cycle=state['cycle'], world=state['world'], creationBinding=state['creationBinding'],
                 weather=dict(attackerKind=weather, defenderKind=weather))
    result = steps.candidate(facts)
    require(result is not None, 6)
    return result


def proof(packet):
    state = replay(packet)
    digest = hashlib.sha256(INVENTORY['domains']['source'].encode('ascii') + b'\0' + packet_bytes(packet)).hexdigest()
    return canonical(dict(contractVersion=1, sourceId='pe.' + digest, sourceHash='sha256:' + digest,
                          entry=state, candidate=candidate(state)), 'PositiveEntryProof')


def read_proof(data, packet):
    value = parse(data, 'PositiveEntryProof')
    require(data == raw(proof(packet), 'PositiveEntryProof'), 6)
    return value


def read_source(data):
    packet = parse(data, 'PositiveSource')
    replay(packet)
    return packet


def source_packet(side):
    # Test construction only: admission does not import new fixture or trust this helper.
    require(side in ('axis', 'commonwealth'), 3)
    choice = 'act-first' if side == 'axis' else 'act-last'
    case = next(c for c in json.loads(rd.FIXTURE.read_text())['cases']
                if c['name'] == f'normal-{choice}-none')
    result = rd.trace(case)
    assert rd.goldens(result) == case['goldens']
    q, created, preamble, weather, stage, _, reserve, _ = result
    packet = dict(contractVersion=1, requestCanonicalUtf8=encode(q).decode('ascii'),
                  createdCanonicalUtf8=created.decode('ascii'))
    packet.update({k: [x.decode('ascii') for x in records]
                   for k, records in zip(HISTORY_KEYS, (preamble, weather, stage, reserve))})
    packet['entryEventCanonicalUtf8'] = []
    return packet


def trace(side):
    packet = source_packet(side)
    state = replay(packet)
    before = copy.deepcopy(state)
    states, inputs, events, packets = [state], [], [], [copy.deepcopy(packet)]
    for kind in KINDS:
        inp = command(state, kind)
        state, event, duplicate = apply(packet, inp)
        assert not duplicate
        inputs.append(inp)
        events.append(event)
        packet['entryEventCanonicalUtf8'].append(event.decode('ascii'))
        packets.append(copy.deepcopy(packet))
        states.append(state)
    # Literal semantic expectations precede golden freeze/comparison.
    assert [s['stateVersion'] for s in states] == [11, 12, 13]
    assert [len(s['receipts']) for s in states] == [10, 11, 12]
    assert [s['sequencePosition'] for s in states] == [lc.MOVEMENT, lc.TERMINAL, bd.TERMINAL]
    assert [i['actor'] for i in inputs] == [side, 'system']
    for s in states:
        assert s['world'] == before['world'] and s['randomState'] == before['randomState']
        assert s['members'] == before['members'] and s['actualProgressRefs'] == [] and s['tracks'] == []
        assert s['cycle'] == before['cycle'] and s['cycleId'] == before['cycleId']
        assert s['openingBaseHash'] == before['openingBaseHash']
        assert s['completionReceiptId'] == before['completionReceiptId']
        assert s['operationStageWeather'] == before['operationStageWeather']
        assert s['operationStageOrders'] == before['operationStageOrders']
        assert s['breakdownFlow'] == {'kind': 'idle'} and s['interruptContext'] is None
        assert s['sequencePosition']['activeSide'] is None
        for e in s['world']['elements']:
            assert e['operationalState']['capabilityPointsExpended'] == {'numerator': 0, 'denominator': 1}
            assert e['operationalState']['cohesionLevel'] == 0 and e['ammunition']['points'] == 10
            assert [c['currentToe'] for c in e['components']] == [10]
    movement, breakdown = [read_event(e) for e in events]
    assert movement['progress'] == movement['excludedUnits'] == []
    assert movement['contractVersion'] == 3 and breakdown['contractVersion'] == 2
    assert (movement['priorStateVersion'], movement['stateVersion']) == (11, 12)
    assert (breakdown['priorStateVersion'], breakdown['stateVersion']) == (12, 13)
    end = states[1]['movementEnd']
    assert end['endLocations'] == lc.ctl.locations(before['world'])
    assert end['excludedBefore'] == lc.ctl.excluded(end, side) == []
    assert end['scope'] == lc.ctl.rel.scope(before['cycle']) and end['ordinal'] == 1
    assert end['completionReceiptId'] == movement['receiptId'] == breakdown['movementCompletionReceiptId']
    assert states[2]['movementEnd'] == end
    assert states[2]['breakdownCompletionReceiptId'] == breakdown['receiptId']
    assert len({before['completionReceiptId'], movement['receiptId'], breakdown['receiptId']}) == 3
    assert [proof(p)['candidate'] is None for p in packets] == [True, True, False]
    c = proof(packet)['candidate']
    assert c['attacker']['unit']['originalSide'] == side and c['defender']['unit']['originalSide'] != side
    assert c['basis'] == 'voluntary-adjacent' and c['targetLocationId'] == c['defender']['locationId']
    assert c['defender']['locationId'] in rd.world.GRAPH[c['attacker']['locationId']]
    return packets, states, inputs, events


def semantic_red():
    # Retained RED first replayed existing no-move route reader: CMB-IBC-004, both owners.
    for side in ('axis', 'commonwealth'):
        trace(side)



def artifacts(trace_result):
    packets, states, inputs, events = trace_result
    out = {'source': packet_bytes(packets[-1])}
    for i, inp in enumerate(inputs):
        out[f'input-{i+1}'] = raw(inp, input_type(inp))
    for i, event in enumerate(events):
        out[f'event-{i+1}'] = event
    for i, packet in enumerate(packets):
        out[f'proof-{i}'] = raw(proof(packet), 'PositiveEntryProof')
        out[f'state-{i+11}'] = raw(states[i], 'CombatEntryState')
    return out


def goldens(result):
    return {k: dict(bytes=len(data), sha256=sha(data)) for k, data in artifacts(result).items()}


def rejected(label, fn, counts, code=None):
    try:
        fn()
    except Invalid as error:
        if code is not None:
            assert error.code == f'CMB-PEN-{code:03}', (label, error.code, code)
        counts[label.split('/')[0]] = counts.get(label.split('/')[0], 0) + 1
        return
    raise AssertionError('unexpected acceptance: ' + label)


def leaves(value, path=()):
    if type(value) is dict:
        for k, child in value.items():
            yield from leaves(child, path + (k,))
    elif type(value) is list:
        for i, child in enumerate(value):
            yield from leaves(child, path + (i,))
        if not value:
            yield path, value
    else:
        yield path, value


def changed(value, path, replacement):
    out = copy.deepcopy(value)
    target = out
    for part in path[:-1]:
        target = target[part]
    target[path[-1]] = replacement
    return out


def altered(old):
    if type(old) is bool:
        return not old
    if type(old) is int:
        return old + 1
    if old is None:
        return 'forged'
    if type(old) is list:
        return ['forged']
    if old.startswith('sha256:'):
        return sha(b'forged')
    return 'forged'


def semantic_checks(results, counts):
    for side, result in results.items():
        packets, states, inputs, events = result
        for cut, packet in enumerate(packets):
            assert read_source(packet_bytes(packet)) == packet
            data = raw(proof(packet), 'PositiveEntryProof')
            assert read_proof(data, packet)['entry'] == states[cut]
            counts['cuts'] = counts.get('cuts', 0) + 1
            for prior in range(cut):
                after, data, duplicate = apply(packet, copy.deepcopy(inputs[prior]))
                assert duplicate and data == events[prior] and after == states[cut]
                counts['retries'] = counts.get('retries', 0) + 1
            if cut < 2:
                after, data, duplicate = apply(packet, inputs[cut])
                assert not duplicate and data == events[cut] and after == states[cut+1]
            if cut == 0:
                rejected('order/early-breakdown', lambda: apply(packet, inputs[1]), counts, 6)
            if cut == 2:
                for kind in KINDS:
                    rejected('order/new-command-at-terminal',
                             lambda: apply(packet, command(states[cut], kind)), counts, 6)
            # Cache is evidence only; every leaf, empty list and null is replay-authenticated.
            p = proof(packet)
            for path, old in leaves(p):
                bad = changed(p, path, altered(old))
                rejected('proof/leaf', lambda: read_proof(encode(bad), packet), counts)
            for extra in ('completedHistory', 'selection', 'result', 'admittedAt', 'clockAvailable'):
                bad = dict(p, **{extra: None})
                rejected('proof/extra-field', lambda: read_proof(encode(bad), packet), counts, 1)
            for i, inp in enumerate(inputs):
                # Consumed/retry commands never escape owner/scope/input authentication.
                for path, old in leaves(inp):
                    bad = changed(inp, path, altered(old))
                    rejected('input/leaf', lambda: apply(packet, bad), counts)
                for now in (None, '2000-01-01T00:00:00.000Z', '2000-01-01T00:00:00Z', 'bad'):
                    for field, value in [('admittedAt', now), ('clockAvailable', True), ('deadline', now)]:
                        bad = dict(inp, **{field: value})
                        rejected('clock/input', lambda: apply(packet, bad), counts, 1)
                for version in (-1, 0, 9, 10, 13, 14, 2**63-1, 2**63, True, 11.0):
                    bad = changed(inp, ('command', 'expectedPriorVersion'), version)
                    rejected('input/stale-version', lambda: apply(packet, bad), counts)
                for other in ('axis', 'commonwealth', 'system'):
                    if other != inp['actor']:
                        rejected('input/owner', lambda: apply(packet, dict(inp, actor=other)), counts, 5)
        for index, data in enumerate(events):
            event = read_event(data)
            for path, old in leaves(event):
                bad = changed(event, path, altered(old))
                packet = copy.deepcopy(packets[index+1])
                packet['entryEventCanonicalUtf8'][index] = encode(bad).decode('ascii')
                rejected('event/leaf', lambda: replay(packet), counts)
                # Well-typed leaf attacks get a correctly re-signed receipt too.
                try:
                    if index == 0:
                        bad['receiptId'] = lc.receipt(bad, 2)
                        encoded = lc.raw(bad, 'CompleteEvent')
                    else:
                        bad['receiptId'] = bd.receipt(bad)
                        encoded = bd.raw(bad, 'EntryEvent')
                except ValueError:
                    continue
                if path == ('receiptId',):
                    assert encoded == data  # Re-signing restores the genuine receipt; no forgery remains.
                    continue
                packet['entryEventCanonicalUtf8'][index] = encoded.decode('ascii')
                rejected('event/resigned-leaf', lambda: replay(packet), counts)
            for now in ('2000-01-01T00:00:00.000Z', '2000-01-01T00:00:00Z', None):
                bad = copy.deepcopy(event)
                bad['input']['admittedAt'] = now
                unsigned = {k: v for k, v in bad.items() if k != 'receiptId'}
                domain = lc.DOMAINS[2] if index == 0 else bd.INVENTORY['domains']['receipt']
                prefix = 'iml.' if index == 0 else 'ibc.'
                bad['receiptId'] = prefix + hashlib.sha256(domain.encode() + b'\0' + encode(unsigned)).hexdigest()
                packet = copy.deepcopy(packets[index+1])
                packet['entryEventCanonicalUtf8'][index] = encode(bad).decode('ascii')
                rejected('clock/resigned-event', lambda: replay(packet), counts)
        terminal = packets[-1]
        histories = ([], [events[1]], [events[1], events[0]], [events[0], events[0]],
                     [events[0], events[1], events[1]])
        for records in histories[1:]:
            bad = dict(terminal, entryEventCanonicalUtf8=[e.decode('ascii') for e in records])
            rejected('order/suffix', lambda: replay(bad), counts)
        # Every predecessor record and identity leaf is authenticated, including earlier cuts.
        for key in ('requestCanonicalUtf8', 'createdCanonicalUtf8', *HISTORY_KEYS):
            records = terminal[key] if key in HISTORY_KEYS else [terminal[key]]
            for index, text in enumerate(records):
                value = json.loads(text)
                for path, old in leaves(value):
                    bad_text = encode(changed(value, path, altered(old))).decode('ascii')
                    bad = copy.deepcopy(terminal)
                    if key in HISTORY_KEYS:
                        bad[key][index] = bad_text
                    else:
                        bad[key] = bad_text
                    rejected('source/leaf', lambda: replay(bad), counts)
                for extra in ('admittedAt', 'completedHistory'):
                    bad = copy.deepcopy(terminal)
                    bad_text = encode(dict(value, **{extra: '2000-01-01T00:00:00.000Z'})).decode('ascii')
                    if key in HISTORY_KEYS:
                        bad[key][index] = bad_text
                    else:
                        bad[key] = bad_text
                    rejected('source/clock-or-cache', lambda: replay(bad), counts)
            if key in HISTORY_KEYS:
                for records in (terminal[key][:-1], terminal[key]*2, list(reversed(terminal[key]))):
                    if records != terminal[key]:
                        rejected('source/order-cut', lambda: replay(dict(terminal, **{key: records})), counts)
                # Earlier creation-rooted partial prefix remains outside new entry admission.
                for cut in range(len(terminal[key])):
                    rejected('source/predecessor-cut',
                             lambda: replay(dict(terminal, **{key: terminal[key][:cut]})), counts, 7)
        other = results['commonwealth' if side == 'axis' else 'axis'][0][-1]
        for key in (*HISTORY_KEYS, 'entryEventCanonicalUtf8'):
            if other[key] != terminal[key]:
                rejected('source/cross-owner', lambda: replay(dict(terminal, **{key: other[key]})), counts)
        rejected('proof/cross-owner',
                 lambda: read_proof(raw(proof(other), 'PositiveEntryProof'), terminal), counts, 6)
        for field in ('world', 'entry', 'candidate', 'movementEnd', 'clockAvailable'):
            rejected('source/completed-state', lambda: replay(dict(terminal, **{field: None})), counts, 1)
        # Returned projections/arrays are owned; future replay does not retain caller mutations.
        p = proof(terminal)
        p['entry']['world']['elements'].clear()
        p['candidate']['attacker']['componentIds'].clear()
        assert proof(terminal)['entry'] == states[-1]
        copied = read_source(packet_bytes(terminal))
        copied['entryEventCanonicalUtf8'].clear()
        assert replay(terminal) == states[-1]
        # Old no-move route reader and current synthetic C3a remain closed.
        args = (rd.pre.env.parse(terminal['requestCanonicalUtf8'].encode(), 'Request'),
                terminal['createdCanonicalUtf8'].encode(),
                *[[t.encode() for t in terminal[k]] for k in HISTORY_KEYS])
        try:
            lc.initial(*args, [])
        except lc.Invalid as error:
            assert error.code == 'CMB-IML-003'
        else:
            raise AssertionError('old route reader widened')


def unsupported_checks(results, counts):
    for case in json.loads(rd.FIXTURE.read_text())['cases']:
        if case['name'] in ('normal-act-first-none', 'normal-act-last-none'):
            continue
        result = rd.trace(case)
        assert rd.goldens(result) == case['goldens']
        q, created, preamble, weather, stage, _, reserve, _ = result
        packet = dict(contractVersion=1, requestCanonicalUtf8=encode(q).decode('ascii'),
                      createdCanonicalUtf8=created.decode('ascii'))
        packet.update({k: [e.decode('ascii') for e in records]
                       for k, records in zip(HISTORY_KEYS, (preamble, weather, stage, reserve))})
        packet['entryEventCanonicalUtf8'] = []
        rejected('unsupported/' + case['name'], lambda: replay(packet), counts)
    for side, result in results.items():
        packets, states, _, _ = result
        args = lc.source_trace(side, 1)
        bad = dict(packets[0], entryEventCanonicalUtf8=[args[-1][0].decode('ascii')])
        rejected('unsupported/actual-move', lambda: replay(bad), counts)
        # Actual initial facts do not satisfy frozen seed0 C3a boundary admission.
        boundary = json.loads(json.loads(steps.FIXTURE.read_text())['boundaries'][0]['canonicalUtf8'])
        boundary['creationBinding'] = states[-1]['creationBinding']
        try:
            steps.boundary(encode(boundary))
        except steps.Invalid as error:
            assert error.code == 'CMB-STP-004'
        else:
            raise AssertionError('actual source promoted to synthetic C3a')


def canonical_checks(results, counts):
    for result in results.values():
        packets, _, inputs, events = result
        cases = [('PositiveSource', packets[-1]), ('PositiveEntryProof', proof(packets[-1]))]
        cases += [(input_type(i), i) for i in inputs]
        cases += [('CompleteEvent' if i == 0 else 'EntryEvent', json.loads(e)) for i, e in enumerate(events)]
        for kind, value in cases:
            data = raw(value, kind)
            version = value.get('contractVersion', value.get('command', {}).get('contractVersion', 1))
            variants = [b'\xef\xbb\xbf'+data, data+b' ', data+b'\n', data[:-1]+b',"extra":null}',
                        encode(dict(reversed(list(value.items())))),
                        b'{"contractVersion":true,'+data[1:],
                        data.replace(b'"contractVersion":'+str(version).encode(),
                                     b'"contractVersion":1.0', 1),
                        b'{"contractVersion":1,"contractVersion":1,'+data[1:],
                        data.replace(b'"contractVersion"', b'"\\u0063ontractVersion"', 1),
                        b'\xff', b'null', b'[]', b'{', b'['*40+b'0'+b']'*40]
            for bad in variants:
                rejected('canonical/' + kind, lambda: parse(bad, kind), counts)
        for key in HISTORY_KEYS + ('entryEventCanonicalUtf8',):
            rejected('capacity/array513', lambda: replay(dict(packets[-1], **{key: ['{}']*513})), counts, 1)
        for text in ('a'*1048577, 'é', '\ud800', ''):
            bad = dict(packets[-1], createdCanonicalUtf8=text)
            rejected('capacity/string', lambda: replay(bad), counts, 1)
        for data in (b'', b' '*1048577):
            rejected('capacity/bytes', lambda: read_source(data), counts, 1)
        rejected('capacity/depth', lambda: typed(packets[-1], 'PositiveSource', 33), counts, 1)
    # Exact byte/string and array bounds, separate shape probes (not admitted sources).
    data = raw('a'*1048574, 'utf8')
    assert len(data) == 1048576 and parse(data, 'utf8') == 'a'*1048574
    rejected('capacity/byte-edge', lambda: raw('a'*1048575, 'utf8'), counts, 1)
    typed(['{}']*512, 'utf8[]')
    rejected('capacity/array-edge', lambda: typed(['{}']*513, 'utf8[]'), counts, 1)


def verify():
    fixture = json.loads(FIXTURE.read_text())
    repo = ROOT.parent.parent
    for path, digest in fixture['sourceHashes'].items():
        require(sha((repo/path).read_bytes()) == digest, 9)
    assert fixture['contract'] == INVENTORY['contract'] and fixture['task'] == INVENTORY['task']
    results = {side: trace(side) for side in ('axis', 'commonwealth')}
    counts = {}
    semantic_checks(results, counts)
    unsupported_checks(results, counts)
    canonical_checks(results, counts)
    for case in fixture['cases']:
        assert case['expected'] == dict(versions=[11, 12, 13], receiptCounts=[10, 11, 12],
                                       seed=1, cursor=2, capabilityPoints=[0, 0], ammunition=[10, 10],
                                       toe=[10, 10], candidateCuts=[False, False, True])
        result = results[case['actor']]
        values = artifacts(result)
        assert goldens(result) == case['goldens']
        assert values['source'].decode('ascii') == case['sourceCanonicalUtf8']
        assert [e.decode('ascii') for e in result[3]] == case['eventCanonicalUtf8']
        assert [raw(i, input_type(i)).decode('ascii') for i in result[2]] == case['inputCanonicalUtf8']
        assert [values[f'proof-{i}'].decode('ascii') for i in range(3)] == case['proofCanonicalUtf8']
    print('PASS positive entry: 2 actual owners, 2 events each, 6 literal proofs;', json.dumps(counts, sort_keys=True))


if __name__ == '__main__':
    if len(sys.argv) == 2 and sys.argv[1] == '--semantic':
        semantic_red()
        print('PASS semantic positive entry: both actual owners, versions11->12->13')
    elif len(sys.argv) == 1:
        verify()
    else:
        raise SystemExit('usage: verify-combat-positive-entry-v1.py [--semantic]')
