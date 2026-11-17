# Graziani map: Benghazi coordinate geometry review

**Result:** A 300-dpi inspection of the original sheets gives a direct geometric
cross-check of Benghazi's position and the competing scenario coordinate.
It supports the existing RUL-GRA-05 planning interpretation, but does not
constitute an official correction, release admission, or certification of any
sheet seam. This is a targeted same-edition check, not an independent edition
or a blind review: earlier research was consulted before this inspection.

## Sources and reproducibility

- Five-sheet map PDF `/private/tmp/sandtable-maps.pdf`, SHA-256
  `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`.
  Map A is PDF p. 1; Map B is p. 2. Hash checked immediately before inspection.
- Scenario PDF `/private/tmp/sandtable-scenarios.pdf`, SHA-256
  `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`.
  Scenario §60.31, printed p. 27 / PDF p. 3, and §60.92(B), printed p. 28 /
  PDF p. 4, both name Benghazi and print `B4827`; these readings are from the
  [earlier independent source entry](2026-11-17-graziani-map-independent-review.md),
  not a new transcription here.
- Original Air/Logistics §37.42, PDF p. 25: prior source reading records
  Benghazi `A4827` and the note that corresponding numbers on neighboring
  sheets are 33 hexes apart. See the same source entry for limits. This review
  does not use the inconsistent distance-table cells as a seam certificate.

To reproduce the new inspection with Poppler, render the PDFs in their default
rotation. These crops are specified in pixels at 300 dpi from the rendered page
origin; no resizing or image modification was used:

```sh
pdftoppm -f 1 -l 1 -r 300 -x 4750 -y 2200 -W 1000 -H 1000 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-mapcheck/benghazi
pdftoppm -f 2 -l 2 -r 300 -x 4650 -y 2200 -W 1000 -H 1000 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-mapcheck/b4827
```

Observed crop SHA-256 values respectively:
`fee61e2e56d018b622751379782145108b3e14cbd47a5e8c48f790ae88105703` and
`00fbd7171e20f757a5430efadf9112eedb9c5dc4b6eb6339c18d9e786d528160`.
PNG hashes identify these local renders; PDF hash and crop parameters are the
primary reproduction evidence because rendering versions can differ. Scans and
crops remain outside Git; matching hashes do not establish redistribution rights.

## Observations and inference

1. Map A, Benghazi coastal artwork: the city and port marks occupy the central
   hex bounded by the neighboring printed cells `4927`, `4928`, `4826`, `4828`,
   `4727`, and `4728`. The label `4826` is partly covered by the large city
   name; the other five are legible. The city's own small number is covered by
   the city art, although its trailing digits appear in the crop. The six-sided
   position and the surrounding numbering independently imply `4827` on sheet A;
   this is a **geometric inference**, not a claim to have read every covered digit.
   The port and city artwork lies near a hex boundary; no rule about feature
   ownership follows solely from the position of individual ink strokes.
2. Map A's printed Summary of Important Locations lists Benghazi `A4827`,
   matching the above inference. The adjacent El Berca dot is in `4728` and
   Benina's dot is in `4829`; the latter two coincide with the prior scenario
   §60.5 airfield transcription. This nearby check helps guard against a row or
   column misread; it does not resolve setup priority.
3. Map B's `4827` is directly legible in the second crop, surrounded by `4927`,
   `4928`, `4826`, `4828`, `4727`, and `4728`. It has the yellow rock/gravel
   pattern shown in Map A's terrain key and no visible city or port symbol.
   That is a distinct printed hex, not the Benghazi city hex. Its printed
   neighbors do not establish which of their edges are legal for movement.
4. Scenario `B4827` is therefore in direct conflict with the named location and
   Map A's location summary. The repeated `B` in two scenario rows cannot be
   silently erased. The §37.42 same-number 33-hex statement further rules out
   treating the two coordinates as a same-hex alias under that source's
   distance convention; it does not certify intervening terrain or a land route.
   Selecting the named city for both rows remains the **project interpretation**
   in [RUL-GRA-05](2026-11-12-graziani-ruling-decisions.md#rul-gra-05-benghazi-setup-coordinate-working-interpretation),
   not a publisher correction. No fresh errata or later edition was inspected
   for this targeted check.

## Test vectors for a versioned admission decision

These are proposed checks, not assertions that current implementation passes:

- Source transcription: map A location record `Benghazi -> A4827`; scenario
  §60.31 and §60.92(B) both retain literal `B4827` and their original locators.
  A differing map/scenario coordinate must surface a conflict before a ruling.
- Grid identity: `A4827` has six geometric neighbors `A4927`, `A4928`,
  `A4826`, `A4828`, `A4727`, `A4728`; test these as geometry only, without
  inventing movement access or edge terrain. The same six local-number
  relationships hold for the visibly distinct `B4827` on sheet B.
- Under an explicitly versioned RUL-GRA-05, both named initial garrison and
  Land-only supply row target `A4827`; raw source values remain `B4827`.
  With no accepted ruling or no matching city record, fail closed rather than
  choose a hex silently. Content/ruling hash changes must not reinterpret an
  old replay or save without an explicit version migration.
- `A4827` and `B4827` remain distinct keys; city/port membership at `A4827`
  does not transfer to `B4827`. Under §37.42's distance convention, the pair
  is 33 hexes apart; this is not a traversable route or proof of coverage.
- Unknown adjacent edge data is unknown, not clear terrain: none of the local
  neighbor assertions admits unreviewed crossings, incoming ZOC, or control.

## Remaining gates

This targeted check does not resolve printed Land §4.1 versus map geometry,
the 11 distance discrepancies, any cross-sheet edge, source hierarchy for
admission, map extent, incoming ZOC, or distinct coverage/control predicates.
Reviewers must still inspect ambiguous edge sides and apply the existing
fail-closed seam policy. No runtime data, readiness flag, or tests changed;
no test suite was run for this source-review document.
