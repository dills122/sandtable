#!/usr/bin/env python3
"""One-shot, bounded private paid-commit readback in a fresh process."""
import base64
import binascii
import importlib.util
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location(
    "paid_commit_fresh_reference", ROOT / "verify-combat-p01-paid-commit-v1.py")
reference = importlib.util.module_from_spec(spec)
spec.loader.exec_module(reference)
LIMITS = reference.SCHEMA["limits"]


def raw(value):
    if type(value) is not str:
        raise ValueError("non-string base64 field")
    return base64.b64decode(value, validate=True)


def encoded(value):
    return base64.b64encode(value).decode("ascii")


def bounded(raw_value, maximum, label):
    if not 0 < len(raw_value) <= maximum:
        raise ValueError(label + " byte cap")
    return raw_value


def read_payload():
    # A retained cut envelope is read from disk in this independent process.
    if len(sys.argv) != 2:
        raise ValueError("one retained cut path required")
    path = Path(sys.argv[1])
    if path.stat().st_size > 16 * 1024 * 1024:
        raise ValueError("request byte cap")
    incoming = path.read_bytes()
    payload = json.loads(incoming)
    if type(payload) is not dict or set(payload) != {
            "source", "creationRequest", "parentInputLedgers", "r03Ledger",
            "selectedLedger", "assignmentLedger", "ownLedger", "retryInput",
            "expectedControl", "expectedWorld", "expectedReceiptId"}:
        raise ValueError("readback envelope")
    source = bounded(raw(payload["source"]), LIMITS["sourceBytes"], "source")
    request_raw = raw(payload["creationRequest"])
    request = json.loads(request_raw)
    if reference.encode(request) != request_raw:
        raise ValueError("noncanonical CreationRequest")
    ledgers = payload["parentInputLedgers"]
    # Ten ledgers precede R03; its ledger is the eleventh parent ledger.
    if type(ledgers) is not list or len(ledgers) != 10:
        raise ValueError("ten pre-R03 parent ledgers required")
    parents = [request, *[[raw(item) for item in ledger] for ledger in ledgers]]
    r03 = [raw(item) for item in payload["r03Ledger"]]
    selected = [raw(item) for item in payload["selectedLedger"]]
    assignment = [raw(item) for item in payload["assignmentLedger"]]
    own = [raw(item) for item in payload["ownLedger"]]
    if len(own) > LIMITS["ledgerEvents"]:
        raise ValueError("paid ledger event cap")
    for item in own:
        reference.parse_input(bounded(item, LIMITS["inputBytes"], "input"))
    packet = reference.parse_source(source)
    for item in packet["eventCanonicalUtf8"]:
        reference.parse(bounded(item.encode("ascii"), LIMITS["eventBytes"],
                                "event"), reference.SCHEMA["eventFields"],
                        LIMITS["eventBytes"])
    control = bounded(raw(payload["expectedControl"]), LIMITS["controlBytes"],
                      "Control")
    world = bounded(raw(payload["expectedWorld"]),
                    LIMITS["projectedWorldBytes"], "World")
    receipt = payload["expectedReceiptId"]
    if not reference.ident(receipt, "p01y.") and not reference.ident(receipt, "p01p."):
        raise ValueError("receipt grammar")
    retry = None if payload["retryInput"] is None else reference.parse_input(
        bounded(raw(payload["retryInput"]), LIMITS["inputBytes"], "retry input"))
    return source, parents, r03, selected, assignment, own, retry, control, world, receipt


def result():
    try:
        (source, parents, r03, selected, assignment, own, retry,
         expected_control, expected_world, expected_receipt) = read_payload()
    except (ValueError, TypeError, KeyError, OverflowError, binascii.Error) as error:
        return dict(rejected="preflight", error=str(error))
    try:
        state = reference.replay(source, parents, r03, selected, assignment, own)
        control = reference.control_bytes(state)
        world = reference.encode(state["world"])
        if (control != expected_control or world != expected_world or
                state["lastReceiptId"] != expected_receipt):
            raise ValueError("stored cut differs from recomputed state")
    except (reference.Invalid, ValueError, TypeError, KeyError) as error:
        return dict(rejected="replay", error=str(error))
    output = dict(control=encoded(control), world=encoded(world),
                  receiptId=state["lastReceiptId"], status=state["status"],
                  stateVersion=state["stateVersion"], prefix=state["prefix"],
                  resources=[dict(cp=x["operationalState"]
                                  ["capabilityPointsExpended"],
                                  ammunition=x["ammunition"]["points"])
                             for x in state["world"]["elements"]],
                  attackHistory=state["attackHistory"],
                  targetUses=state["targetUses"],
                  admitted=state["admitted"], publicActions=state["publicActions"])
    if retry is not None:
        try:
            next_source, next_ledger, event, receipt = reference.apply(
                source, parents, r03, selected, assignment, own,
                reference.input_bytes(retry))
            if (next_source != source or next_ledger != own or event is not None
                    or receipt != state["lastReceiptId"]):
                raise ValueError("retry changed committed cut")
            output["retry"] = dict(receiptId=receipt, event=None,
                                   source=encoded(next_source),
                                   ledger=[encoded(item) for item in next_ledger])
        except (reference.Invalid, ValueError, TypeError, KeyError) as error:
            return dict(rejected="retry", error=str(error))
    return output


if __name__ == "__main__":
    print(json.dumps(result(), ensure_ascii=True, separators=(",", ":")))
