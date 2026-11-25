#!/usr/bin/env python3
"""Check which of the nine source PDFs you have downloaded, by content hash (file names do not matter)."""
import argparse
import sys

from kit_common import ReviewInputError, discover_sources, kit_index


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("directory", help="folder containing the downloaded PDFs (for example sources/)")
    args = parser.parse_args(argv)
    try:
        index = kit_index()
        found, unmatched = discover_sources(args.directory, index)
    except (OSError, ValueError) as error:
        print(f"error: {error}", file=sys.stderr)
        return 2
    for source_id, info in index["sources"].items():
        if source_id in found:
            print(f"OK       {source_id:<20} {found[source_id]}")
        else:
            print(f"MISSING  {source_id:<20} download: {info['url']}")
    for path in unmatched:
        print(f"UNKNOWN  {path}  (bytes match no expected source; wrong or changed file - do not use it)")
    needed = {name: [s for s in entry["sources"] if s not in found]
              for name, entry in ((e["name"], e) for e in index["packets"])}
    ready = [name for name, missing in needed.items() if not missing]
    print(f"\npackets you can read now: {', '.join(ready) if ready else 'none yet'}")
    return 0 if len(found) == len(index["sources"]) and not unmatched else 1


if __name__ == "__main__":
    sys.exit(main())
