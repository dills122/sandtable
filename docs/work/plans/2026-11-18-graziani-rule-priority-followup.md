# Rain-wadi breakdown: chart cross-check

**Result:** The terrain chart separates the **hexside** breakdown contribution from the road or track **hex** value. That distinction narrows the existing §8.41/§29.56 question, but does not choose which breakdown rate applies to a road hex during rain. No implementation ruling follows.

## Question and prior work

[Weather effects](2026-11-18-graziani-land-weather-effects.md) and its [review](2026-11-18-graziani-land-weather-effects-review.md) identify the tension: Land §8.41 says a rain-swollen wadi crossed on a road adds 2 CP but no additional breakdown, specifying that it remains ½ BP; §§29.56 and 21.37(c) say road breakdown is treated as track breakdown in rain. [Road stacking source work](2026-11-17-graziani-road-stacking-next.md) already examines chart notes 6–11; its [review](2026-11-17-graziani-road-stacking-review.md) already records that note 10 also covers a railroad for this purpose. Neither finding establishes precedence between the cited rain breakdown statements.

## Fresh comparison

Visually inspected the [Air rules scan](https://www.spigames.net/PDFv10/CNA_AirGameRules.pdf), PDF p. 23, chart §8.37, against the Land passages cited above. The chart is printed there as well as in the map scan referenced by the earlier road work. This is another reproduction in the same source set, not independent edition evidence.

- The chart's wadi row has **+8** in its breakdown-value column. Its road row prints **½** and refers to note 6: a road negates all hexside terrain entry costs and breakdown values. The road value and wadi-side increment are separately printed quantities.
- The track row refers to note 8, which halves terrain-feature entry costs and all hex and hexside breakdown values, with an exception for the CP and breakdown of moving a vehicle down an escarpment. Note 10, attached to the wadi CP entries, permits crossing during rain at a road or railroad, treating railroad as road **for this purpose**, for two additional CP. It does not itself specify a rain breakdown total.
- Thus §8.41's “no additional” wording addresses the wadi-side addition, while its explicit “still ½ BP” also states a total/rate. If §29.56 makes the road hex use the track breakdown rate, eliminating the separate wadi-side addition alone does not establish that the remaining rate is ½. Conversely, note 8's general treatment does not itself choose precedence over §8.41's explicit rain example. The underlying hex terrain and other applicable features matter when evaluating a track rate; the chart does not supply one universal replacement number.

The explicit railroad exception was already documented and should not be presented as newly discovered. This comparison does not establish that a particular wadi is crossable, authorize railroad movement, establish scenario weather, or settle whether a particular entity incurs breakdown. Land-only §32.55 separately exempts Motorization Points from breakdown.

## Source and limits

Air PDF SHA-256 `9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4`; [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) SHA-256 `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`, printed/PDF pp. 14–15, 34 and 41. Chart notes 6, 8 and 10 were inspected visually in the Air scan; Land comparisons rely on the source readings recorded in the linked weather notes. Prior road work reports a September 1979 chart erratum; this follow-up did not independently recheck errata or later editions. No source-wide absence or precedence claim is made. No runtime or data changes, gameplay test, or admitted rule decision.
