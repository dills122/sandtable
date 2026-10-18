#!/usr/bin/env python3
"""Fail-sensitive source-boundary tests for the provisional P01 action matrix."""

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


reference = load('p01_matrix_reference_tests',
                 ROOT / 'test-combat-actual-moved-repeat-position-v1.py')
matrix = load('p01_matrix_proof', ROOT / 'verify-combat-p01-action-matrix-v1.py')


class P01ActionMatrixTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.content = matrix.action.p01.selected_content()
        cls.rows = {}
        for index in (0, 36, 37, 168):
            source, ledgers, own = reference.terminal(index)
            cls.rows[index] = (matrix.action.p01.r03.replay(source, ledgers, own)[2],
                               ledgers[0])

    def check(self, index, state=None, content=None, request=None):
        authentic_state, authentic_request = self.rows[index]
        return matrix.classify(state or authentic_state, content or self.content,
                               request or authentic_request)

    def test_both_actor_sides_and_three_action_shapes_stay_unadmitted(self):
        expected = {0: ('no-loaded-acting-infantry', 'provisional-empty'),
                    36: ('provisional-adjacent', 'provisional-positive'),
                    37: ('no-adjacent-opposing-target', 'provisional-empty'),
                    168: ('provisional-adjacent', 'provisional-positive')}
        for index, (shape, physical) in expected.items():
            with self.subTest(index=index):
                fact = self.check(index)
                self.assertEqual((shape, physical),
                                 (fact['assaultShape'], fact['physicalShape']))
                self.assertFalse(fact['sourceLegalActionExhaustive'])
                self.assertFalse(fact['typedOrdinalTwoDecisionProved'])
                self.assertFalse(fact['admitted'])
                self.assertEqual([], fact['publicActions'])
        self.assertEqual('axis', self.check(36)['actorSide'])
        self.assertEqual('commonwealth', self.check(168)['actorSide'])

    def test_policy_exclusion_and_unproved_later_choices_are_distinct(self):
        modes = self.check(36)['modeStatuses']
        self.assertEqual('source-legal-policy-excluded', modes['voluntary-surrender'])
        self.assertEqual('later-trigger-fail-closed', modes['automatic-surrender'])
        self.assertEqual('later-explicit-decision-open', modes['retreat-before-assault-decline'])
        self.assertEqual('unsupported-in-bounded-profile', modes['retreat-before-assault-move'])
        self.assertEqual('selected-content-absent', modes['barrage'])
        self.assertEqual('selected-content-absent', modes['mandatory-zoc-assault'])
        self.assertEqual('later-typed-choice-open', modes['close-assault'])

    def test_authentic_nonadjacent_rba_is_not_erased_by_assault_absence(self):
        # r012-c02: actor at axis-rear, non-phasing defender at assault-east.
        # The defender has CP headroom and a vacant clear edge to its rear;
        # source RBA is not ruled out by initial nonadjacency (Land 13.23-24).
        state, request = self.rows[37]
        self.assertEqual('no-adjacent-opposing-target', self.check(37)['assaultShape'])
        actor_side = state['activeCycle']['actingSide']
        static = {unit['elementId']: unit['sideId'] for unit in self.content['elements']}
        defender = next(unit for unit in state['world']['elements']
                        if static[unit['elementId']] != actor_side)
        self.assertEqual('assault-east', defender['currentLocationId'])
        self.assertEqual(0, defender['operationalState']['capabilityPointsExpended']['numerator'])
        occupied = {unit['currentLocationId'] for unit in state['world']['elements']}
        self.assertNotIn('commonwealth-rear', occupied)
        self.assertTrue(any({edge['firstLocationId'], edge['secondLocationId']} ==
                            {'assault-east', 'commonwealth-rear'} and edge['features'] == []
                            for edge in self.content['edges']))
        modes = self.check(37)['modeStatuses']
        self.assertEqual('later-explicit-decision-open', modes['retreat-before-assault-decline'])
        self.assertEqual('unsupported-in-bounded-profile', modes['retreat-before-assault-move'])
        self.assertEqual('not-available-in-authenticated-row', modes['close-assault'])
        self.assertNotEqual('not-available-in-authenticated-row',
                            modes['retreat-before-assault-decline'])
        self.assertNotEqual('not-available-in-authenticated-row',
                            modes['retreat-before-assault-move'])

        # Negative variant: a forged defender relocation cannot turn the
        # source possibility into an authoritative move/decline receipt.
        changed = copy.deepcopy(state)
        changed_defender = next(unit for unit in changed['world']['elements']
                                if unit['elementId'] == defender['elementId'])
        changed_defender['currentLocationId'] = 'commonwealth-rear'
        with self.assertRaises(ValueError):
            matrix.classify(changed, self.content, request)

    def test_no_ammunition_after_prior_assault_is_not_automatic_surrender(self):
        state, _ = self.rows[0]
        self.assertEqual(0, next(unit for unit in state['world']['elements']
                                 if unit['elementId'] == state['members'][0]['unit']['elementId'])
                         ['ammunition']['points'])
        self.assertEqual('no-loaded-acting-infantry', self.check(0)['assaultShape'])
        self.assertFalse(matrix.source.automatic_surrender(
            cohesion_at_trigger=0, ammunition_at_trigger=0, trigger='combat-position'))
        self.assertTrue(matrix.source.automatic_surrender(
            cohesion_at_trigger=0, ammunition_at_trigger=0,
            trigger='close-assault-against-unit'))

    def test_mutated_paid_ammo_or_fresh_target_use_rejects(self):
        state, _ = self.rows[0]
        changed = copy.deepcopy(state)
        actor_id = state['members'][0]['unit']['elementId']
        actor = next(unit for unit in changed['world']['elements']
                     if unit['elementId'] == actor_id)
        actor['ammunition']['points'] = 1
        with self.assertRaises(ValueError):
            self.check(0, state=changed)
        changed = copy.deepcopy(self.rows[36][0])
        changed['targetUses'].append({'unexpected': 'paid target in current segment'})
        with self.assertRaises(ValueError):
            self.check(36, state=changed)

    def test_nonselected_units_and_weather_area_reject(self):
        content = copy.deepcopy(self.content)
        content['elements'].append(copy.deepcopy(content['elements'][0]))
        with self.assertRaises(ValueError):
            self.check(36, content=content)
        content = copy.deepcopy(self.content)
        content['weatherAreaAssignments'][0]['weatherArea'] = 'b'
        with self.assertRaises(ValueError):
            self.check(36, content=content)


if __name__ == '__main__':
    unittest.main()
