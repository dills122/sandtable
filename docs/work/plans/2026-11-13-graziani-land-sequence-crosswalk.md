# Land sequence crosswalk: six-turn scenario

**Status:** source-to-catalogue research, not an executable authority matrix or
an exhaustive inventory. This pass visually rechecked the original Land §5.2
sequence, printed pp. 11–12, in the locally retained compiled scan SHA-256
`836c14949c4fef1b066043e19a570256b22598d8ae2948d26844b949f53b60a5`.
The scan is another package of source material, not a second edition or an
independent reading. [Scenario §60.22](2026-11-12-graziani-source-register.md)
sets six turns, each with three operation stages. The actual application,
exceptions and errata for each source provision remain to be checked.

Catalogue names below refer to
[`Cna1979LandSequence`](../../../src/Cna.Core/Rules/Cna1979LandSequence.cs).
A catalogue position represents a place in the turn, not proof that its decisions,
subsegments, side effects or six-turn repetition are implemented. `A` and `B`
are relative player order for the current stage, not fixed national sides.

| Source sequence | Existing catalogue boundary | Critical obligation or unresolved applicability |
| --- | --- | --- |
| Turn initiative determination | Initiative determination | Predetermined Italian first turn; later turns need eligible ratings, draw/tie handling and replay. |
| Turn naval-convoy schedule | Naval convoy schedule | Axis cargo/routes and replacement plans; Commonwealth replacement production and two-turn planning are named in the outline. Determine exact abstract-mode and scenario applicability; do not declare empty from land reinforcement data. |
| Turn tactical shipping | Tactical shipping | Both players plan eligible cargo between ports; establish how Land-only convoy and side-specific port capacity apply. |
| Stage initiative declaration | Initiative declaration | Initiative holder chooses A or B separately each stage. |
| Stage weather determination | Weather determination | Weather and affected-area/resource consequences must match accepted rule and state; current empty-subject synthetic policy does not prove historical effects absent. |
| Organization: reorganization | Organization | Assign/attach/detach eligible units and trucks; validate owner, cost, limits and choice. |
| Organization: construction | Organization | Completion versus initiation/continuation are separate; outline allows organization segments in any chosen order. Continued/new work has a voluntary-movement restriction and reaction exception. Establish when each is reachable. |
| Organization: training | Organization | Completion versus initiation/continuation are separate; source describes voluntary-movement restriction and reaction exception. Reconcile special arrival/replacement rules and distinguish optional morale training. |
| Naval convoy arrival | Naval convoy arrival | Reinforcements, replacement points and ammo scheduled **and actually arriving** appear at designated ports or entrance hexes. Their ordering, transport and other obligations need source-backed authority; an empty land schedule alone is insufficient. |
| Commonwealth fleet assignment | Commonwealth fleet / assignment | Determine which fleet obligations remain under the accepted Land-only abstraction and scenario errata. |
| Commonwealth fleet repair | Commonwealth fleet / repair | Separate from land vehicle maintenance and temporary/major repair-facility questions. |
| A reserve designation | First-player reserve designation | Explicit owner designation and eligible units before movement/combat. |
| A movement | First-player movement | Eligible phasing units move; permitted non-phasing reaction is separate. Validate reserve and type exceptions. |
| A breakdown determination | First-player breakdown | Applicable vehicles/motorized units of both sides check; apply accepted abstract-mode exceptions. |
| A combat position, barrage, retreat, force assignment, anti-armor and close assault | Six first-player combat steps | Both sides have distinct decisions and private information; actual rules and legal no-attack paths require separate certification. |
| A reserve release | First-player reserve release | The owner may release reserves. The preceding movement, breakdown, combat and release segments repeat together when legally chosen; the catalogue is not a fixed one-pass turn. |
| A truck convoy movement | First-player truck convoy | Source names second/third-line trucks, POWs and guards; evaluate replacement by abstract motorization rules and applicability independently. |
| A Commonwealth rail movement | First-player Commonwealth rail | The outline limits this named phase to Commonwealth when that player has the A role. Errata §8.71 separately references Axis rail under §54.4, and §8.73 adds a reorganization restriction; determine applicability rather than assuming Axis rail can never occur. |
| A repair: towing and maintenance | First-player repair / towing and maintenance | The outline distinguishes towing from repair and restricts maintenance of vehicles to those not towed in that phase. Reconcile Land-only resource and repair-facility decisions. |
| A patrol | First-player patrol | Outline conditions patrol on the phasing player not having engaged in assault; define its accepted meaning and relevant history before authorizing. |
| B player phases | Second-player counterparts of reserve through patrol | Repeat source phases with A/B exchanged; preserve nationality-specific Commonwealth rail and correct ownership, rather than treating roles as fixed sides. |
| Second and third operation stages | Operation stages 2 and 3 | Source repeats all facets of the first stage. Existing bounded stage-1 execution does not establish either later stage. |
| End of game turn | End-of-turn | Source marks the turn boundary; admit rollover and any separately specified turn-end obligations. On turn 6, terminal timing and result still need accepted victory rules. |

## Executed catalogue check

[`GrazianiSequenceCatalogTests`](../../../tests/Cna.Core.Tests/Rules/GrazianiSequenceCatalogTests.cs)
checks the 672 currently catalogued positions across turns 1–6, all 18 stage
preludes and relative-player reserve roles, and the catalogue's stage and turn
successors. In particular, the generic catalogue continues to turn 7 after turn
6; scenario terminalization must be a separate admitted authority rule. This test
does not run a campaign, execute the repeated Combat loop, establish no-op
obligations or certify the source completeness of the catalogue.

## Evidence next needed

1. Cross-check each row against referenced detailed cases and errata, including
   changed applicability under §32 Land-only abstraction. Mark source-proven
   empty positions separately from not-yet-modelled or unknown positions.
2. For each decision or obligation, identify actor, timing, preconditions,
   meaningful choices, state/events, source/ruling identity and private data.
   Organization's discretionary segment order and repeatable combat mean a
   simple count of catalogue positions cannot be the requirement count.
3. Bind each row to exact runtime profile and tests, or mark explicitly unsupported
   or unresolved. Prove turn/stage transitions and repeat paths rather than
   multiplying an initial-stage test by 18.
4. Independently review the source readings, map the full scenario content and
   settle rights before accepting `MVP-GATE-01` or admitting a historical setup.
