# Independent check: A/B El Abiar identity lead

**Disposition:** Local reading supports the candidate's inference that the same named location is depicted in A's outer cell beside `4933` and B's clipped western cell beside `4902`. Read those cells as candidate A4934 and B4901 from their grid sequence, not as independently legible full printed labels. The matching town and local linework do not by themselves certify a legal alias, sheet registration, connected rail/track or usable crossing.

## What is visible

Re-rendered the [original five-sheet scan](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 1 (Map A) and p. 2 (Map B), at 300 dpi after SHA-256 verification: `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. Same source scan, separately inspected; not a separate map edition. The candidate's reading was available to this reviewer, so this is not a blind transcription.

- On A, `4933` is clear in the cell west of the El Abiar cell. The next eastern cell contains the blue town mark, town name, railway and black dashed strokes. The supposed `4934` digits are heavily overprinted; sequential numbering and visible outlines support the locator, not an unambiguous independent reading of every digit.
- On B, the leftmost cell containing the El Abiar town mark is partially cut by the sheet edge. Its eastern neighbor is `4902`, but that number itself is substantially overprinted by blue rail and black dashes; neighboring `4903`, `4801`, `4802`, `5001`, and `5002` and the hex grid support the proposed numbering. The left cell's printed number is partly clipped/overprinted, so `4901` remains a sequence-based designation. Town symbol remains visible despite cell clipping.
- The El Abiar labels and town marks are repeated. Both views show blue railway and black dashed routes around the town, including a dashed route running southward. B continues eastward while A ends at the sheet edge. These motifs are evidence of a common locality, not proof that every mark represents a matching side feature. Text, overprint, edge clipping and differences in terrain printing complicate pixel alignment.

Useful reproducible local crops (pixels relative to each 300-dpi rendered page):

```sh
pdftoppm -f 1 -l 1 -r 300 -x 5350 -y 2050 -W 1200 -H 1000 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/review-el-abiara
pdftoppm -f 2 -l 2 -r 300 -x 0 -y 2050 -W 1000 -H 1000 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/review-el-abiarb
```

## Limit and next check

The observations are consistent with the proposed 33-column overlap for this cell; a repeated location can be editorial overlap and does not by itself establish which grid cells are legally identical, which crossing features continue, or the exact registration across the remaining seam. This review did not measure a multi-vertex transform or residuals, inspect all cells and side features, reconcile Land §4.1 wording, or resolve contradictory printed distance values. Retain unresolved status and unknown edge semantics until those separate checks and a versioned policy are complete. No map data, runtime behavior, tests or readiness flags changed.
