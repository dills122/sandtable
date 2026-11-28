#!/usr/bin/env python3
"""Conservative tracked-file provenance audit; never prints matched content."""

import argparse
from collections import Counter
import fnmatch
import hashlib
import json
import math
from pathlib import Path
import re
import subprocess


# Path rules cover the prior rights memo's conservative carve-outs, including
# review artifacts whose contents may no longer carry a source identifier.
PATH_RULES = (
    "src/Cna.Core/Content/HistoricalMap/*",
    "src/Cna.Core/Content/Cna1979Graziani*.cs",
    "src/Cna.Core/Content/Cna1979HistoricalMap.cs",
    *("src/Cna.Core/Rules/Cna1979" + name + ".cs" for name in (
        "CombatAdjudication", "Weather", "Breakdown", "Movement",
        "InitiativeRatings", "HistoricalTerrainEffects")),
    "src/Cna.Core/Campaigns/CampaignSource*.cs",
    "src/Cna.Core/Visibility/CampaignSource*.cs",
    "src/Cna.Core/Content/ContentSource*PackArtifact.cs",
    "tests/Cna.Core.Tests/Rules/Fixtures/*.golden.json",
    "tests/Shared/GrazianiSourceExcerpt.cs",
    "docs/research/fixtures/*",
    "docs/specs/fixtures/*",
    "docs/work/plans/*graziani*",
    "docs/research/*source*",
    "docs/*graziani*",
    "docs/*source*",
    "docs/specs/graziani-*.md",
    "docs/specs/weather-determination-v1.md",
    "docs/specs/combat-weather-v1.md",
    ".planning/*/evidence/*",
)
MARKERS = {
    "source_id": re.compile(r"spi-1979", re.IGNORECASE),
    "provenance_reference": re.compile(r"source[- ]derived|source[- ]backed|spigames\.net|CNA Living Rules", re.I),
    "source_origin": re.compile(r"\bSourceDerived\b"),
    "locator": re.compile(r'"oa-[\w.-]+'),
    "source_symbol": re.compile(
        r"\b(?:Cna1979Graziani\w*|\w*HistoricalMap\w*|CampaignSource\w*|"
        r"ContentSource\w*Pack\w*|Cna1979(?:CombatAdjudication|Weather|Breakdown|"
        r"Movement|InitiativeRatings|HistoricalTerrainEffects))\b"),
}
ID_KEY = re.compile(r'"(?:unit|hex|hexside)[_-]?(?:id|ids|key|keys)"\s*:', re.I)
NUMBER = re.compile(r"(?<![\w.])-?\d+(?:\.\d+)?(?![\w.])")
# Contiguous integer/decimal or quoted-string literals separated by commas.
# A fixed-length run bounds regex work and avoids nested unbounded repetitions.
LITERAL = r'(?:-?\d+(?:\.\d+)?|"[^"\r\n]{0,256}")'
ARRAY = re.compile(rf'{LITERAL}(?:\s*,\s*{LITERAL}){{31}}')
MANIFEST_PATH = "scripts/source_derived_allowlist.json"
ARCHIVE_SUFFIXES = {".zip", ".gz", ".tgz", ".7z", ".tar"}


def tracked_paths(root):
    raw = subprocess.check_output(["git", "-C", root, "ls-files", "-z", "--cached"])
    return sorted({p.decode("utf-8", errors="surrogateescape")
                   for p in raw.split(b"\0") if p})


def numeric_grid(text):
    """Count groups of 4+ consecutive dense numeric rows with diverse cells."""
    groups = 0
    rows = []

    def qualifies():
        if len(rows) < 4:
            return False
        counts = Counter(cell for row in rows for cell in row)
        total = sum(counts.values())
        entropy = -sum((n / total) * math.log2(n / total) for n in counts.values())
        return len(counts) >= 8 and entropy >= 2.5

    for line in text.splitlines():
        # Strip table separators; require numeric cells to dominate the row.
        cells = NUMBER.findall(line)
        residue = NUMBER.sub("", line)
        if len(cells) >= 6 and len(residue.strip(" |,;\t[]{}()")) <= len(line) / 4:
            rows.append(cells)
        else:
            groups += qualifies()
            rows.clear()
    return groups + qualifies()


def detect(path, root):
    counts = Counter()
    if any(fnmatch.fnmatchcase(path, pattern) for pattern in PATH_RULES):
        counts["known_path"] = 1
    file = root / path
    # Do not follow links to untracked/outside content. Treat opaque archives and
    # binary files as candidates rather than expanding them in public CI.
    if file.is_symlink() or not file.is_file():
        counts["unreadable"] = 1
        return counts
    if file.suffix.lower() in ARCHIVE_SUFFIXES:
        counts["archive"] = 1
        return counts
    try:
        # Huge files are review candidates in their own right. Bound expensive
        # text heuristics and memory use, rather than silently skipping them.
        if file.stat().st_size > 2 * 1024 * 1024:
            counts["large_file"] = 1
            return counts
        raw = file.read_bytes()
        text = raw.decode("utf-8-sig")
    except OSError:
        counts["unreadable"] = 1
        return counts
    except UnicodeDecodeError:
        counts["opaque_binary"] = 1
        return counts
    for kind, pattern in MARKERS.items():
        count = sum(1 for _ in pattern.finditer(text))
        if count:
            counts[kind] = count
    if file.suffix.lower() in {".json", ".jsonl"} and len(raw) >= 16_384:
        count = sum(1 for _ in ID_KEY.finditer(text))
        if count:
            counts["large_id_json"] = count
    for kind, count in (("numeric_grid", numeric_grid(text)),
                        ("literal_array", sum(1 for _ in ARRAY.finditer(text)))):
        if count:
            counts[kind] = count
    return counts


def audit(root, manifest):
    paths = tracked_paths(root)
    stale = sorted(manifest.keys() - set(paths))
    hits = []
    for path in paths:
        counts = detect(path, root)
        if counts:
            allowed = path in manifest and "unreadable" not in counts
            if allowed and manifest[path]["sha256"] is not None:
                try:
                    with (root / path).open("rb") as stream:
                        digest = hashlib.file_digest(stream, "sha256").hexdigest()
                    if digest != manifest[path]["sha256"]:
                        counts["baseline_changed"] = 1
                        allowed = False
                except OSError:
                    counts["unreadable"] = 1
                    allowed = False
            hits.append({"path": path, "status": "ALLOWED" if allowed else "UNEXPECTED",
                         "counts": dict(counts)})
    allowed = sum(row["status"] == "ALLOWED" for row in hits)
    return {"tracked_files": len(paths), "allowed": allowed,
            "unexpected": len(hits) - allowed, "stale_manifest_count": len(stale),
            "stale_manifest_paths": stale, "hits": hits}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path,
                        help="Exact tracked path to review reason and baseline hash JSON mapping")
    parser.add_argument("--strict", action="store_true", help="Fail on unexpected or stale paths")
    args = parser.parse_args()
    root = Path(subprocess.check_output(["git", "rev-parse", "--show-toplevel"],
                                       text=True).strip())
    manifest_path = args.manifest or root / MANIFEST_PATH
    try:
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        if not isinstance(manifest, dict) or not all(
            isinstance(entry, dict)
            and isinstance(entry.get("reason"), str)
            and entry["reason"].strip()
            and "sha256" in entry
            and ((path == MANIFEST_PATH and entry["sha256"] is None)
                 or (isinstance(entry["sha256"], str)
                     and re.fullmatch(r"[0-9a-f]{64}", entry["sha256"])))
            for path, entry in manifest.items()
        ):
            raise ValueError("invalid manifest")
    except (OSError, ValueError):
        # Never echo malformed input or excerpts.
        parser.error("manifest must map paths to nonempty reasons and SHA-256 hashes (self-entry may use null)")
    report = audit(root, manifest)
    print(json.dumps(report, sort_keys=True, indent=2))
    return int(args.strict and bool(report["unexpected"] or report["stale_manifest_count"]))


if __name__ == "__main__":
    raise SystemExit(main())
