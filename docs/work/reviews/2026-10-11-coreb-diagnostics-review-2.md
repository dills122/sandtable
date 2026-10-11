# Independent review

Review instance: 2 of 9; round1 pass2; recovery0 of2.
Target: base df831a42293cde3a105cc778ca2a762eab043400, head 3a0cfde5a9fce8646eff5143210dc4ed7453cee4, branch codex/coreb-crash-diagnosis.

## Preliminary ledger — before author explanation

Blind pass completed against neutral bootstrap, exact Git diff, all changed implementation, relevant surrounding Python verification/filter/discovery logic, tests, workflow, package pin, repository instructions and primary documentation. No author packet read yet.

- Scope verified: four named files only, tracked tree clean, preexisting untracked .serena/ excluded. Helper and Python version guards both now require exactly 4.0.2; package already pinned to 4.0.2 in base. No arbitrary-version fallback.
- Crash capture correctly copies parent environment, creates absolute diagnostics directory, applies documented dump settings only to executed subprocess and descendants, preserves process return/status/log. Final XML and successful dependency checks remain mandatory.
- Diff does not change filters, completeness checks, concurrency, timeouts, test bodies/assertions or workflow upload. Workflow retains artifact directory with always() and 14-day retention.
- Added real Python child tests exercise success and 139 exit, settings inheritance, parent isolation, retained stderr/status and failure without final XML. They do not prove Linux createdump or JSON file creation. Directory-before-launch assertion occurs after return, although implementation ordering is clear.
- Retained compatibility summaries indicate five providers / two formatter controls / 41 provider rows / 39 control rows unchanged and 2770 total identities. Need independently reconstruct inventory from frozen actual discovery logs, compare baseline identities and validate binaries/proof fingerprints before treating provenance as confirmed.
- Managed unsupported-runner rejection (-6 / shell134) is distinct from original native139. Patch enables diagnosis and corrects reviewed runner compatibility; it cannot establish original crash root cause or successful Linux dump capture.
- Initial assessment: no actionable defect found; readiness contingent on proportionate focused checks and retained frozen evidence verification. Candidate CI is separate merge gate.

## Findings

No actionable findings. Whole four-file patch reviewed, including compatibility correction. Crash configuration matches documented .NET semantics, uses a parent-environment copy and is passed only to the execution child and its descendants. Diagnostics directory creation precedes subprocess launch. Absolute dump pattern includes executable, PID and time. Real process status, merged stdout/stderr and final-report coverage remain intact. Strict guards retain fail-closed behavior and reviewed provider/formatter controls.

## Plan Review

Bounded plan is implemented: preserve original evidence; add child crash settings under uploaded shard diagnostics; maintain status/XML acceptance; validate the newly pinned 4.0.2 compiled formatter/providers before updating strict guards; document both behaviors. Compatibility adjustment is justified because base already pins xunit.v3.mtp-v2 4.0.2 while the old discovery helper explicitly rejects it. No package downgrade, runtime workaround, concurrency change, timeout increase, filter alteration, removed assertion, domain change, generated artifact churn or workflow mutation.

Broad objective of diagnosing/fixing original native crash remains incomplete and accurately deferred. This verdict applies to diagnostic and discovery-compatibility patch only. Original-source control and candidate CI are distinct evidence streams. Candidate run 38108441743 remains a separate merge gate, not part of this review's readiness proof. Documentation change belongs in CONTRIBUTING because workflow/helper behavior changed; architecture and naming remain unaffected.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Child-only crash settings preserve parent env/status/log | test_partition.py execution_environment/main; three real-child tests in focused suite | Confirmed | Runtime launch boundary correct; no failure masking |
| Final XML and all rows still required | verify_coverage, load_partition, discovery_rows, core_partition and partition_filters; AST comparison; focused suite | Confirmed | Completeness/filter/verifier unchanged |
| Only reviewed 4.0.2 accepted | Program.cs constant and both loaded assembly checks; Python exact string guard; version rejection tests and retained C# substitution failures | Confirmed | No arbitrary-version bypass |
| Frozen discovery has 2770 rows and same identities | Reconstructed inventory from all five actual discovery logs; exact-head inventory; direct baseline comparisons | Confirmed | Actual discovery compatibility established, not test execution coverage |
| Five providers/41 rows, two controls/39 rows and display calls unchanged | Frozen deferred.json versus retained hosted 4.0.1 baseline deferred.json | Confirmed | Compatibility upgrade grounded in actual formatter/provider proof |
| Frozen proof tied to built binaries | Four assembly fingerprints plus helper DLL fingerprint recomputed; frozen inventory/deferred hashes recomputed | Confirmed | Retained proof and current compiled Release artifacts agree |
| Original source exit139 evidence preserved | Original seven evidence files recomputed byte lengths/SHA256; original status.json | Confirmed | Original native failure remains separate from managed rejection |
| Linux fatal-path dump and JSON production works | Runtime docs support settings; no reproduced Linux fatal-path capture executed | Unverified and acknowledged | Follow-up requires recurrence/capture; cannot claim crash fix |
| Release rebuild succeeded cleanly | Retained frozen-build.log states 0 warnings/errors | Confirmed retained evidence only | Reviewer did not rerun .NET build or heavy tests |
| Candidate CI/unchanged control will establish relevant results | Not monitored by reviewer; assigned separate owner/gates | Unverified | No CI completion or merge authorization implied |

Author packet's earlier 94-test statement describes prior diagnostic patch, while compatibility update explicitly records 96. Actual reviewed suite contains and passes 96; no inconsistency in final coverage claim.

## Verification Performed

Executed from /Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable unless stated otherwise:

- `git status --short`, `git branch --show-current`, `git rev-parse HEAD`, `git diff --stat df831a42293cde3a105cc778ca2a762eab043400..3a0cfde5a9fce8646eff5143210dc4ed7453cee4`, whole exact-range diff: expected head/branch/four paths; no tracked edits; untracked .serena/ preexisting and excluded.
- `PYTHONDONTWRITEBYTECODE=1 python3 .github/scripts/test_partition_test.py`: PASS, 96 tests in 0.255s. Includes strict 4.0.2 admission and malformed/old/new/missing-version rejection; completeness/filter/control failures; actual child inheritance, retained failure and missing XML rejection.
- `git diff --check df831a42293cde3a105cc778ca2a762eab043400..3a0cfde5a9fce8646eff5143210dc4ed7453cee4`: PASS, no whitespace errors.
- Read-only `PYTHONDONTWRITEBYTECODE=1 python3 -` probe imported candidate helper and rebuilt inventory using `build_inventory` over frozen core/core-a/core-b/exercise/contracts discovery logs plus deferred.json. JSON-normalized reconstruction equals retained inventory exactly; head equals 3a0cfde5a9fce8646eff5143210dc4ed7453cee4, SDK 10.0.400, Release, 71/2220/469/10 = 2770. First probe compared Python tuples to serialized JSON lists and failed equality; corrected serialization normalization passed. This was a probe representation issue, not product failure.
- Same probe recomputed SHA256 for Cna.Core.Tests.dll, Cna.Core.dll, xunit.v3.common.dll, xunit.v3.core.dll and ReleaseTestRows.dll against frozen proof: PASS all five. Frozen inventory/deferred file digests match frozen-selfreview.json.
- Same probe compared ASTs against base: only existing functions expand_core/main changed; execution_environment added. Filters, load_partition, build_inventory, verify_coverage, identity, run_logged and execution_command unchanged.
- Read-only Python baseline comparison against `/private/tmp/actual-assault-ci-38105360059/inventory/ci-inventory-47c510031233250311a100056875928f8b5e2b1a-1/{inventory,deferred}.json`: PASS all 2770 exact identities, providers, formatter controls and display-call sequence. Baseline version 4.0.1. Earlier retained baselines 38101027022 and 38103537916 also match exactly. This directly verifies comparison, not merely author summary flags.
- Read-only Python hash probe against original-evidence.json: PASS seven files' lengths and SHA256. Original status exit139, SHA80f633f11271e8a0d4470459fee41bac7a014e61, SDK10.0.401, attempt1. Original diagnostics/logs not modified.
- Inspected retained real C# rejection failures for each substituted 4.0.1 common/core assembly: explicit Unsupported runner version exceptions; results report -6/nonzero and no proof. These managed aborts correspond to shell134, distinct from original139.
- Inspected always-upload artifact path, retention and caller context in ci.yml; no candidate workflow edit. Package pin 4.0.2 verified already in exact base.
- Primary sources read: [Microsoft collect dumps on crash](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/collect-dumps-crash) supports all five settings, Triage type3 and %e/%p/%t templates; [xUnit 4.0.2 release notes](https://xunit.net/releases/v3/4.0.2) lists runner/runtime-affecting changes and MTP2.5.1. Therefore candidate and original control differ in runner/runtime context; a green candidate cannot establish original root cause.

No .NET builds/test execution, remote CI mutations, fixes, commits, publication or spawned agents performed. Only this report written. Scope stayed focused on known changed paths and caller context; no structural architecture expansion required.

## Open Questions And Residual Risks

- Linux runtime dump generation, JSON crash-report generation and successful artifact uptake after a native fatal event remain unverified. Permissions/runtime constraints may prevent capture despite valid settings; createdump diagnostics in execution.log should expose failures.
- Original native139 root cause remains unresolved. Temporal active-row overlap does not identify a culprit, and managed unsupported-runner134 is a separate preparation blocker corrected here.
- Dump filename PID/time tokens distinguish expected child processes and recurrence; generation itself depends on runtime/platform behavior. Crash evidence cannot satisfy final XML coverage.
- Original80f attempt2 control and candidate38108441743 merge gate remain owned separately. No passing execution coverage inferred from discovery or retained build success.

## Verdict

**Ready** for bounded diagnostic plus strict4.0.2 discovery-compatibility patch at exact head **3a0cfde5a9fce8646eff5143210dc4ed7453cee4**. No actionable defect found. This is not a native crash remediation verdict and does not authorize merge before separate CI gate completes.

## Recommended Next Actions

Initiating owner should evaluate candidate38108441743 merge gate at its tested merge SHA and keep original80f attempt2 control separate. On Linux recurrence, inspect retained dump/JSON/createdump logs to diagnose native139; if native evidence remains absent, investigate runtime capture failure. Preserve original evidence and avoid speculative concurrency/runtime workarounds without causal evidence.
