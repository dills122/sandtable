#!/usr/bin/env python3
"""Genuine ancestor and adversarial checks for the private ordinal-two debit."""
import copy
import importlib.util
import json
import unittest
from pathlib import Path
from unittest import mock

ROOT = Path(__file__).resolve().parent


def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, ROOT / filename)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


fixture = load("paid_fixture", "test-combat-p01-force-assignment-v1.py")
ref = load("paid_reference", "verify-combat-p01-paid-commit-v1.py")
assignment = fixture.ref
GOLDEN = json.loads(
    (ROOT / "fixtures/combat-p01-paid-commit-v1.json").read_text())


def prepared(root, selected_source, selected_ledger, order):
    source = assignment.source_for(selected_source)
    state = assignment.initial(selected_source, root[1], root[2], selected_ledger)
    ledger = []
    for kind, role, now in (
            ("open-assignment", None, 2000),
            ("seal-assignment", order[0], 2001),
            ("seal-assignment", order[1], 2002),
            ("complete-step", None, None),
            ("complete-step", None, None)):
        owner = next((x["owner"] for x in state["slots"] if x["role"] == role),
                     "system")
        value = assignment.expected_input(state, kind, actor=owner,
                                          role=role, now=now)
        raw_input = assignment.input_bytes(value)
        state, event, _ = assignment.transition(state, value)
        packet = assignment.parse_source(source)
        packet["eventCanonicalUtf8"].append(event.decode("ascii"))
        source = assignment.encode(packet)
        ledger.append(raw_input)
    assert state["status"] == "prepared" and state["stepIndex"] == 5
    return source, ledger


class PaidCommitTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.roots = {"axis": fixture.fixture.terminal(36),
                     "commonwealth": fixture.fixture.terminal(168)}
        cls.selected = {side: fixture.selected_path(root)
                        for side, root in cls.roots.items()}
        cls.paths = {}
        for side, root in cls.roots.items():
            for order in (("attacker", "defender"), ("defender", "attacker")):
                fa_source, fa_ledger = prepared(
                    root, *cls.selected[side], order)
                cls.paths[side, order] = (fa_source, fa_ledger)

    def start(self, side="axis", order=("attacker", "defender")):
        root = self.roots[side]
        selected_source, selected_ledger = self.selected[side]
        fa_source, fa_ledger = self.paths[side, order]
        source = ref.source_for(fa_source)
        state = ref.replay(source, root[1], root[2],
                           selected_ledger, fa_ledger, [])
        return root, selected_ledger, fa_ledger, source, state

    def test_four_genuine_prepared_traces_pay_atomically_before_any_roll(self):
        for side in ("axis", "commonwealth"):
            for order in (("attacker", "defender"), ("defender", "attacker")):
                with self.subTest(side=side, order=order):
                    root, selected_ledger, fa_ledger, source, state = self.start(
                        side, order)
                    before = copy.deepcopy(state)
                    value = ref.expected_input(state)
                    after, event, receipt = ref.transition(state, value)
                    self.assertEqual(before, state)
                    self.assertEqual("committed", after["status"])
                    self.assertEqual(1, len(after["attackHistory"]))
                    self.assertEqual(1, len(after["targetUses"]))
                    self.assertEqual(after["attackHistory"][0]["commitmentId"],
                                     after["targetUses"][0]["commitmentId"])
                    self.assertEqual(after["commitmentId"],
                                     after["attackHistory"][0]["commitmentId"])
                    self.assertEqual(before["randomState"], after["randomState"])
                    self.assertEqual(before["position"], after["position"])
                    self.assertFalse(after["admitted"])
                    self.assertEqual([], after["publicActions"])
                    self.assertEqual(receipt, after["lastReceiptId"])
                    effect = json.loads(event)["effect"]
                    self.assertEqual([9, 3],
                                     [x["afterCp"]["numerator"] for x in effect["costs"]])
                    self.assertEqual([0, 0],
                                     [x["afterAmmo"] for x in effect["costs"]])
                    self.assertEqual([10, 10],
                                     [x["beforeAmmo"] for x in effect["costs"]])
                    self.assertEqual([10, 10],
                                     [x["components"][0]["currentToe"]
                                      for x in after["world"]["elements"]])
                    self.assertEqual([9, 3] if side == "axis" else [3, 9],
                                     [x["operationalState"]["capabilityPointsExpended"]
                                      ["numerator"] for x in after["world"]["elements"]])
                    self.assertEqual([0, 0],
                                     [x["ammunition"]["points"]
                                      for x in after["world"]["elements"]])
                    self.assertLessEqual(effect["preflight"]["matrix"]
                                         ["maximumProjectedWorldBytes"],
                                         ref.SCHEMA["limits"]["projectedWorldBytes"])
                    if side == "axis" and order == ("attacker", "defender"):
                        self.assertEqual(GOLDEN["candidateId"], effect["candidateId"])
                        self.assertEqual(GOLDEN["assignmentReceiptIds"],
                                         effect["assignmentReceiptIds"])
                        self.assertEqual(GOLDEN["commitmentId"],
                                         effect["commitmentId"])
                        self.assertEqual(GOLDEN["eventReceiptId"], receipt)
                        self.assertEqual(GOLDEN["preflightHash"],
                                         effect["preflightHash"])
                        self.assertEqual(GOLDEN["finalPrefix"], after["prefix"])
                        self.assertEqual(
                            GOLDEN["maximumProjectedWorldBytes"],
                            effect["preflight"]["matrix"]["maximumProjectedWorldBytes"])
                        self.assertEqual(GOLDEN["projectedCuts"],
                                         effect["preflight"]["matrix"]["projectedCuts"])
                        packet = ref.parse_source(source)
                        packet["eventCanonicalUtf8"].append(event.decode("ascii"))
                        committed_source = ref.encode(packet)
                        ledger = [ref.input_bytes(value)]
                        self.assertEqual(
                            ref.control_bytes(after),
                            ref.control_bytes(ref.replay(
                                committed_source, root[1], root[2],
                                selected_ledger, fa_ledger, ledger)))

    def test_changed_source_clock_receipt_and_ancestry_fail_closed(self):
        root, selected_ledger, fa_ledger, source, state = self.start()
        good = ref.expected_input(state)
        variants = []
        for field, replacement in (
                ("actor", "axis"), ("expectedPriorVersion", state["stateVersion"] - 1),
                ("expectedPriorPrefix", state["parent"]["parent"]["prefix"]),
                ("previousReceiptId", "p01y." + "0" * 64),
                ("roundId", "p01f." + "0" * 64),
                ("candidateId", "p01a." + "0" * 64)):
            bad = copy.deepcopy(good)
            bad[field] = replacement
            variants.append((field, bad))
        bad = copy.deepcopy(good)
        bad["assignmentReceiptIds"][2] = "p01v." + "0" * 64
        variants.append(("defender seal", bad))
        for label, bad in variants:
            with self.subTest(label=label), self.assertRaises(ValueError):
                ref.transition(state, bad)
        self.assertEqual("prepared", state["status"])
        packet = ref.parse_source(source)
        packet["forceAssignmentSourceCanonicalUtf8"] = (
            packet["forceAssignmentSourceCanonicalUtf8"].replace(
                "combat-force-assignment-opened", "combat-force-assignment-forged"))
        with self.assertRaises(ValueError):
            ref.replay(ref.encode(packet), root[1], root[2],
                       selected_ledger, fa_ledger, [])
        bad_ledger = fa_ledger.copy()
        bad_ledger[2] = fa_ledger[1]
        with self.assertRaises(ValueError):
            ref.replay(source, root[1], root[2],
                       selected_ledger, bad_ledger, [])

    def test_resource_weather_geometry_chart_and_capacity_mutations_reject(self):
        _, _, _, _, state = self.start()
        value = ref.expected_input(state)
        for field, mutate in (
                ("ammo", lambda s: s["world"]["elements"][0]["ammunition"]
                 .__setitem__("points", 9)),
                ("cp", lambda s: s["world"]["elements"][0]["operationalState"]
                 ["capabilityPointsExpended"].__setitem__("numerator", 5)),
                ("geometry", lambda s: s["world"]["representations"][0]
                 .__setitem__("currentLocationId", "axis-rear")),
                ("weather receipt", lambda s: s["parent"]["parent"]["candidate"]
                 .__setitem__("weatherReceiptId", "forged.weather"))):
            changed = copy.deepcopy(state)
            mutate(changed)
            with self.subTest(field=field), self.assertRaises(ValueError):
                ref.transition(changed, value)
        changed_rules = copy.deepcopy(ref.result.inputs.expected_rules())
        next(x for x in changed_rules["losses"]
             if x["role"] == "defender" and x["differential"] == 2
             and x["coordinate"] == 34)["lossPercent"] = 5
        with mock.patch.object(ref.result.inputs, "expected_rules",
                               return_value=changed_rules):
            with self.assertRaises(ValueError):
                ref.preflight(state)
        old = ref.SCHEMA["limits"]["projectedWorldBytes"]
        try:
            ref.SCHEMA["limits"]["projectedWorldBytes"] = 1
            with self.assertRaises(ValueError):
                ref.preflight(state)
        finally:
            ref.SCHEMA["limits"]["projectedWorldBytes"] = old
        with mock.patch.object(ref.geometry, "certify_row", return_value=[]):
            with self.assertRaises(ValueError):
                ref.preflight(state)
        changed_content = copy.deepcopy(ref.p01.selected_content())
        changed_content["edges"] = []
        with mock.patch.object(ref.p01, "selected_content",
                               return_value=changed_content):
            with self.assertRaises(ValueError):
                ref.preflight(state)

    def test_candidate_absent_cannot_reach_paid_boundary(self):
        root = fixture.fixture.terminal(0)
        selected_source = assignment.selected.source_for(root[0])
        with self.assertRaises(ValueError):
            assignment.selected.initial(root[0], root[1], root[2])
        with self.assertRaises(ValueError):
            assignment.initial(selected_source, root[1], root[2], [])

    def test_retry_readback_and_failed_publication_preserve_prior(self):
        root, selected_ledger, fa_ledger, source, state = self.start()
        value = ref.expected_input(state)
        raw = ref.input_bytes(value)
        before = copy.deepcopy(state)
        old_event_limit = ref.SCHEMA["limits"]["eventBytes"]
        try:
            ref.SCHEMA["limits"]["eventBytes"] = 1
            with self.assertRaises(ValueError):
                ref.transition(state, value)
        finally:
            ref.SCHEMA["limits"]["eventBytes"] = old_event_limit
        self.assertEqual(before, state)
        self.assertEqual([], state["attackHistory"])
        self.assertEqual([], state["targetUses"])
        updated, ledger, event, receipt = ref.apply(
            source, root[1], root[2], selected_ledger, fa_ledger, [], raw)
        self.assertIsNotNone(event)
        self.assertEqual(1, len(ledger))
        self.assertEqual((updated, ledger, None, receipt),
                         ref.apply(updated, root[1], root[2], selected_ledger,
                                   fa_ledger, ledger, raw))
        cut = ref.replay(updated, root[1], root[2],
                         selected_ledger, fa_ledger, ledger)
        self.assertEqual("committed", cut["status"])
        packet = ref.parse_source(updated)
        tampered = json.loads(packet["eventCanonicalUtf8"][0])
        tampered["effect"]["afterWorldHash"] = "sha256:" + "0" * 64
        packet["eventCanonicalUtf8"][0] = ref.encode(tampered).decode("ascii")
        with self.assertRaises(ValueError):
            ref.replay(ref.encode(packet), root[1], root[2],
                       selected_ledger, fa_ledger, ledger)
        with self.assertRaises(ValueError):
            ref.apply(updated, root[1], root[2], selected_ledger,
                      fa_ledger, ledger, ref.input_bytes(
                          {**value, "expectedPriorVersion": value["expectedPriorVersion"] + 1}))


if __name__ == "__main__":
    unittest.main()
