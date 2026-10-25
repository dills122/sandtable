# Retreat and Selection deadline audit

Base `22c03209`, branch `codex/rc-overnight-plan-20261023`.
Ruflo task `task-1792884853053-r9n6av`, namespace `sandtable-rc-20261023`.

## Findings and selected packet

Host scheduler has five lanes: RBA, Force, Custody, Reserve Release and cycle
control. Source inspection of journal entry/opening/submission paths confirms
neither required post-assault Retreat nor Combat Selection has a persisted round.

Required Retreat is a concrete bounded gap: `AppendEmptyAntiArmor` retains the
assault result, then automatically advances only when no retreat is required.
A required retreat waits for the defender without an entry deadline. Owner
opening/submission paths do not gate this window. Configured fallback is
`retreat = refuse-retreat`, distinct from RBA's `cancel-without-decline`.

Selection is also open for initial and repeated Combat. Initial entry follows
legacy Breakdown; repeated entry follows retained repeated movement/Breakdown.
It needs its own entry contract and `no-selection` System semantics, not an
invented owner Finish or a reused post-selection no-attack record. Keep this
separate rather than broadening the selected fix across both windows.

## Implementation plan

Core worker owns explicit native System Retreat timeout (verify free version37),
source-legal refusal effects, initial/repeated replay/checkpoint/adapter, focused
Core tests and native spec. Host worker owns schema13, immutable result-entry
round, background lane, owner/timeout transaction, strict row verification,
focused Host tests and journal spec. Preserve existing owner event bytes.
Coordinator owns this audit, high-level docs, CI weights, final validation,
RC checklist, implementation commit and accumulated-branch PR.

Refusal must follow existing source effects through settlement; do not equate
refusal with no effect or fabricate a defender choice. Bind campaign, accepted
result, paid World/source hashes, authority and timing/clock evidence. Invalid
or stale proposals reject before clock mutation. Owner acceptance wins once;
timeout, round closure and successor/settlement effects commit atomically.
Queries and restart cannot renew entry deadline. Preserve historical schema
verification and read-only access; no invented migration timestamps.

## Validation and PR gate

Focused deterministic cases: required versus not-required result, exact deadline,
background no-query restart, rejected late proposals, owner wins/retry, clock
loss/regression, unavailable entry evidence, altered/missing rows, rollback and
repeated runtime. Check refusal settlement/TOE/RNG against source. Run affected
Core classes, Host/Runner/boundary/contract suites, Release build and format;
measure new Core rows and reconcile actual discovery. No full Core iteration.

Prior independent review budget remains exhausted3/3; report coordinator review
honestly. Preserve actual failing evidence. Update checklist with closed bounded
behavior and remaining Selection/integration/certificate gates.

Prepare accumulated branch PR after validation. Fresh origin/main fetch has no
incoming commits at audit start; no open PR exists for this branch. Retain commit
history and scope notes; do not rewrite, merge or close unrelated PRs. Exact-head
CI and release review remain required before merge. Draft status is appropriate
until those gates are confirmed; public RC certification remains separate.

## Selection follow-up anchors

Initial owner opening: `QueryCombatOwnerOpening` → `QueryOwnerOpeningCore`
CombatSelection branch. Owner submission reaches
`SubmitContactOwnerCombatSelection`. Repeated entry follows
`SubmitNativeRepeatMovement` → `AppendNativeRepeatBreakdown` →
`CompleteNativeRepeatBreakdown` → persisted `NativeRepeatBreakdownKind`;
`QueryRepeatCombatOwnerOpening` and `SubmitNativeRepeatCombat` expose/accept the
next selection. These paths have no Selection round initialization or gate.
Initial legacy Breakdown entry still needs its own precise contract boundary.

Graph coverage was checked for relied Core/Host files. New files were not tracked
and edited files metadata-changed in generation2026-10-24T23:27:50Z; Exercises
also reports a partial parse at line147. Changed source ranges were read directly;
this audit does not claim exhaustive graph coverage.
