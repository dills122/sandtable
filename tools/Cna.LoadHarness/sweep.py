"""Repeat full-policy sweeps and preserve each failed campaign's decision/attempt prefix."""
import argparse
import concurrent.futures
import json
import pathlib
import subprocess

DLL = pathlib.Path(__file__).resolve().parents[2] / 'artifacts/bin/Cna.LoadHarness/release/Cna.LoadHarness.dll'


def run_case(output, driver, store, campaigns, seed, repeat, timeout):
    output.mkdir(parents=True, exist_ok=True)
    run = output / 'run'
    command = ['dotnet', str(DLL), '--driver', driver, '--store', store,
               '--campaigns', str(campaigns), '--policy', 'random-legal-full',
               '--until', 'turn2-reserve', '--root-seed', str(seed), '--output', str(run)]
    with (output / 'console.log').open('w') as log:
        try:
            exit_code = subprocess.run(command, stdout=log, stderr=subprocess.STDOUT,
                                       timeout=timeout).returncode
            process_failure = None
        except subprocess.TimeoutExpired:
            exit_code = None
            process_failure = 'hang-timeout'
        except OSError as exception:
            exit_code = None
            process_failure = f'launch-error:{type(exception).__name__}'
    summary_path = run / 'summary.json'
    try:
        summary = json.loads(summary_path.read_text()) if summary_path.exists() else None
    except (OSError, json.JSONDecodeError):
        summary = None
        process_failure = process_failure or 'invalid-summary'
    if summary is None and process_failure is None:
        process_failure = 'missing-summary'
    by_ordinal = {row['campaignOrdinal']: row for row in summary['perCampaign']} if summary else {}
    failures = []
    for ordinal in range(campaigns):
        row = by_ordinal.get(ordinal, {})
        status = row.get('status', process_failure or 'missing-summary')
        if status == 'turn2-reserve':
            continue
        decision = pathlib.Path(f'run/campaigns/{ordinal}.jsonl')
        attempt = pathlib.Path(f'run/attempts/{ordinal}.jsonl')
        failures.append(dict(campaignOrdinal=ordinal, status=status,
                             decisionPrefix=str(decision) if (output / decision).exists() else None,
                             attemptPrefix=str(attempt) if (output / attempt).exists() else None))
    record = dict(seed=seed, repeat=repeat, driver=driver, store=store, campaigns=campaigns,
                  command=command, exitCode=exit_code, processFailure=process_failure,
                  summary=summary, failures=failures)
    (output / 'result.json').write_text(json.dumps(record, indent=2) + '\n')
    return record


def compare(first, second, campaigns):
    def same(path):
        a, b = first / 'run' / path, second / 'run' / path
        return a.exists() and b.exists() and a.read_bytes() == b.read_bytes()
    return dict(decisionJsonlIdentical=all(same(f'campaigns/{i}.jsonl') for i in range(campaigns)),
                attemptJsonlIdentical=all(same(f'attempts/{i}.jsonl') for i in range(campaigns)))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=pathlib.Path, required=True)
    parser.add_argument('--driver', choices=['inproc', 'http'], default='inproc')
    parser.add_argument('--store', choices=['sqlite', 'postgres'], default='sqlite')
    parser.add_argument('--campaigns', type=int, default=1)
    parser.add_argument('--seeds', type=int, nargs='+', required=True)
    parser.add_argument('--parallel', type=int, default=1)
    parser.add_argument('--timeout', type=int, default=1200, help='per-run timeout in seconds')
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=False)

    def pair(seed):
        folder = args.output / f'seed-{seed}'
        first, second = (folder / name for name in ('a', 'b'))
        runs = [run_case(path, args.driver, args.store, args.campaigns, seed, repeat, args.timeout)
                for path, repeat in ((first, 'a'), (second, 'b'))]
        result = dict(seed=seed, runs=runs, **compare(first, second, args.campaigns))
        (folder / 'comparison.json').write_text(json.dumps(result, indent=2) + '\n')
        print(f"seed {seed}: {result['decisionJsonlIdentical']=}; "
              f"failures {[len(run['failures']) for run in runs]}", flush=True)
        return result

    with concurrent.futures.ThreadPoolExecutor(max_workers=args.parallel) as pool:
        results = list(pool.map(pair, args.seeds))
    (args.output / 'results.json').write_text(json.dumps(results, indent=2) + '\n')


if __name__ == '__main__':
    main()
