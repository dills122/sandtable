# Handoff: overnight Combat integration

## Objective And Boundary

Session started2026-10-05T02:47:28Z (Oct4 22:47:28 Toronto); closeout08:47:28Z,
hard end09:47:28Z (Oct5 05:47:28 Toronto). Product work is frozen. N0/R1/R2/N1/N2/N3
are accepted and delivered. N4 fresh set1/pass1,total1of9 reviewed
`36df75b1fe55f66f71b576077c6391ada8f9dbe4` and returned Ready with non-blocking follow-ups,
accepted by author/coordinator. Publication/CI/merge remain pending and coordinator-owned;
the overnight session is not declared complete.
Private actual entry stops at supported candidate before selection. No C3a/Result2 consumption,
repeat execution, later-II/consumed lineage, public/Snapshot/transport/host activation, parent017–019
closure or all32actual reachable claim. Initial synthetic content-origin labels remain explicit.

## Canonical Sources

- [Roadmap](../../roadmap/pre-alpha-roadmap.md), current status authority.
- [Combat implementation plan](../../design/combat-cycle-implementation-plan.md),019D3/019E0/019E1.
- [Post-Movement dispatch](../../design/combat-cycle-post-movement-dispatch.md), remaining actual-history gates.
- [Settled control](../../specs/combat-settled-control-v1.md) and [positive entry](../../specs/combat-positive-entry-v1.md), canonical executable contracts.
- [Approved overnight plan](../plans/2026-10-05-combat-overnight.md), historical allocations plus execution reconciliation.
- [N4 integrated evidence](../reviews/2026-10-05-overnight-integration-evidence.md).

This handoff is restart context; contracts and roadmap remain product truth.

## Current Repository State

Coordinator:01a0c9dc-00bc-78a3-800d-3cb36859e422; N4 child:01a10b0c-e975-7d01-ab55-301d8ef854d9.
Worktree `/Users/dsteele/.codex/worktrees/overnight-core-sync/sandtable`, branch
`codex/overnight-core-sync`. Base `origin/main` is `e90eef556bde6bbde4fd6b3e17064ba613868ee6`.
Retained coordinator history: initial published checkpoint `ea86b4127a3cb381d49f4d23d147db45b1dd1816`,
checkpoint `477744308791ceb1347427ae79f627a2c3bfdae7`, published checkpoint
`a9a51f9d21c497b4e1935941a910f27571cd7cad`, then main merge
`4660093ebad34b3e8b806fe62a71f34f9c24fcf7`. Frozen reviewed N4 head is
`36df75b1fe55f66f71b576077c6391ada8f9dbe4`; subsequent administrative report/status
retention changes no executable/evidence bytes. Resolve final metadata head with
`git rev-parse HEAD`, then inspect any subsequent commits.
At preparation, coordinator remote ref was a9a51f9; the merge and N4 still need preservation push.
Generated `.serena/` is untracked and excluded. No retained runtime/spec/test edits in N4.

Primary `/Users/dsteele/repos/sandtable` remains main at `18e8f99f81aed8bb76afb97c86117584b788fa62`,
two merged product commits behind origin/main at inspection. Do not reset/clean it.
Preserve tracked user edits `.claude/settings.json`, `.github/copilot-instructions.md`,
`AGENTS.md`, `CLAUDE.md`; untracked `.serena/` and
`docs/work/handoffs/2026-09-20-combat-delivery-stop.md`. N4 never edited primary.
Coordinator recorded byte-identical user-diff preservation at earlier fast-forwards.

## Completed Work And Evidence

All five product/research PRs below were merged by the coordinator after independent acceptance
and all required CI success at the exact final head. Live CI observations are retained in
`/Users/dsteele/repos/sandtable/.planning/combat-overnight/run-state.md`; local branch and
remote-tracking equality was independently checked by N4. No network publication was performed by N4.

| Slice | PR | Final published head | Squash merge |
| --- | --- | --- | --- |
| R1 research | [152](https://github.com/dills122/sandtable/pull/152) | abdc86a9833f188a8641d806e28b2ebbb51ef94c | 868126d65f35d18a2713e82d2427516f1ce4de5b |
| R2 diagnosis | [153](https://github.com/dills122/sandtable/pull/153) | 4ad53b67bc38f0d154e6ebec412e183ba2883394 | 9c34401ac5a3f52483ea32268e0ff7e463311163 |
| N1 native settled control | [154](https://github.com/dills122/sandtable/pull/154) | f67d9a327a2cf43c1f34e43bbc6f9587f515763b | 18e8f99f81aed8bb76afb97c86117584b788fa62 |
| N2 actual-entry contract | [155](https://github.com/dills122/sandtable/pull/155) | dfdf3b7429b78aecc1346feddd50a03ed72c71f2 | 84f1fac861cef7c8ffaf8f36dd75b1b7f4dedc83 |
| N3 native actual entry | [156](https://github.com/dills122/sandtable/pull/156) | 1fbc4ced8377155f327b4ab27b36bc1a88e7abbe | e90eef556bde6bbde4fd6b3e17064ba613868ee6 |

Published branch names in the same order: `codex/positive-entry-feasibility`,
`codex/runner-aggregation-research`, `codex/native-settled-control`,
`codex/combat-positive-entry-contract`, `codex/native-positive-entry`.
All local tips match their origin refs. These facts supersede **all historical child
publication-pending, review-pending and messaging-blocker text** for R1/R2/N1/N2/N3.
They do not supersede the recorded failed checks or outstanding product obligations.

N1 reproduces32+4settled sources/80control traces,236cuts/464retries; final16focused,
2512solution/81Boundary passed. R1 accepts candidate-before-selection prerequisite only;
current C3a/Result2 consumption is NO-GO. R2 fresh present-environment control succeeded,
both arms17steps/122checks; incident cause remains unknown, no runtime fix.
N2 literal both-owner entry,3966rejections/6cuts/6retries,2512solution/81Boundary passed;
seven predecessor oracles passed, original Breakdown failed. N3 final21focused,
2533solution/81Boundary passed with corrected route-order parity. Earlier2530/2531gates
are historical and do not prove final corrected code.
N4 byte-equivalence and fresh combined checks are in the linked evidence report; full2533
suite was reused, not rerun, with explicit complete runtime/test/spec/build-input equivalence.

## Review Ledger

| Slice | Count and verdict | Disposition |
| --- | --- | --- |
| N0 | set1/pass1,total1of9; Ready with non-blocking follow-up | Stale launch-baseline P3 corrected before dispatch; [report](../reviews/2026-10-05-overnight-plan-review.md) |
| R1 | set1/pass1,total1of9; Ready | Entry-only GO accepted; [report](../reviews/2026-10-04-positive-entry-feasibility-review.md) |
| R2 | set1/pass1,total1of9; Ready | Unknown/no-fix accepted; [report](../reviews/2026-10-04-runner-aggregation-review.md) |
| N1 | set1/pass1,total1of9; Ready | No findings; [report](../reviews/2026-10-05-native-settled-control-review.md) |
| N2 | set1/pass1,total1of9; initially Not ready, same instance reconciled Ready with non-blocking follow-up | At reviewed `5ba58040bc225a96b9cd30e537002f86c9c941e9`, administrative baseline-gate disposition accepted; no material code change/new pass; [report](../reviews/2026-10-05-positive-entry-review.md) |
| N3 | set1/pass1 Not ready, fresh pass2 Ready with non-blocking follow-ups,total2of9 | P2 fixed at `423bf4b818d5380250fd9f9509a5846b117b5b10`; pass2 reviewed `5c847895bc269d06e9d4e8d86bf90f9acd473f31`; [full reports](2026-10-05-native-positive-entry.md#full-independent-report-n3-set1pass2) |
| N4 | set1/pass1,total1of9; Ready with non-blocking follow-ups | Reviewed `36df75b1fe55f66f71b576077c6391ada8f9dbe4`; author/coordinator Accept tracked baseline pin, Runner unknown and temporary-evidence limitations; [full report](../reviews/2026-10-05-overnight-integration-review.md) |

No recovery research spikes. Each milestone allows at most3passes per set,9total and2bounded
recovery spikes; no count reset. N4 reviewer must not self-dispatch another reviewer or implementation.

## Decisions And Rationale

Accepted R1 narrowed N2/N3 to actual seed1/Normal/ordinary NONE openings for both owners:
Request/Created11 plus full4+1+4+1 opening history, owner idle Movement11→12,
System empty Breakdown12→13, actual Movement-end proof and candidate certification.
C3a/Result2 request/seed/receipt/position compatibility is still a future contract-first gate.
Existing synthetic Result2 catalogue and settled adapters retain their original trust bounds.
Reusing the final2533gate is justified by identical Git content trees and no interacting runtime
changes; fresh local combined tests supplement that evidence. Documentation gets no fabricated TDD.

## Blockers And Limitations

Original `verify-combat-inherited-breakdown-completion-v1.py` remains FAILED on pre146sequence
source pin (expected c5426245 prefix, current/base d019a3bc). N2 reviewer independently reproduced
at exact18e8f99baseline. Separate8semantic/golden traces passed, including16cuts/8retries,
426mutations/92raw/298boundary probes. This does not repair or waive the original command.
Coordinator owns separate narrowly scoped, independently reviewed pin maintenance; no runtime
fix, pin rewrite, skip or assertion weakening in this session.
Runner historical AggregationFailed cause is unknown; current success is not repair evidence.
Frozen native grammar duplication/two-source admission require explicit review when extended.
Graph coverage is best-effort; N3 stale correction paths used direct source fallback. N4 verifies
content identity and documentation, not a new structural graph claim.

## Immediate Next Actions

1. Coordinator verifies the final administrative delta against reviewed36df75b; runtime/evidence
   bytes stay unchanged. N4 independent review and author/coordinator acceptance are complete.
2. Preserve/push all retained coordinator commits even if review or CI cannot complete by deadline;
   record WIP/unverified status. After Ready, publish the scoped docs PR, require all CI success at
   its exact final head and merge. Record N4 verdict/head/PR/merge in final closeout metadata.
3. Pause `sandtable-overnight-orchestration` heartbeat on final closeout; do not leave an active task
   promise after the hard stop. No new product behavior during closeout.
4. Next safe product task is the separate reviewed Breakdown pin-maintenance follow-up: compare
   the exact baseline/cache-only source change with golden semantics before any pin adjustment.
   Then research/contract the C3a/Result2 actual provenance bridge; do not dispatch its native
   consumer before the separate contract is accepted/merged. Do not promote synthetic sources.

## Verification Commands

Use the N4 evidence report for exact executed commands, results, paths and SHA256 inventory.
Native MTP SDK10/xUnit v3; always explicit `--project` or `--solution`. Coordinator owns any full-suite
lease. Retained log/binlogs are local temporary evidence, with hashes committed in the report;
missing logs after cleanup reduce reproducibility and must be reported, not silently replaced.

## Delivery Metadata

N4 branch `codex/overnight-core-sync`; suggested PR title:
`Reconcile overnight Combat acceptance and durable handoff`.
Body `/private/tmp/n4-pr-body.md`; neutral [bootstrap](../reviews/2026-10-05-overnight-integration-bootstrap.md),
separate [author packet](../reviews/2026-10-05-overnight-integration-author.md).
N4 review Ready with non-blocking follow-ups is retained and accepted; no N4 PR/push/CI/merge
claim yet. Coordinator resolves final publication and may add
administrative verdict metadata after review; any material scope/content change needs review reconciliation.
