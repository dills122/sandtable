# P01 A2 private owner path: decision packet v1

**Status:** proposed A2/P03b design for a separate owner decision. A1/P03a was
approved and its [boundary proposal](p01-private-authority-journal-proposal.md)
merged in #295. Nothing in this packet approves A2, changes the P01 acceptance
freeze, issues an owner opening, adds a host route or store, or admits Combat.
The original 300 R03 roots remain 44 provisional candidates, 256 candidate
absences and zero admitted actions. Public Maproom and native P01 stay closed.

## Existing seams and missing authority

- `CampaignCombatActualMovedRepeatBreakdownCompletion` replays trusted R03
  evidence in Core, but receives its creation request, eleven input ledgers and
  dependency bytes from its caller. A1 may validate and bind those bytes in an
  immutable `admitted=false` checkpoint. It cannot itself open a decision.
- `CampaignAuthorityHandle` currently wraps the admitted Rules9/Content6 V11
  snapshot. Its creation path does not own the private Rules10/Content7 R03
  journal. An A2 proof must neither graft a P01 handle onto that campaign nor
  convert a supplied Control hash into authority.
- `LocalSessionStore` provides process-local operator and seat grants, a
  two-hour seat session, and serialized admission/relinquish. The temporary
  grain holds requests and accepted steps in process memory for the truck
  fixture. `TemporaryOutwardActionIngress` is host-private and no public
  submit/receipt route calls it. These are useful patterns, not durable P01
  seat provenance or a P01 store. No executable Chronicle/Archives persistence
  module is present for this path. [Current host limits](../../README.md#project-at-a-glance),
  [host seam](../../src/Cna.OrleansHost/Api/TemporaryOutwardActionIngress.cs),
  [session store](../../src/Cna.OrleansHost/Api/LocalSessionStore.cs), and
  [temporary grain](../../src/Cna.OrleansHost/TemporaryCampaignGrain.cs)
  remain unchanged by this packet.

The immediate blocker is authentic owner input. Test-injected actor, clock or
decline bytes cannot prove AC-003, AC-006 or AC-007. The approved private
[owner-visible tuple and rejection contract](../specs/combat-p01-owner-decision-proposal-v1.md)
fixes field and codec rules, but supplies no source for an owner observation,
seat, current opening, public references or receipt.

## Proposed private observation and submission path

The narrow A2 proof target is **one retained R03 campaign**, one authenticated
seat at a time, and its first eligible ordinal-two selection opening. No
Maproom entry, public campaign listing, intelligence dispatch or generic
action API can reach it. The owner uses a separate development-only local
proof client supplied by the host after a campaign-specific grant redemption.
That client may call two **new private, disabled-by-default** host operations:
`ReadOwnerOpeningV1` and `SubmitOwnerInputV1`; exact transport and route names
remain a design choice, and this document creates neither. A headless fixture
that simply supplies a seat or decision cannot replace an owner-operated read
and submit through the selected host path.

1. An operator imports the retained original CreationRequest, ten predecessor
   ledgers, R03 ledger and pinned dependencies through the A1 validator. The
   host selects the evidence by a trusted manifest identity, verifies its
   physical bytes and stores the A1 checkpoint only if replay agrees. It
   assigns a private campaign journal key **from the authenticated
   CreationRequest campaign ID**; a caller cannot choose a different key.
2. A trusted creation coordinator commits a campaign-scoped seat-grant record
   before releasing any grant. The record binds campaign ID, side, a stable
   private `seatAuthorityId`, grant generation, issuer, expiry and a monotonic
   revocation epoch. The authority ID persists across an explicitly authorized
   renewal for the *same* owner; a transfer to another owner gets a new ID.
   It is not the process-local session generation. The grant secret is never logged
   or stored in plaintext. A real owner redeems it in the local proof client;
   the host records redemption and issues a new session generation. A1
   checkpoint bytes or a browser-selected side are never seat credentials.
   Process-local `LocalSessionStore` alone is insufficient after restart:
   A2 requires a durable grant/seat registry or a separately reviewed
   authenticator with equivalent recovery and revocation semantics.
3. `ReadOwnerOpeningV1` reauthenticates the session, obtains a trusted clock
   sample, reads the fenced journal head, reconstructs Core state, and asks
   Core for one immutable **owner-visible observation cut**. That same cut
   supplies the approved own `participantRef`, visible hex `locationId` used
   to derive `targetRef`, public rules/config/policy refs, position and cycle;
   Core verifies all of them and legal candidate membership against the
   frozen combat base before deriving the `selection` decision and
   `close-assault-full` alias. It cannot assemble the tuple from separate
   reads or from client-supplied refs. If source legality, ref provenance or
   equal-visible privacy is unproved, return generic unavailable and append
   nothing. Otherwise conditionally append an `owner-opening` **authority
   metadata** record under the same durable head and seat fence. This record
   retains exact owner-visible opening bytes, revision, decision/alias mapping,
   frozen combat base, deadline, seat authority and cut/source bindings. It
   advances journal sequence but has no game event, CP/ammo debit, RNG draw,
   or change to game state version/prefix. Read back and replay that exact
   committed record **before** returning the opening to the owner. A retry
   finds the existing opening and returns byte-identical visible bytes;
   competing derivations cannot each publish an offer. The private mapping
   stays in trusted storage; neither `p01.` nor hidden Source/World/dependency
   bytes leave it. Opening kind, empty game-event representation and caps
   require the separate A2 schema approval described below.
4. `SubmitOwnerInputV1` takes only the offered owner decision ref, candidate
   ref or explicit decline, opening revision and request ID. The host
   authenticates the current seat and CSRF/session
   generation **before** any alias lookup. It supplies Core with seat, side,
   campaign ID, grant generation, trusted clock sample and journal fence as
   host facts, never as client-selected fields. Core reads the persisted
   opening and checks its exact mapping, frozen combat base, permitted
   bookkeeping suffix, deadline, revision and
   accepted policy; it emits a canonical accepted input/event/receipt or the
   approved generic rejection. Only Chronicle can commit an accepted result.
   A timeout or unavailable clock may take an approved System cancellation
   path; it cannot invent an owner decline.
5. After a successful conditional append and durable Archives publication,
   the host returns the retained owner-safe receipt and next visible cut. A
   lost response is recovered by durable `(campaignId, seatAuthorityId,
   requestId)` and exact canonical input bytes after fresh authentication for
   that same authority ID. Session generation may change on restart and is
   never the dedupe key. No response is called accepted until readback proves
   the committed record. Revocation and append compare-and-swap the **same
   durable seat epoch/fence**: append first may commit and then revocation
   advances the epoch; revocation first invalidates the old epoch and forbids
   the append. A process-local semaphore cannot establish this order across
   host activations.

This path tests genuine seat-bound owner choice, not human identity beyond the
campaign grant. The owner must decide whether that grant authority is
sufficient for the private certificate. If independent person identity is
required, stop A2 and specify that authority before implementation.

## Closed contract inventory to freeze before implementation

These are proposed *fields*, not implemented schemas or approved wire bytes.
Freeze and obtain exact owner approval for field order, canonical encoding,
size limits, hash domains, unknown-field handling and rejection classes in
versioned schemas and golden vectors **before implementing A2**. This packet
does not approve those bytes or caps. Use exact canonical bytes rather than trusting
supplier hashes. The approved owner-visible codec remains authoritative.

| Contract | Required fields and exclusion |
| --- | --- |
| `P01PrivateSeatGrantV1` | Version, authenticated campaign ID, side, stable private seat authority ID, grant generation, issuer, issued/expiry times, monotonic revocation epoch and digest of a single-use secret. Persist before redemption. No raw grant or private combat bytes in ordinary logs. |
| `P01HostActorFactsV1` | Version, campaign ID from the authenticated session, stable seat authority ID, grant/session generations, side, trusted clock instant plus clock availability, journal key, durable seat epoch/fence and expected head. Server constructs it after authorization; it is never accepted from a request body. |
| `P01OwnerOpeningV1` | Version and only the approved owner `context`, independently verified `cycleRef`, `positionId`, `openingRevision`, `selection` decision ref, own `participantRef`, `targetRef` derived from the same approved visible hex, and offered owner candidate refs. Deadline and raw head/fence stay trusted; any additional outward status or token requires a separate visible-field decision. No Source/World hash, private candidate ID, opponent binding or journal sequence. |
| `P01OwnerInputV1` | Version, request ID, decision ref, opening revision and exactly one of approved candidate ref or explicit decline. The host binds this to the retained opening and head. No actor, side, clock, campaign identity, private candidate or Core action ID supplied by owner. |
| `P01PrivateJournalRecordV1` | The [A2 record inventory](p01-private-authority-journal-proposal.md#proposed-closed-import-checkpoint-and-later-journal-contracts), plus stable seat authority ID, seat epoch, canonical request digest and accepted receipt binding. Sequence 0 binds the A1 R03 checkpoint once. A separately approved `owner-opening` metadata kind at sequence 1 retains visible bytes, revision, candidate mapping, frozen base and deadline with **no game event** or state-version/prefix change; first accepted owner input is sequence 2. Later openings/closures advance journal sequence and visible revision by their own typed records. |
| `P01OwnerReceiptV1` | Version, request ID, approved generic accepted/rejected class, owner decision ref and revision. Any further outward field needs its own owner-visible approval. Core/private receipt and raw event stay trusted. Exact duplicate accepted input returns the same retained owner receipt. |
| `P01PrivateReadbackV1` | Retained import/dependencies, checkpoint, grant/seat provenance and ordered journal records with durable head/fence; replay verifies full CreationRequest and eleven-ledger R03 ancestry **and every opening record** before presenting or accepting an owner offer. |

Unknown version or kind, duplicate JSON keys, noncanonical bytes, malformed
IDs, over-cap input and inconsistent journal head fail closed. Client errors
follow the approved `invalid-request`, `stale-view` and generic
`not-current-or-not-admissible` split. A hidden-dependent failure cannot
become a distinct HTTP status, body, retry hint, log correlation or timing
class without AC-007 review.

## Journal publication and recovery order

Chronicle is the logical owner of ordered accepted events/receipts; Archives
is the durable byte store. A2 must choose and test a concrete provider before
claiming recovery. A minimal transaction boundary is:

1. Read a durable campaign head `(campaignId, authorityEpoch, sequence,
   stateVersion, prefix, recordHash, fence)` **and** the durable seat authority
   epoch. Require equal IDs across host session, CreationRequest, A1 checkpoint
   and journal key. Check the persisted opening against that head; creating
   one is itself a conditional metadata append, followed by exact readback,
   before owner exposure.
2. Core derives the proposed successor from readback-verified bytes. The
   store conditionally appends exactly one canonical record and stable
   `(campaignId, seatAuthorityId, requestId)` binding when *all* expected
   head fields, journal fence and seat epoch match. The receipt, accepted
   event, input, new head and durable material needed for replay publish
   atomically, or through a reviewed pending/committed protocol with no
   externally accepted pending state. Revocation atomically increments the
   same seat epoch in this store, so another process cannot commit with an
   old grant after revocation. Neither Orleans turn serialization nor a hash
   chain alone fences a competing process.
3. Read back the exact committed bytes, verify Core replay and receipt, then
   expose the safe result. On uncertain write outcome, read by campaign,
   stable seat authority ID and request ID, after reauthenticating the current
   session to that authority ID. Exact same input returns the original
   receipt; altered reuse conflicts. If readback is unavailable
   or mismatched, return unavailable and issue no new decision. Never retry a
   paid transition by recomputing an uncommitted debit or RNG draw.
4. On real host restart, restore grants/revocations and journal head from
   Archives, replay the original pinned R03 evidence and all accepted
   records in Core, compare every source, Control, World, RNG, prefix, event,
   receipt and conservation invariant, then reconstruct the owner-visible
   revision and **persisted** outstanding offer, including candidate mapping,
   base and deadline. A hash-only snapshot or process-local
   `TemporaryCampaignMemoryStore` is insufficient. A stale host epoch cannot
   append after a new host acquires the fence.

Persist original bytes for audit and encrypted/controlled hidden state for
recovery. Set retention, encryption key ownership and restore-failure policy
before writing a store. Chronicle/Archives are domain modules inside the
host, not new services. No model I/O occurs inside the authoritative turn.

## Acceptance proof and fail-sensitive tests

Run these on one reviewed head using retained original R03 evidence; synthetic
cases can supplement but cannot replace it.

1. **Source and ID:** physical archive and repository pins match; import one
   authentic root; validate campaign ID equality across CreationRequest,
   checkpoint, seat grant, host envelope, journal key and stored head. Mutate
   each identity independently and require rejection before opening or append.
2. **Actual owner path:** two independently issued seat grants, actual
   redemption, owner read, and explicit owner submission from the private
   client. Verify accepted receipt from durable readback, not test-injected
   actor/time or a fixture-only shortcut. Cross-seat, revoked, expired,
   reissued-generation and CSRF failures never reach alias lookup. Reconnect
   under a new session generation but the same durable seat authority ID and
   find the exact accepted receipt; a different authority ID cannot find it.
3. **Opening and action:** use the approved field codec; prove authentic
   own participant ref, target ref from the same visible hex, public refs and
   legal action membership at one committed cut. Crash before opening append
   exposes nothing; crash after opening commit but before read response
   returns the same persisted bytes and alias on restart. Tamper stored
   revision, mapping, base or deadline and fail closed. Reopen and
   hidden-only bookkeeping cases preserve the exact owner-visible revision
   rules. Reject a changed combat base even if a previously offered alias
   still exists. Exercise explicit decline, selected assault and System
   cancellation as distinct typed inputs; do not infer any from silence.
4. **Race and retry:** duplicate exact request across concurrent host
   activations and after restart returns one record and byte-identical
   receipt, with one CP/ammo debit and RNG draw. Changed-input duplicate,
   stale fence, competing append, lost response, crash before append, crash
   after append and archive publication failure all have deterministic
   readback and no double acceptance. Revocation/submit order is proved
   across two host processes using one atomic seat epoch and append fence;
   a local admission semaphore is not accepted as that proof.
5. **Privacy:** authenticate two independent original CreationRequest plus
   eleven-ledger histories with equal approved visible projection and
   differing hidden facts. Compare full owner output, IDs, ordering,
   acceptance/rejection, status, retry and logs after opening and hidden
   bookkeeping. If legality differs, fail before issuing the offer; never
   claim equal-visible acceptance with a synthetic pair. This gate remains
   open until the authentic pair is retained and reviewed.
6. **Recovery and bounds:** cold host restart reads full canonical history;
   remove, reorder, duplicate or mutate record/import/dependency bytes and
   require fail-closed recovery. Freeze per-field, event, journal, replay-time
   and retained-byte caps; test over-cap and unavailable clock/storage.
   Re-run the original 300-root audit and the complete AC-003/006/007/008
   certificate on one exact reviewed head before any admission claim.

## Decision, migration and stop rules

**Separate A2 owner decision required.** This packet is a design input, not
an implementation authorization. A future exact A2 approval package must
include the concrete transport/authenticator and transactional store,
versioned canonical schemas/vectors, byte and replay caps, approved public
ref and clock sources, retention/key ownership, and explicit amendment to the
P01 acceptance freeze. The owner must approve that whole exact package
**before A2 implementation**; a direction-only preference cannot authorize
code or issuance. Such approval would permit only
a private, disabled-by-default owner proof with authenticated campaign-seat
observation/submission, Core-generated typed decisions, and durable
Chronicle/Archives journal. It would also amend the
[P01 acceptance freeze](../specs/combat-actual-moved-repeat-position-v1.md#freeze-decision)
**only** to allow that private pre-certificate proof. It would not authorize
Maproom/public/native issuance, admission, or a production Combat loop. A1
approval alone is insufficient. Before seeking that exact approval, reviewers must
settle: concrete host transport and authenticator, whether campaign grants
are enough owner identity, storage provider/transaction/fence, canonical
schemas/caps, public ref source, deadline/clock source, archive encryption
and retention, and the exact authentic hidden-twin evidence path.

Implementation order after exact package approval: prove durable import/head
and campaign equality; add seat/clock authority and persisted opening read;
add typed submit and conditional append; prove restart, races and
privacy; only then consider certificate use. No test-only actor or clock fact
can be relabelled as authentic by migration. Version 1 readers remain
available for its immutable bytes; new fields require version 2 and explicit
replay comparison. Rollback disables new private openings and submissions,
preserves the journal for verified readback, and never promotes it to public
authority. Stop if any trusted origin, conditional append, durable readback,
equal-visible privacy proof or byte cap cannot be established.
