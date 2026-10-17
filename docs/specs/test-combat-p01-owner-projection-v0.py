#!/usr/bin/env python3
"""Fail-sensitive tests for the private owner-alias design scaffold."""

import importlib.util
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location(
    "p01_owner_projection", ROOT / "verify-combat-p01-owner-projection-v0.py")
projection = importlib.util.module_from_spec(spec)
spec.loader.exec_module(projection)


def visible():
    return dict(audienceSide="axis", visibleDecisionRevision=1,
                capabilityKind="close-assault-full", apparentTargetRef="assault-east")


class OwnerProjectionScaffoldTests(unittest.TestCase):
    def test_hidden_private_identity_does_not_salt_visible_alias(self):
        first = projection.private_mapping([
            dict(ownerVisibleFacts=visible(), privateCandidateId="p01." + "a" * 64)])
        second = projection.private_mapping([
            dict(ownerVisibleFacts=visible(), privateCandidateId="p01." + "b" * 64)])
        self.assertEqual(list(first), list(second))
        self.assertNotEqual(list(first.values()), list(second.values()))
        self.assertEqual(first.keys(), second.keys())
        for private_id in ("p01." + "a" * 64, "p01." + "b" * 64):
            self.assertNotIn(private_id.encode(), projection.denial_bytes())
        self.assertEqual(
            "p01-owner-draft.ffe6cfb584ad7720ff9bff4d3bc2f1c76163aed2e2b5705b962cb930a92c6447",
            list(first)[0])
        self.assertEqual(list(first)[0], projection.compare_trial_visibility(
            visible(), visible(), True, True))

    def test_hidden_dependent_legality_fails_closed(self):
        with self.assertRaises(ValueError):
            projection.compare_trial_visibility(visible(), visible(), True, False)

    def test_only_visible_decision_revision_changes_alias(self):
        facts = visible()
        original = projection.draft_alias(facts)
        facts["visibleDecisionRevision"] += 1
        self.assertNotEqual(original, projection.draft_alias(facts))

    def test_private_or_ambiguous_fields_reject(self):
        for field, value in (("privateCandidateId", "p01." + "a" * 64),
                             ("stateVersion", 33), ("hiddenDefenderId", "enemy")):
            with self.subTest(field=field):
                facts = visible()
                facts[field] = value
                with self.assertRaises(ValueError):
                    projection.draft_alias(facts)
        binding = dict(ownerVisibleFacts=visible(), privateCandidateId="p01." + "a" * 64)
        with self.assertRaises(ValueError):
            projection.private_mapping([binding, binding])

    def test_unapproved_values_and_private_alias_cannot_publish(self):
        for field, value in (("visibleDecisionRevision", True),
                             ("visibleDecisionRevision", 0),
                             ("apparentTargetRef", "Hidden Target"),
                             ("capabilityKind", "barrage")):
            with self.subTest(field=field):
                facts = visible()
                facts[field] = value
                with self.assertRaises(ValueError):
                    projection.draft_alias(facts)
        self.assertEqual(
            b'{"contractVersion":1,"status":"unsupported","admitted":false,"publicActions":[]}',
            projection.denial_bytes())


if __name__ == "__main__":
    unittest.main()
