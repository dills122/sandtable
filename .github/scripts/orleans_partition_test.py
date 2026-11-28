"""Pure-Python checks for the Orleans shard planner/verifier; no .NET or Docker is started."""
import json
from pathlib import Path
import tempfile
import sys
import unittest
from unittest import mock

import orleans_partition as op

T = 'Cna.OrleansHost.Tests.'
LISTING = '\n'.join(
    ['Some banner', ''] +
    ['  ' + name for name in sorted(op.EXCLUDED | op.REQUIRED)] +
    ['  ' + T + 'A.Theory(x: 1)', '  ' + T + 'A.Theory(x: 2)', '  ' + T + 'A.Plain', '  ' + T + 'B.Heavy', '  ' + T + 'C.Light',
     '  duration: 112ms'])


def xml(rows, assembly=op.ASSEMBLY, failed=0):
    tests = ''.join(f'<test name="{n}" type="{t}" method="{m}" result="Pass" time="1"/>' for n, t, m in rows)
    return (f'<assemblies><assembly name="/x/{assembly}.dll" total="{len(rows)}" passed="{len(rows) - failed}" '
            f'failed="{failed}" skipped="0" not-run="0" errors="0"><collection>{tests}</collection></assembly></assemblies>')


def rows_for(counts):
    out = []
    for method, count in counts.items():
        type_, name = method.rsplit('.', 1)
        out += [(f'{method}#{i}', type_, name) for i in range(count)]
    return out


class PlanTests(unittest.TestCase):
    def test_parse_inventory_and_exclusions(self):
        rows = op.parse_listing(LISTING)
        self.assertEqual(len(rows), len(op.EXCLUDED | op.REQUIRED) + 5)
        counts = op.inventory(rows)
        self.assertEqual(counts[T + 'A.Theory'], 2)
        self.assertFalse(set(counts) & op.EXCLUDED)

    def test_missing_excluded_or_required_row_is_rejected(self):
        rows = [r for r in op.parse_listing(LISTING) if op.method_of(r) not in op.REQUIRED]
        with self.assertRaises(ValueError):
            op.inventory(rows)
        rows = [r for r in op.parse_listing(LISTING) if op.method_of(r) not in op.EXCLUDED]
        with self.assertRaises(ValueError):
            op.inventory(rows)

    def test_assignment_is_deterministic_disjoint_complete_and_balanced(self):
        counts = {f'{T}X.M{i}': 1 for i in range(40)}
        weights = {f'{T}X.M{i}': float(i % 7 + 1) for i in range(40)}
        first, second = op.assign(counts, weights, 4), op.assign(counts, weights, 4)
        self.assertEqual(first, second)
        flat = [m for s in first for m in s]
        self.assertEqual(sorted(flat), sorted(counts))
        loads = [sum(weights[m] for m in s) for s in first]
        self.assertLessEqual(max(loads) - min(loads), 7)

    def test_committed_weights_cover_known_shape(self):
        weights = json.loads(op.WEIGHTS.read_text())
        self.assertTrue(weights and all(k.startswith(op.ASSEMBLY + '.') and v >= 0 for k, v in weights.items()))


class VerifyTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(); self.addCleanup(self.tmp.cleanup)
        counts = {m: 1 for m in op.REQUIRED} | {T + 'A.Theory': 2, T + 'B.Heavy': 1}
        self.plan = op.build_plan([m + (f'(i: {i})' if m.endswith('Theory') else '') for m, c in counts.items() for i in range(c)]
                                  + list(op.EXCLUDED), {}, 2)
        self.dirs = []
        for shard in (1, 2):
            d = Path(self.tmp.name) / f's{shard}'; d.mkdir()
            methods = self.plan['shards'][shard - 1]
            (d / 'orleans-plan.json').write_text(json.dumps({**self.plan, 'shard': shard, 'methods': methods}))
            (d / 'r.xml').write_text(xml(rows_for({m: self.plan['counts'][m] for m in methods})))
            self.dirs.append(d)

    def test_complete_shards_verify(self):
        result = op.verify(self.dirs, 2)
        self.assertEqual(result['rows'], sum(self.plan['counts'].values()))

    def test_missing_shard_fails(self):
        with self.assertRaises(ValueError):
            op.verify(self.dirs[:1], 2)

    def test_dropped_row_fails(self):
        d = self.dirs[0]
        methods = self.plan['shards'][0]
        short = rows_for({m: self.plan['counts'][m] for m in methods})[:-1]
        (d / 'r.xml').write_text(xml(short))
        with self.assertRaises(ValueError):
            op.verify(self.dirs, 2)

    def test_failed_row_fails(self):
        d = self.dirs[1]
        rows = rows_for({m: self.plan['counts'][m] for m in self.plan['shards'][1]})
        (d / 'r.xml').write_text(xml(rows, failed=1))
        with self.assertRaises(ValueError):
            op.verify(self.dirs, 2)

    def test_tampered_plan_fails(self):
        p = json.loads((self.dirs[1] / 'orleans-plan.json').read_text())
        p['shards'] = [p['shards'][1], p['shards'][0]]
        (self.dirs[1] / 'orleans-plan.json').write_text(json.dumps(p))
        with self.assertRaises(ValueError):
            op.verify(self.dirs, 2)


class ExecuteTests(unittest.TestCase):
    """The retry loop must never turn a failed or report-less run into success."""
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(); self.addCleanup(self.tmp.cleanup)
        self.out = Path(self.tmp.name) / 'res'
        self.listing = op.parse_listing(LISTING)
        patcher = mock.patch.object(op, 'WEIGHTS', Path(self.tmp.name) / 'w.json'); patcher.start(); self.addCleanup(patcher.stop)
        op.WEIGHTS.write_text('{}')

    def run_execute(self, runner):
        return op.execute(1, 1, self.out, runner=runner, discoverer=lambda: self.listing, attempts=2)

    def test_explicit_weights_leave_default_unchanged(self):
        custom = self.tmp.name + '/nightly.json'
        Path(custom).write_text(json.dumps({T + 'B.Heavy': 100.0}))
        counts = op.inventory(self.listing)
        def runner(cmd, log):
            (self.out / 'r.xml').write_text(xml(rows_for(counts))); return 0
        with mock.patch.object(op, 'build_plan', wraps=op.build_plan) as plan:
            self.assertEqual(op.execute(1, 1, self.out, runner=runner,
                                        discoverer=lambda: self.listing, weights_path=custom), 0)
            self.assertEqual(plan.call_args.args[1], {T + 'B.Heavy': 100.0})
        self.assertEqual(op.WEIGHTS.read_text(), '{}')

    def test_measured_slow_methods_have_weights(self):
        weights = json.loads(Path(op.__file__).with_name('orleans-weights.json').read_text())
        for method, measured in {
            'LaterStageCombatSelectionRunsThroughHttp': 431.2,
            'FreshHttpCampaignReachesTurnTwoCheckpoint': 100.4,
            'TwoHttpSeatsCompleteStageThreeWithDiscoveryAndColdReplay': 88.3,
            'CombatTurnBoundaryOwnerRoundsReplayAndReachTurnTwo': 77.6,
            'AdvertisedTruckSelectionContinuationIsAccepted': 54.2,
            'HttpTurnBoundaryReachesLaterTurnReserveOpening': 51.8,
            'TwoHttpSeatsCompleteStageTwoWithDiscoveryAndColdReplay': 43.8,
        }.items():
            self.assertGreaterEqual(weights.get(T + 'CampaignDurableAuthorityJournalTests.' + method, 0), measured)

    def test_started_row_timeout_is_not_retried(self):
        calls = []
        def runner(cmd, log):
            calls.append(cmd)
            return op.ROW_TIMEOUT
        self.assertEqual(self.run_execute(runner), op.ROW_TIMEOUT)
        self.assertEqual(len(calls), 1)

    def test_watchdog_times_out_started_row_with_name(self):
        self.out.mkdir()
        script = ('import pathlib, sys, time; '
                  'd = pathlib.Path(sys.argv[sys.argv.index("--diagnostic-output-directory") + 1]); '
                  'd.joinpath("test.diag").write_text('
                  '"TestNode = TestNode { Uid = TestNodeUid { Value = a }, DisplayName = A.Blocked, "'
                  '"Properties = [ InProgressTestNodeStateProperty { Explanation = } ] }\\n"); '
                  'time.sleep(30)')
        log = self.out / 'console.log'
        self.assertEqual(op.run_attempt([sys.executable, '-c', script], log,
                                        cap=5, bootstrap_cap=2, row_cap=0.3), op.ROW_TIMEOUT)
        self.assertIn('START A.Blocked', log.read_text())
        self.assertIn('Row exceeded 0.3s: A.Blocked', log.read_text())

    def test_watchdog_bounds_pre_row_timeout(self):
        self.out.mkdir()
        log = self.out / 'console.log'
        self.assertEqual(op.run_attempt([sys.executable, '-c', 'import time; time.sleep(30)'], log,
                                        cap=5, bootstrap_cap=0.3, row_cap=2), op.BOOTSTRAP_TIMEOUT)
        self.assertIn('No row started within 0.3s', log.read_text())

    def test_reportless_non_timeout_failure_is_not_retried(self):
        calls = []
        def runner(cmd, log):
            calls.append(cmd)
            return 2
        self.assertEqual(self.run_execute(runner), 2)
        self.assertEqual(len(calls), 1)

    def test_lifecycle_accepts_multiline_captured_output(self):
        tracker = op.RowProgress()
        header = 'TestNode = TestNode { Uid = TestNodeUid { Value = a }, DisplayName = A.Row, Properties = ['
        self.assertEqual(tracker.observe(header + ' InProgressTestNodeStateProperty { Explanation = } ] }', 10), 'START A.Row')
        self.assertIsNone(tracker.observe(header + ' StandardOutputProperty { Value = first line', 20))
        self.assertIsNone(tracker.observe('second line', 20))
        self.assertEqual(tracker.observe(' }, PassedTestNodeStateProperty { Explanation = } ] }', 20), 'FINISH A.Row (10.0s, Passed)')
        self.assertEqual(tracker.expired(100, 10), [])

    def test_lifecycle_distinguishes_parallel_rows_and_theory_names(self):
        tracker = op.RowProgress()
        def message(uid, name, state):
            return (f'TestNodeUpdateMessage {{ TestNode = TestNode {{ Uid = TestNodeUid {{ Value = {uid} }}, '
                    f'DisplayName = {name}, Properties = [ {state}TestNodeStateProperty {{ Explanation =  }} ] }} }}')
        name = 'A.Theory(x: 1, role: "first-player")'
        self.assertEqual(tracker.observe(message('a', name, 'InProgress'), 10), f'START {name}')
        self.assertEqual(tracker.observe(message('b', 'B.Other', 'InProgress'), 20), 'START B.Other')
        self.assertEqual(tracker.observe(message('a', name, 'Passed'), 25), f'FINISH {name} (15.0s, Passed)')
        self.assertEqual(tracker.expired(31, 10), ['B.Other'])
        self.assertEqual(tracker.observe(message('b', 'B.Other', 'Failed'), 31), 'FINISH B.Other (11.0s, Failed)')
        self.assertEqual(tracker.expired(100, 10), [])

    def test_two_stalls_without_report_fail(self):
        calls = []
        def runner(cmd, log):
            calls.append(cmd); return 124
        self.assertEqual(self.run_execute(runner), 124)
        self.assertEqual(len(calls), 2)

    def test_stall_then_success_retries_once(self):
        counts = op.inventory(self.listing)
        calls = []
        def runner(cmd, log):
            calls.append(cmd)
            if len(calls) == 1:
                return 124
            (self.out / 'r.xml').write_text(xml(rows_for(counts))); return 0
        self.assertEqual(self.run_execute(runner), 0)
        self.assertEqual(len(calls), 2)

    def test_real_failure_with_report_is_not_retried(self):
        calls = []
        def runner(cmd, log):
            calls.append(cmd); (self.out / 'r.xml').write_text(xml([], failed=0)); return 2
        self.assertEqual(self.run_execute(runner), 2)
        self.assertEqual(len(calls), 1)

    def test_exit_zero_without_matching_report_fails(self):
        def runner(cmd, log):
            return 0
        with self.assertRaises(ValueError):
            self.run_execute(runner)


if __name__ == '__main__':
    unittest.main()
