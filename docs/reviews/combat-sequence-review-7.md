# Combat sequence independent review 7

Review instance: **7 of 7**. Date: 2026-09-06. Reviewer: fresh-context
`combat_sequence_review_7`, GPT-5.6 Sol, high reasoning. Blind preliminary findings preceded author
explanation and claim reconciliation. Retained [bootstrap](combat-sequence-review-7-bootstrap.md),
[author explanation](combat-sequence-review-7-author.md), and
[frozen manifest](combat-sequence-review-7-manifest.json).

## Findings

No actionable P0–P3 findings. No heavy pivot or owner decision gate.

## Plan review

Target matches accepted `003D1` scope: sequence/catalog5, cycle identity codec1, occurrence bytes,
Chronicle-prefix framing, fixtures, verifier, and plan status. Five-file cap and dependency order
preserved. Plan correctly keeps `003C2`, `003C3`, `003D2`, `004`, parent `003`, checkpoint B,
runtime admission, persistence, and outward privacy evidence open. No premature Rules10 or
playable-cycle claim.

## Author-claim reconciliation

| Author claim | Evidence | Status | Consequence |
| --- | --- | --- | --- |
| Catalog5 preserves112 positions and one interrupt, changing versions only | Literal predecessor/successor goldens and oracle transformation | Confirmed | Historical catalog bytes remain anchored |
| Six edges preserve same stage/slot and exact Movement→Breakdown→six Combat steps→Reserve Release→Truck Convoy order | All six fixture edges, catalog membership derivation, independent endpoint inspection | Confirmed | Accepted cycle structure satisfied |
| Authority/public tuples match accepted field order and privacy split | Composition design, schema, codec golden, binary preimages | Confirmed | Hidden prefix/config/version absent from public tuple |
| Opening prefix uses pre-event cut without recursion | Spec formulas, three retained prefix values, order/domain/fork checks | Confirmed | Accepted Chronicle framing satisfied |
| Occurrence binds cycle and permitted position without claiming lifecycle completion | Schema, occurrence golden, cross-cycle/cross-slot negatives | Confirmed | Correctly bounded structural identity |
| `admittedPolicyBundleDigest` binds003C1 configuration | Exact `3de30c…` match in rules-input and sequence fixtures | Confirmed |003C2 can assemble retained dependencies without placeholder Rules10 hash |
| Actual C# catalog4 capture matches fixture | Nine linked source files, all source hashes, capture execution and byte comparison | Confirmed | Predecessor anchor trustworthy |
| Capture build had zero warnings/errors | Author binlog exists and compiled executable runs; build not independently rerun | Unverified process detail | No readiness impact |
| TDD RED/GREEN history | Final repository cannot prove author workflow | Unverified process detail | No readiness impact |
| No runtime/admission/replay/privacy claim | Spec, plan, and verifier output | Confirmed | Scope remains honest |

## Verification performed

Manifest branch: `codex/cmb-task-003c-authority-envelopes`.
HEAD/base: `4c2bd033edb5ef994dd11bf7606d551e02b63f10`. Exactly one modified plan and four untracked
contract files; all five SHA-256 values matched before and after author review.

```text
python3 docs/specs/verify-combat-cycle-sequence-v1.py
PASS:112 positions,1 interrupt,6 edges,2 artifact and2 identity goldens,
38 mutations,3,996 identities,896 actor materializations.

python3 docs/specs/verify-combat-rules-inputs-v1.py
python3 docs/specs/verify-combat-world-settlement-v1.py
python3 docs/specs/verify-combat-creation-ledger-v1.py
python3 docs/specs/verify-combat-content-v7.py
PASS: all four predecessor oracles.

python3 docs/research/verify-combat-source-freeze.py
FAIL: default Python3.9; pre-existing verifier uses zip(strict=True).
/opt/homebrew/bin/python3 docs/research/verify-combat-source-freeze.py
PASS: Python3.14.6.

dotnet /tmp/cmb-sequence-capture/bin/Debug/net10.0/Capture.dll
FAIL: default PATH selected .NET9 without .NET10 runtime.
/opt/homebrew/bin/dotnet /tmp/cmb-sequence-capture/bin/Debug/net10.0/Capture.dll
PASS: emitted sha256:42b4e721252ddf5f1107927d12b3a7f0c7ed956b08c766a3b9e2d28e3601ab9c.

git diff --check
PASS
```

Captured catalog:52,310 bytes, byte-identical to retained predecessor. All nine linked source hashes
matched. Additional untracked-file trailing-whitespace scan clean. JSON and Python AST parsing
passed. All103 local links across changed plan/spec resolved. Independent catalog inspection
confirmed canonical root order, six same-slot edges, six Combat entries per edge, and exact source
union. Temporary capture/build files are local verification aids; literal catalog and source hashes
remain retained in fixture. No full solution test or production replay claim.

## Open questions and residual risks

- Real creation/event canonical readers and prefix cuts:003C2/003C3.
- Cycle transition atomicity, retry/readback, restoration, and full active-stage history:003C3/003D2.
- Candidate/action-set codecs and equal-visible-history admission behavior:004.
- Runtime admission, migration, Exercise terminals, replay, and paired evidence: later plan tasks.
- Default Python and .NET PATH selected older runtimes in review shell; explicit Homebrew runtimes
  completed affected checks.

## Verdict

**Ready** for contract-only003D1 and progression to003C2. Review budget exhausted at **7 of 7**.
No further review instance authorized or warranted.

## Author closeout

Accepted Ready verdict and no-findings result. No material fix required. Post-review edits limited to
plan status, navigation, and retained review evidence. Four contract artifacts remain byte-identical
to reviewed manifest; plan's reviewed hash identifies pre-closeout status. This closeout is not an
additional independent review. Next task003C2; parent gates remain open.
