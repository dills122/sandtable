# Map D/E alternate source search: audit

**Disposition:** Supports the [bounded search](2026-11-18-graziani-alternate-map-source-search.md)'s central local-image comparison and its limits. The two retained PDF packages contain byte-identical extracted D JPEGs and byte-identical extracted E JPEGs. This does not establish live full-file bytes, scan lineage, a separate physical edition, rights, or absence of other sources. No asset or rights contact was made.

## Reproduced comparison

```sh
mkdir -p /private/tmp/map-source-audit
pdfimages -f 191 -l 192 -j /private/tmp/sandtable-full-original.pdf /private/tmp/map-source-audit/full
pdfimages -f 4 -l 5 -j /private/tmp/sandtable-maps.pdf /private/tmp/map-source-audit/maps
cmp /private/tmp/map-source-audit/full-000.jpg /private/tmp/map-source-audit/maps-000.jpg
cmp /private/tmp/map-source-audit/full-001.jpg /private/tmp/map-source-audit/maps-001.jpg
shasum -a 256 /private/tmp/sandtable-full-original.pdf /private/tmp/sandtable-maps.pdf /private/tmp/map-source-audit/*
```

Both `cmp` calls succeeded. Full PDF SHA-256 `836c14949c4fef1b066043e19a570256b22598d8ae2948d26844b949f53b60a5`, maps PDF `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`; extracted D pair `576bfc4131887a62f7970fef5afd27e689c312e5442529ef0b2f3a990cbca932`; E pair `39795e0807402a195ba1e65ac96fb6fa842643b067934383af48fa27e643bb1e`. `pdfimages -list` identifies D as 10,585 × 6,689 and E as 10,586 × 6,693, both 300 × 300 ppi in each PDF. Local full and maps lengths are 134,328,922 and 48,700,355 bytes respectively. Matching JPEG bytes establish same embedded image data in these copies, not provenance or identical surrounding PDF metadata/rendering.

## Access and scope check

- [SPI index](https://spigames.net/rules_downloads.htm) lists all-in-one and separate maps links. During this audit, opening each PDF through web returned a size-limit error reporting the same respective lengths as local copies. That verifies reported response lengths, not current remote full-file hash equality. The index describes helping owners and is not a rights grant.
- [VASSAL listing](https://vassalengine.org/wiki_old/wiki/Module%3AThe_Campaign_for_North_Africa%3A_The_Desert_War_1940-43) states v0.93 replaced the map with an actual map copy, v0.94 added the D2228/2328 escarpment, and v1 corrected aspect ratio. Both inspected module links returned unsupported `application/zip` in the web tool; no D/E module image, scanner or physical edition was checked. A changelog's claim is not evidence that this is independent of the SPI pixels.
- The [CNA Living Rules map](https://basmith.net/cna/map) was accessible through an indexed cached result in this audit, but direct opening returned a tool error. Cached text describes its own schematic redraw based on a 2021 VASSAL redraw checked against the SPI scan and labels its data CC0; the [site introduction](https://basmith.net/cna/) states non-affiliation with SPI. That is an attributed transcription and site claim, not an independent original scan or a determination of rights in underlying art.
- [BoardGameGeek image](https://boardgamegeek.com/image/1347097/the-campaign-for-north-africa-the-desert-war-1940) returned HTTP 403 in this audit. [Decision Games listing](https://shop.decisiongames.com/ProductDetails.php?ProductCode=P3034) describes a design-stage preorder update with a graphically updated map and five maps, not an identified original-edition scan. Neither access result establishes all that may exist outside the inspected links.

No exhaustive search of private collections, different printings, later errata or other repositories was performed. The original candidate's statement that the Living Rules page opened describes its earlier attempt; direct access failed during this review, while indexed cached text supported the content description. No scans committed, no data or runtime changes, no gameplay tests.
