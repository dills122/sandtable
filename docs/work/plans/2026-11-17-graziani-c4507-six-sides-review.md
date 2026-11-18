# Independent review: Graziani C4507 six sides

## Finding

- **Crop coordinates identify wrong area.** The stated vertices `(570,600)` through `(414,680)` outline parts of C4606 and C4607, not C4507. In the cited 1700 × 2000 crop, C4507 vertices are approximately `(570,850)`, `(727,940)`, `(727,1125)`, `(570,1205)`, `(414,1125)`, `(414,940)`. Fix coordinates before using them for reproducibility.

## Checks

- Map C crop identifies C4507 by its label and six neighboring labels. All four diagonal sides carry broad brown bands; paired dashed route ink crosses C4607, C4508 and C4506 sides. Thick single dashed track ink approaches the lower-right C4407 side and crosses or nearly crosses amid a broken segment; its exact attribution is uncertain. Track-like ink near the C4506 junction is similarly crowded. These observations support cautious wording, not certification of every route crossing.
- The terrain key shows rock/gravel as circles and stipple. Exposed pale C4507 interior shows no obvious matching circle pattern; route, label, settlement dot and ridge overprint obscure portions. This is evidence of a visual mismatch with pinned `gravel`, not proof of a data error or an independent edition comparison.
- El Adem dot lies near the C4507/C4508 boundary and is partly overprinted. Village/bir symbol and location are supported; exact cell assignment is uncertain from the dot alone. No sea appears in C4507.
- Pinned JSON independently checked: C4507 is `gravel`, `village`, non-coastal. Six pair features and all `side: null` / `up: null` values match the table. PDF, three crop, and two JSON SHA-256 values match the document.
- No gameplay rules or runtime behavior assessed.
