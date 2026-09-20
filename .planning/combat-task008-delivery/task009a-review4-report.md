# Task009A independent review

Review instance: 4 of 4. Final user-authorized conditional review.

## Findings

No actionable findings in frozen Task009A candidate. No P0–P3 defect established. Verdict limited to dormant inherited admission and identity, not parent009 completion or positive combat certification.

## Blind preliminary ledger — persisted before author/checks

Target: base `f84b312`, candidate `39dccfe4bac27d88e816ec787eda8a0f71583ff0`, branch `codex/combat-task008-reaction-lifecycle`. HEAD verified; all five primary SHA256 values match frozen manifest. Initial dirty state contains only `task009a-checks.md` and `task009a-evidence.md`; neither read during blind pass. Historical reports, aggregate evidence, execution history and cross-session retrieval excluded. CCE unavailable per verified invocation configuration supplied by user; exact source reads and rg used.

Sources inspected: all three new Core files, full CombatIdentityTests, fixture-link diff, canonical Task009 refinement, inherited-selection schema/spec/oracle admission, original selection Participant contract, replay router, retained-history ownership, inherited movement and G2 completion, README/design/naming/roadmap diff.

| Area | Preliminary evidence and assessment | Remaining check |
| --- | --- | --- |
| Scope/plan | 009A limited to actual G2 empty admission and identity. 009B,010–016,020/publication remain open. Five implementation/test paths plus supporting docs; administrative packets excluded from authority. | Reconcile author claims without importing previous verdicts. |
| Admission | Replay mandatory creation/full chain, exact G2 completion and first-cycle scope, six/seven movement events; weather, adjacency and CP ceilings derived from state. Unsupported histories throw before empty assessment returned. | Execute focused tests. |
| Identity | Stable UnitKey/ComponentKey separated from representation/location; trusted-world binder checks provenance and exact independent representation. New-occupant tests retain original relation endpoint. Component depletion probe asserts identity only. | Do not interpret hypothetical probes as admitted gameplay. |
| Bytes/bounds | Boundary grammar walk precedes replay; Participant parse and byte comparison precede binder. Whole value1MiB, depth32 and arrays512 checked globally; 512/513 assessment test distinguishes syntax from history. Exact serialization compared to independent history. | Transitive descriptor maintenance remains review concern; compare canonical grammar and run tests. |
| Ownership | Participant copies component list; retained history captures and returns copies; tests mutate input/output buffers and re-admit owned evidence. | Execute buffer tests. |
| Tests | Four literal fixture hashes and lengths, assessment mutations, malformed nested shapes, unsupported/truncated histories, identity changes, bounds and ownership covered. No actionable defect established in blind source pass. | Focused execution must complete before verdict. |

Preliminary conclusion: no confirmed actionable finding; readiness pending author reconciliation and independent execution. No author explanation/check logs read yet.

## Plan Review

Canonical `docs/design/combat-cycle-implementation-plan.md:862` splits009A actual inherited admission from009B positive certification. Implementation honors this boundary: `CampaignCombatCertification.cs:9` replays mandatory creation and full history; lines12–24 require completed G2 and supported first-cycle scope; lines26–44 derive both bindings and all assessment predicates before returning empty candidates. Existing predecessor readers validate inventory, movement effects, stop/resolve/end and completion. No count or family tag replaces these checks.

`CampaignCombatIdentityCodec.cs:25` and `:95` validate external syntax/canonical spelling before trusted binding/history. Four literal hashes cover complete supported AdmissionBoundary bytes. Full inherited entry, receipts, prefix, World and RNG remain embedded, with owned retained evidence. Participant unit/component provenance remains separate from location and representation (`CampaignCombatCertification.cs:49`); identity-only probes do not authorize gameplay.

Five primary files stay within bounded implementation scope. Fixture links add existing frozen contracts only. README, tech-design, naming-overview and roadmap keep009B, selection/seals, public activation and durable publication open. Canonical plan refinement already exists at base; absence of plan change in this diff is not missing scope. No migration, rollout or new operational endpoint needed for these dormant internal values. Future successor-root capacity and actual positive assault composition remain explicit integration gates.

## Author-Claim Reconciliation

Author/check packets read only after preliminary ledger persisted. Packets contain retrospective correction notes and prior-review outcome summaries; these were not used as verdict evidence. No prior report, aggregate evidence file, execution history or alternate cross-session retrieval opened.

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Actual retained G2 is sole admission authority; all four cases empty | Certification.Admit; HistoryReplay; inherited movement/G2 readers; focused literal and negative tests | Confirmed | No unsupported-input-to-empty fallback. |
| Binder is trusted-world projection, not authentication or eligibility | Certification.BindParticipant; WorldV7 constructor coverage/location/two-element checks; identity tests | Confirmed | Hypothetical relocation/depletion cannot certify a positive offer. |
| Boundary and Participant syntax precede trusted context | Codec parsing order; null/inaccessible-context sentries; focused execution | Confirmed | Malformed bytes cannot force trusted context access first. |
| Exact bytes and array512 boundary preserved | Four frozen hashes/lengths; global bounds;512/513 sentries;51 descriptor shape audit | Confirmed | World4096 allowance not imported into this value. |
| Immutable/owned history and participant values | RetainedHistory.CopyRange/Capture/getters; immutable keys; component copies; mutation tests | Confirmed | Returned byte arrays cannot mutate accepted authority. |
| Ten focused tests pass | Independent MTP run,10/0/0 | Confirmed independently | Focused behavioral evidence reproduced. |
| Final build/full suite/boundary passed | `/tmp/task009a-freeze-build.log`, `task009a-freeze-suite.log`, `task009a-freeze-boundary.log` | Confirmed as retained log evidence | Build0 warnings/errors;2254 suite tests;81 boundary tests. Reviewer did not rerun full gate. |
| Full format exit0 and exact-head remote CI success | Author checks packet only; empty format output cannot independently prove exit status; remote CI not queried | Unverified independently | No independent format/remote-CI pass claim made. |
| Parent009/positive capability remain incomplete | Canonical plan and source/docs | Confirmed | Ready verdict applies only to009A. |

## Verification Performed

1. `git rev-parse HEAD`, `git branch --show-current`, `git status --short`, `git diff --stat f84b312 39dccfe`, scoped diff and exact reads: requested target verified. No source/test/doc drift from candidate at final check. Administrative `execution.md` became dirty during review; contents not read. Other pre-existing dirty check/evidence logs retained untouched.
2. `shasum -a 256` over five primary paths: every value matches `task009a-source.sha256`.
3. `git diff --check f84b312 39dccfe`: exit0, no whitespace errors.
4. Read `global.json`, test project and shared props; `dotnet --version` returned10.0.400, permitted by latestFeature. Native MTP/xUnit SDK-style net10.0 configuration confirmed. Existing `/tmp/task009a-round3-after-boundary-20260920` explicitly clears independent execution.
5. Independently executed with `login:false` and approved local IPC:

   ```sh
   dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatIdentityTests' '-bl:/tmp/task009a-review4-{}.binlog'
   ```

   Exit0;10 passed,0 failed,0 skipped;11.033s total,10.862s module time. Process fully closed. Binlog verified present: `/tmp/task009a-review4-20260920-072429--11862--q_pXBN-dotnet-test.binlog`. Uses prebuilt output from retained final build; reviewer did not rebuild.
6. Read-only Python audit extracted51 BoundaryShapes descriptors and compared exact shape strings against frozen schema objects plus creation-ledger oracle definitions:51 checked,0 unmatched. This checks descriptor parity, not exhaustive semantic equivalence across unsupported union arms.
7. `python3 -B docs/specs/verify-combat-inherited-selection-v1.py`: exit0;4 traces/8 events,12 cuts,8 retries,1126 mutations,46 raw,72 boundaries,6 source pins. Oracle process fully closed. All processes started by this review completed before report delivery.

## Open Questions And Residual Risks

No blocking open question. Frozen transitive grammar duplication requires synchronized maintenance if contracts change. Current supported histories exercise empty settlement/relationship arrays; exhaustive positive inventory/result/geometry and hidden-fork privacy evidence belongs to009B/later tasks. Bounds and exact replay reject unsupported payloads; this review does not certify those future capabilities.

Blind source/plan ledger was completed before author/check packets; retrospective notes encountered only afterward. CCE-disabled configuration accepted as verified by user/bootstrap; reviewer did not access cross-session tools or histories. Full gate/CI provenance limitations stated above. No source fixes, commits, agents or further review instances created.

## Verdict

**Ready** for frozen Task009A candidate `39dccfe4bac27d88e816ec787eda8a0f71583ff0` against `f84b312`, within stated dormant scope.

## Recommended Next Actions

Return this report to initiating task for delivery reconciliation. Keep parent009 and all excluded capability/publication gates open. Review budget exhausted at4of4; no further review instance will start. No heavy pivot or additional approval needed for this review conclusion.
