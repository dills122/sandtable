# B2 verification chronology

Status: final focused run and regenerated fixture PASS at2026-09-16 00:27:50UTC.
Final ordinary review and source reconciliation PASS; B2 accepted. Interrupted day-stop run remains historical
only; final fixture is now current. Prior full native run and final focused run are separate.

## Complete native execution at prior freeze

Author session25057 exited0 on2026-09-15 at18:15:41.723246UTC,822.895s.
Oracle hash3730e835087a83003d2893b6514aa2e1ff59fcdc1be808a3c765e67fe18d6538.
All134 sources:126 succeeded,8 expected failed prefixes,2442 accepted transitions,
44 owner-triggered System fallbacks,28 historical zero-execution endpoints.19 failure/forgery
checks,160 disposition mappings,4 isolated completion witnesses. Missing fixture RED observed.
Then-current fixture generated; superseded by final SourceSummary/12-golden layout.
See004b2-author-full-native-prior-freeze.log. This was not a full final-code rerun.

## Final-code applicability

Root AST/schema comparison preserved44 functions, all native/controller/reconstruction/proof
logic,15 existing schema definitions and163 accepted predecessor files. Changes limited to
admission/controller/reference-bound checks, actual rejected-attempt trustedClock, fixture
layout and their tests. See004b2-root-ast-compatibility.json and archived baseline inputs.
Final focused verification must cover all134 canonical admissions, changed failure branch,
representative native runs/readback and regenerated build/fixture bytes.

## Repository gate

Root just check exited0: format/build clean,81 boundary tests,1670 full tests,zero skipped.
Full-test duration3m06.093s. See004b2-just-check.log. No src/tests changes during subsequent
Python contract corrections; repeating unchanged native repository suite is unnecessary.

## Independent checks

Ordinary reviewer native session53304 exited0,251.569s: inherited release, custody fallback,
historical endpoint,4 failure profiles,6 semantic forgery rejects,4 provenance rejects,
5 fault-index rejects and5 malformed-byte rejects. Build-only session45749 exited0,4.552s.
Final static corrections reviewed; exact final fixture approval pending.

Native source auditor verified15 sources/209 transitions,53 owner outcomes,24 bridge receipts,
18 disposition mappings,26 proof digests,15 terminal/provenance pairs and2 historical boundaries.
All native assertions completed at335.307s; obsolete fixture wait deliberately interrupted,
exit130. No assertion failure. Final source-summary/literal reconciliation pending.

## Final resumed focused run — current oracle/schema

Author session48462 exit0. START2026-09-16T00:20:11.861038UTC;
END00:27:50.688248UTC.458.829s wall/409.957s CPU, including305.561s catalog authentication.
Reproducible driver:004b2-author-focused.py; full log:004b2-author-focused-resumed.log.
-134 authenticated sources;10 boundary checks.
-134 canonical admissions,2 unsupported configuration rejects,1 incomplete-reference overrun
  reject and1 completed-reference headroom successful native run.
-7 representative native executions and strict child readbacks.
-22 failure/forgery checks, including actual rejected trustedClock and defensive-copy behavior.
-6 SystemKey fields,9 native disposition mappings,4 isolated completion witnesses.
-Existing stale fixture RED observed before regeneration; final exact readback GREEN.
-Current fixture7,722,577 bytes,134 SourceSummary records,12 actual Child goldens.

Oracle a7385011125ca9105a1fbd3197c2b0b7b1825bf921e479ce9f953a4950343c92 and
schema cc8a7c9e89068b9a78f3abb4044230ccc9ef359665198aaa7ef6db5b15782bf6 unchanged.
Fixture5587cb17e53c48a9ab393532ebaa186f3d1384632eb13b105e1af8a698e799b6.

Root independent final checks PASS:134 summaries,169 embedded reference-event instances,
12 goldens,13 proofs,95 inventory entries,122 accepted steps and3 pins. Domain/BE-Int32 framing,
manifest/native provenance, actual copied rejection clock and proof-failure matrix checked.
Final applicability script rechecks163 predecessor hashes,44 function bodies,15 schema definitions
and exact reviewed code/schema hashes. See004b2-root-final-checks.log.

## Final review and acceptance

Ordinary reviewer result_v2_reviewer: scoped APPROVE, no actionable findings. Final literal
session31999 exit0,6.144s:134 summary matches,12 children,122 steps,13 proofs,36 native null Steps
authors,7 full-source digests,1 actual rejected clock,2 owner-triggered System fallbacks,3 pins
and4 isolated witnesses. All four frozen hashes checked before/after.
Source auditor: final reconciliation PASS;134 summaries,12 child identities,22 checkpoint cuts,
122 steps,13 proofs and7 full-source reference/event hashes independently checked. Prior209 native
transition audit remains applicable, separately attributed. No new cold native suite claimed.
Spec acceptance status is the only post-review change to four primary contract files. Source/
schema/fixture bytes unchanged; reviewed and accepted hashes both retained.
