# Graziani victory cross-reference: source check

**Disposition:** source evidence supports retaining [RUL-GRA-02](2026-11-12-graziani-ruling-decisions.md#rul-gra-02-victory-reference) as a planning interpretation. This is not an official correction, admission of the victory algorithm, or an independent edition. The paired Italian Campaign reference supplies a second internal consistency check, not independent external evidence.

## Source observations

- [Scenario scan](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf), PDF p. 3 / printed p. 27, §60.22 names Graziani’s Offensive, ends it after game turn 6, OpStage 3, and points to §60.71 for victory. Immediately below, §60.23 names the Italian Campaign, ends it after game turn 20, OpStage 3, and points to §60.72 for victory.
- Same scan, PDF p. 4 / printed p. 28: §60.7 is **Construction on the Map at the Start of the Game**, with a single unnumbered paragraph, not a victory conditions section or printed cases §60.71/§60.72. Section §60.8 is **First Scenario Group Victory Conditions**; its §60.81 heading names Graziani’s Offensive and §60.82 names the Italian Campaign. The two named headings correspond in order to §60.22 and §60.23, while both printed references differ by the same one-section offset.
- [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf), PDF p. 3, has entries §59.2, §59.45, §60.31, §60.32 and §60.45, then §61.38 and §61.41. Inspection of its four pages found no explicit §60.22, §60.23, §60.71, §60.72, §60.81 or §60.82 correction. This is limited to this errata package; absence is not proof that no other official correction exists.

## Interpretation and consequences

The existing choice to use §60.81 for the six-turn Graziani scenario is strongly supported by scenario name, victory heading, and the analogous §60.23/§60.82 pairing. Literal application of §60.71 has no corresponding printed subsection in the inspected §60.7; importing §60.82 would instead apply the named twenty-turn scenario's different scoring conditions. Neither is an equivalent silent substitution. Preserve the original incorrect pointer in provenance; do not present the correction as an official erratum.

This check does **not** establish route predicates, individual row quantifiers, terminal event ordering, result precedence, map coverage or privacy. Keep those gates in [victory supply review](2026-11-16-graziani-victory-supply-source-review.md) and [policy review](2026-11-17-graziani-victory-policy-review.md). Named heading and end boundary alone do not define an executable contract.

Affected future content: versioned Graziani scenario source reference and victory ruleset identity. Focused tests should bind six-turn Graziani to §60.81 rather than §60.82; bind the longer scenario separately if implemented; verify adjudication after turn 6, OpStage 3 at the separately accepted terminal boundary; and preserve fail-closed behavior while route/control evidence is unknown. These are proposed tests, not executed results.

## Provenance and limits

Visually inspected scenario PDF pp. 2–4 and errata PDF pp. 1–4 on November 17, 2026. SHA-256: scenario `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`; errata `5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb`. Copies match recorded source package hashes, which do not establish distribution rights or independent editions. No runtime, content, readiness or tests changed.
