#!/usr/bin/env python3
"""Focused R03 admission, forged event/readback, framing and retry negatives."""
import copy
import gzip
import importlib.util
import json
import sys
from pathlib import Path

script = Path(__file__).resolve().parents[2] / "specs/verify-combat-actual-moved-repeat-breakdown-completion-v1.py"
spec = importlib.util.spec_from_file_location("r03_negatives", script)
r03 = importlib.util.module_from_spec(spec)
spec.loader.exec_module(r03)
with gzip.open(sys.argv[1], "rt", encoding="ascii") as stream:
    row = json.loads(next(stream))
assert row["rowKey"] == "r000-c01"
v = row["vectors"]
parent_source, parent_args = r03.parent_terminal(r03.r02.semantic_rows()[0])
source = r03.source_for(parent_source)
input_bytes = v["inputCanonicalUtf8"].encode("ascii")
after_source = v["sourceCanonicalUtf8"].encode("ascii")
input_value = r03.parse(input_bytes, "Input")
_, context, before = r03.replay(source, parent_args, [])
assert r03.raw(before, "Control").decode("ascii") == v["beforeControlCanonicalUtf8"]
counts = {}

def reject(name, code, call):
    try:
        call()
    except r03.Invalid as error:
        assert error.code == f"CMB-MRB-{code:03}", (name, error)
    else:
        raise AssertionError((name, "accepted"))
    counts[name] = code

reject("own-source-before-ledger-framing", 1, lambda: r03.replay(b"{}", [], []))
reject("ledger-without-event", 4, lambda: r03.replay(source, parent_args, [input_bytes]))
reject("event-without-ledger", 4, lambda: r03.replay(after_source, parent_args, []))
for name, path, value, code in (
    ("foreign-actor", ("actor",), "axis", 4),
    ("wrong-action", ("command", "actionId"), "sha256:" + "0" * 64, 5),
    ("wrong-base", ("command", "baseHash"), "sha256:" + "0" * 64, 4),
    ("wrong-stage", ("command", "operationStage"), 2, 3),
    ("stale-version", ("command", "expectedPriorVersion"), 0, 6),
    ("wrong-capability", ("command", "capabilityId"), "sha256:" + "0" * 64, 5),
):
    altered = copy.deepcopy(input_value)
    target = altered
    for component in path[:-1]:
        target = target[component]
    target[path[-1]] = value
    reject(name, code, lambda altered=altered: r03.apply(
        source, parent_args, [], r03.raw(altered, "Input")))

extra = copy.deepcopy(input_value)
extra["deadline"] = 1
reject("unowned-clock-field", 1, lambda: r03.raw(extra, "Input"))
reject("noncanonical-suffix", 8, lambda: r03.parse(input_bytes + b" ", "Input"))

packet = r03.parse(after_source, "Source")
event = r03.parse(v["eventCanonicalUtf8"].encode("ascii"), "Event")
event["effect"]["kind"] = "forged"
del event["receiptId"]
event["receiptId"] = "mrbc." + r03.domain("receipt", r03.encode(event))
packet["breakdownEventCanonicalUtf8"] = [r03.raw(event, "Event").decode("ascii")]
reject("resigned-effect", 6, lambda: r03.replay(r03.raw(packet, "Source"), parent_args, [input_bytes]))

after = r03.parse(v["afterControlCanonicalUtf8"].encode("ascii"), "Control")
after["world"]["elements"][0]["currentLocationId"] = "forged"
reject("forged-world-readback", 6, lambda: r03.read_control(
    r03.raw(after, "Control"), after_source, parent_args, [input_bytes]))
proof = r03.parse(v["afterProofCanonicalUtf8"].encode("ascii"), "Proof")
proof["sourceHash"] = "sha256:" + "0" * 64
reject("forged-proof-readback", 6, lambda: r03.read_proof(
    r03.raw(proof, "Proof"), after_source, parent_args, [input_bytes]))

ceiling = copy.deepcopy(before)
ceiling["stateVersion"] = 2**63 - 1
reject("clock-ceiling", 7, lambda: r03.transition(
    context, ceiling, r03.command(context, ceiling)))
retry = r03.apply(after_source, parent_args, [input_bytes], input_bytes, False)
assert retry == (after_source, [input_bytes], None, row["receiptId"])
assert r03.raw(r03.replay(after_source, parent_args, [input_bytes])[2], "Control").decode(
    "ascii") == v["afterControlCanonicalUtf8"]
print(json.dumps(dict(status="PASS", rejected=counts, exactRetryPreservedBothReceiptLedgers=True)))
