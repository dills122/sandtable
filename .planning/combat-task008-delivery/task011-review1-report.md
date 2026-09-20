# Task011 independent review

Review instance: 1 of 3. Reviewer mode. Base: `8108ccd40f84d12135f17290c25bf3d268efbd8f`; branch: `codex/combat-task008-reaction-lifecycle`. Target: five files in `task011-source.sha256`, plus current README, roadmap and tech-design scope changes. Initial HEAD equals base; source manifest checked successfully. Source/Git read-only; only this report written.

## Preliminary blind ledger — persisted before author/check packets

No actionable defect established from blind source/test/canonical-plan inspection.

| Concern | Independent evidence | Preliminary assessment |
| --- | --- | --- |
| Separate input provenance | `CampaignCombatSealedRound.Replay` validates/copies event bytes, serializes separately supplied trusted inputs, replays predecessor independently, then compares newly derived full event bytes. `AuthenticateBase` requires selected decline and Force Assignment. | No event.input self-authentication found. Trusted Boundary remains explicitly synthetic. |
| Base2 and original creation/config | `AuthenticateBase`, existing `CampaignCombatSelectionSteps.ValidateBoundary`, `ReadBase`; exact retained creation binding plus Created11 validation; exact canonical Base comparison. | Supplemental v2 configuration derives original configuration hash and budget without rewriting Config1. |
| Private clock leak | `Transition` checks own slot before `Gate`; immutable timing; no accepted-time high-water update. | 4000 then3500 accepted after opening3000; opening1999 independent of RBA2001. Tests pair both sides/orders and invalid proposals. |
| Retry and races | Receipt recovery before status/time gates; actor check; Prepared callbacks no-op; changed allocation rejects. | Consistent with Round2 contract. |
| Prepared and cancellation | `complete-step` advances Prepared only through index5; cancelled closes at6. World bytes/RNG unchanged, history/use arrays empty; commit throws. | Task012 correctly excluded. |
| Strict grammar before context | `ReadBase`/`ReadState` validate raw syntax first; replay validates all raw events before indexing trusted inputs. Shared grammar adapter has closed allowlist. | Tests include sentry inputs, malformed nested values, bounds, canonical order and rehashed effects. |
| Literal evidence | `AllTenBasesFiftyFourEventsAndSixtyFourStatesMatchFrozenPrecommitLiterals` compares literal states/events and rejects four commit suffixes. | Correctly bounded 10 bases/54 events/64 states; not58/68 implemented effects. |
| Plan and scope | Canonical Task011/012, Round2 spec/schema/oracle, protocol acceptance criteria and three scope-doc diffs. | Dormant mechanism matches Task011. Public revision projection, positive campaign provenance, Snapshot12 and publication remain later gates. |

Pending independent verification: Python Round2 oracle running; .NET prohibited until `/tmp/task011-after-boundary-20260920` exists (absent at last check). Need author-claim reconciliation, focused tests after clearance, final source-manifest check and final verdict. No author explanation, check packet, prior review, aggregate evidence, devnotes or cross-session retrieval read before this ledger.

## Findings

No actionable code or plan findings identified in frozen Task011 scope.

## Plan Review

Canonical `docs/design/combat-cycle-implementation-plan.md:860` allocates slots, exact retry, pinned deadlines and Prepared continuation to011; line896 allocates debit/commit to012. Implementation respects this split. `CampaignCombatSealedRound.cs` reconstructs rather than accepts caller prior state, preserves role-ordered proofs, prevents Prepared expiry and closes cancellation without resource effects. Strict raw reader boundary and separate trusted input provenance are appropriate for dormant Core authority.

Plan phrase “preserves other-side revision” is supported here only at dormant mechanism/witness level. No audience revision mapping or public adapter exists; Round2 contract expressly reserves public projection, and current tech-design states that limit. Positive synthetic C3a fixture must not be represented as actual campaign provenance. Actual positive history, extended Snapshot12, Task012 commitment and HOST-PUB-001 remain explicit parent/integration obligations. No heavy pivot or extra workstream needed for this bounded change.

## Author-Claim Reconciliation

| Claim | Evidence | Status / consequence |
| --- | --- | --- |
| Independent Created, Boundary, predecessor and round input authority | `AuthenticateBase`, `Replay`, existing `ValidateBoundary`; provenance and rehash tests | Confirmed statically; local event.input cannot supply trusted actor/time. |
| Closed syntax before context | `ReadBase`, `ReadState`, event preflight; sentry tests | Confirmed. Syntax admission alone intentionally does not imply lifecycle validity; full replay rejects commit effects. |
| Seventeen exact descriptors / shared external grammar | Independent Python dictionary equality against Round2 schema; narrow external-kind allowlist | Confirmed, 17/17. |
| Owned buffers and collections | Created/event copies, cached Boundary/World bytes, read-only collection copies and result-byte cloning; ownership test | Confirmed for exercised API boundary. |
| Fixed opening floor and private accepted times | Immutable timing, `Gate`, own-slot validation before gate, no private high-water input | Confirmed; confidence source remains independently trusted and host monitoring deferred. |
| Exact retries before clock/status, primitive validation first | `Transition` serialization, command/actor checks, receipt lookup; all-cut retry tests | Confirmed. Prepared and cancelled callbacks recover exact receipts before no-op logic where applicable. |
| 10 bases /54 events /64 states, four commits excluded | Independent fixture count; literal test and explicit commit rejection | Confirmed. Python full-contract68cuts is not C# precommit64-state evidence. |
| 192 fresh outcomes and96 retries | Both-side/order clock matrix test and asserted counters | Confirmed in test source; runtime confirmation pending clearance. |
| No resource/history/RNG effects and no activation | State serialization retains canonical base World/Random, empty history/use, null commitment; unsupported commit throws; no host registration changes | Confirmed. |
| Author-reported full/scoped gates | Supplied check packet only | Not independently rerun at this stage; not used as reviewer-executed evidence. |

## Verification Performed

- `shasum -a 256 -c .planning/combat-task008-delivery/task011-source.sha256`: all five paths OK before inspection and after root commit `94a4ecd99054d11a36350d9ec8fba55c9b5363bb`.
- Manifest file SHA256: `ea407cc67f571124bb8bec05cf24fc0f3eebc63b2bd2385464360bf1197fa5c5`.
- `python3 -B docs/specs/verify-combat-sealed-round-v2.py`: exit0;12 semantic groups,10 retained traces,68 cuts,610 replay mutations,340 raw rejects,288 clock comparisons/retries,480 lifecycle retries,30 invalid proposals. This checks full frozen Python contract, including Task012 suffixes, not C# implementation.
- Independent read-only Python audit: parsed C# `Shapes` dictionary using regular expression and compared equality with `combat-sealed-round-v2.schema.json` objects; all17 exact. Counted events before first `attack-committed` in each retained trace;10 bases/54 events/64 initial-plus-event states/four excluded events.
- `git diff --check`: exit0.
- .NET remains gated pending marker; no .NET command launched before clearance.

### Final reviewer execution after clearance

Marker `/tmp/task011-after-boundary-20260920` observed before first .NET invocation. Both commands used `login:false`, approved local IPC, existing Debug build and no `--disable-build-servers`.

1. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class Cna.Core.Tests.Campaigns.CombatSealsTests -bl:/tmp/task011-review1-seals-{}.binlog`
   - Exit0;8 passed,0 failed,0 skipped;7.813s.
   - Binlog exists: `/tmp/task011-review1-seals-20260920-104403--26573--Sr959T-dotnet-test.binlog`.
2. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class Cna.Core.Tests.Campaigns.CombatStepsTests Cna.Core.Tests.Campaigns.CombatIdentityTests -bl:/tmp/task011-review1-shared-{}.binlog`
   - Exit0;26 passed,0 failed,0 skipped;13.193s.
   - Binlog exists: `/tmp/task011-review1-shared-20260920-104420--26586--nwTrji-dotnet-test.binlog`.
3. Final source-manifest check: all five files match; manifest SHA256 unchanged. Final HEAD `94a4ecd99054d11a36350d9ec8fba55c9b5363bb`; permitted root commit preserved frozen source. Final `git diff --check` exit0. Working-tree change observed only in root-owned check packet; not read again or edited by reviewer.

Runtime results confirm all eight CombatSeals tests, including literal54/64, paired192/96, provenance, grammar-before-context, retry/cut and ownership assertions. Shared26 tests pass against existing grammar/predecessor/identity behavior. Python and both test processes completed with exit0; no reviewer-launched process remains running.

## Open Questions And Residual Risks

No blocking open question within frozen scope. Reviewer used existing build as authorized; did not independently rebuild, rerun full solution/boundary gates, format, remote CI, benchmark or test actual host publication. Root owns those gates and final acceptance. Synthetic trusted Boundary and separately supplied trusted input histories remain API preconditions; tests do not establish live authenticated transport or positive campaign history. Public revision/privacy projection, extended Snapshot12, commitment/result processing and durable publication remain excluded.

## Verdict

Ready — for frozen Task011 dormant precommit mechanism and stated plan slice. This verdict does not close parent delivery, publication or activation gates.

## Recommended Next Actions

Root reconciles this report with remaining scheduled reviews and required parent gates; preserve explicit54/64 scope and four excluded commit effects in acceptance evidence. No fix, additional review instance or new workstream requested by this reviewer.
