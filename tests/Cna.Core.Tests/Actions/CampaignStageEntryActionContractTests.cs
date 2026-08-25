using System.Globalization;
using System.Reflection;
using System.Text;
using Cna.Core.Actions;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Actions;

public sealed class CampaignStageEntryActionContractTests
{
    [Fact]
    public void StageEntryCandidatesHaveFrozenPayloadFreeSemanticsAndIds()
    {
        CampaignActionCandidate[] candidates =
        [
            new ResolveNoObligationOrganizationAction(),
            new ResolveNoObligationNavalConvoyArrivalAction(),
            new ResolveNoObligationFleetAssignmentAction(),
            new ResolveNoObligationFleetRepairAction(),
        ];
        (string Kind, string ActionId)[] expected =
        [
            ("resolve-no-obligation-organization",
                "sha256:2200e6c4cef001d344d85de78fc7a10c13b32c12975d905c633ca430c3c4bd4c"),
            ("resolve-no-obligation-naval-convoy-arrival",
                "sha256:a49ff99f7e52193fdee44b50751e64025121cb9a2a75a054fdf2ad045e013632"),
            ("resolve-no-obligation-fleet-assignment",
                "sha256:c2d7dae34d20f826d2e7e682b8d3b437224e42b522857f63b3869d1a1bf3bcc5"),
            ("resolve-no-obligation-fleet-repair",
                "sha256:ea4fe4f27344a8659c81b05fd84df2e260bb22da1edf1115cd4d75dfd89d7d3e"),
        ];

        Assert.Collection(candidates, expected.Select(value =>
            new Action<CampaignActionCandidate>(candidate =>
            {
                Assert.Equal(CampaignActionCandidate.CurrentContractVersion,
                    candidate.ContractVersion);
                Assert.Equal(value.Kind, candidate.Kind);
                Assert.Equal(value.ActionId, candidate.ActionId);
                Assert.Null(candidate.OperationStage);
                Assert.Equal(
                    Encoding.UTF8.GetBytes(
                        $"{{\"contractVersion\":1,\"kind\":\"{value.Kind}\"}}"),
                    CampaignActionCandidate.WriteSemantics(candidate.Kind,
                        candidate.OperationStage));
            })).ToArray());
    }

    [Fact]
    public void StageEntryActionSetBytesAreCanonicallyOrderedAndCultureInvariant()
    {
        var callerOrder = new List<CampaignActionCandidate>
        {
            new ResolveNoObligationOrganizationAction(),
            new ResolveNoObligationNavalConvoyArrivalAction(),
            new ResolveNoObligationFleetRepairAction(),
            new ResolveNoObligationFleetAssignmentAction(),
        };
        var actionSet = new CampaignLegalActionSet(
            "campaign-stage-entry",
            6,
            Cna1979Ruleset.Manifest.Hash,
            "land.position.operation-1.organization",
            CampaignActionAudience.System,
            callerOrder);
        callerOrder.Clear();

        var expected =
            $"{{\"contractVersion\":2,\"policyId\":\"sandtable.legal-actions.v2\",\"campaignId\":\"campaign-stage-entry\",\"stateVersion\":6,\"rulesetHash\":\"{Cna1979Ruleset.Manifest.Hash}\",\"positionId\":\"land.position.operation-1.organization\",\"audience\":\"system\",\"candidates\":[{{\"contractVersion\":1,\"actionId\":\"sha256:c2d7dae34d20f826d2e7e682b8d3b437224e42b522857f63b3869d1a1bf3bcc5\",\"kind\":\"resolve-no-obligation-fleet-assignment\"}},{{\"contractVersion\":1,\"actionId\":\"sha256:ea4fe4f27344a8659c81b05fd84df2e260bb22da1edf1115cd4d75dfd89d7d3e\",\"kind\":\"resolve-no-obligation-fleet-repair\"}},{{\"contractVersion\":1,\"actionId\":\"sha256:a49ff99f7e52193fdee44b50751e64025121cb9a2a75a054fdf2ad045e013632\",\"kind\":\"resolve-no-obligation-naval-convoy-arrival\"}},{{\"contractVersion\":1,\"actionId\":\"sha256:2200e6c4cef001d344d85de78fc7a10c13b32c12975d905c633ca430c3c4bd4c\",\"kind\":\"resolve-no-obligation-organization\"}}]}}";
        var baseline = CampaignLegalActionSerializer.Serialize(actionSet);

        Assert.Equal(expected, Encoding.UTF8.GetString(baseline));
        Assert.Equal(
            [
                "resolve-no-obligation-fleet-assignment",
                "resolve-no-obligation-fleet-repair",
                "resolve-no-obligation-naval-convoy-arrival",
                "resolve-no-obligation-organization",
            ],
            actionSet.Candidates.Select(candidate => candidate.Kind));

        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ar-SA");
            Assert.Equal(baseline, CampaignLegalActionSerializer.Serialize(actionSet));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public void StageEntryCandidatesAreClosedOutputOnlyContracts()
    {
        Type[] candidateTypes =
        [
            typeof(ResolveNoObligationOrganizationAction),
            typeof(ResolveNoObligationNavalConvoyArrivalAction),
            typeof(ResolveNoObligationFleetAssignmentAction),
            typeof(ResolveNoObligationFleetRepairAction),
        ];

        Assert.All(candidateTypes, type =>
        {
            Assert.True(type.IsPublic);
            Assert.True(type.IsSealed);
            Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
            var parameterlessConstructor = type.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                Type.EmptyTypes,
                modifiers: null);
            Assert.NotNull(parameterlessConstructor);
            Assert.True(parameterlessConstructor.IsAssembly);
            Assert.Empty(type.GetProperties(BindingFlags.Public | BindingFlags.Instance
                | BindingFlags.DeclaredOnly));
        });
        Assert.DoesNotContain(
            typeof(CampaignLegalActionSerializer).GetMethods(
                BindingFlags.Public | BindingFlags.Static),
            method => method.Name.StartsWith("Deserialize", StringComparison.Ordinal));
    }
}
