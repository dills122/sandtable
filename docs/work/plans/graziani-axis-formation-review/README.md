# Cirene and Marmarica: OA-member review packet

**Status:** existing researcher's reading and blind template, not independent
field-level verification or admitted content. The [bounded earlier review](../graziani-source-review/review-disposition.md)
checked grouped formation row counts, deployed markers and tank notes; it did
not independently validate every counter abbreviation, ID code or equipment mark.
The new packet covers 15 candidate Cirene OA rows and 14 Marmarica rows in the
September errata, PDF p. 4. It does not assert all listed elements are separate
map counters, resolve other formation tables, or clear distribution rights.

A fresh reviewer should retrieve the original errata PDF, verify SHA-256 against
the manifest and independently fill a copy of `reading-template.json` without
consulting `reading-a.json`. Enter printed counter abbreviations, ID codes,
TO&E/weapon marks and arrival marks as text. For a blank field use `not-listed`;
use JSON `null` for an unreadable mark. The two tank rows additionally record
the OA note's stated parent and starting attachment; these are distinct
relationships. Mark uncertain or abbreviated interpretations explicitly and
record exact page, chart and row locators. The displayed group and unit IDs are
research labels, not final content IDs.

```sh
python3 -B docs/work/plans/graziani-source-review/compare_readings.py \
  docs/work/plans/graziani-axis-formation-review/manifest.json \
  docs/work/plans/graziani-axis-formation-review/reading-a.json \
  /private/tmp/independent-axis-members.json \
  --source errata=/private/tmp/sandtable-cna-errata.pdf
```

Preserve source wording/mark differences and all dispositions. Comparison of
same-edition records does not establish independently published evidence, legal
formation structure, scenario placement, Land-only equipment handling, rights
or acceptance. The tank is printed on an OA and named as an attachment in the
scenario; it must remain one underlying unit rather than being added twice.
Sirte is outside this bounded member packet and its later arrival is a separate
source question.

## Scenario-to-OA relationship check

A separate four-row `relationship-manifest.json` targets the scenario's Cirene
and Marmarica placement/attachment rows and the two errata OA tank rows/notes.
It preserves the scenario's `Ar/LTC` marker separately from the OA note's
spelled-out parent and attached formation. A fresh reader should complete
`relationship-reading-template.json` directly from scenario printed p. 27 and
errata PDF p. 4. It is not enough to check only matching tank abbreviations:
the independent reading must also distinguish parent assignment from current
attachment and must not count a repeated mention as an additional unit.

```sh
python3 -B docs/work/plans/graziani-source-review/compare_readings.py \
  docs/work/plans/graziani-axis-formation-review/relationship-manifest.json \
  docs/work/plans/graziani-axis-formation-review/relationship-reading-a.json \
  /private/tmp/independent-axis-relationships.json \
  --source scenario=/private/tmp/sandtable-scenarios.pdf \
  --source errata=/private/tmp/sandtable-cna-errata.pdf
```

The earlier independent pilot checked the errata's grouped tank notes, not this
whole two-source relationship packet. No independent entry exists for this
manifest; any ambiguity remains open rather than being encoded as authority.

The local consistency check verifies the exact bounded member scope and joins
the two tank mentions across both first readings and hash-matched source files.
The uniqueness check applies only to these candidate rows; it is not a general
proof that counter abbreviations uniquely identify units across the game.

```sh
python3 -B docs/work/plans/graziani-axis-formation-review/verify_relationships.py \
  docs/work/plans/graziani-axis-formation-review/manifest.json \
  docs/work/plans/graziani-axis-formation-review/reading-a.json \
  docs/work/plans/graziani-axis-formation-review/relationship-manifest.json \
  docs/work/plans/graziani-axis-formation-review/relationship-reading-a.json \
  /private/tmp/sandtable-scenarios.pdf /private/tmp/sandtable-cna-errata.pdf
python3 -B -m unittest discover -s docs/work/plans/graziani-axis-formation-review -p 'test_*.py' -v
```

This is a same-reader join; it does not interpret `Ar/LTC`, certify that all
identities and relationships are correct, or replace independent source and
rules review. Output includes canonical input hashes and actual source hashes.
