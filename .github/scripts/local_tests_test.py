"""Guard local selection and failure reporting without running simulation suites."""
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch
import subprocess
import sys

sys.dont_write_bytecode = True
import local_tests as local


class LocalSelectionTests(unittest.TestCase):
    def test_contracts_include_both_assemblies(self):
        commands = local.commands_for('contracts', 'Release')
        self.assertEqual(2, len(commands))
        self.assertIn('tests/Cna.Game.Contracts.Tests/Cna.Game.Contracts.Tests.csproj', commands[0])
        self.assertIn('tests/Cna.Intelligence.Contracts.Tests/Cna.Intelligence.Contracts.Tests.csproj', commands[1])

    def test_quoted_pattern_is_single_argument_and_no_solution_run(self):
        pattern = '*InvalidForce*; echo unsafe'
        command = local.commands_for('host', 'Release', method_filter=pattern)[0]
        self.assertEqual(['--filter-method', pattern], command[-2:])
        self.assertNotIn('--solution', command)

    def test_boundary_is_not_full_core(self):
        self.assertEqual(['--filter-trait', 'Boundary=UserSpace'], local.commands_for('boundary', 'Release')[0][-2:])

    def test_shard_uses_exact_ci_display_rows_and_classes(self):
        groups = {
            'rows': {'job': 'long-01', 'selector_kind': 'display', 'rows': [['Cna.Core.Tests', 'Cna.Core.Tests.Example.Test', 'Example(2)']]},
            'classes': {'job': 'fast-01', 'selector_kind': 'classes', 'rows': [['Cna.Core.Tests', 'Cna.Core.Tests.Example.Test', 'Example(2)']]},
        }
        with patch.object(local.suites, 'make_plan', return_value={'groups': groups}):
            selected = local.shard_commands('long-01', {'legacy_core_rows': []}, {})
            self.assertEqual(1, len(selected))
            self.assertEqual(['--filter-display-name', 'Example(2)'], selected[0][1][-2:])
            selected = local.shard_commands('fast-01', {'legacy_core_rows': []}, {})
            self.assertEqual(['--filter-class', 'Cna.Core.Tests.Example'], selected[0][1][-2:])

    def test_r02_preserves_exclusive_group_and_excludes_other_jobs(self):
        proof = {'groups': {'support': {'job': 'r02-1', 'class': 'Support', 'phase': 'exclusive'},
                            'other': {'job': 'r02-2', 'class': 'Other'}}}
        selected = local.shard_commands('r02-1', proof, {})
        self.assertEqual(1, len(selected))
        self.assertEqual(['--filter-class', 'Support'], selected[0][1][-2:])
        with self.assertRaises(ValueError):
            local.shard_commands('r02-3', proof, {})

    def test_failed_test_stops_and_receipt_does_not_claim_success(self):
        with tempfile.TemporaryDirectory() as directory, patch.object(local, 'ROOT', Path(directory)), \
             patch.object(local.subprocess, 'run', return_value=subprocess.CompletedProcess([], 8)) as run:
            self.assertEqual(1, local.main(['contracts', '--no-build']))
            self.assertEqual(1, run.call_count)
            receipt = local.json.loads(next(Path(directory).glob('artifacts/local-tests/*/run.json')).read_text())
            self.assertEqual('failed', receipt['status'])
            self.assertEqual(8, receipt['commands'][0]['exit_code'])

    def test_build_failure_never_launches_tests(self):
        with tempfile.TemporaryDirectory() as directory, patch.object(local, 'ROOT', Path(directory)), \
             patch.object(local.subprocess, 'run', side_effect=subprocess.CalledProcessError(1, ['dotnet', 'build'])) as run:
            self.assertEqual(1, local.main(['host']))
            self.assertEqual(1, run.call_count)
            self.assertEqual('build', run.call_args.args[0][1])

    def test_project_dry_run_never_launches_process(self):
        with patch.object(local.subprocess, 'run') as run:
            self.assertEqual(0, local.main(['host', '--dry-run']))
            run.assert_not_called()

    def test_shard_rejects_debug_or_extra_filter_before_execution(self):
        for extra in (['--configuration', 'Debug'], ['--method', '*']):
            with patch.object(local.subprocess, 'run') as run, self.assertRaises(SystemExit) as error:
                local.main(['fast-01', *extra])
            self.assertEqual(2, error.exception.code)
            run.assert_not_called()


if __name__ == '__main__':
    unittest.main()
