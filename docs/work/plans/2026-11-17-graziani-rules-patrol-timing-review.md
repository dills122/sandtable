# Patrol timing: independent source review

**Disposition:** Source checks corroborate the companion [patrol timing note](2026-11-17-graziani-rules-patrol-timing.md)'s sequence and the competing readings. No source passage checked establishes a complete history-window or combat-participation policy. Evaluate the proposed alternatives explicitly before admission; this review selects none.

## Evidence

Visual inspection of [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), PDF and printed pp. 12 and 26–27, and all four pages of [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf). Before reading, verified SHA-256:

- Land rules `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`.
- Errata `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`.

On p. 12, first Operations Stage, player A completes F–L, including G movement/combat and L patrol, before player B undertakes F–L with the roles reversed. The next two stages repeat the first stage's facets. §6.14 explicitly describes both players' portions within an Operations Stage for CP accounting. The sequence places B's own phase-G decisions after A's patrol, although B can react and both players participate in combat steps during A's phase G. Thus the order supports a check at A's patrol boundary; it does not by itself settle whether §16.0 imposes a constraint across the entire future stage.

On p. 26, §16.0's general rule describes an Operations Stage in which no anti-armor or close assault combat **has taken place**. Specific §16.21 instead names a phasing player who does not engage in either against enemy units. Phase L on p. 12 summarizes the phasing player not having engaged in assault and does not mention anti-armor. These wordings support the companion note's distinct interpretations, not substitution of the abbreviated phase summary for §16.21. The checked text does not settle whether defending against another player's attack is engagement for §16.21. Section 17.25 uses capitalized `Engaged` as an outcome in a special morale case; this occurrence does not itself define the participant scope of §16.21's ordinary verb.

Section 16.21 allows a player to patrol despite artillery barrage during a given Operations Stage, but its next clause bars reconnaissance of a hex that **has undergone** any barrage or air bombardment. That target clause does not independently state a reset window or identify an attacker. Same-stage context is evidence for a bounded reading, not a printed explicit reset; retain the competing longer-history reading pending review. Section 16.22 separately specifies an enemy-occupied hex within five hexes and path restrictions. Section 16.17 additionally prohibits patrol origin or destination in rainstorm or sandstorm hexes. These qualifications do not decide the timing ambiguity.

Errata p. 2 §16.11 adds patrol point types. No explicit §16.0 or §16.21 correction was found in the four-page package; this is not proof of absence from other material.

## Consequences and limits

A future contract should distinguish stage, phase, attack type, initiator, participants and target history, preserving enough information for whichever interpretation is accepted. Check B defending in A's portion, both attack categories, target barrage in current and earlier stages, and A patrolling before B's later combat. Any privacy and deterministic-replay tests depend on the accepted rules and event schema. This separate reading followed the companion note and uses the same scans, so it is neither blind nor a new edition; hashes do not grant rights. No runtime changes, admission, or gameplay tests.
