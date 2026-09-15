# Combat sealed-round v2: public opening clock floor

**Status:** W01-CLOCK-CONTRACT accepted contract checkpoint. Contract-only successor; Task004 and checkpoint B
remain incomplete. [Owner clock disposition](../design/combat-cycle-policy-reconciliation.md#clock-privacy-correction--owner-decision-2026-09-15)
replaces the private accepted-seal high-water gate for this version. Historical
[v1](combat-sealed-round-v1.md), its readers and retained bytes remain unchanged.
[Combined plan](../design/combat-cycle-implementation-plan.md) owns successor integration.

[Field inventory](combat-sealed-round-v2.schema.json), [retained vectors](fixtures/combat-sealed-round-v2.json)
and [executable oracle](verify-combat-sealed-round-v2.py) define this packet. The inventory is an
ordered type descriptor, not JSON Schema. No production registration, transport field number,
host clock monitor, public observation version or runtime activation is allocated here.

## Bounded predecessor and version binding

This packet continues the same synthetic C3a accepted-defender-decline Force Assignment boundary
as v1. Both acting sides and both seal orders are retained. The initial infantry/full-close-assault
allocation, geometry, finite budgets, CP bounds, World7, RNG, sequence, Rules10, original Config1,
Setup/Content and creation evidence keep their existing meanings. This is not a genuine campaign
creation-to-Combat trace or support for arbitrary new forces or histories.

`Base2` is exactly `{contractVersion,boundary,steps,clockConfiguration}`. `read_base` separately
replays the supplied historical selection predecessor using the unchanged v1 reader, comparing
Boundary and Control before admitting the supplement. The extracted `{contractVersion:1,boundary,
steps}` is authentic predecessor evidence only. Active v2 events, states and receipts are never
converted to v1 or supplied to a v1 transition. Direct `initial` and `transition` calls are internal
helpers for an authenticated base and internally derived prior; persisted data enters through
`read_base`, `read_event` and `read_state`.

`ClockConfiguration2` has ordered fields:

| Field | Exact meaning |
| --- | --- |
| contractVersion | 2 |
| parentConfigurationHash | Exact original Config1 hash, independently validated against admitted inputs |
| timingPolicyId | `sandtable.combat.public-opening-clock.v2` |
| decisionBudgetMilliseconds | Exact original force-assignment window budget, 30000 in retained profile |

Original Config1 still names `sandtable.combat.fixed-deadline.v1`; it remains predecessor evidence.
The supplemental configuration explicitly selects corrected clock authority. It does not rewrite
Config1 or silently claim Config1 selected v2. `clockConfigurationHash` binds this supplement in
Base2-derived state and every RoundCommand2. RoundEvent2 `configurationHash` is this new hash;
`predecessorConfigurationHash` records original Config1. All three are validated by causal replay.
Rules10 describes unchanged public gameplay/sequence artifacts; no Rules11 is allocated solely
for this round codec. A future audience-safe config reference must include the public policy ID
and budget, thereby distinguishing v1 and v2, without exposing private authority hashes.

## Clock correction and preserved ordering

`ClockTiming2` is exactly `{contractVersion,clockConfigurationHash,kind,
decisionBudgetMilliseconds,openedAtUnixMilliseconds,deadlineUnixMilliseconds,
openingFloorUnixMilliseconds}`. Version is 2; kind is `force-assignment`; floor equals opening;
deadline equals opening plus checked budget. Opening instant, floor and deadline are immutable.

A new opening requires an independently supplied valid trusted instant and available confidence,
a validated completed predecessor and a checked representable deadline. Earlier private RBA or
seal timestamps do not define a later opening floor. Thus a trusted opening1999 can follow the
retained RBA acceptance2001. This is an explicit successor change, not an assertion that host
wall time cannot regress. Host confidence must come from an independent clock policy; it must
not be inferred from a private opposing action's timestamp. Host monitoring remains deferred.

After context, actor, field and live-own-slot validation, a new valid seal uses this fixed gate:

| Trusted input | Result |
| --- | --- |
| `clockAvailable=false`, or admittedAt null, or time below opening | System-authored clock-unavailable cancellation |
| Available time at or above opening and below deadline | Seal accepted at exact supplied time |
| Available time equal to or above deadline | Seal rejected; a separate valid System expiry may cancel |

Each sealed slot retains its actual `sealedAt`; 4000 followed by3500 is valid after opening3000.
Neither seal modifies timing. Accepted timestamps remain private audit evidence, never a shared
regression gate. No per-side floor, positive clock event or renewed deadline exists.

Malformed/stale/foreign-owner proposals reject before clock handling and cannot manufacture a
cancellation. Exact command-and-owner retries recover their original receipt before status or
clock gates, including null/unavailable time, Prepared, committed and cancelled/closed recovery.
A changed consumed proposal rejects. Trusted input shape and primitive validation still precede
receipt recovery; malformed booleans or timestamps are not accepted as clock faults.

Current System expiry before deadline is a no-op; equality expires. Unavailable/absent/below-floor
time yields System clock-unavailable cancellation. Explicit current controller-unavailable yields
its existing deterministic controller-unavailable cancellation. Stale or already terminal System
callbacks are no-ops. Prepared always wins the expiry/unavailability race; subsequent structural
step/commit recovery takes null time with `clockAvailable=true`, as v1 requires. An explicit
admission-disabled flag prevents new opening, while exact retries, authenticated historical replay,
existing prepared commit and cancellation completion remain available.

## Privacy statement and limits

For an unsealed owner, two valid Collecting prefixes that differ only by an opposite seal have
equal own allocation/status, opening and deadline. For every supported equal trusted instant and
availability, their same valid own proposal has the same outcome. In particular, opening3000,
opposite seal4000 and own proposal3500 produces acceptance in either order for either acting side.
Null time, unavailable confidence, below-opening time and deadline equality satisfy the same rule.
Malformed or foreign proposals also receive identical rejection before the gate.

The oracle's `live_owner_witness` is a test declassifier of those facts, not a serialized public
contract. It excludes private seal count/time, authority version, prefix and raw receipts. Authority
receipts differ across histories; public receipt/revision projection remains Task004 work. Existing
common-terminal disclosure and Prepared-versus-Collecting ordering remain governed by
[DES-002](../design/combat-sealed-decision-protocol-v1.md). This packet proves the bounded admission
property and does not claim complete CON005 or all future host clock behavior.

## Canonical authority and lifecycle

Every schema field is mandatory, with explicit null where allowed. ASCII-compatible canonical
UTF-8 JSON uses schema property order, no whitespace/BOM/trailing newline, no duplicate or unknown
fields, no noncanonical escaping and no floating-point integers. Limits are one MiB per value,
depth32, arrays512, accepted event/receipt sequence16 and exactly two role-ordered slots after
opening. UTC values are integers0 through253402300799999; authority versions use checked signed64
increments. Boolean values never substitute for integers. Raw readers validate shape, primitives,
canonical bytes and replay semantics. Slots, allocations, costs and proof arrays retain their
specified role or causal order.

Private error families: `CMB-RND2-001` shape/size/decode;002 primitive bounds;003 version/tag/field
combinations;004 context/actor/candidate mismatch;005 time;006 lifecycle/replay mismatch;007
unsupported continuation/admission disabled;008 noncanonical bytes. No public error allocation.

RoundCommand2 kinds remain open-round, seal-choice, expire-round, controller-unavailable,
complete-step and commit-attack. Only seal carries slot/allocation; only open/step/commit carry
expectedPriorVersion; open alone has null roundId. Untrusted command identity carries the explicit
clock configuration binding. RoundInput carries independently trusted actor/time/confidence.
Effects remain round-opened, choice-sealed, round-cancelled, step-completed and attack-committed,
with distinct enclosing contractVersion2 and receipt domains. Unknown tags and versions reject.

Collecting becomes Prepared after both exact full10 own allocations; a cancellation keeps prior
sealed audit records but commits no attack. Prepared completes Force Assignment and certified empty Anti-Armor before
Close Assault commitment; cancelled completes all three remaining steps and closes. Commit spends attacker5
and defender3 ordinary CP, ammunition10 to0, records role-ordered attack/target use and retains RNG
before result. Existing eligibility/cost guards apply. No result, retreat, custody, reserve or cycle
continuation is admitted by this packet.

## Identity and recovery

`D(domain,value)` is SHA-256 of ASCII domain, one zero byte and canonical JSON. Schema fixes all v2
domains. Existing cycle ID and Sequence prefix framing retain exact predecessor meaning.

| Identity | Ordered preimage |
| --- | --- |
| Clock configuration hash | `sha256:` + D(configuration, ClockConfiguration2) |
| Base hash | `sha256:` + D(base, Base2) |
| Opportunity | `opp.` + D(opportunity, `{baseHash,cycleId,positionId,candidate,declineReceiptId}`) |
| Round | `rnd.` + D(round, `{baseHash,opportunityId,openingAuthorityVersion,openingHistoryPrefix,timing}`) |
| Slot | `slt.` + D(slot, `{roundId,role}`) |
| Commitment | `cmt.` + D(commitment, `{roundId,priorVersion,priorPrefix,allocations}`) |
| Receipt | `cmb.` + D(receipt, complete RoundEvent2 excluding final receiptId) |

Opening timing includes the fixed floor. Event priorPrefix precedes receipt derivation; unchanged
Sequence framing folds complete event bytes afterward. No digest includes itself. New version,
closed fields and distinct domains prevent accidental cross-version admission. Local rehashing of
a forged event cannot replace exact causal replay. Persisted states must exactly equal full replay;
recovery at every retained cut plus suffix yields the same final bytes.

## Evidence and integration boundary

Run `python3 -B docs/specs/verify-combat-sealed-round-v2.py`. Default execution requires the retained
fixture and exact source hashes; it never creates missing goldens. Ten traces retain both sides,
both committed seal orders (4000 then3500), empty/partial expiry and unavailable-time cancellation.
Clock vectors, malformed proposals, exact retries, immutable deadline, every state cut, replayed
suffixes, rehashed tampering, canonical raw rejection, size/depth limits and old-reader rejection
are checked separately. Fixture file bytes are compared against deterministic two-space-indented JSON with a final newline;
float/integer or boolean/integer substitutions, duplicate keys and formatting changes reject.
Retained authority canonical strings remain exact; no golden is regenerated in place.

Historical v1 files remain authoritative for their version. Old result-settlement and armed
snapshot readers accept v1 round state only. They must not consume v2 via private-state reshaping
or import mutation. A separately versioned result/settlement packet must bind RoundState2 and
preserve the independent clock correction; full snapshot integration may require its own successor.
Until those packets and side projection are accepted, no live Combat dispatch is enabled.
