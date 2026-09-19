# Author Explanation — B1

## Intent and plan
Task008 B1 adds dormant first-turn opening replay from independently trusted creation to Weather entry.
B parent is refined into B1→C Weather→B2 stage-entry, because canonical stage-entry requires actual
Weather history. No contract bytes or acceptance requirement changed. Actual publication HOST-PUB-001
and full Task008 restore remain open.

## Flow and components
CampaignOpeningPreambleModels owns immutable commands, trusted actor metadata, three typed event arms,
receipts and private state. CampaignOpeningPreamble revalidates Created11 through A2 then reconstructs
up to four retained events. Each input derives expected event bytes; exact comparison rejects imported
claims. Apply authorizes actor before receipt lookup; exact historical retry returns original event
and current state, changed input conflicts. ReadState compares the entire derived projection.
CampaignOpeningPreambleCodec owns canonical input/event/state writing and input extraction, receipt
hash domain and D1 event-prefix framing. Reuses established position/World/RNG writers.
Tests link frozen fixture via csproj and compare byte counts plus SHA256, not literal fixture bytes.

## Choices and invariants
Only fixed certified Setup/config hashes admitted; campaign ID and unsigned seed remain caller inputs.
This narrower B1 contract avoids admitting unsupported C2 variants. Full bounded replay avoids cache
trust and accepts at most four events, each at most1MiB/depth32. Exact reconstructed output prevents
unknown/duplicate fields or large imported collections becoming authority. World and RNG unchanged.
State lists own read-only copies, source arrays are read-only, input records immutable; caller event
bytes copied before validation. No public registration, old reader edits or provider code.

## Verification
Worker RED missing implementation, then GREEN12 tests (six traces and six negative/isolation groups).
All60 frozen fingerprints match, including30 state cuts. Targeted checks cover retry at later cuts,
wrong actors, omissions/reordering/fifth event, scalar and raw mutations, valid-format forged hashes,
receipt/source changes, opposite-order cache, unknown contexts, malformed commands, old-reader rejection
and buffer isolation. Lead dev review found no remaining actionable defect; build zero warnings/errors.
Full-suite/format results retained in b1-evidence.md as checks finish.

## Costs, gaps and challenge points
Replaying entire prefix is bounded to four here; later adapters need cumulative causal evidence.
Private PreambleState1 is not Snapshot12. Actor metadata presumes trusted submission; not authentication.
No atomic publication/durable history, Weather resolution, stage-entry, random initiative or gameplay
activation. Fixed hashes couple adapter to certified profile intentionally. Review causal retry identity,
world/sequence preservation, framing, strict import bytes and dependency refinement independently.
