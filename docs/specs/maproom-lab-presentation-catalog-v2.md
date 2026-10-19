# Rules-lab Maproom diary presentation catalog v2 — owner proposal

Status: **proposed for owner approval; no runtime or browser activation**. This catalog
would extend the development-only `breakdown-truck-maproom.v1` initial-cut presentation
catalog. It does not change adjudication, grant an audience, approve the proposed
entry/silent decisions, or make a successor `/view` available.

## Boundary and version

- Proposed catalog ID: `breakdown-truck-maproom.diary.v2`; locale: `en-CA` only.
  This version is limited to the same development fixture identity as catalog v1:

  | Source component | Exact accepted value |
  | --- | --- |
  | Ruleset hash | `17f3e6047f34b5bf6f5f809055863b664a4bd82a8481db83ee83e0ae5cad3a2a` |
  | Scenario ID | `breakdown-truck-lab` |
  | Setup ID and hash | `rules-lab.breakdown.truck.v1`; `sha256:e6631e81ad8f97e39fd9d7eec93bad7fe2b39db4d2d3059ed94a02dd4093e7a3` |
  | Content ID and hash | `rules-lab.content.breakdown-truck.v1`; `sha256:646e76e69ecceb82216b37d84e950928099acd8a3cb04b51526d0fe631e512ee` |

  The current v1 `SourcePin` is the ruleset hash, scenario ID, setup hash and
  content hash in that order, joined with `|`; the IDs are checked against
  the pinned creation source. An unsupported locale, catalog ID,
  ruleset/scenario/setup/content pin, event version,
  action/reason pairing, or owner mapping makes the paired committed projection
  unavailable. There is no fallback to event text, a model response, or generic copy.
- `CampaignAudienceOutcomeSource` must first prove the exact accepted step and both
  authorized cuts. The host must bind that proof to the committed head and receipt.
  Catalog lookup supplies presentation only after these checks; it never supplies
  eligibility or authority.
- Each approved entry uses its fixed title/body below. No text substitution is
  permitted. `formationId`, when present, is an independent session-scoped outward
  ID for an own formation already present in the same authorized cut. Missing or
  conflicting formation mapping is unavailable, not a text-only fallback.
- Titles are at most 80 characters and bodies at most 160 characters; both fit the
  current `DiaryExcerpt.body` 1,024-character contract. Cursor and entry ID remain
  tracker-owned opaque audience values and are not derived from source event IDs.
  Hidden-only and silent accepted steps do not advance the diary cursor.

## Proposed owner-visible entries

The exact fixed strings below are **candidates**, not approved copy. An entry is
visible only to the authenticated acting seat after the matching typed
event/action/audience/receipt/head proof in
`maproom-audience-diary-source-v1.md`. The other seat receives no excerpt.

| Copy key | Exact title | Exact body | Optional own `formationId` |
| --- | --- | --- | --- |
| `diary.initiativeOrder` | Initiative order recorded | Your choice of acting order was recorded. | No |
| `diary.reserveDesignated` | Reserve assigned | A formation you control was assigned to reserve. | Yes |
| `diary.reserveCompleted` | Reserve choices complete | Your reserve choices were completed. | No |
| `diary.elementMoved` | Formation moved | A formation you control moved. | Yes |
| `diary.movementStopped` | Movement stopped | Movement for one of your formations stopped. | Yes |
| `diary.reactionMoved` | Reaction move recorded | A formation you control made a reaction move. | Yes |
| `diary.reactionCompleted` | Reaction complete | Your reaction opportunity was completed. | No |
| `diary.reactionDeclined` | Reaction declined | You declined the reaction opportunity. | No |
| `diary.movementCompleted` | Movement complete | Your movement segment was completed. | No |

The strings intentionally omit destination, route, movement cost, weather, stop
cause, opposing state, private opportunity IDs, and inferred success. An optional
own `formationId` supplies selection/linking only; the text remains fixed. Approval
of a key also requires the owner to approve the corresponding entry classification
in the source proposal. Silence for the other seat is not evidence that its map,
ledger, action list, or ordinary audience revision stayed unchanged.

## Proposed silent classifications

No excerpt for either seat from the following exact current typed events, after
their matching source proof: initial `CampaignCreatedV10` only;
`InitiativeDetermined`; `NoObligationNavalConvoyScheduleResolved`;
`NoObligationTacticalShippingResolved`; `WeatherDetermined`;
`NoObligationOrganizationResolved`; `NoObligationNavalConvoyArrivalResolved`;
`NoObligationFleetAssignmentResolved`; `NoObligationFleetRepairResolved`;
`BreakdownStopResolved`; and `BreakdownSegmentCompleted`.

`ReactionWindowClosedV2` is conditional: a proven human
`DeclineReactionWindowAction` with `PlayerDecline` uses
`diary.reactionDeclined` for that seat; the exact `System`
unavailable/timeout/no-eligible closure variants are silent for both seats.
Any other action, reason, audience, or event-version pairing is unavailable.
Silence cannot be used as a fallback for a missing copy key, unknown event, or
failed proof.

## Approval and implementation gate

Owner decision needed: approve or revise each entry/silent classification and
the exact `en-CA` strings. Until that decision is recorded and a runtime catalog
is implemented, any proposed copy key must remain unavailable to the tracker;
the connected browser remains on its initial read-only cut. This document does
not define a currently implemented response code.

After approval, implement a versioned catalog in
`TemporaryPlayerViewProjection` and test all current typed variants for both
seats, including authentic no-eligible System reaction closure. Verify
formation mapping, 51-to-50 retention and cursor overflow, hidden-only and
silent cursor stability, exact retry without duplicate append, cross-seat
nondisclosure, unknown pins/locale/key rejection, and raw-ID/receipt/seed
non-leakage. The accepted step, both cuts, tracker head, and catalog version
must form one coherent read; public order submission and durable save remain
separate gates.
