# Private P01 ordinal-two Force Assignment v1

**Status:** bounded private reference. The [ordered schema](combat-p01-force-assignment-v1.schema.json),
[reference](verify-combat-p01-force-assignment-v1.py), [golden](fixtures/combat-p01-force-assignment-v1.json),
and [tests](test-combat-p01-force-assignment-v1.py) do not admit P01 or expose
native, hosted, or public actions. They stop Prepared at Close Assault before
debit, result, settlement, or surrender. The separate selected-assault v1
source and ledger remain immutable.

## Source and authority

The only opening predecessor is a fully replayed original CreationRequest,
eleven independent parent input ledgers, the R03 ledger, and a separate
seven-input selected-assault ledger. Its final receipt must be the accepted
RBA-step completion at the same slot's Force Assignment position, with exact
candidate, selection receipt, defender-owned decline receipt, prior version,
prefix, World, RNG and source bytes. A cancelled or truncated predecessor
cannot open this family. Every new readback and retry replays those same
arguments before trusting any new event. The caller independently authenticates
the ledgers, actor and clock; event-embedded actor/time are evidence, not a
browser credential or clock authority.

Land §11 permits the selected adjacent assault, and §13 permits defender RBA
after Barrage. Accepted CMB-POL-001 bounds this exercise to an explicit RBA
decline; CMB-POL-004 cancels instead of inventing a missing decision.
CMB-POL-009 excludes voluntary surrender, while automatic surrender remains
fail-closed at any later actual trigger. The prior ordinal-one
[prepared-round entry](combat-actual-round-entry-v1.md) informs the two-seal
mechanics but is not an ordinal-two admission authority.

The candidate is the exact adjacent armed 10-TOE pair selected by v1. The
reference rechecks pinned Content at use and derives each slot from the
original-side UnitKey, sole infantry component ID and current original World.
Attacker and defender slots remain in role order regardless of seal order.
No caller may alter candidate, component, target, allocation or current TOE.

## Transition boundary

The [schema](combat-p01-force-assignment-v1.schema.json) fixes ordered closed
keys, domains, prefixes and byte limits. Canonical ASCII JSON uses compact
UTF-8, exact key order, no duplicate keys, no alternate escaping, and explicit
nulls. Source embeds the complete selected source as canonical text while the
selected ledger and new ledger remain separate arguments. Domain-separated
SHA-256 receipts bind full input, predecessor and effect; sequence prefix folds
complete event bytes once. Source limit is 4 MiB because the already nested
selected source expands to roughly 2.57 MiB when embedded; this does not raise
the predecessor's separate caps.

1. System opens one Force Assignment round at an authenticated time no earlier
   than the accepted defender decline. A fixed 30,000 ms deadline and the
   opening version/prefix, source hash, candidate, selection and decline
   receipts bind its round ID. Two fixed full-10-TOE slots receive distinct
   IDs. The source remains private and unissuable.
2. Each original owner seals only its own exact allocation at an available
   trusted time in `[opening, deadline)`. Either order is accepted. The first
   seal remains Collecting; the second makes Prepared. No allocation changes
   World, RNG, CP, ammunition, TOE, relationships or attack history.
3. In Prepared, System completes Force Assignment and then derives an empty
   Anti-Armor certificate from pinned Content bytes, current World, candidate,
   both role-ordered full allocations, component IDs and the Force Assignment
   step receipt. It reaches Close Assault still Prepared and open. There is no
   positive Close Assault completion or result draw.
4. While Collecting, System cancels at deadline equality/later or on an
   unavailable trusted clock/controller. Zero or one accepted seal is retained
   as audit evidence. Cancellation has its own receipt and no fabricated second
   seal, payment or defender decision. Three no-attack structural completions
   reach the same slot's Reserve Release. A Prepared round cannot be cancelled
   by a late callback.

An identical canonical input retry returns the recorded receipt and no event.
Changed actor/context does not borrow a receipt. Stale version, prefix,
position, previous receipt, candidate, allocation, clock or participant fail
closed. Every cut keeps `admitted=false`, `publicActions=[]`, and unchanged
original World/RNG/resources. All event and control bytes are replay-derived;
caller-supplied source events are compared byte for byte.

## Evidence and remaining gates

The focused tests cover both original actor orientations and seal orders,
Prepared and cancelled paths, accepted one-seal audit, deadline/unavailable
causes, cuts, retry and negative mutations. The full pinned original 300-row
catalogue remains 44 candidate-positive, 256 candidate-absent, zero admitted.
This is not a complete source-legal action inventory or P01 support
certificate. Selected Close Assault result/settlement, automatic surrender at
a trigger, approved outward owner mapping, equal-authorized hidden-history
privacy, all-row cuts/restart/readback, physical negatives and resource caps
remain open. P02/P03, public Maproom orders and a playable Combat loop are
not enabled.

```sh
python3 -B docs/specs/test-combat-p01-force-assignment-v1.py
python3 -B docs/specs/verify-combat-p01-force-assignment-v1.py /Users/dsteele/.codex/worktrees/combat-p01-inventory/p01-evidence
```
