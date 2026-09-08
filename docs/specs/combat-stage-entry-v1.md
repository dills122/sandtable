# Creation-rooted stage entry v1

`CMB-TASK-003D2c.2c`, input `78be145`. [Schema](combat-stage-entry-v1.schema.json),
[retained vectors](fixtures/combat-stage-entry-v1.json), [oracle](verify-combat-stage-entry-v1.py),
[combined plan](../design/combat-cycle-implementation-plan.md).
Consumes complete [Weather2](combat-weather-v1.md) provenance and resolves four no-obligation
stage-entry successors to **Reserve Designation entry**. Prospective contract evidence only;
C# implementation, Snapshot12 admission, public/Exercise projection and simulator activation remain gated.

## Scope and compatibility

Fixed C2 Rules10/Setup7/Content7/World7, turn1/stage1, sequence5. The four commands1 and events1
become2 as inventoried here and declared for events in [D2c.1](combat-inherited-successors-v1.md).
Organization advances state6→7, Naval Convoy Arrival7→8, Fleet Assignment8→9, Fleet Repair9→10.
Each exact predecessor/successor is from sequence5. All four commands require trusted System actor,
even while the fleet positions have Commonwealth actorRole/activeSide. No user side action is implied.

Setup7 must equal the schema's full StageEntryPolicy1: all four gates `explicit-none`, turn1/stage1,
exact version/source. Null, omitted, `has-obligations`, unknown or mixed policies reject. Empty World
lists never establish this permission. There is no positive Organization, arrival, assignment or repair
behavior. References retain the existing lab policy plus land-rule5.2 phase locator; they do not
claim the source game universally skips these phases.

All outcomes of accepted Weather2, including Hot/Sandstorm/Rainstorm, carry through unchanged.
World7, resources, obligations, Initiative holder, frozen nested order1, Weather1 and RNG bytes
are preserved. No die is drawn. Fleet Repair emits the catalog's **unmaterialized**
`first-acting-side` Reserve position with null activeSide. The retained turn/stage order determines
who will designate Reserve: Axis for ActFirst, Commonwealth for ActLast. Do not copy the fleet's
Commonwealth activeSide or force the Initiative holder into Reserve's position.

This freezes four more declared-only inherited payloads; nine remain after Weather/preamble and
previous Created/Reserve-completion freezes. Existing specs, codecs and canonical fixtures stay
unchanged. Private StageEntryState1 has WeatherState1's exact shape, not a new Snapshot12 arm.

## Canonical envelope and authority

Schema fixes field names, types, order and nullability. Commands bind creation identity, expected
version and predecessor position. Events retain current gameTurn/operationStage/from-position,
successor and source fields, plus complete campaign/rules/configuration/creation identity,
prior/result version, priorPrefix, accepted input and receipt. Event kind and phase cannot be mixed.

Receipt is `ste.` plus lowercase SHA-256 of UTF-8 `sandtable.combat.stage-entry-receipt.v1`, NUL,
and canonical event with only receiptId omitted. Event hash includes receipt. Extend actual prior
prefix using D1's prefix_event; append exactly one commandHash/eventHash/receiptId/actor/version
receipt. Five accepted predecessor receipts become nine at Reserve entry. No self-hashing field.

Canonical compact ASCII JSON uses schema property order, sorted references and inherited collection
ordering. Duplicate/missing/extra properties, reordered bytes, invalid encodings, BOMs, whitespace,
noncanonical numbers/escapes, boolean integers and unsupported tags reject. Limits:1MiB per record,
depth32, arrays512; one Created11, exactly four preamble records, exactly one Weather event, zero to
four ordered stage-entry events. Ten records maximum does not prove full campaign capacity.

`initial(request,created,preamble,weatherEvents)` requires complete accepted predecessor replay via
the frozen Weather reader; state6/Organization is derived, never supplied as a cache. `replay` accepts
only ordered prefixes of the four events and recomputes canonical events. `read_state` compares its
cache against full replay. Complete internally valid history still needs authenticated ingress and
trusted published-head verification in production; hashes alone are not authentication.

`apply` replays history and authorizes actor/tag/version before receipt lookup. Exact accepted input
returns the original event and receipt with duplicate=true and the **current** replayed state, even
after later stage-entry events. Changed reuse, stale/future version, wrong phase/identity/actor,
missing/reordered/duplicate events or a fifth event rejects without mutation. No command runs after
Reserve entry except an exact retry; designation belongs to the next packet. A re-signed invented
event or altered cache cannot replace source replay. Private errors:001 shape/type/limits,
003 unsupported tag/policy,004 predecessor/identity,005 actor,006 history/conflict/cache,
008 canonical bytes,009 source drift.

## Verification and next handoff

`python3 docs/specs/verify-combat-stage-entry-v1.py` passes12 creation-rooted traces (six seeds ×
both order choices),60 replay/state cuts,11052 leaf mutations,1299 raw rejections and3552 boundary/
retry checks. Seeds0/1/2/3 cover all four Weather kinds;81 and unsigned64 max preserve rejected RNG
bytes. Five literal sequence positions and four source groups agree with current
[StageEntryEvents](../../src/Cna.Core/Campaigns/StageEntryEvents.cs); the ActLast/null-active-side
handoff matches [Fleet Repair tests](../../tests/Cna.Core.Tests/Campaigns/StageEntryFleetRepairTests.cs).
Twelve source hashes and228 creation/preamble/Weather/input/event/state artifact entries are retained.
All19 predecessor/research oracles also pass. Source comparisons establish a drift boundary, not
C# successor parity; no new .NET run is claimed. Normal oracle runs never regenerate fixtures.

The initial non-advancing kernel failed the ActLast chain regression; the implemented chain passes,
including original retry after all four transitions. The full negative suite then exposed a lower-level
creation-policy error before the stage-entry guard. Moving the exact policy check before predecessor
replay fixes all four gate regressions; the complete suite passes with unchanged canonical goldens.

D2c.2d consumes request, Created11, four preamble events, Weather2 and all four accepted stage-entry
events through this reader. It derives state10/version/prefix, nine receipts, first-side order,
Weather/RNG and World. No standalone StageEntryState or synthetic Reserve prefix can establish
that authority. Reserve designation/completion and atomic first opening remain2d; Movement/Reaction/
Breakdown continuation and full World/Snapshot reconciliation remainD2c.3–4/004/B. Parent003 stays open.
