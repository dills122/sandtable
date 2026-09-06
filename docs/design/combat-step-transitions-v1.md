# Combat Step Transitions

**Status:** `CMB-DES-003` bounded design complete for review; production remains gated.
**Date:** 2026-09-06. **Input checkpoint:** `f5be873`.

This packet joins the [identity](combat-opportunity-identity-v1.md) and
[sealed protocol](combat-sealed-decision-protocol-v1.md) designs to the six structural Combat
steps. It defines voluntary selection, an explicit Retreat Before Assault decline, empty-step
proofs, assignment handoff and cancellation/settlement closure. `CMB-DES-004` owns cost and
resolution ordering in the [cost/resolution packet](combat-cost-resolution-order-v1.md). Exact wire
schemas and implementation tasks still require the combined freeze.

## Evidence and admitted path

| Evidence | Consequence |
| --- | --- |
| [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), PDF p18-19, 11.0 procedure | Determine involved combat before gun positions; Barrage precedes Retreat Before Assault, assignment, Anti-Armor and Close Assault. |
| Land rules, PDF p20, 12.11-12.19 and Barrage procedure | Forward/Back choices belong to gun units; Barrage effects settle before the next step. No guns does not mean all future Barrage choices can be replaced with a pass. |
| Land rules, PDF p21, 13.0-13.28 | Retreat Before Assault is optional non-phasing movement after Barrage, distinct from Reaction and result-mandated Retreat. Declining movement is not a default inferred from silence. |
| Land rules, PDF p21-22, 14.0, 14.21-14.26 and 15.14-15.16 | Assignments precede Anti-Armor; TOE cannot be spent in both assault roles. Actual Anti-Armor effects precede Close Assault and can alter its strength. |
| [Result-surface research](../research/combat-rules-result-surface-spike.md) | Selected infantry path has no guns/armor/Barrage, a declined Retreat Before Assault, and two full 10-TOE assignments. Every reachable admitted roll still needs settlement. |
| [Cycle decision](../research/continual-cycle-identity-and-history-decision.md) | One cycle occurrence traverses all four segments. Empty/cancelled choices cannot create material-progress loops; no cycle closure while obligations remain. |
| [Current sequence](../../src/Cna.Core/Rules/Cna1979LandSequenceV4.cs) | `GetNext` accepts exact catalog positions; `IsSupportedCheckpoint` stops at first-side, stage-1 Position Determination. A catalog successor is not implemented combat authority. |
| [Breakdown lifecycle](../../src/Cna.Core/Campaigns/CampaignBreakdownLifecycleFactory.cs) | `CreateBreakdownCompletion` requires idle Breakdown and no Reaction window before entering Combat. Preserve this completed boundary and its cumulative ledgers. |

Land PDF pages 18-22 were checked as source evidence for the procedure; page 21 was rendered and
visually checked for this packet's Retreat Before Assault boundary. Digital states, receipts,
timeouts and identifiers below are Sandtable design proposals, not printed-rule mechanisms.

Admission stays with the selected singleton, non-Reserve, full-strength infantry exercise. It
allows the **decline path only** for Retreat Before Assault and the full-assignment path only for
Close Assault, as the preceding research/protocol specify. This is an explicit bounded capability
proposal; it does not claim other source-legal retreat routes or partial assignments are illegal.
The scripted demonstration submits the decline as a real choice. No automatic decline, hidden
terrain trick or inferred lack of retreat eligibility substitutes for that submission.

A scenario requiring actual Retreat Before Assault, gun choices, Barrage, Anti-Armor, mandatory ZOC
attacks, or multiple participating units needs a reviewed extension before admission. A public
profile claiming the complete source action surface cannot use this restricted fixture's action
set. Certification must also establish full reachable-result/settlement support before the selected
attack is offered; this design does not authorize a campaign to stall on a later unsupported roll.

## Transition decisions

| ID | Decision |
| --- | --- |
| `CMB-STEP-001` | Open a segment occurrence at the exact post-Breakdown Position Determination checkpoint, bound to the authoritative cycle and acting-side order. Keep all six catalog step IDs; do not duplicate them for retries or cycles. |
| `CMB-STEP-002` | Choose voluntary combat once at Position Determination. Retain a provisional selection; create the final DES-001 opportunity and DES-002 round only at Force Assignment after the actual decline and prior-step closures. |
| `CMB-STEP-003` | Close each step through one typed completion event carrying an exact predecessor/successor and evidence. Empty means proven no admitted work, not an unvalidated empty list supplied by a caller. |
| `CMB-STEP-004` | At Retreat Before Assault, the selected defender owns the explicit decline decision. Silence/unavailability cancels the voluntary selection; it never creates a decline receipt or assignment round. |
| `CMB-STEP-005` | Prepared assignment advances only through the exact Force Assignment and empty Anti-Armor completion events. Preserve every combat fact and sealed payload; DES-004 commits at Close Assault. |
| `CMB-STEP-006` | No-selection and cancelled paths still traverse remaining structural steps. They close with no attack, never a fictitious completed assault. Successful combat closes only after DES-004/005 settle all mandatory effects. |
| `CMB-STEP-007` | Reconstruct control states, choices and closure proofs from retained events/checkpoints; side-visible references and errors remain independently projected. Never reuse raw authority receipts as outward step tokens. |

## Segment context and voluntary selection

`CombatSegmentOpened` follows the completed Breakdown boundary and keeps the current position at
Position Determination. It retains full cycle/config provenance, segment-opening authority version
and history-prefix binding, exact catalog position, resolved phasing/non-phasing sides, opening
snapshot proof and admitted profile. Resolve the actor from retained first/second-side order; a
missing `ActiveSide` on the historical catalog template is not permission for either side to act.
Only the first-side stage-1 entry is a current implementation checkpoint; later slots/cycles need
their own versioned capability admission and the same relative-slot validation.

An authority segment ID binds that context; an audience reference uses only approved cycle/position
facts and visible revision. Step occurrence identity is `(segment occurrence, catalog step ID)`;
sequence position alone cannot identify a repeat. Exact hash codecs remain the combined freeze's
responsibility, following DES-001/002's authority/public separation.

When one approved voluntary candidate exists, the phasing side can `select-close-assault` using
its own participant capability plus the approved apparent target reference, or `finish-without-attack`.
Current authority maps selection to exact unit/component/target-hex evidence. Zero candidates
require one system-authored no-selection outcome and no player/model decision. Mandatory obligations
must already be excluded by profile certification; an empty outward list cannot prove their absence.
Segment opening initializes a pending selection decision and its pinned timing only for a nonempty
admitted candidate set. Otherwise its state requires the system no-selection closure, with no
decision ID/deadline that a stale timer could consume.

`CombatSelectionClosed` records exactly one outcome: selected with a provisional `selectionId` and
bindings, or no-selection. It consumes the selection decision but does not advance the structural
position. A selection is intent, not an attack commitment or the later frozen opportunity. It
spends no CP/ammo/RNG, creates no attacked-history entry and consumes no target-hex use. There is no
second selection/retarget action in this segment, even after cancellation or a newly minted action ID.

The provisional selection links the original participants, target, opening evidence and later
decline receipt. At Force Assignment revalidate those bindings and derive a new opportunity from
the **current pre-round snapshot/prefix**, including completed prior-step evidence. Do not reuse the
selection-opening digest as the post-decline combat base. Current facts may not be silently changed
under the selection; future effectful steps must explicitly retire/revalidate affected selections.

## Six-step completion table

Each `CombatStepCompleted` event binds segment occurrence, from/to catalog positions, prior/result
authority versions, the previous step receipt (or segment opening), and one closed proof variant
valid for that step. Persist its receipt and successor position atomically. There is exactly one
such event per step; choice events stay at their current step. Only the system completes steps,
after validating the proof against authority. No generic user `advance` capability exists.

| From | Required closure proof | Exact successor |
| --- | --- | --- |
| Position Determination | Selection decision closed; profile proves no participating gun-position choices on either side. | Barrage |
| Barrage | Position receipt; no admitted Barrage participants/effects or mandatory Holding Off obligation. Preserve all world/resource/RNG facts. | Retreat Before Assault |
| Retreat Before Assault | Barrage receipt plus selected defender's accepted decline, **or** terminal no-selection/cancellation evidence. No pending defender decision. | Force Assignment |
| Force Assignment | RBA receipt plus DES-002 `Prepared` round with both full assignments, **or** no-selection/cancellation evidence. No collecting round. | Anti-Armor |
| Anti-Armor | Assignment receipt; no admitted armor/fire/exposure work. Any prepared round remains byte-identical; cancelled/omitted paths have none pending. | Close Assault |
| Close Assault | DES-002 completed terminal round with all settlement closed, **or** terminal no-attack disposition and no round/commitment/obligation pending. | Same slot's Reserve Release |

There is no separate `CombatSegmentCompleted` event that advances a second time. The Close Assault
step receipt also closes the segment occurrence. Its terminal disposition is `assault-completed`
with commitment/settlement references, or `no-attack` with selection/cancellation references.
Reserve Release then owns its own actions; arriving there releases no unit, resets no ledger,
closes no Operation Stage and cannot itself repeat the cycle.

Empty-step proofs come from immutable profile facts joined to current authority, not from empty
private responses. The absence of gun, Barrage and Anti-Armor capability is distinct from choosing
to withhold capable forces. Introducing one such unit makes this profile unsupported at trusted
admission; later rules cannot silently run the empty branch against it.

## Retreat Before Assault decline and cancellation

For a live selected attempt, `CombatRetreatDecisionOpened` persists one defender-owned decision at
the RBA position. Its authority context binds segment/selection, original defender, current state,
completed Barrage receipt and profile; outward context contains only approved own/apparent facts.
The bounded typed candidate is `decline-retreat-before-assault` for that own participant. It is
not a zero-length movement command and carries no cost, path or RNG field.

`CombatRetreatBeforeAssaultDeclined` records a valid authenticated current decision/action binding,
accepted canonical proposal, trusted admission time and own receipt. It closes that decision while
remaining at RBA; a subsequent system step completion advances to Force Assignment. The decline
does not consume combat ammo/CP, create Contact/Engaged, or waive later resource validation. A
duplicate, wrong-side, stale or changed-participant decline emits nothing. A late decline cannot
revive a cancelled selection or be reused for another cycle/selection.

Both selection and RBA waits use DES-002's pinned finite-budget, trusted-time, authentication,
receipt and exact-deadline rules, applied to their own decision contexts. Each real stage opens
its deadline once. Retries/readback/restart cannot refresh it; progressing to a different stage
creates that stage's own configured decision, never another instance of the old choice.
Clock regression/loss of clock confidence follows trusted unavailability. Selection timeout or
trusted controller-unavailable input emits system-authored `CombatSelectionClosed(no-selection)`;
after selection closes, its old timer/unavailability commands are no-ops and cannot affect a later
decision context. RBA timeout/unavailability emits `CombatSelectionCancelled`, closes the pending
defender decision and disposes the selected attempt. It records no decline and opens no sealed round.
Cancellation causes/clock diagnostics remain private. After a decline has been accepted, its stale
timer is a no-op; the system completes RBA without waiting for the defender again.

If timeout occurs later while the DES-002 assignment round is collecting, that protocol's
`CombatChoiceRoundCancelled` supplies the terminal cancellation proof. Do not also emit an earlier
selection/RBA cancellation or erase already accepted choice receipts. Once `Prepared`, neither
prestep timers nor assignment expiry can discard the choices. Accepted selection/decline has no
user withdrawal action; supported cancellation comes only from the appropriate live system context.

All three no-attack routes (no selection, RBA cancellation, assignment cancellation) finish the
remaining step receipts without costs, new attacked history, target-hex use or RNG. They never
return to Position Determination. Step bookkeeping alone is not material progress for a later
cycle repeat; any earlier Movement progress is preserved for CYCLE-DES-001's close assessment.

## Prepared-round proof and resolver handoff

At Force Assignment, `CombatChoiceRoundOpened` creates the DES-001 opportunity and DES-002 round
together, referring to the prior RBA receipt and accepted decline. The slot set is exactly attacker
and defender; each full assignment is accepted through DES-002. Neither partial assignment nor
`force assignment complete` supplied by a player can bypass a missing slot.

First recompute `CombatChoiceRoundOpened` from DES-002's pre-round snapshot and retained opening
inputs. From that validated post-opening state until `CombatAttackCommitted`, replay permits only:

1. the two valid DES-002 seals, in either accepted order, with their allowed receipt/time/revision
   bookkeeping;
2. one Force Assignment completion, requiring and preserving the `Prepared` state already derived
   by the second seal, and advancing to Anti-Armor;
3. one empty Anti-Armor completion, preserving the prepared round and advancing to Close Assault.

A restart or readback adds no event. Expired/stale timer commands add no event once prepared. No
generic state-version jump, extra step completion, third seal, movement, resource delta, altered
relation, changed policy or RNG consumption fits this trace. Recompute the expected post-state by
applying the validated opening event and this typed suffix to the frozen base; do not mask fields to make the
hashes match. Segment/step receipts and positions change; world facts and sealed allocations do not.

At Close Assault the prepared round is still open. DES-004 owns `CombatAttackCommitted`, cost
timing and result transitions; DES-005 owns loss/retreat/custody/relationship settlement. DES-003
offers no step-completion action until DES-002's completed terminal proof exists. A committed but
unresolved attack can therefore resume at Close Assault after restart without a new selection,
deadline or roll. A partially settled result cannot be relabelled as `no-attack` to escape an
unsupported obligation.

## Persistence, disclosure, and extension boundaries

Archives retains segment opening, selection/decision contexts and deadlines, accepted decline and
receipts, ordered step receipts, optional sealed round, terminal disposition and active-stage
history/progress. Restore must validate every receipt's cycle/slot/side, predecessor, successor,
typed proof and version chain. Wrong order, missing receipt, duplicated completion or a receipt
copied from the same structural step in another cycle fails strict readback.

For each closed choice, preserve its authenticated own receipt separately from side-projected step
history. Opponents see approved phase progress and generic waiting/closure, not raw selection/base
digests, real unit bindings, candidate counts, cancelled-party identity or timeout cause. A common
step transition increments each affected audience revision once; private choice bookkeeping changes
only its owner's authorized projection. DES-005 must approve phase/decline-inference and candidate
membership disclosure, including equal visible histories producing equal submission outcomes,
before public activation. Arbitrary omission of hidden obligations is not a privacy mechanism.

| Extension / owner | Required work before admission |
| --- | --- |
| Actual RBA movement; later movement/Combat extension with DES-005 continuity | Source 13.1 pin/Cohesion eligibility; 13.21 CP/fuel/vehicle Breakdown; 13.22 and 8.6 break-off; 13.23-13.24 initial-adjacency limits; 13.25 action restriction; 13.26 ZOC stops; 13.28 later exposure. Add move/stop/completion and timeout continuation, not a renamed Reaction or result-Retreat event. |
| Position/Barrage; DES-004 plus a pre-step extension | Forward/Back, simultaneous effects, pinning and targets before RBA; new private-step envelopes and base invalidation rules. The infantry no-op proof cannot stand in for this. |
| Broader force assignment/AA; DES-004 plus DES-001/002 extension | Partial/component/multi-unit allocation, withheld versus exposed units, category-specific history and post-AA base. Preserve original commitments while deriving reduced surviving CA strength. |
| Settlement; DES-005 | All selected result branches, including zero-loss Engaged, one-hex Retreat, capture/custody and post-assault Contact. Completion proof must cover each mandatory effect. |
| Cycle/Reserve; CYCLE-DES-001 | Relative-side repeated occurrence, material progress, disposed attempts, source release obligations and exact repeat/finish successor. No segment receipt alone certifies this loop. |
| Combined contract/implementation plan | Versioned segment/step/choice unions, canonical hashes, size limits, event/snapshot/observation schemas and migrations, hosted recovery and proportionate executable tests. |

## Acceptance and verification plan

| Case | Required observation | Decisions |
| --- | --- | --- |
| `CMB-STEP-AC-001` | Only exact completed Breakdown/no-Reaction authority opens Combat. Wrong stage/side/order rejects; CP/BP/Cohesion/TOE/RNG remain unchanged. | 001 |
| `CMB-STEP-AC-002` | Selection creates no attack/round. Force Assignment derives its final opportunity from the later snapshot and links the original selected participants and decline. | 002/004 |
| `CMB-STEP-AC-003` | Selected decline/full-assignment trace has exactly six ordered step completions. Each successor is the same cycle/slot's next step, ending at Reserve Release. | 001/003/005 |
| `CMB-STEP-AC-004` | No-candidate closure, voluntary no-selection and selection timeout/trusted unavailability produce no RBA/assignment round; zero candidates creates no model decision. All six steps close without attack/resource effects; old timer/unavailability commands after selection closure cannot affect the next decision. | 002-003/006 |
| `CMB-STEP-AC-005` | Missing RBA response cancels without a decline receipt; accepted decline defeats a later timer. Stale/wrong-side/cross-selection decline rejects without mutation. | 004/007 |
| `CMB-STEP-AC-006` | RBA/assignment cancellation traverses remaining steps once, preserves earlier receipts/resources and never permits reselection or synthetic material progress. | 003-004/006 |
| `CMB-STEP-AC-007` | Second seal derives Prepared; FA completion requires and preserves it, then permits exactly FA->AA->CA for either valid seal order. Injected extra event, changed world/round or forged empty-AA proof fails replay. Prepared choices cannot expire. | 003/005/007 |
| `CMB-STEP-AC-008` | Committed/partially settled CA cannot complete or become no-attack. Only fully settled terminal proof advances to Reserve Release; release itself remains separate. | 005-006 |
| `CMB-STEP-AC-009` | Restart after opening, selection, empty steps, RBA opening/decline, either seal, preparation and settlement reconstructs the same next capability and bytes without refreshed deadlines. | 001-007 |
| `CMB-STEP-AC-010` | Duplicate/out-of-order/cross-cycle step receipts and caller-supplied empty lists cannot advance authority. Each accepted completion advances authority exactly once. | 001/003/007 |
| `CMB-STEP-AC-011` | Adding a gun/armor/mandatory obligation or requiring actual RBA fails the bounded profile gate before decisions. The decline-only fixture is never advertised as the full source action surface. | 003-004/006 |
| `CMB-STEP-AC-012` | Hidden bindings/reasons with equal authorized visible histories preserve tokens, waiting, step history and submission outcomes; raw authority receipts cannot reach player/model/Runner output. | 007 |

These are future executable-test obligations. Source and state-trace inspection plus local
link/ID/diff checks validate the present design artifact; they do not establish runtime support.
DES-003 completes this bounded transition proposal, not the full Combat/cycle capability gate.

**Verification (2026-09-06):** 193 local link targets, five design/review heading anchors, 22 combined
decision rows and 36 combined acceptance rows passed structural checks across the branch documents.
`git diff --check` passed. Ordinary second-model quality review identified two wording/coverage
defects: the second seal must derive Prepared, and pending selection must explicitly close on trusted
unavailability. Both were corrected with AC-004/007 coverage and verified by that reviewer; no
actionable findings remain in this bounded check. This was not a new independent-review checkpoint.
No .NET build or tests ran because the change is documentation only.
