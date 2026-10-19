#!/usr/bin/env python3
"""Exact, CI-local all-44 private paid prepared/committed cut witness."""
import importlib.util
import json
import tempfile
import unittest
from pathlib import Path
from unittest import mock

ROOT = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location(
    "paid_all44_cuts", ROOT / "verify-combat-p01-paid-all44-cuts-v2.py")
cuts = importlib.util.module_from_spec(spec)
spec.loader.exec_module(cuts)


class PaidAll44CutTests(unittest.TestCase):
    def test_retained_manifest_and_partition_pins_fail_closed(self):
        self.assertEqual({
            "manifestSha256": cuts.ref.result.PINS["manifest.json"],
            "boundedProfileAuditSha256":
                cuts.ref.result.PINS["bounded-profile-audit.json"],
        }, cuts.check_retained_pins())
        for field in cuts.RETAINED_FILES:
            with self.subTest(field=field), mock.patch.dict(
                    cuts.FIXTURE["retainedPins"], {field: "0" * 64}):
                with self.assertRaisesRegex(AssertionError, "retained fixture pins"):
                    cuts.check_retained_pins()
        with tempfile.TemporaryDirectory(prefix="p01-retained-pins-") as directory:
            evidence = Path(directory)
            (evidence / "manifest.json").write_bytes(b"changed manifest")
            (evidence / "bounded-profile-audit.json").write_bytes(b"changed partition")
            with mock.patch.object(cuts.ref, "audit",
                                   side_effect=AssertionError("audit reached")):
                with self.assertRaisesRegex(AssertionError, "manifest.json"):
                    cuts.audit_retained(evidence)
                trusted_manifest = cuts.FIXTURE["retainedPins"]["manifestSha256"]
                with mock.patch.object(cuts, "digest", side_effect=[
                        "sha256:" + trusted_manifest, "sha256:" + "0" * 64]):
                    with self.assertRaisesRegex(AssertionError,
                                                "bounded-profile-audit.json"):
                        cuts.audit_retained(evidence)

    def test_ordered_prepared_and_committed_cuts_match_pinned_output(self):
        result = cuts.verify()
        fixture = cuts.FIXTURE
        self.assertEqual(44, result["preparedCuts"])
        self.assertEqual(44, result["committedCuts"])
        self.assertEqual(256, result["candidateAbsent"])
        self.assertEqual(0, result["admittedRows"])
        self.assertEqual(0, result["publicActions"])
        self.assertEqual({"axis": 22, "commonwealth": 22}, result["orientations"])
        self.assertEqual(fixture["orderedOutputHash"], result["orderedOutputHash"])
        self.assertEqual(fixture["orderedOutputBytes"], result["orderedOutputBytes"])
        self.assertEqual(fixture["maxima"], result["maxima"])
        self.assertEqual([root["index"] for root in fixture["roots"]],
                         [row["index"] for row in result["rows"]])
        self.assertEqual(44, len({row["committedReceiptId"]
                                  for row in result["rows"]}))
        self.assertEqual(44, len({row["committedControlHash"]
                                  for row in result["rows"]}))


if __name__ == "__main__":
    unittest.main()
