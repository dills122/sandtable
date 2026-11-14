#!/usr/bin/env python3
"""Check two-source research joins without admitting historical content."""
import argparse
import json
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "graziani-source-review"))
from compare_readings import load_json, manifest_hash, source_hash  # noqa: E402

MEMBER_IDS = {
    "cirene-hq", "cirene-157-hq", "cirene-157-i", "cirene-157-ii", "cirene-157-iii",
    "cirene-158-hq", "cirene-158-i", "cirene-158-ii", "cirene-158-iii", "cirene-mg",
    "cirene-at", "cirene-artillery", "cirene-engineer", "cirene-bersaglieri", "cirene-tank",
    "marmarica-hq", "marmarica-115-hq", "marmarica-115-i", "marmarica-115-ii", "marmarica-115-iii",
    "marmarica-116-hq", "marmarica-116-i", "marmarica-116-ii", "marmarica-116-iii", "marmarica-mg",
    "marmarica-at", "marmarica-artillery", "marmarica-engineer", "marmarica-tank",
}


def require(condition, message):
    if not condition:
        raise ValueError(message)


def envelope(manifest, reading, scope, sources):
    require(isinstance(manifest, dict) and isinstance(reading, dict), "invalid envelopes")
    require(type(manifest.get("version")) is int and manifest["version"] == 1
            and manifest.get("scope") == scope and manifest.get("sources") == sources, "manifest identity mismatch")
    reader = reading.get("reader_id")
    require(type(reading.get("version")) is int and reading["version"] == 1
            and reading.get("scope") == scope and reading.get("sources") == sources
            and reading.get("manifest_sha256") == manifest_hash(manifest)
            and isinstance(reader, str) and reader.strip() == reader
            and reader not in {"", "replace-with-reviewer-id"}, "reading identity mismatch")
    require(isinstance(manifest.get("rows"), list) and isinstance(reading.get("rows"), list), "missing rows")
    specs = {row["id"]: row for row in manifest["rows"]}
    rows = {row["id"]: row for row in reading["rows"]}
    require(len(specs) == len(manifest["rows"]) and len(rows) == len(reading["rows"])
            and set(specs) == set(rows), "missing or duplicate rows")
    for key, row in rows.items():
        spec = specs[key]
        require(set(spec) == {"id", "source", "fields"} and spec["source"] in sources
                and isinstance(spec["fields"], list) and bool(spec["fields"])
                and len(set(spec["fields"])) == len(spec["fields"]), f"invalid schema: {key}")
        require(set(row) == {"id", "locator", "values"} and isinstance(row["locator"], str)
                and row["locator"].strip() == row["locator"] and row["locator"] not in {"", "UNVERIFIED"}
                and isinstance(row["values"], dict) and set(row["values"]) == set(spec["fields"]), f"incomplete row: {key}")
        require(all(isinstance(v, str) and v.strip() == v and v != "" for v in row["values"].values()),
                f"unknown or invalid field: {key}")
    return rows


def check(member_manifest, member_reading, relation_manifest, relation_reading, scenario, errata):
    sources = {"scenario": source_hash(scenario), "errata": source_hash(errata)}
    members = envelope(member_manifest, member_reading, "graziani-axis-formations-members-v1", {"errata": sources["errata"]})
    relationships = envelope(relation_manifest, relation_reading, "graziani-axis-setup-relationships-v1", sources)
    require(set(relationships) == {"cirene-scenario", "marmarica-scenario", "cirene-tank-oa", "marmarica-tank-oa"},
            "unexpected relationship rows")
    require(set(members) == MEMBER_IDS, "unexpected member scope")
    require(all(row["source"] == "errata" for row in member_manifest["rows"]), "unexpected member source")
    require(all(row["source"] == ("scenario" if row["id"].endswith("-scenario") else "errata")
                for row in relation_manifest["rows"]), "unexpected relationship source")
    identities = set()
    for key, row in members.items():
        values = row["values"]
        required = {"counter_abbreviation", "id_code", "toe_mark", "arrival_mark"}
        if key.endswith("-tank"):
            required |= {"assigned_parent", "initial_attachment"}
        require(set(values) == required and values["arrival_mark"] == "D", f"unexpected OA fields or marker: {key}")
        identity = (values["counter_abbreviation"], values["id_code"])
        require(identity not in identities, f"duplicate bounded candidate identity: {key}")
        identities.add(identity)
    for group in ("cirene", "marmarica"):
        scenario_row = relationships[f"{group}-scenario"]["values"]
        oa = relationships[f"{group}-tank-oa"]["values"]
        member = members[f"{group}-tank"]["values"]
        require(set(scenario_row) == {"hex", "tank_mark", "parent_mark"}
                and re.fullmatch(r"[A-E][0-9]{4}", scenario_row["hex"]) is not None, f"invalid setup row: {group}")
        require(set(oa) == {"counter_abbreviation", "assigned_parent", "initial_attachment", "arrival_mark"},
                f"invalid tank OA row: {group}")
        require(scenario_row["tank_mark"] == oa["counter_abbreviation"] == member["counter_abbreviation"]
                and all(oa[k] == member[k] for k in ("assigned_parent", "initial_attachment", "arrival_mark")),
                f"tank relationship mismatch: {group}")
    return {"member_manifest_sha256": manifest_hash(member_manifest),
            "member_reading_sha256": manifest_hash(member_reading),
            "relationship_manifest_sha256": manifest_hash(relation_manifest),
            "relationship_reading_sha256": manifest_hash(relation_reading),
            "source_sha256": sources,
            "candidate_members": len(members), "scenario_oa_tank_joins": 2,
            "note": "Same-reader identity joins only; source truth, abbreviation interpretation, unit legality, independence and rights remain open."}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ("member_manifest", "member_reading", "relationship_manifest", "relationship_reading", "scenario", "errata"):
        parser.add_argument(name, type=Path)
    args = parser.parse_args()
    try:
        print(json.dumps(check(load_json(args.member_manifest), load_json(args.member_reading),
                               load_json(args.relationship_manifest), load_json(args.relationship_reading),
                               args.scenario, args.errata), indent=2))
    except (OSError, ValueError, TypeError, KeyError) as error:
        print(f"invalid formation join: {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
