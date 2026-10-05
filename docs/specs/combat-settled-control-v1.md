# Result2 settled Combat control v1

CMB-019D2 is a private executable contract consuming the full [019D0 packet and proof](combat-settled-continuation-v1.md), with native019D1 parity as dependency. Earlier Movement remains **synthetic-pre-combat**. No C# control implementation, actual positive campaign entry, ordinary repeated Movement execution, later-II/consumed history, Snapshot, transport or public activation is delivered. Parents017–019 remain open.

## Frozen manifest and acceptance

Frozen2026-10-04 before implementation, baseline `d919749b9969fd3c86e6a6ba72243d992d3cda5a`, dependency PR150 still OPEN when checked. Sole primary paths: this spec, `combat-settled-control-v1.schema.json`, `fixtures/combat-settled-control-v1.json`, `verify-combat-settled-control-v1.py`, and [canonical implementation plan](../design/combat-cycle-implementation-plan.md#task019d2--settled-control-executable-contract). Administrative review/handoff is allowed. No old frozen files change.

Acceptance: meaningful semantic RED before policy implementation; literal receipt/hash progress and witnesses; all32 admitted owner/seal contexts; retained original-distance/exclusion probes; forced and owner outcomes; actor/deadline/fallback/retry and restart at every cut; canonical and valid re-signed attacks; pure World/resources/RNG/member/history/future-duty preservation; exact authority/prefix/catalogue identities. Freeze new regression bytes only after semantic GREEN. Run new and seven unchanged oracles, syntax/JSON/link/pin/diff checks and repository `just check`, then fresh independent review (max3) before authorized draft PR. This Python contract is not native implementation evidence.

## Admission and assessment

`SettledControlBase` carries complete canonical019D0 packet bytes and canonical proof bytes. Replay full Request/Created11, selection/RBA, Round2/Result2 and empty Release using the unchanged019D0 reader and independent Result2 catalogue before accepting the proof. A proof, hash, stale historical Result1 source, caller World/progress/witness/booleans or self-consistent re-signed source cannot supply trust. New control identity uses a new domain and the entire base bytes. Successful admission cache holds only immutable bytes; parsed projections are freshly owned.

Assessment copies independently derived progress, supported witnesses and original Movement descriptor receipts from the authenticated proof. Progress binds the actual accepted commitment event receipt **and** canonical event hash, with full owner/turn/stage/slot/ordinal/prefix source binding. Empty Release adds no progress. Unsupported inputs reject before opening; `supported` plus an empty witness list is a valid no-continuation outcome.

| Supported witnesses | Material progress | Control |
| --- | --- | --- |
| empty | either | System finish, no decision/deadline |
| present | empty | System finish, no decision/deadline |
| present | present | One owner repeat/finish decision |
| unsupported | any | Reject before opening |

All32 admitted default settled contexts contain commitment progress and legal witnesses. Distant-original/prior-exclusion descriptors are authenticated synthetic evidence probes, not additional campaign histories; both force finish. No-progress/no-attack/retain-only policy rows are explicitly independent policy probes and unsupported admission boundaries, never fabricated reachable settled lineage. Literal Contact5+2+2→9/DP0 and zero-loss Engaged5+2+4→11/DP1 remain certified. Assessment spends no CP/DP/RNG and retains guards, custody, replacements and future duties.

## Opening, decision and resolution

Historical [cycle-control policy](combat-cycle-control-v1.md) is a policy reference only. Its Result1 identity/base cannot authenticate Result2. This new closed inventory retains the policy's commands (`open`, `repeat`, `finish`, `expire`, `unavailable`, `fallback-step`) and prospective event tags, with new control and receipt domains and explicit sourceProofId on state/event/assessment.

Initial authority/version/prefix and Reserve Release position are the019D0 terminal; accepted high-water starts null because native empty Release is untimed. Opening owner-choice needs room for opening plus closure and a next ordinal. Forced finish needs one version. Actor/control/cycle binding precedes command+actor retry lookup. A duplicate returns its original receipt and original canonical event bytes, including after closure. Changed stale input rejects; stale decision timers and early expiry are no-ops. Retained successful commands do not renew deadlines on restart.

System opens or drives timeout/unavailability/fallback; only the actual owner submits repeat/finish. Equality with deadline rejects available owner input. Missing/regressed/unavailable clocks force System finish. Opening clock loss stores no invented Timing and permits deterministic fallback. Configuration-pinned cycle-control budget/high-water follows existing Timing grammar. Overflow/capacity faults reject before mutation, never become no-continuation.

Repeat closes ordinal k and atomically establishes k+1 at the same turn/stage/slot's Movement catalogue entry. New Authority openingPrefix is the prefix immediately before repeat; openedAuthorityVersion is repeat result version. Serialize event/receipt before extending prefix. Reset only targetUses/new-cycle progress; retain complete World/resources/RNG/members/Reserve histories/stage attack history/future obligations and closure receipt. No Movement action or cost is executed. Proof assessment describes the closed old ordinal: it grants no reusable successor admission; another replayed successor proof is required to assess the next cycle.

Finish closes once at the established same-slot Truck Convoy entry. It executes no Convoy or housekeeping. Existing pending next-Movement exceptions would expire with finish receipt while records persist; this admitted empty-Release profile has no release exception, and cannot manufacture one. Original Movement exclusions remain immutable source evidence; no current retreat distance rewrites them. Member, obligation and proof lifetime follow existing control policy; no new expiry policy is invented.

## Bytes, replay and faults

[Ordered inventory](combat-settled-control-v1.schema.json) is a typed field inventory, not general JSON Schema. ASCII JSON bytes, fixed key/array ordering, mandatory nulls; reject duplicate/unknown/missing fields, floats/bool-as-int, NaN, BOM, trailing newline/alternate whitespace/Unicode spellings. Complete base/event/state and embedded packet/proof are bounded1MiB, depth32, arrays512, members32 and accepted control events2. Upstream stricter limits remain. CMB-SCC-001 shape/capacity,003 unsupported profile/version,004 source/authority,005 action/timing,006 replay/order,007 overflow/capacity,008 noncanonical bytes. Primitive failures normalize001. No rejected input returns partial accepted evidence.

`replay(baseBytes, inputs, events)` independently admits full source and regenerates every canonical suffix byte. `read_state` compares complete original state bytes against replay; `apply` reconstructs state before transition and checks any caller cache against replay. Exact retry uses retained event bytes. Internal transition/assessment helpers are not admission APIs. No mutable caller projection enters admission cache.

## Verification and next native gate

Run `python3 -B docs/specs/verify-combat-settled-control-v1.py`. [Fixture](fixtures/combat-settled-control-v1.json) freezes hashes/lengths and complete command/event/state strings after literals; it is regression evidence, not admission authority. Full source is retained by the unchanged019D0 fixture and independently replayed Result2 catalogue.

Next separately accepted native manifest (five primary paths): `src/Cna.Core/Campaigns/CampaignCombatSettledControl.cs` (private engine/models), `src/Cna.Core/Campaigns/CampaignCombatSettledControlCodec.cs`, `tests/Cna.Core.Tests/Campaigns/CombatSettledControlTests.cs`, `tests/Cna.Core.Tests/Cna.Core.Tests.csproj` (fixture links), and `docs/design/combat-cycle-implementation-plan.md`. Native reader must consume the full packet through019D1 before deriving assessment and reproduce this new domain/bytes, original-byte retries, all cuts and forced/choice/fallback outcomes. No native adapter starts before contract acceptance. Actual positive-history entry and ordinary repeated Movement admission remain subsequent bounded gates. Executed results, review, dependency and PR metadata belong in the [handoff](../work/handoffs/2026-10-04-settled-control-contract.md).
