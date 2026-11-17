# Alexandria supply placement candidate: independent review

**Disposition:** retain `gra-alexandria-placement-v1-proposed` as a coherent
planning interpretation, not an admitted rule. Its arithmetic and listed sample
caps check out under its own assumptions. Define the receipt boundary,
classification completeness and side-safe event/choice projection before
admission. The running algorithm is one policy for ambiguous timing, not a
source-proven reading of a whole Game Turn's final receipts.

## Source audit

Visually rechecked hash-matched [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
pp. 43–44 §§32.18, 32.31, 32.41–.45, [scenario booklet](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf)
PDF p. 5 / printed p. 29 §60.92(C), and [Commonwealth chart](https://spigames.net/PDFv10/CNA_ChartsCWPlayer.pdf)
PDF p. 31 §32.47. Their SHA-256 values are respectively
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`,
`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`, and
`f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981`.
This is a source recheck prompted by the candidate, not a blind transcription
or independent edition. Errata and every arrival pathway were not exhaustively
reviewed here.

§32.42 says Commonwealth supply units are placed in Cairo unless more than
three are received in one Game Turn, in which case one for every three received
**may** be placed in Alexandria. It does not state the candidate's floor,
arrival grouping, running calculation, treatment of later batches or city-hex
choice. §32.31 allows land or rail movement during the arrival OpStage;
§32.43 describes four-turn-ahead Commonwealth planning and the starting-turn
exception, and §32.45 ties the table to receipt four turns hence. §60.92(C)
separately supplies three regular units each at Alexandria and Cairo as setup.
The period-I table column reads 1, 1, 2, 2, 3, 3; its printed period spans
1940 through April 1941. One such result alone cannot exceed three. This does
not prove that all potentially qualifying receipts in a turn are limited to
one table result, or decide the initial preplanning exception.

## Findings

1. **Math and invariants.** With whole nonnegative `R`, `A`, `n` and
   `0 <= A <= cap(R)`, the candidate's `cap(R+n) - A` cannot be negative.
   Choosing integer `k` in `0..min(n, cap(R+n)-A)` preserves `A <= cap(R+n)`
   and assigns exactly `n` units. The strict threshold correctly gives 0 for
   3 and 1 for 4–5; floor gives 2 for 6–8. For 4 then 2, up to two of the
   latter batch can go to Alexandria **only if** none of the first four did;
   otherwise only one can. Owner may decline any permitted placement. Check
   input overflow, negative/non-integral values, duplicate IDs and corrupted
   replay invariants explicitly. Floor is a proposed whole-unit rounding rule,
   not text that the original expressly prints.
2. **Ordering changes outcomes.** Four units treated as simultaneous permit one
   of all four to start in Alexandria; sequential `2 + 2` permits only one of
   the second pair. Likewise `3 + 1` selects only the last unit, while a
   known-final-total policy could let one of the initial three start there.
   These can change arrival-stage movement and identity, even where final
   counts agree. Bind the atomic batch to a source-backed arrival event and a
   total order when events really differ; combine all genuinely simultaneous
   receipts before choosing. Do not let provider message partition, retry,
   asynchronous delivery order or player choice manufacture the boundary.
   Determine whether §32.43–.45 gives one authoritative whole-turn delivery
   or several and how scenario-start preplanning works; neither the table nor
   the current proposal proves either. A retroactively discovered receipt that
   belongs to an already committed boundary requires a correction/replay policy,
   not silent insertion into today's batch.
3. **Scope and unknowns.** §32.41 distinguishes initial scenario supply from
   additional table availability, supporting separate treatment of the explicit
   Alexandria setup, but does not settle every scenario grant, delay, captured
   supply unit, §59.66(C) loaded motorization or §32.18 dummy. Classify actual
   receipt, quantity, turn and stage under accepted source rules; planning rolls,
   movement or repeat observation are not automatically new receipts. The
   candidate's blocking rule needs a precise *closed boundary*: require complete
   information about receipts that could belong to the current or prior committed
   boundary, without demanding foreknowledge of an ordinary future batch merely
   because it might occur. Unknown future category rules and subsequently
   revealed earlier events need explicit handling; never assume zero, make a
   hidden future decision visible through blocking, or rewrite past placement.
   Separate ratio allowance reset from actual supply ownership, carryover and
   physical identity.
4. **Destination and atomicity.** A ratio allowance is an upper bound, not
   proof either city has a legal placement hex or capacity. Enumerate legal
   allocations and hexes against the same snapshot; where legality is unknown,
   block affected evaluation rather than auto-redirect, destroy or duplicate
   stock. Define what happens if Cairo cannot accept the mandatory remainder or
   neither city is legal; do not infer a fallback. Receipt, chosen allocations,
   stock and counter updates must commit atomically and idempotently. A crash,
   retry or duplicate event cannot grant new allowance or partially place
   units. Multi-hex city identity, control and stacking remain independent gates.
5. **Privacy needs an explicit contract.** “Protect private future plans and
   allocation choices” alone does not specify audiences, authorized timing or
   observable side channels. Keep future planning, receipt identities, `R`, `A`,
   cap, batch provenance and detailed blocked reasons in authoritative or
   owner-authorized views; release only what the accepted fog/discovery rules
   permit. Choice availability, number of offered choices, event count/order,
   stable IDs, timing, replay endpoints and `unsupported` responses can themselves
   reveal concealed receipts, stock or dummy identity. Compare histories equal
   under each audience's authorized view but differing in hidden future plans,
   other receipts and dummy identity; assert equivalent observable behavior
   until an expressly authorized disclosure. Owner-facing options may disclose
   only information that owner is authorized to know. This review does not
   decide that all such data must remain secret after a permitted observation.

## Review vectors

In addition to the candidate's examples, assert 8 -> cap 2 and 9 -> cap 3;
compare one batch of four against sequential `2 + 2` with distinct identities;
compare `4 + 2` after taking zero versus one from the first batch; test a
turn reset with placed stock retained. Test replay, duplicate and out-of-order
receipt IDs, integer bounds, a later discovery belonging to a prior boundary,
missing current classification versus an ordinary unknown future batch,
blocked Cairo and Alexandria separately, and source-specific dummy treatment.
Test identical authorized histories across all player and intelligence-plane
projections, including timing and errors. These are unexecuted design vectors,
not demonstrations that the synthetic receipts occur in the six-turn scenario.

No runtime changes or gameplay tests were made. Admission requires reviewed
source precedence, timing, classifications, city placement, event privacy and
contract versioning.
