# Graziani independent source entries — November 17, 2026

**Status:** fresh-context source transcription, not acceptance of a scenario,
rules ruling, licensing, full source coverage or a second edition. Readers
worked from the listed source scans and packet templates before consulting first
entries. Supply/truck packet READMEs disclosed that a prior contextual recheck
agreed, but not its field values; the new reader was blind to those values. Other
packet instructions and target labels provide limited context; independence is
from the prior transcribed values, not ignorance of the subject. Raw comparator
output remains unchanged and exits 1 where it reports differences.

| Packet | Independent reading | Raw comparison | Disposition |
|---|---|---|---|
| Initial supply, 15 rows / 50 fields | [entry](graziani-supply-review/reading-independent.json) | [comparison](graziani-supply-review/comparison-independent.json) | All values agree; 15 locator-wording differences. Reader locates same printed pages, cases and rows. Does not resolve location, stacking or setup rules. |
| Initial trucks, 32 rows / 96 fields | [entry](graziani-truck-review/reading-independent.json) | [comparison](graziani-truck-review/comparison-independent.json) | All values agree; 32 locator-wording differences. Reader locates same printed page, cases and rows. Does not resolve allocations or motorization rules. |
| Commonwealth arrival groups, 14 rows / 54 fields | [entry](graziani-arrival-review/reading-independent.json) | [comparison](graziani-arrival-review/comparison-independent.json) | Values agree; 14 locator-wording differences. Does not settle NZ source precedence or scenario reachability. |
| Commonwealth arrival members, 21 rows / 84 fields | [entry](graziani-arrival-review/member-reading-independent.json) | [comparison](graziani-arrival-review/member-comparison-independent.json) | Seven weapon-mark differences are whitespace around multiplication signs; 21 locator strings differ. Source meaning agrees on those readings; preserve literal marks. |
| French Marine, 4 grouped rows / 10 fields | [entry](graziani-marine-review/reading-independent.json) | [comparison](graziani-marine-review/comparison-independent.json) | Counter abbreviation `1` versus printed `“1”`, and two equivalent descriptions of `D`; four locator strings differ. Reader reports same source locations. Later identity and source precedence remain separate. |
| Cirene/Marmarica members, 29 rows / 120 fields | [entry](graziani-axis-formation-review/reading-independent.json) | [comparison](graziani-axis-formation-review/comparison-independent.json) | Eight equipment marks differ only in spaces around `×`; two tank attachment labels differ by ordinal suffix (`63`/`63rd`, `62`/`62nd`); 29 locator strings differ. Reader reports same source identities. |
| Cirene/Marmarica relationships, 4 rows / 14 fields | [entry](graziani-axis-formation-review/relationship-reading-independent.json) | [comparison](graziani-axis-formation-review/relationship-comparison-independent.json) | One tank notation spacing difference and two ordinal suffix differences; four locator strings differ. Preserve scenario `Ar/LTC` separately from OA parent and attachment. |

For these entries, source packages matched each packet manifest; compare reports
retain source hashes and every raw difference. Dispositions above are bounded
interpretations of format and locator differences, not modifications to either
reading or the comparator. The Axis reader also ran the documented first-reading
relationship consistency verifier and four unit tests successfully; those tests
check internal joins, not the truth of the independent entry.

## Rules, victory, fortifications, ports, weather and patrol

- [Rules reading](graziani-rules-review/reading-independent.json),
  [raw comparison](graziani-rules-review/comparison-independent.json): 15
  locator differences and four value differences. Land §20.15's Benghazi
  diversion depends on no Commonwealth land-combat unit west of Mersa Matruh
  inclusive, stacking, port efficiency, unit-equivalent and transport limits;
  the independent entry spells out conditions omitted by the first entry's
  destination-only field. Land §32.42 says **one supply unit for every three
  received** may be placed at Alexandria when more than three are received;
  the first entry's `units_per_alternative: 3` does not encode that ratio
  unambiguously. Preserve both, and specify numerator, denominator, threshold
  and any rounding rule explicitly before implementation. For field repair, the
  readers represented absence of expenditure as `none` versus `0`; a later
  source recheck of Land p. 35, §§22.23–22.24, confirms both cases explicitly
  require no supply expenditure. This does not prove whether a distinct general
  supply-availability condition in §22.13 applies.
- [Victory reading](graziani-victory-review/reading-independent.json),
  [raw comparison](graziani-victory-review/comparison-independent.json): eight
  locator differences and 13 wording differences. The independent reader reports
  the §60.81 heading spelled `Grazianni` while §60.22 uses `Graziani`; preserve
  the source-specific spelling difference. The other differences include
  explicit quantifiers and coordinates; do not normalize them away when
  choosing winner and supply semantics. Both readings retain the §60.71
  cross-reference and six-turn end boundary.
- [Fortification reading](graziani-fortification-review/reading-independent.json),
  [raw comparison](graziani-fortification-review/comparison-independent.json):
  all values agree, with three locator differences. This does not settle whether
  the scenario removes intrinsic city fortifications.
- [Port reading](graziani-port-review/reading-independent.json),
  [raw comparison](graziani-port-review/comparison-independent.json): seven
  locator and three value differences. The independent entry preserves §55.14's
  direction of rounding port-capacity reductions **upward** to the nearest ton;
  `nearest ton` alone omits that direction. Other differences are wording. The
  7/5/three-level Tobruk source conflict remains unresolved.
- [Weather reading](graziani-weather-review/reading-independent.json),
  [raw comparison](graziani-weather-review/comparison-independent.json): nine
  locator and eight value differences, including boolean versus count for the
  foul-weather location die and punctuation of printed ranges. Errata §§29.1
  and 29.61 contain different examples for the spring boundary; retain that
  distinction. Neither reading independently approves the existing season ruling.
- [Patrol reading](graziani-patrol-review/reading-independent.json),
  [raw comparison](graziani-patrol-review/comparison-independent.json): ten
  locator differences, six value differences and one independent unknown. Land
  §16.51's original main text specifies the reported unit and approximate
  strength; printed p. 45's §16.51 addendum separately says the patroller does
  not identify the types of constituent TOE points. The fresh reader recorded
  `null` after inspecting the main case but did not cite the addendum. A later
  [attributed independent addendum check](graziani-patrol-review/independent-addendum.md)
  confirms the negative; preserve the original `null` and raw comparator output.
  Other wording differences, especially stage conditions and
  route restrictions, need source-specific review rather than blanket dismissal.

## Transport, convoy and rail

- [Transport reading](graziani-transport-review/reading-independent.json),
  [raw comparison](graziani-transport-review/comparison-independent.json):
  nine locator and six field differences. The fresh reader retained printed
  `Torbruk` in Land §32.36 where the first entry normalizes `Tobruk`; keep the
  printed spelling and canonical port identity distinct. Other differences
  add phase or direction wording; they do not by themselves settle reachability.
- [Convoy reading](graziani-convoy-review/reading-independent.json),
  [raw comparison](graziani-convoy-review/comparison-independent.json):
  11 locator and eight field differences. Axis chart §32.66, PDF p. 34, defines
  the **qualifying** Allied division itself as complete (non-shell), in addition
  to requiring another non-shell Allied division in the specified relative
  position. The first entry's field records the latter qualification but omits
  the former; retain both and do not dismiss this as punctuation. The §41.67
  point-type categories in the independent entry are more explicit than the
  first entry's nationality-only summary and need source-specific disposition.
  Several other differences concern punctuation, interval dash style or labels.
- [Rail reading](graziani-rail-review/reading-independent.json),
  [raw comparison](graziani-rail-review/comparison-independent.json): ten
  locator and five value differences, including expanded names and the Matruh
  coordinate. These do not decide the separate Land-only applicability, phase,
  stock or construction questions.

All three readers' source hashes matched their manifests. These records are
same-edition, fresh-context transcriptions, not official clarifications, legal
rights determinations or proof that the relevant actions are scenario-reachable.

## Targeted follow-up on comparator questions

The [field-by-field audit](2026-11-17-graziani-comparison-audit.md) enumerates
all 75 raw disagreements and the independent unknown, including material source
omissions. The following same-stream visual rechecks narrow some of its `N`
items; they do not rewrite either reading or close unrelated rule questions:

- Land p. 43, §§32.32–32.34 and 32.37: supply-unit motorization attachment is
  during an operation stage's Organization Phase; the movement/convoy choice
  belongs to the owning player and excludes using both; Commonwealth supply
  rail permits any **one** direction in an operation stage; abstract air
  transport occurs during an operation stage's Organization Phase. Preserve
  all stated additional prerequisites, timing and phase limitations.
- Land p. 41, §29.1: after a sandstorm or rainstorm result, roll **one** die
  for the Foul Weather Location Table. The boolean/count discrepancy is a
  schema representation issue if both the condition and exact count are stored.
- Land p. 27, §16.51: report includes the historical designation, unit type,
  motorized status and strength within two of actual. Land p. 45 addendum
  separately excludes the types of constituent TOE points; the independent
  reader later verified that addendum without altering the initial unknown.
- September errata p. 2, §16.11 adds Italian L/6, Commonwealth Stuart, and
  mechanized infantry (or Panzergrenadiers) of either side. Apply patrol
  restrictions and source-specific visibility separately.
- Land p. 44, §32.65(B) states tank and gun losses are percentages of totals
  within each nationality, not each individual weapon type. Its example uses
  tanks only; whether tanks and guns form separate categories for this specific
  abstract-convoy case is not conclusively settled by that example. Detailed
  Air §41.67's four categories are separate source evidence, not an automatic
  override of §32.65(B). Keep the grouping question open until reconciled.

Source copies are identified in the packet manifests and the raw status report.
