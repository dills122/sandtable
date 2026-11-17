# Map D corrected escarpment: D2228 / D2328 direction

**Result:** The September erratum establishes an escarpment on the D2228/D2328 shared side, but does not state which side is high. The printed side itself has no directional splash mark. Neighboring printed marks are consistent with D2328 being high, but do not prove the omitted mark's direction. Preserve direction **unknown** in source admission unless an explicitly labeled interpretation is selected or additional authoritative evidence establishes it.

## Inspected evidence

* Original [Map D](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 4, around printed `2228` and `2328`, SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. The two numbered hexes share a diagonal side. The scan shows no complete heavy escarpment symbol along that shared side. By contrast, the nearby D2328/D2227 side has a dark escarpment mark with splash toward D2227; the laterally adjacent marking is evidence for **that** printed side, not a directional glyph on the missing one.
* [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf), PDF p. 1, **MAPS** entry, SHA-256 `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`, explicitly says D2228/D2328 should have an escarpment. It supplies the feature identity and side; it does not supply a splash direction or select an endpoint as high.
* [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), printed/PDF p. 14 §8.35, p. 15 §8.42 and p. 18 §10.21, SHA-256 `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`. §8.35 explains splash contours lie on the **down** side. §8.42 distinguishes vehicle up versus down movement; §10.21 prohibits ZOC projection through escarpment hexsides without an up/down distinction.

On the neighboring printed D2328/D2227 side, the splash-toward-D2227 reading makes D2227 the down side **for that side**. Extending that direction to D2228/D2328 is a plausible contour-continuity inference, not a mark present on the corrected edge. Even a topographically plausible plateau boundary does not prove the orientation of an errata-supplied feature. The existing candidate data records `up: D2328`; this audit identifies its source status as an inference needing independent evidence, not a verified source transcription. It does not propose changing data or deciding whether an external map transcription is authoritative.

## Consequences and distinguishing tests

1. Both directions of the corrected D2228/D2328 edge have an escarpment for §10.21 ZOC. Presence alone may support that specific symmetric effect, subject to certified geometry and applicable state; it does not prove either hex is occupied or reachable.
2. For direction-dependent §8.42 vehicle movement, compare proposed `up = D2328` against the alternative `up = D2228`. Block/mark unknown if authoritative direction is required but unsupported; do not silently use the missing splash as a drawn mark. A downward vehicle crossing also requires an actual connecting track and applicable movement/breakdown rules, not merely an escarpment.
3. Compare a source record whose direction is explicitly **unknown** with a versioned inferred-direction candidate and an explicitly proven printed symbol. Do not collapse `no known direction` into `no escarpment`; do not treat absent feature records on other sides as clear.
4. Test exact side endpoint labels, reciprocal edge representation, corrected-feature provenance and replay. The map is one scan edition and no full neighboring-edge or route survey was performed.

No runtime or map content changed; no gameplay tests ran. Independent inspection of the original high-resolution scan and any later official direction correction remains useful, but cannot turn a selected inference into a printed source fact.
