# P01 private authority journal: owner decision packet

**Status:** proposal for owner decision. This document changes no contract, runtime,
support certificate, admission, native P01 transition, host route, or public action.
The original 300 CreationRequest plus eleven-ledger R03 roots remain 44 provisional
positive / 256 candidate-absent / zero admitted. The complete AC-003/006/007/008
certificate and separate P02/P03 gates remain open.

## Why a decision is needed

The private native R03 consumer already replays a caller-supplied trusted
`CampaignCombatCreationRequest`, ten predecessor input ledgers, R03's own input
ledger, and pinned dependency bytes; it derives the ordinal-two Combat Position
Control, Proof, event and receipt. It is not an authoritative campaign journal or
a P01 command consumer. All 300 R03 terminal vector hashes have native parity
([R03 contract](../specs/combat-actual-moved-repeat-breakdown-completion-v1.md),
[native replay](../../src/Cna.Core/Campaigns/CampaignCombatActualMovedRepeatBreakdownCompletion.cs),
[architecture account](../../tech-design.md#project-responsibilities-and-current-boundaries)).

The currently admitted `CampaignAuthorityHandle` wraps a V11 snapshot and
`CampaignContentContext` created from the Rules9/Content6 path. The private R03
replay uses a separately retained Rules10/Content7
`CampaignCombatCreationContext`. Neither the existing handle nor the local
Orleans fixture establishes how the eleven R03 ledgers become owned campaign
history ([handle](../../src/Cna.Core/Campaigns/CampaignAuthorityHandle.cs),
[creation path](../../src/Cna.Core/Campaigns/CampaignCreationExecution.cs),
[local host limits](../../README.md#project-at-a-glance)).
The [overnight P01 plan](../work/plans/2026-10-19-p01-admission-overnight.md)
currently defers P03 native consumption until the complete support certificate.
Adding a disconnected Core state wrapper or importing an asserted Control hash
would not advance that gate.

## Decision A: permit a pre-certificate, private authority proof in two decisions

**A1/P03a** is a Core-owned immutable *validation checkpoint*. It replays one
original retained R03 root and returns a versioned, `admitted=false` object.
It does not append, commit, issue an owner opening, or promise recovery after a
host restart. The existing R03 System event is represented once, never
adjudicated a second time. `Cna.Core` owns deterministic validation and bytes;
the host only supplies retained evidence. Maproom and the Intelligence plane
receive nothing. A1 alone does **not** break the AC-003/006/007 critical path:
authenticated owner decisions are still absent.

**A2/P03b** is a *separate owner decision* for private, non-public pre-certificate
owner-decision proof. If approved after a concrete storage design review, Core
would derive typed owner opening/input/event/receipt from its validated
checkpoint, while OrleansHost supplies independently authenticated seat and
trusted clock facts. Chronicle would own conditional append and Archives would
own durable recovery. A2 can supply authentic owner decisions needed by the
certificate without making P01 public or admitted. Neither A1 approval nor
this proposal approves A2, an actual store, a route, or a runtime transition.

### Proposed closed import, checkpoint and later journal contracts

These are field inventories for a **new** version-1 canonical schema, not
implemented types. Before code, freeze their exact field order, byte caps,
hash domains, duplicate-key handling and rejection codes in a schema and tests.
Every digest is SHA-256 over exact canonical bytes; no digest authenticates an
untrusted supplier by itself.

| Contract | Exact proposed fields and owner |
| --- | --- |
| `P01R03ImportV1` (A1) | `contractVersion:1`; canonical CreationRequest bytes; ten ordered predecessor input ledgers; one ordered R03 input ledger; canonical final R03 Source bytes; independently selected trusted Rules10/Content7/configuration identity; pinned dependency-manifest identity. Core reconstructs the context from trusted catalog inputs, replays all eleven ledgers using the existing native R03 consumer, and recomputes terminal Source, Control, Proof, event, receipt, prefix, World and RNG. Supplier row labels, vectors and hashes never authorize import. |
| `P01ValidatedCheckpointV1` (A1) | `contractVersion:1`, private creation binding, R03 source hash, creation/request hash, ordered ledger hashes and lengths, dependency-manifest hash, recomputed final R03 Control/Proof hashes, terminal version/prefix/receipt, exact ordinal-two Combat Position and active-cycle identity, and `admitted:false`. This immutable Core result has no append sequence or outward ID. Repeating validation gives byte-identical output, not a committed write. |
| `P01PrivateJournalRecordV1` (A2 only) | `contractVersion:1`, monotonic sequence, prior record hash, prior state version/prefix, closed event kind, canonical input/event bytes and hashes, accepted receipt ID, resulting state version/prefix, and `admitted:false`. The first record binds the **validated R03 checkpoint**, not a new Combat step or second R03 event append. Later P01 records require separately approved typed owner transitions. Unknown kind/version fails closed. |
| `P01PrivateReadbackV1` (A2 only) | Retained import/dependency bytes plus ordered journal records and storage fence. Readback replays CreationRequest and all eleven ledgers, verifies the checkpoint binding and every later accepted record, and rejects missing, duplicate, reordered or mutated bytes. A hash-only snapshot cannot restore authority. |

If A2 is later approved, its first binding record has kind
`r03-import-checkpoint`, references the already accepted R03 System input/event,
uses the authenticated R02 prior version/prefix and replayed R03 result, and
never appends that game event twice or consumes a twelfth P01 input. Its private
journal sequence starts at zero; separately authorized later records start at
one. A1 has **no journal sequence**.

For A1, `r012-c01` (row index 36) comes from the **retained private archive**
`/Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence`. Its retained
`manifest.json` (SHA-256
`39cae77c405b83a793b3d806fcf4a67860b9ff8514e59c9bfa83384d7dfc2b35`)
pins the original chunks and summary. Independently frozen
repository vectors are in
[`combat-actual-moved-repeat-breakdown-native-hashes-v1.json`](../specs/fixtures/combat-actual-moved-repeat-breakdown-native-hashes-v1.json),
at SHA-256 `cf3f8ecfe9e4040d5678a6e0e9cc2441525e1e8fe3a3f35cf316207a9f45ec4e`;
it records source and transcript SHA-256 identities and all 300 semantic
rows. The archive and repository fixture must match at their separately
verified pins before this row is selected; the importer cannot select an
expected row from supplier input. CI compares recomputed native output with
the in-repository frozen vector; the local physical-evidence gate additionally
hashes and replays the retained archive bytes. Without that archive, CI proves
semantic parity only, not physical R03 provenance. For `r012-c01`, the reviewed
R03 source hash is
`sha256:7dd4a0860b65d8bd1db51f65cd2eadc7b57110cf95c0bf5d32447f2937573638`,
R02 prior Control hash is
`sha256:53f566f533d0ec30541776056bb3164e03c02cae023de7cef500d5070603837a`,
R02 prior Proof hash is
`sha256:766d07b0c703c16e8e32cb87946984f4da0ef23023a6b64676e2845f3ece7a2a`,
terminal Control hash is
`sha256:1ac7c737fd7f7fe38d13b741cdc92ef658d3e4e9e2b9d06d736207f24c50de7c`,
terminal event hash is
`sha256:3165ec2878d3f27235410aa4ae6a2827583fc6a27757f8e9fd2d99a492689e9d`,
terminal Proof hash is
`sha256:c084755d9e970cf3bb7b751748db47a9193d51e3d2281b4f1f3a00c68e96d93f`,
and receipt is
`mrbc.af668a6a66dd890e432e4012f3690f10cfa8fb4e5a23c4ec4f228836a30f61d9`.
The supplier's copy of any vector is never authority. Neither gate manufactures
owner authority.

### Conditional A2 owner opening, input and receipt boundary

The owner-approved private [owner-decision tuple](../specs/combat-p01-owner-decision-proposal-v1.md)
(approved in coordinator chat on 2026-10-20; merged proposal PR #286)
supplies `context`, public cycle reference, position, owner-visible opening
revision, `selection` decision kind, own participant reference and public hex
target. A1 may define closed `P01OwnerOpeningV1`, `P01OwnerInputV1` and
`P01OwnerReceiptV1` schemas for A2, but creates no opening. If A2 is separately
approved and its host/storage proof passes, it may privately issue and retain
an authenticated owner opening/input/receipt **before** the support certificate
for proof. It remains unissuable to Maproom, public/native action APIs and
admitted campaigns. Every opening binds a persisted journal version/prefix,
storage fence, authenticated seat and trusted clock. Revision starts at zero
at the R03 boundary; the first visible opening becomes one, exactly as the approved
owner contract specifies. The private `p01.` candidate, Source hash and
hidden opponent bindings never enter owner-visible bytes.

The host-to-Core command envelope must carry an independently authenticated
`campaignId`, seat grant identity, side, current journal fence, and trusted
clock sample/availability. These are host facts, not fields a browser or
retained R03 input may authorize. Core verifies that the side owns the live
decision and that version, prefix, revision, membership, deadline and frozen
combat base match; it emits canonical accepted input/event/receipt bytes or a
generic rejection. A timeout or unavailable clock may cancel under accepted
policy, never synthesize defender decline. Exact duplicate input returns the
retained receipt without a second append, debit or RNG draw. Changed-input
duplicates and stale fences reject. Until an actual authenticated host and
journal fence exist, tests inject **explicitly labelled trusted test facts**
only; their actor/time values cannot satisfy AC-003/006/007 owner proof or be
promoted to production authority
([selected private reference](../specs/combat-p01-selected-assault-v1.md),
[owner rejection policy](../specs/combat-p01-owner-decision-proposal-v1.md#retained-private-binding-and-rejection)).

### A2 storage, restart and privacy prerequisites

- A1 has only an immutable Core checkpoint; no committed state, storage fence,
  host restart guarantee or cold journal readback. A2 must first pass a separate
  versioned storage/host design review. Core owns deterministic record
  validation and replay. Chronicle owns the
  append order and accepted receipts; Archives must persist header, original
  import bytes, pinned dependency bytes and records atomically or with a
  recoverable pending/committed protocol. A host process restart cannot resume
  from an in-memory-only hash. The present temporary grain is process-lifetime
  memory and is **not** a durable P01 store
  ([README](../../README.md#project-at-a-glance),
  [tech design](../../tech-design.md#accepted-local-server-and-maproom-direction)).
- Append uses campaign-scoped expected sequence/version/prefix and a storage
  fence. Competing appenders cannot both publish the same successor. After an
  ambiguous write, read back the exact canonical record and receipt before
  retry. A failed or mismatched readback remains closed; no inferred success.
- Archive and logs retain hidden World, Source, ledgers and private candidate
  only in trusted storage. Maproom, Signals and generic errors receive none of
  them. Unauthenticated requests never reach alias lookup; authenticated
  hidden-dependent failures use the approved generic rejection. Timing and
  traffic equivalence remain a separate AC-007 review.
- Version 1 is immutable once frozen. Removed fields are reserved, new fields
  require version 2 and an explicit migration reader. Recovery compares all
  canonical bytes against the old version before migration; an unknown version
  or changed dependency pin fails closed. No auto-upgrade of a private import
  into a public action.

### A1 acceptance tests and stop rules

1. Freeze closed import/checkpoint schemas and exact caps before
   implementation. Verify the original CreationRequest, eleven independent
   ledgers, selected context, 108 pinned runtime dependencies, R03 Source and
   terminal vector for one retained root. Reuse native R03 replay; do not
   translate the Python assessment into authority or accept a claimed Control.
2. Core test: authentic root validates to byte-identical private checkpoint,
   duplicate validation is exact and has no side effect; mutate each
   ledger, Source, context/dependency pin, event, receipt, version, prefix and
   order, then assert rejection before a checkpoint result is returned. A1
   may serialize/replay that result in a fresh interpreter, but cannot claim
   durable host recovery or journal readback. Keep `admitted=false`
   and no owner opening, legal action, result, settlement or public bytes.
3. A2, only if separately owner-approved, must prove conditional append/fence,
   exact byte readback after a real host restart, authenticated seat/clock,
   accepted retry without duplicate record/debit/RNG, failed and competing
   appenders, privacy and resource caps. It must then exercise authentic
   owner decisions and equal-visible hidden-history acceptance/rejection for
   AC-007. A1 alone cannot satisfy those gates.

Stop A1 before implementation if trusted context or expected vector cannot be
selected independently of the imported envelope; the full original ledger and
dependency set cannot be physically checked; or exact native replay differs
from the pinned root. Stop A2 before implementation if storage cannot fence
writes or an opening would rely on unverified seat/clock claims. Report the
gate without fabricating authority.

### Migration and rollback

A1 is additive and private. It does not rewrite an existing Rules9/V11
campaign, alter the 300 original R03 records, change the Python reference,
or activate P01 in an admitted host. Rollback disables the new validation entry
point and leaves existing campaigns untouched. If A2 is later approved, its
private journal stays readable by its versioned verifier but cannot be opened
or submitted through the old host. A later migration to admitted P03 must
replay the exact original history under both versions, compare World/RNG/receipts and owner-visible
history, then atomically select one authority epoch; it cannot treat the
A1 checkpoint or A2 private proof as prior public acceptance.

## Decision B: defer all Core-owned P01 journal work until P03

Keep the existing private native R03 consumer and offline P01 proofs only.
Complete AC-003/006/007/008, the all-row support certificate and authentic
hidden-twin privacy proof first. Then design one integrated P03 journal,
host-seat/clock binding, durable readback and outward issuance contract.
No plan amendment or pre-certificate Core import work occurs. This avoids
premature storage/API commitments, but leaves native journal integration and
its restart/cap risks to the later critical path. Existing all-300 native R03
parity remains valid evidence, not a running P01 authority.

## Exact decision and plan amendment

**Approve A1** only if the owner accepts this limited pre-certificate scope:

> Permit a private, unissuable P03a Cna.Core R03 **validation checkpoint**
> before the P01 support certificate. It may authenticate one original
> CreationRequest plus eleven-ledger R03 root and return an immutable,
> `admitted=false` Core result. It may define, but must not issue, owner
> opening/input/receipt contracts. It does not append or commit history and
> makes no cold-host recovery, owner-decision, Maproom, public/native action,
> or admission claim. A1 alone does not unblock the owner-decision proof.

**A2 requires a separate owner decision** after a concrete storage/host design
review. The proposed scope is private and non-public authenticated owner
opening/input/receipt proof before certification, with host-derived seat and
clock, Chronicle/Archives conditional append and restart readback, and
authentic AC-007 twin acceptance/rejection. It cannot be inferred from A1
approval. Native/public P03 consumption and admission remain gated on the
complete certificate and separate approval.

If A1 is approved, replace item 5 under **Work order and stop rules** in
[`2026-10-19-p01-admission-overnight.md`](../work/plans/2026-10-19-p01-admission-overnight.md)
with this exact text in a later, separately reviewed documentation change:

> 5. Separately owner-approved, private and unissuable P03a may precede the
> support certificate solely to validate retained R03 inputs and return an
> immutable Core-owned `admitted=false` checkpoint; it cannot append history,
> open an owner decision, claim durable readback or admit P01. A distinct
> owner-approved P03b, after storage/host design review, may prove authenticated
> owner decisions privately with retained journal, seat/clock binding and
> restart, enabling AC-003/006/007 evidence without public/native issuance.
> It does not itself certify or admit P01. Only after AC-003/006/007/008, the
> complete support certificate and authentic privacy proof pass on one
> reviewed head may P02 selection and P03 native/public consumption,
> activation, Exercise and Runner be considered. Each remains a separate gate;
> neither pre-certificate phase nor provisional 44/256 admits P01 or completes
> the Combat loop.

**Choose B** to retain the current item 5 unchanged. In either case, this
proposal itself is not approval or implementation of A1 or A2.
