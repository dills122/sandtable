# Task011 independent review — final

Review instance: 3 of 3. Target branch `codex/combat-task008-reaction-lifecycle`; base `8108ccd40f84d12135f17290c25bf3d268efbd8f`; observed start/end HEAD `94a4ecd99054d11a36350d9ec8fba55c9b5363bb`. Frozen five-file manifest plus current README/roadmap/tech-design scope docs. Source/Git read-only; only this report written. Preliminary ledger below was persisted before opening author/checks packets.

## Findings

No actionable findings. Independent rebuild and all34 selected tests passed. No code or plan blocker found within dormant Task011 precommit scope.

Authority review covered `CampaignCombatSealedRound.AuthenticateBase`, `Replay`, `Transition`, raw `ReadBase`/`ReadState`, the C3a syntax bridge and its Identity grammar. Separately supplied trusted Boundary, predecessor inputs and Round inputs establish authority; exact retained Created/configuration and complete replay bytes remain required. No event.input self-authentication or caller state transition API found. Full typed grammar validation precedes semantic context access for persisted Base/state and precedes trusted input indexing for malformed Round events.

Clock review covered immutable opening floor and original budget, both role orders, actual private seal timestamps, actor/allocation validation before cancellation, exact retries before lifecycle/time gates, stale callback no-ops and Prepared precedence. Tests demonstrate opposing4000 seal cannot reject own3500 input after opening3000; opening1999 may follow predecessor RBA2001. These behaviors agree with active v2 contract.

## Plan Review

Canonical `docs/design/combat-cycle-implementation-plan.md` Task011 and Checkpoint E align with implementation: two private slots, retained duplicate event readback, pinned deadline, cancellation without costs/history/RNG, and Prepared structural continuation. Task010 dependency is marked accepted in canonical plan; independently exercised shared010B code here. Acceptance history itself was not re-audited.

Prepared stops at Close Assault step5; cancelled paths close at step6. Task012 commit/debit is rejected, including all four retained attack-committed suffixes. Literal scope is exactly10 Base2 values,54 precommit events,64 states. Full58 events/68 states would overstate implementation and are not credited.

Five primary source/test files fit planned size and Core ownership. Narrow existing-grammar bridge avoids a second World grammar; signed/unsigned primitives and semantic array order stay with existing typed grammar. No new protocol, hosting service or activation path added. README and roadmap correctly retain implementation/verification status pending root acceptance; tech-design documents bounded implemented mechanism. Canonical plan's next-task wording is not treated as an acceptance claim.

First-seal privacy evidence covers the waiting owner's declassified allocation/status/opening/deadline witness and equal admission outcomes. It does not establish public revision serialization or complete public privacy. Positive campaign provenance, actual history integration, audience-safe projection, extended Snapshot12 and `HOST-PUB-001` remain explicit parent/integration gates. No heavy pivot or new workstream required for this slice.

## Author-Claim Reconciliation

| Claim | Independent evidence | Status / consequence |
| --- | --- | --- |
| Separate trusted predecessor and Round inputs; no caller Control/State authority | AuthenticateBase/Replay; SeparatePredecessorAndRoundInputsRemainRequiredAuthority; shared010B provenance checks | Confirmed for dormant trusted-boundary API; future host must supply authenticated inputs |
| Base2/configuration binding and17 exact descriptors | ReadBase equality, retained literals, schema comparison17/17, original C3a compatible creation guard | Confirmed |
| Fixed public opening floor; first seal preserves other owner witness | Gate/Transition;192 fresh outcomes and96 retries;1999 opening test | Confirmed for bounded contract; public/host privacy not claimed |
| Exact retries precede clock/status but follow primitive checks | Transition; every-cut recovery/admission-disabled and negative UTC checks | Confirmed |
| Prepared FA/Anti-Armor; cancellation closes without paid use | Transition/SerializeState; literal cuts and world/RNG equality | Confirmed;012 remains excluded |
| Raw grammar precedes trusted context; retained bytes owned | Codec raw readers; sentry, nested primitive, UInt64, order and alias tests | Confirmed |
|10Bases/54events/64states excluding4commit effects | Literal test passed; independent fixture inventory | Confirmed; no credit for full58/68 C# implementation |
| Root fullsuite2292, Boundary81, fullformat and exact-candidate CI | task011-checks.md only | Reported, not independently rerun or remotely checked; root retains gate ownership |
| Bounded replay, no benchmark or persistence/publication claim | Every operation reconstructs bounded histories; no runtime adapter introduced | Consistent with inspected code and plan |

## Verification Performed

All .NET commands used `login:false`, approved local IPC, unique `/tmp/task011-review3-*.binlog`; no `--disable-build-servers` passed to tests. Installed SDK `dotnet --version`:10.0.400, permitted by global.json10.0.302/latestFeature. SDK-style net10.0 xUnit native MTP confirmed from global.json, test project and shared properties.

1. `shasum -a 256 -c .planning/combat-task008-delivery/task011-source.sha256`: all five files passed before review and after tests. Manifest contents unchanged; SHA256 `ea407cc67f571124bb8bec05cf24fc0f3eebc63b2bd2385464360bf1197fa5c5`.
2. `dotnet build tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore -t:Rebuild -m:1 -nodeReuse:false -p:UseSharedCompilation=false '-bl:/tmp/task011-review3-build-{}.binlog'`: exit0;0 warnings/0 errors;9.21s. Binlog `/tmp/task011-review3-build-20260920-105052--27255--psn1qQ.binlog`.
3. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class Cna.Core.Tests.Campaigns.CombatSealsTests '-bl:/tmp/task011-review3-seals-{}.binlog'`: exit0;8 passed/0 failed/0 skipped;8.174s. Binlog `/tmp/task011-review3-seals-20260920-105111--27290--h+zTvS-dotnet-test.binlog`.
4. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class Cna.Core.Tests.Campaigns.CombatStepsTests Cna.Core.Tests.Campaigns.CombatIdentityTests '-bl:/tmp/task011-review3-shared-{}.binlog'`: exit0;26 passed/0 failed/0 skipped;13.035s. Binlog `/tmp/task011-review3-shared-20260920-105142--27312--pqH8rp-dotnet-test.binlog`.
5. Inline read-only `python3 -B` comparison: parsed17 Shapes entries from frozen Round codec and asserted equality with `combat-sealed-round-v2.schema.json` objects; inventoried fixture cuts before attack-committed, asserted `(bases,events,states,excluded)==(10,54,64,4)`. Passed.
6. `git diff --check 8108ccd40f84d12135f17290c25bf3d268efbd8f -- src/Cna.Core/Campaigns/CampaignCombatSealedRoundModels.cs src/Cna.Core/Campaigns/CampaignCombatSealedRound.cs src/Cna.Core/Campaigns/CampaignCombatSealedRoundCodec.cs src/Cna.Core/Campaigns/CampaignCombatSelectionStepsCodec.cs tests/Cna.Core.Tests/Campaigns/CombatSealsTests.cs README.md docs/roadmap/pre-alpha-roadmap.md tech-design.md`: exit0, no output.
7. All three build/test sessions exited0. Final `ps -axo pid,ppid,comm | rg 'dotnet|Cna.Core.Tests|VBCSCompiler|MSBuild'`: no matching processes (rg exit1). All three binlogs exist. No reviewer process left running.

Reviewed scope-document SHA256 values: README `408653e1de9c9a2918481626442d74b4b6ed68023e44d01ede7a0cb0ce38723a`; roadmap `2bf338bfdecc4a45dcc59bda43064618d66f47a3bdd2deddedb3ad045eab7fb7`; tech-design `e42cf48594cdaa6309858c1d8130593382c20d9cf3bbea03b70f37e4b9cab343`.

## Open Questions And Residual Risks

No blocking question within frozen slice. Trusted-input authentication remains an upstream obligation: typed values and event bytes are not themselves proof of host identity. Synthetic retained C3a evidence must not be presented as authentic positive campaign history. No full solution, full format, Python oracle, remote CI, performance, durable restart, public projection or publication check independently run in this instance. Claims for those checks remain root-owned and separately bounded.

## Verdict

**Ready** for root acceptance of the frozen dormant Task011 precommit slice.

## Recommended Next Actions

Root owns acceptance and parent gates. Preserve manifest identity when accepting or publishing review result. Keep012, positive-history integration, projection, Snapshot12 and publication obligations open. Review budget3of3 exhausted; no further reviewer instance, fix, commit or workstream started.

---

# Task011 independent review

Review instance: 3 of 3. Reviewer mode; source/Git read-only. CCE, prior review reports, aggregate evidence, worker/dev notes, rationale and execution history excluded.

## Preliminary blind ledger — persisted before author/checks reads

Target: branch `codex/combat-task008-reaction-lifecycle`; base `8108ccd40f84d12135f17290c25bf3d268efbd8f`; observed HEAD `94a4ecd99054d11a36350d9ec8fba55c9b5363bb`. Explicit target is five files in `task011-source.sha256` plus current README, roadmap and tech-design scope docs. All five SHA-256 checks passed. Initial dirty path was task011-checks.md; not read. Other planning artifacts in base diff excluded from source review.

Read bootstrap first, then independent-review skill, canonical Task011/010/012 and Checkpoint E plan, Round2 contract, source and tests without author explanation. Local exact reads and scoped searches only; incidental filename inventory did not read excluded content.

- No actionable defect identified in blind pass; execution still pending.
- Authority: AuthenticateBase replays separately trusted predecessor inputs/events, validates retained Created and C3a boundary, requires selected defender-decline at step3. Round replay separately serializes trusted inputs and compares exact regenerated bytes; event.input never authenticates itself.
- Clock/privacy: immutable opening floor, budget and deadline; gate ignores opposing seal time. Own allocation/actor/context checks precede clock cancellation. Both seal orders/private4000 then3500 represented. 1999 opening after older RBA2001 deliberately supported by v2 contract.
- Recovery: command-and-owner exact retries precede status/clock gates; malformed timestamp still rejected before retry. Prepared completes only Force Assignment/Anti-Armor; cancellation closes three remaining steps; commitment throws Task012 exclusion. World/RNG retained, no debit/history writes.
- Grammar: raw Base/state syntax before predecessor/context reads; raw event grammar before trusted input indexing. Closed shape, canonical bytes, UTC, depth32, arrays512 and accepted-events16 bounded. Existing C3a grammar reused through narrow helper.
- Tests explicitly assert 10 Base2 values, 54 events, 64 state cuts, four excluded commitment suffixes; clock192/retry96 outcomes; changed retries, foreign actors, rehashed effects, malformed raw/context sentries and collection ownership covered. Must execute independently.
- Plan: Task011 precommit boundary coherent with Task010 synthetic provenance and Task012 debit/commit; public revision/projection, positive campaign provenance, host publication and extended Snapshot12 remain separate gates. README/roadmap say implementation and verification underway; tech-design states bounded implementation. Canonical plan still places011 next pending acceptance, consistent with review stage.
- Remaining checks: independent rebuild; CombatSealsTests, shared010B CombatStepsTests and CombatIdentityTests; final source-manifest check; author-claim reconciliation; close reviewer .NET processes.
