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


def child_result(mode, claim):
    with tempfile.TemporaryDirectory(prefix="p01-all44-fresh-negative-") as folder:
        path = Path(folder) / (mode + ".json")
        path.write_bytes(ref.encode(claim))
        process = subprocess.run(
            [sys.executable, "-B", str(fresh.CHILD), mode, str(path)],
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

    def test_separate_cold_children_rehydrate_cuts_and_deduplicate_retry(self):
        prepared = child_result("prepared", self.prepared)
        committed = child_result("committed", self.committed)
        self.assertNotIn("rejected", prepared)
        self.assertNotIn("rejected", committed)
        self.assertEqual({"cut"}, set(prepared))
        self.assertEqual({"cut", "retry"}, set(committed))
        self.assertEqual("prepared", prepared["cut"]["status"])
        self.assertEqual("committed", committed["cut"]["status"])
        self.assertEqual(self.prepared["expectedControl"],
                         prepared["cut"]["control"])
        self.assertEqual(self.committed["expectedWorld"],
                         committed["cut"]["world"])
        self.assertIsNone(committed["retry"]["event"])
        self.assertFalse(prepared["cut"]["admitted"])
        self.assertFalse(committed["cut"]["admitted"])

    def test_retained_control_and_ancestor_ledger_mutations_fail_closed(self):
        broken = copy.deepcopy(self.prepared)
        broken["expectedControl"] = base64.b64encode(b"{}").decode("ascii")
        result = child_result("prepared", broken)
        self.assertEqual("readback", result["rejected"])
        self.assertIn("prepared retained cut differs", result["error"])

        broken = copy.deepcopy(self.prepared)
        broken["selectedLedger"][0] = base64.b64encode(b"{}").decode("ascii")
        result = child_result("prepared", broken)
        self.assertIn("rejected", result)

    def test_retained_committed_world_and_paid_input_mutations_fail_closed(self):
        broken = copy.deepcopy(self.committed)
        broken["expectedWorld"] = base64.b64encode(b"{}").decode("ascii")
        result = child_result("committed", broken)
        self.assertEqual("readback", result["rejected"])
        self.assertIn("committed retained cut differs", result["error"])

        broken = copy.deepcopy(self.committed)
        broken["ownLedger"][0] = base64.b64encode(b"{}").decode("ascii")
        self.assertIn("rejected", child_result("committed", broken))

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
