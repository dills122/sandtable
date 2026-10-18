# Graziani six-turn Land scenario: source-surface inventory

**Status:** Research inventory, **not** an `MVP-GATE-01` freeze or admitted content

**Date:** 2026-10-18

**Scope:** Original 1979 *Graziani's Offensive*, Land Game only, both sides
**Owner packet:** `REL-AUD-04` / `MVP-GATE-01`

This inventory identifies source obligations before scenario transcription and implementation. It
does not reproduce the published order of battle, setup tables, victory text, map art, or scans. It
does not determine distribution rights. No historical scenario records were added to the engine.

## Sources and method

- **Primary scenario:** preserved [1979 scenario scan](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf), printed pp. 25–29, especially §§59.0–59.6, 60.22, 60.31–60.47, and 60.6–60.92. The image-only scan was rendered and read visually; the local copy stayed outside Git. PDF image pages 1–5 correspond to printed pp. 25–29.
- **Primary sequence:** preserved [1979 Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), especially §§5.2 and 7.11–7.15. The [current typed sequence catalogue](../../src/Cna.Core/Rules/Cna1979LandSequence.cs) is an implementation cross-check, not scenario authority.
- **Corrections:** [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf), especially map D, §§4.44b, 8.86, 32.0, 59.45, 60.31, 60.32, and 60.45. Errata supersedes conflicting printed scenario material under the [adopted source hierarchy](cna-source-material-spike.md#recommended-authority-policy).
- **Project acceptance:** [first-release audit](2026-10-05-first-release-audit.md#dependency-ordered-release-work) and [MVP gates](../roadmap/pre-alpha-roadmap.md#post-skeleton-milestones-toward-the-first-playable-mvp).

The scenario pages were checked independently of the repository's earlier source-material summary.
The errata PDF was checked directly. This is one-person source reading, **not** the independent
double-entry setup check required by `MVP-GATE-02`.

## Verified size and boundaries

| Measure | Verified result | Evidence and limit |
| --- | --- | --- |
| Scenario duration | Game Turn 1 / Operation Stage 1 through the conclusion of Game Turn 6 / Operation Stage 3 | Scenario §60.22, printed p. 27. Six turns and three stages per turn yield **18 stage occurrences**. |
| Side activations | **36** first/second-player activation blocks, **18 per side** | Land sequence §5.2 and `Cna1979LandSequence.CreateTurn`: two player blocks per stage. Initiative can change their order; these are not 36 individual decisions. |
| Initiative | Italian side holds the entire first turn; normal initiative determination starts with turn two | Scenario §60.6, printed p. 28; Land §7. This yields one assigned turn and five later determinations, not five preselected winners. |
| Current catalogue size | **112 position entries per turn**, **672 occurrences** if its current shape repeats for all six turns | Code calculation: 3 opening entries + 3 × (6 stage prelude entries + 2 × 15 player entries) + 1 end entry. This is an implementation skeleton, not a count of decisions, actions, repeats, or source rules. Its six Combat steps are position identifiers, not proof that every branch is implemented. |
| Victory categories | **Six** named categories: strategic, decisive, tactical for each side | Scenario §60.81, printed p. 28. These are source conditions, not six implemented evaluators. |
| Physical play estimate | Published estimate is **25–50 hours** | Scenario §60.22. It is not a digital implementation estimate. |

The source gives location-dependent and player-choice setup clauses, hierarchical formations,
attached/detached units, off-map categories, trucks, supply, air facilities, and reinforcement
tracks. Those cannot be safely collapsed into a single unit or record count from visual reading.
**Exact setup record, component, map-hex, arrival, chart-coordinate, reachable-rule, and unsupported
item totals remain unknown.** `MVP-GATE-01` requires those totals and a second check before freeze.

## Requirement-to-source matrix

| Requirement / reachable question | Source anchor | Measured now | Missing gate evidence |
| --- | --- | --- | --- |
| Turn and operation-stage boundary | Scenario §60.22; Land §5.2 | Six turns; 18 stages; two ordered side blocks per stage. | Independently reconcile every source position and repeatable movement/combat loop with catalogue and terminal transition. |
| Initiative and acting side | Scenario §60.6; Land §§7.11–7.15 | Italian first-turn holder; later holder not predetermined. | Exercise both holder outcomes and source-cited tie/actor rules across turns 2–6. |
| Axis land setup and formation hierarchy | Scenario §§59.2, 60.31; errata §§4.44b, 60.31 | Printed p. 27 contains fixed, region-choice, and off-map placement clauses; Cirene and Marmarica appear among the initial formations. Errata removes a duplicated artillery listing and reports missing OA sheets for Sirte, Cirene, and Marmarica. | **Source gap:** the linked four-page errata scan does not include the supplemental OA sheets that its §4.44b mentions. Locate an authoritative original supplement before normalizing those three formation rosters, strengths, or subordinate assignments. Setup record counts remain unknown; scenario placement lines cannot substitute for OA composition. |
| Commonwealth land setup and formation hierarchy | Scenario §§59.2, 60.41; errata §4.44b | Printed p. 27 contains fixed and choice placements plus an initial assignment correction. | Independently transcribe units and variants; reject a placement or composition that only a community aid supports. |
| Map, terrain, facilities, and start construction | Scenario §§60.5, 60.7; Land movement/terrain and source charts; errata map D and §8.37 | Printed p. 28 lists airfield/strip/basin categories and start-map construction constraints; errata changes one map-D hexside. | Inventory exact relevant hexes/edges/topology and source authority for each; prove whether the erratum is reachable in six turns. Do not import map art. |
| Initial trucks and movement capability | Scenario §§59.4, 60.33, 60.43, 60.92(A); errata §59.45 | Both sides have starting truck pools and placement choices; Land-only mode changes motorization treatment. | Normalize truck types, attachment/load choices, capability, losses, and all player allocations; verify no fleet/air truck deduction is silently applied twice. |
| Land-only initial supply and ongoing supply | Scenario §§59.5–59.6, 60.92(B–C); Land §§32, 57; errata §§32.0, 59.45 | §60.92 **replaces the initial-supply status** in §§60.34/60.44 for this mode; replacement data spans printed pp. 28–29 and permits discretionary placement. | Transcribe corrected supply units, dumps, capacity, routes, consumption, source/target hexes, and placement choices; separately adjudicate supply traces for every victory condition. Abstract-mode procedures were explicitly warned as untested in errata §32.0. |
| Initial and ongoing repair | Scenario §§60.33, 60.44; Land repair sequence | Axis repair facilities are stated in §60.33; Commonwealth repair facilities appear after the supply details in §60.44. | Resolve whether §60.44's repair-facility statement survives the Land-only §60.92(C) replacement of Commonwealth initial supply status. Then reconcile location/capacity, recovery/maintenance rules, and six-turn reachability; do not silently carry or drop those facilities. |
| Arrivals, reinforcements, replacements | Scenario §§60.37, 60.47; printed reinforcement tracks; Land §8.86; errata §§4.43a, 8.86 | Scenario points to side-specific tracks; errata allows a reinforcement to move in its arrival stage. | Extract only turns/stages 1–6, locations, roster and prerequisites; prove whether general errata schedule additions occur in this window. Do not infer an empty arrival stage from missing scanned text. |
| Air/naval/logistics abstraction boundary | Scenario §§60.35–60.37, 60.45–60.47, 60.91–60.93; Land §§32, 58; errata §§32.0, 60.45 | Scenario explicitly supplies a Land-only abstraction. §60.45's later printed fleet-availability paragraphs are invalidated by errata; its first paragraph survives. | Freeze exactly which naval/air/convoy effects remain active and which detailed-game inputs are excluded. Retain source-required abstract effects; do not equate “abstracted” with “no work.” |
| Stage decisions and rules/tables | Land §5.2 and referenced Land/charts; current typed catalogue | Current catalogue includes stage prelude and both side blocks, with movement, breakdown, six Combat step identifiers, convoy, rail, repair, and patrol positions. | For each of 18 stages classify each reachable action/result/table coordinate as implemented, source-proven empty, or explicit accepted exclusion; count every unsupported family. Repeatable movement/combat requires a separate bound. |
| Termination and victory | Scenario §§60.22, 60.81; supply rules; Land end-of-turn | End after turn 6/stage 3; three categories per side include occupation/hold and supply predicates. | Specify evaluation instant and precedence among conditions, source-verified location identities, supply-route proof, and deterministic final-result event. No victory implementation is claimed here. |
| Side knowledge and replay | Land visibility/rules; project FOG/RPL gates; scenario setup | Scenario contains side-specific and shared setup facts; current rules laboratory is synthetic. | Classify initial private/public records, hot-seat release timing, deterministic seeds, all cuts, and opponent-view negatives across the entire six-turn path. |
| Rights and distribution | [source-material rights assessment](cna-source-material-spike.md#unknowns); `MVP-GATE-02/07` | Original scans, map/counter art, and rule text have no identified redistribution license. Source scans remain outside Git. | Owner-reviewed reuse/license posture for source-derived data, product/name, notices, packaging and any distributed content. This inventory is not legal clearance. |

## Correction traps for transcription

1. Apply the September errata before counting setup records. Its §60.31 removes a duplicated Axis
   artillery listing; §4.44b reports omitted OA sheets for Sirte, Cirene, and Marmarica and changes
   two Commonwealth initial assignments relevant to the Italian scenarios. The preserved errata
   scan names but does not supply those OA sheets; printed §60.31 places Cirene and Marmarica but
   does not establish their complete rosters. Treat all three formations' OA data as blocked until
   an authoritative supplement is found. §60.32 corrects an aircraft identifier, though detailed
   aircraft records must first be classified for Land-only use.
2. Land-only §60.92 replaces the printed initial-supply status in §§60.34 and 60.44. Copying both
   would double-count; dropping the replacement would leave no authenticated initial supply.
   §60.44 also states Commonwealth repair facilities; whether that statement survives §60.92(C)
   needs an explicit source ruling. The errata's §59.45 permits additional supplies on starting
   trucks, so fixed dump rows are not the complete player-selected initial supply state.
3. Errata §60.45 keeps only the first paragraph of that scenario fleet case. Do not normalize the
   invalidated later paragraphs as authoritative starting fleet records.
4. Section 60.81 victory supply/hold conditions still require reachable map and route facts even
   though the Air and Logistics Games are abstracted. The errata's §32.0 warning means neither
   automatic emptiness nor balanced outcomes can be assumed.

## Work needed before `MVP-GATE-01`

1. **Independent double entry:** transcribe source/errata-derived setup, OA, map, supply, trucks,
   repairs, and turns 1–6 arrival data in two independent passes; reconcile IDs and counts. Keep
   scan images outside Git; store only normalized facts with precise provenance after rights review.
2. **Reachability pass:** for 18 stages and both side blocks, enumerate decisions, rule branches,
   charts, repeatable segments, random results, and source-proven empty cases. Map each to existing
   authority, accepted exclusion, or explicit unsupported item; publish exact counts.
3. **Victory and terminal ruling:** pin evaluation timing, side precedence, hold/supply route
   semantics, and deterministic terminal record against source/errata, then independently cross-check.
4. **Content and rights decision:** approve derived data workflow, map/counter placeholder posture,
   source/errata manifest, distribution rights, and provenance scheme before content admission.
5. **Size review:** split the measured residual into implementation packets with owners, tests,
   dependency order and effort ranges. Current inventory is evidence for that review, not the
   reviewed task graph or a date forecast.

**Freeze state:** open. Exact unsupported-item count: **unknown** until independent transcription
and reachability analysis. `MVP-GATE-02` historical content admission, six-turn authority, durable
save/resume, hot-seat browser play, and complete-game acceptance remain separate gates.
