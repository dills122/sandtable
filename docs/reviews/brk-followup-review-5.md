# BRK-AC-009 follow-up independent review

Review instance: **5 of 5**, explicitly authorized by initiating user.

## Findings

### P3 — Synchronize remaining review-limit statement

**Evidence:** `docs/design/breakdown-adjudication-v1.md:180` identifies authorized review 5; line 211 still states review limit exhausted at 4 of 4. Line references identify frozen reviewed HEAD.

**Scenario and impact:** Maintainer reading current plan encounters contradictory review-count guidance. Earlier four-instance statement is retained text made stale by current follow-up.

**Smallest correction:** During report retention, update current review count and acceptance status to reflect this fifth report. Preserve historical review 4 unchanged.

**No remaining P0–P2 findings.** Review 4’s missing current-version transcript coverage is addressed within accepted capability scope. No runtime disclosure defect demonstrated.

## Plan Review

Verified frozen target:

- Repository: `/Users/dsteele/repos/sandtable`
- Branch: `codex/breakdown-adjudication-design`
- Base: `77a3203c2cda4f106b0ff4b2033dd7909d1aa737`
- Head: `26c1b397402f25bac8964d687e610f920db6016f`
- Changes: one new 299-line test file, one research evidence document, one design-ledger paragraph.

Production source and scenarios match base. Declared user-owned dirt remained unchanged.

Reviewed BRK-REQ-008, BRK-AC-009, wire-contract activation status, disclosure manifest, review 4, public observation/query/submission paths, checkpoint admission, movement eligibility and surrounding observation tests.

`tests/Cna.Core.Tests/Observations/CampaignObservationV7TranscriptTests.cs:13` adds fifteen cases covering:

- Hidden route/stop identity across ordinary resumption and final-CP deferred stop.
- Participant completion, active unavailable closure and active timeout closure.
- Suppressed frozen opportunity membership/location.
- Opposing BP and alternative private eligibility reasons.
- Suppressed own location through Reaction and pending stop, followed by explicit approved release.

Each audience frame compares canonical Observation 7, History 2 and complete legal-action bytes, plus public state/position progress. Sequence-length equality detects extra transitions. Public submission assertions, required stop states and terminal Combat action closure prevent vacuous success.

Checkpoint permutations pass current admission and differ canonically. They exercise accepted continuation states rather than independently reconstructed alternate event histories. This distinction is explicit in production admission and governing evidence. Together with existing projection, strict-boundary and replay coverage, new tests provide proportionate evidence for review 4’s bounded acceptance finding.

Review 4’s P3 activation wording is corrected in normative specification openings.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
|---|---|---|---|
| Follow-up changes tests/evidence only | Frozen diff; source/scenario comparisons | Confirmed | No new production behavior or migration risk |
| Fifteen cases exercise public continuations and full audience frames | Test driver, frame serializer, assertions; fresh execution | Confirmed | Closes missing current continuation evidence |
| Hidden-state variants require valid current authority | `Replace`, `CampaignSnapshotV11Admission`, public query/submission validation | Confirmed | Invalid snapshots cannot satisfy comparisons |
| Location equalities respect approved disclosure | Observation projector exposes apparent opposing positions; suppression logic and release assertions | Confirmed | Visible opposing blocker changes correctly excluded from equality |
| Paired checkpoints do not prove alternate preamble provenance | Shared-prefix construction and explicit evidence limitation | Confirmed | Proof remains continuation-scoped |
| Narrow mutation produces three transcript failures | Retained mutation log shows three `AssertTraces` failures and three passing controls | Confirmed as retained output | Supports failure sensitivity; mutation itself not reproduced |
| Final full suite passed 1,670 tests | Author report; fresh build, fifteen cases and 81 boundary cases | Unverified independently for full-suite total | Full solution suite not rerun during this pass |

Preliminary ledger delivered before opening author explanation or research document. Required CCE retrieval incidentally exposed research document’s opening status paragraph and prior architectural memory; complete author rationale remained separated until preliminary assessment.

## Verification Performed

| Command/check | Result |
|---|---|
| `dotnet --version` | `10.0.400`; native MTP/xUnit configuration confirmed |
| `dotnet build Sandtable.slnx --no-restore '-bl:artifacts/binlogs/brk-review-5-{}.binlog'` | Passed; zero warnings/errors |
| `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CampaignObservationV7TranscriptTests'` | **15 passed**, zero failed/skipped |
| `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-trait 'Boundary=UserSpace'` | **81 passed**, zero failed/skipped |
| `dotnet format Sandtable.slnx --verify-no-changes --no-restore` | Passed |
| `python3 docs/research/verify-breakdown-contract-freeze.py` | Passed: historical hashes, migration obligations, mappings and 45 links |
| `python3 docs/research/verify-breakdown-outcomes.py` | Passed: 324 cells, nine probes, 606 combinations |
| `git diff --check 77a3203c2cda4f106b0ff4b2033dd7909d1aa737 26c1b397402f25bac8964d687e610f920db6016f` | Passed |
| Final HEAD/status checks | Frozen head unchanged; only declared protected dirt |

Build log: `artifacts/binlogs/brk-review-5-20260905-232023--43151--ZfeG1d.binlog`.

Initial sandboxed build stalled and was terminated; normal-access retry passed. No automatic approval rejection remained. No source/doc edits, staging, commits or mutation experiments performed.

## Open Questions And Residual Risks

- Fixed deterministic driver covers selected continuations, not exhaustive legal-history exploration.
- Shared prefix does not prove altered checkpoints arose from those earlier events. Evidence document accurately limits this claim.
- Positive-cohort Reaction, positive ZOC, larger worlds, general placement, later-stage reset and Combat execution remain excluded.
- Historical full-suite, mutation and Runner reproducibility evidence was not regenerated.
- Test harness depends on certified fixture topology and public action classes; future contract changes require deliberate review of equality boundaries.

## Verdict

**Ready with non-blocking follow-ups.**

New evidence addresses review 4 P2 and supports BRK-AC-009 acceptance within admitted public scope. Remaining P3 is review-status bookkeeping; no architectural pivot required.

## Recommended Next Actions

1. Retain this report and record disposition of review 4 P2 as addressed.
2. Synchronize current acceptance/review-count pointers, including design line 211.
3. Preserve stated proof limits and historical evidence identities.

Review **5 of 5** complete. Authorized limit exhausted; no further review instance or workstream started.

## Implementation-task disposition

- **Accept review 4 P2; addressed by `26c1b39`.** Fifteen current-version transcript/continuation cases and retained evidence accepted by this review. Proof remains within admitted-checkpoint continuation scope; no runtime disclosure defect was asserted or corrected.
- **Accept review 5 P3; corrected during retention.** Current design/specification, architecture, index, roadmap and evidence pointers now reflect completed bounded Tasks 006–007, this report and exhausted review limit 5 of 5. Historical review 4 and clean-run source identities remain unchanged.

No remaining actionable finding. Last independent verdict stays **Ready with non-blocking follow-ups**, as issued against frozen target. Retention corrects documentation only; reviewed test and production bytes are unchanged. No sixth review started.

Parent retention checks: source/test/scenario diff against reviewed HEAD empty; whitespace check passed; freeze audit passed with 47 links; 317 local Markdown link targets exist. No behavior change followed review.
