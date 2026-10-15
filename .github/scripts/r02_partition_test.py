"""Failure-sensitive checks for the exhaustive R02 process allocation candidate."""
import copy
import importlib.util
import json
from pathlib import Path
import tempfile
import shutil
import subprocess
import sys
import xml.etree.ElementTree as ET
import unittest
from unittest import mock

spec = importlib.util.spec_from_file_location('r02_partition', Path(__file__).with_name('r02_partition.py'))
r02 = importlib.util.module_from_spec(spec)
spec.loader.exec_module(r02)
PREFIX = 'Cna.Core.Tests.Campaigns.CombatActualMovedRepeatMovementCompletion'


def fixture():
    rows = []
    for profile, side, start, stop, count in [('paid', 'axis', 0, 6, 3), ('paid', 'commonwealth', 6, 12, 3), ('armed', 'axis', 12, 34, 6), ('armed', 'commonwealth', 34, 56, 6)]:
        for ordinal in range(start, stop):
            for cut in range(1, count + 1):
                rows.append({'rootOrdinal': ordinal, 'movementCut': cut, 'profile': profile, 'group': f'{profile}-{side}-m{cut - 1}', 'rowKey': f'r{ordinal:03}-c{cut:02}'})
    support = []
    four = [(0, 1, 'paid'), (0, 3, 'paid'), (12, 1, 'armed'), (12, 6, 'armed')]
    for name, method, cases in [
        ('Tests', 'AuthenticPublishedMovementCompletesLifecycle', four),
        ('ContractTests', 'EveryCutContextActorCapabilityAndOrderReject', four),
        ('ContractTests', 'SourceControlProofReadersRejectForgedClaims', four),
        ('ContractTests', 'CatalogFullPositionsAndBothProofsStayDistinct', four),
        ('ContractTests', 'TenIndependentLedgersAndResignedEventsCannotAuthenticate', [four[1], four[3]]),
        ('ContractTests', 'OwnedBeforeCallbackAndAllReturnedBuffers', [four[1], four[3]]),
    ]:
        for values in cases:
            support.append({'className': PREFIX + name, 'methodName': method, 'arguments': [{'name': name, 'value': value} for name, value in zip(('rootOrdinal', 'movementCut', 'profile'), values)]})
    for method in ['AllPhysicalPinsRemainRequiredAfterWarmReplay', 'EveryHandlerAndCapacityRequired', 'ClosedGrammarAndSizeFraming', 'WholeTraceCapacityAndHistoricalExclusionAreBounded', 'FullSourceFramingCannotForgeParent']:
        support.append({'className': PREFIX + 'ContractTests', 'methodName': method, 'arguments': []})
    return {'semanticRows': rows, 'supportRows': support, 'supportObligations': {'physicalPins': 106, 'physicalFaultModes': 5, 'physicalPinFaults': 530, 'handlers': 14, 'limitEntries': 25, 'handlerCapacityOmissions': 156, 'ledgerFaults': 60, 'resignedEvents': 6, 'claimFaults': 24, 'contextFaults': 72, 'isolatedBoundaries': 8, 'grammarFramingOwnershipChecks': 88}}



class SemanticManifestTests(unittest.TestCase):
    def test_complete_manifest_preserves_each_work_identity(self):
        result = r02.semantic_manifest(fixture())
        self.assertEqual((len(result['rows']), len(result['work_ids'])), (300, 3696))
        self.assertEqual(result['counts'], {'rows': 300, 'events': 788, 'cuts': 1088, 'retries': 1520})
        self.assertEqual(len(result['groups']), 20)
        self.assertEqual(result['rows']['r012-c06']['group'], 'armed-axis-m5')

    def test_missing_whole_root_cannot_pass_with_aggregate_counts(self):
        data = fixture(); data['semanticRows'].pop()
        with self.assertRaises(ValueError): r02.semantic_manifest(data)

    def test_duplicate_replacing_root_cannot_pass(self):
        data = fixture(); data['semanticRows'][-1] = copy.deepcopy(data['semanticRows'][0])
        with self.assertRaises(ValueError): r02.semantic_manifest(data)

    def test_forged_group_profile_cut_or_key_fails(self):
        for field, value in [('profile', 'armed'), ('group', 'paid-commonwealth-m0'), ('movementCut', 0), ('rootOrdinal', True), ('rowKey', '../r000-c01')]:
            with self.subTest(field=field):
                data = fixture(); data['semanticRows'][0][field] = value
                with self.assertRaises(ValueError): r02.semantic_manifest(data)

    def test_focused_loop_obligations_cannot_be_omitted_reduced_or_forged(self):
        for kind in ['missing', 'reduced', 'boolean', 'extra']:
            data = fixture()
            if kind == 'missing': data.pop('supportObligations')
            if kind == 'reduced': data['supportObligations']['physicalPinFaults'] = 529
            if kind == 'boolean': data['supportObligations']['physicalFaultModes'] = True
            if kind == 'extra': data['supportObligations']['unknown'] = 1
            with self.subTest(kind=kind), self.assertRaises(ValueError): r02.semantic_manifest(data)

    def test_one_missing_support_case_or_unknown_method_fails(self):
        for kind in ['missing', 'method']:
            data = fixture()
            if kind == 'missing': data['supportRows'].pop()
            else: data['supportRows'][0]['methodName'] = 'UnfrozenReplacement'
            with self.subTest(kind=kind), self.assertRaises(ValueError): r02.semantic_manifest(data)

    def test_unfrozen_or_duplicate_support_identity_fails(self):
        for mutation in ['empty', 'duplicate', 'unknown', 'complex']:
            data = fixture()
            if mutation == 'empty': data['supportRows'] = []
            if mutation == 'duplicate': data['supportRows'].append(copy.deepcopy(data['supportRows'][0]))
            if mutation == 'unknown': data['supportRows'][0]['className'] += 'Unknown'
            if mutation == 'complex': data['supportRows'][0]['arguments'][0]['value'] = {'hidden': 1}
            with self.subTest(mutation=mutation), self.assertRaises(ValueError): r02.semantic_manifest(data)


class DiscoveryConservationTests(unittest.TestCase):
    def setUp(self):
        self.semantic = r02.semantic_manifest(fixture())
        self.groups = {key: entry['rows'] for key, entry in self.semantic['groups'].items()}
        self.old = ('Cna.Core.Tests', 'Cna.Core.Tests.Campaigns.Existing.Pass', 'Cna.Core.Tests.Campaigns.Existing.Pass')
        self.full = [self.old] + [row for values in self.groups.values() for row in values]

    def test_actual_filters_preserve_complete_new_and_old_union(self):
        result = r02.reconcile_discovery(self.semantic, self.full, self.groups)
        self.assertEqual(result, {self.old})

    def test_missing_extra_overlap_or_unregistered_group_fails(self):
        for kind in ['missing-group', 'extra-group', 'overlap', 'omission', 'unknown-class']:
            groups = copy.deepcopy(self.groups); full = copy.deepcopy(self.full)
            key = next(iter(groups))
            if kind == 'missing-group': groups.pop(key)
            if kind == 'extra-group': groups['foreign'] = groups[key]
            if kind == 'overlap': groups[key].append(next(iter(groups.values()))[0])
            if kind == 'omission': full.pop()
            if kind == 'unknown-class': full.append(('Cna.Core.Tests', PREFIX + 'Unknown.Pass', PREFIX + 'Unknown.Pass'))
            with self.subTest(kind=kind), self.assertRaises(ValueError): r02.reconcile_discovery(self.semantic, full, groups)

    def test_formatter_or_metadata_argument_drift_fails(self):
        groups = copy.deepcopy(self.groups); key = next(iter(groups)); row = groups[key][0]
        groups[key][0] = (row[0], row[1], row[2].replace('rootOrdinal: 0', 'rootOrdinal: 999'))
        with self.assertRaises(ValueError): r02.reconcile_discovery(self.semantic, self.full, groups)


class ChildEvidenceTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(); self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name); self.directory = self.root / 'paid-axis-m0'
        self.directory.mkdir(); (self.directory / 'results').mkdir()
        self.group = 'paid-axis-m0'
        self.entry = r02.semantic_manifest(fixture())['groups'][self.group]
        self.binding = {'tested_sha': 'a' * 40, 'run_attempt': '1', 'sdk': '10.0.402', 'configuration': 'Release', 'run_id': '123456', 'module_sha256': 'b' * 64, 'fixture_sha256': 'c' * 64}
        self.status = self.good_status()
        self.write_status()
        xml = ET.Element('assemblies')
        assembly = ET.SubElement(xml, 'assembly', name='/bin/Cna.Core.Tests.dll', total='6', passed='6', failed='0', skipped='0', errors='0', **{'not-run': '0', 'target-framework': '.NETCoreApp,Version=v10.0'})
        collection = ET.SubElement(assembly, 'collection')
        for module, method, name in self.entry['rows']:
            ET.SubElement(collection, 'test', name=json.dumps(name)[1:-1], type=method.rsplit('.', 1)[0], method=method.rsplit('.', 1)[1], result='Pass')
        ET.ElementTree(xml).write(self.directory / 'results' / 'report.xml')

    def good_status(self):
        from ci_group_runner import execution_command
        return dict(self.binding, group=self.group, module='Cna.Core.Tests', class_name=self.entry['class'], exit_code=0,
            evidence_version=1, phase=r02.candidate_allocation()[self.group]['phase'], process_id=123, cancelled=False, elapsed_seconds=1.0,
            started_utc='2026-10-15T20:00:00+00:00', completed_utc='2026-10-15T20:00:01+00:00',
            module_path='/bin/Cna.Core.Tests.dll', output_directory=str(self.directory.resolve()),
            command=execution_command('/bin/Cna.Core.Tests.dll', self.directory, self.entry['class']))

    def test_cancelled_or_missing_process_trace_cannot_pass(self):
        for field, value in [('cancelled', True), ('process_id', None), ('command', ['dotnet', 'test']), ('module_path', '/bin/Other.dll'), ('output_directory', '/foreign/other-group'), ('elapsed_seconds', float('nan')), ('started_utc', 'yesterday'), ('evidence_version', 2)]:
            self.status = self.good_status(); self.status[field] = value; self.write_status()
            with self.subTest(field=field), self.assertRaises(ValueError): self.check()

    def write_status(self): (self.directory / 'status.json').write_text(json.dumps(self.status))
    def check(self): return r02.verify_child(self.group, self.entry, self.directory, self.binding)

    def test_exact_child_process_and_xml_pass(self): self.assertEqual(len(self.check()), 6)

    def test_stale_failed_or_foreign_process_cannot_pass_with_good_xml(self):
        for field, value in [('tested_sha', 'd' * 40), ('run_attempt', '2'), ('sdk', '10.0.400'), ('configuration', 'Debug'), ('module_sha256', 'd' * 64), ('fixture_sha256', 'd' * 64), ('group', 'paid-axis-m1'), ('module', 'Other'), ('class_name', PREFIX + 'Unknown'), ('exit_code', 1), ('exit_code', True)]:
            with self.subTest(field=field, value=value):
                self.status = self.good_status()
                self.status[field] = value; self.write_status()
                with self.assertRaises(ValueError): self.check()

    def test_unattributed_test_outside_only_assembly_fails(self):
        path = self.directory / 'results' / 'report.xml'
        document = ET.parse(path); ET.SubElement(document.getroot(), 'test', name='unattributed', result='Pass'); document.write(path)
        with self.assertRaises(ValueError): self.check()

    def test_only_one_immediate_regular_lowercase_xml_report_is_supported(self):
        results = self.directory / 'results'; report = results / 'report.xml'; original = report.read_bytes()
        for kind in ['nested-failed', 'nested-duplicate', 'uppercase-extra', 'uppercase-only', 'nested-only', 'non-xml-extra', 'empty-directory', 'symlink-report', 'symlink-results']:
            with self.subTest(kind=kind):
                if results.is_symlink(): results.unlink()
                elif results.exists(): shutil.rmtree(results)
                results.mkdir(); report.write_bytes(original)
                if kind.startswith('nested'):
                    nested = results / 'unexpected'; nested.mkdir()
                    extra = nested / 'extra.xml'; extra.write_bytes(original)
                    if kind == 'nested-failed':
                        document = ET.parse(extra); document.getroot().find('assembly').set('name', '/bin/Foreign.dll'); document.getroot().find('.//test').set('result', 'Fail'); document.write(extra)
                    if kind == 'nested-only': report.unlink()
                elif kind.startswith('uppercase'):
                    (results / 'extra.XML').write_bytes(original)
                    if kind == 'uppercase-only': report.unlink()
                elif kind == 'non-xml-extra': (results / 'extra.txt').write_text('unexpected')
                elif kind == 'empty-directory': (results / 'unexpected').mkdir()
                elif kind == 'symlink-report':
                    source = self.root / 'external.xml'; source.write_bytes(original); report.unlink(); report.symlink_to(source)
                else:
                    external = self.root / 'external-results'; external.mkdir(); (external / 'report.xml').write_bytes(original); shutil.rmtree(results); results.symlink_to(external, target_is_directory=True)
                with self.assertRaises(ValueError): self.check()

    def test_missing_duplicate_or_failed_xml_fails(self):
        path = self.directory / 'results' / 'report.xml'; original = path.read_bytes()
        for kind in ['missing', 'duplicate', 'failure', 'wrong-row']:
            path.write_bytes(original)
            if kind == 'missing': path.unlink()
            elif kind == 'duplicate': (path.parent / 'extra.xml').write_bytes(original)
            else:
                doc = ET.parse(path); test = doc.getroot().find('.//test')
                test.set('result', 'Fail') if kind == 'failure' else test.set('name', 'foreign')
                doc.write(path)
            with self.subTest(kind=kind), self.assertRaises(ValueError): self.check()
            (path.parent / 'extra.xml').unlink(missing_ok=True)


class ProcessAllocationTests(unittest.TestCase):
    def setUp(self): self.semantic = r02.semantic_manifest(fixture())

    def test_all_groups_have_one_static_job_and_lane(self):
        allocation = r02.candidate_allocation()
        self.assertEqual(set(allocation), set(self.semantic['groups']))
        self.assertEqual({entry['job'] for entry in allocation.values()}, {'r02-1', 'r02-2', 'r02-3'})
        self.assertTrue(all(type(entry['lane']) is int and 1 <= entry['lane'] <= 4 for entry in allocation.values()))
        self.assertEqual(allocation['armed-axis-m1'], {'job': 'r02-2', 'lane': 2, 'phase': 'parallel'})
        self.assertEqual(allocation['support-contract'], {'job': 'r02-1', 'lane': 1, 'phase': 'exclusive'})

    def test_missing_extra_or_invalid_allocation_fails(self):
        for kind in ['missing', 'extra', 'job', 'lane', 'boolean']:
            data = r02.candidate_allocation(); key = next(iter(data))
            if kind == 'missing': data.pop(key)
            if kind == 'extra': data['unknown'] = {'job': 'r02-1', 'lane': 1}
            if kind == 'job': data[key]['job'] = 'core-b'
            if kind == 'lane': data[key]['lane'] = 5
            if kind == 'boolean': data[key]['lane'] = True
            with self.subTest(kind=kind), self.assertRaises(ValueError): r02.validate_allocation(data, self.semantic)

    def test_complete_group_directory_set_required(self):
        with tempfile.TemporaryDirectory() as root:
            for name in self.semantic['groups']: (Path(root) / name).mkdir()
            self.assertTrue(r02.check_group_directories(root, self.semantic))
            (Path(root) / 'unexpected').mkdir()
            with self.assertRaises(ValueError): r02.check_group_directories(root, self.semantic)


class PreparedProofTests(unittest.TestCase):
    def setUp(self):
        self.fixture = fixture(); self.semantic = r02.semantic_manifest(self.fixture)
        self.groups = {group: entry['rows'] for group, entry in self.semantic['groups'].items()}
        self.old = ('Cna.Core.Tests', 'Cna.Core.Tests.Cases.Old.Pass', 'Cna.Core.Tests.Cases.Old.Pass')
        self.full = [self.old] + [row for rows in self.groups.values() for row in rows]
        self.binding = {'tested_sha': 'a' * 40, 'run_attempt': '1', 'sdk': '10.0.402', 'configuration': 'Release', 'run_id': '123456', 'module_sha256': 'b' * 64, 'fixture_sha256': 'c' * 64}

    def test_fresh_proof_reconstructs_exact_semantic_actual_and_allocation(self):
        proof = r02.make_proof(self.fixture, self.full, self.groups, self.binding)
        self.assertEqual(len(proof['groups']), 20)
        self.assertEqual(sum(len(entry['rows']) for entry in proof['groups'].values()), 325)
        self.assertEqual(r02.load_proof(proof, self.fixture, self.binding), proof)

    def test_workflow_run_id_prevents_same_sha_and_attempt_cross_run_mix(self):
        binding = dict(self.binding, run_id='123456')
        proof = r02.make_proof(self.fixture, self.full, self.groups, binding)
        self.assertEqual(r02.load_proof(proof, self.fixture, binding), proof)
        with self.assertRaises(ValueError): r02.load_proof(proof, self.fixture, dict(binding, run_id='123457'))

    def test_altered_prepared_rows_allocation_or_provenance_rejects(self):
        proof = r02.make_proof(self.fixture, self.full, self.groups, self.binding)
        for kind in ['row', 'allocation', 'fingerprint', 'attempt', 'old-row']:
            data = copy.deepcopy(proof)
            if kind == 'row': next(iter(data['groups'].values()))['rows'].pop()
            if kind == 'allocation': next(iter(data['groups'].values()))['lane'] = 4
            if kind == 'fingerprint': data['semantic_sha256'] = 'd' * 64
            if kind == 'attempt': data['binding']['run_attempt'] = '2'
            if kind == 'old-row': data['full_core_rows'].pop(0)
            with self.subTest(kind=kind), self.assertRaises(ValueError): r02.load_proof(data, self.fixture, self.binding)


class LegacyMappingIntegrationTests(unittest.TestCase):
    def test_new_class_exclusions_preserve_every_old_assignment(self):
        import test_partition as legacy
        from test_partition_test import DiscoveryTests
        source = DiscoveryTests(); source.setUp(); baseline = source.check()
        semantic = r02.semantic_manifest(fixture())
        new_rows = [row for entry in semantic['groups'].values() for row in entry['rows']]
        names = [line[2:] for line in source.texts['core'].splitlines() if line.startswith('  ')]
        source.texts['core'] = source.text(names + [row[2] for row in new_rows])
        actual = legacy.build_inventory(source.texts, source.payload, 'a' * 40, '10.0.400', excluded_rows=new_rows)
        self.assertEqual(actual['shards'], baseline['shards'])
        plan = legacy.load_partition(actual, 'a' * 40, '10.0.400', 'local')
        filters = legacy.partition_filters('core-b', plan)
        self.assertTrue(set(r02.registered_classes()) <= set(filters))
        for shard in ('core-a', 'core-c', 'core-d'):
            self.assertEqual(legacy.partition_filters(shard, plan), legacy.partition_filters(shard, legacy.load_partition(baseline, 'a' * 40, '10.0.400', 'local')))


class CombinedConservationTests(unittest.TestCase):
    def setUp(self):
        import test_partition as legacy
        from test_partition_test import DiscoveryTests
        self.tmp = tempfile.TemporaryDirectory(); self.addCleanup(self.tmp.cleanup); self.root = Path(self.tmp.name)
        source = DiscoveryTests(); source.setUp(); self.inventory = source.check(); self.inventory['sdk'] = '10.0.402'; self.inventory['run_attempt'] = '1'
        self.inventory['excluded_classes'] = r02.registered_classes()
        self.fixture = fixture(); self.semantic = r02.semantic_manifest(self.fixture)
        self.binding = {'tested_sha': 'a' * 40, 'run_attempt': '1', 'sdk': '10.0.402', 'configuration': 'Release', 'run_id': '123456', 'module_sha256': 'b' * 64, 'fixture_sha256': 'c' * 64}
        self.needs = {'prepare': {'result': 'success'}, 'tests': {'result': 'success'}}
        core = [row for shard, entry in self.inventory['shards'].items() if shard.startswith('core-') for row in entry['rows']]
        groups = {group: entry['rows'] for group, entry in self.semantic['groups'].items()}
        self.proof = r02.make_proof(self.fixture, core + [row for values in groups.values() for row in values], groups, self.binding)
        for shard, entry in self.inventory['shards'].items():
            directory = self.root / shard; directory.mkdir()
            self.report(directory, entry['module'], entry['rows'])
            (directory / 'status.json').write_text(json.dumps({'shard': shard, 'module': entry['module'], 'tested_sha': 'a' * 40, 'run_attempt': '1', 'sdk': '10.0.402', 'configuration': 'Release', 'exit_code': 0}))
        for index, (group, entry) in enumerate(self.semantic['groups'].items()):
            directory = self.root / 'r02-groups' / group; directory.mkdir(parents=True)
            self.report(directory, 'Cna.Core.Tests', entry['rows'])
            child = ChildEvidenceTests(); child.binding = self.binding; child.group = group; child.entry = entry; child.directory = directory
            status = child.good_status(); status['process_id'] += index
            if group == 'support-contract': status.update(started_utc='2026-10-15T20:00:02+00:00', completed_utc='2026-10-15T20:00:03+00:00')
            (directory / 'status.json').write_text(json.dumps(status))

        from ci_group_runner import RESOURCE_POLICY
        for job in ('r02-1', 'r02-2', 'r02-3'):
            snapshot = {'platform': 'Linux', 'effective_memory_limit_bytes': 16 * 2**30, 'memory_available_bytes': 12 * 2**30, 'effective_cpu_count': 4}
            groups = {group: 1024 for group, entry in self.proof['groups'].items() if entry['job'] == job}
            resource = {'schema_version': 1, 'job': job, 'binding': self.binding, 'policy': dict(RESOURCE_POLICY), 'phase_preflights': [snapshot] * (2 if job == 'r02-1' else 1), 'peak_group_rss_bytes': groups, 'peak_aggregate_rss_bytes': 4096, 'min_available_bytes': 12 * 2**30, 'sample_count': 10, 'abort_reason': None, 'kernel_children_peak_rss_bytes': 2048, 'kernel_peak_kind': 'job-wide completed-children maximum; not per-child attribution', 'complete': True, 'elapsed_seconds': 3}
            (self.root / (job + '-resource.json')).write_text(json.dumps(resource))

    @staticmethod
    def report(directory, module, rows):
        (directory / 'results').mkdir()
        xml = ET.Element('assemblies'); assembly = ET.SubElement(xml, 'assembly', name='/bin/' + module + '.dll', total=str(len(rows)), passed=str(len(rows)), failed='0', skipped='0', errors='0', **{'not-run': '0', 'target-framework': '.NETCoreApp,Version=v10.0'})
        collection = ET.SubElement(assembly, 'collection')
        for _, method, name in rows:
            ET.SubElement(collection, 'test', name=json.dumps(name)[1:-1], type=method.rsplit('.', 1)[0], method=method.rsplit('.', 1)[1], result='Pass')
        ET.ElementTree(xml).write(directory / 'results' / 'report.xml')

    def check(self): return r02.verify_combined(self.proof, self.fixture, self.inventory, self.root, self.needs, self.binding)

    def test_complete_old_and_new_processes_are_required_together(self):
        self.assertEqual(self.check()['r02_rows'], 325)

    def test_nested_foreign_failed_report_cannot_pass_combined_gate(self):
        nested = self.root / 'r02-groups' / 'support-contract' / 'results' / 'unexpected'
        nested.mkdir()
        (nested / 'foreign.xml').write_text('<assemblies><assembly name="Foreign.dll" failed="1"><collection><test result="Fail" /></collection></assembly></assemblies>')
        with self.assertRaises(ValueError): self.check()

    def test_missing_support_xml_or_old_process_fails_combined_gate(self):
        path = self.root / 'r02-groups' / 'support-contract' / 'results' / 'report.xml'
        original = path.read_bytes(); path.unlink()
        with self.assertRaises(ValueError): self.check()
        path.write_bytes(original)
        (self.root / 'core-b' / 'status.json').unlink()
        with self.assertRaises(ValueError): self.check()

    def test_missing_aborted_stale_resource_or_overlapped_exclusive_phase_fails(self):
        path = self.root / 'r02-1-resource.json'; original = path.read_bytes()
        for kind in ['missing', 'aborted', 'over-limit', 'stale', 'barrier']:
            path.write_bytes(original)
            data = json.loads(original)
            if kind == 'missing': path.unlink()
            elif kind == 'aborted': data['abort_reason'] = '4GiB exceeded'
            elif kind == 'over-limit': data['kernel_children_peak_rss_bytes'] = 4 * 2**30 + 1
            elif kind == 'stale': data['binding'] = dict(data['binding'], run_id='999')
            else:
                status_path = self.root / 'r02-groups' / 'support-contract' / 'status.json'; status = json.loads(status_path.read_text()); status['started_utc'] = '2026-10-15T20:00:00+00:00'; status_path.write_text(json.dumps(status))
            if kind not in ('missing', 'barrier'): path.write_text(json.dumps(data))
            with self.subTest(kind=kind), self.assertRaises(ValueError): self.check()

    def test_changed_legacy_mapping_or_failed_dependencies_fail(self):
        self.needs['tests']['result'] = 'failure'
        with self.assertRaises(ValueError): self.check()
        self.needs['tests']['result'] = 'success'
        row = self.inventory['shards']['core-b']['rows'].pop(); self.inventory['shards']['core-a']['rows'].append(row)
        with self.assertRaises(ValueError): self.check()


class DiscoveryCommandIntegrationTests(unittest.TestCase):
    def test_prepare_uses_actual_full_and_each_exact_class_discovery(self):
        import test_partition as legacy
        from test_partition_test import DiscoveryTests
        source = DiscoveryTests(); source.setUp(); semantic = r02.semantic_manifest(fixture())
        with tempfile.TemporaryDirectory() as temporary:
            temporary = Path(temporary); binary = temporary / 'bin'; binary.mkdir()
            fingerprints = {}
            import hashlib
            for name in ['Cna.Core.Tests.dll', 'Cna.Core.dll', 'xunit.v3.common.dll', 'xunit.v3.core.dll']:
                (binary / name).write_bytes(name.encode()); fingerprints[name] = hashlib.sha256(name.encode()).hexdigest()
            source.payload['fingerprints'] = fingerprints
            fixture_path = temporary / 'fixture.json'; fixture_path.write_text(json.dumps(fixture()))
            old_names = [line[2:] for line in source.texts['core'].splitlines() if line.startswith('  ')]
            full_text = source.text(old_names + [row[2] for entry in semantic['groups'].values() for row in entry['rows']])
            calls = []
            def logged(command, log):
                calls.append(command)
                log = Path(log)
                if log.name == 'provider-discovery.log':
                    Path(command[-1]).write_text(json.dumps(source.payload)); log.write_text('compiled provider proof'); return
                if log.name == 'core-discovery.log': text = full_text
                elif log.name.removesuffix('-discovery.log') in semantic['groups']:
                    text = source.text([row[2] for row in semantic['groups'][log.name.removesuffix('-discovery.log')]['rows']])
                else: text = source.texts[log.name.removesuffix('-discovery.log')]
                log.write_text(text)
            with mock.patch.object(legacy, 'run_logged', side_effect=logged):
                proof = r02.discover(temporary / 'evidence', fixture_path, 'a' * 40, '10.0.402', '1', binary)
            self.assertEqual(sum(len(entry['rows']) for entry in proof['groups'].values()), 325)
            inventory = json.loads((temporary / 'evidence' / 'inventory.json').read_text())
            self.assertEqual(inventory['shards'], json.loads(json.dumps(source.check()['shards'])))
            self.assertEqual(len([command for command in calls if '--filter-class' in command and command[-1] in r02.registered_classes()]), 20)
            self.assertTrue((temporary / 'evidence' / 'r02-inventory.json').exists())


class ExecuteGroupProofTests(unittest.TestCase):
    def setUp(self):
        self.data = CombinedConservationTests(); self.data.setUp(); self.addCleanup(self.data.tmp.cleanup)
        self.output = self.data.root / 'job-evidence'
        self.selected = {group for group, entry in self.data.proof['groups'].items() if entry['job'] == 'r02-1'}
        self.statuses = {}
        for group in self.selected:
            directory = self.output / 'r02-groups' / group
            shutil.copytree(self.data.root / 'r02-groups' / group, directory)
            self.statuses[group] = json.loads((directory / 'status.json').read_text())
        shutil.copyfile(self.data.root / 'r02-1-resource.json', self.output / 'r02-1-resource.json')

    def test_execute_accepts_only_expected_processes_and_complete_xml(self):
        import ci_group_runner
        with mock.patch.object(ci_group_runner, 'run_job', return_value=self.statuses):
            result = r02.execute_groups(self.data.proof, self.data.fixture, self.output, self.data.binding, 'r02-1', '/bin/Cna.Core.Tests.dll')
        self.assertEqual(result['groups'], 5)
        self.assertEqual(result['rows'], 109)

    def test_zero_exits_cannot_replace_missing_or_foreign_job_evidence(self):
        import ci_group_runner
        for kind in ['missing-xml', 'extra-process']:
            statuses = copy.deepcopy(self.statuses)
            if kind == 'missing-xml':
                path = self.output / 'r02-groups' / next(iter(self.selected)) / 'results' / 'report.xml'; original = path.read_bytes(); path.unlink()
            else: statuses['foreign'] = next(iter(statuses.values()))
            with mock.patch.object(ci_group_runner, 'run_job', return_value=statuses), self.subTest(kind=kind), self.assertRaises(ValueError):
                r02.execute_groups(self.data.proof, self.data.fixture, self.output, self.data.binding, 'r02-1', '/bin/Cna.Core.Tests.dll')
            if kind == 'missing-xml': path.write_bytes(original)


class WorkflowCandidateTests(unittest.TestCase):
    def test_candidate_keeps_bounded_concurrency_and_required_combined_gate(self):
        text = (Path(__file__).parents[1] / 'workflows' / 'ci.yml').read_text()
        self.assertIn('max-parallel: 6', text)
        for mode in ('discover', 'execute', 'verify'):
            self.assertIn('python3 .github/scripts/r02_partition.py ' + mode, text)
        for script in ('r02_partition_test.py', 'ci_group_runner_test.py'):
            self.assertIn('python3 .github/scripts/' + script, text)
        self.assertIn("if: ${{ always() }}", text.split('  verify:', 1)[1])


class CommandLineTests(unittest.TestCase):
    def test_help_exposes_staged_discovery_execution_and_verification(self):
        result = subprocess.run([sys.executable, str(Path(__file__).with_name('r02_partition.py')), '--help'], capture_output=True, text=True)
        self.assertEqual(result.returncode, 0)
        for flag in ('discover', 'execute', 'verify', '--fixture', '--inventory', '--binary-directory'):
            self.assertIn(flag, result.stdout)


if __name__ == '__main__': unittest.main()
