using System.Reflection;
using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Exercises;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Commands;
using Cna.ExerciseRunner.Execution;

namespace Cna.ExerciseRunner.Tests.Execution;

// Tasks 022A0/022A (issue #448): the Runner drives first-cycle Combat Selection and the System step
// after it through the trusted Exercise path (CampaignExercises.QueryWithCombat/SubmitWithCombat).
// Test-harness capability only: not Combat activation or certification, independent of Task 021.
// See docs/work/plans/2026-11-20-task-022-runner-combat-adoption-scoping.md.
public sealed class CombatSelectionRunnerTests : IDisposable
{
    private const string Prefix = "land.position.operation-1.first-player.movement-and-combat.";
    private const string CombatEntry = Prefix + "combat.position-determination";
    private const string RetreatBeforeAssault = Prefix + "combat.retreat-before-assault";
    private const string ReserveRelease = Prefix + "reserve-release";

    private static readonly Setup Assault = new(
        "rules-lab.breakdown.reaction.combat-assault.v1",
        "sha256:69db7f2c961f88f5469627a9e82f76c6a8b85330aa48a94ee3eaf0d22d18bbb3",
        "rules-lab.content.breakdown-reaction.combat-assault.v1",
        "sha256:d2d569a3f04f4915108e2220ac27ddfc25c97fcc047a4d4a67b496a6d62ca19c",
        "breakdown-reaction-combat-assault");

    private static readonly Setup DoubleFresh = new(
        "rules-lab.breakdown.reaction.combat-double-fresh.v1",
        "sha256:d02a9996e2c87b4f5feda75bd0b6cf4a6756b0abbb83abb36ffe037e6fac8b5d",
        "rules-lab.content.breakdown-reaction.combat-double-fresh.v1",
        "sha256:44a76f21f1b492493c898495721f5f684acc83a14c391289c5e8d66220b6a648",
        "breakdown-reaction-combat-double-fresh");

    private static readonly string[] Stages =
    [
        CampaignCombatAudienceDecisions.SelectionStage,
        CampaignCombatAudienceDecisions.ForceAssignmentStage,
        CampaignCombatAudienceDecisions.ReserveReleaseStage,
        CampaignCombatAudienceDecisions.CycleControlStage,
        CampaignCombatAudienceDecisions.CustodyStage,
        CampaignCombatAudienceDecisions.RbaDeclineStage,
        CampaignCombatAudienceDecisions.PostAssaultRetreatStage,
        CampaignCombatAudienceDecisions.RepeatCombatSelectionStage,
        CampaignCombatAudienceDecisions.RepeatMovementStage,
    ];

    private readonly string temp = Path.Combine(
        Path.GetTempPath(),
        $"sandtable-combat-runner-{Guid.NewGuid():N}");
    private readonly string repositoryManifestDirectory = Path.Combine(
        ".planning",
        "combat-runner-tests",
        Guid.NewGuid().ToString("N"));

    // The existing policies pick the lowest action ID, and offer IDs depend on the Exercise-derived
    // campaign ID, so the Exercise ID pins which offer (assault or finish) each run takes.
    public static TheoryData<string, string, string, string, string> Paths => new()
    {
        { "assault", "reaction-all-by-action-id", "c", RetreatBeforeAssault, "combat-barrage-no-effect" },
        { "assault", "first-by-action-id", "a", ReserveRelease, "combat-no-attack-completed" },
        { "double-fresh", "reaction-all-by-action-id", "a", RetreatBeforeAssault, "combat-barrage-no-effect" },
        { "double-fresh", "first-by-action-id", "a", ReserveRelease, "combat-no-attack-completed" },
    };

    [Theory]
    [MemberData(nameof(Paths))]
    public void RunnerAdvancesPastCombatEntryThroughSelectionAndItsSystemStep(
        string setup, string policy, string variant, string terminal, string systemEvent)
    {
        var manifest = Manifest(setup == "assault" ? Assault : DoubleFresh, policy, variant, terminal);
        var result = ExecuteWithCombat(manifest);

        Assert.True(result.IsSucceeded, Encoding.UTF8.GetString(ExerciseRunResultCodec.Serialize(result.RunResult)));
        Assert.Equal(terminal, result.BoundaryPositionId);
        Assert.True(result.Reconstruction!.IsVerified);
        Assert.True(ReadjudicationVerifier.Verify(manifest, result, includeCombat: true).IsVerified);
        // The ordinary re-adjudication path cannot replay a Combat step.
        Assert.False(ReadjudicationVerifier.Verify(manifest, result).IsVerified);
        Assert.All(result.Steps, step => Assert.Single(step.EventRecords));

        var selection = result.Steps[^2];
        var system = result.Steps[^1];
        Assert.Equal(CombatEntry, selection.PriorPositionId);
        Assert.Equal(CampaignActionAudience.Axis, selection.Audience);
        Assert.Equal(Prefix + "combat.barrage", selection.Receipt.ResultingPositionId);
        Assert.Equal("combat-selection-decided", EventType(selection));
        Assert.Equal(CampaignActionAudience.System, system.Audience);
        Assert.Equal(terminal, system.Receipt.ResultingPositionId);
        Assert.Equal(systemEvent, EventType(system));
        // Every step before Selection is the unchanged pre-Combat path.
        Assert.DoesNotContain(result.Steps.SkipLast(2), step => step.PriorPositionId.Contains(".combat.", StringComparison.Ordinal));
    }

    [Fact]
    public void RepeatedRunsAreByteIdentical()
    {
        var manifest = Manifest(Assault, "reaction-all-by-action-id", "c", RetreatBeforeAssault);
        var first = ExecuteWithCombat(manifest);
        var second = ExecuteWithCombat(manifest);
        Assert.Equal(first.Steps.Count, second.Steps.Count);
        Assert.Equal(first.Steps.SelectMany(step => step.EventRecords), second.Steps.SelectMany(step => step.EventRecords));
        Assert.Equal(first.Steps.Select(step => step.ActionId), second.Steps.Select(step => step.ActionId));
        Assert.Equal(first.FinalSnapshot, second.FinalSnapshot);
    }

    [Theory]
    [InlineData("reaction-all-by-action-id", "c", RetreatBeforeAssault)]
    [InlineData("first-by-action-id", "a", ReserveRelease)]
    public void UnsupportedNextCombatStepIsAnHonestNoLegalActionFrontier(string policy, string variant, string frontier)
    {
        var result = ExecuteWithCombat(Manifest(Assault, policy, variant, "land.position.unreachable"));

        Assert.Equal(ExerciseFailureCategory.NoUniqueLegalAction, result.FailureCategory);
        var failed = Assert.Single(result.FailedDecisions);
        Assert.Equal(frontier, failed.PositionId);
        Assert.All(failed.Queries, query =>
        {
            Assert.Equal(0, query.CandidateCount);
            Assert.Equal(ExerciseCheckFailureCode.None, query.FailureCode);
        });
    }

    [Fact]
    public void TheCliAndDefaultExecutorKeepTheOrdinaryCombatEntryFrontier()
    {
        // Staged opt-in (T022-Q8/Q9): a Combat step's snapshot checkpoint is a Combat state
        // document, and the bundle's final-snapshot reader admits only campaign snapshot V11. Until
        // that readback and a manifest opt-in are ruled, the CLI and the public executor keep the
        // pre-022A behavior and still write a strictly readable failed bundle at Combat entry.
        var manifest = Manifest(Assault, "reaction-all-by-action-id", "c", "land.position.unreachable");
        var combat = ExecuteWithCombat(Manifest(Assault, "reaction-all-by-action-id", "c", RetreatBeforeAssault));
        Assert.True(combat.IsSucceeded);
        Assert.Throws<JsonException>(() => ExerciseEvidenceCodec.DeserializeSnapshot(combat.FinalSnapshot));

        var ordinary = ExerciseExecutor.Execute(manifest, TestContext.Current.CancellationToken);
        Assert.Equal(ExerciseFailureCategory.NoUniqueLegalAction, ordinary.FailureCategory);
        Assert.Equal(CombatEntry, Assert.Single(ordinary.FailedDecisions).PositionId);

        var relative = Path.Combine(repositoryManifestDirectory, "combat-assault.json");
        var path = Path.Combine(FindRepositoryRoot(AppContext.BaseDirectory), relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, ExerciseManifestCodec.Serialize(manifest));
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = ExerciseRunCommand.Execute(
            ["exercise", "run", "--manifest", relative, "--artifact-root", Path.Combine(temp, "artifacts")],
            output,
            error,
            TestContext.Current.CancellationToken);

        Assert.Equal(ExerciseProcessExitCode.NoUniqueLegalAction, exitCode);
        var bundlePath = output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Single(value => value.StartsWith("bundle=", StringComparison.Ordinal))["bundle=".Length..];
        var bundle = ExerciseBundleReader.Read(bundlePath);
        Assert.Equal(ArtifactBundleProfile.FailedExecuted, bundle.Manifest.Profile);
    }

    [Fact]
    public void PublicAudienceSubmissionIsAcceptedButGivesTheRunnerNoSuccessorOrEvidence()
    {
        // 022A0 evidence for why the trusted Exercise path exists: the frozen audience surface
        // (CampaignCombatAudienceDecisions) is unchanged and still hands a public caller only a ref.
        var manifest = Manifest(Assault, "reaction-all-by-action-id", "entry", CombatEntry);
        var result = ExerciseExecutor.Execute(manifest, TestContext.Current.CancellationToken);
        Assert.Equal(CombatEntry, result.BoundaryPositionId);
        var session = Replay(manifest, result);
        var before = CampaignExercises.QueryCheckpoint(session);
        var decisions = Stages
            .SelectMany(stage => Enum.GetValues<CampaignActionAudience>()
                .SelectMany(audience => Subjects(result.FinalSnapshot).Select(subject =>
                    CampaignCombatAudienceDecisions.Query(session, audience, stage, subject))))
            .OfType<CampaignCombatAudienceDecision>()
            .ToArray();
        Assert.Equal(["axis-infantry", "axis-truck"], decisions.Select(value => value.ActingElementId));
        Assert.All(decisions, value => Assert.Equal(CampaignCombatAudienceDecisions.SelectionStage, value.Stage));

        var decision = decisions[0];
        var accepted = CampaignCombatAudienceDecisions.Submit(session, new CampaignCombatAudienceSubmission(
            decision.ContractVersion, decision.CampaignId, decision.Audience, decision.Stage,
            decision.ActingElementId, decision.AudienceRevision, decision.VisibleDigest, decision.OfferRefs[^1]));
        Assert.True(accepted.IsAccepted);
        Assert.Equal(
            ["AcceptedRef", "IsAccepted"],
            typeof(CampaignCombatAudienceSubmissionResult)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(value => value.Name)
                .Order(StringComparer.Ordinal));
        Assert.Equal(before.StateVersion, CampaignExercises.QueryCheckpoint(session).StateVersion);

        // The same cut, through the trusted Exercise path, offers the union of those decisions.
        var offered = CampaignExercises.QueryWithCombat(session, CampaignActionAudience.Axis).ActionSet!;
        Assert.Equal(decisions.Sum(value => value.OfferRefs.Count), offered.Candidates.Count);
        Assert.Equal(CampaignLegalActionSet.CombatExercisePolicyId, offered.PolicyId);
        Assert.Empty(CampaignExercises.Query(session, CampaignActionAudience.Axis).ActionSet!.Candidates);
    }

    public void Dispose()
    {
        if (Directory.Exists(temp)) Directory.Delete(temp, recursive: true);
        var repositoryDirectory = Path.Combine(
            FindRepositoryRoot(AppContext.BaseDirectory),
            repositoryManifestDirectory);
        if (Directory.Exists(repositoryDirectory)) Directory.Delete(repositoryDirectory, recursive: true);
        GC.SuppressFinalize(this);
    }

    private static ExerciseManifest Manifest(Setup setup, string policy, string variant, string terminal) =>
        ExerciseManifestCodec.Deserialize(Encoding.UTF8.GetBytes($$"""
            {"contractVersion":2,"exerciseId":"combat-selection.{{variant}}","setupId":"{{setup.SetupId}}","setupHash":"{{setup.SetupHash}}","contentPackId":"{{setup.ContentPackId}}","contentHash":"{{setup.ContentHash}}","scenarioId":"{{setup.ScenarioId}}","rulesetHash":"17f3e6047f34b5bf6f5f809055863b664a4bd82a8481db83ee83e0ae5cad3a2a","terminalBoundary":"{{terminal}}","maximumSteps":200,"rootSeed":6,"buildMode":"exploratory","confidentiality":"trusted-authority","detail":"compact","controllers":{"system":"{{policy}}","axis":"{{policy}}","commonwealth":"{{policy}}"},"assertFailureCategory":null}
            """));

    private static ExerciseExecutionResult ExecuteWithCombat(ExerciseManifest manifest) =>
        ExerciseExecutor.Execute(manifest, CoreExerciseExecutionRuntime.WithCombat, TestContext.Current.CancellationToken);

    private static ExerciseSession Replay(ExerciseManifest manifest, ExerciseExecutionResult result)
    {
        var start = CampaignExercises.Begin(ExerciseExecutor.CreateRequest(manifest));
        Assert.True(start.IsStarted);
        var session = start.Session!;
        foreach (var step in result.Steps)
        {
            var set = CampaignExercises.QueryWithCombat(session, step.Audience).ActionSet!;
            var submitted = CampaignExercises.SubmitWithCombat(session, new(
                CampaignActionSubmission.CurrentContractVersion, set.CampaignId, set.StateVersion,
                set.PositionId, set.Audience, step.ActionId));
            Assert.True(submitted.IsAccepted);
            session = submitted.SuccessorSession!;
        }
        return session;
    }

    private static string[] Subjects(byte[] snapshot)
    {
        using var document = JsonDocument.Parse(snapshot);
        return document.RootElement.GetProperty("world").GetProperty("elements").EnumerateArray()
            .Select(value => value.GetProperty("elementId").GetString()!)
            .Append(CampaignCombatAudienceDecisions.CycleControlSubject)
            .Append(CampaignCombatAudienceDecisions.RepeatMovementSubject)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static string EventType(ExerciseAcceptedStep step)
    {
        using var document = JsonDocument.Parse(step.EventRecords[0]);
        return document.RootElement.GetProperty("eventType").GetString()!;
    }

    private static string FindRepositoryRoot(string start)
    {
        for (var current = new DirectoryInfo(start); current is not null; current = current.Parent)
        {
            if (File.Exists(Path.Combine(current.FullName, "Sandtable.slnx")))
                return current.FullName;
        }
        throw new InvalidOperationException("The repository root was not found.");
    }

    private sealed record Setup(string SetupId, string SetupHash, string ContentPackId, string ContentHash, string ScenarioId);
}
