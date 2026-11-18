# RUL-GRA-27: independent audit

**Date:** November 18, 2026. **Disposition:** Agree with the bounded planning ruling; no blocking discrepancy found. It selects option A's literal §60.5 coordinate for planning, not a verified correction, historical location, or runtime admission.

## Evidence and scope

Visually checked the [scenario scan](https://spigames.net/PDFv10/CNA_Scenarios_DupFromAirRules.pdf), PDF p. 4 / printed p. 28: §60.5 lists Amiriya **E2212** in its initial Airfields table. PDF p. 5 / printed p. 29 separately lists Amiriya **E3212** among §61.33 Desert Fox airfields. The latter is competing scenario-specific evidence, not an identified correction to §60.5. The checked local scan's SHA-256 is `fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66`; this is the same scan used in the [policy review](2026-11-18-graziani-amiriya-coordinate-policy-review.md), not independent-edition corroboration.

The [ruling](2026-11-12-graziani-ruling-decisions.md#rul-gra-27-graziani-amiriya-coordinate-bounded-planning) preserves both source statements, treats E2212 as a planning precedence choice, and requires reevaluation if an applicable verified correction appears. This matches the policy review's bounded recommendation. The identity of the Map E mark remains unknown; neither its identity nor an airfield location was independently checked in this audit. No alias, facility movement, construction history, or other explanation for the discrepancy follows from these rows or the mark.

## Dependencies before admission

- Preserve scenario, edition and source provenance for both coordinates; document selected policy and ruleset/configuration version. Review any future correction or alternative interpretation explicitly, including affected setup, map and legal-action consumers, persistence, migration and replay compatibility.
- Establish relevant country and initial ownership independently, then applicable mode, control, use and access at the authoritative state. §60.5's Egypt ownership and control-based use wording is conditional; the coordinate itself establishes none of these facts or any legal action.
- Determine facility capacity and aircraft/HQ occupancy from applicable setup and mode rules, plus later changes from state; do not infer them from the facility name or other scenario.
- Resolve source rights before shipping source-derived material. Obtain independent review and focused deterministic checks for setup, affected consumers and replay before implementation or release.

Audit limited to the ruling, policy review and cited scenario pages. No runtime or data changes; no gameplay tests.
