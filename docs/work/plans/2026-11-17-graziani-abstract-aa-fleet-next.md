# Graziani Land-only: abstract fleet bombardment and anti-air

**Status:** bounded source inventory for LA-19, not a complete naval, AA,
weather or six-turn reachability contract. Detailed Air is absent in the
Land-only profile, but §§32.7–.8 retain fleet bombardment and alter anti-air
state. No implementation or readiness admission follows from this reading.

## Source scope

Visually inspected hash-matched original [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf),
printed/PDF pp. 12, 42, 43–45, §§5.2, 30.34–.37, 32.0, 32.71–.84, SHA-256
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`;
[scenario booklet](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf),
PDF pp. 3–5 / printed pp. 27–29, §§60.22, 60.41, 60.45, 60.92,
`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`;
[Commonwealth charts](https://spigames.net/PDFv10/CNA_ChartsCWPlayer.pdf),
PDF p. 31 §30.6,
`f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981`;
[common charts](https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf),
PDF/printed p. 12 §41.5,
`51aa5a5bfdaca3d23794da71a45a830d98b798d060097cde6418126d6bd63bb0`;
and [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf),
PDF pp. 2–3, §§32.0, 41.5, 60.45,
`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.
Errata text search also covered `32.7`, `32.8`, fleet, anti-air and
bombardment; no direct correction naming §§32.71–.84 was found. This is a
bounded same-edition source pass, not a complete errata or other naval-rule
review. Scans stay outside Git pending rights.

## Initial anti-air modifications

These are source-directed modifications for Land-only abstraction at setup;
apply to actual source-identified units/points and applicable arriving or
replacement state only after their timing and representation are defined. Do
not erase an entire non-AA unit merely because it has an AA rating, and do not
manufacture initial zeroes from unknown inventories.

| Source | Exact affected type | Source-directed effect and boundary |
| --- | --- | --- |
| §32.81, Commonwealth | All anti-air **units**; all light and heavy anti-air **TOE Strength Points**, wherever assigned; anti-air **Replacement Points**. Captured Axis anti-air TOE points are also explicitly addressed. | Remove those Commonwealth anti-air units and points from play; captured Axis anti-air TOE points are immediately removed. Scenario §60.41 explicitly lists 9th Heavy AA, 15th Light AA and 57th Light AA among the units placed anywhere on D/E: these named AA units are affected, subject to resolving setup application. No complete count of all assigned AA points or replacement pools was established here. |
| §32.82, German Axis | German anti-air units with ID `z` and `bb`; assigned light anti-air TOE Strength Points and light Replacement Points. | Remove those units and light categories; heavy anti-air TOE Strength Points and heavy Replacement Points are unaffected. Do not extend the German rule to all Axis nationalities or assert a particular German unit is in the six-turn start. |
| §32.83(1), Italian Axis | Italian anti-air units, IDs `ss`, `tt`, `uu`. | Remove those units from play. Actual applicable starting counter identities and any reinforcements need source review. |
| §32.83(2), Italian Axis | Anti-air TOE Strength Points assigned to emplaced-gun units, ID `pp`. | Remove the **AA points**, both light and heavy as encompassed by the text; do not delete the emplaced gun unit or its unrelated guns solely on this rule. |
| §32.83(3), Italian Axis | Artillery units that may be assigned AA points, ID `kk`. | Lose assigned **heavy** AA TOE Strength Points. The case does not say delete the artillery unit or all light points; validate actual attachments and point types. |
| §32.83(4)–(5), Italian Axis | Heavy AA Replacement Points (75mm and 90mm); light AA Replacement Points. | Heavy replacements are lost; light replacements may be brought in at one per month. First-month timing, rounding of any entitlement, assignment and actual receipt remain separate; no specific extra point is inferred. |
| §32.84, all above | AA TOE Strength Points and AA Replacement Points versus non-AA points that have an AA rating. | Restrictions do **not** affect non-AA TOE or Replacement Points merely because they have an anti-air rating. Preserve ID/type, nationality, assigned relationship and loss reason. |

The source says “removes” and “lost”; this inventory does not decide whether
future arrivals of every removed class are suppressed, arrive and immediately
leave, or are reflected in a modified reinforcement schedule. That lifecycle,
monthly light-replacement cap, captured-point timing and replay identity need a
versioned setup/arrival contract. Any source-proven absent class should be
identified only after the actual schedule and captured state are checked.

## Axis bombardment of Commonwealth fleet

- **Available activity:** §32.71 gives the **Axis** a limited number of
  bombardment attacks on the Commonwealth fleet in **Alexandria Harbor**.
  Section 32.72 has Axis secretly roll one die for the number permitted in the
  scenario, adding two if playing a Campaign Game. This is a **scenario-wide
  allowance**, not one die per ship, turn or stage. Determine whether the
  Campaign Game modifier applies to the selected scenario profile explicitly;
  this check does not assume it applies to the six-turn Graziani scenario.
  Resolve initial roll timing, persistence and visibility without exposing the
  secret result.
- **Stage and weather:** §32.73 permits at most one attack per Operations
  Stage, secretly plotted during the Commonwealth Fleet Assignment Segment;
  it occurs in the Tactical Naval Movement Segment of that OpStage. §32.75
  forbids the attack when **map section E** has rainstorm or sandstorm. A
  forbidden storm, a player declining a permitted attack, no ships in the
  harbor, an unsupported plot and an actual 0-damage attack are distinct
  states. The rule does not specify here whether an attempted storm/empty
  harbor plot consumes allowance; retain that as an explicit decision.
- **Sequence mismatch to reconcile:** Land §5.2, PDF/printed p. 12, lists a
  Commonwealth Fleet Phase with Assignment and Repair segments, but does not
  separately name the §32.73 Tactical Naval Movement Segment. Do not silently
  reorder an attack before the fleet choice, assume the target stayed in
  harbor, or reapply it in both places. Decide how the abstract timing maps to
  the normal stage, actor A/B order and a snapshot of Alexandria's ships.
- **Resolution:** §32.74(A) rolls a die for the bomb column of the Air
  Bombardment and Secondary Barrage Target Combat Results Table, counting
  columns from the left, adding two for a Campaign Game. §32.74(B) rolls two
  dice sequentially, yielding **damage points** on ships in Alexandria Harbor
  only; Axis distributes them as desired. The parenthetical reference to
  Chariot Raid Table §30.46 describes damage points; it does not alone import
  the separate Chariot Raid's 1941 timing or once-per-campaign restriction.
  Exact target eligibility, division of excess damage, empty target and point
  conservation need their own accepted source policy. No plane inventory,
  fuel cost, sortie or inference call is specified by this abstract case; do
  not invent one or claim every other naval requirement has been examined.
- **Damage consequences:** §30.34 says damage reduces a ship's **Gun Rating**
  by damage points; its example states the resulting AA rating but does not
  establish the starting AA rating. A later [source follow-up](2026-11-17-graziani-aa-damage-followup.md)
  and [independent review](2026-11-17-graziani-aa-damage-followup-review.md)
  identify §30.13's proportional AA reduction; rounding and restoration remain
  open. §30.36 prevents a
  ship below 50% of face Gun Rating from leaving port until repaired to more
  than 50%; losing its entire Gun Rating sinks it and makes it unrepairable.
  The exact equality-at-50% interaction is not resolved by those phrases.
  §30.37 repairs one Gun Rating level after six consecutive OpStages in
  Alexandria, without engineer or supply expense, and resets work if it moves
  early. Damage allocation, AA rating, ship status and repair progress must
  be modeled as different source quantities, not as generic fleet hits.

For a synthetic non-Campaign profile with column die `1`, common chart §41.5's
1–20 bomb-points column and the row covering Commonwealth Fleet give
sequential `62 → 0` damage and `63 → 1` damage. These are chart-coordinate
checks, not a historical roll or proof any specific ship is present. Errata
§41.5 changes the **barrage-points header** and flak terminology, not these
bomb-point cells. Verify valid sequential d6 digits and all chart columns
before admitting a complete table.

## Scenario setup and six-turn boundary

Scenario §60.22 runs GT1 OpStage 1 through GT6 OpStage 3. §60.92 is explicitly
**Air and Logistics abstracted / Land Game Only**, so §32 applies; the section's
specific supply/motorization replacements do not erase §§32.7–.8. Scenario
§60.45's **first paragraph**, preserved by September errata §60.45, says one
battleship, three cruisers and seven destroyers are initially available as per
the Fleet Schedule, with no repair needed, and cannot move until the first
OpStage of GT2. Errata instructs readers to ignore information starting at
“The following units are available”; do not use that discarded list to invent
additional ships, deployment or a different movement restriction.

The source fleet schedule §30.6, Commonwealth charts PDF p. 31, lists GT1
OpStage 1 deployment: Valiant, Arethusa, Dido, Euryalus, Airedale, Bedouin,
Jervis, Lance, Ledbury, Mohawk and Nubian. Its footnotes allow one of the
three asterisked cruisers and two of the seven daggered destroyers to deploy
at Malta; the remaining deployment is Alexandria. These are owner choices,
not proof every listed vessel is in Alexandria at attack time. Its next listed
fleet reinforcement is 3/8, beyond GT6, but this bounded schedule reading does
not exclude a source override or another way a ship's status changes. Initial
schedule, fleet immobility and later movements make an Alexandria attack a
**candidate**, not proof a particular attack, target or 18-stage sequence is
reachable under all setup/sequence/weather rules. Whether the GT1 OpStage 1
assignment/plot exists at scenario entry and whether an earlier event was
initialized remain timing questions; the first normal stage's placement and
plot cannot be assumed solely from the track.

## Proposed admission tests

- Apply each AA modification only to its matching nationality, unit or TOE/
  replacement type; check Commonwealth three listed AA units, Italian `pp`
  preserving the gun and `kk` preserving non-heavy/non-AA points, German
  `z`/`bb` with heavy AA retained, captured Axis AA and non-AA points with AA
  ratings. Unknown source type is not silently classified. Cover initial,
  arrival, monthly light-replacement and replay semantics separately.
- Seed the secret scenario allowance once, preserve modifier/profile and
  visibility, and enforce at most one plotted attack per OpStage. Check storm
  and normal E weather, allowance exhausted, optional decline, empty harbor,
  target moving during the ambiguous stage, and source-backed resolution
  timing without inventing a no-op outcome.
- Test column die and sequential d6 pair, including `1`/`62 → 0` and
  `1`/`63 → 1` in the non-Campaign example; consume exactly the accepted
  rolls once and preserve private plot/allocation projections on retry/replay.
- Test legal Axis distribution over actually present ships, gun-rating loss,
  zero-rating sinking, below/at/above 50% port status, six-stage repair and
  interruption. Keep Malta choices and discarded §60.45 material distinct.

No runtime data, source content, readiness flag or test was changed. No
gameplay tests were run. The [independent source review](2026-11-17-graziani-abstract-aa-fleet-review.md)
corrected the Malta destroyer allowance to two and flags §30.34's AA-rating
wording conflict; no choice of actual deployment or damage result is inferred.
