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


def build_inventory(texts, payload, sha, sdk):
    require(re.fullmatch(r'[a-f0-9]{40}', sha) and sdk.startswith('10.'), 'Unsupported discovery SHA/SDK')
    require(payload.get('schema_version') == 1 and payload.get('runner_version') == '4.0.1', 'Unsupported provider proof')
    providers = payload.get('providers', [])
    require(len(providers) == len(DEFERRED) and {x['method'] for x in providers} == DEFERRED, 'Missing/unknown provider')
    deferred = {}
    for provider in providers:
        names = provider['names']
        unique([row_identity('Cna.Core.Tests', name) for name in names], 'deferred provider')
        require(all(name.startswith(provider['method'] + '(') for name in names), 'Deferred method mismatch')
        deferred[provider['method']] = names
    actual_names = {line[2:] for line in texts['core'].splitlines() if line.startswith('  ')}
    controls = payload.get('controls', [])
    control_methods = {'Cna.Core.Tests.Campaigns.CombatActualRoundEntryTests.NativeActualEntryRetainsExactPreparedAndCancelledTerminalProofs', 'Cna.Core.Tests.Campaigns.BreakdownRecordTests.InvalidFiniteShapesAndNoncanonicalBytesReject'}
    require(len(controls) == 2 and {control['method'] for control in controls} == control_methods, 'Missing/unknown/duplicate formatter control')
    for control in controls:
        require(all(name.startswith(control['method'] + '(') for name in control['names']), 'Control method mismatch')
        require(control['names'] and len(control['names']) == len(set(control['names'])) and set(control['names']) <= actual_names, 'Runner formatter control mismatch')
    require(len(payload.get('controls', [])) == 2, 'Missing formatter controls')
    core = discovery_rows(texts['core'], 'Cna.Core.Tests', deferred)
    a = discovery_rows(texts['core-a'], 'Cna.Core.Tests', deferred)
    b = discovery_rows(texts['core-b'], 'Cna.Core.Tests', deferred)
    selected = {s.casefold() for s in SELECTORS}
    require(all(any(row[1].casefold() == s for row in core) for s in selected), 'Unknown/empty method selector')
    require(a == {r for r in core if r[1].casefold() in selected}, 'Include filter differs from predicate')
    require(not a & b and a | b == core, 'Filters are not disjoint/exhaustive')
    inventory = {'schema_version': 1, 'tested_sha': sha, 'sdk': sdk, 'configuration': 'Release', 'shards': {}}
    rows = {'core-a': a, 'core-b': b, 'exercise': discovery_rows(texts['exercise'], MODULES['exercise'], {}), 'contracts': discovery_rows(texts['contracts'], MODULES['contracts'], {})}
    for shard, values in rows.items():
        inventory['shards'][shard] = {'module': MODULES[shard], 'rows': sorted(values)}
    return inventory


def verify_coverage(inventory, directory, needs, expected_sha):
    try:
        require(set(needs) == {'prepare', 'tests'} and all(needs[k]['result'] == 'success' for k in needs), 'Failed/cancelled/skipped/missing dependency')
        require(inventory['schema_version'] == 1 and inventory['tested_sha'] == expected_sha and re.fullmatch(r'[a-f0-9]{40}', expected_sha), 'Mismatched tested SHA/schema')
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
        result = subprocess.run(args, stdout=output, stderr=subprocess.STDOUT)
    require(result.returncode == 0, 'Process failed (' + str(result.returncode) + '): ' + str(log))


def identity(expected_sha):
    actual = subprocess.check_output(['git', 'rev-parse', 'HEAD'], text=True).strip()
    require(actual == expected_sha, 'Checkout differs from tested event SHA')
    sdk = subprocess.check_output(['dotnet', '--version'], text=True).strip()
    require(sdk.startswith('10.'), 'SDK10 required')
    return sdk


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('mode', choices=['discover', 'execute', 'verify'])
    parser.add_argument('--sha', required=True)
    parser.add_argument('--directory', default='artifacts/ci')
    parser.add_argument('--shard', choices=MODULES)
    args = parser.parse_args()
    root = Path(args.directory)
    if args.mode == 'verify':
        verify_coverage(json.loads((root / 'inventory.json').read_text()), root, json.loads(os.environ['NEEDS_JSON']), args.sha)
        print('Complete exact-head Release coverage verified.')
        return
    sdk = identity(args.sha)
    if args.mode == 'discover':
        root.mkdir(parents=True, exist_ok=True)
        texts = {}
        for label, module in [('core', 'Cna.Core.Tests'), *MODULES.items()]:
            path = root / (label + '-discovery.log')
            command = ['dotnet', 'test', '--project', project(module), '--configuration', 'Release', '--no-build', '--list-tests', '--pre-enumerate-theories', 'on', '--no-ansi', '/bl:' + str((root / (label + '-{}.binlog')).resolve())]
            if label in ['core-a', 'core-b']:
                command.extend(['--filter-method' if label == 'core-a' else '--filter-not-method', *SELECTORS])
            run_logged(command, path)
            texts[label] = path.read_text()
        run_logged(['dotnet', 'run', '--project', '.github/scripts/ReleaseTestRows/ReleaseTestRows.csproj', '--configuration', 'Release', '--no-build', '--', str(Path('artifacts/bin/Cna.Core.Tests/release').resolve()), str((root / 'core-discovery.log').resolve()), str((root / 'deferred.json').resolve())], root / 'provider-discovery.log')
        inventory = build_inventory(texts, json.loads((root / 'deferred.json').read_text()), args.sha, sdk)
        (root / 'inventory.json').write_text(json.dumps(inventory, indent=2) + '\n')
        print({key: len(value['rows']) for key, value in inventory['shards'].items()})
    else:
        require(args.shard is not None, 'Shard required')
        shard = root / args.shard
        require(not shard.exists(), 'Stale shard output directory')
        (shard / 'results').mkdir(parents=True)
        module = MODULES[args.shard]
        command = ['dotnet', 'test', '--project', project(module), '--configuration', 'Release', '--no-build', '--no-ansi', '--report-xunit-xml', '--results-directory', str((shard / 'results').resolve()), '/bl:' + str((shard / 'test-{}.binlog').resolve())]
        if args.shard in ['core-a', 'core-b']:
            command.extend(['--filter-method' if args.shard == 'core-a' else '--filter-not-method', *SELECTORS])
        with (shard / 'execution.log').open('w') as output:
            result = subprocess.run(command, stdout=output, stderr=subprocess.STDOUT)
        (shard / 'status.json').write_text(json.dumps({'shard': args.shard, 'module': module, 'tested_sha': args.sha, 'sdk': sdk, 'configuration': 'Release', 'exit_code': result.returncode}) + '\n')
        print((shard / 'execution.log').read_text())
        raise SystemExit(result.returncode)


if __name__ == '__main__':
    try:
        main()
    except (ValueError, OSError, KeyError, json.JSONDecodeError) as error:
        print('Coverage gate failed: ' + str(error), file=sys.stderr)
        raise SystemExit(1)
