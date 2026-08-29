# Breakdown Continuity Spike

**Status:** Owner-approved, source-locked, implemented, and independently reviewed in `MOV-TASK-004B`

**Date:** 2026-08-29

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

This decision does not authorize Breakdown rolls or broken-vehicle mutation. The owner closed the
original two rulings on 2026-08-29. Independent source verification then exposed one additional
conflict that must close before versioned implementation:

| Decision | Recommendation | Status |
| --- | --- | --- |
| `BRK-DEC-001` | Use two independent d6 read sequentially as the 11-66 table coordinate; treat the general “add” wording as an error because the specific procedure and table otherwise cannot agree | Approved |
| `BRK-DEC-002` | Add minimum BP continuity before `MOV-TASK-005`; do not create terminal-at-Breakdown Movement histories | Approved |
| `BRK-DEC-003` | Use Table 21.38's share of accumulated BP for the Sandstorm threshold; retain exact Sandstorm-attributed BP and apply the shift when it is at least half the total | Approved |

## Source index

| Source | Locator | Use |
| --- | --- | --- |
| [1979 Land Game rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf) | 5.2; 21.11-21.45 | Breakdown timing, subjects, cumulative BP, checks, losses, and persistence |
| Map A Terrain Effects Chart | 8.37 | Supported terrain/route/hexside BP inputs |
| [Common charts](https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf) | Table 21.38 | BP bands and sequential-dice percentage results |
| [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf) | 8.37; 21.12 | Corrected Track derivation and the Italian M13/40 BAR; no Truck or dice-language correction |

The bounded source vectors are Clear `4 BP`, Desert `24 BP`, Road `1/2 BP`, Track halving the
applicable underlying value, and `+2 BP` for Ridge/up-slope/down-slope. These are normalized data,
not permission to reproduce the source charts.

The implemented canonical Breakdown artifact explicitly carries all 36 legal sequential-d6
coordinates. The Truck `2L` BAR is supported by Land Rules 21.14 and Table 21.38; errata 21.12 is
not attached to that row because it corrects only the Italian M13/40. Exact artifact and ruleset-v7
identities are frozen by the checked golden and manifest tests.

## Approved source and contract freeze

`BRK-RSH-001` fixes the following minimum continuity vocabulary. Task 004B still exposes no action,
resolver, or RNG path.

| Contract fact | Frozen value | Primary locator |
| --- | --- | --- |
| Artifact | `cna-1979.1.breakdown-tables`, schema 1 | Repository normalization of 21.11-21.38 |
| BP amount | Exact non-negative reduced rational; canonical zero `0/1`; accumulated BP is rounded upward only for raw-band selection | 21.31 |
| Accumulated bands | `0-3`, `4-10`, `11-20`, `21-30`, `31-40`, `41-50`, `51-60`, `61-70`, `71+` | Common charts, 21.38 |
| Supported profile | `land.breakdown.profile.truck`, vehicle type `land.breakdown.vehicle-type.truck`, adjustment `2` columns left | 21.11-21.14 |
| Weather shifts | Normal `0`; Hot `+1` column right; Sandstorm `+1` right after its selected half-or-more threshold is met | 21.37a, 21.37b, 21.37d and Table 21.38 note |
| Rainstorm route rule | Road BP is treated as Track BP; it is not a column shift | 21.37c |
| Recheck memory | Retain the highest effective checked band during the Operation Stage; another check is possible only after reaching a higher band | 21.25-21.26 |
| Roll identity | Two independent d6 form a sequential `11`-`66` coordinate; for example `3,3 => 33`, never arithmetic `6` | 21.34 and common charts 21.38; owner ruling `BRK-DEC-001` |
| Synthetic cohorts | `axis-element-a.vehicle-cohort.trucks` and `commonwealth-element-a.vehicle-cohort.trucks`; each has one working Truck Point, zero broken points, and the truck profile/type above | Synthetic fixture rationale constrained by 21.11-21.14 |

The normalized continuity artifact retains the nine bands, signed shifts, edge clamping, exact
ceiling-to-band behavior, Rainstorm input transformation, and the 36-value sequential-dice domain.
The percentage result matrix (`0`, `10`, `25`, `33`, `50`, `75`) is verified research evidence but
is deferred to Breakdown adjudication: retaining dormant outcomes now would expand Task 004B beyond
its no-roll/no-result boundary. Task 004B records a nullable highest effective checked band but does
not calculate a result.

The sources conflict on Sandstorms. Table 21.38 shifts when at least half of accumulated BP was
acquired in Sandstorm sections; Rule 21.37(d) shifts when at least half of movement measured in CP
was spent there. Errata is silent. `BRK-DEC-003` adopts the table-specific instruction because it is
attached to the exact Breakdown table and composes with the stage-persistent BP ledger. Authority
therefore retains exact `sandstormAttributedBreakdownPoints` alongside total BP and rejects a
negative subtotal or one greater than the total.

The clean-cut identity migration retains Content Pack ID
`rules-lab.content.movement-contact.v1` while advancing ruleset `6 -> 7`, Content schema `3 -> 4`
and canonical format `v2 -> v3`, world `3 -> 4`, snapshot `8 -> 9`, and `CampaignCreated`
`7 -> 8`. Setup schema remains `5`, with derived hashes migrated. Older versions reject under the
repository's existing strict compatibility policy.

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

The approved delivery graph is:

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

## Smallest acceptance vectors

1. Golden supported BP rows and corrected Track derivation retain exact locators.
2. A non-motorized/no-cohort mover changes no BP state.
3. Two Road entries accumulate exact BP `1` without enabling a check.
4. One Clear entry accumulates BP `4` and reaches the first raw eligible band; the Truck `2L` BAR
   still shifts its effective band below the check surface.
5. Re-stopping in the same checked band does not enable a second roll; entering a higher band does.
6. Dice `3` and `3` produce coordinate `33`, never arithmetic total `6`, if `BRK-DEC-001` is approved.
7. BAR/weather shifts, upward rounding, replay equality, privacy absence, and forged-component
   rejection each have deterministic positive/negative vectors.

## Limitations and next gate

This packet does not normalize every vehicle class, loss-placement exception, or Breakdown result.
Task 004B implements only the approved continuity lane and passed two fresh-context review
instances. Breakdown adjudication remains a separate later package.
