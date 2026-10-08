# Handoff: private native actual selection REVIEW_READY

Date: 2026-10-07 America/Toronto. Author handoff; not an independent Ready verdict.

## Objective And Boundary

REL-AUD-01 / S4 / Task019F1: dormant native exact-byte consumer for the frozen actual-selection
contract, two original actual owners, positive Force Assignment20 after defender decline19,
without FA completion; seven fallback variants per owner to Reserve Release. Full original
source/proof ownership, separate caller-trusted ledger, every cut/retry, closed grammar/capacity,
all16 admitted dependencies and systematic combined-error precedence. Five primary files only.

## Canonical Sources

- `docs/specs/combat-actual-selection-v1.md`, its schema, fixture and unchanged oracle.
- `docs/research/combat-actual-selection-bridge-feasibility.md`.
- `docs/work/plans/2026-10-05-actual-selection-dependency-disposition.md`.
- `docs/design/combat-cycle-implementation-plan.md`, including Task019F1.
- Applicable `AGENTS.md`; canonical repository facts override this operational handoff.

## Current Repository State

Repository/worktree: `/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable`. Branch: `codex/native-actual-selection`.
Base: `1dcbb5e6f27437ad17a98a732e31748d2b493fd7` (merged PR164).
Implementation/review head: `458e49229e29315ff13e52035d2a1b168b79a752`.
Implementation commit contains exactly the five primary paths below. Following administrative
commit adds only bootstrap/author/handoff; final branch head is returned in the task final message.
Verify clean status and implementation file hashes before review. No new worktree or automation.
Serena-generated untracked project metadata and symbol caches were removed before the freeze; no user files were
reverted. No PR, publication, CI or merge was attempted. Full-suite lease returns to coordinator.

## Completed Work And Evidence

All152 literal cuts/events/Controls/full proofs and original history match. Positive state20 has
stepIndex3 and accepted defender decline19; FA completion rejects007 after005 clock validation.
Fourteen fallback traces close the same release successor, stepIndex6/six ordered step receipts,
without defender decline, attack, resource effect or RNG draw. Original twelve entry receipts,
actual catalog activeSide=null, MovementEndProof, Weather receipt/full-event hashes, original
World and seed1/cursor2 persist. Separate trusted ledger is mandatory on replay/apply/readbacks;
embedded actors/time are evidence to compare, not authentication.

Semantic RED before native behavior: both original owner openings failed expected14/actual13.
Initial analyzer-only stub compile failure is not counted as semantic RED. Opening GREEN2 passed;
intermediate GREEN exposed Int32/Int64 JSON conversion, corrected through numeric access. Parity21,
precedence16 and exhaustive57 then passed. Focused range/dependency RED3 produced2fail/1pass:
UTC beyond Int64 yielded001 rather than002; missing dependency propagated KeyNotFoundException
rather than009. Bounded corrections produced GREEN3. No TDD claim is made for administrative prose
or later already-passing coverage assertions.

Native exhaustive coverage:4,456 locally re-signed event leaves,8,266 proof leaves and1,512
original Request/Created/history leaves;6,080 all-kind/state/clock mechanics and276 complete/close
public probes plus9,154 adjacent-gate comparisons; malformed raw spellings, private field smuggling,
owner/clock/capacity/family/ownership and every dependency before replay/retry/readback. Final retry
coverage is1,962 frozen variants plus654 availability-only inversions,2,616 checks across all cuts.
The initial full gate passed60 focused/81 Boundary/2,593 solution/format before that final retry
coverage improvement; its logs are retained under `initial-final-*`. Gates were repeated only
because final test bytes changed; expensive unchanged oracles were not repeated.

### Final required local gates

- `dotnet restore Sandtable.slnx '/bl:/private/tmp/s4-native-gates/final-restore-{}.binlog'`: PASS exit0, 1.21s.
- `dotnet build Sandtable.slnx --no-restore '/bl:/private/tmp/s4-native-gates/final-build-{}.binlog'`: PASS exit0, 10.753s, zero warnings/errors.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class Cna.Core.Tests.Campaigns.CombatActualSelectionTests '/bl:/private/tmp/s4-native-gates/final-focused-{}.binlog'`: PASS exit0, 246.171s, 60 tests, zero failed/skipped.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-trait Boundary=UserSpace '/bl:/private/tmp/s4-native-gates/final-boundary-{}.binlog'`: PASS exit0, 6.68s, 81 tests, zero failed/skipped.
- `dotnet test --solution Sandtable.slnx --no-build '/bl:/private/tmp/s4-native-gates/final-full-{}.binlog'`: PASS exit0, 686.267s, 2593 tests, zero failed/skipped.
- `dotnet format Sandtable.slnx --verify-no-changes --no-restore`: PASS exit0, 21.608s.

These are the required local gate components, not an executed `just check` claim. Native MTP/xUnit
v3, .NET SDK10.0.400 (global10.0.302 latestFeature), macOS arm64. No-build test commands still
produced unique MSBuild binlogs. No fixture regeneration or admission widening occurred.

### Unchanged contract and predecessor oracles

- `python3 -B docs/specs/verify-combat-actual-selection-v1.py`: PASS exit0, 222.432s.
- `python3 -B docs/specs/verify-combat-positive-entry-v1.py`: PASS exit0, 116.49s.
- `python3 -B docs/specs/verify-combat-selection-steps-v1.py`: PASS exit0, 3.603s.
- `python3 -B docs/specs/verify-combat-sealed-round-v2.py`: PASS exit0, 17.135s.
- `python3 -B docs/specs/verify-combat-result-settlement-v2.py`: PASS exit0, 89.089s.
- `python3 -B docs/specs/verify-combat-inherited-breakdown-completion-v1.py`: FAIL exit1, 0.373s.
- `python3 -B docs/specs/verify-combat-cycle-sequence-v1.py`: FAIL exit1, 0.049s.
- `python3 -B docs/specs/verify-combat-snapshot-composition-v1.py`: PASS exit0, 86.872s.
- `python3 -B docs/specs/verify-combat-outward-composition-v1.py`: FAIL exit1, 4.837s.
- `python3 -B docs/specs/verify-combat-inherited-snapshot-v1.py`: FAIL exit1 at recursive land-sequence source pin; retained independent log.

Actual-selection main passes all S3 counts, including152cuts/1,962retries/4,456event and8,266proof
mutations/1,512entry leaves/6,080state-clock/276public probes and128 dependency-before-cache probes.
Its SHA256 is recorded below. Original Breakdown and cycle-sequence fail the unchanged
`src/Cna.Core/Rules/Cna1979LandSequence.cs` source pin; inherited Snapshot fails that recursive pin.
Outward composition fails its unchanged original gate (the disposition's Content-document drift).
Separate synthetic Snapshot composition passes and does not waive inherited Snapshot failure.
Historical S1a12/60-second timeout observations remain unverified; later completed checks do not
rewrite them. No pin refresh or waiver.

### Immutable implementation file manifest

| Path | SHA256 |
| --- | --- |
| `src/Cna.Core/Campaigns/CampaignCombatActualSelection.cs` | `8a94b2d5f9ecd688a0eb5670bbed4408add89f488ccd070ed53361210b7f3721` |
| `src/Cna.Core/Campaigns/CampaignCombatActualSelectionCodec.cs` | `a8c858c202d8d8245fce4942d316060080f1b0158db9f9d4b5e78903aa9cfccd` |
| `tests/Cna.Core.Tests/Campaigns/CombatActualSelectionTests.cs` | `c8bb9f0dfbd00f276196b115d82879421b6f3bc072da5c3d98d2e73ec7abb905` |
| `tests/Cna.Core.Tests/Cna.Core.Tests.csproj` | `d536a627f2b398b72fd3a97d3a53c885d712027dcd13de699d1acc3f58512388` |
| `docs/design/combat-cycle-implementation-plan.md` | `bc344ddab834d756837952e9f3df749ff204e060fc497808aea217b1da2bc97c` |

All16 native embedded manifest entries equal the unchanged ordered inventory and current physical
bytes. Frozen specification/schema/fixture/oracle and all predecessors are byte-identical to base;
only the five primary paths and following three administrative packets are in the delivery.

### Retained evidence manifest

Evidence directory: `/private/tmp/s4-native-gates`. Hashes bind the executed logs/binlogs; preserve
these files for review. Raw fixed-input assertion diagnostics are not copied into repository prose.

| Evidence file | SHA256 |
| --- | --- |
| `actual-selection-oracle.log` | `0ec2ecc01734e319a6abd590a7bf1484cf5569d657874981e893a6c93d8cc874` |
| `adversarial-20261008-000923--16402--3BanTZ.binlog` | `ef94c867d58a1daf423a41bbd1654dc330e6d720f0361bb9b7f03dba22685eb0` |
| `adversarial-20261008-000925--16402--i+WUpW-dotnet-test.binlog` | `da8713fa7f58ef7ab095b92b33ce66e42c826f6f036c98844963fa67ae46a733` |
| `adversarial.log` | `33f0dc88c4a9d55db8f848ed173db0566c7b12786f114a114458b14dc249f26e` |
| `cycle-sequence-oracle.log` | `2b37c9479b23cf9201a18ce0fae319c542b09c20f23e85a7105be3cc474dbdcf` |
| `final-boundary-20261008-002056--22860--CJyoPT-dotnet-test.binlog` | `d588b94d2d63db8a6db9edca477c76f885b6a96f690624077f5dd574179d65ad` |
| `final-boundary-20261008-004401--34844--G9Spih-dotnet-test.binlog` | `098d99f6c2e707802f901d180325020d88f5321c97ec86ba402a64742559c392` |
| `final-boundary.log` | `51a96b7acc619d7b04850395819601b2761a1d6e502c79ad9a1150f36ffda281` |
| `final-build-20261008-001713--20375--SRj9mZ.binlog` | `b0196d14cb455f178abd4cb84cceb8235a7963fc4f035115d0580eab6f9c8abd` |
| `final-build-20261008-003944--31833--UyyjCH.binlog` | `23c2e05eff66c8a481bc4058447025cc9261803b74105d4e56c4e8117b8fadba` |
| `final-build.log` | `87a214ac7adab8c17babc86abbfed9f52ce279dd4339b6d8e9f37926adc4e757` |
| `final-focused-20261008-001718--20384--pmAqUS-dotnet-test.binlog` | `915251d32861a38a20dc243df6f867f1f22f8d70b7d62f5cfde7dc149e82485d` |
| `final-focused-20261008-003955--31911--aCCKVh-dotnet-test.binlog` | `14263d335ae5647929f3148137d7a3efb9bd561b7a65cf7aab07630ca4bd5500` |
| `final-focused.log` | `851848d1beeb8340e3c9d513b42833cc3d1af1199280e1cd6f442c28e1a028d1` |
| `final-format.log` | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` |
| `final-full-20261008-002103--22868--N6N+2K-dotnet-test.binlog` | `d3c1ea913d5e6a18e0b769e7cb8b548448a9ad4d7b01197b131df55cbc108eb2` |
| `final-full-20261008-004408--34932--9vfERm-dotnet-test.binlog` | `571f5239080e2e5f8bf0dab98c1c13cdbc73f2927dd982c798056b29eabf74e4` |
| `final-full.log` | `438fc95a663e3893ea92075ef0a44778ad93f3725cbe6304deb101f4e0ee5eea` |
| `final-gates.json` | `645347126458731dc2ae0ea091bd1e23f76065c6f903bde05b619bac388f8622` |
| `final-restore-20261008-001712--20365--_+xKWy.binlog` | `fba8276965d1115b9733a9a25f943b759dbcfb38e006cb542efeb583a5ad49ed` |
| `final-restore-20261008-003943--31823--wg37Mg.binlog` | `f0c5560ea62eb7724210dac1c18f6805fa369e77d9a2e7ca4119f609b06a37ac` |
| `final-restore.log` | `40d104602671164ab3958225da3e1c8cabd55f0251d6df6bc911d9d5a24f44ce` |
| `green-20261008-000002--12210--6FOa9s.binlog` | `154165b1d2ec8b2b02bff0807936a255972aef1e630b95224caef56fa672a8a8` |
| `green-20261008-000010--12210--2264K9-dotnet-test.binlog` | `d6c19a8c87b3e3c5b2063e836760b45cd5e70cc852601165118fec3c8a426b49` |
| `green.log` | `9c970eb5d799e0e59f5064af57825a2b6089a53715f804cf6edb9915cb05069f` |
| `inherited-breakdown-completion-oracle.log` | `53763fd8940f5a72d343f67ae7ef1c41175d0df64abfa414ecd532309674f748` |
| `inherited-snapshot-oracle.log` | `3dbdcdc29d0a06b51cb07e39075f7934ea519e393f4f301bf4293905c27c8952` |
| `initial-final-boundary.log` | `e1e655ff79770b6cd851e1ff900fd17357d0ed9a3a47d7f1139833131f640248` |
| `initial-final-build.log` | `5eb3427130da119026cef5dd478a91c838074881758298237a9d4e7a906674cb` |
| `initial-final-focused.log` | `530c32b53356c409650b19a91a5e6162b60ed6bc9279c1c825973997c57c79dc` |
| `initial-final-format.log` | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` |
| `initial-final-full.log` | `1b2c046e70805b35f8baa743e14c131a2793d71058f6e303de7f88aef5b65ac5` |
| `initial-final-gates.json` | `35e1fcbe1fd62097f64bc0d8f7e61b04ee92ce142333b1b284701246bfc23eb5` |
| `initial-final-restore.log` | `40d104602671164ab3958225da3e1c8cabd55f0251d6df6bc911d9d5a24f44ce` |
| `matrix-20261008-000423--14269--QrrLpx.binlog` | `fd3a76a59b398b9fc20620288a4dba0a2772694dce6cb49f74eefe5433b44c0e` |
| `matrix-20261008-000427--14269--fp13gk-dotnet-test.binlog` | `d4ad485c9f80e802e1abbef406c990e6234d23e642318f4ef28e9679d657c41e` |
| `matrix.log` | `aeb3a01ffe2fed50253f895ed0e634cc9f88de54b02a0a32c5137892084b0969` |
| `opening-green-20261008-000021--12392--AEgiyY.binlog` | `7aef3cb16d3ad4921d215768cedda6b6e81cc8b85800d820dc9d68fff1e4c9e8` |
| `opening-green-20261008-000026--12392--Xsremg-dotnet-test.binlog` | `52479b5f80d43b4e560395c9eabfa072de2b4adda64350692ab253f757a33111` |
| `opening-green.log` | `a735d9aa6254504f014bce88b0927d48922e5675a22f011556ce4c7e34bb6399` |
| `oracles.json` | `55d3cba6ddcbd331c5f4830a54e45102aad61ee451126588262d47d021eb1e62` |
| `outward-composition-oracle.log` | `5e02eac0416e1a906b8da65ad298bfdaa0bfbc5e4ae57d0627346748a799af36` |
| `parity-20261008-000234--13344--kj+UEK.binlog` | `b59ba42325584e4690a2cffae981ddd5edef2e402442fa0d507be88b4e9c2864` |
| `parity-20261008-000241--13344--zy8Cp1-dotnet-test.binlog` | `5a781b3f14fa09289da357ee4766bd0b9b2df3676b852c16d94750bb621e8f29` |
| `parity.log` | `89b78827188234b594f671369895f3e35c57b62a54f463d066214d1282bdd2ec` |
| `positive-entry-oracle.log` | `37988e248972aed56c4b2f9112a3cb8b55e94c023add34a1635e4e31b4359199` |
| `range-green-20261008-001516--19492--JbGnJB.binlog` | `4c3976c444aa50e00378d7c18cb7140535759d0f15e39bd897b9047153d9a472` |
| `range-green-20261008-001519--19492--ubdUd4-dotnet-test.binlog` | `59b62794d0939d3fefb41746b0e0bd0b0624e5da0113f121b811a42e6d5e07cf` |
| `range-green.log` | `081b2e737ea38de3724940d0a9f871174e96332674d1bb9b588e4a1687761f56` |
| `range-red-20261008-001351--18804--TNyLOb.binlog` | `b3a80b7d372941b108d3a366d062c53a09c25a562f63f2351775db62ec5fd48a` |
| `range-red-20261008-001353--18804--u5EU9y-dotnet-test.binlog` | `6a427b0ad491675f1e91f182127adb64618ebdc602706e32640109bdb36817c2` |
| `range-red.log` | `c45372c3c4855078c60f1c2f2ad4a4eb77c014a5253316e15edd86e8b7a1961e` |
| `red-20261007-235615--10429--vsiNem.binlog` | `52e88d79273fbdf20bf856c218186bd31ea3b48247ba3eaa86e738d9a69ee521` |
| `red.log` | `46d4ae77bab1ce2f25499cd4e28ee594063c81e4ef8090445e9e7975354ed04c` |
| `result-settlement-v2-oracle.log` | `2a034604581df3937100289a18e23f5b3270ba57d6ff345cb0f5c2e9f3b400bb` |
| `retry-scoped-format.log` | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` |
| `scoped-format.log` | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` |
| `sealed-round-v2-oracle.log` | `7497cdcac2a85a116e5b2eaaf43be2313d90522fddc1da852312b388eedd527c` |
| `selection-steps-oracle.log` | `a6d1fb28ec2d5487bfd23e5cd0c4dc208c4bda1147d027ad9ded9c0639777691` |
| `semantic-red-20261007-235637--10628--y94Q8o.binlog` | `8f9bc07080312a8911eb616db2a43ebb1763244ef027c36e3fffff75fe5cc6af` |
| `semantic-red-20261007-235647--10628--REZ8LI-dotnet-test.binlog` | `a5f0a8e271c8f22741cc85082aa200b62f072d95eae34b443ef3e009424883a5` |
| `semantic-red.log` | `4bd25501b97d70b155d8d644fc0334cc89a34fb18c8c9e1f98e7b13549f678b4` |
| `snapshot-composition-oracle.log` | `8b4f91220c348c27947067df340969ac204dc3a3183eb8c924283e51ecd5067a` |

## Decisions And Rationale

Keep full replay and fresh owned byte/projection copies; no cache can bypass dependency or ledger
checks. Derive full original provenance via unchanged positive-entry admission. Use distinct
actual-selection framing and private copied mechanics instead of widening old synthetic admission
or extracting a shared legacy kernel. Dependency lookup is a dormant caller boundary: future
production callers must provide retained in-memory bytes and perform acquisition I/O outside
turns. Seat/clock confidence and retained-store authentication remain caller activation gates.

## Blockers And Limitations

No blocker to author handoff. Independent review, coordinator reconciliation, requested publication
and exact-head CI/merge remain pending. Four historical oracle failures remain failures. No new
reviewer, Ready verdict, auth retry loop, public/host/AI/UI/transport, actual round/result/repeat,
later-II/consumed, full Snapshot/Archives restart/Maproom/Runner or parent017–019 closure. No all32
Result2 reachability claim or synthetic label promotion. No old runtime/spec/fixture/pin edit.
One automatic approval review timed out before executing an adversarial test-writing command;
the permitted single retry succeeded. It reported no safety finding and leaves no blocked action.
Graph generation was2026-10-05; new native paths had missing coverage. Focused source reads and
Serena supplied fallback; no whole-codebase graph completeness claim.

## Immediate Next Actions

1. Coordinator verifies base/implementation head/final admin-only head and clean file manifest.
2. Dispatch next fresh read-only S4 review from the neutral bootstrap. Proposed instance1of9,
   set1/pass1; coordinator owns actual counter/recovery. Review code/plan before author explanation.
3. Reconcile findings; bounded3sets×3 and coordinator research recovery between failed sets remain.
4. Publish only when requested, run exact-head CI, attach any resulting PR and merge only through
   coordinator authorization after independent review. No automatic publication/merge here.

## Verification Commands

Use exact commands above; add unique `/bl` for MSBuild calls and native MTP `--project`/`--solution`.
Do not run historical fixture regeneration, recursive pin refresh, broader runtime admission or
an uncoordinated full suite. Proportionate review checks can reuse the retained immutable evidence.

## Delivery Metadata

PR title: `Add private native actual combat selection`.
Summary: fully replay two original actual-entry sources against a separate trusted selection ledger;
match frozen source/event/control/proof bytes; stop positive at FA20; preserve seven fallback
variants per owner, ownership, grammar/clock/retry/error ordering and all16 frozen dependencies.

Neutral bootstrap: `docs/work/reviews/2026-10-07-native-actual-selection-bootstrap.md`.
Author explanation: `docs/work/reviews/2026-10-07-native-actual-selection-author.md`.
This handoff: `docs/work/handoffs/2026-10-07-native-actual-selection.md`.
No reviewer dispatched here, no independent count reset, no PR/CI/merge/automation/public admission.
