# Creation-rooted released-I armed continuation v1

Status: `CMB-TASK-003D2c.3j` contract checkpoint, corrected before implementation after direct
predecessor inspection. The [combined Combat plan](../design/combat-cycle-implementation-plan.md),
[inherited Reserve Release](combat-inherited-reserve-release-v1.md),
[continuation design](../design/continual-cycle-reserve-composition-v1.md), and frozen Combat
selection/seal/result/settlement contracts govern this packet. Contract evidence only; no cycle
repeat, Combat execution, production schema, public action, Exercise, or simulator activation.

## Objective

Prove both accepted creation-rooted `3i` terminals have a source-legal and supported next-cycle
Combat continuation. Each released-I unit retains ammunition10, so D2b.2’s exhausted-ammunition
profile must reject direct reuse. This packet admits only the exact released-I/status-none profile,
derives one prospective candidate after empty Movement/Breakdown/presteps, and binds the complete
selection, sealed-round, result/settlement, and Snapshot contract support set. Later guarded cycle
control consumes this proof; 3j itself spends nothing and emits no event.

## Commands and project structure

- Focused: `python3 docs/specs/verify-combat-inherited-armed-continuation-v1.py`
- Direct predecessor: `python3 docs/specs/verify-combat-inherited-reserve-release-v1.py`
- Support oracles: run `verify-combat-selection-steps-v1.py`, `verify-combat-sealed-round-v1.py`,
  `verify-combat-result-settlement-v1.py`, and `verify-combat-snapshot-composition-v1.py`.
- Syntax/shape: `python3 -m py_compile docs/specs/verify-combat-inherited-armed-continuation-v1.py`
  plus JSON parsing, `git diff --check`, and local Markdown-link checks.
- Specification, closed typed inventory, two-case fixture, and failure-sensitive Python oracle live
  under `docs/specs`; combined plan is fifth primary file.

Canonical style follows predecessor packets: fixed-order ASCII UTF-8 JSON, mandatory nulls, exact
integer types, no extra keys, depth≤32, arrays≤512, and whole values≤1MiB. Oracle replays source
history and derives expected proof before comparing bytes; goldens never define behavior.

## Admission and proof

For `(axis, act-first)` and `(commonwealth, act-last)` seed1 Normal-Weather Reserve-I cases, replay
3i through authority25 and compare exact terminal Control bytes. Project only accepted release-I
disposition onto inherited World. Admit exactly:

- cycle1, first-acting-side slot, pending ordinal2 Movement exception in same scope;
- own member status none, releasedType I, releaseCycle1, CPA basis/ceiling10, spent CP0;
- unused offensive commitment, ammunition10, surviving infantry TOE10;
- adjacent opposing surviving infantry at CP0 under Normal Weather;
- empty attack/target-use history and no immediate vehicle, custody, guard, or future obligation.

Derive nextOrdinal2 and an unchanged-location, empty Movement/Breakdown/prestep path. Retain the
completed cycle1 Movement receipt as the prior location proof; prospective cycle2 emptiness emits
no receipt. Reuse the
inherited selection assessment algorithm: Normal Weather, adjacency, attacker CP≤5, defender CP≤7.
Exactly one canonical candidate must result for the actual acting side. Target-hex and released-I
offensive use are available; proof does not select, decline, seal, commit, draw RNG, or settle.

`support` pins exact canonical fixtures for five selection cases, four sealed-round cases, eight
result/settlement cases, and Snapshot composition’s matching 5/4/8 inventory. This is a bounded
released-I participation extension: release history adds scope, ceiling, exception, and unused
offensive-use guards while existing full-result semantics remain unchanged. Released-II, consumed
offensive use, changed resources, added participants, non-Normal Weather, nonadjacency, target reuse,
or any immediate obligation reject as unsupported rather than producing `supported=false`.

## Testing strategy and success criteria

Fixture holds one proof per actual owner. Oracle verifies exact replay/readback, immutable source
inputs, candidate identity, support counts/hashes, deep single-leaf mutations, rehashed semantic
forgeries, cross-owner/history substitution, malformed/alternate bytes, bounds, and source pins.
Completion also requires every direct predecessor/support oracle green.

| Requirement / decision | Evidence | Status boundary |
| --- | --- | --- |
| CYCLE-COMP-005/AC-007 | Exact supported Combat witness; unsupported/ambiguous legality rejects | Contract only |
| CYCLE-COMP-006/AC-008 | Signed release-I disposition is retained material progress | Contract only |
| CYCLE-COMP-009/AC-009 | Unused released-I offensive allowance and pending exception persist | No repeat yet |
| CON-002–004 | Canonical proof, source pins, strict readback and capacity | Advances open parent003 |

## Boundaries

Always replay 3i, carry rules/config/cycle/World/RNG/resource/history identity, and reject unknown
capability. Ask owner before adding released-II, new policy/version, registered runtime type, or an
independent review beyond exhausted15of15. Never mutate historical bytes, infer support from ammo
alone, execute Combat/repeat/Movement, or expose hidden opponent state.

After 3j, next child composes this proof with D2b.2 guarded repeat/finish. Positive released-Reserve
Movement/exception expiry, broader Reaction/vehicle families, D2c.4, Task004, checkpoint B, and
runtime/public/simulator work remain open.
