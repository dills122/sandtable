# CONTACT-001 ZOC and Reaction Source/Ruling Lock

**Status:** Decision-complete research; all five rulings accepted; no production task authorized

**Date:** 2026-08-29

**Decision owner:** Project owner

**Research work item:** `CONTACT-001`

**Proposed implementation predecessor:** `ZOR-TASK-001`

**Governing proposal:** [ZOC and Reaction v1 specification](../specs/zoc-reaction-v1.md) and
[technical design](../design/zoc-reaction-v1.md); owner approval remains required

**Proposed v1 clarification:** the trigger is adjacency to a represented non-phasing combat
element. V1 has no Close Assault declaration, so Rule 8.53(b)'s conditional CPA comparison is
deferred; the accepted visibility policy below remains binding if a later declaration activates it.
Observation 6 additionally proposes one aggregate apparent enemy-controlled-location set, with no
source mapping or rationale, because local Movement legality cannot otherwise remain observation-
derived under Rule 10.21(c).

**Parent research:** [ZOC and Reaction Interruption Spike](contact-reaction-zoc-spike.md)

## Decision boundary

This record closes the five rulings required before a later ZOC/Reaction specification may freeze.
It defines source facts, adopted digital semantics, contract consequences, acceptance vectors, and
the dependency graph. It does not freeze a production contract or authorize implementation.

The complete non-contact Movement vertical remains the predecessor of every production task in
this package. Contact and Engaged remain Sprint 5 participant relationships, but their creation
truths differ: Rule 8.62 derives Contact from enemy-ZOC presence at the beginning of a Movement
Segment, while Rule 8.63 makes Engaged a Close Assault result. Immediate enemy-ZOC entry creates
neither relationship, and neither is a field on an element in this package.

## Executive decision

Reaction is one persisted opponent-owned window created by a committed adjacency-triggering
Movement event.
The reacting player chooses the order in which eligible own representations react. Canonical
candidate order is deterministic but does not decide play order. One representation may open at
most one Reaction episode for one trigger; an episode contains one or more ordinary movement steps.
The same representation may react again to a later adjacency trigger in the same Operation Stage
if it remains eligible. Closing the window declines every remaining opportunity for that trigger
and does not affect later triggers.

The phasing side observes only that an opponent Reaction decision is pending. The reacting side
observes exact own candidates plus the trigger's apparent representation ID and its origin and
destination locations, never its real binding, costs, route adjustments, or intermediate path.
ZOC is a Umpire-derived result over rules-owned predicates, content source facts, current Campaign
state, and topology. Content must not store an `exertsZoc` boolean.

## Ruling table

`Accept` adopts the stated source interpretation or digital ruling for the later specification.
`Reject` identifies an alternative that must not enter the design. `Escalate` would leave a named
owner choice open and block contract freeze. No ruling remains escalated.

| ID | Question | Disposition | Accepted ruling | Rejected alternative |
| --- | --- | --- | --- | --- |
| `ZOR-DEC-001` | Multiple-reactor ordering | **Accept** | The reacting player selects the next eligible own Reaction opportunity. Candidate serialization is canonical by stable opportunity/action identity; it is not an automatic resolution priority. Once selected, that participant completes its episode before another begins. | Umpire-selected order, hidden strength/CPA priority, map enumeration order, or simultaneous mutation |
| `ZOR-DEC-002` | Repeat Reaction eligibility | **Accept** | One representation may open at most one episode per trigger window. An episode contains one or more legal movement steps and ends explicitly. A later committed enemy move creates a new trigger, and the representation may react again during the same Operation Stage if all current restrictions still pass. | One Reaction per Operation Stage; treating every adjacent step as an independent repeat opportunity; reopening a completed participant in the same window |
| `ZOR-DEC-003` | Decline scope and persistence | **Accept** | One explicit close/decline action closes the entire current window and declines all unresolved opportunities. The closure is permanent for that trigger only. A deterministic timeout/unavailable fallback uses the same authoritative closure with a distinct internal closure reason, no CP/BP/RNG change, and no revealed eligibility list. | Mandatory per-unit pass actions; a decline that survives into later triggers; silent timeout advancement; automatic Reaction routes |
| `ZOR-DEC-004` | Waiting and hidden-information visibility | **Accept** | The phasing side receives only a generic `awaiting-opponent-reaction` state and retains its already-authorized own Movement candidate/ledger facts. The reacting side receives exact own opportunity membership plus the trigger's apparent representation ID, origin, and destination, but not the trigger's real binding, costs, route adjustments, intermediate path, hidden excluded-unit reasons, or internal closure/fallback reason. The phasing side receives no eligible-reactor list or count. | Publishing the authoritative window, real binding, eligibility rationale, or opponent path/cost internals; stripping already-known own Movement facts from the phasing side |
| `ZOR-DEC-005` | Positive-ZOC vocabulary and authority | **Accept** | Rules own the source-cited ZOC predicate and exclusions; Content owns immutable primitive source facts; Campaign owns current location, representation/attachment binding, Cohesion, and current combat capability; the Umpire derives controlled edges/hexes. The outward `exertsZoc` value remains a derived apparent fact. The first positive fixture uses two combat battalion-equivalents in one hex with a combined stacking value greater than one and at least ten current raw defensive Close Assault Points. | CPA, organization alone, a content boolean, an observation field, or an Intelligence proposal as ZOC authority |

## Source index and precedence

| Source | Exact locator | Decision use |
| --- | --- | --- |
| [1979 Land Game rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) | PDF pages 13-15; 8.0, 8.11-8.25, 8.5-8.56 | Ordinary movement sequence, enemy-ZOC stopping, reacting-player ownership, repeat Reaction, individual restrictions, CP expenditure, and route extent |
| [1979 Land Game rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) | PDF pages 17-18; 9.11-9.29, 10.0-10.29 | Stacking/equivalence inputs, ZOC qualification/exclusions, edge projection, non-accumulation, and the limited disclosure duty |
| [1979 Land Game rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) | PDF pages 32-34; 21.21-21.29, 21.37 | Reaction movement contributes to stage-cumulative Breakdown state |
| [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf) | 10.3 | The ZOC-combat requirement does not apply to the non-phasing player; do not import that duty into Reaction eligibility |
| [Movement Foundation specification](../specs/movement-foundation-v1.md) | `MOV-REQ-005` through `MOV-REQ-011`; owner approval | Existing apparent-presence, exact-state, stale-submission, canonical-contract, fog, and replay boundaries |
| [Movement Foundation design](../design/movement-foundation-v1.md) | Fog invariant; sequence behavior; Tasks 007-010 | Movement must commit/replay before this package and current global positive-ZOC fail-closed behavior is temporary |
| [Reconnaissance/contact research](recon-contact-knowledge-spike.md) | representation/knowledge split; movement/contact dependency | Real force truth, apparent representation, and side knowledge remain distinct |
| [Planning PR #70](https://github.com/dills122/sandtable/pull/70) | `be1ed98`, “Current parallel execution window” | Decision-only CONTACT lane may proceed in parallel; no Core implementation precedes completed non-contact Movement |

The official rules scan was visually checked at the cited PDF pages. The errata is silent on Rules
8.5 and 10.1-10.2 and changes only the Rule 10.3 applicability noted above. Repository sources take
the normal precedence for current architecture and compatibility, but they do not override the
adopted ruleset.

## Evidence ledger

### Documented facts

- Rule 8.14 and Rules 10.22-10.24 end ordinary movement on enemy-ZOC entry and constrain later exit.
  The triggering movement must therefore be authoritative before Reaction legality is evaluated.
- Rules 8.0 and 8.11 move units one at a time or as a stack. Rules 8.51 and 8.53 make Reaction
  movement by a friendly non-phasing unit in response to an adjacent enemy mover. The source does
  not prescribe an automatic order among multiple eligible units.
- Rule 8.52 allows a unit to React repeatedly during an Operation Stage and charges CP. Rule 8.55
  permits ordinary Reaction movement without a fixed range while prohibiting enemy-ZOC entry.
  Separating an episode from its individual digital movement steps preserves both facts.
- Rules 8.53 and 8.56 require eligibility to be evaluated for the individual reacting unit and its
  current parent/attachment situation. Rule 8.53 also bars specified units, adverse CPA
  relationships, existing enemy-ZOC presence, combat, and Engaged state.
- Rules 10.11 and 10.15 require both a stacking/organization basis and sufficient raw defensive
  Close Assault capability. Rules 10.12-10.14 exclude named categories, informational markers, and
  Cohesion `-26` or worse. Rules 10.21 and 10.28 make control topology-sensitive and non-additive.
- Rule 10.16 creates only a conditional duty to disclose whether a located unit could exert a ZOC;
  it does not disclose the force's real binding, statistics, or Reaction eligibility basis.
- Rules 21.22-21.29 make Reaction movement part of stage-cumulative Breakdown accounting. Reaction
  cannot use a separate or approximate movement ledger.

### Repository observations

- `CampaignObservation` contract 5 orders apparent opposing presences by opaque representation ID,
  carries an audience-visible state version, and exposes only location plus `exertsZoc`
  (`src/Cna.Core/Observations/CampaignObservation.cs:6-155`).
- The projector currently hard-codes every apparent ZOC value to `false`; real bindings remain in
  internal `CampaignMapRepresentationState`
  (`src/Cna.Core/Observations/CampaignObservationProjector.cs:148-171` and
  `src/Cna.Core/Campaigns/CampaignMapRepresentationState.cs:5-53`).
- Dormant Movement derivation currently suppresses every move when any apparent positive ZOC exists.
  This is an intentional global fail-closed placeholder, not local Rule 10 topology
  (`src/Cna.Core/Actions/CampaignMovementActionDerivation.cs:62-74`).
- Current rules data derives a battalion stacking value of one and has no combat-classification or
  raw defensive Close Assault vocabulary (`src/Cna.Core/Rules/Cna1979Movement.cs:92-109` and
  `src/Cna.Core/Content/ContentForces.cs`).
- Legal action sets reject duplicate candidate IDs and serialize deterministically by kind and
  action ID (`src/Cna.Core/Actions/CampaignLegalActionSet.cs:11-47`). Submissions validate campaign,
  exact state version, position, audience membership, and action identity before dispatch
  (`src/Cna.Core/Actions/CampaignActionExecution.cs:48-93`).
- The current event and snapshot contracts carry state version but no persisted opponent window,
  suspended phasing position, movement-ended participant state, or Reaction event
  (`src/Cna.Core/Campaigns/CampaignEvent.cs` and `CampaignSnapshot.cs`).
- Tests freeze the three-field apparent-presence allowlist, hidden-binding absence, false-only ZOC,
  same-observation action equivalence, positive-ZOC fail-closed behavior, and movement action IDs
  (`tests/Cna.Core.Tests/Observations/CampaignMovementObservationContractTests.cs:100-220`,
  `CampaignObservationPrivacyTests.cs:15-114`, and
  `tests/Cna.Core.Tests/Actions/CampaignMovementActionDerivationTests.cs:200-258`).

### Adopted digital rulings, not source quotations

- A trigger window is the digital boundary for one committed adjacency event. The physical rules do
  not define server retries, state versions, candidate serialization, or timeouts.
- A selected participant's multi-step Reaction is one episode. This avoids changing the source's
  unlimited normal Reaction route into repeated eligibility decisions caused only by an adjacent
  step API.
- A single close action declines the remainder. It preserves the player's ability to react with any
  chosen subset without forcing revealing or bookkeeping-only pass decisions for every eligible
  unit.
- Waiting text and redaction are a Sandtable fog policy. They implement, but are not verbatim rules
  language.

## Later contract requirements

These are specification inputs, not frozen type names or serialized shapes.

### Stable identity

The accepted triggering Movement event must deterministically establish one `reactionWindowId` from
a canonical preimage containing at least:

- campaign and ruleset identity;
- the triggering event's committed state version;
- triggering Movement contract identity;
- authoritative triggering representation identity;
- origin and destination; and
- reacting side.

A Reaction trigger is every committed phasing combat-element move whose destination is adjacent to
at least one authoritative non-phasing represented combat element. Trigger qualification is
adjacency under Rule 8.51, not whether that combat element exerts a ZOC and not whether any reactor
survives Rule 8.53's eligibility filters. A committed move with no adjacent non-phasing represented
combat element creates no window.

An eligible participant receives a stable `reactionOpportunityId` derived from the window identity
and its authoritative reacting representation identity. An outward action ID additionally binds the
current action semantics. Real element bindings remain internal. Candidate arrays use ordinal
canonical ordering; event order records the reacting player's actual choices.

Exact own Reaction-opportunity membership is an approved derived fact in the reacting-side window
observation. When an approved phasing Close Assault declaration exists, it may depend on frozen
authoritative eligibility inputs that are not themselves outward, including Rule 8.53(b)'s
conditional comparison with the triggering enemy mover's CPA. The observation and candidate may
disclose that the reacting representation has an opportunity; they must not disclose the enemy CPA,
threshold calculation, failed alternative, real binding, or exclusion reason. The proposed v1 has
no declaration and therefore does not apply this conditional comparison.

### Persisted state machine

```text
phasing Movement at version N
        |
        | accepted adjacency-triggering ElementMoved
        v
open Reaction window at N+1, reacting side owns actions
        |
        +--> empty opportunity universe: deterministic system close
        |                                     |
        |                                     v
        |                              resume phasing Movement
        |
        +--> start/continue one participant episode --+
        |                                              |
        |<---------- complete participant -------------+
        |
        +--> select another unresolved opportunity
        |
        +--> close/decline all remaining opportunities
        v
closed window, resume the exact suspended phasing Movement position
```

The opening state records the stable window/trigger identity, source committed version, phasing and
reacting sides, suspended Movement position, authoritative trigger binding, apparent trigger
projection, frozen opportunity universe and trigger-time evidence, resolved participants, and
optional active participant. Each current action is still regenerated and revalidated against the
latest state. A live query against later mutable world state must not add a participant that was not
eligible for the original trigger or rewrite the historical opportunity universe.

If the frozen opportunity universe is empty, the persisted window still exists at version `N+1`.
The next transition is one deterministic system-authored `ReactionWindowClosed` event at `N+2`
with internal reason `no-eligible-reactor`; it changes no CP, BP, or RNG and resumes the suspended
phasing Movement position. No human, worker, or model decision is requested. If the intermediate
state is observed, the phasing side receives the same generic waiting shape used for every other
window; outward history contains neither the empty count nor the internal reason.

Every accepted Reaction step, participant completion, or window closure emits exactly one semantic
event and increments state version once. Projection must atomically update location, CP, BP,
representation, participant/window state, and the resumed position as applicable. No authoritative
turn remains open while waiting for a human, worker, model, or timeout.

### Stale, duplicate, and replay behavior

- Every submission binds campaign, expected state version, current Reaction position/window,
  audience, opportunity/action identity, and exact current membership.
- A submission from any earlier version, completed participant, closed window, wrong audience,
  changed route, or no-longer-legal state rejects with zero events and no state/RNG change.
- Retrying a previously accepted submission against its old version is stale and cannot emit a
  duplicate event. Replaying the same event twice is invalid history because the prior version and
  window transition no longer match.
- Replaying creation through the trigger, every Reaction step/completion, closure, and resumed
  Movement must reproduce byte-identical snapshot and side-projection bytes.
- The opening event records the eligibility basis needed for replay; later events use stable IDs
  rather than list positions or mutable enumeration order.

### Decline and fallback

One close event records the window identity, prior state version, reacting side, and an internal
closed reason such as `player-declined` or `scripted-unavailable`. Both outcomes:

- close all unresolved opportunities for that trigger;
- consume no CP, BP, or RNG;
- reveal no eligible list, count, or reason to the phasing side; and
- resume the exact suspended phasing Movement position.

If the player reacts with a subset and then closes, only the unresolved remainder is declined. A
later adjacency-triggering Movement event creates a new window and recalculates eligibility from
its new committed pre-state.

### Truth and fog-safe projections

| Fact | Authoritative truth | Reacting-side projection | Phasing-side projection |
| --- | --- | --- | --- |
| Real trigger binding | Exact representation/element binding in Umpire event/state | Never | Never |
| Window identity/state | Exact trigger, sides, eligibility basis, active/resolved set | Stable public window ID and current own decision state | Stable public window ID and generic waiting/closed state |
| Eligible reactors | Exact frozen authoritative basis | Exact own legal opportunities | Absent, including count |
| Apparent enemy-controlled locations | Exact non-additive derived set and source evidence | Aggregate controlled location IDs only; no source mapping or rationale | Aggregate controlled location IDs only; no source mapping or rationale |
| Triggering force | Exact real binding/path/cost | Apparent representation ID plus origin and destination only | Retains already-authorized own Movement candidate semantics and post-move ledger facts; the waiting shape adds no opponent fact |
| Decline/fallback reason | Exact internal closure kind | Absent from observation and projected history; an explicit player action remains known through its own submission/receipt | Absent from observation and projected history |
| Chronicle | Complete event truth | Separate redacted projection | Separate redacted projection |

Two authorities that produce byte-identical facts for an audience must produce byte-identical
observations and action sets for that audience. For the reacting side, exact own opportunity
membership is itself an approved derived observation fact. If a future declaration-aware contract
activates Rule 8.53(b), a hidden enemy-CPA change may add or remove that membership, but the
observable delta is confined to the own opportunity identity and matching candidate membership.
Candidate payloads, rejection reasons, and projected history still contain no enemy CPA, threshold,
binding, or exclusion reason. The same hidden eligibility change must not alter the phasing side's
observation or generic waiting shape.

## Positive-ZOC vocabulary and first fixture

The minimum source-faithful inputs are:

| Owner | Primitive or derived fact | Rule basis |
| --- | --- | --- |
| Rules | Closed unit-category/qualification predicate and exclusions | 10.11-10.15 |
| Rules | Stacking-point derivation and aggregation; total greater than one qualifies the stack basis | 9.11-9.29; 10.11 |
| Content | Explicit source/synthetic unit category and initial raw defensive Close Assault capability; no derived ZOC boolean | 10.11-10.15 |
| Campaign | Current representation/attachment binding, location, Cohesion, current raw defensive Close Assault capability, and participant state | 10.11, 10.14-10.16 |
| Rules + topology | Edge projection exclusions and whether the source hex's force could enter the adjacent hex | 10.21 |
| Umpire | Derived ZOC source qualification and controlled adjacent locations for the exact current state | architecture boundary |
| Observation | Apparent representation ID, apparent location, and derived `exertsZoc` only | approved `MOV-DEC-002` boundary |

`ZOR-TASK-002` should use two independent synthetic combat battalion-equivalents stacked in one
permitted hex. Each retains the existing stacking value `1`; their aggregate is `2`. Their admitted
current raw defensive Close Assault values must total at least `10`. The fixture must explicitly
identify them as combat rather than inferring qualification from CPA, mobility, or organization.

Required negative vectors are: one battalion alone; aggregate stacking value no greater than one;
raw defensive Close Assault total below ten; Cohesion `-26`; excluded/non-combat category; unattached
HQ; All-Sea/Major-River/Lake edge; Escarpment edge; and an adjacent location the source force cannot
enter. Multiple qualifying sources into one location still produce one non-additive controlled fact.

The exact field/type names and content/world version migration belong to the later specification.
Do not add a convenient `exertsZoc` field to Content or treat base CPA as combat capability.

## Proposed dependency and acceptance graph

```text
CONTACT-001 / ZOR-TASK-001 decision lock [this record: complete]
        |
        | decision artifact may merge now; production remains gated
        v
complete MOV-TASK-007 -> 008 -> 009 -> 010 Movement vertical
        |
        v
owner-approved ZOC/Reaction specification + technical design
        |
        v
ZOR-TASK-002 rules/content vocabulary + positive/negative fixture
        |
        v
ZOR-TASK-003 window, movement-ended, snapshot, event, and replay contracts
        |
        v
ZOR-TASK-004 dormant observation/action/decline contracts + fog equivalence
        |
        v
ZOR-TASK-005 internal move-to-window trigger; public membership still dormant
        |
        v
ZOR-TASK-006 Reaction movement, CP/BP, episode completion, close, resumption,
             and atomic publication
        |
        v
ZOR-TASK-007 Exercise/Maneuver evidence, synchronization, and independent review
        |
        v
Sprint 5 Contact/Engaged combat-cycle design gate
```

The production tasks remain proposed. The governing technical design refines them into lettered,
bounded slices without changing these stable parent IDs. Owner approval is still required, and the
refinement must not bypass dependency order or move Contact/Engaged into Sprint 4.

### Acceptance consequences for the later specification

1. Two or more eligible reactors appear in canonical candidate order, while two different valid
   player selection orders produce their respective deterministic event orders and replay exactly.
2. One participant can take multiple legal movement steps in one episode but cannot start a second
   episode in the same window after completion.
3. The same participant can react in a later trigger during the same Operation Stage if current
   restrictions pass; changed CP, BP, Cohesion, position, ZOC, combat, Engaged, or attachment facts
   can make it ineligible.
4. Closing before any Reaction or after a subset closes every remaining opportunity only for that
   trigger, preserves CP/BP/RNG, and resumes the suspended phasing position.
5. Scripted unavailable/timeout closure is deterministic and replay-identical to its retained
   event; it never synthesizes a route.
6. A committed move adjacent to a non-ZOC-exerting represented combat element still creates a Reaction
   trigger. A trigger with zero eligible reactors creates the same generic window at `N+1`, closes
   through one deterministic no-cost/no-RNG event at `N+2`, and exposes neither count nor reason.
7. Phasing-side observations are byte-identical for hidden eligible-reactor permutations and
   counts. When a later declaration-aware contract activates Rule 8.53(b), a hidden enemy-CPA
   threshold change affects the reacting-side observation only through exact own opportunity
   membership and the matching candidate set; neither artifact exposes CPA, threshold, binding,
   failed alternatives, or exclusion reason. The proposed v1 explicitly defers that conditional
   behavior rather than applying it without a Close Assault declaration.
8. The reacting-side trigger projection contains only apparent representation ID, origin, and
   destination. Trigger costs, route adjustments, intermediate path, binding, and closure reason
   are absent. The phasing side retains its already-received own Movement candidate/cost semantics
   and post-move own ledger facts; the waiting projection removes none of them.
9. Stale, duplicate, wrong-window, wrong-side, forged-identity, completed-participant, and closed-
   window submissions emit zero events.
10. A positive fixture controls exactly the permitted adjacent locations; every named qualification
   and topology negative fails independently.
11. Reaction CP and BP deltas reuse the approved exact amounts, rules provenance, stage ledger, and
   atomic event/projector/replay path. No separate Reaction accounting shortcut exists.
12. Authoritative events retain real bindings and rule evidence; both player histories use separate
    redacted projections and contain no hidden eligibility or fallback reason.

## Rejected approaches

| Approach | Reason |
| --- | --- |
| Automatically sort and execute all reactors | Removes the non-phasing player's original choice and makes hidden data an authority priority |
| Treat each adjacent Reaction move as a new trigger | Converts an API step size into repeat eligibility and can create unbounded artificial windows |
| Require an explicit decline for every eligible unit | Adds bookkeeping decisions and can leak the hidden size of the authoritative eligible set |
| Keep the phasing action open while the opponent decides | Violates persisted authority, replay, and remote-I/O boundaries |
| Derive old eligibility from the current world during replay | Later movement or state changes can rewrite the historical decision surface |
| Persist `exertsZoc` in Content | Stores a topology- and state-dependent result as immutable source data |
| Use organization or CPA as a combat proxy | Fails Rules 10.11-10.15 and the repository's explicit source-fact boundary |
| Add Contact/Engaged as generic element status | Conflates immediate ZOC interruption with participant relationships whose source lifecycle differs: Contact at Movement-Segment entry and Engaged from Close Assault |

## Uncertainties and limitations

- The physical rules do not define digital window IDs, stale retries, timeout behavior, canonical
  action ordering, or player-history redaction. `ZOR-DEC-001` through `005` explicitly adopt those
  engineering semantics; they are not represented as source facts.
- The first positive fixture proves only the supported synthetic combat-battalion stack. It does not
  normalize every unit category, HQ attachment case, published TOE, anti-tank composition, or
  current loss-dependent Close Assault calculation.
- Rule 8.53's CPA comparison, attached-unit Reaction, detachment, pinning, combat/Engaged exclusion,
  and Truck special cases need separate source vectors before those categories are admitted.
- Exact human-facing prose may vary, but observation/history contracts must never expose the
  internal closure reason. The reacting player's own explicit decline remains knowable only through
  its submitted action and receipt.
- The contract does not claim that wall-clock duration is indistinguishable between an automatic
  empty-window close and a human-owned window. It fixes canonical state/event/projection content and
  forbids count/reason fields; later hosting must assess timing metadata separately.
- The five rulings are accepted and synchronized into the central roadmap and proposed governing
  ZOC/Reaction package. This retained ruling lock remains source evidence rather than implementation
  authorization.
- Planning PR #70 was verified from the local remote-tracking commit `be1ed98`. Authenticated GitHub
  readback was unavailable because the configured Keychain credential was invalid.

No unresolved source-ruling choice blocks implementation planning. The non-contact Movement vertical
is complete; production remains blocked by explicit owner approval of the proposed ZOC/Reaction
specification/design package.
