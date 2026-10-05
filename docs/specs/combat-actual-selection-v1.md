# Actual Combat selection v1

Private executable contract, Task019F0 / session2 S3. Governing research:
[actual-selection bridge](../research/combat-actual-selection-bridge-feasibility.md),
accepted S2 head80cc969 and merged PR159/base96596dde. The coordinator's
2026-10-05 dependency disposition at b86c062 freezes S1a's16 admitted dependency
bytes and defers the20-file historical maintenance closure. This specification does
not waive any historical failure. The [ordered inventory](combat-actual-selection-v1.schema.json),
[literal fixture](fixtures/combat-actual-selection-v1.json) and
[oracle](verify-combat-actual-selection-v1.py) define the executable acceptance.

## Authority and scope

Admit only the complete original two seed1 Normal/NONE first-owner opening histories
already accepted by019E0/E1: Axis normal-act-first-none and Commonwealth
normal-act-last-none. Retain original Request, Created11, four preamble, one Weather,
four stage-entry, one Reserve completion, Movement11→12 and Breakdown12→13 records.
All source strings and original receipt IDs remain exact. The original twelve entry
receipts, actual catalog activeSide=null, cycle opening version11/prefix/ordinal1,
current version13/prefix, candidate, World, seed1/cursor2 and MovementEndProof persist.
Content/resource-origin labels remain synthetic where the original history labels them so.

The seven-record positive path opens selection13→14, accepts exact owner/candidate14→15,
completes Position15→16 and Barrage16→17, opens RBA17→18, accepts the actual defender's
decline18→19 and completes RBA19→20. Endpoint is Force Assignment, stepIndex3,
three ordered step receipts, closed=false. Force Assignment completion rejects007;
there is no allocation, seal, commitment, resource charge, RNG draw or result.
World/RNG remain byte-identical to accepted entry at every cut.

Seven fallback variants per owner reach the same cycle's single Reserve Release successor
with six ordered step receipts and closed=true: unavailable initial opening, player
finish-without-attack, selection expiry, unavailable selection controller, unavailable
RBA controller before opening, RBA expiry, and unavailable open RBA controller. They
produce no accepted defender decline, attack, resource effect or progress. Unavailable
initial opening requires explicit close-empty-selection before traversal. Cancelled
selected intent retains its selection receipt and gains a cancellation receipt.

C3a, Round2 and Result2 remain unchanged and incompatible. Their synthetic branches are
not promoted; all32 Result2 branches are not claimed reachable. Actual round/result,
repeat/later-II/consumed, full Snapshot, outward/observation/intelligence/public action,
transport, host, Archives restart, Maproom and Runner admission remain separate gates.

## Trusted input precondition and API

Replay is consistency relative to an **independently supplied trusted ledger**. Authority
caller must authenticate actor and clock confidence before supplying each Input, and
retain that ledger independently for replay. Embedded event.actor/admittedAt is evidence
to compare, never an authenticator. A coherent alternative event chain paired with a
replacement alleged trusted ledger crosses this precondition; receipts cannot detect
that ingress violation. No production seat, clock or retained-store authenticator is
implemented or claimed.

Oracle APIs (the same separation is binding for the future native adapter):

- `replay(source, trusted_inputs)` owns a canonical copy of the separate ledger and
  replays every retained event against its corresponding input; count/order must match.
- `apply(source, trusted_history_inputs, trusted_current_input)` fully replays retained
  history, owns the current input and returns owned Control, event bytes and receipt ID.
- `read_source(data, trusted_inputs)`, `read_proof(data, source, trusted_inputs)` and
  `read_control(data, source, trusted_inputs)` require complete source plus ledger.
  They never reconstruct trusted inputs from events or accept a digest-only cache.
- `boundary(source)` derives complete actual-entry provenance after admission. It is
  a projection, not a standalone boundary authenticator or accepted apply API.
- `parse`, `raw`, `initial`, `transition`, `read_event` and test construction helpers
  are syntax/mechanics helpers; they do not authenticate standalone boundary/control.

New source contains version1, full positiveEntrySourceCanonicalUtf8 and ordered
selectionEventCanonicalUtf8. New proof contains version1, source identity, complete
PositiveEntryProof, actual boundary and Control. The boundary stores both original
receipt IDs and full event hashes, actual Weather provenance, creationEventHash,
cycleId, full MovementEndProof and derived Candidate. It never fabricates a Weather hash
or converts a receipt suffix into a full-event hash. An earlier legal entry cut11/12
remains valid for its old reader but cannot enter actual selection.

## Dependency admission and ownership

All16 S1a table digests must match before any replay or cached entry proof access, even
on warm retries/readbacks. Oracle embeds the frozen manifest and requires inventory
sourceHashes equality; import preflight checks before loading predecessors. Its entry
cache key is complete immutable original bytes, with at most two admitted sources;
cache value is canonical immutable proof bytes. A separate proof cache holds at most128
fully replayed immutable proofs, keyed by complete canonical source bytes and every independently
supplied trusted Input byte sequence. It checks all16 pins before access, never accepts a source
digest/ledger-free key, and clears on capacity. Every returned projection is decoded fresh. No caller buffer or returned mutable collection becomes retained authority.

Admit through the unchanged positive-entry source reader. Importing Breakdown helpers
is permitted; executing historical Breakdown fixture main/check_fixture, recursive
Snapshot source_pins or outward admission is forbidden. Cold executable separation
probes prohibit their files/gates and still admit both exact sources. Original Breakdown
and cycle-sequence sequence-source failures, recursive Snapshot pin failure and outward
Content drift remain separate failures. S1a's12/60-second historical timeouts remain
unverified historical observations; later completed checks do not rewrite them.

## Canonical bytes and framing

Inventory field order is mandatory; explicit nullable fields remain present. Reject
unknown/missing/duplicate keys, alternate property order, whitespace/BOM, alternate
escaping, floating/exponent integers and bool-as-int. Strict ASCII retained JSON strings
are bounded UTF-8 record text. Each source/proof/input/event/control packet is at most
1MiB; depth32; ordinary arrays512; selection events/inputs/receipts16; version arithmetic
is checked signed Int64. Full original history plus all new records is retained; no
cumulative truncation. Identity arrays use their inherited canonical order; semantic
history, route, stepReceipts and receipts arrays never sort. Syntactically canonical
reordered causal arrays still fail replay.

For canonical bytes J define D(domain,J)=SHA256(ASCII domain || NUL || J):

| Identity | Frame |
| --- | --- |
| sourceId / sourceHash | `asrc.` / `sha256:` + D(`sandtable.combat.actual-selection.source.v1`, complete ordered source) |
| boundaryHash | ordinary SHA256 of complete ordered ActualSelectionBoundary |
| segmentId | `asg.` + D(`sandtable.combat.actual-selection.segment.v1`, complete ordered boundary) |
| selection / RBA decision | segmentId + `.selection` / `.rba` |
| receiptId | `asc.` + D(`sandtable.combat.actual-selection.receipt.v1`, complete ordered Event omitting final receiptId) |
| eventHash | ordinary SHA256 of complete accepted Event including receiptId |
| next prefix | existing Sequence prefix-event framing over prior prefix and complete accepted event bytes |

EventType is `actual-combat-` plus one closed effect tag. Event includes the original
positiveEntrySourceHash and cycleId, campaign/rules/config identities, segment, checked
versions, priorPrefix, complete Input and closed Effect. No digest contains itself.
The source digest is a cache binding; it confers no authority without replay and trusted
inputs. Old event tags and mixed families fail new replay.

## Input, clocks, retries and error order

Command/Input, candidate, fixed Config1 budgets, windows, closed effects and ordered
Control retain compatible C3a semantics with new framing. Selection opens at trusted1000,
deadline31000/highWater1100 after the retained choice; RBA opens2000,
deadline32000/highWater2100 after decline in the positive literals. These are fake
trusted test instants, not live clock evidence. Deadline is exclusive. Null/unavailable,
below-high-water time and deadline equality reject player actions; appropriate System
expiry/unavailable actions close or cancel. RBA opening cannot precede accepted selection
high-water. Deadline overflow rejects; no deadline is renewed on retry.

Order: dependencies009; primitive/closed syntax001/002; command version/kind/allowed
arms003; segment/System-vs-seat004; exact commandHash+actor duplicate lookup; stale timer
no-op; closed/capacity/version006; expected structural version/position006; active
state/decision006; actor/participant/candidate004 and choice003; clock005; positive FA007.
Candidate validity explicitly precedes clock in this new family, as required by S2.
Raw alternate canonical spelling is008; causal event/proof/control mismatch006.
The oracle freezes concrete vectors and rejects every locally re-signed event/proof leaf.

Retries identify exact original canonical Command plus actor, preserving C3a semantics.
Changed valid trusted time/availability alone returns original owned event/receipt bytes
and unchanged current Control, even after later or terminal steps. Invalid primitives
reject before duplicate lookup. Another actor cannot borrow a receipt. Entry's existing
exact-entire-input retry identity remains unchanged. Stale System timers and before-deadline
expiry return no event/receipt and byte-identical Control; a timer cannot consume another
window or a closed state.

## Verification and remaining gates

Semantic RED ran first across both owners and all eight variants, before implementation
and literal freeze. Semantic GREEN then established versions14/15/19/20, endpoint and
World/RNG preservation. A second RED/GREEN freezes candidate-before-clock ordering.
Oracle checks every cut/suffix, all original retries after later steps, canonical bytes,
all entry/event/proof leaves, ledger actor/time forgeries, owner/clock/error order,
capacity, cache pin tamper, ownership, family incompatibility and private field smuggling.
Normal oracle execution compares literals and never regenerates or repairs fixture bytes.

Run `python3 -B docs/specs/verify-combat-actual-selection-v1.py`. Dated author/handoff
records exact results and unchanged predecessor outcomes. Fresh independent review,
coordinator reconciliation and exact-head CI/merge precede S4. Native byte parity,
production authentication, actual Round/result admission and outward privacy remain unproved.
