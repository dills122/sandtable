# Exercise Runner runbook

Run these commands from the repository root. These are synthetic rules-laboratory fixtures,
not published scenario content or a playable campaign. Public Rules9 actions stop at first-side
Combat entry. Commands below document supported profiles; historical evidence does not certify
their current admission or replace a new run. Read the
[verification inventory](../research/combat-verification-pin-maintenance.md) and
[simulator check-in](../research/simulator-post-merge-checkin.md) for known fixture/pin limitations.

The Runner commands below use the retained Task 007 successor manifests. Original Rules 8 manifests
remain frozen historical inputs and are rejected by current admission. The [closeout evidence](../../docs/research/breakdown-runner-closeout.md) records two clean runs of these commands at that checkpoint.

Run the Organization-boundary Exercise:

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  exercise run --manifest scenarios/exercises/rules-lab.organization.breakdown.v1.json \
  --artifact-root artifacts/exercises
```

The command prints the finalized bundle path. The checked-in manifest is explicitly exploratory,
so a dirty development tree is recorded honestly as nonbaseline and nonreproducible.

The corresponding Stage Entry profile runs all nine accepted actions to the Reserve boundary:

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  exercise run --manifest scenarios/exercises/rules-lab.reserve.breakdown.v1.json \
  --artifact-root artifacts/exercises
```

From a clean checkout, request a fail-closed baseline bundle with the checked baseline twin:

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  exercise run --manifest scenarios/exercises/rules-lab.organization.baseline.breakdown.v1.json \
  --artifact-root artifacts/exercises
```

The Reserve profile has the corresponding clean-checkout twin
`scenarios/exercises/rules-lab.reserve.baseline.breakdown.v1.json`. Run the 12-step Reserve Designation path
through first-side Movement with:

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  exercise run --manifest scenarios/exercises/rules-lab.reserve-designation.breakdown.v1.json \
  --artifact-root artifacts/exercises
```

Its clean-checkout twin is
`scenarios/exercises/rules-lab.reserve-designation.baseline.breakdown.v1.json`.

Set the manifest's `detail` to `compact`, `forensic`, or `debug`. Forensic adds correlated audience
queries, controller selection, checks, proofs, payload sizing, and the progressively assembled
context of failed query/controller/submission decisions. Debug also retains every available
monotonic phase timing on failure and prints a structured post-readback artifact trace. These
diagnostics are trusted local instrumentation and never participate in replay equality.

Run the checked two-child serial Maneuver with:

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  maneuver run --manifest scenarios/maneuvers/rules-lab.serial.breakdown.v1.json \
  --artifact-root artifacts/exercises
```

Run the two-setup Stage Entry regression Maneuver to Reserve with:

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  maneuver run --manifest scenarios/maneuvers/rules-lab.stage-entry.serial.breakdown.v1.json \
  --artifact-root artifacts/exercises
```

Run the two-setup Reserve Designation Maneuver through Movement with:

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  maneuver run --manifest scenarios/maneuvers/rules-lab.reserve-designation.serial.breakdown.v1.json \
  --artifact-root artifacts/exercises
```

Run the six-policy Movement-entry matrix with:

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  maneuver run --manifest scenarios/maneuvers/rules-lab.controller-matrix.serial.breakdown.v1.json \
  --artifact-root artifacts/exercises
```

Run the optional serial-paired Reserve-policy comparison with:

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  maneuver run --manifest scenarios/maneuvers/rules-lab.reserve-policy.paired.breakdown.v1.json \
  --artifact-root artifacts/exercises
```

The pair runs baseline then candidate sequentially in isolated Exercise sessions. Its report may
describe first divergence and outcome/count deltas only; it cannot support causal, statistical,
balance, recommendation, or synchronized-post-divergence conclusions.

Run the paired Movement route-cost sensitivity comparison with:

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  maneuver run --manifest scenarios/maneuvers/rules-lab.movement-cost.paired.breakdown.v1.json \
  --artifact-root artifacts/exercises
```

This unladen-Truck pair keeps declared inputs and initial evidence equal while comparing stable-route
and lowest-public-cost controllers, including each move's BP accounting and explicit stop
resolution. It is simulator instrumentation, not an Umpire rule or gameplay recommendation.

The command prints each validated child bundle path in manifest order, followed by the strictly
read-back aggregate report path and deterministic report fingerprint. The report's local paths and
timings are diagnostics and do not participate in that fingerprint.

Run the bounded Reaction successor or repeated Truck-stop study with:

```sh
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  maneuver run --manifest scenarios/maneuvers/rules-lab.reaction.serial.breakdown.v1.json \
  --artifact-root artifacts/exercises

dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj -- \
  maneuver run --manifest scenarios/maneuvers/rules-lab.breakdown-truck.serial.v1.json \
  --artifact-root artifacts/exercises
```


See the [Exercise Harness specification](../specs/exercise-harness-v1.md),
[design](../design/exercise-harness-v1.md), and [project setup](../../README.md#start-run-and-develop).
