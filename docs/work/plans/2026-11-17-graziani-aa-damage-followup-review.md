# Independent review: fleet damage and AA

**Finding: revise disposition before use.** Land rules p. 41 §30.13 explicitly
states that AA points are reduced in ratio to Gun Points lost. This is a
printed proportional damage mechanism in the same Land rules, preceding
§30.34. The follow-up's claim that no inspected clause supplies a mechanism is
true only of its narrow stated inspection, but its proposed conclusion and
future vectors omit a directly relevant governing clause. Replace the general
absence claim with §30.13's rule, retaining uncertainty about precise
implementation and rounding. Do not infer that §32.74 is exempt: it inflicts
ship damage points and §30.34 takes ship damage in Gun Rating Points. This
connection is a reading across the sections, not a specific §32.74 AA example.

§30.12 distinguishes the two printed strengths and §30.13 relates AA loss to
Gun loss. §30.34 then says the Gun Rating falls accordingly and specifies Ajax
with Gun 4 taking two points ends with Gun 2 and AA 4. If Ajax began at AA 8,
that example fits a one-half proportional reduction; the example alone does
not establish its starting AA. No Ajax counter was independently identified in
this pass, so do not assert its face AA from the example or assume all ships
have the same AA-to-Gun ratio. §30.13 does not spell out rounding, intermediate
fractional ratings, or recovery of AA on Gun repair. It supports proportional
loss, not an invented universal one-for-one subtraction. Establish how to
handle nonintegral values and repeated damage/repair before implementation.

The history-sensitive §30.36 reading is supported: falling below half triggers
port restriction and repair must exceed half to lift it. Thus a face-4 ship
reaching 2 from 3 does not trigger that rule, while one repaired to 2 after
falling to 1 remains restricted. This is a rules-state distinction, not
historical evidence that Ajax or any ship actually followed either path. Zero
Gun sinks a ship; the printed p. 45 commando addition is conditional. Six
consecutive Alexandria Operations Stages repair one Gun level under §30.37;
§30.38 permits AA fire during repair, without specifying AA restoration. The
plan's caution about timing, overflow, availability, and scenario reachability
remains appropriate. §32.74(b) explicitly lets Axis distribute points in any
manner among ships in Alexandria Harbor; it does not specify handling points
beyond available Gun or an empty harbor.

**Required future vectors:** include proportional AA loss alongside Gun loss
for separately sourced face ratings; include a nonintegral ratio, successive
hits, and repair as unresolved policy/source questions. Keep Gun 4 → 2 and
AA 4 as the printed Ajax outcome, without treating AA 4 as an initial value.
Preserve distinct entry-to-half and repair-to-half cases and independent
scenario reachability proof. No runtime rule or universal rounding policy is
approved by this review.

## Evidence and scope

Visually inspected [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
printed pp. 41–42 and 44: §§30.12–.13, 30.34–.38, 32.71–.75 and 32.81–.83.
Local PDF SHA-256 `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
Checked text of [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
for numbered §§30, 32 and 41.5; no numbered correction to §30.13 or §30.34 was
found. Local errata SHA-256
`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.
This was not an exhaustive search of other editions, naval/air rules, counter
sheets, or later clarifications. No gameplay tests or runtime edits.
