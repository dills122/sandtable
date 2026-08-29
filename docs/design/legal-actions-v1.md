# Legal Actions v1 Technical Design

**Status:** Implemented baseline; Task 006 dormant evolution implemented

**Date:** 2026-08-17

**Specification:** [Legal Actions v1](../specs/legal-actions-v1.md)

**Research:** [Turn-Preamble Action Boundary](../research/turn-preamble-action-boundary-spike.md)

**Current evolution:** The original v1 delivery below is historical design context. Reserve
Designation clean-cuts only the action-set envelope to contract 2 / `sandtable.legal-actions.v2`
for audience-visible revision semantics; candidate, submission, and receipt contracts stay at 1.
Movement Foundation `MOV-TASK-006` now owns the implemented dormant Movement output contracts,
pure observation-derived vectors, and strict canonical set/submission/receipt readback.
`MOV-TASK-008` owns atomic public membership after internal adjudication and completion are both
executable. The current action set intentionally has no Movement member.

## Current Task 006 evolution

Candidate contract version 1 gains output-only `MoveElementAction` and
`CompleteMovementSegmentAction` values. A move carries own element ID, origin, destination, and a
closed explanatory exact-cost value containing:

- destination terrain ID and exact destination-terrain cost;
- a nullable route adjustment with route ID, `override` or `scale-underlying` behavior, and exact
  amount;
- canonically ordered crossed-hexside additions with feature ID, `either`, `up`, or `down`
  direction, and exact added cost; and
- one exact total coherent with the component semantics.

Completion has no target. Each action ID remains lowercase SHA-256 over the complete typed
side-safe canonical semantic preimage excluding the ID. An internal pure deriver consumes only
Campaign Observation contract 5 and produces deterministic dormant vectors, but the public side
generator does not publish them.

The action-set, payload-free submission, and mechanic-neutral receipt shapes retain versions 2, 1,
and 1. Their strict readers accept only exact field order/shape, closed discriminants, canonical
exact amounts and hashes, locally coherent cross-fields, and byte-identical canonical
reserialization. These readers are compatibility tools, not authority: execution still re-derives
exact current-audience membership. Task 006 adds no Movement command, event, command mapping,
adjudication, state mutation, or accepted receipt; Task 007 is next and keeps membership dormant.

## Delivery shape

Implement `ACTION-001` as one atomic Core contract cutover with four internal layers and one narrow
public surface:

```text
public creation request
    -> internal admission / event / projection
    -> opaque CampaignAuthorityHandle
        -> public side observation
        -> public legal-action query
        -> public action submission
            -> internal current-membership check
            -> internal mechanic command / event / projection
            -> successor handle + side-safe receipt
```

No public API accepts or returns a snapshot, context, command, event, random state, or complete
Content Pack. `Cna.OrleansHost` may retain the opaque handle. `Cna.DecisionWorker` removes its Core
reference. Internal replay remains test evidence only until `HOST-001` defines authenticated
activation and Chronicle persistence.

## Contract cutover

Use the exact version matrix in the specification:

- setup definition/snapshot schema 3;
- opening-preamble policy contract 1;
- `CreateCampaign`, `CampaignCreated`, and `CampaignSnapshot` contract 4;
- operation-stage order contract 1;
- new convoy/declaration commands and events contract 1;
- legal action set/candidates/submission/receipt contract 1;
- unchanged Initiative event 2, Campaign World 1, Land sequence 2, Observation 1, and ruleset
  manifest 2.

The new executable does not read setup 2, creation/snapshot 3, or the prior ruleset hash. Existing
version-2 Initiative events remain valid only after version-4 creation. The prior Git revision is
the historical executable for the former golden fixtures.

## Setup policy and ruling

Add a closed `CampaignOpeningPreamblePolicy` value:

```csharp
internal sealed record CampaignOpeningPreamblePolicy(
    int ContractVersion,
    CampaignOpeningPreambleKind Kind,
    IReadOnlyList<RuleReference> Sources);
```

The only kind is `NoOpeningNavalConvoyObligations`, canonically
`no-opening-naval-convoy-obligations`. Its only source is
`sandtable-rules-lab:opening-preamble.no-naval-convoy-obligations.v1`. Both catalog setups carry the
same value nested under `openingPreamble`; top-level setup sources remain Initiative-specific.

Setup hashing writes `openingPreamble` after `initialInitiative` and before `content`, including
contract version, kind, and sorted sources.

Add ruling `cna-1979.1.ruling.explicit-empty-opening-convoy-resolution` to the existing manifest-2
rulings array with the exact conflict, alternatives, selected behavior, sources, and protecting test
IDs from the specification. This changes the canonical ruleset hash but not the Land sequence
artifact hash or manifest schema.

## Authority state and events

Add `CampaignOperationStageOrder` contract 1 with Operation Stage, first side, and second side.
Constructor validation requires stage 1-3, defined distinct sides, and exact complement. Snapshot 4
stores an ordinally/canonically ordered immutable unique-stage collection.

Add internal commands:

- `ResolveNoObligationNavalConvoySchedule`;
- `ResolveNoObligationTacticalShipping`;
- `DeclareInitiativeOrder` with closed `ActFirst`/`ActLast` choice.

Add internal events:

- `NoObligationNavalConvoyScheduleResolved`;
- `NoObligationTacticalShippingResolved`;
- `InitiativeOrderDeclared`.

Each event carries contract version, campaign/state identity, `fromPositionId`, exact next sequence
position, and sorted sources. Declaration additionally carries Operation Stage, declaring holder,
first side, and second side. Convoy events cite Land Rules 5.2 plus the admitted policy source;
declaration cites 5.2, 7.11, 7.14, and 7.16.

The engine validates command version, concurrency coordinates, exact current position, initiative
state, and policy. It emits one event or a typed rejection. The projector independently recomputes
the expected event and applies it. No generic sequence event is accepted.

Snapshot validation admits exactly:

| State | Position | Holder | Orders |
| --- | --- | --- | --- |
| 1 | Initiative Determination | none | empty |
| 2 | Naval Convoy Schedule | set | empty |
| 3 | Tactical Shipping | set | empty |
| 4 | Operation 1 Initiative Declaration | set | empty |
| 5 | Operation 1 Weather | set | exactly Operation 1 |

Only Initiative changes the random cursor. Declaration order must be one of the two complements
relative to the retained holder.

## Opaque public authority surface

Make snapshot, setup snapshot, world state, content context, commands, events, authority serializers,
engine, projector, replay preparation, replay result/harness, and mechanic resolvers internal. Tests
retain access through the existing `InternalsVisibleTo="Cna.Core.Tests"`; no production friend is
added.

Add a public sealed non-record `CampaignAuthorityHandle`. It has an internal constructor and
internal snapshot/context properties only. It exposes no public property, field, event, indexer,
deconstructor, serializer, or equality over contained authority. `ToString()` returns the constant
`CampaignAuthorityHandle`.

Add a public creation facade accepting an immutable contract-1 `CampaignCreationRequest` containing
the existing exact creation inputs. It resolves the resident synthetic catalog, executes internal
creation/projection, and returns either a handle or a stable creation rejection. It never returns an
event or snapshot.

Add a public observation facade accepting handle plus side and returning the existing side-safe
Campaign Observation v1 result. The raw snapshot/context projector becomes internal.

Remove the unused `Cna.Core` project reference from `Cna.DecisionWorker`. Add project/public-API
tests proving only OrleansHost references Core, no production friend exists, and forbidden authority
types/methods are not public.

## Legal-action contracts

Add public closed `CampaignActionAudience`: `System`, `Axis`, `Commonwealth`.

Add five output-only concrete candidates with internal constructors:

- `ResolveInitiativeAction` -> `resolve-initiative`;
- `ResolveNoObligationNavalConvoyScheduleAction` ->
  `resolve-no-obligation-naval-convoy-schedule`;
- `ResolveNoObligationTacticalShippingAction` ->
  `resolve-no-obligation-tactical-shipping`;
- `ActFirstAction(operationStage: 1)` -> `act-first`;
- `ActLastAction(operationStage: 1)` -> `act-last`.

Candidate semantic preimages are explicit UTF-8 JSON containing `contractVersion`, `kind`, and
`operationStage` only when applicable. `actionId` is `sha256:` plus lowercase SHA-256 of that
preimage. Candidate ordering is kind then action ID using ordinal comparison.

`CampaignLegalActionSet` contract 2 contains policy ID,
campaign/audience-visible-state/ruleset/position/audience, and the immutable candidates. Its
explicit serializer writes those fields in that order and each
candidate as `contractVersion`, `actionId`, `kind`, then optional `operationStage`.

Task 006 extends that exhaustive writer/reader with the two dormant Movement values described in
the current-evolution section. This is a compatible concrete-type addition under candidate version
1, not a public-membership or envelope-version change.

## Query and fog boundary

`CampaignLegalActions.Query(handle, audience)` validates audience first, then unwraps and validates
authority.

System generation reads only admitted internal checkpoint/policy and produces one action at states
1-3, empty at states 4-5.

Side generation first projects Campaign Observation v1 for the exact side, then passes only that
observation to an internal observation-only generator. At state 4 the observed initiative holder
gets act-first and act-last; the opponent gets empty. All side sets are empty elsewhere.

The Task 006 dormant Movement deriver is a separate observation-only seam. It is tested as a pure
vector but is deliberately absent from the public Movement switch until Task 008.

Privacy tests build valid paired handles whose content/world differ only in opponent IDs, counts,
static facts, and locations. The holder's complete non-empty set and canonical bytes must be equal.

## Submission

`CampaignActionSubmission` contract 1 contains campaign ID, expected state version, expected
position ID, audience, and action ID.

Its strict reader added by Task 006 preserves that payload-free shape. Movement element/path/cost
semantics are bound by candidate ID and are not duplicated as caller-editable submission fields.

`CampaignLegalActions.Submit(handle, submission)` validates:

1. submission shape/version;
2. unwrapped admitted authority;
3. campaign ID;
4. expected state version;
5. expected position;
6. exact-audience current membership.

Stable rejection reasons are `InvalidSubmission`, `InvalidAuthority`, `CampaignMismatch`,
`StaleState`, `UnexpectedPosition`, and `ActionNotLegal`. Failure returns neither receipt nor
successor handle and emits nothing.

The closed mapping converts the matched candidate to the internal mechanic command, decides exactly
one event, projects it, and creates a successor handle with the same exact context.

`CampaignActionAcceptanceReceipt` contract 1 contains, in canonical order: contract version,
campaign ID, prior state version, committed state version, resulting position ID, audience, and
action ID. It contains no kind or event summary. The result returns receipt plus successor handle.
Strict receipt readback validates canonical bytes but cannot establish that a commit occurred;
only the authoritative submission path may construct an accepted result.

## Serializer and identity changes

Update internal event/snapshot serializers to strict version-4 creation/snapshot shapes. Setup gains
`openingPreamble`; snapshot gains `operationStageOrders` after `initiativeHolder` and before random
state. Event serializers gain the three new strict event shapes. Unknown, old, missing, extra, and
duplicate properties reject.

Refresh setup hashes, ruleset hash, creation/snapshot/observation goldens, and related tests.
Initiative event-2 golden bytes remain unchanged because policy sources are nested separately.
Content Pack hashes and Land sequence artifact hash remain unchanged.

## Implementation tasks

### `ACT-IMP-001` — Setup/ruleset cutover

Write failing setup-policy, source-separation, setup-hash, ruling, version, and old-cutover tests.
Implement setup 3 and manifest ruling. Refresh exact expected identities.

### `ACT-IMP-002` — Preamble authority and replay

Write failing state-order/event/projection/replay/forged-history tests. Implement snapshot 4,
stage-order value, commands/events, engine/projector/serializer changes, and state 1-5 validator.

### `ACT-IMP-003` — Opaque authority facade

Write failing reflection/project-reference/handle tests. Internalize authority primitives, add
creation/observation handle facades, remove DecisionWorker Core reference, and migrate existing tests
to test-only internal access.

### `ACT-IMP-004` — Action query and canonical contracts

Write failing candidate ID, action-set golden, audience/checkpoint, non-mutation, culture/order, type
graph, and non-empty privacy tests. Implement typed candidates, generator, query, and writers.

### `ACT-IMP-005` — Submission enforcement

Write failing precedence, stale/forged/cross-audience, receipt, accepted-event equivalence, and
successor-handle tests. Implement membership revalidation and closed command mapping.

### `ACT-IMP-006` — Reconcile and close

Update README, roadmap status, tech design, and naming overview as implementation requires. Run
focused tests, format, build, full solution tests, `git diff --check`, code-quality review, and a new
independent implementation review.

## Traceability

| Specification | Implementation | Evidence |
| --- | --- | --- |
| `ACT-006`-`011`, `ACT-NFR-005` | `ACT-IMP-001`, `002` | setup/ruling identity, phase events, state invariants, replay/golden tests |
| `ACT-001`-`005`, `ACT-012`, `013`, `ACT-019` | `ACT-IMP-003`, `004` | public surface, handle, query, serializer, fog/non-mutation tests |
| `ACT-014`-`018`, `ACT-020`, `021` | `ACT-IMP-003`, `005` | precedence, membership, receipt, API/reference, accepted-event tests |
| `ACT-NFR-001`-`004`, `006` | all tasks, `ACT-IMP-006` | package/reference inspection, focused and full repository gates |

Every `ACT-AC-001` through `ACT-AC-016` scenario is covered by at least one listed test family.
