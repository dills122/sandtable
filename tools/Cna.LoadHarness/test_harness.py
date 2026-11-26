"""Black-box load harness contract tests; run with the Release harness already built."""
import hashlib
import json
import os
import pathlib
import subprocess
import tempfile
import unittest

DLL = pathlib.Path(__file__).resolve().parents[2] / 'artifacts/bin/Cna.LoadHarness/release/Cna.LoadHarness.dll'


class HarnessTests(unittest.TestCase):
    def run_harness(self, output, *args):
        return subprocess.run(['dotnet', str(DLL), '--output', str(output), *args],
                              capture_output=True, text=True, timeout=600)

    def test_invalid_config(self):
        with tempfile.TemporaryDirectory() as tmp:
            result = self.run_harness(pathlib.Path(tmp) / 'run', '--campaigns', '17')
            self.assertEqual(result.returncode, 2)
            self.assertIn('campaigns', result.stderr)

    def test_invalid_l3_options(self):
        with tempfile.TemporaryDirectory() as tmp:
            for args in [('--store', 'unknown'), ('--until', 'invalid'), ('--hot-pool', '0')]:
                with self.subTest(args=args):
                    result = self.run_harness(pathlib.Path(tmp) / 'run', *args)
                    self.assertEqual(result.returncode, 2, result.stdout + result.stderr)

    def test_explicit_sqlite_preserves_contract(self):
        with tempfile.TemporaryDirectory() as tmp:
            output = pathlib.Path(tmp) / 'run'
            result = self.run_harness(output, '--store', 'sqlite', '--max-steps-per-campaign', '1')
            self.assertEqual(result.returncode, 1, result.stdout + result.stderr)
            self.assertEqual(json.loads((output / 'summary.json').read_text())['outcomes'], {'step-limit': 1})
            self.assertNotIn('gc', json.loads((output / 'summary.json').read_text()))
            implicit = pathlib.Path(tmp) / 'implicit'
            result = self.run_harness(implicit, '--max-steps-per-campaign', '1')
            self.assertEqual(result.returncode, 1, result.stdout + result.stderr)
            for name in ('run-manifest.json', 'fingerprints.json', 'campaigns/0.jsonl', 'seed-ledgers/0.json'):
                self.assertEqual((output / name).read_bytes(), (implicit / name).read_bytes())

    @unittest.skipUnless(os.getenv('SANDTABLE_LOAD_POSTGRES_TEST') == '1', 'opt-in Postgres integration')
    def test_postgres_turn2_reserve(self):
        with tempfile.TemporaryDirectory() as tmp:
            output = pathlib.Path(tmp) / 'run'
            result = self.run_harness(output, '--store', 'postgres', '--until', 'turn2-reserve',
                                      '--hot-pool', '2', '--policy', 'random-legal', '--root-seed', '7')
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            summary = json.loads((output / 'summary.json').read_text())
            self.assertEqual(summary['outcomes'], {'turn2-reserve': 1})
            self.assertGreater(summary['steps'], 22)
            self.assertGreater(summary['postgresConnectionPeak'], 0)
            self.assertLessEqual(summary['postgresConnectionPeak'], 2)
            self.assertGreater(summary['rssPeakBytes'], 0)
            self.assertGreaterEqual(summary['gc']['pauseMs'], 0)
            rows = [json.loads(line) for line in (output / 'campaigns/0.jsonl').read_text().splitlines()]
            self.assertEqual(sum(row['stage'] in ('initiative-order', 'stage-entry-initiative-declaration')
                                 for row in rows), 4)
            self.assertEqual(rows[-1]['stage'], 'stage-entry-initiative-declaration')

    @unittest.skipUnless(os.getenv('SANDTABLE_LOAD_POSTGRES_TEST') == '1', 'opt-in Postgres integration')
    def test_postgres_hot_pool_and_step_limit(self):
        with tempfile.TemporaryDirectory() as tmp:
            output = pathlib.Path(tmp) / 'run'
            result = self.run_harness(output, '--store', 'postgres', '--campaigns', '2',
                                      '--hot-pool', '1', '--max-steps-per-campaign', '1')
            self.assertEqual(result.returncode, 1, result.stdout + result.stderr)
            summary = json.loads((output / 'summary.json').read_text())
            self.assertEqual(summary['outcomes'], {'step-limit': 2})
            self.assertEqual(summary['steps'], 2)
            self.assertEqual(summary['postgresConnectionPeak'], 1)

    def test_seeded_random_is_reproducible_and_isolated(self):
        with tempfile.TemporaryDirectory() as tmp:
            outputs = [pathlib.Path(tmp) / name for name in ['single', 'a', 'b']]
            for output, count in zip(outputs, [1, 2, 2]):
                result = self.run_harness(output, '--campaigns', str(count), '--policy', 'random-legal', '--root-seed', '7')
                self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
                self.assertEqual(json.loads((output / 'summary.json').read_text())['completed'], count)
                for ordinal in range(count):
                    ledger = json.loads((output / f'seed-ledgers/{ordinal}.json').read_text())
                    self.assertEqual(ledger['schemeId'], 'sandtable.exercise-seeds.v1')
                    states = {entry['role']: entry['derivedSeed'] for entry in ledger['entries']
                              if entry['domain'] == 'controller'}
                    mask = (1 << 64) - 1
                    for line in (output / f'campaigns/{ordinal}.jsonl').read_text().splitlines():
                        row = json.loads(line)
                        role = row['audience']
                        bound = row['choiceCount']
                        while True:
                            states[role] = (states[role] + 0x9e3779b97f4a7c15) & mask
                            value = states[role]
                            value = ((value ^ (value >> 30)) * 0xbf58476d1ce4e5b9) & mask
                            value = ((value ^ (value >> 27)) * 0x94d049bb133111eb) & mask
                            value ^= value >> 31
                            if value >= (1 << 64) % bound:
                                break
                        self.assertEqual(row['chosenIndex'], value % bound)
            self.assertEqual((outputs[1] / 'fingerprints.json').read_bytes(),
                             (outputs[2] / 'fingerprints.json').read_bytes())
            self.assertEqual((outputs[0] / 'campaigns/0.jsonl').read_bytes(),
                             (outputs[1] / 'campaigns/0.jsonl').read_bytes())

    def test_first_policy(self):
        with tempfile.TemporaryDirectory() as tmp:
            output = pathlib.Path(tmp) / 'run'
            result = self.run_harness(output, '--policy', 'first')
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            rows = [json.loads(line) for line in (output / 'campaigns/0.jsonl').read_text().splitlines()]
            self.assertGreater(len(rows), 12)
            self.assertTrue(all(row['chosenIndex'] == 0 for row in rows))

    def test_step_limit_is_not_success(self):
        with tempfile.TemporaryDirectory() as tmp:
            output = pathlib.Path(tmp) / 'run'
            result = self.run_harness(output, '--max-steps-per-campaign', '1')
            self.assertEqual(result.returncode, 1, result.stdout + result.stderr)
            summary = json.loads((output / 'summary.json').read_text())
            self.assertEqual(summary['outcomes'], {'step-limit': 1})
            self.assertEqual(summary['steps'], 1)

    def test_two_fresh_runs(self):
        with tempfile.TemporaryDirectory() as tmp:
            outputs = [pathlib.Path(tmp) / name for name in ['a', 'b']]
            for output in outputs:
                result = self.run_harness(output, '--campaigns', '1', '--policy', 'last', '--root-seed', '7')
                self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
                summary = json.loads((output / 'summary.json').read_text())
                self.assertEqual(summary['completed'], 1)
                steps = (output / 'campaigns/0.jsonl').read_bytes()
                rows = [json.loads(line) for line in steps.splitlines()]
                self.assertGreater(len(rows), 12)
                self.assertTrue(all(row['chosenIndex'] == row['choiceCount'] - 1 for row in rows))
                self.assertTrue(all(row['stage'] != 'stage-entry-initiative-declaration' for row in rows))
                fingerprints = json.loads((output / 'fingerprints.json').read_text())
                self.assertEqual(fingerprints['campaigns'][0]['decisionFingerprint'],
                                 'sha256:' + hashlib.sha256(steps).hexdigest())
            self.assertEqual((outputs[0] / 'fingerprints.json').read_bytes(),
                             (outputs[1] / 'fingerprints.json').read_bytes())
            self.assertEqual((outputs[0] / 'campaigns/0.jsonl').read_bytes(),
                             (outputs[1] / 'campaigns/0.jsonl').read_bytes())


if __name__ == '__main__':
    unittest.main()
