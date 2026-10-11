# Independent implementation review 2

Review instance: 2 of 9; round1 pass2; recovery0/2. Research scope review remains separate. Target: codex/actual-assault-contract at ddddbb57c2926e0b38e528ae5a43a604ccf73957, overall base94cf23883978f7fd9726f4bdd4f63677c6af6a9f; correction base0f6becc712b592b988dae1891eb963820d86b9bb.

## Blind preliminary ledger

Recorded before opening author.md or review-1-response.md. Bootstrap/evidence, target spec/inventory/fixture, Task019F4, full candidate oracle, predecessor grammar routines and resolved overall/correction diffs inspected. Dispatch disclosed correction themes but did not provide author's rationale. Neutral canonical plan necessarily mentions prior finding; a wholly uninformed prior-finding pass was impossible, but author testimony remained unread.

- Scope verified: five primary files plus eight administrative context files; no native/predecessor/workflow change. HEAD/branch correct; only pre-existing untracked .serena/ remains. Five-primary, bootstrap and manifest hashes match dispatch. All current/carried gate logs and correction-history hashes checked so far match manifest.
- No confirmed defect yet. Recursive PE namespace inheritance, special-arm dispatch, per-type version rules and numeric bounds merit targeted adversarial checks; missing/extra version inventory would reject valid proof at parse and needs reachability check.
- Full-source/independent-ledger authority flow uses ARE public read_source/proof, not fixture whitelist. Empty assault list forbids payment; capability flags invariant false. Need challenge independent ledger ordering and candidate ownership, not infer authentication from fixture equality alone.
- Catalogue generator is streamed and independent Fraction/world facts tests exist; expected 16616 exact rows and counts are consistent. Need execute focused mechanics and check retained literals independently.
- Dependency closure physical preflight includes full ARE57 plus oracle/schema59; test-only ARE fixture separately pinned. Need reconcile trace observation versus pin-only paths and at-use cache protection.
- Existing owning failures, full paid/native/public/full-loop scope and prior-head CI remain explicitly excluded; private checkpoint can be assessed without granting broader activation. Corrected exact-head CI belongs to Brain.
- Codebase-memory list/coverage consulted: docs subtree excluded/not tracked. Focused source reads used; Serena active project differs from target so no mutation or project reconfiguration performed.

## Findings

**[P2] Finish recursive diagnostics through inherited union arms.** At `docs/specs/verify-combat-actual-assault-v1.py:95`, `LifecycleFlow` is delegated to `are.selection.pe.typed` rather than dispatched to its declared arm and recursively checked by the candidate. PE/lifecycle wrappers collapse underlying errors to001. Consequently the correction fixes the ordinary inherited records but still violates the new claim bounds contract for a reachable union arm.

Reproduced on authentic Axis/attacker-first source with both retained independent ledgers and empty assault ledger. Replace only `actualRoundProof.base.actualSelectionProof.positiveEntryProof.entry.breakdownFlow` in the supplied claim with a complete `phasing-stop` arm. The valid typed arm parses successfully and the authenticated public `read_proof` returns006, as expected for a coherent divergent claim. Change only its `stop.recordedStateVersion` to9223372036854775808 (2**63): both `parse` and the same public readback return `CMB-AAD-001` at the breakdownFlow path, instead of the promised primitive-bound002. Lower overflow −9223372036854775809 likewise returns001. A `{'kind':'bogus'}` flow also returns001 rather than the declared family003; the numeric example alone establishes the finding.

The accepted contract at lines139–145 and addendum's final error proposal require primitive002 before canonical008/equality006. Preserving PE signed domains means the valid interval is −2**63 through2**63−1; the tested overflow is outside either signed or nonnegative interpretation. This is the same bounded diagnostic category as review1, left incomplete on the special-arm path, not an acceptance/payment bypass.

`grammar_nodes` at lines318–329 only traverses schema records and arrays. `LifecycleFlow` is not in `pe.SCHEMA`; its arm table lives in `pe.lc.INVENTORY['unions']`. The walker stops there, and all four genuine prepared proofs contain `{'kind':'idle'}`, so the passing matrix never reaches `LifecycleStop.recordedStateVersion` or `MovingRoute.firstMoveStateVersion`. The claim that the matrix walks the complete declared inherited grammar is therefore too broad. All26 version fields actually present in the first frozen proof are walked; this finding concerns declared alternate arms, not missing baseline version fields.

Smallest credible correction: preserve the original PE/lifecycle union namespace, dispatch its declared tag into the correct arm, and recurse with candidate primitive/version diagnostics. Add exact-code tests for complete shaped union alternatives and their bounds/type/tag errors, including authenticated public readback and relevant precedence overlays. Keep all pinned predecessor bytes, fixture literals, authority exclusions and dependency maps unchanged. If the intended contract excludes these arms earlier, explicitly freeze that narrower grammar and rejection rule rather than leaving typed alternatives accepted and malformed alternatives generically001. No heavy pivot or separate workstream is needed.

No other actionable defect established. The original reported ordinary-record versions and integer overflows now classify correctly in the focused matrix. The remaining finding does not grant paid/native/public/settlement authority.

### Minimal reproduction shape

Build the supplied flow in this order, then insert it into an otherwise frozen complete proof and encode with compact ASCII JSON:

```python
flow = {
    'kind': 'phasing-stop',
    'stop': {
        'stopId': 'sha256:' + '0' * 64,
        'recordedStateVersion': 1,
        'route': {
            'routeId': 'sha256:' + '0' * 64,
            'firstMoveStateVersion': 1,
            'elementId': 'x', 'representationId': 'x', 'owner': 'axis',
            'originLocationId': 'x', 'currentLocationId': 'x', 'cohortIds': []
        },
        'reason': 'x', 'weatherKind': 'x', 'cohortInputs': []
    }
}
# Valid shape: parse accepts; authenticated read_proof -> CMB-AAD-006.
flow['stop']['recordedStateVersion'] = 2**63
# Same readers -> CMB-AAD-001; contract requires CMB-AAD-002.
```

## Plan Review

Task019F4 accurately describes a private admission prerequisite and remains IN PROGRESS pending independent review/CI. Its implementation uses unchanged public ARE source/proof authentication over full history and independently retained selection/round ledgers; the assault ledger authenticates absence only. It creates no paid commands/events, cost, draw, commitment/result/settlement, synthetic old context, cache or native activation. The later paid consumer must independently certify complete actual outcome/settlement before debit. Deferred World projection and typed result/settlement identity compatibility are material and explicitly retained.

The five-primary ownership and59 runtime/1 test-only closure match the final inventory and frozen planning addendum. Initial66-path proposal is historical; all57 inherited pins are preserved plus ARE oracle/schema. Physical consumption includes hash enforcement, not57 claimed executable imports. Three owned code/schema/fixture paths remain outside the external self-hash cycle. No unknown acquired path appears in the retained trace, and the exact role/hash maps match the executable inventory. Candidate runtime APIs do not use the ARE test fixture as a whitelist or caller-ledger replacement.

The new oracle follows neighboring compact inline-test reference organization. Half its source is verification, and the large fixture dominates added bytes; a framework/shared refactor is unwarranted for this slice. Recordwise catalogue hashing bounds working memory. Repeated derivation is deliberate and does not add a mutable cache. Delivery count is13 files: five primary plus eight administrative documents, no unrelated native/test-project/workflow edits. The diagnostic correction remains incomplete under the current acceptance contract. Broader project-map lag is author-disclosed and coordinator-owned; it was not treated as a new activation/readiness claim or silently reconciled by this reviewer.

## Author-Claim Reconciliation

| Author claim | Evidence inspected | Status / consequence |
| --- | --- | --- |
| Ordinary inherited versions/ranges corrected, including Position5 and World7 | Candidate recursive validator/inventory; direct focused admission exact-code matrix | Confirmed for actual baseline fields. Original review1 examples are addressed. |
| Complete declared grammar and special-arm diagnostics covered | `typed:95`, `grammar_nodes:318`, PE/lifecycle union table, direct alternate-arm parse/public probes | Contradicted by P2. Walker stops at LifecycleFlow; alternate numeric descendants still collapse to001. |
| Four authentic ARE25 source combinations, independent ledgers, strict empty assault | Candidate `admitted`/`ledgers`, unchanged ARE public readers, four direct admission/readbacks | Confirmed within private caller-authentication precondition; no public authentication implied. |
| Unchanged World/RNG, owned outputs/inputs, no new payment or cache | Admission boundary and canonical ownership source; focused admission; retained recovery and normal evidence | Confirmed inspected behavior and retained evidence. Reviewer did not rerun full recovery. |
| Exact16616 streamed rows, independent facts/rounding/conservation | Direct focused mechanics; additional independent ordered coordinate/index walk and SHA framing | Confirmed1296/6480/8840, complete end-of-stream, literal digest039b3f2282635f761006cf89b766b4a5ca5df6966ece8d0462aa46ae18b54bbb. Pure arithmetic only. |
| Runtime59/test1 with unchanged inherited57 | Inventory, ARE map, exact retained trace role maps, all current physical hashes | Confirmed; own three paths excluded deliberately. Trace covers representative negatives, not every recovery branch. |
| Preflight/material-use dependency009 survives inherited caches | `consume`, `admitted`, `normalized_rules`, focused at-use checks and retained120 dependency probes | Confirmed by source plus scoped execution/retained evidence; no transactional filesystem snapshot claimed. |
| Current correction normal/focused gates distinct from carried owning and old CI | Log hashes, matching status records, source before/after identity, prior packet hash, evidence manifest | Confirmed retained provenance. Brain-authorized carry remains carry; no reviewer heavy rerun or current CI conclusion. |
| Full paid/native/public/loop work remains open and owning failures unwaived | Spec, Task019F4, packet/evidence exclusions, actual diff | Confirmed. Research Ready1/9 and old-head CI are not current implementation approval. |

## Verification Performed

Read-only author target `/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable`, branch/head/base as above. `git diff --check 94cf23883978f7fd9726f4bdd4f63677c6af6a9f HEAD` exited0. Final status remains only pre-existing `?? .serena/`; HEAD unchanged. Only this report was written by reviewer. No fixes, staging, commits, publication, spawning, project reconfiguration or remote CI action.

Applied independent-review and code-review-and-quality. Read Serena initial instructions. Applied codebase-memory list_projects/coverage; exact target indexed identity exists but docs subtree is excluded/not tracked at generation2026-10-05. Used focused source/schema reads as instructed; no graph completeness claim. A read-rate hook blocked one batched read and explicitly allowed retry after resetting its counter; no permission or safety bypass occurred. A process-inspection command was sandbox-denied; unrelated to evidence or test execution and not retried with escalation.

Verified bootstrap SHA b5d0f224550934c17d06bf4817a9fdc753b31583c15c0c9f49cc38667d44e71e and manifest SHA28450003da51f13e4f05f0ec5bb33ce34382552f63b8c3b159e593693d112d4d. All five primary delivery hashes matched manifest before and after probes; candidate Python SHA41b1d3ea40ebd8dde4c031ec74434c0177d204ef698b6a92b000d5ca940f759e. Verified all eight listed unchanged predecessor/native/test-project hashes, both initial planning hashes, all60 current external physical hashes, inherited exact57 map inclusion,59/1 trace role/hash equality,3-owned trace identity, trace/log hash, separate research note/review hashes, verbatim review1 hash, prior packet evidence SHA from Git and prior-head CI receipt hash.

Verified three current gate logs, seven carried owning logs, all ten listed correction-history log/status hashes and their matching status data. Current gate status before/after source SHA equals delivered candidate source; no timeout. Retained current times: admission56.169s, mechanics3.956s, complete normal370.078s. The retained normal contains152 version/2936 integer/772 public/228 overlay/4 dependency exact-code probes, four genuine readbacks,16616 mechanics rows,703 proof leaves,229 re-signed event leaves,120 dependency probes and four literal readbacks. This is verified retained execution evidence, not a direct reviewer full-normal run.

Carried unchanged gates remain explicitly separated: original ARE1960.297s, RulesInput0.528s, source freeze0.072s, RNG0.039s, actual selection232.633s, positive entry117.411s, World settlement0.358s; all retained exit0/no timeout. Original ARE status lacks source-before/after hashes, so checked unchanged oracle/map and separate log digest without inventing missing metadata. Historical review1 RED and intermediate correction failures remain failures, not passes. Prior CI source0f6becc/tested merge081516df/run38101027022 supports old head only.

Direct reviewer execution, all read-only with bytecode disabled:

- `/opt/homebrew/opt/python@3.14/bin/python3.14 -B docs/specs/verify-combat-actual-assault-v1.py --check-admission`: exit0. Same exact-code counts and four proofs; maximum source109334/proof32853/selection3544/round3051 bytes, container depth14. This exhausts current four-source shapes, not future paid histories.
- Same command with `--check-mechanics`: exit0.16616 rows, coverage forgeries, maxima157/235/581 bytes; record JSONL6813827 bytes.
- Ephemeral Python import/probe: inspected every actual baseline version key against walker;26/26 present keys walked. Identified special leaf LifecycleFlow and reproduced valid alternate arm006 versus overflow001/unknown tag001 through both parse and authenticated public read_proof. Lower overflow/bool types additionally inspected through parse.
- Ephemeral independent ordered index walk: explicit coordinate/differential/capture-die/refusal loops using unchanged World facts verified all16616 row ordinals, section/index keys, declared record order and exact end-of-stream. Separate literal catalogue-domain/header/LF SHA framing produced039b3f2282635f761006cf89b766b4a5ca5df6966ece8d0462aa46ae18b54bbb, matching frozen proof.

No full normal/recovery/ARE/.NET gate rerun. No golden generation. Corrected target CI is ongoing under Brain and not inspected or certified here.

## Open Questions And Residual Risks

Private ingress still assumes authentic independently supplied context/ledgers. Physical checks are boundary checks rather than atomic filesystem snapshots. Pinned helper coupling requires deliberate contract/pin reconciliation for future changes. Shared normalized source facts are intentionally used, with different rounding/order/identity checks providing independent sensitivities. Exact diagnostic coverage must include declared union alternatives rather than only leaves present in the four genuine prepared proofs.

Four inherited owning failures remain unwaived (Breakdown/cycle-sequence/inherited Snapshot LandSequence and outward Content composition), and original native ExerciseRunner aggregation cause remains unexplained. No new paid/native/public/loop inference follows from this report. Costs/results/settlement, guard/retreat/custody/windows/duties, Reserve/repeated Movement/later-II/consumed lineage, Snapshot/Archives and Exercise/Runner remain separately gated. Brain owns current CI and merge.

## Verdict

**Not ready.** One bounded P2 diagnostic/test-sensitivity finding remains under the frozen contract; no heavy pivot. Implementation review2/9, round1 pass2 complete; recovery0/2 unchanged. Research review remains separate. This verdict covers private admission implementation and plan only.

## Recommended Next Actions

Return finding to author for Accept / evidence-based Dispute / explicit Defer disposition through Brain. Complete union-aware owned validation and exact-code tests, or explicitly reconcile the narrower contract if that is intended. Refresh immutable evidence after material correction, preserve prior failures and external pins, and let Brain decide subsequent fresh review within9-instance limit. Do not reset counters. Current CI/merge disposition remains Brain-owned; broader activation blockers remain unwaived. Reviewer performed no fix.
