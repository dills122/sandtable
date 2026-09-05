# ZOR-TASK-007 independent review

Review instance: 1 of 3

## Preliminary blind ledger

Recorded before opening author explanation. Repository target verified as branch
`codex/zor-task-007-runner-closeout`, base `2627423`, frozen candidate `dad5f1c`.
CCE search accidentally returned heading and opening success-criteria snippet of author packet;
full rationale remained unread during initial code/test inspection. Independence therefore has
this limited search-excerpt qualification.

- No confirmed defect in initial pass. Reaction controllers consume public candidate ID/kind and
  accepted-step counters; malformed mixed audience shapes fail closed.
- Check current Movement-completion canonical-position validation and preservation of Reaction
  world state against predecessor completion semantics.
- Check strict nested event validation reaches Core reconstruction rather than accepting only
  top-level Runner event shape.
- Check CP/BP acceptance coverage: new checked Content uses nonmotorized elements, so unchanged BP
  assertions alone cannot establish positive motorized Reaction cost accumulation.
- Clean repeated-run evidence and final status synchronization explicitly pending initiating task.

## Findings

No actionable findings in frozen implementation `2627423..dad5f1c`. No P0/P1/P2/P3 defects
confirmed. Review changed Core/Runner source, controller and semantic-tamper tests, new Content
fixtures, checked Maneuver assertions, canonical requirements, current plan, and documentation.
User-owned context files remain excluded.

Preliminary concerns resolved:

- `CampaignCurrentMovementCompletion.Create` requires valid current Snapshot 10, first-side
  stage-1 Movement, and no open Reaction. Snapshot validation checks canonical sequence authority
  against stage orders. `Apply` compares complete expected event bytes and preserves World/RNG.
  This matches predecessor completion semantics while admitting legal non-phasing Reaction costs.
- Runner's exact outer event schema is not sole semantic admission. Existing
  `ExerciseBundleSemanticValidator.ObserveReconstruction` enumerates fresh current Core actions
  and requires one exact event-byte match; `ObserveReadjudication` separately submits retained
  action IDs and verifies receipt/event/final-state evidence.
- Initial accounting concern incorrectly generalized added remote C/D mobility to inherited A/B.
  `Cna1979SyntheticContentCatalog.CreateElements` includes motorized Commonwealth A with Truck
  cohort. Checked trajectories exercise that reactor. Current Movement foundation explicitly
  excludes post-creation BP mutation (`MOV-REQ-015`, `MOV-AC-017`); preserving exact Breakdown state
  is appropriate to current scope. Positive CP accumulation and preserved BP/RNG are checked.

## Plan Review

007A implemented coherently: nine named policies, exact Reaction audience arbitration, bounded
episodes, seven admitted synthetic Content packs/setups, and fifteen checked children.
`ReactionManeuverTests.AssertReactionEvidence` checks ordering, two-step episodes, recurrence,
subset/active/reason-specific closure, remote exclusion, empty/noncombat cases, local ZOC results,
CP sums, preserved Breakdown state, and final first-side Breakdown checkpoint. Existing Core
suites retain illegal-submission and disclosure acceptance coverage.

007B code coverage is sufficient: strict Reaction envelope readback, three event-kind shape mutation
tests, fully rehashed binding/reason tamper rejection, and fresh-session semantic admission.
`docs/research/simulator-reaction-trajectories.md` maps all 18 acceptance criteria to checked
Runner/Core evidence. README, technical design, naming, harness docs, and roadmap describe same
delivery boundary. Direct current Movement completion is justified scope adjustment: Runner could
not reach approved terminal with legal Reaction state through predecessor validation.

Remaining plan checkbox covers initiating task's final clean-run record, review reconciliation,
status update, and commit; bootstrap explicitly identified these as pending closure operations.
No implementation redesign or additional workstream required.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Controllers need only public ID/kind and accepted counters | `ExerciseExecutor` projection and post-acceptance updates; `ReactionController.SelectReaction`; candidate constructor | Confirmed | No new authority disclosure or hidden binding input |
| Previous Movement policies retain identity/behavior | Explicit `IsReactionPolicy` dispatch; appended enum values; three policy-token maps | Confirmed | New arbitration opt-in, prior conflicts still fail closed |
| New fixtures use admitted Content/creation | Content catalog construction, setup/resolver wiring, seven `ReactionRunnerContentTests` cases | Confirmed | No checkpoint injection or fabricated ZOC flags |
| Current completion preserves accepted Reaction state | New completion factory/application and current runtime dispatch; final matrix snapshot/cost assertions | Confirmed | Necessary compatibility fix preserves event-v1 semantics |
| Rehashed forged events cannot gain authority | Semantic validator fresh submissions and canonical-byte checks; binding/reason tamper tests | Confirmed | Hash refresh alone cannot bypass semantic validation |
| Full suite green after setup assumption fix | `/tmp/zor007-just-check.log`; narrowed initiative-specific weather lookup | Confirmed | 1216 tests pass; original weather coverage retained |
| BP and RNG preserved through checked Movement/Reaction | Matrix initial/final Breakdown-state and random-cursor comparisons; Movement continuity requirements | Confirmed | Supports current continuity boundary, no Breakdown adjudication claim |

## Verification Performed

- Executed `git diff 2627423 dad5f1c --check`: exit 0, no output.
- Inspected supplied `/tmp/zor007-just-check.log`: restore, format verification, and build complete;
  build reports zero warnings/errors. Mandatory user-space boundary suite: 48 passed, zero failed
  or skipped. Full solution suite: 1216 passed, zero failed or skipped.
- Verified branch, candidate commit, changed-path scope, canonical requirements, and plan against
  frozen source. Did not repeat builds/tests already covered by completed full gate.
- Inspected `/tmp/zor007-clean-comparison.json`, comparison script, and detached checkout HEAD:
  exact candidate `dad5f1cddf6e5e3b81bbef9b1cdc7deae7169671`. Independently reran comparison
  assertions with script's final file-write line omitted: exit 0. Both runs contain fifteen
  children, all 30 build identities are clean, 60 proofs are verified, and 135 canonical files
  match byte-for-byte. Matching report fingerprint:
  `sha256:21e2e2dde1002c677b382a398c6ff3e40635c324437fe1c3ab1d30c90694f845`.
  Initial attempt to omit final write by string search failed before comparison execution;
  corrected line omission succeeded without writing comparison artifacts.
- Read-only source review; only this assigned review report written. No implementation changes,
  new reviews, extra agents, commits, or staging performed by reviewer.

## Open Questions And Residual Risks

- No unresolved implementation question. Exact trajectory counts intentionally change if content
  or action identities change; corresponding evidence must be reviewed together.
- Policy strings exist in three maps; current matrix/manifests exercise all nine additions.
- Synthetic checked scenarios prove supported rules-laboratory behavior, not campaign balance or
  later hosting deadlines, Combat, or Breakdown adjudication.
- Blind pass had limited accidental CCE excerpt exposure, disclosed above; substantive author
  rationale was read only after preliminary ledger was written.

## Verdict

**Ready.** Frozen implementation and acceptance plan supported by full gate, strict checked
trajectories, and independently checked clean repeated-run equality. No source remediation or
additional review instance required. Initiating task retains responsibility for final status
synchronization and authorized commit.

## Recommended Next Actions

Retain clean repeated-run evidence, reconcile this review, update final completion status, and
finish authorized feature-branch commit. No source remediation requested.

## Final Documentation Reconciliation

Same review instance 1; inspected only final working diff in
`docs/specs/exercise-harness-v1.md` and `docs/design/exercise-harness-v1.md`.
Revised EXR-025/EXR-AC-014 accurately state already-reviewed opt-in Reaction arbitration:
exact System unavailable/timeout pair plus one reacting player, or sole empty-window System
close; malformed shapes reject. EXR-027 accurately records accepted current-episode move and
current-window completion counters. Design traceability and completion status match verified
implementation/evidence. Documentation corrects stale unconditional single-audience wording;
no source, behavior, architecture, or scope change. **Ready** verdict unchanged.
