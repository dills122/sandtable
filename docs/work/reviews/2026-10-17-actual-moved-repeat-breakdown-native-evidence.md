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

307 new Release-measured R03 row identities extend the merged manifest from 2,916 to 3,223 rows;
195 existing classes stay intact and the two R03 classes bring it to 197. The
300-row class measured 536,031 ms summed row time and splits into five process groups. The seven
focused rows most recently measured 12,854 ms. A single Release parity process was sampled at 442–601 MiB RSS;
this is not peak or hosted capacity proof.

Exact-head installed discovery on review-fix commit `fe814c71b6196ae918e25bac15e2f928e118d65d`
found 3,223 ordinary Core rows and the preserved 325 R02 rows in 20 groups. All 300 parity,
seven focused R03 rows and four MAP-06 provisional rows were present; the manifest had zero
missing/extra row hashes. `make_plan` allocated every ordinary Core row once into 61 groups across
the unchanged 12 jobs, including five R03 parity groups of 59/61/60/60/60 rows. After rebase onto
main `f0266be`, all 108 runtime file hashes still matched the schema and native codec; frozen
schema/verifier SHA and delivery pins also matched. Fresh independent engineering review and hosted CI are required before native acceptance.
Public Combat, broader profiles, subsequent Combat execution and full-loop closure remain outside
this boundary.

## Independent review correction

Fresh review of PR #216 found that out-of-range numeric leaves under borrowed R02 Proof and
R03 Control could be remapped to shape error `CMB-MRB-001`; frozen verifier requires numeric
`CMB-MRB-002`. The native-only correction classifies six primitive numeric kinds locally and
walks borrowed Proof's R02 Base/Control grammar before invoking the predecessor reader. Regression
checks now include `futureObligations[].eligibleScope.gameTurn=116`, a borrowed Proof nested
`gameTurn` overflow, all six primitive overflows, a wrong-type float and JSON `-0`. The focused
Release class passes 7/7 on the rebased branch; latest XML SHA-256 is
`df8867becd7587b091fdf61fb29b5ee3dd46743965ab01febb5e35aee3e54e6b`.
The 300-row Release parity was completed on the earlier native head. The review correction changes
malformed numeric validation; representative valid terminal rows pass in the focused class after
that correction. Hosted CI on the updated head remains the full parity gate.

The later rebase onto main `f0266be` brought four MAP-06 provisional timing rows. The manifest
retains them, all 2,912 original baseline weights and the 307 R03 measured row identities. Frozen
R03 schema/verifier and all 108 physical runtime pins still match after this rebase.
