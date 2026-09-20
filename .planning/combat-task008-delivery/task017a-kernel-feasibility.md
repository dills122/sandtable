# Task017A kernel feasibility — bounded follow-up

Date2026-09-20. Decision owner: root coordinator. Research only; Task017 remains open and no implementation before016 acceptance. Own artifact only. No source/test/plan edits, .NET/oracle execution, fixture generation, commits, agents, memory writes or reviews. Current files may contain concurrent016 edits; inspected existing type restrictions remain as stated below. Timebox10minutes.

## Answer

**Not all48/140/188 through current typed runtime and a small fixture builder.**44 isolated traces are directly reconstructible without historical Result1 runtime. Four historical settled guard/escape traces require old Result1/Round1 source identity, World/prefix/receipts and both-owner lineage; current Result2 is not byte-compatible. Claiming all48 in initial five-file kernel scope would conceal significant historical test adapter work or trust arbitrary injected base fields.

**Recommend revised017A-isolated target:44 bases /132 event hashes /176 state hashes+lengths /2 literal terminal events, covering11 first/later/empty/history cases across both sides and both slots.** Keep four historical settled cases (8 event hashes/12 state hashes/1 terminal literal) explicitly excluded. Native Result2 settled-empty coverage belongs to017B's64 native event literals after016 acceptance. Neither completes parent017's actual positive lineage.

## Reproduced evidence

Read current source and frozen fixture/oracle text; Python standard-library JSON/hash calculations only, no oracle imports. Exact retained fixture hash remains `70ed683c21f95a511c06362765dbe0971e0bb024bfcee74be70865be70601af9` for `docs/specs/fixtures/combat-reserve-release-v1.json`.

**Observation:** filtered golden rows by case `settledCase` presence:44 isolated /4 historical. Isolated totals132 `eventHashes`,176 `frames`,2 `terminalEvent`; historical totals8/12/1. No fixture regenerated.

**Observation:** independently reconstructed every44 isolated ReleaseBase in memory from retained current authority Created11, Content7 element-side map, current Result2 boundary cycle and frozen `base_for` recipe. SHA256 matched all44 retained `baseHash` values. No Release transition executed; this proves test setup identity feasibility, not C# serializer/kernel correctness.

Recipe inputs:

- `combat-authority-envelope-v1.json` → `goldens.created.canonicalUtf8` → creationBinding, request RNG and canonical setup/configuration.
- `combat-content-v7.canonical.json` → own element by `sideId`.
- `combat-result-settlement-v2.json` first trace → `baseCanonicalUtf8.boundary.cycle`. Compared with `combat-sealed-round-v1.json` baseGolden boundary cycle: exact field equality observed.
- `combat-reserve-release-v1.json` cases/goldens → name, status list, ordinal, side, slot, actions and expectations.
- Frozen `verify-combat-reserve-release-v1.py:128–157` recipe sets openedAuthorityVersion20, priorVersion100, high-water1000, fixed probe completion, synthetic World-digest string, name/side/slot-derived prefix, optional `.probe-2` member, designation/conversion/prior-release receipt vocabulary. These are named test probes, not admitted gameplay history.
- Official `Cna1979LandSequence.CreateTurn(1)` should supply first/second-slot Reserve Release position in C#; read source lines32–33/98 confirms selected two positions. Research hash calculation used matching exact position strings only.

Canonical test recipe excludes self-reference: calculate base bytes first, compare retained base hash, then calculate kernel IDs/events/state; never install retained hash as authority. Full48 fixture remains unchanged and excluded4 are counted explicitly, not silently skipped.

## Current types and test-builder fit

**Documented facts:** `CampaignCombatUnitKey` validates stable identity/side and permits probe member IDs; it does not authenticate Content membership. `CampaignCombatCycleAuthority` is positional immutable record. `CapabilityPointAmount`, `RandomStreamState`, `CampaignElementReserveStatus`, `CombatStepsTiming`, `CampaignOpeningPreambleReceipt`, cycle framing/hash helpers and fixed topology already exist. `CampaignCombatReserveHistory` is designation-only and earlier codec writes later fields null.

**Inference:** isolated kernel needs new immutable Release history/member/exception/state/effect values but no World7 instance. Native Release contract deliberately stores `retainedWorldHash`; its isolated profile hashes literal synthetic bytes rather than a World. Forcing synthetic multi-member/CPA9 probes into certified two-element World7 would be incorrect and unnecessary. Existing types can represent their key/cycle/CP/RNG/timing ingredients without weakening World7 constructors.

Existing `CombatSealsTests.Case` builds typed creation/setup/configuration from retained Created11 and current catalogue, then separately trusted positive boundary. Reusing it merely to get request/cycle would unnecessarily replay/parse unrelated Round2 inputs. Minimal test helper should copy its short creation-context construction recipe (or use existing shared creation helper if found), read exact typed cycle fields from retained boundary, then build isolated test values from named case. No new production fixture-name switch, JSON-backed authority constructor or generic state-machine framework. Existing csproj already links authority/Content/Result2 fixtures; add only Release fixture link for narrowedA. Source hashes/schema may be checked through repository-relative source verification in root gate; do not require production assembly to load Python.

Minimal boundary: dormant **internal** Release-specific kernel `Transition` plus raw codec/replay taking an independently supplied owned typed context. No public gameplay admission factory from raw JSON/hash. Explicit probe context constructed in test assembly from deterministic named fixture recipe; every persisted-state reader replays from that separately retained context plus complete input/event suffix and compares exact canonical bytes. A raw candidate base must equal independently constructed expected base in probe tests. Real adapters later construct context only after authenticated predecessor replay and full membership/World checks. A generic `ReadBase(bytes)` that validates self-consistency is not enough; no inherited actual-world claim from internal context validity.

Strict target remains contractVersion1, closed `combat-reserve-release-v1.schema.json`, `sandtable.combat.reserve-release.v1` identity and `sandtable.combat.reserve-release-receipt.v1` receipt. All arrays/bytes/history owned;34-event limit independent of Result2's32. Actor and field checks before retry; immutable accepted budget/deadline and high-water; original actor retained through System fallback. Clock privacy means no owner choices influence another unrelated budget; isolated high-water1000 is explicit probe input. Actual016→Release starts independently at null high-water per frozen native bridge, not inherited Result2 audit time.

## Why historical four do not fit cheap reuse

`verify-combat-reserve-release-v1.py:120–125` calls historical Result1 `trace_case` for defender-capture-guard/escape then changes attackerSide for both owners. It requires source-derived cycle, World hash, RNG, final state version/prefix, actual CA receipt, accepted high-water and Round1 attack history. `verify-combat-result-settlement-v1.py:71–95` rebuilds selection/Round1 before result replay; simply reading expected Release base hash cannot recover those values.

**Observed:** retained Result1 fixture has eight complete final state strings/event strings, all original cases attackerSide Axis. Commonwealth counterparts are produced by oracle replay and not full literals in that fixture. Current `CombatResolutionTests.Case`/`CombatSealsTests.Case` consume Result2/Round2, not historical Result1/Round1. Replacing v1 by v2 changes prefix and World receipt-bearing collections despite equal visible gameplay values/version.

For defender-capture-guard, retained v1 settlement ID is `set.e06ba57b78817d612579649e0100d3f532bb401f1e8e5d000eee60140b7f84c6`; recomputing current v2 domain over same commitment/result gives `set.a4ffbb7aa3d09d4c7b475dd6e98f70c18f66921c71d19392ea9bd2080362ac04`. Legacy public settlement constructor instead demands `.commit`/`.result` suffix IDs; retained hashed v1 occurrence matches neither path. Current typed World7 cannot simply deserialize this historical settlement through accepted constructors. No constructor relaxation justified for testing.

Both guard and escape v1/v2 World comparisons differ in settlement/custody/relationship and guard-or-entitlement/future-obligation receipt IDs; prefixes also differ. Config digest and cycle template match, so incompatibility is source occurrence/clock/event protocol, not missing content/config setup. Historical initial fields can be represented as opaque **test-only retained artifacts**, but full48 causal reconstruction would require dedicated historical source adapter/port or separately retained authenticated historical bases. Neither exists in inspected helpers. Parsing Axis retained final JSON alone would only cover half historical rows and would not authenticate full predecessor/attack history. No blind side substitution or extracting convenient hashes from expected outputs.

## Scope, size and recommended gate

Five primary paths remain conceivable for narrowedA: new `CampaignCombatReserveRelease.cs` with compact nested immutable Release-specific models/kernel; new `CampaignCombatReserveReleaseCodec.cs`; new `CombatReserveReleaseTests.cs`; test csproj; canonical plan (root-owned). This is medium work, not quick completion based on file count. Estimate only: kernel/models roughly350–550 lines; codec250–400; focused tests/builders350–600, total about950–1550 plus link/plan. Reference current Resolution384 lines, codec382, Custody tests280; native Release oracle466 includes kernel and substantial validation/tests. Estimates not implementation promises.

Nested models are reasonable only while ownership/readability remain clear. Including historical Result1 replay inside same tests or making codec generic to save a physical file would be forced complexity. If narrowedA exceeds one coherent reviewable packet, split before edits into typed history/base codec with isolated base44-hash tests, then lifecycle/replay kernel with132/176 event-state evidence; parentA and017 stay open between slices. Do not waive complete mandatory fallback/retry tests to satisfy nominal five-file estimate.

Required meaningful RED: next first-I release or conversion after explicit opening cannot execute; verify fallback drain, converted member cannot be revisited, later bulk retention and empty explicit completion. Base-only44 hash setup success is useful prerequisite but not behavior RED. FinalA evidence must compare132 exact event hashes,176 state hashes/lengths and2 full terminal event literals, with separate semantic assertions, all-cut discard/readback, postcompletion retry, stale callbacks, deadlines/clock-loss ordering, CPA/history/offensive/exception forgeries, raw/owned-buffer checks. No .NET evidence claimed by this research.

**Next decision:** after016 acceptance, root chooses narrowed017A isolated packet only if remaining session accommodates full implementation/gates; otherwise defer. Do not dispatch original all48 assertion scope.017B supplies current native Result2 empty composition; historical four may stay explicit historical-contract-only evidence unless separately scoped historical adapter need arises. Missing positive3h/3i and later authenticated campaign lineage remain parent017 blockers; no production implementation authorized here.
