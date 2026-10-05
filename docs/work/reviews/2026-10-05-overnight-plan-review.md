Historical N0 plan review, retained unchanged below. Its launch-baseline P3 was corrected before dispatch; later milestone acceptance belongs to the session handoff and N4 review.

N0 independent plan review — Review instance: set 1, pass 1 of 3; total 1 of 9. Verdict: Ready with non-blocking follow-ups.

## Findings

P3 — Refresh the historical launch baseline before copying it into child bootstraps.
Evidence: /Users/dsteele/repos/sandtable/.planning/combat-overnight/session-plan.md:12–14 says primary remains f33c78c and launch must fast-forward. The same document:3 marks the session launched. Independent git rev-parse HEAD returned 907f41403ed159d65024f193f3e1f730a23b9bbb on main; the supplied bootstrap and plan:11 agree with that current baseline.
Scenario/impact: copying the stale paragraph into a child bootstrap could misidentify its predecessor or prompt redundant synchronization. The N1 dependency itself is correct and this does not block dispatch from verified current main.
Smallest correction: label lines12–14 as pre-launch observations, then record successful launch synchronization to907f414. Coordinator owns this administrative correction; no product behavior change or new review instance is needed.

No actionable blocking plan finding. No implementation correctness verdict is issued because this target contains no code diff.

## Plan Review

Ground truth inspected: latest primary AGENTS.md; docs/roadmap/pre-alpha-roadmap.md:38–66; docs/design/combat-cycle-implementation-plan.md:933–974 and1892–1970; docs/design/combat-cycle-post-movement-dispatch.md:118–153; complete docs/specs/combat-settled-control-v1.md; newest settled-control and native-settled-continuation handoffs.

N1 matches the accepted five-path native manifest exactly (settled-control engine/models, codec, focused tests, test-project fixture links, canonical plan). Plan:89–97 requires complete019D1 packet admission,32+4 sources/80 traces, receipt/hash and scope/prefix/ordinal identities, immutable ownership, original-byte retry/restart, forced/owner/fallback outcomes, retained World/resources/RNG/duties, and synthetic provenance. It preserves repeat-to-Movement and finish-to-Truck-Convoy without executing either. This agrees with the contract:13–42 and48; native code may not load fixtures or run Python as authority.

R1 is appropriately research-first, parallel to N1 with separate document ownership. It must prove an actual creation-rooted Movement/Breakdown/selection source and reject relabelled synthetic Result2 contexts (plan:101–115; dispatch:128–134). N2/N3 are conditional, have provisional manifests, and explicitly stop if no compatible source exists. N2 ends at accepted positive entry and N3 must state which later round/result bridge remains; these slices cannot be reported as full creation-to-settled admission. The canonical later-II, consumed, actual ordinary repeat, public/Snapshot/transport and parent017–019 obligations remain open.

Concurrency is bounded to two workers plus one reviewer; separate worktrees, single canonical-plan owner and one host-wide full-suite gate avoid obvious ownership/resource races (plan:191–201). Research/Runner fixes cannot silently expand production ownership. R2 records the real prior AggregationFailed uncertainty, requires causal RED before a fix and forbids assertion weakening or treating reruns as a fix.

Review recovery is bounded: three sets of at most three passes, two intervening research spikes, no count reset on task rename/rebase/model change, spike review max3 without recursive recovery, and immediate human gate for heavy pivots (plan:164–187). Direct human planning and approval turns in Brain support the user override, coordination/messages, medium/high model assignment and merging only after review+CI. A pass stops early; unavailable/failed gates block dependents.

The six-hour behavior cutoff and seven-hour hard end are arithmetically correct:02:47:28Z→08:47:28Z is6h; →09:47:28Z is7h. Reserve displaces N3 then N2. Closeout:218–224 explicitly preserves retained WIP on committed/pushed feature branches, labels it unverified, excludes unrelated/tooling/credentials, and requires a committed/pushed coordinator handoff even if review or CI is unfinished. This satisfies the latest direct human requirement without falsely accepting incomplete work.

Merge policy:203–209 requires exact final-head CI and independent acceptance, verified own diff/base, no unresolved blocker/conflict, no administrative bypass, and rechecking changed behavior after rebase/conflict repair. N4 requires integrated evidence and final independent review, keeping final completion distinct from preservation.

## Author-Claim Reconciliation

| Claim | Evidence | Status/consequence |
| --- | --- | --- |
| Main907f414 includes150/151 | Independent HEAD/main and git log (#150 b908465; #151907f414) | Confirmed; N1 predecessor available |
| Primary stillf33c78c | Independent HEAD/main | Contradicted historical launch wording; P3 above |
| Four recent deliveries took24–41min | Independent read_thread timestamps:1768884,1465568,2484642,1679907ms | Confirmed:29.48,24.43,41.41,28.00min; estimates remain observations |
| PR151 repair added~2.2min | Separate child turn130612ms | Confirmed |
| Native manifest is accepted and80 control traces retained | Contract:48 and independent fixture JSON parse | Confirmed |
| Positive entry has a certified executable source | Plan expressly withholds this claim; dispatch:128–134 | Correctly unresolved; R1 is a genuine decision gate |
| PR151 final hosted checks allSUCCESS | Parent historical verification assertion inspected; no fresh GitHub CI query in this review | Not independently refreshed; coordinator still must verify exact heads at each publication/merge |
| Seven-hour ceiling and preserve-on-stop authorization | Direct human approval/merge answer and plan timestamps/closeout | Confirmed |

## Verification Performed

Read-only commands:
- git status --short; git rev-parse HEAD; git branch --show-current; git log -8 --date=iso-strict --format='%h %aI %s': main907f414 and expected merged order.
- git diff --stat: only unrelated user-config changes; git diff --name-only HEAD907f414: no commit diff.
- git check-ignore .planning/combat-overnight/session-plan.md: target is ignored.
- nl/sed/cat and focused rg of canonical sources and target: requirement/manifest/order reconciliation.
- Python -B read-only assertions: all five N1 paths match accepted spec; existing fixture parses with80 traces;6h/7h deadline intervals correct.
- git diff --check -- target: exit0, but ignored/untracked content is not covered by this command; no whitespace-proof claim for the plan.
- Independent read_thread calls for four delivery chats and parent approval/authorization.
- Target SHA256 at both observations:526b5c863f61a967a551ba247b31f970f4bb471bd8f6b73fdbf8ab5d620ee471.

No files edited, no tests/oracles/full suite run, no implementation/commits/reviewers spawned. A search for proposed019D3/native-control symbols returned no matches, consistent with future work, not a failed behavioral test. No structural source exploration was needed for this plan-only review, so Codebase Memory queries were not used; R1/N1 retain their required coverage/source checks.

## Open Questions And Residual Risks

Blind-pass limitation: the execution plan necessarily contains rationale, and reading the parent chat to verify direct human authorization exposed its plan-creation command before local target inspection. I reconstructed requirements separately and recorded preliminary concerns before the focused target pass, but cannot claim a fully blind review.

Positive-entry reachability is unproven; no-go is valid. Estimates use only four recent, established slices; review/recovery, oracle runtime, hosted queues or Runner recurrence may displace scope. No fresh CI rerun or runtime trust-boundary audit is implied by this plan approval. The plan's conditional gates and preservation policy handle these limits without widening scope.

## Verdict

Ready with non-blocking follow-ups.

## Recommended Next Actions

Coordinator: Accept the P3 launch-state correction with ownership, record this review/count/hash, and dispatch N1 plus R1 from verified907f414. Freeze each later child manifest and acceptance before edits. Require a reviewed R1 decision before N2; retain stop/no-go evidence if actual provenance cannot be established. Enforce full-suite serialization, exact-head review/CI, six-hour closeout and honest committed/pushed WIP/handoff preservation.
