# Combat sealed-round and commitment contracts

**Status:** CMB-TASK-003C3b, bounded authority contract at input `c126f16` (merged PR94).
[Combined plan](../design/combat-cycle-implementation-plan.md) retains parent003C3 and checkpoint B.
[Sealed protocol](../design/combat-sealed-decision-protocol-v1.md),
[step transitions](../design/combat-step-transitions-v1.md) and
[cost ordering](../design/combat-cost-resolution-order-v1.md) govern this packet.
[Field inventory](combat-sealed-round-v1.schema.json), [fixtures](fixtures/combat-sealed-round-v1.json)
and [oracle](verify-combat-sealed-round-v1.py) specify exact private bytes and checked transitions.

## Scope and trusted base

Continue the [C3a](combat-selection-steps-v1.md) accepted-decline cut at Force Assignment. Base is
exactly `{contractVersion:1,boundary:Boundary,steps:Control}`. Admission separately replays the
caller-supplied C3a predecessor inputs/events and compares both Boundary and Control, including
three step receipts, selected participants and accepted defender decline. A caller cannot supply
its own alleged history as authentication. The fixture pins the complete predecessor fixture hash.

The same bounded initial infantry, first-relative-slot/stage1/turn1/ordinal1 restrictions apply.
Ordinary CP may be nonzero; attacker E≤5 and defender E≤7. World7, RNG, prior prefix/version,
creation/Rules10/config/Setup/Content bindings and selected geometry all enter the frozen Base.
No different force, weather, expenditure history, prior target use or active relationship is
silently admitted. Full reachable-result certification remains C3c/009 before any live offer.

Base is a **typed fragment**, not a full pre-round Snapshot12. Its digest explicitly uses the
`base-fragment` domain. C3c/D2 must reconcile the full snapshot, inherited pre-Combat path, capacity
and complete prior history before B; this fragment digest is not a substitute for the design's
full snapshot digest. Later composition binds these retained bytes or versions a changed family;
it must not silently reinterpret this packet's goldens. The C3a fixture has synthetic trusted
boundary/Weather/Breakdown hashes; neither its trace nor this extension is a genuine campaign run.

## Closed canonical data

The schema file is a field-order/type descriptor, not JSON Schema. Every field is mandatory;
nullable fields contain explicit null. UTF-8 JSON has no whitespace, BOM, trailing newline,
unknown/duplicate fields, noncanonical escaping or floating-point integers. Private errors are
CMB-RND-001 shape/size/decode,002 primitive bounds,003 version/tag/field combinations,004 context or
actor/candidate mismatch,005 time,006 lifecycle/replay mismatch,007 unsupported continuation and
008 noncanonical bytes. Raw readback checks shape/types, then canonical bytes, then semantics.

Limits: one MiB per encoded value/event, depth32, arrays at most512 entries, at most16 new accepted
round events/receipts, and exactly two role-ordered slots after opening. Authority versions use
checked signed64-bit increments. Arrays of events, receipts, steps and slots are ordered, never
sorted by arrival or hidden identity. Roles are always attacker then defender for allocations,
costs and history derivation, regardless of seal arrival order.

`RoundState` carries base/opportunity/round identities, current version/prefix/step, status, opening
receipt/timing, two slots, full ordered step receipts, World7/RNG, attack history, target uses,
commitment/cancellation receipts, accepted-command ledger and segment-closed flag. Frozen C3a
Control remains the prior input; RoundState owns subsequent active position/disposition. A cancelled
round does not alter old selection/decline receipts or mint an earlier C3a cancellation.

`Allocation` is `full-close-assault` plus exact own UnitKey, own component ID and committedToe10.
Each slot retains role, owner, derived slot ID, that exact allocation, and nullable accepted receipt
and admission time. No partial allocation, edited seal or third slot exists.

These are private authority contracts.004 still owns public round/slot/decision/action IDs, visible
revisions, approved config references, projections and side-safe rejection mapping. The other
slot's private record remains unchanged by a seal, but that alone is not a public privacy proof.
No authority ID, hash, version, World or raw event is a player/model payload.

## Identity preimages

`D(domain,value)` is lowercase SHA-256 of ASCII domain, one zero byte, then canonical JSON value.
The inventory fixes domains. Raw digest strings below are lowercase hex, except base/cycle/prefix
hash values carry `sha256:`. Object key order is exactly the order shown:

| Value | Derivation |
| --- | --- |
| Base hash | `sha256:` + D(base domain, Base) |
| Cycle ID | C3a's SHA-256 of the D1 Authority identity preimage, unchanged |
| Opportunity ID | `opp.` + D(opportunity domain, `{baseHash,cycleId,positionId,candidate,declineReceiptId}`); position is Force Assignment and candidate is exact C3a selection |
| Round ID | `rnd.` + D(round domain, `{baseHash,opportunityId,openingAuthorityVersion,openingHistoryPrefix,timing}`) |
| Slot ID | `slt.` + D(slot domain, `{roundId,role}`) |
| Commitment ID | `cmt.` + D(commitment domain, `{roundId,priorVersion,priorPrefix,allocations}`) |
| Event receipt | `cmb.` + D(receipt domain, complete RoundEvent except final receiptId) |

Timing in the opening preimage has the initial high-water value. Later seals do not recreate any
identity. The event records the **prior** prefix; D1 framing advances the prefix using complete
event bytes after receipt calculation. No digest includes itself. Arrival order can change receipts,
commitment IDs and prefixes; it cannot change the same selected allocations, resource effects or
retained pre-result RNG. Cross-order full snapshot equality is not promised.

## Commands and lifecycle

RoundCommand has fixed fields `contractVersion,kind,segmentId,roundId,slotId,expectedPriorVersion,
allocation`. Open uses roundId null; all other kinds require a round ID. Only seal supplies slotId
and allocation. Only open/step/commit supply expectedPriorVersion. RoundInput adds independently
trusted `actor,admittedAt,clockAvailable`; actors come from authenticated admission, not user claims.

| Command | Preconditions and semantic effect |
| --- | --- |
| `open-round` | System, exact prior version, validated C3a FA cut, reliable opening time at least the accepted RBA high-water. Create opportunity, round, exactly two slots and one shared configured30s deadline. No resources/history/RNG change. |
| `seal-choice` | Authenticated slot owner, exact round/slot/allocation, unconsumed slot, strictly before deadline. Record one seal; first stays Collecting, second derives Prepared without another event. No private authority-version precondition can stale the other slot. |
| `expire-round` | System exact Collecting round; before deadline is a no-op, equality or later cancels once. Clock regression/loss takes unavailable cancellation. Stale round binding or Prepared/Committed/Cancelled status is a no-op. |
| `controller-unavailable` | Trusted system exact Collecting round; cancel without filling any missing slot. Stale/finished/prepared timers or unavailability cannot discard prepared choices. |
| `complete-step` | System exact prior version. Prepared: close FA then certified empty AA, preserving slots and all world/RNG facts. Cancelled: close remaining FA/AA/CA as no-attack, stopping at Reserve Release entry. |
| `commit-attack` | System, Prepared at CA with all five step receipts and exact replayed permitted suffix. Atomically record commitment, costs, directional history and target use. No RNG draw or settlement. |

Opening pins C1 `force-assignment` policy/config/timing. Missing/unreliable/overflowing opening time
rejects before a round exists; this fragment does not fabricate an opening deadline or implement a
pre-opening hosting fallback. Seals at deadline equality reject; a trusted timer then cancels.
For an otherwise valid seal, trusted clock regression/loss produces system-authored cancellation
with no sealed allocation. It retains the prior accepted high-water; other system cancellation
inputs retain the maximum recorded time without extending the fixed deadline. Invalid actor or
allocation cannot trigger a clock cancellation. Rejections/read-only calls mutate nothing.
Prepared continuations require no wall-clock value or live controller. Their internal command input
uses null admission time and the canonical clockAvailable true sentinel; it requests no clock read.

Exact canonical command hash plus the same authenticated actor returns the recorded receipt, even
after deadline/commitment, without a new event. Changed content cannot replace a consumed slot or
commitment. The ledger excludes fresh transport time from command identity but accepted event
bytes retain original trusted time. No-op timers have no receipt and may be retried.

## Event, cost and recovery proofs

RoundEvent retains campaign/rules/config/cycle/segment/round, prior and resulting versions, prior
prefix, trusted input, semantic author, effect and receipt. The inventory enumerates event tags.
`combat-round-step-completed` is deliberately distinct from C3a `combat-step-completed`; their
closed schemas must not share an ambiguous decoder tag. Every accepted event increments once.

Prepared suffix is exactly two valid seals in either order, one FA completion and one empty AA
completion. Step proof binds preceding step receipt and both role-ordered seal receipts. Cancelled
step proofs instead bind the round-cancellation receipt. No movement, spending, third seal or generic
position jump can fit the replayed suffix. A result or CA completion while committed rejects until
C3c supplies settlement/completed-round evidence; no extra segment-completed event advances again.

Commit costs are attacker CP+5, defender CP+3, and each Ammo10→0. It preserves all other World fields
and the complete RNG state. Cost evidence retains before/after balances and original unit keys.
Append one directional attacker/defender history entry with cycle/segment/target/turn/stage and
commitment, plus one separate target-use entry. History is not replaced by target use. Zero ammo
records the paid use; it cannot cancel the committed assault or permit another use. C3c/D2 owns
cumulative history and whole-snapshot composition; this initial-context slice admits empty prior
attack history only. No settlement, TOE loss, retreat, relationship or future obligation is applied.

Read_base requires an independently supplied predecessor. Replay/read_state start there, recompute
every input/effect/receipt/prefix and compare exact stored bytes; they do not authenticate a
self-consistent attacker-supplied transcript. Transition helpers assume an internally derived prior;
persisted state must enter through read_state, not directly through transition. Immutable return
values prove no partial in-memory effect on rejection. Durable Chronicle/state publication, lost
storage acknowledgment and actual host restart remain unimplemented and need separate tests.

## Verification and remaining gates

Four literal behavioral cases fix both seal orders and zero/one-seal expiry, with independent expected
resource/terminal counts. They contain23 canonical events and four final-state goldens. Canonical
regression goldens were recorded from the oracle after those assertions passed; they are not an
independent serializer implementation and normal verification never regenerates them.

`/opt/homebrew/bin/python3 -B docs/specs/verify-combat-sealed-round-v1.py` checks23 replay/state cuts,
276 event/state mutations and138 raw rejections, plus actor/binding/changed-payload rejection,
exact retries, just-before/equal/after deadlines, stale timers, clock regression/loss, unavailability,
Prepared/Committed timer immunity, missing/skipped/reordered steps, CP equality ceilings and unchanged
RNG. Three additional current-CP boundaries reconstruct C3a predecessor traces before applying costs.
C3a base/decline tampering, strict raw decoding and checked-version failure are included.

This adds bounded authority evidence toward PRO-AC001–009/011/012 and STEP prepared-path obligations;
full public revision/privacy AC010, full result/settlement recovery, complete Snapshot12 and genuine
pre-Combat history remain C3c/D2/004 and runtime gates. C3a's four artifact bytes remain unchanged.
No production C# parity, live simulation, durable recovery or independent-review verdict is claimed.
