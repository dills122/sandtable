# Task011 proposed dispatch — research only

Parent010 must close through accepted010B and010C before implementation authorization. Research baseline:010B candidate762468e under review;010A accepted2eda2ed. This proposal changes no runtime, fixture, schema or task dependency. Own only this proposal during research. Canonical order stays009→010A→010B→010C→011→012. No actual positive-history admission, public DTO/revision, host publication or extended Snapshot12 claim.

## Scope and authority

Implement active sealed-round-v2 precommit mechanism: authenticated Base2, opening, two private role-ordered slots, seals, exact retry, cancellation, Prepared and structural continuation. Prepared may complete Force Assignment and certified empty Anti-Armor to step5; cannot complete Close Assault or execute commit-attack. Cancelled may complete all remaining steps and close at6. Every accepted transition preserves World, RNG, empty attackHistory/targetUses and null commitmentId. Task012 separately adds atomic debit/history/target use. Recognizing frozen commit syntax must not execute commit semantics or imply full Round2 support.

Governing sources: `docs/design/combat-cycle-implementation-plan.md:860` (011), `:891` (012), `:975` (cumulative dormant Core evidence); `docs/specs/combat-sealed-round-v2.md` (Base authentication, supplemental clock policy, immutable opening floor, retry and privacy sections); complete `combat-sealed-round-v2.schema.json`; `verify-combat-sealed-round-v2.py:149–177` (supplement/Base), `:180–212` (initial state/timing/gate), `:214–358` (transition), `:360–382` (authenticated readers/replay). Historical `verify-combat-sealed-round-v1.py:83–93` supplies only Base semantic checks; do not implement a v1 runtime or reshape active v2 state for v1 transition.

Independent authority remains trusted creation request + retained Created + independently trusted010B Boundary + separate predecessor input/event history. Boundary validation retains010B’s fixed compatible C2 creation-binding profile and independently validates Created against request. Synthetic C3a provenance remains explicit. Replayed event.input is never promoted into a trusted input. No caller Base, Control, World cache or supplied hash replaces replay.

## Exact five primary paths

1. New `src/Cna.Core/Campaigns/CampaignCombatSealedRoundModels.cs`: internal immutable Base2, ClockConfiguration2, Timing2, allocation/slot, command/input, state/effects/result models. Copy all collections and byte buffers at construction/exposure. Reuse existing UnitKey, World7, RNG, Receipt, CombatStepsBoundary/Control and Candidate models. Timing2 must be distinct from010B Timing1.
2. New `src/Cna.Core/Campaigns/CampaignCombatSealedRound.cs`: private transition; full predecessor authentication; history replay and fresh application; domain hashes; no debit behavior.
3. New `src/Cna.Core/Campaigns/CampaignCombatSealedRoundCodec.cs`: exact ordered v2 writers, closed local grammar, bounds, syntax-only input decoding, Base/state readback through actual replay. Include frozen future-effect grammar where needed, but reject unsupported commit semantically. No generic serialization framework.
4. Existing `src/Cna.Core/Campaigns/CampaignCombatSelectionStepsCodec.cs`: one narrowly named internal external-syntax writer bridge for frozen Boundary, Control, Receipt, World, Random, UnitKey and required scalar kinds. Delegate existing private grammar; no descriptor duplication and no changes to current010B bytes/array behavior. Existing World bridge already rejects Route/LegacyBrokenVehicleLot; retain that behavior.
5. New `tests/Cna.Core.Tests/Campaigns/CombatSealsTests.cs`: literal, clock/privacy, causal readback, negative and ownership tests. Existing project fixture link for Round2 from009B suffices; no csproj change.

Worker evidence is separate non-primary `.planning/combat-task008-delivery/task011-worker-evidence.md`. Root owns plan/docs/gates/reviews. If implementation needs a sixth primary path, stop for scope agreement. Expected engine size is substantial but localized: reuse authenticated010B and all external grammar; do not compress code to meet a line target.

## Proposed API and provenance contract

Names may follow established internal naming, but authority parameters and semantics are fixed:

```csharp
CombatRoundState ReplayTrustedBoundary(
    CampaignCombatCreationRequest request, ReadOnlySpan<byte> created,
    CombatStepsBoundary independentlyTrustedBoundary,
    IReadOnlyList<CombatStepsInput> predecessorInputs,
    IReadOnlyList<byte[]> predecessorEvents,
    IReadOnlyList<CombatRoundInput> acceptedInputs,
    IReadOnlyList<byte[]> acceptedEvents);

CombatRoundResult ApplyTrustedBoundary(
    CampaignCombatCreationRequest request, ReadOnlySpan<byte> created,
    CombatStepsBoundary independentlyTrustedBoundary,
    IReadOnlyList<CombatStepsInput> predecessorInputs,
    IReadOnlyList<byte[]> predecessorEvents,
    IReadOnlyList<CombatRoundInput> acceptedInputs,
    IReadOnlyList<byte[]> acceptedEvents,
    CombatRoundInput input, bool admissionEnabled);
```

Codec `ReadBase(bytes, request, created, boundary, predecessorInputs, predecessorEvents)` and `ReadState(bytes, request, created, boundary, predecessorInputs, predecessorEvents, acceptedInputs, acceptedEvents)` parse full closed grammar/canonical bytes before touching trusted context. `SerializeBase` constructs Base from the same independently authenticated predecessor tuple; `SerializeState` emits an owned projection, never authority for Apply. Input decoder is syntax-only. A separate public prior-state transition API is unnecessary and forbidden by this design. ReadEvent equivalence is enforced within replay against independently supplied acceptedInput, not against event.input.

Before any caller list Count/index, capture owned Created. Bounds-check raw bytes before cloning unbounded buffers. Capture bounded histories and immutable typed inputs/Boundary before causal use; maintain ownership even if list access mutates caller buffers. Authenticate predecessor via010B `ReplayTrustedBoundary`; derive canonical Boundary via existing validated writer. Required Control: selected outcome; non-null declineReceiptId; stepIndex3; exactly three stepReceipts; not closed; selection exactly candidate derived from validated Boundary. Compare complete Base2 bytes, including predecessor Control, to this reconstruction. These are the direct legacy.read_base semantics, with no legacy engine dependency.

For state World writing, reuse owned canonical World subtree from the authenticated typed Boundary serialization inside a private reconstructed context. This is serialization reuse, not accepting caller JSON as authority. World stays identical throughout011. Local receipts begin empty while version/prefix and three step receipts continue predecessor Control. Each Apply rebuilds predecessor and local history; no trusted mutable Base cache.

## Clock supplement and hashes

Read original force-assignment decision budget from independently validated original Config1; require supplement contractVersion2, original configuration hash, exact policy `sandtable.combat.public-opening-clock.v2`, exact budget. Retained budget30000 is golden evidence, not a replacement authority constant. ClockConfiguration hash uses `sandtable.combat.clock-configuration.v2`; Base uses `sandtable.combat.base-fragment.v2`. Round event configurationHash is supplemental hash; predecessorConfigurationHash remains original. Commands bind supplemental hash.

Reuse009B opportunity-v2 ordered preimage primitive with Base2 hash, cycleId, positionId, authenticated Candidate and decline receipt. Round preimage includes original opening authority version/prefix and immutable Timing2; slots derive roundId/role; receipt hashes unsigned complete event; prefix uses existing sequence fold. No new wire certificate/profile. Validate UTC0..253402300799999 and checked deadline arithmetic; signed64 versions and actual external UInt64 RNG primitive remain distinct.

Opening has its own trusted time/confidence:1999 is legal after predecessor RBA2001. Timing openedAt=floor; deadline=opening+budget. Never update floor after either private seal, and never borrow predecessor/private maximum time. Slot order attacker then defender, owners resolve from side/candidate. Allocation exact own UnitKey/component/full-close-assault10TOE; neither side chooses an opposite allocation.

## Ordering and transitions

1. Validate input closed types, command/context/version/segment/configuration, actor category, kind-specific nullable slots/allocation/expectedVersion/roundId shape. Hash Command only; authenticated actor checked separately.
2. Exact command digest already accepted: require same authenticated actor, return original receipt/event without append, before status/time gates. Invalid primitive time or confidence still rejects first. Changed consumed proposal fails own-slot validation.
3. Stale System expiry/unavailable roundId or non-Collecting status produces no-op. Otherwise require live state/round/base invariants and applicable expected version; enforce capacity/version overflow before fresh append.
4. Fresh open requires admissionEnabled and unopened step3, available valid time; construct immutable timing/two empty slots. Disabled admission affects only fresh open, not replaying retained open, retries or later structural recovery.
5. Seal requires Collecting step3, live own unconsumed slot and exact allocation before clock gate. Unavailable/null/below opening floor yields system-authored clock cancellation; receipt actor remains authenticated input actor. Available time before deadline seals; equality/after deadline rejects player input. First seal does not change opposite own-status/witness; second produces Prepared.
6. System expiry before deadline no-ops; unavailable controller cancels; expiry at/after deadline cancels. Prepared wins subsequent callback races. Cancellation retains previous seal data and pays nothing.
7. Structural completion requires null admittedAt and clockAvailable=true. Prepared proofs are role-ordered seal receipts; cancelled proof is cancellation receipt. Previous/from/to/proofs must match exact catalog position and prior receipt. Prepared advances3→4→5 only; cancelled3→4→5→6 closed. Commit-attack and positive CA completion explicitly reject pending012.

## Acceptance and failure-sensitive proof

Literal inventory independently audited from `docs/specs/fixtures/combat-sealed-round-v2.json`:10 cases; full fixture58events/68states. Exclude exactly four final effects whose kind is `attack-committed` (eventType `combat-attack-committed`), each at final index. Result54events/64states: four precommit prefixes20events/24states and six complete cancellation traces34events/40states. Validate all ten Base occurrences and separate predecessor histories. For every retained cut, serialize/read exact stored state bytes, including initial state; replay suffix to retained endpoint. Compare every event/state and embedded hashes with literal oracle bytes; expected outputs must not come from production transition. Four excluded events remain explicit012 rejection cases.

Audit reproduction: load fixture JSON; parse each event string; find first effect.kind == `attack-committed` or len(events); assert any excluded commit is final; sum cut and cut+1. Corrected result `cases10, precommit events54, states64, excluded final commits4, clock outcomes24`. Two initial read-only count attempts used wrong discriminator spelling (`attack-committed` as eventType, then `committed` as effect kind) and produced58/68; corrected discriminator above resolves inventory, no fixture changes.

Clock matrix follows v2 oracle clock-matrix checks and independent24 clockOutcomes: both sides×both first roles×24 clock inputs×two opposite-slot prefixes =192 fresh outcomes, plus96 exact retries. Assert own-visible witness equality across opposite unsealed/sealed prefixes, not equality of private full state/version/receipt. Cover opening3000/opposite4000/own3500; opening1999 after predecessor2001; exactdeadline33000; unavailable/belowfloor cancellation authorship vs receipt.actor. Include unchanged original seal timestamp/receipt after all retries. These are dormant mechanism privacy outcomes, not a production public-projection claim.

Negative tests must be failure-sensitive: malformed/duplicate/missing/unknown/reordered/escaped/whitespace syntax and nested invalid World/Control/Timing/slot primitive before null/sentry trusted context; depth32, root1MiB, array512, all named fields/types/unions and UInt64 RNG; no dotted-key exception. Valid canonical bytes with invalid context must reach authentication. Foreign/missing predecessor input, shortened events, legal other-side Base, changed supplement budget/parent hash/policy, whole rehashed state/event fields, floor/open/deadline tampering, reversed slots, receipt actors, wrong proof/order/positions and forged suffix must fail causal equality. Test malformed own allocation, wrong owner/slot/round/config and consumed slot before unavailable-clock cancellation. Never derive expected authentication from event.input.

Retry/restart at each retained cut: exact original command+actor returns original event even when fresh metadata is null/unavailable, after Prepared/cancelled/closed, and admission disabled. Mutated command/actor rejects; post-Prepared callbacks no-op; stale callbacks no-op. Disabled fresh open rejects while retained histories/structural continuations still replay. No costs/history/target use/RNG change on seals/cancellation/Prepared; reject commit and fabricated committed state. Add list-access mutation of caller Created and event buffers to prove captured ownership; returned collection/buffer mutation cannot affect later replay. Capacity tests should distinguish valid grammar from impossible causal history; do not invent a reachable16-event trace if this finite mechanism cannot generate one.

Before freeze audit all local v2 descriptors against schema and transitive bridge primitives (including explicit Route prohibition), not only field text. Focused010B shared regressions protect preserved array order and strict parse-before-context. Use meaningful TDD RED then focused GREEN, unique binlogs, scoped format and post-format focused run; root full gate/reviews afterward. No oracle/fixture regeneration.

## Feasibility and remaining limits

No architectural contradiction or blocker found. Five paths are feasible through typed010B replay and a narrow syntax bridge; no duplicate certification, World grammar, v1 engine or new framework needed. Parent010 acceptance is an explicit dispatch dependency, not technical implementation failure. Retained positive Boundary is independently trusted synthetic C3a only; future actual-history provenance adapter and public publication remain open. Parent011 closes only its exact precommit evidence, with012 debit and later settlement/gameplay/Snapshot obligations still open.
