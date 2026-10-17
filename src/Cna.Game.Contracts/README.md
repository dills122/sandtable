# Maproom browser contract v1

`openapi.v1.json` is the outward HTTP source for the development-only rules lab. It defines
browser-visible shapes, routes and errors; MAP-08/09 will implement handlers. The current package
contains no campaign host or save/resume implementation. Intelligence protobuf and Core snapshots
remain separate contracts.

## Lifecycle

1. `GET /api/v1/bootstrap` on loopback establishes an operator cookie and returns a CSRF token.
   MAP-08A requires positive same-origin Fetch Metadata or matching `Origin`; requests with
   neither are denied. Bounded local session capacity returns typed `503 unavailable` when full.
2. `GET /api/v1/fixtures` lists admitted public descriptors. `POST /api/v1/campaigns` uses one
   fixture ID and unique request ID; the server binds ruleset, setup, content and configuration
   hashes and returns `seatGrants.axis` and `seatGrants.commonwealth`, each a distinct short-lived,
   CSPRNG-issued opaque grant ID. These grants are operator-only and never appear in campaign
   listings. OpenAPI enforces exactly those two seat keys; the `x-distinct-values` constraint
   requires the MAP-08 host to reject equal grant IDs because OpenAPI 3.0 cannot compare values.
3. `POST /api/v1/campaigns/{campaignId}/seat` redeems one grant. Server rotates a separate
   HttpOnly SameSite=Strict seat cookie and `sessionGeneration`. Seat identity comes only from
   this server-owned session. A seat switch invalidates the prior generation before reply.
   Malformed requests and absent operator sessions return typed `400` and `401` errors.
4. `GET /api/v1/campaigns/{campaignId}/view` returns one committed audience cut: map items,
   legal actions, formation ledger and bounded War Diary excerpts. Every field derives from the
   permitted observation. `audienceRevision` and diary cursor advance only for this audience's
   visible changes; neither is the private authority state version or raw journal sequence.
   `actionSetToken` and each outward `actionId` are opaque audience-issued capabilities. Their
   values must remain stable across hidden-only commits. The host privately maps them to current
   Core candidates and revalidates legality at submission.
5. `POST /api/v1/campaigns/{campaignId}/actions` uses a UUID request ID, current generation,
   action-set token, position ID, outward action ID and action contract version. It accepts no
   actor, generic payload or private authority revision. A stale token or action rejects with a
   static audience-safe reason and no event, RNG advance or charge.
6. A submission returns HTTP 200 with a discriminated `pending`, `accepted` or `rejected`
   receipt. HTTP success means receipt lookup succeeded, not that action was accepted. Pending
   exposes no successor and is not a save. Accepted alone carries the current audience revision.
   Request ID binds the canonical request and server-owned seat for deduplication. An exact retry
   returns the **latest retained status**: pending may later settle to accepted or rejected,
   while a final outcome is immutable. Reusing the ID with a different seat or canonical payload
   conflicts. `GET .../receipts/{requestId}` authorizes campaign and
   seat before lookup and returns the same latest retained status. A lost or cancelled HTTP reply
   does not undo a commit; receipt lookup and current view reconcile it. Conflict errors use
   fixed, audience-safe text.
7. `DELETE .../seat` invalidates generation and clears seat cookie. Clients must clear prior-seat
   DOM, drafts, overlays and subscriptions before entering the next seat. Session reads and seat
   relinquish expose typed `403` errors for rejected loopback/origin checks; relinquish also
   rejects invalid CSRF with `403`.

Mutations use CSRF header and same-origin cookies. Local host must reject cross-origin mutation,
validate Host/Origin against the loopback profile and never log cookies, grants, player payloads or
hidden identities. Operator bootstrap is for local development only. Production identity and
remote play are outside this contract.

## Data rules

Audience revisions and cursors are unsigned decimal **strings**, including values beyond
JavaScript's safe integer range. They count only permitted visible changes. Core `stateVersion`
stays in trusted records and must never appear in player payloads, browser ingress logs or
player tokens.
`formationId`, `contactId` and location IDs are projection-issued audience-safe identities.
Contact IDs must not reveal hidden source IDs or remain stable across visibility loss unless the
observation policy permits it. Action display fields derive from current permitted legal actions
and observations; unsupported kinds are not browser-submittable forms. Ledger rows show name,
kind, position and order status from the same permitted view. No strength, supply or readiness
figure is inferred. Diary text is bounded, sanitized and derived from approved audience facts.
Neither surface exposes Chronicle events, snapshots, private traces, RNG/seed state or hidden
opponent identifiers.

`signalr.v1.json` defines the hub path, negotiate and subscription handshake, server-owned seat
scope, event payload and reconnect rule. SignalR negotiate requires the seat cookie, loopback
same-origin check and CSRF header; direct negotiation bypass is forbidden. `viewChanged` emits
only after a committed change to this audience's permitted view. It carries no hidden event count
or raw journal position. Subscribe first, then read the current HTTP view on every connection;
push is a hint, and missed/coalesced notifications are recovered by HTTP readback. Handoff
invalidates the previous generation and its subscriptions.

`tests/Cna.Game.Contracts.Tests` checks local references, protected operations, a representative
creation and coherent view fixtures, receipt variants, combined cookie/CSRF policies,
SignalR handshake,
wide audience revision strings, nested forbidden properties and lack of client actor input.
The `typescript-angular` client is generated under
`src/Cna.Maproom/projects/maproom-alpha/src/app/api/generated`. It compiles but is not wired to
gameplay. Regenerate with `./src/Cna.Game.Contracts/generate-angular-client.sh` after schema edits.
Configure its base path to the browser origin and supply the CSRF token through client configuration
before use. MAP-04 awaits independent contract review; host mapping belongs to MAP-08/09.
