# Axis convoy loss grouping: source review

**Disposition:** retain the [grouping comparison](2026-11-17-graziani-convoy-loss-groups-next.md)
as an unadmitted research packet. Its printed example, 5% cell and conditional
arithmetic check out. Qualify its statement that the *eligible categories* for
5% are open: Land §32.64 already describes its result as a percentage of the
supply units, motorization points and replacement points assigned to that lane.
What remains unclear is how to turn 5% into whole losses, group replacements,
and reconcile §32.64 with any applicable Air rounding—not whether §32.65(A)'s
10%-only exception automatically removes supply units from a 5% result.

## Source check

Visually rechecked hash-matched [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
p. 44 §§32.61–.65; [Air rules](https://spigames.net/PDFv10/CNA_AirGameRules.pdf)
p. 12 §§41.66–.68; [common chart](https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf)
p. 12 §41.5; and [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
p. 3 §§41.5, 41.67. SHA-256 values, respectively:
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`,
`9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4`,
`51aa5a5bfdaca3d23794da71a45a830d98b798d060097cde6418126d6bd63bb0`,
`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.
Same scans as the packet; prompted inspection, not a blind transcription or
independent edition. No exhaustive review of other corrections, modes, legal
loads or scenario reachability.

## Findings

1. **Table and 5%.** Chart §41.5's Axis Naval Convoys row gives 5% for bomb
   points **21–40** and sequential dice **55–64**. Dice values denote two
   sequential d6 digits, not every integer in the interval; e.g. 56, 61 and 64
   are valid, 57 and 60 are not. The printed table also contains 25%. The
   erratum to §41.5 corrects the *barrage-points header* and changes a flak
   label; it does not expressly remove that 5% bomb-points cell. §32.64 names
   the Air Bombardment table and describes the percentage result for all three
   listed simplified cargo categories. §32.65 modifies 10% and 20% or more,
   but does not explicitly spell out 5% rounding. Air §41.67 has round-up
   language and grouping for detailed cargo; whether and how it supplies a
   Land-only default is a policy/source-hierarchy question. Air §41.66 calls
   the table result tens of percent, while the chart prints 5% and 25%: preserve
   that additional wording tension rather than silently erase the table cells.
   A printed cell does not establish that the §32.66 bomb-point lookup can
   produce its column in a particular six-turn position.
2. **10% branch and example.** Land §32.65(A) applies 10% only to
   motorization and replacement points, rounding motorization fractions down
   and replacement fractions up. Its example explicitly carries one Italian
   CV33 and two each of German PzII, PzIIIJ and PzIVH; the result removes the
   CV33 and one Axis-selected German tank point. `ceil(1/10) + ceil(6/10) = 2`
   reproduces the example, while per-German-model rounding would remove three
   German points. The example has no guns, so it does not decide whether
   tanks and guns are one nationality pool or separate nationality/class
   pools. Other replacement categories and allocation choice still need
   definitions. The example establishes the stated German type choice, not
   necessarily a universal chooser rule for all cargo.
3. **20% or more.** §32.65(B) says all conveyed items are affected, fractions
   rounded down, and prints a minimum in wording referring to at least one
   point of each and a Supply Unit. The ambiguous minimum cannot create items
   absent from cargo. With four Italian tanks and four Italian guns at 20%, a
   combined pool yields `floor(8/5)=1` before minimum; separate pools yield
   `floor(4/5)=0` each. Under the *stated hypothetical* minimum of one per
   nonempty pool, the results become one versus two. Other readings produce
   other outcomes; neither the grouping nor minimum scope is established by
   that arithmetic.
4. **Detailed Air comparison.** §41.67 groups all truck replacement points,
   Italian tanks, German tanks, Italian guns and German guns as the listed
   respective types, with fractions rounded up. Errata §41.67 says the
   percentage applies to each cargo type, not a share divided among types.
   It does not explicitly amend Land §32.65 or define every simplified cargo
   class. Even if Air supplies defaults, Land's explicit 10% and 20%+
   modifications require separate reconciliation. Do not assume detailed
   physical trucks and abstract motorization are interchangeable for grouping.
5. **Conditional vectors.** At 10%, a one-tank/one-gun Italian load loses two
   if separate groups each round up, or one if combined and rounded up; the
   allocation of the combined loss needs its own rule. At 5%, one supply unit,
   one motorization point and one tank replacement yield one loss in each only
   if an accepted Land interpretation applies per-category ceiling to all
   three; §41.67 alone does not establish that mapping. At any percentage,
   losses must be nonnegative, bounded by actually carried units in each
   accepted group, and reconcile with arrivals and destruction. No attack and
   a zero table result are distinct events; neither is a 5% default.

Before implementation, version the table and mode; determine 5% whole-loss
rule, grouping, minimum, absent categories, selection authority and exact event
ordering. Check a 5% cell with valid sequential rolls, 10% mixed tank/gun
cargo, 20% and 25% with fractional groups, empty cargo categories, distinct
convoys, replay and filtered views. These are unexecuted synthetic vectors,
not proof of cargo legality or six-turn reachability. No runtime or readiness
changes were made.
