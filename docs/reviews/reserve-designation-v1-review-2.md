# Independent Review 2: Reserve Designation v1 Planning

**Review instance:** 2 of 3

**Date:** 2026-08-24

**Mode:** Fresh-task, blind-first, read-only review of the review-1-corrected target

**Verdict:** Changes requested; Task 002 remains blocked

## Review-1 correction result

The reviewer confirmed that all four review-1 findings were substantively resolved: Capability Point
terminology, stateless semantic controller design, layered validation, and the public observation
reserve-status type are now sound. The delivered Stage Entry harness adoption remained accepted.

## Findings and author responses

### IR2-001 — P1: Core identity-lane ownership was incomplete

The create v6/snapshot v7 tasks omitted `CampaignEngine.cs` and
`CampaignEventSerializer.cs`, which hardcode the current creation/snapshot identities.

**Author response:** Accept. Task 004 now owns `CampaignEngine.cs` with the create command/event/state
contracts and authoritative validator. Task 004A owns `CampaignEventSerializer.cs`, projector, and
focused creation/event/snapshot/replay tests. The Core identity lane must be green before Task 005
or candidate work.

### IR2-002 — P1: Retained checked v1 fixtures had no migration owner

Strict Exercise/Maneuver manifest v2 admission would invalidate the retained Organization and
Reserve-boundary Exercise twins and both existing serial Maneuvers.

**Author response:** Accept. Task 011A now renames/migrates all four retained checked Exercise
fixtures from `.v1.json` to `.v2.json` and updates command references/goldens. Task 012A does the
same for both retained serial Maneuvers. Task 013 refreshes remaining identity/hash goldens. Tasks
014/015 then add the new Movement-terminal v2 profiles. Stale v1 filenames are removed during the
clean cut.

### IR2-003 — P2: Exercise evidence does not contain Campaign Observation

The design incorrectly required the Exercise step-evidence decoder to migrate to observation v3.

**Author response:** Accept. The evidence requirement now names only snapshot v7/world v2 and the
new event/config identities. Campaign Observation v3 remains a Core observation-contract/test
concern and is not claimed as an Exercise payload.

## Confirmed by reviewer

- Capability terminology/no-mutation, controller semantics, v2 persisted policy identities, layered
  validation/formulas, historical preservation, and observation boundary pass.
- Requirements and acceptance cases cover zero/one/all designation, rejection, replay, fog,
  versioning, harness evidence, and Movement stop.
- No Reserve production code exists.
- Harness Stage Entry event admission and standalone/two-setup coverage remain sound.

## Re-review decision

The review loop has one remaining instance. Review instance 3 of 3 must verify the corrected Core
and fixture ownership inventory before Task 002 begins. No further review instance may be created
after that report.
