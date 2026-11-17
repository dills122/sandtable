# Six-turn Commonwealth arrivals: double-entry packet

**November 17 independent review:** fresh [group](reading-independent.json)
and [member](member-reading-independent.json) entries and their raw
[group](comparison-independent.json) and [member](member-comparison-independent.json)
comparisons are retained. See [disposition](../2026-11-17-graziani-independent-reviews.md)
for locator and weapon-mark formatting differences and remaining scope.

**Status:** one reading plus blind template; no independent entry for this whole
manifest. It targets six schedule rows and eight grouped OA checks covering 21
candidate printed OA rows. The earlier independent pilot already checked the
bounded NZ schedule and OA conflict; that does not constitute independent review
of the other entries or acceptance of the 6th NZ ruling.

A fresh reviewer should hash and visually inspect the original Commonwealth
charts, without consulting `reading-a.json` or the arrival pilot. Schedule rows
are on PDF p. 3; OA target pages are 11, 15, 18 and 21. Schedule columns print
turn then operation stage. OA markers print **stage/turn**; enter them as named
`turn` and `stage` fields, not in printed order. Count only the target's printed
OA constituent rows; do not infer a legal force, strengths, deployment, training
or attachments from that count. For trucks, `not-listed` means no figure is
printed on that schedule row; it does not mean no trucks exist elsewhere. Use
`null` when the source cannot be read. Keep a page, case, formation and row
locator for each entry.

After finishing the independent reading:

```sh
python3 -B docs/work/plans/graziani-source-review/compare_readings.py \
  docs/work/plans/graziani-arrival-review/manifest.json \
  docs/work/plans/graziani-arrival-review/reading-a.json \
  /private/tmp/independent-arrival-reading.json \
  --source commonwealth-charts=/private/tmp/sandtable-cw-charts.pdf
```

Preserve all differing locators, missing fields, unknowns and discrepancies with
sourced dispositions. A successful comparison does not establish source rights,
independent editions, complete scenario forces, source precedence, choice
legality, or a versioned ruling. The conflicting Land §20.11 example and
scenario §60.47 must be reconciled separately; they are not hidden by a match
between schedule and OA in this manifest.

For the first reading, a separate arithmetic/join check verifies the source hash,
manifest binding, six unique turn rows, OA-to-schedule positions, counts and
explicitly listed truck subtotals. It does not compare two readings or verify
source transcription:

```sh
python3 -B docs/work/plans/graziani-arrival-review/verify_crosswalk.py \
  docs/work/plans/graziani-arrival-review/manifest.json \
  docs/work/plans/graziani-arrival-review/reading-a.json \
  /private/tmp/sandtable-cw-charts.pdf
python3 -B -m unittest discover -s docs/work/plans/graziani-arrival-review -p 'test_*.py' -v
```

## Per-member follow-up

`member-manifest.json`, `member-reading-a.json` and
`member-reading-template.json` enumerate 21 **candidate printed OA rows** for a
separate field-level check. They preserve the chart's counter abbreviation,
ID code, TO&E/weapon marker and printed arrival notation; research IDs are not
admitted content identities. A fresh reader should fill the member template from
OA PDF pp. 11, 15, 18 and 21 without consulting the first entry. Do not interpret
`N`, `U@1`, weapon counts or arrival marks as an accepted setup schema. Exact
marking and punctuation differences remain review issues, not automatic errors
in either reader's account.

```sh
python3 -B docs/work/plans/graziani-source-review/compare_readings.py \
  docs/work/plans/graziani-arrival-review/member-manifest.json \
  docs/work/plans/graziani-arrival-review/member-reading-a.json \
  /private/tmp/independent-arrival-members.json \
  --source commonwealth-charts=/private/tmp/sandtable-cw-charts.pdf
```

The bounded 6th NZ pilot reviewed timing and grouped count, not every individual
field above. No second member reading has been supplied.

A separate local check joins the candidate individual rows to the grouped
first reading and rejects missing/duplicate members, count mismatches and OA
markers interpreted in the wrong order:

```sh
python3 -B docs/work/plans/graziani-arrival-review/verify_members.py \
  docs/work/plans/graziani-arrival-review/manifest.json \
  docs/work/plans/graziani-arrival-review/reading-a.json \
  docs/work/plans/graziani-arrival-review/member-manifest.json \
  docs/work/plans/graziani-arrival-review/member-reading-a.json \
  /private/tmp/sandtable-cw-charts.pdf
```

It reports the source byte hash and canonical JSON hashes of the manifests and
readings. It is an internal consistency check of one research stream, not an
independent transcription, source validation or approval of the 21 identities.
