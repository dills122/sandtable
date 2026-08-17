using System.Globalization;
using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Observations;
using Cna.Core.Rules;
using Cna.Core.Setups;
using Cna.Core.Tests.Campaigns;

namespace Cna.Core.Tests.Observations;

public sealed class CampaignObservationSerializationTests
{
    [Fact]
    public void InitialAxisObservationMatchesTheCompleteCanonicalGolden()
    {
        var observation = CreateAxisObservation();
        var expected = File.ReadAllBytes(Path.Combine(
            AppContext.BaseDirectory,
            "Observations",
            "Fixtures",
            "campaign-observation-axis.v1.golden.json"));

        var actual = CampaignObservationSerializer.SerializeCanonical(observation);

        Assert.Equal((byte)'\n', expected[^1]);
        Assert.Equal(expected.AsSpan(0, expected.Length - 1).ToArray(), actual);
        Assert.NotEqual((byte)'\n', actual[^1]);
        Assert.Equal((byte)'{', actual[0]);
    }

    [Theory]
    [InlineData("fr-FR")]
    [InlineData("ar-SA")]
    public void CanonicalBytesIgnoreCurrentCulture(string cultureName)
    {
        var observation = CreateAxisObservation();
        var expected = CampaignObservationSerializer.SerializeCanonical(observation);
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);

            Assert.Equal(
                expected,
                CampaignObservationSerializer.SerializeCanonical(observation));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public void ReturnedBytesAreIndependentAndContainOnlyTheApprovedTopLevelAllowlist()
    {
        var observation = CreateAxisObservation();
        var first = CampaignObservationSerializer.SerializeCanonical(observation);
        var baseline = first.ToArray();
        first[0] = (byte)'[';

        var second = CampaignObservationSerializer.SerializeCanonical(observation);
        using var document = JsonDocument.Parse(second);

        Assert.Equal(baseline, second);
        Assert.Equal(
            [
                "contractVersion",
                "policyId",
                "campaignId",
                "stateVersion",
                "rulesetHash",
                "scenarioId",
                "observer",
                "position",
                "locations",
                "edges",
                "ownElements",
            ],
            document.RootElement.EnumerateObject().Select(property => property.Name));
        Assert.DoesNotContain("contentPack", Encoding.UTF8.GetString(second), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("source", Encoding.UTF8.GetString(second), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("random", Encoding.UTF8.GetString(second), StringComparison.OrdinalIgnoreCase);
    }

    private static CampaignObservation CreateAxisObservation()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        var created = CampaignTestHarness.Decide(
            null,
            CampaignTestHarness.Create(
                "campaign-1",
                Cna1979Ruleset.Manifest.Hash,
                12345,
                setup.SetupId,
                setup.Hash));
        var snapshot = CampaignTestHarness.Replay(created.Events);
        var result = CampaignObservationProjector.Project(
            snapshot,
            CampaignTestHarness.ContextFor(snapshot),
            LandSide.Axis);

        return Assert.IsType<CampaignObservation>(result.Observation);
    }
}
