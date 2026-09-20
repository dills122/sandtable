# F1 Reaction trigger evidence

Branch codex/combat-task008-reaction-trigger, base b9cb26f (G2 PR134).
Canonical scope f-scope.md and f1-dispatch.md; two traces/two events/four cuts/eight artifacts.
Baseline: PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-inherited-reaction-trigger-v1.py
exit0 /tmp/f1-reaction-baseline.log:2traces2triggers4cuts2retries270mutations60raw30boundaries11pins.
Worker final focused15/15 (F1seven+E1eight) /tmp/f1-final.log.
Scoped whitespace and git diff --check pass. Dev source/test review complete; no blocker.
Root integration and final review results below.

TDD RED /tmp/f1-red.log: missing new types, expected before implementation.
Dev test read caught opportunity preimage property typo; canonical key windowId supplied to worker.

Final focused command: dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class '*CombatReactionTriggerTests' --filter-class '*CombatInheritedMovementTests' --no-restore '-bl:/tmp/f1-final-{}.binlog'.
First green1 analyzer CA1822 corrected to immutable auto-properties; green2 six tests passed, then
typed World probe and terminal Command rejection added before final fifteen pass.

Root build exit0 zero warnings/errors /tmp/f1-build.log; format exit0 /tmp/f1-format.log.
Added-line local Markdown links and git diff --check pass. Full suite /tmp/f1-suite.log exit0:2,051 passed,0failed/skipped,3m25s719ms.
Commands: dotnet build Sandtable.slnx --no-restore '-bl:/tmp/f1-build-{}.binlog';
dotnet format Sandtable.slnx --verify-no-changes --no-restore;
dotnet test --solution Sandtable.slnx --no-build '-bl:/tmp/f1-suite-{}.binlog'.
Independent rounds1/2/3 Ready with no actionable findings; all reports retained.
Round1 independently reran canonical oracle; rounds2/3 inspected source and retained logs.
Lead accepts all reports; frozen source unchanged, no conditional experiment/fourth round needed.
