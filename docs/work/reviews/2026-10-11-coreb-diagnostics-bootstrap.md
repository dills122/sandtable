# Fresh Review Bootstrap

Review instance: 2 of 9; round1 pass2; recovery0 of2. Review a bounded CI diagnostic change, not an asserted crash fix.

## Review Objective
Assess whether CI test-shard execution retains crash evidence without weakening process failure or complete-row coverage. Reconstruct requirements and inspect the implementation/tests before reading the separate author explanation.

## Repository And Worktree
Repository dills122/sandtable. Worktree /Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable. Applicable AGENTS.md and CONTRIBUTING.md govern the change.

## Base, Head, Branch, And Dirty State
Base df831a42293cde3a105cc778ca2a762eab043400.
Head 3a0cfde5a9fce8646eff5143210dc4ed7453cee4.
Branch codex/coreb-crash-diagnosis, pushed. Tracked tree clean; preexisting untracked .serena/ excluded.

## In-Scope Commits And Paths
Whole patch against latest-main includes crash capture and strict4.0.2 runner proof compatibility. Feature commits6385f98 and3a0cfde, with latest-main integration20d0a25.
.github/scripts/ReleaseTestRows/Program.cs; .github/scripts/test_partition.py; .github/scripts/test_partition_test.py; CONTRIBUTING.md.
Read .github/workflows/ci.yml for artifact retention and caller context. Newer origin/main df831a42293cde3a105cc778ca2a762eab043400 changes upload-artifact action pins only in this workflow; no candidate workflow edit.

## Canonical Requirements And Plan
Diagnose intermittent CoreB exit139 at unchanged merge source. Preserve original evidence and hypotheses separately. Add regression-tested child-only crash capture under existing shard artifact path. Preserve failure status and final XML coverage. Freeze and hand off diagnostic draft; underlying root cause and Linux recurrence capture remain outstanding.
Success for this patch: triage settings reach the executed child, diagnostics directory exists before launch, unique absolute dump filename, parent environment unchanged, real exit/log/status retained, coverage rejects nonzero status and absent final report.

## Explicit Exclusions
Gameplay/domain files, test filters, concurrency, timeout increases, removed assertions, speculative runtime workarounds, coordinator worktree changes, original PR172, automatic merge.
No broad local suite run during the exact-source control lease. Proportionate Python checks are allowed.

## Verification Commands Available To Reviewer
python3 .github/scripts/test_partition_test.py
git diff --check df831a42293cde3a105cc778ca2a762eab043400..3a0cfde5a9fce8646eff5143210dc4ed7453cee4
Original CI https://github.com/dills122/sandtable/actions/runs/38106271573/attempts/1
Original evidence /private/tmp/sandtable-coreb-crash/original-evidence.json; crash-triage.json.
RED/GREEN capture-red.log and capture-green.log in the same directory.

## Author Explanation Location Or Delivery Step
Read /private/tmp/sandtable-coreb-crash/review-author-2.md only after recording preliminary concerns.

Use $independent-review in reviewer mode. This is review instance 2 of 9, round1pass2, recovery0of2. Work from the Fresh Review Bootstrap first and record a preliminary review before reading the Author Explanation. Then verify the explanation against the repository, review both the implementation and its plan, run proportionate non-mutating checks, and return an evidence-backed verdict. Do not implement fixes, create further review instances, or split the work into new workstreams.

Additional compatibility evidence: runner-proof-{red,green}.log; runner402-experiment/provider-comparison.json; runner402-rejection/results.json; runner402-complete-comparison.json. Frozen-head evidence location runner402-frozen/. Prior report independent-review-1.md preserved verbatim; accepted. Earlier candidate prepare failed managed unsupported4.0.2, not a native-crash clue.

Frozen local evidence: Release solution and helper rebuild PASS0warnings/errors; complete actual frozen-head discovery PASS2770rows,71/2220/469/10; all exact identities equal retained baseline. frozen-selfreview.json records SHA256,SDK and AST invariants. These are discovery/build proofs, not actual test execution coverage.
