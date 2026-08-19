# Weather Determination v1 Specification

**Status:** Proposed; owner approval required before implementation

**Date:** 2026-08-18

**Rules target:** `cna-1979.1`

**Capability:** `WEATHER-001`

**Research decision:**
[Operation-Stage Preamble Source and Contract Spike](../research/operation-stage-preamble-spike.md)

## Objective

Resolve Weather as the next authoritative mechanic after Legal Actions v1. A trusted system action
must use the retained initiative holder, current Game Turn and Operation Stage, versioned random
stream, corrected source-normalized tables, admitted synthetic content, and explicit setup policy
to emit one replayable Weather event and stop at Organization.

Success means both current synthetic scenarios can reach a source-correct Organization decision
barrier without caller-supplied dice or outcomes, hidden-state disclosure, generic sequence
advancement, or an inference that missing resource/air state means zero immediate effects.

## Approved assumptions required to implement

Implementation may begin only after the project owner approves these proposed decisions:

1. `TURN-DEC-001`: `cna-1979.1.ruling.weather-season-boundary` selects the explicit 29.1 season boundaries and
   remaps chart Game-Turn ranges while preserving each season's printed outcome probabilities.
2. `TURN-DEC-003`: Weather v1 supports only setup-hashed synthetic fixtures that explicitly declare no stored
   fuel/water, depleted wells, or grounded aircraft at the Weather checkpoint.
3. `TURN-DEC-004`: The Land sequence is corrected to represent Organization as one decision barrier because its
   subsegments may occur in player-chosen order.
4. `TURN-DEC-002`: Weather v1 stops at Organization. Stage-entry obligations, Reserve, and Movement are separate
   capabilities.
5. `TURN-DEC-005`: mandatory Weather resolution is exposed to the trusted `system` audience while
   the retained initiative holder remains the determining side recorded by authority.
6. `TURN-DEC-006`: the printed Weather chart's omission of Game Turn 111 is not silently repaired. Weather v1
   supports Game Turns 1-110 and rejects 111 before randomness until a later full-campaign ruling
   selects source-backed behavior.

## Technology and commands

- Runtime: .NET 10, C# with warnings as errors.
- Test platform: Microsoft.Testing.Platform through xUnit.
- Serialization: explicit canonical UTF-8 JSON owned by `Cna.Core`.
- Randomness: existing repository-owned `sandtable.sha256-counter.v1` byte stream.
- Dependencies: no new runtime or test package.

Commands:

```sh
dotnet restore Sandtable.slnx
dotnet build Sandtable.slnx --no-restore
dotnet test --solution Sandtable.slnx --no-build
dotnet format Sandtable.slnx --verify-no-changes --no-restore
just check
```

## Project structure

```text
src/Cna.Core/Rules/          normalized Weather tables, areas, source references, ruling
src/Cna.Core/Randomness/     named Weather draw procedure over the existing stream
src/Cna.Core/Content/        source-neutral location-to-weather-area assignments
src/Cna.Core/Setups/         explicit synthetic Weather admission policy
src/Cna.Core/Campaigns/      command, event, state, decision, projection, serialization, replay
src/Cna.Core/Actions/        system candidate and submission translation
src/Cna.Core/Observations/   source-free public resolved-Weather projection
tests/Cna.Core.Tests/        focused rules, content, campaign, action, observation, replay tests
docs/                        retained research, specification, design, roadmap, and rationale
```

## Code style

Use immutable typed values, deterministic validation order, ordinal identity, and pure factories.
Callers supply concurrency coordinates only; the Umpire derives rules inputs and outcomes.

```csharp
internal sealed record ResolveWeather(
    int ContractVersion,
    long ExpectedStateVersion,
    string ExpectedPositionId) : CampaignCommand;

internal static WeatherResolution Resolve(
    CampaignSnapshot snapshot,
    WeatherRulesArtifact rules);
```

Do not represent Weather as free-form strings, a dice expression, a generic phase-completion event,
or caller-selected table coordinates.

## Contract vocabulary

| Value | Contract | Meaning |
| --- | --- | --- |
| `WeatherSeason` | 1 | `fall`, `winter`, `spring`, or `summer` |
| `WeatherKind` | 1 | `normal`, `hot`, `sandstorm`, or `rainstorm` |
| `WeatherScope` | 1 | `none`, `global`, or `listed-areas` spatial meaning |
| `WeatherArea` | 1 | Typed rules areas A-E used by the source lookup |
| `WeatherTableDefinition` | 1 | Corrected season ranges and d66 outcome ranges with sources |
| `FoulWeatherLocationDefinition` | 1 | Location d6 to affected-area set with sources |
| `ContentWeatherAreaAssignment` | 1 | Synthetic content location to one typed Weather area |
| `CampaignWeatherPolicy` | 1 | Explicit supported immediate-effect-subject policy |
| `CampaignOperationStageWeather` | 1 | One resolved public Weather record for one `(GameTurn, OperationStage)` identity |
| `ResolveWeather` | 1 | Trusted mechanic command carrying concurrency only |
| `WeatherDetermined` | 1 | Recomputable authoritative result and transition event |
| `ResolveWeatherAction` | 1 | System candidate kind `resolve-weather`; derived SHA-256 action ID below |

## Requirements

| ID | Requirement |
| --- | --- |
| `WTH-001` | Add a versioned Weather rules artifact containing the corrected Game-Turn-to-season mapping, each season's d66 outcome ranges, the foul-location mapping, stable source references, and canonical content hash. |
| `WTH-002` | Add proposed ruling `cna-1979.1.ruling.weather-season-boundary` to the ruleset manifest with the design's exact conflict ID, ordinally sorted alternative IDs, selected behavior, protecting IDs `WTH-AC-001`, `WTH-AC-002`, `WTH-AC-004`, and exact ordered source array. Any array difference changes the manifest hash and fails golden validation. |
| `WTH-003` | Game Turns map to seasons exactly as follows: Fall `1-12`, `49-60`, `97-108`; Winter `13-24`, `61-72`, `109-110`; Spring `25-36`, `73-84`; Summer `37-48`, `85-96`. Per `TURN-DEC-006`, Game Turn 111 remains explicitly unsupported because the published chart omits it; every unsupported turn rejects before randomness. |
| `WTH-004` | The first Weather d6 is the tens digit and the second is the ones digit. Lookup uses the ordered d66 value, never the sum. The normalized ranges are exhaustive and non-overlapping for valid d66 values `11-66`. |
| `WTH-005` | Normal and hot invoke exactly two accepted d6 operations. Sandstorm and rainstorm invoke exactly one additional accepted location-d6 operation. Rejection sampling may consume extra underlying bytes: `NextByteCursor` advances across every consumed byte, including values 252-255 rejected by `RollD6`, while the event records only accepted dice and the exact resulting byte cursor. Normal resolves scope `none`; hot resolves scope `global`; foul Weather resolves scope `listed-areas`. No rejected or future byte is exposed publicly. |
| `WTH-006` | Location rolls map `1 -> A,B`, `2 -> C,D`, `3 -> D,E`, `4 -> B,C`, `5 -> B,D`, and `6 -> B,C,D`. Weather v1 preserves E in roll 3 and carries stable deferred rule reference `spi-1979-land-rules:29.41` for the Nile Delta sandstorm exception. It does not execute that exception because current content has no published Delta subarea identity and v1 applies no positive area effects; a later effect-enforcement capability must normalize and test the subarea contract before use. |
| `WTH-007` | Generalize the normalized random-procedure artifact to identify Initiative and Weather draw procedures without changing `sandtable.sha256-counter.v1` byte generation or accepted-d6 normalization. |
| `WTH-008` | Content schema adds exactly one typed Weather-area assignment for every location when the `land.weather-areas` capability is declared. Missing, duplicate, unknown-location, undefined-area, or extra assignments fail canonical validation. |
| `WTH-009` | The synthetic pack uses the design's exact ordinal fixture mapping: `center -> A`, `east -> B`, `north -> C`, `north-east -> D`, `north-west -> E`, `south -> A`, `south-east -> B`, `south-west -> C`, and `west -> D`. Each row uses content-origin kind `synthetic` and one location-specific `sandtable-rules-lab` reference; the source index retains content-source kind `repository-synthetic`. These assignments are test fixture data, not claims that the synthetic theater is a published map. |
| `WTH-010` | Setup schema adds Weather policy contract 1. Version 1 admits only `no-immediate-weather-effect-subjects` for the two current synthetic setups and carries source `sandtable-rules-lab:weather.no-immediate-effect-subjects.v1`. |
| `WTH-011` | The policy means the admitted checkpoint has no stored fuel/water subject to 29.34, no depleted wells subject to 29.53, and no grounded aircraft subject to 29.47/38.5. Absence or mismatch is invalid authority; the policy does not generalize to published content. |
| `WTH-012` | Campaign creation, setup hash, creation event, snapshot, canonical serialization, projection, and replay all carry and revalidate the exact Weather policy and evolved content identity under the design's exact migration matrix. Unchanged outer command/event shapes retain their versions while canonical bytes change with nested hashes; `CampaignSnapshot` advances from 4 to 5 only when its Weather collection is added. |
| `WTH-013` | Campaign state stores at most one `CampaignOperationStageWeather` per reached `(GameTurn, OperationStage)` identity and orders the historical collection by that pair. The record contains contract, Game Turn, stage, determining initiative holder, season, both Weather dice, kind, scope, optional location die, canonical affected-area set, and source-free immediate-effect counts, which are zero in v1. Scope/area invariants are exact. Retained `CampaignOperationStageOrder` evolves to the same pair identity. Isolated internal pair-value codecs/structural validators enforce versions, value invariants, ordering, and duplicate freedom without asserting reachability; `CampaignSnapshotValidator` composes them and separately retains strict setup/position/state-version reachability checks. |
| `WTH-014` | At a valid unresolved Weather checkpoint, the `system` audience receives exactly one candidate with kind `resolve-weather`. Its canonical semantic bytes are `{"contractVersion":1,"kind":"resolve-weather"}` and its derived `actionId` is `sha256:61bca28b7e06c2ec8b7919bce4c7c226198e7fecb0afcc2186b224311e7e1413`. Axis and Commonwealth receive successful empty sets. Candidate query is pure, deterministic, and consumes no randomness. |
| `WTH-015` | `ResolveWeather` carries only contract version, expected state version, and expected position ID. It carries no Game Turn, stage, side, season, die, table coordinate, outcome, area, source, policy, or random cursor. |
| `WTH-016` | Decision validation checks command shape, admitted authority, concurrency, Weather position, retained stage order for the current `(GameTurn, OperationStage)`, retained initiative holder, exact Weather policy, Weather-area content compatibility, absence of a duplicate pair result, and supported ruleset before any random draw. |
| `WTH-017` | Accepted resolution emits exactly one `WeatherDetermined` event, increments state version once, invokes exactly two or three accepted d6 operations, advances the byte cursor across all bytes consumed by rejection sampling, appends exactly one `(GameTurn, OperationStage)` Weather record, preserves initiative holder/stage order/world facts, and advances to Organization in the same Game Turn and stage. |
| `WTH-018` | `WeatherDetermined` uses required event discriminator `weather-determined` and records complete accepted evidence, including Game Turn and Operation Stage, needed to explain and validate the result. Projection independently recomputes season, dice, outcome, affected areas, cursor, policy compatibility, the design's exact base-plus-outcome source array, and same-pair successor position; any missing/extra/duplicate/reordered source or other forged history throws `InvalidCampaignHistoryException`. |
| `WTH-019` | Canonical event and snapshot serializers reject unknown versions, missing or unknown event discriminators, properties, enum tokens, malformed d66, invalid kind/scope/location-die/area combinations, noncanonical area ordering, wrong source evidence, duplicate `(GameTurn, OperationStage)` Weather or actor-order identity, and inconsistent random cursors. |
| `WTH-020` | Evolve the Land sequence to one Organization Phase position with no ordered Organization segment/step positions. Positive Reorganization, Construction, and Training actions remain future actions at that barrier. |
| `WTH-021` | Weather v1 advances exactly from the current `(GameTurn, OperationStage)` Weather position to that pair's Organization barrier. It cannot advance to Naval Convoy Arrival, Fleet, Reserve, Movement, another stage, or another Game Turn. |
| `WTH-022` | Campaign Observation contract 2 retains Observation v1's top-level canonical `rulesetHash` concurrency identity and adds exact nullable nested `weather`. Before resolution `weather` is null; after resolution it is contract 1 with `gameTurn`, `operationStage`, `season`, `kind`, `scope`, and canonical `affectedAreas`. The nested value contains no dice, additional ruleset/content/setup hash, source references, random algorithm, seed, cursor, rejected bytes, or future outcomes. |
| `WTH-023` | Equal admitted authority produces equal semantic resolution and byte-identical event/snapshot/observation output across runs and supported cultures. Querying actions or observations never consumes randomness. |
| `WTH-024` | Unsupported ruleset, policy, content, state, future positive immediate-effect subjects, or out-of-range Game Turn produces a stable typed rejection with zero events and unchanged authority/random cursor. The pure rules lookup rejects Game Turn 111 before constructing randomness; Weather v1 does not claim command-level turn-111 evidence because no admitted setup reaches that checkpoint. No fallback guesses or extrapolates a result; authoritative turn-111 rejection evidence is deferred with full-campaign admission. |
| `WTH-025` | Weather rules, decision, projection, serialization, and queries remain pure, synchronous, cancellation-free, dependency-free Core operations with no clock, network, file, service-container, model, host, persistence, or transport I/O. |

## Corrected normalized table

The implementation must encode the exact factual table below as rules data with provenance; it
must not scatter thresholds through campaign decision code.

| Season | Game Turns | Normal d66 | Hot d66 | Sandstorm d66 | Rainstorm d66 |
| --- | --- | --- | --- | --- | --- |
| Fall | `1-12`, `49-60`, `97-108` | `11-35` | `36-54` | `55-61` | `62-66` |
| Winter | `13-24`, `61-72`, `109-110` | `11-52` | none | none | `53-66` |
| Spring | `25-36`, `73-84` | `11-42` | `43-55` | `56-64` | `65-66` |
| Summer | `37-48`, `85-96` | `11-23` | `24-55` | `56-66` | none |

## Validation precedence

Before random resolution, reject in this order:

1. null/programmer misuse;
2. unsupported command contract or malformed concurrency coordinates;
3. invalid or mismatched admitted authority;
4. stale state version;
5. unexpected position;
6. missing/inconsistent initiative holder or stage order;
7. unsupported/mismatched Weather policy or content Weather areas;
8. duplicate `(GameTurn, OperationStage)` actor order or Weather;
9. unsupported Game Turn/ruleset.

Action submission continues to apply Legal Actions v1 shape, authority, concurrency, position, and
exact-audience membership precedence before translation to the internal command.

## Acceptance scenarios

| ID | Scenario | Expected evidence |
| --- | --- | --- |
| `WTH-AC-001` | Validate the normalized definition and ruling manifest | Every Game Turn `1-110` maps to exactly one season; every valid d66 maps to exactly one outcome for that season; every range, outcome, and foul-location value resolves the design's exact frozen provenance group. The ruling segment is byte-identical to the design's compact canonical UTF-8 golden, including conflict, sorted alternatives, selected behavior, protecting IDs, and ordered sources; changing any array member changes the full ruleset manifest hash and fails the golden. |
| `WTH-AC-002` | Resolve boundary d66 values for all four seasons | Each inclusive threshold selects the expected outcome; the dice are ordered, not summed. |
| `WTH-AC-003` | Resolve normal/hot then foul outcomes | Normal invokes two accepted d6 with scope `none`; hot invokes two with scope `global`; sandstorm/rainstorm invoke three with scope `listed-areas` and the exact canonical non-empty area set. At least one seeded vector consumes a rejected byte before an accepted die and proves accepted-die evidence plus the larger exact byte-cursor delta. Location roll 3 retains E and the artifact retains the deferred Delta reference. |
| `WTH-AC-004` | Resolve both synthetic setups from Legal Actions v1's Weather checkpoint | Game Turn 1 uses Fall; Game Turn 43 uses Summer; each accepted event reaches the same stage's Organization barrier. |
| `WTH-AC-005` | Query every audience at Weather | System receives exactly kind `resolve-weather` with the frozen derived SHA-256 action ID; both sides are empty; repeated query is byte-identical and cursor-neutral. |
| `WTH-AC-006` | Validate, remove, or alter Weather policy or area assignment | A content golden locks the design's exact nine location/area/provenance rows and survives input permutation; removing, adding, duplicating, or altering any row, policy, or reference makes admission, decision, projection, or replay reject before drawing with zero events. |
| `WTH-AC-007` | Forge season, dice order, outcome, location die, area list, determining side, cursor, source, or successor | Projection rejects the event and retains no partial state. |
| `WTH-AC-008` | Replay campaign creation through Weather | Rebuilt canonical snapshot bytes equal accepted projection bytes and contain exactly one Weather record for the resolved `(GameTurn, OperationStage)`. |
| `WTH-AC-009` | Reuse or alter the submitted action | Reused/stale action, wrong position, altered kind, or nonmatching action ID fails existing shape/concurrency/position/exact-membership precedence with zero events and unchanged cursor. |
| `WTH-AC-010` | Serialize malformed Weather event/snapshot variants | Canonical event bytes place fixed `eventType: "weather-determined"` immediately after `contractVersion`; its absence, alteration, or reordering fails closed. Every other unknown, missing, duplicate, noncanonical, or inconsistent field also fails, including every invalid kind/scope/location-die/affected-area combination. |
| `WTH-AC-011` | Project observations before and after resolution | Observation contract 2 preserves the top-level ruleset hash. Before resolution nested `weather` is null; after resolution it contains exactly contract, current Game Turn, Operation Stage, season, kind, scope, and canonical areas—no dice or nested authority/provenance/random fields. An isolated pure-selector fixture with older-pair history plus a different unresolved current pair returns null without constructing an unreachable snapshot. Golden bytes cover normal, global hot, and listed foul scope. |
| `WTH-AC-012` | Attempt to advance from Organization with the Weather action or a generic command | Exact-audience membership or internal transition validation rejects with zero events. |
| `WTH-AC-013` | Validate pair-keyed historical contracts across repeated stage numbers | Direct fixtures exercise isolated `CampaignOperationStageOrder` and `CampaignOperationStageWeather` value codecs/structural collection validators with `(GameTurn 1, OperationStage 1)` and `(GameTurn 2, OperationStage 1)`. They sort, serialize, parse, and round-trip without collision; same-pair duplicates fail. The test does not construct or validate a `CampaignSnapshot`, weaken reachability, or claim authoritative turn rollover. End-to-end two-turn replay is deferred to the first capability that legitimately completes a Game Turn. |
| `WTH-AC-014` | Query the pure Weather rules definition at Game Turn 111 | The table-support rejection occurs before constructing a random cursor and no Winter row is inferred. This Task-1 rules test makes no command/event/authority claim; authoritative turn-111 evidence is deferred until a later admitted full-campaign checkpoint exists. |

## Testing strategy

- Table-driven unit tests for every season range, every d66 threshold, and all six location rolls.
- Seed-search helpers in tests may identify deterministic vectors, but accepted production commands
  never carry rolls or seeds.
- Retain at least one fixed seeded Weather vector where `RollD6` rejects a byte in `252-255`, proving
  accepted-die evidence and exact byte-cursor advancement are distinct.
- Golden canonical JSON for Weather rules artifact, event, snapshot, and observation contracts.
- Property-style invariants for table coverage, ordering, equality/hash, immutability, and culture.
- Cross-turn structural contract tests proving actor-order and Weather identity, duplicate
  handling, canonical serialization, parsing, and ordering are keyed by
  `(GameTurn, OperationStage)`. End-to-end cross-turn projection/replay evidence is explicitly
  deferred until turn rollover is reachable through authoritative mechanics.
- Forged-history and parser-negative tests derived from each event invariant.
- Full public-surface and cross-assembly boundary tests retained from Legal Actions v1.
- Full solution build, tests, format verification, documentation links, and independent review
  before merge.

## Boundaries

Always:

- update source-normalized contracts and manifest before campaign consumers;
- preserve source references and record the approved ruling;
- reject before drawing when authority cannot support every possible accepted result;
- recompute events during projection and replay; and
- keep player observations/actions source-free and side-safe.

Ask first:

- change the proposed ruling or season boundaries;
- support positive fuel/water, well, or grounded-aircraft immediate effects;
- add a dependency or public adapter; or
- expand Weather v1 past Organization.

Never:

- add generic sequence advancement;
- let callers select dice, season, outcome, areas, or immediate effects;
- infer empty obligations from missing schema fields;
- expose seed/cursor/future draws to a side or Intelligence; or
- commit source scans or copied rules prose.

## Explicit non-goals

- Positive stored fuel/water attrition, depleted-well restoration, or grounded-aircraft damage.
- Executable Nile Delta sandstorm exclusion or published-map subarea representation; Weather v1
  retains only the stable deferred rule reference and unmodified affected-area result.
- Organization, Reorganization, Construction, Training, Naval Convoy Arrival, Fleet, Reserve, or
  Movement actions.
- General Naval Convoy, logistics, production, replacement, ports, or arrivals.
- Weather effect enforcement inside future movement, construction, breakdown, air, or water rules.
- Published scenario/map transcription.
- Orleans, Chronicle persistence/provenance, HTTP/protobuf, Maproom, or Intelligence integration.

## Traceability

| Requirement group | Governing decision/source | Planned tasks | Verification |
| --- | --- | --- | --- |
| `WTH-001`-`WTH-006` | `TURN-DEC-001`, `TURN-DEC-006`; Rules 29.0-29.1; Charts 29.61/29.7; errata 29.1/29.61 | `WTH-TASK-001` | `WTH-AC-001`-`003`, `014` |
| `WTH-007` | deterministic random-procedure metadata | `WTH-TASK-002` | existing Initiative vectors plus `WTH-AC-003` |
| `WTH-008`-`WTH-009` | typed source-neutral content assignments | `WTH-TASK-003` | content validation/permutation tests; `WTH-AC-006` |
| `WTH-010`-`WTH-011` | `TURN-DEC-003`; explicit-admission policy | `WTH-TASK-004` | `WTH-AC-004`, `006` |
| `WTH-012` | admitted authority and canonical campaign identity | `WTH-TASK-004`, `WTH-TASK-006` | `WTH-AC-006`, `008`, `010` |
| `WTH-013` | cross-turn actor-order and Weather identity | `WTH-TASK-005`, `WTH-TASK-006` | `WTH-AC-008`, `010`, `013` |
| `WTH-014` | exact-audience legal-action query | `WTH-TASK-007` | `WTH-AC-005`, `009` |
| `WTH-015`-`WTH-017` | deterministic authority transition | `WTH-TASK-006` | `WTH-AC-003`, `004`, `007`, `009` |
| `WTH-018` | recomputed projection | `WTH-TASK-006` | `WTH-AC-007`, `008` |
| `WTH-019` | strict canonical serialization | `WTH-TASK-006` | `WTH-AC-010`, `013` |
| `WTH-020` | `TURN-DEC-004`; Rule 5.2 | `WTH-TASK-002` | sequence catalog tests; `WTH-AC-012` |
| `WTH-021` | `TURN-DEC-002`; exact same-pair successor | `WTH-TASK-002`, `WTH-TASK-006` | `WTH-AC-004`, `012` |
| `WTH-022` | FOW-001; public current Weather only | `WTH-TASK-007` | `WTH-AC-011` |
| `WTH-023` | DET-001/002; byte-identical authority | `WTH-TASK-006`, `WTH-TASK-007` | `WTH-AC-005`, `008`, `011`, `013` |
| `WTH-024` | fail-closed support boundary; `TURN-DEC-006` | `WTH-TASK-001`, `WTH-TASK-004`, `WTH-TASK-006` | `WTH-AC-006`, `009`, `014` |
| `WTH-025` | Core purity boundary | `WTH-TASK-006` | architecture tests plus full gate |

No implementation or executed test evidence exists yet. Status remains proposed until owner approval
and incomplete until every acceptance scenario has executed evidence.

## Open questions

- Does the project owner approve the proposed season-boundary ruling?
- Does the project owner approve explicit Game Turn 111 rejection in Weather v1, with the source
  gap deferred to a later full-campaign ruling rather than silently extrapolated?
- Does the project owner approve the single Organization barrier and revised capability split?
