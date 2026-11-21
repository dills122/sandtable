using System.Reflection;
using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Exercises;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Commands;
using Cna.ExerciseRunner.Execution;

namespace Cna.ExerciseRunner.Tests.Execution;

// Tasks 022A0/022A/022B (issue #448): the Runner drives first-cycle Combat (Selection, decline-only
// RBA, Force, Anti Armor and the result or cancelled assault) through the trusted Exercise path
// (CampaignExercises.QueryWithCombat/SubmitWithCombat).
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

    private const string CloseAssault = Prefix + "combat.close-assault";
    private static readonly string[] Finish = ["Axis:combat-selection-decided", "System:combat-no-attack-completed"];
    private static readonly string[] Cancelled =
    [
        "Axis:combat-selection-decided", "System:combat-barrage-no-effect", "Commonwealth:combat-rba-declined",
        "System:combat-force-cancelled", "System:combat-anti-armor-completed", "System:combat-cancelled-assault-completed",
    ];
    // T022-Q4 evidence: Anti Armor and the assault result share the close-assault position, so a
    // position-only terminal stops before the result. The frontier test below reaches the result.
    private static readonly string[] ToCloseAssault =
    [
        "Axis:combat-selection-decided", "System:combat-barrage-no-effect", "Commonwealth:combat-rba-declined",
        "Axis:combat-force-sealed", "Commonwealth:combat-force-sealed", "System:combat-force-completed",
        "System:combat-anti-armor-completed",
    ];

    // The existing policies pick the lowest action ID, and offer IDs depend on the Exercise-derived
    // campaign ID, so the Exercise ID pins which offer (assault or finish) each run takes.
    // combat-assault's assault is truck-bound (T020-Q8), so it takes the force-unavailable cancel.
    public static TheoryData<string, string, string, ulong, string, string[]> Paths => new()
    {
        { "assault", "first-by-action-id", "a", 6, ReserveRelease, Finish },
        { "assault", "reaction-all-by-action-id", "c", 6, ReserveRelease, Cancelled },
        { "double-fresh", "first-by-action-id", "a", 6, ReserveRelease, Finish },
        { "double-fresh", "reaction-all-by-action-id", "c", 5, CloseAssault, ToCloseAssault },
        { "double-fresh", "reaction-all-by-action-id", "c", 6, CloseAssault, ToCloseAssault },
    };

    [Theory]
    [MemberData(nameof(Paths))]
    public void RunnerDrivesFirstCycleCombatToItsCoveredFrontier(
        string setup, string policy, string variant, ulong seed, string terminal, string[] combatEvents)
    {
        var manifest = Manifest(setup == "assault" ? Assault : DoubleFresh, policy, variant, terminal, seed);
        var result = ExecuteWithCombat(manifest);

        Assert.True(result.IsSucceeded, Encoding.UTF8.GetString(ExerciseRunResultCodec.Serialize(result.RunResult)));
        Assert.Equal(terminal, result.BoundaryPositionId);
        Assert.True(result.Reconstruction!.IsVerified);
        Assert.True(ReadjudicationVerifier.Verify(manifest, result, includeCombat: true).IsVerified);
        // The ordinary re-adjudication path cannot replay a Combat step.
        Assert.False(ReadjudicationVerifier.Verify(manifest, result).IsVerified);
        Assert.All(result.Steps, step => Assert.Single(step.EventRecords));

        var entry = result.Steps.ToList().FindIndex(step => step.PriorPositionId == CombatEntry);
        Assert.True(entry > 0);
        var combat = result.Steps.Skip(entry).ToArray();
        Assert.Equal(combatEvents, combat.Select(step => $"{step.Audience}:{EventType(step)}"));
        Assert.Equal(Prefix + "combat.barrage", combat[0].Receipt.ResultingPositionId);
        Assert.Equal(terminal, combat[^1].Receipt.ResultingPositionId);
        // Every step before Selection is the unchanged pre-Combat path.
        Assert.DoesNotContain(result.Steps.Take(entry), step => step.PriorPositionId.Contains(".combat.", StringComparison.Ordinal));
    }

    [Fact]
    public void SimultaneousForceSealsAreTakenInFixedAudienceOrderOnly()
    {
        var policies = new ExerciseControllerManifest(
            ExerciseControllerPolicy.FirstByActionId,
            ExerciseControllerPolicy.FirstByActionId,
            ExerciseControllerPolicy.FirstByActionId);
        static Cna.ExerciseRunner.Controllers.ExerciseControllerActionSet Set(CampaignActionAudience audience, params string[] kinds) =>
            new(audience, kinds.Select((kind, index) => new Cna.ExerciseRunner.Controllers.ExerciseControllerCandidate(
                Cna.ExerciseRunner.Controllers.ExerciseControllerCandidate.CurrentContractVersion,
                $"sha256:{audience}-{index}", kind, null)));
        var seals = Cna.ExerciseRunner.Controllers.ExerciseController.Select(policies,
        [
            Set(CampaignActionAudience.System),
            Set(CampaignActionAudience.Axis, "seal-combat-force-assignment"),
            Set(CampaignActionAudience.Commonwealth, "seal-combat-force-assignment"),
        ]);
        Assert.Equal(CampaignActionAudience.Axis, seals.Audience);
        Assert.Equal("sha256:Axis-0", seals.ActionId);
        // Any other simultaneous offer still fails closed.
        Assert.Equal(Cna.ExerciseRunner.Controllers.ExerciseControllerSelectionFailure.MultipleActiveAudiences,
            Cna.ExerciseRunner.Controllers.ExerciseController.Select(policies,
            [
                Set(CampaignActionAudience.System),
                Set(CampaignActionAudience.Axis, "seal-combat-force-assignment"),
                Set(CampaignActionAudience.Commonwealth, "decline-retreat-before-assault"),
            ]).FailureReason);
    }

    [Fact]
    public void RepeatedRunsAreByteIdentical()
    {
        var manifest = Manifest(DoubleFresh, "reaction-all-by-action-id", "c", CloseAssault);
        var first = ExecuteWithCombat(manifest);
        var second = ExecuteWithCombat(manifest);
        Assert.Equal(first.Steps.Count, second.Steps.Count);
        Assert.Equal(first.Steps.SelectMany(step => step.EventRecords), second.Steps.SelectMany(step => step.EventRecords));
        Assert.Equal(first.Steps.Select(step => step.ActionId), second.Steps.Select(step => step.ActionId));
        Assert.Equal(first.FinalSnapshot, second.FinalSnapshot);
    }

    [Theory]
    [InlineData("assault", "reaction-all-by-action-id", "c", ReserveRelease, "combat-cancelled-assault-completed")]
    [InlineData("assault", "first-by-action-id", "a", ReserveRelease, "combat-no-attack-completed")]
    [InlineData("double-fresh", "reaction-all-by-action-id", "c", CloseAssault, "combat-assault-resolved")]
    public void UnsupportedNextCombatStepIsAnHonestNoLegalActionFrontier(string setup, string policy, string variant,
        string frontier, string lastEvent)
    {
        var result = ExecuteWithCombat(Manifest(setup == "assault" ? Assault : DoubleFresh, policy, variant,
            "land.position.unreachable"));

        Assert.Equal(ExerciseFailureCategory.NoUniqueLegalAction, result.FailureCategory);
        var failed = Assert.Single(result.FailedDecisions);
        Assert.Equal(frontier, failed.PositionId);
        Assert.Equal(lastEvent, EventType(result.Steps[^1]));
        Assert.True(result.Steps.Count > 0 && CampaignExercises.Reconstruct(Replay(
            Manifest(setup == "assault" ? Assault : DoubleFresh, policy, variant, "land.position.unreachable"),
            result)).IsVerified);
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

    private static ExerciseManifest Manifest(Setup setup, string policy, string variant, string terminal,
        ulong seed = 6) =>
        ExerciseManifestCodec.Deserialize(Encoding.UTF8.GetBytes($$"""
            {"contractVersion":2,"exerciseId":"combat-selection.{{variant}}","setupId":"{{setup.SetupId}}","setupHash":"{{setup.SetupHash}}","contentPackId":"{{setup.ContentPackId}}","contentHash":"{{setup.ContentHash}}","scenarioId":"{{setup.ScenarioId}}","rulesetHash":"17f3e6047f34b5bf6f5f809055863b664a4bd82a8481db83ee83e0ae5cad3a2a","terminalBoundary":"{{terminal}}","maximumSteps":200,"rootSeed":{{seed}},"buildMode":"exploratory","confidentiality":"trusted-authority","detail":"compact","controllers":{"system":"{{policy}}","axis":"{{policy}}","commonwealth":"{{policy}}"},"assertFailureCategory":null}
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
