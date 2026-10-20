#!/usr/bin/env python3
"""Fail-sensitive tests for original-root private RBA opening and typed test decline."""
import base64
import copy
import unittest
import importlib.util
from pathlib import Path

ROOT = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location(
    "all44_rba_proof", ROOT / "verify-combat-p01-all44-rba-decline-fresh-v1.py")
proof = importlib.util.module_from_spec(spec)
spec.loader.exec_module(proof)


class FreshDeclineTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        rows = proof.pinned_rows()
        cls.axis = proof.build(rows[0])
        cls.commonwealth = proof.build(rows[22])

    def test_both_original_orientations_and_exact_cold_bytes(self):
        for case, defender in ((self.axis, "commonwealth"),
                               (self.commonwealth, "axis")):
            with self.subTest(defender=defender):
                observed = proof.child(case["claim"])
                self.assertNotIn("rejected", observed)
                for field, expected in case["expected"].items():
                    self.assertEqual(expected, observed[field], field)
                self.assertEqual(defender, case["state"]["rbaDecision"]["owner"])
                self.assertEqual("declined", case["after"]["rbaOutcome"])
                self.assertFalse(case["after"]["closed"])
                self.assertTrue(case["final"]["closed"])
                self.assertEqual(3, case["final"]["stepIndex"])
                self.assertTrue(observed["finalPositionId"].endswith(
                    ".force-assignment"))
                self.assertEqual(case["finalReceipt"],
                                 observed["finalReceiptId"])
                self.assertFalse(observed["admitted"])
                self.assertEqual([], observed["publicActions"])

    def reject_input(self, change):
        claim = copy.deepcopy(self.axis["claim"])
        value = proof.selected.parse_input(
            base64.b64decode(claim["declineInput"]))
        change(value)
        claim["declineInput"] = proof.b64(proof.selected.encode(value))
        observed = proof.child(claim)
        self.assertEqual("fresh-rba-decline", observed.get("rejected"), observed)

    def test_wrong_actor_decision_participant_and_receipt_reject(self):
        for change in (
            lambda v: v.__setitem__("actor", "axis"),
            lambda v: v.__setitem__("decisionId", "p01d." + "0" * 64),
            lambda v: v["participant"].__setitem__("elementId", "forged"),
            lambda v: v.__setitem__("previousReceiptId", "p01o." + "0" * 64),
        ):
            with self.subTest(change=change):
                self.reject_input(change)

    def test_deadline_equality_and_unavailable_clock_reject(self):
        deadline = self.axis["state"]["rbaDecision"]["timing"][
            "deadlineUnixMilliseconds"]
        self.reject_input(lambda v: v.__setitem__(
            "admittedAtUnixMilliseconds", deadline))
        self.reject_input(lambda v: v.__setitem__("clockAvailable", False))

    def test_mutated_retained_source_and_ancestry_reject(self):
        for field in ("source", "creationRequest", "expectedControl"):
            with self.subTest(field=field):
                claim = copy.deepcopy(self.axis["claim"])
                raw = bytearray(base64.b64decode(claim[field]))
                raw[-1] = 32 if raw[-1] != 32 else 33
                claim[field] = proof.b64(bytes(raw))
                self.assertEqual("fresh-rba-decline",
                                 proof.child(claim).get("rejected"))
        for field in ("parentLedgers", "r03Ledger", "ownLedger"):
            with self.subTest(field=field):
                claim = copy.deepcopy(self.axis["claim"])
                target = claim[field][0] if field != "parentLedgers" else (
                    claim[field][0][0] if claim[field][0] else None)
                if target is None:
                    first = next(x for x in claim[field] if x)
                    first[0] = proof.b64(b"{}")
                elif field == "parentLedgers":
                    claim[field][0][0] = proof.b64(b"{}")
                else:
                    claim[field][0] = proof.b64(b"{}")
                self.assertEqual("fresh-rba-decline",
                                 proof.child(claim).get("rejected"))

    def test_nonidentical_open_retry_rejects(self):
        claim = copy.deepcopy(self.axis["claim"])
        claim["retryInput"] = claim["declineInput"]
        self.assertEqual("fresh-rba-decline",
                         proof.child(claim).get("rejected"))

    def test_completion_requires_exact_decline_chain(self):
        claim = copy.deepcopy(self.axis["claim"])
        value = proof.selected.parse_input(
            base64.b64decode(claim["completionInput"]))
        value["previousReceiptId"] = self.axis["state"]["rbaOpenReceiptId"]
        claim["completionInput"] = proof.b64(proof.selected.encode(value))
        self.assertEqual("fresh-rba-decline",
                         proof.child(claim).get("rejected"))


if __name__ == "__main__":
    unittest.main()
