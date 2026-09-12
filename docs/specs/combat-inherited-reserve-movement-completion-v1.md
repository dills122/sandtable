# Creation-rooted released-I Movement completion v1

Status: `CMB-TASK-003D2c.3m` contract checkpoint. The
[released-I ordinal-2 Movement](combat-inherited-reserve-movement-v1.md),
[inherited Movement lifecycle](combat-inherited-movement-lifecycle-v1.md),
[cycle-control expiry projection](combat-cycle-control-v1.md), and
[combined Combat plan](../design/combat-cycle-implementation-plan.md) govern this packet.
Contract evidence only; no Breakdown execution, Combat continuation, Snapshot12, runtime, public
action, Exercise, or simulator activation.

## Objective

Replay each canonical 3l terminal at authority28, close its ordinal-2 Movement through the
accepted lifecycle order, and expire the released-I member's pending one-cycle exception from the
real Movement-completion receipt. Owner deliberately stops; System resolves the empty Breakdown
cohort; owner completes Movement into Breakdown Determination. Success ends at authority31
without executing Breakdown.

## Contract and project structure

`InheritedReserveMovementCompletionBase` carries actor plus exact 3l predecessor hash/base/input/event
and canonical Movement, Breakdown-stop, and Breakdown positions. Reader independently regenerates
and byte-compares the 3l trace; caller-supplied matching hashes never suffice.

`InheritedReserveMovementCompletionState` retains ordinal-2 Authority, World, RNG, attack history,
released member, D2a Movement track, lifecycle flow/context, current Movement-end proof and local
receipts. Files live with other retained contracts:

- schema: `docs/specs/combat-inherited-reserve-movement-completion-v1.schema.json`;
- vectors: `docs/specs/fixtures/combat-inherited-reserve-movement-completion-v1.json`;
- executable contract: `docs/specs/verify-combat-inherited-reserve-movement-completion-v1.py`.

## Lifecycle and style

Commands use closed canonical JSON and hash-derived capabilities. Profile-specific v1 events adapt
the accepted `stop-element-movement`, `resolve-breakdown-stop`, and `complete-movement-segment`
semantics and order without claiming wire compatibility with 3b's richer first-cycle envelopes.
Stop binds the accepted 3l Movement receipt and exact `MovementTrack`; it does not invent the richer
first-cycle `MovingRoute` or creation fields that 3l no longer carries.

```text
authority28 moving
  -> owner stop / authority29 / Breakdown-stop interrupt
  -> System resolve / authority30 / resumed Movement idle
  -> owner complete / authority31 / Breakdown Determination
```

Completion derives end locations from current World, binds ordinal2 proof to its accepted receipt,
then invokes unchanged D2b.2 `expire_movement`. Result replaces member/proof atomically: matching
pending ordinal2 exception becomes expired with that same completion receipt. World, RNG, attack
history, CP2, ammunition10, TOE10, Cohesion, and route track remain unchanged.

## Commands

```sh
python3 docs/specs/verify-combat-inherited-reserve-movement-completion-v1.py
python3 docs/specs/verify-combat-inherited-reserve-movement-v1.py
python3 docs/specs/verify-combat-inherited-movement-lifecycle-v1.py
python3 docs/specs/verify-combat-cycle-control-v1.py
just check
```

## Testing strategy and success criteria

Retained fixture freezes axis and Commonwealth traces, three events each, every cut, exact retry,
canonical bytes, source pins, mutation rejection, and owner/System authority boundaries.

Success requires:

- byte-exact 3l replay for both owners;
- authority `28→29→30→31` and exact interrupt/resume/terminal positions;
- stop bound to accepted 3l track and move receipt;
- empty resolution with unchanged RNG and rules source `21.24-21.26`;
- ordinal2 Movement proof covering the same units as ordinal1 and using current World locations;
- pending exception expiry only from the accepted completion receipt;
- no World/resource/history/track drift and no Breakdown execution.

Reject wrong predecessor, actor, order, cycle, position, track, move receipt, stop capability,
version, proof, expiry result, stale command, overflow, malformed bytes, self-consistent re-signing,
and unsupported fourth event. Exact accepted retry returns the original event/receipt without a
second transition.

## Boundaries and open questions

Always retain deterministic replay, closed bytes, source pins, both-owner symmetry, and exact
`D2b.2` projection. Ask before widening into runtime/Snapshot/public-action work. Never treat the
completion projection as Breakdown execution or reveal hidden opposing state to intelligence.

No open question blocks this checkpoint. Later Breakdown/Combat continuation and remaining
positive Movement/Reaction/vehicle families stay explicit follow-on work.
