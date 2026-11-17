# Graziani rail scope: independent source review

**Disposition:** retain the source matrix as a credible bounded planning
inventory, not an admitted rail policy or reachability proof. Correct the
unconditional wording of two proposed vectors: the general/unit versus
supply-specific capacity interaction and the friendly-combat-unit ZOC case
remain open. This is a separate reading of the same edition, not an independent
edition or publisher ruling.

## Findings

1. **The opposing-direction negative vector needs a stated precedence.** Land
   §32.34 allows the Commonwealth any two real and/or dummy Supply Units as
   the total load in *one* direction for a given OpStage, subject to normal
   railroad rules. The candidate correctly identifies a single two-counter
   cap, not two real plus two dummy or two per direction. Land §8.72 also
   gives a general once-per-OpStage rail movement, one stack in each direction,
   of units and/or supplies. The candidate explicitly leaves their relationship
   unsettled, but its vector says to reject two opposing supply movements
   without that qualification. Rejecting the use of **one §32.34 allowance**
   in both directions follows the selected reading of that allowance; a
   blanket rejection of every possible one-each-direction supply arrangement
   presupposes that §32.34 constrains all uses of §8.72. Label the latter as a
   proposed precedence, or keep it unsupported pending a ruling. Likewise,
   distinguish a second independent rail action from two counters in one
   directed load; command batching and §8.72's one-stack rule need a contract.
   Owner-visible allowance errors must not disclose dummy identity.

2. **Do not turn any projected enemy ZOC into an unconditional rejection.**
   Section 8.73 says a traveling unit may not be in an enemy ZOC and may not
   enter enemy ZOC or an enemy-occupied hex while using rail movement.
   Section 10.26 says that, **for all movement purposes**, a friendly combat
   unit's presence in a hex negates the effect of enemy ZOC. The candidate
   correctly identifies their interaction as open, but its test list initially
   says to reject crossing an enemy-ZOC path without qualifying that case.
   Test an unoccupied projected ZOC separately from one containing a qualifying
   friendly combat unit at the relevant time; decide and version whether
   §10.26's movement exception applies to both origin and intermediate/
   destination rail hexes. A narrow §8.73-specific prohibition is another
   interpretation, not an established source correction. Friendly occupancy
   does not independently resolve the separate **enemy-occupied** prohibition;
   unknown incoming ZOC, unit classification or occupancy must not become
   clear by default. Preserve noninterference for hidden units and legal-action
   validation until authorized disclosure is defined.

3. **Stage and CP statements need their existing caution.** Section 8.72
   locates Commonwealth rail in its Rail Movement Phase, after that player's
   Movement/Combat and Truck Convoy Movement phases in the §5.2 player block.
   Player A/B assignments can change with initiative; rail is not intrinsically
   the first or second player's phase. Section 8.73 says the unit starts the
   OpStage on rail and may not have expended CP for another reason during that
   stage. The phrase's application to later CP spending, arrival placement,
   represented cargo and any non-movement action remains a source/ordering
   question; the candidate appropriately does not choose a blanket future
   restriction. Errata §8.73 says rail travelers may not **earn
   reorganization points** that stage. It does not explicitly forbid every
   reorganization action; specify how earlier organization and later rail
   selection interact without inventing retroactive undo or a broader ban.

4. **Initial railhead does not establish a whole network.** Scenario §60.7
   says the railroad runs to Mersa Matruh and ends there at setup. Land §8.71
   calls Alexandria–Mersa Matruh active; §24.67 treats unbuilt track as
   nonexistent, requires construction westward from the completed railhead
   and permits no skipped hexes. The candidate accurately separates the
   source-listed endpoint from certified hex-by-hex connection, condition and
   control. For precision, §8.71 calls the unused historical line
   **Soluch–Benghazi–Barce**, while §24.6 calls it Benghazi–Barce; the
   candidate's shorter label reproduces §24.6 but should not be taken as an
   exhaustive statement of §8.71's named extent. Neither name makes it active.
   Errata §8.71 refers to Axis use under §54.4 despite the original exclusive
   wording; it does not itself settle Axis rolling stock, control or Land-only
   abstraction and does not extend the Commonwealth supply allowance to Axis.

5. **Construction and repair remain distinct.** Section 24.61 names the 10th
   and 13th NZ railroad construction companies as the only builders of new
   track; §24.62 gives one company two OpStages per hex or both in the same hex
   one OpStage. Section 24.63 separately permits eligible engineers to rebuild
   up to three destroyed railroad hexes as specified. Section 24.65 forbids
   building or rebuilding enemy-controlled **or** enemy-occupied railroad
   hexes; friendly occupation does not automatically negate enemy control for
   that non-movement purpose. Section 24.64's one Store Point per built or
   rebuilt hex must be reconciled with §32.11 before declaring Land-only
   construction free, forbidden or paid. The candidate correctly leaves
   arrival, resources, paths and six-turn reachability open.

## Distinguishing checks

After accepting specific rules, test `2 real`, `1 real + 1 dummy`, `2 dummy`
and `3` in one directed load; separately test one supply counter in each
direction under each proposed §32.34/§8.72 precedence. Include ordinary cargo
in the same and opposite directions only when stack semantics are defined.
For ZOC, test both a qualifying friendly combat occupant and none at origin,
intermediate and destination, plus enemy occupancy, unknown projection and
unknown incoming map coverage. Check both A/B assignments, pre-rail CP,
post-rail attempted CP use, prior reorganization points, stage rollover,
continuity and replay. An unbuilt or destroyed rail segment is distinct from
a known adjacent but unconnected hex. These are synthetic tests, not executed
results or evidence of a legal historical route.

## Evidence and scope

Visually rechecked [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf)
printed/PDF pp. 12, 15, 18, 37 and 43, §§5.2, 8.71–.73, 10.26,
24.61–.67 and 32.34 (SHA-256
`b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`);
[scenario](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf)
PDF p. 4 / printed p. 28 §60.7
(`fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`);
and [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf)
PDF p. 1 §§8.71 and 8.73
(`5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`).
Compared [candidate](2026-11-17-graziani-rail-scope-next.md). Detailed
Axis rail, all related errata, source map, rail company arrivals and actual
reachability were not exhaustively checked. No runtime changes or gameplay
tests.
