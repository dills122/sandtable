# Persisted Custody timeout

Base `ac1cab5403637e85970f3d425885ca92171f5c37`, branch
`codex/rc-overnight-plan-20261023`. Ruflo task `task-1792883677865-f620ih`,
namespace `sandtable-rc-20261023`, swarm `swarm-1792808186330-z3idiq`.

## Observable result and scope

At accepted settlement with pending native Custody, retain an immutable deadline.
Expiry records an explicit System `leave-unguarded` disposition, applies the
existing source-legal lot/escape/replacement effects, then continues through
Combat successor and Reserve Release. No owner choice, guard debit or random
result is fabricated. Restart and background expiry require no owner query.
Current native contract supports exactly one captive lot; do not claim ordered
multi-lot or partial multi-lot coverage. Accepted owner disposition wins once.

Existing source legality remains authoritative. Audit whether a supported cut can
lack a legal leave-unguarded route; preserve explicit unsupported rejection and
record any residual profile gap rather than inventing a route or effects.

## Ownership

Core worker owns versioned native System contract, runtime/replay/checkpoints,
adapter, Core tests and native spec. Host worker owns durable schema/lifecycle,
scheduler, Host tests and journal spec. Freeze API before dependent work.
Coordinator owns plan, high-level docs, measured CI weights, source/diff review,
final validation, commits and refreshed RC checklist with explicit pass criteria.
Preserve concurrent edits; serialize builds. No more than three workers.

## Required behavior and validation

Bind campaign, settlement/lot, current authority, timing configuration and trusted
clock evidence. Start deadline at entry, never renew on query/restart. Validate
stale/invalid/foreign proposals before clock/history mutation. Retain receipts on
retry, race owner/timeout in one immediate transaction, and verify complete
transcript/round evidence on cold readback. Timeout and successor transitions must
roll back together on storage failure. Preserve historical schema checks and
read-only access; never synthesize old deadlines during migration.

Use focused deterministic tests for exact deadline, no-query restart, sourcelegal
lot effects, owner wins/late rejection, duplicate ticks, unavailable/regressing
clocks, altered/missing rows, transaction rollback and initial/repeated replay.
Run affected Core classes, Host/Runner consumers and boundaries plus build/format.
Measure new Core rows and verify actual discovery against CI weights. No full
Core rerun during iteration. Retain actual failing logs and test limitations.

Prior independent review allowance remains exhausted3/3. Coordinator review is
not new independent acceptance. Commit validated implementation first, then
refresh RC checklist. Existing certificates, source-pin failures, dependencies,
scenario/UI/distribution gates stay open until their own evidence passes.
