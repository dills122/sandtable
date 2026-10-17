#!/usr/bin/env python3
"""Fail-sensitive AC-008 diagnostic checks; full corpus lives in verifier CLI."""
import copy
import importlib.util
import json
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('p01_replay_gates', ROOT / 'verify-combat-p01-replay-gates-v1.py')
gates = importlib.util.module_from_spec(spec)
spec.loader.exec_module(gates)


class P01ReplayGateTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.samples = {}
        for index in (36, 168):
            row = gates.p01.r03.r02.semantic_rows()[index]
            parent_source, ledgers = gates.p01.r03.parent_terminal(row)
            source = gates.p01.r03.source_for(parent_source)
            _, context, before = gates.p01.r03.replay(source, ledgers, [])
            command = gates.p01.r03.raw(gates.p01.r03.command(context, before), 'Input')
            source, own, _, _ = gates.p01.r03.apply(source, ledgers, [], command)
            cls.samples[index] = (index, row['rowKey'], 'provisional-positive',
                                  source, ledgers, own)

    def test_both_actor_sides_retry_without_state_or_input_changes(self):
        sides = set()
        for sample in self.samples.values():
            index, _, shape, source, ledgers, own = sample
            before = copy.deepcopy((source, ledgers, own))
            state_before = gates.p01.r03.replay(source, ledgers, own)[2]
            first = gates.p01.assess(source, ledgers, own)
            second = gates.p01.assess(source, ledgers, own)
            self.assertEqual(first, second, index)
            self.assertEqual(before, (source, ledgers, own))
            self.assertEqual(state_before, gates.p01.r03.replay(source, ledgers, own)[2])
            self.assertEqual('provisional-positive', shape)
            self.assertEqual('unsupported', first['status'])
            self.assertFalse(first['admitted'])
            self.assertEqual([], first['publicActions'])
            sides.add(state_before['activeCycle']['actingSide'])
        self.assertEqual({'axis', 'commonwealth'}, sides)

    def test_changed_output_fields_reject(self):
        sample = self.samples[36]
        _, _, shape, source, ledgers, own = sample
        assessment = gates.p01.assess(source, ledgers, own)
        for field, value in (('status', 'supported'), ('admitted', True),
                             ('publicActions', [{'kind': 'fake'}]),
                             ('physicalShape', 'provisional-empty'),
                             ('privateCandidateId', None)):
            changed = copy.deepcopy(assessment)
            changed[field] = value
            with self.subTest(field=field), self.assertRaises(ValueError):
                gates.check_assessment(changed, 'sha256:' + gates.sha(source), shape)

    def test_parent_cuts_mutations_and_dependency_faults_reject(self):
        for index, sample in self.samples.items():
            with self.subTest(row=index):
                self.assertGreater(gates.bounded_mutations(sample), 20)

    @unittest.skipUnless(gates.EVIDENCE.is_dir(), 'private retained corpus unavailable')
    def test_frozen_full_corpus_output_root(self):
        expected = json.loads(gates.FIXTURE.read_bytes())
        report = gates.audit()
        self.assertEqual(300, report['rows'])
        self.assertEqual(44, report['provisionalPositive'])
        self.assertEqual(256, report['provisionalEmpty'])
        self.assertEqual(0, report['admittedRows'])
        self.assertEqual(expected['rowHashRoot'], report['rowHashRoot'])


if __name__ == '__main__':
    unittest.main()
