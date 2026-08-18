namespace Cna.Core.Campaigns;

public sealed class CampaignAuthorityHandle
{
    internal CampaignAuthorityHandle(CampaignSnapshot snapshot, CampaignContentContext context)
    {
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    internal CampaignSnapshot Snapshot { get; }
    internal CampaignContentContext Context { get; }

    public override string ToString() => nameof(CampaignAuthorityHandle);
}
