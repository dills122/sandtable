#!/usr/bin/env python3
"""Explicit one-time canonical fixture generation; ordinary verification is read-only."""

import argparse
import hashlib
import importlib.util
import json
import sys
from pathlib import Path

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2] / "specs"
OUTPUT = ROOT / "fixtures/combat-actual-moved-repeat-breakdown-completion-v1.json"


def load(name):
    path = ROOT / name
    spec = importlib.util.spec_from_file_location(name.removesuffix(".py").replace("-", "_"), path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def cut(child, source, context, state):
    control = child.raw(state, "Control")
    proof = child.raw(child._proof_candidate(source, context, state), "Proof")
    return {
        "source": {"bytes": len(source), "sha256": child.sha(source)},
        "control": {"bytes": len(control), "sha256": child.sha(control)},
        "proof": {"bytes": len(proof), "sha256": child.sha(proof)},
    }


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--write-once", action="store_true", required=True)
    args = parser.parse_args()
    assert args.write_once and not OUTPUT.exists(), "fixture already exists; ordinary checks must not rewrite it"
    parent = load("verify-combat-actual-moved-repeat-movement-completion-v1.py")
    child = load("verify-combat-actual-moved-repeat-breakdown-completion-v1.py")
    predecessor_fixture = json.loads(parent.FIXTURE.read_bytes())
    rows = parent.semantic_rows()
    cases_by_key = {case["rowKey"]: case for case in predecessor_fixture["cases"]}
    assert len(rows) == len(cases_by_key) == 300
    cases = []
    event_hashes = set()
    manifest = None
    for index, row in enumerate(rows, 1):
        packet, arguments = parent.root(row)
        predecessor_case = cases_by_key[row["rowKey"]]
        packet["lifecycleEventCanonicalUtf8"] = predecessor_case["eventCanonicalUtf8"]
        predecessor_source = parent.source_bytes(packet)
        arguments = [*arguments, [entry.encode("ascii") for entry in predecessor_case["inputCanonicalUtf8"]]]
        source = child.source_for(predecessor_source)
        context, before, _ = child.replay(source, arguments, [])
        command = child.command(context, before)
        completed, ledger, _ = child.apply(source, arguments, [], command)
        final_context, after, _ = child.replay(completed, arguments, ledger)
        event = child.parse(completed, "Source")["breakdownEventCanonicalUtf8"][0]
        event_hash = child.sha(event.encode("ascii"))
        assert event_hash not in event_hashes
        event_hashes.add(event_hash)
        current_manifest = context["base"]["dependencyManifestHash"]
        manifest = current_manifest if manifest is None else manifest
        assert manifest == current_manifest
        cases.append({
            "rootOrdinal": row["rootOrdinal"],
            "movementCut": row["movementCut"],
            "profile": row["profile"],
            "group": row["group"],
            "rowKey": row["rowKey"],
            "inputCanonicalUtf8": [item.decode("ascii") for item in ledger],
            "eventCanonicalUtf8": [event],
            "cuts": [cut(child, source, context, before), cut(child, completed, final_context, after)],
        })
        if index % 25 == 0:
            print("generated", index, flush=True)
    assert len(cases) == len(event_hashes) == 300
    fixture = {
        "contractVersion": 1,
        "wireSchemaSha256": "sha256:" + digest(ROOT / "combat-actual-moved-repeat-breakdown-completion-v1.schema.json"),
        "referenceSha256": "sha256:" + digest(ROOT / "verify-combat-actual-moved-repeat-breakdown-completion-v1.py"),
        "predecessorFixtureSha256": "sha256:" + digest(parent.FIXTURE),
        "dependencyManifestHash": manifest,
        "semanticRows": rows,
        "cases": cases,
    }
    data = json.dumps(fixture, ensure_ascii=True, separators=(",", ":")).encode("ascii") + b"\n"
    OUTPUT.write_bytes(data)
    print("WROTE", OUTPUT, "bytes", len(data), "sha256", hashlib.sha256(data).hexdigest(), flush=True)


if __name__ == "__main__":
    main()
