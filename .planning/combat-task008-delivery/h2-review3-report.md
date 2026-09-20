# H2 independent review

Review instance: 3 of 3.

## Preliminary blind ledger

Inspected frozen four primary paths against base64cd011/head4a43ff4, canonical inherited Snapshot12 contract, Task008 execution index and H refinement, retained-history ownership, and actual Movement/lifecycle/Breakdown readers before author/checks/session recall. Four source hashes match. No prior review reports consulted.

- No actionable defect found in first pass. Count selects bounded candidate partitions; each passes actual reader before successor is reached. Reserve completion typed state disambiguates designation at event10.
- Movement event tag only dispatches candidate; exact re-emission comparison in actual reader rejects same-tag Reaction effects and noncanonical encodings.
- Lifecycle reader enforces mandatory three events; G2 requires actual end proof and rejects extra suffixes. H1 prefixes retain earlier projections.
- Tests compare actual typed serialization with predecessor goldens, all48 E1/E2G1/G2 unique fixture histories, H0 fields and retained event identity. Forged canonical effects, actor changes, chronology attacks, mutable buffers, and legacy-reader boundaries covered.
- Replaying each prefix adds bounded repeated work; existing Movement cap32 and closed selected profile limit cost. No production activation changed.
- Need independent focused run and author-claim reconciliation. Full-root Snapshot12 codec, Reaction routing and disabled-admission restore remain H3/H4; remote exact-head CI not independently observed.

## Findings

No actionable findings. Review target: branch `codex/combat-task008-reaction-lifecycle`, base `64cd011`, HEAD `4a43ff44ecb04c4bbefb5f7cd7d25a86027c1ea9`. Four primary paths match frozen SHA-256 manifest before and after review; scoped working tree clean. Diff contains two router/model changes, adapted H1 positive test, and new H2 test file. No production fixes or Git mutations performed.

## Plan Review

H2 meets bounded acceptance criterion in `docs/design/combat-cycle-implementation-plan.md:820`: one stream derives ordinary Movement, mandatory stop/resolution/completion, then Breakdown completion. Typed Reserve completion supplies causal gate; closed three added projection arms retain underlying actual state without new authority model. Candidate event tags cannot bypass canonical re-adjudication. Existing H1 terminal cuts remain valid, including designated Reserve and unsupported-for-Movement Weather cuts.

Tests cover both sides and every prefix for selected 1/5/6/7-move lifecycle traces, exact existing event/state goldens, frozen H0 current fields, all48 distinct assigned histories, self-consistent typed forgeries, omission/reorder/duplicate/foreign effects, raw canonical negatives, retained bytes and projection isolation. Test generation uses actual readers, but independent frozen predecessor hashes and H0 vectors prevent reliance on implementation self-comparison alone.

README, tech-design and naming-overview preserve dormant scope and H3/H4 boundaries. Execution ledger identifies H2 active; canonical plan remains owner of dependencies. H2 acceptance/status updates belong to lead after gate completion. No architecture pivot, added workstream, migration, public admission or host publication needed for this child. Parent Initial H remains open until Reaction routing/full-root codec/actual disabled-admission restore complete.

## Author-Claim Reconciliation

| Claim | Evidence inspected | Status | Consequence |
| --- | --- | --- | --- |
| Typed completion disambiguates event10 designation/completion | HistoryReplay reserve loop, strict Reserve reader call, designation negative test | Confirmed | No fixed-count admission bug |
| Same-tag Reaction cannot pass ordinary Movement | Strict E1 re-emission equality; actual F1 trigger negatives for both sides | Confirmed | Family tag remains dispatch hint |
| Mandatory lifecycle gates and complete G2 suffix | Lifecycle Replay count/Emit checks; G2 requires three lifecycle events and at most one completion | Confirmed | No swallowed/unchecked tail |
| 48 histories /64 selected vectors | Independent fixture inventory: E1=16, E2G1=32, G2=16; unique identities48; exhaustive C# set test | Confirmed | Proportional selected-profile coverage |
| Existing H1 tests retained; focused70 pass | Diff plus independent filtered run70/0/0 | Confirmed | Pre-cycle regression coverage retained |
| Owned transcript and typed state resist caller/export mutations | RetainedHistory capture/copy implementation; new every-stage mutation test | Confirmed | No exposed byte authority alias |
| Full-root bytes/disabled-admission restore remain later work | Canonical H refinement, docs, actual router result shape | Confirmed | No parent or runtime activation claim |
| Canonical comparison authenticates | Code proves history-derived canonical effects; separately trusted retained commitment remains caller responsibility, acknowledged elsewhere in author packet | Confirmed only as causal validation | Do not treat canonical hashes as storage authentication |
| Full local gates passed | Raw `/tmp/h2-suite.log`2174/0/0, `/tmp/h2-boundary.log`81/0/0, build log0errors; format exit0 reported in checks | Confirmed for raw test/build logs; format exit is retained lead testimony | Focused rerun supplies independent execution |
| Exact-head remote CI | Lead reports running | Unverified here | Lead-owned integration gate remains |

## Verification Performed

- Read neutral bootstrap and independent-review skill; blind primary source/tests/requirements pass recorded above before author/checks/session recall. No prior H2 reports or aggregate H2 verdicts read. Later targeted execution-ledger read exposed historical other-slice status only; no role in verdict.
- `git rev-parse HEAD`, branch inspection, scoped diff/status, `shasum -a 256 -c .planning/combat-task008-delivery/h2-source.sha256`: correct branch/head, clean reviewed paths, all four hashes pass.
- `git diff --check 64cd011 4a43ff4 -- src/Cna.Core/Campaigns/CampaignCombatHistoryReplay.cs src/Cna.Core/Campaigns/CampaignCombatHistoryModels.cs tests/Cna.Core.Tests/Campaigns/CombatHistoryReplayTests.cs tests/Cna.Core.Tests/Campaigns/CombatMovementHistoryReplayTests.cs`: exit0.
- Independent Python read-only fixture inventory:64 vectors,48 exact history identities; no oracle or fixture regeneration.
- Native MTP/xUnit configuration checked in global.json, test csproj, shared props/packages; installed SDK `10.0.400`, allowed roll-forward from10.0.302.
- Independent command: `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*HistoryReplayTests' '-bl:/tmp/h2-review3-{}.binlog' > /tmp/h2-review3-focused.log 2>&1` (login:false, escalated local IPC): exit0,70passed/0failed/0skipped,31s322ms. Log `/tmp/h2-review3-focused.log`; binlog `/tmp/h2-review3-20260920-051553--1009--oUDSjn-dotnet-test.binlog` exists. Process session34700 observed complete before report.
- Read retained full/build/boundary logs without rerunning unchanged cumulative suites. No-build run uses lead-built binary; source hashes unchanged and lead build precedes independent run. Remote CI not claimed executed by reviewer.

## Open Questions And Residual Risks

Bounded repeated prefix replay is quadratic but accepted Movement cap32 and selected small histories bound cost; no unrestricted-history scalability claim. New routing intentionally rejects Reaction and unsupported profiles. Legal shorter prefixes are valid; external trusted head/commit authentication belongs outside this router. Full literal Snapshot12 serialization, disabled fresh admission, durable host restart and publication proof remain separate gates.

## Verdict

Ready for H2 bounded scope. Remote exact-head CI remains lead-owned integration gate, not evidence of completed parent restore.

## Recommended Next Actions

Lead finish exact-head CI and reconcile H2 status/evidence. No blocker-driven experiment, additional review, fix, or new workstream requested. Review instance3of3 complete; reviewer work and all launched test processes closed.
