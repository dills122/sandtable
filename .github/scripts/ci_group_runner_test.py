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

    def test_stale_output_directory_rejects_before_launch(self):
        self.root.mkdir()
        with self.assertRaises(ValueError): runner.run_child([sys.executable, '-c', 'pass'], self.root, self.template, time.monotonic() + 5)
        self.assertFalse((self.root / 'status.json').exists())

    def test_exact_class_command_has_no_build_or_shared_results(self):
        command = runner.execution_command('/bin/Cna.Core.Tests.dll', self.root, 'Cna.Core.Tests.Cases.One')
        self.assertEqual(command[:3], ['dotnet', 'exec', '/bin/Cna.Core.Tests.dll'])
        self.assertEqual(command[-2:], ['--filter-class', 'Cna.Core.Tests.Cases.One'])
        self.assertIn(str((self.root / 'results').resolve()), command)
        self.assertNotIn('--project', command)
        self.assertNotIn('--no-build', command)
        self.assertFalse(any(arg.startswith('/bl:') for arg in command))


class LaneSupervisorTests(unittest.TestCase):
    def test_static_four_lane_job_runs_each_group_once(self):
        allocation = r02_partition.candidate_allocation()
        groups = {name: {'class': 'Cna.Core.Tests.Cases.' + name.replace('-', '_')} for name in allocation}
        with tempfile.TemporaryDirectory() as root:
            results = runner.run_job(groups, allocation, 'r02-1', Path(root) / 'groups', {}, '/bin/not-used.dll', 5,
                command_builder=lambda binary, directory, class_name: [sys.executable, '-c', 'pass'])
            expected = {name for name, entry in allocation.items() if entry['job'] == 'r02-1'}
            self.assertEqual(set(results), expected)
            self.assertEqual(len({status['process_id'] for status in results.values()}), 4)
            self.assertTrue(all(status['exit_code'] == 0 for status in results.values()))
            self.assertEqual({path.name for path in (Path(root) / 'groups').iterdir()}, expected)

    def test_failed_group_does_not_silently_skip_other_expected_groups(self):
        allocation = r02_partition.candidate_allocation()
        groups = {name: {'class': 'Cna.Core.Tests.Cases.One'} for name in allocation}
        with tempfile.TemporaryDirectory() as root:
            results = runner.run_job(groups, allocation, 'r02-1', Path(root) / 'groups', {}, '/bin/not-used.dll', 5,
                command_builder=lambda binary, directory, class_name: [sys.executable, '-c', 'raise SystemExit(3)'])
            self.assertEqual(len(results), 4)
            self.assertTrue(all(status['exit_code'] == 3 for status in results.values()))


if __name__ == '__main__': unittest.main()
