using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignScheduledWorld6Command(int ContractVersion, string CampaignId, string SetupHash,
    string CreationHash, string CommandId, long ExpectedVersion, LandSide Actor, string DestinationId);

internal sealed record CampaignScheduledWorld6Event(int ContractVersion, string SetupHash,
    CampaignScheduledWorld6Command Command, long ResultVersion, string ElementId,
    string RepresentationId, string SuccessorPositionId);

internal sealed record CampaignScheduledWorld6Offer(string ElementId, string DestinationId);

// Synthetic successor cut only. Not admitted to the existing campaign event union.
internal sealed class CampaignScheduledWorld6Phase
{
    public const string SetupId = "rules-lab.scheduled-world6.v1";
    public const string PinnedSetupHash = "sha256:f1a99a29b552f2cdc87ea80ac6c35061a97663d7933bb54b97a19d4f3935f109";
    internal const string ReferenceSetupHash = "sha256:e6631e81ad8f97e39fd9d7eec93bad7fe2b39db4d2d3059ed94a02dd4093e7a3";
    private static readonly byte[] SetupBytes = CreateSetupBytes();
    private readonly CampaignScheduledWorld6Event? receipt;

    private CampaignScheduledWorld6Phase(string campaignId, string creationHash, long version, CampaignWorldSnapshotV6 world,
        LandSequencePosition position, CampaignScheduledWorld6Event? receipt)
    {
        CampaignId = campaignId;
        CreationHash = creationHash;
        Version = version;
        World = world;
        Position = position;
        this.receipt = receipt;
    }

    public static string SetupHash { get; } = ValidateSetupHash();
    public static byte[] CopySetupBytes() => SetupBytes.ToArray();
    public static bool IsSupportedSetup(ReadOnlySpan<byte> bytes, string expectedHash) =>
        expectedHash == SetupHash && bytes.SequenceEqual(SetupBytes);
    public string CampaignId { get; }
    public string CreationHash { get; }
    public long Version { get; }
    public CampaignWorldSnapshotV6 World { get; }
    public LandSequencePosition Position { get; }
    internal CampaignScheduledWorld6Event? Receipt => receipt;
    internal long BaseVersion => receipt?.Command.ExpectedVersion ?? Version;

    public IReadOnlyList<CampaignScheduledWorld6Offer> QueryOffers(LandSide side)
    {
        if (!Enum.IsDefined(side)) throw new ArgumentOutOfRangeException(nameof(side));
        return side == LandSide.Axis && receipt is null && Position == CampaignScheduledWorld6.ArrivalPosition()
            ? Array.AsReadOnly(new[] { new CampaignScheduledWorld6Offer("axis-infantry", "north-west") })
            : Array.Empty<CampaignScheduledWorld6Offer>();
    }

    public static CampaignScheduledWorld6Phase Start(CampaignScheduledWorld6PreambleEvents.VerifiedCut proof)
    {
        ArgumentNullException.ThrowIfNull(proof);
        var cut = proof.Cut;
        var admittedCreation = CampaignScheduledWorld6Creation.Replay(cut.Request, cut.Creation);
        ArgumentOutOfRangeException.ThrowIfLessThan(cut.Version, 1);
        if (cut.Position != CampaignScheduledWorld6.ArrivalPosition())
            throw new ArgumentException("The exact sequence-v4 arrival position is required.", nameof(proof));
        return new CampaignScheduledWorld6Phase(admittedCreation.CampaignId,
            CampaignScheduledWorld6CreatedCodec.CalculateHash(admittedCreation), cut.Version, admittedCreation.World, cut.Position, null);
    }

    public (CampaignScheduledWorld6Phase State, CampaignScheduledWorld6Event Event, bool IsRetry) Decide(CampaignScheduledWorld6Command command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.ContractVersion != 1 || command.CampaignId != CampaignId || command.SetupHash != SetupHash
            || command.CreationHash != CreationHash)
            throw new ArgumentException("Unsupported campaign, setup or command version.", nameof(command));
        ContentContractGuards.RequireStableId(command.CommandId, nameof(command));
        if (receipt is not null)
        {
            if (receipt.Command == command) return (this, receipt, true);
            throw new ArgumentException("The arrival has already completed.", nameof(command));
        }
        if (command.ExpectedVersion != Version)
            throw new ArgumentException("The arrival command is stale.", nameof(command));
        var world = CampaignScheduledWorld6.Arrive(World, command.Actor, Position, command.DestinationId);
        var successor = Cna1979LandSequenceV4.GetNext(Position);
        if (successor.PhaseId != LandPhaseIds.CommonwealthFleet || successor.SegmentId != LandSegmentIds.FleetAssignment)
            throw new InvalidOperationException("Unexpected arrival successor.");
        var representation = world.Representations.Single(value => value.BoundElementIds.Contains("axis-infantry", StringComparer.Ordinal));
        var recorded = new CampaignScheduledWorld6Event(1, SetupHash, command, checked(Version + 1),
            "axis-infantry", representation.RepresentationId, successor.PositionId);
        return (new CampaignScheduledWorld6Phase(CampaignId, CreationHash, recorded.ResultVersion, world, successor, recorded), recorded, false);
    }

    public static CampaignScheduledWorld6Phase Replay(CampaignScheduledWorld6PreambleEvents.VerifiedCut proof,
        IEnumerable<CampaignScheduledWorld6Event> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        var state = Start(proof);
        foreach (var recorded in events)
        {
            if (recorded is null || recorded.ContractVersion != 1 || recorded.SetupHash != SetupHash || recorded.Command is null)
                throw new InvalidOperationException("Invalid scheduled World6 history.");
            try
            {
                var result = state.Decide(recorded.Command);
                if (result.IsRetry || result.Event != recorded)
                    throw new InvalidOperationException("Invalid scheduled World6 history.");
                state = result.State;
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException("Invalid scheduled World6 history.", exception);
            }
        }
        return state;
    }

    private static byte[] CreateSetupBytes()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("format", "sandtable.synthetic-arrival-world6-setup.v1");
            writer.WriteString("setupId", SetupId);
            writer.WriteString("rulesetHash", Cna1979Ruleset.Manifest.Hash);
            writer.WriteString("contentHash", Cna1979SyntheticContentCatalog.ArtifactV6.Identity.Hash);
            writer.WriteString("preambleReferenceSetupHash", ReferenceSetupHash);
            writer.WriteString("initiativeOrder", "axis-first");
            writer.WriteString("scenarioId", "breakdown-truck-lab");
            writer.WriteString("deferredElementId", "axis-infantry");
            writer.WriteString("destinationId", "north-west");
            writer.WriteString("positionId", CampaignScheduledWorld6.ArrivalPosition().PositionId);
            writer.WriteString("otherArrivalObligations", "synthetic-none");
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    private static string ValidateSetupHash()
    {
        var hash = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SetupBytes));
        if (hash != PinnedSetupHash)
            throw new InvalidOperationException("The synthetic World6 setup no longer matches its pinned identity.");
        return hash;
    }
}
