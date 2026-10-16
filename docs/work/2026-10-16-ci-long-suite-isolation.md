# Dedicated parallel suites for expensive tests — implementation checkpoint

This checkpoint records the design and focused local measurements before full candidate validation. It does not claim the hosted latency target has been met. Subsequent exact-head review and CI evidence belong in the pull request and delivery handoff.

## Objective

Reduce the complete required pipeline to at most ten minutes, with five to six minutes as a stretch target. The measurement includes preparation, formatting, discovery, every required test suite and aggregate verification. Initial platform queue time is reported separately; waits between dependent jobs remain inside the pipeline measurement.

Every previously required test remains required. Expensive tests run in dedicated parallel suites rather than being moved into an optional or scheduled workflow. No timeouts, correctness checks or resource limits are weakened to achieve the target.

## Baseline

Verified main `121baa56820fffce42316b48ac551cb24c012a6c`, CI run `38830848442`, attempt 1:

- 3,716 actual test rows; 26 execution processes.
- Workflow elapsed time: 1,619 seconds; initial queue: 1 second.
- Required CI consumed 78.333 runner-minutes.
- Preparation took 240 seconds, including formatting 78, build 65 and discovery 59 seconds.
- Core-B took 1,350 seconds, including 1,249 seconds in its test step.
- Core-A took 821 seconds; the three R02 suites took 324, 184 and 261 seconds.

Prior PR191 redistributed five whole classes and demonstrated some shorter runs, but failed its frozen performance/cost acceptance criteria. Its mapping is not the basis of this change. Its measured investigation remains useful evidence.

## Implemented design awaiting full validation

Isolate costly legacy classes into exact-row groups, each executed in a fresh process. Retain existing class, method and argument identities; discover actual runner rows and prove complete, disjoint allocation. Remaining ordinary classes stay whole. Keep the existing R02 suites, exclusive support phase and semantic obligations intact.

Run formatting as a separate required job alongside preparation. Final verification must reject any failed, missing or skipped required dependency, including formatting. This removes a serial wait; it does not remove formatting from the gate.

The implemented bounded matrix has 17 test suites: eight long-running suites, four ordinary Core suites, three R02 suites, Exercise and Contracts. Fifty-two long-test groups plus four ordinary Core processes, twenty R02 processes and two other module processes yield 78 executions. Full candidate validation must confirm these counts and actual command proof.

Each run binds results to its exact source revision, attempt, SDK, build configuration, binary and fixture fingerprints, process exits and strict XML identities. Unexpected, duplicate, nested or foreign results fail verification. Resource monitoring and owned-process cleanup remain required.

A narrowly scoped test-only optimization removes redundant dependency-buffer copies in RoundEntry mutation tests. The focused regression measured 6,868,848 allocated bytes across 57 callbacks before the change and passed an upper bound of 1,824 bytes afterward. The passing test does not print an exact allocation count. Default helper ownership assertions and production copies/hashes remain intact. The controlled 34-row baseline passed in 502.6095 seconds with 522,551,296 bytes kernel peak RSS; the after measurement passed the same 34 exact rows in 471.3990 seconds with 377,913,344 bytes kernel peak RSS (about 6.2% less wall time and 27.7% less peak RSS). Source-tree equality establishes comparable native source, while host/cache variability remains uncontrolled. This is one controlled local observation, not a hosted performance guarantee.

Build-artifact reuse is deferred: the current inventory artifact does not contain the complete validated runtime closure (assemblies, dependencies, runtime configuration and fixtures). The roughly 65-second leaf build is an upper bound on possible savings, not a measured net gain after artifact transfer.

## Timing and reliability limits

Historical XML test durations include overlap and contention. Scheduling models based on those durations are forecasts, not measured capacity. A faster final result requires complete hosted observations at the reviewed source tree.

GitHub account concurrency is shared with other workflows. Increasing the matrix concurrency removes an artificial limit but does not guarantee immediate host availability. Standard hosted runners in this public repository require no larger-runner purchase. Runner use and artifact storage remain separate operational considerations.

No general zero-flakiness guarantee follows from a passing run. Deterministic failures, environment limitations, capacity failures and genuine inconsistent assertions must be reported separately. Diagnostic repetition never erases an original failure.

## Sources

- https://xunit.net/docs/running-tests-in-parallel
- https://docs.github.com/en/actions/reference/limits
- https://docs.github.com/en/billing/concepts/product-billing/github-actions

## Validation at this checkpoint

Installed-runner selector pilots passed six actual rows covering numeric, quoted-string and escaped-JSON arguments with exact prepared/XML identity equality. The focused allocation regression failed before the callback change and passed afterward. All 34 mutation rows passed both timing observations. These checks do not replace the full 3,716-row gate.

The complete candidate still requires full local workload reconciliation, final author self-review, fresh independent review and complete hosted measurements. This checkpoint is not merge acceptance. The [timing-manifest maintenance instructions](../../.github/scripts/long-suite-weights.md) describe how future test additions update the exhaustive allocation.

