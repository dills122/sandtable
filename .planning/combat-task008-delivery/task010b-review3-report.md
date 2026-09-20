# Task010B independent review

Review instance: 3 of 3. Reviewer mode; fresh independent context.
Base: `6e6dd6fad206e886f2ceeba5c9c031158e5fdfa7`.
Candidate: `762468e5a2744957b5b720905b45d455721d3037`.
Branch: `codex/combat-task008-reaction-lifecycle`.

## Preliminary blind ledger — persisted before author/checks read

No actionable finding established in blind source/requirements pass. Final verdict pending claim reconciliation and independent checks.

Scope verified: HEAD matches candidate; all five entries in task010b-source.sha256 pass. Initial dirty files only task010b-checks.md and task010b-evidence.md. Aggregate evidence, prior reviews, worker/developer notes, proposals, execution history and cross-session retrieval not read. CCE intentionally disabled. Source/Git remain read-only.

Inspected all five source/test files, applicable AGENTS.md, canonical Task010 row and A/B/C refinement, frozen selection prose/schema and transition oracle, existing009B certification/identity models, and supporting documentation diff. Extra changed files are review/workflow metadata; excluded material not read.

| Question | Blind evidence and preliminary assessment |
| --- | --- |
| Replay authority | Replay captures Created/events, requires paired separately trusted inputs, re-derives each transition and compares exact bytes. Imported event.input cannot grant actor/time authority. Boundary API explicitly retains caller-authenticated provenance. |
| Profile and mutation | CompatibleCreation pins exact retained C2 request; existing009B certifies World except allowed integral CP. Position, RNG provenance, Weather scope and cycle checked. Transitions produce control/events only; no World/resource/RNG mutation found. |
| State machine | Timed choice, explicit defender decline, pre/post-window cancellation, six empty/cancelled closures and positive Force Assignment stop match frozen transition oracle. No public/host routing added. |
| Retries and timing | Command-only digest plus actor check precedes live-state checks; stale callbacks no-op. Opening/deadline/high-water rules match historical C3a, including regression/unavailable fallback. |
| Codec and bounds | Closed canonical shapes, byte/depth/array limits, raw syntax before trusted-context access. Shared Identity bridge opts into semantic array order only for C3a; Route/LegacyBrokenVehicleLot explicitly refused there. Regression checks still needed. |
| Tests and evidence quality | Tests compare 41 original literal events and five final Controls; intermediate control readbacks are derived. Both-side mirrors use supplemental final commitments, not new literal fixtures. Hostile bytes, rehashed effects, actor retries, time edges, unavailable traversal and owned buffers covered. |
| Plan |010B matches frozen synthetic C3a mechanism scope.010C routing and011 Prepared remain distinct; parent010 and extended Snapshot12 cannot close from this change. Documentation retains those limits. |

Remaining checks: independently rebuild; run CombatSteps plus shared Identity/InheritedSteps regressions; run frozen oracle; verify supplemental commitments and source freeze. Diagnostic messages are plain JsonException, consistent with surrounding runtime codecs; no new outward diagnostic contract is activated.

## Findings

No actionable findings. Blind concerns resolved by source/oracle comparison and checks below. No source correction or plan pivot proposed.

## Plan Review

Canonical `docs/design/combat-cycle-implementation-plan.md:859` and refinement at875–883 define this bounded child. Implementation covers010B's timed provisional choice, explicit defender decline, expiry/unavailability cancellation, six no-attack closures and positive stop at Force Assignment. Existing009B certification is reused; no new gameplay profile or contract introduced. Five-primary-file boundary is preserved.

Dependencies and deferrals are coherent:010A owns actual inherited empty history;010C owns cumulative routing;011 owns Prepared. Parent010, checkpoint E, live admission, full Snapshot12 and publication are not proved by010B. README/roadmap retain010B in progress; design/naming describe dormant trusted-boundary APIs without claiming acceptance. No rollout or migration needed for unregistered internal mechanism. Root retains acceptance and budget ownership.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Separately trusted inputs authorize replay; events cannot authenticate themselves | `CampaignCombatSelectionSteps.Replay`, `Transition`; rehashed-forgery and wrong-input tests | Confirmed | Every event compared with recomputed bytes; caller-supplied Control never authorizes transitions. |
| Fixed C2 request and009B profile | `ValidateBoundary`, request constructor's full canonical digest, `CertifyInitialProfileFacts` | Confirmed | Compatible creation pin binds full request; unsupported current World/profile rejects. |
| Historical clock/retry policy | Frozen Python transition versus C# `Transition`, `ClockGate`, `AdmittedTiming`; deadline/retry tests | Confirmed | Actor checked on retained Command retry; invalid primitives still reject before retry; stale callbacks cannot cancel another window. |
| Owned values and no resource/RNG mutation | Model copy constructors, immutable participant/World data, event getters; owned-buffer tests | Confirmed | Replay captures Created before hostile list callbacks, captures event bytes, preserves boundary values. |
| Frozen grammar and narrow shared-code bridge | Schema/oracle comparison:49 reachable descriptors; C3a primitive traversal; Route/order sentry tests | Confirmed | Default inherited array checks remain enabled; C3a preserves semantic order and forbids external Route/LegacyBrokenVehicleLot values. |
|41 literal events/five literal final Controls; mirrors supplemental | Fixture tests and independently recomputed five Commonwealth final hashes | Confirmed | Intermediate Controls and mirrored events remain derived evidence; no inflation to additional original literals. |
| Rebuild/focused success | Independent build and12 CombatSteps tests | Confirmed independently | Current source freeze compiled and exercised. |
| Root full2277/Boundary81/format/CI success | `task010b-checks.md` testimony only | Not independently verified | Not used as substitute for reviewer checks; no aggregate evidence or prior review reports read. |
| No actual positive-history/Snapshot12/publication proof | Internal API and plan/documentation scope | Confirmed | Future gates remain open; Ready verdict below applies only to010B mechanism. |

## Verification Performed

All commands used `login:false`. .NET build/test commands used approved local IPC and unique `/tmp` binlogs. SDK reported10.0.400; project uses net10.0, xUnit v3 MTP-v2, native MTP selected by global.json.

- `shasum -a 256 -c .planning/combat-task008-delivery/task010b-source.sha256`: all five pass before and after checks. HEAD remains exact candidate.
- `git diff --check 6e6dd6fad206e886f2ceeba5c9c031158e5fdfa7 762468e5a2744957b5b720905b45d455721d3037`: exit0.
- `dotnet build tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --disable-build-servers '-bl:/tmp/task010b-review3-build-{}.binlog'`: exit0, zero warnings/errors,8.30s. Binlog `/tmp/task010b-review3-build-20260920-093338--21279--6hJ9B3.binlog`.
- Initial test command: `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --disable-build-servers --filter-class '*CombatStepsTests' '*CombatIdentityTests' '*CombatInheritedStepsTests' '-bl:/tmp/task010b-review3-tests-{}.binlog'`: exit5, zero tests,468ms. Single-class isolation with same optional flag and `-bl:/tmp/task010b-review3-focused-{}.binlog` also exit5, zero tests,126ms. Neither counted as a pass. Binlogs retained with suffixes `20260920-093410--21328--oxNTh1-dotnet-test.binlog` and `20260920-093425--21338--VJuyvd-dotnet-test.binlog` respectively.
- Removing optional build-server flag and using exact bootstrap invocation restored discovery: `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatStepsTests' '-bl:/tmp/task010b-review3-focused-standard-{}.binlog'`: exit0,12 passed,0 failed/skipped,3.705s. Binlog `/tmp/task010b-review3-focused-standard-20260920-093439--21347--qmZ3fw-dotnet-test.binlog`. This isolates invocation-specific zero discovery; no source change made.
- `/opt/homebrew/bin/python3 -B docs/specs/verify-combat-selection-steps-v1.py`: exit0; five literal traces,41 event/control cuts,246 mutations,164 raw rejections and timing/retry/RBA/FA checks. Oracle evidence remains synthetic contract evidence.
- Inline read-only Python check imported frozen selection oracle, recursively compared reachable descriptors in both C# codecs:49/49 exact matches. Primitive grammar additionally inspected against authority-envelope oracle.
- Inline read-only Python check invoked frozen `mirror_steps`, then replayed each Commonwealth mirror through selection oracle: all five test commitments reproduced exactly (`780315d6…`, `8aec8126…`, `4650da67…`, `ffd20b2d…`, `1e61e714…`). No fixture generated or modified.

- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatIdentityTests' '*CombatInheritedStepsTests' '-bl:/tmp/task010b-review3-shared-{}.binlog'`: exit0,21 passed,0 failed/skipped,41.233s. Binlog `/tmp/task010b-review3-shared-20260920-093507--21363--EtZpp+-dotnet-test.binlog`.

## Open Questions And Residual Risks

No blocking open question for010B. Independent checks are focused; full solution suite, full format and remote CI not rerun or independently inspected. Shared synthetic oracle/fixture lineage remains a limit. Trusted Boundary and Input are internal admission prerequisites, not authentication APIs for untrusted callers. Later integration must establish genuine predecessor provenance and preserve unsupported-profile rejection. Duplicate descriptor text carries maintenance cost but independently matches frozen schema now; no generic serializer refactor requested.


## Verdict

**Ready** — bounded dormant010B mechanism at exact candidate762468e5a2744957b5b720905b45d455721d3037. No actionable findings; code and canonical plan satisfy scoped review. This is reviewer judgment, not automatic root acceptance or parent010 closure.

## Recommended Next Actions

Return report to root for acceptance under existing budget. Review instance3of3 exhausted; no further review, agent, fix, commit or workstream started. All started build/test/oracle processes completed. Final elevated process inventory found no remaining .NET/test/build-server process. Source/Git unchanged; only authorized report authored, plus authorized generated build/test outputs and unique temporary binlogs. Existing dirty checks/evidence metadata preserved; evidence content never read.
