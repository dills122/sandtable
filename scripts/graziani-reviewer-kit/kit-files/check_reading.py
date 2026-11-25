#!/usr/bin/env python3
"""Self-check one completed reading before you send it back. Needs no other reading and no repo."""
import argparse
import json
import sys
from pathlib import Path

from kit_common import (KIT_ROOT, ReviewInputError, discover_sources, kit_index, load_json,
                        packet_entry)
from compare_readings import manifest_hash, nonempty


def check(manifest, reading, available_sources):
    """Return (problems, notes). Problems block sending; notes are for you to review."""
    problems, notes = [], []
    expected = {row["id"]: row["fields"] for row in manifest["rows"]}
    expected_keys = {"version", "scope", "reader_id", "manifest_sha256", "sources", "rows"}
    if not isinstance(reading, dict) or set(reading) != expected_keys:
        return ["top level must be an object with exactly: " + ", ".join(sorted(expected_keys))], notes
    if reading["version"] != 1 or type(reading["version"]) is not int:
        problems.append("version must be the integer 1")
    if reading["scope"] != manifest["scope"]:
        problems.append(f"scope must be '{manifest['scope']}'")
    if reading["manifest_sha256"] != manifest_hash(manifest):
        problems.append("manifest_sha256 does not match this packet's manifest.json (start again from the template)")
    if reading["sources"] != manifest["sources"]:
        problems.append("'sources' block differs from the manifest (do not edit it)")
    reader = reading["reader_id"]
    if not nonempty(reader) or reader == "replace-with-reviewer-id":
        problems.append("reader_id is still the placeholder; set a stable identifier for yourself")
    if not isinstance(reading["rows"], list):
        return problems + ["'rows' must be a list"], notes

    seen, unknown, filled, placeholder_locators = set(), 0, 0, 0
    for row in reading["rows"]:
        if not isinstance(row, dict) or set(row) != {"id", "locator", "values"}:
            problems.append("every row needs exactly: id, locator, values")
            continue
        row_id = row["id"]
        if not nonempty(row_id):
            problems.append(f"a row has an invalid id (must be non-empty text): {json.dumps(row_id)[:60]}")
            continue
        if row_id in seen:
            problems.append(f"duplicate row {row_id}")
        seen.add(row_id)
        if row_id not in expected:
            problems.append(f"row '{row_id}' is not in the manifest")
            continue
        if not nonempty(row["locator"]) or row["locator"] == "UNVERIFIED":
            placeholder_locators += 1
            problems.append(f"{row_id}: locator is empty or still UNVERIFIED (give page, rule/table and row)")
        values = row["values"]
        if not isinstance(values, dict):
            problems.append(f"{row_id}: values must be an object")
            continue
        for name in sorted(set(values) - set(expected[row_id])):
            problems.append(f"{row_id}: unexpected field '{name}'")
        for name in expected[row_id]:
            if name not in values:
                problems.append(f"{row_id}: missing field '{name}'")
                continue
            value = values[name]
            if value is None:
                unknown += 1
                notes.append(f"{row_id}.{name} is null (recorded as unknown)")
            elif isinstance(value, str) and (not nonempty(value)):
                problems.append(f"{row_id}.{name}: empty or space-padded text; use null for unknown")
            elif type(value) not in (str, int, bool):
                problems.append(f"{row_id}.{name}: only integers, true/false, text or null are allowed")
            else:
                filled += 1
    for row_id in sorted(set(expected) - seen):
        problems.append(f"missing row '{row_id}'")

    total = sum(len(fields) for fields in expected.values())
    notes.insert(0, f"{len(seen & set(expected))}/{len(expected)} rows, {filled}/{total} fields filled, {unknown} unknown (null)")
    if available_sources is None:
        notes.append("source files not checked (pass --sources-dir to confirm you read the exact hash-matched files)")
    else:
        for source_id in sorted(set(manifest["sources"]) - set(available_sources)):
            problems.append(f"source '{source_id}' not found in your sources folder (hash must match)")
    return problems, notes


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--packet", help="packet name, for example supply")
    parser.add_argument("--reading", type=Path, help="your completed reading JSON")
    parser.add_argument("--sources-dir", type=Path, help="folder with the downloaded source PDFs")
    parser.add_argument("--list", action="store_true", help="list packet names and exit")
    args = parser.parse_args(argv)
    try:
        index = kit_index()
        if args.list:
            for entry in index["packets"]:
                print(f"{entry['name']:<20} {entry['rows']:>3} rows {entry['fields']:>3} fields   sources: {', '.join(entry['sources'])}")
            return 0
        if not args.packet or not args.reading:
            parser.error("--packet and --reading are required (or use --list)")
        entry = packet_entry(index, args.packet)
        manifest = load_json(KIT_ROOT / entry["manifest"])
        reading = load_json(args.reading)
        sources = None
        if args.sources_dir:
            sources, _ = discover_sources(args.sources_dir, index)
    except (OSError, ValueError, TypeError, KeyError, json.JSONDecodeError) as error:
        print(f"cannot check: {error}", file=sys.stderr)
        return 2
    try:
        problems, notes = check(manifest, reading, sources)
    except (TypeError, KeyError, AttributeError) as error:
        print(f"cannot check: malformed reading structure ({type(error).__name__}: {error})", file=sys.stderr)
        return 2
    for note in notes[:1] + ([n for n in notes[1:] if not n.endswith("(recorded as unknown)")]):
        print(f"note: {note}")
    unknowns = [n for n in notes if n.endswith("(recorded as unknown)")]
    if unknowns:
        print(f"note: {len(unknowns)} unknown (null) values; first few: " + "; ".join(unknowns[:5]))
    for problem in problems:
        print(f"PROBLEM: {problem}")
    print("READY TO SEND" if not problems else f"NOT READY: {len(problems)} problem(s)")
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
