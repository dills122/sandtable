# Crash recovery PR336 CI correction

First head `eb88630cc2199dca2fa73f49f40de1ebf3658c8e`:
[CI run40616911744](https://github.com/dills122/sandtable/actions/runs/40616911744).
Orleans began tests at01:49:41UTC and was stopped at02:09:53UTC by the20-minute
step deadline. No completed test XML/verdict was produced; this run is incomplete,
not passing Host evidence. Final verify correctly rejected failed dependency.
Other46 reported checks passed. Retained log: `artifacts/rc-crash-pair/ci-failure.log`.

Correction changes only Orleans test-step budget20→25minutes and enclosing job
budget25→30minutes, preserving setup/upload headroom. Real process-recovery proofs
increase full Host work. Tests, selections, XML checks, required artifacts and
failure propagation unchanged. No timeout waived, failed test suppressed or result
inferred from process absence. Other jobs retain their existing deadlines.

Validation: `actionlint .github/workflows/ci.yml` exit0;
`python3 -B .github/scripts/test_partition_test.py WorkflowDeadlineTests`2/2 exit0;
`git diff --check` exit0. Same independent reviewer10 accepted bounded correction,
no actionable findings; [addendum](2026-10-27-crash-recovery-independent-review.md).
No runtime/test changes after the16 passing focused cases. Complete new-head CI
remains required before merge; this record does not claim that future run passed.
