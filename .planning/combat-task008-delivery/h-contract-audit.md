# Initial H literal Snapshot12 contract audit

Status: bounded research complete; recommendation awaits lead scope decision. Not implementation,
independent review, runtime parity or publication evidence. Requested base: accepted F6 `45a1882`;
source checkout inspected 2026-09-19 local date. No git command used, so commit identity comes from
dispatch, not an independent checkout assertion. No .NET builds, schema/code edits or publication.
Only this audit file owned; concurrent work preserved.

## Decision frame

Question: smallest additive contract-first packet enabling literal Snapshot12 restore for every
implemented B–G/019A/F1–F6 retained cut with fresh admission disabled. Lead owns decision. Need now:
family caches are implemented, but complete persisted root cannot yet express their state. Sources:
canonical plan and schemas/oracles first; accepted C# second; provisional h-restore-research.md only
as testimony. Success: exact root order/old bytes preserved, every retained field accounted for,
trusted-history boundary explicit, ≤5-primary-file implementation slices feasible. Stop: evidence-backed
recommendation and precise unresolved design questions; no implementation authority inferred.

## Conclusion

**Inference, high confidence:** add H0 `combat-inherited-snapshot-v1` schema/spec/fixture/oracle packet.
Keep creation Snapshot12 and historical synthetic first-Combat vectors frozen. Define two new closed
`cycleState` arms: pre-opening Reserve designation and inherited first-cycle state. Root `combatState`
stays null throughout initial H, including G2 entry before Combat handlers. Extend root position,
Reaction window and Breakdown flow unions only for already implemented causal states. Preamble cuts
retain null cycleState. Do not embed family `State` JSON or RootSnapshot1 fragments as authority.

H0 should establish full literal roots from actual creation-rooted family oracle cuts, not replace
history replay with family-cache acceptance. Later009–019 must add/version composition retaining
inherited evidence; old synthetic CycleFrame cannot silently replace inherited-cycle arm.

## Sources and evidence

- **Documented fact:** [plan](../../docs/design/combat-cycle-implementation-plan.md), lines782–842,
  especially Initial H boundary829 and checkpoint D839: every retained cut whose handlers exist;
  later009–019 extend same contract. Task009 depends005–008. HOST-PUB-001 remains separate.
- **Documented fact:** [C2 schema](../../docs/specs/combat-authority-envelope-v1.schema.json) `Snapshot`
  fixes creation null/empty slots. [creation codec](../../src/Cna.Core/Campaigns/CampaignCreationSnapshotV12Codec.cs):
  Serialize writes full root; Deserialize57–68 reconstructs from trusted request/Created11 and exact
  whole-byte compares. Noninitial state deliberately rejected. Preserve this reader.
- **Documented fact:** [snapshot schema](../../docs/specs/combat-snapshot-composition-v1.schema.json)
  `FullSnapshot` fixes reactionWindow:null, breakdownFlow:Idle, cycleState:CycleFrame and three
  combat arms selection/round/settlement. [spec](../../docs/specs/combat-snapshot-composition-v1.md)
  explicitly calls inherited header/ledger synthetic and excludes arbitrary Reaction. Its
  [oracle](../../docs/specs/verify-combat-snapshot-composition-v1.py):78–119 composes those lanes only.
- **Documented fact:** [authority schema](../../docs/specs/combat-authority-composition-v1.schema.json)
  RootSnapshot has contractVersion1, trace/source metadata, fragments and terminal witness.
  [oracle](../../docs/specs/verify-combat-authority-composition-v1.py):438–463 builds that shape,
  not C2's literal root. Its 28 terminal traces/capacity measurements do not prove every H root cut.
- **Documented fact:** [F6 replay](../../src/Cna.Core/Campaigns/CampaignCombatReactionCompletion.cs):11–58
  reconstructs F5 predecessor, requires actual active second move, compares each event, and ReadState
  compares complete family cache. [F6 codec](../../src/Cna.Core/Campaigns/CampaignCombatReactionCompletionCodec.cs):88+
  is contractVersion1 family state with currentPosition/windows, not Snapshot12. Same distinction
  appears in [F1](../../src/Cna.Core/Campaigns/CampaignCombatReactionTrigger.cs):9–47.
- **Documented fact:** [G2 model](../../src/Cna.Core/Campaigns/CampaignCombatBreakdownCompletionModels.cs)
  retains real lifecycle predecessor and separate Breakdown completion receipt; [G2 codec](../../src/Cna.Core/Campaigns/CampaignCombatBreakdownCompletionCodec.cs):91+
  serializes lifecycle evidence. Cannot replace these with idle/null defaults at Combat entry.

**Observation:** CCE search used first; expand_chunk on named schema returned `Chunk not found`.
Precise file fallback used thereafter. Cheap Python schema inspection compared ordered field names
and SHA256 of source bytes. Both C2 Snapshot and historical FullSnapshot have identical 19-field order:

`contractVersion,campaignId,stateVersion,rulesetHash,setup,world,initiativeHolder,operationStageOrders,operationStageWeather,randomState,currentPosition,reactionWindow,breakdownFlow,configuration,creationReceipt,chroniclePrefix,cycleState,combatState,commandReceipts`

Command (read-only, Python3 from environment):

```python
import hashlib,json,pathlib
for name in ['combat-authority-envelope-v1','combat-snapshot-composition-v1','combat-authority-composition-v1','combat-inherited-reaction-movement-completion-v1']:
    p=pathlib.Path('docs/specs')/(name+'.schema.json')
    print(str(p),hashlib.sha256(p.read_bytes()).hexdigest())
a=json.loads(pathlib.Path('docs/specs/combat-authority-envelope-v1.schema.json').read_text())['objects']['Snapshot']
b=json.loads(pathlib.Path('docs/specs/combat-snapshot-composition-v1.schema.json').read_text())['objects']['FullSnapshot']
keys=lambda s:[x.split(':')[0] for x in s.split()]
print('root field order equal:',keys(a)==keys(b))
```

Result: root field order equal `True`. SHA256 values respectively:

- C2: `91f90ba47cdfff088b3d8e5dba55c6c283d41c96381e56c7bba115bfb5390550`
- historical full composition: `e9e8f45751cad5fcab1be9e735359d50f34ec4e6bf5bdf41d9011ba9d176a586`
- authority composition: `47210ccd00d3eba301c97d2190c16354626265707a4f6109754150e30406bd42`
- F6: `ea4e4ce4b425e00b3bac08310cd93a1638d7c9c51a928b5062e90c101ac2dfad`

No oracle run claimed. These are source observations, not parity results.

## Exact retained-field accounting

State names below refer to `objects` in corresponding canonical schema under docs/specs; all common
identity fields bind trusted creation. Root retains Setup/Config/creationReceipt from trusted creation,
current World/RNG/version/prefix/headers/receipts from replay. `configurationHash`, `creationBinding`,
`creationEventHash` remain derivable bindings checked against root Config/receipt; duplicate root
fields unnecessary. Family `prefix` maps to `chroniclePrefix`, `receipts` to `commandReceipts`.

| Existing schema/state | Required root or new typed arm retention |
| --- | --- |
| combat-opening-preamble-v1 PreambleState | Nullable initiativeHolder, ordered operationStageOrders, actual sequencePosition, World/randomState/receipts. Weather array empty only before Weather event. No invented cycle. |
| combat-weather-v1 WeatherState; combat-stage-entry-v1 StageEntryState | Actual operationStageWeather and sequence position, all prior headers/receipts. Root currentPosition sequence arm. |
| combat-reserve-designation-v1 ReserveState | firstActingSide, members:ReserveMember[]; before opening cycle/cycleId/openingBaseHash/completionReceiptId genuinely null. Root has no slot for membership. New pre-opening Reserve arm needed. |
| Same state after019A atomic completion | Exact cycle:Authority, cycleId, openingBaseHash, completionReceiptId plus firstActingSide/members. New inherited-cycle arm. |
| combat-inherited-movement-v1 InheritedState | tracks:InheritedTrack[] (unit + ordered route), actualProgressRefs:ActualProgressRef[] (eventType/receiptId/eventHash), live moving flow; preserve no-route state according to oracle rather than manufacture MovingRoute. |
| combat-inherited-movement-lifecycle-v1 LifecycleState | interruptContext:InterruptContext? (cycle/cycleId/sequencePosition), movementEnd:MovementEndProof? (scope/ordinal/completionReceiptId/endLocations/excludedBefore), phasing-stop/moving/idle flow. |
| combat-inherited-breakdown-completion-v1 CombatEntryState | All prior lifecycle fields and breakdownCompletionReceiptId. Enter Combat position without dropping movementEnd/progress/interrupt evidence. |
| combat-inherited-reaction-trigger-v1 TriggerState | Suspended sequencePosition, currentPosition reaction, full window identity/trigger/opportunities, phasingContinuation, initial null reactorRoute. |
| combat-inherited-reaction-lifecycle-v1 LifecycleState; closure-v1 using inherited state; active-fallback-v1 FallbackState; second-move-v1 SecondMoveState; movement-completion-v1 CompletionState | Full activeOpportunityId/resolvedOpportunityIds evolution, retained or null window per cut, current position reaction/breakdown-stop/sequence, moving/reacting/reactor-stop-open/reactor-stop-closed flows, tracks/progress/current World. |

ReactionWindow must retain reactionWindowId, triggerCommittedStateVersion, phasingSide, reactingSide,
reactingPosition (suspendedMovementPosition/phasingSide/reactingSide), triggerAuthority,
apparentTrigger, frozenOpportunities (opportunityId/representation/adjacency evidence), resolved IDs
and active ID. Reuse existing typed structures; no lossy projection to public handles.

**Inference:** minimal new arm payloads can carry only fields absent from root. Pre-opening arm:
kind + firstActingSide + members. Inherited-cycle arm: kind + firstActingSide + members + cycle +
cycleId + openingBaseHash + completionReceiptId + sequencePosition + tracks + actualProgressRefs +
interruptContext + movementEnd + breakdownCompletionReceiptId. Exact names/order/nullability need
H0 approval. Retaining sequencePosition here protects suspended position while root currentPosition
is Reaction/Breakdown; equality with root sequence position enforced whenever both apply. Empty
tracks/progress and null later evidence must be replay-proven at earlier cuts, never caller defaults.
No current World duplicate. Historical proof Worlds in existing later Combat arms remain historical.

F6 resolution event writes interruptContextAfter:null while state retains inactive resolved window
until closure. Do not equate event null interrupt context with deletion of reactionWindow. F4 closes
window before reactor-stop-closed resolution; F2/F6 reactor-stop-open retains it. These are different
cuts even when same eventType/version appears.

## Options and recommendation

| Option | Result / tradeoff |
| --- | --- |
| Treat family cache or RootSnapshot1 as full restore | Reject: no literal root parity or closed root coverage. |
| Widen old creation codec or rewrite historical FullSnapshot | Reject: weakens old-reader boundary or repins frozen source graph; old vectors are different prospective contexts. |
| Add fields to C2 root or carry opaque full-family blob | Reject: changes root contract or duplicates current authority/World and hides omitted evidence. |
| Add separate H0 contract importing pinned predecessors; closed typed root-slot extensions | Recommend: preserves historical bytes, explicit initial scope, modest extra schema/oracle work. Later gameplay extension remains visible. |

H0 four primary files: new spec, schema, fixture and verifier. Fifth only if dedicated inventory/helper
needed; do not modify pinned predecessor schemas to squeeze helper changes into scope. Pin complete
used schema/fixture/oracle sources, compare creation golden unchanged, and preserve historical
first-Combat oracle/goldens. Generate literal complete roots independently of C# from actual family
replay; freeze bytes/hash/length at every cut, both owners and every implemented family fixture
variant (opening branches, Weather kinds, optional Reserve designation, all supported move counts,
all selected Reaction exits). Deduplicate identical cuts by trusted history identity, not family name.

Shared-cut invariant: B2 terminal10/D1 initial10, F1 terminal13/F2 initial13/F3 initial13, and F2
first-move14/F4 initial14/F5 initial14 must yield identical complete root bytes for identical trusted
history. Choose arm by materialized causal state, not cache owner or sourceContract. Family adapters
may expose overlapping initial cuts; normalize those to one root representation before serialization.
If two legal replay adapters propose different authority for same accepted history, fail ambiguous
routing and fix contract; priority by family label is not acceptable. H0 goldens must retain explicit
shared-cut equality assertions as well as unique-cut counts.

Bounded subsequent packets, each ≤5 primary files and own dev + three independent-review gate:

1. H1 pre-cycle history router + immutable normalized restore state, tests and fixture loader if
   needed. Creation/B1/C/B2/D1/D2/019A; no full runtime admission yet.
2. H2 ordinary Movement/E2/G1/G2 routing + tests; state-based transition choice and exact full ledger.
3. H3 Reaction routing F1–F6 + tests; split closure/fallback from second-move completion if cap tight.
4. H4 full-root model/codec/readback + tests using accepted H0 roots and routers; admission-disabled
   restore seam with explicit trusted history inputs. Preserve old creation-only reader.
5. H5 cumulative cut/retry/tamper/capacity proof, bounded test/evidence packet if H4 cannot fit it.

Routers consume one ordered trusted event stream, bound total count/bytes before copying/enumerating,
then derive legal causal family from replay state and candidate exact input. EventType alone is not
selector: ElementMoved4 is E1 or F1; completion3 can follow first or second reactor move; close3 is
direct/F4/F2/F6. Reject unsupported tails, missing/duplicate/reordered/foreign events, ambiguous routes
and partial predecessor claims. Snapshot bytes and caller family tags never choose authority.

## Trust and admission policy

**Documented fact:** CampaignCreatedV11Serializer.cs:63–77 `CampaignCombatCreationCut.Decide` checks
retained bytes first: exact retained Created11 returns RequiresPublication=false even with admission
false; absent bytes + admission false throws. Keep restore independent of fresh publication.

Required proof: trusted request/registry + independently retained exact Created11 + trusted ordered
accepted history/head + matching snapshot succeed with fresh admission disabled at every cut.
Missing retained creation fails even if request deterministically regenerates identical bytes. Wrong
trusted request/registry or trusted history rejects. Compare snapshot against independently replayed
complete root. Self-consistent alternative lawful branch with different committed trusted history
must reject; recomputable prefix/event/receipt hashes alone do not authenticate commitment.

Read-only Core caller trust is explicit contract, not cryptographic archive authenticity. Freeze
input ownership/copy semantics, mutated caller buffers and size/depth/array negatives. No snapshot
history field added. Storage/provider/head publication remains HOST-PUB-001; in-process H cannot
claim process restart, atomic archive uniqueness or host admission feature-flag integration.

## Later009–019 consequences and unresolved decisions

Historical CycleFrame holds authority/authorityId/firstActingSide/attackHistory/targetUses only.
It omits members, tracks, actualProgressRefs, openingBaseHash/completionReceiptId, interruptContext,
movementEnd and Breakdown completion receipt. Therefore converting H0 inherited-cycle directly to
that frame on selection would lose retained evidence. **Recommendation:** later first actual Combat
composition must add/version typed arm family retaining inherited evidence alongside new selection/
round/settlement state, and recompute full preRoundSnapshotHash from actual predecessor root. Do not
promise old synthetic first-Combat root bytes equal future reachable campaign roots. Existing old
vectors/readers stay frozen; new version/tag choice explicit before consuming runtime handlers.

Later009–019 add attack/target ledgers, future obligations, release/cycle continuity and their own
cut/mutation/capacity proofs. Initial H ends at actual implemented G2 Combat entry and selected
Reaction closure/resumption cuts; restored closure is not permission for currently unsupported
post-closure continuation. Do not require future handlers now or claim cumulative28 closure.

Lead must settle before H0 freeze:

1. Exact two arm tags, field order and conditional null rules; pre-Reserve cycleState null versus
   first Reserve arm at state10 must be determined by actual cut, with equal histories equal bytes.
2. Explicit inherited receipt type alias (PreambleReceipt) versus later C3 command Receipt; preserve
   actor encoding and full increasing-version ledger without Receipt-name collision.
3. Root union definitions and exact mapping of no-route E1 null flow to root representation, plus
   sequencePosition retention/equality for interrupted states. No implicit synthetic idle default.
4. Total supported-history limit and aggregate envelope capacity; one-MiB event limit alone is
   insufficient. H0 measures every literal root against one-MiB/depth32/512-array/4096-cause limits;
   later whole-campaign receipt policy remains separate.
5. Trusted retained-history/head API ownership and proof missing creation is rejected; policy must
   not be simulated by calling codec alone.
6. Later additive/versioned actual Combat arm boundary, ensuring no evidence loss or old-golden
   reinterpretation. Decide now as extension rule, not implementation of future handlers.

Confidence high on coverage gap/field inventory/trust boundary; proposed exact arm schema and runtime
file split remain design choices, not verified implementations. Next gate: lead accepts or adjusts
H0 scope; contract authoring and review precede router/root codec work.
