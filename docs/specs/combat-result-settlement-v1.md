# Combat result, settlement and closure contracts

Status: CMB-TASK-003C3c.1 complete with author checks, input `7bb2d11`. Governed by the accepted
[cost/result design](../design/combat-cost-resolution-order-v1.md),
[settlement design](../design/combat-settlement-disclosure-v1.md) and
[combined plan](../design/combat-cycle-implementation-plan.md). Parent C3c also requires snapshot
composition. [Schema](combat-result-settlement-v1.schema.json),
[fixtures](fixtures/combat-result-settlement-v1.json), [oracle](verify-combat-result-settlement-v1.py).

## Scope and input

Consume independently replayed [C3b commitment](combat-sealed-round-v1.md) at Close Assault, five step
receipts, two seals and paid CP/ammo. No cancel/refund after commitment. Preserve original unit keys,
attack history, target use, configuration, creation and cycle/segment/round identity.
The selected singleton infantry, initial geometry and Cohesion0 restrictions remain unchanged.
C3a boundary lineage remains an isolated trusted probe, not authentic pre-Combat history. Seed0
with explicit retained cursors exercises branches; no test claims those cursors follow campaign play.

## Result and settlement

Resolve all eight purpose-ordered dice plus conditional capture die in one event; rejection sampling
consumes bytes. Bind input/output RNG, per-die purposes/bytes/cursors, both Morale coordinates and
adjustments, basic differential0, final differential, source/procedure hashes and full result facts.
No intermediate die checkpoint, second draw for flags, unused capture draw or reseed. Checked overflow
is a fault with unchanged committed authority, not controller fallback.009 must certify admission.

World7 payloads and arithmetic reuse the frozen [003B packet](combat-world-settlement-v1.md).
The composition substitutes real commitment/result IDs for the payload oracle's synthetic example
IDs; all suffix receipt/lot/guard/relationship rules stay unchanged. Derive result and settlement
identities before World construction. Current World is separate from immutable pre-loss evidence.

Order: result → required retreat window/disposition → joint losses → actual retreat/DP/RP → positive
custody window/disposition → relationships → round closed → one CA completion at Reserve Release.
No retreat window if not required; no custody window/event for zero captives. No unrelated action or
RNG draw during settlement. Retain future obligations on closure; do not execute release or repeat.

A live choice has one owner and a fixed configured30s deadline. Valid owner choices strictly before
deadline are accepted; equality rejects and system expiration defaults. Clock loss/regression or
controller unavailability records scripted refusal/unguarded escape, never cancellation. If reliable
time cannot open a required window, record fallback directly with explicit reason and null timing;
never fabricate an opening time. Already recorded choices are immutable. Stale timers are no-ops.

Command identity excludes fresh transport time but includes canonical command and authenticated actor.
Exact retries return stored receipt before stale-version checks. Changed reuse rejects. New authority
increments/prefix changes happen only for accepted events; system no-ops/rejections preserve all bytes.

## Canonical/readback contract

Fixed mandatory field order in schema; no missing/extra/duplicate fields, floats, unknown tags,
alternate bytes, BOM/newline, nullable omission or opaque dictionaries. One MiB/value, depth32,
arrays512,32 new events; counters signed64, RNG unsigned64. State readback independently replays prior
C3a/b and every result/settlement input; compare complete canonical event and state bytes. Transition
helpers accept internally derived state only. Stored self-consistent data is not authentication.

Private error codes CMB-RES001 shape/size,002 bounds,003 tags/combinations,004 context/actor,
005 timing,006 lifecycle/replay,007 unsupported continuation/overflow,008 noncanonical. No public
error mapping, side-visible ID/revision or projection contract is added;004 owns those boundaries.

## Identity and event framing

Use D(domain,value)=SHA256(ASCII domain, zero byte, canonical JSON). Result ID is `res.` plus D
of AssaultResult excluding resultId; commitment and both RNG states are inside that preimage.
Settlement ID is `set.` plus D of `{commitmentId,resultId}`. Outer event receipt is `cmb.` plus D
of ResultEvent excluding receiptId; eventType is `combat-result-` plus the closed effect kind.
These event tags are distinct from C3a/b. D1 prefix framing consumes the complete event after its
receipt is added. Embedded World payload receipts use the B suffix IDs and are not extra events;
event replay binds each payload to its outer receipt and effects. No self-referential digest exists.

ResultState retains the committed-state digest, version/prefix, status, result/settlement identity,
current window/high-water, World/RNG and closure/command receipts. Current status is one of
committed, resolved, waiting-retreat, disposition, losses, retreat, waiting-custody, custody,
relationships, round-closed, closed; exact receipt prefixes determine each state. The committed input
retains original step receipts/history/use; C3c.2 composes these with subsequent state into Snapshot12.
Round closure proves all immediate settlement payload receipts; CA completion binds that closure and
the preceding AA receipt, then reaches the single D1 Reserve Release position.

## Verification and limits

Eight literal outcome cases pass, with mirrored original-side roles and reversed seal order:
ordinary losses, zero-loss retreat, refusal-DP, zero-loss Engaged, both capture roles with guard or
escape, and CP10→11 retreat.68 replay cuts,728 event/state mutations,340 raw rejections and96 timing
checks pass, including duplicate receipts, stale timers, missing/reversed events and RNG overflow.
Golden bytes were recorded after literal assertions passed and are regression vectors, not a second
serializer implementation. Source hashes pin imported B/C3b/RNG code and fixture. Normal verification
never writes goldens. Per-die resequencing, changed cursors and altered raw result proofs reject.

Header/history composition remains C3c.2; cumulative release/history/first opening remains D2, public
privacy remains004 and runtime capability certification remains009. The selected snapshot must prove
capacity before live offers; these small first-round cases do not prove arbitrary4096-cause histories
fit an envelope. No production or actual process-restart proof, new review round, or policy change.
