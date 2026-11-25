# CI wall-clock measurement — 2026-11-24

Goal: cut PR CI wall-clock (push to all-green) without reducing what is verified, so required checks are practical to enforce again. This is the Phase 1 measurement. Data: `gh api` over the last 60 CI `pull_request` runs (2026-11-23 17:23 UTC to 2026-11-25 00:48 UTC), 20 CI `push` runs on `main`, plus CodeQL and docs-links run lists. Job and step times are `completed_at - started_at`; queue is `started_at - created_at`. Medians are over successful jobs. Earlier context: [2026-10-15 CI01 investigation](ci-runtime-optimization-2026-10-15.md), [2026-10-16 long-suite isolation](2026-10-16-ci-long-suite-isolation.md).

## Headline

The `orleans` job is the PR critical path, by a wide margin, and since 2026-11-24 03:53 UTC it has been green without running any tests.

| Window | PR runs (success) | Wall-clock median | Wall-clock excluding `orleans` + `verify` |
| --- | --- | --- | --- |
| Before the in-job retry (2026-11-23 17:23 to 2026-11-24 03:53) | 10 | 24.0 min (min 16.9) | 9.0 min |
| After the in-job retry (since 45322534173) | 21 | 38.1 min (min 17.1) | 10.2 min |

Everything except `orleans` finishes in about 9-10 minutes. Queue time is not a factor in this sample (median 3-4 s per job).

## Top contributors to wall-clock

| # | Contributor | Evidence | Wall-clock cost |
| --- | --- | --- | --- |
| 1 | `orleans` runs one serial 258-row class | `CampaignDurableAuthorityJournalTests` = 1244 s of 1320 s summed test time (420 rows, 20 classes); one process, single xunit collection. Healthy run: 15.3-21.5 min test step | 15-21 min, ~65-75% of PR wall-clock before the retry change |
| 2 | `orleans` per-attempt cap + retry masks the timeout | Test step is exactly 2160 s (2 x 18 m) on every `orleans` run since 45322534173 (also on main). Job logs: `no report after 2 attempts (exit 0)` | 36.0 min per run; ~+14-21 min over a healthy pass, and zero Orleans tests execute |
| 3 | Every shard rebuilds the whole solution | Build step median 84-92 s in each of the 9 PR test shards plus 85 s in `prepare`; restore 13-16 s more | ~100 s of a 223-303 s shard job (about a third) |
| 4 | `prepare` is a serial 3 min gate before any test shard | Build 85 s + discovery 44 s + restore 13 s + checkout 14 s; `tests` needs it only for the row-proof artifact | 3.0 min median before shards start |
| 5 | `long-01` fails on every main push | 11 of 11 non-cancelled main runs: `long-01` and `verify` failed (runner memory; PR #571). Main never goes green | Not PR wall-clock, but PRs cannot be compared with a green main and main reruns are common |

Honorable mentions, measured and small: checkout 13-14 s per job (about 14 jobs per run), superseded pushes (22 of 60 PR runs were cancelled; `concurrency` cancel-in-progress already works), 5 of 60 PR runs needed a rerun attempt > 1.

## The Orleans finding (read-only diagnosis of run 45516809247)

Run 45516809247 (PR #570) was last pending with only `orleans` in progress. It was not hung. Evidence:

- `orleans` started 00:16:38 UTC; step "Run and verify Orleans host rows" begins about 1 minute later. At 00:52:57 UTC the job was 36.3 min old, which is the 2 x 18 m cap plus about 1 min build. Every other recent run (PR and main) shows the same 36.0 min step.
- Job log from a comparable run (138862475233): the dotnet process starts, prints nothing for 18 m, `timeout 18m` fires, attempt 2 repeats, then `##[error]Orleans host test run produced no report after 2 attempts (exit 0)`.
- Cause of `exit 0`: the loop does `if timeout ... | tee ...; then break; fi` then `status=$?`. After a false `if` condition with no `else`, `$?` is 0. `exit "$status"` therefore exits 0 and `python3 .github/scripts/verify_orleans_results.py` never runs. The artifact upload still passes because the console logs exist.
- Why the cap is hit: healthy full passes need 15.3-21.5 min (median 20.6 over 11 pre-retry runs), above the 18 m per-attempt cap added in 45322534173. Single serial class, so nothing runs in parallel.
- Timeouts that exist: job `timeout-minutes: 45`, step `timeout-minutes: 40`, per-attempt `timeout 18m` x 2 attempts. They worked as designed; the exit-status bug turned them into a silent pass.

Consequence for the owner: for roughly the last 40 runs `orleans` has reported success while running zero rows. Real Orleans regressions in that window were not caught by CI.

## Per-job timing (PR runs after 2026-11-24 03:53 UTC, successful jobs)

| Job | n | Median | Max |
| --- | --- | --- | --- |
| orleans | 35 | 22.4 min (36.0 in the 21 post-retry runs) | 37.8 |
| fast-01..04 | 37-39 | 4.3-5.0 | 9.6 |
| exercise | 38 | 4.7 | 7.0 |
| r02-1 / r02-2 / r02-3 | 38-39 | 4.7 / 3.7 / 3.8 | 5.9 |
| prepare | 42 | 3.0 | 3.8 |
| contracts | 39 | 2.2 | 5.0 |
| format | 47 | 2.0 | 3.0 |
| architecture-tests | 50 | 1.3 | 1.6 |
| verify | 31 | 0.4 | 0.7 |
| dependency-review | 52 | 0.3 | 0.5 |

Main push, `long-*` shards (full matrix, 11 runs): long-07 9.1 min, long-08 8.8, long-06 8.2, long-02 7.7, long-04 7.4, long-03 6.7, long-05 6.3; `long-01` fails (memory, #571).

## Per-step timing (median / max, seconds)

| Step | prepare | each test shard | format | architecture-tests | orleans (old) |
| --- | --- | --- | --- | --- | --- |
| Checkout | 14 / 23 | 13 / 26 | 14 / 18 | 13 / 17 | 13 / 20 |
| Restore | 13 / 16 | 15 / 32 | 15 / 23 | 11 / 23 | 64 restore+build / 87 |
| Build | 85 / 121 | 86 / 128 | verify 85 / 142 | 38 / 60 | |
| Tests / discovery | discovery 44 / 68 | partition 155 / 502 | | 2 / 4 | 2160 / 2160 |

Test-shard `Run complete-row partition` medians (post-retry): fast-01 134 s, fast-02 170, fast-03 166, fast-04 176, exercise 156, r02-1 152, r02-2 97, r02-3 106, contracts 4. Slowest/median shard job ratio is 1.39: reasonably balanced; the spread is run-to-run variance (max 502 s on one shard) more than a bad static split.

## Other measures

- Queue: median 3-4 s per job. No capacity queueing in the sample above. During this investigation, with several workers' PRs running together, hosted queueing of 5-13 min was observed on #584's own run, so shared concurrency (20 jobs) matters if shard counts grow.
- Cache hit rate: 0%. No `actions/cache` is used anywhere; restore and build run from scratch in every job.
- Flaky reruns: 5 of 60 PR runs had `run_attempt` > 1 (up to 4), all between 2026-11-23 17:23 and 2026-11-24 01:41 UTC, before the retry change; 7 PR runs ended in `failure` (5 before it, 2 after).
- Runner-minutes per PR run: median 68.5 before the retry change, 85.0 after (+16.5, entirely the second 18 m `orleans` attempt).
- CodeQL (dynamic): 3.5-8 min per run, runs in parallel, never on the critical path. docs-links: under 1 min.
- Slowest test classes: only `orleans` has per-test timing in retained artifacts (above). Core shard XML timings are in each shard's artifact but their own history documents (linked above) note that per-test durations overlap and are not safe to use as capacity forecasts.

## Changes in flight

| PR | Change | Expected effect |
| --- | --- | --- |
| #584 | 4-way Orleans shard by test method; per-shard row-count proof; aggregate proof in `verify`; fixes the `exit 0` masking | `orleans` 36 min to about 7-11 min; PR wall-clock about 36-38 min to about 11-13 min; same 420 rows; real failures red again |
| #586 | NuGet package cache in format/architecture-tests/prepare/tests | about 5-10 s per job, 10-20 s on critical path (small) |
| #571 (other worker) | `long-01` capped at 2 lanes | fixes main being permanently red; not touched here |

Hosted before/after numbers are in the PR comments.

## Recommendations needing an owner decision (not done)

1. **Expect red PRs after #584 lands if Orleans is genuinely broken.** The last ~40 runs verified nothing there. If a shard fails, that is real signal that was hidden.
2. **Required checks:** `orleans` becomes `orleans (1..4)`. Require `verify` (it `needs` all of them) instead of individual job names.
3. **Share one build across shards (largest remaining lever, about 85-90 s per shard job, about 100 s on the critical path).** The 2026-10-16 note deferred this because the inventory artifact does not hold the full validated runtime closure and the row proof binds binary fingerprints. It needs an owner-approved design: build once in `prepare`, upload `artifacts/bin`+`obj` for Release, shards download and `--no-build`. Estimated net saving after artifact transfer: 40-60 s per run. Risk: proof-binding changes.
4. **Orleans 18 m per-attempt cap:** after sharding each attempt takes about 7-11 min so the cap is no longer binding; leaving it as-is. Do not raise it without a reason.
5. **Orleans shard count:** 4 shards finish in about 7-11 min. 6 shards would save about 2 more minutes but add 2 concurrent jobs (shared 20-job pool). Shard 1 ran longer than its modeled weight; refresh `.github/scripts/orleans-weights.json` from a real shard XML after a few runs.
6. **Moving heavy suites to main-only:** nothing qualifies. PRs already skip the 8 `long-*` shards (covered on main push only). Recommendation: do not move more; `long-*` on main is already uncovered pre-merge, which is an existing coverage tradeoff the owner may want to revisit once main is green.
7. **Path filters:** `paths-ignore: docs/**, **/*.md` already exists for CI. A further filter (for example skipping `orleans` when only `tests/Cna.Core.Tests/**` changes) would reduce coverage and is not proposed.
