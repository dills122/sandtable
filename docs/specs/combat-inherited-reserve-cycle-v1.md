# Creation-rooted inherited Reserve cycle entry v1

Status: `CMB-TASK-003D2c.3h` contract checkpoint, accepted by owner on 2026-09-11. The
[combined Combat plan](../design/combat-cycle-implementation-plan.md) remains governing authority.

## Objective

Consume only the accepted `normal-act-first-I` and `normal-act-last-I` histories from
[Reserve designation and first opening v1](combat-reserve-designation-v1.md). Carry each actual
held-Reserve infantry unit through a direct no-move Movement completion, idle Breakdown completion,
empty Combat selection and six no-attack structural completions to the same slot's Reserve Release.
Preserve the real designation receipt/history and `reserveStatus=I`. Stop before any release action,
cycle repeat, positive Reserve movement, full Snapshot composition or simulator/runtime activation.

## Commands and event order

The only accepted ten-event path is:

1. owner `complete-movement-segment` -> `movement-segment-completed`3;
2. System `complete-breakdown-segment` -> `breakdown-segment-completed`2;
3. System opens the derived empty Combat selection -> `combat-selection-opened`2;
4. System closes it with no candidates -> `combat-selection-closed`2;
5. System completes Position Determination -> Barrage;
6. System completes Barrage -> Retreat Before Assault;
7. System completes Retreat Before Assault -> Force Assignment;
8. System completes Force Assignment -> Anti-Armor;
9. System completes Anti-Armor -> Close Assault;
10. System completes Close Assault -> same-slot Reserve Release.

No route is active at initial Movement, so no `element-movement-stopped` or
`breakdown-stop-resolved` event is allowed. The designated unit remains at its original location
with CP0. Movement-end proof binds all final locations, the real completion receipt and canonical
ordinary-proximity exclusions; later Release exception logic may waive proximity, but this packet
does not create that exception.

## Authority, replay and privacy

Every fresh command derives from full accepted creation, preamble, Weather, stage-entry and 2d
Reserve records plus all accepted 3h predecessors. Cache state is advisory and must equal replay.
Each command binds current state version, exact sequence position, cycle/creation/configuration
identity and current public capability where applicable. Exact retries return original event bytes;
changed stale, reordered, omitted, duplicated, cross-owner or cross-history inputs reject before
mutation.

Public selection projection contains zero candidate IDs and no opposing hidden state. System-only
structural actions are not published as player choices. Receipt and prefix identity use canonical
event bytes and append exactly once.

## Preserved state and progress

Across all ten events preserve byte-for-byte:

- World except for no fields at all: both locations, CP, Cohesion, TOE, ammo and Reserve status are
  unchanged;
- `reserveStatus=I`, designation receipt and complete Reserve history for the acting unit;
- Weather, RNG state/cursor, stage order, setup/content/creation identities and future obligations;
- opening base/completion identity, cycle1 owner/slot/ordinal and all prior receipts;
- zero new material-progress references.

Only state version, Chronicle prefix, current sequence position, event receipts, Movement-end proof
and bounded selection/traversal control advance. Terminal state is closed at same-slot Reserve
Release and does not release, convert or reset anything.

## Project structure and implementation style

Implementation remains within five primary files:

- this specification;
- `combat-inherited-reserve-cycle-v1.schema.json` ordered canonical inventory;
- `fixtures/combat-inherited-reserve-cycle-v1.json` retained literal vectors/goldens;
- `verify-combat-inherited-reserve-cycle-v1.py` executable oracle;
- one update to `combat-cycle-implementation-plan.md` for boundary/status.

Follow existing compact Python contract-oracle style: explicit ordered object descriptors, strict
ASCII canonical JSON, domain-separated SHA-256 identities, immutable predecessor readers, pure
transition kernel and separate history-rooted apply/readback boundaries. No new dependency.

## Testing strategy and commands

RED must fail before transition implementation on missing direct no-move Reserve-cycle handling.
GREEN must cover both resolved owners, every prefix cut, exact retry bytes, deep event/state
mutations, malformed canonical bytes, source pins, capacity/version boundaries and cross-history
forks.

```text
python3 -B docs/specs/verify-combat-inherited-reserve-cycle-v1.py
python3 -B docs/specs/verify-combat-reserve-designation-v1.py
python3 -B docs/specs/verify-combat-inherited-movement-lifecycle-v1.py
python3 -B docs/specs/verify-combat-inherited-breakdown-completion-v1.py
python3 -B docs/specs/verify-combat-inherited-selection-v1.py
python3 -B docs/specs/verify-combat-inherited-no-attack-v1.py
git diff --check
```

## Success criteria and traceability

| Requirement | Decision/input | Verification | Status |
| --- | --- | --- | --- |
| `CMB-IRC-REQ-001` actual positive Reserve lineage | 2d both-owner Normal `I` histories | predecessor-byte/hash and source-pin checks | proposed |
| `CMB-IRC-REQ-002` direct no-move Movement end | idle completion source plus completion3 fields | exact event, exclusion and World/CP assertions | proposed |
| `CMB-IRC-REQ-003` structural first-cycle close | 3c/3d/3e successor rules | ten events and every replay cut | proposed |
| `CMB-IRC-REQ-004` Reserve/history preservation | POL-007/008 and D2b contracts | status/history/receipt equality at every cut | proposed |
| `CMB-IRC-REQ-005` deterministic authority | CON-002–004 | retry, mutation, malformed and fork suites | proposed |
| `CMB-IRC-REQ-006` bounded deferral | D2c.3/4 and Task004/022 gates | release/repeat/move/runtime rejection probes | proposed |

## Boundaries and open questions

Always preserve authoritative history, exact predecessor bytes, fog-of-war boundaries and
deterministic replay. Ask owner before widening beyond two Normal-Weather `I` cases or adding a new
gameplay policy. Never edit frozen predecessor artifacts, infer release from arrival at Reserve
Release, treat unsupported capability as empty work, or claim C# runtime/simulator support.

This closes `CMB-TASK-003D2c.3h` only. Actual release, repeat and positive Reserve movement remain
dependent children; no runtime or simulator support is inferred.
