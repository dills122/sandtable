using System.Reflection;
using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Exercises;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Execution;

namespace Cna.ExerciseRunner.Tests.Execution;

// Task 022A0 (issue #448): test-only harness evidence for the Runner's Combat frontier. This is not
// Combat activation or certification and does not depend on Task 021. It records, from this
// assembly (which has no Core friend access), what the public Combat audience surface does and does
// not give a Runner today. See docs/work/plans/2026-11-20-task-022-runner-combat-adoption-scoping.md.
public sealed class CombatFrontierEvidenceTests
{
    private const string CombatEntry =
        "land.position.operation-1.first-player.movement-and-combat.combat.position-determination";

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

    [Theory]
    [InlineData(
        "rules-lab.breakdown.reaction.combat-assault.v1",
        "sha256:69db7f2c961f88f5469627a9e82f76c6a8b85330aa48a94ee3eaf0d22d18bbb3",
        "rules-lab.content.breakdown-reaction.combat-assault.v1",
        "sha256:d2d569a3f04f4915108e2220ac27ddfc25c97fcc047a4d4a67b496a6d62ca19c",
        "breakdown-reaction-combat-assault",
        new[] { "axis-infantry", "axis-truck" })]
    [InlineData(
        "rules-lab.breakdown.reaction.combat-double-fresh.v1",
        "sha256:d02a9996e2c87b4f5feda75bd0b6cf4a6756b0abbb83abb36ffe037e6fac8b5d",
        "rules-lab.content.breakdown-reaction.combat-double-fresh.v1",
        "sha256:44a76f21f1b492493c898495721f5f684acc83a14c391289c5e8d66220b6a648",
        "breakdown-reaction-combat-double-fresh",
        new[] { "axis-infantry", "axis-infantry-second", "axis-truck" })]
    public void RunnerStopsAtCombatEntryWhereOnlyPerElementSelectionDecisionsOpen(
        string setupId,
        string setupHash,
        string contentPackId,
        string contentHash,
        string scenarioId,
        string[] actingElements)
    {
        var (_, result, session) = RunToFrontier(
            setupId, setupHash, contentPackId, contentHash, scenarioId);

        // The Runner's only offer source is CampaignExercises.Query; it is empty for every audience.
        Assert.Equal(ExerciseFailureCategory.NoUniqueLegalAction, result.FailureCategory);
        Assert.Equal(CombatEntry, CampaignExercises.QueryCheckpoint(session).PositionId);
        Assert.All(Enum.GetValues<CampaignActionAudience>(), audience =>
            Assert.Empty(CampaignExercises.Query(session, audience).ActionSet!.Candidates));

        // The public audience surface has no "what is pending" query: a caller must name the stage
        // and acting element. Probing every stage x audience x World element finds one Selection
        // decision per acting-side element and nothing else.
        var decisions = ProbeAllDecisions(session, result.FinalSnapshot);
        Assert.All(decisions, decision =>
        {
            Assert.Equal(CampaignCombatAudienceDecisions.SelectionStage, decision.Stage);
            Assert.Equal(CampaignActionAudience.Axis, decision.Audience);
            Assert.Equal(CampaignCombatAudienceDecision.CurrentContractVersion, decision.ContractVersion);
            Assert.NotEmpty(decision.OfferRefs);
        });
        Assert.Equal(actingElements, decisions.Select(value => value.ActingElementId));
    }

    [Fact]
    public void PublicAudienceSubmissionIsAcceptedButGivesTheRunnerNoSuccessorOrEvidence()
    {
        var (_, result, session) = RunToFrontier(
            "rules-lab.breakdown.reaction.combat-assault.v1",
            "sha256:69db7f2c961f88f5469627a9e82f76c6a8b85330aa48a94ee3eaf0d22d18bbb3",
            "rules-lab.content.breakdown-reaction.combat-assault.v1",
            "sha256:d2d569a3f04f4915108e2220ac27ddfc25c97fcc047a4d4a67b496a6d62ca19c",
            "breakdown-reaction-combat-assault");
        var before = CampaignExercises.QueryCheckpoint(session);
        var decision = ProbeAllDecisions(session, result.FinalSnapshot)[0];
        var submission = new CampaignCombatAudienceSubmission(
            decision.ContractVersion,
            decision.CampaignId,
            decision.Audience,
            decision.Stage,
            decision.ActingElementId,
            decision.AudienceRevision,
            decision.VisibleDigest,
            decision.OfferRefs[^1]);

        var accepted = CampaignCombatAudienceDecisions.Submit(session, submission);
        Assert.True(accepted.IsAccepted);
        Assert.Equal(submission.OfferRef, accepted.AcceptedRef);

        // The outward result carries only the accepted ref: no successor session, receipt, event
        // bytes or snapshot checkpoint. The Runner's held session is immutable, so it cannot advance,
        // record one-event-per-step evidence, check continuity, or reconstruct.
        Assert.Equal(
            ["AcceptedRef", "IsAccepted"],
            typeof(CampaignCombatAudienceSubmissionResult)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(value => value.Name)
                .Order(StringComparer.Ordinal));
        Assert.Equal(
            """{"contractVersion":1,"outcome":"accepted","offerRef":"__REF__"}"""
                .Replace("__REF__", submission.OfferRef, StringComparison.Ordinal),
            Encoding.UTF8.GetString(accepted.SerializeOutwardCanonical()));
        var after = CampaignExercises.QueryCheckpoint(session);
        Assert.Equal(before.StateVersion, after.StateVersion);
        Assert.Equal(CombatEntry, after.PositionId);
        Assert.Equal(
            decision.SerializeCanonical(),
            CampaignCombatAudienceDecisions.Query(
                session, decision.Audience, decision.Stage, decision.ActingElementId)!
                .SerializeCanonical());
    }

    private static (ExerciseManifest Manifest, ExerciseExecutionResult Result, ExerciseSession Session)
        RunToFrontier(
            string setupId,
            string setupHash,
            string contentPackId,
            string contentHash,
            string scenarioId)
    {
        var manifest = ExerciseManifestCodec.Deserialize(Encoding.UTF8.GetBytes($$"""
            {"contractVersion":2,"exerciseId":"combat-frontier.evidence","setupId":"{{setupId}}","setupHash":"{{setupHash}}","contentPackId":"{{contentPackId}}","contentHash":"{{contentHash}}","scenarioId":"{{scenarioId}}","rulesetHash":"17f3e6047f34b5bf6f5f809055863b664a4bd82a8481db83ee83e0ae5cad3a2a","terminalBoundary":"land.position.unreachable","maximumSteps":200,"rootSeed":6,"buildMode":"exploratory","confidentiality":"trusted-authority","detail":"compact","controllers":{"system":"reaction-all-by-action-id","axis":"reaction-all-by-action-id","commonwealth":"reaction-all-by-action-id"},"assertFailureCategory":null}
            """));
        var result = ExerciseExecutor.Execute(manifest, TestContext.Current.CancellationToken);
        Assert.NotEmpty(result.Steps);

        // Rebuild the frontier session from the Runner transcript through public Core calls only.
        var start = CampaignExercises.Begin(ExerciseExecutor.CreateRequest(manifest));
        Assert.True(start.IsStarted);
        var session = start.Session!;
        foreach (var step in result.Steps)
        {
            var set = CampaignExercises.Query(session, step.Audience).ActionSet!;
            var submitted = CampaignExercises.Submit(session, new(
                CampaignActionSubmission.CurrentContractVersion,
                set.CampaignId,
                set.StateVersion,
                set.PositionId,
                set.Audience,
                step.ActionId));
            Assert.True(submitted.IsAccepted);
            session = submitted.SuccessorSession!;
        }
        Assert.Equal(result.FinalSnapshot, result.Steps[^1].SnapshotCheckpoint);
        return (manifest, result, session);
    }

    private static CampaignCombatAudienceDecision[] ProbeAllDecisions(
        ExerciseSession session,
        byte[] snapshot)
    {
        using var document = JsonDocument.Parse(snapshot);
        var subjects = document.RootElement.GetProperty("world").GetProperty("elements")
            .EnumerateArray()
            .Select(value => value.GetProperty("elementId").GetString()!)
            .Append(CampaignCombatAudienceDecisions.CycleControlSubject)
            .Append(CampaignCombatAudienceDecisions.RepeatMovementSubject)
            .Order(StringComparer.Ordinal)
            .ToArray();
        return Stages
            .SelectMany(stage => Enum.GetValues<CampaignActionAudience>()
                .SelectMany(audience => subjects.Select(subject =>
                    CampaignCombatAudienceDecisions.Query(session, audience, stage, subject))))
            .OfType<CampaignCombatAudienceDecision>()
            .ToArray();
    }
}
