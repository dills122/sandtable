#!/usr/bin/env python3
"""Genuine R03 ancestry and canonical input negatives for P01 no-attack v0."""
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


reference = load("p01_no_attack_test_parent", "test-combat-actual-moved-repeat-position-v1.py")
boundary = load("p01_no_attack", "verify-combat-p01-no-attack-v0.py")


class NoAttackBoundaryTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.paid = reference.terminal(0)
        cls.armed = reference.terminal(36)
        cls.other_side = reference.terminal(168)

    def test_authentic_shapes_cannot_emit_twelfth_event(self):
        for source, parents, r03 in (self.paid, self.armed, self.other_side):
            with self.subTest(source=source[-80:]):
                _, _, before = boundary.p01.r03.replay(source, parents, r03)
                result = boundary.inspect(source, parents, r03, [])
                _, _, after = boundary.p01.r03.replay(source, parents, r03)
                self.assertEqual("unsupported", result["status"])
                self.assertFalse(result["admitted"])
                self.assertEqual([], result["twelfthLedger"])
                self.assertEqual([], result["publicActions"])
                self.assertEqual(before["world"], after["world"])
                self.assertEqual(before["randomState"], after["randomState"])
        self.assertEqual("provisional-empty", boundary.inspect(*self.paid, [])["physicalShape"])
        self.assertEqual("provisional-positive", boundary.inspect(*self.armed, [])["physicalShape"])

    def test_canonical_proposed_opening_is_still_unissuable(self):
        source, parents, r03 = self.paid
        _, _, state = boundary.p01.r03.replay(source, parents, r03)
        data = dict(contractVersion=0, kind="open-zero-candidate-selection", actor="system",
                    creationBinding=state["world"]["creationBinding"],
                    sourceHash=boundary.p01.digest(boundary.p01.r03.raw(
                        json.loads(source), "Source")),
                    cycleId=state["cycleId"],
                    positionId=state["sequencePosition"]["positionId"],
                    expectedPriorVersion=state["stateVersion"],
                    expectedPriorPrefix=state["prefix"],
                    predecessorReceiptId=state["breakdownCompletionReceiptId"],
                    supportCertificateHash="sha256:" + "0" * 64,
                    candidateCount=0, decisionId=None, trustedTime=None, deadline=None)
        raw = boundary.opening_input_bytes(data)
        self.assertEqual(data, boundary.parse_opening_input(raw))
        with self.assertRaisesRegex(boundary.Invalid, "CMB-P01-NA-004"):
            boundary.inspect(source, parents, r03, [raw])
        for change in (dict(actor="player"), dict(candidateCount=1),
                       dict(deadline=0), dict(expectedPriorVersion=True)):
            with self.subTest(change=change):
                proposed = {**data, **change}
                with self.assertRaises(boundary.Invalid):
                    boundary.opening_input_bytes(proposed)

    def test_duplicate_reordered_and_padded_opening_bytes_reject(self):
        source, parents, r03 = self.paid
        _, _, state = boundary.p01.r03.replay(source, parents, r03)
        value = dict.fromkeys(boundary.SCHEMA["zeroCandidateOpeningInputFields"])
        value.update(contractVersion=0, kind="open-zero-candidate-selection", actor="system",
                     creationBinding=state["world"]["creationBinding"],
                     sourceHash=boundary.p01.digest(source), cycleId=state["cycleId"],
                     positionId=state["sequencePosition"]["positionId"],
                     expectedPriorVersion=state["stateVersion"],
                     expectedPriorPrefix=state["prefix"],
                     predecessorReceiptId=state["breakdownCompletionReceiptId"],
                     supportCertificateHash="sha256:" + "0" * 64,
                     candidateCount=0, decisionId=None, trustedTime=None, deadline=None)
        raw = boundary.opening_input_bytes(value)
        variants = [raw + b" ", b"\xef\xbb\xbf" + raw,
                    raw.replace(b'"kind":', b'"kind":"duplicate","kind":', 1),
                    json.dumps(dict(reversed(list(value.items()))),
                               separators=(",", ":")).encode("ascii")]
        for candidate in variants:
            with self.subTest(candidate=candidate[:50]), self.assertRaises(boundary.Invalid):
                boundary.parse_opening_input(candidate)

    def test_mutated_ancestry_rejects_before_any_transition(self):
        source, parents, r03 = self.armed
        damaged = copy.deepcopy(parents)
        damaged[10][0] += b" "
        with self.assertRaises(ValueError):
            boundary.inspect(source, damaged, r03, [])
        with self.assertRaises(ValueError):
            boundary.inspect(source, parents, [], [])
        with self.assertRaises(boundary.Invalid):
            boundary.inspect(source, parents, r03, [b"{}", b"{}"])


if __name__ == "__main__":
    unittest.main()
