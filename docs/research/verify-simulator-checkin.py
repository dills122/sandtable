#!/usr/bin/env python3
"""Bounded post-merge simulator check-in; stdlib only, no tracked repository mutations.

Build current and baseline Runner checkouts first. Both must be Git-clean.
Usage: python3 verify-simulator-checkin.py CURRENT BASELINE NEW_OUTPUT [--prior-run ROOT]
Outputs: summary.json, seeds.csv, canonical hash ledgers, exact inputs and CLI logs.
Timing is fresh-process Debug CLI plus trusted evidence I/O, not engine-only latency.
"""
import argparse
import csv
import hashlib
import json
import platform
import statistics
import subprocess
import time
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path

CANONICAL = ('exercise-manifest.json', 'seed-ledger.json', 'accepted-actions.jsonl',
             'canonical-events.jsonl', 'initial-snapshot.json', 'final-snapshot.json',
             'step-evidence.jsonl', 'reconstruction-proof.json', 'readjudication-proof.json')
STUDIES = ('scenarios/maneuvers/rules-lab.breakdown-truck.serial.v1.json',
           'scenarios/maneuvers/rules-lab.reaction.serial.breakdown.v1.json')


def require(condition, message):
    if not condition:
        raise ValueError(message)


def read(path):
    return json.loads(path.read_text())


def sha(data):
    return 'sha256:' + hashlib.sha256(data).hexdigest()


def digest(value):
    return sha(json.dumps(value, sort_keys=True, separators=(',', ':')).encode())


def command(args, cwd):
    return subprocess.check_output(args, cwd=cwd, text=True).strip()


def identity(repo):
    require(not command(['git', 'status', '--porcelain'], repo), f'Dirty checkout: {repo}')
    return command(['git', 'rev-parse', 'HEAD'], repo)


def children(manifest):
    if 'pairs' in manifest:
        return [child for pair in manifest['pairs'] for child in (pair['baseline'], pair['candidate'])]
    return manifest.get('exercises', [manifest])


def inspect_run(folder, manifest, commit):
    bundles = {}
    records = []
    expected = {child['exerciseId']: child for child in children(manifest)}
    require(len(expected) == len(children(manifest)), 'Duplicate input exercise IDs')
    for path in sorted(folder.rglob('exercise-manifest.json')):
        bundle = path.parent
        exercise = read(path)
        key = exercise['exerciseId']
        require(key in expected and key not in bundles, f'Unexpected/duplicate child: {key}')
        require(exercise == {**expected[key], 'rootSeed': manifest['rootSeed']}, f'Wrong executed input: {key}')
        result = read(bundle / 'run-result.json')
        require(result['status'] == 'succeeded', f'Failed child: {bundle}')
        require(result['outcome']['positionId'] == expected[key]['terminalBoundary'], f'Wrong terminal: {key}')
        build = read(bundle / 'build-identity.json')
        require(build['dirty'] is False and build['headCommit'] == commit, f'Wrong source: {bundle}')
        for proof in ('reconstruction-proof.json', 'readjudication-proof.json'):
            require(read(bundle / proof)['status'] == 'verified', f'Unverified proof: {bundle / proof}')
        entries = read(bundle / 'artifact-manifest.json')['files']
        names = [entry['path'] for entry in entries]
        require(len(names) == len(set(names)) and set(CANONICAL) <= set(names), f'Incomplete/duplicate artifact index: {bundle}')
        for entry in entries:
            artifact = (bundle / entry['path']).resolve()
            require(artifact.is_relative_to(bundle.resolve()), f'Artifact path escape: {artifact}')
            data = artifact.read_bytes()
            require(len(data) == entry['sizeBytes'] and sha(data) == entry['sha256'], f'Artifact mismatch: {artifact}')
        bundles[key] = {name: bundle / name for name in CANONICAL}
        events = [json.loads(line) for line in (bundle / 'canonical-events.jsonl').read_text().splitlines()]
        checks = [check for event in events if event['eventType'] == 'breakdown-stop-resolved' for check in event['checks']]
        final = read(bundle / 'final-snapshot.json')
        cohorts = [element['operationalState']['vehicleBreakdownState'] for element in final['world']['elements']
                   if element['operationalState']['vehicleBreakdownState'] is not None]
        records.append({'exerciseId': key, 'steps': len((bundle / 'accepted-actions.jsonl').read_text().splitlines()),
                        'weather': [event['kind'] for event in events if event['eventType'] == 'weather-determined'],
                        'reactionMoves': sum(event['eventType'] == 'reacting-element-moved' for event in events),
                        'stopResolutions': sum(event['eventType'] == 'breakdown-stop-resolved' for event in events),
                        'rolledChecks': sum(check['status'] == 'rolled' for check in checks),
                        'noRollChecks': sum(check['status'] != 'rolled' for check in checks),
                        'noRollReasons': dict(Counter(check['status'] for check in checks if check['status'] != 'rolled')),
                        'zeroLossRolls': sum(check['roll'] is not None and check['roll']['lossCount'] == 0 for check in checks),
                        'lostPoints': sum(check['roll']['lossCount'] for check in checks if check['roll'] is not None),
                        'zeroWorkingCohorts': sum(cohort['workingPointCount'] == 0 for cohort in cohorts),
                        'terminal': result['outcome']['positionId']})
    require(set(bundles) == set(expected), f'Missing children: {set(expected) - set(bundles)}')
    reports = [read(path) for path in folder.rglob('*report.json') if 'deterministic' in read(path)]
    require(len(reports) == (1 if 'exercises' in manifest or 'pairs' in manifest else 0), f'Report count: {folder}')
    for report in reports:
        require(report['deterministic']['manifest'] == manifest, f'Wrong report input: {folder}')
        require(report['deterministic']['status'] == 'succeeded', f'Failed aggregate: {folder}')
    hashes = {key: {name: sha(path.read_bytes()) for name, path in files.items()} for key, files in sorted(bundles.items())}
    return {'bundles': bundles, 'records': records, 'hashes': hashes, 'digest': digest(hashes),
            'reports': [{'reportFingerprint': report['reportFingerprint'], 'deterministic': report['deterministic']} for report in reports],
            'reportMs': sum(report['diagnostics']['elapsedMicroseconds'] / 1000 for report in reports)}


def compare(left, right):
    require(left['hashes'] == right['hashes'], 'Canonical hashes differ')
    require(left['reports'] == right['reports'], 'Deterministic reports differ')
    for key, files in left['bundles'].items():
        for name, path in files.items():
            require(path.read_bytes() == right['bundles'][key][name].read_bytes(), f'Canonical bytes differ: {key}/{name}')
    return len(left['bundles']) * len(CANONICAL)


def execute(repo, manifest_path, folder, commit):
    require(identity(repo) == commit, f'Checkout changed: {repo}')
    manifest = read(manifest_path)
    if manifest_path.is_relative_to(repo):
        relative = manifest_path.relative_to(repo)
    else:
        # CLI rejects external manifests; materialize exact inputs under ignored build artifacts.
        data = manifest_path.read_bytes()
        relative = Path('artifacts/simulator-checkin-inputs') / (hashlib.sha256(data).hexdigest() + '.json')
        subprocess.check_call(['git', 'check-ignore', '-q', str(relative)], cwd=repo)
        target = repo / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        if target.exists():
            require(target.read_bytes() == data, f'Input hash collision: {target}')
        else:
            target.write_bytes(data)
    kind = 'maneuver' if 'exercises' in manifest or 'pairs' in manifest else 'exercise'
    args = ['dotnet', str(repo / 'artifacts/bin/Cna.ExerciseRunner/debug/Cna.ExerciseRunner.dll'),
            kind, 'run', '--manifest', str(relative), '--artifact-root', str(folder)]
    started = time.perf_counter()
    result = subprocess.run(args, cwd=repo, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=120)
    elapsed = (time.perf_counter() - started) * 1000
    folder.parent.mkdir(parents=True, exist_ok=True)
    Path(str(folder) + '.log').write_text(result.stdout)
    require(result.returncode == 0, f'CLI failed ({result.returncode}): {Path(str(folder) + ".log")}')
    inspected = inspect_run(folder, manifest, commit)
    inspected['wallMs'] = elapsed
    Path(str(folder) + '.timing.json').write_text(json.dumps({'wallMs': elapsed, 'reportMs': inspected['reportMs']}) + '\n')
    Path(str(folder) + '.hashes.json').write_text(json.dumps(inspected['hashes'], indent=2) + '\n')
    return inspected


def aggregate(records):
    keys = ('steps', 'reactionMoves', 'stopResolutions', 'rolledChecks', 'noRollChecks', 'zeroLossRolls', 'lostPoints', 'zeroWorkingCohorts')
    return {'campaigns': len(records), **{key: sum(row[key] for row in records) for key in keys},
            'weather': dict(sorted(Counter(weather for row in records for weather in row['weather']).items())),
            'noRollReasons': dict(sum((Counter(row['noRollReasons']) for row in records), Counter())),
            'terminals': dict(sorted(Counter(row['terminal'] for row in records).items()))}


def timing(values):
    return {'minMs': round(min(values), 3), 'medianMs': round(statistics.median(values), 3),
            'maxMs': round(max(values), 3)}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('current', type=Path)
    parser.add_argument('baseline', type=Path)
    parser.add_argument('output', type=Path)
    parser.add_argument('--prior-run', type=Path)
    parser.add_argument('--checked-runs', type=Path, help='Re-audit existing checked-a/checked-b runs at the current commit')
    args = parser.parse_args()
    current, baseline, root = args.current.resolve(), args.baseline.resolve(), args.output.resolve()
    require(not root.exists(), f'Output already exists: {root}')
    commits = {'current': identity(current), 'baseline': identity(baseline)}
    root.mkdir(parents=True)
    (root / 'inputs').mkdir()
    summary = {'schemaVersion': 1, 'startedUtc': datetime.now(timezone.utc).isoformat(), 'commits': commits,
               'environment': {'platform': platform.platform(), 'machine': platform.machine(), 'python': platform.python_version(),
                               'dotnetSdk': command(['dotnet', '--version'], current),
                               'dotnetRuntimes': command(['dotnet', '--list-runtimes'], current).splitlines()},
               'artifactRoot': str(root), 'timing': {}, 'checked': {}, 'sweep': {}}
    # Interleave baseline/current order; exclude one warmup per source. No builds/tests run concurrently.
    samples = []
    for repetition in range(-1, 6):
        order = ('baseline', 'current') if repetition % 2 == 0 else ('current', 'baseline')
        runs = {}
        for label in order:
            repo = current if label == 'current' else baseline
            runs[label] = execute(repo, repo / STUDIES[0], root / 'timing' / f'{repetition + 1}-{label}', commits[label])
            print(f'timing {repetition + 1}/6 {label}: {runs[label]["wallMs"]:.0f} ms', flush=True)
        compare(runs['baseline'], runs['current'])
        if repetition >= 0:
            samples.append({'pair': repetition, 'order': list(order),
                            **{label: {key: round(runs[label][key], 3) for key in ('wallMs', 'reportMs')} for label in order}})
    summary['timing'] = {'study': STUDIES[0], 'warmupsPerSource': 1, 'pairs': samples,
                         'sources': {label: {key: timing([row[label][key] for row in samples]) for key in ('wallMs', 'reportMs')}
                                     for label in ('baseline', 'current')}}
    summary['timing']['medianPairedWallChangePercent'] = round(statistics.median(
        (row['current']['wallMs'] / row['baseline']['wallMs'] - 1) * 100 for row in samples), 2)
    inventory = read(current / 'docs/specs/breakdown-fixture-migration.v1.json')
    migration = read(current / 'docs/research/breakdown-runner-migration-results.json')
    paths = [row['successorPath'] for row in inventory['scenarioFiles']] + [row['path'] for row in migration['supplementalScenarioFiles']]
    fixed_records, fixed_hashes, reports = [], {}, {}
    comparisons = prior_comparisons = 0
    for index, relative in enumerate(paths):
        path = current / relative
        name = path.stem
        if args.checked_runs:
            left = inspect_run(args.checked_runs.resolve() / 'checked-a' / name, read(path), commits['current'])
            right = inspect_run(args.checked_runs.resolve() / 'checked-b' / name, read(path), commits['current'])
        else:
            left = execute(current, path, root / 'checked-a' / name, commits['current'])
            right = execute(current, path, root / 'checked-b' / name, commits['current'])
        comparisons += compare(left, right)
        if args.prior_run:
            prior = inspect_run(args.prior_run.resolve() / name, read(path), commits['baseline'])
            prior_comparisons += compare(left, prior)
        fixed_records.extend(left['records'])
        fixed_hashes[name] = left['hashes']
        if left['reports']:
            reports[name] = left['reports'][0]['reportFingerprint']
        print(f'checked {index + 1}/{len(paths)}: {name}', flush=True)
    summary['checked'] = {'manifests': len(paths), 'runs': 2, 'canonicalFileComparisons': comparisons,
                           'priorCandidateFileComparisons': prior_comparisons, 'canonicalMatrixHash': digest(fixed_hashes),
                           'reportFingerprints': reports, 'oneRunTotals': aggregate(fixed_records)}
    summary['checked']['artifactRoot'] = str(args.checked_runs.resolve() if args.checked_runs else root)
    (root / 'checkpoint.json').write_text(json.dumps(summary, indent=2) + '\n')
    seed_rows, sweep_records, sweep_comparisons = [], [], 0
    for seed in range(16):
        for relative in STUDIES:
            manifest = read(current / relative)
            manifest['rootSeed'] = seed
            name = f'{Path(relative).stem}-seed-{seed:02}'
            path = root / 'inputs' / (name + '.json')
            path.write_text(json.dumps(manifest, separators=(',', ':')))  # Canonical codec rejects trailing whitespace.
            left = execute(current, path, root / 'sweep-a' / name, commits['current'])
            right = execute(current, path, root / 'sweep-b' / name, commits['current'])
            sweep_comparisons += compare(left, right)
            sweep_records.extend(left['records'])
            row = {'rootSeed': seed, 'study': Path(relative).stem, **aggregate(left['records']),
                   'canonicalMatrixHash': left['digest'], 'reportFingerprint': left['reports'][0]['reportFingerprint']}
            seed_rows.append(row)
            print(f'sweep seed {seed:02}/15 {Path(relative).stem}: {row["campaigns"]} campaigns, {row["lostPoints"]} lost points', flush=True)
    with (root / 'seeds.csv').open('w', newline='') as stream:
        writer = csv.DictWriter(stream, fieldnames=list(seed_rows[0]), lineterminator='\n')
        writer.writeheader()
        for row in seed_rows:
            writer.writerow({key: json.dumps(value, sort_keys=True, separators=(',', ':')) if isinstance(value, dict) else value for key, value in row.items()})
    summary['sweep'] = {'rootSeeds': list(range(16)), 'studies': list(STUDIES), 'runs': 2,
                         'canonicalFileComparisons': sweep_comparisons, 'oneRunTotals': aggregate(sweep_records),
                         'seedDatasetHash': sha((root / 'seeds.csv').read_bytes())}
    campaigns = 2 * (len(fixed_records) + len(sweep_records)) + 14 * len(children(read(current / STUDIES[0])))
    summary['totalExecutedCampaigns'] = campaigns
    summary['verifiedProofs'] = campaigns * 2
    require(identity(current) == commits['current'] and identity(baseline) == commits['baseline'], 'Source changed during run')
    summary['completedUtc'] = datetime.now(timezone.utc).isoformat()
    (root / 'summary.json').write_text(json.dumps(summary, indent=2) + '\n')
    print(json.dumps({'campaigns': campaigns, 'verifiedProofs': campaigns * 2, 'output': str(root)}, indent=2))


if __name__ == '__main__':
    main()
