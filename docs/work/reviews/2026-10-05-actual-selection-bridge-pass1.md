# Independent review: S2 actual-selection bridge research

Review instance: S2 set1/pass1, total1of9. Verdict: **Ready** for this research packet and its conditional private contract-first plan.

## Findings

No actionable P0–P3 findings.

Verified exact worktree `/Users/dsteele/.codex/worktrees/combat-actual-bridge-research/sandtable`, branch `codex/combat-actual-bridge-research`, base `afa396ad5094fae7b8f054c60a9df9e03f83dfd5`, head `80cc969efd347ee7390c2dea511cc50fc6592a0d`. Diff contains exactly the four authorized new Markdown files, 1,256 added lines. No runtime, canonical spec, fixture, pin, project or test changes. Only untracked `.serena/` is excluded generated tooling. Committed diff whitespace check passes. Head stayed fixed throughout review.

AGENTS/bootstrap, canonical requirements, research, source predicates and tests were inspected before the separate Author Explanation. Preliminary ledger was recorded at `/private/tmp/s2-set1-pass1-preliminary.md` before author-packet access. The research deliverable necessarily exposes its recommendation; the separate author rationale was withheld until after that ledger. No author conversation history was inherited.

## Plan Review

The recommendation meets S2's research objective: establish a useful bounded next consumer without seed, creation, position, receipt, owner, clock or prior-history substitutions. Existing 019E0/E1 admits the two seed1 Normal/NONE sources only through a supported candidate at version13. Canonical overnight reconciliation explicitly leaves selection/result consumption and parents017–019 open.

The proposed positive endpoint at Force Assignment after accepted defender decline is credible: seven pure-kernel records reach version20, preserve World/RNG/resources, and retain three ordered structural step receipts. The seven named no-selection/cancellation variants reach the same-slot Reserve Release without an accepted decline or committed assault. Force Assignment completion remains unsupported. This is a feasibility observation, not a new admitted selection lineage or actual attack/result.

Direct C3a reuse is correctly rejected. `CampaignCombatSelectionSteps.ValidateBoundary` (lines22–48) requires its exact compatible creation, request seed provenance and an owner-materialized Position. Actual entry preserves seed1 and catalog activeSide=null. Its Weather/Breakdown hash fields are trusted boundary evidence, not admission of the original histories. `CampaignCombatSealedRound.AuthenticateBase` (lines11–24) directly calls old C3a replay/validation. Result2 independently authenticates that old Round2 base and committed replay. Neither consumer becomes compatible through a scratch projection.

The proposed new source/boundary/event/proof framing is coherent: full actual entry retained, actual receipt IDs distinguished from full-event hashes, original cycle opening distinct from current version/prefix, nonrecursive domain-separated identities, separate trusted selection ledger, complete owned history, closed canonical grammar and bounded capacities. No outward schema or intelligence path is introduced. New syntax must reject mixed historical families and digest-only caches.

The two five-primary-file manifests are credible as bounded proposals: S3 spec/schema/fixture/oracle/plan; S4 engine-and-models/codec/tests/fixture-link/plan. Native C3a Transition is private and cannot legally supply a public pure kernel. Separate per-family mechanics therefore entails real duplication cost, correctly retained as an S4 parity obligation rather than claiming implementation fit is proved. Full cuts/retries/fallbacks, actor/clock/error order, ownership, canonical ordering, capacity and privacy rejection obligations are sufficient for the next contract gate. A sixth primary file or architecture/gameplay change returns the stated scope gate.

Contract review/CI/merge precedes native work; native review/CI/merge precedes downstream admission. No rollout or public activation is proposed; public seats, clock confidence, Archives/restart, Snapshot/transport, actual round/result and authentic repeat remain separate gates. No heavy pivot is required by this review.

## Author-Claim Reconciliation

| Claim | Evidence inspected | Status and consequence |
| --- | --- | --- |
| Four Markdown files only; 40 cited source paths unchanged | Exact Git diff; independent size/hash/Git-object comparison of all40 handoff rows | Confirmed; research-only scope |
| Actual entry preserves source/proof facts for both owners | 019E0 reader, native ReplayAuthority/Replay, focused literal/cut tests, fresh complete oracle and probe | Confirmed within narrow pinned source profile; content-origin labels remain synthetic |
| Existing C3a/Round2/Result2 rejects actual lineage | Native boundary/base predicates; Python boundary/read_base/verify_context; independent reproduction | Confirmed; new contract family required |
| Pure selection mechanics reach positive FA20 and all fallback endpoints | Reproduced durable probe, exact stdout hash, source transition inspection | Confirmed as controlled mechanics only |
| Re-signed event actor/time cannot substitute for independent trusted ledger | Native decode-only comment/API; Python read_event; eight probe rejects; independent coherent-alternative experiment | Confirmed as consistency relative to trusted inputs, not authentication of human/clock/store |
| Changed valid retry time preserves original evidence; malformed primitives reject first | Independent post-terminal retry checks | Confirmed for existing C3a semantics; entry exact-entire-input contract stays unchanged |
| Native21/21 and four original oracles pass; original Breakdown fails | Fresh exact-checkout command executions | Confirmed, with identical Python output hashes |
| New framing parity, complete acceptance closure and five-file fit already proved | Not claimed; explicitly U1/future gates | Unverified and appropriately deferred to S3/S4 |
| S1 deferral permits unconditional S3 | Not claimed; S1a report and coordinator plan require frozen dependency disposition | Not established; Ready S2 grants no automatic waiver |

The independent ledger experiment changed selection admittedAt1100→1101, regenerated the subsequent coherent event chain, and reached FA20 when the replacement ledger was supplied. The changed event rejected CMB-STP-006 against the original trusted input. Therefore cryptographic-looking receipt/hash self-consistency cannot establish the authentication precondition. The report expressly acknowledges this at lines229–245 and must remain binding for S3/S4 API design.

## Verification Performed

Commands ran from the exact frozen worktree unless noted. Python processes used a180-second bound; every listed process completed without timeout. No full-suite lease was taken.

| Exact check | Observed outcome |
| --- | --- |
| `git status --short`; `git rev-parse HEAD`; `git branch --show-current` | Exact dispatched branch/head; only excluded .serena/ |
| `git diff --stat afa396ad5094fae7b8f054c60a9df9e03f83dfd5 80cc969efd347ee7390c2dea511cc50fc6592a0d`; matching `--name-status` | Four authorized Markdown additions only |
| `git diff --check afa396ad5094fae7b8f054c60a9df9e03f83dfd5 80cc969efd347ee7390c2dea511cc50fc6592a0d` | Exit0 |
| `dotnet --version` plus global.json/project/build/package source inspection | SDK10.0.400; SDK-style net10; native MTP; xUnit v3 |
| `dotnet test --project tests/Cna.Core.Tests/Cna.Core.Tests.csproj --filter-class Cna.Core.Tests.Campaigns.CombatPositiveEntryTests /bl:/private/tmp/s2-set1-pass1-native-{}.binlog` | Exit0;21passed,0failed/skipped;13.481s |
| `python3 -B /private/tmp/s2-set1-pass1-probe.py /Users/dsteele/.codex/worktrees/combat-actual-bridge-research/sandtable` | Exit0;4.131s; exact retained stdout |
| `python3 -B docs/specs/verify-combat-positive-entry-v1.py` | Exit0;115.702s;2owners/4events/6proofs;6cuts/6retries and3966 rejection probes |
| `python3 -B docs/specs/verify-combat-selection-steps-v1.py` | Exit0;3.640s;5traces/41cuts/246mutations/164raw rejects |
| `python3 -B docs/specs/verify-combat-sealed-round-v2.py` | Exit0;17.180s;12groups/10traces/68cuts/610mutations/340raw/288clock/480retry/30invalid |
| `python3 -B docs/specs/verify-combat-result-settlement-v2.py` | Exit0;89.575s;10groups/32synthetic traces/304cuts/3728mutations/1360raw/384timing/200prior-time comparisons |
| `python3 -B docs/specs/verify-combat-inherited-breakdown-completion-v1.py` | **Exit1**;0.375s; source drift in Cna1979LandSequence.cs; not waived |
| Independent Python comparison of all40 source-inventory rows | Recorded sizes/hashes and exact baseline Git bytes all match |
| Independent comparison of S1's16 frozen actual-entry dependency rows against S2 checkout | All16 digests match |
| Independent event/ledger and retry probes via unchanged Python kernel | Changed event rejects original ledger; coherent replacement ledger replays; changed valid retry time preserves original receipt/control; bool timestamp rejects CMB-STP-001 |

Probe counts reproduced:6entry cuts,24history/admission rejects,16positive selection cuts,56retries,22clock/actor/FA rejects,14re-signed event forgeries,14noncanonical rejects,14fallback traces,3mapping rejects,8event actor/time claim forgeries. Scratch C3a framing remains inadmissible authority evidence.

Probe extraction SHA256: `f096a07b1ea779a8d4fa70c60fd4b38ef5fef9c57cdaf74944d19f7747ab3cc0`.
Probe stdout SHA256: `c1ec75db3d42aadd2a07589df14a9ac5d33ce96af77eb1b16c8995f0c9ba18f2`.
Fresh Python stdout/log hashes all equal their recorded handoff hashes, including the original Breakdown failure. Logs: `/private/tmp/s2-set1-pass1-checks/`.

Unique native binlogs exist:

- `/private/tmp/s2-set1-pass1-native-20261005-133541--20905--PraNAu.binlog`
- `/private/tmp/s2-set1-pass1-native-20261005-133551--20905--SzR8+A-dotnet-test.binlog`

Exact-worktree Serena activation/manual and symbolic source reads used. Codebase Memory project `sandtable-actual-bridge-research`, fast generation2026-10-05T12:47:25Z, exact head/root verified. Relevant search pages exhausted; PositiveEntry page0/100 consumed all108 rows. ValidateBoundary both-direction depth1 trace exhausted4 production callers/8 callees. Cited native/test paths report metadata_match/no_recorded_issue; test-project metadata is untracked and docs/specs excluded, so direct project/Python/spec reads supplied fallback. Graph metadata is not completeness proof. CCE tools were not available.

One supplemental digest harness initially scanned past S1's16-row table into later temporary-evidence rows, causing a missing inventory_probe.py lookup. Restricting the parse to that table produced the16-row match. This was reviewer harness error, not a repository failure. A mistaken S1 document read in the S2 checkout likewise yielded only a missing-file diagnostic; the exact S1 checkout was subsequently read.

No full solution/Boundary/format gate, new contract/native selection tests, fixture regeneration, hosted CI or publication was executed or inferred from these checks.

## Open Questions And Residual Risks

S1a independently reviewed head `cb16f676f09d81e124fcdcc7d7533cf2ad9a199c` returned Ready, set1/pass1,total1of9. Its20-file maintenance closure and conditional deferral were inspected, not independently re-audited in S2. Its fresh review confirms original cycle-sequence and Snapshot source-pin failures and independent outward Content drift. Those remain failures; this S2 review freshly reproduced Breakdown only. Historical author timeouts remain historical even when a later reviewer completes an oracle.

The actual-entry replay imports unchanged Breakdown reader/schema functions without invoking the old failing fixture's main/check_fixture admission. A new contract can therefore be independent of the blocked historical fixture, recursive Snapshot and outward readers. Import success alone does not verify predecessor file digests; S3 must explicitly enforce its frozen admitted dependency pins before relying on replay/cache data. S1's16 inputs and full20-path old closure/status must be preserved in the coordinator freeze.

No production actual-selection seat/clock/store authenticator was established. Core trusted API arguments require independently authenticated authority input; syntax/receipt/hash replay proves only consistency. Private owner checks are not full outward fog-of-war proof. New event/proof literals, adversarial error ordering, ownership/capacity closure and native duplication parity remain required S3/S4 evidence.

Temporary review logs/reports may expire; coordinator should retain this report and count in its durable review ledger. No source, plan, pin, fixture or author artifact was edited. No agents/reviewers/streams, messages, commits or publication were created.

## Verdict

**Ready** for the exact S2 research packet and conditional private actual-selection contract/native plan.

This is not S3 dispatch or public activation approval. S3 requires accepted/merged S2 and an explicit frozen coordinator disposition that reconciles reviewed S1a with the chosen actual-entry-only admission dependencies. Original Breakdown/cycle/Snapshot/outward failures remain individually reported. Any use of blocked historical admission makes the proposed deferral insufficient.

## Recommended Next Actions

Coordinator records S2 set1/pass1,total1of9 Ready at80cc969efd347ee7390c2dea511cc50fc6592a0d, accepts the research through its normal publication/CI gate, and freezes the reviewed S1/S2 dependency disposition before S3. Preserve the exact manifests, trusted-ledger API precondition, two-source limits and unchanged historical contracts. Then require S3's semantic RED/GREEN, new literal/adversarial acceptance, honest predecessor outcomes, fresh counted review and exact-head CI before S4. No additional reviewer or recovery set is warranted by this report.
