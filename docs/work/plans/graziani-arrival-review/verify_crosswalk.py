#!/usr/bin/env python3
"""Check research transcription joins; never certify source correctness or playability."""
import argparse
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "graziani-source-review"))
from compare_readings import load_json, manifest_hash, source_hash  # noqa: E402


GROUPS = {
    "oa-polish": "schedule-polish",
    "oa-2-rtr": "schedule-rtr-and-nz-artillery",
    "oa-7-rtr": "schedule-rtr-and-nz-artillery",
    "oa-nz-artillery": "schedule-rtr-and-nz-artillery",
    "oa-indian-artillery": "schedule-indian-artillery",
    "oa-hussars": "schedule-hussars",
    "oa-indian-brigade": "schedule-indian-brigade",
    "oa-nz-brigade": "schedule-nz-brigade",
}


def require(condition, message):
    if not condition:
        raise ValueError(message)


def check(manifest, reading, source):
    require(isinstance(manifest, dict) and isinstance(reading, dict), "manifest and reading must be objects")
    require(type(manifest.get("version")) is int and manifest.get("version") == 1
            and manifest.get("scope") == "graziani-cw-arrivals-six-turns-v1",
            "unexpected manifest identity")
    sources = manifest.get("sources")
    require(isinstance(sources, dict) and set(sources) == {"commonwealth-charts"}, "unexpected source")
    require(source_hash(source) == sources["commonwealth-charts"], "source hash mismatch")
    require(type(reading.get("version")) is int and reading.get("version") == 1
            and isinstance(reading.get("reader_id"), str) and reading["reader_id"].strip() == reading["reader_id"]
            and reading["reader_id"] not in {"", "replace-with-reviewer-id"}
            and reading.get("scope") == manifest["scope"]
            and reading.get("manifest_sha256") == manifest_hash(manifest)
            and reading.get("sources") == sources, "reading identity mismatch")
    require(isinstance(manifest.get("rows"), list) and isinstance(reading.get("rows"), list), "missing rows")
    specs = {row["id"]: row for row in manifest["rows"]}
    rows = {row["id"]: row for row in reading["rows"]}
    expected = set(GROUPS) | set(GROUPS.values())
    require(len(specs) == len(manifest["rows"]) and len(rows) == len(reading["rows"])
            and set(specs) == set(rows) == expected, "missing, duplicate or unexpected rows")
    for key in expected:
        spec, row = specs[key], rows[key]
        fields = ["turn", "stage", "listed_rows"] if key.startswith("oa-") else ["turn", "stage", "light", "medium", "heavy"]
        require(set(spec) == {"id", "source", "fields"} and spec["source"] == "commonwealth-charts"
                and spec["fields"] == fields, f"unexpected schema: {key}")
        require(set(row) == {"id", "locator", "values"} and isinstance(row["locator"], str)
                and bool(row["locator"].strip()) and row["locator"] == row["locator"].strip()
                and row["locator"] != "UNVERIFIED"
                and isinstance(row["values"], dict) and set(row["values"]) == set(fields), f"incomplete reading: {key}")
        value = row["values"]
        require(type(value["turn"]) is int and 1 <= value["turn"] <= 6
                and type(value["stage"]) is int and 1 <= value["stage"] <= 3, f"invalid position: {key}")
        if key.startswith("oa-"):
            require(type(value["listed_rows"]) is int and value["listed_rows"] > 0, f"invalid OA count: {key}")
        else:
            for field in ("light", "medium", "heavy"):
                entry = value[field]
                require(entry == "not-listed" or (type(entry) is int and entry >= 0), f"invalid truck value: {key}")
    schedule = [rows[key]["values"] for key in sorted(set(GROUPS.values()))]
    require(len({(row["turn"], row["stage"]) for row in schedule}) == len(schedule), "duplicate schedule slot")
    require({row["turn"] for row in schedule} == set(range(1, 7)), "expected one schedule row per turn")
    for oa, group in GROUPS.items():
        actual, expected_group = rows[oa]["values"], rows[group]["values"]
        require((actual["turn"], actual["stage"]) == (expected_group["turn"], expected_group["stage"]),
                f"schedule/OA position disagreement: {oa}")
    return {
        "scope": manifest["scope"],
        "manifest_sha256": manifest_hash(manifest),
        "reading_sha256": manifest_hash(reading),
        "source_sha256": sources["commonwealth-charts"],
        "schedule_rows": len(schedule),
        "oa_groups": len(GROUPS),
        "candidate_oa_rows": sum(rows[key]["values"]["listed_rows"] for key in GROUPS),
        "listed_truck_totals": {field: sum(row[field] for row in schedule if type(row[field]) is int)
                                for field in ("light", "medium", "heavy")},
        "truck_rows_with_unlisted_values": {field: sum(row[field] == "not-listed" for row in schedule)
                                             for field in ("light", "medium", "heavy")},
        "note": "One-reading join and arithmetic only; unlisted is not zero, and source truth, independence, legal arrivals and rights are not established.",
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("manifest", type=Path)
    parser.add_argument("reading", type=Path)
    parser.add_argument("source", type=Path)
    args = parser.parse_args()
    try:
        print(json.dumps(check(load_json(args.manifest), load_json(args.reading), args.source), indent=2))
    except (OSError, ValueError, TypeError, KeyError) as error:
        print(f"invalid crosswalk: {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
