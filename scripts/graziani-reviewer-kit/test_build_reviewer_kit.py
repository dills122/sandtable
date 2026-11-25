"""Tests for the reviewer-kit builder and its tools. Synthetic inputs only; no source PDFs needed.

    python3 -B -m unittest scripts/graziani-reviewer-kit/test_build_reviewer_kit.py
"""
import hashlib
import importlib.util
import json
import subprocess
import sys
import tempfile
import unittest
import zipfile
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO = HERE.parent.parent
PLANS = REPO / "docs" / "work" / "plans"

spec = importlib.util.spec_from_file_location("build_reviewer_kit", HERE / "build_reviewer_kit.py")
builder = importlib.util.module_from_spec(spec)
spec.loader.exec_module(builder)


class KitTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.temp = tempfile.TemporaryDirectory()
        cls.out = Path(cls.temp.name) / "out"
        cls.result = builder.build(cls.out)
        cls.kit = Path(cls.result["directory"])

    @classmethod
    def tearDownClass(cls):
        cls.temp.cleanup()

    def run_tool(self, *args):
        return subprocess.run([sys.executable, "-B", *map(str, args)], cwd=self.kit, capture_output=True, text=True)

    def test_counts_match_the_seventeen_packets(self):
        self.assertEqual((self.result["packets"], self.result["rows"], self.result["fields"]), (17, 212, 666))
        index = json.loads((self.kit / "kit.json").read_text())
        self.assertEqual(len(index["sources"]), 9)

    def test_build_is_deterministic(self):
        with tempfile.TemporaryDirectory() as other:
            again = builder.build(Path(other))
        self.assertEqual(again["zip_sha256"], self.result["zip_sha256"])

    def test_manifest_checksums_verify(self):
        lines = (self.kit / "MANIFEST.sha256").read_text().splitlines()
        self.assertGreater(len(lines), 50)
        for line in lines:
            digest, path = line.split("  ", 1)
            self.assertEqual(hashlib.sha256((self.kit / path).read_bytes()).hexdigest(), digest, path)
        with zipfile.ZipFile(self.result["zip"]) as archive:
            names = {name.split("/", 1)[1] for name in archive.namelist()}
        self.assertEqual(names, {path for _, path in (l.split("  ", 1) for l in lines)} | {"MANIFEST.sha256"})

    def test_no_first_readings_or_comparisons_are_included(self):
        forbidden = {path.name for path in PLANS.glob("graziani-*-review/*.json")
                     if "template" not in path.name and "manifest" not in path.name}
        shipped = {path.name for path in self.kit.rglob("*") if path.is_file()}
        self.assertFalse(forbidden & shipped)
        first_reading_hashes = {hashlib.sha256(path.read_bytes()).hexdigest()
                                for path in PLANS.glob("graziani-*-review/*reading-[ab].json")}
        first_reading_hashes |= {hashlib.sha256(path.read_bytes()).hexdigest()
                                 for path in PLANS.glob("graziani-*-review/*reading-independent.json")}
        for path in self.kit.rglob("*"):
            if path.is_file():
                self.assertNotIn(hashlib.sha256(path.read_bytes()).hexdigest(), first_reading_hashes, path)

    def test_every_template_is_blank_and_bound_to_its_manifest(self):
        for entry in json.loads((self.kit / "kit.json").read_text())["packets"]:
            template = json.loads((self.kit / entry["template"]).read_text())
            self.assertEqual(template["reader_id"], "replace-with-reviewer-id")
            for row in template["rows"]:
                self.assertEqual(row["locator"], "UNVERIFIED")
                self.assertTrue(all(value is None for value in row["values"].values()))

    def test_notes_drop_references_to_earlier_readings(self):
        for notes in self.kit.glob("packets/*/NOTES.md"):
            text = notes.read_text()
            self.assertNotRegex(text, r"reading-(a|b|independent)\b")
            self.assertNotIn("compare_readings", text)
            self.assertNotIn("docs/work", text)

    def test_sanitizer_keeps_instructions_and_drops_leaks(self):
        text = ("# Title\n\nAfter November 16 the first entry matched. Use `not-listed` when absent.\n\n"
                "Compare with:\n\n```sh\npython3 compare_readings.py a b\n```\n\n- keep this item\n- see reading-a.json\n")
        clean, dropped = builder.sanitize_readme(text)
        self.assertIn("Use `not-listed` when absent.", clean)
        self.assertIn("keep this item", clean)
        self.assertNotIn("compare_readings", clean)
        self.assertNotIn("Compare with:", clean)
        self.assertNotIn("reading-a", clean)
        self.assertEqual(len(dropped), 4)

    def test_blank_template_is_not_ready_and_filled_reading_is(self):
        template = self.kit / "packets/supply/reading-template.json"
        blank = self.run_tool("tools/check_reading.py", "--packet", "supply", "--reading", template)
        self.assertEqual(blank.returncode, 1)
        self.assertIn("NOT READY", blank.stdout)
        reading = json.loads(template.read_text())
        reading["reader_id"] = "test-reader"
        for row in reading["rows"]:
            row["locator"] = "p. 1"
            row["values"] = {name: 1 for name in row["values"]}
        filled = Path(self.temp.name) / "filled.json"
        filled.write_text(json.dumps(reading))
        done = self.run_tool("tools/check_reading.py", "--packet", "supply", "--reading", filled)
        self.assertEqual(done.returncode, 0, done.stdout)
        self.assertIn("READY TO SEND", done.stdout)
        empty_sources = Path(self.temp.name) / "empty-sources"
        empty_sources.mkdir()
        gated = self.run_tool("tools/check_reading.py", "--packet", "supply", "--reading", filled,
                              "--sources-dir", empty_sources)
        self.assertEqual(gated.returncode, 1)
        self.assertIn("source 'scenario' not found", gated.stdout)

    def test_tampered_manifest_binding_is_rejected(self):
        reading = json.loads((self.kit / "packets/supply/reading-template.json").read_text())
        reading["reader_id"] = "test-reader"
        reading["manifest_sha256"] = "0" * 64
        path = Path(self.temp.name) / "tampered.json"
        path.write_text(json.dumps(reading))
        result = self.run_tool("tools/check_reading.py", "--packet", "supply", "--reading", path)
        self.assertIn("manifest_sha256 does not match", result.stdout)

    def test_unhashable_row_ids_are_reported_not_crashed(self):
        reading = json.loads((self.kit / "packets/supply/reading-template.json").read_text())
        reading["reader_id"] = "test-reader"
        reading["rows"][0]["id"] = ["a", "list"]
        reading["rows"][1]["id"] = {"an": "object"}
        path = Path(self.temp.name) / "badid.json"
        path.write_text(json.dumps(reading))
        result = self.run_tool("tools/check_reading.py", "--packet", "supply", "--reading", path)
        self.assertEqual(result.returncode, 1, result.stderr)
        self.assertNotIn("Traceback", result.stderr)
        self.assertIn("invalid id", result.stdout)

    def test_duplicate_json_keys_are_invalid_input(self):
        path = Path(self.temp.name) / "dup.json"
        path.write_text('{"version": 1, "version": 1}')
        result = self.run_tool("tools/check_reading.py", "--packet", "supply", "--reading", path)
        self.assertEqual(result.returncode, 2)

    def test_source_discovery_identifies_by_hash_not_name(self):
        sys.path.insert(0, str(self.kit / "tools"))
        try:
            import kit_common  # noqa: PLC0415
            folder = Path(self.temp.name) / "sources"
            folder.mkdir()
            (folder / "any-name.pdf").write_bytes(b"synthetic source")
            (folder / "other.pdf").write_bytes(b"something else")
            index = {"sources": {"scenario": {"sha256": hashlib.sha256(b"synthetic source").hexdigest()}}}
            found, unmatched = kit_common.discover_sources(folder, index)
        finally:
            sys.path.remove(str(self.kit / "tools"))
            sys.modules.pop("kit_common", None)
            sys.modules.pop("compare_readings", None)
        self.assertEqual(set(found), {"scenario"})
        self.assertEqual([Path(p).name for p in unmatched], ["other.pdf"])

    def test_packaged_sources_folder_has_no_unknown_files(self):
        result = self.run_tool("tools/verify_sources.py", "sources")
        self.assertNotIn("UNKNOWN", result.stdout)
        self.assertEqual(result.stdout.count("MISSING"), 9)

    def test_rerun_reports_missing_readings_and_never_certifies(self):
        for name in ("a", "b", "s"):
            (Path(self.temp.name) / name).mkdir(exist_ok=True)
        result = self.run_tool("tools/rerun_comparison.py", "--first-readings", Path(self.temp.name) / "a",
                               "--readings", Path(self.temp.name) / "b", "--sources-dir", Path(self.temp.name) / "s")
        self.assertEqual(result.returncode, 1)
        report = json.loads(result.stdout)
        self.assertEqual({p["status"] for p in report["packets"]}, {"not-supplied"})
        self.assertEqual(len(report["packets"]), 17)


if __name__ == "__main__":
    unittest.main()
