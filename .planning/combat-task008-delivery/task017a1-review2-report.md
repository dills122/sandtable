# Task017A1 independent review

Review instance: 2 of 3.

## Findings

No actionable findings in bounded Task017A1 implementation or child plan. Reviewed candidate `77ef166f03674a7e5875acb8c1ebfde36b0d406a` against `afdcd9e` in clean detached clone `/tmp/sandtable-task017a1-review2-1925`. Blind ledger persisted before reading author packet. No CCE, memory, prior review or worker/check/dev evidence used.

Four runtime/test changes exactly match manifest: Release model, Release codec, six focused tests, fixture link. Remaining thirteen diff paths are planning/status/documentation; relevant README/design/naming/roadmap/site changes retain dormant-scope limits. Frozen spec/schema/fixture/oracle unchanged. Final clone tracked status clean.

## Plan Review

Canonical Task017 refinement correctly limits A1 to immutable isolated base values and expected-context codec. Native lifecycle, commands/events/state, actual retained Result2 bridge, held-I positive predecessor and later-II/consumed campaign provenance remain separate gates. No parent017, public admission, Snapshot, durable publication or future-duty completion inferred.

Reviewed `CampaignCombatReserveReleaseCodec.ReadBase` and `CheckRaw`: raw grammar, complete ordered closed inventory, bounds and numeric/hash types checked before trusted inputs. Reader compares all canonical bytes against separately supplied expected value, then returns immutable expected object. Base constructor owns copied read-only collections; nested cycle/unit/CP/RNG/history/attack values contain no mutable collections. Context and membership checks cover creation/configuration identity, actual acting side, official position, order/uniqueness, positive CPA, first/later Reserve status, retained released ceilings and expired next-Movement history. Linked offensive commitment resolves uniquely to matching attacker and scope. Unreferenced attack entries retain structural-only validation, consistent with isolated contract and without claiming campaign authentication.

Tests independently reconstruct44 isolated bases from retained Created11/Content7/Result2 template and recipe fields, compare frozen hashes, and explicitly exclude four historical settled Result1 rows. Negatives cover complete leaf substitution, canonical framing/order, malformed primitives, invalid history, foreign context, ownership and structural bounds. Frozen oracle inspected directly, including `validate_base`, `typed`, `raw` and `base_for`.

## Author-Claim Reconciliation

| Claim | Evidence | Status / consequence |
| --- | --- | --- |
| Exactly44 isolated hashes; four historical rows excluded | Native golden test independently executed; Build reads recipes, not expected base hashes | Confirmed; no lifecycle inference |
| Immutable owned values and returned expected object | Model constructor, nested value definitions, ownership test | Confirmed |
| Raw canonical checks precede trusted context | ReadBase/CheckRaw/BaseShape; null-context malformed tests and attack-cycle hash regression | Confirmed |
| Retained creation/cycle/member/history constraints | Validate against frozen oracle and negative tests | Confirmed within isolated scope |
| Frozen contracts unchanged | Exact base-to-candidate diff of four frozen assets empty; oracle run passes | Confirmed |
| Prior worker tests, RED and development fixes | Historical testimony not independently replayed | Unverified historical claims; no readiness weight assigned |
| Full gates and review sequence pending | This pass ran only focused/shared/boundary checks | Confirmed limitation; root owns remaining gates |

## Verification Performed

Own SDK `10.0.400` selected under repository `10.0.302` / latestFeature policy. All commands run from own clone with `login:false`; approved network/IPC used after sandbox failure. Logs retained there.

- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class '*CombatReserveReleaseBaseTests' -p:UseSharedCompilation=false -nodeReuse:false`: first attempt failed NU1900 because sandbox could not load NuGet service index. Approved retry built successfully but wildcard selected zero tests (exit5). Neither attempt counted as test pass.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --list-tests`: exit0; discovered six fully qualified Release tests.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class Cna.Core.Tests.Campaigns.CombatReserveReleaseBaseTests`: six passed, zero failed/skipped; 1.563s (`release-exact.log`).
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class Cna.Core.Tests.Campaigns.CombatReserveCompletionTests`:21 passed, zero failed/skipped;2.626s (`shared-cycle.log`), covering shared cycle serialization/context.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-trait 'Boundary=UserSpace'`:81 passed, zero failed/skipped;10.122s (`boundary.log`).
- `python3 docs/specs/verify-combat-reserve-release-v1.py`: exit0;13 literal cases/48 side-slot traces,188 cuts,2368 mutations,840 raw rejects,20 timing and27 boundary checks (`oracle.log`). Python historical contract evidence remains separate from C#44-base claim.
- `dotnet format tests/Cna.Core.Tests/Cna.Core.Tests.csproj --verify-no-changes --no-restore --include src/Cna.Core/Campaigns/CampaignCombatReserveReleaseModels.cs src/Cna.Core/Campaigns/CampaignCombatReserveReleaseCodec.cs tests/Cna.Core.Tests/Campaigns/CombatReserveReleaseBaseTests.cs`: exit0 (`format.log`).
- `git diff afdcd9e HEAD --check`: exit0. Frozen Release spec/schema/fixture/oracle diff empty. `git status --porcelain`: empty.

All eight owned execution sessions completed and exit statuses collected. Process check showed only checking shell/rg for clone path; no remaining clone process. Shared compilation/node reuse disabled for build. No global process shutdown, source edits, commits or subagents.

## Open Questions And Residual Risks

Independently expected base and retained request remain explicit trust inputs; candidate-derived expected object defeats authentication by design. Synthetic receipts/World hashes establish no actual campaign history. Full solution rebuild/test and later review instance remain outside this pass. No unresolved A1 correctness question found.

## Verdict

**Ready** for bounded A1 scope on this pass's evidence. This verdict does not replace remaining root acceptance gates or establish parent017 completion.

## Recommended Next Actions

Root should retain exact candidate for remaining full-solution gate/review and respond to report. Preserve A2/B/positive-history exclusions; no automatic next-child dispatch.
