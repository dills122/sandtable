# Graziani: CNA Living Rules evidence lead

Research snapshot November 17, 2026. [CNA Living Rules](https://basmith.net/cna/) is a separate, unaffiliated, provisional restatement. Its [repository](https://github.com/basmith7/cna) says map data derives from a community VASSAL redraw checked against an SPI scan; its scenario data cites SPI scans. These are useful independent *analysis and transcription leads*, not independent primary editions or automatic confirmation of source readings. No data or art imported; no upstream revision was pinned during this web-only examination, so inspect and pin the exact commit before comparison.

## Available artifacts and provenance

- [Map and data conventions](https://basmith.net/cna/data/) describe `data/map/raw/<sheet>.json` as extraction from Mitch Guthrie's VASSAL redraw, `corrections/M-nnn.json` as scan-cited hand fixes, `hexes.json` and `hexsides.json` as generated output, `places.json` and `regions.json`, plus `sheets.json` and generated `seams.json`. The [map page](https://basmith.net/cna/map) renders A–E and Malta from this data. The project states no scanned map art is published. Access to individual raw files or their history was not verified in this pass; the published conventions describe their intended contents. A later [geometry comparison](2026-11-17-graziani-seam-geometry-comparison.md) pins a repository commit and inspects its seam generator.
- The same conventions describe `data/oa/it.json`, `data/scenarios/grazianis-offensive.json`, `data/tables/port-capacity.json` and errata overlays. The [scenario JSON](https://raw.githubusercontent.com/basmith7/cna/main/data/scenarios/grazianis-offensive.json) was accessible and cites §60 and scan pages 76–79. Its first CCNN HQ placement is C4218 with an attached I(M) tank battalion; that placement is not evidence of the HQ's weapon or gun strength. Italian OA file could not be opened in this pass; do not infer its contents for the missing HQ weapon. Independently inspect the exact OA record and its references against the [known source gap](2026-11-17-graziani-remaining-source-research.md#1-1st-ccnn-hq-artillery--source-lookup-completed-with-a-confirmed-gap).
- The project [offers](https://github.com/basmith7/cna) CC0 for `data/`, CC-BY-SA-4.0 for rules/rulings and MIT for code, only to the extent it holds rights. This is not proof of underlying SPI/successor clearance; see [rights review](2026-11-17-graziani-rights-independent.md). No import or legal clearance follows from this lead.

## Specific comparisons

| Question | Lead and limit | Next check |
| --- | --- | --- |
| Sheet seams | [Conventions](https://basmith.net/cna/data/) state A–E abut in west-to-east order, column 33 to column 01 on equal rows, with extra diagonals for rows not shifted west; `seams.json` combines paired edge features. This is an explicit external geometric interpretation. It does not itself establish each physical sheet join, printed feature continuity, movement legality, or reconcile Sandtable's distance-chart contradictions. | Pin upstream revision; examine generator, sheet bounds and edge records. Compare A/B, B/C, C/D and D/E pairs and feature evidence against [independent edge observations](2026-11-17-graziani-seam-ab-independent.md), [B/C](2026-11-17-graziani-seam-bc-independent.md), and original sheet edges, including whether column 34 is represented. Record disagreements before any seam ruling. |
| Hex terrain and locations | Site says terrain read from VASSAL redraw and checked against SPI, with each manual correction citing scan/crop. This is a different transcription pipeline but shares underlying SPI material; uncorrected extraction is not evidence of an independent scan verification of every cell. [Scenario data](https://raw.githubusercontent.com/basmith7/cna/main/data/scenarios/grazianis-offensive.json) explicitly notes printed Benghazi B4827 versus `places.json` A4827, supporting further examination of that discrepancy, not an automatic source correction. | Compare targeted cells, metadata and correction `seen` citations with original scans, retaining differing interpretations and missing cells. Review completeness, exclusions, and per-edge direction before using as movement evidence. |
| Tobruk efficiency | [Port-capacity page](https://basmith.net/cna/tables/port-capacity) reports chart maximum 5, checks two scans cell-by-cell, and cites §55.12; [R-027](https://basmith.net/cna/rulings/R-027) interprets three lost levels as starting 2, overriding scenario 7. This is a separately published interpretation consistent with the chart reading, not an official erratum or independent edition; same original print underlies both scans. | Compare scans, footnote and §55.25 with [open conflict](2026-11-12-graziani-ruling-decisions.md#open-conflict-tobruk-port-efficiency); record ruling as external evidence, retain local decision gate. |
| 1st CCNN HQ weapons | Scenario JSON identifies the HQ and deployment but provides no weapon/initial gun-strength resolution in the inspected record. OA data advertised but inaccessible in this pass. | Inspect pinned `data/oa/it.json`, chart citations and any related ruling; distinguish organisational equipment, initial deployment strength and source-specific uncertainty. Keep unknown until independently supported. |

## Follow-up: Italian OA record

On November 17, the GitHub contents connector retrieved
[`data/oa/it.json`](https://github.com/basmith7/cna/blob/main/data/oa/it.json)
with content blob SHA `80ebe0c37bd68f21b431ee3f6ff7b7587604d6b5` (blob identity,
not a pinned repository commit). Around line 7357, its 1st CCNN division HQ
record gives ID `g`, deployed, `toe_mark` `N`, and an empty `toe` array; the
formation cites `scan:p159`. This gives no identified gun system or starting
gun strength. The empty array alone does not establish a zero-gun ruling or
independent source; its schema and conventions would need investigation.
No data was imported. The earlier inaccessible-file note describes the first
web-only pass, superseded by this targeted connector retrieval.

Other relevant upstream decisions include [R-002 road stubs and R-110 Land-only victory supply](https://basmith.net/cna/rulings/register). Their accepted status is upstream project policy, not Sandtable approval. Check exact text and provenance before using them in local reviews.
