# Initial H restore research — provisional

Question: recover every implemented B–G/019A retained cut with fresh admission disabled, then extend
same contract for009–019. No storage-provider choice or public-registration work. F3–F6 implementation
must finish before final H scope freeze. Lead research; no code changed.

Evidence inspected:
- Canonical plan Initial H boundary explicitly limits checkpoint D to available handlers and keeps
  28-trace cumulative target open for later gameplay. Task009 depends005–008; do not invert dependency.
- CampaignCreationSnapshotV12.Create validates separately trusted request/Created11 before hashes.
  CampaignCreationSnapshotV12Codec.Deserialize compares exact whole creation Snapshot12 and rejects
  all noninitial state. Preserve this old reader and its creation golden.
- CampaignCombatCreationCut.Decide accepts retained exact Created11 regardless admissionEnabled;
  null retained evidence with admissionEnabled=false rejects. This is existing initial recovery seam.
- Each family exposes typed Replay from full predecessor bytes plus family suffix, strict ReadState,
  and independently derived canonical state. No cached state admission. Current F2 wrapper holds real
  trigger, current World/version/prefix, reaction window/flow, receipts/tracks/progress.
- combat-snapshot-composition-v1.md retains C2 root order but its first-Combat headers are explicitly
  synthetic. It says no arbitrary Reaction arm and later arms must extend prospective family before
  registration. Cannot use those vectors as actual B–G full snapshot proof.
- verify-combat-authority-composition-v1.py:438 build_snapshot emits RootSnapshot contractVersion1
  witness with fragments/digests, NOT literal Snapshot12 root. Its28 terminal witnesses alone do not
  supply exact full Snapshot12 golden for each early inherited cut.

Likely bounded sequence (needs final design/review):
1. Pin complete noninitial root envelope/arm representation and exact per-cut bytes for implemented
   inherited profiles, preserving C2 creation root/order. Reuse existing family state bytes as typed
   arms only if canonical ownership permits; do not invent an unreviewed wrapper. If exact frozen
   vectors absent, add a bounded contract-first H0 packet before C# codec implementation.
2. Trusted retained-history router reconstructs from exact Created11/trusted request and ordered
   event bytes, chooses supported family/cut by actual causal kind/version/state (never caller cache),
   bounds histories before enumeration, rejects missing/reordered/extra/foreign/unsupported events.
   Normalize typed current metadata/World/RNG/position/flow/cycle/receipts without loss. Consider
   split pre-cycle and cycle/Reaction routes to honor five-primary-file boundaries.
3. Whole Snapshot12 writer/readback from routed history plus recover-with-admission-disabled entry.
   Compare complete exact bytes; no deserialize-and-trust current fields. Public registration later.
4. Cumulative matrix: creation and every B–G/019A/F1–F6 cut, both owners/forks, exact retries after
   restore, all field/history/cache mutations including re-signed tamper, missing root/prefix/receipt,
   foreign trusted artifacts, size/depth/array limits, detached caller/result buffers, old-reader rejection.
   Fresh disabled rejects new creation but accepts complete retained histories at every cut.

Unresolved before implementation: exact root cycle/reaction arm schema for all early cuts, concrete
ledger routing and normalization API, whole-envelope capacity. Do not claim solved by prospective
RootSnapshot witness or a synthetic legacy Snapshot11 conversion. No blocking verdict yet; this is
bounded contract discovery for a later gate, not a reason to interrupt accepted F3–F6 delivery.

## Routing constraints for final H design

Retained snapshot and retained event history should remain separate inputs: Snapshot12 root must not
silently acquire a new history field. Existing restore requires independent trusted request/registry
and exact Created11; history may be archived separately and must be replayed before snapshot comparison.

Same event version/type occurs on competing paths: ElementMoved4 can be ordinary E1 or F1 return
trigger; close3 can be F3 direct, F4 active fallback, or F2/F6 noeligible; completion3 can be after first
or second participant move. A generic router cannot select semantics by eventType alone or trust a
caller-supplied family label. Classify against reconstructed current causal state and accepted command,
then let exact family replay reject mismatches. Prefix/cut and predecessor history disambiguate.

Candidate H0 contract packet, if required, should preserve existing creation Snapshot12 bytes/root
field order and add bounded typed inherited arms with independently generated full-root goldens.
Do not rewrite old creation-only schema or claim authority-composition RootSnapshot1 is Snapshot12.
Snapshot root current World/version/RNG/position must reflect selected cut, not initial placeholders.
No final API/arm schema choice made by this provisional research.

Admission-policy nuance for H: creation validation reconstructs deterministic expected Created11
without authorizing a new campaign. Existing Decide(retainedCreated, request, false) allows exact
retained evidence; Decide(null, request, false) rejects. Restore must preserve this distinction and
never call a fresh-create publication path simply to read history. Tests must demonstrate both
negative fresh admission and positive retained recovery in the same policy setting; do not claim a
host admission feature flag or durable process restart from an independent low-level codec call.
The eventual Core restore API should make trusted retained evidence explicit, with policy ownership
kept separate from pure canonical reconstruction. Final API choice awaits H contract review.

History trust distinction: replay proves legal/canonical transitions, not that one legal branch was
actually committed. A caller replacing both snapshot and complete history with another lawful fork
cannot be distinguished using recomputable hashes alone. H readback must take independently trusted
accepted history/head evidence separately from snapshot bytes, document that provenance, and reject a
snapshot paired with different trusted history. Do not imply public arbitrary history upload or claim
cryptographic archive authenticity. HOST-PUB-001/provider publication/recovery proof remains later.

## Contract audit at F5 checkpoint6dd60cf

Decision question: smallest contract-first packet needed before initial H C# restore; lead owns
decision. Scope only dormant complete Snapshot12 for implemented inherited cuts. Prohibited:
storage-provider selection, live registration, replacing historical creation goldens, claiming all28
later gameplay traces. Primary sources are canonical schemas/oracles and accepted C# codecs.
Stop when literal envelope gap and bounded next packet are evidenced; no code implementation here.

Documented fact: combat-snapshot-composition-v1.schema.json FullSnapshot fixes reactionWindow:null,
breakdownFlow:Idle, cycleState:CycleFrame and combatState:CombatArm. It defines only selection,
round and settlement arm tags. These shapes cannot represent F1–F6 retained Reaction cuts.
Observed: CampaignCreationSnapshotV12Codec always emits nullcycle/nullcombat/nullreaction/idleflow,
and Deserialize compares against exact creation. CurrentEventRuntime remains Content6/Snapshot11;
it cannot serve as Rules10 restore without later registration changes.
Documented fact: authority-composition oracle build_snapshot emits contractVersion1 with traceId,
sourceContract, fragments and terminalData witness, not full C2 root. RootSnapshot witness cannot
substitute for literal Snapshot12 acceptance.

Options: (1) claim family ReadState sufficient—reject, no full envelope parity; (2) extend existing
creation-only codec to accept arbitrary caches—reject, weakens trusted authority and old-reader
boundary; (3) freeze additive bounded inherited Snapshot12 arms and exact vectors, then separate
history router/root writer—recommended, pending final H scope after F6. Existing creation bytes
must remain identical; do not repin source-hashed predecessor schema files casually.

Normalization inventory from canonical State schemas: preamble carries sequence/header/World/RNG/
receipts; Weather/stage add weather; Reserve adds firstSide/members/cycle/opening/completion evidence;
Movement adds tracks/progress/flow; Reaction adds currentPosition/window; G2 adds interruptContext,
movementEnd and breakdownCompletionReceiptId. H0 must account for each extra, preserve sole current
World at root, and avoid opaque family-state blobs or synthetic root defaults. Unresolved: exact
closed arm placement and capacity proof; trusted retained histories remain separate inputs.

Cross-family equivalence requirement: identical actual retained history/cut must produce identical
full-root bytes, e.g.B2terminal10/D1initial10 and F1terminal13/F2initial13/F3initial13. Adapter family
names or sourceContract tags cannot choose different authority for the same cut. Normalize by actual
state/history and test every shared boundary. Later Combat composition must not erase inherited
members/tracks/progress/G2 evidence merely to fit older synthetic CycleFrame examples; any future
new arm is additive/versioned and retains frozen old examples without claiming authentic lineage.
