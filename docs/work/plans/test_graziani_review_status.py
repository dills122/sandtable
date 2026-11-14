"""Status summary retains comparator issues and does not certify independence."""
import copy
import hashlib
import importlib.util
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

path = Path(__file__).with_name("graziani-review-status.py")
spec = importlib.util.spec_from_file_location("review_status", path)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class ReviewStatusTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.source = Path(self.temp.name) / "source"
        self.source.write_bytes(b"synthetic")
        digest = hashlib.sha256(self.source.read_bytes()).hexdigest()
        self.manifest = {"version": 1, "scope": "example", "sources": {"source": digest},
                         "rows": [{"id": "row", "source": "source", "fields": ["number"]}]}
        self.a = {"version": 1, "scope": "example", "reader_id": "first",
                  "manifest_sha256": module.manifest_hash(self.manifest), "sources": {"source": digest},
                  "rows": [{"id": "row", "locator": "p.1", "values": {"number": 7}}]}
        self.b = copy.deepcopy(self.a)
        self.b["reader_id"] = "second"

    def test_differing_locators_remain_raw_issues_even_when_values_match(self):
        self.b["rows"][0]["locator"] = "page 1"
        result = module.summarize("sample", self.manifest, [self.a, self.b], {"source": self.source}, False)
        self.assertEqual("open", result["raw_comparison"])
        self.assertEqual({"locator-difference": 1}, result["raw_issue_counts"])
        self.assertEqual("attributed-reading", result["second_entry"])
        self.assertEqual(1, result["expected_fields"])

    def test_blank_template_cannot_look_complete(self):
        self.b["reader_id"] = "replace-with-reviewer-id"
        self.b["rows"][0]["locator"] = "UNVERIFIED"
        self.b["rows"][0]["values"]["number"] = None
        result = module.summarize("sample", self.manifest, [self.a, self.b], {"source": self.source}, True)
        self.assertEqual("template", result["second_entry"])
        self.assertEqual(1, result["raw_issue_counts"]["unknown"])
        self.assertEqual(1, result["raw_issue_counts"]["unverified-reader"])
        self.assertEqual("open", result["raw_comparison"])

    def test_source_or_reader_identity_errors_fail(self):
        self.source.write_bytes(b"changed")
        with self.assertRaises(module.ReviewInputError):
            module.summarize("sample", self.manifest, [self.a, self.b], {"source": self.source}, False)
        self.source.write_bytes(b"synthetic")
        self.b["reader_id"] = self.a["reader_id"]
        with self.assertRaises(module.ReviewInputError):
            module.summarize("sample", self.manifest, [self.a, self.b], {"source": self.source}, False)

    def test_duplicate_or_missing_assignment_rejects(self):
        with self.assertRaises(module.ReviewInputError):
            module.assignments(["a=/tmp/one", "a=/tmp/two"], "source")
        with self.assertRaises(module.ReviewInputError):
            module.assignments(["a="], "source")

    def test_registered_packets_bind_complete_envelopes_without_requiring_source_scans(self):
        # Hash verification is exercised separately; this checks repository packet
        # wiring even on machines that do not have the third-party scans.
        source_hashes = {}
        scopes = set()
        for name, paths in module.PACKETS.items():
            with self.subTest(packet=name):
                manifest = module.load_json(module.PLAN / paths[0])
                readings = [module.load_json(module.PLAN / path) for path in paths[1:]]
                self.assertNotIn(manifest["scope"], scopes)
                scopes.add(manifest["scope"])
                for source_id, digest in manifest["sources"].items():
                    self.assertEqual(source_hashes.setdefault(source_id, digest), digest)
                # Use the digest as a fake path and return it as the fake hash.
                # This is not evidence that any real source bytes were checked.
                with patch.dict(module.compare.__globals__, {"source_hash": lambda value: str(value)}):
                    report = module.compare(manifest, readings, manifest["sources"])
                structural_issues = {"unexpected-row", "missing-row", "unexpected-field", "missing-field"}
                self.assertFalse([issue for issue in report["issues"] if issue["kind"] in structural_issues])
                if readings[1]["reader_id"] == "replace-with-reviewer-id":
                    self.assertTrue(all(row["locator"] == "UNVERIFIED" and
                                        all(value is None for value in row["values"].values())
                                        for row in readings[1]["rows"]))


if __name__ == "__main__":
    unittest.main()
