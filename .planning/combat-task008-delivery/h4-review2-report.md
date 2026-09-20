Review instance: **2 of 3**  
Target: `codex/combat-task008-reaction-lifecycle`, base/HEAD `ca43442edaebd21a812733207f49dd5f435fd4a9`, five working-tree paths pinned by `h4-source.sha256`.

## Findings

**No actionable implementation findings.**

Blind-first ordering preserved: inspected requirements, schema, implementation, tests, and supporting docs; recorded preliminary ledger in commentary before reading author/checks packets. No prior review reports, `execution.md`, or aggregate evidence read. No files changed, agents spawned, or `dotnet` commands run.

Key evidence:

- [Restore](/Users/dsteele/repos/sandtable/src/Cna.Core/Campaigns/CampaignCombatInheritedSnapshotV12Codec.cs:13) requires retained Created11, invokes actual admission decision, rejects publication, replays retained history, and compares complete canonical bytes.
- [Composition](/Users/dsteele/repos/sandtable/src/Cna.Core/Campaigns/CampaignCombatInheritedSnapshotV12Codec.cs:28) derives authority exclusively from creation and typed replay writers. Checks bind configuration, creation, identity, complete receipt ledger, event hashes, versions, and prefix.
- [State mapping](/Users/dsteele/repos/sandtable/src/Cna.Core/Campaigns/CampaignCombatInheritedSnapshotV12Codec.cs:56) preserves Reaction positions, suspended Movement, resolved windows, closed flows, and inherited evidence. Defaults require causal absence checks.
- [Bounds](/Users/dsteele/repos/sandtable/src/Cna.Core/Campaigns/CampaignCombatInheritedSnapshotV12Codec.cs:107) enforce structural limits before history access. Larger array allowance follows actual root `world.cohesionCauses` structure; dotted names cannot impersonate that path.
- [Tests](/Users/dsteele/repos/sandtable/tests/Cna.Core.Tests/Campaigns/CombatInheritedSnapshotTests.cs:11) compare exact frozen bytes and exercise disabled-admission restore across every selected history, plus canonical tampering, lawful foreign forks, forged causal RNG, missing evidence, bounds, and defensive ownership.

## Plan Review

**No blocking plan findings.**

Implementation satisfies bounded H4 responsibilities in [canonical plan](/Users/dsteele/repos/sandtable/docs/design/combat-cycle-implementation-plan.md:834): literal nineteen-field roots and retained-history restore across implemented Initial H cuts.

Dependency order remains sound: H1–H3 supply causal routing; H4 supplies root composition and restore. Existing creation-only reader remains separate. No schema migration, public endpoint, new service, or gameplay activation introduced.

README, technical design, and naming overview describe H4 consistently as active work. Later 28-trace runtime closure, Tasks009–019 composition, public admission, and `HOST-PUB-001` remain explicitly open. This review does not close those gates or remaining review round.

## Author-Claim Reconciliation

| Claim | Evidence | Status / consequence |
|---|---|---|
| Exact 368 roots, 286 histories, 62 shared groups | Fixture consistency check; exhaustive test assertions; focused log | **Confirmed.** Reviewer independently checked fixture counts and shared-byte equality; runtime execution supported by retained log. |
| All selected cuts restore with admission disabled | Exhaustive test passes `false`; Restore invokes `CampaignCombatCreationCut.Decide` | **Confirmed.** Actual policy seam exercised. |
| Missing Created11 never regenerated | Explicit guard before capture/decision; tests cover both admission values | **Confirmed.** No fresh-creation fallback. |
| Snapshot/cache cannot confer authority | Private composition consumes replay result; supplied root only bounded and compared | **Confirmed.** Trusted history remains authoritative input. |
| Complete causal state and ledger preserved | Mapping, prefix/hash checks, frozen comparisons, targeted negatives | **Confirmed.** No family-local ledger truncation found. |
| Creation compatibility preserved | C2 template; creation byte equality; legacy rejection tests | **Confirmed** for inspected scope. |
| Bounds and ownership preserved | Structural walker, retained-history capture, sentry and mutation tests | **Confirmed.** Boundary checks distinguish rejection from accidental history access. |
| Focused and full suites pass | Raw retained test logs | **Confirmed as retained execution evidence**, not reviewer execution. |
| Full format check exited zero | Empty format log; exit status stated in checks packet | **Unverified independently.** Empty log alone cannot establish exit status. |
| Durable publication remains outside H4 | Code surface and canonical plan | **Confirmed.** No durability claim inferred. |

## Verification Performed

All shell calls used `login:false`.

- `git branch --show-current` and `git rev-parse HEAD`: matched bootstrap.
- Path-scoped `git status --short` and `git diff`: two new untracked files; three existing test files modified only by history enumerators.
- `shasum -a 256 -c .planning/combat-task008-delivery/h4-source.sha256`: **all five matched**, checked twice.
- Path-scoped `git diff --check`: no whitespace diagnostics. This check covers tracked diff, not untracked file contents.
- Read-only `python3 -B -c` fixture assertions: **passed**. Checked exact nineteen-field order, canonical encoding, lengths, hashes, complete receipt hashes/versions, null `combatState`, and identical roots for shared histories. Inventory includes all six flow kinds, three position kinds, and both non-null cycle arms. This was fixture validation, not runtime codec execution.
- Inspected raw retained logs:
  - `/tmp/h4-focused-freeze.log`: **11 passed, 0 failed, 0 skipped**, 14.930 seconds.
  - `/tmp/h4-build.log`: **build succeeded**, zero warnings/errors.
  - `/tmp/h4-suite.log`: **2,244 passed, 0 failed, 0 skipped**, 6m47s343ms.

Recorded focused command, **not rerun**:

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatInheritedSnapshotTests' '-bl:/tmp/h4-focused-freeze-{}.binlog'
```

Tool limitations: CCE search and post-ledger session recall required unavailable approval. Exact-path reads substituted for search. Initial Python here-document failed because sandbox prohibited temporary-file creation; equivalent `-c` invocation passed. Git emitted sandbox cache warnings but returned requested results.

## Open Questions And Residual Risks

- No independent runtime execution this round; conclusions combine source inspection, fixture checks, and retained execution logs.
- JSON mapping depends on thirteen typed writer shapes. Future families must explicitly extend mapping and golden coverage.
- History remains bounded to current profile; this proves neither unrestricted campaign retention nor durable archive authentication.
- Boundary-gate completion and format exit status were not independently established here.

## Verdict

**Ready — bounded H4 implementation and plan.**

No correction or architectural pivot requested. Verdict excludes parent Task008 completion, durable publication, later gameplay, and remaining delivery gates.

## Recommended Next Actions

Root retains ownership of boundary-gate evidence, format exit status, and third independent review. Preserve explicit Initial H and publication exclusions when recording acceptance.