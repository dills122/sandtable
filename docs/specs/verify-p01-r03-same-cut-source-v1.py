#!/usr/bin/env python3
"""Read-only physical archive parity for the private A1 same-cut Control claim."""

import gzip
import hashlib
import importlib.util
import json
import sys
from pathlib import Path


CONTROL_FILE_HASH = "3c4eb5a37af03f5e07c03e38403fed371d802af51bdd0099552e5fc3f3037cf4"
CONTROL_HASH = "1ac7c737fd7f7fe38d13b741cdc92ef658d3e4e9e2b9d06d736207f24c50de7c"


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def verify(archive: Path) -> None:
    repository = Path(__file__).resolve().parents[2]
    a1_path = repository / "docs/specs/verify-p01-r03-a1-physical-v1.py"
    specification = importlib.util.spec_from_file_location("a1_physical", a1_path)
    if specification is None or specification.loader is None:
        raise ValueError("missing A1 physical verifier")
    module = importlib.util.module_from_spec(specification)
    specification.loader.exec_module(module)
    module.verify(archive)

    chunk = archive / "rows-036-067.jsonl.gz"
    with gzip.open(chunk, "rt") as stream:
        rows = [json.loads(line)["r03"] for line in stream]
    matches = [row for row in rows if row["rowKey"] == "r012-c01"]
    if len(matches) != 1:
        raise ValueError("P01-A1S physical row mismatch")
    row = matches[0]
    control = row["vectors"]["afterControlCanonicalUtf8"].encode("ascii")
    fixture = repository / "docs/specs/fixtures/p01-r03-a1-r012-c01-after-control-v1.json.gz"
    compressed = fixture.read_bytes()
    if (sha256(compressed) != CONTROL_FILE_HASH or
            sha256(control) != CONTROL_HASH or
            row["afterControlHash"] != "sha256:" + CONTROL_HASH or
            gzip.decompress(compressed) != control):
        raise ValueError("P01-A1S physical Control mismatch")
    print("A1 same-cut terminal Control claim verified against pinned physical archive")


if __name__ == "__main__":
    if len(sys.argv) != 2:
        raise SystemExit("usage: verify-p01-r03-same-cut-source-v1.py ARCHIVE_DIRECTORY")
    verify(Path(sys.argv[1]))
