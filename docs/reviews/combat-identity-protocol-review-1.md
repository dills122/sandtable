# Combat Identity and Sealed Protocol — Independent Review 1

**Review instance:** 1 of 3. **Verdict:** Ready for bounded DES-001/DES-002 design inputs.
**Date:** 2026-09-06.

**Reviewed range:** `c210bb2215ded8f2526935f12ad7002ddbace518` through
`893aeec13d4b61720b1cf53509096fc3552e322d` on
`codex/cmb-des-001-opportunity-identity`: commits `d37efd8`, `0363d0c`, `893aeec`;
five documentation files, +560/-11. Review worktree was clean.

Fresh reviewer received no implementation conversation. A neutral bootstrap preceded repository
inspection; the reviewer recorded its preliminary ledger before receiving the separate author
explanation. Review remained read-only. This report and navigation closeout follow the frozen
reviewed head; they do not represent an additional implementation change or review instance.

## Findings

**No actionable findings.** Source checks support voluntary adjacency without Contact, original
relationship membership, Retreat priority, post-assault Contact and once-per-segment target-hex use.
The protocol separates private bookkeeping from frozen combat facts and distinguishes preparation,
irreversible commitment, settlement and closure.

## Plan Review

- [Identity decisions](../design/combat-opportunity-identity-v1.md#identity-decisions) cover stable
  unit/component identity, frozen participation, target-hex use, relation provenance and separate
  public/internal bindings.
- [Lifecycle](../design/combat-sealed-decision-protocol-v1.md#decisions-and-state-model) has explicit
  transitions/preconditions. The second seal derives `Prepared`; deadline cancellation applies
  only while collecting.
- [Commit/recovery](../design/combat-sealed-decision-protocol-v1.md#events-atomicity-and-strict-readback)
  preserves pre-RNG history append and permits certified structural advance without accepting
  arbitrary combat-base changes.
- [Handoffs](../design/combat-sealed-decision-protocol-v1.md#compatibility-and-implementation-handoff)
  assign remaining transitions, costs/results, settlement/disclosure, cycle composition and exact
  contract freeze. All 24 acceptance cases map to numbered decisions.

Navigation consistently identifies DES-003 next, preserving research approval and production gates.
No heavy pivot is warranted.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status | Review consequence |
| --- | --- | --- | --- |
| Five documentation files; no runtime changes | Frozen Git diff, log and clean status | Confirmed | Documentation-level checks are appropriate. |
| Two private slots share one frozen base | Protocol decisions/state/revisions; AC-001/007 | Confirmed | Hidden first-seal bookkeeping cannot alone stale the other candidate. |
| Missing choices cancel voluntary rounds without invented assignments | Deadline/cancellation policy; AC-004/005/011 | Confirmed | Mandatory-attack profiles remain excluded. |
| Prepared/committed recovery requires no controller | Lifecycle/continuations; AC-006/008 | Confirmed as design requirement | Runtime durability still requires implementation evidence. |
| Existing code supplies patterns, not a hosted Combat lifecycle | Action mapping, proposal validator, protobuf, Host/Worker entry points | Confirmed | No unsupported reuse or implementation claim. |
| Exact codecs/versions/sizes/migrations remain deferred | Binding, compatibility and handoff sections | Confirmed | Packet is not a serializer or wire-contract specification. |
| 175 local targets, 15 decisions and 24 acceptance rows pass | Independent Python checks | Confirmed | Structural evidence reproduced; it does not prove runtime races. |

Author drafting chronology and historical tool invocations were not independently audited; neither
has a readiness consequence.

## Verification Performed

Against the clean review worktree at `893aeec`:

- `git status --short --branch` and `git rev-parse HEAD`: expected branch/head; clean.
- `git diff --name-status c210bb2..893aeec`: exactly five scoped documents.
- `git log --format='%h %s' c210bb2..893aeec`: three expected commits.
- `git diff --stat c210bb2..893aeec`: 560 insertions, 11 deletions.
- `git diff --check c210bb2..893aeec`: passed.
- Read-only Python standard-library checks: 175 local path targets, zero missing; one local heading
  anchor resolved; 15 decision IDs and 24 acceptance IDs unique and ordered.

Reviewer inspected canonical cycle/Contact requirements, mutable-state/RNG/Reserve research,
relevant current code, and Land source renders on PDF pages 15, 18, 22 and 24. State traces covered
both seal orders, deadline equality, duplicate submission, cancellation disposal, structural advance
and restart after preparation/commitment. These were design reasoning checks, not executable
runtime tests. No .NET build or tests were run.

## Open Questions And Residual Risks

- Contract freeze must give `requiredSlots` an acyclic canonical encoding, excluding identifiers
  derived from the round itself.
- DES-005 must settle hidden-legality equivalence and permitted terminal disclosure; generic error
  labels alone cannot establish privacy.
- DES-003/004/005 and cycle design must close every reachable retreat, custody, relation, resource
  and continuation obligation.
- Hosted clock confidence, durable publication, timer/outbox recovery and traffic analysis require
  implementation evidence.

These are assigned downstream gates, not unreported approval or reasons to expand this review.

## Verdict

**Ready** for bounded DES-001/DES-002 design inputs. This does not approve pending policy choices,
production contracts or runtime activation.

## Recommended Next Actions

Proceed with DES-003. Preserve remaining gates and require combined contract/implementation-plan
review before production work.

**Author reconciliation:** No findings require Accept/Dispute/Defer treatment. Retain all downstream
gates above. Review flow closes at instance 1 of 3; no material fix requires another pass.
