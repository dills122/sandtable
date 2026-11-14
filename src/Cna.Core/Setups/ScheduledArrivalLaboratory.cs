using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Setups;

// Synthetic-only transition model. This is not CampaignEngine authority or a wire contract.
internal enum ArrivalLabActor { System, Axis, Commonwealth }
internal enum ArrivalLabCommandKind { ResolveLand, ResolveOther, Complete }

internal sealed record ArrivalLabCommand(
    int ContractVersion,
    string CommandId,
    long ExpectedVersion,
    int GameTurn,
    int OperationStage,
    ArrivalLabCommandKind Kind,
    string? SubjectId = null,
    string? DestinationId = null);

internal sealed record ArrivalLabEvent(
    int ContractVersion,
    string FixtureHash,
    ArrivalLabActor Actor,
    ArrivalLabCommand Command,
    long ResultVersion,
    IReadOnlyList<string> UnitIds);

internal sealed class ArrivalLabFixture
{
    private readonly IReadOnlyDictionary<string, ScheduledArrivalGroup> groups;
    private readonly ReadOnlyDictionary<string, IReadOnlyList<string>> destinations;
    private readonly IReadOnlyDictionary<string, string> initialUnits;
    private readonly IReadOnlyList<string> otherObligations;

    public ArrivalLabFixture(
        string fixtureId,
        int gameTurn,
        int operationStage,
        IEnumerable<ScheduledArrivalGroup> groups,
        IReadOnlyDictionary<string, IReadOnlyList<string>> destinations,
        IEnumerable<string> otherObligations,
        IReadOnlyDictionary<string, string> initialUnits)
    {
        FixtureId = ContentContractGuards.RequireStableId(fixtureId, nameof(fixtureId));
        ArgumentOutOfRangeException.ThrowIfLessThan(gameTurn, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(gameTurn, 111);
        ArgumentOutOfRangeException.ThrowIfLessThan(operationStage, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(operationStage, 3);
        ArgumentNullException.ThrowIfNull(groups);
        ArgumentNullException.ThrowIfNull(destinations);
        ArgumentNullException.ThrowIfNull(otherObligations);
        ArgumentNullException.ThrowIfNull(initialUnits);
        GameTurn = gameTurn;
        OperationStage = operationStage;

        var groupCopy = groups.ToArray();
        _ = new ScheduledArrivalIndex(groupCopy);
        if (groupCopy.Any(group => group.GameTurn != gameTurn || group.OperationStage != operationStage))
            throw new ArgumentException("Groups must belong to the fixture slot.", nameof(groups));
        if (groupCopy.SelectMany(group => group.Sources)
            .Any(source => !IsWellFormedUtf16(source.SourceId) || !IsWellFormedUtf16(source.Locator)))
            throw new ArgumentException("Source references must contain valid Unicode.", nameof(groups));
        this.groups = new ReadOnlyDictionary<string, ScheduledArrivalGroup>(
            groupCopy.ToDictionary(group => group.GroupId, StringComparer.Ordinal));
        if (!this.groups.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(destinations.Keys))
            throw new ArgumentException("Every group needs exactly one destination list.", nameof(destinations));
        var allowed = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (var pair in destinations)
        {
            ArgumentNullException.ThrowIfNull(pair.Value);
            var values = pair.Value.Select(value => ContentContractGuards.RequireStableId(value, nameof(destinations)))
                .Order(StringComparer.Ordinal).ToArray();
            if (values.Length == 0 || values.Distinct(StringComparer.Ordinal).Count() != values.Length)
                throw new ArgumentException("Destination lists must be nonempty and distinct.", nameof(destinations));
            allowed.Add(pair.Key, Array.AsReadOnly(values));
        }
        this.destinations = new ReadOnlyDictionary<string, IReadOnlyList<string>>(allowed);

        var other = otherObligations.Select(value => ContentContractGuards.RequireStableId(value, nameof(otherObligations)))
            .Order(StringComparer.Ordinal).ToArray();
        if (other.Distinct(StringComparer.Ordinal).Count() != other.Length)
            throw new ArgumentException("Other obligations must be distinct.", nameof(otherObligations));
        this.otherObligations = Array.AsReadOnly(other);
        var units = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in initialUnits)
            units.Add(ContentContractGuards.RequireStableId(pair.Key, nameof(initialUnits)),
                ContentContractGuards.RequireStableId(pair.Value, nameof(initialUnits)));
        if (groupCopy.SelectMany(group => group.UnitIds).Any(units.ContainsKey))
            throw new ArgumentException("Arriving units must not already be present.", nameof(initialUnits));
        this.initialUnits = new ReadOnlyDictionary<string, string>(units);
        FixtureHash = CalculateHash();
    }

    public string FixtureId { get; }
    public int GameTurn { get; }
    public int OperationStage { get; }
    public string FixtureHash { get; }
    internal IReadOnlyDictionary<string, ScheduledArrivalGroup> Groups => groups;
    internal IReadOnlyDictionary<string, IReadOnlyList<string>> Destinations => destinations;
    internal IReadOnlyList<string> OtherObligations => otherObligations;
    internal IReadOnlyDictionary<string, string> InitialUnits => initialUnits;

    private static bool IsWellFormedUtf16(string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            if (char.IsHighSurrogate(value[index]))
            {
                if (++index >= value.Length || !char.IsLowSurrogate(value[index])) return false;
            }
            else if (char.IsLowSurrogate(value[index])) return false;
        }
        return true;
    }

    private string CalculateHash()
    {
        using var stream = new MemoryStream();
        using (var json = new Utf8JsonWriter(stream))
        {
            json.WriteStartObject();
            json.WriteString("format", "sandtable.arrival-lab-fixture.v1");
            json.WriteString("id", FixtureId);
            json.WriteNumber("turn", GameTurn);
            json.WriteNumber("stage", OperationStage);
            json.WriteStartArray("groups");
            foreach (var group in groups.Values.OrderBy(value => value.GroupId, StringComparer.Ordinal))
            {
                json.WriteStartObject();
                json.WriteString("id", group.GroupId);
                json.WriteNumber("owner", (int)group.Owner);
                json.WriteStartArray("units");
                foreach (var id in group.UnitIds) json.WriteStringValue(id);
                json.WriteEndArray();
                json.WriteStartArray("sources");
                foreach (var source in group.Sources)
                {
                    json.WriteStartArray();
                    json.WriteStringValue(source.SourceId);
                    json.WriteStringValue(source.Locator);
                    json.WriteEndArray();
                }
                json.WriteEndArray();
                json.WriteStartArray("destinations");
                foreach (var id in destinations[group.GroupId]) json.WriteStringValue(id);
                json.WriteEndArray();
                json.WriteEndObject();
            }
            json.WriteEndArray();
            json.WriteStartArray("other");
            foreach (var id in otherObligations) json.WriteStringValue(id);
            json.WriteEndArray();
            json.WriteStartArray("initialUnits");
            foreach (var pair in initialUnits.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                json.WriteStartArray();
                json.WriteStringValue(pair.Key);
                json.WriteStringValue(pair.Value);
                json.WriteEndArray();
            }
            json.WriteEndArray();
            json.WriteEndObject();
        }
        return "sha256:" + Convert.ToHexStringLower(SHA256.HashData(stream.ToArray()));
    }
}

internal sealed class ArrivalLabState
{
    private ArrivalLabState(
        ArrivalLabFixture fixture,
        long version,
        bool complete,
        IDictionary<string, string> units,
        IEnumerable<string> completedGroups,
        IEnumerable<string> completedOther,
        IDictionary<string, ArrivalLabEvent> receipts)
    {
        Fixture = fixture;
        Version = version;
        Complete = complete;
        Units = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(units, StringComparer.Ordinal));
        CompletedGroups = Array.AsReadOnly(completedGroups.Order(StringComparer.Ordinal).ToArray());
        CompletedOther = Array.AsReadOnly(completedOther.Order(StringComparer.Ordinal).ToArray());
        Receipts = new ReadOnlyDictionary<string, ArrivalLabEvent>(new Dictionary<string, ArrivalLabEvent>(receipts, StringComparer.Ordinal));
    }

    public ArrivalLabFixture Fixture { get; }
    public long Version { get; }
    public bool Complete { get; }
    public IReadOnlyDictionary<string, string> Units { get; }
    public IReadOnlyList<string> CompletedGroups { get; }
    public IReadOnlyList<string> CompletedOther { get; }
    private ReadOnlyDictionary<string, ArrivalLabEvent> Receipts { get; }

    public static ArrivalLabState Create(ArrivalLabFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        return new ArrivalLabState(fixture, 0, false,
            new Dictionary<string, string>(fixture.InitialUnits, StringComparer.Ordinal), [], [],
            new Dictionary<string, ArrivalLabEvent>(StringComparer.Ordinal));
    }

    public ArrivalLabResult Decide(ArrivalLabActor actor, ArrivalLabCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!Enum.IsDefined(actor) || command.ContractVersion != 1 || !IsStableId(command.CommandId))
            return ArrivalLabResult.Rejected(this);
        if (Receipts.TryGetValue(command.CommandId, out var prior))
            return prior.Actor == actor && prior.Command == command
                ? new ArrivalLabResult(this, prior, true)
                : ArrivalLabResult.Rejected(this);
        if (Complete || command.ExpectedVersion != Version || command.GameTurn != Fixture.GameTurn
            || command.OperationStage != Fixture.OperationStage || !Enum.IsDefined(command.Kind))
            return ArrivalLabResult.Rejected(this);

        var units = new Dictionary<string, string>(Units, StringComparer.Ordinal);
        var groups = CompletedGroups.ToHashSet(StringComparer.Ordinal);
        var other = CompletedOther.ToHashSet(StringComparer.Ordinal);
        IReadOnlyList<string> arrivingUnits = Array.Empty<string>();
        var complete = false;
        switch (command.Kind)
        {
            case ArrivalLabCommandKind.ResolveLand:
                if (command.SubjectId is null || command.DestinationId is null
                    || !Fixture.Groups.TryGetValue(command.SubjectId, out var group)
                    || actor != OwnerActor(group.Owner)
                    || groups.Contains(group.GroupId)
                    || !Fixture.Destinations[group.GroupId].Contains(command.DestinationId, StringComparer.Ordinal)
                    || group.UnitIds.Any(units.ContainsKey))
                    return ArrivalLabResult.Rejected(this);
                foreach (var id in group.UnitIds) units.Add(id, command.DestinationId);
                groups.Add(group.GroupId);
                arrivingUnits = group.UnitIds;
                break;
            case ArrivalLabCommandKind.ResolveOther:
                if (actor != ArrivalLabActor.System || command.SubjectId is null || command.DestinationId is not null
                    || !Fixture.OtherObligations.Contains(command.SubjectId, StringComparer.Ordinal)
                    || !other.Add(command.SubjectId))
                    return ArrivalLabResult.Rejected(this);
                break;
            case ArrivalLabCommandKind.Complete:
                if (actor != ArrivalLabActor.System || command.SubjectId is not null || command.DestinationId is not null
                    || groups.Count != Fixture.Groups.Count || other.Count != Fixture.OtherObligations.Count)
                    return ArrivalLabResult.Rejected(this);
                complete = true;
                break;
            default:
                return ArrivalLabResult.Rejected(this);
        }

        var nextVersion = checked(Version + 1);
        var recorded = new ArrivalLabEvent(1, Fixture.FixtureHash, actor, command, nextVersion,
            Array.AsReadOnly(arrivingUnits.ToArray()));
        var receipts = new Dictionary<string, ArrivalLabEvent>(Receipts, StringComparer.Ordinal) { [command.CommandId] = recorded };
        var next = new ArrivalLabState(Fixture, nextVersion, complete, units, groups, other, receipts);
        return new ArrivalLabResult(next, recorded, false);
    }

    public static ArrivalLabState Replay(ArrivalLabFixture fixture, IEnumerable<ArrivalLabEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        var state = Create(fixture);
        foreach (var recorded in events)
        {
            if (recorded is null || recorded.ContractVersion != 1 || recorded.UnitIds is null || recorded.Command is null
                || !string.Equals(recorded.FixtureHash, fixture.FixtureHash, StringComparison.Ordinal))
                throw new InvalidOperationException("Invalid arrival lab history.");
            var result = state.Decide(recorded.Actor, recorded.Command);
            var expected = result.Event;
            if (expected is null || result.IsRetry || expected.ResultVersion != recorded.ResultVersion
                || !expected.UnitIds.SequenceEqual(recorded.UnitIds, StringComparer.Ordinal))
                throw new InvalidOperationException("Invalid arrival lab history.");
            state = result.State;
        }
        return state;
    }

    private static ArrivalLabActor OwnerActor(LandSide side) => side switch
    {
        LandSide.Axis => ArrivalLabActor.Axis,
        LandSide.Commonwealth => ArrivalLabActor.Commonwealth,
        _ => throw new ArgumentOutOfRangeException(nameof(side)),
    };

    private static bool IsStableId(string? value)
    {
        try { _ = ContentContractGuards.RequireStableId(value!, nameof(value)); return true; }
        catch (ArgumentException) { return false; }
    }
}

internal sealed record ArrivalLabResult(ArrivalLabState State, ArrivalLabEvent? Event, bool IsRetry)
{
    public static ArrivalLabResult Rejected(ArrivalLabState state) => new(state, null, false);
}
