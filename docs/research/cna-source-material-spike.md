# Campaign for North Africa Source-Material Spike

**Status:** Approved baseline; implementation active

**Date:** 2026-08-15

**Decision owner:** Project owner

## Executive conclusion

Sandtable should implement the original SPI edition of *The Campaign for North Africa: The Desert
War, 1940-43*, using the 1979 rules and the September 1979 errata as its initial authority. The
published Land Game and six-turn `Graziani's Offensive` scenario provide the shortest defensible
route to a working adaptation without inventing a simplified ruleset.

Community errata-integrated PDFs, tracking sheets, play reports, and the VASSAL module are valuable
comparison oracles. They are not authoritative rules. The currently advertised Decision Games
redesign deliberately changes the original procedures and must not be mixed into the baseline.

The original scans, map art, counter art, and rule text do not carry an identified redistribution
license. Sandtable should retain provenance and derived factual data, write original code and user
interface assets, and keep source scans outside Git. Public or commercial distribution requires a
separate rights review.

## Question and scope

The spike asked:

> What authoritative rules baseline and implementation sequence let Sandtable become a faithful,
> demonstrable pre-alpha quickly without prematurely generalizing the engine or inventing mechanics?

The work covered publication identity, rules, errata, scenarios, charts, log sheets, community
tools, digital adaptations, source gaps, and delivery implications. It did not implement mechanics,
copy game assets into the repository, or make a legal determination about intellectual-property
ownership.

## Method and source hierarchy

Sources were assessed in this order:

1. Original publisher/designer material, rules, components, and official errata.
2. Preserved scans with identifiable publication provenance.
3. Contemporary publications and reputable catalog records.
4. Community aids and play records, used only for corroboration and risk discovery.

The image-only rules, scenario, chart, and log-sheet scans were rendered and visually inspected.
The OCR-enabled errata was also text-extracted. Temporary copies remained outside the repository.

## Recommended authority policy

Apply sources in this precedence order:

1. An adopted Sandtable ruling for a specifically recorded ambiguity.
2. September 1979 errata.
3. Original 1979 rules, scenario booklet, charts, and component data.
4. Designer commentary where it clearly resolves intent without contradicting higher authority.
5. Community interpretation only after an explicit Sandtable ruling.

Every normalized rule, table, scenario datum, and ruling should carry a stable source reference,
for example `CNA1979:8.22`, `CNA1979:60.22`, or `CNA1979-ERRATA:8.37`. A ruling must record the
conflict, alternatives considered, chosen behavior, and tests protecting that behavior. Source
references identify provenance; they must not embed copied rules prose.

## Evidence

### Documented facts

- The original rules divide the game into a Land Game, an optional detailed Air Game, an optional
  detailed Logistics Game, and five scenario groups. Land can be played alone.
- A complete weekly Game Turn contains three Operation Stages. Within a stage, players perform
  movement and combat in initiative order, with repeatable movement/combat segments rather than a
  single move-then-fight pass.
- The six-turn `Graziani's Offensive` scenario begins at Game Turn 1 / Operation Stage 1 and ends
  after Game Turn 6 / Operation Stage 3. The original estimate is 25-50 hours of physical play.
- The scenario has distinct strategic, decisive, and tactical victory conditions for both sides
  and includes published adjustments for Land-only play.
- The September 1979 errata corrects rules, map data, tables, order-of-arrival information, and
  scenario setups. Some corrections explicitly prefer a chart over prose or prose over a chart.
- The same errata warns that the abstract rules were not tested. Published Land-only support is
  therefore an authentic mode, but it is not evidence of balance or defect-free behavior.
- The preserved set includes Land rules, Air/Logistics rules, scenarios, common and side-specific
  charts, log sheets, counters, and maps. A separately cataloged Historical Background booklet was
  not found as an accessible scan.
- A maintained VASSAL module reports that it incorporates the September 1979 errata into relevant
  maps, counters, charts, and tables.
- Decision Games currently advertises a distinct redesign in its design stage. Its description
  says paperwork and data tracking have been removed and procedures changed.

### Observations

- The charts supply adjudication inputs, not merely summaries: combat tables, capability costs,
  unit characteristics, organization/arrival data, production, and shipping are among them.
- The physical log sheets expose long-lived domain concepts for organization, supply, transport,
  repair, replacements, air/naval missions, and requisitions.
- Interleaved movement/combat, Capability Point accounting, cohesion, and simultaneous anti-armor
  resolution are higher-risk architectural seams than basic turn advancement.
- Community players continue to report ambiguous and contradictory cases. Software cannot defer
  those cases to informal table judgment; it needs a versioned ruling ledger.

### Inferences

- Land-first is a fidelity-preserving increment because it follows a published mode of play.
- `Graziani's Offensive` is the best initial scenario target because it is the shortest published
  scenario and provides complete setup and victory boundaries.
- A smaller, nonhistorical rules-laboratory fixture should precede full scenario ingestion. It can
  prove rules architecture quickly without misrepresenting partial mechanics as a playable edition.
- The authoritative engine needs hierarchical phase identifiers, commands, events, replay, and
  source citations before large-scale map or order-of-battle transcription begins.
- Air and Logistics should be future capability layers over the same campaign state, not separate
  games or services.

### Unknowns

- Whether a later official or designer-sanctioned errata sheet supersedes September 1979.
- Whether the project owner has a physical copy or additional source material, particularly the
  Historical Background booklet.
- What rights or permissions apply to the original name, scans, maps, counter art, and a commercial
  derivative implementation.
- How defects in the published abstract Air/Logistics rules affect scenario balance.

## Source inventory

| Material | Provenance and use | Assessment |
|----------|--------------------|------------|
| [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) | Preserved original SPI scan | Primary baseline |
| [Air and Logistics rules](https://spigames.net/PDFv10/CNA_AirGameRules.pdf) | Preserved original SPI scan | Primary future-layer baseline |
| [Scenarios](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf) | Preserved original SPI scan | Primary scenario baseline |
| [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf) | Period correction sheet | Primary correction authority |
| [Common charts](https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf) | Preserved original SPI scan | Required adjudication data |
| [Commonwealth charts](https://spigames.net/PDFv10/CNA_ChartsCWPlayer.pdf) | Preserved original SPI scan | Required side data |
| [Axis charts](https://spigames.net/PDFv10/CNA_ChartsAxisPlayer.pdf) | Preserved original SPI scan | Required side data |
| [Log sheets](https://spigames.net/PDFv10/CNA_Charts_LogSheets.pdf) | Preserved original SPI scan | State-model evidence |
| [SPI preservation index](https://spigames.net/rules_downloads.htm) | Collection and component discovery | Useful index; no reuse license identified |
| [Contemporary game profile](https://www.spigames.net/MovesScans/Moves49/CNAGPM49.pdf) | 1979 *Moves* profile | Design context, not rules authority |
| [Community rules and trackers](https://friendorfoe.com/war/cfna/) | Errata-applied PDFs, tracking sheets, interpretations, play logs | Reconciliation and comparison only |
| [VASSAL module](https://vassalengine.org/wiki_old/wiki/Module%3AThe_Campaign_for_North_Africa%3A_The_Desert_War_1940-43) | Community digital board and setup | Comparison oracle, not rules engine |
| [Current Decision Games redesign](https://shop.decisiongames.com/ProductDetails.asp?ProductCode=P3034) | Current publisher product page | Explicitly out of baseline |
| [Historical Background catalog record](https://www.nobleknight.com/P/2148003114/Campaign-for-North-Africa---Historical-Background-Book) | Physical-component catalog | Confirms source gap |

## Options considered

| Option | Fidelity | Delivery speed | Principal risk | Decision |
|--------|----------|----------------|----------------|----------|
| Original 1979 rules plus September errata | Highest defensible baseline | Moderate | Ambiguities require rulings | **Recommend** |
| Community errata-integrated PDFs as authority | High apparent convenience | Faster transcription | Silent editorial changes and unclear authority | Use only to compare |
| Current Decision Games redesign | Different product intent | Unknown | Mixes streamlined mechanics into the original | Reject for this project |
| Generic North Africa engine inspired by CNA | Low | Fast initially | Cannot claim faithful adaptation | Reject |

## Recommendation and consequences

Adopt the original 1979 rules plus September 1979 errata as baseline `cna-1979.1`. Begin with a
rules laboratory, then build toward the Land-only `Graziani's Offensive` scenario. Label known
abstract-mode uncertainty instead of repairing it silently.

Implementation consequences:

- Define command, event, snapshot, ruleset-manifest, source-reference, and ruling schemas first.
- Store normalized tables and scenarios as versioned typed data with provenance and validation.
- Keep map art, counter art, and scans out of Git; use original placeholder visuals until rights
  are settled.
- Treat event replay, seeded randomness, and legal-action generation as MVP requirements.
- Add Air and detailed Logistics only after the Land scenario reaches its fidelity gate.
- Do not train or validate intelligence behavior against hidden opposing state.

## Adopted project decision

The project owner approved:

1. Baseline `cna-1979.1`: original 1979 material plus September 1979 errata.
2. `Graziani's Offensive` Land-only as the first playable scenario target.
3. Original placeholder visuals and external-only source scans pending a rights review.

The proposed delivery sequence is in [Pre-alpha roadmap](../roadmap/pre-alpha-roadmap.md).
Source gaps, ambiguity rulings, and distribution rights remain explicit gates as their affected
content enters implementation; they do not reopen the adopted baseline by default.
