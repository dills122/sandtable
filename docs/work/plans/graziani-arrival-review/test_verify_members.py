"""Mutation checks for the bounded OA-member research join."""
import copy
import hashlib
import tempfile
import unittest
from pathlib import Path

from verify_members import check_members, load_json, manifest_hash


class ArrivalMemberTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.source = Path(self.temp.name) / "source"
        self.source.write_bytes(b"synthetic charts")
        root = Path(__file__).parent
        self.group_manifest = load_json(root / "manifest.json")
        self.group_reading = load_json(root / "reading-a.json")
        self.member_manifest = load_json(root / "member-manifest.json")
        self.member_reading = load_json(root / "member-reading-a.json")
        digest = hashlib.sha256(self.source.read_bytes()).hexdigest()
        for manifest, reading in ((self.group_manifest, self.group_reading),
                                  (self.member_manifest, self.member_reading)):
            manifest["sources"]["commonwealth-charts"] = digest
            reading["sources"]["commonwealth-charts"] = digest
            reading["manifest_sha256"] = manifest_hash(manifest)

    def run_check(self):
        return check_members(self.group_manifest, self.group_reading, self.member_manifest,
                             self.member_reading, self.source)

    def test_bounded_join_reports_21_candidates_without_certifying_them(self):
        result = self.run_check()
        self.assertEqual(8, result["groups"])
        self.assertEqual(21, result["candidate_members"])
        self.assertIn("does not verify", result["note"])
        self.member_reading["rows"][0]["values"]["toe_mark"] = "changed-mark"
        self.assertNotEqual(result["member_reading_sha256"], self.run_check()["member_reading_sha256"])

    def test_swapped_arrival_or_missing_member_fails(self):
        nz = next(row for row in self.member_reading["rows"] if row["id"] == "24-nz")
        nz["values"]["arrival_mark"] = "6/3"
        with self.assertRaises(ValueError):
            self.run_check()
        nz["values"]["arrival_mark"] = "3/6"
        self.member_reading["rows"].pop()
        with self.assertRaises(ValueError):
            self.run_check()

    def test_duplicate_or_unknown_field_fails(self):
        self.member_reading["rows"].append(copy.deepcopy(self.member_reading["rows"][0]))
        with self.assertRaises(ValueError):
            self.run_check()
        self.member_reading["rows"].pop()
        self.member_reading["rows"][0]["values"]["toe_mark"] = None
        with self.assertRaises(ValueError):
            self.run_check()

    def test_changed_source_or_group_count_fails(self):
        self.source.write_bytes(b"changed")
        with self.assertRaises(ValueError):
            self.run_check()
        self.source.write_bytes(b"synthetic charts")
        group = next(row for row in self.group_reading["rows"] if row["id"] == "oa-polish")
        group["values"]["listed_rows"] = 7
        with self.assertRaises(ValueError):
            self.run_check()


if __name__ == "__main__":
    unittest.main()
