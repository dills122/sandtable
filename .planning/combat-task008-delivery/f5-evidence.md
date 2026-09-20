# F5 second Reaction move evidence

Base26ff137 accepted F4, branch codex/combat-task008-reaction-lifecycle, main-based PR136.
Actual causal predecessor F2 first participant move at14. Two owner traces/two events/four cuts/
10 artifacts. Baseline /tmp/f5-reaction-baseline.log passed before implementation:
2traces2events4cuts2retries260mutations60raw48boundaries14pins.
Command: PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-inherited-reaction-second-move-v1.py.

Implementation worker owns five primary files per dispatch. Lead dev/full integration checks and
three sequential independent rounds pending. No F5 acceptance claim yet.

Lead independently checked canonical fixture source hashes and two case inventory before source
review. F4 remote checks at26ff137 all passed, including CI verify4m23s, CodeQL, dependency review,
and offline links. No changes to accepted F4 sources.

Worker reports initial sandbox RED /tmp/f5-red.log cancelled after MSBuild IPC stall; it is not
missing-type evidence. Escalated /tmp/f5-red-ipc.log records actual missing SecondMove types CS0246.
First green /tmp/f5-green1.log failed compilation on omitted local Cp/WriteUnit codec helpers;
worker correcting. Lead preliminary dev review of models/projector/codec found history reconstruction,
actor-before-retry and own whole-World guard intact; requested removal of copied unreachable F2
position/window branches and accurate second-move diagnostics before freeze. Expanded tests pending.

/tmp/f5-green2.log failed xUnit2031 analyzer: Assert.Single must take predicate directly rather than
Where filtering. Worker corrected test harness. /tmp/f5-green3.log passed initial four tests,
0failed/skipped,2s250ms: both-owner frozen bytes and literal transition/retention. Expanded negatives
still pending; no full gate/review acceptance yet.

Final focused /tmp/f5-final-focused.log:22passed(13F5+9F2),0failed/skipped,40s142ms.
Scoped format verification exit0 /tmp/f5-format-verify.log. Five-source manifest frozen and checked.
Lead dev review complete: requested unreachable-branch cleanup and omitted-vehicle typed World
negative at both cuts implemented. Independent identity, every actor/retry, history/fork/cache,
raw/re-signed leaves, capacity and detached-buffer tests inspected. No remaining dev findings.
Root full build /tmp/f5-build.log started. Three independent rounds pending.

Root build exit0:0warnings/errors,4.41s, /tmp/f5-build.log, unique binlog recorded in log.
Root fullsuite /tmp/f5-suite.log and fullformat /tmp/f5-format-full.log active. Exact commands:
- dotnet build Sandtable.slnx --no-restore '-bl:/tmp/f5-build-{}.binlog'
- dotnet test --solution Sandtable.slnx --no-build '-bl:/tmp/f5-suite-{}.binlog'
- dotnet format Sandtable.slnx --verify-no-changes --no-restore
Independent round1 dispatched fresh after worker returned, against frozen manifest.

Full format verification exit0 /tmp/f5-format-full.log. Retained detailed worker commands, earlier
tool/build failures and design decisions in f5-worker.md. Source manifest unchanged.

Independent round1 Ready, no actionable code/plan findings. Lead read and accepted report;
blind ledger preceded author/evidence/recall, incidental CCE snippet exposure disclosed.
Round2 started fresh after round1 returned. Fullsuite remains pending; source unchanged.

Root fullsuite exit0 /tmp/f5-suite.log:2,093passed,0failed/skipped,3m22s561ms.
Boundary gate started /tmp/f5-boundary.log:
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build
--filter-trait 'Boundary=UserSpace' '-bl:/tmp/f5-boundary-{}.binlog'.

Boundary exit0:81passed,0failed/skipped,9s747ms. Full local gate complete; source unchanged.
Added local Markdown targets2 checked; git diff --check passed. Independent round2 still active.

Independent round2 Ready with no actionable code/plan defects. Lead read report and accepted
claim reconciliation, including structural whole-World equality and frozen test evidence.
Round3 dispatched fresh after round2 returned; no source changes.

Final status: F5 accepted. Round3 Ready with no actionable findings; lead read all three reports
and reconciled claims/plan/code. Third reviewer independently reran canonical oracle successfully.
No experiment/fourth review needed. Source unchanged; all local gates pass.
