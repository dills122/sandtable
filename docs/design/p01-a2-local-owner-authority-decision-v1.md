# P01 A2 local owner authority: decision packet

**Status:** proposed owner choice; no A2 approval, acceptance-spec amendment,
schema freeze, runtime path, public action or P01 admission. This packet is
separate from the reviewed [A1/A2 boundary](p01-private-authority-journal-proposal.md)
and draft [A2 flow PR #297](https://github.com/dills122/sandtable/pull/297).
The original 300 R03 roots remain 44 provisional positive, 256 candidate
absent and zero admitted. The complete AC-003/006/007/008 certificate is open.

## Repository facts that constrain the choice

| Current fact | Evidence | Limit |
| --- | --- | --- |
| Core can replay R03 from a caller-supplied CreationRequest, eleven ledgers and dependency bytes. A1 allows an immutable `admitted=false` validation checkpoint. | [Native R03 consumer](../../src/Cna.Core/Campaigns/CampaignCombatActualMovedRepeatBreakdownCompletion.cs), [A1 boundary](p01-private-authority-journal-proposal.md) | Neither source is an authenticated owner decision or durable campaign journal. |
| The approved P01 owner codec fixes `context`, `cycleRef`, `positionId`, revision, `selection`, own participant and visible-hex target refs, alias domain and generic rejection. | [Owner contract](../specs/combat-p01-owner-decision-proposal-v1.md) | It does not create a P01 owner observation, authenticate public refs, assign grants or issue an offer. |
| Local host bootstrap creates a process-local operator ID; seat grants and sessions are held in memory. It serializes in-process submission against relinquish. | [Local session store](../../src/Cna.OrleansHost/Api/LocalSessionStore.cs) | It does not identify a person or survive host restart; its semaphore cannot fence another process. |
| Temporary Orleans campaign and private outward ingress target the admitted truck fixture, with in-memory request records. No public submit/receipt HTTP route calls the private ingress. | [Temporary grain](../../src/Cna.OrleansHost/TemporaryCampaignGrain.cs), [private ingress](../../src/Cna.OrleansHost/Api/TemporaryOutwardActionIngress.cs), [local API](../../src/Cna.OrleansHost/Api/LocalGameApi.cs) | They cannot be relabelled a P01 host route or durable proof. |
| MAP-05 specifies conditional publication, persistent fencing, unknown-commit reconciliation and restore provenance, while deferring a concrete provider. | [Trusted-host protocol](../specs/maproom-trusted-host-protocol-v1.md) | No existing Chronicle/Archives provider proves those properties for P01. |

## Choice L: authorize a *design direction* for local private proof

Use a **new, disabled-by-default, loopback-only** owner proof lane for one
retained R03 campaign at a time. It is separate from Maproom's public action
surface and the temporary truck fixture. The owner is a campaign **seat grant
holder**, not a verified real-world person. This is the exact trust assumption
for the choice: custody of a host-issued, single-use campaign/side grant is
sufficient to label a submitted decision as locally seat-authenticated for a
private proof. If certificate review requires independent person identity,
Choice L is insufficient and A2 must stop.

Proposed authority flow:

1. A host-only operator capability, provisioned outside browser bootstrap and
   persisted as a protected digest, authorizes a reviewed physical R03 import.
   The import binds CreationRequest campaign ID, A1 checkpoint and a new
   private journal key. The current self-issued `LocalSessionStore` operator
   ID does **not** authorize this import. The operator issues one grant per
   side only after a committed checkpoint binding; the owner receives and
   redeems that grant through the loopback proof client.
2. A durable seat row binds `(campaignId, side, seatAuthorityId)`, grant
   generation, issuer, secret digest, expiry and monotonic `seatEpoch`.
   `seatAuthorityId` is an internal stable owner lineage across an explicitly
   authorized renewal; a seat transfer gets a new ID. Session generation is
   ephemeral and never the accepted-request dedupe key. A new session after
   host restart must reauthenticate from the durable grant authority; an old
   cookie alone cannot restore a seat.
3. Host obtains a trusted clock sample and one readback-verified committed
   Core cut. That *same cut* must derive the approved own `participantRef`,
   public-hex `targetRef`, public rules/config/policy refs, position and
   cycle. It must prove the candidate legal under the bounded source profile
   before a decision can open. A new `owner-opening` authority-metadata
   record conditionally commits exact visible opening bytes, revision,
   private candidate mapping, frozen combat base and deadline. It changes
   journal sequence but not game event, version/prefix, RNG or resources.
   The host reads it back and replays it before showing an offer. A crash
   between commit and response returns the same persisted offer on retry.
4. Owner explicitly submits the offered decision ref, revision, candidate
   ref or decline, and a request ID. Host independently supplies campaign,
   seat authority, side, clock and durable head/fence to Core; browser claims
   authorize none of these. Authentication precedes alias lookup. Exact
   accepted retry key is `(campaignId, seatAuthorityId, requestId)` plus a
   byte-equality check of canonical input. It survives a changed session
   generation; changed input conflicts. A different authority ID cannot
   retrieve that receipt. System timeout cancellation never invents decline.
5. Revocation and append compare-and-swap the **same durable seat epoch and
   campaign head**. If append commits first, its receipt remains committed
   and revocation advances the epoch next. If revocation commits first, old
   append fails. A local semaphore is not the cross-process proof.

**Proposed provider for this bounded local lane:** one SQLite database in a
restricted local directory, using `Microsoft.Data.Sqlite` as a *new* reviewed
dependency. Use WAL, `synchronous=FULL`, a bounded busy timeout and a
transaction that checks campaign head, storage fence and seat epoch before
inserting opening/accepted record, request index and new head atomically.
Lease/activation fencing advances persistently before a new process writes;
every write checks its epoch in the same transaction. After ambiguous commit,
read back exact record plus request index and replay it; never retry a debit
or RNG draw by guess. `BEGIN IMMEDIATE` serializes local writers, but the
head/epoch predicates, not SQLite locking alone, define correctness. This
chooses a **single-host local proof**, not clustered or production hosting.

SQLite file permissions, encryption/key custody, retention and restore-failure
policy require exact review before storing hidden R03 bytes. WAL and database
files must be backed up/recovered as one authority; copying only a database
file while writes are active is not a valid cold-readback test. A storage
provider cannot authenticate a supplier's CreationRequest or owner seat.

## Proposed closed records and caps to freeze next

These are the minimum v1 records for the later exact schema package, not
approved canonical bytes. The [existing private P01 bounds](../specs/combat-p01-paid-all44-cuts-v2.md)
measure Source 5,627,669 B, input 1,004 B, event 5,978 B, Control 44,589 B
and World 3,326 B, within **existing** 6 MiB, 4 KiB, 8 KiB, 64 KiB and
16 KiB caps. Those bounds can be reused for matching P01 fields; they do
not establish a cap for eleven-ledger import, journal aggregate, opening,
grant registry or replay time. Measure authentic import and all-prefix
worst cases, then obtain owner approval of exact per-field, total retained
byte and replay-time caps before A2 code.

| Proposed record | Minimum private fields |
| --- | --- |
| `ImportBindingV1` | Version, CreationRequest campaign ID and bytes/hash, ordered eleven ledger bytes/hashes, pinned dependency identities/bytes, A1 checkpoint bytes/hash, source profile, `admitted:false`. Supplier vectors cannot select or authenticate it. |
| `SeatAuthorityV1` | Version, campaign ID, side, stable private authority ID, operator/issuer ID, grant generation and digest, expiry, revocation epoch. Secrets never enter ordinary logs. |
| `JournalHeadV1` | Version, campaign ID, authority epoch, sequence, prior/terminal record hash, game state version/prefix, storage fence and seat epoch. Campaign ID equals import, checkpoint, grant and journal key. |
| `OpeningRecordV1` | Version, `owner-opening` kind, expected/result head, approved owner-visible bytes and revision, trusted deadline, frozen combat base, candidate-to-private mapping, stable seat authority ID, seat epoch; no game event or game version/prefix change. First opening follows sequence-0 R03 binding. |
| `AcceptedInputRecordV1` | Version, stable `(campaignId, seatAuthorityId, requestId)`, exact canonical owner input, accepted Core input/event/receipt, expected/result head and seat epoch, resulting World/RNG binding. First accepted input follows the committed opening. |
| `ReadbackV1` | Version, original import, checkpoint, grant/seat history, all ordered journal records and committed head. Core replays each byte and rejects missing, reordered, duplicate, changed or unknown-version material before an owner cut is exposed. |

The approved [owner codec](../specs/combat-p01-owner-decision-proposal-v1.md#frozen-approved-private-codec)
already fixes ASCII JSON order, duplicate-key rejection, ID shape and hash
domains for its tuple. This packet adds **no** owner-visible field. New
private record field order, domain tags, length framing, unknown-field
rejection and all remaining caps need golden vectors and explicit owner
approval before implementation. A guessed constant would create false
coverage; no A2 reader may accept an unfrozen version.

## Required hosted proof before certificate use

Use a real host process, real SQLite file and fake `TimeProvider` for
deadline tests; use two host processes for fencing/race cases. Test both
Axis and Commonwealth with retained original R03 inputs and all 300 roots.
For accepted owner actions, use the actual grant-redemption, owner read and
submit path; test-injected actor or clock facts are only unit-test inputs.

- Import: hash physical evidence and independently selected pins. Mutate
  CreationRequest/host/grant/checkpoint/journal campaign IDs one at a time;
  reject before opening. A caller-provided `p01.` candidate or hash does not
  select import authority.
- Opening: verify own participant/public-hex target and all public refs from
  one committed cut; return unavailable if any source is unapproved. Crash
  before opening append exposes nothing; crash after append but before
  response returns byte-identical opening and alias after restart. Tamper
  revision, mapping, base or deadline and fail closed.
- Submit: wrong seat, expired/revoked/reissued grant, CSRF, stale revision,
  changed combat base, hidden-dependent nonmembership and unavailable clock
  reject without revealing private blockers. Explicit decline, selected
  assault and System cancellation remain distinct typed inputs.
- Publication: duplicate exact input after response loss or process restart
  returns one stored receipt, one event, one CP/ammo debit and one RNG draw;
  changed-input duplicate conflicts. Two-process append/revoke races and
  stale activation fences have one committed order. Crash before commit,
  after commit, during readback and after callback loss never invent success.
- Privacy: two **independent authentic** CreationRequest plus eleven-ledger
  R03 histories with equal approved visible facts but different hidden facts
  must yield equal owner bytes, IDs, action membership and accept/reject
  behavior. If legality differs, suppress the offer before either owner sees
  it. Synthetic pairs cannot close AC-007.
- Recovery/resources: cold restart verifies every canonical import and
  journal byte, World/RNG/receipt/conservation, grants and revocation epoch.
  Remove/reorder/mutate evidence and exceed every frozen cap. Repeat the
  complete AC-003/006/007/008 certificate on one reviewed head before any
  P01 admission or public action claim.

## Choice D: defer this private owner path

Wait for a separately designed durable host identity and journal that can
authenticate real campaign seats without the local grant-custody assumption.
Keep A1 as private validation only and the #297 design draft as research.
This avoids a local-only authority that might not satisfy the certificate,
but owner input, hosted clock, AC-007 twins and P01 admission remain blocked.

## Exact decision requested later

**Choose L only to authorize the local grant-custody trust model and SQLite
design direction for a *private proof*.** It does not approve A2
implementation, public action, or an acceptance-spec amendment. Before code,
review and obtain a further exact owner approval covering: canonical schemas
and all caps, operator-secret provisioning and seat renewal/transfer rules,
same-cut owner projection/public-ref source, key/file custody and retention,
provider transactions/fencing, and the explicit narrow amendment to the
[P01 freeze](../specs/combat-actual-moved-repeat-position-v1.md#freeze-decision).
If the owner requires person-level identity or cannot approve local grant
custody, choose D. Neither choice establishes authentic AC-003/006/007
evidence; only the hosted tests above can do that. Any missing trust root,
unprovable public ref, unfenced writer, unreadable committed record, missing
authentic twin or over-cap source is a stop, never a supported-empty action.
