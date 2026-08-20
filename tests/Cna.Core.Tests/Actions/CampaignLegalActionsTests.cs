using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Observations;
using Cna.Core.Rules;
using Cna.Core.Setups;
using Cna.Core.Tests.Observations;

namespace Cna.Core.Tests.Actions;

public sealed class CampaignLegalActionsTests
{
    [Fact]
    public void PublicFlowAdvancesEveryMandatoryCheckpointAndStopsAtWeather()
    {
        var handle = CreateHandle();

        Assert.Empty(Query(handle, CampaignActionAudience.Axis).Candidates);
        Assert.Empty(Query(handle, CampaignActionAudience.Commonwealth).Candidates);
        Assert.True(CampaignObservations.Query(handle, LandSide.Axis).IsProjected);
        handle = SubmitOnly(handle, CampaignActionAudience.System, "resolve-initiative");
        handle = SubmitOnly(handle, CampaignActionAudience.System,
            "resolve-no-obligation-naval-convoy-schedule");
        handle = SubmitOnly(handle, CampaignActionAudience.System,
            "resolve-no-obligation-tactical-shipping");

        var axis = Query(handle, CampaignActionAudience.Axis);
        var commonwealth = Query(handle, CampaignActionAudience.Commonwealth);
        Assert.Equal(["act-first", "act-last"], axis.Candidates.Select(value => value.Kind));
        Assert.Empty(commonwealth.Candidates);
        Assert.Empty(Query(handle, CampaignActionAudience.System).Candidates);

        var actLast = axis.Candidates.Single(value => value.Kind == "act-last");
        var accepted = CampaignLegalActions.Submit(handle, Bind(axis, actLast));

        Assert.True(accepted.IsAccepted);
        Assert.Equal(4, accepted.Receipt!.PriorStateVersion);
        Assert.Equal(5, accepted.Receipt.CommittedStateVersion);
        Assert.Equal("land.position.operation-1.weather-determination",
            accepted.Receipt.ResultingPositionId);
        Assert.Empty(Query(accepted.SuccessorHandle!, CampaignActionAudience.Axis).Candidates);
        Assert.Empty(Query(accepted.SuccessorHandle!, CampaignActionAudience.Commonwealth).Candidates);
        var weatherSet = Query(accepted.SuccessorHandle!, CampaignActionAudience.System);
        var weatherAction = Assert.Single(weatherSet.Candidates);
        Assert.Equal("resolve-weather", weatherAction.Kind);
        Assert.Equal(
            "sha256:61bca28b7e06c2ec8b7919bce4c7c226198e7fecb0afcc2186b224311e7e1413",
            weatherAction.ActionId);

        var weatherAccepted = CampaignLegalActions.Submit(
            accepted.SuccessorHandle!,
            Bind(weatherSet, weatherAction));
        Assert.True(weatherAccepted.IsAccepted);
        Assert.Equal(6, weatherAccepted.Receipt!.CommittedStateVersion);
        Assert.Equal(
            "land.position.operation-1.organization",
            weatherAccepted.Receipt.ResultingPositionId);
        Assert.Empty(Query(weatherAccepted.SuccessorHandle!, CampaignActionAudience.System).Candidates);

        var internalOrder = Assert.Single(accepted.SuccessorHandle!.Snapshot.OperationStageOrders);
        Assert.Equal(LandSide.Commonwealth, internalOrder.FirstSide);
        Assert.Equal(LandSide.Axis, internalOrder.SecondSide);
        Assert.Equal(LandSide.Axis, accepted.SuccessorHandle.Snapshot.InitiativeHolder);
    }

    [Fact]
    public void SubmissionRevalidatesExactAudienceAndConcurrencyInStablePrecedence()
    {
        var handle = CreateHandle();
        var set = Query(handle, CampaignActionAudience.System);
        var action = Assert.Single(set.Candidates);

        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidSubmission,
            CampaignLegalActions.Submit(handle, Bind(set, action) with { ContractVersion = 99 }).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidSubmission,
            CampaignLegalActions.Submit(handle, Bind(set, action) with { CampaignId = "bad id" }).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidSubmission,
            CampaignLegalActions.Submit(handle, Bind(set, action) with { ExpectedPositionId = "bad id" }).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.CampaignMismatch,
            CampaignLegalActions.Submit(handle, Bind(set, action) with { CampaignId = "campaign-other" }).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState,
            CampaignLegalActions.Submit(handle, Bind(set, action) with { ExpectedStateVersion = 99 }).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.UnexpectedPosition,
            CampaignLegalActions.Submit(handle, Bind(set, action) with { ExpectedPositionId = "land.position.wrong" }).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignLegalActions.Submit(handle, Bind(set, action) with { Audience = CampaignActionAudience.Axis }).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignLegalActions.Submit(handle, Bind(set, action) with { ActionId = $"sha256:{new string('0', 64)}" }).RejectionReason);

        var advanced = SubmitOnly(handle, CampaignActionAudience.System, "resolve-initiative");
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState,
            CampaignLegalActions.Submit(advanced, Bind(set, action)).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignLegalActions.Submit(advanced, Bind(set, action) with
            {
                ExpectedStateVersion = advanced.Snapshot.StateVersion,
                ExpectedPositionId = advanced.Snapshot.SequencePosition.PositionId,
            }).RejectionReason);
    }

    [Fact]
    public void CandidateIdsAndActionSetBytesAreCanonical()
    {
        var set = Query(CreateHandle(), CampaignActionAudience.System);
        var candidate = Assert.Single(set.Candidates);
        var semanticBytes = Encoding.UTF8.GetBytes(
            "{\"contractVersion\":1,\"kind\":\"resolve-initiative\"}");
        Assert.Equal($"sha256:{Convert.ToHexStringLower(SHA256.HashData(semanticBytes))}",
            candidate.ActionId);
        Assert.Equal(semanticBytes, CampaignActionCandidate.WriteSemantics(candidate.Kind, null));

        var json = Encoding.UTF8.GetString(CampaignLegalActionSerializer.Serialize(set));
        Assert.Equal(
            $"{{\"contractVersion\":1,\"policyId\":\"sandtable.legal-actions.v1\",\"campaignId\":\"campaign-actions\",\"stateVersion\":1,\"rulesetHash\":\"{Cna1979Ruleset.Manifest.Hash}\",\"positionId\":\"land.position.initiative-determination\",\"audience\":\"system\",\"candidates\":[{{\"contractVersion\":1,\"actionId\":\"{candidate.ActionId}\",\"kind\":\"resolve-initiative\"}}]}}",
            json);
    }

    [Fact]
    public void EquivalentCallerOrderCanonicalizesAndDuplicateCandidatesReject()
    {
        var first = new ActFirstAction(1);
        var last = new ActLastAction(1);
        var callerValues = new List<CampaignActionCandidate> { last, first };
        var reversed = new CampaignLegalActionSet("campaign-actions", 4,
            Cna1979Ruleset.Manifest.Hash, "land.position.operation-1.initiative-declaration",
            CampaignActionAudience.Axis, callerValues);
        var canonical = new CampaignLegalActionSet("campaign-actions", 4,
            Cna1979Ruleset.Manifest.Hash, "land.position.operation-1.initiative-declaration",
            CampaignActionAudience.Axis, [first, last]);

        callerValues.Clear();

        Assert.Equal(canonical, reversed);
        Assert.Equal(canonical.GetHashCode(), reversed.GetHashCode());
        Assert.Equal(["act-first", "act-last"], reversed.Candidates.Select(value => value.Kind));
        Assert.Equal(CampaignLegalActionSerializer.Serialize(canonical),
            CampaignLegalActionSerializer.Serialize(reversed));
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet("campaign-actions", 4,
            Cna1979Ruleset.Manifest.Hash, "land.position.operation-1.initiative-declaration",
            CampaignActionAudience.Axis, [first, first]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ActFirstAction(2));
    }

    [Fact]
    public void HolderActionSetIsNonEmptyAndIndependentOfOpponentOnlyAuthority()
    {
        var pair = CampaignObservationTestData.CreateOpponentOnlyPair(LandSide.Axis);
        var baseline = AdvanceToDeclaration(pair.BaselineSnapshot, pair.BaselineContext);
        var changed = AdvanceToDeclaration(pair.ChangedSnapshot, pair.ChangedContext);
        var baselineSet = Query(new CampaignAuthorityHandle(baseline, pair.BaselineContext),
            CampaignActionAudience.Axis);
        var changedSet = Query(new CampaignAuthorityHandle(changed, pair.ChangedContext),
            CampaignActionAudience.Axis);

        Assert.NotEmpty(baselineSet.Candidates);
        Assert.Equal(baselineSet, changedSet);
        Assert.Equal(CampaignLegalActionSerializer.Serialize(baselineSet),
            CampaignLegalActionSerializer.Serialize(changedSet));
        var json = Encoding.UTF8.GetString(CampaignLegalActionSerializer.Serialize(changedSet));
        Assert.DoesNotContain("enemy-sentinel", json, StringComparison.Ordinal);
    }

    [Fact]
    public void HandleHasNoPublicAuthorityOrSerializationSurface()
    {
        var type = typeof(CampaignAuthorityHandle);
        Assert.False(type.IsRecord());
        Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.Empty(type.GetProperties(BindingFlags.Public | BindingFlags.Instance));
        Assert.Empty(type.GetFields(BindingFlags.Public | BindingFlags.Instance));
        Assert.Equal(nameof(CampaignAuthorityHandle), CreateHandle().ToString());
    }

    [Fact]
    public void InvalidAudiencePrecedesInvalidAuthorityAndQueriesArePure()
    {
        var valid = CreateHandle();
        var invalid = new CampaignAuthorityHandle(valid.Snapshot with { ContractVersion = 99 }, valid.Context);

        Assert.Equal(CampaignLegalActionQueryRejectionReason.InvalidAudience,
            CampaignLegalActions.Query(invalid, (CampaignActionAudience)99).RejectionReason);
        Assert.Equal(CampaignLegalActionQueryRejectionReason.InvalidState,
            CampaignLegalActions.Query(invalid, CampaignActionAudience.System).RejectionReason);

        var before = CampaignSnapshotSerializer.Serialize(valid.Snapshot);
        var first = Query(valid, CampaignActionAudience.System);
        var second = Query(valid, CampaignActionAudience.System);
        Assert.Equal(CampaignLegalActionSerializer.Serialize(first),
            CampaignLegalActionSerializer.Serialize(second));
        Assert.Equal(before, CampaignSnapshotSerializer.Serialize(valid.Snapshot));
    }

    [Fact]
    public void ActionAndReceiptBytesIgnoreCultureAndContainNoAuthorityPayload()
    {
        var handle = CreateHandle();
        var set = Query(handle, CampaignActionAudience.System);
        var actionBytes = CampaignLegalActionSerializer.Serialize(set);
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ar-SA");
            Assert.Equal(actionBytes, CampaignLegalActionSerializer.Serialize(
                Query(handle, CampaignActionAudience.System)));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }

        var result = CampaignLegalActions.Submit(handle, Bind(set, Assert.Single(set.Candidates)));
        var receiptJson = Encoding.UTF8.GetString(
            CampaignActionAcceptanceReceiptSerializer.Serialize(result.Receipt!));
        Assert.DoesNotContain("event", receiptJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("setup", receiptJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("world", receiptJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("random", receiptJson, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("{}", JsonSerializer.Serialize(handle));
    }

    [Fact]
    public void RejectedSubmissionLeavesAuthorityAndRandomnessUnchanged()
    {
        var handle = CreateHandle();
        var set = Query(handle, CampaignActionAudience.System);
        var before = CampaignSnapshotSerializer.Serialize(handle.Snapshot);
        var cursor = handle.Snapshot.RandomState.NextByteCursor;

        var rejected = CampaignLegalActions.Submit(handle,
            Bind(set, Assert.Single(set.Candidates)) with { Audience = CampaignActionAudience.Axis });

        Assert.False(rejected.IsAccepted);
        Assert.Null(rejected.SuccessorHandle);
        Assert.Null(rejected.Receipt);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            rejected.RejectionReason);
        Assert.Equal(before, CampaignSnapshotSerializer.Serialize(handle.Snapshot));
        Assert.Equal(cursor, handle.Snapshot.RandomState.NextByteCursor);
    }

    [Fact]
    public void AcceptedSubmissionMatchesTheInternalMechanicEventAndProjection()
    {
        var handle = CreateHandle();
        handle = SubmitOnly(handle, CampaignActionAudience.System, "resolve-initiative");
        handle = SubmitOnly(handle, CampaignActionAudience.System,
            "resolve-no-obligation-naval-convoy-schedule");
        handle = SubmitOnly(handle, CampaignActionAudience.System,
            "resolve-no-obligation-tactical-shipping");
        var set = Query(handle, CampaignActionAudience.Axis);
        var candidate = set.Candidates.Single(value => value.Kind == "act-first");
        var command = new DeclareInitiativeOrder(handle.Snapshot.StateVersion,
            handle.Snapshot.SequencePosition.PositionId, 1, LandSide.Axis,
            InitiativeOrderChoice.ActFirst);
        var internalDecision = CampaignEngine.Decide(handle.Snapshot, command, handle.Context);
        var expectedEvent = Assert.IsType<InitiativeOrderDeclared>(
            Assert.Single(internalDecision.Events));
        var expected = CampaignProjector.Apply(handle.Snapshot, expectedEvent, handle.Context);

        var accepted = CampaignLegalActions.Submit(handle, Bind(set, candidate));

        Assert.True(accepted.IsAccepted);
        Assert.Equal(CampaignSnapshotSerializer.Serialize(expected),
            CampaignSnapshotSerializer.Serialize(accepted.SuccessorHandle!.Snapshot));
        Assert.Equal(candidate.ActionId, accepted.Receipt!.ActionId);
    }

    [Fact]
    public void ActionAndReceiptTypeGraphsContainOnlySideSafeValues()
    {
        Type[] roots =
        [
            typeof(CampaignLegalActionSet),
            typeof(CampaignActionCandidate),
            typeof(ResolveInitiativeAction),
            typeof(ResolveNoObligationNavalConvoyScheduleAction),
            typeof(ResolveNoObligationTacticalShippingAction),
            typeof(ResolveWeatherAction),
            typeof(ActFirstAction),
            typeof(ActLastAction),
            typeof(CampaignActionAcceptanceReceipt),
        ];
        Type[] forbidden =
        [
            typeof(CampaignSnapshot),
            typeof(CampaignSetupSnapshot),
            typeof(CampaignWorldSnapshot),
            typeof(CampaignContentContext),
            typeof(CampaignCommand),
            typeof(CampaignEvent),
            typeof(Cna.Core.Randomness.RandomStreamState),
            typeof(RuleReference),
        ];

        Assert.All(roots, root => Assert.All(root.GetProperties(BindingFlags.Public
            | BindingFlags.Instance), property => Assert.DoesNotContain(
                forbidden, value => ContainsType(property.PropertyType, value))));
        Assert.All(roots.Where(type => !type.IsAbstract), root =>
            Assert.Empty(root.GetConstructors(BindingFlags.Public | BindingFlags.Instance)));
    }

    private static CampaignAuthorityHandle CreateHandle()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        var request = new CampaignCreationRequest(1, "campaign-actions", Cna1979Ruleset.Manifest.Hash,
            12345, setup.SetupId, setup.Hash, setup.Content.Pack.PackId, setup.Content.Pack.Hash,
            setup.Content.ScenarioId);
        var result = CampaignAuthority.Create(request);
        Assert.True(result.IsCreated);
        return result.Handle!;
    }

    private static CampaignLegalActionSet Query(CampaignAuthorityHandle handle,
        CampaignActionAudience audience)
    {
        var result = CampaignLegalActions.Query(handle, audience);
        Assert.True(result.IsSuccessful);
        return result.ActionSet!;
    }

    private static CampaignAuthorityHandle SubmitOnly(CampaignAuthorityHandle handle,
        CampaignActionAudience audience, string kind)
    {
        var set = Query(handle, audience);
        var candidate = Assert.Single(set.Candidates, value => value.Kind == kind);
        var result = CampaignLegalActions.Submit(handle, Bind(set, candidate));
        Assert.True(result.IsAccepted);
        return result.SuccessorHandle!;
    }

    private static CampaignActionSubmission Bind(CampaignLegalActionSet set,
        CampaignActionCandidate candidate) => new(1, set.CampaignId, set.StateVersion,
        set.PositionId, set.Audience, candidate.ActionId);

    private static CampaignSnapshot AdvanceToDeclaration(CampaignSnapshot snapshot,
        CampaignContentContext context)
    {
        CampaignCommand[] commands =
        [
            new ResolveInitiative(1, snapshot.SequencePosition.PositionId),
            new ResolveNoObligationNavalConvoySchedule(2, "land.position.naval-convoy.schedule"),
            new ResolveNoObligationTacticalShipping(3, "land.position.naval-convoy.tactical-shipping"),
        ];
        foreach (var command in commands)
        {
            var result = CampaignEngine.Decide(snapshot, command, context);
            snapshot = CampaignProjector.Apply(snapshot, Assert.Single(result.Events), context);
        }
        return snapshot;
    }

    private static bool ContainsType(Type candidate, Type forbidden)
    {
        if (candidate == forbidden) return true;
        if (candidate.IsArray) return ContainsType(candidate.GetElementType()!, forbidden);
        return candidate.IsGenericType
            && candidate.GetGenericArguments().Any(argument => ContainsType(argument, forbidden));
    }
}

internal static class TypeReflectionExtensions
{
    public static bool IsRecord(this Type type) => type.GetMethod("<Clone>$",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) is not null;
}
