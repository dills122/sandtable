# Axis convoy loss thresholds across game modes

**Finding:** The original rules provide different simplified convoy loss
procedures for Land-only play and for the Air game's abstract logistics mode.
The Air procedure is explicitly a replacement for the Land procedure, not an
unqualified correction to it. Both leave a 5% result outside their specifically
described loss branches; the Air procedure also leaves 25% between its 20%
and 30%-or-greater branches. The chart contains both percentages. This narrows
the evidence for the proposed 5% policy without resolving that policy.

## Printed sources

* Land §32.64–.65, printed/PDF p. 44, resolves an Axis convoy against the
  Air Bombardment table. Its 10% branch treats motorization and replacements
  differently; its second branch applies to **20% or greater** and uses a
  minimum. Thus, as a textual threshold, 25% falls under the Land branch;
  5% is not named by either branch. Grouping, minimum wording and fractional
  treatment at 5% remain as documented in [prior convoy analysis](2026-11-17-graziani-five-percent-next.md).
* Air/Logistics §47.6, printed/PDF p. 18, explicitly says its simplified Axis
  naval-convoy rules replace Land §32.6 in the abstract Logistics system.
  Its §47.65 separately describes 10%, 20%, and 30% or greater, with depot
  supply differences and different minimums. It does not explicitly name a
  25% branch, nor a 5% branch. Treating 25% as 20%, extending the 30% branch
  downward, or importing Land's 20%-or-greater wording would be an
  interpretation, not printed text. This question is conditional on playing
  the Air abstract-logistics system; it is not a Graziani Land-only rule.
* Common chart §41.5, printed/PDF p. 12, Axis Naval Convoys rows, prints both
  5% and 25% outcomes. For example, under the **201–260 bomb-point** column,
  sequential dice `64–66` yield 25%; this is a table lookup, not evidence a
  particular convoy or scenario state can reach that column. The 5% cell and
  its valid sequential-die values are recorded in the earlier analysis.
* The September errata's §§41.5 and 41.67 entries address chart and cargo
  classification context, respectively; §41.67 is not a rounding correction.
  This bounded check did not locate an explicit §47.65 correction.
  It does not establish that no later correction or broader rule exists.

## Consequences for review

Keep game-mode identity with each result. In Land-only mode a 25% result
matches the printed `20% or greater` branch; do not silently apply Air §47.65
to replace it. In Air abstract-logistics mode the scope of 25% and 5% needs
an explicit source hierarchy or versioned interpretation. Neither the chart's
percentage nor the existence of a fractional result selects a rounding rule.
The source explicitly replaces §32.6 with §47.6, but the relationship between
either simplified procedure and detailed §41.67 remains a separate question.

Synthetic review vectors: Land 20%, 25%, 30% and 5%; Air abstract 20%, 25%,
30% and 5%; empty versus nonempty cargo classes; one regular versus one depot
supply unit where that mode permits it; and two distinct convoy lanes. Check
actual allowed bomb columns, d66 digits, cargo, weather and scenario mode
separately. No event, legal load or gameplay result is asserted.

## Provenance

Visual readings of [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
SHA-256 `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`,
[Air/Logistics rules](https://spigames.net/PDFv10/CNA_AirGameRules.pdf)
`9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4`, and
[common charts](https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf)
`51aa5a5bfdaca3d23794da71a45a830d98b798d060097cde6418126d6bd63bb0`.
September errata SHA-256
`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`;
no exhaustive later-errata search claimed. The [independent source review](2026-11-17-graziani-convoy-mode-rounding-review.md)
corroborates the branch distinction and table example. Research only; policy
admission pending.
