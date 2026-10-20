#!/usr/bin/env python3
"""Synthetic codec goldens only; authentic AC-007 hidden twins remain open."""

import copy
import importlib.util
import json
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parent
NAME = "verify-combat-p01-owner-decision-proposal-v1"
spec = importlib.util.spec_from_file_location(NAME, ROOT / (NAME + ".py"))
codec = importlib.util.module_from_spec(spec)
spec.loader.exec_module(codec)
GOLDEN = json.loads((ROOT / "fixtures/combat-p01-owner-decision-proposal-v1.json").read_text())


class OwnerDecisionProposalTests(unittest.TestCase):
    def row(self, name):
        return next(r for r in GOLDEN["cases"] if r["name"] == name)

    def test_six_frozen_codec_vectors(self):
        self.assertEqual(len(GOLDEN["cases"]), 6)
        for row in GOLDEN["cases"]:
            with self.subTest(row=row["name"]):
                seed, alias = row["decisionSeed"], row["aliasSeed"]
                expected = row["expected"]
                self.assertEqual(codec.canonical(seed).decode("ascii"), expected["decisionSeedCanonical"])
                self.assertEqual(codec.canonical(alias).decode("ascii"), expected["aliasSeedCanonical"])
                self.assertEqual(codec.target_ref(seed["context"], row["ownerHex"]), expected["targetRef"])
                self.assertEqual(codec.decision_ref(seed), expected["decisionRef"])
                self.assertEqual(codec.validate_opening(seed, alias, row["ownerHex"]), expected["alias"])

    def test_seat_window_ordinal_and_restart_identity(self):
        first = self.row("axis-initial")["expected"]
        self.assertNotEqual(first["alias"], self.row("commonwealth-initial")["expected"]["alias"])
        self.assertNotEqual(first["alias"], self.row("axis-reopen")["expected"]["alias"])
        self.assertNotEqual(first["alias"], self.row("axis-next-ordinal")["expected"]["alias"])
        self.assertEqual(first["alias"], self.row("axis-restart")["expected"]["alias"])
        self.assertEqual(first["alias"], self.row("axis-hidden-only-synthetic-fork")["expected"]["alias"])
        self.assertEqual(first["decisionRef"], self.row("axis-restart")["expected"]["decisionRef"])

    def test_approved_hex_change_rotates_and_hidden_binding_is_not_a_seed(self):
        row = self.row("axis-initial")
        changed = copy.deepcopy(row["decisionSeed"])
        changed["targetRef"] = codec.target_ref(changed["context"], "hex-0406")
        self.assertNotEqual(codec.decision_ref(changed), row["expected"]["decisionRef"])
        with self.assertRaises(ValueError):
            codec.validate_opening(row["decisionSeed"], row["aliasSeed"], "hex-0406")
        changed["hiddenUnitKey"] = "private-defender"
        with self.assertRaises(ValueError):
            codec.decision_ref(changed)

    def test_closed_tags_and_first_open_revision(self):
        row = self.row("axis-initial")
        for field, bad in (("kind", "close-assault-full"), ("openingRevision", 0),
                           ("openingRevision", True), ("openingRevision", 1 << 63),
                           ("cycleRef", "sha256:" + "G" * 64)):
            changed = copy.deepcopy(row["decisionSeed"])
            changed[field] = bad
            with self.subTest(field=field, bad=bad), self.assertRaises(ValueError):
                codec.decision_ref(changed)
        changed = copy.deepcopy(row["aliasSeed"])
        changed["capabilityKind"] = "selection"
        with self.assertRaises(ValueError):
            codec.alias(changed)
        changed = copy.deepcopy(row["decisionSeed"])
        changed["context"]["cycle"]["ordinal"] = 1 << 31
        with self.assertRaises(ValueError):
            codec.decision_ref(changed)

    def test_wrong_seat_or_noncanonical_binding_fails_closed(self):
        row = self.row("axis-initial")
        changed = copy.deepcopy(row["aliasSeed"])
        changed["ownParticipantRef"] = "pub." + "b" * 64
        with self.assertRaises(ValueError):
            codec.validate_opening(row["decisionSeed"], changed, row["ownerHex"])
        changed = copy.deepcopy(row["decisionSeed"])
        changed["context"]["audience"] = "commonwealth"
        with self.assertRaises(ValueError):
            codec.validate_opening(changed, row["aliasSeed"], row["ownerHex"])
        changed = copy.deepcopy(row["decisionSeed"])
        changed["context"]["campaignId"] = "bad\nsource"
        with self.assertRaises(ValueError):
            codec.decision_ref(changed)
        changed = copy.deepcopy(row["decisionSeed"])
        changed["context"]["secret"] = "private"
        with self.assertRaises(ValueError):
            codec.decision_ref(changed)

    def test_rejection_is_generic_and_no_offer_is_minted(self):
        self.assertEqual(codec.reject_bytes(), b'{"status":"not-current-or-not-admissible"}')
        self.assertEqual(codec.SPEC["outputStatus"], "unsupported")
        self.assertFalse(codec.SPEC["admitted"])
        self.assertEqual(codec.SPEC["publicActions"], [])


if __name__ == "__main__":
    unittest.main()
