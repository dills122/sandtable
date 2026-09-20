# Task010C cumulative router dispatch proposal — research only

Research baseline:010A code2eda2ed, root full2265/Boundary81/build/format/exact-head CI passed, ordinary review2 active;010B research proposal retained, not implementation-authorized. No010C implementation until accepted010A+010B and explicit root dispatch. Only this proposal edited; no source/tests/.NET/commits/agents/reviews. Preserve canonical009→010A→010B→010C→011 order.

## Decision and three primary paths

Faithful bounded integration fits original three-path proposal:

1. `src/Cna.Core/Campaigns/CampaignCombatHistoryReplay.cs`: admit one exact G2 completion before partitioning actual selection2 and no-attack6 suffixes. No synthetic C3a route or caller family selector.
2. `src/Cna.Core/Campaigns/CampaignCombatHistoryModels.cs`: two closed typed projections, with true current authority/prefix/position and cumulative read-only receipts.
3. New `tests/Cna.Core.Tests/Campaigns/CombatStepsHistoryReplayTests.cs`: full creation-rooted cut/prefix/ledger/ownership/adversarial integration proof, reusing existing actual-history enumerators and010A APIs.

No model/engine/codec rewrite, new event, fixture/csproj change or Snapshot12 edit required. Tests use existing Movement `SnapshotHistories()` to obtain complete G2 sources and accepted public010A Replay/Apply helpers to append2+6 events. This is a small typed runtime tail builder, not a copied creation/movement transcript generator. Existing010A tests/fixtures remain acceptance authority for exact local bytes; new test can check same published hash/length fields through already-linked fixtures without changing private test helpers.

## Governing source locators

- `docs/design/combat-cycle-implementation-plan.md:859` row010 and accepted execution refinement around878–884:010C routes actual010A only; parent closure requires all three; Prepared011 and extended Snapshot12 excluded.
- `.planning/combat-task008-delivery/task010-scope-research.md`, section010C and parent closure.
- `CampaignCombatHistoryReplay.cs:14–57`: current causal gates; line55 onward sends entire remaining tail into G2 reader. This is sole partition requiring extension.
- `CampaignCombatBreakdownCompletion.cs:10–29`: G2 needs exactly3 lifecycle events and at most1 completion; strict typed reader provides completion authority.
- `CampaignCombatCertification.cs:10–35`:010A `Admit` calls generic Replay then demands exact completed G2, supported six/seven movement scope. Critical recursive dependency must terminate at exact predecessor cut.
- `CampaignCombatInheritedSelection.cs:42–60`: captures≤2 local events and reconstructs actual Boundary; `CampaignCombatInheritedNoAttack.cs:39–56`: captures≤6 local events, requires actual closed selection. Accepted reader state, not family tags or counts, proves continuation.
- `CampaignCombatHistoryModels.cs:6–16,58–64`: abstract projection head/position/receipt contract and existing G2 arm.
- `CampaignCombatRetainedHistory.cs:23–41`: top-level count512, each Created/event≤1MiB, aggregate event bytes16MiB and owned copies.
- `CampaignCombatInheritedSnapshotV12Codec.cs:8–24,126–142`: Serialize/Restore call generic router, but closed family writer switch defaults to `JsonException("Unsupported inherited Snapshot12 projection.")`.
- `CombatMovementHistoryReplayTests.cs:14–23`: existing actual-history enumerator; `CombatInheritedStepsTests.cs` accepted010A88 commitments; `CombatInheritedSnapshotTests.cs:12–50`:368 frozen vectors/286 distinct histories and fresh-disabled restore.

## Exact routing algorithm

Keep existing Created/preamble/Weather/stage/reserve/ordinary Movement/Reaction dispatch unchanged. Retained history remains captured exactly once at entry; downstream readers see owned history slices. No request regeneration or omitted Created bypass.

After accepted three-event Movement lifecycle:

1. If no remaining event, return existing MovementLifecycle projection exactly as today.
2. Read **one** event via strict `CampaignCombatBreakdownCompletion.Replay(..., history.CopyRange(cursor,1))`. Require actual accepted completion (`Completion != null` and existing typed position/proof), increment cursor by1. If now at history end, return existing BreakdownCompletion projection. This preserves every old G2 cut and avoids recursive entry into successor admission.
3. Capture `predecessorEvents = history.CopyRange(0,cursor)` **ending at this accepted G2 event**. Never pass entire outer history to010A/009A admission. Save `selectionStart=cursor`.
4. Capture up to2 remaining events and call `CampaignCombatInheritedSelection.Replay(request, history.CreatedSpan, predecessorEvents, selectionEvents)`. This reader reconstructs trusted G2 through generic replay, which terminates at step2 above; no infinite recursion or synthetic state cache. Count partitions only proposed slice; strict local reader decides whether either event is valid.
5. Advance cursor by actual selection slice length. If at history end, return new Selection projection (after1 opened event or2 closed events). If tail remains, require actual `selection.SelectionClosed`; do not infer closure solely from count/eventType.
6. Pass **all** remaining suffix to `CampaignCombatInheritedNoAttack.Replay(request, history.CreatedSpan, predecessorEvents, selectionEvents, remainingSteps)`. Its≤6 bound rejects a seventh event before individual step work. Return new NoAttack projection at each accepted traversal cut1…6. Do not consume six and silently drop extra bytes.

No event-type dispatch needed after accepted G2: unique frozen family progression and strict readers suffice. Generic eventType/contractVersion may be diagnostic hints only; do not equate C3a combat-segment-opened1 with inherited v2 or repair it into v2. A misplaced extra G2 event will fail selection parser/transition; a pre-G2 selection event fails predecessor reader.

At zero successor events use existing G2 projection; at selection2 +zero traversal retain new Selection projection, not artificial traversal0. Local010A supports both initial controls, but canonical one-stream router returns a single family per actual event cut. No need invent a new event/family to distinguish identical history.

Complexity remains bounded.010A readers replay exact predecessors internally, so integration repeats a small G2 validation; preserve this authority boundary instead of introducing an unsafe prevalidated cache. If later performance work is needed, it requires separate evidence and dispatch, not this slice.

## New projections and cumulative receipt semantics

Proposed closed arms:

```csharp
internal sealed record Selection(CampaignCombatInheritedSelection.State State)
    : CampaignCombatHistoryProjection;
internal sealed record NoAttack(CampaignCombatInheritedNoAttack.State State)
    : CampaignCombatHistoryProjection;
```

For Selection: StateVersion/Prefix from local State; SequencePosition from `State.Boundary.Entry.SequencePosition`; Receipts = actual Entry.Receipts followed by local Selection.Receipts.

For NoAttack: StateVersion/Prefix from traversal State; SequencePosition = `State.Position`; Receipts = actual `State.Selection.Boundary.Entry.Receipts`, then `State.Selection.Receipts`, then traversal `State.Receipts`.

Build owned read-only cumulative array once from immutable accepted state. No sorting/deduplication, no command hash rewriting or synthetic receipt. Every receipt EventHash/StateVersion matches same-index retained event, actor/receipt/command hash preserved. New projection is derived read model, not a wire contract or caller cache.

**Do not merge cumulative receipts into local Controls.** Selection locally has0…2 receipts; traversal locally0…6; frozen nested Selection stays exactly stepIndex0/segmentClosedfalse even at terminal traversal6/closedtrue. Local Control byte/hash commitments remain those accepted in010A. Boundary remains actual G2 World/Weather/RNG/CP/cohesion/progress proof. Current outer projection position moves structurally; nested G2/selection position remains original Combat entry. Arrival at Reserve Release does not release units/reset ledgers/end stage.

## Acceptance and exact counts

Select four **terminal** actual G2 tuples from existing Movement enumerator: event count20/21 **and** final strict accepted Breakdown completion; actors Axis/Commonwealth with six/seven actual moves. Do not accidentally select seven-move partial history at count20. Append accepted two-selection/six-traversal events with real010A Apply calls.

- Full histories contain28 or29 events, authority29 or30. Four histories×8 new suffix cuts =32 new routed cuts; including their G2 boundaries gives36. If replaying every prefix from Created, total118 cut visits (two histories with29 cuts +two with30), not118 distinct histories; deduplicate/count separately if making a unique-history claim.
- At new cuts1/2 assert Selection projection and local Control exact hashes/lengths from inherited-selection fixture; cuts3…8 assert NoAttack projection and local Control hashes/lengths from inherited-no-attack fixture. Zero tail remains BreakdownCompletion. Exact suffix events match010A commitments; accepted010A's28+60=88 independent commitments remain cumulative parent evidence, not newly invented literals.
- Compute full event-prefix fold from independent retained Created creation prefix through **all** events using existing domain helper and compare every routed head. StateVersion=count+1, cumulative receipt count=count, ordinal StateVersions2…count+1 and every event hash match. Compare exact receipt concatenation to accepted family local ledgers, ensuring entry/selection receipts appear once.
- Compare routed local state to direct010A Replay for each suffix cut. Serialize local Control then read it using exact predecessor/selection/traversal slices, comparing restored routed head. No whole Snapshot12 claim follows fragment causal restore.
- Assert Created/event ownership and stable returned history copies; mutation of caller/source/result arrays cannot alter frozen state/head. Returned cumulative receipt collections immutable.
- Real World/RNG/CP/TOE/ammunition/Weather/end/progress evidence remains byte-identical to G2 Boundary. Selection remains empty with no decision/window, no attack/target-use/decline/publication.

Failure-sensitive negatives:

1. Missing/noncanonical/foreign Created; old prefix cuts and all predecessor gates unchanged. Selection before G2, missing/forged/reordered/duplicated G2, unsupported one/five-move G2 tails rejected rather than assumed empty.
2. Missing/reordered/duplicate selection/step, repeated local accepted event after close, extra seventh step, arbitrary trailing object, same tags wrong contract versions, stale versions/prefixes/actors, altered receipt/effect/disposition/from/to and rehashed forgery. No trailing bytes/events ignored.
3. Swap lawful six/seven/other-side suffixes at same-looking cut; branch-specific Boundary/receipt/prefix must reject. Actual Reaction forks cannot attach selection/traversal tail merely because their type/position resembles ordinary completion.
4. Attach each synthetic010B C3a trace (including no-candidate/pass and positive accepted-decline) to real G2, or present it as standalone after Created: reject. May use existing literal C3a fixture directly for negative evidence; never pass its own event.input as trusted live authority. There is no timed C3a generic projection or implicit Boundary extraction.
5. Regression guard: call009A `Admit` with exact G2 prefix succeeds, with new post-G2 full stream rejects as wrong predecessor family. Direct010A replay on valid suffix still succeeds, proving router does not recurse on complete suffix.
6. Existing top-level512/1MiB/16MiB capacity and owned-list behavior preserved; scoped new tests cover suffix2/6 bounds and nonempty extra tail. No need repeat every existing H0 scalar mutation.

## Snapshot12 and dependency regression gate

Leave `CampaignCombatInheritedSnapshotV12Codec` untouched. New Selection/NoAttack projections hit its existing unsupported default. Explicitly assert Serialize rejects each new family and Restore cannot accept an old G2 root against a longer new history, even with admissionEnabled false. Existing old G2 root with exact G2 history remains supported. Never flatten local controls into H4 root, forge an H0 family or silently discard accepted successor state.

Run focused new tests and justified predecessor MovementHistory/InheritedSteps/Identity regressions; root fullgate must retain H4 all368 vectors/286 distinct histories with exact bytes and fresh-disabled restore plus all Reaction fork evidence. Ordinary Core tests may serialize projections through their own closed test switches; old enumerators remain predecessor-only, so no fixture widening expected. If compile/runtime evidence reveals a new consumer requiring a fourth edit, report concrete finding before changing scope; do not add silent fallback.

TDD first new full-history Replay test should fail at old G2 max1 tail bound. After bounded router/models implementation, all32 new routed cuts plus negative cases pass; root verifies prior gates and three fresh review rounds. Scoped format and exact source hash handoff, all owned processes closed. No worker full suite/commits/push.

## Blockers and parent closure

No architecture blocker found. Main risk is recursive admission using wrong predecessor slice; explicit cursor cut and exact terminal-G2 early return prevent it. Second risk is claiming local receipts are cumulative; projections must concatenate immutable accepted ledgers without changing wire Controls. Existing Snapshot12 switch already fails closed, so no extra writer file required.

010B acceptance remains a **delivery dependency**, though timed C3a code is not a runtime dependency of actual010C route. Actual010A history remains the only newly admitted suffix.010B's independently trusted synthetic Boundary cannot establish complete positive provenance, so no positive creation-rooted history, all28-runtime completion, Prepared011 or host publication claim follows.

Parent010 closes only after accepted010A exact actual mechanism, accepted010B timed selected/empty/cancelled mechanism and accepted010C cumulative routing/cut proof, each with dev review/three independent rounds/full gates. Extended Snapshot12 composition remains separately undispatched; existing H4 InitialH scope stays explicit. Genuine live positive admission would require new provenance evidence and scope, not a permissive router branch.
