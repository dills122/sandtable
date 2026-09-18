#!/usr/bin/env python3
"""B3 focused verification driver; -i --warm-only retains predecessor cache for C."""
import datetime
import hashlib
import importlib.util
import json
from pathlib import Path
import sys
import threading
import time

ROOT = Path(__file__).resolve().parents[3]
START = time.monotonic()
CPU = time.process_time()
PHASE = "import-child"
STOP = threading.Event()

def report(event, **fields):
    print(json.dumps(dict(event=event, utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),
                         wallSeconds=round(time.monotonic()-START, 3),
                         cpuSeconds=round(time.process_time()-CPU, 3), **fields)), flush=True)

def heartbeat():
    while not STOP.wait(30):report("progress", phase=PHASE)

def load(name, filename):
    spec=importlib.util.spec_from_file_location(name, ROOT / "docs/specs" / filename)
    module=importlib.util.module_from_spec(spec);sys.modules[name]=module;spec.loader.exec_module(module)
    return module

def verify(generate=False):
    global PHASE, parent
    parent=load("combat_parent", "verify-combat-exercise-parent-evidence-v1.py")
    parent.pins()
    for test in parent.TESTS:
        PHASE=test.__name__;report("begin", phase=PHASE)
        report("passed", phase=PHASE, result=test())
    if generate:
        PHASE="fixture-generation"
        try:parent.test_fixture()
        except (FileNotFoundError,ValueError):report("EXPECTED_RED", phase="missing-or-stale-fixture")
        parent.FIXTURE.write_bytes(parent.fixture_bytes(parent.generated_fixture()))
    report("passed", phase="fixture-readback", result=parent.test_fixture())
    files=["combat-exercise-parent-evidence-v1.md","combat-exercise-parent-evidence-v1.schema.json",
           "fixtures/combat-exercise-parent-evidence-v1.json","verify-combat-exercise-parent-evidence-v1.py"]
    report("B3_FOCUSED_GREEN", hashes={f:hashlib.sha256((ROOT / "docs/specs" / f).read_bytes()).hexdigest() for f in files})
    PHASE="warm-idle";STOP.set()

report("start")
threading.Thread(target=heartbeat,daemon=True).start()
child=load("combat_parent_child", "verify-combat-exercise-child-evidence-v1.py")
PHASE="authenticate-catalog"
rows=child.b.source_cases()
report("catalog-ready", sources=len(rows))
if "--warm-only" not in sys.argv:verify(generate="--generate" in sys.argv)
else:PHASE="warm-ready";report("warm-ready")
