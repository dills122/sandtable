using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Retains preamble field schemas while binding their sequence payload to version 4.</summary>
internal static class CampaignV11PreambleCodec
{
    public static byte[] Serialize(CampaignEvent value)
    {
        if (CampaignPreambleSequenceBinding.Position(value).ContractVersion != 4)
            throw new JsonException("Current preamble requires sequence 4.");
        var historical = CampaignPreambleSequenceBinding.Rebind(value, 3);
        return RebindJson(CampaignEventSerializer.Serialize(historical), 3, 4);
    }
    public static CampaignEvent Deserialize(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            var historical = CampaignEventSerializer.Deserialize(RebindJson(bytes, 4, 3));
            var current = CampaignPreambleSequenceBinding.Rebind(historical, 4);
            if (!bytes.Span.SequenceEqual(Serialize(current))) throw new JsonException("Noncanonical current preamble.");
            return current;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or ArithmeticException or KeyNotFoundException)
        { throw new JsonException("Invalid current preamble event.", error); }
    }
    private static byte[] RebindJson(ReadOnlyMemory<byte> bytes, int expectedVersion, int version)
    {
        using var document = JsonDocument.Parse(bytes);
        var position = document.RootElement.GetProperty("sequencePosition");
        if (position.GetProperty("contractVersion").GetInt32() != expectedVersion)
            throw new JsonException("Mixed preamble sequence identity.");
        return CampaignBreakdownCodec.Bytes(writer =>
        {
            writer.WriteStartObject();
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Name != "sequencePosition") { property.WriteTo(writer); continue; }
                writer.WriteStartObject(property.Name);
                foreach (var field in property.Value.EnumerateObject())
                    if (field.Name == "contractVersion") writer.WriteNumber(field.Name, version);
                    else field.WriteTo(writer);
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
        });
    }
}

internal static class CampaignPreambleSequenceBinding
{
    public static LandSequencePosition Rebind(LandSequencePosition position, int version)
    {
        if (version is not (3 or 4)) throw new ArgumentException("Unsupported preamble sequence version.");
        return new(version, position.PositionId, position.GameTurn, position.OperationStage, position.StageId,
            position.PhaseId, position.SegmentId, position.StepId, position.ActorRole, position.ActiveSide, position.Sources);
    }
    public static LandSequencePosition Position(CampaignEvent value) => value switch
    {
        InitiativeDetermined e => e.SequencePosition,
        OpeningPreambleAdvanced e => e.SequencePosition,
        WeatherDetermined e => e.SequencePosition,
        StageEntryResolved e => e.SequencePosition,
        ReserveDesignationEvent e => e.SequencePosition,
        _ => throw new JsonException("Not an unchanged preamble event."),
    };
    public static CampaignEvent Rebind(CampaignEvent value, int version)
    {
        var p = Rebind(Position(value), version);
        return value switch
        {
            InitiativeDetermined e => new InitiativeDetermined(e.CampaignId, e.StateVersion, e.FromPositionId,
                e.Outcome, e.RandomAlgorithmId, e.RandomCursorBefore, e.RandomCursorAfter, p, e.Sources),
            NoObligationNavalConvoyScheduleResolved e => new NoObligationNavalConvoyScheduleResolved(e.CampaignId,
                e.StateVersion, e.FromPositionId, p, e.Sources),
            NoObligationTacticalShippingResolved e => new NoObligationTacticalShippingResolved(e.CampaignId,
                e.StateVersion, e.FromPositionId, p, e.Sources),
            InitiativeOrderDeclared e => new InitiativeOrderDeclared(e.CampaignId, e.StateVersion, e.FromPositionId,
                p, e.OperationStage, e.DeclaringHolder, e.FirstSide, e.SecondSide, e.Sources),
            WeatherDetermined e => new WeatherDetermined(e.CampaignId, e.StateVersion, e.FromPositionId,
                e.GameTurn, e.OperationStage, e.DeterminingSide, e.Season, e.FirstDie, e.SecondDie, e.Kind, e.Scope,
                e.LocationDie, e.AffectedAreas, e.FuelWaterReductionSubjectCount, e.RestoredWellCount,
                e.DamagedGroundedAircraftCount, e.RandomCursorAfter, p, e.Sources),
            NoObligationOrganizationResolved e => new NoObligationOrganizationResolved(e.CampaignId, e.StateVersion,
                e.FromPositionId, e.GameTurn, e.OperationStage, p, e.Sources),
            NoObligationNavalConvoyArrivalResolved e => new NoObligationNavalConvoyArrivalResolved(e.CampaignId,
                e.StateVersion, e.FromPositionId, e.GameTurn, e.OperationStage, p, e.Sources),
            NoObligationFleetAssignmentResolved e => new NoObligationFleetAssignmentResolved(e.CampaignId,
                e.StateVersion, e.FromPositionId, e.GameTurn, e.OperationStage, p, e.Sources),
            NoObligationFleetRepairResolved e => new NoObligationFleetRepairResolved(e.CampaignId, e.StateVersion,
                e.FromPositionId, e.GameTurn, e.OperationStage, p, e.Sources),
            ReserveElementDesignated e => new ReserveElementDesignated(e.CampaignId, e.StateVersion, e.FromPositionId,
                e.GameTurn, e.OperationStage, e.ActingSide, e.ElementId, e.PriorStatus, e.ResultingStatus, p, e.Sources),
            ReserveDesignationCompleted e => new ReserveDesignationCompleted(e.CampaignId, e.StateVersion,
                e.FromPositionId, e.GameTurn, e.OperationStage, e.ActingSide, p, e.Sources),
            _ => throw new JsonException("Not an unchanged preamble event."),
        };
    }
}
