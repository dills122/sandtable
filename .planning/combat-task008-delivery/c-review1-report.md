# Task008 C Weather independent review

Review instance: 1 of 3.

## Preliminary ledger — recorded before author explanation

Target confirmed: branch `codex/combat-task008-weather`, HEAD/base `22f8da6d0399c90c2d333ce27a4ea37193da343e`, explicit working-tree delta including three untracked Weather implementation files and untracked tests. Five primary files including test project; documentation/status changes match scope.

Read canonical Weather and stage-entry requirements, plan delta, Weather tests before implementation, then all three implementation files. CCE search was used first; it returned signature snippets without expansion IDs, so exact known files were inspected directly. CCE incidentally exposed opening paragraphs of c-author/c-evidence before this ledger; full author rationale remains unread. No prior review reports read.

- No confirmed defect on initial pass. Replay reconstructs actual four-event opening, checks source artifact and explicit-none policy, derives all Weather values, and rejects byte differences.
- Authorize runs before retry lookup; accepted input is immutable value equality; original event is reserialized from retained authority.
- State reader compares complete replay-derived bytes, preserving existing World and receipts. Neither standalone cache nor synthetic predecessor establishes authority.
- Tests cover 34 frozen traces, both cuts, both initiative orders, rejected RNG bytes, all foul locations, re-signed invented Weather, changed retry, leaf/raw mutations, and caller buffer isolation.
- Pending checks: run focused tests/oracle; confirm predecessor trust profile and plan exclusions, then reconcile author claims. Existing RNG implementation owns isolated cursor boundary coverage; C starts at creation cursor.

## Findings

No actionable findings. `CampaignCombatWeather.Replay` requires complete B1 reconstruction; `RequireWeatherAuthority` binds artifact hash, manifest sources and explicit policy; `Emit` uses unchanged Rules/RNG and exact expected input. `ReadState` never trusts cached values. Full canonical comparison rejects altered effects, cursor, source, envelope, receipt and successor even when attacker recomputes checksums. API remains internal/dormant.

## Plan Review

Task008 C row matches implementation: retained dice/RNG/source/context parity through state6. Five primary files meet bounded slice. B1→C→B2 sequencing follows canonical stage-entry requirement for actual Weather predecessor. Positive effects, later stages, noninitial Snapshot12 and publication are explicitly excluded by canonical Weather scope; deferrals do not conceal C acceptance requirements. B parent, H and HOST-PUB-001 remain open. README, design, naming and roadmap consistently describe Organization boundary and dormant status. No migration/rollout change needed for internal unactivated adapter.

## Author-Claim Reconciliation

| Claim | Evidence | Status / consequence |
| --- | --- | --- |
| Actual creation/opening reconstruction, no cache authority | Replay; CampaignOpeningPreamble.Replay; CampaignCreationSnapshotV12.Create; creation request/context guards | Confirmed. Frozen Setup/config plus canonical Rules10 enforced upstream. |
| Existing Rules artifact/RNG semantics preserved | RequireWeatherAuthority; Cna1979Weather.Resolve; ArtifactAndFoulLocationCoverageMatchFrozenAuthority | Confirmed. No inferred outcome table. |
| Axis holder under both choices; all outcomes and World preserved | FrozenChainsMatchAllBytesOutcomesReceiptsAndBothCuts, 34 rows | Confirmed by executed tests and exact frozen fingerprints. |
| Exact retry and forged-history rejection | Apply/Authorize; coherent forged-event/cache and changed-occurrence tests | Confirmed. Retry returns original event and current projection, same resulting cursor/prefix. |
| 306 fingerprints / 68 cuts / all 12 foul locations | Cases, CheckGolden and artifact coverage test | Confirmed by test code and 42-test passing execution. |
| Full integration/build/format evidence | c-evidence.md; /tmp/c-build.log; /tmp/c-suite.log | Build success log inspected. Format reported by lead, not independently rerun. Full suite was still running when inspected; no independent full-suite pass claim. |
| Publication and general restore remain open | canonical authority runtime-evidence section and Task008 plan | Confirmed; readiness is for C slice only. |

## Verification Performed

- `dotnet --version`: 10.0.400. SDK-style net10.0, native MTP, xUnit confirmed from global.json/project/shared props.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatWeatherTests' '-bl:/tmp/c-review1-{}.binlog'`: initial sandbox IPC bind denied (exit134); same command with approved escalation passed42, failed0, skipped0. Successful binlog `/tmp/c-review1-20260919-220226--53573--u4S5Xb-dotnet-test.binlog` exists. No build performed by reviewer.
- `shasum -a 256 -c .planning/combat-task008-delivery/c-source.sha256`: all five files OK.
- `git diff --check`: pass.
- `PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-weather-v1.py`: exit0; PASS34 creation-rooted traces,68 replay/state cuts,11516 leaf mutations,921 raw rejections,1651 boundary/retry checks,3960 Rules coordinates. Contract/source evidence, separate from executed C# parity checks.

## Open Questions And Residual Risks

No unresolved C-specific correctness question. Focused tests use already built artifacts; inspected lead build log shows successful integration build and source hash verification confirms reviewed tree. Generic snapshot admission, authenticated ingress, trusted published head, storage durability and positive effects remain deliberately unproven. Exact bytes enforce collection bounds at admitted values; no generic malicious-cache parsing path added.

## Verdict

Ready for bounded dormant C Weather slice, subject to normal lead integration gate completion. No source fix requested.

## Recommended Next Actions

Lead finishes existing integration checks and review process, then records C acceptance before implementing B2 against real accepted Weather replay.
