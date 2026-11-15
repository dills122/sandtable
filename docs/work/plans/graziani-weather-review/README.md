# Weather: blind source review

**Status:** one reader and a blank second entry. Nine grouped records cover the
Map D calendar, season rule, printed fall row and erratum, and selected weather
effects. They do not establish a full historical weather implementation, map
subareas, application of abstract supply or all applicable weather rules.

An independent reader should inspect the original track, table, cases and
erratum without the first entry, verify hashes, complete the template and retain
locators. `null` means uncertain or unreadable. Record the chart's printed
turn bands separately from §29.1 and the correction; do not silently overwrite
the printed chart with the existing Sandtable ruling.

```sh
python3 -B docs/work/plans/graziani-source-review/compare_readings.py \
  docs/work/plans/graziani-weather-review/manifest.json \
  docs/work/plans/graziani-weather-review/reading-a.json \
  /private/tmp/independent-weather-reading.json \
  --source land-compiled=/private/tmp/sandtable-full-original.pdf \
  --source common-charts=/private/tmp/sandtable-common-charts.pdf \
  --source errata=/private/tmp/sandtable-cna-errata.pdf \
  --source maps=/private/tmp/sandtable-maps.pdf
```

Agreement establishes only scoped transcription evidence. It does not approve
the season ruling, resolve Land-only fuel loss, establish map rights or make a
scenario playable. See [weather scope](../2026-11-12-graziani-weather-scope.md).
