#!/usr/bin/env python3
"""Focused authentic-root tests for private ordinal-two selected assault."""
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


fixture = load("selected_assault_fixture",
               "test-combat-actual-moved-repeat-position-v1.py")
ref = load("selected_assault_reference",
           "verify-combat-p01-selected-assault-v1.py")
GOLDEN = json.loads((ROOT / "fixtures/combat-p01-selected-assault-v1.json").read_text())


class SelectedAssaultTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.axis = fixture.terminal(36)
        cls.commonwealth = fixture.terminal(168)
        cls.paid = fixture.terminal(0)

    def begin(self, root):
        source = ref.source_for(root[0])
        state = ref.replay(source, root[1], root[2], [])
        return source, [], state

    def admit(self, root, source, ledger, before, kind, *, actor="system", now=None,
              available=True):
        raw = ref.input_bytes(ref.expected_input(
            before, kind, actor=actor, now=now, available=available))
        after, event, receipt = ref.transition(before, ref.parse_input(raw))
        self.assertIsNotNone(event)
        packet = ref.parse_source(source)
        packet["eventCanonicalUtf8"].append(event.decode("ascii"))
        updated = ref.encode(packet)
        next_ledger = [*ledger, raw]
        self.assertEqual(before["parent"]["world"], after["parent"]["world"])
        self.assertEqual(before["parent"]["randomState"],
                         after["parent"]["randomState"])
        self.assertEqual([], after["publicActions"])
        self.assertFalse(after["admitted"])
        self.assertEqual(receipt, after["lastReceiptId"])
        return updated, next_ledger, after, json.loads(event)

    def run_path(self, root, *, cancel=False, preopen=False, verify_cuts=False):
        source, ledger, state = self.begin(root)
        attacker = state["candidate"]["attacker"]["originalSide"]
        defender = state["candidate"]["defender"]["originalSide"]
        self.assertEqual(10, state["candidate"]["defenderToe"])
        self.assertEqual(state["candidate"]["targetLocationId"],
                         next(unit["currentLocationId"] for unit in
                              state["parent"]["world"]["elements"]
                              if unit["elementId"] ==
                              state["candidate"]["defender"]["elementId"]))
        events = []
        cuts = [(source, ledger, state)]
        for kind, actor, now, available in [
            ("open-selection", "system", 1000, True),
            ("select-assault", attacker, 1001, True),
            ("complete-step", "system", None, True),
            ("complete-step", "system", None, True),
            *([] if preopen else [("open-rba", "system", 1002, True)]),
            ("cancel-rba" if cancel else "decline-rba",
             "system" if cancel else defender,
             None if preopen else 31002 if cancel else 1003,
             False if preopen else True),
            ("complete-step", "system", None, True),
            *([("complete-step", "system", None, True)] * 3 if cancel else [])
        ]:
            source, ledger, state, event = self.admit(
                root, source, ledger, state, kind, actor=actor,
                now=now, available=available)
            events.append(event)
            cuts.append((source, ledger, state))
        if verify_cuts:
            for cut_source, cut_ledger, control in cuts:
                self.assertEqual(ref.control_bytes(control), ref.control_bytes(
                    ref.replay(cut_source, root[1], root[2], cut_ledger)))
        self.assertEqual(state, ref.replay(source, root[1], root[2], ledger))
        self.assertTrue(state["closed"])
        self.assertEqual("cancelled" if cancel else "declined",
                         state["rbaOutcome"])
        self.assertTrue(state["position"]["positionId"].endswith(
            ".reserve-release" if cancel else ".force-assignment"))
        self.assertIsNone(state["declineReceiptId"] if cancel
                          else state["cancellationReceiptId"])
        self.assertEqual("combat-assault-selected", events[1]["eventType"])
        self.assertEqual("combat-selected-assault-cancelled" if cancel else
                         "combat-retreat-before-assault-declined",
                         events[4 if preopen else 5]["eventType"])
        if cancel:
            self.assertEqual("cancelled-no-attack",
                             events[-1]["effect"]["proofKind"])
            self.assertTrue(all(item["effect"]["proofKind"] !=
                                "accepted-decline" for item in events))
        else:
            self.assertEqual("accepted-decline", events[-1]["effect"]["proofKind"])
            if root[0] == self.axis[0]:
                self.assertEqual(GOLDEN["candidateId"], state["candidateId"])
                self.assertEqual(GOLDEN["targetLocationId"],
                                 state["candidate"]["targetLocationId"])
                self.assertEqual(GOLDEN["receiptIds"],
                                 [item["receiptId"] for item in events])
                self.assertEqual(GOLDEN["finalPrefix"], state["prefix"])
        return source, ledger, events

    def test_both_actor_orientations_decline_to_force_assignment(self):
        for root, actor in ((self.axis, "axis"),
                            (self.commonwealth, "commonwealth")):
            with self.subTest(actor=actor):
                self.assertEqual(actor, self.begin(root)[2]["candidate"]
                                 ["attacker"]["originalSide"])
                self.run_path(root, verify_cuts=True)

    def test_both_actor_orientations_cancel_to_same_slot_release(self):
        for root in (self.axis, self.commonwealth):
            with self.subTest(actor=root[0][:40]):
                self.run_path(root, cancel=True, verify_cuts=True)

    def test_clock_unavailable_before_rba_open_cancels_without_decline(self):
        self.run_path(self.axis, cancel=True, preopen=True)

    def test_wrong_actor_target_participant_and_stale_selection_fail(self):
        root = self.axis
        source, ledger, state = self.begin(root)
        source, ledger, state, _ = self.admit(
            root, source, ledger, state, "open-selection", now=1000)
        correct = ref.expected_input(state, "select-assault", actor="axis",
                                     now=1001)
        variants = []
        for key, value in (
            ("actor", "commonwealth"),
            ("targetLocationId", "land.location.fake"),
            ("participant", state["candidate"]["defender"]),
            ("admittedAtUnixMilliseconds", 31000),
            ("expectedPriorVersion", state["stateVersion"] - 1),
            ("expectedPriorPrefix", state["parent"]["prefix"])):
            bad = copy.deepcopy(correct)
            bad[key] = value
            variants.append((key, bad))
        for label, bad in variants:
            with self.subTest(label=label), self.assertRaises(ValueError):
                ref.transition(state, bad)
        self.assertEqual(1, len(state["receipts"]))

    def test_paid_use_rejected_and_weather_tampering_rejected(self):
        _, _, paid_state = ref.p01.r03.replay(*self.paid)
        self.assertEqual("paid", paid_state["profile"])
        self.assertEqual("provisional-empty", ref.p01.physical_shape(
            paid_state, ref.p01.selected_content())[0])
        paid_actor, paid_defender = ref.p01.exact_selected_profile(
            paid_state, ref.p01.selected_content())
        edge = frozenset((paid_actor["currentLocationId"],
                          paid_defender["currentLocationId"]))
        self.assertIn(edge, {frozenset((item["firstLocationId"],
                                       item["secondLocationId"]))
                             for item in ref.p01.selected_content()["edges"]})
        self.assertEqual(0, paid_actor["ammunition"]["points"])
        with self.assertRaises(ValueError):
            self.begin(self.paid)
        root = self.axis
        parent_source, parents, r03 = root
        source, _, parent = ref.p01.r03.replay(parent_source, parents, r03)
        altered = copy.deepcopy(source)
        packet = altered
        ancestors = []
        for key in ref.p01.SCHEMA["weatherSourcePath"]:
            ancestors.append((packet, key))
            packet = json.loads(packet[key])
        index = ref.p01.SCHEMA["weatherPrefixEventIndex"]
        event = json.loads(packet["prefixEventCanonicalUtf8"][index])
        self.assertEqual("normal", event["kind"])
        event["kind"] = "sandstorm"
        packet["prefixEventCanonicalUtf8"][index] = ref.encode(event).decode("ascii")
        for enclosing, key in reversed(ancestors):
            enclosing[key] = ref.encode(packet).decode("ascii")
            packet = enclosing
        with self.assertRaises(ValueError):
            ref.candidate_from_replayed(altered, parent, parents[0])
        with self.assertRaises(ValueError):
            ref.initial(ref.encode(altered), parents, r03)

    def test_wrong_defender_late_and_preopen_claimed_timeout_fail(self):
        root = self.axis
        source, ledger, state = self.begin(root)
        for kind, actor, now in (("open-selection", "system", 1000),
                                 ("select-assault", "axis", 1001),
                                 ("complete-step", "system", None),
                                 ("complete-step", "system", None)):
            source, ledger, state, _ = self.admit(
                root, source, ledger, state, kind, actor=actor, now=now)
        claimed = ref.expected_input(state, "cancel-rba", now=31002)
        with self.assertRaises(ValueError):
            ref.transition(state, claimed)
        source, ledger, state, _ = self.admit(
            root, source, ledger, state, "open-rba", now=1002)
        wrong = ref.expected_input(state, "decline-rba",
                                   actor="axis", now=1003)
        late = ref.expected_input(state, "decline-rba",
                                  actor="commonwealth", now=31002)
        movement = copy.deepcopy(late)
        movement["kind"] = "move-rba"
        for bad in (wrong, late, movement):
            with self.assertRaises(ValueError):
                ref.transition(state, bad)

    def test_replay_cut_retry_and_next_input(self):
        root = self.axis
        source, ledger, state = self.begin(root)
        for kind, actor, now in (("open-selection", "system", 1000),
                                 ("select-assault", "axis", 1001)):
            source, ledger, state, _ = self.admit(
                root, source, ledger, state, kind, actor=actor, now=now)
        self.assertEqual(state, ref.replay(source, root[1], root[2], ledger))
        next_input = ref.input_bytes(ref.expected_input(state, "complete-step"))
        updated, next_ledger, event, receipt = ref.apply(
            source, root[1], root[2], ledger, next_input)
        self.assertIsNotNone(event)
        self.assertEqual((updated, next_ledger, None, receipt), ref.apply(
            updated, root[1], root[2], next_ledger, next_input))
        self.assertEqual(3, len(next_ledger))
        self.assertEqual("barrage",
                         ref.replay(updated, root[1], root[2], next_ledger)
                         ["position"]["positionId"].split(".")[-1])
        tampered = ref.parse_source(updated)
        tampered["eventCanonicalUtf8"][-1] = tampered["eventCanonicalUtf8"][-1].replace(
            "no-gun-positions", "forged-gun-attack")
        for bad_source, bad_ledger in (
            (ref.encode(tampered), next_ledger),
            (updated, next_ledger[:-1]),
            (updated, [*next_ledger[:-1], next_ledger[-1].replace(
                b"complete-step", b"cancel-rba")])):
            with self.assertRaises(ValueError):
                ref.replay(bad_source, root[1], root[2], bad_ledger)


if __name__ == "__main__":
    unittest.main()
