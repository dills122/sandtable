# Task015 independent review

Review instance: 2 of 3.

Target: `d00a230..00a8b68c580a7efc38070571750c9a973e0fa579`. Clone `/tmp/sandtable-task015-review2-1821`, detached at candidate; clean tracked tree after checks. Integration `src`/`tests` exactly match candidate. Seven source/test paths, three public documentation paths, six planning evidence paths in diff; later dirty integration evidence excluded from implementation review.

Fresh context contained only neutral bootstrap and review instructions. Applied independent-review skill. Read tests and canonical requirements before implementation; persisted `task015-review2-preliminary.md` before author/worker/checks. No prior reviews, CCE, memory, author execution history, source edits, commits or subagents used during blind pass.

## Findings

No actionable findings. No correctness or architecture blocker found within dormant Task015 scope.

Evidence: `CampaignCombatResolution.Transition` authenticates command/actor before fallback, recovers exact command retries, opens custody only after retained loss/retreat with positive lot, selects actual captor owner, and uses independent window timing. `CampaignCombatLossRetreat.Project` reconstructs authoritative donor/lot state, checks reconstructed custody receipt against retained settlement, transfers exactly one TOE or retains an escape entitlement, and publishes future obligations with original source provenance. `CampaignCombatResolutionCodec.WorldValue` compares entire typed projected World before emitting assets and rejects incompatible stage/window/receipt combinations. Raw `ReadState` validates syntax before authenticated complete replay and byte comparison.

Blind concern about generic path search resolved by `CampaignCombatCertification.CertifyInitialProfileFacts` and `ProveSupport`: initial inventory/locations must match certified Content7; Clear terrain, unique supported retreat and both custody branches are checked before admission. This is bounded initial-profile support, not arbitrary-map route support.

## Plan Review

Task015 acceptance criteria covered: positive capture in both roles, guarded rendezvous/donor conservation, escape without immediate TOE restoration, current post-retreat CP including 11, immutable original component/quantity, retained future upkeep/training obligations, timing/fallback/retry and replay cuts. Existing Task014/013 prefix assertions remain, with only later unsupported-family cutoffs moved to relationships.

Four production and three test files exceed nominal 3–5-file planning estimate but are cohesive: shared settlement projector extension avoids separate authority; two test files only update obsolete expected rejection boundaries. No contract/fixture churn. README/roadmap explicitly state implementation under verification; design describes dormant behavior. Plan's pending Task015 acceptance is consistent with unfinished review gate, not evidence of missing custody implementation. Integration owner should reconcile acceptance status after configured reviews complete.

Task016 relationship/round/Close Assault closure correctly remains rejected. Checkpoint G as a whole is not complete. Public activation, actual positive creation-rooted history, full Snapshot successor, HOST-PUB-001 durable publication, and execution of future obligations remain open. No naming/product change requires naming-overview revision.

## Author-Claim Reconciliation

| Claim | Evidence | Status / consequence |
| --- | --- | --- |
| 176 literal events / 208 state-hash cuts; both roles and seal orders | `AllCustodyPrefixesMatch176LiteralEventsAnd208StateHashCuts`, frozen fixture, fresh passing C# run | Confirmed. State hashes are committed expected cuts, not literal full-state JSON. |
| 200 same-owner clock comparisons, earlier acceptance cannot constrain next opening | `TwoHundredSameOwnerClockComparisonsIgnoreEarlierPrivateAcceptance`, transition, fresh Result2 oracle | Confirmed; no claim of opposing-owner public disclosure proof. |
| Guard conserves TOE and current donor resources; escape creates no immediate reunion | Projector, typed guard/entitlement validation, conservation test and World tests | Confirmed. Future work retained, unexecuted. |
| Route/cost maxima rely partly on certification/shared tests | Certification support loop, World route-bound test, fixture routes and author limits | Confirmed limitation. New literal escape routes are zero-edge; do not claim broader geometry coverage. |
| Focused worker 23 and shared 100 passed | Worker evidence retained; fresh combined custody/loss/result/World run independently passed 52 | Current targeted behavior confirmed; historical worker execution not independently reproduced in full. |
| Full gate and CI pending in frozen author packet | Supplied resumed checks record prior 2,324 full-suite pass, 81 boundary pass, build/format, exact-candidate CI success; historical logs unavailable | Superseded pending status recorded, but historical counts/remote CI remain supplied evidence, not fresh reviewer runs. Verdict does not convert them into independently observed results. |
| Atomic Core candidate/retry proof, no durable publication claim | Apply/Replay implementation and tests; documented exclusions | Confirmed; preserve host publication gate. |

## Verification Performed

All commands used `login:false`, isolated candidate clone; generated artifacts retained there. Restore and build ran with build servers disabled. Local test IPC required approved escalation.

```sh
dotnet restore tests/Cna.Core.Tests/Cna.Core.Tests.csproj --disable-build-servers -bl:/tmp/sandtable-task015-review2-1821/review2-restore.binlog
```

Exit 0. Both projects restored; macOS CSSM warning printed, no restore failure.

```sh
dotnet build tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-restore --disable-build-servers -bl:/tmp/sandtable-task015-review2-1821/review2-build.binlog
```

Exit 0; 0 warnings, 0 errors; 9.56 seconds.

```sh
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatCustodyTests' --filter-class '*CombatLossRetreatTests' --filter-class '*CombatResolutionTests' --filter-class '*CombatWorldTests' '-bl:/tmp/sandtable-task015-review2-1821/review2-focused-{}.binlog'
```

Exit 0; 52 passed, 0 failed, 0 skipped; 28.611 seconds. Includes six custody, seven loss/retreat, ten resolution and 29 World tests.

Earlier attempts retained as failed verification attempts: sandbox command with `--disable-build-servers -- --filter-class ...` exited 134 before tests due denied local named-pipe/socket IPC. Escalated attempt with `--disable-build-servers --filter-class ...` exited 5, zero tests. Correct native MTP invocation above omitted unsupported extra switch and passed; neither earlier attempt counts as evidence of passing tests.

```sh
python3 -B docs/specs/verify-combat-result-settlement-v2.py > /tmp/sandtable-task015-review2-1821/review2-result-oracle.log 2>&1
python3 -B docs/specs/verify-combat-world-settlement-v1.py > /tmp/sandtable-task015-review2-1821/review2-world-oracle.log 2>&1
git diff --check d00a230..HEAD
```

All exit 0. Result2: 10 semantic groups, 32 traces, 304 cuts, 3,728 mutations, 1,360 raw rejects, 384 timing checks, 200 prior-time comparisons. World7: six canonical goldens, 57 rejection vectors, 112 receipt cuts/20 scenarios, 8,840 arithmetic cases, eight calendar boundaries. Python evidence is contract/oracle validation, not C# production restart evidence. Diff whitespace check clean. Full solution suite/format not freshly rerun in this review.

All owned build/test/oracle sessions completed. Final escalated process inspection found no process matching clone/review identifier beyond inspection itself. No build servers launched by reviewer restore/build. Sandbox-only process inspection denied first; approved read-only retry succeeded.

## Open Questions And Residual Risks

No unresolved question blocks Task015. Literal corpus and synthetic trusted boundary do not prove actual host durability or all public history/privacy behavior. Later geometry support must preserve certification before broadening inputs. Full campaign/Snapshot integration and future obligation execution must remain gated. Historical full-suite/CI results remain retained supplied observations; fresh reviewer evidence is scoped above.

## Verdict

**Ready** — for immutable dormant Task015 custody implementation and its scoped plan acceptance. Does not approve Task016, checkpoint G completion, public activation, Snapshot successor or durable publication.

## Recommended Next Actions

Integration owner: record configured review outcomes, then synchronize acceptance status. Retain explicit later-task gates. No source remediation requested; reviewer starts no further review instance.
