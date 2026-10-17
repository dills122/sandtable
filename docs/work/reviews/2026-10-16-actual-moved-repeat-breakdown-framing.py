#!/usr/bin/env python3
"""Challenge R03 raw framing, primitive types, versions and bounded carriers."""
import copy
import gzip
import importlib.util
import json
import sys
from pathlib import Path

script = Path(__file__).resolve().parents[2] / "specs/verify-combat-actual-moved-repeat-breakdown-completion-v1.py"
spec = importlib.util.spec_from_file_location("r03_framing", script)
r03 = importlib.util.module_from_spec(spec)
spec.loader.exec_module(r03)
with gzip.open(sys.argv[1], "rt", encoding="ascii") as stream:
    vectors = json.loads(next(stream))["vectors"]
input_bytes = vectors["inputCanonicalUtf8"].encode("ascii")
input_value = r03.parse(input_bytes, "Input")
rejected = {}

def reject(name, code, call):
    try:
        call()
    except r03.Invalid as error:
        assert error.code == f"CMB-MRB-{code:03}", (name, error)
    else:
        raise AssertionError((name, "accepted"))
    rejected[name] = code

for name, data in (
    ("duplicate-key", b'{"contractVersion":1,"contractVersion":1}'),
    ("depth-43", b"[" * 43 + b"0" + b"]" * 43),
    ("array-513", b"[" + b"0," * 512 + b"0]"),
    ("object-129", b"{" + b",".join(b'"x' + str(i).encode() + b'":0'
                                           for i in range(129)) + b"}"),
    ("non-ascii", b"\xff"),
    ("nan-primitive", b"NaN"),
    ("input-byte-cap", b"x" * (r03.L["inputBytes"] + 1)),
    ("event-byte-cap", b"x" * (r03.L["eventBytes"] + 1)),
):
    kind = "Event" if name == "event-byte-cap" else "Input"
    reject(name, 1, lambda data=data, kind=kind: r03.parse(data, kind))

for name, value, code in (
    ("bool-version", True, 1),
    ("negative-version", -1, 2),
    ("floating-version", 1.5, 1),
    ("foreign-version", 2, 3),
):
    altered = copy.deepcopy(input_value)
    altered["command"]["contractVersion"] = value
    reject(name, code, lambda altered=altered: r03.raw(altered, "Input"))
altered = copy.deepcopy(input_value)
altered["command"]["expectedPriorVersion"] = 2**63
reject("long-overflow", 2, lambda: r03.raw(altered, "Input"))
altered = copy.deepcopy(input_value)
altered["unknown"] = None
reject("unknown-field", 1, lambda: r03.raw(altered, "Input"))
for kind, (minimum, maximum) in {
    "int": (0, 2**31 - 1), "long": (0, 2**63 - 1),
    "ulong": (0, 2**64 - 1), "signedInt": (-2**31, 2**31 - 1),
    "futureTurn": (1, 115), "differential": (-2, 2),
}.items():
    r03.typed(minimum, kind)
    r03.typed(maximum, kind)
    reject(kind + "-boolean", 1, lambda kind=kind: r03.typed(True, kind))
    reject(kind + "-underflow", 2, lambda kind=kind, minimum=minimum: r03.typed(minimum - 1, kind))
    reject(kind + "-overflow", 2, lambda kind=kind, maximum=maximum: r03.typed(maximum + 1, kind))
reject("control-byte-cap", 1, lambda: r03.parse(b"x" * (r03.L["controlBytes"] + 1), "Control"))
reject("proof-byte-cap", 1, lambda: r03.parse(b"x" * (r03.L["proofBytes"] + 1), "Proof"))
print(json.dumps(dict(status="PASS", rejected=rejected)))
