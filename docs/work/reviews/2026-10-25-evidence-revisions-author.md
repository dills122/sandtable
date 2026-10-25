# Author Explanation

## Intent And Success Criteria
Reconcile stale private combat evidence pins without invalidating retained legacy evidence. Complete registered legacy/refreshed dependency sets only; proof identity must follow selected manifest. Unknown/mixed bytes fail closed. No public admission or RC certification.

## Plan-To-Implementation Traceability
Original docs-only reconciliation failed native authentication (CMB-MAS-009). Human approved explicit versioned native manifests after review 5. Native snapshot/registry, selected-manifest propagation, retained legacy archives and focused compatibility tests implement expanded plan. Final evidence checks pending; no ready claim.

## Technical Approach And Flow
CombatDependencySnapshot copies registrations and callback bytes, authenticates complete set, rejects mixtures, propagates selected revision through nested delegate.Target. CombatEvidenceRevisions maps exact legacy tuples to reviewed refreshed tuples; family registrations remain legacy. Manifest hashes use selected runtime pins, excluding delivery pins. Existing native codecs authenticate delivery. Static legacy ManifestHash stays available for older internal callers; live replay uses ManifestHashFor/dependency-aware Manifest.

## Changed-Component Walkthrough
Six moved codecs/replayers carry selected manifest into proof construction. Paid, post-assault, repeated movement/completion/breakdown and round entry capture dependency readers at boundaries. Direct repeated-breakdown Derive also captures (focused test found missing boundary). Two new helpers centralize ownership/selection. Legacy ZIP retains 145 base files; gzip retains two full genuine R03 vectors independently matched against original summaries. Tests cover legacy cuts/proofs/retries, cross-revision proof rejection, identity-bound old R03 under refreshed rejection, mixed sets, ownership, concurrency and metadata mutation.

## Decisions And Rejected Alternatives
Blind pin replacement breaks old identities; unchecked caller manifests weaken authentication. Retaining two code-owned exact maps preserves old readers and rejects unknown future revisions. No automatic persistence-store migration. Python owning generators unchanged except declared dependency digest constants; generation uses existing owning functions and split exact row sets for expensive 300-row fixture.

## Invariants And Boundary Conditions
No combat resource/choice/RNG/count changes. Some source histories lack manifest binding and replay identically under both revisions; proof claims still bind selected manifest. Legacy identity-bound commands/events remain exact and cannot translate to refreshed receipts. All buffers owned. Some pre-existing families require physical callbacks to remain stable even within nested use; Revalidate retains that behavior rather than treating cached bytes as immunity from physical drift.

## Verification Performed And Results
Artifacts/rc-source-pins contains durable commands/receipts and logs. Snapshot helper 11 passed. Refreshed moved-assault parity 56 passed. New Legacy* focused run 33 passed (includes existing legacy-named tests). Post-assault 4, repeated movement 4, movement lifecycle 20 passed; direct repeated-breakdown capture fixed after 20 failures, rerun 20 passed. Paid and post-assault physical drift tests passed. Current final native batch covers moved continuation/cycle/repeat movement, all300 R02 traces and four selected R03 cuts; pending. Original Cycle/Breakdown/Selection/Armed/NoAttack/CycleControl/ReserveMovement/ReserveMovementCompletion/AuthorityComposition oracles passed. Snapshot retains unrelated CurrentEventRuntime and CampaignElementMovedV3Factory pin mismatches (first reported failure is CurrentEventRuntime). Side Projection failed three preserved-subset hashes now proven pin-only metadata differences; correction prepared, not yet applied while dependent generation running. B1/B2/B3 pending. Build passed; final format/negative checks pending.

## Risks, Tradeoffs, And Maintenance Costs
Frozen registrations require deliberate future revision edits and retained dependency storage. ZIP is about13MB, expands to about261MB. Native helpers do not fetch archives or upgrade external stores. Nested rereads/byte copies preserve existing validation but can cost CPU/I/O; no speed claim. Exact-head CI still required before merge. No full Core solution gate claimed.

## Deviations, Deferrals, And Known Gaps
Snapshot and Outward drift separate; no unsupported ancestry promotion. Original300 roots44positive/256absent/0admitted remains. R03 legacy regression crosses genuine paid and armed ancestor chains but does not exhaust all300 historical rows. Final generation/semantic map receipts and review delta will be supplied; final review must not infer passes.

## Challenge Points For The Reviewer
Boundary coverage of all direct/internal replay/derive entry points; inherited revision retention when subset bytes identical; legacy wire/proof identity; snapshot aliasing/physical rereads; complete manifest path sets; generated changes restricted to provenance and declared pins; archive completeness; no accidentally weakened negative tests.


## Final native verification update

Final native summary (`artifacts/rc-source-pins/final-native-summary.json`) records 659 passing cases across two focused batches: legacy33, moved continuation56, moved cycle112, repeated movement56, movement completion300, selected repeated breakdown4; then direct-entry clock2, warm physical dependency1, warm handler/capacity1, owned delivery2, snapshot11, boundary81. First batch preceded isolated internal RMC Derive capture correction; second batch rebuilt final source and exercised that correction through both complete inherited clock flows. No affected manifest bytes changed with that internal correction. Prior moved-assault56 and non-moved repeated4+4+20+20 parity checks passed. No full Core solution or exact-head CI claimed.

Final formatting verification passed; builds passed. All27 native refreshed physical registrations still match actual bytes. Graph coverage33 changed/new C# files clean best-effort metadata, generation15:13:08Z, checked15:25:21Z; not proof of semantic completeness.

Final Side preserved subset and source-pin checks passed; Python AST comparison confirms only declared pin dictionaries and three subset digest constants changed, no oracle logic. Final Exercise Child representative7, failures/forgery22, controller identity and exact fixture check (134 summaries/12 goldens) passed. Full134-source registry passed before final subset digest correction; final full-registry gate remains explicitly unrun. Final Exercise Parent original main passed all7 groups; final pin closure changed0 slots.

Final disposition: independent review6/6 Ready with non-blocking follow-ups; accepted Derive finding closed. Report and validation handoff preserve release exclusions and exact unrun coverage.
