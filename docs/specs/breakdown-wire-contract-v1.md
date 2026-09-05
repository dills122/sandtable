# Breakdown v1 wire contract freeze

**Status:** Normative BRK-TASK-001 freeze; dormant Rules schema 2 implemented in Task 002; remaining successor codecs pending.
**Governing behavior:** [Breakdown specification](breakdown-adjudication-v1.md).
**Baseline:** Every predecessor reference below means repository commit `a047547`, never an evolving
file of the same name. This is a precise delta specification: inherit predecessor field names,
order, scalar types and validation except the explicit substitutions/additions below. Do not copy
new fields into old serializers. This avoids duplicating unrelated existing event/preamble schemas.

## Identity set and canonical rules

| Contract/artifact | Baseline | Successor | Change / owner task |
| --- | --- | --- | --- |
| Ruleset manifest `cna-1979.1` | contract 8 | 9 | Outcome artifact/ruling factories / 002; full hash after sequence 4, registration / 006 |
| `cna-1979.1.breakdown-tables` | schema 1 | 2 | Outcome cells/fractions / 002 |
| Land sequence / catalog | 3 / 3 | 4 / 4 | System stop position and first-side Breakdown completion / 003,005 |
| Content / canonical format | 5 / `sandtable.content-json.v4` | 6 / `sandtable.content-json.v5` | Public certified profile / 003 |
| Setup definition and embedded snapshot | schema 5 | 6 | Public profile; Content 6 binding / 003 |
| World | 5 | 6 | Persistent lots; existing cohort ledger retained / 003 |
| Snapshot | 10 | 11 | Finite flow and stop position / 003 |
| CampaignCreated | 9 | 10 | Setup 6, World 6, initial idle flow / 003 |
| ElementMoved | 2 | 3 | BP and flow evidence; Truck eligibility / 004 |
| ReactingElementMoved | 1 | 2 | Shared BP and reactor-route evidence / 004 |
| ReactionParticipantCompleted | 1 | 2 | Recorded reactor stop / 005 |
| ReactionWindowClosed | 1 | 2 | Preserve active stop and phasing continuation / 005 |
| MovementSegmentCompleted | 1 | 2 | Enforced drained flow and sequence 4 / 005 |
| ElementMovementStopped | absent | 1 | Deliberate phasing stop / 005 |
| BreakdownStopResolved | absent | 1 | Atomic check/lot/continuation transaction / 005 |
| BreakdownSegmentCompleted | absent | 1 | Drained first-side Breakdown → Combat / 005 |
| Observation / policy | 6 / `sandtable.observation.zoc-reaction-side-safe.v1` | 7 / `sandtable.observation.breakdown-side-safe.v1` | Safe own route/lots and waiting / 006 |
| Projected decision history | 1 | 2 | Observation 7 decision-state closure / 006 |
| Legal-action envelope / policy | 2 / `sandtable.legal-actions.v2` | 2 / `sandtable.legal-actions.v3` | Closed new kinds and new membership / 006 |
| Creation request/result | request 1 / unversioned typed result | unchanged shapes | Append `UnsupportedCapabilityProfile=11` to rejection enum without renumbering / 003,006 |
| Candidate / submission / acceptance envelopes | 1 / 1 / 1 | unchanged | Same envelopes; new candidate variants / 006 |
| User-space disclosure manifest / policy | 1 / `sandtable.user-space-declassification.v1` | 2 / `sandtable.user-space-declassification.v2` | New audited outward closure / 006 |
| RNG algorithm, stream, procedure | existing identities | unchanged | Existing rejection-sampled d6; new callers only / 005 |
| Combat, ZOC, Movement rule artifacts | existing schemas | unchanged | No new cost/ZOC rule; profile constrains admission |
| Reaction window/opportunity authority and disclosure hash domains | existing domains | unchanged | Already bind rules/state and move contract; bytes rehash |
| Exercise/Maneuver/bundle/report envelopes | existing versions | unchanged | Strict event registry and new fixture IDs/hashes / 007 |
| Earlier preamble, Initiative, weather, Reserve events | existing event versions | unchanged | Their sequence fields bind sequence 4; preserve schemas |
| Intelligence protobuf | existing | unchanged | No intelligence-plane behavior |

Hash dependency order: normalize source artifact + accepted rulings → ruleset manifest; normalize
Content 6 → content hash; Setup 6 binds content/profile → setup hash; creation binds rules/setup/world/
RNG → snapshots/events → Observation and public capability/action hashes → Runner fingerprints.
Existing SHA-256 formatting remains contract-specific (ruleset hash unprefixed lowercase 64 hex;
other `Hash` values `sha256:` plus lowercase 64 hex). Each dependent artifact recomputes from canonical
bytes, not a patched old hash. Future golden bytes/hashes are Task 003–007 output, not fabricated here.

All new objects use UTF-8 compact JSON, fields in listed order, explicit null for nullable members,
no duplicate/unknown properties, comments, trailing commas or noncanonical numeric encodings.
`Id` is an existing stable-ID string, `Hash` is prefixed SHA-256, `Side` is `axis|commonwealth`.
`I32`, `I64` and `U64` use existing checked integer bounds; counts are nonnegative I32, state versions
positive I64, cursors U64. `BP` uses existing reduced nonnegative numerator/positive denominator
`BreakdownPointAmountCodec`; `Fraction` uses the same ordered numerator/denominator shape, with one
of the six accepted values. `Source[]` uses existing `{sourceId,locator}`, ordinal sorted and unique.
Arrays of IDs/records sort by ordinal identity unless a different order is explicitly given. Empty
arrays are emitted. Every deserializer reserializes and compares exact canonical bytes.

## Rules, content and setup deltas

`src/Cna.Core/Rules/BreakdownRulesArtifactCodec.cs`: preserve predecessor fields through
`diceCoordinate`; insert `outcomeFractions` then `outcomeCells` before root `sources`. Set schema 2.

- `outcomeFractions[]`: `{printedLabel:I32, fraction:Fraction, rulingId:Id, sources:Source[]}`,
  sorted by printed label (0,10,25,33,50,75).
- `outcomeCells[]`: `{bandId:Id, coordinate:I32, printedLabel:I32, sources:Source[]}`,
  sorted by existing band index then coordinate. Exactly 324 entries, matching source-locked research
  bounds. Production normalizer owns these values; runtime must not load the Python research file.
- Fraction ruling ID is `land.breakdown.ruling.exact-outcome-fractions`. Register accepted decisions
  004–007 as ruleset rulings with stable IDs respectively `land.breakdown.ruling.exact-outcome-fractions`,
  `land.breakdown.ruling.stop-reaction-precedence`, `land.breakdown.ruling.standalone-truck-profile`,
  `land.breakdown.ruling.persistent-stop-lots`. Existing `Ruling` schema stays unchanged; selected behavior
  IDs are respectively `exact-fractions-ceiling-single-point-exception`, `reaction-before-phasing-stop`,
  `certified-unladen-truck-battalion`, `persistent-stop-location-lots`. Conflict IDs are `BRK-DEC-004`
  through `BRK-DEC-007`; alternative IDs include the accepted behavior and respectively
  `literal-thirty-three-percent`, `breakdown-before-reaction`, `general-motorized-infantry`,
  `scalar-broken-count`. Protecting test IDs use the governing BRK-AC IDs; source references come from
  the accepted packet (004:21.34–35; 005:21.24–26; 006:21.27–29,21.36,21.42–45; 007:21.41–45).

`ContentPackV5Serializer.cs`: successor root preserves all fields/order but writes schema 6 and
format v5, and inserts `capabilityProfileId:Id` immediately after `capabilities`. Its only public
value is `sandtable.capability.breakdown-truck-battalion.v1`. Existing element/cohort/component schemas
remain exact; closed grammar plus profile certification supplies unladen semantics, not new cargo
fields. All other capabilities must remain the exact supported predecessor capability set; adding
this root field is not permission for arbitrary capabilities. Existing source/initial-TOE validation
continues; Trucks have explicit noncombat classification, empty components and valid empty initial
component seed lists. Truck mobility uses existing motorized facts and battalion accounting size.

`CampaignSetupHash.SerializeCanonical` and `CampaignV10CanonicalCodec.WriteSetup`: successor writes schema 6,
Content 6 reference and inserts `capabilityProfileId:Id` immediately after `isSynthetic`. It must equal
referenced Content's profile. Definition retains display name; embedded snapshot retains setup hash
under existing conventions. Reject unknown profile before exposing the root. No caller-certified
boolean replaces structural validation. `CampaignAuthority.cs` retains creation request version 1
and result shape. Append `CampaignCreationRejectionReason.UnsupportedCapabilityProfile = 11` after
existing `InvalidState = 10`; profile failure returns that value and a null handle. Public report
label, where needed, is `unsupported-capability-profile`; detailed BRK-CERT reasons stay trusted.

Land sequence 4 retains all existing ordered sequence catalog positions and adds System interrupt
position ID `land.position.breakdown-stop` to the interrupt position domain, not the normal linear
catalog. It has no active side; turn/stage and suspended normal Movement position bind the stop.
Sequence/catalog hashes change as a coherent identity. Combat remains a terminal unsupported position.

## New authority records

The following braces describe exact serialized field order, not C# declarations.

```text
Route = {routeId:Hash, firstMoveStateVersion:I64, elementId:Id, representationId:Id,
         owner:Side, originLocationId:Id, currentLocationId:Id, cohortIds:Id[]}
CheckInput = {cohortId:Id, vehicleTypeId:Id, profileId:Id, workingPointCount:I32,
              cumulativeBreakdownPoints:BP, sandstormAttributedBreakdownPoints:BP,
              highestEffectiveCheckedBandId:Id?}
Stop = {stopId:Hash, recordedStateVersion:I64, route:Route, reason:StopReason,
        weatherKind:Weather, cohortInputs:CheckInput[]}
StopReason = deliberate | movement-ended | cp-exhausted | reaction-completed |
             reaction-unavailable | reaction-timeout
Weather = normal | hot | sandstorm | rainstorm
PhasingContinuation = {kind:"resume-route", route:Route}
                    | {kind:"resolve-stop", stop:Stop}
BreakdownFlow = {kind:"idle"}
              | {kind:"moving", route:Route}
              | {kind:"reacting", phasingContinuation:PhasingContinuation, reactorRoute:Route?}
              | {kind:"reactor-stop-open", phasingContinuation:PhasingContinuation, stop:Stop}
              | {kind:"reactor-stop-closed", phasingContinuation:PhasingContinuation, stop:Stop}
              | {kind:"phasing-stop", stop:Stop}
BrokenVehicleLot = {lotId:Hash, stopId:Hash, checkId:Hash, owner:Side, cohortId:Id,
                    vehicleTypeId:Id, pointCount:I32, locationId:Id,
                    createdStateVersion:I64, sources:Source[]}
```

`Weather` uses the inherited Breakdown codec spellings exactly. Stop inputs sort by cohort ID and exactly equal
cohort ledgers at recording; resolution validates that they are still current. Route membership is
all and only the moving element's immutable cohort IDs. Route owner/binding/location matches World.
First accepted step sets origin before moving; current location tracks every step. A stop embeds the
completed route, and its location is that route's current location. `pointCount` in a lot must be
positive. When both forced-end reasons apply, `movement-ended` wins over `cp-exhausted`. Route
turn/stage is inherited from its snapshot's suspended sequence and cannot cross a stage boundary.

Trusted identities use SHA-256 over an ordered JSON object with `domain` first, then `campaignId`,
`rulesetHash`, then these fields:

| Identity / domain | Remaining hash input fields, in order |
| --- | --- |
| Route / `sandtable.breakdown.route.v1` | `firstMoveStateVersion`, `elementId`, `representationId`, `owner`, `originLocationId`, `cohortIds` |
| Stop / `sandtable.breakdown.stop.v1` | `recordedStateVersion`, `route` (full), `reason`, `weatherKind`, `cohortInputs` |
| Check / `sandtable.breakdown.check.v1` | `stopId`, `cohortId` |
| Lot / `sandtable.breakdown.lot.v1` | `stopId`, `checkId`, `owner`, `cohortId`, `vehicleTypeId`, `pointCount`, `locationId`, `createdStateVersion` |

Route hash excludes mutable current location. Stop/check/lot IDs are authority-only. Identity does
not substitute for validating full sources, inputs and outcome. These records contain no recursively
typed continuation. Serialization depth is bounded by the six flow variants.

## World, snapshot and creation

`CampaignV10CanonicalCodec.WriteWorld`: successor World 6 retains `elements`, `representations` and
all existing nested fields; append `brokenVehicleLots:BrokenVehicleLot[]` to root, ordered by lot ID.
Existing `brokenPointCount` is retained as checked aggregate, never independently mutable. Initial
lots empty, initial BP/check memory/count conventions preserved; new creation requires positive
working Truck points and zero broken count. Non-cohort ledger remains null.

`CampaignSnapshotV10Serializer`: successor root writes contract 11, replaces Setup/World/sequence
with successors, and appends `breakdownFlow:BreakdownFlow` after `reactionWindow`. Existing
`currentPosition` variants `sequence`/`reaction` retain shapes with sequence 4. Add exactly
`{kind:"breakdown-stop", suspendedSequencePosition:LandSequencePosition}`. Flow `idle`/`moving`
uses sequence position; `reacting` uses Reaction position/window; either reactor-stop variant or
phasing-stop uses Breakdown-stop position. Window exists only in `reacting` or `reactor-stop-open`.
An open stop's window has its participant already resolved and active slot null. Closed-stop flow
has no window but retains P; window authority is reconstructible from the closing event history.
Initial state and all pre-Movement/Breakdown/Combat sequence checkpoints require `idle`.

`CampaignSuccessorEventSerializer.WriteCreated`: successor contract 10 inherits exact fields,
substitutes Setup 6/World 6 and sequence 4 where used; append `breakdownFlow:{kind:"idle"}`. Initial
profile certification and initial World conservation must hold before accepting creation.

## Move and stop event deltas

The predecessor serializers are `CampaignSuccessorEventSerializer.cs` for creation/moves/Reaction,
and `CampaignEventSerializer.cs` for Movement completion. All event types keep their existing
`eventType` string. Readback dispatch must match `(eventType,contractVersion)`, never name alone.

```text
BreakdownStep = {cohortId:Id, vehicleTypeId:Id, profileId:Id,
                 destinationTerrainId:Id, inputRouteId:Id?, effectiveRouteId:Id?,
                 hexsides:BreakdownCrossedHexside[], weatherKind:Weather,
                 before:BP, delta:BP, after:BP, sandstormBefore:BP,
                 sandstormDelta:BP, sandstormAfter:BP, sources:Source[]}
BreakdownCrossedHexside = {hexsideId:Id, direction:Direction, addedPoints:BP, sources:Source[]}
Direction = either | up | down
```

Step inputs bind exact content edge through inherited move origin/destination and content hash in
setup. Null route means absent; hexsides contains every crossed modifier, sorted by ordinal hexside
ID then direction, with no duplicate pair. Sum all directional additions, not just one. Sources include terrain,
route transformation/operation, hexside and weather provenance used, canonical unique ordered union.
No delta for a non-cohort element. Positive BP still does not draw dice before its stop.

| Successor event | Exact changes after predecessor fields |
| --- | --- |
| ElementMoved 3 | Set contract 3 and successor sequence; trigger's `moveContractVersion=3`; append `rulesetHash`, `breakdownAccounting:BreakdownStep[]`, `breakdownFlowAfter:BreakdownFlow` |
| ReactingElementMoved 2 | Set contract 2; append same three fields; update/create reactor route; never creates nested window |
| ReactionParticipantCompleted 2 | Set contract 2; append `rulesetHash`, `breakdownFlowAfter`; flow must be `reactor-stop-open` |
| ReactionWindowClosed 2 | Set contract 2; rename predecessor `resumedSequencePosition` to `suspendedSequencePosition` in the same field position; append `rulesetHash`, `breakdownFlowAfter`; closed flow resumes P or records reactor stop |
| MovementSegmentCompleted 2 | Set contract 2; successor sequence fields; append `rulesetHash`, `breakdownFlowAfter` (idle); requires idle before event |

Normal/Reaction accounting arrays sort by cohort ID. RulesetHash scalar uses existing unprefixed
ruleset form. All appended flow is derived, never a caller-authorized state patch. Existing CP,
cohesion, movement-ended and trigger evidence remain rederived too. A close event's suspended
sequence value identifies eventual resumption, not a claim that it is already executable.

New event common prefix in order:
`{contractVersion:1,eventType:Id,campaignId:Id,stateVersion:I64,priorStateVersion:I64,
rulesetHash,fromPositionId:Id,actionId:Hash}`. State increments exactly one; append the following
suffix fields to complete each closed event object:

| New eventType | Suffix in exact order |
| --- | --- |
| `element-movement-stopped` | `actingSide:Side`, `submittedRouteId:Hash`, `breakdownFlowAfter:BreakdownFlow` |
| `breakdown-stop-resolved` | `stop:Stop`, `randomStateBefore:RandomStreamState`, `checks:CheckEvidence[]`, `createdLots:BrokenVehicleLot[]`, `randomStateAfter:RandomStreamState`, `breakdownFlowAfter:BreakdownFlow`, `sources:Source[]` |
| `breakdown-segment-completed` | `sequencePosition:LandSequencePosition`, `breakdownFlowAfter:BreakdownFlow` (idle), `sources:Source[]` |

`element-movement-stopped` flow embeds the newly recorded deliberate stop; submitted route ID is
public handle, compared to current owner capability. Other two events have System authority.
Breakdown completion's sequence is the first-side Combat checkpoint. Existing RandomStreamState
codec shape/algorithm is reused without new seed or stream. Group evidence contains exactly one entry
per stop input (including no-roll entries), in ordinal `(vehicleTypeId,profileId,cohortId)` order.

```text
CheckEvidence = {checkId:Hash, input:CheckInput, rawBandId:Id,
                  effectiveBandId:Id?, status:CheckStatus, roll:RollEvidence?,
                  workingAfter:I32, brokenBefore:I32, brokenAfter:I32,
                  highestEffectiveCheckedBandIdAfter:Id?, sources:Source[]}
CheckStatus = no-working-points | raw-bp-not-above-three | below-check-surface |
              band-not-higher | rolled
RollEvidence = {firstDie:I32, secondDie:I32, coordinate:I32,
                 randomCursorBefore:U64, randomCursorAfter:U64,
                 printedLabel:I32, fraction:Fraction, rulingId:Id, lossCount:I32}
```

`rawBandId`/effective band always rederive, even for no-roll status. Only `rolled` permits nonnull
roll evidence. No-roll working/broken/check memory unchanged, no lot, no RNG draw. Successive eligible
roll cursors form a contiguous chain from event before-state to after-state, including rejection
sampling. Zero-cohort resolution has empty checks/lots and equal RNG states. Rules sources are not
arbitrary annotations: compare to normalized authority when reconstructing event bytes.

## Outward contracts and capabilities

`CampaignObservationV6Serializer`: successor contract 7/policy above. Insert `capabilityProfileId:Id`
after `scenarioId`; append `ownBrokenVehicleLots:ObservedLot[]` after `decisionState`.
`ObservedLot={cohortId:Id,locationId:Id,pointCount:I32}`; aggregate own lots by cohort/location, sort by
that pair, never publish lot/stop/check ID or provenance. Publish only when the existing policy permits
that audience's phasing-own rows. Reaction owner's raw rows and lots stay empty, including during its
System stop. Apparent enemy representations never expose lots or working/initial loss calculations.

Decision-state successor closed delta:

- `normal` becomes `{kind:"normal",activeMovement:ObservedRoute?}`.
- Existing `phasing-waiting` and `reacting` shapes remain exactly as predecessor.
- Add `{kind:"breakdown-waiting"}` with no stop/route/reason/dice/count fields.
- `ObservedRoute={routeId:Hash,elementId:Id,originLocationId:Id,currentLocationId:Id}` is exposed only
  for the normal phasing owner with an open route; all other normal projections set it null.
  Phasing waiting own rows remain governed by existing rules; no new active-route detail is added there.

`CampaignProjectedDecisionHistorySerializer`: root contract 2, exact inherited fields/order and
successor decision-state shape. It must use the same projector and omit authoritative flow/stop data.
Observation position during pending stop is public `land.position.breakdown-stop` with System actor
and no active side; origin/suspended private state does not enter its public position serialization.

New capability handle hashes have ordered inputs `{domain,campaignId,rulesetHash,stateVersion,audience}`:
route domain `sandtable.observation.movement-route.v1` then appends `elementId,originLocationId,
currentLocationId`; System stop domain `sandtable.action.breakdown-stop.v1` appends nothing. These
use only approved public facts/System envelope, never hidden cohort cardinality/binding. They are
recomputed at each state. They are not the similarly named trusted Route/Stop identities.

Candidate semantics (for existing SHA-256 action ID) have the following exact field order:

```text
{contractVersion:1,kind:"stop-element-movement",routeId:Hash}
{contractVersion:1,kind:"resolve-breakdown-stop",stopId:Hash}
{contractVersion:1,kind:"complete-breakdown-segment",operationStage:1}
```

Candidate wire bodies follow predecessor candidate serializer conventions: envelope contract/action
ID/kind then matching semantic payload. `stopId` in the System action is opaque capability handle,
not authority Stop ID. System handle is not projected to either player. Normal Truck moves reuse
ordinary move candidate field shape; audience/state/rules root plus new policy determines admission.
Reaction move/completion/closure shapes and public hash domains remain unchanged; state-scoped IDs
rehash naturally. Existing Movement completion candidate shape stays unchanged but is absent until
flow idle. Submission/acceptance envelope fields and version remain unchanged.

Task 006 creates `user-space-disclosure-manifest.v2.json`: inherit v1 schema/key structure, set
manifestVersion 2/policy v2, replace Observation/history types with successors, retain all existing
forbidden Reaction raw fields, add authority flow/stop/check/lot identity as forbidden everywhere
outward except explicitly approved ObservedRoute/ObservedLot projections above. Include new candidate
variants and System-only audience rules in the audited contract closure. No blanket allow for new
Core records. User-space boundary tests must fail if a raw authority record becomes reachable.

Runner envelope versions stay unchanged: their generic event payload registry now admits exactly the
successor tuples above for current runs, rejects predecessor/mixed event identities, reconstructs
complete snapshots and re-adjudicates every event from prior authority. This registry change does
not permit arbitrary JSON fields. Derive route/stop/check counts from existing action-kind counters and the new event payloads;
final lots and exact cursors belong in those trusted payloads, with no extra report-root fields; player reports consume only projected history.

Task 002 implementation/evidence: [dormant outcome Rules](../research/breakdown-outcome-rules.md).
Its artifact and ruling factories are complete; full Ruleset 9 hash waits for sequence 4 and coupled
registration. Active Ruleset 8 and schema 1 stay unchanged.
