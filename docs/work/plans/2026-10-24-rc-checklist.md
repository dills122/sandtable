# RC checklist — October 24, 2026

Current merged baseline: PR336 (`0f8a828a`). Status describes retained evidence,
not a public support certificate or a playable RC declaration.
The [roadmap](../../roadmap/pre-alpha-roadmap.md) and canonical acceptance
contracts remain authoritative. Every closure needs tested commit, exact profile,
command/exit/counts, retained artifacts and reviewer disposition. A historical
pass does not prove a later head.

Retained crash packet: abrupt-process Reserve fallback recovery on merged PR333
(`4017d78c`). [Crash handoff](../reviews/2026-10-25-reserve-crash-recovery-handoff.md)
records two deterministic kill points: after Reserve transactional writes before
Cycle entry/commit, and after commit before response. Fresh-process recovery and
retry must match untouched or fully committed control, preserving owner choices
and paid history. This is process termination evidence, not machine power loss.
PR333 merged the earlier three fresh-process proofs; remaining lanes/crash cuts,
public certification and playable-product gates remain open.

Retained evidence-maintenance packet: versioned private Combat dependencies. Legacy/archive
replay, refreshed native parity, rejection of mixed revisions and original oracle results
must all be retained before closure. [Revision contract](../../specs/combat-evidence-revisions-v1.md)
and [preflight](../reviews/2026-10-25-combat-source-pin-preflight.md) record scope. Review5's
docs-only repair was rejected; user approved native compatibility work and final review6.
Original Cycle/Breakdown root oracles now pass. Native focused batches659 cases and format/build pass; independent review6/6 Ready with non-blocking follow-ups. Snapshot pins and Outward anchors/pins subsequently reconciled in merged PR335 (`72a167f7`); full release-head coverage remains a separate gate. [Exact validation](../reviews/2026-10-25-evidence-revisions-validation.md) separates passing, failing and unrun checks.

PR334 CI exposed missed original-P01 consumer coverage and 16 missing measured
Core timing rows. Correction retains original P01 pins, separates refreshed R03
parity, and adds actual measured rows without relaxing coverage checks.
[CI correction evidence](../reviews/2026-10-25-pr334-ci-correction.md) records
selected validation and remaining exact-head CI/review requirements. Prior review6
does not cover this corrective patch.

RoundEntry consumption recheck correction: original five regressions now pass
unchanged; full RoundEntry177 and refreshed native R03 parity300 pass in focused
chunks. Owned snapshots explicitly recheck Content through original provider at
use, including warm replay. Six affected schemas and refreshed fixtures regenerated;
legacy registrations/archives and original P01 goldens preserved. Final review8/8 returned **Ready**, no actionable findings, at91b313c3.
Strict local inventory and all reported final-head hosted checks passed; PR334 merged as `38bf158d`. [Correction evidence](../reviews/2026-10-25-roundentry-consumption-recheck.md).

## Native Combat reliability

These are bounded implementation gates. Recheck them together on the release
head; they do not admit original P01 roots or close product gates.

| Gate | Current evidence | Pass criteria |
| --- | --- | --- |
| RBA deadline | Implemented `cbc6954d` | Deadline retained at Barrage entry; timeout cancels without decline, new roll or debit; moves retained; no-query restart, stale proposal and replay checks pass. |
| Force deadline | Implemented `80d2881d`, `7d0f34fd`; invalid proposal fix `fb79b785` | Shared round starts at accepted decision entry; both owner seats use original deadline; zero/one seal cancellation, background restart, invalid proposal non-mutation and binding checks pass. |
| Cycle-control deadline | Implemented `d7993d77` | Completed Release starts deadline; System finish enters same-slot Truck Convoy Movement with unchanged World/RNG; owner race, cold replay and repeated ordinals pass. |
| Reserve Release deadline | Implemented `ac1cab54` | Due I converts to II, due II retained; accepted owner releases/allowances unchanged; immutable deadline, exact System batch, atomic Cycle entry, cold replay and invalid/tampered cases pass. Genuine cycle2 and cycle3 Host evidence retained: actual history proves initial System conversion and third-cycle retention, saved reopen, exact boundary, owner-record preservation and atomic rollback/retry. Fresh-process third-cycle Reserve now passes before/exact/retry boundaries with full journal parity and Finish continuation; bounded precommit/postcommit crash proof merged in PR334; arbitrary crash cuts remain open. |
| Custody deadline | Implemented `73e3af6f`; initial and genuine repeated single-lot expiry proved | Source-legal leave-unguarded effects recorded as System; settlement-bound immutable entry deadline; no invented guard debit/owner receipt; owner race, no-query restart, duplicate expiry, cold replay, row tampering and rollback pass. Actual repeated seed50 captured lot binds second settlement, escaped replacement state and unchanged paid cursor. Fresh-process before/exact/retry recovery now passes with three child launches, full journal parity and no owner Custody query. Exact initial seed5/repeated seed50 Custody-entry clock failure now passes with leave-unguarded fallback and preserved paid history. Every admitted cut has a legal fallback; unsupported routes/lots remain explicitly excluded. |
| Required post-assault Retreat deadline | Implemented `38c17170`; initial and genuine repeated Host expiry/rollback proved | Result-entry immutable deadline; System `refuse-retreat` applies actual refusal settlement losses; no-query cold expiry, exact boundary, rejected late proposal non-mutation, owner retry, clock faults, tamper and rollback checks pass. Actual second required-Retreat expiry now matches legal owner-refusal World through verified replay. Fresh-process before/exact/retry recovery passes with three children, original deadline, full journal parity, unchanged owner records and paid RNG18. Exact initial seed6/repeated seed4 Retreat-entry clock failure now passes with refuse-retreat fallback and preserved paid history. |
| Selection deadline | Implemented `ee1d7645`; initial and genuine repeated paths | Deadline retained at actual initial or repeated Breakdown entry; System `no-selection` distinct from owner Finish, unchanged World/RNG, no-query cold expiry, owner race/retry, invalid proposal non-mutation, generic-Append gating, atomic no-attack/Reserve continuation and strict replay pass. Preserve readonly historical schemas and fail closed for unsupported fallback scope. |
| Integrated durable Combat flow | Saved-copy/fresh-process cuts, genuine third-cycle Reserve, repeated captured-lot Custody and required-Retreat expiry/rollback pass; broader gate open | One retained genuine accepted history exercises initial and repeated combat, unpaid decisions and background fallbacks, restart at nonterminal cuts, Reserve I/II effects, cycle repeat/finish, and identical replay. Fresh-process initial Retreat and repeated Selection/RBA/one-seal Force now pass before/due/retry boundaries; remaining native decision cuts and abrupt crash/power-loss recovery are not established by this packet. Genuine third-cycle Host Reserve timeout now passes after two paid assaults and two accepted repeats; initial conversion and later retention respect ordered owner choices. Fresh-process third-cycle Reserve now passes using three native child launches; other Reserve process cuts remain separate. Genuine repeated captured-lot Custody expiry and fresh-process before/due/retry recovery now pass with actual two-paid history and fixed seed50; this does not admit other profiles. Exact initial/repeated Custody entry-clock faults now pass using persisted-round calibration. Genuine repeated Host required-Retreat timeout and fresh-process before/due/retry recovery now pass on actual seed4 two-paid history with legal owner-refusal World parity. Exact initial/repeated Retreat entry-clock faults now pass using persisted-round calibration; prior Force succeeds, unavailable fallback invents neither timestamps nor target rounds. No fabricated P01 ancestry. |

### Abrupt-process persistence gate

Third-cycle Reserve precommit and postcommit/lost-response kill cases pass in
bounded fixture. Retained repeated captured-lot Custody packet adds both boundaries;
all14 focused tests and build/format passed. Parent uses OS termination while child is paused, then new
processes recover/retry; full logical journal/replay parity excludes partial state
and duplicate fallback. Other lanes, arbitrary write/fsync cuts and power loss
remain unproved. Repeated required-Retreat now also passes both process-kill boundaries with actual
seed4 history and legal owner-refusal World parity. No production crash hook or
changed persistence contract.

## Release evidence and publication

| Gate / owner lane | Current status | Explicit pass criteria |
| --- | --- | --- |
| Source-oracle reconciliation / Core evidence | Cycle/Breakdown roots pass; Snapshot and dependent Outward reconciliation merged in PR335 with all48 PR-head checks green; full release-head owning-oracle coverage pending | Identify semantic source changes; document contract/source implications; update pins only with reviewed justification; applicable original oracles exit0 on release head. Never regenerate solely to silence failure. |
| Original P01 certificate / evidence | 300 ancestry rows checked historically:44 provisional positive,256 absent,0 admitted | Close canonical AC-001–009 with original-root ancestry, action/obligation completeness, source conditions, authentic owner privacy, matching nonterminal recovery/fault/cap evidence and support review. Absent rows remain unsupported; terminal ancestry alone cannot close certificate. |
| Public Combat / Core+Host | **Status correction, 2026-11-21:** no longer "open" as stated. Tasks 020-021 merged (commits through PR #483); the real Host API (`CampaignDurableAuthorityJournal.Openings.cs`/`.Submissions.cs`) now exposes and resolves a full first-side Combat cycle, confirmed by passing Host tests including cold replay. `HOST-PUB-001` publication-evidence closure was not independently re-checked in this pass and should be treated as unverified rather than assumed closed. See the [2026-11-21 readiness check](2026-11-21-initial-engine-release-readiness-check.md) for current per-item status with citations. | Admit only certified profile; equal-authorized hidden variants yield safe outward actions/results; unsupported profiles reject before decision. Prove atomic actual persistence, retries, lost responses and fresh-process recovery; disabling admission preserves recovery. |
| Runner/studies / evidence | **Status correction, 2026-11-21:** Tasks 022-023 (Exercise Harness Combat adoption/bundle scoping) merged since this row was written; 024-025 remain open. This row is about `Cna.ExerciseRunner` only, a test-automation lane — it does **not** gate or describe the real Host API's Combat status, which is tracked separately above. | Runner consumes same admitted public authority; strict reconstruction/readjudication produces retained repeat/finish trace; independent clean empty/settled-assault/repeated-Movement studies pass; all72 Combat ACs reconciled against actual accepted loop. |
| Dependencies / Maproom | Partially remediated. PR360 bumped Angular 22.2.3→22.2.7, fixing the direct Vite advisory; production dependencies now audit clean (`npm audit --omit=dev`: 0 vulnerabilities, checked 2026-11-17). fast-uri bump already merged separately. Still open: 26 high-severity transitive vulns (vite/picomatch/tinyglobby) via `@angular/build`'s own nested Vite, Storybook `addon-docs`, vitest, and `unplugin` — dev-toolchain only, not shipped, but unresolved; npm peer resolution rejected the full coordinated Angular bump that would clear them (per 2026-11-16 dependency-audit notes). | Compatible reviewed lockfile remediation for the remaining dev-toolchain chain, or an explicit reviewed disposition that these are non-shipping and acceptable to carry; no unresolved release-blocking exposure; `just maproom-check` and relevant browser/privacy tests pass. Clean production audit alone does not close this — dev-toolchain exposure still needs a disposition. |
| Release validation / integration | Focused packets green locally; exact-head CI outstanding | Release commit passes all partitioned Core rows, boundary/Host/Runner/contract checks, format/build, applicable Python oracles and frontend checks. Retain actual row-discovery coverage and zero missing/duplicate rows; independent release review has no material open findings. Do not substitute local focused passes for full release evidence. |

See [certificate matrix](../reviews/2026-10-24-rc-certificate-status.md),
[dependency triage](../reviews/2026-10-24-maproom-dependency-triage.md), and
[oracle pin policy](../../research/combat-verification-pin-maintenance.md).
Earlier packet review allowance exhausted3/3. User subsequently requested full
PR review, explicitly extending to instance4of4: [full review](../reviews/2026-10-24-pr329-full-review.md)
returned Ready with non-blocking follow-ups for bounded merge scope, conditional
on documentation correction and final-head CI. No public release approval.

## Playable RC gates

A bounded engine checkpoint is not the playable six-turn release. These retain
existing roadmap gates; no scope reduction is inferred from finishing Combat.

| Roadmap gate | Current status | Evidence required to pass |
| --- | --- | --- |
| MVP-GATE-01 scenario freeze | Initial inventory; reconciliation open | Exact six-turn/18-stage reachable rules, decisions, content and source matrix; explicit unsupported count and rulings; reviewed implementation task graph. |
| MVP-GATE-02 content | Partial source data; not admitted | Versioned source-verified Graziani setup/map/arrivals/supply/repair content; stable IDs/hash, reference validation, independent setup cross-check, provenance/rights acceptance. |
| MVP-GATE-03 authority | Incomplete | Every reachable mechanic and victory condition executes deterministically through Core; table/branch/replay/conservation tests pass; zero silent approximations. |
| MVP-GATE-04 local lifecycle | Bounded journal implemented; product gate open | Create/checkpoint/stop/resume/recover through supported interface; process faults and schema compatibility pass; recovery and diagnostics documented; no model/I/O in authoritative turns. |
| MVP-GATE-05 Maproom/hot-seat | Foundation/read preview; acceptance open | Two seats can inspect, choose, confirm, submit and hand off privately without model; stale-action, DOM/storage/log privacy and accessibility end-to-end checks pass. |
| MVP-GATE-06 full playthrough | Not demonstrated | Two independently checked complete six-turn games, including save/resume and correct victory, reproduce from seeds/events; no reachable unsupported mechanics; retain build/content/test hashes. |
| MVP-GATE-07 delivery | Open | Approved distribution/rights/notices; CI-built versioned package with hashes and supported-platform matrix; fresh-machine launch/create/play/save/quit/resume smoke; backup/save location/compatibility instructions. |

MVP-GATE-07 note: superseded 2026-11-19 by PR #444 (Phase C4 of #439), which configures Orleans AdoNet clustering/reminders against Postgres for non-Development environments; the Orleans Host is no longer limited to a single dev machine for clustering. Distribution/rights/packaging/fresh-machine-smoke evidence for this gate remains open regardless.

### November 13 scenario and arrival working packet

Matruh and 6th NZ printed conflicts have a bounded independent transcription;
working interpretations and the full scenario source/rights review remain open.
The [setup reconciliation](2026-11-13-graziani-setup-reconciliation.md) records
supply and ruling boundaries, and a [blind supply packet](graziani-supply-review/README.md)
awaits a second reading, as does the [truck-row packet](graziani-truck-review/README.md).
The [six-turn arrival packet](graziani-arrival-review/README.md) and the
[Axis formation packet](graziani-axis-formation-review/README.md) also await
independent field-level entry; the [rules packet](graziani-rules-review/README.md)
also awaits an independent reading, as does the [victory packet](graziani-victory-review/README.md).
The [arrival laboratory](../../specs/scheduled-arrival-lab-v1.md)
implements an isolated synthetic transition/replay exercise. A separate
[World6 successor](../../specs/campaign-arrival-world6-successor-v1.md) now
connects one synthetic arrival to an internal authenticated, durable Host
journal path, with native preamble bytes in versioned records and replay,
backward-compatibility and retry checks. It closes no playable-RC gate:
normal campaign admission, native successor events, public player actions and
observations, historical content and full privacy checks remain open. The
[session handoff](2026-11-13-rc-session-handoff.md#synthetic-world6-durable-journal-checkpoint)
records the scope and evidence.

The production content model now represents a source-linked scheduled arrival
in memory, tested with the 6th NZ artillery timing. A separate canonical content
format now preserves and hashes these schedules; legacy formats reject them.
The combat-fact successor also preserves scheduled seeds and selected printed
gun characteristics, but does not implement the corresponding weapon rules or
campaign admission. See the
[content boundary](../../specs/campaign-scenario-arrival-content-v1.md).

### November 14 working tree: normal engine progression

Uncommitted changes correct Combat ownership when initiative holder acts last,
and add explicit owner passes and second-side Reserve and one-hex Movement to
the ordinary Core and durable Host paths. The latter retains any triggered
Reaction window; movement completion advances to Breakdown only when no
Reaction is pending. **Update (2026-11-17, corrected):** Breakdown (V46,
`CampaignNativeSecondBreakdownV46.cs`) is implemented and tested, and its
checkpoint reports a successor Combat `PositionDetermination` position for
the second acting side — but that is not a working hand-off (GitHub issue
#394, reproduced this session). `CurrentSnapshot` is never advanced by the
native V41–V46 `With*` chain and `CurrentCombatDecision` is left holding the
stale decision from the first-acting-side's earlier combat, so the normal
owner-cut path rejects with an unrelated "already advanced" error; separately,
the Combat-selection authority gates (`CampaignContactCombatSelectionCut`,
`CampaignNativeCombatSelectionTimeoutV38`) hard-require
`LandActorRole.FirstActingSide` and would reject second-side entry even with
a correct snapshot, and the Host has no second-side Combat opening kind
wired up. This needs a scoped design/implementation pass on central Combat
authority code, not a narrow fix. The Reaction-window-resolution gap and
vehicle-cohort breakdown accounting remain additional, separate gaps.
These changes do not advance the scenario clock to the Polish brigade's
turn-1/stage-3 arrival, much less later reinforcements.

Real Graziani creation remains blocked by the synthetic-only normal creation
and snapshot contracts, incomplete source-reviewed map and forces, unsupported
historical weapons/attachments and missing six-turn rules. Source-linked partial
data and a constructible initial-world excerpt are not admitted scenario
content. The existing movement artifact recognizes only clear and desert
terrain; Cairo's major-city terrain and the Delta/river crossings require a
versioned rules successor, not a change to saved games' pinned rules. Full
playable gates remain open. Focused tests and the boundary suite
are local working-tree evidence only; exact-head full validation, independent
review and CI remain outstanding.

The normal durable Host journal also supports a versioned
[source-initialization record](../../specs/campaign-source-creation-v1.md),
retaining exact source-content bytes, placed World8 state and pending group IDs
through cold replay. A source-backed Cairo excerpt exercises that version 2 path;
it is incomplete and has no player seat, gameplay progression or arrival execution.
The source-only format can preserve Cairo major-city identity without
adding movement rules. This does not close campaign admission or MVP-GATE-02.
The Commonwealth OA and characteristics charts also provide allowances for
the excerpt's 16 counters from the 16th, 7th Indian and Polish brigades, plus
26 more arrival and initial-formation candidates. These are source-linked
production facts, not completed scenario setup. A separate independent reading
is still open; Central India Horse's starting training state remains unresolved.

The separate version 3 [pre-game supply cut](../../specs/graziani-pregame-supply-v1.md)
adds a provisional Axis-only GT2 supply roll and partial allocation choices to the
normal session and Host journal. Its source-backed Cairo input remains incomplete;
setup cannot finish, the game clock cannot advance, and no arrival executes. The
date and RNG-order policy need review and this closes no playable gate.

Source-linked working candidates now include the French Motor Marine company,
16th Infantry Brigade, the retained 4th Indian and 7th Armored members, Maletti,
Catanzaro, 1st and 4th CCNN, and the Aresca/Tripoli tank-regiment subsets.
These records retain printed strengths where read, not a complete setup. The
Maletti/LX(L) split, conditional HQ/engineer values, training-dependent Central
India Horse and many attachment, map and supply choices still need authority.
One same-reader check corrected 201st Artillery's heavy AA seed from three to
two; the new entries still require independent source review.

Local November 14 validation: Release solution build succeeded with one
`ASPIRE010` warning about the disabled Aspire CLI bundle. The Host suite passed
251 tests on the current production implementation; later focused Host tests
also verified movement completion through HTTP, retry, a fresh process and a
corrupt predecessor. Runner passed 470, boundary 81, and contract suites 18
tests. Source-linked content, initial-world and ZOC classes passed 65 focused
tests. The full Core suite passed 3,822 tests. These results do not establish
historical admission, exact-head CI or a playable RC.

## Merge checkpoint and follow-ups

[RC follow-up tracker #330](https://github.com/dills122/sandtable/issues/330) carries
remaining work with owner lanes and explicit pass criteria. PR329 merged as
`fc4cb925` after48 final-head checks passed and full independent review completed.
That bounded reliability checkpoint does not close release, certification or
playable-product gates. Known
Breakdown/cycle source-pin failures predate this PR: verifier files and pinned
`Cna1979LandSequence.cs` are unchanged from base `16d76448`. Those historical
failures were not waived; PR334 subsequently reconciled and passed
the original Breakdown/cycle oracles. Release-head oracle coverage remains open.
New PR regressions must be fixed before merge.

## Next execution order

1. Preserve merged [PR329](https://github.com/dills122/sandtable/pull/329) evidence
   and validate subsequent bounded packets on their own heads. All seven decision
   windows exist; prior review/CI does not automatically cover new changes.
2. Prepare the six-turn scenario surface for MVP-GATE-01: reconcile reachable
   rules, decisions, content and source/rights status; count unsupported items and
   propose an implementation task graph. Use that inventory to prioritize remaining
   work against the playable RC; gate acceptance retains the roadmap's dependencies.
   Start with the [working inventory and execution map](2026-11-12-graziani-playable-rc-inventory.md)
   and [next work order](2026-11-12-graziani-rc-work-order.md).
3. Close remaining integrated durable-flow proofs and release-head oracle coverage;
   refresh dependency inventory and remediation in separately scoped packets.
4. Close original/public certificate and Runner evidence gates; freeze exact RC
   support profile before any public admission.
5. Progress remaining scenario/product gates; run release-head partitioned CI and
   independent review at a release checkpoint, not after every small edit.

Use Ruflo namespace `sandtable-rc-20261023` for assignments and receipts. During
iteration run focused classes/methods and affected consumers; full Core belongs
to integration/release checkpoints. Do not merge, promote unsupported rows, or
claim RC readiness while applicable gates above remain open.

## October27 Snapshot reconciliation

PR334 and PR335 merged; Snapshot reconciliation retained at main `72a167f7`. User
approved dependent evidence refresh, native revision compatibility and review9/9.
Snapshot's two runtime pins now match reviewed source changes; owning Snapshot
oracle passes368 cuts,6992 mutations,6992 omissions and2576 raw negatives.

Pass criteria and evidence:

- [x] Explain source changes; preserve Snapshot serializer/projector behavior.
- [x] Preserve22 legacy/P01 files and all27 prior native hash registrations.
- [x] Archive161 exact PR334 dependency files as a separate immutable revision.
- [x] Reject altered source pins and mixed revisions; replay retained PR334 cuts.
- [x] Pass focused native consumers: Selection60, Snapshot24, PR3341, legacy33,
  Core P0110, Host P0146, boundary81, RoundEntry177, current R03 parity300.
- [x] Pass Release solution build, changed-C# format,21 inherited owning oracles.
- [x] Finish dependent B1/B3/Outward oracle checks and final semantic comparison.
- [x] Final independent review9/9: Ready, no actionable findings.
- [x] PR335 all48 checks passed at `1cac9de8`; merged as `72a167f7`. New packets need their own exact-head CI.

[Evidence and validation scope](../reviews/2026-10-27-snapshot-evidence-reconciliation.md)
records source rationale, preservation and exact selected/unrun checks.
No public certificate, Outward runtime acceptance or release-readiness claim follows.

## Repeated Custody abrupt-process recovery

Retained bounded #330 Host packet: actual two-paid, repeated captured-lot Custody.
[Boundary and evidence](../reviews/2026-10-27-custody-crash-recovery.md).
All14 selected tests, Host Release build and changed-file format passed.
Initial coordinator review retained; combined independent review10 is Ready.
PR336 passed all 48 checks on PR head `687e80bb` and merged as `0f8a828a`.

- [x] Precommit process kill preserves original committed journal; fresh recovery
  applies leave-unguarded once.
- [x] Postcommit/lost-response process kill retains complete fallback; fresh retry
  adds no event or state-version change.
- [x] All14 logical tables/replay hashes match healthy control; exact lot, World,
  owner records and paid RNG preserved.
- [x] Focused crash, existing fresh-process and Custody regressions; build/format pass.

No public admission or power-loss claim. Required-Retreat packet follows below.

## Repeated required-Retreat abrupt-process recovery

Current #330 Host packet uses genuine seed4 two-paid history requiring Retreat;
[proof and scope](../reviews/2026-10-27-retreat-crash-recovery.md).

- [x] Calibrated precommit OS kill preserves original journal; new process applies
  System refusal exactly once.
- [x] Postcommit/lost-response kill preserves complete settlement; fresh retry
  produces no additional event or version.
- [x] All14 tables/replay bytes/hashes match healthy control; settled World matches
  separate legal owner-refusal branch, with paid RNG and owner records unchanged.
- [x] New crash tests and existing Reserve/Custody/fresh-process/Retreat/default-child
  regressions pass; focused Host build and format pass.

All16 selected tests, Host build and changed-file format passed.
Combined fresh-context review10: Ready, no actionable findings.
[Review report](../reviews/2026-10-27-crash-recovery-independent-review.md).
PR336 passed all 48 checks on PR head `687e80bb` and merged as `0f8a828a`:
[hosted run](https://github.com/dills122/sandtable/actions/runs/40619680565).
Broader crash/certification obligations and release-head validation remain open.
