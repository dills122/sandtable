# Breakdown adjudication design independent review 2

Review instance: 2 of 3, BRK-RSH-002 decision-packet readiness.

Target: `codex/breakdown-adjudication-design`, `0512ec2db00fa2b695e911e2e48797c8dc11aa42..b745c51`. Initial working scope was checkpointed without candidate content changes during review. Only this report is reviewer-owned. User context/configuration edits remain excluded.

## Preliminary blind ledger

Recorded before opening `/tmp/brk-review-2/author.md` or retained review 1. CCE search returned review 1's title/scope snippet, but no findings; candidate research itself includes its correction rationale, so complete blindness to correction intent was impossible.

- Source visual comparison: Land pages 33–34 support stop checks, cumulative BP, sequential dice, upward rounding, one-point/10% exception, persistent placement and larger-than-battalion origin exception. Common Charts page 7 matches all nine JSON column boundaries, including null zero-result ranges at 61–70 and 71+.
- Numeric checker passes 324 cells, nine source probes and 606 proposed-loss combinations. PDF hashes and transcription hash match packet. No production behavior is tested by those checks.
- Proposed public capability profile removes hidden-dependent placement support by excluding larger combat units and aggregates in every admitted state. One standalone unladen Truck cohort per side also bounds grouping and transport effects. This is a material public capability limit requiring owner acceptance, not a claim of general Section 21 support.
- Positive public Reaction loss remains deferred; design explicitly distinguishes non-cohort zero-roll continuation tests from dormant motorized-infantry BP tests. No completion overclaim found.
- Exact schemas, closed continuation discriminators, certification predicates and rejection vocabulary are deferred to Task 001 behind owner decisions. Appropriate for current decision-packet objective; insufficient to begin production directly.
- No actionable defect established in blind pass. Verify remaining repository claims and reconciliation before final verdict.

## Findings

No actionable findings remain for decision-packet readiness.

Prior review 1's two P2 findings are addressed by bounded documentation changes:

- Hidden placement-dependent progress: `docs/design/breakdown-adjudication-v1.md:89` requires public-profile certification before visible creation and preservation after every transition. Lines 104–122 exclude multiple same-side Truck cohorts, passenger/cargo capability and every larger-than-battalion combat unit/formation/aggregate. Because Land 21.41 requires a qualifying larger enemy, this conservative structural exclusion makes its predicate false throughout admitted histories. A legal stop therefore cannot reveal hidden placement facts through support versus failure. `BRK-AC-008/009` require creation negatives, transition preservation and public transcript checks.
- Missing start-time placement truth: `docs/design/breakdown-adjudication-v1.md:124` explicitly requires immutable route-start threat/blocker evidence, retained through Reaction and replay, for the later general-placement package. Enemy-entering/leaving and blocker-change vectors are named. Narrow initial profile needs no such hidden predicate because its structural invariant rules out the exception. This is an explicit deferral, not recomputation from later positions.

These conclusions assess proposed design constraints, not implemented certification. Exact predicates remain Task 001's obligation.

## Plan Review

Research scope is covered: locked sources, numeric surface, proposed arithmetic and precedence choices, current BP integration gap, stop/check/loss vocabulary, RNG evidence, privacy/replay boundaries and seven ordered tasks with twelve proposed acceptance criteria. README, technical/naming maps, docs index and roadmap consistently distinguish research completion from production approval.

Task ordering is credible: owner decisions and exact requirements/contracts precede dormant Rules, authority state, shared BP accounting, stop/check transitions, public activation, then strict Runner evidence. Current `MOV-AC-017` prohibits post-creation BP mutation; successor events and explicit rejection of mixed identities avoid changing old histories. `CampaignElementMovedV2Factory.ProjectMoveForAuthority` preserves `VehicleBreakdownState`, and Reaction calls the shared movement calculator, confirming the stated integration gap. No production source changed.

Task 001 must still specify exact identities, closed bounded continuation states, public-profile certification and rejection vocabulary. Those are clear future deliverables rather than falsely completed requirements. General grouping, transport consequences, origin placement, capture/towing/repair and later-stage reset remain named exclusions. Positive public Reaction-stop tests explicitly exercise non-cohort zero-roll continuations; dormant motorized-infantry accounting cannot be labeled positive public Reaction-loss support. No heavy pivot required.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Outcome transcription and conditional arithmetic verified | Common Charts p. 7 versus all nine JSON rows; checker; PDF/fixture hashes | Confirmed | No numerical finding; checker remains research-only |
| Current moves preserve BP; Reaction shares movement calculation | `CampaignElementMovedV2Factory.ProjectMoveForAuthority`, `CampaignReactionParticipantEventFactory.CreateMove`, `MOV-AC-017` | Confirmed | Successor integration justified |
| Rainstorm needs input transformation, not existing shift call | `Cna1979Breakdown.GetWeatherColumnShift` lines 229–252 | Confirmed | Design correctly names intentional exception |
| Two d6 require actual cursor evidence | `SandtableRandom.RollD6` lines 48–62; `D6ConsumesRejectedCandidatesBeforeAcceptedByte` | Confirmed by inspection | Rejection-sampling requirement justified; tests not rerun |
| Accepted DEC-001–003 preserved; 004–007 pending | Continuity packet decision table; new packet lines 94–111 | Confirmed | No owner approval inferred |
| Revised capability profile closes placement findings | Design lines 89–130 and AC-008/009; Land 21.41 | Confirmed as proposed contract | Both prior P2 findings addressed |
| Unsupported cases fail before RNG | Initial author paragraph versus amended admission section | Superseded wording | Invalid/out-of-profile roots reject before visible creation; valid stops must not fail based on hidden support. Revised packet makes distinction explicit |
| Research complete means ready for owner decisions | Status, alternatives, task graph and retained limitations | Confirmed | Verdict limited to decision packet |

## Verification Performed

- `python3 docs/research/verify-breakdown-outcomes.py`: passed 324 coordinate/band cells, unique coverage, monotone columns, nine source probes and 606 bounded proposed-loss combinations. Reported hash `f63dc336364648acb52d20070bb1076205a36a40769f4391531ba65aaf94c401` matches packet.
- Visual source comparison of `/tmp/brk-land-33.png`, `/tmp/brk-land-34.png`, `/tmp/brk-chart-7.png`: all nine numeric bound rows match, including absent zero-percent ranges. Source facts and explicit interpretation boundaries match packet.
- `shasum -a 256 /tmp/brk-land-rules.pdf /tmp/brk-common-charts.pdf /tmp/brk-errata.pdf`: all PDF hashes match source lock.
- `pdftotext -layout /tmp/brk-errata.pdf - | rg -n -A3 -B2 '21.12'`: confirms Italian M13/40 BAR correction to 1R.
- `git diff --check 0512ec2..b745c51`: passed with no output. `git diff --name-only 0512ec2..b745c51` confirms eleven scoped documentation/research files, including retained review 1; no production code.
- Read-only Python link check over README, technical/naming maps, docs index, roadmap, continuity packet and new packet/design: all 17 local Breakdown link targets exist. Anchor correctness and unrelated repository links were not checked.
- CCE used for recall and discovery; compressed symbol snippets lacked usable chunk IDs, so exact located artifacts and supporting symbols inspected directly. No .NET build/test gate run because reviewed changes are documentation and research data/checker only.

## Open Questions And Residual Risks

Owner must still resolve DEC-004–007, particularly exact one-third arithmetic, stop/Reaction precedence and willingness to accept a tightly restricted synthetic public capability profile. Review does not approve those choices. Task 001 must make aggregate-size certification concrete and prove every admitted transition preserves it; a capability label alone is insufficient. Source terrain normalization was inherited from approved continuity work and not re-audited. General Breakdown and positive motorized Reaction losses remain unsupported future work.

## Verdict

**Ready** for BRK-RSH-002 owner decisions. Corrected packet is source-grounded, coherent and sufficiently concrete for its stated research/design gate. Production implementation remains blocked behind owner decisions and Task 001's exact contract freeze.

## Recommended Next Actions

Present DEC-004–007 with restrictions and alternatives intact. After owner resolution, freeze governing requirements, capability predicates, closed transition diagrams, successor identity matrix and acceptance criteria through Task 001. Review instance 2 of 3 consumed; no further instance, implementation fix or subagent created.
