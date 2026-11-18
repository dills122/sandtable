# Tobruk port follow-up: source review

**Date:** 2026-11-17
**Disposition:** No basis found to change the provisional 2/5 policy or admit a throughput formula. The cross-source conflict is material; retain the follow-up's separate scope and implementation gates.

## Findings

1. **Preserve the erratum's reciprocal threshold and magnitude when turning this into policy.** September 1979 errata §30.57 says that, for ports with incoming capacity above one stacking point, bringing in at least 50% of maximum supply tonnage reduces the SP level by at least one-third. It also says supplies have no effect for a one-SP maximum. The follow-up accurately flags this interaction but omits these qualifiers; its shorthand must not become an unconditional or precisely one-third rule. The printed phrase “at least” and how to round or apply that reduction need interpretation.
2. **Do not generalize the Tobruk example into actual initial throughput.** The erratum says each incoming SP reduces maximum stage tonnage 10%, outgoing SP have no effect, and illustrates 1,700 minus 170 as 1,530 at Tobruk. That arithmetic matches the follow-up. Its example does not decide starting efficiency, how efficiency affects the maximum used in the calculation, or whether the transfer rule applies to a particular shipment or Land-only mode. The chart's reported independence statement and the erratum's incoming transfer rule conflict in overlapping circumstances; their ultimate scope and precedence remain open.
3. **Keep setup facts distinct from a chosen reconciliation.** The reviewed follow-up preserves scenario §60.7's reported 7 and the chart's reported maximum 5 with a three-level obstruction as separate claims. The provisional 2/5 follows only if the latter two are selected together. No identified official correction establishes that reconciliation; no assumption should silently treat the example's 1,700 tons as the starting throughput.

## Evidence and limits

Reviewed [follow-up](2026-11-17-graziani-tobruk-port-followup.md), [prior independent reading](2026-11-17-graziani-tobruk-port-independent.md), and searchable text of [September 1979 errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf), PDF p. 2, §§30.5, 30.57–30.59. Errata §30.59 identifies the former chart reference as §55.3; §30.57 calls itself an important correction. The follow-up correctly leaves the interaction's relation to chart §55.3, §55.15, efficiency, rounding and Land-only §32.36 for later determination.

**Visual verification incomplete:** Browser PDF viewer returned a blank surface; web screenshot requests did not expose page images; direct local download failed DNS. The chart, scenario, Air/Logistics pages and errata were not visually inspected in this review. Their figures and wording above rely on the prior documented page reading and searchable errata extraction, respectively. This is an informed review, not a blind transcription, independent visual confirmation, complete errata census or reachability proof. No runtime changes or gameplay tests.

**Later parent-session check:** The available hash-matched local errata PDF was
rendered separately at 160 DPI. Visual inspection of PDF p. 2 corroborated
§30.57's incoming-SP 10% reduction, 1,700-to-1,530 example, 50% threshold and
“at least” one-third reciprocal reduction. This does not retroactively make
the independent review visual, verify the chart, or decide mode and precedence.
