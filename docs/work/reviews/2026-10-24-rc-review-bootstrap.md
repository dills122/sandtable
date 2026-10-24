# Fresh Review Bootstrap

Review instance: 1 of 3.

## Review objective

Review bounded overnight RC packet and its plan. Assess exact creation retries,
third-cycle cancellation and transaction rollback proofs, and honest gate status.
Do not certify the whole original P01 profile or playable RC.

## Repository and worktree

`/Users/dsteele/.codex/worktrees/3d3c/sandtable`; follow AGENTS.md and applicable
skills. Read-only review; do not commit, stage, fix, or delegate further reviewers.

## Base, head, branch, and dirty state

Base and HEAD: `16d76448a008ea4f16299402b8ef44e8c96a4843`.
Branch: `codex/rc-overnight-plan-20261023`. Review working-tree delta including
untracked files. No implementation commit yet. Recheck actual status before review.

## In-scope paths

- `src/Cna.OrleansHost/Api/CampaignDurableAuthorityJournal.cs`
- `tests/Cna.OrleansHost.Tests/CampaignDurableCreationRetryTests.cs` (new)
- `tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.cs`
- `tests/Cna.OrleansHost.Tests/CampaignDurableOutwardApiTests.cs`
- `docs/specs/campaign-durable-authority-journal-v1.md`
- `README.md`, `tech-design.md`, `naming-overview.md`
- `docs/roadmap/pre-alpha-roadmap.md`
- `docs/design/combat-cycle-implementation-plan.md`
- `docs/work/plans/2026-10-23-rc-overnight-plan.md` (new)
- `docs/work/reviews/2026-10-24-rc-baseline-evidence.md` (new)
- `docs/work/reviews/2026-10-24-rc-certificate-status.md` (new)
- `docs/work/reviews/2026-10-24-maproom-dependency-triage.md` (new)
- This bootstrap and separate author explanation (review administration).

## Canonical requirements and plan

Read overnight plan, current durable journal spec, publication obligations in
`docs/specs/combat-authority-envelope-v1.md`, and contact projection contract
`docs/specs/combat-contact-side-projection-v2.md`. Verify acceptance against actual
source/tests, not assertions in generated reports. Original P01 obligations remain
in `docs/specs/combat-actual-moved-repeat-position-v1.md`.

## Explicit exclusions

No original-300-root admission, public Combat activation, runtime Core feature,
schema/wire migration, dependency upgrade, old-PR merge or complete HOST-PUB closure.
Retained historical source-pin failures must remain visible. The all300 ancestry
measurement may finish during review; it changes evidence status only, not code.

## Verification available

Baseline `just check` passed: 4,195 full-suite tests plus 81 boundary tests.
Current focused creation/outward runs and third-cycle/rollback logs under
`/tmp/sandtable-rc-*`. Review exact final source and latest logs; intermediate RED
and expectation-correction runs are retained. Integrated full gate is still due.
Use proportionate read-only checks; no duplicate full suite or shared build while
another worker holds build lease. Ask coordinator for lease if needed.

## Author explanation delivery

First inspect diff, tests, canonical requirements and plan; send preliminary
findings to coordinator before reading
`docs/work/reviews/2026-10-24-rc-author-explanation.md`.

Use $independent-review in reviewer mode. This is review instance 1 of 3. Work
from this bootstrap first and record a preliminary review before reading author
explanation. Then verify the explanation, review implementation and plan, run
proportionate non-mutating checks, and return evidence-backed verdict. Do not
implement fixes, create further review instances, or split into new workstreams.
