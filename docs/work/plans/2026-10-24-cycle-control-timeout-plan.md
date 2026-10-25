# Persisted cycle-control timeout

Base `7d0f34fd`, branch `codex/rc-overnight-plan-20261023`.
Ruflo task `task-1792881286899-jfjd9v`, namespace `sandtable-rc-20261023`.

## Blocker and observable result

Configured `cycle-control = finish-phase` currently has no native persisted
fallback. Completed Reserve Release waits for an owner choice indefinitely.
A cold-reopened campaign must finish into same-slot Truck Convoy Movement at its
immutable deadline even if no owner queries cycle control. Preserve World, RNG,
cycle ordinal and completed Release ancestry. Never fabricate an owner Finish.

## Ownership and contracts

Core worker owns explicit native V34 System timeout contract (verify version free),
validation/replay/session/checkpoint/contact/adapter integration and Core tests.
Freeze the API before dependent Host implementation. Prefer existing V28 finish
projection internally without attributing the new canonical event to an owner.
Binding includes completed Release hash, campaign/state/cycle, timing hash,
opened/deadline/high-water/cancellation/reason and exact next cursor.
Absent entry timing/clock has explicit unavailable evidence, never invented time.

Host worker owns schema10 lifecycle, tests and journal spec. Legacy5–9 stay
read-only with historical verification. Preserve schema9 Force entry completeness
and timestamp binding and schema8 RBA checks independently of current version.
Create cycle round atomically with completed Release, including manual, automatic
and repeated paths. Queries cannot start/reset it. Accepted owner Finish/Repeat
closes the round; validate invalid/stale/foreign proposals before mutation.
Use strict transcript/row verification and known transaction-local successors;
no broad verification bypass. Add independent bounded scheduler pagination.

Coordinator owns integration, high-level docs, CI timing weights, validation,
source/diff review and commit. Maximum coordinator plus three workers. Serialize
build leases; no duplicate healthy full test runs.

## Validation

Prove no-query cold-restart failure before fix. Focused deterministic checks cover
exact deadline, clock regression/loss, no-config opening, malformed/foreign
canonical evidence, owner/timeout races, retries, rollback, missing/tampered rows,
legacy9 compatibility, repeated paths and unchanged World/RNG. Run affected Core
classes, Host suite, Runner and boundaries; format/build checks. New Core rows
require measured Release XML weights and actual discovery reconciliation.

Earlier independent-review budget exhausted3/3; do not reset it or claim new
independent acceptance. Coordinator review plus tests validate this packet;
exact-head hosted CI remains required before merge. No full Core rerun per edit.
Original P01/certificate/source-pin/dependency gaps and other fallback windows
remain separate; no public admission or RC readiness claim.
