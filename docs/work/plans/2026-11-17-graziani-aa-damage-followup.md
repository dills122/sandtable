# Land-only fleet damage and AA wording: bounded follow-up

**Disposition, corrected:** §32.74 produces ship damage points; §30.34 converts
them into Gun Rating loss, and §30.13 explicitly reduces ship AA points in
ratio to Gun Point loss. That rule was missed in the first pass. Its precise
rounding, repeated damage and repair arithmetic remain unresolved; do not
substitute a guessed formula or change runtime on this source pass.

Later [counter research](2026-11-17-graziani-ajax-counter-check.md) and
[independent review](2026-11-17-graziani-ajax-counter-check-review.md) read
Ajax as `CL 3/6`, conflicting with the example's initial Gun Rating 4; the
example and counter remain distinct source evidence.

## Printed evidence and limits

- Land §32.74(b), p. 44, directs the Axis player to distribute the chart's
  damage points among ships in Alexandria Harbor. It does not describe AA
  rating loss, overflow, empty-harbor behavior, or an allocation algorithm.
  The reference to §30.46 identifies the damage-point concept; it does not
  establish the separate Chariot Raid's availability or timing.
- Land §30.13, p. 41, states AA points are reduced in ratio to loss of Gun
  Points. Section 30.12 identifies Gun and AA as distinct naval counter
  ratings. The proportional rule governs the loss relationship, but these
  clauses do not specify rounding, intermediate fractional ratings, or
  accumulated-event arithmetic. Applying it to §32.74 follows from reading
  ship damage through §30.34; §32.74 gives no separate AA example.
- Land §30.34, p. 42, says ship damage is taken in Gun Rating Points and
  reduces Gun Rating accordingly. Its example gives Ajax, identified as a 4,
  two damage points, leaving Gun 2 and AA 4. The example does not state Ajax's
  initial AA rating in that sentence; without independent counter evidence it
  does not itself prove AA remained unchanged. Its final sentence directs
  firing with reduced Gun and AA ratings, if any, consistent with §30.13.
  Compare the example with the actual Ajax counter before claiming either
  consistency or conflict: if its initial AA were 4, the example would leave
  AA unreduced after 50% Gun loss; if initial AA were 8, AA 4 would fit.
  Neither initial value is established by the example itself. No complete
  naval or air rules search was performed here.
- §30.36 says a ship whose Gun Rating falls below half its face value cannot
  leave port until repaired to **better than** half. At exactly half, arriving
  from above does not satisfy the stated below-half trigger; a ship already
  restricted after falling below remains restricted at exactly half on repair.
  This history-sensitive interpretation follows the words, subject to another
  governing rule. Loss of the entire Gun Rating sinks the ship and makes it
  unrepairable. The printed addendum at p. 45 §30.36 also eliminates a commando
  unit aboard a warship eliminated while transporting it; applicability to an
  actual reachable transport requires separate evidence.
- §30.37 repairs one Gun Rating level after six consecutive Operations Stages
  in Alexandria, with no engineer or supply expense; moving before the six
  finish restarts work. It does not specify repairing AA ratings or how to
  order the sixth stage against a concurrent attack. Whether damage interrupts
  work, and treatment of progress when sunk, need an explicit policy or more
  source evidence rather than invention from the movement clause.
- §30.38 says ships may fire as anti-air guns even during repair. This is a
  permission, not proof that detailed AA fire is
  available in the Land-only abstraction. Section 32.8 removes specified
  land AA units and points; its categories do not establish that a ship's AA
  rating is a removable TOE or Replacement Point. Keep these separate.

## Discriminating cases for a future ruling

1. With a present eligible ship at Gun 4, allocate two damage points: Gun 2;
   reduce AA in ratio to lost Gun per §30.13. A synthetic face AA 8 gives AA 4
   after 50% Gun loss without rounding. The Ajax example specifies resulting
   AA 4; independently establish its starting AA before asserting consistency
   or conflict. Preserve choice of target and distinct damage, Gun and AA
   values; do not debit land AA inventory. Exercise fractional ratios, separate
   hits versus one combined hit, and replay under an explicitly chosen rounding
   policy; no rounding rule was established in this bounded pass. Do not infer
   a one-for-one AA subtraction across ships with different face ratios.
2. With multiple eligible ships, allocate the same total differently and
   verify each ship's Gun loss and total allocated points. Distinguish no
   targets, zero damage, over-allocation and damage beyond remaining Gun;
   source reviewed here does not prescribe overflow or allowance consumption.
3. For face Gun 4, exercise 3→2, 2→1, repair 1→2 and repair 2→3: entering
   exactly half from above does not trigger the printed restriction, below half
   does, and an already restricted ship remains restricted at half until
   better than half. Track prior restriction; a stateless `current <= half`
   or `current < half` check alone cannot express both paths.
4. At zero Gun, apply sinking and unrepairability. Separately cover the p. 45
   commando addendum when the prerequisite transport exists; do not infer one
   exists in the six-turn scenario. Establish bounds for extra damage before
   accepting or discarding it.
5. Test five completed consecutive Alexandria stages versus six, departure
   before completion and replay of the completion event. Separate attack
   during repair, sixth-stage attack ordering, and damage during work as
   unresolved vectors; AA restoration on Gun repair is not specified by these
   clauses and must not be assumed.
6. Distinguish the scenario-wide secret attack allowance from the independent
   attack-column die, eligible harbor snapshot and section E storm gate.
   Verify plot/repair sequencing only after the missing segment mapping is
   decided. No vector here asserts an attack or specific ship is reachable.

## Source and errata scope

Visually inspected hash-matched [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
PDF/printed pp. 41–42, 44–45, §§30.12–.13, 30.34–.38, 32.74–.84 and printed
addendum §30.36;
SHA-256 `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
Checked [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf),
SHA-256 `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`,
for §§30.13, 30.34–.38, 32.7–.8 and §41.5: no directly numbered damage or AA
mechanism correction found. Its §41.5 correction changes barrage column
headings and flak terminology; it does not change §30.13’s ratio rule.
Its §32.0 note warns abstract rules were untested. This is a bounded check of
one errata document, not proof no later clarification or other governing rule
exists. No runtime changes or gameplay tests.

The [independent review](2026-11-17-graziani-aa-damage-followup-review.md)
identified §30.13 and prompted the correction above.
