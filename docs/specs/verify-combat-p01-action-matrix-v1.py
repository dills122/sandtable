#!/usr/bin/env python3
"""Source-cited, read-only AC-003 action matrix for the selected P01 content.

This classifies only the authenticated two-infantry R03 terminal. It deliberately
does not issue a legal-action set, an AC-003 certificate, or a P01 admission.
"""

import argparse
import gzip
import hashlib
import json
from collections import Counter
from importlib.util import module_from_spec, spec_from_file_location
from pathlib import Path

ROOT = Path(__file__).resolve().parent


def load(name, path):
    spec = spec_from_file_location(name, path)
    module = module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


action = load('p01_matrix_action', ROOT / 'verify-combat-p01-action-provenance-v1.py')
source = load('p01_matrix_source', ROOT / 'verify-combat-p01-source-conditions-v1.py')
result = action.result


def require(condition, label):
    if not condition:
        raise ValueError('P01-AC-003-MATRIX ' + label)


# These are source/proposal boundaries, not an exhaustive source-legal action
# inventory. The distinction between an absent component, a deliberate policy
# exclusion, and an unproved later choice is essential to fail-closed admission.
MODES = {
    'no-selection': ('Land 15.21-15.29; CMB-STEP-002/006', 'later-typed-closure-open'),
    'close-assault': ('Land 15.0-15.89; CMB-POL-001', 'later-typed-choice-open'),
    'gun-position': ('Land 11.0, 12.11-12.19', 'selected-content-absent'),
    'barrage': ('Land 12.0-12.6', 'selected-content-absent'),
    'anti-armor': ('Land 14.0-14.6', 'selected-content-absent'),
    'mandatory-zoc-assault': ('Land 10.11, 10.31-10.36; common charts 9.4', 'selected-content-absent'),
    'holding-off': ('Land 10.31-10.36', 'selected-content-absent'),
    'retreat-before-assault-decline': ('Land 13.0-13.28; CMB-POL-004', 'later-explicit-decision-open'),
    'retreat-before-assault-move': ('Land 13.0-13.28; CMB-STEP-AC-011', 'unsupported-in-bounded-profile'),
    'full-close-assault-assignment': ('Land 14.0, 15.1-15.29; CMB-POL-001', 'later-sealed-round-open'),
    'alternate-assignment': ('Land 14.0, 15.1-15.29', 'unsupported-in-bounded-profile'),
    'voluntary-surrender': ('Land 15.88; CMB-POL-009', 'source-legal-policy-excluded'),
    'automatic-surrender': ('Land 15.88; September errata 15.88, 50.12', 'later-trigger-fail-closed'),
    'probe-modifier': ('Land 15.0-15.89; CMB-POL-001', 'unsupported-in-bounded-profile'),
}


def classify(state, content, request):
    """Classify an already replay-authenticated terminal; reject altered shape.

    The caller must reconstruct state from original CreationRequest and eleven
    distinct R03 ledgers. This function cannot authenticate a parsed World.
    """
    fact = action.check_selected_structure(state, content, request)
    side = fact['actorSide']
    units = {unit['elementId']: unit for unit in state['world']['elements']}
    static = {unit['elementId']: unit['sideId'] for unit in content['elements']}
    actor = next(unit for key, unit in units.items() if static[key] == side)
    defender = next(unit for key, unit in units.items() if static[key] != side)
    actor_hex = actor['currentLocationId']
    defender_hex = defender['currentLocationId']
    clear_edges = {frozenset((edge['firstLocationId'], edge['secondLocationId']))
                   for edge in content['edges'] if edge['features'] == []}
    adjacent = frozenset((actor_hex, defender_hex)) in clear_edges
    require(fact['targetLocationId'] == defender_hex, 'defender target identity')
    require(fact['physicalShape'] in ('provisional-positive', 'provisional-empty'),
            'bounded physical partition')
    if fact['physicalShape'] == 'provisional-positive':
        require(adjacent and state['profile'] == 'armed' and
                actor['ammunition']['points'] == 10, 'positive selected assault input')
        assault_shape = 'provisional-adjacent'
    elif adjacent:
        require(state['profile'] == 'paid' and actor['ammunition']['points'] == 0,
                'adjacent empty must be paid and ammunitionless')
        assault_shape = 'no-loaded-acting-infantry'
    else:
        assault_shape = 'no-adjacent-opposing-target'

    # A target without ammunition does not surrender merely because the two
    # units are adjacent. Source conditions are inspected at the actual assault
    # or enemy-movement trigger, not at this Combat Position checkpoint.
    for unit in (actor, defender):
        require(not source.automatic_surrender(
            cohesion_at_trigger=unit['operationalState']['cohesionLevel'],
            ammunition_at_trigger=unit['ammunition']['points'],
            trigger='combat-position'), 'no surrender at position')
    require(fact['selectedParticipantCount'] == fact['selectedComponentCount'] == 2 and
            fact['weatherAreaBound'] is True, 'bounded participant and area facts')
    require(fact['sourceLegalActionExhaustive'] is False and fact['admitted'] is False,
            'provisional-only source interpretation')
    statuses = {key: value[1] for key, value in MODES.items()}
    if assault_shape != 'provisional-adjacent':
        for mode in ('close-assault', 'retreat-before-assault-decline',
                     'retreat-before-assault-move', 'full-close-assault-assignment',
                     'alternate-assignment', 'probe-modifier'):
            statuses[mode] = 'not-available-in-authenticated-row'
    return dict(physicalShape=fact['physicalShape'], assaultShape=assault_shape,
                actorSide=side, sourceLegalActionExhaustive=False,
                currentWeatherKindProved=False, typedOrdinalTwoDecisionProved=False,
                automaticSurrenderTransitionProved=False, admitted=False,
                publicActions=[], modeStatuses=statuses)


def audit(evidence_path):
    """Replay the original 300-row catalogue before inspecting any row's matrix."""
    evidence_path = Path(evidence_path)
    for name, expected in result.PINS.items():
        require(hashlib.sha256((evidence_path / name).read_bytes()).hexdigest() == expected,
                'source pin ' + name)
    summary = json.loads((evidence_path / 'summary.json').read_text())
    manifest = json.loads((evidence_path / 'manifest.json').read_text())
    partition = json.loads((evidence_path / 'bounded-profile-audit.json').read_text())
    require(len(manifest['chunks']) == 11 and
            len(summary['rows']) == len(partition['rows']) == summary['count'] == 300,
            'complete catalogue')
    content = action.p01.selected_content()
    counts = Counter()
    shapes = Counter()
    keys = set()
    index = 0
    for chunk in manifest['chunks']:
        data = (evidence_path / chunk['name']).read_bytes()
        require(hashlib.sha256(data).hexdigest() == chunk['sha256'] ==
                summary['chunkSha256'][chunk['name']], 'chunk pin')
        for line in gzip.decompress(data).splitlines():
            row = json.loads(line)
            require(row['index'] == index, 'contiguous index')
            record = row['r03']
            key = record['rowKey']
            require(key not in keys and key == summary['rows'][index]['rowKey'] ==
                    partition['rows'][index]['rowKey'], 'unique authenticated row key')
            keys.add(key)
            state = result.require_control_vector(record, result.authenticated_control(record))
            fact = classify(state, content, record['vectors']['parentInputLedgersCanonicalUtf8'][0])
            require(fact['physicalShape'] == partition['rows'][index]['partition'] and
                    fact['admitted'] is False and fact['publicActions'] == [],
                    'provisional partition and admission')
            counts[fact['physicalShape']] += 1
            shapes[fact['assaultShape']] += 1
            index += 1
    require(index == len(keys) == 300 and counts ==
            {'provisional-positive': 44, 'provisional-empty': 256}, 'bounded 44/256')
    require(shapes == {'provisional-adjacent': 44, 'no-loaded-acting-infantry': 12,
                       'no-adjacent-opposing-target': 244}, 'bounded action shapes')
    return dict(authenticatedRows=300, provisionalPositive=44, provisionalEmpty=256,
                actionShapes=dict(shapes), sourceLegalActionExhaustive=False,
                admittedRows=0, publicActionCount=0)


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('evidence', type=Path)
    print(audit(parser.parse_args().evidence))
