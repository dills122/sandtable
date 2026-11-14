using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Content;

// Synthetic replay laboratory; never a campaign event or persistence contract.
internal sealed record CampaignArrivalDraftCommand(
    int ContractVersion, string CommandId, long ExpectedVersion, string GroupId,
    LandSide Actor, int GameTurn, int OperationStage, string DestinationId);

internal sealed class CampaignArrivalDraftEvent
{
    public CampaignArrivalDraftEvent(int contractVersion, string identityHash,
        CampaignArrivalDraftCommand command, long resultVersion, IEnumerable<string> elementIds)
    {
        ArgumentNullException.ThrowIfNull(elementIds);
        ContractVersion = contractVersion;
        IdentityHash = identityHash;
        Command = command;
        ResultVersion = resultVersion;
        ElementIds = Array.AsReadOnly(elementIds.ToArray());
    }

    public int ContractVersion { get; }
    public string IdentityHash { get; }
    public CampaignArrivalDraftCommand Command { get; }
    public long ResultVersion { get; }
    public IReadOnlyList<string> ElementIds { get; }
}

internal sealed class CampaignArrivalReplayDraft
{
    private readonly Dictionary<string, CampaignArrivalDraftEvent> receipts;
    private readonly HashSet<string> arrived;

    private CampaignArrivalReplayDraft(CampaignArrivalDraftIdentity identity, long version,
        CampaignWorldSnapshot world, Dictionary<string, CampaignArrivalDraftEvent> receipts, HashSet<string> arrived)
    {
        Identity = identity;
        Version = version;
        World = world;
        this.receipts = receipts;
        this.arrived = arrived;
    }

    public CampaignArrivalDraftIdentity Identity { get; }
    public long Version { get; }
    public CampaignWorldSnapshot World { get; }

    public static CampaignArrivalReplayDraft Create(CampaignArrivalDraftIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        return new CampaignArrivalReplayDraft(identity, 0,
            CampaignWorldFactory.CreateCandidateInitial(identity),
            new Dictionary<string, CampaignArrivalDraftEvent>(StringComparer.Ordinal), new HashSet<string>(StringComparer.Ordinal));
    }

    public (CampaignArrivalReplayDraft State, CampaignArrivalDraftEvent Event, bool IsRetry) Decide(CampaignArrivalDraftCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.ContractVersion != 1)
            throw new ArgumentException("Unsupported command version.", nameof(command));
        ContentContractGuards.RequireStableId(command.CommandId, nameof(command));
        if (receipts.TryGetValue(command.CommandId, out var prior))
        {
            if (prior.Command != command)
                throw new ArgumentException("Command ID is already bound to another command.", nameof(command));
            return (this, prior, true);
        }
        if (command.ExpectedVersion != Version || arrived.Contains(command.GroupId))
            throw new ArgumentException("Stale command or already arrived group.", nameof(command));
        if (Identity.Groups.Any(group => !arrived.Contains(group.GroupId)
            && (group.GameTurn < command.GameTurn || group.GameTurn == command.GameTurn && group.OperationStage < command.OperationStage)))
            throw new ArgumentException("An earlier arrival group is still pending.", nameof(command));
        var contribution = CampaignDeferredWorldDraft.Create(Identity, World, command.GroupId, command.Actor,
            command.GameTurn, command.OperationStage, command.DestinationId);
        var nextWorld = new CampaignWorldSnapshot(CampaignWorldSnapshot.CurrentContractVersion,
            World.Elements.Concat(contribution.Elements).ToArray(), World.Representations.Concat(contribution.Representations).ToArray());
        var nextVersion = checked(Version + 1);
        var recorded = new CampaignArrivalDraftEvent(1, Identity.Hash, command, nextVersion,
            contribution.Elements.Select(element => element.ElementId));
        var nextReceipts = new Dictionary<string, CampaignArrivalDraftEvent>(receipts, StringComparer.Ordinal) { [command.CommandId] = recorded };
        var nextArrived = new HashSet<string>(arrived, StringComparer.Ordinal) { command.GroupId };
        return (new CampaignArrivalReplayDraft(Identity, nextVersion, nextWorld, nextReceipts, nextArrived), recorded, false);
    }

    public static CampaignArrivalReplayDraft Replay(CampaignArrivalDraftIdentity identity, IEnumerable<CampaignArrivalDraftEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        var state = Create(identity);
        foreach (var recorded in events)
        {
            if (recorded is null || recorded.ContractVersion != 1 || recorded.Command is null || recorded.IdentityHash != identity.Hash)
                throw new InvalidOperationException("Invalid arrival draft history.");
            try
            {
                var result = state.Decide(recorded.Command);
                if (result.IsRetry || result.Event.ResultVersion != recorded.ResultVersion
                    || !result.Event.ElementIds.SequenceEqual(recorded.ElementIds, StringComparer.Ordinal))
                    throw new InvalidOperationException("Invalid arrival draft history.");
                state = result.State;
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException("Invalid arrival draft history.", exception);
            }
        }
        return state;
    }
}
