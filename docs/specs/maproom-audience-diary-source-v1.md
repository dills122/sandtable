# MAP-08B audience diary source proposal

Status: **proposed contract for review; no post-step view or diary implementation**. This
document narrows the [MAP-08B side projection prerequisites](maproom-side-projection-prerequisites-v1.md)
for the development-only `breakdown-truck-lab` fixture. It does not approve player copy or
authorize decoding canonical events in the browser or HTTP handler.

## Existing boundary and missing source

MAP-04 `CoherentPlayerView` requires `warDiary` (at most 50 `DiaryExcerpt` values) and
`nextDiaryCursor`. Each excerpt requires `entryId`, unsigned-decimal `audienceCursor`,
`title`, and `body`; `formationId` is optional. The cursor is an unsigned-decimal string,
independent of authority `stateVersion` and `audienceRevision`. See
[`openapi.v1.json`](../../src/Cna.Game.Contracts/openapi.v1.json).

The current [private audience tracker](../../src/Cna.OrleansHost/TemporaryAudienceTracker.cs)
receives two coherent successor side cuts and retains locations, own elements, contacts,
actions, audience revision, and action handles. It has no committed-outcome diary source or
cursor. [`TemporaryCampaignGrain.SubmitAsync`](../../src/Cna.OrleansHost/TemporaryCampaignGrain.cs)
holds accepted canonical event/receipt/snapshot evidence privately, then advances the tracker
with successor cuts alone. [`TemporaryPlayerViewProjection`](../../src/Cna.OrleansHost/Api/TemporaryPlayerViewProjection.cs)
therefore admits only state version 1 and emits an empty diary with cursor `0`. Keep that
guard until this source contract, copy catalog, and tests are implemented and reviewed.

`CampaignHostAcceptedStep.EventRecords` contains canonical private event bytes, not
`DiaryExcerpt` values. The active [`CampaignExercises.Submit`](../../src/Cna.Core/Exercises/CampaignExercises.cs)
path writes through [`CampaignCurrentEventSerializer`](../../src/Cna.Core/Campaigns/CampaignCurrentEventRuntime.cs):
current creation, 11 preamble event classes, and eight Breakdown successor event classes
(20 typed classes total). The historical `CampaignEventSerializer` alone is not the
current accepted-step inventory. Only a trusted Core hosting adapter can validate an exact
typed accepted event, its actor and source pins, and derive the proposed side-safe outcome below.
The Orleans tracker must not parse raw event JSON into presentation text.

## Proposed private source contract

The first contract-only implementation is
[`CampaignAudienceOutcomeSource.Decide`](../../src/Cna.Core/Hosting/CampaignAudienceOutcomeSource.cs).
It accepts a trusted predecessor session and one canonical accepted-step record, then
recomputes the step **in memory only** to compare its canonical command, event, receipt,
and successor snapshot. It never commits a second turn or advances the supplied session.
Both predecessor and successor side cuts must be coherent before it emits a paired,
versioned host-private proof with source and committed-head hashes. Unknown or mismatched
input returns no typed proof for either side. The proposed entry copy keys remain
`copy-catalog-unapproved` and unavailable; no diary cursor, text, HTTP response, or
successor `/view` consumes this proof yet. This is only the accepted-outcome decision
source prerequisite, not the presentation approval or diary projection.

The temporary host now consumes the proof only for the
[private outcome head binding](maproom-private-outcome-head-binding-v1.md). It checks the
canonical accepted step and paired proof against the current tracker head and both successor
cuts before installing the private successor. This retains one latest proof, not approved
entry copy, a diary history/cursor, or permission to remove the public initial-only guard.

For each ordered event in an accepted step, the Core hosting adapter supplies one
immutable, versioned `AudienceOutcomeDecision` per authorized side **in addition to**
the canonical evidence and both successor cuts. Each decision carries the
campaign/source pins, private prior and successor committed-head identity, event index,
event contract kind/version, acting audience when defined, and either `approvedEntry`
with a catalog copy key and permitted own element reference, or `silent` with an
approved reason code. This is host-private data; no authority
version, event bytes, Core action ID, opposing element ID, `RepresentationId`, seed, receipt,
or snapshot crosses the browser boundary. An explicit `unavailable` classification is
required for unknown event kinds/versions, incoherent pins, missing actor evidence, or an
unapproved copy key. A generic silence fallback is forbidden.

The adapter validates every exact accepted event against the submitted action and actor,
then evaluates both sides independently using the *same* predecessor and successor
committed cuts. Event class or an `ActingSide` field alone does not prove seat visibility.
For an own-order entry, the actor must be that authenticated seat (never System), the event
must match the accepted action kind, and any element must be that seat's permitted own
element in the coherent side cut. Opposing and System events may not borrow that proof.
An entry-kind event submitted by System is unsupported and unavailable until a separate
explicit policy is approved. A silent lifecycle classification also requires an exact
known event/action/actor pairing and verified source pins; the event class alone cannot
silence an unexpected accepted step.

The table is an **allowlist proposal** for the 20 classes serialized by the current path.
It does not establish that every kind is reachable in the temporary fixture. `Entry` means
one excerpt for the authenticated acting seat and `silent` for the other; `silent` means no
excerpt for either seat. A row is eligible only when its exact event version, accepted
action candidate, submitted audience, receipt, and predecessor/successor heads agree.
The event's `ActingSide` is a cross-check, never an audience grant. `System` may use only
the listed System variants. Exact `title`/`body` strings need approval in a new pinned
presentation catalog version before implementation. Keys below identify proposed copy,
not permission to invent text from event fields.

| Current typed accepted event | Accepted action/audience proof | Proposed diary decision / copy key |
| --- | --- | --- |
| `CampaignCreatedV10` | Creation only; no submitted action | Initial-only silent; later appearance unavailable; cursor `0`. |
| `InitiativeDetermined` | `ResolveInitiativeAction`, `System` | Silent; no outcome details enter diary. |
| `NoObligationNavalConvoyScheduleResolved` | `ResolveNoObligationNavalConvoyScheduleAction`, `System` | Silent. |
| `NoObligationTacticalShippingResolved` | `ResolveNoObligationTacticalShippingAction`, `System` | Silent. |
| `InitiativeOrderDeclared` | `ActFirstAction` or `ActLastAction`, same authenticated `DeclaringHolder`; declared first/last side agrees with candidate | Entry only for declaring seat; `diary.initiativeOrder`; omit opposing order value. |
| `WeatherDetermined` | `ResolveWeatherAction`, `System` | Silent; weather presentation needs separate approval. |
| `NoObligationOrganizationResolved` | `ResolveNoObligationOrganizationAction`, `System` | Silent. |
| `NoObligationNavalConvoyArrivalResolved` | `ResolveNoObligationNavalConvoyArrivalAction`, `System` | Silent. |
| `NoObligationFleetAssignmentResolved` | `ResolveNoObligationFleetAssignmentAction`, `System` | Silent. |
| `NoObligationFleetRepairResolved` | `ResolveNoObligationFleetRepairAction`, `System` | Silent. |
| `ReserveElementDesignated` | `DesignateReserveAction`, same human `ActingSide`; own element in side cut | Entry for acting seat; `diary.reserveDesignated`; map private element ID to permitted outward formation ID. |
| `ReserveDesignationCompleted` | `CompleteReserveDesignationAction`, same human `ActingSide` | Entry for acting seat; `diary.reserveCompleted`. |
| `ElementMovedV3` (`element-moved` v3) | `MoveElementAction`, same human `ActingSide`; own element in side cut | Entry for acting seat; `diary.elementMoved`; omit cost, representation and unapproved route facts. Historical `ElementMoved` is not a current successor. |
| `ElementMovementStopped` (`element-movement-stopped` v1) | `StopElementMovementAction`, same human `ActingSide` and route/action capability | Entry for acting seat; proposed `diary.movementStopped`; omit private route and stop IDs. |
| `BreakdownStopResolved` (`breakdown-stop-resolved` v1) | `ResolveBreakdownStopAction`, `System`; stop capability and heads match | Silent. System resolution cannot inherit the prior player's audience. |
| `ReactingElementMovedV2` (`reacting-element-moved` v2) | `MoveReactingElementAction`, same human `ActingSide`; own reacting element and outward opportunity alias match | Entry for acting seat; proposed `diary.reactionMoved`; omit private opportunity/window IDs. |
| `ReactionParticipantCompletedV2` (`reaction-participant-completed` v2) | `CompleteReactionParticipantAction`, same human `ActingSide`, opportunity and window capabilities match | Entry for acting seat; proposed `diary.reactionCompleted`; no opposing opportunity details. |
| `ReactionWindowClosedV2` (`reaction-window-closed` v2) | `DeclineReactionWindowAction` by same human `ActingSide` and reason `PlayerDecline`; or matching `System` unavailable/timeout/no-eligible close action and reason | Human decline: entry only for acting seat, proposed `diary.reactionDeclined`. Three System close reasons: silent. Any other action/reason/audience pairing is unavailable. |
| `MovementSegmentCompletedV2` (`movement-segment-completed` v2) | `CompleteMovementSegmentAction`, same human `ActingSide` | Entry for acting seat; `diary.movementCompleted`. Historical v1 completion is not current. |
| `BreakdownSegmentCompleted` (`breakdown-segment-completed` v1) | `CompleteBreakdownSegmentAction`, `System`, idle Breakdown flow and heads match | Silent. |

No title or body is copied from an event, command, receipt, model response, or element ID.
The catalog owns fixed locale-specific text and any permitted substitutions. Formation
labels and outward formation IDs come from the same pinned, side-permitted presentation
source used by the Maproom view. Missing catalog key, missing own formation, unknown
event/action pairing, or newly added event kind makes that committed side projection
unavailable until explicitly reviewed. An accepted event that is classified silent may
still change map/ledger/actions and the ordinary audience revision; silence only governs
the diary cursor.

## Cursor, retention, and commit coherence

- Start each campaign/side diary cursor at `0`. On one approved entry, increment only that
  side's cursor by one after overflow checking. Never derive it from authority version,
  journal sequence, action ID, timestamp, or HTTP read count. A silent or hidden-only
  committed step leaves that side's cursor unchanged.
- Assign an unpredictable, campaign/side/session-generation-scoped outward `entryId` to
  each approved entry. `audienceCursor` is the entry's assigned decimal cursor. Retained
  entries keep their private per-side cursor and approved content across an authorized
  session-generation change, but receive fresh, unrelated outward `entryId` values when
  read under the new generation. An old `entryId` never crosses a generation or seat.
  Retries of an accepted request within one generation reuse the same decision, outward
  entry ID, cursor, and text; they do not append a second entry. Opposing seats cannot
  correlate entry IDs.
- Keep the latest 50 approved entries per side in commit order. `warDiary` returns this
  window in ascending `audienceCursor` order; `nextDiaryCursor` is that side's latest
  cursor, including when the window is empty. The current MAP-04 view defines no
  independent diary pagination route, so this proposal does not promise older entries.
- Advance both side cuts and all ordered diary decisions as one private committed-head update,
  before publishing either side's view. Any mismatch, skipped cut, unsupported source,
  or failed projection makes reads audience-neutrally unavailable. A diary entry must
  never appear with a map, ledger, or action set from another committed head.
- An approved entry changes that side's audience revision even if all other permitted
  fields are unchanged. A hidden-only step cannot change outward revision, action-set
  token, action handles, contact IDs, or diary cursor. Existing contact disappearance
  and reappearance rules still apply even if no HTTP read intervenes.
- The temporary grain and tracker are process-local. A restart loses the campaign,
  sessions, diary, and cursors; a reactivation or unverified continuity gap cannot
  rebuild diary history by guessing from the latest side cut. A future durable replay
  needs a separately versioned, verified source and retention contract.

## Acceptance before removing the initial-only guard

1. Pin and review a new presentation catalog version with exact `title`/`body` for each
   approved key, length and localization rules, and source identity. No runtime fallback
   strings.
2. Enumerate the 20 current typed classes through `CampaignCurrentEventSerializer`,
   including all eight Breakdown successor classes and both v3/v2 replacement versions.
   Test each table row for both seats and each `ReactionWindowClosedV2` reason variant.
   Test actor/action/reason mismatch, private element references, unknown event/version,
   missing copy key, System submission of an entry kind, and a historical v1 movement
   event substituted for its current version. Assert hidden opposing action/event
   details never enter either payload or logs.
3. Test initial cursor `0`; one own accepted order gives exactly one entry/cursor `1`;
   exact retry gives no duplicate; opposing seat remains at its prior cursor. Test a
   silent lifecycle step and a hidden-only step preserve the untouched side's cursor,
   revision, token, action handles, and contact identities.
4. Test 51 visible entries retain exactly the latest 50 with ordered cursors and a
   monotonic high-water mark. After handoff and authorized reentry to the same seat
   under a new session generation, readback retains that side's cursor and approved
   content but rotates every outward entry ID; no old-generation or other-seat ID
   appears. Exact retry within one generation keeps its ID. Test decimal-string
   boundaries and overflow failure.
5. Test consecutive accepted commits with no HTTP read, both coherent side cuts at each
   head, and a later read that cannot mix successor map/ledger/actions with predecessor
   diary. Missing/unsupported source and reactivation gaps return the same neutral
   unavailable response for either seat.
6. Test no canonical event/receipt/snapshot bytes, Core IDs, opposing element IDs,
   `RepresentationId`, source seed, or authority state version in browser JSON, safe
   errors, or logs. Run real in-process two-seat HTTP tests at the loopback/session
   boundary after the private source exists.

## Review decisions still required

Approve the proposed entries, including stop, reaction move/completion/decline, and
movement completion, or explicitly classify any one of them silent. Approve exact fixed
catalog copy for each approved key. Freeze a typed Core-hosting outcome API that proves
each accepted event/action/reason pair and both committed heads without exposing canonical
evidence to the Orleans presenter. Until those decisions and tests pass, successor `/view`
remains unavailable and this document is a proposal, not a claim of a playable API or
durable history.
