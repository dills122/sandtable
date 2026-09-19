# PR115 final metadata

Title: Add dormant Combat rules and privacy-safe evidence

URL: https://github.com/dills122/sandtable/pull/115

Branch: `codex/overnight-combat-wave-01`

Status: open, ready for review; preserve existing status. No merge.

## Summary
Add dormant C# selected Combat rules, pure dice adjudication and a strict RulesInput1 codec. Close contract checkpoint B with privacy-safe clock models, authenticated side projections and private child/parent Exercise evidence.

In the corrected reference contracts, admission uses the published opening instant; private seal timestamps remain audit evidence. Historical contract bytes stay intact.

## Changes
- Implement immutable source-verified tables and explicit-dice loss, retreat, capture and DP arithmetic. Preserve ordered dice coordinates and role-specific rounding.
- Reproduce the exact 30,395-byte RulesInput1 artifact and frozen hash. Normalize typed set arrays; reject noncanonical raw input, altered source/policy metadata and forged hashes. Artifact creation leaves Rules9 registration unchanged.
- Validate child evidence before parent counts or comparisons, and retain failed/unavailable results accurately. Reconcile all 72 requirements through 99 source pins, 51 reader witnesses and the original 28-trace handoff.

## Validation
- Final `just check`: formatting/build clean, zero warnings/errors; 81 boundary checks and 1,741 full tests passed, zero failures/skips.
- 19 new focused arithmetic tests cover all 36 morale/360 loss cells, three approved source gaps, and exhaustive morale/loss/capture combinations. All 52 codec tests pass, including exact bytes/hash, tampering and defensive copies.
- B3: 12 authenticated children and 10 parent cases. C: integrated readback, rejection and per-record capacity checks across 2,440 records. Ordinary reviews approved every slice.
- Six existing Rules/registration files remain byte-identical. Evidence and exact commands are retained under `.planning/2026-09-14-overnight-combat-wave-01/evidence/`. Earlier B2 full-native evidence and later focused verification retain their separate chronology.

## Scope
B3, Task004/checkpoint B and Task005 are complete. The new C# helpers are dormant: gameplay remains Rules9 through Combat entry. Task006 Content/scenario admission comes next; public Combat/cycle activation, hosted transport, durable artifact publication and Runner activation remain later work. Unequal-length parent divergence is helper-level evidence for the current closed source corpus. No merge is included.
