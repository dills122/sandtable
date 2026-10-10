import copy
import fnmatch
import re
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
import xml.etree.ElementTree as ET

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
        modules = {'core-a': 'Cna.Core.Tests', 'core-b': 'Cna.Core.Tests', 'exercise': 'Cna.ExerciseRunner.Tests', 'contracts': 'Cna.Intelligence.Contracts.Tests'}
        self.inventory = {'schema_version': 1, 'tested_sha': SHA, 'sdk': '10.0.400', 'configuration': 'Release', 'run_attempt': 'local', 'shards': {}}
        for shard, module in modules.items():
            method = module + '.Cases.' + ('Alpha' if shard == 'core-a' else 'Beta')
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

class DiscoveryTests(unittest.TestCase):
    def setUp(self):
        self.control_methods = ['Cna.Core.Tests.Campaigns.CombatActualRoundEntryTests.NativeActualEntryRetainsExactPreparedAndCancelledTerminalProofs', 'Cna.Core.Tests.Campaigns.BreakdownRecordTests.InvalidFiniteShapesAndNoncanonicalBytesReject']
        self.controls = [method + '(value: "quoted")' for method in self.control_methods]
        self.payload = {'schema_version': 1, 'runner_version': '4.0.1', 'providers': [{'method': method, 'provider': 'Reviewed', 'names': [method + '(value: "deferred")']} for method in sorted(partition.DEFERRED)], 'controls': [{'method': method, 'names': [name]} for method, name in zip(self.control_methods, self.controls)]}
        a = [method + '(index: 0)' for method in partition.SELECTORS]
        b = sorted(partition.DEFERRED) + self.controls
        self.texts = {'core': self.text(a + b), 'core-a': self.text(a), 'core-b': self.text(b), 'exercise': self.text(['Cna.ExerciseRunner.Tests.Example.Fact']), 'contracts': self.text(['Cna.Intelligence.Contracts.Tests.Example.Fact'])}

    @staticmethod
    def text(names):
        return f'Discovered {len(names)} tests in assembly - release\n' + ''.join('  ' + name + '\n' for name in names) + f'Discovered {len(names)} tests.\n'

    def check(self):
        return partition.build_inventory(self.texts, self.payload, SHA, '10.0.400')

    def test_complete_discovery(self):
        inventory = self.check()
        self.assertEqual(len(inventory['shards']['core-a']['rows']), 12)
        self.assertEqual(len(inventory['shards']['core-b']['rows']), 7)

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

    def test_runner_upgrade_fails(self):
        self.payload['runner_version'] = '4.0.2'
        with self.assertRaises(ValueError): self.check()

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

class DiagnosticCommandTests(unittest.TestCase):
    def test_supported_trace_and_synchronous_write(self):
        for shard in partition.MODULES:
            with self.subTest(shard=shard):
                command = partition.execution_command(shard, Path('/tmp/control') / shard)
                self.assertIn('--diagnostic', command)
                self.assertIn('--diagnostic-synchronous-write', command)
                self.assertEqual(command[command.index('--diagnostic-verbosity') + 1], 'Trace')
                self.assertEqual(command[command.index('--output') + 1], 'Detailed')

    def test_diagnostics_stay_inside_uploaded_leaf_directory(self):
        for shard in partition.MODULES:
            with self.subTest(shard=shard):
                directory = Path('/tmp/artifacts/ci') / shard
                command = partition.execution_command(shard, directory)
                path = Path(command[command.index('--diagnostic-output-directory') + 1])
                self.assertEqual(path, (directory / 'diagnostics').resolve())
                workflow = (Path(__file__).parents[1] / 'workflows' / 'ci.yml').read_text()
                leaf = workflow.split('  tests:', 1)[1].split('  verify:', 1)[0]
                self.assertIn('if: ${{ always() }}', leaf)
                self.assertIn('path: artifacts/ci', leaf)

    def test_whole_method_filters_and_acceptance_reports_preserved(self):
        for shard, module in partition.MODULES.items():
            with self.subTest(shard=shard):
                command = partition.execution_command(shard, Path('/tmp/control') / shard)
                self.assertEqual(command[:2], ['dotnet', 'test'])
                self.assertEqual(command[command.index('--project') + 1], partition.project(module))
                self.assertIn('--report-xunit-xml', command)
                if shard.startswith('core-'):
                    key = '--filter-method' if shard == 'core-a' else '--filter-not-method'
                    self.assertEqual(command[command.index(key) + 1:], list(partition.SELECTORS))
                else:
                    self.assertNotIn('--filter-method', command)
                    self.assertNotIn('--filter-not-method', command)
                self.assertNotIn('--max-threads', command)
                self.assertNotIn('--timeout', command)

if __name__ == '__main__': unittest.main()
