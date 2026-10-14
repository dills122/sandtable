# B00 author explanation and engineering self-review

Base ed443bdf2dfb5e6d8e3333364f52b49b9d939c83. Branch codex/ci-core-partition-capacity.
Read after the neutral bootstrap and preliminary independent assessment. This is author testimony,
not an independent verdict. Brain owns fresh review, hosted execution and merge.

## Problem and accepted decision

Successful PR183 Core-B took19m36s under20minutes; successful post-merge main took19m57s, leaving
three seconds. Core-A ranged9m10s to13m23s under15minutes, so transferring work there lacks a safe
measured margin. Exact XML/status/diagnostic identities across both runs show identical71A/2393B
rows with zero failure/skip/process error. Underlying host variability remains unmeasured.

Brain accepted the bounded proposal before implementation. Move the two native actual Movement
completion classes,64 rows, from B to a new C. Existing A predicate and eight mutation transfers
stay unchanged. B is the complement after A and C, rather than a row snapshot. Existing classes
and future methods in the selected two classes stay in C; unrelated future methods stay in B.
No existing deadline increases: B20, A/C/Exercise/Contracts15, prepare/verify15, dependency-review5.

The four-worker observed-order class model reproduces the original B wall within+1.13/+1.24%.
Measured C classes total918–954 summed seconds, dominated by one650–672second collection. Estimated
B jobs15m33–15m45 and C12m27–12m43 retain substantial margin versus prior three seconds. These are
projections based on retained costs/order/overhead, not hosted validation or future latency proof.
Extra VM setup/billing and a fifth required leaf are the deliberate operational costs. Another
cap increase, moving to variable A, changing class parallelism, diagnostics or physical checks,
and generic dynamic bin packing would widen scope or lack corresponding evidence.

## Walkthrough and traceability

BC01: proposal, extractor, schedule model and timing/row analysis retained under dated research/
reviews. Current and prior source hashes and successful status/counters are explicit. No old archives
were copied into this PR. Native runtime, test behavior, fixtures and packages are unchanged.
BC02: immutable b348aab genuine6policy RED fails only absent C partition/module after original A
preconditions,0.128s/120. No import/harness failure earns credit. Earlier diagnostic regex mistake
was corrected before timing claims; proposal explicitly records that failed analysis.
BC03: a475386 adds CORE_C_CLASSES and module map entry, subtracts C after A, adds B exclusions/C
inclusions, and incorporates C into prepared load/discovery/execution/full inventory. Workflow adds
one matrix entry. Version1 artifact structure remains a generic exact-SHA-bound shard map; new code
rejects old four-leaf payloads through exact module-set validation. No wire/public contract changed.
BC04: six focused GREEN0.123s;117 helper tests0.439s; actionlint0.184s; each bounded120. Tests cover
future selected/unrelated/similar class names, existing A transfer and filters, missing/stale/
foreign/process/overlapping C evidence, same-union C/B misassignment and actual filter mismatch.
Compiled Release discovery/provider expansion at committed a475386/b00-local took14.085s/120 and
proved71/2329/64/469/10=2943. A is exactly prior A; B∪C exactly prior2393B; B∩C empty. Native/source/
compiled fingerprints still match accepted C04; no test execution or heavy lease occurred.
BC05: this self-review plus neutral packet and frozen draft PR, then Brain independent review and
fresh hosted actual five-leaf2943 reconciliation. Local discovery is not execution acceptance.
BC06: new session09:10:17UTC closeout/09:55:17UTC stop; all retained work/evidence pushed and final
handoff. B02 waits merged B00 and explicit GO despite accepted B01 research. No scope expansion.

## Five-axis assessment

Correctness: inspected changed functions and tests first. Exact class membership is casefolded like
MTP filters; row names/method/module identities remain validated and collisions fail. A is computed
before C, preserving priority. Presence of both selected classes fails closed if policy becomes stale.
Actual compiled filters agree with predicates, and prepared load rejects rearranged same-union rows.
Aggregate verification still requires every leaf, exact SHA/attempt/SDK/config/module, process exit0,
unique passing XML rows/counters, owning dependency success and exhaustive disjoint identities.
Existing diagnostic/crash/report paths and no-assertion-bypass checks remain unchanged.
Readability: two named class selectors and one extra complementary set; narrow changes follow the
existing literal method/filter policy. No general expression evaluator, bin-packing engine or row
allowlist. Synthetic helper fixtures use distinct A/B/C names to preserve meaningful overlap checks.
Architecture: CI-only capacity change. No src/tests/.NET project/Release exporter/package/ruleset/
reference edits. Adding C requires MODULES-driven five-leaf discovery, load and verification together;
the workflow and artifact naming remain generic. No domain behavior or service added.
Security: static reviewed selectors and unchanged structured subprocess argv/shell=False. No secrets,
permissions or untrusted shell interpolation. Current-attempt/SHA bindings and stale aggregate rejection
remain; a missing new leaf cannot be hidden by other successful reports. Publication uses Keychain
outside sandbox without inspecting credentials.
Performance: explicit measured capacity migration, unchanged workload and caps. Class scheduling model
can be wrong on new VM resources/order; fresh CI is the experiment. C15 observed estimate has137–153sec
headroom; B20 retains255–267sec. New cases can erode margins; retain hosted timing receipts and diagnose
stalls/timeouts before any further cap change. No production performance claim.

## Scope notes and residual gates

Applied only the accepted narrow P3 roadmap wording from767fc37: actual ordinal2 Breakdown execution
is next, current MovementEndProof is emitted, public/production exclusions remain. No old binary
archives or preservation branch history was imported. Documentation-only, no additional runtime review.

Author finds no unresolved defect in the inspected CI delta. Fresh independent judgment and actual
hosted five-leaf2943 execution remain required; discovery/projections/previous four-leaf passes do not
substitute. Original raw run archives remain Brain-owned preservation, current extracted/hash/row/model
records are retained here. No broad local Core/solution/reference suite and no heavy lease was used.
Challenge the class predicates/actual filters, future membership, five-leaf acceptance binding and
projection assumptions across the whole scope rather than accepting this explanation as evidence.
