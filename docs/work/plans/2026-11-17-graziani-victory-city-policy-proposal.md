# Graziani Italian strategic victory city policy proposal

**Status:** selected for planning in [RUL-GRA-22](2026-11-12-graziani-ruling-decisions.md#rul-gra-22-italian-strategic-city-witness-bounded-planning), not runtime, map, route, or gameplay admission. Based on the [city-hex scope note](2026-11-17-graziani-victory-city-hex-scope.md) and its [independent review](2026-11-17-graziani-victory-city-hex-scope-review.md). See also the [independent proposal review](2026-11-17-graziani-victory-city-policy-proposal-review.md); no additional source review performed.

## Proposed interpretation

For §60.81 Italian strategic victory, interpret “either Alexandria or Cairo (any hex)” as selecting either city and one qualifying hex of that city. Apply “any hex” to both Alexandria and Cairo. Interpret “with a combat unit that can be supplied” to require the combat unit holding that selected objective hex to be the witness of the applicable convoy supply capability. This is a proposed project interpretation of the printed grammar, not an explicit source-defined identity predicate. It concerns capability to be supplied, not necessarily actual supply status.

The parenthesis follows Cairo, and a Cairo-only scope is an alternative grammatical reading. An all-hex possession requirement is another distinct policy alternative. A separate holding unit and supply-capability witness, including a witness on another city hex, is a more permissive alternative; neither alternative is established by the cited location summary or distance chart. The chart's E3613 and E1829 are representative distance endpoints, not proof of exclusive victory hexes or legal convoy endpoints.

This proposal binds the holder and capability witness at the relevant evaluation; it does not decide whether the same unit must have taken and held continuously, whether another unit may substitute, or how interruption and recapture affect holding. Taking, holding duration, timing, qualification as a combat unit and evaluation of capability need separate rulings. It does not decide the quantifier over Game-Map D or define the convoy predicate.

## Admission boundaries

Before implementation, independently establish exact eligible city-hex membership and playable map topology; determine whether occupation and control differ, what holding means, and who controls relevant origins and endpoints. Establish convoy origin and route legality, supply-capability conditions, and authoritative evidence of taking and holding. Specify continuity, permitted substitution, interruption and recapture, the evaluation boundary, and terminal and cleanup ordering; a capable holder in a terminal snapshot alone does not prove the required history. The location summary gives candidates, not a complete playable membership or topology certificate; its marks and the chart endpoints do not establish eligibility, control or routes.

Missing map, route, control or history evidence is unknown, not a failed conjunct. A verified disqualifying fact establishes a particular conjunct false only under the accepted predicate and sufficiently complete evidence for that fact; it does not certify other predicates. Apply the selected composition below only after its predicates and evidence rules are admitted. Do not certify a route from a chart distance. Version any accepted policy and dependencies for deterministic replay. Select the separate Commonwealth city-source policy, including source eligibility, control and whether different objectives may trace to different cities or hexes; the Italian parenthesis does not decide it.

**Composition clarification:** RUL-GRA-07 selects known-false dominance within
a row: a proven false required conjunct makes that row no-match even if a
different conjunct is unknown; any row that remains unknown keeps the overall
result unresolved. Sufficient coverage means proof for the selected row
determination, not knowledge of every masked conjunct. External verdict and
terminal behavior still require the separate privacy and timing gates below.

Define authorized victory disclosures and side-specific observations before exposing evaluation. Compare histories indistinguishable to an observing side across offers, result and rejection categories, event counts and identifiers, timing, whether and when play stops or state freezes, verdict availability, retries, acceptance of later actions, and subsequent observable effects. A generic rejection alone does not guarantee privacy: an unresolved-to-terminal transition can itself reveal hidden state. Define authorized terminal disclosures and filter internal results and evidence per side. If no safe protocol is established, keep externally observable evaluation and terminal behavior independent of hidden inputs; do not disclose hidden opponent location, control or supply information. This is a software admission requirement, not printed source text.

## Proposed discrimination checks

These synthetic cases compare policies; they are not executed tests or claims of legal reachable play. Run only after respective dependencies are specified:

- For each city, compare one eligible hex held by a qualifying combat unit that can be supplied under the accepted predicate with all remaining city hexes unheld; compare Cairo-only parenthesis and all-hex alternatives.
- Compare a holder that can be supplied with a holder that cannot, while a different unit on another city hex can. The proposed binding distinguishes these; the separate-witness alternative may not.
- Compare a non-chart city hex with E3613 or E1829 under independently established membership and equivalent route capability. Do not assume a route exists.
- Compare continuous same-unit holding with substitution, interruption and recapture at each candidate evaluation boundary; expected results await temporal policy. Compare capability with actual supply status separately.
- Exercise unknown map, control, history and route inputs, including a known-false conjunct paired with an unknown conjunct, replay and duplicate evaluation. Compare paired histories with identical permitted observations but different hidden capability or control, including terminalization, freeze, retries and subsequent actions. Compare Commonwealth endpoints separately under each proposed source policy.
