#!/usr/bin/env python3
"""Fail-sensitive tests for the private P01 denial projection."""

import copy
import importlib.util
import json
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parent


def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, ROOT / filename)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


privacy = load("p01_privacy_tests", "verify-combat-p01-privacy-gates-v1.py")
r03 = privacy.p01.r03


def authentic(index):
    row = r03.r02.semantic_rows()[index]
    parent, ledgers = r03.parent_terminal(row)
    source = r03.source_for(parent)
    _, context, before = r03.replay(source, ledgers, [])
    source, final_ledger, _, _ = r03.apply(
        source, ledgers, [], r03.raw(r03.command(context, before), "Input"))
    return privacy.p01.assess(source, ledgers, final_ledger)


class PrivateDenialProjectionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.empty = authentic(0)
        cls.positive = authentic(36)
        cls.other_actor = authentic(168)

    def test_genuine_shapes_have_identical_denial_bytes(self):
        self.assertEqual("provisional-empty", self.empty["physicalShape"])
        self.assertEqual("provisional-positive", self.positive["physicalShape"])
        self.assertNotEqual(self.positive["privateCandidateId"],
                            self.other_actor["privateCandidateId"])
        values = [privacy.denial_bytes(x)
                  for x in (self.empty, self.positive, self.other_actor)]
        self.assertEqual([values[0]] * 3, values)
        self.assertEqual({"contractVersion": 1, "status": "unsupported",
                          "admitted": False, "publicActions": []},
                         json.loads(values[0]))
        for item in (self.empty, self.positive, self.other_actor):
            self.assertNotIn(str(item["privateCandidateId"]).encode("ascii"), values[0])
            self.assertNotIn(item["sourceHash"].encode("ascii"), values[0])

    def test_private_mutations_cannot_change_denial_bytes(self):
        baseline = privacy.denial_bytes(self.positive)
        for field, value in (("privateCandidateId", "p01." + "a" * 64),
                             ("sourceHash", "sha256:" + "b" * 64),
                             ("worldHash", "sha256:" + "c" * 64),
                             ("randomHash", "sha256:" + "d" * 64),
                             ("physicalShape", "unresolved"),
                             ("activeCycleId", "hidden-cycle"),
                             ("positionId", "hidden-position")):
            with self.subTest(field=field):
                changed = copy.deepcopy(self.positive)
                changed[field] = value
                self.assertEqual(baseline, privacy.denial_bytes(changed))

    def test_action_or_private_field_leak_rejects(self):
        mutations = (("publicActions", [{"candidateId": self.positive["privateCandidateId"]}]),
                     ("status", "bounded-positive"), ("admitted", True),
                     ("contractVersion", 2), ("contractVersion", True))
        for field, value in mutations:
            with self.subTest(field=field):
                changed = copy.deepcopy(self.positive)
                changed[field] = value
                with self.assertRaises(ValueError):
                    privacy.denial_bytes(changed)
        changed = copy.deepcopy(self.positive)
        changed["privateCandidateIdCopy"] = changed["privateCandidateId"]
        with self.assertRaises(ValueError):
            privacy.denial_bytes(changed)


if __name__ == "__main__":
    unittest.main()
