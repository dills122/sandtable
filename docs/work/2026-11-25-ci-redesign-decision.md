# CI redesign: measurement and decision (2026-11-25)

Owner problem: CI is a blocker. Runs queue for an hour, every rebase restarts 25-60 minutes, semantic collisions reach `main`, CodeQL is red or queued, and 13 PRs were merged without waiting. This note measures why and decides what to change. Labels: **VERIFIED** (measured from `gh api` data pulled 2026-11-25 17:20 UTC, window 2026-11-22 17:20 to 2026-11-25 17:20 UTC, 913 runs), **DOC** (GitHub documentation or an earlier repo note), **ASSUMED** (extrapolated; stated as such). Earlier context: [2026-11-24 wall-clock measurement](2026-11-24-ci-wall-clock-measurement.md), [2026-10-15 runtime note](ci-runtime-optimization-2026-10-15.md).

## 1. Findings (numbers)

### 1.1 What runs per event

| Event | Workflows triggered | Jobs | Runner-min (typical) |
| --- | --- | --- | --- |
| PR push (code) | CI (19 jobs: changes, format, prepare, architecture-tests, 9 test shards, 4 Orleans shards, verify, dependency-review) + CodeQL default setup (4 jobs: csharp, actions, python, javascript-typescript) + docs-links (1) | **24** (+1 Maproom when `src/Cna.Maproom/**` changes) | CI 64-84 (VERIFIED, 8 recent runs), CodeQL 6.6-8.9 (VERIFIED, 6 runs) |
| PR push (docs only) | CodeQL (4) + docs-links (1); CI skipped by `paths-ignore` | 5 | about 9 |
| Push to `main` | CI full (26 jobs: PR set plus `long-01..08`, minus dependency-review) + CodeQL (4) + docs-links (1) + Maproom/Pages when paths match | **31** | CI 131 (VERIFIED, run 45649857794) + 8 |

The pool is 20 concurrent jobs (free-plan, user-owned repo; DOC). One code PR push needs 24 slots, so a single PR alone saturates the pool. The `tests` matrix has `max-parallel: 17`; 19 CI jobs plus 4 CodeQL plus docs-links exceed 20 even with no second PR.

### 1.2 Volume, restarts, waste (3-day window)

| Metric | Value | Label |
| --- | --- | --- |
| PRs merged | 95 | VERIFIED |
| CI runs on `pull_request` | 201 over 85 branches; per branch median 2, mean 2.4, max 8; 43 branches (51%) needed 2+ runs, 20 branches 4+ | VERIFIED |
| PR CI outcome | 84 success, 76 cancelled, 25 failure, 16 in progress | VERIFIED |
| PR CI runs still running after their PR was merged/closed (not cancelled) | **21** (no workflow cancels on close; only a later push to the same PR does) | VERIFIED |
| CI runs on `push` to `main` | 79 total: 44 failure, 33 cancelled, 2 in progress, **0 success** | VERIFIED |
| Why `main` is always red | `tests (long-01)` fails ("Required suite proof failed: Failed/cancelled/incomplete suite process set", for example run 45649857794); `verify` fails with it. Cause inside the suite process is not in the job log; out of scope here, but every main push burns 131 runner-min to report red | VERIFIED |
| CodeQL runs (dynamic default setup) | 302: 267 success, 10 failure, 1 startup_failure, 1 cancelled, 23 in flight. 4 jobs each = about 1,200 jobs; median 5.0 min per run span, 8 runner-min per run, queue up to 11 min per job | VERIFIED |
| CodeQL runs on docs-only PRs | Yes, the default setup ignores `paths-ignore` (PR #622, #623, #625, #627 all analyzed) | VERIFIED |
| docs-links | 313 runs, about 0.7 min each, so about 4 hours of runner time but 313 job slots | VERIFIED |
| Current queue | 34 runs queued and 5 in progress at 17:20 UTC | VERIFIED |

### 1.3 Queue vs run time

For 8 successful PR runs started 2026-11-25 (run ids 45650229378 to 45650607042): 19 jobs each, 64-84 runner-min, **median job queue 411-921 s (7-15 min), max 1,339-1,911 s (22-32 min)**, wall-clock 37-45 min. Job run time is the minority: critical path is `changes` 0.3 + `prepare` 3 + slowest shard 7-11 (`orleans (1)` 6-9 min, `tests (exercise)` 8.7) + `verify` 0.5, about 13-15 min. The 2026-11-24 note measured 17.1 min best case on a quiet pool (DOC). So **wall-clock is about 70% queueing**, not run time. Main push run 45649857794: 26 jobs, 131.4 runner-min, 49.2 min wall, median job queue 931 s.

### 1.4 Where the runner-minutes go (3 days)

ASSUMED extrapolation from the samples above; error bar about 20%.

| Bucket | Runs | Runner-min each | Total | Share |
| --- | --- | --- | --- | --- |
| PR CI success + failure | 109 | about 75 | about 8,200 | 43% |
| PR CI cancelled | 76 | about 25 | about 1,900 | 10% |
| Main push CI (failure, 131 each) | 44 | 131 | about 5,800 | 30% |
| Main push CI cancelled | 33 | about 15 | about 500 | 3% |
| CodeQL | 302 | 8 | about 2,400 | 13% |
| docs-links, Maproom, other | about 330 | about 1 | about 400 | 2% |
| **Total** | | | **about 19,000** | |

### 1.5 What each heavy shard protects

All fast-*, r02-* and long-* shards partition the single `Cna.Core.Tests` project (315 test files) by row; `verify` proves every discovered Release row ran exactly once, so shards cannot be dropped individually without changing the proof (`long-*` is the only exclusion the verifier allows, via `excluded_shards`).

| Shard | Project | Protects | In PR today | In main push |
| --- | --- | --- | --- | --- |
| fast-01..04 | Cna.Core.Tests | Rules engine, actions, campaigns, observations, exercise sessions (bulk of unit tests) | yes | yes |
| r02-1..3 | Cna.Core.Tests | R02 group (large grouped classes, per-group process supervision) | yes | yes |
| long-01..08 | Cna.Core.Tests | Long-running row groups, bounded by 720 s deadline and 3 GiB memory reserve | **no** | yes |
| exercise | Cna.ExerciseRunner.Tests | Scenario runner, replay/exercise determinism | yes | yes |
| contracts | Game.Contracts + Intelligence.Contracts tests | Wire/contract shape and compatibility | yes | yes |
| orleans 1-4 | Cna.OrleansHost.Tests (421 rows, Postgres+Marten via Testcontainers) | Durable campaign journal, host activation, replay | yes | yes |
| architecture-tests | Cna.ArchitectureTests | Layering/authority-boundary fitness functions | yes | yes |
| format | whole solution | `dotnet format --verify-no-changes` | yes | no (PR only) |

### 1.6 Repo and org limits and settings

| Setting | Value | Label |
| --- | --- | --- |
| Owner type | **User** account `dills122`, public repo | VERIFIED |
| Actions | enabled, all actions allowed, `sha_pinning_required: true`, no self-hosted runners | VERIFIED |
| Concurrency | 20 concurrent jobs on the free plan | DOC |
| Cache usage | 10.8 GB active in 115 caches; the repo limit is 10 GB, so caches evict continuously | VERIFIED |
| Branch rules | One ruleset "main" (active): deletion, non_fast_forward, pull_request (0 approvals, thread resolution required, squash only), `required_status_checks` with **an empty list** and `strict_required_status_checks_policy: false`. No classic branch protection. **No merge queue rule.** | VERIFIED |
| Merge methods | squash, merge, rebase allowed at repo level; ruleset restricts to squash. `allow_auto_merge: false` | VERIFIED |
| CodeQL | default setup, `state: configured`, languages actions, csharp, javascript-typescript, python, typescript, `query_suite: default`, schedule weekly, standard runners | VERIFIED |
| Merge queue availability | Public repos **owned by an organization**, and Enterprise Cloud private repos. Not available for personal-account repos | DOC (GitHub docs; not testable without changing settings) |

Consequences. GitHub itself requires no checks and no "up to date" status, so no rebase is forced by GitHub. The forced rebases came from `ao pr merge` policy, conflicts, and `cancel-in-progress` restarts. `ao pr merge` refusing on a red CodeQL check is an Orchestrator-side rule, not a ruleset rule.

## 2. Decision summary

| # | Change | Who | Runner-min saved (3-day, ASSUMED) | Wall-clock effect | Status |
| --- | --- | --- | --- | --- | --- |
| 1 | Cancel PR runs on PR close/merge (add `closed` trigger, jobs skip on `closed`) | PR (Phase 3) | about 21 runs x 45 = about 950 | Frees pool slots immediately | **PR** |
| 2 | Main push: `cancel-in-progress: true`, plus nightly full CI schedule so `long-*` is never starved by bursts | PR (Phase 3) | about 4,500 (main push 6.3k to about 1.8k) | Largest queue relief; main's pending/running backlog disappears | **PR** |
| 3 | Scope gate also skips .NET jobs for `src/Cna.Maproom/**`, `tools/**`, `deploy/**` (besides `site/**`); docs-links only for Markdown changes | PR (Phase 3) | about 400 plus 200 job slots | Narrow-area PRs go from 24 jobs to 1-6 | **PR** |
| 4 | CodeQL: advanced workflow on weekly schedule + manual, remove per-PR/per-push scans | PR + owner (disable default setup first) | about 2,400 (13%), 1,200 job slots | Removes 4 of 24 slots from every PR push; ends red/queued CodeQL on PRs | **PR + owner steps** |
| 5 | Merge queue | **Owner** (transfer repo to an organization, then ruleset) | enables moving Orleans (about 30 runner-min per PR) to the queue | Eliminates "rebase to stay mergeable" and semantic collisions | Owner decision |
| 6 | Tier Orleans to queue/main-only; consolidate matrix | After 5 | about 30 per PR push | PR critical path about 13 to about 9 min | Blocked on 5 |
| 7 | Merge-preview guard (interim, no queue) | PR on owner approval | adds about 15 per merge | Catches collisions before merge | Optional |

Expected result with 1-4 only (no owner steps beyond CodeQL): code PR push 24 jobs to 19, pool no longer saturated by one PR; average PR runner-min about 83 to about 75; total about 19,000 to about 11,000 runner-min per 3 days (-40%); estimated queue median 7-15 min to 1-3 min under the same PR rate (ASSUMED: demand drops about 40% and a single PR fits in the pool); PR wall-clock 37-45 min to 15-20 min. With 5-6 on top: about 10-13 min and about 45 runner-min per PR.

## 3. Evaluation of each option

### a. Merge queue

What it fixes: the queue builds `main + PR` speculatively (`merge_group` event) and runs the required checks on that exact combination, then fast-forwards main. No PR is ever "behind"; collisions like #590 vs #600 and #607 vs #613 fail in the queue instead of on `main`. Light PR checks plus one full run per queue entry, as requested.

**Blocker (DOC):** merge queue is not offered for repositories owned by a personal account. This repo is user-owned (VERIFIED). Options:

1. **Transfer the repo to an organization** (free organization is enough for a public repo, DOC) and enable the queue. Costs: repo URL changes (GitHub redirects git and API), the Pages URL changes from `dills122.github.io/sandtable` to `<org>.github.io/sandtable`, Actions secrets/variables (`TRUNK_API_KEY`, `TRUNK_*` vars) and the ruleset must be checked after transfer (ASSUMED they move; verify), Orchestrator project remote and `ao` config need the new slug, and the GitHub App/CodeQL default setup state should be re-checked. Roughly 1 hour of owner work.
2. **Do not transfer**; use the interim merge-preview guard (item g/h below) plus serial merging by the Orchestrator.

If the owner transfers, the exact steps are:

```
# 1. Create org in the UI (https://github.com/organizations/plan), then transfer:
gh api -X POST repos/dills122/sandtable/transfer -f new_owner=<ORG>
# 2. Add a merge_queue rule and required checks to the "main" ruleset (id 20898169; re-query the id after transfer):
gh api repos/<ORG>/sandtable/rulesets/<ID> | jq '.rules'   # inspect first
gh api -X PUT repos/<ORG>/sandtable/rulesets/<ID> --input - <<'JSON'
{ "rules": [
  {"type":"deletion"},{"type":"non_fast_forward"},
  {"type":"pull_request","parameters":{"required_approving_review_count":0,"dismiss_stale_reviews_on_push":false,"require_code_owner_review":false,"require_last_push_approval":false,"required_review_thread_resolution":true,"allowed_merge_methods":["squash"]}},
  {"type":"required_status_checks","parameters":{"strict_required_status_checks_policy":false,"do_not_enforce_on_create":false,
     "required_status_checks":[{"context":"verify"},{"context":"format"}]}},
  {"type":"merge_queue","parameters":{"merge_method":"SQUASH","grouping_strategy":"ALLGREEN","max_entries_to_build":5,"min_entries_to_merge":1,"max_entries_to_merge":5,"min_entries_to_merge_wait_minutes":1,"check_response_timeout_minutes":60}}
]}
JSON
```

UI path: Settings > Rules > Rulesets > main > "Require merge queue", merge method Squash, build concurrency 5, "ALLGREEN", status check timeout 60 min. (Pass the full `rules` array; a PUT replaces it. Re-copy the pull_request parameters from the GET output.)

CI changes needed once the queue exists (done in a follow-up PR after the owner confirms): add `merge_group:` trigger to `ci.yml`; on `pull_request` run the light tier (changes, format, architecture-tests, prepare, fast/exercise/contracts shards), on `merge_group` run everything (full tier: r02, long-*, orleans). Required check names must report on both events; keep the `verify` gate pattern from #598.

How workers and `ao pr merge` interact: instead of merging, the worker/Orchestrator enqueues (`gh pr merge <n> --squash --auto` enqueues when a queue is required; if `ao pr merge` calls the merge API directly it needs a flag for enqueueing, ASSUMED: check `ao pr merge --help`). The PR then leaves the queue merged or ejected; a failed queue entry only removes that PR and the rest rebuild. Workers never rebase to stay mergeable. A red CodeQL no longer exists on PRs (change 4), so the Orchestrator refusal rule no longer trips.

Risk if the queue is slow: entries build in parallel (max 5), so throughput is about 5 PRs per full-run time (about 25-30 min with Orleans and `long-*`) = about 10-12 merges per hour at best, versus 95 merges in 3 days now (about 1.3 per hour average, bursts to 15 in 8 minutes). Burst of 15 means the last waits about 45-60 min. Mitigations: grouping (`max_entries_to_merge: 5`) merges 5 at once on one green run; `min_entries_to_merge_wait_minutes: 1`; keep the queue tier at about 15 min by leaving `long-*` to the nightly run (item d). If the queue stalls, the owner can add bypass for admins temporarily (ruleset bypass list) as an escape hatch.

Savings: removes all rebase-restarts (PRs averaged 2.4 CI runs; 51% needed 2+; about 110 of 201 runs avoidable, about 35% of PR CI minutes, ASSUMED) and the post-merge main push run (about 30% of minutes) because the queue run already is the main-equivalent run.

### b. Concurrency and cancellation

Current: `group: ci-${{ github.workflow }}-${{ github.ref }}`, `cancel-in-progress` only for `pull_request`. For `push` to main, a running run is never cancelled and the newest pending run replaces older pending ones (33 cancelled in the window are those replaced pending runs); the running ones finished at 131 runner-min each and all 44 were red. Why runs of merged branches kept consuming runners: a PR close/merge fires no `pull_request` run with `synchronize`, so nothing joins the concurrency group and cancels the running or queued run (21 runs in the window ran past merge).

Changes: (1) add `closed` to the `pull_request` types so closing starts a run in the same group (cancelling the live one), with all jobs gated off for `closed`; (2) `cancel-in-progress: true` for pushes to main; (3) a `schedule` trigger in a different concurrency group (`ci-scheduled`) so a daily full run of main always completes even when every push run gets cancelled. Same for `docs-links.yml` and `maproom.yml`. Expected: about 5,500 runner-min per 3 days (about 29%). Loses: per-commit green state on `main` (it has been 0 of 79 anyway); gained back by the daily full run and the Orchestrator can still run `gh workflow run ci.yml --ref main`.

### c. Path-based selection

Safe now: site/ (done in #598), plus `src/Cna.Maproom/**` (own workflow, no .NET reference; VERIFIED by grep of csproj/slnx/props/cs/py), `tools/**` (Python sweep script, no references from tests or CI scripts), `deploy/**` (compose files). Not safe to skip: `scenarios/**` (data), `scripts/**` (shell used by local test flow), `.github/**`, `*.props`, `global.json`.

Not safe now: per-project selection inside .NET (host-only, Core-only). All shards run the same built solution, and `verify` proves exact row coverage across fast/r02/long; skipping a shard needs a verifier change plus a dependency graph (OrleansHost depends on Core; a Core change must run Orleans). Do this only after the merge queue exists, where "light tier on PR, full on queue" gives the same saving with no verifier surgery. A nightly full run is the backstop either way (item b, change 2).

### d. Tier the heavy suites (risk table)

| Suite | Runner-min per PR push (approx) | Protects | If moved to queue/main-only the regression is caught | Recommendation |
| --- | --- | --- | --- | --- |
| orleans 1-4 | about 30 | Durable journal, Postgres/Marten, host replay | In queue run (with queue) or on main push (without queue: after merge) | Move to queue **only with merge queue**; otherwise keep on PR |
| long-01..08 | 0 (already not on PR) | Long Core rows | On main push / nightly | Keep out of PR; in queue only if the queue stays under about 15 min, otherwise nightly |
| r02-1..3 | about 11 | R02 grouped classes | Queue | Move to queue with merge queue; PR keeps fast-* |
| contracts | 2 | Wire/contract shape | Queue | Keep on PR (cheap, 2 min) |
| exercise | about 5 | Scenario determinism | Queue | Keep on PR (catches the common mistakes); optional move |
| architecture-tests, format | about 3 | Layering, formatting | PR | Keep on PR |

PR tier (with merge queue): changes, format, prepare, architecture-tests, fast-01..04, exercise, contracts, dependency-review = 12 jobs, about 35 runner-min, critical path about 9 min. Queue tier: everything. Loss without queue: none moved; so tiering waits on option a.

### e. CodeQL

Today: default setup, per PR and per push, 4 jobs, about 8 runner-min each, 302 runs (about 2,400 runner-min, 13%), queued up to 11 min per job, red or startup failures at times, and it ignores `paths-ignore`. Proposal: an advanced workflow `.github/workflows/codeql.yml` on `schedule` (weekly) plus `workflow_dispatch`, languages `actions`, `csharp` (`build-mode: none`), `javascript-typescript`, `python`. Lost: inline PR annotations for new alerts, and alerts reaching `main` up to a week before detection (the default setup already also scans weekly). Replaced by: weekly scan, `dependency-review` stays on PRs (supply chain), SHA-pinned actions are enforced by repo setting. Detect regression: Security tab alerts after the weekly run; the owner can `workflow_dispatch` it before a release. **Owner steps (cannot be done by a worker; the default and advanced configurations conflict, so do these two in order):**

```
# 1. Disable default setup (stops per-PR/per-push runs):
gh api -X PATCH repos/dills122/sandtable/code-scanning/default-setup -f state=not-configured
# 2. Merge the PR that adds .github/workflows/codeql.yml, then run it once to seed baselines:
gh workflow run codeql.yml --ref main
# Roll back: gh api -X PATCH repos/dills122/sandtable/code-scanning/default-setup -f state=configured
```

If the workflow file is merged before step 1, its scheduled runs fail with "CodeQL analyses from advanced configurations cannot be processed when the default setup is enabled" (DOC); nothing else breaks.

### f. Job count and runners

Per-job fixed overhead (checkout 13 s, setup-dotnet, artifact download of the shared build, chmod, upload) is about 60-90 s of a 4-5 min shard job. Merging the 9 test shards into about 5 larger shards would save about 6 minutes of runner time and 4 slots per PR, but it changes the partition plan the verifier proves and the weights; saving about 8% of PR minutes for a risky change to the proof machinery. **Not recommended now.** Reducing slots by removing CodeQL (4), cancelling superseded runs, and tiering Orleans (4, with queue) is cheaper and safer. Larger or self-hosted runners: GitHub-hosted larger runners are unavailable on the free plan (DOC); a self-hosted runner removes the 20-job cap but moves CI into a security-sensitive setup (public repo: fork PR code on your machine) and is rejected. Cache: 10.8 GB of 10 GB limit means constant eviction; the NuGet cache is small relative to the `build-output` artifacts, so trimming nothing here is fine, but cache keys for PR branches should not be written (follow-up).

### g. "Turn things off" menu (ranked, least risk first)

| Rank | Switch off | Saved per 3 days (ASSUMED) | What regresses | How to detect | Done by |
| --- | --- | --- | --- | --- | --- |
| 1 | CodeQL per PR and per push (default setup) | about 2,400 min, 1,200 slots | PR-time static-analysis alerts; weekly instead | Security tab, weekly run | owner step + PR |
| 2 | Superseded and merged-branch runs (cancel on close, cancel on main) | about 5,500 | per-commit green state on main | daily full run; manual dispatch | PR |
| 3 | docs-links on code-only PRs and pushes | about 150 min, about 200 slots | link rot noticed only on Markdown changes | weekly (can add schedule) | PR |
| 4 | .NET jobs for Maproom/tools/deploy-only PRs | about 400 | none (no coupling) | n/a | PR |
| 5 | `long-*` on every main push (keep nightly) | about 1,500 (8 shards x about 8 min x 25 pushes) | long-row regression noticed within 24 h instead of per push; **only after long-01 is fixed**, since it is red every run | nightly run | needs owner OK, not done here |
| 6 | Orleans on PRs (move to queue/main) | about 30 per PR push | durable journal regressions found after merge | main push / queue | with merge queue |
| 7 | `dependency-review` | about 20 | vulnerable dependency additions | n/a | **do not** (0.3 min, protective) |

Not turning off: row-proof, memory reserve, deadlines, format, architecture-tests.

### h. Semantic-collision guards

The collisions: `PendingDecisionReconstructCoverageTests` (allow-list or fixture per `PendingDecision` subtype) and `PendingEquivalenceOracleTests` (`EverySubtypeIsDerivableOrExceptionListedAndObservedOrNamedNotDriven`) both reflect over `typeof(PendingDecision).Assembly`. #607 added the oracle, #613 added a subtype without a row, and each passed CI alone. A registry file does not stop two concurrent PRs from combining badly: PR A adds a test that enumerates subtypes, PR B adds a subtype; each is green alone because neither sees the other. Only testing the combination does that (merge queue, or the interim guard). A registry reduces the repair cost: one file (for example `tests/Cna.Core.Tests/Exercises/PendingDecisionSubtypeRegistry.cs`) listing each subtype with its fixture/exception/reason, read by both tests. That consolidates edits from three places to one and gives a single conflict point for concurrent PRs (git conflict instead of a green-but-broken merge). It is a modest refactor of two test classes (not done in this change; recommended as a follow-up once the owner chooses between queue and guard).

**Interim merge-preview guard (no queue):** a `workflow_dispatch`/label-triggered job `merge-preview` that checks out the current `main`, merges the PR head, builds, and runs architecture-tests plus the reflection-style classes (`PendingDecisionReconstructCoverageTests`, `PendingEquivalenceOracleTests`, `*Catalog*`) in about 6 minutes. The Orchestrator would run it immediately before merge and merge serially. Cost about 15 runner-min per merge, so about 1,400 per 3 days; the collision window shrinks from hours to minutes. Not built; needs the owner's choice between this and the queue.

## 4. What this change delivers and what the owner must do

Delivered as separate small PRs (Phase 3): cancel-on-close and main-push collapse with nightly schedule; scope gate for Maproom/tools/deploy plus docs-links path filter; advanced CodeQL workflow file (inert until the default setup is disabled).

Owner actions, in order of value:

1. Disable CodeQL default setup (command above) after the CodeQL PR merges, or sooner to stop the per-PR scans immediately (the advanced workflow is not required to stop them).
2. Decide: transfer the repo to an organization and turn on the merge queue (section 3a), or accept the interim merge-preview guard.
3. Decide whether to move `long-*` to nightly-only after `long-01` is fixed (it is red on every main push; cause not found in the logs).
4. After the first hosted runs of the Phase 3 PRs, confirm queue median and per-PR runner-min against the table in 1.3 and 1.4. **CI-only effects (cancel-on-close, schedule, paths) cannot be proven locally; they are proven on hosted runs.**

## 5. Appendix: method

Runs: `gh api repos/dills122/sandtable/actions/runs?created=>=2026-11-22T17:20:13Z` (913 runs, paginated). Jobs for 8 PR runs, 1 main push run, and 6 CodeQL runs fetched with `actions/runs/<id>/jobs`; runner-min = sum of `completed_at - started_at` of non-skipped jobs; queue = job `started_at - created_at`. PR-to-branch mapping from `gh pr list --state all`. Branch-run and post-merge counts are computed per `head_branch` against PR `closedAt`. Totals in 1.4 are ASSUMED extrapolations (run count x sampled per-run minutes).
