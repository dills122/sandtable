# F4 independent review

Review instance: 1 of 3.

## Blind preliminary ledger

Recorded before author explanation, evidence logs, and session recall. Read canonical spec, schema, oracle transition logic, tests, five source paths, supporting documentation diff, and F2 surrounding replay/move admission. Source hashes match frozen manifest; branch `codex/combat-task008-reaction-lifecycle`, HEAD/base `f0cb5ca9aa256eb330a46c0ff86fe8f326174d3c`. Four implementation/test files untracked plus test project change; six supporting docs modified.

- No actionable defect identified in initial static pass. Replay reconstructs exact F2 first move, limits extension to two events, derives stop from retained reactor route, and resolves only recorded empty stop before resuming phasing route.
- System authorization precedes duplicate lookup. Retry compares complete typed input and preserves terminal state/original event. Both reason forks have separate action identities.
- Golden coverage spans both owners and both reasons, three state cuts, eight artifacts per trace. Strict readback compares full reconstruction; re-signed leaf mutations cannot replace authority.
- Plan dependency F2 first-move → F4 fits implementation. F5/F6, public activation, parent restoration, clocks, and positive vehicles remain explicitly deferred.
- Pending evidence review: focused/full gate results and canonical oracle/pin evidence. No test execution claimed yet.
- Independence: no author explanation or prior review read. CCE symbol query incidentally returned an unrelated historical 004b preflight snippet and commit subject; neither carried F4 author claims or findings.

## Findings

No actionable findings. No heavy pivot indicated.

## Plan Review

F4 meets bounded `008F4` and D2c.3o plan: actual F2 first participant move supplies authority; System close14→15 records reason-specific empty stop and clears window; separate resolution15→16 consumes exact stop and resumes retained phasing route. Five primary paths respect child limit. F3 is publication/base context, not causal predecessor. Supporting README, roadmap, naming and design updates preserve dormant/publication boundaries. Plan correctly leaves F5/F6, H/full restore, positive vehicles, multiple opportunities, public activation and HOST-PUB-001 open. No migration or host rollout needed for this internal codec addition. Status remains in progress pending lead's acceptance, which is accurate during review.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Exact first-move admission, full retained-history reconstruction | `CampaignCombatReactionFallback.Replay`, F2 `Replay`/`Initial`/`MoveOptions` | Confirmed | Caller cache cannot supply authority; missing/completed/foreign histories reject. |
| Close records stop without resuming; mandatory resolve restores exact route | `Command`, `Emit`, oracle `_emit`, `StopsPreserveMaterialAndResumeOnlyAfterMandatoryResolution` | Confirmed | Correct two-event lifecycle; no early resume or extra closure. |
| Actor checked before retry; complete input equality | `Apply`, `Authorize`, `EveryCommandFieldActorRetryAndCompetingForkRejects` | Confirmed | Player/altered retry cannot bypass System boundary. |
| Public handles differ from persisted IDs; kind-specific actions and legacy receipt domains | `Command`, `StopCapability`, model `ReceiptId`, independent hash assertions | Confirmed | Contract/action/receipt identities match frozen specification. |
| All World/resource/progress fields retained | State delegation, `SerializeState`, F2 `WriteWorld` guard, preservation and whole-World forgery tests | Confirmed | No unsupported state silently substituted or material progress added. |
| Four forks, 12 cuts, 32 artifacts, strict and re-signed tamper rejection | Fixture inventory, golden tests, mutation tests, retained focused run | Confirmed | Both owners/reasons covered, including terminal retries and state0 readback. |
| Full gate underway | Retained build/full-suite/format logs | Partially confirmed | Build passed; suite still running at final inspection. Format exit0 recorded by lead, empty log itself offers no independent exit-status evidence. |

## Verification Performed

Reviewer executed read-only scope checks: `git status --short`, `git branch --show-current`, `git rev-parse HEAD`, `git diff --stat f0cb5ca`, supporting-path `git diff f0cb5ca`, and `git diff --check f0cb5ca` (no whitespace findings). Untracked source/test files reviewed in full rather than relying on tracked diff.

`shasum -a 256 -c .planning/combat-task008-delivery/f4-source.sha256` passed all five paths before and after review. Reviewer Python/hashlib check independently verified all 17 canonical source hashes, four fixture cases and 32 golden artifacts.

Inspected retained execution evidence; did not rerun builds/tests/oracle:

- `PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-inherited-reaction-active-fallback-v1.py` — `/tmp/f4-reaction-baseline.log`: PASS, 4 traces, 8 events, 12 cuts, 8 retries, 612 mutations, 200 raw rejects, 132 boundaries, 17 pins.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class '*CombatReactionFallbackTests' '*CombatReactionLifecycleTests' '*CombatReactionClosureTests' --no-restore '-bl:/tmp/f4-focused-{}.binlog'` — `/tmp/f4-focused.log`: 29 passed, 0 failed/skipped, 1m38s722ms. Includes 11 F4 tests and predecessor F2/F3 regression cases.
- `dotnet build Sandtable.slnx --no-restore '-bl:/tmp/f4-build-{}.binlog'` — `/tmp/f4-build.log`: succeeded, zero warnings/errors, 4.05s.
- `dotnet test --solution Sandtable.slnx --no-build '-bl:/tmp/f4-suite-{}.binlog'` — still running at inspection; no full-suite passing claim.
- `dotnet format Sandtable.slnx --verify-no-changes --no-restore` — lead evidence records exit0; `/tmp/f4-format-full.log` empty. No reviewer rerun.

## Open Questions And Residual Risks

Full-suite result remains lead-owned integration check. No remote/public/durable recovery guarantee inferred from internal tests. Packet-local serialization repeats established field layouts, but frozen hashes and full-state comparisons constrain drift; no refactor warranted within this slice. Per instructions, reviewer did not execute builds/tests, mutate implementation, stage, commit, or delegate.

Required session recall ran only after persisted blind ledger. It returned F4 code-area description and historical contract decisions/review summaries; these were post-ledger evidence, not premises of blind findings. No prior report file or implementation conversation was read.

## Verdict

**Ready** for bounded F4 implementation and plan. No actionable findings; final integration acceptance still requires lead's running full-suite result. This verdict does not complete Task008 parent or authorize public activation.

## Recommended Next Actions

Lead retain full-suite terminal result, reconcile F4 status after required reviews, and publish exact frozen scope through existing PR136 workflow. No corrective code changes requested; no further review instance started by reviewer.
