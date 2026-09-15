# Draft PR115

Title: Add privacy-preserving Combat clock contracts

Private Combat seal timestamps previously changed whether an opponent’s unchanged proposal succeeded after a clock regression. Add an explicit v2 round/clock configuration whose acceptance floor stays at the published opening instant. Integrate authenticated side profiles that preserve identical observations, candidate identities and outcomes across private seals and clock faults.

Round-v2, Task004A1/A2 and result/settlement-v2 are accepted. Historical v1 contracts, all18 original audience traces and their failing clock diagnostic remain exact. Corrected cycle-finish composition, Reserve/cycle side projections, Exercise contracts and Task005 remain incomplete. This draft contains contract evidence only; no Combat runtime activation or merge requested.

Result-v2 authenticates native committed round evidence and opens each mandatory decision window independently. Earlier accepted timestamps remain audit evidence; each live window retains its fixed budget, deadline and deterministic fallback. The same-owner timing distinction is explicit. Side codec2 carries authenticated own settlement/custody/guard/replacement facts through the same causal history; System fallback creates no accepted player receipt.

## Validation

- Round-v2 oracle:12 semantic groups,10 traces,68 cuts,610 replay mutations,340 raw rejects,288 clock comparisons/retries,480 lifecycle retries.
- Side oracle:23 semantic groups,58 traces/466 cuts,207 submissions,969 mutations,580 raw rejects,69 receipt/stale bindings,672 clock comparisons/retries.
- Result-v2 oracle:10 semantic groups,32 causal traces/304 cuts,3728 mutations,1360 raw rejects,384 timing checks,200 same-owner comparisons. Root added192 paired outcomes; independent source audit verified784 literal records and11 pins.
- A2 side oracle:37 total groups;96 audience traces/2080 cuts,408 actions,7344 clock outcomes,5852 binding/candidate mutants,4872 retries,210 equal-history pairs and32 seal-order pairs. Independent source audit checked26 pins and628 observation literals. Root added240 fallback/retry outcomes and24 cache/source checks.
- Ordinary fresh-context reviews approved all four packets; source audits verified retained literals, pins and historical bytes.
- Root independently checked88 round outcomes and144 serialized side outcomes;17 legacy profile admission rejects. Historical v1 counterexample retained.
- Root `just check` at each accepted boundary: format/build clean,81 boundary tests,1670 full tests,0 skipped. Latest full-test duration3m11.782s.

Accepted behavior commits:3fcd822,8cb1cef,c4654cb and042edbc. Exact logs, scope limits, owner privacy disposition and renewed work deadline are retained under `.planning/2026-09-14-overnight-combat-wave-01/`.
