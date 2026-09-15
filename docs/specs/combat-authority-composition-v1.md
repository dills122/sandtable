# Combat authority composition v1

Status: `CMB-TASK-003D2c.4` contract checkpoint complete. This packet closes the contract scope of
parent `CMB-TASK-003D2c`, `CMB-TASK-003D2`, `CMB-TASK-003D`, and `CMB-TASK-003` by composing the
accepted selected-profile authority and supplying the `CMB-TASK-004` handoff. Checkpoint B remains
open until Task004 freezes CON-005/006 and maps all 72 acceptance criteria. No Combat runtime,
public action, Exercise, Runner, simulator, or hosted intelligence capability is active.

The [combined plan](../design/combat-cycle-implementation-plan.md),
[schema inventory](combat-authority-composition-v1.schema.json),
[retained fixture](fixtures/combat-authority-composition-v1.json), and
[executable oracle](verify-combat-authority-composition-v1.py) define this contract.

## Objective and trust boundary

Compose actual creation-rooted authority from already-frozen predecessor oracles. Each trace is
regenerated from its predecessor command/event chain, byte-compared with that predecessor's retained
golden, and only then projected into one closed `RootSnapshot`. Caller-supplied hashes, cached state,
or fixture assertions never substitute for replay.

`RootSnapshot` retains:

- campaign, Rules10, configuration, creation-binding and Created11 identities;
- exact state version and pre-event prefix;
- authority header, symbolic cycle and sequence position as canonical fragments;
- full World7 and deterministic RNG state;
- family-specific authority arms and every discoverable command receipt;
- all World7 obligation collections, including empty collections; and
- exact terminal predecessor bytes, type, length and digest.

Canonical fragments are closed ASCII JSON. Each stores source contract, type label, canonical bytes,
byte count and SHA-256 digest. Full snapshot encoding is independently canonicalized and hashed.
Unknown keys, reordered keys, duplicate keys, alternate number spellings, whitespace, BOM, malformed
bytes, changed cached values, or self-consistent re-signing reject.

## Selected profile and trace set

Profile is `combat-infantry-reserve-I-v1`: one independent nonmotorized infantry element per side,
Normal selected Reserve-I continuation where required, voluntary combat, one Reaction opportunity,
and empty vehicle Breakdown cohorts. The composition contains 28 actual traces:

| Family | Contract | Traces | Terminal evidence |
| --- | --- | ---: | --- |
| Ordinary no attack | `combat-inherited-no-attack-v1` | 4 | Both owners at CP12/14 after six structural Combat steps |
| Direct Reaction closure | `combat-inherited-reaction-closure-v1` | 6 | Both owners under decline, unavailable and timeout causes |
| Active Reaction fallback | `combat-inherited-reaction-active-fallback-v1` | 4 | Both owners under unavailable and timeout after first reactor move |
| Reaction second-move completion | `combat-inherited-reaction-movement-completion-v1` | 2 | Both owners after completion, empty Breakdown stop and closure |
| Reserve cycle entry | `combat-inherited-reserve-cycle-v1` | 2 | Both owners at same-slot Reserve Release after empty Combat |
| Reserve release | `combat-inherited-reserve-release-v1` | 2 | Both owners after I→none release and ordinal-2 exception creation |
| Armed continuation | `combat-inherited-armed-continuation-v1` | 2 | Both owners with one supported next-cycle assault proof |
| Cycle control | `combat-inherited-cycle-control-v1` | 4 | Both owners across repeat and finish outcomes |
| Reserve second-Movement completion | `combat-inherited-reserve-movement-completion-v1` | 2 | Both owners after released-I Movement and exception expiry |

This set deliberately covers first-cycle no-attack, direct and active Reaction exits, released-Reserve
continuation, guarded repeat/finish and second-Movement completion. It does not claim every legal
future profile or two executed assaults.

## CON-002–004 reconciliation

Task003 hands exact prospective versions to Task004 and later runtime work:

| Contract owner | Frozen identities | Bound evidence |
| --- | --- | --- |
| `CMB-CON-002` | Setup7, World7, settlement receipts v1 | World settlement schema plus complete World values and obligation fragments in every root snapshot |
| `CMB-CON-003` | Created11, Snapshot12, Combat command/event v1 | Authority envelope, snapshot composition and inherited no-attack schemas |
| `CMB-CON-004` | Sequence5, cycle codec1, inherited cycle v1 | Sequence schema and all eight Reaction/Reserve/continuation/control successor schemas used here |

Exact version set is Content7, Setup7, World7, Rules10, Created11, Snapshot12, Sequence5 and cycle
codec1. `sourcePins` freezes 31 schema, fixture and oracle files. Contract-binding digests hash each
owner's ordered pinned schema records. `compositionDigest` hashes all 28 ordered `TraceWitness`
values, so trace deletion, insertion, reordering, duplication, or substitution changes the handoff.

Historical readers remain isolated: no registered C# type or writer changes in this checkpoint.
Old readers continue to reject these prospective contracts. Later runtime restore must derive each
boundary from accepted predecessor history and reject missing, forged, partial or unsupported
history; it must not promote these Python projections into registered production support by name.

## Capacity

Closed limits are 1,048,576 bytes per canonical value, depth 32, 512 items per array, 4,096 World
cohesion causes, 28 traces and 31 source pins. Current composed evidence is:

| Measure | Result |
| --- | ---: |
| Full `AuthorityComposition` bytes | 890,546 |
| Sum of root snapshot bytes | 870,258 |
| Largest root snapshot | 54,181 |
| Largest terminal | 20,900 |
| Maximum measured depth | 12 |
| Maximum array length | 21 |
| Maximum World cohesion causes | 2 |
| Maximum retained receipts | 27 |
| Maximum retained future obligations | 0 |

All obligation fields remain present even when selected traces contain no due future obligation.
Capacity success is evidence for this exact selected set, not permission to admit a larger profile.
Overflow or a 29th trace rejects instead of truncating or silently dropping history.

## Task004 handoff

`Task004Handoff` is Task003's complete input boundary, not Task004 completion. It carries exact
versions, three CON-002–004 bindings, ordered trace IDs, 11 exclusions, six runtime evidence lanes,
and six requirement ranges. Task004 must consume this value byte-exactly, freeze CON-005/006, and
expand each 12-item range into an explicit row-by-row 72-AC evidence map:

| Requirement range | Contract owners | Runtime / evidence direction |
| --- | --- | --- |
| `CMB-ID-AC-001-012` | CON-003, CON-005 | Identity, admission, authority and side-safe reference work in 006/009/016/018–022 |
| `CMB-PRO-AC-001-012` | CON-003, CON-005, CON-006 | Process, private decisions, restore, projection and Runner work in 008/010–013/020–024 |
| `CMB-STEP-AC-001-012` | CON-003, CON-005, CON-006 | Step authority, submission and Exercise work in 009–011/016/020–024 |
| `CMB-RES-AC-001-012` | CON-002, CON-003, CON-005, CON-006 | Resource, loss, settlement and outward evidence in 005–008/012–016/020/022–024 |
| `CMB-SET-AC-001-012` | CON-002, CON-003, CON-005, CON-006 | World settlement, restore, public and Runner evidence in 007–009/014–016/020–024 |
| `CYCLE-COMP-AC-001-012` | CON-004, CON-005, CON-006 | Release, movement, repeat/finish, public and Runner evidence in 008/017–024 |

Task025 remains final evidence owner for every range. A range-level handoff cannot satisfy its 12
individual criteria, and neither Task003 nor this packet closes checkpoint B.

Remaining evidence lanes are explicit: Task008 codec/restore; Tasks009–016 Combat authority;
Tasks017–019 cycle authority; Tasks020–021 public Core; Tasks022–024 Exercise/Runner; Task025
closeout and authentic-loop readback.

## Explicit exclusions

The handoff rejects or defers multiple-opportunity Reaction, positive vehicle Breakdown, two runtime
assaults, general resupply, real RBA movement, released-Reserve offensive Combat, motorized retreat,
gun/armor Combat, mandatory attacks, new phase-slot execution, and hosted intelligence. First two
are post-B profile extensions; remaining items require their named later work or separately admitted
profiles. Unsupported input must return an explicit rejection, never `no legal continuation`.

## Errors and verification

Oracle error family is `CMB-ACM-NNN`:

- `001` shape, canonical fragment, depth, byte or malformed input;
- `002` scalar type, range or identifier;
- `004` predecessor golden, source pin, contract binding or fixture mismatch;
- `006` byte-exact readback, digest or ordered-trace mismatch;
- `007` trace count or capacity failure; and
- `008` noncanonical JSON encoding.

Run:

```sh
python3 -B docs/specs/verify-combat-authority-composition-v1.py
```

Author verification passes 28 traces across nine families, 31 direct source pins, embedded
transitive source-pin validation, 225 readbacks, 203 mutations, six raw-byte negatives and eight
trace/capacity/source boundaries. Every family oracle remains
independently runnable; full repository gate remains `just check`.

No open question blocks Task003 contract closeout. Task004 is next and must preserve this handoff
without widening certified gameplay or activating runtime.
