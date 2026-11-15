"""Research crosswalk checks reject malformed and inconsistent inputs."""
import copy
import hashlib
import tempfile
import unittest
from pathlib import Path

from verify_crosswalk import check, load_json, manifest_hash


class ArrivalCrosswalkTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.source = Path(self.temp.name) / "source"
        self.source.write_bytes(b"synthetic source")
        root = Path(__file__).parent
        self.manifest = load_json(root / "manifest.json")
        self.reading = load_json(root / "reading-a.json")
        digest = hashlib.sha256(self.source.read_bytes()).hexdigest()
        self.manifest["sources"]["commonwealth-charts"] = digest
        self.reading["sources"]["commonwealth-charts"] = digest
        self.reading["manifest_sha256"] = manifest_hash(self.manifest)

    def run_check(self):
        return check(self.manifest, self.reading, self.source)

    def test_joins_and_lists_unprinted_truck_values_separately(self):
        result = self.run_check()
        self.assertEqual((6, 8, 21), (result["schedule_rows"], result["oa_groups"], result["candidate_oa_rows"]))
        self.assertEqual({"light": 30, "medium": 105, "heavy": 5}, result["listed_truck_totals"])
        self.assertEqual({"light": 4, "medium": 4, "heavy": 5}, result["truck_rows_with_unlisted_values"])
        self.assertIn("unlisted is not zero", result["note"])

    def test_disagreeing_or_swapped_oa_timing_fails(self):
        row = next(row for row in self.reading["rows"] if row["id"] == "oa-nz-brigade")
        row["values"]["turn"], row["values"]["stage"] = 3, 6
        with self.assertRaises(ValueError):
            self.run_check()
        row["values"]["stage"] = 2
        with self.assertRaises(ValueError):
            self.run_check()

    def test_missing_duplicate_unknown_and_bool_numeric_fail(self):
        original = copy.deepcopy(self.reading)
        self.reading["rows"].pop()
        with self.assertRaises(ValueError):
            self.run_check()
        self.reading = copy.deepcopy(original)
        self.reading["rows"].append(copy.deepcopy(self.reading["rows"][0]))
        with self.assertRaises(ValueError):
            self.run_check()
        self.reading = copy.deepcopy(original)
        self.reading["rows"][0]["values"]["light"] = None
        with self.assertRaises(ValueError):
            self.run_check()
        self.reading = copy.deepcopy(original)
        self.reading["rows"][0]["values"]["turn"] = True
        with self.assertRaises(ValueError):
            self.run_check()

    def test_changed_source_manifest_and_placeholder_fail(self):
        self.source.write_bytes(b"different source")
        with self.assertRaises(ValueError):
            self.run_check()
        self.source.write_bytes(b"synthetic source")
        self.reading["manifest_sha256"] = "a" * 64
        with self.assertRaises(ValueError):
            self.run_check()
        self.reading["manifest_sha256"] = manifest_hash(self.manifest)
        self.reading["reader_id"] = "replace-with-reviewer-id"
        with self.assertRaises(ValueError):
            self.run_check()


if __name__ == "__main__":
    unittest.main()
