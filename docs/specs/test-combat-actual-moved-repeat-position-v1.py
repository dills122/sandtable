#!/usr/bin/env python3
"""Focused genuine-source tests for the private P01 assessment reference."""
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import unittest
from unittest import mock

ROOT = Path(__file__).resolve().parent


def load(name, file):
    spec = importlib.util.spec_from_file_location(name, ROOT / file)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


r03 = load("p01_r03", "verify-combat-actual-moved-repeat-breakdown-completion-v1.py")
p01 = load("p01_reference", "verify-combat-actual-moved-repeat-position-v1.py")
FIXTURE = json.loads((ROOT / "fixtures/combat-actual-moved-repeat-position-v1.json").read_text())


def terminal(index):
    row = r03.r02.semantic_rows()[index]
    parent_source, args = r03.parent_terminal(row)
    source = r03.source_for(parent_source)
    _, context, before = r03.replay(source, args, [])
    command = r03.raw(r03.command(context, before), "Input")
    source, ledger, _, _ = r03.apply(source, args, [], command)
    return source, args, ledger


class P01ReferenceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.paid = terminal(0)
        cls.armed = terminal(36)
        cls.other_side = terminal(168)

    def test_authentic_read_only_shapes(self):
        for source, ledgers, r03_ledger, expected in (
                (*self.paid, "provisional-empty"), (*self.armed, "provisional-positive"),
                (*self.other_side, "provisional-positive")):
            _, _, before = r03.replay(source, ledgers, r03_ledger)
            result = p01.assess(source, ledgers, r03_ledger)
            _, _, after = r03.replay(source, ledgers, r03_ledger)
            self.assertEqual(expected, result["physicalShape"])
            self.assertEqual(expected == "provisional-positive", result["placementShapeVerified"])
            self.assertFalse(result["admitted"])
            self.assertEqual(before, after)
            self.assertEqual(before["randomState"], after["randomState"])
            self.assertEqual([], result["publicActions"])

    def test_fixture_samples_are_genuine_roots(self):
        self.assertEqual([0, 36, 168], [x["index"] for x in FIXTURE["genuineSamples"]])
        self.assertEqual(300, FIXTURE["terminalRows"])
        for sample in FIXTURE["genuineSamples"]:
            row = r03.r02.semantic_rows()[sample["index"]]
            self.assertEqual(sample["rowKey"], row["rowKey"])
            source, ledgers, r03_ledger = (self.paid, self.armed, self.other_side)[
                [0, 36, 168].index(sample["index"])]
            state = r03.replay(source, ledgers, r03_ledger)[2]
            self.assertEqual(sample["actingSide"], state["activeCycle"]["actingSide"])
            self.assertEqual(sample["physicalShape"], p01.assess(source, ledgers, r03_ledger)["physicalShape"])

    def test_private_identity_is_stable_and_bound_to_source(self):
        source, ledgers, r03_ledger = self.armed
        first = p01.assess(source, ledgers, r03_ledger)
        second = p01.assess(source, ledgers, r03_ledger)
        self.assertEqual(first, second)
        self.assertTrue(first["privateCandidateId"].startswith("p01."))
        self.assertNotIn(first["privateCandidateId"], str(first["publicActions"]))

    def test_private_identity_matches_frozen_preimage_vector(self):
        source, ledgers, r03_ledger = self.armed
        state = r03.replay(source, ledgers, r03_ledger)[2]
        pair = p01.physical_shape(state, p01.selected_content())[1]
        golden = FIXTURE["privateCandidateGolden"]
        self.assertEqual(36, golden["rowIndex"])
        self.assertEqual(p01.SCHEMA["privateCandidatePreimageFields"],
                         list(json.loads(golden["preimageCanonicalUtf8"])))
        expected = "p01." + hashlib.sha256(
            p01.SCHEMA["candidateIdentityDomain"].encode("ascii") + b"\0"
            + golden["preimageCanonicalUtf8"].encode("ascii")).hexdigest()
        self.assertEqual(golden["candidateId"], expected)
        self.assertEqual(golden["preimageCanonicalUtf8"].encode("ascii"),
                         p01.private_candidate_preimage(state, pair))
        self.assertEqual(golden["candidateId"], p01.assess(source, ledgers, r03_ledger)["privateCandidateId"])

    def test_ledger_mutation_rejects_before_classification(self):
        source, ledgers, r03_ledger = self.armed
        changed = copy.deepcopy(ledgers)
        changed[10][0] = changed[10][0][:-1] + b" "
        with self.assertRaises(ValueError):
            p01.assess(source, changed, r03_ledger)

    def test_nonterminal_and_source_mutation_reject(self):
        source, ledgers, r03_ledger = self.armed
        with self.assertRaises(ValueError):
            p01.assess(source, ledgers, [])
        with self.assertRaises(ValueError):
            p01.assess(source[:-1] + b" ", ledgers, r03_ledger)

    def test_all_nonterminal_parent_cuts_reject_before_shape(self):
        source, ledgers, r03_ledger = self.armed
        with mock.patch.object(p01, "physical_shape", side_effect=AssertionError("classified")):
            for ledger_index, ledger in enumerate(ledgers[1:], 1):
                for cut in range(len(ledger)):
                    with self.subTest(ledger=ledger_index, cut=cut):
                        changed = copy.deepcopy(ledgers)
                        changed[ledger_index] = changed[ledger_index][:cut]
                        with self.assertRaises(ValueError):
                            p01.assess(source, changed, r03_ledger)

    def test_missing_reordered_and_substituted_parent_ledgers_reject(self):
        source, ledgers, r03_ledger = self.armed
        missing = copy.deepcopy(ledgers)
        missing.pop(7)
        reordered = copy.deepcopy(ledgers)
        reordered[1][0], reordered[1][1] = reordered[1][1], reordered[1][0]
        substituted = copy.deepcopy(ledgers)
        substituted[10] = copy.deepcopy(self.other_side[1][10])
        for name, changed in (("missing", missing), ("reordered", reordered),
                              ("substituted", substituted)):
            with self.subTest(name=name), mock.patch.object(
                    p01, "physical_shape", side_effect=AssertionError("classified")):
                with self.assertRaises(ValueError):
                    p01.assess(source, changed, r03_ledger)

    def test_swapped_nonempty_parent_ledger_arguments_reject_before_shape(self):
        source, ledgers, r03_ledger = self.armed
        first, second = 7, 8
        self.assertTrue(ledgers[first] and ledgers[second])
        self.assertNotEqual(ledgers[first], ledgers[second])
        changed = copy.deepcopy(ledgers)
        changed[first], changed[second] = changed[second], changed[first]
        with mock.patch.object(p01, "physical_shape", side_effect=AssertionError("classified")):
            with self.assertRaises(ValueError):
                p01.assess(source, changed, r03_ledger)

    def test_each_independent_parent_input_mutation_rejects(self):
        source, ledgers, r03_ledger = self.armed
        with mock.patch.object(p01, "physical_shape", side_effect=AssertionError("classified")):
            for index in range(11):
                with self.subTest(ledger=index):
                    changed = copy.deepcopy(ledgers)
                    if index == 0:
                        changed[0]["campaignId"] += ".mutated"
                    elif changed[index]:
                        changed[index][0] = b"{}"
                    else:
                        changed[index] = [b"{}"]
                    with self.assertRaises(ValueError):
                        p01.assess(source, changed, r03_ledger)

    def test_source_field_and_dependency_pin_mutations_reject_before_shape(self):
        source, ledgers, r03_ledger = self.armed
        changed = r03.parse(source, "Source")
        event = json.loads(changed["breakdownEventCanonicalUtf8"][0])
        event["stateVersion"] += 1
        changed["breakdownEventCanonicalUtf8"][0] = r03.encode(event).decode("ascii")
        field_mutation = r03.raw(changed, "Source")
        with mock.patch.object(p01, "physical_shape", side_effect=AssertionError("classified")):
            with self.assertRaises(ValueError):
                p01.assess(field_mutation, ledgers, r03_ledger)
            path = p01.SCHEMA["contentPath"]
            with mock.patch.dict(p01.r03.PINS, {path: "0" * 64}):
                with self.assertRaises(ValueError):
                    p01.assess(source, ledgers, r03_ledger)

    def test_retry_and_private_output_golden_hashes(self):
        for index, source_data in ((36, self.armed), (168, self.other_side)):
            source, ledgers, r03_ledger = source_data
            before = r03.replay(source, ledgers, r03_ledger)[2]
            originals = copy.deepcopy((source, ledgers, r03_ledger))
            first = p01.assess(source, ledgers, r03_ledger)
            retry = p01.assess(source, ledgers, r03_ledger)
            with self.subTest(index=index):
                self.assertEqual(first, retry)
                self.assertEqual(originals, (source, ledgers, r03_ledger))
                self.assertEqual(before, r03.replay(source, ledgers, r03_ledger)[2])
                self.assertEqual("unsupported", first["status"])
                self.assertIs(first["admitted"], False)
                self.assertEqual([], first["publicActions"])
                self.assertEqual(FIXTURE["assessmentGoldenSha256"][str(index)],
                                 hashlib.sha256(p01.canonical(first)).hexdigest())
        self.assertNotEqual(p01.assess(*self.armed)["privateCandidateId"],
                            p01.assess(*self.other_side)["privateCandidateId"])

    def test_unsupported_physical_variants_do_not_become_empty(self):
        source, ledgers, r03_ledger = self.armed
        state = r03.replay(source, ledgers, r03_ledger)[2]
        content = p01.selected_content()
        extra = copy.deepcopy(state)
        extra["world"]["elements"].append(copy.deepcopy(extra["world"]["elements"][0]))
        self.assertEqual("unresolved", p01.physical_shape(extra, content)[0])
        surrender = copy.deepcopy(state)
        surrender["world"]["elements"][0]["operationalState"]["cohesionLevel"] = -17
        self.assertEqual("unresolved", p01.physical_shape(surrender, content)[0])
        blocked = copy.deepcopy(content)
        blocked["edges"][0]["features"] = ["impassable"]
        self.assertEqual("unresolved", p01.physical_shape(state, blocked)[0])


if __name__ == "__main__":
    unittest.main()
