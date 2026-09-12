# Creation-rooted guarded cycle repeat/finish v1

Status: `CMB-TASK-003D2c.3k` contract checkpoint. The
[combined Combat plan](../design/combat-cycle-implementation-plan.md),
[private cycle control](combat-cycle-control-v1.md),
[inherited Reserve Release](combat-inherited-reserve-release-v1.md), and
[armed continuation](combat-inherited-armed-continuation-v1.md) govern this packet. Contract
evidence only; no ordinal-2 Movement, Combat execution, Snapshot12 admission, production schema,
public action, Exercise, or simulator activation.

## Objective

Compose both accepted creation-rooted 3i release-I terminals with their exact 3j ammunition10
continuation proofs. Freeze owner repeat and finish through the unchanged D2b.2 timing, retry,
fallback, successor and closure semantics. Repeat is now reachable because one supported Combat
witness and one signed material-progress reference coexist. This adapter authenticates full source
bytes and changes no historical contract.

## Commands and project structure

- Focused: `python3 docs/specs/verify-combat-inherited-cycle-control-v1.py`
- Direct sources: run `verify-combat-inherited-armed-continuation-v1.py`,
  `verify-combat-inherited-reserve-release-v1.py`, and `verify-combat-cycle-control-v1.py`.
- Syntax/shape: `python3 -m py_compile docs/specs/verify-combat-inherited-cycle-control-v1.py`,
  JSON parsing, `git diff --check`, and local Markdown-link checks.
- Specification, closed typed inventory, four-trace fixture, and failure-sensitive Python oracle
  live under `docs/specs`; combined plan is fifth primary file.

Canonical style follows the three sources: fixed-order ASCII UTF-8 JSON, mandatory nulls, exact
integer types, no extra keys, depth≤32, arrays≤512, and complete values≤1MiB. Fixture goldens are
regression evidence only; oracle independently replays all source history before comparison.

## Authenticated base and assessment

`InheritedCycleControlBase` contains actor, exact 3i Control hash, full 3j Proof and a
D2b.2-shaped `CycleControlBase`. The nested profile ID includes the canonical proof digest, while
outer admission replays and byte-compares full 3i and3j values. A matching hash alone never admits.

Derive nested base only from actual sources:

- exact inherited ReleaseBase, three trusted inputs/events, original pre-release World and
  cycle1 Movement-end proof;
- closed authority25 Release state, release-I member now status none, pending ordinal2 Movement
  exception, unchanged ammunition10/TOE10/CP0/RNG/history;
- one signed `reserve-unit-disposition-recorded` material-progress reference;
- one exact 3j candidate for the adjacent opponent under Normal Weather and all full-result support
  pins.

Map that candidate to one typed `ContinuationWitness(kind=combat)`: acting unit, defending
location, zero Movement terrain/break-off/DP, current CP0, and no release-exception claim. Assessment
uses `combatAssessment=supported-armed-combat`. This is a legality/support witness, not target use,
selection, commitment, dice, cost, settlement or execution.

## Control behavior

Reuse D2b.2 `CycleControlCommand`, `CycleControlEvent`, `CycleControlState`, receipts, control
domain, timing and transition rules unchanged. The proof-bound nested profile changes base/control
identity so synthetic D2b.2 commands cannot cross into this adapter.

- `open`: System only. With progress and Combat witness, opens one existing decision/timing.
- `repeat`: actual cycle owner before deadline. Emits exactly one
  `movement-combat-cycle-repeated`, closes ordinal1, opens ordinal2 at the same-slot Movement
  position using prior prefix/version, retains full World/RNG/attack/release history and pending
  exception, and resets only occurrence-local target-use/progress.
- `finish`: actual owner before deadline. Emits one `movement-combat-phase-finished`, closes
  ordinal1, enters same-slot Truck Convoy, and expires the pending exception with closure receipt.
- expiry, controller unavailable, opening-clock loss, clock regression and unavailable clock
  deterministically finish with exact D2b.2 reasons. Duplicate canonical commands return accepted
  receipts; changed stale commands reject.

Opening requires capacity for open plus closure. Repeat/finish never mutates ammunition, TOE, CP,
Cohesion, locations, RNG, attack history, full release history or future obligations. Neither path
performs stage housekeeping or changes side/slot.

## Testing strategy and success criteria

Fixture freezes axis/commonwealth × repeat/finish. Oracle proves exact base/proof/source readback;
two-event cut replay; retry identity; event/state/base/proof deep mutations; self-consistent
semantic forgeries; cross-owner/history substitution; malformed/alternate bytes; timing/fallback;
version/ordinal/size capacity; and pinned source hashes.

| Requirement / decision | Evidence | Status boundary |
| --- | --- | --- |
| CYCLE-COMP-005/AC-007 | Exact 3j Combat witness makes continuation supported | Contract only |
| CYCLE-COMP-006/AC-008 | Exact 3i disposition supplies material progress | Contract only |
| CYCLE-COMP-009/AC-009 | Repeat retains pending ordinal2 release-I exception; finish expires it | No Movement execution |
| CON-002–004 | Canonical base/event/state replay and exact successor/closure identity | Advances open parent003 |

Success requires four canonical traces and every direct source oracle green. Unsupported proof,
resource, participant, candidate, support, progress, obligation, owner or history rejects rather
than forcing finish.

## Boundaries and next gates

Always replay 3i/3j, retain hidden authority locally, and reject unknown capability. Ask owner
before adding released-II, new policy/version, runtime registration, or another independent review
beyond exhausted15of15. Never mutate historical bytes, infer continuation from ammunition alone,
or execute Movement/Combat.

After3k, positive released-Reserve Movement/exception completion, broader Reaction/vehicle/Reserve
families and D2c.4 full World/Snapshot composition remain open before Task004/checkpoint B. Parent
`003D2c.3`, `003D2c` and `003` remain open.
