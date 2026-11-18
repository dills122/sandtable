# Map E rail sweep F: visual source challenge

**Date:** November 18, 2026. **Disposition:** The 16 listed pairs have visible cross-tied rail at their shared sides and matching `railroad` pins, with qualifications below. This supports the local readings, not a complete network or movement certification.

## Method and findings

Viewed the [Map E scan](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF page 5, using the west, mid and east crops described in [sweep F](2026-11-18-graziani-map-e-rail-sweep-f.md), and checked the page 1 terrain key. The key depicts railroad as a continuous blue cross-tied line and unfinished railroad with a broken centerline; paired dark lines depict road. Located sides using printed cell numbers and checked each exact pair in [hexsides.v1.json](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json). Sweep F and its crop coordinates were already visible, so this is a non-blind challenge of the same scan, not an independent edition or a fresh transcription.

| Pair | Source challenge against pin |
| --- | --- |
| `E3328–E3427` | Cross-tied line crosses diagonal, amid road and river ink; agrees with rail pin. |
| `E3329–E3330` | Cross-tied line crosses vertical river band near road junction; agrees. |
| `E3330–E3331` | Cross-tied line crosses vertical river band; agrees. |
| `E3331–E3431` | Cross-tied line crosses diagonal; agrees. |
| `E3417–E3418` | Cross-tied line crosses vertical close to upper vertex, amid road and branching ink; agrees provisionally for this side. |
| `E3417–E3517` | Cross-tied line crosses diagonal beside road; agrees. |
| `E3418–E3419` | Cross-tied line crosses vertical beside road and number; agrees. |
| `E3420–E3421` | Cross-tied line crosses vertical beside road; agrees. |
| `E3421–E3522` | Cross-tied line crosses diagonal beside road; agrees. |
| `E3426–E3427` | Cross-tied line crosses vertical with river and road overprint; agrees. |
| `E3426–E3526` | Cross-tied line crosses diagonal near road junction; agrees. |
| `E3431–E3432` | Cross-tied line crosses vertical; agrees. |
| `E3432–E3433` | Cross-tied line crosses vertical; agrees. |
| `E3522–E3523` | Cross-tied line crosses river band near road and junction ink; agrees. |
| `E3615–E3715` | Cross-tied line crosses diagonal near Alexandria lettering and city artwork; agrees with overprint qualification. |
| `E3623–E3624` | Cross-tied line crosses vertical beside road; agrees. |

Each exact pair occurs once with `railroad` and null `side` and `up`. Other pinned features were not independently adjudicated. Scan limitations and overprint qualify exact placement and continuous versus broken centerline where obscured; visible adjacent ties support rail identification. This review does not survey unpinned sides, establish junction connectivity or operating status, or decide legal movement.

## Evidence and limits

Reviewed local PDF SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; west crop `40e8de792ef197b0a29af1b7c0adc32a7f5905ab09c5802a5600b7829d9231fb`, mid `ad412390228a676604148a94aee3c7f9df49b74c17f7a0bba6c4bcd9769b0cdb`, east `470e45fb05ec46ea0a7a78c4f9c9599574d8bde4691163b3a28fcb69d8abe406`, key `ea29b2f8389e4159126f6fda082db5e5c992cb0ece99fc2b1809e73f3d3de189`, and pin `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`. Crop commands are in sweep F; PNG bytes depend on rendering tools. No runtime or map-data changes; no gameplay tests.
