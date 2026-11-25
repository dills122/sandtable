"""Shared helpers for the reviewer-kit tools. Standard library only (Python 3.8+)."""
import hashlib
import json
import sys
from pathlib import Path

KIT_ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(Path(__file__).resolve().parent))

from compare_readings import ReviewInputError, load_json  # noqa: E402,F401


def kit_index():
    """The kit.json index: packets and the nine source packages."""
    return load_json(KIT_ROOT / "kit.json")


def file_sha256(path):
    digest = hashlib.sha256()
    with Path(path).open("rb") as handle:
        for block in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def discover_sources(directory, index=None):
    """Identify downloaded source files by SHA-256, whatever they are named.

    Returns (found, unmatched): found maps source ID to a path, unmatched lists files whose
    bytes match no known source package.
    """
    index = index or kit_index()
    by_hash = {value["sha256"]: key for key, value in index["sources"].items()}
    found, unmatched = {}, []
    root = Path(directory)
    if not root.is_dir():
        raise ReviewInputError(f"not a directory: {directory}")
    for path in sorted(root.rglob("*")):
        if not path.is_file() or path.name.startswith(".") or path.name == "README.txt":  # kit-provided note
            continue
        source_id = by_hash.get(file_sha256(path))
        if source_id is None:
            unmatched.append(str(path))
        elif source_id not in found:
            found[source_id] = path
    return found, unmatched


def packet_entry(index, name):
    for entry in index["packets"]:
        if entry["name"] == name:
            return entry
    raise ReviewInputError(f"unknown packet '{name}'; run with --list to see packet names")
