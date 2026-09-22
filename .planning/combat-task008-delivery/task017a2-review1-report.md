# Task017A2 independent review

Review instance: 1 of 3.
Base: `a1cd42525f0f0c055f62270306d1e8ed832054d2`.
Candidate: `3811202ee23b3ef5c972b85953f19ca77759b36f`.
Reviewed detached clone: `/tmp/task017a2-review1` (clean before and after checks).

## Findings

No actionable findings. Blind preliminary ledger was persisted in `task017a2-review1-preliminary.md` before reading `task017a2-author.md`; author explanation was not available during initial review. No prior reports, CCE/memory, implementation check/dev material, additional reviewers, source edits or commits used.

Evidence supports bounded isolated lifecycle:

- `CampaignCombatReserveRelease.cs:9–37` owns/copies persisted event bytes, validates raw shape before retained context, reexecutes separately admitted inputs, compares complete canonical events, and reconstructs prior state for every Apply. Caller-supplied state never authorizes a transition.
- `Transition` checks command identity/shape/actor before retry; exact command plus actor returns original receipt before version/terminal rejection. Deadline equality rejects owner input; accepted clock high-water changes without renewing budget. Opening clock failure never fabricates timing. Fallback locks canonical conversion or bulk retention and preserves earlier accepted choices and original admitted actor.
- Initial base validators remain unchanged. Lifecycle conversion to II and pending Movement exception are handled by derived-state replay rather than weakening A1 admission. Release preserves expenditure, retained World hash/RNG, attack history and earlier release history.
- Codec matches frozen schema field order, primitive types and bounds, then recovered state compares full replay-derived bytes. Immutable owned collections and copied result bytes prevent returned-value mutation from affecting prior state.
- `CombatReserveReleaseTests.cs:34–86` independently builds isolated base recipe, excludes four historical Result1 rows, asserts 44 traces, 132 event hashes, 176 full-state hashes/lengths and two literal terminal events. Focused negative tests exercise deadline/regression/overflow, original actor, fallback lock, duplicate/changed-stale commands, malformed bytes, forged state/event/input history, ordered capacity and ownership.

## Plan Review

Exact implementation manifest honored: three source paths and one test path, plus root plan and administrative documentation. No frozen schema/fixture changes or generated churn. Accepted A1 prerequisite retained. Task017B adapter, positive held-I/inherited-lineage work, Movement/cycle control, Snapshot/durable publication and public admission remain explicit later gates; parent017 remains open.

Plan implementation status is transiently stale (`implementation and acceptance pending`) while README/design say implemented with acceptance checks pending. This does not obscure scope or falsely assert acceptance; update normal status bookkeeping when final gates complete. No architecture pivot or added delivery workstream warranted.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Four source/test paths; frozen fixtures unchanged | Exact base-to-candidate diff, dispatch | Confirmed | Scope controlled |
| 44/132/176/two native parity proof; four Result1 rows excluded | Golden test lines34–86; independently executed16 focused tests | Confirmed | Isolated success criteria covered |
| Separately trusted base/request and admitted history; no caller state | Replay/Apply, ReadState, recovery mutation tests | Confirmed | Trust boundary explicit; no authentic lineage inference |
| Fixed budget, ordered fallback, actor-preserving receipts, explicit completion | Transition against frozen oracle; timing/retry tests | Confirmed | State machine aligned with frozen contract |
| A1 validators unchanged, state validation remains replay-derived | Codec diff begins after existing base helpers;16 tests include A1six | Confirmed | No relaxation of initial history admission |
| Owned state/result collections; atomic capacity | Model init copies/result byte copies, capacity test, final SerializeState before return | Confirmed | No mutable output alias found |
| Meaningful development RED and previous formatting results | Author testimony only; development records intentionally excluded | Unverified | No reliance for readiness; current native tests independently pass |
| Full gates/CI and three reviews pending at freeze | Author packet and status docs | Confirmed as declared limitation | This report does not replace remaining acceptance gates |

## Verification Performed

All commands run with `login:false` in detached clone, except report writes to owned integration paths.

1. `git rev-parse 3811202`, `git diff --stat a1cd425 3811202`, `git status --short`: exact frozen candidate established; clean source tree.
2. `git diff --check a1cd425 3811202`: exit0, no diagnostics.
3. Initial `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class '*CombatReserveRelease*' --no-restore`: exit1, no test projects found before fresh-clone restore; not test evidence.
4. `dotnet restore tests/Cna.Core.Tests/Cna.Core.Tests.csproj`: initial sandbox failure NU1900 (api.nuget.org DNS). Approved retry exit0; Core and test dependencies restored.
5. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class '*CombatReserveRelease*' --no-restore`: sandbox attempt terminated134 due named-pipe SocketException13. Approved identical retry **passed16, failed0, skipped0**, duration3.350s. Ten lifecycle tests plus six A1 base tests; clean clone compilation included.
6. `python3 docs/specs/verify-combat-reserve-release-v1.py`: exit0, **13 literal cases/48 side-slot traces;188 cuts,2368 mutations,840 raw rejects,20 timing checks,27 boundary checks**. This oracle includes historical rows; native A2 claim remains44 isolated rows only.
7. Final `git status --short`: empty. All owned foreground commands completed. No global build-server shutdown.

Full-solution test/build, Boundary suite, format and exact CI were not independently run by this reviewer; root/reviewer3 own those gates. No passing claim made for them here.

## Open Questions And Residual Risks

No blocking open question within A2 scope. Caller must independently admit base/request/input history; candidate-derived expected values do not authenticate campaign lineage. Replay-on-Apply is bounded34events but full active-campaign performance is not measured. Closed in-code shape inventory duplicates frozen schema, mitigated by full golden parity and negative tests. Actual World projection, native settled adapter and positive provenance remain unproved by this slice.

## Verdict

**Ready** for frozen Task017A2 isolated lifecycle implementation and plan. This bounded engineering verdict does not mark all administrative acceptance gates complete, establish authentic campaign lineage, activate public gameplay or close parent017.

## Recommended Next Actions

Complete already-planned remaining reviews/full gates/exact CI, then refresh acceptance evidence and status. No source correction requested by review1.
