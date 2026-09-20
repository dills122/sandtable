# F6 independent review

Review instance: 2 of 3.

## Blind preliminary ledger

Recorded before author explanation, retained execution evidence, or session recall.
Bootstrap and task supplied scope and reported build/format status; no earlier review reports read.
CCE symbol searches returned implementation snippets only; no incidental earlier-review contamination observed.

- Scope verified: HEAD `6dd60cf565f51020cafaf1b07abca3bb6703f759`, branch `codex/combat-task008-reaction-lifecycle`, five primary source hashes match `f6-source.sha256`; supporting documentation diff matches F6 status scope.
- Canonical spec/schema/fixture and F6 source/tests inspected. Completion/resolve/close order is explicit; terminal retry checks actor first; complete byte equality binds event/cache to reconstruction.
- Golden tests cover both owner traces, 22 artifacts, eight cuts; independent preimages, receipt/prefix calculations, actor and same-type command mutation rejection, re-signed event mutations, World guards, buffer ownership and capacity checks are present.
- Provisional code judgment: no actionable defect found. Need verify retained test evidence and predecessor empty-inventory admission before final verdict.
- Provisional plan judgment: F6 correctly depends on F5 and remains pending gates/reviews; parent restore, publication, public activation and vehicles remain excluded. Five-primary-file limit respected.

## Findings

No actionable P0–P3 findings in frozen F6 implementation or plan.

Exact admission reconstructs F5 rather than consuming caller state. F5 has one actual second move to supply, CP4, retained first-move route14, and one active opportunity. F6 `Command` verifies supply/CP4/retained route before assigning empty inventory capability. This is sound for the exact frozen profile; it is not a general movement-option enumerator. `Apply` authenticates command-kind-specific actor before any accepted retry lookup. `Emit` requires command equality and emits completion, resolution and closure separately. State cache readback and event replay compare entire canonical byte sequences, so forged cached authority cannot enter accepted replay.

## Plan Review

Ready for bounded F6 acceptance. Implementation matches `008F6` and `3Q-AC-01` through `3Q-AC-06`: actual second move prerequisite, capability rotation, three causal transitions, material retention/resumption, replay/retry/rejection checks, and narrow scope closure. No architectural pivot, new authority boundary, or runtime activation introduced. Three production files plus one test file and project fixture link respect five-primary-path budget.

Supporting README, roadmap, design, naming and execution updates retain pending review/full-gate status. H/noninitial Snapshot12, publication proof, positive vehicles and public activation remain open. Lead must reconcile final gate/review status after results exist; present pending wording is accurate rather than a plan defect.

## Author-Claim Reconciliation

Author/evidence read only after blind ledger. Required session recall returned unrelated historical review summaries; these did not influence blind pass. Evidence file contained a one-line prior-round verdict after blind pass; no earlier review report opened or relied upon.

| Author claim | Evidence inspected | Status | Consequence |
| --- | --- | --- | --- |
| Two traces, six events, eight cuts, 22 artifacts | Fixture, `BothOwnersReproduceTwentyTwoFrozenArtifactsAndEightCuts`, independently executed oracle | Confirmed | Exact canonical parity evidence exists. |
| Owner completion, System resolution, System closure; actor-before-retry | `Authorize`, `Apply`, `Command`, `Emit`, `EveryCommandFieldActorRetryAndSequenceForkRejects` | Confirmed | Terminal retries preserve authenticated roles and original bytes. |
| Stable window13, opportunity15 over empty inventory, stop capability16 distinct from authority | Codec and `EmptyOpportunityStopAndActionsUseIndependentPreimages` | Confirmed | Independent hash preimages validate authority/public separation. |
| No early resume, resolved IDs distinct from empty closed list, null resolve interrupt | Event writer and literal per-cut assertions | Confirmed | Closure alone resumes suspended phasing route. |
| Whole World/tracks/progress/RNG preserved and omitted typed fields guarded | `WriteWorld`, F5 `ExpectedWorld`, retention loop and `WholeTypedWorldGuardsAndOwnershipBuffersHoldAtEveryCut` | Confirmed | Bounded serializer does not silently discard forged World fields. |
| Final focused24 passed | `/tmp/f6-final.log` | Confirmed | 24 passed, zero failed/skipped; no reviewer rerun claimed. |
| Build/full-format complete, full gate pending at author freeze | Build log, format evidence, final suite log | Build and full suite confirmed; format exit reported by lead | Final suite subsequently completed successfully. |

## Verification Performed

- `git status --short`, `git rev-parse HEAD`, `git branch --show-current`: frozen base and scoped working-tree boundary verified.
- `shasum -a 256 -c .planning/combat-task008-delivery/f6-source.sha256`: all five paths OK.
- `git diff --check`: passed.
- Read-only Python SHA-256 check independently verified all 18 canonical fixture source pins, two cases and 22 retained artifacts.
- `PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-inherited-reaction-movement-completion-v1.py`: exit0; two traces, six events, eight cuts, six retries, 506 mutations, 54 malformed-byte rejections, 72 boundaries and 18 pins.
- Inspected `/tmp/f6-reaction-baseline.log`: same oracle totals.
- Inspected `/tmp/f6-final.log`: exact retained command recorded in evidence is `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatReactionCompletionTests' '*CombatReactionSecondMoveTests' '-bl:/tmp/f6-final-{}.binlog'`; 24 passed, zero failed/skipped, 2m22s661ms.
- Inspected `/tmp/f6-build.log`: `dotnet build Sandtable.slnx --no-restore '-bl:/tmp/f6-build-{}.binlog'`; build succeeded, zero warnings/errors, 4.10s.
- `/tmp/f6-format-full.log` is empty, consistent with quiet formatter success; exit0 comes from lead evidence rather than independently recoverable log content. Command: `dotnet format Sandtable.slnx --verify-no-changes --no-restore`.
- `/tmp/f6-suite.log` initially had no aggregate summary; final read after lead completion notice confirms 2,104 passed, zero failed/skipped, 4m51s848ms. Command: `dotnet test --solution Sandtable.slnx --no-build '-bl:/tmp/f6-suite-{}.binlog'`.
- Final `/tmp/f6-boundary.log` read confirms 81 passed, zero failed/skipped, 10s140ms.
- No builds, .NET tests, code edits, Git mutations or delegation performed by reviewer. Only own report written.

## Open Questions And Residual Risks

Full solution aggregate and boundary results completed before report return. Packet-local writer duplication and repeated bounded creation-rooted replay remain explicit maintenance costs; evidence does not establish generalized runtime performance or broader profile admission. Changes to frozen profile/content require revisiting literal empty-inventory reasoning. No evidence supports parent restore, durable publication or public activation, and none is claimed.

## Verdict

**Ready** for scoped F6 code and plan review. Full solution and boundary logs now pass. Lead retains ownership of final review round and documentation reconciliation before integration; this report makes no publication claim.

## Recommended Next Actions

Complete remaining authorized review, reconcile pending documentation with final retained results, and integrate after required gates pass. No additional review instance or workstream started here.
