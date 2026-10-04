# Combat continuation after released-I Movement

Status: CMB-NEXT planning assessment, 2026-10-04. Baseline `origin/main` at
`2e17f60767cceff6db950db532d9432f5b81a9cd`. This is a proposed dispatch sequence, not
parent acceptance or authorization to expand the selected gameplay profile. Task018D/frozen3m
is an in-progress predecessor; its contract exists, but this lane has no accepted runtime evidence
for it. Tasks017–019, Snapshot successor/publication and public020–021 remain open.

This lane owns this document and the [planning handoff](../work/handoffs/2026-10-04-combat-continuation-planning.md).
The coordinator owns canonical status changes. The [implementation plan](combat-cycle-implementation-plan.md#checkpoint-h--reserve-release-and-real-continuation),
[movement delivery plan](combat-cycle-movement-delivery-plan.md#dependency-reconciliation), and
[roadmap](../roadmap/pre-alpha-roadmap.md#current-checkpoint-and-next-gates) remain authoritative.

## Evidence classes and present boundaries

| Boundary | Exact source/test evidence | What remains unproved |
| --- | --- | --- |
| Release lifecycle | `CampaignCombatReserveRelease.Replay` and `CombatReserveReleaseTests`; [Release contract](../specs/combat-reserve-release-v1.md#membership-and-stage-history) | Isolated first-I/later-II/consumed vectors do not authenticate actual later-II or offensive campaign lineage. |
| Settled source to empty Release | [CampaignCombatResultRelease.cs](../../src/Cna.Core/Campaigns/CampaignCombatResultRelease.cs), `Replay`/`Derive`, and `CombatResultReleaseTests` | Full native Result2 suffix replay for 32 supported sources starts at an independently trusted **synthetic pre-Combat boundary**. This is not creation-to-settled campaign admission. |
| Ordinary break-off/move | [CampaignCombatCycleMovementRules.cs](../../src/Cna.Core/Campaigns/CampaignCombatCycleMovementRules.cs), `Assess`/`AssessAndCharge`, `CombatCycleMovementRulesTests`; [CampaignCombatCycleMovement.cs](../../src/Cna.Core/Campaigns/CampaignCombatCycleMovement.cs), `ReplayIsolatedBoundary`, `CombatCycleMovementTests` | Native ordinary move is isolated: `Derive` fabricates ordinal2 with domain `isolated-unimplemented-release-repeat`. No accepted repeat event authorizes that path. |
| Exhausted-ammo continuation | [CampaignCombatContinuation.cs](../../src/Cna.Core/Campaigns/CampaignCombatContinuation.cs), `AssessTrustedBoundary`, [CombatContinuationTests.cs](../../tests/Cna.Core.Tests/Campaigns/CombatContinuationTests.cs), `Probe` | Tests manufacture `probe.movement-completed` and supply trusted location/exclusion projections. The pure helper authenticates neither that receipt nor semantic progress. |
| Actual first positive Release | [CampaignCombatInheritedReserveRelease.cs](../../src/Cna.Core/Campaigns/CampaignCombatInheritedReserveRelease.cs), `Replay`, `Projection.Progress`, `CombatInheritedReserveReleaseTests` | Actual held-I no-move history supports first release-I and conversion fallback; it does not reach a later-II Release occurrence. |
| Armed witness and control | [CampaignCombatArmedContinuation.cs](../../src/Cna.Core/Campaigns/CampaignCombatArmedContinuation.cs), `Derive`; [CampaignCombatInheritedCycleControl.cs](../../src/Cna.Core/Campaigns/CampaignCombatInheritedCycleControl.cs), `Replay`/`Transition`; `CombatArmedContinuationTests`/`CombatInheritedCycleControlTests` | Exact frozen3j/3k released-I terminals only. `CombatInheritedCycleControlSource.Derive` requires the armed proof. It is not a general settled-control implementation. |
| Actual ordinal2 move | [CampaignCombatInheritedReserveMovement.cs](../../src/Cna.Core/Campaigns/CampaignCombatInheritedReserveMovement.cs), `Replay`/`Apply`, `CombatInheritedReserveMovementTests` | Frozen3l one-move profile; pending exception persists. [Frozen3m](../specs/combat-inherited-reserve-movement-completion-v1.md#testing-strategy-and-success-criteria) closes Movement, not Breakdown/Combat. |
| Creation-rooted positive Combat | [CampaignCombatInheritedSelection.cs](../../src/Cna.Core/Campaigns/CampaignCombatInheritedSelection.cs), `Replay`/`Transition`; [inherited selection](../specs/combat-inherited-selection-v1.md#objective-and-accepted-boundary) | Existing ordinary inherited selection accepts only empty candidates at CP12/14. No positive settled entry bridge is established by this assessment. |
| Full composition/public activation | [authority composition](../specs/combat-authority-composition-v1.md#selected-profile-and-trace-set), 28 contract traces; implementation plan Tasks020–024 | Frozen composition is not registered runtime/public gameplay. Full noninitial Snapshot routing, coherent side-safe actions and Exercise/Runner acceptance stay separate. |

The settled control [oracle](../specs/verify-combat-cycle-control-v1.py) pins and imports
historical Result1. Current [Result2](../specs/combat-result-settlement-v2.md#scope-and-authenticated-predecessor)
adds explicit clock provenance and independent mandatory-window timing. Old occurrence IDs,
committed hashes, fixtures and synthetic Movement proofs cannot be relabelled into native Result2.
A new compatibility packet is the smallest safe executable step before a native settled adapter.

## Parent criteria mapped without closure

The following preserves the exact obligations in the three Checkpoint-H parent rows.

| Parent acceptance requirement | Existing evidence | Remaining integration obligation |
| --- | --- | --- |
| 017 release window, canonical own dispositions, pinned budget, first-I conversion/later-II retention fallback, explicit completion | `CampaignCombatReserveRelease`/codec/models and `CombatReserveReleaseTests`; 017A1/A2; first actual bridge017C | Derive later-II membership/conversion receipt from accepted prior history; prove owner and fallback retain/release/completion at the later occurrence. |
| 017 first/later/empty, consumed convert, duplicate/stale/expiry, cumulative CP and no auto-release/repeat | Isolated lifecycle tests, empty Result2 adapter017B, actual first-I017C | Consumed history must link the original unit to a real attacker commitment. Release must preserve it. Do not count defensive participation or a fabricated link. |
| 018 retained Movement-end proximity/next-Movement exception, changed enemy position | `CombatContinuationTests.RetainedDistanceAndEarlierExclusionSurviveCurrentProximity`; pure019A; first-I3l | Replace manufactured settled proof with actual completion receipt and original-unit coverage, preserve earlier exclusions despite retreat/current proximity changes;3m acceptance is pending. |
| 018 Contact/no-ZOC Engaged/overlap/last-counterpart, ordinary150% and stricter Reserve ceilings | `CombatCycleMovementRulesTests`, `CombatCycleMovementTests`; cumulative literal Clear2 CP11/DP1, CP15/DP5, rejected16; isolated I/II ceiling probes | Actual ordinary repeat must authorize Movement. Actual released-II history must authorize its ceiling; arithmetic probes are insufficient. |
| 018 atomic CP/DP/move/member update, resources/relations/history retained, restart/overspend/Breakdown/exhausted assault | 018A/B immutable cost and World projection,3l replay/caches; existing spending and result tests | Integrate lifecycle/recovery at each new cut, preserving unrelated memberships, CP/BP/bands/broken lots, ammo/TOE/Cohesion and offensive history. Empty Breakdown resolution does not prove positive vehicle behavior. |
| 019 first opening, semantic progress, supported witness and repeat/finish | First opening implementation;019A pure Movement witnesses;019B/019C actual signed first-I progress plus armed witness/control | Authenticate settled commitment progress plus retained Movement proof; implement settled control and actual ordinary repeated Movement. |
| 019 full truth table; no-op/cancel/retain-only; ordinal/prefix forks; zero-loss Engaged repeat | [Cycle control contract](../specs/combat-cycle-control-v1.md#assessment); `CombatInheritedCycleControlTests` only frozen3k branch | Result2 compatibility before settled control; commitment receipt/eventHash is progress, not commitmentId, receipt count or caller boolean. A no-op/retain-II history alone cannot repeat. |
| 019 lost reply/deadline, obligations, unsupported continuation is not none |019C timing/retry and frozen contract truth table | Reexercise all outcomes at the new settled boundary; preserve guard/escape/future duties and finish at same-slot Truck Convoy entry. Unsupported/armed profiles reject rather than force finish. |

## Frozen next dispatch: CMB-019D0 Result2 settled continuation contract bridge

This is the recommended next **executable contract** slice after accepted3m; it changes no C#.
It reconciles the settled source/progress boundary before any runtime adapter. Dependency order is
coordinator acceptance of018D evidence, then this packet, then separately frozen native proof/control
children. Technically independent contract research may proceed earlier, but cannot count3m complete.
Use existing017B and019A as the native reference, not historical Result1 parity by name.

Exact five-primary-file ownership for the future child:

1. `docs/specs/combat-settled-continuation-v1.md` — closed authority/trust contract and source-to-field mapping.
2. `docs/specs/combat-settled-continuation-v1.schema.json` — ordered inventory with explicit Result2 provenance.
3. `docs/specs/fixtures/combat-settled-continuation-v1.json` — retained literal vectors/bytes/pins.
4. `docs/specs/verify-combat-settled-continuation-v1.py` — TDD executable bridge and rejection suite.
5. `docs/design/combat-cycle-implementation-plan.md` — coordinator-owned manifest/status reconciliation.

The last file requires coordinator scheduling; do not race the3m owner. All five are proposed new
ownership for a later dispatch, not edits authorized in this planning lane. If new gameplay,
predecessor types or a sixth primary file are needed, return a scope gate before changing them.

### Inputs, outputs and authority

Input: independently admitted synthetic Result2 boundary, request/Created11, complete selection,
round and settlement inputs/events, untimed System empty Release open/complete inputs/events, plus
an independently pinned synthetic Movement-end descriptor. Retain actor and clock inputs separately
from event effects; regenerate all source bytes before extracting anything. Label the descriptor
`synthetic-pre-combat`, with explicit scope/original-unit locations/exclusions/receipt source. Do
not populate it from post-retreat World or accept an arbitrary completion receipt as authenticated.

Output: immutable canonical settled continuation proof, complete source identity and trust label,
current World/RNG/cycle/target-use/attack history, Release completion receipt, ordered progress refs
and the pure019A-equivalent Movement witness list. No authority increment/event/action, resource
spend, repeat permission, exception or catalog advance. Use existing contract versions and new proof
identity; never overwrite old bytes or imply new Snapshot/transport registration.

Progress is extracted only from the replayed `combat-attack-committed` event: bind its canonical
event hash and event receipt to the retained commitment/attacker/scope. Commitment ID and receipt ID
are distinct. Empty Release opening/completion adds zero progress. A complete proof can be safely
cached by exact admitted bytes; self-consistency or a matching terminal digest cannot supply trust.
This intermediate packet explicitly retains synthetic earlier history; actual-history replacement
is a later frozen bridge, not a hidden acceptance criterion this slice can satisfy.

### RED tests and acceptance criteria

Start with a compiling/importable deliberately missing bridge; retain failing semantic assertions,
not syntax/import failures. Record RED before implementation and GREEN after, without regenerating
existing fixtures. At least:

- both-owner zero-loss Engaged sources yield terrain2+break-off4, CP5→11/DP1 and one signed commitment progress reference, despite ammo0;
- Contact yields CP5→9; literal prior-spend9+4+2=15/DP5 and16 rejection remain independent rule probes, labelled separately from admitted histories;
- each of the32 owner-selected native Result2 source contexts preserves its own settlement World/RNG and obligations through empty Release; evaluate witnesses or explicitly reject a documented unsupported profile;
- wrong receipt substituted for commitmentId, re-signed effect/state, changed input/actor/clock policy, missing/reordered source or Release suffix, cross-owner/scope/unit, incomplete membership or false prior proof rejects;
- synthetic retained end-distance/exclusions survive current enemy retreat/proximity changes; missing proof cannot become an empty legal-move list;
- no progress from empty Release; cancelled/private-seal/timer/retain-only evidence is rejected as a substitute for the required signed commitment (broader no-attack truth-table admission belongs to later control);
- immutable input preservation, canonical readback, whole-value/depth/array limits and checked overflow; no World/resource/history/position/version drift.

Before recording GREEN, freeze all new bytes/field order/source pins and write an explicit compatibility
ledger explaining differences from historical Result1 `settled-control`. New snapshots must not use
old hashes as substitute provenance. The oracle must independently check literal expected arithmetic,
progress identities and obligations before storing regression hashes. Do not require all32 to have
identical witness counts; custody, guard occupancy and cumulative CP differ by source.

Verification for this future docs/Python slice: new oracle plus unchanged Result2, sealed-round2,
Reserve Release, ordinary Movement, cycle-control and3m oracles; JSON/Python syntax, links, pins,
`git diff --check`, fresh independent review. Run `just check` as the repository regression gate if
available and record exact output, but do not present .NET success as implementation of this bridge.
Use run-tests/binlog-generation skills for exact MTP commands/binlogs; `--solution`/`--project` are
required. A docs-only failure/environment limitation remains explicit.

## Following runtime and actual-history sequence

After019D0 acceptance, freeze a native settled-proof adapter (engine/models, codec, focused tests,
fixture-link Core.Tests project file, canonical plan: at most five primary paths) implementing the
new packet through `CampaignCombatResultRelease.Replay` and `CampaignCombatContinuation.AssessTrustedBoundary`.
It must preserve the synthetic trust label; this narrows duplicate semantics and proves source
bindings but cannot close parents. A separate settled-control child then implements all forced/
owner outcomes and receipt/prefix/retry rules against that admitted proof. It must not widen019C's
armed source type or promote isolated018B ordinal2 to an accepted repeat history.

Actual-history integration needs a contract-first positive pre-Combat entry/selection bridge carrying
real completed Movement/Breakdown history into supported round/result authority, then a native
ordinary repeat-to-Movement lifecycle adapter. Existing inherited empty selection and first-I armed
certification do not establish this bridge. The32 synthetic result seeds/cursors/CP cases cannot all
be presumed reachable from campaign play. Choose a genuinely reachable smallest source, then freeze
its exact fields/bytes and <=5-file runtime manifest. No exact executable manifest for that bridge
is certified here; absence of a compatible creation-rooted positive source is an explicit gate.

For later-II, preserve actual first-I→II conversion, CP and designation/conversion receipts, obtain a
supported continuation witness (retained-II itself cannot move), emit real repeat, close the next
Movement/Breakdown/Combat and reach later Release. The current019B proof rejects conversion fallback,
so converted-II cannot borrow its armed witness. Freeze each causal bridge before native consumers;
prove later owner release/retain and retention fallback plus cumulative floor(CPA/2) rights.

For consumed offensive history, first freeze/admit released-Reserve offensive Combat, including
released-II pre-Morale DP and one-off allowance consumption. The Release contract explicitly excludes
this gameplay extension from the current Combat profile. Then derive `offensiveCommitmentId` from a
real accepted attacker commitment, retain it through repeat/Movement/Release, and reject reuse.
Do not manufacture positive consumed fixtures or interpret defensive participation as consumption.
This profile extension requires its own authority decision; this recommendation authorizes no rules.

Future native children require focused tests, full solution build/test, Boundary, format, direct
frozen-oracle checks, each-cut replay/original-byte retries and fresh review. `just check` is the
full gate. Public020–021 and Exercise/Runner022–024 follow parent reconciliation; HOST-PUB-001 and
full Snapshot restore remain independent obligations. Checkpoint H still requires both actual
Movement/Reserve repetition and settled Combat-to-finish through dormant authority.

## Uncertainties and unsupported profiles

- 3m runtime success is not established by this lane. Its terminal authority31 is Breakdown entry;
there is no new repeated Breakdown/Combat authority solely because the exception expired.
- Native Result2 synthetic boundary versus historical Result1 control is a measured contract gap;
new literal compatibility mapping is required, not a version-number guess.
- Positive created-to-settled source, converted-II supported continuation and released-Reserve
attacker admission need separately bounded contracts; do not equate existing armed proof with assault execution.
- Arbitrary second-slot settled sources, larger/motorized/gun/armor profiles, positive vehicle
Breakdown/RBA/ZOC, unsupported Weather/terrain, multiple assaults, supply/reset/housekeeping and
hosted/model dispatch have no admission claim here. Isolated generic scope probes do not add one.
- The graph is best-effort. The worktree index `sandtable-cmb-next-20261004` generation
2026-10-04T21:55:58Z has metadata-matched cited source/spec paths. Both-direction continuation trace
finds only a test caller; source checks also verify the actual synthetic test construction. Some
name-resolved callee edges are heuristic and are not authority evidence. Known modern-C# partial
ranges were read directly; no source was changed to satisfy parsing.

## Validation of this planning change

This lane changes only two Markdown files. No executable verifier, production source, test or
frozen fixture changed; RED/GREEN and new .NET test evidence are not applicable. Existing contract
oracle reruns and document/source checks are retained in the handoff. Parent closure is not claimed.
