#!/usr/bin/env python3
"""Provisional exhaustive per-group semantic gate for authentic R02 Breakdown."""

import argparse
import importlib.util
import json
import sys
import time
from pathlib import Path

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parent


def load(filename):
    spec = importlib.util.spec_from_file_location(filename.removesuffix(".py").replace("-", "_"), ROOT / filename)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


parent = load("verify-combat-actual-moved-repeat-movement-completion-v1.py")
child = load("verify-combat-actual-moved-repeat-breakdown-completion-v1.py")
fixture = json.loads(parent.FIXTURE.read_bytes())
semantic = parent.semantic_rows()
assert fixture["semanticRows"] == semantic and len(semantic) == len(fixture["cases"]) == 300
cases = {case["rowKey"]: case for case in fixture["cases"]}
assert len(cases) == 300


def verify(group):
    selected = [row for row in semantic if row["group"] == group]
    assert selected
    start = time.monotonic()
    event_hashes = []
    for row in selected:
        packet, arguments = parent.root(row)
        retained = cases[row["rowKey"]]
        assert retained["profile"] == row["profile"] and retained["movementCut"] == row["movementCut"]
        packet["lifecycleEventCanonicalUtf8"] = retained["eventCanonicalUtf8"]
        predecessor_source = parent.source_bytes(packet)
        arguments = [*arguments, [item.encode("ascii") for item in retained["inputCanonicalUtf8"]]]
        source = child.source_for(predecessor_source)
        context, before, _ = child.replay(source, arguments, [])
        command = child.command(context, before)
        completed, own_ledger, receipt = child.apply(source, arguments, [], command)
        _, after, _ = child.replay(completed, arguments, own_ledger)
        completed_packet = child.parse(completed, "Source")
        assert completed_packet["movementCompletionSourceCanonicalUtf8"] == predecessor_source.decode("ascii")
        assert len(completed_packet["breakdownEventCanonicalUtf8"]) == len(own_ledger) == 1
        event = child.parse(completed_packet["breakdownEventCanonicalUtf8"][0].encode("ascii"), "Event")
        assert event["receiptId"] == receipt and event["priorVersion"] == before["stateVersion"]
        assert event["effect"]["movementCompletionReceiptId"] == before["movementCompletionReceiptId"]
        assert event["effect"]["sources"] == before["sequencePosition"]["sources"]
        assert event["effect"]["successorPosition"] == context["successor"]
        assert after["predecessorControlCanonicalUtf8"] == before["predecessorControlCanonicalUtf8"]
        assert after["movementCompletionReceiptId"] == before["movementCompletionReceiptId"]
        assert after["activeCycle"] == before["activeCycle"] and after["cycleId"] == before["cycleId"]
        assert after["stateVersion"] == before["stateVersion"] + 1
        assert after["sequencePosition"] == context["successor"] and after["phase"] == "combat"
        assert after["breakdownCompletionReceiptId"] == receipt and after["closed"]
        assert child.apply(completed, arguments, own_ledger, command, admission=False) == (completed, own_ledger, receipt)
        event_hashes.append(child.sha(completed_packet["breakdownEventCanonicalUtf8"][0].encode("ascii")))
    print(json.dumps(dict(group=group, rows=len(selected), events=len(event_hashes), eventHashes=event_hashes, seconds=round(time.monotonic() - start, 3))), flush=True)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--group", choices=sorted({row["group"] for row in semantic}), required=True)
    args = parser.parse_args()
    verify(args.group)


if __name__ == "__main__":
    main()
