import copy
import fnmatch
import re
import importlib.util
import json
import os
import shutil
import sys
from pathlib import Path
import tempfile
import unittest
import xml.etree.ElementTree as ET
from unittest import mock

spec = importlib.util.spec_from_file_location('partition', Path(__file__).with_name('test_partition.py'))
partition = importlib.util.module_from_spec(spec)
spec.loader.exec_module(partition)
SHA = 'a' * 40


class CoverageTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.needs = {'prepare': {'result': 'success'}, 'tests': {'result': 'success'}}
        modules = {'core-a': 'Cna.Core.Tests', 'core-b': 'Cna.Core.Tests', 'core-c': 'Cna.Core.Tests', 'core-d': 'Cna.Core.Tests', 'exercise': 'Cna.ExerciseRunner.Tests', 'contracts': 'Cna.Intelligence.Contracts.Tests'}
        self.inventory = {'schema_version': 1, 'tested_sha': SHA, 'sdk': '10.0.400', 'configuration': 'Release', 'run_attempt': 'local', 'shards': {}}
        for shard, module in modules.items():
            method = module + '.Cases.' + ({'core-a': 'Alpha', 'core-b': 'Beta', 'core-c': 'Gamma', 'core-d': 'Delta'}.get(shard, 'Beta'))
            name = method + '(value: "a\\b")'
            self.inventory['shards'][shard] = {'module': module, 'rows': [[module, method, name]]}
            d = self.root / shard
            (d / 'results').mkdir(parents=True)
            (d / 'status.json').write_text(json.dumps({'shard': shard, 'module': module, 'tested_sha': SHA, 'sdk': '10.0.400', 'configuration': 'Release', 'run_attempt': 'local', 'exit_code': 0}))
            root = ET.Element('assemblies')
            assembly = ET.SubElement(root, 'assembly', name='/bin/' + module + '.dll', total='1', passed='1', failed='0', skipped='0', errors='0', **{'not-run': '0', 'target-framework': '.NETCoreApp,Version=v10.0'})
            collection = ET.SubElement(assembly, 'collection')
            ET.SubElement(collection, 'test', name=json.dumps(name)[1:-1], type=method.rsplit('.', 1)[0], method=method.rsplit('.', 1)[1], result='Pass')
            ET.ElementTree(root).write(d / 'results' / 'report.xunit.xml', encoding='utf-8')

    def test_progress_cannot_replace_missing_final_report(self):
        leaf = self.root / 'core-a'
        (leaf / 'results' / 'report.xunit.xml').unlink()
        (leaf / 'diagnostics').mkdir()
        (leaf / 'diagnostics' / 'progress.diag').write_text('Timestamped diagnostic progress is not acceptance evidence')
        with self.assertRaises(ValueError): self.check()

    def test_inventory_attempt_mismatch_fails(self):
        self.inventory['run_attempt'] = '1'
        with self.assertRaises(ValueError): self.check()

    def test_stale_aggregate_copy_cannot_replace_missing_current_leaf(self):
        status_path = self.root / 'core-a' / 'status.json'
        stale_status = json.loads(status_path.read_text())
        status_path.unlink()
        stale_status['run_attempt'] = '1'
        # An old aggregate contains the same SHA, report and passing rows, but an older attempt.
        status_path.write_text(json.dumps(stale_status))
        with self.assertRaises(ValueError): self.check()

    def check(self):
        return partition.verify_coverage(self.inventory, self.root, self.needs, SHA)

    def report(self, shard='core-a'):
        p = self.root / shard / 'results' / 'report.xunit.xml'
        return p, ET.parse(p)

    def change_status(self, **values):
        p = self.root / 'core-a' / 'status.json'
        d = json.loads(p.read_text()); d.update(values); p.write_text(json.dumps(d))

    def test_complete_disjoint_argument_bearing_coverage_passes(self):
        self.assertTrue(self.check())

    def test_missing_report_fails(self):
        self.report()[0].unlink()
        with self.assertRaises(ValueError): self.check()

    def test_missing_status_fails(self):
        (self.root / 'core-a' / 'status.json').unlink()
        with self.assertRaises(ValueError): self.check()

    def test_malformed_report_fails(self):
        self.report()[0].write_text('<assemblies>')
        with self.assertRaises(ValueError): self.check()

    def test_missing_row_fails(self):
        p, t = self.report(); a = t.getroot().find('assembly'); a.find('collection').clear(); a.set('total', '0'); a.set('passed', '0'); t.write(p)
        with self.assertRaises(ValueError): self.check()

    def test_duplicate_report_fails(self):
        p, _ = self.report(); p.with_name('duplicate.xunit.xml').write_bytes(p.read_bytes())
        with self.assertRaises(ValueError): self.check()

    def test_duplicate_row_fails(self):
        p, t = self.report(); c = t.getroot().find('.//collection'); c.append(copy.deepcopy(c.find('test'))); a = t.getroot().find('assembly'); a.set('total', '2'); a.set('passed', '2'); t.write(p)
        with self.assertRaises(ValueError): self.check()

    def test_unexpected_argument_row_fails(self):
        p, t = self.report(); t.getroot().find('.//test').set('name', json.dumps(self.inventory['shards']['core-a']['rows'][0][2].replace('a', 'z'))[1:-1]); t.write(p)
        with self.assertRaises(ValueError): self.check()

    def test_forged_type_or_method_fails(self):
        p, t = self.report(); t.getroot().find('.//test').set('type', 'Cna.Core.Tests.Forged'); t.write(p)
        with self.assertRaises(ValueError): self.check()

    def test_counter_lie_fails(self):
        p, t = self.report(); t.getroot().find('assembly').set('total', '3'); t.write(p)
        with self.assertRaises(ValueError): self.check()

    def test_missing_counter_fails(self):
        p, t = self.report(); del t.getroot().find('assembly').attrib['errors']; t.write(p)
        with self.assertRaises(ValueError): self.check()

    def test_environment_error_fails(self):
        p, t = self.report(); ET.SubElement(ET.SubElement(t.getroot().find('assembly'), 'errors'), 'error'); t.write(p)
        with self.assertRaises(ValueError): self.check()

    def test_unknown_module_fails(self):
        p, t = self.report(); t.getroot().find('assembly').set('name', '/bin/Unknown.dll'); t.write(p)
        with self.assertRaises(ValueError): self.check()

    def test_malformed_xml_name_escape_fails(self):
        p, t = self.report(); t.getroot().find('.//test').set('name', '\\q'); t.write(p)
        with self.assertRaises(ValueError): self.check()

    def test_duplicate_inventory_row_fails(self):
        rows = self.inventory['shards']['core-a']['rows']; rows.append(rows[0])
        with self.assertRaises(ValueError): self.check()

    def test_missing_or_unexpected_shard_fails(self):
        self.inventory['shards']['unexpected'] = self.inventory['shards'].pop('core-a')
        with self.assertRaises(ValueError): self.check()

    def test_cross_shard_overlap_fails(self):
        self.inventory['shards']['core-b']['rows'] = self.inventory['shards']['core-a']['rows']
        with self.assertRaises(ValueError): self.check()

    def test_wrong_inventory_sha_fails(self):
        self.inventory['tested_sha'] = 'b' * 40
        with self.assertRaises(ValueError): self.check()

    def test_wrong_configuration_fails(self):
        self.inventory['configuration'] = 'Debug'
        with self.assertRaises(ValueError): self.check()


def bad_status(field, value):
    def test(self):
        self.change_status(**{field: value})
        with self.assertRaises(ValueError): self.check()
    return test

for field, values in {'exit_code': [1, 2, 8, 130, True, None], 'tested_sha': ['b' * 40, None], 'sdk': ['11.0.100', '10.0.401', None], 'configuration': ['Debug'], 'run_attempt': ['1', None], 'shard': ['core-b'], 'module': ['Unknown']}.items():
    for i, value in enumerate(values): setattr(CoverageTests, f'test_bad_status_{field}_{i}_fails', bad_status(field, value))

def bad_dependency(dependency, value):
    def test(self):
        if value is None: del self.needs[dependency]
        else: self.needs[dependency]['result'] = value
        with self.assertRaises(ValueError): self.check()
    return test

for dependency in ['prepare', 'tests']:
    for value in ['failure', 'cancelled', 'skipped', None]: setattr(CoverageTests, f'test_dependency_{dependency}_{value}_fails', bad_dependency(dependency, value))

def bad_result(result):
    def test(self):
        p, t = self.report(); t.getroot().find('.//test').set('result', result); t.write(p)
        with self.assertRaises(ValueError): self.check()
    return test

for result in ['Fail', 'Skip', 'NotRun', 'Unknown']: setattr(CoverageTests, f'test_result_{result}_fails', bad_result(result))

class FifthLeafCoverageTests(unittest.TestCase):
    def setUp(self):
        self.fixture = CoverageTests(); self.fixture.setUp()
        self.addCleanup(self.fixture.temp.cleanup)
        self.leaf = self.fixture.root / 'core-c'
        self.assertEqual(set(self.fixture.inventory['shards']), {'core-a', 'core-b', 'core-c', 'core-d', 'exercise', 'contracts'})

    def status(self, **values):
        p = self.leaf / 'status.json'; data = json.loads(p.read_text()); data.update(values); p.write_text(json.dumps(data))

    def test_complete_five_leaf_current_attempt_passes(self): self.assertTrue(self.fixture.check())

    def test_missing_c_report_cannot_use_other_passing_leaves(self):
        (self.leaf / 'results' / 'report.xunit.xml').unlink()
        with self.assertRaises(ValueError): self.fixture.check()

    def test_missing_c_status_cannot_use_other_passing_leaves(self):
        (self.leaf / 'status.json').unlink()
        with self.assertRaises(ValueError): self.fixture.check()

    def test_nonzero_c_process_rejects_passing_xml(self):
        self.status(exit_code=2)
        with self.assertRaises(ValueError): self.fixture.check()

    def test_stale_c_attempt_rejects_same_sha(self):
        self.status(run_attempt='1')
        with self.assertRaises(ValueError): self.fixture.check()

    def test_foreign_c_sha_rejects_complete_rows(self):
        self.status(tested_sha='b' * 40)
        with self.assertRaises(ValueError): self.fixture.check()

    def test_foreign_c_module_rejects_complete_rows(self):
        self.status(module='Cna.Intelligence.Contracts.Tests')
        with self.assertRaises(ValueError): self.fixture.check()

    def test_c_b_overlap_rejects_even_with_all_leaves(self):
        self.fixture.inventory['shards']['core-c']['rows'] = self.fixture.inventory['shards']['core-b']['rows']
        with self.assertRaises(ValueError): self.fixture.check()

class DiscoveryTests(unittest.TestCase):
    def setUp(self):
        self.control_methods = ['Cna.Core.Tests.Campaigns.CombatActualRoundEntryTests.NativeActualEntryRetainsExactPreparedAndCancelledTerminalProofs', 'Cna.Core.Tests.Campaigns.BreakdownRecordTests.InvalidFiniteShapesAndNoncanonicalBytesReject']
        self.controls = [method + '(value: "quoted")' for method in self.control_methods]
        self.payload = {'schema_version': 1, 'runner_version': '4.0.2', 'providers': [{'method': method, 'provider': 'Reviewed', 'names': [method + '(value: "deferred")']} for method in sorted(partition.DEFERRED)], 'controls': [{'method': method, 'names': [name]} for method, name in zip(self.control_methods, self.controls)]}
        mutation = partition.SELECTORS[0]
        indices = {0, 1, 2, 3, 17, 18, 19, 20}
        a = [method + '(index: 0)' for method in partition.SELECTORS[1:]] + [mutation + f'(index: {i})' for i in range(34) if i not in indices]
        b = sorted(partition.DEFERRED) + self.controls + [mutation + f'(index: {i})' for i in sorted(indices)]
        c = [cls + '.Initial(index: 0)' for cls in partition.CORE_C_CLASSES]
        d = [cls + '.Initial(index: 0)' for cls in partition.CORE_D_CLASSES]
        self.texts = {'core': self.text(a + b + c + d), 'core-a': self.text(a), 'core-b': self.text(b), 'core-c': self.text(c), 'core-d': self.text(d), 'exercise': self.text(['Cna.ExerciseRunner.Tests.Example.Fact']), 'contracts': self.text(['Cna.Intelligence.Contracts.Tests.Example.Fact'])}

    @staticmethod
    def text(names):
        return f'Discovered {len(names)} tests in assembly - release\n' + ''.join('  ' + name + '\n' for name in names) + f'Discovered {len(names)} tests.\n'

    def check(self):
        return partition.build_inventory(self.texts, self.payload, SHA, '10.0.400')

    def test_complete_discovery(self):
        inventory = self.check()
        self.assertEqual(len(inventory['shards']['core-a']['rows']), 37)
        self.assertEqual(len(inventory['shards']['core-b']['rows']), 15)

    def test_duplicate_formatter_controls_fail(self):
        self.payload['controls'][1] = self.payload['controls'][0]
        with self.assertRaises(ValueError): self.check()

    def test_wrong_control_method_fails(self):
        self.payload['controls'][0]['method'] = 'Unknown'
        with self.assertRaises(ValueError): self.check()

    def test_missing_formatter_control_fails(self):
        self.payload['controls'].pop()
        with self.assertRaises(ValueError): self.check()

    def test_formatter_drift_fails(self):
        self.payload['controls'][0]['names'] = ['Different']
        with self.assertRaises(ValueError): self.check()

    def test_unknown_provider_fails(self):
        self.payload['providers'][0]['method'] = 'Unknown'
        with self.assertRaises(ValueError): self.check()

    def test_duplicate_provider_rows_fail(self):
        self.payload['providers'][0]['names'] *= 2
        with self.assertRaises(ValueError): self.check()

    def test_wrong_deferred_method_fails(self):
        self.payload['providers'][0]['names'] = [self.payload['providers'][1]['names'][0]]
        with self.assertRaises(ValueError): self.check()

    def test_only_reviewed_runner_402_proof_is_accepted(self):
        inventory = self.check()
        self.assertEqual(set(inventory['shards']), set(partition.MODULES))
        self.assertEqual(len(inventory['shards']['core-a']['rows']), 37)
        self.assertEqual(len(inventory['shards']['core-b']['rows']), 15)

    def test_unreviewed_or_malformed_runner_versions_fail(self):
        for version in ['4.0.1', '4.0.3', 'unknown', '', 'v4.0.2', '4.0.2.0',
                        '4.0.2 ', None, 402, True, ['4.0.2'], {'version': '4.0.2'}]:
            with self.subTest(version=version):
                self.payload['runner_version'] = version
                with self.assertRaisesRegex(ValueError, 'Unsupported provider proof'):
                    self.check()

    def test_missing_runner_version_fails(self):
        del self.payload['runner_version']
        with self.assertRaisesRegex(ValueError, 'Unsupported provider proof'):
            self.check()

    def test_include_filter_failure_fails(self):
        self.texts['core-a'] = self.text(self.controls)
        with self.assertRaises(ValueError): self.check()

    def test_filter_overlap_fails(self):
        self.texts['core-b'] = self.texts['core']
        with self.assertRaises(ValueError): self.check()

    def test_truncated_discovery_fails(self):
        self.texts['core'] = self.texts['core'].rsplit('Discovered', 1)[0]
        with self.assertRaises(ValueError): self.check()

    def test_duplicate_discovery_fails(self):
        self.texts['core'] = self.text(self.controls * 2)
        with self.assertRaises(ValueError): self.check()

    def test_ansi_discovery_fails(self):
        self.texts['core'] += '\x1b[0m'
        with self.assertRaises(ValueError): self.check()

    def test_unknown_display_semantics_fail(self):
        self.texts['exercise'] = self.text(['Custom name'])
        with self.assertRaises(ValueError): self.check()

class ArtifactProtocolTests(unittest.TestCase):
    def test_aggregate_cannot_be_reimported_as_leaf_evidence(self):
        workflow = Path(__file__).parents[1] / 'workflows' / 'ci.yml'
        text = workflow.read_text()
        pattern = re.search(r'pattern: (.+)', text).group(1)
        aggregate = re.search(r'name: (.+)', text.split('      - name: Retain aggregate evidence', 1)[1]).group(1)
        self.assertFalse(fnmatch.fnmatchcase(aggregate, pattern), 'A prior aggregate could hide missing leaf artifacts on retry')

class WorkflowDeadlineTests(unittest.TestCase):
    def test_core_b_alone_has_twenty_minute_budget(self):
        text = (Path(__file__).parents[1] / 'workflows' / 'ci.yml').read_text()
        leaf = text.split('  tests:', 1)[1].split('  verify:', 1)[0]
        shards = [s.strip() for s in re.search(r'shard: \[(.+)\]', leaf).group(1).split(',')]
        self.assertEqual(set(shards), set(partition.MODULES))
        value = re.search(r'^    timeout-minutes: (.+)$', leaf, re.MULTILINE).group(1)
        if value.isdecimal():
            actual = {shard: int(value) for shard in shards}
        else:
            # Recognize this workflow's numeric conditional; actionlint validates GitHub syntax.
            branch = re.fullmatch(r"\$\{\{ matrix\.shard == '([^']+)' && ([1-9][0-9]*) \|\| ([1-9][0-9]*) \}\}", value)
            self.assertIsNotNone(branch, 'Unrecognized shard deadline policy')
            selected, longer, default = branch.groups()
            actual = {shard: int(longer if shard.casefold() == selected.casefold() else default) for shard in shards}
        self.assertEqual(actual, {'core-a': 15, 'core-b': 25, 'core-c': 15, 'core-d': 15, 'exercise': 15, 'contracts': 15})

    def test_other_job_deadlines_remain_bounded(self):
        text = (Path(__file__).parents[1] / 'workflows' / 'ci.yml').read_text()
        for job, minutes in [('prepare', 15), ('verify', 15), ('dependency-review', 5)]:
            # Job body lines have four spaces; delimit on a subsequent two-space job header.
            block = re.split(r'\n  [^ ]', text.split('  ' + job + ':', 1)[1], maxsplit=1)[0]
            self.assertEqual(int(re.search(r'^    timeout-minutes: ([0-9]+)$', block, re.MULTILINE).group(1)), minutes)

class DiagnosticCommandTests(unittest.TestCase):
    def setUp(self):
        fixture = CoreRowPartitionTests(); fixture.setUp()
        self.plan = fixture.check()

    def test_supported_trace_and_synchronous_write(self):
        for shard in partition.MODULES:
            with self.subTest(shard=shard):
                command = partition.execution_command(shard, Path('/tmp/control') / shard, self.plan)
                self.assertIn('--diagnostic', command)
                self.assertIn('--diagnostic-synchronous-write', command)
                self.assertEqual(command[command.index('--diagnostic-verbosity') + 1], 'Trace')
                self.assertEqual(command[command.index('--output') + 1], 'Detailed')

    def test_diagnostics_stay_inside_uploaded_leaf_directory(self):
        for shard in partition.MODULES:
            with self.subTest(shard=shard):
                directory = Path('/tmp/artifacts/ci') / shard
                command = partition.execution_command(shard, directory, self.plan)
                path = Path(command[command.index('--diagnostic-output-directory') + 1])
                self.assertEqual(path, (directory / 'diagnostics').resolve())
                workflow = (Path(__file__).parents[1] / 'workflows' / 'ci.yml').read_text()
                leaf = workflow.split('  tests:', 1)[1].split('  verify:', 1)[0]
                self.assertIn('if: ${{ always() }}', leaf)
                self.assertIn('path: artifacts/ci', leaf)

    def test_whole_method_filters_and_acceptance_reports_preserved(self):
        for shard, module in partition.MODULES.items():
            with self.subTest(shard=shard):
                command = partition.execution_command(shard, Path('/tmp/control') / shard, self.plan)
                self.assertEqual(command[:2], ['dotnet', 'test'])
                self.assertEqual(command[command.index('--project') + 1], partition.project(module))
                self.assertIn('--report-xunit-xml', command)
                if shard in ['core-a', 'core-b']:
                    key = '--filter-method' if shard == 'core-a' else '--filter-not-method'
                    methods = list(partition.SELECTORS if shard == 'core-a' else partition.SELECTORS[1:])
                    self.assertEqual(command[command.index(key) + 1:command.index('--filter-not-display-name')], methods)
                elif shard in ['core-c', 'core-d']:
                    classes = partition.CORE_C_CLASSES if shard == 'core-c' else partition.CORE_D_CLASSES
                    self.assertEqual(command[command.index('--filter-class') + 1:], list(classes))
                    self.assertNotIn('--filter-method', command)
                    self.assertNotIn('--filter-not-method', command)
                else:
                    self.assertNotIn('--filter-method', command)
                    self.assertNotIn('--filter-not-method', command)
                self.assertNotIn('--max-threads', command)
                self.assertNotIn('--timeout', command)

class CrashCaptureExecutionTests(unittest.TestCase):
    crash_keys = (
        'DOTNET_DbgEnableMiniDump',
        'DOTNET_DbgMiniDumpType',
        'DOTNET_EnableCrashReport',
        'DOTNET_DbgMiniDumpName',
        'DOTNET_CreateDumpDiagnostics',
    )

    def setUp(self):
        self.fixture = CoverageTests()
        self.fixture.setUp()
        self.addCleanup(self.fixture.doCleanups)
        self.root = self.fixture.root
        shutil.rmtree(self.root / 'core-b')
        self.inventory_path = self.root / 'inventory.json'
        self.inventory_path.write_text(json.dumps(self.fixture.inventory))

    def execute_child(self, exit_code):
        # Read only triage settings; never emit the inherited process environment.
        child = (
            'import json, os, sys; '
            'print(json.dumps({key: os.environ.get(key) for key in ' + repr(self.crash_keys) + '}), flush=True); '
            'print("child progress retained", file=sys.stderr, flush=True); '
            'sys.exit(' + str(exit_code) + ')'
        )
        argv = ['test_partition.py', 'execute', '--sha', SHA,
                '--directory', str(self.root), '--shard', 'core-b',
                '--inventory', str(self.inventory_path)]
        inherited = {key: 'parent-value-' + key for key in self.crash_keys}
        with mock.patch.dict(os.environ, inherited):
            parent_environment = dict(os.environ)
            with mock.patch.object(sys, 'argv', argv), \
                    mock.patch.object(partition, 'identity', return_value='10.0.400'), \
                    mock.patch.object(partition, 'load_partition', return_value={}), \
                    mock.patch.object(partition, 'execution_command', return_value=[sys.executable, '-c', child]):
                with self.assertRaises(SystemExit) as stopped:
                    partition.main()
            self.assertEqual(stopped.exception.code, exit_code)
            self.assertEqual(dict(os.environ), parent_environment, 'Crash capture must not mutate parent environment')
        leaf = self.root / 'core-b'
        status = json.loads((leaf / 'status.json').read_text())
        self.assertEqual(status['exit_code'], exit_code)
        self.assertEqual(status['shard'], 'core-b')
        output = (leaf / 'execution.log').read_text()
        self.assertIn('child progress retained', output)
        settings = json.loads(output.splitlines()[0])
        return leaf, settings

    def assert_crash_capture_enabled(self, settings):
        expected = {'DOTNET_DbgEnableMiniDump': '1', 'DOTNET_DbgMiniDumpType': '3',
                    'DOTNET_EnableCrashReport': '1', 'DOTNET_CreateDumpDiagnostics': '1'}
        actual = {key: settings[key] for key in expected}
        self.assertEqual(actual, expected, 'Executed child is missing required .NET crash triage settings')

    def test_exit139_retains_failure_evidence_and_enables_crash_capture(self):
        leaf, settings = self.execute_child(139)
        self.assertFalse(list((leaf / 'results').glob('*.xml')))
        with self.assertRaises(ValueError):
            self.fixture.check()
        self.assert_crash_capture_enabled(settings)

    def test_success_without_final_xml_is_not_coverage_and_enables_crash_capture(self):
        leaf, settings = self.execute_child(0)
        self.assertFalse(list((leaf / 'results').glob('*.xml')))
        with self.assertRaisesRegex(ValueError, 'Missing/duplicate report'):
            self.fixture.check()
        self.assert_crash_capture_enabled(settings)

    def test_dump_pattern_uses_pid_and_time_inside_uploaded_shard_diagnostics(self):
        leaf, settings = self.execute_child(139)
        dump_name = settings['DOTNET_DbgMiniDumpName']
        self.assertIn('%p', dump_name, 'Dump filename needs runtime PID to distinguish child processes')
        self.assertIn('%t', dump_name, 'Dump filename needs runtime time to distinguish repeated crashes')
        diagnostics = (leaf / 'diagnostics').resolve()
        self.assertEqual(Path(dump_name).parent, diagnostics)
        self.assertTrue(diagnostics.is_dir(), 'Crash diagnostics directory must exist before child executes')


class CoreRowPartitionTests(unittest.TestCase):
    def setUp(self):
        self.method = partition.SELECTORS[0]
        self.transfer = {self.method + f'(index: {i})' for i in [0, 1, 2, 3, 17, 18, 19, 20]}
        self.rows = [('Cna.Core.Tests', self.method, self.method + f'(index: {i})') for i in range(34)]
        self.rows += [('Cna.Core.Tests', method, method + '(index: 0)') for method in partition.SELECTORS[1:]]
        method = 'Cna.Core.Tests.Future.NewMethod'
        self.rows += [('Cna.Core.Tests', method, method)]
        self.rows += [('Cna.Core.Tests', cls + '.Initial', cls + '.Initial(index: 0)') for cls in (*partition.CORE_C_CLASSES, *partition.CORE_D_CLASSES)]

    def check(self): return partition.core_partition(self.rows)

    def test_exact_eight_complete_rows_move(self):
        plan = self.check()
        self.assertEqual({row[2] for row in plan['core-b'] if row[1] == self.method}, self.transfer)
        self.assertFalse({row[2] for row in plan['core-a']} & self.transfer)
        self.assertEqual(plan['core-a'] | plan['core-b'] | plan['core-c'] | plan['core-d'], set(self.rows))
        self.assertFalse(plan['core-a'] & plan['core-b'])

    def test_missing_selected_completion_class_fails(self):
        self.rows = [row for row in self.rows if row[1].rsplit('.', 1)[0] != partition.CORE_C_CLASSES[0]]
        with self.assertRaises(ValueError): self.check()

    def test_missing_transfer_fails(self):
        self.rows = [row for row in self.rows if row[2] != self.method + '(index: 0)']
        with self.assertRaises(ValueError): self.check()

    def test_missing_selected_method_fails(self):
        self.rows = [row for row in self.rows if row[1] != partition.SELECTORS[1]]
        with self.assertRaises(ValueError): self.check()

    def test_duplicate_row_fails(self):
        self.rows.append(self.rows[0])
        with self.assertRaises(ValueError): self.check()

    def test_casefold_collision_fails(self):
        module, method, name = self.rows[0]
        self.rows.append((module, method.lower(), name.lower()))
        with self.assertRaises(ValueError): self.check()

    def test_unknown_custom_name_fails(self):
        self.rows[4] = ('Cna.Core.Tests', self.method, 'Custom row')
        with self.assertRaises(ValueError): self.check()

    def test_wildcard_name_fails(self):
        self.rows[4] = ('Cna.Core.Tests', self.method, self.method + '(index: *)')
        with self.assertRaises(ValueError): self.check()

    def test_unrelated_wildcard_name_fails(self):
        self.rows.append(('Cna.Core.Tests', 'Cna.Core.Tests.Future.Wildcard', 'Cna.Core.Tests.Future.Wildcard(value: "*")'))
        with self.assertRaises(ValueError): self.check()

    def test_new_mutation_row_enters_a_and_fresh_q(self):
        row = ('Cna.Core.Tests', self.method, self.method + '(index: 34)')
        self.rows.append(row); plan = self.check()
        self.assertIn(row, plan['core-a']); self.assertNotIn(row, plan['core-b'])
        self.assertIn(row[2], plan['retained_mutation'])

    def test_new_unrelated_method_enters_b(self):
        row = ('Cna.Core.Tests', 'Cna.Core.Tests.Future.Extra', 'Cna.Core.Tests.Future.Extra')
        self.rows.append(row); plan = self.check()
        self.assertIn(row, plan['core-b']); self.assertNotIn(row, plan['core-a'])

    def test_literal_display_filters_complement_predicate(self):
        plan = self.check(); a = partition.partition_filters('core-a', plan); b = partition.partition_filters('core-b', plan)
        self.assertEqual(a[1:13], list(partition.SELECTORS))
        self.assertEqual(set(a[a.index('--filter-not-display-name') + 1:]), self.transfer)
        self.assertEqual(b[1:12], list(partition.SELECTORS[1:]))
        self.assertEqual(set(b[b.index('--filter-not-display-name') + 1:b.index('--filter-not-class')]), set(plan['retained_mutation']))

    def test_execution_uses_same_row_filters(self):
        plan = self.check()
        for shard in ['core-a', 'core-b']:
            command = partition.execution_command(shard, Path('/tmp/control') / shard, plan)
            self.assertEqual(command[-len(partition.partition_filters(shard, plan)):], partition.partition_filters(shard, plan))
            self.assertIn('--filter-not-display-name', command)

    def test_discovery_process_has_structured_argv_and_shell_false(self):
        with tempfile.TemporaryDirectory() as tmp:
            command = ['dotnet', 'test', '--filter-not-display-name', *sorted(self.transfer)]
            with mock.patch.object(partition.subprocess, 'run', return_value=partition.subprocess.CompletedProcess(command, 0)) as run:
                partition.run_logged(command, Path(tmp) / 'log')
                self.assertEqual(run.call_args.args[0], command)
                self.assertIs(run.call_args.kwargs.get('shell'), False)

class CoreThirdShardTests(unittest.TestCase):
    classes = (
        'Cna.Core.Tests.Campaigns.CombatActualRepeatedMovementCompletionTests',
        'Cna.Core.Tests.Campaigns.CombatActualRepeatedMovementCompletionContractTests',
        'Cna.Core.Tests.Campaigns.CombatActualRepeatedBreakdownCompletionTests',
        'Cna.Core.Tests.Campaigns.CombatActualRepeatedBreakdownCompletionContractTests',
        'Cna.Core.Tests.Campaigns.CombatActualMovedContinuationContractTests',
        'Cna.Core.Tests.Campaigns.CombatActualRepeatedMovementContractTests',
        'Cna.Core.Tests.Campaigns.CombatActualPaidAssaultContractTests',
    )

    def setUp(self):
        fixture = CoreRowPartitionTests(); fixture.setUp()
        self.transfer = fixture.transfer
        self.rows = [r for r in fixture.rows if r[1].rsplit('.', 1)[0] not in self.classes]
        self.completion = {('Cna.Core.Tests', cls + '.Initial', cls + '.Initial(index: 0)') for cls in self.classes}
        self.rows += sorted(self.completion)
        self.expected_a = {r for r in self.rows if r[1] in partition.SELECTORS and r[2] not in self.transfer}

    def plan(self):
        plan = partition.core_partition(self.rows)
        self.assertEqual(plan['core-a'], self.expected_a, 'Existing A policy must stay unchanged')
        self.assertIsNotNone(plan.get('core-c'), 'Measured completion workload needs its own complete-row shard')
        return plan

    def test_third_partition_conserves_exact_rows_and_old_transfer(self):
        plan = self.plan()
        self.assertEqual(plan['core-c'], self.completion)
        a, b, c = [plan[s] for s in ['core-a', 'core-b', 'core-c']]
        self.assertEqual(a | b | c | plan['core-d'], set(self.rows))
        self.assertFalse(a & b or a & c or b & c)
        self.assertEqual({r[2] for r in b if r[1] == partition.SELECTORS[0]}, self.transfer)

    def test_new_method_in_completion_class_enters_c(self):
        method = self.classes[0] + '.FutureMethod'
        row = ('Cna.Core.Tests', method, method + '(index: 7)')
        self.rows.append(row); plan = self.plan()
        self.assertIn(row, plan['core-c']); self.assertNotIn(row, plan['core-b'])

    def test_unrelated_future_case_remains_in_b(self):
        row = ('Cna.Core.Tests', 'Cna.Core.Tests.Future.Extra', 'Cna.Core.Tests.Future.Extra(index: 0)')
        self.rows.append(row); plan = self.plan()
        self.assertIn(row, plan['core-b']); self.assertNotIn(row, plan['core-c'])

    def test_similar_class_name_does_not_widen_c(self):
        method = self.classes[0] + 'Extra.Initial'
        row = ('Cna.Core.Tests', method, method)
        self.rows.append(row); plan = self.plan()
        self.assertIn(row, plan['core-b']); self.assertNotIn(row, plan['core-c'])

    def test_filters_keep_a_and_bind_exact_c_class_membership(self):
        plan = self.plan()
        a = partition.partition_filters('core-a', plan)
        b = partition.partition_filters('core-b', plan)
        c = partition.partition_filters('core-c', plan)
        self.assertEqual(a[:13], ['--filter-method', *partition.SELECTORS])
        self.assertEqual(set(a[14:]), self.transfer)
        self.assertEqual(c, ['--filter-class', *self.classes])
        self.assertEqual(b[-(len(self.classes) + len(partition.CORE_D_CLASSES) + 1):], ['--filter-not-class', *self.classes, *partition.CORE_D_CLASSES])
        for shard in ['core-a', 'core-b', 'core-c']:
            filters = partition.partition_filters(shard, plan)
            command = partition.execution_command(shard, Path('/tmp/control') / shard, plan)
            self.assertEqual(command[-len(filters):], filters)

    def test_third_required_core_leaf_is_part_of_module_map(self):
        self.assertEqual(partition.MODULES.get('core-c'), 'Cna.Core.Tests')

class PreparedPartitionTests(unittest.TestCase):
    def setUp(self):
        fixture = DiscoveryTests(); fixture.setUp()
        self.inventory = fixture.check()

    def check(self):
        return partition.load_partition(self.inventory, SHA, '10.0.400', 'local')

    def test_prepared_complete_partition(self): self.check()

    def test_wrong_partition_same_union_fails(self):
        entry = self.inventory['shards']
        row = next(row for row in entry['core-b']['rows'] if row[1] == partition.SELECTORS[0])
        entry['core-b']['rows'].remove(row); entry['core-a']['rows'].append(row)
        with self.assertRaises(ValueError): self.check()

    def test_wrong_c_b_assignment_same_union_fails(self):
        entry = self.inventory['shards']; row = entry['core-c']['rows'].pop()
        entry['core-b']['rows'].append(row)
        with self.assertRaises(ValueError): self.check()

    def test_missing_prepared_c_leaf_fails(self):
        del self.inventory['shards']['core-c']
        with self.assertRaises(ValueError): self.check()

    def test_future_completion_method_filter_mismatch_fails(self):
        fixture = DiscoveryTests(); fixture.setUp(); name = partition.CORE_C_CLASSES[0] + '.Future(index: 9)'
        fixture.texts['core'] = fixture.text([line[2:] for line in fixture.texts['core'].splitlines() if line.startswith('  ')] + [name])
        with self.assertRaises(ValueError): fixture.check()

    def test_future_completion_method_filter_conservation(self):
        fixture = DiscoveryTests(); fixture.setUp(); name = partition.CORE_C_CLASSES[0] + '.Future(index: 9)'
        for label in ['core', 'core-c']: fixture.texts[label] = fixture.text([line[2:] for line in fixture.texts[label].splitlines() if line.startswith('  ')] + [name])
        inventory = fixture.check(); self.assertIn(name, {r[2] for r in inventory['shards']['core-c']['rows']}); self.assertNotIn(name, {r[2] for r in inventory['shards']['core-b']['rows']})

    def test_stale_prepared_sha_fails(self):
        self.inventory['tested_sha'] = 'b' * 40
        with self.assertRaises(ValueError): self.check()

    def test_stale_prepared_attempt_fails(self):
        self.inventory['run_attempt'] = '1'
        with self.assertRaises(ValueError): self.check()

    def test_wrong_prepared_sdk_fails(self):
        self.inventory['sdk'] = '10.0.401'
        with self.assertRaises(ValueError): self.check()

    def test_future_mutation_row_actual_filter_mismatch_fails(self):
        fixture = DiscoveryTests(); fixture.setUp()
        name = partition.SELECTORS[0] + '(index: 34)'
        fixture.texts['core'] = fixture.text([line[2:] for line in fixture.texts['core'].splitlines() if line.startswith('  ')] + [name])
        # Runner omitted the new row from A: stale filter realization cannot pass.
        with self.assertRaises(ValueError): fixture.check()

    def test_future_mutation_row_actual_filter_conservation(self):
        fixture = DiscoveryTests(); fixture.setUp()
        name = partition.SELECTORS[0] + '(index: 34)'
        for label in ['core', 'core-a']:
            fixture.texts[label] = fixture.text([line[2:] for line in fixture.texts[label].splitlines() if line.startswith('  ')] + [name])
        inventory = fixture.check()
        self.assertIn(name, {row[2] for row in inventory['shards']['core-a']['rows']})
        self.assertNotIn(name, {row[2] for row in inventory['shards']['core-b']['rows']})

    def test_prepared_proof_bound_download(self):
        text = (Path(__file__).parents[1] / 'workflows' / 'ci.yml').read_text()
        leaf = text.split('  tests:', 1)[1].split('  verify:', 1)[0]
        self.assertIn('name: ci-inventory-${{ github.sha }}-${{ github.run_attempt }}', leaf)
        self.assertIn('path: artifacts/partition-proof', leaf)

    def test_failed_discovery_process_fails(self):
        with tempfile.TemporaryDirectory() as tmp:
            with mock.patch.object(partition.subprocess, 'run', return_value=partition.subprocess.CompletedProcess([], 17)):
                with self.assertRaises(ValueError): partition.run_logged(['dotnet', 'test'], Path(tmp) / 'log')


class NativeCompletionCapacityTests(unittest.TestCase):
    native = ('Cna.Core.Tests.Campaigns.CombatActualRepeatedBreakdownCompletionTests', 'Cna.Core.Tests.Campaigns.CombatActualRepeatedBreakdownCompletionContractTests')

    def setUp(self):
        fixture = CoreThirdShardTests(); fixture.setUp()
        self.rows = list(fixture.rows); self.expected_a = fixture.expected_a

    def test_both_full_native_classes_enter_c_and_leave_b(self):
        plan = partition.core_partition(self.rows)
        self.assertEqual(plan['core-a'], self.expected_a)
        for cls in self.native:
            rows = {r for r in self.rows if r[1].rsplit('.', 1)[0] == cls}
            self.assertTrue(rows); self.assertTrue(rows <= plan['core-c']); self.assertFalse(rows & plan['core-b'])
        self.assertEqual(plan['core-a'] | plan['core-b'] | plan['core-c'] | plan['core-d'], set(self.rows))
        self.assertFalse(plan['core-a'] & plan['core-b'] or plan['core-a'] & plan['core-c'] or plan['core-b'] & plan['core-c'])

    def test_future_native_methods_follow_full_class_policy(self):
        for cls in self.native:
            method = cls + '.Future'; row = ('Cna.Core.Tests', method, method + '(index: 7)')
            plan = partition.core_partition(self.rows + [row]); self.assertIn(row, plan['core-c']); self.assertNotIn(row, plan['core-b'])
            similar = cls + 'Extra.Future'; extra = ('Cna.Core.Tests', similar, similar)
            plan = partition.core_partition(self.rows + [extra]); self.assertIn(extra, plan['core-b']); self.assertNotIn(extra, plan['core-c'])

    def test_missing_native_selected_class_fails_closed(self):
        for cls in self.native:
            with self.assertRaises(ValueError): partition.core_partition([r for r in self.rows if r[1].rsplit('.', 1)[0] != cls])

    def test_exact_native_classes_bind_execution_filters(self):
        plan = partition.core_partition(self.rows); filters = partition.partition_filters('core-c', plan)
        self.assertEqual(filters, ['--filter-class', *CoreThirdShardTests.classes])
        for cls in self.native:self.assertIn(cls, partition.partition_filters('core-b', plan))
        self.assertEqual(partition.execution_command('core-c', Path('/tmp/control/core-c'), plan)[-len(filters):], filters)

class CoreFourthShardTests(unittest.TestCase):
    classes = (
        'Cna.Core.Tests.Campaigns.CombatActualMovedCycleControlTests',
        'Cna.Core.Tests.Campaigns.CombatActualMovedCycleControlContractTests',
        'Cna.Core.Tests.Campaigns.CombatActualMovedAssaultContractTests',
        'Cna.Core.Tests.Campaigns.CombatActualPostAssaultContinuationRetryTests',
    )

    def setUp(self):
        fixture = CoreRowPartitionTests(); fixture.setUp()
        self.rows = fixture.rows
        self.native = {('Cna.Core.Tests', cls + '.Initial', cls + '.Initial(index: 0)') for cls in self.classes}
        self.rows += sorted(self.native - set(self.rows))
        self.expected_a = {r for r in self.rows if r[1] in partition.SELECTORS and r[2] not in fixture.transfer}
        self.expected_c = {r for r in self.rows if r[1].rsplit('.', 1)[0] in CoreThirdShardTests.classes}

    def test_complete_n3_classes_move_to_required_d_without_changing_a_c_or_transfer(self):
        plan = partition.core_partition(self.rows)
        self.assertIsNotNone(plan.get('core-d'), 'Measured N3 needs a required complete-row shard')
        self.assertEqual(plan['core-d'], self.native)
        self.assertEqual(plan['core-a'], self.expected_a); self.assertEqual(plan['core-c'], self.expected_c)
        shards = [plan[k] for k in ['core-a', 'core-b', 'core-c', 'core-d']]
        self.assertEqual(set.union(*shards), set(self.rows))
        for i, rows in enumerate(shards):
            for other in shards[i + 1:]: self.assertFalse(rows & other)

    def test_missing_n3_selected_class_fails_closed(self):
        for cls in self.classes:
            with self.assertRaises(ValueError): partition.core_partition([r for r in self.rows if r[1].rsplit('.', 1)[0] != cls])

    def test_future_methods_follow_exact_class_and_similar_class_remains_b(self):
        for cls in self.classes:
            method = cls + '.Future'; row = ('Cna.Core.Tests', method, method + '(index: 7)')
            plan = partition.core_partition(self.rows + [row]); self.assertIn(row, plan.get('core-d', set()))
            similar = cls + 'Extra.Future'; extra = ('Cna.Core.Tests', similar, similar)
            plan = partition.core_partition(self.rows + [extra]); self.assertIn(extra, plan['core-b'])

    def test_d_is_required_module_and_filters_bind_same_exact_class_policy(self):
        plan = partition.core_partition(self.rows)
        self.assertEqual(partition.MODULES.get('core-d'), 'Cna.Core.Tests')
        self.assertEqual(partition.partition_filters('core-d', plan), ['--filter-class', *self.classes])
        b = partition.partition_filters('core-b', plan)
        for cls in self.classes: self.assertIn(cls, b)

class CoreFourthPreparedProofTests(unittest.TestCase):
    def setUp(self):
        fixture = DiscoveryTests(); fixture.setUp()
        self.inventory = fixture.check()

    def test_exact_prepared_four_core_partition_is_accepted(self):
        partition.load_partition(self.inventory, SHA, '10.0.400', 'local')

    def test_missing_or_wrong_d_assignment_fails_even_when_union_is_unchanged(self):
        bad = copy.deepcopy(self.inventory); del bad['shards']['core-d']
        with self.assertRaises(ValueError): partition.load_partition(bad, SHA, '10.0.400', 'local')
        bad = copy.deepcopy(self.inventory); row = bad['shards']['core-d']['rows'].pop(); bad['shards']['core-b']['rows'].append(row)
        with self.assertRaises(ValueError): partition.load_partition(bad, SHA, '10.0.400', 'local')

    def test_actual_discovery_missing_overlap_or_swapped_d_rows_fails(self):
        for kind in ['missing', 'overlap', 'swap']:
            fixture = DiscoveryTests(); fixture.setUp()
            if kind == 'missing': del fixture.texts['core-d']
            elif kind == 'overlap': fixture.texts['core-d'] = fixture.texts['core-b']
            else: fixture.texts['core-d'], fixture.texts['core-b'] = fixture.texts['core-b'], fixture.texts['core-d']
            with self.assertRaises((ValueError, KeyError)): fixture.check()

    def test_d_execution_requires_current_partition_and_exact_classes(self):
        plan = partition.load_partition(self.inventory, SHA, '10.0.400', 'local')
        with self.assertRaises(ValueError): partition.execution_command('core-d', Path('/tmp/control/core-d'))
        command = partition.execution_command('core-d', Path('/tmp/control/core-d'), plan)
        self.assertEqual(command[-(len(CoreFourthShardTests.classes) + 1):], ['--filter-class', *CoreFourthShardTests.classes])

class CoreFourthCoverageFailureTests(unittest.TestCase):
    def setUp(self):
        self.fixture = CoverageTests(); self.fixture.setUp(); self.addCleanup(self.fixture.doCleanups)
        self.leaf = self.fixture.root / 'core-d'

    def test_required_d_missing_leaf_process_or_xml_is_not_coverage(self):
        for relative in ['status.json', 'results/report.xunit.xml']:
            path = self.leaf / relative; data = path.read_bytes(); path.unlink()
            with self.assertRaises(ValueError): self.fixture.check()
            path.write_bytes(data)

    def test_d_nonzero_stale_sha_attempt_and_debug_never_satisfy_coverage(self):
        path = self.leaf / 'status.json'; original = json.loads(path.read_text())
        for key, value in [('exit_code', 1), ('tested_sha', 'b' * 40), ('run_attempt', '1'), ('configuration', 'Debug')]:
            changed = copy.deepcopy(original); changed[key] = value; path.write_text(json.dumps(changed))
            with self.assertRaises(ValueError): self.fixture.check()
        path.write_text(json.dumps(original)); self.fixture.check()

    def test_d_skipped_failed_duplicate_or_foreign_report_fails_closed(self):
        path = self.leaf / 'results/report.xunit.xml'; original = path.read_bytes()
        for kind in ['skip', 'fail', 'duplicate', 'foreign']:
            document = ET.parse(path); assembly = document.getroot().find('assembly'); test = assembly.find('.//test')
            if kind == 'skip': test.set('result', 'Skip'); assembly.set('passed', '0'); assembly.set('skipped', '1')
            elif kind == 'fail': test.set('result', 'Fail'); assembly.set('passed', '0'); assembly.set('failed', '1')
            elif kind == 'duplicate': assembly.find('collection').append(copy.deepcopy(test)); assembly.set('total', '2'); assembly.set('passed', '2')
            else: test.set('method', 'Foreign')
            document.write(path)
            with self.assertRaises(ValueError): self.fixture.check()
            path.write_bytes(original)

class MeasuredCoreBFinalizationBudgetTests(unittest.TestCase):
    def test_finite_b_workload_has_bounded_setup_execution_and_report_margin(self):
        workflow = (Path(__file__).parents[1] / 'workflows' / 'ci.yml').read_text()
        leaf = workflow.split('  tests:', 1)[1].split('  verify:', 1)[0]
        expression = re.search(r'^    timeout-minutes: (.+)$', leaf, re.MULTILINE).group(1)
        match = re.fullmatch(r"\$\{\{ matrix\.shard == 'core-b' && ([0-9]+) \|\| ([0-9]+) \}\}", expression)
        self.assertIsNotNone(match)
        self.assertEqual(tuple(map(int, match.groups())), (25, 15))
        self.assertGreaterEqual(int(match[1]) * 60, 1204 * 1.15 + 60)
        self.assertEqual(set(partition.MODULES), {'core-a', 'core-b', 'core-c', 'core-d', 'exercise', 'contracts'})

class MeasuredClassBalanceTests(unittest.TestCase):
    moves = {
        'core-c': (
            'Cna.Core.Tests.Campaigns.CombatActualMovedContinuationContractTests',
            'Cna.Core.Tests.Campaigns.CombatActualRepeatedMovementContractTests',
            'Cna.Core.Tests.Campaigns.CombatActualPaidAssaultContractTests',
        ),
        'core-d': (
            'Cna.Core.Tests.Campaigns.CombatActualMovedAssaultContractTests',
            'Cna.Core.Tests.Campaigns.CombatActualPostAssaultContinuationRetryTests',
        ),
    }

    def setUp(self):
        fixture = CoreRowPartitionTests(); fixture.setUp()
        self.rows = list(fixture.rows)
        self.moved = {}
        for shard, classes in self.moves.items():
            for cls in classes:
                methods = [cls + '.Initial', cls + '.Future']
                rows = {('Cna.Core.Tests', method, method + '(index: ' + str(index) + ')')
                        for method in methods for index in [0, 7]}
                self.rows = [row for row in self.rows if row[1].rsplit('.', 1)[0] != cls]
                self.rows.extend(sorted(rows)); self.moved[cls] = rows

    def test_measured_classes_move_whole_without_changing_a_or_old_transfer(self):
        plan = partition.core_partition(self.rows)
        for shard, classes in self.moves.items():
            for cls in classes:
                with self.subTest(cls=cls):
                    self.assertTrue(self.moved[cls] <= plan[shard])
                    for other in partition.CORE_SHARDS:
                        if other != shard: self.assertFalse(self.moved[cls] & plan[other])
        self.assertEqual(plan['core-a'], {row for row in self.rows if row[1] in partition.SELECTORS and row[2] not in set(plan['transferred'])})
        parts = [plan[shard] for shard in partition.CORE_SHARDS]
        self.assertEqual(set.union(*parts), set(self.rows))
        for i, rows in enumerate(parts):
            for other in parts[i + 1:]: self.assertFalse(rows & other)
        self.assertEqual(plan, partition.core_partition(list(reversed(self.rows))))

    def test_measured_classes_bind_exact_include_and_complement_filters(self):
        plan = partition.core_partition(self.rows)
        excluded = partition.partition_filters('core-b', plan)
        excluded = excluded[excluded.index('--filter-not-class') + 1:]
        for shard, classes in self.moves.items():
            included = partition.partition_filters(shard, plan)
            self.assertEqual(included[0], '--filter-class')
            for cls in classes:
                self.assertIn(cls, included[1:]); self.assertIn(cls, excluded)
            self.assertTrue(all('*' not in cls for cls in included[1:]))

    def test_unknown_and_similarly_named_classes_still_enter_b(self):
        for classes in self.moves.values():
            for cls in classes:
                method = cls + 'Extra.NewMethod'
                row = ('Cna.Core.Tests', method, method + '(index: 9)')
                plan = partition.core_partition(self.rows + [row])
                self.assertIn(row, plan['core-b'])
                self.assertEqual(sum(row in plan[s] for s in partition.CORE_SHARDS), 1)

    def test_missing_measured_class_fails_closed(self):
        for classes in self.moves.values():
            for cls in classes:
                with self.subTest(cls=cls), self.assertRaises(ValueError):
                    partition.core_partition([r for r in self.rows if r[1].rsplit('.', 1)[0] != cls])

if __name__ == '__main__': unittest.main()
