# Independent review — diagnostic PR174

Review instance: 1 of 9; round 1 pass 1; recovery 0 of 2. Bootstrap's 1 of 3 describes current round; initiating task supplied session ceiling 9.

## Preliminary ledger (recorded before author explanation)

- Scope verified: branch codex/coreb-crash-diagnosis, HEAD 6385f9802ee46f1fba4f4aeee9c3b4c895d7f0ae, base 80f633f11271e8a0d4470459fee41bac7a014e61, one commit, exactly three stated files. Tracked tree clean; preexisting untracked .serena/ visible.
- No actionable implementation finding observed. Child-only environment copy, prelaunch diagnostics mkdir, absolute process/time dump template, raw subprocess returncode/status/log retention are present. Commands, predicates, concurrency, timeouts, domain assertions, workflow are unchanged.
- Coverage checks require status exit zero before exactly one final XML report. Added tests execute a real Python subprocess with 139/0 exits and assert parent environment isolation, stderr retention, configuration and coverage rejection. This is not proof of native signal or .NET dump capture.
- Primary Microsoft documentation confirms DOTNET_DbgEnableMiniDump=1, dump type3=Triage, EnableCrashReport=1 (non-Windows), CreateDumpDiagnostics=1, %e/%p/%t templates; default dump diagnostics go to console. Existing always() shard upload encompasses diagnostics with 14-day retention.
- Test caveat: diagnostics directory existence is asserted after execution rather than within child, so test alone does not prove creation ordering; inspected implementation clearly establishes ordering. No blocker.
- Residual limits: actual Linux runtime crash/dump generation and original exit139 root cause unverified. Dump overhead, runtime permissions and abrupt runner termination can limit capture. Triage does not imply all crash-report/log content is sanitized.
- Executed python3 .github/scripts/test_partition_test.py: 94 tests, OK, exit0. git diff --check base..head: exit0, no output. No .NET/full-suite run.

Author explanation not yet read at time of this ledger.

## Findings

No actionable findings. No weakened predicates, concurrency, timeouts, assertions, retries or failure acceptance. Exact identity/attempt, zero exit and complete final XML remain required.

## Plan Review

Bounded diagnostic criteria satisfied. execution_environment creates diagnostics before launch, copies parent environment and supplies five supported settings; main passes this only to subprocess.run and preserves existing log/status/exit behavior. No domain, contract, test filtering or workflow edits. CONTRIBUTING owns changed workflow guidance. Existing always() upload includes diagnostics with 14-day retention. Original crash goal remains incomplete; exact-head hosted CI and rootcause work remain initiating task responsibilities. No heavy pivot warranted.

## Author-Claim Reconciliation

| Claim | Evidence | Status | Consequence |
| --- | --- | --- | --- |
| Original evidence retained, exit139 at80f source | original status and all seven SHA256/size matches against original-evidence.json | Confirmed; active-row parsing not independently rederived | No culprit attribution |
| Child-only supported crash settings and absolute unique path | implementation, real Python child tests, primary Microsoft docs | Confirmed | Diagnostic scope sound |
| Failure and final XML gate intact | diff, AST comparison, verifier,94tests | Confirmed | Dumps cannot replace coverage |
| Three RED tests then GREEN94 | retained capture-red.log/capture-green.log; independent current GREEN | Logs/current GREEN confirmed; RED sequence not replayed | Tests sensitive to missing settings |
| Local macOS exact-three pass and Release build | Author explanation only | Unverified here | No Linux/full-suite/rootcause inference |
| Candidate hosted CI outstanding | Packet only; hosted status not queried | Current status unverified | Parent owns exact-head gate |
| Actual Linux dump production/rootcause unresolved | No fatal-path runtime experiment; packet defers | Confirmed limitation | Verdict diagnostic-only |

Primary source: [Microsoft Learn: Collect dumps on crash](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/collect-dumps-crash). Confirms variables, Triage3, non-Windows JSON suffix, console logging default and %e/%p/%t path templates. Inspected before author explanation.

## Verification Performed

Workdir /Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable.

- git status --short; git branch --show-current; git rev-parse HEAD: tracked clean, expected branch/head, preexisting .serena/ untracked.
- git diff --stat and git diff and git log --oneline for 80f633f11271e8a0d4470459fee41bac7a014e61..6385f9802ee46f1fba4f4aeee9c3b4c895d7f0ae: three named files, one commit.
- python3 .github/scripts/test_partition_test.py: exit0,94tests,OK,0.290s. Real Python child confirms settings,139/0 return preservation,stderr retention,parent environment isolation and fail-closed coverage.
- git diff --check 80f633f11271e8a0d4470459fee41bac7a014e61..6385f9802ee46f1fba4f4aeee9c3b4c895d7f0ae: exit0,no output.
- Read-only Python AST comparison: only existing main changed; only execution_environment added. Commands/filter/partition/verifier function ASTs unchanged.
- Read-only Python hashlib comparison: all seven original evidence SHA256 and sizes MATCH. Original status80f source,SDK10.0.401,Release,attempt1,exit139.
- Applicable instructions, contributor guidance,diff,surrounding helpers/fixtures/workflow and bootstrap inspected; preliminary ledger recorded before author explanation. No structural domain exploration needed.
- No .NET/heavy-suite/crash-injection/commits/spawns/publication performed. Only review report written.

## Open Questions And Residual Risks

Actual Linux createdump,JSON writing and upload on recurrence unverified. Permissions,runtime helper,disk,runner cancellation and capture duration can limit evidence. Existing job timeout15minutes retained; no new per-dump size/count bound. Triage limits payload relative to heap/full but accompanying JSON/logs are not guaranteed sanitized. Python sys.exit139 tests wrapper handling,not SIGSEGV. New test observes directory after launch; source ordering independently establishes prelaunch creation. No source/runtime/infrastructure rootcause established by these tests or ordinary passing CI.

## Verdict

Ready with non-blocking follow-ups for diagnostic-only head6385f9802ee46f1fba4f4aeee9c3b4c895d7f0ae. No actionable findings. Exact-head hosted CI remains required before delivery; crash diagnosis incomplete.

## Recommended Next Actions

Parent reconcile report and complete exact-head CI gate. Preserve unresolved rootcause status. On Linux recurrence inspect dump-generation log,actual dump/crash report and uploaded artifact before claiming capture works or attributing culprit. No new fresh review needed absent material behavior changes.
