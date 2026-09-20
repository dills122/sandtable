# Task013 API reconciliation against frozen012 — research only

Candidate012 `8fcf8b9`; full gate/review ongoing, not acceptance or013 authorization. Read frozen source, original task013-dispatch-proposal.md and task013-root-scope-notes.md; no prior review reads, source/test changes, .NET, oracle execution, agents or commits. This file only. Original proposal remains scope authority with following concrete API reconciliation.

## Decision

Five material paths plus mechanical sixth fixture link remain feasible. Use named resolved-only settlement factory and Result2 external grammar profile. No pending overlay, broad World codec/refactor, new wire version or relaxed legacy constructor needed. No real API blocker discovered. Parent012 acceptance and explicit dispatch retaining all six physical paths remain delivery gates, not requests for user approval.

## Existing exact APIs and immutable provenance

`CampaignCombatSealedRound.cs:26–39` exposes:

```csharp
CombatRoundState ReplayTrustedBoundary(
    CampaignCombatCreationRequest request, ReadOnlySpan<byte> created,
    CombatStepsBoundary boundary,
    IReadOnlyList<CombatStepsInput> predecessorInputs, IReadOnlyList<byte[]> predecessorEvents,
    IReadOnlyList<CombatRoundInput> inputs, IReadOnlyList<byte[]> events);
CombatRoundResult ApplyTrustedBoundary(
    CampaignCombatCreationRequest request, ReadOnlySpan<byte> created,
    CombatStepsBoundary boundary,
    IReadOnlyList<CombatStepsInput> predecessorInputs, IReadOnlyList<byte[]> predecessorEvents,
    IReadOnlyList<CombatRoundInput> inputs, IReadOnlyList<byte[]> events,
    CombatRoundInput input, bool admissionEnabled = true);
```

Authenticate by calling ReplayTrustedBoundary with complete separately trusted Round inputs and literal events. Do not accept a caller CombatRoundState as authority. Require returned status committed, stepIndex5, five step receipts, both sealed slots, commitment/round IDs present, one attack-history edge/target use and closed=false. Existing replay proves exact payment and eligibility from original Boundary; do not re-run initial eligibility against depleted World. `CampaignCombatSealedRoundCodec.SerializeState(state)` yields exact committed bytes; SHA256 is ResultState committedHash. `ReadState(bytes, request, created, boundary, predecessorInputs, predecessorEvents, inputs, events)` at codec37 validates grammar/canonical bytes before context, then authenticates and compares whole bytes. `ReadBase` at28 authenticates actual C3a to selected, declined step3.

Frozen `CombatRoundState` has typed `World`, `CommitmentId`, `RoundId`, `StateVersion`, `Prefix`, `Timing`, copied `Slots`, `StepReceipts`, `AttackHistory`, `TargetUses`, `Receipts`, and get-only `Base`. `CombatRoundBase` has get-only `Boundary`, `Steps`, `Configuration`, `ConfigurationHash`, `BaseHash`; private owned BoundaryBytes/WorldBytes describe ORIGINAL pre-use state. RNG stays `state.Base.Boundary.RandomState` through012; there is no separate paid-state RNG property. Do not invent one or modify012 to read it. Role participants come from `state.Base.Steps.Selection.Attacker/Defender.Unit`; paid elements come from `state.World.Elements`. Accepted public floor comes from `state.Timing.OpeningFloorUnixMilliseconds`, not last seal time. Round clock hash comes from `state.Base.ConfigurationHash`; original configuration hash from `state.Base.Configuration.ParentConfigurationHash`.

Frozen codec128–153 serializes typed cost-only World on demand, verifies full allowed World equality, then maps CP/ammo into owned original World JSON. Result codec can obtain owned PAID canonical World by serializing authenticated RoundState and extracting its world; never use Base.WorldBytes directly as paid World. Couple committed state/canonical bytes/hash in a get-only internal context; no record-with independently editable cache. Result serializer derives pending settlement from current typed result/World and verifies full allowed World equality against authenticated paid World + singleton settlement before canonical mapping. Arbitrary caller JSON is never authoritative.

## Proposed concrete new API

Keep all following inside new CampaignCombatResolution.cs, including bounded immutable models:

```csharp
CombatResolutionState ReplayTrustedBoundary(
    CampaignCombatCreationRequest request, ReadOnlySpan<byte> created,
    CombatStepsBoundary boundary,
    IReadOnlyList<CombatStepsInput> predecessorInputs, IReadOnlyList<byte[]> predecessorEvents,
    IReadOnlyList<CombatRoundInput> roundInputs, IReadOnlyList<byte[]> roundEvents,
    IReadOnlyList<CombatResolutionInput> inputs, IReadOnlyList<byte[]> events);
CombatResolutionResult ApplyTrustedBoundary(
    CampaignCombatCreationRequest request, ReadOnlySpan<byte> created,
    CombatStepsBoundary boundary,
    IReadOnlyList<CombatStepsInput> predecessorInputs, IReadOnlyList<byte[]> predecessorEvents,
    IReadOnlyList<CombatRoundInput> roundInputs, IReadOnlyList<byte[]> roundEvents,
    IReadOnlyList<CombatResolutionInput> inputs, IReadOnlyList<byte[]> events,
    CombatResolutionInput input);
```

Names proposed, no API implemented. No fresh-admission switch needed: Result2 requires already committed authority; retained recovery must not reopen admission. If consistent adapter signature retains such a switch, it cannot suppress retained resolution/retry or imply admission. New codec ReadState takes raw bytes then same full authority/local tuple; SerializeInput/ReadInput/SerializeCommand/SerializeState follow existing pattern. Capture Created before caller list Count/index access; bound32 local records,16 Round records,1MiB each, depth32/arrays512 before expensive history reads/copies; preserve UInt64 cursor and signed64 version/time distinctions. Validate local event syntax before trusted-input indexing and whole raw state before any context. Never recover independent actor/time from event.input.

Result state includes immutable authenticated committed context, World, RandomState, result, settlementId, head/receipts, status, policy hashes, acceptedHighWater and frozen null window/closure fields. Draw/result/receipt arrays copied; returned event bytes copied. Private transition only receives replay-derived state. Duplicate detection hashes command and checks actor separately before status/version/time checks after primitive/context checks. No-window expire/unavailable is NoOp; fresh resolve is System/null time/available, exact head, null choice/decision. Later advance/choose/event families reject; parser may recognize frozen full grammar without executing future behavior.

## Narrow compatibility changes

1. `CampaignCombatObligations.cs:228–273`: retain public legacy constructor requirement commitmentId=settlementId+'.commit' and resultId=settlementId+'.result'. Add internal named `CreateResolvedResultV2(commitmentId, resultId, gameTurn, operationStage, attacker, defender, preLossElements, result)` factory, deriving `set.` + domain hash of ordered commitmentId/resultId. Require exact cmt./res. lowercase SHA256-shaped IDs. Factory cannot take arbitrary settlementId, receipts or skip-validation flag. Private constructor/common validation may distinguish identity provenance; preserve all scope/opposing-side/shared-creation/two-distinct-element checks, ammo0, Cohesion0, matching ledger scope, integer CP attacker>=5/defender>=3 and<=10, single10TOE component. All disposition/loss/retreat/custody/relationship receipts null. World7 constructor remains unchanged and enforces current elements equal retained preloss elements for pending settlement (`CampaignWorldV7.cs:126–140,210–228`). Identity string shape alone is not authentication; caller engine derives cmt/res from actual replay/draws.
2. `CampaignCombatIdentityCodec.cs:227–241`: add named whitelisted Result2 external syntax entry point. Existing C3a preserve-array-order mode forbids Route and LegacyBrokenVehicleLot together. Result2 needs Route allowed as semantic id array, LegacyBrokenVehicleLot still forbidden, futureTurn1..115 preserved. Thread explicit private grammar profile through every recursive nullable/array/object arm; do not change old default/C3a behavior or sort Result2 semantic arrays. Audit transitive descriptors and special primitive behavior, including UInt64. Whitelist only external kinds actually referenced by frozen Result2 local descriptors. New result codec owns local descriptors; no copied World schema or generalized serializer.

Authoritative distinction: `docs/specs/verify-combat-result-settlement-v2.py:121–165` initial/resolve/projected_world; resolved world minus settlement MUST equal committed world. `:173–220` command/retry/noop/resolve ordering; `:304–321` context authentication. Frozen Result2 intentionally replaces sample settlement IDs; compatibility is not contract drift.

## Resolution and atomicity mapping

Use `SandtableRandom.NextByte` (Randomness/SandtableRandom.cs:24–43), retaining rejected bytes until<252; no prefetch/ninth draw unless capture trigger. Eight ordered purposes from pinned Procedure, two preloss Morale coordinates and both assault coordinates, optional ninth capture die only. Existing Cna1979CombatAdjudication.Resolve and LookupMoraleAdjustment remain sole rule-table implementations. Return result/final RNG/pending World/event/receipt as one immutable candidate. No second CP/ammo charge, no changes to history/target use/TOE/location/Cohesion, no settled losses/retreat/custody/closure. Overflow or candidate failure leaves authenticated committed bytes/cursor unchanged. Discard/retry verifies Core atomic candidate behavior only, not host transaction.

## Exact six physical paths

1. New src/Cna.Core/Campaigns/CampaignCombatResolution.cs — immutable models/context + one-transition replay engine.
2. New src/Cna.Core/Campaigns/CampaignCombatResolutionCodec.cs — local grammar, strict readers, typed canonical writes.
3. src/Cna.Core/Campaigns/CampaignCombatObligations.cs — resolved-only factory preserving legacy.
4. src/Cna.Core/Campaigns/CampaignCombatIdentityCodec.cs — named Result2 grammar profile preserving others.
5. New tests/Cna.Core.Tests/Campaigns/CombatResolutionTests.cs — integration/compatibility/strictness proof.
6. tests/Cna.Core.Tests/Cna.Core.Tests.csproj — mechanical existing combat-result-settlement-v2.json fixture link only.

No Round/World7/RNG/rules production edits needed. Tests can reuse existing internal CombatSealsTests.Case/WithCp and public internal replay APIs; Result fixture has its own predecessor/round tuples, so small explicit fixture parsing adapter belongs in new tests. Do not duplicate production transition or expected-result algorithm.

## Test mapping and evidence limits

- First RED: authenticated paid012 trace cannot yet emit its first literal assault-resolved event.
- All32 Result2 contexts: separately authenticate original C3a inputs/events, six Round inputs/events, committed literal bytes and Base. Compare32 literal resolve event bytes; initial/resolved serialized SHA256 against64 stateHashes. These64 are HASH cuts, not literal state strings. Full272events/304hashcuts includes future work.
- 16eight/16nine draw traces;12 rejected-byte traces; ZERO literal block crossings. Add seed0 cursor30 existing research vector through rebuilt C3a/Round APIs: consumed4dd79fabb6070dc7, final38, dice66443222, differential-1/no capture. UInt64 end/overflow tests leave predecessor intact; no alternate seed or refund.
- Resolve/restart/discard/lost-response retry from paid original context; admission-disabled predecessor was already closed to new opening, never inferred from depleted World. Wrong independent input/actor, edited Round history, wrong seed/cursor/paidWorld/hash, missing capture die, extra ninth draw, consumed/purpose/order/facts rehash and pending settlement/root forgeries reject.
- Whole raw state/event syntax, noncanonical ordering/escapes/duplicates, primitive/type/size/depth/array errors before null/sentry context. Route-valid Result2 syntax reaches trusted-context sentinel while C3a rejects same Route shape first; LegacyBrokenVehicleLot rejects both. FutureTurn endpoints and UInt64 exercised.
- Factory tests legacy rejection unchanged; valid hashed pending IDs derived exactly; malformed IDs, swapped/foreign units, unpaid ammo, wrong ledger/CP/TOE fail; World7 rejects mismatch against retained preloss state. Non-settlement World mutation cannot be silently normalized. Null consequence receipts mandatory.
- Preserve32/64 scope: all later events rejected, no window/closure/settled losses, no generic HistoryReplay/Snapshot integration. Shared selected Rules/Randomness/World/Identity/Steps/Seals/Commit regressions, then root full gate/reviews.

No real architecture blocker found. Current actual campaign history remains empty; every positive result here uses independently trusted synthetic Boundary. Root must wait012 acceptance and finalize explicit scope before013 implementation.
