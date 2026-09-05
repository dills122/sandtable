namespace Cna.Core.Campaigns;

public sealed class CampaignAuthorityHandle
{
    private readonly CampaignSnapshot? legacySnapshot;

    internal CampaignAuthorityHandle(CampaignSnapshot snapshot, CampaignContentContext context)
    {
        legacySnapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    internal CampaignAuthorityHandle(CampaignSnapshotV10 snapshot, CampaignContentContext context)
    {
        CurrentSnapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        Context = context ?? throw new ArgumentNullException(nameof(context));
        try
        {
            legacySnapshot = CampaignV10LegacyBridge.ToLegacy(snapshot, context);
        }
        catch (InvalidOperationException)
        {
            // Friend tests can exercise structurally valid non-catalog successor fixtures. They
            // have no predecessor compatibility view and never enter the legacy branch.
            legacySnapshot = null;
        }
    }

    internal CampaignSnapshot Snapshot => legacySnapshot ?? throw new InvalidOperationException(
        "This current Campaign authority has no predecessor compatibility view.");
    internal CampaignSnapshotV10? CurrentSnapshot { get; }
    internal CampaignContentContext Context { get; }

    public override string ToString() => nameof(CampaignAuthorityHandle);
}
