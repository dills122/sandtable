#!/usr/bin/env python3
"""Cold-read one retained private paid cut in an independent interpreter."""
import base64
import binascii
import hashlib
import importlib.util
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location(
    "paid_fresh_cut_reader", ROOT / "combat-p01-paid-commit-fresh-readback.py")
readback = importlib.util.module_from_spec(spec)
spec.loader.exec_module(readback)
ref = readback.reference


def digest(value):
    return "sha256:" + hashlib.sha256(value).hexdigest()


def ledger_hash(ledger):
    return digest(ref.encode([base64.b64encode(item).decode("ascii")
                              for item in ledger]))


def require(condition, label):
    if not condition:
        raise ValueError(label)


def parse_path(path):
    argv = sys.argv
    try:
        sys.argv = [argv[0], str(path)]
        return readback.read_payload()
    finally:
        sys.argv = argv


def view(state, source, ledger):
    return dict(control=readback.encoded(ref.control_bytes(state)),
                world=readback.encoded(ref.encode(state["world"])),
                receiptId=state["lastReceiptId"], status=state["status"],
                stateVersion=state["stateVersion"], prefix=state["prefix"],
                sourceHash=digest(source), ledgerHash=ledger_hash(ledger),
                resources=[dict(cp=x["operationalState"]
                                ["capabilityPointsExpended"],
                                ammunition=x["ammunition"]["points"])
                           for x in state["world"]["elements"]],
                attackHistory=state["attackHistory"],
                targetUses=state["targetUses"],
                admitted=state["admitted"], publicActions=state["publicActions"])


def result():
    if len(sys.argv) != 3 or sys.argv[1] not in ("prepared", "committed"):
        return dict(rejected="preflight", error="one cut mode and path required")
    try:
        mode = sys.argv[1]
        source, parents, r03, selected, assignment, own, retry, control, world, receipt = (
            parse_path(sys.argv[2]))
        packet = ref.parse_source(source)
        if mode == "prepared":
            require(own == [] and retry is None and
                    packet["eventCanonicalUtf8"] == [],
                    "prepared ledger/source shape")
        else:
            require(len(own) == 1 and retry is not None and
                    ref.input_bytes(retry) == own[0] and
                    len(packet["eventCanonicalUtf8"]) == 1,
                    "committed ledger/source/retry shape")
        state = ref.replay(source, parents, r03, selected, assignment, own)
        require(state["status"] == mode and
                ref.control_bytes(state) == control and
                ref.encode(state["world"]) == world and
                state["lastReceiptId"] == receipt,
                mode + " retained cut differs")
        output = dict(cut=view(state, source, own))
        if mode == "committed":
            # This process starts without any Prepared interpreter state and
            # replays its own complete ancestry before accepted retry.
            retry_source, retry_ledger, retry_event, retry_receipt = ref.apply(
                source, parents, r03, selected, assignment, own,
                ref.input_bytes(retry))
            require((retry_source, retry_ledger, retry_event, retry_receipt) ==
                    (source, own, None, receipt),
                    "accepted retry mutated committed cut")
            output["retry"] = dict(receiptId=retry_receipt, event=None,
                                   sourceHash=digest(retry_source),
                                   ledgerHash=ledger_hash(retry_ledger))
        return output
    except (ref.Invalid, ValueError, TypeError, KeyError, OverflowError,
            binascii.Error) as error:
        return dict(rejected="readback", error=str(error))


if __name__ == "__main__":
    print(json.dumps(result(), ensure_ascii=True, separators=(",", ":")))
