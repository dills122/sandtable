"""Resource-bounded concurrent local test run; same CI-compatible shards `just
test-shard` already exposes one at a time, run concurrently on this machine
instead, with the one known ~29m43s Orleans outlier excluded the same way
CI's own nightly `orleans` job already excludes it. Not a CI coverage proof;
see `.github/scripts/local_tests.py` for that distinction.
"""
import argparse
import json
import os
from pathlib import Path
import queue
import shlex
import subprocess
import sys
import tempfile
import threading
import time

sys.dont_write_bytecode = True

import ci_group_runner as runner
import local_tests as local
import long_suite_partition as suites
import r02_partition as r02
import test_partition as partition

ROOT = Path(__file__).resolve().parents[2]
GIB = 2 ** 30

# Same test, same reasoning, as .github/workflows/ci-nightly-heavy.yml's
# `orleans` job: this one row alone measured ~29m43s and is excluded from
# hosted CI timing for that reason, not because it is unimportant.
ORLEANS_OUTLIER = 'Cna.OrleansHost.Tests.GrazianiPregameJournalTests.FinalizedRealSupplyPlanSurvivesFreshHostReplayWithoutStartingTheGame'

# Reserves are fixed headroom for the host OS/IDE/etc, not a CI-runner-sized
# assumption; the actual bound below is always computed from THIS machine's
# real snapshot (resource_snapshot()), never a hardcoded total.
RESERVED_SYSTEM_BYTES = 4 * GIB
PER_JOB_BUDGET_BYTES = 3 * GIB
RESERVED_CPUS = 1


def concurrency_limit(snapshot, job_count):
    """How many local_tests-style processes this machine can run at once.

    Bounded by real usable CPUs (effective_cpu_count minus one reserved for
    the host) and by real available memory (memory_available_bytes minus a
    fixed host reserve, divided by a per-job budget sized from observed
    `dotnet test` RSS for these suites) - never by a CI-runner-sized constant.
    """
    cpu_budget = max(1, int(snapshot['effective_cpu_count']) - RESERVED_CPUS)
    memory_budget = max(0, snapshot['memory_available_bytes'] - RESERVED_SYSTEM_BYTES)
    memory_jobs = max(1, memory_budget // PER_JOB_BUDGET_BYTES)
    return max(1, min(job_count, cpu_budget, memory_jobs))


def host_command():
    command = local.commands_for('host', 'Release')[0]
    return command + ['--filter-not-display-name', ORLEANS_OUTLIER]


def plan_jobs(proof, weights):
    """Every CI-equivalent Core shard plus exercise/contracts/host, each its
    own concurrent job; same row allocation CI uses, Orleans outlier excluded."""
    jobs = []
    for shard in suites.LONG_JOBS + suites.FAST_JOBS + ('r02-1', 'r02-2', 'r02-3'):
        for name, command in local.shard_commands(shard, proof, weights):
            jobs.append((shard + ':' + name, command))
    for suite in ('exercise', 'contracts'):
        for index, command in enumerate(local.commands_for(suite, 'Release')):
            jobs.append((suite if index == 0 else suite + '-' + str(index), command))
    jobs.append(('host', host_command()))
    return jobs


def run_one(name, command, directory):
    results = directory / name / 'results'
    log = directory / name / 'log.txt'
    log.parent.mkdir(parents=True, exist_ok=True)
    full_command = command + ['--report-xunit-xml', '--report-junit', '--report-junit-filename',
                               'results.junit.xml', '--results-directory', str(results)]
    started = time.monotonic()
    with log.open('w') as output:
        result = subprocess.run(full_command, cwd=ROOT, stdout=output, stderr=subprocess.STDOUT)
    return {'name': name, 'command': full_command, 'exit_code': result.returncode,
            'elapsed_seconds': round(time.monotonic() - started, 3), 'log': str(log)}


def run_jobs(jobs, max_workers, directory, run_one_func=run_one, poll_seconds=0.2):
    """Bounded concurrent execution: a fixed worker pool pulls from a shared
    queue. A live resource monitor pauses new launches (it never kills an
    already-running job) when available memory drops below the reserve,
    mirroring ci_group_runner.ResourceGuard's intent without its hosted-runner
    kill behavior, which is wrong for a developer's own long-lived machine."""
    pending = queue.Queue()
    for job in jobs:
        pending.put(job)
    results = {}
    lock = threading.Lock()
    stop = threading.Event()
    paused = threading.Event()

    def monitor():
        while not stop.is_set():
            try:
                snapshot = runner.resource_snapshot()
                if snapshot['memory_available_bytes'] < RESERVED_SYSTEM_BYTES:
                    paused.set()
                else:
                    paused.clear()
            except Exception:
                pass
            stop.wait(poll_seconds)

    monitor_thread = threading.Thread(target=monitor, daemon=True)
    monitor_thread.start()

    def worker():
        while True:
            try:
                name, command = pending.get_nowait()
            except queue.Empty:
                return
            while paused.is_set():
                time.sleep(poll_seconds / 2)
            outcome = run_one_func(name, command, directory)
            with lock:
                results[name] = outcome

    threads = [threading.Thread(target=worker) for _ in range(max_workers)]
    for thread in threads:
        thread.start()
    for thread in threads:
        thread.join()
    stop.set()
    monitor_thread.join(timeout=2)
    return results


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--max-workers', type=int, default=None, help='Override the computed concurrency bound')
    parser.add_argument('--dry-run', action='store_true', help='Print planned jobs; still builds/discovers to resolve exact rows')
    args = parser.parse_args(argv)
    os.chdir(ROOT)
    sha = subprocess.check_output(['git', 'rev-parse', 'HEAD'], text=True).strip()
    for target in ['Sandtable.slnx', '.github/scripts/ReleaseTestRows/ReleaseTestRows.csproj']:
        subprocess.run(['dotnet', 'build', target, '--configuration', 'Release'], cwd=ROOT, check=True)
    artifact_root = ROOT / 'artifacts/local-parallel-check'
    artifact_root.mkdir(parents=True, exist_ok=True)
    directory = Path(tempfile.mkdtemp(prefix='check-', dir=artifact_root))
    proof = r02.discover(directory / 'discovery', r02.FIXTURE_PATH, sha, partition.identity(sha), 'local')
    weights = json.loads((ROOT / suites.WEIGHTS_PATH).read_text())
    jobs = plan_jobs(proof, weights)
    snapshot = runner.resource_snapshot()
    max_workers = args.max_workers or concurrency_limit(snapshot, len(jobs))
    print('Resource snapshot: {} platform, {} effective CPUs, {} GiB available -> '
          '{} jobs, {} concurrent'.format(snapshot['platform'], snapshot['effective_cpu_count'],
                                           snapshot['memory_available_bytes'] // GIB, len(jobs), max_workers), flush=True)
    if args.dry_run:
        for name, command in jobs:
            print(name, shlex.join(command))
        return 0
    print('Local selection only; logs: ' + str(directory), flush=True)
    results = run_jobs(jobs, max_workers, directory)
    failed = sorted(name for name, outcome in results.items() if outcome['exit_code'] != 0)
    receipt = {'kind': 'local-parallel-check-not-ci-proof', 'max_workers': max_workers,
               'resource_snapshot': snapshot, 'jobs': results, 'failed': failed}
    (directory / 'run.json').write_text(json.dumps(receipt, indent=2) + '\n')
    if failed:
        for name in failed:
            print('FAILED: ' + name + ' (' + results[name]['log'] + ')', flush=True)
        return 1
    print('All {} jobs passed.'.format(len(jobs)), flush=True)
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
