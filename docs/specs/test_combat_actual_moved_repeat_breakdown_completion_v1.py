#!/usr/bin/env python3
"""Focused RED/GREEN check for an authentic moved-repeat Breakdown successor."""

import importlib.util
import copy
import json
import sys
from pathlib import Path

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent


def load(name):
    path = ROOT / name
    spec = importlib.util.spec_from_file_location(name.removesuffix(".py").replace("-", "_"), path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


parent = load("verify-combat-actual-moved-repeat-movement-completion-v1.py")
fixture = json.loads(parent.FIXTURE.read_bytes())
cases = {case["rowKey"]: case for case in fixture["cases"]}
targets = (("paid", 0), ("paid", 2), ("armed", 0), ("armed", 5))
authentic = []

for profile, moves in targets:
    row = next(
        row for row in parent.semantic_rows()
        if row["profile"] == profile and row["movementCut"] == moves + 1
    )
    packet, arguments = parent.root(row)
    retained = cases[row["rowKey"]]
    packet["lifecycleEventCanonicalUtf8"] = retained["eventCanonicalUtf8"]
    source = parent.source_bytes(packet)
    predecessor_arguments = [
        *arguments,
        [input_text.encode("ascii") for input_text in retained["inputCanonicalUtf8"]],
    ]
    _, state = parent.replay(source, *predecessor_arguments)
    assert state["phase"] == "breakdown" and state["closed"] is True
    assert state["activeCycle"]["ordinal"] == 2
    assert state["movementEnd"]["ordinal"] == 2
    assert state["originalMovementEnd"]["ordinal"] == 1
    authentic.append((row, source, predecessor_arguments, state))

# The predecessor has passed genuine full replay before the new transition is exercised.
child = load("verify-combat-actual-moved-repeat-breakdown-completion-v1.py")

for row, predecessor_source, arguments, prior in authentic:
    initial_source = child.source_for(predecessor_source)
    context, initial, _ = child.replay(initial_source, arguments, [])
    assert initial["stateVersion"] == prior["stateVersion"]
    assert initial["sequencePosition"] == prior["sequencePosition"]
    command = child.command(context, initial)
    source, ledger, receipt = child.apply(initial_source, arguments, [], command)
    _, after, _ = child.replay(source, arguments, ledger)
    assert after["stateVersion"] == prior["stateVersion"] + 1
    assert after["sequencePosition"]["positionId"] == context["successor"]["positionId"]
    assert after["phase"] == "combat" and after["closed"] is True
    assert after["breakdownCompletionReceiptId"] == receipt
    assert after["predecessorControlCanonicalUtf8"] == parent.raw(prior, "Control").decode("ascii")
    retry = child.apply(source, arguments, ledger, command, admission=False)
    assert retry == (source, ledger, receipt)
    print("PASS authentic moved-repeat Breakdown", row["rowKey"], receipt, flush=True)


def rejects(number, action):
    try:
        action()
    except child.Invalid as error:
        assert error.number == number, (number, error)
        return
    raise AssertionError(f"expected CMB-MRB-{number:03}")


row, predecessor_source, arguments, prior = authentic[0]
start = child.source_for(predecessor_source)
context, initial, _ = child.replay(start, arguments, [])
good = child.command(context, initial)
completed, accepted_ledger, receipt = child.apply(start, arguments, [], good)

for field, value, code in (
    ("baseHash", "sha256:" + "0" * 64, 4),
    ("cycleId", "sha256:" + "0" * 64, 4),
    ("expectedPositionId", "foreign.position", 4),
    ("expectedPriorVersion", initial["stateVersion"] - 1, 6),
    ("actionId", "sha256:" + "0" * 64, 5),
    ("capabilityId", "sha256:" + "0" * 64, 5),
):
    changed = copy.deepcopy(good)
    changed["command"][field] = value
    rejects(code, lambda changed=changed: child.apply(start, arguments, [], changed))

wrong_actor = copy.deepcopy(good)
wrong_actor["actor"] = prior["activeCycle"]["actingSide"]
rejects(4, lambda: child.apply(start, arguments, [], wrong_actor))
rejects(7, lambda: child.apply(start, arguments, [], good, admission=False))
rejects(7, lambda: child.apply(start, arguments, [], good, handlers=[]))
lower_capacity = dict(child.LIMITS)
lower_capacity["acceptedEvents"] = 0
rejects(7, lambda: child.apply(start, arguments, [], good, capacities=lower_capacity))

missing_parent = child.parse(start, "Source")
missing_parent["movementCompletionSourceCanonicalUtf8"] = "{}"
rejects(1, lambda: child.replay(child.raw(missing_parent, "Source"), arguments, []))
rejects(1, lambda: child.replay(start, [*arguments[:-1], None], []))
rejects(4, lambda: child.replay(completed, arguments, []))

nonterminal_packet, nonterminal_arguments = parent.root(row)
nonterminal_source = child.source_for(parent.source_bytes(nonterminal_packet))
rejects(3, lambda: child.replay(nonterminal_source, [*nonterminal_arguments, []], []))

resigned = child.parse(completed, "Source")
event = json.loads(resigned["breakdownEventCanonicalUtf8"][0])
event["effect"]["movementCompletionReceiptId"] = "forged"
event["receiptId"] = "mrb." + child.identity("receipt", child.encode({key: value for key, value in event.items() if key != "receiptId"}))
resigned["breakdownEventCanonicalUtf8"][0] = child.raw(event, "Event").decode("ascii")
rejects(6, lambda: child.replay(child.raw(resigned, "Source"), arguments, accepted_ledger))

candidate_context, candidate_state, _ = child.replay(completed, arguments, accepted_ledger)
readback = child._proof_candidate(completed, candidate_context, candidate_state)
assert readback["control"]["predecessorControlCanonicalUtf8"] == parent.raw(prior, "Control").decode("ascii")
rejects(7, lambda: child.proof(completed, arguments, accepted_ledger))
rejects(7, lambda: child.read_proof(child.raw(readback, "Proof"), completed, arguments, accepted_ledger))
rejects(7, lambda: child.read_source(completed, arguments, accepted_ledger))
assert child.apply(completed, arguments, accepted_ledger, good, admission=False) == (completed, accepted_ledger, receipt)
mutable_arguments = copy.deepcopy(arguments)
mutable_action = copy.deepcopy(good)
actual_read = Path.read_bytes
touched = [False]

def mutate_during_first_acquisition(path):
    if not touched[0]:
        touched[0] = True
        mutable_arguments[-1].clear()
        mutable_action["actor"] = prior["activeCycle"]["actingSide"]
    return actual_read(path)

try:
    Path.read_bytes = mutate_during_first_acquisition
    owned_result = child.apply(start, mutable_arguments, [], mutable_action)
finally:
    Path.read_bytes = actual_read
assert touched[0] and owned_result == (completed, accepted_ledger, receipt)
print("PASS context, actor, capacity, ancestor, own ledger and re-signed effect negatives", flush=True)
pin = (ROOT.parent.parent / next(iter(child.DEPENDENCIES))).resolve()

def missing_pin(path):
    if path.resolve() == pin:
        raise FileNotFoundError(str(path))
    return actual_read(path)

try:
    Path.read_bytes = missing_pin
    rejects(9, lambda: child.replay(completed, arguments, accepted_ledger))
finally:
    Path.read_bytes = actual_read
print("PASS owned-before-callback and warm physical-pin negative", flush=True)

if len(sys.argv) > 1:
    assert len(sys.argv) == 3 and sys.argv[1] == "--group"
    group = sys.argv[2]
    selected = [row for row in parent.semantic_rows() if row["group"] == group]
    assert selected and group in {row["group"] for row in fixture["semanticRows"]}
    for row in selected:
        packet, arguments = parent.root(row)
        retained = cases[row["rowKey"]]
        packet["lifecycleEventCanonicalUtf8"] = retained["eventCanonicalUtf8"]
        predecessor_source = parent.source_bytes(packet)
        arguments = [*arguments, [item.encode("ascii") for item in retained["inputCanonicalUtf8"]]]
        source = child.source_for(predecessor_source)
        context, before, _ = child.replay(source, arguments, [])
        command = child.command(context, before)
        completed, own_ledger, receipt = child.apply(source, arguments, [], command)
        _, after, _ = child.replay(completed, arguments, own_ledger)
        original = parent.raw(parent.replay(predecessor_source, *arguments)[1], "Control").decode("ascii")
        assert after["predecessorControlCanonicalUtf8"] == original
        assert after["movementCompletionReceiptId"] == before["movementCompletionReceiptId"]
        assert after["stateVersion"] == before["stateVersion"] + 1
        assert after["sequencePosition"] == context["successor"]
        assert after["breakdownCompletionReceiptId"] == receipt and len(after["receipts"]) == 1
        print("VERIFIED", row["rowKey"], flush=True)
    print("PASS exhaustive authentic group", group, len(selected), flush=True)
