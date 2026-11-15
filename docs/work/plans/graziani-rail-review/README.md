# Rail rules: blind source review

**Status:** one reader and a blank second entry. Ten grouped records cover
scenario railhead, Land rail, errata, construction, detailed Axis rail and
simplified supply rail. They do not decide Land-only applicability, resource
costs, phase, physical topology or actual six-turn reachability.

A fresh reader should inspect each original case and correction without the first
entry, verify the source hashes, complete the template and retain precise page
locators. `null` means uncertain or unreadable; a boolean records only the
statement named by that field, not absence of other exceptions. Keep the
corrected Axis eligibility distinct from the separate historical Barce line.

```sh
python3 -B docs/work/plans/graziani-source-review/compare_readings.py \
  docs/work/plans/graziani-rail-review/manifest.json \
  docs/work/plans/graziani-rail-review/reading-a.json \
  /private/tmp/independent-rail-reading.json \
  --source scenario=/private/tmp/sandtable-scenarios.pdf \
  --source land-compiled=/private/tmp/sandtable-full-original.pdf \
  --source air-rules=/private/tmp/sandtable-air-rules.pdf \
  --source errata=/private/tmp/sandtable-cna-errata.pdf
```

Agreement is limited transcription evidence, not a ruling, rights decision or
release acceptance. [Rail scope](../2026-11-13-graziani-rail-scope.md) lists
the remaining choice, construction and resource questions.
