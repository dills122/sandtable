"""Strict semantic and child-process evidence for the R02 CI candidate.

The compact product fixture supplies metadata, never authenticated runtime authority.
The independent frozen ordinal/cut grid below rejects omission or reassignment.
"""
import hashlib
import json
import re

PREFIX = 'Cna.Core.Tests.Campaigns.CombatActualMovedRepeatMovementCompletion'
MODULE = 'Cna.Core.Tests'
METHOD = 'CompleteTraceEveryCutAndAllPriorRetry'
SUPPORT_CLASSES = {'support-behavior': PREFIX + 'Tests', 'support-contract': PREFIX + 'ContractTests'}

SUPPORT_OBLIGATIONS = {'physicalPins': 106, 'physicalFaultModes': 5, 'physicalPinFaults': 530, 'handlers': 14, 'limitEntries': 25, 'handlerCapacityOmissions': 156, 'ledgerFaults': 60, 'resignedEvents': 6, 'claimFaults': 24, 'contextFaults': 72, 'isolatedBoundaries': 8, 'grammarFramingOwnershipChecks': 88}


def require(condition, message):
    if not condition:
        raise ValueError(message)


def fingerprint(value):
    return hashlib.sha256(json.dumps(value, sort_keys=True, separators=(',', ':'), ensure_ascii=False).encode()).hexdigest()


def frozen_grid():
    rows = {}
    for profile, side, start, stop, count in [('paid', 'axis', 0, 6, 3), ('paid', 'commonwealth', 6, 12, 3), ('armed', 'axis', 12, 34, 6), ('armed', 'commonwealth', 34, 56, 6)]:
        for ordinal in range(start, stop):
            for cut in range(1, count + 1):
                key = f'r{ordinal:03}-c{cut:02}'
                rows[key] = {'rootOrdinal': ordinal, 'movementCut': cut, 'profile': profile, 'group': f'{profile}-{side}-m{cut - 1}', 'rowKey': key}
    return rows


def group_class(group):
    profile, side, move = group.split('-')
    return PREFIX + profile.title() + side.title() + 'Move' + move[1:] + 'ContractTests'


def registered_classes():
    return sorted({group_class(row['group']) for row in frozen_grid().values()} | set(SUPPORT_CLASSES.values()))


def display_name(class_name, method_name, arguments):
    require(isinstance(method_name, str) and re.fullmatch(r'[A-Za-z][A-Za-z0-9_]*', method_name), 'Invalid method name')
    require(isinstance(arguments, list), 'Primitive ordered arguments required')
    names, values = set(), []
    for argument in arguments:
        require(isinstance(argument, dict) and set(argument) == {'name', 'value'}, 'Invalid argument record')
        name, value = argument['name'], argument['value']
        require(isinstance(name, str) and re.fullmatch(r'[A-Za-z][A-Za-z0-9_]*', name) and name not in names, 'Duplicate/invalid argument name')
        names.add(name)
        require(value is None or type(value) in (str, int, bool), 'Unsupported nonprimitive argument')
        if type(value) is str:
            require(re.fullmatch(r'[A-Za-z0-9_.:-]{1,80}', value), 'Unsupported/truncated string display')
        values.append(name + ': ' + json.dumps(value, ensure_ascii=False))
    method = class_name + '.' + method_name
    return method if not arguments else method + '(' + ', '.join(values) + ')'


def frozen_support_rows():
    """The independently agreed complete support identity contract (25 actual rows)."""
    result = []
    four = [(0, 1, 'paid'), (0, 3, 'paid'), (12, 1, 'armed'), (12, 6, 'armed')]
    specifications = [
        ('support-behavior', 'AuthenticPublishedMovementCompletesLifecycle', four),
        ('support-contract', 'EveryCutContextActorCapabilityAndOrderReject', four),
        ('support-contract', 'SourceControlProofReadersRejectForgedClaims', four),
        ('support-contract', 'CatalogFullPositionsAndBothProofsStayDistinct', four),
        ('support-contract', 'TenIndependentLedgersAndResignedEventsCannotAuthenticate', [four[1], four[3]]),
        ('support-contract', 'OwnedBeforeCallbackAndAllReturnedBuffers', [four[1], four[3]]),
    ]
    for group, method, cases in specifications:
        for values in cases:
            result.append({'className': SUPPORT_CLASSES[group], 'methodName': method, 'arguments': [{'name': name, 'value': value} for name, value in zip(('rootOrdinal', 'movementCut', 'profile'), values)]})
    for method in ['AllPhysicalPinsRemainRequiredAfterWarmReplay', 'EveryHandlerAndCapacityRequired', 'ClosedGrammarAndSizeFraming', 'WholeTraceCapacityAndHistoricalExclusionAreBounded', 'FullSourceFramingCannotForgeParent']:
        result.append({'className': SUPPORT_CLASSES['support-contract'], 'methodName': method, 'arguments': []})
    return result


def semantic_manifest(fixture):
    require(isinstance(fixture, dict), 'Invalid compact fixture')
    obligations = fixture.get('supportObligations')
    require(isinstance(obligations, dict) and all(type(value) is int for value in obligations.values()) and obligations == SUPPORT_OBLIGATIONS, 'Missing/changed focused-loop obligations')
    values = fixture.get('semanticRows')
    require(isinstance(values, list), 'Missing semantic rows')
    expected = frozen_grid()
    actual = {}
    groups = {}
    work_ids, counts = set(), {'rows': 0, 'events': 0, 'cuts': 0, 'retries': 0}
    for row in values:
        require(isinstance(row, dict) and set(row) == {'rootOrdinal', 'movementCut', 'profile', 'group', 'rowKey'}, 'Invalid semantic row shape')
        require(type(row['rootOrdinal']) is int and type(row['movementCut']) is int, 'Noninteger semantic identity')
        key = row['rowKey']
        require(isinstance(key, str) and key not in actual and expected.get(key) == row, 'Duplicate/foreign/reassigned semantic row')
        actual[key] = row
        group = row['group']
        arguments = [{'name': name, 'value': row[name]} for name in ('rootOrdinal', 'movementCut', 'profile')]
        name = display_name(group_class(group), METHOD, arguments)
        groups.setdefault(group, {'class': group_class(group), 'rows': []})['rows'].append((MODULE, group_class(group) + '.' + METHOD, name))
        n = 1 if row['movementCut'] == 1 else 3
        owned = {key} | {f'{key}/e{i:02}' for i in range(1, n + 1)} | {f'{key}/k{i:02}' for i in range(n + 1)} | {f'{key}/k{cut:02}/retry{prior:02}' for cut in range(1, n + 1) for prior in range(1, cut + 1)}
        require(not work_ids & owned, 'Overlapping semantic work')
        work_ids |= owned
        counts['rows'] += 1; counts['events'] += n; counts['cuts'] += n + 1; counts['retries'] += n * (n + 1) // 2
    require(set(actual) == set(expected) and counts == {'rows': 300, 'events': 788, 'cuts': 1088, 'retries': 1520}, 'Incomplete semantic work')
    support = fixture.get('supportRows')
    require(isinstance(support, list) and support, 'Missing focused-support inventory')
    require(sorted(fingerprint(row) for row in support) == sorted(fingerprint(row) for row in frozen_support_rows()), 'Incomplete/changed focused-support contract')
    seen = set()
    for row in support:
        require(isinstance(row, dict) and set(row) == {'className', 'methodName', 'arguments'}, 'Invalid support row shape')
        class_name = row['className']
        matching = [key for key, value in SUPPORT_CLASSES.items() if value == class_name]
        require(len(matching) == 1, 'Unregistered support class')
        group = matching[0]
        name = display_name(class_name, row['methodName'], row['arguments'])
        identity = (MODULE, class_name + '.' + row['methodName'], name)
        require(identity not in seen, 'Duplicate support identity')
        seen.add(identity)
        groups.setdefault(group, {'class': class_name, 'rows': []})['rows'].append(identity)
    require(set(SUPPORT_CLASSES) <= set(groups), 'Missing focused-support class')
    for entry in groups.values(): entry['rows'] = sorted(entry['rows'])
    return {'rows': actual, 'work_ids': sorted(work_ids), 'counts': counts, 'groups': groups, 'support_obligations': dict(obligations)}


def reconcile_discovery(semantic, full_core_rows, group_rows):
    """Return old rows only after actual full and per-class discovery agree exactly."""
    from test_partition import unique
    complete = unique(list(full_core_rows), 'complete Core discovery')
    require(all(row[0] == MODULE for row in complete), 'Unexpected Core module')
    require(set(group_rows) == set(semantic['groups']), 'Missing/unregistered group discovery')
    new_rows = set()
    for group, entry in semantic['groups'].items():
        actual = unique(list(group_rows[group]), group + ' discovery')
        expected = unique(list(entry['rows']), group + ' semantic rows')
        require(actual == expected and not new_rows & actual, 'Group actual discovery differs/overlaps')
        new_rows |= actual
    require(new_rows <= complete, 'Missing actual R02 row')
    old = complete - new_rows
    require(not any(row[1].startswith(PREFIX) for row in old), 'Unregistered R02 class/method/argument')
    return old


def validate_binding(binding):
    require(isinstance(binding, dict) and set(binding) == {'tested_sha', 'run_attempt', 'sdk', 'configuration', 'module_sha256', 'fixture_sha256', 'run_id'}, 'Invalid child binding')
    require(all(isinstance(value, str) for value in binding.values()), 'Invalid binding value type')
    require(re.fullmatch(r'[a-f0-9]{40}', binding['tested_sha']) and re.fullmatch(r'[a-f0-9]{64}', binding['module_sha256']) and re.fullmatch(r'[a-f0-9]{64}', binding['fixture_sha256']), 'Invalid child fingerprints')
    require(binding['configuration'] == 'Release' and re.fullmatch(r'10\.[0-9]+\.[0-9]+', binding['sdk']) and re.fullmatch(r'local|[1-9][0-9]*', binding['run_attempt']) and re.fullmatch(r'local|[1-9][0-9]*', binding['run_id']), 'Invalid child SDK/configuration/run/attempt')
    return binding


def verify_child(group, entry, directory, binding):
    from pathlib import Path
    import xml.etree.ElementTree as ET
    from test_partition import report_rows, unique
    try:
        validate_binding(binding)
        status = json.loads((Path(directory) / 'status.json').read_text())
        require(all(status[key] == value for key, value in binding.items()), 'Stale/foreign child binding')
        require(status['group'] == group and status['module'] == MODULE and status['class_name'] == entry['class'], 'Wrong child group/module/class')
        require(type(status['exit_code']) is int and status['exit_code'] == 0, 'Failed/invalid child exit')
        from datetime import datetime, timezone
        import math
        from ci_group_runner import execution_command
        require(type(status['evidence_version']) is int and status['evidence_version'] == 1, 'Unsupported child evidence')
        require('launch_error' not in status and status['cancelled'] is False and type(status['process_id']) is int and status['process_id'] > 0, 'Cancelled/missing child process')
        require(type(status['elapsed_seconds']) in (int, float) and math.isfinite(status['elapsed_seconds']) and status['elapsed_seconds'] >= 0, 'Invalid child duration')
        started, completed = (datetime.fromisoformat(status[key]) for key in ('started_utc', 'completed_utc'))
        require(started.utcoffset() == completed.utcoffset() == timezone.utc.utcoffset(started) and completed >= started, 'Invalid child UTC timing')
        output = Path(status['output_directory'])
        binary = Path(status['module_path'])
        require(output.is_absolute() and output.name == group and binary.is_absolute() and binary.name == MODULE + '.dll', 'Invalid child paths')
        require(status['command'] == execution_command(binary, output, entry['class']), 'Child command/filter differs')
        actual = report_rows(Path(directory), MODULE)
        require(actual == unique(list(entry['rows']), group + ' expected rows'), 'Missing/extra/foreign child rows')
        return actual
    except (KeyError, TypeError, OSError, ET.ParseError, json.JSONDecodeError) as error:
        raise ValueError('Missing/malformed child evidence: ' + str(error)) from error


def candidate_allocation():
    """Provisional parent-weighted packing; full R02 costs/review still gate hosted use."""
    lanes = [
        ['armed-commonwealth-m1'], ['armed-commonwealth-m2'], ['armed-commonwealth-m3'], ['armed-commonwealth-m4'],
        ['armed-commonwealth-m5'], ['armed-axis-m1', 'paid-axis-m0'], ['armed-axis-m2', 'paid-commonwealth-m0'], ['armed-axis-m3'],
        ['armed-axis-m4', 'support-behavior'], ['armed-axis-m5', 'support-contract'], ['paid-axis-m1', 'paid-commonwealth-m1', 'armed-commonwealth-m0'], ['paid-axis-m2', 'paid-commonwealth-m2', 'armed-axis-m0'],
    ]
    return {group: {'job': f'r02-{i // 4 + 1}', 'lane': i % 4 + 1} for i, groups in enumerate(lanes) for group in groups}


def validate_allocation(allocation, semantic):
    require(isinstance(allocation, dict) and set(allocation) == set(semantic['groups']), 'Missing/extra allocation group')
    for entry in allocation.values():
        require(isinstance(entry, dict) and set(entry) == {'job', 'lane'}, 'Invalid allocation entry')
        require(entry['job'] in ('r02-1', 'r02-2', 'r02-3') and type(entry['lane']) is int and 1 <= entry['lane'] <= 4, 'Invalid job/lane')
    require({entry['job'] for entry in allocation.values()} == {'r02-1', 'r02-2', 'r02-3'}, 'Missing candidate job')
    return allocation


def check_group_directories(root, semantic):
    from pathlib import Path
    try:
        paths = list(Path(root).iterdir())
        require(all(path.is_dir() and not path.is_symlink() for path in paths) and {path.name for path in paths} == set(semantic['groups']), 'Missing/extra/foreign child directory')
        return True
    except OSError as error:
        raise ValueError('Missing child directory set') from error


def make_proof(fixture, full_core_rows, group_rows, binding):
    from test_partition import unique
    validate_binding(binding)
    semantic = semantic_manifest(fixture)
    legacy = reconcile_discovery(semantic, full_core_rows, group_rows)
    allocation = validate_allocation(candidate_allocation(), semantic)
    result = {'schema_version': 1, 'binding': dict(binding), 'semantic_sha256': fingerprint(semantic),
        'full_core_rows': sorted(unique(list(full_core_rows), 'complete prepared Core rows')),
        'legacy_core_rows': sorted(legacy), 'groups': {}}
    for group, entry in semantic['groups'].items():
        result['groups'][group] = dict(entry, **allocation[group])
    # Serialized representation is the contract; no tuple/list distinction after retention.
    return json.loads(json.dumps(result))


def load_proof(proof, fixture, binding):
    try:
        require(proof['binding'] == binding, 'Prepared source/attempt/SDK/binary/fixture mismatch')
        expected = make_proof(fixture, proof['full_core_rows'], {group: entry['rows'] for group, entry in proof['groups'].items()}, binding)
        require(proof == expected, 'Changed/incomplete prepared semantic/actual/allocation proof')
        return proof
    except (KeyError, TypeError) as error:
        raise ValueError('Malformed prepared R02 proof') from error


def verify_combined(proof, fixture, inventory, directory, needs, binding):
    from pathlib import Path
    from test_partition import CORE_SHARDS, load_partition, unique, verify_coverage
    load_proof(proof, fixture, binding)
    require(all(inventory[key] == binding[field] for key, field in [('tested_sha', 'tested_sha'), ('run_attempt', 'run_attempt'), ('sdk', 'sdk'), ('configuration', 'configuration')]), 'Legacy/R02 provenance differs')
    require(inventory.get('excluded_classes') == registered_classes(), 'Missing complete new-class exclusions')
    load_partition(inventory, binding['tested_sha'], binding['sdk'], binding['run_attempt'])
    verify_coverage(inventory, directory, needs, binding['tested_sha'], binding['run_attempt'])
    old_core = unique([row for shard in CORE_SHARDS for row in inventory['shards'][shard]['rows']], 'legacy Core union')
    require(old_core == unique(proof['legacy_core_rows'], 'prepared legacy Core union'), 'Legacy/Core prepared union differs')
    root = Path(directory) / 'r02-groups'
    semantic = semantic_manifest(fixture)
    check_group_directories(root, semantic)
    new_rows, processes = set(), set()
    for group, entry in proof['groups'].items():
        actual = verify_child(group, entry, root / group, binding)
        require(not new_rows & actual and not old_core & actual, 'Old/new/group row overlap')
        new_rows |= actual
        status = json.loads((root / group / 'status.json').read_text())
        process_key = (entry['job'], status['process_id'], status['started_utc'])
        require(process_key not in processes, 'Duplicate attributed child process')
        processes.add(process_key)
    require(old_core | new_rows == unique(proof['full_core_rows'], 'full prepared Core rows'), 'Incomplete full Core execution union')
    all_rows = unique([row for entry in inventory['shards'].values() for row in entry['rows']], 'all legacy rows') | new_rows
    return {'total_rows': len(all_rows), 'r02_rows': len(new_rows), 'child_processes': len(processes), 'row_identity_sha256': fingerprint(sorted(all_rows)), 'semantic_counts': semantic['counts']}


BINARY_NAMES = ('Cna.Core.Tests.dll', 'Cna.Core.dll', 'xunit.v3.common.dll', 'xunit.v3.core.dll')
FIXTURE_PATH = 'docs/specs/fixtures/combat-actual-moved-repeat-movement-completion-v1.json'
BINARY_DIRECTORY = 'artifacts/bin/Cna.Core.Tests/release'


def binary_fingerprints(directory):
    from pathlib import Path
    return {name: hashlib.sha256((Path(directory) / name).read_bytes()).hexdigest() for name in BINARY_NAMES}


def binding_for(fixture_path, sha, sdk, attempt, fingerprints, run_id='local'):
    from pathlib import Path
    require(set(fingerprints) == set(BINARY_NAMES) and all(isinstance(value, str) and re.fullmatch(r'[a-f0-9]{64}', value) for value in fingerprints.values()), 'Missing/malformed compiled binary fingerprints')
    return validate_binding({'tested_sha': sha, 'run_attempt': attempt, 'run_id': run_id, 'sdk': sdk, 'configuration': 'Release', 'module_sha256': fingerprint(fingerprints), 'fixture_sha256': hashlib.sha256(Path(fixture_path).read_bytes()).hexdigest()})


def discovery_command(module, log, filters=()):
    from pathlib import Path
    from test_partition import project
    return ['dotnet', 'test', '--project', project(module), '--configuration', 'Release', '--no-build', '--list-tests', '--pre-enumerate-theories', 'on', '--no-ansi', '/bl:' + str(Path(log).with_name(Path(log).stem + '-{}.binlog').resolve()), *filters]


def discover(directory, fixture_path, sha, sdk, attempt, binary_directory=BINARY_DIRECTORY, run_id='local'):
    from pathlib import Path
    import test_partition as legacy
    root = Path(directory)
    root.mkdir(parents=True, exist_ok=True)
    require(not (root / 'inventory.json').exists() and not (root / 'r02-inventory.json').exists(), 'Stale prepared inventory')
    fixture = json.loads(Path(fixture_path).read_text())
    semantic = semantic_manifest(fixture)
    binding = binding_for(fixture_path, sha, sdk, attempt, binary_fingerprints(binary_directory), run_id)
    core_log = root / 'core-discovery.log'
    legacy.run_logged(discovery_command(MODULE, core_log), core_log)
    legacy.run_logged(['dotnet', 'run', '--project', '.github/scripts/ReleaseTestRows/ReleaseTestRows.csproj', '--configuration', 'Release', '--no-build', '--', str(Path(binary_directory).resolve()), str(core_log.resolve()), str((root / 'deferred.json').resolve())], root / 'provider-discovery.log')
    payload = json.loads((root / 'deferred.json').read_text())
    require(payload['fingerprints'] == binary_fingerprints(binary_directory), 'Compiled discovery binary drift')
    texts = {'core': core_log.read_text()}
    core, deferred = legacy.expand_core(texts['core'], payload)
    group_rows = {}
    for group, entry in semantic['groups'].items():
        log = root / (group + '-discovery.log')
        legacy.run_logged(discovery_command(MODULE, log, ['--filter-class', entry['class']]), log)
        group_rows[group] = sorted(legacy.discovery_rows(log.read_text(), MODULE, deferred))
    proof = make_proof(fixture, core, group_rows, binding)
    partition = legacy.core_partition(proof['legacy_core_rows'])
    partition['excluded_classes'] = registered_classes()
    for shard, module in legacy.MODULES.items():
        log = root / (shard + '-discovery.log')
        legacy.run_logged(discovery_command(module, log, legacy.partition_filters(shard, partition)), log)
        texts[shard] = log.read_text()
    new_rows = [row for entry in proof['groups'].values() for row in entry['rows']]
    inventory = legacy.build_inventory(texts, payload, sha, sdk, attempt, excluded_rows=new_rows)
    require(binary_fingerprints(binary_directory) == payload['fingerprints'], 'Binary inputs mutated during discovery')
    require(binding_for(fixture_path, sha, sdk, attempt, payload['fingerprints'], run_id) == binding, 'Fixture mutated during discovery')
    (root / 'inventory.json').write_text(json.dumps(inventory, indent=2) + '\n')
    (root / 'r02-inventory.json').write_text(json.dumps(proof, indent=2) + '\n')
    return proof


def execute_groups(proof, fixture, directory, binding, job, binary):
    from pathlib import Path
    from ci_group_runner import run_job
    load_proof(proof, fixture, binding)
    require(job in ('r02-1', 'r02-2', 'r02-3'), 'Unknown R02 job')
    allocation = {group: {'job': entry['job'], 'lane': entry['lane']} for group, entry in proof['groups'].items()}
    statuses = run_job(proof['groups'], allocation, job, Path(directory) / 'r02-groups', binding, binary)
    selected = {group for group, entry in allocation.items() if entry['job'] == job}
    require(set(statuses) == selected, 'Missing/extra job child process')
    check_group_directories(Path(directory) / 'r02-groups', {'groups': {group: proof['groups'][group] for group in selected}})
    rows = set()
    for group in selected:
        actual = verify_child(group, proof['groups'][group], Path(directory) / 'r02-groups' / group, binding)
        require(statuses[group] == json.loads((Path(directory) / 'r02-groups' / group / 'status.json').read_text()), 'Retained/observed child status differs')
        require(not rows & actual, 'Job child overlap')
        rows |= actual
    return {'job': job, 'groups': len(selected), 'rows': len(rows)}


def main():
    import argparse
    import os
    from pathlib import Path
    import subprocess
    import sys
    import test_partition as legacy
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('mode', choices=['discover', 'execute', 'verify'])
    parser.add_argument('--sha', required=True)
    parser.add_argument('--attempt', default='local')
    parser.add_argument('--run-id', default=os.environ.get('GITHUB_RUN_ID', 'local'))
    parser.add_argument('--fixture', default=FIXTURE_PATH)
    parser.add_argument('--binary-directory', default=BINARY_DIRECTORY)
    parser.add_argument('--directory', default='artifacts/ci')
    parser.add_argument('--inventory', default='artifacts/partition-proof/inventory.json')
    parser.add_argument('--shard', choices=[*legacy.MODULES, 'r02-1', 'r02-2', 'r02-3'])
    args = parser.parse_args()
    root = Path(args.directory)
    actual = subprocess.check_output(['git', 'rev-parse', 'HEAD'], text=True).strip()
    require(actual == args.sha, 'Checkout differs from tested event SHA')
    require(subprocess.run(['git', 'diff', '--quiet', 'HEAD'], check=False).returncode == 0, 'Dirty tracked source cannot supply exact-head evidence')
    subprocess.run(['git', 'ls-files', '--error-unmatch', args.fixture], stdout=subprocess.DEVNULL, check=True)
    fixture = json.loads(Path(args.fixture).read_text())
    if args.mode == 'discover':
        proof = discover(root, args.fixture, args.sha, legacy.identity(args.sha), args.attempt, args.binary_directory, args.run_id)
        print({'groups': len(proof['groups']), 'r02_rows': sum(len(entry['rows']) for entry in proof['groups'].values())})
        return
    prepared_directory = root if args.mode == 'verify' else Path(args.inventory).parent
    proof = json.loads((prepared_directory / 'r02-inventory.json').read_text())
    inventory = json.loads((prepared_directory / 'inventory.json').read_text())
    if args.mode == 'verify':
        payload = json.loads((root / 'deferred.json').read_text())
        binding = binding_for(args.fixture, args.sha, inventory['sdk'], args.attempt, payload['fingerprints'], args.run_id)
        result = verify_combined(proof, fixture, inventory, root, json.loads(os.environ['NEEDS_JSON']), binding)
        (root / 'r02-verification.json').write_text(json.dumps(result, indent=2) + '\n')
        print(result)
        return
    require(args.shard is not None, 'Shard required')
    sdk = legacy.identity(args.sha)
    fingerprints = binary_fingerprints(args.binary_directory)
    binding = binding_for(args.fixture, args.sha, sdk, args.attempt, fingerprints, args.run_id)
    load_proof(proof, fixture, binding)
    legacy.load_partition(inventory, args.sha, sdk, args.attempt)
    legacy_rows = legacy.unique([row for shard in legacy.CORE_SHARDS for row in inventory['shards'][shard]['rows']], 'prepared old Core rows')
    require(legacy_rows == legacy.unique(proof['legacy_core_rows'], 'R02 prepared old Core rows'), 'Old/new proof disagreement')
    if args.shard in legacy.MODULES:
        result = subprocess.run([sys.executable, str(Path(__file__).with_name('test_partition.py')), 'execute', '--sha', args.sha, '--attempt', args.attempt, '--directory', args.directory, '--shard', args.shard, '--inventory', args.inventory], check=False)
        require(result.returncode == 0, 'Legacy leaf process failed')
    else:
        result = execute_groups(proof, fixture, root, binding, args.shard, Path(args.binary_directory) / (MODULE + '.dll'))
        (root / (args.shard + '-summary.json')).write_text(json.dumps(result, indent=2) + '\n')
        print(result)
    require(binary_fingerprints(args.binary_directory) == fingerprints and binding_for(args.fixture, args.sha, sdk, args.attempt, fingerprints, args.run_id) == binding, 'Binary/fixture inputs changed during execution')


if __name__ == '__main__':
    import sys
    import subprocess
    try:
        main()
    except (ValueError, OSError, KeyError, TypeError, json.JSONDecodeError, subprocess.CalledProcessError) as error:
        print('R02 coverage gate failed: ' + str(error), file=sys.stderr)
        raise SystemExit(1)
