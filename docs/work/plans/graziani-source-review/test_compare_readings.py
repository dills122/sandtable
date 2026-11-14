"""Failure-sensitive tests for research reading comparison."""

import copy
import hashlib
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

from compare_readings import ReviewInputError, compare, load_json, manifest_hash


class ComparisonTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.path = Path(self.temp.name) / "source.bin"
        self.path.write_bytes(b"source bytes")
        self.digest = hashlib.sha256(self.path.read_bytes()).hexdigest()
        self.manifest = {"version": 1, "scope": "sample", "sources": {"s": self.digest},
                         "rows": [{"id": "r", "source": "s", "fields": ["turn", "stage"]}]}
        self.a = {"version": 1, "scope": "sample", "reader_id": "alice",
                  "manifest_sha256": manifest_hash(self.manifest),
                  "sources": {"s": self.digest},
                  "rows": [{"id": "r", "locator": "p.1", "values": {"turn": 6, "stage": 3}}]}
        self.b = copy.deepcopy(self.a)
        self.b["reader_id"] = "bob"

    def run_compare(self):
        return compare(self.manifest, [self.a, self.b], {"s": self.path})["issues"]

    def test_matching_source_fields_have_no_issues(self):
        self.assertEqual([], self.run_compare())

    def test_same_reader_case_insensitively_rejected(self):
        self.b["reader_id"] = "ALICE"
        with self.assertRaises(ReviewInputError):
            self.run_compare()

    def test_real_source_bytes_must_match_digest(self):
        self.path.write_bytes(b"changed")
        with self.assertRaises(ReviewInputError):
            self.run_compare()

    def test_reader_must_attest_exact_source_and_scope(self):
        self.b["sources"]["s"] = "a" * 64
        with self.assertRaises(ReviewInputError):
            self.run_compare()
        self.b["sources"]["s"] = self.digest
        self.b["scope"] = "other"
        with self.assertRaises(ReviewInputError):
            self.run_compare()

    def test_conflicting_values_reported_without_coercion(self):
        self.a["rows"][0]["values"]["stage"] = 1
        self.b["rows"][0]["values"]["stage"] = True
        issues = self.run_compare()
        self.assertEqual(["disagreement"], [item["kind"] for item in issues])
        self.assertEqual([1, True], issues[0]["values"])

    def test_both_unknown_never_claim_agreement(self):
        self.a["rows"][0]["values"]["turn"] = None
        self.b["rows"][0]["values"]["turn"] = None
        self.assertEqual(["unknown", "unknown"], [item["kind"] for item in self.run_compare()])

    def test_unknown_and_zero_are_distinct(self):
        self.a["rows"][0]["values"]["turn"] = None
        self.b["rows"][0]["values"]["turn"] = 0
        self.assertEqual(["unknown"], [item["kind"] for item in self.run_compare()])

    def test_missing_row_and_field_and_extra_are_reported(self):
        del self.a["rows"][0]["values"]["stage"]
        self.a["rows"][0]["values"]["unexpected"] = 2
        self.b["rows"] = []
        self.assertEqual({"missing-row", "missing-field", "unexpected-field"},
                         {item["kind"] for item in self.run_compare()})

    def test_duplicate_row_rejected(self):
        self.a["rows"].append(copy.deepcopy(self.a["rows"][0]))
        with self.assertRaises(ReviewInputError):
            self.run_compare()

    def test_missing_locator_rejected(self):
        self.a["rows"][0]["locator"] = ""
        with self.assertRaises(ReviewInputError):
            self.run_compare()

    def test_different_source_locators_are_visible_and_require_review(self):
        self.b["rows"][0]["locator"] = "p.2"
        result = compare(self.manifest, [self.a, self.b], {"s": self.path})
        self.assertEqual("locator-difference", result["issues"][0]["kind"])
        self.assertEqual(["p.1", "p.2"], result["evidence"][0]["locators"])

    def test_unmodified_template_placeholders_never_pass(self):
        self.b["reader_id"] = "replace-with-reviewer-id"
        self.a["rows"][0]["locator"] = "UNVERIFIED"
        self.b["rows"][0]["locator"] = "UNVERIFIED"
        self.assertEqual({"unverified-reader", "unverified-locator"},
                         {item["kind"] for item in self.run_compare()})

    def test_omitted_source_file_rejected(self):
        with self.assertRaises(ReviewInputError):
            compare(self.manifest, [self.a, self.b], {})

    def test_manifest_duplicate_fields_rejected(self):
        self.manifest["rows"][0]["fields"] = ["turn", "turn"]
        with self.assertRaises(ReviewInputError):
            self.run_compare()

    def test_manifest_change_cannot_silently_shrink_scope(self):
        self.manifest["rows"][0]["fields"] = ["turn"]
        with self.assertRaisesRegex(ReviewInputError, "manifest hash"):
            self.run_compare()

    def test_duplicate_json_key_rejected(self):
        self.path.write_text('{"stage": 2, "stage": 3}')
        with self.assertRaisesRegex(ReviewInputError, "duplicate JSON key"):
            load_json(self.path)

    def test_nonstandard_nonfinite_json_rejected(self):
        self.path.write_text('{"stage": NaN}')
        with self.assertRaisesRegex(ReviewInputError, "non-finite"):
            load_json(self.path)

    def test_cli_success_disagreement_and_bad_source_exit_codes(self):
        root = Path(self.temp.name)
        script = Path(__file__).with_name("compare_readings.py")
        manifest, a, b = [root / name for name in ("manifest.json", "a.json", "b.json")]
        manifest.write_text(json.dumps(self.manifest))
        a.write_text(json.dumps(self.a))
        b.write_text(json.dumps(self.b))
        cmd = [sys.executable, "-B", str(script), str(manifest), str(a), str(b), "--source", f"s={self.path}"]
        self.assertEqual(0, subprocess.run(cmd, capture_output=True, check=False).returncode)
        self.b["rows"][0]["values"]["stage"] = 2
        b.write_text(json.dumps(self.b))
        self.assertEqual(1, subprocess.run(cmd, capture_output=True, check=False).returncode)
        self.path.write_bytes(b"changed")
        self.assertEqual(2, subprocess.run(cmd, capture_output=True, check=False).returncode)


if __name__ == "__main__":
    unittest.main()
