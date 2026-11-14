"""Bounded Axis formation join must fail on drift and conflicting relationships."""
import copy
import hashlib
import tempfile
import unittest
from pathlib import Path

from verify_relationships import check, load_json, manifest_hash


class FormationJoinTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.scenario = Path(self.temp.name) / "scenario"
        self.errata = Path(self.temp.name) / "errata"
        self.scenario.write_bytes(b"scenario")
        self.errata.write_bytes(b"errata")
        root = Path(__file__).parent
        self.member_manifest = load_json(root / "manifest.json")
        self.member_reading = load_json(root / "reading-a.json")
        self.relation_manifest = load_json(root / "relationship-manifest.json")
        self.relation_reading = load_json(root / "relationship-reading-a.json")
        hashes = {"scenario": hashlib.sha256(self.scenario.read_bytes()).hexdigest(),
                  "errata": hashlib.sha256(self.errata.read_bytes()).hexdigest()}
        for manifest, reading in ((self.member_manifest, self.member_reading),
                                  (self.relation_manifest, self.relation_reading)):
            manifest["sources"] = {key: hashes[key] for key in manifest["sources"]}
            reading["sources"] = dict(manifest["sources"])
            reading["manifest_sha256"] = manifest_hash(manifest)

    def run_check(self):
        return check(self.member_manifest, self.member_reading, self.relation_manifest,
                     self.relation_reading, self.scenario, self.errata)

    def test_two_tank_joins_and_29_candidate_members(self):
        result = self.run_check()
        self.assertEqual(29, result["candidate_members"])
        self.assertEqual(2, result["scenario_oa_tank_joins"])
        self.assertIn("source truth", result["note"])
        self.assertEqual(hashlib.sha256(b"scenario").hexdigest(), result["source_sha256"]["scenario"])

    def test_changed_source_and_missing_or_duplicate_members_fail(self):
        self.errata.write_bytes(b"changed")
        with self.assertRaises(ValueError):
            self.run_check()
        self.errata.write_bytes(b"errata")
        original = copy.deepcopy(self.member_reading)
        self.member_reading["rows"].pop()
        with self.assertRaises(ValueError):
            self.run_check()
        self.member_reading = original
        self.member_reading["rows"].append(copy.deepcopy(self.member_reading["rows"][0]))
        with self.assertRaises(ValueError):
            self.run_check()

    def test_tank_identity_or_attachment_disagreement_fails(self):
        row = next(row for row in self.relation_reading["rows"] if row["id"] == "cirene-tank-oa")
        row["values"]["initial_attachment"] = "62 Marmarica"
        with self.assertRaises(ValueError):
            self.run_check()
        row["values"]["initial_attachment"] = "63 Cirene"
        row["values"]["counter_abbreviation"] = "LXII(L)"
        with self.assertRaises(ValueError):
            self.run_check()

    def test_unverified_or_duplicate_candidate_identity_fails(self):
        first, second = self.member_reading["rows"][:2]
        first["locator"] = "UNVERIFIED"
        with self.assertRaises(ValueError):
            self.run_check()
        first["locator"] = "verified row"
        second["values"]["counter_abbreviation"] = first["values"]["counter_abbreviation"]
        second["values"]["id_code"] = first["values"]["id_code"]
        with self.assertRaises(ValueError):
            self.run_check()


if __name__ == "__main__":
    unittest.main()
