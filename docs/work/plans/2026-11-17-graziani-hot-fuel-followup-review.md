# Hot fuel follow-up: independent review

**Disposition:** Agree with [follow-up](2026-11-17-graziani-hot-fuel-followup.md): retain exact-only proposal as conditional, unadmitted interpretation. Neither applying nor waiving §29.34 to abstract Supply Unit fuel, nor a rounding direction, is established by these passages. No runtime or readiness change.

## Source check

Visually inspected [Land](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) printed/PDF pp. 41 and 43 and [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf) PDF p. 2. SHA-256 respectively `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99` and `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`. Same edition and scans as follow-up; this is an independent reading, not independent authority.

- §29.34 reduces all fuel and water levels by 5% during Weather Determination, including dumps and trucks; it excludes wells/oases and fuel or water actually being consumed by a unit. Transport alone does not establish actual consumption. §29.1 excludes named off-map regions from weather and gives a specific Map E rule for Allied off-map airfields; §29.31 places hot weather on all game-map sections.
- §32.0 says it applies when playing the Land Game “without the Air and Logistics Games”; it replaces supply expenditures with listed costs. §32.11 also says only its following supply rules apply. §32.12 represents abstract Supply Units by Supply Dump counters; §32.15 retains tracked fuel and ammo. Those facts support both readings in follow-up, but neither counter representation nor the word expenditure settles whether a weather reduction survives. The printed activation wording should be preserved when specifying mode combinations; the cited passage alone does not expressly enumerate every mixed-game combination.
- Errata p. 2 §32.0 warns abstract rules were untested, without directing precedence. Its §29.1 and §29.61 entries address dating and the weather table, not this fuel interaction. This page check does not establish absence of indirect, later or other-edition corrections.

## Arithmetic and admission gates

For a single eligible balance of 50, 5% is 2.5 and the exact remainder 47.5. This arithmetic assumes the effect and subject mapping first; it is not an integer adjudication. Two 10 balances yield 0.5 each before rounding; aggregate loss is 1 before allocation. Per-unit exact-only gating, per-location grouping, aggregation and all rounding choices require separate policy. Section 29.34 supplies no grouping or rounding instruction in the inspected text. Follow-up's bounded glossary check is not an exhaustive search; this review did not repeat it or inspect the scenario setup.

Whole-effect blocking on an unknown subject or fractional loss prevents partial mutation but can reveal private stock or dummy identity through whether, when, or how play stops. Redacting diagnostics alone cannot cure an observable stop. Before admission, define authorized observations and a pending or uniformly exposed behavior that does not distinguish equal-authorized histories, including legal actions, events, retries, timing and operator/intelligence projections; test those paired histories. If no such behavior is feasible, keep the effect unadmitted until a disclosure policy is accepted.

For a drawn hot result, persist the result, stage identity, policy version and RNG cursor durably across retry/crash; do not redraw or advance with unresolved effects. A pre-draw gate may block non-hot outcomes too and must itself meet the privacy requirement. Classify all eligible stocks, loaded and off-map holdings, consumption exceptions and applicable water before whole-effect commit. A fractional representation also needs versioned serialization and later spending/comparison rules. These are design gates, not printed rules.

No gameplay tests run. No exhaustive rulebook, chart, errata or later-edition search performed.
