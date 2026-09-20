# Task013 independent review

Review instance: 3 of 3.

## Findings

No actionable findings. Frozen Task013 code and bounded canonical plan satisfy reviewed initial→resolved acceptance criteria. No host publication or later settlement acceptance implied.

## Preliminary blind ledger

Persisted before reading author/checks packets. No prior reviews, aggregate evidence, worker/dev notes, proposal rationale, execution history, or CCE accessed.

Target: branch `codex/combat-task008-reaction-lifecycle`, base `f2c0046`, observed HEAD `1c514640b38e43abf72f1e9725ec8d7e7e8a8e8c`. All six `task013-source.sha256` pins pass. Source is committed; current README, roadmap, and tech-design diff included. Dirty execution/checks metadata excluded from blind pass.

- No actionable defect found in initial source/tests/canonical-plan inspection.
- Replay derives paid Round2 through trusted Boundary/C3a/Round inputs and events; full-byte comparison authenticates Result2 events/state. Event input is not authority.
- Result preserves ordered eight/conditional nine draws, rejection bytes, checked UInt64 cursor, and singleton pending settlement. Factory only bypasses legacy identity convention; paid-state/participant/receipt checks remain shared.
- Serializer compares typed World with reconstructed expected World before using privately owned paid bytes. Route permission is confined to Result2 profile; C3a and legacy vehicle exclusions remain intact.
- Evidence target is 32 literal resolve events and 64 initial/resolved state HASH cuts, not 64 literal state JSON fixtures. Retained corpus has no block crossing; separately rebuilt cursor30 case supplies crossing evidence.
- Candidate discard and retained retry prove Core semantics only. No host transaction, process restart, public activation, HistoryReplay, Snapshot, later settlement, or closure proof inferred.
- Plan Task013/Checkpoint F fits bounded implementation. Five material paths plus mechanical fixture link; later consequence gates remain separate.

At preliminary persistence, independent rebuild, focused resolution/shared regression runs, author-claim reconciliation, and final pin/process checks remained pending. Results below added only afterward.

## Code and plan review

- `CampaignCombatResolution.Replay` validates raw Result2 events before trusted history access, owns event/Created buffers, reconstructs accepted Task012 committed authority, and compares each complete regenerated event. `ReadState` validates syntax first and compares whole canonical bytes after causal replay. Separate trusted inputs prevent event-input self-authentication.
- `Transition` validates binding/actor/field combinations before duplicate recovery. Exact authenticated retries return original event bytes despite changed valid clock metadata; malformed primitives still reject. New resolution requires original version, System actor, null time, available confidence, and committed status. Later consequence commands reject; inactive timer commands cannot cancel or refund.
- `Resolve` uses existing selected tables and ordered purposes. Capture draw is conditional; rejected bytes are retained; overflow after partial local draws publishes no candidate. Both paid pre-loss elements remain intact. Core candidate holds result, cursor, World, receipt, version and prefix together; discard followed by reconstruction yields original authority.
- `WorldValue` checks full typed World equality against reconstructed pending World before serializing owned paid World bytes. Shared equality includes readiness, representations, causes, inventory and settlement. No unverified caller cache supplies Apply authority.
- Resolved-only factory derives settlement identity from exact commitment/result IDs and shares legacy participant, paid CP/ammunition/TOE/scope and receipt checks. Existing public constructor retains synthetic ID convention. Result2 grammar preserves semantic arrays and permits Route while C3a/default behavior remains unchanged; legacy broken-vehicle records remain forbidden.
- Task013 at `docs/design/combat-cycle-implementation-plan.md:900` requires selected result/cursor publication, pending consequences, deterministic retry and overflow handling. Implementation fits five material files plus mechanical fixture link. Shared selected-rule tests supply exhaustive table evidence beyond bounded 32 resolution traces. Tasks014–016 retain disposition, losses, retreat, custody and closure. README/roadmap correctly leave acceptance underway; tech-design explicitly limits atomicity to Core candidate semantics.

## Author-claim reconciliation

Author/checks packets read only after preliminary ledger persisted.

| Claim | Independent evidence | Status and consequence |
| --- | --- | --- |
| Frozen six paths, five material components | SHA-256 verification and base/HEAD path diff | Confirmed; root commit preserves pins. |
| 32 literal resolve events / 64 HASH cuts | `ThirtyTwoResolveEventsAndSixtyFourStateHashesMatchFrozenAuthority`, independent fixture count, focused run | Confirmed; hash cuts are not literal intermediate state JSON. |
| Eight/nine ordered draws, rejection retention, block crossing | Literal equality, draw tests, independent counts; authenticated rebuilt cursor30 test | Confirmed: 16 eight-draw, 16 nine-draw, 12 rejection traces, zero retained crossings; supplemental cursor30→38 covers crossing. |
| Overflow/discard/lost-reply retry preserve authority | UInt64 max/max-minus-three tests, discard/retry tests, code inspection | Confirmed for immutable Core candidate/replay. No transaction or actual process-restart proof. |
| Pending World only, unchanged paid facts | Typed World equality, null consequence assertions, shared World/commit tests | Confirmed. |
| Strict grammar, provenance, ownership and legacy isolation | Raw-before-context sentries, rehashed forgeries, returned-buffer mutation checks, C3a/legacy factory tests; shared regressions | Confirmed within bounded profile. |
| 18 descriptors | Independent 16 local inventory descriptors exact; inherited Timing compared with rules-input inventory; Receipt inspected against existing serialization | Consistent: 16 local plus Timing/Receipt. |
| Focused ten tests pass | Independent clean rebuild and ten-test run | Confirmed. |
| Root full suite 2311, Boundary81, full format, full Python oracle and remote CI pass | Checks packet only | Author-reported; not rerun or independently verified here. No readiness argument depends on converting these into reviewer-run evidence. |
| Full Result2 oracle includes later gates | Active spec/schema and oracle inspected | Confirmed; no C#014–016 coverage inferred. |

## Verification performed

All shell calls used `login:false`; no CCE tools/processes invoked. .NET verification used approved local IPC. SDK observed `10.0.400`, allowed by `10.0.302`/latestFeature selection. Native MTP, xUnit v3, net10.0/arm64. No `--disable-build-servers` used on `dotnet test`.

Exact rebuild:

```sh
dotnet build tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --no-incremental --disable-build-servers -p:UseSharedCompilation=false -m:1 '/bl:/tmp/task013-review3-build-{}.binlog'
```

Passed, zero warnings/errors, 8.75s. Binlog: `/tmp/task013-review3-build-20260920-120448--33073--bF7iBo.binlog`.

Exact focused run:

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatResolutionTests' '/bl:/tmp/task013-review3-focused-{}.binlog'
```

Passed 10/0/0, 12.468s. Binlog: `/tmp/task013-review3-focused-20260920-120512--33120--PLfUCO-dotnet-test.binlog`.

Exact shared regression run:

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatCommitTests' '*CombatSealsTests' '*CombatStepsTests' '*CombatIdentityTests' '*CombatWorldTests' '*CombatWorldInitialCodecTests' '*CombatSelectedRulesTests' '*RandomStreamTests' '/bl:/tmp/task013-review3-shared-{}.binlog'
```

Passed 103/0/0, 17.288s. Binlog: `/tmp/task013-review3-shared-20260920-120546--33141--GwVHbZ-dotnet-test.binlog`.

Additional reviewer checks: scoped `git diff --check f2c0046 --` across six source/test paths plus README/roadmap/tech-design passed; read-only Python `-B` descriptor/fixture audit confirmed exact 16 local schema descriptors, inherited Timing, counts above; six source checksums passed both before and after verification. HEAD remained `1c514640b38e43abf72f1e9725ec8d7e7e8a8e8c`.

Reviewed documentation SHA-256 pins remained unchanged:

```text
42bfeb1e7c0b1be85bba7ab1acc4791704fd63ef0d0091e9a1f41918678704f7  README.md
91f39969b7af1993fb99a68f46f684da939026525a3e9800c9a6e24a3c165e04  docs/roadmap/pre-alpha-roadmap.md
d949bd80dea61aba35011c08d0c0025b58c1b446b1330b126eaead8fe5af8dd4  tech-design.md
```

All three verification sessions exited. `dotnet build-server shutdown` succeeded. Final `ps -axo pid=,ppid=,comm= | rg 'dotnet|Cna.Core.Tests|VBCSCompiler|MSBuild'` returned no matches (exit1). Source/Git unchanged by reviewer; only own report written, besides authorized build/test artifacts.

## Open questions and residual risks

No blocking question for bounded Task013. Synthetic Boundary/C3a provenance remains separately trusted; actual positive campaign lineage, host durable publication, extended Snapshot12, HistoryReplay integration, public activation and later settlement remain open integration gates. No actual host transaction or restart experiment performed. Full solution/format/Boundary/oracle/remote CI evidence belongs to root; this review independently establishes rebuild and 113 selected tests only.

## Verdict

Ready.

## Recommended next actions

Return report to root for acceptance and budget ownership. Review instance 3 of 3 exhausted; no further review instance, fixes, commits, agents, or workstreams initiated.
