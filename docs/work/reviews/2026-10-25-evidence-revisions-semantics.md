# Evidence revision semantic comparison

Status: native and Exercise Child/Parent fixture comparisons complete; final independent disposition recorded separately.

Baseline: exact files retained in `docs/specs/fixtures/combat-evidence-legacy-v1.zip`, base `3e2d4336b1505d9f308529841361e5413cf70f74`.

Comparison recursively decodes nested canonical JSON strings. All compared native fixtures retain exact object keys, array lengths, scalar types, numbers and booleans. Changed strings listed below. No resource totals, owners, choices, clock values, counters or capability labels changed. Twelve older fixture families (Cycle through Exercise Occurrence) retain every non-pin JSON value exactly; comparison excludes only named pin containers, not arbitrary hash fields.

Provenance dependency chain: changed source bytes alter declared pin maps; sorted runtime maps alter domain-separated dependency manifests; manifests alter arithmetic headers and basis hashes; basis hashes alter generated command/event IDs, receipts, rolling prefixes and references; parent provenance changes cascade into child source/control/proof hashes. Delivery files authenticate physical artifacts but remain excluded from runtime manifest preimages. Compact `sha256` leaves identify source/control/proof cuts and are checked by native replay parity, not assumed interchangeable. Changes to owner/resource/choice fields would fail comparison. Original generator functions produce these artifacts; native parity is separate validation.

| Fixture | Changed string fields |
| --- | --- |
| `docs/specs/fixtures/combat-actual-assault-v1.json` | arithmeticCatalogueHash, dependencyManifestHash, docs/specs/combat-actual-round-entry-v1.schema.json, docs/specs/fixtures/combat-actual-round-entry-v1.json, docs/specs/fixtures/combat-cycle-sequence-v1.json, sha256 |
| `docs/specs/fixtures/combat-actual-moved-assault-v1.json` | sha256 |
| `docs/specs/fixtures/combat-actual-moved-continuation-v1.json` | sha256 |
| `docs/specs/fixtures/combat-actual-moved-cycle-control-v1.json` | baseHash, controlId, decisionId, priorPrefix, receiptId, sha256 |
| `docs/specs/fixtures/combat-actual-moved-repeat-breakdown-completion-v1.json` | afterControlHash, afterProofHash, beforeControlHash, beforeProofHash, eventHash, parentSourceHash, receiptId, sourceHash |
| `docs/specs/fixtures/combat-actual-moved-repeat-movement-completion-v1.json` | baseHash, capabilityId, cycleId, dependencyManifestHash, eventHash, lastMoveReceiptId, movementSourceHash, openingPrefix, priorPrefix, receiptId, sha256, stopId |
| `docs/specs/fixtures/combat-actual-moved-repeat-movement-v1.json` | baseHash, controlId, cycleId, decisionId, intentReceiptId, nextCycleId, openingPrefix, parentSourceHash, priorPrefix, receiptId, sha256 |
| `docs/specs/fixtures/combat-actual-paid-assault-v1.json` | arithmeticCatalogueHash, baseHash, caCompletionReceiptId, commandHash, commitmentId, creationReceiptId, dependencyManifestHash, eventHash, predecessorReceiptId, prefix, priorPrefix, proofReceipts, receiptId, relationId, relationshipId, resultId, roundClosureReceiptId, settlementId, sourceHash, sourceId |
| `docs/specs/fixtures/combat-actual-post-assault-continuation-v1.json` | arithmeticCatalogueHash, baseHash, caCompletionReceiptId, commandHash, commitmentId, controlId, creationReceiptId, decisionId, dependencyManifestHash, eventHash, eventHashes, openingPrefix, openingReceiptId, paidSourceHash, predecessorReceiptId, prefix, priorPrefix, proofHash, proofReceipts, receiptId, relationId, relationshipId, releaseCompletionReceiptId, releaseId, releaseOpeningReceiptId, resultId, roundClosureReceiptId, settlementId, sourceHash, sourceId |
| `docs/specs/fixtures/combat-actual-repeated-breakdown-completion-v1.json` | arithmeticCatalogueHash, baseHash, breakdownCompletionReceiptId, caCompletionReceiptId, capabilityId, causeId, commandHash, commitmentId, completionReceiptId, continuationSourceHash, controlId, creationReceiptId, cycleId, decisionId, dependencyManifestHash, endedByReceiptId, eventHash, eventHashes, lastMoveReceiptId, movementCompletionReceiptId, movementSourceHash, openingPrefix, openingReceiptId, paidSourceHash, predecessorReceiptId, prefix, priorPrefix, proofHash, proofReceipts, receiptId, relationId, relationshipId, releaseCompletionReceiptId, releaseId, releaseOpeningReceiptId, repeatedMovementCompletionSourceHash, resultId, roundClosureReceiptId, settlementId, sourceHash, sourceId, stopId |
| `docs/specs/fixtures/combat-actual-repeated-movement-completion-v1.json` | arithmeticCatalogueHash, baseHash, caCompletionReceiptId, capabilityId, causeId, commandHash, commitmentId, completionReceiptId, continuationSourceHash, controlId, creationReceiptId, cycleId, decisionId, dependencyManifestHash, endedByReceiptId, eventHash, eventHashes, lastMoveReceiptId, movementSourceHash, openingPrefix, openingReceiptId, paidSourceHash, predecessorReceiptId, prefix, priorPrefix, proofHash, proofReceipts, receiptId, relationId, relationshipId, releaseCompletionReceiptId, releaseId, releaseOpeningReceiptId, resultId, roundClosureReceiptId, settlementId, sourceHash, sourceId, stopId |
| `docs/specs/fixtures/combat-actual-repeated-movement-v1.json` | arithmeticCatalogueHash, baseHash, caCompletionReceiptId, causeId, commandHash, commitmentId, continuationSourceHash, controlId, creationReceiptId, cycleId, decisionId, dependencyManifestHash, endedByReceiptId, eventHash, eventHashes, openingPrefix, openingReceiptId, paidSourceHash, predecessorReceiptId, prefix, priorPrefix, proofHash, proofReceipts, receiptId, relationId, relationshipId, releaseCompletionReceiptId, releaseId, releaseOpeningReceiptId, resultId, roundClosureReceiptId, settlementId, sourceHash, sourceId |
| `docs/specs/fixtures/combat-actual-round-entry-v1.json` | docs/specs/fixtures/combat-cycle-sequence-v1.json |
| `docs/specs/fixtures/combat-exercise-child-evidence-v1.json` | buildHash, childHash, fixtures/combat-exercise-occurrence-v1.json, hash, manifestHash, verify-combat-exercise-occurrence-v1.py |
| `docs/specs/fixtures/combat-exercise-parent-evidence-v1.json` | buildHash, childHash, combat-exercise-child-evidence-v1.schema.json, fingerprint, fixtures/combat-exercise-child-evidence-v1.json, hash, initialEvidenceHash, manifestHash |

Detailed per-field counts and before/after examples: `artifacts/rc-source-pins/semantic-*.json`. Generation receipts retain commands, failures and elapsed times. This comparison alone does not prove generated identities correct; focused native parity and independent review remain required.


## Reproduction boundaries

Owning Python verification modules remain authoritative. Artifact construction used their existing functions, not handwritten expected identities:

- Round entry: `semantic_tests()` followed by `fixture_case` for each result.
- Actual assault: original case arguments, `proof` and `raw(..., 'AdmissionProof')`.
- Paid assault, post-assault and non-moved repeated families: `fixture_artifacts()`.
- Moved assault: `generate()` plus `proof_for` at each retained cut; compact byte counts and SHA256 values retain existing fixture shape.
- Moved continuation/cycle: `generate()`.
- Moved repeat movement/completion: owning trace functions partitioned by original row ordinal into four chunks, merged in original order. Completion assertions require 300 rows, 788 inputs, 1,088 cuts and 3,396 parent replays.
- Moved repeat breakdown: `verify([0, 2, 36, 41])`; existing 300-row declaration and selected ordinals retained.
- Exercise Child: original registry/admission/failure/identity groups populate `RUNS`, then `generated_fixture()`. After final digest-only correction, original representative/failure/identity groups regenerate selected goldens; this is explicitly not a full final-head registry run.
- Exercise Parent: `generated_fixture()`, followed by original `main()` for final verification.

Schema/source-pin closure was updated only at declared path-to-digest containers. Side Projection's three preserved-subset digest constants are hashes of existing fixture subsets after pin-only changes; all non-pin values were compared against archived base before updating constants. Python AST comparison permits only declared pin values and these three constants, with no oracle logic edits. Final release still requires applicable full owning-oracle gates on exact release head.

Exercise provenance adds `buildHash` (oracle/schema/predecessor descriptor), `manifestHash`, child evidence hashes, artifact-entry hashes and parent fingerprints/initial-evidence hashes. Original child/parent validation functions independently recompute and reject forged identities. Non-pin schema grammar, limits, fixture membership, numeric/boolean values and runtime choices remain unchanged. Final oracle results and unrun coverage are listed in validation handoff.
