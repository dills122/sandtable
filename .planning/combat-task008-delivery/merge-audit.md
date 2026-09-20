# Task008 merge audit

Audit: 2026-09-20 UTC (2026-09-19 America/Toronto). Read-only GitHub GraphQL queries plus fetched commit objects; no branch, worktree, PR, or commit changes. Active dirty F2 work remains untouched.

## Finding

Only PR124 reached main. PR125–134 were squash-merged into their predecessor feature branches. PR135 remains open against predecessor branch. Their MERGED badges do not establish main inclusion.

Fresh remote main: `5becd0da2793fb7e2af4f0bcdf0af1688b3676ac`.
Original PR124 head: `a8eed35bbf22ba69959530a7f0f703ca3a8f6361`.
Both complete trees: `f83152a581917a037077a258f5970ea63820abd4`.
`git diff --exit-code origin/main a8eed35` passed. There are no unrelated main additions beyond the PR124 squash.

## Per-PR inclusion

Each merged PR's entire squash-commit tree equals its original head tree. PR124 squash commit is an ancestor of main; PR125–134 squash commits are not. Every newly introduced source file from PR125–135 is absent from main, independently confirming missing behavior rather than relying only on ancestry after squash.

| PR | Slice | Original head | GitHub state | Actual merge target | Merge commit | Included main |
|---|---|---|---|---|---|---|
| [124](https://github.com/dills122/sandtable/pull/124) | A1c creation binding | a8eed35 | MERGED | main | 5becd0d | Yes, exact tree |
| [125](https://github.com/dills122/sandtable/pull/125) | A2 Snapshot12 | b67b4f9 | MERGED | codex/combat-task008-creation-binding | 2a00325 | No |
| [126](https://github.com/dills122/sandtable/pull/126) | B1 opening preamble | 22f8da6 | MERGED | codex/combat-task008-creation-snapshot | bf29e86 | No |
| [127](https://github.com/dills122/sandtable/pull/127) | C Weather | a0babdb | MERGED | codex/combat-task008-opening-preamble | f3e46d1 | No |
| [128](https://github.com/dills122/sandtable/pull/128) | B2 stage entry | 3ded1eb | MERGED | codex/combat-task008-weather | d997b45 | No |
| [129](https://github.com/dills122/sandtable/pull/129) | D1 Reserve designation | 073423f | MERGED | codex/combat-task008-stage-entry | ab033e7 | No |
| [130](https://github.com/dills122/sandtable/pull/130) | D2 Reserve completion | fa5e723 | MERGED | codex/combat-task008-reserve-designation | 18a6f02 | No |
| [131](https://github.com/dills122/sandtable/pull/131) | Task019a Reserve opening | ba58c43 | MERGED | codex/combat-task008-reserve-completion | 22a723d | No |
| [132](https://github.com/dills122/sandtable/pull/132) | E1 ordinary Movement | e641bd3 | MERGED | codex/combat-task019a-first-opening | 38c7eb5 | No |
| [133](https://github.com/dills122/sandtable/pull/133) | E2/G1 Movement lifecycle | e177a4b | MERGED | codex/combat-task008-inherited-movement | fc90542 | No |
| [134](https://github.com/dills122/sandtable/pull/134) | G2 Breakdown completion | b9cb26f | MERGED | codex/combat-task008-movement-lifecycle | 8baed06 | No |
| [135](https://github.com/dills122/sandtable/pull/135) | F1 Reaction trigger | 32e4e6d | OPEN | codex/combat-task008-breakdown-completion | — | No |

Live PR135 head: `32e4e6d3c6c5a7989a27fd15800a91e9a0446712` (MERGEABLE at audit). Its complete tree is `b2acf077c21dd6ac563bec4e7a1c80155889b3ba`.

`git cherry origin/main 32e4e6d` marks only a8eed35 patch-equivalent (`-`); all eleven later original slice commits are missing (`+`). Main-relative F1 tip delta: 170 files, 12574 insertions, 22 deletions, including retained planning/review evidence. No divergence found between original slice heads and their GitHub squash results.

## Smallest safe repair

Use a separate main-based feature branch/worktree pinned to fresh main. Apply only original accepted aggregate delta `git diff --binary a8eed35 32e4e6d`, then commit once and open one PR targeting main. This produces clean main ancestry without reintroducing original PR124 or relying on misleading stacked merge status. Eleven original slice cherry-picks in order, excluding a8eed35, are also valid but unnecessary.

Require complete resulting tree to equal `b2acf077c21dd6ac563bec4e7a1c80155889b3ba` before adding any reconciliation metadata. Run required checks on that candidate, merge only after checks, and verify final main tree equivalence. Recheck live main before repair; if changed, preserve new main content and revise equality proof. Include PR135 accepted F1 content; exclude active dirty F2 work. Close/supersede PR135 only after main inclusion is verified.

Audit did not run tests or execute repair. Root owns authorized repair and preserves active F2 work.

## Preferred reconciliation after root coordination

Preserve existing PR135: in an isolated worktree at pinned F1 `32e4e6d`, merge pinned current main `5becd0d` into a repair branch. Any conflict resolution must preserve exact F1 tree, justified by main's complete tree equality with ancestor a8eed35. Verify tree remains `b2acf077c21dd6ac563bec4e7a1c80155889b3ba` and both pinned F1/main are ancestors; push this merge commit normally to original F1 branch after rechecking its remote head. Retarget PR135 to main, update title/body to all included accepted slices, run fresh checks, then merge. This avoids force-pushing and preserves original slice ancestry if PR uses merge-commit method.

Fresh repository settings confirm `allow_merge_commit=true`, `allow_squash_merge=true`, `allow_rebase_merge=true`, `delete_branch_on_merge=false`. Earlier historical squash-only notes are stale. Root executes authorized repair; audit itself performed no PR mutation.

## Repair execution

User authorized actual main integration. Root created isolated `/private/tmp/sandtable-main-reconcile` on `codex/reconcile-combat-main` at F1, merged pinned main, and resolved seven documentation conflicts to reviewed F1 versions. Reconciliation commit `2d01a6bd4968704aec59f70ff866893ba6288625` has both pinned heads as ancestors and exact F1 tree `b2acf077c21dd6ac563bec4e7a1c80155889b3ba`. No implementation changes. Normal fast-forward push updated original F1 branch; PR135 now targets main with aggregate title/body.

Fresh isolated restore, build (zero warnings/errors), format and boundary (81 passed) succeeded. GitHub verify, dependency-review and offline links passed. Full isolated regression still running; main merge remains pending. Active root F2 worktree untouched.

## Verified completion

PR135 merged to main at `dd22088fb64dc9ffa4b513d7e1095b066f36ee81` on 2026-09-20T01:32:42Z. Fresh fetch confirms main tree `b2acf077c21dd6ac563bec4e7a1c80155889b3ba`, exactly reviewed original F1 `32e4e6d` and validated reconciliation `2d01a6b`; `git diff --exit-code 32e4e6d origin/main` passed. All accepted PR124–135 content is now present on main.

Full isolated suite passed:2,051 succeeded,0failed/skipped,3m13s797ms (`/tmp/combat-main-suite.log`). Restore/build/format/boundary81 and fresh GitHub checks also passed. GitHub rejected merge-commit mode (“Merge commits are not allowed on this repository”) even though REST settings reported all methods true. Root used supported squash merge, without bypass or history rewrite. Original slice commits therefore need not be main ancestors; exact complete-tree equality proves inclusion. Old merged PR125–134 retain their historical feature-branch targets.

Active root F2 implementation remains separate. Its third independent review returned Ready; acceptance/publication bookkeeping remains for resumed delivery. Future delivery PRs target main.
