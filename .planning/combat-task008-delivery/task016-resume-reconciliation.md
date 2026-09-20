# Task016 resume reconciliation — research only

## Packet and conclusion

Date: 2026-09-20. Branch: `codex/combat-custody-closure-session`. Observed HEAD: `ba54efd720b38627b473cfd5d2e2ba4ee638d6b2`. Decision owner: root implementation coordinator, acting within user-authorized continuation. Question: after Task015 independent acceptance, is proposed Task016 relationship/round/CA closure faithful, minimally scoped and implementable?

**Recommendation: retain one bounded closure slice, conditional on Task015 acceptance and root's explicit canonical scope reconciliation before edits.** No contract, API or independent-subsystem blocker found. Five material paths plus at most three narrowly dependent test-maintenance paths remain feasible. Eight is source/test ceiling, not claim of compliance with canonical five-file rule and not complete publication-file count. Refinement: preserve still-valid skipped-prefix rejection assertions in `CombatResolutionTests`; only stale explanatory comment needs maintenance there.

Task015 source match and fixture inventory are observations, not acceptance or executed Task016 proof. Task016 implementation, tests, reviews, commits and publication were not authorized by this research packet. No production/test changes, .NET execution or full oracle execution performed.

Source hierarchy: current canonical plan/roadmap and frozen Result2/World7 contracts → current source APIs and literal fixture → four prior Task016 research inputs. Success criteria: exact canonical closure behavior, complete causal World validation, explicit limited ownership, reproducible inventory and honest downstream boundaries. Stop condition: decision artifact plus remaining gate, within 15-minute research timebox. Skill: `/Users/dsteele/repos/ai-central/templates/skills/first-party/research-to-decision/SKILL.md`.

## Method and source index

Read AGENTS and named research inputs. Called Sandtable `session_recall` and `context_search` before exploration. CCE retrieval was broad and returned unrelated planning/review snippets; no review report was opened or used as evidence. Used narrow named-source reads after retrieval failed to isolate requested canonical sections. Parsed fixture with Python standard-library `json` only; did not import oracle. `git diff --stat 00a8b68 HEAD --` for seven existing proposed source/test paths returned empty: inspected APIs match merged Task015 baseline.

Primary references (repository-relative):

- `docs/design/combat-cycle-implementation-plan.md:95–99`, `:908–917`: five-file split rule, Tasks014–016, checkpoint G; `:919–928`: later Reserve/continuation ownership.
- `docs/roadmap/pre-alpha-roadmap.md:907–920`: dormant Sprint5 status; Task015 merged/tested but independent acceptance unfinished; Task016 follows.
- `docs/specs/combat-result-settlement-v2.md:117–176`: ordered effects, closure, canonical identity/readback and fixture limitations.
- `docs/specs/verify-combat-result-settlement-v2.py:173–260`: retry/callback ordering, final transitions, proof and CA topology, receipt assignment. Read as frozen reference, not executed.
- `docs/specs/combat-world-settlement-v1.md:95–108`, `:165–177`: predecessor chain, immediate versus future work, original-only relationship rules.
- `src/Cna.Core/Campaigns/CampaignCombatResolution.cs:67–97`, `:167–295`: state/effects, retry and final assembly.
- `src/Cna.Core/Campaigns/CampaignCombatResolutionCodec.cs:40–145`, `:319–320`: replay readback, World comparison, metadata and existing final grammar.
- `src/Cna.Core/Campaigns/CampaignCombatObligations.cs:245–393`: hashed append factory and full chain validation.
- `src/Cna.Core/Campaigns/CampaignCombatLossRetreat.cs:103–138`: pre/post-custody projection branches needing bounded common finalization.
- `src/Cna.Core/Campaigns/CampaignWorldV7.cs:133–141`, `:157–164`, `:256–277`: stage-dependent validation, complete typed equality and publication linkage.
- `src/Cna.Core/Campaigns/CampaignCombatSealedRound.cs:247–252`: authenticated-boundary route from official topology.
- Four retained inputs: `task016-dispatch-proposal.md`, `task016-api-reconciliation.md`, `task016-root-scope-notes.md`, `task016-dispatch-draft.md` in this directory.

## Scope reconciliation

**Documented fact:** canonical plan says primary estimates include focused tests and tasks exceeding five files must split before editing. It does not grant automatic exemption for maintenance. Prior root research selects eight paths; that is proposed exception, not silently equivalent to five physical files.

**Inference:** one slice minimizes causal fragmentation: relationship append, typed projection, codec and terminal transitions share same existing Result2 authority. Splitting relationships from terminal closure is feasible but adds separate partial-frontier lifecycle/test changes. Explicitly retaining five material paths plus strictly bounded dependent maintenance avoids new subsystem. Root must record selected execution refinement against canonical rule before implementation. Root has confirmed routine refinement is within current user continuation; no fresh user permission inferred by this research.

Exact source/test ceiling:

1. `src/Cna.Core/Campaigns/CampaignCombatResolution.cs` — three effects, closure state, structural stages, terminal guard.
2. `src/Cna.Core/Campaigns/CampaignCombatResolutionCodec.cs` — writers, stage/receipt/World consistency.
3. `src/Cna.Core/Campaigns/CampaignCombatObligations.cs` — named immutable Result2 relationship append.
4. `src/Cna.Core/Campaigns/CampaignCombatLossRetreat.cs` — existing projection finalization and causal original-pair relationships.
5. `tests/Cna.Core.Tests/Campaigns/CombatClosureTests.cs` — new comprehensive closure suite.
6. `tests/Cna.Core.Tests/Campaigns/CombatLossRetreatTests.cs` — narrow future-family assertion maintenance. On noncapture traces, first relationship now valid at retained retreat frontier; capture traces still cannot skip custody.
7. `tests/Cna.Core.Tests/Campaigns/CombatCustodyTests.cs` — narrow future-family assertion maintenance; first relationship now valid. Preserve `End` as first relationship index and 176/208 frontier.
8. `tests/Cna.Core.Tests/Campaigns/CombatResolutionTests.cs` — stale-comment correction only if needed. Lines112–119 apply later commands against single resolved-event prefix; those remain valid missing-history/stale-version rejects. Do not remove them as unsupported-family tests.

Retain 32/64, 144/176 and 176/208 earlier golden boundaries. Root planning/map/acceptance updates and worker evidence file are additional administrative artifacts: enumerate separately in final physical diff. Draft's “exactly8physicalpaths … and task016-worker-evidence.md” needs wording correction; evidence file is ninth artifact if counted physically. No World7, receipt model, schema, fixture, topology, Round or project-registration edits expected. Exceeding ceiling or altering authority requires re-scope before edits.

## API and contract findings

**Documented fact:** World7 skips `ValidateOpenSettlementEffects` after relationship receipt, while its value equality compares all elements, representations, broken lots, causes, relationships, custody, guards, entitlements, future obligations and settlements. This supports later movement; it does not authenticate Result2's exact frontier.

**Required consequence:** retain `state.World == Project(...)` through relationships, round-closed and closed. Reconstruct exact pre/post-custody frontier first, then derive relation from current original attacker/defender locations plus authenticated Content edges. Compare final expected settlement; never treat retained supplied World as reconstruction seed. Typed fabricated post-relationship World that constructor accepts but changes resource/location must fail serializer, separately from raw causal replay forgery tests. No World7 relaxation needed.

**Documented fact:** existing private settlement constructor enforces hashed Result2 occurrence identity, receipt predecessors and custody iff positive captured TOE at relationship stage. Add named `WithResultV2Relationships` through that constructor with disposition/losses/retreat required, no prior relationship, matching custody presence. Preserve legacy constructor semantics and unconditional hash validation. Null relation still requires relationship receipt. Adjacent original pair becomes engaged only for raw Engaged and RequiredRetreat0; otherwise contact. Nonadjacent pair has null kind/ID. Refused raw retreat still suppresses Engaged; no guard/new-arrival endpoints.

**Observation:** typed state/effects currently lack closure metadata and final effects; codec emits null/null/false. Final grammar already exists. Add owned immutable proof collection; metadata must match exact terminal receipt positions, stage and Closed flag, not only syntactically valid IDs. Relationships → round-closed proof order is disposition/losses/retreat/[custody]/relationships. Round event receipt derives from unsigned event; store that receipt before state serialization. CA effect references same round receipt and committed fifth step receipt, then atomically retains own receipt and closed=true. Official seven-position route from authenticated Boundary supplies Combat index5 → release index6; do not hardcode fixture strings or mutate paid Base.

**Documented fact:** existing retry lookup precedes stale/no-window callback NoOp; frozen oracle places closed guard afterward. Preserve ordering. Every accepted command remains retryable after closure with original event recovery/current state; fresh resolve/advance/choose rejects, stale expire/unavailable remains NoOp. No repeated CA completion. Structural events require null time, available clock, exact version and no window. Existing trusted independent-history APIs and raw-before-context validation remain sufficient; no supplied-state Apply authority.

**Inference:** these are bounded missing implementations, not API blockers. Original result, RNG, paid context, attack/target history, CP/ammo/TOE/Cohesion, guards/lots/entitlements and future obligations remain unchanged across final three events except relationship addition. Immediate settlement completes before round/CA; future feeding/training obligations remain unexecuted and do not block closure.

## Reproduced fixture inventory

Input: `docs/specs/fixtures/combat-result-settlement-v2.json`, SHA256 `6a1f6cda74424680669539d73583fa3ba5affc612380b3a3efe2e8986a09804a`. Python3 standard-library parsing on macOS/zsh in named worktree, with `python3 -B`; no .NET or oracle module import.

**Observation:** 32 traces, 272 `resultEventCanonicalUtf8`, 304 `stateHashes`, 32 complete final `stateCanonicalUtf8` literals. Prefix ending immediately before relationships totals176 events/208 hashes. Final three families each32 add96 events/cuts. Relationships16 contact /4 engaged /12 null. Round proofs16 length4 noncapture /16 length5 capture. All finals status=closed and closed=true. For every trace, parsed proof equals ordered retained settlement receipts; round/CA metadata equals final two event IDs; CA round link matches penultimate receipt; CA previous-step link equals committed final step receipt. These checks passed. They verify fixture consistency, not executable C# parity.

Recount recipe: parse `traces`; sum event-array/hash-array lengths and count final literals; parse each event/final state; count relationship payload `kind` and round `proofReceipts` length; compare proof to nonnull final settlement fields in disposition/losses/retreat/custody/relationships order; compare terminal receipt links as above. No fixture regenerated.

## Options, required evidence and next gate

| Option | Consequence | Disposition |
| --- | --- | --- |
| One closure slice with explicit bounded maintenance exception | Existing causal authority stays together; honest eight-source/test-path ceiling | Recommended after015 acceptance and root scope refinement |
| Split relationship then terminal closure | Strict cap may require separate maintenance/plan sequencing; repeated partial-frontier handling | Fallback if root cannot retain exception or material scope grows |
| Broaden World7/public admission/Snapshot/Reserve scope | Independent authority and downstream behavior; beyond016 | Reject |

Required implementation evidence: meaningful RED at authentic accepted015 frontier's next literal relationship advance; all272 exact event bytes,304 state-cut replay/hash comparisons and32 whole final JSON literals. Explicit relationship/refusal/null/guard-exclusion and proof4/5 counts. Test receipt order/missing/duplicate/foreign proofs, topology and fifth-step/round links, stage/window/closure ID mismatch, complete typed World equality, raw malformed/bounded payloads and copied proof ownership. Retry/discard every cut including closed; retry all prior commands after closed; fresh command rejection; stale callbacks; duplicate/suffixed event history rejection. Preserve original regression suites and clock invariants. Root owns required full gate, dev review and independent acceptance workflow; none claimed here.

Confidence: high for inventory and API fit; implementation feasibility remains inference until focused C# evidence. Unknown: pending015 independent acceptance result and any resulting source changes; exact implementation size; executed016 serializer/ownership failure behavior. Reconcile again if015 source changes.

Next gate: finish Task015 independent acceptance; root records canonical execution refinement and concrete dispatch, then016 implementation may start. Research is not dispatch. Task016 synthetic Result2 closure alone does not close all28 creation-rooted runtime traces, positive history admission, cumulative HistoryReplay/Snapshot successor, HOST-PUB-001, public activation, Task017–019 or Task025. No genuine new contract blocker found; these remain explicit downstream gates.

Memory retention note: automatic approval review rejected attempted CCE `record_decision`, citing nonpublic implementation details sent to an unspecified external memory connector. No retry or workaround; this local artifact retains decision evidence. External memory remains unwritten and is not a blocker to research result.
