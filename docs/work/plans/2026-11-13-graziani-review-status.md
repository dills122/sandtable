# Graziani source-review packet status

The [status tool](graziani-review-status.py) reports the raw comparator result
for seventeen bounded packets. It checks each actual source file's recorded SHA-256,
manifest binding, attributed readings and all comparator issues. It accepts
explicit second-reading overrides without editing the templates. It does not
establish reader independence, source truth, rights, completeness, accepted
rulings or release readiness.

At the November 13 working-tree snapshot, the seventeen manifests contain **212
grouped rows and 666 fields**, with significant overlap between packets. Sixteen
packets have only a blank second-reading template; their counts are not a measure
of complete scenario coverage or a number of unsupported mechanics. The earlier
11-row Matruh/NZ/formation pilot has a fresh-context agent reading. Its raw
comparator still reports 11 differently worded locators; their manual
[disposition](graziani-source-review/review-disposition.md) is separate, and the
status tool deliberately does not convert it to an automatic pass.

## Review order

Use fresh source readings and the packet templates; do not copy the first
entry. Counts are grouped records and fields, not distinct source rules.

| Order | Packets | Rows / fields | Why this order |
| --- | --- | ---: | --- |
| Existing pilot | [Matruh, NZ and formation pilot](graziani-source-review/README.md) | 11 / 34 | Attributed independent-agent entry exists; review its retained locator disposition and unresolved rulings. |
| 1 | [Supply](graziani-supply-review/README.md), [trucks](graziani-truck-review/README.md) | 47 / 146 | Initial quantities and allocations are prerequisites for setup admission. |
| 2 | [Arrival groups and members](graziani-arrival-review/README.md), [Axis members and relationships](graziani-axis-formation-review/README.md), [French Motor Marine company](graziani-marine-review/README.md) | 72 / 282 | Reconcile timing, unit identities and formation relationships before admitting arrivals or setup. |
| 3 | [Repair/arrival rules](graziani-rules-review/README.md), [victory](graziani-victory-review/README.md), [ports](graziani-port-review/README.md), [fortifications](graziani-fortification-review/README.md) | 33 / 79 | Record conflicting source text and applicability before accepting scenario rulings. |
| 4 | [Transport](graziani-transport-review/README.md), [convoys](graziani-convoy-review/README.md), [rail](graziani-rail-review/README.md) | 30 / 82 | Establish reachable movement, plans, resource and loss behavior; retain source-specific exceptions. |
| 5 | [Weather](graziani-weather-review/README.md), [patrol](graziani-patrol-review/README.md) | 19 / 43 | Connect stage effects and limited disclosure to accepted map and visibility contracts. |

These are review priorities, not hard serial dependencies: an independent reader
may work on a later packet while setup review is pending. A matching transcription
does not itself settle the Matruh, NZ, repair, port, victory or source-rights
decisions. The sixteen new packets still have blank second entries.

## Reacquiring source files

The `/private/tmp` paths below are temporary, not repository artifacts. The
packet manifests retain each expected SHA-256; the status tool rejects changed
bytes. A matching hash identifies a package, not its licensing, authority,
independent edition or chain of custody.

| Source ID | Retained external package |
| --- | --- |
| `scenario` | [Scenario booklet](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf) |
| `errata` | [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf) |
| `land-rules` | [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) |
| `land-compiled` | [Complete archival package](https://spigames.net/PDFv2/CampaignNorthAfrica.pdf) |
| `air-rules` | [Air and Logistics rules](https://spigames.net/PDFv10/CNA_AirGameRules.pdf) |
| `common-charts` | [Common charts](https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf) |
| `commonwealth-charts` | [Commonwealth charts](https://spigames.net/PDFv10/CNA_ChartsCWPlayer.pdf) |
| `axis-charts` | [Axis charts](https://spigames.net/PDFv10/CNA_ChartsAxisPlayer.pdf) |
| `maps` | [Five map sheets](https://spigames.net/PDFv10/CNA_Maps.pdf) |

The Land pages in the separate and compiled packages were checked as the same
scan for the bounded §20.11 example; they are not independent readings. Source
redistribution and derived-content rights remain open.

Run with the actual source files, adapting paths as needed:

```sh
python3 -B docs/work/plans/graziani-review-status.py \
  --source scenario=/private/tmp/sandtable-scenarios.pdf \
  --source errata=/private/tmp/sandtable-cna-errata.pdf \
  --source commonwealth-charts=/private/tmp/sandtable-cw-charts.pdf \
  --source land-rules=/private/tmp/sandtable-land-rules.pdf \
  --source land-compiled=/private/tmp/sandtable-full-original.pdf \
  --source common-charts=/private/tmp/sandtable-common-charts.pdf \
  --source air-rules=/private/tmp/sandtable-air-rules.pdf \
  --source axis-charts=/private/tmp/sandtable-axis-charts.pdf \
  --source maps=/private/tmp/sandtable-maps.pdf
```

The current run intentionally exits `1`: blank readings and the pilot's raw
locator differences are reported. To evaluate a new second entry, add for
example `--reading supply=/path/to/reading.json`; the reader's claimed identity
is not proof of independence. Exit `0` means only that all supplied raw
comparisons have no issues; exit `2` means invalid input or source bytes. Preserve
source conflict and rights decisions even if every raw comparison eventually
matches. No source scan is stored in Git.

Run the local review-tool tests without needing the scans (Unix shell):

```sh
PYTHONPATH=docs/work/plans/graziani-source-review:docs/work/plans/graziani-arrival-review:docs/work/plans/graziani-axis-formation-review \
  python3 -B -m unittest \
  docs/work/plans/graziani-source-review/test_compare_readings.py \
  docs/work/plans/graziani-arrival-review/test_verify_crosswalk.py \
  docs/work/plans/graziani-arrival-review/test_verify_members.py \
  docs/work/plans/graziani-axis-formation-review/test_verify_relationships.py \
  docs/work/plans/test_graziani_review_status.py \
  docs/work/plans/test_verify_graziani_register.py
```

Those tests use synthetic inputs and do not substitute for the source-byte run.
