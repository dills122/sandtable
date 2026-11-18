# Graziani: C0919 oasis source review

**Disposition, 17 November 2026:** positive cross-source discrepancy candidate;
retain pinned data unchanged. The [radius reconciliation](2026-11-17-graziani-radius-reconciliation.md)
correctly identifies a separate visible symbol in C0919 and calls for a source
check. This review is informed by that note, uses the same scan, and is neither
blind nor an independent edition.

On [original Map C](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 3,
the printed `0919` identifies the cell immediately right of `0918` in the crop.
Blue palms and water lie predominantly inside C0919, matching the **Oasis**
example in the p. 1 terrain key. The number and the blue-and-black border
artwork overlap parts of the symbol or its immediate edge; the visible palms and
water remain recognizable. Nearby “Giarabub Oasis” lettering crosses other
cells and does not assign this symbol to C0918. This observation does not prove
absence of another symbol under C0918's overprint.

Pinned [hex data](../../../src/Cna.Core/Content/HistoricalMap/hexes.v1.json)
records C0919 as `terrain: clear`, `settlement: null`, and identifies its
source as `vassal:CNAv2.1.0`. The scan therefore supports a candidate
`settlement: oasis`; it does not independently adjudicate the `clear` terrain
field. Difference from the VASSAL-derived record alone does not establish an
upstream error or authorize a correction. Check a clearer or independent source,
upstream correction history, and applicable symbol assignment convention before
admission. Preserve v1 bytes and use a new map version for any accepted change;
no gameplay effects follow from this review.

## Evidence and reproduction

PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`;
pinned hex JSON SHA-256 `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`.
Using the hash-matched PDF as `/private/tmp/sandtable-maps.pdf`:

```sh
pdftoppm -f 3 -l 3 -scale-to 5000 -x 1000 -y 3950 -W 1000 -H 700 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/c0919-review-crop
pdftoppm -f 1 -l 1 -r 300 -x 2250 -y 300 -W 2100 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/c0919-review-key
```

Map crop SHA-256 `1e8857e67f0a5369e03bdafbcb8c45183d969408d2a7004e73f12360abdb5c70`;
key crop SHA-256 `8218f9ff8e4f6c9a49c0e631fe2a9b861ef82dd81dfb9107da4e759ccf155894`.
PNG bytes depend on renderer and encoding; PDF hash anchors source. No runtime
changes or gameplay tests.
