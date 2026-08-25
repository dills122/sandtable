using System.Globalization;
using System.Text;
using System.Text.Json;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Setups;

public sealed class CampaignStageEntryPolicyTests
{
    private const string CanonicalExplicitNone =
        "{\"contractVersion\":1,\"gameTurn\":1,\"operationStage\":1,"
        + "\"organization\":\"explicit-none\","
        + "\"navalConvoyArrival\":\"explicit-none\","
        + "\"fleetAssignment\":\"explicit-none\","
        + "\"fleetRepair\":\"explicit-none\","
        + "\"sources\":[{\"sourceId\":\"sandtable-rules-lab\","
        + "\"locator\":\"stage-entry.no-obligations.v1\"}]}";

    [Fact]
    public void FrozenEnumOrdinalsAreStable()
    {
        Assert.Equal(1, (int)StageEntryObligationKind.ExplicitNone);
        Assert.Equal(2, (int)StageEntryObligationKind.HasObligations);
    }

    [Fact]
    public void ExplicitNonePolicyStrictlyRoundTripsCanonicalBytes()
    {
        var policy = CreatePolicy();
        var canonical = CampaignStageEntryPolicyCodec.SerializeCanonical(policy);
        var parsed = CampaignStageEntryPolicyCodec.DeserializeCanonical(canonical);

        Assert.Equal(CanonicalExplicitNone, Encoding.UTF8.GetString(canonical));
        Assert.Equal(policy, parsed);
        Assert.Equal(policy.GetHashCode(), parsed.GetHashCode());
        Assert.Equal(canonical, CampaignStageEntryPolicyCodec.SerializeCanonical(parsed));
    }

    [Fact]
    public void RecognizedHasObligationsKindStrictlyRoundTripsForLaterAdmissionRejection()
    {
        var policy = CreatePolicy(
            organization: StageEntryObligationKind.HasObligations,
            navalConvoyArrival: StageEntryObligationKind.HasObligations,
            fleetAssignment: StageEntryObligationKind.HasObligations,
            fleetRepair: StageEntryObligationKind.HasObligations);

        var canonical = CampaignStageEntryPolicyCodec.SerializeCanonical(policy);
        var parsed = CampaignStageEntryPolicyCodec.DeserializeCanonical(canonical);

        Assert.Contains("\"organization\":\"has-obligations\"", Encoding.UTF8.GetString(canonical));
        Assert.Equal(policy, parsed);
    }

    [Fact]
    public void ConstructorRejectsInvalidVersionPairKindAndSourceContracts()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreatePolicy(contractVersion: 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreatePolicy(gameTurn: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreatePolicy(operationStage: 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreatePolicy(
            organization: (StageEntryObligationKind)0));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreatePolicy(
            navalConvoyArrival: (StageEntryObligationKind)0));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreatePolicy(
            fleetAssignment: (StageEntryObligationKind)0));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreatePolicy(
            fleetRepair: (StageEntryObligationKind)0));
        Assert.Throws<ArgumentNullException>(() => new CampaignStageEntryPolicy(
            CampaignStageEntryPolicy.CurrentContractVersion,
            1,
            1,
            StageEntryObligationKind.ExplicitNone,
            StageEntryObligationKind.ExplicitNone,
            StageEntryObligationKind.ExplicitNone,
            StageEntryObligationKind.ExplicitNone,
            null!));
        Assert.Throws<ArgumentException>(() => CreatePolicy(sources: []));
        Assert.Throws<ArgumentException>(() => CreatePolicy(
            sources:
            [
                CampaignStageEntryPolicy.SourceReference,
                CampaignStageEntryPolicy.SourceReference,
            ]));
        Assert.Throws<ArgumentException>(() => CreatePolicy(
            sources: [new RuleReference("sandtable-rules-lab", "changed.v1")]));
        Assert.Throws<ArgumentException>(() => CreatePolicy(
            sources:
            [
                CampaignStageEntryPolicy.SourceReference,
                new RuleReference("spi-1979-land-rules", "5.2"),
            ]));
    }

    [Fact]
    public void DecoderRejectsNoncanonicalMalformedAndUnknownContracts()
    {
        string[] invalid =
        [
            CanonicalExplicitNone.Replace(
                "\"contractVersion\":1",
                "\"contractVersion\":2",
                StringComparison.Ordinal),
            CanonicalExplicitNone.Replace(
                "\"operationStage\":1",
                "\"operationStage\":2",
                StringComparison.Ordinal),
            CanonicalExplicitNone.Replace(
                "\"organization\":\"explicit-none\"",
                "\"organization\":\"unknown\"",
                StringComparison.Ordinal),
            CanonicalExplicitNone.Replace(
                "\"sourceId\":\"sandtable-rules-lab\"",
                "\"sourceId\":\"changed\"",
                StringComparison.Ordinal),
            CanonicalExplicitNone.Replace(
                "{\"sourceId\":\"sandtable-rules-lab\","
                + "\"locator\":\"stage-entry.no-obligations.v1\"}",
                "{\"locator\":\"stage-entry.no-obligations.v1\","
                + "\"sourceId\":\"sandtable-rules-lab\"}",
                StringComparison.Ordinal),
            CanonicalExplicitNone.Replace(
                "\"gameTurn\":1,\"operationStage\":1",
                "\"operationStage\":1,\"gameTurn\":1",
                StringComparison.Ordinal),
            CanonicalExplicitNone.Replace(
                "\"gameTurn\":1",
                "\"gameTurn\":1,\"gameTurn\":1",
                StringComparison.Ordinal),
            CanonicalExplicitNone[..^1] + ",\"extra\":true}",
            CanonicalExplicitNone + "\n",
            " {" + CanonicalExplicitNone[1..],
            "{}",
        ];

        Assert.All(invalid, value => Assert.Throws<JsonException>(() =>
            CampaignStageEntryPolicyCodec.DeserializeCanonical(Encoding.UTF8.GetBytes(value))));
    }

    [Fact]
    public void CanonicalEncodingIsCultureIndependentAndPolicyCopiesSources()
    {
        var sources = new List<RuleReference> { CampaignStageEntryPolicy.SourceReference };
        var policy = CreatePolicy(gameTurn: 43, sources: sources);
        var invariant = CampaignStageEntryPolicyCodec.SerializeCanonical(policy);
        sources.Clear();
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            Assert.Equal(invariant, CampaignStageEntryPolicyCodec.SerializeCanonical(policy));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }

        Assert.Equal([CampaignStageEntryPolicy.SourceReference], policy.Sources);
    }

    private static CampaignStageEntryPolicy CreatePolicy(
        int contractVersion = CampaignStageEntryPolicy.CurrentContractVersion,
        int gameTurn = 1,
        int operationStage = 1,
        StageEntryObligationKind organization = StageEntryObligationKind.ExplicitNone,
        StageEntryObligationKind navalConvoyArrival = StageEntryObligationKind.ExplicitNone,
        StageEntryObligationKind fleetAssignment = StageEntryObligationKind.ExplicitNone,
        StageEntryObligationKind fleetRepair = StageEntryObligationKind.ExplicitNone,
        IReadOnlyList<RuleReference>? sources = null) => new(
            contractVersion,
            gameTurn,
            operationStage,
            organization,
            navalConvoyArrival,
            fleetAssignment,
            fleetRepair,
            sources ?? [CampaignStageEntryPolicy.SourceReference]);
}
