# CON006 preparation (read-only)

Agent task004_evidence complete;004B implementation not begun. No source/gameplay ambiguity found.
Full structured source return in task conversation; key evidence retained here for resumption.

## Required successor seams

1. Occurrence terminal: game/stage/relative slot/side/ordinal/structural position/closure/obligations.
   Movement1 cannot satisfyMovement2. Unsupported terminal rejects before execution; same-slot Truck
   Convoy entry preserves future guard/upkeep/replacement records and cannot cross maturity.
2. Dual-slot scheduling: pin deterministic selected order in typed manifest/config, record actual
   order and reproduce re-adjudication. Evidence both permutations; historical exactly-one-active-
   audience invariant unchanged.
3. Checkpoint continuation: full retained history and exact suffix reproduce pending owner/IDs/
   candidate bytes/receipts. Missing history/duplicate repeat/reopened control/guessed ordinal reject.

Sources: docs/design/continual-cycle-reserve-composition-v1.md:244–268,287–307;
docs/design/exercise-harness-v1.md:391–424;
src/Cna.ExerciseRunner/Execution/ExerciseExecutor.cs:451–488;
src/Cna.Core/Exercises/ExerciseCheckpoint.cs:8–24.

## Child evidence and parent report

- Ordinary Core authority only; controllers choose approved candidate IDs, never adjudicate.
- Reconstruct from creation/events; independent re-adjudication regenerates observations/candidates,
  submits recorded audience/action and compares receipts/events/final snapshot.
- Strict manifest fields/order/version/terminal/bounds/build/controller/schedule/negative assertion.
- Contiguous action/step/event ordinals, receipt version continuity and digests; terminal, decoded
  snapshot and final step agree. Self-consistent forged hashes insufficient.
- Failure/step-limit/cancel never success, even matching expected-failure assertion.
- Manifest-last confined fixed paths, bytes/sizes/hash, strict reopening; partial artifact not complete.
- Validate all children before aggregation, expected materialized manifest/seed/build/config bindings,
  exact counts/order/fingerprint. Trusted raw authority remains private; CON005 governs export.
- Repeat clean runs compare deterministic simulation subset excluding diagnostics/path/time.

Sources: exercise-harness-v1.md:94–148,174–180,192–225,345–387,449–492;
src/Cna.ExerciseRunner/Execution/ReadjudicationVerifier.cs:22–93;
src/Cna.ExerciseRunner/Artifacts/ExerciseBundleSemanticValidator.cs:90–125,179–210.

## Paired comparison

Same declared setup/content/scenario/rules/terminal/bounds/build/confidentiality/assertion and actual
creation/seed/initial bytes. Exercise IDs differ; controllers may differ. Ordered baseline/candidate.
First accepted-action ordinal with changed audience or action ID is divergence; unequal lengths use
shared-prefix length and null missing arm. No-divergence has no invented ordinal. Compare only two
validated children; incomplete fields null. Recompute from transcripts. No RNG-purpose alignment
claim after divergence. Equal private forks may preserve CON005 references.

Sources: Artifacts/PairedManeuverManifestContracts.cs:52;
Execution/PairedManeuverPairingEvidence.cs:45;
Artifacts/PairedManeuverReportContracts.cs:138,477 under src/Cna.ExerciseRunner.

## Exact current inventory

Checkpoint1 wraps CoreSnapshot11; reconstruction result1. Exercise manifest payload2 but registry
label `sandtable.exercise-manifest.v1`; snapshot artifact label `sandtable.campaign-snapshot.v1`.
Serial Maneuver manifest2/labelv2. Paired manifest/pair1. Run result/artifact manifest/accepted-action/
step evidence/reconstruction proof/re-adjudication proof/checks/serial report/paired report/build/
seed ledger/campaign-ID/pairing-input scheme all1. Controller candidate3/configuration2.
Do not normalize historical label/payload mismatch or rely on stale harness prose calling manifestv1.

Registry: Artifacts/ArtifactSchema.cs:24; ExerciseContracts.cs:76; ManeuverManifestContracts.cs:115;
PairedManeuverManifestContracts.cs:91; ReplayProofContracts.cs:9,77;
ManeuverReportContracts.cs:588; PairedManeuverReportContracts.cs:555;
Controllers/ExerciseController.cs:9; Execution/ExerciseConfigurationIdentity.cs:9.
Proof hash framing: ReplayProofContracts.cs:138–150, big-endian Int32 length+record, not JSONL LF or
cycle-prefix U64. Preserve distinction.

## Suggested bounded split

004B1 terminal/continuation/scheduling;004B2 strict child evidence;004B3 parent/paired evidence.
Each spec/schema/fixture/oracle plus combined plan <=5 primary files; navigation separate.
No IDs/write ownership authorized yet.004A acceptance remains prerequisite to implementation.

## Verification status and limitations

No tests executed in this preparation. Existing paired report codec/executor tests inspected only.
High confidence source/version inventory, no claim about successor schema, runtime or new gate.
Mandatory negatives include ordinal/terminal/obligation tamper, schedule/order, missing history,
forged proofs, corrupt/foreign children/counts, false divergence, negative-success conflation,
malformed versions/path/status, raw-bundle side disclosure and false second-assault claims.
Next action: finish004A then allocate bounded004B packets using this source checklist.

## Root baseline cross-check, 2026-09-15

ExerciseBundleSemanticValidator555–836 confirms seven ordered passed records per accepted step;
zero-step success requires identical initial/final bytes at requested boundary; StepLimitExceeded
requires exact maximum accepted count and no failed decision record. Execution failure has no
proofs; reconstruction/readjudication failure follows ordered terminal/check/proof-presence matrix.
PairedManeuverPairingEvidence45–85 compares actual audience+actionID, uses null missing arm for
unequal length and null divergence for identical streams. PairedManeuverExecutor204–272 additionally
requires exact initial snapshots, count integrity, seed ledger, creation inputs and build cohort.
These are source checks for B2/B3 planning, not new runtime tests or contract acceptance.
