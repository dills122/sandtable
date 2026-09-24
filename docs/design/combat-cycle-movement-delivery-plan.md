# Task018 Movement delivery plan

Baseline: merged PR141, `54029a502859a270cea0846e99fc397ea9b00d54`, all PR checks successful.
User approved dependency reconciliation and definition of smallest implementation slice.
Task018A locally accepted: focused49/full2,397/Boundary81, clean build/format, frozen oracle and independent review. Task018B locally accepted: focused66/full2,407/Boundary81, clean build/format, frozen oracle and independent review. Later children remain planned.

## Dependency reconciliation

Parent017 remains open for later-II and consumed offensive history. Requiring its full closure
before any018 work would block prerequisites needed to produce that history. Split mechanism
acceptance from actual campaign admission; do not waive any parent acceptance criterion.

1. **018A — relation-aware break-off assessment:** reuse accepted spending and relationship types.
2. **018B — atomic ordinary Movement at a trusted boundary:** native command/event/replay and
   World projection. Bind independently retained inputs; label isolated evidence explicitly.
3. **019 mechanism and inherited adapters:** use accepted018 rules for Movement witnesses;
   implement supported armed continuation (frozen3j) and guarded repeat/finish (3k) as separately
   bounded children. Missing support must reject, never mean no legal continuation.
4. **018 campaign integration:** actual repeat authorizes released-I Movement (3l), completion
   and pending-exception expiry (3m). Reuse retained Movement-end proximity evidence.
5. **017 remaining provenance:** real conversion, repeat, Movement and offensive history supply
   later-II/consumed admission. Bound and reconcile those paths before claiming parent closure.

The original018→019 dependency means accepted movement rules/event mechanism, not completion of
future repeat-dependent campaign traces. Parent018/019 remain open until their full criteria pass.
No public action, Snapshot/history dispatcher or simulator activation belongs to these children.

## First slice: 018A

Objective: derive deterministic affected relationship set and total voluntary movement cost from
original unit identity and retained relationships, then apply existing spending semantics through
one reusable internal rule. This rule supplies later execution and continuation assessment.

Exact primary manifest (four paths):

- `src/Cna.Core/Campaigns/CampaignCombatCycleMovementRules.cs` — new internal immutable assessment
  and pure rule; keep small result types in same file. Derive affected pairs and maximum break-off
  cost; delegate CP ceiling and immediate incremental DP charging to `CampaignCombatSpending`.
- `tests/Cna.Core.Tests/Campaigns/CombatCycleMovementRulesTests.cs` — focused literal and rejection
  cases, independent expected arithmetic and prior-input preservation.
- `docs/design/combat-cycle-movement-delivery-plan.md` — retained scope/evidence.
- `docs/design/combat-cycle-implementation-plan.md` — root status and parent acceptance mapping.

Existing `CampaignCombatSpending.cs`, `CampaignCombatObligations.cs` and frozen schemas/fixtures
are reuse-only. If implementation requires changing their contracts or exceeding this manifest,
revise scope before edits rather than silently expanding. Administrative README/roadmap status
updates do not add runtime behavior.

Acceptance criteria:

1. Derive active membership by complete original UnitKey, never proximity. Validate creation/scope,
   duplicate identities and capacity; return exact affected pairs in canonical relation-ID order.
   Contact2/Engaged4 overlap or multiple counterparts charges maximum once; inactive/unrelated
   pairs add zero. Last-counterpart and unbound-arrival cases preserve identity semantics.
2. Add independently admitted terrain cost to maximum break-off cost and reuse existing cumulative
   spending/DP rule. Literal Clear2 cases: spent5+4+2=11/DP1; spent9+4+2=15/DP5; spent10+6 rejects.
   Cover ordinary floor(3*CPA/2), released-I CPA, released-II floor(CPA/2), CPA9, prior mandatory
   overspend and arithmetic overflow. Synthetic Reserve ceiling probes do not prove release rights.
3. Rejection leaves all inputs unchanged; result owns retained collections. No event emission,
   World/location mutation, relationship ending receipt, Movement completion, proximity override,
   repeat permission or public admission. Assessment alone never authorizes a move.

This slice does not duplicate terrain legality, custody/guard checks, topology, ZOC or Weather
admission. Callers must prove those before using the rule. Exact admission belongs to018B and
subsequent actual-history adapters, with unsupported profiles rejected explicitly.

## Verification and checkpoints

Start with compiling failing tests for missing relation-aware assessment. Match independent
literal expectations and frozen Python arithmetic/membership behavior; do not regenerate fixtures.
Run focused rules and existing CombatWorld spending tests, full solution build/test, Boundary gate,
format and independent review before accepting018A. Use repository native MTP commands and unique
binlogs according to run-tests/binlog-generation skills when executing .NET checks.

Planning checks: `git diff --check`; local links; existing
`python3 -B docs/specs/verify-combat-ordinary-movement-v1.py`. No .NET test claim for docs-only plan.

018B must receive its own exact <=5-primary-file manifest before code. Historical D2a isolated
fixture derives C3c World; current accepted source is Result2/World7. Do not force old IDs into new
history or claim direct parity without auditing that boundary. Prefer smallest compatible rule
slice first; retain16 bases/20 events/16 final-state vectors as historical contract evidence only
until native boundary compatibility is demonstrated.

After018B and each019 child, require cumulative replay, original-byte retries, stale/foreign input,
re-signed event/cache tampering, atomic World/resource preservation and exact successor checks.
After actual repeat/Movement integration, reconcile all parent017–019 acceptance criteria before
moving to public020–021. No blanket full-cycle completion from isolated probes.

## Risks and open work

- Circular coarse dependencies: resolved by explicit child order above; parent closure unchanged.
- Cost drift: one relation assessment plus existing spending implementation shared by later callers.
- Historic/current lineage mismatch:018B scope gate, not permission to change frozen contracts.
- Reserve ceiling mistaken for authority: pure018A probe boundary explicit; actual history deferred.
- Later children remain sizing work, not preapproved manifests or completed implementations.

Planning validation passed: frozen ordinary-Movement oracle (8 cases/both sides,36 cuts,486
mutations,120 raw rejections,630 arithmetic coordinates,14 atomic/overflow guards), new plan
links and `git diff --check`. These are existing contract checks, not018 runtime test evidence.

## Task018A implementation

`CampaignCombatCycleMovementRules.Assess` returns owned immutable affected memberships, terrain,
break-off and total cost. Full UnitKey equality selects active pairs; canonical relation-ID ordering
and maximum-cost precedence retain original receipt/endpoints. It accepts a separately admitted
current-stage ledger only: foreign creation, wrong scope, duplicate IDs, nulls and >512 records
reject, including unrelated invalid records. Historical ledger filtering/admission remains a caller
obligation; no old relationship is silently charged as current.

`AssessAndCharge` re-derives assessment from operational ledger scope and delegates to existing
`CampaignCombatSpending.ChargeOrdinary`. Result is provisional immutable state and Cause history;
caller supplies receipt identity and independently authenticated original unit/operational state.
No move, ending receipt or World mutation occurs. Pure `Assess` needs no receipt and supports later
continuation checks without generating hypothetical events. Both terrain1 research probes and
terrain2 actual Clear cost accepted; terrain legality remains external.

Initial RED: one compiled test failed at missing assessment. Expanded RED:19 compiled cases failed
against stubs after fixing public-test/internal-enum signature accessibility. GREEN:48 new/existing
World tests passed. Added final full-key opposite-side identity test; final focused49/full2,397/Boundary81 passed, with clean build/format and frozen oracle.

Independent review1 of3: Ready with non-blocking follow-ups, no actionable findings; full-suite
condition satisfied. Reviewed source/test hashes unchanged. [Evidence](../../.planning/combat-cycle-movement/evidence.md)
and [review](../../.planning/combat-cycle-movement/review-report.md) retain exact checks and limits.
Task018B has its own bounded native-event/provenance manifest below; parent018 remains open.

## Task018B implementation

Exact five-primary-file manifest retained in canonical implementation plan before coding:
World7, Movement engine, Movement codec, focused tests and canonical plan. Current Result2
source replay authenticates all32 accepted owner-selected settled contexts; synthetic ordinal2
`isolated-next-movement` remains explicit. Historical D2a Result1 bytes are not native parity evidence.

Selected nonmotorized independent CPA10 singleton infantry moves across adjacent unoccupied
featureless Clear edges at cost2 plus maximum active Contact2/Engaged4 break-off. Atomic
projection updates element/representation location, cumulative CP/DP, ended relationships and
causes while retaining other resources, guards and duties. Later moves preserve spent allowance
and do not recharge ended relationships. Event-derived receipts, exact full-history reconstruction,
original-byte retries and source-bound base/cache/state readers reject stale or forged input.

World7 gains only a computed successor; ordinary constructor and generic readers stay strict.
Movement readback reconstructs typed World through its own authenticated replay. No public action
or Snapshot routing is added.018B does not accept actual repeated Movement or reserve-release rights.
Next dependency is019 guarded continuation/finish/repeat, followed by018 integrated admission,
then remaining017 later-II/consumed lineage. All parent acceptance criteria remain open.

Validation and independent review: [Task018B evidence](../../.planning/combat-cycle-movement/task018b-evidence.md).

## Task019A continuation assessment

Implemented pure `CampaignCombatContinuation.AssessTrustedBoundary` for the frozen
exhausted-ammunition profile. Input projections must already be independently admitted:
Content7, cycle, completed Release/World7 and prior Movement-end evidence. This helper checks
coverage/scope and computes witnesses; it does not authenticate receipts or event history.

All original own units must be represented in completed Release, and Movement-end evidence
covers both original units canonically. Ordinary eligibility retains end-distance and prior
exclusions even when current positions change. Pending same-scope next-ordinal release exceptions
waive proximity only; Reserve-I/II ceilings and occupied/guarded destinations still apply.
Adjacent free Clear2 witnesses reuse018A maximum relationship cost and provisional spending,
preserving cumulative CP and incremental DP. Assessment owns output and mutates no input.

Armed own units, unsupported Weather/terrain/composition, incomplete Release/settlement, malformed
proof/history/ledger and arithmetic/capacity faults reject. Empty witness lists represent only
supported cases without eligible/affordable/free moves. No repeat/finish or progress decision
is made. Released-I/II tests are isolated ledger probes, not new campaign lineage acceptance.

Exact four-primary-file manifest: continuation engine/models, focused tests, canonical plan and
this delivery plan. Native source/progress adapters, armed3j support, guarded3k lifecycle and
actual repeated Movement remain following children; parent017–019 and public activation open.
Validation/review: [Task019A evidence](../../.planning/combat-cycle-movement/task019a-evidence.md).

Task019A locally accepted:62 focused/2,429 full/81 Boundary tests,0 skipped; clean build/format,
frozen cycle-control oracle and fresh Ready review1of3. Source/test hashes unchanged at closeout.
