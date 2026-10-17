#!/usr/bin/env python3
"""Offline P01 AC-005 geometry/custody proof over authenticated R03 terminals.

This certificate is deliberately read-only. It cannot admit an action or issue a P01 event.
"""

import argparse
import copy
import json
from collections import Counter, deque
from pathlib import Path

ROOT = Path(__file__).resolve().parent


def load(name):
    import importlib.util
    spec = importlib.util.spec_from_file_location(name, ROOT / name)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


result = load('verify-combat-p01-result-gates-v1.py')
ancestry = result.ancestry
p01 = ancestry.p01


def require(ok, label):
    if not ok:
        raise ValueError('P01-AC-005 ' + label)


def graph_from_content(content):
    locations = {item['locationId']: item for item in content['locations']}
    require(len(locations) == 6 and len(content['locations']) == 6, 'six unique locations')
    require(all(item['kind'] == 'hex' and item['terrainId'] == 'land.terrain.clear'
                for item in locations.values()), 'Clear hex terrain')
    graph = {name: set() for name in locations}
    edges = set()
    for edge in content['edges']:
        first, second = edge['firstLocationId'], edge['secondLocationId']
        require(first in graph and second in graph and first != second and edge['features'] == [],
                'traversable feature-free edge')
        key = frozenset((first, second))
        require(key not in edges, 'unique edge')
        edges.add(key)
        graph[first].add(second)
        graph[second].add(first)
    require(len(edges) == 5, 'five-edge topology')
    expected = ['axis-supply', 'axis-rear', 'assault-west', 'assault-east',
                'commonwealth-rear', 'commonwealth-supply']
    require(set(locations) == set(expected)
            and edges == {frozenset((a, b)) for a, b in zip(expected, expected[1:])},
            'pinned selected topology')
    scenarios = content['scenarios']
    require(len(scenarios) == 1, 'one selected scenario')
    anchors = scenarios[0]['retreatSupplyAnchors']
    require(len(anchors) == 2 and {a['sideId'] for a in anchors} == {'axis', 'commonwealth'}
            and all(a['kind'] == 'friendly-supply-direction' for a in anchors),
            'two friendly supply anchors')
    require({a['sideId']: a['locationId'] for a in anchors}
            == {'axis': 'axis-supply', 'commonwealth': 'commonwealth-supply'},
            'pinned supply directions')
    return graph


def unique_path(graph, start, end, blocked=()):
    """Find sole shortest path, rejecting an ambiguous same-cost relocation."""
    require(start in graph and end in graph, 'path endpoint')
    excluded = set(blocked) - {start}
    queue = deque([(start,)])
    found = []
    best = None
    while queue:
        path = queue.popleft()
        if best is not None and len(path) > best:
            break
        at = path[-1]
        if at == end:
            found.append(path)
            best = len(path)
            continue
        for neighbor in sorted(graph[at] - excluded - set(path)):
            queue.append((*path, neighbor))
    require(len(found) == 1, 'one unobstructed least-cost path')
    return list(found[0])


def bounded_custody_routes(graph, origin, victim_final, captor_final):
    guard = unique_path(graph, origin, captor_final, (victim_final,))
    reunion = unique_path(graph, origin, victim_final)
    require(len(guard) - 1 <= 3 and len(reunion) - 1 <= 8,
            'Land §§28.12/28.24 guard and reunion limits')
    require(not any(hex_id == victim_final for hex_id in guard[1:]),
            'guard avoids victim final occupied hex')
    return guard, reunion


def certify_row(state, content):
    """Check post-R03 World, both retreat dispositions and eight placement shapes."""
    graph = graph_from_content(content)
    static = {x['elementId']: x for x in content['elements']}
    members = {x['elementId']: x for x in state['world']['elements']}
    require(len(content['elements']) == len(static) == len(members)
            == len(state['world']['representations']) == 2
            and set(static) == set(members), 'complete original participant set')
    side = state['activeCycle']['actingSide']
    require(side in ('axis', 'commonwealth'), 'authenticated acting side')
    actor = next((members[k] for k in static if static[k]['sideId'] == side), None)
    defender = next((members[k] for k in static if static[k]['sideId'] != side), None)
    require(actor is not None and defender is not None and actor != defender, 'one actor and defender')
    require(state['profile'] == 'armed' and not state['targetUses']
            and not state['priorCycleTargetUses'] and not state['attackHistory'],
            'fresh ordinal-two attack basis')
    require(all(not state['world'][key] for key in ('custodyLots', 'guards', 'settlements',
                                                   'futureObligations', 'replacementEntitlements',
                                                   'relationships', 'brokenVehicleLots',
                                                   'cohesionCauses')),
            'no inherited custody/settlement blocker')
    representations = {r['boundElementIds'][0]: r for r in state['world']['representations']
                       if r['bindingKind'] == 'independent-element' and len(r['boundElementIds']) == 1}
    require(set(representations) == set(members), 'independent representation binding')
    for member, cp in ((actor, 4), (defender, 0)):
        frozen = static[member['elementId']]
        require(frozen['organizationId'] == 'land.organization.battalion'
                and frozen['placementMode'] == 'independent'
                and frozen['mobilityId'] == 'land.mobility.non-motorized'
                and len(frozen['components']) == len(member['components']) == 1
                and frozen['components'][0]['componentClassId'] == 'land.combat-component.infantry'
                and member['components'][0]['currentToe'] == 10
                and member['ammunition']['points'] == 10
                and member['operationalState']['cohesionLevel'] == 0
                and member['operationalState']['capabilityPointsExpended']
                == {'numerator': cp, 'denominator': 1}
                and representations[member['elementId']]['currentLocationId'] == member['currentLocationId'],
                'exact singleton infantry/current-resource profile')
    # Printed §15.88 automatic surrender is a separate condition. Post-commit attacker ammo 0
    # is not a fresh pre-assault surrender trigger. No voluntary-surrender choice is admitted.
    origin = actor['currentLocationId']
    target = defender['currentLocationId']
    require(origin != target and target in graph[origin], 'adjacent assault participants')
    require({origin, target} == {'assault-west', 'assault-east'}, 'selected original hexes')
    defender_side = static[defender['elementId']]['sideId']
    anchor = {'axis': 'axis-supply', 'commonwealth': 'commonwealth-supply'}[defender_side]
    prior_supply = unique_path(graph, target, anchor)
    retreat_choices = []
    occupied = {origin, target}
    for node in sorted(graph[target] - occupied):
        if (len(unique_path(graph, node, anchor)) == len(prior_supply) - 1
                and len(unique_path(graph, origin, node)) == 3):
            retreat_choices.append(node)
    require(len(retreat_choices) == 1, 'one source-directed retreat hex')
    destination = retreat_choices[0]
    require(destination not in occupied and destination in graph[target], 'legal one-hex retreat')
    require(actor['operationalState']['capabilityPointsExpended']['numerator'] + 5 == 9
            and defender['operationalState']['capabilityPointsExpended']['numerator'] + 3 + 1 == 4,
            'paid assault/defense/retreat CP within CPA 10')

    shapes = []
    for moved in (False, True):
        final = {actor['elementId']: origin,
                 defender['elementId']: destination if moved else target}
        for victim, captor in ((actor, defender), (defender, actor)):
            capture_origin = victim['currentLocationId']  # CMB-SET-004 pre-retreat convention
            victim_final = final[victim['elementId']]
            captor_final = final[captor['elementId']]
            guard, reunion = bounded_custody_routes(
                graph, capture_origin, victim_final, captor_final)
            shapes.append(dict(actorSide=side, defenderMoves=moved,
                               victimSide=static[victim['elementId']]['sideId'],
                               retreat=[target, destination] if moved else [target],
                               guard=guard, reunion=reunion))
    require(len(shapes) == 4, 'four placement shapes per actor orientation')
    return shapes


def mutation_checks(state, content):
    def rejected(label, change):
        changed_state, changed_content = copy.deepcopy(state), copy.deepcopy(content)
        change(changed_state, changed_content)
        try:
            certify_row(changed_state, changed_content)
        except (ValueError, KeyError, StopIteration):
            return
        raise AssertionError('P01-AC-005 mutation escaped: ' + label)

    rejected('entered edge feature', lambda s, c: c['edges'][0]['features'].append('land.hexside.escarpment'))
    rejected('impassable retreat terrain', lambda s, c: next(x for x in c['locations']
             if x['locationId'].endswith('-rear')).__setitem__('terrainId', 'land.terrain.impassable'))
    rejected('missing supply anchor', lambda s, c: c['scenarios'][0]['retreatSupplyAnchors'].pop())
    rejected('alternate edge/path', lambda s, c: c['edges'].append(copy.deepcopy(c['edges'][0])))
    rejected('enemy blocker at retreat hex', lambda s, c: s['world']['elements'].append(
             dict(s['world']['elements'][0], elementId='injected-blocker', currentLocationId='commonwealth-rear')))
    rejected('guard donor shortage', lambda s, c: s['world']['elements'][0]['components'][0]
             .__setitem__('currentToe', 0))
    rejected('pre-assault ammunitionless surrender', lambda s, c: s['world']['elements'][0]
             ['ammunition'].__setitem__('points', 0))
    rejected('assault surrender cohesion', lambda s, c: s['world']['elements'][0]
             ['operationalState'].__setitem__('cohesionLevel', -17))
    rejected('representation displacement', lambda s, c: s['world']['representations'][0]
             .__setitem__('currentLocationId', 'axis-rear'))
    rejected('retreat CP shortage', lambda s, c: s['world']['elements'][1]
             ['operationalState']['capabilityPointsExpended'].__setitem__('numerator', 10))
    return 10


def source_quantity_bounds(rules):
    """Conservatively bound all adopted chart losses, refusal and captive lots."""
    require(len(rules['losses']) == 360 and len(rules['captureShares']) == 6,
            'normalized source catalogue')
    attacker_loss = max((10 * entry['lossPercent'] + 99) // 100
                        for entry in rules['losses'] if entry['role'] == 'attacker')
    # Refusal is a ten-point defender surcharge; include it even when the chart does not
    # require retreat, which is a conservative bound for this geometry-only certificate.
    defender_loss = max(10 * (entry['lossPercent'] + 10) // 100
                        for entry in rules['losses'] if entry['role'] == 'defender')
    max_loss = max(attacker_loss, defender_loss)
    max_share = max(x['percent'] for x in rules['captureShares'])
    max_lot = (max_loss * max_share + 99) // 100
    require(max_loss <= 3 and 10 - max_loss >= 7 and max_lot <= 3
            and max_lot <= 5, 'one-TOE donor and corrected moving guard ratio 1:5')
    return dict(maxLossToe=max_loss, minimumSurvivingDonorToe=10 - max_loss,
                maxCaptiveToe=max_lot, movingGuardCapacityToe=5)


def reachable_capture_examples(rules):
    """Select chart witnesses for each reachable capture/retreat combination."""
    coordinates = [10 * tens + ones for tens in range(1, 7) for ones in range(1, 7)]
    effects = {entry['differential']: entry for entry in rules['effects']}
    found = {}
    for differential in range(-2, 3):
        effect = effects[differential]
        for attacker in coordinates:
            for defender in coordinates:
                a_sum = attacker // 10 + attacker % 10
                d_sum = defender // 10 + defender % 10
                captured = (a_sum in effect['attackerCaptureSums']
                            or d_sum in effect['defenderCaptureSums'])
                if not captured:
                    continue
                facts = result.independent_result(rules, differential, attacker, defender, 1)
                key = (facts['capturedRole'], bool(facts['requiredRetreat']))
                found.setdefault(key, facts)
    require(set(found) == {('attacker', False), ('attacker', True), ('defender', False)},
            'source-reachable capture/retreat joint shapes')
    # Defender capture with required retreat has no normalized chart witness. Its placement
    # geometry is still checked as a policy shape, but is never reported as reachable.
    return found


def check_projected_paths(state, rules, content, examples):
    """Compare independent graph paths with actual bounded settlement World projections."""
    side = state['activeCycle']['actingSide']
    shapes = {(s['defenderMoves'], s['victimSide']): s for s in certify_row(state, content)}
    witnesses = reachable_capture_examples(rules)
    checked = 0
    for (role, required), facts in witnesses.items():
        dispositions = ('retreat', 'refuse-retreat') if required else ('not-required',)
        for disposition in dispositions:
            moved = disposition == 'retreat'
            victim_side = side if role == 'attacker' else (
                'commonwealth' if side == 'axis' else 'axis')
            paths = shapes[(moved, victim_side)]
            for custody in ('relocate-and-guard', 'leave-unguarded'):
                case = result.initial_case(state)
                case.update(differential=facts['differential'],
                            attackerCoordinate=facts['attackerCoordinate'],
                            defenderCoordinate=facts['defenderCoordinate'],
                            captureDie=facts['captureDie'], retreatChoice=disposition,
                            custodyChoice=custody)
                projected = result.world.world_at(case, 'relationships')
                settlement = projected['settlements'][0]
                require(settlement['custody'] is not None and len(projected['custodyLots']) == 1,
                        'projected positive custody')
                if moved:
                    require(settlement['retreat']['route'] == paths['retreat'],
                            'projected retreat equals unique supply path')
                route = paths['guard'] if custody == 'relocate-and-guard' else paths['reunion']
                require(settlement['custody']['route'] == route,
                        'projected custody equals independent graph path')
                lot = projected['custodyLots'][0]
                require(lot['quantity'] <= 3 and lot['originLocationId'] == route[0],
                        'capture origin and bounded lot')
                if custody == 'relocate-and-guard':
                    require(len(projected['guards']) == 1 and projected['guards'][0]['toe'] == 1
                            and lot['currentLocationId'] == route[-1]
                            and settlement['custody']['donorToeBefore'] >= 7
                            and settlement['custody']['donorToeAfter'] >= 6,
                            'one-TOE guard capacity and donor conservation')
                else:
                    require(not projected['guards'] and lot['status'] == 'escaped'
                            and lot['currentLocationId'] is None
                            and len(projected['replacementEntitlements']) == 1,
                            'unguarded escape and delayed replacement')
                checked += 1
    require(checked == 8, 'eight reachable projected branch witnesses per orientation')
    return checked


def run(evidence):
    # Audit all original CreationRequests and eleven independently retained ledgers before
    # using the 44 rows. The sibling AC-004 verifier binds physical sources and chart bounds.
    summary = ancestry.audit(evidence)
    require(summary == dict(verifiedRows=300, provisionalPositive=44,
                            provisionalEmpty=256, admittedRows=0), 'complete pinned ancestry')
    rules = result.pinned_rules(evidence)
    quantities = source_quantity_bounds(rules)
    rows = result.positive_worlds(evidence)
    matrix = result.verify_matrix(rules, rows)
    require(matrix['settlementWorlds'] == {'axis': 9728, 'commonwealth': 9728},
            'complete bounded donor/lot result matrix')
    content = p01.selected_content()
    placements = Counter()
    examples = {}
    representatives = {}
    for index, state, _, _ in rows:
        require(p01.physical_shape(state, content)[0] == 'provisional-positive',
                f'authenticated positive {index}')
        for shape in certify_row(state, content):
            key = (shape['actorSide'], shape['defenderMoves'], shape['victimSide'])
            placements[key] += 1
            examples.setdefault(key, shape)
        representatives.setdefault(state['activeCycle']['actingSide'], state)
    require(len(placements) == 8 and set(placements.values()) == {22},
            'eight placement cases across all 44 rows')
    require(max(len(v['guard']) - 1 for v in examples.values()) == 2
            and max(len(v['reunion']) - 1 for v in examples.values()) == 1,
            'bounded route distances')
    projections = sum(check_projected_paths(state, rules, content, examples)
                      for state in representatives.values())
    negatives = mutation_checks(rows[0][1], content)
    return dict(status='bounded AC-005 evidence only; no P01 admission',
                authenticRows=summary['verifiedRows'], provisionalPositive=44,
                provisionalEmpty=256, admittedRows=0, placementShapes=dict(
                    axis=88, commonwealth=88), guardMaxHexes=2, reunionMaxCp=1,
                sourceResultMatrix=matrix['settlementWorlds'], quantityBounds=quantities,
                reachableProjectedPaths=projections, negativeMutations=negatives)


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--evidence', type=Path, default=result.EVIDENCE)
    args = parser.parse_args()
    print(json.dumps(run(args.evidence), sort_keys=True))
