# Creation-rooted inherited Breakdown completion v1

Status: CMB-TASK-003D2c.3c contract checkpoint. The
[ordered inventory](combat-inherited-breakdown-completion-v1.schema.json),
[literal vectors](fixtures/combat-inherited-breakdown-completion-v1.json) and
[oracle](verify-combat-inherited-breakdown-completion-v1.py) freeze actual System
`breakdown-segment-completed`2 into first Combat Position Determination.
This is a private contract projection, not runtime registration, Combat execution or Snapshot12.
The [combined plan](../design/combat-cycle-implementation-plan.md) retains all remaining gates.

## Accepted boundary

Every public initial/apply/replay/cache entry reconstructs complete accepted Created11, preamble,
Weather2, stage-entry, Reserve completion2, Move4, owner stop2, System empty stop-resolved2 and owner
Movement completion3 through the unchanged [route lifecycle reader](combat-inherited-movement-lifecycle-v1.md).
All three lifecycle records are mandatory. The final position must be sequence5 first-side Breakdown
Determination, flow idle, interruptContext null, with the actual first MovementEndProof present.
There is no caller-supplied World, initial hash, idle flag or proof admission.

The predecessor's closed profile remains: turn1/stage1/first slot/ordinal1, both resolved owners,
normal Weather, ordinary NONE independent infantry, one route with1..7 legal Clear2 moves,
no adjacent enemy combat representation, positive vehicle cohort, Reaction, release exception or
other unhandled obligation. Completion is not evidence that a positive Breakdown check occurred.
Unsupported histories reject; a cache or correctly recomputed hash cannot turn them into support.

## Command, event and state

EntryCommand2 has kind `complete-breakdown-segment`, operationStage1, actionId, creationBinding,
creationEventHash, cycleId, expectedPriorVersion and expectedPositionId, in inventory order.
EntryInput adds the trusted actor wrapper, which must be `system`. Neither owning side may execute
this System step. The adapter owns authenticating that wrapper; a command cannot self-authorize.
The actionId is lowercase `sha256:` plus SHA256 of compact ASCII UTF8 for the ordered object
`{contractVersion:1,kind:"complete-breakdown-segment",operationStage:1}`. The version2 history-bound
command preserves this legacy version1 action identity. Sources:
[action candidate](../../src/Cna.Core/Actions/BreakdownActionCandidates.cs),
[factory](../../src/Cna.Core/Campaigns/CampaignBreakdownLifecycleFactory.cs).

EntryEvent2 retains all11 legacy fields in order: contractVersion, eventType, campaignId,
stateVersion, priorStateVersion, rulesetHash, fromPositionId, actionId, sequencePosition,
breakdownFlowAfter, sources. Sources are exactly those of the accepted **predecessor Breakdown
position**, not the successor Combat position or the earlier empty stop-resolution ruling.
The source of this distinction is
[lifecycle codec/factory](../../src/Cna.Core/Campaigns/CampaignBreakdownLifecycleCodec.cs).
Append configurationHash, creationBinding, creationEventHash, cycleId, openingBaseHash,
completionReceiptId (original Reserve completion), movementCompletionReceiptId (actual3b completion),
priorPrefix, input and receiptId. Full event replay derives every field from accepted history.

The successor is selected from the frozen sequence5 cycle edge's first combatPositionIds entry,
which must be the catalog position directly following the predecessor:
`land.position.operation-1.first-player.movement-and-combat.combat.position-determination`.
The position retains actorRole first-acting-side and activeSide null. Actual owning side stays in
cycle authority; no materialization event or later ordinal is invented. Flow remains idle.

CombatEntryState retains every26 LifecycleState field, with one appended nullable
breakdownCompletionReceiptId: null at the admitted input, actual new receipt after completion.
World, RNG, Weather, orders, members/history/spent CP, tracks, actualProgressRefs, MovementEndProof,
initiative, creation/cycle/opening identities, suspended-context null and all prior receipts persist.
Only position, stateVersion, prefix, the appended completion ID and one new command receipt change.
Movement-end locations remain those recorded by actual Movement completion; they are not recomputed
or replaced. This lifecycle event creates no material-progress reference and does not reset CP,
cohesion, resources, exclusions, target use or any other inherited state.

## Bytes, receipt, replay and retry

Canonical JSON uses the predecessor typed codec for all retained shapes: compact ASCII UTF8,
mandatory ordered keys, sorted declared identity arrays, unchanged ordered histories/routes,
no duplicate/unknown properties, no alternate numeric/string spellings. Caps1MiB, depth32,
512 array items; this suffix contains zero or one event. Exactly adjacent checked Int64 versions
and capacity for the new receipt are required. Private codes CMB-IBC001 codec/bounds,003 unsupported
family,004 identity/provenance,005 actor/capability,006 order/retry,007 capacity,008 noncanonical bytes.

Receipt is `ibc.` plus lowercase SHA256 of domain
`sandtable.combat.inherited-breakdown-completion-receipt.v2`, NUL and canonical unsigned event bytes
(receiptId alone omitted). Chronicle prefix uses the unchanged sequence5 event-prefix algorithm on
prior prefix and full event bytes. Command receipt retains accepted EntryInput hash, full event hash,
receipt ID, System actor and resulting version. State's new completion ID is assigned only after
the event receipt is known; original Reserve and Movement completion IDs remain distinct.

Replay reconstructs the whole predecessor chain and recomputes the exact event from its accepted
input before comparing all bytes. read_state checks canonical cache equality against that replay.
apply validates kind/version/operationStage, System actor and creation/cycle/action identity before
retry lookup. Reusing expectedPriorVersion with changed input rejects; exact authenticated retry
returns original event and unchanged final reconstructed state. Fresh command additionally requires
exact current version/position and uncompleted boundary. Duplicate history events and attempts to
complete again after Combat entry reject. Errors never mutate input history or state.

## Verification and remaining work

Retain both owners at1/5/6/7 moves (CP2/10/12/14, cohesion0/0/-2/-4), including both distance2 and
distance3 Movement-end exclusion results. Vectors pin full predecessor digest, input, event and both
state cuts. Checks cover System-only authority, exact legacy fields/action/source mapping, full-state
preservation, retry, canonical byte rejection, every event leaf with re-signed effects, altered proof
or progress cache, missing/reordered lifecycle records and same-owner/cross-owner history swaps.
Fixture missing/duplicate cases, source drift and changed goldens must fail. A semantic RED verifies
that no-op behavior cannot satisfy the actual Combat-entry/version/prefix transition.

This child reaches Combat entry only. It must not feed a synthetic pre-Combat C3a base into the new
chain or treat lack of an implemented armed surface as a no-continuation result. Actual Combat
admission, positive Reaction/vehicle/Reserve families, repeat exception expiry, full World/Snapshot
composition and Task004 evidence remain separate obligations. No additional independent review is
implied; review13 covers3a, while3b/3c have author verification.
