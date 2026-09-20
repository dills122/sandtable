# F5 independent review

Review instance: 1 of 3. Reviewer started without inherited implementation conversation.

## Blind preliminary ledger

Recorded before author packet, retained author evidence, and session recall.

- Scope verified: branch codex/combat-task008-reaction-lifecycle, working-tree delta against 26ff137; five primary manifest paths match SHA-256. Supporting docs mark F5 active and F6 pending.
- Canonical second-move specification/schema inspected alongside full new implementation and tests. Reconstruction uses complete creation-rooted predecessor through exactly one participant event; movement stays open at version 15. No preliminary actionable defect identified.
- Tests independently derive public handles/action IDs; both-owner goldens cover ten artifacts and four cuts. Tests exercise actor before duplicate lookup, fork/conflict rejection, canonical and re-signed tampering, full history changes, cache restore, capacity, and defensive buffer ownership.
- Remaining verification: validate retained oracle/source pins and build/full-suite/format evidence; inspect predecessor World validation and structural equality supporting bounded writer. No independent execution claimed yet.
- Incidental CCE contamination: symbol query returned path/snippet for f5-dispatch.md and unrelated old review target-manifest metadata, not prior findings or author rationale. Neither old reports nor author packet opened during blind pass. CCE supplied insufficient full source; exact known paths read locally for complete review.

## Findings

No actionable findings in frozen F5 code or implementation plan.

Replay admits exactly one actual F2 participant move and at most one second-move event (`CampaignCombatReactionSecondMove.Replay`, lines 11–35). `Apply` authorizes actor/creation/cycle before duplicate lookup; command equality and terminal guards reject conflicting retries and subsequent movement (lines 37–50, 62–94). `Emit` advances one receipt/prefix/progress reference, appends existing track, and moves element plus representation while preserving route identity (lines 105–124). `ReadState` compares complete canonical history-derived bytes, so altered cache never becomes authority (lines 52–60).

Bounded World serialization validates predecessor via F2 `WriteWorld` and independently replays F5 suffix (`ExpectedWorld`, lines 125–136); `CampaignWorldSnapshotV7.Equals`, lines 157–163, compares every typed World collection. New writer rejects mismatched World before writing hardcoded absent fields. Accepted F2 policy remains unchanged.

## Plan Review

Ready for this bounded F5 scope. Canonical spec/schema, frozen fixture, Task008 execution index and F5 scope agree: actual F2 first-move version14 is causal predecessor despite delivery after F4; two owners, one Clear2 move each, version15 retains active window and route. Five-primary-file boundary met. README, naming, tech design and roadmap accurately describe pending verification and remaining F6/H/publication work. No migration, runtime rollout or public activation introduced; those gates remain explicitly open. Lead must update status only after required full gate and remaining independent rounds finish.

## Author-Claim Reconciliation

| Claim | Evidence inspected | Status | Consequence |
| --- | --- | --- | --- |
| Full creation-rooted authority; no cache authority | Replay/ReadState; missing/foreign/altered predecessor test | Confirmed | Correct causal boundary |
| Ten artifacts/four cuts, both owners | BothOwnersReproduceTenFrozenArtifactsAndFourCuts; fixture cases and source pins; focused log | Confirmed | Exact selected contract parity covered |
| Stable public window and rotated opportunity/action | PublicWindowOpportunityAndActionUseIndependentOrderedPreimages | Confirmed | Independent ordered preimages, separate persisted IDs |
| CP2→4, route/track preservation, active window | MovePreservesOpenRouteAndAllUnchangedAuthority; Emit/ProjectWorld | Confirmed | No premature completion or phasing resumption |
| Strict tamper, retry and buffer defenses | EveryCommandFieldActorRetryAndWrongStepRejects; EveryEventAndCacheLeafRejectsRawAndResigned; WholeWorldForgeryRejectsBeforeAndAfterMoveAndAllBuffersDetach; capacity test; inherited parser | Confirmed | Relevant attack/error paths covered |
| Focused22 and clean build passed | Actual /tmp/f5-final-focused.log and /tmp/f5-build.log | Confirmed | 22 passed, zero failed/skipped; zero warnings/errors |
| Full format passed | Empty /tmp/f5-format-full.log plus lead exit0 attestation | Confirmed by lead attestation | Reviewer did not rerun format |
| Full suite complete | /tmp/f5-suite.log still active at cutoff | Not yet claimed or verified | Required delivery gate remains lead-owned |

## Verification Performed

Reviewer executed read-only scope/hash/whitespace checks, inspected full new production source/tests, canonical specification/schema/fixture and surrounding predecessor validation, then author packet/evidence only after blind ledger. Session recall ran after blind ledger; no prior review report read.

- `git status --short`, `git branch --show-current`, `git diff 26ff137 --stat`: named branch/base boundary confirmed; four new files untracked, test-project link plus supporting docs changed.
- `shasum -a 256 -c .planning/combat-task008-delivery/f5-source.sha256`: all five paths OK.
- Python read-only SHA-256 comparison of fixture `sourceHashes`: all14 match; exactly two cases.
- `git diff --check`: exit0, no whitespace findings.
- Inspected `/tmp/f5-reaction-baseline.log`: PASS, 2 traces/2 events/4 cuts/2 retries/260 mutations/60 raw/48 boundaries/14 source pins. Oracle not rerun.
- Inspected retained result of `dotnet build Sandtable.slnx --no-restore '-bl:/tmp/f5-build-{}.binlog'`: succeeded, 0 warnings/errors, 4.41 seconds.
- Inspected focused result: 22 succeeded, 0 failed/skipped, 40.142 seconds (13 F5 plus 9 F2 per evidence).
- `dotnet test --solution Sandtable.slnx --no-build '-bl:/tmp/f5-suite-{}.binlog'` still running at review cutoff; no passing full-suite claim.
- Full `dotnet format Sandtable.slnx --verify-no-changes --no-restore` exit0 attested by lead; log empty.

No builds/tests/code/git mutations performed by reviewer. Only this report written.

## Open Questions And Residual Risks

Full suite and lead boundary gate still pending at cutoff. Trusted internal constructors permit synthetic objects, but accepted authority entry points replay history; this review does not claim arbitrary typed-state serialization as trusted restoration. Profile deliberately limited to ordinary infantry, sole opportunity and exact selected move. Repeated replay and duplicated bounded canonical writers impose maintenance cost, acceptable within current packet scope; no runtime performance claim evaluated.

## Verdict

Ready — code and plan for frozen F5 scope. This review does not authorize delivery acceptance before pending full/boundary gates and remaining required independent rounds complete.

## Recommended Next Actions

Lead complete existing full/boundary verification, retain results, run remaining configured independent rounds, then reconcile delivery statuses. No fix or architectural pivot requested. Reviewer starts no further round.
