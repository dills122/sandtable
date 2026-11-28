# Seat-filtered campaign observation (outward HTTP v2)

`POST /api/v2/campaigns/{campaignId}/observation` is a side-effect-free read on the same
Development-only, loopback, same-origin route group as opening discovery. Body:

```json
{"contractVersion":2,"credential":{"campaignId":"example","audience":"axis","epoch":1,"secret":"<seat secret>","expiresUnixMs":0}}
```

Credentials have the same campaign, seat, expiry and epoch checks as existing v2 reads.
The reply is `application/json` with `Cache-Control: no-store`; invalid credentials
return 401, rejected origin or remote address returns 403, malformed requests return
400, and an unavailable or invalid journal returns the existing v2 error envelope.

## Response

| Field | Meaning |
| --- | --- |
| `contractVersion`, `campaignId`, `audience` | Version 2, requested campaign, authenticated seat (axis or commonwealth). |
| `observationRevision` | Opaque deterministic SHA-256 of the published cut, including campaign and audience; equality only, not a global authority counter or action capability. |
| `position` | Committed `gameTurn`, `operationStage`, `positionId`, `activeSide` (nullable). Turn and stage follow the retained sequence and rollover history, not setup's initial turn. Null when no gameplay position is admitted. |
| `deadline` | Unix milliseconds for an already collecting owner round with an exposed choice at this cut, otherwise null. No round is opened or advanced by this read. |
| `worldAvailable` | False if no canonical owner world cut is available. Render only the state panel in that case. |
| `visibleLocations` | Stable `locationId`, `label` (ID fallback). |
| `visibleEdges` | `firstLocationId`, `secondLocationId` from the published topology. |
| `ownElements` | `elementId`, `parentFormationId`, `currentLocationId`, `displayLabel` (ID fallback), for independently placed own elements only. |
| `apparentContacts` | Formation-accessible contact views: opaque `handle`, `observerFormationId`, `apparentLocationId`, `identification`, `access`, `isCurrent`. Lost contacts remain explicitly non-current; never substitute authoritative opposing identities or placement. |

The response reads a single verified committed head. It does not preview system steps
(the discovery route may preview them), create openings, persist events, update timing
high-water marks, arm timers, or insert a new journal cache entry. Read-only Marten
working sets use repeatable-read transaction isolation and are discarded without flushing.
Source setup and source-turn worlds are not represented by the admitted synthetic owner
projection: their response is a state panel without map rows.

The canonical owner location/edge projections do not expose display coordinates or
labels. No coordinates are guessed. Drawing a map requires an admitted, versioned
topology-presentation mapping keyed by published location IDs (including a declared
coordinate system); a UI should not infer positions from names or hidden World rows.

Discover actionable elements via `POST /api/v2/campaigns/{campaignId}/openings/open`;
link `actingElementIds` to `ownElements.elementId`. Allowed-destination highlights
must come from the visible choices of the current owner opening and its public
references, never from topology alone. An observation is not an action submission
fence; existing opening revision and public-reference checks remain authoritative.
