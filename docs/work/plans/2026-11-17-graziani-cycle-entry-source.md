# Cycle entry and destruction retry: source boundary

**Finding:** Repeating the four segments is optional; §5.2.G does not make the first Movement and Combat Phase optional. Once a repetition is undertaken, it requires all four segments. Sources inspected do not define a separate declaration or cancellation procedure, establish that a repetition may be entered solely to retry destruction, or equate a repetition with §32.17's sub-segment. An actual no-movement combat is supported; an otherwise empty repetition remains unresolved.

## Evidence

- [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), printed/PDF p. 12 §5.2.G: Player A may repeat segments 1–4 as often as desired, subject to §8.2; each repetition must include all four. The four are Movement, Breakdown Determination, Combat and Reserve Release. Movement and reserve release are expressed as permissions; the breakdown segment directs checks for vehicles and motorized units of both sides. The sequence specifies required segments, not that each must contain a voluntary movement, attack, or release. An absence of movement and combat does not necessarily mean no procedural effects: breakdown checks may still occur.
- Printed p. 14 §8.22 describes movement followed by combat, then further movement and combat; it says the cycle repeats until the phasing player no longer wishes to move units or have combat. Combat awaits cessation of all movement in each sequence. This assigns the described termination decision to the phasing player, not automatically both players; Player B later undertakes phases F–L with roles exchanged. It describes that player's continued wish, not an explicit irrevocable commitment mechanism, declaration format, or permission to create an empty repetition for another purpose. Its wording alone does not prove whether a destruction attempt supplies an independent reason to repeat.
- Printed p. 14 §8.25 expressly allows repeated combat with neither involved unit moving, and combat when no units move, subject to ammunition and required CP. Thus no actual movement is required for such combat; a repetition containing eligible combat is distinguishable from a repetition containing no movement or combat. Neither provision expressly authorizes retrying a Supply Unit's destruction.
- Printed p. 43 §32.17 places the attempt before any movement in a given sub-segment of a Movement/Combat Segment and permits only one attempt per Supply Unit per sub-segment. It does not define that sub-segment as a four-segment repetition, identify a cycle-entry event, or address cancellation. A qualifying combat cycle therefore does not by itself establish a fresh retry.
- [September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf), checked as reported in [destruction sub-segment source note](2026-11-17-graziani-destruction-subsegment-source.md): §8.23 corrects an internal reference; no identified §8.22, §8.25 or §32.17 correction resolves these questions. Scope limited to this edition and inspected errata.

## Implication

See [independent cycle-entry review](2026-11-17-graziani-cycle-entry-source-review.md) for actor, first-phase and breakdown qualifications.

Any implementation must state its interpretation of sub-segment identity, how a repetition starts and completes, and whether cancellation or a repetition with no movement or combat is allowed. Neither an action request nor a declaration alone has source-established boundary effects. Avoid treating a no-movement combat example as proof that an empty cycle or destruction retry is legal. The source also does not establish that a cycle must contain an unrelated player action; requiring one would itself be a project policy.

## Provenance

Visually inspected Land pp. 12 and 14; §32.17 and errata cross-checked against linked source note and independent review. SHA-256: Land `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`; errata `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`. No runtime changes.
