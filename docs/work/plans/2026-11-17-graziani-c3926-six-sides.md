# Buq Buq C3926: six-side source check

**Finding:** Original Map C shows paired dashed unfinished-road crossings toward C3825 and C3927 and a single dashed track toward C3826, matching the three literal pinned entries. Shoreline, water, Buq Buq lettering and a crowded dot/junction obscure portions of the remaining sides or endpoints. This is a local visual reading, not a complete clear-side or legal-movement certification.

## Six sides

[Map C](https://spigames.net/PDFv10/CNA_Maps.pdf) PDF p. 3 prints `3926` in the coastal hex and places the blue Buq Buq dot near its lower-right vertex; the name crosses the hex boundary and neighboring text. The map key on PDF p. 1 distinguishes a single dark dashed `Track`, paired dark dashed `Unfinished Road`, blue land/water `Coast`, and the blue `Village/Bir` dot. The table uses **page-image** directions and approximate pixel positions within the 500-dpi crop below, not compass or legal movement directions.

| Neighbor | Side in crop | Original visible observation | Pinned literal pair |
| --- | --- | --- | --- |
| C4025 | Upper-left diagonal, about `(780,520)`–`(940,425)` | Thin grid under blue water; no track or wide band identified on exposed segment. | No entry found. |
| C4026 | Upper-right diagonal, about `(940,425)`–`(1095,520)` | Mostly water/grid; shoreline and route/name ink converge near the eastern endpoint. | No entry found. |
| C3927 | Right vertical, about `(1095,520)`–`(1095,700)` | Paired dark dashes run across near `(1095,580)` toward C3927, partly overprinted by `Buq Buq`; the dot is close to the lower endpoint. | `unfinished-road`. |
| C3826 | Lower-right diagonal, about `(940,785)`–`(1095,700)` | Single dark dashed route leaves the dot/junction and passes into C3826 close to the eastern vertex; ink makes exact grid intersection crowded. | `track`. |
| C3825 | Lower-left diagonal, about `(780,700)`–`(940,785)` | Paired dark dashes cross around the western/central portion, aligned with the road on either side. | `unfinished-road`. |
| C3925 | Left vertical, about `(780,520)`–`(780,700)` | Shoreline intersects the vertical side; exposed land and water grid sections are thin, with no crossing route identified. | No entry found. |

The blue dot near a vertex is interior evidence for a named village/bir, not an automatic fortification or evidence that all adjoining sides have a feature. Water on exposed side portions does not by itself classify an entire hexside as all-sea; determine that and any relevant unit-entry or ZOC effects from full source and rules. Dark route marks, label and shoreline cover short sections of grid. No literal data record is not proof of source feature absence; local crossing is not a full-route certificate.

## Reproduction and provenance

Map SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. Reproduce crop: `pdftoppm -f 3 -l 3 -r 500 -x 7100 -y 6050 -W 2100 -H 1600 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/c3926-sides`; PNG SHA-256 `56b1d2c2484f9f15a0dd7d85ca51be71da8b5f5960103a1d85e02f69c0620f1c` (renderer-dependent). Compared literal pairs in `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json`, SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Same scan, not an independent edition or complete map audit. No runtime changes or tests.
