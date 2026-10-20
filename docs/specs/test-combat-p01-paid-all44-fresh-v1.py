#!/usr/bin/env python3
"""Focused physical-cut negatives for the all-44 fresh-process paid proof."""
import base64
import copy
import importlib.util
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location(
    "all44_fresh_under_test", ROOT / "verify-combat-p01-paid-all44-fresh-v1.py")
fresh = importlib.util.module_from_spec(spec)
spec.loader.exec_module(fresh)
ref = fresh.ref


def child_result(prepared, committed):
    with tempfile.TemporaryDirectory(prefix="p01-all44-fresh-negative-") as folder:
        before = Path(folder) / "prepared.json"
        after = Path(folder) / "committed.json"
        before.write_bytes(ref.encode(prepared))
        after.write_bytes(ref.encode(committed))
        process = subprocess.run(
            [sys.executable, "-B", str(fresh.CHILD), str(before), str(after)],
            stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=False,
            timeout=300)
    if process.returncode:
        raise AssertionError(process.stderr.decode("utf-8", "replace")[:1000])
    return json.loads(process.stdout)


class FreshAll44CutTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        root = fresh.cuts.paid.fixture.fixture.terminal(36)
        selected_source, selected = fresh.cuts.paid.fixture.selected_path(root)
        assignment_source, assignment = fresh.cuts.paid.prepared(
            root, selected_source, selected, ("attacker", "defender"))
        source = ref.source_for(assignment_source)
        before = ref.replay(source, root[1], root[2], selected, assignment, [])
        command = ref.input_bytes(ref.expected_input(before))
        after, event, _ = ref.transition(before, ref.parse_input(command))
        packet = ref.parse_source(source)
        packet["eventCanonicalUtf8"].append(event.decode("ascii"))
        committed_source = ref.encode(packet)
        cls.prepared = fresh.fresh_v1.payload(
            root, selected, assignment, source, [], before)
        cls.committed = fresh.fresh_v1.payload(
            root, selected, assignment, committed_source, [command], after,
            retry=command)

    def test_genuine_child_rehydrates_both_cuts_and_deduplicates_retry(self):
        result = child_result(self.prepared, self.committed)
        self.assertNotIn("rejected", result)
        self.assertEqual("prepared", result["prepared"]["status"])
        self.assertEqual("committed", result["committed"]["status"])
        self.assertEqual(self.prepared["expectedControl"],
                         result["prepared"]["control"])
        self.assertEqual(self.committed["expectedWorld"],
                         result["committed"]["world"])
        self.assertIsNone(result["retry"]["event"])
        self.assertFalse(result["prepared"]["admitted"])
        self.assertFalse(result["committed"]["admitted"])

    def test_retained_control_and_ancestor_ledger_mutations_fail_closed(self):
        broken = copy.deepcopy(self.prepared)
        broken["expectedControl"] = base64.b64encode(b"{}").decode("ascii")
        result = child_result(broken, self.committed)
        self.assertEqual("readback", result["rejected"])
        self.assertIn("prepared retained cut differs", result["error"])

        broken = copy.deepcopy(self.prepared)
        broken["selectedLedger"][0] = base64.b64encode(b"{}").decode("ascii")
        result = child_result(broken, self.committed)
        self.assertIn("rejected", result)

    def test_retained_committed_world_and_paid_input_mutations_fail_closed(self):
        broken = copy.deepcopy(self.committed)
        broken["expectedWorld"] = base64.b64encode(b"{}").decode("ascii")
        result = child_result(self.prepared, broken)
        self.assertEqual("readback", result["rejected"])
        self.assertIn("committed retained cut differs", result["error"])

        broken = copy.deepcopy(self.committed)
        broken["ownLedger"][0] = base64.b64encode(b"{}").decode("ascii")
        self.assertIn("rejected", child_result(self.prepared, broken))

    def test_missing_shard_and_duplicate_shard_are_rejected(self):
        with self.assertRaisesRegex(AssertionError, "four independent shard"):
            fresh.combine([])
        with tempfile.TemporaryDirectory(prefix="p01-all44-shard-negative-") as folder:
            paths = []
            for index in range(4):
                path = Path(folder) / (str(index) + ".json")
                path.write_text(json.dumps({"shard": 0}))
                paths.append(path)
            with self.assertRaisesRegex(AssertionError, "each four-way shard"):
                fresh.combine(paths)


if __name__ == "__main__":
    unittest.main()
