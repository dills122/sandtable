using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Exercises;

// Opaque synthetic session. Host must authenticate callers and persist evidence atomically.
public sealed class CampaignScheduledWorld6Session
{
    internal CampaignScheduledWorld6Session(CampaignCreationRequest request, CampaignScheduledWorld6Created creation,
        IEnumerable<CampaignScheduledWorld6PreambleEvent> preamble, CampaignScheduledWorld6Event? arrival)
    {
        Request = request;
        Creation = creation;
        Preamble = Array.AsReadOnly(preamble.ToArray());
        Arrival = arrival;
    }

    internal CampaignCreationRequest Request { get; }
    internal CampaignScheduledWorld6Created Creation { get; }
    internal IReadOnlyList<CampaignScheduledWorld6PreambleEvent> Preamble { get; }
    internal CampaignScheduledWorld6Event? Arrival { get; }
}

public sealed class CampaignScheduledWorld6Step
{
    private readonly byte[] eventBytes;
    private readonly byte[] checkpointBytes;
    private readonly byte[] commandBytes;
    private readonly byte[] receiptBytes;

    internal CampaignScheduledWorld6Step(CampaignScheduledWorld6Session session, byte[] commandBytes, byte[] eventBytes,
        byte[] receiptBytes, byte[] checkpointBytes, bool isRetry)
    {
        Session = session;
        this.eventBytes = eventBytes.ToArray();
        this.checkpointBytes = checkpointBytes.ToArray();
        this.commandBytes = commandBytes.ToArray();
        this.receiptBytes = receiptBytes.ToArray();
        IsRetry = isRetry;
    }

    public CampaignScheduledWorld6Session Session { get; }
    public byte[] EventBytes => eventBytes.ToArray();
    public byte[] CheckpointBytes => checkpointBytes.ToArray();
    public byte[] CommandBytes => commandBytes.ToArray();
    public byte[] ReceiptBytes => receiptBytes.ToArray();
    public bool IsRetry { get; }
}

public sealed record CampaignScheduledWorld6OfferView(string ElementId, string DestinationId);

public sealed class CampaignScheduledWorld6Evidence
{
    private readonly byte[] command;
    private readonly byte[] campaignEvent;
    private readonly byte[] receipt;
    private readonly byte[] checkpoint;

    public CampaignScheduledWorld6Evidence(byte[] command, byte[] campaignEvent, byte[] receipt, byte[] checkpoint)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(campaignEvent);
        ArgumentNullException.ThrowIfNull(receipt);
        ArgumentNullException.ThrowIfNull(checkpoint);
        this.command = command.ToArray();
        this.campaignEvent = campaignEvent.ToArray();
        this.receipt = receipt.ToArray();
        this.checkpoint = checkpoint.ToArray();
    }

    public byte[] CommandBytes => command.ToArray();
    public byte[] EventBytes => campaignEvent.ToArray();
    public byte[] ReceiptBytes => receipt.ToArray();
    public byte[] CheckpointBytes => checkpoint.ToArray();
}

// Explicitly synthetic Core bridge, not the ordinary campaign creation API.
public static class CampaignScheduledWorld6Bridge
{
    public static CampaignScheduledWorld6Step Begin(CampaignCreationRequest request)
    {
        var creation = CampaignScheduledWorld6Creation.Create(request);
        var session = new CampaignScheduledWorld6Session(request, creation, [], null);
        return new CampaignScheduledWorld6Step(session, [], CampaignScheduledWorld6CreatedCodec.Encode(creation), [],
            CampaignScheduledWorld6PreambleCheckpointCodec.Encode(request, creation, []), false);
    }

    public static CampaignScheduledWorld6Step AdvancePreamble(CampaignScheduledWorld6Session session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.Arrival is not null || session.Preamble.Count >= 6)
            throw new InvalidOperationException("The synthetic preamble cannot advance.");
        var expected = CampaignScheduledWorld6PreambleEvents.Create(session.Request, session.Creation);
        var next = expected[session.Preamble.Count];
        var prefix = session.Preamble.Append(next).ToArray();
        var successor = new CampaignScheduledWorld6Session(session.Request, session.Creation, prefix, null);
        var eventBytes = CampaignScheduledWorld6PreambleEventCodec.Encode(next);
        return new CampaignScheduledWorld6Step(successor, CampaignScheduledWorld6EvidenceCodec.PreambleCommand(next), eventBytes,
            CampaignScheduledWorld6EvidenceCodec.Receipt(session.Request.CampaignId, next.ResultVersion, eventBytes),
            CampaignScheduledWorld6PreambleCheckpointCodec.Encode(session.Request, session.Creation, prefix), false);
    }

    public static CampaignScheduledWorld6Step AdvanceNativePreamble(CampaignScheduledWorld6Session session)
    {
        var legacy = AdvancePreamble(session);
        var ordinal = session.Preamble.Count;
        var native = CampaignScheduledWorld6Preamble.Run(session.Request, session.Creation).Trace[ordinal];
        var value = legacy.Session.Preamble[ordinal];
        var eventBytes = CampaignScheduledWorld6NativePreambleCodec.Encode(value, native.NativeEventBase64);
        return new CampaignScheduledWorld6Step(legacy.Session, legacy.CommandBytes, eventBytes,
            CampaignScheduledWorld6EvidenceCodec.Receipt(session.Request.CampaignId, value.ResultVersion, eventBytes),
            legacy.CheckpointBytes, false);
    }

    public static IReadOnlyList<CampaignScheduledWorld6OfferView> QueryOffers(CampaignScheduledWorld6Session session, LandSide side)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (!Enum.IsDefined(side)) throw new ArgumentOutOfRangeException(nameof(side));
        if (session.Preamble.Count != 6 || session.Arrival is not null) return Array.Empty<CampaignScheduledWorld6OfferView>();
        var proof = RequireComplete(session);
        return Array.AsReadOnly(CampaignScheduledWorld6Phase.Start(proof).QueryOffers(side)
            .Select(value => new CampaignScheduledWorld6OfferView(value.ElementId, value.DestinationId)).ToArray());
    }

    public static CampaignScheduledWorld6Step Arrive(CampaignScheduledWorld6Session session,
        string commandId, long expectedVersion, LandSide side, string destinationId)
    {
        ArgumentNullException.ThrowIfNull(session);
        var proof = RequireComplete(session);
        var phase = session.Arrival is null ? CampaignScheduledWorld6Phase.Start(proof)
            : CampaignScheduledWorld6Phase.Replay(proof, [session.Arrival]);
        var command = new CampaignScheduledWorld6Command(1, session.Request.CampaignId,
            CampaignScheduledWorld6Phase.SetupHash, CampaignScheduledWorld6CreatedCodec.CalculateHash(session.Creation),
            commandId, expectedVersion, side, destinationId);
        var result = phase.Decide(command);
        var successor = result.IsRetry ? session : new CampaignScheduledWorld6Session(session.Request, session.Creation, session.Preamble, result.Event);
        var eventBytes = CampaignScheduledWorld6EventCodec.Encode(result.Event);
        return new CampaignScheduledWorld6Step(successor, CampaignScheduledWorld6EvidenceCodec.ArrivalCommand(command), eventBytes,
            CampaignScheduledWorld6EvidenceCodec.Receipt(session.Request.CampaignId, result.Event.ResultVersion, eventBytes),
            CampaignScheduledWorld6CheckpointCodec.Encode(result.State), result.IsRetry);
    }

    public static CampaignScheduledWorld6Session Import(CampaignCreationRequest request, byte[] creationBytes,
        IEnumerable<byte[]> preambleEvents, byte[]? arrivalEvent, byte[] checkpointBytes)
    {
        ArgumentNullException.ThrowIfNull(creationBytes);
        ArgumentNullException.ThrowIfNull(preambleEvents);
        ArgumentNullException.ThrowIfNull(checkpointBytes);
        var creation = CampaignScheduledWorld6CreatedCodec.Decode(creationBytes, request);
        var records = preambleEvents.Take(7).Select(value => CampaignScheduledWorld6PreambleEventCodec.Decode(value)).ToArray();
        var verified = CampaignScheduledWorld6PreambleEvents.Verify(request, creation, records);
        if (arrivalEvent is null)
        {
            _ = CampaignScheduledWorld6PreambleCheckpointCodec.Decode(checkpointBytes, request, creation);
            if (!checkpointBytes.AsSpan().SequenceEqual(CampaignScheduledWorld6PreambleCheckpointCodec.Encode(request, creation, records)))
                throw new InvalidOperationException("Preamble checkpoint does not match the event prefix.");
            return new CampaignScheduledWorld6Session(request, creation, records, null);
        }
        var proof = verified.CompleteCut ?? throw new InvalidOperationException("Arrival requires a complete preamble.");
        var arrived = CampaignScheduledWorld6EventCodec.Decode(arrivalEvent);
        var phase = CampaignScheduledWorld6Phase.Replay(proof, [arrived]);
        _ = CampaignScheduledWorld6CheckpointCodec.Decode(checkpointBytes, proof);
        if (!checkpointBytes.AsSpan().SequenceEqual(CampaignScheduledWorld6CheckpointCodec.Encode(phase)))
            throw new InvalidOperationException("Arrival checkpoint does not match its event.");
        return new CampaignScheduledWorld6Session(request, creation, records, arrived);
    }

    public static CampaignScheduledWorld6Session Restore(CampaignCreationRequest request, byte[] creationBytes,
        byte[] initialCheckpointBytes, IEnumerable<CampaignScheduledWorld6Evidence> evidence)
    {
        ArgumentNullException.ThrowIfNull(creationBytes);
        ArgumentNullException.ThrowIfNull(initialCheckpointBytes);
        ArgumentNullException.ThrowIfNull(evidence);
        var created = Begin(request);
        if (!creationBytes.AsSpan().SequenceEqual(created.EventBytes)
            || !initialCheckpointBytes.AsSpan().SequenceEqual(created.CheckpointBytes))
            throw new InvalidOperationException("Synthetic creation evidence does not match replay.");
        var session = created.Session;
        var index = 0;
        bool? nativePreamble = null;
        foreach (var retained in evidence)
        {
            if (retained is null || index >= 7)
                throw new InvalidOperationException("Invalid synthetic evidence sequence.");
            CampaignScheduledWorld6Step expected;
            if (index < 6)
            {
                var native = CampaignScheduledWorld6NativePreambleCodec.IsNative(retained.EventBytes);
                if (nativePreamble is not null && nativePreamble != native)
                    throw new InvalidOperationException("Mixed synthetic preamble versions are not admitted.");
                nativePreamble = native;
                expected = native ? AdvanceNativePreamble(session) : AdvancePreamble(session);
            }
            else
            {
                var value = CampaignScheduledWorld6EventCodec.Decode(retained.EventBytes);
                expected = Arrive(session, value.Command.CommandId, value.Command.ExpectedVersion,
                    value.Command.Actor, value.Command.DestinationId);
            }
            if (expected.IsRetry || !retained.CommandBytes.AsSpan().SequenceEqual(expected.CommandBytes)
                || !retained.EventBytes.AsSpan().SequenceEqual(expected.EventBytes)
                || !retained.ReceiptBytes.AsSpan().SequenceEqual(expected.ReceiptBytes)
                || !retained.CheckpointBytes.AsSpan().SequenceEqual(expected.CheckpointBytes))
                throw new InvalidOperationException("Synthetic step evidence does not match replay.");
            session = expected.Session;
            index++;
        }
        return session;
    }

    public static bool IsArrivalEventBound(byte[] eventBytes, string campaignId, string commandId)
    {
        ArgumentNullException.ThrowIfNull(eventBytes);
        var value = CampaignScheduledWorld6EventCodec.Decode(eventBytes);
        return value.Command.CampaignId == campaignId && value.Command.CommandId == commandId;
    }

    public static bool IsNativePreambleEvent(byte[] eventBytes) =>
        CampaignScheduledWorld6NativePreambleCodec.IsNative(eventBytes);

    private static CampaignScheduledWorld6PreambleEvents.VerifiedCut RequireComplete(CampaignScheduledWorld6Session session) =>
        CampaignScheduledWorld6PreambleEvents.Verify(session.Request, session.Creation, session.Preamble).CompleteCut
        ?? throw new InvalidOperationException("The synthetic preamble is incomplete.");
}
