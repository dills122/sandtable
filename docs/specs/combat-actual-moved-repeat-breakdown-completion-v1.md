# Actual moved repeat Breakdown completion v1

**Status:** private reference contract in progress. Native, public and Runner activation are not
claimed. [R03 delivery packet](../work/plans/2026-10-16-actual-moved-repeat-breakdown.md) owns gates.

## Authenticated predecessor

The only predecessor is an authentic, fully replayed R02 terminal from the paid or armed R01 moved
repeat lineage. The reference must replay the original CreationRequest and all ten independent R02
input ledgers; the outer eleventh ledger contains the new Breakdown input. A supplied R02 Control,
Proof, fixture row, source hash or signed claim cannot stand in for that replay. All 300 R02 terminal
rows (36 paid, 264 armed) are required. Each has an actual ordinal-two MovementEndProof bound to
its own completion receipt, an unchanged ordinal-one proof, `phase=breakdown`, closed R02 fragment,
idle flow, null interrupt, and first-acting-side ordinal two. Nonterminal R02 cuts reject.

## Transition

One trusted `system` `complete-breakdown-segment` command moves the full Breakdown Determination
position to the adjacent Combat Position Determination position. The command binds the derived Base,
cycle, exact prior position/version, action identity and capability. Actor, context and action checks
precede exact accepted retry lookup; an exact retry returns its original receipt and unchanged bytes.
Fresh admission requires the open R03 fragment, available signed version, matching capability and
one unused event slot. Event receipt derives from the unsigned canonical event. Prefix derives from
the signed canonical event. The active ordinal-two cycle stays open while the R03 fragment closes.

R03 preserves World, RNG, resources, both Movement proofs, all prior events/receipts, original and
active cycles, member and release history, assessment, pending intent, targets, actual progress,
tracks, movement receipts and interrupted-flow history. It adds only its Base/source binding,
one System event, one receipt in both the inherited cumulative `Control.receipts` ledger and local
`Control.breakdownReceipts` ledger, updated version/prefix/position and local closure. An exact
retry preserves both receipt arrays byte for byte. No Breakdown
checks, lots, losses, BP reset, progress charge, third ordinal or subsequent Combat step occurs.

## Canonical and failure boundary

The companion schema owns closed Source, Base, Command, Input, Event, Control and Proof shapes, domain
separation, and provisional raw caps. Canonical JSON is ASCII, compact, field ordered and rejects
unknown or duplicate keys, malformed primitives, suffixes and over-depth structures. Validate own
Source framing before ancestral replay, then authenticate the full R02 predecessor before the
eleventh input, own event or readback claim. CMB-MRB-001 is shape/framing; 002 numeric; 003 unsupported
profile or command; 004 ancestor, ledger, actor or context; 005 action/capability; 006 event/order/
claim mismatch; 007 admission/capacity; 008 noncanonical bytes; 009 dependency delivery. Physical
source acquisition must fail closed on missing, changed, unreadable, null or drifting dependencies.

Raw limits remain provisional until field/carrier/depth/collection domination and measured physical
closure are recorded. A fitting sample fixture is not a capacity certificate. Reference RED/GREEN,
all cuts/retries, fault injection, retention checks, fresh review and exact-head CI are required
before native implementation or publication.

The R02 300-case completion catalogue and its 56-root moved-repeat fixture are pinned physical
dependencies. The catalogue must contain exactly 300 expected unique row keys, 36 paid and 264
armed, with every root ordinal represented. Pin-map deletion or substitution after manifest
capture fails closed before reading dependency bytes.

## Scope

No public Tasks 020–021, Exercise/Runner Tasks 022–024, later ordinal-two Combat/Reserve/Convoy,
broader profiles, durable host publication or all-72 AC closure is established by this contract.
