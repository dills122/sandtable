# RUL-GRA-22 independent audit

**Disposition:** Conditional planning choice is supported by the linked source
reading and reviews. The composition ambiguity below is resolved by the revised
ruling and proposal. No runtime admission or historical verdict follows from
this audit. No original scans or implementation inspected, and no gameplay tests
run.

## Re-review of composition clarification

Revised RUL-GRA-22 and the city proposal both define sufficient coverage as
proof for each row's determination, without requiring every masked conjunct to
be known. They explicitly preserve a proven false conjunct making its row
`no-match`, any still-unknown row making the overall result unresolved, and the
separate privacy and timing gate for externally observable verdict and terminal
behavior. These statements match RUL-GRA-07. The proposal retains the fallback
requiring hidden-input-independent observable behavior when no safe protocol is
established. No remaining inconsistency found in the revised choice.

The independent proposal review still uses the earlier phrase “incomplete
coverage”; read it with the revised proposal's explicit definition. The
six-row discriminator below remains useful for the eventual contract, with
expected logical results fixed by RUL-GRA-07 and publication separately gated.

## Original finding: completeness and result composition

The earlier RUL-GRA-22 required “complete evidence” while preserving RUL-GRA-07.
The earlier city proposal said to avoid a final result from “incomplete coverage.” RUL-GRA-07
instead makes a row `no-match` when a required conjunct is known false, even
if another conjunct is unknown; if all six row outcomes are known, its result
can be final. For example, sufficiently proved failure of the Italian strategic
holding predicate plus unavailable convoy evidence gives that row `no-match`;
five other known rows permit a composed result under RUL-GRA-07. If complete
evidence means every conjunct must be known, the city admission language blocks
that same result. If it means sufficient evidence to determine every row, both
can coexist. Neither earlier document defined that distinction explicitly.

The revision selects row-outcome sufficiency. Preserve the distinction between
sufficiently proved false and missing evidence. Add a synthetic
six-row case with a known-false holding conjunct and unknown route, five known
other rows, and explicit expected row and overall outcomes under the agreed
contract. Separately test a row with no known false and an unknown route, which
RUL-GRA-07 requires to make the overall result unresolved.

## Checks and remaining gates

- One named city, one qualifying hex, and the same current holder/capability
  witness match the proposal. Cairo-only scope, all-hex possession and separate
  witnesses remain recorded alternatives; unit continuity is not selected.
- The source review distinguishes capability from actual supply and chart
  endpoints from certified objective hexes. Commonwealth source selection and
  Map D quantification remain separate questions, as the ruling states.
- The proposal and its review require an audience-safe protocol covering
  terminalization, freezes, retries and subsequent actions. If no safe protocol
  exists, they require observable evaluation and terminal behavior independent
  of hidden inputs. RUL-GRA-22 links these requirements and requires a protocol
  before admission; the fallback must remain binding in the eventual contract.
- Synthetic discrimination cases are not evidence of reachable play. City
  membership, topology, route and control, causal holding history, temporal
  boundaries, replay and versioned policy remain prerequisites.

Evidence: [RUL-GRA-22 and RUL-GRA-07](2026-11-12-graziani-ruling-decisions.md),
[city proposal](2026-11-17-graziani-victory-city-policy-proposal.md),
[proposal review](2026-11-17-graziani-victory-city-policy-proposal-review.md),
[source reading](2026-11-17-graziani-victory-city-hex-scope.md),
[source review](2026-11-17-graziani-victory-city-hex-scope-review.md), and
[result composition proposal](2026-11-17-graziani-victory-independent-decision.md).
