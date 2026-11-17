# Graziani patrol eligibility, disclosure and losses

**Status:** source boundaries for FG-02/SQ-07, not an approved observation
contract or proof that a scenario patrol is possible. Examples below are
conditional, unexecuted vectors. Preserve unknowns and hidden authoritative
state until the relevant choice and source inputs are resolved.

## Sources and scope

Visually inspected [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
printed pp. 12, 26–27, 43, 45: §§5.2, 16.0–16.55, 32.25 and addenda §§3.6,
16.51. Land scan SHA-256
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
Visually inspected [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
p. 2 §16.11, scan SHA-256
`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.
Tables §§16.6–16.8 were not inspected for this note: do not infer numerical
probabilities or loss results from the procedure alone. Other clarifications,
scenario compositions and later errata remain outside this pass.

## Timing and eligibility

§5.2 places each player's Patrol Phase after that player's movement/combat,
truck movement, rail if applicable, and repair; player B then takes phases F
through L after A. §16.0 says no anti-armor or close assault combat occurred in
the Operations Stage; §16.21 instead specifies that the **phasing player** did
not engage in either against enemy units in that Operations Stage. §5.2's
outline mentions assault only. These wordings do not establish whether A's
earlier attack bars B's later patrol when B participated as defender. Preserve
combat occurrence, participants and types per stage and side; compare both
interpretations. A prior barrage alone does not globally bar patrol, but
§16.21 prohibits reconnaissance of a hex that underwent any barrage or air
bombardment. Reading the current Operations Stage as that prohibition's window
is a plausible interpretation of surrounding text, but the second clause
does not repeat that window explicitly. Keep hex and stage history for both
sides and retain the timing question, not just the acting unit's attacks.

§16.11 permits recce-type, Italian CV/33, British Mark VI Light, German Panzer
I, and motorized infantry TOE points. Errata adds Italian L/6, Commonwealth
Stuart, and anyone's mechanized infantry (including Panzergrenadiers). These
are point/type conditions, not evidence that a particular initial counter has
such points. §16.12 detaches designated eligible TOE points abstractly; no unit
physically moves. §16.13 caps points assigned **from one hex** at two regardless
of unit count; §16.14 caps points sent **to one target** at three, possibly
combined from origins. The text does not explicitly settle cumulative reuse of
the same points in separate patrols; §16.21 permits as many patrols as desired.
§16.15 excludes a unit at cohesion −8 or worse. §16.17 excludes patrol origin
or target hex affected by rainstorm or sandstorm, not every intervening hex on
that sentence alone.

§16.22 targets an enemy-occupied hex within five hexes, using a path containing
no terrain impassable to the patrol units and no enemy-controlled or occupied
hex other than a hex in the ZOC of the unit being reconnoitered. Source does
not specify every path-selection or target-identity detail for mixed patrols;
do not substitute straight-line range for a legal path. §16.16 normally costs
one ammo and two fuel per point at origin regardless of outcome, but Land-only
§32.25 says patrolling requires no supply expenditure. Do not charge §16.16 in
that mode; other capability costs require separate source review.

## Resolution and disclosure

1. Announce target and allocated points. Under §16.32 the non-phasing player
   first reports if the target contains **no combat units**; AA/Flak and
   engineers do not count as combat units for this particular check. In that
   case skip patrol-survival roll and patrol losses, not automatically the
   subsequent reconnaissance and objective-loss procedures. Whether the
   reported empty status covers a particular other unit category requires its
   classification; it is not a blanket absence-of-all-counters disclosure.
2. Otherwise roll on the survival table. §16.33 applies −1 only when **all**
   patrol points are recce. Elimination/capture losses, including trucks, are
   immediately deducted with phasing player's choice of unit type; captured
   points become prisoner points. If none survive, no reconnaissance
   information is obtained. Check table and prisoner treatment before defining
   concrete quantity or type conversion.
3. With survivors, resolve reconnaissance table. §16.5 expresses result as
   number of battalion-size units about which the non-phasing player must
   report. §16.51 requires historical designation, unit type, motorization
   status and TOE strength reported within two of actual at patrol time.
   The printed wording says “exact TOE Strength within two,” and does not
   prescribe deterministic rounding, bounds or which admissible report the
   defender chooses; do not report guaranteed exact strength. Addendum §16.51
   explicitly withholds constituent TOE point types: its 8-point mixed-tank
   example allows reporting a six-point tank battalion without identifying its
   tank models. §16.52 lets the defender select units when more exist than
   required, subject to §§16.53–.55: battalion-sized unless only smaller or
   larger present; no HQ or AA/Flak unless no combat units, engineers may be
   reported; patroller may identify units they do not want information about,
   which defender may not select. Exact behavior if exclusions leave too few
   selectable units needs a ruling.
4. §16.34 still calls for an objective-loss roll after information **or** the
   entire patrol's elimination, whichever first. Defender selects losses from
   combat units; if none, the listed priority is trucks, AA/Flak, engineers,
   then HQ. If the whole patrol is eliminated, an objective-table captured
   result becomes eliminated. Do not equate “entire patrol eliminated” with
   every no-survivor case involving captured or mixed losses without a separate
   ruling. Resolve loss and disclosure separately: casualty
   application does not itself authorize exposing unit-by-unit private truth.

§3.6 addendum limits opposing knowledge to explicit disclosures; patrol does
not disclose supply status or attached trucks beyond motorized status. Its
map-counter, combat and prisoner disclosures have their own scope. §§16.41–.46
allow misleading dummy-tank reports under specified conditions, including a
defender-selected apparent designation and strength; never normalize every
reported field to true state. Italian formations cannot use those dummies;
initial availability, Land-only construction cost and coexistence with the
§16.32 empty-combat check need separate review. Report freshness, lasting
knowledge and authorized event/audience shape are not resolved by the cited
passages.

## Distinguishing tests before implementation

* Source limits 2 versus 3 from one hex and destination limits 3 versus 4;
  cohesion −7 versus −8; eligible mixed types versus an ineligible type;
  5 versus 6 hexes, blocked versus clear legal route, storm origin/target.
* Prior own or opponent anti-armor/assault, barrage at target versus elsewhere,
  stage rollover and both A/B orders. Preserve ambiguity as a versioned policy,
  rather than erase either combat history or the narrower §16.21 wording.
* No-combat target containing only AA or engineers: announce scoped absence,
  skip survival roll, still take later applicable steps. All-recce versus
  mixed modifier, total survival loss, objective capture converted to
  elimination, and deterministic replay with correct distinct draws.
* Defender's authorized unit and strength choices, §16.55 exclusions, mixed
  tank model secrecy, dummy report, stale report after movement, and equal
  player-visible histories with different hidden supply and TOE composition.
  Keep RNG, private losses and report selection authoritative; emit only
  audience-authorized observations, including in errors and event metadata.

No runtime changes or gameplay tests. Reconnaissance and loss-table mappings,
dummy interactions, stage-history interpretation and report schema remain
admission blockers.
The [independent source check](2026-11-17-graziani-patrol-details-independent.md)
corroborates the bounded caps, eligibility, disclosures and objective-loss
procedure, and preserves those unresolved timing and all-eliminated distinctions.
