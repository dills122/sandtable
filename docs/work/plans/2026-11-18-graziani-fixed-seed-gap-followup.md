# Graziani fixed objective and supply seed gap follow-up

**Date:** November 18, 2026. The [extent crosswalk](2026-11-18-graziani-scenario-extent-crosswalk.md), [objective and supply inventory](2026-11-17-graziani-objective-supply-extent-seeds.md), its [source review](2026-11-18-graziani-objective-supply-extent-seeds-review.md), and [combined coverage](2026-11-18-graziani-combined-map-coverage.md) identify one additional Halfaya candidate worth checking: `C3821`, listed alongside `C3922` in the Air/Logistics location summary. Scenario §60.81 explicitly specifies `C3922` for Halfaya; the summary candidate does not replace it or establish an alias. `C3821` is outside the combined note's selected six-side target union, though earlier `C3921` and `C3922` readings inspect two shared sides. This is one informed reading, not a separate review or a new edition.

## Explicit scope check

The fixed on-map objective and supply candidates in the inventory map to `C4807 C4321 B4827 B5925 C0716 D3714 C4131 C4021 C3019 C1014 C3922 C0127 C3618`, city-summary candidates `E3613 E3714 E1730 E1829 E1830 E1930 E1931`, and map-identified Benghazi `A4827`. All have paired six-side readings in the crosswalk and linked index. The scenario's `B4827` and map's `A4827` remain distinct; the Derna supply row prints `B5925`. City candidates do not select individual occupied or supply hexes. `C3821` is an additional summary locator, not a scenario-explicit tactical coordinate. Tripoli is a box; chosen dumps, Map D origin, and unspecified city placement are not fixed hexes. Facility-only entries are outside this check and covered by the separate facility batches. This checks the cited inventory, not every possible scenario or map location.

## C3821 source versus pin

Map C prints `3821`; neighbor IDs follow visible labels and the local grid. Page-image directions follow the crop. The terrain key identifies gold slope, dark splash-edged escarpment, single dark dashed track and blue cross-tied unfinished railroad. Descriptions concern visible ink, with shared vertices and dashes limiting exact side assignment. `up` is the pin's value, not an independently established direction.

| Neighbor | Original map observation | Pinned pair |
| --- | --- | --- |
| `C3820`, left | Exposed vertical grid line; no distinct crossing apparent. | No record; absence does not certify a clear side. |
| `C3921`, upper-left | Blue unfinished railroad crosses; dashed track approaches the crowded upper vertex. Exact track crossing remains uncertain. | `track`, `unfinished-railroad`. |
| `C3922`, upper-right | Gold slope band lies along side; dashed track reaches crowded junction. Name and overlapping features obscure exact contact. | `slope`, `track`; up `C3821`. |
| `C3822`, right | Dark escarpment band follows side, splash toward `C3822`. | `escarpment`; up `C3821`. |
| `C3722`, lower-right | Blue unfinished railroad crosses; dashed track reaches side near eastern vertex, with contact crowded. | `track`, `unfinished-railroad`. |
| `C3721`, lower-left | Single dark dashed track crosses side; remaining exposed grid has no broad band apparent. | `track`. |

These comparisons do not settle obscured ink, other features, whole-hex terrain, legal traversal or location identity. Compare existing [C3921](2026-11-17-graziani-c3921-six-sides-next.md) and [C3922](2026-11-17-graziani-c3922-six-sides.md) readings and their reviews for shared-side qualifications. No complete route, setup, control, ZOC, or playable boundary follows.

## Provenance

Original [map scan](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 3 and key p. 1, local `/private/tmp/sandtable-maps.pdf` SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. Reproduced crop:

```sh
pdftoppm -f 3 -l 3 -r 500 -x 5750 -y 6200 -W 1900 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/gra-c3821
```

PNG SHA-256 `47d48306fba59c1ad47cac0eb7b21785d1d2c76ad914d160b9c05a162c0ee74c` (renderer-dependent). Pinned [hexsides](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json) SHA-256 `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`; [hexes](../../../src/Cna.Core/Content/HistoricalMap/hexes.v1.json) SHA-256 `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`. Pin source is `vassal:CNAv2.1.0`; missing records do not prove feature absence. Same scan, no independent edition. No runtime or map data changed; no gameplay tests run.
