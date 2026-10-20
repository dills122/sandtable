#!/usr/bin/env python3
"""Read-only physical provenance gate for the private r012-c01 A1 fixture."""

import gzip
import hashlib
import json
import sys
from pathlib import Path


MANIFEST_HASH = "39cae77c405b83a793b3d806fcf4a67860b9ff8514e59c9bfa83384d7dfc2b35"
CHUNK_HASH = "9f79444d6efe3577f7739b4e1dac363f20bb589e7ad0661e58d7293d3006f5cb"
CATALOGUE_HASH = "cf3f8ecfe9e4040d5678a6e0e9cc2441525e1e8fe3a3f35cf316207a9f45ec4e"
IMPORT_HASH = "5019978589938979d7010416f6e3424e07615532475f5633a60db66414db14aa"
MANIFEST_ID = "sha256:0052acb77da04ecbbee792a0cb330fd4d8189cca1513d5d450fbb716d265cfeb"


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def canonical(value: object) -> bytes:
    return json.dumps(value, separators=(",", ":"), ensure_ascii=False).encode("ascii")


def require(condition: bool, label: str) -> None:
    if not condition:
        raise ValueError(f"P01-A1 physical evidence mismatch: {label}")


def verify(archive: Path) -> None:
    repository = Path(__file__).resolve().parents[2]
    manifest_bytes = (archive / "manifest.json").read_bytes()
    require(digest(manifest_bytes) == MANIFEST_HASH, "manifest hash")
    manifest = json.loads(manifest_bytes)
    require(manifest["counts"]["rows"] == 300, "row count")
    require(manifest["counts"]["parentInputLedgersPerRow"] == 11, "ledger count")
    require(manifest["counts"]["runtimeSourcePins"] == 108, "runtime source pin count")
    chunk = archive / "rows-036-067.jsonl.gz"
    require(digest(chunk.read_bytes()) == CHUNK_HASH, "chunk hash")
    require(next(entry["sha256"] for entry in manifest["chunks"] if entry["name"] == chunk.name) == CHUNK_HASH, "manifest chunk hash")
    with gzip.open(chunk, "rt") as stream:
        row = next(json.loads(line)["r03"] for line in stream if json.loads(line)["r03"]["rowKey"] == "r012-c01")
    require(row["rootOrdinal"] == 12 and row["movementCut"] == 1, "selected row and cut")
    catalogue_path = repository / "docs/specs/fixtures/combat-actual-moved-repeat-breakdown-native-hashes-v1.json"
    catalogue_bytes = catalogue_path.read_bytes()
    require(digest(catalogue_bytes) == CATALOGUE_HASH == manifest["sourceHashes"]["nativeHashCatalogue"], "catalogue hash")
    frozen = next(entry for entry in json.loads(catalogue_bytes)["rows"] if entry["rowKey"] == "r012-c01")
    require(frozen == {key: row[key] for key in frozen}, "frozen selected vector")
    vectors = row["vectors"]
    for expected, field in (
        ("sourceHash", "sourceCanonicalUtf8"),
        ("eventHash", "eventCanonicalUtf8"),
        ("afterControlHash", "afterControlCanonicalUtf8"),
        ("afterProofHash", "afterProofCanonicalUtf8"),
        ("beforeControlHash", "beforeControlCanonicalUtf8"),
        ("beforeProofHash", "beforeProofCanonicalUtf8"),
        ("parentSourceHash", "parentSourceCanonicalUtf8"),
    ):
        require("sha256:" + digest(vectors[field].encode("ascii")) == row[expected], expected)
    retained = vectors["parentInputLedgersCanonicalUtf8"]
    require(len(retained) == 11 and len(vectors["inputCanonicalUtf8"]) > 0, "retained inputs")
    require(isinstance(retained[0], dict), "retained creation request")
    require(all(isinstance(ledger, list) for ledger in retained[1:]), "retained predecessor ledgers")
    expected_import = {
        "contractVersion": 1,
        "creationRequestCanonicalUtf8": canonical(retained[0]).decode("ascii"),
        "predecessorInputLedgersCanonicalUtf8": retained[1:],
        "r03InputLedgerCanonicalUtf8": [vectors["inputCanonicalUtf8"]],
        "r03SourceCanonicalUtf8": vectors["sourceCanonicalUtf8"],
        "dependencyManifestHash": MANIFEST_ID,
    }
    fixture = repository / "docs/specs/fixtures/p01-r03-a1-r012-c01-import-v1.json.gz"
    require(digest(fixture.read_bytes()) == IMPORT_HASH, "import fixture hash")
    require(gzip.decompress(fixture.read_bytes()) == canonical(expected_import), "import fixture content")
    print("A1 physical archive, independent catalogue, and selected import fixture verified")


if __name__ == "__main__":
    if len(sys.argv) != 2:
        raise SystemExit("usage: verify-p01-r03-a1-physical-v1.py ARCHIVE_DIRECTORY")
    verify(Path(sys.argv[1]))
