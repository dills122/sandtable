# Fresh Review Bootstrap
Review instance: 6 of 7. Read-only; do not create further reviewers or edit/stage/commit files.
## Review Objective
Review the TASK003C1 contract slice and its plan/dependency changes against accepted Combat requirements. Return findings and a supported verdict for this slice; parent003C is not claimed complete.
## Repository And Worktree
/tmp/sandtable-combat-authority-envelopes. Read applicable AGENTS.md. CCE indexes primary checkout, which is updated to8bbea59; new branch-only files require explicit local reads.
## Base, Head, Branch, And Dirty State
Base and HEAD8bbea59bbcc40d35beb17d7068d269e6834c1288; branchcodex/cmb-task-003c-authority-envelopes. One modified plan and four untracked new contract files, nothing staged. Exact SHA-256 paths in combat-inputs-review-6-manifest.json; verify before and after.
## In-Scope Commits And Paths
No commits after base; exactly the five manifest working-tree paths.
## Canonical Requirements And Plan
Review combined implementation plan, accepted policy register, source-freeze manifest/oracle, creation-ledger and World-settlement contracts, sealed protocol, cost-resolution, step transitions and continual-cycle Reserve composition. Use relevant surrounding RulesetManifest/Cna1979BreakdownRuleset/sequence4/RNG contracts as needed.
## Explicit Exclusions
No runtime C#, full Rules10 manifest, Created11/Snapshot12, command/event lifecycle, public projection or actual replay claim. No original source visual re-audit. Existing primary checkout user edits excluded. Status-only navigation synchronization will follow the review; current target is the contract slice and its plan.
## Verification Commands Available To Reviewer
python3 docs/specs/verify-combat-rules-inputs-v1.py
python3 docs/research/verify-combat-source-freeze.py
python3 docs/specs/verify-combat-content-v7.py
python3 docs/specs/verify-combat-creation-ledger-v1.py
python3 docs/specs/verify-combat-world-settlement-v1.py
git diff --check
## Author Explanation Location Or Delivery Step
combat-inputs-review-6-author.md. First send preliminary concerns/findings to parent before reading that separate file. Then reconcile the explanation with actual evidence.

Use $independent-review in reviewer mode. This is review instance6of7. Work from the Fresh Review Bootstrap first and record a preliminary review before reading the Author Explanation. Then verify the explanation against the repository, review both implementation and plan, run proportionate non-mutating checks, and return an evidence-backed verdict. Do not implement fixes, create further review instances, or split work into new workstreams. Report heavy pivots for owner decision. Include exact commands and residual uncertainty.

Retention note: original temporary packet references were adapted to adjacent committed files after review. Frozen target hashes remain unchanged in the manifest.
