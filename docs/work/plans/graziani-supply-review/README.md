# Initial supply double-entry packet

**November 17 independent review:** [fresh entry](reading-independent.json)
matches all 50 values; [raw comparison](comparison-independent.json) retains
15 locator-wording differences. See [disposition](../2026-11-17-graziani-independent-reviews.md).
The reader saw the README's disclosure of earlier agreement but not field values.

**November 16 recheck:** [reading B](reading-b.json) and its [raw comparison](comparison-b.json)
match all 50 recorded fields. The reader had prior planning context, so this is
not a blind independent entry; all 15 locator-wording differences and the
independent-review gate remain open. See the [research disposition](../2026-11-16-graziani-seam-supply-review.md#supply-and-truck-row-comparisons).

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
for separate review, not silently normalized. Reading B is a contextual recheck;
a blind independent entry is still required.
