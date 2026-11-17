"""Guard concurrency bounding, job planning and outlier exclusion without
starting real `dotnet test` workloads."""
from pathlib import Path
import tempfile
import threading
import time
import unittest
from unittest import mock
from unittest.mock import patch
import sys

sys.dont_write_bytecode = True
import local_parallel_check as check


class ConcurrencyLimitTests(unittest.TestCase):
    def test_bounded_by_cpu_when_memory_is_plentiful(self):
        snapshot = {'effective_cpu_count': 4, 'memory_available_bytes': 100 * check.GIB}
        self.assertEqual(3, check.concurrency_limit(snapshot, 20))

    def test_bounded_by_memory_when_cpu_is_plentiful(self):
        snapshot = {'effective_cpu_count': 32, 'memory_available_bytes': 10 * check.GIB}
        # (10 - 4) GiB reserve headroom / 3 GiB per job == 2.
        self.assertEqual(2, check.concurrency_limit(snapshot, 20))

    def test_never_exceeds_job_count(self):
        snapshot = {'effective_cpu_count': 32, 'memory_available_bytes': 100 * check.GIB}
        self.assertEqual(2, check.concurrency_limit(snapshot, 2))

    def test_floor_is_one_even_under_severe_pressure(self):
        for snapshot in [{'effective_cpu_count': 1, 'memory_available_bytes': 4 * check.GIB},
                          {'effective_cpu_count': 8, 'memory_available_bytes': 0}]:
            with self.subTest(snapshot=snapshot):
                self.assertEqual(1, check.concurrency_limit(snapshot, 20))


class HostCommandTests(unittest.TestCase):
    def test_excludes_known_slow_outlier_by_exact_display_name(self):
        command = check.host_command()
        self.assertEqual('Cna.OrleansHost.Tests.GrazianiPregameJournalTests.'
                          'FinalizedRealSupplyPlanSurvivesFreshHostReplayWithoutStartingTheGame',
                          check.ORLEANS_OUTLIER)
        self.assertIn('tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj', command)
        self.assertEqual(['--filter-not-display-name', check.ORLEANS_OUTLIER], command[-2:])


class PlanJobsTests(unittest.TestCase):
    def test_includes_every_shard_suite_and_host_as_distinct_jobs(self):
        def fake_commands_for(suite, configuration):
            if suite == 'exercise':
                return [['dotnet', 'test', 'exercise']]
            if suite == 'contracts':
                return [['dotnet', 'test', 'contracts-a'], ['dotnet', 'test', 'contracts-b']]
            return [['dotnet', 'test', suite]]
        with patch.object(check.local, 'shard_commands', return_value=[('g1', ['dotnet', 'test', 'shard'])]), \
             patch.object(check.local, 'commands_for', side_effect=fake_commands_for):
            jobs = check.plan_jobs({'legacy_core_rows': []}, {})
        names = [name for name, _ in jobs]
        expected_shards = len(check.suites.LONG_JOBS) + len(check.suites.FAST_JOBS) + 3
        self.assertEqual(expected_shards, len([name for name in names if ':' in name]))
        self.assertIn('exercise', names)
        self.assertIn('contracts', names)
        self.assertIn('contracts-1', names)
        self.assertIn('host', names)
        self.assertEqual(len(names), len(set(names)), 'job names must be unique')

    def test_every_shard_job_keeps_its_own_group_name_distinct(self):
        with patch.object(check.local, 'shard_commands', return_value=[('a', ['dotnet']), ('b', ['dotnet'])]), \
             patch.object(check.local, 'commands_for', return_value=[['dotnet']]):
            jobs = check.plan_jobs({'legacy_core_rows': []}, {})
        shard_names = {name for name, _ in jobs if ':' in name}
        self.assertIn('fast-01:a', shard_names)
        self.assertIn('fast-01:b', shard_names)


class RunJobsTests(unittest.TestCase):
    def test_never_exceeds_the_worker_bound(self):
        active, peak = [], []
        lock = threading.Lock()

        def fake_run_one(name, command, directory):
            with lock:
                active.append(name)
                peak.append(len(active))
            time.sleep(0.05)
            with lock:
                active.remove(name)
            return {'name': name, 'command': command, 'exit_code': 0, 'elapsed_seconds': 0.05, 'log': ''}

        jobs = [('job-%d' % index, ['noop']) for index in range(10)]
        with tempfile.TemporaryDirectory() as directory, \
             mock.patch.object(check.runner, 'resource_snapshot',
                                return_value={'memory_available_bytes': 100 * check.GIB}):
            results = check.run_jobs(jobs, 3, Path(directory), run_one_func=fake_run_one)
        self.assertEqual(10, len(results))
        self.assertLessEqual(max(peak), 3)
        self.assertTrue(all(status['exit_code'] == 0 for status in results.values()))

    def test_failed_job_does_not_stop_the_others(self):
        def fake_run_one(name, command, directory):
            return {'name': name, 'command': command, 'exit_code': 1 if name == 'bad' else 0,
                    'elapsed_seconds': 0, 'log': ''}

        jobs = [('good-1', ['noop']), ('bad', ['noop']), ('good-2', ['noop'])]
        with tempfile.TemporaryDirectory() as directory, \
             mock.patch.object(check.runner, 'resource_snapshot',
                                return_value={'memory_available_bytes': 100 * check.GIB}):
            results = check.run_jobs(jobs, 2, Path(directory), run_one_func=fake_run_one)
        self.assertEqual(3, len(results))
        self.assertEqual(1, results['bad']['exit_code'])
        self.assertEqual(0, results['good-1']['exit_code'])
        self.assertEqual(0, results['good-2']['exit_code'])

    def test_resource_pressure_pauses_new_launches_until_it_clears(self):
        calls = []
        state = {'low': True}

        def snapshot():
            available = 1 * check.GIB if state['low'] else 8 * check.GIB
            return {'memory_available_bytes': available}

        def fake_run_one(name, command, directory):
            calls.append(name)
            return {'name': name, 'command': command, 'exit_code': 0, 'elapsed_seconds': 0, 'log': ''}

        def clear_pressure():
            time.sleep(0.3)
            state['low'] = False

        threading.Thread(target=clear_pressure, daemon=True).start()
        with tempfile.TemporaryDirectory() as directory, \
             mock.patch.object(check.runner, 'resource_snapshot', side_effect=snapshot):
            started = time.monotonic()
            results = check.run_jobs([('a', ['noop'])], 1, Path(directory), run_one_func=fake_run_one, poll_seconds=0.1)
            elapsed = time.monotonic() - started
        self.assertEqual(['a'], calls)
        self.assertEqual(0, results['a']['exit_code'])
        self.assertGreaterEqual(elapsed, 0.25)


class RunOneTests(unittest.TestCase):
    def test_writes_log_and_results_directory_and_reports_real_exit_code(self):
        with tempfile.TemporaryDirectory() as directory:
            directory = Path(directory)
            outcome = check.run_one('demo', [sys.executable, '-c', 'import sys; print("hi"); sys.exit(3)'], directory)
            self.assertEqual(3, outcome['exit_code'])
            self.assertEqual(['--report-xunit-xml', '--results-directory', str(directory / 'demo' / 'results')],
                              outcome['command'][-3:])
            self.assertIn('hi', Path(outcome['log']).read_text())


if __name__ == '__main__':
    unittest.main()
