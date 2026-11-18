# Independent review: Bardia C4321 six-side source check

Reviewed original Map C crop and Map A terrain key from the PDF identified in the source check, plus both pinned JSON files. This is an independent reading of the same scan, not a second map edition or rules adjudication. PDF, crop and JSON SHA-256 values match those recorded in the source check.

## Finding

The opening claim that three neighboring cells are “substantially or entirely sea” is imprecise. C4421 and C4322 are sea; C4420 and C4221 each contain both land and sea. The latter two are predominantly land in this crop. Replace the count with “two neighboring cells are sea; two others contain both land and sea.” This does not change the table's coastline observations.

## Side checks

- C4420, upper-left: shoreline crosses shared side near (700, 740). No continuous escarpment band apparent along that side; shore and city details limit certainty.
- C4421, upper-right: shared side lies in sea. Port anchor sits inside C4321 near the side; no road or terrain band is apparent.
- C4322, right: shared side lies in sea; Bardia lettering obscures part. Cannot exclude a feature under the overprint solely from this scan.
- C4221, lower-right: shoreline crosses near (880, 995). Overprint crowds the landward section; visible portions do not establish a complete feature inventory.
- C4220, lower-left: brown band follows the side, consistent with the key's one-sided slope symbol. City and AXIS artwork obscure parts. Key also shows a distinct brown ridge symbol; this check does not establish the high side. JSON's `up: C4220` remains unverified from map/key alone.
- C4320, left: paired solid road lines cross near (610, 840–885). Artwork and city blocks obscure nearby detail and part of the side.

Printed neighboring labels and grid support all six assignments. Approximate vertices and shoreline coordinates in source check agree with crop. Map key supports road, slope, escarpment, coast, port and major-city identifications; AXIS figures are overprint, not evidence of a terrain feature or setup legality.

Pinned side JSON has exactly two C4321 records: `C4220|C4321` slope with `up: C4220`, and `C4320|C4321` road with `up: null`. Pinned hex JSON records major-city terrain and settlement, and coastal true. Missing side records cannot prove those sides feature-free. No conclusion on movement, ZOC, combat or setup follows from this visual check.
