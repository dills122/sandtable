#!/usr/bin/env python3
"""Fail-sensitive checks for the bounded all-root ancestor-cut rejection proof."""
import copy
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
from unittest import mock

ROOT = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location(
    "all_prefix_reject", ROOT / "verify-combat-p01-all-prefix-reject-v1.py")
proof = importlib.util.module_from_spec(spec)
spec.loader.exec_module(proof)


class P01AllPrefixRejectTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.fixture, cls.rows = proof.reviewed_rows()
        cls.semantic = proof.r03.r02.semantic_rows()
        cls.sample = proof.genuine_root(
            36, cls.rows[36], cls.semantic,
            cls.fixture["ledgerVectorRootsSha256"][36])

    def test_reviewed_all_300_pin_and_authentic_both_side_samples(self):
        self.assertEqual(300, len(self.rows))
        self.assertEqual(proof.REVIEWED_ROOT, proof.digest(proof.encode(self.rows)))
        sides = set()
        for index in (36, 168):
            source, ledgers, own, shape, _ = proof.genuine_root(
                index, self.rows[index], self.semantic,
                self.fixture["ledgerVectorRootsSha256"][index])
            self.assertEqual("provisional-positive", shape)
            sides.add(proof.r03.replay(source, ledgers, own)[2]
                      ["activeCycle"]["actingSide"])
        self.assertEqual({"axis", "commonwealth"}, sides)

    def test_source_control_and_proof_pin_mutations_reject(self):
        for field in ("sourceHash", "controlHash", "proofHash"):
            changed = copy.deepcopy(self.rows[36])
            changed[field] = "sha256:" + "0" * 64
            with self.subTest(field=field), self.assertRaisesRegex(
                    AssertionError, "frozen authentic Source/Control/Proof"):
                proof.genuine_root(36, changed, self.semantic,
                                   self.fixture["ledgerVectorRootsSha256"][36])

        with self.assertRaisesRegex(AssertionError, "frozen authentic CreationRequest"):
            proof.genuine_root(36, self.rows[36], self.semantic, "0" * 64)

    def test_parent_prefix_and_missing_own_reject_before_shape(self):
        source, ledgers, own, _, _ = self.sample
        before = copy.deepcopy((ledgers, own))
        with mock.patch.object(proof.p01, "physical_shape",
                               side_effect=AssertionError("classification reached")):
            for ledger_index in range(1, 11):
                if ledgers[ledger_index]:
                    proof.reject_cut(source, ledgers, own, ledger_index,
                                     len(ledgers[ledger_index]) - 1)
            proof.reject_cut(source, ledgers, own, 11, 0)
        self.assertEqual(before, (ledgers, own))

    def test_accepted_cut_is_detected(self):
        source, ledgers, own, _, _ = self.sample
        with mock.patch.object(proof.p01, "assess", return_value={}), \
                self.assertRaisesRegex(AssertionError, "accepted truncated ledger"):
            proof.reject_cut(source, ledgers, own, 1, 0)

    def test_ordered_result_root_ignores_object_key_order_but_not_row_order(self):
        original = [{"index": 0, "rowKey": "a"},
                    {"index": 1, "rowKey": "b"}]
        reordered_keys = [{"rowKey": "a", "index": 0},
                          {"rowKey": "b", "index": 1}]
        self.assertEqual(proof.result_digest(original),
                         proof.result_digest(reordered_keys))
        self.assertNotEqual(proof.result_digest(original),
                            proof.result_digest(list(reversed(original))))

    def test_missing_duplicate_and_wrong_head_shards_reject(self):
        head = proof.tested_head()
        with tempfile.TemporaryDirectory() as directory:
            paths = [Path(directory) / f"shard-{index}.json" for index in range(4)]
            for index, path in enumerate(paths):
                path.write_text(json.dumps(dict(contractVersion=1, testedSha=head,
                                                shard=index,
                                                orderedReviewedRootSha256=proof.REVIEWED_ROOT,
                                                rows=[])))
            with self.assertRaisesRegex(AssertionError, "full shard index coverage"):
                proof.combine(paths)
            first = json.loads(paths[0].read_text())
            first["testedSha"] = "0" * 40
            paths[0].write_text(json.dumps(first))
            with self.assertRaisesRegex(AssertionError, "same-head same-shard"):
                proof.combine(paths)
            first["testedSha"] = head
            first["rows"] = [{"index": value} for value in range(0, 300, 4)]
            paths[0].write_text(json.dumps(first))
            for shard in range(1, 4):
                artifact = json.loads(paths[shard].read_text())
                artifact["rows"] = [{"index": value}
                                    for value in range(shard, 300, 4)]
                paths[shard].write_text(json.dumps(artifact))
            second = json.loads(paths[1].read_text())
            second["rows"][0]["index"] = 0
            paths[1].write_text(json.dumps(second))
            with self.assertRaisesRegex(AssertionError, "full shard index coverage"):
                proof.combine(paths)


if __name__ == "__main__":
    unittest.main()
