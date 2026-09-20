# Independent review — Task008 D1

Review instance: **1 of 3**.

## Preliminary ledger — before author explanation

- Ground truth: branch `codex/combat-task008-reserve-designation`, HEAD/base `3ded1ebd8906b04969a9289a8b1b8068b0b8f75c`; seven tracked modified files and four untracked implementation/test files match bootstrap. Review target is working tree, not HEAD alone.
- Canonical Reserve spec requires complete creation/preamble/Weather/stage provenance, owner resolved from retained order, exactly one own none→I designation, unchanged resources, canonical receipt/history, and eventual atomic completion. Current D1 implements only precompletion portion; plan explicitly keeps full terminal obligations open under D2→019A before Movement.
- Tests examined before implementation: sixteen frozen cases across all Weather outcomes, both orders and empty/I; predecessor byte fingerprints; actor/identity/history/cache forgery checks; caller-buffer isolation; bounded World writer rejection.
- Code inspection: full predecessor replay precedes admission; input authorization precedes accepted retry lookup; canonical event recomputation prevents resigned invention; World reconstruction changes only own status and compares complete typed World before serialization.
- Preliminary concerns to reconcile: private serializer intentionally handles only precompletion profile; completion evidence must not be claimed as C# execution. Hardcoded empty World fields require exact causal World equality. No actionable defect found in initial pass.
- Retained `/tmp/d-reserve-baseline.log`: PASS, 16 traces/40 cuts/2342 leaf mutations/733 raw rejections/1578 boundary checks/4 frozen-kernel parity cases/14 pins; Python contract evidence only.

## Findings

No actionable findings. Scope, implementation, tests and plan agree for dormant D1 precompletion admission.

## Plan Review

D1→D2→019A→E refinement preserves canonical contract: D1 covers state10 and optional designated state11; D2 must derive OpeningBase and completion2 from actual retained history; 019A applies that same event atomically and owns terminal state11/12, designation retry after completion and full readback. No extra opening event or synthetic terminal state is allowed. Execution index explicitly leaves D parent/full Reserve replay open until terminal projection; E now depends on 019A. This resolves ordering without moving gameplay ownership or dropping requirements. README, tech-design, naming-overview and roadmap reflect bounded scope.

Five primary files are present: three source files, focused test file and test project fixture link. Remaining edits are supporting documentation. No old codec, fixture, schema or production registration changed. World writer duplication is bounded and justified: `ExpectedWorld` reconstructs accepted none→I mutation, and `CampaignWorldSnapshotV7.Equals` compares every World collection plus element values before hardcoded absent fields are emitted. Initial-only reader remains unchanged.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Full actual predecessor history supplies authority | `CampaignCombatReserveDesignation.Replay`, `CampaignCombatStageEntry.Replay`; predecessor-negative and frozen tests | Confirmed | No cache/receipt substitutes for ancestry |
| Owner resolves from order; only own status changes | `Initial`, `Command`, `Emit`, `DesignatedWorld`; ActLast fixture cases and World comparison assertions | Confirmed | Fleet/Initiative ownership cannot leak into designation |
| Retry authorizes before lookup and returns original event/current state | `Apply` calls `Authorize`, then full input equality; focused retry/actor tests | Confirmed within D1 | After-completion retry remains 019A obligation |
| Bounded writer validates typed causal mutation | `ExpectedWorld`, `WriteWorld`, complete World equality; four typed mutation probes | Confirmed | No initial-reader weakening or general World admission |
| 16 rows, 24 precompletion cuts and 88 fingerprints | Test loop: 8 empty rows ×4 fingerprints, 8 I rows ×7 fingerprints; state10 in all rows and state11 in eight | Confirmed | Frozen terminal cuts are not counted as C# coverage |
| Focused23 passed; Python covers broader contract | Retained final test log, unchanged five-file hash manifest and baseline oracle log | Confirmed as retained evidence | No independent rerun claimed |
| Fullsuite/three independent rounds pending | Author evidence file | Not treated as passing | Lead retains integration and remaining review gates |

## Verification Performed

- `git status --short`, `git diff --stat`, `git diff`, `git branch --show-current`, `git rev-parse HEAD`: working-tree boundary verified.
- `shasum -a 256 -c .planning/combat-task008-delivery/d1-source.sha256`: all five primary source/test files OK.
- Inspected canonical Reserve/stage/inherited-successor contracts, Reserve schema, release member/history schema, cycle identity/opening-prefix rules, Python initial/designation logic, current C# implementation and focused tests.
- Inspected `/tmp/d1-final.log`: retained focused test run **23 passed, 0 failed, 0 skipped**. Author command: `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --filter-class '*CombatReserveDesignationTests' '-bl:/tmp/d1-final-{}.binlog'`.
- Inspected `/tmp/d-reserve-baseline.log`: retained unchanged Python oracle PASS with counts listed in preliminary ledger. Author command: `PYTHONDONTWRITEBYTECODE=1 python3 docs/specs/verify-combat-reserve-designation-v1.py`.
- No build, test or oracle rerun performed. Source and retained unchanged evidence sufficient for bounded review; avoids interference with lead integration run.

## Open Questions And Residual Risks

- Terminal completion/cycle authority is intentionally absent. D1 readiness must not be interpreted as full Reserve or Task008 acceptance.
- Future broader World profiles must replace/extend closed writer validation and history models together; present hardcoded null/empty fields are safe only under current derived profile.
- Full integration test/format/build status and subsequent independent review rounds remain lead gates, not results independently established here.
- Review began from neutral packet, with preliminary ledger persisted before reading author explanation. No previous review report consulted. CCE recall/search used first; targeted local inspection followed noisy search results.

## Verdict

**Ready** for bounded D1 slice and documented D1/D2/019A execution refinement.

## Recommended Next Actions

Lead completes integration evidence and required remaining independent rounds. Preserve terminal completion/retry/readback and Movement obligations under D2→019A; keep production/public/restore gates closed.
