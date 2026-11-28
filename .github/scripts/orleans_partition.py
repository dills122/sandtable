"""Deterministic, proof-carrying shards for the Orleans host (Postgres/Marten) test module.

Cna.OrleansHost.Tests is dominated by one partial class whose rows run serially
in a single process. This splits the module by
test method (all rows of a theory stay together) across N parallel CI jobs and
proves, at aggregate time, that the shards ran every discovered row exactly once.

Subcommands (run from the repository root):
  execute --shard I --total N   discover, run shard I (bounded retry on a
                                pre-row timeout), then check its own report
  verify  --total N DIR...      require shards 1..N, one identical plan, a
                                disjoint complete method allocation and a fully
                                passing, exactly matching report per shard
"""
import argparse
import hashlib
import json
from pathlib import Path
import os
import re
import signal
import subprocess
import sys
import tempfile
import time
import xml.etree.ElementTree as ET

ASSEMBLY = 'Cna.OrleansHost.Tests'
PROJECT = 'tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj'
BINARY = 'artifacts/bin/Cna.OrleansHost.Tests/release/Cna.OrleansHost.Tests.dll'
WEIGHTS = Path(__file__).with_name('orleans-weights.json')
ATTEMPT_CAP = 18 * 60
BOOTSTRAP_CAP = 3 * 60
ROW_CAP = 6 * 60
BOOTSTRAP_TIMEOUT = 124
ROW_TIMEOUT = 125
ATTEMPTS = 2

# Rows deliberately not run in PR CI (covered by orleans-slow.yml / the nightly
# workflow). Identical to the former --filter-not-display-name list.
EXCLUDED = frozenset({
    'Cna.OrleansHost.Tests.GrazianiPregameJournalTests.FinalizedRealSupplyPlanSurvivesFreshHostReplayWithoutStartingTheGame',
    'Cna.OrleansHost.Tests.CampaignDurableAuthorityJournalTests.RecommendationConnectorGivesGroundedAdviceAgainstARealCampaign',
    'Cna.OrleansHost.Tests.CampaignDurableAuthorityJournalTests.RecommendationConnectorGivesGroundedReserveAdviceAgainstARealCampaign',
    'Cna.OrleansHost.Tests.CampaignDurableAuthorityJournalTests.RecommendationConnectorGivesGroundedMovementAdviceAgainstARealCampaign',
    'Cna.OrleansHost.Tests.CampaignDurableAuthorityJournalTests.RecommendationConnectorGivesGroundedSecondReserveAdviceAgainstARealCampaign',
})

# Same rows verify_orleans_results.py always required, now checked across all shards.
REQUIRED = frozenset({
    'Cna.OrleansHost.Tests.TemporaryCampaignGrainTests.InProcessHostMatchesDirectCoreAndDeduplicatesConcurrentRequests',
    'Cna.OrleansHost.Tests.TemporaryCampaignGrainTests.SystemProgressionStopsForHumanAndRestartLosesTemporaryCampaign',
    'Cna.OrleansHost.Tests.TemporaryCampaignGrainTests.TwelveStepTraceStopsAtUnsupportedCombatWithoutGuessingTerminal',
})


def require(condition, message):
    if not condition:
        raise ValueError(message)


def method_of(row_name):
    return row_name.split('(', 1)[0]


def parse_listing(text):
    """Display names from `--list-tests` output (rows are the two-space-indented lines)."""
    return [line[2:].rstrip() for line in text.splitlines() if line.startswith('  ') and line.strip()
            and not line.strip().startswith('duration:')]


def inventory(rows):
    """method -> discovered row count, with the documented exclusions removed."""
    require(rows and len(set(rows)) == len(rows), 'Discovery produced no rows or duplicate rows')
    counts = {}
    for row in rows:
        method = method_of(row)
        require(method.startswith(ASSEMBLY + '.'), 'Foreign row discovered: ' + row)
        counts[method] = counts.get(method, 0) + 1
    require(EXCLUDED.issubset(counts), 'An excluded method is no longer discovered; update EXCLUDED')
    require(REQUIRED.issubset(counts), 'A required method is no longer discovered')
    return {method: count for method, count in counts.items() if method not in EXCLUDED}


def assign(counts, weights, total):
    """Longest-processing-time-first. Unknown methods get the median known weight."""
    require(total >= 1, 'Need at least one shard')
    known = sorted(weights[m] for m in counts if m in weights)
    default = known[len(known) // 2] if known else 1.0
    order = sorted(counts, key=lambda m: (-weights.get(m, default), m))
    loads, shards = [0.0] * total, [[] for _ in range(total)]
    for method in order:
        index = min(range(total), key=lambda i: (loads[i], i))
        shards[index].append(method)
        loads[index] += weights.get(method, default)
    return [sorted(s) for s in shards]


def digest(value):
    return hashlib.sha256(json.dumps(value, sort_keys=True, separators=(',', ':')).encode()).hexdigest()


def build_plan(rows, weights, total):
    counts = inventory(rows)
    shards = assign(counts, weights, total)
    return {'total_shards': total, 'counts': counts, 'shards': shards,
            'plan_sha256': digest({'counts': counts, 'shards': shards})}


def check_report(directory, expected_counts):
    """One passing xUnit XML whose rows exactly match the expected per-method counts."""
    reports = list(Path(directory).glob('*.xml'))
    require(len(reports) == 1, f'Expected one Orleans test XML in {directory}, found {len(reports)}')
    assembly = ET.parse(reports[0]).getroot().find('assembly')
    require(assembly is not None and Path(assembly.attrib.get('name', '')).stem == ASSEMBLY, 'Wrong Orleans test assembly')
    rows = assembly.findall('.//test')
    names = [row.attrib.get('name', '') for row in rows]
    require(int(assembly.attrib['total']) == len(rows) and int(assembly.attrib['passed']) == len(rows)
            and all(int(assembly.attrib[k]) == 0 for k in ('failed', 'errors', 'skipped', 'not-run'))
            and all(r.attrib.get('result') == 'Pass' for r in rows)
            and all(names) and len(set(names)) == len(names), 'Incomplete or failed Orleans test rows')
    seen = {}
    for row in rows:
        method = row.attrib.get('type', '') + '.' + row.attrib.get('method', '')
        seen[method] = seen.get(method, 0) + 1
    require(seen == expected_counts, 'Executed rows differ from the planned shard rows')
    return seen


def verify(directories, total):
    plans = {}
    for directory in directories:
        plan = json.loads((Path(directory) / 'orleans-plan.json').read_text())
        index = plan['shard']
        require(isinstance(index, int) and 1 <= index <= total and index not in plans, f'Bad or duplicate shard {index}')
        require(plan['total_shards'] == total, 'Shard ran with a different shard count')
        plans[index] = (directory, plan)
    require(sorted(plans) == list(range(1, total + 1)), 'Missing Orleans shard results')
    first = plans[1][1]
    require(all(p['plan_sha256'] == first['plan_sha256'] and p['counts'] == first['counts']
                and p['shards'] == first['shards'] for _, p in plans.values()), 'Shards disagree on the plan')
    require(digest({'counts': first['counts'], 'shards': first['shards']}) == first['plan_sha256'], 'Plan digest mismatch')
    allocated = [m for shard in first['shards'] for m in shard]
    require(len(allocated) == len(set(allocated)) and set(allocated) == set(first['counts']), 'Allocation is not disjoint and complete')
    executed = {}
    for index, (directory, plan) in plans.items():
        shard_methods = first['shards'][index - 1]
        require(plan['methods'] == shard_methods, 'Shard ran a different method set than planned')
        executed.update(check_report(directory, {m: first['counts'][m] for m in shard_methods}))
    require(REQUIRED.issubset(executed), 'Required Orleans rows missing from the union')
    total_rows = sum(executed.values())
    return {'module': ASSEMBLY, 'shards': total, 'methods': len(executed), 'rows': total_rows}


def discover(binary=BINARY):
    result = subprocess.run(['dotnet', 'exec', binary, '--list-tests', '--pre-enumerate-theories', 'on', '--no-ansi'], capture_output=True, text=True, check=True, timeout=300)
    return parse_listing(result.stdout)


class RowProgress:
    """Track MTP TestNode lifecycle messages by UID, not method (theories may overlap)."""
    event = re.compile(r'TestNode = TestNode \{ Uid = TestNodeUid \{ Value = ([^}]+) \}, DisplayName = (.*?), Properties = \[')
    state = re.compile(r'\b(InProgress|Passed|Failed|Skipped|Error|Timeout|Cancelled|Canceled)TestNodeStateProperty\b')

    def __init__(self):
        self.active = {}
        self.started = False

    def observe(self, line, now):
        event, state = self.event.search(line), self.state.search(line)
        if not event or not state:
            return None
        uid, name = event.groups()
        outcome = state.group(1)
        if outcome == 'InProgress' and uid not in self.active:
            self.started = True
            self.active[uid] = (name, now)
            return f'START {name}'
        if outcome != 'InProgress' and uid in self.active:
            name, start = self.active.pop(uid)
            return f'FINISH {name} ({now - start:.1f}s, {outcome})'
        return None

    def expired(self, now, cap):
        return [name for name, start in self.active.values() if now - start >= cap]


def stop_process(process):
    """Terminate the entire test process group, including any child host."""
    try:
        os.killpg(process.pid, signal.SIGTERM)
        process.wait(timeout=10)
    except (subprocess.TimeoutExpired, ProcessLookupError):
        pass
    finally:
        # The leader may exit before its child hosts; reap the whole group.
        try:
            os.killpg(process.pid, signal.SIGKILL)
        except ProcessLookupError:
            pass
        process.wait()


def run_attempt(command, log_path, cap=ATTEMPT_CAP, bootstrap_cap=BOOTSTRAP_CAP, row_cap=ROW_CAP):
    """Flush runner and row lifecycle logs while bounding bootstrap, each row and the shard."""
    # MTP trace messages include row UIDs and state transitions even when dotnet test
    # buffers console output. A unique directory keeps old attempts out of the watchdog.
    diagnostics = Path(tempfile.mkdtemp(prefix=f'{log_path.stem}-diagnostics-', dir=log_path.parent))
    command = [*command, '--diagnostic', '--diagnostic-synchronous-write',
               '--diagnostic-output-directory', str(diagnostics.resolve()), '--no-ansi']
    progress = RowProgress()
    offsets = {}
    start = time.monotonic()
    raw_path = diagnostics / 'runner.log'
    with raw_path.open('w') as raw, raw_path.open() as console, log_path.open('w', buffering=1) as log:
        def emit(line):
            print(line, flush=True)
            print(line, file=log, flush=True)

        def drain():
            for line in console:
                emit(line.rstrip('\n'))
            for path in diagnostics.glob('*.diag'):
                with path.open() as stream:
                    stream.seek(offsets.get(path, 0))
                    while line := stream.readline():
                        if not line.endswith('\n'):
                            break  # writer may still be writing this record; read it again
                        offsets[path] = stream.tell()
                        message = progress.observe(line, time.monotonic())
                        if message:
                            emit(message)

        with subprocess.Popen(command, stdout=raw, stderr=subprocess.STDOUT, start_new_session=True) as process:
            while True:
                drain()
                status = process.poll()
                if status is not None:
                    drain()
                    # A report-less process failure after work began is not a bootstrap retry.
                    return ROW_TIMEOUT if status in (BOOTSTRAP_TIMEOUT, ROW_TIMEOUT) and progress.started else status
                now = time.monotonic()
                expired = progress.expired(now, row_cap)
                if expired:
                    emit(f'::error::Row exceeded {row_cap}s: {", ".join(expired)}')
                    status = ROW_TIMEOUT
                elif not progress.started and now - start >= bootstrap_cap:
                    emit(f'::error::No row started within {bootstrap_cap}s; test runner or fixture bootstrap timed out.')
                    status = BOOTSTRAP_TIMEOUT
                elif now - start >= cap:
                    emit(f'::error::Shard exceeded {cap}s; active rows: {", ".join(name for name, _ in progress.active.values()) or "none"}')
                    status = ROW_TIMEOUT
                else:
                    time.sleep(0.25)
                    continue
                stop_process(process)
                drain()
                return status


def execute(shard, total, results, runner=run_attempt, discoverer=discover, attempts=ATTEMPTS, weights_path=None):
    weights = json.loads(Path(weights_path or WEIGHTS).read_text())
    plan = build_plan(discoverer(), weights, total)
    require(1 <= shard <= total, 'Shard out of range')
    methods = plan['shards'][shard - 1]
    require(methods, 'Empty shard')
    results = Path(results)
    results.mkdir(parents=True, exist_ok=True)
    (results / 'orleans-plan.json').write_text(json.dumps({**plan, 'shard': shard, 'methods': methods}, indent=1, sort_keys=True))
    status = 1
    for attempt in range(1, attempts + 1):
        for stale in results.glob('*.xml'):
            stale.unlink()
        command = ['dotnet', 'test', '--project', PROJECT, '--configuration', 'Release', '--no-build', '--no-restore',
                   '--report-xunit-xml', '--results-directory', str(results), '--filter-method', *methods]
        status = runner(command, results / f'console-attempt-{attempt}.log')
        if status == 0:
            break
        if status != BOOTSTRAP_TIMEOUT or list(results.glob('*.xml')):
            print(f'::error::Orleans shard {shard} failed on attempt {attempt} (exit {status}) -- not a retryable pre-row timeout.', flush=True)
            return status
        if attempt == attempts:
            print(f'::error::Orleans shard {shard} timed out before any row started after {attempts} attempts (exit {status}).', flush=True)
            return status
        print(f'::warning::Orleans shard {shard} timed out before any row started on attempt {attempt} (exit {status}). Retrying bootstrap once.', flush=True)
    check_report(results, {m: plan['counts'][m] for m in methods})
    print(json.dumps({'shard': shard, 'methods': len(methods), 'rows': sum(plan['counts'][m] for m in methods)}, sort_keys=True))
    return 0


def main(argv):
    parser = argparse.ArgumentParser()
    sub = parser.add_subparsers(dest='command', required=True)
    run = sub.add_parser('execute')
    run.add_argument('--shard', type=int, required=True)
    run.add_argument('--total', type=int, required=True)
    run.add_argument('--results', default='artifacts/orleans-results')
    run.add_argument('--weights', type=Path, default=None, help='Optional timing hints (default: PR CI weights)')
    check = sub.add_parser('verify')
    check.add_argument('--total', type=int, required=True)
    check.add_argument('directories', nargs='+')
    args = parser.parse_args(argv)
    if args.command == 'execute':
        return execute(args.shard, args.total, args.results, weights_path=args.weights)
    print(json.dumps(verify(args.directories, args.total), sort_keys=True))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
