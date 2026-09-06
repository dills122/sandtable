#!/usr/bin/env python3
"""TASK-001 source diagnostics; candidate evaluation is not production admission."""
import copy
import importlib.util
import json
import sys
from collections import Counter
from itertools import product
from pathlib import Path

ROOT = Path(__file__).resolve().parent
FIXTURE = ROOT / 'fixtures/combat-selected-source-v1.json'
COORDS = [10*a+b for a in range(1, 7) for b in range(1, 7)]
DIFFS = list(range(-2, 3))


def require(condition, message):
    if not condition:
        raise AssertionError(message)


def expand(bands):
    result = {}
    for value, low, high in bands:
        require(type(value) is int and type(low) is int and type(high) is int,
                'integer source cells required')
        require(11 <= low <= high <= 66, 'source interval outside chart')
        for coordinate in COORDS:
            if low <= coordinate <= high:
                require(coordinate not in result, 'overlapping source bands')
                result[coordinate] = value
    return result


def tables(data):
    return {(role, diff): expand(data[f'{role}_loss_bands'][str(diff)])
            for role in ('attacker', 'defender') for diff in DIFFS}


def gaps(values):
    return [(role, diff, coordinate) for (role, diff), table in values.items()
            for coordinate in COORDS if coordinate not in table]


def strict_admission(values):
    if gaps(values):
        raise ValueError('source table incomplete; owner ruling required before admission')


def loss(values, role, differential, coordinate):
    if role not in ('attacker', 'defender') or differential not in DIFFS or coordinate not in COORDS:
        raise ValueError('outside selected ordered-d6 surface')
    return values[(role, differential)][coordinate]


def rejected(call, label):
    try:
        call()
    except (ValueError, AssertionError):
        return
    raise AssertionError(label)


def due_scope(turn, stage):
    if type(turn) is not int or turn < 1 or type(stage) is not int or not 1 <= stage <= 3:
        raise ValueError('invalid game scope')
    due = (turn - 1) * 3 + stage - 1 + 12
    return due // 3 + 1, due % 3 + 1


def move_cost(spent, relations, terrain=1, release=None):
    """Selected integer-CP infantry arithmetic, not topology or Core movement authority."""
    cost = max(({'contact': 2, 'engaged': 4}[r] for r in relations), default=0) + terrain
    ceiling = {'I': 10, 'II': 5, None: 15}[release]
    after = spent + cost
    if after > ceiling:
        raise ValueError('voluntary ceiling exceeded')
    return after, max(0, after - 10) - max(0, spent - 10)


def main():
    data = json.loads(FIXTURE.read_text())
    require(data['ordered_coordinates'] == COORDS, 'coordinate order changed')
    raw = tables(data)
    missing = gaps(raw)
    require(missing == [('defender', 2, c) for c in (34, 35, 36)], 'unexpected source gaps')
    for (role, diff), table in raw.items():
        optical = data['optical_loss_percent_by_coordinate'][f'{role}:{diff}']
        require([table.get(c) for c in COORDS] == optical, 'visual/optical disagreement')
    rejected(lambda: strict_admission(raw), 'incomplete source admitted')
    rejected(lambda: loss(raw, 'attacker', -2, 18), 'invalid d6 accepted through printed13-18')
    rejected(lambda: loss(raw, 'defender', 3, 11), 'unselected differential accepted')
    mutated = copy.deepcopy(data['attacker_loss_bands']['0']) + [[0, 11, 11]]
    rejected(lambda: expand(mutated), 'overlap accepted')

    proposed = data['proposed_ruling']
    require(proposed['status'] == 'pending-owner-decision', 'research must retain pending status')
    candidate = copy.deepcopy(raw)
    for c in proposed['coordinates']:
        require(c not in candidate[('defender', 2)], 'proposal overwrites a defined source cell')
        candidate[('defender', 2)][c] = proposed['loss_percent']
    strict_admission(candidate)  # Hypothetical numeric closure only; proposal is NOT adopted.
    require(sum(a.get(c) != candidate[k].get(c) for k, a in raw.items() for c in COORDS) == 3,
            'candidate changed more than three missing cells')

    morale = expand(data['cohesion_zero_morale_bands'])
    require(len(morale) == 36 and Counter(morale.values()) == {0: 34, 1: 1, -1: 1}, 'morale row')
    weights = Counter(morale[a] - morale[d] for a, d in product(COORDS, repeat=2))
    require(dict(weights) == {-2: 1, -1: 68, 0: 1158, 1: 68, 2: 1}, 'differential weights')
    capture_paths = 0
    max_losses = [0, 0]
    rows = 0
    for diff, ac, dc in product(DIFFS, COORDS, COORDS):
        a_sum, d_sum = ac//10 + ac%10, dc//10 + dc%10
        ca = a_sum in data['attacker_capture_sums'][str(diff)]
        cd = d_sum in data['defender_capture_sums'][str(diff)]
        require(not (ca and cd), 'both roles captured outside selected surface')
        if ca or cd:
            capture_paths += weights[diff]
        retreat = d_sum in data['defender_retreat_one_hex_sums'][str(diff)]
        for refusal in range(2 if retreat else 1):
            al = (10*loss(candidate, 'attacker', diff, ac)+99)//100
            dl = (10*(loss(candidate, 'defender', diff, dc)+10*refusal))//100
            max_losses = [max(max_losses[0], al), max(max_losses[1], dl)]
            for share in data['capture_share_percent_by_die'] if ca or cd else [0]:
                for total, captured in ((al, (al*share+99)//100 if ca else 0),
                                        (dl, (dl*share+99)//100 if cd else 0)):
                    require(0 <= captured <= total <= 3, 'loss/capture bounds')
                    require((10-total)+captured+(total-captured) == 10, 'TOE conservation')
                rows += 1
    require(max_losses == [3, 3] and capture_paths == 44208, 'candidate semantic envelope')
    # A pre-existing research oracle supplies expected seeded losses/flags.
    sys.dont_write_bytecode = True
    spec = importlib.util.spec_from_file_location('rng_oracle', ROOT / 'verify-combat-rng.py')
    oracle = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(oracle)
    for v in oracle.VECTORS:
        _, _, dice, _, diff, ap, dp, engaged, retreat, *_ = v
        ac, dc = int(dice[4:6]), int(dice[6:8])
        require(loss(candidate, 'attacker', diff, ac) == ap, 'seeded attacker loss differs')
        require(loss(candidate, 'defender', diff, dc) == dp, 'seeded defender loss differs')
        require((ac//10+ac%10 in data['attacker_engaged_sums'][str(diff)]) == engaged, 'Engaged flag')
        require(int(dc//10+dc%10 in data['defender_retreat_one_hex_sums'][str(diff)]) == retreat, 'Retreat flag')

    calendar_vectors = [((1, 1), (5, 1)), ((1, 3), (5, 3)), ((4, 3), (8, 3)),
                        ((48, 2), (52, 2)), ((49, 1), (53, 1))]
    for start, expected in calendar_vectors:
        require(due_scope(*start) == expected, '12-stage delay including turn/year boundary')
    for invalid in [(0, 1), (1, 0), (1, 4), (True, 1)]:
        rejected(lambda value=invalid: due_scope(*value), 'invalid calendar scope accepted')
    movement_vectors = [(5, ['engaged'], None, (10, 0)), (6, ['engaged'], None, (11, 1)),
                        (10, ['engaged'], None, (15, 5)), (5, ['contact'], None, (8, 0)),
                        (5, ['contact', 'engaged', 'engaged'], None, (10, 0)),
                        (10, [], None, (11, 1)), (2, ['contact'], 'II', (5, 0)),
                        (5, ['engaged'], 'I', (10, 0))]
    for spent, relations, release, expected in movement_vectors:
        require(move_cost(spent, relations, release=release) == expected, 'break-off precedence/DP')
    for spent, relations, release in [(11, ['engaged'], None), (6, ['engaged'], 'I'), (3, ['contact'], 'II')]:
        rejected(lambda: move_cost(spent, relations, release=release), 'excess voluntary spend accepted')
    print(json.dumps({'diagnostics': 'passed', 'source_admission': 'BLOCKED: CMB-SRC-RUL-001 pending',
                      'loss_coordinates': 360, 'defined_source_values': 357, 'gaps': missing,
                      'morale_coordinates': 36, 'candidate_joint_coordinates': 6480,
                      'candidate_settlement_cases': rows, 'candidate_capture_paths': capture_paths,
                      'seeded_cross_checks': len(oracle.VECTORS), 'calendar_vectors': len(calendar_vectors),
                      'movement_vectors': len(movement_vectors),
                      'limit': 'research/candidate arithmetic only; no adopted repair or runtime proof'}, indent=2))


if __name__ == '__main__':
    main()
