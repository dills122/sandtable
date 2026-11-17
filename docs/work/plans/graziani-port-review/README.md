# Port conflict: blind source review

**November 17 independent review:** [entry](reading-independent.json) and
[raw comparison](comparison-independent.json) are retained; see the
[independent review disposition](../2026-11-17-graziani-independent-reviews.md)
for source-specific differences and limits.

**Status:** one source reader and a blank second entry. Seven grouped records
cover the scenario's initial Tobruk value, the port chart, capacity example,
scheduled-reinforcement exception, harbor blockage and clearance, and the
Land-only Commonwealth sea-supply threshold. They do not select an initial
efficiency or establish whether the interaction is reachable in this scenario.

A fresh reader should check the original pages and source hashes, complete the
template without looking at the first entry, and retain exact locators. `null`
means uncertain or unreadable; a boolean records the specified statement, not
the absence of every exception. The chart's general reinforcement statement and
§55.15's scheduled-reinforcement language are separate records. Check the
potential interaction with §20.15 separately before adopting a rule.

```sh
python3 -B docs/work/plans/graziani-source-review/compare_readings.py \
  docs/work/plans/graziani-port-review/manifest.json \
  docs/work/plans/graziani-port-review/reading-a.json \
  /private/tmp/independent-port-reading.json \
  --source scenario=/private/tmp/sandtable-scenarios.pdf \
  --source common-charts=/private/tmp/sandtable-common-charts.pdf \
  --source air-rules=/private/tmp/sandtable-air-rules.pdf \
  --source land-compiled=/private/tmp/sandtable-full-original.pdf
```

Agreement is limited transcription evidence, not resolution of the conflict,
applicable errata, abstract-mode clearance cost, map reachability, rights or
release acceptance. See the [conflict analysis](../2026-11-12-graziani-ruling-decisions.md#open-conflict-tobruk-port-efficiency).
