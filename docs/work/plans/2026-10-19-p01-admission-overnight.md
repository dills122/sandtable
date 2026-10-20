# P01 admission proof: overnight execution packet

**Status:** working plan, not a support certificate or admission decision.
**Baseline:** `228aec8ce65e1d901fe667f3c5f47e1511002a14` (#283).
**Authority:** [P01 acceptance matrix](../../specs/combat-actual-moved-repeat-position-v1.md), accepted `CMB-POL-001`, `004`, `009`, and the pinned original CreationRequest plus eleven R03 ledgers.

## Decision this run must enable

Determine whether the bounded initial-RC P01 profile has a complete, source-backed support certificate for its original 300 authenticated rows. Preserve the current `44` provisional positive / `256` candidate-absent classification and `0` admitted actions until every gate below passes on one reviewed head. A provisional absence is never a supported-empty result.

| Gate | Current evidence | Overnight proof or explicit blocker |
| --- | --- | --- |
| AC-003 action and provenance | Authenticated R03 ancestry, Weather bridge, action matrix and selected private continuations. Matrix states `sourceLegalActionExhaustive=false`. | Enumerate all relevant source actions and accepted policy exclusions. Prove no alternate action inside the bounded profile with fail-sensitive tests; identify any case that needs new typed authority. Do not describe the profile as fully source-legal while voluntary surrender and movement RBA are excluded. |
| AC-006 surrender and RBA | Private explicit defender decline/cancellation, selected-assault and Force Assignment paths. | Authenticate automatic-surrender trigger and settlement, or show exact missing source/input path. Timeouts cannot synthesize decline. Current 300 roots may not contain a positive trigger; a negative classification alone cannot close this gate. |
| AC-007 outward privacy | Private `p01.` candidate hash and disconnected owner-projection v0 scaffold. | Prepare exact owner-visible fields, scope, revision, alias and rejection contract for owner review. Then authenticate two complete equal-visible histories with different hidden facts and compare candidate bytes, IDs, acceptance and rejection. Synthetic twins and denial-only output do not close the gate. |
| AC-008 replay and resources | #281 two-root fresh-process readback and #283 all-44 in-process prepared/committed cut matrix. | Prove all 44 paid roots cross a genuine fresh process boundary at both cuts with byte-equal Control, World and receipt, accepted retry without second debit, physical negatives and frozen caps. Extend to the remaining 300-row and nonterminal-cut requirements before calling AC-008 complete. |

## Work order and stop rules

1. Resume the saved all-44 fresh-process worker without replacing its worktree or untracked scripts. Benchmark four roots, then the full 44; stop without a PR if the proof cannot fit the measured CI budget or a fresh process is not real for every root.
2. Run independent AC-003 and AC-006 source audits in disjoint worktrees. Produce code only for behavior-bearing, fail-sensitive proof. If current authenticated inputs cannot support a positive branch, retain a precise blocker rather than adding another diagnostic-only PR.
3. Draft the AC-007 owner decision packet before seeking approval. No public or native candidate is issued from the private digest. The proposal must pin campaign/window scope, owner-visible participant and target references, visible revision, alias encoding, and side-safe rejection behavior; authentic twin histories are a separate executable requirement.
4. For each reviewable slice: focused deterministic tests, pinned full audit, draft PR, fresh independent review, exact-head CI, unchanged clean mergeable head, then merge. Re-run review and CI after every fix. Record observed outcome in Ruflo, not an inferred completion.
5. Only after AC-003/006/007/008 and the complete P01 support certificate pass on one reviewed head, consider P02 selection, P03 native consumption, public activation, Exercise and Runner. These are separate gates. Do not claim a full Combat loop or playable Maproom from private proofs.

## Owner decision still needed for AC-007

The [owner projection v0](../../specs/combat-p01-owner-projection-v0.md) proposes four alias inputs: side, visible decision revision, capability kind and apparent target reference. It deliberately has no approved owner observation or positive outward action. Before contract freeze, present a concrete proposal covering:

- which own participant and apparent target facts the player may see, including an authorized no-target case;
- campaign and decision-window scope without hidden authority hashes, versions, event counts or private IDs;
- visible revision changes only when approved owner-visible facts change;
- canonical alias byte encoding and collision/ambiguity rejection; and
- generic side-safe stale, unavailable and malformed rejection shapes that do not reveal a hidden blocker.

Approval of wording does not replace the authenticated equal-visible hidden-history test. If the accepted fields cannot yield such a pair from original sources, AC-007 remains open and P01 remains unsupported.
