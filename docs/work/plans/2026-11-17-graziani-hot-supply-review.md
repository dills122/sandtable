# Hot weather and abstract supply: independent review

**Disposition:** Retain `gra-hot-abstract-fuel-v1` as a narrow, fail-closed
candidate. Its source facts and conditional exact arithmetic check out; its
per-unit grouping and decision to apply §29.34 to abstract fuel are project
choices, not established printed precedence. Complete subject classification,
privacy and RNG handling must be admitted alongside any effect. No numeric
rule follows for the 2.5-point Tobruk example in integer-only state.

## Original sources

Visually rechecked hash-matched [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
pp. 41 and 43 §§29.1, 29.31, 29.34 and 32.0–.18, and
[scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf)
PDF pp. 4–5 / printed pp. 28–29 §60.92(B–C). SHA-256 respectively
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`
and `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`.
The [candidate](2026-11-17-graziani-hot-supply-next.md) reports a bounded
September errata search; this check did not independently repeat that search or
survey later corrections. Same scans and edition, not a blind second edition.

## Findings

1. **Weather exclusions and scope.** §29.34 reduces fuel and water levels by
   5% during Weather Determination, includes supplies in dumps and trucks,
   excludes water in wells/oases and fuel/water actually being consumed by a
   unit. It does not say all fuel in a transported supply unit is being
   consumed. §29.31 applies hot across map sections; §29.1 separately excludes
   Malta, Italy, Sicily, Crete and Tunisia–Tripoli from weather and provides a
   map-E rule for Allied off-map airfields. Excluding only certified on-map
   subjects from the candidate's numerical scope is appropriately bounded;
   it must not be mistaken for proof that every other off-map holding is exempt.
   Actual-consumption identity, carrying and source-specific location remain
   inputs. Check other on-map/loaded fuel and applicable water independently,
   not just identified real supply counters.
2. **Land-only precedence and subjects.** §32.0 replaces ordinary expenditures
   with the abstract procedures; §32.11 instructs players to ignore ordinary
   usage/expenditure and use §32's supply rules. §32.12–.15 represent supply
   units by dump counters, retain fuel and ammo balances, and remove a unit
   when both reach zero. These facts support examining fuel as a potential
   subject but do not explicitly decide whether a non-expenditure weather loss
   survives abstraction. Applying, waiving or deferring it remain materially
   different interpretations. §32.18 calls dummies supply units and describes
   them as deceptive; treating them as having no actual debit is sensible but
   still requires accepted identity and state semantics. Initial motorization
   can also have load choices, so a complete preflight cannot infer all fuel
   subjects from the scenario's regular supply rows alone.
3. **Numbers and grouping.** §60.92(B) lists three Italian Tobruk units at 50
   fuel each and two Bardia units at 40 each; §60.92(C) lists five Commonwealth
   Matruh units at 60 and three each at Alexandria and Cairo at 60. These are
   initial facts, not proof of a later balance, legal placement or a hot roll.
   Per-unit 5% yields exact losses 3 from 60 and 2 from 40; 50 yields 2.5.
   Two separate 10-fuel balances each yield 0.5, versus 1 from the aggregate
   20. Under the candidate's **per-unit** grouping, an integer-only model must
   block on either fractional per-unit loss even when their aggregate is whole;
   it cannot use the aggregate to evade the gate. A per-location or aggregate
   rounding rule is an alternative, not equivalent behavior. Candidate's
   exact-only logic is mathematically coherent but will block ordinary
   nonmultiples of 20; it is deliberately incomplete, not a playable default.
4. **Atomicity and replay.** A hot result must not mutate a subset of stocks
   before discovering an unknown subject or fractional case. A durable pending
   weather result and RNG cursor must survive retries; do not reroll on
   blockage, change stage, or duplicate an effect. A pre-draw preflight is a
   separate choice: it must not inspect/select a favorable outcome or discard
   consumed RNG, and may conservatively block a stage whose eventual weather
   would have had no such effect. Source does not require that implementation
   strategy. A rational-fuel alternative would also require explicit serialization,
   future spending and comparison semantics, not just a different display.
5. **Privacy and other state.** Block status, timing, event counts, stable IDs,
   error codes and differing legal actions can reveal an opposing fractional
   balance, real/dummy identity or missing subject even when detailed logs are
   withheld. Define authorized player, observer, operator and intelligence-plane
   projections, including pending behavior and timing, before using those
   diagnostics. Compare equal-authorized histories that differ only in hidden
   quantities or dummies, plus an explicitly permitted disclosure case. Preserve
   ammo and apply §32.15's removal rule only to an accepted valid post-state;
   do not create or retain an impossible already-empty counter merely to test
   the weather rule. Atomicity and redaction do not establish source precedence.

## Verification boundary

Use synthetic 60/40/50/10+10 cases, a real transported counter, separately
loaded motorization, an actual-consumption exclusion, Tripoli/Tunisia, unknown
other off-map location, water/wells, dummy versus real, zero fuel with positive
ammo, and missing subject classification. Test no-hot outcomes independently,
crash/retry and exact RNG cursor, same-authorized-history projections and
no partial debit. These are unexecuted vectors; source coverage, fraction
representation, subject rules, interpretation of §32.11 and stage ordering
remain gates. No code, runtime or readiness changes and no gameplay tests.
