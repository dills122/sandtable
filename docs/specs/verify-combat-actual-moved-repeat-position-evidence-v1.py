#!/usr/bin/env python3
"""Offline P01 R03 ancestry replay; no Combat admission or action publication."""
import argparse
import collections
import gzip
import hashlib
import importlib.util
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location(
    "p01_reference", ROOT / "verify-combat-actual-moved-repeat-position-v1.py")
p01 = importlib.util.module_from_spec(spec)
spec.loader.exec_module(p01)

PINS = {
    "summary.json": "847972f5eb9c12be2e7a1774d2acd61caa1dc603b43413f7fb9d30cbec80c45b",
    "manifest.json": "39cae77c405b83a793b3d806fcf4a67860b9ff8514e59c9bfa83384d7dfc2b35",
    "bounded-profile-audit.json": "ba60a05a14ab907bb5be1e06509a5a24646a97759e564bcd525bdd7515bef429",
    "CNA_LandGameRules.pdf": p01.SCHEMA["physicalSourceHashes"]["landRulesPdf"],
    "ERR_CampaignforNorthAfrica.pdf": p01.SCHEMA["physicalSourceHashes"]["septemberErrataPdf"],
}


def require(condition, label):
    if not condition:
        raise ValueError(f"CMB-P01-EVIDENCE {label}")


def hash_bytes(data):
    return hashlib.sha256(data).hexdigest()


def original_ledgers(encoded):
    require(type(encoded) is list and len(encoded) == 11, "ledger count")
    require(type(encoded[0]) is dict, "creation request")
    require(all(type(ledger) is list for ledger in encoded[1:]), "ledger types")
    require(all(type(item) is str and item.isascii()
                for ledger in encoded[1:] for item in ledger), "canonical input types")
    return [encoded[0], *[[item.encode("ascii") for item in ledger] for ledger in encoded[1:]]]


def audit(evidence, requested=None, progress=None):
    evidence = Path(evidence)
    for name, expected in PINS.items():
        require(hash_bytes((evidence / name).read_bytes()) == expected, f"pin {name}")
    summary = json.loads((evidence / "summary.json").read_text())
    manifest = json.loads((evidence / "manifest.json").read_text())
    predicates = json.loads((evidence / "bounded-profile-audit.json").read_text())
    chunks = manifest["chunks"]
    require(summary["count"] == len(summary["rows"]) == len(predicates["rows"]) == 300,
            "row catalogue")
    require(len(chunks) == 11 and len({item["name"] for item in chunks}) == 11
            and {item["name"] for item in chunks} == set(summary["chunkSha256"])
            and {p.name for p in evidence.glob("rows-*.jsonl.gz")} == set(summary["chunkSha256"]),
            "chunk catalogue")
    if requested is not None:
        require(type(requested) is set and all(type(i) is int and 0 <= i < 300 for i in requested),
                "selected indices")
    seen = 0
    replayed = 0
    counts = collections.Counter()
    keys = set()
    for chunk in chunks:
        name = chunk["name"]
        raw = (evidence / name).read_bytes()
        require(hash_bytes(raw) == chunk["sha256"] == summary["chunkSha256"][name],
                f"chunk {name}")
        for line in gzip.decompress(raw).splitlines():
            row = json.loads(line)
            index = row["index"]
            require(index == seen, f"index {index}")
            item = row["r03"]
            require(item["rowKey"] == summary["rows"][index]["rowKey"]
                    == predicates["rows"][index]["rowKey"] and item["rowKey"] not in keys,
                    f"key {index}")
            keys.add(item["rowKey"])
            if requested is None or index in requested:
                vectors = item["vectors"]
                source = vectors["sourceCanonicalUtf8"].encode("ascii")
                ledgers = original_ledgers(vectors["parentInputLedgersCanonicalUtf8"])
                own = [vectors["inputCanonicalUtf8"].encode("ascii")]
                require(hash_bytes(source) == item["sourceHash"].removeprefix("sha256:"),
                        f"source vector {index}")
                assessment = p01.assess(source, ledgers, own)
                shape = predicates["rows"][index]["partition"]
                require(assessment["physicalShape"] == shape
                        and assessment["status"] == "unsupported"
                        and assessment["admitted"] is False
                        and assessment["publicActions"] == []
                        and assessment["sourceHash"] == item["sourceHash"],
                        f"assessment {index}")
                counts[shape] += 1
                replayed += 1
                if progress:
                    progress(index, item["rowKey"], shape)
            seen += 1
    require(seen == len(keys) == 300, "complete catalogue")
    if requested is None:
        require(replayed == 300 and counts == {"provisional-positive": 44,
                                               "provisional-empty": 256}, "partition")
    else:
        require(replayed == len(requested), "selected replay count")
    return dict(verifiedRows=replayed, provisionalPositive=counts["provisional-positive"],
                provisionalEmpty=counts["provisional-empty"], admittedRows=0)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("evidence", type=Path)
    parser.add_argument("--rows", nargs="*", type=int)
    parser.add_argument("--progress", action="store_true")
    args = parser.parse_args()
    print(audit(args.evidence, set(args.rows) if args.rows is not None else None,
                (lambda index, key, shape: print(index, key, shape, flush=True))
                if args.progress else None))
