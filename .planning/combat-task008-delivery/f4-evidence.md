# F4 active fallback evidence

Base f0cb5ca accepted F3; branch codex/combat-task008-reaction-lifecycle; main-based PR136.
Actual causal predecessor F2 first participant move, not F3 terminal. Fourtraces/eightevents/12cuts/32artifacts.
Baseline /tmp/f4-reaction-baseline.log passed:4traces8events12cuts8retries612mutations200raw132boundaries17pins.
Command PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-inherited-reaction-active-fallback-v1.py.
Worker TDD/implementation active; root dev/integration and three independent rounds pending.

TDD /tmp/f4-red.log expected missing F4 types. Root independently checked17 canonical source pins
and four cases, read spec/oracle emit transition and new models/projector/codec. Preliminary dev
review no blocker; final expanded tests pending. Requested2 cases per owner and explicit both-event
terminal retry, competingreason, no earlyresume and accurate activefallback diagnostics.

Initial implementation /tmp/f4-green1.log failed compile on extra .Value dereference of active
opportunity string. Worker corrected access; /tmp/f4-green2.log two owner golden theories passed,
0failed/skipped,2s153ms. Final adversarial expansion still active; no acceptance claim yet.

Expanded tests added independent actual stop identity/public capability/receipt/prefix hashes,
literal14→15→16 flow, both terminal retries, raw/re-signed every event/cache leaf, history and buffers.
Worker reports test-only extra StopId.Value compile failure then two owner failures from expecting
shape-only input decoder to reject valid changed source atoms. Corrected assertion uses decoded input
through Apply; production codec unchanged. Root requested positive initial-state readback alongside
existing state1/2 readback and all three-cut tamper tests. Final corrected run/freeze pending.
F3 PR136 at f0cb5ca verified all remote CI/CodeQL/dependency/link checks passed during this work.

Freeze: five source hashes match worker handoff, no active worker processes. Final focused29/29
(F4eleven+F2nine+F3nine),0failed/skipped,1m38s722ms /tmp/f4-focused.log. Exact command:
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class '*CombatReactionFallbackTests'
'*CombatReactionLifecycleTests' '*CombatReactionClosureTests' --no-restore '-bl:/tmp/f4-focused-{}.binlog'.
Scoped formatter exit0 /tmp/f4-format.log; no source edits afterward. Lead full format will verify.
Earlier logs retained: green1 production wrapper compile, expanded1 test-only StopId.Value compile,
expanded2 9/11 with two shape-vs-authority harness failures. Corrections described above; no parser
contract weakening. Positive state0 readback/per-owner counts/diagnostics and final tests inspected.
Lead dev review complete, no remaining findings. Full build /tmp/f4-build.log started; review1 next.

Root build exit0 /tmp/f4-build.log,0warnings/errors,4.05s. Full suite /tmp/f4-suite.log and full format
/tmp/f4-format-full.log running. Exact commands:
- dotnet build Sandtable.slnx --no-restore '-bl:/tmp/f4-build-{}.binlog'
- dotnet test --solution Sandtable.slnx --no-build '-bl:/tmp/f4-suite-{}.binlog'
- dotnet format Sandtable.slnx --verify-no-changes --no-restore
Independent round1 active against frozen manifest.

Full format verification exit0 /tmp/f4-format-full.log; frozen source unchanged.

Independent round1 Ready, no actionable findings; lead read report and accepted code/plan/claim
reconciliation. Blind ledger preceded author/evidence/recall; no prior report read. Five hashes,17pins,
four cases/32artifacts checked independently; focused29/build observed. Fullsuite pending at cutoff.
Round2 began fresh after round1 returned. No source changes.

Root full suite exit0 /tmp/f4-suite.log:2,080passed,0failed/skipped,3m13s731ms. Exact command above.
Boundary /tmp/f4-boundary.log running: dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj
--no-build --filter-trait 'Boundary=UserSpace' '-bl:/tmp/f4-boundary-{}.binlog'.

Boundary exit0 /tmp/f4-boundary.log:81passed,0failed/skipped,9s911ms. Full local gate complete.
Independent round2 Ready, no actionable findings; lead read report and accepted plan/code/claims.
Blind ledger before author/evidence/recall; incidental scope/dispatch snippets, no prior reports.
Round3 started fresh after round2 returned. Frozen code unchanged.

Final status: F4 accepted. Independent round3 Ready, no actionable findings; lead read all reports,
reconciled code/plan/claims and retained disclosed post-ledger contamination. No experiment/fourth
review needed. Frozen source unchanged; focused29/full2080/boundary81/build/fullformat all pass.
Added local Markdown targets2 checked; git diff --check pass. No source changes after final tests.
