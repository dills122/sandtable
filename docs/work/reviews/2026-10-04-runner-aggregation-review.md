# Independent review: R2 Runner aggregation diagnosis

Review instance: set 1/pass 1, total 1 of maximum 9; maximum 3 passes per set. Fresh reviewer chat 01a10a34-5898-72f2-a34b-6e064a297124. Read-only; no edits, builds, test executions, commits, fixes or reviewer dispatch.

## Findings

No actionable findings. The report correctly documents an unresolved historical incident and does not claim a repaired runtime.

Preliminary ledger was recorded in commentary before reading the separate author explanation: scope/base matched; historical source-equivalence diff was empty; completed-report handling and first paired invocation were supported; cleanup and suppressed assertion diagnostics explained the evidence gap; no defect identified, pending hash/input checks.

Blind-pass limitation: the required primary coordination ledger disclosed “cause unknown/no fix” before the preliminary pass. No inherited implementation conversation or separate author packet was read then. Later authorization inspection occurred after the preliminary pass and author reconciliation. Human authorization was independently confirmed in Brain: “you kick off tasks coordinate work and messages between child chats” and subsequent approval of the overnight plan; merge authorization was explicitly “Merge after review and CI pass.”

## Plan Review

R2 acceptance permits either a diagnosed cause or an honest unknown with a concrete next diagnostic step. This delivery meets the second outcome. The four Markdown files match the frozen working-tree boundary on codex/runner-aggregation-research, base/head 907f41403ed159d65024f193f3e1f730a23b9bbb. Only excluded .serena tooling accompanies them; no tracked runtime/test/fixture/configuration diff.

The primary run-state records the explicit short lease, one build/one Execute result, and release. No competing gate or repeat experiment was introduced by this reviewer. Proposed future evidence preservation is separately assigned and distinguished from repair; actual repair remains contingent on diagnosed mechanism, named ownership of at most three files, meaningful RED/GREEN, fresh review and CI. No architectural/product-scope expansion or heavy pivot is needed.

## Author-Claim Reconciliation

| Claim | Evidence inspected | Status / consequence |
| --- | --- | --- |
| Historical failure is first paired invocation, child unknown | Failure stack at test lines331/339; Run before second invocation | Confirmed; no baseline/candidate attribution justified |
| Exit14 implies completed paired-report handling | ExecutePaired and WriteStatusAndMapExit; default PairedReportWriter readback | Confirmed inference; execution/report exceptions return different exits |
| Five explicit aggregation branches converge on public status | PairedManeuverExecutor.Execute and completion/profile guards | Confirmed; missing bundle, reader exception, identity/profile, pair evidence, terminal/check rejection are distinguished accurately |
| Default path is synchronous and arms sequential | ManeuverExecutionDependencies.Default; paired loop | Confirmed; no child CLI/model inference involved |
| Git/build cohort mismatch is possible, unobserved historically | Capture, RunGit, HasEqualPairEvidence/HasEqualBuildCohort | Confirmed as hypothesis only; no unequal historical field or timeout threshold established |
| Evidence is suppressed/deleted by test lifecycle | Run exit assertion before output inspection; Dispose; GUID roots | Confirmed; copying evidence before cleanup is a credible diagnostic next step |
| Frozen JSON is not the actual materialized test input | CurrentFixture and exact Serena Create symbol; retained harness/input JSON | Confirmed; Truck identity/rules hash and maxSteps30 replacements match, rootSeed/pairKey/repetition/controllers preserved |
| Current source equals retained e8fb checkpoint in bounded scope | Independently executed Git diff over Core/Runner/Runner tests/input | Confirmed source equivalence; historical binary/environment identity remains unknown |
| Present control passed but is not historical reproduction | Build/result logs, paired report, two child build identities, binary/input/output hashes | Confirmed; 2 succeeded,17 steps/122 passing checks each, divergence13, no aggregation failures |
| Original artifacts were not recovered | Exact original worktree absent; zero matching roots immediately under both named temp roots | Confirmed bounded inventory; no exhaustive filesystem absence claim |

Historical logs independently show 2495/2496 with one failure, filtered1/1 pass, full repeat2496/2496 pass, all0skipped. Log mtimes confirm the filtered result predates failure-log completion; “isolated” cannot establish an idle host. Cross-assembly concurrency is visible; exhaustion/corruption is not.

## Verification Performed

Executed independently on the exact target:
- git rev-parse HEAD; git branch --show-current; git status --short — expected SHA/branch/four docs plus .serena.
- git diff --check — pass (tracked diff empty).
- git diff e8fbaefd9c73edb198103ef1b4e9423ec89befe1 HEAD -- src/Cna.Core src/Cna.ExerciseRunner tests/Cna.ExerciseRunner.Tests scenarios/maneuvers/rules-lab.movement-cost.paired.v1.json — empty.
- git show -s --format='%H %cI' e8fbaefd9c73edb198103ef1b4e9423ec89befe1 — 2026-10-04T19:53:42-04:00, after the historical logs.
- git check-ignore .planning/r2-runner-probe/movement-cost.json — ignored.
- shasum -a 256 on all three historical logs and retained Program.cs, probe.csproj, Core/Runner DLLs, deps.json, probe.dll, materialized input, stdout/stderr and paired report — all documented hashes match.
- Python pathlib/hashlib/json/re scripts — source/input hashes match; relative links resolve; harness fenced source exact; project XML matches except omitted terminal newline; control input replacements match test; binlog exists; source-equivalence.diff is0bytes; bounded inventory confirms no original temp roots/worktree.

Inspected author evidence, not rerun: build exit0/0warnings/0errors/6.31s; Execute exit0/4443ms. SDK10.0.400 and macOS26.6.2 are recorded in precontrol provenance; runtime.NET10.0.11/arm64 is independently visible in both child identities/result. No claim that historical versions were verified. Existing build binlog path was checked, not decoded.

Serena manual read, exact worktree activated, Create and writer symbols retrieved. Codebase Memory Verify tier: exact project sandtable-runner-aggregation-research, generation2026-10-05T03:24:32Z,16279nodes/111258edges; symbol search fully returned13rows; both-direction depth1 paired Execute trace fully returned. Coverage for11 cited input/source/test paths and both Runner scopes: metadata_match/no_recorded_issue, no further pages. Graph trace omitted delegate-bound CLI caller and contained a spurious Add resolution; direct source, not the graph, establishes the chain. Clean best-effort coverage does not prove completeness; known parser gaps are elsewhere.

## Frozen Hashes

| File | SHA256 |
| --- | --- |
| docs/research/runner-aggregation-failure.md | c1549d8641dbd9cbf90eba7a4e856642c8b15f75475688218d736d8ad12ccfe7 |
| docs/work/handoffs/2026-10-04-runner-aggregation-research.md | 68d3c4d9d4d44218321a41f5543735568a1f39c5bfe03767e7791b2abcee2b71 |
| docs/work/reviews/2026-10-04-runner-aggregation-bootstrap.md | 914ab4f89de8752c0611dac4bdbe51288119175a750a0be67856b7486d9c026b |
| docs/work/reviews/2026-10-04-runner-aggregation-author.md | b4d16f264abfef944341595fc07c13563c537c59ec976b6fcb8cc6f40a1a1ebc |

Verified historical log hashes: failure31f2cc3f7328fb0ed995c7968573025e1298b09d4148522f2ba7d261fd48831c; filtered4de01fcb4f35fab4eade35b57ae665063c1e57f24351d191b67a9371dcace8a8; repeat97e29245d8c29b9829d736a53a6dc2fe3f83e6056e420d223612bedc15154ac1.
Verified control input cb00d2f10dbd8139ae21092d340df73a2b4f2aa27093577f9d2138bd7ea97926 and paired report d0fdedbd53fc03a0dfe56640e607947d58b0920d74bcf84c028a795ee6d48e2c. Both child identities match documented Core e86e43206b4227c9e5ab1664c03d2f9cb49f10e2f7d57dedeb4678626f2838f5 and Runner5d93921be9c7a3ed7d3207fd3362541755b0323668829efe19a8d4f35f5be20d hashes.

## Open Questions And Residual Risks

Original report/category/ordinal/child diagnostics, rejected bytes, binary hashes, Git stderr, cancellation and host resource state are unavailable. Therefore the historical mechanism remains unconfirmed. One current success supplies no intermittency estimate or historical exclusion. Temporary evidence may disappear; the committed report must retain the honest distinction and reproducible harness. No new suite/CI result is supplied by this review.

## Verdict

Ready — for the scoped research and diagnostic decision only. No production fix has been established or approved.

## Recommended Next Actions

Coordinator retain this report and obtain author acceptance, then publish only scoped documentation with reconciled review/publication metadata. Merge remains subject to required final-head CI. Preserve strict aggregation guards. If recurrence or separately assigned diagnostic work proceeds, retain report/child evidence and captured output before cleanup; obtain exact exception/cohort/finalization discriminator before any repair dispatch. No additional reviewer or experiment is required by this report.


## Author response

**Accept.** No findings. Retain the unknown/no-fix conclusion; the passing current control is not incident repair. Publication updates only review/PR/commit metadata. Coordinator owns final-head CI and merge.
