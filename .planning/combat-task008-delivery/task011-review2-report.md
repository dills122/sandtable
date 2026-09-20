# Task011 independent review

Review instance: 2 of 3. Reviewer mode; source/Git read-only. CCE disabled; no agents or prior review evidence used.

## Findings

No actionable findings. Frozen implementation satisfies bounded dormant Task011 precommit scope. Residual limits below remain acceptance gates for later delivery; no public activation or durable publication readiness inferred.

## Preliminary blind ledger — persisted before author/checks

Target: branch `codex/combat-task008-reaction-lifecycle`, base `8108ccd40f84d12135f17290c25bf3d268efbd8f`, observed HEAD `94a4ecd99054d11a36350d9ec8fba55c9b5363bb`. Explicit target comprises five files in `task011-source.sha256` plus current README, `docs/roadmap/pre-alpha-roadmap.md`, and tech-design scope documentation. All five hashes match. Initial tracked dirty path: `task011-checks.md`; contents not yet read. Other planning artifacts outside review target.

Blind evidence: canonical implementation plan Task011/Checkpoint E and Task012 boundary; Round2 specification/schema; five frozen source/test files; shared C3a replay/grammar context; current scope docs. No author explanation/check results read before this ledger.

1. No actionable source defect established in blind pass. Separate predecessor and Round2 trusted input histories generate exact expected event bytes; embedded event input does not authenticate itself. Base2 reconstruction requires exact retained Created binding, selected C3a decline, and Force Assignment predecessor.
2. Fixed opening floor survives private timestamp regression; owner/slot/context checks precede cancellation; exact command/owner receipt recovery precedes clock/status gates. Prepared reaches step5; cancelled reaches closed step6; commit explicitly rejected. World/RNG remain inherited, attack/target arrays empty.
3. Raw Base/state and replay events validate closed grammar before causal context replay; primitive validation precedes duplicate recovery. Shared external syntax wrapper delegates unchanged grammar. Remaining check: execute sentry/forgery/alias tests and shared regression coverage.
4. Literal test explicitly counts 10 Base2, 54 precommit events, 64 states; four attack-committed suffixes reject. Test inputs come from separate fixture inputs, not event extraction. Synthetic boundary provenance remains explicit.
5. Plan dependency order sound for dormant mechanism. Public revision projection, actual positive campaign provenance, Snapshot12 integration, Task012 debit/commit and HOST-PUB-001 remain open; bounded witness test cannot prove public revision integration.

Pending: focused executable checks, proportionate shared checks, author-claim reconciliation, final manifest stability check and process closure. No verdict issued at preliminary stage.

## Plan Review

Canonical `docs/design/combat-cycle-implementation-plan.md:860` orders private slots/duplicate readback/pinned deadlines/Prepared continuation after010, before012 atomic debit. Implementation follows that split: `CampaignCombatSealedRound.AuthenticateBase` requires authenticated selected decline at step3; `Transition` advances Prepared only through step5 and cancellation through closed step6. Default commit branch rejects Task012. No transport, host, snapshot router or production registration changed.

Plan's first-seal other-side revision requirement is evidenced here by stable own slot identity/allocation/status and timing, plus paired equal-input outcomes. This is appropriate to dormant mechanism scope; public receipt/revision projection remains separate integration work under frozen Round2 specification. Positive campaign history, Snapshot12 extension, side-safe admission, host clock confidence, and HOST-PUB-001 publication remain explicit dependencies. README/roadmap label011 underway; tech-design describes implemented mechanism without promoting parent acceptance. No material plan drift or heavy pivot required.

## Author-Claim Reconciliation

Author and checks files read only after preliminary ledger persisted.

| Claim | Evidence | Status / consequence |
| --- | --- | --- |
| Separate trusted predecessor and Round2 histories; embedded input insufficient | `AuthenticateBase`, `Replay`, `ReadBase`, `ReadState`; provenance and rehashed forgery tests | Confirmed; exact regenerated event/state bytes required |
| Owned immutable buffers/collections, narrow external grammar bridge | Models copying constructors/init setters, returned event copy; captured-Created/alias tests; SelectionStepsCodec bridge and IdentityCodec primitive delegation | Confirmed for supported API path; caller state cannot authorize transitions |
| Config1 preserved, supplemental clock policy binds original budget | Base reconstruction and full literal Base2/hash tests | Confirmed; no Config1 rewrite |
| Private timestamps cannot raise shared clock floor | Fixed `Gate`, immutable timing, opening1999 test, both-owner/both-order clock matrix | Confirmed; 192 fresh outcomes and96 retries checked |
| Exact retries precede clock/status, invalid proposals cannot cancel | `Transition` ordering; all-cut retries, changed/foreign input negatives and primitive rejection | Confirmed; actor preserved in receipt while clock cancellation event author is System |
| Prepared wins callbacks; no costs/history/RNG | Terminal callback tests, step guards, inherited World/Random serialization and literal comparisons | Confirmed; no commitment support claimed |
| Ten bases/54 events/64 states, excluding four commits | Focused literal test plus independent fixture count | Confirmed; full58/68 not C# precommit evidence |
| Seventeen descriptors match frozen schema | Independent parsed dictionary/schema equality audit | Confirmed; UInt64 and semantic array order exercised |
| Full2292 suite, Boundary81, format, oracle and exact-candidate CI passed | Statements in allowed `task011-checks.md` only | Not independently rerun/verified; retained as author testimony, not reviewer check results |

## Verification Performed

All shell invocations used `login:false`. Authorized native-MTP runs used approved local IPC, existing build with `--no-build`, and no `--disable-build-servers`. SDK reports `10.0.400`; global.json selects .NET10 native MTP, project uses xUnit MTP runner.

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatSealsTests' --results-directory /tmp/task011-review2-seals-results '-bl:/tmp/task011-review2-seals-{}.binlog'
```

Exit0; 8 passed,0 failed,0 skipped; duration7.648s. Binlog exists: `/tmp/task011-review2-seals-20260920-104707--26908--mdjR1c-dotnet-test.binlog`.

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatStepsTests' '*CombatIdentityTests' --results-directory /tmp/task011-review2-shared-results '-bl:/tmp/task011-review2-shared-{}.binlog'
```

Exit0; 26 passed,0 failed,0 skipped; duration13.627s. Binlog exists: `/tmp/task011-review2-shared-20260920-104740--26926--+z9fxr-dotnet-test.binlog`.

Additional checks:

- `shasum -a 256 -c .planning/combat-task008-delivery/task011-source.sha256`: all five files OK before and after review.
- Python3 `-B` read-only audit: parsed codec Shapes equal schema objects exactly (17); counted each trace before first attack-committed effect (54 events/64 states/four excluded effects across10 traces); built fixture bytes equal source fixture bytes.
- `git diff --check 8108ccd40f84d12135f17290c25bf3d268efbd8f -- src/Cna.Core/Campaigns/CampaignCombatSealedRoundModels.cs src/Cna.Core/Campaigns/CampaignCombatSealedRound.cs src/Cna.Core/Campaigns/CampaignCombatSealedRoundCodec.cs src/Cna.Core/Campaigns/CampaignCombatSelectionStepsCodec.cs tests/Cna.Core.Tests/Campaigns/CombatSealsTests.cs README.md docs/roadmap/pre-alpha-roadmap.md tech-design.md`: exit0, no output.
- Final HEAD unchanged: `94a4ecd99054d11a36350d9ec8fba55c9b5363bb`. Final tracked status retains only pre-existing checks-file modification; reviewer report is locally excluded from Git status. No source/Git mutations performed.
- Both test sessions exited0. Process inspection found no active dotnet, Core test executable, MSBuild or compiler server; only reviewer Codex command text matched process filter. No reviewer processes left running.

Stable SHA-256 pins recorded before final verification and matched afterward:

```text
ea407cc67f571124bb8bec05cf24fc0f3eebc63b2bd2385464360bf1197fa5c5  task011-source.sha256
408653e1de9c9a2918481626442d74b4b6ed68023e44d01ede7a0cb0ce38723a  README.md
2bf338bfdecc4a45dcc59bda43064618d66f47a3bdd2deddedb3ad045eab7fb7  docs/roadmap/pre-alpha-roadmap.md
e42cf48594cdaa6309858c1d8130593382c20d9cf3bbea03b70f37e4b9cab343  tech-design.md
```

## Open Questions And Residual Risks

No blocking question for frozen011. Tests used authorized existing build; reviewer did not independently rebuild or verify assembly source checksums. Full solution, format, Python oracle, Boundary suite and remote CI were not independently repeated. Review establishes bounded synthetic precommit mechanism, not positive campaign provenance, complete public privacy, durable/process recovery, atomic publication or Task012 commitment. Replay cost bounded by16 events; no performance benchmark claimed.

## Verdict

Ready — for frozen dormant Task011 scope.

## Recommended Next Actions

Return instance2of3 report to initiating root for acceptance and remaining review-budget decisions. Preserve later public-projection, positive-history, Snapshot12, Task012 and HOST-PUB-001 gates. No fixes, commits, agents or further review instances started.
