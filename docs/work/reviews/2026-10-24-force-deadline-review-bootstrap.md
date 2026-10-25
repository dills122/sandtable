# Force deadline review bootstrap

Review instance: 2 of 3 across RC execution flow.

## Objective and target

Independently assess proposed Force submission validation ordering fix and its
regression coverage against existing contracts. Review patch artifact
`artifacts/rc-overnight/force-deadline-validation.patch` against current working
tree at `/Users/dsteele/.codex/worktrees/3d3c/sandtable`, branch
`codex/rc-overnight-plan-20261023`, base `16d76448a008ea4f16299402b8ef44e8c96a4843`.
Patch is not yet applied to main working tree. Earlier RC packet is dirty and
under full validation; inspect that surrounding code as context, not new scope.

## In scope

- `src/Cna.OrleansHost/Api/CampaignDurableAuthorityJournal.Submissions.cs`
- Added theory in `tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.cs`
- Contracts: `docs/specs/combat-sealed-round-v1.md`,
  `docs/specs/combat-sealed-round-v2.md`,
  `docs/specs/campaign-durable-authority-journal-v1.md`.

Evaluate rejected proposal side effects, accepted receipt retries, legitimate
deadline cancellation, closed-round behavior and transactional consistency.
Do not infer complete v2 adoption or public certification from this bounded fix.
RBA timeout, scheduled ticking and original P01 certification are excluded.

## Verification and workflow

Main integrated gate owns existing build outputs. Do not run duplicate suites.
Author worker tests scratch copy separately; results pending. Read patch and
relevant implementation/contracts first, record preliminary findings, then request
author explanation at `artifacts/rc-overnight/force-deadline-validation-evidence.md`.
Do not edit production/test sources, stage, commit, or create more reviewers.
Use independent-review skill; return source verdict and explicit pending checks.
