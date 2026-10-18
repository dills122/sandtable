# P01 automatic surrender and RBA boundary v0

**Status:** private diagnostic contract only. This neither settles an automatic surrender nor
opens ordinal-two RBA. It cannot admit a P01 action or publish a public action.

The closed field inventory is in
[`combat-p01-surrender-rba-boundary-v0.schema.json`](combat-p01-surrender-rba-boundary-v0.schema.json).
Inputs must come from authenticated authority, never from a user proposal or a decoded R03 row
accepted as authority. The current witness reconstructs the original CreationRequest and eleven
separate frozen R03 ledgers before inspecting the active ordinal-two Combat Position terminal.
There is no ordinal-two assault or RBA decision in those terminals.

## Source condition

Pinned Land §15.88 and September errata §15.88 distinguish automatic surrender when a unit at
Cohesion −17 or lower **is assaulted** from surrender when an enemy **moves adjacent** to a unit
at −26 or lower. September errata §50.12 requires an actual close or anti-armor assault to make
an ammunitionless unit surrender; existing adjacency, ZOC and Barrage do not suffice. Inspect
target Cohesion and ammunition **before the trigger**. A zero balance after a paid assault is not
the pre-assault value for another event. `CMB-POL-009` excludes the distinct voluntary surrender
choice from this bounded RC exercise.

The private diagnostic returns `requires-typed-settlement` when an authenticated trigger would
require automatic surrender. It returns `no-automatic-surrender-at-trigger` when the typed
source condition does not require surrender; that result alone does not authenticate the trigger.
`surrenderSettlementReceiptId` must
remain null in v0: no typed ordinal-two surrender event, full-TOE custody/relationship settlement,
or resulting World and receipt chain has been proved. A triggering state cannot be downgraded to
an empty action or ordinary assault result. Missing trigger, pre-event values, participant identity,
or source receipt fails closed before this diagnostic can be used.

## RBA control

`unopened` has neither decline nor cancellation receipt. `declined` requires a defender-owned
accepted decline receipt and no cancellation; `cancelled` requires the system cancellation
receipt and **no** decline receipt. These are mutually exclusive control shapes. Under
`CMB-POL-004`, timeout or trusted unavailability cancels a live RBA without fabricating a decline,
and later stale timeout cannot undo accepted decline. The existing ordinal-one selection contract
provides eight regression witnesses across both acting sides. They do not authenticate an
ordinal-two P01 RBA transition, deadline, actor, prefix, or receipt. Accordingly
`ordinalTwoRbaProofProved=false` for every v0 diagnostic.

The full `P01-AC-006` gate remains open until a versioned ordinal-two input/event contract proves
trigger provenance, automatic full-TOE/custody settlement, and actual decline/cancellation replay
with exact authority/clock/receipt binding. Every v0 output has `admitted=false` and an empty
`publicActions` array. P02 selection and P03 native consumption remain separate.
