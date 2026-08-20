# Exercise Capability And Replay Spike

**Status:** Complete; decisions adopted by the proposed specification

**Date:** 2026-08-19

**Decision owner:** Sandtable project owner

**Governing specification:** [Exercise Harness v1](../specs/exercise-harness-v1.md)

**Delivery plan:** [Exercise Harness v1 technical design](../design/exercise-harness-v1.md)

## Decision question

What is the smallest Core-owned Exercise capability and replay contract that gives a local runner
enough trusted evidence for deterministic verification without exposing arbitrary production
campaign state, duplicating the legal-action mutation path, or making runner orchestration concerns
part of the Umpire domain contract?

## Adopted decisions

| ID | Decision |
| --- | --- |
| `DEC-001` | Domain terminal outcomes and harness failure categories are disjoint closed types; an expected negative-test category never becomes success. |
| `DEC-005` | Ordinary submission and Exercise submission share one internal exact-membership, command-mapping, adjudication, event-count, and projection primitive. |
| `DEC-006` | The Exercise session is opaque, fresh-only, and non-convertible to or from `CampaignAuthorityHandle`. |
| `DEC-011` | Exercise Begin accepts admitted creation inputs and shares one internal creation primitive; no API accepts arbitrary authority, snapshots, history, or resume input. |
| `DEC-012` | Core owns reconstruction from session-retained history; the runner proves re-adjudication through a second fresh Exercise session using accepted audience/action identities. |

## Why this matters now

The current public authority handle intentionally hides snapshots, events, content context,
commands, and replay primitives. A runner outside Core cannot produce canonical evidence from the
safe outward receipts alone. The wrong seam would create a second authority API or make trusted
export possible from any in-process production campaign handle.

## Authorized scope and prohibited actions

In scope:

- current Core creation, legal-action, adjudication, projection, serialization, replay, and
  architecture-test seams;
- read-only comparison with the named Liar's Dice and Reef references;
- proposed contracts, ownership, validation, and acceptance evidence.

Out of scope:

- production code changes;
- `InternalsVisibleTo` for the runner;
- Orleans, Chronicle persistence, remote execution, or model-backed decisions;
- changes to either reference repository.

## Source hierarchy

1. `AGENTS.md` and accepted Sandtable specifications/designs.
2. Current Cna.Core code and tests at checkpoint
   `4277d3867350204f0236fc5e6a62cb3d69131787`.
3. Retained reference-repository observations, labeled as non-normative.

## Decision criteria

- preserves one authoritative execution path;
- cannot export trusted state from an ordinary `CampaignAuthorityHandle`;
- uses stable semantic action identity rather than list position;
- supports distinct reconstruction and re-adjudication proofs;
- keeps manifest/report/retention policy outside Core;
- permits enforceable architecture and behavioral parity tests;
- minimizes permanent public Core surface.

## Stop condition

Stop when the capability lifecycle, public/internal type boundaries, shared execution primitive,
step evidence, replay inputs/equality, rejected-probe handling, terminal outcome model, and focused
acceptance tests are concrete enough to place in the governing specification without unresolved
authority ambiguity.

## Evidence

### Repository facts

- `CampaignAuthority.Create` maps one outward creation request into an internal `CreateCampaign`
  command, authoritative creation decision, exact content resolution, projection, and an opaque
  `CampaignAuthorityHandle`. It discards the creation event from the outward result. See
  `src/Cna.Core/Campaigns/CampaignAuthority.cs`.
- `CampaignAuthorityHandle` exposes no authority-bearing public member and keeps snapshot/context
  internal. See `src/Cna.Core/Campaigns/CampaignAuthorityHandle.cs`.
- `CampaignLegalActions.Submit` currently re-derives exact-audience membership, maps the candidate
  to an internal command, adjudicates, applies the first accepted event, constructs the successor
  handle, and emits a side-safe receipt. See `src/Cna.Core/Actions/CampaignLegalActions.cs`.
- Candidate IDs are SHA-256 identities over versioned candidate semantics, not list positions.
  Submission re-queries the current action set and matches by ID. See
  `src/Cna.Core/Actions/CampaignActionCandidate.cs` and `CampaignLegalActions.Submit`.
- `CampaignProjector.Replay` reconstructs trusted snapshot state from canonical events.
  `CampaignReplayHarness.Execute` instead re-adjudicates internal commands. These are already two
  different proof mechanisms. See `src/Cna.Core/Campaigns/CampaignProjector.cs` and
  `src/Cna.Core/Campaigns/CampaignReplayHarness.cs`.
- Current architecture tests keep raw authority/replay types internal, grant friend access only to
  tests, and allow only OrleansHost to reference Core in production. See
  `tests/Cna.Core.Tests/Campaigns/AuthorityBoundaryTests.cs`.
- The current supported vertical slice reaches exact position
  `land.position.operation-1.organization` after Weather and has no complete victory-capable game.

### Repository observations

- Existing parity tests compare normal public submission with direct internal mechanic
  adjudication and canonical successor snapshot bytes. The evidence seam can extend this test
  pattern instead of defining a new adjudicator.
- Existing legal actions currently produce exactly one event per accepted action and receipts
  require committed state version `prior + 1`. The artifact schema can allow an ordered event
  collection, but v1 parity must preserve the one-event receipt contract rather than invent
  multi-event outward behavior.
- A runner cannot obtain creation/action events or trusted snapshot bytes from current outward
  results. Storage/reporting code alone cannot close this gap.

### Inferences

- A trusted export method that accepts an ordinary `CampaignAuthorityHandle` would allow any
  in-process Core consumer to convert a production handle into raw evidence. That contradicts the
  handle's safety purpose.
- A new submission implementation inside an Exercise façade would be a second mutation path and
  could drift from legal-action membership, validation precedence, receipt behavior, or future
  mechanics.
- A capability minted only for a fresh isolated Exercise cannot be used to attach to an existing
  production campaign. It can therefore expose trusted evidence without generalizing production
  handle export.
- Deterministic re-adjudication does not require exposing internal commands: the runner can begin a
  second Exercise, re-query each recorded audience/action ID against the current checkpoint, and
  submit through the same Exercise path.

### Unknowns resolved by the decision

- Exact symbol names remain implementation choices, but the capability and ownership constraints
  below are normative.
- Future activation replay from Chronicle remains separate. This decision does not create a live
  production activation or mutation path.

## Options and recommendation

| Option | Authority safety | Parity | Cost | Decision |
| --- | --- | --- | --- | --- |
| Give the runner friend/internal access | Poor; exposes all Core internals | Fragile | Low initially | Reject |
| Export trusted evidence from any ordinary authority handle | Poor; punctures the production handle boundary | Medium | Low | Reject |
| Route v1 through Orleans/Chronicle | Safe if fully designed, but adds unrelated activation/provenance work | Medium | High | Defer |
| Mint an isolated Exercise session and reuse shared internal creation/submission primitives | Strong and testable | Strong | Moderate | **Recommend** |

### Recommended capability boundary

1. Add a dedicated public namespace such as `Cna.Core.Exercises`; exact names may change without
   changing the boundary.
2. `Begin` accepts only the Core-relevant creation inputs already represented by
   `CampaignCreationRequest`. The complete runner manifest, controller policy, artifact settings,
   expected outcome, retention, and reporting stay in `Cna.ExerciseRunner`.
3. `Begin` mints a sealed, non-record, non-serializable `CampaignExerciseSession`. It is not
   convertible to or from `CampaignAuthorityHandle`, cannot be created from one, and exposes no raw
   internal type.
4. Extract one internal creation primitive used by both `CampaignAuthority.Create` and Exercise
   `Begin`. It returns internal snapshot/context plus the accepted creation event to the two
   outward mappers.
5. Extract one internal action-execution primitive used by both `CampaignLegalActions.Submit` and
   Exercise `Submit`. It owns membership re-derivation, command mapping, adjudication, validation
   that the current v1 decision emitted exactly one event, projection of that event, successor
   authority, and receipt construction. It never truncates an unchecked event collection.
6. Exercise query delegates to the same legal-action query logic. It returns the existing
   side-safe action-set result; the session itself remains opaque.
7. Accepted Exercise submission returns a successor Exercise session plus a trusted immutable
   evidence value containing defensive copies of the exact canonical event bytes and canonical
   successor snapshot bytes. Rejection returns no event bytes and no successor; its evidence
   records the typed rejection and proves the retained session checkpoint did not change.
8. The Exercise start result similarly exposes the exact canonical creation-event and initial
   snapshot bytes for the newly minted isolated session only.
9. Core verifies history reconstruction from the session's accepted event history and reports
   canonical event-stream/final-snapshot hashes. The runner independently performs
   re-adjudication by starting a second session and replaying the accepted
   audience/action-identity transcript.
10. The runner declares success only when the current checkpoint exactly equals a manifest-owned
    domain terminal such as `BoundaryReached("land.position.operation-1.organization")` and both
    replay proofs pass.

### Terminal and failure separation

- Core exposes checkpoint facts; the runner owns expected bounded completion.
- Success types are domain outcomes only: v1 `BoundaryReached(positionId)` and future
  `VictoryReached(victor)`.
- Harness, invariant, replay, artifact, interruption, step-limit, unexpected no-action, invalid
  manifest, and unsupported-mechanic conditions are unconditional failure categories.
- Negative tests may assert an expected failure category, but the Exercise remains failed and the
  process exit remains nonzero.

## Limitations and unknowns

- Exact public evidence DTO names and whether snapshot bytes are returned every step or through an
  explicit trusted checkpoint call should be chosen for the smallest allocation/API surface.
- V1 should retain an ordered collection in evidence even though current accepted actions emit one
  event; normal receipt parity remains `prior + 1` until the governing legal-action contract
  changes.
- Any future ability to resume an Exercise from retained history requires a separate capability
  and security review. This spike authorizes verification, not activation.
- Controller determinism is outside Core. Re-adjudication proves the Umpire given a fixed accepted
  action transcript; a separate runner test proves controller transcript repeatability.

## Implementation consequences and next gate

Required first checkpoint:

1. specify the opaque Exercise session and trusted byte-evidence contracts;
2. extract shared creation and action-execution primitives without changing public receipts;
3. add real-assembly tests for the new allowed Core host and forbidden direct raw-type access;
4. add parity tests proving normal and Exercise creation/submission produce identical accepted
   receipts and canonical successor state;
5. add tests proving an ordinary authority handle cannot enter any trusted Exercise API;
6. add reconstruction and transcript re-adjudication tests before a filesystem writer is added.

The governing specification may proceed with this boundary. Production implementation should not
begin until the final independent plan review accepts it.
