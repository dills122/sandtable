# Indian–Australian convoy pair: independent source check

**Disposition:** The [conditional pair analysis](2026-11-17-graziani-convoy-pair-next.md)
is internally consistent. Given both hypothesized non-shell divisions at
D3615 and E1430 and the stated D+0/E+33 translation, the Australian is east
and qualifies using the Indian as the other division. With only those two,
the Indian cannot use the Australian to satisfy the directional condition.
This is not a certified initial formation, traversable seam, actual convoy
snapshot, attack or bomb-points result.

## Evidence and calculation

Visually rechecked hash-matched [Axis charts](https://spigames.net/PDFv10/CNA_ChartsAxisPlayer.pdf)
PDF p. 34 §32.66; [scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf)
PDF p. 3 / printed p. 27 §§60.22 and 60.41; [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
pp. 7, 9, 11–12 and 17 §§4.1, 4.25, 5.2 and 9.26–.27; and the
[map package](https://spigames.net/PDFv10/CNA_Maps.pdf) PDF pp. 4–5. SHA-256
values respectively match
`9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca`,
`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`,
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`, and
`8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`.
The conditional brigade baselines rely on the linked independent
[Indian division review](2026-11-17-graziani-convoy-divisions-review.md) and
[Australian review](2026-11-17-graziani-australia-shell-review.md); they were
not freshly reconstructed in this pair check. This is a prompted same-edition
inspection, not a blind source transcription, complete map certification or
independent publisher edition.

- §32.66 calls a qualifying Allied division **complete/non-shell**, situated
  east of or in the same east–west hexrow as at least one **other** Allied
  non-shell division. Its notation explanation identifies `xx` as any
  north–south hex and the remaining two digits as the east–west hexrow. The
  predicate is asymmetric for positions in different rows. A separately
  certified same-row pair of distinct non-shell divisions can satisfy the
  relation in either direction; shell/unknown status cannot be guessed.
- §60.41 places 4th Indian at D3615 with listed detachments and 6th Australian
  in training at “Cairo and/or Helwan (E1430)” with its stated exclusions.
  E1430 is the scenario's Helwan locator, not an assertion that all Australian
  subunits must start there. In the checked formation reviews, each division
  has two qualifying brigades out of three under a stated co-located/default
  attachment baseline. §9.26's division threshold is 50% **or less** for a
  shell; §9.27 excludes a shell brigade from the needed count. Training alone
  is not the stated shell criterion, but stacking, trainer, legal co-location,
  owner choices and actual phase state still need separate review.
- Under the explicitly hypothetical offsets `D = 0`, `E = +33`, D3615
  maps to `15` and E1430 to `63`; the difference is **48** columns eastward.
  The candidate Cairo cells with last digits 29–31 map to 62–64 under that
  hypothesis. This confirms the arithmetic, not the legal city set. §4.1
  does order maps A–E west to east but prints an `01xx`/`39xx` overlap
  instruction; it does not itself supply the selected +33 transform. That
  transform's origin, overlap discrepancies, exact row equality and seam
  traversability remain separately gated. A large difference cannot reverse
  within the stated arithmetic, but the 48 is not an independently measured
  original-map distance.
- Normal §5.2 puts the turn's Naval Convoy Stage before OpStage 1, while
  §60.22 starts this scenario at GT1 OpStage 1. OA `D` establishes deployment
  at the first scenario OpStage, not automatically a valid pre-OpStage
  authoritative setup snapshot. If an opening preamble is played or
  initialized, its state and order need an accepted rule. Subsequent
  OpStage/convoy lookups use current authoritative positions, attachments and
  weather/phase rules, not a permanently frozen setup inference.

## Scope and tests

The 4th Indian could itself qualify if a **third** non-shell Allied division
is at or west of its row; then the westernmost qualifying division and lane
lookup can change. The two-division example does not determine §32.66's
“beyond” threshold/equality, the complete set of candidates, cargo, weather,
actual attack or dice. A false premise—one division shell, absent or not
legally co-located—removes this particular witness; an unknown premise yields
unknown, not automatically false.

Proposed unexecuted vectors: asymmetric pair and reverse; same-row distinct
pair; added western third division; shell or unknown attached brigades;
validated versus unavailable transform; GT1 preamble versus actual later
snapshot; and a separately selected equality/beyond lookup policy. Keep
hidden composition and positions out of unauthorized previews and diagnostic
reasons. No runtime, map data or readiness flag changed; no gameplay tests ran.
