# Handoff: Result2 settled Combat control executable contract

## Objective and boundary

CMB-019D2 freezes private Result2 control consuming the full019D0 packet/proof and019D1 native proof dependency. Five primary paths retained; no C# behavior or old frozen bytes change. Earlier Movement remains synthetic-pre-combat. Parents017–019, actual positive-entry/ordinary repeated Movement/later-II/consumed lineage and Snapshot/transport/public activation remain open. Coordinator:01a0c9dc-00bc-78a3-800d-3cb36859e422, local.

## Canonical sources

[Settled-control spec](../../specs/combat-settled-control-v1.md), schema/fixture/oracle and [019D2 plan](../../design/combat-cycle-implementation-plan.md#task019d2--settled-control-executable-contract). Unchanged019D0/native019D1, historical cycle-control policy, Checkpoint H/post-Movement dispatch, Movement delivery plan and roadmap govern exclusions. [Neutral bootstrap](../reviews/2026-10-04-settled-control-bootstrap.md), [author explanation](../reviews/2026-10-04-settled-control-author.md) and [independent report](../reviews/2026-10-04-settled-control-review.md) are separate artifacts. This handoff records execution, not new product truth.

## Repository state

Worktree:/Users/dsteele/.codex/worktrees/settled-control-contract/sandtable.
Branch:codex/settled-control-contract. Exact baseline:d919749b9969fd3c86e6a6ba72243d992d3cda5a,
fetched origin/codex/native-settled-continuation. PR150 verified OPEN at this head with all hosted checks SUCCESS after implementation. No merge claimed or performed. Publication must stack draft on codex/native-settled-continuation while dependency remains open; rebase onto verified main containing it if merged. Primary and other worktrees untouched. Untracked .serena tooling is excluded from commits.

## Completed work and evidence

Complete019D0 packet/proof independent replay/catalogue admission precedes assessment/control. Attack progress binds accepted receipt+canonical hash, never commitmentId/hash-only/booleans. Explicit supported witness plus progress selects owner repeat/finish; supported empty descriptor probes force one untimed System finish. Unsupported source rejects before opening. All32 default contexts have attack progress; no-progress/no-attack truth-table rows are independent policy probes, not fabricated reachable histories.

New sctl./scc. identity domains and sourceProofId bind full bytes/scope/ordinal/prefix. Public replay/apply reconstruct source and every original suffix; caller cache cannot supply authority. Lost replies return original canonical event+receipt for command/actor retries even after closure. Deadlines/high-water/clock regression/fallback use existing policy. Repeat establishes next ordinal at Movement without execution/spend, with pre-repeat prefix and result authority version. Finish stops at same-slot Truck Convoy. Complete World/resources/RNG/members/Reserve history/stage attack history/future obligations persist; targetUses/new-cycle progress reset only on repeat. Empty-Release profile introduces no exception; proof remains old-ordinal evidence, not reusable successor admission.

Semantic RED exit1 (missing assessment loses ordinary.axis.attacker commitment/witness), /private/tmp/cmb019d2-red.log. Literal resolution assertions authored before transition implementation. GREEN development corrected local mode-shadow, Timing-field assertion, duplicate-timer expectation and malformed producer/readback sequencing; signed Release replay uses its event reader. These failed iterations are not passing evidence and no contract rejection was weakened.

Final semantic GREEN then fixture freeze/readback, /private/tmp/cmb019d2-freeze.log:10groups;32 source contexts,5 independent policy rows,64 owner outcomes,4 forced descriptors,48 timing/authority checks,236 restart cuts,464 original-byte retries,3676 event/state/signed forgeries,341 source/proof attacks,1 valid re-signed Result2+Release rejection,93 canonical/capacity boundaries.80 retained traces,32 exact source pins; fixture4,492,276bytes while individual base/event/state limits remain1MiB. Shared earlier fixture retains full source bytes; new fixture is regression evidence, never admission authority.

Seven unchanged oracle commands exit0, /private/tmp/cmb019d2-upstream-oracles.log:

| `python3 -B docs/specs/verify-…py` | Evidence |
| --- | --- |
| combat-settled-continuation-v1 |9groups,32proofs,4descriptors,29pins |
| combat-result-settlement-v2 |10groups,32traces,304cuts,3728mutations,1360raw rejects,384timing/200same-owner comparisons |
| combat-sealed-round-v2 |12groups,10traces,68cuts,610mutations,340raw rejects,288clock comparisons,480retries,30invalid proposals |
| combat-reserve-release-v1 |13cases/48traces,188cuts,2368mutations,840raw rejects,20timing/27boundary |
| combat-ordinary-movement-v1 |8cases/both owners,36cuts,486mutations,120raw rejects,630cost coordinates,14atomic/overflow |
| combat-cycle-control-v1 |19cases/64traces,164cuts,1748mutations,700raw rejects,43boundary,216cost coordinates |
| combat-inherited-reserve-movement-completion-v1 |2traces/6events,16readbacks,6retries,1626mutations,60raw/58boundary rejects |

All140 prior frozen JSON/oracle files byte-identical to exact baseline. Old specs and all native sources/tests untouched. Python AST, new JSON parse and git diff --check pass. Final local18links/anchors pass, /private/tmp/cmb019d2-static.log. Reviewer independently compared all200pre-existing spec files byte-identical.

### Repository gate

PATH="/private/tmp/cmb019d2-tools:/opt/homebrew/bin:$PATH" just check exit0, /private/tmp/cmb019d2-just-check.log: restore,format,build0warnings/errors; Boundary81/81; full2496passed,0failed/skipped,7m34.889s. Existing Runner aggregation failure from019D1 did not recur; no earlier root cause is inferred. Temporary wrapper adds unique /bl paths and uses /opt/homebrew/bin/dotnet; no recipe/config modification. Four unique binlogs exist under /private/tmp/cmb019d2-gate-*.binlog.

Actual .NET10 native MTP commands executed by gate:

```text
dotnet restore Sandtable.slnx /bl:/private/tmp/cmb019d2-gate-restore-<uuid>.binlog
dotnet format Sandtable.slnx --verify-no-changes --no-restore
dotnet build Sandtable.slnx --no-restore /bl:/private/tmp/cmb019d2-gate-build-<uuid>.binlog
dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-trait Boundary=UserSpace /bl:/private/tmp/cmb019d2-gate-test-<uuid>.binlog
dotnet test --solution Sandtable.slnx --no-build /bl:/private/tmp/cmb019d2-gate-test-<uuid>.binlog
```

No .NET settled-control implementation claim is made: this gate establishes regression evidence for the Python/documentation slice only.

## Decisions, limits and tooling

Existing full019D0 source replay is reused; historical Result1 control supplies policy/vocabulary only. No fixture/hash/self-consistency substitutes source admission. Literal costs/world/obligation assertions precede byte freeze. Internal altered-authority capacity probes do not assert campaign reachability. Offline mutation-heavy oracle has substantial runtime and a4.49MiB byte fixture; no runtime performance claim. Native new-control parity remains next.

Codebase Memory unique sandtable-cmb019d2, fast generation2026-10-04T23:59:56Z. Docs excluded, direct reads supplied coverage; native settled/inherited-control source metadata_match and graph relationships inspected. Exact-worktree Serena Bridge retrieval verified. Primary index/config untouched; generated .serena excluded.

## Independent review

Fresh GPT-6.1-sol medium /root/independent_review, fork_turns none, instance1 of3. Neutral bootstrap and preliminary blind ledger preceded separate author packet. Final verdict **Ready**, no actionable implementation/plan findings. Independent complete new oracle10groups/80traces passes plus9malformed-input probes; inspected full/upstream gate logs. Author response **Accept**; no code change or new review instance. [Report](../reviews/2026-10-04-settled-control-review.md) retains exact reviewed hashes.

## Immediate next actions

Independent review and publication are complete. Coordinator reviews [draft PR151](https://github.com/dills122/sandtable/pull/151), reconciles PR150 dependency and separately accepts the native control manifest. No merge performed. Coordinator reconciles dependency and contract acceptance. Next native manifest, separately accepted before implementation:

1. src/Cna.Core/Campaigns/CampaignCombatSettledControl.cs (private engine/models).
2. src/Cna.Core/Campaigns/CampaignCombatSettledControlCodec.cs.
3. tests/Cna.Core.Tests/Campaigns/CombatSettledControlTests.cs.
4. tests/Cna.Core.Tests/Cna.Core.Tests.csproj (019D0/new fixture links).
5. docs/design/combat-cycle-implementation-plan.md.

Consume full packet through019D1, derive source-specific assessment, match all32+4source contexts and80control traces, canonical domains/prefixes/receipts, original-byte retries, forced/owner/fallback outcomes and all cuts/forgeries. Genuine positive-entry/ordinary repeated Movement and later-II/consumed/public families remain separate gates.

## Delivery metadata and authorization

Date2026-10-04 America/Toronto. Human original coordinator turn01a108e4-42ed-7720-90a7-bb8406e1a2f5 directly verified: GPT-6.1 medium child chats, TDD, independent review, Keychain PR and handoff back. Latest human requested next slice. Scoped publication/message authorized; no merge. Use `github-keychain-auth` skill (installed locally) outside sandbox, env -u GH_TOKEN -u GITHUB_TOKEN for gh; never extract credentials. Implementation commit `8b9eff448c553ee2ce4971616d040fd6631a56c2`; [draft PR151](https://github.com/dills122/sandtable/pull/151) published and attached, targeting `codex/native-settled-continuation`. This metadata-only followup records publication; obtain final task HEAD with `git rev-parse HEAD`. Exact source baseline remains `d919749b9969fd3c86e6a6ba72243d992d3cda5a`; dependency PR150 was still OPEN at immediate pre-publication verification. No merge. Only untracked .serena tooling remains outside commits.

## Dependency merge reconciliation

User requested PR151 conflict repair after merging150. PR150 verified MERGED at
`b9084656efe1e1584dda3b85e6f28272f23f99a1`; PR151 already targets main. Dependency
final tree is byte-identical to original d919749 baseline. Replayed only the two019D2
commits onto this merged main. Before metadata closeout, complete rebased tree matched
original9637137 exactly, and binary PR patch compared identical before/after rebase.
Reviewed contract/spec/schema/oracle/fixture and native code remain unchanged; prior
local gates and independent Ready review remain applicable. No redundant test rerun
is claimed. This reconciliation changes ancestry and delivery metadata only.
Current PR base is main at b908465; original baseline/review/publication records above
remain historical evidence. Obtain current rebased task head via git rev-parse HEAD.
Original published history retained locally on codex/settled-control-contract-before-main-20261004.
Force-with-lease is pinned to original remote96371371e81bbd4a32206ee06eaff8dfd40c0175.
No PR merge performed here; new hosted checks remain separate acceptance evidence.
