# Creation-rooted inherited Movement lifecycle v1

Status: CMB-TASK-003D2c.3b contract checkpoint. Specification and
[ordered inventory](combat-inherited-movement-lifecycle-v1.schema.json) govern the
[literal vectors](fixtures/combat-inherited-movement-lifecycle-v1.json) and
[executable oracle](verify-combat-inherited-movement-lifecycle-v1.py).
This is contract evidence, not production registration or a full Snapshot12 reader.
Parent3/4/003/004 and runtime gateB remain open in the
[implementation plan](../design/combat-cycle-implementation-plan.md).

## Admission and ownership

Every public entry reconstructs the complete accepted creation, preamble, Weather, stage-entry,
Reserve-completion and Move4 prefix through the unchanged
[inherited Movement contract](combat-inherited-movement-v1.md). At least one legal Move4 is required.
The same first-turn/stage/slot/ordinal, normal-Weather, ordinary NONE, independent infantry profile
applies. One route,1..7 legal Clear2 moves, no positive Reaction or Breakdown cohorts, no next-Movement
exceptions, guards, relationships, settlements, broken lots or future obligations are admitted.
There is no caller-supplied initial World or proof. No-move completion, another route after stopping,
mixed new Move4/lifecycle histories, positive vehicle checks, Reserve movement and later cycles
remain unsupported capabilities, not game-rule prohibitions.

Exactly three accepted commands form this closed trace:

| Command2 | Trusted actor | Successor event | Required prior flow | Result |
| --- | --- | --- | --- | --- |
| `stop-element-movement` | resolved owning side | `element-movement-stopped`2 | moving | phasing-stop at generic Breakdown-stop interrupt |
| `resolve-breakdown-stop` | System | `breakdown-stop-resolved`2 | phasing-stop | idle at suspended Movement position |
| `complete-movement-segment` | resolved owning side | `movement-segment-completed`3 | idle after actual resolution | idle at Breakdown Determination |

The owner actor is resolved from replayed cycle authority; symbolic sequence5 positions keep
`activeSide=null`. The actor wrapper is a trusted caller boundary, not a command field a player
may use to claim System authority. A production adapter must supply it from authenticated dispatch.
Commands bind creationBinding, creationEventHash, cycleId, expectedPriorVersion and expectedPositionId.
The command version2 wraps the retained version1 action identity; it does not rewrite old candidates.

## Ordered bytes and identities

All11 stop,15 resolution and12 completion legacy top-level fields retain order and meaning before
the appended bindings in the inventory. Sources:
[lifecycle codec](../../src/Cna.Core/Campaigns/CampaignBreakdownLifecycleCodec.cs),
[resolution codec](../../src/Cna.Core/Campaigns/CampaignBreakdownStopResolvedCodec.cs).
New fields bind configuration, creation, cycle, opening base, original Reserve completion receipt,
prior Chronicle prefix, accepted input, post-position/context and event receipt. Completion also
carries final endLocations, excludedUnits and existing actual Move4 progress references.

The retained route has ordered fields routeId, firstMoveStateVersion, elementId, representationId,
owner, originLocationId, currentLocationId, cohortIds. Deliberate stop has ordered fields stopId,
recordedStateVersion, route, reason, weatherKind, cohortInputs. Stop preimage is the canonical object
with domain `sandtable.breakdown.stop.v1`, campaignId, rulesetHash, then every stop field except
stopId. It includes the complete route (including current location and routeId), the newly committed
version, `reason=deliberate`, `weatherKind=normal`, and `cohortInputs=[]`.
These identities follow [Breakdown codec](../../src/Cna.Core/Campaigns/CampaignBreakdownCodec.cs).

Owner route capability is distinct from authoritative routeId. Hash the ordered object domain,
campaignId, rulesetHash, stateVersion, audience, elementId, originLocationId, currentLocationId;
domain=`sandtable.observation.movement-route.v1`, audience=resolved owner. Stop actionId hashes
`{contractVersion:1,kind:"stop-element-movement",routeId:<owner capability>}`. System stop capability
hashes domain, campaignId, rulesetHash, stateVersion, audience; domain=`sandtable.action.breakdown-stop.v1`,
audience=`system`. Resolve actionId hashes
`{contractVersion:1,kind:"resolve-breakdown-stop",stopId:<System capability>}`. Completion actionId
hashes `{contractVersion:1,kind:"complete-movement-segment"}`. All use lowercase `sha256:` plus SHA256
of compact canonical ASCII UTF8. Persisted stopId and routeId are never submitted capabilities.
Sources: [factory](../../src/Cna.Core/Campaigns/CampaignBreakdownLifecycleFactory.cs),
[action identity](../../src/Cna.Core/Actions/CampaignActionCandidate.cs).

Each receipt is `iml.` plus lowercase SHA256 of its inventory domain UTF8, NUL, and canonical unsigned
event bytes (only receiptId omitted). Domains differ by event type/version. Chronicle prefix uses
the retained sequence5 prefix-event codec over the prior prefix and full event bytes. Command receipt
records retain input hash, full event hash, receiptId, actor and resulting stateVersion. Event receipt
is derived before the state MovementEndProof, so no self-referential receipt hash occurs.

## Interrupt and state transition

LifecycleState preserves all24 inherited fields; breakdownFlow becomes the closed tagged union
moving/phasing-stop/idle. Add interruptContext and movementEnd, initially null. Stop leaves World,
RNG, members, tracks and progress unchanged; captures exact cycle, cycleId and suspended Movement
position in interruptContext; selects the canonical sequence5 interrupt
`land.position.breakdown-stop`. The generic position is not a substitute for captured context.
Replay binds that context to the original opened cycle and Movement position, including ordinal.

Resolution must consume that actual pending stop. Its checks and createdLots are empty, before/after
RNG bytes are equal, and sources are exactly `spi-1979-land-rules:21.24-21.26`. It restores the captured
Movement position, clears interruptContext and sets idle. Even zero cohorts require this event;
skipping resolution cannot establish idle authority. Sources:
[resolution factory](../../src/Cna.Core/Campaigns/CampaignBreakdownStopResolvedFactory.cs),
[projector](../../src/Cna.Core/Campaigns/CampaignV11BreakdownProjector.cs).

Completion requires the actual resolved prefix and exact owner command. It advances one canonical
edge to `land.position.operation-1.first-player.movement-and-combat.breakdown-determination`.
It does not enter Combat or complete Breakdown Determination. A subsequent
`breakdown-segment-completed`2 remains a separate required family.
Across all three events only sequence/flow/context/proof, version, prefix and receipts change;
full World, prior receipts, opening/cycle authority, initiative, Weather, order, RNG, member spending,
tracks and actual material progress are preserved. Deliberate stopping does not invent a
movementEnded marker or reset CP/cohesion; full completion obligations outside this profile remain open.

## First actual Movement-end proof

Use the unchanged [cycle-control proof shape](combat-cycle-control-v1.schema.json): scope, ordinal,
completionReceiptId, endLocations, excludedBefore. Scope is the real opened cycle scope; ordinal1;
completionReceiptId is this new completion receipt, while LifecycleState.completionReceiptId continues
to mean Reserve-opening completion. endLocations contains every original unit on both sides, sorted
by (creationBinding,originalSide,elementId), with positions from replayed finalWorld. Start prior
exclusions empty, derive own units farther than two hexes from every enemy, and persist them in
excludedBefore. The event excludedUnits must match that derivation. Ending at own rear (distance2)
does not exclude; ending at own supply (distance3) does. Use the existing pure cycle-control exclusion
predicate, never a synthetic ordinal0 proof or repeat-only expire_movement adapter. Members stay
unchanged because the admitted NONE profile has no release exceptions. Future repeated completions
must retain historical exclusion union and real exception-expiry semantics.

Completion progress equals the actual inherited Move4 references, including their full event hashes.
Stop/resolution/completion are lifecycle bookkeeping and do not add material-progress references.
This first proof does not by itself admit release, armed continuation or full Snapshot composition.

## Replay, retry and rejection

Public replay rederives every event from actual predecessor state and accepted input, then compares
full canonical bytes. read_state parses a cache but accepts it only if full replay produces exactly
those bytes. apply authenticates owner/System plus creation/cycle identity before looking for an
exact historical command retry. Reuse of expectedPriorVersion with changed input rejects. An exact
authenticated retry after later lifecycle events returns the original event and current reconstructed
state without another receipt or effect. Fresh commands also require exact position/version and
current capability. Stale/cross-owner/cross-cycle/changed-prefix histories fail without mutation.

Encoding follows the inherited strict typed compact JSON codec: mandatory ordered fields, no unknown
or duplicate fields, booleans distinct from integers, canonical numeric/string representation and
sorted declared identity arrays. Ordered routes and progress/history arrays retain order. Caps1MiB,
depth32,512 items; Move4 prefix cap32 (actual positive cost limit7), lifecycle suffix cap3. Closed union
tags reject unknown flow/command families. Private diagnostics CMB-IML001 codec/bounds,003 unsupported
family,004 identity/provenance,005 actor/capability,006 ordering/retry,007 capacity,008 noncanonical bytes.

## Verification boundary

Both owners have literal cuts at1/5/6/7 moves (CP2/10/12/14, cohesion0/0/-2/-4). Each trace pins input,
event and state bytes/hashes; restores every lifecycle cut; retries every command after terminal;
checks both proximity outcomes and exact world/RNG preservation. Mutations must challenge event
effects with re-signed receipts, canonical caches, identities, pending context, source references,
proof locations/exclusions and progress. Missing/duplicate fixture cases and altered source/golden
controls must fail. Predecessor Movement and cycle-control suites remain regression evidence.
