# 1st RTR baseline and counter: independent source review

**Disposition:** Confirm the [1st RTR source note](2026-11-17-graziani-rules-first-rtr-zoc.md)'s seven-A9 baseline and 21 raw defensive points. An independently rendered original counter scan is visually accessible and shows `II`, `1`, `1 RTR`, `7/7`: the numeric mark is one stacking point. Under the isolated printed-counter baseline, that does not meet §10.11's greater-than-one stack criterion, although 21 meets §10.15's raw threshold. This does not settle the actual setup stack, terrain, ZOC or combat.

## Evidence

Visually checked [scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf) PDF p. 3 / printed p. 27 §60.41, [Commonwealth charts](https://spigames.net/PDFv10/CNA_ChartsCWPlayer.pdf) PDF pp. 6, 22 and 27, [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) PDF/printed pp. 7–8 and 17–19, and [counter sheets](https://spigames.net/PDFv10/CNA_Counters.pdf) PDF p. 4. SHA-256 reverified: scenario `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`; Commonwealth `f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981`; Land `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`; counters `bc3e6c60131e8c813481510a5a0c991d027bdaaa7e0aa83f413737c22d62dae9`.

Scenario §60.41 places `1st RTR (7/7)` at C3520 with five medium trucks. Commonwealth OA p. 6 lists 1st Royal Tank Regiment, `1 RTR`, `g`, `7 × A9`, `D`; §4.46a p. 22 gives `g` as tank battalion-equivalent, CPA 25, maximum 10, with separate US-tank qualification. Commonwealth §4.47 p. 27 gives A9 Cruiser CPA 25, AA 1, anti-armor 3, armor protection 1, close assault 3/3, fuel rate 2 and breakdown adjustment 1R. Land §11.3's raw calculation gives `7 × 3 = 21` defensive points at the stated baseline. Seven of ten is above §9.26's below-half battalion-shell condition; later state and scenario corrections remain separate.

Original counter PDF p. 4, counter section 7, British left block, fourth row, third counter, between `7 Hus` and `8 Hus`, shows `II`, unparenthesized `1`, `1 RTR` and `7/7`. Land §4.21 labels the numeric position as stacking value; p. 8 identifies `II` as battalion. This directly fills the counter-face gap in the companion note:

```sh
pdftoppm -f 4 -l 4 -r 600 -x 550 -y 1100 -W 800 -H 750 -png -singlefile /private/tmp/sandtable-counters.pdf /private/tmp/first-rtr-review/counter
```

Crop SHA-256 `c1bcf44aa33b1f8286c914dff098a7f1cf8a9bdddd994df4ec79955e549a4cd4`. Renderer bytes may vary; PDF hash and crop parameters identify source. Image and scan remain outside Git.

Land §10.11 requires a combat unit above battalion-equivalent, expressed as more than one stacking point, or a hex with more than one stacking point. §10.15 separately denies ZOC to unit(s) in a hex below ten raw defensive Close Assault Points. Therefore the **isolated, unchanged source baseline** clears the raw test but its one-point battalion counter alone does not clear the size/stack test. Other valid co-located units and representation can change the hex total; do not assert that the actual scenario stack is isolated. Cohesion, exact current strength, §10.21 entry and terrain, phase and applicable exceptions remain separately necessary.

The [side check](2026-11-17-graziani-map-first-rtr-side.md) supports a visible track on the C3520/C3419 diagonal; it is not complete ZOC or route certification. Synthetic tests should distinguish seven A9 from the maximum ten, printed one versus enriched stack, 9/10 raw threshold, source and policy versions, and unknown or certified map edges. This review followed the companion note and the parent-supplied locator, using the same scan, not a blind reading, new edition or rights grant. No runtime, readiness or gameplay tests changed.
