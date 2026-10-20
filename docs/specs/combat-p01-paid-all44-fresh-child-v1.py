#!/usr/bin/env python3
"""Read both retained private paid cuts in one independent Python interpreter."""
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
    if len(sys.argv) != 3:
        return dict(rejected="preflight", error="two retained cut paths required")
    try:
        prepared = parse_path(sys.argv[1])
        committed = parse_path(sys.argv[2])
        source, parents, r03, selected, assignment, own, retry, pc, pw, pr = prepared
        csource, cparents, cr03, cselected, cassignment, cown, cretry, cc, cw, cr = committed
        require((parents, r03, selected, assignment) ==
                (cparents, cr03, cselected, cassignment),
                "cuts have different original ancestry")
        require(own == [] and retry is None and len(cown) == 1 and cretry is not None,
                "prepared/committed ledger shape")
        require(ref.input_bytes(cretry) == cown[0], "accepted retry differs from paid input")
        before = ref.replay(source, parents, r03, selected, assignment, own)
        require(before["status"] == "prepared" and
                ref.control_bytes(before) == pc and ref.encode(before["world"]) == pw and
                before["lastReceiptId"] == pr, "prepared retained cut differs")
        after, event, receipt = ref.transition(before, ref.parse_input(cown[0]))
        packet = ref.parse_source(source)
        require(packet["eventCanonicalUtf8"] == [], "prepared event contamination")
        packet["eventCanonicalUtf8"].append(event.decode("ascii"))
        require(ref.encode(packet) == csource, "committed Source differs")
        require(ref.parse_source(csource)["eventCanonicalUtf8"] ==
                [event.decode("ascii")], "committed event differs")
        require(after["status"] == "committed" and
                ref.control_bytes(after) == cc and ref.encode(after["world"]) == cw and
                receipt == cr == after["lastReceiptId"],
                "committed retained cut differs")
        # The real apply path replays the committed cut again from its original
        # ancestors, then deduplicates the accepted input without another debit.
        retry_source, retry_ledger, retry_event, retry_receipt = ref.apply(
            csource, parents, r03, selected, assignment, cown,
            ref.input_bytes(cretry))
        require((retry_source, retry_ledger, retry_event, retry_receipt) ==
                (csource, cown, None, receipt), "accepted retry mutated committed cut")
        return dict(prepared=view(before, source, own),
                    committed=view(after, csource, cown),
                    retry=dict(receiptId=retry_receipt, event=None,
                               sourceHash=digest(retry_source),
                               ledgerHash=ledger_hash(retry_ledger)))
    except (ref.Invalid, ValueError, TypeError, KeyError, OverflowError,
            binascii.Error) as error:
        return dict(rejected="readback", error=str(error))


if __name__ == "__main__":
    print(json.dumps(result(), ensure_ascii=True, separators=(",", ":")))
