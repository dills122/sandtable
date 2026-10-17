#!/usr/bin/env python3
"""Authentic paid and armed R02 nonterminal cuts reject as R03 predecessors."""
import copy
import importlib.util
import json
from pathlib import Path

script = Path(__file__).resolve().parents[2] / "specs/verify-combat-actual-moved-repeat-breakdown-completion-v1.py"
spec = importlib.util.spec_from_file_location("r03_cuts", script)
r03 = importlib.util.module_from_spec(spec)
spec.loader.exec_module(r03)

counts = dict(paid=0, armed=0)
for index in (0, 41):
    row = r03.r02.semantic_rows()[index]
    source, args = r03.parent_terminal(row)
    packet = r03.r02.parse(source, "Source")
    events = packet["lifecycleEventCanonicalUtf8"]
    for cut in range(len(events)):
        partial = copy.deepcopy(packet)
        partial["lifecycleEventCanonicalUtf8"] = events[:cut]
        predecessor = r03.r02.raw(partial, "Source")
        candidate = r03.source_for(predecessor)
        ledgers = copy.deepcopy(args)
        ledgers[-1] = ledgers[-1][:cut]
        try:
            r03.replay(candidate, ledgers, [])
        except r03.Invalid as error:
            assert error.code in ("CMB-MRB-003", "CMB-MRB-004"), (row, cut, error)
        else:
            raise AssertionError((row, cut, "nonterminal predecessor accepted"))
        counts[row["profile"]] += 1

assert counts == dict(paid=1, armed=3), counts
print(json.dumps(dict(status="PASS", authenticNonterminalCuts=counts)))
