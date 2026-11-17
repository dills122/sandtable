# French Motor Marine company: blind source review

**November 17 independent review:** [fresh entry](reading-independent.json)
and [raw comparison](comparison-independent.json) are retained. See
[disposition](../2026-11-17-graziani-independent-reviews.md) for differing
locators, quote marks and deployment wording; later identity remains open.

**Status:** one reader and a blank second entry. Four grouped records cover the
company's original OA entry, its errata correction, the deployed-marker
definition and the scenario setup entry. They do not establish that every
similarly named formation or counter is the same unit.

Independently inspect the cited scans without using the first entry, verify the
hashes and complete the template with exact locators. `null` means unreadable or
uncertain. The original printed marker and its correction are separate fields;
do not replace the printed value before comparison.

```sh
python3 -B docs/work/plans/graziani-source-review/compare_readings.py \
  docs/work/plans/graziani-marine-review/manifest.json \
  docs/work/plans/graziani-marine-review/reading-a.json \
  /private/tmp/independent-marine-reading.json \
  --source scenario=/private/tmp/sandtable-scenarios.pdf \
  --source commonwealth-charts=/private/tmp/sandtable-cw-charts.pdf \
  --source errata=/private/tmp/sandtable-cna-errata.pdf \
  --source land-rules=/private/tmp/sandtable-land-rules.pdf
```

See the [arrival analysis](../2026-11-12-graziani-arrival-scope.md). Matching
entries do not establish complete scenario setup, rights or release acceptance.
