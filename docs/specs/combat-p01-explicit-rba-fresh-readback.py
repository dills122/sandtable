#!/usr/bin/env python3
"""One-shot, fresh-process private P01 cut readback and continuation test helper."""
import base64
import importlib.util
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location(
    "p01_fresh_readback_reference", ROOT / "verify-combat-p01-explicit-rba-v1.py")
reference = importlib.util.module_from_spec(spec)
spec.loader.exec_module(reference)


def raw(value):
    return base64.b64decode(value, validate=True)


def b64(value):
    return base64.b64encode(value).decode("ascii")


def result():
    payload = json.load(sys.stdin)
    source = raw(payload["source"])
    request_raw = raw(payload["creationRequest"])
    request = json.loads(request_raw)
    if reference.encode(request) != request_raw:
        raise ValueError("noncanonical CreationRequest in fresh readback")
    parents = [request,
               *[[raw(item) for item in part] for part in payload["parentInputLedgers"]]]
    r03 = [raw(item) for item in payload["r03Ledger"]]
    ledger = [raw(item) for item in payload["ownLedger"]]
    try:
        state = reference.replay(source, parents, r03, ledger)
    except (reference.Invalid, ValueError) as error:
        return dict(rejected="replay", error=str(error))
    output = dict(control=b64(reference.control_bytes(state)),
                  version=state["stateVersion"], prefix=state["prefix"],
                  position=state["position"]["positionId"],
                  lastReceiptId=state["lastReceiptId"],
                  world=b64(reference.encode(state["parent"]["world"])),
                  randomState=b64(reference.encode(state["parent"]["randomState"])),
                  admitted=state["admitted"], publicActions=state["publicActions"])
    if payload["retryInput"] is not None:
        try:
            retry_source, retry_ledger, retry_event, retry_receipt = reference.apply(
                source, parents, r03, ledger, raw(payload["retryInput"]))
        except (reference.Invalid, ValueError) as error:
            return dict(rejected="retry", error=str(error))
        output["retry"] = dict(source=b64(retry_source),
                               ledger=[b64(item) for item in retry_ledger],
                               event=None if retry_event is None else b64(retry_event),
                               receipt=retry_receipt)
    if payload["nextInput"] is not None:
        try:
            next_source, next_ledger, next_event, next_receipt = reference.apply(
                source, parents, r03, ledger, raw(payload["nextInput"]))
        except (reference.Invalid, ValueError) as error:
            return dict(rejected="next", error=str(error))
        next_state = reference.replay(next_source, parents, r03, next_ledger)
        output["next"] = dict(source=b64(next_source),
                              ledger=[b64(item) for item in next_ledger],
                              event=None if next_event is None else b64(next_event),
                              receipt=next_receipt,
                              control=b64(reference.control_bytes(next_state)))
    return output


if __name__ == "__main__":
    print(json.dumps(result(), ensure_ascii=True, separators=(",", ":")))
