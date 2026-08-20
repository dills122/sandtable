# Weather Determination v1 Technical Design

**Status:** Implemented and verified 2026-08-19

**Date:** 2026-08-18

**Capability:** `WEATHER-001`

**Specification:** [Weather Determination v1](../specs/weather-determination-v1.md)

**Research:** [Operation-Stage Preamble Source and Contract Spike](../research/operation-stage-preamble-spike.md)

## Design summary

Weather v1 extends the existing opaque-authority/legal-action path by one mechanic. Rules data maps
Game Turn to corrected season, two deterministic d6 draws to Weather, and a conditional third draw
to affected Weather areas. A setup-hashed synthetic policy proves that no immediate resource, well,
or grounded-aircraft mutations are owed in this version. One `WeatherDetermined` event records the
complete recomputable result and moves the campaign exactly to a corrected Organization barrier.

No host, service, persistence, UI, or Intelligence contract changes.

```text
CampaignAuthorityHandle
        |
        v
CampaignLegalActions.Query(system)
        |
        `-- resolve-weather
                |
                v
CampaignLegalActions.Submit
  membership + concurrency + admitted policy/content
                |
                v
WeatherResolver
  Game Turn -> season
  d6,d6 -> d66 -> kind
  foul ? d6 -> areas : no third draw
                |
                v
WeatherDetermined
                |
                v
CampaignProjector recomputes exact result
                |
                v
successor handle at Organization barrier
```

## Architecture decisions

### `TURN-DEC-001`: Resolve the season conflict through a manifest ruling

Use the explicit errata 29.1 boundaries and remap chart Game-Turn ranges to seasons. Preserve each
season's published probability row. Record both alternatives in the ruleset ruling ledger. Do not
hide the conflict in resolver constants or documentation only.

### `TURN-DEC-002`: Weather is one vertical slice

Weather v1 owns Weather inputs, random draws, outcome, affected areas, public state, and immediate
effect admission. It stops at Organization. It does not claim that the full Operation Stage preamble
is complete.

### `TURN-DEC-003`: Admit empty immediate effects explicitly

The current content/world model cannot distinguish “there are no stored resources/wells/aircraft”
from “those systems are not modeled.” Setup policy contract 1 closes that gap only for the two
synthetic fixtures. Any missing, altered, or positive-subject case rejects before randomness.

### `TURN-DEC-004`: Organization is a barrier, not an ordered catalog walk

Replace the catalog's fixed positive Organization segment positions with one Organization Phase
position. Future Organization legal actions execute at that position in player-selected order;
completion becomes its own mechanic-specific decision after all obligations are satisfied.

### `TURN-DEC-005`: Weather is a system action

The source assigns physical rolling to the initiative holder, but the player makes no rules choice.
The Umpire therefore exposes `resolve-weather` to `system`, derives the determining side from
authority, and records it for explanation. Side audiences remain empty.

### `TURN-DEC-006`: Game Turn 111 remains an explicit unsupported boundary

The published Weather chart assigns seasons only through Game Turn 110 while the campaign is
described as 111 turns and Weather is generally required in each Operation Stage. Weather v1 must
not infer Winter for turn 111 or invent a shortened final turn. It returns the stable unsupported-
turn rejection before randomness. A later full-campaign ruling may replace this scoped deferral
after source review establishes the final turn's sequence and Weather treatment.

## Proposed rules model

```csharp
internal enum WeatherSeason { Fall = 1, Winter = 2, Spring = 3, Summer = 4 }
internal enum WeatherKind { Normal = 1, Hot = 2, Sandstorm = 3, Rainstorm = 4 }
internal enum WeatherScope { None = 1, Global = 2, ListedAreas = 3 }
internal enum WeatherArea { A = 1, B = 2, C = 3, D = 4, E = 5 }

internal sealed record WeatherResolution(
    WeatherSeason Season,
    int FirstDie,
    int SecondDie,
    WeatherKind Kind,
    WeatherScope Scope,
    int? LocationDie,
    IReadOnlyList<WeatherArea> AffectedAreas,
    RandomStreamState RandomState);
```

`Cna1979Weather` owns pure lookup. It accepts supported Game Turn and a `SandtableRandom` cursor,
draws in fixed order, returns immutable canonical values, and does not know campaigns, setups,
content, observations, or serialization.

### Corrected lookup data

Store season Game-Turn ranges and outcome d66 ranges as immutable rows. Each row resolves its exact
source set through the frozen schema-level provenance groups below; rows never select or infer
provenance dynamically. Validate during static construction or artifact creation:

- every Game Turn `1-110` belongs to exactly one season;
- Game Turn `111` and all other unsupported turns remain outside the table and reject before a
  random cursor is constructed;
- every valid ordered d66 belongs to exactly one outcome per season;
- no invalid digit, overlap, gap, empty affected-area set, or duplicate source exists; and
- each affected-area list is ordinally canonical; roll 3 retains E; and
- artifact metadata retains deferred rule reference `spi-1979-land-rules:29.41`, explicitly marking
  the Nile Delta sandstorm exclusion non-executable until a published subarea contract exists.

The ruleset manifest adds artifact `cna-1979.1.weather-tables`, schema 1, and ruling
`cna-1979.1.ruling.weather-season-boundary`. Game Turn 111 rejection is a declared capability
support boundary under `TURN-DEC-006`, not a second ruleset ruling; no canonical ruling authority is
invented while the intended source behavior remains unresolved.

The ruling's canonical manifest record uses the existing writer order and these exact sorted arrays:

```json
{
  "rulingId": "cna-1979.1.ruling.weather-season-boundary",
  "conflictId": "cna-1979.1.conflict.weather-season-boundary",
  "alternativeIds": [
    "use-errata-29.61-parenthetical-and-derive-shifted-ranges",
    "use-rule-29.1-boundaries-and-remap-chart-game-turns"
  ],
  "selectedBehaviorId": "use-rule-29.1-boundaries-and-remap-chart-game-turns",
  "protectingTestIds": ["WTH-AC-001", "WTH-AC-002", "WTH-AC-004"],
  "sources": [
    { "sourceId": "spi-1979-common-charts", "locator": "29.61" },
    { "sourceId": "spi-1979-errata", "locator": "29.1" },
    { "sourceId": "spi-1979-errata", "locator": "29.61" },
    { "sourceId": "spi-1979-land-rules", "locator": "29.0-29.1" }
  ]
}
```

The byte-exact ruling golden is the existing canonical writer's compact UTF-8 serialization of
that property order:

```text
{"rulingId":"cna-1979.1.ruling.weather-season-boundary","conflictId":"cna-1979.1.conflict.weather-season-boundary","alternativeIds":["use-errata-29.61-parenthetical-and-derive-shifted-ranges","use-rule-29.1-boundaries-and-remap-chart-game-turns"],"selectedBehaviorId":"use-rule-29.1-boundaries-and-remap-chart-game-turns","protectingTestIds":["WTH-AC-001","WTH-AC-002","WTH-AC-004"],"sources":[{"sourceId":"spi-1979-common-charts","locator":"29.61"},{"sourceId":"spi-1979-errata","locator":"29.1"},{"sourceId":"spi-1979-errata","locator":"29.61"},{"sourceId":"spi-1979-land-rules","locator":"29.0-29.1"}]}
```

Task 1 asserts those exact bytes as the ruling segment in the manifest golden and proves that
changing any alternative, protecting ID, or source changes the full ruleset manifest hash.

## Random procedure migration

Keep the byte stream and d6 normalization unchanged. Evolve only normalized rules metadata from the
Initiative-specific procedure shape to named procedures:

```text
initiative-determination: axis, commonwealth, repeat-on-tie
weather-determination: tens, ones, location-if-foul
```

The Weather resolver invokes the existing accepted-d6 operation twice, then once more only for
sandstorm or rainstorm. `NextByteCursor` is a byte offset, not a draw count: each operation may skip
bytes 252-255 before accepting a die. Event projection starts from the predecessor cursor and must
reproduce accepted dice, every internally consumed byte, and the exact successor byte cursor. A
seeded acceptance vector must cross at least one rejected byte. No public value includes algorithm,
seed, byte cursor, rejected bytes, or future stream data.

## Content migration

Add `ContentWeatherAreaAssignment(locationId, weatherArea, origin)` to Content Pack schema 2 and
canonical JSON. A pack declaring capability `land.weather-areas` must provide exactly one assignment
per location. The assignment is static public geography, not changing campaign Weather.

The synthetic pack receives original, repository-sourced A-E assignments selected to exercise:

- multiple affected areas;
- unaffected locations;
- canonical ordering independent from input order; and
- the D/E location result.

The fixture remains nonhistorical. It does not gain printed coordinates or claim correspondence to
the published map. It also does not gain a synthetic Nile Delta marker: Weather v1 applies no
positive area effects and carries only the deferred 29.41 rule reference. A future published-content
capability must define and validate a typed subarea identity before enforcing the exception.

Content validation issue codes should be stable, for example:

```text
content.weather-area.missing-location
content.weather-area.duplicate-location
content.weather-area.unknown-location
content.weather-area.undefined-area
content.weather-area.unexpected-without-capability
```

## Setup and authority migration

Add `CampaignWeatherPolicy` contract 1 to setup schema 4:

```text
contractVersion: 1
kind: no-immediate-weather-effect-subjects
sources:
  - sandtable-rules-lab:weather.no-immediate-effect-subjects.v1
```

It is admitted only for the two current synthetic setup IDs. It participates in setup equality,
canonical bytes, setup hash, campaign creation event, setup snapshot, snapshot validation, and
replay preparation.

Clean-cut the current executable to the exact migration matrix below. Previous history remains
executable by its Git revision; the new executable rejects old setup/content/snapshot contracts
rather than guessing missing policy or Weather areas. Unchanged outer creation shapes retain their
versions while their canonical values/goldens change with nested identities.

## Sequence migration

Introduce Land-sequence artifact schema 2 for the changed catalog while retaining
`LandSequencePosition` wire contract 2 because its property shape is unchanged. Do not continue
using one constant for both meanings. Also evolve `CampaignOperationStageOrder` to contract 2 by
adding `GameTurn`; actor order is historical state identified by `(GameTurn, OperationStage)`,
never by the repeating stage number alone:

```text
initiative-declaration
weather-determination
organization                <- one phase barrier
naval-convoy-arrival
commonwealth-fleet.assignment
commonwealth-fleet.repair
first-player.reserve-designation
...
```

Remove Reorganization, Construction, and Training subpositions from the top-level ordered catalog.
Their identifiers may remain as future action/mechanic vocabulary, but `GetNext(Weather)` returns
the Organization phase and no positive Organization ordering is asserted by catalog position.

No implemented history currently proceeds past Weather, so this migration does not reinterpret an
accepted Organization event.

## Campaign state

Add immutable `CampaignOperationStageWeather` contract 1 and a canonical collection sorted by
`(GameTurn, OperationStage)` on `CampaignSnapshot`.

The value contains:

- contract version, Game Turn, and Operation Stage;
- determining initiative holder;
- corrected season;
- ordered first/second Weather dice;
- Weather kind and explicit spatial scope;
- optional location die;
- canonical affected areas; and
- zero immediate fuel/water, well, and grounded-aircraft effect counts for v1.

Invariants:

- no result before the stage's Weather resolution;
- at most one result per reached `(GameTurn, OperationStage)` pair;
- pairs form no impossible future/duplicate set and may coexist for the same Operation Stage in
  different Game Turns;
- every result has exactly one retained actor-order record for the same pair;
- location die exists exactly for foul Weather;
- normal uses scope `none`, no location die, and empty areas;
- hot uses scope `global`, no location die, and empty areas;
- sandstorm/rainstorm use scope `listed-areas`, a location die, and the exact canonical non-empty
  location-table areas;
- the determining side equals retained initiative holder; and
- v1 immediate-effect counts are zero and require the exact admitted policy.

### Structural pair codecs versus authority validation

Add internal `CampaignOperationStageOrderCodec` and `CampaignOperationStageWeatherCodec` value/
collection seams. Each writer/parser fixes the canonical shape below; each structural validator
checks only contract version, field invariants, `(GameTurn, OperationStage)` ordering, and duplicate
freedom. `WTH-AC-013` calls these isolated seams directly with repeated stage numbers across turns.

`CampaignSnapshotValidator` composes the same structural validators but separately enforces setup
initial turn, current position, retained actor, state-version index, no-future-record, and reachable-
history constraints. The structural fixture never creates a `CampaignSnapshot` and cannot weaken
authoritative validation to admit an unreachable Game Turn 2 checkpoint.

## Command and event

`ResolveWeather` carries contract version, expected state version, and expected position ID only.

`WeatherDetermined` carries:

- normal event envelope and exact predecessor position;
- Game Turn, Operation Stage, and determining side;
- season, both Weather dice, kind, and scope;
- optional location die and affected areas;
- successor random cursor;
- exact rules/ruling source references; and
- exact successor Organization position.

The event carries no setup/content hash, complete policy, seed, future randomness, hidden force
fact, or caller-authored result. Projection resolves exact content before entry, validates the
policy, replays the random procedure from predecessor authority, builds the expected event, and
compares the complete value.

## Legal actions and observations

At Weather:

| Audience | Candidates |
| --- | --- |
| system | exactly `resolve-weather` |
| Axis | empty |
| Commonwealth | empty |

Submission uses the existing exact-audience membership gate, then translates internally to
`ResolveWeather`.

`resolve-weather` is the candidate `kind`, not its action ID. Contract 1 semantic bytes are exactly
`{"contractVersion":1,"kind":"resolve-weather"}` with no `operationStage` property. Existing
SHA-256 derivation produces
`sha256:61bca28b7e06c2ec8b7919bce4c7c226198e7fecb0afcc2186b224311e7e1413`.

Campaign Observation contract 2 retains every Observation v1 top-level property, including the
canonical `rulesetHash`, and adds exact nullable nested `weather`. It is `null` before resolution;
after resolution its complete contract 1 shape is:

```text
contractVersion
gameTurn
operationStage
season
kind
scope
affectedAreas
```

The nested value contains no dice, hashes, source references, algorithm, seed, cursor, rejected
bytes, or future outcomes. Dice may appear later in Chronicle explanation, but are explicitly not
part of Campaign Observation contract 2.

Internal pure `CampaignObservationWeatherSelector.Select(currentPair, history)` selects nested
Weather only by exact `(GameTurn, OperationStage)` equality; the full projector delegates to it. A
historical record from an earlier pair never appears as current, and an isolated selector test can
prove this without manufacturing an unreachable snapshot. When the current pair is unresolved,
`weather` is null even if older Weather history exists.

## Validation and failure behavior

All support/admission checks occur before constructing `SandtableRandom`. A typed rejection returns
no event, successor handle, receipt, or random mutation. Invalid trusted event history throws and
does not return partial projection.

Key failure classes:

- malformed/unsupported command;
- invalid authority or concurrency;
- wrong phase/stage or missing actor order;
- unsupported ruleset/season;
- missing or altered Weather policy;
- incompatible/missing content Weather areas;
- duplicate `(GameTurn, OperationStage)` actor order or Weather result; and
- any future positive immediate-effect subject under policy contract 1.

## Canonical serialization

Use explicit writers/readers. Do not rely on reflection or default enum formatting. Arrays are
sorted before construction and writers preserve that canonical order. Readers reject unknown
properties and noncanonical variants rather than normalizing untrusted bytes silently.

### Exact contract and canonical-byte migration matrix

| Artifact or contract | Current | Weather v1 | Activation and byte effect |
| --- | --- | --- | --- |
| `Cna1979Ruleset` manifest | 2 | 3 | Tasks 1-2; artifacts/ruling and canonical hash change |
| Land-sequence artifact schema | 1 | 2 | Task 2; catalog membership changes at/after Weather |
| `LandSequencePosition` wire contract | 2 | 2 | Shape is unchanged; decouple it from catalog schema so retained pre-Weather position bytes stay stable |
| Random-procedure artifact schema | 1 | 2 | Task 2; adds named Initiative and Weather procedures; RNG byte algorithm unchanged |
| Content Pack schema | 1 | 2 | Task 3; assignments/hash and dependent creation bytes change |
| Setup schema | 3 | 4 | Task 4; adds Weather policy contract 1 and changes setup/hash/creation bytes |
| `CampaignCreationRequest` / internal `CreateCampaign` | 1 / 4 | 1 / 4 | Shapes unchanged; callers supply new canonical hashes |
| `CampaignCreated` | 4 | 4 | Shape unchanged; canonical bytes intentionally change with ruleset/setup/content identities; update goldens in Task 4 |
| `InitiativeDetermined` / opening-preamble events | 2 / 1 | 2 / 1 | Shapes and retained pre-Weather positions unchanged; canonical bytes remain stable after the Task-4 baseline |
| `CampaignOperationStageOrder` | 1 | 2 | Task 5; adds Game Turn; post-declaration snapshot bytes change |
| `CampaignSnapshot` | 4 | 5 | Nested setup/order bytes evolve in Tasks 4-5; Task 6 adds the Weather collection and activates outer contract 5/goldens |
| `WeatherDetermined` | absent | 1 | Task 6; new strict event contract |
| Legal-action set/candidate/submission | 1 | 1 | Generic opaque shapes unchanged; add kind `resolve-weather`; derive action ID from frozen semantic bytes |
| `CampaignObservation` / nested Weather | 1 / absent | 2 / 1 | Task 7; preserve top-level ruleset hash and add exact nullable nested Weather; update goldens |

No checkpoint claims universal byte stability. Task-specific goldens distinguish unchanged wire
shapes from canonical values that intentionally change because a nested identity or hash changed.

### Canonical additions and exact writer order

All writers emit the properties below in listed order. Arrays use the stated order. Parsers require
the exact property set, token spelling, and canonical order; they do not normalize alternate input.
Every `sources` item is `{ "sourceId", "locator" }` in that property order and sources sort by
`sourceId`, then `locator`, ordinally.

Weather rules definition schema 1, whose exact UTF-8 bytes are hashed for the ruleset artifact:

```json
{
  "schemaVersion": 1,
  "provenance": {
    "gameTurnRanges": [
      { "sourceId": "cna-1979.1.ruling.weather-season-boundary", "locator": "selected-behavior" },
      { "sourceId": "spi-1979-common-charts", "locator": "29.61" },
      { "sourceId": "spi-1979-errata", "locator": "29.1" },
      { "sourceId": "spi-1979-errata", "locator": "29.61" },
      { "sourceId": "spi-1979-land-rules", "locator": "29.0-29.1" }
    ],
    "outcomes": [
      { "sourceId": "spi-1979-common-charts", "locator": "29.61" },
      { "sourceId": "spi-1979-errata", "locator": "29.61" }
    ],
    "foulWeatherLocations": [
      { "sourceId": "spi-1979-common-charts", "locator": "29.7" },
      { "sourceId": "spi-1979-land-rules", "locator": "29.1" }
    ]
  },
  "seasons": [
    {
      "season": "fall",
      "gameTurnRanges": [
        { "first": 1, "last": 12 },
        { "first": 49, "last": 60 },
        { "first": 97, "last": 108 }
      ],
      "outcomes": [
        { "kind": "normal", "firstD66": 11, "lastD66": 35 },
        { "kind": "hot", "firstD66": 36, "lastD66": 54 },
        { "kind": "sandstorm", "firstD66": 55, "lastD66": 61 },
        { "kind": "rainstorm", "firstD66": 62, "lastD66": 66 }
      ]
    },
    {
      "season": "winter",
      "gameTurnRanges": [
        { "first": 13, "last": 24 },
        { "first": 61, "last": 72 },
        { "first": 109, "last": 110 }
      ],
      "outcomes": [
        { "kind": "normal", "firstD66": 11, "lastD66": 52 },
        { "kind": "rainstorm", "firstD66": 53, "lastD66": 66 }
      ]
    },
    {
      "season": "spring",
      "gameTurnRanges": [
        { "first": 25, "last": 36 },
        { "first": 73, "last": 84 }
      ],
      "outcomes": [
        { "kind": "normal", "firstD66": 11, "lastD66": 42 },
        { "kind": "hot", "firstD66": 43, "lastD66": 55 },
        { "kind": "sandstorm", "firstD66": 56, "lastD66": 64 },
        { "kind": "rainstorm", "firstD66": 65, "lastD66": 66 }
      ]
    },
    {
      "season": "summer",
      "gameTurnRanges": [
        { "first": 37, "last": 48 },
        { "first": 85, "last": 96 }
      ],
      "outcomes": [
        { "kind": "normal", "firstD66": 11, "lastD66": 23 },
        { "kind": "hot", "firstD66": 24, "lastD66": 55 },
        { "kind": "sandstorm", "firstD66": 56, "lastD66": 66 }
      ]
    }
  ],
  "foulWeatherLocations": [
    { "die": 1, "areas": ["a", "b"] },
    { "die": 2, "areas": ["c", "d"] },
    { "die": 3, "areas": ["d", "e"] },
    { "die": 4, "areas": ["b", "c"] },
    { "die": 5, "areas": ["b", "d"] },
    { "die": 6, "areas": ["b", "c", "d"] }
  ],
  "deferredRules": [
    {
      "ruleId": "nile-delta-sandstorm-exclusion",
      "weatherKind": "sandstorm",
      "area": "e",
      "status": "deferred",
      "sources": [{ "sourceId": "spi-1979-land-rules", "locator": "29.41" }]
    }
  ],
  "sources": [
    { "sourceId": "cna-1979.1.ruling.weather-season-boundary", "locator": "selected-behavior" },
    { "sourceId": "spi-1979-common-charts", "locator": "29.61" },
    { "sourceId": "spi-1979-common-charts", "locator": "29.7" },
    { "sourceId": "spi-1979-errata", "locator": "29.1" },
    { "sourceId": "spi-1979-errata", "locator": "29.61" },
    { "sourceId": "spi-1979-land-rules", "locator": "29.0-29.1" },
    { "sourceId": "spi-1979-land-rules", "locator": "29.1" },
    { "sourceId": "spi-1979-land-rules", "locator": "29.41" }
  ]
}
```

Seasons order `fall`, `winter`, `spring`, `summer`; ranges sort by `first`; outcomes retain only
present kinds in `normal`, `hot`, `sandstorm`, `rainstorm` order. Foul rows sort by `die`; areas sort
`a` through `e`. Every Game-Turn range inherits exactly `provenance.gameTurnRanges`; every outcome
inherits exactly `provenance.outcomes`; and every foul-location row inherits exactly
`provenance.foulWeatherLocations`. The artifact validator recomputes `sources` as the ordinal union
of all three provenance groups plus every deferred-rule source, rejecting any missing, extra,
duplicate, reordered, or otherwise noncanonical reference. Golden tests change one member of each
group and prove both the artifact hash and ruleset manifest hash change.

Random-procedure schema 2 keeps the existing first nine scalar properties in order, replaces
`initiativeDrawOrder` with `procedures`, then writes `sources`:

```json
{
  "schemaVersion": 2,
  "algorithmId": "sandtable.sha256-counter.v1",
  "domainAscii": "sandtable.random.v1",
  "separatorByte": 0,
  "integerEncoding": "unsigned-64-big-endian",
  "blockBytes": 32,
  "d6AcceptBelow": 252,
  "d6Modulo": 6,
  "d6Offset": 1,
  "procedures": [
    {
      "procedureId": "initiative-determination",
      "acceptedD6Order": ["axis", "commonwealth"],
      "repeat": "whole-procedure-on-tie",
      "conditionalAcceptedD6": null
    },
    {
      "procedureId": "weather-determination",
      "acceptedD6Order": ["tens", "ones"],
      "repeat": "never",
      "conditionalAcceptedD6": {
        "label": "location",
        "whenKindIn": ["sandstorm", "rainstorm"]
      }
    }
  ],
  "sources": [
    { "sourceId": "sandtable-random-procedure", "locator": "sha256-counter.v1" },
    { "sourceId": "spi-1979-land-rules", "locator": "29.1" },
    { "sourceId": "spi-1979-land-rules", "locator": "7.14" }
  ]
}
```

Procedures use the shown order; `whenKindIn` uses the shown order.

Content Pack schema 2 top-level order is `schemaVersion`, `formatId`, `packId`, `rulesetId`,
`capabilities`, `sourceIndex`, `locations`, `weatherAreaAssignments`, `edges`, `formations`,
`elements`, `scenarios`. The new array sorts by `locationId` and is exactly:

```json
[
  { "locationId": "center", "weatherArea": "a", "origin": { "kind": "synthetic", "references": [{ "sourceId": "sandtable-rules-lab", "locator": "weather-areas.v1:center" }] } },
  { "locationId": "east", "weatherArea": "b", "origin": { "kind": "synthetic", "references": [{ "sourceId": "sandtable-rules-lab", "locator": "weather-areas.v1:east" }] } },
  { "locationId": "north", "weatherArea": "c", "origin": { "kind": "synthetic", "references": [{ "sourceId": "sandtable-rules-lab", "locator": "weather-areas.v1:north" }] } },
  { "locationId": "north-east", "weatherArea": "d", "origin": { "kind": "synthetic", "references": [{ "sourceId": "sandtable-rules-lab", "locator": "weather-areas.v1:north-east" }] } },
  { "locationId": "north-west", "weatherArea": "e", "origin": { "kind": "synthetic", "references": [{ "sourceId": "sandtable-rules-lab", "locator": "weather-areas.v1:north-west" }] } },
  { "locationId": "south", "weatherArea": "a", "origin": { "kind": "synthetic", "references": [{ "sourceId": "sandtable-rules-lab", "locator": "weather-areas.v1:south" }] } },
  { "locationId": "south-east", "weatherArea": "b", "origin": { "kind": "synthetic", "references": [{ "sourceId": "sandtable-rules-lab", "locator": "weather-areas.v1:south-east" }] } },
  { "locationId": "south-west", "weatherArea": "c", "origin": { "kind": "synthetic", "references": [{ "sourceId": "sandtable-rules-lab", "locator": "weather-areas.v1:south-west" }] } },
  { "locationId": "west", "weatherArea": "d", "origin": { "kind": "synthetic", "references": [{ "sourceId": "sandtable-rules-lab", "locator": "weather-areas.v1:west" }] } }
]
```

This fixture assigns A-E cyclically over the existing ordinal location-ID order. That mechanical
choice exercises all five areas, multi-location and unaffected results, and the D/E branch without
claiming historical geography. Each assignment uses the existing content-origin token `synthetic`;
the pack's source-index entry separately retains content-source kind `repository-synthetic`. The
canonical fixture golden locks all nine rows and references; permutation must reproduce the same
bytes and any missing, extra, or altered row must fail.

Setup schema 4 inserts `weather` after existing `openingPreamble` and before `content`; all other
top-level setup properties retain their existing order. The nested policy is exactly:

```json
{
  "contractVersion": 1,
  "kind": "no-immediate-weather-effect-subjects",
  "sources": [
    {
      "sourceId": "sandtable-rules-lab",
      "locator": "weather.no-immediate-effect-subjects.v1"
    }
  ]
}
```

Pair values and their collection arrays sort by `gameTurn`, then `operationStage`:

```json
{
  "contractVersion": 2,
  "gameTurn": 1,
  "operationStage": 1,
  "firstSide": "axis",
  "secondSide": "commonwealth"
}
```

```json
{
  "contractVersion": 1,
  "gameTurn": 1,
  "operationStage": 1,
  "determiningSide": "axis",
  "season": "fall",
  "firstDie": 1,
  "secondDie": 1,
  "kind": "normal",
  "scope": "none",
  "locationDie": null,
  "affectedAreas": [],
  "fuelWaterReductionSubjectCount": 0,
  "restoredWellCount": 0,
  "damagedGroundedAircraftCount": 0
}
```

Snapshot contract 5 retains current top-level order and inserts `operationStageWeather` immediately
after `operationStageOrders` and before `randomState`. Its items use the Weather value order above.

`WeatherDetermined` contract 1 canonical event property order is:

```text
contractVersion, eventType, campaignId, stateVersion, fromPositionId,
gameTurn, operationStage, determiningSide, season,
firstDie, secondDie, kind, scope, locationDie, affectedAreas,
fuelWaterReductionSubjectCount, restoredWellCount, damagedGroundedAircraftCount,
randomCursorAfter, sequencePosition, sources
```

`eventType` is the required fixed token `weather-determined`, immediately after `contractVersion`,
matching the existing event-dispatch contract. The parser rejects its absence, any other token, or
any other property order before constructing the event.

`locationDie` is always present and is null for normal/hot. `affectedAreas` is always present and
canonically sorted. `sources` uses the common source shape/order. `sequencePosition` reuses the
existing canonical `LandSequencePosition` writer unchanged.

`WeatherDetermined.sources` is recomputed, ordinally sorted, and outcome-dependent. Every event has
this exact base set:

```text
cna-1979.1.ruling.weather-season-boundary:selected-behavior
sandtable-rules-lab:weather.no-immediate-effect-subjects.v1
spi-1979-common-charts:29.61
spi-1979-errata:29.1
spi-1979-errata:29.61
spi-1979-land-rules:29.0
spi-1979-land-rules:29.1
```

Each outcome adds exactly these references:

| Outcome | Additional references, in canonical order |
| --- | --- |
| Normal | `spi-1979-land-rules:29.2` |
| Hot | `spi-1979-land-rules:29.31`; `spi-1979-land-rules:29.34` |
| Sandstorm | `spi-1979-common-charts:29.7`; `spi-1979-land-rules:29.41`; `spi-1979-land-rules:29.47`; `spi-1979-land-rules:38.5` |
| Rainstorm | `spi-1979-common-charts:29.7`; `spi-1979-land-rules:29.53` |

The complete union is sorted by `sourceId`, then `locator`; any missing, extra, duplicate, or
differently ordered reference makes event parsing/projection fail.

Campaign Observation contract 2 retains v1 top-level order and inserts `weather` immediately after
`position` and before `locations`. It is null or exactly:

```json
{
  "contractVersion": 1,
  "gameTurn": 1,
  "operationStage": 1,
  "season": "fall",
  "kind": "normal",
  "scope": "none",
  "affectedAreas": []
}
```

## Implementation tasks

### `WTH-TASK-001`: Normalize Weather rules and ruling

**Advances:** `WTH-001`-`WTH-006`, `WTH-024`

Add typed seasons/kinds/areas, corrected table rows, foul-location rows, deferred 29.41 metadata,
validation, canonical artifact/hash, the season-boundary manifest ruling, and explicit turn-111
support rejection.

**Acceptance:** `WTH-AC-001`-`WTH-003` and `WTH-AC-014` pass with byte-exact ruling-manifest
evidence, exact boundary vectors, and no turn-111 extrapolation.

**Likely files:** `src/Cna.Core/Rules`, `tests/Cna.Core.Tests/Rules` (medium).

### `WTH-TASK-002`: Generalize random metadata and correct the sequence barrier

**Advances:** `WTH-007`, `WTH-020`, `WTH-021`

Preserve byte/d6 behavior, add named Weather draw order, migrate the sequence to one Organization
position, split catalog schema 2 from the unchanged `LandSequencePosition` wire contract 2, and
update ruleset hash evidence. This task does not mutate Campaign snapshot or actor-order contracts.

**Acceptance:** existing Initiative vectors remain unchanged; new sequence tests prove Weather's
exact successor and absence of ordered Organization subpositions.

**Likely files:** `src/Cna.Core/Randomness`, `src/Cna.Core/Rules`, corresponding tests (medium).

### `WTH-TASK-003`: Add content Weather areas

**Advances:** `WTH-008`, `WTH-009`

Evolve content schema/canonical serialization/validation/hash and add synthetic assignments.

**Acceptance:** a golden locks the exact nine assignments and provenance; every malformed or
altered assignment has stable issues; permutation produces identical bytes.

**Likely files:** `src/Cna.Core/Content`, `tests/Cna.Core.Tests/Content` (split into contract and
catalog checkpoints if more than five files change).

### `WTH-TASK-004`: Add setup Weather policy and campaign admission

**Advances:** `WTH-010`-`WTH-012`, `WTH-024`

Evolve setup schema and nested creation/snapshot values while retaining their unchanged outer wire
shapes; update creation/snapshot canonical goldens for the new ruleset, setup, and content hashes;
reject missing/mismatched policy before decision or replay.

**Acceptance:** admission and replay-preparation negative matrix passes.

**Likely files:** `src/Cna.Core/Setups`, campaign setup/creation values, setup/campaign tests
(medium; split serializer migration if necessary).

### `WTH-TASK-005`: Migrate actor-order identity end to end

**Advances:** `WTH-013`

Evolve `CampaignOperationStageOrder` to `(GameTurn, OperationStage)` identity as one coherent green
Campaign migration. Update the nested value, validator, declaration projection, snapshot writer/
parser, replay preparation, and snapshot goldens together while outer snapshot contract 4 remains
unchanged. Do not add Weather state in this task.

**Acceptance:** event bytes remain unchanged from the Task-4 baseline; post-declaration snapshot
bytes intentionally change only for actor-order contract 2/Game Turn; same-pair duplicates reject;
and the actor-order half of `WTH-AC-013` round-trips two direct structural pairs without claiming
turn rollover is reachable.

**Likely files:** `src/Cna.Core/Campaigns` actor-order/snapshot/validator/serializer/projector/replay
contracts and focused golden tests (medium; one checkpoint, no split ownership).

### `WTH-TASK-006`: Implement, serialize, and replay Weather authority

**Advances:** `WTH-012`, `WTH-013`, `WTH-015`-`WTH-019`, `WTH-021`, `WTH-023`-`WTH-025`

Add Weather state, command, resolver, event factory, engine decision, projector, exact successor to
Organization, explicit `none`/`global`/`listed-areas` scope, strict event/snapshot canonical JSON,
outer snapshot contract 5, and replay evidence from reachable creation checkpoints through
Organization.

**Acceptance:** deterministic normal/hot/foul vectors, rejection-before-draw, pair-keyed duplicate
protection, forged-event recomputation, golden bytes, and the full malformed parser/projector matrix
pass. The Weather half of `WTH-AC-013` uses direct structural canonical fixtures; end-to-end two-turn
replay is deferred to the first capability that can traverse Organization and Game-Turn completion.

**Likely files:** `src/Cna.Core/Campaigns` Weather state/command/resolution/event/projection/
serialization/replay plus focused campaign tests (medium; split only after a green pair-keyed
identity checkpoint exists).

### `WTH-TASK-007`: Extend legal actions and observations

**Advances:** `WTH-014`, `WTH-022`, `WTH-023`

Expose the system candidate, submit through exact membership, return side-safe receipt/successor,
and project Campaign Observation contract 2 with the frozen nullable nested Weather contract 1,
retained top-level ruleset hash, and no Weather dice/provenance/random leakage.

**Acceptance:** `WTH-AC-005`, `009`, and `011` pass for every audience and paired hidden worlds.

**Likely files:** `src/Cna.Core/Actions`, `src/Cna.Core/Observations`, focused tests (medium).

### `WTH-TASK-008`: Reconcile docs and execute the gate

**Advances:** closure evidence and documentation reconciliation for all requirements; implements no
new authority requirement.

Update README, roadmap, `tech-design.md`, `naming-overview.md`, spec traceability, and exact contract
versions/hashes. Run focused tests, full gate, and independent review.

**Acceptance:** every requirement maps to passing evidence or explicit deferral; no P0-P2 review
finding remains.

## Checkpoints

### Owner activation checkpoint before Task 1

- `TURN-DEC-001` through `TURN-DEC-006` are approved or revised;
- the proposed Sprint 3.3-3.5 roadmap split is activated as implementation authority; and
- no retained roadmap instruction directs Weather past Organization.

### Rules checkpoint after Tasks 1-2

- normalized table/ruling and sequence decisions independently reviewed;
- prior Initiative random vectors unchanged; and
- exact ruleset hash migration recorded.

### Admission checkpoint after Tasks 3-4

- content/setup canonical migrations complete;
- absence never means zero obligations; and
- campaign creation/replay fails closed on old or altered authority.

### Campaign identity checkpoint after Task 5

- actor order is pair-keyed across value, snapshot, validation, projection, serialization, and
  existing replay;
- CampaignCreated/snapshot goldens reflect Task-4 identity changes, Task 5 changes no event bytes,
  and post-declaration snapshot bytes reflect nested actor-order contract 2 exactly; and
- structural repeated-stage fixtures pass without implying turn rollover is implemented.

### Authority checkpoint after Tasks 6-7

- every accepted Weather path is deterministic and replay-identical;
- every rejected path returns zero events/cursor change;
- hot Weather is explicitly global, normal has no affected scope, foul Weather lists exact areas;
- Observation contract 2 retains its top-level ruleset hash and exact source-free nested Weather;
  and
- campaign stops exactly at Organization.

### Completion checkpoint after Task 8

- `just check` passes;
- documentation links and contract evidence agree;
- independent review is ready; and
- roadmap marks only Weather v1 complete, not full stage entry.

## Risks and mitigations

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Errata conflict is silently “fixed” in code | Untraceable fidelity decision | Manifest ruling plus exact boundary tests |
| Weather rolls before support checks | Retry/cursor corruption | Validate policy/content/authority before constructing resolver |
| Empty synthetic policy leaks into published scenarios | Missing immediate effects | Setup-ID admission, hash, replay validation, explicit contract version |
| Fixed Organization sequence survives migration | Deletes future player ordering | Single phase barrier and catalog tests |
| Foul area identity is confused with synthetic geography | Misrepresented content | Source-neutral typed assignment, synthetic provenance, presentation notice |
| Event trusts stored outcome | Forged history | Recompute complete event from predecessor authority/random state |
| Repeating Operation Stage overwrites an earlier Game Turn | Corrupt history/replay | Key actor order, Weather, events, validation, and canonical ordering by `(GameTurn, OperationStage)`; prove structural contracts now and defer end-to-end rollover replay |
| Nested contract changes are hidden behind vague “next version” language | Broken replay/goldens | Enforce the exact migration matrix and task-specific canonical-byte assertions |
| Game Turn 111 is silently treated as Winter | Unreviewed rules invention | Explicit owner-gated deferral and rejection-before-randomness acceptance |
| Nile Delta exception is accidentally enforced from area E alone | Incorrect future effects | Retain E and a stable deferred rule reference; require a typed published subarea contract before executable enforcement |
| Weather scope grows into Movement | Oversized change | Stop at Organization and preserve explicit downstream capabilities |

## Deferred next capabilities

- `STAGE-ENTRY-001`: positive or explicitly empty Organization, Naval Convoy Arrival, and Fleet
  obligations through legal actions; stop at Reserve Designation.
- `RESERVE-001`: first-player reserve designation and completion; stop at Movement.
- `MOVE-001`: resource/capability/terrain/contact movement loop with Weather effects enforced.
