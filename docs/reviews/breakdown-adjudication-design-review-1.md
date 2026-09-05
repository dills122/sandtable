# Breakdown adjudication design independent review 1

Review instance: 1 of 3, separate BRK-RSH-002 research/design delivery.

Target: `codex/breakdown-adjudication-design`, HEAD/base `0512ec2db00fa2b695e911e2e48797c8dc11aa42`, explicit working-tree scope from `/tmp/brk-review-1/bootstrap.md`. Decision-packet readiness only; no production approval or implementation readiness claim.

## Blind preliminary ledger

Recorded before reading `/tmp/brk-review-1/author.md`. Primary Land pp. 33–34 and Common Charts p. 7 inspected visually before proposed packet; September errata 21.12 checked as text. Repository instructions and independent-review skill read. CCE used for retrieval and memory; its compressed results could not be expanded (`Chunk not found`), so exact located review targets and implementation symbols read directly.

| Item | Independent evidence | Preliminary assessment |
| --- | --- | --- |
| Numerical surface | Common Charts p. 7 compared against all nine JSON bound rows, including null zero-percent rows for 61–70 and 71+ | Matches visible source; checker passes 324 cells, nine probes and 606 loss bounds |
| Arithmetic decision | Land 21.34 example and 21.35; proposal distinguishes 1/3 from 33/100 at 100 points | Concrete owner choice, accurately marked proposed |
| Accounting and RNG | `CampaignElementMovedV2Factory.ProjectMoveForAuthority`, `CampaignReactionParticipantEventFactory.CreateMove`, `Cna1979Breakdown.GetWeatherColumnShift`, `SandtableRandom.RollD6` | Claimed BP preservation, shared calculation, Rainstorm exception and variable cursor consumption confirmed |
| Origin-placement timing | Land 21.41 tests where movement started; design lines 51–68 record route origin and defer phasing resolution until after Reaction; lines 108–110 require authoritative placement detection | Potential missing frozen start-time threat facts; later enemy movement can change predicate |
| Hidden unsupported placement | Design lines 89–92 fail with no event; lines 108–110 predicate uses hidden enemy facts; lines 120–122 and AC-009 promise player transcript invariance | Potential observable progress/failure channel; requires explicit admission or disclosure boundary |
| Delivery scope | Seven tasks begin with owner decisions and exact contract freeze; no production source changed | Appropriate staged research/design scope; pending schema details alone are not findings |

## Findings

### P2 — Reconcile hidden placement rejection with public progress guarantees

Evidence: `docs/design/breakdown-adjudication-v1.md:89`–92 rejects unsupported placement with no event or World change; lines 108–110 determine support using authoritative hidden enemy facts. Lines 120–122 and `BRK-AC-009` (line 172) require player transcript invariance under hidden placement-input permutations. `BRK-DEC-007` in `docs/research/breakdown-adjudication-spike.md:101` proposes this dynamic boundary without describing its observable consequence.

Failing scenario: two otherwise player-equivalent stops differ only in hidden enemy facts that trigger 21.41. Ordinary-placement world emits resolution and resumes; exception world emits nothing and Runner reports unsupported/failure. Number and availability of subsequent player actions disclose whether hidden placement predicate held. One atomic event per supported stop hides group count within that event, but cannot hide successful progression versus permanent rejection.

Smallest correction: state an enforceable admission rule that makes supported/unsupported classification invariant across player-equivalent worlds, such as a declared initial scenario capability boundary that structurally excludes qualifying enemy contexts; alternatively present an explicit owner disclosure decision and revise privacy claims/tests accordingly. Keep authoritative detailed unsupported reasons in trusted evidence. Packet needs this consequence resolved or made a concrete owner choice before DEC-007 is decision-ready. No new implementation workstream is required.

### P2 — Specify start-time placement evidence before deferred checks

Evidence: Land 21.41, printed p. 34, conditions origin placement on where the unit **started** movement relative to qualifying enemies and intervening blockers. Design lines 51–68 retain route-origin location and defer phasing resolution until Reaction closes; lines 108–110 specify authoritative geometry/enemy inputs but no evaluation time or retained start-time predicate. Task 003/AC-008 negatives currently do not name this temporal case.

Failing scenario: phasing route begins with a qualifying enemy within two hexes; that enemy reacts away before deferred Breakdown resolution. Re-evaluating current enemy geometry at check time incorrectly admits ordinary placement. Reverse movement can reject a route whose start was outside the exception. Origin location alone cannot reconstruct beginning-of-route enemy positions and friendly blockers.

Smallest correction: require immutable start-time placement eligibility/provenance captured before the route's first move, retained through every Reaction interruption and snapshot, and reset only at the next route start. Add enemy-enters/enemy-leaves and blocker-change acceptance vectors. If first admitted scenario structurally excludes all qualifying contexts, explicitly bind that invariant to the content gate and defer general start-time evidence with origin-placement support. This is a bounded clarification of DEC-007, not a demand to implement placement now.

## Plan Review

Research objective substantially covered: source locks, full outcome transcription, proposed arithmetic, current accounting gap, stop lifecycle, persistent lots, exact RNG evidence, migration/privacy boundaries, seven ordered tasks and twelve proposed acceptance criteria. README, architecture/naming maps, docs index and roadmap distinguish research completion from production approval. No production task marked complete.

Task ordering is credible: approved decisions/specification and identity matrix precede Rules, dormant state, BP integration, stop/check authority, public activation and Runner evidence. Existing `MOV-REQ-015`/`MOV-AC-017` explicitly prohibit current BP mutation, so a versioned successor is warranted. Legacy histories must not be silently backfilled. Excluding grouped loss allocation, transport consequences, towing, capture, repair and later-stage authority is clear and proportionate.

Exact wire/version tables and closed transition diagrams remain appropriate Task 001 outputs. Findings concern coherence of the proposed placement boundary itself; they are not objections to intentionally pending contract details. Heavy-pivot gate not triggered.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Visually source-locked numerical bounds; 324 cells, nine probes, 606 bounded combinations | All nine JSON band rows versus chart p. 7; Python checker; PDF and fixture hashes | Confirmed | No numerical finding |
| No accepted BP accumulation; shared movement calculation | `CampaignElementMovedV2Factory.CalculateMovement`/`ProjectMoveForAuthority`; Reaction factory lines 124 onward; Movement requirements | Confirmed | Shared successor BP integration justified |
| RNG uses rejection sampling | `src/Cna.Core/Randomness/SandtableRandom.cs:48`–62 | Confirmed | Actual cursor evidence requirement correct |
| Accepted DEC-001–003 preserved; DEC-004–007 proposed | Approved continuity packet decision table; new packet lines 94–108 | Confirmed | No inferred owner approval |
| Atomic System event per stop protects hidden cardinality | Design lines 72–75 versus unsupported semantics lines 89–92 | Partially confirmed | Covers admitted stop batching; does not establish placement-based progress privacy, finding 1 |
| Stop state supports deferred phasing and reactor stops without unbounded recursion | Design lines 47–68 | Confirmed as proposed design constraint | Detailed discriminator freeze legitimately pending; start-time placement requirement still missing, finding 2 |
| Research ready for owner decision; production not approved | Scope/status throughout packet and task graph | Scope confirmed; readiness qualified by findings | Bounded packet revision needed before DEC-007 decision |

## Verification Performed

- `python3 docs/research/verify-breakdown-outcomes.py`: passed 324 coordinate/band cells, complete unique ranges, monotone columns, nine source probes and 606 proposed-loss bounds. Transcription hash `f63dc336364648acb52d20070bb1076205a36a40769f4391531ba65aaf94c401` matches packet.
- Visual comparison of `/tmp/brk-land-33.png`, `/tmp/brk-land-34.png`, `/tmp/brk-chart-7.png`: all numerical upper bounds matched source. Source supports cumulative stage BP, repeat-check threshold, sequential dice, rounding and persistent equipment location; one-third remains an explicit owner interpretation.
- `shasum -a 256 /tmp/brk-land-rules.pdf /tmp/brk-common-charts.pdf /tmp/brk-errata.pdf docs/research/fixtures/breakdown-outcome-bounds.v1.json`: all four hashes match retained packet values.
- `pdftotext -layout /tmp/brk-errata.pdf - | rg -n -A3 -B2 '21.12'`: confirms M13/40 BAR correction to 1R; no Truck/dice correction in that clause.
- `git diff --check -- README.md docs/README.md docs/research/breakdown-continuity-spike.md docs/roadmap/pre-alpha-roadmap.md naming-overview.md tech-design.md`: passed, no output.
- Read-only Python local-link check across eight scoped Markdown files: all 16 Breakdown link targets exist. Scope limited to local Breakdown links; not a general repository anchor/link gate.
- Git status/branch/HEAD matched bootstrap. User-owned context edits excluded. No .NET tests run: production code unchanged and numeric experiment/source inspection are proportionate checks for this delivery.

## Open Questions And Residual Risks

Owner decisions 004–007 remain pending, including admitted single-unit interpretation of the one-point/10% exception. No claim of all-vehicle, grouped, passenger/cargo, placement/capture/repair or later-stage support is made. Content admission must prove bounded fixtures rather than merely labeling them supported. Source terrain BP normalization was inherited from approved continuity work and was not re-audited here. Future task 001 must turn accepted choices into exact requirements, event identities, transition tables and acceptance evidence.

## Verdict

**Not ready for DEC-007 owner decision as currently framed.** Numerical research and staged delivery plan verified; placement admission still has an unacknowledged public-progress consequence and missing temporal input rule. Other proposed decisions have a concrete evidence base. This verdict applies only to decision-packet readiness, not production readiness, and requires bounded documentation clarification rather than a heavy pivot.

## Recommended Next Actions

Initiating task should respond to each finding with Accept, Dispute with evidence, or Defer with owner/rationale. Reconcile placement admission/privacy and start-time evidence, then present amended owner choices. Keep production blocked behind approved decisions and Task 001 contract freeze. Review instance 1 of 3 consumed; no additional instance or subagent created.
