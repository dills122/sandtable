# Creation-rooted inherited Reserve Release v1

Status: `CMB-TASK-003D2c.3i` contract checkpoint, accepted by owner on 2026-09-11. The
[combined Combat plan](../design/combat-cycle-implementation-plan.md),
[private Release contract](combat-reserve-release-v1.md), and
[inherited Reserve-cycle parent](combat-inherited-reserve-cycle-v1.md) remain governing authority.

## Objective

Consume only the accepted Axis and Commonwealth Normal-Weather Reserve-I terminal histories from
3h. Reconstruct their complete creation-rooted authority, open one first-occurrence Reserve Release
decision, accept the owner `release-I` disposition, and complete Release. Preserve all prior World,
RNG, Weather, sequence, designation and structural history while changing the sole own member from
Reserve I to none and recording its exact ordinal-2 Movement exception. Stop before guarded cycle
repeat, ordinal-2 Movement, full Snapshot composition or simulator/runtime activation.

## Commands and event order

The only accepted positive three-event path is:

1. System `open` -> `reserve-release-opened`1 with the sole Reserve-I UnitKey pending, one decision
   ID and one Config-pinned `reserve-release` Timing;
2. actual cycle owner `choose release-I` before the deadline ->
   `reserve-unit-disposition-recorded`1, changing I to none;
3. System `complete` -> `reserve-release-completed`1 with the disposition receipt and no retained
   unit.

The 3h terminal is authority version 22 at first-slot Reserve Release, so accepted events advance
22→23→24→25 and extend Chronicle prefix exactly once each. Completion remains at the same sequence
position. It neither closes ordinal 1 nor opens ordinal 2.

Exact retries return accepted event bytes. Changed stale, wrong-owner, out-of-order, duplicate,
unknown-choice, late, malformed, cross-history and unsupported conversion/retention inputs reject
before mutation. Clock regression, clock unavailability, expiry and unavailable-controller paths
must deterministically enter D2b.1 fallback behavior without model or remote I/O; they are negative
and recovery probes, not additional positive fixture lineages.

## Authority, history and privacy

Every fresh command derives from full accepted creation→3h history. Optional cached Release state
is advisory and must equal replay. The inherited Release base binds:

- exact cycle, same-slot Reserve Release position, 3h terminal version/prefix and final Combat-step
  receipt;
- full accepted 3h predecessor bytes and source-pinned codecs;
- retained World hash, RNG, stage attack history and complete own Reserve membership/history;
- one acting-side member with real UnitKey/designation receipt, `status=I`, `baseCpa=10`, spent CP0;
- nullable accepted high-water derived as `null`, because no earlier contract in this lineage
  persists reliable clock evidence.

The first accepted opening timestamp establishes Release high-water and Timing. Hashes alone do not
admit state. A reconstructed reader verifies retained World and changes only the matched own unit's
`reserveStatus`; no opposing hidden state appears in any public capability or owner command.

The release disposition records `releasedType=I`, `releaseCycle=1`, `cpaBasis=10`,
`voluntaryCeiling=10`, its receipt, and pending `nextMovement` authority scoped to the same turn,
stage, slot and acting side at ordinal 2. The exception waives only ordinary two-hex proximity;
this packet does not assess a destination, spend CP, move the unit, expire the exception or reset
cycle-local state.

## Preserved state and progress

Across all three events preserve byte-for-byte:

- World except the one accepted `reserveStatus` transition I→none at disposition;
- every location, CP, DP, Cohesion, TOE, ammo, BP, relation and obligation;
- RNG state/cursor, Weather, stage order, setup/content/creation identities and cycle ordinal 1;
- real designation receipt, 3h Movement-end proof, selection/no-attack receipts and full prior
  prefix/history.

Release I→none is material progress and must be exported with exact event type/hash/receipt for
later D2b.2 assessment. Release opening and completion are not material progress. Timing/high-water,
Release control, disposition/history, state version and prefix are the only other advancing facts.

## Project structure and implementation style

Implementation remains within five primary files:

- this specification;
- `combat-inherited-reserve-release-v1.schema.json` ordered canonical inventory;
- `fixtures/combat-inherited-reserve-release-v1.json` two inherited vectors and retained goldens;
- `verify-combat-inherited-reserve-release-v1.py` executable oracle;
- one update to `combat-cycle-implementation-plan.md` for boundary/status.

Follow existing compact Python contract-oracle style: strict ASCII canonical JSON, mandatory nulls,
fixed field/array order, domain-separated SHA-256 identities, immutable predecessor readers, pure
transition kernel and separate history-rooted apply/readback boundaries. Reuse frozen D2b.1
semantics without importing its synthetic `base_for` authority. Add no dependency or production
runtime registration.

## Testing strategy and commands

RED must fail after replaying both valid 3h terminals because inherited Release composition is
missing. GREEN must cover both owners, all four state cuts, exact retry bytes, re-signed deep
event/state mutations, malformed canonical bytes, source pins, deadline/fallback controls,
version/capacity boundaries and cross-history forks.

```text
python3 -B docs/specs/verify-combat-inherited-reserve-release-v1.py
python3 -B docs/specs/verify-combat-inherited-reserve-cycle-v1.py
python3 -B docs/specs/verify-combat-reserve-release-v1.py
python3 -B docs/specs/verify-combat-cycle-control-v1.py
git diff --check
```

## Success criteria and traceability

| Requirement | Decision/input | Verification | Status |
| --- | --- | --- | --- |
| `CMB-IRR-REQ-001` actual positive Release lineage | 3h both-owner terminal histories | predecessor-byte/hash and source-pin checks | complete |
| `CMB-IRR-REQ-002` ordered first-I control | D2b.1 open/choose/complete semantics | three exact events and all replay cuts | complete |
| `CMB-IRR-REQ-003` Release history projection | POL-007/008 and actual member | I→none plus receipt/CPA/exception assertions | complete |
| `CMB-IRR-REQ-004` deterministic timing/recovery | one Config-pinned budget | retry, deadline, clock and fallback probes | complete |
| `CMB-IRR-REQ-005` retained authority/privacy | CON-002–004 | World/RNG/history equality and cross-owner rejection | complete |
| `CMB-IRR-REQ-006` bounded deferral | D2b.2 and D2c.3/4 gates | repeat/move/Snapshot/runtime rejection probes | complete |

## Boundaries and open questions

Always preserve exact inherited bytes, authority hierarchy, fog-of-war boundary and deterministic
fallback. Ask owner before accepting `convert-to-II`, widening beyond the two Normal-Weather cases,
combining guarded repeat, or adding gameplay policy. Never edit frozen predecessor artifacts,
derive trusted state from a supplied digest, infer repeat from Release completion, expose hidden
opponent state, or claim C# runtime/simulator support.

Owner approved this release-only three-event boundary. Guarded ordinal-2 repeat and positive
Reserve movement remain dependent children.

## Author verification

`python3 -B docs/specs/verify-combat-inherited-reserve-release-v1.py` passes two both-owner traces
and six events: eight state cuts, six accepted retries, 658 valid-but-wrong Base/event/Control
mutations, 24 malformed-byte rejections, 31 authority/timing/capacity/unsupported-family boundaries,
10 deterministic recovery paths and seven source pins. Retained probes explicitly cover out-of-order
and duplicate events, unknown choice, post-open clock regression/unavailability, and excluded
complete-release/repeat/Movement/Snapshot/runtime commands. Retained vectors include both
predecessor/base hashes, every Control frame, every event hash and both terminal completion events.
These are regression vectors recorded after literal behavior assertions, not independent serializer
parity.
