#!/usr/bin/env python3
"""Audit retained R03 canonical vectors against pinned R02 catalogue and row manifest."""
import argparse
import gzip
import hashlib
import importlib.util
import json
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument("vectors", type=Path)
args = parser.parse_args()
script = Path(__file__).resolve().parents[2] / "specs/verify-combat-actual-moved-repeat-breakdown-completion-v1.py"
spec = importlib.util.spec_from_file_location("r03_audit", script)
r03 = importlib.util.module_from_spec(spec)
spec.loader.exec_module(r03)
r03.verify_dependencies()
fixture_path = "docs/specs/fixtures/combat-actual-moved-repeat-movement-v1.json"
assert fixture_path in r03.PINS
fixture_bytes = (r03.REPOSITORY / fixture_path).read_bytes()
assert hashlib.sha256(fixture_bytes).hexdigest() == r03.PINS[fixture_path]
catalogue = json.loads(fixture_bytes)["cases"]
assert len(catalogue) == 56 and len({case["name"] for case in catalogue}) == 56
expected = r03.r02.semantic_rows()
completion_path = "docs/specs/fixtures/combat-actual-moved-repeat-movement-completion-v1.json"
assert completion_path in r03.PINS
completion_bytes = (r03.REPOSITORY / completion_path).read_bytes()
assert hashlib.sha256(completion_bytes).hexdigest() == r03.PINS[completion_path]
completion = json.loads(completion_bytes)
assert len(completion["cases"]) == 300 and completion["semanticRows"] == expected
assert len(expected) == 300 and len({row["rowKey"] for row in expected}) == 300
assert sum(row["profile"] == "paid" for row in expected) == 36
assert sum(row["profile"] == "armed" for row in expected) == 264
assert {row["rootOrdinal"] for row in expected} == set(range(56))
assert all(1 <= row["movementCut"] <= len(catalogue[row["rootOrdinal"]]["inputCanonicalUtf8"])
           and row["profile"] == catalogue[row["rootOrdinal"]]["profile"] for row in expected)

def sha(value):
    return "sha256:" + hashlib.sha256(value.encode("ascii")).hexdigest()

transcript = hashlib.sha256()
count = 0
with gzip.open(args.vectors, "rt", encoding="ascii") as stream:
    for line in stream:
        assert count < 300
        transcript.update(line.encode("ascii"))
        row = json.loads(line)
        expected_row = expected[count]
        assert (row["rowKey"], row["rootOrdinal"], row["movementCut"], row["profile"]) == (
            expected_row["rowKey"], expected_row["rootOrdinal"], expected_row["movementCut"],
            expected_row["profile"])
        vectors = row["vectors"]
        assert set(vectors) == {"parentSourceCanonicalUtf8", "parentInputLedgersCanonicalUtf8",
                                "inputCanonicalUtf8", "eventCanonicalUtf8", "sourceCanonicalUtf8",
                                "beforeControlCanonicalUtf8", "afterControlCanonicalUtf8",
                                "beforeProofCanonicalUtf8", "afterProofCanonicalUtf8"}
        for field, hash_field in (("parentSourceCanonicalUtf8", "parentSourceHash"),
                                  ("sourceCanonicalUtf8", "sourceHash"),
                                  ("eventCanonicalUtf8", "eventHash"),
                                  ("beforeControlCanonicalUtf8", "beforeControlHash"),
                                  ("afterControlCanonicalUtf8", "afterControlHash"),
                                  ("beforeProofCanonicalUtf8", "beforeProofHash"),
                                  ("afterProofCanonicalUtf8", "afterProofHash")):
            assert sha(vectors[field]) == row[hash_field], (row["rowKey"], field)
        source = json.loads(vectors["sourceCanonicalUtf8"])
        event = json.loads(vectors["eventCanonicalUtf8"])
        before = json.loads(vectors["beforeControlCanonicalUtf8"])
        after = json.loads(vectors["afterControlCanonicalUtf8"])
        before_proof = json.loads(vectors["beforeProofCanonicalUtf8"])
        after_proof = json.loads(vectors["afterProofCanonicalUtf8"])
        assert source["movementCompletionSourceCanonicalUtf8"] == vectors["parentSourceCanonicalUtf8"]
        assert source["breakdownEventCanonicalUtf8"] == [vectors["eventCanonicalUtf8"]]
        assert len(vectors["parentInputLedgersCanonicalUtf8"]) == 11
        assert event["input"] == json.loads(vectors["inputCanonicalUtf8"])
        assert event["receiptId"] == row["receiptId"] == after["breakdownCompletionReceiptId"]
        assert len(after["receipts"]) == len(before["receipts"]) + 1
        assert after["receipts"][-1] == after["breakdownReceipts"][0]
        assert before["breakdownReceipts"] == []
        assert after_proof["sourceHash"] == row["sourceHash"]
        assert after_proof["control"] == after and before_proof["control"] == before
        count += 1
assert count == 300, count
print(json.dumps(dict(status="PASS", roots=56, rows=count, paid=36, armed=264,
                      vectorTranscriptSha256=transcript.hexdigest(),
                      compressedSha256=hashlib.sha256(args.vectors.read_bytes()).hexdigest(),
                      compressedBytes=args.vectors.stat().st_size), indent=2))
