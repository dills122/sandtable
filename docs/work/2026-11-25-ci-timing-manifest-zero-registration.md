# CI timing manifest: zero registration for PRs that add tests (2026-11-25)

Status: design + implementation proposal. Supersedes the "register every new row" workflow that
#604 made merge-friendly but did not remove.

## Problem

`long-suite-weights` conflated two guarantees:

1. **Coverage** — every discovered Release Core row runs exactly once across the shards
   (complementary filters; no missing, duplicate or stale rows).
2. **Weights** — per-row millisecond timings, used only to balance shards (and class `parts`).

`make_plan` required `set(weights.rows) == set(discovered rows)`. So every PR that added (or removed) a test
had to register rows, and `long_suite_weights.py add --proof` needs timing artifacts that only exist after a
failed `prepare`, i.e. a fix commit plus another ~1h CI cycle. #604 removed *textual* conflicts (one new
`<topic>.jsonl` per PR) but not the registration step itself; workers hit it 10+ times in one day.

Coverage is already enforced independently of the weights: `unique(...)` + the allocation check at the end of
`make_plan`, `verify_process` (exact rows vs XML per process), and `verify_all` (`executed == expected`,
shard/process/XML accounting). The weights equality check was only an accidental coverage tripwire.

## Options considered

| Option | Zero manifest edits | Coverage strict | Can break main by semantic merge | Verdict |
|---|---|---|---|---|
| A. `merge=union` gitattributes on the manifest | no (still register) | yes | yes (dupes/invalid) | rejected: the #595/#596 failure class |
| B. Plan from previous main run's artifacts | yes | yes | no | rejected: plan must be reproducible from the checked-out SHA in every job (`prepare`, each shard, `verify` all recompute and compare plan digests); an external input makes the plan depend on artifact retention/availability and breaks local runs |
| C. Nothing committed; always estimate | yes | yes | no | rejected: loses the measured balance (see simulation) |
| D. Auto-assign unknown rows to an "unmeasured" shard | yes | yes | no | rejected: adds a ninth bucket/shard; a PR adding many tests makes it the critical path; no better than estimating into existing bins |
| **E. Estimate unknown rows at plan time + scheduled refresh of committed weights** | **yes** | **yes** | **no** | **chosen** |

## Chosen design

### Plan (`long_suite_partition.make_plan`)

- Unknown row (no committed weight): estimated as the **p90 of its class's known rows, floored at 1000 ms**
  (class with no known rows: 1000 ms). Conservative so a new row is never assumed free.
- Unknown class: `parts = 1` (unsplit; lands in a `fast-*` bucket, whose rows run via `--filter-class`).
- Weights for rows/classes that no longer exist are **ignored** (reported as `stale_weight_rows`), so
  deleting or renaming a test also needs no manifest edit.
- The plan carries an `estimation` block (`estimated_rows`, `new_classes`, `stale_weight_rows`,
  `estimated_ms`, `total_ms`, `refresh_recommended`). It is part of the plan digest, which stays derived
  purely from the checkout, so `prepare`, shards and `verify` still agree.
- `discover` prints a note, and a GitHub `::warning::` when estimated rows exceed 5% of rows or estimated
  milliseconds exceed 10% of planned milliseconds (`refresh_recommended`). Informational; never fails CI.

### Guards: unchanged vs changed

Unchanged (not relaxed): duplicate/foreign/wildcard/casefold-ambiguous rows; complementary-allocation check
(`unique(actual) == core`); schema version; positive-int validation of every *present* weight; class `parts`
bounds; per-process 720 s deadline and 750 s evidence cap; `ResourceGuard` memory reserve; `long-01` lane cap;
`verify_all` exact executed-vs-discovered accounting and XML accounting.

Changed meaning (deliberate, and the whole point):
- "`weights.rows` == discovered rows" and "`weights.classes` == discovered classes" are no longer required.
  Missing weight → estimate; extra weight → ignored. Coverage is judged from discovery and execution evidence.
- Loader: the same row/class key in two entry files is no longer an error; later file name wins (duplicates
  inside one file still fail). Lets an automated refresh coexist with PRs that still carry a hand-registered
  file, so a semantic collision cannot fail `main`.

Backward compatible: the `.github/scripts/long-suite-weights/` directory format, `meta.json`, `baseline.jsonl`
and per-PR entry files load exactly as before; PRs already migrated by #604 do not have to change anything,
and `long_suite_weights.py add/check` keep working (now optional).

### Failure modes with estimated weights

| Failure | Behaviour |
|---|---|
| New row is far heavier than its estimate | Its process runs long → existing per-process deadline (720 s) / 750 s cap / 15-min budget fail the shard **loudly**; fix = add measured weight or split class. Never silent. |
| New heavy class lands in a `fast-*` bucket | Same: deadline breach on that `fast-*` job. |
| Memory-heavy new test | `ResourceGuard` 3 GiB reserve still aborts the shard (guard is runtime, not plan-based). |
| Many new tests → unbalanced shards | Slower but correct; `refresh_recommended` warning; refresh job rebalances. |
| Plan differs between jobs | Not possible: estimate is a pure function of (discovered rows, committed weights). |
| Refresh PR collides with a PR's hand-added entry file | Later-file-wins loader; no hard failure. |

### Imbalance measurement

Approximate simulation (real 3994 committed row weights, synthetic class assignment, true-cost makespan of the
slowest of 12 jobs, 3 random draws per level): removing weights for 1% / 2% / 5% / 10% / 20% / 50% / 100% of rows
raised the slowest-job cost by ≈1% / 1% / 2% / 5% / 7% / 15% / 9% over fully measured. Absolute numbers are
not CI-accurate (synthetic class mapping), but degradation is gentle and bounded. Threshold chosen: refresh when
estimated rows > 5% **or** estimated ms > 10% of plan.

### Refresh (separate PR)

A scheduled (weekly) + manually dispatchable workflow downloads shard artifacts (`ci-<shard>-<sha>-<attempt>`,
which already retain MTP XML) from the latest successful `main` CI run, re-measures every row, preserves `parts`
for known classes, sets `parts` for new classes from measured class total, drops stale rows, writes the
consolidated `baseline.jsonl` and opens a bot PR. Nothing else ever needs to edit weights. Until it lands, drift
only costs shard balance.

## Out of scope
`src/`, test contents, orleans/r02 weights, CodeQL.
