# Author Explanation — B2 stage-entry

## Intent and plan
Task008 B2 completes the four explicit-none stage transitions after accepted Weather C. It reaches
Reserve-entry state10 while preserving complete predecessor authority; B parent then has B1+B2.
No Reserve execution, first opening, generic Snapshot12, public gameplay or publication claim.

## Flow and components
CampaignCombatStageEntryModels owns typed command/input/event, four-edge metadata, immutable private
state and result. Engine first requires full StageEntryPolicy1 and exactlyone Weather event, then
calls actual C replay over independently trusted creation/fourpreamble/Weather. State6 must be genuine
Organization with five receipts/one Weather. Each event is regenerated from accepted input and compared
in full. ReadState compares complete reconstruction. Apply authorizes System/kind/version before
historical-input lookup; retry returns original event and current state even after later stage gates.
Codec owns exact event/input/private state bytes and ste receipt hashing, reusing D1 prefix framing.
Tests plus fixture link cover12chains/60cuts/228frozen byte-length/SHA fingerprints.

## Choices and invariants
Existing StageEntryEvents sources and catalog positions retained unchanged. Rebind only sequence
version5, preserving Commonwealth ActiveSide for fleet positions and null for final Reserve. All
Weather kinds, post-Weather RNG cursor, World, holder and orders survive byte-for-byte. Receipts
append fromfive to nine. Policy requires all4explicit-none gates, not empty World lists. Guard before
predecessor replay matches frozen oracle policy regression. Isolated typed HasObligations probes test
same guard because broader Setup7 creation already rejects those variants; no reflection bypass.
No externally supplied cache/prefix/version acts as authority. Bounded chain is Created11+4prelude+
1Weather+0–4stage;1MiB records/depth32. Exact canonical bytes reject unknown/duplicate/reordered data.
Buffers copied and immutable/read-only values retained. No legacy readers edited or fixture regenerated.

## Verification and dev findings
See b2-evidence.md for exact final executed commands/results. Tests include all frozen positions/source
arrays, both orders/allWeather kinds/rejected-byte seeds, historicalretry at everylatercut, wrongactors
including Commonwealthfleet, changedoccurrence, missing/reordered/duplicate/foreignhistory, complete
policygates, rawcanonical variants, forgedvalidformat/re-signedsuccessor/cache and bufferisolation.
Oldreader/creationSnapshot rejection retained. Lead dev review caught a test mutation being undone by
re-signing the receipt itself; worker corrected by preserving badreceipt and asserting bytes changed.
Source review found no remaining actionable defect. Tests/build/format and independent rounds recorded
separately rather than inferred from oracle success.

## Tradeoffs, limits and challenge points
Small fixed full replay intentionally favors causal authority over independent cached-state admission.
Private state still serializes initial World because B2 performs no World mutation; Reserve needs its
own causal World serialization next. System metadata is trusted ingress, not public authentication.
No provider durability or positive obligations implemented. Verify fleet/nullside handoff, preserved
advanced RNG, complete policy validation, currentstate retries, exact source order and B parent scope.
