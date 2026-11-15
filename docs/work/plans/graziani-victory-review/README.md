# Six-turn victory: blind source review

**Status:** first-reader research and blind template, not an independently
reviewed or accepted victory contract. Eight grouped rows target the scenario's
printed cross-reference, named victory heading and six victory categories in
§60.81. They do not choose a winner policy, prove any position reachable,
interpret Land-only supply, normalize map identities or establish rights.

A fresh reviewer should verify the hash-matched original scenario PDF and read
printed pp. 27–28 without consulting `reading-a.json` or the existing victory
matrix. Fill `reading-template.json` in a separate file. The fields ask for the
printed objectives and supply references, not inferred reachability, precedence
or proof of exclusive control. Preserve the source spelling `Sidi Birani` if
that is what is printed; do not silently normalize it to another location. Use
`null` where unreadable. String differences in phrasing or punctuation will be
reported for explicit review; keep both readings and reasoned dispositions.

```sh
python3 -B docs/work/plans/graziani-source-review/compare_readings.py \
  docs/work/plans/graziani-victory-review/manifest.json \
  docs/work/plans/graziani-victory-review/reading-a.json \
  /private/tmp/independent-victory-reading.json \
  --source scenario=/private/tmp/sandtable-scenarios.pdf
```

A match establishes only attributed transcription agreement within this packet.
The source's `as above` references, overlap/both/neither outcomes, timing,
convoy-route semantics and choice of §60.81 for the printed §60.22 cross-reference
require separate reviewed, versioned decisions.
