#!/usr/bin/env python3
"""Authentic predecessor and adversarial tests for private Force Assignment."""
import copy
import importlib.util
import json
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parent


def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, ROOT / filename)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


fixture = load("force_assignment_fixture",
               "test-combat-actual-moved-repeat-position-v1.py")
ref = load("force_assignment_reference", "verify-combat-p01-force-assignment-v1.py")
GOLDEN = json.loads((ROOT / "fixtures/combat-p01-force-assignment-v1.json").read_text())


def selected_path(root):
    source = ref.selected.source_for(root[0])
    ledger = []
    state = ref.selected.initial(root[0], root[1], root[2])
    attacker = state["candidate"]["attacker"]["originalSide"]
    defender = state["candidate"]["defender"]["originalSide"]
    for kind, actor, now in (
        ("open-selection", "system", 1000),
        ("select-assault", attacker, 1001),
        ("complete-step", "system", None),
        ("complete-step", "system", None),
        ("open-rba", "system", 1002),
        ("decline-rba", defender, 1003),
        ("complete-step", "system", None),
    ):
        value = ref.selected.expected_input(state, kind, actor=actor, now=now)
        raw_input = ref.selected.input_bytes(value)
        state, event, _ = ref.selected.transition(state, value)
        packet = ref.selected.parse_source(source)
        packet["eventCanonicalUtf8"].append(event.decode("ascii"))
        source = ref.encode(packet)
        ledger.append(raw_input)
    assert state["rbaOutcome"] == "declined" and state["stepIndex"] == 3
    return source, ledger


class ForceAssignmentTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.axis = fixture.terminal(36)
        cls.commonwealth = fixture.terminal(168)
        cls.selected = {"axis": selected_path(cls.axis),
                        "commonwealth": selected_path(cls.commonwealth)}

    def start(self, root, actor):
        selected_source, selected_ledger = self.selected[actor]
        source = ref.source_for(selected_source)
        state = ref.replay(source, root[1], root[2], selected_ledger, [])
        return source, [], state, selected_ledger

    def append(self, root, selected_ledger, source, ledger, state, kind,
               *, actor="system", role=None, now=None, available=True,
               cuts=False):
        value = ref.expected_input(state, kind, actor=actor, role=role,
                                   now=now, available=available)
        raw_input = ref.input_bytes(value)
        after, event, receipt = ref.transition(state, value)
        packet = ref.parse_source(source)
        packet["eventCanonicalUtf8"].append(event.decode("ascii"))
        next_source = ref.encode(packet)
        next_ledger = [*ledger, raw_input]
        self.assertEqual(state["parent"]["parent"]["world"],
                         after["parent"]["parent"]["world"])
        self.assertEqual(state["parent"]["parent"]["randomState"],
                         after["parent"]["parent"]["randomState"])
        self.assertFalse(after["admitted"])
        self.assertEqual([], after["publicActions"])
        self.assertEqual(receipt, after["lastReceiptId"])
        if cuts:
            self.assertEqual(ref.control_bytes(after), ref.control_bytes(ref.replay(
                next_source, root[1], root[2], selected_ledger, next_ledger)))
        return next_source, next_ledger, after, json.loads(event)

    def test_both_orientations_and_seal_orders_to_prepared_close_assault(self):
        for root, actor, order in ((self.axis, "axis", ("attacker", "defender")),
                                   (self.commonwealth, "commonwealth",
                                    ("defender", "attacker"))):
            with self.subTest(actor=actor, order=order):
                source, ledger, state, predecessor = self.start(root, actor)
                source, ledger, state, opened = self.append(
                    root, predecessor, source, ledger, state,
                    "open-assignment", now=2000, cuts=True)
                self.assertEqual("collecting", state["status"])
                for role, now in zip(order, (2001, 2002)):
                    owner = next(x["owner"] for x in state["slots"]
                                 if x["role"] == role)
                    source, ledger, state, sealed = self.append(
                        root, predecessor, source, ledger, state,
                        "seal-assignment", actor=owner, role=role,
                        now=now, cuts=True)
                    self.assertEqual(role, next(x["role"] for x in state["slots"]
                                                if x["sealedReceiptId"] ==
                                                sealed["receiptId"]))
                self.assertEqual("prepared", state["status"])
                self.assertEqual(["attacker", "defender"],
                                 [x["role"] for x in state["slots"]])
                for index in (3, 4):
                    source, ledger, state, event = self.append(
                        root, predecessor, source, ledger, state,
                        "complete-step", cuts=True)
                    self.assertEqual(index + 1, state["stepIndex"])
                    self.assertEqual(2, len(event["effect"]["proofReceipts"]))
                self.assertEqual("certified-empty-aa", event["effect"]["proofKind"])
                self.assertTrue(ref.ident(event["effect"]["certificateHash"],
                                          "sha256:"))
                self.assertTrue(state["position"]["positionId"].endswith(
                    ".close-assault"))
                self.assertFalse(state["closed"])
                if actor == "axis":
                    self.assertEqual(GOLDEN["candidateId"],
                                     state["parent"]["candidateId"])
                    self.assertEqual(GOLDEN["roundId"], state["roundId"])
                    self.assertEqual(GOLDEN["receiptIds"],
                                     [x["receiptId"] for x in state["receipts"]])
                    self.assertEqual(GOLDEN["emptyAaCertificateHash"],
                                     event["effect"]["certificateHash"])
                    self.assertEqual(GOLDEN["finalPrefix"], state["prefix"])
                with self.assertRaises(ValueError):
                    ref.transition(state, ref.expected_input(state, "complete-step"))

    def test_cancellation_preserves_one_seal_and_closes_no_attack(self):
        for root, actor in ((self.axis, "axis"),
                            (self.commonwealth, "commonwealth")):
            for cause, now, available in (("deadline", 32000, True),
                                          ("unavailable", None, False)):
                with self.subTest(actor=actor, cause=cause):
                    source, ledger, state, predecessor = self.start(root, actor)
                    source, ledger, state, _ = self.append(
                        root, predecessor, source, ledger, state,
                        "open-assignment", now=2000)
                    owner = state["slots"][0]["owner"]
                    source, ledger, state, seal = self.append(
                        root, predecessor, source, ledger, state,
                        "seal-assignment", actor=owner, role="attacker", now=2001)
                    source, ledger, state, cancel = self.append(
                        root, predecessor, source, ledger, state,
                        "cancel-assignment", now=now, available=available,
                        cuts=True)
                    self.assertEqual([seal["receiptId"]],
                                     cancel["effect"]["proofReceipts"])
                    self.assertIsNone(state["slots"][1]["sealedReceiptId"])
                    for _ in range(3):
                        source, ledger, state, event = self.append(
                            root, predecessor, source, ledger, state,
                            "complete-step", cuts=True)
                        self.assertEqual("cancelled-no-attack",
                                         event["effect"]["proofKind"])
                    self.assertTrue(state["closed"])
                    self.assertTrue(state["position"]["positionId"].endswith(
                        ".reserve-release"))
                    self.assertEqual(6, state["stepIndex"])

    def test_wrong_owner_partial_allocation_stale_and_changed_candidate(self):
        root = self.axis
        source, ledger, state, predecessor = self.start(root, "axis")
        source, ledger, state, _ = self.append(
            root, predecessor, source, ledger, state,
            "open-assignment", now=2000)
        correct = ref.expected_input(state, "seal-assignment",
                                     actor="axis", role="attacker", now=2001)
        variants = []
        for key, value in (("actor", "commonwealth"),
                           ("slotId", state["slots"][1]["slotId"]),
                           ("expectedPriorVersion", state["stateVersion"] - 1),
                           ("expectedPriorPrefix", state["parent"]["prefix"]),
                           ("previousReceiptId", state["parent"]["lastReceiptId"]),
                           ("roundId", "p01f." + "0" * 64),
                           ("admittedAtUnixMilliseconds", 32000)):
            bad = copy.deepcopy(correct)
            bad[key] = value
            variants.append((key, bad))
        for key, value in (("committedToe", 9),
                           ("componentId", "foreign.toe.infantry")):
            bad = copy.deepcopy(correct)
            bad["allocation"][key] = value
            variants.append((key, bad))
        for label, bad in variants:
            with self.subTest(label=label), self.assertRaises(ValueError):
                ref.transition(state, bad)
        modified = copy.deepcopy(state)
        modified["parent"]["candidate"]["targetLocationId"] = "forged"
        with self.assertRaises(ValueError):
            ref.transition(modified, correct)
        self.assertEqual("collecting", state["status"])

    def test_zero_seal_expiry_and_prepared_timer_do_not_fabricate_a_seal(self):
        root = self.axis
        source, ledger, state, predecessor = self.start(root, "axis")
        with self.assertRaises(ValueError):
            ref.transition(state, ref.expected_input(
                state, "open-assignment", now=1002))
        with self.assertRaises(ValueError):
            ref.transition(state, ref.expected_input(
                state, "open-assignment",
                now=ref.SCHEMA["limits"]["maximumUtcMilliseconds"] - 29999))
        source, ledger, state, _ = self.append(
            root, predecessor, source, ledger, state,
            "open-assignment", now=2000)
        with self.assertRaises(ValueError):
            ref.transition(state, ref.expected_input(
                state, "cancel-assignment", now=31999))
        source, ledger, cancelled, event = self.append(
            root, predecessor, source, ledger, state,
            "cancel-assignment", now=32000)
        self.assertEqual([], event["effect"]["proofReceipts"])
        self.assertEqual([None, None],
                         [x["sealedReceiptId"] for x in cancelled["slots"]])
        self.assertEqual("cancelled", cancelled["status"])
        with self.assertRaises(ValueError):
            ref.transition(cancelled, ref.expected_input(
                cancelled, "seal-assignment", actor="axis",
                role="attacker", now=32001))

        source, ledger, state, predecessor = self.start(root, "axis")
        source, ledger, state, _ = self.append(
            root, predecessor, source, ledger, state,
            "open-assignment", now=2000)
        for role, owner, now in (("attacker", "axis", 2001),
                                 ("defender", "commonwealth", 2002)):
            source, ledger, state, _ = self.append(
                root, predecessor, source, ledger, state,
                "seal-assignment", actor=owner, role=role, now=now)
        self.assertEqual("prepared", state["status"])
        with self.assertRaises(ValueError):
            ref.transition(state, ref.expected_input(
                state, "cancel-assignment", now=32000))

    def test_retry_and_tampered_event_ledger(self):
        root = self.axis
        source, ledger, state, predecessor = self.start(root, "axis")
        raw = ref.input_bytes(ref.expected_input(state, "open-assignment", now=2000))
        updated, next_ledger, event, receipt = ref.apply(
            source, root[1], root[2], predecessor, ledger, raw)
        self.assertIsNotNone(event)
        self.assertEqual((updated, next_ledger, None, receipt), ref.apply(
            updated, root[1], root[2], predecessor, next_ledger, raw))
        packet = ref.parse_source(updated)
        packet["eventCanonicalUtf8"][0] = packet["eventCanonicalUtf8"][0].replace(
            "full-ten-toe-slots", "forged-full-slots")
        for bad_source, bad_ledger in ((ref.encode(packet), next_ledger),
                                       (updated, [])):
            with self.assertRaises(ValueError):
                ref.replay(bad_source, root[1], root[2], predecessor,
                           bad_ledger)

    def test_selected_cancelled_and_missing_predecessor_rejected(self):
        root = self.axis
        selected_source, selected_ledger = self.selected["axis"]
        with self.assertRaises(ValueError):
            ref.initial(selected_source, root[1], root[2],
                        selected_ledger[:-1])
        packet = ref.selected.parse_source(selected_source)
        packet["eventCanonicalUtf8"][-2] = packet["eventCanonicalUtf8"][-2].replace(
            "accepted-decline", "forged-decline")
        with self.assertRaises(ValueError):
            ref.initial(ref.encode(packet), root[1], root[2], selected_ledger)


if __name__ == "__main__":
    unittest.main()
