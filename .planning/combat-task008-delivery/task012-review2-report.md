# Task012 Independent Review

Review instance: 2 of 3. Reviewer mode. CCE unused; no agents, fixes or source/Git writes.

## Preliminary blind ledger — persisted before author/checks packets

Target: branch `codex/combat-task008-reaction-lifecycle`; base `6f9b5013ed5ab5f9fdfc3c5b7e6cac064a809405`; observed HEAD `8fcf8b9099909d970bfa942cf6dda869de8af38e`. All five `task012-source.sha256` pins pass. Explicit working-tree scope adds current README, roadmap and tech-design. Dirty execution/checks/dev-notes paths observed by status only; excluded contents unread.

Blind evidence: five pinned source/test files; selection predecessor authentication and certification call path; Task012 canonical plan row and dependency boundary; round-v2 spec/schema/oracle commitment fold; cost and opportunity designs; scoped documentation diff.

No actionable finding identified in blind pass. `Transition` reconstructs original eligibility, requires both seals and FA/AA proof, spends attacker5/defender3 plus ammo10 each, enforces ceiling10 and preserves RNG/step5. `Commit` builds immutable candidate World/history/target-use; original Base stays pre-use. `ReadState` compares strict bytes against independent trusted replay. Duplicate command returns original retained event before paid-state rejection or fresh clock checks. Typed World serialization overlays only allowed CP/ammo fields and rejects unrelated changes.

Tests cover four added commitment events/states, full58events/68cuts, both sides/seal orders, cumulative CP5/7 to10, discard/retry, rehashed forgeries, grammar sentries, paid readback and ownership. Existing54/64 coverage retained. Candidate discard is not host transaction/crash durability proof. No positive campaign adapter, extended Snapshot12, result or public activation claimed.

Follow-through checks pending: author claim reconciliation, focused/shared test execution, final pins and process cleanup. No readiness verdict issued from preliminary ledger alone.

## Findings

No actionable findings. Preliminary ledger above retained unchanged; final assessment follows author/checks reconciliation and independent execution.

## Code And Plan Review

- `CampaignCombatSealedRound.cs:26–70` authenticates Boundary through separate010B inputs/events, then replays separate Round inputs against exact event bytes. Persisted event.input supplies no independent authority. All hostile event syntax is checked before indexing trusted Round inputs.
- `CampaignCombatSealedRound.cs:91–105,161–184` checks command ownership/context before duplicate recovery, and reconstructs Prepared/step5 proof before new commitment. Slot construction fixes attacker/defender ordering independently of seal arrival order. Cost guard explicitly bounds selected CP at10 before general spending helper. Boundary certification restricts World to exact two-element initial profile except supported current CP; charging each World element is safe within this admission.
- `CampaignCombatSealedRound.cs:188–241` derives one immutable candidate containing both debits, one commitment, directional attacker/defender history and segment target use; emits one event/receipt/version/prefix. Original state is never modified. RNG is retained from authenticated Boundary; commit leaves step5 open. Committed-state guard prevents new completion/cancellation/commit; exact retries recover retained bytes without another debit.
- `CampaignCombatSealedRoundCodec.cs:35–42,126–154` separates original Base eligibility from typed current World. Serialization uses authenticated base bytes only as canonical structure and derives current CP/ammo from typed fields on every call; unrelated World changes reject. Readback requires exact equality with authenticated replay, so a syntactically valid altered paid World cannot become authority. Collection setters and result buffers retain ownership.
- Canonical Task012 row, round-v2 commitment grammar/oracle, cost design and opportunity identity requirements align with implementation. Four added commitment events/cuts extend54/64 to58/68 without replacing regression scope. CP5/7 boundary histories are rebuilt through predecessor APIs rather than patched/rehashed as accepted state.
- Five primary files respect planned size. README/roadmap correctly say implementation/verification underway; tech-design describes dormant commitment and local-only atomicity. Canonical plan acceptance remains root-owned. Task013 result, settlement, positive campaign history, public activation, extended Snapshot12 and host publication remain separate gates. No architectural expansion, rollout or migration is introduced by this internal dormant slice.

## Author-Claim Reconciliation

| Claim | Evidence | Status / consequence |
| --- | --- | --- |
| Both role costs, ammo and history/use enter one candidate before RNG | Transition/Commit; four literal commit assertions; paid-world fields | Confirmed for dormant Core |
| Paid ammo0 supports historical readback without new eligibility | Base retained; typed World serializer; all68 readback cuts; retry tests | Confirmed |
| CP ceiling and existing ledgers preserved | Explicit after<=10 guard; CP5/7 test; shared spending tests | Confirmed |
| Four new commits complete58events/68cuts, preserving54/64 | Independently executed CombatCommit+CombatSeals tests | Confirmed |
| Separate trusted provenance, strict raw grammar, ownership, forgeries | Replay/ReadState; predecessor, sentry, mutation and collection tests | Confirmed within selected synthetic profile |
| Discard and lost-response recovery are atomic | Candidate remains unpublished until caller retains event; retry returns exact original bytes | Confirmed as Core candidate/replay proof only; not host transaction, process crash or storage durability |
| Root fullsuite2301, Boundary81, formatting and exact-head CI passed | task012-checks.md testimony | Not independently rerun or remotely verified; not represented as reviewer results |
| No schema/fixture/oracle or public-version change | Scoped base diff and exact source pins | Confirmed |

## Verification Performed

All shell calls used `login:false` except initial bootstrap/skill reads. CCE never invoked. Native MTP verified from global.json, SDK-style project, props/packages: xUnit v3 MTP, net10.0; installed SDK10.0.400 permitted by latestFeature roll-forward. Existing build used under explicit user clearance; no reviewer rebuild. Tests used approved IPC escalation and unique binlogs; no `--disable-build-servers` on test commands.

1. `shasum -a 256 -c .planning/combat-task008-delivery/task012-source.sha256` — all5 OK before inspection and after tests; HEAD unchanged at8fcf8b9099909d970bfa942cf6dda869de8af38e. Git commit-ID range contains one source candidate commit; “four new commits” in contract means four attack-committed events, not four Git commits.
2. `git diff --check 6f9b5013ed5ab5f9fdfc3c5b7e6cac064a809405 -- src tests README.md tech-design.md docs/roadmap/pre-alpha-roadmap.md` — exit0.
3. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatCommitTests' '*CombatSealsTests' '-bl:/tmp/task012-review2-focused-{}.binlog'` — exit0;17passed/0failed/0skipped;8.659s. Binlog `/tmp/task012-review2-focused-20260920-112308--29843--UB4qjc-dotnet-test.binlog` exists.
4. `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-class '*CombatStepsTests' '*CombatIdentityTests' '*CombatWorldTests' '*CombatSelectedRulesTests' '-bl:/tmp/task012-review2-shared-{}.binlog'` — exit0;74passed/0failed/0skipped;13.303s. Binlog `/tmp/task012-review2-shared-20260920-112335--29862--9+2RrN-dotnet-test.binlog` exists.
5. `dotnet build-server shutdown` — exit0; MSBuild and compiler servers closed. Both test sessions exited0. `pgrep -fl 'dotnet|Cna.Core.Tests|MSBuild|VBCSCompiler'` — exit1/no matches; no remaining .NET processes.

Reviewed documentation SHA256:

- README.md: `5993debf8f77ecc2ada9964dff7966841e205e2fb0e888da76d3bc559860704a`
- docs/roadmap/pre-alpha-roadmap.md: `7e2a77898a9b1fb80bdfdcd7e45d6f6d693e87ba90c2bea3fb8e9b9288d2dd57`
- tech-design.md: `6b619f787bd547f07e4c9c0a5b8acf2d9cc8364ad79c2a4c5d8d4b04f107f502`

## Open Questions And Residual Risks

No blocking question for this frozen slice. Checks exercise cleared existing binaries; source was independently inspected/pinned, but reviewer did not rebuild or rerun full suite, full formatting, Boundary gate or Python oracle. Actual host publication failure, ambiguous storage acknowledgement and authentic positive campaign provenance are explicitly unproved here. Future integration must retain separate authenticated inputs and original eligibility, and must not substitute current paid World for initial eligibility. No prior reviews, aggregate evidence, worker/dev notes or execution-history files read; canonical plan context contains its own historical annotations, which were not used as readiness evidence.

## Verdict

Ready — bounded dormant Task012 source and current scoped docs only.

## Recommended Next Actions

Return report to root for acceptance and remaining review-budget decisions. No source fixes, commits, agents or further review instances started. Only own report intentionally written; verification produced authorized runtime/binlog artifacts.
