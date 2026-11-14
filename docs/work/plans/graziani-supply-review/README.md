# Initial supply double-entry packet

**Status:** first entry and blind template; independent entry not yet done. This
packet covers 13 fixed §60.92(B–C) supply rows and two grouped printed
placement/dummy statements. It does not cover trucks, optional loads, map
identity, rule interactions, rights, or scenario content admission. Reading A
is the existing researcher’s transcription, rechecked against the original PDF;
its numerical totals were not used as a substitute for row review. See the
[reconciliation](../2026-11-13-graziani-setup-reconciliation.md).

A fresh reviewer should inspect the hash-matched original scenario PDF printed
pp. 28–29 without consulting `reading-a.json` or the supply pilot, and fill a
copy of `reading-template.json`. Review the source by row, including the printed
mark in `ammo_mark`: enter the literal `—` when the source prints a dash, not
`0` or `null`. `null` means the reader could not determine the printed mark.
`proximity_hexes` records the number in the printed Axis restriction, not an
interpretation of its inclusive boundary. `relation` uses `west-of-matruh` only
if the text supports it. Preserve uncertainty and record all locators.

The template contains the fields and IDs but no first-reader values. It uses the
existing [double-entry comparator](../graziani-source-review/README.md); compare
only after the second reading is complete:

```sh
python3 -B docs/work/plans/graziani-source-review/compare_readings.py \
  docs/work/plans/graziani-supply-review/manifest.json \
  docs/work/plans/graziani-supply-review/reading-a.json \
  /private/tmp/independent-supply-reading.json \
  --source scenario=/private/tmp/sandtable-scenarios.pdf
```

Retain both entries, all differences and a reasoned, source-specific disposition.
A zero-issue comparison alone does not prove independence, source truth, rights,
legal placement or acceptance of a ruling. Different locator wording is reported
for separate review, not silently normalized. No second entry has been made here.
