"""Lightweight real subprocess checks; no .NET workload is started."""
import importlib.util
import json
import os
import signal
import subprocess
from pathlib import Path
import sys
import tempfile
import time
import threading
import unittest
from unittest import mock
import r02_partition

spec = importlib.util.spec_from_file_location('ci_group_runner', Path(__file__).with_name('ci_group_runner.py'))
runner = importlib.util.module_from_spec(spec); spec.loader.exec_module(runner)


class ChildProcessTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(); self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name) / 'child'
        self.template = {'group': 'paid-axis-m0', 'exit_code': 'must be replaced'}

    def test_nonzero_exit_is_retained_with_command_and_timing(self):
        command = [sys.executable, '-c', 'import sys; print("diagnostic"); sys.exit(7)']
        status = runner.run_child(command, self.root, self.template, time.monotonic() + 5)
        self.assertEqual(status['exit_code'], 7)
        self.assertEqual(status['command'], command)
        self.assertEqual(json.loads((self.root / 'status.json').read_text()), status)
        self.assertIn('diagnostic', (self.root / 'execution.log').read_text())
        self.assertGreater(status['process_id'], 0)
        self.assertGreaterEqual(status['elapsed_seconds'], 0)
        self.assertFalse(status['cancelled'])

    def test_deadline_terminates_owned_process_and_cannot_pass(self):
        status = runner.run_child([sys.executable, '-c', 'import time; time.sleep(30)'], self.root, self.template, time.monotonic() + 0.15)
        self.assertNotEqual(status['exit_code'], 0)
        self.assertTrue(status['cancelled'])
        self.assertLess(status['elapsed_seconds'], 5)

    def test_parent_exit_cannot_leave_a_live_owned_descendant(self):
        pid_file = Path(self.tmp.name) / 'descendant.pid'
        child = 'import time; time.sleep(30)'
        command = [sys.executable, '-c', 'import pathlib,subprocess,sys; p=subprocess.Popen([sys.executable,"-c",' + repr(child) + ']); pathlib.Path(' + repr(str(pid_file)) + ').write_text(str(p.pid))']
        status = runner.run_child(command, self.root, self.template, time.monotonic() + 5)
        pid = int(pid_file.read_text())
        def cleanup():
            try: os.kill(pid, signal.SIGKILL)
            except ProcessLookupError: pass
        self.addCleanup(cleanup)
        output = subprocess.run(['ps', '-o', 'stat=', '-p', str(pid)], capture_output=True, text=True).stdout.strip()
        self.assertTrue(not output or output.startswith('Z'), 'owned descendant survived')
        self.assertNotEqual(status['exit_code'], 0)
        self.assertTrue(status['cancelled'])

    def test_cleanup_inspection_failure_cannot_retain_successful_exit(self):
        for index, error in enumerate([OSError('process inspection unavailable'), subprocess.CalledProcessError(1, ['ps'])]):
            cancellation = threading.Event()
            with self.subTest(error=type(error).__name__), mock.patch.object(runner, 'owned_session_members', side_effect=error):
                status = runner.run_child([sys.executable, '-c', 'pass'], self.root / str(index), self.template, time.monotonic() + 5, cancellation)
            self.assertTrue(cancellation.is_set())
            self.assertNotEqual(status['exit_code'], 0)
            self.assertTrue(status['cancelled'])

    def test_live_child_is_killed_when_cleanup_inspection_fails(self):
        created = []
        original = subprocess.Popen
        def launch(*args, **kwargs):
            process = original(*args, **kwargs); created.append(process); return process
        def cleanup():
            for process in created:
                if process.poll() is None:
                    os.killpg(process.pid, signal.SIGKILL); process.wait(timeout=2)
        self.addCleanup(cleanup)
        with mock.patch.object(runner.subprocess, 'Popen', side_effect=launch), mock.patch.object(runner, 'owned_session_members', side_effect=OSError('inspection failed')):
            status = runner.run_child([sys.executable, '-c', 'import time; time.sleep(30)'], self.root, self.template, time.monotonic() + 0.15)
        self.assertTrue(status['cancelled'])
        self.assertNotEqual(status['exit_code'], 0)
        self.assertIsNotNone(created[0].poll())
        self.assertIn('cleanup_error', status)
        self.assertTrue((self.root / 'status.json').exists())

    def test_stale_output_directory_rejects_before_launch(self):
        self.root.mkdir()
        with self.assertRaises(ValueError): runner.run_child([sys.executable, '-c', 'pass'], self.root, self.template, time.monotonic() + 5)
        self.assertFalse((self.root / 'status.json').exists())

    def test_exact_class_command_has_no_build_or_shared_results(self):
        binary = Path(self.tmp.name).resolve() / 'Cna.Core.Tests.dll'
        binary.write_bytes(b'command-only fixture')
        command = runner.execution_command(binary, self.root, 'Cna.Core.Tests.Cases.One')
        self.assertEqual(command[:3], ['dotnet', 'exec', str(binary)])
        self.assertEqual(command[-2:], ['--filter-class', 'Cna.Core.Tests.Cases.One'])
        self.assertIn(str((self.root / 'results').resolve()), command)
        self.assertNotIn('--project', command)
        self.assertNotIn('--no-build', command)
        self.assertFalse(any(arg.startswith('/bl:') for arg in command))

    def test_command_uses_real_fixture_targets_through_directory_aliases(self):
        # The expected targets exist independently before aliases are created.
        real_root = Path(self.tmp.name).resolve()
        binary_directory = real_root / 'real-bin'; binary_directory.mkdir()
        binary = binary_directory / 'Cna.Core.Tests.dll'; binary.write_bytes(b'command-only fixture')
        output = real_root / 'real-output'; output.mkdir()
        binary_alias = real_root / 'binary-alias'; binary_alias.symlink_to(binary_directory, target_is_directory=True)
        output_alias = real_root / 'output-alias'; output_alias.symlink_to(output, target_is_directory=True)
        command = runner.execution_command(binary_alias / binary.name, output_alias, 'Cna.Core.Tests.Cases.One')
        self.assertEqual(command[:3], ['dotnet', 'exec', str(binary)])
        self.assertEqual(command[command.index('--results-directory') + 1], str(output / 'results'))
        self.assertEqual(command[command.index('--diagnostic-output-directory') + 1], str(output / 'diagnostics'))
        self.assertNotIn(str(binary_alias / binary.name), command)
        self.assertNotIn(str(output_alias / 'results'), command)
        self.assertEqual(command[-2:], ['--filter-class', 'Cna.Core.Tests.Cases.One'])


class ResourcePolicyTests(unittest.TestCase):
    def snapshot(self):
        return {'platform': 'Linux', 'effective_memory_limit_bytes': 16 * 2**30, 'memory_available_bytes': 12 * 2**30, 'effective_cpu_count': 4}

    def test_preflight_rejects_effective_limit_available_memory_or_cpu_shortage(self):
        self.assertTrue(runner.validate_preflight(self.snapshot()))
        for field, value in [('effective_memory_limit_bytes', 8 * 2**30), ('memory_available_bytes', 7 * 2**30), ('effective_cpu_count', 2), ('effective_cpu_count', float('inf')), ('effective_memory_limit_bytes', float('inf'))]:
            data = self.snapshot(); data[field] = value
            with self.subTest(field=field), self.assertRaises(ValueError): runner.validate_preflight(data)

    def test_runtime_group_aggregate_and_host_reserve_are_fail_closed(self):
        self.assertTrue(runner.validate_sample(self.snapshot(), {'a': 2**30}))
        for snapshot, groups in [(self.snapshot(), {'a': 4 * 2**30 + 1}), (self.snapshot(), {'a': 3 * 2**30, 'b': 3 * 2**30, 'c': 3 * 2**30}), (dict(self.snapshot(), memory_available_bytes=3 * 2**30), {'a': 1})]:
            with self.assertRaises(ValueError): runner.validate_sample(snapshot, groups)

    def test_linux_uses_tightest_ancestor_cgroup_limit_and_remaining_memory(self):
        with tempfile.TemporaryDirectory() as root:
            root = Path(root); proc = root / 'proc'; cg = root / 'cgroup'; (proc / 'self').mkdir(parents=True); (cg / 'job').mkdir(parents=True)
            (proc / 'meminfo').write_text('MemTotal: 16777216 kB\nMemAvailable: 12582912 kB\n')
            (proc / 'self' / 'cgroup').write_text('0::/job\n')
            for directory, limit, used in [(cg, 12 * 2**30, 2**30), (cg / 'job', 10 * 2**30, 4 * 2**30)]:
                (directory / 'memory.max').write_text(str(limit)); (directory / 'memory.current').write_text(str(used)); (directory / 'cpu.max').write_text('400000 100000')
            result = runner.linux_resources(proc, cg)
            self.assertEqual(result['effective_memory_limit_bytes'], 10 * 2**30)
            self.assertEqual(result['memory_available_bytes'], 6 * 2**30)

    def test_kernel_peak_units_are_explicit_and_overshoot_fails(self):
        self.assertEqual(runner.kernel_peak_bytes(1024, 'Linux'), 2**20)
        self.assertEqual(runner.kernel_peak_bytes(1024, 'Darwin'), 1024)
        with self.assertRaises(ValueError): runner.validate_kernel_peak(4 * 2**30 + 1)


class ResourceMonitorTests(unittest.TestCase):
    def test_group_overshoot_cancels_and_preserves_resource_failure(self):
        cancellation = threading.Event()
        snapshot = ResourcePolicyTests().snapshot()
        with mock.patch.object(runner, 'resource_snapshot', return_value=snapshot):
            guard = runner.ResourceGuard(cancellation, 'r02-1', {})
            guard.preflight()
            guard.register('support-contract', 123)
            with mock.patch.object(runner, 'session_rss', return_value={'support-contract': 4 * 2**30 + 1}): guard.sample_once()
        self.assertTrue(cancellation.is_set())
        self.assertIn('4GiB', guard.evidence['abort_reason'])
        self.assertGreater(guard.evidence['peak_group_rss_bytes']['support-contract'], 4 * 2**30)


class LaneSupervisorTests(unittest.TestCase):
    def setUp(self):
        snapshot = dict(ResourcePolicyTests().snapshot(), platform='Darwin')
        patch = mock.patch.object(runner, 'resource_snapshot', return_value=snapshot)
        patch.start(); self.addCleanup(patch.stop)

    def test_static_four_lane_job_runs_each_group_once(self):
        allocation = r02_partition.candidate_allocation()
        groups = {name: {'class': 'Cna.Core.Tests.Cases.' + name.replace('-', '_')} for name in allocation}
        with tempfile.TemporaryDirectory() as root:
            results = runner.run_job(groups, allocation, 'r02-1', Path(root) / 'groups', {}, '/bin/not-used.dll', 5,
                command_builder=lambda binary, directory, class_name: [sys.executable, '-c', 'pass'])
            expected = {name for name, entry in allocation.items() if entry['job'] == 'r02-1'}
            self.assertEqual(set(results), expected)
            self.assertEqual(len({status['process_id'] for status in results.values()}), 5)
            self.assertTrue(all(status['exit_code'] == 0 for status in results.values()))
            self.assertEqual({path.name for path in (Path(root) / 'groups').iterdir()}, expected)

    def test_manifest_object_order_cannot_reverse_frozen_lane_sequence(self):
        allocation = dict(reversed(list(r02_partition.candidate_allocation().items())))
        groups = {name: {'class': 'Cna.Core.Tests.Cases.One'} for name in allocation}
        with tempfile.TemporaryDirectory() as root:
            results = runner.run_job(groups, allocation, 'r02-2', Path(root) / 'groups', {}, '/bin/not-used.dll', 5,
                command_builder=lambda binary, directory, class_name: [sys.executable, '-c', 'pass'])
            self.assertLess(results['armed-axis-m1']['started_utc'], results['paid-axis-m0']['started_utc'])
            self.assertLess(results['armed-axis-m2']['started_utc'], results['paid-commonwealth-m0']['started_utc'])

    def test_heavy_support_starts_after_every_parallel_child_drains(self):
        allocation = r02_partition.candidate_allocation()
        groups = {name: {'class': 'Cna.Core.Tests.Cases.One'} for name in allocation}
        with tempfile.TemporaryDirectory() as root:
            results = runner.run_job(groups, allocation, 'r02-1', Path(root) / 'groups', {}, '/bin/not-used.dll', 5,
                command_builder=lambda binary, directory, class_name: [sys.executable, '-c', 'pass'])
            self.assertIn('support-contract', results)
            latest = max(status['completed_utc'] for group, status in results.items() if group != 'support-contract')
            self.assertGreaterEqual(results['support-contract']['started_utc'], latest)

    def test_failed_group_does_not_silently_skip_other_expected_groups(self):
        allocation = r02_partition.candidate_allocation()
        groups = {name: {'class': 'Cna.Core.Tests.Cases.One'} for name in allocation}
        with tempfile.TemporaryDirectory() as root:
            results = runner.run_job(groups, allocation, 'r02-1', Path(root) / 'groups', {}, '/bin/not-used.dll', 5,
                command_builder=lambda binary, directory, class_name: [sys.executable, '-c', 'raise SystemExit(3)'])
            self.assertEqual(len(results), 5)
            self.assertTrue(all(status['exit_code'] == 3 for status in results.values()))


if __name__ == '__main__': unittest.main()
