# Arrival, supply and repair: blind source review

**Status:** one researcher reading and blind template, not an adopted ruling or
independent review. Fifteen grouped rows bind the facts that drive the recent
arrival/supply timing and repair decisions to three original source packages.
The `land-compiled` file is the original compiled scan, not a new edition; no
independence of the printed material is claimed. The packet omits many applicable
rules, conditions, tables and errata; it is not a complete legal-action contract.

A fresh reviewer should verify source hashes and fill a copy of
`reading-template.json` directly from each named original case without consulting
`reading-a.json` or the working interpretation documents. The locators and exact
case targets are listed in the manifest through IDs and the source key; use the
source-specific reading instructions in the table below, and record full page,
case and row/paragraph locators. Enter `null` for any uncertain value. Booleans
ask only whether the named printed statement is present; they are not a ruling
that another case cannot modify it. The `cw-replacement-scenario` row records the
month statement in its subsection, not a legal conclusion about its full scope.

| Row group | Original pages and cases |
| --- | --- |
| Reinforcement destinations | Land compiled p. 31, §§20.14–20.15. |
| Scenario replacement restriction | Scenario printed p. 28, §60.47. |
| Abstract supply plans and destinations | Land compiled pp. 43–44, §§32.42–32.45. |
| Repair availability and expenditures | Land compiled pp. 34–35, §§22.13, 22.23–22.26, 22.35 and 22.42. |
| Abstract replacement and repair errata | Land compiled p. 43 §32.11; errata PDF p. 2 §§22.34/22.8. |

```sh
python3 -B docs/work/plans/graziani-source-review/compare_readings.py \
  docs/work/plans/graziani-rules-review/manifest.json \
  docs/work/plans/graziani-rules-review/reading-a.json \
  /private/tmp/independent-graziani-rules.json \
  --source land-compiled=/private/tmp/sandtable-full-original.pdf \
  --source scenario=/private/tmp/sandtable-scenarios.pdf \
  --source errata=/private/tmp/sandtable-cna-errata.pdf
```

Retain both entries and all differences. Even full field agreement will not
choose whether §22.13's availability restriction survives §32.11, decide how
pre-scenario plans are initialized, certify all exceptions, establish source
rights, or admit historical rules. Those remain separate versioned decisions.
