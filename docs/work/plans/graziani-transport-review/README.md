# Abstract supply transport: blind source review

**Status:** one-reader research and blank second entry, not accepted rules or a
complete transport model. Nine grouped records cover §32.14 and §§32.31–32.37
in the Land rules and §59.66(C) in the scenario book. Inspect the original cases
and their conditions rather than using the first reading or transport matrix.
The `off-map` field records the printed wording; it does not decide whether that
wording is a typo or what limit applies to ordinary on-map hexes.

A fresh reviewer should verify the source hashes, independently fill
`reading-template.json` and retain exact page/case locators. `null` means an
unreadable or uncertain field; booleans record presence of a printed statement,
not a legal determination that no exception exists. `owner_phase_choice` and
`ports` are compact transcriptions; retain source differences for disposition.
Do not infer that abstract air transport is absent merely because the detailed
Air Game is omitted, or that coastal shipping and strategic convoys share a
capacity rule.

```sh
python3 -B docs/work/plans/graziani-source-review/compare_readings.py \
  docs/work/plans/graziani-transport-review/manifest.json \
  docs/work/plans/graziani-transport-review/reading-a.json \
  /private/tmp/independent-transport-reading.json \
  --source land-compiled=/private/tmp/sandtable-full-original.pdf \
  --source scenario=/private/tmp/sandtable-scenarios.pdf
```

Agreement is scoped transcription evidence only. It does not settle errata,
map/port capacity, source conflicts, actual six-turn reachability, privacy,
rights or implementation admission.
