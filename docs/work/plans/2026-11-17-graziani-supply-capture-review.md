# Graziani supply capture: source recheck

**Status:** unresolved rules interaction; provisional interpretation for planning,
not an implemented or source-confirmed state machine. This is a fresh visual
pass by the author of the earlier capture note, not an independent reader or
edition. No proof of scenario contact or capture reachability follows.

## Evidence and correction

Rechecked the printed [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
pp. 14, 18, 43, §§8.13, 10.21, 10.29 and 32.12–.18, and
[scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf)
printed pp. 28–29, §60.92(B–C). Local scan SHA-256: Land
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`;
scenario `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`.
Prior bounded errata search is documented in
[capture note](2026-11-17-graziani-supply-capture-next.md); it does not prove
there is no correction elsewhere.

The earlier note correctly flagged both triggers, but should state more sharply:
**§10.29 does not itself establish an ordering fix for §8.13.** Even if ZOC
capture applies first, the printed passage does not say the counter disappears,
changes ownership immediately, or ceases to block entry. Conversely, §32.13
does not explicitly say it overrides §8.13. Neither provision settles all
resulting state changes. A simple rule that adjacency never captures also
overstates the evidence.

## Trigger and precedence

* §8.13 prohibits entry into a hex containing an enemy unit and expressly
  references §27.4; it does not expressly name §32.13. §32.12 represents a
  Supply Unit by a Supply Dump counter. §32.13 says it has no combat
  capabilities and says enemy combat-unit entry captures it, with supplies
  used immediately and freely. Treating the capture case as a narrow specific
  exception to the general entry prohibition is a reasonable **provisional
  project choice**, not an explicit printed precedence rule. It must not
  authorize entry through a remaining enemy combat defender or waive terrain,
  movement and other applicable restrictions. The precise interaction with
  another remaining enemy noncombat counter still needs a ruling.
* §10.29 separately captures a qualifying noncombat unit **alone** in enemy
  ZOC, with no strength of any type, at any time during the enemy
  Movement/Combat Phase. The supply counter has no combat capabilities, but
  whether that satisfies “no strength of any type,” whether cargo is relevant,
  and what “alone” means with multiple counters remain interpretive questions.
  A qualifying ZOC can arise without entry: §10.21 supplies projection and
  terrain limits, so adjacency alone does not establish the trigger.
* §32.13's entry trigger and immediate/free-use language must remain visible in
  any accepted interpretation. Neither printed case specifies an allocation,
  whether all stock is consumed, remainder, durable owner, or how to order a
  possible earlier §10.29 capture against later entry. Do not defer a known
  immediate effect to a later stage merely for implementation convenience;
  gate unsupported transactions until that effect and stock accounting have a
  reviewed interpretation. §32.15 independently removes a supply unit when
  **both** ammo and fuel reach zero, not when only one is zero.
* §32.18 removes a dummy on discovery; it does not here identify every
  discovery trigger. Do not infer discovery solely from adjacency, invent dummy
  cargo, or assert a simultaneous dummy-removal/capture ordering. The §10.29
  test on a dummy and mixed stack also requires a discovery and “alone” ruling.

## Examples and tests

These are conditional state vectors, not assertions about a legal route or
historically reachable deployment. §60.92(B) lists three Axis Tobruk real
units at 15 ammo/50 fuel each and two Bardia units at 10/40 each; §60.92(C)
lists five Commonwealth Matruh units at 40/60 each, plus specified dummy
groups and placement instructions. Those quantities do not settle identity,
current contents, co-location legality or capture outcomes.

1. Place one real 15/50 Supply Unit alone in a synthetic hex and a qualifying
   opposing ZOC across a valid side. During the enemy Movement/Combat Phase,
   evaluate both plausible §10.29 classifications; if captured, do not infer
   unit deletion, free consumption of 65 points, or later entry legality. With
   no qualifying ZOC or outside that phase, this particular trigger is absent.
2. Test real supply and enemy combat entrant when no enemy combat defender
   remains: provisional specific-exception policy admits otherwise legal entry
   and invokes §32.13 immediately. Contrast a defender still present, noncombat
   entrant, and incomplete occupancy data. Do not silently apply the exception
   to those cases. Assert exact resource changes only after their policy exists.
3. Test two supply counters, supply plus friendly combat unit, and real/dummy
   mixtures: record competing interpretations of “alone,” discovery and
   resulting ordering; do not choose a result from adjacency or zero stacking
   value. A qualifying ZOC may already exist at phase start or arise during it.
4. Check 0/positive and positive/0 survive the §32.15 empty condition; 0/0
   meets it. Capture, consumption and destruction must not double subtract or
   duplicate resources on replay. Keep private identities, amounts, and event
   ordering out of opposing and intelligence views until authorized disclosure.

No runtime files changed; no gameplay tests run. Source questions and chosen
ordering require review before admitting executable capture behavior.
