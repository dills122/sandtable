#!/usr/bin/env python3
"""Audit retained R03 P01 ancestry and participant facts without admitting Combat.

Usage: python3 docs/work/verify-combat-p01-provenance.py PATH_TO_P01_EVIDENCE
The evidence directory is intentionally external to Git. This is an independent
retained-vector consistency check, not an independent replay or support certificate.
"""

import argparse
import collections
import gzip
import hashlib
import json
from pathlib import Path


def sha(data):
    return "sha256:" + hashlib.sha256(data).hexdigest()


def require(condition, message):
    if not condition:
        raise AssertionError(message)


parser = argparse.ArgumentParser()
parser.add_argument("evidence", type=Path)
args = parser.parse_args()
root = args.evidence.resolve()
require(sha((root / "summary.json").read_bytes()) ==
        "sha256:847972f5eb9c12be2e7a1774d2acd61caa1dc603b43413f7fb9d30cbec80c45b", "summary pin")
require(sha((root / "manifest.json").read_bytes()) ==
        "sha256:39cae77c405b83a793b3d806fcf4a67860b9ff8514e59c9bfa83384d7dfc2b35", "manifest pin")
require(sha((root / "bounded-profile-audit.json").read_bytes()) ==
        "sha256:ba60a05a14ab907bb5be1e06509a5a24646a97759e564bcd525bdd7515bef429", "audit pin")
summary = json.loads((root / "summary.json").read_text())
audit = json.loads((root / "bounded-profile-audit.json").read_text())
require(summary["count"] == 300, "summary count")
require(len(summary["rows"]) == len(audit["rows"]) == 300, "row count")
require(sha((root / "CNA_LandGameRules.pdf").read_bytes()) ==
        "sha256:b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99", "Land PDF pin")
require(sha((root / "ERR_CampaignforNorthAfrica.pdf").read_bytes()) ==
        "sha256:5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb", "errata PDF pin")

counts = collections.Counter()
keys = set()
seen = 0
for path in sorted(root.glob("rows-*.jsonl.gz")):
    require(path.name in summary["chunkSha256"], f"unexpected chunk {path.name}")
    require(sha(path.read_bytes()).removeprefix("sha256:") == summary["chunkSha256"][path.name],
            f"chunk hash {path.name}")
    with gzip.open(path, "rt", encoding="ascii") as stream:
        for line in stream:
            record = json.loads(line)
            index = record["index"]
            require(index == seen, f"index {index}")
            r03 = record["r03"]
            vector = r03["vectors"]
            control = json.loads(vector["afterControlCanonicalUtf8"])
            source = json.loads(vector["sourceCanonicalUtf8"])
            before = json.loads(vector["beforeControlCanonicalUtf8"])
            require(r03["rowKey"] == summary["rows"][index]["rowKey"] == audit["rows"][index]["rowKey"],
                    f"row key {index}")
            require(r03["rowKey"] not in keys, f"duplicate row key {index}")
            keys.add(r03["rowKey"])
            for field, payload in (("sourceHash", "sourceCanonicalUtf8"),
                                   ("parentSourceHash", "parentSourceCanonicalUtf8"),
                                   ("beforeControlHash", "beforeControlCanonicalUtf8"),
                                   ("afterControlHash", "afterControlCanonicalUtf8"),
                                   ("beforeProofHash", "beforeProofCanonicalUtf8"),
                                   ("afterProofHash", "afterProofCanonicalUtf8"),
                                   ("eventHash", "eventCanonicalUtf8")):
                require(r03[field] == sha(vector[payload].encode("ascii")), f"{field} {index}")
            require(len(vector["parentInputLedgersCanonicalUtf8"]) == 11, f"ledger count {index}")
            require(source["contractVersion"] == 1 and control["contractVersion"] == 1,
                    f"version {index}")
            require(source["movementCompletionSourceCanonicalUtf8"] == vector["parentSourceCanonicalUtf8"] and
                    source["breakdownEventCanonicalUtf8"] == [vector["eventCanonicalUtf8"]] and
                    control["r02SourceHash"] == r03["parentSourceHash"],
                    f"source ancestry {index}")
            proof = json.loads(vector["afterProofCanonicalUtf8"])
            require(proof["sourceHash"] == r03["sourceHash"] and proof["control"] == control,
                    f"proof binding {index}")
            request = vector["parentInputLedgersCanonicalUtf8"][0]
            cycle = control["activeCycle"]
            for key in ("campaignId", "rulesetHash", "setupId", "setupHash"):
                require(request[key] == cycle[key], f"creation/cycle {key} {index}")
            require(request["configurationHash"] == cycle["admittedPolicyBundleDigest"] and
                    request["content"]["hash"] == cycle["contentHash"] and
                    request["content"]["packId"] == cycle["contentPackId"] and
                    request["content"]["scenarioId"] == cycle["scenarioId"],
                    f"creation/pin binding {index}")
            require(control["activeCycle"]["ordinal"] == 2 and
                    control["originalCycle"]["ordinal"] == 1 and
                    control["activeCycle"]["actingSide"] in ("axis", "commonwealth"),
                    f"cycle {index}")
            require(control["sequencePosition"]["stepId"] == "land.step.position-determination" and
                    control["sequencePosition"]["activeSide"] is None,
                    f"position {index}")
            require(control["world"] == before["world"] and
                    control["randomState"] == before["randomState"], f"R03 changed world/RNG {index}")
            elements = {e["elementId"]: e for e in control["world"]["elements"]}
            reps = control["world"]["representations"]
            require(len(elements) == len(reps) == 2, f"participants {index}")
            require(set(elements) == {"axis-assault-battalion", "commonwealth-assault-battalion"},
                    f"element identity {index}")
            require({tuple(rep["boundElementIds"]) for rep in reps} ==
                    {(element_id,) for element_id in elements}, f"representation binding {index}")
            require(all(rep["bindingKind"] == "independent-element" and
                        rep["currentLocationId"] == elements[rep["boundElementIds"][0]]["currentLocationId"]
                        for rep in reps), f"representation location {index}")
            side = control["activeCycle"]["actingSide"]
            actor = elements[f"{side}-assault-battalion"]
            other = "commonwealth" if side == "axis" else "axis"
            defender = elements[f"{other}-assault-battalion"]
            row = summary["rows"][index]
            require(row["actor"]["elementId"] == actor["elementId"] and
                    row["opponent"]["elementId"] == defender["elementId"] and
                    row["actor"]["locationId"] == actor["currentLocationId"] and
                    row["opponent"]["locationId"] == defender["currentLocationId"],
                    f"summary participant {index}")
            require(record["classification"] == "unresolved" and
                    audit["rows"][index]["partition"] in ("provisional-positive", "provisional-empty"),
                    f"classification {index}")
            counts[audit["rows"][index]["partition"]] += 1
            seen += 1

require(seen == 300 and len(keys) == 300, "complete inventory")
require(counts == {"provisional-positive": 44, "provisional-empty": 256}, "pinned audit labels")
require(set(summary["chunkSha256"]) == {p.name for p in root.glob("rows-*.jsonl.gz")}, "chunk set")
print("PASS retained P01 provenance: 300 rows; 11 ledgers/row; exact cycle, position, participant and R03 world/RNG invariants")
print("Pinned audit labels: 44 provisional-positive, 256 provisional-empty; physical partition and Combat support are not independently proved here")
