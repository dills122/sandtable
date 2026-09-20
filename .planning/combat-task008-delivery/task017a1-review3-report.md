# Task017A1 independent review

Review instance: 3 of 3.

Candidate `77ef166f03674a7e5875acb8c1ebfde36b0d406a`; base `afdcd9e`. Independently cloned and checked out detached at `/tmp/sandtable-task017a1-review3-1930` (macOS physical path `/private/tmp/sandtable-task017a1-review3-1930`). Tracked checkout clean before and after checks. No source modifications, commits, subagents, CCE, memory, prior reviews or worker/check/developer evidence used. Neutral bootstrap inspected first; own preliminary ledger persisted before reading author packet. Used independent-review skill in reviewer mode.

## Findings

No actionable findings.

Reviewed four source/test paths against frozen contract and relevant immutable carriers. `CampaignCombatReserveReleaseModels.cs` copies member/attack enumerables to read-only arrays; nested records/UnitKeys/CP/RNG values expose no mutable state. `CampaignCombatReserveReleaseCodec.ReadBase` checks raw closed shape, fixed field order, ASCII atom/hash spelling, exact numeric spelling, bounds and reduced rational grammar before touching trusted values. Complete bytes must equal canonical serialization of independently expected base under retained creation context. Returning that owned immutable base preserves supplied trust boundary.

`Validate` binds campaign/rules/setup/content/scenario/configuration, actual side/relative slot and official Release position; validates sorted unique own members, first-I/later-II history, retained release ceilings, prior release ordinal, expired immediate-next exception and referenced offensive attacker/scope. Unreferenced history remains typed input data; this is deliberately not a replay proof. No game transition, remote I/O, authority registration or publication added.

## Plan Review

Canonical Task017 refinement inspected before implementation. Four-path source/test diff matches A1 manifest; full candidate additionally contains 13 planning/documentation/status paths. Documentation consistently keeps native lifecycle, World adapters and actual positive lineage open. No frozen spec/schema/fixture/oracle changes against base.

All A1 requirements have code and executed focused evidence: immutable scope/history/exception/member/base carriers; context-checked complete canonical readback; exactly 44 isolated base hashes, with four historical settled Result1 rows explicitly counted/excluded. Test builder uses retained Created11/Content7 plus Result2 cycle template and literal recipes, not expected hashes as construction inputs. Dormant isolation matches agreed bounded plan. A2, B, positive predecessor/bridge, public admission, Task018/019 and parent017 completion remain outside verdict.

## Author-Claim Reconciliation

| Author claim | Independent evidence | Status / consequence |
| --- | --- | --- |
| Four source/test paths; frozen contract artifacts unchanged | Git diff paths and exact contract diff check | Confirmed; scope bounded |
| Collections and nested values immutable | Models constructor, reused UnitKey/CP/RNG/cycle/attack carriers, ownership fact | Confirmed |
| Raw canonical checks precede trusted context | CheckRaw/Fields/scalar readers; null-trust malformed tests including attack CycleId hash | Confirmed |
| Full independently expected bytes checked under retained creation | ReadBase and SerializeBase/Validate; leaf-forgery and foreign-context tests | Confirmed; caller trust remains explicit |
| 44 isolated hashes and four historical exclusions | Independent build and six Release facts; recipe inspection | Confirmed; no campaign provenance inferred |
| Legal first/later/released history and preserved overspend | Validate vs frozen oracle validate_base; history and structural-bound facts | Confirmed |
| Worker 20-test result, earlier RED and development fixes | Not consumed/reproduced as historical evidence | Unverified historical claims; readiness instead based on current independent build/checks |
| No lifecycle/event/state or actual positive lineage proof | Code surface and plan exclusions | Confirmed limitation |

## Verification Performed

All commands below run with `login:false` from own fresh clone. Restore/build/format/tests used approved MSBuild/test IPC access. Logs and binlog remain in clone.

1. Initial `dotnet restore Sandtable.slnx > restore-review3.log 2>&1` stalled without output under sandbox. Sandbox process inspection denied. Approved inspection identified owned PID31823; terminated only that PID with TERM, then retried. Initial session ultimately returned exit0 after termination; it is not counted as successful restore evidence.
2. `dotnet restore Sandtable.slnx --disable-build-servers > restore-review3-approved.log 2>&1` — exit0; all 11 solution projects freshly restored.
3. `dotnet build Sandtable.slnx --no-restore --disable-build-servers -p:UseSharedCompilation=false -bl:review3-build.binlog > review3-build.log 2>&1` — exit0; FULL solution independently rebuilt from fresh source, zero warnings/errors, 14.59s.
4. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class Cna.Core.Tests.Campaigns.CombatReserveReleaseBaseTests > review3-focused.log 2>&1` — exit0; 6 passed, 0 failed/skipped, 1.741s. Exact class selected directly; no zero-test wildcard mistake.
5. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class Cna.Core.Tests.Campaigns.CombatSealsTests --filter-class Cna.Core.Tests.Campaigns.CombatCustodyTests --filter-class Cna.Core.Tests.Campaigns.CombatReserveCompletionTests --filter-class Cna.Core.Tests.Campaigns.CombatCreationInputsCodecTests > review3-shared.log 2>&1` — exit0; 40 passed, 0 failed/skipped, 23.877s.
6. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-trait Boundary=UserSpace > review3-boundary.log 2>&1` — actual justfile trait; exit0; 81 passed, 0 failed/skipped, 9.807s.
7. `python3 docs/specs/verify-combat-reserve-release-v1.py > review3-oracle.log 2>&1` — exit0; 13 literal cases/48 side-slot traces, 188 cuts, 2368 mutations, 840 raw rejects, 20 timing and 27 boundary checks. Frozen Python contract coverage is broader than A1 C# coverage; does not extend A1 claim.
8. `dotnet format Sandtable.slnx --verify-no-changes --no-restore --include src/Cna.Core/Campaigns/CampaignCombatReserveReleaseModels.cs src/Cna.Core/Campaigns/CampaignCombatReserveReleaseCodec.cs tests/Cna.Core.Tests/Campaigns/CombatReserveReleaseBaseTests.cs > review3-format.log 2>&1` — exit0; empty log.
9. `git diff --check afdcd9e HEAD` — exit0. `git diff --quiet afdcd9e HEAD -- docs/specs/combat-reserve-release-v1.md docs/specs/combat-reserve-release-v1.schema.json docs/specs/fixtures/combat-reserve-release-v1.json docs/specs/verify-combat-reserve-release-v1.py` — exit0. Final tracked status empty, HEAD unchanged.

Independent SHA256 pins:

- Models: `a98fa668141a756b1034a99984130af3be46af860651ab6315a57c6837799c2e`
- Codec: `54b24b57901e6778596bf7c0ab5b31633f031cf8cac7ab7ae4a35b7a35e8a678`
- Tests: `196e4e00a133f4054927a3a578b620e7a618667973d4f2619cc24d36f2a97d6a`
- Test project: `ae58b5752e0e2fe6d759e7f4eda79b237746e6dbb1ddffd969979d49727a8ce8`

## Open Questions And Residual Risks

No blocking question. Complete expected model is an explicit independent trust input; candidate-derived expected data defeats authentication. Codec neither proves actual retained World history nor supplies lifecycle behavior. Frozen Python traces include historical cases expressly excluded from C# scope. Full solution test suite and full-solution formatting were not rerun by reviewer3; focused/shared and Boundary coverage is proportionate to isolated four-path change. Root acceptance gates remain separately required.

Every owned command session completed. Targeted process inventory after checks showed no reviewer-clone processes or residual MSBuild/VBCSCompiler servers. Only stalled owned restore PID was explicitly terminated; no global shutdown or other task process touched.

## Verdict

**Ready** for bounded Task017A1 candidate. Evidence supports immutable isolated base/history codec and 44-base parity only. No parent017 acceptance or follow-on dispatch implied.

## Recommended Next Actions

Return verdict to initiating task for root gate reconciliation. Review budget exhausted at 3 of 3; do not start another review instance or advance lifecycle work automatically. No remaining finding requires remediation.
