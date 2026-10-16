#!/usr/bin/env python3
"""Provisional physical dependency fault gate for moved-repeat Breakdown."""

import importlib.util
import json
import sys
from pathlib import Path

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent
REPOSITORY = ROOT.parent.parent


def load(filename):
    spec = importlib.util.spec_from_file_location(filename.removesuffix(".py").replace("-", "_"), ROOT / filename)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


parent = load("verify-combat-actual-moved-repeat-movement-completion-v1.py")
child = load("verify-combat-actual-moved-repeat-breakdown-completion-v1.py")
row = parent.semantic_rows()[0]
packet, arguments = parent.root(row)
case = next(case for case in json.loads(parent.FIXTURE.read_bytes())["cases"] if case["rowKey"] == row["rowKey"])
packet["lifecycleEventCanonicalUtf8"] = case["eventCanonicalUtf8"]
arguments = [*arguments, [item.encode("ascii") for item in case["inputCanonicalUtf8"]]]
source = child.source_for(parent.source_bytes(packet))
child.replay(source, arguments, [])

paths = [(REPOSITORY / name).resolve() for name in child.DEPENDENCIES]
paths += [path.resolve() for path in child.OWN]
assert len(paths) == len(set(paths)) == 108
actual_read = Path.read_bytes
checks = 0

for target in paths:
    for fault in ("missing", "changed", "unreadable", "null", "drift"):
        hits = [0]

        def altered(path):
            if path.resolve() == target:
                hits[0] += 1
                if fault == "missing":
                    raise FileNotFoundError(str(path))
                if fault == "unreadable":
                    raise PermissionError(str(path))
                if fault == "null":
                    return None
                if fault == "changed" or hits[0] > 1:
                    return b"changed"
            return actual_read(path)

        try:
            Path.read_bytes = altered
            try:
                child.replay(source, arguments, [])
            except child.Invalid as error:
                assert error.number == 9, (target, fault, error)
            else:
                raise AssertionError((target, fault, "accepted"))
        finally:
            Path.read_bytes = actual_read
        checks += 1

print("PASS all physical dependencies reject missing/changed/unreadable/null/drift", len(paths), checks, flush=True)
