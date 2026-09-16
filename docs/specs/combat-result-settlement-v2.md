# Combat result and settlement v2

**Status:** W01-CLOCK-RESULT accepted checkpoint. Contract evidence only; Task004, checkpoint B and
runtime activation remain incomplete. [Combined plan](../design/combat-cycle-implementation-plan.md)
records the independent mandatory-window opening disposition. This packet consumes accepted
[RoundState2](combat-sealed-round-v2.md) and preserves historical
[result/settlement v1](combat-result-settlement-v1.md) bytes and readers.

[Ordered field inventory](combat-result-settlement-v2.schema.json),
[retained fixture](fixtures/combat-result-settlement-v2.json) and
[oracle](verify-combat-result-settlement-v2.py) define exact private records and replay. Inventory
is an ordered type descriptor, not JSON Schema. No transport, host, schema registration, full
snapshot successor or new gameplay profile is allocated here.

## Scope and authenticated predecessor

The admitted singleton infantry, initial geometry, Cohesion0, full10 allocations, original
participants, Rules10 arithmetic, World7 types, Config1 budgets/fallback values and role-ordered RNG
remain unchanged. Authority starts from synthetic C3a accepted-decline evidence, not an actual
creation-to-Combat campaign history. Seed0 with explicit retained cursors exercises results;
these cursors are not claimed to follow campaign play.

Internal context is exactly `{base,predecessor,roundInputs,roundEvents,committed}`. Base is Base2;
committed is exact RoundState2. `verify_context` independently validates C3a through the accepted
v2 base reader, then replays every supplied round input/event and compares complete committed
bytes. Both seals, five step receipts, attack/target records, paid CP/ammunition, world and RNG
must match. Contexts with missing/extra fields, wrong versions, fabricated binding, changed
inputs or inconsistent committed state reject. Successful context authentication may be cached
by exact input bytes; the cache stores no caller-owned state.

The32 retained causal traces are eight literal gameplay branches times two acting sides times
two seal orders. Each opens assignment3000, admits first seal4000 and second seal3500, completes
Force Assignment and certified empty Anti-Armor, then commits Close Assault. Case-specific CP and
RNG cursor enter the authenticated synthetic base before the C3a replay. No active v2 state is
reshaped as v1, and no historical module is modified or monkeypatched.

| Literal branch | Unchanged gameplay evidence |
| --- | --- |
| ordinary | Ordinary losses and Contact |
| zero-retreat | Zero-loss paid retreat and victory RP |
| refusal-loss-dp | Refusal percentage, rounded loss and loss DP |
| zero-engaged | Zero-loss Engaged |
| defender-capture-guard | Captured defender, attacker custody and guard |
| defender-capture-escape | Captured defender, attacker custody and escape entitlement |
| attacker-capture-guard-cp-limit | Captured attacker, defender retreat/custody, CP10→11 and guard |
| attacker-capture-escape | Captured attacker, defender retreat/custody and escape entitlement |

Branch names identify the captured side, not the captor. Retained branches with both retreat and
custody have the same owner. No cross-owner retreat/custody branch is invented.

## Explicit clock and configuration binding

ResultCommand2, ResultState2 and ResultEvent2 all require these first fields:

| Field | Meaning |
| --- | --- |
| contractVersion | Exactly2 |
| roundClockConfigurationHash | Exact supplemental clock configuration hash in authenticated RoundState2 |
| resultClockPolicyId | Exactly `sandtable.combat.mandatory-window-clock.v2` |

ResultEvent2 `configurationHash` retains original Config1 identity. That configuration supplies
unchanged mandatory retreat/custody budgets and scripted fallback values. It is deliberately
separate from `roundClockConfigurationHash`, which authenticates assignment-clock provenance.
Result2 version and fixed result clock policy explicitly select independent mandatory-window
opening; Config1 is not silently relabelled as a new policy. `committedHash` is SHA-256 of exact
canonical RoundState2, including its explicit clock binding.

Future A2 public configuration references must identify both public clock policy IDs and approved
budgets from that profile's first frame. They must preserve accepted A1's existing literal lane,
and must not expose either private configuration hash or an authority state/prefix digest.
This packet allocates no outward observations or public receipt versions.

## Mandatory-window timing correction

Each required retreat or positive-custody window opens using its own independently trusted valid
instant and confidence, with checked Config1 fixed budget. Earlier private accepted times do not
gate opening or trigger fallback. Opening can therefore be earlier than a previously accepted
retreat timestamp when trusted confidence remains available. The host must supply that confidence
independently of private action history; host monitoring is outside this packet.

Existing Timing1 remains the window record: original policy/config identity, kind, budget, opening,
fixed exclusive deadline and local high-water. A new window's local high-water equals its opening.
Choices compare against that window's timing only. The retained `acceptedHighWater` field is
result-local audit evidence, seeded from the committed round's public opening floor and updated
monotonically on available accepted result inputs. It never gates admission or supplies a new
window's local high-water. Actual assignment seal times remain in RoundState2 audit evidence.

| Trigger | Deterministic behavior |
| --- | --- |
| Valid available required-window opening | Open at supplied instant with checked fixed deadline, regardless of prior audit maximum |
| Missing/unavailable opening time or unrepresentable deadline | Record existing scripted fallback immediately with null timing; do not fabricate an opening |
| Valid owner choice at/above current window opening and below deadline | Record exact selected disposition once |
| Available owner choice at/after deadline | Reject; System expiry can record scripted fallback |
| Missing/unavailable/below-window time | System-authored refusal or unguarded escape, never attack cancellation |
| Explicit controller unavailability | Existing scripted fallback |
| Expiry before deadline with valid clock | No-op |
| Stale timer or exact accepted-command retry | Existing no-op or original receipt recovery; no renewed deadline |

Context/actor/choice/primitive validation precedes fallback; foreign or malformed proposals cannot
manufacture settlement effects. Exact authenticated command retries recover original receipt before
clock or stale-version checks. Invalid primitive types still reject before retry recovery. New
structural work requires null time and available confidence except the actual required-window
opening. No committed attack can cancel, refund, reseed or reopen an already settled choice.

### Retained prior-time isolation regression

The same defender retreat choice accepted10001 versus11000 produces equal gameplay World7 facts.
A subsequent trusted custody opening10500 must open an identical same-owner window in both
histories. Choosing guard at10600 succeeds in both, even when one audit maximum remains11000.
The test also compares null/unavailable time, below-window regression, deadline equality and
fallback outcomes for both acting sides and both seal orders.

This proves same-owner prior-time isolation in the bounded corpus. It does not claim an opposing-
retreat leak or a complete A2 public-history proof. Private event prefixes, audit times and authority
receipts can differ; A2 owns authorized observations, revisions, action identities and public errors.

## Result, World7 and closure

Resolve all eight ordered purposes plus the conditional capture die in one event. Preserve rejection
sampling, consumed bytes, before/after RNG, both Morale coordinates and adjustments, differential0
basis, exact result facts, source/procedure binding and checked cursor overflow. Overflow rejects
without mutating committed authority; it is not controller fallback. There is no intermediate die
checkpoint, unused capture draw, second flag draw or changed table column after retreat CP.

World7 settlement arithmetic reuses immutable [003B](combat-world-settlement-v1.md) rules. Only real
commitment/result/settlement identities replace synthetic example IDs. Deterministic World7
construction may be cached using exact case bytes and cut; callers receive copies. No cached world
can substitute for authenticated causal replay.

Order remains result → required retreat disposition → joint losses → actual retreat/DP/RP →
positive custody disposition → relationships → round closed → one Close Assault completion at
Reserve Release. Zero retreat produces no retreat window; zero captives produce no custody window.
Refusal, guard transfer, escape, delayed replacement and later feeding obligations keep their exact
existing effects. Closure retains future obligations and original attack history/target use; it
executes neither Reserve release nor cycle repeat.

## Canonical records and identity

Every schema field is mandatory, with explicit null where allowed. Canonical UTF-8 JSON follows
schema order; reject unknown/missing/duplicate fields, unknown effect tags, floats or booleans in
integer positions, noncanonical escaping, whitespace, BOM, trailing newline and alternate ordering.
Limits: one MiB per value/event, depth32, arrays512 and32 new result events/receipts. Authority
versions are checked signed64; RNG cursors unsigned64. UTC timestamps are integers0 through
253402300799999. Base/round replay retains its separate16-event limit.

Private errors: `CMB-RES2-001` shape/size/decode;002 primitive bounds;003 tags/field combinations;
004 context/actor/policy binding;005 time;006 lifecycle/replay;007 unsupported continuation/capacity/
overflow;008 noncanonical bytes. No public error allocation. Raw event/state readback recomputes
causal effects and complete bytes. Locally recomputed hashes do not authenticate forged events.
`initial` and `transition` accept internally derived state. `read_event` authenticates context and
requires a previously replayed prior; `read_state` authenticates context and replays the full supplied
result prefix. A caller-supplied prior state alone is not recovery evidence.

`D(domain,value)` is SHA-256 of ASCII domain, zero byte and canonical JSON. Schema fixes v2 domains:

| Identity | Preimage |
| --- | --- |
| Result | `res.` + D(result, AssaultResult excluding resultId) |
| Settlement | `set.` + D(settlement, `{commitmentId,resultId}`) |
| Outer receipt | `cmb.` + D(receipt, ResultEvent2 excluding receiptId) |
| Window decision | Derived settlement ID followed by `.choice.retreat` or `.choice.custody` |

Event tags remain `combat-result-` plus closed effect kind, now explicitly enclosed by version2
and both clock bindings. Unchanged Sequence framing folds complete event bytes after receipt
calculation; no digest includes itself. Embedded World7 receipt/lot/guard/relation rules remain
unchanged. Historical readers reject new closed shapes, and successor readers reject v1 records.

## Verification and integration limits

Run `python3 -B docs/specs/verify-combat-result-settlement-v2.py`. Default execution requires exact
retained fixture bytes and all source hashes. Fixture serialization is deterministic two-space
JSON with final newline; float/integer substitutions, boolean/integer substitutions, duplicate
keys and CRLF changes reject. No default regeneration or missing-golden fallback exists.

Each of32 traces retains full C3a evidence, Base2, round inputs/events, committed bytes, result
inputs/events, final state and every state-cut hash. Literal gameplay assertions remain independent
of generated identities. Checks cover every state/event cut, exact retries, stale timers, bounds,
RNG overflow, wrong bindings, changed world/dice, locally rehashed events and the same-owner clock
matrix. These are deterministic contract checks, not actual process restart or production evidence.

A2 must consume ResultState2 explicitly. Historical result/snapshot readers and the Task00328-trace
creation-rooted handoff retain their exact meanings. Any corrected full snapshot needs its own
successor binding. Reserve/cycle composition, complete side projection, source-capacity certification,
host lifecycle and simulator activation remain subsequent packets.
