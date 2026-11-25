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
        # The p01-* proof families run nightly only (see ci-nightly-heavy.yml)
        # and are never part of this dependency closure on pull_request or
        # push; orleans is required on both (Phase D of #439).
        required = ('prepare', 'tests', 'format', 'orleans')
        good = {key: {'result': 'success'} for key in required}
        suites.validate_dependencies(good)
        for key in good:
            for result in ('failure', 'cancelled', 'skipped'):
                bad = copy.deepcopy(good); bad[key]['result'] = result
                with self.subTest(key=key, result=result), self.assertRaises(ValueError): suites.validate_dependencies(bad)
        for missing in required:
            with self.subTest(missing=missing), self.assertRaises(ValueError):
                suites.validate_dependencies({key: value for key, value in good.items() if key != missing})
        for extra in ('p01-paid-fresh-aggregate',):
            with self.subTest(extra=extra), self.assertRaises(ValueError):
                suites.validate_dependencies(dict(good, **{extra: {'result': 'success'}}))


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

    def test_four_lanes_never_run_more_than_three_processes_at_once(self):
        """Regression for the long-01 CI shard failure: with all four lanes
        populated, true concurrency must stay at MAX_CONCURRENT_LANES (3), not
        4 - CI's hosted runner tripped its 3GiB memory reserve when all four
        lanes' heavy rows briefly overlapped (abort_reason: "Available memory
        below 3GiB reserve", peak_aggregate_rss_bytes ~10.8GiB of ~15.3GiB
        physical). Each synthetic lane here has exactly one row (32 bins / 32
        lanes), so a concurrently-running fourth process is directly observed,
        not inferred from timing."""
        import os
        import sys
        from unittest import mock
        import ci_group_runner
        snapshot = {'platform': 'Darwin', 'physical_memory_bytes': 16 * 2**30, 'effective_memory_limit_bytes': 16 * 2**30, 'memory_available_bytes': 12 * 2**30, 'effective_cpu_count': 4}
        plan = suites.make_plan(self.rows, self.weights)
        self.assertEqual(4, sum(1 for entry in plan['groups'].values() if entry['job'] == 'long-01'))
        with tempfile.TemporaryDirectory() as temporary, mock.patch.object(ci_group_runner, 'resource_snapshot', return_value=snapshot):
            root = Path(temporary) / 'groups'
            markers = Path(temporary) / 'markers'
            peaks_dir = Path(temporary) / 'peaks'
            markers.mkdir()
            peaks_dir.mkdir()
            script = (
                "import os, sys, time\n"
                "marker_dir, peak_dir = sys.argv[1], sys.argv[2]\n"
                "pid_file = os.path.join(marker_dir, str(os.getpid()))\n"
                "open(pid_file, 'w').close()\n"
                "time.sleep(0.3)\n"
                "count = len(os.listdir(marker_dir))\n"
                "open(os.path.join(peak_dir, 'peak-' + str(count)), 'w').close()\n"
                "time.sleep(0.1)\n"
                "os.remove(pid_file)\n"
            )
            command_builder = lambda *_: [sys.executable, '-c', script, str(markers), str(peaks_dir)]
            statuses = suites.run_suite_job(plan, 'long-01', root, {}, '/bin/not-used.dll', 10, command_builder=command_builder)
            self.assertTrue(all(status['exit_code'] == 0 and not status['cancelled'] for status in statuses.values()))
            peaks = [int(name.split('-', 1)[1]) for name in os.listdir(peaks_dir) if name.startswith('peak-')]
            self.assertTrue(peaks)
            self.assertLessEqual(max(peaks), suites.MAX_CONCURRENT_LANES)
            self.assertEqual(suites.MAX_CONCURRENT_LANES, max(peaks), 'test is not exercising full concurrency; strengthen it')

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

    def check(self, excluded_shards=frozenset()):
        return suites.verify_all(self.suite_proof, self.weights, self.proof, self.fixture, self.inventory, self.root, self.needs, self.binding, excluded_shards)

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

    def test_unscoped_call_matches_omitted_default_exactly(self):
        # (a) The push-to-main path (no --exclude-shards) must be byte-for-byte
        # today's behaviour: passing an explicit empty scope equals omitting it.
        explicit = self.check(frozenset())
        default = suites.verify_all(self.suite_proof, self.weights, self.proof, self.fixture, self.inventory, self.root, self.needs, self.binding)
        self.assertEqual(explicit, default)
        self.assertEqual(explicit['excluded_shards'], [])

    def test_pr_scope_excludes_long_rows_without_failing_closed(self):
        # (b) Narrowing the expected-row set to drop a long-* job's rows (as
        # pull_request verify does) must not fail closed just because that
        # job's evidence was never produced.
        import shutil
        plan = self.suite_proof['plan']
        self.assertEqual({'long-01'}, {entry['job'] for entry in plan['groups'].values()} - {'fast-01', 'fast-02', 'fast-03', 'fast-04'})
        for group, entry in plan['groups'].items():
            if entry['job'] == 'long-01': shutil.rmtree(self.root / 'long-groups' / group)
        (self.root / 'long-01-resource.json').unlink()
        result = self.check(frozenset({'long-01'}))
        self.assertEqual(result['excluded_shards'], ['long-01'])
        self.assertNotIn('long-01', result['resource_jobs'])
        self.assertIn('fast-01', result['resource_jobs'])

    def test_pr_scope_still_fails_closed_for_in_scope_missing_row(self):
        # (c) Scoping must not become toothless for the rows that remain in
        # scope: a missing fast-* row still fails closed even with long-01
        # excluded.
        import shutil
        plan = self.suite_proof['plan']
        for group, entry in plan['groups'].items():
            if entry['job'] == 'long-01': shutil.rmtree(self.root / 'long-groups' / group)
        (self.root / 'long-01-resource.json').unlink()
        status = self.root / 'long-groups' / 'fast-01' / 'status.json'
        original = status.read_bytes(); status.unlink()
        with self.assertRaises(ValueError): self.check(frozenset({'long-01'}))
        status.write_bytes(original)
        self.assertEqual(self.check(frozenset({'long-01'}))['excluded_shards'], ['long-01'])

    def test_only_long_jobs_may_ever_be_excluded(self):
        for bad in (frozenset({'fast-01'}), frozenset({'orleans'}), frozenset({'r02-1'}), frozenset({'exercise'}), frozenset({'contracts'})):
            with self.subTest(bad=bad), self.assertRaises(ValueError):
                self.check(bad)



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


class CommandLineTests(unittest.TestCase):
    def test_help_exposes_shard_scoping(self):
        import subprocess
        import sys
        result = subprocess.run([sys.executable, str(Path(__file__).with_name('long_suite_partition.py')), '--help'], capture_output=True, text=True)
        self.assertEqual(result.returncode, 0)
        for flag in ('discover', 'execute', 'verify', '--exclude-shards'):
            self.assertIn(flag, result.stdout)

    def test_exclude_shards_rejected_outside_verify_mode(self):
        import subprocess
        import sys
        result = subprocess.run([sys.executable, str(Path(__file__).with_name('long_suite_partition.py')), 'discover', '--sha', 'd' * 40, '--exclude-shards', 'long-01'], capture_output=True, text=True)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('only applies to verify mode', result.stderr + result.stdout)


class WorkflowTests(unittest.TestCase):
    def test_tiered_shard_scope_matches_pr_and_push_expectations(self):
        import json
        import re
        text = (Path(__file__).parents[1] / 'workflows' / 'ci.yml').read_text()
        # The "if pull_request / else" scope step writes the pull_request
        # (narrowed) branch textually first, the push (full/default) branch
        # second.
        shard_matches = re.findall(r"shards=(\[.*?\])'", text)
        excluded_matches = re.findall(r"excluded_shards=(.*?)'", text)
        self.assertEqual(2, len(shard_matches), 'Expected exactly one pull_request and one push shard list')
        self.assertEqual(2, len(excluded_matches), 'Expected exactly one pull_request and one push excluded-shard list')
        pr_set, full_set = set(json.loads(shard_matches[0])), set(json.loads(shard_matches[1]))
        pr_excluded, full_excluded = excluded_matches[0], excluded_matches[1]
        self.assertEqual(set(suites.LONG_JOBS + suites.FAST_JOBS) | {'r02-1', 'r02-2', 'r02-3', 'exercise', 'contracts'}, full_set)
        self.assertEqual(set(suites.FAST_JOBS) | {'r02-1', 'r02-2', 'r02-3', 'exercise', 'contracts'}, pr_set)
        self.assertFalse(pr_set & set(suites.LONG_JOBS))
        self.assertEqual('', full_excluded)
        self.assertEqual(set(suites.LONG_JOBS), set(pr_excluded.split(',')))
        # The tests matrix now sources its shard list from prepare's output,
        # not a literal array, so both triggers share one job definition.
        self.assertIn("shard: ${{ fromJSON(needs.prepare.outputs.shards) }}", text)
        self.assertIn('max-parallel: 17', text)
        # p01-* are gone from ci.yml's dependency closure entirely (nightly
        # only - see ci-nightly-heavy.yml); orleans gates every trigger again,
        # excluding only the nightly soak row (the #446 rows gate again).
        self.assertIn('needs: [prepare, tests, format, orleans]', text)
        orleans_job = text.split('\n  orleans:', 1)[1].split('\n  verify:', 1)[0]
        # The orleans job is sharded 4-way; the dotnet test command, exclusions and per-shard
        # report check live in orleans_partition.py (covered by orleans_partition_test.py), and
        # the verify job proves the shards are disjoint, complete and passing.
        self.assertIn('python3 .github/scripts/orleans_partition.py execute --shard "$ORLEANS_SHARD" --total 4', orleans_job)
        self.assertIn('shard: [1, 2, 3, 4]', orleans_job)
        self.assertIn('python3 .github/scripts/orleans_partition.py verify --total 4', text)
        partition_source = Path(__file__).with_name('orleans_partition.py').read_text()
        self.assertIn("'dotnet', 'test', '--project', PROJECT", partition_source)
        self.assertNotIn('--filter-not-trait', orleans_job)
        self.assertNotIn('continue-on-error', orleans_job)
        self.assertNotIn('\n  p01-', text)
        self.assertIn('--exclude-shards "$EXCLUDE_SHARDS"', text)
        self.assertIn('EXCLUDE_SHARDS: ${{ needs.prepare.outputs.excluded_shards }}', text)
        self.assertNotIn('Verify formatting', text.split('  prepare:', 1)[1].split('  tests:', 1)[0])
        format_job = text.split('  format:', 1)[1].split('  prepare:', 1)[0]
        self.assertIn('dotnet format Sandtable.slnx --verify-no-changes --no-restore', format_job)
        self.assertNotIn('continue-on-error', format_job)
        for mode in ('discover', 'execute', 'verify'):
            self.assertIn('python3 .github/scripts/long_suite_partition.py ' + mode, text)

    def test_nightly_heavy_workflow_carries_orleans_and_every_p01_family_unmodified(self):
        text = (Path(__file__).parents[1] / 'workflows' / 'ci-nightly-heavy.yml').read_text()
        trigger = text.split('\non:\n', 1)[1].split('\njobs:', 1)[0]
        self.assertIn('schedule:', trigger)
        self.assertIn('cron:', trigger)
        self.assertNotIn('pull_request', trigger)
        self.assertNotIn('push:', trigger)
        self.assertIn('dotnet test --project tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj', text)
        self.assertIn('python3 .github/scripts/verify_orleans_results.py', text)
        self.assertIn('--filter-not-display-name "Cna.OrleansHost.Tests.GrazianiPregameJournalTests.FinalizedRealSupplyPlanSurvivesFreshHostReplayWithoutStartingTheGame"', text)
        self.assertNotIn('continue-on-error', text)
        for job in (
            'p01-paid-fresh-shard', 'p01-paid-fresh-aggregate',
            'p01-rba-decline-fresh-shard', 'p01-rba-decline-fresh-aggregate',
            'p01-rba-cancel-fresh-shard', 'p01-rba-cancel-fresh-aggregate',
            'p01-force-seal-fresh', 'p01-absent-shard', 'p01-absent-aggregate',
            'p01-all-prefix-reject-shard', 'p01-all-prefix-reject-aggregate',
            'orleans',
        ):
            self.assertIn('\n  ' + job + ':', text)
        self.assertNotIn('needs: prepare', text)


if __name__ == '__main__':
    unittest.main()
