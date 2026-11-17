# Initial fortifications: blind source review

**November 17 independent review:** [entry](reading-independent.json) and
[raw comparison](comparison-independent.json) are retained; see the
[independent review disposition](../2026-11-17-graziani-independent-reviews.md)
for source-specific differences and limits.

**Status:** one reader and a blank second entry. Three grouped records cover the
scenario's no-fortification statement and the general rule for major-city
fortifications. They do not determine which provision takes precedence, whether
all city hexes are relevant or how Combat applies the chosen interpretation.

Independently inspect the original sources and applicable errata without using
the first entry, verify their hashes, fill the template and preserve locators.
`null` means unreadable or uncertain. A source agreement is not a ruling.

```sh
python3 -B docs/work/plans/graziani-source-review/compare_readings.py \
  docs/work/plans/graziani-fortification-review/manifest.json \
  docs/work/plans/graziani-fortification-review/reading-a.json \
  /private/tmp/independent-fortification-reading.json \
  --source scenario=/private/tmp/sandtable-scenarios.pdf \
  --source land-compiled=/private/tmp/sandtable-full-original.pdf
```

See [conflict analysis](../2026-11-12-graziani-ruling-decisions.md#open-interaction-starting-city-fortifications).
Source truth, rights, reachability and release acceptance remain separate.
