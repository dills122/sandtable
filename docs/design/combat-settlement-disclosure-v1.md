# Combat Settlement and Disclosure

**Status:** `CMB-DES-005` bounded design complete for review; production remains gated.
**Date:** 2026-09-06. **Input checkpoint:** `0a1cf99`.

This packet consumes [DES-004 result evidence](combat-cost-resolution-order-v1.md), preserves
[DES-001 identity](combat-opportunity-identity-v1.md), and closes the
[DES-002 round](combat-sealed-decision-protocol-v1.md) before
[DES-003 step completion](combat-step-transitions-v1.md). It defines loss/retreat/custody ordering,
mandatory-choice fallback, final relationships and proposed disclosure for the singleton infantry
exercise. It does not activate a contract or approve the research policies. Cycle/Reserve composition
and combined schema/implementation-plan review remain next.

## Evidence and admitted settlement

| Evidence | Required consequence |
| --- | --- |
| [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), PDF p13, 6.21-6.24 | CP excess and loss DP immediately lower Cohesion; a proved assault victory grants participating attacker 3 RP, capped at +10. |
| Land rules, PDF p24-25, 15.74-15.87 | Retreat outranks raw Engaged even with zero losses; refusal adds ten percentage points per unfulfilled hex before asymmetric rounding. Captured personnel form a subset of loss. Actual TOE loss of at least 30% triggers 3 DP. |
| Land rules, PDF p24, 15.81 | Surviving original participants still adjacent after assault have Contact when not Engaged. Movement-entry ZOC Contact is a different provenance. |
| Land rules, PDF p40, 28.11-28.25 | Infantry capture becomes prisoners; initial relocation is at most three hexes. Guards require infantry TOE transfer; unguarded/excess prisoners escape. Nearby escapees leave play and become later replacements, not immediate unit strength. |
| [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf), 28.17 | Moving custody needs one guard per five prisoners, not per ten. |
| [Mutable-state research](../research/combat-mutable-state-spike.md) and [RNG goldens](../research/combat-rng-golden-spike.md) | Selected surface includes both capture roles, one-hex retreat/refusal, 30% actual loss and zero-loss Engaged. All such branches need closure. |

Land pages 13, 24, 25 and 40 were visually checked; errata 28.17 checked as text. Digital event
boundaries, default choices and the custody rendezvous convention below are explicit proposals.
The sources specify consequences, not these software transactions or disclosure rules.

Retain DES-004's units, 10 committed TOE per side, one-use ammo and pre-roll Cohesion 0. Add a
public exercise geometry certificate: one approved one-hex Clear retreat destination, farther from
the attacker, along a least-resistance direction toward the defender's nearest friendly supply
dump/city. Cost is exactly 1 CP; no hexside surcharge, city exception, enemy unit/ZOC, attachment,
vehicle, pin or Breakdown work intervenes. Destination is on-map and cannot change during settlement.
This bounded route is an exercise capability, not a claim that other source-legal routes are illegal.

Before selection, certify all potential casualty/capture/retreat outcomes, not the seeded outcome:
both original units survive (at least seven TOE before guard transfer), possible prisoner relocation
has the bounded legal route described below, and escapees can reach their surviving original unit
within eight CP under 28.24's distance test. Exclude existing guards/custody and unsupported escape
or surrender cases. Structural scenario restrictions must prove absence of hidden blockers across
all worlds allowed by the same public profile; inspecting one hidden world then suppressing its
choices does not prove side-safe legality. Profile, timing and disclosure policies require approval.

## Settlement decisions

| ID | Decision |
| --- | --- |
| `CMB-SET-001` | Derive one ordered obligation chain from the committed result. Only live typed decisions may wait; all other transitions are system continuations. No postcommit cancellation. |
| `CMB-SET-002` | Resolve defender retreat intent before final casualty arithmetic, then apply both sides' losses and loss DP atomically before actual retreat movement. Preserve frozen strength and capture evidence. |
| `CMB-SET-003` | Apply the approved retreat or explicit refusal once. Movement adds its exact CP and immediate excess-CP DP; completed target vacating grants victory RP once. Refusal grants no victory. |
| `CMB-SET-004` | Captured TOE becomes an origin-bearing custody lot, never a second loss. Settle a bounded guarded rendezvous or source-required escape before round closure; preserve later feeding/replacement obligations. |
| `CMB-SET-005` | Derive Contact/Engaged only from original surviving participants and final positions, with raw Retreat precedence. Guards and new arrivals inherit no original marker. |
| `CMB-SET-006` | Settlement deadlines cannot cancel an attack. Fixed scripted refusal and unguarded escape close missing choices, with retained reason and replayable timing inputs. |
| `CMB-SET-007` | Project own state, authorized apparent changes and own custody through a field allowlist. Internal event count, causes, rolls, opponent exact values and authoritative IDs remain private. |
| `CMB-SET-008` | Strict replay checks causal receipts, conservation, timing, pending work and projections. Preserve later obligations across Reserve/cycle entry and retain compatible readers for open settlements. |

## Ordered transitions and decision ownership

All events bind commitment/result, original participants, cycle/slot, pinned rules/config, exact
prior/result authority versions and causal receipt. Publication of an event and all its effects
is atomic and advances authority once. Keep the DES-004 post-result RNG cursor unchanged throughout
this settlement; none of these choices draws dice. No Movement/Reaction/other combat may interleave.

| Order / semantic event | Preconditions and effect |
| --- | --- |
| `CombatRetreatChoiceOpened` | Only when result requires one hex. Defender receives exact bounded route and `refuse-retreat`; retain own decision/action mapping and pinned deadline. No world effects. |
| `CombatRetreatDispositionRecorded` | Accept one authenticated current choice or scripted refusal. Retain route, required/completed-planned/unfulfilled distance and own receipt; close decision. No movement or loss yet. With zero required distance, system records `not-required`, without opening a decision. |
| `CombatLossesSettled` | Require disposition. Compute both final losses from original 10-TOE inputs, deduct once, apply each loss-DP cause immediately, and create any positive custody lot in pending state. Neither retreat nor guard transfer can change the loss basis. |
| `CombatRetreatSettled` | Require loss receipt. Apply planned one-hex move and CP/DP, or validated refusal/not-required no-move proof. Record actual completed distance. Award attacker victory RP only on proved full defender evacuation due to this assault. |
| `CombatCustodyChoiceOpened` / `CombatCustodySettled` | If positive prisoners exist, captor chooses bounded guarded rendezvous or `leave-unguarded`; scripted fallback is the latter. Settlement atomically relocates/forms guard or executes escape accounting. No positive lot means no custody decision/event; absence is proved from result/loss evidence. |
| `CombatRelationshipsSettled` | Require losses, retreat and all custody dispositions. Reconcile only original surviving participants; record final relations and complete this packet's immediate obligation chain. |
| `CombatChoiceRoundClosed`, then DES-003 `CombatStepCompleted` | Existing owners validate all immediate receipts and retained future obligations. No new duplicate segment-completion event. Arriving at Reserve Release does not discharge future work or grant a repeat. |

`CombatAssaultResolved` already enters Settling; no additional settlement-opening event is needed.
A choice receipt is intent evidence until its corresponding world event publishes. There is no
external action window between them. Recovery completes the retained disposition; it never asks
again, substitutes a new route or reopens a timer. A forged suffix fails readback rather than being
converted to refusal. Every still-live decision has exactly one permitted owner.

## Loss, retreat and Cohesion arithmetic

For role `s`, let `P_s` be the source table loss percentage and `U` be the defender's unfulfilled
retreat distance (0 or 1). `L_A = ceil(10 * P_A / 100)` and
`L_D = floor(10 * (P_D + 10 * U) / 100)`. Do not round table and refusal losses separately.
For a capture trigger affecting role `s`, `C_s = ceil(L_s * share / 100)`; otherwise `C_s = 0`.
Retain `otherLoss_s = L_s - C_s` and `TOE_afterLoss_s = 10 - L_s`.
A capture trigger with zero rounded loss creates no positive lot or guard/escape decision.

Conservation at the loss boundary is `10 = TOE_afterLoss + captured + otherLoss`. Apply 3 loss DP
iff `100 * L_s >= 30 * committedTOE_s`; captured personnel count toward loss, later guard transfers
do not. Never deduct captured personnel twice, rewrite immutable maximum TOE or net different
Cohesion causes into one anonymous delta. These formulas exploit rating 1 and singleton assignment;
general rating/partial/multiunit allocation remains outside admission.

For the actual retreat use the current cumulative stage CP `E` after combat costs. Add 1 CP and
apply new excess-CP DP immediately: `max(0, E + 1 - CPA) - max(0, E - CPA)`. This fixture's stage
expenditure may be fractional, so admission additionally requires integer E at this settlement
boundary; fractional excess-to-Cohesion policy needs a separate extension. DES-004's equality case
E=10 remains supported: retreat ends at11 and adds1 DP. This happens after both Morale/results and
must not retroactively change their table column. Refusal/not-required spends no retreat CP.

With the admitted route, fulfilled retreat vacates the sole target unit's hex completely and
awards attacker3 RP, after any attacker loss DP, capped at+10. A refusal or merely rolled Retreat
awards none. Retain cause-specific before/after Cohesion in the movement receipt even if loss DP
and victory RP sum to zero. The original units cannot reach surrender thresholds in this bounded
surface. Post-use zero ammo is still DES-004's paid-use state, not a new assault/surrender trigger.

Synthetic anchors: seed208's refusal yields defender3 loss/3DP; attacker25% yields3 loss/3DP;
seed31707 can yield attacker3 loss including2 captured, then actual defender retreat earns
attacker3RP. If captor later forms one guard, its own post-loss TOE falls by one for transfer,
without changing either side's already-assessed combat-loss percentage.

## Custody, guard transfer and escape

Positive capture is at most three Prisoner Points in one lot and may belong to either side.
Retain original side/component, captor, result/loss receipt, quantity and capture-origin location.
**Proposed location convention:** origin is the victim's pre-retreat hex; prisoner movement is
separate from surviving victim movement. This digital convention and the rendezvous sequence
below need owner approval before activation; no source sentence is claimed to prescribe them.

Bounded `relocate-and-guard` selects the single certified initial relocation route from that origin
to the captor's post-retreat infantry location, within three hexes. It may leave the capture-origin
hex despite remaining enemy survivors there; no entered hex may be enemy occupied/controlled or
impassable. This is 28.12's initial free prisoner relocation, not a generic infantry/convoy command.
The route and destination must be certified for either capture role and either retreat disposition.
No prisoner transport vehicle, guard marching or field detention camp is silently simulated.

Publish initial relocation and guard formation as one transaction: at the destination transfer
one surviving infantry TOE from the co-located captor into one distinct guard asset, retaining
origin component, location, custody link and source guard characteristics. Transfer zero carried
ammo from the exhausted donor; retain its readiness provenance, current stage expenditure and
Cohesion in the guard asset rather than minting fresh supply or a reset stage ledger. No guard teleports from
another hex; no unguarded intermediate location becomes an actionable World state. This is the
proposed atomic rendezvous policy, not proof that arbitrary unescorted prisoner movement is legal.
The alternative remains `leave-unguarded` at the capture origin, with no relocation or guard cost.
The restricted exercise does not advertise every source-legal relocation/guard option.

One guard suffices for the lot under the corrected moving ratio of1:5; static custody still needs
a guard. Guard formation is `postLossTOE = remainingCombatTOE + guardTOE`, not death, another
captured subset or loss-DP input. Asset identity is derived once from the custody receipt and
origin; it does not replace the original rules-unit key or inherit Contact/Engaged. Guard units
have source CPA10 and assault ratings0/1; their future movement, combat, return-to-origin and
observation representation need explicit capability/schema support. Do not fake a second infantry
component in current World6 to evade that contract work.

`leave-unguarded` makes all lot members escape immediately under28.23. Keep capture and escape
as different historical facts. The admission certificate proves the original surviving friendly
unit is reachable within8 CP under28.24 (ignore enemy units/ZOC for this distance test). Bind that
unit as the deterministic reunion recipient; this is a source-permitted scripted selection, not
a shortest-hidden-ID tiebreak. Record reunion, removal from map custody, quantity and original
side's replacement entitlement due one game month later. Never restore its current TOE immediately
or discard its captured-loss provenance. The combined calendar contract must pin the due-date
calculation; wall-clock time cannot mature this entitlement. Far-away wandering/recapture/water
attrition cases require a different certified profile before the assault.

Guarded prisoners remain current custody with guard link/location and a later priority feeding
obligation. Escape reunion retains the replacement entitlement. These are settled *immediate*
consequences, not empty World state. CYCLE-DES-001 must carry them through Reserve/cycle snapshots
and prohibit crossing any unsupported upkeep, guard-action or replacement-maturity boundary.
A design that merely saves a pending obligation forever cannot claim a complete repeating game.

## Deterministic fallback and restoration

Use DES-002's pinned finite budget, trusted time, strict deadline equality and own receipt rules
for each actual retreat/custody decision. Policy is explicit: expired/unavailable retreat chooses
`refuse-retreat`; expired/unavailable custody chooses `leave-unguarded`. Record system authorship,
live decision identity and private cause, then execute the same source consequences as those
choices. Controller loss never cancels, refunds, rerolls or leaves a committed round waiting forever.
The default is a proposed scripted policy, not a printed-rule penalty for silence.

An accepted disposition makes old timeout/unavailability commands no-ops. A zero-distance/zero-lot
branch creates no player decision/deadline. Clock regression or loss of confidence invokes the
same trusted unavailable fallback; retries/restart do not renew time. System transitions proceed
without a model. Restore validates state-specific pending variants, owner/candidate/time bindings,
ordered predecessor receipts, exact quantities/CP/Cohesion and unchanged result/RNG. Invalid
source evidence is an operational fault, not authorization to invent a different fallback.

## Final relationships and authorized disclosure

First evaluate original pair membership and surviving positions. A raw Retreat result suppresses
raw Engaged even when retreat was refused; if survivors still remain adjacent they receive
post-assault Contact. With no Retreat, raw Engaged yields Engaged; otherwise adjacent survivors
receive Contact, and separated survivors have neither. Zero losses do not suppress these tests.
No transitive merge or marker inheritance for the newly formed guard is allowed. Preserve attack
history/target use; stage-end Engaged expiry and later movement break-off remain separately owned.

Proposed field allowlist for side projections:

| Audience fact | Allowed payload / boundary |
| --- | --- |
| Own participant | Exact own current TOE, costs, Cohesion changes and own origin-bearing losses; own accepted decisions/receipts use side-safe references. |
| Own retreat decision | Own required distance, approved route/refusal and exact own cost; no opposing roll, table column or hidden blocker diagnostics. |
| Captor's custody | Own prisoner count, guard asset/transfer, authorized location and own custody options; prisoner origin side/class allowed, raw opposing unit/component identity withheld unless already authorized. |
| Original owner's escapes | Own lost/captured quantity and reunion/replacement entitlement; no hidden enemy guard choice or custody route history. |
| Apparent enemy and relation | Only approved apparent location/marker membership and movement updates from the established observation policy; Contact/Engaged membership is an explicit authorized relation fact, not exact enemy strength. |
| Shared lifecycle | Approved structural phase progress and generic waiting/closed status; no internal event/slot counts, timed-out party, private settlement substage or cause. |

Raw rolls, differential, enemy TOE/ammo/CP/Cohesion, seed/cursor, authoritative versions/hashes,
real opposing bindings and raw result/settlement receipts remain Umpire-only. Chronicle authority
retains them; War Diary/Runner/Dispatch consume typed projected records, never format raw events.
Own replacement entitlement may reveal an escape occurred: that is an explicit proposed disclosure,
not permission to expose all enemy custody actions. Likewise later phase entry can imply accepted
RBA decline on this restricted path; approving that implication is part of this profile's policy.

Each audience revision changes only when its allowed projection changes; internal steps do not
create empty numbered side events. Compare equal authorized histories, including deliberately
released facts, across hidden-world variants: candidate membership/order, references, acceptance
and rejection, waiting/closed shapes and side-log bytes must match. A generic error does not fix
an oracle if command success differs. Public profile restrictions must eliminate hidden-dependent
route/guard/admission branches, or that profile cannot activate. Timing/traffic equivalence needs
hosted tests and transport policy; this document does not claim constant-time behavior.

Current [disclosure alias logic](../../src/Cna.Core/Observations/CampaignObservationV6DisclosureIdentity.cs)
rejects indistinguishable capabilities, and [action derivation](../../src/Cna.Core/Actions/CampaignObservationV7ActionDerivation.cs)
validates the submitted side-visible candidate and revision. These are reusable boundary patterns,
not Combat projections. Combined freeze must define exact new unions/codecs, approved public
fields, guard/custody/replacement representation, calendar semantics and migrations before runtime.

## Acceptance and verification plan

| Case | Required observation | Decisions |
| --- | --- | --- |
| `CMB-SET-AC-001` | Admission covers every role/result/retreat/custody branch, certified route/reunion geometry and integer settlement CP. Hidden-blocker or unsupported variants fail profile certification before choice; no seed narrows results. | 001/007 |
| `CMB-SET-AC-002` | Refusal adds percentage before rounding; seed208 yields3 defender loss/3DP, while fulfilled retreat changes that result. Capture subset plus other losses conserve original10 TOE. | 002 |
| `CMB-SET-AC-003` | Guard transfer never counts toward30% loss DP. Loss3 plus captured2 deducts3, not5; subsequent one-TOE guard transfer preserves a separate asset balance. | 002/004 |
| `CMB-SET-AC-004` | Actual one-hex retreat spends1 CP; E=10->11 adds1DP after result. Refusal spends none. Frozen Morale/RNG do not change; DP then proved victory RP retain both causes. | 002-003 |
| `CMB-SET-AC-005` | Zero-distance produces no decision; real retreat/refusal takes exactly one disposition/loss/movement path. Lost replies, duplicates and stale timers cannot choose twice or move twice. | 001/003/006 |
| `CMB-SET-AC-006` | Captures on either side settle with valid initial route and co-located one-TOE guard transfer, or immediate unguarded escape. No off-map guard, extra prisoner debit or unguarded transient action is possible. | 004 |
| `CMB-SET-AC-007` | Escape reunites with certified friendly recipient, removes current custody and creates delayed replacement entitlement without restoring TOE. Guarded branch retains priority feeding and guard obligations across closure. | 004/008 |
| `CMB-SET-AC-008` | Deadline equality, controller loss and clock regression produce refusal/unguarded fallback once. Accepted choices defeat later timers; committed attack cannot cancel or renew deadlines. | 006 |
| `CMB-SET-AC-009` | Raw Retreat suppresses Engaged even on refusal; adjacent survivors become Contact. Zero-loss Engaged remains when no Retreat; guard/new arrivals never inherit original relation. | 005 |
| `CMB-SET-AC-010` | Cuts before/after every event restore the same pending owner/capability, balances, entitlements, receipts and RNG. Wrong order, forged absence proof or altered before/after quantities fails readback. | 001/008 |
| `CMB-SET-AC-011` | Own-only changes advance only own projection; approved shared facts advance affected audiences. Hidden-equivalent variants preserve candidates, success/errors and side bytes, with no raw authority receipt in Runner/Dispatch/War Diary. | 007 |
| `CMB-SET-AC-012` | No immediate pending loss/retreat/custody/relation allows round closure; later upkeep/replacement records survive. Reserve entry does not prove supported repeat or production readiness. | 001/008 |

These are future executable tests. Source/state-trace checks and research arithmetic validate
this proposal, not World schemas, full chart entry, hosted recovery or simulator behavior.
[CYCLE-DES-001](continual-cycle-reserve-composition-v1.md) now composes these obligations with
Reserve Release and cycle control. Owner policy rulings and combined contract/implementation-plan
review remain next.

**Checks (2026-09-06):** existing research verifiers passed 12 seeded RNG vectors, eight loss
boundary vectors, 108 conditional arithmetic combinations and three guard-capacity probes.
Local checks resolved 189 path links across the six cumulative branch documents and validated
15 ordered decisions/24 acceptance IDs for DES-004/005. `git diff --check` passed. Source and manual
state traces covered refusal, paid retreat, both capture roles, guarded/escape closure and zero-loss
relationships. No .NET tests ran for this documentation-only design; independent review follows.
