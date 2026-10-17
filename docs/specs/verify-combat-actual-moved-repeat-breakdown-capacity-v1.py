#!/usr/bin/env python3
"""Provisional structural and observed capacity checks for moved-repeat Breakdown."""

import copy
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
fixture = json.loads(parent.FIXTURE.read_bytes())
cases = {case["rowKey"]: case for case in fixture["cases"]}
P = parent.I["encodedUpperBounds"]
L = child.LIMITS
model = dict(
    Source=2 * P["Source"] + 2 * L["eventBytes"] + 512,
    Control=2 * P["Control"] + 131072,
    Proof=2 * P["Control"] + 2 * 131072,
    Event=16384,
    Input=2048,
    BreakdownLedger=2050,
)
caps = {name: L[key] for name, key in (("Source", "sourceBytes"), ("Control", "controlBytes"), ("Proof", "proofBytes"), ("Event", "eventBytes"), ("Input", "inputBytes"), ("BreakdownLedger", "breakdownLedgerBytes"))}
assert all(model[name] <= caps[name] for name in model), (model, caps)
assert model["Source"] == child.INVENTORY["encodedUpperBounds"]["Source"]
assert L["depth"] == parent.L["depth"] + 2


def inflated(value, skip_carriers=False):
    if type(value) is dict:
        result = {}
        for key, member in value.items():
            if skip_carriers and key == "predecessorControlCanonicalUtf8":
                result[key] = ""
            elif key == "sources" and type(member) is list and member:
                padded = member + [copy.deepcopy(member[-1]) for _ in range(3 - len(member))]
                result[key] = inflated(padded, skip_carriers)
            else:
                result[key] = inflated(member, skip_carriers)
        return result
    if type(value) is list:
        return [inflated(member, skip_carriers) for member in value]
    if type(value) is str:
        return "x" * 128
    if type(value) is bool:
        return False
    if type(value) is int:
        return -(2**63)
    return value


measured = {name: 0 for name in model}
fields = {"ControlMetadata": 0, "ProofMetadata": 0, "EventMetadata": 0, "InputStructural": 0}
for profile, moves in (("paid", 0), ("paid", 2), ("armed", 0), ("armed", 5)):
    row = next(row for row in parent.semantic_rows() if row["profile"] == profile and row["movementCut"] == moves + 1)
    packet, arguments = parent.root(row)
    case = cases[row["rowKey"]]
    packet["lifecycleEventCanonicalUtf8"] = case["eventCanonicalUtf8"]
    predecessor = parent.source_bytes(packet)
    arguments = [*arguments, [item.encode("ascii") for item in case["inputCanonicalUtf8"]]]
    source = child.source_for(predecessor)
    context, before, _ = child.replay(source, arguments, [])
    action = child.command(context, before)
    completed, ledger, _ = child.apply(source, arguments, [], action)
    _, after, _ = child.replay(completed, arguments, ledger)
    event_bytes = child.parse(completed, "Source")["breakdownEventCanonicalUtf8"][0].encode("ascii")
    proof_bytes = child.raw(child._proof_candidate(completed, context, after), "Proof")
    for name, size in (("Source", len(completed)), ("Control", len(child.raw(after, "Control"))), ("Proof", len(proof_bytes)), ("Event", len(event_bytes)), ("Input", len(child.raw(action, "Input"))), ("BreakdownLedger", len(child.encode([json.loads(ledger[0])])))):
        measured[name] = max(measured[name], size)
    fields["ControlMetadata"] = max(fields["ControlMetadata"], len(child.encode(inflated(after, True))))
    fields["ProofMetadata"] = max(fields["ProofMetadata"], len(child.encode(inflated(child._proof_candidate(completed, context, after), True))))
    fields["EventMetadata"] = max(fields["EventMetadata"], len(child.encode(inflated(json.loads(event_bytes)))))
    fields["InputStructural"] = max(fields["InputStructural"], len(child.encode(inflated(action))))

assert all(measured[name] <= model[name] for name in model), (measured, model)
assert fields["ControlMetadata"] <= 131072 and fields["ProofMetadata"] <= 262144
assert fields["EventMetadata"] <= model["Event"] and fields["InputStructural"] <= model["Input"]

# A full runtime replay must acquire exactly the declared physical dependency set.
expected = {(REPOSITORY / name).resolve() for name in child.DEPENDENCIES} | {path.resolve() for path in child.OWN}
assert len(expected) == 108
observed = set()
actual_read = Path.read_bytes


def observe(path):
    observed.add(path.resolve())
    return actual_read(path)


try:
    Path.read_bytes = observe
    child.replay(source, arguments, [])
finally:
    Path.read_bytes = actual_read
assert observed == expected, (observed - expected, expected - observed)
print(json.dumps(dict(model=model, caps=caps, observedMaxima=measured, inflatedFields=fields, physicalPaths=len(observed)), sort_keys=True), flush=True)
print("PASS provisional valid-state domination, observed samples and runtime physical closure", flush=True)
