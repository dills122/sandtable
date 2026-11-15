using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// Successor snapshot for the bounded synthetic cut, not a current campaign save.
internal sealed class CampaignScheduledWorld6Snapshot
{
    private readonly CampaignScheduledWorld6PreambleEvents.VerifiedCut proof;
    private readonly CampaignScheduledWorld6Phase phase;

    private CampaignScheduledWorld6Snapshot(CampaignScheduledWorld6PreambleEvents.VerifiedCut proof, CampaignScheduledWorld6Phase phase)
    {
        this.proof = proof;
        this.phase = phase;
    }

    public string CampaignId => phase.CampaignId;
    public long StateVersion => phase.Version;
    public CampaignWorldSnapshotV6 World => phase.World;
    public LandSequencePosition Position => phase.Position;
    public RandomStreamState RandomState => proof.Cut.RandomState;
    public LandSide? InitiativeHolder => proof.Cut.InitiativeHolder;
    public IReadOnlyList<CampaignOperationStageOrder> Orders => proof.Cut.Orders;
    public IReadOnlyList<CampaignOperationStageWeather> Weather => proof.Cut.Weather;
    public IReadOnlyList<CampaignScheduledWorld6Offer> QueryOffers(LandSide side) => phase.QueryOffers(side);

    public static CampaignScheduledWorld6Snapshot AtArrival(CampaignScheduledWorld6PreambleEvents.VerifiedCut proof)
    {
        ArgumentNullException.ThrowIfNull(proof);
        return new CampaignScheduledWorld6Snapshot(proof, CampaignScheduledWorld6Phase.Start(proof));
    }

    public (CampaignScheduledWorld6Snapshot State, CampaignScheduledWorld6Event Event, bool IsRetry) Decide(CampaignScheduledWorld6Command command)
    {
        var result = phase.Decide(command);
        return (result.IsRetry ? this : new CampaignScheduledWorld6Snapshot(proof, result.State), result.Event, result.IsRetry);
    }

    public byte[] EncodeCheckpoint() => CampaignScheduledWorld6CheckpointCodec.Encode(phase);

    public static CampaignScheduledWorld6Snapshot DecodeCheckpoint(ReadOnlySpan<byte> bytes, CampaignScheduledWorld6PreambleEvents.VerifiedCut proof)
    {
        ArgumentNullException.ThrowIfNull(proof);
        return new CampaignScheduledWorld6Snapshot(proof, CampaignScheduledWorld6CheckpointCodec.Decode(bytes, proof));
    }
}
