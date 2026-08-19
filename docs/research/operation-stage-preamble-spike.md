# Operation-Stage Preamble Source and Contract Spike

**Status:** Decision-ready; owner approval required

**Date:** 2026-08-18

**Decision owner:** Project owner

**Rules target:** `cna-1979.1`

**Roadmap capability:** `TURN-001A`

## Executive conclusion

The next authoritative implementation should be **Weather Determination v1**, but it must stop at
one source-correct Organization Phase barrier. It must not jump from Weather to Reserve
Designation or Movement.

Weather v1 needs three foundations that are absent today:

1. a source-normalized, versioned Weather Table and Foul Weather Location Table;
2. source-neutral content assignments from synthetic locations to typed weather areas; and
3. an explicit setup policy proving that the supported synthetic checkpoints contain no stored
   fuel/water, depleted wells, or grounded aircraft requiring immediate Weather mutations.

The current sequence catalog also needs correction. Published Organization segments may occur in
player-chosen order, so the catalog must represent Organization as one decision barrier rather than
as a fixed sequence of Reorganization, Construction, and Training positions. Later positive
Organization actions can execute while the campaign remains at that barrier.

After Weather v1, a separate `STAGE-ENTRY-001` capability should handle Organization, Naval Convoy
Arrival, and Commonwealth Fleet obligations. It may use explicit no-obligation fixture policy where
supported, but it must stop at first-player Reserve Designation because Reserve selection is a real
side decision. `RESERVE-001` then becomes the final prerequisite to Movement.

The broader roadmap Task 3.2 is not a prerequisite for Weather at the two current synthetic
checkpoints: `ACTION-001` already resolved their opening convoy cases and retained Operation Stage 1
initiative order. General convoy and every-stage declaration work is deferred and becomes blocking
only before a later capability admits checkpoints that depend on it.

## Decision question

> What is the smallest source-faithful capability after Legal Actions v1 that resolves Weather,
> preserves deterministic replay and fog boundaries, and advances toward player execution without
> skipping mandatory rules or inventing empty obligations?

The answer matters now because Legal Actions v1 ends at Operation Stage 1 Weather Determination,
while the roadmap currently compresses Weather and several later phases into one “stage entry”
task.

## Scope and stop condition

This spike covers:

- the Land sequence from Weather Determination through first-player Reserve Designation;
- Weather timing, season selection, d66 lookup, foul-weather area selection, and immediate effects;
- the September 1979 Weather errata conflict;
- current sequence, content, world, setup, observation, legal-action, randomness, event, and replay
  boundaries; and
- implementation options and a testable Weather v1 contract.

It does not implement code, copy source scans into Git, normalize positive Organization/Fleet/
Reserve rules, implement general Naval Convoy, or begin Movement.

The stop condition is a decision-ready research packet, proposed specification, technical design,
traceability view, and owner decision gate.

## Source hierarchy and method

Sources were assessed under the approved repository policy:

1. adopted Sandtable rulings;
2. September 1979 errata;
3. original 1979 Land rules and common charts; and
4. community errata-integrated material only as a comparison oracle.

Temporary copies of the image-only Land rules and common charts were kept outside Git, rendered,
and visually inspected. The OCR-enabled errata was text-searched and visually checked. Repository
contracts and tests were compared against those source findings. No source scan or copied rules
prose is retained here.

## Documented facts

### Weather procedure

| Fact | Stable source reference |
| --- | --- |
| Weather is determined separately in every Operation Stage. | `spi-1979-land-rules:29.0` |
| The initiative holder rolls two dice and reads them sequentially as d66, not as a sum. | `spi-1979-land-rules:29.1` |
| The selected season row maps d66 to normal, hot, sandstorm, or rainstorm. | `spi-1979-common-charts:29.61` |
| Normal weather has no listed weather effects. | `spi-1979-land-rules:29.2` |
| Sandstorm or rainstorm requires one additional d6 to select affected map sections. | `spi-1979-land-rules:29.1`; `spi-1979-common-charts:29.7` |
| Unaffected sections have normal weather; specific off-map areas are never affected. | `spi-1979-land-rules:29.1` |
| Hot weather occurs across all game-map sections, so its spatial scope is global rather than an empty affected-area result. | `spi-1979-land-rules:29.31` |
| A sandstorm does not affect Nile Delta hexes even when its location result includes section E. | `spi-1979-land-rules:29.41`; `spi-1979-common-charts:29.7` |
| Hot weather reduces stored fuel and water by five percent during Weather Determination. | `spi-1979-land-rules:29.34` |
| Rainstorms restore depleted wells immediately. | `spi-1979-land-rules:29.53` |
| Sandstorms can damage grounded aircraft. | `spi-1979-land-rules:29.47`; `spi-1979-land-rules:38.5` |

The minimum normalized seasonal mapping, after the recommended ruling below, is:

| Season | Game Turns | Normal | Hot | Sandstorm | Rainstorm |
| --- | --- | --- | --- | --- | --- |
| Fall | `1-12`, `49-60`, `97-108` | `11-35` | `36-54` | `55-61` | `62-66` |
| Winter | `13-24`, `61-72`, `109-110` | `11-52` | none | none | `53-66` |
| Spring | `25-36`, `73-84` | `11-42` | `43-55` | `56-64` | `65-66` |
| Summer | `37-48`, `85-96` | `11-23` | `24-55` | `56-66` | none |

The table preserves each published season's probability row and corrects the Game-Turn ranges
assigned to that season.

The published chart ends at Game Turn 110 even though the rules describe a 111-Game-Turn campaign
and require Weather in every Operation Stage. No retained primary source explains whether Game Turn
111 omits Operations Stages, inherits Winter, or was accidentally omitted from the chart. Extending
Winter to 111 would therefore be a new rules ruling, not normalization. `WEATHER-001` deliberately
supports only Game Turns 1-110 and rejects 111 before randomness until a later full-campaign source
decision resolves the gap. This scoped deferral is owner-visible in `TURN-DEC-006` and must not be
mistaken for a claim that the source campaign has no Weather on turn 111.

The foul-weather location mapping is:

| Location d6 | Affected weather areas |
| --- | --- |
| `1` | A, B |
| `2` | C, D |
| `3` | D, E, subject to the Nile Delta sandstorm exception |
| `4` | B, C |
| `5` | B, D |
| `6` | B, C, D |

The source's Nile Delta sandstorm exception cannot be executed from an A-E weather-area assignment
alone: it identifies a subarea within section E, while current synthetic content deliberately has no
published-map subarea identity. Weather v1 therefore retains E in the location result and records
stable deferred reference `spi-1979-land-rules:29.41`; it does not infer a Delta selector or enforce
positive area effects. A later published-content/effect capability must normalize and test that
subarea contract before enforcement.

### Post-Weather sequence

| Fact | Stable source reference |
| --- | --- |
| Organization follows Weather. | `spi-1979-land-rules:5.2` |
| Reorganization, Construction, and Training segments may occur in any order selected by the players. | `spi-1979-land-rules:5.2.organization` |
| Naval Convoy Arrival follows Organization and applies scheduled arrivals. | `spi-1979-land-rules:5.2.naval-convoy-arrival`; `spi-1979-land-rules:20.21` |
| Commonwealth Fleet Assignment and Fleet Repair follow Naval Convoy Arrival. | `spi-1979-land-rules:5.2.commonwealth-fleet` |
| Player A then designates reserves before Movement and Combat. | `spi-1979-land-rules:5.2.reserve-designation` |

Organization is therefore not a deterministic ordered checklist, Fleet Assignment is not a system
choice, and Reserve Designation cannot be deleted merely to reach Movement.

## The Weather errata conflict

The September 1979 errata contains adjacent incompatible statements:

- `29.1` says Spring begins in March week III and ends in June week II; and
- `29.61` says the chart is backwards, refers back to `29.1`, but gives March week IV through June
  week III as its parenthetical example.

The original Land rules, Game Turn 1 example, and twelve-week table blocks align with the first
statement: Game Turn 1 is September III, so Fall occupies Game Turns 1-12 and the subsequent seasons
follow in twelve-week blocks. The second parenthetical would require shifted ranges not printed in
the chart and contradicts the provision it claims to summarize. The community errata-integrated
rules retain both statements and do not resolve the conflict.

### Proposed ruling

- Ruling ID: `cna-1979.1.ruling.weather-season-boundary`
- Conflict ID: `cna-1979.1.conflict.weather-season-boundary`
- Alternative: `use-rule-29.1-boundaries-and-remap-chart-game-turns`
- Alternative: `use-errata-29.61-parenthetical-and-derive-shifted-ranges`
- Recommended behavior: `use-rule-29.1-boundaries-and-remap-chart-game-turns`
- Protecting acceptance IDs, ordinally sorted: `WTH-AC-001`, `WTH-AC-002`, `WTH-AC-004`
- Canonical sources, sorted by source ID then locator:
  - `spi-1979-common-charts:29.61`
  - `spi-1979-errata:29.1`
  - `spi-1979-errata:29.61`
  - `spi-1979-land-rules:29.0-29.1`

This preserves the explicit 29.1 boundaries, each season's published outcome probabilities, Game
Turn 1's September III date, and the chart's twelve-week blocks. Evidence that a later official
correction intended shifted one-week boundaries would reopen the ruling.

## Repository observations

- Legal Actions v1 stops at Weather with a retained initiative holder and exact Stage 1 actor order.
- `SandtableRandom` already supplies a versioned d6 stream, but the normalized random artifact names
  only Initiative draw order and has no Weather procedure entry.
- Campaign state has no per-stage Weather record.
- `CampaignOperationStageOrder` identifies only Operation Stage 1-3 even though those stages repeat
  every Game Turn. Weather and retained actor order must use `(GameTurn, OperationStage)` identity
  so later turns cannot collide with earlier records.
- Content locations have terrain and optional source coordinates, but no typed Weather-area
  assignment.
- Content/world schemas have no closed collection of stored fuel/water, wells, or grounded aircraft.
  Their absence therefore cannot silently prove zero immediate Weather effects.
- The two synthetic scenarios begin at Game Turns 1 and 43, which exercise Fall and Summer after
  the proposed corrected season mapping.
- The sequence catalog currently encodes Organization subsegments in a fixed order even though the
  source lets players choose their order.
- Campaign Observation contains public turn/order facts but no resolved Weather value.

### Synthetic Weather-area fixture decision

The current nine-location content pack needs deterministic Weather areas for contract and replay
testing, but those locations are intentionally nonhistorical. Assign A-E cyclically over the
existing ordinal location-ID order and freeze content-origin kind `synthetic` plus one
location-specific `sandtable-rules-lab` reference per row. The source index separately retains
content-source kind `repository-synthetic`:

| Location ID | Weather area | Stable fixture reference |
| --- | --- | --- |
| `center` | A | `sandtable-rules-lab:weather-areas.v1:center` |
| `east` | B | `sandtable-rules-lab:weather-areas.v1:east` |
| `north` | C | `sandtable-rules-lab:weather-areas.v1:north` |
| `north-east` | D | `sandtable-rules-lab:weather-areas.v1:north-east` |
| `north-west` | E | `sandtable-rules-lab:weather-areas.v1:north-west` |
| `south` | A | `sandtable-rules-lab:weather-areas.v1:south` |
| `south-east` | B | `sandtable-rules-lab:weather-areas.v1:south-east` |
| `south-west` | C | `sandtable-rules-lab:weather-areas.v1:south-west` |
| `west` | D | `sandtable-rules-lab:weather-areas.v1:west` |

This mechanical mapping exercises all five areas, affected and unaffected locations, and the D/E
branch without implying coordinates or correspondence to the published map. It is frozen fixture
data rather than a seventh owner ruling.

## Options considered

| Option | Result | Decision |
| --- | --- | --- |
| Jump from Weather directly to Reserve or Movement | Fastest visible progress, but skips mandatory phases and deletes choices. | Reject |
| Roll Weather without representing immediate-effect subjects or affected areas | Produces an outcome that later rules cannot apply or replay faithfully. | Reject |
| Implement Weather, all Organization/Fleet/Reserve rules, and Movement entry together | Source-faithful in principle but too broad to specify, test, or review as one change. | Reject |
| Implement Weather with typed areas, explicit empty immediate-effect policy, deterministic event/replay, and stop at Organization | Smallest complete authoritative mechanic; exposes remaining boundaries honestly. | **Recommend** |

## Recommended capability graph

```text
Legal Actions v1 (complete)
        |
        v
WEATHER-001
  normalized tables + ruling
  typed weather areas
  explicit immediate-effect-subject policy
  deterministic command/event/replay
        |
        v
Organization decision barrier
        |
        v
STAGE-ENTRY-001
  Organization / Convoy Arrival / Fleet
  positive choices or explicit admitted emptiness
        |
        v
RESERVE-001
  first-player reserve choices
        |
        v
MOVE-001
```

## Recommended decisions

| ID | Decision | Status |
| --- | --- | --- |
| `TURN-DEC-001` | Adopt the proposed season-boundary ruling and corrected Game-Turn mapping. | Owner approval required |
| `TURN-DEC-002` | Scope Weather as its own vertical slice and stop at Organization. | Owner approval required |
| `TURN-DEC-003` | Require exact setup-hashed admission for empty immediate Weather-effect subjects. | Owner approval required |
| `TURN-DEC-004` | Replace fixed ordered Organization subpositions with one decision barrier. | Owner approval required |
| `TURN-DEC-005` | Expose Weather resolution as a trusted system action while retaining the initiative holder as the recorded determining side. | Owner approval required |
| `TURN-DEC-006` | Keep Game Turn 111 explicitly unsupported in Weather v1 until a full-campaign ruling resolves the source chart's omission. | Owner approval required |

## Implementation consequences

- Add a versioned Weather rules artifact and the proposed ruling to the ruleset manifest.
- Treat Game Turn 111 rejection as an owner-approved capability support boundary, not as a second
  canonical ruleset ruling while source behavior remains unresolved.
- Generalize the random-procedure artifact so Weather draw order is normalized beside Initiative.
- Evolve the Land sequence contract to one Organization barrier; do not retain ordered positive
  Organization substeps as sequence positions.
- Add typed Weather-area assignments to content and include them in canonical bytes, validation,
  and content hash.
- Add a setup-hashed policy that admits no immediate Weather-effect subjects only for the current
  synthetic scenarios. Missing policy or future positive subjects remain unsupported.
- Evolve retained stage order and add Weather state keyed by `(GameTurn, OperationStage)`, plus one
  mechanic-specific system action, command, event, strict serialization, recomputed projection,
  and deterministic replay.
- Expose resolved Weather as source-free public observation data. Do not expose seed, cursor, future
  draws, rules provenance, or complete authority.
- Retain the 29.41 Nile Delta exception as explicitly deferred rules metadata; do not synthesize a
  published subarea identity or execute the exception in Weather v1.
- Stop at Organization. Do not add generic completion or auto-resolve later phases in Weather v1.
- Revise roadmap Sprint 3 after owner approval so Weather, stage-entry obligations, and Reserve are
  separate gates before Movement.

## Confidence, limitations, and unknowns

Confidence is high for sequence order, d66 semantics, table values, the location branch, immediate
hot/rain effects, and the need for an Organization barrier. Confidence is medium for the proposed
season ruling because the primary errata contradicts itself; the recommendation is the most
internally consistent interpretation, not an unambiguous printed correction.

Confidence is high that the printed Weather table itself stops at Game Turn 110 and that the
campaign is described as 111 turns. The intended turn-111 Weather behavior is unknown; this package
defers rather than invents it.

Positive Organization, Fleet, Reserve, resource, well, and grounded-aircraft contracts remain
future source work. Weather v1's explicit empty-subject policy must not be generalized to a
published scenario. Executable Nile Delta semantics likewise remain future work until published
content can represent the source subarea rather than only section E.

## Required owner decisions and next gate

Approve, revise, or reject:

1. the proposed Weather season-boundary ruling;
2. Weather v1 stopping at Organization;
3. exact setup admission for empty immediate-effect subjects;
4. correcting Organization to one decision barrier;
5. Weather as a trusted system action with retained initiative-holder attribution;
6. explicit rejection of Game Turn 111 pending a later full-campaign ruling; and
7. splitting `STAGE-ENTRY-001` and `RESERVE-001` from Weather before Movement.

After approval, the proposed [Weather Determination v1 specification](../specs/weather-determination-v1.md),
[technical design](../design/weather-determination-v1.md), and proposed roadmap Tasks 3.3-3.5 become
implementation authority together. Implementation must not begin while any retained roadmap path
still directs Weather past Organization.

## Sources

- [Original Land Rules scan](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
- [Original common charts scan](https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf)
- [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
- [Community errata-integrated Land rules](https://friendorfoe.com/d/CfNA/CNA%20-%20Rules%20Land.pdf)
- [Approved source-material baseline](cna-source-material-spike.md)
- [Turn-preamble action boundary](turn-preamble-action-boundary-spike.md)
