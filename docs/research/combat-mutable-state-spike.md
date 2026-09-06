# Combat Mutable State Spike

**Status:** Decision-ready `CMB-RSH-003` research; recommendations await project-owner approval.
No production contract or implementation is authorized by this packet.

**Date:** 2026-09-06

**Decision owner:** Project owner

**Baseline:** `5cc55769224c0b73f27dd7bc41f8ea500371fc9e` (merged PR #88)

**Inputs:** [result surface](combat-rules-result-surface-spike.md),
[static Content research](combat-content-static-schema-spike.md), and
[parent inventory](combat-cycle-source-inventory.md).

## Question and recommendation

Which mutable facts and mandatory consequences close every outcome of the selected infantry Close
Assault, without duplicating current authority or silently dropping prisoners, retreat, or supply?

Recommend reusing current component TOE and the operational CP/Cohesion ledger, adding logical
ammunition balances, loss allocations, prisoner custody, and explicit pending combat obligations.
An integer prisoner count or a generic `combatComplete` flag is insufficient. A rolled retreat is
not yet a completed retreat; capture removes a subset of the loss, not additional strength.

This is a research boundary, not a choice of classes, schema versions, event names, or migrations.
The first implementation may checkpoint with replayable pending consequences, but cannot declare
Combat complete or advance past them. Before admitting an assault, its approved design must cover
every reachable result and immediate consequence. Choosing a favorable seed is never admission.

Success means a source-backed ownership map, arithmetic evidence, and explicit design obligations.
Stop at this packet and verifier. Full Logistics, historical content, Combat implementation, RNG
ordering, and production schema approval are outside this task.

## Method and sources

Primary scans were rendered and visually checked outside Git. Errata supersedes base rules.
Only normalized facts and synthetic arithmetic are retained; no scans, published unit rows, or
chart matrices are copied. PDF page numbers below are one-based.

| Source | Locator | Evidence used |
| --- | --- | --- |
| [Land Game rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) | §§6.21–6.26, PDF 13; §8.12, PDF 13–14 | CP, immediate Cohesion changes, loss Disorganization, victory recovery, retreat movement |
| Land Game rules | §§15.73–15.88, PDF 24–25 | Rounding, capture, retreat refusal, Engaged precedence, surrender |
| Land Game rules | §§28.11–28.24, PDF 40 | Prisoner identity, relocation, guards, escape, feeding |
| [Complete rules compilation](https://www.spigames.net/PDFv2/CampaignNorthAfrica.pdf) | Air/Logistics §§50.0–50.17, printed 20–21 / PDF 67–68 | Carried ammunition, consumption, accessible stock, immediate debit |
| Complete rules compilation | §§51.12, 52.51–52.53, printed 21–22 / PDF 68–69 | Prisoner upkeep and water-dependent infantry readiness |
| [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf) | §§15.88, 28.17, 50.12, 50.2 | Surrender trigger, corrected moving guard capacity, infantry ammunition rate |

**Observation:** CCE located current Content and Campaign definitions; direct inspection of those
definitions confirmed the following baseline. Older research absence statements describe their
earlier baseline, not today's implementation.

| Existing authority | Reuse / remaining gap |
| --- | --- |
| `ContentCombatComponent` in [ContentPackV5Models.cs](../../src/Cna.Core/Content/ContentPackV5Models.cs), also used by Content 6 | Stable component/class IDs, maximum TOE, defensive Close Assault rating and origin already exist. Offensive rating and Basic Morale remain static-research inputs. |
| `CampaignComponentToeState` in [CampaignWorldV5.cs](../../src/Cna.Core/Campaigns/CampaignWorldV5.cs), reused by World 6 | Current TOE and initial provenance already exist; retain them rather than adding another strength balance. |
| Component validation in [CampaignWorldV6.cs](../../src/Cna.Core/Campaigns/CampaignWorldV6.cs) | Current TOE joins Content IDs and is bounded by maximum; creation separately checks the seed. A loss must not restore seed or maximum TOE. |
| Operational state in CampaignWorldV5.cs | Exact CP, stage identity, Cohesion, and Breakdown state already exist. Combat needs source-specific changes through that authority. |

Adoption of those ZOC foundations does not imply approval of all proposals in `CMB-RSH-002`.
This packet does not claim ammunition, prisoner custody, or combat settlement already work.

## Bounded input and result closure

Retain `CMB-RSH-001`'s two independent, adjacent, non-Reserve infantry battalions in Clear terrain:
one component and 10 current/maximum TOE each, ratings 1/1, Basic Morale 0, Cohesion 0, CPA 10.
Both commit all TOE; defender declines Retreat Before Assault. No armor, guns, trucks, air,
fortifications, attachments, pinning, or other special state. Barrage and Anti-Armor positions
remain structurally present with no eligible participants.

**Proposed admission additions:** current water/stores readiness must support the unmodified
infantry calculation; CP remaining must preserve the selected pre-roll Cohesion; each side has
exactly 10 carried Ammo Points at the assault-use checkpoint. The fixture starts after required
supply distribution, with origin-bearing readiness facts. These are synthetic input conditions,
not an implicit implementation of prior Logistics phases or a perpetual `supplied` boolean.

**Documented fact:** the prior result-surface spike reaches five final differentials, −2 through +2;
attacker losses reach 25%, defender losses reach 25%, capture can affect either side, and defender
retreat can require one hex. Engaged and zero-loss retreat also remain possible. The favorable
one-hex-retreat golden is only one evidence vector.

**Inference:** admission must support the entire selected surface, including infantry prisoners
and 30% actual TOE loss. Excluding guns/tanks removes captured-equipment cases structurally;
excluding capture or Disorganization would discard reachable infantry outcomes.

## Logical mutable state and settlement obligations

| State / obligation | Minimum retained facts and invariant |
| --- | --- |
| Current strength | Existing element/component IDs and current TOE; before, committed, lost, and after values in authoritative settlement evidence. Never mutate immutable maximum TOE. |
| Loss allocation | Assault identity, side/component, pre-loss raw strength, table percentage, refusal increment, rounding rule, total TOE lost, captured subset, and other loss. `before = after + captured + other loss`. |
| Ammunition | Owner, component or accessible stock identity, location, exact Ammo Points, and source/use provenance. Carrying capacity and accessible stock are different facts. No second-line convoy stock is usable before unloading. |
| CP and Cohesion | Existing stage ledger plus reasoned combat/retreat CP expenditure, loss DP, and victory RP. DP lowers Cohesion immediately; RP raises it up to +10. Do not introduce an independent Disorganization counter as a second truth. |
| Retreat | Required distance, chosen/actual route, completed distance, unfulfilled distance, and pending/resolved status. Map legality, enemy influence, CP, and any Breakdown consequences remain part of the obligation. |
| Prisoners | Original side, captor, source component/loss allocation, Prisoner Points, location, custody/guard allocation, and pending relocation or escape. Capture evidence remains after escape; current custody changes. |
| Contact / Engaged | Preserve source result separately from final relationship state; derive changes only after position and surviving-strength consequences are known. Bind to current ZOC/Reaction episodes through later design. |
| Settlement identity | Bind opportunity, cycle, participants, frozen state/rules/config, chosen inputs, and pending obligations. Repeat/replay cannot charge ammunition, losses, CP, guards, DP, or RP twice. |

These are logical facts, not a mandate to store every derivable number in World. Chronicle may
retain calculations; snapshots must retain enough unresolved state to resume without rerolling or
reapplying effects. Sealed inputs and exact enemy quantities stay in Umpire authority. Observation,
legal actions, and side-visible Chronicle need explicit disclosure projections in `CMB-DES-005`.

### Loss, capture, and retreat arithmetic

**Documented facts:** §§15.82–15.87 apply the percentage to pre-loss raw assault strength. Attacker
rounds up; defender rounds down. Each required retreat hex not taken adds ten percentage points
before rounding. Capture share rounds up from the resulting loss and is part of that loss.
For the selected infantry rating of 1, raw points lost and TOE lost coincide.

| Synthetic case | Expected result |
| --- | --- |
| Attacker: 10 TOE, 25% loss | 3 lost, 7 remain; actual loss is 30%, therefore 3 DP |
| Defender: 10 TOE, 25% loss, retreat fulfilled | 2 lost, 8 remain; no loss DP |
| Defender: 15% table loss, one unfulfilled retreat hex | Floor of 25% × 10 = 2 |
| Defender: 20% table loss, one unfulfilled retreat hex | Floor of 30% × 10 = 3; 7 remain and 3 DP |
| Either side: 3 lost, capture share 33% | 1 Prisoner Point, 2 other losses, 7 remain; never deduct capture again |
| Zero loss with a retreat / Engaged result | Preserve the positional / relationship consequence; Retreat takes precedence over Engaged |

The 10-TOE envelope cannot distinguish combined rounding from rounding refusal separately: each
refusal increment is exactly one raw point. An additional arithmetic probe uses 9 committed TOE,
15% table loss and one unfulfilled hex: combined rounding gives 2 losses; separate rounding gives
1. This tests the source rule for a later reduced-strength case, not admission of that case into
the selected Morale/chart surface.

**Documented fact:** §15.87 tests actual TOE lost against committed TOE, not merely the printed
loss percentage. §6.24(2) awards 3 recovery points to victorious participating units when Close
Assault causes the defenders to vacate their hex completely. A rolled or refused retreat does not
establish that completed condition.

**Inference:** retain loss DP and victory RP as separate reasoned effects. A side can incur 3 DP
and later earn 3 RP; a net zero change must not erase either cause. The exact ordering with retreat
CP, guard formation, and Contact changes is a design/RNG handoff, not silently fixed by this spike.

### Full-game ammunition and readiness

**Documented fact:** §§50.14–50.15 charge TOE used at the instant it is used, including defensive
assault, from ammunition accessible in the hex. Errata §50.2 sets infantry consumption to one
Ammo Point per TOE point used. Thus each selected battalion spends 10 points, leaving zero from
its one-use carried load. Charge committed use, not surviving TOE or final Actual Assault Points.

§50.0 permits each TOE point to carry enough ammunition to fire once. More accessible ammunition
may be in a dump or first-line trucks; the selected fixture includes neither. A later repeat needs
an explicit resupply/readiness path or must not admit another supplied assault. Do not cancel an
already-paid use because its debit leaves zero; later uses must recheck the resulting balance.

Errata §50.12 requires assault for an ammunitionless unit to surrender: adjacency or Barrage alone
is insufficient. §15.88 separately distinguishes assault surrender at low Cohesion from the
adjacency threshold. These conditions must not be conflated with a harmless empty Combat step.

Land §32 and Air/Logistics §47 describe an abstract variant. Its per-battalion attacker/defender
ammunition costs cannot stand in for §50. The present recommendation uses full-game units.
Water deprivation also changes infantry assault/readiness (§52.52); zero water cannot be treated
as the selected normal-strength input. Fuel and vehicle Breakdown have no consuming participant
in this fixture, but remain explicit future capability boundaries.

### Prisoners require custody, not just counting

**Documented facts:** infantry capture yields one Prisoner Point per captured TOE (§28.11).
The captor may relocate prisoners up to three hexes immediately, subject to map/enemy restrictions
(§28.12). Guards are required; moving custody uses one guard per five Prisoner Points (errata
§28.17 corrects the printed rule), and a guard
can be formed by removing one infantry TOE (§§28.17, 28.22). Unguarded or excess prisoners can
escape immediately (§28.23). Prisoner feeding has priority at the later supply obligation
(§28.15; Logistics §51.12).

**Recommendation:** represent the capture first as attributable loss plus a custody obligation.
The approved design must then settle legal relocation, guard allocation (including its distinct
TOE transfer), or source-required escape. Guard creation is not another combat casualty. No
mandatory custody work may be deferred behind a completed-Combat flag or silently replaced by
dead TOE. Capture can occur to either side; defender capture of attackers needs the same support.

For the selected arithmetic envelope, at most three Prisoner Points arise in one assault. Existing
custody is excluded from the fixture. That bounds quantity, not the legal guard/escape choices.
The general moving-custody capacity boundary is five prisoners needing one guard, six needing two;
the verifier retains that separate source arithmetic probe without admitting a six-prisoner fixture.
Later convoy movement, feeding, camps, and departure are not implemented by this research; campaign
advance must stop before an unsupported obligation. A full repeating skeleton must close those
gates, not merely save a pending record forever.

## Options and proposed decision

| Option | Fidelity / consequence | Assessment |
| --- | --- | --- |
| Apply TOE losses only; ignore capture, retreat refusal, ammo, and recovery | Loses reachable source outcomes and permits unsupported repeat | Reject |
| Import all Logistics and prisoner transport now | Broad campaign scope before Combat identities and contracts are designed | Defer; unnecessary for this research checkpoint |
| Reuse TOE/ledger; retain ammo, attributable losses, custody, and explicit pending obligations | Makes all reachable obligations visible and testable; still requires settlement and phase gates | Recommend for owner approval |

Proposed decisions `CMB-RSH-003-D1`–`D3` are **not yet approved**:

1. Reuse current TOE/CP/Cohesion authority; add no duplicate maximum/current strength or DP truth.
2. Use full-game infantry ammunition and loss/capture/retreat rules, including immediate custody
   and completed-retreat recovery. Do not narrow the result surface by seed.
3. Permit an explicit resumable intermediate checkpoint; gate Combat completion and later phase
   advance on supported, settled mandatory consequences. Keep production representations open.

Confidence is high in the cited arithmetic and ownership distinction. Remaining uncertainty is
integration order, legal retreat/custody choices and fallback, observation disclosure, and the
production stock/readiness representation. Discovery of another reachable immediate effect, an
applicable erratum, or inability to settle all custody branches would require revising admission
and design scope before implementation.

## Reproducible evidence and next gates

Run from repository root with Python 3 (standard library only):

```sh
python3 docs/research/verify-combat-mutable-state.py
```

The verifier checks independently specified boundary examples and 108 conditional arithmetic
combinations, including capture shares 0/10/25/33/50/75 and refusal additions 0/10. This is a
conservative arithmetic envelope, **not** a claim that every cross-product is a reachable joint
chart coordinate. It has no source chart, RNG implementation, or runtime integration. It cannot
prove replay, custody legality, privacy, or full Combat correctness.

The subsequent [RNG/golden spike](combat-rng-golden-spike.md) checks actual chart correlations.
Its seed 208 demonstrates the reachable defender maximum of three TOE and the loss-DP case.
Independent review corrected an inherited two-hex `+2` claim: two-hex results start at `+3`, outside
the selected surface. This envelope now permits at most one refused hex; combinations still do
not imply joint chart reachability.

**Observed validation:** Python 3.14.6; eight loss boundary vectors, three guard-capacity probes,
and all 108 combinations pass.
Temporary in-memory mutations of attacker rounding, capture double deduction, loss-DP threshold,
and separate refusal rounding must each fail the boundary evidence. Source scans and mutation
copies remain outside the change; no .NET behavior changed, so no runtime test claim is made.

| Handoff | Required acceptance evidence |
| --- | --- |
| `CMB-RSH-004` (next bounded research) | Exact draws and conditional capture draw, complete selected coordinate coverage, independent golden calculations including refusal, capture, loss DP, and completed-retreat RP; preserve table correlations. |
| `CMB-DES-001`–`003` | Opportunity/participant joins, state-bound sealed choices, admission/readiness, source-correct CP timing, and deterministic fallback. |
| `CMB-DES-004`–`005` | Before-state strength/use, exact rounding and conservation, custody/guard/escape and retreat ordering, Contact/Engaged reconciliation, replay/snapshot idempotence, and negative disclosure evidence. |
| `RESREL-RSH-001` and `CYCLE-DES-001` | Legal release/repeat/finish with no pending mandatory consequences; exhausted ammo and later prisoner upkeep cannot disappear during advance. |

Research can proceed to `CMB-RSH-004`; production contracts still require approved choices and a
reviewed design/task plan. This packet does not turn research arithmetic into simulator coverage.
