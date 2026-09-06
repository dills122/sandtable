# Combat Design Independent Review 2

Review instance: **2 of 3**. Date: 2026-09-06.

- Base: `c210bb2215ded8f2526935f12ad7002ddbace518`.
- Reviewed head: `29a6a3ef2e3b5885960d96a0bcb5e157ea78bd55`.
- Branch: `codex/cmb-des-001-opportunity-identity`.
- Scope: six commits, seven documentation files, +918/-13; clean review worktree.
- Reviewer: fresh-context `combat_design_independent_2`, read-only.

## Findings

**No actionable findings.** Selection, defender decline, sealed assignments, preparation,
commitment, settlement and structural closure remain distinct. Cancellation invents no choices,
spends no resources, appends no attack history and skips no outstanding obligations.
No runtime or unrelated files changed.

Independence caveat: an initial CCE query unexpectedly returned brief author-planning/review snippets;
those results were not expanded. Changed navigation also contained prior-review summaries.
Preliminary findings were recorded before reading the separate author packet or prior report body.
The first pass was substantially independent, with limited incidental exposure.

## Plan Review

- [DES-001](../design/combat-opportunity-identity-v1.md) covers stable unit/component identity,
  frozen participants, target hex versus attacked-unit history, relationship provenance and
  authority/public separation.
- [DES-002](../design/combat-sealed-decision-protocol-v1.md) covers two private slots sharing a
  base, per-audience revisions, immutable choices, deadline/unavailability cancellation,
  pre-RNG commitment, durable recovery and strict readback.
- [DES-003](../design/combat-step-transitions-v1.md) covers provisional selection, actual defender
  decline, six ordered step completions, prepared advance and cancelled versus settled completion.

The final opportunity derives from the current pre-round snapshot. The permitted suffix consists
of two seals, Force Assignment completion and empty Anti-Armor completion before commitment.
The second seal derives Prepared. No-selection, RBA cancellation and assignment cancellation consume
their live contexts, preserve prior receipts and finish structural steps without combat effects.
Unresolved committed settlement prevents Close Assault completion.

Acceptance coverage includes both seal orders, exact-deadline races, stale timers, duplicates,
cross-cycle receipts, restart boundaries, forged empty-step proofs and hidden-information equivalence.
All 36 acceptance rows map to numbered decisions. Costs/results, settlement/disclosure, cycle/Reserve,
policy approval, exact contracts and implementation planning remain assigned gates. The decline-only
fixture is accurately described as a bounded capability proposal. No heavy pivot is warranted.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Documentation-only scope | Git status, log, name/status and stat | Confirmed | Runtime tests unnecessary for this diff. |
| 22 decisions and 36 acceptance cases | Independent structural check | Confirmed | Unique ordered numbering. |
| Selection precedes final frozen opportunity | DES-003 selection and opening sections | Confirmed | No stale selection base reused. |
| Second seal derives Prepared | DES-002 state/events and DES-003 suffix | Confirmed | No optional preparation event or expiry gap. |
| Missing responses close only live context | DES-003 timing and DES-002 race policy | Confirmed | Stale timers cannot cancel a later decision. |
| Prepared/committed continuation needs no controller | Protocol recovery and step handoff | Confirmed as design requirement | Hosted implementation evidence remains necessary. |
| Current code supplies patterns, not Combat lifecycle | Sequence V4, Breakdown factory, proposal validator, protobuf, Host/Worker | Confirmed | No unsupported implementation claim. |
| 193 local targets and five anchors pass | Independent Python check | Confirmed | Four design anchors and one research anchor. |
| Prior review covers DES-001/002 at `893aeec` | Retained report and Git history | Confirmed | Does not substitute for DES-003 review. |
| Historical quality corrections and fetch/CCE chronology | Commit sequence and final wording | Partially verified | Final corrections confirmed; historical tool calls not independently audited. |

## Verification Performed

Read-only checks in the isolated review worktree:

- `git status --short --branch` and `git status --porcelain=v1`: expected branch, clean.
- `git rev-parse HEAD origin/main`: exact head and stated base.
- `git log --oneline c210bb2215ded8f2526935f12ad7002ddbace518..HEAD`: six scoped commits.
- `git diff --name-status c210bb2215ded8f2526935f12ad7002ddbace518..HEAD`: seven documentation files.
- `git diff c210bb2215ded8f2526935f12ad7002ddbace518..HEAD --stat`: +918/-13.
- `git diff --check c210bb2215ded8f2526935f12ad7002ddbace518..HEAD`: passed.
- Read-only `python3 -` standard-library validation: 193 local paths, five heading anchors,
  22 unique ordered decisions and 36 unique ordered acceptance IDs; zero errors.

Visually inspected Land source renders for pages 15, 18, 21, 22 and 24. Checked September errata text
for phasing-only mandatory combat and battalion-equivalent Barrage targeting. Evidence supports
relationship membership, voluntary adjacency, RBA ordering, allocation/exposure distinctions,
once-per-segment target-hex use, Retreat priority and post-assault Contact.

Manual traces covered successful assignments in either order, cancellation at each decision stage,
deadline equality, stale timers, preparation recovery and incomplete settlement. These are design
reasoning checks, not executable lifecycle tests. Land PDF text extraction returned no substantive
text; source checks used renders. No .NET build/tests ran. Reviewer changed no files.

## Open Questions And Residual Risks

- Combined freeze must define canonical bytes, versions, bounds, migrations and acyclic round/slot
  hash preimages.
- DES-005 must establish hidden-legality equivalence and permitted phase/decline/result disclosure.
- DES-004/005 and cycle design must close every reachable resource, retreat, custody, relationship
  and continuation obligation.
- Clock confidence, atomic persistence, outbox/timer recovery and traffic analysis need implementation
  evidence. Pending owner policies remain unapproved by this review.

These are downstream gates, not missing deliverables within this documentation scope.

## Verdict

**Ready** for the bounded DES-001/DES-002/DES-003 documentation PR. This verdict does not approve
production contracts, pending policies or runtime activation.

## Recommended Next Actions

Proceed with DES-004 while retaining the gates. Require combined contract and implementation-plan
review before production work. No fixes or additional review instance required for the frozen diff.
Author accepted the report; review closeout adds this artifact and navigation links only.
