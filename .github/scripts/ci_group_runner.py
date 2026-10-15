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
    result = subprocess.run(['ps', '-axo', 'pid=,pgid=,stat='], capture_output=True, text=True, check=True)
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


def stop_owned_group(process):
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


def run_child(command, directory, template, deadline, cancellation=None):
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
    except (OSError, subprocess.CalledProcessError) as error:
        cancellation.set()
        status['launch_error'] = str(error)
        status['exit_code'] = 127
        status['cancelled'] = True
    finally:
        if process is not None and process.poll() is None:
            status['cancelled'] = True
            stop_owned_group(process)
            status['exit_code'] = process.returncode
        status['completed_utc'] = datetime.now(timezone.utc).isoformat()
        status['elapsed_seconds'] = time.monotonic() - started
        temporary = directory / 'status.json.tmp'
        temporary.write_text(json.dumps(status, indent=2) + '\n')
        temporary.replace(directory / 'status.json')
    return status


def run_job(groups, allocation, job, root, binding, binary, timeout_seconds=720, command_builder=execution_command):
    from concurrent.futures import ThreadPoolExecutor
    from r02_partition import MODULE, require, validate_allocation
    validate_allocation(allocation, {'groups': groups})
    require(job in ('r02-1', 'r02-2', 'r02-3'), 'Unknown R02 job')
    require(type(timeout_seconds) in (int, float) and 0 < timeout_seconds <= 720, 'Invalid job execution deadline')
    root = Path(root)
    require(not root.exists(), 'Stale job group directory')
    root.mkdir(parents=True)
    expected = {name for name, entry in allocation.items() if entry['job'] == job}
    cancellation = threading.Event()
    deadline = time.monotonic() + timeout_seconds
    previous = {}
    if threading.current_thread() is threading.main_thread():
        for signum in (signal.SIGTERM, signal.SIGINT):
            previous[signum] = signal.signal(signum, lambda _signum, _frame: cancellation.set())

    def lane(number):
        statuses = {}
        for group in allocation:
            entry = allocation[group]
            if entry['job'] != job or entry['lane'] != number:
                continue
            if cancellation.is_set() or time.monotonic() >= deadline:
                break
            directory = root / group
            template = dict(binding, group=group, module=MODULE, class_name=groups[group]['class'], module_path=str(Path(binary).resolve()))
            statuses[group] = run_child(command_builder(binary, directory, groups[group]['class']), directory, template, deadline, cancellation)
        return statuses

    try:
        with ThreadPoolExecutor(max_workers=4) as workers:
            futures = [workers.submit(lane, number) for number in range(1, 5)]
            statuses = {}
            for future in futures:
                statuses.update(future.result())
        require(set(statuses) == expected and not cancellation.is_set(), 'Cancelled/incomplete job process set')
        return statuses
    finally:
        cancellation.set()
        for signum, handler in previous.items():
            signal.signal(signum, handler)
