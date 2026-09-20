# Task010 scope research — proposed execution refinement, no implementation

Research baseline:009B code0f3ec7b frozen; acceptance/fullgate/reviews still pending. No010 code,
.NET, commits, agents or review dispatched. Only this note edited. Canonical dependency remains
009→010→011; every proposed child waits for009B acceptance and root dispatch. Parent010 stays open
until all responsibilities below pass. No new policy, wire contract or gameplay profile proposed.

## Decision

A faithful single five-path010 delivery is not credible. It combines three different frozen
contract grammars and authority boundaries, two clock regimes (untimed and timed), cumulative
routing, strict readers and fixture links. Recommend **010A actual inherited selection/traversal**,
**010B timed C3a selection/RBA**, then **010C cumulative router integration**. Each independently
reviewed child carries concrete runtime evidence. This is an execution refinement of row010, not
three newly invented capabilities or permission to omit original acceptance criteria.

010A and010B can each fit five primary paths with cohesive local models;010C fits three. Global
router integration may follow mechanisms, but every010A public operation must already reconstruct
complete creation-rooted predecessor history. No child may claim new full Snapshot12 persistence;
H4 writer currently handles only its accepted InitialH projections.

## Exact governing contracts and counts

Canonical `docs/design/combat-cycle-implementation-plan.md:859` requires segment/selection/RBA-decline,
six exact closures, no attack/progress for empty/cancelled and011-only Prepared continuation.

- `combat-inherited-selection-v1.md:14–80`, corresponding schema and oracle `read_boundary:124`,
  `transition:165`, `replay:212`, `apply:221`, `read_control:232`: four actual side/move histories,
  two events each (8 total), three local control cuts each (12), no clock/window. Four fixture rows
  retain seven byte lengths/hashes each (28 checks): predecessor record-hash list, AdmissionBoundary,
  two events and three controls. These are **hash/length goldens**, not stored literal event JSON.
  Tests reconstruct bytes and compare independent frozen commitments; label evidence accurately.
- `combat-inherited-no-attack-v1.md:14–80`, schema and oracle `initial:110`, `_transition:140`,
  `replay:183`, `apply:193`, `read_control:207`: same four histories, six steps each (24events),
  seven traversal cuts each (28),24 retries. Four fixture rows each retain15 byte lengths/hashes
  (60 checks): hash of predecessor golden inventory, nested selection, six events, seven controls.
  Frozen oracle also reports956 deep mutations,36 raw negatives,208 boundary/order/capacity probes.
  C# tests need meaningful equivalent causal coverage, not mechanical duplication of every scalar.
- `combat-selection-steps-v1.md:21–59,66–183`, schema and oracle `transition:173`: two literal
  synthetic Boundaries and five literal traces: no-candidate8events, voluntary-pass8, selection-expiry8,
  accepted-decline7, RBA-expiry10 =41events and41 post-event controls; five final Control goldens.
  Four no-attack traces close6steps each, positive trace closes3steps and stopsFA. Literal initial
  controls can additionally be checked as derived starts; do not relabel41 as including starts.
- No selection-steps-v2 contract was found. `combat-side-projection-v1.md:49–61` and oracle
  `mirror_steps:388` reuse unchanged C3a transition semantics for10 both-side mirrored synthetic
  sources. `steps-clock-v2` is a corrected source-profile label, not a new selection wire grammar.
  Round2 public-opening-floor correction belongs011. It must not silently replace C3a's retained
  selection/RBA high-water timing or rewrite old bytes. Preserve current Round2 fixture dependency.

##010A — actual inherited empty selection plus no-attack traversal

Five primary paths proposed:

1. New `src/Cna.Core/Campaigns/CampaignCombatInheritedSelection.cs`: local immutable Command2/Input2,
   Control1/event/result values and history-rooted selection engine. Two transitions only.
2. New `src/Cna.Core/Campaigns/CampaignCombatInheritedNoAttack.cs`: local immutable Command2/Input2,
   traversal Control1/event/result values and six-step engine. Keep frozen nested selection unchanged.
3. Existing `CampaignCombatIdentityCodec.cs`: reuse its private frozen Boundary grammar and expose
   cohesive selection/traversal codecs. Avoid copying the51-object G2 grammar into new modules.
   Named syntax helpers used by these readers may become internal; no global schema framework.
4. New `tests/Cna.Core.Tests/Campaigns/CombatInheritedStepsTests.cs`.
5. `tests/Cna.Core.Tests/Cna.Core.Tests.csproj`: link only two existing inherited fixtures.

Local models remain contract-specific, not a universal mutable Control or inferred caller family.
Do not flatten selection/traversal controls: selection.stepIndex0/segmentClosedfalse remains retained
inside terminal traversal.stepIndex6/closedtrue. One file per compact state machine is acceptable;
do not conceal an architectural rewrite merely to hit a file count. Root freezes exact layout before
implementation if codec growth warrants a separately approved tiny helper extraction.

Proposed API shapes (full names may retain existing naming convention):

```csharp
SelectionControl ReplaySelection(CampaignCombatCreationRequest request,
    ReadOnlySpan<byte> created, IReadOnlyList<byte[]> predecessorEvents,
    IReadOnlyList<byte[]> selectionEvents);
SelectionResult ApplySelection(CampaignCombatCreationRequest request,
    ReadOnlySpan<byte> created, IReadOnlyList<byte[]> predecessorEvents,
    IReadOnlyList<byte[]> selectionEvents, SelectionInput input);
SelectionControl ReadSelectionControl(ReadOnlySpan<byte> bytes,
    CampaignCombatCreationRequest request, ReadOnlySpan<byte> created,
    IReadOnlyList<byte[]> predecessorEvents, IReadOnlyList<byte[]> selectionEvents);
TraversalControl ReplayTraversal(CampaignCombatCreationRequest request,
    ReadOnlySpan<byte> created, IReadOnlyList<byte[]> predecessorEvents,
    IReadOnlyList<byte[]> selectionEvents, IReadOnlyList<byte[]> stepEvents);
TraversalResult ApplyTraversal(CampaignCombatCreationRequest request,
    ReadOnlySpan<byte> created, IReadOnlyList<byte[]> predecessorEvents,
    IReadOnlyList<byte[]> selectionEvents, IReadOnlyList<byte[]> stepEvents,
    TraversalInput input, byte[]? cachedControl = null);
```

ReadTraversalControl mirrors ReplayTraversal with leading bytes. Result owns copies of emitted or
retained event bytes, immutable current control and IsDuplicate. No result says publication occurred.
PredecessorEvents must terminate at actual G2;009A `Admit` replays all of it. Caller partition lengths
are not evidence. Selection replay requires≤2 local events; traversal replay first reconstructs
actual closed selection, then≤6 steps. No supplied state cache can replace either predecessor.
Use `CampaignCombatRetainedHistory.Capture` ownership/count/aggregate bounds for input snapshots;
check local capacities before indexing adversarial lists. Take stable copies before replay.

Selection opening and closure remain System-only, no time fields. Validate version/kind/permitted
fields/actor before duplicate lookup. Match exact Input hash and actor; duplicate returns retained
accepted bytes, not new serialization substituted as a retry. Open derives boundary/assessment hash,
zero candidates and null decision. Close requires actual opening receipt. All invariants preserve
World/RNG/Weather/CP/Cohesion/proofs/position.

Traversal derives six catalog successors, never caller successor/proof. First previousStepReceiptId
is selection opening receipt; every dispositionReceiptId is selection closure; later previous-step
receipt is immediately prior accepted step. After six, actual position is same-slot ReserveRelease,
not released state. No extra SegmentCompleted event, reset, material progress or cycle completion.

Readers apply shape/bounds→canonical spelling→trusted replay→whole-byte equality. Sentry tests must
cover malformed nested Boundary/Control before history access, learning from009A's two accepted
ordering findings. Reuse typed canonical prefix/receipt helpers, but respect distinct cis./cin.
domains and each packet's Input-vs-Command hash definition. No universal retry hash assumption.

Acceptance: all four actual source histories from existing `SnapshotHistories()` enumerator; all
28+60 frozen hash/length commitments; every selection/traversal cut and retry; altered receipt,
assessment, prior/head/prefix, disposition/position/previous-step, actor, foreign six/seven/side,
reordered/duplicate/missing/post-close tail and noncanonical/capacity/ownership rejection. Assert
byte-identical inherited entry and nested selection, no changed World/RNG or attacked/target history.
Cumulative test history is Created+actual20/21 predecessor events+2selection+6traversal =28/29 total
events, final authority version29/30. This is actual Core replay evidence even before generic router
accepts its concatenated suffix. No Snapshot12 claim.

##010B — timed C3a provisional intent and cancellation

Depends010A (and009B); preserves its untimed actual arm. Five primary paths proposed:

1. New `CampaignCombatSelectionSteps.cs`: immutable frozen Boundary/Control/Input/Timing and effect
   values plus cohesive timed state machine. No authority supplied by a precomputed Candidate.
2. New `CampaignCombatSelectionStepsCodec.cs`: strict current C3a grammar/writers/readback.
3. Existing `CampaignCombatIdentityCodec.cs`: minimal named syntax reuse for Candidate/Participant
   and frozen World/Position primitives; production readers still compare independently trusted facts.
4. New `CombatStepsTests.cs`.
5. Core test csproj: existing selection-steps-v1 fixture already linked by009A; add only current
   side-profile fixture if chosen as a literal supplemental source. No generated fixture edits.

Request/config/Created are independently supplied. New typed Boundary exactly follows existing wire
shape (creationBinding,cycle,firstActingSide,priorVersion/prefix,completedBreakdownReceipt,Position,
World,Random,ApplicableWeather,idle,nullReaction). It is a trusted input value, not an event or wire
certificate. Constructor/readback checks local consistency and009B positive-profile predicate;
caller owns complete history authentication. Full C3a admission remains bound to its exact retained
compatible C2 fixture campaign/Setup/config/seed per spec:33–36;009B's pure fact predicate does not
permit generalizing that complete boundary merely because another request passes local predicates.
Synthetic fixture Boundary provenance remains explicit.
Do not feed actual moved G2 into this initial-position C3a arm or reset its CP to make a candidate.

Proposed entry points:

```csharp
StepsControl ReplaySteps(CampaignCombatCreationRequest request, ReadOnlySpan<byte> created,
    SelectionBoundary trustedBoundary, IReadOnlyList<SelectionInput> trustedInputs,
    IReadOnlyList<byte[]> acceptedEvents);
StepsResult ApplySteps(CampaignCombatCreationRequest request, ReadOnlySpan<byte> created,
    SelectionBoundary trustedBoundary, IReadOnlyList<SelectionInput> trustedInputs,
    IReadOnlyList<byte[]> acceptedEvents, SelectionInput input);
StepsControl ReadStepsControl(ReadOnlySpan<byte> bytes,
    CampaignCombatCreationRequest request, ReadOnlySpan<byte> created,
    SelectionBoundary trustedBoundary, IReadOnlyList<SelectionInput> trustedInputs,
    IReadOnlyList<byte[]> acceptedEvents);
```

Trusted inputs here pair with accepted events; no-op calls do not become invented persisted events.
Trusted inputs remain separate from event bytes; do not authenticate actor/time merely by reading
its claim out of the same untrusted event. Typed Input fields exactly Command1, authenticated actor,
nullable UTC and clockAvailable. Actor enum can reuse existing CampaignOpeningPreambleActor.
StepsResult distinguishes accepted event, exact retained retry, no-op and rejection without publishing.

Existing `CombatDecisionConfiguration` provides seven pinned budgets/fallbacks; no implemented Core
Timing/high-water type was found. Add frozen Timing1 in this module: checked deadline, UTC0..
253402300799999, opening/deadline immutable, retained high-water. Read admitted clock values only;
no wall-clock API or async work. Exact retry lookup precedes active-window clock handling per oracle.
Stale timer/unavailability IDs and pre-deadline timer are no-ops. Equality expires; regressed/missing
reliable clock rejects or follows exact trusted-unavailability branch, never clamps or invents time.
Selection unavailable before opening creates no-window system-no-selection; RBA pre-opening
unavailability cancels with null timing and no synthetic decline. Defender alone may explicitly
decline, binding original UnitKey and current selection receipt. Closed windows remain retained.

Positive intent closes Position(no-gun-positions), Barrage(no-barrage-work), RBA(accepted-decline),
then stops atFA. No assignment, Prepared, commitment or later result code. No-selection/cancelled
paths finish all6steps with no-attack proof and no resources/history/RNG change. Distinguish target
hex from frozen original participant and use009B Candidate comparator and all-result certification.

Current World serialization reuse needs care: initial-world codec rejects nonzero CP. Follow existing
bounded typed-writer pattern: serialize trusted initial baseline and project only validated current
CP into fixed fields, or expose a small typed canonical writer; never accept arbitrary caller JSON
as authority. Existing inherited movement WriteWorld demonstrates this mapping approach. No broad
World codec refactor is authorized by this research.

Acceptance: all five literal traces/41 exact events/five finals/every cut; two literal Boundaries;
explicit before/equal/after deadlines, backward/unavailable clock at opening and RBA, stale timers,
wrong owner/participant, changed retry, malformed bytes before trusted context, capacity/version
overflow, step order, missing disposition and unsupported positive FA completion. Test both role
orientations with clearly labelled synthetic mirrors (side oracle already has10 mirrored sources).
Mirrors are supplemental mechanism evidence, not extra actual-history cases or independent literal
C3a goldens. Check011 Round2 authenticates the same C3a accepted-decline prefix later; do not implement
that handoff's seal/Prepared behavior here.

##010C — bounded cumulative dispatch closure

Depends accepted010A/010B. Three primary paths:

1. `CampaignCombatHistoryReplay.cs`: after exactly one accepted G2 completion, route actual2-event
   selection and then6-event traversal using typed predecessor acceptance. Preserve existing forks.
2. `CampaignCombatHistoryModels.cs`: new closed projections expose true current head/position and
   cumulative receipts; preserve local nested Control receipt arrays unchanged.
3. New `CombatStepsHistoryReplayTests.cs`: concatenate actual full history; reuse010A fixture links
   and existing prefix enumerators. No transcripts copied or synthetic path admitted by generic router.

Current Replay sends whole post-lifecycle tail to BreakdownCompletion; change that partition only
when its typed completion has been accepted. Call010A with the exact already-captured G2 prefix, so
its own009A admission cannot recurse on a suffix. Two families share no caller-supplied family tag.
Every local cut, prefix comparison and complete ledger length agrees with direct family replay.
Same-slot ReserveRelease is structural position only. Generic router must continue rejecting C3a
synthetic Boundary/event histories as actual creation-rooted assault.

Do not update H4 Snapshot12 writer by inventing a new family arm. Tests retain all old H4 byte/hash
vectors and state clearly that new family persistence remains unsupported until exact current
composed-root writer work is separately dispatched. This does not waive010 fragment restore tests.

## Parent010 closure criterion and dependencies

All three accepted children, cumulative actual2+6-event replay, exact timed selected/empty/cancelled
mechanisms and strict restart/readback at every step must pass dev review, three fresh independent
rounds per behavioral slice and root gates. Reuse cumulative005 source arithmetic/006–009 actual
history/009B certification evidence. Parent cannot close after only actual empty traces or only five
synthetic timed traces. No current full positive creation-rooted assault, all28-runtime completion,
Checkpoint-B/public-side privacy or HOST-PUB-001 claim follows.

011 alone extends positiveFA with two private slots, current Round2 opening floor/deadlines and
Prepared evidence, then later tasks own commitment/result/settlement. Canonical dependency remains
009A→009B→010A→010B→010C→011. No semantic blocker found; file-count and trust-boundary complexity
justify execution refinement before code. Root approval must freeze this proposal before any010 edit.
