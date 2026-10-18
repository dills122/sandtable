#!/usr/bin/env python3
"""Deterministic private ordinal-two no-assault/RBA continuation tests."""
import copy
import importlib.util
import json
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parent


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, ROOT / path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


fixture = load("p01_explicit_fixture", "test-combat-actual-moved-repeat-position-v1.py")
reference = load("p01_explicit_reference", "verify-combat-p01-explicit-rba-v1.py")
GOLDEN = json.loads((ROOT / "fixtures/combat-p01-explicit-rba-v1.json").read_text())


class ExplicitRbaTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.axis = fixture.terminal(1)
        cls.commonwealth = fixture.terminal(169)
        cls.adjacent = fixture.terminal(36)

    def run_path(self, root, cancel=False, expected_actor=None):
        original, parents, r03 = root
        source = reference.source_for(original)
        ledger = []
        initial = reference.replay(source, parents, r03, ledger)
        if expected_actor is not None:
            self.assertEqual(expected_actor, initial["parent"]["activeCycle"]["actingSide"])
        defender = reference.defender_ref(initial)["originalSide"]
        self.assertIn(defender, ("axis", "commonwealth"))
        path = [("close-no-assault-selection", "system", None),
                ("complete-no-assault-step", "system", None),
                ("complete-no-assault-step", "system", None),
                ("open-rba-decision", "system", 1000),
                ("cancel-rba" if cancel else "decline-rba",
                 "system" if cancel else defender, 31000 if cancel else 1001),
                *[("complete-no-assault-step", "system", None)] * 4]
        events = []
        cuts = [(source, ledger)]
        for kind, actor, now in path:
            before = reference.replay(source, parents, r03, ledger)
            raw = reference.input_bytes(reference.expected_input(before, kind, actor=actor, now=now))
            updated, next_ledger, event, receipt = reference.apply(
                source, parents, r03, ledger, raw)
            self.assertIsNotNone(event)
            self.assertEqual((updated, next_ledger, None, receipt), reference.apply(
                updated, parents, r03, next_ledger, raw))
            source, ledger = updated, next_ledger
            events.append(json.loads(event))
            cuts.append((source, ledger))
        terminal = reference.replay(source, parents, r03, ledger)
        self.assertTrue(terminal["closed"])
        self.assertTrue(terminal["position"]["positionId"].endswith(".reserve-release"))
        self.assertEqual("cancelled" if cancel else "declined", terminal["rbaOutcome"])
        self.assertEqual(9, len(events))
        expected_order = reference.SCHEMA["eventOrders"]["cancelledDecision" if cancel
                                                          else "acceptedDecline"]
        self.assertEqual(expected_order, [event["eventType"] for event in events])
        self.assertEqual(initial["parent"]["world"], terminal["parent"]["world"])
        self.assertEqual(initial["parent"]["randomState"], terminal["parent"]["randomState"])
        self.assertEqual([], terminal["publicActions"])
        self.assertFalse(terminal["admitted"])
        self.assertIsNone(terminal["declineReceiptId"] if cancel else terminal["cancellationReceiptId"])
        if not cancel and root[0] == self.axis[0]:
            self.assertEqual(GOLDEN["zeroAssaultCertificateId"], terminal["certificateId"])
            self.assertEqual(GOLDEN["receiptIds"], [event["receiptId"] for event in events])
            self.assertEqual(GOLDEN["finalPrefix"], terminal["prefix"])
        return cuts, events

    def test_both_actor_orientations_accept_only_explicit_decline(self):
        for root, expected_actor in ((self.axis, "axis"), (self.commonwealth, "commonwealth")):
            with self.subTest(root=root[0][:50]):
                cuts, _ = self.run_path(root, expected_actor=expected_actor)
                # Replay from independent original ancestry at nonterminal cuts.
                for source, ledger in (cuts[0], cuts[3], cuts[5], cuts[-1]):
                    replayed = reference.replay(source, root[1], root[2], ledger)
                    self.assertEqual(len(ledger), len(replayed["receipts"]))

    def test_timeout_cancels_without_synthetic_decline(self):
        cuts, events = self.run_path(self.axis, cancel=True)
        self.assertEqual("combat-retreat-decision-cancelled", events[4]["eventType"])
        self.assertEqual("cancel-without-decline", events[4]["effect"]["proofKind"])
        self.assertIsNone(reference.replay(cuts[-1][0], self.axis[1], self.axis[2],
                                           cuts[-1][1])["declineReceiptId"])

    def test_clock_regression_cancels_without_decline(self):
        root = self.axis
        source = reference.source_for(root[0]); ledger = []
        for kind, now in [("close-no-assault-selection", None),
                          ("complete-no-assault-step", None),
                          ("complete-no-assault-step", None),
                          ("open-rba-decision", 1000)]:
            state = reference.replay(source, root[1], root[2], ledger)
            raw = reference.input_bytes(reference.expected_input(state, kind, now=now))
            source, ledger, _, _ = reference.apply(source, root[1], root[2], ledger, raw)
        pending = reference.replay(source, root[1], root[2], ledger)
        self.assertEqual(1000, pending["rbaDecision"]["timing"]["highWaterUnixMilliseconds"])
        self.assertEqual("unavailable", reference.steps.inputs.clock_gate(
            pending["rbaDecision"]["timing"], 999, True))
        raw = reference.input_bytes(reference.expected_input(
            pending, "cancel-rba", now=999, available=True))
        source, ledger, event, _ = reference.apply(source, root[1], root[2], ledger, raw)
        self.assertEqual("combat-retreat-decision-cancelled", json.loads(event)["eventType"])
        cancelled = reference.replay(source, root[1], root[2], ledger)
        self.assertEqual("cancelled", cancelled["rbaOutcome"])
        self.assertIsNone(cancelled["declineReceiptId"])
        self.assertIsNotNone(cancelled["cancellationReceiptId"])

    def test_pending_missing_wrong_actor_movement_and_stale_decline_fail_closed(self):
        root = self.axis
        source = reference.source_for(root[0]); ledger = []
        for kind, now in [("close-no-assault-selection", None),
                          ("complete-no-assault-step", None),
                          ("complete-no-assault-step", None),
                          ("open-rba-decision", 1000)]:
            state = reference.replay(source, root[1], root[2], ledger)
            raw = reference.input_bytes(reference.expected_input(state, kind, now=now))
            source, ledger, _, _ = reference.apply(source, root[1], root[2], ledger, raw)
        pending = reference.replay(source, root[1], root[2], ledger)
        self.assertEqual(2, pending["stepIndex"])
        for value in (
            reference.expected_input(pending, "complete-no-assault-step"),
            reference.expected_input(pending, "decline-rba", actor="axis", now=1001),
            reference.expected_input(pending, "decline-rba", actor="commonwealth", now=999),
            reference.expected_input(pending, "decline-rba", actor="commonwealth", now=31000),
            {**reference.expected_input(pending, "decline-rba", actor="commonwealth", now=1001),
             "kind": "move-rba"},
            {**reference.expected_input(pending, "decline-rba", actor="commonwealth", now=1001),
             "participant": {**reference.defender_ref(pending), "elementId": "wrong-unit"}},
        ):
            with self.subTest(kind=value["kind"], actor=value["actor"]), self.assertRaises(ValueError):
                reference.apply(source, root[1], root[2], ledger,
                                reference.encode(value))
        self.assertEqual(4, len(reference.replay(source, root[1], root[2], ledger)["receipts"]))
        unavailable = reference.input_bytes(reference.expected_input(
            pending, "cancel-rba", now=None, available=False))
        cancelled_source, cancelled_ledger, event, _ = reference.apply(
            source, root[1], root[2], ledger, unavailable)
        self.assertEqual("combat-retreat-decision-cancelled", json.loads(event)["eventType"])
        cancelled = reference.replay(cancelled_source, root[1], root[2], cancelled_ledger)
        self.assertEqual("cancelled", cancelled["rbaOutcome"])
        self.assertIsNone(cancelled["declineReceiptId"])

    def test_adjacent_or_mutated_ancestry_cannot_claim_zero_assault(self):
        for root in (self.adjacent,):
            with self.assertRaisesRegex(reference.Invalid, "CMB-P01-RBA-003"):
                reference.zero_assault_certificate(*root)
        altered = copy.deepcopy(self.axis[1])
        altered[10][0] += b" "
        with self.assertRaises(ValueError):
            reference.zero_assault_certificate(self.axis[0], altered, self.axis[2])

    def test_event_and_input_mutations_reject_at_authentic_cut(self):
        cuts, _ = self.run_path(self.axis)
        source, ledger = cuts[5]
        parsed = reference.parse_source(source)
        parsed["eventCanonicalUtf8"][4] += " "
        with self.assertRaises(ValueError):
            reference.replay(reference.encode(parsed), self.axis[1], self.axis[2], ledger)
        mutated = copy.deepcopy(ledger)
        mutated[4] += b" "
        with self.assertRaises(ValueError):
            reference.replay(source, self.axis[1], self.axis[2], mutated)


if __name__ == "__main__":
    unittest.main()
