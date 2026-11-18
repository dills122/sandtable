# C0716 radius: eastern edge audit review

17 November 2026. Independently checked the [eastern nine-cell audit](2026-11-17-graziani-radius-edges-east.md) against original Map C, PDF page 3, using its reproduced crop. PDF and crop hashes match the audit; this is the same scan, not independent-edition confirmation.

**Result: no blocking discrepancy found.** The 54 neighbor labels match the displayed hex geometry, including reciprocal sides shared by assigned cells. Brown ink supports the marked C0914 and C1014 side candidates; the crop does not establish slope class or `up` direction. Dashed tracks support the marked routes, with the stated vertex ambiguity at C0915/C1014/C1015 and C0915/C0916/C0815. In particular, the route approaching C0915|C1015 does not prove a crossing of that side. Patterned ink supports the marked `M` contacts as visual contacts, not whole-hex terrain or a distinct edge rule. Unmarked and `?` sides remain unknown, not clear or negative findings.

Checked all pinned records touching the nine cells against `src/Cna.Core/Content/HistoricalMap/hexsides.v1.json`: the audit lists all ten, including both slope-plus-track pairs, their `up` values, and null `side` and track `up` fields. Both pinned-data SHA-256 values match. This comparison verifies transcription of pinned claims, not their source accuracy. Vertex ambiguity, blue lettering, labels, scan resolution, and brown interior artwork still constrain exact feature attribution; no pinned-data change follows.

No runtime or data changes. No gameplay tests; visual and pinned-record review only.
