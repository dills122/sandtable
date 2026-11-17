# Graziani training and arrival-stage order: bounded proposal

**Status:** provisional interpretation for review, not an admitted runtime
contract or proof of which scheduled units need training. Proposed
`gra-training-arrival-v1` follows the ordinary stage sequence for whole-unit
morale training and separately honors the explicit replacement-point
arrival-stage provision. A competing reading extends that exception to
reinforcements; keep the gate closed until source hierarchy and training
contracts are accepted.

## Source findings

Visually checked hash-matched [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
printed/PDF pp. 12, 27–28 and 31, especially §§5.2, 17.31–.36 and 20.11–.14,
20.21, 20.42–.43; [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
pp. 1–2 §§8.86 and 17.3; and [scenario booklet](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf)
PDF p. 4 / printed p. 28 §60.47. SHA-256 respectively
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`,
`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`,
and `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`.
This is a prompted same-edition check, not a blind independent edition or an
exhaustive review of all training interactions.

- §5.2 places Organization, including Training completion and initiation/
  continuation, **before** Naval Convoy Arrival in each OpStage. Its
  Organization segments may be performed in any order within that phase; that
  does not move Naval Arrival into it. Units beginning or continuing training
  may not voluntarily move in the remainder of the stage, with reaction
  excepted. Completion is a separate step.
- §20.12 puts whole-unit reinforcements at the **beginning** of Naval Convoy
  Arrival, charges no CP for disembarkation or the first placed hex, and allows
  movement in the arrival stage; §20.13 treats arrived units normally. Errata
  §8.86 corrects the contrary Tripoli/Tunisia-specific printed restriction; it
  is not independently a blanket rule for Commonwealth Cairo arrivals. These do not by
  themselves erase other applicable movement or training rules.
- §17.31 distinguishes untrained from maximum basic morale on applicable OA
  sheets. §§17.32–.35 specify permitted locations, no CP expenditure during a
  training OpStage, six training OpStages per morale point, and a qualified
  instructor in the hex during each training stage. §17.36 allows units to
  fight at untrained morale instead of training. Scenario §60.47 warns that
  some units need training and instructors; it does not say all arrivals must
  train or settle the stage-order conflict. Training interruption consequences
  beyond the fact of interruption require further review.
- §20.21 places replacement points in Naval Convoy Arrival; §20.43 expressly
  says their required training stages **may include the arrival stage**, and
  cross-refers to §17.3, saying the rules applying to training reinforcements
  apply to replacements. It separately states replacement-point training is
  not Commonwealth morale training, and lists one stage for gun, three for
  infantry, six for tank/AC/recce replacements. Errata §17.3 says Axis training
  centers train replacement points, not actual units. This exception cannot be
  dismissed merely because Organization precedes Arrival.

## Proposed ordering and limitations

1. **Whole-unit morale training:** a newly arriving reinforcement was not
   present at that stage's earlier Organization/Training step. Do not begin or
   credit its §17.3 morale training in its arrival OpStage under this policy;
   allow an eligible unit to elect training at a subsequent Organization step.
   Arrival-stage movement remains available when otherwise legal and the unit
   is not under another applicable restriction. A unit choosing not to train
   may fight at its applicable untrained morale; don't invent mandatory morale
   training for the final turn-6 stage-3 brigade. At the end of this scenario,
   that brigade has no subsequent in-scenario Organization step, but its actual
   morale status and roster remain independently gated.
2. **Replacement points:** the explicit §20.43 language is a specific
   exception for those points, not permission to reopen all Organization
   segments or retroactively train other units. Proposed bookkeeping: on a
   qualifying arrival, record a pending arrival-stage training attempt; at
   stage end credit at most one stage only if the applicable location,
   instructor and no-CP requirements are satisfied for the legally relevant
   interval, and no other restriction prevents training. Define instructor
   presence from arrival through the stage's end as a project interpretation
   for an entity not present earlier, not as explicit printed timing. Do not
   commit the credit early or assume that zero-cost placement alone guarantees
   it. The exact selection time, instructor identity and classification,
   whether a qualifying point can complete its one-stage training and act in
   the same stage, and interaction with the earlier completion step remain
   **unknown**; block dependent actions until decided. Other replacement-point
   arrival and training restrictions apply separately.
3. **No-CP versus no-voluntary-movement:** for a unit that began or continued
   training in Organization, §5.2 independently prohibits voluntary movement
   for the rest of that stage even if a proposed movement costs zero CP;
   source-excepted reaction is permitted only when otherwise legal. §17.33 says
   **any** CP expenditure during an OpStage interrupts training, including CP
   spent in a permitted reaction or an involuntary action; a zero-CP reaction
   does not violate that particular CP test, but other instructor/location and
   interruption rules must still be checked. Do not equate no CP with no
   movement, or assume reaction automatically preserves training credit.
   Training completion without a new continuation should not automatically be
   labeled initiation/continuation; actual completion timing, permitted actions
   and credit across interrupted stages require their own accepted rule.
4. **Snapshot and replay:** establish eligibility and timing against
   authoritative state, preserve distinction between a scheduled arrival and
   one actually placed, record instructor, selection, interruption and stage
   credit once, and bind choices to the ruleset. A failed/unknown prerequisite
   must not silently grant a stage or erase previously earned progress. Do not
   infer consecutive stages or reset progress without a separate ruling.

This is not the only plausible reading. §20.43's sentence applying the same
training rules to replacements could be read to extend the specific arrival-stage
exception to whole reinforcement units as well. Alternatively, §17.3's
stage-long requirements might require special prearrival or arrival-time
handling. The proposed distinction gives effect to the clause's explicit
replacement-point subject and to §5.2's sequence; it is **not** an official
clarification. Resolve that hierarchy, the eligible training types and source
errata before admitting historical behavior. Confirm whether the special
replacement procedure applies in Land-only mode, which points can actually
arrive in the six turns, and whether replacement-point instructor rules differ
by side or class.

## Proposed distinguishing tests

These are unexecuted policy vectors, not claims of legal historical setups:

- A trained and an untrained whole-unit reinforcement arrive after the
  Organization step: neither gains same-stage morale credit; the untrained
  unit may move/fight if otherwise eligible. In the next stage, a valid
  selection, location and instructor allow a training attempt. Compare the
  alternative policy explicitly allowing same-stage training.
- A replacement point arrives at a permitted training location with a valid
  instructor and no CP expense: arrival stage may earn one pending credit;
  no instructor, wrong location, CP expense or unknown classification prevents
  a confirmed credit. A gun point reaching its one required stage does not
  become immediately usable until the completion/absorption boundary is
  separately accepted.
- A unit beginning/continuing training attempts zero-CP voluntary movement:
  reject under §5.2 even though no CP would be spent. Legal reaction with CP
  interrupts the training attempt; test a zero-CP reaction and an involuntary
  CP expenditure separately, including location/instructor changes.
- Distinguish completion from continuation; test six eligible stages and the
  morale maximum, missing instructor on one stage, nonconsecutive stages and
  interruption without assuming an unreviewed reset rule. Verify any dependent
  unresolved completion returns typed unsupported with no partial mutation.
- Exercise first/last-player order, arrival-stage Organization ordering, turn-6
  final-stage boundary, source-specific replacement/whole-unit identity,
  checkpoint replay, duplicate arrival/credit and filtered opponent views.

No code, runtime state or readiness flags changed, and no gameplay tests ran.
