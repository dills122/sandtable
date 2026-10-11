# Independent implementation review

Review instance: 1 of 9; recovery 0/2. Target HEAD 0f6becc712b592b988dae1891eb963820d86b9bb; base 94cf23883978f7fd9726f4bdd4f63677c6af6a9f.

## Blind preliminary concerns ledger

Recorded before reading author explanation. Bootstrap read first; independent code/spec/plan/tests inspection completed. All five primary delivery hashes and bootstrap/evidence hashes match. Diff contains exactly five primary and six administrative documents, no native/shared/workflow changes; only preexisting .serena/ untracked.

1. Confirm exact inherited-versus-new version error semantics: candidate own record versions explicitly reject003; nested ARE grammar delegates type checking but many leaf mutations merely assert rejection, not exact code. Need challenge against canonical promised error boundary.
2. Confirm ordered coverage independence: streaming comparison compares supplied records to the same catalogue generator; mechanics independently verifies facts and rounding but coverage cardinality/uniqueness alone would not catch coordinated generator-order drift. Frozen catalogue proof hash may supply necessary sensitivity.
3. Confirm preflight and at-use dependency009 survives inherited catch/remapping and warmed caches. Code has explicit correction, physical mock probes and final publication preflight; retained evidence needs reconciliation.
4. Confirm mandatory independent ledger primitive/capacity validation and ownership, genuine source full history, no fixture whitelist or fabricated old context in runtime APIs. Preliminary source inspection supports this.
5. Bound claims to arithmetic/no-cost private admission. Plan remains IN PROGRESS and flags false; paid/native/public/full loop deliberately not demonstrated.

No confirmed actionable finding at this preliminary stage. Retained log digests match all eight final gates and all historical records; these are verified retained evidence, not reviewer reruns.

## Findings

**[P2] Enforce or explicitly reconcile inherited proof error classification before claiming exact error boundaries.** `docs/specs/verify-combat-actual-assault-v1.py:64-75` checks versions only for the three owned records and delegates `ActualRoundProof` as a whole to unchanged ARE typing; `read_proof` at lines 244-250 then treats remaining inherited divergence as006. On authentic ingress and an otherwise canonical complete claim, changing `actualRoundProof.contractVersion` from1 to2, `actualRoundProof.control.contractVersion` from1 to2, or the inherited World version from7 to8 each produces `CMB-AAD-006 /proof`. Changing inherited `control.world.elements[0].components[0].currentToe` from10 to9223372036854775808 produces `CMB-AAD-001 /actualRoundProof`; the same oversized integer in `control.stateVersion` produces002. The accepted API addendum at line83 promises claim shape/types001, ranges002, version003, canonical008 before derived equality006, and author line35 repeats that claim. The final contract lines135-141 distinguishes primitive/version errors from valid coherent claim divergence and requires leaf/competing-error verification. These malformed-version/out-of-range claims are not coherent claims. ARE/PE delegation inherits version-as-integer grammar and collapses some primitive range errors to001. The recovery leaf loop (lines465-469) checks only that some Invalid is raised, so final-normal passes despite this inconsistency.

Impact is incorrect promised diagnostics/precedence for the new private reader, not an acceptance or payment bypass. Smallest credible correction: validate the inherited claim version/boundary rules at the candidate boundary without changing pinned predecessor bytes, preserving original version values per inherited type; preserve range002 distinctions for delegated primitives, and add exact-code tests for these nested examples plus competing malformed-claim overlays. If inherited claim diagnostics are intentionally generic, the coordinator must explicitly reconcile that narrower contract rather than retain the universal claim. No broad architecture pivot is necessary. This finding remains required for the currently promised acceptance criteria.

No other actionable defect established. The malformed claims still reject, capability flags stay false, and no debit, RNG advancement, settlement, public/native activation or additional authority is exposed.

## Plan Review

Task019F4 at `docs/design/combat-cycle-implementation-plan.md:2428` accurately limits delivery to a private admission-only checkpoint. The diff adds this task and the four new contract/oracle/fixture files; six administrative planning/review artifacts are consistent with the handoff. No predecessor/native/shared/test-project/workflow change occurred. Full DAY-C1 and Task017–019 remain open. Later paid consumption must independently certify actual result/settlement, including real World projection and typed identity compatibility, before debit. The planned work remains IN PROGRESS pending independent review/CI, appropriately avoiding premature closure. The error-boundary finding is a narrow implementation/test gap in that plan. Broader README/tech-design map lag is author-disclosed and predates this slice; coordinator owns its separately accepted reconciliation. Neither research Ready1/9 nor prior native review is implementation approval here.

## Author-Claim Reconciliation

| Author claim | Evidence | Status / consequence |
| --- | --- | --- |
| Authentic full ARE25 roots and both seal orders, independent ledgers, empty assault ingress | `admitted`, `ledgers`, ARE `read_source`/`replay`/`base`, focused admission four cases, unchanged ARE owning log | Confirmed within private trusted-context precondition. Full Request/Created/entry/selection/round histories are authenticated by unchanged predecessor, not a candidate fixture whitelist. |
| Owned results, immutable original World/RNG, no new paid state/cache | Public APIs derive and canonical-parse owned objects; recovery mutation/ownership source; focused admission, retained normal | Confirmed; cache is inherited and keys contain complete source and both complete ledgers. Candidate adds none. Empty assault list authenticates absence only. |
| Exact canonical grammar/error classification | `typed`, `parse`, `read_proof`, reviewer nested-claim probes | Partly contradicted by P2 finding. Own versions/type/capabilities work; inherited claim ranges/versions differ from promised error codes. |
| 59 runtime plus1 test-only physical paths, unchanged57 | Inventory, ARE map, trace exact-map comparison, all current physical hashes | Confirmed exact runtime/test maps; 3 owned trace artifacts remain outside external self-hash cycle. Trace notice correctly covers representative negatives, not exhaustive recovery. |
| Preflight and material-use009 through warm caches | `consume`, `verify_dependencies`, `admitted`, `normalized_rules`, 120 retained dependency probes; focused at-use I/O checks | Confirmed inspected correction and retained evidence. No transactional filesystem snapshot claimed. |
| 16616 indexed streamed arithmetic rows and independent rounding | `catalogue_records`, `catalogue_hash`, focused mechanics, independent reviewer ordered-index walk, unchanged World `result_facts`, Fraction checks | Confirmed all1296/6480/8840 rows and exact index ordering. Stream framing/domain and canonical record ordering inspected; independently computed catalogue digest matches frozen proof. No actual campaigns or settlement claimed. |
| Final normal/owning gates and historical failures accurately separated | Eight final logs/status records and evidence hashes; historical source hash map and RED logs | Confirmed retained evidence. All logs match manifest; final-normal before/after source SHA matches delivered Python. Earlier normal source differs and remains historical. Reviewer did not rerun heavy gates. |
| No new .NET gate, CI separate, four inherited failures unwaived | Actual diff, task/spec/author/evidence exclusions | Confirmed scope. Remote exact-head CI is coordinator-owned and not verified here; unexplained original native aggregation remains separate. |

## Verification Performed

Read-only author checkout `/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable`; branch `codex/actual-assault-contract`; HEAD `0f6becc712b592b988dae1891eb963820d86b9bb`; base `94cf23883978f7fd9726f4bdd4f63677c6af6a9f`. `git diff --check BASE...HEAD` exited0. `git status --short` contained only preexisting `?? .serena/`; preserved. Neutral bootstrap read first and preliminary concerns written before author explanation. Applicable independent-review and code-review-and-quality skills applied. Serena initial instructions read. Codebase Memory list_projects plus check_index_coverage confirmed excluded docs/not-tracked scope at generation2026-10-05; used focused source fallback, no graph completeness claim.

Verified all five primary SHA256 values, bootstrap SHA7f7f2d996281d198f13b4590d573d404857512e6cab087bafdff730dfc3cfb41 and evidence SHA2794efa6c13f65083b424f0c472b404ea8fda9612831f35061e4c838f1b657e8. Verified two unchanged planning inputs and all eight listed predecessor file hashes. Verified all eight final gate log hashes, all24 historical artifact hashes, consumption trace/log hashes, exact59 runtime/1test path hash-map equality, and current physical dependency bytes. Gate status records corroborate commands/exit0/no timeout/timings; original ARE status omits source/log hash fields, while its unchanged source and separate manifest log hash were independently checked. Retained log/status provenance supports reported execution; it is not direct reviewer reexecution.

Direct reviewer checks:

- `python3 -B docs/specs/verify-combat-actual-assault-v1.py --check-mechanics`: exit0; independently checked16616 records, stream forgeries, maxima157/235/581bytes,6813827 record-stream bytes.
- `python3 -B docs/specs/verify-combat-actual-assault-v1.py --check-admission`: exit0; ledger-shape001 and inherited/material009 probes; four genuine full proof/readbacks; maximum source109334, proof32853, selection3544, round3051bytes; container depth14. These shapes exhaust the currently admitted four prepared cases, not future paid histories.
- Ephemeral `python3 -B -` import/probe scripts: authentic ingress plus three inherited version mutations reproduced006; two own versions reproduced003; nested boolean version reproduced001; oversized inherited TOE reproduced001 while oversized control version reproduced002. No file mutation or fixture generation.
- Ephemeral independent ordered index walk: explicit attacker/defender/differential/capture/refusal loops and unchanged source-derived World triggers verified all16616 ordinals and record coordinates, then end-of-stream. Exit0.
- Fresh derived catalogue SHA256 `039b3f2282635f761006cf89b766b4a5ca5df6966ece8d0462aa46ae18b54bbb` exactly matches retained literal assessment.

Retained gates all exit0/no timeouts: original ARE1960.297s; final normal374.593s; RulesInput0.528s; source-freeze0.072s; RNG0.039s; actual-selection232.633s; positive-entry117.411s; World-settlement0.358s. Final normal records703 proof-leaf mutations,229 locally re-signed event leaves,120 dependency probes and four literal readbacks. No full normal/recovery/ARE/.NET heavy gate rerun, no regenerated fixture, remote CI action, fix, commit, publication or further reviewer spawn.

## Open Questions And Residual Risks

Exact-head CI and merge disposition belong to coordinator. Retained logs do not prove remote CI. Private ingress assumes authentic independently supplied context/ledgers; outward/public authentication is absent by design. Physical preflight/recheck is not atomic filesystem snapshotting. Fixed-source/table arithmetic paths share authoritative normalized data intentionally; independent Fraction rounding, old result_facts parity, explicit index walk, and frozen full-stream digest supply different failure sensitivities. Four inherited owning failures and the original unexplained native ExerciseRunner aggregation cause remain unwaived. Paid identity/settlement/guard/retreat/custody/windows/duties, Reserve/repeated Movement/later-II, Snapshot/Archives, Exercise/Runner and full loop are not certified.

## Verdict

**Not ready** against the currently promised exact error-boundary criteria. One bounded P2 finding; no heavy pivot. Review instance1/9 complete, recovery0/2 unchanged. This verdict assesses only the private admission implementation and its plan; it grants no broader C1/native/public/loop authority.

## Recommended Next Actions

Coordinator should obtain author response to the P2 finding, reconcile/correct owned claim diagnostics and exact-code regression coverage, refresh immutable evidence if implementation materially changes, and then decide whether another independent implementation instance is required. Preserve historical failures and no-cost scope. Separately verify exact-head CI and own merge disposition. No fixes performed by reviewer.
