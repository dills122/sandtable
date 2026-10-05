# First playable release audit — 2026-10-05

Status: planning audit at `af37155`; no runtime capability or release is declared by this document.
Canonical status remains the [pre-alpha roadmap](../roadmap/pre-alpha-roadmap.md).

## Release boundary and conclusion

First release means two local players finish the six-turn, Land-only **Graziani's Offensive**
through Maproom, with source-verified setup and victory, all reachable rules, private seat handoff,
durable save/resume, and identical seed/accepted-order/event replay. This is the existing
[MVP boundary](../roadmap/pre-alpha-roadmap.md#first-playable-mvp), not a new scope proposal.
The original source baseline and Land-only adjustments remain governed by the
[source-material spike](cna-source-material-spike.md#recommended-authority-policy).

Sandtable has a substantial deterministic engine foundation and a tested synthetic public path.
It has not reached the working Combat skeleton, and that skeleton will not itself make the
published scenario playable. Current public authority stops at first-side Combat entry in Operation
Stage 1. Private Combat adapters and executable contracts prove bounded later transitions, with
different provenance limits. Published scenario content, later scenario-required mechanics,
termination/victory, durable campaign hosting, and Maproom are separate remaining delivery lanes.
There is no defensible whole-project completion percentage or first-release date before the exact
scenario inventory is measured.

## Method and evidence limits

Read-only audit inspected current `src`, `tests`, `scenarios`, solution/build configuration,
workflows, canonical plans, research, and session handoffs. Source/test references below identify
actual methods or named tests; test presence is not a fresh passing execution claim. No .NET build,
suite, host probe, browser playthrough, or Python contract oracle was executed for this audit.
Retained passing/failing evidence applies only to its recorded inputs and commits.

Codebase Memory `list_projects` did not list this audit checkout. The `sandtable` graph points to
the primary checkout, generation `2026-10-05T02:49:00Z`. Coverage for 24 evidence paths was fully
returned: README/architecture/vocabulary/roadmap metadata changed; positive-entry and settled-control
tests were not tracked by that generation. Scope results also reported partial parse ranges in
Legal Actions, observation projectors, and three dormant Reaction tests. Those ranges were read
directly. Tracked-file inventory, focused `rg`, and direct source reads supply this audit; graph
metadata does not establish current-worktree completeness. No CCE `context_search` or
`session_recall` tool was available.

This is a bounded readiness audit, not a new transcription of original scans, an exhaustive rules
correctness review, legal advice, a storage benchmark, or an independent engineering review.
The source/rights uncertainties already recorded by the project remain open.

## Capability evidence matrix

States use the roadmap vocabulary. **R** = researched; **C** = contract-frozen;
**I** = implemented in typed runtime; **A** = publicly activated and witnessed.
`—` means the release capability has no evidence at that state; a narrower fixture never upgrades
the full release requirement. Foundations marked A remain bounded to their admitted profiles.

| Release capability | R | C | I | A | Exact evidence and remaining boundary |
| --- | --- | --- | --- | --- | --- |
| Source hierarchy, provenance, deterministic rules identity | Yes | Yes | Yes | Synthetic | [Rules9 manifest](../../src/Cna.Core/Rules/Cna1979BreakdownRuleset.cs), `Manifest`; [source baseline](cna-source-material-spike.md). Versioned source identity is implemented; complete scenario source/rights matrix is not. |
| Static topology/forces/setup admission | Yes | Yes | Yes | Synthetic | [Content canonical tests](../../tests/Cna.Core.Tests/Content/ContentCanonicalTests.cs), `MinimalPackMatchesTheCompleteGoldenCanonicalVector`, `EverySelectedSemanticMutationChangesTheContentIdentity`; [synthetic catalog](../../src/Cna.Core/Content/Cna1979SyntheticContentCatalog.cs), `CreateArtifact`/`Origin`. Content foundation is reusable; packs are original laboratory data. |
| Initiative, Weather, stage preamble, Reserve | Yes | Yes | Yes | Bounded | [Preamble](../../src/Cna.Core/Campaigns/CampaignV11Preamble.cs), `Create`/`Apply`; [admission](../../src/Cna.Core/Campaigns/CampaignSnapshotV11Admission.cs), `IsValid`; [legal actions](../../src/Cna.Core/Actions/CampaignLegalActions.cs), `Query`/`GenerateForSide`. No-obligation Operation Stage 1 path is not general Organization, convoy, fleet, later-stage or next-turn authority. |
| Movement, Reaction, Breakdown | Yes | Yes | Yes | Bounded | [current action execution](../../src/Cna.Core/Actions/CampaignCurrentActionExecution.cs), `Execute`/`CreateEvent`; [Runner closeout](breakdown-runner-closeout.md#verification); [Truck tests](../../tests/Cna.ExerciseRunner.Tests/Execution/BreakdownTruckStudyTests.cs). Retained two-run evidence terminates at Combat position determination; positive ZOC, broader cohorts, towing/repair and later-stage reset are excluded there. |
| Replay/checkpoints and side-safe observations/actions | Yes | Yes | Yes | Bounded | [current event runtime](../../src/Cna.Core/Campaigns/CampaignCurrentEventRuntime.cs), `CampaignCurrentProjector.Replay`; [Exercise reconstruction](../../src/Cna.Core/Exercises/CampaignExercises.cs), `Reconstruct`; [privacy tests](../../tests/Cna.Core.Tests/Observations/CampaignObservationPrivacyTests.cs), `HiddenOpponentChangesRemainInvisibleAtEveryCheckpointThroughMovement`. These are Core/harness guarantees, not durable user saves or browser seat isolation. |
| Combat opening and private selection | Yes | Yes | Partial | — | [positive-entry tests](../../tests/Cna.Core.Tests/Campaigns/CombatPositiveEntryTests.cs), `ActualIdleSourceReachesPositionDeterminationWithRealMovementProof`, `UnsupportedActualOpeningsAndOldRouteAdmissionStayClosed`; [actual-selection contract](../specs/combat-actual-selection-v1.md). Native entry proves two actual opening histories; selection contract stops at Force Assignment after defender decline or no-attack Release. Native selection consumption is still open. |
| Combat round/result, Reserve Release, repeat/finish | Yes | Yes | Bounded private | — | [settled-continuation tests](../../tests/Cna.Core.Tests/Campaigns/CombatSettledContinuationTests.cs), `OriginalDistanceAndPriorExclusionsRemainAuthenticatedSyntheticEvidence`; [settled-control tests](../../tests/Cna.Core.Tests/Campaigns/CombatSettledControlTests.cs), `NativeSettledControlOpensOwnerDecisionAndRepeatPreservesLiteralWorld`; [bounded plan](../design/combat-cycle-movement-delivery-plan.md). Native settled control retains synthetic earlier Movement; actual round/result joins and later-II/consumed lineage remain open. |
| Full authentic continual cycle and public Combat | Yes | Selected contracts | Partial private | — | [Combat task graph](../design/combat-cycle-implementation-plan.md). Tasks017–019 parent boundary is open; public020–021, Exercise/Runner022–024 and all72-AC closeout025 remain gates. |
| Historical scenario map/ORBAT/supply/arrivals/victory data | Baseline only | Foundation only | — | — | [Content7 models](../../src/Cna.Core/Content/ContentPackV7Models.cs), `ContentCombatScenario`, component/readiness/ammunition types; [MVP-GATE-01/02](../roadmap/pre-alpha-roadmap.md#post-skeleton-milestones-toward-the-first-playable-mvp). Selected static combat/readiness facts do not implement full supply or arrival systems. Current catalogs/manifests are laboratory fixtures; no admitted Graziani pack was found. |
| Six-turn lifecycle, remaining Land mechanics, victory | Baseline only | Sequence catalog only | — for complete scenario | — | [sequence catalog](../../src/Cna.Core/Rules/Cna1979LandSequence.cs), `CreateTurn`/`AddPlayerPhase`; [supported checkpoint boundary](../../src/Cna.Core/Rules/Cna1979LandSequenceV4.cs), `IsSupportedCheckpoint`. Catalog names include three stages, both sides, convoy/rail, repair, patrol and end-turn; presence in a catalog is not executable six-turn progression or victory. |
| Durable local campaign publication/save/resume | Bounded probe | Proposal | — | — | [Orleans feasibility](orleans-publication-feasibility.md#proposed-production-contract); [host Program](../../src/Cna.OrleansHost/Program.cs). Development-only silo has no campaign grain/storage registrations. Probe uses injected memory CAS and same-process reactivation; kill/restart/provider durability is unresolved. `HOST-PUB-001` remains open. |
| Maproom, hot-seat, Chronicle presentation | Reviewed direction | Proposed composer | — | — | [composer specification](../specs/player-intent-composer-v1.md), proposed status and privacy/confirmation requirements; [solution](../../Sandtable.slnx); [Pages workflow](../../.github/workflows/pages.yml) packages only `site/`. No Maproom project or executable player path in surveyed solution/source. Event bytes and labels do not establish a complete explanatory player interface. |
| Local launch/package and distribution readiness | Developer workflow | Build config | Dev stack | No game release proof | [AppHost](../../src/Cna.AppHost/AppHost.cs); [CI](../../.github/workflows/ci.yml); [Just recipes](../../justfile). Restore/format/Release build/tests are automated. No game release-bundle workflow, fresh-machine player installation proof, release support matrix, or license/notice decision was found in tracked files. |
| Intelligence, parser, remote multiplayer | Reviewed future direction | Transport/proposals | Scaffold | — | [worker Program](../../src/Cna.DecisionWorker/Program.cs) only registers gRPC client; [gateway](../../src/Cna.Intelligence.Gateway/Services/IntelligenceGrpcService.cs), `ChoosePlan`/`GenerateNarrative` report unavailable. Optional AI and remote play are excluded from first-release blockers. |

## Definite gaps and decisions still to measure

**Definite first-release blockers:** an authentic public Combat cycle; admitted published content;
all reachable six-turn rules and victory; process-durable save/resume; side-safe playable Maproom
including seat isolation; and two retained complete six-turn games. Existing `MVP-GATE-01` through
`06` express these boundaries correctly. The audit proposes delivery hygiene gate `MVP-GATE-07`
for distribution readiness; it adds no gameplay scope. A developer Aspire launch and website
deployment do not prove player delivery.

**Evidence debt:** [pin-maintenance inventory](combat-verification-pin-maintenance.md) records the
20-file live repair closure, recursive Snapshot and cycle-source failures, and independent outward
Content drift. [session-two handoff](../work/handoffs/2026-10-05-combat-session-two.md#blockers-and-limitations)
retains unresolved historical timeouts and 51 baseline Markdown failures. The separately admitted
actual-selection contract bypasses neither the failed evidence nor later public admission gates.
Before each consuming gate, repair its required dependencies or record an explicit reviewed
replacement with equivalent failure sensitivity. Failed checks cannot be counted as passing.
The historical Markdown failure count records the earlier session; this audit does not carry that
count forward as current debt. Coordinator integration checked the complete offline Markdown corpus after
link cleanup: zero errors, versus65 at the audit baseline. External/offline exclusions remain unchanged. The Runner aggregation research is a diagnosis gap, not a demonstrated runtime fix; reproduce and
classify it before depending on broad clean evidence.

**Unknown size, not permission to omit:** complete required map extent/unit/arrival record counts;
reachable Organization/reorganization, logistics abstraction, supply/water/fuel/ammunition,
shipping, truck/rail, repair/towing, patrol/construction and other Land obligations; adopted ruling
count; full table-coordinate coverage; performance at scenario size; supported release platforms;
durable provider/fencing behavior; and distribution rights. `MVP-GATE-01` must classify each item as
required, explicitly empty under sourced setup/rules, or outside Land-only scope. Mere absence from
the laboratory cannot justify a silent bypass or invented approximation.

Original presentation assets and keeping scans out of Git are already sound project constraints.
They do not settle permission for distribution of derived content or other rights-sensitive use.
The retained [rights uncertainty](cna-source-material-spike.md#unknowns) needs an owner-reviewed
release posture. Tracked inventory found no `LICENSE` or `NOTICE`; decide licensing and applicable
dependency notices before distribution rather than assuming the public repository grants them.
No new legal conclusion is made here.

## Dependency-ordered release work

These are planning packets, not authorization to activate gameplay or select new runtime
dependencies. Preserve governing requirements, owner decisions, contract-first ordering and fresh
review gates. Each broad packet must be split into one contract or one native vertical slice, usually
at most five primary files, before dispatch. Checkpoints require executed evidence, not checked boxes
based on source presence.

| Packet / priority | Advances and dependencies | Acceptance / verification | Estimated scope and uncertainty |
| --- | --- | --- | --- |
| `REL-AUD-01` / P0: native actual selection | FID/DET/EVT; accepted Task019F0 contract and actual opening owners | Native consumer matches all frozen Force-Assignment/no-attack traces, canonical cuts/retries/rejections and independent input-ledger boundary; focused native tests, unchanged oracle, full gate, fresh review. Does not complete Force Assignment or rounds. | One medium native packet; budget 1–3 focused sessions including review/gates, not the previous 90-minute coding allowance as a guarantee. |
| `REL-AUD-02` / P0: actual round/result join | REL-AUD-01; remaining017–019 actual C3a/Round2/Result2 contracts | Contract first for authentic assignment/order/cost/result/settlement; every admitted random outcome has obligations implemented or pre-mutation rejection. Replay actual creation-rooted histories; cannot relabel synthetic earlier Movement. Focused branch/conservation/tamper tests and full gate at each native slice. | Several contract/native packets; number remains contingent on the reviewed join inventory. Re-estimate after first authentic round. |
| `REL-AUD-03` / P0: complete lineage and public skeleton | REL-AUD-02 plus later-II/consumed Reserve lineage and replay/recovery obligations | Remaining017–019 close;020–021 admit only certified side-safe public actions;022–024 reconstruct/readjudicate and repeat clean runs;025 reconciles all72ACs and authentic repeat/finish. Required pin dependencies and Runner failure classified before counting evidence. | Multiple sequential packets across Core, observation/action, Exercise and Runner; no single-session estimate. Use existing Combat task IDs as implementation owners. |
| `REL-AUD-04` / P1: scenario-surface inventory | SRC/FID/IPR; MVP-GATE-01 formally follows skeleton; source/rights preparation can begin independently | Requirement-to-source matrix for all six turns/18stages, both sides, setup/arrivals/supply/repair/termination/victory; exact records/coordinates and unsupported counts; each obligation maps to implementation, sourced emptiness or explicit exclusion. Independent source cross-check; freeze bounded task graph before coding. | Initial inventory 1–3 research sessions is a planning allowance, low confidence; larger transcription/ruling discovery expands it. This packet determines the missing full-release estimate. |
| `REL-AUD-05` / P1: source-derived content admission | REL-AUD-04; MVP-GATE-02; approved rights/content workflow | Contract/schema extensions first where current types lack required facts; stable IDs/canonical hash/reference validation; double-entry or independent setup check; source/errata/rights manifest; historical byte compatibility. No copied scans/art/prose. | Data volume unknown until04. Estimate per measured content batch, with validation and independent checking as explicit work. |
| `REL-AUD-06` / P1: required Land slices | REL-AUD-04/05 plus certified skeleton; MVP-GATE-03 | One reachable obligation family per contract/native/public/evidence slice; both seats, later stages/turn rollover and required abstract-mode behavior; all table outcomes, CP/BP/supply conservation, obligation ordering, replay cuts and unsupported rejection. | Unknown number of rule families; likely dominant release uncertainty. Laboratory Movement cost is not a proxy for full shipping/supply/repair work. |
| `REL-AUD-07` / P1: termination and victory | Sourced victory contract from04/05, relevant06 authority | Exact end of turn6/stage3, all published victory levels and Land-only adjustments; boundary/tie cases; independent setup/result cross-check; no post-terminal mutation; replay identical verdict. | Separate medium contract plus native packets; tentatively 2–4 sessions, low confidence until sources and dependencies are frozen. |
| `REL-AUD-08` / P1: durable local lifecycle | HOST-PUB-001; accepted storage/publication decision, public021 plus verified023trace; MVP-GATE-04 | Freeze commit/head/receipt/checkpoint and recovery/authenticated-ingress contracts. Implement chosen local provider and host boundary; durable ack, duplicate/stale commands, reply loss, partial failure, kill/restart, checkpoint lag and historical-version recovery. Disable new admission without disabling reads/replay/recovery. | Provider choice and operational model unresolved. Plan 3–6 bounded contract/native/failure-test packets, not 3–6 guaranteed hours. No cloud or multi-silo deployment is required by local MVP. |
| `REL-AUD-09` / P1: no-model Maproom prototype | UX/FOW; skeleton then approved representative Command/Staff/Umpire classification | Prototype map/list/form controls and synchronized typed draft using synthetic data; confirm intent, Staff review and final current action separately; every task completes through structured controls; hot-seat cleanup including DOM/storage/late responses. Run specified no-model interaction/privacy/accessibility gate. | 2–4 small/medium prototype and evidence packets; UI stack and plan-to-action contract remain decisions. Parser benchmarking is outside this estimate. |
| `REL-AUD-10` / P1: complete Maproom campaign path | REL-AUD-08/09 plus stable public06/07 authority; MVP-GATE-05 | Create/resume, formation inspection, legal submission, stale rejection, private handoff, readable Chronicle reasons and save/recovery feedback. Keyboard/accessibility and browser privacy negatives; full game with Intelligence disabled/unavailable. | Several vertical UI/host packets; exact count follows prototype and scenario decision inventory. No website redesign can replace this work. |
| `REL-AUD-11` / P1: first-release fidelity gate | REL-AUD-05–10; MVP-GATE-06 | Two complete clean six-turn hot-seat games with save/resume, correct independently checked setup/victory, identical replay, zero reachable unsupported mechanics, and fog/privacy gates. Retain build/rules/content hashes, commands/events, runbook, failures and review verdicts. | Reserve at least two distinct playthrough/evidence sessions plus correction cycles; duration depends on measured decision count. Physical-game25–50h estimate is not a digital estimate. |
| `REL-AUD-12` / P1: package and launch gate | REL-AUD-11 plus rights/licensing/support decisions; proposed MVP-GATE-07 | Choose local installation/launch/update posture and supported OS/runtime/browser matrix; package exact version/hash with allowed content/notices; fresh-machine create/play/save/quit/resume smoke; documented save location, backup/recovery, compatibility and known limitations. Build package in CI; readiness review before tag/publication. | 1–3 small/medium packaging/verification packets after platform choice, low confidence. Research/support decisions can start earlier. |

Graph: `01 → 02 → 03 → 04 → 05/06 → 07`; eligible `public021 + verified023 → 08`;
`03 → 09`; `06/07/08/09 → 10 → 11 → 12`. Durable hosting may begin at its existing eligible
public-action/trace boundary before the entire03/025 closeout, after its storage decision is accepted.
Preparatory source/rights and packaging research can proceed alongside Combat without promoting
their implementation status. Do not couple local deterministic hosting to Intelligence dispatch,
model providers, cloud clustering, or remote-player notifications.

## Effort calibration and checkpoints

Estimates are ranges for bounded packets, not calendar promises or a sum of sprint numbers.
A focused session here means roughly 1–3 hours of active work with retained evidence and handoff;
gate/review/CI and recovery can extend elapsed time. It is a planning unit, not a claim of measured
uniform productivity.

Observed anchors: [overnight plan's measured history](../work/plans/2026-10-05-combat-overnight.md#verified-baseline-and-evidence)
records 29.5 min released-I completion, 24.4 min settled proof contract, 41.4 min native settled proof and
28.0 min settled-control contract. Coordinator session review recorded roughly 111 min native positive
entry including review/CI and roughly 2.5 h actual-selection contract with four reviews and diagnostic
recovery. [session-two handoff](../work/handoffs/2026-10-05-combat-session-two.md#decisions-and-rationale)
records native selection deferred because90–120min plus review/CI no longer fitted protected
closeout. These narrow, prepared predecessors are not evidence that new scenario systems take the
same time. Git first-parent history confirms Movement/Reaction delivery spread over distinct
contract, native, public, Runner and review changes (PR69/72/77/78/79/81/84/87); Combat has likewise
needed many independent seams since PR89. Merge spacing includes idle/review time and squash
history hides starts, so it cannot be used as coding throughput.

Planning checkpoints:

1. After REL-AUD-01, update native-selection status only; retain actual round/result/public gaps.
2. After REL-AUD-03, declare skeleton only with025evidence, then measure04 and publish a
   packet-count estimate for05–07. This is the earliest defensible whole-scenario forecast.
3. After first full turn and provider kill/restart evidence, re-estimate remaining authority and
   lifecycle work from observed reachable branches/data, not test counts.
4. After no-model prototype, estimate Maproom from real decision forms, handoff and accessibility
   evidence; reject shortcuts that expose private previous-seat state.
5. After two full games, close06, then packaging07. A first game is an integration rehearsal;
   failures create bounded correction tasks and rerun affected acceptance evidence.

## Documentation and metadata reconciliation

Canonical roadmap cleanup in this change corrects stale “Sprint 4 active” wording and older Sprint 5
summaries, links this whole-release audit, and adds explicit packaging acceptance. Historical review,
source and session evidence remains tied to its original checkpoint; it is not rewritten as current
completion. [Content Pack v1's current-evolution note](../specs/content-pack-v1.md) still says
Movement/contact unimplemented. That document records an older boundary; use a supersession note
in the documentation index/current summary before reusing it for onboarding. Any change to
hash-pinned historical specification bytes needs its own dependency-aware maintenance scope.

Coordinator read-only GitHub metadata check on2026-10-05 found no published releases and one open
issue, [#63 BREAKDOWN-001](https://github.com/dills122/sandtable/issues/63). Its original decision
question appears stale against owner adoption on2026-08-29 in
[Movement spec](../specs/movement-foundation-v1.md#owner-approval), the
[approved continuity ruling](breakdown-continuity-spike.md#executive-conclusion), and executable
[Breakdown rules tests](../../tests/Cna.Core.Tests/Rules/BreakdownRulesTests.cs), sequential-dice
ruling assertions. Recommend reconciling or closing that stale tracker question after confirming
exact issue body/state; it is not a current release blocker. Preserving accepted decisions in
canonical docs is sufficient for this audit. No GitHub comment, closure, tag, or release is sent.

## Audit verification record

- Base/source: `af37155`; branch `codex/release-planning-audit`; surveyed managed checkout
  `combat-session-two-sync/sandtable`. Parent owns concurrent doc/link edits; audit owns only this
  report and roadmap changes.
- Executed: graph project/coverage queries; `rg --files` inventories; focused source/test/config
  reads; `git log --first-parent` and retained session-history comparison. Verified no Maproom
  project in `Sandtable.slnx`, current host/worker registrations, current action/exercise route and
  selected content catalogs. Negative claims are limited to surveyed tracked scope.
- Fresh documentation checks: `git diff --check` passed; a scoped local file/heading checker
  validated 193 links across this report and roadmap with zero issues. Coordinator owns the final
  full-corpus check after integration; earlier link failures remain historical evidence and are
  not presumed to persist in the corrected corpus.
- Not executed: new runtime/tests/build/service launch, full scenario transcription, fresh original
  contract-oracle sweep, independent review or player playthrough.
- No runtime code, contract bytes, assets, dependencies, historical golden evidence, GitHub metadata
  or released capability changed by this planning audit.
