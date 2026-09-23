using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Owned creation-rooted predecessor, never a caller-supplied terminal World.</summary>
internal sealed class CombatInheritedReserveReleaseSource
{
    private readonly byte[] created;
    private readonly byte[][] preamble, weather, stage, reserve, cycle;
    public CombatInheritedReserveReleaseSource(CampaignCombatCreationRequest request, byte[] created,
        IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weather, IReadOnlyList<byte[]> stage,
        IReadOnlyList<byte[]> reserve, IReadOnlyList<byte[]> cycle)
    {
        ArgumentNullException.ThrowIfNull(request); Request = request;
        this.created = CampaignCombatInheritedReserveCycleCodec.CopyRecord(created);
        this.preamble = Copy(preamble, 4); this.weather = Copy(weather, 1); this.stage = Copy(stage, 4);
        this.reserve = Copy(reserve, 2); this.cycle = Copy(cycle, 10);
    }
    public CampaignCombatCreationRequest Request { get; }
    internal CampaignCombatInheritedReserveCycle.State Replay() => CampaignCombatInheritedReserveCycle.Replay(
        Request, created, preamble, weather, stage, reserve, cycle);
    private static byte[][] Copy(IReadOnlyList<byte[]> records, int maximum)
    {
        ArgumentNullException.ThrowIfNull(records);
        if (records.Count > maximum) throw new JsonException("Inherited Release predecessor exceeds capacity.");
        return records.Select(CampaignCombatInheritedReserveCycleCodec.CopyRecord).ToArray();
    }
}

/// <summary>Native Release following authenticated held-I Combat history; no cycle advancement.</summary>
internal static class CampaignCombatInheritedReserveRelease
{
    internal sealed record Basis(CampaignCombatInheritedReserveCycle.State Predecessor, CombatReleaseBase ReleaseBase, string PredecessorHash);
    internal sealed record Progress(string EventType, string ReceiptId, string EventHash);
    internal sealed class Projection(Basis basis, CombatReleaseState release, CampaignWorldSnapshotV7 world)
    {
        public Basis Base { get; } = basis;
        public CombatReleaseState Release { get; } = release;
        public CampaignWorldSnapshotV7 World { get; } = world;
        public IReadOnlyList<Progress> Progress { get; } = Array.AsReadOnly(release.Dispositions.Select(d =>
            new Progress("reserve-unit-disposition-recorded", d.ReceiptId,
                release.Receipts.Single(r => r.ReceiptId == d.ReceiptId).EventHash)).ToArray());
        public bool Closed => Release.Status == "completed";
    }

    public static Projection Replay(CombatInheritedReserveReleaseSource source,
        IReadOnlyList<CombatReleaseInput> inputs, IReadOnlyList<byte[]> events)
    {
        var (trusted, retained) = Capture(inputs, events);
        var basis = Derive(source);
        var release = CampaignCombatReserveRelease.Replay(basis.ReleaseBase, source.Request, trusted, retained);
        return new(basis, release, ProjectWorld(basis.Predecessor.Base.Predecessor.World, release));
    }

    public static CombatReleaseResult Apply(CombatInheritedReserveReleaseSource source,
        IReadOnlyList<CombatReleaseInput> inputs, IReadOnlyList<byte[]> events, CombatReleaseInput input, byte[]? cachedControl = null)
    {
        var cache = cachedControl is null ? null : CampaignCombatInheritedReserveCycleCodec.CopyRecord(cachedControl);
        var (trusted, retained) = Capture(inputs, events);
        ValidateInput(input);
        var projection = Replay(source, trusted, retained);
        if (cache is not null && !cache.AsSpan().SequenceEqual(SerializeControl(projection, source.Request)))
            throw new JsonException("Inherited Release cache differs from replay.");
        var result = CampaignCombatReserveRelease.Apply(projection.Base.ReleaseBase, source.Request, trusted, retained, input);
        if (result.Disposition != CombatStepsDisposition.Duplicate) return result;
        var index = projection.Release.Receipts.ToList().FindIndex(r => r.ReceiptId == result.ReceiptId);
        if (index < 0) throw new JsonException("Inherited Release retry receipt missing from retained history.");
        return new(result.State, result.Disposition, retained[index], result.ReceiptId);
    }

    public static Basis ReadBase(ReadOnlySpan<byte> bytes, CombatInheritedReserveReleaseSource source)
    {
        CheckBounds(bytes); var basis = Derive(source);
        if (!bytes.SequenceEqual(SerializeBase(basis, source.Request))) throw new JsonException("Inherited Release base differs from actual predecessor.");
        return basis;
    }

    public static Projection ReadControl(ReadOnlySpan<byte> bytes, CombatInheritedReserveReleaseSource source,
        IReadOnlyList<CombatReleaseInput> inputs, IReadOnlyList<byte[]> events)
    {
        CheckBounds(bytes); var projection = Replay(source, inputs, events);
        if (!bytes.SequenceEqual(SerializeControl(projection, source.Request))) throw new JsonException("Inherited Release control differs from replay.");
        return projection;
    }

    private static Basis Derive(CombatInheritedReserveReleaseSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var predecessor = source.Replay();
        if (!predecessor.Closed || predecessor.StateVersion != 22 || predecessor.Receipts.Count != 10 ||
            predecessor.Position.SegmentId != LandSegmentIds.ReserveRelease)
            throw new JsonException("Inherited Release requires complete actual held-I traversal.");
        var opening = predecessor.Base; var member = opening.Predecessor.Members[0]; var scope = member.History.Scope;
        var native = new CombatReleaseBase(1, "inherited-reserve-cycle", opening.Cycle!, opening.Predecessor.FirstActingSide,
            predecessor.Position.PositionId, predecessor.StateVersion, predecessor.Prefix, predecessor.Receipts[^1].ReceiptId,
            CampaignOpeningPreambleCodec.Hash(CampaignCombatReserveCodec.WriteWorld(opening.Predecessor)),
            opening.Predecessor.Stage.Weather.RandomState, null,
            [new(member.Unit, member.Status, member.BaseCpa, member.SpentCp,
                new(new(scope.GameTurn, scope.OperationStage, scope.PlayerPhaseSlot, scope.ActingSide), member.History.DesignationReceiptId))], []);
        _ = CampaignCombatReserveReleaseCodec.SerializeBase(native, source.Request);
        return new(predecessor, native, CampaignOpeningPreambleCodec.Hash(CampaignCombatInheritedReserveCycleCodec.SerializeControl(predecessor)));
    }

    private static (CombatReleaseInput[] Inputs, byte[][] Events) Capture(IReadOnlyList<CombatReleaseInput> inputs, IReadOnlyList<byte[]> events)
    {
        ArgumentNullException.ThrowIfNull(inputs); ArgumentNullException.ThrowIfNull(events);
        if (inputs.Count != events.Count || events.Count > 3) throw new JsonException("Inherited Release suffix capacity/count mismatch.");
        var trusted = inputs.ToArray(); var retained = events.Select(CampaignCombatInheritedReserveCycleCodec.CopyRecord).ToArray();
        foreach (var input in trusted) ValidateInput(input);
        foreach (var bytes in retained) CampaignCombatReserveReleaseCodec.ValidateSyntax(bytes, "event");
        return (trusted, retained);
    }
    private static void ValidateInput(CombatReleaseInput input)
    {
        _ = CampaignCombatReserveReleaseCodec.SerializeInput(input);
        if (input.Command.Kind is not ("open" or "choose" or "complete" or "expire" or "unavailable" or "fallback-step") ||
            input.Command.Kind == "choose" && input.Command.Choice != "release-I")
            throw new JsonException("Inherited Release admits only release-I or deterministic first-I fallback.");
    }

    private static CampaignWorldSnapshotV7 ProjectWorld(CampaignWorldSnapshotV7 world, CombatReleaseState release)
    {
        var member = release.Members.Single();
        return new(7, world.CreationBinding, world.Elements.Select(e => e.ElementId == member.Unit.ElementId
            ? new CampaignElementStateV6(e.ElementId, e.CurrentLocationId, member.Status, e.OperationalState,
                e.Components, e.SourceParentFormationId, e.CurrentParentFormationId, e.Ammunition, e.Readiness) : e),
            world.Representations, world.BrokenVehicleLots, world.CohesionCauses, world.Relationships, world.CustodyLots,
            world.Guards, world.ReplacementEntitlements, world.FutureObligations, world.Settlements);
    }

    public static byte[] SerializeBase(Basis basis, CampaignCombatCreationRequest request) => Bytes(w =>
    {
        w.WriteStartObject(); w.WriteNumber("contractVersion", 1); w.WriteString("predecessorHash", basis.PredecessorHash);
        w.WritePropertyName("releaseBase"); w.WriteRawValue(CampaignCombatReserveReleaseCodec.SerializeBase(basis.ReleaseBase, request)); w.WriteEndObject();
    });
    public static byte[] SerializeControl(Projection projection, CampaignCombatCreationRequest request) => Bytes(w =>
    {
        w.WriteStartObject(); w.WriteNumber("contractVersion", 1);
        w.WriteString("baseHash", CampaignOpeningPreambleCodec.Hash(SerializeBase(projection.Base, request)));
        w.WritePropertyName("release"); w.WriteRawValue(CampaignCombatReserveReleaseCodec.SerializeState(projection.Release));
        w.WriteStartArray("progress");
        foreach (var progress in projection.Progress)
        {
            w.WriteStartObject(); w.WriteString("eventType", progress.EventType); w.WriteString("receiptId", progress.ReceiptId);
            w.WriteString("eventHash", progress.EventHash); w.WriteEndObject();
        }
        w.WriteEndArray(); w.WriteBoolean("closed", projection.Closed); w.WriteEndObject();
    });
    private static void CheckBounds(ReadOnlySpan<byte> bytes)
    { if (bytes.Length is 0 or > 1_048_576) throw new JsonException("Inherited Release record exceeds bounds."); }
    private static byte[] Bytes(Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream(); using (var writer = new Utf8JsonWriter(stream)) write(writer);
        var bytes = stream.ToArray(); CheckBounds(bytes); return bytes;
    }
}
