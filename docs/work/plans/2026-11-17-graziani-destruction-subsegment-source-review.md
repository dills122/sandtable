# Destruction sub-segment source review

Independent review of `2026-11-17-graziani-destruction-subsegment-source.md`. **Result: core timing uncertainty confirmed; qualify chart interaction and target scope before adopting a policy.**

## Source checks

- Land rules PDF SHA-256 `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`, printed p. 12 §5.2 and p. 14 §§8.21–8.25: the Movement and Combat Phase repeats movement, breakdown determination, combat and reserve release. §8.25 permits combat without movement. These passages do not explicitly equate a §32.17 sub-segment with one cycle or establish a zero-movement destruction window.
- Land printed p. 43 §32.17: destruction occurs before any movement in a given sub-segment of a Movement/Combat Segment, with at most one attempt per Supply Unit per sub-segment. It specifies one-half the CPA of all units in the hex, rounding upward, and one die for each Supply Unit in the hex; a result of 1–3 destroys it, otherwise it remains. Add one to the roll when the sum of stacking points in the hex during the attempt is one or less. The text does not settle whether rounding is per unit or on an aggregate, nor does it explicitly define a selected subset of targets. Avoid presenting either choice as source fact.
- Common charts PDF SHA-256 `51aa5a5bfdaca3d23794da71a45a830d98b798d060097cde6418126d6bd63bb0`, printed p. 1 §6.3: “Attempt to blow a supply dump” costs one-third CPA; the chart entry carries no explicit mode qualifier. Land §32.12 represents abstract Supply Units with Supply Dump counters. Thus distinct terminology alone does not prove the chart inapplicable: one-third versus §32.17's one-half needs an explicit source-precedence or project policy decision. Do not silently merge the costs.
- Errata PDF SHA-256 `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`: reviewed relevant sequence and abstract-supply references; no identified correction defines the destruction sub-segment or resolves the cost discrepancy. The §8.23 correction concerns an internal cross-reference.

## Consequences for planning

Retain the source note's open timing question. Any proposed cycle identity, eligibility for an otherwise empty cycle, or interpretation of which movement closes the window requires a documented project choice. Track each actual Supply Unit's attempt within the chosen sub-segment; transport-command retries and fresh attempts are separate concepts. Record the relevant stacking-point condition and all affected units at the attempt, with the target and rounding interpretations explicitly selected before implementation. No runtime admission follows from this review.
