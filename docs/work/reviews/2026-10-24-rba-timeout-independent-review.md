# Persisted native RBA timeout independent review

Review instance: 3 of 3. Reviewer started from neutral bootstrap, inspected requirements,
plan, implementation and tests, then sent preliminary ledger before reading separate
`2026-10-24-rba-timeout-author.md`. No implementation history inherited. Read-only review;
only this report written. No builds/tests run by reviewer because shared build lease
belonged to implementation task.

Repository: `/Users/dsteele/.codex/worktrees/3d3c/sandtable`.
Branch: `codex/rc-overnight-plan-20261023`.
Base/HEAD: `7554bb223f75ebd92595514104ff123507a346cf` plus working-tree changes.
Final boundary captured after Host XML completed at 2026-10-24 21:40:42 UTC:
28 changed/new implementation, test, config, spec, plan and high-level documentation files,
excluding `docs/work/reviews/` packets. SHA-256 over sorted relative path + NUL + current
file bytes + NUL:
`024b87c21da2598351f90266b5b5e4c418f072e95305d1d891f5beee74c8399c`.
No commits created during review. Added report/handoff/author packets are review artifacts.
Scope: native V33 cancellation, Core/session/checkpoint/adapter integration, schema-8
journal and RBA scheduler, relevant tests, RBA spec, plan and accompanying documentation.
Excluded: pre-existing P01 source pins, public admission/certificates, dependency gaps,
other missing fallback windows and performance certification.

## Findings

**P2, corrected during review — malformed persisted seed escaped scheduler isolation.**
Original `CampaignRbaTimeoutService.SweepOnce` catch omitted `FormatException` and
`OverflowException`; `CampaignDurableAuthorityJournal.Load` parses persisted seed with
`ulong.Parse`. A collecting campaign with seed `bad` or `18446744073709551616` therefore
terminated sweep instead of logging per-campaign failure and allowing later campaigns
to progress. Author accepted finding. Current catch handles both exceptions; regression
`RbaSchedulerRotatesPastCorruptCampaigns` exercises both seed values across full first
page, then verifies later healthy campaign expires. Correction is bounded to error
isolation and regression coverage, without architecture or contract change.

No further actionable implementation defect identified in inspected scope. Corrected
isolation regression passed in focused Host run (13/13 total).

## Plan Review

- Contract-first separation is sound: native event V33 and accepted-step version 27
  are distinct version domains. Canonical event binds original Barrage, latest retreat,
  current version, configured budget and exact same-slot entry/exit cursors.
- Core cancellation preserves accepted retreat World and RNG, advances contact authority,
  bypasses decline/Force/result creation and enters Reserve Release. Initial and repeated
  runtime branches retain explicit cancellation through reconstruction and checkpointing.
- Host appends Barrage and creates deadline row inside same immediate transaction.
  Returning in-memory successor from `PersistAccepted` avoids validating transient
  Barrage-without-round state. Row failure rolls back selection/Barrage transaction.
- Submission validates credentials, retained opening, action mapping and Core acceptance
  before RBA clock mutation. Receipt retries return retained evidence before clock gate.
  Valid late offer and background tick share cancellation path. Retreat/reopen never
  replaces opening/deadline; only high-water changes.
- Cold replay verifies every Barrage has matching round or explicit pre-opening unavailable
  evidence, checks closing status against accepted events and binds cancellation to retained
  timing row. Schema 5/6/7 remain readable; mutation guards require schema 8. No automatic
  migration invents past opening times.
- Scheduler shares injected TimeProvider, registers independently of outward API, processes
  bounded pages and advances cursor past failed campaigns. Cancellation checked between
  campaigns. Per-campaign replay cost remains proportional to retained history.
- Actual ordinal-two/ordinal-three persisted timeout and explicit deadline-row insertion
  rollback tests were added during review and inspected. Third-assault test traverses
  two actual paid assaults, retained reserve choices and generic post-repeat selection
  before expiring third RBA and cold-reconstructing history. All passed in focused run.
- README, naming overview, technical design and journal/RBA specs updated. Timing manifest
  adds exactly one class and six measured rows, preserving all existing row values.
  Actual discovery validates updated allocation; final evidence below.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Consequence |
| --- | --- | --- | --- |
| Cancellation has no fabricated decline, seal or result | V33 Create/Replay; CancelNativeCombatRba; repeat Apply; Core and Host cancellation tests | Confirmed statically | Correct semantic boundary |
| World, RNG and accepted retreat survive | Initial/repeat Reserve Release initializers, retreat-chain reconstruction, Core tests and Host repeated retreat test | Confirmed statically | No new spend/random path |
| Deadline captured once with Barrage | Submissions transaction, InitializeRbaRound, immutable update fields, rollback test | Confirmed statically | Owner request cannot renew deadline |
| Invalid submission cannot consume deadline | SubmitOwnerActionCore ordering, late invalid-reference test, move-as-decline rejection | Confirmed statically | Clock mutation occurs after accepted proposal validation |
| Cold replay and legacy safety retained | Import canonical command/evidence comparison, VerifyRbaRounds, RequireWritable and schema gates | Confirmed statically | Legacy reads do not synthesize timing |
| Scheduler isolates corruption and avoids starvation | Cursor pagination and corrected seed-error catch, two-case regression | Confirmed after correction | Both regressions pass in focused Host XML |
| Native cancellation does not admit public P01 evidence | Scope/spec/docs and no public contract activation changes | Confirmed | Existing admission gaps remain excluded |
| Focused suites/build/format/row discovery pass | Core/Host XML and coordinator receipt/logs | Confirmed | Bounded local validation supports scoped verdict |

## Verification Performed

Reviewer executed Git status/branch/HEAD/diff inspection and `git diff --check` (passed),
read applicable AGENTS.md, RBA plan/spec, canonical timing/fallback requirements in
`combat-rules-inputs-v1.md` and `combat-step-transitions-v1.md`, and inspected relevant
implementation/test source. No builds or test processes started by reviewer.

Used codebase-memory Tier 2: index status, RBA symbol search, both-direction TickRbaRound
trace, exact V33 snippet, coverage check for 21 relied-on source/test paths. Index generation
reported `2026-10-24T21:33:24Z`; graph is best-effort, not exhaustive authority. Coverage
reported partial `CampaignExercises.cs:147` and numerous `CampaignHostAdapter.cs` ranges;
material checkpoint, command, import and accepted-step paths were verified directly from
source. Broad trace results included unrelated resolver edges and were not treated as
proof of runtime calls. CCE context_search/session_recall tools unavailable in tool catalog.
Follow-up coverage included requirements/docs, changed scheduler/tests and timing manifests;
changed/unknown freshness paths were read directly. Final manifest check reported generation
`2026-10-24T21:38:42Z` and no recorded issue. This review makes no exhaustive graph claim.

Implementation-owned checks inspected directly:

| Command | Result and evidence |
| --- | --- |
| `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj -c Release --no-restore --report-xunit-xml --results-directory artifacts/rc-rba-core --filter-class Cna.Core.Tests.Campaigns.CampaignNativeRbaCancellationTests Cna.Core.Tests.Observations.CampaignContactCombatSelectionCutTests Cna.Core.Tests.Campaigns.CampaignReserveReleaseRulesTests Cna.Core.Tests.Hosting.CampaignHostAdapterTests` | 17/17 pass, including six new cases; `artifacts/rc-rba-core/receipt.json` and XML |
| `dotnet test --project tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj -c Release --no-build --no-restore --filter-method '*Rba*' --report-xunit-xml --results-directory artifacts/rc-rba/host-focused` | 13/13 pass; focused XML includes seed corruption, third assault and rollback |
| `dotnet test --project tests/Cna.OrleansHost.Tests/Cna.OrleansHost.Tests.csproj -c Release --no-build --no-restore --report-xunit-xml --results-directory artifacts/rc-rba/host-all` | 141/141 pass, zero skipped/errors; `host-all/dsteele_Dylans-MacBook-Pro_2026-10-24_21_37_41.840810.xunit.xml` |
| `dotnet build Sandtable.slnx --configuration Release --no-restore` | Exit 0, zero warnings/errors; `artifacts/rc-rba-validation/build.log` |
| `dotnet format Sandtable.slnx --verify-no-changes --no-restore` | Exit 0 per `artifacts/rc-rba-validation/coordinator-receipt.json`; empty format log |
| `python3 .github/scripts/local_tests.py boundary --no-build` | 81/81 pass; boundary log/XML |
| `python3 .github/scripts/local_tests.py runner --no-build` | 470/470 pass; `artifacts/rc-rba-validation/runner.log` |
| `python3 -m unittest discover -s .github/scripts -p '*partition_test.py'` | 194 pass outside sandbox; earlier sandbox supervisor failure retained in `partition-sandbox-failure.txt` |
| `python3 .github/scripts/local_tests.py fast-01 --dry-run --no-build` | Exit 0: actual Release discovery 3,606 Core rows (3,281 ordinary plus 325 R02); discovery only, no shard execution |

Initial Host sandbox startup hang was stopped; subsequent analyzer errors fixed through
logger delegates/static helpers. Earlier wrong
test argument placement selected zero tests (exit 5), not treated as passing evidence.

## Open Questions And Residual Risks

- Actual PeriodicTimer start/stop lifecycle remains beyond direct new test coverage;
  scheduler unit tests invoke SweepOnce with fake clock.
- Legacy schema tests cover ordinary retained reads and rejection; no migration utility
  ships. Operating existing journal requires retaining read-only access or explicit future
  migration rather than changing user_version in production.
- No workload/latency guarantee from 16-campaign batch bound: each tick cold-verifies history.
- No full solution rerun or exact-head hosted CI claimed by reviewer. Original P01/public
  gates remain open regardless of native timeout results.

## Verdict

**Ready with non-blocking follow-ups.** Persisted native RBA timeout meets inspected
contract and plan, with passing focused Core, full Host, Runner, boundary, build, format
and discovery evidence. Original P2 corrected and regression-tested. No outstanding
blocking finding. Verdict applies only to reviewed native timeout change; no public RC,
P01 admission, performance, full-solution test or exact-head CI claim.

## Recommended Next Actions

1. Retain referenced validation artifacts and captured boundary with implementation handoff.
2. Track actual PeriodicTimer start/stop lifecycle coverage as non-blocking test.
3. Require exact-head CI before merge; preserve explicit non-public scope.

Review budget exhausted: 3 of 3. No further independent reviewer instance will start.
Any material architecture/migration pivot or new review allowance requires human decision.
