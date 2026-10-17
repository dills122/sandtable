# Maproom browser contract v1

`openapi.v1.json` is the outward HTTP source for the development-only rules lab. It defines
browser-visible shapes, routes and errors; MAP-08/09 will implement handlers. The current package
contains no campaign host or save/resume implementation. Intelligence protobuf and Core snapshots
remain separate contracts.

## Lifecycle

1. `GET /api/v1/bootstrap` on loopback establishes an operator cookie and returns a CSRF token.
2. `GET /api/v1/fixtures` lists admitted public descriptors. `POST /api/v1/campaigns` uses one
   fixture ID and unique request ID; the server binds ruleset, setup, content and configuration
   hashes and returns two short-lived seat grants. These grants are operator-only and never appear
   in campaign listings.
3. `POST /api/v1/campaigns/{campaignId}/seat` redeems one grant. Server rotates a separate
   HttpOnly SameSite=Strict seat cookie and `sessionGeneration`. Seat identity comes only from
   this server-owned session. A seat switch invalidates the prior generation before reply.
4. `GET /api/v1/campaigns/{campaignId}/view` returns one committed audience cut: map items,
   legal actions, formation ledger and bounded War Diary excerpts. Every field is derived from
   the permitted observation. `stateVersion` binds submissions; `audienceRevision` and diary
   cursor indicate only audience-visible changes. They are not raw journal sequence numbers.
5. `POST /api/v1/campaigns/{campaignId}/actions` uses a UUID request ID, current generation,
   state version, position ID, action ID and action contract version. No actor or generic action
   payload is accepted. Host maps the server-issued action ID to a still-current legal action.
6. A submission may return `pending`, `accepted` or `rejected`. Pending means no successor is
   exposed or claimed saved. A retry with the same request ID and canonical request returns the
   original outcome; changed seat or payload conflicts. Fresh stale requests reject before an
   event, RNG advance or charge. `GET .../receipts/{requestId}` authorizes campaign and seat
   before lookup. A lost or cancelled HTTP response does not undo a commit; the client reconciles
   through receipt lookup and current view. Accepted receipts include a committed state version and
current audience revision; pending and rejected receipts omit those fields. Conflict errors use fixed,
audience-safe text.
7. `DELETE .../seat` invalidates generation and clears seat cookie. Clients must clear prior-seat
   DOM, drafts, overlays and subscriptions before entering the next seat.

Mutations use CSRF header and same-origin cookies. Local host must reject cross-origin mutation,
validate Host/Origin against the loopback profile and never log cookies, grants, player payloads or
hidden identities. Operator bootstrap is for local development only. Production identity and
remote play are outside this contract.

## Data rules

All revisions and cursors are unsigned decimal **strings**, including values beyond JavaScript's
safe integer range. `formationId`, `contactId`, and location IDs are only audience-safe,
projection-issued identities. Contact IDs must not reveal hidden source IDs or remain stable across
visibility loss unless the observation policy explicitly permits it. `LegalAction` display fields
must be derived from current permitted action set and observation; unsupported action kinds are
not represented as browser-submittable forms. Ledger rows show name, kind, position and order status from the same permitted view;
no strength, supply or readiness figure is inferred. They are not independent state. Diary text is bounded and sanitized from audience-approved facts.
Neither surface exposes Chronicle events, snapshots, private traces, RNG/seed state or hidden
opponent identifiers. SignalR hub `/hubs/v1/campaigns` emits `viewChanged` with `SafeChangeHint` only after a
permitted post-commit view change; reconnect reads `view` again. No hidden event count or raw journal position is pushed.

`tests/Cna.Game.Contracts.Tests` checks local references, protected operations, a representative
coherent view fixture, wide revision strings, forbidden properties and lack of client actor input.
The `typescript-angular` client is generated under
`src/Cna.Maproom/projects/maproom-alpha/src/app/api/generated`. It compiles but is not wired to
gameplay. Regenerate with `./src/Cna.Game.Contracts/generate-angular-client.sh` after schema edits.
Configure its base path to the browser origin and supply the CSRF token through client configuration
before use. MAP-04 awaits independent contract review; host mapping belongs to MAP-08/09.
