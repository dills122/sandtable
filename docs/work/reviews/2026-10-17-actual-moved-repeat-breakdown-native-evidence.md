# R03 private native consumer evidence

## Boundary

The additive `CampaignCombatActualMovedRepeatBreakdownCompletion` Core consumer takes the original
CreationRequest, ten independent R02 ancestor ledgers, the frozen R02 Source, and an independent
R03 Breakdown ledger. It fully authenticates the R02 ordinal-two terminal before one trusted System
`complete-breakdown-segment` event. The event advances to adjacent Combat Position Determination;
it preserves World, resources, RNG, cycle and both Movement completion proofs. It neither executes
Combat nor exposes a public campaign action, host entry, persistence or intelligence input.

The frozen schema SHA-256 is `26909e67501f59a0d0892181b9c9499099552c721585369983c3dbde5784c955`;
verifier SHA-256 is `f79ca3b29c2a61c3aa8957f0e2873fc4968c8db85cd0e71183960e4489928bd3`.
The native codec copies all 108 schema runtime paths and hashes exactly, rejects a changed pin map
against frozen manifest `sha256:0052acb77da04ecbbee792a0cb330fd4d8189cca1513d5d450fbb716d265cfeb`,
and acquires the frozen schema and verifier as two additional delivery pins. The contract schema,
verifier and vectors were not changed by this native branch.

## Local parity and rejection

The compact native hash catalogue derives from the frozen full-vector gzip SHA-256
`e3274b3f9b23dee08bf7a68bf585f440d772855ce1ee631db6577fbc10eda378` and its
uncompressed transcript SHA-256 `c5403e56b1bb2829509a0f83024bf36d3548bc6a3b2564c46baeb043eb1f4dcc`.
It records all 300 authentic R02 terminals (36 paid, 264 armed), including before/after
Source/Event/Control/Proof hashes and receipt. Each native row checks the frozen hashes, exact
replay, duplicate retry, ordinal-two state and cumulative/local receipt equality.

- Initial unpartitioned Debug run: 300/300 passed in 11m50s.
- Final Release run: 300/300 passed in 8m58s; xUnit XML SHA-256
  `74e86dee0b6a44a33a67131c680ba61403fa1e580c47b8146483633b1781d7af`.
- Release focused acceptance/rejection: 7/7 passed in 15.7s; xUnit XML SHA-256
  `eec23f4d489ee076a39d5ee93388d600245d4555871789413e2ee8c330ad7a77`.
- Focused cases include nonterminal ancestry, wrong actor/capability, duplicate-key and numeric
  framing, pinned verifier mutation, forged event and forged Control/Proof readback.
- Release Core test DLL SHA-256 for these XML reports:
  `c307c80634f4682fdd9892d2aa40fbab7a071bd33fdc76fe42a06b24a1870e11`.

Exact commands and per-row timing provenance are in
[long-suite-weights.md](../../../.github/scripts/long-suite-weights.md). The initial local
`long_suite_partition_test.py` run inside the restricted sandbox failed a subprocess resource
check; the same unchanged 28-test suite passed outside the sandbox. No partition script or CI
workflow changed.

## Scheduling and outstanding gates

307 new Release-measured row identities extend the scheduling manifest from 2,912 to 3,219 rows;
194 existing classes stay byte-for-byte preserved and the two new classes bring it to 196. The
300-row class measured 536,031 ms summed row time and splits into five process groups. The seven
focused rows measured 14,550 ms. A single Release parity process was sampled at 442–601 MiB RSS;
this is not peak or hosted capacity proof.

Exact installed discovery, merged-main pin revalidation, fresh independent engineering review and
hosted CI are required before native acceptance. Public Combat, broader profiles, subsequent Combat
execution and full-loop closure remain outside this boundary.
