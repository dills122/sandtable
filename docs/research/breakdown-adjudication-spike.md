# Breakdown adjudication decision packet

**Status:** Research/design complete; owner accepted BRK-DEC-004–007 on 2026-09-05. Contract freeze follows in BRK-TASK-001; production remains unimplemented.

**Work item:** `BRK-RSH-002` — next Sprint 4 gate after ZOR-007.

**Decision owner:** Project owner. **Baseline:** `0512ec2`, including completed ZOR authority and Runner adoption.

## Decision and recommendation

Adopt a Truck-profile Breakdown package that first adds exact Movement/Reaction BP accounting,
then handles movement stops, deterministic checks, persistent broken-vehicle locations, and strict
replay. A dice-only action at the existing Breakdown checkpoint would leave accepted movement
histories without their inputs and would miss stops inside Reaction.

This packet records accepted decisions and ordered work in the [design](../design/breakdown-adjudication-v1.md).
Acceptance opens the bounded contract-freeze task. Existing `BRK-DEC-001` through `003` remain accepted:
sequential d6, continuity now, and accumulated-BP Sandstorm attribution. Decisions `004` through `007` below are now accepted; they are not yet added to the active rules manifest.

## Method and source lock

Primary scans were downloaded and visually inspected, including Land pages 33–34 and Common Charts
page 7; September errata was text-checked at 21.12. The scans contain image pages, so empty
`pdftotext` output was not treated as missing rules. Existing implementation and accepted Movement/ZOR
boundaries were examined separately. No chart image or rules text is redistributed here.

| Source | Locator | SHA-256 of inspected PDF |
| --- | --- | --- |
| [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) | PDF/printed pp. 33–34; 21.2–21.4 | `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99` |
| [Common charts](https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf) | PDF/printed p. 7; 21.38 | `51aa5a5bfdaca3d23794da71a45a830d98b798d060097cde6418126d6bd63bb0` |
| [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf) | 21.12 | `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb` |
| [Approved continuity packet](breakdown-continuity-spike.md) | BRK-DEC-001–003 and minimum seam | Existing repository decision authority |
| [Movement specification](../specs/movement-foundation-v1.md) | MOV-REQ-015 / MOV-AC-017 | Current no-post-creation-BP-mutation boundary |
| [ZOR specification](../specs/zoc-reaction-v1.md) | ZOR-REQ-007–012 | Reaction interruption, accounting and strict authority |

### Documented source facts

- 21.24–26: check at a movement stop; stage BP persists, including the opponent's portion; recheck
  needs a higher column.
- 21.27–29: raw BP must exceed three; grouping distinguishes vehicle types, BAR, and BP columns.
- 21.31–35: round BP upward for bands; combine column adjustments; cap at the highest column;
  read dice sequentially. Loss fractions round upward; a one-point unit ignores a 10% result.
  The example equates printed 33% with one-third.
- 21.36: distribute losses proportionally across affected types/units.
- 21.41–45: broken vehicles need a location, cannot move or fight, and may affect cargo/motorization.
  A nearby larger enemy can require some losses at the movement origin.
- Errata 21.12 changes Italian M13/40 BAR, not Truck BAR or dice interpretation.

These are source facts. Stop scheduling, canonical ties, and the first supported content boundary
below are accepted software decisions, not printed procedures.

## Repository observations

`CampaignVehicleBreakdownState` retains exact cumulative/Sandstorm BP, highest effective checked
band, and working/broken counts. Its enclosing `CampaignElementOperationalState` carries the stage.
It has no independent broken-lot location, movement-stop record, or pending check.

`Cna1979Breakdown` already normalizes Truck BAR, terrain/route/hexside inputs, bands and weather.
It has no outcome resolver. Rainstorm has an input transformation; `GetWeatherColumnShift` rejects
Rainstorm, so integration must apply that transformation and select a neutral shift deliberately.

`CampaignElementMovedV2Factory.CalculateMovement` and `CampaignReactionParticipantEventFactory`
share Movement calculation. Accepted events retain CP evidence; current projection preserves BP
state. Tests require that existing boundary. It must change as one versioned successor, not by
silently altering current `ElementMoved` v2 or `ReactingElementMoved` v1 semantics.

`SandtableRandom.RollD6` uses rejection sampling. Two accepted d6 do **not** necessarily consume two
bytes. Evidence must retain actual before/after cursors and replay the established generator.
Current completion reaches first-side Breakdown with no resolver; there is no continual-cycle or
repair authority to assume as an available continuation.

## Outcome normalization and experiment

[Research-only bounds](fixtures/breakdown-outcome-bounds.v1.json) encode the numerical outcome
surface by inclusive sequential-coordinate bounds. The printed `33` label maps to exact one-third under accepted `BRK-DEC-004`. No production code reads this file.

```bash
python3 docs/research/verify-breakdown-outcomes.py
```

Observed result: all 324 band/coordinate cells are covered exactly once; outcome columns are
monotone; nine independently selected source-cell probes pass. All 606 proposed loss combinations
for 0–100 working points and six labels remain between zero and available points. Structural
checks do not independently prove every transcribed boundary; visual source comparison is required.

Exact transcription hash:
`f63dc336364648acb52d20070bb1076205a36a40769f4391531ba65aaf94c401`.
The experiment asserts the source example: 30 points at 10% gives 3 losses; 20 at the `33` label
gives 7 under the accepted one-third interpretation. A 100-point case distinguishes that ruling
from literal 33/100. Existing source rules/rulings, not this Python checker, remain authority.

## Accepted owner decisions

Owner accepted all four recommendations and their scope restrictions on 2026-09-05 after review 3.
Alternatives below retain decision rationale; they are not open choices.

| ID | Accepted choice | Rejected alternative and consequence |
| --- | --- | --- |
| `BRK-DEC-004` | Treat printed `33` as exact `1/3`; other labels use exact percentage fractions. Apply the one-point/10% exception at the admitted check-unit boundary. | Literal `33/100` differs for larger counts and loses the example's exact fraction. Grouped one-point exceptions need an explicit later interpretation. |
| `BRK-DEC-005` | Introduce explicit per-element stop boundaries. Finish a triggered Reaction window before resolving a phasing stop; resolve an active reactor's stop before another participant or phasing Movement resumes. A forced System close records the stop and pending continuation without discarding costs. | Segment-only checks are smaller but omit intermediate stops. Breakdown-before-Reaction changes interruption order and potentially the trigger's visible facts; source precedence is not fully explicit. |
| `BRK-DEC-006` | First playable adjudication mode admits only publicly identified, capability-certified synthetic packs: at most one unladen standalone Truck cohort per side; no passenger/cargo model or commands that can create one. This structurally prevents grouped/mixed checks. Preserve broader grouping/capacity work as a named later gate. | Generalize all current motorized infantry immediately: requires proportional allocation, one-point exception grouping, passenger/capacity truth, and mobility consequences before safe activation. |
| `BRK-DEC-007` | Add persistent broken lots now. The public initial capability profile structurally excludes every combat unit/represented formation or combined combat grouping larger than one battalion, at creation and after every transition. Origin-placement exceptions are therefore impossible in admitted histories. Keep general placement/capture/towing/repair unsupported. | A scalar broken count is smaller but cannot preserve abandoned equipment when survivors move. Full placement/capture/repair expands into new content and player choices. |

`005` is an explicit precedence ruling, not a claim that the source settles competing stop and
Reaction triggers. `006`/`007` are bounded capability exclusions, not replacement historical rules.
Admission must prove public capability-profile invariants before a campaign becomes player-visible;
every successor state preserves them. Runtime progress must not depend on a hidden placement-support
predicate. Invalid/out-of-profile roots reject at creation, not at an otherwise legal later stop.
A malformed later submission remains a rejection, never a successful zero-loss check. Existing motorized-infantry Reaction fixtures can prove dormant BP accounting but do not qualify
for this initial public adjudication profile; they cannot be relabeled as full Breakdown success.
Positive Reaction-stop lifecycle vectors use non-cohort combat reactors, with an admitted no-roll
resolution. Motorized-infantry Reaction adjudication remains a later transport-support gate.

## Confidence, limits, and next gate

High confidence in chart coordinates, existing BP gap, and RNG cursor requirement. Medium confidence
in source-determined interrupt ordering; the owner has now resolved scheduling and delivery scope explicitly.
Map terrain BP inputs are reused from the approved continuity normalization, not newly source-audited
in this packet. No exhaustive repair, towing, capture, transport-capacity or vehicle-class research
is claimed.

Decisions `004`–`007` are accepted. Freeze the complete successor identity table and acceptance criteria in `BRK-TASK-001`. Only that approved contract freeze opens
production tasks. A future claim of general Breakdown support additionally needs grouped loss
allocation, origin-placement, passenger/cargo consequences and later-domain gates.


## Independent review reconciliation

[Review 1](../reviews/breakdown-adjudication-design-review-1.md) found two P2 design defects.
Both are **accepted**: hidden placement-dependent runtime failure could disclose private facts;
recomputing placement after Reaction could lose beginning-of-route enemy/blocker truth.

DEC-006/007 define a public capability profile with structural creation and transition
invariants, rather than secret-dependent runtime support. No qualifying larger combat context can
arise in an admitted history, so the placement predicate is always false. General placement remains
a later versioned gate requiring immutable start-time threat/blocker evidence; a route-origin ID
alone is explicitly insufficient. Scope remains the same bounded research/design item.

[Review 2](../reviews/breakdown-adjudication-design-review-2.md) returned **Ready for owner
decisions**, with no actionable findings remaining. It independently compared every outcome-bound
row to the chart and reconfirmed the numerical, hash, link and diff checks. This closes
`BRK-RSH-002`. At that review, DEC-004–007 and `BRK-TASK-001` remained pending; owner acceptance followed review 3 on 2026-09-05. No production tests were run because
this delivery changes only documentation and research artifacts, not runtime behavior.

[Review 3](../reviews/breakdown-adjudication-design-review-3.md) was Ready for owner decisions and contract freeze. Its three-instance research/design review budget is exhausted. Acceptance does not imply that later schema edits or production code received that review.
