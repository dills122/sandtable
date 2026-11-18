# Graziani: map and flexible-setup evidence ledger

**Date:** November 17, 2026. This batch compares the reviewed source readings
with pinned map records and collects their dispositions. It is not another
independent scan reading, an admission decision, or a review of the 33 fixed
starting hexes. All source checks cited below use the same scanned edition;
their second readings are not independent editions.

## Radius comparison

Compared all 37 inclusive-radius C0716 candidate records in
[`hexes.v1.json`](../../../src/Cna.Core/Content/HistoricalMap/hexes.v1.json)
with the paired [map interior reading](2026-11-17-graziani-c0716-radius-interiors.md)
and [visual review](2026-11-17-graziani-c0716-radius-interiors-review.md).
The original [Map C](https://spigames.net/PDFv10/CNA_Maps.pdf) is PDF p. 3.
These readings describe visible ink, not necessarily the rule for assigning a
whole cell's terrain. Notable differences and unresolved comparisons:

| Cell | Reviewed map observation | Pinned record | Disposition |
| --- | --- | --- | --- |
| C0718 | Blue palm oasis symbol, crossed by track. | `clear`, no settlement. | Apparent missing oasis; source-check full symbol and assignment before a versioned correction. |
| C0818 | Salt-marsh pattern visible, partly masked by lettering. | `clear`, no settlement. | Artwork differs from record; determine whole-hex classification rule and inspect obscured area before changing terrain. |
| C0817 | Salt-marsh pattern visible, partly masked by lettering; review does not describe a palm here. | `salt-marsh`, `oasis`. | Open settlement comparison: omission from the reviewed description is not proof of absence under overprint. |
| C0918 | Salt-marsh pattern visible; review does not describe a palm here. | `salt-marsh`, `oasis`. | Open settlement comparison: obtain a specific symbol/hex assignment reading before retaining or removing it. |
| C0914 | Brown artwork extends inside cell; its classification remains uncertain. | `gravel`, no settlement. | Do not equate color presence with a verified terrain classification; inspect key and applicable assignment rule. |
| C0613–C0618 | Both yellow and uncolored portions visible in every cell. | All `desert`. | Color presence alone does not validate whole-cell assignment. |

Other radius readings and records broadly agree at the level of the grouped
visible observations, subject to their limits; this is not negative proof of
every other feature. C0716 and C1014 remain `oasis` and `village` respectively
in the pinned records, consistent with the cited visible symbols. Separate
source review is needed for the four highlighted settlement assignments and
terrain rules; no record is corrected by this comparison.

Reproduce the record extraction with ranges from the [radius inventory](2026-11-17-graziani-c0716-radius-source.md):

```sh
python3 - <<'PY'
import json
ranges = [(4,14,17),(5,14,18),(6,13,18),(7,13,19),
          (8,13,18),(9,14,18),(10,14,17)]
ids = {f'C{row:02}{col:02}' for row, lo, hi in ranges for col in range(lo, hi+1)}
records = json.load(open('src/Cna.Core/Content/HistoricalMap/hexes.v1.json'))['hexes']
for h in records:
    if h['id'] in ids:
        print(h['id'], h['terrain'], h['settlement'])
PY
```

SHA-256 checked in this batch: map PDF
`8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`;
pinned hex JSON `ea99a78ccbee220bf318c9fd6ccab631fa96c118df7d9d0b9e94904e0978a568`.
Hashes identify bytes, not source rights or independence.

## Placement and seam dispositions

| Question | Evidence and disposition |
| --- | --- |
| Saharan setup | Scenario §60.31 gives a flexible area within three hexes of C0716, not a fixed placement; all 37 geometric candidates are on Map C under the selected inclusive planning interpretation. Composition, allocation, terrain, stacking and region rules still gate actual placement. See [radius](2026-11-17-graziani-c0716-radius-source.md), [rules](2026-11-17-graziani-saharan-setup-rules.md), and [RUL-GRA-23](2026-11-12-graziani-ruling-decisions.md#rul-gra-23-saharan-detachment-setup-radius-bounded-planning). |
| C0719 border | [Paired check](2026-11-17-graziani-saharan-radius-border-review.md) finds the candidate opposite the center across the visible line; its identity and legal implications remain conditional. Placement is not movement. |
| C0716 diagonal track | [Six-side reading](2026-11-17-graziani-c0716-six-sides.md) reaches the crowded upper vertex; C0816 crossing remains uncertain despite the pinned track record. Obtain a clearer source, not an inferred extra crossing. |
| A/B overlap | [Registration pilot](2026-11-17-graziani-map-ab-registration.md) fits a selected one-cell grid translation; regular grid alignment cannot identify the correct shift, and town-mark centers differ. Distinctive landmarks and source interpretation of §4.1 are still needed before an alias or crossing is admitted. |
| C/D links | [Track check and review](2026-11-17-graziani-cd-track-review.md) support local marks without proving continuity; [rail check](2026-11-17-graziani-cd-rail-followup.md) finds mismatched visible intersection positions. Retain existing bounded policy; neither proves a usable crossing. |
| Benghazi and chart | Scenario `B4827` conflicts with Map A location-summary `A4827`; obscured symbol and inconsistent distance-chart triangles prevent treating agreement with candidate geometry as proof. Retain source conflict and [bounded decision](2026-11-17-graziani-map-distance-decision.md). |
| Map D correction | Errata explicitly adds D2228/D2328 escarpment, already present in pinned data; high side is an inference, not specified by errata. See [source check](2026-11-17-graziani-map-remaining-independent.md) and [direction review](2026-11-17-graziani-d2228-escarpment-direction-review.md). |

## Remaining gates

Resolve terrain assignment and obscured symbols from clearer primary material;
check additional editions or authoritative corrections when available. Resolve
seam identities and distances through source hierarchy and reviewed physical
evidence, including the 11 candidate-distance discrepancies. Establish
reviewed extent, unknown versus verified-clear sides, boundary and incoming
ZOC semantics before playable-map admission. Existing sparse records are not
proof of clear edges. Track per-cell provenance and evaluate content version,
replay impact and focused tests before any correction. These are external
evidence and policy/implementation tasks, not findings completed by this
ledger. No runtime, data, readiness flag or gameplay test changed.
