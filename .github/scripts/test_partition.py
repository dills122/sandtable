"""Complete, fail-closed CI coverage; row identity never uses cross-run XML IDs."""
import argparse
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

SELECTORS = (
    'Cna.Core.Tests.Campaigns.CombatActualRoundEntryTests.EveryResignedEventInputProofBaseAndControlLeafRejects',
    'Cna.Core.Tests.Campaigns.CombatActualSelectionTests.EveryFrozenCutSuffixRetryAndProofMatchesOriginalBytes',
    'Cna.Core.Tests.Campaigns.CombatClosureTests.EveryCutRetriesAndDiscardRemainStableIncludingClosedAndFreshCommandsReject',
    'Cna.Core.Tests.Campaigns.CombatInheritedCycleControlTests.ReadbackRejectsDeepMutationAndResignedEvents',
    'Cna.Core.Tests.Campaigns.CombatInheritedReserveMovementCompletionTests.DeepReadbackAndResignedHistoryForgeriesReject',
    'Cna.Core.Tests.Campaigns.CombatInheritedReserveMovementTests.DeepReadbackMutationsAndResignedForgeriesReject',
    'Cna.Core.Tests.Campaigns.CombatReactionCompletionTests.EveryEventAndEveryCutCacheLeafRejectsRawAndResigned',
    'Cna.Core.Tests.Campaigns.CombatReactionFallbackTests.EveryEventAndCacheLeafRejectsRawAndResigned',
    'Cna.Core.Tests.Campaigns.CombatReactionHistoryReplayTests.EveryOccurrenceRejectsMissingDuplicateReorderedForeignAndUnsupportedTails',
    'Cna.Core.Tests.Campaigns.CombatReactionSecondMoveTests.EveryEventAndCacheLeafRejectsRawAndResigned',
    'Cna.Core.Tests.Campaigns.CombatSettledControlTests.FullSourceScopeOrdinalPrefixProgressAndOriginalDescriptorRejectTampering',
    'Cna.Core.Tests.Campaigns.CombatStepsHistoryReplayTests.SyntheticC3aAndReactionForksCannotSupplyActualSelectionProvenance',
)
MODULES = {'core-a': 'Cna.Core.Tests', 'core-b': 'Cna.Core.Tests', 'exercise': 'Cna.ExerciseRunner.Tests', 'contracts': 'Cna.Intelligence.Contracts.Tests'}
DEFERRED = {
    'Cna.Core.Tests.Rules.InitiativeRatingTests.AxisPresenceIsDerivedFromTypedLocationFacts',
    'Cna.Core.Tests.Campaigns.CampaignTests.InvalidAuthoritativeSnapshotCannotProduceAnEvent',
    'Cna.Core.Tests.Rules.ZocStaticFixtureTests.EveryNamedSourceNegativeFailsIndependently',
    'Cna.Core.Tests.Content.ContentPackV5Tests.PlacementSeedsRejectEveryCompletenessAndBoundsFailure',
    'Cna.Core.Tests.Content.ContentPackV5Tests.StrictReadbackRejectsEverySuccessorAndInheritedShapeFailure',
}


def require(condition, message):
    if not condition:
        raise ValueError(message)


def unique(rows, label):
    require(isinstance(rows, list) and rows, label + ': empty or invalid rows')
    result = []
    for row in rows:
        require(isinstance(row, (list, tuple)) and len(row) == 3 and all(isinstance(x, str) and x for x in row), label + ': invalid identity')
        module, method, name = row
        require(name == method or name.startswith(method + '('), label + ': inconsistent method/name')
        result.append(tuple(row))
    require(len(result) == len(set(result)), label + ': duplicate identity')
    return set(result)


def row_identity(module, name):
    require(isinstance(name, str) and name.startswith(module + '.'), 'Unknown/custom display identity')
    method = name.split('(', 1)[0]
    require(method.count('.') >= 3, 'Missing class/method identity')
    return (module, method, name)


def discovery_rows(text, module, deferred):
    require('\x1b' not in text, 'ANSI discovery output is not supported')
    records = [line[2:] for line in text.splitlines() if line.startswith('  ') and line.strip()]
    counts = [int(x) for x in re.findall(r'Discovered (\d+) tests(?: in assembly -|\.)', text)]
    require(len(counts) == 2 and all(n == len(records) for n in counts), 'Truncated/malformed discovery counts')
    require(len(records) == len(set(records)) and records, 'Empty/duplicate discovery')
    rows = []
    for name in records:
        if name in deferred:
            rows.extend(row_identity(module, value) for value in deferred[name])
        else:
            rows.append(row_identity(module, name))
    return unique(rows, module + ' discovery')


MUTATION_METHOD = SELECTORS[0]
TRANSFER_INDICES = (0, 1, 2, 3, 17, 18, 19, 20)


def core_partition(rows):
    core = unique(list(rows), 'complete Core inventory')
    names = [row[2] for row in core]
    require(len(names) == len({name.casefold() for name in names}), 'Casefold identity collision')
    require(all('*' not in name for name in names), 'Wildcard display identity is unsupported')
    for module, method, name in core:
        require(module == 'Cna.Core.Tests' and row_identity(module, name)[1] == method, 'Unknown/custom Core identity')
    selected = {method.casefold() for method in SELECTORS}
    require(all(any(row[1].casefold() == method for row in core) for method in selected), 'Missing selected method')
    mutation = {row for row in core if row[1].casefold() == MUTATION_METHOD.casefold()}
    require(all(re.fullmatch(re.escape(MUTATION_METHOD) + r'\(index: (0|[1-9][0-9]*)\)', row[2]) for row in mutation), 'Wildcard/custom mutation display name')
    transferred = {MUTATION_METHOD + f'(index: {index})' for index in TRANSFER_INDICES}
    require(transferred <= {row[2] for row in mutation}, 'Missing transferred mutation row')
    retained = {row[2] for row in mutation} - transferred
    a = {row for row in core if row[1].casefold() in selected and row[2] not in transferred}
    b = core - a
    return {'core-a': a, 'core-b': b, 'transferred': sorted(transferred), 'retained_mutation': sorted(retained)}


def partition_filters(shard, partition):
    if shard == 'core-a':
        return ['--filter-method', *SELECTORS, '--filter-not-display-name', *partition['transferred']]
    if shard == 'core-b':
        result = ['--filter-not-method', *SELECTORS[1:]]
        if partition['retained_mutation']:
            result.extend(['--filter-not-display-name', *partition['retained_mutation']])
        return result
    return []


def load_partition(inventory, sha, sdk, attempt):
    require(inventory['schema_version'] == 1 and inventory['tested_sha'] == sha, 'Inventory source mismatch')
    require(inventory['sdk'] == sdk and inventory['configuration'] == 'Release' and inventory['run_attempt'] == attempt, 'Inventory SDK/configuration/attempt mismatch')
    require(set(inventory['shards']) == set(MODULES), 'Missing/unexpected prepared shard')
    rows = []
    for shard in ['core-a', 'core-b']:
        require(inventory['shards'][shard]['module'] == MODULES[shard], 'Prepared Core module mismatch')
        rows.extend(inventory['shards'][shard]['rows'])
    partition = core_partition(rows)
    for shard in ['core-a', 'core-b']:
        require(unique(inventory['shards'][shard]['rows'], shard) == partition[shard], 'Prepared row partition mismatch')
    return partition


def expand_core(text, payload):
    require(payload.get('schema_version') == 1 and payload.get('runner_version') == '4.0.1', 'Unsupported provider proof')
    providers = payload.get('providers', [])
    require(len(providers) == len(DEFERRED) and {x['method'] for x in providers} == DEFERRED, 'Missing/unknown provider')
    deferred = {}
    for provider in providers:
        names = provider['names']
        unique([row_identity('Cna.Core.Tests', name) for name in names], 'deferred provider')
        require(all(name.startswith(provider['method'] + '(') for name in names), 'Deferred method mismatch')
        deferred[provider['method']] = names
    actual_names = {line[2:] for line in text.splitlines() if line.startswith('  ')}
    controls = payload.get('controls', [])
    control_methods = {'Cna.Core.Tests.Campaigns.CombatActualRoundEntryTests.NativeActualEntryRetainsExactPreparedAndCancelledTerminalProofs', 'Cna.Core.Tests.Campaigns.BreakdownRecordTests.InvalidFiniteShapesAndNoncanonicalBytesReject'}
    require(len(controls) == 2 and {control['method'] for control in controls} == control_methods, 'Missing/unknown/duplicate formatter control')
    for control in controls:
        require(all(name.startswith(control['method'] + '(') for name in control['names']), 'Control method mismatch')
        require(control['names'] and len(control['names']) == len(set(control['names'])) and set(control['names']) <= actual_names, 'Runner formatter control mismatch')
    require(len(payload.get('controls', [])) == 2, 'Missing formatter controls')
    return discovery_rows(text, 'Cna.Core.Tests', deferred), deferred


def build_inventory(texts, payload, sha, sdk, attempt='local'):
    require(re.fullmatch(r'[a-f0-9]{40}', sha) and sdk.startswith('10.'), 'Unsupported discovery SHA/SDK')
    core, deferred = expand_core(texts['core'], payload)
    partition = core_partition(core)
    a = discovery_rows(texts['core-a'], 'Cna.Core.Tests', deferred)
    b = discovery_rows(texts['core-b'], 'Cna.Core.Tests', deferred)
    require(a == partition['core-a'] and b == partition['core-b'], 'Actual filters differ from complete-row predicate')
    require(not a & b and a | b == core, 'Filters are not disjoint/exhaustive')
    inventory = {'schema_version': 1, 'tested_sha': sha, 'sdk': sdk, 'configuration': 'Release', 'run_attempt': attempt, 'shards': {}}
    rows = {'core-a': a, 'core-b': b, 'exercise': discovery_rows(texts['exercise'], MODULES['exercise'], {}), 'contracts': discovery_rows(texts['contracts'], MODULES['contracts'], {})}
    for shard, values in rows.items():
        inventory['shards'][shard] = {'module': MODULES[shard], 'rows': sorted(values)}
    return inventory


def verify_coverage(inventory, directory, needs, expected_sha, expected_attempt='local'):
    try:
        require(set(needs) == {'prepare', 'tests'} and all(needs[k]['result'] == 'success' for k in needs), 'Failed/cancelled/skipped/missing dependency')
        require(inventory['schema_version'] == 1 and inventory['tested_sha'] == expected_sha and re.fullmatch(r'[a-f0-9]{40}', expected_sha), 'Mismatched tested SHA/schema')
        require(inventory['run_attempt'] == expected_attempt and (expected_attempt == 'local' or expected_attempt.isdecimal()), 'Inventory attempt mismatch')
        require(inventory['configuration'] == 'Release' and isinstance(inventory['sdk'], str) and inventory['sdk'].startswith('10.'), 'Unsupported configuration/SDK')
        require(set(inventory['shards']) == set(MODULES), 'Missing/unexpected shard')
        all_expected = set()
        for shard, module in MODULES.items():
            entry = inventory['shards'][shard]
            require(entry['module'] == module, 'Shard module mismatch')
            expected = unique(entry['rows'], shard + ' inventory')
            require(all(row[0] == module for row in expected) and not all_expected & expected, 'Wrong module/cross-shard overlap')
            all_expected |= expected
            root = Path(directory) / shard
            status = json.loads((root / 'status.json').read_text())
            require(type(status['exit_code']) is int and status['exit_code'] == 0, 'Nonzero/invalid process exit')
            require(status['shard'] == shard and status['module'] == module and status['tested_sha'] == expected_sha, 'Status identity mismatch')
            require(status['run_attempt'] == expected_attempt, 'Stale/mismatched leaf attempt')
            require(status['sdk'] == inventory['sdk'] and status['configuration'] == 'Release', 'Status SDK/configuration mismatch')
            reports = list((root / 'results').glob('*.xml'))
            require(len(reports) == 1, 'Missing/duplicate report')
            document = ET.parse(reports[0]).getroot()
            assemblies = document.findall('assembly')
            require(document.tag == 'assemblies' and len(assemblies) == 1, 'Unexpected report assemblies')
            assembly = assemblies[0]
            require(Path(assembly.attrib['name'].replace('\\', '/')).stem == module and assembly.attrib['target-framework'] == '.NETCoreApp,Version=v10.0', 'Report module/framework mismatch')
            counts = {key: int(assembly.attrib[key]) for key in ['total', 'passed', 'failed', 'skipped', 'not-run', 'errors']}
            require(counts['failed'] == counts['skipped'] == counts['not-run'] == counts['errors'] == 0 and not document.findall('.//error'), 'Report failures/skips/NotRun/errors')
            tests = assembly.findall('.//test')
            require(counts['total'] == counts['passed'] == len(tests), 'Report counter mismatch')
            actual = []
            for test in tests:
                require(test.attrib['result'] == 'Pass', 'Non-passing row')
                # The installed xUnit XML writer escapes display text once, beyond XML encoding.
                name = json.loads('"' + test.attrib['name'] + '"')
                method = test.attrib['type'] + '.' + test.attrib['method']
                actual.append((module, method, name))
            require(unique(actual, shard + ' report') == expected, 'Missing/unexpected argument-bearing rows')
        return True
    except (KeyError, TypeError, OSError, ET.ParseError, json.JSONDecodeError) as error:
        raise ValueError('Missing/malformed coverage evidence: ' + str(error)) from error


def project(module):
    return 'tests/' + module + '/' + module + '.csproj'


def run_logged(args, log):
    with Path(log).open('w') as output:
        result = subprocess.run(args, stdout=output, stderr=subprocess.STDOUT, shell=False)
    require(result.returncode == 0, 'Process failed (' + str(result.returncode) + '): ' + str(log))


def identity(expected_sha):
    actual = subprocess.check_output(['git', 'rev-parse', 'HEAD'], text=True).strip()
    require(actual == expected_sha, 'Checkout differs from tested event SHA')
    sdk = subprocess.check_output(['dotnet', '--version'], text=True).strip()
    require(sdk.startswith('10.'), 'SDK10 required')
    return sdk


def execution_command(shard_name, directory, partition=None):
    # Progress is retained for diagnosis; only final reports/status satisfy coverage.
    command = ['dotnet', 'test', '--project', project(MODULES[shard_name]), '--configuration', 'Release', '--no-build', '--no-ansi', '--output', 'Detailed', '--diagnostic', '--diagnostic-verbosity', 'Trace', '--diagnostic-synchronous-write', '--diagnostic-output-directory', str((directory / 'diagnostics').resolve()), '--report-xunit-xml', '--results-directory', str((directory / 'results').resolve()), '/bl:' + str((directory / 'test-{}.binlog').resolve())]
    if shard_name in ['core-a', 'core-b']:
        require(partition is not None, 'Fresh complete Core partition required')
        command.extend(partition_filters(shard_name, partition))
    return command


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('mode', choices=['discover', 'execute', 'verify'])
    parser.add_argument('--sha', required=True)
    parser.add_argument('--attempt', default='local')
    parser.add_argument('--directory', default='artifacts/ci')
    parser.add_argument('--shard', choices=MODULES)
    parser.add_argument('--inventory', default='artifacts/partition-proof/inventory.json')
    args = parser.parse_args()
    root = Path(args.directory)
    if args.mode == 'verify':
        verify_coverage(json.loads((root / 'inventory.json').read_text()), root, json.loads(os.environ['NEEDS_JSON']), args.sha, args.attempt)
        print('Complete exact-head Release coverage verified.')
        return
    sdk = identity(args.sha)
    if args.mode == 'discover':
        root.mkdir(parents=True, exist_ok=True)
        texts = {}
        core_path = root / 'core-discovery.log'
        command = ['dotnet', 'test', '--project', project('Cna.Core.Tests'), '--configuration', 'Release', '--no-build', '--list-tests', '--pre-enumerate-theories', 'on', '--no-ansi', '/bl:' + str((root / 'core-{}.binlog').resolve())]
        run_logged(command, core_path)
        texts['core'] = core_path.read_text()
        run_logged(['dotnet', 'run', '--project', '.github/scripts/ReleaseTestRows/ReleaseTestRows.csproj', '--configuration', 'Release', '--no-build', '--', str(Path('artifacts/bin/Cna.Core.Tests/release').resolve()), str(core_path.resolve()), str((root / 'deferred.json').resolve())], root / 'provider-discovery.log')
        payload = json.loads((root / 'deferred.json').read_text())
        core, _ = expand_core(texts['core'], payload)
        partition = core_partition(core)
        for label, module in MODULES.items():
            path = root / (label + '-discovery.log')
            command = ['dotnet', 'test', '--project', project(module), '--configuration', 'Release', '--no-build', '--list-tests', '--pre-enumerate-theories', 'on', '--no-ansi', '/bl:' + str((root / (label + '-{}.binlog')).resolve())]
            command.extend(partition_filters(label, partition))
            run_logged(command, path)
            texts[label] = path.read_text()
        inventory = build_inventory(texts, payload, args.sha, sdk, args.attempt)
        (root / 'inventory.json').write_text(json.dumps(inventory, indent=2) + '\n')
        print({key: len(value['rows']) for key, value in inventory['shards'].items()})
    else:
        require(args.shard is not None, 'Shard required')
        shard = root / args.shard
        require(not shard.exists(), 'Stale shard output directory')
        (shard / 'results').mkdir(parents=True)
        module = MODULES[args.shard]
        partition = load_partition(json.loads(Path(args.inventory).read_text()), args.sha, sdk, args.attempt)
        command = execution_command(args.shard, shard, partition)
        with (shard / 'execution.log').open('w') as output:
            result = subprocess.run(command, stdout=output, stderr=subprocess.STDOUT, shell=False)
        (shard / 'status.json').write_text(json.dumps({'shard': args.shard, 'module': module, 'tested_sha': args.sha, 'sdk': sdk, 'configuration': 'Release', 'run_attempt': args.attempt, 'exit_code': result.returncode}) + '\n')
        print((shard / 'execution.log').read_text())
        raise SystemExit(result.returncode)


if __name__ == '__main__':
    try:
        main()
    except (ValueError, OSError, KeyError, json.JSONDecodeError) as error:
        print('Coverage gate failed: ' + str(error), file=sys.stderr)
        raise SystemExit(1)
