# Graziani landing-strip edges A: independent same-scan challenge

**Date:** November 18, 2026. Visual challenge of the [ten-cell reading](2026-11-18-graziani-landing-strip-edges-a.md), using the same map scan and map key, not independent map-edition evidence. Reviewed all ten supplied 300-dpi crops, with an additional 600-dpi Gazala crop. Compared all incident records in the pinned sides JSON. No proposal below certifies movement, current control, airfield capacity, or an unprinted connection.

## Priority: Gazala

Map prints **Gazala**, with its blue village dot inside B4933. The key identifies the two parallel dark lines as road, blue dot as village/bir, and blue fill as sea/coast. Road clearly crosses B4933–B4934 at the right vertical; no corresponding incident pin exists. This is a source-versus-pin discrepancy candidate, not a patch recommendation. The road's western approach meets the B4933 left/lower-left junction; its bend and return toward the dot meet the lower junction under blue lettering. Ink widths and label overlap prevent confident assignment among B4932, B4832 and B4833. Keep all three provisional rather than inferring three independent crossings. Coast crosses the upper-left diagonal; coastline and salt-marsh artwork lie near the upper-right diagonal, but exact coast-side assignment remains uncertain. Coast is irregular artwork, not automatically a pin-side rule. Rightward road continues across B4934 to the printed sheet edge; this crop alone does not establish its continuation onto another sheet.

## Other challenges

| Cell | Visual challenge / remaining ambiguity |
| --- | --- |
| B0707 | Track intersections and upper-right slope band support the four recorded feature comparisons. Yellow desert artwork inside the hex is not a side band. |
| C4108 | Track and wadi comparison is consistent with the exposed ink. Green wadi artwork runs along and across nearby grid; pin labels alone do not adjudicate its rule effect. |
| E3109 | Blue railroad and dark road overlap around the upper vertex; possible road junction at E3209 remains uncertain. Key distinguishes ridge from slope by the band's graphic, but overlap makes E3208 classification provisional. |
| A1816 | Map prints El Agheila. Blue dot sits very close to the lower vertex, apparently on the A1816 side of the junction at this scale; road ink straddles nearby diagonal boundaries. Retain uncertainty for both A1716 and A1717 and avoid treating a vertex touch as two independently established routes. |
| E3007 | Printed label is El Hamman. Road spur and blue lettering crowd the upper-left boundary; railroad crossings at E3006 and E3108 are visible. E3107 remains a candidate, not a certain road pair. |
| D3323 | Printed Fuka dot and track are visible; road follows near upper-left and upper-right junctions, with blue railway and name adding overlap. Keep road assignments near D3422, D3423 and D3322 provisional. |
| C4414 | Ridge bands and track support the recorded comparisons, subject to ink width and junction overlap. Dashed track rather than railroad crosses the target; railroad lies farther south. |
| B5410 | Road crosses both vertical sides; track exits toward B5311 and another runs very near the lower vertex. B5310 candidate remains uncertain. |
| D3520 | Map prints **Maaten Baggush**; reviewed row uses scenario spelling **Matten Baggush**. Dot lies close to the lower junction and dark road, dashed spur, blue railway and coast complicate attribution. Keep D3420, D3419 and D3519 provisional; coast geometry alone does not establish a particular pin convention. |

Programmatic incident-record comparison reproduced every pin listing in the reviewed table, including no B4933 incident records. This verifies transcription of the pin, not its completeness or map interpretation. `u` in the source table remains an observation at crop scale, not proof of absence. No other table entry is promoted to an independent route or sheet-edge certification.

## Reproduction

Map PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; pinned sides SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Crop coordinates and hashes for ten 300-dpi views are in the reviewed note. Gazala higher-resolution view: `pdftoppm -f 2 -l 2 -r 600 -x 11180 -y 3860 -W 2000 -H 2000 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/gazala-review-600`; resulting PNG SHA-256 `254cbb508b043794550ef1b96d425aaaba83ad26f820be498bd1567fa56e5c49` with local renderer. Map PDF and crops remain outside Git. No runtime/data changes or gameplay tests.
