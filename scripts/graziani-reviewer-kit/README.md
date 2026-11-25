# Graziani reviewer kit

Builds one self-contained folder and zip so a person with no repository knowledge can do blind second
readings of the 17 Graziani source-review packets (212 rows, 666 values), and so any returned reading can
be compared mechanically.

Status: documentation and scripts only. Nothing here changes `src/` or moves data. The packet manifests,
templates and the comparator remain where they are under `docs/work/plans/`; the builder copies them.

## Build

```sh
python3 -I scripts/graziani-reviewer-kit/build_reviewer_kit.py --out /tmp/graziani-reviewer-kit-build
```

Output (keep it outside the repository):

- `graziani-reviewer-kit/` and `graziani-reviewer-kit.zip` (deterministic: same inputs give the same zip hash).
- `sanitizer-report.md` (owner only): every README sentence removed from the reviewer notes. Skim it, and
  skim the generated `packets/*/NOTES.md`, before sending the kit. The sanitizer is a regex filter, not a proof of blindness.

The kit contains, per packet: `manifest.json`, a blank `reading-template.json` and `NOTES.md` (row table plus the
packet README with sentences about earlier readings and repository paths removed). It also contains `START-HERE.md`
(step-by-step, no repository knowledge assumed), `READER-DECLARATION.md.tmpl`, `kit.json` (index and source hashes),
`MANIFEST.sha256`, and `tools/`:

| Tool | Audience | Purpose |
| --- | --- | --- |
| `verify_sources.py` | reviewer | Identify the nine downloaded source PDFs by SHA-256 regardless of file name. |
| `check_reading.py` | reviewer | Structure, placeholder and source-hash self-check of one reading. Knows no answers. |
| `rerun_comparison.py` | owner | Re-run the raw comparison of first readings against returned readings, packet by packet. |
| `compare_readings.py` | both | Unmodified copy of `docs/work/plans/graziani-source-review/compare_readings.py`. |

First readings (`reading-a.json`, `reading-b.json`, `*-independent.json`) and prior comparison outputs are never copied.
No scans are included: reviewers download the nine public PDFs themselves, and hashes bind the bytes they read.

## Re-running a comparison

Either with the kit tool:

```sh
python3 tools/rerun_comparison.py --first-readings <dir of NAME.json first readings> \
  --readings <dir of returned NAME.json> --sources-dir <folder of PDFs> [--packet supply] [--full]
```

or with the repository's existing aggregate tool, one override per returned packet:

```sh
python3 -B docs/work/plans/graziani-review-status.py --source ID=PATH ... --reading supply=/path/to/returned.json
```

Exit codes: `0` no issues, `1` differences or missing files, `2` invalid input or source bytes. A raw comparison does not prove
reader independence, source truth, rights or release acceptance.

## Test

```sh
python3 -B -m unittest scripts/graziani-reviewer-kit/test_build_reviewer_kit.py
```

The tests use synthetic inputs and do not need the PDFs. They are not part of the .NET test suite or the CI timing manifest.
Not verified: a run against the real source PDFs (the scans are not stored in this repository).

## Rights note

The row lists and notes derive from a published game and fall under README "Material not licensed under
Apache-2.0". Share the kit only with the reviewers. This is not legal advice.
