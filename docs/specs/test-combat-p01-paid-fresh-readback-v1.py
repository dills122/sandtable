#!/usr/bin/env python3
"""Authentic prepared/committed paid cuts across a fresh Python process."""
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
READBACK = ROOT / "combat-p01-paid-commit-fresh-readback.py"
FIXTURE = json.loads((ROOT / "fixtures/combat-p01-paid-fresh-readback-v1.json").read_text())


def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, ROOT / filename)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


paid = load("fresh_paid_test", "test-combat-p01-paid-commit-v1.py")
ref = paid.ref


def b64(value):
    return base64.b64encode(value).decode("ascii")


def payload(root, selected, assignment, source, own, state, retry=None):
    return dict(source=b64(source), creationRequest=b64(ref.encode(root[1][0])),
                parentInputLedgers=[[b64(item) for item in ledger]
                                    for ledger in root[1][1:]],
                r03Ledger=[b64(item) for item in root[2]],
                selectedLedger=[b64(item) for item in selected],
                assignmentLedger=[b64(item) for item in assignment],
                ownLedger=[b64(item) for item in own],
                retryInput=None if retry is None else b64(retry),
                expectedControl=b64(ref.control_bytes(state)),
                expectedWorld=b64(ref.encode(state["world"])),
                expectedReceiptId=state["lastReceiptId"])


def fresh(value):
    with tempfile.TemporaryDirectory(prefix="p01-paid-readback-") as directory:
        cut = Path(directory) / "retained-cut.json"
        cut.write_bytes(json.dumps(value, separators=(",", ":")).encode("ascii"))
        process = subprocess.run([sys.executable, "-B", str(READBACK), str(cut)],
                                 stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                                 check=False, timeout=300)
    if process.returncode:
        raise AssertionError(f"fresh process exited {process.returncode}: "
                             f"{process.stderr.decode('utf-8', 'replace')}")
    return json.loads(process.stdout)


class PaidFreshReadbackTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.paths = {}
        for case in FIXTURE["authenticRoots"]:
            root = paid.fixture.fixture.terminal(case["index"])
            selected_source, selected_ledger = paid.fixture.selected_path(root)
            assignment_source, assignment_ledger = paid.prepared(
                root, selected_source, selected_ledger,
                ("attacker", "defender"))
            source = ref.source_for(assignment_source)
            before = ref.replay(source, root[1], root[2], selected_ledger,
                                assignment_ledger, [])
            command = ref.input_bytes(ref.expected_input(before))
            after, event, receipt = ref.transition(before, ref.parse_input(command))
            packet = ref.parse_source(source)
            packet["eventCanonicalUtf8"].append(event.decode("ascii"))
            committed_source = ref.encode(packet)
            cls.paths[case["actingSide"]] = dict(
                root=root, selected=selected_ledger, assignment=assignment_ledger,
                before=payload(root, selected_ledger, assignment_ledger,
                               source, [], before),
                after=payload(root, selected_ledger, assignment_ledger,
                              committed_source, [command], after, retry=command),
                command=command, receipt=receipt, source=source)

    def test_both_orientations_prepared_and_committed_cuts_are_byte_identical(self):
        for side, path in self.paths.items():
            for label in ("before", "after"):
                with self.subTest(side=side, cut=label):
                    claim = path[label]
                    observed = fresh(claim)
                    self.assertNotIn("rejected", observed)
                    self.assertEqual(claim["expectedControl"], observed["control"])
                    self.assertEqual(claim["expectedWorld"], observed["world"])
                    self.assertEqual(claim["expectedReceiptId"], observed["receiptId"])
                    self.assertFalse(observed["admitted"])
                    self.assertEqual([], observed["publicActions"])
                    if label == "before":
                        self.assertEqual("prepared", observed["status"])
                        self.assertEqual([], observed["attackHistory"])
                        self.assertEqual([], observed["targetUses"])
                    else:
                        self.assertEqual("committed", observed["status"])
                        self.assertEqual(1, len(observed["attackHistory"]))
                        self.assertEqual(1, len(observed["targetUses"]))
                        self.assertEqual(FIXTURE["expectedPaidAmmo"],
                                         [x["ammunition"] for x in observed["resources"]])
                        self.assertIsNone(observed["retry"]["event"])
                        self.assertEqual(path["receipt"], observed["retry"]["receiptId"])
                        self.assertEqual(claim["source"], observed["retry"]["source"])
                        self.assertEqual(claim["ownLedger"], observed["retry"]["ledger"])

    def test_parent_source_ledger_and_conflicting_retry_fail_closed(self):
        path = self.paths["axis"]
        variants = []
        forged_parent = copy.deepcopy(path["after"])
        forged_parent["parentInputLedgers"][0][0] = b64(b"{}")
        variants.append(("parent", forged_parent))
        forged_source = copy.deepcopy(path["after"])
        source = base64.b64decode(forged_source["source"])
        packet = ref.parse_source(source)
        self.assertIn("combat-force-assignment-opened",
                      packet["forceAssignmentSourceCanonicalUtf8"])
        packet["forceAssignmentSourceCanonicalUtf8"] = (
            packet["forceAssignmentSourceCanonicalUtf8"].replace(
                "combat-force-assignment-opened", "combat-force-assignment-forged"))
        forged_source["source"] = b64(ref.encode(packet))
        variants.append(("source", forged_source))
        forged_ledger = copy.deepcopy(path["after"])
        forged_ledger["ownLedger"] = [b64(b"{}")]
        variants.append(("ledger", forged_ledger))
        for label, claim in variants:
            with self.subTest(label=label):
                self.assertIn(fresh(claim)["rejected"], ("preflight", "replay"))
        conflict = copy.deepcopy(path["after"])
        command = ref.parse_input(path["command"])
        command["expectedPriorVersion"] += 1
        conflict["retryInput"] = b64(ref.encode(command))
        self.assertEqual("retry", fresh(conflict)["rejected"])

    def test_four_byte_caps_fail_before_parent_replay(self):
        path = self.paths["axis"]
        limits = ref.SCHEMA["limits"]
        for field, maximum in (("source", limits["sourceBytes"]),
                               ("expectedControl", limits["controlBytes"]),
                               ("expectedWorld", limits["projectedWorldBytes"])):
            with self.subTest(field=field):
                claim = copy.deepcopy(path["after"])
                claim[field] = b64(b"x" * (maximum + 1))
                self.assertEqual("preflight", fresh(claim)["rejected"])
        claim = copy.deepcopy(path["after"])
        source = ref.parse_source(base64.b64decode(claim["source"]))
        source["eventCanonicalUtf8"] = ["x" * (limits["eventBytes"] + 1)]
        claim["source"] = b64(ref.encode(source))
        self.assertEqual("preflight", fresh(claim)["rejected"])


if __name__ == "__main__":
    unittest.main()
