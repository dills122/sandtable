# Inherited Movement successor preparation

Status: preparation for `CMB-TASK-003D2c.3`, baseline `4c10ede`, 2026-09-08.
One bounded planning artifact; no successor payload freeze, implementation, positive campaign
admission or completed .3 checkpoint. [Combined plan](combat-cycle-implementation-plan.md)
retains its accepted dependency graph. This packet adds field/replay obligations to the
[D2c.1 declaration inventory](../specs/combat-inherited-successors-v1.md); it does not replace it.

Scope: `element-moved`3→4 and `movement-segment-completed`2→3. Reaction3 and Breakdown2
are separate inherited families. Bound each implementation packet before edits; retain the
five-primary-file cap. Sources below describe historical Rules9/World6/Snapshot11/sequence4
behavior. Their presence is not evidence that Rules10/World7/Snapshot12/sequence5 already works.

## Exact legacy field matrix

These rows exhaust the top-level property sets accepted by `ParseMoved` in
[move codec](../../src/Cna.Core/Campaigns/CampaignBreakdownMoveEventSerializer.cs), lines105–131,
and `ParseMovementSegmentCompletedV2` in
[lifecycle codec](../../src/Cna.Core/Campaigns/CampaignBreakdownLifecycleCodec.cs), lines168–180.
Grouped fields remain separately present on the wire; nested contracts need their own later audit.
“Derive” means reconstruct from accepted predecessor state/input, not accept a self-consistent cache.
No successor field spelling, order, nullability or receipt domain is frozen here.

| Existing fields | Move3 | Completion2 | Required successor treatment / evidence owner |
| --- | --- | --- | --- |
| `contractVersion`, `eventType` | yes | yes | Exact declared pairs4/3. Keep historical dispatch strict; D2a's `combat-cycle-element-moved`1 is a distinct event, never an alias. .3 contracts;008E codecs. |
| `campaignId`, `rulesetHash` | yes | yes | Bind to accepted opening context; reconcile complete configuration/creation/cycle identities, not merely Rules10 hash substitution. .3 with2d input. |
| `stateVersion`, `priorStateVersion` | yes | yes | Derive adjacent checked versions and real prior prefix; reserve room for mandatory successors at composition. .3 event arithmetic; .4 capacity. |
| `fromPositionId`, `gameTurn`, `operationStage`, `actingSide`, `sequencePosition` | yes | yes | Derive exact sequence5 position and cycle scope from accepted chain. Do not copy sequence4 actor/materialization behavior. Completion advances only through its accepted successor boundary. .3;018/019 runtime domains. |
| `elementId`, `representationId`, `originLocationId`, `destinationLocationId` | yes | no | Validate original-unit/representation binding, ownership, current origin and legal destination from retained World7. Preserve creation identity; do not substitute representation identity for original unit. .3 Movement. |
| `mobilityId`, `mobilitySources` | yes | no | Recompute supported mobility and exact source references; reject invented sources. Positive unsupported mobility remains a capability gate. .3 Movement. |
| `cost` | yes | no | Reconcile nested legacy movement cost with D2a terrain plus maximum applicable Contact/Engaged cost, never sum memberships. Freeze exact adapter mapping later; .3/018. |
| `capabilityPointsExpendedBefore`, `capabilityPointsExpendedAfter` | yes | no | Preserve cumulative ledger and recompute delta/ceiling. Ordinary, I and II ceilings need explicit profile admission; no refund or reset. .3/018. |
| `cohesionBefore`, `cohesionAfter` | yes | no | Reconcile D2a immediate incremental excess-CPA DP. Legacy Move3 validation requires equality, so copying that guard would reject/lose this effect. .3 must derive accepted successor semantics, not relabel Move3. |
| `movementEndedAfter` | yes | no | Audit nested sequence identity and actual affected-membership ending evidence. Distinguish per-move ending marker from complete Movement-end proof used by continuation. .3 Movement. |
| `openedReactionWindow` | yes | no | Preserve trigger/version/unit binding when Reaction is admitted; null requires supported no-window semantics. Positive window payload and replay depend on separate .3 Reaction family;008F later. |
| `breakdownAccounting` | yes | no | Retain/rederive each admitted cohort's ordered accounting, weather attribution and source evidence. Empty array does not establish unsupported vehicle behavior. Positive vehicle/stop behavior belongs to separate .3 Breakdown capability work;008G later. |
| `breakdownFlowAfter` | yes | yes | Preserve exact route/stop/Reaction state and idle-completion guards for admitted family. No synthetic idle flow to bypass pending work. .3 with Reaction/Breakdown owners. |

Move3 has26 top-level fields; Completion2 has12. Neither contains an accepted-command record,
command receipt, creation/configuration hash, cycle authority or Chronicle prefix. Successor
contracts must bind these requirements through their agreed envelopes/history; this matrix does
not imply each requirement must become a new top-level property.

[Move3 validation](../../src/Cna.Core/Campaigns/CampaignElementMovedV3.cs), lines132–188,
requires materialized sequence4 Movement, equal before/after Cohesion, and matching Reaction
trigger version3. `ToReplayInput` reconstructs only campaign/prior version/from position/side/unit/
origin/destination. [Move projector](../../src/Cna.Core/Campaigns/CampaignV11MoveProjector.cs),
lines8–15 and56–59, reconstructs the expected event and compares full serialized bytes before
projecting. That authority check must survive the successor adaptation.

[Completion factory](../../src/Cna.Core/Campaigns/CampaignBreakdownLifecycleFactory.cs),
lines133–147, requires idle sequence authority, no Reaction window, stage1/first-side Movement
and sequence4 materialization. [Lifecycle projector](../../src/Cna.Core/Campaigns/CampaignV11BreakdownProjector.cs),
lines18–31 and68–85, rederives completion, compares canonical event bytes and preserves World/RNG
while advancing sequence. These are historical constraints to map, not automatic sequence5 rules.

## Missing proof obligations

Each positive trace must begin at an independently replayed accepted boundary. Invented base
hashes, private D2a ordinal2 fixtures and isolated D2b.2 resource Worlds cannot establish it.
The [ordinary Movement packet](../specs/combat-ordinary-movement-v1.md), lines17–28 and70–76,
explicitly limits those fixtures and distinguishes its wire type.

| Obligation | Positive evidence required in later .3 packet | Negative evidence required | Owner/dependency |
| --- | --- | --- | --- |
| Opening-to-Movement admission | Accepted2d history supplies exact initial cycle, World, Weather, RNG, Reserve designation history and prefix; legal move derives all effects. | Reject a substituted opening/cache, foreign creation, changed actor/slot/ordinal, altered Weather or missing predecessor event despite recomputed hashes. | .3 consumes frozen2d reader and vectors. |
| Canonical event and replay | Serialize/read/replay each cut; reconstruct effects from accepted input, append exactly one receipt/version/prefix contribution. | Unknown/reordered/duplicate fields, noncanonical numbers/bytes, wrong type/version, forged self-consistent cost/accounting/route, overflow. | .3 contracts;008E implementation. |
| Retry and progress | Authenticate exact accepted input before receipt reuse; return original event/receipt without another effect. Export only accepted material changes. | Changed reuse, stale/future input, cross-history receipt, duplicate progress reference, caller progress flag or arbitrary event name. | .3 progress adapter to D2b.2;004 public mapping. |
| Movement-end proof | Derive canonical final locations for every original unit, actual completion receipt, current cycle scope and prior exclusion proof. | Missing/duplicated unit, copied per-move marker, swapped receipt, prior-cycle proof, fabricated final location. | .3 Movement-completion adapter. |
| Exception expiry | At accepted completion, expire only matching immediately-next-ordinal/same-turn/stage/slot/side exceptions once; retain prior exclusions and receipts. No second event/version increment. | Wrong scope/ordinal, repeated expiry, erased historical exclusion, use after expiry, altered prior proof. | .3 integrates D2b.2 `MovementExpiryInput` semantics;018/019 runtime. |
| Positive Reserve movement | Reach I/released status through real designation/release/control history; derive CPA/half-CPA ceilings and unchanged prior expenditure. | Retained-II move, invented release receipt, CP reset/refund, exception treated as cost/occupancy waiver. | Explicit .3 Reserve/continuation admission; not closed by2d designation. |
| Armed continuation | Supported legal-action evidence distinguishes available next action from no continuation. | Unsupported armed surface must reject, never manufacture a no-continuation/system-finish result. | Separate .3 continuation family and004 capability map. |
| Full authority preservation | Retain World resources, obligations, RNG, stage target history and history across accepted events and replay cuts. | Dropped obligations, reset ammo/CP/TOE, truncated receipts, actor or sequence leakage, altered cache. | .3 bounded cases; .4 whole World/Snapshot/capacity. |

[Cycle control](../specs/combat-cycle-control-v1.md), lines27–48 and94–104, supplies progress,
continuation and expiry obligations. Movement-end locations belong to the actual end of Movement;
later retreat/proximity changes cannot replace them. Completing this matrix does not admit a
Reaction/Breakdown event or prove positive Reserve or armed continuation.

## Legacy tests to carry forward

Existing tests are source evidence only; no new .NET run is claimed by this packet.

| Test and locator | Carry-forward assertion / limitation |
| --- | --- |
| [BreakdownMoveReplayTests](../../tests/Cna.Core.Tests/Campaigns/BreakdownMoveReplayTests.cs), line14, `TruckMoveStrictReadbackAndReplayPreserveRngAndLots` | Retain strict readback, incompatible reader rejection and exact RNG/lot preservation. Truck fixture does not grant Content7 vehicle admission. |
| Same file, line43, `ForgedMovementCannotPatchAuthority` | Reuse negative categories: delta, internally consistent delta, source, Weather, missing accounting, route, rules, version and unknown property. Preserve prior bytes after rejection. |
| Same file, line77, `ReactionMovesRetainPhasingRouteAndActiveEpisodeWithoutNestedWindow` | Assign route/window preservation to separate Reaction work; Movement matrix must identify trigger handoff, not duplicate Reaction implementation. |
| [BreakdownLifecycleTests](../../tests/Cna.Core.Tests/Campaigns/BreakdownLifecycleTests.cs), line29, `MovementCompletionRequiresIdleAndAdvancesOnlyToBreakdown` | Map idle guard and exact successor boundary; reject completion with unresolved moving route. Sequence5 successor must be derived from accepted catalog, not copied from this fixture. |
| Same file, line148, `PendingOpenStopPreventsCompletionAndWindowClosure` | Preserve unresolved-stop blocking when positive stop family is admitted; no invented idle state. |

## Upstream handoff and sequencing

The [2d packet](../specs/combat-reserve-designation-v1.md),
[schema](../specs/combat-reserve-designation-v1.schema.json) and
[reader](../specs/verify-combat-reserve-designation-v1.py), with
[retained vectors](../specs/fixtures/combat-reserve-designation-v1.json), provide the concrete
private `ReserveState` contractVersion1 handoff below, frozen at `4d5199c`. Binding checked
after2d's final author oracle run; no inherited Movement proof is inferred from that completed packet.

| Actual2d entry/field | .3 consumption requirement |
| --- | --- |
| `initial(request,created,preamble,weather_events,stage_events)` | Replays Created11, four preamble records, one Weather2 and four stage-entry records to state10/nine receipts. This entry has no cycle; it is not sufficient Movement authority. |
| `replay(request,created,preamble,weather_events,stage_events,events)` | Supply accepted full history and zero–two Reserve records: optional designation followed by completion. Movement entry requires the completed suffix, not a cached `ReserveState`. |
| `read_state(data,request,created,preamble,weather_events,stage_events,events)` | Optional cache validation against the same full replay; cache equality never replaces predecessor records or production authenticated-head checks. |
| `campaignId`, `rulesetHash`, `configurationHash`, `creationBinding`, `creationEventHash`, `stateVersion`, `prefix` | Bind exact identity/head produced by the reader; derive next version/prefix rather than reseed. Completed state11/ten receipts for empty selection, state12/eleven receipts for I. |
| `sequencePosition`, `firstActingSide`, `cycle`, `cycleId` | Exact Movement catalog position remains unmaterialized: `activeSide=null`, first-acting-side role. `cycle.actingSide` resolves retained order via `firstActingSide`; ordinal1. Future Movement contract must explicitly reconcile this input with legacy materialized-sequence4 requirements, not mutate the2d output. |
| `openingBaseHash`, `completionReceiptId`, `receipts` | Retain actual completion binding and complete command receipts. Last four added state fields (`cycle`, `cycleId`, `openingBaseHash`, `completionReceiptId`) are null before completion. Historical `isolated-first-opening` base-profile string is a structural carrier inside2d, not permission to supply an independent base. |
| `world`, `randomState`, `operationStageWeather`, `initiativeHolder`, `operationStageOrders`, `members` | Preserve replay-produced World/Weather/RNG/order and own `members[].history.designationReceiptId`. Closed2d profile: two infantry at creation locations, CP0/ammo10, optional own none→I, all four Weather outcomes, no future obligations. It proves no positive Movement or released-Reserve capability. |

`apply(request,created,preamble,weather_events,stage_events,events,inp)` remains2d's command
entry; .3 must not reuse it as a generic Movement dispatcher. Actual input names and output fields
above come from the reader/schema, not a new interface declared by this preparation.

Preparation can run alongside2d; positive inherited replay waits for2d's frozen contract. Bound
Movement, Reaction, Breakdown and continuation packets independently before implementation. Retain
[D2c.1 owner DAG](../specs/combat-inherited-successors-v1.md#bounded-implementation-ownership):
008D precedes008E and019A;008E precedes008F/008G;008H waits for all declared codec families and019A.
These are runtime owners subject to checkpoint B; no runtime task starts through this preparation.
Full .4 composition/capacity follows accepted constituent contracts, then003 handoff→004→B.

## Source pins and validation

SHA-256 pins below bind baseline source bytes, not successor readiness. Paths are repository-relative.

| Source | SHA-256 |
| --- | --- |
| `src/Cna.Core/Campaigns/CampaignBreakdownMoveEventSerializer.cs` | `2885afd854bc57c5b8f782be0e0cd875fe13eb78658f433145568d45b2b1c704` |
| `src/Cna.Core/Campaigns/CampaignElementMovedV3.cs` | `b3ff30e9330c3c923503077d17ffad4945ff0184ec5682b677675d9aeca404f2` |
| `src/Cna.Core/Campaigns/CampaignV11MoveProjector.cs` | `c62fc410cfdf372c11c4e796a354b2e3015d0d2eeb466c3e53729806dfc43241` |
| `src/Cna.Core/Campaigns/CampaignBreakdownLifecycleCodec.cs` | `5d8c11b6d13df33d767f4d3812f52fdf463ef85ce645b92e73d50cea074beb2c` |
| `src/Cna.Core/Campaigns/CampaignBreakdownLifecycleFactory.cs` | `07b97600a212bf2b255004bafdeba7da0804624198be354814d25ede5f0d4413` |
| `src/Cna.Core/Campaigns/CampaignV11BreakdownProjector.cs` | `a1ed1aefd2ceb6fe3641d82b13ab0d04eb26505bdf98ccf3745cd974cee49826` |
| `tests/Cna.Core.Tests/Campaigns/BreakdownMoveReplayTests.cs` | `591778897fa345797b711082843e1eb41ca473d014e990d427c6a305fdd328e1` |
| `tests/Cna.Core.Tests/Campaigns/BreakdownLifecycleTests.cs` | `07cf07fa03d66dfa9e251ffb068ed2c55949cef9a5a542df765b9be91c4a0090` |
| `docs/specs/combat-ordinary-movement-v1.md` | `e28cb696892d58c6671dd35de4850644985fb44bf6c217aee4ecfb37d4e484cc` |
| `docs/specs/combat-cycle-control-v1.md` | `2d183ed4e891eda31c73eaa7d9479a3175f7ecc2842b6c405d0f47d768620639` |
| `docs/specs/combat-inherited-successors-v1.md` | `50469a7ec9937287947d8639f89786518e7812911892ca1c4dcf1c7b16acab40` |

2d handoff pins bind the actual new reader, schema and retained inputs; they are not baseline files.

| Handoff artifact | SHA-256 |
| --- | --- |
| `docs/specs/combat-reserve-designation-v1.md` | `a9ccf1dafc2e0947582151e5d0649e1771f471e1d0d0bc14d9914766ad9dd826` |
| `docs/specs/combat-reserve-designation-v1.schema.json` | `f324fb22e53ff6d9e084c2f65532e5af89277f632087549756b7df1dcb1710d9` |
| `docs/specs/fixtures/combat-reserve-designation-v1.json` | `6c8abb2038bb61c739e3bd3cab5c8adf00689c265ec3c6bc6ea29bebe66b12c2` |
| `docs/specs/verify-combat-reserve-designation-v1.py` | `fa40ec6b10d0d5ecc113aefba47ff4df44ae616851acdc827e9f676f2412e465` |

Author preparation checks: extracted exact `RequireProperties` lists and confirmed all26 Move3 /
12 Completion2 fields appear in the matrix; recomputed all11 baseline SHA-256 pins and compared
their bytes with `git show 4c10ede:<path>`; checked17 local Markdown targets, including the owner-DAG
fragment. Extracted actual Python signatures via AST and schema fields via JSON; all four entry
signatures and21 ReserveState fields match the handoff section. All pass. Exact test names and source
line locators checked against pinned files. A separate `git diff --name-only --diff-filter=CDMRTUXB 4c10ede -- src tests scenarios docs/specs`
returned empty, excluding new2d files while checking tracked predecessors. Only this preparation document is
owned by this lane. No oracle or .NET suite rerun by this lane; lead owns shared oracle evidence.
After2d author reported its final passing16-trace run, all four handoff hashes were recomputed and
checked against the frozen files. Preparation is complete; .3 successor contracts, genuine Movement
provenance, positive Reserve/armed continuation and .4 composition remain open.
