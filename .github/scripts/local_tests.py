"""Focused local tests; CI remains responsible for exhaustive coverage proof."""
import argparse
import json
from pathlib import Path
import shlex
import subprocess
import sys
import tempfile
import time

sys.dont_write_bytecode = True

import long_suite_partition as suites
import r02_partition as r02
import test_partition as partition

ROOT = Path(__file__).resolve().parents[2]
PROJECTS = {
    'core': ['Cna.Core.Tests'],
    'host': ['Cna.OrleansHost.Tests'],
    'runner': ['Cna.ExerciseRunner.Tests'],
    'exercise': ['Cna.ExerciseRunner.Tests'],
    'contracts': ['Cna.Game.Contracts.Tests', 'Cna.Intelligence.Contracts.Tests'],
    'boundary': ['Cna.Core.Tests'],
}
SHARDS = (*suites.LONG_JOBS, *suites.FAST_JOBS, 'r02-1', 'r02-2', 'r02-3')


def commands_for(suite, configuration, class_filter=None, method_filter=None):
    commands = []
    for module in PROJECTS[suite]:
        command = ['dotnet', 'test', '--project', partition.project(module),
                   '--configuration', configuration, '--no-build', '--no-restore']
        if suite == 'boundary':
            command += ['--filter-trait', 'Boundary=UserSpace']
        if class_filter:
            command += ['--filter-class', class_filter]
        if method_filter:
            command += ['--filter-method', method_filter]
        commands.append(command)
    return commands


def shard_commands(shard, proof, weights):
    if shard in suites.LONG_JOBS + suites.FAST_JOBS:
        groups = suites.make_plan(proof['legacy_core_rows'], weights)['groups']
    else:
        groups = proof['groups']
    selected = [(name, entry) for name, entry in sorted(groups.items()) if entry['job'] == shard]
    if not selected:
        raise ValueError('No groups selected for ' + shard)
    commands = []
    # Sequential local processes avoid multiplying CI's 4 lanes across one laptop.
    for name, entry in selected:
        command = commands_for('core', 'Release')[0]
        if shard.startswith('r02-'):
            command += ['--filter-class', entry['class']]
        elif entry['selector_kind'] == 'display':
            command += ['--filter-display-name', *sorted(row[2] for row in entry['rows'])]
        else:
            command += ['--filter-class', *sorted({row[1].rsplit('.', 1)[0] for row in entry['rows']})]
        commands.append((name, command))
    return commands


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('suite', choices=['list', *PROJECTS, *SHARDS])
    filters = parser.add_mutually_exclusive_group()
    filters.add_argument('--class', dest='class_filter', help='Quoted xUnit class pattern')
    filters.add_argument('--method', dest='method_filter', help='Quoted xUnit method pattern')
    parser.add_argument('--configuration', choices=['Debug', 'Release'], default='Release')
    parser.add_argument('--no-build', action='store_true', help='Reuse binaries; caller must ensure they match source')
    parser.add_argument('--dry-run', action='store_true', help='Print project commands; shards still build/discover to resolve exact rows')
    args = parser.parse_args(argv)
    if args.suite == 'list':
        print('Projects: core host runner contracts boundary\nCI shards: ' + ' '.join(SHARDS))
        print('Examples: just test-suite host; just test-class core "*CampaignContact*"; just test-shard fast-01')
        return 0
    shard = args.suite in SHARDS
    if shard and (args.class_filter or args.method_filter or args.configuration != 'Release'):
        parser.error('CI shards require Release and cannot be combined with class/method filters')
    if args.suite == 'boundary' and (args.class_filter or args.method_filter):
        parser.error('Use core for class/method selection; boundary always runs the entire boundary suite')
    commands = [] if shard else list(enumerate(commands_for(
        args.suite, args.configuration, args.class_filter, args.method_filter)))
    if args.dry_run and not shard:
        for _, command in commands:
            print(shlex.join(command))
        return 0
    artifact_root = ROOT / 'artifacts/local-tests'
    artifact_root.mkdir(parents=True, exist_ok=True)
    directory = Path(tempfile.mkdtemp(prefix=args.suite + '-', dir=artifact_root))
    receipt = {'suite': args.suite, 'kind': 'local-selected-tests-not-ci-proof',
               'configuration': args.configuration, 'no_build': args.no_build,
               'status': 'running', 'commands': []}
    receipt_path = directory / 'run.json'
    started = time.monotonic()
    receipt_path.write_text(json.dumps(receipt, indent=2) + '\n')
    print('Local selection only; logs: ' + str(directory), flush=True)
    try:
        if not args.no_build:
            targets = ['Sandtable.slnx', '.github/scripts/ReleaseTestRows/ReleaseTestRows.csproj'] if shard else [
                partition.project(module) for module in PROJECTS[args.suite]]
            for target in targets:
                subprocess.run(['dotnet', 'build', target, '--configuration', args.configuration], cwd=ROOT, check=True)
        if shard:
            # Reuse CI's actual-row discovery and allocation. No copied class lists,
            # stale cached plan, Linux cgroup supervisor, or exact-head CI claim.
            import os
            os.chdir(ROOT)
            sha = subprocess.check_output(['git', 'rev-parse', 'HEAD'], text=True).strip()
            proof = r02.discover(directory / 'discovery', r02.FIXTURE_PATH,
                                 sha, partition.identity(sha), 'local')
            weights = json.loads((ROOT / suites.WEIGHTS_PATH).read_text())
            commands = shard_commands(args.suite, proof, weights)
        for index, (group, command) in enumerate(commands):
            results = directory / ('results-' + str(index))
            command += ['--report-xunit-xml', '--results-directory', str(results)]
            print(shlex.join(command), flush=True)
            if args.dry_run:
                continue
            log = directory / (str(index) + '.log')
            command_started = time.monotonic()
            with log.open('w') as output:
                result = subprocess.run(command, cwd=ROOT, stdout=output, stderr=subprocess.STDOUT)
            print(log.read_text(), end='', flush=True)
            receipt['commands'].append({'group': group, 'command': command, 'exit_code': result.returncode,
                                        'elapsed_seconds': round(time.monotonic() - command_started, 3), 'log': str(log)})
            if result.returncode:
                raise subprocess.CalledProcessError(result.returncode, command)
        receipt['status'] = 'planned' if args.dry_run else 'passed'
        return 0
    except (subprocess.CalledProcessError, ValueError, OSError) as error:
        receipt['status'] = 'failed'
        receipt['error'] = str(error)
        print(str(error), flush=True)
        return 1
    except KeyboardInterrupt:
        receipt['status'] = 'interrupted'
        return 130
    finally:
        receipt['elapsed_seconds'] = round(time.monotonic() - started, 3)
        receipt_path.write_text(json.dumps(receipt, indent=2) + '\n')


if __name__ == '__main__':
    raise SystemExit(main())
