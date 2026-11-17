# Simplified Axis convoy 5% loss: bounded planning choice

**Status:** `gra-axis-convoy-5pct-v1-proposed` is a project interpretation for
review, not an official correction, admitted runtime contract, proof of a
legal load or six-turn reachability. The source prints a 5% outcome but does
not unambiguously specify how its fractional Land-only cargo losses round.
Adopt the complete policy and its prerequisites explicitly or return
`unsupported`; never silently turn 5% into no loss.

## Evidence and remaining source gap

Rechecked hash-matched [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
pp. 4 and 44 §§2.1, 32.61–.65; [Air rules](https://spigames.net/PDFv10/CNA_AirGameRules.pdf)
p. 12 §§41.66–.67; [common chart](https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf)
p. 12 §41.5; and [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
p. 3 §§41.5 and 41.67. SHA-256 respectively
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`,
`9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4`,
`51aa5a5bfdaca3d23794da71a45a830d98b798d060097cde6418126d6bd63bb0`, and
`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.
See [source comparison](2026-11-17-graziani-convoy-loss-groups-next.md) and
[independent check](2026-11-17-graziani-convoy-loss-groups-review.md). Same
scans, not a separate edition; no exhaustive source-wide or later-errata search.

Land §32.64 invokes the Air Bombardment results table and expresses its result
as a percentage of Supply Units, Motorization Points and Replacement Points
in the convoy lane. The §41.5 Axis Naval Convoys row has 5% for 21–40 bomb
points and sequential dice 55–64, with valid d6 digits only. Land §32.65
modifies 10% and 20% or greater and says tank/gun losses are based on totals
within nationality rather than individual types; it has no explicit 5%
rounding instruction. Air §41.67 rounds each cargo type upward and groups
Italian tanks, German tanks, Italian guns and German guns separately; errata
clarifies applying the percentage to each type. Air §41.66's “tens of percent”
wording does not neatly describe the table's 5% and 25% results. Land §2.1
says its §§6–32 contain all rules necessary for Land play, while the Land
procedure refers to the Air results table. Invoking that table does not by
itself prove that detailed Air §41.67's rounding and cargo schema survive in
Land-only mode. No general rounding rule was identified in the inspected
introductory or convoy passages; this bounded check cannot prove none exists
elsewhere.

## Proposed versioned hierarchy

1. Determine a legal, attacked **individual** Axis convoy and its 5% table
   result using accepted lane, qualifying-division and dice rules. Under
   §32.63 map weather alone does not bar this simplified attack.
   Separate no attack and a table zero. The printed cell is a possible table
   result, not evidence that its bomb column or cargo is reachable.
2. For this 5% branch only, deliberately **adopt by analogy** Air §41.67's
   per-group upward rounding, after Land §32.64's abstract cargo subjects and
   §32.65's nationality/not-model distinction. This choice fills a gap; the
   source does not explicitly prescribe it for Land-only mode. Compute exact
   integer `ceil(group_count / 20)` for each nonempty accepted group, zero for
   an empty group. Cap losses by actual carried group quantity; do not create
   a minimum-bearing absent item. Do not aggregate different convoys or stages.
3. The proposed **only supported** groups are: real Supply Units as one group;
   Motorization Points as one group; Italian tank replacement points, German
   tank replacement points, Italian gun replacement points and German gun
   replacement points as four separate groups, pooling models within each
   nationality/class. Assigning abstract supply and motorization categories to
   Air-style groups and separating tank/gun classes are project choices. An
   unclassified replacement, uncertain nationality, unfamiliar cargo type,
   dummy status or ambiguous membership is `unsupported`; don't guess, drop it
   or pool it with a convenient group. Whether an apparent convoy can carry a
   dummy Supply Unit is a separate legality question, not an assumed exclusion.
4. Actual model selection for a nonzero group must be made only under an
   accepted, authorized allocation rule and may not exceed the carried
   quantities. Land's printed 10% example assigns the Axis player the choice
   of one German tank model; extending that choice to 5%, guns or other groups
   is a **separate policy**. Until resolved, a group with multiple possible
   allocations is pending rather than arbitrarily selecting a model. A
   singleton group has no choice ambiguity.
5. This 5% proposal does not change Land's express 10% and 20%+ branches,
   their own unresolved group/minimum issues, or import the whole detailed Air
   game. At 10%, Land expressly excludes Supply Units and uses different
   motorization rounding. At 20%+ it directs rounding down with a stated
   minimum; do not apply this 5% ceiling to those branches.

Preflight legality, complete cargo identity, group membership, loss allocation
and all relevant sources before delivery. Persist an already-drawn result and
RNG cursor if the effect cannot complete; never reroll, partly remove cargo,
deliver survivors, or leak private cargo through reason/timing metadata.
Commit result, selected losses and survivors atomically with deterministic IDs
and ruleset identity. Unknown classification is not an empty group. Detailed
Air's broader types, infantry replacements, other nationalities and any
unverified carried cargo remain outside this bounded schema.

## Distinguishing arithmetic and tests

These are synthetic conditional examples, not executed tests or legal
historical loads:

- One real Supply Unit and one Motorization Point, with no other cargo:
  proposed 5% yields one of each lost, `ceil(1/20) = 1`. A floor policy yields
  zero of each; the printed 10% branch would exclude the Supply Unit and round
  that motorization fraction down. This stark difference is why the hierarchy
  requires acceptance rather than being labeled as source fact.
  It also means the proposed 5% outcome can be **more destructive than 10%**
  for the same cargo. This follows from combining the proposed default with
  the printed 10% exception; review must assess that non-monotonic consequence
  before selecting a policy rather than assuming it is historically intended.
- Three real Supply Units, 20 Motorization Points, six German tank
  replacements and two Italian gun replacements: group counts produce one
  loss in each group, total four. If German tanks contain several models,
  group count is still one but allocation is unresolved until a chooser rule
  is accepted. The source caps of three Supply Units and 20 Motorization Points
  per lane do not alone prove this replacement load or convoy legal.
- One Italian tank plus one Italian gun point gives two losses under separate
  class groups; one combined Italian equipment group rounded up gives one.
  Neither result is established by the tank-only 10% example. An absent German
  gun group contributes zero. An infantry replacement point or unknown
  nationality blocks the bounded policy rather than disappearing.
- Check 1, 19, 20 and 21 points in a permitted synthetic group: ceiling gives
  1, 1, 1, 2; obey actual lane/cargo limits before admitting a real convoy.
  Check each valid sequential roll at the 5% boundary, separate convoys,
  declined attack, replay and crash after draw, unavailable allocation, cargo
  conservation and filtered player/intelligence observations. A table zero
  must not acquire this minimum.

Independent review should check source-wide rounding and errata, meaning of
5% under Land-only rules, group mapping, allocation authority, exact event
order and legal loads. The [independent source review](2026-11-17-graziani-five-percent-review.md)
confirms the cell and arithmetic, and flags weather independence, grouping,
allocation, overflow and hidden-state/event safeguards for admission. No
gameplay tests or runtime changes were made.
