# P01 A2 private owner proof contract v1 — proposed

**Status:** exact byte and lifecycle proposal for a separately approved A2
decision. Choice L approved a local proof *direction*; it did not approve this
schema, a credential, a route, a durable P01 journal, an owner opening, or a
P01 action. This document is inert. The original 300 authenticated R03 roots
remain 44 provisional positive, 256 candidate absent, and zero admitted or
public. Full AC-003/006/007/008 certification remains open.

This packet narrows the [A2 owner path design](../design/p01-private-owner-path-proposal-v1.md)
and the [A1/A2 authority boundary](../design/p01-private-authority-journal-proposal.md).
It reuses the **approved** [owner-visible context, refs, revision and generic
rejection rules](combat-p01-owner-decision-proposal-v1.md). The
[closed shape definitions](combat-p01-a2-private-owner-contract-v1.schema.json),
[synthetic golden vectors](fixtures/combat-p01-a2-private-owner-contract-v1.json)
and [standard-library verifier](verify-combat-p01-a2-private-owner-contract-v1.py)
are the reviewable proposal. Goldens contain invented identities and digests,
never retained R03 evidence or a credential. They cannot prove an authentic
seat, legal action, privacy twin, append or host restart.

## Authority, identity and field origins

| Object | Creator and storage | Trust boundary |
| --- | --- | --- |
| `importBinding` | Core validates original CreationRequest, eleven ledgers and pinned dependencies; trusted import coordinator persists binding with A1 checkpoint. | `campaignId` is authenticated CreationRequest ID. `journalKey` is a stored one-to-one mapping for that ID, never a request parameter or supplier hash. Import cannot be selected by a client row label. |
| `seatGrant` | Trusted local creation coordinator commits it before releasing a single-use secret; trusted store retains only secret digest. | Campaign, side, stable `seatAuthorityId`, issuer, generation, expiry and revocation epoch come from committed record. Possession of valid local grant is proposed private seat authority; human identity beyond it is not asserted. |
| `actorFacts` | Host constructs after grant redemption and session/CSRF verification, fresh trusted clock sample and durable head read. | Never accepted from owner request body. Session generation can change after restart; stable seat authority ID cannot be inferred from browser-selected side. |
| `ownerOpening` | Core derives one immutable visible cut from persisted, fenced journal state and approved public refs. Host persists `owner-opening` metadata record, reads it back, then exposes exact visible bytes. | Only approved owner context, refs and revision, plus **proposed** offered candidate-ref list. `candidateRefs` is an outward-field decision in future exact A2 approval. No private candidate ID, raw source, hidden target, journal head/fence or deadline in visible bytes. |
| `ownerInput` | Owner supplies request ID, offered decision ref and revision, plus exactly selected alias **or** explicit decline. | No campaign, side, clock, grant, private candidate or Core action ID from owner. Host authenticates before alias lookup. |
| `journalRecord` and `retryBinding` | Private Chronicle/Archives contract; Core determines canonical transition bytes; durable store conditionally appends and binds request ID to exact input/receipt. | Never owner-visible. `seatEpoch` and append fence compare atomically with revocation across host processes. |
| `ownerReceipt` | Derived only after exact committed-byte readback and Core replay. | Outward fields are request ID, decision ref, revision and approved generic class. No event, journal sequence, CP/ammo, RNG or hidden reason. |

All repeated campaign IDs must equal CreationRequest, checkpoint, grant, actor
facts, owner context, record, retry binding, journal key mapping and stored
head. Any mismatch closes before offer or append. Host verifies physical
retained import and A1 checkpoint rather than trusting hashes in these
objects. `seatAuthorityId` is stable across authorized renewal for *same*
owner; transfer creates a different authority ID. New session generation does
not change retry key. Grant/secret never enters ordinary logs, Maproom,
Signals or intelligence plane.

## Canonical v1 bytes and bounds

Each closed shape has `contractVersion:1` and exactly ordered keys in
`x-field-order` in schema. Canonical bytes: ASCII JSON, no whitespace or
trailing newline, no duplicate/unknown keys, floats, Unicode escapes or
non-ASCII strings; shortest decimal nonnegative integers; JSON
`true`/`false`/`null`; exact Core-supplied array order. IDs and hashes use
lowercase lexical forms in schema. Parse with duplicate-key detection and
require byte equality after canonical re-encoding. Unknown version/kind,
extra/missing/reordered key, malformed ID or over-cap bytes fail before
mutation. Version 1 bytes remain immutable; extensions require version 2 and
explicit replay reader. Hashes are `sha256:` plus lowercase SHA-256 of ASCII
`sandtable.combat.p01.a2.<shape>.v1`, one NUL byte, then exact canonical
shape bytes. Existing owner `pub.`/`p01-owner-v1.` domains remain unchanged.

Proposed per-object canonical byte caps: import binding and seat grant
1,024 each; actor facts 1,536; owner opening 4,096; owner input 1,024;
journal record 2,048; owner receipt and retry binding 1,024 each. At most
eight unique candidate refs in one opening. Later A2 implementation must
measure and freeze separate retained-import, private payload, journal count,
total-byte and replay-time caps against original R03 evidence before issuance.
This proposal does **not** guess that observed source maximum is safe for
production. Over-cap import or replay closes rather than truncates history.

Synthetic accepted golden contains ordered sequence 0 checkpoint, sequence 1
metadata opening and sequence 2 accepted owner input. Frozen domain-separated
record digests:

| Sequence | Kind | Canonical digest |
| --- | --- | --- |
| 0 | `r03-import-checkpoint` | `sha256:c8b98f1d4b72dd732dfdf30363381e20f8371e2efe1014d826b564fbda9f074b` |
| 1 | `owner-opening` | `sha256:61452a7f704e4d497a4e836d946a748516e443386b2b77fd6069acfd87522e66` |
| 2 | `owner-accepted-input` | `sha256:f9929cd62e0f227860f9a6f9cb95976ee3e28e7caa0d8eb577081db88a77f173` |

These are **codec goldens only**; fabricated `sha256:` values inside sample
are placeholders, not actual CreationRequest, source, event or receipt pins.
Before A2 implementation, independent verifier must replace illustrative
chain with physically authenticated R03 root and real Core-derived bytes,
without changing v1 field definitions.

## Journal sequence, decision and retry rules

Sequence 0 binds A1 checkpoint and *already represented* R03 System
input/event/receipt. It does not append game event twice or consume twelfth
P01 input. It has no seat authority. Sequence 1 is conditional
`owner-opening` authority-metadata append: exact visible opening bytes,
frozen combat base, alias mapping, source/cut binding and trusted deadline
are retained in private payload. It changes journal sequence/hash and
visible revision to one; it does **not** change game state version/prefix or
emit game input/event/receipt, debit or RNG draw. Sequence 2 is first
separately approved accepted owner input. It may change game state
version/prefix only after Core derivation and conditional append. Later
opening/closure kinds require a new version or separately reviewed closed
extension; unknown kind is not accepted.

`ownerInput.choiceKind=select` requires nonnull alias in exact persisted
offer; `decline` requires `candidateRef:null`. Silence, timeout or unavailable
clock never becomes owner decline. Private retry key is
`(campaignId, seatAuthorityId, requestId)` under authenticated current seat.
Same canonical input bytes return *same retained* safe receipt. Same key
with changed input conflicts, even if new alias looks legal. Different seat
authority cannot look up receipt. Opening revision, frozen combat base and
permitted bookkeeping suffix must still match before new submission.
Hidden-dependent rejection uses approved `not-current-or-not-admissible`;
`stale-view` requires actual visible revision change. Transport status,
latency and log equivalence still require AC-007 review.

Conditional append compares campaign ID, stable seat authority, seat epoch,
expected sequence, record hash, state version/prefix and storage fence in
**one durable transaction**. It atomically stores canonical record, accepted
receipt and retry binding. Revocation increments same durable seat epoch
under that transaction boundary. Append first may commit and be read back,
then revocation wins; revocation first makes append fail. Orleans turn lock
or process-local semaphore does not prove this order.

| Boundary | Required observable result |
| --- | --- |
| Crash before import/checkpoint commit | No journal head or owner offer. Revalidate original evidence on retry. |
| Crash after sequence 0 commit, before acknowledgment | Exact checkpoint record readback; no second R03 event. |
| Crash before sequence 1 append | No visible opening; derive only from reconstructed Core state. |
| Crash after sequence 1 commit, before read response | Reauthenticate, read back and return byte-identical persisted opening and alias. |
| Crash before sequence 2 append | No accepted result/debit; retry may conditionally derive once from same verified opening. |
| Commit sequence 2, lose response | Reauthenticate, look up stable retry key, verify exact input and committed-byte replay; return same receipt without new debit/RNG. |
| Ambiguous append/readback failure | Return generic unavailable; do not report acceptance or recompute paid transition. |
| Concurrent revocation or competing host | One store seat-epoch/head/fence order decides; stale process cannot append. |

Readback after actual host restart must revalidate retained original
CreationRequest, all eleven ledgers, pinned dependencies, A1 checkpoint,
grant/revocation history and every journal record in sequence. Compare
Core-derived source, Control, World, RNG, prefix, event, receipt and
conservation properties. Hash-only snapshot or current temporary grain is
insufficient. Concrete storage provider, encryption-at-rest/key owner,
retention and restore-failure policy require separate review and approval.
Parallel disabled SQLite host foundation is only possible substrate; it
has no P01 opening or journal authority.

## Rejection and approval gate

Reject malformed/noncanonical/over-cap input as `invalid-request`; reject
real owner-visible revision change as `stale-view`; collapse hidden-dependent
nonmembership, changed base, revoked/expired grant and profile failure to
`not-current-or-not-admissible` after appropriate authentication.
Unauthenticated requests fail before alias lookup. Store/clock unavailable
returns generic unavailable, never invented owner action. Internal
diagnostics must not leak hidden blocker detail to owner. Verifier checks
duplicate keys, field order, version, caps, chain and identity mismatches;
it is not authentic owner/hidden-twin or two-process race proof.

**Separate exact A2 owner approval remains required before implementing
this contract.** Approval package must settle local grant issuer/authenticator
and proof client transport, public ref/clock origin, concrete transactional
store/fence, retained import caps, encryption/retention, rejection timing,
authentic equal-visible hidden-twin evidence path, and limited
pre-certificate amendment to [P01 acceptance freeze](combat-actual-moved-repeat-position-v1.md#freeze-decision).
Choice L authorizes preparing this reviewable package, not opening a route,
issuing a seat, submitting action, admitting P01 or exposing it publicly.
