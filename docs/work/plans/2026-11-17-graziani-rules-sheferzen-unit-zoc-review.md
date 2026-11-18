# Sheferzen and 2nd Rifle ZOC baseline: source review

**Disposition:** Confirm the [unit/ZOC note](2026-11-17-graziani-rules-sheferzen-unit-zoc.md)'s chart readings and conditional raw strengths. The newly inspected original counter-sheet scan also shows printed stacking values **0** for `Scfzrn` and **1** for `2RflBde`. Under standalone assumptions neither meets §10.11's greater-than-one stacking criterion; Scfzrn also fails §10.15's raw-defense threshold. Co-located units and setup choices remain unverified, so this is not a complete initial ZOC or combat ruling.

## Source evidence

Visually inspected [scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf) PDF p. 3; [Axis chart](https://spigames.net/PDFv10/CNA_ChartsAxisPlayer.pdf) PDF pp. 4, 14 and 26; [Commonwealth chart](https://spigames.net/PDFv10/CNA_ChartsCWPlayer.pdf) PDF pp. 5–6 and 22; [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) PDF/printed pp. 16–19; and [counter sheets](https://spigames.net/PDFv10/CNA_Counters.pdf), PDF pp. 1 and 4. SHA-256 verified:

- Scenario `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`.
- Axis `9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca`.
- Commonwealth `f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981`.
- Land `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
- Counter sheet `bc3e6c60131e8c813481510a5a0c991d027bdaaa7e0aa83f413737c22d62dae9`.

Scenario §§60.31 and 60.41 place Sheferzen Garrison at C3419 and 2nd Rifle Brigade at C3320 with ten medium trucks. Axis OA p. 14 lists Sceferzen Oasis Garrison Bn, `Scfzrn`, `dd`, `N`, `D`, morale −3, and locates the garrison at C3419. Axis characteristics §4.46b p. 26 places `dd` under infantry company-equivalents, with maximum TOE 1, close assault 2/2, CPA 15, AA 1, barrage 5, anti-armor 1 and vulnerability 2. The battalion name and company-equivalent chart are a source distinction, not a correction.

Commonwealth OA p. 6 lists `2nd Rifle Brigade`, `2 RflBde`, `n`, `N`, `D`. Commonwealth characteristics §4.46a p. 22 puts `n` under infantry battalion-equivalents, maximum TOE 6, close assault 1/2 and CPA 10+. Both OA keys define `N` for non-tank/non-gun units as maximum TOE and `D` as deployed. Source-baseline arithmetic under §11.3 gives raw defense `1 × 2 = 2` for the garrison and `6 × 2 = 12` for 2nd Rifle, before scenario or state changes. Land §9.22 separately cautions that unit names and game sizes can differ.

## Counter marks and qualification

Counter section 2, PDF p. 1, shows `Scfzrn` with `G`, `II` and a printed `0` left of its symbol. Counter section 7, PDF p. 4, shows `2RflBde` / `7Spt/7`, `II` and a printed `1`. This independently corroborates the [counter-face check](2026-11-17-graziani-sheferzen-counter-check.md). Scfzrn’s `II` battalion face mark conflicts with the `dd` company-equivalent chart category; neither is silently corrected here. The counter layout and Land §§9.11–.16 establish the numeric field as stacking value; `G` denotes garrison treatment. Reproducible crops:

```sh
pdftoppm -f 1 -l 1 -r 600 -x 4800 -y 5480 -W 650 -H 650 -png -singlefile /private/tmp/sandtable-counters.pdf /private/tmp/sheferzen-zoc-review/axis-counter
pdftoppm -f 4 -l 4 -r 600 -x 1450 -y 1100 -W 700 -H 750 -png -singlefile /private/tmp/sandtable-counters.pdf /private/tmp/sheferzen-zoc-review/cw-counter
```

Crop SHA-256 respectively `03226366c46ff20472d11409cd1584ec8b01e8fcde047636b43bb120b86abfc1`, `6b3f5c9de9bdfd4895a44de9ad533e59a2fb82380001c7f47b0b7e2015e27e9a`. Bytes may vary by renderer; source hash and crop coordinates identify the scan. Copies stay outside Git.

Section 10.11 requires a combat unit above battalion-equivalent, i.e. more than one stacking point, or a hex with more than one stacking point. Section 10.15 independently withholds ZOC when units in a hex total less than ten raw defensive close-assault points. On the **standalone** printed counter and baseline-strength assumptions, Scfzrn has zero stacking and raw 2; 2RflBde has one stacking and raw 12. Thus neither satisfies the standalone §10.11 size/stack condition, even though the rifle unit satisfies the raw threshold. Land §9.16's special assigned-garrison stacking treatment and any interaction with §10.11 need explicit policy rather than extrapolation; the printed zero here does not prove all cases. Other admissible units in either hex can change aggregate strength or stacking, and accepted placement, formation, cohesion, terrain/entry and applicable exceptions remain prerequisites.

The [independent map reading](2026-11-17-graziani-map-sheferzen-initial-contact-review.md) found adjacent printed cells but did not certify their common edge clear. Do not assert positive directional ZOC, an initial attack obligation, or the legality of a final setup from the two counter readings. Review co-location and setup, garrison exceptions, unit strength, edge coverage, phasing and §§10.31–.32 separately. Candidate tests should distinguish isolated printed counters from enriched stacks and actual post-setup state.

This is a separate source check after reading the unit note, using the same scans; it is not blind or a new edition. The counter scan's recorded hash has no independent edition baseline and does not establish rights. No runtime, readiness or gameplay tests changed.
