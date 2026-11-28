"""Run the single excluded replay soak with bounded, private progress diagnostics."""
import os
from pathlib import Path
import sqlite3
import subprocess
import sys
import tempfile
import time

from orleans_partition import PROJECT, check_report

SOAK_METHOD = 'Cna.OrleansHost.Tests.GrazianiPregameJournalTests.FinalizedRealSupplyPlanSurvivesFreshHostReplayWithoutStartingTheGame'


def progress(root):
    journals = list(Path(root).glob('graziani-source-authority-*/campaign.sqlite'))
    if len(journals) != 1:
        return 'waiting for journal' if not journals else 'multiple journals; no progress sampled'
    db = journals[0]
    try:
        # Read only metadata, never log or retain checkpoint contents. A busy
        # journal must not block the test or be mistaken for a test failure.
        with sqlite3.connect(db.as_uri() + '?mode=ro', uri=True, timeout=0.1) as con:
            count = con.execute('SELECT COUNT(*) FROM accepted_steps').fetchone()[0]
        return f'accepted_steps={count} db_bytes={db.stat().st_size}'
    except (sqlite3.Error, OSError):
        return 'journal temporarily unavailable'


def verify_soak(directory):
    check_report(directory, {SOAK_METHOD: 1})


def main(directory):
    results = Path(directory).resolve()
    results.mkdir(parents=True, exist_ok=True)
    # Keep the test's temporary journal isolated from other rows and outside
    # uploaded artifacts. GNU timeout bounds the whole test process tree.
    with tempfile.TemporaryDirectory(prefix='orleans-soak-') as tmp:
        command = ['timeout', '--kill-after=30s', '90m', 'dotnet', 'test', '--project', PROJECT,
                   '--configuration', 'Release', '--no-build', '--no-restore', '--report-xunit-xml',
                   '--results-directory', str(results), '--filter-display-name', SOAK_METHOD]
        start = time.monotonic()
        with (results / 'console.log').open('w') as log, (results / 'progress.log').open('w') as samples:
            with subprocess.Popen(command, stdout=log, stderr=subprocess.STDOUT,
                                  env={**os.environ, 'TMPDIR': tmp}) as process:
                while True:
                    try:
                        status = process.wait(timeout=60)
                        break
                    except subprocess.TimeoutExpired:
                        line = f'elapsed_seconds={int(time.monotonic() - start)} {progress(Path(tmp))}'
                        print(line, flush=True)
                        print(line, file=samples, flush=True)
            line = f'elapsed_seconds={int(time.monotonic() - start)} exit={status}'
            print(line, flush=True)
            print(line, file=samples, flush=True)
        print((results / 'console.log').read_text(), end='', flush=True)
        if status != 0:
            return status
        verify_soak(results)
        return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1]))
