# Task008 C Weather independent review

Review instance: 2 of 3.

## Preliminary ledger — before author explanation

Target verified: branch `codex/combat-task008-weather`, base/HEAD `22f8da6d0399c90c2d333ce27a4ea37193da343e`; explicit working-tree delta includes three untracked implementation files and Weather tests, project fixture inclusion and documentation updates.

Inspected Weather specification, stage-entry handoff, Task008 execution-index diff, implementation/models/codec, tests and predecessor replay. No actionable defect identified in first pass. Complete four-event predecessor reconstruction, exact reserialization rejection, actor authorization before retry, existing Weather resolver and Rules artifact/source binding match requirements. Tests target all frozen chains, both cuts/orders, changed retries, forged weather/cache, mutation rejection and caller isolation.

Pending verification: focused compiled tests, canonical Python oracle, retained artifact schema, author claims and source/build correspondence. Positive effects/public activation/general Snapshot12 correctly remain excluded by canonical scope. No prior review report intentionally read. CCE search returned report filename/header and first scope paragraph, and author packet heading/brief intent before this ledger; no prior findings or author rationale were inspected.


## Findings

No actionable findings. Replay derives state5 from trusted request, Created11 and exactly four accepted B1 events (`CampaignCombatWeather.Replay`), resolves original Rules/RNG behavior (`Emit`), and compares complete event bytes. No cache, supplied cursor/prefix or coherent re-signing can bypass recomputation. Authorization precedes retry lookup. Nested Weather values copy their area arrays; retained input buffers do not become state authority.

## Plan Review

Task008 C delivers the stated retained dice/RNG/source/context parity through state6. B1 → C → B2 ordering is necessary: canonical stage-entry consumes real accepted Weather, preserving five predecessor receipts and all outcomes. Five primary files match scope; navigation/design updates accurately describe dormant Organization-entry completion. Positive effects, later turns/stages, Reserve, generic Snapshot12, authenticated ingress and HOST-PUB-001 are explicit canonical exclusions with later owners, not concealed C acceptance gaps. No architecture pivot or plan expansion required.

## Author-Claim Reconciliation

| Claim | Evidence | Status/consequence |
| --- | --- | --- |
| 34 chains, 68 cuts, 306 frozen fingerprints | `FrozenChainsMatchAllBytesOutcomesReceiptsAndBothCuts`, fixtures, focused test pass | Confirmed: nine retained records per chain and two state cuts |
| Genuine predecessor authority and Rules binding | `Replay`, `RequireWeatherAuthority`, B1 `Replay`, frozen schema hash and artifact test | Confirmed |
| All outcomes and both orders retained, Axis determines | Existing resolver, typed Weather constructor, all fixture rows and 12 foul-pair assertion | Confirmed |
| Exact retry leaves state/RNG unchanged | `Apply`, per-chain retry state/event equality | Confirmed |
| Coherently forged Weather/cache reject | `CoherentlyResignedInventedWeatherAndCacheStillReject`, full event comparison | Confirmed |
| Legacy/Snapshot12 readers reject | Per-chain reader-negative assertions | Confirmed |
| Build/full suite/format completed | `/tmp/c-build.log`, `/tmp/c-suite.log`, evidence packet; source fingerprints match | Build log shows zero warnings/errors; full log shows 1,928 passing, none skipped. Format log empty, author exit0 retained; not independently rerun |
| RED/GREEN history | Author packet/evidence only | RED failure not independently rerun; current GREEN independently confirmed |

## Verification Performed

- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatWeatherTests' '-bl:/tmp/c-review2-{}.binlog'`: initial sandbox IPC bind denied (exit134); same authorized command outside sandbox passed42, failed0, skipped0, 4.465s. Successful binlog `/tmp/c-review2-20260919-220619--54146--R9aIlI-dotnet-test.binlog`.
- `shasum -a 256 -c .planning/combat-task008-delivery/c-source.sha256`: all five source/project fingerprints OK.
- `git diff --check`: pass.
- SDK10.0.400; native MTP in global.json; SDK-style net10.0 project; xUnit v3 MTP package. No builds performed by reviewer.

## Open Questions And Residual Risks

No open C correctness question. This is dormant, bounded replay evidence: production authentication/published-head durability, larger histories and positive effects remain separately gated. Existing built outputs were used as instructed; matching frozen source hashes plus retained build logs and focused results support correspondence, but reviewer did not rebuild. CCE preview exposed author title/intent and prior report scope header before ledger; no previous findings were read. Author evidence packet later included prior round verdict in its status table; this was encountered only after preliminary conclusions.


Oracle check: `PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-weather-v1.py` passed34 creation-rooted traces,68 replay/state cuts,11,516 leaf mutations,921 raw rejections,1,651 boundary/retry checks and3,960 Rules coordinates. Contract evidence complements independently passing C# checks.

## Verdict

Ready.

## Recommended Next Actions

Lead may proceed to configured third independent review. No remediation or additional workstream requested. Keep Task008 parent, B2 and production-publication gates open.
