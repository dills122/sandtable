# Initial truck rows: double-entry packet

**November 17 independent review:** [fresh entry](reading-independent.json)
matches all 96 values; [raw comparison](comparison-independent.json) retains
32 locator-wording differences. See [disposition](../2026-11-17-graziani-independent-reviews.md).
The reader saw the README's disclosure of earlier agreement but not field values.

**November 16 recheck:** [reading B](reading-b.json) and its [raw comparison](comparison-b.json)
match all 96 recorded fields. The reader had prior planning context, so this is
not a blind independent entry; all 32 locator-wording differences and the
independent-review gate remain open. See the [research disposition](../2026-11-16-graziani-seam-supply-review.md#supply-and-truck-row-comparisons).

**Status:** existing researcher's first entry and blind template; no independent
reading. The 32 source rows cover explicitly quantified first-line truck rows in
§§60.31/60.41 and all seven initial second/third-line pools in §§60.33/60.43.
They do not establish complete formation counts, initial allocation legality,
conversion rules, supply choices, later reinforcements, map identity or rights.

A fresh reviewer checks the hash-matched scenario PDF printed p. 27 from source,
without consulting `reading-a.json` or the supply accounting pilot. Fill a copy
of `reading-template.json`: use the printed integer for a listed type; use the
literal `not-listed` if a truck type is absent from that row; use JSON `null`
only when the source cannot be read. Do not use numeric zero to represent an
unprinted type. Supply a specific page, case, location and row locator. The IDs
identify target rows; they do not encode the amounts. Each row records printed
trucks, not inferred motorization points or an initial allocation.

After completing the independent reading, compare using the existing tool:

```sh
python3 -B docs/work/plans/graziani-source-review/compare_readings.py \
  docs/work/plans/graziani-truck-review/manifest.json \
  docs/work/plans/graziani-truck-review/reading-a.json \
  /private/tmp/independent-truck-reading.json \
  --source scenario=/private/tmp/sandtable-scenarios.pdf
```

Retain both entries and all differences, including locator differences. Agreement
does not settle rules applicability, source independence, distribution rights or
any scenario admission decision. The first entry was rechecked in this research
stream and must not be described as an independent review.
