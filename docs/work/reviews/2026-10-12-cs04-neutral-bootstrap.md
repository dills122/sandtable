# CS04 independent review bootstrap

Review branch codex/actual-repeated-movement-contract against base
6ed3846387fd787692971c9926a76663bf9368da. Product freeze is
 e46ebfa9fffb4bbbc0e3177f57bfc85efd050f53. Final PR head will be pinned by Brain's dispatch;
subsequent author commit adds only this bootstrap and the separate rationale. Review the complete
base-to-head diff, including implementation plan and administrative evidence. Do not assume an
author verdict or a contract/lifecycle/public completion result. This is review1/9, recovery0/2,
round1/pass1; Brain owns counter advancement, scope reconciliation and merge.

Author checkout: /Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable.
Primary delivery paths:
- docs/specs/combat-actual-repeated-movement-v1.md
- docs/specs/combat-actual-repeated-movement-v1.schema.json
- docs/specs/verify-combat-actual-repeated-movement-v1.py
- docs/specs/fixtures/combat-actual-repeated-movement-v1.json
- docs/design/combat-cycle-implementation-plan.md

Additional scope is README.md, tech-design.md, Brain's roadmap patch, dated research/plan and
cs04 administrative review/test evidence. No C#/project/workflow/Content/rules changes. All228
base docs/specs paths have no modifications. Unrelated .serena/ is excluded from Git.

Read repository guidance and the independent-review skill. Assess correctness, design,
idiomatic fit, scope/plan fidelity, testing and actual admitted behavior from source independently.
Keep author rationale separate until your first assessment is formed; it is
2026-10-12-cs04-author-rationale.md beside this file. The plan is
../plans/2026-10-12-actual-repeated-movement-contract.md, including an explicit later RED evidence
correction. Test logs are observations, not substitutes for independent causal reasoning.

Evidence inputs: final-normal.json/.log, frozen-identities.json, author-self-review.md,
final-closure-{core,adversarial,dependencies}{,-receipt}.json, capacity-final.json and
red-evidence-correction.md, all with dated2026-10-12-cs04 prefixes in this directory.
Final Python normal command was python3 -B -u docs/specs/verify-combat-actual-repeated-movement-v1.py,
344.251s/900, exit0/no timeout; runtime/schema/fixture hashes unchanged. Literal payload is
4,740,034bytes with four traces,20frames,16events. .NET/CI verification is a separate Brain-owned
gate; expected inventory remains2843 argument-bearing rows. CI does not execute the Python oracle.

Do not rerun broad or predecessor suites without Brain's sole heavy lease. Focused oracle groups
--group semantic/preservation/recovery/adversarial/grammar/timestamp/capacity/dependencies/fixture
are available; owner/seal filters keep deeper groups within120s. Use an explicit process bound.
The normal verifier does not regenerate fixtures; --write-fixture is a separate author operation
and should not be used for review. Preserve repository file ownership and immutable artifacts.

Report concrete findings with severity/path/lines and reproducible evidence, plus an independent
Ready/Ready-with-nonblocking/Not-ready disposition and plan assessment. Return the review to Brain
under the user's authorized coordination scope. No merge or broader completion claim is implied.
