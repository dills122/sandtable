"""Protect scoped inventory counts against malformed and truncated tables."""
import importlib.util
import unittest
from pathlib import Path

path = Path(__file__).with_name("verify-graziani-register.py")
spec = importlib.util.spec_from_file_location("register_check", path)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class RegisterCountTests(unittest.TestCase):
    def setUp(self):
        self.table = ("## Initial obligation rows\n\n"
                      "| ID | Source | Evidence | Outstanding |\n"
                      "| --- | --- | --- | --- |\n"
                      "| TI-01 | source | seen | open |\n"
                      "| LA-01 | source | seen | check |\n\n"
                      "The [victory matrix] follows.\n")

    def test_scoped_counts_never_claim_unsupported_mechanics(self):
        result = module.summarize(self.table)
        self.assertEqual(2, result["recorded_rows"])
        self.assertEqual({"LA": 1, "TI": 1}, result["rows_by_prefix"])
        self.assertEqual(2, result["rows_with_nonempty_outstanding_field"])
        self.assertIsNone(result["unsupported_mechanics_count"])

    def test_duplicate_or_malformed_row_rejected(self):
        with self.assertRaises(ValueError):
            module.summarize(self.table.replace("LA-01", "TI-01"))
        with self.assertRaises(ValueError):
            module.summarize(self.table.replace("LA-01 | source", "bad-id | source"))
        with self.assertRaises(ValueError):
            module.summarize(self.table.replace("LA-01 | source", "LA-01 |"))

    def test_missing_boundary_and_premature_gap_rejected(self):
        with self.assertRaises(ValueError):
            module.summarize(self.table.replace("The [victory matrix]", "Other text"))
        with self.assertRaises(ValueError):
            module.summarize(self.table.replace("| LA-01", "\n| LA-01"))

    def test_closed_row_requires_updated_counting_policy(self):
        with self.assertRaises(ValueError):
            module.summarize(self.table.replace("| check |", "| complete |"))


if __name__ == "__main__":
    unittest.main()
