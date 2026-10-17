#!/usr/bin/env python3
"""Fail-sensitive, genuine-source tests for the private P01 Weather bridge."""
import copy
import importlib.util
import json
from pathlib import Path
import unittest
from unittest import mock

ROOT = Path(__file__).resolve().parent


def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, ROOT / filename)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


p01 = load("p01_weather_reference", "verify-combat-actual-moved-repeat-position-v1.py")


def terminal(index):
    row = p01.r03.r02.semantic_rows()[index]
    parent_source, ledgers = p01.r03.parent_terminal(row)
    source = p01.r03.source_for(parent_source)
    _, context, before = p01.r03.replay(source, ledgers, [])
    command = p01.r03.raw(p01.r03.command(context, before), "Input")
    source, own, _, _ = p01.r03.apply(source, ledgers, [], command)
    return source, ledgers, own


def prefix(source):
    packet = source
    for key in p01.SCHEMA["weatherSourcePath"]:
        packet = json.loads(packet[key])
    return packet


class P01WeatherBridgeTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.samples = [terminal(index) for index in (0, 36, 168)]

    def test_authentic_weather_is_bound_to_receipt_and_active_stage(self):
        for source_bytes, ledgers, own in self.samples:
            source, _, state = p01.r03.replay(source_bytes, ledgers, own)
            event_bytes = prefix(source)["prefixEventCanonicalUtf8"][4].encode("ascii")
            event = json.loads(event_bytes)
            fact = p01.current_weather_fact(source, state)
            self.assertEqual(list(fact), p01.SCHEMA["currentWeatherFactFields"])
            self.assertEqual("normal", fact["kind"])
            self.assertEqual((1, 1), (fact["gameTurn"], fact["operationStage"]))
            self.assertEqual(event["receiptId"], fact["receiptId"])
            self.assertEqual(p01.digest(event_bytes), fact["eventHash"])
            self.assertEqual("unsupported", p01.assess(source_bytes, ledgers, own)["status"])

    def test_missing_altered_foreign_and_wrong_stage_weather_reject(self):
        source_bytes, ledgers, own = self.samples[1]
        source, _, state = p01.r03.replay(source_bytes, ledgers, own)
        for label in ("missing", "altered-kind", "foreign-event", "foreign-receipt",
                      "changed-event-stage", "changed-stage"):
            with self.subTest(label=label):
                changed_source, changed_state = copy.deepcopy(source), copy.deepcopy(state)
                if label in ("missing", "altered-kind", "foreign-event", "changed-event-stage"):
                    packet = changed_source
                    path = p01.SCHEMA["weatherSourcePath"]
                    layers = [packet]
                    for key in path:
                        packet = json.loads(packet[key])
                        layers.append(packet)
                    events = packet["prefixEventCanonicalUtf8"]
                    if label == "missing":
                        events.pop(4)
                    elif label == "altered-kind":
                        events[4] = events[4].replace('"kind":"normal"', '"kind":"storm"')
                    elif label == "foreign-event":
                        events[4] = events[4].replace('"receiptId":"wth.', '"receiptId":"wth.foreign-')
                    else:
                        events[4] = events[4].replace('"operationStage":1', '"operationStage":2', 1)
                    for index in range(len(path) - 1, -1, -1):
                        layers[index][path[index]] = p01.canonical(layers[index + 1]).decode("ascii")
                elif label == "foreign-receipt":
                    changed_state["receipts"][4]["receiptId"] = "wth.foreign"
                else:
                    changed_state["activeCycle"]["operationStage"] = 2
                with self.assertRaises(ValueError):
                    p01.current_weather_fact(changed_source, changed_state)

    def test_weather_gate_runs_before_physical_classification(self):
        source, ledgers, own = self.samples[1]
        with mock.patch.object(p01, "current_weather_fact", side_effect=ValueError("weather")):
            with mock.patch.object(p01, "physical_shape", side_effect=AssertionError("classified")):
                with self.assertRaisesRegex(ValueError, "weather"):
                    p01.assess(source, ledgers, own)


if __name__ == "__main__":
    unittest.main()
