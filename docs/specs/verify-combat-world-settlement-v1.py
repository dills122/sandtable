#!/usr/bin/env python3
"""World7 settlement contract oracle, not a production campaign/event reader."""
import copy
import importlib.util
import itertools
import json
import re
import sys
from pathlib import Path

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result


init = module('creation', ROOT / 'verify-combat-creation-ledger-v1.py')
source = module('selected_source', ROOT.parent / 'research/verify-combat-source-freeze.py')
DATA = json.loads(source.FIXTURE.read_text())
TABLES = source.normalize(source.tables(DATA), DATA['source_ruling'])
INVENTORY = json.loads((ROOT / 'combat-world-settlement-v1.schema.json').read_text())
SCHEMA = init.SCHEMA | {k: [tuple(f.split(':')) for f in v.split()] for k, v in INVENTORY['objects'].items()}
KEYS = init.KEYS | INVENTORY['sortedArrays']
PACK = init.content.read((ROOT / 'fixtures/combat-content-v7.canonical.json').read_bytes())
CREATION = json.loads((ROOT / 'fixtures/combat-creation-ledger-v1.json').read_text())
SETUP = init.read_setup(CREATION['setup']['canonicalUtf8'].encode(), PACK)
INITIAL = init.read_elements(CREATION['initialElements']['canonicalUtf8'].encode(), SETUP, PACK)
GRAPH = {p['locationId']: set() for p in PACK['locations']}
for edge in PACK['edges']:
    a, b = edge['firstLocationId'], edge['secondLocationId']
    GRAPH[a].add(b)
    GRAPH[b].add(a)
SIDES = {e['elementId']: e['sideId'] for e in PACK['elements']}
ANCHORS = {a['sideId']: a['locationId'] for a in PACK['scenarios'][0]['retreatSupplyAnchors']}
CUTS = ['resolved', 'disposition', 'losses', 'retreat', 'custody', 'relationships']


class Invalid(ValueError):
    def __init__(self, code, path):
        self.code, self.path = f'CMB-WLD-{code:03}', path
        super().__init__(f'{self.code} {path or "/"}')


def require(condition, code, path):
    if not condition:
        raise Invalid(code, path)


def typed(value, kind, path='', depth=0):
    require(depth <= 32, 1, path)
    if kind.endswith('?'):
        if value is not None: typed(value, kind[:-1], path, depth)
    elif kind in SCHEMA:
        require(type(value) is dict, 1, path)
        for key, child in SCHEMA[kind]:
            require(key in value, 1, path + '/' + key)
            typed(value[key], child, path + '/' + key, depth + 1)
        require(set(value) == {k for k, _ in SCHEMA[kind]}, 1, path)
    elif kind.endswith('[]') or kind == 'Route':
        require(type(value) is list and len(value) <= 4096, 1, path)
        child = 'id' if kind == 'Route' else kind[:-2]
        if child == 'LegacyBrokenVehicleLot': require(not value, 2, path)
        else:
            for i, item in enumerate(value): typed(item, child, f'{path}/{i}', depth + 1)
    elif kind == 'futureTurn':
        require(type(value) is int, 1, path)
        require(1 <= value <= 115, 2, path)
    else:
        try:
            init.shape(value, kind, path)
            init.primitives(value, kind, path)
        except init.Invalid as error:
            raise Invalid(1 if error.code.endswith('001') else 2, error.path) from error


def canonical(value, kind):
    if kind.endswith('?'): return None if value is None else canonical(value, kind[:-1])
    if kind in SCHEMA: return {k: canonical(value[k], c) for k, c in SCHEMA[kind]}
    if kind == 'Route': return value[:]
    if kind.endswith('[]'):
        child = kind[:-2]
        values = [canonical(v, child) for v in value]
        return sorted(values, key=(lambda v: tuple(v[k] for k in KEYS[child])) if child in KEYS else None)
    return value


def encode(world):
    return init.encode(canonical(world, 'World'))


def cp(amount):
    return dict(numerator=amount, denominator=1)


def unit(element, creation):
    return dict(creationBinding=creation, originalSide=SIDES[element['elementId']], elementId=element['elementId'])


def component(element, creation):
    return dict(unit=unit(element, creation), componentId=element['components'][0]['componentId'])


def eligible_scope(turn, stage):
    require(type(turn) is int and 1 <= turn <= 111, 2, '/earnedScope/gameTurn')
    require(type(stage) is int and 1 <= stage <= 3, 2, '/earnedScope/operationStage')
    ordinal = (turn - 1) * 3 + stage - 1 + 12
    return dict(gameTurn=ordinal // 3 + 1, operationStage=ordinal % 3 + 1)


def result_facts(diff, ac, dc, die):
    require(type(diff) is int and diff in source.DIFFS, 2, '/result/differential')
    require(type(ac) is int and ac in source.COORDS and type(dc) is int and dc in source.COORDS, 2, '/result/coordinate')
    asum, dsum = ac // 10 + ac % 10, dc // 10 + dc % 10
    ca = asum in DATA['attacker_capture_sums'][str(diff)]
    cd = dsum in DATA['defender_capture_sums'][str(diff)]
    require(not (ca and cd), 2, '/result/capturedRole')
    role = 'attacker' if ca else 'defender' if cd else None
    require((role is None and die is None) or (role is not None and type(die) is int and 1 <= die <= 6), 2, '/result/captureDie')
    return dict(differential=diff, attackerCoordinate=ac, defenderCoordinate=dc, captureDie=die,
                attackerPercent=source.loss(TABLES, 'attacker', diff, ac), defenderPercent=source.loss(TABLES, 'defender', diff, dc),
                rawEngaged=asum in DATA['attacker_engaged_sums'][str(diff)],
                requiredRetreat=int(dsum in DATA['defender_retreat_one_hex_sums'][str(diff)]), capturedRole=role,
                captureShare=DATA['capture_share_percent_by_die'][die - 1] if die else 0)


def world_at(case, cut):
    """Reconstruct an isolated cut from trusted, pre-existing fixture inputs."""
    require(cut in CUTS or cut == 'initial', 2, '/cut')
    sid, creation = case['settlementId'], case['creationBinding']
    require(type(sid) is str and len(sid) <= 80 and re.fullmatch(init.content.ID, sid), 2, '/settlementId')
    require(case['attackerSide'] in ('axis', 'commonwealth'), 2, '/attackerSide')
    before = case['preAssaultCp']
    require(len(before) == 2 and all(type(v) is int for v in before) and 0 <= before[0] <= 5 and 0 <= before[1] <= 7, 2, '/preAssaultCp')
    elements = copy.deepcopy(INITIAL)
    attacker = next(e for e in elements if SIDES[e['elementId']] == case['attackerSide'])
    defender = next(e for e in elements if e is not attacker)
    representations = [dict(representationId=f'map-representation.{i+1:04}', currentLocationId=e['currentLocationId'],
                            bindingKind='independent-element', boundElementIds=[e['elementId']]) for i, e in enumerate(elements)]
    world = dict(contractVersion=7, creationBinding=creation, elements=elements, representations=representations,
                 brokenVehicleLots=[], cohesionCauses=[], relationships=[], custodyLots=[], guards=[],
                 replacementEntitlements=[], futureObligations=[], settlements=[])
    if cut == 'initial': return world
    for e, expenditure in [(attacker, before[0] + 5), (defender, before[1] + 3)]:
        e['ammunition']['points'] = 0
        e['operationalState']['capabilityPointsExpended'] = cp(expenditure)
    facts = result_facts(case['differential'], case['attackerCoordinate'], case['defenderCoordinate'], case['captureDie'])
    frozen = copy.deepcopy(elements)
    frozen_by_id = {e['elementId']: e for e in frozen}
    settlement = dict(settlementId=sid, commitmentId=sid + '.commit', resultId=sid + '.result', gameTurn=1, operationStage=1,
                      attacker=unit(attacker, creation), defender=unit(defender, creation), preLossElements=frozen, result=facts,
                      disposition=None, losses=None, retreat=None, custody=None, relationships=None)
    world['settlements'].append(settlement)
    if cut == 'resolved': return world
    required = facts['requiredRetreat']
    choice = case['retreatChoice']
    require(choice in (('retreat', 'refuse-retreat') if required else ('not-required',)), 2, '/retreatChoice')
    origin = defender['currentLocationId']
    retreat_path = init.content.route(GRAPH, origin, ANCHORS[SIDES[defender['elementId']]])[:2]
    require(len(retreat_path) == 2 and len(init.content.route(GRAPH, attacker['currentLocationId'], retreat_path[-1])) == 3, 2, '/retreatRoute')
    planned = int(choice == 'retreat')
    route = retreat_path if planned else [origin]
    settlement['disposition'] = dict(receiptId=sid + '.disposition', predecessorReceiptId=settlement['resultId'], kind=choice,
                                     requiredDistance=required, plannedDistance=planned, unfulfilledDistance=required-planned, route=route)
    if cut == 'disposition': return world
    def cause(e, receipt, kind, points):
        if not points: return
        prior = e['operationalState']['cohesionLevel']
        after = min(10, prior + points) if kind == 'assault-victory-rp' else prior - points
        e['operationalState']['cohesionLevel'] = after
        world['cohesionCauses'].append(dict(causeId=sid + '.' + kind + '.' + SIDES[e['elementId']], ordinal=len(world['cohesionCauses'])+1,
            receiptId=receipt, elementId=e['elementId'], gameTurn=1, operationStage=1, kind=kind, points=points, before=prior, after=after))
    roles = []
    for role, e in [('attacker', attacker), ('defender', defender)]:
        percent = facts[role + 'Percent']
        refusal = 10 * (required - planned) if role == 'defender' else 0
        numerator = 10 * (percent + refusal)
        loss = (numerator + 99) // 100 if role == 'attacker' else numerator // 100
        captured = (loss * facts['captureShare'] + 99) // 100 if role == facts['capturedRole'] else 0
        roles.append(dict(role=role, component=component(e, creation), committedToe=10, tablePercent=percent, refusalPercent=refusal,
                          lossToe=loss, capturedToe=captured, otherLossToe=loss-captured, remainingToe=10-loss, lossDp=3 if loss >= 3 else 0))
        e['components'][0]['currentToe'] = 10 - loss
        cause(e, sid + '.losses', 'loss-dp', 3 if loss >= 3 else 0)
    settlement['losses'] = dict(receiptId=sid + '.losses', predecessorReceiptId=sid + '.disposition', roles=roles)
    captured_role = next((r for r in roles if r['capturedToe']), None)
    if captured_role:
        victim = attacker if captured_role['role'] == 'attacker' else defender
        captor = defender if victim is attacker else attacker
        world['custodyLots'].append(dict(lotId=sid + '.captives', lossReceiptId=sid + '.losses', originalComponent=component(victim, creation),
            captor=unit(captor, creation), quantity=captured_role['capturedToe'], originLocationId=frozen_by_id[victim['elementId']]['currentLocationId'],
            currentLocationId=frozen_by_id[victim['elementId']]['currentLocationId'], status='pending', guardId=None, escapeReceiptId=None))
    if cut == 'losses': return world
    e_before = defender['operationalState']['capabilityPointsExpended']['numerator']
    e_after = e_before + planned
    excess = max(0, e_after - 10) - max(0, e_before - 10)
    defender['operationalState']['capabilityPointsExpended'] = cp(e_after)
    defender['currentLocationId'] = route[-1]
    for rep in representations:
        if rep['boundElementIds'] == [defender['elementId']]: rep['currentLocationId'] = route[-1]
    cause(defender, sid + '.retreat', 'retreat-excess-dp', excess)
    cause(attacker, sid + '.retreat', 'assault-victory-rp', 3 if planned else 0)
    settlement['retreat'] = dict(receiptId=sid + '.retreat', predecessorReceiptId=sid + '.losses', kind=choice, route=route,
        completedDistance=planned, beforeCp=cp(e_before), afterCp=cp(e_after), excessCpDp=excess, attackerVictoryRp=3 if planned else 0)
    if cut == 'retreat': return world
    custody_choice = case['custodyChoice']
    if captured_role:
        require(custody_choice in ('relocate-and-guard', 'leave-unguarded'), 2, '/custodyChoice')
        lot = world['custodyLots'][0]
        donor_before = captor['components'][0]['currentToe']
        earned = dict(gameTurn=1, operationStage=1)
        guarded = custody_choice == 'relocate-and-guard'
        destination = captor['currentLocationId'] if guarded else victim['currentLocationId']
        custody_route = init.content.route(GRAPH, lot['originLocationId'], destination)
        require(0 < len(custody_route) <= (4 if guarded else 9), 2, '/custodyRoute')
        if guarded:
            require(victim['currentLocationId'] not in custody_route[1:], 2, '/custodyRoute')
            captor['components'][0]['currentToe'] -= 1
            guard = dict(guardId=sid + '.guard', formationReceiptId=sid + '.custody', lotId=lot['lotId'], originComponent=component(captor, creation),
                currentLocationId=destination, toe=1, baseCapabilityPointAllowance=10, offensiveCloseAssaultRating=0, defensiveCloseAssaultRating=1,
                operationalState=copy.deepcopy(captor['operationalState']), ammunition=copy.deepcopy(captor['ammunition']), readiness=copy.deepcopy(captor['readiness']))
            world['guards'].append(guard)
            lot.update(status='guarded', currentLocationId=destination, guardId=guard['guardId'])
            world['futureObligations'].append(dict(obligationId=sid+'.upkeep',receiptId=sid+'.custody',kind='guard-priority-upkeep',subjectId=guard['guardId'],earnedScope=earned,
                eligibleScope=None,activationGate='before-prisoner-upkeep-or-guard-action',status='retained-unimplemented'))
        else:
            entitlement = dict(entitlementId=sid + '.replacement', escapeReceiptId=sid + '.custody', lotId=lot['lotId'], originalComponent=component(victim, creation),
                quantity=lot['quantity'], reunionLocationId=destination, earnedScope=earned, delayOperationStages=12, eligibleScope=eligible_scope(1,1),status='awaiting-eligibility-and-training')
            world['replacementEntitlements'].append(entitlement)
            lot.update(status='escaped', currentLocationId=None, escapeReceiptId=sid + '.custody')
            world['futureObligations'].append(dict(obligationId=sid+'.training',receiptId=sid+'.custody',kind='replacement-training-gate',subjectId=entitlement['entitlementId'],
                earnedScope=earned,eligibleScope=entitlement['eligibleScope'],activationGate='before-replacement-eligibility-training',status='retained-unimplemented'))
        settlement['custody'] = dict(receiptId=sid+'.custody',predecessorReceiptId=sid+'.retreat',lotId=lot['lotId'],kind=custody_choice,route=custody_route,
            guardId=sid+'.guard' if guarded else None,entitlementId=None if guarded else sid+'.replacement',donorToeBefore=donor_before,donorToeAfter=captor['components'][0]['currentToe'])
    else:
        require(custody_choice is None, 2, '/custodyChoice')
        require(cut != 'custody', 2, '/cut')
    if cut == 'custody': return world
    adjacent = defender['currentLocationId'] in GRAPH[attacker['currentLocationId']]
    relation_kind = ('engaged' if facts['rawEngaged'] and not required else 'contact') if adjacent else None
    if relation_kind:
        world['relationships'].append(dict(relationId=sid+'.relation',creationReceiptId=sid+'.relationships',kind=relation_kind,
            attacker=unit(attacker,creation),defender=unit(defender,creation),gameTurn=1,operationStage=1,active=True,endedByReceiptId=None,endingCause=None))
    settlement['relationships'] = dict(receiptId=sid+'.relationships',predecessorReceiptId=sid+('.custody' if captured_role else '.retreat'),
        relationshipId=sid+'.relation' if relation_kind else None,kind=relation_kind)
    return world


def compare(actual, expected, path=''):
    require(type(actual) is type(expected), 6, path)
    if isinstance(expected, dict):
        for k in expected: compare(actual[k], expected[k], path + '/' + k)
    elif isinstance(expected, list):
        require(len(actual) == len(expected), 6, path)
        for i, (a, e) in enumerate(zip(actual, expected)): compare(a, e, f'{path}/{i}')
    else: require(actual == expected, 6, path)


def read(raw, case, cut):
    require(len(raw) <= 1048576 and not raw.startswith(b'\xef\xbb\xbf'), 1, '')
    try:
        value = json.loads(raw.decode('utf-8'), object_pairs_hook=init.content.pairs,
                          parse_constant=lambda _: (_ for _ in ()).throw(ValueError()))
    except (ValueError, RecursionError) as error: raise Invalid(1, '') from error
    typed(value, 'World')
    compare(canonical(value, 'World'), canonical(world_at(case, cut), 'World'))
    require(encode(value) == raw, 8, '')
    return value


def assert_conservation(world):
    s = world['settlements'][0]
    for loss in s['losses']['roles']:
        e = next(e for e in world['elements'] if e['elementId'] == loss['component']['unit']['elementId'])
        transferred = sum(g['toe'] for g in world['guards'] if g['originComponent'] == loss['component'])
        assert 10 == e['components'][0]['currentToe'] + transferred + loss['capturedToe'] + loss['otherLossToe']
        assert loss['capturedToe'] + loss['otherLossToe'] == loss['lossToe'] <= 3
        assert e['ammunition']['points'] == 0
    for lot in world['custodyLots']:
        assert lot['status'] in ('guarded','escaped') and 1 <= lot['quantity'] <= 3
        assert len(world['futureObligations']) == 1
    for guard in world['guards']:
        donor = next(e for e in world['elements'] if e['elementId'] == guard['originComponent']['unit']['elementId'])
        assert guard['operationalState'] == donor['operationalState'] and guard['ammunition']['points'] == 0
        assert guard['toe'] * 5 >= world['custodyLots'][0]['quantity']
    for entitlement in world['replacementEntitlements']:
        assert entitlement['eligibleScope'] == eligible_scope(**{'turn':entitlement['earnedScope']['gameTurn'],'stage':entitlement['earnedScope']['operationStage']})


def main():
    manifest = json.loads((ROOT/'fixtures/combat-world-settlement-v1.json').read_text())
    for golden in manifest['goldens']:
        raw = golden['canonicalUtf8'].encode()
        assert len(raw) == golden['byteCount'] and init.digest(raw) == golden['sha256']
        read(raw, golden['input'], golden['cut'])
    for vector in manifest['negativeVectors']:
        g = manifest['goldens'][vector['golden']]
        value = json.loads(g['canonicalUtf8'])
        if vector['op'] == 'append': raw = g['canonicalUtf8'].encode() + vector['value'].encode()
        else: raw = init.encode(init.mutate(value, vector))
        try: read(raw, g['input'], g['cut'])
        except Invalid as error:
            assert (error.code,error.path) == (vector['code'],vector['errorPath']), (vector['name'],str(error))
        else: raise AssertionError('Accepted negative: '+vector['name'])
    cuts = 0
    for case in manifest['cutCases']:
        final = world_at(case, 'relationships')
        assert_conservation(final)
        for cut in CUTS:
            if cut == 'custody' and not final['custodyLots']: continue
            raw = encode(world_at(case, cut))
            read(raw, case, cut)
            # Restore of this isolated contract cut retains the same receipt payload and continuation result.
            restored = json.loads(raw)
            assert encode(restored) == raw
            assert encode(world_at(case, 'relationships')) == encode(final)
            cuts += 1
    for v in manifest['calendarVectors']:
        assert eligible_scope(*v['earned']) == dict(gameTurn=v['eligible'][0],operationStage=v['eligible'][1])
    for args in [(0,1),(112,1),(1,0),(1,4),(True,1)]:
        try: eligible_scope(*args)
        except Invalid: pass
        else: raise AssertionError('Bad calendar accepted')
    # Exhaustive source-derived arithmetic checked independently of World materialization.
    outcomes = 0
    for diff, ac, dc in itertools.product(source.DIFFS, source.COORDS, source.COORDS):
        trigger = (ac//10+ac%10 in DATA['attacker_capture_sums'][str(diff)] or dc//10+dc%10 in DATA['defender_capture_sums'][str(diff)])
        for die in range(1,7) if trigger else [None]:
            facts = result_facts(diff,ac,dc,die)
            for refusal in range(facts['requiredRetreat']+1):
                a = (10*facts['attackerPercent']+99)//100
                d = 10*(facts['defenderPercent']+10*refusal)//100
                for role, total in [('attacker',a),('defender',d)]:
                    captured = (total*facts['captureShare']+99)//100 if facts['capturedRole']==role else 0
                    assert 0 <= captured <= total <= 3 and 10 == 10-total+captured+(total-captured)
                outcomes += 1
    assert outcomes == 8840
    print(f"PASS: {len(manifest['goldens'])} canonical World goldens; {len(manifest['negativeVectors'])} rejection vectors; "
          f"{cuts} isolated receipt cuts/{len(manifest['cutCases'])} scenarios; {outcomes} source arithmetic cases; {len(manifest['calendarVectors'])} calendar boundaries. No production replay claim.")


if __name__ == '__main__':
    main()
