# Ordinal-two Combat Position Determination after R03

**Status:** Decision-ready private contract plan; candidate and action support have not been
certified. No executable contract or native consumer is authorized by this document.

**Base:** `e68e4e3c754de8032ce6b8423ea0ae33f5558077` (main after PR #214).
**Predecessor:** Frozen [R03 reference](../../specs/combat-actual-moved-repeat-breakdown-completion-v1.md)
and merged private Core consumer (PRs #211 and #216). The open moved-repeat Breakdown draft
PR #200 is a different, superseded checkpoint; it is not a predecessor.

## Decision boundary

R03 authenticates the original CreationRequest, ten independent R02 ancestor ledgers, and its
own Breakdown ledger. It ends at ordinal-two Combat Position Determination with one new System
receipt in both cumulative and local ledgers. Its 300 terminals comprise 36 paid and 264 armed
rows. R03 executes no Combat selection, action, or phase closure. The next contract must start
from that exact terminal, not an ordinal-one positive-entry state or a supplied R03 Proof.

The next executable slice should be **position admission and selection disposition only**:
derive the acting side, candidate set and supported choices from authenticated World, Weather,
cycle and source-backed rules; open the Combat segment; then either close a proven empty
selection by System action or accept one authenticated owner choice into a separately certified
ordinal-two selection path. Stop before Round, assault cost, RNG, result, Reserve Release or another movement.
If either branch needs a wider rule family, split it into a later contract rather than treating
an unsupported candidate as absent. The positive-choice arm is conditional on the inventory below.

## Required inventory before freezing shapes

1. Replay all 300 R03 terminal rows from original CreationRequest and eleven independent input
   ledgers. Retain row key, root ordinal, movement cut, paid/armed profile, acting side, exact
   R03 Source/Control/Proof bytes and their hashes, and both ordinal-one and ordinal-two
   MovementEndProofs. Count every original and active receipt, target use, membership, obligation,
   World, resources, RNG and progress record. No fixture row, cached Proof or source digest is
   authority for this replay.
2. For each terminal, derive the complete candidate predicates using adopted source rules and
   current native support: actual attacker/defender location and representation, Weather, CP,
   Cohesion, ammunition/TOE, adjacency, prior target use, eligibility, ongoing relationships,
   and active ordinal. Partition into supported-empty, supported-positive and unsupported,
   preserving each failing reason. A rejection or missing support must never become an empty
   candidate set. Confirm whether positive candidates remain after paid attack CP and the armed
   movement geometries. Four exploratory rows are insufficient: sampled paid worlds had attacker
   CP 9 or 15, while one armed world had CP 0 and a defender at the rear location.
3. Record the actual rule/table/errata locators behind every predicate and decision budget.
   The [source inventory](../../research/combat-cycle-source-inventory.md#source-index) names
   Land 5.2, 8.21–8.25 and 10.31–10.36 plus corrected errata as starting locators;
   [CYCLE-DEC-007/008](../../research/continual-cycle-identity-and-history-decision.md#decision-table)
   already establish that same-pair attack history is evidence, not an automatic veto. Recheck
   current ammunition/CP and exact selection support for ordinal two. Freeze a new ruling only
   for a genuine unresolved ordinal-two or paid/armed distinction. Existing ordinal-one selection
   policy is a semantic pattern, not evidence that ordinal-two choices or windows are supported.
4. Recheck every R03 physical source pin against the feature head and add the frozen R03 schema
   and verifier as separate delivery pins. Both parent catalogues are already among the 108 R03
   runtime pins and must remain present. On this base, all 108 R03
   runtime hashes match; its schema SHA-256 is
   `26909e67501f59a0d0892181b9c9499099552c721585369983c3dbde5784c955` and verifier
   SHA-256 is `f79ca3b29c2a61c3aa8957f0e2873fc4968c8db85cd0e71183960e4489928bd3`.
   A changed, missing, unreadable, null or drifting byte source must fail closed before replay.

## Proposed contract split

**P01, position and support certificate.** Add one closed v1 spec, ordered schema, executable
reference and compact retained vectors. Its Source carries the complete R03 Source and only its
own event suffix; the caller supplies the original CreationRequest and all eleven ledgers
independently. Base binds full R03 replay proof, active ordinal-two cycle, position/version,
ruleset/configuration identity, original creation provenance and physical dependency manifest.
The reference derives a private support certificate and candidate set without a state-changing
event. Do not publish P01 as frozen until the complete 300-row partition and unsupported reasons
are retained, reviewed and source-pinned.

**P02, selection transition.** In a separate increment, define closed Command/Input/Event,
Control/Proof and exact canonical byte domains for System segment opening, supported-empty
closure and any supported-positive owner choice. Bind actor, decision/candidate/set identity,
expected version and position, exact window/deadline policy when applicable, and retry identity.
Keep System fallback authorship distinct from accepted owner intent. Append each event receipt
once to cumulative and local ledgers; exact retry returns original event/receipt and current
state without a second publication. Preserve both Movement proofs, original and active cycle,
World/resources/RNG, source provenance, progress, members, target uses and all prior receipts.
Fresh admission disabled must still permit authenticated replay and accepted retries.

**P03, native consumer.** Only after P01/P02 reference freeze and independent review, add an
additive private Core codec/consumer on a separate branch. Compare every admitted terminal/cut,
event, retry, Source/Control/Proof byte and rejection class with retained reference vectors.
Measure installed test discovery, time and memory before assigning CI partitions. Public
actions, Exercise/Runner, host ingress and later Combat steps are separate gates.

## Acceptance and stop conditions

- Show a genuine missing-consumer RED only after authentic R03 terminal replay. The P01/P02
  reference must reproduce every admitted row, event cut and accepted retry, including both
  paid and armed roots; challenge all nonterminal R02/R03 cuts.
- Compare exact canonical bytes and source pins, not merely semantic state. Derive structural
  size/depth/array budgets from inherited maxima and new suffixes. Test just-fitting and
  just-over framing, malformed numeric leaves, duplicate/reordered keys and physical pin faults.
- Challenge wrong side/System authority, stale or changed decision, re-signed candidate or
  effect, altered original/active Movement proof, changed prior target use, source substitution,
  reordered ledgers, changed World/CP/RNG, and forged readback. Verify no hidden opposing state
  crosses any outward boundary; this private contract itself exposes no public observation.
- Keep the canonical [roadmap](../../roadmap/pre-alpha-roadmap.md),
  [Combat plan](../../design/combat-cycle-implementation-plan.md), README, tech design and naming
  summary synchronized **after** an executable boundary passes its own gate. This planning
  packet changes no delivery state.

**Current blocker:** The full 300-row candidate/support partition and its physical rule pins are
not yet retained. Without them, neither a positive owner-choice path nor an empty-selection
path can be honestly frozen. The next task is the bounded P01 inventory and source decision;
do not start a native consumer or public activation from this plan alone.
