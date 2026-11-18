# Rain-swollen wadi on road: decision input

**Decision needed:** Select how a road crossing's rain-specific breakdown
statement interacts with the rain rule treating road breakdown as track. The
source provides evidence for competing readings; none is selected here.

## Printed evidence and scope

[Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
§8.41, PDF/printed pp. 14–15, says that a vehicle crossing a rain-swollen
wadi by road pays an additional 2 CP and no additional breakdown, describing
the breakdown as still ½ BP. Its ordinary-weather discussion says a road
crossing uses the ½ BP road rate, while track crossing incurs 4 rather than 8
BP. Section 29.56, p. 41, says road hexes have track movement cost and
breakdown rate in rain; §21.37(c), p. 34, also treats road breakdown values as
track values in rain. These cases do not explicitly rank themselves.

The Terrain Effects Chart §8.37 in [Air rules](https://www.spigames.net/PDFv10/CNA_AirGameRules.pdf)
PDF p. 23 prints road **½ BP**, clear hex **4 BP**, and wadi hexside **+8 BP**
in distinct rows. Note 6 negates hexside terrain entry costs and breakdown
values on a road; note 8 halves hex and hexside breakdown values for a track,
with a vehicle down-escarpment exception. Note 10 permits rain-wadi crossing
at a road or railroad and treats railroad as road *for that purpose*, adding
2 CP; it does not give a breakdown total. Whether rain track treatment changes
road note 6's hexside negation is another precedence question. Do not infer
rail movement permission or extend railroad equivalence to unrelated rules.

## Candidate readings

| Choice | Road crossing during rain | Source tradeoff |
| --- | --- | --- |
| **Specific crossing rate** | Apply §8.41's stated ½ BP for this crossing. | Gives the specific rain-wadi statement its explicit rate; limits the broader road-as-track breakdown rule in this case. |
| **Track hex, road-negated side** | Apply track-equivalent breakdown for the entered hex, but keep the wadi-side increment at zero. | Gives §29.56 and §21.37(c) effect on the hex rate while retaining §8.41's no-additional-breakdown language and road note 6; does not use its explicit ½ as the final crossing rate. |
| **Track hex and side** | Apply track-equivalent breakdown to both the entered hex and the wadi-side contribution. | Gives track treatment broader effect; requires an explicit decision against retaining the road-negation and specific ½/no-additional language. Chart values make this computable only under further assumptions. |
| **Defer unresolved crossing** | Return unsupported until precedence and all inputs are known, with no movement or BP committed. | Avoids adopting a substantive reading while retaining the question for an explicit versioned rule. |

These are decision candidates, not proven exhaustive rules or official
corrections. Movement cost, breakdown accrual, actual breakdown checks and
entity eligibility are separate decisions. Land §32.55 exempts Motorization
Points from breakdown; the comparison below assumes a vehicle subject to it.

## Minimal numeric discriminator

Assume one otherwise legal road move across one rain-swollen wadi into a clear
hex, no other terrain features or modifiers, and a breakdown-eligible vehicle.
Assume also that clear's printed **4 BP** is the applicable base contribution,
track note 8 halves it to **2 BP**, and its wadi contribution is half of the
printed **+8 BP**, or **+4 BP**, when applying track treatment to that side.
These assumptions construct a test, not a source ruling or an actual map
witness. In particular, treating the track wadi figure as a separate increment
on top of the clear hex requires its own interpretation.

- Specific crossing rate: **½ BP** for the crossing under the stated reading.
- Track hex, road-negated side: **2 + 0 = 2 BP**.
- Track hex and side: **2 + 4 = 6 BP** under the stated additive assumption.
- Deferred: no BP or movement committed pending the decision.

The +2 CP in §8.41 is an additional crossing cost, not a complete movement-cost
total. Do not infer actual vehicle loss, rounding, a particular route, weather
history or a legal move from these illustrative numbers. Preserve both the
selected rule version and the individual hex and hexside inputs in any future
replay or test.

This synthesis follows the informed [chart comparison](2026-11-18-graziani-rule-priority-followup.md)
and [independent review](2026-11-18-graziani-rule-priority-followup-review.md).
Land SHA-256 `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`;
Air SHA-256 `9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4`.
Bounded source comparison, not an exhaustive errata search. No runtime/data
changes or gameplay tests.
