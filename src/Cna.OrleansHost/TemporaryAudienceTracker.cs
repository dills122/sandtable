using System.Security.Cryptography;
using Cna.Core.Actions;
using Cna.Core.Hosting;

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
    internal long StateVersion { get; private set; }
    internal bool IsAvailable { get; private set; } = true;

    private TemporaryAudienceTracker(string campaignId, string rulesetHash,
        string scenarioId, string setupHash, string contentHash)
    {
        CampaignId = campaignId;
        RulesetHash = rulesetHash;
        ScenarioId = scenarioId;
        SourcePin = $"{rulesetHash}|{scenarioId}|{setupHash}|{contentHash}";
    }

    internal static TemporaryAudienceTracker? Create(string campaignId, string rulesetHash,
        string scenarioId, string setupHash, string contentHash,
        CampaignHostPrivateSideCut? axis, CampaignHostPrivateSideCut? commonwealth)
    {
        if (!ValidPair(campaignId, rulesetHash, scenarioId, axis, commonwealth, 0)) return null;
        var tracker = new TemporaryAudienceTracker(campaignId, rulesetHash, scenarioId,
            setupHash, contentHash);
        tracker.AdvancePair(axis!, commonwealth!);
        return tracker;
    }

    internal bool Advance(CampaignHostPrivateSideCut? axis, CampaignHostPrivateSideCut? commonwealth)
    {
        if (!IsAvailable || !ValidPair(CampaignId, RulesetHash, ScenarioId,
                axis, commonwealth, StateVersion))
        {
            IsAvailable = false;
            return false;
        }
        AdvancePair(axis!, commonwealth!);
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

    private void AdvancePair(CampaignHostPrivateSideCut axis, CampaignHostPrivateSideCut commonwealth)
    {
        // Build both successor projections before publishing either side's committed head.
        var nextAxis = AdvanceSide(axis);
        var nextCommonwealth = AdvanceSide(commonwealth);
        sides[CampaignActionAudience.Axis] = nextAxis;
        sides[CampaignActionAudience.Commonwealth] = nextCommonwealth;
        StateVersion = axis.StateVersion;
    }

    private SideState AdvanceSide(CampaignHostPrivateSideCut source)
    {
        sides.TryGetValue(source.Audience, out var prior);
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
        var changed = prior is null || !string.Equals(prior.Fingerprint, fingerprint,
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
            source.DecisionStateKind);
        return new SideState(cut, fingerprint, contactMap);
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
            action.Kind, action.ElementId, action.TargetLocationId, action.OpportunityId,
        });

    private static string ActionIdentity(TemporaryPrivateAction action) =>
        System.Text.Json.JsonSerializer.Serialize(new
        {
            action.Kind, action.ElementId, action.TargetLocationId, action.OpportunityId,
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
    [property: Id(16)] string DecisionStateKind);
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
