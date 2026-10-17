# MAP-08B side projection prerequisites

Status: contract proposal. No player view, campaign creation endpoint, or playable campaign API is supplied by this document. The MAP-08A `/view` route remains unavailable until these gates pass.

## Why the current boundary cannot serve MAP-04

MAP-06 `CampaignHostAdapter.QueryAudience` returns a coherent, side-specific `CampaignObservationV7` and `CampaignLegalActionSet`. MAP-07 `TemporaryCampaignGrain.QueryAsync` reduces that cut to campaign, version, ruleset, position, observer, and two counts, then returns serialized Core legal actions. The serialized actions carry private candidate IDs and authority version. MAP-04 `CoherentPlayerView` instead requires typed position, map locations, formations, contacts, legal actions, ledger, diary, audience revision/cursor, and opaque action identities. Passing the Core bytes through HTTP or filling required fields with invented values would violate that contract.

## MAP-08B1: trusted side cut and presentation sources

Complete this prerequisite before the MAP-08B HTTP projection. Add a typed, host-private grain result derived from **one** `CampaignHostAdapter.QueryAudience` call against one committed session. Copy only fields needed by the outward adapter; keep `stateVersion`, Core action IDs, canonical command/event/receipt/snapshot bytes, seed, and unpublished opposing identifiers inside trusted code. The grain must not return a System audience cut to browser ingress.

| MAP-04 field | Authority and mapping rule |
| --- | --- |
| `campaignId`, `rulesetHash` | Coherent side observation; must equal the legal set and admitted creation pins. |
| `position.positionId`, `turn`, `phaseId`, `stageId` | Side observation position; decimal turn string. Determine `decision` from supported side decision state and actionable candidate set, not a guessed terminal state. |
| `locations[].locationId` | Published observation topology. Supply labels and coordinates only from a versioned, admitted public presentation catalog bound to the same setup/content identity. Current observation and content topology supply no browser coordinates. Reject missing or mismatched catalog entries. |
| `ownFormations`, `formationLedger` | Group permitted own elements by `ParentFormationId`; use only observed own location and permitted element/formation facts. Labels, kind, and order status require an explicit public/side presentation source or a narrower revised browser schema. Do not infer formation status from an absent Core field. |
| `apparentContacts` | Observation `ApparentOpposingPresences` only. `RepresentationId` is a private Core correlation key, not an outward identity. Issue an unpredictable, campaign/side/session-generation-scoped `contactId` for each currently visible presence. Never publish a raw representation, opposing element, or opposing formation ID. Contact labels need a defined generic policy. |
| `legalActions` | Typed legal candidates from the same cut, translated through a private action map. Publish only admitted human actions. Map any subject or target from side-permitted candidate semantics; never serialize the Core set, action hash, authority version, or a System action. Unsupported candidate kinds make the projection unavailable until mapped explicitly. |
| `warDiary`, `nextDiaryCursor` | Require an audience-safe projection of committed accepted outcomes, with its own cursor and bounded 50-entry readback. Raw Chronicle events and receipts are not diary entries. An initially empty diary may use cursor `0`; later outcomes need an explicit visibility policy before publication. |
| `audienceRevision`, `actionSetToken`, outward `actionId` | Host-owned, side/session-scoped metadata and UUID handles. Advance revision/cursor only on permitted visible changes. Keep action handles and set token stable across hidden-only authority commits when permitted actions are equivalent; remap privately to the newest Core candidate before MAP-09 submission. Never derive a token from `stateVersion` or Core hash. |

MAP-08B1 must define the presentation catalog's owner and pinned version, and the exact public/side visibility policy for labels, coordinates, status, and diary. If those values do not yet exist, revise MAP-04 through a versioned contract change before building a handler; do not manufacture them in `Cna.OrleansHost`.

The host keeps contact correlation private. Within one authorized session generation, a continuously
visible apparent presence keeps its outward `contactId` across hidden-only commits and ordinary
visible updates. Remove that mapping as soon as the presence is absent from the committed side
cut; if a Core representation later reappears, issue a fresh unrelated `contactId`, even when the
private `RepresentationId` is unchanged. Rotate the namespace on a new session generation and
never reuse a contact ID across seats or campaigns. Do not derive outward IDs from Core IDs,
global sequence numbers, location, or authority version. A visibility-loss cut and any later
reappearance each count as a visible change for that side's audience revision. The diary cursor
changes only for an approved diary entry, not merely because a contact disappeared.

## MAP-08B: projection and acceptance

Authorize campaign and seat from the server session before querying the grain. Return one exact MAP-04 `CoherentPlayerView` cut, including the bound session generation. Return an audience-neutral unavailable response if any required source is missing or a cut is incoherent. Creation response/grants must be retained as one idempotent, private operator record; a retry cannot mint different grants for the same accepted request. Keep MAP-09 command submission, SignalR, durable recovery, and public Combat out of this slice.

Focused gates: contract fixture/schema roundtrip; both seats against the same committed head; wrong-seat/campaign and System negatives; no private Core ID (including `RepresentationId`), version, event, snapshot, seed, or opposing element ID anywhere in response/log; catalog pin mismatch fails closed; hidden-only commit preserves outward revision/token/action handles/contact IDs and diary cursor; continuously visible contact retains its ID, disappearance removes it, reappearance receives a new unrelated ID, and seat/session changes cannot correlate IDs; visible commit updates all map, ledger, actions, and diary from one successor; creation retry returns original grants. Run real in-process HTTP tests with the development-only loopback and origin/CSRF boundary.
