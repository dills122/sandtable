"""Bounded long-test suites; all prepared actual rows remain required."""
import hashlib
import json


def row_key(row):
    return hashlib.sha256(json.dumps(list(row), separators=(',', ':'), ensure_ascii=False).encode()).hexdigest()


def make_plan(rows, weights):
    import re
    from test_partition import require, unique, row_identity
    core = unique(rows, 'Complete legacy Core discovery')
    require(all(row[0] == 'Cna.Core.Tests' and row_identity(row[0], row[2])[1] == row[1] for row in core), 'Foreign/custom Core row')
    require(len({row[2].casefold() for row in core}) == len(core) and all('*' not in row[2] for row in core), 'Ambiguous/wildcard display row')
    require(type(weights.get('schema_version')) is int and weights['schema_version'] == 1 and isinstance(weights.get('rows'), dict) and isinstance(weights.get('classes'), dict), 'Unsupported timing manifest')
    require(set(weights['rows']) == {row_key(row) for row in core}, 'Unknown/missing measured row; update reviewed timing manifest')
    require(all(re.fullmatch('[a-f0-9]{64}', key) and type(value) is int and value > 0 for key, value in weights['rows'].items()), 'Invalid measured row weight')
    classes = {}
    for row in sorted(core): classes.setdefault(row[1].rsplit('.', 1)[0], []).append(row)
    require(set(classes) == set(weights['classes']), 'Unknown/missing measured class')
    long_groups, fast = [], []
    for name, class_rows in sorted(classes.items()):
        parts = weights['classes'][name].get('parts')
        require(type(parts) is int and 1 <= parts <= len(class_rows), 'Invalid class split count')
        if parts == 1:
            fast.append((name, class_rows, sum(weights['rows'][row_key(row)] for row in class_rows)))
            continue
        bins, costs = [[] for _ in range(parts)], [0] * parts
        for row in sorted(class_rows, key=lambda value: (-weights['rows'][row_key(value)], value)):
            index = min(range(parts), key=lambda value: (costs[value], value))
            bins[index].append(row); costs[index] += weights['rows'][row_key(row)]
        for index, selected in enumerate(bins):
            long_groups.append(('row-' + hashlib.sha256(name.encode()).hexdigest()[:12] + f'-{index + 1:02}', sorted(selected), costs[index]))
    groups, lane_costs = {}, [0] * 32
    for group, selected, cost in sorted(long_groups, key=lambda value: (-value[2], value[0])):
        lane = min(range(32), key=lambda index: (lane_costs[index], index))
        lane_costs[lane] += cost + 2000
        groups[group] = {'job': f'long-{lane // 4 + 1:02}', 'lane': lane % 4 + 1, 'phase': 'parallel', 'selector_kind': 'display', 'rows': [list(row) for row in selected]}
    fast_bins, fast_costs = [[] for _ in range(4)], [0] * 4
    for name, selected, cost in sorted(fast, key=lambda value: (-value[2], value[0])):
        index = min(range(4), key=lambda value: (fast_costs[value], value))
        fast_bins[index].extend(selected); fast_costs[index] += cost
    require(all(fast_bins) and long_groups, 'Empty/unsupported dedicated suite layout')
    for index, selected in enumerate(fast_bins):
        group = f'fast-{index + 1:02}'
        groups[group] = {'job': group, 'lane': 1, 'phase': 'parallel', 'selector_kind': 'classes', 'rows': [list(row) for row in sorted(selected)]}
    actual = [row for entry in groups.values() for row in entry['rows']]
    require(unique(actual, 'Allocated Core rows') == core, 'Incomplete/overlapping allocation')
    digest = lambda value: hashlib.sha256(json.dumps(value, sort_keys=True, separators=(',', ':'), ensure_ascii=False).encode()).hexdigest()
    return {'schema_version': 1, 'weights_sha256': digest(weights), 'rows_sha256': digest(sorted(core)), 'groups': dict(sorted(groups.items()))}


def validate_plan(plan, rows, weights):
    from test_partition import require
    require(plan_digest(plan) == plan_digest(make_plan(rows, weights)), 'Changed/incomplete suite allocation')
    return plan




def validate_dependencies(needs):
    from test_partition import require
    required = {
        'prepare', 'tests', 'format', 'orleans',
        'p01-paid-fresh-aggregate', 'p01-rba-decline-fresh-aggregate',
        'p01-rba-cancel-fresh-aggregate', 'p01-force-seal-fresh',
        'p01-absent-aggregate', 'p01-all-prefix-reject-aggregate',
    }
    require(set(needs) == required and all(needs[key]['result'] == 'success' for key in needs), 'Failed/cancelled/skipped/missing required dependency')
    return True


def plan_digest(plan):
    return hashlib.sha256(json.dumps(plan, sort_keys=True, separators=(',', ':'), ensure_ascii=False).encode()).hexdigest()


def recorded_command(binary, directory, entry):
    """Validate already canonical producer paths without resolving another host's /home."""
    from pathlib import PurePosixPath
    from test_partition import require, unique
    binary, directory = PurePosixPath(str(binary)), PurePosixPath(str(directory))
    require(binary.is_absolute() and directory.is_absolute() and '..' not in binary.parts + directory.parts, 'Invalid canonical process paths')
    rows = unique(entry['rows'], 'Exact selected process rows')
    classes = sorted({row[1].rsplit('.', 1)[0] for row in rows})
    import re
    require(all(row[0] == 'Cna.Core.Tests' for row in rows) and all(re.fullmatch(r'Cna\.Core\.Tests\.[A-Za-z0-9_.+]+', name) for name in classes), 'Invalid exact Core class')
    command = ['dotnet', 'exec', str(binary), '--no-ansi', '--output', 'Detailed', '--diagnostic', '--diagnostic-verbosity', 'Trace', '--diagnostic-synchronous-write', '--diagnostic-output-directory', str(directory / 'diagnostics'), '--report-xunit-xml', '--results-directory', str(directory / 'results'), '--parallel', 'collections', '--max-threads', '4']
    if entry['selector_kind'] == 'display':
        require(len(classes) == 1 and all('*' not in row[2] for row in rows), 'Unsafe display selector')
        return command + ['--filter-display-name', *sorted(row[2] for row in rows)]
    require(entry['selector_kind'] == 'classes', 'Unknown suite selector')
    return command + ['--filter-class', *classes]


def execution_command(binary, directory, entry):
    from pathlib import Path
    return recorded_command(Path(binary).resolve(), Path(directory).resolve(), entry)


def verify_process(group, entry, directory, binding, expected_plan_sha256):
    from datetime import datetime, timezone
    import math
    from pathlib import Path, PurePosixPath
    import xml.etree.ElementTree as ET
    from r02_partition import validate_binding
    from test_partition import require, report_rows, unique
    try:
        validate_binding(binding)
        directory = Path(directory)
        require(not directory.is_symlink() and not (directory / 'status.json').is_symlink(), 'Linked process evidence')
        status = json.loads((directory / 'status.json').read_text())
        require(all(status[key] == value for key, value in binding.items()), 'Stale/foreign process binding')
        require(status['suite_plan_sha256'] == expected_plan_sha256, 'Changed prepared suite plan')
        classes = sorted({row[1].rsplit('.', 1)[0] for row in entry['rows']})
        require(status['group'] == group and status['module'] == 'Cna.Core.Tests' and status['phase'] == entry.get('phase', 'parallel'), 'Wrong process group/module/phase')
        require(status['class_names'] == classes and status['class_name'] == (classes[0] if len(classes) == 1 else None), 'Wrong selected class set')
        require(type(status['exit_code']) is int and status['exit_code'] == 0, 'Failed/invalid process exit')
        require(type(status['evidence_version']) is int and status['evidence_version'] == 1 and status['cancelled'] is False, 'Cancelled/unsupported process evidence')
        require('launch_error' not in status and 'cleanup_error' not in status and type(status['process_id']) is int and status['process_id'] > 0, 'Missing/unclean process')
        require(type(status['elapsed_seconds']) in (int, float) and math.isfinite(status['elapsed_seconds']) and 0 <= status['elapsed_seconds'] <= 750, 'Invalid process duration')
        started, completed = (datetime.fromisoformat(status[key]) for key in ('started_utc', 'completed_utc'))
        require(started.utcoffset() == completed.utcoffset() == timezone.utc.utcoffset(started) and completed >= started, 'Invalid process UTC timing')
        output, binary = PurePosixPath(status['output_directory']), PurePosixPath(status['module_path'])
        require(output.is_absolute() and output.name == group and binary.is_absolute() and binary.name == 'Cna.Core.Tests.dll', 'Invalid process paths')
        require(status['command'] == recorded_command(binary, output, entry), 'Process command/filter differs')
        actual = report_rows(directory, 'Cna.Core.Tests')
        require(actual == unique(list(entry['rows']), group + ' expected rows'), 'Missing/extra/foreign process rows')
        return actual
    except (KeyError, TypeError, OSError, ET.ParseError, json.JSONDecodeError) as error:
        raise ValueError('Missing/malformed suite process evidence: ' + str(error)) from error


LONG_JOBS = tuple(f'long-{index:02}' for index in range(1, 9))
FAST_JOBS = tuple(f'fast-{index:02}' for index in range(1, 5))


def run_suite_job(plan, job, root, binding, binary, timeout_seconds=720, command_builder=execution_command):
    from concurrent.futures import ThreadPoolExecutor
    from pathlib import Path
    import signal
    import threading
    import time
    from ci_group_runner import ResourceGuard, run_child
    from test_partition import require
    require(job in LONG_JOBS + FAST_JOBS, 'Unknown dedicated suite job')
    require(type(timeout_seconds) in (int, float) and 0 < timeout_seconds <= 720, 'Invalid suite execution deadline')
    selected = {name: entry for name, entry in plan['groups'].items() if entry['job'] == job}
    require(selected and all(type(entry['lane']) is int and 1 <= entry['lane'] <= 4 and entry['phase'] == 'parallel' for entry in selected.values()), 'Empty/invalid dedicated suite lanes')
    root = Path(root)
    require(not root.exists(), 'Stale suite output directory')
    root.mkdir(parents=True)
    cancellation = threading.Event()
    deadline = time.monotonic() + timeout_seconds
    guard = ResourceGuard(cancellation, job, binding)
    previous = {}
    if threading.current_thread() is threading.main_thread():
        for signum in (signal.SIGTERM, signal.SIGINT):
            previous[signum] = signal.signal(signum, lambda _signum, _frame: cancellation.set())
    digest = plan_digest(plan)

    def lane(number):
        statuses = {}
        for group, entry in sorted(selected.items()):
            if entry['lane'] != number: continue
            if cancellation.is_set() or time.monotonic() >= deadline: break
            directory = root / group
            classes = sorted({row[1].rsplit('.', 1)[0] for row in entry['rows']})
            template = dict(binding, group=group, module='Cna.Core.Tests', class_name=classes[0] if len(classes) == 1 else None, class_names=classes, phase=entry['phase'], suite_plan_sha256=digest, module_path=str(Path(binary).resolve()))
            status = run_child(command_builder(binary, directory, entry), directory, template, deadline, cancellation, guard)
            statuses[group] = status
            if status['exit_code'] != 0 or status['cancelled']:
                cancellation.set()
                break
        return statuses

    statuses = {}
    try:
        guard.start()
        with ThreadPoolExecutor(max_workers=4) as workers:
            futures = [workers.submit(lane, number) for number in range(1, 5)]
            for future in futures: statuses.update(future.result())
        require(set(statuses) == set(selected) and not cancellation.is_set(), 'Failed/cancelled/incomplete suite process set')
    finally:
        cancellation.set()
        evidence = guard.finish()
        (root.parent / (job + '-resource.json')).write_text(json.dumps(evidence, indent=2) + '\n')
        for signum, handler in previous.items(): signal.signal(signum, handler)
    require(evidence['abort_reason'] is None, 'Suite resource guard failed: ' + str(evidence['abort_reason']))
    return statuses


def make_suite_proof(r02_proof, weights):
    from r02_partition import validate_binding
    validate_binding(r02_proof['binding'])
    return {'schema_version': 1, 'binding': dict(r02_proof['binding']), 'plan': make_plan(r02_proof['legacy_core_rows'], weights)}


def verify_suite_resources(plan, directory, binding, job):
    import os
    from pathlib import Path
    from ci_group_runner import validate_resource_evidence
    from test_partition import require
    selected = {group: entry for group, entry in plan['groups'].items() if entry['job'] == job}
    evidence = json.loads((Path(directory) / (job + '-resource.json')).read_text())
    validate_resource_evidence(evidence, job, binding, selected)
    if os.environ.get('GITHUB_ACTIONS') == 'true': require(all(snapshot['platform'] == 'Linux' for snapshot in evidence['phase_preflights']), 'Hosted Linux resource proof required')
    return evidence


def verify_all(suite_proof, weights, r02_proof, fixture, inventory, directory, needs, binding):
    from pathlib import Path
    import xml.etree.ElementTree as ET
    import r02_partition as r02
    import test_partition as legacy
    from test_partition import require, unique, report_rows
    try:
        validate_dependencies(needs)
        r02.load_proof(r02_proof, fixture, binding)
        legacy.load_partition(inventory, binding['tested_sha'], binding['sdk'], binding['run_attempt'])
        require(plan_digest(suite_proof) == plan_digest(make_suite_proof(r02_proof, weights)) and suite_proof['binding'] == binding, 'Changed/stale dedicated suite proof')
        plan = suite_proof['plan']; root = Path(directory)
        legacy_core = unique([row for key in legacy.CORE_SHARDS for row in inventory['shards'][key]['rows']], 'Prepared legacy Core rows')
        require(legacy_core == unique(r02_proof['legacy_core_rows'], 'Prepared old/new Core boundary'), 'Prepared discovery disagreement')
        r02.check_group_directories(root / 'long-groups', {'groups': plan['groups']})
        r02.check_group_directories(root / 'r02-groups', {'groups': r02_proof['groups']})
        executed, process_keys, report_directories = set(), set(), []
        for groups, subdirectory, checker in ((plan['groups'], 'long-groups', lambda group, entry, path: verify_process(group, entry, path, binding, plan_digest(plan))), (r02_proof['groups'], 'r02-groups', lambda group, entry, path: r02.verify_child(group, entry, path, binding))):
            for group, entry in groups.items():
                child_root = root / subdirectory / group
                actual = checker(group, entry, child_root)
                require(not executed & actual, 'Overlapping process rows')
                executed |= actual; report_directories.append(child_root)
                status = json.loads((child_root / 'status.json').read_text())
                key = (entry['job'], status['process_id'], status['started_utc'])
                require(key not in process_keys, 'Duplicate attributed execution process')
                process_keys.add(key)
        resources = {}
        for job in sorted({entry['job'] for entry in plan['groups'].values()}): resources[job] = verify_suite_resources(plan, root, binding, job)
        for job in ('r02-1', 'r02-2', 'r02-3'): resources[job] = r02.verify_job_resources(r02_proof, root, binding, job)
        require(executed == unique(r02_proof['full_core_rows'], 'Complete prepared Core rows'), 'Incomplete full Core execution')
        for shard in ('exercise', 'contracts'):
            entry = inventory['shards'][shard]; module = legacy.MODULES[shard]; child_root = root / shard
            status = json.loads((child_root / 'status.json').read_text())
            require(type(status['exit_code']) is int and status['exit_code'] == 0, 'Nonzero/invalid module process exit')
            require(status['shard'] == shard and status['module'] == module and status['tested_sha'] == binding['tested_sha'] and status['run_attempt'] == binding['run_attempt'], 'Stale/foreign module process')
            require(status['sdk'] == binding['sdk'] and status['configuration'] == 'Release', 'Module SDK/configuration mismatch')
            actual = report_rows(child_root, module)
            require(actual == unique(entry['rows'], shard + ' prepared rows') and not executed & actual, 'Missing/extra/overlapping module rows')
            executed |= actual; report_directories.append(child_root)
        expected = unique([row for entry in inventory['shards'].values() for row in entry['rows']] + [row for entry in r02_proof['groups'].values() for row in entry['rows']], 'Complete prepared solution rows')
        require(executed == expected, 'Missing/extra full solution execution')
        expected_reports = {path for child_root in report_directories for path in (child_root / 'results').iterdir()}
        actual_reports = {path for path in root.rglob('*') if path.suffix.lower() == '.xml'}
        require(actual_reports == expected_reports, 'Unexpected XML outside supported process report layout')
        return {'total_rows': len(executed), 'legacy_core_rows': len(legacy_core), 'r02_rows': sum(len(entry['rows']) for entry in r02_proof['groups'].values()), 'long_suite_processes': len(plan['groups']), 'r02_child_processes': len(r02_proof['groups']), 'legacy_module_processes': 2, 'execution_processes': len(process_keys) + 2, 'xml_reports': len(expected_reports), 'row_identity_sha256': r02.fingerprint(sorted(executed)), 'semantic_counts': r02.semantic_manifest(fixture)['counts'], 'suite_plan_sha256': plan_digest(plan), 'resource_jobs': sorted(resources)}
    except (KeyError, TypeError, OSError, ET.ParseError, json.JSONDecodeError) as error:
        raise ValueError('Missing/malformed complete suite evidence: ' + str(error)) from error


WEIGHTS_PATH = '.github/scripts/long-suite-weights.json'
JOBS = (*LONG_JOBS, *FAST_JOBS, 'r02-1', 'r02-2', 'r02-3', 'exercise', 'contracts')


def main():
    import argparse
    import os
    from pathlib import Path
    import subprocess
    import sys
    import r02_partition as r02
    import test_partition as legacy
    from test_partition import require
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('mode', choices=['discover', 'execute', 'verify'])
    parser.add_argument('--sha', required=True)
    parser.add_argument('--attempt', default='local')
    parser.add_argument('--run-id', default=os.environ.get('GITHUB_RUN_ID', 'local'))
    parser.add_argument('--fixture', default=r02.FIXTURE_PATH)
    parser.add_argument('--weights', default=WEIGHTS_PATH)
    parser.add_argument('--binary-directory', default=r02.BINARY_DIRECTORY)
    parser.add_argument('--directory', default='artifacts/ci')
    parser.add_argument('--inventory', default='artifacts/partition-proof/inventory.json')
    parser.add_argument('--shard', choices=JOBS)
    args = parser.parse_args()
    require(subprocess.check_output(['git', 'rev-parse', 'HEAD'], text=True).strip() == args.sha, 'Checkout differs from tested event SHA')
    require(subprocess.run(['git', 'diff', '--quiet', 'HEAD'], check=False).returncode == 0, 'Dirty tracked source cannot supply exact-head evidence')
    for path in (args.fixture, args.weights): subprocess.run(['git', 'ls-files', '--error-unmatch', path], stdout=subprocess.DEVNULL, check=True)
    root = Path(args.directory); weights = json.loads(Path(args.weights).read_text()); fixture = json.loads(Path(args.fixture).read_text())
    if args.mode == 'discover':
        require(not (root / 'long-suites.json').exists(), 'Stale dedicated suite proof')
        proof = r02.discover(root, args.fixture, args.sha, legacy.identity(args.sha), args.attempt, args.binary_directory, args.run_id)
        prepared = make_suite_proof(proof, weights)
        require({entry['job'] for entry in prepared['plan']['groups'].values()} == set(LONG_JOBS + FAST_JOBS), 'Empty dedicated matrix suite')
        (root / 'long-suites.json').write_text(json.dumps(prepared, indent=2) + '\n')
        print({'legacy_core_rows': len(proof['legacy_core_rows']), 'long_processes': len(prepared['plan']['groups']), 'matrix_jobs': len(JOBS), 'suite_plan_sha256': plan_digest(prepared['plan'])})
        return
    prepared_root = root if args.mode == 'verify' else Path(args.inventory).parent
    proof = json.loads((prepared_root / 'r02-inventory.json').read_text()); inventory = json.loads((prepared_root / 'inventory.json').read_text()); suite_proof = json.loads((prepared_root / 'long-suites.json').read_text())
    if args.mode == 'verify':
        deferred = json.loads((root / 'deferred.json').read_text())
        binding = r02.binding_for(args.fixture, args.sha, inventory['sdk'], args.attempt, deferred['fingerprints'], args.run_id)
        result = verify_all(suite_proof, weights, proof, fixture, inventory, root, json.loads(os.environ['NEEDS_JSON']), binding)
        (root / 'suite-verification.json').write_text(json.dumps(result, indent=2) + '\n'); print(result)
        return
    require(args.shard is not None, 'Suite required')
    sdk = legacy.identity(args.sha); fingerprints = r02.binary_fingerprints(args.binary_directory)
    binding = r02.binding_for(args.fixture, args.sha, sdk, args.attempt, fingerprints, args.run_id)
    r02.load_proof(proof, fixture, binding); legacy.load_partition(inventory, args.sha, sdk, args.attempt)
    require(plan_digest(suite_proof) == plan_digest(make_suite_proof(proof, weights)) and suite_proof['binding'] == binding, 'Changed/stale dedicated suite proof')
    if args.shard in LONG_JOBS + FAST_JOBS:
        plan = suite_proof['plan']
        statuses = run_suite_job(plan, args.shard, root / 'long-groups', binding, Path(args.binary_directory) / 'Cna.Core.Tests.dll')
        selected = {group: entry for group, entry in plan['groups'].items() if entry['job'] == args.shard}
        r02.check_group_directories(root / 'long-groups', {'groups': selected})
        rows = set()
        for group, entry in selected.items():
            actual = verify_process(group, entry, root / 'long-groups' / group, binding, plan_digest(plan))
            require(not rows & actual and statuses[group] == json.loads((root / 'long-groups' / group / 'status.json').read_text()), 'Changed/overlapping retained process status')
            rows |= actual
        verify_suite_resources(plan, root, binding, args.shard)
        result = {'job': args.shard, 'groups': len(selected), 'rows': len(rows)}
        (root / (args.shard + '-summary.json')).write_text(json.dumps(result, indent=2) + '\n'); print(result)
    else:
        command = [sys.executable, str(Path(__file__).with_name('r02_partition.py')), 'execute', '--sha', args.sha, '--attempt', args.attempt, '--run-id', args.run_id, '--fixture', args.fixture, '--binary-directory', args.binary_directory, '--directory', args.directory, '--inventory', args.inventory, '--shard', args.shard]
        require(subprocess.run(command, check=False).returncode == 0, 'Retained R02/module suite failed')
    require(r02.binary_fingerprints(args.binary_directory) == fingerprints and r02.binding_for(args.fixture, args.sha, sdk, args.attempt, fingerprints, args.run_id) == binding, 'Binary/fixture inputs changed during execution')
    require(json.loads(Path(args.weights).read_text()) == weights and subprocess.run(['git', 'diff', '--quiet', 'HEAD'], check=False).returncode == 0, 'Tracked source/timing inputs changed during execution')


if __name__ == '__main__':
    try:
        main()
    except (ValueError, KeyError, TypeError, OSError, json.JSONDecodeError) as error:
        raise SystemExit('Required suite proof failed: ' + str(error)) from error
