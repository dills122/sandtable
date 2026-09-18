# 004B3 ordinary review — APPROVE

Reviewed 2026-09-18. Scope: four new combat-exercise-parent-evidence-v1 files,
packet004B B3 and existing paired Exercise contract semantics. Ordinary in-session
review; no formal independent-review pass. No open findings on frozen candidate.
Approval accepts B3 contract evidence only; C, parent004/checkpoint B and005 remain
separate acceptance work.

## Findings and five-axis assessment

- Correctness: exact materialization binds supported source, native provenance,
  B2 build/seed/config and caller-declared limit/fault/assertion. Every child is
  authenticated against that exact manifest before derived evidence/counts.
  Invalid/missing children contribute no trusted summary. Equal initial bytes,
  prefix/cut/capability, gameplay, terminal, bounds, clock policy, fault and
  expected failure are required for comparison; future reference/controller/order
  choices remain individually authenticated without seeding pair equality.
- Readability/quality: compact explicit schema and pure derived report helpers.
  First divergence uses actual semantic audience/action ID, retaining System
  fallback distinction. Deterministic first mismatch and nullable unavailable
  dependencies are explicit. Report readback regenerates all derived evidence.
- Architecture: uses accepted B1/B2 readers; no authority/gameplay change,
  runtime registration, artifact publication or predecessor edits. Parent
  failure remains failure even when child expected-failure assertion matches.
- Security/boundaries: canonical byte/type/field/size bounds, exact source pins,
  full source authentication before cache hits, complete immutable byte cache
  keys and defensive returned copies. Rehashed child/report forgeries are tested.
- Performance: selected native children run in one warm predecessor process;
  successful validations cached by full child/manifest/source bytes. Largest
  parent 23,998 bytes, within 8 MiB parent limit. Multi-record fixture size
  10,943,909 bytes does not claim to be one parent record.

Review found present-null payload classified as missing. Final membership-based
fix classifies absent key as missing and supplied null/non-bytes as invalid;
observed RED then two passing cases. Initial fixture mutation sanity checks were
replaced with actual exact-byte reader rejection for whitespace, float, boolean
and duplicate-key mutations. Both issues closed before freeze.

## Evidence and limits

Reviewed final implementation and tests statically without importing cold native
catalog. Author driver reloads current B3 module before final tests, retaining
unchanged predecessor caches. `004b3-author-resumed.log` records:

- Six focused groups: divergence6; null/absent2; 12 fresh actual child validations,
  10 parent cases and5 authenticated-prefix helper cases; 9 invalid children,
  1 missing and4 cache checks; 8 materialization rejects/10 binding mismatches;
  5 report forgeries/7 raw rejects/8 bounds plus hash/copy checks.
- Expected missing/stale fixture RED, generation, exact readback and4 actual
  fixture mutation rejects GREEN.
- B3_COMMAND_COMPLETED passed at 2026-09-18T23:31:04.076157Z,
  command elapsed138.722 seconds. Warm process remains idle for later work;
  this is completed command evidence, not a claimed interpreter exit.

Reviewer independently ran `shasum -a 256` over all four files (exit0), then
`python3 -B` standard-library-only artifact/log consistency check (exit0,
tool chunk b13b22): exact formatted fixture bytes,12 children/10 parents,
largest parent23,998 bytes, all4 actual hashes matching final GREEN log,
null2/fixture4 evidence and completed138.722-second command. Root separately
ran retained `004b3-root-literal-check.py`:10 parents,12 children,
19 validated entries,5 compared pairs,4 unavailable pairs,3 predecessor pins.

Unequal-length/null-arm coverage uses authenticated stream prefixes as algorithm
tests; no admitted unequal-length parent pair is claimed. Selected real cases
cover initial/limit/fault mismatch and absent initial evidence. Additional
seed/build/config/terminal/profile mismatches are PairKey helper tests. Prior
B2 full134-source/native evidence remains predecessor evidence and was not
rerun here. Full repository gate belongs to root's C integration checkpoint.

## Frozen SHA-256

| File under docs/specs | SHA-256 |
| --- | --- |
| combat-exercise-parent-evidence-v1.md | b1840ce112df387752413653e9bdac7cc4a3e66f707aa7f504358c89c4e23f0c |
| combat-exercise-parent-evidence-v1.schema.json | 00d1fcd2cb7d06b6f25d9224ff0839308f23fd2052b42dab24c98ea378b30820 |
| fixtures/combat-exercise-parent-evidence-v1.json | 08316dae3d4f12298c35bfe05f640dcd2559ae41cbf4144430cd52b121847a9f |
| verify-combat-exercise-parent-evidence-v1.py | bb0134c42293a18c8453513c9b23b13d4c365cb2734f8e93c2989d47773e7345 |
