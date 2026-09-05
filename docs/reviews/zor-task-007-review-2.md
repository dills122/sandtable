# ZOR-TASK-007 independent review

Review instance: 2 of 3

Target: `2627423..1db4b02` on `codex/zor-task-007-runner-closeout`.

## Preliminary blind ledger

Recorded before opening `/tmp/zor007-review-2/author-explanation.md` or prior review report. Bootstrap, current instructions, canonical requirements/design, plan, exact source/test diff, and relevant execution/replay context inspected. CCE precise path queries returned source snippets but lacked complete chunk identifiers; exact known-path reads supplied missing context. No author explanation or prior review contents encountered. Requirements and plan themselves contain prior readiness claims; these were treated as claims.

| Concern | Blind evidence | Preliminary result |
| --- | --- | --- |
| Scope integrity | HEAD, branch, commit log, changed-path list and dirty status match bootstrap; excluded user edits remain separate. | Scope verified. |
| Current Movement completion might drop Reaction costs or weaken replay | New factory preserves entire World/random/setup state, checks valid Snapshot 10 and closed window, and compares reconstructed canonical event bytes before applying. Public execution still checks audience/current action membership first. | No defect found; verify predecessor equivalence and terminal tests. |
| Reaction controller could consume hidden bindings or select arbitrary concurrent audiences | Executor maps Reaction candidates only to action IDs/kinds; controller requires exact System close pair plus one player with closed Reaction kinds. Counts update after accepted submissions and reset on completion/close. | No authority/privacy defect found; review counter and mixed-policy limits. |
| Artifact hashes could authenticate semantically forged events | New event kinds enter exact root-property/version/canonical checks; existing semantic validator reconstructs and freshly submits accepted actions. New rehashed binding/reason mutations reject. | No defect found; focused checks pending. |
| Checked matrix might overclaim CP/BP, fog, negative fixtures, or recurrence | Fifteen-child test asserts explicit counts, selected order, two-step episodes, later windows, close reasons, final CP, unchanged BP state, RNG cursor, and ZOC-ended state. Most fixtures are nonmotorized; broader fog and qualification negatives rely on existing Core suites. | Verify precise evidence mapping; no concrete defect established. |
| Clean-run evidence provenance | Saved comparison identifies implementation checkpoint `dad5f1c`, not final documentation head `1db4b02`; source/test diff must remain unchanged between them. | Check documentation distinguishes these commits and rerun read-only comparison. |

## Findings

No actionable findings in `2627423..1db4b02`. No confirmed P0/P1/P2/P3 defect.

Blind concerns resolved through source and verification:

- Current Movement completion preserves full World, setup, orders, weather, initiative and random state. `CampaignSnapshotV10Validator.IsValid` checks current position against canonical stage orders; `CampaignCurrentMovementCompletion.Create` additionally requires stage-1 first-side Movement and no open window. `Apply` compares complete reconstructed event bytes. Existing public submission membership checks remain ahead of execution. Comparison with `CampaignMovementEventFactory.ValidateCompletionAuthority` and `CampaignProjector.ApplyMovementCompletion` confirms unchanged completion semantics without predecessor rejection of accepted non-phasing movement.
- `ExerciseControllerActionSet` sorts action IDs ordinally. `ReactionController.SelectReaction` admits only one reacting player with exactly one completion/decline plus Reaction moves, alongside exact System unavailable/timeout pair; sole no-eligible-reactor close has its own shape. Ordinary policy behavior remains unchanged. `ExerciseExecutor` supplies public ID/kind for Reaction, updates counts after accepted submissions, and resets episode/window counts on completion/closure.
- Strict Runner envelopes are only first validation layer. `ExerciseBundleSemanticValidator.ObserveReconstruction` requires one exact canonical event match from fresh Core legal submissions; `ObserveReadjudication` separately resubmits retained actions and checks receipts/events/final state. Rehashed event bindings and inconsistent System close reason fail actual bundle readback.
- Accounting coverage includes inherited motorized Commonwealth A with Truck cohort (`Cna1979SyntheticContentCatalog.CreateElements`), not just added nonmotorized remote elements. Checked tests sum exact rational CP and compare full initial/final vehicle-Breakdown state. `MOV-REQ-015` and `MOV-AC-017` retain existing no-BP-mutation boundary; this is continuity evidence, not new Breakdown adjudication.
- Clean-run evidence explicitly names `dad5f1cddf6e5e3b81bbef9b1cdc7deae7169671`. `git diff --stat dad5f1c..1db4b02` contains only documentation; final head has identical implementation, tests, and checked manifest. No claim of clean execution at a different source commit is needed.

## Plan Review

007A obligations covered by nine distinct controller policies, seven admitted synthetic Content v5 packs/setups, and fifteen checked children. Fixtures use public campaign creation and provenance-bearing TOE seeds. Positive and low-defense variants change admitted strength; remote topology has a connected branch; no runtime ZOC flag or checkpoint injection substitutes for authority.

`ReactionManeuverTests.AssertReactionEvidence` checks exact child counts, descending order, two-step episodes, recurrence across distinct windows, subset close, active System close, reason-specific fallback, empty/noncombat windows, frozen local participant membership, CP totals, BP/RNG continuity, and exact final Breakdown checkpoint with no open window. Strict content tests separately check initial controlled-location sets. Retained Core suites cover independent qualification/topology negatives, recurrence restrictions, stale/forged submissions, fog equivalence, and disclosure. Evidence study maps all 18 ZOR acceptance criteria to these complementary layers; it does not claim the fifteen children alone exhaust every rule vector.

007B obligations covered by strict new event envelopes, malformed-shape and fully rehashed semantic mutations, fresh-session reconstruction/re-adjudication, repeatable clean-run evidence, synchronized project/design/naming/roadmap documents, full retained gate, and review reconciliation. EXR-025/027/AC-014 now accurately state opt-in Reaction audience arbitration and accepted-history counters.

Direct current Movement completion is a necessary bounded Core adjustment: predecessor validation prevented approved Runner terminal after legal Reaction. It preserves authority ownership and current event-v1 semantics. No migration beyond previously activated identity set, new service, remote I/O, Combat, hosting scheduler, or Breakdown adjudication entered scope. Plan marks all approved phases complete; no uncovered delivery obligation or redesign identified.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Nine policies and seven new admitted fixtures cover fifteen checked children | Policy enum/token maps; content/setup catalogs; checked manifest; `ReactionControllerTests`, `ReactionRunnerContentTests`, `ReactionManeuverTests` | Confirmed | Planned Runner adoption implemented with bounded explicit identities. |
| Controllers use public IDs/kinds and accepted episode/window counts | `ExerciseExecutor` candidate projection and accepted-step updates; `ReactionController.SelectReaction`; sorted action-set constructor | Confirmed | No hidden representation binding or authority-event input introduced. |
| Existing policies and identities remain stable | Appended enum values; unchanged existing token strings; opt-in policy dispatch; retained setup IDs/hashes and legacy-policy tests in supplied full gate | Confirmed | Existing Movement studies retain declared behavior. |
| Current completion fixes predecessor rejection while preserving authority | Current completion factory/application; current execution/router; predecessor completion validation; matrix final-state/replay assertions | Confirmed | Necessary scope adjustment maintains same semantic completion event. |
| Hash refresh cannot legitimize forged Reaction events | Exact envelope codec; semantic validator fresh submissions; personally executed binding/reason tamper tests | Confirmed | Strict readback rejects semantic corruption beyond file-hash integrity. |
| CP, vehicle-Breakdown state, and RNG continuity checked | Matrix exact CP sums and full BP-state/cursor assertions; inherited motorized A; existing Movement scope | Confirmed | Evidence supports implemented accounting without claiming BP mutation. |
| Full gate passed 1216 solution tests and 48 boundary tests | `/tmp/zor007-just-check.log`, including restore/format/build and zero warnings/errors | Confirmed from retained log | Broad gate corroborates focused reviewer execution; not represented as newly run full gate. |
| Two clean candidate runs match 135 canonical files and verify 60 proofs | Supplied roots/build identities/proofs; independently executed read-only comparison; succeeded report states | Confirmed | Reproducibility evidence tied to exact implementation checkpoint. |
| Final documentation synchronized without source changes | `dad5f1c..1db4b02` path/diff review; README, technical design, naming, Harness/ZOR documents, roadmap, evidence study | Confirmed | Final head accurately closes approved work. |

## Verification Performed

Personally executed:

- `git diff --check 2627423..1db4b02`: exit 0, no output.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*Reaction*' --filter-class '*Zoc*' --filter-class '*CampaignSuccessorActivationTests'`: **87 passed**, zero failed/skipped.
- `dotnet test --project tests/Cna.ExerciseRunner.Tests/Cna.ExerciseRunner.Tests.csproj --no-build --filter-class '*Reaction*' --filter-class '*ExerciseBundleSemanticValidatorTests'`: **83 passed**, zero failed/skipped; includes complete fifteen-child strict Maneuver and semantic tamper cases.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-trait 'Boundary=UserSpace'`: **48 passed**, zero failed/skipped.
- Read-only execution of `/tmp/zor007-compare.py` with final file-write statement removed: exit 0. Both retained reports have `succeeded` status; all thirty child build identities are clean and name `dad5f1c`; all sixty proofs verify; 135 canonical files and deterministic report objects match. Report fingerprint `sha256:21e2e2dde1002c677b382a398c6ff3e40635c324437fe1c3ab1d30c90694f845`; canonical-file hash matrix `sha256:00d3f8d351c4722653fe531e2121f067be1bf7e72e7815a26a2e60b7eae1ec43`.

Inspected, not rerun: supplied full `just check` log showing restore, format check, warning-free build, 48 boundary tests and all 1216 solution tests passing. Native MTP/xUnit configuration verified from `global.json`, test projects and shared props/packages; selected SDK reports `10.0.400`. Focused tests used existing built output and required normal outside-sandbox IPC. No new restore/build or duplicate clean CLI run was needed because source/tests/manifest match retained verified implementation checkpoint.

Branch, exact HEAD, three in-scope commits, changed paths, and dirty state checked before review. Final status contains only original excluded user edits plus this report. No source fixes, plan edits, commits, staging, further review instances, or subagents created by reviewer.

## Open Questions And Residual Risks

- No unresolved implementation or plan question. Tests establish supported synthetic rules-laboratory behavior; later hosting deadlines, arbitrary content, Combat and Breakdown adjudication remain outside review.
- Policy tokens remain duplicated across three existing maps; strict event envelopes duplicate Core property names. Current tests exercise additions, but future changes must keep these surfaces synchronized.
- Exact trajectory counts intentionally depend on content/action identities. Future identity changes require review of updated expected trajectories and clean evidence together.
- Clean runs used exploratory manifest mode with clean source identity; they are not baseline-mode artifacts. Timing/build-path diagnostics remain outside deterministic report identity.
- Independence preserved for blind pass: author explanation and prior report opened only after preliminary ledger was written. CCE session recall was deferred until after that ledger because it can retrieve author/prior-review conclusions; its later results were treated as testimony. Prior report inspected afterward only to assess in-scope documentation, not as proof of correctness.

## Verdict

**Ready.** Exact final diff and implementation plan supported by inspected source, personally executed focused tests, retained full gate, strict semantic readback, and independently checked clean-run equality. No corrective source work or heavy pivot required.

## Recommended Next Actions

Return this report to initiating task for reconciliation and retain it with final delivery evidence. No source remediation or further review instance requested.
