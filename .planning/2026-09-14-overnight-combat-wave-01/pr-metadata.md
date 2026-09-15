# Draft PR115 metadata

Title: Add privacy-preserving Combat clock contracts

Private Combat seal timestamps previously changed whether an opponent’s unchanged proposal succeeded after a clock regression. Add an explicit v2 round/clock configuration whose acceptance floor stays at the published opening instant. Integrate authenticated side profiles that preserve identical observations, candidate identities and outcomes across private seals and clock faults.

Round-v2, Task004A1/A2/A3a/A3b, result/settlement-v2 and native cycle-finish composition are accepted. Historical v1 contracts, all18 original audience traces and their failing clock diagnostic remain exact. Exact historical terminal and continuous corrected bridge side projections complete bounded CON-005 evidence. Private Exercise occurrence checkpoints (B1) are accepted. Child/parent Exercise evidence, integrated checkpoint B and Task005 remain incomplete. This draft contains contract evidence only; no Combat runtime activation or merge requested.

Result-v2 authenticates native committed round evidence and opens each mandatory decision window independently. Earlier accepted timestamps remain audit evidence; each live window retains its fixed budget, deadline and deterministic fallback. The same-owner timing distinction is explicit. Side codec2 carries authenticated own settlement/custody/guard/replacement facts through the same causal history; System fallback creates no accepted player receipt.

## Validation

- Round-v2 oracle:12 semantic groups,10 traces,68 cuts,610 replay mutations,340 raw rejects,288 clock comparisons/retries,480 lifecycle retries.
- Side oracle:23 semantic groups,58 traces/466 cuts,207 submissions,969 mutations,580 raw rejects,69 receipt/stale bindings,672 clock comparisons/retries.
- Result-v2 oracle:10 semantic groups,32 causal traces/304 cuts,3728 mutations,1360 raw rejects,384 timing checks,200 same-owner comparisons. Root added192 paired outcomes; independent source audit verified784 literal records and11 pins.
- A2 side oracle:37 total groups;96 audience traces/2080 cuts,408 actions,7344 clock outcomes,5852 binding/candidate mutants,4872 retries,210 equal-history pairs and32 seal-order pairs. Independent source audit checked26 pins and628 observation literals. Root added240 fallback/retry outcomes and24 cache/source checks.
- Native cycle-finish bridge:11 groups,32 lineages/160 cuts/128 retries,192 prior-time comparisons and24 completed-source fallback rejects. Root independently checked1088 clock/recovery outcomes; source audit verified14 pins,320 nested literals and128 suffix transitions retaining exact World/RNG/future obligations.
- A3a side oracle:52 total groups,104 live cuts,112 ledger traces,1140 clock outcomes,472 retries,96 stale rejections. Root reconstructed92 binary sets/190 action identities; source audit checked456 cuts,344 observation literals,190 candidate pairs and38 pins. Accepted A1/A2 bytes preserved.
- A3b side oracle:62 total groups;116 sources,4744 cuts,1208 literal observations,720 candidate pairs. Native audit checked all28 terminal profiles, including both releaseMember completion records. Root checked32 native fallback pairs,320 clock outcomes,64 retries and56 terminal admission rejects. A1/A2/A3a bytes preserved.
- B1 occurrence oracle: 134 sources, 2576 cuts, 192 dual schedules, 352 full-World bridge checks, 67 rejection checks and 5 exact-byte fixture mutants. Native source audit verified every cut; root independently reconstructed hash domains and 2576 fixture bindings.
- Ordinary fresh-context reviews approved all eight packets; source audits verified retained literals, pins and historical bytes.
- Root independently checked88 round outcomes and144 serialized side outcomes;17 legacy profile admission rejects. Historical v1 counterexample retained.
- Root `just check` at each accepted boundary: format/build clean,81 boundary tests,1670 full tests,0 skipped. Latest full-test duration3m13.738s.

Accepted behavior commits:3fcd822,8cb1cef,c4654cb,042edbc b5ede50,a4f5b2c b33f69c and ac9baef. Exact logs, scope limits, owner privacy disposition and renewed work deadline are retained under `.planning/2026-09-14-overnight-combat-wave-01/`.
