using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Observations;
using Cna.Core.Rules;

namespace Cna.OrleansHost;

// Trusted in-silo API only. MAP-08 must authorize a seat and translate outward action IDs
// before calling this interface; no HTTP route exposes private Core bytes.
internal interface ITemporaryCampaignGrain : IGrainWithStringKey
{
    Task<TemporaryCampaignReply> CreateAsync(byte[] capability, string operatorId, string requestId);
    Task<TemporaryCampaignReply> QueryAsync(byte[] capability, CampaignActionAudience audience);
    Task<TemporaryCampaignReply> SubmitAsync(byte[] capability, string actorId, string requestId,
        CampaignActionAudience audience, byte[] canonicalCommand);
    Task<TemporaryCampaignReply> FindRequestAsync(byte[] capability, string actorId, string requestId,
        CampaignActionAudience audience);
    Task<TemporaryCampaignReply> AdvanceSystemAsync(byte[] capability, int stepBudget);
    Task<string> ActivationIdForTestAsync(byte[] capability);
    Task DeactivateForTestAsync(byte[] capability);
}

public enum TemporaryCampaignStatus
{
    Created, Current, Accepted, Rejected, Conflict, Missing, NeedsHuman,
    UnsupportedPhase, StepLimit, Pending, Unavailable,
}

[GenerateSerializer]
public sealed record TemporaryAudienceView
{
    [Id(0)] public required string CampaignId { get; init; }
    [Id(1)] public long StateVersion { get; init; }
    [Id(2)] public required string RulesetHash { get; init; }
    [Id(3)] public required string PositionId { get; init; }
    [Id(4)] public required string Observer { get; init; }
    [Id(5)] public int OwnElementCount { get; init; }
    [Id(6)] public int ApparentOpposingPresenceCount { get; init; }
}

[GenerateSerializer]
public sealed record TemporaryCampaignReply
{
    [Id(0)] public TemporaryCampaignStatus Status { get; init; }
    [Id(1)] public byte[]? Event { get; init; }
    [Id(2)] public byte[]? Snapshot { get; init; }
    [Id(3)] public byte[]? Receipt { get; init; }
    [Id(4)] public TemporaryAudienceView? View { get; init; }
    [Id(5)] public byte[]? LegalActions { get; init; }
    [Id(6)] public string? Reason { get; init; }
    [Id(7)] public int Steps { get; init; }
}

/// <summary>Process-local only. Never supply the capability to a browser or remote client.</summary>
public sealed class TemporaryCampaignAccess
{
    private readonly byte[] capability = RandomNumberGenerator.GetBytes(32);
    internal byte[] Capability => capability.ToArray();
    internal bool Allows(byte[]? presented) => presented is { Length: 32 }
        && CryptographicOperations.FixedTimeEquals(capability, presented);
}

public sealed class TemporaryCampaignMemoryStore
{
    private readonly ConcurrentDictionary<string, TemporaryCampaignMemory> campaigns =
        new(StringComparer.Ordinal);
    internal TemporaryCampaignMemory Get(string id) =>
        campaigns.GetOrAdd(id, static _ => new TemporaryCampaignMemory());
}

internal sealed class TemporaryCampaignMemory
{
    internal Dictionary<string, RequestRecord> Requests { get; } = new(StringComparer.Ordinal);
    internal ExerciseSession? Committed { get; set; }
    internal string? Creator { get; set; }
    internal string? CreationRequestId { get; set; }
    internal TemporaryCampaignReply? CreationReply { get; set; }
    internal PendingStep? Pending { get; set; }
}

internal sealed record RequestRecord(string Actor, CampaignActionAudience Audience,
    byte[] Command, TemporaryCampaignReply Reply);
internal sealed record PendingStep(string Actor, string RequestId,
    CampaignActionAudience Audience, byte[] Command, ExerciseStepResult Result);

/// <summary>One activation, one admitted Rules9 lab campaign, no persistence or browser ingress.</summary>
public sealed class TemporaryCampaignGrain(
    TemporaryCampaignMemoryStore store, TemporaryCampaignAccess access) : Grain, ITemporaryCampaignGrain
{
    private const string FixtureCampaignId = "host-rsh-001";
    private const string RulesetHash = "17f3e6047f34b5bf6f5f809055863b664a4bd82a8481db83ee83e0ae5cad3a2a";
    private const string SetupHash = "sha256:e6631e81ad8f97e39fd9d7eec93bad7fe2b39db4d2d3059ed94a02dd4093e7a3";
    private const string ContentHash = "sha256:646e76e69ecceb82216b37d84e950928099acd8a3cb04b51526d0fe631e512ee";
    private readonly string activationId = Guid.NewGuid().ToString("N");
    private TemporaryCampaignMemory memory = null!;

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        memory = store.Get(this.GetPrimaryKeyString());
        return Task.CompletedTask;
    }

    public Task<TemporaryCampaignReply> CreateAsync(byte[] capability, string operatorId, string requestId)
    {
        if (!access.Allows(capability)) return Task.FromResult(Reply(TemporaryCampaignStatus.Rejected, "unauthorized"));
        if (!ValidIdentity(operatorId) || !ValidIdentity(requestId)
            || this.GetPrimaryKeyString() != FixtureCampaignId)
            return Task.FromResult(Reply(TemporaryCampaignStatus.Rejected, "invalidCreation"));
        if (memory.Committed is not null)
            return Task.FromResult(memory.Creator == operatorId && memory.CreationRequestId == requestId
                ? Copy(memory.CreationReply!) : Reply(TemporaryCampaignStatus.Conflict, "creationExists"));

        var request = new CampaignCreationRequest(1, FixtureCampaignId, RulesetHash, 12345,
            "rules-lab.breakdown.truck.v1", SetupHash,
            "rules-lab.content.breakdown-truck.v1", ContentHash, "breakdown-truck-lab");
        var start = CampaignHostAdapter.Create(request);
        if (!start.IsStarted)
            return Task.FromResult(Reply(TemporaryCampaignStatus.Unavailable, "fixtureAdmissionFailed"));
        memory.Committed = start.Session;
        memory.Creator = operatorId;
        memory.CreationRequestId = requestId;
        memory.CreationReply = new TemporaryCampaignReply
        {
            Status = TemporaryCampaignStatus.Created,
            Event = start.CreationEventBytes!.ToArray(),
            Snapshot = start.InitialSnapshotBytes!.ToArray(),
        };
        return Task.FromResult(Copy(memory.CreationReply));
    }

    public Task<TemporaryCampaignReply> QueryAsync(byte[] capability, CampaignActionAudience audience)
    {
        if (!access.Allows(capability)) return Task.FromResult(Reply(TemporaryCampaignStatus.Rejected, "unauthorized"));
        if (memory.Committed is null) return Task.FromResult(Reply(TemporaryCampaignStatus.Missing));
        if (audience == CampaignActionAudience.System)
        {
            var system = CampaignHostAdapter.QuerySystem(memory.Committed);
            return Task.FromResult(system.IsSuccessful
                ? new TemporaryCampaignReply
                {
                    Status = TemporaryCampaignStatus.Current,
                    LegalActions = CampaignLegalActionSerializer.Serialize(system.ActionSet!),
                }
                : Reply(TemporaryCampaignStatus.Unavailable, "invalidState"));
        }
        if (audience is not (CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth))
            return Task.FromResult(Reply(TemporaryCampaignStatus.Rejected, "invalidAudience"));
        var cut = CampaignHostAdapter.QueryAudience(memory.Committed, audience);
        if (!cut.IsProjected)
            return Task.FromResult(Reply(TemporaryCampaignStatus.Unavailable, "invalidState"));
        return Task.FromResult(new TemporaryCampaignReply
        {
            Status = TemporaryCampaignStatus.Current,
            View = new TemporaryAudienceView
            {
                CampaignId = cut.Observation!.CampaignId,
                StateVersion = cut.Observation.StateVersion,
                RulesetHash = cut.Observation.RulesetHash,
                PositionId = cut.Observation.Position.PositionId,
                Observer = cut.Observation.Observer.ToString(),
                OwnElementCount = cut.Observation.OwnElements.Count,
                ApparentOpposingPresenceCount = cut.Observation.ApparentOpposingPresences.Count,
            },
            LegalActions = CampaignLegalActionSerializer.Serialize(cut.LegalActions!),
        });
    }

    public Task<TemporaryCampaignReply> SubmitAsync(byte[] capability, string actorId, string requestId,
        CampaignActionAudience audience, byte[] canonicalCommand)
    {
        if (!access.Allows(capability)) return Task.FromResult(Reply(TemporaryCampaignStatus.Rejected, "unauthorized"));
        if (memory.Committed is null) return Task.FromResult(Reply(TemporaryCampaignStatus.Missing));
        if (!ValidIdentity(actorId) || !ValidIdentity(requestId) || canonicalCommand is null)
            return Task.FromResult(Reply(TemporaryCampaignStatus.Rejected, "invalidRequest"));
        if (memory.Requests.TryGetValue(requestId, out var prior))
            return Task.FromResult(prior.Actor == actorId && prior.Audience == audience
                && prior.Command.AsSpan().SequenceEqual(canonicalCommand)
                ? Copy(prior.Reply) : Reply(TemporaryCampaignStatus.Conflict, "requestReuse"));
        if (memory.Pending is not null)
            return Task.FromResult(memory.Pending.Actor == actorId && memory.Pending.RequestId == requestId
                && memory.Pending.Audience == audience
                && memory.Pending.Command.AsSpan().SequenceEqual(canonicalCommand)
                ? Reply(TemporaryCampaignStatus.Pending)
                : Reply(TemporaryCampaignStatus.Conflict, "pendingMutation"));
        if (audience == CampaignActionAudience.System && actorId != "host-system"
            || audience is not (CampaignActionAudience.System or CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth)
            || audience != CampaignActionAudience.System && actorId == "host-system")
            return Task.FromResult(Reply(TemporaryCampaignStatus.Rejected, "invalidActor"));
        CampaignActionSubmission command;
        try { command = CampaignActionSubmissionSerializer.DeserializeCanonical(canonicalCommand); }
        catch (JsonException) { return Task.FromResult(Reply(TemporaryCampaignStatus.Rejected, "invalidCommand")); }
        if (command.Audience != audience || command.CampaignId != this.GetPrimaryKeyString())
            return Task.FromResult(Reply(TemporaryCampaignStatus.Rejected, "invalidCommand"));
        var result = CampaignHostAdapter.Submit(memory.Committed, command);
        if (!result.IsAccepted)
            return Task.FromResult(Reply(TemporaryCampaignStatus.Rejected, result.RejectionReason.ToString()));

        // The memory commit has no await/provider call. Candidate and its request record become
        // visible together in this serialized grain turn; a process restart loses both.
        memory.Pending = new PendingStep(actorId, requestId, audience, canonicalCommand.ToArray(), result);
        var evidence = memory.Pending.Result.Evidence!;
        var reply = new TemporaryCampaignReply
        {
            Status = TemporaryCampaignStatus.Accepted,
            Event = evidence.EventRecords.Single().ToArray(),
            Snapshot = evidence.SnapshotCheckpoint.ToArray(),
            Receipt = CampaignActionAcceptanceReceiptSerializer.Serialize(evidence.Receipt),
        };
        memory.Requests.Add(requestId, new RequestRecord(actorId, audience,
            memory.Pending.Command, Copy(reply)));
        memory.Committed = memory.Pending.Result.SuccessorSession;
        memory.Pending = null;
        return Task.FromResult(Copy(reply));
    }

    public Task<TemporaryCampaignReply> FindRequestAsync(byte[] capability, string actorId, string requestId,
        CampaignActionAudience audience)
    {
        if (!access.Allows(capability)) return Task.FromResult(Reply(TemporaryCampaignStatus.Rejected, "unauthorized"));
        if (memory.Committed is null) return Task.FromResult(Reply(TemporaryCampaignStatus.Missing));
        if (!ValidIdentity(actorId) || !ValidIdentity(requestId))
            return Task.FromResult(Reply(TemporaryCampaignStatus.Rejected, "invalidRequest"));
        if (!memory.Requests.TryGetValue(requestId, out var record))
            return Task.FromResult(Reply(TemporaryCampaignStatus.Missing));
        return Task.FromResult(record.Actor == actorId && record.Audience == audience
            ? Copy(record.Reply) : Reply(TemporaryCampaignStatus.Conflict, "requestOwner"));
    }

    public async Task<TemporaryCampaignReply> AdvanceSystemAsync(byte[] capability, int stepBudget)
    {
        if (!access.Allows(capability)) return Reply(TemporaryCampaignStatus.Rejected, "unauthorized");
        if (memory.Committed is null) return Reply(TemporaryCampaignStatus.Missing);
        if (stepBudget is < 1 or > 32) return Reply(TemporaryCampaignStatus.Rejected, "invalidBudget");
        for (var step = 0; step < stepBudget; step++)
        {
            var system = CampaignHostAdapter.QuerySystem(memory.Committed);
            if (!system.IsSuccessful) return Reply(TemporaryCampaignStatus.Unavailable, "invalidState", step);
            if (system.ActionSet!.Candidates.Count == 0)
            {
                var axis = CampaignHostAdapter.QueryAudience(memory.Committed, CampaignActionAudience.Axis);
                var commonwealth = CampaignHostAdapter.QueryAudience(memory.Committed, CampaignActionAudience.Commonwealth);
                if (!axis.IsProjected || !commonwealth.IsProjected)
                    return Reply(TemporaryCampaignStatus.Unavailable, "invalidState", step);
                return Reply(axis.LegalActions!.Candidates.Count > 0
                    || commonwealth.LegalActions!.Candidates.Count > 0
                    ? TemporaryCampaignStatus.NeedsHuman : TemporaryCampaignStatus.UnsupportedPhase,
                    null, step);
            }
            var candidate = system.ActionSet.Candidates[0];
            var command = new CampaignActionSubmission(1, system.ActionSet.CampaignId,
                system.ActionSet.StateVersion, system.ActionSet.PositionId,
                CampaignActionAudience.System, candidate.ActionId);
            var bytes = CampaignActionSubmissionSerializer.SerializeCanonical(command);
            var id = $"system-{system.ActionSet.StateVersion}-{candidate.ActionId}";
            var accepted = await SubmitAsync(capability, "host-system", id, CampaignActionAudience.System, bytes);
            if (accepted.Status != TemporaryCampaignStatus.Accepted)
                return Reply(TemporaryCampaignStatus.Unavailable, "systemStepRejected", step);
        }
        return Reply(TemporaryCampaignStatus.StepLimit, null, stepBudget);
    }

    public Task DeactivateForTestAsync(byte[] capability)
    {
        if (access.Allows(capability)) DeactivateOnIdle();
        return Task.CompletedTask;
    }

    public Task<string> ActivationIdForTestAsync(byte[] capability) =>
        Task.FromResult(access.Allows(capability) ? activationId : string.Empty);

    private static bool ValidIdentity(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 128;
    private static TemporaryCampaignReply Reply(TemporaryCampaignStatus status,
        string? reason = null, int steps = 0) => new() { Status = status, Reason = reason, Steps = steps };
    private static TemporaryCampaignReply Copy(TemporaryCampaignReply value) => value with
    {
        Event = value.Event?.ToArray(),
        Snapshot = value.Snapshot?.ToArray(),
        Receipt = value.Receipt?.ToArray(),
        LegalActions = value.LegalActions?.ToArray(),
    };
}
