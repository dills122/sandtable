#!/usr/bin/env python3
"""Fail-sensitive private AC-006 boundary tests; no synthetic input is admission proof."""

import importlib.util
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location(
    'boundary', ROOT / 'verify-combat-p01-surrender-rba-boundary-v0.py')
boundary = importlib.util.module_from_spec(spec)
spec.loader.exec_module(boundary)


def sample(**changes):
    value = dict(contractVersion=boundary.SCHEMA['contractVersion'], cycleOrdinal=2,
                 sourceTrigger='combat-position', targetCohesionBefore=0,
                 targetAmmunitionBefore=10, targetAmmunitionAfter=10,
                 rbaOutcome='unopened', declineReceiptId=None,
                 cancellationReceiptId=None, surrenderSettlementReceiptId=None)
    value.update(changes)
    return value


class BoundaryTests(unittest.TestCase):
    def test_pretrigger_never_admits(self):
        report = boundary.diagnose(sample())
        self.assertEqual('no-automatic-surrender-at-trigger', report['sourceCondition'])
        self.assertFalse(report['admitted'])
        self.assertFalse(report['automaticSurrenderSettlementProved'])
        self.assertEqual([], report['publicActions'])

    def test_source_trigger_thresholds_and_pre_cost_ammunition(self):
        cases = (
            ('close-assault-against-unit', -16, 10, False),
            ('close-assault-against-unit', -17, 10, True),
            ('anti-armor-assault-against-unit', 0, 0, True),
            ('enemy-moved-adjacent', -25, 10, False),
            ('enemy-moved-adjacent', -26, 10, True),
            ('enemy-moved-adjacent', 0, 0, False),
            ('existing-adjacency', 0, 0, False),
            ('barrage-against-unit', 0, 0, False),
            ('post-cost-settlement', 0, 0, False),
            ('close-assault-against-unit', 0, 10, False),
        )
        for trigger, cohesion, before, expected in cases:
            with self.subTest(trigger=trigger, cohesion=cohesion, before=before):
                report = boundary.diagnose(sample(
                    sourceTrigger=trigger, targetCohesionBefore=cohesion,
                    targetAmmunitionBefore=before, targetAmmunitionAfter=0))
                self.assertEqual('requires-typed-settlement' if expected else
                                 'no-automatic-surrender-at-trigger', report['sourceCondition'])
                self.assertFalse(report['admitted'])

    def test_rba_cancel_never_invents_decline(self):
        cancelled = sample(sourceTrigger='existing-adjacency', rbaOutcome='cancelled',
                           cancellationReceiptId='cmb.cancelled')
        report = boundary.diagnose(cancelled)
        self.assertEqual('cancelled', report['rbaCondition'])
        self.assertFalse(report['ordinalTwoRbaProofProved'])
        for changed in (dict(declineReceiptId='cmb.synthetic'),
                        dict(cancellationReceiptId=None),
                        dict(sourceTrigger='close-assault-against-unit')):
            with self.subTest(changed=changed), self.assertRaises(ValueError):
                boundary.diagnose(dict(cancelled, **changed))

    def test_decline_and_cancel_are_exclusive(self):
        decline = sample(sourceTrigger='existing-adjacency', rbaOutcome='declined',
                         declineReceiptId='cmb.declined')
        self.assertEqual('declined', boundary.diagnose(decline)['rbaCondition'])
        with self.assertRaises(ValueError):
            boundary.diagnose(dict(decline, cancellationReceiptId='cmb.cancelled'))

    def test_closed_shape_and_unproved_settlement(self):
        for changed in (dict(sourceTrigger=None), dict(targetAmmunitionBefore=True),
                        dict(cycleOrdinal=1), dict(surrenderSettlementReceiptId='cmb.fake'),
                        dict(unknown='field'), dict(rbaOutcome='declined')):
            with self.subTest(changed=changed), self.assertRaises(ValueError):
                boundary.diagnose(sample(**changed))
        missing = sample()
        del missing['targetCohesionBefore']
        with self.assertRaises(ValueError):
            boundary.diagnose(missing)


if __name__ == '__main__':
    unittest.main()
