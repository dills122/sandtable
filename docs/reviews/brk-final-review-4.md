# BRK final review of Tasks 006–007

Review instance: **4 of 4**, explicitly authorized by initiating user.

## Findings

### P2 — Add current-version full-transcript privacy evidence before closing BRK-AC-009

**Evidence:** `tests/Cna.Core.Tests/Observations/CampaignObservationV7ContractTests.cs:138`, single-checkpoint permutation test at line 179; required transcript/progress equivalence in `docs/specs/breakdown-adjudication-v1.md:219`; BRK-AC-009 at line 302. Line references identify frozen reviewed HEAD.

Current tests verify isolated Reaction/pending-stop projections, hidden route age, aggregated lot identity, and outward type/property closure. Existing retained-transcript and hidden-opportunity tests still exercise Observation 6 and Snapshot 10. Repository-wide searches found no paired current-version histories comparing player observations, History 2, legal actions, and progress through new stop/resume transitions.

**Failing verification scenario:** A regression that changes action availability or inserts an extra observable stop/resume transition based on hidden authority can evade isolated projection comparisons. Historical V6 tests cannot exercise those new transitions.

**Impact:** Mandatory AC-009 evidence remains incomplete despite passing boundary gate. This is a coverage/acceptance finding; no runtime disclosure defect demonstrated.

**Smallest correction:** Add deterministic paired-history tests within certified profile, keeping approved audience facts equal. Compare full audience transcript, History 2, candidate bytes, and progress through Reaction completion, active System closure, reactor-stop resolution, and phasing resumption. Exercise admitted hidden identity/BP/eligibility/location/blocker variations where those facts remain hidden. Preserve explicit exclusions for positive-cohort Reaction and out-of-profile worlds.

### P3 — Synchronize normative activation status with implemented version set

**Evidence:** `docs/specs/breakdown-adjudication-v1.md:3` still states Tasks 006–007 pending and describes “next dormant implementation”; `docs/specs/breakdown-wire-contract-v1.md:3` still states public activation pending.

Current source activates Rules 9, Content/Setup 6, Snapshot 11, Observation 7, and action policy v3. Design plan and specification ending acknowledge Task 006 completion.

**Impact:** Canonical documents give contradictory guidance about which identities public entry points admit.

**Smallest correction:** Update opening status/context paragraphs while preserving historical freeze baseline and distinguishing implemented activation from outstanding review acceptance.

## Plan Review

Reviewed frozen range:

- Base: `3d5baa330d257a695bc8f9921200e5fa96195f86`
- Head: `b894972ca7137d3239c9daf8e45a4819bdc57cce`
- Branch: `codex/breakdown-adjudication-design`
- Source candidate: `59679493866d7f23ea0a69964e93bbfb67bd2841`

Range contains five commits and 194 changed files. Candidate-to-head difference is documentation/evidence only. Declared user-owned changes remained excluded; no reviewed source or documentation edited during review.

Task 006 coherently switches public creation, current checkpoint/event admission, query/submission, replay, observations/history, action policy, and Runner admission. Inspected code validates authority before publication/acceptance, preserves explicitly historical readers, and keeps authoritative calculations outside Runner controllers.

Task 007 implements fourteen frozen successor obligations, thirteen bounded Reaction children, supplemental Truck study, additive controller policies, strict bundle negatives, and reproducibility evidence. Conditional cohort capability validation fits profile’s explicitly permitted zero-cohort content.

Task order and architecture remain appropriate. Migration and replay evidence are substantial. Task 006 cannot yet claim complete AC-009 coverage for reason above.

Accepted deferrals remain bounded: positive local/remote ZOC, positive-cohort Reaction loss, motorized infantry transport, grouped allocation, general placement/capture/towing/repair, later-stage reset, and Combat execution. These exclusions do **not** defer privacy checks for admitted current histories.

Preliminary ledger delivered before opening author explanation. Required CCE recall exposed one prior architectural rationale; author packet itself remained unread during initial implementation inspection.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
|---|---|---|---|
| Public current version set activates together; legacy identities reject | Creation execution, current event/snapshot runtime, public activation and Runner admission tests | Confirmed | No activation inconsistency found |
| Controllers use public capabilities; repeated movement stays bounded | Controller selection, policy codecs/configuration identity, study tests | Confirmed | No hidden authority input found in changed controller path |
| Task 006 completes AC-009 privacy evidence | V7 projection/action tests, disclosure manifest tests, historical transcript tests | **Unverified as completion** | P2 finding; full current transcript/progress comparisons missing |
| Fourteen successors and thirteen Reaction children preserve bounded obligations | Frozen inventory, migration tests, retained-run comparison | Confirmed | Two positive-ZOC deferrals remain explicit |
| Rehashed Breakdown forgeries reject | Eight mutation cases in bundle tests; fresh suite execution | Confirmed | Event/proof hash replacement does not bypass tested semantic admission |
| Two clean runs produce matching canonical artifacts | Both retained roots, build identities, proof status, hashes, direct bytes | Confirmed | 47 campaigns/run; 423 matching canonical files |
| Final suite passes 1,655 tests and 66 boundary cases | Independently executed commands below | Confirmed | Passing tests do not close missing AC-009 coverage |

## Verification Performed

Executed against frozen primary head:

| Command/check | Result |
|---|---|
| `dotnet --version` | `10.0.400`; native MTP configuration and xUnit v3 confirmed |
| `dotnet build Sandtable.slnx --no-restore '-bl:artifacts/binlogs/brk-review-4-{}.binlog'` | Passed; zero warnings/errors |
| `dotnet test --solution Sandtable.slnx --no-build` | **1,655 passed**, zero failed/skipped |
| `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-trait 'Boundary=UserSpace'` | **66 passed**, zero failed/skipped |
| `dotnet format Sandtable.slnx --verify-no-changes --no-restore` | Passed |
| `python3 docs/research/verify-breakdown-outcomes.py` | Passed: 324 cells, nine probes, 606 bounded loss combinations |
| `python3 docs/research/verify-breakdown-contract-freeze.py` | Passed: fourteen historical hashes, fifteen Reaction children, fifteen inherited schema hashes, requirement/acceptance mappings and links |
| `git diff --check 3d5baa330d257a695bc8f9921200e5fa96195f86 b894972ca7137d3239c9daf8e45a4819bdc57cce` | Passed |
| Final Git status/head check | Frozen head unchanged; only declared protected user dirt |

Build log: `artifacts/binlogs/brk-review-4-20260905-222402--38682--5qjVkJ.binlog`.

Initial sandboxed build stalled before output and was terminated. Retry with normal local access passed. No automatic approval rejection remained.

Retained evidence independently checked with:

```sh
python3 docs/research/verify-breakdown-runner-runs.py compare /Users/dsteele/repos/sandtable /tmp/brk007-final-run-a /tmp/brk007-final-run-b 59679493866d7f23ea0a69964e93bbfb67bd2841 /tmp/brk-review-4-comparison.json
```

Result: fifteen manifests, 47 campaigns/run, 188 verified proof records, 423 canonical-file comparisons, nine matching report fingerprints. Matrix hash matches author claim:

`sha256:938616c9aa4b9d92e295a4d0b5dfcb1c8e7f8bdf1c9b59f8966bd356c71c50da`

Separate Python read compared all **423 file pairs directly byte-for-byte**, also passing. These checks inspect retained clean-run artifacts; reviewer did not regenerate both complete CLI runs or repeat restore.

## Open Questions And Residual Risks

- No demonstrated runtime disclosure, replay, conservation, or movement defect found in inspected scope.
- Passing allowlist/type-closure tests establish structural boundaries; they do not establish full behavioral noninterference across current histories.
- Checkpoint admission validates current certified state and retained preamble facts; full provenance remains responsibility of replay/reconstruction. Implementation and author explanation agree on that distinction.
- Historical Tasks 002–005 and excluded capabilities receive no new blanket approval from this review.

## Verdict

**Not ready** for Tasks 006–007 acceptance closeout: mandatory BRK-AC-009 current-version transcript/progress evidence remains missing. Runtime implementation and retained Runner evidence otherwise support intended bounded behavior.

No heavy architectural pivot required; correction scope is focused tests and documentation.

## Recommended Next Actions

1. Add missing certified current-history privacy comparisons; rerun relevant boundary and regression checks.
2. Correct normative activation status.
3. Record initiating task’s response to each finding.

Review **4 of 4** complete. No further independent review instance started; configured limit exhausted. Any additional independent pass requires renewed explicit authorization.

## Implementation-task disposition

- **Accept P2; open.** Owner: Core observation/privacy follow-up under BRK-TASK-006, gating BRK-TASK-007 acceptance closeout. Add paired certified current-history tests for observations, projected History 2, legal candidate bytes and progress through the listed stop/resume paths. This review-retention change adds no runtime or test changes and does not claim AC-009 complete. No demonstrated leak is being asserted.
- **Accept P3; corrected during retention.** Governing behavior/wire openings now identify implemented public activation and current identities. Current plan, architecture, index, roadmap and closeout pointers distinguish implemented Tasks 006–007 from open AC-009 acceptance. Historical freeze baseline and past reviews remain unchanged.

Last independent verdict remains **Not ready**, instance **4 of 4**. No fifth reviewer started. Documentation-only retention does not close the P2 finding or change reviewed behavior.
