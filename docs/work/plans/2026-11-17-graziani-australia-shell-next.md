# 6th Australian Division: initial shell baseline

**Result:** Under a source-supported **co-located, default-attachment baseline**,
6th Australian is a **non-shell division**: its deployed 16th and 17th
Australian Brigades are both non-shell and attached, giving two of three
permitted brigade-equivalent slots. The absent 19th Brigade does not shrink
that denominator. This is conditional on actual initial placement and legal
organization, not proof of the division's state at a later convoy lookup or of
the required second non-shell division.

## Original source evidence

| Source | Printed facts | Bounded consequence |
| --- | --- | --- |
| Scenario §60.41, printed p. 27 / PDF p. 3 | 6th Australian Division starts **in training at Cairo and/or Helwan (E1430)**, less 19 Aus Brigade, 2/1 Australian Field Artillery and 2/2 Australian Field Artillery. | The explicit exclusions are not starting attached units. `Cairo and/or Helwan` leaves an actual location/allocation to be established; it does not itself fix every subordinate's attachment snapshot. |
| Commonwealth OA, PDF p. 13 | 16 Aus Brigade HQ and battalions 2/1, 2/2, 2/3; 17 Aus Brigade HQ and battalions 2/5, 2/6, 2/7 all print `N`, `D`. The 19 Aus Brigade HQ and its 2/4, 2/8, 2/11 battalions print `2/9`. | Two distinct assigned brigades and their six listed battalions are deployed; the third brigade is excluded from setup and its listed 2/9 is after GT6. Do not confuse 16 Aus with the separately listed 16th Infantry Brigade `(70)` at E1829. |
| Commonwealth OA key PDF p. 5 and characteristics §4.46a PDF p. 22 | `D` is deployed at GT1 OpStage 1; `N` for non-tank/non-gun units is maximum TOE. Infantry battalion ID `p` has maximum 6 TOE. All six listed Australian battalions are `p`, `N`, `D`. | Each has 6 of 6 listed TOE under this baseline, above §9.26's battalion-shell cutoff. Scenario modifications or accepted later losses must still be applied. |
| Commonwealth formation chart §19.31, PDF p. 24 | The infantry-division row has **three** infantry brigade-equivalent positions. The standard infantry-brigade row has **three** infantry battalion positions. | Each co-located, normally attached 16th or 17th Brigade has three of three qualifying battalions; the division denominator remains three even though 19th is absent. |
| Land §§4.25, 9.26–.28, printed/PDF pp. 9, 17 | Assigned units beginning in the same hex as parent are attached by default unless specifically stated otherwise. A division is shell if 50% or less of its permitted brigade-equivalents are attached. A standard brigade is shell below two-thirds of its assignable battalions, with a separate two-battalion exception; §9.27 treats attached shell subunits specially. §9.28 reduces shell size for specified differentiation/size uses. | Under the baseline's co-location and no changed attachments, each brigade is non-shell (3/3), and the division has 2/3, **greater than 50%**, so is non-shell. Training or absent artillery does not by itself satisfy any listed shell condition. |
| Scenario §59.2, printed pp. 25–26 / PDF pp. 1–2 | Same-hex listed units may be organized by the owner within formation and stacking limits; `Att` identifies attached rather than assigned units. | The default is not immutable. A claimed alternate organization requires an actual permitted setup and snapshot, not an invented forced detachment or an assumed free cross-hex attachment. |

The OA's displayed untrained morale and scenario's training status are
separate from the §9.26 shell thresholds. They may matter for training,
movement, combat and other eligibility; they do not establish shell merely by
being in training. Conversely, `D` and OA assignment alone do not prove a
subordinate remains co-located and attached after a player choice or game
transition.

## What the source baseline does and does not decide

A concrete conditional initial state has the division HQ, both deployed brigade
HQs and their respective battalions together in one legal Cairo/Helwan hex,
with their OA assignments attached by the §4.25 default and no later change.
In that state the two brigades each pass the ordinary 2/3 brigade test and the
division passes the >1/2 test. Two of three is not full historical roster
strength, but it is enough to avoid the **division shell** condition. Source
reading does not independently certify the chosen hex's complete stacking,
training setup or every additional counter, so this is an explicit baseline
rather than an unconditional valid-action certificate.

If legal setup splits or detaches a required brigade, or detaches enough of its
battalions, actual status can differ. One attached qualifying brigade of three
is 1/3 and a shell division; §9.27 excludes an attached shell brigade from the
necessary count, so with exactly two attached brigades and one shell, only one
qualifies and the division is shell. Do not assume a split is legal just
because the location says “and/or,” nor assume all splits are forbidden; apply
actual formation, setup, stacking and owner-choice rules. A separate optional
unit can count only under an accepted §19.45 substitution/attachment rule; it
is not automatically another brigade-equivalent. The 19th's absence does not
turn the denominator into two.

Axis simplified convoy chart §32.66 looks for a qualifying non-shell Allied
division **east of or on the same hexrow as another non-shell Allied division**.
Even if this Australian baseline is accepted, the chart result needs the
actual authoritative convoy-stage snapshot, its qualifying counterpart, the
appropriate **hexrow convention**, lane and source rules. No second qualifying
division, target hexrow, bomb-points column, movement in/out of training or
historical die result is established here. The scenario begins at GT1
OpStage 1, while a normal Naval Convoy Stage precedes that stage; whether an
initial convoy preamble is played, skipped or initialized from setup remains
separate. Don't freeze initial baseline as the answer for every subsequent
lookup, or disclose hidden formation details to an unauthorized side.

## Proposed distinguishing vectors

- Co-located default assignment: deployed 16th and 17th each have three
  attached full `p` battalions; division has 2 qualifying brigades of 3 and
  is non-shell despite training and absent 19th/artillery.
- Hold the denominator at 3; remove one brigade from attached representation
  by a **separately legal** setup or action, leaving one qualifying brigade:
  shell. Keep both brigade HQs present but reduce one below its own threshold:
  assess §9.27 and the resulting division state under the accepted rule.
  Unknown attachment/placement is unknown rather than automatically shell.
- Compare division and both brigades in one hex with a legally certified
  Cairo/Helwan split; apply §4.25 and §59.2 to the actual parents and locations.
  Verify that invalid cross-hex attachment and impossible stacking do not
  qualify. Do not count 16th Infantry Brigade `(70)` as 16 Aus.
- Recompute at the accepted convoy snapshot, with a separately certified
  other non-shell Allied division and row relation; test absent/unknown second
  division, hypothetical row boundary, stage entry and replay. Never use a
  private setup explanation to leak opponent roster or location.

## Provenance and limits

Visually inspected hash-matched [scenario booklet](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf),
PDF pp. 2–3 / printed pp. 26–27, SHA-256
`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`;
[Commonwealth charts](https://spigames.net/PDFv10/CNA_ChartsCWPlayer.pdf), PDF
pp. 5, 13, 22, 24,
`f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981`;
[Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), printed/
PDF pp. 9 and 17,
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`;
and [Axis charts](https://spigames.net/PDFv10/CNA_ChartsAxisPlayer.pdf), PDF
p. 34, §32.66,
`9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca`
(chart content cross-checked against the prior source rendering). Candidate and
prior independent Australian readings were consulted before this pass. Same
scan edition, not an independent edition; this is not an exhaustive setup,
errata, national-formation or convoy-stage proof. The [independent review](2026-11-17-graziani-australia-shell-review.md)
confirms the conditional baseline and §9.27's exclusion of a shell brigade.
No runtime or data changed; no gameplay tests ran.
