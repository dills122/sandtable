# MAP-05 trusted host protocol v1

Status: design contract for MAP-06/07 and MAP-14/15; no host adapter, campaign grain, durable
provider or browser handler is implemented by this document. Source baseline: reviewed MAP-04
`fc2bc21` and the existing Rules9 Core APIs. This protocol covers an admitted public rules-lab
campaign only. Private Combat references are neither imported nor activated.

## Authority and byte ownership

| Owner | Material | May return to browser? |
| --- | --- | --- |
| Umpire (`Cna.Core`) | Opaque executable session; canonical creation, action, event, receipt and snapshot bytes; exact state version, position, RNG and immutable source identity | No. Only side-safe observation and legal candidates cross to host projection. |
| Chronicle/Archives host modules | Versioned commit batches, expected head/fence, trusted principal and side, request dedupe, verified checkpoint, recovery status and outbound delivery marker | No. Storage is trusted input for recovery, not authority to invent a successor. |
| `Cna.Game.Contracts` and Maproom | Granted seat/session generation, opaque outward action identity and set token, audience revision/cursor, permitted view, pending/final receipt and safe change hint | Yes, to the authorized seat only. No authority state version, private action ID, source seed, raw event, raw receipt or snapshot. |

Creation pins campaign ID, canonical creation request contract, ruleset hash, setup/content IDs and
hashes, scenario ID, seed and RNG policy actually supplied by Core. Configuration identity is
required when the selected admitted fixture has one; current Rules9 creation request has no
separate Combat configuration field, so the host must not invent one. The four hashes returned by
MAP-04 creation are outward provenance references, not a license to expose seed or private setup
internals. Creation evidence is the exact `CampaignExercises.Begin` event and initial snapshot;
`ExerciseSession` remains opaque. Never serialize an `ExerciseSession` or treat
`ReadCheckpoint` metadata as executable restore.

The private version-1 record shapes are conceptual field contracts, not a browser wire format:

| Record | Required fields | Invariant |
| --- | --- | --- |
| Creation root and request index | `schemaVersion`, campaign ID, authenticated operator ID, creation request ID, canonical outward `CreateCampaignRequest` bytes and SHA-256, exact original `CampaignCreated` response including both distinct grant IDs, canonical private creation bytes, immutable source pins, canonical creation event bytes, initial snapshot bytes | The operator/request ID index and root publish atomically. One root per campaign; its event and snapshot match direct Core. Grants are protected private operator credentials at rest. |
| Accepted step | `schemaVersion`, campaign ID, prior/resulting journal sequence and head digest, prior/resulting authority version and position, authenticated operator/seat and session generation or host System identity, request ID, canonical outward request envelope and SHA-256, canonical Core command bytes and SHA-256, ordered canonical event bytes, canonical Core receipt bytes, outward final receipt, optional successor checkpoint bytes and SHA-256, source pins | One conditional publication unit. Retry identity is the original outward request; Core command is separate replay evidence. Core outputs match direct Core byte for byte; source pins equal creation root. |
| Pending attempt | Activation fence, expected committed sequence/head, authenticated outward request binding, accepted-step digest, opaque successor | Memory only. Never a committed head or save. |
| Checkpoint binding | `schemaVersion`, campaign/source identity, exact journal head and authority version, canonical snapshot digest and bytes, codec version | Cache may lag but never supersede the committed event stream. |

The head digest is over a versioned, unambiguous length-framed sequence of prior head, sequence,
source identity and canonical event bytes; MAP-14 freezes exact encoding before a provider is
chosen. An implementation must not hash ambient JSON formatting, an unframed concatenation or a
checkpoint in place of events. Authority versions remain private `Int64` values; journal sequence
is a different monotonic value. Neither is MAP-04's audience revision or diary cursor.
The canonical outward action envelope is a private length-framed encoding of campaign path plus
the exact validated `SubmitActionRequest` fields. It is not a browser DTO or Core submission.
MAP-14 freezes its byte encoding before durable use.

## Trusted adapter result

MAP-06 introduces a narrow Core-owned adapter, with versioned private records. For creation it
returns the opaque session, canonical creation event and initial snapshot. For a current query it
returns a **single committed cut** containing the requested side's observation and legal action
set; the host derives browser fields from that cut. For a command it accepts only a server-resolved
`CampaignActionSubmission` with exact current campaign ID, authority version, position, actor and
Core action ID. On acceptance it returns an opaque successor plus `ExerciseStepEvidence`:
canonical accepted event record(s), canonical receipt and successor snapshot checkpoint. Current
evidence has exactly one event; the private record reserves an ordered, nonempty event list rather
than assuming all future steps have one. On rejection it returns a typed reason and **no**
successor/evidence. Rejection may not advance RNG, event history or state.

Before submitting, the host authorizes campaign/seat, checks request-ID dedupe, resolves the
outward action ID to a current Core candidate, and rechecks the current legal set. The outward
token/action ID must stay stable across hidden-only commits when the permitted action remains
equivalent. Internal Core IDs and `stateVersion` can change; that mapping stays private. A
previously used request ID is looked up only after authorization and **before** resolving a Core
candidate or testing new-domain staleness. Compare authenticated operator/seat and original session
generation plus canonical outward request bytes (including campaign path, action-set token,
position, outward action ID and contract version). An exact match returns latest retained status,
even if the current Core candidate or outward mapping has changed. Changed seat/session/payload
conflicts; an invalidated seat generation cannot retrieve a prior-seat receipt. The separate
canonical Core command bytes remain private replay evidence, never the dedupe key.
A new stale request yields an audience-safe rejection without a commit. The host must never trust a
client-supplied System actor.
Fresh rejected requests may be retained as immutable private dedupe outcomes with no event; their
retention/expiry policy is a MAP-14 decision. A rejection cannot be represented as an accepted
step or advance a journal head.

## Pending publication and fencing

The grain owns one campaign and serializes committed authority access. A candidate transition is
prepared only against `(campaign ID, committed journal head, authority version, activation fence)`.
The grain stores an immutable private pending record containing authenticated outward request
identity, seat/session binding, canonical Core command digest, prior head, expected source pins, candidate evidence digest
and successor handle **in memory only**. It admits no competing mutation while that record is
pending. Reads expose the previous committed audience cut, or an explicit unavailable response;
pending never exposes successor or claims save/acceptance. Identical retries report pending;
changed reuse conflicts.

An off-turn coordinator publishes one atomic conditional batch: expected head/fence, authenticated
outward request identity and separate Core command, exact canonical event(s), Core and outward
receipts, resulting head/version,
optional hash-bound checkpoint, and any required outbound-delivery marker. Provider I/O does not
hold an authoritative grain turn or run inside Core. A completion message is accepted only when
the active fence, pending digest and returned committed head match the prepared transition. Then
the grain installs successor, emits audience-safe notifications for changed permitted views, and
returns an accepted outward receipt. A stale activation/completion cannot install state. A failed
conditional write requires reread; it is not retry permission to append blindly.

The provider must issue or conditionally advance a persistent fencing generation before a new
activation publishes. Every write checks both that generation and the exact prior head in the
same atomic operation; a volatile activation GUID alone is insufficient. Re-activation first
recovers the committed head and outstanding request before admitting mutations. Campaign creation
uses the same conditional publication rule: no seat grant or created acknowledgment until the
creation root **and operator/request-ID index** commit, and an exact creation retry reconciles the
committed root. The index is scoped to the authenticated operator, with request ID globally unique
for that operator. Reuse with a different canonical outward create payload conflicts even when it
names another fixture. The same request by a different operator cannot reveal the original
campaign or grants; campaign-ID collisions conflict. Retain the index and original response for
the campaign lifetime (including across durable process restarts); no silent expiration may allow
the same request ID to create a second campaign. If a `201` reply is lost, the same authorized
operator retries the exact request and receives the **same** campaign ID and original two grants,
without rerunning Core or minting grants. Grants remain short-lived and single-use. A retry after
grant expiry may return the original response but does not reactivate its grants; a separate
authenticated renewal flow would need its own MAP-08 contract before such recovery is offered.

An unknown commit result freezes mutation and outbound acceptance. The coordinator reads the
authoritative head and request record under the current fence. If the batch is present and exact,
it validates and installs that committed successor once; if the old head remains, it may reattempt
the same conditional batch; if the head is different or truth cannot be established, the campaign
stays unavailable and activation is discarded. No old in-memory successor becomes truth. A
lost/cancelled HTTP response is reconciled through the retained receipt and current view. An
outbound marker committed with the batch is delivered later and idempotently; only after commit
may it trigger a browser hint.

A pending attempt has a bounded coordinator lease and a grain-owned reconciliation trigger. Lease
time is only a recovery scheduling input, never a game/RNG input. If coordinator execution stops,
its callback is lost, or the pending deadline passes, the grain dispatches a fenced head/request
read under the current activation generation. It validates the committed batch exactly and
installs once, or re-dispatches the identical conditional batch only after proving the old head
still holds. The trigger retries with bounded backoff/attempts; exhausted retries or unreadable
storage transition the campaign to explicit `unavailable`, release no accepted receipt and require
operator-visible retry/re-activation. It never clears a possibly committed pending attempt merely
because a timer expired. A late completion from an earlier attempt is checked against fence,
pending digest and head and ignored if stale.

Temporary MAP-07 memory mode uses the same pending/final semantics and a bounded in-memory commit
record, but loses campaigns and receipts on **process** restart. It must identify that lifetime in
API/operator UI. The publication path above is the contract for MAP-14/15 durable evaluation;
Orleans activation and its default storage are not proof of provider atomicity or process recovery.

## Restore provenance

MAP-06 must supply a trusted import/replay entry point; existing `CampaignExercises.Reconstruct`
requires a live session, while `ReadCheckpoint` only reads snapshot metadata. Recovery verifies
the versioned envelope and campaign key; immutable creation/source pins; contiguous journal
sequence and expected prior/resulting heads; canonical event/receipt bytes; unique creation and
action request indexes with authenticated outward payload/seat binding; separate canonical Core
command and actor binding; checkpoint hash, version, source and journal-head binding; then replays the verified
suffix using the pinned Core version. Re-adjudication must reproduce exact event, receipt and
snapshot bytes. A checkpoint is an acceleration cache, never a replacement for the authoritative
event/receipt sequence. Unknown historical codec or source version, corrupt/missing history,
divergent replay or a checkpoint ahead of the head fails closed. A lagging checkpoint may be
ignored and reconstructed from an earlier verified point within a declared work bound. No
partial view or accepted receipt is exposed during uncertain restore. Upgrade policy and retention
of dedupe/tombstones belong to MAP-14; deletion cannot make a retained retry execute again.

## Bounded System progression

After each **committed** transition, the host may query `CampaignActionAudience.System` from the
committed session. Advance one current legal System candidate through the same pending-publication
path, using a host-owned deterministic request identity bound to prior head and candidate. Repeat
at most a configured step budget per trigger; no wall-clock or browser timing decides outcomes.
Stop when a human legal decision is required, Core reports an admitted terminal state, current
phase is unsupported, or the budget is exhausted. A zero-length legal set alone is not victory.
System work never originates from a browser actor, and a chain cannot silently skip publication
or coalesce authority steps. The host reports an explicit `needsHuman`, `terminal`,
`unsupportedPhase`, `stepLimit`, `pending` or `unavailable` reason. MAP-07 must test each stop.

## Fixed direct-Core vectors for MAP-06/07

The reproducible baseline is `HOST-RSH-001` at
[publication research](../research/orleans-publication-feasibility.md). Its pinned creation input
is `CampaignCreationRequest(1, "host-rsh-001", Rules9 hash, 12345,
"rules-lab.breakdown.truck.v1", sha256:e6631e81ad8f97e39fd9d7eec93bad7fe2b39db4d2d3059ed94a02dd4093e7a3,
"rules-lab.content.breakdown-truck.v1", sha256:646e76e69ecceb82216b37d84e950928099acd8a3cb04b51526d0fe631e512ee,
"breakdown-truck-lab")`; ruleset hash is
`17f3e6047f34b5bf6f5f809055863b664a4bd82a8481db83ee83e0ae5cad3a2a`.
Choose the first nonempty audience set in Core enum order; prefer `act-first`, then
`complete-reserve-designation`, then `complete-movement-segment`, else first candidate. Submit
each exact Core candidate with its current private state version and position. The fixed twelve
kind sequence is:

```text
resolve-initiative
resolve-no-obligation-naval-convoy-schedule
resolve-no-obligation-tactical-shipping
act-first
resolve-weather
resolve-no-obligation-organization
resolve-no-obligation-naval-convoy-arrival
resolve-no-obligation-fleet-assignment
resolve-no-obligation-fleet-repair
complete-reserve-designation
complete-movement-segment
complete-breakdown-segment
```

The baseline ends at
`land.position.operation-1.first-player.movement-and-combat.combat.position-determination`,
state version `13`, with final canonical snapshot SHA-256
`222b36760b250ab6ea8eb14a7913ffb56bab1b1786cb7a9d9b1f720360ca3c84`.
MAP-06/07 evidence must compare **every** creation/event/receipt/snapshot byte and all three
audiences' legal-set bytes at every cut against fresh direct Core, not merely the final hash. The
old same-process Orleans probe records 12 commands and 13 writes, but its whole-record hash and
record size belong to that probe format and are not new host golden values. Add negative vectors:
same request/actor/canonical command; changed actor or command under same ID; fresh stale command;
wrong campaign/seat; tampered event, receipt, source pin and checkpoint. Rejection must leave head,
RNG and all three audience cuts unchanged. First-side Combat entry is the supported stop, not a
successful Combat action.

## Failure matrix and acceptance gates

| Fault point | Required observable result | Recovery evidence |
| --- | --- | --- |
| Before prepare or before conditional commit | Prior committed cut and head remain; no accepted receipt. | Retry can prepare once after reread. |
| Commit rejects stale head/fence | No candidate installation; campaign unavailable until reload. | Fresh activation reconstructs current head; old completion ignored. |
| Creation root commits, `201` reply is lost | No second campaign or grant mint. | Same authorized operator/request ID/outward bytes receives original campaign ID and grants; changed payload conflicts. |
| Accepted action commits, reply is lost after hidden Core state or outward mapping changes | No re-resolution of old outward action for retry. | Authorized original seat/session and exact outward bytes retrieve retained final receipt; changed reuse conflicts; Core command remains separate replay evidence. |
| Commit succeeds, acknowledgment is lost or provider returns unknown | Pending/unavailable, never speculative acceptance. | Reread exact batch and request record; one accepted receipt and no second event. |
| Coordinator dies or completion callback is lost while grain stays active | Pending does not persist forever or become speculative acceptance. | Bounded fenced reconciliation finds exact commit or proves old head before one identical conditional retry; exhaustion becomes `unavailable`. |
| Process dies with only pending in memory | No pending successor survives. | New activation loads old committed head; retry either executes once or reconciles committed record. |
| Process dies after atomic commit, before grain completion/reply | No double effect. | New activation restores batch and dedupe receipt; exact retry returns final outcome. |
| Read fails during unknown-commit reconciliation | No mutation, view advance or accepted claim. | Remain unavailable; later successful read proves head before resuming. |
| Checkpoint lags committed journal | Committed truth still available after bounded replay. | Suffix replay matches exact bytes. |
| Checkpoint corrupt/ahead, event missing/tampered or source pin mismatched | Fail closed; no partial campaign view. | Diagnostic private error; no player-facing hidden details. |
| Outbound notification fails after commit | Authority and receipt remain committed. | Marker retries idempotently; HTTP readback catches missed hint. |
| Step budget exhausted or unsupported phase reached | Explicit stop; no implicit victory or speculative next action. | Last committed cut/retry stable. |

MAP-06/07 acceptance is direct-Core parity and temporary-host behavior. Durable claims require
MAP-14 schema/retention decisions and MAP-15 real-provider kill/restart, stale-fence,
unknown-commit and corruption tests. Production hosting additionally follows `HOST-PUB-001` and
public Core activation gates. This spec does not choose PostgreSQL or a clustering provider.
