#!/usr/bin/env python3
"""Read-only AC-006 source-condition checks; never admits a P01 action."""

import argparse
import hashlib
import importlib.util
import json
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parent


def load(name):
    spec = importlib.util.spec_from_file_location(name, ROOT / name)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


evidence = load('verify-combat-actual-moved-repeat-position-evidence-v1.py')
p01 = evidence.p01

RULE_HASHES = {
    'CNA_LandGameRules.pdf': 'b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99',
    'ERR_CampaignforNorthAfrica.pdf': '5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb',
}
ASSAULT_TRIGGERS = frozenset(('close-assault-against-unit', 'anti-armor-assault-against-unit'))
OTHER_TRIGGERS = frozenset(('combat-position', 'existing-adjacency', 'enemy-moved-adjacent',
                            'barrage-against-unit', 'post-cost-settlement'))


def require(condition, label):
    if not condition:
        raise ValueError('P01-AC-006 ' + label)


def automatic_surrender(*, cohesion_at_trigger, ammunition_at_trigger, trigger):
    """Classify the *target unit* at the source event, never from a later resource balance.

    Land 15.88/September 15.88 distinguish assault at -17 from enemy movement adjacent
    at -26. September 50.12 requires an actual anti-armor or close assault for a unit
    without ammunition; adjacency, enemy ZOC and barrage alone do not suffice.
    """
    require(type(cohesion_at_trigger) is int, 'typed pre-event cohesion')
    require(type(ammunition_at_trigger) is int and ammunition_at_trigger >= 0,
            'typed pre-event carried ammunition')
    require(type(trigger) is str and trigger in ASSAULT_TRIGGERS | OTHER_TRIGGERS,
            'typed source event')
    if trigger == 'enemy-moved-adjacent':
        return cohesion_at_trigger <= -26
    if trigger in ASSAULT_TRIGGERS:
        return cohesion_at_trigger <= -17 or ammunition_at_trigger == 0
    return False


def require_no_unhandled_surrender(*, cohesion_at_trigger, ammunition_at_trigger, trigger):
    """Fail closed for any source event this bounded profile cannot settle."""
    require(not automatic_surrender(cohesion_at_trigger=cohesion_at_trigger,
                                    ammunition_at_trigger=ammunition_at_trigger,
                                    trigger=trigger), 'automatic surrender requires separate contract')


def source_matrix():
    """Literal source-boundary witnesses, including the post-payment counterexample."""
    cases = (
        (-16, 10, 'close-assault-against-unit', False),
        (-17, 10, 'close-assault-against-unit', True),
        (-17, 10, 'anti-armor-assault-against-unit', True),
        (-17, 10, 'enemy-moved-adjacent', False),
        (-25, 10, 'enemy-moved-adjacent', False),
        (-26, 10, 'enemy-moved-adjacent', True),
        (0, 0, 'close-assault-against-unit', True),
        (0, 0, 'anti-armor-assault-against-unit', True),
        (0, 0, 'existing-adjacency', False),
        (0, 0, 'enemy-moved-adjacent', False),
        (0, 0, 'barrage-against-unit', False),
        (0, 0, 'combat-position', False),
        (0, 0, 'post-cost-settlement', False),
        # An originally supplied defender pays 10 -> 0 in a 10-TOE assault. The
        # source event inspected its original 10 points, not the resulting zero.
        (0, 10, 'close-assault-against-unit', False),
    )
    for cohesion, ammo, trigger, expected in cases:
        require(automatic_surrender(cohesion_at_trigger=cohesion,
                                    ammunition_at_trigger=ammo, trigger=trigger) is expected,
                f'source matrix {cohesion}/{ammo}/{trigger}')
    return len(cases)


def check_rba_prior_contract():
    """Check the existing selection contract only; this is not an ordinal-two P01 trace."""
    selection = load('verify-combat-actual-selection-v1.py')
    checked = 0
    for side in ('axis', 'commonwealth'):
        for variant in ('selected', 'rba-before-open-unavailable',
                        'rba-expired', 'rba-unavailable'):
            _, controls, _, events = selection.semantic_expectations(side, variant)
            effects = [json.loads(event)['effect']['kind'] for event in events]
            if variant == 'selected':
                require(effects.count('rba-declined') == 1
                        and controls[-1]['declineReceiptId'] is not None
                        and controls[-1]['selectionOutcome'] == 'selected',
                        'prior explicit defender decline')
            else:
                require('rba-declined' not in effects
                        and effects.count('selection-cancelled') == 1
                        and controls[-1]['selectionOutcome'] == 'cancelled'
                        and controls[-1]['declineReceiptId'] is None
                        and controls[-1]['stepIndex'] == 6
                        and controls[-1]['closed'],
                        'prior missing-response cancellation without invented decline')
            checked += 1
    return checked


def audit(evidence_path):
    evidence_path = Path(evidence_path)
    for name, expected in RULE_HASHES.items():
        require(hashlib.sha256((evidence_path / name).read_bytes()).hexdigest() == expected,
                'physical source pin ' + name)
    content = p01.selected_content()
    static = {unit['elementId']: unit['sideId'] for unit in content['elements']}
    clear_edges = {frozenset((edge['firstLocationId'], edge['secondLocationId']))
                   for edge in content['edges'] if edge['features'] == []}
    count = Counter()
    original_shape = p01.physical_shape

    def inspect_authenticated_state(state, selected_content):
        # assess() calls physical_shape only after frozen R03 replay has authenticated
        # the entire parent chain and exact ordinal-two terminal. Observe that same
        # trusted state in the existing all-row audit, avoiding a second replay.
        require(state['activeCycle']['ordinal'] == 2
                and state['sequencePosition']['positionId'].endswith(
                    '.combat.position-determination'), 'actual pre-assault position')
        require(selected_content == content, 'pinned selected content')
        units = state['world']['elements']
        require(len(units) == 2 and set(static) == {unit['elementId'] for unit in units},
                'original two-unit source profile')
        for unit in units:
            ammo = unit['ammunition']['points']
            cohesion = unit['operationalState']['cohesionLevel']
            require_no_unhandled_surrender(cohesion_at_trigger=cohesion,
                                           ammunition_at_trigger=ammo,
                                           trigger='combat-position')
            # These genuine terminals do not demonstrate an active surrender
            # transition. A low-Cohesion profile would need its own admission path.
            require(cohesion > -17, 'unsupported threshold at terminal')
        actor = next(unit for unit in units
                     if static[unit['elementId']] == state['activeCycle']['actingSide'])
        defender = next(unit for unit in units if unit is not actor)
        adjacent = frozenset((actor['currentLocationId'],
                              defender['currentLocationId'])) in clear_edges
        if adjacent and actor['ammunition']['points'] == 0:
            require(state['profile'] == 'paid' and state['priorCycleTargetUses']
                    and not state['targetUses'], 'post-cost zero is historical')
            require_no_unhandled_surrender(
                cohesion_at_trigger=actor['operationalState']['cohesionLevel'],
                ammunition_at_trigger=actor['ammunition']['points'],
                trigger='existing-adjacency')
            count['postCostAdjacentRows'] += 1
        count['authenticatedRows'] += 1
        return original_shape(state, selected_content)

    p01.physical_shape = inspect_authenticated_state
    try:
        # This authenticates original CreationRequest + eleven separate ledgers,
        # the R03 suffix, all pinned chunks and the exact 44/256/0 partition.
        ancestry = evidence.audit(evidence_path)
    finally:
        p01.physical_shape = original_shape
    require(ancestry == dict(verifiedRows=300, provisionalPositive=44,
                             provisionalEmpty=256, admittedRows=0), 'ancestor partition')
    require(count == {'authenticatedRows': 300, 'postCostAdjacentRows': 12},
            'authenticated source-condition census')
    return dict(**ancestry, sourceMatrixCases=source_matrix(),
                postCostAdjacentRows=count['postCostAdjacentRows'],
                priorSelectionRbaTraces=check_rba_prior_contract(),
                sourceConditionAdmission='unsupported', publicActions=[])


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--evidence', required=True, type=Path)
    args = parser.parse_args()
    print(json.dumps(audit(args.evidence), sort_keys=True))
