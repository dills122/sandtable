# Task017C positive Reserve Release evidence

Baseline: merged PR140, 1a8b35ba9490afd853ca130b54266a3fb89bf10e.
Branch: codex/combat-inherited-reserve-release-bridge.
Worktree: /Users/dsteele/.codex/worktrees/combat-reserve-release-bridge/sandtable.
Scope: first actual held-I release; parent017 remains open for later-II/consumed lineage.

## Checks

- RED: initial2 actual-history opening cases compiled and failed at explicit unimplemented bridge. /tmp/resrel-bridge-red.log.
- GREEN: initial2 exact native opening hashes passed. /tmp/resrel-bridge-green.log.
- Expanded first run:14/16 passed; expiry retry assertion expected NoOp but native receipt identity correctly returns Duplicate. Assertion corrected and original bytes checked; no production change needed.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class '*CombatInheritedReserveReleaseTests' /bl:/tmp/resrel-bridge-focused-{}.binlog`:16 passed,0 skipped,13.769s.
- `dotnet build Sandtable.slnx --no-restore /bl:/tmp/resrel-bridge-build-{}.binlog`: clean,0 warnings/errors.
- `dotnet format Sandtable.slnx --verify-no-changes --no-restore`: passed.
- `python3 docs/specs/verify-combat-inherited-reserve-release-v1.py`: PASS2traces/6events/8cuts/6retries/658mutations/24raw rejections/31boundaries/10recovery paths/7source pins. Existing oracle, not new runtime coverage.
- An incorrect `Category=Boundary` filter ran0tests and failed; not acceptance evidence. Correct repository `Boundary=UserSpace` run passed81/81,0skipped,22.608s.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-trait 'Boundary=UserSpace' /bl:/tmp/resrel-bridge-boundary-correct-{}.binlog`: passed.
- Independent review1: Ready, no actionable findings; report retained in bridge-review-report.md.
- Initial full solution run interrupted before Core result; contracts and ExerciseRunner had passed. Not counted as full acceptance. Restarted unchanged source with `dotnet test --solution Sandtable.slnx --no-build /bl:/tmp/resrel-bridge-full-resumed-{}.binlog`; PASS2,377/2,377,0failed/0skipped,10m01.831s. Log /tmp/resrel-bridge-full-resumed.log.

## Frozen implementation SHA256

- `src/Cna.Core/Campaigns/CampaignCombatInheritedReserveRelease.cs`: `337a1c10d2686615d9149871fa94ac3eba8efeb09e468f5ef2667c7fdab96803`
- `src/Cna.Core/Campaigns/CampaignCombatReserveReleaseCodec.cs`: `b9684ae0c0affc37485e7e1d15f45d52484bdc588513638999f1cacd7cbe2edc`
- `tests/Cna.Core.Tests/Campaigns/CombatInheritedReserveReleaseTests.cs`: `ba0dd27acd78b84cec4db1d7be69b4dc47f87bd59a42bd6d7f0376b0097075ab`
- `tests/Cna.Core.Tests/Cna.Core.Tests.csproj`: `48259d2aed3b8ee097abac1195d379c98e8f691d5046af7b00f3e1a8b5482ff6`

## Runtime evidence and limits

Both owners reproduce2 predecessor/base identities,6 event hashes,8 wrapper Control frames and2 terminal event literals. Replay/retry all positive cuts; deep World equality permits only matched Reserve status. Recovery covers opening clock loss, choice clock loss/regression, backend unavailable and expiry. Raw/leaf mutations, re-signed event, wrong owner, stale command, forged cache, missing/reordered/foreign predecessor reject.
No frozen fixture/schema changed. No public gameplay, Snapshot/history dispatcher, cycle repeat or later-II/consumed lineage activation. Completion stays ordinal1 at same Release position.

Final delivery gates passed. Reviewed implementation SHA256 values unchanged. Independent review condition satisfied by resumed full suite. No exact-candidate CI claim before publication.
