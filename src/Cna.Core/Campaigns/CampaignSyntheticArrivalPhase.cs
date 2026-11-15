using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignSyntheticArrivalCommand(int ContractVersion, string CampaignId, string SetupHash,
    string CommandId, long ExpectedVersion, LandSide Actor, string GroupId, string DestinationId);

internal sealed record CampaignSyntheticArrivalOffer(string GroupId, string DestinationId);

internal sealed class CampaignSyntheticArrivalEvent
{
    public CampaignSyntheticArrivalEvent(int contractVersion, string setupHash, CampaignSyntheticArrivalCommand command,
        long resultVersion, IEnumerable<string> elementIds, string successorPositionId)
    {
        ArgumentNullException.ThrowIfNull(elementIds);
        ContractVersion = contractVersion;
        SetupHash = setupHash;
        Command = command;
        ResultVersion = resultVersion;
        ElementIds = Array.AsReadOnly(elementIds.ToArray());
        SuccessorPositionId = successorPositionId;
    }

    public int ContractVersion { get; }
    public string SetupHash { get; }
    public CampaignSyntheticArrivalCommand Command { get; }
    public long ResultVersion { get; }
    public IReadOnlyList<string> ElementIds { get; }
    public string SuccessorPositionId { get; }
}

// Bounded synthetic Core transition. Not a campaign snapshot or Host authority.
internal sealed class CampaignSyntheticArrivalPhase
{
    private readonly CampaignArrivalReplayDraft arrivals;
    private readonly CampaignSyntheticArrivalEvent? receipt;

    private CampaignSyntheticArrivalPhase(string campaignId, CampaignArrivalReplayDraft arrivals, LandSequencePosition position,
        CampaignSyntheticArrivalEvent? receipt)
    {
        CampaignId = campaignId;
        this.arrivals = arrivals;
        Position = position;
        this.receipt = receipt;
    }

    public static string SetupHash => CampaignSyntheticArrivalSetup.Instance.Hash;
    public long Version => arrivals.Version;
    public string CampaignId { get; }
    public CampaignWorldSnapshot World => arrivals.World;
    public LandSequencePosition Position { get; }
    internal CampaignSyntheticArrivalEvent? Receipt => receipt;

    public IReadOnlyList<CampaignSyntheticArrivalOffer> QueryOffers(LandSide side)
    {
        if (!Enum.IsDefined(side))
            throw new ArgumentOutOfRangeException(nameof(side));
        var identity = CampaignArrivalSyntheticProfile.Identity;
        var group = identity.Groups.Single();
        return side == group.Owner && Position == ArrivalPosition()
            ? Array.AsReadOnly(new[] { new CampaignSyntheticArrivalOffer(group.GroupId, identity.Destinations[group.GroupId].Single()) })
            : Array.Empty<CampaignSyntheticArrivalOffer>();
    }

    public static CampaignSyntheticArrivalPhase Start(string campaignId, LandSequencePosition position)
    {
        ContentContractGuards.RequireStableId(campaignId, nameof(campaignId));
        ArgumentNullException.ThrowIfNull(position);
        if (position != ArrivalPosition())
            throw new ArgumentException("Synthetic arrivals require their exact arrival phase.", nameof(position));
        return new CampaignSyntheticArrivalPhase(campaignId, CampaignArrivalReplayDraft.Create(CampaignArrivalSyntheticProfile.Identity), position, null);
    }

    public (CampaignSyntheticArrivalPhase State, CampaignSyntheticArrivalEvent Event, bool IsRetry) Decide(CampaignSyntheticArrivalCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.ContractVersion != 1 || command.CampaignId != CampaignId || command.SetupHash != SetupHash)
            throw new ArgumentException("Unsupported arrival setup or command version.", nameof(command));
        var draftCommand = new CampaignArrivalDraftCommand(1, command.CommandId, command.ExpectedVersion,
            command.GroupId, command.Actor, 1, 1, command.DestinationId);
        var result = arrivals.Decide(draftCommand);
        if (result.IsRetry)
        {
            if (receipt is null || receipt.Command != command)
                throw new ArgumentException("Arrival retry does not match the retained receipt.", nameof(command));
            return (this, receipt, true);
        }
        if (Position != ArrivalPosition())
            throw new InvalidOperationException("Arrival phase has already completed.");
        var successor = Cna1979LandSequence.GetNext(Position);
        if (successor.PhaseId != LandPhaseIds.CommonwealthFleet || successor.SegmentId != LandSegmentIds.FleetAssignment)
            throw new InvalidOperationException("Unexpected synthetic arrival successor.");
        var recorded = new CampaignSyntheticArrivalEvent(1, SetupHash, command, result.State.Version,
            result.Event.ElementIds, successor.PositionId);
        return (new CampaignSyntheticArrivalPhase(CampaignId, result.State, successor, recorded), recorded, false);
    }

    public static CampaignSyntheticArrivalPhase Replay(string campaignId, IEnumerable<CampaignSyntheticArrivalEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        var state = Start(campaignId, ArrivalPosition());
        foreach (var recorded in events)
        {
            if (recorded is null || recorded.ContractVersion != 1 || recorded.SetupHash != SetupHash || recorded.Command is null)
                throw new InvalidOperationException("Invalid synthetic arrival history.");
            try
            {
                var result = state.Decide(recorded.Command);
                if (result.IsRetry || result.Event.ResultVersion != recorded.ResultVersion
                    || result.Event.SuccessorPositionId != recorded.SuccessorPositionId
                    || !result.Event.ElementIds.SequenceEqual(recorded.ElementIds, StringComparer.Ordinal))
                    throw new InvalidOperationException("Invalid synthetic arrival history.");
                state = result.State;
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException("Invalid synthetic arrival history.", exception);
            }
        }
        return state;
    }

    internal static LandSequencePosition ArrivalPosition() => Cna1979LandSequence.CreateTurn(1).Single(value =>
        value.OperationStage == 1 && value.PhaseId == LandPhaseIds.NavalConvoyArrival && value.SegmentId is null);
}
