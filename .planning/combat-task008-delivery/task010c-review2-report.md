# Task010C independent review

Review instance: 2 of 3. Reviewer mode. Scope base `fe5664d2c0d4ac56b1db8ef21ef850796d625fea` to code head `9b999a7df5d1a0f54b7ca4ffcf045197734169a7`; branch `codex/combat-task008-reaction-lifecycle`.

## Findings

No actionable P0–P3 findings. Exact completed G2 remains required; unsupported successors reject. No code correction or plan pivot recommended.

## Plan Review

Canonical Task010 refinement (docs/design/combat-cycle-implementation-plan.md:875) is satisfied by this bounded implementation: actual010A selection/traversal integrated after G2, timed010B remains synthetic, Prepared011 stays excluded. Source scope is exactly two production files and one new test file; no schemas, fixtures, oracles or Snapshot writer changes. README:69, roadmap:50 and tech-design:1177 describe pending verification and preserve parent/publication exclusions.

Ordered dependency is sound: authoritative predecessor admission before suffix handling; strict local readers before exposing typed projections. Exact existing grammars and frozen Control bytes remain authoritative. Four histories exercise both owners and six/seven moves, with 32 suffix cuts, 36 including G2, and 118 complete prefix visits. Existing inherited retry tests supply original-event recovery coverage. Snapshot12 intentionally fails closed until separately specified roots exist; no migration or rollout is implied by dormant internal types. Full review count, broader gates and parent010 acceptance remain root responsibilities.

Repeated predecessor reconstruction adds bounded replay cost but avoids introducing independently trusted state caches. For this fixed four-history profile and two/six-event suffix, no correctness or maintainability blocker identified. Author's narrow Created ownership correction is relevant to retained-evidence trust and directly covered by adversarial Count callback test.

## Author-Claim Reconciliation

Author/checks packets read only after preliminary ledger was persisted. No excluded prior review, aggregate evidence, worker/devnotes or proposal rationale read.

| Claim | Evidence | Status / consequence |
| --- | --- | --- |
| Exact G2 cut prevents self-admission recursion | HistoryReplay:63–79; Certification.Admit:13; strict inherited Replay calls | Confirmed; nested generic calls receive predecessor only and return at G2. |
| Complete suffix consumed or rejected | Selection reader limit2, traversal limit6, byte-exact regenerated event comparison; malformed/excess/synthetic tests | Confirmed; no successful prefix return with ignored tail. |
| Cumulative receipts owned and State cannot be replaced | HistoryModels:66–94; get-only local states; per-cut ledger equality and reflection/read-only collection tests | Confirmed; cached cumulative arrays remain coupled to immutable state. |
| Local Controls, World/RNG/resources unchanged | No Control writer diff; independent frozen hash/length assertions and byte-identical boundary/nested selection assertions | Confirmed; projection wrapper does not mutate domain state. |
| Four histories,32 new cuts,36 with G2,118 prefix visits | CombatStepsHistoryReplayTests:28–109 | Confirmed by source and focused execution; these counts are not distinct histories. |
| New Snapshot12 families rejected, old368 roots/286 histories preserved | Closed codec switch:126–141; new rejection test; existing literal-root regression | Confirmed by focused and inherited/snapshot execution: 368 literal roots across 286 distinct histories preserved. |
| No fixture/schema/oracle/Snapshot writer/public API changes | Base-to-head path diff | Confirmed. |
| RED failures, intermediate test correction, fullsuite2284, Boundary81, fullformat and CI success | task010c-checks.md statements only | Reported, not independently reproduced or externally verified; no claim of reviewer-observed full gates. |

## Verification Performed

Independent source checks: `shasum -a 256 -c .planning/combat-task008-delivery/task010c-source.sha256` passed all three entries. `git diff --check fe5664d2c0d4ac56b1db8ef21ef850796d625fea 9b999a7df5d1a0f54b7ca4ffcf045197734169a7 -- src tests README.md tech-design.md docs/roadmap/pre-alpha-roadmap.md` exited0. HEAD, branch and status checked; full source/test diff and scoped docs read.

SDK10.0.400; SDK-style net10.0 arm64 test project; global.json native Microsoft.Testing.Platform; xUnit v3 MTP package4.0.1. Existing build reused with user-authorized `--no-build`; no independent rebuild or assembly-source attestation. Both test commands use `login:false`, approved local IPC and unique /tmp binlogs. No `--disable-build-servers` used.

1. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatStepsHistoryReplayTests' '-bl:/tmp/task010c-review2-focused-{}.binlog'` — exit0,7 passed,0 failed,0 skipped,43.760s. Binlog: `/tmp/task010c-review2-focused-20260920-100652--23895--HwMInM-dotnet-test.binlog`.
2. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatInheritedStepsTests' '*CombatInheritedSnapshotTests' '-bl:/tmp/task010c-review2-regressions-{}.binlog'` — exit0,18 passed,0 failed,0 skipped,43.055s. Binlog: `/tmp/task010c-review2-regressions-20260920-100748--23933--1qcOhW-dotnet-test.binlog`. Includes inherited retry/readback regressions and all368 literal roots across286 histories.

Both test sessions exited normally. Final process inspection found no dotnet, Cna.Core.Tests, MSBuild or VBCSCompiler process (only reviewer command text matched search). Both binlog files exist. Final manifest check passed all entries; HEAD unchanged. Only pre-existing tracked metadata dirt remains; reviewer wrote only this report plus authorized test-generated artifacts/binlogs. No source or Git writes.

## Open Questions And Residual Risks

No unresolved implementation question. Review limited to frozen dormant010C scope and surrounding inherited/snapshot contracts. Full build, full solution suite, format, Boundary and remote CI were not rerun. Historical RED/GREEN logs were not inspected. Current checks packet is dirty root-owned metadata and is treated as testimony. New Snapshot12 roots, positive history, Prepared011, public activation and durable publication remain excluded.

## Verdict

**Ready** for root acceptance consideration within frozen Task010C scope. Code and canonical plan agree; focused and inherited/snapshot checks pass. This is review instance2of3, not parent010 acceptance or publication approval.

## Recommended Next Actions

Root owns acceptance and remaining configured review budget. No fixes, new workstreams or extra review instances requested by this reviewer.


## Preliminary blind ledger — persisted before author/checks packets

No actionable finding from blind source/tests/canonical-requirements pass. Verdict pending focused execution and claim reconciliation.

- Source manifest: all three SHA256 entries match. HEAD matches dispatch. Only pre-existing tracked dirt: execution.md and task010c-checks.md; contents not read during blind pass. Source and scoped README/roadmap/tech-design unchanged from head.
- Authority/termination: HistoryReplay lines 63–80 strictly replay one G2 completion, then supply only completed predecessor prefix to inherited readers. Certification.Admit requires completed BreakdownCompletion projection; recursive calls terminate at G2. Selection partition capped at two only to select reader; strict inherited replay validates bytes. Entire remaining traversal tail passed to six-event reader.
- Projection ownership: HistoryModels Selection/NoAttack constructors copy cumulative receipts in causal order; get-only State cannot diverge from cached ledger. Existing local State objects have get-only members and owned read-only local receipts. No Control writer changes.
- Preservation: new tests compare every cut's local Control hashes/lengths to independent frozen fixtures, nested boundary/selection bytes, current version/prefix/position and cumulative ledger; 32 new cuts, 36 including G2, 118 total prefix visits are distinct assertions.
- Rejection: tests exercise missing/reordered/duplicate/excess tails, rehashed effects and identity/actor substitutions, foreign histories, synthetic C3a, unsupported movement/Reaction forks, byte/count bounds and caller-buffer ownership.
- Snapshot12: existing closed SerializeState switch rejects new projection kinds; Serialize and Restore both use it. New tests cover both paths and old G2 restore. Existing CombatInheritedSnapshotTests independently assert 368 literal roots; execution pending.
- Retry coverage: unchanged CombatInheritedStepsTests exercises both families' original-byte retry readback at each later cut. New router is replay-only; no duplicate-command mechanism added.
- Plan: canonical docs/design/combat-cycle-implementation-plan.md lines 875–883 require actual010A integration only; unchanged grammars, separate synthetic010B provenance, Prepared011 and extended Snapshot12 exclusions match implementation. README/roadmap keep parent010 open; tech-design describes exact scope without publication claim.
- Check so far: scoped `git diff --check` passed; SDK10.0.400 with native Microsoft.Testing.Platform and xUnit v3 configuration inspected. No runtime tests yet. No source/Git mutations, other review content, aggregate evidence, worker/devnotes, proposal rationale, execution-history contents, cross-session retrieval or agents used. Dispatch metadata was read; its links to excluded content were not followed.

