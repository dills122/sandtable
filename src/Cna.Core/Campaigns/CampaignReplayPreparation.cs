using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

public enum CampaignReplayPreparationRejectionReason
{
    None,
    InvalidHistory,
    MissingContent,
    ContentHashMismatch,
    UnsupportedRuleset,
}

public sealed record CampaignReplayContext
{
    internal CampaignReplayContext(string rulesetHash, CampaignContentContext content)
    {
        RulesetHash = rulesetHash;
        Content = content;
    }

    public string RulesetHash { get; }

    public CampaignContentContext Content { get; }
}

public sealed record CampaignReplayPreparationResult
{
    private CampaignReplayPreparationResult(
        CampaignReplayContext? context,
        CampaignReplayPreparationRejectionReason rejectionReason)
    {
        Context = context;
        RejectionReason = rejectionReason;
    }

    public bool IsPrepared => Context is not null;

    public CampaignReplayContext? Context { get; }

    public CampaignReplayPreparationRejectionReason RejectionReason { get; }

    internal static CampaignReplayPreparationResult Prepared(CampaignReplayContext context) =>
        new(context, CampaignReplayPreparationRejectionReason.None);

    internal static CampaignReplayPreparationResult Rejected(
        CampaignReplayPreparationRejectionReason reason) => new(null, reason);
}

public static class CampaignReplayPreparation
{
    public static CampaignReplayPreparationResult Prepare(
        ReadOnlyMemory<byte> canonicalCreationEvent,
        IContentPackResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        CampaignCreated created;

        try
        {
            created = CampaignEventSerializer.Deserialize(canonicalCreationEvent)
                as CampaignCreated
                ?? throw new JsonException("History must begin with campaign creation.");
        }
        catch (JsonException)
        {
            return CampaignReplayPreparationResult.Rejected(
                CampaignReplayPreparationRejectionReason.InvalidHistory);
        }

        if (!Cna1979Ruleset.IsCanonicalHash(created.RulesetHash))
        {
            return CampaignReplayPreparationResult.Rejected(
                CampaignReplayPreparationRejectionReason.UnsupportedRuleset);
        }

        var selection = created.Setup.Content;
        var resolution = resolver.Resolve(selection.Pack.PackId, selection.Pack.Hash);

        if (!resolution.IsResolved)
        {
            return CampaignReplayPreparationResult.Rejected(
                resolution.RejectionReason switch
                {
                    ContentCatalogRejectionReason.UnknownPackId =>
                        CampaignReplayPreparationRejectionReason.MissingContent,
                    ContentCatalogRejectionReason.HashMismatch =>
                        CampaignReplayPreparationRejectionReason.ContentHashMismatch,
                    _ => CampaignReplayPreparationRejectionReason.InvalidHistory,
                });
        }

        try
        {
            var content = CampaignContentContext.Create(
                resolution.Artifact!,
                selection.ScenarioId);

            if (content.Selection != selection)
            {
                return CampaignReplayPreparationResult.Rejected(
                    CampaignReplayPreparationRejectionReason.InvalidHistory);
            }

            return CampaignReplayPreparationResult.Prepared(
                new CampaignReplayContext(created.RulesetHash, content));
        }
        catch (ArgumentException)
        {
            return CampaignReplayPreparationResult.Rejected(
                CampaignReplayPreparationRejectionReason.InvalidHistory);
        }
    }
}
