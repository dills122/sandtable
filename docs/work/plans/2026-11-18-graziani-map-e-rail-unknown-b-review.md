# Map E rail: independent visual critique of six pairs

**Date:** November 18, 2026. **Disposition:** Concur with one provisional source-positive and five unknown readings in [unknown B](2026-11-18-graziani-map-e-rail-unknown-b.md). This is a separate visual challenge of the same original scan, informed by that document and existing crop coordinates, not blind or independent-edition corroboration. No map-data change follows.

Inspected page 5 of the [original map PDF](https://spigames.net/PDFv10/CNA_Maps.pdf), its page 1 terrain key, and the cited crops. The key distinguishes blue cross-tied rail from paired road lines. A positive means a printed centerline crossing that specific shared side; unknown does not mean absence.

| Pair | Review | Visual basis and limit |
| --- | --- | --- |
| `E2422–E2523` | **Provisional source-positive** | The blue centerline crosses the diagonal separating labeled 2422 and 2523 at approximately middle-crop `(480, 650)`, before the common vertex with 2423 at approximately `(500, 662)`. The centerline is visibly on either side of that diagonal, but a blue cross-tie and thick grid ink overlap the near-vertex area. The limited separation supports a provisional crossing, not precise survey or branch connectivity. |
| `E2428–E2429` | **Unknown** | Rail approaches the shared vertical at the river and grid vertex. Overlapping ink prevents secure assignment to the vertical. |
| `E2529–E2628` | **Unknown** | Ticked rail, paired road and grid converge close to the vertex with 2528. A crossing of this diagonal cannot be confidently separated from the vertex. |
| `E2829–E2830` | **Unknown** | At Zagazig the rail meets the river-covered upper end of the vertical. Junction and grid overprint obscure exact side attribution. |
| `E2829–E2930` | **Unknown** | City dot, river and rail converge at the diagonal endpoint; the scan does not securely place a crossing along this side. |
| `E2905–E3004` | **Unknown** | Coastal rail crosses the neighboring `E3003–E3004` vertical just above its lower vertex. No distinct branch across the named diagonal is visible, but proximity to that vertex does not establish absence or attribution. |

These observations do not prove a junction, continuous route, service, construction or damage state, control, occupancy, or legal movement. No omission survey or other feature adjudication performed.

## Reproduction and integrity

Compared the cited PDF and five PNGs already rendered from it; hashes below match unknown B. PNG bytes depend on renderer and cropping. Enlarged the middle crop for visual inspection; enlargement adds no source detail. The Cairo north crop and its rotated detail locate the Cairo pairs; the north crop locates Zagazig; the coastal detail locates the coastal pair.

```sh
shasum -a 256 /private/tmp/sandtable-maps.pdf /private/tmp/rail-e-middle.png /private/tmp/rail-d-north.png /private/tmp/cairo-nile-north.png /private/tmp/kafr-review-key.png /private/tmp/e-coast-detail.png
```

In command order: `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`, `ee0df7def4b2ffc194b9c55b822f3e0291e9b1770305cbf70644ef36390bd0b1`, `b1c7ee4e0058394b16413b3243552077d15dd269278267354c3b5df886f4058c`, `ef345c8066e55295c8146a6539b4b39a88d4f62d3f4670993ae43fa79f117cdc`, `ea29b2f8389e4159126f6fda082db5e5c992cb0ece99fc2b1809e73f3d3de189`, `8d70284d466ac84a204e9857dd9fec07c66c8b660cebe0955a9b8ec8db4127b7`. Render commands are linked from unknown B. Documentation only; no runtime or data changes; no gameplay tests.
