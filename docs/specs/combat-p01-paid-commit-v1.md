# Private P01 ordinal-two paid commitment v1

**Status:** bounded private reference only. The
[schema](combat-p01-paid-commit-v1.schema.json),
[reference](verify-combat-p01-paid-commit-v1.py) and
[tests](test-combat-p01-paid-commit-v1.py), with a
[golden vector](fixtures/combat-p01-paid-commit-v1.json), add one irreversible **private**
System continuation at the prepared Close Assault boundary. They do not
admit P01, expose a public/native action, roll a result, settle a World, or
authorize P02/P03. All Control cuts retain admitted=false and empty public actions.

## Authority and predecessor

The only accepted predecessor is the original CreationRequest and eleven
separate parent ledgers, the frozen R03 ledger, the selected-assault ledger
with an accepted defender RBA decline, and the Force Assignment ledger with
both full 10-TOE owner seals and the exact empty Anti-Armor certificate. The
reference replays every canonical byte argument before a commit or readback.
Its input binds cycle, version, prefix, Close Assault position, previous
step receipt, selected candidate, Force Assignment round and all five
opening/seal/step receipts. The caller authenticates actor and structural-step
authority; the embedded System actor is evidence, not a separate credential.
No clock read or client-triggered cost command occurs here.

The source nests the complete canonical Force Assignment source; its own
ledger remains separate. ASCII JSON is compact, ordered and closed. Replay
compares recomputed event bytes and domain-separated receipt against the
recorded event; an identical retry returns the same receipt without a second
event. Changed source, parent ledger, actor, version, prefix, receipt,
allocation, Content, World, resource or policy pin rejects before an event.
Source, input, event, Control and projected-World byte caps are explicit and
checked at use.

## Complete pre-debit support gate

Before changing CP, ammo or history, reference rechecks adopted canonical
RulesInput golden, source hashes, CMB-SRC-RUL-001 three-cell amendment and
pinned Content bytes. Current authenticated ordinal-two World must equal
complete World7 initial fixture after normalizing only two known CP entries.
It verifies exact participants, current resources, empty target use/history,
role-ordered allocations and Force Assignment receipts. No source row or RNG
seed is selected to make an outcome succeed.

AC-004 enumerates **all** 1,296 Morale pairs, 6,480 chart coordinates,
8,840 selected result/refusal/capture cases, and 9,728 terminal settlement
Worlds **for each actor orientation**. Paid reference also projects every
reachable result through resolved, disposition, losses, retreat, custody
where applicable, and relationships cuts for current orientation. It
measures each canonical World byte length and rejects any cap excess. AC-005
independently checks exact participants/representations/hexes, all four
placement shapes, at most three TOE lost/captured, at least seven surviving
donor TOE, one-TOE guard capacity and eight source-reachable projected route
witnesses for current orientation. These normalized rules and World7 fixture
are reviewed **private** support model; full original evidence audit below
also rechecks physical source files. Later native resolver needs its own
complete support and capacity gate before admission.

Complete matrix is cached only by immutable RulesInput hash, creation
binding and orientation within one process. Every transition still checks
exact authenticated World, receipt ancestry, rules/content pins, resources
and physical geometry. Replay after restart recomputes matrix. Changing a
cap after cache population still rejects.

## One atomic private event

CombatAttackCommitted freezes both allocations and owner seal receipts,
candidate, selection/decline receipts, current World/RNG hashes, complete
preflight summary/hash, cost reasons and balances, one attack-history edge
and one same-segment target use. Acting CP increases 4 to 9 and defending
CP 0 to 3; both carried ammo balances fall 10 to 0 for 10 committed TOE
each. Cohesion stays 0. TOE, positions, representations, custody,
relationships and RNG stay unchanged. Post-cost ammo zero describes an
already-paid use, not a new automatic-surrender trigger. No separate charge
event or refund exists.

Pure reference returns one new source/ledger/event together; caller must
publish them atomically. Failed derivation or publication of that aggregate
leaves prior source, ledger and Control unchanged. Lost response after
publication replays to same receipt. No result roll or outcome-dependent
rejection follows inside this slice.

## Verification and remaining gates

Focused tests cover four genuine Prepared paths (both actor orientations
and both seal orders), source/ledger/receipt tampering, resources, Weather
ancestry, geometry, altered chart, caps, failed publication, retry and
readback. Full audit authenticates all 300 original retained rows and
source pins, executes this private transition for all 44 selected positives,
and preserves 256 candidate-absent rows and zero admission.

On the retained 300-row audit, the maximum committed source was 5,627,669
bytes, Control 44,589 bytes and event 5,978 bytes. The exhaustive current-side
result-cut matrix reached 11,521 bytes per projected World across 50,416
cuts. The schema caps are 6 MiB source, 64 KiB Control, 8 KiB event and
16 KiB projected World. Those are bounds for this pinned profile, not
general Combat capacity claims.

    python3 -B docs/specs/test-combat-p01-paid-commit-v1.py
    python3 -B docs/specs/verify-combat-p01-paid-commit-v1.py /Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence

This is one private cost boundary, not a full Combat loop. AC-003 action
exhaustiveness, full AC-006 automatic-surrender/result/RBA continuation,
AC-007 outward privacy and AC-008 all-row cuts, durable restart/readback
and resource caps remain open. World7 result/settlement projection is not
a native ordinal-two transition or durable save.
