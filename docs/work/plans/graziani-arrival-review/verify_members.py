#!/usr/bin/env python3
"""Join candidate OA members to grouped research readings, without source certification."""
import argparse
import json
import sys
from pathlib import Path

from verify_crosswalk import check, load_json, manifest_hash, require


MEMBERS = {
    "oa-polish": ["polish-hq", "polish-1-bn", "polish-2-bn", "polish-3-bn", "polish-kopanski-mg", "polish-cavalry", "polish-at", "polish-artillery"],
    "oa-2-rtr": ["2-rtr"],
    "oa-7-rtr": ["7-rtr"],
    "oa-nz-artillery": ["6-nz-field"],
    "oa-indian-artillery": ["1-field-4-indian"],
    "oa-hussars": ["3-hussars"],
    "oa-indian-brigade": ["7-indian-hq", "royal-sussex", "4-11-sikhs", "4-16-punjab"],
    "oa-nz-brigade": ["6-nz-hq", "24-nz", "25-nz", "26-nz"],
}
FIELDS = ["counter_abbreviation", "id_code", "toe_mark", "arrival_mark"]


def check_members(group_manifest, group_reading, member_manifest, member_reading, source):
    result = check(group_manifest, group_reading, source)
    require(isinstance(member_manifest, dict) and isinstance(member_reading, dict), "member envelopes must be objects")
    require(type(member_manifest.get("version")) is int and member_manifest["version"] == 1
            and member_manifest.get("scope") == "graziani-cw-oa-members-v1"
            and member_manifest.get("sources") == group_manifest["sources"], "member manifest identity mismatch")
    require(type(member_reading.get("version")) is int and member_reading["version"] == 1
            and member_reading.get("scope") == member_manifest["scope"]
            and member_reading.get("sources") == member_manifest["sources"]
            and member_reading.get("manifest_sha256") == manifest_hash(member_manifest), "member reading identity mismatch")
    reader = member_reading.get("reader_id")
    require(isinstance(reader, str) and reader.strip() == reader and reader not in {"", "replace-with-reviewer-id"},
            "unverified member reader")
    require(isinstance(member_manifest.get("rows"), list) and isinstance(member_reading.get("rows"), list), "missing member rows")
    expected = [item for members in MEMBERS.values() for item in members]
    require(len(expected) == len(set(expected)), "duplicate expected member")
    specs = {row["id"]: row for row in member_manifest["rows"]}
    rows = {row["id"]: row for row in member_reading["rows"]}
    require(len(specs) == len(member_manifest["rows"]) and len(rows) == len(member_reading["rows"])
            and set(specs) == set(rows) == set(expected), "missing, duplicate or unexpected member")
    groups = {row["id"]: row["values"] for row in group_reading["rows"]}
    for group, members in MEMBERS.items():
        values = groups[group]
        require(values["listed_rows"] == len(members), f"member count mismatch: {group}")
        mark = f'{values["stage"]}/{values["turn"]}'
        for member in members:
            spec, row = specs[member], rows[member]
            require(set(spec) == {"id", "source", "fields"} and spec["source"] == "commonwealth-charts"
                    and spec["fields"] == FIELDS, f"unexpected member schema: {member}")
            require(set(row) == {"id", "locator", "values"} and isinstance(row["locator"], str)
                    and row["locator"].strip() == row["locator"] and row["locator"] not in {"", "UNVERIFIED"}
                    and isinstance(row["values"], dict) and set(row["values"]) == set(FIELDS), f"incomplete member: {member}")
            require(all(isinstance(value, str) and value.strip() == value and value != ""
                        for value in row["values"].values()), f"invalid member mark: {member}")
            require(row["values"]["arrival_mark"] == mark, f"member arrival mismatch: {member}")
    return {"group_manifest_sha256": result["manifest_sha256"],
            "group_reading_sha256": result["reading_sha256"],
            "member_manifest_sha256": manifest_hash(member_manifest),
            "member_reading_sha256": manifest_hash(member_reading),
            "source_sha256": result["source_sha256"],
            "groups": len(MEMBERS), "candidate_members": len(expected),
            "note": "One-reader structural join only; it does not verify the printed source, identities, legal strengths, independence or rights."}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("group_manifest", type=Path)
    parser.add_argument("group_reading", type=Path)
    parser.add_argument("member_manifest", type=Path)
    parser.add_argument("member_reading", type=Path)
    parser.add_argument("source", type=Path)
    args = parser.parse_args()
    try:
        print(json.dumps(check_members(load_json(args.group_manifest), load_json(args.group_reading),
                                       load_json(args.member_manifest), load_json(args.member_reading), args.source), indent=2))
    except (OSError, ValueError, TypeError, KeyError) as error:
        print(f"invalid member crosswalk: {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
