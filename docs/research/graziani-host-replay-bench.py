#!/usr/bin/env python3
"""Opt-in real source-journal comparison; does not change the checkout.

Example: python3 docs/research/graziani-host-replay-bench.py --baseline <commit>
    --candidate <commit> --output /private/tmp/graziani-replay-comparison.json
Requires local Git refs, .NET 10 and package restore access. Uses repeated valid
setup actions, not a representative gameplay mix. Output contains no seat secrets.
"""

import argparse
from contextlib import closing
import json
import os
import platform
from pathlib import Path
import shutil
import sqlite3
import subprocess
import tempfile
import time


ROOT = Path(__file__).resolve().parents[2]
TEMPLATE = Path(__file__).with_suffix('.cs.txt')
TEST = Path('tests/Cna.OrleansHost.Tests/TemporaryGrazianiReplayBenchmark.cs')
PROJECT = 'tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj'


def run(command, cwd, env=None, capture=False):
    result = subprocess.run(command, cwd=cwd, env=env, text=True,
                            stdout=subprocess.PIPE if capture else None,
                            stderr=subprocess.STDOUT if capture else None, check=False)
    if result.returncode:
        if capture:
            print(result.stdout, flush=True)
        raise RuntimeError(f'Command failed ({result.returncode}): {command[0]}')
    return result.stdout.strip() if capture else None


def archive(ref, target):
    target.mkdir()
    producer = subprocess.Popen(['git', 'archive', ref], cwd=ROOT, stdout=subprocess.PIPE)
    consumer = subprocess.run(['tar', '-x', '-C', str(target)], stdin=producer.stdout, check=False)
    producer.stdout.close()
    if producer.wait() or consumer.returncode:
        raise RuntimeError('Could not create isolated source snapshot')
    test = target / TEST
    if test.exists():
        raise RuntimeError('Ref already contains the benchmark test path; refusing to overwrite')
    shutil.copyfile(TEMPLATE, test)


def test(source, artifacts, mode, env, build=False):
    command = ['dotnet', 'test', '--project', PROJECT, '--configuration', 'Release',
               '--artifacts-path', str(artifacts), '--filter-method',
               f'*TemporaryGrazianiReplayBenchmark.{mode}', '-p:NuGetAudit=false']
    if not build:
        command += ['--no-build', '--no-restore']
    run(command, source, env=env)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--baseline', required=True, help='Baseline Git commit or ref')
    parser.add_argument('--candidate', required=True, help='Candidate Git commit or ref')
    parser.add_argument('--output', type=Path, required=True, help='New JSON result path; never overwritten')
    args = parser.parse_args()
    if args.output.exists():
        raise SystemExit('Output already exists; choose a new path')
    refs = {name: run(['git', 'rev-parse', '--verify', '--end-of-options', value + '^{commit}'], ROOT, capture=True)
            for name, value in [('baseline', args.baseline), ('candidate', args.candidate)]}
    sdk = run(['dotnet', '--version'], ROOT, capture=True)
    with tempfile.TemporaryDirectory(prefix='sandtable-graziani-bench-') as temporary:
        root = Path(temporary)
        sources = {name: root / name for name in refs}
        for name, path in sources.items():
            archive(refs[name], path)
        data = root / 'journal'
        data.mkdir(mode=0o700)
        env = os.environ.copy()
        env['SANDTABLE_BENCH_DIR'] = str(data)
        artifacts = {name: root / ('artifacts-' + name) for name in refs}
        test(sources['baseline'], artifacts['baseline'], 'Prepare', env, build=True)
        for count in (40, 80):
            env['SANDTABLE_BENCH_STEPS'] = str(count - 10)
            env['SANDTABLE_BENCH_GROW_OUTPUT'] = f'source-{count}.sqlite'
            test(sources['baseline'], artifacts['baseline'], 'Grow', env)
        # Build the candidate using an existing fixture; that first measurement is discarded.
        env['SANDTABLE_BENCH_SOURCE'] = 'source-40.sqlite'
        env['SANDTABLE_BENCH_OUTPUT'] = 'candidate-build.json'
        test(sources['candidate'], artifacts['candidate'], 'Measure', env, build=True)
        samples = []
        fresh_process_samples = []
        for count in (40, 80):
            env['SANDTABLE_BENCH_SOURCE'] = f'source-{count}.sqlite'
            for cycle in range(2):
                for name in ('baseline', 'candidate'):
                    filename = f'{count}-{cycle}-{name}.json'
                    env['SANDTABLE_BENCH_OUTPUT'] = filename
                    test(sources[name], artifacts[name], 'Measure', env)
                    samples.append({'expected_steps': count, 'cycle': cycle, 'version': name,
                                    'result': json.loads((data / filename).read_text())})
                    if samples[-1]['result']['count'] != count:
                        raise RuntimeError('Unexpected source-journal length')
                    # Backup before starting the measured process. The timer below includes
                    # the SDK/MTP runner startup and teardown as well as replay.
                    source = data / f'source-{count}.sqlite'
                    copy = data / f'fresh-{count}-{cycle}-{name}.sqlite'
                    with closing(sqlite3.connect(source.as_uri() + '?mode=ro', uri=True)) as original:
                        with closing(sqlite3.connect(copy)) as target:
                            original.backup(target)
                    copy.chmod(0o600)
                    env['SANDTABLE_BENCH_SOURCE'] = copy.name
                    fresh_output = f'fresh-{count}-{cycle}-{name}.json'
                    env['SANDTABLE_BENCH_OUTPUT'] = fresh_output
                    started = time.perf_counter()
                    test(sources[name], artifacts[name], 'FreshProcessReplay', env)
                    invocation_ms = (time.perf_counter() - started) * 1000
                    fresh = json.loads((data / fresh_output).read_text())
                    if fresh['count'] != count:
                        raise RuntimeError('Unexpected fresh-process journal length')
                    fresh_process_samples.append({'expected_steps': count, 'cycle': cycle, 'version': name,
                                                  'invocationWallMs': invocation_ms, 'result': fresh})
                    env['SANDTABLE_BENCH_SOURCE'] = f'source-{count}.sqlite'
        result = {'schema_version': 1, 'commits': refs, 'sdk': sdk, 'platform': platform.platform(),
                  'workload': 'real Graziani source setup; repeated valid Commonwealth motorization adjustments',
                  'notes': 'Four samples per process; each read opens a fresh Host object, but only the first has process startup. Fresh-process invocation wall includes SDK/MTP startup and teardown; replay wall is measured inside that process. Peak working set is whole test process, not one operation, and null where unavailable; end working set is a snapshot.',
                  'samples': samples, 'fresh_process_samples': fresh_process_samples}
        with args.output.open('x', encoding='utf-8') as output:
            json.dump(result, output, indent=2)
            output.write('\n')
    print(f'Benchmark results: {args.output}')


if __name__ == '__main__':
    main()
