# Graziani source register: six-turn scenario obligations

**November 16 follow-up:** [seam, supply and truck source recheck](2026-11-16-graziani-seam-supply-review.md)
preserves additional attributed readings and the 11 distance-chart cells. This
does not certify seam topology, blind independence, or scenario admission.

**Victory supply follow-up:** [Land-only source review](2026-11-16-graziani-victory-supply-source-review.md)
compares competing interpretations for LA-05 and VI-01–06. Motorization and a
Truck Convoy phase persist in §32; the victory-route policy remains unresolved.

**Later research:** The [current research queue](2026-11-17-graziani-remaining-source-research.md)
and its linked source checks supersede this register's historical progress
notes. They distinguish completed readings, planning rulings and open admission
gates; this original register is not a live completion count.

**Status:** initial research register, not an accepted scenario contract or
MVP-GATE-01 freeze. Baseline `0f8a828a`; scope limited to the rows below.
A [fresh-context double entry](graziani-source-review/review-disposition.md)
checked 11 grouped source rows, not the entire register or any runtime ruling.
Sources and remaining families are in the [surface inventory](../../research/2026-10-18-graziani-six-turn-surface-inventory.md)
and [release map](2026-11-12-graziani-playable-rc-inventory.md).

## Method and status

`Source read` means the cited text or image was inspected for this packet;
`retained reading` means this packet relies on the earlier source inventory, not
a new independent inspection. OCR and community transcriptions are discovery or
comparison aids, not replacement authority. Runtime notes identify bounded evidence,
not scenario admission. Counts below cover register rows only; they are neither
the number of all scenario requirements nor an unsupported-mechanic total.

Primary references: [original scenario scan](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf)
printed pp. 26–29, [original Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
§§5.2, 7 and [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
§§4.44b, 32.0. The original scenario scan was rendered and visually checked at
printed pp. 26–29 for §§59.6, 60.22, 60.31, 60.44, 60.6, 60.81 and 60.92.
The retrieved scan's SHA-256 was
`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`.
[OCR scenario transcription](https://www.scribd.com/document/1068442062/The-Campaign-for-North-Africa-text)
was used as a discovery aid; its columns interleave. A
[community integrated Land transcription](https://friendorfoe.com/d/CfNA/CNA%20-%20Rules%20Land.pdf)
was used for comparison. The preserved original Land scan was also rendered and
visually checked at printed pp. 11–13 (§§5.1–5.2, 7.11–7.16) and p. 35 (§22.31) in this packet;
no scan was added to Git. This is a single-reader check, not independent double entry.

## Initial obligation rows

| ID | Source, scope and obligation | Evidence / current runtime boundary | Outstanding evidence |
| --- | --- | --- | --- |
| TI-01 | Scenario §60.22; both sides: start turn 1/stage 1; finish after turn 6/stage 3. | Original scenario p. 27 visually checked; 6 turns and 18 stages. Newly located [Map D turn track](2026-11-12-graziani-map-identity-pilot.md#map-side-calendar-cross-check) corroborates the turn-6/stage-3 ending. | Independently check exact terminal timing; no admitted six-turn lifecycle or victory event. Printed victory cross-reference needs reconciliation in VI-06. |
| TI-02 | Land §5.2; every stage: two acting-side blocks in order, with repeatable Movement/Combat. | Original Land pp. 11–12 visually checked; 36 side blocks. The repeat comprises Movement, Breakdown, Combat and Reserve Release; the second player then performs the side phases. Catalogue contains roles, not fully executable scenario. | Reconcile individual positions, exceptions and repetitions; prove transitions, no silent skipped obligations and stage/turn rollover. |
| TI-03 | Scenario §60.6; turn 1: Italian side holds initiative throughout the turn. | Original scenario p. 28 visually checked; [initiative specification](../../specs/initiative-determination.md) `INIT-002` covers predetermined holder without a draw in its bounded contract. | Verify scenario setup mapping; demonstrate all three declarations and replay in scenario. |
| TI-04 | Scenario §60.6; turns 2–6: determine holder by normal rules. | Original scenario p. 28 visually checked; five later determinations, not preselected winners. | Prove actual turn progression, applicable ratings for every turn and persisted outcomes; bounded synthetic initiative evidence is not scenario proof. |
| TI-05 | Land §§7.13–7.14: each side rolls, adds its date-dependent rating; higher total wins, complete tied contests reroll. | Original Land p. 13 and common chart p. 2 visually checked: Commonwealth turns 1–6 use rating 3; Axis rating depends on qualifying presence. `INIT-002/007` covers the bounded resolution shape. | Independently verify scenario presence facts and source chart; prove seeded draws, ties, cursor and replay in admitted histories. |
| TI-06 | Land §§7.11–7.12, 7.16: holder chooses first or last separately each stage; acting roles follow that choice. | Original Land p. 13 visually checked; holder and actor are separate in `INIT-010`; roadmap retains later-stage declaration gaps. | Test both choices and both holders at each stage, including adjacent activations across stage boundaries, recovery and side-safe actions. |
| SQ-01 | Land §5.2 Organization: reorganization, construction and training segments may be undertaken in any chosen order. | Original Land p. 12 visually rechecked; catalogue currently identifies Organization as one phase. | Establish actors, applicable actions and order-sensitive effects for the scenario; prove legal alternatives and replay rather than imposing an undocumented fixed sequence. |
| SQ-02 | Land §5.2 Organization construction: completion and initiation/continuation have distinct timing and movement implications. | Original Land p. 12 visually rechecked; beginning or continuing work restricts voluntary movement with a Reaction exception. | Source detailed construction rules, eligible projects, CP/resource costs, completion and interruption; test both movement restriction and exception. |
| SQ-03 | Land §5.2 Organization training: completion and initiation/continuation are distinct. | Original Land p. 12 visually rechecked; starting or continuing training restricts voluntary movement with a Reaction exception. | Reconcile specific training and arriving-unit rules, choice, timing, morale, exceptions and actual scenario reachability. |
| SQ-04 | Land §5.2 Naval Convoy Arrival: scheduled reinforcements, replacement points and ammo points that actually arrive appear at designated ports or entrance hexes. | Original Land p. 12 visually rechecked; current explicit-none event is bounded and does not establish those categories empty. | Verify abstract-mode and scenario applicability, placement, loss/timing, mixed obligations, side privacy and deterministic replay. |
| SQ-05 | Land §5.2 Commonwealth rail phase: rail is available when the acting player is Commonwealth; errata §§8.71, 8.73 add Axis-use and reorganization qualifications. | Original Land p. 12 and September errata p. 1 visually rechecked; catalogue contains a rail position in each relative player block. [Rail research](2026-11-13-graziani-rail-scope.md) separates both physical lines, construction and Axis stock. Land-only applicability is not settled. | Prove actor-specific phase and any separately authorized Axis rail, cargo, capacity, route and Land-only interactions; apply the errata's stage-specific reorganization restriction if applicable. |
| SQ-06 | Land §5.2 Repair: towing and maintenance are separate, and the outline limits maintenance to vehicles not towed in that phase. | Original Land p. 12 visually rechecked; repair facility type and abstract resource rules remain separate source conflicts. | Reconcile detailed towing/maintenance exceptions, phase history, eligible resources, owner choices and replay; do not infer facility existence from this timing alone. |
| SQ-07 | Land §5.2 Patrol: the phasing player may use patrols for reconnaissance if that player has not engaged in assault; errata §16.11 adds eligible unit types. | Original Land p. 12 and errata p. 2 visually rechecked; [patrol/fog matrix](2026-11-13-graziani-patrol-fog-matrix.md) records source timing, eligibility and report questions. Scenario reachability and precise assault-history scope are not proved. | Resolve qualifying history, types present in scenario, patrol conditions and information disclosure; test both sides, boundary conditions and replay. |
| LA-01 | Scenario §§59.6, 60.9, 60.92: Land-only uses general abstract rules plus Group One adjustments. | Original scenario pp. 26, 28–29 visually checked; errata §32.0 warns abstract rules were untested. | Enumerate referenced abstract Land/logistics/air procedures and every affected phase; do not infer that excluded detailed modules remove all effects. |
| LA-02 | §60.92(A): each side's incoming motorization allowance tracks medium-truck arrival capability one-for-one. | Original scenario p. 28 visually checked; applies to both sides. | Verify six-turn schedules; define timing, allocations, event identity and interactions with initial trucks. |
| LA-03 | §60.92(B): replace Axis §60.34 initial supply with Land-only supply-unit setup. | Original scenario pp. 28–29 visually checked; bounded [supply pilot](2026-11-12-graziani-supply-accounting-pilot.md) records candidate data, not admitted content. | Independently reconcile all locations, quantities, contents, dummy units and placement choices; errata §59.45 and rights review; prove no double-count. |
| LA-04 | §60.92(C): replace Commonwealth §60.44 with Land-only initial supply-unit setup. | Original scenario p. 29 visually checked; bounded supply pilot records candidate data. | Independently reconcile all rows, choices and dummy units; review working repair decision below. |
| LA-05 | §§59.6, 60.92 and 60.81: abstract supply and victory must compose. | Source inventory flags supply-trace ambiguity; a [community interpretation](https://basmith.net/cna/rules/scenarios/10-the-italians) proposes a route-based test. | Derive original-rule alternatives; obtain explicit project ruling and tests before applying the community interpretation. |
| LA-06 | §60.44 and §60.92(C): decide effect on Commonwealth initial repair facilities. | Original scenario pp. 28–29 and Land §22.31 visually checked: general major facilities are separately stated, unlike scenario-designated temporary facilities. See analysis below. | Independent source check; resolve temporary-facility ambiguity and reconcile general major facilities before implementation. |
| LA-07 | §59.63(A): initial attached motorization derives from first-line trucks at each setup location; scenario-specific modifications take precedence. | Original scenario p. 26 visually checked: medium and heavy points contribute, unlike the incoming allowance in LA-02. | Enumerate per-hex quantities, allocation choices and tests for light/medium/heavy inputs. |
| LA-08 | §59.63(B): initial unattached motorization derives from second/third-line trucks; air-facility trucks are excluded when Air is abstracted. | Original scenario p. 26 visually checked; placement affects potential extra initial loads. | Verify applicability of every truck category; distinguish assigned initial points from later arrivals. |
| LA-09 | §§59.64, 59.66(B): attachment distributions can change under Land rules, subject to the restriction on certain initially loaded attached points. | Original scenario p. 26 visually checked; initial extra loads are not ordinary supply-dump totals. | Independently verify conditions, consumption priority, transfer and detachment constraints; specify event/replay and conservation vectors. |
| LA-10 | §59.66(C): eligible unattached points can begin with additional supply units subject to grouping and location conditions. | Original scenario p. 26 visually checked; grouping thresholds and air-facility treatment differ. §59.66(C) treats Tripoli/Tunisia boxes as hexes for that case. | Independently verify Land-only applicability, thresholds, remainders and cross-pool placement; test below/at/above boundaries and do not generalize the box rule to other cases. |
| LA-11 | §59.65 and §60.92(B–C): initial supply units, contents, dummies and placement have scenario-specific constraints. | Supply-unit and choice families identified; no numerical data admitted. | Independent transcription of source and errata; validate ownership, locations, contents, dummy handling, placement and no double-count. |
| LA-12 | Land §§32.11–32.15: abstract supply units, capture, stacking and contents. | Original Land p. 43 visually checked; source-specific supply units replace detailed supply accounting within stated scope. | Enumerate all capacity, stacking, capture and depletion boundaries; reconcile §60.92 initial contents and replay. |
| LA-13 | Land §§32.16–32.18: supply access, attempted destruction and dummy units. | Original Land p. 43 visually checked; range and terrain/ZOC conditions matter. | Specify per-unit reachability, legal destruction costs and randomness, dummy disclosure, and mutation/replay negatives. |
| LA-14 | Land §§32.21–32.25: ammunition and fuel expenditure. | Original Land p. 43 visually checked; supply varies by combat and movement use. | Map each reachable action to exact debit and timing; verify costs, rounding and insufficient-supply behavior with conservation tests. |
| LA-15 | Land §§32.31–32.37: supply-unit movement and transport. | Original Land p. 43 visually checked; [transport matrix](2026-11-13-graziani-supply-transport-matrix.md) separates the channels and constraints. | Classify land, rail, sea and abstract air obligations for the actual mode; verify legal movement, capacities and opposing visibility. |
| LA-16 | Land §§32.41–32.47: supply-unit availability and advance planning. | Original Land pp. 43–44 visually checked; side-specific planning and tables apply. [Planning grid](2026-11-13-graziani-supply-planning.md) records two- versus four-turn offsets and placement boundaries. | Reconcile initial-turn exception, pre-start initialization, six-turn arrival horizon, original table coordinates and player choices; do not assume no arrivals. |
| LA-17 | Land §§32.51–32.59: motorization use, allocation, loss and supply transport. | Original Land p. 44 visually checked; motorization is not subject to Breakdown, while other Breakdown rules remain. Candidate loss chart and first-October date conflict are recorded in the [crosswalk](2026-11-12-graziani-abstract-logistics-crosswalk.md#monthly-motorization-loss-chart-identity-and-date-ambiguity). | Verify chart identity, dash and lookup month, assignment priorities, abstract losses, special movement and conservation; distinguish motorization from physical truck authority. |
| LA-18 | Land §§32.61–32.66: simplified Axis naval convoys. | Original Land p. 44 and Axis chart p. 34 visually checked; convoys and attacks are still represented in abstract mode. [Convoy matrix](2026-11-13-graziani-abstract-convoy-matrix.md) separates capacity, attack choice, chart inputs and conditional loss examples. | Independently reconcile lane, capacity, arrival timing, loss table, minimum-loss wording, side knowledge and scenario setup; trace every reachable initial and later action. |
| LA-19 | Land §§32.71–32.84: abstract fleet bombardment and anti-air modifications. | Original Land pp. 44–45 visually checked; no assumption that omitted detailed modules eliminate all effects. | Establish initial state, timing, costs, eligible outcomes and both sides' changes; reconcile Group One-specific text and tables. |
| LA-20 | Land §32.9: road/track stacking modification in abstract mode. | Original Land p. 45 visually checked; applies only to the stated road/track restrictions. | Map its exact interaction with general hex stacking, movement and replay; test that non-road/track limits remain enforced. |
| LA-21 | Land §32.14: abstract supply-unit stacking, major-city and other-hex limits, and Tripoli/Tunisia boxes. | Original Land p. 43 visually rechecked: the printed sentence says three in any other type of hex “off-map,” then gives the Tripoli/Tunisia boxes unlimited capacity. The wording and its on-map application need interpretation. A text search of the retained September errata did not find §32.14; that is not proof no correction exists. | Check original errata and applicable map/box rules; do not silently rewrite “off-map” as “on-map.” Version the resolution and test ordinary/major city, off-map boxes, scenario exceptions, dummies and replay. |
| VI-01 | §60.81: Italian tactical outcome uses a designated objective plus retained locations and a supply condition. | Original scenario p. 28 visually checked. | Normalize exact held locations and timing; independently check supply semantics and replayed verdict. |
| VI-02 | §60.81: Italian decisive and strategic outcomes each specify their own location and supply conditions. | Original scenario p. 28 visually checked. | Enumerate both outcomes and overlapping cases; establish precedence and deterministic result event. |
| VI-03 | §60.81: Commonwealth tactical, decisive and strategic outcomes each specify locations and a supply condition. | Original scenario p. 28 visually checked. | Normalize each level, location identity and route; verify all six categories with independent result vectors. |
| VI-04 | §60.81 supply references and Land-only §§32, 59.6, 60.92 interact. | Original scenario p. 28 refers to convoy-based supply; no direct resolution of abstraction conflict was identified here. | Compare source alternatives and specify a versioned ruling for reachability, tracing and enemy interference; do not silently adopt a community rule. |
| VI-05 | §60.22 ends at turn 6/stage 3; §60.81 gives side-specific conditions. | Both source locations visually checked. | Resolve exact adjudication instant, simultaneous or overlapping outcomes, precedence and no-victory case; prohibit post-terminal mutation. |
| VI-06 | §60.22's printed victory reference points to §60.71, while §60.81 is headed for this scenario. | Both preserved scenario packages show the same text; [working decision RUL-GRA-02](2026-11-12-graziani-ruling-decisions.md) selects §60.81 for planning. | Independent review and versioned acceptance before freezing victory contract; separate supply/precedence questions remain open. |
| VI-07 | Land §3.1 glossary and §3.23: combat-unit classification has type- and situation-specific qualifications relevant to §60.81. | Original Land pp. 5–6 visually rechecked; the glossary notes that HQs, engineers and dummy formations may count as combat units in certain situations. | Resolve which conditions apply to each victory predicate, occupation and supply witness; do not assume a zero-strength HQ or dummy qualifies merely because a general exception exists. |
| RF-01 | Scenario §60.33 and Land §22.31 disagree on the type of Tobruk's repair facility. | Both original pages visually checked; scenario text calls it temporary and general text calls it major for its controller. | Source and review a separate precedence ruling, including capture/control and conversion; do not infer RUL-GRA-01 settles this conflict. |
| RF-02 | Land §22.31 lists major facilities independently of scenario §60.44. | Original Land p. 35 visually checked; general facility list includes Alexandria and Cairo. | Reconcile exact location, control and scenario-specific overrides with source map and repair rules; test both owners and replay. |
| RF-03 | Land §§22.13, 22.26, 22.35 and 32.11: repair resource requirements under Land-only abstraction. | Original Land pp. 34–35 and 43 visually checked; general repair costs and §32 resource replacement require reconciliation. [Interaction analysis](2026-11-12-graziani-ruling-decisions.md#open-interaction-repair-supplies-in-land-only-mode). | Rule separately on resource availability, cost and exemptions by repair type; test failure consumption, absence of invented stores and replay. |
| AR-01 | Scenario §60.47, Commonwealth §§4.43a/4.44b and Land §20.11: turn 1–6 reinforcements. | Six schedule positions and 21 candidate OA rows compared in the [arrival pilot](2026-11-12-graziani-arrival-scope.md); §20.11's brigade example conflicts with the schedule/OA date. RUL-GRA-04 selects a planning interpretation. | Independently review and version the selected interpretation; verify each unit, placement, errata, attachment/training and player decisions. |
| AR-02 | Axis §4.43b and scenario §§60.31, 60.37: initial forces and later arrival track. | Original Axis schedule starts at turn 9; this does not eliminate convoy, replacement or scenario-specific obligations. | Verify exceptional arrivals, initial placements, source/errata and convoy inputs before classifying any stage as empty. |
| AR-03 | Land §20.12 permits reinforcement movement in the arrival stage; errata §8.86 corrects the contrary Tripoli/Tunisia-specific case. | Original rule and correction checked; §8.86 is not independent blanket authority for Commonwealth Cairo arrivals. No scenario end-to-end arrival/movement acceptance. | Define restrictions and action ordering; prove arrival plus legal movement, replay, stale commands and rollback. |
| AR-04 | Scenario §60.47: no Replacement Points arrive before November 1940 in the Commonwealth reinforcement subsection. | Original scenario p. 28 and Map D turn track visually checked; the six-turn scenario ends in October IV. This is a candidate Commonwealth arrival exclusion, not evidence Axis convoys or non-replacement reinforcements are absent. | Independently check the restriction's scope and any errata; distinguish in-scenario arrival from plans for later turns, other replacement categories and terminal timing. |
| AR-05 | Land §§5.2, 20.21 and 20.72 with errata §20.72: Commonwealth replacement production planning uses differently stated lead times. | The sequence outline describes replacement production two turns ahead; §20.21 says four turns and §20.72 is corrected to one month ahead, with its table using the planning month. These are replacement rules, not §32.45's supply-unit arrival-period table. | Reconcile outline, corrected detailed procedure, initial planning and scenario horizon; test date boundaries and avoid applying the supply-unit schedule to replacement points. |
| AR-06 | Land §20.14: Commonwealth reinforcements normally arrive at Cairo, with named exceptions and a choice for returning withdrawn units. | Original Land p. 31 visually rechecked; the named Layforce and Tiger examples are later than this scenario, but possible scenario exceptions and returning-unit scope need checking. | Verify every six-turn group and applicable errata, Cairo city-hex eligibility, stacking, owner choice and event/replay; do not silently apply a named later exception. |
| AR-07 | Land §20.15: Axis reinforcements normally arrive in Tripoli, with a conditional Benghazi diversion. | Original Land p. 31 visually rechecked; diversion refers to Commonwealth combat-unit position relative to Mersa Matruh, unit size, assigned transport, stacking and port efficiency. §55.3 separately says scheduled reinforcements are not limited by port capacity/efficiency; the specific diversion condition needs reconciliation. Ordinary Axis track entries start after turn 6, but exceptions are not ruled out. | Determine scenario reachability and exact timing/limits, reconcile the general chart statement with the specific diversion rule, then test conditions without assuming Axis convoys are reinforcements. |
| PT-01 | Scenario §60.7 versus chart §55.3 and rule §55.25: Tobruk port efficiency and blockage. | Original scenario, chart and booklet visually checked; scenario value conflicts with chart maximum and general reduction. [Conflict analysis](2026-11-12-graziani-ruling-decisions.md#open-conflict-tobruk-port-efficiency). | Determine Land-only reachability, verify official corrections and adopt a versioned resolution only after review. Distinguish this from RF-01 facility type. |
| FG-01 | Land addenda §3.6: limited intelligence constrains opposing unit status, composition and attributes, with explicit exceptions for visible information. | Original Land p. 45 visually rechecked; map counters/stacks and permitted combat disclosures have their own specified exceptions. | Build per-action and per-view disclosure policy; prove equal-authorized hidden states do not leak through actions, errors, logs, event streams or snapshots while retaining allowed information. |
| FG-02 | Land addenda §§3.6 and 16.51: patrol disclosure has specific limits, including supply/attached-truck information and the types of component TOE points; errata §16.11 adds eligible types. | Original Land p. 45 and September errata p. 2 visually rechecked; [patrol/fog matrix](2026-11-13-graziani-patrol-fog-matrix.md) records further cases. Exact reporting, eligible patrol and noise rules still need full source review. | Specify authorized observation and uncertainty for both sides; test forbidden supply, type and future-plan disclosure and verify legal patrol actions independently. |
| FG-03 | Land addenda §3.6: the contents of broken-down, destroyed and abandoned vehicle markers are secret. | Original Land p. 45 visually rechecked; the marker's existence and its private contents are distinct. | Define public marker versus private lot projection, permitted capture/reveal exceptions, persistence and equal-observation negatives; do not hide or expose more than accepted rules allow. |
| PT-02 | Logistics §55.26 and Land §32.11: removing a harbor blockage has source-specific engineering and expenditure requirements. | Original Air/Logistics p. 24 visually rechecked; Tobruk has a special ordinary ammo/stores cost per level, while Land-only replaces ordinary expenditure and has no separate stores balance. | Establish whether clearance is reachable/applicable, how any abstract-mode replacement affects the cost and prerequisites, and whether a versioned ruling is needed; test timing, conservation, port identity and replay. |
| CO-01 | Scenario §60.7 and Land §§25.11–25.12: initial fortifications versus intrinsic city levels. | Original scenario p. 28 states no fortifications at setup; original Land p. 38 gives major cities intrinsic fortification levels, including a higher level for Cairo and Alexandria. Whether the scenario statement excludes those intrinsic levels is unresolved. | Independently check source/errata and scenario precedence; distinguish city classification, inherent levels and separately constructed fortifications, then test Combat effects, setup, destruction/rebuilding and replay. |
| ML-01 | Scenario §§60.31, 60.92(B) versus original location summary, map A and §37.42 land-distance chart: Benghazi setup hex. | Both scenario rows print B4827; two reference charts and the newly inspected Map A location summary print A4827. RUL-GRA-05 selects A4827 for planning. [Map identity pilot](2026-11-12-graziani-map-identity-pilot.md) records the scan hash and visual limit. | Review corrections and provenance; independently verify and version the working coordinate before setup admission. |
| ML-02 | Land §4.1 and map §37.42: adjacent map sections and distances. | Original Land p. 7 and Map C distance-chart note visually checked; their 39xx/01xx and 33-hex descriptions may conflict depending on grid interpretation. [Map seam investigation](2026-11-12-graziani-map-identity-pilot.md#potential-map-seam-conflict) separates this from movement legality. | Inspect physical overlap, grid staggering, referenced distance examples and errata; independently review and version transforms, aliases and source conflicts. |
| ML-03 | September errata Maps: Map D hexside 2228/2328 should have an escarpment. | Original errata p. 1 visually rechecked; correction is source evidence, not an admitted map edge or proof that either hex is reachable. | Independently verify hex identities, edge orientation and applicable traversal/LOS/supply effects; preserve symmetric edge semantics and test legal boundaries under the adopted map. |
| WT-01 | Land §§29.0–29.1 and common chart §29.61: determine weather each stage. | Original Land p. 41 and chart p. 6 visually checked; existing season-boundary ruling uses fall for turns 1–6. [Weather scope](2026-11-12-graziani-weather-scope.md) records the 36 ordered outcomes. | Independently verify calendar and ruling applicability, map-area assignments, stage transitions, RNG and replay in admitted scenario. |
| WT-02 | Land §29.34 and §§32.12–32.15: hot weather and abstract supply fuel. | Original source lists hot-weather fuel reduction; scenario supply units carry fuel. Existing Weather v1 admits no immediate-effect subjects only in synthetic setups. | Resolve source interaction, eligible stored amounts and rounding; implement/version and test resource effects or explicit rejection for historical setup. |
| WT-03 | Land §§29.41–29.58 and §32.55: foul-weather location, terrain effects and Breakdown interactions. | Original source visually checked; current weather artifact records Nile Delta exclusion as deferred. | Admit map subarea identity and enforce affected movement/construction/eligible Breakdown; establish wells/aircraft applicability and replay. |
| SU-01 | Scenario §60.41, Commonwealth OA and errata §4.44b: Matruh Garrison composition and movement. | Original scenario, OA and errata visually checked; errata's assignment wording conflicts with OA's no-assignment/attachment description. RUL-GRA-03 selects an assignment interpretation for planning. [Setup conflict](2026-11-12-graziani-formation-setup-conflicts.md) records remaining interactions. | Independently review the working decision, setup choices, attachment limits, close-assault exception, replay and disclosure. |
| SU-02 | Scenario §60.31 and errata §60.31: repeated XXI Corps artillery. | Original setup repeats a corps artillery entry; errata identifies duplication. | Reconcile unique unit/counter identity and scope against OA before normalizing composition or counting setup records. |
| SU-03 | Scenario §60.41, Commonwealth OA p. 21 and errata §4.44b item 3: French Motor Marine company. | Original scenario places a company at C3926; OA prints `3/65`, corrected to `D` by errata. Land §4.45 defines `D` as deployed at the first scenario start. [Arrival analysis](2026-11-12-graziani-arrival-scope.md) records the bounded comparison. | Independently verify company/counter/TOE identity and correction; place once under accepted setup, with no spurious arrival, and preserve separate similarly named formation identities. |
| TR-01 | Land §§5.2, 17.31–17.36, 20.43 and scenario §60.47: training and reinforcement timing. | Original rules and OA inspected; morale training is optional and distinct from replacement-point training. Training segment precedes arrival, while replacement-point text may allow arrival-stage training. [Arrival analysis](2026-11-12-graziani-arrival-scope.md#deployment-and-action-implications) records the boundary. | Resolve timing, eligible instructors, interruption, morale and placement; test optional untrained play and source-supported same-stage exceptions. |

The [victory matrix](2026-11-12-graziani-victory-matrix.md) decomposes VI-01–06
into source predicates and test obligations; it does not adopt a winner policy.

**Historical row accounting:** The original 47-row snapshot comprised 6 turn/initiative, 20 abstraction, 6 victory, 3 repair, 3 arrival, 1 port, 2 map locations, 3 weather, 2 setup and 1 training. The table has since grown to 67 IDs; see the [current gap audit](2026-11-18-graziani-source-register-gap-audit.md). Row count is not a completion metric.
At that snapshot all 47 retained at least one unresolved source, scenario integration or validation
obligation. No row is classified as fully supported for the playable scenario.
This does not assert that all underlying mechanisms are unimplemented.

## Recovered formation addenda

September errata §4.44b reports the omitted Sirte, Cirene and Marmarica OA sheets.
**Correction:** the previously catalogued [four-page errata PDF](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
does include scans of all three on p. 4. An earlier review missed the embedded
image and incorrectly reported it absent. The preserved PDF's SHA-256 is
`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.
The page was rendered and visually inspected. The
[Grognard errata transcription](https://grognard.com/errata1/cna.html) ends with
two linked images: [Cirene](https://grognard.com/errata1/cna1.gif) and
[Sirte and Marmarica](https://grognard.com/errata1/cna2.gif). Both images were
visually inspected: their headers and OA-style columns identify all three sheets;
the Cirene and Marmarica notes identify initial tank-battalion attachments. Original
scenario §60.31 visually places Cirene with LXIII(L) and Marmarica with LXII(L),
consistent with the respective sheet notes. The original Axis reinforcement
schedule §4.43b starts at turn 9 and lists Sirte at turn 10/stage 3, outside the
six-turn boundary; this confirms the schedule entry but does not by itself prove
every possible scenario-specific exception absent. A
[separate project](https://tangled.org/glassparency.bsky.social/cna-agent-sim/blob/main/docs/rules/graziani-offensive.md)
also identifies these as addendum scans. Extracting the two image objects from
errata p. 4 and comparing their RGB pixels with the separately hosted images
gave exact equality: Cirene `668 × 441`, Sirte/Marmarica `665 × 797`. They are
the same pixel reproduction, so their agreement is not independent scan evidence.
Their chain of custody is unknown. This is stronger source-package evidence, not
independent double entry, definitive physical-copy provenance or permission to
redistribute derived content.

Retrieved candidate image SHA-256: Cirene
`fca0a2eddb9380393337a7c88ba9e2c9e1428cdd0d62c90c3a85599b9456d101`;
Sirte/Marmarica `97154dd87f071f77bec4b601a39440bf53badbb0cee5f24922f41a60584d7834`.
Original Axis charts scan SHA-256
`9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca`.
Hashes identify retrieved bytes, not authenticity or permission. Next independently
double-enter needed records from errata p. 4 against scenario placement and
characteristics; corroborate period provenance or a physical copy. Verify scenario-specific
arrival exceptions. Do not copy images into Git or import another project's roster.
Exact setup counts remain unknown.

Reproduce pixel comparison with `pdfimages -f 4 -l 4 -png` on the hashed PDF
and compare decoded RGB image arrays to the two hashed GIFs. Equality is about
decoded pixels, not equality of PDF/GIF file bytes or proof of original origin.

The original Axis charts PDF p. 4 explains `D` as deployed at the start and
the two-part arrival notation as stage/turn. The following limited cross-check
therefore reconciles three fields without asserting the remainder of either sheet:

| Candidate | Candidate sheet | Independent original source comparison | Result / limit |
| --- | --- | --- | --- |
| Cirene | `D`; attached LXIII(L) note | §60.31 initial setup lists Cirene and LXIII(L). | Consistent on start and named attachment; unit roster and quantities not double-entered. |
| Marmarica | `D`; attached LXII(L) note | §60.31 initial setup lists Marmarica and LXII(L). | Consistent on start and named attachment; spelling varies between sources; roster not double-entered. |
| Sirte | `3/10` | Axis schedule §4.43b lists turn 10/stage 3. | Consistent on schedule position outside turn 6; scenario exceptions still require a full check. |

A first-reader count of the errata image finds 15 Cirene, 14 Marmarica and 14
Sirte OA rows, including headquarters and the explicitly listed tank attachment
where present. These are image-row counts, not a setup-unit or counter total:
scenario exceptions, training, attachment rules, air abstraction and duplicate
identity checks can change admitted composition. The two cited tank battalions
already appear in the OA rows and must not be automatically added again merely
because the scenario also names them. An independent reader must verify the
counts and each source field before a content contract relies on them.

| Printed OA category | Cirene | Marmarica | Sirte |
| --- | ---: | ---: | ---: |
| Division HQ | 1 | 1 | 1 |
| Infantry regiment HQ | 2 | 2 | 2 |
| Infantry battalions | 6 | 6 | 6 |
| Machine-gun, anti-tank, artillery, engineer | 4 | 4 | 4 |
| Bersaglieri motorcycle company | 1 | 0 | 1 |
| Listed attached light-tank battalion | 1 | 1 | 0 |
| **OA image rows** | **15** | **14** | **14** |

This grouping is a same-reader arithmetic aid, not another transcription or an
admitted force. Original Land p. 45 counter addenda section 1(A) additionally
identifies missing HQ stripes on `63 Cir`, `157 (63)` and `158 (63)`. That is
relevant when reconciling Cirene's HQ identities with counters: missing printed
stripes do not justify deleting the headquarters or flattening the formation.
Independently verify the complete counter/characteristic data and errata before
normalization. Source-derived imagery and counter art remain outside Git.

The sources are independent records for these particular facts, not proof that
the candidate image bytes are an authentic period printing. The §4.44b legend
also makes clear that parent assignment, attachment, counter and characteristics
codes need separate interpretation; do not flatten a note into a roster assignment.

### Counter-sheet cross-check: Benghazi and Soluch

Original counter section 2 (full-scan PDF p. 182, SHA-256
`836c14949c4fef1b066043e19a570256b22598d8ae2948d26844b949f53b60a5`)
shows VIII/II labeled Benghazi with the engineer symbol. The Axis OA calls the
unit an engineer but prints `bb`; §4.46b uses `bbb` for engineers and `bb` for
machine-gun battalions. The counter supports a `bbb` correction as a candidate;
independent review is still required. A text search of the retained September
errata found neither `Benghazi` nor `VIII/II`, which does not exclude an OCR miss
or another correction.

The same counter sheet depicts separate Soluch anti-tank and artillery counters.
The minor-garrison OA lists two 47/32 guns for the company and twelve 100/17
howitzers for the regiment, both at A4130. These are distinct from the separate
X Corps artillery listed in scenario §60.31. Source-linked records now preserve
the two garrison counters; independent verification and engine support for
their co-location and weapons remain open.

## Commonwealth repair: selected working reading

Original scenario §60.44 places an Alexandria major facility and Mersa Matruh temporary facility
after initial-supply details. Original §60.92(C) says §60.44 is replaced and then gives
Commonwealth initial supply. Original Land §22.31 independently states where major
facilities exist, including Alexandria and Cairo, and distinguishes temporary
facilities designated by scenario. Thus removal of §60.44's sentence would not by
itself eliminate the general major-facility rule; reconcile its exact location and
control conditions separately. The live ambiguity is the scenario-designated
temporary facility: **whole-case replacement** removes its setup statement;
**supply-only replacement** retains it. Heading/context favors considering the
latter, while the literal case reference favors the former. Neither establishes an
authoritative outcome. Community rules retain Mersa Matruh, but that does not itself
establish that §60.92(C) preserves it.

Working decision [RUL-GRA-01](2026-11-12-graziani-ruling-decisions.md) selects the
supply-only replacement for planning, subject to independent source/contract review
and versioned acceptance. Until then, do not silently instantiate or omit the
temporary facility; preserve §22.31 as separate evidence for major facilities.

Ruling candidates can be evaluated without pretending the text is unambiguous:

| Question | Whole-case reading | Supply-only reading | Evidence required |
| --- | --- | --- | --- |
| Mersa Matruh temporary facility at setup | Omitted with §60.44 | Retained as a distinct non-supply setup fact | Independent reading of replacement wording, page context, errata and any authoritative clarification. |
| Alexandria/Cairo major facilities | Evaluate §22.31 independently | Evaluate §22.31 independently | Verify general-rule scope, scenario map and control; no automatic finding about every repair action. |
| Initial Commonwealth supply | Use §60.92(C) | Use §60.92(C) | Independent double entry of counts and placement; do not combine §60.44 supply. |
| Save/replay and identity | Encode chosen ruling in scenario/ruleset identity | Encode chosen ruling in scenario/ruleset identity | Separate fixtures for each interpretation; reject mixed hashes and preserve historical reads. |

On adopting either reading, acceptance must cover presence or absence at initial
setup, major-facility independence, source-correct ownership and control,
repair eligibility, unavailable supplies, save/replay and deterministic rejection
of unsupported alternatives. RUL-GRA-01 selects the second reading for planning;
neither alternative is an accepted runtime rule.

## Next work

1. Obtain independent verification of scenario pp. 26–29 and the recovered errata
   p. 4 scans; extend applicability and review VI-06's working cross-reference choice.
2. Double-enter formation records against scenario and charts, review provenance,
   and review the selected temporary-facility interpretation through the ruling process.
3. Add source-specific statuses and exact evidence for applicable runtime behavior,
   including negative cases and stage/turn replay. Expand beyond this bounded
   register before reporting full scenario or unsupported counts.

## Proposed implementation and evidence sequence

These are candidate packets, not permission to bypass the roadmap's skeleton or
source gates. Exact schemas, owners and task sizes follow source/contract review.

| Packet | Predecessor and deliverable | Failure-sensitive evidence |
| --- | --- | --- |
| S1: source reconciliation | Original scenario pages, original common chart and §59.6; reviewed register with six-turn applicability and identified unresolved items. | Two independently checked readings; record date/rating boundaries, conflict/errata and source identity. Do not count unresolved as absent. |
| S2: scenario setup contract | S1, authenticated OA and rights/provenance review; versioned setup, side, initial-holder, location and abstract-mode identities. | Reject dangling references, conflicting source revisions, invalid choices and duplicate supply; canonical hash changes for semantic mutations; no hidden side data in outward views. |
| S3: initiative/stage lifecycle | S1/S2 plus required skeleton and stage-entry contracts; extend existing typed authority rather than bypassing admitted checkpoints. | Turn 1 consumes no initiative draw; later tied rolls reroll and replay identically; both holder choices each stage; verify same-side consecutive activations and stage/turn rollover; reject stale or wrong-side commands. |
| S4: abstract setup and supply | S1/S2 plus ruling for temporary repair; freeze distinct initial and arriving motorization, supply, placement and optional-load contracts before runtime. | Medium/heavy/light distinctions; air-facility exclusion; source-defined threshold boundaries and remainders; loaded-point restrictions; independent source totals, conservation and tamper/replay negatives. Exact expected values await source confirmation. |
| S5: admitted scenario and product | S1–S4, remaining source families, certified Combat/public/Runner and roadmap gates. | Full six-turn progression and victory, save/restart, private no-model handoff, two independently checked complete games, release-head CI and packaging evidence. |

S1 and source provenance research can run while skeleton work continues. S3 and
S4 do not imply the entire scenario is supported. Each packet must identify its
precise supported profile and reject everything outside it.

### Land-and-Air supply pointer

Scenario §60.93(B–C) points to §60.82(B–C) for starting supply, but printed
§60.82 is the Italian Campaign victory section; §60.92(B–C) contains the
corresponding initial supply tables. Treat §60.92 as a candidate correction
only, pending independent and errata review. The extra air-facility supplies
in §60.93 do not automatically belong to the Land-only §60.92 profile. See
[review evidence](2026-11-13-arrival-ruling-review.md#land-and-air-supply-cross-reference).
