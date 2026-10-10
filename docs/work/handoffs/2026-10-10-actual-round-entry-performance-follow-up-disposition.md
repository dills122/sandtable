# Handoff: Stage 1 performance follow-up disposition

Date: 2026-10-10 America/Toronto. Administrative later-evidence record for the frozen [follow-up](../../research/2026-10-10-actual-round-entry-performance-follow-up.md), SHA256 `daf3f6bff76c0ffa55b7b53a4f07047608e1a9d32c40f268d1d3c48db6bf7bec`. Its historical absence of complete Core XML remains unchanged; evidence below arrived later.

## Objective And Boundary

Preserve Brain's scoped optimization decision and completed Debug evidence. Author retains product/test ownership and the sole .NET lease. This research lane changes only the follow-up and this handoff, with no further diagnostic or product work.

## Canonical Sources

The [frozen ARE contract](../../specs/combat-actual-round-entry-v1.md) remains product authority. The [first investigation](../../research/2026-10-10-actual-round-entry-performance-design.md) and [first disposition](2026-10-10-actual-round-entry-performance-disposition.md) remain byte-identical. This handoff records coordination and evidence, not a new contract or permission to alter pins.

## Current Repository State

Research branch `codex/actual-round-entry-performance-research`, checkpoint `1993aab7de6cf122fdd0fddbb4375ec2da30fa6d`. Product snapshot remains PR169 head `f836a04c7314dd6fdb028e7bc4f077dac6a39a98`; unchanged Core DLL SHA256 `b92a1827dd9b5d77668858ffe79749c50e0d842dabad3980a9b80ec2098eca53`. No follow-on optimization is implemented by this lane.

## Completed Work And Evidence

Corrected full Debug solution passed **2,764/2,764**, zero failures/skips, in **29m56.179s**: Core 2,285, ExerciseRunner 469, Contracts 10. Core runner module elapsed 29m55.822s; XML assembly time 1,794.612s measures a different scope. Standalone native acceptance previously passed 171/171 in 17m12.877s.

Under `/private/tmp/native-round-entry-gates/stage1-reports`:

- Core XML `Cna.Core.Tests.xunit.xml`, SHA256 `fa06f95007fd51b8c208cb12ef4bc3d4f481e86a47268d7dd9aca44f69dbb430`;
- `solution-method-timings.json`, SHA256 `c5cc405791c520fcdcbf90c3fe6082c23a26d1d9ad40ae2b029bdef9593dd68b`.

Native reported case durations sum 1,420.810s: mutation 777.545s/34 cases (maximum31.254s), original-history forgery192.609s/2, grammar/capacity179.300s/34, lifecycle108.456s/2, cuts/retries95.111s/34, remaining approximately67.8s. Mutation is about54.7% of native **summed case durations**, not CPU share or wall-time budget. Concurrent collections overlap and differ from standalone timing. Do not subtract this sum from Core assembly elapsed or forecast Release CI from it.

Substantial existing groups include inherited reserve completion650.711s, reaction completion562.583s and actual-selection cuts506.507s (also summed overlapping case durations). They confer no predecessor/shared edit authority. Method times do not separate memo hits/misses, predecessor derivation, parsing or critical-path costs.

## Decisions And Rationale

Brain chat `01a0c9dc-00bc-78a3-800d-3cb36859e422` reports fresh `performance_research_review_2` returned **Ready for the scoped experiment**, with no findings, after verifying frozen follow-up `daf3f6bf` and later XML. This is an attributed summary, not verbatim reviewer text. Brain accepts; author response: Accept. Research checkpoint **2/9**, separate from DAY-A implementation review **0/9**, recovery **0/2**; Brain owns all counters.

Brain authorizes **only a guard-preserving same-entry read-only Replay result return before hydration**. Keep current source/version/count and round-input canonical guards before full evidence lookup; all57 physical pins before lookup; consumed Content at use; complete owned source/ledgers/context key; owned canonical result copies; fresh claimed-byte parsing/comparison. Apply retains fresh transition/hydration behavior. No early lookup, separate Base cache, shared/predecessor edit, pin change, timeout increase or coverage/assertion reduction is authorized. The result path removes identified redundant work; its savings and CI fit are unproven.

## Blockers And Limitations

Exact-head required Release CI remains canceled by the15m0s job limit, with Core unfinished and no retained hosted Core XML. Full Debug PASS does not satisfy it. Original ExerciseRunner `AggregationFailed` remains unexplained/unwaived despite later passes. Research Ready is not implementation Ready. Further performance/full gates belong to the authorized follow-on head; existing f836 Debug evidence is retained, not discarded.

## Immediate Next Actions

Author implements only Brain's scoped experiment and retains all171 acceptance cases/public assertions. Author and Brain reconcile measured performance, corrected-head focused/full-solution/build/format evidence, required Release CI and implementation review. This lane performs no further diagnostic or implementation work.

## Verification Commands

This preservation change uses `git diff --check`, local-link/whitespace checks, SHA256 checks and exact two-file staging/commit inspection. No .NET run or CI is claimed for the documentation commit. Completed test results above were supplied by the author and inspected through retained logs/XML.

## Delivery Metadata

Repository `/Users/dsteele/.codex/worktrees/combat-actual-bridge-research/sandtable`; branch above; base DAY-B `eca7739fd09a3c15132f38a4b826fbf2931df6be`; retained first research commit `1993aab7de6cf122fdd0fddbb4375ec2da30fa6d`. Authorized new commit contains exactly the frozen follow-up and this separate handoff. Preexisting `.serena/` stays excluded. Brain authorizes commit/push using GitHub Keychain authentication, **no PR yet**. Exact resulting head/file hashes/push outcome are returned to Brain without rewriting the frozen records.
