# Commonwealth convoy-qualifying divisions: source review

**Disposition:** The [division audit](2026-11-17-graziani-convoy-divisions-next.md)'s
three-slot arithmetic and stated source baselines are supported **under its
explicit default/co-location assumptions**. None is a fixed actual convoy
snapshot or proof that the two-division condition is or is not met. Reconcile
initial owner organization, permitted substitutes, all other divisions, current
locations and phase timing before deciding a bombardment row.

## Sources and counting

Visually rechecked [Commonwealth OA/charts](https://spigames.net/PDFv10/CNA_ChartsCWPlayer.pdf)
PDF pp. 6, 15, 18, 22 and 24; [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
pp. 9, 17 and 30 §§4.25, 9.26–.27 and 19.45–.46; and
[scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf)
PDF p. 3 / printed p. 27 §60.41. Their SHA-256 respectively match
`f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981`,
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`, and
`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`.
The comparison's other source hashes were also matched; this review did not
independently repeat its full errata search. Prompted same-edition reading,
not a blind or separate source edition.

§9.26 makes a division a shell at **50% or less** of maximum assignable
brigade-equivalents attached. §9.27 excludes an attached shell brigade from
that count. For a three-slot division, one qualifying brigade is 1/3 (shell),
two is 2/3 (not shell). The denominator is the applicable authorized maximum,
not only presently deployed brigades. §19.31 shows three brigade-equivalent
slots for GT1–18 type-I Commonwealth armour and the relevant infantry
division. For an ordinary three-battalion brigade, fewer than two attached
battalion-equivalents gives a shell; §9.26 also has a **separate two-battalion
brigade exception**, so do not generalize the two-of-three shortcut to every
brigade. Its battalion shell test and §9.27 must be applied as well.

Land §4.25 supplies the default attachment of assigned units starting with
their parent in a hex, subject to specific exceptions. It does not turn
separately located or future units into present attachments. §19.45's
substitution is conditional on its own missing/assigned-unit and organizational
requirements; additional attachments under §19.5 are a distinct rule. Neither
a substitute's existence nor its ability to fill a specific missing slot is
established simply by a counter being in Egypt.

## Roster cross-check

- **7th Armoured.** OA p. 6 lists 4th and 7th Armoured Brigades and 7th
  Support Group. The 4th's 6 RTR and 7 Hussars are `D`, each with ten tanks;
  code `g` chart p. 22 permits ten. Under the source-default attachment at
  D3612 they supply two of three type-I tank-battalion slots. The 7th's 1 RTR
  and 8 Hussars are each listed at seven tanks, but §60.41 places 1 RTR at
  C3520, leaving only 8 Hussars on the stated default. Separately listed support
  elements likewise cannot be silently treated as attached at D3612. Thus
  baseline 1/3 is a shell, **if** the stated default hierarchy and no legal
  setup substitution/reattachment are selected. It is not a mandatory final
  initial organization or a conclusion about a later convoy snapshot.
- **4th Indian.** OA p. 15 lists the three named infantry battalions under each
  of 5th and 11th Indian Brigades as `D`, `N`; §60.41 lists the division at
  D3615 with 31st Field Artillery and 7th Indian Brigade detached. OA places
  the 7th Brigade and its battalions at `1/5`. Under the stated normal-strength,
  default attachment baseline the two present brigades are non-shell, making
  2/3 and a non-shell division. Actual §59.2 organization, constituent
  identity/strength and the snapshot must still be validated; one may not
  treat the baseline as an unconditional owner choice.
- **2nd New Zealand.** OA p. 18 lists 4th Brigade and three battalions `D`,
  5th at `1/35`, 6th at `3/6`; scenario puts the HQ, 4th Brigade and listed
  support at Alexandria E3613 and/or E3714. At most one of those **assigned
  brigade choices** is initially present, hence at most 1/3 under that
  restricted baseline, even if 4th qualifies. Two-hex placement can affect
  attachment; supporting MG, cavalry and artillery are not themselves
  independent brigade-equivalents. This is not proof against an otherwise
  legal substitute or an evolved organization. The §20.11 example conflicts
  with the 6th Brigade's scheduled date; neither datum places that brigade at
  initial setup, and the conflict must not generate a duplicate arrival.

The 6th Australian Division's separately listed training and actual
attachments also matter to the complete pool. Training by itself is not a
shell test. The chart lookup requires a non-shell division east of or in the
same east–west row as **another** non-shell division; its denominator and
pair must be checked at the actual convoy event, not frozen from these three
initial baselines. Arrival/order, alternate assignments and co-location remain
source and state gates, not assumed impossibilities.

Proposed checks: exact one/two-of-three and battalion-strength boundaries;
three versus two-slot brigade exception; shell brigade exclusion; same-hex and
two-hex setup; valid and invalid substitute; another qualifying division;
first/last-player ordering and snapshot timing; unknown attachment rather than
false. These are unexecuted vectors. No runtime data or readiness flags changed,
and no gameplay tests ran.
