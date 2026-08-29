# Sprint 4-5 Research-Gate Audit

**Status:** Completed planning audit; three bounded follow-up packets retained

**Date:** 2026-08-25

**Decision owner:** Project owner

**Research work item:** `RSH-SPRINT-4-5-001`

**Current evolution:** The owner subsequently approved `BREAKDOWN-001`: continuity is recorded
before outward Movement contracts, the table coordinate uses sequential d6, and Sandstorm
eligibility uses Table 21.38's accumulated-BP share. `MOV-TASK-004B` implements that bounded seam
and has passed its repository and independent-review gates. The recommendations below remain the
historical decision record.

## Decision question

Does the merged Movement authority foundation leave any source, state, fog, or sequence uncertainty
that must change the order of `MOV-TASK-005` through Sprint 5?

## Executive conclusion

The apparent-presence ruling, exact Capability Point representation, conservative stacking rule,
all-over-CPA rejection, and non-contact scope remain sound. Deeper `BREAKDOWN-001` research found
that the next owner decision must nevertheless precede `MOV-TASK-005`: choosing replay-continuity
now adds own BP/cohort facts to that observation clean cut, while choosing terminal histories keeps
the original Task 005 shape but requires an explicit later migration.

The retained [Breakdown packet](breakdown-continuity-spike.md) recommends continuity now and a
sequential-dice ruling. The [ZOC/Reaction packet](contact-reaction-zoc-spike.md) separates enemy-ZOC
interruption from combat-created Contact/Engaged. The
[Combat-cycle inventory](combat-cycle-source-inventory.md) completes the research-now source
inventory but keeps contract/task freeze behind approved Breakdown and Contact boundaries. Sprint 5
therefore remains a planning gate, not an implementation-ready task graph.

## Method and source index

This audit compared the merged Movement research/specification/design, current Core state and Land
sequence, and the pre-alpha roadmap against the retained primary source hierarchy. It records no
copyrighted rules prose.

| Source | Use |
| --- | --- |
| [1979 Land Game rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), especially 5.2, 8.14-8.25, 8.5-8.68, 10.1-10.3, 11-15, 18, and 21 | Sequence, Movement/Reaction/Contact, stacking/ZOC, combat, and Breakdown boundaries |
| [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf), especially 8.17, 8.23, 8.37, and 21.12 | Corrected Movement and Breakdown inputs |
| [Movement Foundation research](movement-foundation-spike.md) | Approved non-contact scope and explicit deferrals |
| [Movement Foundation design](../design/movement-foundation-v1.md) | Current event/state/task ownership and dependency graph |
| [`Cna1979LandSequence`](../../src/Cna.Core/Rules/Cna1979LandSequence.cs) | Current single-pass Movement/Breakdown/Combat/Reserve Release catalog |
| [`CampaignElementOperationalState`](../../src/Cna.Core/Campaigns/CampaignElementOperationalState.cs) | Current CP/Cohesion stage ledger |
| [Reconnaissance/contact research](recon-contact-knowledge-spike.md) | Existing knowledge and representation boundary |
| [Breakdown continuity packet](breakdown-continuity-spike.md) | Decision-ready BP/ruling/options and conditional Movement task graph |
| [ZOC/Reaction interruption packet](contact-reaction-zoc-spike.md) | Post-Movement interruption boundary and proposed task graph |
| [Combat-cycle source inventory](combat-cycle-source-inventory.md) | Minimal vector, private-choice/cycle architecture, and gated research/design graph |

## Findings

### `BREAKDOWN-001` — continuity decision before accepted Movement events

**Documented fact:** Breakdown Points accumulate through relevant movement during an Operation Stage,
including Reaction and Retreat movement; vehicle checks depend on accumulated state and the applicable
Breakdown column. Breakdown Determination follows a Movement Segment for both sides.

**Repository observation:** The current operational state records Operation-Stage identity, exact CP
expenditure, and Cohesion. The planned first Movement event has no Breakdown Point, vehicle/BAR, or
last-checked-column state, and current Content has no vehicle/TOE composition.

**Inference:** The current snapshot can replay arrival at Breakdown but cannot independently continue
through an authentic Breakdown adjudication. A later implementation therefore needs either minimum
continuity state introduced with Movement or an explicit clean-cut world/event migration.

**Unknown requiring owner decision:** Which option produces the smaller honest contract for the first
playable skeleton?

**Follow-up result:** The decision-ready packet recommends a bounded BP rules/content/world lane
before `MOV-TASK-005`, plus proposed `BRK-DEC-001` sequential dice and `BRK-DEC-002` continuity-now
rulings. Until the owner chooses continuity or terminal histories, Task 005 is blocked because its
own observation shape may change.

### `CONTACT-001` — Reaction is an interrupting opponent decision

**Documented fact:** Enemy-ZOC entry stops ordinary movement; qualifying adjacency can give the
non-phasing player a Reaction decision with movement, organization, contact/engagement, and
destination restrictions. Reaction consumes movement resources and contributes to Breakdown state.

**Repository observation:** Current authority has no pending non-phasing decision, Contact/Engaged
relationship, Reaction opportunity, opponent-action handoff, or resumed phasing-side contract. The
existing synthetic isolated battalion-equivalent pieces also do not supply a positive ZOC fixture.

**Inference:** ZOC/Reaction needs an explicit state machine and audience transition, not one more
validation branch inside `MoveElement`. Contact/Engaged are combat-produced participant
relationships and move to the Combat-cycle package rather than this interruption slice.

**Follow-up result:** The packet proposes seven post-Movement ZOC/Reaction tasks, a positive ZOC
fixture, opponent-owned pending window, deterministic decline, stale regeneration, and redacted
Chronicle/observation policy. Owner rulings remain for multiple reactors, repeat eligibility,
decline scope, waiting visibility, and minimum ZOC content vocabulary.

### `COMBAT-CYCLE-001` — repeatable cycle and hidden-choice contract

**Documented fact:** The phasing side may repeat the Movement, Breakdown, Combat, and Reserve Release
cycle subject to eligibility rules. Combat includes private/simultaneous choices and outcomes that
must be computed from the same pre-resolution state.

**Repository observation:** The current Land catalog contains one linear Movement-to-Reserve-Release
pass and then advances onward. It has no cycle ordinal, repeat/finish decision, persistent
Contact/Engaged eligibility, side-private simultaneous-choice protocol, or combat content/state.

**Inference:** One pre-alpha loop can be demonstrated with a bounded catalog, but the project must not
call that architecture continual/repeatable until cycle control and eligibility history exist.
Sprint 5's three current tasks are capability headings and are too broad to implement safely.

**Follow-up result:** Source inventory and one deterministic synthetic vector are complete. The
recommended authority shape uses trusted-Umpire sealed typed submissions against one frozen
pre-state, simultaneous calculation where required, a separate side-visible Chronicle projection,
and structural Land positions plus explicit cycle ordinal/history. Six research items can proceed
now; six design items wait for Contact and Breakdown.

## Decision and planning consequences

1. Make the owner choice in `BREAKDOWN-001` the next gate; conditionally insert the recommended
   BP continuity lane before `MOV-TASK-005`. **Current outcome:** approved, implemented, gated,
   and independently reviewed in `MOV-TASK-004B`.
2. Keep Tasks 005-010 otherwise dependency-ordered and keep public Movement membership dormant
   until Task 008.
3. Describe Movement Foundation replay as complete through arrival at the unsupported Breakdown
   boundary, not as proof that the next checkpoint can already adjudicate.
4. Implement ZOC/Reaction only after Movement Foundation; keep combat-created Contact/Engaged with
   `COMBAT-CYCLE-001`.
5. Treat the completed Combat-cycle source inventory as research input only; replace Sprint 5's
   broad headings with gated research/design work until contracts can be frozen.
6. Keep published scenario transcription, over-CPA Disorganization/reorganization, paired
   Maneuvers, Orleans activation, Maproom, and Needle at their existing later gates.

## Confidence and limitations

Confidence is high that the three gaps affect dependency ordering because each crosses authoritative
state, event/replay, or audience boundaries. This audit does not normalize the full Breakdown,
Contact, or Combat tables and does not authorize implementation. The dedicated packets must retain
their own source locators, decisions, acceptance vectors, and independent review before the
corresponding code begins.
