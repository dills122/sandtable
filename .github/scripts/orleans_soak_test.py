"""Check soak diagnostics without running the expensive .NET test."""
import sqlite3
from pathlib import Path
import tempfile
import unittest
from orleans_soak import SOAK_METHOD, progress, verify_soak
from orleans_partition_test import rows_for, xml


class SoakTests(unittest.TestCase):
    def test_progress_reports_only_count_and_size(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            self.assertEqual(progress(root), 'waiting for journal')
            db = root / 'graziani-source-authority-test' / 'campaign.sqlite'
            db.parent.mkdir()
            with sqlite3.connect(db) as con:
                con.execute('CREATE TABLE accepted_steps (sequence INTEGER)')
                con.executemany('INSERT INTO accepted_steps VALUES (?)', [(1,), (2,)])
            self.assertEqual(progress(root), f'accepted_steps=2 db_bytes={db.stat().st_size}')

    def test_soak_requires_exactly_one_passing_outlier(self):
        with tempfile.TemporaryDirectory() as tmp:
            report = Path(tmp) / 'report.xunit.xml'
            report.write_text(xml(rows_for({SOAK_METHOD: 1})))
            verify_soak(tmp)
            report.write_text(xml(rows_for({SOAK_METHOD: 2})))
            with self.assertRaises(ValueError):
                verify_soak(tmp)
            report.write_text(xml(rows_for({SOAK_METHOD: 1}), failed=1))
            with self.assertRaises(ValueError):
                verify_soak(tmp)


if __name__ == '__main__':
    unittest.main()
