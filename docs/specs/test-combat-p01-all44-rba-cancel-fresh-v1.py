#!/usr/bin/env python3
"""Fail-sensitive private all-44 selected-assault cancellation proof tests."""
import base64
import copy
import importlib.util
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location(
    "all44_cancel_proof", ROOT / "verify-combat-p01-all44-rba-cancel-fresh-v1.py")
proof = importlib.util.module_from_spec(spec)
spec.loader.exec_module(proof)


class FreshCancelTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        rows = proof.pinned_rows()
        cls.axis = proof.build(rows[0])
        cls.commonwealth = proof.build(rows[22])

    def reject(self, change):
        claim = copy.deepcopy(self.axis["claim"])
        change(claim)
        observed = proof.child(claim)
        self.assertEqual("fresh-rba-cancel", observed.get("rejected"), observed)

    def change_input(self, claim, ordinal, change):
        value = proof.selected.parse_input(base64.b64decode(
            claim["continuationInputs"][ordinal]))
        change(value)
        claim["continuationInputs"][ordinal] = proof.b64(proof.selected.encode(value))

    def test_both_original_orientations_reach_same_slot_reserve_release(self):
        for case, defender in ((self.axis, "commonwealth"),
                               (self.commonwealth, "axis")):
            with self.subTest(defender=defender):
                observed = proof.child(case["claim"])
                self.assertNotIn("rejected", observed)
                self.assertEqual(case["expected"]["transitions"],
                                 observed["transitions"])
                self.assertEqual(defender, case["state"]["rbaDecision"]["owner"])
                self.assertEqual(5, len(observed["transitions"]))
                self.assertEqual("cancelled", case["final"]["rbaOutcome"])
                self.assertIsNone(case["final"]["declineReceiptId"])
                self.assertTrue(observed["transitions"][-1]["closed"])
                self.assertTrue(observed["transitions"][-1]["positionId"].endswith(
                    ".reserve-release"))
                self.assertFalse(observed["admitted"])
                self.assertEqual([], observed["publicActions"])

    def test_cancellation_requires_system_actor_clock_and_decision(self):
        for change in (
            lambda v: v.__setitem__("actor", "axis"),
            lambda v: v.__setitem__("decisionId", "p01d." + "0" * 64),
            lambda v: v.__setitem__("admittedAtUnixMilliseconds", 1003),
            lambda v: v.__setitem__("clockAvailable", False),
        ):
            with self.subTest(change=change):
                self.reject(lambda claim: self.change_input(claim, 0, change))

    def test_mutated_retained_source_and_ancestry_reject(self):
        for field in ("source", "creationRequest", "expectedControl"):
            with self.subTest(field=field):
                def change(claim):
                    raw = bytearray(base64.b64decode(claim[field]))
                    raw[-1] = 32 if raw[-1] != 32 else 33
                    claim[field] = proof.b64(bytes(raw))
                self.reject(change)
        for field in ("parentLedgers", "r03Ledger", "ownLedger"):
            with self.subTest(field=field):
                def change(claim):
                    if field == "parentLedgers":
                        first = next(x for x in claim[field] if x)
                        first[0] = proof.b64(b"{}")
                    else:
                        claim[field][0] = proof.b64(b"{}")
                self.reject(change)

    def test_nonidentical_open_retry_and_skipped_receipt_reject(self):
        self.reject(lambda claim: claim.__setitem__(
            "retryInput", claim["continuationInputs"][0]))
        self.reject(lambda claim: self.change_input(
            claim, 1, lambda v: v.__setitem__(
                "previousReceiptId", self.axis["state"]["rbaOpenReceiptId"])))

    def test_intermediate_step_actor_and_final_receipt_reject(self):
        self.reject(lambda claim: self.change_input(
            claim, 2, lambda v: v.__setitem__("actor", "commonwealth")))
        self.reject(lambda claim: self.change_input(
            claim, 4, lambda v: v.__setitem__(
                "previousReceiptId", "p01n." + "0" * 64)))

    def test_frozen_selected_partition_and_child_caps(self):
        rows = proof.pinned_rows()
        self.assertEqual(44, len(rows))
        self.assertEqual(["axis"] * 22 + ["commonwealth"] * 22,
                         [row["actingSide"] for row in rows])
        self.assertEqual(256, proof.cuts.FIXTURE["candidateAbsent"])
        self.assertFalse(proof.cuts.FIXTURE["admitted"])
        self.assertEqual([], proof.cuts.FIXTURE["publicActions"])
        self.assertLessEqual(len(proof.selected.encode(self.axis["claim"])),
                             16 * 1024 * 1024)


if __name__ == "__main__":
    unittest.main()
