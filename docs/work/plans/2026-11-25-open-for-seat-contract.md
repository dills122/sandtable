# Read-only "what is open for my seat" (issue #621)

> **2026-11-26 follow-up:** Discovery is now occurrence-aware for all three synthetic stages
> and the successor-turn prelude (#640, #649–#651). #666 reports the retained Stage 1 second-side
> Reaction window via `ruleNotImplemented`, without legal refs; this is not a resolution.
> Turn 2 Reserve is an open, unaccepted checkpoint, not a playable next turn.

Status: implemented in this PR (route, catalog, tests). Resolves #621 gaps 1-3 and the Host side of gap 4's data.

## Route

`POST /api/v2/campaigns/{campaignId}/openings/open` on the existing v2 group, so it keeps the Development-only,
loopback-only, same-origin gate unchanged. POST only because the seat secret travels in the body like every other
v2 call.

Request:

```json
{ "contractVersion": 2, "credential": { "campaignId": "...", "audience": "axis", "epoch": 1, "secret": "...", "expiresUnixMs": 0 } }
```

Response (`200`):

```json
{
  "contractVersion": 2,
  "campaignId": "...",
  "audience": "axis",
  "open": [
    { "openingStage": "retreat-before-assault", "actionStage": "rba-decline",
      "actingElementIds": ["axis-infantry"], "ruleNotImplemented": false }
  ]
}
```

- `openingStage` is the `stage` to send to `/openings`; `actionStage` is the `stage` to send to `/actions` (they differ
  only for Retreat Before Assault; this is data, not a client rule).
- `actingElementIds` is what to send as `actingElementId`: the fixed key for singleton stages (for example
  `land.initiative-order`) or the seat's own element ids for per-element stages (gap 2).
- `ruleNotImplemented: true` means the decision is genuinely open but the engine cannot resolve it yet (today: a
  Reaction window); the opening call will answer `rule-not-implemented`.
- An empty `open` list means nothing is open for this seat now (other side acting, or position not wired).
- Errors: `401` bad/forged/expired/wrong-campaign credential, `403` gate, `409 unavailable` if Core rejects its own
  forced System step.

The answer is advisory. `/openings` and `/actions` stay the authority: round gates and timeouts are applied there, and
the list can include a stage whose round the next `/openings` call then closes.

## Side-effect free

`QueryOpenForSeat` authenticates the seat, loads the committed head, drains pending System steps on an in-memory copy
(Core sessions are immutable values), and probes the pure draft builders for the candidate stages. It never calls
`PersistAccepted`, never upserts `owner_openings`/`owner_actions`, never advances a round high-water mark, and never
commits its SQL transaction. A test asserts the `campaigns`, `accepted_steps`, `owner_openings`, `owner_actions` and
`seats` tables are byte-identical before and after.

## One table, loud failures

- `CampaignOpenSeatCatalog.Rows` (Core, `src/Cna.Core/Hosting/CampaignOpenSeatCatalog.cs`) maps every
  `PendingDecision` subtype to its candidate Host stages, or to an explicit exception reason (`Unwired`, `SystemOnly`).
- `CampaignDurableAuthorityJournal.OpeningStages` (Host) lists every `CampaignOwnerOpeningKind` once with its opening
  stage, action stage and singleton key.
- Tests: every Pending subtype has exactly one row (Core.Tests); every enum kind is in the Host table once and every
  catalog stage name exists there (OrleansHost.Tests).
- Positions without a Pending subtype (pregame/source setup and the pre-#394 Stage 1 first-side chain) probe every
  registered stage.

### Procedure when a slice adds a `PendingDecision` subtype

Add one line to `CampaignOpenSeatCatalog.Rows`:

```csharp
[typeof(NewPending)] = CampaignOpenSeatRow.Unwired("why no Host opening kind exists yet"),
```

When its Host opening kind is wired, replace it with `CampaignOpenSeatRow.Stages("host-stage-name")` and add the kind to
`CampaignDurableAuthorityJournal.OpeningStages`. Automatic Core steps use `CampaignOpenSeatRow.SystemOnly("reason")`.
Until the row exists, `EveryPendingDecisionSubtypeHasExactlyOneCatalogRow` fails and names the missing subtype.

## Not covered

- Gap 4 (Host returns its own `actingElementId`): the route returns the Host's ids, so clients no longer need echo
  semantics for discovery; `/openings` still returns the Host's own id in its response.
- Opponent-hidden information: the list is derived per audience through the same visible-cut builders as `/openings`; it
  reveals only stage names and the seat's own element ids.
