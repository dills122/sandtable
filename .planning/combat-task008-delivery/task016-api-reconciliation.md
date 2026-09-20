# Task016 API reconciliation — research only after015 freeze

Read root task016-dispatch-proposal.md against frozen015 source. No implementation, .NET, oracle execution, review inspection or other file edits. CCE context_search was attempted for source discovery but returned Transport closed; scoped exact source reads used instead. Root decides dispatch after015 acceptance.

## Decision and exact physical scope

One bounded slice remains feasible: five material files plus THREE narrow existing-test maintenance files, eight physical paths. No five-file claim:

1. src/Cna.Core/Campaigns/CampaignCombatResolution.cs — state metadata, three typed effects, structural transitions and terminal guard.
2. src/Cna.Core/Campaigns/CampaignCombatResolutionCodec.cs — actual metadata/effect/relationship writers, complete stage and closure consistency validation.
3. src/Cna.Core/Campaigns/CampaignCombatObligations.cs — named immutable WithResultV2Relationships append through existing private constructor.
4. src/Cna.Core/Campaigns/CampaignCombatLossRetreat.cs — extend existing causal projection through original-participant relationships; no new generic framework.
5. tests/Cna.Core.Tests/Campaigns/CombatClosureTests.cs — new full closure proof.
6–8. Existing CombatResolutionTests.cs, CombatLossRetreatTests.cs, CombatCustodyTests.cs — remove/replace obsolete future-family rejection assertions only. Preserve original 32/64, 144/176 and 176/208 golden boundaries and all earlier behavior tests. Custody End helper must continue finding first relationships event, not become full-history length. New closure suite owns final suffix/closed rejection proof.

No World7, receipt model, schema, fixture, topology, Round or csproj change needed. Existing fixture link and local grammar already include all final arms. Eight-path scope is explicit maintenance plus substantive implementation, not an architectural pivot.

## Frozen APIs and required evolution

Resolution.cs:67–97 owns CombatResolutionState and effect hierarchy. State Context is get-only; World/Result/version/prefix/window are init-only; Receipts setter copies collection. Add nullable RoundClosureReceiptId and CaCompletionReceiptId and Closed, or equivalent derived representation yielding exact frozen fields. Keep immutable paid Context and original Base unchanged. Add Relationships(payload), RoundClosed(settlementId, owned proofReceipts), CaCompleted(fromPositionId,toPositionId,previousStepReceiptId,roundClosureReceiptId). Copy proof collections at construction; do not expose mutable aliases.

Existing ReplayTrustedBoundary/ApplyTrustedBoundary signatures remain unchanged: request, independently retained Created, trusted Boundary, separately trusted predecessor inputs/events, round inputs/events, then result inputs/events. Replay:133–163 clones Created before caller list access, validates all event syntax before trusted replay and limits result history to32; this accommodates longest complete fixture. No supplied prior state becomes Apply authority.

Transition:177–184 checks exact Command hash plus actor retry first, then stale/no-window expire/unavailable NoOp. Insert closed guard AFTER those two cases, with existing receipt/version guard. Therefore every prior accepted command can recover its original event and current closed state; stale callback remains NoOp; fresh resolve/advance/choose cannot mutate closed state. Preserve actor/context/field validation before retry.

Structural advance: add retreat-without-positive-custody and custody → relationships; relationships → round-closed; round-closed → closed. All require null time, available confidence, correct expected version and no window. Existing positive-lot opening branch already prevents skipping custody. At final event assembly:282–294, derive unsigned event and receipt as before, then store receiptId in the appropriate closure field before SerializeState. RoundClosed retains its own event receipt; CA effect links that exact retained receipt. Closed becomes true only for CA completion. Neither closure changes World, RNG, paid commitment, attack history or target uses.

Codec:48–77 currently writes null/null/false closure metadata; replace with actual state fields. WorldValue:105–131 currently enumerates status/receipt/window.Kind stages and compares entire typed World to Project. Extend statuses relationships/round-closed/closed with complete immediate receipt chain and null window. Require no closure IDs/Closed before round-closed; round-closed has round ID only and false; closed has both and true. Check retained closure IDs against appropriate terminal receipts/state stage, not merely stable-ID syntax. ReadState still performs full trusted replay and complete byte equality. Metadata/foreign IDs and fabricated serializer states require failure-sensitive tests.

Codec:145 currently hardcodes settlement relationships null; root World relationships must likewise be written from validated typed projection. Do not serialize arbitrary caller JSON. Existing local descriptors CloseEffect and CaEffect already define exact proof/position/receipt fields; retain current grammar and array order. Raw malformed proof/payload must fail before trusted context.

Obligations:281–293 has WithResultV2Custody and ResultV2Next passing relationships:null to private constructor. Add WithResultV2Relationships(CampaignCombatRelationshipsReceipt), require disposition/losses/retreat, no prior relationships, and custody iff any loss role CapturedToe>0. Extend private ResultV2Next optional relationships argument; reuse resolvedV2:true private constructor. Its unconditional ResultV2SettlementId check:304–305 must remain on every append. Legacy constructor semantics remain unchanged.

## Relationship projection and World7 distinction

LossRetreat.Project:103–138 currently finishes either before custody or after custody, comparing expected==retained before returning. Relationship append must occur after either branch has produced the exact allowed post-custody frontier. Refactor these local terminal branches into one bounded finalization path; preserve intermediate typed World construction with expected frontier settlement, then derive/apply relationship and compare final expected settlement. Do not prematurely pass retained relationship receipt into a World and rely on World7 to authenticate resources.

Derive adjacency using current ORIGINAL attacker/defender locations from reconstructed frontier and authenticated Content7 edges. Receipt predecessor is custody if positive capture, otherwise retreat. ID is settlement+'.relationships'; relationship ID settlement+'.relation' only if adjacent. Kind engaged iff RawEngaged && RequiredRetreat==0, otherwise contact if adjacent; both kind/relationshipId null if nonadjacent. Raw required retreat suppresses engaged even if refused and actual distance zero. New guards and arrivals never participate. Relationship state uses original unit keys and settlement gameTurn/operationStage, active true and null ending metadata. No victim/guard substitution or result-table recalculation.

World7.cs:137–141 always validates relationship/custody/causes but deliberately skips ValidateOpenSettlementEffects after Relationships exists, permitting future movement. That is NOT permission for Result2 to accept changed post-settlement elements. Codec must continue state.World == Project(...) through relationships, round-closed and closed. Add typed fabricated World regression that World7 can construct after relationships but SerializeState rejects (CP/TOE/location/resource mutation); also causal raw rehashed-state rejection. Preserve all guards/lots/entitlements/future obligations and canonical origins unchanged.

## Topology and proof

Use authenticated Boundary position with Cna1979LandSequence.CreateTurn(1), as existing SealedRound.Route:247–252 does. Resolve seven-position route beginning at Boundary.Position.PositionId; completion from combat index5 to release index6. Validate expected topology positions rather than fixture strings. Existing committed StepIndex==5 and StepReceipts.Count==5 replay guard remains prerequisite. CA links committed.StepReceipts[^1], not latest Result2 receipt or an invented sixth step receipt. Round closure proof order is disposition, losses, retreat, optional custody, relationships. Future obligation IDs are excluded; retained future obligations do not block closure and are not executed.

Authoritative transition source: docs/specs/verify-combat-result-settlement-v2.py:173–190 retry/closed ordering, :230–248 structural final stages/proof/topology, :255–260 receipt metadata assignment. Governing prose docs/specs/combat-result-settlement-v2.md:117–135; canonical task docs/design/combat-cycle-implementation-plan.md:913. Existing World7 ValidateRelationship supplements but cannot replace Content adjacency and raw-retreat causal checks.

## Read-only fixture recount and test plan

Parsed docs/specs/fixtures/combat-result-settlement-v2.json with Python json only; no oracle imported/executed. Confirmed32 traces;272 resultEventCanonicalUtf8 entries;304 stateHashes;32 stateCanonicalUtf8 final complete literals. All final states status closed/closed true. New families each32: relationships-settled, round-closed, ca-completed, adding96 events/cuts to015176/208. Relationship kinds16 contact,4 engaged,12 null. Proof lengths4 for16 noncapture,5 for16 capture. Fixture counts are research inventory, not C# execution evidence.

First RED: authentic015 terminal prefix + next literal relationships advance. Then all272 exact literal events,304 causal hash readbacks,32 exact final JSON states. Count16/4/12 and4/5 explicitly. Retain original/refusal/null geometry and both sides/seal orders. Verify no costs, RNG, custody, entitlement or future-obligation mutation across three new transitions; exact fifth-step/topology links and round receipt linkage.

All-cut retry/discard including closed; retry every accepted input after final closure; fresh closure/resolve/choose rejection; stale callbacks NoOp; duplicated/suffixed accepted-event stream rejection. Rehash relation kind/unit/scope/predecessor, proof missing/reordered/duplicated/foreign IDs, CA from/to/previous-step/round receipt and root metadata. Serializer stage/window/Closed/closure ID mismatch and full World equality after relationship receipt are separate regression requirements. Raw payload/proof bounds/type/canonical-before-context and owned proof/byte buffers retain strictness. Shared existing World/rules/identity/steps/seals/commit regressions, scoped format and parent full gate/reviews remain required.

## Blockers and boundaries

No contract or material architecture blocker found. Local projection branch restructuring is necessary and bounded; no shared World7 relaxation is justified. Parent must explicitly authorize eight physical paths after015 acceptance. Trusted synthetic Boundary remains mechanism provenance, not authenticated positive creation-rooted history. No Reserve execution, cycle repeat, Snapshot successor, host publication or actual positive admission claim.
