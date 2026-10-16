"""Fail-closed checks for the provisional moved-repeat event size model."""

import copy
import importlib.util
import json
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parent
SCRIPT = ROOT / "verify-combat-actual-moved-repeat-breakdown-event-bound-v1.py"
SCHEMA = ROOT / "combat-actual-moved-assault-v1.schema.json"
CONTENT = ROOT / "fixtures/combat-content-v7.canonical.json"


def load():
    spec = importlib.util.spec_from_file_location("mrb_event_bound", SCRIPT)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class EventBoundTests(unittest.TestCase):
    def test_all_moved_assault_event_families_fit_frozen_source_cap(self):
        result = load().calculate(json.loads(SCHEMA.read_bytes()))
        self.assertEqual({"SelectionEvent", "RoundEvent", "PaidEvent"}, set(result["eventBounds"]))
        self.assertEqual(47_377, result["eventBounds"]["PaidEvent"])
        self.assertEqual(207_807_680, result["breakdownSourceBound"])
        self.assertLess(result["breakdownSourceBound"], 268_435_456)

    def test_new_array_without_a_source_backed_cardinality_fails_closed(self):
        schema = copy.deepcopy(json.loads(SCHEMA.read_bytes()))
        schema["objects"]["PaidEvent"] += " unreviewed:id[]"
        with self.assertRaisesRegex(ValueError, "PaidEvent.unreviewed"):
            load().calculate(schema)

    def test_new_effect_variant_without_cardinality_fails_closed(self):
        schema = copy.deepcopy(json.loads(SCHEMA.read_bytes()))
        schema["objects"]["UnreviewedEffect"] = "kind:id receipts:id[]"
        schema["effectTags"]["PaidEffect"]["unreviewed"] = "UnreviewedEffect"
        with self.assertRaisesRegex(ValueError, "UnreviewedEffect.receipts"):
            load().calculate(schema)

    def test_pinned_profile_has_one_component_per_participant(self):
        content = json.loads(CONTENT.read_bytes())
        self.assertEqual(2, len(content["elements"]))
        self.assertEqual([1, 1], [len(element["components"]) for element in content["elements"]])
        model = load()
        self.assertEqual(1, model.ARRAY_COUNTS["Participant.componentIds"])
        self.assertEqual(770, len(model.encoded(model.maximal_value("id", {}, {}))))


if __name__ == "__main__":
    unittest.main()
