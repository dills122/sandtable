import copy
import json
from pathlib import Path
import tempfile
import unittest

import long_suite_partition as suites


class AllocationTests(unittest.TestCase):
    def setUp(self):
        self.rows = []
        self.weights = {'schema_version': 1, 'classes': {}, 'rows': {}}
        for number in range(16):
            name = f'Cna.Core.Tests.Campaigns.Example{number:02}Tests'
            count = 8 if number < 8 else 2
            parts = 4 if number < 8 else 1
            self.weights['classes'][name] = {'parts': parts}
            for index in range(count):
                method = name + '.RetainsEveryCut'
                row = ['Cna.Core.Tests', method, method + f'(index: {index})']
                self.rows.append(row)
                self.weights['rows'][suites.row_key(row)] = 60000 if number < 8 else 1000

    def test_real_rows_are_exhaustive_disjoint_and_process_isolated(self):
        plan = suites.make_plan(self.rows, self.weights)
        actual = [tuple(row) for group in plan['groups'].values() for row in group['rows']]
        self.assertCountEqual(actual, [tuple(row) for row in self.rows])
        self.assertEqual(len(actual), len(set(actual)))
        self.assertEqual(36, len(plan['groups']))
        self.assertEqual({f'long-{i:02}' for i in range(1, 9)} | {f'fast-{i:02}' for i in range(1, 5)}, {group['job'] for group in plan['groups'].values()})
        for group in plan['groups'].values():
            self.assertIn(group['lane'], range(1, 5))
            self.assertEqual('parallel', group['phase'])
            if group['job'].startswith('long-'):
                self.assertEqual(1, len({row[1].rsplit('.', 1)[0] for row in group['rows']}))
                self.assertEqual('display', group['selector_kind'])
            else:
                self.assertEqual('classes', group['selector_kind'])

    def test_prepared_json_roundtrip_preserves_exact_allocation(self):
        plan = suites.make_plan(self.rows, self.weights)
        retained = json.loads(json.dumps(plan))
        self.assertEqual(retained, suites.validate_plan(retained, self.rows, self.weights))

    def test_order_independent_allocation(self):
        self.assertEqual(suites.make_plan(self.rows, self.weights), suites.make_plan(list(reversed(self.rows)), self.weights))

    def test_unknown_missing_duplicate_and_foreign_rows_fail_closed(self):
        variants = [self.rows[:-1], self.rows + [self.rows[0]], self.rows + [['Cna.Core.Tests', 'Cna.Core.Tests.New.Tests', 'Cna.Core.Tests.New.Tests']], self.rows + [['Foreign', 'Foreign.A.B.C', 'Foreign.A.B.C']]]
        for rows in variants:
            with self.subTest(rows=len(rows)), self.assertRaises(ValueError):
                suites.make_plan(rows, self.weights)

    def test_wildcard_and_casefold_ambiguity_fail_closed(self):
        for replacement in ('*', self.rows[0][2].upper()):
            rows = copy.deepcopy(self.rows); rows[-1][2] = replacement
            with self.subTest(replacement=replacement), self.assertRaises(ValueError):
                suites.make_plan(rows, self.weights)

    def test_bad_or_unmatched_timing_manifest_fails_closed(self):
        for field,value in (('schema_version', 2), ('schema_version', True), ('rows', {}), ('classes', {})):
            bad = copy.deepcopy(self.weights); bad[field] = value
            with self.subTest(field=field), self.assertRaises(ValueError):
                suites.make_plan(self.rows, bad)

    def test_prepared_plan_reconstruction_rejects_mapping_changes(self):
        plan = suites.make_plan(self.rows, self.weights)
        suites.validate_plan(plan, self.rows, self.weights)
        bad = copy.deepcopy(plan); next(iter(bad['groups'].values()))['lane'] = 5
        with self.assertRaises(ValueError): suites.validate_plan(bad, self.rows, self.weights)

    def test_exact_display_command_is_literal_and_preserves_diagnostics(self):
        plan = suites.make_plan(self.rows, self.weights)
        entry = next(value for value in plan['groups'].values() if value['selector_kind'] == 'display')
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary) / 'long-group'
            command = suites.execution_command(Path(temporary) / 'Cna.Core.Tests.dll', directory, entry)
            self.assertEqual(sorted(row[2] for row in entry['rows']), command[command.index('--filter-display-name') + 1:])
            for flag in ('--diagnostic', '--diagnostic-synchronous-write', '--report-xunit-xml', '--results-directory'):
                self.assertIn(flag, command)
            self.assertNotIn('build', command)
            self.assertNotIn('all', command)



class EvidenceTests(unittest.TestCase):
    def test_exact_quoted_selector_and_explicit_collection_bound(self):
        method = 'Cna.Core.Tests.Campaigns.ExampleTests.EveryClaim'
        name = method + '(json: "{\\"kind\\":\\"idle\\"}")'
        entry = {'selector_kind': 'display', 'rows': [['Cna.Core.Tests', method, name]]}
        with tempfile.TemporaryDirectory() as temporary:
            command = suites.execution_command(Path(temporary) / 'Cna.Core.Tests.dll', Path(temporary) / 'group', entry)
            self.assertEqual([name], command[command.index('--filter-display-name') + 1:])
            self.assertEqual('collections', command[command.index('--parallel') + 1])
            self.assertEqual('4', command[command.index('--max-threads') + 1])

    def test_formatting_remains_in_required_dependency_closure(self):
        good = {key: {'result': 'success'} for key in ('prepare', 'tests', 'format', 'orleans')}
        suites.validate_dependencies(good)
        for key in good:
            for result in ('failure', 'cancelled', 'skipped'):
                bad = copy.deepcopy(good); bad[key]['result'] = result
                with self.subTest(key=key, result=result), self.assertRaises(ValueError): suites.validate_dependencies(bad)
        for missing in ('format', 'orleans'):
            with self.subTest(missing=missing), self.assertRaises(ValueError):
                suites.validate_dependencies({key: value for key, value in good.items() if key != missing})


import r02_partition_test as prior_tests


class LongProcessEvidenceTests(prior_tests.ChildEvidenceTests):
    def good_status(self):
        status = super().good_status()
        entry = dict(self.entry, selector_kind='display')
        status.update(module_path=str(Path(status['module_path']).resolve()), suite_plan_sha256='d' * 64, class_names=[self.entry['class']], command=suites.execution_command(status['module_path'], self.directory, entry))
        return status

    def check(self):
        entry = dict(self.entry, selector_kind='display')
        return suites.verify_process(self.group, entry, self.directory, self.binding, 'd' * 64)

    def test_wrong_plan_or_class_set_cannot_pass_good_xml(self):
        for field, value in [('suite_plan_sha256', 'e' * 64), ('class_names', []), ('phase', 'exclusive')]:
            self.status = self.good_status(); self.status[field] = value; self.write_status()
            with self.subTest(field=field), self.assertRaises(ValueError): self.check()



class SupervisorTests(unittest.TestCase):
    setUp = AllocationTests.setUp

    def test_bounded_four_lanes_execute_each_selected_group_once(self):
        import sys
        from unittest import mock
        import ci_group_runner
        snapshot = {'platform': 'Darwin', 'physical_memory_bytes': 16 * 2**30, 'effective_memory_limit_bytes': 16 * 2**30, 'memory_available_bytes': 12 * 2**30, 'effective_cpu_count': 4}
        plan = suites.make_plan(self.rows, self.weights)
        with tempfile.TemporaryDirectory() as temporary, mock.patch.object(ci_group_runner, 'resource_snapshot', return_value=snapshot):
            root = Path(temporary) / 'groups'
            statuses = suites.run_suite_job(plan, 'long-01', root, {}, '/bin/not-used.dll', 5, command_builder=lambda *_: [sys.executable, '-c', 'pass'])
            expected = {name for name, entry in plan['groups'].items() if entry['job'] == 'long-01'}
            self.assertEqual(expected, set(statuses))
            self.assertEqual(len(expected), len({status['process_id'] for status in statuses.values()}))
            self.assertTrue(all(status['exit_code'] == 0 for status in statuses.values()))
            self.assertTrue((root.parent / 'long-01-resource.json').exists())

    def test_failure_and_stale_outputs_fail_closed(self):
        import sys
        from unittest import mock
        import ci_group_runner
        snapshot = {'platform': 'Darwin', 'physical_memory_bytes': 16 * 2**30, 'effective_memory_limit_bytes': 16 * 2**30, 'memory_available_bytes': 12 * 2**30, 'effective_cpu_count': 4}
        plan = suites.make_plan(self.rows, self.weights)
        with tempfile.TemporaryDirectory() as temporary, mock.patch.object(ci_group_runner, 'resource_snapshot', return_value=snapshot):
            root = Path(temporary) / 'groups'
            with self.assertRaises(ValueError):
                suites.run_suite_job(plan, 'long-01', root, {}, '/bin/not-used.dll', 5, command_builder=lambda *_: [sys.executable, '-c', 'raise SystemExit(3)'])
            self.assertTrue((root.parent / 'long-01-resource.json').exists())
            with self.assertRaises(ValueError): suites.run_suite_job(plan, 'long-01', root, {}, '/bin/not-used.dll', 5)





class CombinedSuiteTests(prior_tests.CombinedConservationTests):
    def setUp(self):
        super().setUp()
        import shutil
        import r02_partition as r02
        from ci_group_runner import RESOURCE_POLICY
        self.needs['format'] = {'result': 'success'}
        self.needs['orleans'] = {'result': 'success'}
        core = self.proof['legacy_core_rows']
        classes = sorted({row[1].rsplit('.', 1)[0] for row in core})
        self.weights = {'schema_version': 1, 'classes': {}, 'rows': {suites.row_key(row): 1000 for row in core}}
        for name in classes:
            count = sum(row[1].rsplit('.', 1)[0] == name for row in core)
            self.weights['classes'][name] = {'parts': 1 if name in classes[:4] else min(2, count)}
        self.suite_proof = suites.make_suite_proof(self.proof, self.weights)
        plan = self.suite_proof['plan']
        for shard in ('core-a', 'core-b', 'core-c', 'core-d'): shutil.rmtree(self.root / shard)
        for index, (group, entry) in enumerate(plan['groups'].items()):
            directory = self.root / 'long-groups' / group; directory.mkdir(parents=True)
            self.report(directory, 'Cna.Core.Tests', entry['rows'])
            names = sorted({row[1].rsplit('.', 1)[0] for row in entry['rows']})
            status = dict(self.binding, group=group, module='Cna.Core.Tests', class_name=names[0] if len(names) == 1 else None, class_names=names, suite_plan_sha256=suites.plan_digest(plan), evidence_version=1, phase='parallel', process_id=1000 + index, exit_code=0, cancelled=False, elapsed_seconds=1., started_utc='2026-10-15T20:00:00+00:00', completed_utc='2026-10-15T20:00:01+00:00', module_path=str(Path('/bin/Cna.Core.Tests.dll').resolve()), output_directory=str(directory.resolve()), command=suites.execution_command('/bin/Cna.Core.Tests.dll', directory, entry))
            (directory / 'status.json').write_text(json.dumps(status))
        for job in {entry['job'] for entry in plan['groups'].values()}:
            groups = {group: 1024 for group, entry in plan['groups'].items() if entry['job'] == job}
            snapshot = {'platform': 'Linux', 'effective_memory_limit_bytes': 16 * 2**30, 'memory_available_bytes': 12 * 2**30, 'effective_cpu_count': 4}
            evidence = {'schema_version': 1, 'job': job, 'binding': self.binding, 'policy': dict(RESOURCE_POLICY), 'phase_preflights': [snapshot], 'peak_group_rss_bytes': groups, 'peak_aggregate_rss_bytes': 4096, 'min_available_bytes': 12 * 2**30, 'sample_count': 10, 'abort_reason': None, 'kernel_children_peak_rss_bytes': 2048, 'kernel_peak_kind': 'job-wide completed-children maximum; not per-child attribution', 'complete': True, 'elapsed_seconds': 3}
            (self.root / (job + '-resource.json')).write_text(json.dumps(evidence))

    def check(self):
        return suites.verify_all(self.suite_proof, self.weights, self.proof, self.fixture, self.inventory, self.root, self.needs, self.binding)

    def test_missing_support_xml_or_old_process_fails_combined_gate(self):
        support = self.root / 'r02-groups' / 'support-contract' / 'results' / 'report.xml'
        original = support.read_bytes(); support.unlink()
        with self.assertRaises(ValueError): self.check()
        support.write_bytes(original)
        (self.root / 'exercise' / 'status.json').unlink()
        with self.assertRaises(ValueError): self.check()

    def test_missing_new_legacy_or_format_cannot_pass(self):
        plan = self.suite_proof['plan']
        group = next(iter(plan['groups']))
        status = self.root / 'long-groups' / group / 'status.json'
        original = status.read_bytes(); status.unlink()
        with self.assertRaises(ValueError): self.check()
        status.write_bytes(original)
        self.needs['format']['result'] = 'failure'
        with self.assertRaises(ValueError): self.check()

    def test_foreign_root_xml_cannot_hide_outside_supported_process_results(self):
        (self.root / 'foreign.xml').write_text('<assemblies/>')
        with self.assertRaises(ValueError): self.check()



class FixturePathAliasTests(unittest.TestCase):
    def test_positive_process_fixtures_record_canonical_producer_binary_under_alias(self):
        # Linux /bin is an alias; use a controlled real symlink on every host.
        from unittest import mock
        original_resolve = Path.resolve
        with tempfile.TemporaryDirectory() as temporary:
            target_directory = original_resolve(Path(temporary)) / 'canonical-bin'
            target_directory.mkdir()
            expected_binary = target_directory / 'Cna.Core.Tests.dll'
            expected_binary.write_bytes(b'fixture marker, never executed')
            alias = Path(temporary) / 'bin-alias'
            alias.symlink_to(target_directory, target_is_directory=True)

            def resolve_with_bin_alias(path, *args, **kwargs):
                if str(path) == '/bin/Cna.Core.Tests.dll':
                    path = alias / 'Cna.Core.Tests.dll'
                return original_resolve(path, *args, **kwargs)

            for fixture_class in (LongProcessEvidenceTests, CombinedSuiteTests):
                with self.subTest(fixture=fixture_class.__name__), mock.patch.object(Path, 'resolve', resolve_with_bin_alias):
                    case = fixture_class()
                    try:
                        case.setUp()
                        if fixture_class is LongProcessEvidenceTests:
                            statuses = [case.directory / 'status.json']
                        else:
                            statuses = sorted((case.root / 'long-groups').glob('*/status.json'))
                        case.check()
                        # Expected target is independently constructed above,
                        # not the same resolve() expression used by the fixture.
                        for status_path in statuses:
                            status = json.loads(status_path.read_text())
                            self.assertEqual(str(expected_binary), status['module_path'])
                            self.assertEqual(str(expected_binary), status['command'][2])
                        # Keep canonical verification strict: an aliased status
                        # cannot match a command that recorded the actual target.
                        status_path = statuses[0]
                        original = status_path.read_bytes()
                        status = json.loads(original)
                        status['module_path'] = '/bin/Cna.Core.Tests.dll'
                        status_path.write_text(json.dumps(status))
                        with self.assertRaisesRegex(ValueError, 'Process command/filter differs'):
                            case.check()
                        status_path.write_bytes(original)
                        case.check()
                    finally:
                        case.doCleanups()


class WorkflowTests(unittest.TestCase):
    def test_finite_matrix_and_parallel_format_are_in_the_required_gate(self):
        import re
        text = (Path(__file__).parents[1] / 'workflows' / 'ci.yml').read_text()
        matrix = re.search(r'shard: \[(.*?)\]', text)[1]
        actual = {value.strip() for value in matrix.split(',')}
        self.assertEqual(set(suites.LONG_JOBS + suites.FAST_JOBS) | {'r02-1', 'r02-2', 'r02-3', 'exercise', 'contracts'}, actual)
        self.assertIn('max-parallel: 17', text)
        self.assertIn('needs: [prepare, tests, format, orleans]', text)
        orleans_job = text.split('  orleans:', 1)[1].split('  verify:', 1)[0]
        self.assertIn('dotnet test --project tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj', orleans_job)
        self.assertIn('python3 .github/scripts/verify_orleans_results.py', orleans_job)
        self.assertNotIn('continue-on-error', orleans_job)
        self.assertNotIn('Verify formatting', text.split('  prepare:', 1)[1].split('  tests:', 1)[0])
        format_job = text.split('  format:', 1)[1].split('  prepare:', 1)[0]
        self.assertIn('dotnet format Sandtable.slnx --verify-no-changes --no-restore', format_job)
        self.assertNotIn('continue-on-error', format_job)
        for mode in ('discover', 'execute', 'verify'):
            self.assertIn('python3 .github/scripts/long_suite_partition.py ' + mode, text)


if __name__ == '__main__':
    unittest.main()
