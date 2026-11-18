# Map E worked-distance chain: independent visual review

Reviewed `2026-11-18-graziani-distance-example-consistency.md` against the original scan, not a different edition. Map PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; Air rules PDF SHA-256 `9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4`. Map E is PDF page 5; Air worked example is printed page 24.

I visually inspected the Map E grid at native rendered resolution. Each consecutive pair below shares a full printed hexside, including the sides under or alongside map ink:

| Pair | Visual result |
| --- | --- |
| E2629–E2729 | shared side |
| E2729–E2828 | shared side |
| E2828–E2928 | shared side |
| E2928–E3027 | shared side |
| E3027–E3127 | shared side |
| E3127–E3226 | shared side |
| E3226–E3326 | shared side |
| E3326–E3425 | shared side |

The printed grid supports eight adjacent steps. In this local regular grid each shared-side step changes the first numeric component by at most one; 26 to 34 therefore requires at least eight steps. The pictured chain attains that bound. This is a grid-distance observation, not a claim about a legal ground route, terrain costs, or external shortcuts. Some labels and boundaries intersect route or river ink; the neighboring numbered cells and their outlines remain traceable.

The Air example actually prints **zero** for the E2629-to-E3425 leg and totals 149. Replacing that one leg with eight while leaving all other printed terms unchanged gives 157 as a counterfactual calculation, not an official correction or proof of the author's intended route. This same-scan review confirms the local geometric inconsistency; it does not resolve map seam ownership or independently verify the other legs or distance chart.

Reproduction: render the map at 300 dpi with `pdftoppm -f 5 -l 5 -r 300 -x 4500 -y 4500 -W 2100 -H 2000 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/distance-chain-review`. The resulting crop's SHA-256 is `72ecaa66392be962d381176e0cf6073e8043524cf2f0d39427114f3ec39170f9`. I inspected overlapping full-resolution crops of that image as well as the whole reduced image; resampling did not provide independent source information.
