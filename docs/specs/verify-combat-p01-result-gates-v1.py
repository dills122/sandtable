#!/usr/bin/env python3
"""Read-only P01 AC-004 bounded result proof; never admits an action."""
import argparse
import copy
import gzip
import hashlib
import importlib.util
import json
import sys
from collections import Counter
from pathlib import Path

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent
EVIDENCE = Path('/Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence')
PINS = {
    'summary.json': '847972f5eb9c12be2e7a1774d2acd61caa1dc603b43413f7fb9d30cbec80c45b',
    'manifest.json': '39cae77c405b83a793b3d806fcf4a67860b9ff8514e59c9bfa83384d7dfc2b35',
    'bounded-profile-audit.json': 'ba60a05a14ab907bb5be1e06509a5a24646a97759e564bcd525bdd7515bef429',
    'CNA_LandGameRules.pdf': 'b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99',
    'ERR_CampaignforNorthAfrica.pdf': '5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb',
}


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


source = load('p01_result_source', ROOT.parent / 'research/verify-combat-source-freeze.py')
inputs = load('p01_result_inputs', ROOT / 'verify-combat-rules-inputs-v1.py')
world = load('p01_result_world', ROOT / 'verify-combat-world-settlement-v1.py')
ancestry = load('p01_result_ancestry', ROOT / 'verify-combat-actual-moved-repeat-position-evidence-v1.py')


def require(ok, label):
    if not ok:
        raise ValueError('P01-AC-004 ' + label)


def sha(data):
    return hashlib.sha256(data).hexdigest()


def pinned_rules(evidence):
    for name, expected in PINS.items():
        require(sha((evidence / name).read_bytes()) == expected, 'source pin ' + name)
    golden = next(g for g in json.loads((ROOT / 'fixtures/combat-rules-inputs-v1.json').read_text())['goldens']
                  if g['kind'] == 'RulesInput')
    raw = golden['canonicalUtf8'].encode('ascii')
    require(len(raw) == golden['byteCount'] and 'sha256:' + sha(raw) == golden['sha256'], 'RulesInput golden pin')
    rules = json.loads(raw)
    require(inputs.expected_rules() == rules, 'source normalization differs from RulesInput')
    hashes = {s['sourceId']: s['sha256'].removeprefix('sha256:') for s in rules['sourceEvidence']}
    require(hashes['spi-1979-land-rules'] == PINS['CNA_LandGameRules.pdf']
            and hashes['spi-1979-september-errata'] == PINS['ERR_CampaignforNorthAfrica.pdf'],
            'physical source binding')
    require(rules['amendment'] == dict(rulingId='CMB-SRC-RUL-001', role='defender',
                                      differential=2, coordinates=[34, 35, 36], lossPercent=10),
            'three-cell adopted amendment')
    return rules


def normalized_positive_world(state):
    result = copy.deepcopy(state['world'])
    for element in result['elements']:
        element['operationalState']['capabilityPointsExpended'] = world.cp(0)
    return result


def initial_case(state):
    return dict(settlementId='p01.proof', creationBinding=state['world']['creationBinding'],
                attackerSide=state['activeCycle']['actingSide'], preAssaultCp=[4, 0], differential=0,
                attackerCoordinate=66, defenderCoordinate=66, captureDie=None,
                retreatChoice='not-required', custodyChoice=None)


def positive_worlds(evidence):
    """Authenticate 44 R03 ancestors, then extract their pinned terminal Worlds."""
    selected = {36 + 6 * n for n in range(44)}
    audited = ancestry.audit(evidence, selected)
    require(audited == dict(verifiedRows=44, provisionalPositive=44,
                           provisionalEmpty=0, admittedRows=0), 'authentic positive ancestry')
    manifest = json.loads((evidence / 'manifest.json').read_text())
    rows = []
    for chunk in manifest['chunks']:
        raw = (evidence / chunk['name']).read_bytes()
        require(sha(raw) == chunk['sha256'], 'row chunk ' + chunk['name'])
        for line in gzip.decompress(raw).splitlines():
            row = json.loads(line)
            if row['index'] not in selected:
                continue
            r03 = row['r03']
            control = r03['vectors']['afterControlCanonicalUtf8'].encode('ascii')
            require('sha256:' + sha(control) == r03['afterControlHash'], 'terminal control hash')
            state = json.loads(control)
            require(row['classification'] == 'unresolved' and r03['rowKey'] == f"r{row['index']//6+6:03}-c01",
                    'row identity')
            rows.append((row['index'], state))
    require({i for i, _ in rows} == selected and len(rows) == 44, 'positive row set')
    counts = Counter()
    for index, state in rows:
        side = state['activeCycle']['actingSide']
        require(side in ('axis', 'commonwealth') and state['world']['settlements'] == [], 'positive base shape')
        expected = world.world_at(initial_case(state), 'initial')
        actual = state['world']
        members = {e['elementId']: e for e in actual['elements']}
        actor = next(e for e in world.PACK['elements'] if e['sideId'] == side)
        for element in actual['elements']:
            expected_cp = 4 if element['elementId'] == actor['elementId'] else 0
            cp = element['operationalState']['capabilityPointsExpended']
            require(cp == {'numerator': expected_cp, 'denominator': 1}, 'positive current CP')
        require(normalized_positive_world(state) == expected and len(members) == 2,
                f'full World equivalence row {index}')
        counts[side] += 1
    require(counts == {'axis': 22, 'commonwealth': 22}, 'orientation coverage')
    return rows


def independent_result(rules, differential, attacker_coordinate, defender_coordinate, die):
    cells = {(v['role'], v['differential'], v['coordinate']): v['lossPercent'] for v in rules['losses']}
    effects = {v['differential']: v for v in rules['effects']}[differential]
    a_sum = attacker_coordinate // 10 + attacker_coordinate % 10
    d_sum = defender_coordinate // 10 + defender_coordinate % 10
    captured = ('attacker' if a_sum in effects['attackerCaptureSums'] else
                'defender' if d_sum in effects['defenderCaptureSums'] else None)
    require((die is None) == (captured is None), 'conditional capture die')
    return dict(differential=differential, attackerCoordinate=attacker_coordinate,
                defenderCoordinate=defender_coordinate, captureDie=die,
                attackerPercent=cells['attacker', differential, attacker_coordinate],
                defenderPercent=cells['defender', differential, defender_coordinate],
                rawEngaged=a_sum in effects['attackerEngagedSums'],
                requiredRetreat=int(d_sum in effects['defenderRetreatOneHexSums']),
                capturedRole=captured,
                captureShare=rules['captureShares'][die - 1]['percent'] if die else 0)


def check_settlement(case, projected, facts, custody_choice):
    s = projected['settlements'][0]
    require(s['result'] == facts and len(s['preLossElements']) == 2, 'immutable result receipt')
    frozen = {world.SIDES[e['elementId']]: e for e in s['preLossElements']}
    defending_side = 'axis' if case['attackerSide'] == 'commonwealth' else 'commonwealth'
    for side, paid_cp in ((case['attackerSide'], 9), (defending_side, 3)):
        element = frozen[side]
        require(element['operationalState']['capabilityPointsExpended'] == world.cp(paid_cp)
                and element['operationalState']['cohesionLevel'] == 0
                and element['components'][0]['currentToe'] == 10
                and element['ammunition']['points'] == 0, 'frozen paid pre-loss basis')
    d = s['disposition']
    required = facts['requiredRetreat']
    moved = int(case['retreatChoice'] == 'retreat')
    require(d['requiredDistance'] == required and d['plannedDistance'] == moved
            and d['unfulfilledDistance'] == required - moved
            and d['kind'] == case['retreatChoice'], 'retreat disposition')
    losses = {r['role']: r for r in s['losses']['roles']}
    require(set(losses) == {'attacker', 'defender'}, 'joint loss roles')
    for role in ('attacker', 'defender'):
        r = losses[role]
        percent = facts[role + 'Percent']
        refusal = 10 * (required - moved) if role == 'defender' else 0
        loss = (10 * percent + 99) // 100 if role == 'attacker' else 10 * (percent + refusal) // 100
        captured = ((loss * facts['captureShare'] + 99) // 100
                    if facts['capturedRole'] == role else 0)
        require((r['committedToe'], r['tablePercent'], r['refusalPercent'], r['lossToe'],
                 r['capturedToe'], r['otherLossToe'], r['remainingToe'], r['lossDp']) ==
                (10, percent, refusal, loss, captured, loss - captured, 10 - loss, 3 if loss >= 3 else 0),
                'loss and capture conservation')
        require(0 <= captured <= loss <= 3 and r['remainingToe'] >= 7, 'bounded survivor')
    retreat = s['retreat']
    require(retreat['beforeCp'] == world.cp(3) and retreat['afterCp'] == world.cp(3 + moved)
            and retreat['completedDistance'] == moved and retreat['excessCpDp'] == 0
            and retreat['attackerVictoryRp'] == 3 * moved, 'retreat ledger and victory')
    require(s['losses']['predecessorReceiptId'] == d['receiptId']
            and retreat['predecessorReceiptId'] == s['losses']['receiptId'], 'causal receipt chain')
    elements = {world.SIDES[e['elementId']]: e for e in projected['elements']}
    attacker = elements[case['attackerSide']]
    defender_side = 'axis' if case['attackerSide'] == 'commonwealth' else 'commonwealth'
    defender = elements[defender_side]
    require(attacker['operationalState']['capabilityPointsExpended'] == world.cp(9)
            and defender['operationalState']['capabilityPointsExpended'] == world.cp(3 + moved)
            and attacker['ammunition']['points'] == defender['ammunition']['points'] == 0,
            'paid CP and ammo conservation')
    require(attacker['operationalState']['cohesionLevel'] == -losses['attacker']['lossDp'] + 3 * moved
            and defender['operationalState']['cohesionLevel'] == -losses['defender']['lossDp'],
            'Cohesion cause order')
    require(len(projected['cohesionCauses']) == sum(int(losses[r]['lossDp'] > 0) for r in losses) + moved,
            'cause count')
    for ordinal, cause in enumerate(projected['cohesionCauses'], 1):
        require(cause['ordinal'] == ordinal and cause['after'] ==
                cause['before'] + cause['points'] * (1 if cause['kind'] == 'assault-victory-rp' else -1),
                'ordered Cohesion cause')
    lot_quantity = sum(r['capturedToe'] for r in losses.values())
    require(len(projected['custodyLots']) == int(lot_quantity > 0), 'custody lot count')
    if lot_quantity:
        lot = projected['custodyLots'][0]
        custody = s['custody']
        require(lot['quantity'] == lot_quantity and custody['predecessorReceiptId'] == retreat['receiptId']
                and custody['kind'] == custody_choice and len(custody['route']) - 1 <= 3,
                'positive custody receipt')
        captor_role = 'defender' if facts['capturedRole'] == 'attacker' else 'attacker'
        captor = defender if captor_role == 'defender' else attacker
        require(custody['donorToeBefore'] == losses[captor_role]['remainingToe']
                and custody['donorToeAfter'] == losses[captor_role]['remainingToe'] - int(custody_choice == 'relocate-and-guard'),
                'guard donor conservation')
        if custody_choice == 'relocate-and-guard':
            require(lot['status'] == 'guarded' and len(projected['guards']) == 1
                    and projected['guards'][0]['toe'] == 1 and projected['guards'][0]['lotId'] == lot['lotId']
                    and len(projected['replacementEntitlements']) == 0
                    and len(projected['futureObligations']) == 1
                    and projected['futureObligations'][0]['kind'] == 'guard-priority-upkeep'
                    and projected['futureObligations'][0]['status'] == 'retained-unimplemented',
                    'guard and upkeep')
        else:
            require(lot['status'] == 'escaped' and len(projected['guards']) == 0
                    and len(projected['replacementEntitlements']) == 1
                    and projected['replacementEntitlements'][0]['quantity'] == lot_quantity
                    and projected['replacementEntitlements'][0]['delayOperationStages'] == 12
                    and len(projected['futureObligations']) == 1
                    and projected['futureObligations'][0]['kind'] == 'replacement-training-gate'
                    and projected['futureObligations'][0]['status'] == 'retained-unimplemented',
                    'escape and replacement')
        require(captor['components'][0]['currentToe'] == custody['donorToeAfter'], 'final donor TOE')
    else:
        require(s['custody'] is None and custody_choice is None and not projected['guards']
                and not projected['replacementEntitlements'] and not projected['futureObligations'],
                'no-custody path')
    relation = s['relationships']
    expected_kind = None if moved else 'engaged' if facts['rawEngaged'] and not required else 'contact'
    require(relation['kind'] == expected_kind
            and relation['predecessorReceiptId'] == (s['custody']['receiptId'] if lot_quantity else retreat['receiptId'])
            and len(projected['relationships']) == int(expected_kind is not None), 'final relation and closure')
    for e in projected['elements']:
        rep = next(r for r in projected['representations'] if r['boundElementIds'] == [e['elementId']])
        require(rep['currentLocationId'] == e['currentLocationId'], 'representation follows survivor')


def verify_matrix(rules, rows):
    coords = [10 * t + o for t in range(1, 7) for o in range(1, 7)]
    morale = {r['coordinate']: r['adjustment'] for r in rules['morale']}
    require(len(morale) == 36 and len(rules['losses']) == 360, 'table cardinality')
    differences = Counter(morale[a] - morale[d] for a in coords for d in coords)
    require(sum(differences.values()) == 1296 and set(differences) == set(range(-2, 3)), 'Morale closure')
    counts = Counter()
    result_counts = Counter()
    branch_sets = {side: set() for side in ('axis', 'commonwealth')}
    binding = rows[0][1]['world']['creationBinding']
    for side in ('axis', 'commonwealth'):
        for diff in sorted(differences):
            for ac in coords:
                for dc in coords:
                    base = independent_result(rules, diff, ac, dc, None if not
                                              ((ac // 10 + ac % 10 in next(e for e in rules['effects'] if e['differential'] == diff)['attackerCaptureSums']) or
                                               (dc // 10 + dc % 10 in next(e for e in rules['effects'] if e['differential'] == diff)['defenderCaptureSums'])) else 1)
                    for disposition in (('retreat', 'refuse-retreat') if base['requiredRetreat'] else ('not-required',)):
                        for die in (range(1, 7) if base['capturedRole'] else (None,)):
                            facts = independent_result(rules, diff, ac, dc, die)
                            require(world.result_facts(diff, ac, dc, die) == facts, 'result table/effects equality')
                            result_counts[side] += 1
                            for custody in (('relocate-and-guard', 'leave-unguarded') if facts['capturedRole'] else (None,)):
                                case = dict(settlementId='p01.proof', creationBinding=binding,
                                            attackerSide=side, preAssaultCp=[4, 0], differential=diff,
                                            attackerCoordinate=ac, defenderCoordinate=dc, captureDie=die,
                                            retreatChoice=disposition, custodyChoice=custody)
                                projected = world.world_at(case, 'relationships')
                                check_settlement(case, projected, facts, custody)
                                counts[side] += 1
                                branch_sets[side].add((diff, facts['requiredRetreat'], disposition,
                                                       facts['rawEngaged'], facts['capturedRole']))
    require(branch_sets['axis'] == branch_sets['commonwealth']
            and len(branch_sets['axis']) == 39, 'all adopted result branches')
    require(result_counts == {'axis': 8840, 'commonwealth': 8840}
            and counts == {'axis': 9728, 'commonwealth': 9728}, 'complete result/settlement matrix')
    return dict(moralePairs=1296, jointCoordinates=5 * 36 * 36,
                resultCases=dict(result_counts), settlementWorlds=dict(counts), branchKindsPerOrientation=39)


def mutation_checks(rules, rows):
    state = rows[0][1]
    baseline = world.world_at(initial_case(state), 'initial')
    require(normalized_positive_world(state) == baseline, 'mutation baseline')
    checks = 0
    for field, change in (
            ('TOE', lambda w: w['elements'][0]['components'][0].__setitem__('currentToe', 9)),
            ('ammo', lambda w: w['elements'][0]['ammunition'].__setitem__('points', 9)),
            ('representation', lambda w: w['representations'][0].__setitem__('currentLocationId', 'axis-rear'))):
        changed = copy.deepcopy(state)
        change(changed['world'])
        require(normalized_positive_world(changed) != baseline, field + ' mutation escaped')
        checks += 1
    changed_rules = copy.deepcopy(rules)
    next(c for c in changed_rules['losses'] if c['role'] == 'defender' and c['differential'] == 2
         and c['coordinate'] == 34)['lossPercent'] = 5
    require(independent_result(changed_rules, 2, 66, 34, None) != world.result_facts(2, 66, 34, None),
            'amended-cell mutation escaped')
    checks += 1
    changed_rules = copy.deepcopy(rules)
    changed_rules['costs']['attackerCapabilityPoints'] = 4
    require(changed_rules != inputs.expected_rules(), 'cost mutation escaped source equality')
    checks += 1
    case = initial_case(state)
    case['differential'] = 0
    case['attackerCoordinate'] = 66
    case['defenderCoordinate'] = 66
    facts = independent_result(rules, 0, 66, 66, None)
    projected = world.world_at(case, 'relationships')
    check_settlement(case, projected, facts, None)
    for field, change in (
            ('loss receipt', lambda w: w['settlements'][0]['losses']['roles'][0].__setitem__('lossToe', 1)),
            ('retreat ledger', lambda w: w['settlements'][0]['retreat'].__setitem__('afterCp', world.cp(4))),
            ('relation', lambda w: w['settlements'][0]['relationships'].__setitem__('kind', 'contact'))):
        changed = copy.deepcopy(projected)
        change(changed)
        try:
            check_settlement(case, changed, facts, None)
        except ValueError:
            checks += 1
        else:
            raise AssertionError(field + ' mutation escaped settlement checks')
    return checks


def run(evidence):
    rules = pinned_rules(evidence)
    rows = positive_worlds(evidence)
    matrix = verify_matrix(rules, rows)
    return dict(status='bounded AC-004 evidence only; no P01 admission',
                authenticPositiveRows=len(rows), **matrix,
                mutations=mutation_checks(rules, rows))


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--evidence', type=Path, default=EVIDENCE)
    args = parser.parse_args()
    print(json.dumps(run(args.evidence), sort_keys=True))
