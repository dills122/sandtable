"""Synthetic, isolated tests for the source-derived guard."""
import hashlib
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
import zipfile

GUARD = Path(__file__).with_name("source_derived_guard.py")


class GuardTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        subprocess.run(["git", "init", "-q", self.root], check=True)
        self.manifest = self.root / "allow.json"
        self.manifest.write_text("{}")

    def put(self, path, data, track=True):
        file = self.root / path
        file.parent.mkdir(parents=True, exist_ok=True)
        file.write_bytes(data if isinstance(data, bytes) else data.encode())
        if track:
            self.track(path)

    def track(self, path):
        subprocess.run(["git", "-C", self.root, "add", "--", path], check=True)

    def allow(self, *paths):
        self.manifest.write_text(json.dumps({
            path: {"reason": "existing reference",
                   "sha256": hashlib.sha256((self.root / path).read_bytes()).hexdigest()}
            for path in paths
        }))

    def run_guard(self, *args):
        result = subprocess.run(
            [sys.executable, GUARD, "--manifest", self.manifest, *args],
            cwd=self.root, capture_output=True, text=True,
        )
        return result, json.loads(result.stdout)

    def test_markers_and_exact_allowlist(self):
        self.put("allowed.cs", "ContentOriginKind.SourceDerived")
        self.put("new.cs", 'spi-1979-fictitious "oa-fake" Cna1979GrazianiFake HistoricalMap')
        self.put("untracked.cs", "ContentOriginKind.SourceDerived", track=False)
        self.allow("allowed.cs")
        result, report = self.run_guard()
        self.assertEqual(result.returncode, 0)
        self.assertEqual(report["allowed"], 1)
        self.assertEqual(report["unexpected"], 1)
        self.assertEqual([row["path"] for row in report["hits"]], ["allowed.cs", "new.cs"])
        self.assertNotIn("fictitious", result.stdout)
        self.assertNotIn("existing reference", result.stdout)
        strict, _ = self.run_guard("--strict")
        self.assertEqual(strict.returncode, 1)

    def test_shapes_without_markers(self):
        self.put("grid.md", "\n".join(" | ".join(str(i * 7 + j) for j in range(8)) for i in range(6)))
        self.put("array.cs", "int[] x = [" + ",".join(map(str, range(50))) + "];")
        self.put("big.json", json.dumps([{"unitId": "invented", "note": "x" * 200} for _ in range(100)]))
        with zipfile.ZipFile(self.root / "bundle.zip", "w") as archive:
            archive.writestr("fake.json", '{"hexId":"invented"}')
        self.track("bundle.zip")
        self.put("plain.txt", "original prose 1 2 3")
        result, report = self.run_guard()
        self.assertEqual(result.returncode, 0)
        self.assertEqual({row["path"] for row in report["hits"]},
                         {"grid.md", "array.cs", "big.json", "bundle.zip"})
        self.assertNotIn("invented", result.stdout)

    def test_deterministic_and_stale_manifest(self):
        self.put("z.cs", "HistoricalMap")
        self.put("a.cs", "HistoricalMap")
        self.allow("z.cs", "a.cs")
        first, report = self.run_guard("--strict")
        second, _ = self.run_guard("--strict")
        self.assertEqual(first.returncode, 0)
        self.assertEqual(first.stdout, second.stdout)
        self.assertEqual(report["unexpected"], 0)
        self.manifest.write_text(json.dumps({"missing.cs": {"reason": "legacy", "sha256": "0" * 64}}))
        strict, report = self.run_guard("--strict")
        self.assertEqual(strict.returncode, 1)
        self.assertEqual(report["stale_manifest_paths"], ["missing.cs"])

    def test_changed_allowlisted_file_is_unexpected(self):
        self.put("existing.cs", "HistoricalMap")
        self.allow("existing.cs")
        self.put("existing.cs", "HistoricalMap // new reference")
        result, report = self.run_guard("--strict")
        self.assertEqual(result.returncode, 1)
        self.assertEqual(report["hits"][0]["status"], "UNEXPECTED")
        self.assertEqual(report["hits"][0]["counts"]["baseline_changed"], 1)

    def test_opaque_or_large_files_are_review_candidates(self):
        self.put("unknown.bin", b"\xff\xfe")
        self.put("huge.txt", "z" * (2 * 1024 * 1024 + 1))
        result, report = self.run_guard()
        self.assertEqual(result.returncode, 0)
        self.assertEqual(report["unexpected"], 2)
        self.assertEqual(report["hits"][0]["counts"], {"large_file": 1})
        self.assertEqual(report["hits"][1]["counts"], {"opaque_binary": 1})


if __name__ == "__main__":
    unittest.main()
