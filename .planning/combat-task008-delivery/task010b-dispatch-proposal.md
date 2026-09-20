# Task010B concrete dispatch proposal — research only

Baseline:010A frozen code2eda2ed, fullgate/review1 active. No010B implementation authorized until010A accepted and root dispatch. Only this proposal edited. No source/test/.NET/commit/agent/review activity. Canonical chain remains009A→009B→010A→010B→010C→011; parent010 closes only after all three children. This refines the accepted execution plan, not a new contract/profile.

## Decision and exact five paths

Faithful bounded slice is feasible, with separate models so timed machine and grammar remain reviewable:

1. New `src/Cna.Core/Campaigns/CampaignCombatSelectionStepsModels.cs`: immutable Boundary, ApplicableWeather, Timing, Window, fixed-field Command/Input, closed Effect arms, Control and Result.
2. New `src/Cna.Core/Campaigns/CampaignCombatSelectionSteps.cs`: fixed C3a context validation, trusted-boundary replay/apply, eight command kinds, six effect kinds, exact retry/no-op/lifecycle/timing semantics.
3. New `src/Cna.Core/Campaigns/CampaignCombatSelectionStepsCodec.cs`: exact writers, strict syntax/canonical readers, trusted whole-value readback, bounded CP-only World serialization mapping.
4. Existing `src/Cna.Core/Campaigns/CampaignCombatIdentityCodec.cs`: narrowly named internal syntax reuse for frozen external World/Authority/Position/Random/UnitKey; preserve all current009/010A behavior. No second51-object grammar or generalized schema framework.
5. New `tests/Cna.Core.Tests/Campaigns/CombatStepsTests.cs`: literal C3a proof, both-role mirrors, clocks/retries/provenance/shape/capacity/cut negatives.

This replaces preliminary csproj slot with Models.cs. Existing project already copies `combat-selection-steps-v1.json` at line107 and C2 authority fixture at65–66. Side projection fixture contains public projection commitments, not literal private mirrored steps; linking it would not supply independent private event goldens. No sixth file or schema/oracle/fixture edit anticipated.

Estimated machine size: one approximately200–300-line transition/admission implementation plus owned replay wrappers, approximately100–160-line models, approximately200–350-line codec plus named existing-grammar bridge, and several focused test groups. These are estimates, not a reason to compress unreadable logic. If exact external grammar reuse requires a broad World refactor, or any production adapter demands real positive-history admission, stop and report pivot rather than widen this dispatch. No such pivot required for the dormant frozen mechanism.

## Governing sources

- `docs/design/combat-cycle-implementation-plan.md:859` row010 and accepted execution refinement around878: provisional selection/RBA, empty/cancelled six-step flow; Prepared belongs011.
- `docs/specs/combat-selection-steps-v1.md:21–59` trusted Boundary and fixed subset;66–88 canonical framing;92–146 command/admission/time semantics;148–175 effects/terminal controls.
- `docs/specs/combat-selection-steps-v1.schema.json:3` all16 object descriptors, effectTags and limits; this is wire contract1.
- `docs/specs/verify-combat-selection-steps-v1.py:79` boundary;119 candidate;137 initial;155 window;163 active_window;169 admitted_timing;173 transition;279 read_event;286 replay;296 read_control. Frozen oracle is exact transition reference, not runtime dependency.
- `docs/specs/verify-combat-authority-envelope-v1.py:144–161` normative Context/request; `docs/specs/fixtures/combat-authority-envelope-v1.json` request,creationBinding,goldens.request/created.
- `docs/specs/verify-combat-rules-inputs-v1.py:162–181` timing creation/validation and213–221 clock_gate; `combat-rules-inputs-v1.schema.json` Timing and UTC limits.
- `docs/specs/verify-combat-side-projection-v1.py:388–416` both-side mirror transformation; `combat-side-projection-v1.md:49–61` corrected source profile. `steps-clock-v2` is a source-profile label, not selection wire-v2.

## Boundary authentication and provenance API

Boundary is independently trusted input, not an event, signature, certificate or proof of complete history. Publicly callable internal Core API should say `TrustedBoundary` in method names/docs, avoid `AdmitActual` or claim that matching a hash authenticates history. Caller remains responsible for authenticated seat/controller, complete predecessor history, completed Breakdown, applicable Weather, movement/CP provenance, first-side order and no Reaction. There is currently **no actual positive-history adapter** satisfying that precondition; real G2 remains010A's empty arm. Tests explicitly name synthetic C3a boundary probes.

Concrete API (nested aliases below refer to new immutable models):

```csharp
StepsControl ReplayTrustedBoundary(CampaignCombatCreationRequest request,
    ReadOnlySpan<byte> retainedCreated, StepsBoundary trustedBoundary,
    IReadOnlyList<StepsInput> trustedAcceptedInputs, IReadOnlyList<byte[]> acceptedEvents);
StepsResult ApplyTrustedBoundary(CampaignCombatCreationRequest request,
    ReadOnlySpan<byte> retainedCreated, StepsBoundary trustedBoundary,
    IReadOnlyList<StepsInput> trustedAcceptedInputs, IReadOnlyList<byte[]> acceptedEvents,
    StepsInput trustedInput);
StepsBoundary ReadBoundary(ReadOnlySpan<byte> bytes,
    CampaignCombatCreationRequest request, ReadOnlySpan<byte> retainedCreated,
    StepsBoundary independentlyTrustedBoundary);
StepsControl ReadControl(ReadOnlySpan<byte> bytes,
    CampaignCombatCreationRequest request, ReadOnlySpan<byte> retainedCreated,
    StepsBoundary independentlyTrustedBoundary, IReadOnlyList<StepsInput> trustedAcceptedInputs,
    IReadOnlyList<byte[]> acceptedEvents);
```

Serializers operate on validated immutable values; Boundary writer receives validated creation context or an owned initial baseline in internal state. New private transition receives validated Boundary/derived Candidate plus replayed Control, never caller Control authority. Result has immutable current Control, disposition Accepted/Duplicate/NoOp, optional owned EventBytes and optional ReceiptId. Duplicate returns original retained bytes; NoOp has no receipt/event. Rejection throws consistent private contract exception, not success/no-op. No publication flag/host behavior added.

Replay captures independently retained Created before adversarial list Count/index access (010A regression lesson), bounds both lists≤16 and equal lengths before index, owns bytes and immutable typed inputs, then validates raw event shape/canonical grammar before trusted semantic comparison. Each event must equal transition output for separately supplied trusted input. Duplicated/no-op input in accepted history rejects because transition yields no new event. No-op attempts never become invented persisted records. Keep trusted inputs separate from untrusted event.input: **do not build trustedAcceptedInputs by decoding the same bytes under review**. Fixture inputs are explicit independent test data; future authenticated admission producer is external to010B.

Readers first reject hostile bytes by whole-root bounds, transitive closed shape/types and canonical spelling. Only then inspect trusted request/Created/Boundary/history. Boundary reader validates independently trusted typed Boundary and compares complete canonical bytes, returning trusted immutable value; no untrusted JSON cache is promoted to authority. Control reader replays trusted tuple and compares entire Control. This permits no arbitrary-world JSON reader and avoids changing initial-World semantics.

### Exact retained C2 guard

`boundary()` always uses default `env.Context().request()` (oracle79–118), so C3a must not broaden to every009B-certifiable request. Pin named private constant:

```text
creation.dc1c1bff9db6122758ab2b06360ba2131231fe2b63871780e416c8fd6ba4490b
```

Source is authority fixture `creationBinding`, recomputed by existing typed `CampaignCombatCreationRequest` from complete canonical request, not loaded from fixture at runtime. Corresponding normative request:

- campaign `rules-lab.combat-creation.1`, seed0, cursor0, `sandtable.sha256-counter.v1`;
- Rules `8af256c6c2bbf71cb72ea7e29922db9c4c897c2535121a68a6849bbd06e0bd18`;
- Setup `rules-lab.combat.close-assault.v1`, hash `sha256:22495e26528c2f4db8335d3b4041194aef26295f30aab0e4e6f384ff17069848`;
- Content `rules-lab.content.close-assault.v1`, hash `sha256:ee4fde9638ceb61ec08612fe32f9ac81db05572aac5ca20941e402d2fed25847`, scenario `close-assault-positive-v1`;
- Config hash `sha256:3de30c451ba7e81d4fdde6493e07d4d06110209a89035d9e59bba738f0aeba34`.

Checking derived full creationBinding pins this entire request; still call `CampaignCreatedV11Serializer.Deserialize(retainedCreated, independentlyTrustedRequest)` and verify boundary.creationBinding/cycle fields against that request. Do not replace Created with regeneration or treat a caller string as a valid binding. Explicit negative requests change campaign, seed, Setup/config—even self-consistent newly generated Created must not expand C3a scope. Tests verify constant from frozen request independently; no production fixture path dependency.

Then validate Boundary: contract1; complete cycle identity, stage1/turn1/ordinal1/first relative slot, firstActingSide==actingSide; openedAuthorityVersion≤priorVersion with signed64 bounds; cycle original OpeningPrefix preserved separately from priorPrefix; exact catalog Position Determination with resolved side; idle Breakdown and null Reaction. Validate Random contract1/algorithm/exact request seed; nextByteCursor is retained UInt64, not reset0. Weather exact stage/turn and one of normal/hot/sandstorm/rainstorm; receipt remains independently trusted opaque hash. CompletedBreakdownReceipt similarly independent trusted hash. No literal synthetic prefix/weather/breakdown hash is promoted to actual provenance.

Invoke009B `CampaignCombatCertification.CertifyInitialProfileFacts` (`CampaignCombatCertification.cs:59–100`) with retained Created, typed current World/cycle/firstSide/applicable Weather. It validates exact initial inventory/positions/provenance apart from integer CP0…10, proves full result/retreat/custody support, returns zero/one derived Candidate. No duplicated Resolve/support certification, caller Candidate list, CP reset, synthetic actual-history claim or live opportunity ID. Boundary validation also handles candidates absent from non-Normal Weather or CP ceilings; unsupported World rejects rather than becoming empty.

## Models, codec and primitive reuse

Use existing `CampaignCombatCycleAuthority`, `CampaignWorldSnapshotV7`, `RandomStreamState`, `LandSequencePosition`, `LandSide`, `CampaignCombatCandidate`/Participant/UnitKey and `CampaignOpeningPreambleActor`/Receipt. Receipt.CommandHash deliberately hashes Command only here;010A's Input hash must not leak into this family. No new Opportunity model;009B opportunity-v2 requires later authenticated Base2 at011.

All nullable wire fields explicitly emitted; read-only arrays own copies. Layout exactly schema:

- Boundary: contractVersion,creationBinding,cycle,firstActingSide,priorVersion,priorPrefix,completedBreakdownReceipt,position,world,randomState,weather,breakdownFlow,reactionWindow.
- Command: contractVersion,kind,segmentId,decisionId?,fromPositionId?,expectedPriorVersion?,choice?,candidate?,participant?. One fixed typed record with per-kind permitted-field table.
- Input: command,actor,admittedAt?,clockAvailable.
- Timing1: contractVersion,configHash,kind,decisionBudgetMilliseconds,openedAtUnixMilliseconds,deadlineUnixMilliseconds,highWaterUnixMilliseconds. Window: decisionId,owner,timing.
- Control: contractVersion,boundaryHash,segmentId,stateVersion,prefix,stepIndex,selectionOutcome,selection?,selectionReceiptId?,declineReceiptId?,cancellationReceiptId?,selectionWindow?,rbaWindow?,stepReceipts[],receipts[],closed.
- Event: contractVersion,eventType,campaignId,rulesetHash,configurationHash,cycleId,segmentId,priorVersion,stateVersion,priorPrefix,input,effect,receiptId.
- Six closed effects: Open(boundaryHash,window?); Selection(outcome,candidate?,timing?); RbaOpen(selectionReceiptId,window); Decline(selectionReceiptId,participant,timing); Cancel(selectionReceiptId,timing?); Step(fromPositionId,toPositionId,previousStepReceiptId,dispositionReceiptId,proofKind), all leading kind.

Reuse `CampaignCombatReserveCompletionCodec.WriteCycle/CycleId` (132/108), `CampaignV11CanonicalCodec.WritePosition`, `CampaignSnapshotSerializer.WriteRandomState`, `CapabilityPointAmountCodec`, typed Candidate serializer, and `CampaignOpeningPreambleCodec.Hash/HashWithDomain/EventPrefix`. Check exact Authority tuple identity rather than hash of Authority JSON. Segment domain `sandtable.combat.segment.v1`, receipt `sandtable.combat.step-receipt.v1`, IDs seg./cmb.; decisions suffix .selection/.rba. Every fresh event increments authority once.

No current Core Timing implementation found by CCE and exact symbol search; add the small frozen Timing1 helper inside engine/models, using existing `CombatDecisionConfiguration.Windows`. UTC range0…253402300799999, addition checked before opening. Seven-window config remains existing validated object; use selection/rba budgets, not literal30000 in production.

External syntax reuse detail: named helper in IdentityCodec should whitelist World/Authority/Position/Random/UnitKey, delegate its existing frozen type walker, and explicitly preserve C3a array order recursively. Existing inherited walker enforces keyed order; blindly calling it is not equivalent to C3a `canonical()` (oracle52–56 preserves **all** arrays). Add optional private recursive preserve-order mode only for this named bridge; default remains unchanged for009A/010A. C3a semantic World/participant validation later establishes admitted fixed ordering. Local Candidate/Participant/Timing descriptors are small frozen syntax, not duplicate World hierarchy. Audit all16 descriptors and transitive primitive compatibility against frozen schemas before freeze, especially UInt64, nullable arms and UTC vs arbitrary int64.

World writer:009B has already proved initial inventory except current CP. Serialize retained typed initial baseline via `CampaignWorldV7InitialCodec.Serialize` then change only the two typed CP numerator/denominator slots using validated current elements, and emit canonical bytes in original property/array order. This is internal serialization mapping after full typed validation, not acceptance of caller JSON authority. Alternatively a compact typed World writer using existing origin/element helpers is acceptable only within scoped codec. Do not call initial World reader on CP>0, reset current World, build fake Movement state, or copy/refactor full World codec.

## Exact transition order and matrix

Before transition: typed Input (including UTC bounds), contract1/known kind, segment, permitted nullable fields, actor category. Canonical **Command** hash then duplicate lookup; require recorded actor equal. Retry ignores new admittedAt/clockAvailable after their primitive validation, even after expiry/closure; state/receipt/retained event unchanged. Do not hash timing into duplicate identity.

Next derive active window: pending selection; or selected+RBA window+no decline. Exact pre-RBA unavailable exception: selected, stepIndex2, no RBA window, .rba decision. Other stale/missing timer/unavailable IDs return no-op before closed/version/capacity checks. Then fresh mutation checks not closed, receipts<16, version<long.MaxValue. Structural/open kinds require exact expectedPriorVersion; complete/open-rba require exact current fromPosition. Complete/close-empty require null admittedAt and clockAvailable=true.

| Command | Prerequisites | Accepted effect/result |
|---|---|---|
| open-segment | unopened/no receipts; system; positive+reliable requires valid time; absent candidate requires null time | derived candidate+reliable opens phasing Window and pending; otherwise system-no-selection/null window. Unavailable opening never invents deadline. |
| close-empty-selection | system-no-selection/null window; system structural clock | no-selection, null candidate/timing; retain selection receipt. |
| choose-selection | pending, exact decision/owner; clock before deadline; selected Candidate equals derived Candidate, pass Candidate null | selection-closed selected or no-selection; update only high-water in retained window. |
| open-rba | selected at step2, no RBA window; system reliable valid time ≥selection high-water | defender Window; retain existing selection/position and link selection receipt. |
| decline-rba | selected at step2, active exact RBA decision, defender actor/original UnitKey; clock before deadline | rba-declined; retain decline receipt and updated RBA timing, no move/resource effect. |
| expire-window | system, exact active decision; early clock gate means no-op | equality/later or unavailable gate closes selection no-selection or cancels RBA; never manufactures decline. |
| controller-unavailable | system exact active decision, or exact pre-opening RBA exception | close selection/cancel RBA; exception has null timing/window; live window retains timing with high-water rule. This trusted command may cancel even if supplied time itself is valid. |
| complete-step | selected/no-selection/cancelled, exact current step/version; selected RBA requires actual decline | six no-attack steps; selected proof sequence no-gun-positions,no-barrage-work,accepted-decline then stop atFA. Positive FA completion rejects pending011. |

Clock gate: unavailable if !clockAvailable, admittedAt null, or admittedAt<retained high-water; otherwise expired if≥deadline, else before. Player choices reject unavailable/expired. System expire/unavailable fallback accepts exact live wait under oracle rules; a regressed timer therefore falls back, not a repaired early no-op. Accepted fallback/choice timing sets highWater=max(old,non-null now), leaving opening/deadline unchanged; null retains old. This is retention semantics, not wall-clock clamping. Opening reliably missing/overflowing time rejects. Separate selection/RBA Windows remain retained after closing.

Step previous receipt is opening receipt for first step, then immediately prior step. Disposition priority: cancellation receipt; else decline for step≥2; else selection receipt. Use six catalog Combat IDs and same-slot Reserve Release; no extra segment event. Cancelled paths retain original provisional Candidate and selection receipt plus cancellation, with no decline. Full Boundary, RNG/resources/World/attack/target-use history never changes.

## Required evidence and acceptance

1. Two literal Boundary UTF8 values plus bytes/hash. Five literal traces: no-candidate8, voluntary-pass8, selection-expiry8, accepted-decline7, rba-expiry10 =41 exact events;41 post-event restores plus5 starts;5 literal final Controls. Every emitted event compares literal bytes/hash/count. Intermediate controls reconstruct exact causal bytes and readback; do not call these stored literal controls because fixture stores only finals. Exact retries at every applicable later cut, including changed valid timing/availability on same Command/actor, return original bytes.
2. Both-side supplemental probes follow `mirror_steps` exactly: five source cases×2 sides=10 traces/82events/92cuts including starts. Axis duplicate mechanism coverage is not41 additional independent literals. Commonwealth mirror swaps current CP assignments, cycle actingSide/firstSide/position activeSide, rewrites derived segment/decision/version/candidate/defender identity and preserves original times. Assert role ownership, target geometry, receipt/prefix derivation, resources frozen and terminal step/closure. Side projection's two audiences do not double private trace counts.
3. Boundary provenance negatives: different valid C2 request/config/seed/Setup, invalid/missing Created, foreign creation/cycle/firstSide/prefix, later scope, wrong position/flow/Reaction, moved/altered inventory, illegal or fractional CP, Random seed/algorithm, Weather scope/kind; non-Normal and exceeded voluntary ceilings still derive empty. Do not reject legal current CP just because nonzero or cursor just because nonzero. Use009B's exhaustive support evidence rather than duplicate Resolve table suite.
4. Time matrix: deadline−1/equal/+1; null/regression/unavailable; UTC max/deadline overflow; before/after RBA window opening; stale selection timer during RBA and after decline/closure; early timer does not advance high-water; controller-unavailable at reliable time still follows trusted command; changed consumed command rejects; wrong retry actor rejects.
5. Strict grammar before trusted context: unknown/missing/duplicate/nullable/type/escaped/order/float/BOM/truncated/oversize/depth arrays, unknown effect tags, nested World/Timing/Candidate. Sentry trusted lists/context; canonical bytes must reach semantics. Arrays remain ordered. Command unused fields must null. Event.input cannot replace separately supplied trusted input; altered actor/time/candidate/effect/head/recomputed receipt rejects. Equal hash fields alone never establish authority.
6. Lifecycle/capacity: missing/reordered/duplicate/no-op accepted events, count mismatch,16 cap before indexing, version overflow, missing selection/decline/previous-step, wrong from/to, pending RBA advance, positiveFA completion, postclose mutation. Whole value1MiB/depth32/arrays512; reject before emission, never truncate. Owned input/Created/event/control/result buffers survive hostile caller mutation. Source-state/Boundary equality on rejected/no-op/retry calls.
7. Retain cumulative005 result certification,006–009 creation/actual-history proof and accepted010A four real20/21+2+6 histories as parent evidence.010B adds no new actual positive creation-rooted path.010C integrates only actual010A families later. No full Snapshot12 extension, Prepared/seal, resource commitment or host/publication claim.

TDD first RED before production behavior; focused nativeMTP `--project` / `*CombatStepsTests`, unique binlogs, scoped format, source SHA manifest, all processes closed before root full gates/dev review/three independent rounds. Existing Identity/InheritedSteps regressions justified by shared grammar bridge. No full suite/commits/push by worker.

## Blockers and limits

No semantic contradiction or required architectural pivot found for the dormant mechanism. Fixed request pin is explicit frozen C3a scope;009B pure support certification alone is broader and cannot replace it. C3a old Timing1 high-water is intentional; current Round2 opening-floor policy belongs011 and must not overwrite these bytes. Main implementation risks are authority naming/accidental event-derived trusted inputs, array-order drift in syntax reuse, command-vs-input duplicate hash, and reliable-vs-unavailable timer behavior. Address with explicit API docs and failure-sensitive tests above.

Actual positive-history authentication remains an external unavailable capability; no API wrapper, source hash or synthetic Boundary can supply it. It is a declared later integration gate, not a blocker to the exact frozen synthetic mechanism authorized by parent010 refinement. If root instead requires live actual positive admission in010B, this proposal is insufficient and implementation must stop for genuine scope revision.
