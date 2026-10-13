# CS04 independent review

Review instance: 1 of 9. Round1/pass1; recovery0/2. Reviewer received neutral dispatch, not author conversation. Independent-review reviewer mode and code-review-and-quality applied. Preliminary ledger saved before reading separate author rationale: `/private/tmp/cs04-independent-review-1-preliminary.md`.

## Findings

No actionable findings identified. No heavy architecture or scope pivot required.

Reviewed checkout `/Users/dsteele/.codex/worktrees/combat-actual-selection-contract/sandtable`, branch `codex/actual-repeated-movement-contract`, exact published HEAD `c30570350bbad4e0572b67cfba21e71cef868a98`, base `6ed3846387fd787692971c9926a76663bf9368da`, product freeze `e46ebfa9fffb4bbbc0e3177f57bfc85efd050f53`. Base-to-head scope: 52 files, docs plus README/tech-design; product-freeze-to-head only neutral bootstrap and author rationale. Only untracked `.serena/`, explicitly excluded. No native code, test projects, workflow, Content/rules or predecessor modifications. Independently compared all 228 base `docs/specs` paths; unchanged. Diff whitespace check passed.

Correctness evidence in `docs/specs/verify-combat-actual-repeated-movement-v1.py`: `derive` (106–124) replays full PAC ancestry before deriving Base/current state; `transition` (131–166) validates primitive/family/context/owner/unit before all-prior retry lookup, then validates current topology, Reaction boundary, original eligibility, occupancy, resources, representation and cost before copy-owned projection; `replay` (167–173) compares independently rebuilt events; public readers (178–185) compare only against reconstructed claims. First Contact ending, cumulative spending/cohesion, member spending, route, receipt/progress and unchanged original proof/Authority/RNG/history are represented explicitly. No completion/stop/phase advance or route-name admission exists. Fifth move reaches inherited cost rejection before event-capacity rejection.

Architecture/security/performance fit: additive private deterministic reference, restricted reuse of frozen pure movement helpers, distinct identity domains, strict canonical input grammar, bounded arrays/bytes/depth, independent ledgers and physical pins. No provider I/O, secrets, public ingress or new dependencies. Full ancestry replay and repeated physical hashing are expensive but deliberate reference semantics; no production runtime performance claim is made. Dense one-line style follows neighboring reference scripts; no justified style-only blocker.

## Plan Review

Canonical contract/schema, CS04 plan, accepted research, implementation-plan addition, README/tech-design/roadmap reconciliation reviewed. RM01–RM06 are materially covered by implementation and retained/executed evidence. RM07 remains Brain-owned review, exact-head CI and merge. Move-only scope is consistent across navigation and canonical documents; native consumer, positive/later-II/consumed Reserve, stop/resolution/completion, Reaction/Breakdown/Convoy, public/Exercise/persistence and parent017–025/all72 remain expressly open.

RM02 chronological precision gap is transparent: original `e9ff5db` stub ignored fifth ledger while proving missing move-consumer failure after four-ledger PAC replay. Independently inspected that commit's code. Later correction is explicitly post-GREEN and cannot establish original chronological fifth-ledger RED. Present grammar/adversarial checks prove current fifth-ledger behavior. The accepted correction is a bounded evidence limitation, not an unresolved product defect or basis for resetting counters.

Capacity calculations independently recomputed: Source17,565,184, Control258,501, Proof1,922,057; all fit frozen caps. Exact new array bounds and metadata padding cover one member/track, five route nodes, four receipts/progress entries, two original excluded units, three added DP causes. World bound is inherited and already covers four causes. PAC64+2 exact pin composition preserved; all 66 current external bytes match declared hashes. No arbitrary pin refresh/self-hash cycle.

## Author-Claim Reconciliation

| Claim | Independently inspected evidence | Status / consequence |
| --- | --- | --- |
| Authentic repeat37 full ancestry; fixture roots not runtime allowlist | derive/replay, five-ledger grammar, retained timestamp tests | Confirmed implementation; clock variant test inspected but not rerun |
| Cumulative CP5→9→11→13→15; DP0/1/2/2; Contact once; original provenance preserved | transition; executed Commonwealth/attacker preservation | Confirmed |
| Every prior command retries at later cuts with original receipt; owned readback | retry/replay/readers; executed Commonwealth/defender recovery:5cuts/10retries | Confirmed for executed trace; retained normal covers other roots |
| Reaction adjacency and fifth CP17 reject before publication; causal forgeries rejected | transition order; executed Axis/attacker adversarial62 checks | Confirmed |
| All66 physical external paths, no unknown existing acquisition,332 fault assertions | independent cold audit; dependency group; static manifest checks; retained closure evidence | Confirmed |
| Structural domination under selected caps | capacity code/schema/predecessor bounds; executed capacity; independent arithmetic | Confirmed |
| Full normal344.251s/900 passed with frozen bytes unchanged;4traces/20frames/16events | retained receipt/log hash, current code/schema/fixture hashes and fixture inventory | Confirmed retained evidence integrity; full normal not independently rerun |
| Original RED does not validate fifth ledger; supplement post-GREEN | original commit code and dated correction | Confirmed, historical limitation preserved |
| No native/public/lifecycle/parent completion claim | full diff and synchronized docs | Confirmed |

## Verification Performed

All focused groups executed sequentially with explicit120s process bounds using `python3 -B docs/work/reviews/2026-10-12-cs04-bounded-run.py <unique-name> 120 <flags>`. Runner invokes `python3 -B -u docs/specs/verify-combat-actual-repeated-movement-v1.py` with listed flags. All exit0, no timeout:

| Flags | Elapsed seconds | Result |
| --- | ---: | --- |
| `--group grammar` |14.255|PASS closed canonical/signed/primitive/preparse/fifth-ledger grammar|
| `--group recovery --owner commonwealth --seal defender` |33.410|PASS5cuts/10all-prior retries/owned readback|
| `--group adversarial --owner axis --seal attacker` |50.590|PASS62 causal/context/Reaction/fifth/source-first checks|
| `--group capacity` |7.292|PASS structural byte/depth/field domination|
| `--group dependencies` |3.219|PASS66external/332physical-fault assertions|
| `--group preservation --owner commonwealth --seal attacker` |7.565|PASS CP/DP/Contact/member/progress/immutable-field preservation|

Receipts/logs: `/private/tmp/cs04-independent-1-{grammar,recovery,adversarial,capacity,dependencies,preservation}.{json,log}`. Independently bounded cold physical audit (Python audit hook installed before import):0.668s/120, exit0, all66 external paths observed, zero unknown existing repository files, two owned delivery paths.

Additional executed static checks: all external/test artifact SHA values; code/schema/fixture bytes and hashes versus frozen receipt; retained normal log hash; literal4trace/20frame/16event inventory; exact PAC64+2 identity; retained final closure manifests equal current pins and recorded unknown/missed sets empty; all228 predecessor docs/specs paths unchanged; Source/Proof/Control arithmetic; `git diff --check base HEAD`.

Serena initial instructions read. Codebase Memory project discovery/search/coverage performed. Docs scope excluded from index (2026-10-05 generation); graph search yielded no symbols. Direct source reads/rg used for excluded evidence scope. No graph absence used as completeness proof. Target AGENTS read; obsolete CCE tools unavailable, with graph-first/source fallback per dispatch/user guidance.

## Open Questions And Residual Risks

No unresolved product question within this bounded private contract. Full normal and all four owner/seal combinations are corroborated by retained source/log/hash evidence; reviewer reran proportionate subsets, not full normal/predecessor/.NET suites. Literal equality check was inspected and retained hash verified; fixture group not rerun. Author core/adversarial acquisition logs were inspected and reconciled, while independent cold acquisition and all dependency faults were executed. Fixtures were not regenerated; target files unchanged.

Python oracle is outside GitHub .NET CI. Exact-head CI and expected2843-row inventory remain separate Brain gates; this review makes no CI passing claim. Frozen successor source support remains narrow; future broader profiles/lifecycle joins need new explicit contracts and evidence. Original fifth-ledger chronological RED gap remains disclosed. Historical owning failures and aggregation/CoreB139 are not waived.

## Verdict

**Ready** for the CS04 private repeated ordinary Movement reference at the exact reviewed HEAD. This verdict covers implementation and plan within stated scope, not merge authorization, native/public activation or broader lifecycle completion.

## Recommended Next Actions

Brain should reconcile this report, retain focused verification receipts, complete exact-head CI/merge gates, and preserve the disclosed RED correction and open downstream boundaries. No repair, counter increase, additional reviewer or workstream requested by this pass.
