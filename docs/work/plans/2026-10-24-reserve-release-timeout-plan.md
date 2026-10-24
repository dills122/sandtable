# Persisted Reserve Release timeout

Base `d7993d7737f761a7d486411c5714a6ad8e221370`, branch
`codex/rc-overnight-plan-20261023`. Ruflo task `task-1792882630464-o1ta19`,
namespace `sandtable-rc-20261023`, swarm `swarm-1792808186330-z3idiq`.

## Behavior

At Reserve Release entry, persist an immutable decision deadline. On expiry,
apply configured `convert-unresolved-i-retain-ii` only to unresolved members:
convert due first-cycle Reserve I to Reserve II and retain due later-cycle
Reserve II. Existing rules offer only the cycle-appropriate status; preexisting
Reserve II in the first cycle remains unchanged.
Preserve accepted owner dispositions. Record explicit System evidence; do not
fabricate owner choices. Cold restart and background sweep must work without an
owner query. Completion must enter the existing cycle-control lifecycle.

## Ownership and implementation

Core worker owns versioned native timeout contract, replay, session/checkpoint,
adapter integration, Core tests and native contract spec. Host worker owns
journal lifecycle/schema, scheduler, Host tests and durable journal spec.
Agree contract/interface before dependent implementation. Coordinator owns
high-level documentation, measured CI weights, final validation and commit.
Preserve concurrent edits and serialize build leases.

Bind timeout to campaign, prior authority, release entry, timing configuration
and clock evidence. Partial owner choices never reset the entry deadline.
Reject stale/invalid/foreign proposals before touching the clock or history.
Require deterministic canonical replay, transcript/round completeness and atomic
persistence. Preserve historical RBA/Force/cycle schema verification thresholds
when introducing current schema. Older journals remain read-only.

## Validation

Use focused deterministic tests for first-cycle Reserve I conversion with existing Reserve II, later-cycle Reserve II retention, partially
resolved queues, exact deadline, no-query cold restart, duplicate sweeps,
unavailable/regressing clocks, rejected late proposals, rollback, row tampering,
and genuine repeated combat continuations. Check World/RNG and movement effects
against existing source-backed rules. Preserve failing evidence, then validate
Core affected classes, Host, Runner and boundary suites plus build/format.
Measure new Core rows from Release XML and reconcile actual discovery with CI
weights. No full Core run after each edit.

Coordinator source/diff review; earlier independent review allowance remains
exhausted at three instances, so no new independent acceptance claimed.
Commit validated work on feature branch. No push, merge, certificate promotion,
or public RC claim. Original source-pin, certificate and dependency gaps remain.
