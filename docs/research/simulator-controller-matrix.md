# Simulator Controller Policy Matrix

**Status:** Completed; acceptance criteria passed

**Date:** 2026-08-25

## Decision question

Can the current Exercise/Maneuver harness deterministically cover both initiative declarations and
zero/one/all Reserve choices before Movement implementation begins, without adding authority or
depending on scenario-specific remaining-candidate counts?

## Scope and method

The experiment used merged base `e09008bc3de2bc3a0a002fc4e0d9f2b02c221f4f` plus the
`EXR-TASK-014J` controller-policy implementation. One checked serial-unpaired Maneuver contains six
children crossing `act-first`/`act-last` with Reserve `none`/`one`/`all`. Every child uses the same
predetermined synthetic setup, root seed 0, Movement terminal, and ordinary legal-action query and
submission path.

The pure controller receives current legal candidates plus only that audience's count of previously
accepted Reserve-designation actions. It does not read a snapshot, authority handle, hidden
opposing state, or content context. Each completed bundle and the aggregate report are strictly read
back through the existing trusted-evidence pipeline.

Command:

```text
dotnet run --project src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj --no-build -- \
  maneuver run \
  --manifest scenarios/maneuvers/rules-lab.controller-matrix.serial.v2.json \
  --artifact-root artifacts/simulator/controller-matrix
```

## Evidence

### Documented facts

- `Cna.Core` remains the sole authority; the runner can select only one advertised action ID and
  submits it through the ordinary membership-checked action boundary.
- Movement remains a terminal checkpoint. This package adds no Movement candidate, command, event,
  mutation, or rules behavior.

### Observations

| Initiative policy | Reserve policy | Accepted actions | Reserve-I elements | Holder acts first | Result |
| --- | --- | ---: | ---: | --- | --- |
| `act-first` | `none` | 10 | 0 | yes | Movement; strict readback passed |
| `act-first` | `one` | 11 | 1 | yes | Movement; strict readback passed |
| `act-first` | `all` | 12 | 2 | yes | Movement; strict readback passed |
| `act-last` | `none` | 10 | 0 | no | Movement; strict readback passed |
| `act-last` | `one` | 11 | 1 | no | Movement; strict readback passed |
| `act-last` | `all` | 12 | 2 | no | Movement; strict readback passed |

- All 6/6 children reconstructed and re-adjudicated exactly.
- The aggregate retained 6 successful children, one Movement terminal count of 6, and zero failed
  or aggregate-invalid artifacts.
- Two independent runs produced the same deterministic report fingerprint:
  `sha256:cab825d30b128ab1f1e2032879ca0ac3f793abc054a2c710dbdf22e93f49e71c`
  under ruleset v7.
- The complete ExerciseRunner suite passed 293/293; the solution suite passed 705/705 with zero
  skipped tests, and the solution build completed with zero warnings and zero errors.

### Inference

The policy gap identified by Simulator Baseline 2 is closed for the current pre-Movement boundary.
Because `one` is driven by accepted controller history rather than the number of remaining
candidates, it is not coupled to the present two-eligible-element setup. The six resulting snapshots
are suitable deterministic starting trajectories for the first Movement vertical.

## Decision

Stop expanding pre-Movement seed sweeps. Keep this six-path Maneuver as a checked regression matrix
and move the primary delivery track to source-backed Movement research, specification, and design.
Defer an in-process performance/batch entry point until Movement creates longer trajectories where
separating CLI startup from adjudication cost materially helps engineering decisions.

## Limitations

- This is policy and evidence coverage, not balance, performance, or outcome analysis.
- All six children use one synthetic setup and root seed.
- No movement, contact, combat, victory, persistence, Maproom, model controller, or side-safe export
  is implemented or implied.
