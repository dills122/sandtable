# A/B seam: El Abiar edge-cell identity lead

**Finding:** Both source sheets depict El Abiar's labeled town symbol inside the candidate overlapping edge cells, A4934 and B4901. The repeated named location, matching row number, and similar surroundings strengthen the 33-column overlap hypothesis locally. This remains a source-informed inference, not a certified identity, registration, crossing, or route. Linework and sheet-edge clipping prevent exact alignment from being inferred by eye.

## Source and reproduction

[Original five-sheet map scan](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 1 Map A right edge and PDF p. 2 Map B left edge, SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`, verified before inspection. Land rules §4.1 is discussed here through the prior [geometry comparison](2026-11-17-graziani-seam-geometry-comparison.md), not newly transcribed; local rules hash `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99` verified. Source bytes do not establish rights or another edition. Prior [A/B edge reading](2026-11-17-graziani-seam-ab-independent.md) was consulted before this targeted check; this is not blind.

```sh
pdftoppm -f 1 -l 1 -r 200 -x 3600 -y 250 -W 846 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-seam-new/a
pdftoppm -f 2 -l 2 -r 200 -x 0 -y 250 -W 846 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-seam-new/b
```

Crop hashes, renderer-dependent: A `dc3d41959a71ac0574ac9de7b2bace97c0d1f95544780c1c201506a0c42a8985`; B `37a63f8d4d0ae7fdcfbd06d0115d8b7050b4031ac8aa3f5627e6eff050bdef87`. Local crops are not committed. Approximate pixels refer to each crop, not a surveyed registration.

## Observations and inference

- A's El Abiar label and dark blue town symbol lie in the outer cell immediately east of printed `4933`, around (680, 1350). Its four-digit number is obscured by heavy railway, track, town and text ink; `4934` is the sequential locator, not an independently legible full label. This outer cell's surrounding outline is mostly visible.
- B's `el-Abiar` label, town mark and blue cross-tied railway lie in the partially clipped cell beside `4902`, around (110, 1350). Its `4901` designation is likewise partly covered and clipped; the locator uses neighboring printed labels and row sequence. The same-named town is a stronger identity cue than similar desert background. The left edge cuts away part of B's cell.
- A blue railway approaches El Abiar from A's interior; B's railway continues from its cropped left edge through the vicinity and curves toward Barce. Black dashed routes and brown terrain also occupy both views. These are similar motifs, but the railway and several dashed strokes are occluded or cut; no stroke-by-stroke registration or crossing is proven. Neither complete local terrain equivalence nor a side feature inventory was performed.

Under the existing candidate, A4934 and B4901 occupy the same position, while A4933 is an adjacent cell. A single town name and matching approximate position support that interpretation for this locality; a duplicated symbol could also be editorial overlap, and it does not alone specify the rule's legal alias, exact offset or all other rows. Land §4.1's printed `01xx`/`39xx` wording remains in tension with this candidate, and the distance chart contradictions remain unresolved. Do not collapse cells in authoritative content on this observation alone.

## Next check and impact

An independent reading should verify the obscured labels using adjacent grid outlines and compare the town symbol and multiple grid vertices at higher resolution, with explicitly measured image transforms and residual error. Differences in cartographic ink and cropping must remain separate from geometry; matching a single town is insufficient to certify another cell or every side. A versioned seam policy still needs reviewed identities, per-edge evidence, unknown/clear semantics, and route, ZOC and control checks. No map data, runtime behavior, readiness flag or tests changed.
