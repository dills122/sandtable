# Inherited ordinary Movement v4

Status: CMB-TASK-003D2c.3a contract checkpoint; no runtime admission. Governing
[combined plan](../design/combat-cycle-implementation-plan.md),
[preparation](../design/combat-inherited-movement-preparation.md),
[schema](combat-inherited-movement-v1.schema.json), [oracle](verify-combat-inherited-movement-v1.py)
and [retained vectors](fixtures/combat-inherited-movement-v1.json).
Parent .3, .4, 003, 004 and checkpoint B remain open.

## Authority and closed scope

`initial(request,created,preamble,weather_events,stage_events,reserve_events)` replays actual
[Reserve/opening reader](verify-combat-reserve-designation-v1.py), including Created11, four
preamble records, Weather2, four stage records and completed Reserve suffix. No caller base or
cached World substitutes for records. Require actual completed ordinal1, turn1/stage1/first-side
opening, NORMAL Weather, ordinary NONE infantry. Both resolved owners are supported. Initial
state retains all21 ReserveState fields and adds ordered tracks, actual progress references and
nullable Breakdown flow. No Movement has occurred when flow is null.

`replay` adds a final `events` argument; `apply` adds `events,inp`; `read_state` adds leading
canonical `data` and final `events`. These APIs reconstruct history before using any cache/input.
Input binds exact original UnitKey, actual creation event, cycle, expected head and position,
actor and current origin/destination. Retry requires identical canonical accepted input at its
original version and correct actor. After later moves it returns current replayed state and old
accepted event, without another version, receipt, prefix or effect. Conflicting reuse rejects.
New public failures normalize known predecessor failures as `CMB-IMV-NNN`.
Private input is `move-element` commandVersion2 in `InheritedInput`, with explicit authenticated
actor; eventVersion4 is independent. Version2 succeeds the inherited current commandVersion1;
this packet freezes its additional creation/cycle binding fields. It does not register a runtime command.

Scalar types reuse predecessor contracts: signed32 `int`, signed64 `long` (booleans rejected),
`id` matches `[A-Za-z0-9][A-Za-z0-9._:-]{0,127}`, `hash` is `sha256:` plus64 lowercase hex,
`rawHash` is64 lowercase hex, `side` is axis/commonwealth, `actor` also admits system structurally
but Movement authorizes only resolved owner. Cp retains signed64 numerator/signed32 denominator;
accepted movement requires nonnegative integer expenditure with denominator1. Public codes:001
shape/type/size,003 unsupported family/profile,004 provenance/source binding,005 legality/actor,
006 stale/conflicting history,007 capacity,008 noncanonical bytes,009 source/fixture evidence.
Cost kernel may report002 numeric bounds; predecessor failures normalize to004.
At most32 records are structurally supported; profile cost rejects eighth move at CP16. Ordered
location lists contain2–33 stable IDs, preserve chronological revisits and never sort/deduplicate.

Positive profile uses frozen Content7 two independently represented nonmotorized infantry units,
CP0/ammo10 at actual opening, no relationships, guards, broken lots or obligations. Each move
requires featureless Clear adjacency, unoccupied destination, own surviving infantry and integer
CP ledger. Any opposing combat representation adjacent to destination requires Reaction3 and
rejects before mutation, even if hypothetical reaction eligibility would be empty. No unsupported
family is interpreted as no-effect, no-continuation or finish. Vehicle admission/Breakdown2,
positive Reserve/released movement, nonnormal Weather, later cycles/slots/stages and general
World7/Snapshot12 restore remain separate gates.

## Exact event and nested mapping

Schema `objects` strings define exact property names, order, types and nullability. This is a
closed contract inventory using predecessor scalar/nested types, not generic JSON Schema.
Canonical UTF-8 JSON has no whitespace, duplicate/unknown/missing properties, reordered fields,
noncanonical numbers, invalid UTF-8 or trailing data. Arrays follow inherited canonical ordering
except ordered movement routes, receipts and progress chronology. Route revisits retain order.
`null` means only JSON null; `empty` means only []; neither admits unsupported positive payloads.

All26 legacy Move3 top-level fields remain separately present in legacy serializer order:

| Legacy fields | Move4 derivation |
| --- | --- |
| `contractVersion`, `eventType` | Exactly4 / `element-moved`; historical3 unchanged. |
| `campaignId`, `rulesetHash` | Actual replayed creation/opening identity. |
| `stateVersion`, `priorStateVersion` | Checked prior +1 and current replayed version. |
| `fromPositionId`, `gameTurn`, `operationStage`, `actingSide` | Current symbolic sequence5 position and cycle-resolved actor. |
| `elementId`, `representationId`, `originLocationId`, `destinationLocationId` | Exact original-unit/independent-representation binding and validated input. |
| `mobilityId`, `mobilitySources` | `land.mobility.non-motorized`; map-a source8.37. |
| `cost` | Exact legacy nested order: terrain ID, amount2/1, source8.37, null route adjustment, empty crossed hexside costs, total2/1. |
| `capabilityPointsExpendedBefore`, `capabilityPointsExpendedAfter` | Cumulative integer ledger; no reset/refund. |
| `cohesionBefore`, `cohesionAfter` | Immediate incremental excess-CPA DP under D2a ordinary150% policy. |
| `movementEndedAfter` | Null: supported destination has no enemy control. Completion proof remains absent. |
| `sequencePosition` | Unchanged catalog5; `activeSide=null`. Cycle resolves owner; no sequence4 materialization. |
| `openedReactionWindow`, `breakdownAccounting` | Null / empty only after supported no-window/no-vehicle checks. |
| `breakdownFlowAfter` | `{kind:"moving",route:MovingRoute}` even without vehicle cohorts; never invented idle. |

New fields bind `configurationHash`, `creationBinding`, `creationEventHash`, `cycleId`,
`openingBaseHash`, `completionReceiptId`, `priorPrefix`, accepted `input` and `receiptId`.
Receipt is `imv.` + lowercase SHA256(domain UTF8 + NUL + canonical unsigned event), domain
`sandtable.combat.inherited-movement-receipt.v4`. Chronicle prefix uses existing sequence5
`prefix_event(priorPrefix,canonicalEvent)`. Replay derives full event and compares exact bytes;
re-signing changed effects or transplanting self-consistent history cannot establish authority.

MovingRoute retains legacy order: routeId, firstMoveStateVersion, elementId, representationId,
owner, originLocationId, currentLocationId, cohortIds. Route ID is `sha256:` + SHA256 of canonical
JSON ordered domain,campaignId,rulesetHash,firstMoveStateVersion,elementId,representationId,owner,
originLocationId,cohortIds. Domain is `sandtable.breakdown.route.v1`; cohortIds is empty here.
Subsequent moves preserve route ID/initial origin/version and change current location only.

## Atomic cost and preservation

Reuse only D2a pure cost and terrain kernels, never its synthetic base, transition or exhausted
ammunition profile. Ordinary CPA10 ceiling15, Clear2 cost, with DP
`max(0,afterCP-10)-max(0,beforeCP-10)`. This intentionally supersedes legacy Move3's CPA10
stop/equal-Cohesion restrictions for this successor; historical contracts remain unchanged.
At CP10→12→14, DP2 then DP2; moving flow continues. Eighth2CP move would reach16 and rejects.
Each accepted move atomically updates element and independent representation locations, CP,
Cohesion, own `members[].spentCp`, ordered track and moving route. Each DP appends causal
World record with current receipt, cumulative ordinal, before/after and points. Append one
command receipt and `{eventType,receiptId,eventHash}` actual progress reference; increment head
and prefix once. Retain ammo10, TOE, RNG, Weather, order, initiative, opening/cycle authority,
member history, unrelated units/resources and all preceding receipts/events.

## Evidence and handoff

| Requirement | Verification |
| --- | --- |
| IMV-001 actual creation provenance | Both-owner creation→opening→seven moves; predecessor source/golden bindings; missing/forged records and cache rejection. |
| IMV-002 legal effects | Literal locations, CP2…14, Cohesion0…-4, two DP causes; ordinary ceiling, occupancy, origin, unit, actor, Weather and unsupported-family negatives. |
| IMV-003 canonical authority | Every event read/replay cut, exact retry after later move, changed retry/stale/future input; representative nested mutations and re-signed effects, raw codec negatives. |
| IMV-004 preservation | Full expected World/member comparison, route revisits, retained receipts/history and actual progress hashes. |

Safe actual paths for both owners: assault-west/east→own-rear→own-supply→own-rear→own-supply→
own-rear→own-supply→own-rear. Opponent remains at opposite assault location; graph distances
from rear/supply are2/3. Returning rear→own assault origin requires Reaction and rejects.
Source pins in fixture bind legacy serializers, route identity, factory trigger and Content7
facts plus actual predecessor contracts. These are source evidence, not runtime test claims.

Next bounded family: actual route lifecycle followed by Movement completion3/end proof. Legacy
moving route requires owner `element-movement-stopped`2 to create phasing-stop, then System
`breakdown-stop-resolved`2 to produce idle even with empty cohorts, then
`movement-segment-completed`3. Future adapters must consume actual move history/progress rather
than fabricate idle flow or treat per-move null as completion. This minimal empty-cohort lifecycle
does not grant positive vehicle Breakdown admission; completion ends at Breakdown-determination,
with subsequent Breakdown-segment completion still separate.
Reaction3, Breakdown2, positive Reserve movement and armed continuation need separate admission.
No .3 aggregate completion or .4 composition/capacity claim follows from this packet.

Author verification: `python3 -B docs/specs/verify-combat-inherited-movement-v1.py` passes2 actual
traces/14 moves,16 replay cuts,14 exact retries,384 event/cache mutations,66 raw rejections,
81 boundary/admission checks and18 source pins. Four temporary-fixture negative controls reject
missing owner, duplicate owner, changed source pin and changed golden. Literal destination/CP/
version/prefix expectation failed against no-op kernel before implementation and passes after it.
Normal verification never writes fixtures. No .NET, runtime or independent-review run is claimed.
