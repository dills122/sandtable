# Task015 independent review

Review instance: 3 of 3.

## Findings

No actionable findings in immutable `d00a230..00a8b68c580a7efc38070571750c9a973e0fa579` Task015 scope. No source changes made.

Independent clone `/tmp/sandtable-task015-review3-1825` detached at candidate; seven source/test SHA256 pins pass. `git diff 00a8b68 ba54efd -- src tests` empty. Full diff comprises four production files, new custody tests, two narrow predecessor-test changes, three project documents and six delivery-evidence files. No unexpected runtime family, schema or fixture changes found.

Blind review recorded in `task015-review3-preliminary.md` before author explanation and worker/check evidence. No CCE, memory, prior review reports, inherited implementation conversation, or additional agents used.

## Plan Review

Task015 matches bounded implementation: positive captured allocation opens captor-owned custody; guarded relocation transfers exactly one donor TOE with current CP/Cohesion/readiness/provenance; escape retains original source and twelve-stage replacement entitlement without immediate reunion. Both branches retain required future obligation. Replay derives complete effect from authenticated predecessor/round/result inputs and rejects duplicate or forged publication.

Canonical requirements inspected first: combined plan Task015/checkpoint G, Result2 specification/schema/oracle, World settlement specification, custody and predecessor tests. Implementation then checked against those requirements and surrounding certification, World7 invariants, and sealed-round/boundary APIs.

Initial concern about path validation discharged: `CampaignCombatCertification.CertifyInitialProfileFacts` demands initial typed World equality except current CP; `ProveSupport` checks both participants, Clear line geometry and supported outcomes before commitment. Custody's shortest path and enemy-position checks do not claim support for arbitrary hidden or later worlds. Singleton lot indexing and donor-backed canonical resource encoding are justified by this admitted profile and full typed projection equality.

Seven physical source/test paths exceed Task015's rough 3–5-file estimate, but only five materially change; two predecessor tests update future-family cutoffs. No unjustified scope expansion. Core authority remains in existing modules; no transport/provider/host behavior added.

Checkpoint G is not complete: Task016 relationships/round/CA closure remains explicitly rejected. Actual positive creation-rooted provenance, Snapshot successor, public activation, HOST-PUB-001 durable publication, and future duty execution remain open. Retained-prefix readback is restart-equivalent Core replay, not a process crash or durable transaction test. Candidate plan's pending status is administrative; acceptance should be recorded only by initiating task after reviewing evidence.

## Author-Claim Reconciliation

| Claim | Independent evidence | Status / consequence |
| --- | --- | --- |
| Captor ownership, local deadline, earlier audit isolation | Resolution transition plus custody clock/ownership tests | Confirmed; 200 paired clock comparisons execute in passing suite. |
| Guard TOE/resources/provenance conserved; escape gives no immediate TOE | LossRetreat projection, World7 checks, conservation test | Confirmed, including four CP11 guards. |
| 176 literal events / 208 state-hash cuts; eight guard and eight escape branches | New literal test plus independently executed suite | Confirmed; counts describe assertions, not separate test cases. |
| Exact retries and rehashed forgeries cannot replace authority | Replay compares exact recomputed event bytes; cut/retry/raw/forgery tests | Confirmed within trusted synthetic boundary model. |
| Canonical donor fields safe after typed equality | Codec WorldValue checks full expected World before constructing resource nodes | Confirmed in admitted profile. |
| Maximum geometry and late eligibility covered broadly | New literal routes cover short guard paths and zero-edge escape; shared certification/World tests cover bounds/calendar | Confirmed limitation; no claim of maximum-route runtime fixture or turn111 custody replay. |
| Prior full suite/format/CI passed after earlier pending notes | Resumed integration checks retain 2,324/full, 81/boundary and successful candidate CI observations | Retained testimony only; historical logs unavailable. Independent rebuild and focused/boundary results below carry this verdict; no fresh full-suite/CI claim. |

## Verification Performed

All commands below executed with `login:false` in independent clone unless stated. Native MTP syntax used. Restore/build/test required approved local IPC/package-cache escalation. First sandbox restore produced no progress, was cancelled and its identified process terminated; no pass attributed to that attempt.

```sh
dotnet restore Sandtable.slnx -bl:/tmp/task015-review3-restore.binlog
```

Sandbox attempt stalled; cancelled. Successful replacement:

```sh
dotnet restore Sandtable.slnx -bl:/tmp/task015-review3-restore-escalated.binlog
dotnet build Sandtable.slnx --no-restore --disable-build-servers -m:1 -bl:/tmp/task015-review3-build.binlog
```

Restore exit0, all11 projects restored. Full independent build exit0, zero warnings/errors, 21.83s. Fresh clone had no prebuilt artifacts.

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatCustodyTests' --filter-class '*CombatLossRetreatTests' --filter-class '*CombatResolutionTests' --filter-class '*CombatWorldTests' --filter-class '*CombatIdentityTests' --filter-class '*CombatStepsTests' --filter-class '*CombatSealsTests' --filter-class '*CombatCommitTests' --filter-class '*CombatSelectedRulesTests' --filter-class '*RandomStreamTests' '-bl:/tmp/task015-review3-focused-{}.binlog' > /tmp/task015-review3-focused.log 2>&1
```

Exit0: 123 passed, zero failed/skipped, 43.561s; includes all23 resolution/loss/retreat/custody tests plus100 shared rule/RNG/World/identity/steps/seals/commit tests.

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-trait 'Boundary=UserSpace' '-bl:/tmp/task015-review3-boundary-{}.binlog' > /tmp/task015-review3-boundary.log 2>&1
```

Exit0: 81 passed, zero failed/skipped, 9.479s. All owned restore/build/test sessions completed; process inventory checked after tests, no review-owned process remains.

```sh
git diff --check d00a230..HEAD
git diff 00a8b68 ba54efd -- src tests
shasum -a 256 -c .planning/combat-task008-delivery/task015-source.sha256
git status --porcelain
```

Diff checks exit0/empty; all seven pins OK; source clone clean. Build/log artifacts remain only in clone or `/tmp`; integration edits limited to this report and blind ledger.

## Open Questions And Residual Risks

No unresolved in-scope correctness concern found. Full solution tests, full format, Python oracles, remote CI and actual process restart were not independently rerun in this instance. Existing fixture/schema files unchanged; focused C# verification tests frozen literal behavior. Dormant synthetic-positive boundary and explicit activation exclusions materially limit what readiness means.

## Verdict

**Ready** for bounded dormant Task015 custody implementation. Independent source review, full rebuild and focused behavioral evidence support implementation and plan; verdict does not accept Task016, complete checkpoint G, or authorize public activation.

## Recommended Next Actions

Initiating task records disposition and updates stale acceptance metadata after consolidating reviews. Preserve listed future gates. Review instance3 of3 exhausted; no additional review instance started.
