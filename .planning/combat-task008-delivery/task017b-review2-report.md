# Task017B independent review

Review instance: 2 of3.

## Findings

No actionable findings. Review covers frozen `201395c3e3faf06735424a8bc70658a2af75f081..3c9aad3b71a1eed1f862bfdd4e286e8d8f5ffd67`, inspected in clean detached shared clone `/tmp/sandtable-task017b-review2`. Integration branch `codex/combat-reserve-release-lifecycle` identified by bootstrap; clone HEAD independently confirmed. No source fixes, commits, CCE access, prior-review reads, subagents or extra workstreams. Blind preliminary ledger saved before reading author explanation.

`CampaignCombatResultRelease.Derive` authenticates native Selection/Round2/Result2 history before deriving Release authority. Closure and immediate-settlement requirements, named source signature, accepted effect reason/author/payload checks, and narrow suffix restrictions match canonical native bridge contract. Canonical World stays in retained Result state; Release receives exact version/prefix/CA receipt/World hash/RNG/attack history and actual selected acting member. Empty pending work does not drop member or future duties. ReadBase/ReadState derive independent authority after raw syntax checks. Existing Release replay supplies accepted transitions and duplicate semantics.

Initial concerns resolved through inspection:
- `CombatResolutionContext.CommittedHash` hashes canonical Round2 state. State binds `BaseHash` and event-prefix/receipts; `SerializeBase` contains canonical boundary, complete Selection control and clock configuration. Selection and Round2 replay require exact accepted input/event pairs. No alternate unbound typed predecessor metadata found.
- Source owns all input lists and event arrays; byte getters copy. Nested commands/allocations/unit keys are immutable records; boundary World owns read-only copied collections; retained creation context validates and reconstructs setup/configuration. Existing source/output mutation checks passed.
- Codec profile extension preserves isolated-ledger path and constrains settled profile to singleton none member with empty scoped history and null high-water. Adapter independently supplies pinned CPA10 and source-bound identity.

## Plan Review

Task017B refinement and exact manifest align with delta: new adapter, narrow codec change, new tests, existing fixture link, plan, project maps and delivery records. No fixture/schema/oracle regeneration or unrelated source changes. Dependencies use existing native Result2 and accepted Release lifecycle. Plan leaves parent017, positive Reserve provenance, Movement/cycle advancement, public admission, Snapshot and publication open. Synthetic upstream boundary and pinned CPA10 explicitly documented.

Acceptance evidence matches bounded objective: 32 native base literals, 64 native event literals, 96 independently replayed native cuts. These cuts establish replay/readback and invariants, not independent frozen native state hashes. Full wrapper/certificate/CycleControl acceptance remains outside this implementation. No rollout/migration requirement introduced by dormant internal adapter; runtime activation remains separately gated. Planned remaining reviews/full build/CI are root delivery gates, not inferred from this report.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Full replay plus committed hash pins authenticates 32 sources | Adapter `Derive`/`Supported`; Result2 replay and `CombatResolutionContext`; Round2 base/state serializers; Selection event prefix/receipt construction | Confirmed | Hash catalogue is bounded compatibility admission, not arbitrary source trust. |
| Reliable Result2 retiming allowed; source fallback rejected even with same World | `ReliableSourceRetimingPreservesAuditAndStartsIndependentEmptyReleaseClock`, `AuthenticatedSourceFallbackRejectsEvenWhenSettledWorldMatchesOwnerChoice`, oracle source checks | Confirmed | 24 retimed source audits accepted; 16 custody and 8 opening fallback variants rejected in native tests. |
| 32 bases/64 events/96 cuts preserve all settled World bytes | `ThirtyTwoNativeBasesSixtyFourEventsAndNinetySixReplayCutsPreserveSettledWorld`; existing frozen fixture link; passing focused suite | Confirmed | Literal native evidence and native replay cuts remain distinct from wrapper hashes. |
| No mutable cached authority | Source constructors/getters, domain models, `RawSyntaxRejectsBeforeMissingSourceAndSourceCollectionsAreOwned`; adapter has no cache | Confirmed | Repeated replay costs CPU but introduces no runtime performance promise. |
| Wrong raw/base/state/source/suffix and post-completion progress reject | Raw-first, leaf tamper, upstream forgery, reverse/extra suffix, invalid actor/version/clock and post-completion tests | Confirmed | Readback and retry boundaries exercised. |
| Scope excludes positive lineage and cycle/public work | Plan refinement, dispatch, README, tech-design, naming, roadmap and source diff | Confirmed | Ready verdict applies only to Task017B bounded native adapter. |
| Author RED/full gate execution history | Author narrative only; not independently rerun as historical RED or full gates | Unverified historical claim | No readiness credit assigned; independent checks below establish current focused behavior. |

## Verification Performed

All commands run from `/tmp/sandtable-task017b-review2` with `login:false`.

- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class Cna.Core.Tests.Campaigns.CombatResultReleaseTests --filter-class Cna.Core.Tests.Campaigns.CombatReserveReleaseTests --filter-class Cna.Core.Tests.Campaigns.CombatReserveReleaseBaseTests`: exit0; 23 passed, 0 failed/skipped; duration23.224s. Log `/tmp/task017b-review2-tests-exact.log`. Earlier build-enabled focused invocation built required artifacts before test-selection correction.
- `python3 -B docs/specs/verify-combat-result-cycle-finish-v1.py`: exit0. Frozen evidence14pins/5tamper; all-lineage32traces/160cuts/128retries/8guards/8entitlements; authentication31; commands/events/states85; raw84; clock17; prior-time192comparisons/24changed audits; cache2; native frames24; structured order2; source semantics24rejections/12same-World fallback cases. Log `/tmp/task017b-review2-oracle.log`. Oracle covers broader existing contract; not counted as implemented CycleControl runtime evidence.
- `git diff --check 201395c3e3faf06735424a8bc70658a2af75f081..3c9aad3b71a1eed1f862bfdd4e286e8d8f5ffd67`: exit0, no whitespace errors.
- Final `git status --short`: empty; `git rev-parse HEAD`: exact frozen candidate.

Non-passing setup attempts retained transparently: initial sandbox runner aborted on IPC bind (`/tmp/task017b-review2-tests.log`); approved wildcard-filter retry selected zero tests, exit5 (`/tmp/task017b-review2-tests-escalated.log`); help probe using `-- --help` aborted with SDK unexpected help-message error, exit134 (`/tmp/task017b-review2-help.log`). Correct exact-class invocation then ran and passed all23 intended tests. No global build-server shutdown performed.

## Open Questions And Residual Risks

No blocking open question. Catalogue intentionally rejects unsupported source lineage; future positive Reserve support requires separate policy/design. Full replay cost not benchmarked for public runtime. Full solution build/test/format and candidate CI not independently executed in review2; reviewer3/root own those declared gates. Native recovery-cut expected bytes are replay-derived rather than frozen independent native state hashes; documentation accurately limits claim.

## Verdict

Ready — bounded Task017B implementation and plan consistent; independent focused checks passed. This verdict does not close parent017 or authorize public/cycle activation.

## Recommended Next Actions

Return report to root; retain source freeze through remaining declared review3/full rebuild and exact-candidate delivery checks. No remediation or plan pivot required.
