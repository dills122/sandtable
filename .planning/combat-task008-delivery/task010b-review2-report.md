# Task010B independent review

Review instance: 2 of 3. Reviewer mode; fresh context.
Base: 6e6dd6fad206e886f2ceeba5c9c031158e5fdfa7.
Candidate: 762468e5a2744957b5b720905b45d455721d3037.
Branch: codex/combat-task008-reaction-lifecycle.

## Preliminary blind ledger

Persisted before opening task010b-author.md or task010b-checks.md. No prior reviews, aggregate evidence, execution history, worker/dev notes, proposal rationale, cross-session retrieval or agents used. Bootstrap read first. Exact five source hashes passed; HEAD equals candidate. Initial dirty state: task010b-checks.md and task010b-evidence.md only (contents not inspected during blind pass).

No actionable defect established in blind pass. Evidence inspected: all five source/test files; C3a prose, schema and oracle transition/boundary/readback logic; C2 creation identity contract; DES003 transition requirements; canonical Task010 row and A/B/C refinement; existing009B certification and immutable participant/candidate models; candidate README/design/naming/roadmap diff.

| Area | Preliminary judgment | Follow-through |
| --- | --- | --- |
| Trusted boundary and replay | Exact C2 creation pinned; existing009B checks current profile; separately supplied inputs re-create each event. Event.input never establishes authority. | Run frozen oracle and focused tests. |
| Lifecycle and timing | Selection/decline ownership, command-only retries with separate actor check, stale timer no-op, fixed deadline/high-water and pre-RBA cancellation match frozen transition. | Execute clock, forgery and lifecycle tests. |
| Six steps and positive stop | No-attack closes six steps; selected path requires decline then stops at FA. No authoritative World/RNG update returned. | Check both-side derived commitments and literal traces. |
| Canonical bytes and ownership | Closed schema, primitive checks before context, complete event comparison and copied result/history buffers. Shared Identity bridge defaults preserve predecessor behavior. | Run Identity/InheritedSteps regression because bridge changed. |
| Evidence quality | Tests distinguish 41 original literal events/five final Controls from 82 derived mirror events/92 cuts. Intermediate Control readback is derived, not another literal oracle. | Compare author claims without promoting provenance. |
| Plan | 010B scope fits refinement; 010C routing and011 Prepared remain open. Fragment replay cannot close parent010, authenticate actual positive history or establish extended Snapshot12. | Confirm author claims retain limits. |

Residual checks before verdict: author reconciliation; focused native-MTP execution with unique binlogs; frozen oracle; final source/status integrity and process completion. No fixes or source/Git writes authorized.

## Findings

No actionable findings. Blind observations survived author reconciliation and independent checks. No source fixes proposed.

Scope verified against exact base/candidate and five source pins. Candidate changes comprise five primary source/test paths, four supporting documentation paths and delivery metadata. Excluded metadata bodies were not inspected. Source, frozen schema/fixtures/oracles and Git remained unchanged by review.

## Plan Review

Canonical source: docs/design/combat-cycle-implementation-plan.md:859 and :875. Task010B implements the specified dormant C3a mechanism, not the whole Task010 outcome. Task010A actual no-attack families pre-exist; cumulative routing remains010C; Prepared remains011. Dependency on009B is explicit through CertifyInitialProfileFacts. No scope expansion, new public action, new timing policy, authoritative grain I/O, persistence implementation or competing certification system found.

CampaignCombatSelectionSteps.ValidateBoundary pins the compatible C2 request and validates current facts through009B. Replay captures Created and event buffers, validates syntax, then re-creates each event from independent Boundary and accepted Inputs. Transition checks actor separately from command digest before duplicate readback; rejected player clocks cannot fabricate a decline. Fold advances one version/prefix per event, retains cancelled selection evidence and cannot complete positive FA. Control receipts and event results own their storage; immutable existing Candidate/Participant values carry selection bindings.

Codec Shapes match frozen inventory and all reachable external descriptors. The Identity bridge enables C3a semantic array order and rejects frozen forbidden Route/LegacyBrokenVehicleLot arms, while predecessor calls keep default ordering. Tests exercise both syntax-before-context and semantic rejection after valid syntax.

README, tech-design, naming-overview and roadmap correctly retain dormant/in-progress status and future gates. This review supplies no parent010 closure, actual positive-history authentication, live admission, public privacy projection, extended Snapshot12, durable publication or process-restart proof. No new plan workstream or additional review instance initiated.

## Author-Claim Reconciliation

Author/checks read only after preliminary ledger persisted above.

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Separate trusted inputs authorize replay; event payload cannot authenticate itself | Replay, ReadControl, rehashed-forgery and wrong-input tests | Confirmed | Trust boundary preserved within caller-trusted dormant API. |
| Exact C2 pin and initial profile except CP | ValidateBoundary; CertifyInitialProfileFacts; foreign-request/current-world tests | Confirmed | Matching names/hashes alone cannot broaden profile. |
| Fixed historical timing, actor-checked Command retries, stale callback no-op | Transition, ClockGate, AdmittedTiming; frozen oracle transition; clock tests | Confirmed | No Round2 opening-floor policy imported. |
| Cancellation fabricates neither decline nor resource debit; positive stop at FA | Fold/step guards; unavailable-before-opening, six-step and FA tests | Confirmed | Prepared/live continuation remains outside scope. |
| Mandatory closed schema, immutable values and copied buffers | Codec; Models; Participant constructor; ownership/sentry tests; independent49-descriptor comparison | Confirmed | No generic trust-by-deserialization path found. |
| 41 literal events/five final Controls; 46 cuts including starts | FiveSyntheticLiteralTracesMatchEveryEventFinalControlAndRestoreCut; frozen oracle | Confirmed | Intermediate Controls are derived readbacks, not independent literals. |
| Supplemental both-side mirrors:82 events/92 cuts; five Commonwealth commitments | BothRoleMirrorsMatchIndependentSupplementalOracleCommitments | Confirmed | Axis repetitions and mirrors are not additional original literal traces. |
| Final focused12 and shared21 pass | Independent runs below | Confirmed independently | Current retained test outputs pass both relevant groups. |
| Root build, full2277, Boundary81, format and exact-candidate CI green | task010b-checks.md testimony only | Not independently rerun/verified | Not used as independent passing evidence; root retains broader gate/acceptance ownership. |
| Earlier RED/correction history | Author/checks narrative only | Not independently verified | Review assesses final frozen implementation; no historical RED claim adopted. |

## Verification Performed

All shell invocations used login:false. Native MTP/xUnit setup confirmed from global.json, project and shared build/package properties. Test execution used approved local IPC; no build or restore requested. Unique binlogs exist.

1. `shasum -a 256 -c .planning/combat-task008-delivery/task010b-source.sha256`: all five OK before and after checks. HEAD remained762468e5a2744957b5b720905b45d455721d3037. Initial/final tracked dirty state contains only task010b-checks.md and task010b-evidence.md. This report is locally excluded from Git status; its file was explicitly persisted/read back.
2. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatStepsTests' '-bl:/tmp/task010b-review2-focused-{}.binlog'`: exit0;12 passed,0 failed,0 skipped;3.723s. Binlog: `/tmp/task010b-review2-focused-20260920-092859--20909--XcBMHK-dotnet-test.binlog`.
3. `/opt/homebrew/bin/python3 -B docs/specs/verify-combat-selection-steps-v1.py`: exit0;5 literal traces,41 event/control cuts,246 event mutations,164 raw rejections; retry/clock/unavailability/RBA/FA checks passed. Oracle expressly reports isolated boundary probes, no full campaign replay.
4. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatIdentityTests' --filter-class '*CombatInheritedStepsTests' '-bl:/tmp/task010b-review2-shared-{}.binlog'`: exit0;21 passed,0 failed,0 skipped;40.903s. Binlog: `/tmp/task010b-review2-shared-20260920-092923--20931--Q3AjFF-dotnet-test.binlog`.
5. Independent `/opt/homebrew/bin/python3 -B -` inline read-only descriptor audit: imported frozen C3a oracle, extracted C# descriptor strings with regex, traversed Boundary/Input/Command/Event/Control and Effect tags recursively, asserted exact field/type order against oracle SCHEMA. Exit0;49 reachable object descriptors matched. No script file or bytecode written.
6. `git diff --check 6e6dd6fad206e886f2ceeba5c9c031158e5fdfa7 762468e5a2744957b5b720905b45d455721d3037 -- src/Cna.Core/Campaigns/CampaignCombatSelectionStepsModels.cs src/Cna.Core/Campaigns/CampaignCombatSelectionSteps.cs src/Cna.Core/Campaigns/CampaignCombatSelectionStepsCodec.cs src/Cna.Core/Campaigns/CampaignCombatIdentityCodec.cs tests/Cna.Core.Tests/Campaigns/CombatStepsTests.cs README.md tech-design.md naming-overview.md docs/roadmap/pre-alpha-roadmap.md`: exit0, no output.
7. Both test sessions and Python oracle returned final exit0. Sandboxed process-list probe failed because sysmond unavailable; same read-only `pgrep -fl 'dotnet|Cna.Core.Tests|verify-combat-selection-steps-v1'` with approved access returned exit1/no matches. No review process left running.

## Open Questions And Residual Risks

No blocking open question within010B. Independent test execution intentionally used retained build output via authorized --no-build command; no fresh compilation, full suite, formatter or remote CI check performed. Current source was reviewed and hashes verified, but no separate binary-to-source attestation performed. Descriptor duplication remains a maintenance cost; current parity is verified. Trusted caller authentication and real history composition remain later integration obligations, not capabilities supplied by this fragment.

## Verdict

Ready — bounded Task010B candidate only. Review instance2of3 complete; root controls review budget, broader gates and acceptance.

## Recommended Next Actions

Return this report to root for acceptance reconciliation. Keep010C,011 and actual-publication/history gates open. No additional review or workstream initiated.
