# Graziani turn obligation: compulsory combat from enemy ZOC

**Scope:** Bounded primary-source reading of the repeatable Movement/Combat
loop and Land §10.3, including the September erratum. This establishes
conditional rules obligations, not a completed six-turn reachability proof,
accepted executable contract, or claim that all turn obligations are known.
No runtime or readiness changes.

## Applicability and timing

Scenario §60.22 starts Graziani's Offensive at turn 1, OpStage 1 and ends at
turn 6, OpStage 3; §60.6 makes the Italians the initiative holder for turn 1,
not necessarily the first-moving side. Land §5.2 permits the initiative holder
to select relative player A or B at each stage. In each acting-side block,
Movement, Breakdown, Combat and Reserve Release repeat together when the
phasing player elects another permitted cycle; player B then has its own block.
The first Operations Stage's facets repeat in stages 2 and 3. These are 36
potential side blocks across 18 stages, not 36 identical combats or proof of
an obligation in any particular initial state.

After movement and reactions cease, evaluate the phasing side's obligation
against the qualifying ZOC and friendly units in that Combat Segment. September
errata §10.3 expressly says the case does **not** apply to the non-phasing
player. The phasing side can face an obligation even if it chose not to move:
§10.31 concerns existing ZOCs, not just ZOCs entered in that segment. An empty
legal-choice set or a pre-existing adjacency is not by itself proof that a
Combat Segment can be skipped. Conversely, adjacency alone does not prove an
obligation, and no assertion is made here that the actual setup starts in
contact or reaches contact within six turns. Full scenario unit identities,
raw strengths, map/edge coverage, private choices and setup rulings remain
needed for an actual reachability finding.

## Source-observed rule boundary

- Land §§10.11–.15 set who exerts ZOC: relevant organization/stacking, a
  ten-raw-defensive-close-assault-point minimum, and cohesion and other
  exclusions apply. §10.21 prevents projection through all-sea, major-river,
  lake and escarpment hexsides and into hexes the exerting unit could not
  enter from its location. These predicates require accepted unit, terrain and
  hexside data; proximity is insufficient. §10.16 gives a specific ZOC
  disclosure when the phasing unit starts a Movement Segment in, or enters, a
  hex where the other unit could project it. It is not authorization to
  disclose all hidden strengths or a whole-map ZOC field.
- §10.31 requires attacking **each enemy-occupied hex whose units exert ZOC on
  friendly combat units**, by artillery barrage or close assault; its note says
  a probe satisfies the requirement if the **basic differential is −4 or
  better**. The subject is enemy hexes imposing qualifying ZOC, not every
  adjacent unit and not a generic command to attack any one neighbor.
- §10.32 excepts the case where friendly units in that enemy ZOC are solely
  artillery, anti-tank, anti-aircraft or non-combat units, or other types are
  present but pinned. The wording and timing must be reconciled with barrage
  and mixed hexes before defining a final predicate. Errata §12.44 specifies
  barrage pinning applies to the specific target battalion-equivalent, not
  automatically the whole formation. Land §12.44 says pinned units cannot
  move or engage in anti-armor or close assault for the rest of the Combat
  Segment, with its stated gun-class and artillery-HQ exceptions.
- §§10.33–.35 permit a special holding-off barrage. To satisfy its printed
  threshold, actual barrage points must be at least the number of individual
  non-gun enemy battalion-equivalent combat units in that target hex; units
  under one stacking point and gun units do not count under §10.34. If the
  phasing player cannot hold off the enemy in a hex under that rule, §10.35
  requires close assault, subject to §10.32; there is no minimum point
  requirement for that assault. Determining how §10.31's general “artillery
  barrage” wording interacts with the special quantitative holding-off
  procedure remains an explicit interpretation question, not permission to
  count every ineffectual barrage as satisfying the obligation.
- §10.36 specifies a fallback if a unit can neither assault nor hold off an
  enemy unit: it must retreat three hexes, pay movement CPs and gain three
  additional disorganization points. It must finish three hexes from that
  enemy, cannot double back or pass through enemy ZOC, and surrenders in
  entirety if forced to enter enemy ZOC. Interactions with multiple enemy
  units, blocked or too-short routes, stacking, terrain, existing cohesion,
  supply and timing need source-backed decisions; do not silently make the
  result a no-op or infer surrender from every kind of missing path.
- §15.91 defines a probe using less than 50% of available TOE points in that
  or another attack; §15.94 allows its nature to remain unannounced until the
  assault is over. The §10.31 threshold uses *basic* differential, distinct
  from §15.96's final adjusted differential. Preserve required secrecy and
  distinguish decision-time knowledge from authoritative validation.

## Acceptance questions and focused vectors

Before admission, define a repeatable segment state machine, who selects each
attack/holding-off/retreat, exactly when qualifying obligations are calculated
and updated after simultaneous barrage/pinning and withdrawal, and how
satisfaction is carried across Combat steps. Do not decide secret opponent
attributes from a player-visible projection; do not leak hidden strength through
legal-action enumeration, rejection, timing or event payload. Preserve source-
authorized disclosure, and test equal-authorized observations with different
hidden characteristics. Check anti-armor/close-assault sequencing, ammo and fuel
under §32, limits and mandatory decisions before permitting end-of-segment or
end-of-block; persist owner choices, any random draws and replay identity.

Candidate deterministic tests, with expected results conditioned on accepted
rules and synthetic certified state:

1. No enemy projection despite adjacency; projection blocked by escarpment;
   stacking/strength or cohesion below threshold; and qualifying projection
   into an unmoving phasing combat unit. Flip phasing side in identical state
   to check the §10.3 erratum without inventing a defensive attack obligation.
2. Two distinct enemy ZOC hexes, each requiring its own satisfaction; one
   held off at exactly the §10.34 threshold and one below it. Check exclusion
   of gun and sub-one-stacking-point units, then assault/exception as appropriate.
3. §10.32 only-exempt-unit case, mixed unpinned case, and individually pinned
   battalion versus other unpinned components; resolve timing before fixing
   expected results. Verify probe at basic −4 versus worse, even if its final
   adjusted differential differs.
4. §10.36 eligible three-hex retreat, its CP and disorganization costs,
   illegal doubled-back or ZOC route, and the source-specific forced-ZOC
   surrender. Test several threats and unavailable legal routes only after
   those ambiguities are settled.
5. Repeated Movement/Combat segments, ZOC changed by movement, pinning or
   elimination, player A/B rotation, crash/replay and exactly-once obligation
   satisfaction. Missing map coverage or a hidden unknown is not a source-
   proven no-combat decision; follow the authority and privacy contracts.

## Source locators and limits

Visually inspected hash-verified originals, PDF pages equal printed page numbers
unless noted:

- [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), pp. 12,
  14, 17–20 and 25, §§5.2, 8.13–.15, 10.1–.3, 11.0, 12.44 and 15.9;
  SHA-256 `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
- [Scenario booklet](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf),
  PDF pp. 3–4 / printed pp. 27–28, §§60.22 and 60.6; SHA-256
  `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`.
- [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf),
  PDF p. 1, §§10.3 and 12.44; SHA-256
  `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.

This reading uses the same edition as the existing source register and
[sequence crosswalk](2026-11-13-graziani-land-sequence-crosswalk.md), not an
independent edition. It does not exhaust combat, errata or the sequence; it
specifies no implementation and no gameplay tests were run.
