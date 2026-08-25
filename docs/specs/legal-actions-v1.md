# Legal Actions v1 Specification

**Status:** Implemented; independent design and implementation reviews passed

**Date:** 2026-08-17

**Roadmap capability:** `ACTION-001`

**Rules target:** `cna-1979.1`

**Predecessors:** [Campaign World v1](campaign-world-v1.md),
[Campaign Observation v1](campaign-observation-v1.md)

**Research decision:**
[Turn-Preamble Action Boundary Spike](../research/turn-preamble-action-boundary-spike.md)

**Current contract evolution:** Reserve Designation retains the Legal Actions v1 capability but
clean-cuts the action-set envelope to contract 2 / `sandtable.legal-actions.v2`. Its `stateVersion`
is audience-visible: active sets retain the current authority revision, while inactive opposing
Reserve/Movement sets do not encode hidden designation count. Candidate, submission, and receipt
contracts remain 1. See [Reserve Designation v1](reserve-designation-v1.md).

## Objective

Create the smallest authoritative Core boundary that tells one exact audience which fully bound
actions are currently legal and accepts an outward action only after re-deriving that same
audience's current set.

Version 1 advances the two existing synthetic rules-laboratory campaigns through every mandatory
opening checkpoint from Initiative Determination to Operation Stage 1 Weather Determination. It
uses explicit no-obligation setup policy and mechanic-specific system events for Naval Convoy
Schedule and Tactical Shipping, then exposes the first non-empty side choice: the initiative holder
declares whether to act first or last in Operation Stage 1.

Side candidates are derived only from Campaign Observation v1. The complete candidate set and
canonical bytes must remain unchanged when valid opponent-only authority facts change. System
progression remains a distinct trusted audience and never appears as a player choice.

## User-visible demonstration

1. Create either admitted synthetic rules-laboratory campaign at Initiative Determination.
2. Query Axis and Commonwealth through the campaign's opaque authority handle and receive complete
   deterministic empty side sets; query `system`
   and receive exactly one `resolve-initiative` action.
3. Submit the system action, then resolve Naval Convoy Schedule and Tactical Shipping through two
   separate current system actions backed by the setup's explicit no-obligation policy.
4. Reach Operation Stage 1 Initiative Declaration and query both sides. The initiative holder
   receives exactly `act-first` and `act-last`; the other side receives an empty set.
5. Submit one holder action, internally record both stage actors without changing initiative
   holder, receive a side-safe acceptance receipt plus successor authority handle, and stop at
   Weather Determination.
6. Resubmit an old action or alter its audience, ID, campaign, state version, or position and
   receive a typed rejection with zero events.
7. Vary opponent-only content/world facts in paired valid Initiative Declaration fixtures and show
   that the holder's complete action-set semantics and canonical bytes remain unchanged.

## Assumptions and accepted boundary

- Version 1 is an internal `Cna.Core` domain and canonical-byte contract, not an HTTP, protobuf,
  Orleans, Maproom, persistence, or Intelligence adapter.
- `system`, `axis`, and `commonwealth` are action audiences, not authenticated principals. A future
  host derives the permitted audience from trusted orchestration or campaign membership and never
  trusts a browser-supplied side.
- System progression is distinct from a side choice. Only trusted orchestration may query or submit
  the system audience.
- The two current synthetic setup definitions, `rules-lab.initiative.predetermined` and
  `rules-lab.initiative.contested`, explicitly declare
  `no-opening-naval-convoy-obligations`. Missing convoy data never implies that policy.
- That policy is contract version 1 and carries the dedicated repository-synthetic source
  `sandtable-rules-lab:opening-preamble.no-naval-convoy-obligations.v1`; it does not reuse either
  setup's Initiative source.
- Existing top-level setup sources remain Initiative-specific and are the only setup sources passed
  to Initiative resolution. Policy provenance stays nested with the opening-preamble value.
- Each admitted no-obligation convoy phase is resolved by its own action, command, and event. No
  generic phase-completion command or event exists.
- The initiative holder chooses act-first or act-last independently for Operation Stage 1. Stage
  order is separate state and does not overwrite `initiativeHolder`.
- Action IDs are deterministic identities over complete typed side-safe candidate semantics. They
  are opaque, never parsed for rules meaning, and are neither authorization nor capability tokens.
- Submission re-derives the current exact-audience action set from admitted current authority. A
  previously returned action is not legal merely because its ID is well formed.
- Snapshot, exact content context, commands, events, serializers, projection, replay preparation,
  and replay execution are Core-internal. Only `Cna.Core.Tests` receives friend access; no production
  assembly does.
- Public creation returns a sealed non-record `CampaignAuthorityHandle` with no public authority
  getters, deconstruction, serialization, or value-revealing `ToString`. Query, observation, and
  submission facades accept that handle and unwrap it only inside Core. Accepted submission returns
  a side-safe receipt plus a successor handle, never a snapshot, event, context, artifact, random
  state, or complete authority payload.
- Cross-assembly activation replay is deferred until `HOST-001` defines authenticated Chronicle
  provenance and a dedicated activation-only capability that cannot be called by outward
  Host/DecisionWorker adapters as a live-decision path.
- The exact Content Pack context is resident inside the admitted authority handle. Query and
  submission perform no catalog resolution, persistence, remote I/O, clock access, model inference,
  or presentation lookup.

## Functional requirements

| ID | Requirement |
| --- | --- |
| `ACT-001` | Query accepts one non-null Core-issued `CampaignAuthorityHandle` and one defined `CampaignActionAudience`. The handle contains an admitted Campaign World snapshot plus its exact already-resolved content context but exposes neither. Query returns either one complete action set or one typed rejection; a null handle is a programmer error. |
| `ACT-002` | Every current action set records set contract version 2, policy ID `sandtable.legal-actions.v2`, campaign ID, audience-visible state version, canonical ruleset hash, current position ID, exact audience, and a canonically ordered immutable candidate collection. An active non-empty set retains the exact current authority revision; an inactive opposing Reserve/Movement set removes hidden designation increments. It contains no complete Content Pack identity. |
| `ACT-003` | Audience is a closed enum with exact canonical values `system`, `axis`, and `commonwealth`. It does not encode a user, seat, Staff persona, authorization claim, or inferred active side. |
| `ACT-004` | Each candidate is a dedicated immutable typed value with its own contract version, stable kind, and SHA-256 action ID computed from canonical candidate semantic bytes excluding the ID. Candidates contain only facts approved for their exact audience. Generic parameter dictionaries and raw `CampaignCommand` values are prohibited. |
| `ACT-005` | At valid unresolved Initiative Determination, `system` contains exactly one `resolve-initiative` candidate with no caller-supplied rules or random inputs. Both side sets are empty. |
| `ACT-006` | Setup schema 3 and its embedded setup snapshot carry opening-preamble policy contract 1. Version 1 admits `no-opening-naval-convoy-obligations` only for `rules-lab.initiative.predetermined` and `rules-lab.initiative.contested`; the policy carries source `sandtable-rules-lab:opening-preamble.no-naval-convoy-obligations.v1`. Absence, source mismatch, or use outside that scope is invalid authority. The complete policy participates in setup hash, creation event 4, snapshot 4, and replay validation. |
| `ACT-007` | At admitted Naval Convoy Schedule, `system` contains exactly one `resolve-no-obligation-naval-convoy-schedule` candidate. Its accepted mechanic-specific event verifies the setup policy and advances exactly once to Tactical Shipping. Side sets are empty. |
| `ACT-008` | At admitted Tactical Shipping, `system` contains exactly one `resolve-no-obligation-tactical-shipping` candidate. Its accepted mechanic-specific event verifies the setup policy and advances exactly once to Operation Stage 1 Initiative Declaration. Side sets are empty. |
| `ACT-009` | At Operation Stage 1 Initiative Declaration, only the initiative-holder audience contains candidates: exactly `act-first` and `act-last`, each carrying public `operationStage = 1`. The other side and `system` are empty. Candidate generation consumes a successful Campaign Observation v1 value for that exact side and reads no authoritative world/content/setup object. |
| `ACT-010` | Accepted Initiative Declaration emits one mechanic-specific event recording operation stage, declaring initiative holder, first-acting side, and second-acting side; preserves initiative holder; stores exactly one stage-order entry; and advances to Weather Determination. Any duplicate, mismatched, or non-holder declaration is rejected. |
| `ACT-011` | At Weather Determination every audience receives a successful empty set. Query does not roll weather, advance generically, or treat the unsupported mechanic as a legal action. |
| `ACT-012` | Query validates defined audience before complete snapshot/context authority, then validates authority before candidate generation. Failures return stable `InvalidAudience` or `InvalidState` reasons in that precedence, with no partial set. A valid state with no legal actions is success. |
| `ACT-013` | Equal admitted state, exact context, and audience produce structurally equal action sets, equal hashes, and byte-identical canonical JSON across runs, supported cultures, and equivalent input order. Query copies retained values, mutates nothing, and consumes no randomness. |
| `ACT-014` | Submission carries submission contract version 1, campaign ID, expected state version, expected position ID, audience, and action ID. It carries no caller-selected action kind, command, rules value, random outcome, stage actor, or arbitrary parameter. |
| `ACT-015` | Submission accepts a Core-issued handle and validates submission shape, admitted authority inside the handle, campaign binding, expected state version, expected position, and exact-audience current membership in deterministic precedence. Stale, forged, cross-campaign, wrong-position, wrong-audience, and no-longer-legal submissions return one stable typed rejection and zero internal events. |
| `ACT-016` | An exact current member maps through a closed Core mapping to one mechanic-specific command and is decided against the same admitted snapshot. Version 1 maps only the five candidate kinds named by `ACT-005`, `ACT-007`, `ACT-008`, and `ACT-009`; no generic completion mapping exists. |
| `ACT-017` | Submission never searches another audience after membership fails. An ID legal for another audience is indistinguishable from any other nonmember and returns `ActionNotLegal`. Core audience selection does not replace future user-to-side authorization. |
| `ACT-018` | Public creation, observation, action-query, and action-submission facades are the only cross-assembly campaign APIs. `CampaignAuthorityHandle` is a sealed non-record reference type with internal construction, no public authority-bearing members/deconstruction/serializer, and a constant nonrevealing `ToString`; it is not a DTO. Submission decides and projects internally and returns only a successor handle plus side-safe receipt. Snapshot, setup snapshot, world, exact context, command, event, event/snapshot serializer, projector, replay preparation, and replay harness types are internal; only `Cna.Core.Tests` has friend access. OrleansHost is the sole production project referencing Core and may store/pass the handle only to safe facades; it exposes no outward player/Intelligence adapter in this capability. DecisionWorker removes its unused Core project reference and cannot reference or receive the handle or complete authority types. Real assembly and public-surface tests enforce these constraints. `HOST-001` must separately review any provenance-bearing activation or write-only Chronicle seam. |
| `ACT-019` | Action-set and acceptance-receipt values/serializers contain no `CampaignAuthorityHandle`, snapshot, setup/world/element state, exact context, Content Pack artifact/definition/origin, `RuleReference`, random state, event, or command member. Side generator inputs additionally contain no authority types beyond Campaign Observation v1. Values are output-only and cannot be projected into state. |
| `ACT-020` | Querying and rejected submission perform no file, database, network, clock, identifier, model, service-container, mutable-catalog, presentation, or random I/O and emit no authoritative event. Accepted submission delegates only to pure mechanic-specific authority, applies the internal event, and exposes only a successor handle and side-safe receipt. |
| `ACT-021` | Acceptance receipt contract 1 contains only campaign ID, prior state version, committed state version, resulting position ID, submitted audience, and accepted action ID. Committed version equals prior plus one. It contains no candidate kind, event type/payload, holder/order, setup/content/random fact, or handle. Rejected submission returns no receipt and no successor handle. |

## Non-functional requirements

| ID | Requirement |
| --- | --- |
| `ACT-NFR-001` | No new runtime package, service, database, generated artifact, host dependency, reflection serializer, or transport contract is introduced. |
| `ACT-NFR-002` | Canonical JSON uses explicit `Utf8JsonWriter`, fixed property order, lower-kebab discriminants, canonical integers, lowercase SHA-256 values, ordinal candidate ordering, and no ambient serializer settings. |
| `ACT-NFR-003` | Canonical values defensively copy inputs, expose no mutable collection, validate versions/enums/stable IDs/hashes/duplicates, and implement structural equality and hash semantics. |
| `ACT-NFR-004` | Focused tests are small, deterministic, single-process, and use resident synthetic artifacts without file, network, clock, or model access. |
| `ACT-NFR-005` | The clean contract cutover, setup/snapshot/event canonical golden bytes, and internal replay evidence follow the exact version matrix below; the adopted empty-phase ruling participates in canonical ruleset identity. |
| `ACT-NFR-006` | New public APIs have nullable annotations and pass repository analyzers, formatting, build, and the complete Microsoft.Testing.Platform suite with zero warnings and no skipped tests. |

## Acceptance scenarios

| ID | Scenario | Expected evidence |
| --- | --- | --- |
| `ACT-AC-001` | Query every audience at unresolved Initiative Determination | System receives exactly one `resolve-initiative`; side sets are successful and empty; all envelopes carry exact concurrency coordinates. |
| `ACT-AC-002` | Submit each current system action through the opening preamble | Initiative, Naval Convoy Schedule, and Tactical Shipping each create exactly one internal expected event and successor handle in source order; internal replay is byte-identical. |
| `ACT-AC-003` | Remove, alter, re-source, or misuse the no-obligation setup policy | Campaign admission, checkpoint validation, or convoy resolution rejects deterministically with zero events; no missing field is treated as zero obligations. |
| `ACT-AC-004` | Query every audience at Operation Stage 1 Initiative Declaration | Holder gets exactly `act-first` and `act-last`; non-holder and system are empty; holder candidate bytes contain no opponent fact. |
| `ACT-AC-005` | Submit holder `act-first` and, in an independent fixture, `act-last` | Each returns a side-safe receipt/successor handle, internally emits one exact declaration event, records inverse first/second actors, preserves holder, and reaches Weather. |
| `ACT-AC-006` | Query every audience at Weather | Every query succeeds empty with exact post-declaration concurrency coordinates and consumes no randomness. |
| `ACT-AC-007` | Reuse any action after its checkpoint advances | Old version returns `StaleState`; current version with old position returns `UnexpectedPosition`; fully rebound old ID returns `ActionNotLegal`; all return zero events. |
| `ACT-AC-008` | Alter submission contract, campaign ID, action ID, audience, state version, or position ID | Deterministic typed rejection follows documented precedence, returns no events, and leaves authority/random cursor unchanged. |
| `ACT-AC-009` | Submit a system action as a side, a holder action as system, or a holder action as the other side | Exact-audience membership returns `ActionNotLegal` without revealing that another audience owns the ID. |
| `ACT-AC-010` | Compare paired valid Initiative Declaration states that differ only in opponent IDs, counts, static values, or locations | Holder's complete action-set semantics and canonical bytes are identical and non-empty; targeted sentinel values are absent. |
| `ACT-AC-011` | Reverse equivalent caller collection order and run under non-default cultures | Semantic equality, hash behavior, candidate order, action IDs, and canonical bytes match reviewed golden values. |
| `ACT-AC-012` | Inspect action/receipt/handle type graphs, generator dependencies, public members, `ToString`, and JSON | Action/receipt values contain no authority/setup/world/content/rule/random/command type; the side generator accepts only Campaign Observation v1; the handle exposes no value, deconstruction, or serialization surface. |
| `ACT-AC-013` | Attempt cross-assembly authority inspection, serialization, or direct mutation through context/artifact, snapshot/event, commands, projection, or replay | Public API and real assembly tests show OrleansHost can only store/pass an opaque handle to safe facades and has no outward adapter; DecisionWorker has no Core reference; neither project has friend access; and post-creation mutation is action-only. |
| `ACT-AC-014` | Capture authority/content/random bytes before and after repeated queries and rejected submissions | Inputs and cursor are unchanged, no event exists, and repeated output bytes match. |
| `ACT-AC-015` | Pass undefined audience or forged/mismatched authority checkpoint | Query returns `InvalidAudience` before `InvalidState`, never returns a partial set, and never leaks candidate membership. |
| `ACT-AC-016` | Change the empty-phase ruling, setup policy/source, sequence source, or stage-order semantics | Setup/ruleset/campaign golden identities change as specified and stale history is rejected. Exact v3-to-v4 cutover tests reject prior setup/creation/snapshot history while preserving event-2 Initiative semantics after v4 creation. |

## Contract cutover and checkpoint invariants

This capability is an explicit clean cutover because no persisted user campaigns exist:

| Contract | Current | `ACTION-001` | Compatibility decision |
| --- | --- | --- | --- |
| Campaign setup definition/snapshot schema | 2 | 3 | New executable rejects schema 2; prior Git revision is its historical executable. |
| Opening-preamble policy | absent | 1 | New closed value; no prior reader. |
| `CreateCampaign` command | 3 | 4 | Version 3 is rejected rather than silently upgraded. |
| `CampaignCreated` event | 3 | 4 | Event reader accepts version 4 only; version 3 requires the prior executable. |
| `CampaignSnapshot` | 3 | 4 | Snapshot reader/validator accepts version 4 only; version 3 requires the prior executable. |
| Operation-stage order value | absent | 1 | New immutable value; no prior reader. |
| New convoy/declaration commands and events | absent | 1 | New mechanic-specific types; no prior reader. |
| `InitiativeDetermined` event | 2 | 2 | Shape and canonical semantics remain unchanged and are accepted only after valid version-4 creation. |
| Campaign World snapshot | 1 | 1 | Unchanged. |
| Land sequence position/catalog | 2 | 2 | Shape and sequence artifact remain unchanged. |
| Campaign Observation | 1 | 1 | Shape unchanged; golden bytes update because the canonical ruleset hash changes. |
| Ruleset manifest | 2 | 2 | Existing schema already carries rulings; adding the stable ruling changes hash, not schema. |

Setup hashes change because schema 3 adds the complete policy and its source. The canonical
`cna-1979.1` ruleset hash changes because the manifest gains ruling
`cna-1979.1.ruling.explicit-empty-opening-convoy-resolution`. Content Pack hashes, Campaign World
contract, random algorithm/state contract, Land sequence artifact hash, and Initiative event-2
golden bytes remain unchanged. Old ruleset hashes are unsupported by the new executable.

The ruling has exact conflict ID `cna-1979.1.conflict.empty-opening-convoy-phase`, alternatives
`reject-empty-opening-convoy-as-unsupported` and
`resolve-explicitly-admitted-empty-opening-convoy`, and selects the second. Its sources are Land
Rules 5.2, 32.43, and 32.61 plus the repository-synthetic policy source. Its protecting tests are
`ACT-AC-002`, `ACT-AC-003`, and `ACT-AC-016`.

`CampaignSnapshot` version 4 contains a canonically ordered immutable collection of
contract-version-1 operation-stage orders. Each entry has an Operation Stage in 1 through 3 and two
defined, distinct, complementary sides. Operation Stage is unique. This slice admits only these
checkpoints:

| State version | Position | Initiative holder | Stage orders | Random cursor |
| --- | --- | --- | --- | --- |
| 1 | Initiative Determination | absent | empty | initial |
| 2 | Naval Convoy Schedule | present | empty | post-Initiative |
| 3 | Tactical Shipping | present | empty | unchanged from state 2 |
| 4 | Operation Stage 1 Initiative Declaration | present | empty | unchanged from state 2 |
| 5 | Operation Stage 1 Weather Determination | present | exactly Stage 1 | unchanged from state 2 |

At state 5, the stage-order entry's first and second sides are the declaring holder and its opponent
for `act-first`, or the opponent and declaring holder for `act-last`. Projection rejects an early,
missing, duplicate, future-stage, same-side, or holder-inconsistent entry. Snapshot validation
recomputes every deterministic checkpoint invariant and permits only the two valid declaration
orders. Internal projection verifies the exact preceding event history; internal snapshot
construction and the handle boundary prevent a cross-assembly caller from manufacturing a locally
shape-valid checkpoint.

## Canonical action semantics

The action-set envelope has fixed semantic groups:

1. set contract and policy identity;
2. campaign, state, and canonical ruleset identity;
3. current position and exact audience; and
4. canonically ordered typed candidate values.

The first three system candidate records contain only action contract version, action ID, and kind.
`ActFirstAction` and `ActLastAction` additionally contain `operationStage`. Each action ID is SHA-256
over that candidate's exact canonical semantic bytes excluding `actionId`. Later mechanics add new
concrete records and writer branches; they do not add generic dictionaries or require consumers to
parse action IDs.

The accepted submission receipt has its own contract-1 canonical writer and only the six scalars in
`ACT-021`. It confirms an authoritative commit without reproducing or summarizing the internal event.
The successor `CampaignAuthorityHandle` is never serialized and is not part of receipt equality or
canonical bytes.

Exact JSON property order, hash preimage bytes, validation precedence, and golden vectors belong in
the technical design after this corrected specification passes independent review.

## Tech stack and commands

Implementation remains C# on .NET 10 in `Cna.Core`, with xUnit v3 tests through native
Microsoft.Testing.Platform. No dependency change is planned.

```bash
dotnet restore Sandtable.slnx
dotnet format Sandtable.slnx --verify-no-changes --no-restore
dotnet build Sandtable.slnx --no-restore
dotnet test --solution Sandtable.slnx --no-build
git diff --check
```

`just check` may be used as the equivalent full local gate.

## Repository structure

```text
src/Cna.Core/
  Actions/                 audiences, typed candidates, query/submission results,
                           side-safe generator, enforcer, canonical writer, identity factory
  Campaigns/               internal authority values/mechanics/replay; public opaque handle and
                           creation/submission facades
  Observations/            internal raw projection plus public handle-based side-safe facade
  Rules/                   adopted ruling and stage-order sequence semantics
  Setups/                  explicit opening-preamble policy

tests/Cna.Core.Tests/
  Actions/                 contract, privacy, canonical-byte, submission, API-boundary tests
  Campaigns/               setup/event/snapshot/projection/replay and stage-order tests

docs/research/             source and decision evidence
docs/specs/                governing behavioral requirements
docs/design/               approved implementation shape and traceability
docs/roadmap/              capability wording and completion status
```

## Code style

Use sealed immutable records/classes, explicit factories, stable enums, ordinal comparison, typed
total results, and explicit canonical writers. Do not introduce generic action dictionaries,
reflection serialization, mutable DTOs, a reusable workflow engine, service-container access, or
client-side legality rules.

The side generator has an observation-only seam, conceptually:

```csharp
internal static CampaignLegalActionSet GenerateForSide(
    CampaignObservation observation,
    CampaignActionAudience audience);
```

The handle-based query unwraps authority inside Core, projects the exact side observation, and
passes only the successful derived value to this seam. System generation is separate and may inspect
the internally admitted authority.

## Testing strategy

- Follow red-green-refactor at each setup, authority, action-contract, generator, serializer, and
  submission checkpoint.
- Derive tests from acceptance scenarios rather than mirroring implementation branches.
- Prove phase order and replay from Initiative Determination through Weather; never use generic
  completion in fixtures.
- Build paired valid non-empty privacy fixtures that preserve public observation facts and mutate
  opponent IDs, counts, static values, and locations. Compare the complete holder action set and
  canonical bytes without exclusions or normalization.
- Freeze reviewed golden bytes for setup, events, snapshot, action semantic preimages, complete
  action sets, and affected ruleset identity.
- Assert every rejected submission returns zero events and leaves snapshot, content, and random
  state unchanged.
- Compare accepted submission with the internal mechanic command/event/projector/replay path using
  real Core implementations rather than mocks.
- Add public-API and real OrleansHost/DecisionWorker compile/reference tests now. Assert OrleansHost
  can only hold/pass the opaque handle to safe facades, contains no outward adapter, and cannot
  inspect/serialize snapshot, world, setup, context, event, or random authority. Assert
  DecisionWorker has no Core project/assembly reference. Neither production assembly has friend
  access; raw command, event, projection, and replay primitives are absent from the public surface.
- Preserve all Content, Campaign World, Initiative, Observation, replay, and authority tests.
- Run focused tests during implementation and the complete repository gate at closure.

## Boundaries

Always:

- Unwrap only a Core-issued authority handle, then validate exact audience, admitted
  snapshot/content context, concurrency binding, and current membership before translating an
  outward submission.
- Derive side candidates only from Campaign Observation v1 and prove non-empty non-interference.
- Treat action IDs as deterministic identity only; re-derive membership every time.
- Keep system and side audiences distinct in contracts, authorization responsibility, and tests.
- Record both mandatory no-obligation convoy phase resolutions separately.
- Keep initiative holder and per-stage first/second actors as separate authoritative facts.
- Keep snapshot/context/event authority, serialization/deserialization, projection, and replay
  Core-internal until the separate authenticated activation capability is specified and reviewed.
- Keep query and rejected submission pure and deterministic.
- Update this specification first if implementation reveals a semantic change.

Ask first:

- Add another setup preamble policy, scenario, action audience, action kind, caller-supplied
  parameter, opponent contact, remembered knowledge, uncertainty, or hidden choice.
- Add transport, authorization, persistence, caching, signing, idempotency storage, host scheduling,
  Maproom, or Intelligence integration.
- Implement general Naval Convoy, Weather, Operation Stage 2/3 declaration, movement, or combat.
- Expose cross-assembly replay or introduce Chronicle provenance/activation APIs before `HOST-001`.

Never:

- Infer no convoy obligations from absent content/state fields or skip a mandatory checkpoint.
- Assign convoy activity to a side without modeled eligible obligations and source-backed inputs.
- Reuse or serialize raw authority, complete content, rules sources, setup facts, random state,
  observation, or commands as action values.
- Let side candidate generation inspect authoritative world/content/setup objects.
- Give a production assembly Core friend access; expose authority getters, deconstruction,
  serialization, construction/mutation, raw commands, event deserialization, projection, or replay;
  add an outward adapter to OrleansHost in this capability; or restore a Core reference from
  DecisionWorker.
- Treat a hash-shaped action ID as authorization or skip membership revalidation.
- Search another audience to improve a rejection message.
- Consume randomness, mutate authority, emit an event, or advance an unsupported mechanic during
  query or rejected submission.
- Resolve mutable latest/default content or perform remote/model I/O inside Core.

## Traceability

| Requirement group | Governing requirement/decision | Planned evidence |
| --- | --- | --- |
| `ACT-001`-`ACT-004`, `ACT-012`, `ACT-013` | `FOW-001`, `DET-001`, `DET-002`; typed exact-audience boundary | `ACT-AC-001`, `ACT-AC-004`, `ACT-AC-010`-`012`, `015` |
| `ACT-005`-`ACT-011` | Land Rules 5.2, 7.11, 7.14, 7.16; turn-preamble research ruling | `ACT-AC-001`-`006`, `ACT-AC-016` |
| `ACT-014`-`ACT-018`, `ACT-021` | roadmap Task 2.5 outward action membership and stale enforcement | `ACT-AC-005`, `ACT-AC-007`-`009`, `ACT-AC-012`, `ACT-AC-013` |
| `ACT-019`, `ACT-020` | Umpire authority and Campaign Observation fog boundary | `ACT-AC-010`, `ACT-AC-012`-`014` |
| `ACT-NFR-001`-`006` | repository architecture, canonical identity, replay, and quality rules | golden tests, dependency/reference inspection, focused tests, full gate |

## Explicit deferrals

- General Naval Convoy Schedule and Tactical Shipping choices or resolution;
- logistics, ports, cargo, production, replacement, routes, lanes, capacity, arrival timing, convoy
  bombing, and optional historical rerouting;
- Weather resolution and its random/table contract;
- Initiative Declaration for Operation Stages 2 and 3;
- movement, contact, combat, apparent contacts, dummy identity, reconnaissance, and memory;
- HTTP/protobuf/Orleans/Maproom/Intelligence adapters and user-to-side authorization;
- persistence, request idempotency, retries, host scheduling, and presentation;
- action-set caching, signing, encryption, secrecy, multi-action plans, drafts, undo, and batching.

## Success and exit criteria

The specification is implementation-ready when a fresh independent review finds no blocking
authority, fog-of-war, source-fidelity, determinism, contract, or traceability gap and the technical
design maps every requirement to ordered tasks and executable evidence.

Implementation is complete only when `ACT-AC-001` through `ACT-AC-016` pass, the full repository
gate passes, governing docs and roadmap are synchronized, and an independent implementation review
finds no blocking defect.
