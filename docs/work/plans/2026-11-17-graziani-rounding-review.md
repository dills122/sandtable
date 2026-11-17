# RUL-GRA-09 monthly motorization rounding: source review

**Disposition:** Support the round-down rule as a bounded, versioned planning
choice, subject to wording correction and the existing admission gates. Do not
implement or mark the obligation ready. Land §32.57 does not expressly specify
fraction rounding or shortfall handling; neither the convoy analogy nor this
review supplies an official resolution. This is a fresh visual reading of the
same edition, informed by the policy under review, not a blind reading or an
independent edition.

## Source check

- [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), PDF and
  printed p. 44, §§32.51, 32.54–32.57, 32.59, 32.65(A)–(B); local
  `/private/tmp/sandtable-land-rules.pdf`, SHA-256
  `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
  Hash verified and page rendered at 200 dpi for this review.
- [Common charts](https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf), PDF
  and printed p. 13, §58.5 and its legend; local
  `/private/tmp/sandtable-common-charts.pdf`, SHA-256
  `51aa5a5bfdaca3d23794da71a45a830d98b798d060097cde6418126d6bd63bb0`.
  Hash verified and page rendered at 175 dpi for this review.
- Compared [candidate](2026-11-17-graziani-rules-next.md) and
  [RUL-GRA-09](2026-11-12-graziani-ruling-decisions.md#rul-gra-09-monthly-motorization-and-dummy-supply-bounded-planning).
  Did not search all original rules for a general rounding rule, recheck
  September errata, inspect later editions, or verify event reachability.

## Findings

1. **Clarify denominator versus removable set before admission.** §32.57
   applies each player's percentage to that player's *total* motorization points
   on the map at that time, explicitly including Tripolitania–Tunis boxes. It
   separately says losses may be removed anywhere in map E for Commonwealth,
   or map A including the off-map region for Axis. The ruling's phrase “total
   eligible points” can be read as the points eligible for removal, which would
   incorrectly narrow the denominator. Define `T` as the full qualifying
   on-map owned total, including the expressly included boxes; define `R` as
   the independently accepted legal-removal subset. The detailed candidate
   makes this distinction, but the selected ruling should be equally explicit.
   Ownership, captured and assigned points, other boxes and arrival/snapshot
   timing need their own sourced rules; “qualifying” cannot silently subtract
   points solely because their locations are outside `R`.

2. **Floor is a reasonable analogy, not source-mandated.** §32.57 has no
   fractional instruction in the inspected case. §32.65(A), an Axis convoy
   result of 10%, rounds motorization fractions down while replacement points
   round up. §32.65(B), convoy result of 20% or more, rounds fractions down
   with an additional minimum elimination. These distinct results support
   treating `floor(T * P / 100)` as a type-specific project choice, but neither
   clause governs monthly loss or proves that monthly losses have no minimum
   or carry. Aggregate once because §32.57 speaks of the player's total, then
   apply the chosen floor once; do not apply separate floors per stack or map.
   Whole nonnegative points and a numeric percentage are preconditions, not
   evidence that all inventory state has been certified. Exact integer
   arithmetic also requires a checked or sufficiently wide intermediate product.

3. **Dash is not zero.** Chart §58.5 prints dashes for January–September 1940,
   then October `6/2` and November `5/3`; legend assigns left to Commonwealth
   and right to Axis and gives no dash definition. Its heading is Abstract
   Truck Loss Chart, while §32.59 uses Abstract Truck/Motorization Loss Chart.
   Prior cross-reference evidence supports the mapping; this page alone does
   not establish a formal renumbering correction. Under the selected previous-
   month policy, a required first September stage consults August's dash and a
   required first October stage consults September's dash. Both remain
   unresolved rather than producing zero; whether either stage occurs is a
   separate calendar and scenario-entry question. The explicit zero-rate test
   is synthetic, not a claim that a dash or a specific chart cell is zero.

4. **Shortfall block is a safe interim policy, not a rule.** §32.57 says points
   lost may be removed within the side's specified region; it does not state
   what happens if that region cannot provide the full computed loss. Blocking
   without partial loss, capping, debt or substitution is an appropriate
   fail-closed project handling while that policy remains undecided. Separately
   label the owner's selection among legal points as an implementation
   interpretation: the case's permissive location wording does not explicitly
   settle all selection, assignment, or forced-loss consequences. Do not
   equate temporary blocking with a final admitted shortfall rule.

5. **Define transaction and privacy boundaries.** §32.54 prioritizes assigning
   motorization to historically motorized infantry, while §32.56 addresses
   elimination and capture of assigned points. Neither inspected case explains
   all consequences of a monthly removal from an assigned unit. Require an
   accepted eligibility/allocation and downstream-status policy first. Atomically
   validate snapshot, loss count and owner choice against the same state version;
   a stale or insufficient choice must leave inventory, assignments and stage
   advancement unchanged. Distinguish a durable blocked obligation from a
   committed zero-loss result; retries must not duplicate loss. Owner-only
   details and public projections need an explicit information policy, including
   whether loss counts or validation errors reveal hidden totals. Do not demand
   equal outputs where independent game rules authorize differing observations.

## Additional deterministic acceptance vectors

These are synthetic contract vectors; no claim of actual scenario reachability
or of a passed test suite:

- Commonwealth `T=25`, `P=6`: loss 1. Let 20 points lie outside map E and 5
  eligible points on E; result remains 1, and a legal removal must come from E.
  If zero are eligible on E, the same calculation produces 1 but the event
  blocks without mutation. Contrast the incorrect `floor(5*6/100)=0`.
- Axis `T=75`, `P=2`: loss 1, including otherwise qualifying points in the
  Tripolitania–Tunis boxes in the denominator. Determine actual removal
  eligibility independently rather than presuming every box is removable.
- Split `T=17` into 9 and 8 at 6%: aggregate result 1, not the sum of two
  independently rounded zeros. `T=16` gives 0; exact `T=50` gives 3. The
  result is stable under input order, and larger qualifying `T` cannot reduce
  loss for fixed nonnegative `P` absent overflow.
- A numeric zero rate and a certified `T=0` each yield zero only after their
  respective inputs are established. An unspecified dash or incomplete snapshot
  blocks even when a provisional denominator is zero. Check both August and
  September dash lookups under the selected month policy, conditional on their
  respective event actually being required.
- Exact `R=loss` permits a complete valid choice; `R=loss-1` blocks without
  partial mutation. Revalidate after an intervening state change. On successful
  commit, duplicate command and journal replay do not remove another point;
  an unresolved event cannot advance the stage. Include assigned-unit status
  and side-authorized views once those separate rules are specified.

No runtime, content, readiness flag, or existing document was changed by this
review. No gameplay tests were run.
