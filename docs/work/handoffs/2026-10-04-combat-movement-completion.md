# CMB-018D released-I Movement completion

Status: implementation, local gates and independent review complete; publication pending. Parent017–019 open.
Coordinator: `01a0c9dc-00bc-78a3-800d-3cb36859e422` (local).

## Scope and repository

Worktree: `/Users/dsteele/.codex/worktrees/combat-movement-completion/sandtable`.
Branch: `codex/combat-movement-completion`. Base: `2e17f60767cceff6db950db532d9432f5b81a9cd`.
PR146 merged before worktree creation; this PR targets main. No merge performed.

Primary paths: completion engine/models and codec in `src/Cna.Core/Campaigns`,
completion tests in `tests/Cna.Core.Tests/Campaigns`, Core test fixture link, and canonical
Combat implementation plan. README, tech-design, naming overview, roadmap and movement delivery
plan retain corresponding administrative status. Existing frozen contracts/fixtures are unchanged.

Both exact frozen3l source histories replay independently. Authority28→29 owner stop binds actual
track and move receipt;30 System empty resolution resumes Movement;31 owner completion reaches
Breakdown Determination. Current World locations and retained original units derive ordinal2 proof;
D2b.2 expiry semantics atomically bind the matching pending exception to the accepted receipt.
World/resources/RNG/attack history/track are preserved. No Breakdown execution or public activation.

## TDD and verification

RED: `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class '*CombatInheritedReserveMovementCompletionTests' /bl:/private/tmp/cmb018d-red.binlog`:
2 failed,0 passed,0 skipped (`NotImplementedException` at the new replay seam).
Initial GREEN: same filter with `/bl:/private/tmp/cmb018d-green3.binlog`:2 passed,0 failed/skipped.
Both owners match frozen base, four state frames, three events, receipt IDs and terminal prefixes.
Expanded focused GREEN: `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatInheritedReserveMovementCompletionTests' --filter-class '*CombatInheritedReserveMovementTests' /bl:/private/tmp/cmb018d-focused-final2.binlog`:
18 passed,0 failed/skipped,2m44.120s. Boundary: `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-trait Boundary=UserSpace /bl:/private/tmp/cmb018d-boundary.binlog`:
81 passed,0 failed/skipped,24.081s. Build: `dotnet build Sandtable.slnx --no-restore /bl:/private/tmp/cmb018d-build.binlog`:
exit0,0 warnings/errors. Format: `dotnet format Sandtable.slnx --verify-no-changes --no-restore`:
exit0. `git diff --check` and new handoff-link validation passed.

All unchanged frozen oracles passed:

- `python3 -B docs/specs/verify-combat-inherited-reserve-movement-completion-v1.py`:2 traces,6 events,16 readbacks,6 retries,1626 mutations,60 raw rejects,58 boundary rejects.
- `python3 -B docs/specs/verify-combat-inherited-reserve-movement-v1.py`:2 traces,8 readbacks,2 retries,1392 mutations,24 raw rejects,11 boundary rejects.
- `python3 -B docs/specs/verify-combat-inherited-movement-lifecycle-v1.py`:8 traces,24 events,32 cuts,24 retries,946 mutations,152 raw rejects,304 boundaries,16 source pins.
- `python3 -B docs/specs/verify-combat-cycle-control-v1.py`:19 cases,64 traces,164 cuts,1748 mutations,700 raw rejects,43 boundaries,216 cost coordinates.

Fresh GPT-6.1 medium independent review instance1 of3: no actionable findings; Ready conditional
on final focused/full gates passing against unchanged reviewed source. Focused and full conditions fulfilled with reviewed executable hashes unchanged. [Review report](../reviews/2026-10-04-combat-movement-completion-review.md).
Reviewed source/tests/project hashes matched after focused gate. No subsequent behavior changes.

Retained failed checks: initial compile namespace/nullable-tuple/analyzer errors were corrected;
expanded initial10-case run had8 pass/2 test-only route-reference comparison failures, fixed to ordered
contents. An interior-wildcard class filter selected zero tests; final two explicit class filters above
ran18 successfully. These do not replace RED or final gate evidence. Binlog names are unique;
MTP also writes corresponding `*-dotnet-test.binlog` records for native no-build test invocations.

## Decisions and limitations

Completion is a bounded dormant profile-specific adapter, following the existing source/codec/replay
pattern. Exact terminal hashes supplement full source replay; they never authenticate caller bytes
alone. Existing first-cycle lifecycle envelopes are not reused because3l retains track/receipt rather
than its richer route. The unchanged Python D2b.2 expiry semantics are ported within the owned engine;
no existing C# expiry helper exists. Preserve exclusion monotonicity using prior and current locations.

Serena exact worktree activated and symbol retrieval verified. Codebase Memory project
`sandtable-cmb018d`, full generation2026-10-04T21:55:51Z; consulted predecessor paths metadata_match,
with no recorded issue. Known unrelated partial ranges are not used. `.serena/` is untracked tooling
state and excluded from commit.

## Next action

Publish the reviewed branch and report to coordinator. Then bound settled-source/progress
integration before later-II/consumed provenance and parent017–019 reconciliation. Public020–021,
Runner022–024, closeout025 and durable HOST-PUB-001 remain separate.

## Reviewed executable hashes

```text
ee8f2043e9e6d2bf5f3149ef4b5f34ea5cfadfc5e002e9c9d5936d700582d3e4  src/Cna.Core/Campaigns/CampaignCombatInheritedReserveMovementCompletion.cs
d5091cd3c643446b20261e5102b832190575a8aff65ec8ef945078af0b8541f2  src/Cna.Core/Campaigns/CampaignCombatInheritedReserveMovementCompletionCodec.cs
4ed160a8c52534a8cda859f69b77339850459f479064743d1d154f593077a166  tests/Cna.Core.Tests/Campaigns/CombatInheritedReserveMovementCompletionTests.cs
a1375dbe9bf8f43d9e13489524d92e45c0bb99e2912d1c2d2495ef3d558e38f7  tests/Cna.Core.Tests/Cna.Core.Tests.csproj
```

## Final local gate reconciliation

`dotnet test --solution Sandtable.slnx --no-build --report-xunit-xml --results-directory /private/tmp/cmb018d-full-results /bl:/private/tmp/cmb018d-full.binlog`:
exit0;2,475 passed,0 failed/skipped;9m47.395s (Debug/net10.0/arm64). Core1,996,
ExerciseRunner469 and Intelligence.Contracts10 cases all passed. Result XML reconciles exactly10
new completion cases. Concurrent focused/Boundary runs overlap the full run; this duration is
verification evidence, not a controlled performance comparison.

Independent review Ready condition is fulfilled by passing final focused/full gates and unchanged
reviewed executable hashes. No findings or code changes required; only evidence/publication metadata
was subsequently refreshed. No second review instance required.

No blocking unresolved contract question. Full gates prove this bounded slice; parent milestones
and future profile admission remain open. No merge authorized or performed.
