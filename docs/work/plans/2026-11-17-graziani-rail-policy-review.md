# Commonwealth rail capacity and ZOC policy: source review

**Disposition:** Retain the proposed cap and friendly-screen rule as
explicitly versioned planning choices; do not admit rail actions until the
remaining source, state and information gates are resolved. Tighten the
proposal's treatment of origin occupancy, disclosure, stopping and no-ops.
The source does not itself require the proposed separate, stationary screen.
This is a targeted, informed visual rereading of the same edition, not an
independent edition or proof of a reachable rail trip.

## Findings

1. **Separate origin legality from the printed entry ban.** Land §8.73
   (printed/PDF p. 15) requires the moving unit to start the OpStage on rail,
   restricts CP expenditure and its starting enemy ZOC, and prohibits entering
   enemy ZOC or an enemy-occupied hex while using rail. It does **not** say, in
   that clause, that enemy occupancy of the origin is an additional express
   rail-specific condition. The proposal applies the occupancy test to the
   origin as well as entered vertices and attributes it to §8.73. This may be a
   defensible consistency or other-rules check, but cite its actual source or
   label it an additional project invariant. Unknown or contradictory
   co-occupancy state should not be silently assumed impossible. Continue to
   test the printed enemy-occupancy prohibition on all **entered** vertices,
   independently of screens.

2. **Screen restriction is a policy, not a source prerequisite.** §10.26
   (printed/PDF p. 18) negates the enemy ZOC's effect for all movement purposes
   when a friendly combat unit is present in the hex. It does not expressly
   require a different unit to have occupied the hex before the traveler, or
   remain stationary through passage. §8.73's special ZOC prohibition raises a
   real precedence question; either an absolute rail-specific ban or a
   broader friendly-presence rule can differ from the selected stationary
   separate-screen rule. The candidate correctly acknowledges that choice,
   but “smaller certified set” should mean **policy-admitted subset**, not an
   independent source certification of legal trips. At origin, the rail
   traveler itself may already be a friendly combat unit; excluding it is the
   sharpest distinguishing case. Keep projected ZOC separate from its negated
   effect, and do not equate negation with enemy control disappearing.

3. **Entry prohibition is not ordinary movement's stop-on-entry.** §10.23
   normally stops a moving unit upon entering enemy-controlled hex; §10.26
   negates the effect for movement when its condition applies. The proposed
   interpretation applies that negation to rail, but absent an accepted screen
   §8.73 prohibits entry rather than instructing an automatic stop on the
   blocked hex. The proposed all-or-nothing validation is a further project
   choice: if the attempted route is rejected, do not also place the traveler
   at the blocked vertex or create a partial journey. Automatic truncation to
   the previous safe vertex, retry and disclosure semantics require separate
   authorization. Conversely, a qualifying screen under the selected reading
   must negate the movement effect, including any generic stop, rather than
   allowing entry but stopping solely because the underlying ZOC exists.
   Source-specific disclosure still applies; negating the movement effect
   does not erase the underlying ZOC fact.

4. **Privacy wording needs audience and source-authorized discovery.** The
   statement “never ... reveal whether a supply counter is real” lacks an
   audience and discovery condition. §32.18 (printed/PDF p. 43) says a dummy
   is removed upon discovery; owners and the umpire also need their authorized
   counter state. Before disclosure, action enumeration, success versus
   rejection, timing, partial movement, capacity history and replay can all
   leak more than an error message. Separately, §10.16 (pp. 17–18) requires the
   non-phasing player to disclose whether a particular unit exerts ZOC in its
   stated movement-trigger circumstances. The exact application to rail
   paths is not settled by this review. Define that trigger and audience, and
   test equal observable histories only where all authorized observations
   are equal; do not adopt either unconditional silence or indiscriminate
   exposure. The proposal's admission gate correctly recognizes this issue.

5. **Define no-op and atomicity, including the phase.** Zero supply counters
   can accompany a **real unit movement** and must not by itself turn the
   command into a no-op. Conversely an empty stack, same-origin/destination,
   decline or unsubmitted order must not consume a directional stack slot,
   two-counter allowance, CP, or rail-movement journal event unless an
   independently accepted contract expressly says otherwise. The phase may
   still advance on an authorized decline; failure/unsupported must not
   accidentally advance it. Define whether no-op commands are rejected or
   acknowledged, and whether identical retries are idempotent, separately
   from the act of ending the phase. Unknown capacity, track, screen or ZOC
   must leave state and slots unchanged. No inspected rail case explicitly
   defines this API behavior; these are needed implementation invariants, not
   claimed printed rules.

6. **Capacity remains a selected interpretation, with another cap to retain.**
   §8.72 permits one stack in each direction during the Commonwealth Rail
   Movement Phase; §32.34 permits any two real and/or dummy Supply Units in one
   direction as the OpStage total load, subject to normal railroad rules.
   Treating the latter as a stage-wide ceiling inside §8.72 rather than an
   additional service is a plausible, deliberately restrictive precedence
   decision. It is not an explicit source reconciliation of mixed unit/supply
   loads or independent separate movements. §8.75 (p. 16) separately caps
   rail-carried stacking points at **two**; the two-Supply-Unit ceiling is not
   a substitute for it. §32.14 says supply units have zero stacking value but
   separately imposes unit-count caps; do not use a zero stacking value to
   bypass §32.34, or assume the full mixed-load rule is now established.
   Stage direction, batching and actual legal stack remain open.

7. **Erratum is narrower than a blanket organization ban.** September errata
   §8.73, PDF p. 1, says a unit traveling by rail may not earn reorganization
   points in that stage. It does not expressly prohibit every organization
   action or settle how a point already earned before the rail phase is
   handled. No retrospective deletion or unqualified future CP permission is
   established. Policy correctly leaves prior/later CP and stage order open.

## Distinguishing acceptance vectors

- With the selected policy and certified identical track/state, try origin,
  transit and destination ZOC separately: distinct stationary friendly combat
  screen, only moving combat unit, no friendly combat unit, and unknown unit
  classification. Compare the broader-presence and absolute-rail-ban policies.
  A screen negates movement effect only; an actually enemy-occupied entered
  hex stays prohibited. Test origin co-occupancy as a separately justified
  invariant, not as a verbatim §8.73 entry rule.
- Reject or defer an unscreened path without moving through it or stopping on
  the forbidden cell under the proposed atomic policy. Test normal land
  stop-on-entry independently; test any accepted source-authorized ZOC
  disclosure on failed and successful paths without exposing extra enemy state.
- Check two real, mixed and two dummy supply counters in one direction, then
  opposing directions and three counters; compare alternate precedence. Also
  test two stacking points and an independently invalid over-cap ordinary or
  mixed stack. Confirm action batching cannot manufacture a second same-direction
  stack, while a valid opposite-direction non-supply stack is not rejected by
  this **supply** cap alone.
- Exercise zero-supply ordinary travel, empty input, same-hex route, decline,
  stale/unknown path and replay. Assert no duplicate slot or cargo consumption;
  verify phase advancement only for the separately accepted phase command.
  Include owner, opponent and post-discovery views as well as §10.16's
  potentially required ZOC disclosure.

## Sources and limitations

Visually inspected hash-matched [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
printed/PDF pp. 15–16 (§§8.72–.75), 17–18 (§§10.16, 10.23, 10.26) and 43
(§§32.14, 32.18, 32.34), SHA-256
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`;
and [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
PDF p. 1 §8.73, SHA-256
`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.
Did not exhaust other occupancy, movement, discovery, stage, map, rail or later
errata rules. These observations do not establish actual six-turn reachability
or approve any source precedence. No runtime or content changed; no gameplay
tests were run.
