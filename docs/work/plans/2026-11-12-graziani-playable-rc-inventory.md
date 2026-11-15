# Graziani playable RC: inventory and execution map

**Status:** working inventory; `MVP-GATE-01` remains open. Baseline `0f8a828a`.
The [initial source register](2026-11-12-graziani-source-register.md) expands turn,
initiative, Land-only, victory and repair rows. [Working source decisions](2026-11-12-graziani-ruling-decisions.md)
select interpretations for Matruh and the printed victory cross-reference; independent
review and versioned acceptance remain open, as does the distinct Tobruk conflict.
Later working choices cover Matruh assignment and 6th NZ Brigade arrival; their
setup and legal-action interactions remain unresolved.
The [logistics crosswalk](2026-11-12-graziani-abstract-logistics-crosswalk.md),
[supply accounting pilot](2026-11-12-graziani-supply-accounting-pilot.md),
[arrival pilot](2026-11-12-graziani-arrival-scope.md), and
[victory matrix](2026-11-12-graziani-victory-matrix.md) detail bounded source work.
The [Land sequence crosswalk](2026-11-13-graziani-land-sequence-crosswalk.md)
maps the original outline to catalogue boundaries and flags unresolved actions.
The [weather scope](2026-11-12-graziani-weather-scope.md) separates the existing
synthetic capability from fuel and map effects needed by the historical setup.
The [map identity pilot](2026-11-12-graziani-map-identity-pilot.md) tracks
objective locations and a conflicting initial-supply coordinate.
The [formation setup analysis](2026-11-12-graziani-formation-setup-conflicts.md)
tracks Matruh relationship wording and the duplicated corps-artillery entry.
The [setup reconciliation](2026-11-13-graziani-setup-reconciliation.md) records
remaining Matruh, arrival and supply decisions. Blind second-entry packets now
cover [supply rows](graziani-supply-review/README.md) and [truck rows](graziani-truck-review/README.md);
independent entries are outstanding.
The [arrival authority handoff](2026-11-12-graziani-arrival-authority-handoff.md)
records existing synthetic limits and the next contract-first slice.
This is a planning crosswalk, not source transcription, an independent source check,
an implementation certificate, or a claim that the scenario is playable. The
[source-surface inventory](../../research/2026-10-18-graziani-six-turn-surface-inventory.md)
retains the source readings and corrections; the [roadmap](../../roadmap/pre-alpha-roadmap.md)
owns gate dependencies. The October 5 release audit is historical: its capability
rows predate later native Combat, local journal and Maproom work.

## Known scope and counting rules

The retained source reading identifies six turns, 18 operation stages, 36 side
activation blocks, Italian first-turn initiative and six victory categories. Setup
records, reachable rules/table outcomes, scenario map and arrival counts remain
unknown. The [scoped register counter](verify-graziani-register.py) currently
finds 67 research rows, all with outstanding evidence; its test verifies the
counting boundary, not source completeness. No exact unsupported-item count is
justified yet. Count distinct sourced
obligations separately from stage occurrences, actions, table coordinates and
player decisions. A repeated Combat/Movement cycle is not one fixed catalogue row.

The following is a **catalogue inventory**, not a source-verified reachability or
runtime support matrix. Counts derive from `CreateTurn`, `AddOperationPrelude` and
`AddPlayerPhase` in [Cna1979LandSequence](../../../src/Cna.Core/Rules/Cna1979LandSequence.cs).
All rows still need a source/errata anchor and a six-turn reachability disposition;
catalogue entries alone do not establish an applicable mechanic or legal no-op.

| Scope | Catalogue position families | Occurrences over six turns |
| --- | --- | ---: |
| Turn opening | Initiative determination; naval-convoy scheduling; tactical shipping | 18 |
| Stage prelude | Initiative declaration; weather; organization; naval-convoy arrival; Commonwealth fleet assignment and repair | 108 |
| Side block | Reserve designation; Movement; Breakdown; six Combat steps; Reserve Release; truck convoy; Commonwealth rail; towing; maintenance; patrol | 540 |
| Turn end | End-of-turn | 6 |
| Total | 112 positions per turn × 6 | **672** |

Each of 36 side blocks contains 15 catalogue positions: 1 Reserve, 1 Movement,
1 Breakdown, 6 Combat, 1 Reserve Release, 1 truck convoy, 1 rail, 2 repair and
1 patrol. The catalogue includes both player-order roles; do not assume each is
executable for both sides or applicable in Land-only mode. The V4 checkpoint
boundary ends at the first acting side's stage-one Combat position; the later
contact-based native path is separate bounded evidence, not a six-turn certificate.

## Obligation register: current evidence and next proof

| ID | Family and source anchor | Current evidence | Missing proof / next action |
| --- | --- | --- | --- |
| G01 | Turn, stage and initiative: scenario §§60.22, 60.6; Land §§5.2, 7 | Duration and initial holder recorded; typed sequence exists. | Cross-check every turn/stage transition, later initiative outcomes and actor ordering against source; prove end boundary. |
| G02 | Both setups and formations: scenario §§59.2, 60.31, 60.41; errata §4.44b | Preserved errata PDF p. 4 contains Cirene, Marmarica and Sirte OA scans; initial attachments and Sirte's later schedule agree with other source records. Source data not admitted. | Independently reconcile scan rows, scenario exceptions, corrections, setup choices and totals; review provenance and rights. |
| G03 | Map, terrain and facilities: scenario §§60.5, 60.7; map-D errata | Relevant families identified; no scenario topology count. | Inventory required locations, edges and facilities with sources; assess erratum reachability and choices; independently check topology. |
| G04 | Trucks, initial and ongoing supply: scenario §§59.4–59.6, 60.33, 60.43, 60.92; Land §§32, 57 | Land-only initial-supply replacement and truck choices identified. | Reconcile allocations, loads, supply routes/consumption and conservation; apply errata; enumerate decisions and reachable outcomes. Avoid duplicate initial supply. |
| G05 | Repair and arrivals: scenario §§60.33, 60.37, 60.44, 60.47; Land §§8.86, 22.31 | Original §22.31 separately identifies major facilities; RUL-GRA-01 provisionally retains the temporary Mersa Matruh facility. Six Commonwealth arrival positions were identified. | Independently review and version the working ruling; extract arrival records and prerequisites; reconcile facility capacity, repair/towing and arrival-stage movement. |
| G06 | Land-only air, naval and logistics abstraction: scenario §§60.91–60.93; errata §§32.0, 60.45 | Abstraction exists; errata invalidate later §60.45 fleet paragraphs. | Classify each retained abstract effect, excluded detailed input and reachable decision; do not assume abstraction means no operation. |
| G07 | Stage/side actions, Movement and Combat: Land §5.2 and referenced rules/charts | Bounded Movement and contact Combat paths, timeouts and recovery exist; public Combat and Runner gates remain open. | Map each source obligation and random/table outcome to applicable runtime, explicit unsupported behavior, or source-proven empty case; include repeat loops and both owners. Certify original P01/public paths separately. |
| G08 | Termination/victory: scenario §§60.22, 60.81; supply rules | Six outcome categories and stage boundary visually checked; RUL-GRA-02 provisionally selects §60.81 for the printed reference; no scenario victory acceptance. | Independently review the working correction; settle timing, precedence, hold/location and supply-route semantics; freeze deterministic event and check all boundary cases. |
| G09 | Fog, replay and local play: project FOG/RPL and MVP gates | Side-safe bounded paths and SQLite journal/cold replay exist; local Maproom read preview exists. | Inventory setup/decision disclosure, handoff, stale actions, replay cuts and durable create/save/resume across the admitted scenario; prove browser privacy/accessibility and no-model operation. |
| G10 | Source provenance, rights and delivery: source hierarchy; MVP-GATE-02/07 | Source scans kept outside Git; distribution rights unresolved. | Document source/errata manifests and derived-data rights posture; determine supported platforms, notices, packaging and fresh-machine smoke. |

These ten rows are **planning families**, not a count of unsupported rules. Some
contain implemented bounded slices and unresolved source, contract and runtime work.
The source record, applicable circumstances, exact authority/version, test and
review evidence must be linked before assigning a finer-grained status.

## Work packets and dependencies

1. **Inventory baseline:** build a row-level register from G01–G10. For every
   obligation record source/errata locator, side and turn/stage applicability,
   choice/outcome, multiplicity or repeat rule, source confidence, runtime version,
   evidence path and status. Use `unresolved` when reachability or support is not
   proved; do not infer unsupported counts from absent evidence. Deliver a
   reproducible count by status and explicit unresolved count.
2. **Source and rulings:** independently reconcile recovered OA scans; reconcile §60.44 repair with
   §60.92(C), Land-only effects, map/arrival corrections and victory semantics.
   Keep unresolved source questions visible; independent double-entry and rights
   review precede content admission. These investigations can proceed alongside
   Combat work; they do not close the skeleton or freeze the gate.
3. **Skeleton/public authority:** finish the existing Combat certificate,
   public020–021/HOST-PUB-001, Runner022–024 and closeout025 on their governing
   contracts. Preserve bounded crash proofs and distinguish them from broader
   recovery and release-head tests. This is the roadmap predecessor for formal
   scenario-surface freeze, not a substitute for it.
4. **Freeze and admission:** after skeleton and source prerequisites, independently
   review the complete requirement-to-source matrix, explicit unsupported counts,
   rulings, task graph and size review for MVP-GATE-01. Then admit versioned
   source-verified content, IDs, hashes, references and rights under MVP-GATE-02.
5. **Playable authority:** implement each reachable Land family and terminal/victory
   behavior under MVP-GATE-03, with contract-first rules, deterministic tests,
   replay, conservation, privacy and explicit unsupported rejection. Decompose
   after the inventory measures scope; no timeline estimate is supported today.
6. **Product path:** complete durable create/checkpoint/stop/resume/recovery
   (MVP-GATE-04), private two-seat no-model Maproom flow (05), two independently
   checked complete six-turn games with save/resume and reproducible results (06),
   and versioned packaging, rights/notices and fresh-machine smoke (07). Release
   validation also requires exact-head CI, applicable oracles, dependency
   disposition and independent review.

**Next bounded packet:** finish source-backed setup and arrival double-entry, establish
map identities and classify sequence applicability. Independently review working
repair and victory-reference decisions; settle victory supply, result precedence and
Tobruk conflicts separately. Double-enter the G02 supplements and retain their provenance
and rights gates. Report scope without marking remaining families complete. In parallel, continue
the roadmap's public Combat/Runner skeleton prerequisites. No model-backed commander
or remote multiplayer is required for the first local playable RC.

The [next work order](2026-11-12-graziani-rc-work-order.md) breaks this into a
bounded independent review packet and dependency-ordered RC deliverables.
