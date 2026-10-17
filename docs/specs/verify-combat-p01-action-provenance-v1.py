#!/usr/bin/env python3
"""Read-only P01 AC-003 participant/action provenance audit.

The authenticated R03 terminal proves selected structural facts. It does not
carry a current Weather determination or prove every source-legal choice.
Consequently this verifier never issues an AC-003 certificate or admits P01.
"""

import argparse
import gzip
import hashlib
import json
from collections import Counter
from pathlib import Path

from importlib.util import module_from_spec, spec_from_file_location

ROOT = Path(__file__).resolve().parent


def load(name, path):
    spec = spec_from_file_location(name, path)
    module = module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


result = load('p01_action_result', ROOT / 'verify-combat-p01-result-gates-v1.py')
ancestry = result.ancestry
p01 = ancestry.p01


def require(ok, label):
    if not ok:
        raise ValueError('P01-AC-003 ' + label)


def unit_ref(element, side, binding):
    return dict(creationBinding=binding, originalSide=side,
                elementId=element['elementId'])


def check_selected_structure(state, content, request):
    """Check facts derivable from an already authenticated R03 terminal.

    Callers must replay the original CreationRequest and eleven distinct parent
    ledgers before passing state. A caller-supplied parsed World is not proof.
    """
    cycle = state['activeCycle']
    world = state['world']
    side = cycle['actingSide']
    require(side in ('axis', 'commonwealth') and
            state['originalCycle']['actingSide'] == side, 'acting side')
    require(request['content'] == dict(schemaVersion=7,
                                       formatId=content['formatId'],
                                       packId=content['packId'],
                                       rulesetId=content['rulesetId'],
                                       hash=cycle['contentHash'],
                                       scenarioId=cycle['scenarioId']),
            'original selected content binding')
    require(request['configurationHash'] == cycle['admittedPolicyBundleDigest'] and
            request['rulesetHash'] == cycle['rulesetHash'] and
            request['setupHash'] == cycle['setupHash'], 'original authority binding')
    static = {item['elementId']: item for item in content['elements']}
    elements = {item['elementId']: item for item in world['elements']}
    require(len(content['elements']) == len(world['elements']) == len(static) == len(elements) == 2
            and set(static) == set(elements), 'complete selected participants')
    require({item['sideId'] for item in static.values()} == {'axis', 'commonwealth'} and
            all(item['organizationId'] == 'land.organization.battalion' and
                item['placementMode'] == 'independent' and
                item['mobilityId'] == 'land.mobility.non-motorized' and
                item['combatClassificationId'] == 'land.combat-classification.combat-unit' and
                len(item['components']) == 1 and
                item['components'][0]['componentClassId'] == 'land.combat-component.infantry'
                for item in static.values()), 'only selected infantry components')
    actor = next(item for item in static.values() if item['sideId'] == side)
    defender = next(item for item in static.values() if item['sideId'] != side)
    binding = world['creationBinding']
    require(binding.startswith('creation.') and
            all(item['sourceParentFormationId'] == item['currentParentFormationId']
                == static[key]['parentFormationId'] and
                len(item['components']) == 1 and
                item['components'][0]['componentId'] == static[key]['components'][0]['componentId']
                for key, item in elements.items()), 'original unit and component binding')
    references = {key: unit_ref(elements[key], static[key]['sideId'], binding) for key in static}
    require(len(state['members']) == 1 and state['members'][0]['unit'] == references[actor['elementId']],
            'acting member original-unit identity')
    for label in ('originalMovementEnd', 'movementEnd'):
        entries = state[label]['endLocations']
        require(len(entries) == 2 and
                {json.dumps(item['unit'], sort_keys=True) for item in entries}
                == {json.dumps(ref, sort_keys=True) for ref in references.values()},
                label + ' original-unit membership')
    require({item['unit']['elementId']: item['locationId'] for item in
             state['movementEnd']['endLocations']}
            == {key: item['currentLocationId'] for key, item in elements.items()},
            'current segment end locations')
    reps = world['representations']
    require(len(reps) == 2 and len({item['representationId'] for item in reps}) == 2 and
            all(item['bindingKind'] == 'independent-element' and
                len(item['boundElementIds']) == 1 and
                item['boundElementIds'][0] in elements and
                item['currentLocationId'] == elements[item['boundElementIds'][0]]['currentLocationId']
                for item in reps) and
            {item['boundElementIds'][0] for item in reps} == set(elements),
            'independent representation bijection')
    locations = {item['locationId'] for item in content['locations']}
    assignments = content['weatherAreaAssignments']
    require(len(assignments) == len(locations) and
            {item['locationId'] for item in assignments} == locations and
            {item['weatherArea'] for item in assignments} == {'a'},
            'selected weather-area map')
    # The content pins an area, not current Weather kind. No authenticated
    # terminal field provides the latter, so normal Weather remains unproved.
    target = elements[defender['elementId']]['currentLocationId']
    actor_hex = elements[actor['elementId']]['currentLocationId']
    occupants = [key for key, item in elements.items() if item['currentLocationId'] == target]
    require(occupants == [defender['elementId']] and actor_hex != target,
            'exact target occupant membership')
    edges = {frozenset((edge['firstLocationId'], edge['secondLocationId']))
             for edge in content['edges'] if edge['features'] == []}
    adjacent = frozenset((actor_hex, target)) in edges
    if state['profile'] == 'armed' and adjacent:
        require(state['targetUses'] == state['priorCycleTargetUses'] ==
                state['attackHistory'] == [] and world['futureObligations'] == [] and
                state['movementEnd']['excludedBefore'] == [] and
                state['originalMovementEnd']['excludedBefore'] == [],
                'fresh target and obligation absence')
    shape, pair = p01.physical_shape(state, content)
    if shape == 'provisional-positive':
        require(adjacent and pair is not None and
                pair[0]['elementId'] == actor['elementId'] and
                pair[1]['elementId'] == defender['elementId'] and
                state['profile'] == 'armed' and
                state['targetUses'] == state['priorCycleTargetUses'] == state['attackHistory'] == [] and
                world['futureObligations'] == [] and
                state['movementEnd']['excludedBefore'] == [] and
                state['originalMovementEnd']['excludedBefore'] == [],
                'fresh target and obligation absence')
        # Land §10.11 plus the pinned one-battalion-per-hex content keeps this
        # bounded pair below the greater-than-one-stacking-point ZOC threshold.
        require(all(item['organizationId'] == 'land.organization.battalion'
                    for item in static.values()), 'bounded ZOC profile')
    elif shape == 'provisional-empty':
        require(not adjacent or (state['profile'] == 'paid' and
                                 elements[actor['elementId']]['ammunition']['points'] == 0),
                'bounded empty physical reason')
    else:
        require(pair is None, 'unresolved is never a positive action')
    return dict(physicalShape=shape, actorSide=side, targetLocationId=target,
                selectedParticipantCount=2, selectedComponentCount=2,
                weatherAreaBound=True, currentWeatherKindProved=False,
                sourceLegalActionExhaustive=False, admitted=False)


def audit(evidence):
    evidence = Path(evidence)
    for name, expected in result.PINS.items():
        require(hashlib.sha256((evidence / name).read_bytes()).hexdigest() == expected,
                'source pin ' + name)
    manifest = json.loads((evidence / 'manifest.json').read_text())
    summary = json.loads((evidence / 'summary.json').read_text())
    partitions = json.loads((evidence / 'bounded-profile-audit.json').read_text())
    require(len(manifest['chunks']) == 11 and
            summary['count'] == len(summary['rows']) == len(partitions['rows']) == 300,
            'complete pinned catalogue')
    content = p01.selected_content()
    counts = Counter()
    keys = set()
    index = 0
    for chunk in manifest['chunks']:
        data = (evidence / chunk['name']).read_bytes()
        require(hashlib.sha256(data).hexdigest() == chunk['sha256'] ==
                summary['chunkSha256'][chunk['name']], 'chunk pin')
        for line in gzip.decompress(data).splitlines():
            row = json.loads(line)
            require(row['index'] == index, 'contiguous row index')
            record = row['r03']
            key = record['rowKey']
            require(key == summary['rows'][index]['rowKey'] == partitions['rows'][index]['rowKey']
                    and key not in keys, 'unique row identity')
            keys.add(key)
            state = result.require_control_vector(record, result.authenticated_control(record))
            request = record['vectors']['parentInputLedgersCanonicalUtf8'][0]
            fact = check_selected_structure(state, content, request)
            expected = partitions['rows'][index]['partition']
            require(fact['physicalShape'] == expected and
                    fact['currentWeatherKindProved'] is False and
                    fact['sourceLegalActionExhaustive'] is False and
                    fact['admitted'] is False, 'provisional-only row result')
            counts[expected] += 1
            index += 1
    require(index == len(keys) == 300 and
            counts == {'provisional-positive': 44, 'provisional-empty': 256},
            '300-row bounded partition')
    return dict(authenticatedRows=300, provisionalPositive=44, provisionalEmpty=256,
                currentWeatherKindProved=False, sourceLegalActionExhaustive=False,
                admittedRows=0)


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('evidence', type=Path)
    print(audit(parser.parse_args().evidence))
