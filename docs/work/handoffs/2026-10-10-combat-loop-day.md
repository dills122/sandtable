# Handoff: October10 Combat-loop delivery closeout

## Objective And Boundary

Continue authentic creation-rooted Combat under Tasks017–025 of the canonical plan. Closeout
03:00 UTC October11; stop product04:00 UTC. Preservation may finish after cutoff. Paid/native
assault/public/whole-loop completion, all 72 ACs and two clean retained runs remain open.

## Canonical Sources

[Combat plan](../../design/combat-cycle-implementation-plan.md),
[actual assault contract](../../specs/combat-actual-assault-v1.md),
[accepted gap research](../../research/actual-combat-commitment-result-gap.md),
[README](../../../README.md), [technical design](../../../tech-design.md),
[names](../../../naming-overview.md). Primary checkout local operational chronology:
`.planning/combat-loop-day/session-plan.md` and `run-state.md`; these are not product truth.

## Current Repository State

Worktree `/Users/dsteele/.codex/worktrees/combat-session-two-sync/sandtable`, branch
`codex/combat-loop-day-plan`, source checkpoint 40886bcefe46d3834e0fd6b0691bc295df8a5827
merges origin/main 80f633f11271e8a0d4470459fee41bac7a014e61. Planning commit
`ecc9367717d8fe18c2ea45348909c6c8d6a509b6` retained. Closeout documentation uncommitted,
pending fresh independent review and Brain publication; no closeout Ready/PR/merge claim.
Dirty paths comprise README, tech-design, Combat plan, this handoff, three preserved review/CI
files and separate closeout bootstrap/author records. No runtime/test/schema/pin changes.
Naming-overview needs no edit: no domain name or architecture boundary changed.

Author chat 01a118ca-0c49-75c0-a052-2345d6f82a7d idle, branch
`codex/actual-assault-contract`, reviewed head d4ce0c1695579f21cb81a2bc7ef7a5a83c73ab72,
source clean except preexisting `.serena/`; retain `codex/native-actual-round-entry`.
Research chat 01a1269c-85d4-77b0-acec-bd5ee2f8bff2 idle, branch
`codex/actual-round-entry-performance-research`, remote e12588149a40a7b86cbf5f7f0095101eece3de3a,
local 219cd68bc075dedf0d6f302e5dbe82afbe703e4f. Retain `codex/actual-combat-result-gap`.
Do not reset retained branches. Primary user settings/AGENTS/CLAUDE/copilot/`.serena/`/old
handoff dirt untouched; this checkout alone owns the prose closeout.

## Completed Work And Evidence

| Milestone | Merged commit | Review / recovery | Evidence |
| --- | --- | --- | --- |
| DAY-B research PR168 | 244830c61240e0257d40b08958a55d1a2e7d6dad | 1/9 Ready;0/2 | Hosted2593/2593; research only |
| DAY-A native entry PR169 at 23:42 UTC | 94cf23883978f7fd9726f4bdd4f63677c6af6a9f | 2/9 Ready, round1 pass2;0/2 | Reviewed 0fac88f625dad8d25a6e5d62e619db683be1871a;CI 38094052388, 2770/2770 |
| C1 bounded scope research | Research only | 1/9 Ready;0/2 | Latest administration publication blocked |
| DAY-C1 no-cost admission PR172 at 02:47:12 UTC | 80f633f11271e8a0d4470459fee41bac7a014e61 | 3/9 Ready, round1 pass3;0/2 | Reviewed d4ce0c1695579f21cb81a2bc7ef7a5a83c73ab72;CI 38105360059 all13SUCCESS, 2770/2770 |
| Coordinator documentation | Local draft | 0/9 before Brain dispatch;0/2 | Self-review only;fresh review/publication pending |

PR172 needed two accepted bounded diagnostic corrections after reviews1/2 Not ready. All
inputs already rejected; inherited version/integer/union diagnostic classifications were
incomplete. Review3 Ready certifies corrected private no-cost scope. Final normal 379.819s,
focused 64.325s, mechanics 3.863s, trace 90.440s passed. Seven unchanged owning gates carried;
not correction-head executions. Final trace 59 runtime+1 test+3 owned; unchanged 57 predecessor
pins, 60 physical dependencies, four literals. Pure 16616 records are arithmetic evidence only.

Preserved verbatim:

- [Final assault review3](../reviews/2026-10-10-actual-assault-final-review-3.md), SHA256
  `0de66289f203051b12184202a79e927db673081716c4752c05fd80c88adb3479`.
- [CI 38105360059 receipt](../reviews/2026-10-10-actual-assault-ci-38105360059.json), SHA256
  `52ed064381b68d56293df0bfb811750e8162dd0191df14868afc5a03ba06e903`.
- [Native final review2](../reviews/2026-10-10-native-actual-round-entry-final-review.md), SHA256
  `66eedf0b0c0d979203347d96b63bc6d28b13a4b6fd10d4ed12177a7b305f67d9`.

Receipt tested merge 47c510031233250311a100056875928f8b5e2b1a, parents 94cf238+d4ce0c1,
SDK 10.0.401 Release attempt1. Partitions 71/2220/469/10 sum2770; zero failed/skipped/NotRun/
errors and process failures. Receipt `fresh_review_pending: true` is historical creation state;
later review3 Ready and Brain exact-head CI verification supplied merge disposition. Neither
receipt nor final review is rewritten to erase its original pending boundary.

### Subsequent main CI failure: current validation boundary

[Main run38106271573](https://github.com/dills122/sandtable/actions/runs/38106271573)
Main CI38106271573 FAILED at merged80f633f11271e8a0d4470459fee41bac7a014e61.
Core B exited139 after2103 passed / zero failed assertions in5m03s, leaving117 of2220
expected rows incomplete; harness reports one process error. No final Core B XML/crashdump
present in retained artifact. Aggregate verify failed consequentially as designed. Prepare/
build/format, Core A, Exercise, Contracts, CodeQL and links succeeded. Prior PR38105360059
all2770 passing remains historical evidence; no blanket main-green claim. Reviewed head
and merge trees are identical (`git diff d4ce0c1..80f633f` empty verified). Cause is unconfirmed;
no infrastructure/runtime/implementation root cause or concurrent-test attribution established.
Bounded main CI diagnosis is next action; no speculative fix/retry or failure waiver.

Last in-progress diagnostic record names
`CombatInheritedReserveCycleTests.ForgedControlCannotReplaceHistoryAndReturnedBytesAreDetached`
(choice `act-first`). This is a clue only; concurrent-test attribution unproven. No fix,
retry or waiver follows. Retained temporary evidence (retrieve logs/artifacts using stable
run ID if absent):

- `/private/tmp/sandtable-main-38106271573-failed.log`, SHA256
  `3087350a05fef792c6cfbff117e66aa92944bdb50eaa3ee3a661cd3ceaad536b`.
- `/private/tmp/sandtable-main-38106271573-core-b/core-b/status.json`, SHA256
  `3143c61c61a4441e73de80312ce9a2baffc9f1c7955cdaecb45e15cd502da845`.
- `/private/tmp/sandtable-main-38106271573-core-b/core-b/diagnostics/Cna.Core.Tests_net10.0_x64_261011025123317.diag`, SHA256
  `06758dd7aa74daff26cd747a4e6adf97ebdffaf088908e7efef9245bc56c0ca6`.

## Decisions And Rationale

Private native prepared entry precedes no-cost assault admission. PR172 authenticates full
prepared ARE25 with independently trusted selection/round ledgers and mandatory empty assault
ledger; all capability flags stay false. No debit/new assault event/RNG/paid native assault/
settlement/public action. Later paid reader is a planned consumer requiring new certification
of result/settlement identities and actual World lineage. README/tech-design now reflect this;
canonical draft chronology is explicitly superseded by final dispositions.

## Blockers And Limitations

Four owning oracle failures unwaived: Breakdown/cycle/inherited Snapshot reject LandSequence
pin; outward composition rejects admission with separately established stale Content relation.
No pin refresh/waiver. Earlier concurrent ExerciseRunner
`CertifiedMovementCostPairRetainsRepeatableRouteDivergence` AggregationFailed remains
unexplained: later isolated/module/solution/CI passes do not establish cause or waive it.
Earlier 15-minute hosted cancellation and 45-minute incomplete Core run remain historical
failures. CI partitioning retained coverage and timeout policy.

Latest three research administrative documents at local 219cd68 are publication-blocked by
automatic approval review pending direct user approval of payload/destination. Remote remains
e125881. Record blocker metadata only; do not copy, publish, retry or indirectly route those
three payloads through this coordinator branch. No circumvention through closeout.
Full Snapshot/Archives restart, later-II/consumed lineage, settlement/repeat, public ingress and
two clean Exercise runs remain open; no full C1/loop completion claim.

## Immediate Next Actions

1. Diagnose main CI38106271573 exit139 using retained evidence and exact merged tree;
   establish cause and a reviewable correction/verification plan before claiming main healthy
   or starting downstream product work. No speculative fix/retry or gate waiver.
2. Brain dispatches fresh documentation review with neutral bootstrap; preliminary findings
   precede separate author explanation. Resolve findings, then Brain commits/publishes and
   records reviewed/published head. Status remains pending meanwhile; no new product slice.
3. Obtain direct user approval in research chat before publishing its three blocked payloads;
   preserve local 219cd68 meanwhile.
4. Next product session: research-backed paid actual assault contract/identity+World lineage,
   then native consumer, actual settlement/repeat/finish and required lineage.
5. Side-safe public Combat, then Exercise replay/re-adjudication, two clean retained runs and
   all 72 ACs. Parent tasks remain open until evidence gates pass.

## Verification Commands

Prose checks: `git diff --check`, local link/path existence, immutable SHA256 comparisons,
commit existence and pin-reference checks. No new behavioral tests or .NET gate for prose.
Review bootstrap records exact checkpoint and separate author explanation. Future product
uses run-tests/binlog skills and MTP `--solution`/`--project`; no normal golden regeneration.

## Delivery Metadata

Date: 2026-10-10 Toronto, preservation after 03:00 UTC October11. Product
[PR168](https://github.com/dills122/sandtable/pull/168),
[PR169](https://github.com/dills122/sandtable/pull/169),
[PR172](https://github.com/dills122/sandtable/pull/172) merged within bounded scopes.
Closeout branch `codex/combat-loop-day-plan`; proposed title:
`Reconcile October10 Combat checkpoints and preserve closeout evidence`.
Summary: synchronize private entry/admission status, preserve review/CI bytes, record open gates
and research blocker. Closeout commit/push/PR pending Brain.
