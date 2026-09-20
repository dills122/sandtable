# Task008 H4 independent review

Review instance: 3 of 3.
Candidate: 30e9921bef2d6f528be27ca3a2abc110eabf5b76. Base: ca43442.
Branch: codex/combat-task008-reaction-lifecycle.

## Preliminary blind ledger

Recorded before reading h4-author.md, h4-checks.md or session recall. Prior review reports, execution.md and aggregate evidence excluded. Exact known source paths read under bootstrap permission.

- Scope verified: all five h4-source.sha256 entries match; scoped Git status clean. Diff contains one production codec, one new test class, three additive test-history enumerators, supporting docs and review artifacts. Review artifacts excluded from code judgment.
- No actionable defect identified in blind pass. Runtime verification still pending; no readiness verdict yet.
- Whole-root authority: Serialize replays retained history; Restore requires independently retained Created11, calls actual creation policy and compares entire supplied root against replay-derived canonical bytes. No supplied family/cache authority input.
- Mapping: starts from creation-only nineteen-field root, validates creation/configuration identity, complete receipt versions and accumulated event prefix, preserves typed causal arms/Reaction positions and copies closed-state evidence.
- Bounds/ownership: root byte/depth/array limits precede history access; World cohesion allowance uses structural position. Retained history validates count, each payload and aggregate before ownership copies. Caller/export mutation test exercises later serialization from restored history.
- Coverage: test enumerators reconstruct actual events through existing Apply/Create handlers. Selected-cut test requires 368 roots, 286 distinct retained histories and 62 shared groups; every history restores with admissionEnabled:false. Exact creation equality and unchanged legacy-reader rejection tested.
- Falsification includes canonical spellings/order/omissions, legal foreign forks, shorter/reordered/missing/duplicate histories, rehashed causal RNG forgery, closed-state omissions, missing retained creation and capacity sentries.
- Plan scope sound: H4 depends on accepted H1–H3; Initial H excludes later 28-trace closure, public admission and HOST-PUB-001. String-field composition and repeated bounded replay/copy costs warrant future care, but no concrete current defect established.
- Pending checks: focused H4 execution; actual creation-policy implementation; author claim reconciliation; existing binary/source provenance for required --no-build run.

## Findings

No actionable P0–P3 findings. Final source hashes and HEAD unchanged after focused execution. No source edits, Git mutations, delegation or additional review rounds performed.

Blind-first ordering preserved: bootstrap/skill, canonical requirements/schema/plan, exact source/tests/diff, persisted preliminary ledger, then author/checks/session recall. No prior H4 review reports, execution.md historical verdicts or h4-evidence.md read. Required session recall returned unrelated historical summaries despite exclusion wording; none used as readiness evidence. Canonical plan contains historical acceptance labels; judgment rests on inspected implementation and checks below.

## Plan Review

H4 meets bounded Initial H acceptance in `docs/design/combat-cycle-implementation-plan.md:822`, `:834` and `:836`: exact inherited roots at every selected retained cut, full history replay and actual fresh-admission-disabled restore. H1–H3 remain causal routing dependencies; no new family selector or cached projection bypass introduced.

Implementation stays within five primary paths: one internal Core codec, one test class, three additive history enumerators. Existing creation-only codec, predecessor codecs, historical synthetic contracts, fixtures and schemas unchanged against base. README, tech-design and naming-overview describe H4 as active and keep public/gameplay/publication gates open. These conservative delivery statuses may be advanced by root after acceptance; they do not falsely claim completed parent Task008.

`Compose` preserves nineteen-field creation template, derives mutable state from strict typed serializers, checks identity/configuration/creation bindings and full event/receipt prefix, and maps explicit Reserve/inherited-cycle arms. Reaction root position and suspended Movement position remain distinct. Closed interruption/end/completion evidence survives. Causal idle/empty normalization is guarded rather than supplied by caller. `SerializeState` has a closed supported-projection switch; unsupported families reject.

No rollout, migration or host registration required for this internal dormant seam. Existing historical readers remain intact. Later Tasks009–019 must extend actual composition while retaining inherited evidence. All 28 later runtime traces, public admission, archive commitment authentication and HOST-PUB-001 durability remain excluded. No heavy pivot or H5 split justified by this review.

## Author-Claim Reconciliation

Paths below relative to repository root. Codec = `src/Cna.Core/Campaigns/CampaignCombatInheritedSnapshotV12Codec.cs`; tests = `tests/Cna.Core.Tests/Campaigns/CombatInheritedSnapshotTests.cs`.

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Root derives only from trusted retained history | Codec:10, :28, :126; existing HistoryReplay router | Confirmed | No caller family/cache state accepted as authority. |
| Complete creation/configuration/ledger/prefix binding | Codec Compose; tests:69, :114, :144 | Confirmed | Foreign legal branches, shortened histories and genuinely rehashed causal forgery reject. |
| All 368 root rows, 286 histories, 62 shared groups match | Tests:13 and three actual-handler Trace builders; independent focused pass | Confirmed | Full selected inventory covered, including exact byte length/hash and creation equality. |
| Restore uses actual admission policy, requiring retained Created11 | Codec:13; CampaignCreatedV11Serializer.cs:61; tests:52 | Confirmed | Missing creation rejects for either admission setting; retained path requires no publication. |
| All selected cuts restore with fresh admission disabled | Tests:13 invokes Restore(... admissionEnabled:false) for every unique history | Confirmed | Runtime evidence supports Initial H, not merely Python vectors. |
| Causal absence, Reaction positions and closed evidence preserved | Compose; tests:69, :170, :180; exact fixture-byte coverage | Confirmed | No observed loss of inherited arm/window/continuation evidence. |
| Bounds precede history access; owned buffers resist mutation | Codec:107; CampaignCombatRetainedHistory.Capture; tests:192, :225 | Confirmed | Structural World exception, count/byte limits and defensive ownership exercised. |
| Creation reader and synthetic contracts unchanged | Scoped base diff; tests:13; retained full-suite log | Confirmed structurally; full-suite result inspected | No compatibility widening; historical suite not independently rerun. |
| Eleven focused cases pass | Independent command below | Confirmed | 11 passed, zero failures/skips. |
| Full build/full suite/boundary pass | Raw /tmp/h4-build.log, /tmp/h4-suite.log, /tmp/h4-boundary.log | Confirmed as retained log results | Supporting evidence only; not represented as this reviewer's execution. |
| Format passed | h4-checks.md reports exit0; /tmp/h4-format-full.log empty | Unverified independently | Empty log cannot establish exit code; scoped diff whitespace check independently passed. |
| Initial H closes all future Task008 obligations | Author explicitly disclaims this | Confirmed exclusion | No parent/publication completion inferred. |

## Verification Performed

Independent commands used `login:false`:

```sh
git rev-parse HEAD
git branch --show-current
git diff --name-status ca43442 HEAD
shasum -a 256 -c .planning/combat-task008-delivery/h4-source.sha256
git status --short -- src/Cna.Core/Campaigns/CampaignCombatInheritedSnapshotV12Codec.cs tests/Cna.Core.Tests/Campaigns/CombatInheritedSnapshotTests.cs tests/Cna.Core.Tests/Campaigns/CombatHistoryReplayTests.cs tests/Cna.Core.Tests/Campaigns/CombatMovementHistoryReplayTests.cs tests/Cna.Core.Tests/Campaigns/CombatReactionHistoryReplayTests.cs
dotnet --version
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatInheritedSnapshotTests' '-bl:/tmp/h4-review3-{}.binlog'
git diff --check ca43442 HEAD -- src/Cna.Core/Campaigns/CampaignCombatInheritedSnapshotV12Codec.cs tests/Cna.Core.Tests/Campaigns/CombatInheritedSnapshotTests.cs tests/Cna.Core.Tests/Campaigns/CombatHistoryReplayTests.cs tests/Cna.Core.Tests/Campaigns/CombatMovementHistoryReplayTests.cs tests/Cna.Core.Tests/Campaigns/CombatReactionHistoryReplayTests.cs README.md tech-design.md naming-overview.md docs/design/combat-cycle-implementation-plan.md
```

- HEAD `30e9921bef2d6f528be27ca3a2abc110eabf5b76`; branch `codex/combat-task008-reaction-lifecycle`; base `ca43442`. Five hashes match before and after execution; scoped status clean.
- SDK `10.0.400`; global.json selects native MTP, SDK-style test project targets net10.0 and xUnit v3 MTP. Platform/test/binlog skills applied.
- Focused test command ran with approved escalation for local IPC. Exit0; **11 passed, 0 failed, 0 skipped; 14s915ms**. Owned session 94302 finished before final report.
- Unique binlog verified present: `/tmp/h4-review3-20260920-062049--6851--WJ2Mqv-dotnet-test.binlog` (651,548 bytes).
- Scoped `git diff --check` exit0, no output.
- Required `--no-build` run used existing debug artifacts. DLL timestamps postdate changed codec/test source; retained frozen-build log corroborates build. No independent rebuild or cryptographic binary/source attestation performed.
- Inspected retained raw logs: focused freeze 11/11 (14s930ms), build 0 warnings/errors (5.10s), full solution 2,244/2,244 (6m47s343ms), boundary 81/81 (9s806ms). All test summaries show zero failures/skips. No unchanged Python oracle rerun.

## Open Questions And Residual Risks

No blocking open question within selected H4 scope. Trusted committed request/history/head remain caller/archive responsibilities: recomputable hashes establish consistency, not commitment. Test coverage is exhaustive for selected frozen cuts, not unrestricted campaign histories or future authority families. Root composition depends on string field names from thirteen typed serializers; future schema changes need matching mapping/golden coverage. Replay and defensive copies incur bounded repeated work; no performance benchmark or production durability proof claimed.

## Verdict

**Ready** for bounded Task008 H4 / Initial H Core codec and retained-history restore. Publication evidence remains open. No whole-parent, public-admission, later 28-trace or durable-restart approval.

## Recommended Next Actions

Root may accept this report and reconcile delivery status under existing workflow. Preserve HOST-PUB-001 and future-family obligations. Review instance 3 of 3 complete; configured review limit reached, so no further round started. No source fixes requested.
