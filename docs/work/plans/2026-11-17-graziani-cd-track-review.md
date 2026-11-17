# C3533–D3501 candidate track: independent visual check

**Disposition:** Source crops corroborate a track across printed
C3533/C3534 and a track within D3501 across D3501/D3502. The observations are
consistent with, but do not independently establish, the candidate
C3534/D3501 overlap or an unbroken ink connection at either outer edge. No
provenance or hash discrepancy found in the cited crops and pinned edge file.
Retain the existing bounded seam policy and its admission limits.

## Reproduction and observed marks

Reproduced all four 300/500-dpi views using the exact commands in the
[track follow-up](2026-11-17-graziani-cd-track-next.md) from the original
[map scan](https://spigames.net/PDFv10/CNA_Maps.pdf). PDF SHA-256 is
`8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`.
In the follow-up's command order the rendered hashes match:

```text
44b0ed0137c3fa353e0bd07ad8317d2e022e72d33d748c17874096a667a23764
5d8e902c46bb4f361bb5efc765df775313d8f8488822b48265864995ecfe8be1
2495c3ce15f563545c72fb570f37ab1944757e9ace40aa7a64dd2261a7420d6c
fadf8ef1df68f5eb4c5823a49d9e83f40e3c9498a1200a23f3f09086d9b35105
```

PDF p. 3 is C and p. 4 D. This is a prompted visual check of the same scan,
not an independent edition or registered overlay; renderer versions can change
PNG hashes. Source hash does not establish distribution rights.

- C's neighboring printed `3533` and `3534` cells share a vertical side
  crossed by a dark single-dashed line. In the 500-dpi crop, a dash visibly
  spans that side near x=296. The key identifies this line style as track;
  the visible crossing is **within printed sheet C**, not the far outer edge
  of C3534. C3534's final visible dash stops short of that farther outline in
  the crop; the interval appears about 11 pixels at the given scale.
- D's `3501` cell is clipped at the sheet's west margin. A similar dark
  dashed line is visible inside it and a dash spans the neighboring
  D3501/D3502 shared side near x=335. Its first visible dash begins just
  inside the clipped western outline, with an apparent small gap of about 11
  pixels in this rendering. The gaps do not prove the intended track ends:
  they can be gaps in a dashed symbol or effects of clipping. They also do
  not prove ink touches or continues across an unobserved edge.
- Neighbor labels and nearby brown slope/blue rail-like linework locate the
  corridor; visual resemblance does not prove sheet identity, rail service,
  track direction or movement legality. The two source views are neither
  registered to a common transform nor evidence resolving §4.1's overlap
  wording. The proposed 33-column mapping of C3534 to D3501 is an independent
  prerequisite, not a consequence of the pair of dashes.

The cited [pinned edges](../../../src/Cna.Core/Content/HistoricalMap/hexsides.v1.json)
hash to `124e6f943930f4fa718eb04fa1dc86b6089582aebfaff8fcb83f98ab30d98e6c`.
`C3533|E` and `D3501|W` each have `track` and a null opposite endpoint. These
are two one-ended records, not source proof that the original sheets' outer
margins form a reciprocal edge. Canonical omission of printed C3534 and the
existing mapping must remain explicit in provenance and replay.

Before any stronger claim, independently settle overlap identity, per-edge
ownership and orientation, crossing feature evidence and applicable movement
rules. Synthetic checks should compare known candidate track, unknown alias,
missing feature on either sheet, and changed content identity without
silently unioning features or upgrading every seam. No runtime, pinned map
content or readiness flag changed; no gameplay tests ran.
