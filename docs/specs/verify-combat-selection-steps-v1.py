#!/usr/bin/env python3
"""C3a selection/step contract oracle; no production authority or full Snapshot12."""
import copy
import hashlib
import importlib.util
import json
import re
import sys
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('selection_envelopes', ROOT/'verify-combat-authority-envelope-v1.py')
env = importlib.util.module_from_spec(spec); spec.loader.exec_module(env)
seq, inputs, world = env.sequence, env.inputs, env.world
INVENTORY = json.loads((ROOT/'combat-selection-steps-v1.schema.json').read_text())
SCHEMA = env.SCHEMA | {'Authority': seq.SCHEMA['Authority'], 'Timing': inputs.SCHEMA['Timing']} | {
    k: [tuple(f.split(':')) for f in v.split()] for k, v in INVENTORY['objects'].items()}
TAGS = INVENTORY['effectTags']
KINDS = {'open-segment', 'choose-selection', 'close-empty-selection', 'complete-step', 'open-rba',
         'decline-rba', 'expire-window', 'controller-unavailable'}
FIXTURE = ROOT/'fixtures/combat-selection-steps-v1.json'


class Invalid(ValueError):
    def __init__(self, code, path=''):
        self.code, self.path = f'CMB-STP-{code:03}', path
        super().__init__(f'{self.code} {path or "/"}')


def require(ok, code, path=''):
    if not ok: raise Invalid(code, path)


def encode(v): return env.encode(v)
def sha(raw): return env.sha(raw)
def digest(domain, raw): return hashlib.sha256(domain.encode('ascii') + b'\0' + raw).hexdigest()


def typed(v, kind, path='', depth=0):
    require(depth <= 32, 1, path)
    if kind.endswith('?'):
        if v is not None: typed(v, kind[:-1], path, depth)
    elif kind == 'Effect':
        require(type(v) is dict and type(v.get('kind')) is str and v['kind'] in TAGS, 3, path)
        typed(v, TAGS[v['kind']], path, depth)
    elif kind in SCHEMA:
        require(type(v) is dict, 1, path)
        for k, child in SCHEMA[kind]:
            require(k in v, 1, path+'/'+k); typed(v[k], child, path+'/'+k, depth+1)
        require(set(v) == {k for k, _ in SCHEMA[kind]}, 1, path)
    elif kind.endswith('[]'):
        require(type(v) is list and len(v) <= 512, 1, path)
        for i, x in enumerate(v): typed(x, kind[:-2], f'{path}/{i}', depth+1)
    elif kind == 'actor': require(v in ('axis', 'commonwealth', 'system'), 2, path)
    else:
        try: env.typed(v, kind, path, depth)
        except env.Invalid as e: raise Invalid(int(e.code[-3:]), e.path) from e


def canonical(v, kind):
    if kind.endswith('?'): return None if v is None else canonical(v, kind[:-1])
    if kind == 'Effect': return canonical(v, TAGS[v['kind']])
    if kind in SCHEMA: return {k: canonical(v[k], c) for k, c in SCHEMA[kind]}
    if kind.endswith('[]'): return [canonical(x, kind[:-2]) for x in v]
    return v


def parse(raw, kind):
    require(type(raw) is bytes and 0 < len(raw) <= 1048576, 1)
    def pairs(items):
        v = {}
        for k, x in items:
            require(k not in v, 1); v[k] = x
        return v
    try: v = json.loads(raw.decode('utf-8'), object_pairs_hook=pairs,
                       parse_constant=lambda _: (_ for _ in ()).throw(ValueError()))
    except (ValueError, UnicodeError, RecursionError) as e: raise Invalid(1) from e
    typed(v, kind)
    require(raw == encode(canonical(v, kind)), 8)
    return v


def boundary(raw):
    b = parse(raw, 'Boundary'); c = env.Context(); q = c.request(); cycle = b['cycle']
    require(b['contractVersion'] == 1, 3)
    require(b['creationBinding'] == env.binding(q), 4, '/creationBinding')
    for k, expected in [('campaignId', q['campaignId']), ('rulesetHash', q['rulesetHash']),
                        ('setupId', q['setupId']), ('setupHash', q['setupHash']),
                        ('contentPackId', q['content']['packId']), ('contentHash', q['content']['hash']),
                        ('scenarioId', q['content']['scenarioId']), ('admittedPolicyBundleDigest', c.config_hash)]:
        require(cycle[k] == expected, 4, '/cycle/'+k)
    try: seq.identity(cycle, 'Authority', b['firstActingSide'])
    except seq.Invalid as e: raise Invalid(4, '/cycle') from e
    require((cycle['gameTurn'], cycle['operationStage'], cycle['ordinal'], cycle['playerPhaseSlot']) ==
            (1, 1, 1, 'first-acting-side'), 3, '/cycle')
    require(cycle['openedAuthorityVersion'] <= b['priorVersion'] < 2**63, 4, '/priorVersion')
    position = next(p for p in seq.expected_catalog()['positions'] if p['positionId'] == edge(b)['combatPositionIds'][0])
    position = copy.deepcopy(position); position['activeSide'] = cycle['actingSide']
    require(b['position'] == position and b['breakdownFlow'] == {'kind': 'idle'} and b['reactionWindow'] is None, 4)
    expected = env.initial_world(q, c)
    # Bounded initial-infantry current facts: certified initial geometry and ordinary CP0..10.
    # Their actual Movement history must be certified by caller; no history repair occurs here.
    for actual, initial in zip(b['world']['elements'], expected['elements']):
        cp = actual['operationalState']['capabilityPointsExpended']
        require(cp['denominator'] == 1 and 0 <= cp['numerator'] <= 10, 4)
        require(actual['currentLocationId'] == initial['currentLocationId'], 4)
        initial['operationalState']['capabilityPointsExpended'] = copy.deepcopy(cp)
    for r in expected['representations']:
        r['currentLocationId'] = next(e['currentLocationId'] for e in expected['elements'] if r['boundElementIds'] == [e['elementId']])
    require(b['world'] == expected, 4, '/world')
    require(b['randomState']['contractVersion'] == 1 and b['randomState']['algorithmId'] == 'sandtable.sha256-counter.v1', 4)
    weather = b['weather']
    require((weather['gameTurn'], weather['operationStage']) == (cycle['gameTurn'], cycle['operationStage']), 4)
    require(all(weather[k] in ('normal', 'hot', 'sandstorm', 'rainstorm') for k in ('attackerKind', 'defenderKind')), 3)
    require(b['randomState']['seed'] == q['randomState']['seed'], 4)
    return b


def edge(b):
    return next(x for x in seq.expected_catalog()['cycles'] if x['operationStage'] == b['cycle']['operationStage']
                and x['playerPhaseSlot'] == b['cycle']['playerPhaseSlot'])


def candidate(b):
    if b['weather']['attackerKind'] != 'normal' or b['weather']['defenderKind'] != 'normal': return None
    side = b['cycle']['actingSide']; other = 'commonwealth' if side == 'axis' else 'axis'
    def participant(owner):
        e = next(e for e in b['world']['elements'] if world.SIDES[e['elementId']] == owner)
        r = next(r for r in b['world']['representations'] if r['boundElementIds'] == [e['elementId']])
        return e, dict(unit=dict(creationBinding=b['creationBinding'], originalSide=owner, elementId=e['elementId']),
                       representationId=r['representationId'], locationId=e['currentLocationId'],
                       componentIds=[x['componentId'] for x in e['components']])
    attacker, a = participant(side); defender, d = participant(other)
    if (defender['currentLocationId'] not in world.GRAPH[attacker['currentLocationId']]
        or attacker['operationalState']['capabilityPointsExpended']['numerator'] > 5
        or defender['operationalState']['capabilityPointsExpended']['numerator'] > 7): return None
    return dict(attacker=a, defender=d, targetLocationId=d['locationId'], basis='voluntary-adjacent')


def initial(b):
    return dict(contractVersion=1, boundaryHash=sha(encode(b)), segmentId='seg.'+digest(INVENTORY['domains']['segment'], encode(b)),
        stateVersion=b['priorVersion'], prefix=b['priorPrefix'], stepIndex=0, selectionOutcome='unopened', selection=None,
        selectionReceiptId=None, declineReceiptId=None, cancellationReceiptId=None, selectionWindow=None, rbaWindow=None,
        stepReceipts=[], receipts=[], closed=False)


def command(state, kind, **kwargs):
    value = dict(contractVersion=1, kind=kind, segmentId=state['segmentId'], decisionId=None, fromPositionId=None,
                 expectedPriorVersion=None, choice=None, candidate=None, participant=None)
    value.update(kwargs); return value


def trusted(cmd, actor='system', now=None, available=True):
    return dict(command=cmd, actor=actor, admittedAt=now, clockAvailable=available)


def window(state, b, kind, now):
    try: timing = inputs.make_timing(env.Context().config, kind, now)
    except inputs.Invalid as e: raise Invalid(5, '/admittedAt') from e
    side = b['cycle']['actingSide']; owner = side if kind == 'selection' else ('commonwealth' if side == 'axis' else 'axis')
    return dict(decisionId=state['segmentId']+'.'+kind, owner=owner, timing=timing)


def active_window(s):
    if s['selectionOutcome'] == 'pending': return s['selectionWindow']
    if s['selectionOutcome'] == 'selected' and s['rbaWindow'] is not None and s['declineReceiptId'] is None: return s['rbaWindow']
    return None


def admitted_timing(w, inp):
    t = copy.deepcopy(w['timing']); now = inp['admittedAt']
    if now is not None: t['highWaterUnixMilliseconds'] = max(now, t['highWaterUnixMilliseconds'])
    return t


def transition(b, prior, inp):
    """Immutable contract transition from independently trusted boundary + authenticated input."""
    typed(inp, 'Input'); cmd = inp['command']; kind = cmd['kind']; actor = inp['actor']
    require(cmd['contractVersion'] == 1 and kind in KINDS, 3)
    require(cmd['segmentId'] == prior['segmentId'], 4)
    allowed = {'open-segment': {'expectedPriorVersion'}, 'close-empty-selection': {'expectedPriorVersion'},
        'choose-selection': {'decisionId', 'choice', 'candidate'}, 'decline-rba': {'decisionId', 'participant'},
        'complete-step': {'fromPositionId', 'expectedPriorVersion'}, 'open-rba': {'fromPositionId', 'expectedPriorVersion'},
        'expire-window': {'decisionId'}, 'controller-unavailable': {'decisionId'}}[kind]
    for k in ('decisionId', 'fromPositionId', 'expectedPriorVersion', 'choice', 'candidate', 'participant'):
        require(k in allowed or cmd[k] is None, 3, '/command/'+k)
    system = kind not in ('choose-selection', 'decline-rba')
    require(actor == 'system' if system else actor in ('axis', 'commonwealth'), 4, '/actor')
    ch = sha(encode(canonical(cmd, 'Command')))
    duplicate = next((r for r in prior['receipts'] if r['commandHash'] == ch), None)
    if duplicate:
        require(duplicate['actor'] == actor, 4, '/actor')
        return copy.deepcopy(prior), None, duplicate['receiptId']
    w = active_window(prior)
    before_rba_unavailable = (kind == 'controller-unavailable' and prior['selectionOutcome'] == 'selected'
        and prior['stepIndex'] == 2 and prior['rbaWindow'] is None and cmd['decisionId'] == prior['segmentId']+'.rba')
    if not before_rba_unavailable and kind in ('expire-window', 'controller-unavailable') and (w is None or cmd['decisionId'] != w['decisionId']):
        return copy.deepcopy(prior), None, None
    require(not prior['closed'], 6)
    require(len(prior['receipts']) < 16 and prior['stateVersion'] < 2**63-1, 6)
    s = copy.deepcopy(prior); choices = candidate(b); step = s['stepIndex']; positions = edge(b)['combatPositionIds']
    if kind in ('open-segment', 'close-empty-selection', 'complete-step', 'open-rba'):
        require(cmd['expectedPriorVersion'] == prior['stateVersion'], 6)
    if kind in ('complete-step', 'open-rba'): require(cmd['fromPositionId'] == positions[step], 6)
    if kind in ('complete-step', 'close-empty-selection'):
        require(inp['admittedAt'] is None and inp['clockAvailable'], 5)
    if kind == 'open-segment':
        require(s['selectionOutcome'] == 'unopened' and not s['receipts'], 6)
        require(inp['admittedAt'] is not None if choices and inp['clockAvailable'] else True, 5)
        require(choices is not None or inp['admittedAt'] is None, 5)
        opens_window = choices is not None and inp['clockAvailable']
        s['selectionOutcome'] = 'pending' if opens_window else 'system-no-selection'
        s['selectionWindow'] = window(s, b, 'selection', inp['admittedAt']) if opens_window else None
        effect = dict(kind='segment-opened', boundaryHash=s['boundaryHash'], window=s['selectionWindow'])
    elif kind == 'close-empty-selection':
        require(s['selectionOutcome'] == 'system-no-selection' and s['selectionWindow'] is None, 6)
        effect = dict(kind='selection-closed', outcome='no-selection', candidate=None, timing=None)
    elif kind == 'choose-selection':
        require(s['selectionOutcome'] == 'pending' and w is not None and cmd['decisionId'] == w['decisionId'], 6)
        require(actor == w['owner'], 4)
        require(inputs.clock_gate(w['timing'], inp['admittedAt'], inp['clockAvailable']) == 'before-deadline', 5)
        require(cmd['choice'] in ('select-close-assault', 'finish-without-attack'), 3)
        chosen = cmd['choice'] == 'select-close-assault'
        require(cmd['candidate'] == choices if chosen else cmd['candidate'] is None, 4)
        effect = dict(kind='selection-closed', outcome='selected' if chosen else 'no-selection', candidate=choices if chosen else None,
                      timing=admitted_timing(w, inp))
    elif kind == 'open-rba':
        require(step == 2 and s['selectionOutcome'] == 'selected' and s['rbaWindow'] is None, 6)
        require(inp['clockAvailable'] and inp['admittedAt'] is not None, 5)
        require(inp['admittedAt'] >= s['selectionWindow']['timing']['highWaterUnixMilliseconds'], 5)
        s['rbaWindow'] = window(s, b, 'rba', inp['admittedAt'])
        effect = dict(kind='rba-opened', selectionReceiptId=s['selectionReceiptId'], window=s['rbaWindow'])
    elif kind == 'decline-rba':
        require(step == 2 and s['selectionOutcome'] == 'selected' and w is not None and cmd['decisionId'] == w['decisionId'], 6)
        require(actor == w['owner'] and cmd['participant'] == s['selection']['defender']['unit'], 4)
        require(inputs.clock_gate(w['timing'], inp['admittedAt'], inp['clockAvailable']) == 'before-deadline', 5)
        effect = dict(kind='rba-declined', selectionReceiptId=s['selectionReceiptId'], participant=cmd['participant'], timing=admitted_timing(w, inp))
    elif kind in ('expire-window', 'controller-unavailable'):
        if before_rba_unavailable:
            effect = dict(kind='selection-cancelled', selectionReceiptId=s['selectionReceiptId'], timing=None)
        else:
            require(w is not None, 6)
            gate = inputs.clock_gate(w['timing'], inp['admittedAt'], inp['clockAvailable'])
            if kind == 'expire-window' and gate == 'before-deadline': return copy.deepcopy(prior), None, None
            if s['selectionOutcome'] == 'pending':
                effect = dict(kind='selection-closed', outcome='no-selection', candidate=None, timing=admitted_timing(w, inp))
            else: effect = dict(kind='selection-cancelled', selectionReceiptId=s['selectionReceiptId'], timing=admitted_timing(w, inp))
    else:
        require(s['selectionOutcome'] in ('selected', 'no-selection', 'cancelled'), 6)
        no_attack = s['selectionOutcome'] != 'selected'
        require(no_attack or step < 3, 7)  # Prepared/committed gates belong to C3b/c.
        if not no_attack and step == 2: require(s['declineReceiptId'] is not None, 6)
        disposition = s['cancellationReceiptId'] or (s['declineReceiptId'] if step >= 2 else None) or s['selectionReceiptId']
        effect = dict(kind='step-completed', fromPositionId=positions[step],
            toPositionId=positions[step+1] if step < 5 else edge(b)['releasePositionId'],
            previousStepReceiptId=s['stepReceipts'][-1] if s['stepReceipts'] else s['receipts'][0]['receiptId'],
            dispositionReceiptId=disposition, proofKind='no-attack' if no_attack else ['no-gun-positions', 'no-barrage-work', 'accepted-decline'][step])
    event = dict(contractVersion=1, eventType='combat-'+effect['kind'], campaignId=b['cycle']['campaignId'], rulesetHash=b['cycle']['rulesetHash'],
        configurationHash=b['cycle']['admittedPolicyBundleDigest'], cycleId=sha(seq.identity(b['cycle'], 'Authority', b['firstActingSide'])),
        segmentId=s['segmentId'], priorVersion=prior['stateVersion'], stateVersion=prior['stateVersion']+1, priorPrefix=prior['prefix'],
        input=canonical(inp, 'Input'), effect=effect)
    rid = 'cmb.'+digest(INVENTORY['domains']['receipt'], encode(event)); event['receiptId'] = rid
    raw = encode(canonical(event, 'Event')); require(len(raw) <= 1048576, 1)
    if effect['kind'] == 'selection-closed':
        s.update(selectionOutcome=effect['outcome'], selection=effect['candidate'], selectionReceiptId=rid)
        if s['selectionWindow'] is not None: s['selectionWindow']['timing'] = effect['timing']
    elif effect['kind'] == 'rba-declined': s['declineReceiptId'] = rid; s['rbaWindow']['timing'] = effect['timing']
    elif effect['kind'] == 'selection-cancelled':
        s.update(selectionOutcome='cancelled', cancellationReceiptId=rid)
        if s['rbaWindow'] is not None: s['rbaWindow']['timing'] = effect['timing']
    elif effect['kind'] == 'step-completed':
        s['stepReceipts'].append(rid); s['stepIndex'] += 1; s['closed'] = s['stepIndex'] == 6
    s['stateVersion'] = event['stateVersion']; s['prefix'] = seq.prefix_event(prior['prefix'], raw)
    s['receipts'].append(dict(commandHash=ch, eventHash=sha(raw), receiptId=rid, actor=actor, stateVersion=s['stateVersion']))
    typed(s, 'Control'); require(len(encode(s)) <= 1048576, 1)
    return s, raw, rid


def read_event(raw, b, prior, trusted_input):
    parse(raw, 'Event')
    s, expected, rid = transition(b, prior, trusted_input)
    require(expected is not None and raw == expected, 6)
    return s


def replay(case, length=None):
    require(length is None or (type(length) is int and 0 <= length <= len(case['events'])), 2)
    b = boundary(encode(case['boundary'])); s = initial(b)
    events = case['events'] if length is None else case['events'][:length]
    require(len(events) <= 16 and len(case['events']) == len(case['inputs']), 1)
    for g, inp in zip(events, case['inputs']): s = read_event(g['canonicalUtf8'].encode(), b, s, inp)
    return s


def read_control(raw, case, length):
    parse(raw, 'Control'); expected = replay(case, length)
    require(raw == encode(expected), 6)
    return expected


def rejected(call, code=None):
    try: call()
    except Invalid as e:
        if code is not None: assert e.code == f'CMB-STP-{code:03}', (e.code, code, e.path)
    else: raise AssertionError('invalid step contract accepted')


def main():
    f = json.loads(FIXTURE.read_text())
    for case in f['cases']:
        bg = f['boundaries'][case['boundaryIndex']]; raw = bg['canonicalUtf8'].encode()
        assert len(raw) == bg['byteCount'] and sha(raw) == bg['sha256']
        case['boundary'] = json.loads(raw)
    cuts = 0; mutations = 0; raw_checks = 0
    for case in f['cases']:
        b = boundary(encode(case['boundary'])); s = initial(b); frozen = encode(b)
        for index, (g, inp) in enumerate(zip(case['events'], case['inputs'])):
            raw = g['canonicalUtf8'].encode(); assert sha(raw) == g['sha256'] and len(raw) == g['byteCount']
            before = copy.deepcopy(s); s = read_event(raw, b, s, inp)
            assert read_control(encode(s), case, index+1) == s; cuts += 1
            same, emitted, receipt = transition(b, s, inp)
            assert same == s and emitted is None and receipt == json.loads(raw)['receiptId']
            for key, replacement in [('priorVersion', 0), ('stateVersion', 999), ('priorPrefix', 'sha256:'+'0'*64),
                                     ('segmentId', 'other'), ('receiptId', 'other'), ('configurationHash', 'sha256:'+'0'*64)]:
                v = json.loads(raw); v[key] = replacement
                rejected(lambda: read_event(encode(v), b, before, inp)); mutations += 1
            for bad in (raw+b'\n', raw[:-1], b'\xef\xbb\xbf'+raw, raw.replace(b'"contractVersion":1', b'"contractVersion":1.0', 1)):
                rejected(lambda: read_event(bad, b, before, inp)); raw_checks += 1
        golden = case['controlGolden']; control_raw = golden['canonicalUtf8'].encode()
        assert len(control_raw) == golden['byteCount'] and sha(control_raw) == golden['sha256']
        assert read_control(control_raw, case, len(case['events'])) == s
        e = case['expected']; assert s['stateVersion'] == e['stateVersion'] and s['prefix'] == e['prefix']
        assert len(s['stepReceipts']) == e['stepCount'] and s['closed'] == (e['terminal'] == 'no-attack')
        assert encode(b) == frozen
        if not s['closed']:
            cmd = command(s, 'complete-step', fromPositionId=edge(b)['combatPositionIds'][3], expectedPriorVersion=s['stateVersion'])
            rejected(lambda: transition(b, s, trusted(cmd)), 7)
        # Old selection wakeups cannot consume the later RBA window or closed state.
        if s['selectionWindow']:
            timer = command(s, 'expire-window', decisionId=s['selectionWindow']['decisionId'])
            result, emitted, _ = transition(b, s, trusted(timer, now=999999)); assert result == s and emitted is None
    c = f['cases'][3]; b = boundary(encode(c['boundary'])); s = replay(c, 1); pick = copy.deepcopy(c['inputs'][1])
    for now in (30999,):
        accepted = copy.deepcopy(pick); accepted['admittedAt'] = now
        assert transition(b, s, accepted)[0]['selectionOutcome'] == 'selected'
    for now in (31000, 31001, 999):
        late = copy.deepcopy(pick); late['admittedAt'] = now
        rejected(lambda: transition(b, s, late), 5)
    wrong = copy.deepcopy(pick); wrong['actor'] = 'commonwealth'
    rejected(lambda: transition(b, s, wrong), 4)
    conflict = copy.deepcopy(pick); conflict['command']['choice'] = 'finish-without-attack'; conflict['command']['candidate'] = None
    chosen = transition(b, s, pick)[0]; rejected(lambda: transition(b, chosen, conflict), 6)
    # Regression/unavailability cancels a live wait without a synthetic decline or resource effect.
    rba = replay(c, 5)
    for now, available in [(1999, True), (None, False), (2001, True)]:
        cmd = command(rba, 'controller-unavailable', decisionId=rba['rbaWindow']['decisionId'])
        cancelled, raw, _ = transition(b, rba, trusted(cmd, now=now, available=available))
        assert cancelled['selectionOutcome'] == 'cancelled' and cancelled['declineReceiptId'] is None
        assert cancelled['rbaWindow']['timing']['deadlineUnixMilliseconds'] == 32000
        assert cancelled['rbaWindow']['timing']['highWaterUnixMilliseconds'] >= 2000
    pre_rba = replay(c, 4)
    backwards_open = command(pre_rba, 'open-rba', fromPositionId=edge(b)['combatPositionIds'][2], expectedPriorVersion=pre_rba['stateVersion'])
    rejected(lambda: transition(b, pre_rba, trusted(backwards_open, now=999)), 5)
    cancel_cmd = command(pre_rba, 'controller-unavailable', decisionId=pre_rba['segmentId']+'.rba')
    cancelled = transition(b, pre_rba, trusted(cancel_cmd, available=False))[0]
    assert cancelled['selectionOutcome'] == 'cancelled' and cancelled['rbaWindow'] is None and cancelled['declineReceiptId'] is None
    no_clock = initial(b); open_cmd = command(no_clock, 'open-segment', expectedPriorVersion=no_clock['stateVersion'])
    no_clock = transition(b, no_clock, trusted(open_cmd, available=False))[0]
    assert no_clock['selectionWindow'] is None and no_clock['selectionOutcome'] == 'system-no-selection'
    close_cmd = command(no_clock, 'close-empty-selection', expectedPriorVersion=no_clock['stateVersion'])
    assert transition(b, no_clock, trusted(close_cmd))[0]['selectionOutcome'] == 'no-selection'
    declined = replay(c, 6); timer = command(declined, 'expire-window', decisionId=declined['rbaWindow']['decisionId'])
    assert transition(b, declined, trusted(timer, now=32000))[1] is None
    for kind in ('hot', 'sandstorm', 'rainstorm'):
        nonnormal = copy.deepcopy(b); nonnormal['weather']['attackerKind'] = kind
        nonnormal = boundary(encode(nonnormal)); assert candidate(nonnormal) is None
        start = initial(nonnormal); cmd = command(start, 'open-segment', expectedPriorVersion=start['stateVersion'])
        opened = transition(nonnormal, start, trusted(cmd))[0]
        assert opened['selectionWindow'] is None and opened['selectionOutcome'] == 'system-no-selection'
    moved = copy.deepcopy(b); moved['world']['elements'][0]['currentLocationId'] = 'axis-supply'
    rejected(lambda: boundary(encode(moved)), 4)
    forged = copy.deepcopy(c); forged['boundary']['reactionWindow'] = {}; rejected(lambda: replay(forged), 1)
    wrong_candidate = copy.deepcopy(pick); wrong_candidate['command']['candidate']['targetLocationId'] = 'axis-supply'
    rejected(lambda: transition(b, s, wrong_candidate), 4)
    wrong_decline = copy.deepcopy(c['inputs'][5]); wrong_decline['actor'] = 'axis'
    rejected(lambda: transition(b, replay(c, 5), wrong_decline), 4)
    skipped = copy.deepcopy(c); skipped['events'][2] = skipped['events'][3]
    rejected(lambda: replay(skipped), 6)
    rejected(lambda: replay(c, -1), 2)
    rejected(lambda: replay(c, len(c['events'])+1), 2)
    unknown_effect = json.loads(c['events'][0]['canonicalUtf8']); unknown_effect['effect']['kind'] = {}
    rejected(lambda: parse(encode(unknown_effect), 'Event'), 3)
    # Frozen boundary/world substitution cannot be laundered by retaining an otherwise valid event.
    changed_boundary = copy.deepcopy(b); changed_boundary['priorPrefix'] = 'sha256:'+'1'*64
    changed_boundary = boundary(encode(changed_boundary))
    rejected(lambda: read_event(c['events'][0]['canonicalUtf8'].encode(), changed_boundary, initial(changed_boundary), c['inputs'][0]))
    forged = copy.deepcopy(c); forged['events'][2], forged['events'][3] = forged['events'][3], forged['events'][2]
    rejected(lambda: replay(forged), 6)
    forged = copy.deepcopy(replay(c, 6)); forged['stepReceipts'] = []; rejected(lambda: read_control(encode(forged), c, 6), 6)
    print(f'PASS: {len(f["cases"])} literal traces; {cuts} event/control cuts; {mutations} event mutations; '
          f'{raw_checks} raw rejections; exact retries, deadline/regression/unavailability, RBA races and FA gate. '
          'Isolated boundary probes only; no full campaign replay.')


if __name__ == '__main__': main()
