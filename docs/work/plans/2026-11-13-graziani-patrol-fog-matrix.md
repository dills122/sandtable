# Graziani: patrol and fog research matrix

**Status:** first-reader source analysis, not a complete detection rule, public
observation contract or proof patrols occur in the scenario. Original Land
pp. 26–27 (§§16.0–16.5), p. 43 (§32.25), p. 45 (§3.6 addendum), the common
charts pp. 1 and 3, and September errata pp. 1–2 were visually inspected. See
the [source register](2026-11-12-graziani-source-register.md) and existing
[contact knowledge contract](../../specs/combat-contact-knowledge-v1.md).

| Source issue | Bounded observation | Decision and test boundary |
| --- | --- | --- |
| Prior combat | §16.0's general wording refers to a stage in which no Anti-Armor or Close Assault combat occurred; §16.21 states the phasing player must not have engaged in either against enemy units. The sequence outline refers to assault. | Review whether earlier opposing-side combat bars a later player's patrol. Retain per-side and per-stage history until interpretation is accepted; test both acting orders and barrage separately. |
| Eligibility and costs | §16.11 identifies eligible reconnaissance, specified light tank and motorized-infantry types; the errata adds more types. §16.13 limits points from one hex to two; §16.14 limits a target to three combined points. §16.15 bars units at cohesion −8 or worse. §16.16 gives ordinary ammo/fuel costs, while Land-only §32.25 says patrolling does not require supply expenditure. | Establish actual eligible TOE types, cohesion, composition and source corrections; test 2/3 source limit, 3/4 target limit, threshold, phasing side and correct abstract-mode debit. Do not use physical truck costs or invent an abstract patrol charge. |
| Target and route | §16.22 permits an enemy-occupied target within five hexes under specified passability and enemy-control/ZOC constraints. §16.17 excludes origin or target hexes affected by rainstorm or sandstorm. Patrols do not physically move the source units. | Resolve path distance/hexside, intermediate enemy control and target exception, origin/target weather and competing routes. Test 5/6, blocked versus clear path, same-stage history and unchanged unit location. |
| Losses and RNG | §16.32 makes an empty-of-combat-units disclosure before the patrol survival lookup and skips that survival roll in that case. §16.33 applies a modifier when all patrol points are from recce units; §16.34 separately resolves objective losses after information or elimination. | Bind ordered draws, exceptions and casualties to one event; do not skip the later objective-loss step merely because the survival roll was skipped. Test no combat units, all-recon versus mixed, all patrol points lost, capture and replay. |
| Information | §§16.5–16.55 govern number of reported battalion-size units, defender choices, exclusions and a tolerance around strength. The p. 45 §16.51 addendum says the patroller does not identify TOE point types; §3.6 addendum separately protects opposing details. | Define which unit designation, class, motorization and bounded strength fields are available, which participant chooses, how long reports remain usable, and audience-safe results. The wording about strength needs an accepted precision/range rule. An empty-combat disclosure is scoped to this procedure, not a global World query. |
| Dummy formations | §§16.43–16.46 constrain dummy tank formations and describe a defender's misleading patrol report. The original contains no global guarantee that every reported designation or strength equals private truth. | Source-check whether either side can have a dummy in this initial six-turn profile, costs under abstraction, report choice and later discovery. Preserve private truth separately from an authorized apparent report; test equal-visible histories and disclosure only when source permits. |

These source facts do not authorize silently changing the existing adjacency
contact policy, or treating a patrol report as a current direct-assault target.
Before activation, freeze the source/ruling identity, full side-safe contact and
report lifecycle, unknown-versus-empty results, event ordering, deterministic
randomness and replay. Ensure unsupported mandatory/fog cases fail before play;
an outward action, error, event count or timing must not expose information the
source does not authorize. Detailed disclosure and gameplay choices require
independent review and privacy tests.
