# R01 capacity derivation, 2026-10-15

These are admitted-profile encoding bounds, not maximum arbitrary grammar objects or resource benchmarks. The unchanged parent authenticates CreationRequest, thirteen prefix events, three lifecycle events, one Breakdown event, actual selection/round/paid ledgers, two Release events and two control events. Closure framing strings deliberately do not authenticate as those roots.

## Source

The retained N3 certificate bounds its canonical Source at31,722,496 bytes. A canonical ASCII JSON document contains no literal control bytes: string controls are already printable escape sequences. Embedding its bytes as a new JSON string therefore costs at most twice its length (quote/backslash escaping). Each of at most six canonical event strings has the same property and at most65,536 bytes. The exact fixed outer envelope for six empty event strings is102 bytes, including keys, punctuation, quotes, commas and contractVersion1. The reserved512 bytes dominates that envelope.

`2*31,722,496 + 6*2*65,536 + 512 =64,231,936 <67,108,864`.

The adversarial all-quote framing measurement64,231,526 equals the payload maxima plus102; its410-byte distance from the bound is exactly512−102. This corroborates an independently enumerated envelope, not an empirical claim about maximum authenticated Sources. Invalid carrier strings containing raw decoded controls may escape more than2x, but they are not canonical embedded documents and cannot authenticate. The Source parser still rejects all inputs above64MiB before parsing.

## Reachable World

The existing parent native World cap is524,288. R01 changes no origins, components, assets, duties, settlements or representations except the single moving representation's current location. Publication leaves World byte-identical. Movement changes only:

| Component | Upper growth | Reason |
|---|---:|---|
| New cohesion causes |7,680 | At most five, each1,536 including comma. Fixed68-character receipt,71-character cause ID, fixed29-character kind, one variable ID≤128 characters, seven bounded decimal fields, fixed keys/punctuation. Six bytes per variable ASCII character conservatively covers JSON escaping. Extreme serialized overapproximation is1,116 bytes. |
| Current locations |1,536 | One element and one representation, each final replacement≤128*6 bytes. These are final-root replacements, not five appended strings. |
| CP/cohesion decimal widths |32 | Only moving CP numerator and cohesion change; denominator unchanged. Final CP≤15 and signed32-bit cohesion. |
| Relationship ending |128 | Trigger admission forbids any existing relationship (`verify-combat-inherited-reaction-trigger-v1.py:162`). Intermediate lifecycle and entry movement create none. One actual paid settlement projection appends at most one relationship (`verify-combat-actual-paid-assault-v1.py:293`); armed creates none. R01 creates none. It can end the sole live relationship once: null→fixed receipt, null→`ordinary-break-off`, true→false, exact growth83. |
| Total |9,376 | Less than10,240 reserved growth. |

Thus `524,288+10,240=534,528 <1,048,576`. The earlier wording allocating all10,240 exclusively to five2,048-byte causes was incomplete; the bound itself is unchanged and the table accounts for every mutated World field. The extra864 reserve is unused. The 512 inherited array ceiling remains independently checked; exhaustion rejects admission and cannot produce an overflowing root.

The593,548-byte World framing vector injects512 arbitrary128-character References into each of four origin fields. Those origins are immutable across authenticated movement and are fixed by the inherited created/prefix replay, so that vector is outside this reachable profile. It establishes that the new owned compound grammar accepts World beyond the predecessor512KiB cap. It is not evidence for a reachable534,528-byte maximum. Complete isolated support routes additionally exercise391 real projections and780 ordinary moves through their CP ceilings; maximum observed World cause count5 is corroboration only.

## Control and Proof

The inherited Control envelope is655,360. The65,536 growth reserve covers the9,376 World growth above, six added records in each of receipts and movementReceipts (each below512 bytes:6,144), current progress five and explicit retained prior progress three (each below512:4,096), retained target-use copies (at most one below1,024), one route of six IDs with its UnitKey (below8,192 including worst escaping), copied authority/binding/closure/pending-intent metadata (below16,384), and all new field names/punctuation (below2,048). Sum47,264≤65,536. No unbounded history is added. The inherited duplicated fields are charged again conservatively. Control≤720,896. Proof embeds Control as an object plus fixed hashes/IDs/version and nullable last/terminal receipts, with2,048 reserved framing bytes:≤722,944. Hard limits remain1MiB and2MiB respectively.

## Executable arithmetic evidence

`python3 docs/work/reviews/2026-10-15-actual-moved-repeat-movement-capacity.py` passed. It checks exact102-byte Source envelope,6-event formula, serialized extreme cause1,116, ending growth83,9,376 total growth, unchanged534,528/64,231,936 schema values and hard-cap dominance. It constructs overapproximating strings solely for encoding arithmetic; it never publishes them as authenticated facts. No oracle/schema/fixture change was needed, so the running final normal verifier still applies to the same semantics and hashes.
