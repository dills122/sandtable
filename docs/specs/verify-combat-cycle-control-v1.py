#!/usr/bin/env python3
"""Bounded D2b.2 oracle. Private control, synthetic inherited Movement evidence."""
import copy
import importlib.util
import json
import sys
from functools import lru_cache
from pathlib import Path
sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent

def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    out = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(out)
    return out

rel = module('release', ROOT/'verify-combat-reserve-release-v1.py')
mov = module('movement', ROOT/'verify-combat-ordinary-movement-v1.py')
r, steps, world = rel.r, rel.steps, rel.world
encode, sha = rel.encode, rel.sha
INVENTORY = json.loads((ROOT/'combat-cycle-control-v1.schema.json').read_text())
FIXTURE = ROOT/'fixtures/combat-cycle-control-v1.json'
SCHEMA = rel.SCHEMA | mov.SCHEMA | {k: [tuple(f.split(':')) for f in v.split()] for k, v in INVENTORY['objects'].items()}

class Invalid(ValueError):
    def __init__(self, code):
        self.code = f'CMB-CYC-{code:03}'
        super().__init__(self.code)

def require(ok, code):
    if not ok:
        raise Invalid(code)

def typed(v, kind, depth=0):
    require(depth <= 32, 1)
    if kind.endswith('?'):
        if v is not None: typed(v, kind[:-1], depth)
    elif kind in SCHEMA:
        require(type(v) is dict and set(v) == {k for k, _ in SCHEMA[kind]}, 1)
        for k, t in SCHEMA[kind]: typed(v[k], t, depth+1)
    elif kind.endswith('[]'):
        require(type(v) is list and len(v) <= 512, 1)
        for x in v: typed(x, kind[:-2], depth+1)
    else:
        try: rel.typed(v, kind, depth)
        except rel.Invalid as e: raise Invalid(int(e.code[-3:])) from e

def canonical(v, kind):
    if kind.endswith('?'): return None if v is None else canonical(v, kind[:-1])
    if kind in SCHEMA: return {k: canonical(v[k], t) for k, t in SCHEMA[kind]}
    if kind.endswith('[]'): return [canonical(x, kind[:-2]) for x in v]
    return rel.canonical(v, kind)

def raw(v, kind):
    typed(v, kind); data = encode(canonical(v, kind)); require(len(data) <= 1048576, 1)
    return data

def parse(data, kind):
    require(type(data) is bytes and 0 < len(data) <= 1048576, 1)
    def pairs(items):
        out = {}
        for k, v in items:
            require(k not in out, 1); out[k] = v
        return out
    try: value = json.loads(data.decode('utf8'), object_pairs_hook=pairs, parse_constant=lambda _: (_ for _ in ()).throw(ValueError()))
    except (ValueError, UnicodeError, RecursionError) as e: raise Invalid(1) from e
    require(raw(value, kind) == data, 8)
    return value

def edge(c):
    return next(e for e in steps.seq.expected_catalog()['cycles'] if e['operationStage'] == c['operationStage'] and e['playerPhaseSlot'] == c['playerPhaseSlot'])

def cycle_id(c, first):
    try: return sha(steps.seq.identity(c, 'Authority', first))
    except steps.seq.Invalid as error: raise Invalid(4) from error
def control_id(b): return 'ctl.'+steps.digest(INVENTORY['domains']['control'], raw(b, 'CycleControlBase'))
def locations(w): return sorted([dict(unit=world.unit(e, w['creationBinding']), locationId=e['currentLocationId']) for e in w['elements']], key=lambda x: rel.key(x['unit']))
def distance(a, b): return len(world.init.content.route(world.GRAPH, a, b))-1

def excluded(proof, side):
    old = {rel.key(x) for x in proof['excludedBefore']}
    enemies = [p['locationId'] for p in proof['endLocations'] if p['unit']['originalSide'] != side]
    return sorted([copy.deepcopy(p['unit']) for p in proof['endLocations'] if p['unit']['originalSide'] == side and
        (rel.key(p['unit']) in old or not any(distance(p['locationId'], e) <= 2 for e in enemies))], key=rel.key)

def validate_proof(p, c, units, ordinal):
    typed(p, 'MovementEndProof'); require(p['scope'] == rel.scope(c) and p['ordinal'] == ordinal, 4)
    keys = [rel.key(x['unit']) for x in p['endLocations']]
    require(keys == sorted(set(keys)) == sorted(rel.key(u) for u in units), 4)
    require(all(x['locationId'] in world.GRAPH for x in p['endLocations']), 3)
    old = [rel.key(u) for u in p['excludedBefore']]
    require(old == sorted(set(old)) and all(k in keys and k[1] == c['actingSide'] for k in old), 4)

def release_state(b):
    rb = b['releaseBase']; ins, events = b['releaseInputs'], b['releaseEvents']
    try: rel.validate_base(rb)
    except rel.Invalid as error: raise Invalid(4) from error
    require(len(ins) == len(events) <= 34, 1)
    s = rel.initial(rb)
    try:
        for inp, event in zip(ins, events): s = rel.read_event(rel.raw(event, 'ReleaseEvent'), rb, s, inp)
    except rel.Invalid as e: raise Invalid(6) from e
    require(s['status'] == 'completed' and not s['pending'], 5)
    return s

def progress_refs(b, released):
    # b.progress is an export from the independently replayed C3b source, bound by read_base.
    refs = copy.deepcopy(b['progress'])
    require(all(p['eventType'] == 'combat-attack-committed' for p in refs), 3)
    for event in b['releaseEvents']:
        eff = event['effect']
        if eff['kind'] == 'unit-disposition' and eff['beforeStatus'] != eff['afterStatus']:
            refs.append(dict(eventType=event['eventType'], receiptId=event['receiptId'], eventHash=sha(rel.raw(event, 'ReleaseEvent'))))
    require(len({x['receiptId'] for x in refs}) == len(refs), 6)
    return refs

def assess(b):
    typed(b, 'CycleControlBase'); require(b['contractVersion'] == 1 and b['profile'] in ('settled-control', 'isolated-release-control'), 3)
    s = release_state(b); rb = b['releaseBase']; c = rb['cycle']; w = b['world']; side = c['actingSide']
    require(sha(r.raw(w, 'World')) == rb['retainedWorldHash'] and w['creationBinding'] == r.env.binding(rel.CONTEXT.request()), 4)
    units = [world.unit(e, w['creationBinding']) for e in w['elements']]
    validate_proof(b['movementEnd'], c, units, c['ordinal'])
    require({e['elementId'] for e in w['elements']} == set(world.SIDES), 3)
    require(not w['brokenVehicleLots'] and all(x['relationships'] is not None for x in w['settlements']), 3)
    current = rel.project_world(w, rb, s)
    ineligible = excluded(b['movementEnd'], side); witnesses = []
    for m in s['members']:
        u = m['unit']; e = next(e for e in current['elements'] if e['elementId'] == u['elementId']); op = e['operationalState']
        facts = next(e for e in world.PACK['elements'] if e['elementId'] == u['elementId'])
        require(m['baseCpa'] == facts['baseCapabilityPointAllowance'] == 10 and op['vehicleBreakdownState'] is None and op['movementEnded'] is None, 3)
        require((op['ledgerGameTurn'], op['ledgerOperationStage']) == (c['gameTurn'], c['operationStage']), 4)
        require(op['capabilityPointsExpended'] == m['spentCp'] and m['spentCp']['denominator'] == 1, 4)
        require(e['ammunition']['points'] == 0, 3)  # Otherwise a Combat adapter is required, never infer false.
        require(e['components'][0]['currentToe'] > 0, 3)
        reps = [x for x in current['representations'] if x['bindingKind'] == 'independent-element' and x['boundElementIds'] == [u['elementId']]]
        require(len(reps) == 1 and reps[0]['currentLocationId'] == e['currentLocationId'], 4)
        h = m['history']; ex = h['nextMovement']; exceptional = False
        if ex is not None:
            require(ex['scope'] == rel.scope(c) and ex['ordinal'] == h['releaseCycle']+1, 4)
            if ex['status'] == 'pending':
                require(h['releaseCycle'] == c['ordinal'] and ex['ordinal'] == c['ordinal']+1 and ex['completionReceiptId'] is None, 4); exceptional = True
            else: require(ex['status'] == 'expired' and ex['completionReceiptId'] is not None, 4)
        if m['status'] != 'none' or u in ineligible and not exceptional: continue
        try: ends = mov.affected(current['relationships'], u)
        except mov.Invalid as error: raise Invalid(3) from error
        require(all((x['gameTurn'], x['operationStage']) == (c['gameTurn'], c['operationStage']) for x in current['relationships'] if x['active']), 4)
        origin = e['currentLocationId']
        for dest in sorted(world.GRAPH[origin]):
            if any(x['currentLocationId'] == dest for x in current['elements'] if x is not e) or any(x['currentLocationId'] == dest for x in current['guards']): continue
            try: terrain = mov.terrain_cost(origin, dest)
            except mov.Invalid as error: raise Invalid(3) from error
            try: after, dp = mov.cost(10, m['spentCp']['numerator'], [x['kind'] for x in ends], h['releasedType'], terrain)
            except mov.Invalid as error:
                if error.code == 'CMB-MOV-005': continue
                raise Invalid(3) from error
            require(op['cohesionLevel']-dp >= -2**31, 7)
            witnesses.append(dict(kind='movement', unit=copy.deepcopy(u), destinationLocationId=dest, terrainCost=terrain,
                breakOffCost=after-m['spentCp']['numerator']-terrain, afterCp=after, excessCpDp=dp, usesReleaseException=exceptional))
    return dict(releaseCompletionReceiptId=s['completionReceiptId'], movementCompletionReceiptId=b['movementEnd']['completionReceiptId'],
        progress=progress_refs(b, s), witnesses=witnesses, combatAssessment='no-own-ammunition')

def control_mode(a):
    if not a['witnesses']: return 'no-continuation'
    if not a['progress']: return 'no-material-progress'
    return 'owner-choice'

@lru_cache(maxsize=64)
def source(name, side):
    case = copy.deepcopy(next(x for x in json.loads(r.FIXTURE.read_text())['cases'] if x['name'] == name)); case['attackerSide'] = side
    return r.trace_case(case)

def base_for(case, side='axis', slot='first-acting-side'):
    settled = case.get('resultCase'); status = case.get('reserve', 'none'); ordinal = case.get('ordinal', 1)
    rc = dict(name=case['name'], ordinal=ordinal, statuses=[status])
    if settled: rc['settledCase'] = settled
    if not settled: rc['spent'] = [case.get('spent', 0)]
    rb = rel.base_for(rc, side, slot); progress = []; target = []
    if 'priorVersion' in case: rb['priorVersion'] = case['priorVersion']
    if settled:
        require(slot == 'first-acting-side', 3)
        ctx, final, _, _ = source(settled, side); w = copy.deepcopy(final['world']); target = copy.deepcopy(ctx['committed']['targetUses'])
        for event in ctx['roundEvents']:
            obj = json.loads(event)
            if obj['effect']['kind'] == 'attack-committed' and obj['effect']['commitmentId'] == ctx['committed']['commitmentId']:
                progress.append(dict(eventType=obj['eventType'], receiptId=obj['receiptId'], eventHash=sha(event)))
        require(len(progress) == 1, 4)
        end = locations(ctx['base']['boundary']['world'])
    else:
        w = r.env.initial_world(rel.CONTEXT.request(), rel.CONTEXT)
        for e in w['elements']:
            e['ammunition']['points'] = case.get('ammo', 0)
            if world.SIDES[e['elementId']] == side:
                e['reserveStatus'] = status; e['operationalState']['capabilityPointsExpended'] = world.cp(case.get('spent', 0))
        rb['retainedWorldHash'] = sha(r.raw(w, 'World')); end = locations(w)
    own = next(x['unit'] for x in end if x['unit']['originalSide'] == side)
    if case.get('farEnd'):
        for item in end: item['locationId'] = world.ANCHORS[item['unit']['originalSide']]
    proof = dict(scope=rel.scope(rb['cycle']), ordinal=ordinal, completionReceiptId='probe.movement-completed', endLocations=end,
        excludedBefore=[copy.deepcopy(own)] if case.get('excludedBefore') else [])
    rs = rel.initial(rb); ins = []; events = []; now = (rb['acceptedHighWater'] or 1000)+1000
    def send(kind, choice=None):
        nonlocal rs
        inp = rel.trusted(rel.command(rs, kind, choice), side if kind == 'choose' else 'system', now)
        rs, data, _ = rel.transition(rb, rs, inp); ins.append(inp); events.append(json.loads(data))
    send('open')
    if status != 'none': send('choose', case['releaseChoice'])
    send('complete')
    return dict(contractVersion=1, profile='settled-control' if settled else 'isolated-release-control', releaseBase=rb,
        releaseInputs=ins, releaseEvents=events, world=w, movementEnd=proof, progress=progress, targetUses=target)

def read_base(data, case, side='axis', slot='first-acting-side'):
    b = parse(data, 'CycleControlBase'); expected = base_for(case, side, slot)
    require(data == raw(expected, 'CycleControlBase'), 4); assess(expected)
    return expected

def initial(b):
    s = release_state(b); c = b['releaseBase']['cycle']
    return dict(contractVersion=1, baseHash=sha(raw(b, 'CycleControlBase')), controlId=control_id(b), stateVersion=s['stateVersion'], prefix=s['prefix'],
        status='unopened', positionId=b['releaseBase']['positionId'], activeCycle=copy.deepcopy(c), closure=None, decisionId=None, timing=None,
        openingClockFailure=False, acceptedHighWater=s['acceptedHighWater'], assessment=None, members=copy.deepcopy(s['members']),
        world=rel.project_world(b['world'], b['releaseBase'], s), randomState=copy.deepcopy(s['randomState']), attackHistory=copy.deepcopy(s['attackHistory']),
        targetUses=copy.deepcopy(b['targetUses']), nextCycleProgress=[], receipts=[])

def command(b, s, kind):
    return dict(contractVersion=1, kind=kind, controlId=s['controlId'], cycleId=rel.cycle_id(b['releaseBase']),
        expectedPriorVersion=None if kind in ('expire', 'unavailable') else s['stateVersion'], decisionId=None if kind == 'open' else s['decisionId'])

def trusted(cmd, actor='system', now=None, available=True): return dict(command=cmd, actor=actor, admittedAt=now, clockAvailable=available)

def transition(b, prior, inp):
    typed(prior, 'CycleControlState'); typed(inp, 'CycleControlInput'); rb = b['releaseBase']; c = rb['cycle']; cmd = inp['command']; kind = cmd['kind']; actor = inp['actor']
    require(prior['contractVersion'] == cmd['contractVersion'] == 1 and kind in ('open', 'repeat', 'finish', 'expire', 'unavailable', 'fallback-step'), 3)
    require(prior['baseHash'] == sha(raw(b, 'CycleControlBase')) and cmd['controlId'] == prior['controlId'] == control_id(b) and cmd['cycleId'] == rel.cycle_id(rb), 4)
    require(actor == c['actingSide'] if kind in ('repeat', 'finish') else actor == 'system', 4)
    require((cmd['expectedPriorVersion'] is None) == (kind in ('expire', 'unavailable')), 3)
    require(kind != 'open' or cmd['decisionId'] is None, 3)
    ch = sha(raw(cmd, 'CycleControlCommand')); receipt = next((x for x in prior['receipts'] if x['commandHash'] == ch and x['actor'] == actor), None)
    if receipt: return prior, None, receipt['receiptId']
    if kind in ('expire', 'unavailable') and (prior['status'] != 'open' or cmd['decisionId'] != prior['decisionId']): return prior, None, None
    require(prior['status'] in ('unopened', 'open') and len(prior['receipts']) < 2 and prior['stateVersion'] < 2**63-1, 7)
    if kind not in ('expire', 'unavailable'): require(cmd['expectedPriorVersion'] == prior['stateVersion'], 6)
    if kind != 'open': require(cmd['decisionId'] == prior['decisionId'], 6)
    s = copy.deepcopy(prior); now = inp['admittedAt']; author = actor; reason = 'owner-choice'; effkind = None; nxt = None
    if kind == 'open':
        require(prior['status'] == 'unopened', 6); s['assessment'] = assess(b); mode = control_mode(s['assessment'])
        if mode != 'owner-choice': effkind = 'phase-finished'; reason = mode
        else:
            require(c['ordinal'] < rel.MAX and prior['stateVersion'] <= 2**63-3, 7)
            s['decisionId'] = s['controlId']+'.decision'; lost = not inp['clockAvailable'] or now is None or s['acceptedHighWater'] is not None and now < s['acceptedHighWater']
            try: s['timing'] = None if lost else steps.inputs.make_timing(rel.CONTEXT.config, 'cycle-control', now)
            except steps.inputs.Invalid: s['timing'] = None
            s['openingClockFailure'] = s['timing'] is None
            if s['timing'] is not None: s['acceptedHighWater'] = now
            effkind = 'control-opened'; s['status'] = 'open'
    else:
        require(prior['status'] == 'open', 5); t = s['timing']
        lost = s['openingClockFailure'] or not inp['clockAvailable'] or now is None or s['acceptedHighWater'] is not None and now < s['acceptedHighWater']
        late = t is not None and now is not None and now >= t['deadlineUnixMilliseconds']
        if kind == 'fallback-step': require(s['openingClockFailure'], 5)
        if kind == 'expire' and not lost and not late: return prior, None, None
        if kind in ('repeat', 'finish') and not lost: require(not late, 5)
        if not lost:
            s['acceptedHighWater'] = now; s['timing']['highWaterUnixMilliseconds'] = now
        if lost or kind in ('expire', 'unavailable', 'fallback-step'):
            author = 'system'; effkind = 'phase-finished'; reason = 'opening-clock-unavailable' if s['openingClockFailure'] else 'clock-unavailable' if lost else 'deadline' if kind == 'expire' else 'controller-unavailable'
        else: effkind = 'cycle-repeated' if kind == 'repeat' else 'phase-finished'
    successor = rb['positionId']; expired = []
    if effkind == 'cycle-repeated':
        require(control_mode(s['assessment']) == 'owner-choice' and c['ordinal'] < rel.MAX, 7)
        nxt = copy.deepcopy(c); nxt.update(ordinal=c['ordinal']+1, openedAuthorityVersion=prior['stateVersion']+1, openingPrefix=prior['prefix'])
        cycle_id(nxt, rb['firstActingSide']); successor = edge(c)['movementPositionId']; s['status'] = 'repeated'; s['activeCycle'] = nxt; s['targetUses'] = []; s['nextCycleProgress'] = []
    elif effkind == 'phase-finished':
        successor = edge(c)['finishPositionId']; s['status'] = 'finished'; s['activeCycle'] = None
        expired = [copy.deepcopy(m['unit']) for m in s['members'] if m['history']['nextMovement'] is not None and m['history']['nextMovement']['status'] == 'pending']
    eff = dict(kind=effkind, reason=reason, assessment=s['assessment'], timing=s['timing'], openingClockFailure=s['openingClockFailure'], successorPositionId=successor, nextCycle=nxt, expiredUnits=expired)
    event = dict(contractVersion=1, eventType={'control-opened':'movement-combat-control-opened','cycle-repeated':'movement-combat-cycle-repeated','phase-finished':'movement-combat-phase-finished'}[effkind], author=author,
        campaignId=c['campaignId'], rulesetHash=c['rulesetHash'], configurationHash=c['admittedPolicyBundleDigest'], cycleId=cmd['cycleId'], positionId=rb['positionId'],
        baseHash=prior['baseHash'], controlId=prior['controlId'], priorVersion=prior['stateVersion'], stateVersion=prior['stateVersion']+1, priorPrefix=prior['prefix'], input=canonical(inp, 'CycleControlInput'), effect=canonical(eff, 'CycleControlEffect'))
    rid = 'cc.'+steps.digest(INVENTORY['domains']['receipt'], encode(event)); event['receiptId'] = rid; data = raw(event, 'CycleControlEvent')
    for m in s['members']:
        if m['unit'] in expired: m['history']['nextMovement'].update(status='expired', completionReceiptId=rid)
    if effkind != 'control-opened': s['closure'] = dict(cycleId=cmd['cycleId'], ordinal=c['ordinal'], outcome=s['status'], receiptId=rid)
    s['positionId'] = successor; s['stateVersion'] = event['stateVersion']; s['prefix'] = steps.seq.prefix_event(prior['prefix'], data)
    s['receipts'].append(dict(commandHash=ch, eventHash=sha(data), receiptId=rid, actor=actor, stateVersion=s['stateVersion']))
    raw(s, 'CycleControlState'); return s, data, rid

def read_event(data, b, prior, inp):
    parse(data, 'CycleControlEvent'); state, expected, _ = transition(b, prior, inp); require(data == expected, 6); return state

def read_state(data, b, case, inputs, events, length=None, side='axis', slot='first-acting-side'):
    parse(data, 'CycleControlState'); require(len(inputs) == len(events) <= 2, 1)
    require(length is None or type(length) is int and 0 <= length <= len(events), 2)
    read_base(raw(b, 'CycleControlBase'), case, side, slot); s = initial(b)
    for inp, event in zip(inputs[:length], events[:length]): s = read_event(event, b, s, inp)
    require(data == raw(s, 'CycleControlState'), 6); return s

def expire_movement(inp):
    typed(inp, 'MovementExpiryInput'); require(inp['contractVersion'] == 1, 3); c = inp['cycle']; cycle_id(c, inp['firstActingSide']); old = inp['priorProof']
    units = [x['unit'] for x in old['endLocations']]; validate_proof(old, c, units, c['ordinal']-1)
    proof = dict(scope=rel.scope(c), ordinal=c['ordinal'], completionReceiptId=inp['completionReceiptId'], endLocations=copy.deepcopy(inp['endLocations']), excludedBefore=excluded(old, c['actingSide']))
    validate_proof(proof, c, units, c['ordinal']); members = copy.deepcopy(inp['members'])
    require([rel.key(m['unit']) for m in members] == sorted(rel.key(u) for u in units if u['originalSide'] == c['actingSide']), 4)
    for m in members:
        h = m['history']; require(h['scope'] == rel.scope(c), 4); ex = h['nextMovement']
        if ex is not None:
            require(ex['scope'] == rel.scope(c) and ex['ordinal'] == h['releaseCycle']+1, 4)
            if ex['status'] == 'pending':
                require(ex['ordinal'] == c['ordinal'] and ex['completionReceiptId'] is None, 4); ex.update(status='expired', completionReceiptId=inp['completionReceiptId'])
            else: require(ex['status'] == 'expired' and ex['completionReceiptId'] is not None and ex['ordinal'] <= c['ordinal'], 4)
    # Persist current failures as earlier exclusion for the next occurrence; proximity can never revive rights.
    proof['excludedBefore'] = excluded(proof, c['actingSide'])
    result = dict(contractVersion=1, members=members, proof=proof); raw(result, 'MovementExpiryResult'); return result

def read_expiry(data, inp):
    parse(data, 'MovementExpiryResult'); expected = expire_movement(inp); require(data == raw(expected, 'MovementExpiryResult'), 6); return expected

def rejected(call, code=None):
    try: call()
    except Invalid as error:
        if code is not None: assert error.code == f'CMB-CYC-{code:03}', (error.code, code)
    else: raise AssertionError('invalid cycle control admitted')

def trace(case, side='axis', slot='first-acting-side'):
    b = base_for(case, side, slot); a = assess(b); assert control_mode(a) == case['mode'], case['name']
    assert len(a['witnesses']) == case['witnessCount'], case['name']
    for w in a['witnesses']:
        assert (w['afterCp'], w['excessCpDp']) == (case['afterCp'], case['dp']), case['name']
    s = initial(b); ins = []; events = []; opened = s['acceptedHighWater']+1000
    def send(kind, now=None, available=True):
        nonlocal s
        inp = trusted(command(b, s, kind), side if kind in ('repeat', 'finish') else 'system', now, available)
        before = copy.deepcopy(s); s, data, rid = transition(b, s, inp); assert data is not None and before != s
        ins.append(inp); events.append(data); return rid
    send('open', opened if case.get('openingClock', True) else None, case.get('openingClock', True))
    if case['mode'] == 'owner-choice':
        action = case['action']; send('repeat' if action == 'regression' else action,
            opened-1 if action == 'regression' else s['timing']['deadlineUnixMilliseconds'] if action == 'expire' else opened+1)
    assert s['status'] == ('repeated' if case.get('action') == 'repeat' else 'finished'), case['name']
    before = initial(b); assert s['world'] == before['world'] and s['randomState'] == before['randomState'] and s['attackHistory'] == before['attackHistory']
    c = b['releaseBase']['cycle']; assert s['closure']['ordinal'] == c['ordinal'] and s['closure']['receiptId'] == s['receipts'][-1]['receiptId']
    if s['status'] == 'repeated':
        assert s['positionId'] == edge(c)['movementPositionId'] and s['activeCycle']['ordinal'] == c['ordinal']+1
        assert s['activeCycle']['openedAuthorityVersion'] == s['stateVersion'] and s['activeCycle']['openingPrefix'] == json.loads(events[-1])['priorPrefix']
        assert rel.scope(s['activeCycle']) == rel.scope(c) and s['members'] == before['members'] and not s['targetUses'] and not s['nextCycleProgress']
    else:
        assert s['positionId'] == edge(c)['finishPositionId'] and s['activeCycle'] is None and s['targetUses'] == before['targetUses']
        for old, new in zip(before['members'], s['members']):
            expected = copy.deepcopy(old); ex = expected['history']['nextMovement']
            if ex and ex['status'] == 'pending': ex.update(status='expired', completionReceiptId=s['closure']['receiptId'])
            assert new == expected
    if case.get('resultCase'):
        assert b['progress'][0]['eventType'] == 'combat-attack-committed'
        assert b['progress'][0]['receiptId'] != s['attackHistory'][0]['commitmentId']  # distinct frozen identities
    return b, s, ins, events

def checkpoint_checks(f):
    count = cuts = mutations = raws = 0; goldens = []
    for case in f['cases']:
        for side in ('axis', 'commonwealth'):
            for slot in ('first-acting-side',) if case.get('resultCase') else ('first-acting-side', 'second-acting-side'):
                b, final, ins, events = trace(case, side, slot); prior = initial(b); count += 1; frames = []
                for index in range(len(events)+1):
                    s = prior if index == 0 else read_event(events[index-1], b, prior, ins[index-1])
                    data = raw(s, 'CycleControlState'); assert read_state(data, b, case, ins, events, index, side, slot) == s; cuts += 1
                    frames.append(dict(cut=index, hash=sha(data), bytes=len(data)))
                    if index:
                        retry, event, rid = transition(b, s, ins[index-1]); assert retry is s and event is None and rid == s['receipts'][-1]['receiptId']
                        for field in ('stateVersion', 'priorVersion', 'priorPrefix', 'configurationHash', 'cycleId', 'controlId', 'receiptId'):
                            obj = json.loads(events[index-1]); obj[field] = obj[field]+1 if type(obj[field]) is int else 'forged'
                            rejected(lambda: read_event(encode(obj), b, prior, ins[index-1])); mutations += 1
                        obj = json.loads(events[index-1]); obj['effect']['reason'] = 'forged'
                        obj.pop('receiptId'); obj['receiptId'] = 'cc.'+steps.digest(INVENTORY['domains']['receipt'], encode(obj))
                        rejected(lambda: read_event(raw(obj, 'CycleControlEvent'), b, prior, ins[index-1])); mutations += 1
                        event = events[index-1]
                        for bad in (event+b'\n', b'\xef\xbb\xbf'+event, event[:-1], event.replace(b'"contractVersion":1', b'"contractVersion":true', 1),
                            event.replace(b'"contractVersion":1', b'"contractVersion":1.0', 1), b'{"extra":0,'+event[1:], event.replace(b'"author":', b'"author":"system","author":', 1)):
                            rejected(lambda: read_event(bad, b, prior, ins[index-1])); raws += 1
                    # Every altered state is compared with this independently suffix-replayed cut.
                    for mutate in (lambda x: x.__setitem__('prefix', 'sha256:'+'0'*64), lambda x: x['receipts'].clear(),
                        lambda x: x['world']['elements'][0]['ammunition'].__setitem__('points', 10),
                        lambda x: x['randomState'].__setitem__('nextByteCursor', x['randomState']['nextByteCursor']+1)):
                        obj = copy.deepcopy(s); mutate(obj)
                        if obj != s:
                            rejected(lambda: (parse(raw(obj, 'CycleControlState'), 'CycleControlState'), require(raw(obj, 'CycleControlState') == data, 6))); mutations += 1
                    prior = s
                for field in ('world', 'movementEnd', 'progress', 'releaseEvents'):
                    obj = copy.deepcopy(b)
                    if field == 'world': obj[field]['elements'][0]['ammunition']['points'] += 1
                    elif field == 'movementEnd': obj[field]['completionReceiptId'] = 'forged.movement'
                    elif field == 'progress': obj[field].append(dict(eventType='combat-attack-committed', receiptId='forged', eventHash=sha(b'forged')))
                    else: obj[field] = obj[field][:-1]
                    rejected(lambda: read_base(raw(obj, 'CycleControlBase'), case, side, slot)); mutations += 1
                if len(events) == 2:
                    rejected(lambda: read_state(raw(final, 'CycleControlState'), b, case, ins, events[::-1], side=side, slot=slot)); mutations += 1
                rejected(lambda: read_state(raw(final, 'CycleControlState'), b, case, ins[:-1], events[:-1], side=side, slot=slot)); mutations += 1
                assert transition(b, final, ins[0])[1] is None  # old opening retry after final receipt
                expiry_hash = None
                if final['status'] == 'repeated':
                    expiry = dict(contractVersion=1, cycle=final['activeCycle'], firstActingSide=b['releaseBase']['firstActingSide'], priorProof=b['movementEnd'],
                        endLocations=locations(final['world']), completionReceiptId='probe.accepted-next-movement', members=final['members'])
                    result = expire_movement(expiry); assert read_expiry(raw(result, 'MovementExpiryResult'), expiry) == result
                    for old, new in zip(final['members'], result['members']):
                        expected = copy.deepcopy(old); ex = expected['history']['nextMovement']
                        if ex and ex['status'] == 'pending': ex.update(status='expired', completionReceiptId=expiry['completionReceiptId'])
                        assert new == expected
                    expiry_hash = sha(raw(result, 'MovementExpiryResult'))
                goldens.append(dict(case=case['name'], side=side, slot=slot, baseHash=sha(raw(b, 'CycleControlBase')), frames=frames, events=[sha(x) for x in events], expiryHash=expiry_hash))
    return dict(traces=count, cuts=cuts, mutations=mutations, rawRejects=raws), goldens

def boundary_checks(f):
    n = 0; case = next(c for c in f['cases'] if c['name'] == 'first-release-exception'); b, final, ins, events = trace(case); first = initial(b)
    opened = read_event(events[0], b, first, ins[0]); deadline = opened['timing']['deadlineUnixMilliseconds']
    # Exact equality is late for owners, including an owner who asks to finish.
    for kind in ('repeat', 'finish'):
        inp = trusted(command(b, opened, kind), 'axis', deadline); rejected(lambda: transition(b, opened, inp), 5); n += 1
    timer = trusted(command(b, opened, 'expire'), now=deadline-1); assert transition(b, opened, timer) == (opened, None, None); n += 1
    timer['admittedAt'] = deadline; after, ev, rid = transition(b, opened, timer)
    assert after['status'] == 'finished' and json.loads(ev)['author'] == 'system'
    assert transition(b, after, timer) == (after, None, rid); n += 1
    stale = copy.deepcopy(timer); stale['command']['decisionId'] = 'old.decision'; assert transition(b, after, stale) == (after, None, None); n += 1
    for mutate in (lambda x: x.__setitem__('actor', 'commonwealth'), lambda x: x['command'].__setitem__('cycleId', sha(b'fork')),
        lambda x: x['command'].__setitem__('controlId', 'wrong.control'), lambda x: x['command'].__setitem__('expectedPriorVersion', first['stateVersion'])):
        inp = copy.deepcopy(ins[-1]); mutate(inp); rejected(lambda: transition(b, opened, inp)); n += 1
    # No-progress and unsupported capability are distinct; armed evidence cannot become auto-finish.
    armed = base_for(case | {'ammo': 10})
    rejected(lambda: assess(armed), 3); n += 1
    no_release = copy.deepcopy(b); no_release['releaseEvents'].pop(); no_release['releaseInputs'].pop(); rejected(lambda: assess(no_release), 5); n += 1
    for kind in ('cycle-opened', 'reserve-release-completed', 'combat-choice-sealed', 'element-moved'):
        obj = copy.deepcopy(b); obj['progress'] = [dict(eventType=kind, receiptId='fake.progress', eventHash=sha(b'fake'))]
        rejected(lambda: assess(obj), 3); n += 1
    # Opening an owner decision needs room for its mandatory closure too.
    almost_full = base_for(case | {'priorVersion': 2**63-5}); before_open = initial(almost_full)
    assert before_open['stateVersion'] == 2**63-2
    rejected(lambda: transition(almost_full, before_open, trusted(command(almost_full, before_open, 'open'), now=before_open['acceptedHighWater']+1)), 7); n += 1
    # Persisted deadline/assessment/history edits must not survive suffix readback.
    for mutate in (lambda x: x['timing'].__setitem__('deadlineUnixMilliseconds', x['timing']['deadlineUnixMilliseconds']+1),
        lambda x: x['assessment']['witnesses'][0].__setitem__('afterCp', 0),
        lambda x: x['members'][0]['history'].__setitem__('voluntaryCeiling', 15)):
        obj = copy.deepcopy(opened); mutate(obj)
        rejected(lambda: read_state(raw(obj, 'CycleControlState'), b, case, ins, events, 1)); n += 1
    # Overflow is operational failure, never a forced finish or renewed budget.
    overflow = copy.deepcopy(opened); overflow['stateVersion'] = 2**63-1
    rejected(lambda: transition(b, overflow, trusted(command(b, overflow, 'repeat'), 'axis', deadline-1)), 7); n += 1
    obj = copy.deepcopy(b); obj['releaseBase']['cycle']['ordinal'] = rel.MAX
    rejected(lambda: read_base(raw(obj, 'CycleControlBase'), case)); n += 1
    # Same candidate hash cannot validate a substituted base, omitted receipt, or changed history.
    for mutation in (lambda x: x['movementEnd'].__setitem__('ordinal', 2), lambda x: x['movementEnd']['endLocations'].pop(),
        lambda x: x['releaseEvents'][-1]['effect'].__setitem__('reason', 'forged')):
        obj = copy.deepcopy(b); mutation(obj); rejected(lambda: read_base(raw(obj, 'CycleControlBase'), case)); n += 1
    # Replay protects assessments and next-cycle opening against self-consistent rehashing.
    for field in ('openingPrefix', 'ordinal', 'openedAuthorityVersion'):
        obj = json.loads(events[-1]); value = obj['effect']['nextCycle'][field]; obj['effect']['nextCycle'][field] = sha(b'wrong') if type(value) is str else value+1
        obj.pop('receiptId'); obj['receiptId'] = 'cc.'+steps.digest(INVENTORY['domains']['receipt'], encode(obj))
        rejected(lambda: read_event(raw(obj, 'CycleControlEvent'), b, opened, ins[-1])); n += 1
    # Pending exceptions last for the whole segment and then expire at its actual completion receipt.
    expiry = dict(contractVersion=1, cycle=final['activeCycle'], firstActingSide=b['releaseBase']['firstActingSide'], priorProof=b['movementEnd'],
        endLocations=locations(final['world']), completionReceiptId='probe.accepted-next-movement', members=final['members'])
    prior = copy.deepcopy(expiry); projected = expire_movement(expiry); assert expiry == prior
    assert read_expiry(raw(projected, 'MovementExpiryResult'), expiry) == projected
    assert projected['members'][0]['history']['nextMovement']['completionReceiptId'] == expiry['completionReceiptId']
    assert projected['proof']['excludedBefore'] == excluded(b['movementEnd'], 'axis'); n += 1
    for change in (lambda x: x['cycle'].__setitem__('ordinal', 3), lambda x: x['cycle'].__setitem__('playerPhaseSlot', 'second-acting-side'),
        lambda x: x['endLocations'].pop(), lambda x: x['members'][0]['history']['nextMovement'].__setitem__('ordinal', 3)):
        obj = copy.deepcopy(expiry); change(obj)
        rejected(lambda: expire_movement(obj))
        n += 1
    for field in ('status', 'completionReceiptId'):
        obj = copy.deepcopy(projected); obj['members'][0]['history']['nextMovement'][field] = 'forged'
        rejected(lambda: read_expiry(raw(obj, 'MovementExpiryResult'), expiry)); n += 1
    # Reading the same completion repeatedly is deterministic; later scope cannot revive this exception.
    assert expire_movement(expiry) == projected; n += 1
    later = copy.deepcopy(expiry); later['cycle']['ordinal'] += 1; later['priorProof'] = projected['proof']; later['members'] = projected['members']; later['completionReceiptId'] = 'probe.later-completion'
    result = expire_movement(later)
    assert result['members'][0]['history']['nextMovement'] == projected['members'][0]['history']['nextMovement']; n += 1
    # Exhaustively compare D2a arithmetic at the supported integer resource coordinates.
    coordinates = 0
    for reserve in (None, 'I', 'II'):
        for spent in range(18):
            for kinds in ([], ['contact'], ['engaged'], ['contact', 'engaged']):
                cost = 2+max(({'contact':2, 'engaged':4}[k] for k in kinds), default=0); limit = 15 if reserve is None else 10 if reserve == 'I' else 5
                if spent+cost <= limit:
                    assert mov.cost(10, spent, kinds, reserve, 2) == (spent+cost, max(0, spent+cost-10)-max(0, spent-10))
                else:
                    mov.rejected(lambda: mov.cost(10, spent, kinds, reserve, 2), 5)
                coordinates += 1
    # Canonical raw boundaries, including unknown fields, oversized records and altered expiry bytes.
    for bad in (b' ' + raw(b, 'CycleControlBase'), b'x'*1048577,
        raw(b, 'CycleControlBase').replace(b'\"contractVersion\":1', b'\"contractVersion\":1e0', 1)):
        rejected(lambda: read_base(bad, case)); n += 1
    stale_new_cycle = trusted(command(b, final, 'expire'), now=deadline)
    assert transition(b, final, stale_new_cycle) == (final, None, None); n += 1
    unavailable = trusted(command(b, opened, 'repeat'), 'axis', None, False)
    fallback, event, _ = transition(b, opened, unavailable)
    assert fallback['status'] == 'finished' and json.loads(event)['author'] == 'system'; n += 1
    for mutate in (lambda x: x.__setitem__('targetUses', []), lambda x: x['movementEnd']['excludedBefore'].append(x['movementEnd']['endLocations'][0]['unit'])):
        settled_case = next(c for c in f['cases'] if c['name'] == 'engaged-repeat'); obj = base_for(settled_case); mutate(obj)
        rejected(lambda: read_base(raw(obj, 'CycleControlBase'), settled_case)); n += 1
    return n, coordinates, dict(inputHash=sha(raw(expiry, 'MovementExpiryInput')), outputHash=sha(raw(projected, 'MovementExpiryResult')))

def source_hashes():
    names = ['verify-combat-reserve-release-v1.py', 'combat-reserve-release-v1.schema.json', 'fixtures/combat-reserve-release-v1.json',
        'verify-combat-ordinary-movement-v1.py', 'combat-ordinary-movement-v1.schema.json', 'fixtures/combat-ordinary-movement-v1.json',
        'verify-combat-cycle-sequence-v1.py', 'verify-combat-result-settlement-v1.py', 'fixtures/combat-result-settlement-v1.json']
    return {p: sha((ROOT/p).read_bytes()) for p in names}

REQUIRED_CASES = (
    'engaged-repeat', 'contact-repeat', 'retreat-repeat', 'guard-finish', 'escape-timeout',
    'exhausted-ammo-still-moves', 'no-progress-finish', 'far-end-finish', 'prior-exclusion-finish',
    'first-release-exception', 'first-release-finish-expiry', 'first-conversion-no-continuation',
    'later-release-cumulative-ceiling', 'later-release-insufficient-cp', 'mandatory-overspend-retained',
    'later-retain-not-progress', 'opening-clock-loss', 'owner-clock-regression', 'controller-unavailable')

def verification(f):
    assert tuple(c['name'] for c in f['cases']) == REQUIRED_CASES, 'required literal regression case missing/reordered'
    assert control_mode(dict(progress=[], witnesses=['legal-move'])) == 'no-material-progress'
    assert control_mode(dict(progress=['accepted-change'], witnesses=[])) == 'no-continuation'
    assert control_mode(dict(progress=['accepted-change'], witnesses=['legal-move'])) == 'owner-choice'
    stats, goldens = checkpoint_checks(f); stats['boundaryChecks'], stats['costCoordinates'], expiry = boundary_checks(f)
    return stats, goldens, expiry

def main():
    f = json.loads(FIXTURE.read_text()); stats, goldens, expiry = verification(f)
    assert f.get('sources') == source_hashes(), 'frozen predecessor sources changed'
    assert f.get('goldens') == goldens and f.get('expiryGolden') == expiry, 'missing/changed frozen regression vectors'
    print(f"PASS: {len(f['cases'])} literal cases; {stats}. Private repeat/finish and Movement expiry; D2c inherited integration remains gated.")

if __name__ == '__main__':
    main()
