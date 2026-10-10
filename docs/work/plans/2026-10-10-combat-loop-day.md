# Combat-loop delivery — 2026-10-10

Brain coordinator:01a0c9dc-00bc-78a3-800d-3cb36859e422.
Baseline merged PR167/main34a494c46ab7b8665e381f9e531c7b8365cb3efe.
User authorizes today's coordinated child work, TDD, author engineering review, fresh
independent reviews and merge after review/required CI pass. No new gameplay scope.
Start about12:17 America/Toronto. Closeout23:00 October10 (03:00UTC October11);
stop product work00:00 October11 Toronto (04:00UTC). Finish preservation if needed after
cutoff but do not begin more product work. Completion is an evidence gate, not a deadline waiver.

## Completion boundary

Supported actual creation-rooted encounter completes through side-safe public Combat actions,
settles obligations and reaches valid repeat/finish with replay/re-adjudication and two clean
retained runs; reconcile all72 Combat ACs and remaining required lineage before claiming loop
complete. Canonical docs/design/combat-cycle-implementation-plan.md Tasks017–025 govern.
Do not relabel synthetic histories or omit required result branches. This is the bounded Combat
skeleton toward first release, not complete six-turn scenario/Maproom/save/packaging delivery.

## Ownership and dependency graph

| ID | Owner/type | Scope and acceptance | Dependency/status |
|---|---|---|---|
| DAY-A / REL-AUD-02C | Existing visible implementation chat, GPT6.1 medium | Native actual prepared-round entry matches merged contract/literals for both owners, cancellations, cuts/retries, two trusted ledgers, privacy/error/capacity/ownership and all57 dependency gates. Five primary files: CampaignCombatActualRoundEntry.cs, Codec.cs, CombatActualRoundEntryTests.cs, Core.Tests.csproj fixturelink, canonicalCombatplan. Dated admin allowed. | READY to start; worktree combat-actual-selection-contract, branch codex/native-actual-round-entry |
| DAY-B | New visible research chat, GPT6.1 high | Source-backed commitment/result/settlement admission gap inventory and smallest contract/native packets. Cover every admitted outcome and lineage/public/recovery dependencies; identify pin maintenance prerequisites. No runtime or frozen-source edits. Own docs/research/actual-combat-commitment-result-gap.md + dated admin only. | Parallel withA; worktree combat-actual-bridge-research, branch codex/actual-combat-result-gap |
| DAY-C | Assigned only after B review; GPT6.1 medium | Executable actual commitment/result contract slice(s), then native slice(s), each with explicit manifest/tests/review. Costs/draws require reviewed complete outcome support or approved pre-mutation rejection. | A/B gates; not dispatched |
| DAY-D | Assigned after C and lineage inventory | Actual settlement→Reserve/Movement→repeat/finish, required later-II/consumed lineage, full reconstruction; no unsupported terminal shortcuts. | Sequential gated slices; not dispatched |
| DAY-E | Assigned only after certified private chain | Tasks020–021 side-safe observations/actions; paired hidden-state tests and independent ingress boundary. | D prerequisites; not dispatched |
| DAY-F | Assigned after E | Tasks022–025 strict Exercise/Runner replay/re-adjudication, two clean runs,72AC reconciliation/full gate/docs. | Loop completion gate; not dispatched |

Brain owns integration, canonical-plan reconciliation between author leases, review counts,
CI/publication/merge and final reporting. Research never edits author's canonical plan.
One full .NET suite lease at a time (initiallyA). No two writers in a checkout. Preserve user
primary-checkout edits and preexisting.serena. Only one active implementation successor per
shared contract/plan; prepare research/review in parallel. Child completion must message Brain.

## Delivery state machine (every milestone)

1. Freeze source-backed scope/manifest and observable acceptance.
2. TDD: reproduce intended semantic failure before behavior; retain RED command/output,
   minimal GREEN, adversarial/cut/retry/conservation tests. Normal oracle never regenerates goldens.
3. Author self engineering review using code-review-and-quality: correctness, readability,
   architecture, security/privacy, performance, test failure sensitivity and plan completeness.
   Retain findings/resolutions; test changes again. This is NOT independent approval.
4. Freeze exact head/five primary hashes plus evidence and separate neutral bootstrap/author
   explanation. Send SELF_REVIEW_COMPLETE/REVIEW_READY to Brain. No author self-issued Ready.
5. Brain dispatches fresh GPT6.1 medium reviewer with no inherited author context. Read bootstrap,
   code/spec/plan first and record preliminary findings BEFORE author rationale. High for a
   justified difficult research/review question. Reviewer is read-only, evidence-backed verdict.
6. One round=up to3 fresh passes; stop at Ready. Author responds Accept/Dispute with evidence/
   explicit deferral, adds regression RED/GREEN for fixes, self-reviews correction and refreshes
   exact freeze. Material changes need fresh review. No rubberstamp or quota of3 passing reviews.
7. If round still not Ready after3 passes: STOP downstream work; send consolidated failures to
   Brain. Brain commissions high-effort bounded investigation/experiment with competing
   hypotheses, primary evidence, reproducer, decision and revised acceptance. Review spike result,
   then rework from its findings, rerun tests+selfreview and begin next review round.
   Maximum3rounds/9passes and2recoveryspikes per milestone; no reset by renaming. Still failing
   after final round => user-sync blocker. Heavy architecture/scope pivot needs user decision.
8. Brain verifies exact reviewed head, mandatory CI and clean integration, then publishes/merges
   with github-keychain-auth. No failed/skipped required gate promoted to pass. Main integration
   preserves hashes or reassesses material changes before merge. Poll boundedly/backoff.
9. Update canonical status and handoff with exact branch/head/PR/test evidence, known failures,
   next ready node and work ownership. No complete-parent claim from completed child.

## Evidence, throughput and forecast

Existing four predecessor oracle failures remain separately tracked; repair only via approved
bounded dependency-aware task when required. No new pin refresh or failure waiver. Use repository
run-tests/binlog skills and native MTP --project/--solution; fake clocks and deterministic RNG.
Run focused tests after changes, full gate at native milestone; don't duplicate unchanged passes.
57-pin round contract normal oracle took1897.769s (~32min); recent CI~8–15min. Include these plus
self-review/independent-review/correction time in forecasts. Whole-loop-today is aspirational:
commitment/result/native/public gaps are still sequential and not fully sized. Initial working
allowanceA3–5h including review/CI, B1–2h plus review, lowconfidence; recalibrate at first handoff.
Do not convert merged PR timestamps into coding throughput. At mid-session replan only ready
bounded tasks; last hour reserved for preservation/CI/handoff, no large new slices.

## Closeout

Commit/push all retained work, label unverified WIP explicitly and never merge it. Preserve logs
or their durable reproduction/hash record; write and push docs/work/handoffs/2026-10-10-combat-loop-day.md.
Report completed/merged, reviewed/unmerged, WIP, blocked, next exact action and test limitations.
Pause heartbeat at completion/deadline. Notify meaningful milestones/failures/user gates only;
no unchanged heartbeat status chatter. Do not recreate children already running or completed.
