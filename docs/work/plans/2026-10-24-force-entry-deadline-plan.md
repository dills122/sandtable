# Force deadlines at decision entry

Base `80d2881d`, branch `codex/rc-overnight-plan-20261023`.
Ruflo task `task-1792880135655-ctru56`, namespace `sandtable-rc-20261023`.

## Behavior

Accepting a defender's RBA decline enters Force Assignment. Persist the immutable
Force round in that same transaction, using the trusted timestamp already checked
by the RBA submission gate and the pinned Force budget. No first owner query is
needed to start the deadline or enable background cancellation. Reopening,
restarting and sealing cannot renew it. Invalid offers remain rejected before
clock mutation; accepted retries recover their retained receipt.

Use schema 9 to make entry-time completeness explicit. Legacy5/6/7/8 journals
stay readable with their historical verification rules, but mutations require9.
Do not invent old entry timestamps or silently migrate lazy schema8 rounds.
Preserve v8 RBA verification when current schema advances.

Every schema9 accepted decline requires a matching persisted round or existing
explicit no-round Force-unavailable cancellation. Owner queries read existing
rounds only; a deleted row rejects rather than reopening. Keep creation atomic
without bypassing strict Load verification: use the known accepted successor
returned by PersistAccepted until paired row/continuation exists, then verify.
No new Core event semantics intended; existing V19/V20/V21 cancellation remains.

## Validation and review

RED test proves no-query round absent/stalled before fix. Focused deterministic
checks cover entry timestamp/budget, late first query, zero owner queries and cold
restart, repeated assault entry, atomic insert rollback, deleted round rejection,
legacy8 reads/mutation rejection and existing Force/RBA behaviors. Run whole Host
once after focused checks; solution build and format. Core/Runner suites only if
Core/contracts change or evidence reveals consumer impact.

Coordinator source/diff review; prior independent-review budget remains exhausted
at3/3 and is not reset. No new independent acceptance or public RC claim. Exact-head
hosted CI required before later merge. Preserve original certificate/source-pin
failures and dependency gaps honestly.

## Regression-driven scope adjustment

Repeated-assault timeout regression exposed an existing adapter query returning
a cancelled repeated Force round as collecting. Add a narrow terminal-state guard
to QueryNativeCombatForceRoundBinding; no event/schema-of-event change. Validate
affected focused Core classes and Runner alongside Host. Retain failed focused
run; do not weaken repeated tick idempotence assertions.
