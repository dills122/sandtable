namespace Cna.Core.Campaigns;

public sealed class InvalidCampaignHistoryException : Exception
{
    public InvalidCampaignHistoryException(string message)
        : base(message)
    {
    }
}
