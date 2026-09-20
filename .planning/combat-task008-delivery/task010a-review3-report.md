# Task010A independent review

Review instance: 3 of 3. Reviewer mode; fresh context.
Base: `6f52d81c5d0ac95a9f0b9799545f304444661dac`.
Candidate: `2eda2edca9f56d8b0d736cf7b8259598255c61ef`.
Branch: `codex/combat-task008-reaction-lifecycle`.

## Preliminary blind ledger — persisted before author/checks read

Bootstrap read first. No prior reviews, aggregate evidence, execution history, or cross-session retrieval read. CCE intentionally disabled; exact local reads used. HEAD and five primary source hashes match dispatch. Initial dirty paths: task010a-checks.md and task010a-evidence.md only; latter not read. Git diff whitespace check passes.

Inspected all five primary paths, relevant certification/retained-history dependencies, canonical plan Task009/010 refinement, selection/no-attack contract text and schemas, oracle entry points, and relevant test helpers. No actionable defect identified in blind pass.

| Concern | Independent source evidence | Preliminary assessment |
| --- | --- | --- |
| Caller state replacing predecessor authority | Selection Replay calls Certification.Admit, which invokes full HistoryReplay; traversal Replay requires reconstructed closed selection. Apply and ReadControl use Replay. | Authority preserved; no cache-only transition entry. |
| Retry bypass or forged effects | Input identity/actor checks precede receipt lookup; exact input hash identifies retry; replay regenerates complete event and compares bytes. | Exact retries retain event bytes and current state; changed commands reject. |
| Incorrect structural closure | Six catalog successors; each version advances once; frozen selection retained; only outer traversal closes. | Matches bounded turn1/stage1/first-slot contract. No release/resource/RNG mutation. |
| Codec shape/bounds/order | Closed recursive grammar, duplicate detection, numeric/string checks, depth/array/byte limits, canonical byte comparison. Local arrays retain semantic order. | Bounds and canonical rejection implemented; run focused malformed-input tests next. |
| Buffer ownership | Created copied before local list access; retained events and returned buffers copied. | Focused adversarial-list and returned-buffer tests present; no concurrent mutation guarantee inferred. |
| Independent positive evidence | Four runtime G2 histories; 28 selection and 60 traversal hash/length commitments; every cut and retry. | Frozen commitments, not stored literal event JSON. Execution still pending. |
| Plan completeness | Canonical plan lines875–882 explicitly split010A/010B/010C. | This slice matches010A; parent010, timed C3a, cumulative router and011 remain open. |

Blind phase ended here. Author/checks read only after this ledger was written.

## Findings

No actionable findings. Full event regeneration, private transition boundaries, frozen nested selection, and exact Control comparison satisfy reviewed causal-authority requirements. Passing focused tests support this assessment; neither prior reviews nor aggregate evidence supplied proof.

## Plan Review

Canonical Task010 refinement at `docs/design/combat-cycle-implementation-plan.md:875` defines this exact slice. Selection admission depends on accepted009A/G2 replay; two System events keep Position Determination unchanged; six System completions advance to same-slot Reserve Release without release action, resource mutation, progress or extra completion event. All four supported side/movement histories are exercised. Frozen schemas/oracles/fixtures have no diff from base. README, technical design, naming and roadmap updates correctly limit scope.

010B timed C3a,010C cumulative router,011 Prepared, extended Snapshot12 and durable publication remain separate gates. Existing generic router correctly remains limited to predecessor families; this slice does not close parent010. No migration, hosting rollout or public API activation introduced. Repeated bounded replay and shared schema descriptors impose known costs but fit current authority model; no speculative refactor requested.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Every authoritative operation reconstructs complete predecessor/local history | Selection Replay/Apply/ReadControl; traversal Replay/Apply/ReadControl; Certification.Admit and HistoryReplay | Confirmed | No caller Control establishes authority. |
| Exact authenticated retries return retained event and current state | Both private Transition methods; input hashes include actor; retry loops at every cut | Confirmed | Changed actor/fields cannot become retry. |
| Two selection events plus six structural completions preserve material state | Event writers, Route, immutable State fields, exact Boundary/selection commitments | Confirmed | No RNG/cost/reset/release side effect. |
| 28 selection plus60 traversal commitments,40 cuts,96 retries | FourActualHistoriesMatchAllEightyEightFrozenCommitmentsAndRestoreEveryCut; independent7/7 run | Confirmed | Commitments are hash/length evidence, not fixture-stored literal JSON. |
| Malformed/corrupt histories, caches and ownership failures covered | Remaining six focused tests; strict recursive codec and replay comparisons | Confirmed within tested cases | Representative C# corruption coverage; exhaustive Python mutations are oracle evidence, not exhaustive C# execution. |
| Existing identity codec remains compatible | Independent CombatIdentityTests14/14 run; external Boundary ordering unchanged | Confirmed within focused regression | No shared-code regression observed. |
| Earlier RED failures and ownership regression reproduced before fix | Author/checks statements only; no old execution history read | Unverified historical claim | Current corrected candidate independently passed; no dependence on historical RED claims. |
| Full suite2265, Boundary81, full format and remote CI passed | Allowed checks metadata only | Not independently rerun/verified | Reported author/root evidence, excluded from independent pass count. |

## Verification Performed

All commands used `login:false`. Build/test commands used approved local IPC. SDK reported10.0.400; repository selects native MTP and xUnit (`xunit.v3.mtp-v2`).

- `shasum -a 256 -c .planning/combat-task008-delivery/task010a-source.sha256`: all five match before and after .NET checks.
- `git diff --check 6f52d81 2eda2ed`: exit0.
- `dotnet build tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --disable-build-servers -p:UseSharedCompilation=false '-bl:/tmp/task010a-review3-build-{}.binlog'`: exit0; zero warnings/errors;7.83s.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatInheritedStepsTests' '-bl:/tmp/task010a-review3-focused-{}.binlog'`: exit0;7 passed, zero failed/skipped;36.350s.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatIdentityTests' '-bl:/tmp/task010a-review3-identity-{}.binlog'`: exit0;14 passed, zero failed/skipped;12.463s.

Binlogs confirmed present:

- `/tmp/task010a-review3-build-20260920-084449--17954--93H52h.binlog`
- `/tmp/task010a-review3-focused-20260920-084508--17985--2bxR2B-dotnet-test.binlog`
- `/tmp/task010a-review3-identity-20260920-084603--18018--7u2nKa-dotnet-test.binlog`

- `python3 -B docs/specs/verify-combat-inherited-no-attack-v1.py`: exit0;4 traces/24 events,28 cuts,24 retries,956 mutations,36 raw negatives,208 boundaries,7 source pins.
- `python3 -B docs/specs/verify-combat-inherited-selection-v1.py`: exit0;4 traces/8 events,12 cuts,8 retries,1126 mutations,46 raw negatives,72 boundaries,6 source pins.
- `dotnet build-server shutdown`: exit0; MSBuild and VB/C# compiler servers shut down successfully. Every launched build/test/oracle process exited0; no review check left running. Final HEAD, source hashes and dirty-path checks unchanged.

## Open Questions And Residual Risks

No blocking questions. Independent checks cover current bounded dormant mechanism and shared identity codec; full solution, Boundary, format and remote CI were not independently repeated. No claim of comprehensive fuzzing, concurrent caller mutation safety, extended Snapshot12 restore, generic successor routing, public action authorization or durable publication. Python deep mutation counts exercise frozen oracle implementations; C# has representative negative tests plus all positive commitments/cuts/retries.

Source and Git stayed read-only; only this review report intentionally edited. Build outputs and unique binlogs came from expressly authorized checks. Original dirty checks/evidence metadata left untouched. No fixes, commits, agents, extra reviews or workstreams created.

## Verdict

**Ready** for bounded Task010A at exact candidate `2eda2edca9f56d8b0d736cf7b8259598255c61ef`.

## Recommended Next Actions

Root owns acceptance and remaining parent-task gates. Review instance3 of3 complete; no further review instance started. Preserve010B/010C/011 and publication/persistence exclusions when recording acceptance.
