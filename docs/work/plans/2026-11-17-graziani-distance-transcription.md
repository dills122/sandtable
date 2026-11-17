# Independent Graziani distance transcription

Source: Graziani Air/Logistics rules, PDF page 25, §37.42 Land Distance Chart. SHA-256: `9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4`. Rendered at 200 and 400 dpi and inspected visually.

The [independent JSON](graziani-distance-independent.json) preserves the initial visual reading, completed before consulting the comparison. Labels retain printed spelling, hex identifiers, and asterisks. Each row contains values against earlier labels, in order; diagonal dashes are omitted. All 20 rows and 190 cells are recorded; no cells marked unclear.

After completing that reading, compared with the `rows` literal in `/private/tmp/check-distances.py`: **zero disagreements across 190 cells**. This comparison does not establish correctness of either transcription.
The [prior calculation's data literals](graziani-distance-prior.json) are now
retained with their original temporary script hash, so the numerical comparison
does not depend on that temporary file surviving. Labels and coordinate semantics
still require their own source review.

Checked all 1,140 unordered triples for triangle inequality using the transcribed values symmetrically. **24 triples violate it**; entries below show the direct pair, intermediate location, and printed values. These are arithmetic observations, not source corrections or route/seam conclusions.

- Nofilia (A2703) to Alexandria (E3613)*, via C2803: 147 > 67 + 79.
- El Agheila (A1816) to Ft. Maddalena (C3019), via C2803: 75 > 57 + 17.
- El Agheila (A1816) to Bardia (C4321), via C2803: 83 > 57 + 25.
- El Agheila (A1816) to Bir-el-Qatrani (D2903), via C2803: 91 > 57 + 33.
- El Agheila (A1816) to Mersa Matruh (D3714), via C2803: 106 > 57 + 48.
- El Agheila (A1816) to Gerawla (D3516), via C2803: 107 > 57 + 49.
- El Agheila (A1816) to Alexandria (E3613)*, via C2803: 138 > 57 + 79.
- Agedabia (A2629) to Bardia (C4321), via Ben Gania (B3715): 67 > 24 + 42.
- Agedabia (A2629) to Bardia (C4321), via C2803: 67 > 41 + 25.
- Agedabia (A2629) to Alexandria (E3613)*, via C2803: 121 > 41 + 79.
- Soluch (A4130) to Wadi Natrun (E2418), via Ben Gania (B3715): 130 > 20 + 109.
- Soluch (A4130) to Wadi Natrun (E2418), via C2803: 130 > 46 + 83.
- Soluch (A4130) to Wadi Natrun (E2418), via Ft. Maddalena (C3019): 130 > 61 + 68.
- Soluch (A4130) to Wadi Natrun (E2418), via Bir-el-Qatrani (D2903): 130 > 77 + 51.
- Soluch (A4130) to Cairo (E1829)*, via Bir-el-Qatrani (D2903): 143 > 77 + 65.
- Soluch (A4130) to Wadi Natrun (E2418), via Mersa Matruh (D3714): 130 > 85 + 44.
- Soluch (A4130) to Wadi Natrun (E2418), via Gerawla (D3516): 130 > 88 + 41.
- Barce (B5504) to Bir-el-Qatrani (D2903), via Ben Gania (B3715): 80 > 20 + 58.
- Barce (B5504) to Bir-el-Qatrani (D2903), via Mechili (B4921): 80 > 20 + 58.
- Barce (B5504) to Bir-el-Qatrani (D2903), via C2803: 80 > 46 + 33.
- Barce (B5504) to Bir-el-Qatrani (D2903), via Tobruk (C4807): 80 > 40 + 38.
- Barce (B5504) to Bir-el-Qatrani (D2903), via Ft. Maddalena (C3019): 80 > 61 + 17.
- Barce (B5504) to Bir-el-Qatrani (D2903), via Bardia (C4321): 80 > 56 + 22.
- C2803 to Cairo (E1829)*, via Wadi Natrun (E2418): 98 > 83 + 14.
