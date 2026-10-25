# Author explanation: persisted RBA timeout

Review target: working-tree change from `7554bb22` on
`codex/rc-overnight-plan-20261023`, including new source, tests, contract and plan.
Review instance 3 of 3. This packet is author testimony, not a readiness verdict.

## Intent and plan

Implement configured `rba = cancel-without-decline` for native Combat. Barrage
entry persists deadline before a defender request; unanswered decisions advance
without invented owner decline, Force seal, ammunition debit or random result.
Plan: `docs/work/plans/2026-10-24-rba-timeout-plan.md`. Public certificates and
original P01 ancestry remain excluded, as do unrelated fallback windows.

## Flow and responsibilities

Core V33 binds selected decision, original Barrage, retained retreat, timing hash,
clock envelope and same-slot RBA/Reserve Release cursors. Initial/repeated runtime,
checkpoint, contact projection and replay retain the cancellation. Adapter accepted
step version 27 imports it through the same deterministic Core path and compares
its System command. Native event version 33 and accepted-step version 27 are
separate version domains.

Host schema 8 persists `rba_rounds` at automatic Barrage entry in the existing
SQLite transaction. Retreat moves do not renew clocks. Valid decline closes the
round. Invalid submissions are validated before a clock gate can mutate state;
accepted retries recover existing receipts. Deadline/clock failure appends V33 and
structural continuation atomically. Missing configuration or clock before opening
uses explicit unavailable evidence with -1 timestamps, not invented history.

A hosted one-second timer uses the journal's injected TimeProvider and pages 16
campaigns, advancing its cursor even after an isolated campaign failure. Host
registration does not depend on outward API enablement. Legacy schema 5/6/7
journals remain readable but cannot be mutated; no implicit migration guesses
historical RBA start times.

## Alternatives and costs

Reusing Force cancellation would fabricate a decline and wrong predecessor;
request-only ticking would leave disconnected defenders stuck. V33 is explicit
rather than modifying old event bytes. Cost: new durable schema, strict replay
branches, and periodic replay/verification per collecting campaign. Paging bounds
work per sweep, but this is not a throughput certification.

## Invariants and checks

Preserve World/contact updates and RNG, including accepted retreat movement.
Reject changed canonical bindings and stale/foreign/malformed proposals. Replay
must verify round/transcript completeness and never accept absent certificate rows.
Focused Core and Host tests, affected consumers, boundary tests, format/build and
new-row timing discovery are validation gates; no full hour-long test rerun is
claimed. Exact final results will be supplied separately.

Initial Core Release build and six new Core cases passed; additional assertions
await rebuild. Host tests are running under a serialized build lease. Reviewer
preliminary scheduler FormatException/OverflowException finding is accepted and
assigned to Host worker with corruption isolation regression. Full persisted
repeated-assault coverage is still being checked, not claimed from synthetic frames.

## Limits and challenge points

Review transactional Barrage/round creation, all accepted-step replay consumers,
legacy read-only guards, retained retreat chains, repeated ordinal closure,
invalid-offer clock ordering, corruption isolation and scheduler fairness.
Original source-pin oracle failures, admission/certificate gaps and dependencies
remain open. No hosted CI, public RC readiness, merge or performance claim follows
from this bounded change.

## Final validation addendum

Core17/17, Host RBA13/13, whole Host141/141, Runner470/470 and boundary81/81
passed with zero failures/skips. Release solution build and format verification
exited0. Partition helpers194 passed elevated; initial sandbox error preserved.
Release discovery accounts for3606Core rows; exactly six new measured weights,
no existing weights changed. See handoff and artifacts/rc-rba-validation receipts.
Actual persisted ordinal2/3 timeout, deadline-insert rollback and malformed-seed
isolation regressions all passed. No full Core or hosted CI run claimed.
