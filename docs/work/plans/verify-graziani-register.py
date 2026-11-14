#!/usr/bin/env python3
"""Count the bounded source register's rows, not scenario mechanics or support."""
import argparse
import collections
import hashlib
import json
import re
from pathlib import Path


ROW = re.compile(r"^[A-Z]{2}-[0-9]{2}$")


def summarize(text):
    heading = "## Initial obligation rows\n"
    if text.count(heading) != 1:
        raise ValueError("expected one initial-obligation heading")
    tail = text.split(heading, 1)[1]
    terminator = "\n\nThe [victory matrix]"
    if tail.count(terminator) != 1:
        raise ValueError("missing or duplicate table terminator")
    lines = tail.split(terminator, 1)[0].splitlines()
    while lines and not lines[0].strip():
        lines.pop(0)
    if len(lines) < 3 or not lines[0].startswith("| ID |") or not lines[1].startswith("| ---"):
        raise ValueError("missing register table header")
    rows = {}
    for line in lines[2:]:
        if not line.startswith("|"):
            raise ValueError("unexpected content within register table")
        cells = [cell.strip() for cell in line.strip().strip("|").split("|")]
        if len(cells) != 4 or not ROW.fullmatch(cells[0]) or any(not cell for cell in cells):
            raise ValueError("malformed register row")
        if cells[3].casefold() in {"none", "n/a", "closed", "resolved", "complete"}:
            raise ValueError("update counting policy before marking a row complete")
        row_id = cells[0]
        if row_id in rows:
            raise ValueError(f"duplicate row: {row_id}")
        rows[row_id] = cells
    if not rows:
        raise ValueError("empty register")
    counts = collections.Counter(row_id.split("-")[0] for row_id in rows)
    return {
        "scope": "recorded rows in initial obligation table only",
        "recorded_rows": len(rows),
        "rows_by_prefix": dict(sorted(counts.items())),
        "rows_with_nonempty_outstanding_field": len(rows),
        "unsupported_mechanics_count": None,
        "note": "Every recorded row has an outstanding-evidence entry; neither source completeness nor runtime support was evaluated.",
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("register", type=Path)
    args = parser.parse_args()
    data = args.register.read_bytes()
    result = summarize(data.decode("utf-8"))
    result["register_sha256"] = hashlib.sha256(data).hexdigest()
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main()
