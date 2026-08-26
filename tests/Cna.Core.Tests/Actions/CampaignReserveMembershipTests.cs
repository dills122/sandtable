using System.Reflection;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Tests.Campaigns;

namespace Cna.Core.Tests.Actions;

public sealed class CampaignReserveMembershipTests
{
    [Theory]
    [InlineData(0, (int)InitiativeOrderChoice.ActFirst)]
    [InlineData(0, (int)InitiativeOrderChoice.ActLast)]
    [InlineData(1, (int)InitiativeOrderChoice.ActFirst)]
    [InlineData(1, (int)InitiativeOrderChoice.ActLast)]
    public void OnlyResolvedFirstSideReceivesOwnDesignationCandidatesAndCompletion(
        int setupIndex,
        int choiceValue)
    {
        var handle = CampaignReserveActionTestData.ReachReserve(
            setupIndex,
            (InitiativeOrderChoice)choiceValue);
        var actingSide = FirstActingSideResolver.Resolve(handle.Snapshot);
        var actingAudience = CampaignReserveActionTestData.ToAudience(actingSide);
        var opponentAudience = actingAudience == CampaignActionAudience.Axis
            ? CampaignActionAudience.Commonwealth
            : CampaignActionAudience.Axis;

        var acting = CampaignReserveActionTestData.Query(handle, actingAudience);
        var designationCandidates = acting.Candidates
            .OfType<DesignateReserveAction>()
            .ToArray();
        var expectedSideId = actingSide == LandSide.Axis ? "axis" : "commonwealth";
        var expectedElements = handle.Context.Artifact.Definition.Elements
            .Where(element => element.SideId == expectedSideId
                && element.PlacementMode == ContentPlacementMode.Independent)
            .Select(element => element.ElementId)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(10, acting.StateVersion);
        Assert.Equal(handle.Snapshot.SequencePosition.PositionId, acting.PositionId);
        Assert.Equal(expectedElements, designationCandidates.Select(candidate =>
            candidate.ElementId).Order(StringComparer.Ordinal));
        Assert.All(designationCandidates, candidate => Assert.Equal(
            $"sha256:{Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(
                CampaignActionCandidate.WriteSubjectSemantics(
                    "designate-reserve",
                    candidate.ElementId)))}",
            candidate.ActionId));
        Assert.Single(acting.Candidates.OfType<CompleteReserveDesignationAction>());
        Assert.Equal(3, acting.Candidates.Count);
        Assert.Empty(CampaignReserveActionTestData.Query(handle, opponentAudience).Candidates);
        Assert.Empty(CampaignReserveActionTestData.Query(
            handle,
            CampaignActionAudience.System).Candidates);
    }

    [Fact]
    public void ReserveCandidatesMapToClosedCommandsBoundToCurrentAuthority()
    {
        var handle = CampaignReserveActionTestData.ReachReserve(
            0,
            InitiativeOrderChoice.ActLast);
        var actingSide = FirstActingSideResolver.Resolve(handle.Snapshot);
        var audience = CampaignReserveActionTestData.ToAudience(actingSide);
        var set = CampaignReserveActionTestData.Query(handle, audience);

        foreach (var candidate in set.Candidates.OfType<DesignateReserveAction>())
        {
            var command = Assert.IsType<DesignateReserveElement>(
                CampaignActionExecution.ToCommand(handle.Snapshot, audience, candidate));

            Assert.Equal(1, command.ContractVersion);
            Assert.Equal(handle.Snapshot.StateVersion, command.ExpectedStateVersion);
            Assert.Equal(handle.Snapshot.SequencePosition.PositionId, command.ExpectedPositionId);
            Assert.Equal(actingSide, command.ActingSide);
            Assert.Equal(candidate.ElementId, command.ElementId);
        }

        var completionCandidate = Assert.Single(
            set.Candidates.OfType<CompleteReserveDesignationAction>());
        var completion = Assert.IsType<CompleteReserveDesignation>(
            CampaignActionExecution.ToCommand(
                handle.Snapshot,
                audience,
                completionCandidate));
        Assert.Equal(1, completion.ContractVersion);
        Assert.Equal(handle.Snapshot.StateVersion, completion.ExpectedStateVersion);
        Assert.Equal(handle.Snapshot.SequencePosition.PositionId, completion.ExpectedPositionId);
        Assert.Equal(actingSide, completion.ActingSide);
    }

    [Fact]
    public void ReserveCommandsHaveFrozenInternalAuthorityBinding()
    {
        AssertCommandShape(
            typeof(DesignateReserveElement),
            ["ExpectedStateVersion", "ExpectedPositionId", "ActingSide", "ElementId"],
            [typeof(long), typeof(string), typeof(LandSide), typeof(string)],
            ["ExpectedPositionId", "ActingSide", "ElementId"]);
        AssertCommandShape(
            typeof(CompleteReserveDesignation),
            ["ExpectedStateVersion", "ExpectedPositionId", "ActingSide"],
            [typeof(long), typeof(string), typeof(LandSide)],
            ["ExpectedPositionId", "ActingSide"]);
    }

    private static void AssertCommandShape(
        Type commandType,
        string[] constructorNames,
        Type[] constructorTypes,
        string[] propertyNames)
    {
        Assert.True(commandType.IsNotPublic);
        Assert.True(commandType.IsSealed);
        Assert.True(commandType.IsAssignableTo(typeof(CampaignCommand)));
        var constructor = Assert.Single(commandType.GetConstructors(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            candidate => candidate.GetParameters().Length == constructorNames.Length);
        Assert.Equal(constructorNames, constructor.GetParameters().Select(parameter =>
            parameter.Name));
        Assert.Equal(constructorTypes, constructor.GetParameters().Select(parameter =>
            parameter.ParameterType));
        Assert.Equal(propertyNames, commandType.GetProperties(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(property => property.Name));
    }
}
