# Matruh/Selby counter: visual source review

**Finding:** The British counter between `1 Buffs` and `1 Hamp` is printed
`Selby` over `Matruh`, with an infantry box, **`X`**, **`G`**, and
parenthesized **`(3)`**. It is not printed `(5)`. The combined label is
physical-counter evidence for the two names, not proof by itself of setup
attachments, a lifecycle transition or an unrestricted initial organization.

## Evidence and reproduction

Visually inspected newly reacquired counter-sheet scan
`/private/tmp/sandtable-counters.pdf`, PDF p. 4, British Counter Section Nr. 7.
Its observed SHA-256 is
`bc3e6c60131e8c813481510a5a0c991d027bdaaa7e0aa83f413737c22d62dae9`.
This identifies the inspected file, not its rights, provenance or a separately
verified physical edition. Reproduce the close crop with:

```sh
pdftoppm -f 4 -l 4 -r 300 -x 1800 -y 1020 -W 750 -H 280 -png -singlefile /private/tmp/sandtable-counters.pdf /private/tmp/gra-matruh-counter-close
```

The crop SHA-256 in this rendering is
`da4d7dd1f5415ac322593a29337bff26e69d1fd5c552dfc46f6d456bcf4a5eb7`; renderer versions can alter image bytes.
The neighboring `1 Buffs` and `1 Hamp` counters each show `II` and `1`,
which makes the target's `X`, `G` and `(3)` visually distinguishable. The
counter's printed parenthesized number is a stacking notation, not its
attachment capacity or a combat strength.

Compared hash-matched [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
pp. 16–17 §§9.11–.12, 9.16, and [common charts](https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf)
pp. 2 and 6 §§9.4 and 19.5. Their hashes respectively match
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`
and `51aa5a5bfdaca3d23794da71a45a830d98b798d060097cde6418126d6bd63bb0`.
This is a prompted visual reading, not a blind transcription or complete
review of every counter, scenario, organization or erratum.

## Interpretation and limits

- `X` is the visible brigade-size symbol; the `G` mark denotes garrison
  status under Land §9.16. The actual eligibility and effect of garrison
  stacking depend on the rule's assigned city/town and the state being
  adjudicated, not the mere existence of the letter. Section 9.12 explains
  parenthesized HQ stacking values in relation to represented attached combat
  units; `(3)` cannot automatically be applied as a standalone HQ with no
  attached combat units.
- Chart §9.4 lists three stacking points for a full **super brigade** and
  for a **shell division**. The counter's `X` and `(3)` are consistent with
  the former rather than establishing a five-point full division. Chart
  §9.4's category comparison and symbol are evidence of counter-scale
  interpretation; neither establishes current attached force, shell status,
  all garrison exceptions or a different stacking result under every state.
- Chart §19.5 separately has **Matruh Garrison GT1–12: six units, no tank,
  one gun class**, and **Selby Force GT13+: five units, no tank, no recce,
  three infantry**. Its general key and additional-company modification must
  be applied where relevant. Six is an attachment-chart value, **not** the
  counter's printed stacking number; three is not an attachment limit. The
  single combined counter does not itself prove the precise event or rule by
  which one organization becomes the other, or resolve the Buffs/Hampshire
  initial assignment and attachment-capacity conflict.

Retain source-specific setup and organization ambiguity, counter provenance,
chart timing and garrison/stack calculations as separate review boundaries.
No historical fixture, runtime content or readiness flag changed; no gameplay
tests were run.
