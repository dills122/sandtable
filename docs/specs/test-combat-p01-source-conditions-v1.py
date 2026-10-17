#!/usr/bin/env python3
"""Fail-sensitive checks for the private AC-006 source-condition diagnostic."""

import importlib.util
from pathlib import Path
import unittest
from unittest import mock

ROOT = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location(
    'p01_source_conditions', ROOT / 'verify-combat-p01-source-conditions-v1.py')
gate = importlib.util.module_from_spec(spec)
spec.loader.exec_module(gate)
EVIDENCE = Path('/Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence')


class P01SourceConditionTests(unittest.TestCase):
    def test_literal_source_boundary_matrix(self):
        self.assertEqual(14, gate.source_matrix())

    def test_ammunition_is_read_at_assault_not_after_payment(self):
        self.assertFalse(gate.automatic_surrender(
            cohesion_at_trigger=0, ammunition_at_trigger=10,
            trigger='close-assault-against-unit'))
        self.assertTrue(gate.automatic_surrender(
            cohesion_at_trigger=0, ammunition_at_trigger=0,
            trigger='close-assault-against-unit'))
        for trigger in ('combat-position', 'existing-adjacency',
                        'enemy-moved-adjacent', 'barrage-against-unit',
                        'post-cost-settlement'):
            with self.subTest(trigger=trigger):
                self.assertFalse(gate.automatic_surrender(
                    cohesion_at_trigger=0, ammunition_at_trigger=0, trigger=trigger))

    def test_cohesion_threshold_requires_correct_event(self):
        self.assertFalse(gate.automatic_surrender(
            cohesion_at_trigger=-17, ammunition_at_trigger=10,
            trigger='enemy-moved-adjacent'))
        self.assertTrue(gate.automatic_surrender(
            cohesion_at_trigger=-17, ammunition_at_trigger=10,
            trigger='anti-armor-assault-against-unit'))
        self.assertFalse(gate.automatic_surrender(
            cohesion_at_trigger=-25, ammunition_at_trigger=10,
            trigger='enemy-moved-adjacent'))
        self.assertTrue(gate.automatic_surrender(
            cohesion_at_trigger=-26, ammunition_at_trigger=10,
            trigger='enemy-moved-adjacent'))

    def test_unhandled_automatic_surrender_fails_closed(self):
        for cohesion, ammo, trigger in (
                (-17, 10, 'close-assault-against-unit'),
                (-26, 10, 'enemy-moved-adjacent'),
                (0, 0, 'anti-armor-assault-against-unit')):
            with self.subTest(trigger=trigger), self.assertRaisesRegex(
                    ValueError, 'automatic surrender requires separate contract'):
                gate.require_no_unhandled_surrender(
                    cohesion_at_trigger=cohesion, ammunition_at_trigger=ammo,
                    trigger=trigger)

    def test_untrusted_or_ambiguous_timing_cannot_be_classified(self):
        for inputs in (
                dict(cohesion_at_trigger=False, ammunition_at_trigger=0,
                     trigger='close-assault-against-unit'),
                dict(cohesion_at_trigger=0, ammunition_at_trigger=True,
                     trigger='close-assault-against-unit'),
                dict(cohesion_at_trigger=0, ammunition_at_trigger=-1,
                     trigger='close-assault-against-unit'),
                dict(cohesion_at_trigger=0, ammunition_at_trigger=0,
                     trigger='unknown-assault'),
        ):
            with self.subTest(inputs=inputs), self.assertRaises(ValueError):
                gate.automatic_surrender(**inputs)

    def test_source_pin_rejects_before_ancestor_replay(self):
        with mock.patch.dict(gate.RULE_HASHES, {'CNA_LandGameRules.pdf': '0' * 64}):
            with mock.patch.object(gate.evidence, 'audit',
                                   side_effect=AssertionError('ancestor replayed')):
                with self.assertRaisesRegex(ValueError, 'physical source pin'):
                    gate.audit(EVIDENCE)

    def test_prior_selection_contract_cancels_without_decline(self):
        # The predecessor contract is a dependency witness, not an ordinal-two trace.
        self.assertEqual(8, gate.check_rba_prior_contract())


if __name__ == '__main__':
    unittest.main()
