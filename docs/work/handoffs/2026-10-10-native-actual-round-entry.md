# Native actual round entry — author freeze and evidence handoff

Date: 2026-10-10. **Author engineering self-review complete; required Release CI still blocked by timeout.** Brain owns fresh independent implementation review, the next scope decision and merge. No product edits follow this freeze without authorization.

## Repository target and packets

Worktree: `/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable`.
Branch: `codex/native-actual-round-entry`.
Base: `34a494c46ab7b8665e381f9e531c7b8365cb3efe` (PR167).
Frozen primary checkpoint: `64439f79356fd6303408e6a9b3579e530235b9d6`.
[Draft PR169](https://github.com/dills122/sandtable/pull/169), title **Add private native actual Combat round entry**.
The accompanying message gives the administrative publication head. All five primary files match the [manifest](../reviews/2026-10-10-native-actual-round-entry-primary-hashes.json). Only `.serena/` is excluded untracked tool state; the main checkout is not this review target.

Give the reviewer the [neutral bootstrap](../reviews/2026-10-10-native-actual-round-entry-bootstrap.md) first. Hold the [author explanation](../reviews/2026-10-10-native-actual-round-entry-author.md) separately until a preliminary inspection is recorded. Implementation counter remains0/9; recovery0/2. Proposed next review instance1/9 is dispatched only by Brain. Diagnostic research reviews do not count as implementation approval.

## Completed local gates

All logs, unique binlogs, XML and diagnostic probes are retained under `/private/tmp/native-round-entry-gates` unless otherwise noted. Debug environment: macOS/arm64, SDK10.0.400, runtime10.0.11, native MTP with xUnit4.0.1. Core DLL SHA256: `c836f5e521eca026e0fb2a86a32680b4babb034b92022a8a489cd93ac293bc69`.

| Gate | Result | Retained evidence |
| --- | --- | --- |
| Solution build |0 warnings/errors,4.52s | `stage1b-solution-build.log`, unique binlog |
| Full solution format verify |Passed | `stage1b-format.log` (empty, exit0) |
| Frozen dependencies |All57 physical hashes match; four round-entry artifacts unchanged | `stage1b-physical-pins.json` |
| New six-case baseline before shortcut |6/6 passed,2.398s | `stage1b-baseline-corrected.log` |
| Focused memo tests |27/27 passed,10.346s | `stage1b-focused.log`, binlog |
| Complete native acceptance |177/177 passed,0 failed/skipped,14m18.036s | `stage1b-native-acceptance.log`, binlog, `stage1b-native-reports` |
| Full Debug solution |2770/2770 passed,0 failed/skipped,25m00.482s | `stage1b-full-solution.log`, binlog, `stage1b-reports` |

Commands executed:

```sh
dotnet build Sandtable.slnx --no-restore '/bl:/private/tmp/native-round-entry-gates/stage1b-solution-build-{}.binlog'
dotnet format Sandtable.slnx --verify-no-changes --no-restore
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-method '*MemoReadOnlyResultAbsentThenPresentPreservesLiteralsGuardsAndFreshApply*' '/bl:/private/tmp/native-round-entry-gates/stage1b-baseline-corrected-{}.binlog'
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-method '*Memo*' '/bl:/private/tmp/native-round-entry-gates/stage1b-focused-{}.binlog'
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class Cna.Core.Tests.Campaigns.CombatActualRoundEntryTests --report-xunit-xml '/bl:/private/tmp/native-round-entry-gates/stage1b-native-acceptance-{}.binlog'
dotnet test --solution Sandtable.slnx --no-build --report-xunit-xml --results-directory /private/tmp/native-round-entry-gates/stage1b-solution-results '/bl:/private/tmp/native-round-entry-gates/stage1b-full-solution-{}.binlog'
```

Full solution module completions: Core2291/2291,25m00.306s; ExerciseRunner469/469,2m32.805s; Contracts10/10,593ms. XML assembly times differ slightly from module/outer-run durations. The paired-route test passed8.283s while Core was active; this does not establish the original failure's cause.

| XML | SHA256 |
| --- | --- |
| `stage1b-native-reports/Cna.Core.Tests.xunit.xml` | `2c709769a536e10415263e6855a006d2db3f16c39ea13532a4edb04737dc708c` |
| `stage1b-reports/Cna.Core.Tests.xunit.xml` | `b73d8c37b344716e089f4366fb9b985e3e626614300da43d09e676cf25dcce33` |
| `stage1b-reports/Cna.ExerciseRunner.Tests.xunit.xml` | `e48fa1f3af226488565a808ddcdf0e62494765560cb3fccade47bc5943e804b5` |
| `stage1b-reports/Cna.Intelligence.Contracts.Tests.xunit.xml` | `035a9532eeeee333ae4b0923a32fcca31287466a692565d5ba74fe9e964f5464` |

`stage1b-reports/solution-method-timings.json` preserves every method group. Native177 case durations sum1195.113s during the full solution; summed case durations across overlapping collections are not additive wall time or causal component shares. Largest groups: native mutations34=664.536s; inherited Reserve completion deep2=558.305s; reaction completion leaves2=458.201s; actual-selection cuts16=376.784s; reaction fallback leaves2=319.389s; reaction history14=302.268s; actual-selection resigned16=282.620s. Native canonical34=166.407s and original-history forgery2=141.093s. Compare the prior complete reports carefully because runtime coactivity differs.

## Required exact-head CI blocker

[Run38082003227](https://github.com/dills122/sandtable/actions/runs/38082003227), verify job114300669399 at `64439f7`:

- Verify20:00:13–20:15:28UTC; conclusion **cancelled**.
- Test20:02:41–20:15:26UTC; Core unfinished. Hosted Contracts passed900ms and ExerciseRunner passed3m56.603s.
- Annotations: “The job has exceeded the maximum execution time of 15m0s” and “The operation was canceled.”
- Restore, format and Release build passed. All seven other PR checks succeeded.
- Result upload skipped; artifacts API returned an empty list. No hosted Core XML or complete test count exists.

Exact raw log: `ci-stage1b-64439f7.log`, SHA256 `3e979d262933d7d8031253d61fe0d87b49a19bcaace594412dd8dc2dcd22d2b3`. Readable ANSI-stripped copy: `ci-stage1b-64439f7-plain.log`. Terminal metadata: `ci-stage1b-64439f7-manifest.json`. An initial grep missed ANSI-colored module PASS lines; its no-module-completion statement was promptly corrected to Brain. The cancellation remains the required gate outcome.

Hosted test command:

```sh
dotnet test --solution Sandtable.slnx --configuration Release --no-build --report-xunit-xml --results-directory artifacts/test-results
```

No timeout increase, test removal, pin weakening or CI waiver is authorized. Brain reopened bounded read-only research into remaining hot paths versus complete-test-preserving scheduling/partitioning. No workflow/code/cache edits follow automatically. Final exact-head CI remains required after any approved scope changes or administrative publication.

## Earlier correctness and performance evidence retained

Original checkpoints: `a37fbd8` then `f836a04`, followed by frozen `64439f7`.

- Initial semantic stub RED34 then literal GREEN34: `semantic-red.log` and retained GREEN/binlogs.
- Original-ledger null runtime leak RED then004 GREEN: `ledger-red-qualified.log`, `ledger-green.log`.
- ASCII carrier RED008 versus reference004, then all128 ASCII values exercised: `ascii-red.log`, `ascii-all-corrected.log`, `ascii-reference-all.log`.
- Prospective warm Replay work target58 callbacks initially observed74, then passed. This is a scoped optimization sensitivity target, not universal contract law. Retain cache-work RED and stage1-work GREEN logs.
- Stage1b new harness first expected001 for noncanonical RoundInput; existing008 was preserved by correcting only the test. `stage1b-baseline.log` retains that failure; corrected six-case baseline passed before the shortcut.
- Stage1 foreign Setup and reversed-ledger expectation harness errors, zero-test filters, compilation/timer/ASCII harness mistakes and reflection ambiguity are not product RED/PASS claims.
- Stage1 complete native171/171 passed17m12.877s; full solution2764/2764 passed29m56.179s. `stage1-native-acceptance.log`, `stage1-full-solution.log`, `stage1-reports` retain those results.
- Unchanged mutation cases0/17/2 passed17.039/18.450/16.300s versus stage1 28.507/25.768/21.714s. Logs `mutation-case-{0,17,2}-stage1b.log`; no original cancelled-case baseline exists.
- `profile-{0,2}-stage1b.log` retains eight optimized-JIT Debug samples and thread allocations. Prepared warm Replay/readback medians6.037/12.226ms, cancelled5.987/12.041ms. Single-run/outlier/coactivity differences prohibit a robust aggregate attribution or CI forecast.
- Temporary BCL-only probe source is under `/private/tmp/native-round-entry-profile`; no production/test package or coverage change was introduced.

The retained original failures are not waived:

- Original local solution stopped incomplete after about45minutes, exit130; Core had no passing count. `full-solution.log` also reported ExerciseRunner `CertifiedMovementCostPairRetainsRepeatableRouteDivergence`, expectedSucceeded/actualAggregationFailed. The isolated test and full469-test module later passed (`aggregation-isolated.log`, `aggregation-module.log`); stage1 and stage1b complete solutions also passed it. Cause remains unknown.
- Original draft Release run38070603604 and stage1 run38078496884 also cancelled at the15-minute limit. `ci-original-head.log` and `ci-stage1-f836a04.log` retain those outcomes.
- Four inherited Python failures: Breakdown LandSequence pin, cycle-sequence LandSequence pin, inherited Snapshot LandSequence pin, and outward composition rejection/accepted Content drift. See the unchanged [contract handoff](2026-10-10-actual-round-entry-contract.md). No pin refresh or parent closure is claimed.

## Frozen round-entry artifacts

| Artifact | SHA256 |
| --- | --- |
| Contract | `ed4fd69b4f599acae7c4850270557363a546df36d0b984107c50c08c29304548` |
| Schema | `677b1c671bebe5f23cf4b80f03078c55000c60e45416701757e2552cbd05edb8` |
| Fixture | `fc17cda07618bdd539a56b8c296037bbf36f37c6eb2ce6258c2b789b765f640b` |
| Verifier | `2596531cb50ddbae7f1e606284f3d845b224d4f1fc10f478b717843d1062990c` |

The unchanged round-entry Python oracle was previously run at the merged contract checkpoint; no redundant long rerun is claimed here. Physical hash verification plus native parity does not waive inherited failures.

## Ownership, limits and next step

The five primary files remain frozen and match the manifest. Dated packets are administrative. No paid commitment/results/settlement, full Snapshot/Archives restart, repeated Movement, later-II/consumed lineage, Exercise/Runner activation or parent017–019 completion is delivered. No public route or service activation is added.

Brain owns independent review and CI-scope reconciliation. Author reports SELF_REVIEW_COMPLETE without merge readiness. The .NET lease is idle; no new heavy run starts without assignment. Preserve the packets, all prior failures and the explicit CI blocker when reviewing or resuming this branch.
