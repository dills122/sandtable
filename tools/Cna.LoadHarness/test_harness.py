"""Black-box load harness contract tests; run with the Release harness already built."""
import base64
import hashlib
import http.server
import threading
import json
import os
import pathlib
import subprocess
import tempfile
import unittest

DLL = pathlib.Path(__file__).resolve().parents[2] / 'artifacts/bin/Cna.LoadHarness/release/Cna.LoadHarness.dll'


class HarnessTests(unittest.TestCase):
    def run_harness(self, output, *args, env=None):
        return subprocess.run(['dotnet', str(DLL), '--output', str(output), *args],
                              capture_output=True, text=True, timeout=600, env=env)

    def test_invalid_config(self):
        with tempfile.TemporaryDirectory() as tmp:
            result = self.run_harness(pathlib.Path(tmp) / 'run', '--campaigns', '17')
            self.assertEqual(result.returncode, 2)
            self.assertIn('campaigns', result.stderr)

    def test_full_policy_coverage_gate_and_repeat(self):
        with tempfile.TemporaryDirectory() as tmp:
            outputs = [pathlib.Path(tmp) / name for name in ('a', 'b')]
            for output in outputs:
                result = self.run_harness(output, '--policy', 'random-legal-full',
                                          '--until', 'turn2-reserve', '--root-seed', '7',
                                          '--max-steps-per-campaign', '1', '--require-coverage', 'moves,attacks')
                self.assertEqual(result.returncode, 1, result.stdout + result.stderr)
                summary = json.loads((output / 'summary.json').read_text())
                self.assertEqual(summary['coverage']['moves'], 0)
                self.assertEqual(summary['coverage']['attacks'], 0)
                self.assertEqual(summary['missingCoverage'], ['moves', 'attacks'])
                self.assertEqual(summary['perCampaign'][0]['coverage']['moves'], 0)
                self.assertEqual(summary['coverage']['journalCampaigns'], 1)
                self.assertEqual(summary['refusalWedgeCount'], 0)
                self.assertGreater(summary['coverage']['journalPositionsVisited'], 0)
            self.assertEqual((outputs[0] / 'campaigns/0.jsonl').read_bytes(),
                             (outputs[1] / 'campaigns/0.jsonl').read_bytes())
            self.assertEqual((outputs[0] / 'attempts/0.jsonl').read_bytes(),
                             (outputs[1] / 'attempts/0.jsonl').read_bytes())

    def test_full_policy_refusal_retry_and_wedge(self):
        for mode in ('retry', 'completion', 'wedge', 'unexpected'):
            with self.subTest(mode=mode):
                submissions = []

                class Handler(http.server.BaseHTTPRequestHandler):
                    def do_POST(self):
                        if self.headers.get('Transfer-Encoding') == 'chunked':
                            chunks = []
                            while size := int(self.rfile.readline().strip(), 16):
                                chunks.append(self.rfile.read(size))
                                self.rfile.readline()
                            self.rfile.readline()
                            body = json.loads(b''.join(chunks))
                        else:
                            body = json.loads(self.rfile.read(int(self.headers['Content-Length'])))
                        parts = self.path.strip('/').split('/')
                        route = '/'.join(parts[4:])
                        status = 200
                        if self.path == '/api/v2/campaigns/':
                            value = {'campaignId': body['campaignId']}
                            status = 201
                        elif route.startswith('grants/'):
                            value = {'grant': {'campaignId': parts[3], 'audience': parts[-1],
                                               'epoch': 1, 'secret': 'test-grant', 'expiresUnixMs': 100}}
                        elif route == 'seats':
                            value = {'credential': body['grant']}
                        else:
                            audience = body['credential']['audience']
                            value = {'campaignId': parts[3], 'audience': audience}
                            if route == 'openings/open':
                                value['open'] = [{'openingStage': 'stage-entry-movement',
                                                  'actionStage': 'stage-entry-movement',
                                                  'actingElementIds': ['test-element'],
                                                  'ruleNotImplemented': False}] if audience == 'axis' else []
                                if mode == 'completion' and audience == 'axis':
                                    value['open'].append({'openingStage': 'stage-entry-movement-completion',
                                                          'actionStage': 'stage-entry-movement-completion',
                                                          'actingElementIds': ['test-completion'],
                                                          'ruleNotImplemented': False})
                            elif route == 'openings':
                                cut = {'positionId': 'test-position', 'movementOffers': [
                                    {'kind': 'move-element', 'totalCostNumerator': 3, 'totalCostDenominator': 2},
                                    {'kind': 'move-element', 'totalCostNumerator': 3, 'totalCostDenominator': 2}]}
                                refs = ['a', 'b']
                                if body['stage'] == 'stage-entry-movement-completion':
                                    cut = {'positionId': 'test-position', 'choices': [{'kind': 'complete'}]}
                                    refs = ['c']
                                value.update(stage=body['stage'], actingElementId=body['actingElementId'], revision=1,
                                             visibleCutBase64=base64.b64encode(json.dumps(cut).encode()).decode(),
                                             publicActionRefs=refs)
                            elif route == 'actions':
                                submissions.append(body['publicActionRef'])
                                if not (mode == 'retry' and len(submissions) > 1 or
                                        mode == 'completion' and body['publicActionRef'] == 'c'):
                                    status = 409
                                    value = {'code': 'invalid-authority' if mode == 'unexpected' else 'rule-not-implemented'}
                                else:
                                    value.update(accepted=True, openingRevision=1, publicActionRef=body['publicActionRef'])
                        data = json.dumps({'contractVersion': 2, **value}).encode()
                        self.send_response(status)
                        self.send_header('Content-Length', str(len(data)))
                        self.end_headers()
                        self.wfile.write(data)

                    def log_message(self, *args):
                        pass

                with http.server.ThreadingHTTPServer(('127.0.0.1', 0), Handler) as server, tempfile.TemporaryDirectory() as tmp:
                    thread = threading.Thread(target=server.serve_forever, daemon=True)
                    thread.start()
                    try:
                        output = pathlib.Path(tmp) / 'run'
                        result = self.run_harness(output, '--driver', 'http', '--policy', 'random-legal-full',
                                                  '--until', 'turn2-reserve', '--max-steps-per-campaign', '1',
                                                  '--base-url', f'http://127.0.0.1:{server.server_port}',
                                                  '--stop-error-rate', '0',
                                                  env={**os.environ, 'SANDTABLE_LOAD_OPERATOR_SECRET': 'test-only'})
                        self.assertEqual(result.returncode, 1, result.stdout + result.stderr)
                        summary = json.loads((output / 'summary.json').read_text())
                        self.assertEqual(summary['errorCount'], int(mode == 'unexpected'))
                        if mode == 'completion':
                            self.assertGreater(summary['coverage']['reactionRefused'], 0)
                            self.assertEqual(submissions[-1], 'c')
                            self.assertEqual(summary['coverage']['finishes'], 1)
                        else:
                            self.assertEqual(summary['coverage']['reactionRefused'], {'retry': 1, 'wedge': 2, 'unexpected': 0}[mode])
                        self.assertEqual(summary['coverage']['moves'], int(mode == 'retry'))
                        self.assertEqual(summary['coverage']['moveCpSpent'], 1.5 if mode == 'retry' else 0)
                        self.assertEqual(summary['outcomes'], { {'retry': 'step-limit', 'completion': 'step-limit', 'wedge': 'wedge',
                                                                 'unexpected': 'submission-rejected'}[mode]: 1})
                        if mode != 'completion':
                            self.assertEqual(len(submissions), 1 if mode == 'unexpected' else 2)
                        self.assertEqual(len(set(submissions)), len(submissions))
                    finally:
                        server.shutdown()
                        thread.join()

    def test_invalid_l3_options(self):
        with tempfile.TemporaryDirectory() as tmp:
            for args in [('--store', 'unknown'), ('--until', 'invalid'), ('--hot-pool', '0')]:
                with self.subTest(args=args):
                    result = self.run_harness(pathlib.Path(tmp) / 'run', *args)
                    self.assertEqual(result.returncode, 2, result.stdout + result.stderr)

    def test_invalid_http_options(self):
        with tempfile.TemporaryDirectory() as tmp:
            for args in [('--driver', 'unknown'), ('--driver', 'http', '--base-url', 'https://example.com'),
                         ('--driver', 'http', '--max-inflight', '0'),
                         ('--driver', 'http', '--think-ms-min', '2', '--think-ms-max', '1')]:
                with self.subTest(args=args):
                    result = self.run_harness(pathlib.Path(tmp) / 'run', *args)
                    self.assertEqual(result.returncode, 2, result.stdout + result.stderr)

    def test_http_step_limit_and_cliff(self):
        with tempfile.TemporaryDirectory() as tmp:
            output = pathlib.Path(tmp) / 'run'
            result = self.run_harness(output, '--driver', 'http', '--max-steps-per-campaign', '1',
                                      '--campaigns', '2', '--max-inflight', '1', '--ramp-seconds', '1',
                                      '--think-ms-min', '1', '--think-ms-max', '2')
            self.assertEqual(result.returncode, 1, result.stdout + result.stderr)
            summary = json.loads((output / 'summary.json').read_text())
            self.assertEqual(summary['outcomes'], {'step-limit': 2})
            self.assertEqual(summary['steps'], 2)
            self.assertEqual(summary['peakInflight'], 1)
            self.assertGreater(summary['discoveryProbes'], 0)
            self.assertFalse(summary['stoppedOnCliff'])
            cliff = pathlib.Path(tmp) / 'cliff'
            result = self.run_harness(cliff, '--driver', 'http', '--stop-submit-p95-ms', '0.01')
            self.assertEqual(result.returncode, 1, result.stdout + result.stderr)
            summary = json.loads((cliff / 'summary.json').read_text())
            self.assertTrue(summary['stoppedOnCliff'])
            self.assertEqual(summary['cliffReason'], 'submit-p95')

    @staticmethod
    def public_rows(output, ordinal=0):
        rows = [json.loads(line) for line in (output / f'campaigns/{ordinal}.jsonl').read_text().splitlines()]
        for row in rows:
            row['resultingRevision'] = None
        return rows

    def test_http_stage2_matches_public_decisions_and_pacing(self):
        with tempfile.TemporaryDirectory() as tmp:
            base, http, paced = (pathlib.Path(tmp) / name for name in ('base', 'http', 'paced'))
            for output, extra in ((base, ()), (http, ('--driver', 'http')),
                                  (paced, ('--driver', 'http', '--think-ms-min', '2', '--think-ms-max', '9'))):
                result = self.run_harness(output, '--policy', 'random-legal', '--root-seed', '7', *extra)
                self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertEqual(self.public_rows(base), self.public_rows(http))
            self.assertEqual((http / 'fingerprints.json').read_bytes(), (paced / 'fingerprints.json').read_bytes())
            rows = [json.loads(line) for line in (http / 'campaigns/0.jsonl').read_text().splitlines()]
            self.assertTrue(all(row['resultingRevision'] is None for row in rows))
            summary = json.loads((http / 'summary.json').read_text())
            self.assertEqual(summary['fallbackProbes'], summary['calls'].get('probe', {}).get('count', 0))
            self.assertEqual(summary['expected409s'], sum(call['conflicts'] for call in summary['calls'].values()))
            self.assertEqual(summary['errorCount'], 0)

    def test_http_attach_error_cliff(self):
        # A loopback transport double injects a server failure; real route success is covered
        # by the launched-host tests. No remote URL or real operator credentials are involved.
        observed = []

        class Handler(http.server.BaseHTTPRequestHandler):
            def do_POST(self):
                observed.append((self.path, self.headers.get('Sec-Fetch-Site'),
                                 self.headers.get('X-Sandtable-Operator')))
                self.send_response(503)
                self.end_headers()

            def log_message(self, *args):
                pass

        with http.server.ThreadingHTTPServer(('127.0.0.1', 0), Handler) as server, tempfile.TemporaryDirectory() as tmp:
            thread = threading.Thread(target=server.serve_forever, daemon=True)
            thread.start()
            try:
                output = pathlib.Path(tmp) / 'run'
                secret = 'test-only-operator-secret'
                result = self.run_harness(output, '--driver', 'http',
                                          '--base-url', f'http://127.0.0.1:{server.server_port}',
                                          '--campaigns', '2', '--max-inflight', '1',
                                          '--stop-error-rate', '0',
                                          env={**os.environ, 'SANDTABLE_LOAD_OPERATOR_SECRET': secret})
                self.assertEqual(result.returncode, 1, result.stdout + result.stderr)
                summary = json.loads((output / 'summary.json').read_text())
                self.assertEqual(summary['errorCount'], 1)
                self.assertEqual(summary['expected409s'], 0)
                self.assertEqual(summary['cliffReason'], 'error-rate')
                self.assertEqual(observed, [('/api/v2/campaigns/', 'same-origin', secret)])
                for path in output.rglob('*'):
                    if path.is_file():
                        self.assertNotIn(secret, path.read_text())
            finally:
                server.shutdown()
                thread.join()

    def test_http_expected_probe_conflict_is_not_a_cliff(self):
        class Handler(http.server.BaseHTTPRequestHandler):
            def do_POST(self):
                if self.headers.get('Transfer-Encoding') == 'chunked':
                    chunks = []
                    while size := int(self.rfile.readline().strip(), 16):
                        chunks.append(self.rfile.read(size))
                        self.rfile.readline()
                    self.rfile.readline()
                    body = json.loads(b''.join(chunks))
                else:
                    body = json.loads(self.rfile.read(int(self.headers['Content-Length'])))
                parts = self.path.strip('/').split('/')
                route = '/'.join(parts[4:])
                status = 200
                if self.path == '/api/v2/campaigns/':
                    value = {'campaignId': body['campaignId']}
                    status = 201
                elif route.startswith('grants/'):
                    value = {'grant': {'campaignId': parts[3], 'audience': parts[-1],
                                       'epoch': 1, 'secret': 'test-grant', 'expiresUnixMs': 100}}
                elif route == 'seats':
                    value = {'credential': body['grant']}
                elif route == 'openings/open' and body['credential']['audience'] == 'axis':
                    value = {'campaignId': parts[3], 'audience': 'axis', 'open': [{
                        'openingStage': 'movement', 'actionStage': 'movement',
                        'actingElementIds': ['test-element'], 'ruleNotImplemented': False}]}
                elif route == 'openings':
                    value = {'code': 'unavailable'}
                    status = 409
                else:
                    value = {'code': 'unavailable'}
                    status = 409  # Discovery conflict is not an expected opening-probe conflict.
                data = json.dumps({'contractVersion': 2, **value}).encode()
                self.send_response(status)
                self.send_header('Content-Length', str(len(data)))
                self.end_headers()
                self.wfile.write(data)

            def log_message(self, *args):
                pass

        with http.server.ThreadingHTTPServer(('127.0.0.1', 0), Handler) as server, tempfile.TemporaryDirectory() as tmp:
            thread = threading.Thread(target=server.serve_forever, daemon=True)
            thread.start()
            try:
                output = pathlib.Path(tmp) / 'run'
                result = self.run_harness(output, '--driver', 'http',
                                          '--base-url', f'http://127.0.0.1:{server.server_port}',
                                          '--stop-error-rate', '0',
                                          env={**os.environ, 'SANDTABLE_LOAD_OPERATOR_SECRET': 'test-only'})
                self.assertEqual(result.returncode, 1, result.stdout + result.stderr)
                summary = json.loads((output / 'summary.json').read_text())
                self.assertEqual(summary['expected409s'], 1)
                self.assertEqual(summary['errorCount'], 1)
                self.assertEqual(summary['calls']['open']['conflicts'], 1)
                self.assertEqual(summary['calls']['discover']['errors'], 1)
                self.assertEqual(summary['cliffReason'], 'error-rate')
            finally:
                server.shutdown()
                thread.join()

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
            http = pathlib.Path(tmp) / 'http'
            result = self.run_harness(http, '--driver', 'http', '--store', 'postgres', '--until', 'turn2-reserve',
                                      '--policy', 'random-legal', '--root-seed', '7', '--max-inflight', '1')
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            http_summary = json.loads((http / 'summary.json').read_text())
            self.assertEqual(http_summary['completed'], 1)
            self.assertEqual(http_summary['errorCount'], 0)
            self.assertEqual(self.public_rows(output), self.public_rows(http))


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
            self.assertGreater(summary['postgresConnectionSamples'], 0)
            self.assertEqual(summary['postgresConnectionSampleFailures'], 0)

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
