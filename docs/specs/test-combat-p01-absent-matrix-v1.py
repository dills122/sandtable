#!/usr/bin/env python3
"""Fail-sensitive tests for the physical P01 absent issuance boundary."""

import copy
import importlib.util
import json
import subprocess
import unittest
from pathlib import Path
from unittest import mock

ROOT = Path(__file__).resolve().parent


def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, ROOT / filename)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


reference = load("absent_test_reference", "verify-combat-p01-absent-matrix-v1.py")
fixture = load("absent_test_root", "test-combat-actual-moved-repeat-position-v1.py")


def record_for(index):
    source, parents, own = fixture.terminal(index)
    _, _, state = reference.rba.p01.r03.replay(source, parents, own)
    source_value = reference.rba.p01.r03.parse(source, "Source")
    event = source_value["breakdownEventCanonicalUtf8"][-1]
    control = reference.rba.p01.r03.raw(state, "Control")
    return dict(rowKey=f"generated-{index}", sourceHash=reference.digest(source),
                eventHash=reference.digest(event.encode("ascii")),
                receiptId=json.loads(event)["receiptId"],
                afterControlHash=reference.digest(control),
                vectors=dict(sourceCanonicalUtf8=source.decode("ascii"),
                             parentInputLedgersCanonicalUtf8=[
                                 parents[0], *[[item.decode("ascii") for item in ledger]
                                              for ledger in parents[1:]]],
                             inputCanonicalUtf8=own[0].decode("ascii"),
                             eventCanonicalUtf8=event,
                             afterControlCanonicalUtf8=control.decode("ascii")))


class AbsentMatrixTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.paid_adjacent = record_for(0)
        cls.nonadjacent_axis = record_for(1)
        cls.nonadjacent_commonwealth = record_for(169)
        cls.selected_axis = record_for(36)
        cls.selected_commonwealth = record_for(168)

    def test_paid_adjacent_issues_neither_selected_nor_explicit_rba(self):
        output = reference.classify_record(self.paid_adjacent, "provisional-empty")
        self.assertEqual("paid-adjacent-no-issuance", output["issuance"])
        self.assertEqual("no-loaded-acting-infantry", output["assaultShape"])

    def test_nonadjacent_one_event_replays_with_rba_choice_still_open(self):
        for record in (self.nonadjacent_axis, self.nonadjacent_commonwealth):
            with self.subTest(actor=record["rowKey"]):
                output = reference.classify_record(record, "provisional-empty")
                self.assertEqual("zero-assault-only-rba-choice-open", output["issuance"])
                self.assertTrue(output["privateReceiptId"].startswith("p01s."))
                self.assertGreater(output["privateSourceBytes"], 1000)
                self.assertEqual(record["sourceHash"], output["originalSourceHash"])

    def test_selected_positive_control_for_both_actors(self):
        actors = set()
        for record in (self.selected_axis, self.selected_commonwealth):
            output = reference.classify_record(record, "provisional-positive")
            self.assertEqual("selected-positive-control", output["issuance"])
            self.assertTrue(output["selectedCandidateId"].startswith("p01a."))
            actors.add(output["actorSide"])
        self.assertEqual({"axis", "commonwealth"}, actors)

    def test_original_source_event_receipt_and_input_mutations_fail(self):
        record = self.nonadjacent_axis
        for label, mutate in (
            ("source", lambda r: r["vectors"].__setitem__(
                "sourceCanonicalUtf8", r["vectors"]["sourceCanonicalUtf8"] + " ")),
            ("event", lambda r: r["vectors"].__setitem__(
                "eventCanonicalUtf8", r["vectors"]["eventCanonicalUtf8"] + " ")),
            ("receipt", lambda r: r.__setitem__("receiptId", "mrbc." + "0" * 64)),
            ("input omission", lambda r: r["vectors"].__setitem__(
                "inputCanonicalUtf8", "")),
            ("parent omission", lambda r: r["vectors"][
                "parentInputLedgersCanonicalUtf8"].__setitem__(1, [])),
        ):
            with self.subTest(label=label):
                changed = copy.deepcopy(record)
                mutate(changed)
                with self.assertRaises((ValueError, KeyError, IndexError)):
                    reference.classify_record(changed, "provisional-empty")

    def test_forged_row_label_and_missing_chunk_fail(self):
        names = {f"rows-{i:03}-{i+3:03}.jsonl.gz" for i in range(0, 33, 3)}
        chunks = [{"name": name} for name in sorted(names)]
        summary = {"count": 300, "rows": [{"rowKey": "r000-c01",
                                           "classification": "unresolved"}] * 300,
                   "chunkSha256": {name: "pin" for name in names}}
        partition = {"rows": [{"rowKey": "r000-c01"}] * 300}
        reference.check_catalogue(chunks, summary, partition, names)
        with self.assertRaisesRegex(ValueError, "complete physical catalogue"):
            reference.check_catalogue(chunks[:-1], summary, partition, names)
        row = {"index": 0, "classification": "unresolved",
               "r03": {"rowKey": "r000-c01"}}
        reference.check_row_identity(0, row, summary, partition, set())
        row["r03"]["rowKey"] = "forged"
        with self.assertRaisesRegex(ValueError, "ordered row identity"):
            reference.check_row_identity(0, row, summary, partition, set())

    def test_unexpected_issuance_and_wrong_partition_fail(self):
        with self.assertRaisesRegex(ValueError, "authenticated partition"):
            reference.classify_record(self.nonadjacent_axis, "provisional-positive")
        with mock.patch.object(reference.selected, "candidate_from_replayed",
                               return_value=({}, "p01a." + "0" * 64)):
            with self.assertRaisesRegex(ValueError, "positive selected control"):
                reference.classify_record(self.nonadjacent_axis, "provisional-empty")

    def test_regenerated_original_source_creation_ledgers_and_input_pins_fail(self):
        root = fixture.terminal(0)
        claim = reference.retained_claims()["rows"][0]
        reference.check_generated_input_pins(0, root, claim)
        for label, mutate in (
            ("source", lambda c: c.__setitem__("sourceHash", "sha256:" + "0" * 64)),
            ("CreationRequest", lambda c: c.__setitem__(
                "creationRequestHash", "sha256:" + "0" * 64)),
            ("parent ledger", lambda c: c["parentLedgerHashes"].__setitem__(
                0, "sha256:" + "0" * 64)),
            ("R03 input", lambda c: c.__setitem__(
                "r03InputHash", "sha256:" + "0" * 64)),
        ):
            with self.subTest(label=label):
                broken = copy.deepcopy(claim)
                mutate(broken)
                with self.assertRaisesRegex(ValueError, "regenerated original"):
                    reference.check_generated_input_pins(0, root, broken)
        with self.assertRaisesRegex(ValueError, "four shard artifacts"):
            reference.combine_shards([], tested_sha=subprocess.check_output(
                ["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip())


if __name__ == "__main__":
    unittest.main()
