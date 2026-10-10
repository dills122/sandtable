# Actual prepared-round entry v1

Private executable contract, REL-AUD-02B. This family joins the full original actual-selection
history at Force Assignment20 to Prepared Close Assault25 without charging or resolving an
attack. It also admits cancellation to no-attack Reserve Release. Contract implementation is
reviewable author work; independent readiness belongs to the coordinator's fresh reviewer.

## Authority and limits

The accepted [feasibility report](../research/combat-actual-round-entry-feasibility.md) and
[Combat plan](../design/combat-cycle-implementation-plan.md) define the boundary. Only the two
original seed1 Normal/NONE first-side stage1 sources are admitted: Axis first and Commonwealth
last. Preserve Request/Created, all twelve entry events/receipts, seven actual selection events,
accepted actual defender decline, full MovementEndProof, Weather receipt/full event, Breakdown
receipt/full event, original cycle/configuration/catalog identities, World and RNG seed1/cursor2.
The actual catalog keeps `activeSide=null`; role ownership resolves from original cycle order.
Laboratory content and setup labels remain synthetic. This join is authentic history over those
fixtures, not historical scenario validation or public gameplay.

The caller supplies two independent ordered trusted-input ledgers: original selection inputs
and new round inputs. Event-embedded actor/time is evidence compared against these ledgers,
never a seat, clock-confidence or persistence authenticator. A coherent substitute source and
alleged trusted ledgers crosses the caller's authentication precondition. The reference oracle
uses the unchanged fixed creation context; future native callers must supply independently
retained context and bytes, with all acquisition I/O outside authoritative turns.

No native, host, observation, action, intelligence, gRPC, Snapshot or Archives activation exists.
No commitment, cost, draw, result, settlement, positive CA completion, repeat, later-II or consumed
lineage is admitted. Old C3a/Round2/Result2 grammars and pins remain immutable and incompatible.

## Closed records and canonical bytes

The [ordered schema inventory](combat-actual-round-entry-v1.schema.json) defines exact keys,
key order, primitive types, explicit nullable arms, effect tags, framing domains and limits.
It is an executable ordered inventory, not a generic JSON Schema validator. The new records are
ActualRoundSource, Base, ClockConfiguration, ClockTiming, Allocation, Slot, RoundCommand,
RoundInput, RoundEvent, four effect arms, RoundReceipt, RoundControl, ActualRoundProof and
EmptyAaCertificate. Inherited records use the unchanged predecessor grammar and its existing syntax-error mapping. New semantic arrays
(slots, inputs, events, receipts, assignments) never sort; inherited identity arrays retain their
established canonical rules.

Canonical bytes are compact ASCII-compatible UTF-8 with ordered closed keys and explicit nulls.
Reject unknown/missing/duplicate keys, BOM, whitespace, trailing documents, alternate escapes,
key reordering, non-ASCII carriers, floats/exponent spellings and booleans as integers. Shape and
wrong primitive types reject001; new numeric ranges and primitive value bounds reject002; alternate
canonical spellings reject008 after valid shape. Each encoded source/proof/input/event/control
is at most1MiB; recursive depth32 and ordinary arrays512. Selection event/ledger capacity16 and
new round event/ledger/receipt capacity16 apply independently. Full aggregate original history
is retained: twelve entry plus seven selection plus at most six round events; no global16 cut.
Versions are nonnegative checked Int64; UTC is0..253402300799999. Int primitives are nonnegative
Int32. New accepts require a nonoverflowing version increment and fewer than sixteen receipts.

ActualRoundSource order: contractVersion, full actualSelectionSourceCanonicalUtf8,
clockConfiguration, ordered roundEventCanonicalUtf8. Ledgers are separate ingress arguments.
Base is derived only after full predecessor replay and binds complete actual-selection proof
plus clock configuration. No caller-derived Base, digest-only proof or private transition helper
is a source admission API. Source/proof/control readback requires the full source and both ledgers.

## Supplemental clock and identity framing

ClockConfiguration version1 binds original Config1 hash, policy
`sandtable.combat.public-opening-clock.v2`, and original force-assignment budget30000. This
separate supplement does not claim Config1 selected v2. Its own configuration hash and original
parent configuration hash are distinct event bindings.

For domain D and canonical preimage P, digest is SHA256 over ASCII(D), one NUL byte, then P.
The inventory lists eight domains under `sandtable.combat.actual-round-entry.*.v1`:
source, base, configuration, opportunity, round, slot, receipt and empty-aa. Source/proof identity
is `arsrc.<digest>` / `sha256:<digest>`; opportunity `aopp.`, round `arnd.`, slot `aslt.`, receipt `arc.`.
Base/configuration/certificate hashes have `sha256:` prefix. No identity hashes itself.

Opportunity preimage order: baseHash, actualSelectionSourceHash, segmentId, cycleId,
positionId, candidate, declineReceiptId. Round preimage: baseHash, opportunityId,
openingAuthorityVersion, openingHistoryPrefix, timing. Slot preimage: roundId, role. Receipt
preimage is the complete ordered RoundEvent without final receiptId. It binds original campaign,
rules, original/supplemental configuration, original selection source, base/cycle/segment/round,
prior/new versions, prior prefix, independent input and derived effect. EventType is
`actual-round-` followed by its closed effect kind. Prefix folds full canonical event bytes,
including receipt, through the unchanged [Sequence prefix-event operation](combat-cycle-sequence-v1.md). Authority advances
once per accepted event; no-op and retry preserve current version/prefix.

## Admission and transitions

Replay all57 ordered dependency pins before any source/cache/readback/retry access. The first16
are the exact original frozen actual-selection inventory;41 additional consumed transitive
files are pinned separately, including actual-selection schema/fixture/oracle and Content-v7.
Physical bytes and import/read closure are verified before freezing literal fixtures. Empty-AA
checks the exact consumed Content bytes again at use; change/disappearance after a successful
preflight still rejects009, including warm-source recovery. This
supplement does not repair old source-pin failures or waive a blocked predecessor entry point.

Full actual-selection replay must yield20, stepIndex3, selected candidate, accepted decline,
three ordered step receipts, no cancellation, closed=false and seven selection events. Earlier
entry/selection cuts and all seven old fallback variants cannot open this family.

1. System `open-round` at20 creates Collecting21. It requires fresh admission enabled,
   trusted available UTC and checked deadline. Unavailable/null/overflow opening rejects with
   zero events; no opening fallback is fabricated. Both slots contain original UnitKey, sole
   infantry component and committedToe10, kind `full-close-assault`, in attacker/defender order.
2. Original slot owner `seal-choice` accepts exactly its full allocation once. Either causal
   order yields versions22 then23, Prepared after the second seal. Slot order stays role order.
3. System `complete-step` requires Prepared at FA and both retained seal receipts, null time
   and available confidence. It appends one FA completion at24, stepIndex4/Anti-Armor.
4. The same structural command at AA derives empty-AA evidence from pinned Content, original
   full current World/candidate, role-ordered full allocations and FA receipt. It appends one
   certified empty-AA completion at25, stepIndex5/Close Assault. Prepared remains open; five
   total step receipts. No positive CA completion is allowed.

Opening3000 defines immutable floor3000 and exclusive deadline33000. Both seal orders4000 then
3500 succeed. An opponent's private seal time never raises a shared floor. Validate shape,
context, owner, live own slot and exact allocation before clock. Available own UTC in
[floor,deadline) accepts. Missing/unavailable/below-opening time generates a System-authored
clock-unavailable cancellation, while retaining the supplied player actor in input/receipt
for retry identity. Deadline equality/after rejects player seal005; a bad/foreign proposal
cannot be rescued by clock fault. System expiry at equality/after cancels; live expiry no-ops.
Controller-unavailable cancels Collecting independently of time. Prepared callbacks no-op,
including unavailable callbacks; Prepared wins timer/fault races.

Cancel empty, attacker-only or defender-only Collecting states for deadline, unavailable
controller, unavailable confidence, null time or below-floor time. Retain any accepted seal audit.
Complete FA→AA→CA using the cancellation receipt and ordered previous-step receipts, no-attack
proofKind, null time and available confidence. Empty cancellation ends25; partial ends26.
StepIndex6, six total step receipts, closed=true and same-cycle Reserve Release successor.
Cancellation does not charge, refund, reseed or manufacture commitment/result evidence.

Exact canonical command+actor retry recovers the original event/receipt before current lifecycle,
clock and admissionEnabled gates, with current Control unchanged. Changed command/context or
actor cannot borrow a receipt. Invalid primitives reject before retry. Stale round timer and
callbacks outside Collecting no-op before lifecycle/capacity checks, after closed arms/context
and System actor. Fresh admission disabled rejects new opening007 but permits authenticated
historical replay, retries and Prepared/cancelled completion recovery.

## Empty-AA certificate and conservation

Certificate order: contractVersion, full contentHash, full worldHash, actual candidate,
role-ordered allocations, assignmentReceiptId, role-ordered componentIds. The derived digest is
embedded in the AA completion effect; readback recomputes it from authenticated source and
ledgers, not caller-supplied emptiness. Both static/live elements must have exactly one component,
class infantry, maximum/current TOE10, matching component IDs, candidate membership and UnitKey,
and full committedToe10. Foreign roles, changed strength/component/content/assignment or
locally re-signed borrowed receipts cannot authenticate the certificate.

Every positive, cancellation, no-op, retry or reject preserves the full original World and RNG:
CP0/1, ammo10, TOE10, cohesion/readiness/relationships/custody/replacement/future obligations,
seed1/cursor2. AttackHistory and TargetUses stay empty; commitmentId stays null. Seals, receipts
and cancellation are bookkeeping only. Causal arrays retain order and each step completes once.

## Error precedence

Family `CMB-ARE-001`–`009`: shape/type, primitive range, command version/tag/arms,
source/context/actor/allocation, time, replay/lifecycle/capacity/version/position,
unsupported continuation, canonical spelling, dependency. Invalid predecessor admission maps004;
malformed full new-source syntax remains001/002/008. Predecessor families are unchanged.

Order: dependencies009; primitive/closed syntax001/002; version/tag/arms003;
source/context/System-or-seat actor004; original command receipt lookup (wrong actor004);
stale timer no-op; lifecycle/capacity006; live round context004; structural version/position006;
own live slot/allocation004 (consumed slot006); clock005; unsupported007. Fresh opening disabled admission rejects007 after lifecycle/state checks and before its clock
check; it cannot rescue malformed/context-invalid input. Positive CA completion rejects007
after clock validation. The literal lifecycle/clock table and conflicting vectors in
the verifier freeze this order, including primitive overlays on every combined-error case.

## Recovery, privacy witness and verification

The cache holds immutable bytes keyed by full canonical source plus both complete canonical
ledgers;57 dependencies are checked before even warm lookup. Returned sources, Bases, Controls,
proofs and retry bytes are owned copies. Caller mutation never changes retained evidence; altered
caller histories must authenticate again. The private kernel accepts only derived facts, not an
independent recovery identity.

A test-only own witness includes owner, role, own allocation, own sealed flag, public opening and
deadline. It excludes opposite allocation/sealedAt/count, raw receipt, authority version/prefix
and source/proof hashes. Compare unsealed own witness and own action outcome with and without an
opposing seal for both roles, all time/confidence classes and malformed/foreign proposals. This
bounded declassifier test does not establish public outward/seat isolation; no private envelope
is projected into observation/intelligence/transport/Snapshot.

[Literal fixture](fixtures/combat-actual-round-entry-v1.json) contains34 original-owner traces:
four positive (both owners/orders), thirty cancelled (both owners, three partial states, five
causes). Each retains full source, separate trusted ledgers, complete event/control/proof bytes,
byte counts and hashes. Fixture generation is a one-time author operation outside the normal
oracle; normal execution never writes or regenerates fixtures.

Run `python3 -B docs/specs/verify-combat-actual-round-entry-v1.py`. `--semantic` checks profile
expectations; `--adversarial` runs semantic and adversarial acceptance without literal comparison.
The full normal check verifies immutable fixture bytes, every cut/suffix/retry/readback,
original entry/selection cuts and mutations, new event/input/Base/Control/proof leaves,
raw grammar, conservation, privacy, ownership, capacity, lifecycle/error matrices and all57
warm dependency/readback gates. Exact results/timings and RED/GREEN records live in the dated
handoff; unexecuted gates are never represented as passing.

Original Breakdown/cycle-sequence/inherited Snapshot source-pin failures and outward rejection
remain four separate failures. Longer completed checks supplement historical12/60s timeouts;
they do not rewrite those observations. No unrelated .NET gate or full native readiness is claimed.
Next authorized milestone after fresh contract review is REL-AUD-02C's separate native adapter.
Paid actual commitment/result and public activation remain subsequent reviewed gates.
