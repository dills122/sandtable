# Author Explanation

## Intent And Success Criteria
The original CoreB process stopped after 2103 of 2220 expected passes, exit139, without final XML or native crash evidence. Temporal overlap names three active tests but does not identify a culprit. The immediate patch addresses absent runtime dump configuration; actual crash diagnosis is incomplete.

## Plan-To-Implementation Traceability
Original logs/status/diagnostics hashed and retained. Parsed active rows and runtime identity. Narrow concurrent exact-three reproduction passed locally on unchanged80f source. One complete exact-source CI control attempt2 is queued; no rerun-failed. Confirmed default missing dump configuration, reproduced through RED tests, enabled capture, verified GREEN, documented artifact behavior, froze diagnostic draft. Native rootcause/fix remains deferred pending evidence.

## Technical Approach And Flow
Execute loads identity and inventory, computes unchanged execution command, creates an absolute diagnostics directory and a copy of the environment, overrides five runtime crash settings, then passes this environment only to subprocess.run. It writes the original return code to status.json and exits with it. Existing always-upload already retains artifacts/ci. Coverage uses status and complete final XML and never substitutes progress/dumps.

## Changed-Component Walkthrough
execution_environment owns child-only runtime triage configuration: enable dump1, type3, crash report1, createdump diagnostics1, crash-%e-%p-%t.dmp. Execute passes env to existing subprocess without retry. Tests use a harmless real Python child that prints only selected settings and exits139 or0; no actual segfault or environment-secret logging. Contributor guidance explains retention/failure semantics.

## Decisions And Rejected Alternatives
Triage rather than full/heap capture follows official runtime documentation; no dependency or workflow edit is needed. Unique PID/time filenames separate child processes. Reject disabling parallelism, adding timeouts, or applying JIT/GC flags without a native stack. Parent os.environ must remain unchanged.

## Invariants And Boundary Conditions
Filters, assertions, row partition, Release/SDK identity, run attempt binding, existing subprocess failure propagation, no final XML on crash, and stale-directory rejection remain intact. No gameplay/public admission changes. Dump collection can fail due to runtime/OS constraints; diagnostics are evidence, never proof of coverage. Normal execution copies only one environment and creates one directory; capture costs occur on fatal failure.

## Verification Performed And Results
94 Python tests RED with three new failures; existing91 passed. GREEN94/94 after wrapper implementation. Both modules compile into tmp without worktree caches; diff check passed. Local exact-three dotnet reproduction PASS3/3 in14.4616s on macOSarm64 SDK10.0.400/runtime10.0.11; failing CI Linuxx64 SDK10.0.401/runtime10.0.12. It does not establish Linux or full-suite safety. Local Release build succeeded0warnings/errors. Candidate hosted CI and independent review outstanding at freeze.

## Risks, Tradeoffs, And Maintenance Costs
Actual Linux fatal-path dump production and artifact uptake require a recurrence. A passing CI run only checks normal behavior. No stack currently supports a rootcause claim. Newer main's artifact upload version remains compatible because candidate does not edit workflow. Crash dumps remain retained14days under existing artifact policy.

## Deviations, Deferrals, And Known Gaps
The authorized goal includes diagnosing/fixing the crash; this packet covers only the independently reviewable diagnostic fix. Runtime/source rootcause, whether failure depends on Linux or suite ordering, and durable native stack are unresolved. Brain owns fresh review and merge. No selfmerge proposed.

## Challenge Points For The Reviewer
Verify the execution boundary actually receives settings, directory creation precedes child launch, and status/report rejection stays fail closed. Check runtime docs support the chosen settings. Distinguish child environment tests from untested Linux createdump behavior and distinguish original80f control from candidate CI. Review scope and plan for overclaimed completion.


## Authorized Compatibility Correction And Whole-Patch Update
Brain authorized this bounded correction after candidateCI inherited newer main's xUnit4.0.2 update and the old helper rejected it. Latest-main integrated without reverting package/action upgrades. Primary4.0.2 release notes inspected; MTP dependency now2.5.1, so candidate runtime/runner differs from original control and no green comparison establishes crash remediation.
Before changing repository guards, isolated strict4.0.2 helper experiment retained all existing signature/IL/provider/configuration/formatter controls and passed against actual compiled4.0.2 binaries. All5providers/41rows,2controls/39rows and display-call sequence exactly match retained PR1724.0.1proof. Complete discovery uses actual complementary filter commands and proves2770exact unchanged rows,71/2220/469/10. Unfrozen preliminary experiment explicitly labelled; frozen-head discovery/rebuild retained separately.
C# helper has one SupportedRunnerVersion4.0.2 constant used by both loaded component checks and emitted runner_version. Python reader accepts exactly4.0.2. Independent RED96test evidence old guard rejects valid4.0.2 and admits4.0.1; GREEN96 covers strict old/new/unknown/malformed/missing rejection and retained formatter/provider/coverage failure cases. Real C# experiments substitute each4.0.1common/core assembly separately and observe unsupported-version exception/nonzero return/no emitted proof. No arbitrary-version allowance or removed validation.
CONTRIBUTING describes upgrade validation. Candidate base df831a42293cde3a105cc778ca2a762eab043400,head3a0cfde5a9fce8646eff5143210dc4ed7453cee4,tracked clean/.serena untracked. Four-file wholepatch; original80f CoreB139 remains separate unresolved issue. Previous review1 report accepted and preserved; fresh review2 requested due material proof behavior change. No selfmerge.

Frozen local evidence: Release solution and helper rebuild PASS0warnings/errors; complete actual frozen-head discovery PASS2770rows,71/2220/469/10; all exact identities equal retained baseline. frozen-selfreview.json records SHA256,SDK and AST invariants. These are discovery/build proofs, not actual test execution coverage.
