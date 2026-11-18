# Local game API: closing MAP-09, and what changes for networked play

**Status:** draft plan for review, not an accepted contract. Written in
response to a direct question about API architecture/security while RC
blocker work continues elsewhere. Nothing here has been built or decided
with the user yet — every recommendation is flagged as a recommendation,
not a fact.

**Scope correction up front:** this doc originally set out to design "the
API" from scratch. It isn't starting from scratch. A real, security-conscious
local HTTP API already exists
(`src/Cna.OrleansHost/Api/LocalGameApi.cs`, mapped in `Program.cs:56`) and the
transport/architecture decisions are already made in
[the Maproom plan](local-game-server-maproom-plan.md)'s "Accepted decisions"
table: HTTP (via Angular `HttpClient`, OpenAPI-generated typed client) for
request/response, SignalR for audience-safe push notifications with "HTTP
readback restores current truth" (the push is a wake-up signal, never a
trusted data channel), Core owns rules/legal actions/RNG/fog-of-war, client
owns presentation/drafts only. This plan does not re-decide any of that.

What's actually missing is named already, in the same doc's own status
table: **MAP-09** ("public HTTP submit/status/receipt lookup pending" —
`local-game-server-maproom-plan.md:234`) and **MAP-11** (real order forms,
blocked on MAP-09). This doc is the concrete plan for finishing MAP-09, plus
a clearly separated section on what has to change when play moves from
local hot-seat to networked multiplayer — which the Maproom plan explicitly
scopes out today ("Loopback-only development ingress; remote multiplayer is
later work," same decisions table).

## What already exists today (read, not assumed)

`LocalGameApi.MapLocalGameApi()` is mounted only when
`app.Environment.IsDevelopment()` (`Program.cs:32`, `LocalGameApi.cs:13`) —
the whole surface is dev-only right now, not reachable in any real
deployment. Within that gate, for every route under `/api/v1`:

- Loopback-only (`IsLoopback`: remote IP must be a loopback address AND
  Host header must be `localhost`/`127.0.0.1`/`::1`) and same-origin
  enforced (`IsSameOrigin`: `Sec-Fetch-Site` must be empty or
  `same-origin`, and any `Origin` header must exactly match scheme+authority
  with no path/query/fragment).
- CSRF: a per-session token issued at `GET /bootstrap`, required on every
  mutating request via an `X-Sandtable-CSRF` header, checked with an exact
  single-value match (`ValidCsrf`).
- Session cookie is `HttpOnly`, `Secure` when HTTPS, `SameSite=Strict`,
  scoped to `Path=/api/v1` (`CookieOptions`).
- Every request body is capped at 4096 bytes
  (`IHttpMaxRequestBodySizeFeature`), and every JSON body is validated by
  exact shape — exact property count, exact types, exact enum/contract
  version — before anything is read out of it (see `POST /campaigns` and
  `POST .../seat` for the pattern). Malformed input never reaches the
  engine.
- `POST /campaigns` takes a client-supplied `requestId` (a GUID) for
  idempotent creation — the create-or-return-existing semantics this
  project already uses elsewhere for retry safety.
- `POST .../seat` (issue #389) follows the same retry-safety idiom for its
  own one-use grant: the grant and the caller's old session are consumed
  atomically with minting the new one, so if the reply carrying the new
  cookie is lost, an exact retry (same cookie, same `grantId`, same
  campaign, same CSRF token) recovers the *same* new session/token instead
  of failing — see `LocalSessionStore.RedeemAsync`/`RecoverRedemption`.
  Recovery is bounded by the grant's own original expiry and stops working
  once the new session itself is gone (relinquished, superseded by a later
  seat switch, or swept) — a retry past that point cannot be recovered and
  must re-authenticate from scratch, by design.
- `GET /campaigns/{id}/view` is a real worked example of side-safe
  projection done right: it maps the session's seat to a
  `CampaignActionAudience`, reads a private cut via
  `ITemporaryPrivateCutReader`, and only then calls
  `TemporaryPlayerViewProjection.Map(reply.Cut, session)` — the server
  decides what a seat sees; nothing about the full world ever reaches the
  client for local filtering. It also re-checks the session after the async
  read (`store.Find(session.Token) != session`) to handle a seat
  switch/revocation racing the in-flight read — a real concurrency edge
  case, already handled, not theoretical.
- Every response carries an explicit `contractVersion` (bootstrap, create,
  seat, session, errors) — consistent with this engine's existing
  hash-pinning/versioning discipline elsewhere (`ContentHash`,
  `*CheckpointContractVersion` fields). New endpoints should keep doing this.

**What's not there yet:** nothing in this file maps onto
`CampaignExercises.Query`/`Submit` (`CampaignExercises.cs:3833`, `:4193`) —
the actual legal-action-query and action-submission loop. `GET .../view` is
read-only. There is no endpoint a player's chosen action can go through.
That's MAP-09's "public submit" gap, exactly.

## MAP-09: the concrete next slice

Add two routes to `LocalGameApi`, following the exact pattern already
established above (same CSRF/cookie/shape-validation/loopback discipline —
don't invent a second pattern):

1. **`GET /api/v1/campaigns/{campaignId}/actions`** — wraps
   `CampaignExercises.Query(session, audience)`. Resolve `audience` from the
   session's seat exactly like `GET .../view` already does. Returns the
   `CampaignLegalActionSet` (campaign id, state version, ruleset hash,
   position id, audience, candidates, policy id) as JSON, or the existing
   `ApiError` shape on `CampaignLegalActionQueryResult.Rejected`. This is
   already side-scoped and already carries a state version for staleness
   detection — it maps onto the wire almost directly.
2. **`POST /api/v1/campaigns/{campaignId}/actions`** — wraps
   `CampaignExercises.Submit(session, submission)`. Build the
   `CampaignActionSubmission` from the validated request body
   (`contractVersion`, `expectedStateVersion`, `expectedPositionId`,
   `actionId`, plus whatever the specific action's choice payload needs —
   check how `CampaignSourceSetupAction`'s `ChoiceJson` is shaped for the
   precedent). Reject on `ExerciseStepResult.Rejected` with the specific
   `CampaignActionSubmissionRejectionReason` surfaced as the error `code`
   (stale state, action not legal, campaign mismatch, etc. — these already
   exist as a real enum, don't re-invent error categories). On
   `Accepted`, return the receipt
   (`CampaignActionAcceptanceReceipt`) plus enough of the successor view to
   avoid a required round-trip (or require the client to immediately
   `GET .../view` again — pick one, note the tradeoff: returning the view
   inline saves a request but duplicates the side-safe-projection logic's
   call site; a forced re-fetch keeps one code path but costs a round trip).
   Recommend the forced re-fetch to start, matching "HTTP readback restores
   current truth" from the accepted decisions table — optimize later only if
   it's actually slow.

**Idempotency:** `CampaignActionSubmission` is state-version-scoped
already (`ExpectedStateVersion`), which gives duplicate-submission
protection for free *if* the client retries with the same submission after a
lost response — the second attempt will either re-apply harmlessly (if the
engine's accept path is idempotent on an unchanged state version — verify
this, don't assume) or correctly reject as stale once the first one actually
landed. This needs an explicit test either way: the Maproom plan's own
acceptance criteria for MAP-09 already calls out "route-level duplicate/lost-reply/cancellation
reconciliation still required" — that test is part of this slice, not
optional polish.

**Acceptance for this slice:** one Playwright/API-level test that queries
legal actions, submits one, and confirms the successor view reflects it
(mirroring MAP-11's own stated precondition); one unit/integration test for
a duplicate submission (same `expectedStateVersion`, resubmitted after the
first already landed) proving it doesn't double-apply; one for a stale
submission (wrong `expectedStateVersion`) proving a clean rejection, not a
silent corruption or a thrown exception that leaks internal state.

## Beyond MVP: what changes for networked play

The Maproom plan deliberately scopes today's work to "local development, one
host process and one browser application... loopback-only... remote
multiplayer is later work." That boundary is doing real work — don't blur
it while building MAP-09. But since the long-term product is a networked
web game, it's worth naming now what specifically has to change later, so
MAP-09's design doesn't accidentally foreclose it:

- **Identity.** Today "a seat" is a cookie-bound in-memory session with no
  real identity behind it (`LocalSessionStore`, `lifetime:
  "temporary-memory"` in `SessionInfo`). Networked play needs a real account/
  identity model before two seats can be different untrusted parties on
  different machines. This is a genuine product decision (accounts? invite
  links? something else?), not an engineering default to pick alone.
- **Trust boundary widens.** Local hot-seat's loopback+same-origin check is
  a reasonable proxy for "both seats have physical access to this machine."
  That proxy is meaningless once seats are on different machines — the CSRF/
  cookie/same-origin scaffolding stays (it's still correct practice), but
  `IsLoopback` stops being a security boundary and needs to be replaced with
  real session-to-identity binding plus rate limiting per identity, not per
  loopback address.
- **SignalR groups must be audience-safe, explicitly.** The accepted
  decision is already "audience-safe notifications" — when seats are on
  different machines this needs to be enforced as a real per-campaign,
  per-side group membership check at hub-connection time, not just a client
  convention. Worth a dedicated test the same way `GET .../view`'s session-
  generation race already has one.
- **The dev-only gate has to become a real one.** `IsDevelopment()` is doing
  double duty right now as both "feature flag" and "security boundary."
  Before any networked deployment, that needs to split into an explicit
  environment/deployment posture check, independent of ASP.NET's
  development-environment flag (which is easy to accidentally leave on).
- **Body-size/rate limits need real numbers for a public network,** not the
  current 4096-byte cap chosen for a trusted loopback client — not
  necessarily larger, but deliberately chosen rather than inherited.

None of this blocks MAP-09. It's listed so MAP-09's implementation doesn't
quietly bake in assumptions (e.g., trusting the loopback check as "the"
security boundary everywhere) that become expensive to undo later.

## Decisions (2026-11-17, synced with the user)

- **Submit-endpoint response shape: forced re-fetch**, not an inline
  successor view. One code path for side-safe projection; the extra
  round-trip is an acceptable cost until actually measured otherwise.
- **Identity model for networked play: deferred entirely.** Not needed for
  MAP-09 (local hot-seat keeps using the existing cookie session). Revisit
  when networked play is actually scoped — it's a product question
  (accounts vs. invite-links vs. something else), not an engineering
  default to pick now. MAP-09's implementation must not assume either
  answer.
- **Different-machines-during-hot-seat: not actually an open question.**
  Remote-desktop into a machine still runs the browser on that machine, so
  its requests are still loopback from the server's point of view — today's
  design already handles it with no changes needed. Only two browsers on
  two physically separate machines counts as "networked," and that's
  already correctly out of scope.
- **The `IsDevelopment()` dev-only/security-gate split is real but explicitly
  not part of MAP-09.** Tracked as its own follow-up
  ([#366](https://github.com/dills122/sandtable/issues/366)) rather than
  bundled into this feature PR, so both stay independently reviewable.

## Not done here

No code was written or changed for this plan. No file outside this new
document was touched.
