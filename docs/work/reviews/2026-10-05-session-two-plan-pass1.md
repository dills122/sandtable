**Not ready — one bounded planning correction is required before dispatch.**

Review instance: **S0 set1/pass1, total1of9**, maximum3 passes per set.

## Findings

**P2 — S1’s three-file repair allowance misses transitive fixture pins.**  
The plan (historical local artifact: `/Users/dsteele/repos/sandtable/.planning/combat-session-2/session-plan.md:28`) anticipates updating the Breakdown fixture and possibly its verifier. However, the [inherited-selection fixture](../../specs/fixtures/combat-inherited-selection-v1.json) pins that fixture’s exact bytes, and its [verifier](../../specs/verify-combat-inherited-selection-v1.py) rejects any mismatch. Snapshot independently enforces the same dependency.

A read-only inventory found **11 fixture paths, including Breakdown itself**, in the recursive hash-reference closure. This is a reference inventory; I did not execute every affected oracle. Updating only the proposed Breakdown pin would introduce downstream failures and undermine S3’s unchanged-predecessor gate.

Smallest correction: make S1 begin with a dependency inventory and an explicit maintenance go/no-go decision. Freeze a scope that accounts for downstream evidence before editing. If it exceeds the permitted slice, retain the original failure and return a coordinator scope decision; do not silently refresh the chain. Adjust S3’s dependency and timing accordingly.

## Plan Review

The technical sequence otherwise matches the accepted roadmap and previous handoff:

- Actual019E1 reaches a supported candidate **before selection**.
- Existing C3a admission still pins a different creation request; receipt, position, Weather and RNG mapping require a separately accepted bridge.
- S2 research precedes contract and native implementation.
- Synthetic Result2 contexts retain their trust classification.
- Repeat, later-II/consumed lineage, public activation and parent017–019 closure remain open.
- TDD, immutable ownership, adversarial ordering, fresh reviews, exact-head CI, serialized full-suite execution and preservation rules are appropriate.

The schedule is plausible for conditional delivery. S4 should remain optional; the newly identified maintenance scope could consume its reserve.

## Author-Claim Reconciliation

No separate author rationale was supplied or needed. I checked the proposed plan’s claims against canonical requirements, contracts, source and the prior final review.

The actual-entry boundary and downstream incompatibility claims are confirmed. The assumed S1 repair size is contradicted by retained hash dependencies. Previous test counts and duration estimates were inspected as historical evidence, not independently rerun.

## Verification Performed

- Confirmed `main` at requested baseline `afa396ad5094fae7b8f054c60a9df9e03f83dfd5`.
- Verified existing user changes and untracked tooling/handoff remain present.
- `git diff --check` — passed.
- `python3 -B docs/specs/verify-combat-positive-entry-v1.py` — passed: two owners, six literal proofs/cuts/retries,3966 rejection probes.
- `python3 -B docs/specs/verify-combat-inherited-breakdown-completion-v1.py` — exit1, reproduced source drift for `src/Cna.Core/Rules/Cna1979LandSequence.cs`.
- Inspected downstream pin-enforcement predicates and recursive fixture references.

Serena was activated for the exact checkout. Codebase Memory lacked recorded coverage for the positive-entry engine; focused source reads supplied that evidence. No full .NET gate or hosted CI refresh was performed.

## Open Questions And Residual Risks

S2 must still establish an achievable bridge manifest. This review does not certify future selection, round or Result2 implementation.

No gameplay or architecture pivot is established. The immediate issue is evidence-maintenance scope and its effect on dependent work.

## Verdict

**Not ready** for the current complete session plan. S2’s read-only research remains technically sound, but S1’s repair boundary needs reconciliation before implementation dispatch.

## Recommended Next Actions

Correct S1’s dependency inventory, scope decision and S3 gate; recalculate the reserve; return the materially revised plan for a fresh counted review. No files were edited, fixes dispatched or additional reviewers created.
