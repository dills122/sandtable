#!/usr/bin/env python3
"""Compare two attributed research readings; no source authority is conferred."""

import argparse
import hashlib
import json
import re
import sys
from pathlib import Path


class ReviewInputError(ValueError):
    """A malformed or incompatible review input."""


def require(condition, message):
    if not condition:
        raise ReviewInputError(message)


def nonempty(value):
    return isinstance(value, str) and bool(value.strip()) and value == value.strip()


def unique(items, label):
    require(isinstance(items, list), f"{label} must be a list")
    require(all(nonempty(item) for item in items), f"{label} has an invalid item")
    require(len(items) == len(set(items)), f"{label} contains duplicates")
    return items


def source_hash(path):
    digest = hashlib.sha256()
    with Path(path).open("rb") as source:
        for block in iter(lambda: source.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def manifest_hash(manifest):
    encoded = json.dumps(manifest, ensure_ascii=False, sort_keys=True, separators=(",", ":")).encode("utf-8")
    return hashlib.sha256(encoded).hexdigest()


def load_json(path):
    def object_without_duplicates(pairs):
        result = {}
        for key, value in pairs:
            require(key not in result, f"duplicate JSON key: {key}")
            result[key] = value
        return result

    def reject_nonfinite(value):
        raise ReviewInputError(f"non-finite JSON number: {value}")

    return json.loads(Path(path).read_text(encoding="utf-8"),
                      object_pairs_hook=object_without_duplicates, parse_constant=reject_nonfinite)


def compare(manifest, readings, source_paths):
    require(isinstance(manifest, dict), "manifest must be an object")
    require(manifest.get("version") == 1 and type(manifest.get("version")) is int,
            "unsupported manifest version")
    scope = manifest.get("scope")
    require(nonempty(scope), "invalid scope")
    sources = manifest.get("sources")
    require(isinstance(sources, dict) and bool(sources), "sources must be nonempty")
    require(all(nonempty(k) and isinstance(v, str) and re.fullmatch(r"[0-9a-f]{64}", v)
                for k, v in sources.items()), "invalid source id or SHA-256")
    require(set(source_paths) == set(sources), "source files must match manifest sources")
    for source_id, digest in sources.items():
        require(source_hash(source_paths[source_id]) == digest,
                f"source hash mismatch: {source_id}")

    rows = manifest.get("rows")
    require(isinstance(rows, list) and bool(rows), "manifest rows must be nonempty")
    expected = {}
    for row in rows:
        require(isinstance(row, dict) and set(row) == {"id", "source", "fields"},
                "invalid manifest row")
        row_id, source_id = row["id"], row["source"]
        require(nonempty(row_id) and row_id not in expected, "invalid or duplicate manifest row id")
        require(nonempty(source_id) and source_id in sources, f"unknown source: {source_id}")
        fields = unique(row["fields"], f"fields for {row_id}")
        require(bool(fields), f"no fields for {row_id}")
        expected[row_id] = fields

    require(isinstance(readings, list) and len(readings) == 2, "two readings required")
    readers, entries = [], []
    for reading in readings:
        require(isinstance(reading, dict) and set(reading) == {"version", "scope", "reader_id", "manifest_sha256", "sources", "rows"},
                "invalid reading envelope")
        require(type(reading["version"]) is int and reading["version"] == 1,
                "unsupported reading version")
        require(reading["scope"] == scope, "reading scope mismatch")
        require(reading["manifest_sha256"] == manifest_hash(manifest), "reading manifest hash mismatch")
        reader = reading["reader_id"]
        require(nonempty(reader), "invalid reader id")
        readers.append(reader)
        require(reading["sources"] == sources, "reading source hashes differ from manifest")
        require(isinstance(reading["rows"], list), "reading rows must be a list")
        keyed = {}
        for row in reading["rows"]:
            require(isinstance(row, dict) and set(row) == {"id", "locator", "values"},
                    "invalid reading row")
            row_id = row["id"]
            require(nonempty(row_id) and row_id not in keyed, "invalid or duplicate reading row id")
            require(nonempty(row["locator"]), f"missing locator for {row_id}")
            values = row["values"]
            require(isinstance(values, dict) and all(nonempty(k) for k in values),
                    f"invalid fields for {row_id}")
            require(all(v is None or type(v) in (str, int, bool) for v in values.values()),
                    f"invalid value for {row_id}")
            require(all(not isinstance(v, str) or nonempty(v) for v in values.values()),
                    f"empty value for {row_id}; use null for unknown")
            keyed[row_id] = row
        entries.append(keyed)
    require(readers[0].casefold() != readers[1].casefold(), "reader ids must differ")

    issues = []
    for index, entry in enumerate(entries):
        if readers[index] == "replace-with-reviewer-id":
            issues.append({"kind": "unverified-reader", "reader": readers[index]})
        for row_id in sorted(set(entry) - set(expected)):
            issues.append({"kind": "unexpected-row", "reader": readers[index], "row": row_id})
    for row_id, fields in expected.items():
        for index, entry in enumerate(entries):
            if row_id not in entry:
                issues.append({"kind": "missing-row", "reader": readers[index], "row": row_id})
                continue
            if entry[row_id]["locator"] == "UNVERIFIED":
                issues.append({"kind": "unverified-locator", "reader": readers[index], "row": row_id})
            values = entry[row_id]["values"]
            for name in sorted(set(values) - set(fields)):
                issues.append({"kind": "unexpected-field", "reader": readers[index], "row": row_id, "field": name})
            for name in fields:
                if name not in values:
                    issues.append({"kind": "missing-field", "reader": readers[index], "row": row_id, "field": name})
                elif values[name] is None:
                    issues.append({"kind": "unknown", "reader": readers[index], "row": row_id, "field": name})
        if row_id in entries[0] and row_id in entries[1]:
            row_a, row_b = entries[0][row_id], entries[1][row_id]
            if row_a["locator"] != row_b["locator"]:
                issues.append({"kind": "locator-difference", "row": row_id,
                               "locators": [row_a["locator"], row_b["locator"]]})
            a, b = row_a["values"], row_b["values"]
            for name in fields:
                if name in a and name in b and a[name] is not None and b[name] is not None:
                    if type(a[name]) is not type(b[name]) or a[name] != b[name]:
                        issues.append({"kind": "disagreement", "row": row_id, "field": name,
                                       "values": [a[name], b[name]]})
    evidence = [{"row": row_id, "source": row["source"],
                 "locators": [entry[row_id]["locator"] if row_id in entry else None for entry in entries]}
                for row in rows for row_id in [row["id"]]]
    return {"scope": scope, "manifest_sha256": manifest_hash(manifest), "readers": readers,
            "expected_rows": len(expected), "evidence": evidence, "issues": issues,
            "note": "Attributed entries and matching bytes do not establish independent readers, source truth, rights or release acceptance."}


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("manifest", type=Path)
    parser.add_argument("reading_a", type=Path)
    parser.add_argument("reading_b", type=Path)
    parser.add_argument("--source", action="append", default=[], metavar="ID=PATH")
    args = parser.parse_args(argv)
    try:
        paths = {}
        for value in args.source:
            source_id, separator, path = value.partition("=")
            require(separator and nonempty(source_id) and bool(path) and source_id not in paths,
                    "source arguments must be unique ID=PATH pairs")
            paths[source_id] = path
        manifest = load_json(args.manifest)
        readings = [load_json(p) for p in (args.reading_a, args.reading_b)]
        result = compare(manifest, readings, paths)
    except (OSError, ValueError, TypeError, KeyError) as error:
        print(f"invalid review input: {error}", file=sys.stderr)
        return 2
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return 1 if result["issues"] else 0


if __name__ == "__main__":
    sys.exit(main())
