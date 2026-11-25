#!/usr/bin/env python3
"""Re-run the mechanical comparison of first readings against returned readings (project owner use).

Both folders hold one file per packet named <packet>.json. The tool hashes the actual source files,
checks every manifest binding and reports every difference. It is a raw comparison only: it does not
prove the readers are independent, the source is right, or that any rights question is settled.
"""
import argparse
import collections
import json
import sys
from pathlib import Path

from kit_common import KIT_ROOT, ReviewInputError, discover_sources, kit_index, load_json
from compare_readings import compare, manifest_hash


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--first-readings", type=Path, required=True, help="folder of <packet>.json first readings")
    parser.add_argument("--readings", type=Path, required=True, help="folder of <packet>.json returned readings")
    parser.add_argument("--sources-dir", type=Path, required=True, help="folder with the source PDFs")
    parser.add_argument("--packet", action="append", default=[], help="limit to these packets")
    parser.add_argument("--full", action="store_true", help="print every issue, not just counts")
    args = parser.parse_args(argv)
    try:
        index = kit_index()
        sources, _ = discover_sources(args.sources_dir, index)
        chosen = [e for e in index["packets"] if not args.packet or e["name"] in args.packet]
        if args.packet and len(chosen) != len(set(args.packet)):
            raise ReviewInputError("unknown packet name in --packet")
        results = []
        for entry in chosen:
            first, second = args.first_readings / f"{entry['name']}.json", args.readings / f"{entry['name']}.json"
            if not first.is_file() or not second.is_file():
                results.append({"packet": entry["name"], "status": "not-supplied",
                                "missing": [str(p) for p in (first, second) if not p.is_file()]})
                continue
            missing = sorted(set(entry["sources"]) - set(sources))
            if missing:
                raise ReviewInputError(f"packet {entry['name']}: source files not found by hash: {', '.join(missing)}")
            manifest = load_json(KIT_ROOT / entry["manifest"])
            report = compare(manifest, [load_json(first), load_json(second)],
                             {key: sources[key] for key in entry["sources"]})
            counts = dict(sorted(collections.Counter(i["kind"] for i in report["issues"]).items()))
            item = {"packet": entry["name"], "scope": report["scope"], "manifest_sha256": manifest_hash(manifest),
                    "readers": report["readers"], "expected_rows": report["expected_rows"],
                    "issue_counts": counts, "status": "open" if report["issues"] else "no-issues"}
            if args.full:
                item["issues"] = report["issues"]
            results.append(item)
    except (OSError, ValueError, TypeError, KeyError) as error:
        print(f"invalid input: {error}", file=sys.stderr)
        return 2
    print(json.dumps({"packets": results,
                      "note": "Raw comparison only; see tool docstring. Exit 1 means differences or missing readings remain."},
                     ensure_ascii=False, indent=2))
    return 1 if any(r["status"] != "no-issues" for r in results) else 0


if __name__ == "__main__":
    sys.exit(main())
