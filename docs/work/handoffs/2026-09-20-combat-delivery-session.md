# Handoff: Custody, round closure and isolated Release bases accepted

## Objective And Boundary

User resumed delivery for1.5–2hours, Astra medium, delegated work and three fresh sequential reviews per slice; third independently rebuilt. Session started2026-09-20 18:12:58UTC;90minute checkpoint19:42:58; hard stop20:12:58. Task015 acceptance, Task016 implementation/acceptance and Task017A1 implementation/acceptance complete. No merge authorized or performed. Original dirty `/Users/dsteele/repos/sandtable` remained read-only.

## Canonical Sources

[Implementation plan](../../design/combat-cycle-implementation-plan.md), [roadmap](../../roadmap/pre-alpha-roadmap.md), [session index](../../../.planning/combat-task008-delivery/session-20260920-index.md), [Task015 evidence](../../../.planning/combat-task008-delivery/task015-evidence.md), [Task016 evidence](../../../.planning/combat-task008-delivery/task016-evidence.md), [A1 evidence](../../../.planning/combat-task008-delivery/task017a1-evidence.md). Plan/specs own requirements; this handoff records execution state. Prior stopped handoff superseded for015/016; do not reload unrelated historical handoffs.

## Current Repository State

Worktree `/Users/dsteele/.codex/worktrees/8fcc/sandtable`, branch `codex/combat-custody-closure-session`. Initial main `ba54efd720b38627b473cfd5d2e2ba4ee638d6b2`; PR136/137 merges verified. Main-based [PR138](https://github.com/dills122/sandtable/pull/138), attached to originating task. No conflicts observed.

Retained commits: `d600966` custody acceptance; `d480556` closure implementation; `afdcd9e` closure acceptance/Release scope; `77ef166f03674a7e5875acb8c1ebfde36b0d406a` A1 implementation. Publication commit containing this handoff records final acceptance/reviews/docs; resolve with `git log -1 -- docs/work/handoffs/2026-09-20-combat-delivery-session.md`. Source pins remain exact to77ef166 and Task016d480556. No unfinished source changes. Final root publication checks verify clean status and pushed HEAD; consult PR for latest metadata-commit CI.

## Completed Work And Evidence

Task015 code00a8b68 already merged136; source/test tree matched initial main, seven pins passed. Historical full2,324/Boundary81/build/format reconciled; exact code CI verified. Interrupted old review excluded. Three fresh Ready reviews; third full rebuild plus123focused/81Boundary. Root development/report reconciliation complete, no source changes.

Task016 coded480556 implements original relationships, ordered immediate proof, round/CA closure into Release. Development review found/fixed borrowed receipt stage promotion via immutable pre-event origins and recomputed terminal identity/prefix chain. Eight source/test paths; contracts/fixtures/World7 unchanged.272event literals/304hashcuts/32finalJSON. Full2,331/Boundary81/build0warnings/errors/fullformat and exactCI passed. Three fresh Ready reviews, third full rebuild plus62focused/81Boundary. Accepted.

Task017A1 code77ef166 implements immutable isolated Release base/history values and expected-base codec. Exactly44base hashes and four explicit historical exclusions; zero lifecycle/event/state proof. Raw canonical-before-context and inherited AttackHistory hash typing corrected during development. Post-format worker20focused/shared passed. Root full2,337/0failed/skipped9m52.898s; Boundary81/0/0passed9.705s; build0warnings/errors2.72s; fullformat passed. ExactCI all eight checks successful, independently confirms2,337/0failed/skipped9m49.507s. Three fresh Ready reviews; third full rebuild0warnings/errors14.59s plus6Release/40shared/81Boundary. Root read all reports/ledgers; accepted.

Nine fresh independent reviews completed this session. Each used own detached clone, blind ledger before author, no CCE/memory/prior-review access. All root/worker/reviewer-owned commands completed; no remaining owned build/test processes. No conditional final review required because no independent finding remained.

## Decisions And Rationale

Task017 children preserve parent criteria: acceptedA1basecodec, A2native lifecycle, Bempty native adapter, separate held-I/no-move positive predecessor and bridge. Four historical Result1 rows excluded from44isolated proof; no World7 relaxation. Isolated/empty proof cannot close parent017. A1 caller supplies independently retained expected base/request; candidate-derived expected data is not authentication.

[A2 API handoff](../../../.planning/combat-task008-delivery/task017a2-api-handoff.md) records actual reusable APIs and proposed four implementation paths plus root plan. Lifecycle state must validate converted-II and pending exceptions separately from initial-base rules. A2 not dispatched; remaining lifecycle/replay work needs a fresh bounded implementation/review budget.

Optional CCE writes/lookup/recall rejected by automatic approval review as internal-code disclosure to unspecified external connector. Local inspection/evidence used; no retries. This did not block delivery. Do not retry external connector without changed authorization.

## Blockers And Limitations

Public Combat, actual positive creation-rooted history, Snapshot successor, HOST-PUB-001 publication, future duties and full cycle remain open. Inherited Movement requires moves and rejects held Reserve; positive3h prerequisite needs implementation/certification before3i. Later-II/consumed actual lineage unproved. No new storage/content spike or fixture regeneration. Parent017 and Tasks018–025 remain open according to canonical dependencies.

## Immediate Next Actions

1. Verify branch/PR HEAD, clean working tree, source pins and latest PR checks. Preserve original dirty checkout. Do not merge without authorization.
2. Read canonical Task017 refinement and A2 API handoff. Confirm acceptedA1 before making exact A2 path/behavior manifest concrete; revise manifest before any extra path. Do not treat research as dispatch authorization.
3. With explicit continuation scope, implement bounded A2 native lifecycle with meaningful RED,132event hashes/176state hashes+lengths/2terminal literals for44isolated rows, replay/retry/timing/bounds proof and unchanged A1 tests. Full gates/dev review/three fresh sequential reviews; third full rebuild. No automatic B or positive adapter.

## Verification Commands

Use native .NET10 MTP, `login:false`, unique logs/binlogs:

- `dotnet build Sandtable.slnx --no-restore`
- `dotnet test --solution Sandtable.slnx --no-build`
- `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --no-build --filter-trait 'Boundary=UserSpace'`
- `dotnet format Sandtable.slnx --verify-no-changes --no-restore`
- `git diff --check`
- `shasum -a 256 -c .planning/combat-task008-delivery/task017a1-source.sha256`
- `shasum -a 256 -c .planning/combat-task008-delivery/task016-source.sha256`

Approved IPC/restore escalation may be needed. Avoid pipe class glob, invented Boundary project and SDK `dotnet test -- --help` crash. If wildcard selects zero, use exact class `Cna.Core.Tests.Campaigns.CombatReserveReleaseBaseTests`; zero-test runs never count. `Assert.ThrowsAny<JsonException>` accepts parser-derived errors; no production normalization needed. Release status vocabulary is none/I/II, not snapshot reserve-i/reserve-ii. Close owned processes only; no global shutdown.

Local file-link check covers targets, not anchors; CI offline links remains observational. Retained worker/check reports disclose analyzer, SDK/filter/sandbox failures and actual REDs separately from final passes.

## Delivery Metadata

2026-09-20; branch and source checkpoint above. PR title: Close dormant Combat settlement and validate Release bases. Published review/evidence/handoff commit follows77ef166 without source changes. Final PR readiness/CI readback belongs live PR; no merge performed. No active implementation left for another agent to finish.
