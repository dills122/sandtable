#!/usr/bin/env python3
"""Deterministic private ordinal-two no-assault/RBA continuation tests."""
import copy
import base64
import importlib.util
import json
import subprocess
import sys
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
READBACK = ROOT / "combat-p01-explicit-rba-fresh-readback.py"


def b64(value):
    return base64.b64encode(value).decode("ascii")


def fresh_readback(root, source, ledger, *, retry=None, next_input=None):
    payload = dict(source=b64(source), creationRequest=b64(reference.encode(root[1][0])),
                   parentInputLedgers=[[b64(item) for item in part]
                                       for part in root[1][1:]],
                   r03Ledger=[b64(item) for item in root[2]],
                   ownLedger=[b64(item) for item in ledger],
                   retryInput=None if retry is None else b64(retry),
                   nextInput=None if next_input is None else b64(next_input))
    process = subprocess.run([sys.executable, "-B", str(READBACK)],
                             input=json.dumps(payload).encode("ascii"),
                             stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                             check=False, timeout=120)
    if process.returncode:
        raise AssertionError(f"fresh readback exited {process.returncode}: "
                             f"{process.stderr.decode('utf-8', 'replace')}")
    return json.loads(process.stdout)


class ExplicitRbaTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.axis = fixture.terminal(1)
        cls.commonwealth = fixture.terminal(169)
        cls.adjacent = fixture.terminal(36)

    def run_path(self, root, cancel=False, expected_actor=None, capture_controls=False):
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
        inputs = []
        controls = [reference.control_bytes(initial)] if capture_controls else []
        for kind, actor, now in path:
            before = reference.replay(source, parents, r03, ledger)
            raw = reference.input_bytes(reference.expected_input(before, kind, actor=actor, now=now))
            updated, next_ledger, event, receipt = reference.apply(
                source, parents, r03, ledger, raw)
            self.assertIsNotNone(event)
            self.assertEqual((updated, next_ledger, None, receipt), reference.apply(
                updated, parents, r03, next_ledger, raw))
            source, ledger = updated, next_ledger
            inputs.append(raw)
            events.append(json.loads(event))
            cuts.append((source, ledger))
            if capture_controls:
                controls.append(reference.control_bytes(reference.replay(source, parents, r03, ledger)))
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
        return cuts, events, inputs, controls

    def test_both_actor_orientations_accept_only_explicit_decline(self):
        for root, expected_actor in ((self.axis, "axis"), (self.commonwealth, "commonwealth")):
            with self.subTest(root=root[0][:50]):
                cuts, _, _, _ = self.run_path(root, expected_actor=expected_actor)
                # Replay from independent original ancestry at nonterminal cuts.
                for source, ledger in (cuts[0], cuts[3], cuts[5], cuts[-1]):
                    replayed = reference.replay(source, root[1], root[2], ledger)
                    self.assertEqual(len(ledger), len(replayed["receipts"]))

    def test_timeout_cancels_without_synthetic_decline(self):
        cuts, events, _, _ = self.run_path(self.axis, cancel=True)
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

    def test_clock_unavailable_before_open_cancels_without_synthetic_decision(self):
        root = self.axis
        source = reference.source_for(root[0]); ledger = []; events = []
        for kind in ("close-no-assault-selection", "complete-no-assault-step",
                     "complete-no-assault-step"):
            before = reference.replay(source, root[1], root[2], ledger)
            raw = reference.input_bytes(reference.expected_input(before, kind))
            source, ledger, event, _ = reference.apply(source, root[1], root[2], ledger, raw)
            events.append(json.loads(event)["eventType"])
        cut = reference.replay(source, root[1], root[2], ledger)
        self.assertEqual(2, cut["stepIndex"])
        self.assertIsNone(cut["rbaDecision"])
        unavailable_open = reference.input_bytes(reference.expected_input(
            cut, "open-rba-decision", now=None, available=False))
        with self.assertRaises(ValueError):
            reference.apply(source, root[1], root[2], ledger, unavailable_open)
        premature_step = reference.input_bytes(reference.expected_input(
            cut, "complete-no-assault-step"))
        with self.assertRaises(ValueError):
            reference.apply(source, root[1], root[2], ledger, premature_step)
        claimed_expiry = reference.input_bytes(reference.expected_input(
            cut, "cancel-rba", now=31000, available=True))
        with self.assertRaises(ValueError):
            reference.apply(source, root[1], root[2], ledger, claimed_expiry)
        cancel = reference.input_bytes(reference.expected_input(
            cut, "cancel-rba", now=None, available=False))
        source, ledger, event, receipt = reference.apply(
            source, root[1], root[2], ledger, cancel)
        self.assertTrue(receipt.startswith("p01c."))
        self.assertEqual("clock-unavailable-before-open", json.loads(event)["effect"]["proofKind"])
        self.assertEqual((source, ledger, None, receipt), reference.apply(
            source, root[1], root[2], ledger, cancel))
        events.append(json.loads(event)["eventType"])
        cancelled = reference.replay(source, root[1], root[2], ledger)
        self.assertEqual("cancelled", cancelled["rbaOutcome"])
        self.assertIsNone(cancelled["rbaDecision"])
        self.assertIsNone(cancelled["rbaOpenReceiptId"])
        self.assertIsNone(cancelled["declineReceiptId"])
        self.assertEqual(receipt, cancelled["cancellationReceiptId"])
        for _ in range(4):
            before = reference.replay(source, root[1], root[2], ledger)
            raw = reference.input_bytes(reference.expected_input(
                before, "complete-no-assault-step", available=False))
            source, ledger, event, _ = reference.apply(source, root[1], root[2], ledger, raw)
            events.append(json.loads(event)["eventType"])
        terminal = reference.replay(source, root[1], root[2], ledger)
        self.assertEqual(reference.SCHEMA["eventOrders"]["preOpenCancelled"], events)
        self.assertTrue(terminal["closed"])
        self.assertTrue(terminal["position"]["positionId"].endswith(".reserve-release"))
        self.assertFalse(terminal["admitted"])
        self.assertEqual([], terminal["publicActions"])
        self.assertEqual(cut["parent"]["world"], terminal["parent"]["world"])
        self.assertEqual(cut["parent"]["randomState"], terminal["parent"]["randomState"])

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
        cuts, _, _, _ = self.run_path(self.axis)
        source, ledger = cuts[5]
        parsed = reference.parse_source(source)
        parsed["eventCanonicalUtf8"][4] += " "
        with self.assertRaises(ValueError):
            reference.replay(reference.encode(parsed), self.axis[1], self.axis[2], ledger)
        mutated = copy.deepcopy(ledger)
        mutated[4] += b" "
        with self.assertRaises(ValueError):
            reference.replay(source, self.axis[1], self.axis[2], mutated)

    def test_all_accepted_decline_cuts_read_back_and_continue_in_fresh_process(self):
        root = self.axis
        cuts, events, inputs, controls = self.run_path(root, capture_controls=True)
        self.assertEqual(10, len(cuts))
        self.assertEqual(9, len(inputs))
        for index in range(len(inputs)):
            with self.subTest(cut=index):
                source, ledger = cuts[index]
                observed = fresh_readback(
                    root, source, ledger,
                    retry=None if index == 0 else inputs[index - 1],
                    next_input=inputs[index])
                self.assertNotIn("rejected", observed)
                control = json.loads(controls[index])
                self.assertEqual(b64(controls[index]), observed["control"])
                self.assertEqual(control["stateVersion"], observed["version"])
                self.assertEqual(control["prefix"], observed["prefix"])
                self.assertEqual(control["position"]["positionId"], observed["position"])
                self.assertEqual(control["lastReceiptId"], observed["lastReceiptId"])
                self.assertEqual(b64(reference.encode(control["parent"]["world"])),
                                 observed["world"])
                self.assertEqual(b64(reference.encode(control["parent"]["randomState"])),
                                 observed["randomState"])
                self.assertFalse(observed["admitted"])
                self.assertEqual([], observed["publicActions"])
                if index:
                    self.assertEqual(dict(source=b64(source),
                                          ledger=[b64(item) for item in ledger],
                                          event=None, receipt=events[index - 1]["receiptId"]),
                                     observed["retry"])
                next_source, next_ledger = cuts[index + 1]
                self.assertEqual(dict(source=b64(next_source),
                                      ledger=[b64(item) for item in next_ledger],
                                      event=b64(reference.encode(events[index])),
                                      receipt=events[index]["receiptId"],
                                      control=b64(controls[index + 1])),
                                 observed["next"])
        terminal_source, terminal_ledger = cuts[-1]
        terminal = fresh_readback(root, terminal_source, terminal_ledger,
                                  retry=inputs[-1])
        self.assertNotIn("rejected", terminal)
        self.assertEqual(b64(controls[-1]), terminal["control"])
        self.assertEqual(dict(source=b64(terminal_source),
                              ledger=[b64(item) for item in terminal_ledger],
                              event=None, receipt=events[-1]["receiptId"]),
                         terminal["retry"])
        self.assertFalse(terminal["admitted"])
        self.assertEqual([], terminal["publicActions"])
        self.assertTrue(json.loads(controls[-1])["closed"])

    def test_fresh_readback_rejects_source_event_input_length_and_stale_next(self):
        root = self.axis
        cuts, _, inputs, _ = self.run_path(root)
        source, ledger = cuts[5]
        altered_source = reference.parse_source(source)
        altered_source["predecessorSourceCanonicalUtf8"] += " "
        self.assertEqual("replay", fresh_readback(
            root, reference.encode(altered_source), ledger)["rejected"])
        altered_event = reference.parse_source(source)
        altered_event["eventCanonicalUtf8"][4] += " "
        self.assertEqual("replay", fresh_readback(
            root, reference.encode(altered_event), ledger)["rejected"])
        altered_ledger = ledger.copy()
        altered_ledger[4] += b" "
        self.assertEqual("replay", fresh_readback(
            root, source, altered_ledger)["rejected"])
        self.assertEqual("replay", fresh_readback(
            root, source, ledger[:-1])["rejected"])
        shortened_parents = (root[0], root[1][:-1], root[2])
        self.assertEqual("replay", fresh_readback(
            shortened_parents, source, ledger)["rejected"])
        stale = json.loads(inputs[5])
        stale["expectedPriorVersion"] -= 1
        self.assertEqual("next", fresh_readback(
            root, source, ledger, next_input=reference.encode(stale))["rejected"])
        self.assertEqual("next", fresh_readback(
            root, source, ledger, next_input=inputs[5] + b" ")["rejected"])


if __name__ == "__main__":
    unittest.main()
