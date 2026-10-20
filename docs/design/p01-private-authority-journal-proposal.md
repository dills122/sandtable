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

## Decision A: permit a pre-certificate, private replay-only journal boundary

**Recommended, subject to explicit approval of the plan amendment below.**
Name this bounded preparatory milestone **P03a**. `Cna.Core` owns deterministic
validation, replay, the versioned private state model, and accepted event/receipt
bytes. Chronicle owns authoritative append history; Archives owns durable bytes
and recovery. `Cna.OrleansHost` may acquire files, authenticate a seat, sample a
trusted clock and coordinate a conditional append, but must never invent a Core
event or infer a defender response. The Intelligence plane and Maproom receive
no P03a data. A P03a proof journal is authoritative only for its own private
replay, never for an admitted campaign. No P01 action or decision opens in this
milestone.

### Proposed closed import and journal contracts

These are field inventories for a **new** version-1 canonical schema, not
implemented types. Before code, freeze their exact field order, byte caps,
hash domains, duplicate-key handling and rejection codes in a schema and tests.
Every digest is SHA-256 over exact canonical bytes; no digest authenticates an
untrusted supplier by itself.

| Contract | Exact proposed fields and owner |
| --- | --- |
| `P01R03ImportV1` | `contractVersion:1`; canonical CreationRequest bytes; ten ordered predecessor input ledgers; one ordered R03 input ledger; canonical final R03 Source bytes; independently selected trusted Rules10/Content7/configuration identity; pinned dependency-manifest identity. The host acquires the bytes outside a grain turn. Core reconstructs the creation context from trusted catalog inputs, replays all eleven ledgers using the existing native R03 consumer, and compares the recomputed terminal Source, Control, Proof, event, receipt, prefix, World and RNG to the supplied canonical vector. A supplied vector, row label or hash never substitutes for replay. |
| `P01PrivateJournalHeaderV1` | `contractVersion:1`, private campaign/creation binding, R03 source hash, creation/request hash, ordered ledger hashes and lengths, dependency-manifest hash, final R03 Control/Proof hashes, terminal version/prefix/receipt, exact ordinal-two Combat Position and active-cycle identity, and `admitted:false`. All bytes are Core-validated before a header can be committed. No private hash is an outward ID. |
| `P01PrivateJournalRecordV1` | `contractVersion:1`, monotonic sequence, prior record hash, prior state version/prefix, closed event kind, canonical input/event bytes and hashes, accepted receipt ID, resulting state version/prefix, and `admitted:false`. The first record is a **replay/import checkpoint** of the already authenticated R03 terminal, not a newly adjudicated Combat step. Later P01 records require separately certified transition contracts. Unknown kind/version fails closed. |
| `P01PrivateReadbackV1` | Exact header plus ordered records and retained import/dependency bytes. Readback replays from CreationRequest and all eleven ledgers, compares the checkpoint record to that already replayed R03 transition, then replays any later accepted records; it rejects missing, duplicate, reordered or mutated bytes and mismatched terminal state. A hash-only snapshot cannot restore authority. |

For the checkpoint record, `kind` is exactly `r03-import-checkpoint`, its
canonical input/event are the already accepted R03 System input/event, its
prior version/prefix come from the authenticated R02 terminal, and its result
is the replayed R03 terminal. It does **not** append that event a second time
to the game history or consume a twelfth P01 input. The journal's own first
sequence is zero; later records, if separately authorized, begin at one.

For the first milestone, one pinned original root such as `r012-c01` can be
read from the **retained private archive**, checked against its independently
pinned chunk/manifest/source digests, then replayed natively. Its reviewed R03
source hash is
`sha256:7dd4a0860b65d8bd1db51f65cd2eadc7b57110cf95c0bf5d32447f2937573638`,
terminal Control hash is
`sha256:1ac7c737fd7f7fe38d13b741cdc92ef658d3e4e9e2b9d06d736207f24c50de7c`,
and receipt is
`mrbc.af668a6a66dd890e432e4012f3690f10cfa8fb4e5a23c4ec4f228836a30f61d9`.
The private archive is not a repository fixture. Hosted CI uses the frozen
in-repository semantic vectors; a local physical-evidence gate checks these
exact retained bytes. Neither may manufacture owner authority.

### Owner opening, input and receipt boundary after P03a

The owner-approved private [owner-decision tuple](../specs/combat-p01-owner-decision-proposal-v1.md)
(approved in the coordinator chat on 2026-10-20; that older proposal's status
text has not yet been synchronized)
supplies `context`, public cycle reference, position, owner-visible opening
revision, `selection` decision kind, own participant reference and public hex
target. P03a may define closed `P01OwnerOpeningV1`, `P01OwnerInputV1` and
`P01OwnerReceiptV1` schemas **for later use**, but does not create or expose
an opening. A later separately approved P03 transition must bind every
opening to a committed journal version/prefix and authenticated seat; only
then may an owner input be considered. Revision starts at zero at the R03
boundary; the first visible opening becomes one, exactly as the approved
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
only; their actor/time values cannot be promoted to production authority
([selected private reference](../specs/combat-p01-selected-assault-v1.md),
[owner rejection policy](../specs/combat-p01-owner-decision-proposal-v1.md#retained-private-binding-and-rejection)).

### Storage, restart and privacy

- Core owns deterministic record validation and replay. Chronicle owns the
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

### First reviewable milestone and stop rules

1. Freeze closed import/header/checkpoint/readback schemas and exact caps before
   implementation. Verify the original CreationRequest, eleven independent
   ledgers, selected context, 108 pinned runtime dependencies, R03 Source and
   terminal vector for one retained root. Reuse native R03 replay; do not
   translate the Python assessment into authority or accept a claimed Control.
2. Core test: authentic root imports to byte-identical private checkpoint,
   duplicate import is exact and produces no additional record; mutate each
   ledger, Source, context/dependency pin, event, receipt, version, prefix and
   order, then assert rejection before a checkpoint is committed. Test a
   truncated and reordered journal and cold readback. Keep `admitted=false`
   and no owner opening, legal action, result, settlement or public bytes.
3. Add a later host/storage milestone only after a separate conditional-append
   design review and measured resource/cap proof. Add owner opening/input/receipt
   behavior only after the full support certificate, authentic AC-007 hidden
   twins, and explicit P03 scope decision. The P03a checkpoint cannot satisfy
   those gates by existing.

Stop before implementation if trusted context cannot be selected independently
of the imported envelope; the full original ledger/dependency set cannot be
retained and re-read; exact native replay differs from the pinned root; storage
cannot fence writes; or a proposed opening would rely on unverified seat/clock
claims. The test may report that gate without fabricating an imported state.

### Migration and rollback

P03a is additive and private. It does not rewrite an existing Rules9/V11
campaign, alter the 300 original R03 records, change the Python reference,
or activate P01 in an admitted host. Rollback disables the new import entry
point and leaves existing campaigns untouched. A written private journal stays
readable by its versioned verifier but cannot be opened or submitted through
the old host. A later migration to admitted P03 must replay the exact original
history under both versions, compare World/RNG/receipts and owner-visible
history, then atomically select one authority epoch; it cannot treat the
P03a checkpoint as prior public acceptance.

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

**Approve A** only if the owner accepts this limited pre-certificate scope:

> Permit a private, unissuable P03a Cna.Core R03 import/journal checkpoint
> before the P01 support certificate. It may authenticate one original
> CreationRequest plus eleven-ledger R03 root and commit/replay a private
> `admitted=false` checkpoint. It may define, but must not issue, owner
> opening/input/receipt contracts. No P01 choice, debit, settlement, public
> observation/action, Maproom route or durable-host claim follows. Any
> owner-facing behavior, P03 native consumption and public activation remain
> gated on the complete certificate, authentic privacy proof and separate
> host/storage approval.

If A is approved, replace item 5 under **Work order and stop rules** in
[`2026-10-19-p01-admission-overnight.md`](../work/plans/2026-10-19-p01-admission-overnight.md)
with this exact text in a later, separately reviewed documentation change:

> 5. A separately owner-approved, private and unissuable P03a may precede
> the support certificate solely to authenticate retained R03 inputs and
> define/replay a Core-owned `admitted=false` journal checkpoint. P03a cannot
> open an owner decision, accept a P01 input, emit a public action, mutate a
> live campaign or claim durable-host recovery. Only after AC-003/006/007/008,
> the complete P01 support certificate and authentic privacy proof pass on one
> reviewed head may P02 selection and P03 native consumption, host/storage
> binding, public activation, Exercise and Runner be considered. Each remains
> a separate gate; neither P03a nor a provisional 44/256 classification
> admits P01 or completes the Combat loop.

**Choose B** to retain the current item 5 unchanged. In either case, this
proposal itself is not the approval or implementation of P03a.
