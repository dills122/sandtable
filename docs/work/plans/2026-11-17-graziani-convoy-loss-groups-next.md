# Axis convoy loss groups: Land-only and detailed Air comparison

**Status:** source comparison and conditional examples, not an accepted
Land-only grouping algorithm, correction, implemented table or proof of
six-turn reachability. The inspected rules do not unambiguously answer every
grouping and rounding question. Keep unresolved cases gated rather than
inventing a publisher clarification.

## What each source actually says

| Source | Scope and observation | Limit |
| --- | --- | --- |
| Land §32.0, p. 43; §§32.61–.64, p. 44 | §32 applies when playing Land without Air and Logistics. Simplified Axis convoy cargo comprises Supply Units, Motorization Points and Replacement Points. Each convoy can be attacked separately; §32.64 uses the Air Bombardment table and expresses loss as a cargo percentage. | Invoking that table is not itself an instruction to import every detailed Air rule. The §32.61 cargo list does not establish a full loss-group schema. |
| Land §32.65(A) | At **10%**, applies to motorization and replacement points only; motorization fractions round down and replacement fractions up. | Supply Units do not fall under that 10% clause. The text does not precisely enumerate all replacement subgroups. |
| Land §32.65(B) | At **20% or more**, applies to all items conveyed, rounding fractions down, with a printed minimum concerning at least one point of each and a Supply Unit. | The minimum's exact scope, particularly which categories count as “each,” empty categories, and Supply Unit wording, needs an explicit ruling. Do not create cargo or subtract absent items. |
| Land §32.65 final paragraph/example | Tank and gun loss percentages are of the total **within each nationality**, not individual types. Its 10% example carries one Italian CV33 point and six German tank points of three types; it removes the CV33 and one German tank point, Axis player's choice. | The example demonstrates a nationality distinction and selection among German **tank types**. It contains no guns and therefore cannot distinguish one combined tank-and-gun pool per nationality from separate tank and gun pools. It also yields two losses from seven points, not a simple rounded 10% of the whole convoy. |
| Air §41.67, printed/PDF p. 12 | Detailed Air convoy losses round fractions up; it explicitly groups all truck replacement points together, Italian tanks together, German tanks together, Italian guns together and German guns together. | This is a particular detailed-game grouping. It does not say all replacements are one group, nor that individual tank models are separate groups. Whether it supplies Land-only defaults is a mode/precedence choice. |
| September errata §41.67, PDF p. 3 | Clarifies the stated percentage applies to **each** listed cargo type; 20% means 20% per type. Read with §41.67's explicit definitions of cargo types. | The clarification does not expressly amend §32.65, group tanks and guns together, define every abstract replacement category, or establish a Land-only 5% rounding rule. |

The same result table's Axis convoy row includes **5%** at bomb points 21–40
and sequential dice 55–64. Section 32.65 expressly singles out 10% and 20%
or higher. Section 32.64's general percentage language names supply units,
motorization and replacement points as subjects; how a 5% result becomes whole
losses and how replacement groups are formed remain open. This table cell
is a printed possibility, not proof that its column or such cargo is reachable
in this six-turn setup. The table also has 25%, covered by the literal “20% or
greater” branch if the result and mode apply. Preserve no-attack and table
zero separately from a 5% event with unresolved arithmetic.

## Competing grouping interpretations

1. **Separate nationality × equipment class:** Italian tanks, Italian guns,
   German tanks and German guns each form a group; individual models within
   each group are pooled. This mirrors explicit Air §41.67 and fits the Land
   example. It is a plausible Land-only policy if the Air categories are
   deliberately adopted, with Land's branch-specific rounding/minimum taking
   precedence where applicable. It is not stated as four categories in §32.65.
2. **Combined tank/gun group per nationality:** pool all Italian tanks and guns
   together and all German tanks and guns together, without model subdivision.
   This reads the Land phrase “tanks and guns, within each nationality” as one
   collective total. It fits the tank-only example but diverges from the
   explicit detailed-Air groups. Treat it as an alternative, not an established
   official interpretation.
3. **Broader replacement aggregate:** apply 10% to all replacement points as a
   single total, then allocate losses. This takes §32.65(A) in isolation but
   needs reconciliation with its own final nationality sentence and example;
   in the example seven points rounded up would produce one total rather than
   the two printed nationality losses. It cannot be applied unchanged to that
   example. A separate per-model rounding also conflicts with the final
   sentence's “not the individual types.”

For other replacement classes, including infantry or other gun categories,
neither the Land sentence nor the cited Air grouping exhaustively defines
every type or how nationality interacts with it. Air combines truck replacement
points; Land-only substitutes motorization and distinguishes it from
replacement points. Do not silently turn every replacement type into a
separate minimum-bearing group or put all of them into one group. Decide the
schema, equipment/model identity and Axis owner allocation after percentage
and source mode are selected. Ensure a choice draws only from actually carried
points and cannot exceed any group or convoy total.

## Conditional distinguishing vectors

All examples use synthetic cargo and stated percentages, not observed draws or
an assertion that the loads are legal or reachable. They intentionally do not
decide the missing group hierarchy.

- **Printed 10% example:** one Italian CV33 and two each German PzII, PzIIIJ
  and PzIVH; source states the Italian point and one German point are lost,
  with the German model selected by Axis. Per-nationality rounding-up yields
  `ceil(0.1×1) + ceil(0.1×6) = 2`. Separate rounding of each German model
  would lose three German points and contradict the example. This does not
  prove how Italian tank and gun points interact.
- **Mixed class, 10%:** one Italian tank replacement and one Italian gun
  replacement. If the two are separate nationality/class groups and each
  follows 10% replacement rounding-up, lose one of each. If one nationality
  pool, lose one point total, with allocation to be defined. If no category
  rule has been accepted, outcome is unknown; the printed tank-only example
  does not choose between them.
- **Mixed class, 20%:** four Italian tank and four Italian gun replacement
  points. A combined pool rounded down yields one point before the minimum;
  separate pools each round to zero before the minimum. If the minimum means
  one per *nonempty* pool, combined gives one, separate gives two. A different
  reading of “each,” or of the Supply Unit minimum, changes that conclusion.
  State the grouping and minimum policy before asserting a final loss.
- **5%:** one Supply Unit, one Motorization Point and one tank replacement.
  Applying detailed Air's per-category round-up by analogy could lose one of
  each; applying a different Land-only rule can differ. Neither result follows
  automatically from the 10% or 20%-plus exceptions. Avoid presuming that
  §41.67 automatically recognizes Land-only motorization or Supply Units as
  detailed-game cargo types.
- **Empty categories and conservation:** a convoy containing no guns cannot
  lose a gun; a convoy containing no Supply Unit cannot lose one. Check absent
  versus present categories, loss bounded by each group, attacker selection per
  convoy, loss distribution, and initial cargo = landed/surviving + destroyed
  under the accepted cancellation and timing rules. A conservation invariant
  rejects impossible arithmetic; it does not settle the source's minimum.

Before admission, resolve detailed Air applicability at 5%, 10% and 20%+;
nationality and equipment grouping; replacement categories; minimum and absent
groups; Supply Unit units/quantity; Axis type choice; and when destroyed versus
landed cargo is journaled. Version those decisions, selected table and scenario
mode. Test each branch and type against exact integer arithmetic, separate
convoys, deterministic replay, privacy and duplicate-delivery handling. No
gameplay tests were run.

## Sources and scope

Visually checked [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
printed/PDF pp. 43–44 §§32.0, 32.61–.65 (SHA-256
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`),
[Air rules](https://spigames.net/PDFv10/CNA_AirGameRules.pdf) printed/PDF p. 12
§§41.66–.68 (`9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4`),
[September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
PDF p. 3 §41.67 (`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`),
and [common charts](https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf)
printed/PDF p. 12 §41.5 (`51aa5a5bfdaca3d23794da71a45a830d98b798d060097cde6418126d6bd63bb0`).
Same-edition sources, not independent editions. This is a bounded review; it
does not exhaust other rules, chart corrections, later errata, legal cargo or
reachability. The earlier [convoy matrix](2026-11-13-graziani-abstract-convoy-matrix.md)
contains useful background but its “aggregated within each nationality” does
not itself resolve whether tanks and guns are combined.
An [independent review](2026-11-17-graziani-convoy-loss-groups-review.md)
confirms the table cell and examples, clarifies §32.64's general cargo scope,
and flags Air §41.66's “tens of percent” wording against the printed 5% and
25% cells. Neither wording conflict is resolved by choosing a source silently.
