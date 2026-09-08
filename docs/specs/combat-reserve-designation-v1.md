# Creation-rooted Reserve designation and first opening v1

`CMB-TASK-003D2c.2d`, input `4c10ede`. [Schema](combat-reserve-designation-v1.schema.json),
[vectors](fixtures/combat-reserve-designation-v1.json), [oracle](verify-combat-reserve-designation-v1.py),
[combined plan](../design/combat-cycle-implementation-plan.md). Contract checkpoint only;
parent003D2c/003/004/B and runtime admission remain open.

## Accepted authority and closed profile

`initial(request, created, preamble, weather_events, stage_events)` consumes actual Created11,
four preamble, one Weather2 and all four [stage-entry](combat-stage-entry-v1.md) events through
unchanged predecessor readers. It derives state10, actual prefix and nine command receipts.
A cached state, invented prefix, standalone designation receipt or OpeningBase cannot establish
this authority. Setup7 no-obligation gates and creation identities retain predecessor checks.

Rules10/Content7/Setup7/World7/sequence5, turn1/stage1/first relative slot only. Retained order
resolves acting side: Axis for ActFirst, Commonwealth for ActLast. Reserve and Movement catalog
positions retain null activeSide and first-acting-side actorRole. Fleet's actor and Initiative holder
do not substitute for resolved owner. Both infantry remain at their creation locations with ammo10,
CP0, unchanged TOE and no future obligations. Weather, RNG cursor and every unrelated World field
remain byte-for-byte unchanged. All four Weather outcomes are admitted through accepted history.

## Designation2 and retained history

`DesignationInput` wraps command2 `designate-reserve-element` and trusted submitting actor.
Command binds creationBinding, creationEventHash, prior version, exact position and element ID.
Exactly one own independently placed infantry may change none→I while still at original location.
Only resolved owner can designate; System/opponent, unknown or cross-side member, repeat designation,
wrong source position, missing identity, stale/future version and prior Reserve status reject.
Empty selection means no designation event followed by completion; there is no empty designation event.

`reserve-element-designated`2 retains old gameTurn/operationStage/actingSide/elementId/priorStatus/
resultingStatus/position/sources fields and adds complete campaign/rules/configuration/creation,
version/prefix, accepted input and receipt binding. Position does not advance. Its `rd.` receipt is
SHA-256 of UTF-8 `sandtable.combat.reserve-designation-receipt.v2`, NUL and canonical event excluding
only receiptId. Event hash includes receipt; D1 prefix_event extends actual prior Chronicle prefix.
Append one command receipt and set own ReleaseHistory.designationReceiptId to this actual receipt.
All conversion/release/offensive/exception fields remain null; scope is turn1/stage1/first slot/owner.
Only reserveStatus changes in World; no CP/ammo/RNG change.

## Unchanged atomic completion2

Completion reuses frozen [D2c.1](combat-inherited-successors-v1.md) OpeningBase/Input/Event shapes,
field order, receipt domain and authority identity framing without modifying historical artifacts.
An internal derived OpeningBase binds replay-produced request, owner, Reserve position, prior
version/prefix, complete World/RNG and own members. Its historical profile string remains
`isolated-first-opening` to preserve frozen base hashing; this structural carrier is never accepted
as external authority by this reader. D2c.1's isolated validator fixes a single request seed and is
not a composed reader. New adapter validates actual predecessor history, then applies identical
completion projection/encoding semantics; it does not weaken or replace that isolated validator.

One `reserve-designation-completed`2 event atomically advances to exact first-slot Movement and
opens ordinal1. Empty selection yields state11 and ten receipts; designation I yields state12 and
eleven receipts. openedAuthorityVersion equals result version; openingPrefix equals actual prefix
before completion. cycle.actingSide resolves retained order; cycleId uses D1 authority framing.
Completion preserves entire World, Weather, RNG, order, designation history and prior receipt list.
Its `rc.` receipt/domain, sources18.11/5.2.reserve-designation and canonical bytes follow frozen
completion contract. No separate opening event/command, random draw, timeout fallback or remote I/O.

## Replay, cache and retry boundary

`replay(request,created,preamble,weather_events,stage_events,events)` accepts zero to two Reserve
records: optional designation, then completion. Every event is recomputed from accepted input and
compared as canonical bytes. Reordered, omitted-parent, duplicate, extra, cross-history or re-signed
invented events reject. `apply(...,events,inp)` authenticates actor and input shape before exact
accepted-input lookup. Exact retry returns original event/receipt with duplicate=true and current
fully replayed state, including designation retry after completion. Conflicting reuse rejects before
stale handling; fresh commands after completion reject. No caller-owned arguments mutate.

`read_state(data,...,events)` compares private ReserveState1 against full replay. This state retains
StageEntryState fields plus firstActingSide, own members, nullable cycle/cycleId/openingBaseHash/
completionReceiptId. Last four fields are null before completion. It is not Snapshot12 or public
restore. Full accepted records must accompany handoff; cached members/history/version are not proof.
Authenticated ingress and trusted published-head comparison remain production responsibilities.

Strict compact ASCII canonical JSON follows schema property order. Unknown/duplicate/missing/reordered
fields, booleans/fractions as integers, wrong types/nullability, BOM, whitespace, malformed UTF-8,
noncanonical escapes/numbers and unsupported tags reject. Limits:1MiB per record, depth32, arrays512,
exact 4/1/4 predecessor counts, zero–two Reserve records, one own member. This finite state10–12
profile cannot certify general campaign capacity. Private codes001 shape/limits,003 unsupported,
004 identity/precondition,005 actor,006 history/conflict/cache,008 noncanonical,009 source drift.

## Acceptance and next handoff

| Requirement | Evidence | Boundary |
| --- | --- | --- |
| CON-003/004 actual predecessor authority | Creation-rooted literal traces and source hashes | No synthetic prefix/receipt admission |
| Both actors and empty/I designation | Four Weather outcomes × both orders × both selections | Closed two-infantry initial World |
| Atomic first opening and frozen completion compatibility | Literal state11/12, ordinal1, exact position/history assertions and isolated-kernel parity | No D2c.1 byte changes |
| Strict replay, retry, preserved state | Every cut, leaf/raw mutations, cross-history and cache rejections | Author contract checks only |

`python3 docs/specs/verify-combat-reserve-designation-v1.py` passes16 creation-rooted traces,
40 replay/state cuts,2342 leaf mutations,733 raw rejections,1578 boundary/retry checks and four
byte-for-byte frozen-kernel parity cases. Fourteen source pins and152 literal artifact entries are
retained. Leaf sweeps cover empty/I and both actors at seed0; all four Weather seeds receive full
literal/golden, replay-cut, retry, malformed-byte and cross-history checks. Four altered source-pin,
expected-ordinal, artifact-digest and missing-case controls reject; Python AST/JSON syntax passes.
Normal verification never regenerates vectors. Lead also ran all20 predecessor/research oracles.

Author RED nonadvancing kernel failed expected designation state11; GREEN derives actual I history
and terminal state12/ordinal1 with original-event/current-state retry. Further parent-policy probes
exposed a lower-level creation validation exception escaping the new reader; normalizing explicitly
known predecessor rejection classes fixes34 malformed request/policy/parent probes. Final full suite
passes with unchanged retained canonical goldens. These are author checks, not another independent
review or evidence of C# successor-reader execution.

D2c.3 consumes full accepted predecessor/Reserve record lists and replay output: creation identities,
stateVersion/prefix, exact unmaterialized Movement position, firstActingSide, cycle authority/ID,
openingBaseHash/completionReceiptId, World/Weather/RNG, order, members/history and complete receipts.
Inherited Movement4/completion3, positive Reserve movement, Reaction/Breakdown, later slots/stages,
armed continuation and general World/Snapshot restoration remain D2c.3–4/004/B and runtime owners.
No new independent review, simulator, C# reader execution or runtime registration is claimed.
