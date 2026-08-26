# Breakdown Continuity Spike

**Status:** Decision-ready; owner rulings required before `MOV-TASK-005`

**Date:** 2026-08-25

**Decision owner:** Project owner

**Research work item:** `BREAKDOWN-001`

## Decision question

Before the first outward Movement contracts and accepted histories are frozen, should Sandtable
record the minimum Breakdown continuity data during Movement while still stopping before Breakdown
adjudication, or deliberately require a later clean-cut migration and treat early Movement histories
as terminal at Breakdown?

## Executive conclusion

Record continuity now. Breakdown is the immediate successor to every Movement Segment, its points
accumulate across relevant Movement/Reaction/Retreat activity during an Operation Stage, and the
current CP/Cohesion-only world cannot independently continue from a snapshot into an authentic
Breakdown check. Adding the accounting seam before observation/action/event contracts minimizes
version churn while preserving the explicit non-adjudicating Movement boundary.

This recommendation does not authorize Breakdown rolls or broken-vehicle mutation. Two owner
rulings must close first:

| Decision | Recommendation | Status |
| --- | --- | --- |
| `BRK-DEC-001` | Use two independent d6 read sequentially as the 11-66 table coordinate; treat the general “add” wording as an error because the specific procedure and table otherwise cannot agree | Proposed; owner approval required |
| `BRK-DEC-002` | Add minimum BP continuity before `MOV-TASK-005`; do not create terminal-at-Breakdown Movement histories | Proposed; owner approval required |

## Source index

| Source | Locator | Use |
| --- | --- | --- |
| [1979 Land Game rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) | 5.2; 21.11-21.45 | Breakdown timing, subjects, cumulative BP, checks, losses, and persistence |
| Map A Terrain Effects Chart | 8.37 | Supported terrain/route/hexside BP inputs |
| Common charts | Table 21.38 | BP bands and sequential-dice percentage results |
| [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf) | 8.37; 21.12 | Corrected Track derivation and BAR example; no dice-language correction |

The bounded source vectors are Clear `4 BP`, Desert `24 BP`, Road `1/2 BP`, Track halving the
applicable underlying value, and `+2 BP` for Ridge/up-slope/down-slope. These are normalized data,
not permission to reproduce the source charts.

## Evidence

**Documented fact:** BP applies to specific vehicle-point classes, uses exact terrain/route/hexside
inputs, persists across both players' relevant activity in the Operation Stage, and suppresses a
repeat check until the cohort reaches a higher applicable column. Reaction and Retreat contribute.

**Repository observation:** The merged world records stage identity, exact CP expenditure, and
Cohesion. Content mobility cannot distinguish breakdown-relevant vehicle cohorts or BAR. The
planned Movement event has no BP components or prior checked-column state.

**Inference:** Recalculating from Chronicle would weaken snapshots as replay checkpoints, and
inferring BP from CP/mobility is source-inaccurate. Either continuity must be versioned before the
first move, or future Breakdown must reject all earlier Movement histories explicitly.

## Options

| Option | Benefit | Cost/risk | Decision |
| --- | --- | --- | --- |
| Record continuity now | Future Breakdown can continue from snapshots; one coordinated identity migration | Adds a bounded rules/content/world lane before outward Movement | Recommended |
| Later clean cut | Smaller immediate slice | Early histories are terminal; observation/action/event/world identities churn again | Viable only by explicit owner choice |
| Recalculate from Chronicle | Avoids snapshot fields | Snapshot is not self-sufficient and current events lack stable BP identity | Rejected |
| Infer from CP/mobility | Minimal fields | Incorrect for terrain/routes and vehicle classes | Rejected |

## Minimum continuity boundary

- Rules identity: exact rational BP, supported terrain/route/hexside rows, bands, BAR/weather shifts,
  and `BRK-DEC-001` if approved.
- Content identity: at most one supported synthetic vehicle cohort per admitted motorized element,
  with stable cohort/type, working point count, and BAR-resolving rules ID; no cohort for admitted
  non-motorized elements.
- World/snapshot: stage key, exact cumulative BP, highest effective checked column or `none`, and
  working/broken counts needed for later continuation. Broken-location consequences remain later.
- Observation: approved own cohort/BP risk facts only; no opposing cohort, BAR, total, check history,
  or broken truth.
- Movement action/event: side-safe BP delta explanation and authoritative before/after components;
  the last-checked column remains unchanged; no dice, loss, or RNG mutation.
- Completion: reaches Breakdown and exposes no Breakdown action.

## Conditional task graph

If `BRK-DEC-002` is approved:

```text
MOV-TASK-004 complete
        |
BRK-RSH-001 source/ruling lock
        |
MOV-TASK-004B BP rules + Content cohort + world continuity
        |
MOV-TASK-005 observation
        |
MOV-TASK-006 dormant action contracts
        |
MOV-TASK-007 CP + BP ElementMoved; no Breakdown RNG
```

If continuity is rejected, the original Task 005-007 order may resume only after the Movement spec
states that Breakdown snapshots are terminal and a future coordinated rules/content/world/event
migration is mandatory.

## Smallest acceptance vectors

1. Golden supported BP rows and corrected Track derivation retain exact locators.
2. A non-motorized/no-cohort mover changes no BP state.
3. Two Road entries accumulate exact BP `1` without enabling a check.
4. One Clear entry accumulates BP `4` and reaches the first eligible band.
5. Re-stopping in the same checked band does not enable a second roll; entering a higher band does.
6. Dice `3` and `3` produce coordinate `33`, never arithmetic total `6`, if `BRK-DEC-001` is approved.
7. BAR/weather shifts, upward rounding, replay equality, privacy absence, and forged-component
   rejection each have deterministic positive/negative vectors.

## Limitations and next gate

This packet does not normalize every vehicle class, weather case, loss-placement exception, or
Breakdown result. After the owner rulings, planning must either insert and independently review the
bounded continuity lane or explicitly document terminal histories. Breakdown adjudication remains a
separate later package.
