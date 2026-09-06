# Combat Selected-Source Manifest and Continuation Facts

**Status:** `CMB-TASK-001` research decision-ready; source normalization **blocked** on
`CMB-SRC-RUL-001`. No production table is admitted. **Date:** 2026-09-06.
**Input:** `9b71faa` after owner acceptance of POL-001–008 and corrected plan at `a10a588`.

## Conclusion and decision requested

The complete selected surface contains **360 role/differential/loss coordinates**. Both manual and
optical source extraction define 357 values and find the same three missing cells: defender, final
differential +2, rolls 34/35/36. The [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
15.79 correction concerns +4, not +2. No applicable correction was found in the retained baseline.

**Recommend `CMB-SRC-RUL-001`: assign 10% defender loss to those three coordinates**, extending the
printed 10% interval 24–33 to 24–36. This is a proposed Sandtable ruling, **not** a transcribed source
fact. Owner decision remains pending. The proposed repair preserves all 357 defined source values,
uses an existing result value and closes the selected surface without excluding a seed or outcome.

| Option | Observable effect for 10 committed defender TOE | Assessment |
| --- | --- | --- |
| Extend 10% band through 36 | One base TOE loss on 34/35/36; on 34, refusal of its one-hex retreat raises loss to 2 | Recommend: one endpoint change and smoother neighboring loss thresholds. Historical intent remains unproved. |
| Start 5% band at 34 | Zero base TOE loss on 34/35/36; refusal on 34 raises loss to 1 | Coherent alternative, but changes the next band's lower endpoint instead. Requires its own owner ruling. |
| Leave unresolved | No supported result for three reachable rolls | Current authority: block selected-profile admission before mutation. Never wait to discover the missing row after drawing. |

POL-001 approved complete selected coverage, not fabricated missing values. Approving this new ruling
must record the owner/date and amendment identity separately; TASK-002 may inspect static facts, but
source-dependent contract acceptance/TASK-005 cannot treat the proposed repaired table as adopted.

## Method and source index

[Source baseline](cna-source-material-spike.md) permits derived factual data with provenance and
requires explicit rulings for ambiguities. Scans, copied chart layout/artwork and OCR prose remain
outside Git. The [research fixture](fixtures/combat-selected-source-v1.json) retains source hashes,
manual numeric bands, a separate optical coordinate vector, declared gaps and the unapplied proposal.
It is not a production Rules schema, version allocation or activation capability.

| Source | Inspected locators | Evidence role |
| --- | --- | --- |
| [Common charts](https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf) | PDF3/15.89; PDF4/15.79; PDF5/17.4 | Selected loss bands, sum flags, captured shares and Cohesion 0 Morale. |
| [Original compilation](https://www.spigames.net/PDFv2/CampaignNorthAfrica.pdf) | PDF98–100, corresponding charts3–5 | Copy comparison and optical extraction input. Assault renders are pixel-identical to the separate chart PDF; these are not independent historical editions. |
| [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) | PDF11/5.1; PDF13/6.21–6.26; PDF14/8.15–8.24; PDF15/8.61–8.68 | Game time, excess-CP Cohesion, ordinary break-off costs and membership. |
| Land rules | PDF31/20.21,20.43,20.49; PDF32/20.72; PDF40/28.15–28.24 | Replacement delay/arrival/training distinction, prisoner upkeep and escape. |
| September 1979 errata | 8.23,15.27,15.79,20.72,28.17,29.61 | Corrected continuation/dice, out-of-scope+4 loss band, one-month planning and calendar notation. |

Manual loss ranges were entered from the rendered chart. Separately, Apple Vision extracted 56
occupied loss-band cells by image position; expansion to valid ordered-d6 coordinates agrees with
manual expansion, including the three undefined entries. This is an independent **extraction
process**, performed by the same author; no independent human/agent review or fifth engineering
review is claimed. The fixture preserves the optical vector and both render/OCR-output hashes.

A bounded web search checked the SPI preservation errata index and the retained September sheet;
no matching +2 correction was located. This does not establish that no additional erratum exists.
The duplicate PDF scan cannot resolve the ambiguity. Another identifiable primary correction or
owner-supplied physical chart would supersede the inference after provenance reconciliation.

## Ruling follow-up: wider evidence and comparative decision

**Research input:** `ac5b188`, 2026-09-06; owner requested a research-backed decision before choosing
a repair. **Decision:** recommend the existing 10% proposal with **moderate confidence**. Its
advantage is local distribution continuity with one endpoint edit. Minimal editing alone was not
enough to distinguish it from 5%; the earlier recommendation needed this comparison. Owner
acceptance of the gameplay amendment is still unrecorded; this research does not adopt it.

### What the wider search established

- [Grognard's September addenda transcription](https://grognard.com/errata1/cna.html), §15.79,
  matches the retained official +4 correction. It supplies no +2 value. Its introductory note
  encourages resolving obvious mistakes, but does not prescribe this particular repair.
- [July 2021 Land rules with integrated errata](https://friendorfoe.com/d/CfNA/CNA%20-%20Rules%20Land.pdf),
  PDF26/§15.79, repeats that +4 correction and refers readers to separate charts. The
  [host attributes the edited PDFs to Clay Stone](https://friendorfoe.com/war/cfna/).
  This is a later transcription, not a newly discovered SPI correction.
- [VASSAL module v1](https://vassalengine.org/wiki_old/wiki/Module:The_Campaign_for_North_Africa:_The_Desert_War_1940-43)
  retains the same +2 gap in its embedded §15.79 chart while correcting +4 to 34–45. Inspected
  `images/15.79 The Close Assault Combat Results Table.png` and `incorporated errata.txt` inside
  the [public module](https://obj.vassalengine.org/images/a/af/CNA.vmod); archive and image hashes
  are retained in the fixture. The module was inspected as data, not executed. Its original chart
  lineage is not independently established; it is evidence of the digital gap, not another original
  printing or endorsement of either repair.
- SPI's preservation indexes, the attributed edited rules, the module and focused public searches
  produced no direct +2 amendment. That bounded search is not proof that none exists; no private
  forum, physical-copy or designer confirmation is claimed. Additional matching copies of the same
  source would not establish intent.

### Compare all four adjacent-band completions

Preserving every defined cell, using the existing neighboring 5%/10% values and keeping loss
non-increasing with ordered rolls gives four possible assignments. For each proposal, all 36
+2 coordinates are covered. Counts below are over uniform ordered-d6 rolls at **fixed +2**.

| Loss at 34/35/36 | Printed endpoints changed | Rolls producing at least 10% loss | Mean chart loss | Mean base TOE loss at strength 10 |
| --- | --- | --- | --- | --- |
| 5/5/5% | 1 | 15/36 | 7.500% | 0.500 |
| 10/5/5% | 2 | 16/36 | 7.639% | 0.528 |
| 10/10/5% | 2 | 17/36 | 7.778% | 0.556 |
| **10/10/10%** | **1** | **18/36** | **7.917%** | **0.583** |

**Structural inference:** the printed +1 and +3 columns produce at least 10% loss on 15/36 and 20/36
rolls; corrected +4 does so on 23/36. The recommended repair yields counts 15→18→20→23. The all 5%
repair yields 15→15→20→23, leaving a plateau then a five-roll jump. Counting ordered outcomes is
appropriate here; interpolating decimal labels such as 33 and 42 would misread the d6 domain.
The two split repairs are plausible, but require editing both adjoining endpoints without a source
specifying a split. These are judgment criteria, not proof that the original table followed a
linear formula. In fact, existing differential 0/roll 23 is 15% while +1/roll 23 is 10%: global
monotonicity already has an exception. Preserve it; do not smooth or rebalance printed values.

**Gameplay sensitivity:** 10% versus 5% changes three of 36 defender rolls at fixed +2 (8.333%).
At the admitted strength 10, each affected roll adds exactly one base TOE loss. Roll 34 also calls
for a one-hex retreat: actual retreat gives 1 versus 0 loss; refusal gives 2 versus 1. Rolls 35/36 do
not require retreat. Capture and Engaged triggers, draw count and already-defined cells stay fixed.
Mean chart loss differs by 5/12 percentage points; mean base TOE loss differs by 1/12 at fixed +2.

Under the current equal-strength, Basic-Morale0/Cohesion0 profile and independent uniform accepted
dice, +2 itself needs attacker Morale 11 and defender Morale 66: probability 1/1296. Thus the repairs
differ on 1/15,552 assaults (about 0.00643%) before conditioning on selected seeds/outcomes. This
small unconditional frequency is specific to this profile, not evidence that the cells may be
ignored or a forecast for broader combat. The conditional effect remains a full TOE point.

### Best path forward and reversal condition

Retain 10% as `CMB-SRC-RUL-001`; once accepted, record it as an explicit Sandtable amendment over the
unchanged scan-derived data. Include its identity and three exact replacements in the future
rules/configuration digest. Preserve existing 357 values, sum flags, capture shares and RNG order.
Do not add a generic gap filler, reroll, seed filter, or default-zero result. TASK-005 tests must
cover all three cells, including roll 34 retreat/refusal; historical rules identities must stay stable.

Confidence is **high** that the retained sources have the gap and that the proposal closes it;
**moderate** that 10% is the best practical repair; **unknown** for the designer's intended endpoint.
A verifiable primary chart/correction specifying these cells would reopen the ruling through an
explicit versioned amendment. Another community transcription alone warrants comparison, not
silent replacement. The search stop condition is met; further unfocused browsing has no identified
source likely to change the decision. Continue to TASK-002 after the amendment is accepted.

## Table coverage and anomalies

**Documented facts:** ordered d6 coordinates are 11…66 with each digit 1…6. Morale 0 has 36 coordinates,
with +1 at 11, −1 at 66 and 0 elsewhere. Five final differentials −2…+2 each need 36 attacker and 36 defender
loss values. Engaged/Retreat/capture use sums of the same ordered pair; capture shares are
10/25/33/50/50/75%. There is no two-hex Retreat in this surface.

**Observations:** all selected bands except defender+2 cover the valid domain exactly once. The
attacker −2/20% band is printed 13–18: preserving that printed endpoint and intersecting with valid
d6 coordinates selects 13–16; 17/18 are invalid inputs, not extra rolls. Defender +2/10% ends 33 while
5% starts 41, leaving 34/35/36 undefined. Both visual and optical inputs expose these facts.

**Candidate-only evidence:** applying the proposed three-cell repair yields 6,480 joint assault
coordinates and 8,840 refusal/capture settlement combinations. Maximum loss remains 3 per side;
all capture amounts are subsets. Morale weighting still yields 44,208 capture paths among 36^4
base draw paths. All 12 earlier seeded research vectors retain their expected losses and flags.
These checks prove numeric closure of a proposal, not its historical correctness or adoption.

## Calendar normalization and retained obligations

**Documented facts:** Land5.1 uses approximately weekly turns with three Operation Stages. Land20.21
requires four Game-Turns of Commonwealth replacement planning; corrected20.72 calls that interval
one month. Land28.24 delays escaped prisoners by one month, then treats them as Replacement Points
for training; it explicitly does not permit immediate unit assignment/attachment. Land20.43 retains
training requirements and20.49 permits absorption only during Reorganization.

**Source-backed normalization inference:** represent the one-game-month delay as four Game-Turns,
or 12 Operation Stages, not civil-date `AddMonths` or 28 wall-clock days. An escape earned in
`(turn, stage)` retains eligibility scope `(turn+4, stage)`. Creation/history scope anchors the
delay; repeating Movement/Combat, changing acting-side order, restoring or finishing a phase cannot
advance it. A due-scope record is not a maturity event or trained-TOE credit.

The exact phase-specific maturity/arrival handler and training remain unsupported future work.
TASK-003 must retain earned scope, duration, due scope and original-unit provenance; the bounded
terminal stops before upkeep/maturity execution. It must not schedule the newly eligible points in
an earlier phase of the due stage or silently map them to the original unit. Future activation must
resolve that phase gate under 28.24/20.21 and preserve the separate training/absorption requirements.

Five arithmetic examples cover early/late stage and turn/year-labelled boundaries: `(1,1)→(5,1)`,
`(1,3)→(5,3)`, `(4,3)→(8,3)`, `(48,2)→(52,2)`, `(49,1)→(53,1)`. These use ordinal game time, not
an invented mapping from synthetic Game-Turn 1 to a historical civil date. Calendar types, limits
and overflow handling remain exact-contract work.

Guarded prisoners retain per-Operation-Stage priority Stores and water obligations under 28.15 and
the Logistics sources linked by DES-005. Escape retains removal/capture history and future
replacement eligibility. Neither branch disappears at Reserve Release, repeat or Truck Convoy entry.

## Ordinary break-off precedence and limits

**Documented facts:** Land8.65 costs 2 CP for Contact;8.66 costs 4 for Engaged. Engaged applies without
ZOC (8.24/8.63). Contact from prior assault adjacency remains a real relation (15.81); absence of
ZOC does not erase it. Reaction's exception is separate (8.52). Last-counterpart departure ends the
remaining relation (8.67); later arrivals do not inherit old membership (8.68).

**Normalization:** charge the highest applicable one-time break-off cost, not 2+4 or one charge per
counterpart; add terrain/hexside cost. Bind the move, CP and affected membership changes atomically.
Preserve unrelated memberships, original participant IDs and all earlier resource/history facts.
TASK-003 freezes those receipts; TASK-018 implements them; TASK-019 uses the same rule for witnesses.
Actual RBA and additional ZOC categories remain outside this task.

**Documented source limit:** ordinary non-motorized units may voluntarily reach 150% of base CPA
(8.17), while CP above CPA adds immediate DP (6.21/6.22). For admitted CPA 10 integer infantry,
spent 6 plus Engaged 4 plus Clear 1 reaches 11 and adds 1 DP; it is not rejected solely for exceeding 10.
Spent 10 plus 5 reaches 15 and adds 5 DP; spent 11 plus 5 exceeds the voluntary ceiling. Released-Reserve
I/II retain their stricter cumulative ceilings 10/5. Mandatory costs remain a separate case.

This corrects a potential misuse of the existing narrower Movement ceiling without changing the
approved pre-assault Cohesion 0 admission. Post-assault movement may alter Cohesion; exhausted Ammo
still prevents a second assault. The verifier's eight positive and three rejection vectors cover
these distinctions, mixed membership, unbound arrivals and released-Reserve ceilings. Topology,
actual movement/replay and final policy-bundle hashing remain future implementation evidence.

## Verification and next gate

Run `python3 docs/research/verify-combat-source-freeze.py`. Expected result: diagnostic checks pass,
**source admission BLOCKED**, exactly three missing coordinates, candidate-only closure, five
calendar vectors and eight positive movement vectors. Negative checks reject incomplete admission,
overlap, invalid dice/differentials, invalid game scopes and voluntary spending above the applicable
ceiling. The same verifier compares all four repairs, neighboring thresholds, rounded losses and exact
profile sensitivity. Existing research oracles remain supplementary rather than production proof.

TASK-001's investigation and reviewable proposal are retained; its complete normalized-table gate
remains open until `CMB-SRC-RUL-001` is decided. No frozen production schema or runtime changed.
Next: owner ruling on the three missing cells, then TASK-002 Content contract freeze with this
manifest and TASK-003's retained calendar/break-off obligations.
