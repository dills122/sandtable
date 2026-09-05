# BRK progress review through Task 002

Review instance: 2 of 3 for implementation flow. Research/design loop remains closed at 3 of 3.

## Preliminary ledger — recorded before author explanation and prior report

Frozen scope verified: `a0475472513bf97aeac5f81b9691b58bc65324ee..2e9f9b8d53ce8ca203bdd3dd2c834bede0d8105d`, branch `codex/breakdown-adjudication-design`, 21 changed paths. User-owned context changes excluded. Author packet and prior review report not read at this point.

Independence limit: mandatory CCE `session_recall` exposed prior Ready verdict and passing-test totals; governing specs also state that verdict. Those claims remain unverified testimony at this stage. No implementation conversation inherited.

| Question | Preliminary evidence | Status |
| --- | --- | --- |
| Exact source table and arithmetic | Expanded 324-cell test oracle, six rational fractions, checked ceiling arithmetic, one-point 10% exception | No code defect identified |
| Dormancy and canonical artifact | Closed schema-2 codec; source/ruling provenance; no active manifest changes; frozen old hashes asserted | Appears consistent; execute focused tests |
| Pure eligibility | Exact raw threshold, predecessor profile/weather calculation, neutral Rainstorm column, checked-band ordering | No code defect identified |
| Contract/plan readiness | Six finite flow variants, pinned predecessor codecs, explicit coupled activation and historical fixture accounting | Inspect forced Reaction-stop interpretation and remaining fixture feasibility before verdict |
| Completion claims | Task 002 is Rules-only; campaign, RNG, profile certification, fixtures and activation remain pending | Completion scope appropriately bounded; retained evidence still to check |

## Findings

### P2 — Make CP-exhaustion stop rule explicitly phasing-only

[`BRK-REQ-004`, lines 119–125](../specs/breakdown-adjudication-v1.md#brk-req-004--route-and-stop-lifecycle) says a step exhausting the mover's CP records a forced stop in that same move event. Its wording includes a reacting mover. However, the exhaustive `reacting` transition at line 142 keeps every reacting step in `reacting` with an active route; only participant completion or System closure records its stop. The wire freeze likewise says `ReactingElementMoved 2` updates/creates a route (line 201), while an open stop requires an already-resolved participant and null active slot (lines 165–169).

Failing specification scenario: an active non-cohort battalion spends its final CP during Reaction. Applying the general forced-stop sentence requires a stop immediately; applying the finite transition/wire contract requires retaining the active route until explicit completion or closure. Implementers of Task 003 snapshot validation and Tasks 004/005 event factories can therefore encode different valid-next-state rules. Existing predecessor `CampaignReactionParticipantEventFactory.CreateMove` retains the active opportunity, and `CreateCompletion` separately clears/resolves it.

Smallest correction: qualify the same-event forced-stop sentence as applying to **phasing** steps, and explicitly retain participant completion/System closure for exhausted Reaction routes, consistent with the frozen state table and existing participant lifecycle. Carry a final-CP Reaction vector into BRK-AC-006. This is a bounded specification clarification, not a defect in dormant Rules code or a reason to reopen accepted research decisions. No runtime change or architectural pivot is required.

No other actionable findings; no P0/P1 defects found. Exact arithmetic, source normalization, canonical artifact rejection and current-version isolation are sound within Task 002 scope.

## Plan Review

Task 001 supplies ten requirements, twelve acceptance mappings, pinned predecessor codec identities, exact successor deltas, certification diagnostics and explicit historical-fixture obligations. Task 002 supplies the complete Rules table/arithmetic artifact and pure eligibility portions; campaign RNG, working-count mutation, conservation, disclosure and activation are correctly left pending. Single-unit `CalculateLoss` is a reusable arithmetic primitive; it stores no global single cohort slot and does not preclude Task 005's collection transaction.

Task 003 remains the right next task after the clarification above: dormant Content 6/Setup 6/World 6/Snapshot 11/creation, finite route-stop-lot records, sequence 4 and a structurally certified Truck fixture. Then shared BP/event accounting (004), stop/RNG/continuation authority (005), coherent public identity/disclosure activation (006), and checked Runner adoption (007). Full Ruleset 9 registration properly waits for sequence 4 and coupled activation. Whole-version-set activation rollback remains explicit.

Fixture/profile obligations are feasible at specification level: standalone Truck ordinary movement is explicitly added without widening combat-only Reaction triggers; non-cohort battalions can react in separate hexes without own combat aggregates above one. Positive public ZOC is correctly deferred because existing ZOC needs stacking greater than one. All 14 historical scenario files and 15 Reaction children are accounted for; 13 children require bounded successors, two positive-ZOC children stay historical/direct. Low-defense/HQ successors expressly avoid claiming isolated positive-ZOC predicate evidence. Future public loss, zero-loss, higher-band, fixed-lot and final-CP-trigger evidence remains required, not counted as passing today.

No missing future implementation is reported as a defect. No heavy pivot, new workstream or extra reviewer is needed.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| All 324 outcomes, six exact fractions, ceiling and one-point exception implemented | Production `CreateDefinition`/`CalculateLoss`, expanded oracle, independent research-to-golden comparison, focused tests | Confirmed | Task 002 numeric scope complete |
| Pure eligibility preserves exact threshold, weather and memory semantics | `EvaluateEligibility`, predecessor `SelectEffectiveCheckBand`/point-share validation, boundary tests | Confirmed | No malformed input silently becomes no-roll |
| Schema 2 is closed canonical authority; no active identity changes | Codec, typed/byte mutation tests, golden, Ruleset 8/schema-1 hash assertions and committed diff | Confirmed | Dormancy/compatibility claims supported |
| Four rulings prepared but not registered | `CreateRulings`, complete IDs/alternatives/source/acceptance assertions | Confirmed | Coupled registration remains properly deferred |
| Contract freeze is coherent for next campaign work | Specification state table and wire transition deltas | Partially contradicted | CP-exhaustion sentence needs bounded clarification above |
| Complete historical fixture migration accounting | Inventory and independently rerun freeze audit | Confirmed as obligations | Does not establish successor fixture execution |
| 72 focused tests pass | Reviewer execution | Confirmed | 72 passed, zero failed/skipped |
| RED 18 failures; full rebuilt gate has 1,288 tests and 48 boundary tests | Retained outcome evidence/progress, prior report; parent independently checked committed source hashes and retained build-binlog provenance | Not independently rerun | Treat as retained author evidence, not a second full gate |
| All seven implementation files match reviewed source and closeout adds only docs | Parent hash audit; reviewer `git diff --name-only 4d3acaf..2e9f9b8 -- src tests` | Confirmed with stated provenance | No production change since prior implementation review |

## Verification Performed

- Git branch, HEAD, dirty state and cumulative 21-path diff checked against frozen target. `4d3acaf..2e9f9b8` changes no source/test files. User-owned context files untouched; only this report added.
- `dotnet --version`: `10.0.400`. `global.json`, SDK-style test project and central build/packages confirm native MTP with xUnit. Run-tests/platform-detection skill instructions applied.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class 'Cna.Core.Tests.Rules.BreakdownOutcome*'`: **72 passed, zero failed, zero skipped**, escalated local IPC execution. Existing built output used; no source rebuild claimed.
- `python3 docs/research/verify-breakdown-outcomes.py`: **PASS**, 324 cells, complete/unique ranges, monotone columns, nine source probes, 606 bounded arithmetic combinations. Source transcription hash unchanged: `f63dc336364648acb52d20070bb1076205a36a40769f4391531ba65aaf94c401`.
- `python3 docs/research/verify-breakdown-contract-freeze.py`: **PASS**, 14 historical fixture hashes, 15 Reaction children (13 successor obligations/two deferred), 15 predecessor schema hashes, ten requirements/twelve acceptance mappings, 25 links. This checks inventory/hash/link structure, not every sentence's semantic consistency.
- Read-only Python comparison: **PASS**, all 324 source/golden `(band,coordinate,label)` triples, six fractions and inherited continuity fields. Canonical golden SHA-256 excluding terminal newline: `0459f4a9ebb4ece6237f789000d3ec4fcddbf7ee3d341012d81ce77fb6ef3e08`.
- `git diff --check a0475472513bf97aeac5f81b9691b58bc65324ee..2e9f9b8d53ce8ca203bdd3dd2c834bede0d8105d`: **PASS**.

## Open Questions And Residual Risks

Research source readings remain within the closed design loop; this review compares production/golden against its retained accepted transcription. Golden and tests share that transcription, not independent primary readings. No full gate, public campaign loss trajectory, fixture certification, RNG replay, privacy permutation or mixed-runtime activation check was newly executed here. The latter behaviors remain assigned to Tasks 003–007. Independent first-pass judgment was partially exposed to CCE's prior verdict/test summary as disclosed above; author rationale and prior report were read only after the preliminary ledger was written.

## Verdict

**Ready with non-blocking follow-ups** for completed BRK-TASK-001/002 progress. Task 002 code is ready; clarify the forced-stop sentence before downstream campaign validators/events turn competing readings into code. This verdict does not approve public Breakdown activation.

## Recommended Next Actions

Accept the bounded CP-exhaustion clarification, align the final-CP Reaction acceptance vector, then proceed with BRK-TASK-003 in the existing order. Preserve all current identities until Task 006. Implementation review budget is now 2 of 3; research/design budget remains closed at 3 of 3. No further review instance started or requested by this report.

## Implementation-task disposition

**Accept** the P2 finding. Direct inspection confirms that BRK-REQ-004's general same-event
forced-stop wording conflicts with the exhaustive reacting transition and wire stop invariant.
Clarify phasing-only forced stops and preserve explicit exhausted-Reaction completion/System
closure, with a final-CP Reaction acceptance vector, before Task 003 implementation. This review
does not apply that correction or change the implementation plan. Report retention is covered by
the owner's existing instruction to commit progress on the feature branch.

Follow-up applied after owner authorization: BRK-REQ-004 now limits same-event forced stops to
phasing steps and explicitly retains exhausted Reaction routes until completion/System closure.
BRK-AC-006 and the design acceptance matrix carry the final-CP Reaction vector. Numeric and
contract-freeze audits pass; runtime transition evidence remains assigned to Tasks 003/005.
