#!/usr/bin/env python3
"""Reject every authentic nonterminal R02 lifecycle cut for all 300 R03 rows."""
import argparse
import copy
import gzip
import importlib.util
import json
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument("vectors", type=Path)
parser.add_argument("--progress", action="store_true")
args = parser.parse_args()
script = Path(__file__).resolve().parents[2] / "specs/verify-combat-actual-moved-repeat-breakdown-completion-v1.py"
spec = importlib.util.spec_from_file_location("r03_all_cuts", script)
r03 = importlib.util.module_from_spec(spec)
spec.loader.exec_module(r03)
r03.verify_dependencies()
expected = r03.r02.semantic_rows()
counts = dict(rows=0, paid=0, armed=0, rejectedCuts=0)
with gzip.open(args.vectors, "rt", encoding="ascii") as stream:
    for line in stream:
        assert counts["rows"] < 300
        row = json.loads(line)
        target = expected[counts["rows"]]
        assert (row["rowKey"], row["profile"], row["movementCut"]) == (
            target["rowKey"], target["profile"], target["movementCut"])
        v = row["vectors"]
        parent = r03.r02.parse(v["parentSourceCanonicalUtf8"].encode("ascii"), "Source")
        all_events = parent["lifecycleEventCanonicalUtf8"]
        ledgers = v["parentInputLedgersCanonicalUtf8"]
        assert len(ledgers) == 11 and len(all_events) == len(ledgers[-1])
        for cut in range(len(all_events)):
            partial = copy.deepcopy(parent)
            partial["lifecycleEventCanonicalUtf8"] = all_events[:cut]
            predecessor = r03.r02.raw(partial, "Source")
            cut_args = [copy.deepcopy(ledgers[0]), *(
                [item.encode("ascii") for item in ledger] for ledger in ledgers[1:-1]),
                [item.encode("ascii") for item in ledgers[-1][:cut]]]
            assert len(cut_args) == 11
            try:
                r03.replay(r03.source_for(predecessor), cut_args, [])
            except r03.Invalid as error:
                assert error.code in ("CMB-MRB-003", "CMB-MRB-004"), (row["rowKey"], cut, error)
            else:
                raise AssertionError((row["rowKey"], cut, "accepted nonterminal R02 cut"))
            counts["rejectedCuts"] += 1
        counts[row["profile"]] += 1
        counts["rows"] += 1
        if args.progress:
            print("VERIFIED", counts["rows"], "/ 300", row["rowKey"],
                  "nonterminalCuts", counts["rejectedCuts"], flush=True)
assert counts == dict(rows=300, paid=36, armed=264, rejectedCuts=788), counts
print(json.dumps(dict(status="PASS", **counts)))
