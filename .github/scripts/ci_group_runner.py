"""Run bounded, isolated class processes; final XML never replaces real exit status."""
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import re
import signal
import subprocess
import threading
import time

from test_partition import execution_environment


def execution_command(binary, directory, class_name):
    if not re.fullmatch(r'Cna\.Core\.Tests\.[A-Za-z0-9_.]+', class_name):
        raise ValueError('Invalid exact class filter')
    directory = Path(directory).resolve()
    return ['dotnet', 'exec', str(Path(binary).resolve()), '--no-ansi', '--output', 'Detailed', '--diagnostic', '--diagnostic-verbosity', 'Trace', '--diagnostic-synchronous-write', '--diagnostic-output-directory', str(directory / 'diagnostics'), '--report-xunit-xml', '--results-directory', str(directory / 'results'), '--filter-class', class_name]


def owned_session_members(session_id):
    """Only processes still in the new session/group created by this exact Popen."""
    result = subprocess.run(['ps', '-axo', 'pid=,pgid=,stat='], capture_output=True, text=True, check=True, timeout=2)
    members = []
    for line in result.stdout.splitlines():
        pid, group, state = line.split(None, 2)
        if int(group) != session_id or state.startswith('Z'):
            continue
        try:
            if os.getsid(int(pid)) == session_id:
                members.append(int(pid))
        except ProcessLookupError:
            pass
    return members


def _stop_owned_group(process):
    # No process-name matching: only this Popen's newly created session/group.
    for signum, grace in ((signal.SIGTERM, 2), (signal.SIGKILL, 2)):
        if owned_session_members(process.pid):
            try:
                os.killpg(process.pid, signum)
            except ProcessLookupError:
                pass
        until = time.monotonic() + grace
        while owned_session_members(process.pid) and time.monotonic() < until:
            time.sleep(0.02)
    if owned_session_members(process.pid):
        raise ValueError('Owned child-session survivors remain')
    process.wait(timeout=2)



def stop_owned_group(process):
    try:
        _stop_owned_group(process)
    except (OSError, subprocess.SubprocessError, ValueError) as error:
        # With inspection unavailable, only the still-live exact Popen leader can
        # establish session ownership. Never substitute process-name matching.
        if process.poll() is None:
            try:
                if os.getsid(process.pid) == process.pid and os.getpgid(process.pid) == process.pid:
                    os.killpg(process.pid, signal.SIGKILL)
            except ProcessLookupError:
                pass
            process.wait(timeout=2)
        raise ValueError('Cleanup inspection failed; ownership audit incomplete: ' + str(error)) from error


def run_child(command, directory, template, deadline, cancellation=None, monitor=None):
    cancellation = cancellation if cancellation is not None else threading.Event()
    if cancellation.is_set() or time.monotonic() >= deadline:
        raise ValueError('Job cancelled/deadline reached before child launch')
    directory = Path(directory)
    try:
        directory.mkdir(parents=True, exist_ok=False)
    except FileExistsError as error:
        raise ValueError('Stale child output directory') from error
    (directory / 'results').mkdir()
    (directory / 'temp').mkdir()
    (directory / 'cli-home').mkdir()
    environment = execution_environment(directory)
    environment.update({'TMPDIR': str((directory / 'temp').resolve()), 'DOTNET_CLI_HOME': str((directory / 'cli-home').resolve())})
    started = time.monotonic()
    status = dict(template, evidence_version=1, output_directory=str(directory.resolve()), command=command, started_utc=datetime.now(timezone.utc).isoformat(), process_id=None, exit_code=127, cancelled=False)
    process = None
    try:
        with (directory / 'execution.log').open('w') as output:
            process = subprocess.Popen(command, stdin=subprocess.DEVNULL, stdout=output, stderr=subprocess.STDOUT, shell=False, env=environment, start_new_session=True)
            status['process_id'] = process.pid
            if monitor is not None: monitor.register(template['group'], process.pid)
            while process.poll() is None:
                if cancellation.is_set() or time.monotonic() >= deadline:
                    cancellation.set()
                    status['cancelled'] = True
                    stop_owned_group(process)
                    break
                try:
                    process.wait(timeout=0.05)
                except subprocess.TimeoutExpired:
                    pass
            status['exit_code'] = process.wait()
            if owned_session_members(process.pid):
                status['cancelled'] = True
                stop_owned_group(process)
                status['exit_code'] = 125
    except (OSError, subprocess.SubprocessError, ValueError) as error:
        cancellation.set()
        status['launch_error'] = str(error)
        if process is not None: status['cleanup_error'] = str(error)
        status['exit_code'] = 127
        status['cancelled'] = True
    finally:
        if process is not None and process.poll() is None:
            status['cancelled'] = True
            try:
                stop_owned_group(process)
            except (OSError, subprocess.SubprocessError, ValueError) as error:
                cancellation.set()
                status['cleanup_error'] = str(error)
            status['exit_code'] = process.returncode if process.returncode not in (None, 0) else 127
        if monitor is not None: monitor.unregister(template['group'])
        status['completed_utc'] = datetime.now(timezone.utc).isoformat()
        status['elapsed_seconds'] = time.monotonic() - started
        temporary = directory / 'status.json.tmp'
        temporary.write_text(json.dumps(status, indent=2) + '\n')
        temporary.replace(directory / 'status.json')
    return status


def run_job(groups, allocation, job, root, binding, binary, timeout_seconds=720, command_builder=execution_command):
    from concurrent.futures import ThreadPoolExecutor
    from r02_partition import MODULE, candidate_allocation, require, validate_allocation
    validate_allocation(allocation, {'groups': groups})
    require(job in ('r02-1', 'r02-2', 'r02-3'), 'Unknown R02 job')
    require(type(timeout_seconds) in (int, float) and 0 < timeout_seconds <= 720, 'Invalid job execution deadline')
    root = Path(root)
    require(not root.exists(), 'Stale job group directory')
    root.mkdir(parents=True)
    expected = {name for name, entry in allocation.items() if entry['job'] == job}
    cancellation = threading.Event()
    deadline = time.monotonic() + timeout_seconds
    guard = ResourceGuard(cancellation, job, binding)
    previous, statuses = {}, {}
    if threading.current_thread() is threading.main_thread():
        for signum in (signal.SIGTERM, signal.SIGINT):
            previous[signum] = signal.signal(signum, lambda _signum, _frame: cancellation.set())

    def child(group):
        directory = root / group
        template = dict(binding, group=group, phase=allocation[group]['phase'], module=MODULE, class_name=groups[group]['class'], module_path=str(Path(binary).resolve()))
        return run_child(command_builder(binary, directory, groups[group]['class']), directory, template, deadline, cancellation, guard)

    def lane(number):
        results = {}
        for group in candidate_allocation():
            entry = allocation[group]
            if entry['job'] != job or entry['lane'] != number or entry['phase'] != 'parallel': continue
            if cancellation.is_set() or time.monotonic() >= deadline: break
            results[group] = child(group)
        return results

    try:
        guard.start()
        with ThreadPoolExecutor(max_workers=4) as workers:
            futures = [workers.submit(lane, number) for number in range(1, 5)]
            for future in futures: statuses.update(future.result())
        for group in candidate_allocation():
            if allocation[group]['job'] == job and allocation[group]['phase'] == 'exclusive':
                require(not cancellation.is_set() and not guard.active, 'Cancelled/undrained before exclusive phase')
                guard.preflight()
                statuses[group] = child(group)
        require(set(statuses) == expected and not cancellation.is_set(), 'Cancelled/incomplete job process set')
    finally:
        cancellation.set()
        evidence = guard.finish()
        (root.parent / (job + '-resource.json')).write_text(json.dumps(evidence, indent=2) + '\n')
        for signum, handler in previous.items(): signal.signal(signum, handler)
    require(evidence['abort_reason'] is None, 'Resource guard failed: ' + str(evidence['abort_reason']))
    return statuses


GIB = 2**30
RESOURCE_POLICY = {'per_group_limit_bytes': 4 * GIB, 'aggregate_limit_bytes': 8 * GIB, 'min_effective_limit_bytes': 12 * GIB, 'min_preflight_available_bytes': 8 * GIB, 'min_runtime_available_bytes': 4 * GIB, 'min_cpu_count': 4}


def linux_resources(proc_root=Path('/proc'), cgroup_root=Path('/sys/fs/cgroup')):
    from r02_partition import require
    proc_root, cgroup_root = Path(proc_root), Path(cgroup_root).resolve()
    text = (proc_root / 'meminfo').read_text()
    total = int(re.search(r'^MemTotal:\s+([0-9]+) kB$', text, re.MULTILINE)[1]) * 1024
    available = int(re.search(r'^MemAvailable:\s+([0-9]+) kB$', text, re.MULTILINE)[1]) * 1024
    rows = [line[3:] for line in (proc_root / 'self' / 'cgroup').read_text().splitlines() if line.startswith('0::')]
    require(len(rows) == 1 and rows[0].startswith('/') and '..' not in Path(rows[0]).parts, 'Unsupported cgroup-v2 topology')
    directory = (cgroup_root / rows[0].lstrip('/')).resolve()
    require(directory.is_relative_to(cgroup_root) and directory.is_dir(), 'Missing effective cgroup directory')
    cpus = min(os.cpu_count() or 1, len(os.sched_getaffinity(0)) if hasattr(os, 'sched_getaffinity') else os.cpu_count() or 1)
    limit, constraints = total, []
    while True:
        maximum = directory / 'memory.max'
        if maximum.exists():
            value = maximum.read_text().strip()
            if value != 'max':
                maximum_bytes = int(value); current = int((directory / 'memory.current').read_text())
                require(maximum_bytes > 0 and current >= 0, 'Invalid cgroup memory limit/current')
                limit = min(limit, maximum_bytes); available = min(available, max(0, maximum_bytes - current))
                constraints.append({'path': str(directory), 'limit_bytes': maximum_bytes, 'current_bytes': current})
        quota = directory / 'cpu.max'
        if quota.exists():
            value, period = quota.read_text().split()
            require(int(period) > 0, 'Invalid CPU period')
            if value != 'max': cpus = min(cpus, int(value) / int(period))
        if directory == cgroup_root: break
        directory = directory.parent
    return {'platform': 'Linux', 'physical_memory_bytes': total, 'effective_memory_limit_bytes': limit, 'memory_available_bytes': available, 'effective_cpu_count': cpus, 'cgroup_limits': constraints, 'memory_available_source': 'MemAvailable and ancestor cgroup remaining'}


def resource_snapshot():
    import platform
    from r02_partition import require
    system = platform.system()
    if system == 'Linux': return linux_resources()
    require(system == 'Darwin', 'Unsupported resource platform')
    total = int(subprocess.check_output(['sysctl', '-n', 'hw.memsize'], text=True, timeout=2))
    vm = subprocess.check_output(['vm_stat'], text=True, timeout=2)
    page_size = int(re.search(r'page size of ([0-9]+) bytes', vm)[1])
    available = sum(int(re.search(r'^Pages ' + label + r':\s+([0-9]+)\.', vm, re.MULTILINE)[1]) for label in ('free', 'inactive', 'speculative')) * page_size
    return {'platform': system, 'physical_memory_bytes': total, 'effective_memory_limit_bytes': total, 'memory_available_bytes': available, 'effective_cpu_count': os.cpu_count(), 'cgroup_limits': [], 'memory_available_source': 'Darwin free+inactive+speculative pages; not Linux MemAvailable'}


def validate_preflight(snapshot):
    import math
    from r02_partition import require
    require(all(type(snapshot[key]) is int for key in ('effective_memory_limit_bytes', 'memory_available_bytes')), 'Invalid memory units/types')
    require(type(snapshot['effective_cpu_count']) in (int, float) and math.isfinite(snapshot['effective_cpu_count']), 'Invalid effective CPU count')
    require(snapshot['platform'] in ('Linux', 'Darwin'), 'Unsupported resource platform')
    require(snapshot['effective_memory_limit_bytes'] >= RESOURCE_POLICY['min_effective_limit_bytes'], 'Effective memory limit below 12GiB')
    require(snapshot['memory_available_bytes'] >= RESOURCE_POLICY['min_preflight_available_bytes'], 'Available memory below 8GiB preflight')
    require(snapshot['effective_cpu_count'] >= RESOURCE_POLICY['min_cpu_count'], 'Fewer than four effective CPUs')
    return True


def validate_sample(snapshot, groups):
    from r02_partition import require
    require(snapshot['effective_memory_limit_bytes'] >= RESOURCE_POLICY['min_effective_limit_bytes'], 'Effective memory limit changed')
    require(snapshot['memory_available_bytes'] >= RESOURCE_POLICY['min_runtime_available_bytes'], 'Available memory below 4GiB reserve')
    require(all(type(value) is int and 0 <= value <= RESOURCE_POLICY['per_group_limit_bytes'] for value in groups.values()), 'Per-child session RSS exceeds 4GiB')
    require(sum(groups.values()) <= RESOURCE_POLICY['aggregate_limit_bytes'], 'Aggregate owned RSS exceeds 8GiB')
    return True


def kernel_peak_bytes(value, system):
    import math
    from r02_partition import require
    require(system in ('Linux', 'Darwin') and type(value) in (int, float) and math.isfinite(value) and value >= 0, 'Unsupported kernel RSS units')
    return int(value * (1024 if system == 'Linux' else 1))


def validate_kernel_peak(value):
    from r02_partition import require
    require(type(value) is int and 0 <= value <= RESOURCE_POLICY['per_group_limit_bytes'], 'Kernel completed-child RSS exceeded 4GiB')
    return True


def session_rss(active):
    result = subprocess.run(['ps', '-axo', 'pid=,pgid=,rss='], capture_output=True, text=True, check=True, timeout=2)
    by_session = {pid: group for group, pid in active.items()}
    values = {group: 0 for group in active}
    for line in result.stdout.splitlines():
        pid, pgid, rss = map(int, line.split())
        if pgid not in by_session: continue
        try:
            if os.getsid(pid) == pgid: values[by_session[pgid]] += rss * 1024
        except ProcessLookupError:
            pass
    return values


class ResourceGuard:
    def __init__(self, cancellation, job, binding):
        self.cancellation = cancellation
        self.lock = threading.Lock()
        self.active = {}
        self.stop = threading.Event()
        self.thread = None
        self.started = time.monotonic()
        self.evidence = {'schema_version': 1, 'job': job, 'binding': dict(binding), 'policy': dict(RESOURCE_POLICY), 'phase_preflights': [], 'peak_group_rss_bytes': {}, 'peak_aggregate_rss_bytes': 0, 'min_available_bytes': None, 'sample_count': 0, 'abort_reason': None, 'kernel_children_peak_rss_bytes': None, 'kernel_peak_kind': 'job-wide completed-children maximum; not per-child attribution', 'complete': False}

    def abort(self, error):
        with self.lock:
            if self.evidence['abort_reason'] is None: self.evidence['abort_reason'] = str(error)
        self.cancellation.set()

    def preflight(self):
        try:
            snapshot = resource_snapshot()
            with self.lock: self.evidence['phase_preflights'].append(snapshot)
            validate_preflight(snapshot)
            return snapshot
        except Exception as error:
            self.abort(error)
            raise ValueError('Resource preflight failed: ' + str(error)) from error

    def register(self, group, pid):
        with self.lock:
            self.active[group] = pid
            self.evidence['peak_group_rss_bytes'].setdefault(group, 0)

    def unregister(self, group):
        with self.lock: self.active.pop(group, None)

    def sample_once(self):
        try:
            with self.lock: active = dict(self.active)
            snapshot = resource_snapshot()
            values = session_rss(active)
            with self.lock:
                for group, value in values.items(): self.evidence['peak_group_rss_bytes'][group] = max(value, self.evidence['peak_group_rss_bytes'][group])
                self.evidence['peak_aggregate_rss_bytes'] = max(sum(values.values()), self.evidence['peak_aggregate_rss_bytes'])
                previous = self.evidence['min_available_bytes']
                self.evidence['min_available_bytes'] = snapshot['memory_available_bytes'] if previous is None else min(previous, snapshot['memory_available_bytes'])
                self.evidence['sample_count'] += 1
            validate_sample(snapshot, values)
        except Exception as error:
            self.abort(error)

    def start(self):
        self.preflight()
        def monitor():
            while not self.stop.is_set():
                self.sample_once()
                if self.stop.wait(0.2): break
        self.thread = threading.Thread(target=monitor, name='owned-session-resource-guard', daemon=True)
        self.thread.start()

    def finish(self):
        import platform
        import resource
        self.stop.set()
        if self.thread is not None:
            self.thread.join(timeout=5)
            if self.thread.is_alive(): self.abort('Resource monitor did not finish')
        try:
            value = kernel_peak_bytes(resource.getrusage(resource.RUSAGE_CHILDREN).ru_maxrss, platform.system())
            self.evidence['kernel_children_peak_rss_bytes'] = value
            validate_kernel_peak(value)
        except Exception as error:
            self.abort(error)
        self.evidence['elapsed_seconds'] = time.monotonic() - self.started
        self.evidence['complete'] = True
        return self.evidence


def validate_resource_evidence(evidence, job, binding, groups):
    import math
    from r02_partition import require
    require(type(evidence['schema_version']) is int and evidence['schema_version'] == 1 and evidence['job'] == job and evidence['binding'] == binding, 'Stale/foreign resource proof')
    require(evidence['policy'] == RESOURCE_POLICY and evidence['complete'] is True and evidence['abort_reason'] is None, 'Aborted/unsupported resource proof')
    require(len(evidence['phase_preflights']) == (2 if job == 'r02-1' else 1), 'Missing phase resource preflight')
    for snapshot in evidence['phase_preflights']: validate_preflight(snapshot)
    require(set(evidence['peak_group_rss_bytes']) == set(groups), 'Missing/extra measured child group')
    require(all(type(value) is int and 0 <= value <= RESOURCE_POLICY['per_group_limit_bytes'] for value in evidence['peak_group_rss_bytes'].values()), 'Per-child sampled RSS limit exceeded')
    require(type(evidence['peak_aggregate_rss_bytes']) is int and 0 <= evidence['peak_aggregate_rss_bytes'] <= RESOURCE_POLICY['aggregate_limit_bytes'], 'Aggregate RSS limit exceeded')
    require(type(evidence['sample_count']) is int and evidence['sample_count'] > 0 and type(evidence['min_available_bytes']) is int and evidence['min_available_bytes'] >= RESOURCE_POLICY['min_runtime_available_bytes'], 'Missing samples/host reserve')
    require(evidence['kernel_peak_kind'] == 'job-wide completed-children maximum; not per-child attribution' and evidence['kernel_children_peak_rss_bytes'] > 0, 'Missing/incorrect kernel peak attribution')
    validate_kernel_peak(evidence['kernel_children_peak_rss_bytes'])
    require(type(evidence['elapsed_seconds']) in (int, float) and math.isfinite(evidence['elapsed_seconds']) and 0 <= evidence['elapsed_seconds'] <= 750, 'Invalid resource execution/cleanup duration')
    return True
