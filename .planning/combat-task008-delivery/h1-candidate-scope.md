# H1 candidate runtime routing scope — not dispatched

Research only while H0 validation/reviews pending. No H1 implementation before H0 acceptance.

H1 should accept one independently retained ordered event list plus trusted request and Created11,
not caller partition/family tags. Bound total count512/aggregate16MiB/each1MiB before copying.
Retain complete immutable exact buffers and full head/prefix evidence. Dispatch exact causal
opening→Weather→stage→Reserve designation/completion transitions through accepted strict readers.
Every legal prefix through atomic first opening supported; unsupported tails fail closed.

Known existing strict adapters:
- CampaignOpeningPreamble.Replay(request,created,preamble)
- CampaignCombatWeather.Replay(request,created,preamble,weatherEvents), four opening events.
- CampaignCombatStageEntry.Replay(...,preamble,weatherEvents,stageEvents), one Weather/four stages.
- CampaignCombatReserveOpening.Replay(...,preamble,weatherEvents,stageEvents,reserveEvents), optional
  designation then one exact completion. Completion applies019A and enters Movement.
These currently require caller-partitioned lists, so H1 must derive partitions from actual causal
state/next event. Parsing eventType is only dispatch; accepted codec/replay must validate entire bytes.
No trusting caller version/prefix/root/cache or silently truncating unsupported suffix.

Choose bounded typed projection/results that can be extended by H2/H3 and later H4 root codec,
without introducing opaque serialized family-state authority or speculative generic infrastructure.
Five primary files maximum (likely history/result models, router, tests, fixture csproj link; refine
at dispatch). H0 fixture must supply creation plus exact eventbytes or reconstruct by existing fixtures;
root history hashes alone cannot stand in for actual acceptedbytes. Literal fullroot parity is H4,
not a claim available from H1's routing alone. Reuse frozen family fixtures for exact replay evidence.

Tests: every precycle prefix across selected variants/bothowners; trusted creation/context mismatch,
omit/duplicate/reorder/alternate canonical/raw/wrong event successor, unsupported post-openingtail,
count/byte ownership boundaries, no sourcebuffer aliasing. Mandatory freshadmissiondisabled fullroot
seam remains H4; H1 must require independently retained Created11 and never manufacture it.
Actual C# public creation entrypoints remain dormant. User dev+three review policy applies.

Concrete handoff guidance after H0 acceptance:
- Internal Core-only API; no registration/provider/grain/public admission changes.
- Result may carry typed accepted family projection plus complete owned transcript; avoid object,
  dynamic, JSON cache-as-authority or a generalized handler registry. Common header/prefix/receipts
  must derive from accepted reader. H4 serializes the eventual typed complete root.
- Exact fixed pre-cycle ordering is profile constraint, not permission to dispatch solely on
  untrusted declared stateVersion/count. Each next event must pass causal reader and canonical bytes.
- Root owns fullbuild/fullsuite/format/boundary, docs and independent review packets. Worker owns
  focused RED→GREEN evidence and primary files. Existing5MB H0fixture need not enter shipping assets.
- Candidate source names may be refined by worker; preserve no more than five primary files,
  separate contract/test implementation from next H2/H3 work, and provide complete final file hashes.

Existing test setup reference (known reviewed paths): CombatReserveOpeningTests.cs Chain/Predecessor
and CombatWeatherTests.cs Opening derive trusted context from C2 created golden, seed-varied request,
then canonical preamble/Weather/stage events through dormant Apply. Reuse this local pattern rather
than inventing a synthetic predecessor. Compare resulting complete event hashes and selected causal
headers/receipts against H0 frozen roots and existing family golden artifacts. H4 owns literal root
serialization; H1 must not claim full Snapshot12 parity merely from matching headers.
