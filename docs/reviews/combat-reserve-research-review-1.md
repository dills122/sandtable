# Combat and Reserve Research — Independent Review 1

**Review instance:** 1 of 3. **Verdict:** Not ready.

**Reviewed range:** `5cc55769224c0b73f27dd7bc41f8ea500371fc9e` through
`2c071b38e9a7e137eb8cb6ff3ff0f855c7dc6902`; three research commits, nine files.

Fresh reviewer had no inherited implementation conversation, inspected scope, code, source scans,
and probes, then sent a preliminary ledger before reading the author explanation. Review was
read-only. This retained report summarizes the returned independent findings and author response.

## Findings

1. **P2 — Invented +2 two-hex Retreat.** At the reviewed head,
   `docs/research/verify-combat-rng.py:77,108` and `combat-rng-golden-spike.md:102` assigned two-hex
   Retreat and victory RP to seed 1296. Common charts §15.79, PDF 4 starts two-hex Retreat at `+3`.
   The selected `+2` result instead has no Retreat and retains Engaged. The source error predates
   this branch in `CMB-RSH-001`, but the new executable golden entrenched it. Correct the helper,
   golden, and inherited requirement rather than changing the valid seed bytes.
2. **P2 — Incorrect moving guard capacity.** At the reviewed head,
   `docs/research/combat-mutable-state-spike.md:162` stated one guard per ten Prisoner Points.
   September errata §28.17 corrects the printed capacity to five. Six prisoners need two guards,
   affecting guard TOE transfers and escape. Correct the source claim and add a 5/6 boundary probe;
   the selected maximum-three prisoner fixture itself is unaffected.

Sources: [common charts](https://spigames.net/PDFv10/CNA_ChartsBothPlayers.pdf) and
[September errata](https://www.spigames.net/db_pages/ERR_CampaignforNorthAfrica.pdf).

## Plan Review

Research sequence and authority boundaries are sound. Source corrections are bounded; no heavy
architectural pivot required. Owner rulings and later production design remain explicit gates.

## Author-Claim Reconciliation

| Claim | Independent result |
| --- | --- |
| Research only, existing stream compatibility | Confirmed from diff, Core code and independent digests |
| Twelve source-backed golden outcomes | Contradicted for seed 1296 Retreat/RP |
| Source-backed custody requirements | Contradicted for moving guard capacity |
| Bounded, non-runtime helper coverage | Confirmed |
| Prior sixteen mutation checks and thirteen OpenSSL blocks | Historical author claim; temporary executions not independently verified in this pass |

## Verification Performed

All three documented Python commands passed at the reviewed head: mutable 8 vectors/144
combinations; RNG 12 vectors/6,480 coordinate domain; Reserve 7 release/8 CP/7 scope/4 offensive
cases. Diff check passed; four OpenSSL digest probes matched. A source-correct in-memory Retreat
probe failed the retained expectation. Relevant Land, Logistics, charts, and errata were visually
checked. Worktree stayed clean; no .NET gate was run because runtime was unchanged.

## Open Questions and Residual Risks

Full typed-table transcription, runtime integration, route/custody settlement, and fog/replay
proofs remain future work. Research closure requires fixing the two source errors first.

## Author Response and Recommended Next Actions

Both findings **accepted**. Correct the existing result/static research references, remove the
unsupported two-hex refusal envelope, retain seed 1296 as an Engaged/no-Retreat regression, and
normalize guard capacity with focused probes. Rerun all research verifiers and request a new fresh
pass over the corrected branch. Review budget remains two available instances; it is not reset.
