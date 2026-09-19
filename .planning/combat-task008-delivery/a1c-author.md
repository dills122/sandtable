# Author Explanation — A1c

## Intent and success criteria

Implement Task008 A1c only: dormant canonical Request1 and Created11, independent trusted input
binding, exact-retry/changed-request decision. Frozen requirements:
`docs/specs/combat-authority-envelope-v1.md`, schema/fixture/oracle alongside it, Task008 execution
index in `docs/design/combat-cycle-implementation-plan.md`.

## Plan traceability and flow

A1b already supplies Setup7/configuration. New validated context retains Rules10, Setup7,
Content7/scenario and configuration. Request fixes explicit campaign ID and unsigned seed with
cursor0, derives binding from domain/NUL/canonical request. Created11 constructs initial World7
with that binding and initial catalog5 preamble position, then emits exact frozen fields.
Request reader reads identity/seed and reconstructs all other fields from trusted context before
exact-byte comparison. Created11 reader requires a separately trusted request and compares complete
derived event. Pure cut returns copied retained bytes before admission switch; absent/disabled fails.

## Changed components

- `CampaignCombatCreationRequest.cs`: validated immutable context, request and canonical codec.
- `CampaignCreatedV11.cs`: creation-only typed value and derived initial World/position.
- `CampaignCreatedV11Serializer.cs`: exact event writer/readback and pure retry cut/result.
- `CombatCreationBindingTests.cs`: literal parity, frozen mutations, raw-byte negatives, identity
  forks, trusted substitutions, unsigned seeds, turn bounds, disabled-admission exact retry.
- Navigation/design docs: feature-branch state, terms and remaining gates; no merged claim.

## Choices and invariants

Reuse strict predecessor Setup/config readers and World factory. Entire creation event is derived,
so parsing imported nested state would create unnecessary opportunities to trust self-described
authority. Exact reconstruction is intentionally limited to creation. Request hash excludes World,
event and receipt; no self-reference. No I/O, RNG draws, public registration or prior codec edits.
Catalog5 preamble uses retained predecessor position with version5; golden verifies exact parity.

## Verification

See `a1c-evidence.md` for exact current integration commands/results. Worker red build failed on
missing type before implementation; green focused suite passes48. Literal bytes741/7520, 34 frozen
request/Created mutations plus raw negatives, 12 identity forks and turn1/111. Frozen Python
envelope, Setup, Rules/config, World oracles pass. Format first failed whitespace/import order;
targeted formatter applied. Environment-only named-pipe failure passed after approved access.
Round2 found Setup/configuration IDs could exceed envelope128 limit because predecessor stable-ID
guard checks syntax only. Accepted finding; new context checks their length before reconstruction.
Four boundary tests accept128/reject129; RED two129 cases failed, GREEN full focused52 passed.
No predecessor codec changed. Initial full suite1836 passed; post-fix integration refresh underway.

## Costs and limits

Creation readback rebuilds small certified initial World as validation; it does not restore later
states or mutate a campaign. Pure retry decision proves no persistence atomicity, concurrency,
lost-response recovery or durable restart. Host uniqueness remains campaign-ID keyed. Snapshot12,
receipt/P0, inherited events and full retained-history restore remain pending.

Publication ownership wording conflict is tracked for A2: envelope assigns actual persistence proof
to Task008 while plan checkpoint D defers hosting. A1c does not satisfy or waive that obligation.

## Challenge points

Verify independently trusted inputs cannot be replaced by rehashed embedded objects; immutable
context transitive properties; full ulong handling and ID grammar; shape/canonical rejection;
same-campaign changed input conflicts even with admission disabled; historical/public isolation;
tests derive expected output from literal fixtures, not implementation.
