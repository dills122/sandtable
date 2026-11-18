# Abstract motorization losses: September dash meaning

**Finding:** The inspected original §58.5 chart and related instructions do **not** unambiguously define the September 1940 dash as zero loss, inapplicable, or unavailable. Its position among other dash-only months supports several readings but supplies no numeric value. Preserve it as unspecified pending an accepted versioned interpretation or further authoritative source; do not silently manufacture a 0% loss or suppress a monthly event.

## Source and chart identity

Visually checked [common charts](https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf) PDF/printed p. 13 §§46.3 and 58.5, [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) PDF/printed p. 44 §§32.51, 32.57 and 32.59, [scenario/abstract-air rules](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf) PDF p. 1 / printed p. 25 §§58.42–.44, and [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf) pp. 1–4. SHA-256 verified: common charts `51aa5a5bfdaca3d23794da71a45a830d98b798d060097cde6418126d6bd63bb0`; Land `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`; scenario `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`; errata `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.

```sh
pdftoppm -f 13 -l 13 -r 240 -png -singlefile /private/tmp/sandtable-common-charts.pdf /private/tmp/motorization-dash/chart
pdftoppm -f 1 -l 1 -r 200 -png -singlefile /private/tmp/sandtable-scenarios.pdf /private/tmp/motorization-dash/scenario1
```

Chart PNG SHA-256 `e0e0b345eaee480883d5e70f3654ed25466fa4d27bca5fb11a69e43fc49256d5`; scenario PNG `dd882f972d473a9b32d29843a5f4fe3190d649b64a07acb99b87a25d9c74aa86`. Renderer bytes may differ; source hash and page locate evidence. Scans/crops remain outside Git. Prior [source follow-up](2026-11-17-graziani-rules-followup.md) and [independent review](2026-11-17-graziani-motorization-dummy-review.md) were known, so this is informed same-scan inspection, not blind or another edition.

Land §32.57 refers to an Abstract Motorization Point Loss Chart at §32.59. Its §32.59 heading instead names an Abstract Truck/Motorization Loss Chart, while common chart p. 13 labels the table §58.5 Abstract Truck Loss Chart. Scenario §58.42 expressly directs physical-truck abstract-air losses to §32.57 and §32.59 percentage figures; §58.44 refers to an Abstract Truck Loss Chart. The §58.5 legend describes percentages of Commonwealth and Axis Motorization Points destroyed, in `Nr/Nr` order. These links strongly support using the printed table as a candidate for §32.59 but do not themselves constitute an official renumbering.

## Dash evidence and alternatives

The printed 1940 row has a single dash per month January–September, then `6/2` October, `5/3` November and `4/4` December. The 1943 row prints `2/4` in January and dashes February–December. The chart's two legend lines explain the numerator/denominator side order and North Africa scope, but give **no dash key, footnote or example**. The dashes at both ends are consistent with a non-applicable or unavailable period, a no-loss convention, or other editorial shorthand. Their placement does not determine which interpretation applies to September, when Graziani begins partway through the month. The printed table does not give a pair such as `0/0` for September.

The adjacent, separately keyed §46.3 anti-aircraft result table also uses dashes for cells that provide no die range while giving an explicit zero-result row. That local usage shows a dash can denote absence of a listed range in **another** table; it does not define §58.5's dash as inapplicable or prove a global convention. Conversely, a dash in a different chart cannot be transplanted as a numeric 0% in §58.5 without source support. No direct §32.57/.59 or §58.5 dash clarification was found in the four-page errata. Errata §32.0 warns that abstract rules were untested; it does not define the symbol. Later material or indirect corrections may exist.

Section 32.57 says losses for the **previous month** are determined in the first Naval Convoy Stage of each month and instructs use of the chart for the appropriate month. Whether to index the event month or elapsed loss month remains a separate decision; under the existing previous-month planning interpretation an October event would encounter September's dash. The scenario begins during September; whether a first September event or pre-scenario time is processed is another distinct startup question. Neither calendar assumption changes the symbol into a known percentage. The §58.42 example computes a hypothetical 10% physical-truck loss and its proportional distribution but names no month and does not explain the dash. Its physical-truck mode and allocation rules must not be silently applied to Land-only motorization.

## Consequences

Represent source dash, explicit numeric 0%, not-applicable period and missing data distinctly. If a selected monthly event needs this dash, keep the result unknown/fail closed until a reviewed policy states whether and why to treat it as zero or no event. Do not infer October `6/2` as September's rate, a rounding rule, a removal outcome, or an actual monthly event from this chart alone. A future review should test candidate month keys, startup, dash semantics, numeric side order, stage snapshot and denominator, rounding, removal shortfall, replay and side-safe disclosure. No runtime, source content, readiness or gameplay tests changed.
