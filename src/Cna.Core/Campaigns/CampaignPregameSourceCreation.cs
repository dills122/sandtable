using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Content;
using Cna.Core.Randomness;

namespace Cna.Core.Campaigns;

/// <summary>Bounded, provisional source-backed pre-game planning; not full campaign admission.</summary>
public static class CampaignPregameSourceCreation
{
    public const int RequestVersion = 3;
    public const string SetupId = "cna.1979.graziani.pregame-supply.v1";
    public const string PolicyId = "graziani.pregame.gt2-supply.sep-ii.maximum.first-draw.standard-letter.v1";
    public const string SupportedContentHash = "sha256:ae700ad1664ae9706e3f7dce6c58e13ef4d025034eacd6a2bfba664f3c2ced3d";
    public const string PositionId = "land.pregame-axis-gt2-supply";

    public static string CalculateSetupHash(ContentScheduledPackIdentity identity,
        CampaignSourceInitialization initialization)
    {
        var sourceHash = CampaignSourceCreation.CalculateSetupHash(identity,
            Cna1979GrazianiCairoSourceExcerpt.ScenarioId, initialization);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("setupId", SetupId);
            writer.WriteString("policyId", PolicyId);
            writer.WriteString("sourceSetupHash", sourceHash);
            writer.WriteString("planningDate", "1940-september-ii");
            writer.WriteNumber("targetGameTurn", 2);
            writer.WriteString("randomOrdering", "first-recorded-pregame-draw");
            WriteSources(writer);
            WriteLaneRoutes(writer);
            writer.WriteEndObject();
        }
        return Digest(stream.ToArray());
    }

    internal static string Digest(byte[] bytes) => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));

    internal static void WriteSources(Utf8JsonWriter writer)
    {
        writer.WriteStartArray("sources");
        writer.WriteStringValue(Cna1979GrazianiSourceFacts.LandRulesSourceId + ":32.43");
        writer.WriteStringValue(Cna1979GrazianiSourceFacts.LandRulesSourceId + ":32.44");
        writer.WriteStringValue(Cna1979GrazianiSourceFacts.LandRulesSourceId + ":32.61");
        writer.WriteStringValue(Cna1979GrazianiSourceFacts.LandRulesSourceId + ":32.62");
        writer.WriteStringValue(Cna1979GrazianiSourceFacts.AxisChartsSourceId + ":32.46");
        writer.WriteStringValue(Cna1979GrazianiSourceFacts.AxisChartsSourceId + ":56.4");
        writer.WriteStringValue(Cna1979GrazianiSourceFacts.ScenariosSourceId + ":60.37");
        writer.WriteStringValue(Cna1979GrazianiSourceFacts.AirLogisticsSourceId + ":56.18");
        writer.WriteStringValue(Cna1979GrazianiSourceFacts.ErrataSourceId + ":56.25");
        writer.WriteStringValue(Cna1979GrazianiSourceFacts.ErrataSourceId + ":56.29");
        writer.WriteEndArray();
    }

    internal static (string Origin, string Destination) LaneRoute(int lane) => lane switch
    {
        2 => ("West Sicily", "Tripoli"),
        3 => ("Italy", "Benghazi"),
        6 => ("Italy", "Tobruk"),
        _ => throw new ArgumentOutOfRangeException(nameof(lane)),
    };

    private static void WriteLaneRoutes(Utf8JsonWriter writer)
    {
        writer.WriteStartArray("laneRoutes");
        foreach (var lane in new[] { 2, 3, 6 })
        {
            var route = LaneRoute(lane);
            writer.WriteStartObject();
            writer.WriteNumber("lane", lane);
            writer.WriteString("origin", route.Origin);
            writer.WriteString("destination", route.Destination);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }
}

internal sealed class CampaignPregameSourceState
{
    private static readonly int[] Lanes = [2, 3, 6];
    private static readonly int[] SupplyByDie = [0, 1, 1, 2, 2, 3];
    private readonly int[] allocations;

    private CampaignPregameSourceState(CampaignCreationRequest request, CampaignSourceCreationResult source,
        long version, RandomStreamState random, int? die, int? maximum, int[] allocations)
    {
        Request = request;
        Source = source;
        StateVersion = version;
        RandomState = random;
        Die = die;
        MaximumSupply = maximum;
        this.allocations = allocations.ToArray();
    }

    internal CampaignCreationRequest Request { get; }
    internal CampaignSourceCreationResult Source { get; }
    internal long StateVersion { get; }
    internal RandomStreamState RandomState { get; }
    internal int? Die { get; }
    internal int? MaximumSupply { get; }
    internal int Allocation(int laneIndex, int stage) => allocations[laneIndex * 3 + stage - 1];
    internal IEnumerable<(int Lane, int Stage, int Units)> Allocations()
    {
        for (var i = 0; i < Lanes.Length; i++)
        {
            for (var stage = 1; stage <= 3; stage++)
            {
                if (Allocation(i, stage) > 0)
                    yield return (Lanes[i], stage, Allocation(i, stage));
            }
        }
    }

    internal static CampaignPregameSourceState Create(CampaignCreationRequest request,
        ContentSourceScheduledCombatPackArtifact artifact, CampaignSourceInitialization initialization)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(initialization);
        if (request.ContractVersion != CampaignPregameSourceCreation.RequestVersion
            || request.SetupId != CampaignPregameSourceCreation.SetupId
            || request.ScenarioId != Cna1979GrazianiCairoSourceExcerpt.ScenarioId
            || artifact.Identity.Hash != CampaignPregameSourceCreation.SupportedContentHash
            || request.SetupHash != CampaignPregameSourceCreation.CalculateSetupHash(artifact.Identity, initialization))
            throw new ArgumentException("Unsupported source content or provisional pre-game setup identity.");
        var baseHash = CampaignSourceCreation.CalculateSetupHash(artifact.Identity, request.ScenarioId, initialization);
        var source = CampaignSourceCreation.Create(LegacyRequest(request, baseHash), artifact, initialization);
        return new(request, source, 1, SandtableRandom.Create(request.Seed), null, null, new int[9]);
    }

    internal static CampaignPregameSourceState ReplayGenesis(CampaignCreationRequest request,
        ReadOnlyMemory<byte> creation, ReadOnlyMemory<byte> snapshot)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var document = JsonDocument.Parse(creation);
        var root = document.RootElement;
        CampaignSnapshotSerializer.RequireProperties(root, "contractVersion", "eventType", "campaignId", "policyId", "sources",
            "sourceCreationBase64", "sourceSnapshotBase64");
        if (root.GetProperty("contractVersion").GetInt32() != CampaignPregameSourceCreation.RequestVersion
            || root.GetProperty("eventType").GetString() != "pregame-source-created"
            || root.GetProperty("campaignId").GetString() != request.CampaignId
            || root.GetProperty("policyId").GetString() != CampaignPregameSourceCreation.PolicyId)
            throw new JsonException("Invalid pre-game source genesis.");
        var sourceEvent = root.GetProperty("sourceCreationBase64").GetBytesFromBase64();
        using var nested = JsonDocument.Parse(sourceEvent);
        var baseHash = nested.RootElement.GetProperty("setupHash").GetString()!;
        var source = CampaignSourceCreation.Replay(LegacyRequest(request, baseHash), sourceEvent,
            root.GetProperty("sourceSnapshotBase64").GetBytesFromBase64());
        var init = source.Session.InitialWorld.Initialization;
        var state = Create(request, source.Session.Content, new(init.CapabilityPointsExpended, init.CohesionLevel));
        if (!creation.Span.SequenceEqual(state.SerializeGenesis()) || !snapshot.Span.SequenceEqual(state.SerializeCheckpoint()))
            throw new InvalidOperationException("Pre-game genesis differs from retained canonical evidence.");
        return state;
    }

    internal byte[] SerializeGenesis()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CampaignPregameSourceCreation.RequestVersion);
            writer.WriteString("eventType", "pregame-source-created");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteString("policyId", CampaignPregameSourceCreation.PolicyId);
            CampaignPregameSourceCreation.WriteSources(writer);
            writer.WriteBase64String("sourceCreationBase64", Source.CreationEventBytes);
            writer.WriteBase64String("sourceSnapshotBase64", Source.InitialSnapshotBytes);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal byte[] SerializeCheckpoint()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CampaignPregameSourceCreation.RequestVersion);
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("stateVersion", StateVersion);
            writer.WriteString("rulesetHash", Request.RulesetHash);
            writer.WriteString("contentHash", Request.ContentHash);
            writer.WriteString("setupHash", Request.SetupHash);
            writer.WriteString("positionId", CampaignPregameSourceCreation.PositionId);
            writer.WriteString("randomAlgorithm", RandomState.AlgorithmId);
            writer.WriteNumber("randomSeed", RandomState.Seed);
            writer.WriteNumber("randomCursor", RandomState.NextByteCursor);
            if (Die is { } die) writer.WriteNumber("die", die);
            else writer.WriteNull("die");
            if (MaximumSupply is { } max) writer.WriteNumber("maximumSupply", max);
            else writer.WriteNull("maximumSupply");
            writer.WriteStartArray("allocations");
            foreach (var allocation in Allocations())
            {
                writer.WriteStartObject();
                writer.WriteNumber("lane", allocation.Lane);
                writer.WriteNumber("operationStage", allocation.Stage);
                writer.WriteNumber("units", allocation.Units);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteBase64String("sourceSnapshotBase64", Source.InitialSnapshotBytes);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal IReadOnlyList<CampaignActionCandidate> Candidates()
    {
        if (Die is null) return [new PregameAxisSupplyRollAction()];
        var result = new List<CampaignActionCandidate>();
        var total = allocations.Sum();
        for (var i = 0; i < Lanes.Length; i++)
        {
            for (var stage = 1; stage <= 3; stage++)
            {
                if (total < MaximumSupply!.Value && Allocation(i, stage) < 3)
                    result.Add(new PregameAxisSupplyAllocationAction(Lanes[i], stage, 1));
                if (Allocation(i, stage) > 0)
                    result.Add(new PregameAxisSupplyAllocationAction(Lanes[i], stage, -1));
            }
        }
        return result;
    }

    internal (CampaignPregameSourceState State, byte[] Event) Apply(CampaignActionCandidate action)
    {
        if (!Candidates().Any(value => value.ActionId == action.ActionId))
            throw new InvalidOperationException("Pre-game choice is unavailable.");
        var random = RandomState;
        var die = Die;
        var maximum = MaximumSupply;
        var nextAllocations = allocations.ToArray();
        if (action is PregameAxisSupplyRollAction)
        {
            var roll = SandtableRandom.RollD6(random);
            random = roll.State;
            die = roll.Value;
            maximum = SupplyByDie[roll.Value - 1];
        }
        else if (action is PregameAxisSupplyAllocationAction allocation)
            nextAllocations[Array.IndexOf(Lanes, allocation.Lane) * 3 + allocation.Stage - 1] += allocation.Delta;
        else throw new InvalidOperationException("Unsupported pre-game choice.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CampaignPregameSourceCreation.RequestVersion);
            writer.WriteString("eventType", action is PregameAxisSupplyRollAction
                ? "pregame-axis-gt2-supply-rolled" : "pregame-axis-gt2-supply-adjusted");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("policyId", CampaignPregameSourceCreation.PolicyId);
            writer.WriteString("actionId", action.ActionId);
            CampaignPregameSourceCreation.WriteSources(writer);
            writer.WriteNumber("randomCursorBefore", RandomState.NextByteCursor);
            writer.WriteNumber("randomCursorAfter", random.NextByteCursor);
            if (action is PregameAxisSupplyRollAction)
            {
                writer.WriteNumber("die", die!.Value);
                writer.WriteNumber("maximumSupply", maximum!.Value);
            }
            else
            {
                var allocation = (PregameAxisSupplyAllocationAction)action;
                writer.WriteNumber("lane", allocation.Lane);
                writer.WriteNumber("operationStage", allocation.Stage);
                writer.WriteNumber("delta", allocation.Delta);
            }
            writer.WriteEndObject();
        }
        return (new(Request, Source, checked(StateVersion + 1), random, die, maximum, nextAllocations), stream.ToArray());
    }

    private static CampaignCreationRequest LegacyRequest(CampaignCreationRequest request, string sourceSetupHash) => new(
        CampaignSourceCreation.RequestVersion, request.CampaignId, request.RulesetHash, request.Seed,
        CampaignSourceCreation.SetupId, sourceSetupHash, request.ContentPackId, request.ContentHash, request.ScenarioId);
}
