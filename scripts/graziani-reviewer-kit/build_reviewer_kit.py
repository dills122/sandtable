#!/usr/bin/env python3
"""Build the blind-reading reviewer kit for the 17 Graziani source-review packets.

Reads the packets under docs/work/plans (manifests, blank reading templates, comparator) and writes a
self-contained folder plus a deterministic zip that a reviewer can use with no repository knowledge.
First readings and prior comparison results are never copied. Nothing in the repository is modified.

    python3 scripts/graziani-reviewer-kit/build_reviewer_kit.py --out /tmp/graziani-reviewer-kit
"""
import argparse
import hashlib
import importlib.util
import json
import re
import shutil
import sys
import zipfile
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO = HERE.parent.parent
PLANS = REPO / "docs" / "work" / "plans"
KIT_FILES = HERE / "kit-files"
KIT_NAME = "graziani-reviewer-kit"

# Source packages named in docs/work/plans/2026-11-13-graziani-review-status.md ("Reacquiring source files").
SOURCES = {
    "scenario": ("Scenario booklet", "https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf"),
    "errata": ("September 1979 errata", "https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf"),
    "land-rules": ("Land game rules", "https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf"),
    "land-compiled": ("Complete archival package (compiled rules)", "https://spigames.net/PDFv2/CampaignNorthAfrica.pdf"),
    "air-rules": ("Air and Logistics rules", "https://spigames.net/PDFv10/CNA_AirGameRules.pdf"),
    "common-charts": ("Common charts", "https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf"),
    "commonwealth-charts": ("Commonwealth charts", "https://spigames.net/PDFv10/CNA_ChartsCWPlayer.pdf"),
    "axis-charts": ("Axis charts", "https://spigames.net/PDFv10/CNA_ChartsAxisPlayer.pdf"),
    "maps": ("Five map sheets", "https://spigames.net/PDFv10/CNA_Maps.pdf"),
}

# Packet name -> (suggested order group, why), from the same status document's "Review order" table.
ORDER = {
    "pilot": (0, "Existing pilot (Matruh, NZ, formations)"),
    "supply": (1, "Initial quantities and allocations"), "trucks": (1, "Initial quantities and allocations"),
    "arrival-groups": (2, "Timing, unit identities and formations"), "arrival-members": (2, "Timing, unit identities and formations"),
    "axis-members": (2, "Timing, unit identities and formations"), "axis-relationships": (2, "Timing, unit identities and formations"),
    "marine": (2, "Timing, unit identities and formations"),
    "rules": (3, "Conflicting printed text and applicability"), "victory": (3, "Conflicting printed text and applicability"),
    "ports": (3, "Conflicting printed text and applicability"), "fortifications": (3, "Conflicting printed text and applicability"),
    "transport": (4, "Movement, plans, resources and losses"), "convoys": (4, "Movement, plans, resources and losses"),
    "rail": (4, "Movement, plans, resources and losses"),
    "weather": (5, "Stage effects and limited disclosure"), "patrol": (5, "Stage effects and limited disclosure"),
}

# Packet README sentences or list items that match any of these are dropped from the reviewer notes: they
# disclose earlier readings or outcomes, or point at repository files the reviewer does not have.
LEAK = re.compile(
    r"reading-(a|b|independent)|member-reading|relationship-reading|comparison|compar(e|ing|ison)|"
    r"compare_readings|first entry|first reading|earlier|prior|previous|independent(ly)? review|re-?check|"
    r"disposition|match(es|ed)? all|agree|differ|November|docs/work|/private/tmp|\.py\b|"
    r"unit ?test|researcher|working interpretation|planning|\bsession\b|blank second|one (reader|reading|source reader)|"
    r"research aid|same-reader|(no|an) independent|no second|attribut|importing|canonical (SHA|JSON)|reader (identit|names)|"
    r"self-test|bind the reading|existing reading|second (member )?reading|internal consistency|same-stream|retain", re.I)
LINK = re.compile(r"\[([^\]]+)\]\((?!https?://)[^)]*\)")
SENTENCE = re.compile(r"(?<=[.!?;:])\s+(?=[A-Z`\[*(])")

PLACEHOLDER_READER = "replace-with-reviewer-id"


def load_status_module():
    spec = importlib.util.spec_from_file_location("graziani_review_status", PLANS / "graziani-review-status.py")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def split_blocks(text):
    blocks, current, fenced = [], [], False
    for line in text.splitlines():
        if line.startswith("```"):
            fenced = not fenced
        if not line.strip() and not fenced:
            if current:
                blocks.append(current)
                current = []
        else:
            current.append(line)
    if current:
        blocks.append(current)
    return blocks


def units_of(lines):
    """Smallest droppable pieces of one block: fence, table row, list item, heading, or sentence."""
    first = lines[0]
    if first.startswith("```") or first.startswith("#"):
        return ["\n".join(lines)], "\n"
    if first.startswith("|"):
        return list(lines), "\n"
    if first.lstrip().startswith(("- ", "* ")) or re.match(r"\s*\d+\.\s", first):
        items = []
        for line in lines:
            if re.match(r"\s*([-*]|\d+\.)\s", line) or not items:
                items.append(line)
            else:
                items[-1] += "\n" + line
        return items, "\n"
    return SENTENCE.split(" ".join(line.strip() for line in lines)), " "


def sanitize_readme(text):
    """Return (clean text, dropped units, kept text) after removing leaky or repo-specific pieces."""
    kept_blocks, dropped = [], []
    for lines in split_blocks(text):
        units, joiner = units_of(lines)
        kept = [LINK.sub(r"\1", unit) for unit in units if not LEAK.search(unit)]
        dropped += [unit for unit in units if LEAK.search(unit)]
        was_fence = lines[0].startswith("```")
        if was_fence and not kept and kept_blocks and kept_blocks[-1].rstrip().endswith(":"):
            kept_blocks.pop()  # a lead-in line such as "Compare with:" whose command was removed
        if kept:
            kept_blocks.append(joiner.join(kept))
    return "\n\n".join(kept_blocks), dropped


def sha256_bytes(data):
    return hashlib.sha256(data).hexdigest()


def assert_blank_template(template, manifest, manifest_digest, name):
    if template.get("reader_id") != PLACEHOLDER_READER:
        raise SystemExit(f"{name}: template is not blank (reader_id)")
    if template.get("manifest_sha256") != manifest_digest:
        raise SystemExit(f"{name}: template is not bound to the current manifest (stale template)")
    expected = {row["id"]: row["fields"] for row in manifest["rows"]}
    rows = {row["id"]: row for row in template["rows"]}
    if set(rows) != set(expected):
        raise SystemExit(f"{name}: template rows differ from manifest")
    for row_id, fields in expected.items():
        row = rows[row_id]
        if row["locator"] != "UNVERIFIED" or set(row["values"]) != set(fields) or any(
                value is not None for value in row["values"].values()):
            raise SystemExit(f"{name}: template row {row_id} is not blank")


def packet_notes(name, entry, manifest, notes_body, dropped_count):
    lines = [f"# Packet `{name}`", "",
             f"Scope: `{manifest['scope']}`. {entry['rows']} rows, {entry['fields']} values.", "",
             "Sources needed (identified by hash in `sources/`): " + ", ".join(
                 f"`{s}` ({SOURCES[s][0]})" for s in entry["sources"]) + ".", "",
             "## Rows to read", "",
             "Each row ID names the target; it does not encode the answer. Fields are listed in the order they appear in your reading file.", "",
             "| Row ID | Source | Fields |", "| --- | --- | --- |"]
    lines += [f"| `{row['id']}` | `{row['source']}` | {', '.join(f'`{f}`' for f in row['fields'])} |"
              for row in manifest["rows"]]
    lines += ["", "## Packet-specific reading instructions", ""]
    lines.append(notes_body if notes_body.strip() else "_No packet-specific text; follow START-HERE.md._")
    lines += ["", "---", f"_Generated from the project's packet README; {dropped_count} sentence(s) or item(s) that referred to "
              "earlier readings or repository files were removed so the reading stays blind._", ""]
    return "\n".join(lines)


def build(out_dir):
    status = load_status_module()
    compare_readings = PLANS / "graziani-source-review" / "compare_readings.py"
    sys.path.insert(0, str(compare_readings.parent))
    from compare_readings import load_json, manifest_hash  # noqa: E402

    root = Path(out_dir) / KIT_NAME
    if root.exists():
        shutil.rmtree(root)
    (root / "tools").mkdir(parents=True)
    (root / "packets").mkdir()
    (root / "sources").mkdir()
    (root / "returns").mkdir()
    files = {}  # relative path -> bytes

    source_hashes, packets, dropped_report = {}, [], {}
    for name, (manifest_rel, _first, second_rel) in status.PACKETS.items():
        manifest_path = PLANS / manifest_rel
        manifest = load_json(manifest_path)
        digest = manifest_hash(manifest)
        template_path = PLANS / ("graziani-source-review/reading-template.json" if name == "pilot" else second_rel)
        template = load_json(template_path)
        assert_blank_template(template, manifest, digest, name)
        for source_id, source_digest in manifest["sources"].items():
            if source_hashes.setdefault(source_id, source_digest) != source_digest:
                raise SystemExit(f"source {source_id} has conflicting hashes across manifests")
        readme = manifest_path.parent / "README.md"
        body, dropped = sanitize_readme(readme.read_text(encoding="utf-8")) if readme.is_file() else ("", [])
        dropped_report[name] = dropped
        entry = {"name": name, "scope": manifest["scope"], "manifest": f"packets/{name}/manifest.json",
                 "template": f"packets/{name}/reading-template.json",
                 "sources": list(manifest["sources"]), "rows": len(manifest["rows"]),
                 "fields": sum(len(row["fields"]) for row in manifest["rows"]),
                 "order_group": ORDER[name][0]}
        packets.append(entry)
        base = f"packets/{name}/"
        files[base + "manifest.json"] = manifest_path.read_bytes()
        files[base + "reading-template.json"] = template_path.read_bytes()
        files[base + "NOTES.md"] = packet_notes(name, entry, manifest, body, len(dropped)).encode("utf-8")

    if set(source_hashes) != set(SOURCES):
        raise SystemExit(f"source table out of date: manifests use {sorted(source_hashes)}")
    index = {"kit_version": 1,
             "sources": {key: {"description": SOURCES[key][0], "url": SOURCES[key][1], "sha256": source_hashes[key]}
                         for key in SOURCES},
             "packets": packets}
    files["kit.json"] = (json.dumps(index, indent=2, sort_keys=True) + "\n").encode("utf-8")

    for tool in sorted(KIT_FILES.glob("*.py")):
        files["tools/" + tool.name] = tool.read_bytes()
    files["tools/compare_readings.py"] = compare_readings.read_bytes()
    files["READER-DECLARATION.md.tmpl"] = (KIT_FILES / "READER-DECLARATION.md.tmpl").read_bytes()
    files["sources/README.txt"] = b"Save the downloaded source PDFs in this folder (see START-HERE.md, Step 1).\n"
    files["returns/README.txt"] = b"Your completed readings go here as <packet-name>.json (see START-HERE.md, Step 3).\n"

    total_rows = sum(p["rows"] for p in packets)
    total_fields = sum(p["fields"] for p in packets)
    source_table = "\n".join(
        f"| `{key}` | {SOURCES[key][0]} | <{SOURCES[key][1]}> | `{source_hashes[key]}` |" for key in SOURCES)
    groups = {}
    for p in sorted(packets, key=lambda p: (p["order_group"], p["name"])):
        groups.setdefault(p["order_group"], []).append(p)
    order_table = "| Order | Packets (rows / values) | Why |\n| --- | --- | --- |\n" + "\n".join(
        f"| {'pilot' if g == 0 else g} | " + ", ".join(f"`{p['name']}` ({p['rows']}/{p['fields']})" for p in group)
        + f" | {ORDER[group[0]['name']][1]} |" for g, group in sorted(groups.items()))
    start = (KIT_FILES / "START-HERE.md.tmpl").read_text(encoding="utf-8")
    for key, value in {"ROWS": str(total_rows), "FIELDS": str(total_fields), "PACKETS": str(len(packets)),
                       "SOURCE_TABLE": source_table, "ORDER_TABLE": order_table}.items():
        start = start.replace("{{" + key + "}}", value)
    files["START-HERE.md"] = start.encode("utf-8")

    files["MANIFEST.sha256"] = "".join(
        f"{sha256_bytes(data)}  {path}\n" for path, data in sorted(files.items())).encode("utf-8")
    for path, data in files.items():
        target = root / path
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(data)

    report = ["# Reviewer kit sanitizer report (owner only; not part of the kit)", "",
              "Review each packet's kept notes in the built kit, and the pieces removed below, before sending the kit.", ""]
    for name, dropped in dropped_report.items():
        report += [f"## {name}: {len(dropped)} removed", ""] + [f"- {item.strip()[:300]!r}" for item in dropped] + [""]
    (Path(out_dir) / "sanitizer-report.md").write_text("\n".join(report), encoding="utf-8")

    archive = Path(out_dir) / f"{KIT_NAME}.zip"
    with zipfile.ZipFile(archive, "w", zipfile.ZIP_DEFLATED) as zipped:
        for path, data in sorted(files.items()):
            info = zipfile.ZipInfo(f"{KIT_NAME}/{path}", date_time=(1980, 1, 1, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            info.external_attr = 0o644 << 16
            zipped.writestr(info, data)
    return {"directory": str(root), "zip": str(archive), "zip_sha256": sha256_bytes(archive.read_bytes()),
            "packets": len(packets), "rows": total_rows, "fields": total_fields, "files": len(files)}


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--out", type=Path, required=True,
                        help="output directory (keep it outside the repository; created if missing)")
    args = parser.parse_args(argv)
    args.out.mkdir(parents=True, exist_ok=True)
    print(json.dumps(build(args.out), indent=2))
    return 0


if __name__ == "__main__":
    sys.exit(main())
