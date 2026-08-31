using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignSnapshotV10ContractTests
{
    [Fact]
    public void CreationAndSnapshotRoundTripCanonicalV5Truth()
    {
        var fixture = CampaignV10TestData.Create();

        var creationBytes = CampaignSuccessorEventSerializer.Serialize(fixture.Created);
        var roundTrippedCreation = Assert.IsType<CampaignCreatedV9>(
            CampaignSuccessorEventSerializer.Deserialize(creationBytes));
        var projected = CampaignV10Projector.ApplyCreation(
            roundTrippedCreation,
            fixture.Artifact,
            fixture.Scenario);
        var snapshotBytes = CampaignSnapshotV10Serializer.Serialize(projected);
        var roundTrippedSnapshot = CampaignSnapshotV10Serializer.Deserialize(snapshotBytes);

        Assert.Equal(9, roundTrippedCreation.ContractVersion);
        Assert.Equal(10, projected.ContractVersion);
        Assert.Equal(5, projected.World.ContractVersion);
        Assert.Equal(fixture.Artifact.Identity, projected.Setup.Content.Pack);
        Assert.All(projected.World.Elements, element =>
            Assert.Equal(5, Assert.Single(element.Components).CurrentToe));
        Assert.Equal(snapshotBytes, CampaignSnapshotV10Serializer.Serialize(roundTrippedSnapshot));
    }

    [Fact]
    public void PresentEmptyWindowIsCanonicalAndDistinctFromNoWindow()
    {
        var fixture = CampaignV10TestData.Create();
        var emptyMove = CampaignV10TestData.CreateTriggeringMove(
            fixture.MovementSnapshot,
            []);
        var projected = CampaignV10Projector.ApplyMovement(
            fixture.MovementSnapshot,
            emptyMove,
            fixture.Artifact,
            fixture.Scenario,
            (_, _) => emptyMove);
        var noWindowMove = CampaignV10TestData.CreateNonTriggeringMove(
            fixture.MovementSnapshot);
        var withoutWindow = CampaignV10Projector.ApplyMovement(
            fixture.MovementSnapshot,
            noWindowMove,
            fixture.Artifact,
            fixture.Scenario,
            (_, _) => noWindowMove);
        var emptyBytes = CampaignSnapshotV10Serializer.Serialize(projected);
        var absentBytes = CampaignSnapshotV10Serializer.Serialize(withoutWindow);

        Assert.NotNull(projected.ReactionWindow);
        Assert.Empty(projected.ReactionWindow.FrozenOpportunities);
        Assert.Equal(CampaignPositionV10Kind.Reaction, projected.CurrentPosition.Kind);
        Assert.Null(withoutWindow.ReactionWindow);
        Assert.Equal(CampaignPositionV10Kind.Sequence, withoutWindow.CurrentPosition.Kind);
        Assert.NotEqual(emptyBytes, absentBytes);
        Assert.Contains("\"frozenOpportunities\":[]", Encoding.UTF8.GetString(emptyBytes));
        Assert.Equal(
            emptyBytes,
            CampaignSnapshotV10Serializer.Serialize(
                CampaignSnapshotV10Serializer.Deserialize(emptyBytes)));
    }

    [Fact]
    public void SnapshotReaderRejectsExtraReorderedAndMixedLegacyShapes()
    {
        var fixture = CampaignV10TestData.Create();
        var canonical = Encoding.UTF8.GetString(
            CampaignSnapshotV10Serializer.Serialize(fixture.MovementSnapshot));
        var extra = canonical.Replace(
            "{\"contractVersion\":10,",
            "{\"contractVersion\":10,\"unexpected\":true,",
            StringComparison.Ordinal);
        var reordered = canonical.Replace(
            "{\"contractVersion\":10,\"campaignId\":",
            "{\"campaignId\":\"campaign-v10\",\"contractVersion\":10,\"discarded\":",
            StringComparison.Ordinal);
        var mixed = canonical.Replace(
            "\"schemaVersion\":5,\"formatId\":\"sandtable.content-json.v4\"",
            "\"schemaVersion\":4,\"formatId\":\"sandtable.content-json.v3\"",
            StringComparison.Ordinal);
        var missing = canonical.Replace(
            ",\"reactionWindow\":null",
            string.Empty,
            StringComparison.Ordinal);
        var duplicate = canonical.Replace(
            "{\"contractVersion\":10,",
            "{\"contractVersion\":10,\"contractVersion\":10,",
            StringComparison.Ordinal);

        Assert.Throws<JsonException>(() => CampaignSnapshotV10Serializer.Deserialize(
            Encoding.UTF8.GetBytes(extra)));
        Assert.Throws<JsonException>(() => CampaignSnapshotV10Serializer.Deserialize(
            Encoding.UTF8.GetBytes(reordered)));
        Assert.Throws<JsonException>(() => CampaignSnapshotV10Serializer.Deserialize(
            Encoding.UTF8.GetBytes(mixed)));
        Assert.Throws<JsonException>(() => CampaignSnapshotV10Serializer.Deserialize(
            Encoding.UTF8.GetBytes(missing)));
        Assert.Throws<JsonException>(() => CampaignSnapshotV10Serializer.Deserialize(
            Encoding.UTF8.GetBytes(duplicate)));
        Assert.Throws<JsonException>(() => CampaignSnapshotSerializer.Deserialize(
            Encoding.UTF8.GetBytes(canonical)));
    }

    [Fact]
    public void WindowRejectsRemoteEvidenceAndInvalidParticipantState()
    {
        var fixture = CampaignV10TestData.Create();
        var valid = fixture.TriggeringMove.OpenedReactionWindow!;
        var opportunity = Assert.Single(valid.FrozenOpportunities);
        var unknownId = new CampaignReactionOpportunityId(
            "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

        Assert.Throws<ArgumentException>(() => new CampaignFrozenReactionOpportunity(
            opportunity.OpportunityId,
            opportunity.ReactingRepresentation,
            new CampaignReactionAdjacencyEvidence(
                "remote",
                valid.TriggerAuthority.DestinationLocationId,
                true,
                [new RuleReference("spi-1979-land-rules", "8.3.remote")])));
        Assert.Throws<ArgumentException>(() => new CampaignReactionWindow(
            valid.WindowId,
            valid.TriggerCommittedStateVersion,
            valid.PhasingSide,
            valid.ReactingSide,
            valid.ReactingPosition,
            valid.TriggerAuthority,
            valid.ApparentTrigger,
            valid.FrozenOpportunities,
            [unknownId],
            null));
    }

    [Fact]
    public void WindowCanonicallyOrdersFrozenAndResolvedIdentitySets()
    {
        var fixture = CampaignV10TestData.Create();
        var valid = fixture.TriggeringMove.OpenedReactionWindow!;
        var first = Assert.Single(valid.FrozenOpportunities);
        var secondRepresentation = new CampaignMapRepresentationState(
            "map-representation.9998",
            first.ReactingRepresentation.CurrentLocationId,
            first.ReactingRepresentation.BindingKind,
            first.ReactingRepresentation.BoundElementIds);
        var second = new CampaignFrozenReactionOpportunity(
            CampaignReactionIdentity.CreateOpportunity(valid.WindowId, secondRepresentation),
            secondRepresentation,
            first.AdjacencyEvidence);
        var reordered = new CampaignReactionWindow(
            valid.WindowId,
            valid.TriggerCommittedStateVersion,
            valid.PhasingSide,
            valid.ReactingSide,
            valid.ReactingPosition,
            valid.TriggerAuthority,
            valid.ApparentTrigger,
            [second, first],
            [second.OpportunityId, first.OpportunityId],
            null);

        Assert.Equal(
            reordered.FrozenOpportunities.Select(value => value.OpportunityId.Value)
                .Order(StringComparer.Ordinal),
            reordered.FrozenOpportunities.Select(value => value.OpportunityId.Value));
        Assert.Equal(
            reordered.ResolvedOpportunityIds.Select(value => value.Value)
                .Order(StringComparer.Ordinal),
            reordered.ResolvedOpportunityIds.Select(value => value.Value));
        Assert.Throws<ArgumentException>(() => new CampaignReactionWindow(
            valid.WindowId,
            valid.TriggerCommittedStateVersion,
            valid.PhasingSide,
            valid.ReactingSide,
            valid.ReactingPosition,
            valid.TriggerAuthority,
            valid.ApparentTrigger,
            [first, first],
            [],
            null));
    }

    [Fact]
    public void CreationSeedTamperingWithUnchangedContentIdentityRejectsAtProjection()
    {
        var fixture = CampaignV10TestData.Create();
        var canonical = Encoding.UTF8.GetString(
            CampaignSuccessorEventSerializer.Serialize(fixture.Created));
        var tampered = canonical.Replace(
            "\"currentToe\":5",
            "\"currentToe\":4",
            StringComparison.Ordinal);
        var readback = Assert.IsType<CampaignCreatedV9>(
            CampaignSuccessorEventSerializer.Deserialize(Encoding.UTF8.GetBytes(tampered)));

        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignV10Projector.ApplyCreation(
                readback,
                fixture.Artifact,
                fixture.Scenario));
    }

    [Fact]
    public void CreationProjectionRejectsNoninitialRandomCursorAndScenarioIncoherentSetup()
    {
        var fixture = CampaignV10TestData.Create();
        var created = fixture.Created;
        var noninitialRandom = new CampaignCreatedV9(
            created.CampaignId,
            created.StateVersion,
            created.RulesetHash,
            created.Setup,
            created.InitialWorld,
            new RandomStreamState(
                created.RandomState.ContractVersion,
                created.RandomState.AlgorithmId,
                created.RandomState.Seed,
                1),
            created.SequencePosition);
        var mismatchedSetup = CampaignSetupSnapshotV5.FromCanonical(
            created.Setup.SchemaVersion,
            created.Setup.SetupId,
            CampaignSetupHash.CalculateV5(
                created.Setup.SchemaVersion,
                created.Setup.SetupId,
                created.Setup.IsSynthetic,
                checked(created.Setup.InitialGameTurn + 1),
                created.Setup.InitialInitiative,
                created.Setup.OpeningPreamble,
                created.Setup.Weather,
                created.Setup.StageEntry,
                created.Setup.Content.Pack,
                created.Setup.Content.ScenarioId,
                created.Setup.Sources),
            created.Setup.IsSynthetic,
            checked(created.Setup.InitialGameTurn + 1),
            created.Setup.InitialInitiative,
            created.Setup.OpeningPreamble,
            created.Setup.Weather,
            created.Setup.StageEntry,
            created.Setup.Content,
            created.Setup.Sources);
        var mismatchedCreation = new CampaignCreatedV9(
            created.CampaignId,
            created.StateVersion,
            created.RulesetHash,
            mismatchedSetup,
            created.InitialWorld,
            created.RandomState,
            created.SequencePosition);

        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignV10Projector.ApplyCreation(
                noninitialRandom,
                fixture.Artifact,
                fixture.Scenario));
        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignV10Projector.ApplyCreation(
                mismatchedCreation,
                fixture.Artifact,
                fixture.Scenario));
    }

    [Fact]
    public void ActiveContractIdentitiesRemainUnchanged()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        var creation = CampaignTestHarness.Decide(
            null,
            CampaignTestHarness.Create(
                "campaign-active-identities",
                Cna1979Ruleset.Manifest.Hash,
                12345,
                setup.SetupId,
                setup.Hash));
        var movement = CampaignMovementTestData.ReachMovement();
        var candidate = CampaignMovementTestData.FindMove(
            movement.Snapshot,
            movement.Context,
            movement.ActingSide,
            "commonwealth-element-a",
            "north-east");
        var moved = CampaignEngine.Decide(
            movement.Snapshot,
            CampaignMovementTestData.CommandFor(
                movement.Snapshot,
                movement.ActingSide,
                candidate),
            movement.Context);

        Assert.Equal(9, CampaignSnapshot.CurrentContractVersion);
        Assert.Equal(4, CampaignWorldSnapshot.CurrentContractVersion);
        Assert.Equal(8,
            Assert.IsType<CampaignCreated>(Assert.Single(creation.Events)).ContractVersion);
        Assert.Equal(1,
            Assert.IsType<ElementMoved>(Assert.Single(moved.Events)).ContractVersion);
        Assert.Equal(2, Cna1979LandSequence.ContractVersion);
        Assert.Equal(2, Cna1979LandSequence.CatalogSchemaVersion);
        Assert.DoesNotContain("ReactingSide", Enum.GetNames<LandActorRole>());
    }
}
