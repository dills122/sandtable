# Graziani training instructor criteria: queue gap follow-up

**Disposition:** A bounded source reading fills in the printed instructor
criteria for morale training, a prerequisite left open by the
[training-order proposal](2026-11-17-graziani-training-order-next.md) and its
[review](2026-11-17-graziani-training-order-review.md). Applying those criteria
to replacement points remains an interpretation, not an admitted rule. This
is an informed check of the same scan, not an independent edition. An
[independent review](2026-11-17-graziani-queue-gap-followup-review.md)
confirms the bounded transcription and adds the qualifications below.

## Source and finding

Visually inspected [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf),
PDF/printed pp. 28 and 31, §§17.35, 20.43 and 20.45, and [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf),
PDF p. 2, §17.3. Local PDF SHA-256 values respectively
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99` and
`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.
Rendered each cited page at 150 dpi using `pdftoppm -f N -l N -r 150 -png -singlefile`.

Section 17.35 requires a combat battalion at its designated Basic Morale in
the training unit's hex during each training Operations Stage. Infantry
divisions or brigades may use an infantry-type battalion; any unit with tanks
must have a tank battalion. An instruction battalion must be at least one third
of its assigned TOE strength. Such battalions do not count against stacking
limits and each may train as many units as may be stacked in the hex. The
printed reference is to **assigned** TOE strength; no rounding method for
one third is specified in this clause. For integral strengths, compare three
times actual strength with assigned strength; mixed TOE types and the strength
calculation remain open. Other occupants, multiple instructors, partial groups
and substitution affect capacity or continuity and are not resolved here. A training unit's eligibility, location
and no-CP restriction remain separate checks.

Section 20.43 says the same rules applying to training reinforcements apply to
replacement points, gives specific replacement training locations and says five
replacement points equal one battalion when in doubt as to unit size. This
supports considering §17.35 when evaluating replacement instructors, but it
does not explicitly classify every replacement counter or say whether the
battalion and tank conditions apply point by point. Gun replacements' one-stage
requirement is described as uncrating. The errata says Axis training centers
train replacement points, not actual units; it does not explicitly say that a
center replaces the §17.35 instructor requirement. Do not infer either a
universal instructor exemption or mandatory battalion for every replacement
class without an accepted hierarchy.

## Consequence and remaining work

This gap has cross-cutting leverage: instructor identity, strength and type
can determine whether a proposed training stage is valid, while an erroneous
stacking charge can change legal placement. Preserve the criteria as separate
fields in a future versioned training contract. Resolve instructor continuity,
strength calculation, battalion classification, replacement-point and Axis
center interaction, and any more specific scenario or errata exception before
admission. This check establishes neither an actual instructor nor a legal
six-turn training witness, and does not decide completion, interruption or
arrival-stage timing.

Proposed focused tests, not run: reject a below-threshold instructor and a
non-tank instructor for a tank-bearing trainee; cover exact one-third strength
and fractional assigned strength under whichever rounding policy is accepted;
verify eligible instructors do not consume stacking capacity while trainee
capacity still applies; and compare replacement and Axis-center interpretations
with unknown prerequisites failing closed. No runtime, content, readiness or
ruleset changes; no gameplay tests run.
