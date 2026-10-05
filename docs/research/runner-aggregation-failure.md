# Runner aggregation failure research

Status: bounded diagnosis; historical cause **unconfirmed**. R2 overnight research, 2026-10-04 America/Toronto (2026-10-05 UTC).

## Decision and boundary

Question: what produced `AggregationFailed` in `CertifiedMovementCostPairRetainsRepeatableRouteDivergence`, and does the evidence justify a deterministic bounded fix?
Decision owner: overnight coordinator `01a0c9dc-00bc-78a3-800d-3cb36859e422`.

The available evidence identifies the reporting chain, not the original failing branch. No production repair is justified yet. Preserve failure diagnostics before considering a fix. A passing rerun does not establish cause or exclude interaction with new code.

Scope: source/log forensics and at most one temporary present-environment control. No retained executable edits, retries, timeout changes, skips, assertion weakening, public contracts or canonical Combat documentation changes. Research budget is 30–45 minutes; stop earlier when available evidence cannot distinguish causes. Full-suite/heavy processes require the coordinator's host lease. A future diagnosed fix requires coordinator-assigned exact ownership (at most three files), meaningful RED, GREEN, fresh review and final CI.

Source hierarchy: retained failure logs; finalized report/child artifacts if available; exact repository source at the retained checkpoint; handoff/review testimony; controlled present-environment observation. Success means an auditable diagnosed cause **or** an honest unknown with a concrete diagnostic next step.

## Historical observations and provenance

The original task used `/Users/dsteele/.codex/worktrees/native-settled-continuation/sandtable`, branch `codex/native-settled-continuation`, base `f33c78cdb4aa44c694e0baf3aeb963d98819ce7b`. Retained implementation commit `e8fbaefd9c73edb198103ef1b4e9423ec89befe1` was committed at 2026-10-04T23:53:42Z, after these gate logs. It is a source checkpoint, not proof of the exact original executing commit/binary. Source/test hashes and dirty scope were independently recorded in the [019D1 review](../work/reviews/2026-10-04-native-settled-continuation-review.md); [019D1 handoff](../work/handoffs/2026-10-04-native-settled-continuation.md) records commands and wrapper provenance.

| Observed invocation | Result | Evidence |
| --- | --- | --- |
| `PATH="/private/tmp/cmb019d1-tools:$PATH" just check` | Restore, format, build (0 warnings/errors), Boundary 81/81; solution 2495/2496 pass, one failure, 0 skipped; 7m43.953s | `/private/tmp/cmb019d1-just-check-final.log`, lines 1–64 |
| Filtered one-test rerun (`--project tests/Cna.ExerciseRunner.Tests/Cna.ExerciseRunner.Tests.csproj --no-build`, filter not printed in log) | 1/1 pass, 0 failed/skipped, 9.022s | `/private/tmp/cmb019d1-runner-repro.log` |
| Unchanged complete `just check` repeat, per contemporaneous review | Restore/format/build/Boundary 81 pass; solution 2496/2496 pass, 0 failed/skipped, 7m33.826s | `/private/tmp/cmb019d1-just-check-repeat.log` |

The failed test lasted 6.810s. Stack lines 331/339 identify `Run("movement-cost-first")`, before the second paired invocation. This does **not** identify baseline versus candidate child. Command exit was `AggregationFailed` (14), while MTP/recipe exit was 2; these are separate layers.

Failure log SHA256: `31f2cc3f7328fb0ed995c7968573025e1298b09d4148522f2ba7d261fd48831c`.
Filtered rerun log: `4de01fcb4f35fab4eade35b57ae665063c1e57f24351d191b67a9371dcace8a8`.
Repeat log: `97e29245d8c29b9829d736a53a6dc2fe3f83e6056e420d223612bedc15154ac1`.
UTC last-write times respectively 23:43:55Z, 23:38:39Z, 23:52:18Z on October 4 (filesystem observations, not authoritative process start/end timestamps). Filtered rerun binlog name contains `20261004-233829`; it completed before the failure log finished. Thus “isolated” establishes one selected test, not an otherwise idle host. Exact overlap/process scheduling cannot be reconstructed from those names alone.

Historical logs establish net10.0/arm64 and `/opt/homebrew/bin/dotnet` via the retained wrapper. Historical SDK/runtime patch, OS release, Git stderr, disk/resource pressure, cancellation state, exact original binary hashes and process inventory were not retained here. Current SDK/version observations must not be projected backwards.

## Source facts: reporting chain

All paths below refer to research base `907f41403ed159d65024f193f3e1f730a23b9bbb`. `git diff e8fbaefd9c73edb198103ef1b4e9423ec89befe1 907f41403ed159d65024f193f3e1f730a23b9bbb -- src/Cna.Core src/Cna.ExerciseRunner tests/Cna.ExerciseRunner.Tests scenarios/maneuvers/rules-lab.movement-cost.paired.v1.json` is empty: cited runtime/test/input source is byte-equivalent to the retained 019D1 checkpoint, not proof of runtime causality.

1. [Test](../../tests/Cna.ExerciseRunner.Tests/Commands/ManeuverRunCommandTests.cs), lines 319–376: two paired invocations, each capturing stdout/stderr in private `StringWriter`s. The exit assertion at 331 runs before inspecting error/output. On failure, paths/category are absent from xUnit's assertion message.
2. [CLI](../../src/Cna.ExerciseRunner/Commands/ManeuverRunCommand.cs), `ExecutePaired` (151–197): execute report, finalize/read back paired report, print completed report path/fingerprint, map report status. `WriteStatusAndMapExit` (236–259) maps aggregation status to 14 with generic trusted-evidence text. Unexpected execution and report-finalization exceptions have different exits (12 and 11). **Inference:** this observed 14 traversed completed report handling; report evidence existed before cleanup.
3. [Dependencies](../../src/Cna.ExerciseRunner/Execution/ManeuverExecutionContracts.cs), default (19–21): synchronous `ExerciseRunCoordinator.Execute`, then `ExerciseBundleReader.Read` and a copied child view. No child CLI subprocess or model service is involved in this path.
4. [Paired executor](../../src/Cna.ExerciseRunner/Execution/PairedManeuverExecutor.cs), `Execute` (22–202): baseline and candidate run sequentially in one loop. A failed aggregation appends a diagnostic entry, marks the remaining tail not-run and ends the loop. Status prioritizes aggregation failure over cancellation/exercise failure (629–638).

| Branch | Retained aggregation category | What would distinguish it |
| --- | --- | --- |
| Missing/blank `CompletedBundlePath` (71–83) | `CompletedBundleMissing` | Coordinator exit/failure message and partial artifacts |
| Nonfatal child-reader exception, including null reader result (85–105) | `BundleInvalid` | Observed path, reader exception; size/hash/semantic validation results |
| Ineligible child profile or manifest/build/run identity mismatch (122–137) | `BundleIdentityMismatch` | Child profile, manifest, build identity, seed ledger |
| Candidate pair-evidence mismatch (140–157) | `BundleIdentityMismatch` | Baseline/candidate initial snapshot, creation inputs, seed ledger, build cohort |
| Terminal completion/profile/boundary/check combination rejected (160–180, 332–401) | `BundleInvalid` | Child completion, profile, failed checks and terminal boundary |

These are all explicit `AppendAggregationFailure` sites within the bounded paired `Execute` source, not an exhaustive catalogue of underlying OS/domain causes. Reader exceptions lose exception details at this layer. The coordinator's `FailureMessage` is also not copied into the paired diagnostic report.

[Coordinator](../../src/Cna.ExerciseRunner/Execution/ExerciseRunCoordinator.cs), lines 75–102: build identity capture failure finalizes a `FailedAdmitted` bundle. Paired eligibility excludes that profile; if successfully written/read, it maps to identity mismatch. Unexpected execution yields `FailedIdentified`; finalization may yield missing completed path or a fallback `FailedPreAdmission` bundle (264–310). Multiple distinct underlying failures therefore converge on the same public status/category.

[Build identity capture](../../src/Cna.ExerciseRunner/Execution/BuildIdentityCapture.cs), lines 123–188: each child independently captures raw Git porcelain bytes (`git status --porcelain=v1 -z --untracked-files=all`), HEAD commit/tree and executed artifact hashes. Exploratory dirty worktrees are accepted. Paired `HasEqualBuildCohort` (238–269) requires equal dirty flags, porcelain hashes, commit/tree, runtime/architecture, ruleset/seed scheme and artifact identities. A change between children can cause rejection, but **no original unequal field has been observed**.

`SystemBuildIdentityEnvironment.RunGit` (221–256) drains both redirected streams asynchronously, waits synchronously for completion, and disposes the process. There is no explicit timeout or cancellation input. Startup/I/O exceptions or nonzero Git results become unavailable/head failure; detailed Git stderr is not retained by the capture result. A 6.810s failure duration supplies no evidence for a timeout threshold. Arbitrary timeout/retry changes are unjustified.

## Artifact and concurrency facts

[Test](../../tests/Cna.ExerciseRunner.Tests/Commands/ManeuverRunCommandTests.cs), fields (35–42): per-instance artifact root `Path.GetTempPath()/sandtable-maneuver-cli-<GUID>`; manifest root `.planning/maneuver-command-tests/<GUID>`. First and second invocations use distinct `movement-cost-first` / `movement-cost-second` roots. `Dispose` (667–676) recursively deletes both artifact and manifest directories even after a failed assertion. No original GUID is present in the failure log.

`CurrentFixture` (709–741) rematerializes the historical movement-cost paired envelope with the current Truck setup/content/scenario/rules hash and maximumSteps 30. Root seed 0, pair key `movement-cost-route`, repetition 0 and distinct baseline/candidate controllers remain. Using the frozen JSON unchanged is **not** the original test input. `.planning` is Git-ignored, so the test's own materialized manifest does not itself alter porcelain identity.

[Bundle writer](../../src/Cna.ExerciseRunner/Artifacts/ExerciseBundleWriter.cs), default `TryWrite` (135–144) uses a fresh GUID; write (203–256) flushes payloads and manifest, moves from `.partial/<GUID>` to final status and validates readback. [Paired report writer](../../src/Cna.ExerciseRunner/Artifacts/PairedReportWriter.cs), lines 5–75, likewise uses a GUID, flush/move/readback. These boundaries make a shared fixed artifact-path collision unsupported by the observed source; external I/O failure remains possible and unobserved.

[Runner xUnit configuration](../../tests/Cna.ExerciseRunner.Tests/xunit.runner.json) sets `parallelizeTestCollections: false`. Paired arms are sequential. Historical solution output does show Core, Runner and Contracts assemblies active concurrently. This is evidence of ordinary solution concurrency, not evidence of resource exhaustion or corruption. The filtered passing rerun also does not establish load-independence.

Current forensic inventory found zero matching `sandtable-maneuver-cli-*` roots directly under `/private/tmp` and `/private/var/folders/lg/j56llg991d1gpjvtjh8l9gbh0000gn/T`; original worktree no longer exists. No original report, child bundle, run identity or binary was recovered. This bounded inventory is not proof that no copy exists elsewhere.

## Hypotheses and options

| Candidate | Support | Missing discriminator / decision |
| --- | --- | --- |
| Git/artifact capture or OS I/O failure | Explicit capture/finalization branches can reach aggregation | Original child result/message and report category absent; unconfirmed |
| Build cohort changed between arms | Exact strict equality guard exists | No unequal identity field, concurrent writer or Git error observed; unconfirmed |
| Reader/semantic/terminal evidence rejection | Explicit `BundleInvalid` branches exist | Original rejected bytes/check/profile and exception absent; unconfirmed |
| Shared fixed temp path or internal parallel arm race | Source uses GUID roots and sequential arms | No evidence supporting this mechanism; do not implement a fix |
| Timeout/load exhaustion | Ordinary cross-assembly concurrency observed | No timeout threshold or resource/process telemetry; do not infer cause from elapsed time |

Recommendation: keep the strict aggregation guards. Defer production repair. At recurrence, expose/copy the finalized paired report and available child evidence before test cleanup and include captured stdout/stderr in the assertion diagnostic. If `BundleInvalid` is from reader failure, obtain the exact exception; if identity mismatch, compare retained cohort/manifest/ledger fields. Missing completed artifacts require coordinator finalization/capture diagnostics. Only then create a failing case for that diagnosed mechanism.

A test-only failure-evidence preservation change would improve diagnosis but would **not** repair this unconfirmed incident. It remains a separately assigned follow-up; this lane retains no code. Even a successful present-environment control can establish only successful materialization/execution now.

## Method and tooling

Exact isolated worktree `/Users/dsteele/.codex/worktrees/runner-aggregation-research/sandtable`, branch `codex/runner-aggregation-research`, base/head `907f41403ed159d65024f193f3e1f730a23b9bbb` before docs commit. Primary/other worktrees untouched.

Serena manual read; exact worktree activated and `HasEqualPairEvidence`, `BuildIdentityCapture.Capture`, `SystemBuildIdentityEnvironment.RunGit`, `ExerciseManifestCodecTests.Create` and pair-mismatch test retrieved successfully. Activation generated local `.serena` tooling, explicitly excluded from publication.

Codebase Memory Verify tier: unique project `sandtable-runner-aggregation-research`, fast generation `2026-10-05T03:24:32Z`, 16279 nodes/111258 edges. Symbol queries and both-direction depth-1 traces for paired executor/coordinator fully returned; `has_more=false`. Best-effort coverage reports metadata_match/no_recorded_issue for 15 cited paths (12 reporting/tooling paths plus three input/test paths) and no recorded gaps in Runner/test scopes. Direct source reads verified reporting guards, fixtures, writer and disposal. Graph missed delegate-bound CLI callers in the trace, so direct source is the call-chain authority. Six known parser gaps are elsewhere; clean coverage is not completeness proof. Prose/handoff sources are read directly because docs are excluded from the graph.

## Present-environment control

Coordinator explicitly granted the short lease after N1's final gate completed. Exactly **one fresh build and one public command invocation** ran; lease released immediately after completion. No fullsuite, stress/retry loop, or new xUnit test invocation ran. The temporary harness is outside the repository; its source is retained below for reproducibility, not as a product implementation.

```text
# cwd: /Users/dsteele/.codex/worktrees/runner-aggregation-research/sandtable
/opt/homebrew/bin/dotnet build /private/tmp/r2-runner-aggregation-probe/probe.csproj /bl:/private/tmp/r2-runner-aggregation-probe/build-{}.binlog > /private/tmp/r2-runner-aggregation-probe/build.log 2>&1
/opt/homebrew/bin/dotnet /private/tmp/r2-runner-aggregation-probe/bin/Debug/net10.0/probe.dll > /private/tmp/r2-runner-aggregation-probe/console.log 2>&1
```

Build: exit0, 0 warnings/errors, 6.31s; unique binlog `/private/tmp/r2-runner-aggregation-probe/build-20261005-035247--84040--7SrfA2.binlog` (build began around2026-10-05T03:52:47Z). Command: exit0/Succeeded, 4.443s. SDK10.0.400, runtime .NET10.0.11, macOS26.6.2 arm64. The control uses `CancellationToken.None`, while the test uses its current context token; the test lifecycle, two-run fingerprint equality assertion and original host concurrency are not reproduced.

The harness obtains the current ruleset hash from the fresh Core assembly and performs the same Truck rematerialization as `CurrentFixture`, then calls `ManeuverRunCommand.Execute` once with the six command arguments. The ignored repository manifest is `.planning/r2-runner-probe/movement-cost.json`; its exact bytes are copied to `/private/tmp/r2-runner-aggregation-probe/materialized-input.json`. It preserves all output/artifacts without calling test Dispose. Historical root seed0/pair key/repetition/controllers remain; inputs were not guessed from the old rules hash.

Observed report: 2 requested/attempted/validated/succeeded, 0 failed/aggregationFailed/notRun; both arms17 accepted steps,122 passed checks,0 failed checks. Comparison `compared`, first accepted-action divergence at13, step-count delta0, equal terminal/failure outcomes. This is a successful **present-environment diagnostic control**, not a historical reproduction, intermittent reliability estimate or root-cause repair.

Both child build identities retain head `907f41403ed159d65024f193f3e1f730a23b9bbb`, tree `536f20e11c4dcdafe071f96ab67c75e689479fd1`, dirty=true, identical porcelain hash `sha256:4c52bebed7f6b75712a56738aa62be364236f1e8acf4cbc0ed132ddf275c76dc`, runtime/arm64, rules hash `17f3e6047f34b5bf6f5f809055863b664a4bd82a8481db83ee83e0ae5cad3a2a`, seed scheme `sandtable.exercise-seeds.v1` and executed artifact hashes below. The dirty report/tooling files were stable during both arms; dirty exploratory status alone did not prevent this run from succeeding.

| Retained control object | SHA256 (without prefix) |
| --- | --- |
| Cna.Core.dll (2989568 bytes) | `e86e43206b4227c9e5ab1664c03d2f9cb49f10e2f7d57dedeb4678626f2838f5` |
| Cna.ExerciseRunner.dll (394752 bytes) | `5d93921be9c7a3ed7d3207fd3362541755b0323668829efe19a8d4f35f5be20d` |
| Cna.ExerciseRunner.deps.json (781 bytes) | `1b8ecfa3fbd783c701649a6d42c33ad9f886738d33def075a36334b9d9700380` |
| probe.dll | `a931c9e540fc0a3409d0ea90b1f59eb467ed638671b20350acc9c21a91c0cf14` |
| materialized-input.json | `cb00d2f10dbd8139ae21092d340df73a2b4f2aa27093577f9d2138bd7ea97926` |
| stdout.txt | `8ec67a9325601a76509768fbefc6de3735c97207b790d0722ed86908349e75b7` |
| stderr.txt (empty) | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` |
| paired-maneuver-report.json | `d0fdedbd53fc03a0dfe56640e607947d58b0920d74bcf84c028a795ee6d48e2c` |

All control paths below share `/private/tmp/r2-runner-aggregation-probe/`:

- Baseline: `artifacts/succeeded/831bd5e65f0344f6b1ba272f379070d3`.
- Candidate: `artifacts/succeeded/3e871d50c0754e27b587e835fa96a65f`.
- Paired report: `artifacts/maneuvers/succeeded/88b2e8f794844d5f97faed03617e8b34/paired-maneuver-report.json`; fingerprint `sha256:47a40683ad1aa5353485dbc9ff6dce969ffd9ec0736737d4de5cea64aeb2e05e`.
- `stdout.txt`, `stderr.txt`, `console.log`, `result.txt`, `build.log`, `precontrol-provenance.json`, `postcontrol-hashes.json`, empty `source-equivalence.diff`, complete input and temporary source/project.

These raw artifacts remain machine-local temporary evidence, not committed binaries. Durable source/result/identity/command evidence is retained here. Input source SHA256 `5c3c30e20919ba658bdb5ba17aed1f6ad276788979ebf94e12b31feafd81c055`; `ExerciseManifestCodecTests.cs` `fe26666dcbc5117267008dd9d40fe32d1e210e00a8b862b2ea2671ce2487aa60`; `ManeuverRunCommandTests.cs` `ba86193f9147c7dc0d12d149082656271ea9ed9f67fa8e3d608226a70b516c1d`.

### Exact temporary harness

`probe.csproj` SHA256 `9fe3c84e38f8f3f6dc9a76107ad420113ca302c78d2aa81c347275a7cff352ca`:

```xml
<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><ProjectReference Include="/Users/dsteele/.codex/worktrees/runner-aggregation-research/sandtable/src/Cna.ExerciseRunner/Cna.ExerciseRunner.csproj" /></ItemGroup></Project>
```

`Program.cs` SHA256 `1d2b695ee2517a06e0f8fe331388fe267479830d4a1c9aa47b6645c8ef4a9834`:

```csharp
using System.Diagnostics;
using System.Text.Json.Nodes;
using Cna.Core.Rules;
using Cna.ExerciseRunner.Commands;
var root = Directory.GetCurrentDirectory();
var node = JsonNode.Parse(File.ReadAllBytes(Path.Combine(root, "scenarios/maneuvers/rules-lab.movement-cost.paired.v1.json")))!;
void Replace(JsonNode value)
{
    if (value is JsonObject obj && obj.ContainsKey("setupId"))
    {
        obj["setupId"] = "rules-lab.breakdown.truck.v1";
        obj["setupHash"] = "sha256:e6631e81ad8f97e39fd9d7eec93bad7fe2b39db4d2d3059ed94a02dd4093e7a3";
        obj["contentPackId"] = "rules-lab.content.breakdown-truck.v1";
        obj["contentHash"] = "sha256:646e76e69ecceb82216b37d84e950928099acd8a3cb04b51526d0fe631e512ee";
        obj["scenarioId"] = "breakdown-truck-lab";
        obj["rulesetHash"] = Cna1979Ruleset.Manifest.Hash;
        if (obj["terminalBoundary"]!.GetValue<string>() == "land.position.operation-1.first-player.movement-and-combat.breakdown-determination") obj["maximumSteps"] = 30;
    }
    if (value is JsonObject parent) foreach (var child in parent.Select(pair => pair.Value).OfType<JsonNode>()) Replace(child);
    else if (value is JsonArray array) foreach (var child in array.OfType<JsonNode>()) Replace(child);
}
Replace(node);
var relative = ".planning/r2-runner-probe/movement-cost.json";
Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(root, relative))!);
File.WriteAllText(Path.Combine(root, relative), node.ToJsonString());
File.WriteAllText("/private/tmp/r2-runner-aggregation-probe/materialized-input.json", node.ToJsonString());
var output = new StringWriter(); var error = new StringWriter();
var started = Stopwatch.StartNew();
var code = ManeuverRunCommand.Execute(["maneuver", "run", "--manifest", relative, "--artifact-root", "/private/tmp/r2-runner-aggregation-probe/artifacts"], output, error, CancellationToken.None);
File.WriteAllText("/private/tmp/r2-runner-aggregation-probe/stdout.txt", output.ToString());
File.WriteAllText("/private/tmp/r2-runner-aggregation-probe/stderr.txt", error.ToString());
File.WriteAllText("/private/tmp/r2-runner-aggregation-probe/result.txt", $"exit={(int)code} status={code} elapsedMs={started.ElapsedMilliseconds} runtime={System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription} ruleset={Cna1979Ruleset.Manifest.Hash}\n");
Console.Write(output.ToString()); Console.Error.Write(error.ToString());
Console.WriteLine($"probeExit={(int)code} status={code} elapsedMs={started.ElapsedMilliseconds}");
return (int)code;
```


## Next gate and confidence

High confidence in the bounded source/reporting reconstruction; low confidence in any particular historical cause. Fresh high [independent review](../work/reviews/2026-10-04-runner-aggregation-review.md) returned **Ready**, set1/pass1,total1, with no actionable findings. Author **Accept**; coordinator accepts the scoped unknown/no-fix outcome. No self-dispatched reviewer or fix. Publish scoped research and durable handoff as a draft PR; coordinator owns final-head CI/merge. A future bounded diagnostic/repair dispatch must name its precise files and preserve this unresolved incident evidence.
