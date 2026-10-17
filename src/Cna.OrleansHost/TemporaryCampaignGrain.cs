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
public interface ITemporaryCampaignGrain : IGrainWithStringKey
{
    Task<TemporaryCampaignReply> CreateAsync(string operatorId, string requestId);
    Task<TemporaryCampaignReply> QueryAsync(CampaignActionAudience audience);
    Task<TemporaryCampaignReply> SubmitAsync(string actorId, string requestId,
        CampaignActionAudience audience, byte[] canonicalCommand);
    Task<TemporaryCampaignReply> FindRequestAsync(string actorId, string requestId,
        CampaignActionAudience audience);
    Task<TemporaryCampaignReply> AdvanceSystemAsync(int stepBudget);
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

/// <summary>One activation, one admitted Rules9 lab campaign, no persistence or browser ingress.</summary>
public sealed class TemporaryCampaignGrain : Grain, ITemporaryCampaignGrain
{
    private const string FixtureCampaignId = "host-rsh-001";
    private const string RulesetHash = "17f3e6047f34b5bf6f5f809055863b664a4bd82a8481db83ee83e0ae5cad3a2a";
    private const string SetupHash = "sha256:e6631e81ad8f97e39fd9d7eec93bad7fe2b39db4d2d3059ed94a02dd4093e7a3";
    private const string ContentHash = "sha256:646e76e69ecceb82216b37d84e950928099acd8a3cb04b51526d0fe631e512ee";
    private readonly Dictionary<string, RequestRecord> requests = new(StringComparer.Ordinal);
    private ExerciseSession? committed;
    private string? creator;
    private string? creationRequestId;
    private TemporaryCampaignReply? creationReply;
    private PendingStep? pending;

    public Task<TemporaryCampaignReply> CreateAsync(string operatorId, string requestId)
    {
        if (!ValidIdentity(operatorId) || !ValidIdentity(requestId)
            || this.GetPrimaryKeyString() != FixtureCampaignId)
            return Task.FromResult(Reply(TemporaryCampaignStatus.Rejected, "invalidCreation"));
        if (committed is not null)
            return Task.FromResult(creator == operatorId && creationRequestId == requestId
                ? creationReply! : Reply(TemporaryCampaignStatus.Conflict, "creationExists"));

        var request = new CampaignCreationRequest(1, FixtureCampaignId, RulesetHash, 12345,
            "rules-lab.breakdown.truck.v1", SetupHash,
            "rules-lab.content.breakdown-truck.v1", ContentHash, "breakdown-truck-lab");
        var start = CampaignHostAdapter.Create(request);
        if (!start.IsStarted)
            return Task.FromResult(Reply(TemporaryCampaignStatus.Unavailable, "fixtureAdmissionFailed"));
        committed = start.Session;
        creator = operatorId;
        creationRequestId = requestId;
        creationReply = new TemporaryCampaignReply
        {
            Status = TemporaryCampaignStatus.Created,
            Event = start.CreationEventBytes!.ToArray(),
            Snapshot = start.InitialSnapshotBytes!.ToArray(),
        };
        return Task.FromResult(Copy(creationReply));
    }

    public Task<TemporaryCampaignReply> QueryAsync(CampaignActionAudience audience)
    {
        if (committed is null) return Task.FromResult(Reply(TemporaryCampaignStatus.Missing));
        if (audience == CampaignActionAudience.System)
        {
            var system = CampaignHostAdapter.QuerySystem(committed);
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
        var cut = CampaignHostAdapter.QueryAudience(committed, audience);
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

    public Task<TemporaryCampaignReply> SubmitAsync(string actorId, string requestId,
        CampaignActionAudience audience, byte[] canonicalCommand)
    {
        if (committed is null) return Task.FromResult(Reply(TemporaryCampaignStatus.Missing));
        if (!ValidIdentity(actorId) || !ValidIdentity(requestId) || canonicalCommand is null)
            return Task.FromResult(Reply(TemporaryCampaignStatus.Rejected, "invalidRequest"));
        if (requests.TryGetValue(requestId, out var prior))
            return Task.FromResult(prior.Actor == actorId && prior.Audience == audience
                && prior.Command.AsSpan().SequenceEqual(canonicalCommand)
                ? Copy(prior.Reply) : Reply(TemporaryCampaignStatus.Conflict, "requestReuse"));
        if (pending is not null)
            return Task.FromResult(pending.Actor == actorId && pending.RequestId == requestId
                && pending.Audience == audience
                && pending.Command.AsSpan().SequenceEqual(canonicalCommand)
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
        var result = CampaignHostAdapter.Submit(committed, command);
        if (!result.IsAccepted)
            return Task.FromResult(Reply(TemporaryCampaignStatus.Rejected, result.RejectionReason.ToString()));

        // The memory commit has no await/provider call. Candidate and its request record become
        // visible together in this serialized grain turn; a process restart loses both.
        pending = new PendingStep(actorId, requestId, audience, canonicalCommand.ToArray(), result);
        var evidence = pending.Result.Evidence!;
        var reply = new TemporaryCampaignReply
        {
            Status = TemporaryCampaignStatus.Accepted,
            Event = evidence.EventRecords.Single().ToArray(),
            Snapshot = evidence.SnapshotCheckpoint.ToArray(),
            Receipt = CampaignActionAcceptanceReceiptSerializer.Serialize(evidence.Receipt),
        };
        requests.Add(requestId, new RequestRecord(actorId, audience,
            pending.Command, Copy(reply)));
        committed = pending.Result.SuccessorSession;
        pending = null;
        return Task.FromResult(Copy(reply));
    }

    public Task<TemporaryCampaignReply> FindRequestAsync(string actorId, string requestId,
        CampaignActionAudience audience)
    {
        if (committed is null) return Task.FromResult(Reply(TemporaryCampaignStatus.Missing));
        if (!ValidIdentity(actorId) || !ValidIdentity(requestId))
            return Task.FromResult(Reply(TemporaryCampaignStatus.Rejected, "invalidRequest"));
        if (!requests.TryGetValue(requestId, out var record))
            return Task.FromResult(Reply(TemporaryCampaignStatus.Missing));
        return Task.FromResult(record.Actor == actorId && record.Audience == audience
            ? Copy(record.Reply) : Reply(TemporaryCampaignStatus.Conflict, "requestOwner"));
    }

    public async Task<TemporaryCampaignReply> AdvanceSystemAsync(int stepBudget)
    {
        if (committed is null) return Reply(TemporaryCampaignStatus.Missing);
        if (stepBudget is < 1 or > 32) return Reply(TemporaryCampaignStatus.Rejected, "invalidBudget");
        for (var step = 0; step < stepBudget; step++)
        {
            var system = CampaignHostAdapter.QuerySystem(committed);
            if (!system.IsSuccessful) return Reply(TemporaryCampaignStatus.Unavailable, "invalidState", step);
            if (system.ActionSet!.Candidates.Count == 0)
            {
                var axis = CampaignHostAdapter.QueryAudience(committed, CampaignActionAudience.Axis);
                var commonwealth = CampaignHostAdapter.QueryAudience(committed, CampaignActionAudience.Commonwealth);
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
            var accepted = await SubmitAsync("host-system", id, CampaignActionAudience.System, bytes);
            if (accepted.Status != TemporaryCampaignStatus.Accepted)
                return Reply(TemporaryCampaignStatus.Unavailable, "systemStepRejected", step);
        }
        return Reply(TemporaryCampaignStatus.StepLimit, null, stepBudget);
    }

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
    private sealed record RequestRecord(string Actor, CampaignActionAudience Audience,
        byte[] Command, TemporaryCampaignReply Reply);
    private sealed record PendingStep(string Actor, string RequestId,
        CampaignActionAudience Audience, byte[] Command, ExerciseStepResult Result);
}
