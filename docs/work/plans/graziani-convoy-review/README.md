# Simplified Axis convoy: blind source review

**Status:** one reader and a blank second entry. Eleven grouped records cover the
initial scenario restriction, cargo, lane limits, attack, table input, loss
rounding, qualifying-division definition, one 5% chart cell and the detailed
Air rule plus erratum. They do not transcribe the full
charts, resolve unclear minima, or establish six-turn reachability.

A fresh reader should inspect the original cases without the first entry,
verify the hashes, fill the template and retain exact locators. `null` means
uncertain or unreadable. Separate the source's 10% rounding rule from the 20%
minimum instruction; an asserted instruction is not yet an interpretation of
its effect on empty cargo categories. Check corrections and the separate
bombardment results table before any runtime admission.

```sh
python3 -B docs/work/plans/graziani-source-review/compare_readings.py \
  docs/work/plans/graziani-convoy-review/manifest.json \
  docs/work/plans/graziani-convoy-review/reading-a.json \
  /private/tmp/independent-convoy-reading.json \
  --source scenario=/private/tmp/sandtable-scenarios.pdf \
  --source axis-charts=/private/tmp/sandtable-axis-charts.pdf \
  --source land-compiled=/private/tmp/sandtable-full-original.pdf \
  --source common-charts=/private/tmp/sandtable-common-charts.pdf \
  --source air-rules=/private/tmp/sandtable-air-rules.pdf \
  --source errata=/private/tmp/sandtable-cna-errata.pdf
```

Agreement is scoped transcription evidence, not an accepted ruling, provenance
approval or a playable scenario. The [convoy matrix](../2026-11-13-graziani-abstract-convoy-matrix.md)
records conditional arithmetic and open integration questions.
