# 1st CCNN HQ gun-presence policy: independent review

**Disposition: revise before accepting.** One unidentified artillery TOE point is a defensible, unit-specific interpretation of conflicting printed instructions, not an established source count. The proposal preserves that distinction and appropriately forbids deriving a usable weapon. Its claim that the affirmative reading is the “strongest” is a judgment, not a source fact; the checked material does not rank the competing interpretations. Acceptance would select a policy, not close the evidence gap or establish scenario readiness.

## Source check

- [Axis OA key](https://spigames.net/PDFv10/CNA_ChartsAxisPlayer.pdf), PDF p. 4, says `N` means a non-tank/non-gun unit possesses its characteristics ID's maximum TOE at arrival/deployment. The same key says tank and gun TOE points, even in other unit types, are always listed by number and weapon system. PDF p. 17 lists 1st CCNN division HQ as `g`, `N`, `D`, without such a listing. The adjacent Legion HQs' `1 × 65/17 Gun` entries apply to those separate units.
- PDF p. 26's characteristics ID `g` permits assignment of one artillery TOE point and shows maximum TOE 1. PDF p. 27 defines maximum as how many points the unit may contain. Reading `N` literally together with that maximum supports one present point; treating the omitted mandatory gun listing as material leaves presence unresolved. The source does not explicitly reconcile those instructions. Neither the maximum alone nor missing listing proves zero. The chart says artillery point; it does not name its system or independently resolve whether that point is a gun for the key's listing rule.
- [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf), PDF p. 1, §4.44b and §4.46, contains no specific correction for this HQ; extracted text of all four pages, including §60.31, yielded no specific starting count or weapon correction. Extraction can miss text, and this bounded check does not exclude later or other sources.

## Changes needed before policy selection

1. Replace “strongest affirmative reading” with “a literal affirmative reading” or explicitly identify ranking as project judgment. State directly that selection of **one present artillery TOE point** is inferred from `N` plus `g`, despite the unresolved listing conflict; unknown weapon identity does not make the count itself a printed fact.
2. Clarify “do not assign ... equipment class” alongside the selected **artillery** point: artillery is the chart's permitted TOE category, while specific weapon-system class and characteristics remain unknown. Avoid implying that source established a particular gun model, or that a generic gun is usable.
3. Specify acceptance and operational gate separately. A versioned initial presence interpretation must not clear readiness, make a legal action available, or default unknown characteristics to zero. Define fail-closed behavior for consumers needing unknown characteristics, including combat, movement, supply, losses, transfer and visibility; test each affected boundary. A single selected point can affect counts, capacity, targeting and replacement even without a firing action.
4. Before implementation, define canonical representation of source uncertainty versus selected policy, ownership and version binding, initial deployment scope, save migration/replay behavior, loss and replacement transitions, and how later contrary evidence changes new and existing campaigns. Verify deterministic replay and fog-of-war projection. Preserve unresolved state if representation cannot safely support these cases.

The proposal itself does not authorize a runtime change or override existing rulings. No claim of independent edition, exhaustive errata search, or new gameplay result follows from this review.

## Provenance

November 17, 2026: visually checked local Axis scan images for PDF pp. 4, 17, 26–27 and errata p. 1; searched extracted text of all four errata pages. Local PDF SHA-256: Axis `9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca`; errata `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`. Same scanned editions as prior reviews, not an independent publisher clarification. No runtime or data changes; no gameplay tests.
