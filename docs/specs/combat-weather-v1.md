# Creation-rooted Weather v1

`CMB-TASK-003D2c.2b`, input `3ca453a`. [Schema inventory](combat-weather-v1.schema.json),
[retained vectors](fixtures/combat-weather-v1.json), [oracle](verify-combat-weather-v1.py),
[combined plan](../design/combat-cycle-implementation-plan.md).
Consumes validated Created11 plus all four [opening preamble](combat-opening-preamble-v1.md)
events, resolves Weather2, and stops at **Organization entry**. This is prospective contract
evidence, not runtime C# execution, Snapshot12 admission, public projection or simulator activation.

## Scope, Rules and compatibility

The fixed C2 Rules10/Setup7/Content7/World7 context and sequence5 admit only turn1/stage1, after
predetermined Axis Initiative and either first-side declaration. `ResolveWeather`1 becomes2;
`weather-determined`1 becomes2, matching the [D2c.1 declaration](combat-inherited-successors-v1.md).
The nested Weather value remains version1 with its existing field meanings. The predecessor
packet and historical C# codecs/goldens remain unchanged. This freezes one previously declared-only
payload; thirteen inherited successors remain unfrozen after the four preamble successors.

The existing [Weather specification](weather-determination-v1.md) and
[accepted design/ruling](../design/weather-determination-v1.md) own gameplay semantics. Read the
retained C# Weather artifact bytes, removing only their file-ending LF, and require the exact
schema-listed hash and the same artifact/hash/sources in C2's Rules10 manifest. No table inferred
from selected test outcomes is authoritative. The selected game turn maps to Fall: ordered d66
11–35 Normal,36–54 Hot,55–61 Sandstorm,62–66 Rainstorm, considering only valid d6 digits.

Draw accepted tens then ones with the existing SHA256 counter stream. Every byte advances the
checked unsigned64 cursor; reject bytes252–255, otherwise die=`byte%6+1`. Foul outcomes alone draw
one additional accepted location die. Locations1–6 map respectively to AB,CD,DE,BC,BD,BCD. Normal
scope is `none`, Hot is `global`, foul is `listed-areas`; Normal/Hot have null location die and an
empty area list. Area E is retained: the existing Nile Delta subarea exclusion is explicitly
deferred in the Rules artifact, not silently inferred from area E.

The fixed Setup7 must contain the exact `no-immediate-weather-effect-subjects` policy and source.
All three immediate effect counts are explicitly zero: fuel/water subjects, restored wells and
damaged grounded aircraft. This is certified absence in this closed profile, not a default for
missing policy or positive subjects. World7, CP, ammo, TOE, lots, obligations and holder/order bytes
are preserved. Hot/foul outcomes are retained even when future Combat capability will reject them;
Weather is never forced to Normal. Determining side is the Initiative holder, including ActLast
when Commonwealth moves first. System actor is trusted command metadata, not remote authentication.

## Exact envelope and canonical bytes

The schema fixes every field/type/order/nullability. WeatherCommand2 binds kind, creation binding
and event hash, expected prior version5 and exact Weather position. WeatherEvent2 retains every
current semantic payload plus campaign/rules/configuration/creation identity, prior/result version,
pre-event prefix, accepted input, RNG algorithm/before/after cursor and receipt. Result is version6
at sequence5's exact same-turn/stage Organization position, with null active side.

Receipt is `wth.` plus lowercase SHA-256 of UTF-8 domain `sandtable.combat.weather-receipt.v1`, NUL,
and canonical event bytes with only receiptId omitted. Event hash includes receipt; D1 prefix
extends the accepted preamble prefix with final canonical event bytes. No self-hashing field.
WeatherState1 retains all prior receipts and appends Weather's command/event/receipt/actor/version
entry. It is a private projection, **not Snapshot12**, and adds pair-keyed operationStageWeather
to the preamble fields. It preserves the preamble's frozen nested order shape unchanged.

Canonical JSON uses compact ASCII escaping and schema property order. References and affected
areas are sorted; duplicate, missing, extra or reordered fields/values reject on replay/readback.
Boolean/fractional integers, unsupported tags, invalid strings, noncanonical whitespace/escapes,
BOMs and malformed encodings reject. Each record is at most1MiB, depth32 and arrays512. Inputs are
one creation, exactly four preamble records and zero or one Weather record: six bounded records
at most. This local capacity does not prove full campaign/Snapshot admission; D2c.4 owns that.

## Replay, rejection and next-family handoff

`initial(request, created, preamble)` requires exactly four events and calls the frozen preamble
reader. It derives Weather-entry state5, complete accepted prefix, holder/order, World, RNG and
receipts. No standalone cache, supplied prefix/version, substituted request or partial preamble
may establish authority. `replay(request, created, preamble, events)` admits zero or one Weather
event, recomputes the complete transition, and compares exact bytes. `read_state` compares a
supplied cache to that complete replay. Internally admissible history still requires authenticated
ingress and trusted published-head verification in production.

`apply` validates complete history and trusted System actor before retry lookup. Exact accepted
input returns original event/receipt and current state with duplicate=true, without another draw,
prefix extension or publication. Changed reuse, unsupported command/version, stale/future version,
wrong actor/position/creation identity, duplicate/extra Weather, tampered effects/dice/cursors,
incorrect Organization successor or cross-order/campaign/seed history rejects atomically.
Caller inputs are unchanged on success, retry and failure. There is no timer or async decision.

D2c.2c consumes request, Created11, the four preamble events and this accepted Weather event,
reconstructs state6 with this reader, and retains Weather/RNG/receipts while resolving explicit
stage-entry gates. A lone WeatherState cannot substitute for that chain. Later turns/stages,
Reserve, positive immediate effects, Movement/Combat and public error mapping remain outside scope.
Private errors:001 shape/type/limits,002 numeric domain/overflow,003 unsupported command/tag/profile,
004 creation/predecessor identity,005 actor,006 history/conflict/cache,008 canonical bytes,009 source drift.

## Verification

`python3 docs/specs/verify-combat-weather-v1.py` passes34 creation-rooted traces (17 seeds, both order
choices),68 replay/state cuts,11516 leaf mutations,921 raw rejections,1651 boundary/retry checks and
3960 Rules turn/d66 coordinates. Literal expected outcomes are separate from the artifact-driven
resolver; seed0/1/2/3 expectations also match retained C# Weather tests. Seeds81 and unsigned64 max
consume rejected bytes. All12 foul kind/location pairs and both initiative orders are represented.
Thirteen current source hashes and306 creation/preamble/input/event/state artifact entries are
retained. Re-signed invented Weather, duplicate/missing receipts, arrays and cross-history caches
reject. Isolated cursor31/32/63/max-minus-one probes cover block and overflow boundaries without
claiming those cursors are reachable at this creation-rooted cut.

The initial non-advancing implementation failed the seed1 Normal/ActLast-holder regression; the
implemented transition passes it, including exact retry. All18 predecessor/research oracles pass.
Normal oracle runs never regenerate fixture goldens. Source hashes, existing C# literal expectations
and Rules artifact binding are compatibility evidence; no new .NET test run or C# Weather2
serializer/projector parity is claimed. Full Snapshot composition remains a future owner.
