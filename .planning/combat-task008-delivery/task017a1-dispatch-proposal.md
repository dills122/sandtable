# Task017A1 typed Release base codec — proposed dispatch

Research-only manifest, 2026-09-20. **Not dispatched. Root may authorize only after016 acceptance.** Parent017 remains open. This artifact refines `task017a-kernel-feasibility.md`; its44-base reconstruction evidence remains inventory/setup research, not C# execution. No source/test/plan changes, .NET/oracle execution, fixture generation, commits or memory writes performed here.

## Outcome and exact ownership

Implement only immutable Release scope/history/exception/member/base values and a context-checked canonical ReleaseBase codec for named isolated probes. No lifecycle, ReleaseState, commands, effects, events, timing transitions, release identity generation, gameplay admission, World projection or World7 relaxation. No historical Result1 four-case claim. No generic serialization framework or registry.

Exactly five primary paths:

1. New `src/Cna.Core/Campaigns/CampaignCombatReserveReleaseModels.cs` — Release-owned immutable values, collection ownership and narrowly scoped expected-base context/value validation.
2. New `src/Cna.Core/Campaigns/CampaignCombatReserveReleaseCodec.cs` — ReleaseBase canonical writer and independently expected-base reader; private specific nested-value readers/writers only.
3. New `tests/Cna.Core.Tests/Campaigns/CombatReserveReleaseBaseTests.cs` — frozen isolated test-builder,44 base hashes, semantic/raw/context/ownership negatives.
4. Existing `tests/Cna.Core.Tests/Cna.Core.Tests.csproj` — link existing `../../docs/specs/fixtures/combat-reserve-release-v1.json` to `Campaigns/Fixtures/combat-reserve-release-v1.json`, copy PreserveNewest. Existing authority/Content/Result2 fixture links suffice.
5. Existing `docs/design/combat-cycle-implementation-plan.md` — **root-owned** child scope/status/evidence update. Worker does not edit plan.

Worker source/test ownership is paths1–4 only; not alone, preserve concurrent edits. Administrative worker/review evidence counted separately in total physical publication inventory. No edits to designation models/codec, Round2/Result2, World7, schema/oracle/fixtures or other project registrations. If implementation cannot fit cleanly, report and re-scope before adding files.

## Frozen ordered schema

Source: `docs/specs/combat-reserve-release-v1.schema.json`, external contracts through historical Result1/Round1/Steps/authority inventories. These five objects only; every property mandatory, null explicit, field order exact:

| Value | Canonical fields in order |
| --- | --- |
| ReleaseScope | `gameTurn:int operationStage:int playerPhaseSlot:id actingSide:side` |
| MovementException | `scope:ReleaseScope ordinal:int status:id completionReceiptId:id?` |
| ReleaseHistory | `scope:ReleaseScope designationReceiptId:id? conversionReceiptId:id? releasedType:id? releaseReceiptId:id? releaseCycle:int? cpaBasis:int? voluntaryCeiling:int? offensiveCommitmentId:id? nextMovement:MovementException?` |
| ReserveMember | `unit:UnitKey status:id baseCpa:int spentCp:Cp history:ReleaseHistory` |
| ReleaseBase | `contractVersion:int profile:id cycle:Authority firstActingSide:side positionId:id priorVersion:long priorPrefix:hash combatCompletionReceiptId:id retainedWorldHash:hash randomState:Random acceptedHighWater:utc? members:ReserveMember[] attackHistory:AttackHistory[]` |

External AttackHistory order: `commitmentId cycleId segmentId attacker defender targetLocationId gameTurn operationStage`. UnitKey order: `creationBinding originalSide elementId`; CP order numerator/denominator; Authority order already implemented by `CampaignCombatReserveCompletionCodec.WriteCycle`. Random uses current canonical contractVersion/algorithmId/seed/nextByteCursor order.

Primitive rules: closed exact fields, side axis/commonwealth, canonical stable IDs `[A-Za-z0-9][A-Za-z0-9._:-]{0,127}`, prefixed SHA256 `sha256:` plus64 lowercase hex; raw rules hash64 lowercase hex. Int signed32, long signed64, ulong0..2^64−1, nullable UTC0..253402300799999. Preserve distinctions between JSON integers and booleans/floats; do not silently normalize fractions/numbers. Bounds1MiB/frame, depth32, arrays512; members additionally32. No event cap enforcement needed inA1 because events absent.

Use compact ASCII canonical JSON, no BOM/trailing bytes/newline/alternate escaping/order. Reject duplicate/unknown/missing fields recursively. Exact serialization/readback parity catches noncanonical forms, but raw size/depth/type checks must precede use of independently trusted context. Current ID/hash helpers may be reused only where their accepted alphabet/length exactly matches frozen contract; do not assume helper name implies compatibility.

## Minimum semantic checks

Grounding: `verify-combat-reserve-release-v1.py:84–118` `validate_base`; `verify-combat-cycle-sequence-v1.py:124–135` identity validation. Preserve checks without implementing later actions:

- Release contractVersion1. A1 caller/reader admits **isolated-ledger only** as explicitly bounded probe profile; reject settled-empty-release/inherited profiles rather than treating them as verified sources. Later adapter scope can add independently derived profile admission without changing frozen bytes. No inference of World authenticity from profile/hash.
- Authority contractVersion1; turn1..111, stage1..3, ordinal1..Int32.MaxValue, openedAuthorityVersion≥1. Valid relative slot; actual acting side must match firstActingSide and slot. Position must be exact same-turn/stage/slot Reserve Release from official catalog; priorVersion≥openedAuthorityVersion and≤Int64.MaxValue. Do not hardcode first-player topology in runtime codec.
- Campaign/rules/setup/content/scenario/config fields equal independently retained creation request/context; own member creationBinding equals that request's binding. Context validation must not derive expected fields from incoming candidate JSON.
- Members max32, strictly canonical `(creationBinding, originalSide, elementId)` order, no duplicates; do not silently sort incoming candidate order. Every member originalSide equals acting side. Synthetic `.probe-2` keys remain test-probe values; A1 does not assert certified Content membership or real World coverage.
- CPA1..Int32.MaxValue; spent CP numerator≥0, denominator>0, gcd1. Do not constrain spent to ceiling: existing overspend is retained. Raw unreduced CP must reject even if reused CP constructor would reduce it.
- History scope equals cycle turn/stage/slot/actual side. Status none has no decision; I legal only ordinal1, II only later. I/II require designation; I has no conversion, II requires conversion.
- No releasedType: releaseReceipt/releaseCycle/cpaBasis/voluntaryCeiling/offensiveCommitment/nextMovement all null; status-none also has null designation/conversion. ReleasedType present: status none, type I/II, designation+release receipts nonnull,1≤releaseCycle<current ordinal, cpaBasis==member CPA, ceiling CPA for I or floor(CPA/2) for II. Conversion present iff released II. Released I cycle1; released II cycle>1.
- Historical released member's nextMovement required: same scope, ordinal=releaseCycle+1, status expired and completionReceipt nonnull. A1 base must reject pending exception. Model may represent enum/string pending for futureA2 only if base validation still rejects it; no need to introduce pending behavior now.
- Nonnull offensiveCommitment requires exactly one retained attack record with matching commitment, original attacker UnitKey and same turn/stage. Absence/duplicates/foreign attacker/scope reject. Release does not create/consume this field inA1. Validate all attack record primitives and max512, even when no link references them; do not invent new history sorting requirement beyond frozen schema.
- Null acceptedHighWater allowed; nonnull respects UTC range. No budget/deadline calculations inA1. PriorPrefix/World hash/CA receipt must be valid primitives and exactly equal independently expected base at readback; lexical validity alone never authenticates them. RNG values match expected base, no draws.

## Concrete API trust boundary and reuse

Suggested narrow shape (names adjustable, semantics fixed): `SerializeBase(typedBase, retainedCreationContext)` validates typed value/context then writes canonical bytes; `ReadBase(bytes, independentlyExpectedBase, retainedCreationContext)` checks raw syntax/bounds/canonical form, validates independently expected typed base/context, compares entire candidate bytes to serialization of expected base, then returns owned immutable expected value. Do not offer context-free `DeserializeBase(bytes)` that promotes arbitrary candidate into authority, or method whose only expected input is baseHash. Independently expected base is a caller responsibility; parameter documentation must state that supplying candidate-derived expected value is not authentication.

Owning caller constructs expected probe outside codec from frozen test recipe. Production constructor/codec is internal and dormant; it does not claim that a valid isolated probe is actual campaign state. A2 will replay suffix against retained typed base; real adapters later derive full expected base from independently authenticated predecessor history before invoking reader. No fixture names, JSON nodes or expected golden hashes stored inside production types.

Reuse existing immutable `CampaignCombatUnitKey`, `CampaignCombatCycleAuthority`, `CapabilityPointAmount`, `RandomStreamState`, `CampaignElementReserveStatus`, `CombatRoundAttackHistory`; `CampaignCombatReserveCompletionCodec.WriteCycle`, standard JSON writer, snapshot side/status/random helpers and hash primitives where accessible/canonical. Existing designation-only `CampaignCombatReserveHistory` remains unchanged. Add Release-owned immutable Scope/History/Exception/Member/Base types with copied read-only member/attack arrays. Do not reuse mutable caller arrays or expose array aliases. New private explicit writers/parsers are fine; no shared descriptor-engine refactor or generic framework to save lines.

If reader compares raw bytes to expected serialization without materializing candidate, still validate closed raw shape/type/bounds before trusted context handling; duplicate fields, oversized arrays and alternate numbers must not exploit JsonDocument last-value semantics. No need to build an arbitrary JSON authority model.

## Test builder and acceptance evidence

Test-only builder mirrors exact frozen `base_for:128–157` isolated branch, documented locally. Use current retained Created11 creation context/setup/config and RNG; Content7 own element IDs; typed cycle template from existing Result2 boundary. Previous research proved that template equals historical Round1 cycle fields. For each11 non-settled case×2 sides×2 slots: explicit synthetic overrides ordinal/cycle-openVersion20, priorVersion100, high-water1000, probe receipt vocabulary, original own key plus optional `.probe-2`, literal synthetic World-hash preimage, name/side/slot prefix preimage, and case CPA/spent/prior-release history. Official catalog supplies topology. Construct expected typed base independently of expected hash; serialize and compare44 retained golden base hashes.

Freeze Release fixture SHA256 `70ed683c21f95a511c06362765dbe0971e0bb024bfcee74be70865be70601af9`. Enumerate exactly44 isolated rows and exactly4 excluded historical rows, failing on unexpected family/count; do not silently skip unknown cases. No fixture regeneration or reading current output back as expected data. Detailed successful JSON-only reconstruction recipe/evidence retained in `task017a-kernel-feasibility.md`.

Meaningful RED: first frozen isolated `first-release` Axis/first-slot base cannot produce expected canonical hash through new codec; first absent symbol/compile failure may establish skeleton RED but retain behavioral RED after skeleton exists (missing/wrong field/order or rejected legal typed base). After implementation,44 hashes must match; context checked reader must roundtrip each original bytes into equal owned value and serialization. A1 has **zero executed transition/event/state-golden claim**.

Required focused negatives:

- Change each authority/creation/context binding, first-side/slot/acting combination, position, prior/opened version, prefix/World hash/CA receipt/RNG; even syntactically canonical, locally rehashed candidates reject against independently expected base.
- Typed constructor/serializer rejects semantic illegal status/history combinations, missing designation/conversion, release type/cycle/ceiling mismatch, pending/wrong-scope exception, offensive link missing/duplicate/foreign record, duplicate/unsorted/33 members and invalid CP. Positive structural32-member and512-attack shape bounds may be synthetic typed probes, explicitly not new golden family/campaign membership.513 arrays fail raw bound.
- Wrong contract/profile, malformed ID/hash/numeric boundary, bool/float-as-int, duplicate/missing/extra property, order swap, BOM/whitespace/newline/truncation, invalid UTF8/escaping, excessive size/depth and null combinations; no context lookup masks malformed payload rejection.
- Mutate supplied member/history/attack collections after construction; mutate returned byte buffers; prove original model/hash unchanged. Repeated serialization identical. Inspect immutable nested references and no mutable byte alias. Reused CP normalization cannot make unreduced raw input acceptable.

Use ordinary existing C# focused test infrastructure; worker later records exact failing and passing commands. Root owns full gate/dev review/independent acceptance per parent workflow. This research ran none.

## Size, downstream dependency and stop rules

Estimated Models120–220 lines; Codec220–350; BaseTests220–350; total560–920 plus fixture link/plan. Estimate not assurance; smaller than full kernel but raw semantic/ownership coverage still meaningful. Avoid expanding full contract into commands/state/effects “for later.” If model/codec duplication or fixture builder demands historical engine, stop and report scope issue.

A1 closure means44 independently built isolated bases canonical/semantic/context-safe in C#. A2 depends on acceptedA1 and owns explicit opening/queue/disposition/consumed conversion/fallback/retry/completion, owned ReleaseState/event codecs,132 event hashes/176 state hashes/2 literal terminal events plus timers/bounds. A2 manifest must independently fit/split; A1 does not pre-authorize it.

Parent017 still owes lifecycle, native Result2 empty Release bridge and positive creation-rooted3h→3i prerequisite/admission; later-II/consumed real campaign provenance remains explicitly bounded. Historical four Result1 cases stay historical contract evidence unless separately scoped compatibility work. No Snapshot/cumulative positive HistoryReplay/publication/runtime activation or Task018/019 execution claim.

**Next gate:** accepted016 → root authorizes this exactA1 manifest and implementation worker ownership. Research itself does not authorize edits. Stop afterA1 tests/evidence for root gate; no automaticA2 start.
