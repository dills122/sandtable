# Native Retreat timeout V37

Status: bounded native runtime contract; no public/source-root admission.

A pending required post-assault retreat receives an explicit System
`combat-retreat-timeout` event (native V37, Host accepted-step V31). Fallback
`refuse-retreat` projects the same source-legal refusal losses as an accepted
owner refusal. It does not fabricate an owner action, choose a movement route,
roll dice, or debit ammunition again. Owner V23 records remain byte-compatible.

Evidence binds campaign, prior version, decision, position, sealed result hash,
paid World hash, retreat source hash, defender, timing configuration, opening,
deadline, high-water mark, optional observation and reason. Canonical timeout
contains no owner action ID. Replay regenerates refusal from retained
result/contact cut before comparing bytes. Initial and repeated assault paths
share replay dispatch. Checkpoints retain actual contract V37.

Reasons: `deadline`, `clock-unavailable`, `clock-regression`,
`controller-unavailable`, `retreat-unavailable`. Deadline must match configured
`retreat` budget. Foreign rules/content configuration rejects. Unavailable
entry carries all -1 clocks, no observation; absent configuration uses zero
hash sentinel. Duplicate, already resolved and non-required retreats reject.

Host owns durable deadline entry at accepted assault result, monotonic clock
observations, terminal evidence and atomic downstream settlement.
An owner query must never establish or renew window. Selection's separate
`no-selection` deadline gap remains open; this contract does not cover it.

Core repeated tests construct native runtime fixtures at ordinals 2 and 3; they
prove replay dispatch, not complete accepted Host history through multiple cycles.
