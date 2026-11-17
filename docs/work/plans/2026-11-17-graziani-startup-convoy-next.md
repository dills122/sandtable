# Graziani Axis convoy startup: GT1–3 planning boundary

**Status:** bounded source and conditional sequencing matrix, not an admitted
startup contract, reconstructed pregame ledger, actual roll or delivery. The
scenario requires September convoy preplanning, but the inspected sources do
not settle whether the turn-1 pre-operation Naval Convoy Stage is played,
initialized or skipped. Do not use either interpretation to manufacture cargo,
reroll commitments or suppress a distinct obligation silently.

## Source anchors

Visually rechecked hash-matched [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
pp. 11–12 and 43–44 §§5.1–.2, 32.41–.44, 32.57 and 32.61–.65;
[scenario booklet](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf)
PDF p. 3 / printed p. 27 §§60.22, 60.31, 60.37, and PDF pp. 4–5 §60.92;
[Axis charts](https://spigames.net/PDFv10/CNA_ChartsAxisPlayer.pdf) PDF
pp. 34 and 36 §§32.46 and 56.4; and [map package](https://spigames.net/PDFv10/CNA_Maps.pdf)
PDF p. 4, Map D turn track. Their SHA-256 values respectively match
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`,
`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`,
`9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca`,
and `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`.
Compared the [first-month timing review](2026-11-17-graziani-first-month-review.md)
and [supply schedule](2026-11-17-graziani-supply-schedule-next.md). This is a
prompted check of the same scanned edition, not a separate historical source
or exhaustive errata audit.

- §60.22 names **GT1 OpStage 1** as the scenario start. The map track labels
  GT1 September III, GT2 September IV and GT3 October I, 1940.
- A normal §5.2 Game Turn has initiative, then a **Naval Convoy Stage**
  containing a schedule phase and tactical-shipping phase, then OpStages 1–3.
  Each OpStage separately has its own **Naval Convoy Arrival Phase**. Planning
  before the stages and actual arrival within a stage are different events.
- §60.37 requires the Axis player to plan convoy runs for the **remainder of
  September before starting**, using only lanes 2, 3 and 6, and consult the
  availability charts. This is not an express waiver of the entire GT1 naval
  stage, proof of a positive convoy on any specific stage, or a stated October
  lane restriction. The phrase's exact treatment of the opening partial turn
  and content/timing of initialized records require interpretation.
- §32.43 says to plan at the beginning of each Game Turn, Axis two turns in
  advance, with players able to plan as far ahead as necessary for arrivals on
  all turns **except the scenario's starting Game Turn**, if planning dice
  permit. §32.44 uses the month **of planning**, rolls one die against the
  Axis simplified availability table, and gives the number of real supply
  units the player **may bring in** two turns hence. This exception does not
  erase §60.92(B)'s scenario starting stock or prove that every other kind of
  opening convoy is empty. Keep supply availability separate from a planned
  lane, actual allocation, transport, loss and landing.
- Axis §56.4 prints convoy letter B for September and October 1940; §32.46
  column B gives die 1–6 → `0, 1, 1, 2, 2, 3`. Apply B only when the
  applicable planning month and normal chart are established. A printed zero
  from an actual roll differs from an unknown or absent roll. The prior supply
  schedule records the optional errata §56.29 rearrangement as a separate
  applicability question; it was not independently re-audited here.

## Conditional matrix

| Target / boundary | What is source-supported | What remains unknown |
| --- | --- | --- |
| **GT1, Sep III** startup | §60.92(B) establishes initial Axis supply placements; §60.37 requires September convoy preplanning. §32.43 excludes the starting Game Turn from its conditional expectation of later supply arrivals if rolls permit; it does not guarantee a positive receipt. Each reached OpStage still has an Arrival Phase under the normal outline. | Whether GT1's preceding Naval Convoy Stage is played, already accounted for or omitted; whether any permitted preplanned non-ordinary convoy actually arrives, its lane/stage/cargo and losses. Starting stock is not a new rolled shipment. |
| **GT2, Sep IV** target | Its ordinary Axis planning offset is two turns earlier, before the scenario's GT1; its September convoy runs fall under §60.37's pre-start planning and lanes 2/3/6. If a lawful planning month is September and the normal §56.4 chart applies, B is the table input. | The actual pregame planning record and date, applicability and result of any §32.44 roll, optional variants, lane allocation, stage, quantity and survival. A numerical pre-scenario slot is an arithmetic label, not proof of an existing Sep II track turn or die. |
| **GT3, Oct I** target | Ordinary offset is GT1 Sep III; if that planning is performed in September with the normal chart, use B. §60.37's September convoy constraint alone does not prove an October lane restriction or a GT3 commitment. | Whether GT1 beginning-of-turn planning is executed or provided by a separate explicit initialization; actual roll, allocation, lane, stage, losses and receipt. Missing GT1 planning cannot be silently converted to zero or a fabricated pregame roll. |

An availability result of three means at most the source's permitted planning
quantity, not three units landed. Prior commitments, pool and per-lane
limits, capture/cancellation, destination, actual arrivals and loss-group
rules must be reviewed separately. Section 32.61 includes motorization and
replacement points as well as supply units in simplified convoys; the
starting-turn supply-table exception is not evidence all such categories are
absent.

## Startup alternatives and bookkeeping

**Full opening preamble:** retain required §60.37 pregame September
commitments, then execute applicable GT1 initiative and Naval Convoy Stage
before OpStage 1. Under the ordinary two-turn planning reading, GT1 §32.44
plans GT3 using a September-month lookup if not already validly initialized.
Section 60.6 fixes Italian initiative for GT1; do not invent a new initiative
winner by rerolling it. Commonwealth GT5 planning has the same GT1 startup
entry question under its four-turn offset. Section 32.43 specifies the beginning
of the turn but does not independently name an exact sequence phase for its
lookup.
This needs a rule for matching or reconciling any existing GT3 commitment;
executing the preamble is not permission to roll twice.

**Direct OpStage-1 entry:** accept that §60.22 skips the earlier GT1 preamble,
while still carrying §60.37's required September pregame plans into the first
OpStage. A GT3 supply entitlement cannot simply arise from a skipped GT1 plan:
require a separately sourced, versioned initialization/deferred-planning
policy or leave it unknown. §60.37's preplanning for the **remainder of
September** alone is not proof of a GT3 October supply roll. OpStage arrival
phases are not thereby skipped. Neither alternative proves the other wrong or
converts a missing plan into zero.

Record immutable setup stock, pregame plan provenance, planning month/table,
roll if any, available quantity, lane, actual shipment and arrival/loss as
separate state; reconcile duplicate identities rather than replaying the same
plan. Preserve private cargo and future plans through player and intelligence
views, including action/diagnostic timing. A missing prerequisite blocks the
specific dependent decision rather than permitting guessed state or a second
roll.

§32.57's first Naval Convoy Stage of each month is a further **separate**
startup issue: whether a partial September start triggers it, whether GT2 can
become the first encountered stage under direct entry, its month key and chart
dash semantics are unresolved. A decision about September cargo preplanning
does not answer that question.

## Proposed checks

Unexecuted vectors: exercise full versus direct startup, a valid explicit GT2
September preplan, GT3 missing versus explicitly initialized planning, B
result zero versus unrolled/unknown, duplicate plan identity and replay; enforce
2/3/6 for the applicable September planning scope while testing October under
its own accepted availability rules. Distinguish setup stock from arrivals,
arrival-phase no-arrival from an unclassified cargo category, lane
announcement from hidden cargo, and checkpoint restore from a new roll. Test
GT1, GT2 and GT3 boundaries, stage-specific delivery and monthly-loss
applicability separately. No runtime, readiness or content changes, and no
gameplay tests ran.
The [independent startup review](2026-11-17-graziani-startup-convoy-review.md)
confirms the bounded arithmetic and flags these initiative, GT1 planning and
conditional-arrival qualifications.
