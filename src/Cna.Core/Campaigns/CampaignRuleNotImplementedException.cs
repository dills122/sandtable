namespace Cna.Core.Campaigns;

// A source-legal rule edge the engine deliberately has not built yet. Callers
// that admit owner choices map it to RuleNotImplemented instead of treating it
// as an illegal or never-legal choice (FID-002).
internal sealed class CampaignRuleNotImplementedException : InvalidOperationException
{
    public CampaignRuleNotImplementedException()
    {
    }

    public CampaignRuleNotImplementedException(string message) : base(message)
    {
    }

    public CampaignRuleNotImplementedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
