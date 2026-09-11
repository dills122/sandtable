# Creation-rooted inherited Reaction trigger v1

Status: `CMB-TASK-003D2c.3f` contract checkpoint; no runtime admission. Governing
[combined plan](../design/combat-cycle-implementation-plan.md),
[Movement predecessor](combat-inherited-movement-v1.md),
[schema](combat-inherited-reaction-trigger-v1.schema.json),
[oracle](verify-combat-inherited-reaction-trigger-v1.py) and
[retained vectors](fixtures/combat-inherited-reaction-trigger-v1.json).
Parent .3, .4, 003, 004 and checkpoint B remain open.

## Authority and closed scope

`initial(request,created,preamble,weather_events,stage_events,reserve_events,movement_events)`
replays the actual Movement predecessor from creation. Exactly one accepted Move4 record is
required: the resolved owner moved its ordinary independent infantry from its assault origin to
its own rear area. The resulting CP2, unchanged Cohesion0, moving route, World, receipts, progress,
version12 and prefix are the only admitted trigger base. Caller state is never authority.

`replay` adds final `events`; `apply` adds `events,inp`; `read_state` adds leading canonical `data`.
Every fresh command first reconstructs creation through the accepted Movement prefix. Cache
equality is diagnostic only. Input is the unchanged Movement commandVersion2 and must return the
same unit from own rear to its assault origin at expected version12. Retry at version12 returns the
accepted event bytes and current replayed state without another version, receipt, prefix or effect.
Conflicting reuse rejects. Public failures use `CMB-IRT-NNN`: 001 shape/type/size, 003 unsupported
family/profile, 004 provenance/source binding, 005 legality/actor, 006 stale/conflicting history,
007 capacity, 008 noncanonical bytes and 009 source/fixture evidence.

The committed destination is adjacent to exactly one opposing ordinary independent combat
representation at the other assault location. Trigger discovery precedes eligibility filtering;
the admitted profile then proves that representation eligible and freezes it. Zero or multiple
adjacent combat representations, zero or multiple eligible opportunities, nonnormal Weather,
Reserve/released units, vehicle cohorts, positive ZOC control, Movement-ended state and any other
history reject. This packet stops before participant selection or movement. It does not close the
window, resume Movement, complete either segment, admit runtime authority or imply simulator
support.

## Exact event, window and state

Schema `objects` strings define exact property names, order, types and nullability. Canonical UTF-8
JSON admits no whitespace, duplicate/unknown/missing or reordered properties, noncanonical numbers,
invalid UTF-8 or trailing data. Arrays retain predecessor canonical order; frozen and resolved
Reaction identities sort lexically. `null` means JSON null only; `empty` means `[]` only.

TriggerEvent retains all 26 Move4 legacy fields in predecessor order. Cost is Clear2, cumulative CP
is 2→4, Cohesion remains0, Movement-ended is null and Breakdown accounting is empty. The existing
route identity, first-move version12 and assault origin remain fixed while current location returns
to that origin. The event then carries the same nine inherited authority fields and uses receipt
domain `sandtable.combat.inherited-reaction-trigger-receipt.v1`. Historical Move4 bytes remain
unchanged.

`openedReactionWindow` is non-null and exact:

- `triggerCommittedStateVersion` is13; sides are the cycle-resolved phasing owner and its opponent.
- Reacting position suspends the unchanged sequence5 Movement position.
- Trigger authority binds moveContractVersion4, moved element, its post-move independent
  representation, rear origin and assault destination.
- Apparent trigger exposes only the same representation ID and locations.
- Exactly one frozen opportunity binds the opposing post-prefix representation. Its positive local
  adjacency evidence cites `spi-1979-land-rules` 8.51.
- Window and opportunity IDs are lowercase SHA-256 identities. Window preimage follows
  `sandtable.campaign.reaction-window.v1`; opportunity preimage follows
  `sandtable.campaign.reaction-opportunity.v1`. Ordered JSON fields match the pinned identity code.
- Resolved opportunity IDs are empty and active opportunity is null.

`breakdownFlowAfter` is reacting with a `resume-route` phasing continuation containing that same
updated route and null reactor route. `TriggerState` retains all predecessor authority and material
state, updates moved element/representation location and cumulative CP atomically, appends track,
receipt and actual progress once, and adds an explicit current Reaction position plus the exact
window. Suspended sequence context remains byte-equivalent to the predecessor position. World
relationships, ammunition, TOE, Cohesion, RNG, Weather, order, initiative, creation/cycle authority,
other unit, receipts and resource histories remain unchanged.

## Evidence and handoff

| Requirement | Executable evidence |
| --- | --- |
| IRT-001 creation-rooted authority | Both owners replay creation→opening→first Move4→trigger; missing, synthetic, cross-owner and altered predecessor records reject. |
| IRT-002 exact trigger/window | Literal CP4/location/version13, one eligible frozen opponent, identity preimages, Reaction position and suspended route assertions. |
| IRT-003 canonical/retry | Every cut, exact later retry, changed retry, raw codec and re-signed event/state mutation challenges. |
| IRT-004 preservation/fail-closed | Exact post-state comparison; zero/multiple opportunity, post-trigger command and unsupported sibling families reject. |

Source pins bind this packet, the four Movement predecessor artifacts, trigger factory, Reaction
identity/window and canonical codecs. They support prospective contract derivation; they are not
runtime test claims. Next bounded Reaction child owns participant selection/movement/completion and
window closure. Positive vehicle Breakdown, Reserve movement, armed continuation, .4 composition,
004/B, runtime Tasks008F/021 and simulator Task022 remain open.

Author verification: `python3 -B docs/specs/verify-combat-inherited-reaction-trigger-v1.py`
passes 2 actual owner traces/2 triggers, 4 replay cuts, 2 exact retries, 270 event/state
mutations, 60 raw rejections, 30 boundary checks and 11 source pins. Direct predecessor
`python3 -B docs/specs/verify-combat-inherited-movement-v1.py` passes 2 traces/14 moves,
16 cuts, 14 retries, 384 mutations, 66 raw rejections, 81 boundary checks and 18 pins. JSON
structural parse and diff checks pass. Literal trigger evidence failed with the expected
`NotImplementedError` before implementation. No .NET, runtime, simulator or independent-review
run is claimed.
