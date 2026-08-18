using System.Text;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignReplayPreparationTests
{
    [Fact]
    public void ExactCreationBytesPrepareReplayContextAndProjectCanonically()
    {
        var created = CreateEvent();
        var bytes = CampaignEventSerializer.Serialize(created);

        var result = CampaignReplayPreparation.Prepare(
            bytes,
            Cna1979SyntheticContentResolver.Instance);

        Assert.True(result.IsPrepared);
        Assert.Equal(CampaignReplayPreparationRejectionReason.None, result.RejectionReason);
        Assert.NotNull(result.Context);
        Assert.Equal(created.RulesetHash, result.Context.RulesetHash);
        Assert.Equal(created.Setup.Content, result.Context.Content.Selection);
        Assert.Equal(
            CampaignTestHarness.Replay([created]),
            CampaignProjector.Replay([created], result.Context.Content));
    }

    [Fact]
    public void MalformedCreationBytesMapToInvalidHistoryWhileDirectReadThrows()
    {
        var malformed = Encoding.UTF8.GetBytes("{not-json");

        Assert.ThrowsAny<System.Text.Json.JsonException>(
            () => CampaignEventSerializer.Deserialize(malformed));

        var result = CampaignReplayPreparation.Prepare(
            malformed,
            Cna1979SyntheticContentResolver.Instance);

        Assert.False(result.IsPrepared);
        Assert.Null(result.Context);
        Assert.Equal(
            CampaignReplayPreparationRejectionReason.InvalidHistory,
            result.RejectionReason);
    }

    [Fact]
    public void NonIntegerCreationMetadataMapsToInvalidHistory()
    {
        var canonical = Encoding.UTF8.GetString(
            CampaignEventSerializer.Serialize(CreateEvent()));
        var malformed = Encoding.UTF8.GetBytes(canonical.Replace(
            "\"stateVersion\":1,",
            "\"stateVersion\":1.5,",
            StringComparison.Ordinal));

        var result = CampaignReplayPreparation.Prepare(
            malformed,
            Cna1979SyntheticContentResolver.Instance);

        Assert.False(result.IsPrepared);
        Assert.Null(result.Context);
        Assert.Equal(
            CampaignReplayPreparationRejectionReason.InvalidHistory,
            result.RejectionReason);
    }

    [Theory]
    [InlineData(ContentCatalogRejectionReason.UnknownPackId, (int)CampaignReplayPreparationRejectionReason.MissingContent)]
    [InlineData(ContentCatalogRejectionReason.HashMismatch, (int)CampaignReplayPreparationRejectionReason.ContentHashMismatch)]
    public void ExactContentResolutionFailuresAreTyped(
        ContentCatalogRejectionReason catalogReason,
        int expected)
    {
        var bytes = CampaignEventSerializer.Serialize(CreateEvent());

        var result = CampaignReplayPreparation.Prepare(
            bytes,
            new RejectingResolver(catalogReason));

        Assert.False(result.IsPrepared);
        Assert.Null(result.Context);
        Assert.Equal((CampaignReplayPreparationRejectionReason)expected, result.RejectionReason);
    }

    [Fact]
    public void WellFormedButUnsupportedRulesetHashIsTypedBeforeContentResolution()
    {
        var created = CreateEvent();
        var canonical = Encoding.UTF8.GetString(CampaignEventSerializer.Serialize(created));
        var unsupported = canonical.Replace(
            created.RulesetHash,
            new string('0', 64),
            StringComparison.Ordinal);
        var resolver = new CountingResolver();

        var result = CampaignReplayPreparation.Prepare(
            Encoding.UTF8.GetBytes(unsupported),
            resolver);

        Assert.Equal(
            CampaignReplayPreparationRejectionReason.UnsupportedRuleset,
            result.RejectionReason);
        Assert.Equal(0, resolver.CallCount);
    }

    private static CampaignCreated CreateEvent()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        var result = CampaignEngine.DecideCreation(
            null,
            new CreateCampaign(
                "campaign-1",
                Cna1979Ruleset.Manifest.Hash,
                12345,
                setup.SetupId,
                setup.Hash,
                setup.Content.Pack.PackId,
                setup.Content.Pack.Hash,
                setup.Content.ScenarioId),
            Cna1979SyntheticContentResolver.Instance);
        return Assert.IsType<CampaignCreated>(Assert.Single(result.Events));
    }

    private sealed class RejectingResolver(ContentCatalogRejectionReason reason)
        : IContentPackResolver
    {
        public ContentCatalogResolution Resolve(string packId, string expectedHash) =>
            reason == ContentCatalogRejectionReason.UnknownPackId
                ? Cna1979SyntheticContentCatalog.Resolve("unknown-pack", expectedHash)
                : Cna1979SyntheticContentCatalog.Resolve(
                    packId,
                    $"sha256:{new string('0', 64)}");
    }

    private sealed class CountingResolver : IContentPackResolver
    {
        public int CallCount { get; private set; }

        public ContentCatalogResolution Resolve(string packId, string expectedHash)
        {
            CallCount++;
            return Cna1979SyntheticContentCatalog.Resolve(packId, expectedHash);
        }
    }
}
