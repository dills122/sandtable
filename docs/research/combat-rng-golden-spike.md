# Combat RNG Order and Golden Vectors

**Status:** Decision-ready `CMB-RSH-004` research. Proposed order awaits project-owner approval;
production contracts and complete typed Combat tables remain design-gated.

**Date:** 2026-09-06. **Decision owner:** Project owner.

**Baseline:** `b8079e2` on `codex/cmb-rsh-003-mutable-state`, following merged PR #88.

**Inputs:** [result surface](combat-rules-result-surface-spike.md),
[static Content](combat-content-static-schema-spike.md), and
[mutable state](combat-mutable-state-spike.md). Their proposed choices remain proposals.

## Question, scope, and conclusion

Which exact random procedure preserves source dice dependencies and current deterministic-stream
compatibility for the selected infantry assault, with reproducible golden evidence?

Recommend eight accepted d6 in role/purpose order, followed by one conditional capture die.
Continue the existing campaign byte cursor; never reseed at Combat, use side submission order, or
reserve an unused capture draw. Empty Barrage/Anti-Armor positions consume nothing. Capture,
retreat, and Engaged reuse the assault pair; they do not receive independent result pairs.

The stop condition is this research packet and executable oracle. This task does not implement
Combat, approve a schema version, transcribe the complete loss matrix into a rules artifact, or
prove runtime replay. Success requires exact purposes/counts, source-backed correlations,
independent seeded byte evidence, and explicit later acceptance gates.

## Evidence and source precedence

**Documented facts:** primary Land rules §§15.61, 15.71–15.76 require Morale before assault results,
one distinguishable assault pair per side, ordered reading for percentage losses, reuse of its sum
for other tests, and one additional die for capture. Errata §15.27 reinforces this reuse.
The first die represents the source's physically larger die, not the numerically larger result.

| Source | Locators inspected | Use |
| --- | --- | --- |
| [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) | §§15.61–15.87, PDF 24–25; §§6.21–6.24, PDF 13 | Dice dependencies, rounding, retreat refusal, DP/RP |
| [Common charts](https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf) | §15.89 PDF 3; §15.79 PDF 4; §17.4 PDF 5 | Capture die, selected assault coordinates and sum flags, Cohesion-0 Morale |
| [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf) | §§15.27, 15.79 | Three uses of the pair; corrected +4 coordinate remains outside this surface |

Scans were visually inspected outside Git, including a rotated rendering of the assault chart.
Only normalized facts, synthetic cases, and aggregate coverage are retained; no scan or complete
loss matrix is included. The cross-side serialization below is a Sandtable recommendation, not a
claim that the tabletop source specifies a software stream order.

**Repository observation through CCE:** [SandtableRandom](../../src/Cna.Core/Randomness/SandtableRandom.cs)
implements `sandtable.sha256-counter.v1`; [RandomStreamState](../../src/Cna.Core/Randomness/RandomStreamState.cs)
retains version, algorithm, seed, and next byte cursor. The existing
[random procedure artifact](../../src/Cna.Core/Randomness/Cna1979RandomProcedure.cs) establishes
purpose order and hash sensitivity; this packet does not mutate it or any Breakdown RNG procedure.
[Core goldens](../../tests/Cna.Core.Tests/Randomness/RandomStreamTests.cs) independently anchor
digest blocks, rejection, and block crossing.

Each block hashes 36 bytes: ASCII `sandtable.random.v1`, a zero byte, unsigned-64 big-endian seed,
and unsigned-64 big-endian block index. Cursor divided by 32 selects the block; remainder selects
the byte. Accept bytes below 252 and map them with `byte % 6 + 1`. Rejected bytes advance the cursor
but create no accepted die. Cursor overflow fails before consuming a byte.

## Proposed procedure: CMB-RSH-004-D1

For one admitted assault with the `CMB-RSH-001` inputs and both sealed choices resolved:

| Accepted-die positions | Purpose labels | Interpretation |
| --- | --- | --- |
| 1–2 | `attacker.morale.tens`, `attacker.morale.ones` | Attacker Cohesion-0 Morale coordinate |
| 3–4 | `defender.morale.tens`, `defender.morale.ones` | Defender Cohesion-0 Morale coordinate |
| 5–6 | `attacker.assault.tens`, `attacker.assault.ones` | Attacker ordered percentage coordinate and sum |
| 7–8 | `defender.assault.tens`, `defender.assault.ones` | Defender ordered percentage coordinate and sum |
| 9, only when triggered | `attacker.capture.share` or `defender.capture.share` | Captured share of that side's loss |

These labels identify draw purposes in one campaign stream, not separately seeded streams.
Attacker/defender are assault roles, not fixed Axis/Commonwealth order. Labels bind to the retained
opportunity, participants, cycle, base-state/rules/config hashes, and before/after cursor.

Derive final differential after both Morale pairs, then resolve both assault pairs against that
same differential and pre-loss inputs. Only then draw any capture share. In this selected surface,
attacker capture can occur only at −2, defender capture only at 0/+1/+2; both cannot trigger in one
assault. No tie rerolls occur. Eight or nine accepted dice can consume more than eight or nine bytes.
Any broader surface needs its own approved ordering, including the possibility of two captures.

Source-defined CP/Cohesion changes must occur before the relevant Morale check. If those changes
leave the selected Cohesion-0 surface, admission/design must handle that before drawing. This
procedure must not overwrite a changed Cohesion value to force the desired row.

## Golden evidence: CMB-RSH-004-D2

All vectors use 10 committed TOE per side, ratings 1/1, Basic Morale 0, and Cohesion 0 at the roll.
Seed/cursor identify the **assault-start stream state**, not a complete campaign seed replay.
These cases do not assert that Initiative/Weather/Breakdown naturally stop at those cursors.
Pairs below are attacker Morale / defender Morale / attacker assault / defender assault.

| Seed / start cursor | Pairs; extra die | Differential | Source-backed result / boundary |
| --- | --- | ---: | --- |
| 0 / 0 | 66 / 24 / 12 / 23 | −1 | 2 attacker TOE lost, 1 defender; end cursor 8 |
| 7 / 0 | 54 / 56 / 56 / 26 | 0 | Attacker sum 11 is **not** Engaged in this column; one rejected byte; end 9 |
| 15 / 0 | 53 / 66 / 15 / 22 | +1 | Losses 2/1; one-hex retreat, fulfilled in this case |
| 18 / 0 | 65 / 52 / 55 / 32 | 0 | Required favorable vector: losses 0/1, one-hex retreat; raw Engaged also true |
| 26 / 0 | 36 / 54 / 11 / 66 | 0 | Attacker 25% rounds to 3 TOE: 3 loss DP; defender loses zero |
| 47 / 0 | 22 / 35 / 14 / 11; 2 | 0 | Losses 2/2; one defender Prisoner Point from 25% capture share; end 9 |
| 208 / 0 | 21 / 66 / 55 / 13 | +1 | Defender 20% plus one refused retreat hex becomes 3 TOE lost and 3 DP; no victory RP |
| 1296 / 0 | 11 / 66 / 56 / 65 | +2 | Zero losses, no retreat, Engaged and no victory RP; rejected byte before defender pair; end 9 |
| 4983 / 0 | 66 / 11 / 35 / 45 | −2 | Attacker loses 1; zero defender loss still requires one-hex retreat |
| 31707 / 0 | 66 / 11 / 11 / 63; 5 | −2 | Attacker loses 3, including 2 captured; defender retreats one hex; attacker loss DP and later victory RP both 3 |
| 0 / 30 | 66 / 44 / 32 / 22 | −1 | Crosses SHA block boundary without resetting; end cursor 38 |
| 0 / 129 | 21 / 63 / 61 / 33 | 0 | First candidate byte rejected; rounded losses 0/0 still require retreat; end cursor 138 |

All required retreats except seed 208 are supplied as fully completed choices for the arithmetic
oracle. This demonstrates the consequent 3 RP, not route legality or a final World transition.
Retreat/custody settlement, guard formation, CP effects of paths, and final Contact/Engaged
projection remain with `CMB-DES-005`. Keep raw Engaged and retreat evidence until settlement;
the RNG packet must not turn a roll into proof of completed movement.

Independent review corrected an inherited source-column error: two-hex Retreat first appears at
`+3`, outside this surface. Seed 1296 therefore stays Engaged without retreat or victory RP.
`CMB-RSH-003` now limits its conservative arithmetic envelope to one refused hex. The actual
defender maximum after refusal is three, shown by seed 208, so defender loss DP remains required.

## Coverage and its limits

The verifier enumerates all 36 ordered pairs per Morale check and all 1,296 combinations of both
Morale pairs. Differential multiplicities are `−2:1, −1:68, 0:1158, +1:68, +2:1`.
It enumerates all 6,480 joint assault-coordinate triples: five differentials × 36 attacker pairs
× 36 defender pairs. Their projections cover all 180 coordinates on each side.

Weighting by Morale gives 1,679,616 base eight-die paths, of which 44,208 trigger one capture die.
Expanding those six possible extra values gives 1,900,656 leaves. These counts describe dice-domain
coverage and the conditional draw rule, not empirical seed frequencies or complete loss-table
validation. In particular, eight- and nine-die leaves do not have equal probability.

Loss percentages in the twelve retained cases were manually checked against the source; the helper
uses those fixed sampled facts as inputs and independently checks their resulting arithmetic.
It is deliberately not an executable loss lookup for other coordinates. Full typed-table coverage,
independent transcription, and canonical hash admission remain required before Combat runtime.

## Options, validation, and next gate

| Option | Assessment |
| --- | --- |
| New Combat seed/substreams | Changes existing stream continuity and requires a new seed-derivation contract; no demonstrated need |
| Always consume a ninth die | Makes capture-free continuation wrong under the proposed source-dependent procedure |
| Continue campaign stream; role-ordered pairs and conditional extra die | Recommended: preserves existing RNG algorithm, source dependencies, and explicit replay evidence |

Run `python3 docs/research/verify-combat-rng.py` from repository root. Python 3.14.6 passes all twelve
seeded vectors, per-die cursor continuation, existing Core digest anchors, and the domain/count
checks above. OpenSSL independently matched thirteen digest blocks used by the probes. Six
temporary mutations fail as expected: accepting rejected bytes, omitting capture, swapping Morale
roles, changing seed endianness, sorting assault dice, and changing a purpose label.
No .NET runtime behavior changes; these checks do not claim current Combat simulator support.

Confidence is high in the checked dice semantics and seeded bytes. Owner approval of D1/D2,
complete production table entry, event/snapshot boundaries, and route/custody settlement remain
open. Duplicate/stale/rejected commands and pending user choices must consume no new RNG; replay
must verify bindings, dice, conditional labels, and byte cursors without rerolling. Future design
must publish effects and cursor together, preserve old artifact hashes, and redact enemy rolls,
exact quantities, seed, and cursor from side-visible output as required by its disclosure policy.

Next research is `RESREL-RSH-001`: Reserve release eligibility/history and movement handoff.
Then reconcile these proposals in `CMB-DES-001`–`005` and `CYCLE-DES-001`, and independently review
the resulting implementation plan before freezing production contracts.
