using System.Security.Cryptography;
using Cna.Core.Actions;
using Cna.Core.Hosting;
using Cna.OrleansHost.Api;

namespace Cna.OrleansHost;

// Process-private continuity state. No member of this type is a browser DTO.
internal sealed class TemporaryAudienceTracker
{
    private readonly Dictionary<CampaignActionAudience, SideState> sides = new();
    private readonly byte[] namespaceKey = RandomNumberGenerator.GetBytes(32);
    internal string CampaignId { get; }
    internal string SourcePin { get; }
    private string RulesetHash { get; }
    private string ScenarioId { get; }
    private string SetupHash { get; }
    private string ContentHash { get; }
    internal long StateVersion { get; private set; }
    internal string CommittedHeadHash { get; private set; } = string.Empty;
    internal CampaignAudienceOutcomePair? LastAcceptedOutcome { get; private set; }
    internal bool IsAvailable { get; private set; } = true;

    private TemporaryAudienceTracker(string campaignId, string rulesetHash,
        string scenarioId, string setupHash, string contentHash)
    {
        CampaignId = campaignId;
        RulesetHash = rulesetHash;
        ScenarioId = scenarioId;
        SetupHash = setupHash;
        ContentHash = contentHash;
        SourcePin = $"{rulesetHash}|{scenarioId}|{setupHash}|{contentHash}";
    }

    internal static TemporaryAudienceTracker? Create(string campaignId, string rulesetHash,
        string scenarioId, string setupHash, string contentHash,
        string initialHeadHash, CampaignHostPrivateSideCut? axis,
        CampaignHostPrivateSideCut? commonwealth)
    {
        if (!ValidHash(initialHeadHash)
            || !ValidPair(campaignId, rulesetHash, scenarioId, axis, commonwealth, 0))
            return null;
        var tracker = new TemporaryAudienceTracker(campaignId, rulesetHash, scenarioId,
            setupHash, contentHash);
        tracker.AdvancePair(axis!, commonwealth!, initialHeadHash, null);
        return tracker;
    }

    internal bool AdvanceAccepted(CampaignHostPrivateSideCut? axis,
        CampaignHostPrivateSideCut? commonwealth, CampaignHostAcceptedStep? acceptedStep,
        CampaignAudienceOutcomePair? outcomes)
    {
        if (!IsAvailable || !ValidPair(CampaignId, RulesetHash, ScenarioId,
                axis, commonwealth, StateVersion)
            || !ValidAcceptedStep(axis!, commonwealth!, acceptedStep, outcomes))
        {
            IsAvailable = false;
            return false;
        }
        try { AdvancePair(axis!, commonwealth!, outcomes!.Axis!.SuccessorHeadHash, outcomes); }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException
            or OverflowException)
        {
            IsAvailable = false;
            return false;
        }
        return true;
    }

    internal TemporaryPrivateAudienceCut? Read(CampaignActionAudience audience,
        long committedVersion, string sessionGeneration)
    {
        if (!IsAvailable || committedVersion != StateVersion
            || !Guid.TryParse(sessionGeneration, out _)
            || !sides.TryGetValue(audience, out var side))
            return null;
        var cut = side.Cut;
        return cut with
        {
            ActionSetToken = Scope("set", audience, sessionGeneration, cut.ActionSetToken),
            Locations = cut.Locations.ToArray(),
            OwnElements = cut.OwnElements.ToArray(),
            Contacts = cut.Contacts.Select(c => c with
            {
                ContactId = Scope("contact", audience, sessionGeneration, c.ContactId),
            }).ToArray(),
            Actions = cut.Actions.Select(a => a with
            {
                OutwardActionId = Scope("action", audience, sessionGeneration, a.OutwardActionId),
            }).ToArray(),
            Diary = cut.Diary.Select(entry => entry with
            {
                EntryId = Scope("diary", audience, sessionGeneration, entry.EntryId),
            }).ToArray(),
        };
    }

    // A trusted grain turn resolves a session-scoped handle against the current committed cut.
    // The Core ID is never part of the outward token or stable action identity.
    internal string? Resolve(CampaignActionAudience audience, long committedVersion,
        string sessionGeneration, string actionSetToken, string outwardActionId)
    {
        var cut = Read(audience, committedVersion, sessionGeneration);
        if (cut is null || cut.ActionSetToken != actionSetToken) return null;
        return cut.Actions.SingleOrDefault(action =>
            action.OutwardActionId == outwardActionId)?.CoreActionId;
    }

    internal void Invalidate() => IsAvailable = false;

    private string Scope(string domain, CampaignActionAudience side, string generation,
        string privateId)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(
            $"{domain}\0{CampaignId}\0{side}\0{generation}\0{privateId}");
        var hash = HMACSHA256.HashData(namespaceKey, bytes);
        return new Guid(hash.AsSpan(0, 16)).ToString("D");
    }

    private void AdvancePair(CampaignHostPrivateSideCut axis, CampaignHostPrivateSideCut commonwealth,
        string headHash, CampaignAudienceOutcomePair? outcome)
    {
        // Build both successor projections before publishing either side's committed head.
        var nextAxis = AdvanceSide(axis, outcome?.Axis);
        var nextCommonwealth = AdvanceSide(commonwealth, outcome?.Commonwealth);
        sides[CampaignActionAudience.Axis] = nextAxis;
        sides[CampaignActionAudience.Commonwealth] = nextCommonwealth;
        StateVersion = axis.StateVersion;
        CommittedHeadHash = headHash;
        LastAcceptedOutcome = outcome;
    }

    private bool ValidAcceptedStep(CampaignHostPrivateSideCut axis,
        CampaignHostPrivateSideCut commonwealth,
        CampaignHostAcceptedStep? step, CampaignAudienceOutcomePair? outcomes)
    {
        if (step is null || outcomes?.HasTypedProof != true
            || step.ContractVersion != CampaignHostAcceptedStep.CurrentContractVersion
            || step.EventRecords.Count != 1)
            return false;
        try
        {
            var commandBytes = step.CanonicalCommand;
            var command = CampaignActionSubmissionSerializer.DeserializeCanonical(commandBytes);
            var receiptBytes = step.CanonicalReceipt;
            var receipt = CampaignActionAcceptanceReceiptSerializer.DeserializeCanonical(receiptBytes);
            var commandHash = Hash(commandBytes);
            var eventHash = Hash(step.EventRecords[0]);
            using var eventDocument = System.Text.Json.JsonDocument.Parse(step.EventRecords[0]);
            var eventKind = eventDocument.RootElement.GetProperty("eventType").GetString();
            var eventVersion = eventDocument.RootElement.GetProperty("contractVersion").GetInt32();
            var successorHash = Hash(step.SnapshotCheckpoint);
            var campaignHash = Hash(System.Text.Encoding.UTF8.GetBytes(CampaignId));
            var sourceHash = Hash(System.Text.Encoding.UTF8.GetBytes(string.Join('\n',
                RulesetHash, SetupHash, ContentHash, ScenarioId)));
            if (command.CampaignId != CampaignId || receipt.CampaignId != CampaignId
                || command.ExpectedStateVersion != StateVersion
                || receipt.PriorStateVersion != StateVersion
                || receipt.CommittedStateVersion != axis.StateVersion
                || command.ExpectedPositionId != sides[CampaignActionAudience.Axis].Cut.PositionId
                || receipt.ResultingPositionId != axis.PositionId
                || receipt.Audience != command.Audience || receipt.ActionId != command.ActionId
                || !commandBytes.AsSpan().SequenceEqual(
                    CampaignActionSubmissionSerializer.SerializeCanonical(command)))
                return false;
            var first = outcomes.Axis!;
            var second = outcomes.Commonwealth!;
            bool Matches(CampaignAudienceOutcomeDecision decision,
                CampaignActionAudience audience) =>
                decision.ContractVersion == 1 && decision.Audience == audience
                && decision.CampaignBindingHash == campaignHash
                && decision.SourceBindingHash == sourceHash
                && decision.PriorHeadHash == CommittedHeadHash
                && decision.SuccessorHeadHash == successorHash
                && decision.PriorStateVersion == StateVersion
                && decision.SuccessorStateVersion == axis.StateVersion
                && decision.CommandBindingHash == commandHash
                && decision.EventHash == eventHash && decision.EventIndex == 0
                && decision.ActingAudience == command.Audience
                && decision.EventContractVersion == eventVersion
                && decision.EventKind == eventKind
                && !string.IsNullOrWhiteSpace(decision.ActionKind);
            var classification = CampaignAudienceOutcomeSource.ClassifyCanonicalAcceptedEvent(
                step.EventRecords[0], first.ActionKind, command.Audience);
            if (classification is null) return false;
            if (command.Audience is CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth
                && (!sides.TryGetValue(command.Audience, out var actingSide)
                    || !actingSide.Cut.Actions.Any(action =>
                        action.CoreActionId == command.ActionId
                        && action.Kind == first.ActionKind))) return false;
            bool Classified(CampaignAudienceOutcomeDecision decision,
                CampaignHostPrivateSideCut successor)
            {
                var isActor = decision.Audience == command.Audience;
                var expectedKey = isActor ? classification.Value.CopyKey : null;
                var expectedElement = isActor ? classification.Value.ElementId : null;
                // Reaction observations intentionally suppress even the reacting side's
                // own rows. Verify that exact state and retain the committed step without
                // inventing a formation link or a diary entry.
                if (isActor && expectedKey == "diary.reactionMoved"
                    && expectedElement is not null
                    && sides.TryGetValue(decision.Audience, out var suppressed)
                    && suppressed.Cut.DecisionStateKind == "reacting"
                    && successor.DecisionStateKind == "reacting"
                    && suppressed.Cut.OwnElements.Length == 0
                    && successor.OwnElements.Count == 0)
                    return decision.ProposedCopyKey is null
                        && decision.OwnElementIndex is null
                        && decision.Disposition == CampaignAudienceOutcomeDisposition.Unavailable
                        && decision.ReasonCode == "own-element-unavailable";
                if (decision.ProposedCopyKey != expectedKey) return false;
                if (expectedElement is null) return decision.OwnElementIndex is null;
                if (!sides.TryGetValue(decision.Audience, out var before)
                    || !before.Cut.OwnElements.Any(element =>
                        element.ElementId == expectedElement)) return false;
                var index = Array.FindIndex(successor.OwnElements.ToArray(), element =>
                    element.ElementId == expectedElement);
                return index >= 0 && decision.OwnElementIndex == index;
            }
            return Matches(first, CampaignActionAudience.Axis)
                && Matches(second, CampaignActionAudience.Commonwealth)
                && first.EventKind == second.EventKind
                && first.EventContractVersion == second.EventContractVersion
                && first.ActionKind == second.ActionKind
                && Classified(first, axis) && Classified(second, commonwealth);
        }
        catch (Exception error) when (error is System.Text.Json.JsonException
            or ArgumentException or InvalidOperationException or KeyNotFoundException
            or OverflowException)
        {
            return false;
        }
    }

    private static string Hash(ReadOnlySpan<byte> bytes) =>
        $"sha256:{Convert.ToHexStringLower(SHA256.HashData(bytes))}";

    private static bool ValidHash(string? value) => value is { Length: 71 }
        && value.StartsWith("sha256:", StringComparison.Ordinal)
        && value.AsSpan(7).ToArray().All(static character =>
            character is >= '0' and <= '9' or >= 'a' and <= 'f');

    private SideState AdvanceSide(CampaignHostPrivateSideCut source,
        CampaignAudienceOutcomeDecision? decision)
    {
        sides.TryGetValue(source.Audience, out var prior);
        var (diary, cursor, appended) = AdvanceDiary(source, decision, prior);
        var contacts = source.Contacts.Select(contact =>
        {
            var id = prior?.Contacts.GetValueOrDefault(contact.RepresentationId)
                ?? Guid.NewGuid().ToString("D");
            return new TemporaryPrivateContact(id, contact.RepresentationId,
                contact.LocationId, contact.ExertsZoc);
        }).ToArray();
        var contactMap = source.Contacts.Zip(contacts).ToDictionary(
            pair => pair.First.RepresentationId, pair => pair.Second.ContactId,
            StringComparer.Ordinal);
        var fingerprint = Fingerprint(source, contacts);
        var changed = prior is null || appended || !string.Equals(prior.Fingerprint, fingerprint,
            StringComparison.Ordinal);
        var revision = prior is null ? 1 : prior.Cut.AudienceRevision + (changed ? 1 : 0);
        var token = prior is not null && !changed ? prior.Cut.ActionSetToken : Guid.NewGuid().ToString("D");
        var oldActions = prior?.Cut.Actions.ToDictionary(ActionIdentity,
            a => a.OutwardActionId, StringComparer.Ordinal);
        var actions = source.Actions.Select(action => new TemporaryPrivateAction(
            oldActions?.GetValueOrDefault(ActionIdentity(action)) ?? Guid.NewGuid().ToString("D"),
            action.CoreActionId, action.Kind, action.ElementId,
            action.TargetLocationId, action.OpportunityId)).ToArray();
        var cut = new TemporaryPrivateAudienceCut(source.CampaignId, source.StateVersion,
            source.RulesetHash, source.ScenarioId, source.PositionId, source.GameTurn,
            source.PhaseId, source.StageId, source.Audience, revision, token,
            source.Locations.Select(location => new TemporaryPrivateLocation(
                location.LocationId, location.TerrainId)).ToArray(),
            source.OwnElements.Select(element => new TemporaryPrivateOwnElement(
                element.ElementId, element.FormationId, element.LocationId,
                element.ReserveStatus)).ToArray(), contacts, actions, SourcePin,
            source.DecisionStateKind)
        {
            Diary = diary,
            NextDiaryCursor = cursor,
        };
        return new SideState(cut, fingerprint, contactMap);
    }

    private (TemporaryPrivateDiaryEntry[] Entries, ulong Cursor, bool Appended) AdvanceDiary(
        CampaignHostPrivateSideCut source, CampaignAudienceOutcomeDecision? decision,
        SideState? prior)
    {
        var entries = prior?.Cut.Diary ?? [];
        var cursor = prior?.Cut.NextDiaryCursor ?? 0;
        if (decision is null) return (entries, cursor, false);
        if (decision.Audience != source.Audience
            || decision.SuccessorStateVersion != source.StateVersion)
            throw new InvalidOperationException("Diary proof does not match the side cut.");
        if (decision.ProposedCopyKey is null)
        {
            if (decision.Disposition == CampaignAudienceOutcomeDisposition.Unavailable
                && decision.ReasonCode == "own-element-unavailable"
                && decision.ActionKind == "move-reacting-element"
                && decision.EventKind == "reacting-element-moved"
                && decision.Audience == decision.ActingAudience
                && decision.OwnElementIndex is null
                && source.DecisionStateKind == "reacting"
                && source.OwnElements.Count == 0)
                return (entries, cursor, false);
            if (decision.Disposition != CampaignAudienceOutcomeDisposition.Silent
                || decision.ReasonCode != "known-silent"
                || decision.OwnElementIndex is not null)
                throw new InvalidOperationException("Unsupported silent diary decision.");
            return (entries, cursor, false);
        }
        if (decision.ActingAudience != source.Audience
            || decision.Disposition != CampaignAudienceOutcomeDisposition.Unavailable
            || decision.ReasonCode != "copy-catalog-unapproved")
            throw new InvalidOperationException("Unsupported diary entry decision.");
        var copy = TemporaryPlayerViewProjection.LookupDiaryCopy(SourcePin,
            TemporaryPlayerViewProjection.DiaryCatalogVersion, "en-CA",
            decision.ProposedCopyKey)
            ?? throw new InvalidOperationException("No approved diary copy.");
        string? formation = null;
        if (copy.OwnFormationLink)
        {
            var index = decision.OwnElementIndex
                ?? throw new InvalidOperationException("Missing own formation proof.");
            if (index < 0 || index >= source.OwnElements.Count)
                throw new InvalidOperationException("Invalid own formation proof.");
            formation = source.OwnElements[index].FormationId;
            if (string.IsNullOrWhiteSpace(formation))
                throw new InvalidOperationException("Invalid own formation proof.");
        }
        else if (decision.OwnElementIndex is not null)
            throw new InvalidOperationException("Unapproved own formation link.");
        var next = checked(cursor + 1);
        var entry = new TemporaryPrivateDiaryEntry(Guid.NewGuid().ToString("D"), next,
            copy.Title, copy.Body, formation);
        return (entries.Append(entry).TakeLast(50).ToArray(), next, true);
    }

    private static string Fingerprint(CampaignHostPrivateSideCut cut,
        TemporaryPrivateContact[] contacts)
    {
        using var stream = new MemoryStream();
        using (var writer = new System.Text.Json.Utf8JsonWriter(stream))
        {
            System.Text.Json.JsonSerializer.Serialize(writer, new
            {
                cut.PositionId,
                cut.GameTurn,
                cut.PhaseId,
                cut.StageId,
                cut.DecisionStateKind,
                cut.Locations,
                cut.OwnElements,
                Contacts = contacts.Select(c => new { c.ContactId, c.LocationId, c.ExertsZoc }),
                Actions = cut.Actions.Select(a => new
                {
                    a.Kind,
                    a.ElementId,
                    a.TargetLocationId,
                    a.OpportunityId,
                }),
            });
        }
        return Convert.ToHexStringLower(SHA256.HashData(stream.ToArray()));
    }

    private static string ActionIdentity(CampaignHostPrivateAction action) =>
        System.Text.Json.JsonSerializer.Serialize(new
        {
            action.Kind,
            action.ElementId,
            action.TargetLocationId,
            action.OpportunityId,
        });

    private static string ActionIdentity(TemporaryPrivateAction action) =>
        System.Text.Json.JsonSerializer.Serialize(new
        {
            action.Kind,
            action.ElementId,
            action.TargetLocationId,
            action.OpportunityId,
        });

    private static bool ValidPair(string campaignId, string rulesetHash, string scenarioId,
        CampaignHostPrivateSideCut? axis, CampaignHostPrivateSideCut? commonwealth,
        long priorVersion) =>
        axis is not null && commonwealth is not null
        && axis.Audience == CampaignActionAudience.Axis
        && commonwealth.Audience == CampaignActionAudience.Commonwealth
        && axis.CampaignId == campaignId && commonwealth.CampaignId == campaignId
        && axis.RulesetHash == rulesetHash && commonwealth.RulesetHash == rulesetHash
        && axis.ScenarioId == scenarioId && commonwealth.ScenarioId == scenarioId
        && axis.PositionId == commonwealth.PositionId
        && axis.GameTurn == commonwealth.GameTurn
        && axis.PhaseId == commonwealth.PhaseId
        && axis.StageId == commonwealth.StageId
        && axis.StateVersion == commonwealth.StateVersion
        && axis.Actions.Select(ActionIdentity).Distinct(StringComparer.Ordinal).Count() == axis.Actions.Count
        && commonwealth.Actions.Select(ActionIdentity).Distinct(StringComparer.Ordinal).Count()
            == commonwealth.Actions.Count
        && axis.StateVersion == priorVersion + 1;

    private sealed record SideState(TemporaryPrivateAudienceCut Cut, string Fingerprint,
        Dictionary<string, string> Contacts);
}

// Contains private Core keys for later trusted translation. Never serialize this as HTTP JSON.
[GenerateSerializer]
public sealed record TemporaryPrivateAudienceCut(
    [property: Id(0)] string CampaignId, [property: Id(1)] long StateVersion,
    [property: Id(2)] string RulesetHash, [property: Id(3)] string ScenarioId,
    [property: Id(4)] string PositionId, [property: Id(5)] int GameTurn,
    [property: Id(6)] string PhaseId, [property: Id(7)] string StageId,
    [property: Id(8)] CampaignActionAudience Audience,
    [property: Id(9)] long AudienceRevision,
    [property: Id(10)] string ActionSetToken,
    [property: Id(11)] TemporaryPrivateLocation[] Locations,
    [property: Id(12)] TemporaryPrivateOwnElement[] OwnElements,
    [property: Id(13)] TemporaryPrivateContact[] Contacts,
    [property: Id(14)] TemporaryPrivateAction[] Actions,
    [property: Id(15)] string SourcePin,
    [property: Id(16)] string DecisionStateKind)
{
    [Id(17)] public TemporaryPrivateDiaryEntry[] Diary { get; init; } = [];
    [Id(18)] public ulong NextDiaryCursor { get; init; }
}
[GenerateSerializer]
public sealed record TemporaryPrivateDiaryEntry([property: Id(0)] string EntryId,
    [property: Id(1)] ulong AudienceCursor, [property: Id(2)] string Title,
    [property: Id(3)] string Body, [property: Id(4)] string? FormationId);
[GenerateSerializer]
public sealed record TemporaryPrivateLocation([property: Id(0)] string LocationId,
    [property: Id(1)] string TerrainId);
[GenerateSerializer]
public sealed record TemporaryPrivateOwnElement([property: Id(0)] string ElementId,
    [property: Id(1)] string FormationId, [property: Id(2)] string LocationId,
    [property: Id(3)] string ReserveStatus);
[GenerateSerializer]
public sealed record TemporaryPrivateContact([property: Id(0)] string ContactId,
    [property: Id(1)] string RepresentationId, [property: Id(2)] string LocationId,
    [property: Id(3)] bool ExertsZoc);
[GenerateSerializer]
public sealed record TemporaryPrivateAction([property: Id(0)] string OutwardActionId,
    [property: Id(1)] string CoreActionId, [property: Id(2)] string Kind,
    [property: Id(3)] string? ElementId, [property: Id(4)] string? TargetLocationId,
    [property: Id(5)] string? OpportunityId);
