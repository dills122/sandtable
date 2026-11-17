# Graziani: simplified Axis convoy research matrix

**Status:** first-reader source research and candidate test vectors, not an
admitted rule, complete table transcription, actual action or reachability proof.
Original [Land rules](https://spigames.net/PDFv10/CNA_LandGameRules.pdf) printed
p. 44 (§§32.61–32.65), the [Axis chart](https://spigames.net/PDFv10/CNA_ChartsAxisPlayer.pdf)
PDF p. 34 (§32.66) and scenario p. 27 (§60.37) were visually inspected. Source
and errata need an independent reading, and the complete bombardment result
table and six-turn starting conditions remain open. The [common chart](https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf)
printed p. 12 (§41.5) was subsequently inspected for one result. See the broader
[logistics crosswalk](2026-11-12-graziani-abstract-logistics-crosswalk.md).

**Independent source follow-up:** [fresh reading and comparator](2026-11-17-graziani-independent-reviews.md#transport-convoy-and-rail)
confirm that §32.66 requires the qualifying division itself to be non-shell,
as well as another non-shell division at the stated relative position. The
packet's first reading omitted the former qualifier from its abbreviated field;
retain both. The broader table, minimum-loss and reachability questions remain.

| Step | Observed source boundary | Contract and evidence needed |
| --- | --- | --- |
| Plan | §32.61 includes that turn's supply units, motorization and replacement points in Axis convoys; items may be divided among available lanes. It removes tonnage limits for this simplified convoy. Scenario §60.37 separately requires pre-start plans for the remainder of September and limits those plans to lanes 2, 3 and 6. | Authenticate carried-in plans, origin, arrival slot, private owner choice and later lane availability. Do not generalize the scenario's initial restriction to all future turns without source review, or apply no-tonnage language to unrelated transport. |
| Allocate | §32.62 limits a single lane in one Naval Convoy Stage to three supply units and 20 motorization points; replacement-pool charts independently limit total items by type for the Game Turn. | Test 3/4 and 20/21 boundaries, multiple lanes and stages, Game-Turn totals, category identity and no duplication. Find the applicable replacement chart and exceptions before setting any total. |
| Attack choice | §32.63 lets Commonwealth attack each arriving convoy separately, regardless of map weather; Axis announces lanes used. | Model opponent choice and per-convoy identity without disclosing other private cargo/plans beyond the applicable observation rules. Test a declined attack, multiple convoys and weather independence. Verify what else each side can observe. |
| Bomb strength | §32.64 and chart §32.66 select bomb points from the convoy route and a qualifying Commonwealth division's westward position. The chart notes that a qualifying division is complete rather than a shell and is in, or in the same east–west hexrow as, at least one other Allied non-shell division. | Define map/hexrow identity, division completeness, coordinate ordering and the no-qualifying-division case from reviewed sources. Test ties, adjacent rows, shells, different lanes and map seams. Do not silently use the worst, first or lowest row. |
| Result | §32.64 directs sequential two-die lookup on the Air Bombardment Combat Results Table using that bomb-point column; §32.65 modifies losses. | Independently verify every reachable result cell, die order, modifiers, RNG consumption and loss distribution. Compute each attacked convoy separately, retain provenance and replay exactly. The fact that detailed Air Game is omitted does not remove this abstract lookup. |
| Loss and delivery | At a 10% result §32.65 applies losses to motorization and replacement points, rounding the former down and the latter up. At 20% or more it says the result applies to all cargo with fractions rounded down, subject to a printed minimum-loss instruction; tank and gun losses are aggregated within each nationality, not by type. | Clarify the minimum for absent categories and supply units, category/nationality aggregation, player choice among types, provenance and simultaneous delivery. Conserve initial cargo as delivered plus destroyed; verify duplicate retries do not attack or deliver twice. Do not borrow the separate monthly motorization-loss rule. |

## Candidate arithmetic checks

These are deliberately conditional on the source percentages actually being
reachable and on the unresolved minimum-loss interpretation. They do not encode
the bombardment table or adjudicate empty categories.

- A convoy with two supply units, 19 motorization points and one replacement
  point, at **10%**, would lose one motorization point and one replacement point
  under the stated rounding, with neither supply unit lost by that 10% clause.
- A convoy with three supply units, five motorization points and five replacement
  points, at **20%**, is a useful minimum-loss boundary: ordinary rounding gives
  zero supply units and one of each point category; the source's minimum text
  must be interpreted before asserting the supply loss.
- For tank or gun replacement losses, compare two types of the same nationality
  with separately rounded types: the source requires aggregation by nationality.
  Test different nationalities separately and retain any permitted owner choice.

Before implementation, bind the applicable chart/version and accepted rulings
to the scenario identity. Preserve pre-start planning, hidden state, category
totals, no-op proof and the distinction between destination-port restrictions,
convoy-lane capacity and this special no-tonnage rule. A locally passing arithmetic
test cannot prove that any particular chart coordinate or convoy is reachable.

## September lanes and destination ports

The original Air/Logistics PDF p. 25, §56.18 names the three lanes admitted
by scenario §60.37 for the pregame plan for the rest of September:

| Lane | Origin | Destination |
| --- | --- | --- |
| 2 | West Sicily | Tripoli |
| 3 | Italy | Benghazi |
| 6 | Italy | Tobruk |

Land §32.61 explicitly refers to §56.0 for lane assignment. Air/Logistics
§§56.11–16, 56.25 say lane identifies its origin and destination, is assigned
when planning, and cannot be changed; a convoy is cancelled before sailing if
the Commonwealth captures its destination. This dynamic cancellation requires
actual port control at execution, not an assumption from initial setup. Errata
§56.25 permits allocation to any operation stage unless otherwise designated;
it does not clearly require that stage be fixed during pregame planning.

Scenario §60.7 (PDF p. 4) starts all ports at listed efficiency except Tobruk.
The common chart §55.3 (PDF p. 15) lists Tripoli at 10 and Benghazi at 3;
the scenario prints Tobruk at 7 while the chart lists a maximum of 5 and
Air/Logistics §55.25 says the San Giorgio reduces its efficiency by three.
The separate [Tobruk conflict](2026-11-12-graziani-ruling-decisions.md#open-conflict-tobruk-port-efficiency)
remains open. Errata §55.11 directs players to the chart for what ports can
ship; chart §55.3 handles Axis replacement cargo by stacking capacity, not
tonnage. Whether and how port capacity limits apply to abstract supply units
remains unresolved because Land §32.61 specifies no tonnage limitation. Do not
derive delivery legality from these initial values alone.

## Five-percent result requires its own disposition

The printed §41.5 Axis Naval Convoys row has a **5%** result for bomb points
21–40 and sequential dice 55–64. Section 32.65's special clauses explicitly
address 10% and 20% or greater; §32.64 states the general percentage result,
but those passages do not specify 5% rounding.

Original Air/Logistics p. 12 §41.67 describes the detailed Air Game's convoy
loss allocation and rounds fractions up. September errata p. 3 clarifies that
the percentage applies to each cargo type; the original defines tank and gun
groups by nationality. This is a **candidate** 5% rule, not yet an applicable
Land-only ruling: §32.64 invokes the results table and §32.65 supplies its own
10% and 20%-plus modifications, while detailed Air play is omitted. Review
whether §41.67's allocation and rounding survive that abstraction and how its
general rule interacts with the explicit §32.65 exceptions. Section 47.6 also
has a different convoy abstraction and must not be substituted without checking
its game-mode applicability.

Verify all table notes, general rounding rules, errata, applicability of that
column and actual divisional positions before choosing. Test 5%, 10% and 20%
independently; a valid 10% example does not settle 5%. A machine-assisted
search of the Land and Air/Logistics scans located the above candidate; OCR
omissions and unreviewed text mean it does not prove no other rule exists.
