# Handoff: CoreB crash diagnosis and CI row proof

## Objective And Boundary
Diagnose intermittent CoreB process exit139, preserve discriminating evidence, and fix confirmed CI blockers. PR174 implements crash-evidence capture and exact xUnit4.0.2 row-proof compatibility. The original crash cause remains unresolved. This is an engineering handoff, not a product completion ledger.

## Canonical Sources
Applicable AGENTS.md and CONTRIBUTING.md govern authority, feature branches and verification. Implementation is .github/scripts/test_partition.py and .github/scripts/ReleaseTestRows/Program.cs, with .github/scripts/test_partition_test.py. Runtime settings follow https://learn.microsoft.com/en-us/dotnet/core/diagnostics/collect-dumps-crash and runner changes follow https://xunit.net/releases/v3/4.0.2 . Review packets/reports are retained alongside this evidence in docs/work/reviews.

## Current Repository State
Author worktree /Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable; branch codex/coreb-crash-diagnosis; frozen head3a0cfde5a9fce8646eff5143210dc4ed7453cee4; comparison base/latest-main df831a42293cde3a105cc778ca2a762eab043400. Tracked tree clean; preexisting untracked .serena/ excluded. Source branch pushed; diagnostic draft PR174 https://github.com/dills122/sandtable/pull/174 . Keep source/head frozen during independent review. Separate codex/coreb-diagnostics-evidence contains only documentary additions against the candidate; it is published without switching the author checkout.

## Completed Work And Evidence
Original main run38106271573 attempt1 tested80f633f11271e8a0d4470459fee41bac7a014e61, SDK10.0.401, Linuxx64 .NET10.0.12, xUnit4.0.1/MTP2.4.0. CoreB139 after2103passes,117unfinished,zero assertion failures; no final XML or native stack. Seven original files hashed; active-three temporal overlap recorded, not culprit attribution. Local unchanged-source exact-three PASS3/3 in14.4616s on macOSarm64/.NET10.0.11; environmental mismatch prevents Linux inference.
Crash wrapper RED94/3expectedfailures then GREEN94/94. Child-only supported triage settings now reach executed shards; PID/time dump pattern under always-uploaded diagnostics; process status/log/final-XML rejection unchanged.
Candidate initial CI38107972649 failed prepare with managed InvalidOperationException Unsupportedrunner4.0.2 at testedmerged8ff9d83480086939a32a23aa9518c86246d1974. Newer main's central package upgrade caused this separate failure. Integrated latestmain without reverting dependencies/actions. Actual4.0.2 formatter/providers match retained4.0.1proof:5providers/41rows,2controls/39rows,same display-call sequence. Strict C#/Python guards now both require ONLY4.0.2; RED96oldguard rejection/acceptance reproduced, GREEN96/96. Isolated oldcommon/core assemblies each rejected without emitting proof.
Frozen Release solution/helper builds PASS0warnings/errors; helperformat/diff checks PASS. Frozen complete discovery2770exact rows71CoreA/2220CoreB/469Exercise/10Contracts matches retainedbaseline. This is discovery, not test-execution coverage.
Independent review1 at6385f98: Ready with non-blocking follow-ups diagnostic-only, no actionable findings, accepted; report copied verbatim SHA0ee0052b5ae01d576966c746bd2030bede2f0b63f59f562ae84516272d6aa9af. Review2 at3a0cfde wholepatch dispatched,2of9/round1pass2/recovery0of2, pending at03:33UTC.
Candidate CI38108441743 attempt1 testedmerge26575b55a353438af08116e207224d5923195f13, parents latestmain df831a and frozenhead3a0cfde. Linux prepare PASS: SDK10.0.401 Release, runner4.0.2,2770exact identities. Test shards in progress asof2026-10-11 03:33UTC. Original80f control38106271573 attempt2 in progress separately; prepare/contracts/exercise PASS,CoreA/CoreB running. Do not substitute either run for the other.

## Decisions And Rationale
No guessed GC/JIT/concurrency workaround, timeout increase, removed assertion, retry-inside-wrapper or arbitrary runner-version allowance. Triage capture closes an evidence gap; it does not establish native fatal-path behavior. Existing exact source/SDK/configuration/attempt binding and zero-status complete final XML gates remain authoritative. No gameplay, public admission, domain, workflow or central package edits against latest-main.

## Blockers And Limitations
Actual Linux dump/JSON production requires a natural recurrence; no deliberate production crash proposed. Passing ordinary CI cannot prove intermittent-crash remediation. Candidate xUnit4.0.2 brings MTP2.5.1 while originalcontrol uses4.0.1/MTP2.4.0, so changes in outcome cannot identify causality. Exact final candidate coverage and review2 still pending at snapshot. Original logs live in temporary retained downloads; committed manifests/triage and GitHub run URLs preserve bounded facts without publishing unnecessary raw logs. Product stop04:00UTC: preserve WIP and avoid an unverified merge if gates remain incomplete.

## Immediate Next Actions
Read newest receipt/review additions on evidence branch and verify current PR head/check state. Keep both runs separate. On success, download attempt-bound aggregate, verify every expected identity/final report/status and merge parents before emitting final receipt. On natural candidate fatal recurrence, inspect diagnostics for native stack and dump-creation messages before selecting any fix. Return Accept/Dispute/Defer for review2 to Brain; Brain owns fresh review and any merge. No additional full retry authorized; no rerun-failed. Do not change source absent findings or evidence-backed authorization.

## Verification Commands
PYTHONDONTWRITEBYTECODE=1 python3 .github/scripts/test_partition_test.py
git diff --check df831a42293cde3a105cc778ca2a762eab043400..3a0cfde5a9fce8646eff5143210dc4ed7453cee4
Use native MTP --solution/--project and unique /bl for any future dotnet restore/build/test. Local suite lease owned by author; serialize local heavy work. For authorized gh operations invoke github-keychain-auth, execute outside sandbox and unset stale GH_TOKEN/GITHUB_TOKEN; never extract credentials.

## Delivery Metadata
Repository dills122/sandtable. Author feature commits6385f9802ee46f1fba4f4aeee9c3b4c895d7f0ae and3a0cfde5a9fce8646eff5143210dc4ed7453cee4; latestmain merge20d0a2583120c92c9563a28eeba6b24eed5ef768 retained. Source PR174 draft, no author merge. Brain chat01a0c9dc-00bc-78a3-800d-3cb36859e422 owns coordination. Snapshot2026-10-11 03:33UTC (Toronto date2026-10-10). Temporary detailed evidence /private/tmp/sandtable-coreb-crash; durable evidence branch codex/coreb-diagnostics-evidence. Read evidence files with git show if keeping author head frozen.
