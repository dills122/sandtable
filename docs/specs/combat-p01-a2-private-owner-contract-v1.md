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
| `actorFacts` | Host constructs at original acceptance after grant redemption and session/CSRF verification, preflight clock sample and durable head read. Archives retains its exact canonical historical bytes with the accepted transition and the historical `seatGrant`. | Never accepted from owner request body. These are acceptance facts for cold replay, not a fresh retry session. A later session generation or grant epoch must not rewrite them. |
| `ownerOpening` | Core derives one immutable visible cut from persisted, fenced journal state and approved public refs. Host persists `owner-opening` metadata record, reads it back, then exposes exact visible bytes. | Only approved owner context, refs and revision, plus **proposed** offered candidate-ref list. `candidateRefs` is an outward-field decision in future exact A2 approval. No private candidate ID, raw source, hidden target, journal head/fence or deadline in visible bytes. |
| `ownerInput` | Owner supplies request ID, offered decision ref and revision, plus exactly selected alias **or** explicit decline. | No campaign, side, clock, grant, private candidate or Core action ID from owner. Host authenticates before alias lookup. |
| `privateOpeningPayload` | Core supplies one exact, capped private payload with the full canonical visible opening, ordered alias-to-private-candidate mapping, frozen combat base, permitted bookkeeping suffix, source-cut hash and trusted deadline. Archives persists its **actual canonical bytes** atomically with sequence 1. | Payload is never owner-visible. Sequence 1 `privatePayloadDigest` binds its domain-separated hash but cannot substitute for persisted bytes. On restart, missing, mutated or over-cap payload closes the offer. |
| `acceptedTransitionPayload` | Trusted local store samples acceptance UTC inside the conditional append transaction; Core then derives proposed A2 v1 typed input, selection-closed event and receipt against that same sample. Exact canonical bytes of all three are lowercase-hex encoded beside owner input, safe receipt, historical grant and preflight actor facts. Archives persists the capped payload bytes atomically with sequence 2, record and retry binding. | Sequence 2 `privatePayloadDigest` binds historical evidence and transition bytes; `requestDigest` binds owner input; `inputDigest`/`eventDigest`/`receiptDigest` bind decoded, typed Core bytes. A digest alone cannot reconstruct a transition after restart. |
| `journalRecord` and `retryBinding` | Private Chronicle/Archives contract; Core determines canonical transition bytes; durable store conditionally appends and binds request ID to exact input/receipt. | Never owner-visible. `seatEpoch` and append fence compare atomically with revocation across host processes. |
| `ownerReceipt` | Derived only after exact committed-byte readback and Core replay. | Outward fields are request ID, decision ref, revision and approved generic class. No event, journal sequence, CP/ammo, RNG or hidden reason. |
| `retryAuthFacts` | Trusted host constructs anew after authenticating the current same stable seat authority and checking current durable store revocation epoch. | Current clock, grant generation, epoch and session can advance after the original acceptance. These facts gate receipt lookup only; they do not rejudge historical acceptance against a closed opening or expired old grant. No owner-supplied values are trusted. |

All repeated campaign IDs must equal CreationRequest, checkpoint, grant, actor
facts, owner context, record, retry binding, journal key mapping and stored
head. Any mismatch closes before offer or append. Host verifies physical
retained import and A1 checkpoint rather than trusting hashes in these
objects. `seatAuthorityId` is stable across authorized renewal for *same*
owner; transfer creates a different authority ID. New session generation does
not change retry key. Grant/secret never enters ordinary logs, Maproom,
Signals or intelligence plane.

This R03 v1 profile pins `campaignId` to
`rules-lab.combat-creation.1`: all 300 retained CreationRequest and resulting
Control vectors in the 11 SHA-256-pinned chunks under the physical R03 manifest
(`sha256:39cae77c405b83a793b3d806fcf4a67860b9ff8514e59c9bfa83384d7dfc2b35`)
carry that exact ID. The prior synthetic `campaign-demo` value is not an
authenticated import and is rejected. The retained Control boundary also uses
`land.position.operation-1.first-player.movement-and-combat.combat.position-determination`
as its position ID in all 300 rows. The proposed owner-opening golden carries
those source-backed identifiers but remains **synthetic** for public refs,
opening, seat authority, journal, Core transition and receipt. A closed R03
Control boundary does not establish an authentic owner opening. Position IDs
use lowercase segments separated by a single dot or hyphen; the host must
still verify the exact Core-derived position at issue time.

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
Decoded Core bytes must parse as the closed `coreInputV1`, `coreEventV1` and
`coreReceiptV1` objects in the schema, with exact key order, version 1,
canonical byte equality and the per-object cap. The proposed v1 Core path
covers one ordinal-two selection result: `select-close-assault` or
`finish-without-attack`, a complete `p01-selection-closed` effect with
opening/source-cut binding, outcome/candidate, state/prefix transition and
Control/World/random hashes,
then a bound acceptance receipt. No generic kind-only JSON or opaque Core
event is accepted. Core must implement and emit this exact codec before A2
can issue anything; the synthetic golden is not evidence that current native
Core emits these bytes. Source replay must independently reconstruct and
compare the effect, Control, World, RNG and receipt, since hashes inside a
typed event alone are insufficient proof.

Decoded Core bytes use separate domains
`sandtable.combat.p01.a2.core-input.v1`,
`sandtable.combat.p01.a2.core-event.v1` and
`sandtable.combat.p01.a2.core-receipt.v1` with the same NUL separator.
`requestDigest` hashes the canonical owner input shape;
`retryBinding.receiptDigest` hashes the owner-safe receipt shape, while
`journalRecord.receiptDigest` hashes exact decoded Core receipt bytes.
The accepted record and payload each persist the same store-generated
`acceptedAtUnixMilliseconds` and fixed
`acceptanceClockSource=local-store-transaction-utc-v1`. Checkpoint/opening
records carry null for both. The private store API must never accept either
field from owner input or caller-supplied preflight facts: it samples its
trusted local UTC clock after taking the journal/seat fence inside the append
transaction, fails closed if unavailable, and compares the instant with the
grant issue/expiry and persisted opening deadline **before** Core derivation
and commit. Host preflight `clockAtUtc` is an earlier check, never the
authoritative append time. Cold replay validates the persisted source and
time against the **historical** grant and actor facts retained inside the
accepted payload, equality across record/payload/Core input/event, and the
original strict windows. Later retry separately authenticates the *current*
same seat authority and current store revocation epoch, then reads the
retained receipt. Its current clock/epoch never enters the historical
acceptance test and cannot append another event. This local trust boundary must be
implemented and tested separately; an asserted source string by itself does
not authenticate a clock.

The approved visible cycle permits operation stages 1–3 and only
`first-acting-side` or `second-acting-side` phase slots. Boolean `true` is
never an integer `contractVersion:1`.

Proposed per-object canonical byte caps: import binding and seat grant
1,024 each; actor facts 1,536; owner opening 4,096; private opening payload
8,192; owner input 1,024; accepted transition payload 65,536;
journal record 2,048; owner receipt and retry binding 1,024 each. Each
decoded Core input, event or receipt is capped at 8,192 bytes (16,384
lowercase hex characters). At most
eight unique candidate refs in one opening. Later A2 implementation must
measure and freeze separate retained-import, private payload, journal count,
total-byte and replay-time caps against original R03 evidence before issuance.
This proposal does **not** guess that observed source maximum is safe for
production. Over-cap import or replay closes rather than truncates history.

Two alternative synthetic goldens each contain ordered sequence 0 checkpoint,
sequence 1 metadata opening and sequence 2 accepted owner input. They share
the opening, then diverge into a selected assault or explicit decline.
Frozen domain-separated record digests:

| Sequence | Kind | Canonical digest |
| --- | --- | --- |
| 0 | `r03-import-checkpoint` | `sha256:119b5192966b5ede1b6a2e56f6af58052b847a53ae40ecf221bd3d15035f71be` |
| 1 | `owner-opening` | `sha256:3e5b5e45a09e4f38ace9061e544a2a5e3cb7578aa178d0b4d62d34942303b380` |
| 2 selected | `owner-accepted-input` | `sha256:0c00028064988dbd78cc21b4b3099418015f316314f1b7bf7fa3df598148ee45` |
| 2 decline | `owner-accepted-input` | `sha256:97c61fa8254cebc21ffa30166b7e8259d17f464662b471859c014b6e0277b287` |

These are **codec goldens only**; fabricated `sha256:` values inside sample
are placeholders, not actual CreationRequest, source, event or receipt pins.
Before A2 implementation, independent verifier must replace illustrative
chain with physically authenticated R03 root and real Core-derived bytes,
without changing v1 field definitions.

## Journal sequence, decision and retry rules

Sequence 0 binds A1 checkpoint and *already represented* R03 System
input/event/receipt. It does not append game event twice or consume twelfth
P01 input. It has no seat authority. Sequence 1 is conditional
`owner-opening` authority-metadata append: the exact 1,656-byte synthetic
`privateOpeningPayload` golden is retained as **canonical bytes**, not merely
as a digest. Its frozen digest is
`sha256:2e4509de2633fd68d6e5d363f6fa26575b2ed433ed0894afc91ede8e02b9694d`.
The payload contains full visible opening, frozen combat base, alias mapping,
source/cut binding and trusted deadline. The record digest binds those bytes;
recovery must read and rehash them before returning the same offer. Sequence
1 changes journal sequence/hash and
visible revision to one; it does **not** change game state version/prefix or
emit game input/event/receipt, debit or RNG draw. Sequence 2 is the **only**
accepted owner input in v1. Selected and decline synthetic
`acceptedTransitionPayload` byte counts/digests are respectively 6,382 /
`sha256:d691899a1cc9e40663134c647e06236e88cc25042bd8e3e691cf818122d63ef5`
and 6,050 /
`sha256:bce610b150e1951ec0b08b0286f3fab00038bd81a3b3a152c58924a32422ee70`.
Exact capped payload bytes, including owner input/receipt objects and decoded
Core input/event/receipt bytes, must be durably published in the same
transaction as record and retry key. Sequence 2 may change game state
version/prefix only after Core derivation and conditional append. V1 is
terminal at sequence 2. A repeated `owner-accepted-input`, second opening
or sequence 3 record is unsupported; an exact duplicate request returns the
retained sequence 2 receipt without append. Later gameplay and closure
kinds require version 2 or separately reviewed closed extension.

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
expected sequence, record hash, state version/prefix, storage fence, the
store-sampled acceptance instant against grant and opening windows in
**one durable transaction**. It atomically stores canonical record, accepted
transition payload bytes, receipt and retry binding. Revocation increments same durable seat epoch
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
| Commit sequence 2, lose response | Reauthenticate current same stable seat authority against current grant/epoch, look up stable retry key, verify historical accepted bytes using retained historical grant/preflight and committed-byte replay; return same receipt without new debit/RNG even after original deadline or grant expiry. |
| Ambiguous append/readback failure | Return generic unavailable; do not report acceptance or recompute paid transition. |
| Concurrent revocation or competing host | One store seat-epoch/head/fence order decides; stale process cannot append. |

Readback after actual host restart must revalidate retained original
CreationRequest, all eleven ledgers, pinned dependencies, A1 checkpoint,
historical grant/preflight and revocation order, exact persisted private opening payload bytes,
exact accepted-transition payload bytes and every journal record in
sequence. Rehash and decode Core input/event/receipt bytes, then compare
Core-derived source, Control, World, RNG, prefix, event, receipt and
conservation properties. Hash-only snapshot or current temporary grain is
insufficient. Concrete storage provider, encryption-at-rest/key owner,
retention and restore-failure policy require separate review and approval.
Parallel disabled SQLite host foundation is only possible substrate; it
has no P01 opening or journal authority.

An owner input can be accepted only when a trusted store transaction clock is
available, grant issue time is at or before the persisted acceptance instant,
and that instant precedes both grant expiry and persisted private opening
deadline. A false
`clockAvailable` flag or deadline equality/past time closes this owner
submission. Any System cancellation is a separate typed path, never forged
owner decline. Synthetic negatives re-freeze all digests and still reject
post-deadline or expired-grant committed times. They also reject duplicate
terminal append, sequence 3, kind-only/unknown-version Core bytes, missing
Core event and over-cap Core receipt. Both complete goldens replay under a
current authenticated retry clock after the original opening deadline with
current seat epoch 1, while historical acceptance remains epoch 0. Current
unauthenticated, wrong-seat, stale-store-epoch or expired-grant retry is denied
without changing the historical acceptance verdict.

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
