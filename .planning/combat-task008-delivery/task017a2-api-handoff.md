# Task017A2 API handoff — research only, not dispatched

Prepared against accepted A1 `77ef166`; A2 requires a fresh exact manifest and dispatch. No A2 source/test edits or new oracle execution. Canonical Task017 refinement remains authoritative; this record replaces speculative pre-A1 API assumptions only.

## Reusable current boundary

`CombatReleaseBase` owns copied member/attack arrays and immutable cycle, history, exception, unit, CP and RNG values. `CampaignCombatReserveReleaseCodec.SerializeBase(base, retainedRequest)` validates isolated-ledger base/history and exact frozen bytes. `ReadBase(raw, independentlyExpectedBase, retainedRequest)` checks raw syntax/spelling before trusted inputs and compares complete canonical bytes. A2 must not turn candidate-derived expected data into authority or change A1's44base hashes/four historical exclusions.

Existing base validation deliberately permits only initial Release histories: I at ordinal1, II later, historical released-none with expired exception. A2 state can legitimately hold converted-II during ordinal1 or newly released-none with pending exception. Do not run ReleaseState members through initial-base validation or relax initial-base rules to accommodate lifecycle. Add separate state validation/replay with immutable state-owned arrays; keep retained base unchanged.

## Proposed four implementation paths plus root plan

- Extend `src/Cna.Core/Campaigns/CampaignCombatReserveReleaseModels.cs` with owned command/input/effect/disposition/receipt/state values.
- Extend `src/Cna.Core/Campaigns/CampaignCombatReserveReleaseCodec.cs` with explicit frozen command/event/state writers and raw-before-context replay readers. Preserve A1 base reader.
- Add `src/Cna.Core/Campaigns/CampaignCombatReserveRelease.cs` for deterministic transition/replay. Public internal entry takes retained expected base/request and separately trusted input/event history, not caller-supplied previous state.
- Add `tests/Cna.Core.Tests/Campaigns/CombatReserveReleaseTests.cs`; frozen fixture already linked. Existing A1 builder is private; use a bounded test-local independent recipe unless a separately scoped helper extraction is justified. No production fixture loader or reflection workaround.
- Root owns `docs/design/combat-cycle-implementation-plan.md`; administrative evidence/status docs counted separately. If source layout needs another path, revise manifest before editing.

This is a proposed manifest, not automatic dispatch or a guaranteed size estimate. Remaining lifecycle/timing/replay work is materially larger than A1 and should receive a fresh session with full gates and three reviews available.

## Mandatory transition and proof points

First-I release/convert, one-consumed conversion, later-II release/retain and owner bulk completion; explicit open and explicit completion for empty queue. One pinned Config budget/high-water, no renewed deadline, owner equality-at-deadline rejection. Opening clock loss records no fabricated Timing. First fallback converts remaining I in canonical queue order; later fallback retains remaining II once. Preserve original input actor while fallback event author becomes System.

Shape/actor/identity checks precede retry, retry precedes terminal/stale checks, old retry succeeds after completion, stale callbacks no-op. Maximum34events,32members,512array items,1MiB/depth32, checked version/ordinal arithmetic. Completed kernel remains at same Release position; no CycleControl or automatic repeat.

Receipt follows unsigned canonical event digest; conversion/release history then stores actual receipt. Owned pending next-Movement exception waives no other costs and performs no Movement. Retain CP/CPA/RNG/attack history and World hash; no resource refund, relation/custody/future-duty change or World projection. State readback reconstructs full suffix against independently retained base and trusted inputs, then compares all canonical bytes.

Target44isolated rows:132event hashes,176state hashes/lengths and2literal terminal events. These are not132event literals or176full-state literals. Four historical Result1 rows stay excluded. Native empty adapterB, positive3h→3i provenance and later-II/consumed actual campaign lineage remain outside A2 and parent017 stays open.

Meaningful RED must exercise an actual first-I open/disposition sequence after compiling skeleton, then deterministic fallback/retry/recovery negatives. Existing A1 six facts must remain. Full root build/tests/Boundary/format, exact candidate CI, development review and three fresh sequential independent reviews (third full rebuild) remain required before acceptance. No work authorized by this handoff alone.
