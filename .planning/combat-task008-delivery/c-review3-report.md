# Task008 C Weather independent review

Review instance: 3 of 3. Target: branch codex/combat-task008-weather, HEAD 22f8da6d0399c90c2d333ce27a4ea37193da343e plus explicitly listed working-tree delta.

## Preliminary ledger — before author explanation

- Independently read canonical Weather and stage-entry requirements, Task008 execution index, all three new implementation files, focused tests, and tracked documentation/project diff. CCE used for retrieval and historical contract recall; no prior review reports read.
- No actionable defect identified in blind pass. Replay reconstructs trusted Created11 and all four accepted opening events, derives RNG and Initiative holder, re-emits event bytes, and rejects any noncanonical mismatch. Retry authorizes System before accepted-input comparison.
- Existing Weather artifact hash and manifest sources bind authority. Explicit Setup policy guards zero immediate effects. Source-derived resolver retains every Weather kind and advances to Organization, satisfying C rather than stage-entry B2.
- Tests cover 34 literal fixture traces, both replay cuts, all foul/location pairs, ActLast, RNG rejected bytes, changed retry/actor/history/cache and coherent re-signed invention. Verification pending at ledger creation.
- Plan ordering B1→C→B2 preserves genuine predecessor provenance; generic Snapshot12/publication/activation remain explicit later gates. No scope expansion required.
- Initial focused no-build invocation blocked by sandbox named-pipe bind (SocketException 13); not a failing test. Oracle running.

## Findings

No actionable findings. Contract parity, predecessor provenance, unchanged Rules/RNG reuse, canonical rejection, atomic caller-buffer behavior, and scope boundaries support acceptance.

## Plan Review

Task008 C meets its bounded state5→6 objective. `CampaignCombatWeather.Replay` derives opening from request/Created11/four records, preventing a standalone cache from establishing authority. `Emit` consumes existing Rules/RNG and retains World/order/holder through Organization. B2 can consume exact accepted Weather event and reconstruct state6; no synthetic predecessor needed. B parent, B2, D–H, general Snapshot12, public registration, and HOST-PUB-001 remain open. Excluded positive immediate effects are prohibited by this frozen setup policy, not omitted accepted behavior. No architectural pivot or extra workstream required. Current C row remains in-progress pending normal lead closeout.

## Author-Claim Reconciliation

| Claim | Evidence | Status / consequence |
| --- | --- | --- |
| Genuine complete predecessor reconstruction | `CampaignCombatWeather.Replay`, accepted `CampaignOpeningPreamble.Replay` and creation snapshot call | Confirmed; no supplied prefix/version/cache authorizes transition |
| Rules artifact/source and explicit effect absence | `RequireWeatherAuthority`, `Cna1979Weather.Resolve`, `WeatherEventFactory.GetSources`, artifact test | Confirmed; existing authority reused |
| All outcomes and both orders retain determining holder | 34-case theory with frozen Weather hashes, literal payload expectations, all12 foul/location pairs | Confirmed; ActLast still uses Axis |
| Exact canonical events/state/receipt/prefix | Codec field order against schema; 306 fixture fingerprints; receipt/prefix formulas; forged-history tests | Confirmed |
| Authorization before retry and no extra transition | `Apply`, per-trace wrong-actor checks before/after acceptance, byte-equal retries | Confirmed |
| Caller bytes not retained as authority | Replay copies event records; buffer-isolation test overwrites supplied/output bytes after replay | Confirmed |
| Build, format, full-suite checks passed | Retained evidence; `/tmp/c-build.log` zero warnings/errors and `/tmp/c-suite.log` 1,928 passed/0 failed/skipped inspected | Build/full suite confirmed from retained logs; format claim retained, not independently rerun |
| Five-primary-file scope and unchanged predecessors | Git status/diff, five-file SHA-256 verification | Confirmed; documentation synchronization separate from runtime scope |

## Verification Performed

- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatWeatherTests' '-bl:/tmp/c-review3-{}.binlog'`: initial sandbox invocation terminated before tests because local named-pipe bind denied. Same command with approved escalation passed **42**, failed0, skipped0 (4s846ms). No build performed. Successful binlog: `/tmp/c-review3-20260919-220958--54527--dSdbkS-dotnet-test.binlog`.
- `shasum -a 256 -c .planning/combat-task008-delivery/c-source.sha256`: all five files OK.
- `git diff --check`: exit0.
- Reviewed canonical Weather schema/spec, opening/creation/sequence contracts, stage-entry successor requirements, focused tests, implementation, surrounding Rules/RNG/source factory, tracked documentation/project diff, and Task008 plan.

## Open Questions And Residual Risks

No blocking questions. No-build execution relies on retained build; matching source fingerprints and inspected successful build/full-suite logs support that target. C alone proves neither later-stage recovery nor durable publication or authenticated ingress. Frozen first-turn/stage profile deliberately rejects broader setup/history. Standard bounded full replay has small fixed cost here; generic long-history behavior belongs to later owners. No claim of independent full rebuild/full-suite execution in this pass.

Independent oracle: `PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-weather-v1.py` passed: 34 creation-rooted traces, 68 replay/state cuts, 11,516 leaf mutations, 921 raw rejections, 1,651 boundary/retry checks, 3,960 Rules coordinates. This supports frozen contract evidence, alongside separate C# execution above.

## Verdict

**Ready** for bounded Task008 C Weather slice. No actionable findings against implementation or plan.

## Recommended Next Actions

Lead may close C evidence/status and perform authorized delivery. Continue B2 using genuine accepted Weather replay when scheduled. Review instance3 of3 complete; no further review initiated because no unresolved blocker or heavy pivot remains.
