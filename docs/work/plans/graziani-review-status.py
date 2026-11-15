#!/usr/bin/env python3
"""Summarize bounded source-review packets; this is not an admission decision."""
import argparse
import collections
import json
import sys
from pathlib import Path

PLAN = Path(__file__).resolve().parent
sys.path.insert(0, str(PLAN / "graziani-source-review"))
from compare_readings import ReviewInputError, compare, load_json, manifest_hash  # noqa: E402


# Second reading is either an explicitly supplied file or the listed existing/template file.
PACKETS = {
    "pilot": ("graziani-source-review/manifest.json", "graziani-source-review/reading-a.json", "graziani-source-review/reading-b.json"),
    "supply": ("graziani-supply-review/manifest.json", "graziani-supply-review/reading-a.json", "graziani-supply-review/reading-template.json"),
    "trucks": ("graziani-truck-review/manifest.json", "graziani-truck-review/reading-a.json", "graziani-truck-review/reading-template.json"),
    "arrival-groups": ("graziani-arrival-review/manifest.json", "graziani-arrival-review/reading-a.json", "graziani-arrival-review/reading-template.json"),
    "arrival-members": ("graziani-arrival-review/member-manifest.json", "graziani-arrival-review/member-reading-a.json", "graziani-arrival-review/member-reading-template.json"),
    "axis-members": ("graziani-axis-formation-review/manifest.json", "graziani-axis-formation-review/reading-a.json", "graziani-axis-formation-review/reading-template.json"),
    "axis-relationships": ("graziani-axis-formation-review/relationship-manifest.json", "graziani-axis-formation-review/relationship-reading-a.json", "graziani-axis-formation-review/relationship-reading-template.json"),
    "rules": ("graziani-rules-review/manifest.json", "graziani-rules-review/reading-a.json", "graziani-rules-review/reading-template.json"),
    "transport": ("graziani-transport-review/manifest.json", "graziani-transport-review/reading-a.json", "graziani-transport-review/reading-template.json"),
    "convoys": ("graziani-convoy-review/manifest.json", "graziani-convoy-review/reading-a.json", "graziani-convoy-review/reading-template.json"),
    "fortifications": ("graziani-fortification-review/manifest.json", "graziani-fortification-review/reading-a.json", "graziani-fortification-review/reading-template.json"),
    "marine": ("graziani-marine-review/manifest.json", "graziani-marine-review/reading-a.json", "graziani-marine-review/reading-template.json"),
    "weather": ("graziani-weather-review/manifest.json", "graziani-weather-review/reading-a.json", "graziani-weather-review/reading-template.json"),
    "patrol": ("graziani-patrol-review/manifest.json", "graziani-patrol-review/reading-a.json", "graziani-patrol-review/reading-template.json"),
    "rail": ("graziani-rail-review/manifest.json", "graziani-rail-review/reading-a.json", "graziani-rail-review/reading-template.json"),
    "ports": ("graziani-port-review/manifest.json", "graziani-port-review/reading-a.json", "graziani-port-review/reading-template.json"),
    "victory": ("graziani-victory-review/manifest.json", "graziani-victory-review/reading-a.json", "graziani-victory-review/reading-template.json"),
}


def assignments(values, label):
    result = {}
    for value in values:
        key, separator, path = value.partition("=")
        if not separator or not key or key.strip() != key or not path or key in result:
            raise ReviewInputError(f"{label} entries must be unique ID=PATH pairs")
        result[key] = Path(path)
    return result


def summarize(name, manifest, readings, source_paths, using_template):
    report = compare(manifest, readings, source_paths)
    return {
        "packet": name,
        "scope": report["scope"],
        "manifest_sha256": report["manifest_sha256"],
        "reading_sha256": [manifest_hash(reading) for reading in readings],
        "expected_rows": report["expected_rows"],
        "expected_fields": sum(len(row["fields"]) for row in manifest["rows"]),
        "second_entry": "template" if using_template else "attributed-reading",
        "raw_issue_counts": dict(sorted(collections.Counter(issue["kind"] for issue in report["issues"]).items())),
        "raw_comparison": "open" if report["issues"] else "no-issues",
    }


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", action="append", default=[], metavar="ID=PATH")
    parser.add_argument("--reading", action="append", default=[], metavar="PACKET=PATH",
                        help="supply a completed second entry for a named packet")
    args = parser.parse_args(argv)
    try:
        source_paths = assignments(args.source, "source")
        overrides = assignments(args.reading, "reading")
        if set(overrides) - set(PACKETS):
            raise ReviewInputError("unknown packet override")
        manifests = {name: load_json(PLAN / paths[0]) for name, paths in PACKETS.items()}
        required = set().union(*(set(value["sources"]) for value in manifests.values()))
        if set(source_paths) != required:
            raise ReviewInputError("source files must match all packet source IDs")
        packets = []
        for name, paths in PACKETS.items():
            second = overrides.get(name, PLAN / paths[2])
            readings = [load_json(PLAN / paths[1]), load_json(second)]
            sources = {key: source_paths[key] for key in manifests[name]["sources"]}
            using_template = (isinstance(readings[1], dict)
                              and readings[1].get("reader_id") == "replace-with-reviewer-id")
            packets.append(summarize(name, manifests[name], readings, sources, using_template))
    except (OSError, ValueError, TypeError, KeyError) as error:
        print(f"invalid review status input: {error}", file=sys.stderr)
        return 2
    output = {"packets": packets,
              "note": "Raw comparison only. A supplied reader ID does not prove independence; open locator differences may have a separate human/agent disposition, and a clean comparison does not establish rights, source truth, complete scope or release acceptance."}
    print(json.dumps(output, ensure_ascii=False, indent=2))
    return 1 if any(packet["raw_comparison"] == "open" for packet in packets) else 0


if __name__ == "__main__":
    sys.exit(main())
