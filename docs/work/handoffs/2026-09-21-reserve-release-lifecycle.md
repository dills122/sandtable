# Reserve Release lifecycle delivery — 2026-09-21

## Scope and current state

User approved a75–90minute slice beginning22:45UTC. Task017A2 implements dormant isolated Release:
explicit opening, fixed own-unit queue, first-I release/conversion, later-II release/retention,
one Config deadline, deterministic fallback, idempotent retry, canonical replay and completion.
Task017A2 accepted after full gates, three fresh Ready reviews and exact-candidate CI.

Branch `codex/combat-reserve-release-lifecycle`; [draft PR139](https://github.com/dills122/sandtable/pull/139).
Original candidate3811202 was rebased onto main32a038c after PR138 squash merge. Rebased candidate
`a4b4f630d1250fd00d61ebae486f18813ffedd01` has exactly identical tree; oldbasea1cd425 and main32a038c
also have identical trees. This removed ancestry-only PR conflicts without changing reviewed code.
No merge performed. Original dirty `/Users/dsteele/repos/sandtable` working tree remained read-only.

## Implementation and proof boundary

Four source/test paths per [dispatch](../../../.planning/combat-task008-delivery/task017a2-dispatch.md).
Initial A1 base validation remains unchanged. Apply reconstructs prior state from independently
retained base/request and separately trusted input/event history; no caller state authorizes work.
Complete state/event bytes are checked on replay. Original input actor persists through System
fallback. Immutable histories record actual conversion/release receipts and pending next-Movement
exceptions; no Movement executes. Completion remains at Reserve Release.

Proof:44isolated traces,132event hashes,176state hashes/byte lengths andtwo terminal event literals.
Four historical Result1 rows excluded. Source/fixture hashes and full check/review results retained
in [checks](../../../.planning/combat-task008-delivery/task017a2-checks.md) and [A2 evidence](../../../.planning/combat-task008-delivery/task017a2-evidence.md).
No World projection, positive campaign provenance, public Combat activation, cycle control,
Snapshot successor or durable publication claimed. Parent017 remains open.

## Next bounded slice

Task017B: derive native empty Release bases from actual accepted Result2/Task016 terminals across
32selected contexts, then replay explicit open/complete with64native event literals and checkpoint
readback. Preserve resources, attack/custody/future-duty history and separate hash domains. Current
A1 admits isolated-ledger only; any adapter profile/context extension requires explicit bounded
manifest and authenticated predecessor verification, never caller-provided World hashes alone.

Positive held-I/no-move3h predecessor and3i bridge remain separate. Current inherited Movement
requires moves and rejects held Reserve. Later-II and consumed actual campaign lineage remain
unproved. Tasks018/019 movement/repeat control and020–025 public/Runner/loop gates remain open.
No next slice automatically dispatched.

## Final acceptance record

Full solution:2,347passed/0failed/0skipped9m46.822s; Boundary81passed; build0warnings/errors;
full format/diff passed. Three fresh sequential independent reviews Ready, no findings. Reviewer3
independently rebuilt full Release solution13.90s0warnings/errors, then16focused/81Boundary passed.
Exact source-candidate CI all8checks passed; verify35665875884 confirms2,347/0/0 in9m55.443s.
Root read all blind ledgers/reports. Four A2source pins and eight Task016pins remain exact.
Documentation publication follows accepted source candidate without source/test changes; resolve
publication commit via git log for this file and check latest PR status. No next slice dispatched.

Canonical [plan](../../design/combat-cycle-implementation-plan.md) and
[roadmap](../../roadmap/pre-alpha-roadmap.md) keep parent017 and public gameplay gates open.
Optional CCE remains blocked by earlier automatic approval review; local evidence used, no retry.
