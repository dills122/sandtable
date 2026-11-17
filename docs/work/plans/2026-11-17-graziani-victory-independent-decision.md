# Graziani six-turn victory: independent decision proposal

**Status:** planning proposal, not an official correction, accepted rule, runtime
contract, reachability proof, or release approval. This review selects a narrow
result-composition policy for consideration and keeps historical victory
adjudication fail-closed. Its source reading is independent of the earlier
analysis but uses the same edition and the same scan files, not an independent
edition. No code or tests changed.

## Evidence and limits

- [Original scenario scan](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf),
  printed p. 27 §60.22 and p. 28 §§60.81–60.82: the six-turn scenario ends
  at the conclusion of turn 6, OpStage 3, but points to §60.71; the named
  six-turn victory heading is §60.81. Section 60.82 is the longer campaign.
  The working [RUL-GRA-02](2026-11-12-graziani-ruling-decisions.md#rul-gra-02-victory-reference)
  selects §60.81. The reference correction is an inference, not an erratum.
- The same p. 28 names Italian strategic (Alexandria **or** Cairo; any hex;
  combat unit; convoy from any map D hex), decisive (both Mersa Matruh and
  Siwa; combat unit; convoy from Tobruk), and tactical (Sidi Birani; retain
  Sollum, Fort Maddalena and Giarabub; supply as above to Tobruk). Commonwealth
  tactical names Sollum, Halfaya Pass C3922 and Siwa C0127, with a truck-convoy
  trace to Alexandria or Cairo; decisive and strategic each say supply as above.
  The p. 28 location list prints Sidi Barrani C4131; matching the spellings is
  a plausible map-identity choice, not a silently corrected transcription.
- [Original Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf),
  printed p. 43 §§32.11, 32.16, 32.32–32.33, replace supply expenditure,
  specify local supply-unit access and retain motorization and the Truck Convoy
  phase. Section 32.16 blocks impassable terrain and enemy ZOCs unoccupied by
  friendly units for **local draw**; it does not prescribe the victory route.
  Section 32.58, printed p. 44, assigns CPA 15 to motorization transporting a
  supply unit; it does not set a hypothetical victory-route limit.
- [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf),
  PDF p. 2 §32.0, warns that abstract rules were untested. Text search of all
  four pages found no §60.81 or §60.22 correction; this is not proof of absence
  from every possible edition or ruling. The inspected scenario, Land and errata
  files respectively hash to `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`,
  `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`, and
  `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.

The [blind source readings](graziani-victory-review/comparison-independent.json)
agree on the essential categories but differ in spelling and transcription;
they do not establish policy. The [source review](2026-11-16-graziani-victory-supply-source-review.md)
and [matrix](2026-11-12-graziani-victory-matrix.md) retain details and alternatives.

## Proposed result-composition policy: `gra-victory-result-v1`

This is a **Sandtable planning choice**, contingent on acceptance and a separate
complete predicate contract. It deliberately expresses outcomes without inventing
an official tie-break or treating an incomplete evaluation as a draw.

1. Evaluate each of the six §60.81 rows independently against the same immutable
   terminal snapshot. Preserve each row's `match`, `no-match`, or `unknown`
   result and its reason. A known false required conjunct makes that row
   `no-match`; otherwise any unavailable required conjunct makes it `unknown`.
   Do not treat unimplemented route, missing map data or missing state as false.
2. If **any** row is unknown, report `unresolved` with no winner or level.
   Retain row results for diagnosis; do not terminalize an official verdict.
3. If all rows are known, select each side's highest matching label, in order
   strategic, decisive, tactical. Do not require lower rows to match: their
   location sets are not nested. Preserve all six matches as evidence.
4. Exactly one side matching yields that side and its highest label. Neither
   matching yields `no qualifying victory`; both matching yields `simultaneous
   qualifying victories`, with each side's highest label and **no sole winner**.
   Neither result becomes a default win, draw, or cross-side label comparison.
5. Bind the policy ID and predicate/source versions to the ruleset and recorded
   verdict. Existing saves retain their original ruleset; changing policy creates
   a new version, never a reinterpretation of a prior result. Authoritative
   evidence remains private until filtered for each player's permitted view.

Rationale: labels naturally rank achievement within a side; independent row
evaluation preserves the printed, non-nested objectives. Reporting both outcomes
instead of choosing a stronger label across sides avoids an unsupported comparison.
Alternatives include first printed row, requiring all lower rows, awarding neither
match to a default side, or using §60.82 points. Each adds an unsupported rule or
silently discards relevant matches. Reporting unresolved whenever any row is
unknown is conservative, including when a known row already qualifies.

The snapshot must follow **all** turn 6 OpStage 3 procedures and any accepted
end-of-stage cleanup, before later play or mutation. Exact event ordering is
still a prerequisite: source timing does not itself define this implementation's
cleanup boundary. An unresolved evaluation may be retried after missing evidence
is supplied, but must not be published as a finished historical verdict.

## Land-only route: defer complete policy

The source leaves genuine interpretation choices, not merely missing
transcription: what “as above” applies to, the units/locations that must trace,
whether a route denotes actual shipment or hypothetical connectivity, eligible
map-D origins and source control, terrain/hexsides, distance, enemy occupancy and
ZOCs, friendly neutralization, weather and the Commonwealth western restriction.
It also leaves possession and multi-hex objective semantics to resolve. Choosing
these jointly can change both which side qualifies and whether either can win;
neither the scan nor its errata supplies a complete algorithm.

Hypothetical medium-truck connectivity is the strongest **candidate** because it
retains an end-to-end named-source condition without manufacturing shipment
history. It remains underspecified until each question above and map seams are
settled. Actual or presently feasible shipment preserves motorization constraints
but invents timing, transport availability and relay rules; local §32.16 draw is
source-defined but can succeed with no path to the named source. Dropping supply
entirely contradicts the explicit conditions. No alternative is safe to freeze
as historical adjudication on current evidence. Use `unknown`, not an optimistic
route, local-draw substitution, zero-length route, or fabricated shipment. The
unaffiliated community proposal cited in the source review is comparison evidence,
not official errata.

Before acceptance, version route semantics and row-specific subjects together;
prove map geometry and relevant movement rules complete, specify invalid/missing
inputs as unknown, and resolve the terminal boundary and location/control identity.
Reachability through six turns remains a separate proof obligation.

## Acceptance vectors, not executed tests

- Six separately qualifying rows and removal of each printed condition; one
  decisive objective missing its route; strategic one-of location and map-D
  origin versus disconnected alternatives; no extra lower-row prerequisites.
- Multiple levels on one side, both sides matching at unequal levels, neither
  matching, and unknown on either side even with a known match. Preserve all row
  evidence and distinguish false from missing data.
- Route candidate tests: local draw without source connectivity; connectivity
  without local draw or available transport; enemy unit/ZOC with and without
  friendly occupant; impassable terrain and hexside; missing seam; blocked source
  or endpoint; weather and western restriction; actual shipment versus no shipment.
  Expected outcomes require an accepted route policy, not assumptions here.
- Before/at/after final boundary, exactly-once verdict, checkpoint restore,
  deterministic replay, stable policy identity, old-save isolation and filtered
  player views. Synthetic combinations do not prove historical reachability.
