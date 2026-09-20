# F5 independent review

Review instance: 2 of 3. Frozen target: base 26ff13766e3fcf62942a4efa9db33f497d03ecd6 plus five paths in f5-source.sha256 and supporting docs.

## Blind preliminary ledger

Recorded before author packet/evidence and recall. Canonical spec, schema, oracle source, implementation, tests and plan inspected. No actionable defect found in first pass. Replay roots authority in full actual F2 history; actor checked before accepted retry; command equality binds stable/rotated disclosures; deterministic one-event transition preserves open route/window. Tests cover two owners, ten artifact pins, four cuts, raw/re-signed mutations, typed World alterations, buffer detachment and capacity boundaries. Whole-World equality and predecessor guard require final corroboration in underlying types. Plan correctly separates F5 from F6, H, public activation and publication obligations. Five primary path hashes match freeze; branch/HEAD and dirty scope match bootstrap.

Independence: no implementation conversation or prior review reports read. CCE search incidentally returned f5-worker.md summary describing bounded scope and root gate ownership; no author rationale or prior verdict exposed. CCE expand_chunk returned "Chunk not found" for cited tests chunk; exact known files then inspected directly. Root status message reports full suite passed but retained evidence not yet checked at preliminary-ledger time.

## Findings

No actionable findings. Whole-World concern cleared: `CampaignWorldSnapshotV7.Equals` (CampaignWorldV7.cs:157) compares every typed collection, while F5 `ExpectedWorld` first invokes F2's own history-derived World guard, then replays its own suffix. Bounded writer cannot silently erase forged vehicle/other omitted fields. `Replay` (CampaignCombatReactionSecondMove.cs:11) derives actual predecessor; `Apply` (:37) authenticates before retry lookup; `Emit` (:98) compares exact expected command and derives one transition. ReadState accepts only exact regenerated cache bytes. Internal typed state constructors are not external authority entry points.

## Plan Review

Canonical .3p spec/schema and Task008 index agree: F5 branches from F2 authority14, not F4 terminal. Five primary files satisfy bounded ownership. Actual transition retains same active window/opportunity and reactor route while appending supply and one receipt/progress item; F6 completion remains pending. Supporting README, naming and technical design accurately describe implemented behavior without claiming activated runtime. H/general Snapshot12, multiple opportunities, positive vehicles and HOST-PUB-001 remain separate obligations. No dependency inversion, missing rollout, wire change or migration introduced by dormant internal adapter. Existing gate-status prose is expected to be refreshed by lead after final round; no acceptance inferred from pending wording.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Consequence |
| --- | --- | --- | --- |
| Full actual predecessor and exact one-event suffix | Replay, ReadState; MissingCompletedForeignAndAlteredPredecessorHistoryRejects | Confirmed | Caller cache cannot substitute authority |
| Stable public window13, rotated opportunity14, exact Clear2 action | Command; PublicWindowOpportunityAndActionUseIndependentOrderedPreimages | Confirmed | Public/private handles remain distinct |
| Actor-before-retry, exact retry and fork rejection | Apply/Authorize; EveryCommandFieldActorRetryAndWrongStepRejects | Confirmed | Authenticated deduplication intact |
| CP2→4, same route/start14, appended track, preserved window/RNG | Emit/ProjectWorld; MovePreservesOpenRouteAndAllUnchangedAuthority; two-owner goldens | Confirmed | F5 scope complete; no premature F6 effect |
| Own whole-World guard and detached data | ExpectedWorld/WriteWorld; World equality; WholeWorldForgeryRejectsBeforeAndAfterMoveAndAllBuffersDetach | Confirmed | Omitted-field serialization remains bounded |
| Two traces, ten artifacts, four cuts, canonical tamper rejection | BothOwnersReproduceTenFrozenArtifactsAndFourCuts; EveryEventAndCacheLeafRejectsRawAndResigned; fixture and schema | Confirmed | Independent frozen outputs plus meaningful semantic negatives |
| Focused22 and clean build | Retained focused/build logs | Confirmed | Runtime evidence applies to frozen source manifest |
| Full gate initially pending | Updated evidence and retained fullsuite/boundary logs | Superseded by completed evidence | Full test and boundary summaries now pass |

## Verification Performed

Read-only inspection only; no build/test rerun, code/git edits or delegated work. Only this report written.

- `git status --short`, `git rev-parse --abbrev-ref HEAD`, `git rev-parse HEAD`, `git diff --stat 26ff137`: correct branch/base and expected five primary paths plus supporting docs. Four new source/test files correctly included despite being untracked.
- `shasum -a 256 -c .planning/combat-task008-delivery/f5-source.sha256`: all five paths OK, checked twice including after full gate.
- `git diff --check`: pass, no output.
- Independent Python SHA-256 comparison of fixture sourceHashes: 14/14 match; 2 cases and 10 golden artifacts confirmed.
- Retained `/tmp/f5-reaction-baseline.log`: oracle PASS, 2 traces, 2 events, 4 cuts, 2 retries, 260 mutations, 60 raw cases, 48 boundaries, 14 pins. Oracle not rerun by reviewer.
- Retained `/tmp/f5-final-focused.log`: 22 passed, 0 failed/skipped, 40s142ms (13 F5 + 9 F2 per evidence).
- Retained `/tmp/f5-build.log`: build succeeded, zero warnings/errors, 4.41s; unique binlog path retained.
- Retained `/tmp/f5-suite.log`: 2,093 passed, 0 failed/skipped, 3m22s561ms; all three test assemblies passed.
- Retained `/tmp/f5-boundary.log`: 81 passed, 0 failed/skipped, 9s747ms.
- `/tmp/f5-format-full.log`: empty; lead tool status and evidence report exit0. Empty log alone does not independently establish exit code.
- CCE recall performed only after durable blind ledger. Recall corroborated F5/F6/H ownership; no prior review report opened. Evidence packet incidentally disclosed prior round's Ready summary after blind pass; verdict above independently derived.

## Open Questions And Residual Risks

No blocking question. Evidence is retained lead execution, not reviewer rerun. Dense bounded writers duplicate predecessor serialization and require care if contracts evolve; frozen hashes, literal artifacts and whole-World guard constrain current risk. No production performance, durable persistence or public activation claim made. Typed World tests sample CP and omitted vehicle fields at both cuts rather than individually constructing every possible nonempty typed collection; structural full equality and full cache leaf mutation coverage support present bounded profile.

## Verdict

Ready for F5 bounded scope. Review instance 2 of 3 complete. No heavy pivot or additional workstream needed.

## Recommended Next Actions

Lead owns final sequential independent round and status/evidence reconciliation before F5 acceptance. Keep F6, H and HOST-PUB-001 open. No code correction requested.
