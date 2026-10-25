# RC checklist — October 24, 2026

Working branch: `codex/rc-reserve-crash-recovery`. Status describes retained
local evidence, not a public support certificate or a playable RC declaration.
The [roadmap](../../roadmap/pre-alpha-roadmap.md) and canonical acceptance
contracts remain authoritative. Every closure needs tested commit, exact profile,
command/exit/counts, retained artifacts and reviewer disposition. A historical
pass does not prove a later head.

Latest packet: abrupt-process Reserve fallback recovery on merged PR333
(`4017d78c`). [Crash handoff](../reviews/2026-10-25-reserve-crash-recovery-handoff.md)
records two deterministic kill points: after Reserve transactional writes before
Cycle entry/commit, and after commit before response. Fresh-process recovery and
retry must match untouched or fully committed control, preserving owner choices
and paid history. This is process termination evidence, not machine power loss.
PR333 merged the earlier three fresh-process proofs; remaining lanes/crash cuts,
public certification and playable-product gates remain open.

## Native Combat reliability

These are bounded implementation gates. Recheck them together on the release
head; they do not admit original P01 roots or close product gates.

| Gate | Current evidence | Pass criteria |
| --- | --- | --- |
| RBA deadline | Implemented `cbc6954d` | Deadline retained at Barrage entry; timeout cancels without decline, new roll or debit; moves retained; no-query restart, stale proposal and replay checks pass. |
| Force deadline | Implemented `80d2881d`, `7d0f34fd`; invalid proposal fix `fb79b785` | Shared round starts at accepted decision entry; both owner seats use original deadline; zero/one seal cancellation, background restart, invalid proposal non-mutation and binding checks pass. |
| Cycle-control deadline | Implemented `d7993d77` | Completed Release starts deadline; System finish enters same-slot Truck Convoy Movement with unchanged World/RNG; owner race, cold replay and repeated ordinals pass. |
| Reserve Release deadline | Implemented `ac1cab54` | Due I converts to II, due II retained; accepted owner releases/allowances unchanged; immutable deadline, exact System batch, atomic Cycle entry, cold replay and invalid/tampered cases pass. Genuine cycle2 and cycle3 Host evidence retained: actual history proves initial System conversion and third-cycle retention, saved reopen, exact boundary, owner-record preservation and atomic rollback/retry. Fresh-process third-cycle Reserve now passes before/exact/retry boundaries with full journal parity and Finish continuation; abrupt crash recovery remains open. |
| Custody deadline | Implemented `73e3af6f`; initial and genuine repeated single-lot expiry proved | Source-legal leave-unguarded effects recorded as System; settlement-bound immutable entry deadline; no invented guard debit/owner receipt; owner race, no-query restart, duplicate expiry, cold replay, row tampering and rollback pass. Actual repeated seed50 captured lot binds second settlement, escaped replacement state and unchanged paid cursor. Fresh-process before/exact/retry recovery now passes with three child launches, full journal parity and no owner Custody query. Exact initial seed5/repeated seed50 Custody-entry clock failure now passes with leave-unguarded fallback and preserved paid history. Every admitted cut has a legal fallback; unsupported routes/lots remain explicitly excluded. |
| Required post-assault Retreat deadline | Implemented `38c17170`; initial and genuine repeated Host expiry/rollback proved | Result-entry immutable deadline; System `refuse-retreat` applies actual refusal settlement losses; no-query cold expiry, exact boundary, rejected late proposal non-mutation, owner retry, clock faults, tamper and rollback checks pass. Actual second required-Retreat expiry now matches legal owner-refusal World through verified replay. Fresh-process before/exact/retry recovery passes with three children, original deadline, full journal parity, unchanged owner records and paid RNG18. Exact initial seed6/repeated seed4 Retreat-entry clock failure now passes with refuse-retreat fallback and preserved paid history. |
| Selection deadline | Implemented `ee1d7645`; initial and genuine repeated paths | Deadline retained at actual initial or repeated Breakdown entry; System `no-selection` distinct from owner Finish, unchanged World/RNG, no-query cold expiry, owner race/retry, invalid proposal non-mutation, generic-Append gating, atomic no-attack/Reserve continuation and strict replay pass. Preserve readonly historical schemas and fail closed for unsupported fallback scope. |
| Integrated durable Combat flow | Saved-copy/fresh-process cuts, genuine third-cycle Reserve, repeated captured-lot Custody and required-Retreat expiry/rollback pass; broader gate open | One retained genuine accepted history exercises initial and repeated combat, unpaid decisions and background fallbacks, restart at nonterminal cuts, Reserve I/II effects, cycle repeat/finish, and identical replay. Fresh-process initial Retreat and repeated Selection/RBA/one-seal Force now pass before/due/retry boundaries; remaining native decision cuts and abrupt crash/power-loss recovery are not established by this packet. Genuine third-cycle Host Reserve timeout now passes after two paid assaults and two accepted repeats; initial conversion and later retention respect ordered owner choices. Fresh-process third-cycle Reserve now passes using three native child launches; other Reserve process cuts remain separate. Genuine repeated captured-lot Custody expiry and fresh-process before/due/retry recovery now pass with actual two-paid history and fixed seed50; this does not admit other profiles. Exact initial/repeated Custody entry-clock faults now pass using persisted-round calibration. Genuine repeated Host required-Retreat timeout and fresh-process before/due/retry recovery now pass on actual seed4 two-paid history with legal owner-refusal World parity. Exact initial/repeated Retreat entry-clock faults now pass using persisted-round calibration; prior Force succeeds, unavailable fallback invents neither timestamps nor target rounds. No fabricated P01 ancestry. |

### Abrupt-process persistence gate

Third-cycle Reserve precommit and postcommit/lost-response kill cases pass in
bounded fixture. Parent uses OS termination while child is paused, then new
processes recover/retry; full logical journal/replay parity excludes partial state
and duplicate fallback. Other lanes, arbitrary write/fsync cuts and power loss
remain unproved. No production crash hook or changed persistence contract.

## Release evidence and publication

| Gate / owner lane | Current status | Explicit pass criteria |
| --- | --- | --- |
| Source-oracle reconciliation / Core evidence | Original Breakdown and cycle-sequence source-pin failures retained | Identify semantic source changes; document contract/source implications; update pins only with reviewed justification; applicable original oracles exit0 on release head. Never regenerate solely to silence failure. |
| Original P01 certificate / evidence | 300 ancestry rows checked historically:44 provisional positive,256 absent,0 admitted | Close canonical AC-001–009 with original-root ancestry, action/obligation completeness, source conditions, authentic owner privacy, matching nonterminal recovery/fault/cap evidence and support review. Absent rows remain unsupported; terminal ancestry alone cannot close certificate. |
| Public Combat / Core+Host | 020–021 and HOST-PUB-001 open | Admit only certified profile; equal-authorized hidden variants yield safe outward actions/results; unsupported profiles reject before decision. Prove atomic actual persistence, retries, lost responses and fresh-process recovery; disabling admission preserves recovery. |
| Runner/studies / evidence | 022–025 open | Runner consumes same admitted public authority; strict reconstruction/readjudication produces retained repeat/finish trace; independent clean empty/settled-assault/repeated-Movement studies pass; all72 Combat ACs reconciled against actual accepted loop. |
| Dependencies / Maproom | October24 inventory reports unresolved nested Vite, fast-uri and Angular builder findings | Fresh inventory, compatible reviewed lockfile remediation or explicit reviewed disposition, no unresolved release-blocking exposure; `just maproom-check` and relevant browser/privacy tests pass. Clean audit alone does not prove compatibility. |
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
| MVP-GATE-02 content | Not admitted | Versioned source-verified Graziani setup/map/arrivals/supply/repair content; stable IDs/hash, reference validation, independent setup cross-check, provenance/rights acceptance. |
| MVP-GATE-03 authority | Incomplete | Every reachable mechanic and victory condition executes deterministically through Core; table/branch/replay/conservation tests pass; zero silent approximations. |
| MVP-GATE-04 local lifecycle | Bounded journal implemented; product gate open | Create/checkpoint/stop/resume/recover through supported interface; process faults and schema compatibility pass; recovery and diagnostics documented; no model/I/O in authoritative turns. |
| MVP-GATE-05 Maproom/hot-seat | Foundation/read preview; acceptance open | Two seats can inspect, choose, confirm, submit and hand off privately without model; stale-action, DOM/storage/log privacy and accessibility end-to-end checks pass. |
| MVP-GATE-06 full playthrough | Not demonstrated | Two independently checked complete six-turn games, including save/resume and correct victory, reproduce from seeds/events; no reachable unsupported mechanics; retain build/content/test hashes. |
| MVP-GATE-07 delivery | Open | Approved distribution/rights/notices; CI-built versioned package with hashes and supported-platform matrix; fresh-machine launch/create/play/save/quit/resume smoke; backup/save location/compatibility instructions. |

## Merge checkpoint and follow-ups

[RC follow-up tracker #330](https://github.com/dills122/sandtable/issues/330) carries
remaining work with owner lanes and explicit pass criteria. PR329 merged as
`fc4cb925` after48 final-head checks passed and full independent review completed.
That bounded reliability checkpoint does not close release, certification or
playable-product gates. Known
Breakdown/cycle source-pin failures predate this PR: verifier files and pinned
`Cna1979LandSequence.cs` are unchanged from base `16d76448`. They remain release
blockers in #330, not waived successes. New PR regressions must be fixed before merge.

## Next execution order

1. Preserve merged [PR329](https://github.com/dills122/sandtable/pull/329) evidence
   and validate subsequent bounded packets on their own heads. All seven decision
   windows exist; prior review/CI does not automatically cover new changes.
2. Close remaining integrated durable-flow proofs: unproved native process cuts
   and other-lane crash/lost-response boundaries, alongside source-pin
   reconciliation and dependency remediation in
   separate owned packets.
3. Close original/public certificate and Runner evidence gates; freeze exact RC
   support profile before any public admission.
4. Progress measured scenario/product gates; run release-head partitioned CI and
   independent review at a release checkpoint, not after every small edit.

Use Ruflo namespace `sandtable-rc-20261023` for assignments and receipts. During
iteration run focused classes/methods and affected consumers; full Core belongs
to integration/release checkpoints. Do not merge, promote unsupported rows, or
claim RC readiness while applicable gates above remain open.
