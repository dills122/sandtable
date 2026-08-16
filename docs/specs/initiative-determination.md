# Initiative Determination Specification

**Status:** Implemented and independently reviewed

**Date:** 2026-08-15

**Rules target:** `cna-1979.1`

**Research:** [Initiative Determination spike](../research/initiative-determination-spike.md)

## Objective

Extend the replayable Umpire spine through the first mandatory mechanic. A developer can create a
campaign from a recognized setup, submit the currently available system command, inspect a
source-explainable initiative outcome, replay it byte-for-byte, and observe the campaign stopped at
Naval Convoy.

This is a rules-engine vertical slice, not a playable scenario or UI slice.

## User-visible demonstration

1. Create a campaign using the canonical `cna-1979.1` ruleset, a seed, and a recognized setup.
2. Observe that the campaign is at Initiative Determination and can resolve initiative once.
3. Submit `ResolveInitiative` with the current state version and expected position.
4. Inspect the accepted event:
   - resolution mode and source references;
   - derived ratings and their authoritative input facts when contested;
   - every opposed roll round, including ties; and
   - final initiative holder.
5. Observe the campaign at Naval Convoy with no inferred Operation Stage actor order.
6. Recreate the same event and canonical snapshot from the same setup, seed, and commands.
7. Attempt to advance Naval Convoy and receive a typed unsupported-transition rejection with no
   event.

For a scenario-predetermined setup, step 4 contains no roll rounds and the random cursor does not
move.

## Functional requirements

| ID | Requirement |
| --- | --- |
| `INIT-001` | Initiative resolves only while the authoritative sequence position is Initiative Determination and the command's expected state version and position match. |
| `INIT-002` | A scenario-predetermined first-turn holder resolves without a random draw; a contested turn rolls one d6 per side, adds source-derived ratings, and rerolls the complete opposed contest on a tie. |
| `INIT-003` | Commonwealth ratings are derived from the three published Game Turn bands. |
| `INIT-004` | Axis ratings are derived by classifying typed Rommel/German-land-combat-unit location facts; Tripoli/Tunisia Holding Boxes are represented distinctly and never qualify as a Game Map. |
| `INIT-005` | Commands cannot supply ratings, dice, random cursor changes, modified totals, or the holder. |
| `INIT-006` | An accepted resolution emits exactly one versioned `InitiativeDetermined` event; rejected commands emit no event and do not change state. |
| `INIT-007` | The event records enough normalized facts to explain the outcome and validate every roll, rating, total, tie, cursor boundary, and holder without copied source prose. |
| `INIT-008` | Projection stores the holder, advances exactly once to Naval Convoy, and rejects duplicate resolution or generic completion. |
| `INIT-009` | Replaying accepted history produces byte-equivalent canonical state, and deciding the same command against identical state produces byte-equivalent canonical event data. |
| `INIT-010` | The initiative holder is distinct from the first-acting side. No stage actor is assigned until that stage's later Initiative Declaration. |
| `INIT-011` | The normalized rating table, sequence actor semantics, algorithm identifier, and adopted rulings participate in canonical ruleset identity. |
| `INIT-012` | Campaign creation selects a validated setup identifier; free-form initiative situation data is not accepted from the command boundary. |
| `INIT-013` | Seed and unconsumed random-stream state remain internal authoritative data and are excluded from side observations and player-facing legal-action payloads. |
| `INIT-014` | The engine remains pure: resolution performs no clock, identifier, file, network, model, host, or persistence I/O. |
| `INIT-015` | Legacy `CampaignSequenceAdvanced` history cannot cross Initiative Determination; obsolete contract-v1 generic advancement is rejected rather than interpreted as initiative resolution. |
| `INIT-016` | Setup hash covers schema, identifier, synthetic marker, initial turn, complete initiative policy, and setup sources; creation and initiative events retain the applicable setup provenance. |
| `INIT-017` | `CampaignCreated` embeds the immutable authoritative setup snapshot and initial random state needed for replay; setup display text is excluded from setup hash, authoritative events, and canonical snapshots. |

## Validation and rejection behavior

Validation order is observable and stable. The dispatcher first rejects a supplied noncanonical
snapshot as `InvalidState`, then dispatches by command type.

For `CreateCampaign`:

1. a valid existing snapshot returns `CampaignAlreadyCreated`, even when create fields are bad;
2. an unknown contract, nonzero expected version, invalid campaign/ruleset/setup/hash/seed field,
   or unknown setup returns `InvalidCommand`; and
3. otherwise creation succeeds.

For `ResolveInitiative` and generic sequence commands:

1. a null snapshot returns `CampaignNotCreated`, even when command fields are bad;
2. an unknown contract, missing expected position, or invalid field returns `InvalidCommand`;
3. a mismatched state version returns `StaleState`;
4. a mismatched position identifier returns `UnexpectedSequenceStep`;
5. a recognized command at the wrong phase returns `UnsupportedTransition`;
6. an internally incomplete setup/random/sequence state returns `InvalidState`; and
7. otherwise the command resolves.

Validation must not consume random bytes. Every rejection leaves the snapshot and event history
unchanged.

## State invariants

- State version starts at 1 after campaign creation and increments by exactly 1 for the initiative
  event.
- Ruleset hash is the current canonical `cna-1979.1` hash.
- Random algorithm identifier is recognized and immutable within a campaign.
- Random cursor is an unsigned next-byte offset and never decreases.
- Predetermined resolution consumes zero bytes and contains zero roll rounds.
- Contested resolution contains one or more complete rounds; all but the last are ties and the last
  is not a tie.
- Modified totals equal die plus derived rating.
- Initiative holder matches the last round's higher modified total.
- Naval Convoy has no active side merely because initiative has been determined.
- No campaign position with a concrete first/second actor is valid without the corresponding
  stage-order declaration.

## Non-functional requirements

| ID | Requirement |
| --- | --- |
| `INIT-NFR-001` | Results are identical across supported platforms and .NET major versions for the same versioned random-stream algorithm. |
| `INIT-NFR-002` | Rules resolution and event validation are deterministic, synchronous, and allocation-conscious; no service container is required. |
| `INIT-NFR-003` | Commands, events, snapshots, setup data, table data, and random algorithm all have explicit contract or schema versions. |
| `INIT-NFR-004` | Canonical serialization uses fixed property ordering, invariant casing, and culture-independent numeric encoding. |
| `INIT-NFR-005` | Source locators identify original material without embedding copyrighted rules prose or component art. |
| `INIT-NFR-006` | New public APIs use nullable annotations and pass repository analyzer, formatting, build, and test gates with zero warnings. |
| `INIT-NFR-007` | Every collection-bearing setup, outcome, and event value object defensively copies its input and implements structural equality/hash semantics; replay never relies on `IReadOnlyList` reference equality. |

## Acceptance scenarios

| ID | Scenario | Expected evidence |
| --- | --- | --- |
| `INIT-AC-001` | Recognized predetermined setup | Holder is recorded, no rounds exist, cursor is unchanged, state advances to Naval Convoy, and sources include Rule 7.15 plus the synthetic setup locator. |
| `INIT-AC-002` | Recognized contested setup with no tie | One Axis/Commonwealth roll pair is recorded; ratings, totals, holder, cursor, rule/table sources, and synthetic situation source are correct. |
| `INIT-AC-003` | Seed yielding one or more ties | Complete tie rounds are retained and rerolled until exactly one non-tie round. |
| `INIT-AC-004` | Commonwealth Game Turns 1, 42, 43, 90, 91, and 111 | Boundary ratings are 3, 3, 4, 4, 5, and 5; turns outside the supported table fail validation. |
| `INIT-AC-005` | Each Axis presence combination and Holding Box-only presence | Ratings follow 6/3/1 and Holding Box-only pieces do not qualify. |
| `INIT-AC-006` | Caller attempts stale, wrong-position, duplicate, generic, or malformed resolution | Typed rejection, zero events, identical state, and unchanged cursor. |
| `INIT-AC-007` | Same setup, seed, state, and command executed twice | Canonical event bytes and projected snapshot bytes match. |
| `INIT-AC-008` | Different seeds against the same contested setup | At least one selected golden seed pair differs only in declared random outcomes and downstream holder/cursor facts. |
| `INIT-AC-009` | Replay creation plus initiative event | Projector validates the exact event from prior state and reaches byte-identical canonical state. |
| `INIT-AC-010` | Forge a roll, rating, total, cursor, holder, source, or next position in history | Replay rejects the event as invalid history. |
| `INIT-AC-011` | Inspect state after resolution | Holder exists; Operation Stage first actor does not; Naval Convoy is the position. |
| `INIT-AC-012` | Change a chart row, actor role, source reference, ruling, or random algorithm identifier | Canonical ruleset hash changes. |
| `INIT-AC-013` | Create with an unknown setup or arbitrary situation fields | Creation rejects and emits no event. |
| `INIT-AC-014` | Replay creation followed by a legacy `CampaignSequenceAdvanced` from Initiative Determination to Naval Convoy | Replay rejects the obsolete history instead of advancing. |
| `INIT-AC-015` | Mutate a caller-owned source/round list after contract construction, or compare separately allocated equal lists | The value object remains unchanged and semantic equality/hash behavior is stable. |
| `INIT-AC-016` | Change a synthetic setup policy fact or setup source | Setup hash changes while the unchanged ruleset hash does not. |
| `INIT-AC-017` | Change only a setup display name | Setup hash and canonical event/snapshot bytes remain unchanged. |

## Boundaries and non-goals

In scope:

- normalized Initiative Ratings data and provenance;
- corrected initiative-holder/stage-order semantics;
- two stable synthetic rules-laboratory setups covering predetermined and contested integration;
- predetermined and contested resolution paths;
- versioned deterministic d6 stream;
- command, event, snapshot, canonical serialization, projection, and replay validation;
- focused unit, contract, golden-vector, negative-history, and integration tests.

Out of scope:

- published scenario transcription or claims that the fixture is historical;
- Naval Convoy mechanics or advancement beyond its first position;
- Initiative Declaration and the holder's per-stage first/last choices;
- Weather Determination, movement, combat, map topology, unit movement, or general content schemas;
- a general dice-expression language or random service shared outside `Cna.Core`;
- Orleans activation, Chronicle persistence/authentication, Maproom UI, transport contracts, or the
  Intelligence plane.

## Traceability

| Requirement group | Governing roadmap requirements | Implemented evidence |
| --- | --- | --- |
| `INIT-001`-`INIT-008`, `INIT-015` | `FID-001`, `FID-002`, `EVT-001` | resolver, command, rejection, projection, legacy-bypass, and forged-history tests |
| `INIT-009`, `INIT-NFR-001`, `INIT-NFR-004` | `DET-001`, `DET-002`, `REL-001` | golden random vectors, repeated decision, canonical serialization, replay tests |
| `INIT-003`, `INIT-004`, `INIT-007`, `INIT-011` | `SRC-001` | table boundary, provenance, manifest-hash mutation tests |
| `INIT-010` | `UX-001`, `FID-001` | sequence contract and no-inferred-actor tests |
| `INIT-012` | `FID-002`, `EVT-001` | known/unknown setup creation tests |
| `INIT-016` | `SRC-001`, `EVT-001` | setup hash mutation and creation/initiative provenance tests |
| `INIT-017` | `DET-001`, `REL-001` | catalog-independent creation replay and presentation-metadata exclusion tests |
| `INIT-013` | `FOW-001` | negative observation/DTO serialization test when that boundary is introduced; internal-state test now |
| `INIT-014`, `INIT-NFR-002` | `DET-001` | pure unit tests with no host or external resources |
| `INIT-NFR-003`, `INIT-NFR-006`, `INIT-NFR-007` | repository contract and quality rules | serialization compatibility, collection mutation/equality tests, and `just check` |
| `INIT-NFR-005` | `IPR-001` | repository asset/status inspection |

`INIT-013` cannot be fully proven through a player observation API because that API is scheduled for
Sprint 2. This slice must keep the fields internal and add the negative contract test when the
observation boundary is introduced; it must not invent that boundary solely for initiative.

## Success and exit criteria

The specification is complete when all acceptance scenarios except the explicitly deferred
observation-boundary portion of `INIT-013` pass, the full repository gate passes, repository design
documents describe the new authority boundary, and an independent reviewer finds no blocking
correctness or plan gap.

The project owner approved this specification before implementation. The completed implementation
passed its repository gate, reconciled every independent-review finding, and received a focused
`Ready` readback on the corrections.
