#!/usr/bin/env python3
"""Private, denial-only P01 privacy check; no outward action admission."""

import gzip
import hashlib
import importlib.util
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent
EVIDENCE = Path("/Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence")


def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, ROOT / filename)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


p01 = load("p01_privacy_reference", "verify-combat-actual-moved-repeat-position-v1.py")
source_audit = load("p01_privacy_source_audit", "verify-combat-actual-moved-repeat-position-evidence-v1.py")


def require(condition, reason):
    if not condition:
        raise ValueError("CMB-P01-PRIVACY " + reason)


def denial_bytes(assessment):
    """An internal proof envelope, not a P01 public action/observation contract."""
    require(type(assessment) is dict and list(assessment) == p01.SCHEMA["assessmentFields"],
            "assessment fields")
    require(type(assessment["contractVersion"]) is int and assessment["contractVersion"] == 1
            and assessment["status"] == "unsupported" and assessment["admitted"] is False
            and type(assessment["publicActions"]) is list and assessment["publicActions"] == [],
            "unsupported-only boundary")
    # Deliberately do not copy the private candidate, source/world/random hashes,
    # physical partition, authority prefix, cycle, or position into this envelope.
    return p01.canonical(dict(contractVersion=1, status="unsupported",
                              admitted=False, publicActions=[]))


def audit(evidence=EVIDENCE):
    """Authenticate retained sources and check all 300 denial projections."""
    evidence = Path(evidence)
    for name, pin in source_audit.PINS.items():
        require(hashlib.sha256((evidence / name).read_bytes()).hexdigest() == pin,
                "source pin " + name)
    summary = json.loads((evidence / "summary.json").read_bytes())
    manifest = json.loads((evidence / "manifest.json").read_bytes())
    partitions = json.loads((evidence / "bounded-profile-audit.json").read_bytes())
    require(summary["count"] == len(summary["rows"]) == len(partitions["rows"]) == 300,
            "row catalogue")
    require(len(manifest["chunks"]) == 11 and
            {x["name"] for x in manifest["chunks"]} == set(summary["chunkSha256"]) ==
            {p.name for p in evidence.glob("rows-*.jsonl.gz")}, "chunk catalogue")
    counts = {"provisional-positive": 0, "provisional-empty": 0}
    keys = set()
    encoded = set()
    index = 0
    for chunk in manifest["chunks"]:
        raw = (evidence / chunk["name"]).read_bytes()
        require(hashlib.sha256(raw).hexdigest() == chunk["sha256"] ==
                summary["chunkSha256"][chunk["name"]], "chunk pin")
        for line in gzip.decompress(raw).splitlines():
            row = json.loads(line)
            require(row["index"] == index and row["r03"]["rowKey"] ==
                    summary["rows"][index]["rowKey"] ==
                    partitions["rows"][index]["rowKey"], "row identity")
            require(row["r03"]["rowKey"] not in keys, "unique row key")
            keys.add(row["r03"]["rowKey"])
            vectors = row["r03"]["vectors"]
            source = vectors["sourceCanonicalUtf8"].encode("ascii")
            require(hashlib.sha256(source).hexdigest() ==
                    row["r03"]["sourceHash"].removeprefix("sha256:"), "source vector")
            ledgers = source_audit.original_ledgers(
                vectors["parentInputLedgersCanonicalUtf8"])
            assessment = p01.assess(source, ledgers,
                                    [vectors["inputCanonicalUtf8"].encode("ascii")])
            shape = assessment["physicalShape"]
            require(shape == partitions["rows"][index]["partition"] and shape in counts,
                    "physical partition")
            counts[shape] += 1
            encoded.add(denial_bytes(assessment))
            index += 1
    require(index == len(keys) == 300 and counts ==
            {"provisional-positive": 44, "provisional-empty": 256}, "complete partition")
    require(encoded == {b'{"contractVersion":1,"status":"unsupported",'
                        b'"admitted":false,"publicActions":[]}'},
            "single denial encoding")
    return dict(authenticatedRows=index, provisionalPositive=counts["provisional-positive"],
                provisionalEmpty=counts["provisional-empty"], admittedRows=0,
                denialSha256=hashlib.sha256(next(iter(encoded))).hexdigest())


if __name__ == "__main__":
    import argparse
    parser = argparse.ArgumentParser()
    parser.add_argument("evidence", nargs="?", type=Path, default=EVIDENCE)
    print(audit(parser.parse_args().evidence))
