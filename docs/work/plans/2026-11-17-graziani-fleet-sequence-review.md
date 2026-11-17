# Abstract fleet bombardment stage sequence: source review

**Disposition:** The proposed Assignment → Repair → abstract attack order is a
clearly labeled project choice, not a printed Land-only sequence. Retain the
unsupported admission gate for timing, secret-choice visibility, empty harbor,
repair completion and startup. Clarify the Campaign-dice test and the scope of
atomic commits. This is an informed visual rereading of the same edition, not
a separate edition or proof of an actual six-turn attack.

## Source checks and findings

1. **Sequence gap is real.** Land §5.2, printed/PDF p. 12, puts Weather,
   Organization, Naval Convoy Arrival and Commonwealth Fleet Phase before the
   first player's Reserve/Movement; the Fleet Phase lists **Assignment then
   Repair**. Land §32.73, printed/PDF p. 44, plots the Axis attack secretly
   during Fleet Assignment and says it occurs in the **Tactical Naval Movement
   Segment**, which §5.2 does not separately name. Air §33.0, PDF/printed p. 3,
   gives Fleet Assignment and Repair followed by Land Support Air Mission
   Completion before Reserve, also without that segment name. Air/Logistics
   p. 18's printed §42.7 says ignore §32.7 where Air rules govern fleet
   bombardment. These facts support the proposal's analogy and its explicit
   caveat; they do not source-certify an attack after repair. A separately
   sourced naval stage or placement before repair remains possible.

2. **Repair order has a real state consequence.** §30.37, printed/PDF p. 42,
   requires six consecutive OpStages in Alexandria to repair one Gun Rating
   level; moving early restarts the work. It does not in the inspected case
   specify how the missing §32.73 segment orders an attack against a
   same-stage repair completion, whether an additional hit resets otherwise
   continuous work, or exact completion-at-boundary bookkeeping. The proposed
   placement means a completed repair is reflected before the new damage;
   explicitly retain that outcome as policy and compare attack-before-repair.
   Do not infer that an initially undamaged ship has any repair progress, or
   give a ship away from Alexandria a valid six-stage repair merely because it
   later returns. §30.34–.36 ship type, Gun Rating, AA rating, damage,
   below/at/above-half status and sinking remain separate state fields.

3. **Two Campaign modifiers apply to two distinct single-die rolls.**
   §32.72 rolls a **secret one die once for scenario attack allowance**, with
   +2 if a Campaign Game. §32.74(A) separately rolls **one die per attack for
   the bomb-points column**, also +2 if Campaign. §32.74(B) then rolls **two
   dice read sequentially** for table damage; neither case states +2 to those
   result digits. The proposal's vector “Campaign modifier on both separate
   dice” should explicitly name the allowance die and column die to prevent
   applying it to each sequential result die. For example, if the Campaign
   profile is separately accepted, allowance die 1 gives 3 permitted attacks,
   and column die 1 selects column 3, not column 1; the subsequent sequential
   pair remains two valid d6 digits. Whether the short Graziani scenario uses
   the Campaign modifier is not determined here. Enforce **one actual attack
   at most per OpStage** as well as the scenario-wide allowance.

4. **Storm and empty harbor need different handling.** §32.75 forbids the
   attack if **map section E** has rainstorm or sandstorm. A known storm is a
   source prohibition, while unknown E weather is unresolved. §32.74(B)
   restricts actual damage allocation to ships in **Alexandria Harbor**, not
   Malta or all Commonwealth ships. The case does not expressly say whether
   a plotted attack with no ship there can execute, expend allowance, roll or
   be withdrawn. The proposal's unsupported empty-harbor gate is conservative;
   charging, refunding, forcing a zero or revealing the absence are additional
   choices. Similarly, treating a resolved 0-damage table result as consuming
   one attack is a reasonable explicit accounting proposal, not a separate
   printed 0%-specific sentence. Stage progress and user-visible outcomes must
   not turn hidden target presence into an oracle.

5. **Sealing both choices is policy, not text.** §32.73 explicitly makes the
   Axis plot secret during the Fleet Assignment Segment; it does not prescribe
   simultaneous commitments, what assignment information Axis receives, or a
   reveal order. The proposal's sealed Commonwealth assignment and Axis plot
   is a defensible information policy but can change the Axis's choice set.
   Specify who knows the pre-stage fleet state, allowed observations, timeout/
   decline behavior and the exact snapshot; a generic hidden error is not
   enough. Equal-observation tests apply only to histories for which no
   separately authorized disclosure differs. Do not expose private allowance,
   target or plot to an unauthorized side or intelligence projection.

6. **Commit boundaries need an explicit recovery contract.** The proposed
   Assignment, Repair and attack are separate authoritative boundaries. If
   Assignment/Repair were validly committed before an empty or unknown target
   blocks attack, “no partial attack” should not silently undo those earlier
   committed events, erase a completed repair or reapply them on retry. Keep a
   pending obligation and their journal identities, block dependent sequence
   progress where required, and atomically commit the attack's accepted die
   results, allowance debit, target selection and damage exactly once.
   Alternatively adopt an explicitly atomic whole-stage contract, but do not
   mix its rollback semantics with already visible phase events. The source
   does not prescribe either software transaction model.

7. **Corrected deployment is not attack-time presence.** Scenario §60.45's
   retained first paragraph refers to the Fleet Schedule, says no initial ship
   needs repair, and prohibits moving until GT2 OpStage 1. September errata
   §60.45, PDF p. 3, discards the later list starting “The following units are
   available.” Commonwealth Fleet Schedule §30.6, PDF p. 31, allows one of
   three marked cruisers and **two** of seven marked destroyers at Malta. The
   proposal correctly preserves these choices. Initial immobility and schedule
   do not decide a later harbor snapshot or how GT1 OpStage 1 initializes the
   missing segment and private allowance. No discarded scenario fleet list or
   no-ship assumption should be smuggled into the policy.

## Distinguishing vectors

- Apply Campaign +2 only to the separate §32.72 allowance die and §32.74(A)
  column die; verify §32.74(B)'s sequential d6 pair has valid digits and no
  extra modifier absent another accepted source. Compare an exhausted
  scenario allowance with a second attempted attack in the same OpStage.
- At the proposed boundary, compare a ship remaining in Alexandria with one
  validly assigned away, and a six-stage repair finishing there; compare the
  alternative attack-before-repair result. Test newly inflicted damage,
  subsequent repair progress and a ship moving before six stages without
  inventing an unprinted reset rule.
- E rainstorm or sandstorm prohibits execution; normal E permits further
  checks, unknown E blocks as unknown. Empty eligible harbor stays unsupported
  under the proposal, while optional no-plot and an executed zero-damage roll
  have separately versioned consequences.
- Crash/retry after sealed plot, after committed Assignment, after committed
  Repair, after a recorded draw and before allocation. Verify each prior event
  remains once, no damage/debit duplicates, pending target cannot expose
  hidden placement, and actor/observer projections follow the accepted
  disclosure policy. Test invalid choices of two Malta cruisers or three
  destroyers separately from legal choices.

## Sources and limits

Visually inspected hash-matched [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf),
printed/PDF pp. 12, 42, 44, §§5.2, 30.34–.37, 32.71–.75, SHA-256
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`;
[Air rules](https://spigames.net/PDFv10/CNA_AirGameRules.pdf), PDF/printed
pp. 3 and 18 §§33.0 and 42.7,
`9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4`;
[scenario booklet](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf),
PDF p. 4 / printed p. 28 §60.45,
`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`;
[Commonwealth charts](https://spigames.net/PDFv10/CNA_ChartsCWPlayer.pdf),
PDF p. 31 §30.6,
`f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981`;
and [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf),
PDF p. 3 §60.45,
`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.
Not an exhaustive scan of all naval rules, scenario overrides, timing cases,
weather details or later errata. No runtime/data changed and no gameplay
tests ran.
