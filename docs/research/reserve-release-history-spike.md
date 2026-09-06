# Reserve Release Eligibility and History

**Status:** Decision-ready `RESREL-RSH-001` research for the independent infantry/cycle boundary;
owner rulings and production contracts remain pending. **Date:** 2026-09-06.

**Decision owner:** Project owner. **Baseline:** `9d233a9`.

## Question and recommendation

Which Reserve I/II release decisions, persistent restrictions, and next-Movement permissions must
the later continual-cycle design preserve?

Retain current Reserve status plus a stage-scoped release record. At the first friendly Reserve
Release, each Reserve I unit must be released or converted to Reserve II. Later Reserve II units
may be released or retained. Release costs no CP, restores no spent CP, and never resets Cohesion,
Breakdown, or attack history. Released units retain their type-specific restrictions even though
their current Reserve status becomes `None`.

This answers a research question, not an instruction to implement Reserve Release. The deliverable
is a logical transition/permission boundary with an executable arithmetic/truth-table probe.
Production action/event names, schemas, migrations, complete combat rules, attached formations,
transport changes, general Barrage/Anti-Armor participation, and second-side activation remain
outside this packet. Decisions below are proposed rather than silently approved.

## Sources and current authority

| Evidence | Locator | Finding |
| --- | --- | --- |
| [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) | PDF 28, §§18.11–18.15 | Friendly designation only; first-release release/flip obligation; Reserve II persists until release or stage end |
| Land rules | PDF 28, §§18.22–18.26 | Reserve movement, release limits, ensuing-Movement exception, no release CP cost |
| Land rules | PDF 14, §§8.23–8.25; PDF 13, §§6.21–6.24 | Continued movement and attack eligibility; existing stage CP/Cohesion effects |
| [Errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf) | §8.23 | Corrected Reserve exception reference; no Section 18 correction found |
| [Reserve Designation spec](../specs/reserve-designation-v1.md) | `RES-REQ-005`, `011`, `015` | Existing designation creates Reserve I; Reserve II/release were deliberately excluded |
| [Cycle decision](continual-cycle-identity-and-history-decision.md) | `CYCLE-DEC-001`, `003`–`005`, `014` | Friendly phase-local cycle identity; mandatory obligations precede repeat/finish; release is this research handoff |
| [Combat mutable state](combat-mutable-state-spike.md) and [RNG research](combat-rng-golden-spike.md) | Selected Cohesion-0 infantry surface | Later release modifiers must not invalidate the admitted Morale row silently |

Primary PDF pages were rendered and visually checked outside Git; only normalized facts are
retained. CCE located current definitions. [CampaignElementState](../../src/Cna.Core/Campaigns/CampaignElementState.cs)
already defines `None`, `ReserveI`, `ReserveII`, reused by successor worlds.
[CampaignElementMovedV3Factory.IsEligible](../../src/Cna.Core/Campaigns/CampaignElementMovedV3Factory.cs)
requires `None` for ordinary Movement. Existing Reserve status vocabulary is not evidence that
release, remaining combat allowances, or the source Reserve I one-hex move is implemented.

## Proposed release boundary

`RESREL-DEC-001`: interpret first/subsequent release relative to the owning side's Movement and
Combat Phase, using the existing `(gameTurn, operationStage, playerPhaseSlot, cycleOrdinal)`.
This reconciles §18.13's each-phase language with §§18.23–18.24 and the accepted cycle identity.
The second acting side's first friendly release is not that side's second release merely because
the opponent already completed a phase. Retain acting side as corroboration, not a replacement
for slot/stage identity. This is an explicit digital/source interpretation for owner approval.

| Current own status | First friendly release | Subsequent friendly release |
| --- | --- | --- |
| `None` | No per-unit obligation | No per-unit obligation |
| `ReserveI` | Choose release to `None` or convert to `ReserveII`; retaining I is illegal | Invalid unresolved history; never silently normalize it |
| `ReserveII` | Invalid for this fresh designated phase under DEC-001 | Choose release to `None` or retain II |

Only units owned by the phasing side receive these choices. Resolve units incrementally with
stable IDs, following Reserve Designation's bounded candidate pattern; no exponential subset list.
Completion is available only after every mandatory first-release obligation is settled. An empty
segment can complete. A subsequent segment can explicitly complete without releasing any II unit.
New designation during Movement/Combat is forbidden. A freshly converted II unit cannot then be
released in the same first segment; §18.24 requires a subsequent segment.

`RESREL-DEC-002`: deterministic unavailable-controller fallback converts unresolved first-release
I units to II in canonical own-unit order and retains II later, then completes the segment. It
never auto-releases or auto-repeats. This is a proposed Sandtable fallback, not a tabletop rule.
Each resolved choice consumes its binding; retries must not duplicate transitions or history.

## Restrictions survive release

**Documented facts:** Reserve I release permits voluntary spending up to the unit's CPA in that
Operation Stage and at most one offensive Close Assault. Reserve II release permits voluntary
spending up to half CPA, rounded down, and at most one offensive Close Assault or Probe. Release
does not itself grant movement or consume ammunition, CP, RNG, or the offensive assault allowance.

`RESREL-DEC-003`: use the existing cumulative stage CP ledger when checking these ceilings.
For unchanged base CPA `C`, I's ceiling is `C`, II's is `floor(C/2)`; a new voluntary action costing
`x` requires `spent + x <= ceiling` and all other legality. Do not replace `spent` by zero or grant
half CPA as a fresh budget on top of it. If prior mandatory spending already exceeds the ceiling,
retain the state and reject further voluntary expenditure; do not reject history or refund CP.
Forced retreat/defense remains governed by its source rules, including immediate CP-overrun DP.
Changes to effective CPA through transport/formation changes require later approved normalization.

Both released types retain a one-offensive-Close-Assault allowance for the stage. Probe is a Close
Assault (§15.91), so it consumes that allowance as well. Count an accepted irreversible offensive
commitment, not a favorable result; zero loss and capture still consume it. A sealed envelope,
rejected action, or defensive assault does not. Releasing again cannot restore the allowance.
Use the same committed-participant evidence as `CYCLE-DEC-008`; do not maintain divergent counters.

**Documented fact:** §18.24(3) adds one Disorganization Point for voluntary combat by a released
Reserve II unit, beyond other DP. For the selected single offensive infantry assault, apply that
DP immediately before its Morale check under §6.22. Its exact stage/participation provenance must
survive snapshot/replay. A retry cannot charge twice. Incidence across multiple Barrage/Anti-Armor
participations is not settled here; those excluded capabilities require a source ruling before
admission, not a guessed once-per-stage flag for every kind of combat.

**Composition consequence:** starting at Cohesion 0, that DP selects Cohesion −1, outside
`CMB-RSH-001`'s admitted row. Releasing II and moving can be designed independently; voluntarily
assaulting with it requires expanded Morale/result normalization or a separately approved fixture
that actually reaches the admitted pre-roll state. Never suppress the DP or force the row back to 0.

## Immediate next-Movement exception and history

**Documented fact:** §18.25 exempts a released unit from §8.23's two-hex proximity condition only
in the immediately subsequent Movement Segment. It does not waive CP, terrain, enemy control,
stacking, Contact/Engaged, Breakdown, or any combat rule.

`RESREL-DEC-004`: bind this exception to the next cycle's Movement occurrence in the same friendly
phase. It remains usable throughout that one segment, not merely its first move, and expires when
the segment closes even if the unit did not move. Finishing the phase without repeating expires it;
later cycles, another acting-side slot, and another stage cannot revive it.

Release completion does not itself authorize a repeat: the accepted cycle decision still requires
current legal Movement/Combat continuation and material progress. If every unit remains II and no
other continuation exists, that bounded cycle policy finishes; it cannot invent empty cycles just
to reach another release. A broader deliberate-wait policy would require an owner-approved change
to `CYCLE-RSH-001`, not an implicit exception introduced by release research.

Reserve I still has a separate source permission to move one hex while in Reserve, without entering
an enemy-controlled hex (§18.22); this does not make the movement free. Reserve II cannot move while
it remains II. Existing ordinary Movement rejects both statuses. Later design must explicitly add
and track the I one-hex permission, or keep a declared unsupported pre-release path; changing status
alone cannot claim source-complete Reserve lifecycle coverage.

Retain, logically, stable unit ID; designation/release type; stage and friendly phase; release-cycle
identity; one-segment exception scope/expiry; offensive commitment occurrence; and II combat-DP
provenance. Snapshot retains the active restrictions and unresolved obligations; Chronicle retains
their full history. Stage end clears Reserve II and stage-only restrictions, while preserving
Chronicle and ordinary Cohesion persistence. Phase finish alone does not clear stage restrictions.

Owner observation and action sets may show own status and derived remaining permission. Opponent
bytes must not reveal hidden releases through action count, revision, history digest, or fallback
reason. Bind current choices to audience-safe freshness; authoritative replay verifies the full
scope and pre-state. Joining the next Movement must preserve accumulated CP, BP, checked bands,
current TOE, ammunition, and unresolved Combat obligations.

## Evidence and design handoff

Run `python3 docs/research/verify-reserve-release.py` from repository root (Python standard library).
It checks the finite first/later release choices, exhausted and fractional cumulative CP examples,
one-offensive-use rule, and exact next-segment exception scope. It is a logical research probe,
not runtime action generation, full history validation, fog testing, or a source table artifact.
Observed: seven release cases, eight CP vectors, seven scope vectors, and four offensive-use cases
pass. Six temporary incorrect variants were caught: early II release, refreshed CP budget, rounded-up
half CPA, extended exception duration, ignored phase/stage scope, and a second offensive use.

| Acceptance case | Required later evidence |
| --- | --- |
| First release with I units | Every unit releases or converts before completion; converted II cannot release immediately |
| Later II release at CPA 9 with 3 CP spent | Ceiling 4, only 1 voluntary CP remains; zero-cost release preserves ledger |
| Already spent above ceiling | State/history retained; further voluntary expenditure rejected; no CP refund |
| First offensive commitment and retry | Allowance consumed once, II DP once, current Morale row revalidated; defensive participation does not consume offensive allowance |
| Skip next Movement or finish | Exception expires; later replay/snapshot cannot renew it |
| Fog-equivalent opposing Reserve histories | Equal outward bindings/action bytes and admission outcomes |
| End Operation Stage | II/current stage restrictions clear through explicit authority; Cohesion and Chronicle persist |

Reject a status-only release implementation: it loses ceilings and consumed allowances. Defer a
general Reserve/transport/combat engine: it crosses the selected capability boundary. Recommend
the logical history and scope model above, with DEC-001–004 submitted for owner approval.

Confidence is high in the cited first/later choices, ceilings, and immediate-segment exception.
Multi-participant DP incidence, effective-CPA changes, full Reserve I movement activation, and
second-side lifecycle remain explicit later gates. `CMB-DES-001`–`005` and `CYCLE-DES-001` must
compose these requirements into one reviewed implementation plan before production contracts freeze.
