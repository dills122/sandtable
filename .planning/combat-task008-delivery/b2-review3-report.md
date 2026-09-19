# B2 stage-entry independent review

Review instance: 3 of 3.

## Preliminary ledger — before author explanation

Verified branch codex/combat-task008-stage-entry and HEAD/base a0babdb157c28d048601e3b3a1cceaa28cfe849c. Working-tree scope matches bootstrap: three new Core files, one new test file, fixture project inclusion, documentation and execution tracking. No canonical fixture/spec/oracle changes.

Canonical requirements, tests, production implementation, predecessor contracts, implementation-plan/execution rows and retained oracle output inspected before author rationale. Context search returned noisy artifact matches; used exact canonical local paths. Recall/search exposed a short B2 evidence heading and predecessor decisions, but no B2 author rationale or prior review findings; blind implementation pass retained.

No actionable defect identified in preliminary pass. Replay reconstructs Weather from creation, validates every transition by canonical regeneration, derives all five positions from sequence catalog, preserves Weather/World/RNG/order, and authenticates System actor before retry lookup. Tests cover 12 frozen chains/60 cuts, cross-order cache rejection, changed retries, re-signed invented successors, raw/scalar mutation and buffer isolation. Plan correctly keeps Reserve execution, generic Snapshot12, publication and public activation separate. Pending verification: focused no-build run and author-claim reconciliation; inspect canonical schema/sequence authority further before verdict.

## Findings

No actionable findings. Five primary-file fingerprints match retained verification manifest. No source, fixture, or legacy-reader changes outside declared implementation scope.

## Plan Review

Ready for bounded B2 acceptance. Canonical Task008 index correctly orders A2 → B1 → C → B2 → D and preserves B1+B2 parent ownership. Implemented four explicit-none edges consume full Weather authority and stop at state10; Reserve designation, atomic first-cycle opening/019A, noninitial Snapshot12/H, actual publication/HOST-PUB-001, and public activation remain explicit later gates. No hidden prerequisite, unsupported scope expansion, or premature parent008 completion found. Lead should update B2/B status only after accepting this report and retaining final checkpoint.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Consequence |
| --- | --- | --- | --- |
| Complete creation/opening/Weather replay establishes state6 | CampaignCombatStageEntry.Replay delegates CampaignCombatWeather.Replay; predecessor requires four opening events; wrong-chain tests | Confirmed | Cache/prefix cannot establish authority |
| Exact four gate policy checked first | RequirePolicy, Replay ordering, policy tests, canonical oracle initial | Confirmed | Empty World cannot imply obligations absent |
| 12 chains, 60 cuts, 228 retained fingerprints | Cases/FrozenTracesMatchEveryCutAndPreserveWeatherAuthority; six fixture seeds × both choices ×19 artifacts | Confirmed | Exact frozen lengths/digests checked across command/event/state and predecessors |
| World/Weather/RNG/order preserved; Reserve side null | SerializeState, Emit, catalog Position, ImmutableFields and final assertions | Confirmed | No random draw or first-side materialization introduced |
| Historical retry returns original event/current state with System admission | Apply authorizes before lookup; every-cut retries and wrong-actor tests | Confirmed | No extra receipt/prefix transition on retry |
| Canonical and causal forgeries reject | ReadEventInput plus full regeneration comparison; ReadState full replay equality; raw/semantic/re-signed/cache tests | Confirmed | Event hash alone insufficient for admission |
| Build, format and full suite passed | /tmp/b2-build.log, empty /tmp/b2-format.log, /tmp/b2-suite.log, verified b2-source.sha256 | Confirmed build/full suite; format exit reported by author, empty log inspected | Reviewer did not rebuild or rerun full gate |
| Dormant scope only | Internal classes; unchanged legacy readers; README/design/naming/roadmap and canonical plan | Confirmed | No public/provider/Snapshot12 readiness implied |

## Verification Performed

- `git status --short`, `git rev-parse HEAD`, `git branch --show-current`: explicit working-tree target verified at a0babdb157c28d048601e3b3a1cceaa28cfe849c on codex/combat-task008-stage-entry.
- `git diff --check`: passed.
- `shasum -a 256 -c .planning/combat-task008-delivery/b2-source.sha256`: all five primary files OK.
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatStageEntryTests' '-bl:/tmp/b2-review3-{}.binlog'`: initial sandbox invocation failed before test execution, local IPC bind Permission denied. Same command with approved escalation passed17, failed0, skipped0, duration5s239ms.
- Read unchanged canonical Python verifier transition/authorization/replay/source-check implementation and /tmp/b2-stage-baseline.log: retained PASS12 traces/60cuts/11052leaf mutations/1299raw rejections/3552boundary checks. Long oracle deliberately not rerun per assignment.
- Independently recomputed fixture sourceHashes using Python hashlib against current files: all12 source fingerprints passed.
- Read retained build log: zero warnings/errors. Read full-suite log:1945 passed,0 failed/skipped. Format log empty; did not independently reproduce format exit status.
- No builds, commits, implementation edits, delegation, or prior reviewer reports. Only this report written. Author/evidence read after preliminary ledger; evidence contains prior-round summaries, which did not inform preliminary assessment.

## Open Questions And Residual Risks

No B2 blocking question. No-build execution necessarily uses retained binaries; matching source fingerprints and retained build/full-suite logs support target consistency. Test mutation exhaustiveness differs from Python oracle but exact goldens and separate semantic/forgery probes cover relevant B2 boundaries. Production authenticated ingress, published-head verification, positive obligations and full campaign capacities remain outside bounded dormant codec proof and assigned to later work.

## Verdict

**Ready** for Task008 B2 bounded acceptance. Implementation and plan agree; no actionable defect or required fix identified.

## Recommended Next Actions

Lead accept B2, retain checkpoint/evidence and update B parent status for B1+B2 only. Continue D against complete state10 history while preserving open Task008/H/019A/publication/public gates. Review instance3of3 complete; no further reviewer started and no conditional experiment/fourth review needed because no blockers remain.
