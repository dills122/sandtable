using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Rules;
using Cna.Core.Tests.Campaigns;
using Cna.Core.Tests.Content;
using Cna.Core.Visibility;

namespace Cna.Core.Tests.Observations;

public sealed class CampaignContactJournalTests
{
    [Fact]
    public void CommittedReactionSourceSurvivesColdReplayWithoutExposingPrivateBinding()
    {
        var prior = BreakdownMovementTests.ChangeElement(BreakdownSnapshotTests.Create(true),
            "commonwealth-infantry", "north-east");
        var moved = BreakdownMovementTests.Move(prior, "axis-infantry", "north-west", "north");
        var empty = CampaignContactJournal.Empty(prior.CampaignId);
        var committed = empty.Append(moved);
        var retained = committed.Records;
        var replayed = CampaignContactJournal.Replay(prior.CampaignId, [moved], retained);

        Assert.Empty(empty.Records);
        Assert.Single(retained);
        Assert.Equal(committed.ObserveCanonical(LandSide.Commonwealth, "commonwealth-infantry"),
            replayed.ObserveCanonical(LandSide.Commonwealth, "commonwealth-infantry"));
        Assert.Equal([new CampaignContactTargetCandidate("commonwealth-infantry", "c-00000001", "north")],
            replayed.CurrentTargetsFor(LandSide.Commonwealth, "commonwealth-infantry"));
        Assert.Empty(replayed.CurrentTargetsFor(LandSide.Axis, "axis-infantry"));
        Assert.DoesNotContain("axis-infantry",
            Encoding.UTF8.GetString(replayed.ObserveCanonical(LandSide.Commonwealth, "commonwealth-infantry")),
            StringComparison.Ordinal);
    }

    [Fact]
    public void ColdReplayRejectsMissingOrAlteredContactRecord()
    {
        var prior = BreakdownMovementTests.ChangeElement(BreakdownSnapshotTests.Create(true),
            "commonwealth-infantry", "north-east");
        var moved = BreakdownMovementTests.Move(prior, "axis-infantry", "north-west", "north");
        var committed = CampaignContactJournal.Empty(prior.CampaignId).Append(moved);
        var record = Assert.Single(committed.Records);
        record[^2] ^= 1;

        Assert.Throws<JsonException>(() => CampaignContactJournal.Replay(prior.CampaignId,
            [moved], [record]));
        Assert.Throws<JsonException>(() => CampaignContactJournal.Replay(prior.CampaignId,
            [moved], []));
        Assert.Throws<JsonException>(() => CampaignContactJournal.Replay(prior.CampaignId,
            [], committed.Records));
        Assert.Throws<ArgumentException>(() => CampaignContactJournal.Empty("wrong-campaign")
            .Append(moved));
    }

    [Fact]
    public void MovementWithoutSightProducesNoContactRecord()
    {
        var prior = BreakdownSnapshotTests.Create(true);
        var moved = BreakdownMovementTests.Move(prior, "axis-truck", "west", "center");
        var journal = CampaignContactJournal.Empty(prior.CampaignId).Append(moved);

        Assert.Empty(journal.Records);
        Assert.Empty(journal.CurrentTargetsFor(LandSide.Commonwealth, "commonwealth-infantry"));
        Assert.Empty(CampaignContactJournal.Replay(prior.CampaignId, [moved], []).Records);
    }

    [Fact]
    public void ExerciseSessionRebuildsContactKnowledgeFromRetainedAuthorityHistory()
    {
        var prior = BreakdownMovementTests.ChangeElement(BreakdownSnapshotTests.Create(true),
            "commonwealth-infantry", "north-east");
        var moved = BreakdownMovementTests.Move(prior, "axis-infantry", "north-west", "north");
        var artifact = BreakdownContentFixture.Artifact();
        var context = CampaignContentContext.Create(artifact,
            artifact.Definition.LegacyDefinition.Scenarios.Single().ScenarioId);
        var restored = new ExerciseSession(prior, context, [moved]);

        Assert.Single(restored.CurrentContacts.Records);
        Assert.Equal([new CampaignContactTargetCandidate("commonwealth-infantry", "c-00000001", "north")],
            restored.CurrentContacts.CurrentTargetsFor(LandSide.Commonwealth, "commonwealth-infantry"));
    }
}
