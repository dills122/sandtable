# Rail west-of ordering: decision input

**Decision needed:** Define how Land §8.78's “west of” compares individual
rail hexes, including equality and cross-sheet cases. No comparator, seam
registration or real route is selected here.

## Source evidence and separate predicates

[Land §8.78](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf),
PDF/printed p. 16, forbids Commonwealth railroad movement on a rail hex west
of any rail hex occupied by an Axis combat unit. It does not define a numeric
individual-hex comparator, equal-position treatment or sheet alignment. The
[Axis convoy chart §32.66](https://spigames.net/PDFv10/CNA_ChartsAxisPlayer.pdf),
PDF p. 34, defines its own same-suffix east–west hexrows and expressly includes
same-row equality in its qualifying-division condition. It does not say that
its row equivalence governs §8.78. The chart's `Dxx01`, `Cxx16`, `Cxx01`,
`Bxx16` thresholds provide chart-specific directional context, not a transform
between individual map cells or a general threshold-equality rule.

[Map D](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 4, visually places
the center of D3515 west of the center of D3615, although both share suffix
`15`. [Land §4.1](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
orders sections A–E west to east but its edge-number wording conflicts with
the displayed map edges. [Air §37.42](https://www.spigames.net/PDFv10/CNA_AirGameRules.pdf)
describes same-number distances; it does not alone establish cross-sheet
identity or a west-order transform. See the informed [source comparison](2026-11-18-graziani-rail-west-order-source.md)
and [review](2026-11-18-graziani-rail-west-order-source-review.md).

[RUL-GRA-26](2026-11-12-graziani-ruling-decisions.md#rul-gra-26-destroyed-railroad-witness-bounded-planning)
is a **planning interpretation** about which constructed or destroyed rail
hexes can witness the rule; it explicitly leaves west and use semantics open.
Choosing an ordering does not determine whether a candidate is railroad, is
occupied by an Axis combat unit, or counts as use. Track status, origin and
entered-hex treatment, and witness coverage remain separate decisions.

## Comparator alternatives

| Candidate | Decision and evidence needed | Equality and uncertainty |
| --- | --- | --- |
| **Projected center** | Compare registered map-space center positions; use visible within-sheet staggering. Establish exact projection, orientation, sheet registration and tolerance first. | Define equal or overlapping positions explicitly; incomplete registration gives unknown, not an assumed order. |
| **Hexrow grouping** | Use a documented east–west row relation, adapting §32.66's chart notation by analogy rather than asserting it governs rail. Define a cross-sheet row mapping separately. | Same row could be treated as not strictly west, but §8.78 does not expressly establish that convention; other equality semantics require a separate choice. |
| **Partial relation** | Admit only independently evidenced comparisons, leave unresolved pairs unknown until registration and precedence are established. | Same-row and seam pairs may remain unknown. Unknown is not proof of equality, east, or absence of a witness. |

These are planning alternatives, not an exhaustive set or an official rule
correction. Sheet sequence alone must not supply an exact seam order; possible
aliasing, overlap and edge registration require explicit evidence.

## Distinguishing cases

These synthetic cases assume candidates are independently established rail
hexes with all relevant occupancy and combat classifications known. They do
not claim the listed cells actually hold rail or an Axis unit.

1. **Same sheet and suffix:** Suppose an Axis combat unit occupies D3615 and
   D3515 is the rail hex proposed for use. A center comparator using the
   observed map positions identifies D3515 as west and, for this witness,
   applies §8.78's restriction. A same-suffix row comparator with strict
   *different-row* west treats both as row 15, so this witness alone does not
   establish west. A partial comparator that has not accepted either relation
   returns unknown. A not-west result for this one witness does not authorize
   movement or establish absence of other witnesses.
2. **Across the C/D seam:** Compare hypothetical individually identified rail
   cells on opposite sheets near the seam. Until their exact identities,
   section registration and overlap/equality convention are evidenced, neither
   their relative position nor their relationship as distinct cells may be
   inferred from the chart's `Cxx16` and `Dxx01` threshold labels. A center or
   row model can yield a result only after its own explicit transform is
   supplied; a partial relation returns unknown. The labels do not identify a
   concrete occupied or usable hex.

In an eventual state transition, a proven qualifying eastern witness may
block a western use even if other witnesses are unknown. Concluding that no
witness blocks requires complete relevant track, occupancy and combat-unit
coverage and a decided comparator. Keep unknown distinct from not-west and
from a successful action; preserve state on unsupported inputs and separately
resolve information disclosure and replay semantics.

Land SHA-256 `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`;
Axis charts `9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca`;
map `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`.
This synthesis relies on the linked same-scan comparison and review; no new
edition, exhaustive corrections search, runtime/data change or gameplay test.
