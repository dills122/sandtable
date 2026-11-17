# Graziani monthly motorization: fractional-loss policy candidate

**Status:** proposed planning policy `monthly-motorization-round-down-v1`;
not an official clarification, accepted ruling, implementation, or readiness
change. This resolves only the fractional arithmetic once a numeric percentage
and an authorized snapshot exist. It does not resolve the September dash or
make the first October event executable.

## Source evidence and boundary

Visually rechecked the hash-matched [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
printed/PDF p. 44, §§32.51, 32.57, 32.59 and 32.65, and
[common charts](https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf)
printed/PDF p. 13, §58.5. SHA-256 respectively
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99` and
`51aa5a5bfdaca3d23794da71a45a830d98b798d060097cde6418126d6bd63bb0`.
This is a targeted reread of the same edition, not independent source or a
complete search for a general rounding convention or later correction.

§32.57 calculates each player's monthly percentage on that player's total
on-map motorization at the time, including Tripolitania–Tunis boxes, and
permits removal on map E for Commonwealth and map A including off-map for
Axis. It does not specify fractional rounding in that case. §32.51 treats
motorization as medium truck points except as modified. §32.65(A) explicitly
rounds *motorization* fractions down for a 10% convoy result, while rounding
replacement points up; §32.65(B) rounds a 20%-or-greater convoy result down
but imposes a separate minimum loss. These are convoy rules, not an express
monthly-loss rule. They offer a nearby type-specific analogy, not authority
to import the convoy minimum. Chart §58.5 prints September 1940 as a dash,
October as 6/2, and November as 5/3; its legend gives Commonwealth/Axis
ordering but no dash definition. The chart title/number differs from §32.59;
the rationale for mapping it is retained in [motorization source review](2026-11-17-graziani-motorization-dummy-review.md).

## Proposed arithmetic and rationale

For each side and monthly event, after separately admitted chart, calendar and
snapshot decisions yield nonnegative whole-point total `T` and numeric integer
percentage `P`, calculate **one** loss `floor(T * P / 100)` using exact integer
arithmetic. Aggregate all qualifying on-map points before rounding, not per
stack, unit or region. No remainder carries to another event. A result below
one point is zero; do not impose a minimum. This applies the percentage to the
whole total as §32.57 specifies and uses the nearby motorization-specific
round-down convention without presenting it as a printed monthly instruction.
Snapshot membership and timing are independently gated: do not use this policy
to decide whether a point or arrival belongs in `T`.

Alternatives remain material: round half up/nearest minimizes per-event
fractional error, round up preserves some loss for any positive percentage,
and a persistent fractional remainder preserves more long-run aggregate loss.
Each changes low-total outcomes, and a carry adds state absent from the cited
case. Convoy's minimum-one clause is another possible analogy, but depends on a
different event and result threshold. None is established by §32.57. Version
this choice independently so a later authoritative rule can supersede it
without silently changing existing replays.

## Selection, shortfall and admission

The computed count and permitted removal set are different: total includes
all eligible on-map points, while the source limits where losses are removed.
If enough eligible points exist, the owning player selects exactly that count
from permitted locations; do not auto-select using hidden or arbitrary order.
The accepted rules must separately specify what happens to assigned points,
legal allocation and consequences. If fewer are eligible, **block this event
before any removal**, preserve state, and surface an authoritative unresolved
rules condition. Do not cap losses, borrow from another map, create debt, or
make partial removals under this proposal. This is a fail-closed project
handling decision, not a claim the printed rules require a stop. Persist the
snapshot and validated choice atomically with event identity on eventual
admission; repeated submissions or replay must not apply losses twice. Public
reporting must follow the existing side-specific information policy, without
exposing hidden location or inventory through errors or choices.

Admission remains blocked on a versioned choice for chart identity and month
key, September dash, first-month trigger and pre-operation convoy, snapshot
ordering relative to arrivals, eligible ownership and removal semantics,
shortfall disposition, and disclosure. [RUL-GRA-09](2026-11-12-graziani-ruling-decisions.md#rul-gra-09-monthly-motorization-and-dummy-supply-bounded-planning)
already selects the previous-month key for planning and leaves the dash unknown;
this proposal does not alter that decision. It must be reviewed against any
general rounding rule or authoritative errata found later. It cannot make a
claim that a given event is reachable in the historical six-turn setup.

## Focused deterministic vectors

Use synthetic certified snapshots and numeric chart inputs; values below test
this proposal, not the unresolved September dash:

- `T=25, P=6` yields 1 (not 2); `T=50, P=6` yields 3; `T=16, P=6`
  yields 0; `T=0` yields 0. For Axis, `T=75, P=2` yields 1. A numeric
  zero percentage also yields 0, but a dash produces no numeric result.
- Two qualifying stacks of 9 and 8 at 6% yield 1 after aggregation, while
  separately rounded stacks would yield 0. Vary iteration order, region and
  integer scale; exact arithmetic and results must remain stable.
- Include qualifying points outside the eligible removal region in `T`, then
  verify owner selection stays in E or A, respectively. Exact availability
  succeeds; one point short blocks with no state change or partial choice.
  Check assigned points only after their eligibility is decided.
- Re-submit a successful event and replay its journal: one committed loss,
  identical snapshot and result. An unresolved dash, incomplete snapshot or
  shortfall cannot advance the event or masquerade as a zero loss. Compare
  player-visible outputs for states differing only in hidden inventories.

Affected future surfaces: versioned abstract logistics configuration and
monthly event/choice contracts, Core authoritative accounting and state,
Host journal/replay, and side-safe boundary tests. No runtime or content was
modified, and no gameplay tests were run for this research document.
