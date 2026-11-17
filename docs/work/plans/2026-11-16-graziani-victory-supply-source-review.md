# Graziani victory supply under Land-only rules — source review

**Status:** targeted, non-independent source research and decision packet; no
accepted victory rule or runtime change. This follows the [victory matrix](2026-11-12-graziani-victory-matrix.md)
and [abstract logistics crosswalk](2026-11-12-graziani-abstract-logistics-crosswalk.md).
The original sources do not explicitly specify a complete §60.81 Land-only
victory-route algorithm in the passages reviewed. Keep the gate fail-closed
until a reviewed, versioned interpretation and its prerequisites exist.

**November 17 independent follow-up:** [source and decision review](2026-11-17-graziani-victory-independent-decision.md)
proposes a versioned, fail-closed result-composition policy that retains all six
row outcomes, distinguishes unknown from false and avoids inventing a sole
winner when both sides qualify. Land-only route and terminal ordering remain
unresolved; this is planning guidance, not accepted runtime authority.

## Source scope

Original [scenario scan](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf),
PDF pp. 2–5, printed pp. 26–29, §§59.6, 60.22, 60.81 and 60.92;
[Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), printed
pp. 12, 14, 43–44, §§5.2, 8.18–8.19 and 32.0–32.6; and
[September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
are the source package. Local copies were identified respectively by SHA-256
`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`,
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`, and
`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.
Relevant scenario and Land pages were visually rechecked; errata has not been
exhaustively reviewed for every interaction. These are same-edition records,
not independent editions or a blind second reading.

## What the source establishes

| Question | Source observation | Limit |
|---|---|---|
| End boundary | §60.22 ends this scenario after turn 6, stage 3. Its printed victory reference points to §60.71; the named Graziani heading is §60.81. | [RUL-GRA-02](2026-11-12-graziani-ruling-decisions.md#rul-gra-02-victory-reference) chooses the named heading for planning. Exact terminal event ordering and independent ruling review remain open. |
| Italian victory | §60.81 names convoy supply from map D for strategic, and from Tobruk for decisive; tactical uses the latter supply reference. | The printed rows differ in location and unit wording. Map D “any hex,” supply subjects, control and actual route semantics need interpretation. |
| Commonwealth victory | Tactical names a truck-convoy trace to Alexandria or Cairo; decisive and strategic say supply as above. | This is a source condition in addition to possession. It does not state an algorithm for the Land-only substitution. |
| Land-only replacement | §32.0 and §32.11 replace ordinary supply usage/expenditure with §32's supply-unit and motorization procedures. Scenario §§59.62 and 60.92 specify initial abstraction. | The printed passages reviewed do not say that §60.81's victory supply condition disappears or that §32.16 is the victory test. |
| Local supply draw | §32.16 lets a friendly combat unit draw from a supply unit within half its CPA, measuring travel as a medium truck except for non-motorized infantry; it blocks impassable terrain and enemy ZOC not occupied by friendly units. | This is access to a nearby supply unit, not explicitly an end-to-end connection to Tobruk, map D, Cairo or Alexandria. It contains no stated victory-route substitution. |
| Convoy concepts persist | §32.51 treats motorization points as medium truck points except as modified; §32.33 explicitly allows a motorized supply unit to move in the Truck Convoy phase or the owner's Movement/Combat phase, subject to its restrictions. §32.58 gives motorization transporting a supply unit CPA 15. Land §5.2 retains the Truck Convoy phase; §32.6 also retains **Axis naval** convoys. | Physical truck accounting is abstracted, but it is misleading to infer that every convoy concept disappears. Neither the continuing phase nor a naval convoy alone proves what §60.81 requires at victory. The printed CPA 15 for an actual supply-unit transporter does not establish a range for a hypothetical victory trace. |

The [scenario row matrix](2026-11-12-graziani-victory-matrix.md#condition-decomposition)
preserves all six printed categories and their different location/wording
boundaries. The longer Italian Campaign §60.82 is a different scenario; do not
import its one-game-turn trace length, point scoring or cut-off rule into §60.81.

## Competing interpretations to decide

1. **Hypothetical route:** test a source-specific medium-truck-traversable path
   from each applicable objective/holding unit to the named source, without
   requiring an actual shipment. Specify ZOC, opposing occupancy, source control,
   map D quantifier, terrain, geometry, weather, length, and whether a friendly
   unit in an enemy ZOC lifts the obstruction. This preserves a meaningful
   route condition but introduces terms the scenario does not fully spell out.
2. **Actual or feasible movement/supply:** require an actual or presently legal
   supply movement or shipment. This reflects continuing motorization and
   convoy phases, but requires a separate ruling on timing, availability,
   capacity, path and whether shipment history is necessary. A naval arrival
   cannot automatically substitute for an inland victory route. Section 32.58's
   CPA 15 is evidence for the specific transporter, not proof that a whole route
   must fit in one stage or that relays are permitted.
3. **Local supply-unit access:** use §32.16's range and path test. This has a
   detailed printed rule, but does not itself establish connectivity to the
   named victory source; applying it alone changes the scenario's condition.
4. **Defer:** reject historical victory adjudication until source hierarchy and
   required map/control semantics are reviewed. This avoids inventing a rule,
   but leaves the playable scenario blocked.

An external [CNA Living Rules scenario](https://basmith.net/cna/rules/scenarios/10-the-italians)
proposes a route-only interpretation in R-110 and a combat-unit-only blocking
interpretation in R-119. It is an unaffiliated, provisional community ruling,
not official errata or an independent source transcription; its premise that
there are no convoys to trace must be read alongside §32.33 and §32.51 above.
It is comparison evidence for option 1, not an accepted Sandtable rule. Its
specific unlimited length, blocker definitions and source treatment need their
own review. The source scan alone does not settle those implementation choices.

## Decision and test checklist

Before selecting and versioning a policy, decide:

- Which named subject must satisfy supply for each row, including the two-place
  Italian decisive row and Italian tactical retain-versus-take wording.
- Whether the map D source is an existential eligible hex or a stronger
  condition, what ownership and control mean, and whether objective cities
  denote one specified hex or a source-supported set.
- Whether the route is hypothetical, feasible this stage, or evidenced by an
  actual shipment; direction, distance or CP limits, applicable terrain and
  hexside rules, source/endpoint treatment, weather and map-boundary semantics.
- Whether enemy units, enemy ZOCs, friendly units, supply units and dummies
  obstruct or neutralize a trace. Decide whether the §32.16 wording carries
  over and whether Land §8.19's Commonwealth western limit applies to a
  hypothetical convoy; do not silently infer either.
- Exact after-stage snapshot, no-match and simultaneous outcomes, and precedence
  when one side satisfies non-nested level conditions. Do not assume all lower
  levels are prerequisites or import the longer scenario's points policy.

Candidate distinguishing vectors: (a) a unit has a friendly supply unit within
§32.16 range but no connection to its named victory source; (b) a route exists
but no supply unit is within draw range; (c) a route exists but transport is
unavailable; (d) enemy ZOC with and without an eligible friendly occupant;
(e) blocked or missing seam and physical boundary; (f) multiple map D source
candidates; (g) one of two decisive locations has no qualifying route; and
(h) both sides or multiple non-nested levels match. Evaluate route changes at
the accepted terminal cut and verify deterministic replay and side-safe output.
These are policy tests to write after a ruling, not passing tests or proofs of
historical reachability.

**Next work:** independent source/errata review, map/terrain and control
coverage, a reviewed decision for each bullet, then contract-first versioned
implementation and focused negative/replay tests. Preserve all competing
readings and provenance in the ruleset identity; no current-save reinterpretation.
