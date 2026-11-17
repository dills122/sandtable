# Patrol details: source recheck

**Status:** fresh visual audit of [patrol details](2026-11-17-graziani-patrol-details-next.md)
by its author; not an independent reader or edition. Core numbers, disclosure
and ordering check out. Two phrasings deserve narrower treatment before they
are used as definitive rules: the barrage-history window and the distinction
between no survivors and all eliminated.

Checked [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
printed pp. 12, 26–27 and 45: §§5.2, 16.0–.55 and addenda §§3.6, 16.51;
scan SHA-256 `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
Rechecked [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
p. 2 §16.11, SHA-256
`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.
Tables §§16.6–.8, scenario forces and later errata not inspected here.

## Findings

1. **Barrage window requires qualification.** §16.21 describes a player using
   artillery barrage during a given Operations Stage, then forbids
   reconnaissance of any hex that “has undergone” any type of barrage or air
   bombardment. Reading that prohibition within the current stage is natural,
   but the second clause does not independently repeat the time window. The
   note's unqualified “during that Operations Stage” should be a provisional
   interpretation. Preserve target hex, event kind and stage history; compare
   current-stage, prior-stage and other-target cases before accepting a rule.
2. **No survivors versus all eliminated.** §16.33 says no surviving patrol
   points yields no information. §16.34 still calls for objective loss after
   information or elimination, and changes its captured result to eliminated
   if the entire patrol is **eliminated**. The note correctly names complete
   elimination for that conversion. Its test phrase “total survival loss” must
   not imply that a patrol wholly captured, or a mix of eliminated and captured
   points, necessarily satisfies the source's “eliminated in entirety” wording.
   Whether the objective roll's trigger includes a zero-survivor mixed outcome
   remains an interpretation; retain separate captured and eliminated counts.
3. **No-combat check is scoped.** §16.32 requires the non-phasing player to
   say if no combat units are in the target; AA/Flak and engineers are not
   combat units for this check. Skip the survival table and patrol losses when
   none; do not equate that with an empty hex, skip all later procedures, or
   reveal identities of noncombat occupants automatically. The general
   procedure and §16.34 support subsequent reconnaissance and objective loss,
   with target contents and table results still affecting applicable results.
4. **Information is bounded, not exact truth.** §16.51 calls for historical
   designation, unit type, motorization and “exact TOE Strength within two of
   the actual strength at the time of the Patrol.” Addendum §16.51 illustrates
   eight mixed tank points reported as six, and forbids identifying constituent
   TOE point types. It does not supply a generic numeric choice algorithm.
   Defender selects reportable units subject to §§16.52–.55, including the
   patroller's exclusions; §16.45 allows a dummy's misleading designation and
   strength. Addendum §3.6 separately withholds supply state and attached
   trucks beyond whether motorized. Do not project exact private composition,
   unrestricted current truth, or a generic true-strength interval for dummies.
5. **Origin, destination and phase details check.** §§16.13–.15 state at most
   two points from one hex, at most three to a target, and no contribution from
   a unit at cohesion −8 or worse. §16.17 names storm-affected origin or target;
   §16.22 imposes five-hex distance and path restrictions. Cumulative reuse
   across patrols remains unresolved. Errata adds Italian L/6, Commonwealth
   Stuart and anyone's mechanized infantry to §16.11. §16.0's stage-wide
   wording, §16.21's phasing-player wording and §5.2's assault shorthand do
   not settle earlier opponent combat versus later player patrol.

## Required distinguishing cases

* A patroller eliminated entirely versus all captured versus mixed losses:
  separately assert reconnaissance absence, objective-roll trigger and
  captured-to-eliminated conversion. Do not collapse these into one boolean.
* Enemy hex containing AA or engineers only versus a combat unit: survival
  check differs, but occupancy, later information and objective losses do not
  become public wholesale. Two same-visible states must not leak hidden
  composition through choice options, error responses or event timing.
* Eight real mixed tank points may produce the source's six-point report;
  prohibit underlying model revelation. Compare a dummy reported as a tank
  unit, defender selection, patroller exclusions and changed strength after
  observation. Do not infer report freshness from this rule alone.
* Test 2/3 points from one origin, 3/4 to a target, −7/−8 cohesion, storm at
  origin/target, route beyond five, blocked route, both stage orders, and
  barrage in current/prior stages. Preserve uninspected chart results as
  unknown rather than inventing die outcomes.

No implementation or tests changed; these vectors are proposed, not run.
