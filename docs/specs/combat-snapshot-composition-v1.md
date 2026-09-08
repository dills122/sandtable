# Combat noninitial Snapshot12 composition

Status: CMB-TASK-003C3c.2 complete as a bounded contract checkpoint after `65d708b`. This packet composes the bounded first-Combat
arms and audits inherited compatibility. [Plan](../design/combat-cycle-implementation-plan.md),
[C2 creation](combat-authority-envelope-v1.md), [C3a](combat-selection-steps-v1.md),
[C3b](combat-sealed-round-v1.md), [C3c.1](combat-result-settlement-v1.md).
[Schema](combat-snapshot-composition-v1.schema.json), [fixture](fixtures/combat-snapshot-composition-v1.json),
[oracle](verify-combat-snapshot-composition-v1.py). Production remains gated.

## Trusted composition boundary

Retain Snapshot12's complete C2 root field order. Replace its creation-only null/empty slots with
closed typed first-Combat arms; do not reinterpret any C2 creation golden. Recover C2 creation
receipt from separately supplied exact Created11/Request/registry inputs. Current World/RNG/version/
position/prefix come from the selected replay cut, never from creation defaults after initialization.

Caller independently supplies the admitted pre-Combat boundary and complete inherited metadata/
command ledger. This oracle verifies and preserves those supplied values; it does not authenticate
or reconstruct preamble events that do not yet have Rules10 adapters. Fixture headers and receipt
ledger are explicitly **synthetic**: valid prospective shapes, fabricated receipt hashes and normal
fall Weather11. Together with C3a's synthetic boundary/prefix they are not a reachable campaign trace.
D2 and the runtime adapter owners must replace assumed lineage with checked accepted predecessors.

Header preserves initiative holder, ordered stage-order/Weather values and inherited receipts.
Selected examples start turn1/stage1, axis predetermined holder, one stage order and one normal
Weather entry. Retain the other original fields: Setup7, World7, RNG, reaction null, idle Breakdown,
Config and creation receipt. No arbitrary reaction, nonempty vehicle lot or later-stage state is
admitted by these first-Combat arms. Arrays are mandatory even where the profile proves emptiness.

## Closed arms and boundaries

CycleFrame retains exact D1 Authority identity, authority hash, first actor, directional attack
history and target use. It remains `kind=combat-boundary` through Reserve Release entry; D2 owns the
next cycle/release family. Never infer repeat permission or erase retained future obligations.

CombatState is exactly selection, round or settlement. Selection embeds immutable boundary + C3a
Control; round embeds immutable C3b Base + RoundState; settlement embeds Base + immutable committed
RoundState + current ResultState. Embedded predecessor Worlds are historical proof, not additional
live balances. Snapshot.world is the sole current projection and must equal the current arm's World.

For every round/settlement arm, preRoundSnapshotHash is SHA256 of the complete FA Snapshot12 before
opening, including inherited metadata/receipts and the selected C3a cut. Recompute it from trusted
inputs; never accept it merely because an attacker supplied matching bytes. Preserve C3b's explicit
base-fragment digest/IDs unchanged and verify both bindings. Real prior-prefix/event validation must
make the mapping unique before runtime admission; this synthetic fixture does not prove that lineage.

Root commandReceipts is inherited ledger followed by exactly the C3a, C3b and C3c receipts at that
cut, in increasing authority-version order with no duplicate receipt/command identity. No omitted,
reordered or post-cut receipt is admitted. Creation receipt has its distinct C2 schema; it must not
be decoded as C3a's command Receipt despite their old inventory name collision.

Current position uses D1's exact materialized step: C3a/C3b index0–5 or Reserve Release at6;
settlement stays atCA until its one final completion. Completed/no-selection/cancelled states retain
their exact evidence. Every cut binds full current resources, custody/future values, prefix and RNG.

Canonical limits: one MiB, depth32,512 ordinary array entries,4096 Cause entries in the root World
when a later compatible arm can prove them. These selected first-round arms have much tighter
reachable inventories. No truncation; D2/009 must prove remaining whole-envelope and ledger capacity
before any live offer, including repeated movement. A World limit is not a promise it fits all
snapshots. Missing/extra/duplicate fields, unknown arm tags and alternate bytes reject.

## Inherited-family compatibility audit and owners

Observed at `7bb2d11` (runtime unchanged by C3c). Current
[event/snapshot dispatch and projector](../../src/Cna.Core/Campaigns/CampaignCurrentEventRuntime.cs)
explicitly bind Created10, Snapshot11, Content6 and the current admission validator. None accepts
Rules10/World7 by virtue of keeping an eventType string. Existing historical bytes/readers remain
unchanged; a new-context variant below is not migration of an old save.

| Family | Current reader/writer facts | Required Rules10 path and implementation owner |
| --- | --- | --- |
| Creation and root snapshot | [Created10](../../src/Cna.Core/Campaigns/CampaignCreatedV10Serializer.cs), Setup6/World6 and Snapshot11. | Versioned Created11/Setup7/World7/Snapshot12 successors required, as frozen C2 + this packet.008 creation/codec child;021 registration after parity. |
| Initiative/order, no-obligation convoy and stage-entry preamble | [Preamble codec](../../src/Cna.Core/Campaigns/CampaignV11PreambleCodec.cs) retains event fields but explicitly rebinds sequence3↔4; [projector](../../src/Cna.Core/Campaigns/CampaignV11Preamble.cs) consumes Snapshot11/Content6. | Existing value fields can remain, but unchanged complete bytes are invalid in sequence5/new context. Freeze an explicit sequence5 event variant and typed successor projector.008 preamble child; do not silently accept both sequences in historical reader. |
| Weather | Same preamble wrapper; Weather state has exact per-stage dice/scope/effect counts via [codec](../../src/Cna.Core/Campaigns/CampaignOperationStageWeatherCodec.cs). | Preserve existing Weather value/rule bytes and RNG procedure; versioned sequence5 envelope/context adapter required.008 Weather child. Persist actual event provenance; synthetic C3a Weather hash is not a substitute. |
| Reserve designation/completion | Same preamble wrapper and current Snapshot11/Content6 selection checks. | Preserve designation meaning and receipts; adapt to Element/World7, sequence5 and new snapshot/projector.008 Reserve child, with007 World support. Never reinitialize CP/ammo/readiness at handoff. |
| Movement | [Dispatch](../../src/Cna.Core/Campaigns/CampaignBreakdownEventSerializer.cs) admits ElementMoved3/MovementSegmentCompleted2 into V11 projectors. | New-context/sequence5 variants and typed World7 successor projection required; retain cost/RNG/routing fields unless a changed rule contract explicitly requires otherwise.008 movement adapter +019 first-cycle handoff; D2 freezes cycle/break-off ledger deltas. |
| Reaction | ReactingElementMoved2, ReactionParticipantCompleted2, ReactionWindowClosed2; current typed windows/positions and V11 projector. | Historical complete bytes stay in historical context. New-context envelopes/positions require an explicit successor before any positive Reaction capability.008 Reaction child. Selected first-Combat profile has no positive opposing-ZOC episode; reject unsupported state rather than reset it. |
| Breakdown | ElementMovementStopped1, BreakdownStopResolved1, BreakdownSegmentCompleted1 and current World6 BP/lot bindings. | New-context/sequence5 variants required; retain all applicable movement-ended/BP/band/lot fields.008 Breakdown child +019/D2 terminal-to-first-cycle binding. Selected infantry has no vehicle lots, which does not authorize truncating them in another profile. |
| Current dispatch/observation/legal-action/Exercise adapter | Current dispatcher/projector hard-binds Content6/Snapshot11; opaque handles do not provide public restore import. |008 trusted codec/restore +021 current registration +020 public projections +022 Runner adoption. Hosting needs its own accepted restore/publication contract; this oracle is not that implementation. |

This inventory classifies compatibility; it does **not** assign unverified event version numbers or
freeze every inherited event field into this five-file packet. D2/004/checkpoint B must reconcile
exact successor declarations and first-opening evidence with these owners. No complete pre-Combat
Rules10 decoder is claimed. Retaining Rules9 artifacts in the Rules10 manifest preserves rule meaning,
not automatic compatibility of root state or sequence-bearing event bytes.

## Bounded adapter scheduling at checkpoint B

Split Task008 before implementation into creation/root codec, preamble, Weather, Reserve, Movement,
Reaction and Breakdown families above, with their focused parity/rejection fixtures. Each child must
fit five primary files; shared dispatch wiring follows verified families and preserves old readers.
These are named ownership boundaries, not seven production changes authorized by this contract turn.

Task019 retains cycle implementation ownership but must supply the first-cycle-opening child before
the first predecessor-to-Combat integration test. It must not wait until021 registration to discover
that Movement/Breakdown cannot reach the new Combat boundary. D2 owns its exact first-opening event,
prior-prefix and cumulative history/CP contract.005–007 dormant tables/state work can proceed only
after B; adapter and first-opening prerequisites must precede end-to-end009–019 evidence as needed.
004 must map public/Exercise contracts and each design AC to those owners without reporting this
prospective full-snapshot trace as runtime or privacy completion.

## Reconciliation and limits

Readback compares the complete canonical snapshot against separately replay-derived state for the
requested cut, including inherited header/receipt bytes. Private CMB-SNP errors:001 shape/size,
002 primitive bounds,003 arm/tag,004 trusted header mismatch,006 replay/composition mismatch,
008 alternate bytes. A self-consistent replacement history/header is not authenticated by its hash.
The caller must obtain those inputs independently from trusted accepted history/archive state.

Two creation contracts formerly used the inventory name Receipt for different shapes; this schema
uses CreationReceipt for C2's request/binding/hash value and retains Receipt for command evidence.
Neither predecessor schema changes. All first-Combat arm discriminator values are closed; later
cycle/release/Reaction arms must extend the prospective family before registration, preserving
frozen existing bytes or explicitly versioning any changed arm.

Capacity reconciliation: C2's creation-only512-item bound and B's4096 Cause allowance cannot be
promoted into an unbounded live guarantee. This full root allows4096 causes structurally, retains
512 ledger/other limits and the one-MiB envelope limit, and admits only replay-derived selected
first-Combat examples. D2/009 still owe whole-campaign capacity proof, receipt-retention policy,
future-obligation continuity and actual first-opening provenance before public runtime offers.
No elapsed-clock reconstruction, durable storage publication, production serializer parity or
independent-review verdict is supplied by these Python contracts.


## Author verification

`python3 docs/specs/verify-combat-snapshot-composition-v1.py` passes17 traces/149 complete cuts:
46 selection,27 round and76 settlement snapshots;1358 mutations and596 raw rejections. Fixture
pins all149 canonical byte lengths/hashes and two terminal full canonical strings. These are
regression vectors recorded after literal behavior checks, not a second serializer implementation.
Normal verification requires and compares frozen vectors; it never regenerates them.

| C3 layer | Checked cuts | Mutations | Raw rejections | Additional evidence |
| --- | --- | --- | --- | --- |
| C3a selection/control |41 |246 |164 | Exact retries, deadline/regression, availability, RBA races and FA gate. |
| C3b sealed round/commitment |23 |276 |138 | Seal order, deadlines, cancellation, costs/history and commitment guards. |
| C3c.1 result/settlement |68 |728 |340 | Eight literal cases, mirrored roles/reversed seals,96 timing checks, RNG and closure guards. |
| C3c.2 full composition |149 |1358 |596 | Inherited ledger, full pre-round binding and all current root projections. |

Counts describe overlapping contract layers, not distinct runtime scenarios. All ten predecessor,
source and RNG oracles also pass. Author verification only; independent-review budget remains8of8.
Next D2 freezes release/history/ordinary movement and actual first-opening contracts;004/B reconcile
outward contracts and remaining successor/capacity obligations before runtime implementation.
