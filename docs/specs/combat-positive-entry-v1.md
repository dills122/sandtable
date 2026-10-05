# Actual positive-entry contract v1

Task: `CMB-TASK-019E0`. Private executable contract; native adapter pending.
Accepted [R1 decision](../research/combat-positive-entry-feasibility.md) supplies smallest
creation-rooted candidate-before-selection prerequisite. No new gameplay or public registration.
[Ordered inventory](combat-positive-entry-v1.schema.json), [literal fixture](fixtures/combat-positive-entry-v1.json)
and [oracle](verify-combat-positive-entry-v1.py) define executable evidence.

## Frozen manifest and acceptance

Five primary paths: this specification, matching schema/fixture/oracle, and
[implementation plan](../design/combat-cycle-implementation-plan.md#task019e0--actual-positive-entry-executable-contract).
Administrative dated review, author/bootstrap and handoff documents allowed. Existing contracts,
oracles, native code, tests, transport and Snapshot remain byte-identical. Any prerequisite needing
another primary file or wider gameplay returns to coordinator before dependent implementation.

Acceptance: replay exact Request/Created11 and full original opening for both owners, then two
untimed real completion records; preserve World/resources/RNG/Weather/order/cycle; retain actual
Movement-end proof; derive supported candidate only at final Position Determination. Freeze
literal command/event/proof bytes after semantic assertions; prove every suffix cut, original-byte
retry, full-source rejection, completed-cache forgery, owner/scope/stale/input/canonical/clock and
capacity boundaries. Semantic RED/GREEN, unchanged predecessor oracles, repository regression gate
and fresh independent review precede publication. Coordinator owns review dispatch and final CI/merge.

## Actual retained source and trust

Only two source openings admitted: predecessor Reserve fixtures `normal-act-first-none` (Axis)
and `normal-act-last-none` (Commonwealth). Both retain seed1, Weather cursor2, Normal Weather,
ordinary NONE infantry, zero current CP/cohesion, ammo10/TOE10 and original adjacent geometry.
The scenario's existing synthetic initial-ledger/resource provenance labels are retained exactly;
actual creation/event replay does not relabel those content-origin facts. Full ordered Request,
Created11, four preamble records, one Weather2 record, four stage-entry records and one Reserve
completion2 record are mandatory. No designation, route, stop, Reaction or selection record exists.

`PositiveSource` is a closed ordered object: `contractVersion,requestCanonicalUtf8,
createdCanonicalUtf8,preambleEventCanonicalUtf8,weatherEventCanonicalUtf8,
stageEventCanonicalUtf8,reserveEventCanonicalUtf8,entryEventCanonicalUtf8`.
All UTF-8 carriers contain original compact ASCII canonical JSON bytes represented as strings.
Request and Created are single records; history arrays preserve authority order. Trusted actors
are retained in each closed original event input. No caller-supplied completed state, World,
Weather, actor order, cycle or candidate substitutes for replay.

Oracle first parses Request and replays full original Reserve history through unchanged reader.
It then compares SHA256 of canonical source with empty entry suffix against literal per-owner
`openingHashes` in inventory. These two exact pins bind all original Request/history bytes;
no equivalent re-signed or differently encoded opening is a third source. This is narrow fixture
profile admission, not arbitrary campaign admission. New fixture is test output only; admission
never imports it. Existing Request/Created reader validates retained creation context and rules.

## Causal transitions and exact reused grammar

| Cut | Version / receipts | Authority and effect | Candidate |
| --- | --- | --- | --- |
| Original Movement |11 /10 |Resolved owner; actual Reserve cycle ordinal1/openedAuthorityVersion11 |null |
| Movement completed |12 /11 |Owner CompleteCommand2 / CompleteEvent3; idle Movement→Breakdown |null |
| Breakdown completed |13 /12 |System EntryCommand2 / EntryEvent2; idle Breakdown→Position Determination |supported facts only |

`LifecycleInput` / `CompleteCommand` / `CompleteEvent` retain exact declared order, mandatory nulls
and 12-field legacy prefix from [Movement lifecycle](combat-inherited-movement-lifecycle-v1.schema.json).
Movement actionId is SHA256 of canonical `{contractVersion:1,kind:"complete-movement-segment"}`.
Actor must equal replay-derived cycle owner. Command carries creation binding/hash, cycleId,
expectedPriorVersion11 and original Movement positionId. Completion emits version3,11→12,
original gameTurn/stage/actingSide, catalog Breakdown position (activeSide=null), idle flow,
null interrupt, exact original-unit endLocations, derived exclusions=[], progress=[] and receipt.
No stop/resolution occurs because no moving route exists.

Movement receipt uses unchanged `iml.` + lowercase SHA256 of ASCII domain
`sandtable.combat.inherited-movement-completion-receipt.v3`, NUL and canonical unsigned
CompleteEvent (receiptId omitted). Actual Chronicle prefix extends once using existing
`prefix_event` framing. Full `MovementEndProof` contains original scope, ordinal1, actual Movement
completion receipt, full sorted original-unit endLocations and independently derived excludedBefore.
Distance1 leaves exclusions empty. Reserve completion receipt is distinct and never replaces it.

`EntryInput` / `EntryCommand` / `EntryEvent` retain exact order and 11-field legacy prefix from
[Breakdown completion](combat-inherited-breakdown-completion-v1.schema.json). ActionId is existing
stage1 completion hash. System alone executes untimed version2 Breakdown completion12→13;
exact prior position sources, real Movement completion receipt, original Reserve receipt/cycle/
opening identity, current prefix and input remain bound. `ibc.` receipt uses unchanged domain
`sandtable.combat.inherited-breakdown-completion-receipt.v2`, NUL and canonical unsigned EntryEvent.
Prefix extends once. Empty cohort performs no breakdown checks or RNG draws.

New admission reuses unchanged compatible transition kernels; it does not call/widen old route
or held-I admission. Existing route lifecycle still rejects no-move histories. This new source
reader alone authenticates ordinary NONE idle source. Versions, receipts and prefixes derive only
from accepted events; no synthetic completed-history promotion or digest-only cache is accepted.

## State, candidate and proof

Full ordered 27-field `CombatEntryState` is inherited unchanged from Breakdown inventory.
Initial entry projection extends replayed Reserve state with tracks=[], actualProgressRefs=[],
idle flow, null interrupt/movementEnd/breakdownCompletionReceiptId. These fields summarize new
reader's zero-move history; they do not alter World. Movement completion fills movementEnd;
Breakdown completion fills breakdownCompletionReceiptId. World, CP, ammo/TOE, original resource
provenance, RNG seed/cursor, memberships, Reserve history/future duties, Weather/order/initiative,
cycle, openingBaseHash and original Reserve receipt stay byte-equal across all three cuts.
Catalog activeSide remains null; owner derives from original cycle/order, not a position rewrite.

`PositiveEntryProof` order: `contractVersion,sourceId,sourceHash,entry,candidate`.
Version1. `sourceHash` is `sha256:` plus lowercase SHA256 of ASCII domain
`sandtable.combat.positive-entry-source.v1`, NUL and full canonical PositiveSource **including
ordered accepted suffix**. `sourceId` is `pe.` plus same lowercase digest. Thus each cut has its
own identity. Proof readback requires entire source and exact replay-derived bytes, not hash-only
trust. No source identity grants selection or later result authority.

Candidate=null at versions11/12. At13 derive applicable Weather from retained Weather state and
participants from exact authenticated World/creation scope. Candidate order:
`attacker,defender,targetLocationId,basis`; Participant order:
`unit,representationId,locationId,componentIds`. UnitKey carries actual creationBinding,
originalSide and elementId; representation and complete component provenance come from World.
Basis is `voluntary-adjacent`, target is defender location. Preserve full initial-profile facts,
Normal Weather and CP support (attacker≤5, defender≤7) under existing certification semantics.
Oracle uses unchanged pure candidate derivation only after exact source admission; future native
adapter calls `CertifyInitialProfileFacts` with actual Request/retained Created11/replayed World/
cycle/owner/Weather. Facts certification alone never authenticates history.

## Replay, retries and rejects

`replay(packet)` admits full original source and zero..two suffix records in causal order.
`apply(packet,input)` replays first, then authenticates closed input version/kind/owner/creation/
cycle before retry lookup. Exact accepted command at same expectedPriorVersion returns current
state, original owned event bytes and duplicate=true, including Movement retry after final entry.
Altered consumed bytes, foreign/stale command and new terminal command reject. Fresh commands
must equal replay-derived current capability and expected position/version. No clock, deadline,
selection window or controller-fallback policy belongs to either untimed command.

`read_source(bytes)` validates canonical packet and replays; `proof(packet)` derives full cache;
`read_proof(bytes,packet)` validates closed cache and byte-compares full replay. Every read returns
owned projections; caller mutation cannot change subsequent replay. Public oracle entry points
never accept supplied terminal state; private transition kernels are implementation details.

Error family `CMB-PEN`:001 malformed shape/type/size/depth;003 unsupported contract/kind;
004 foreign/unsupported source, scope or malformed predecessor/event;005 wrong actor;
006 stale/reordered/forged transition, consumed-input mismatch or proof mismatch;
007 history-count bound;008 alternate canonical encoding;009 retained source pin mismatch.
Exact error ordering follows executable reader; no compatibility guarantee for unused002.

Reject missing/reordered/duplicate opening/suffix, other owner histories, altered Request seed,
Weather/order/cycle/ordinal/slot, held-I/designated/released/converted/vehicle/moved/Reaction/
pending-stop sources, invented completion/receipt/proof/foreign prefix, changed consumed input,
re-signed event/state/proof leaves, completed-history caches, clock smuggling and alternate bytes.
Source pins plus unchanged predecessor replay reject narrower unsupported histories without
claiming all possible gameplay explored. Never accept all32 synthetic settled contexts as reachable.

## Canonical format and limits

Strict compact ASCII canonical JSON; inventory order mandatory; duplicate/unknown keys, BOM,
trailing whitespace, alternate escaping/key order, bool-as-int, float-as-int and nonfinite tokens
reject. Mandatory nulls stay explicit. UnitKey/EndLocation and identity component arrays sort by
existing identity rules; authoritative history arrays never sort. Maximum1MiB per packet/proof/
individual record, depth32, arrays512, signed Int64 versions with checked increment. Packet
counters are exactly4/1/4/1 original predecessor records and at most2 suffix events. UTF-8 record
strings are nonempty ASCII; enclosing record size bound also applies. Bounds do not authorize
extra records. Shape-only edge probes remain distinct from admitted campaign histories.

## Verification and next boundary

Run `python3 -B docs/specs/verify-combat-positive-entry-v1.py` for literal parity, semantic,
cut/retry, source/event/proof forgery and canonical/capacity matrix. `--semantic` runs both legal
transitions before fixture comparison. Retained fixture pins predecessor schemas/oracles/fixtures;
source files are checked against those hashes. Unchanged Reserve designation, stage-entry,
Movement lifecycle, Breakdown, empty selection, C3a, Round2 and Result2 oracles plus repository
regression gate precede fresh independent review. [Handoff](../work/handoffs/2026-10-05-positive-entry-contract.md)
records executed evidence, failures, hashes and review/publication state.

Next separately frozen native manifest after accepted/merged019E0: `CampaignCombatPositiveEntry.cs`,
`CampaignCombatPositiveEntryCodec.cs`, `CombatPositiveEntryTests.cs`, Core test project fixture
link and implementation plan. Full owned source capture, exact bytes, both owners, all cuts/retries,
source/canonical/clock/forgery negatives, build/Boundary/full/format and fresh review remain required.

Current C3a request/seed/Weather/receipt/position representations are incompatible with actual
entry. Selection/round/Result2 consumer bridge requires distinct contract/review; no consumption
or actual attack/result claim here. Later-II/consumed, release/repeat, ordinary repeated Movement,
Snapshot/public/transport/host/Runner and parents017–019 remain open.
