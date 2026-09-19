#!/usr/bin/env python3
"""Retained final B2 focused verification and fixture-generation driver.

Does not repeat the previously completed 134-source native execution group.
Run from any directory; stdout/stderr should be retained together in a log.
"""
import datetime
import hashlib
import importlib.util
import json
from pathlib import Path
import threading
import time
import traceback

ROOT = Path(__file__).resolve().parents[3]
STARTED = time.monotonic()
CPU_STARTED = time.process_time()
STOP = threading.Event()
PHASE = "import"


def report(event, **fields):
    print(json.dumps(dict(event=event, utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),
                         wallSeconds=round(time.monotonic()-STARTED, 3),
                         cpuSeconds=round(time.process_time()-CPU_STARTED, 3), **fields)), flush=True)


def heartbeat():
    while not STOP.wait(30):
        report("progress", phase=PHASE)


def main():
    global PHASE
    report("start", scope="final focused checks; prior full native execution retained separately")
    threading.Thread(target=heartbeat, daemon=True).start()
    try:
        path = ROOT / "docs/specs/verify-combat-exercise-child-evidence-v1.py"
        spec = importlib.util.spec_from_file_location("child_final", path)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        PHASE = "authenticate_catalog"
        report("begin", phase=PHASE)
        rows = module.b.source_cases()
        report("catalog", sources=len(rows))
        for test in (module.test_boundary, module.test_supported_admission,
                     module.test_representatives, module.test_failures_and_forgery,
                     module.test_identity_and_controller):
            PHASE = test.__name__
            report("begin", phase=PHASE)
            report("passed", phase=PHASE, result=test())
        PHASE = "fixture_readback_before_generation"
        try:
            module.test_fixture()
        except (FileNotFoundError, ValueError):
            report("expected_red", reason="retained fixture differs from current generated build")
        else:
            report("existing_fixture_current")
        PHASE = "fixture_generation_and_readback"
        module.FIXTURE.write_bytes(module.fixture_bytes(module.generated_fixture()))
        result = module.test_fixture()
        report("passed", phase=PHASE, result=result)
        paths = [ROOT / "docs/specs/combat-exercise-child-evidence-v1.md",
                 ROOT / "docs/specs/combat-exercise-child-evidence-v1.schema.json",
                 module.FIXTURE, path]
        hashes = {str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest() for p in paths}
        report("FINAL_FOCUSED_GREEN", hashes=hashes, goldens=len(module.RUNS))
    except BaseException:
        report("failed", phase=PHASE)
        traceback.print_exc()
        raise
    finally:
        STOP.set()


if __name__ == "__main__":
    main()
