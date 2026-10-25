#!/usr/bin/env python3
"""Fail-sensitive retained Force Assignment seal cold-readback tests."""
import base64
import copy
import importlib.util
import json
import unittest
import sys
from unittest import mock
from pathlib import Path

ROOT = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location(
    "force_seal_fresh_tests", ROOT / "verify-combat-p01-force-seal-fresh-v1.py")
proof = importlib.util.module_from_spec(spec)
spec.loader.exec_module(proof)
GOLDEN = json.loads((ROOT / "fixtures/combat-p01-force-seal-fresh-v1.json").read_text())


def altered_input(raw, **changes):
    value = json.loads(base64.b64decode(raw))
    value.update(changes)
    return proof.b64(proof.ref.encode(value))


class LegacyDependencyArchiveTests(unittest.TestCase):
    def test_changed_original_archive_rejects_before_loading_oracle(self):
        helper = sys.modules["sandtable_combat_evidence_legacy"]
        for name in helper._ARCHIVES:
            with self.subTest(archive=name), mock.patch.object(helper, "_directory", None), \
                    mock.patch.dict(helper._ARCHIVES, {name: "0" * 64}):
                with self.assertRaisesRegex(ValueError, "archive hash mismatch"):
                    helper.legacy_root()


class ForceSealFreshTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.axis = proof.build(36, "attacker")
        cls.commonwealth = proof.build(168, "defender")

    def test_both_original_actor_orientations_and_frozen_bytes(self):
        for case, expected in zip((self.axis, self.commonwealth), GOLDEN["rows"]):
            with self.subTest(row=case["row"]["rowKey"]):
                observed = proof.child(case["claim"])
                self.assertNotIn("rejected", observed, observed)
                for key, value in case["expected"].items():
                    self.assertEqual(value, observed[key], key)
                self.assertEqual(case["firstReceiptId"], observed["retryReceiptId"])
                self.assertIsNone(observed["retryEvent"])
                self.assertFalse(observed["admitted"])
                self.assertEqual([], observed["publicActions"])
                self.assertEqual(expected["firstSourceHash"],
                                 proof.digest(case["source"]))
                self.assertEqual(expected["secondEventHash"], proof.digest(
                    base64.b64decode(observed["nextEvent"])))
        self.assertEqual("sha256:d5a5fb14fd7657d8dadf99e73dc065f98979ade54f475fba16e8a988abc81921",
                         proof.digest(proof.ref.encode(GOLDEN)))

    def test_source_ledger_event_owner_clock_and_receipt_mutations_reject(self):
        claim = self.axis["claim"]
        source = proof.ref.parse_source(self.axis["source"])
        changed_source = copy.deepcopy(source)
        changed_source["eventCanonicalUtf8"][-1] = changed_source[
            "eventCanonicalUtf8"][-1].replace("owner-full-ten-toe", "forged-toe")
        missing = copy.deepcopy(source)
        missing["eventCanonicalUtf8"].pop()
        duplicate = copy.deepcopy(source)
        duplicate["eventCanonicalUtf8"].append(
            duplicate["eventCanonicalUtf8"][-1])
        cases = [
            ("source-only", "source", proof.b64(proof.ref.encode(changed_source))),
            ("missing-event", "source", proof.b64(proof.ref.encode(missing))),
            ("duplicate-event", "source", proof.b64(proof.ref.encode(duplicate))),
            ("missing-ledger", "ownLedger", claim["ownLedger"][:-1]),
            ("duplicate-ledger", "ownLedger", [*claim["ownLedger"],
                                                 claim["ownLedger"][-1]]),
        ]
        for label, changes in (
            ("ledger-only", {"expectedPriorVersion":
                             self.axis["collecting"]["stateVersion"]}),
            ("wrong-owner", {"actor": "commonwealth"}),
            ("wrong-clock", {"admittedAtUnixMilliseconds": 1999}),
            ("wrong-receipt", {"previousReceiptId": "p01u." + "0" * 64}),
        ):
            ledger = claim["ownLedger"].copy()
            ledger[1] = altered_input(ledger[1], **changes)
            cases.append((label, "ownLedger", ledger))
        for label, key, value in cases:
            with self.subTest(label=label):
                changed = copy.deepcopy(claim)
                changed[key] = value
                self.assertEqual("fresh-readback",
                                 proof.child(changed).get("rejected"))

    def test_second_owner_and_expected_state_mutations_reject(self):
        claim = self.commonwealth["claim"]
        second_actor = json.loads(base64.b64decode(claim["secondInput"]))["actor"]
        wrong_actor = "axis" if second_actor == "commonwealth" else "commonwealth"
        cases = [
            ("second-owner", "secondInput",
             altered_input(claim["secondInput"], actor=wrong_actor)),
            ("second-clock", "secondInput",
             altered_input(claim["secondInput"],
                           admittedAtUnixMilliseconds=32000)),
            ("second-receipt", "secondInput",
             altered_input(claim["secondInput"],
                           previousReceiptId="p01v." + "0" * 64)),
            ("control", "expectedControl", proof.b64(b"{}")),
            ("world", "expectedWorld", proof.b64(b"{}")),
            ("random", "expectedRandom", proof.b64(b"{}")),
        ]
        for label, key, value in cases:
            with self.subTest(label=label):
                changed = copy.deepcopy(claim)
                changed[key] = value
                self.assertEqual("fresh-readback",
                                 proof.child(changed).get("rejected"))


if __name__ == "__main__":
    unittest.main()
