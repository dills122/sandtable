#!/usr/bin/env python3
"""Fail-sensitive selected provenance tests on authenticated P01 ancestors."""

import copy
import importlib.util
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parent


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


reference_tests = load('p01_action_reference_tests',
                       ROOT / 'test-combat-actual-moved-repeat-position-v1.py')
proof = load('p01_action_proof', ROOT / 'verify-combat-p01-action-provenance-v1.py')


class P01ActionProvenanceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.content = proof.p01.selected_content()
        cls.rows = {}
        for index in (0, 36, 168):
            source, ledgers, own = reference_tests.terminal(index)
            cls.rows[index] = (proof.p01.r03.replay(source, ledgers, own)[2], ledgers[0])

    def check(self, state, request, content=None):
        return proof.check_selected_structure(state, content or self.content, request)

    def test_authenticated_shapes_stay_provisional(self):
        for index, expected, side in ((0, 'provisional-empty', 'axis'),
                                      (36, 'provisional-positive', 'axis'),
                                      (168, 'provisional-positive', 'commonwealth')):
            with self.subTest(index=index):
                state, request = self.rows[index]
                fact = self.check(state, request)
                self.assertEqual((expected, side), (fact['physicalShape'], fact['actorSide']))
                self.assertFalse(fact['currentWeatherKindProved'])
                self.assertFalse(fact['sourceLegalActionExhaustive'])
                self.assertFalse(fact['admitted'])

    def test_original_authority_and_member_mutations_reject(self):
        state, request = self.rows[36]
        changes = []
        bad = copy.deepcopy(request)
        bad['configurationHash'] = 'sha256:' + '0' * 64
        changes.append((state, bad, self.content))
        bad = copy.deepcopy(state)
        bad['originalCycle']['actingSide'] = 'commonwealth'
        changes.append((bad, request, self.content))
        bad = copy.deepcopy(state)
        bad['members'][0]['unit']['originalSide'] = 'commonwealth'
        changes.append((bad, request, self.content))
        bad = copy.deepcopy(state)
        bad['originalMovementEnd']['endLocations'][0]['unit']['creationBinding'] = 'creation.wrong'
        changes.append((bad, request, self.content))
        for index, (changed_state, changed_request, changed_content) in enumerate(changes):
            with self.subTest(case=index), self.assertRaises(ValueError):
                self.check(changed_state, changed_request, changed_content)

    def test_representation_and_target_mutations_reject(self):
        state, request = self.rows[36]
        changes = []
        bad = copy.deepcopy(state)
        bad['world']['representations'][0]['representationId'] = bad['world']['representations'][1]['representationId']
        changes.append(bad)
        bad = copy.deepcopy(state)
        bad['world']['representations'][0]['boundElementIds'] = ['commonwealth-assault-battalion']
        changes.append(bad)
        bad = copy.deepcopy(state)
        bad['world']['elements'][1]['currentLocationId'] = 'commonwealth-rear'
        changes.append(bad)
        bad = copy.deepcopy(state)
        bad['movementEnd']['endLocations'].pop()
        changes.append(bad)
        for index, changed in enumerate(changes):
            with self.subTest(case=index), self.assertRaises(ValueError):
                self.check(changed, request)

    def test_current_and_prior_segment_obligations_reject(self):
        state, request = self.rows[36]
        for field in ('targetUses', 'priorCycleTargetUses', 'attackHistory'):
            changed = copy.deepcopy(state)
            changed[field].append({'unexpected': 'target'})
            with self.subTest(field=field), self.assertRaises(ValueError):
                self.check(changed, request)
        changed = copy.deepcopy(state)
        changed['world']['futureObligations'].append({'unexpected': 'mandatory assault'})
        with self.assertRaises(ValueError):
            self.check(changed, request)

    def test_paid_target_history_is_prior_only_and_identity_bound(self):
        state, request = self.rows[0]
        self.assertEqual('provisional-empty', self.check(state, request)['physicalShape'])
        changes = []
        bad = copy.deepcopy(state)
        bad['targetUses'].append(copy.deepcopy(bad['priorCycleTargetUses'][0]))
        changes.append(bad)
        bad = copy.deepcopy(state)
        bad['priorCycleTargetUses'].clear()
        changes.append(bad)
        bad = copy.deepcopy(state)
        bad['priorCycleTargetUses'][0]['targetLocationId'] = 'axis-rear'
        changes.append(bad)
        bad = copy.deepcopy(state)
        bad['priorCycleTargetUses'][0]['segmentId'] = 'other-segment'
        changes.append(bad)
        bad = copy.deepcopy(state)
        bad['attackHistory'][0]['cycleId'] = bad['cycleId']
        changes.append(bad)
        bad = copy.deepcopy(state)
        bad['attackHistory'][0]['attacker']['originalSide'] = 'commonwealth'
        changes.append(bad)
        bad = copy.deepcopy(state)
        bad['world']['settlements'][0]['commitmentId'] = 'other-commitment'
        changes.append(bad)
        for index, changed in enumerate(changes):
            with self.subTest(case=index), self.assertRaisesRegex(ValueError, 'paid'):
                self.check(changed, request)

    def test_alternate_selected_content_and_weather_area_mutations_reject(self):
        state, request = self.rows[36]
        alternatives = []
        changed = copy.deepcopy(self.content)
        changed['elements'][0]['components'].append(copy.deepcopy(changed['elements'][0]['components'][0]))
        alternatives.append(changed)
        changed = copy.deepcopy(self.content)
        changed['elements'][0]['components'][0]['componentClassId'] = 'land.combat-component.gun'
        alternatives.append(changed)
        changed = copy.deepcopy(self.content)
        changed['elements'].append(copy.deepcopy(changed['elements'][0]))
        alternatives.append(changed)
        changed = copy.deepcopy(self.content)
        changed['weatherAreaAssignments'][0]['weatherArea'] = 'b'
        alternatives.append(changed)
        for index, content in enumerate(alternatives):
            with self.subTest(case=index), self.assertRaises(ValueError):
                self.check(state, request, content)


if __name__ == '__main__':
    unittest.main()
