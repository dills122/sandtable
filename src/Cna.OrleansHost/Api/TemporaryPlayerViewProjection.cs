using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Cna.Core.Actions;

namespace Cna.OrleansHost.Api;

// Development fixture presentation data. The public layout remains truck-only;
// private fixed-copy lookup has a separately approved Reaction source pin.
// Neither presentation path owns authority state.
internal sealed class TemporaryPlayerViewProjection
{
    internal const string CatalogVersion = "breakdown-truck-maproom.v1";
    internal const string DiaryCatalogVersion = "breakdown-truck-maproom.diary.v2";
    private const string SourcePin =
        "17f3e6047f34b5bf6f5f809055863b664a4bd82a8481db83ee83e0ae5cad3a2a|" +
        "breakdown-truck-lab|" +
        "sha256:e6631e81ad8f97e39fd9d7eec93bad7fe2b39db4d2d3059ed94a02dd4093e7a3|" +
        "sha256:646e76e69ecceb82216b37d84e950928099acd8a3cb04b51526d0fe631e512ee";
    // Approved private diary-only source scope. This does not admit Map(...), which
    // remains restricted to the truck fixture's initial read-only presentation.
    private const string ReactionDiarySourcePin =
        "17f3e6047f34b5bf6f5f809055863b664a4bd82a8481db83ee83e0ae5cad3a2a|" +
        "breakdown-reaction-lab|" +
        "sha256:ed4e33358ede11f3c25661798add9088f11d8853053dbf7c8992ba5f92d9a36e|" +
        "sha256:1e10648bb65f7ec6d4ad3f33bbf612f51c11a927ebc2e8b110aa5544af3918dd";

    private readonly byte[] formationKey = RandomNumberGenerator.GetBytes(32);

    // Fixed owner-approved text only. A caller must first prove the accepted step, both
    // committed side cuts and its head; this lookup never grants diary eligibility.
    internal static TemporaryDiaryCatalogCopy? LookupDiaryCopy(
        string sourcePin, string catalogVersion, string locale, string copyKey)
    {
        if ((sourcePin != SourcePin && sourcePin != ReactionDiarySourcePin)
            || catalogVersion != DiaryCatalogVersion
            || locale != "en-CA") return null;
        return copyKey switch
        {
            "diary.initiativeOrder" => new("Initiative order recorded",
                "Your choice of acting order was recorded.", false),
            "diary.reserveDesignated" => new("Reserve assigned",
                "A formation you control was assigned to reserve.", true),
            "diary.reserveCompleted" => new("Reserve choices complete",
                "Your reserve choices were completed.", false),
            "diary.elementMoved" => new("Formation moved",
                "A formation you control moved.", true),
            "diary.movementStopped" => new("Movement stopped",
                "Movement for one of your formations stopped.", false),
            "diary.reactionMoved" => new("Reaction move recorded",
                "A formation you control made a reaction move.", true),
            "diary.reactionCompleted" => new("Reaction complete",
                "Your reaction opportunity was completed.", false),
            "diary.reactionDeclined" => new("Reaction declined",
                "You declined the reaction opportunity.", false),
            "diary.movementCompleted" => new("Movement complete",
                "Your movement segment was completed.", false),
            _ => null,
        };
    }

    private static readonly Dictionary<string, (string Label, double X, double Y)> Locations =
        new Dictionary<string, (string, double, double)>(StringComparer.Ordinal)
        {
            ["center"] = ("Center", 0, 0),
            ["east"] = ("East", 1, 0),
            ["north"] = ("North", 0, -1),
            ["north-east"] = ("North East", 1, -1),
            ["north-west"] = ("North West", -1, -1),
            ["south"] = ("South", 0, 1),
            ["south-east"] = ("South East", 1, 1),
            ["south-west"] = ("South West", -1, 1),
            ["west"] = ("West", -1, 0),
        };

    private static readonly Dictionary<string, (CampaignActionAudience Side, string Label,
        string Kind)> Formations =
        new Dictionary<string, (CampaignActionAudience, string, string)>(StringComparer.Ordinal)
        {
            ["axis-infantry.formation"] = (CampaignActionAudience.Axis, "Axis infantry", "Infantry"),
            ["axis-truck.formation"] = (CampaignActionAudience.Axis, "Axis truck", "Truck convoy"),
            ["commonwealth-infantry.formation"] =
                (CampaignActionAudience.Commonwealth, "Commonwealth infantry", "Infantry"),
            ["commonwealth-truck.formation"] =
                (CampaignActionAudience.Commonwealth, "Commonwealth truck", "Truck convoy"),
        };

    // Proposed MAP-10a public read scope: exact early positions in the authenticated
    // truck fixture; version 10 admits only its Axis-first reserve cut. The tracker/grain prove accepted
    // steps and paired cuts; this table only admits their presentation shapes.
    private static readonly Dictionary<long, (string Position, string Phase, string Stage)>
        SuccessorPositions = new()
        {
            [2] = ("land.position.naval-convoy.schedule",
                "land.phase.naval-convoy-schedule", "land.stage.naval-convoy"),
            [3] = ("land.position.naval-convoy.tactical-shipping",
                "land.phase.tactical-shipping", "land.stage.naval-convoy"),
            [4] = ("land.position.operation-1.initiative-declaration",
                "land.phase.initiative-declaration", "land.stage.operation"),
            [5] = ("land.position.operation-1.weather-determination",
                "land.phase.weather-determination", "land.stage.operation"),
            [6] = ("land.position.operation-1.organization",
                "land.phase.organization", "land.stage.operation"),
            [7] = ("land.position.operation-1.naval-convoy-arrival",
                "land.phase.naval-convoy-arrival", "land.stage.operation"),
            [8] = ("land.position.operation-1.commonwealth-fleet.assignment",
                "land.phase.commonwealth-fleet", "land.stage.operation"),
            [9] = ("land.position.operation-1.commonwealth-fleet.repair",
                "land.phase.commonwealth-fleet", "land.stage.operation"),
            [10] = ("land.position.operation-1.first-player.reserve-designation",
                "land.phase.reserve-designation", "land.stage.operation"),
        };

    internal object? Map(TemporaryPrivateAudienceCut? cut, LocalSession session)
    {
        if (cut is null || session.CampaignId != cut.CampaignId
            || session.Generation is null || !Guid.TryParse(session.Generation, out _)
            || session.Seat != SideName(cut.Audience)
            || cut.SourcePin != SourcePin || cut.ScenarioId != "breakdown-truck-lab"
            || cut.RulesetHash != SourcePin[..64] || cut.StateVersion < 1
            || cut.AudienceRevision < 1 || cut.GameTurn < 0
            || cut.DecisionStateKind != "normal"
            || cut.Locations.Length != Locations.Count
            || cut.Locations.Select(l => l.LocationId).Distinct(StringComparer.Ordinal).Count()
                != Locations.Count
            || cut.Locations.Any(l => !Locations.ContainsKey(l.LocationId))
            || cut.OwnElements.Any(e => !Formations.TryGetValue(e.FormationId, out var known)
                || known.Side != cut.Audience || !Locations.ContainsKey(e.LocationId))
            || cut.Contacts.Any(c => !Locations.ContainsKey(c.LocationId)
                || !Guid.TryParse(c.ContactId, out _))
            || cut.Actions.Any(a => !Guid.TryParse(a.OutwardActionId, out _)
                || string.IsNullOrWhiteSpace(a.CoreActionId))
            || cut.Actions.Select(a => a.OutwardActionId)
                .Distinct(StringComparer.Ordinal).Count() != cut.Actions.Length
            || cut.Actions.Select(a => a.CoreActionId)
                .Distinct(StringComparer.Ordinal).Count() != cut.Actions.Length
            || !Guid.TryParse(cut.ActionSetToken, out _)
            || !SupportedSuccessor(cut))
            return null;

        var formations = cut.OwnElements.GroupBy(e => e.FormationId, StringComparer.Ordinal)
            .ToArray();
        if (formations.Any(group => group.Select(e => e.LocationId)
            .Distinct(StringComparer.Ordinal).Skip(1).Any()
            || group.Select(e => e.ReserveStatus).Distinct(StringComparer.Ordinal).Skip(1).Any()))
            return null;
        if (cut.OwnElements.Select(e => e.ElementId)
                .Distinct(StringComparer.Ordinal).Count() != cut.OwnElements.Length
            || cut.Contacts.Select(c => c.ContactId)
                .Distinct(StringComparer.Ordinal).Count() != cut.Contacts.Length)
            return null;
        var byElement = cut.OwnElements.ToDictionary(e => e.ElementId,
            e => e.FormationId, StringComparer.Ordinal);

        var actions = new List<object>();
        foreach (var action in cut.Actions)
        {
            var presentation = Action(action, byElement, cut.CampaignId, cut.Audience,
                session.Generation);
            if (presentation is null) return null;
            actions.Add(presentation);
        }
        var own = formations.Select(group =>
        {
            var info = Formations[group.Key];
            var id = FormationId(cut.CampaignId, cut.Audience, session.Generation, group.Key);
            var location = group.First().LocationId;
            var status = group.Select(e => e.ReserveStatus).Distinct(StringComparer.Ordinal).SingleOrDefault();
            return new
            {
                formationId = id,
                label = info.Label,
                kindLabel = info.Kind,
                locationId = location,
                orderStatusLabel = status switch
                {
                    "None" => "No reserve designation",
                    "ReserveI" => "Reserve I",
                    "ReserveII" => "Reserve II",
                    _ => null,
                }
            };
        }).ToArray();
        if (own.Any(f => f.orderStatusLabel is null)) return null;
        var diary = ProjectDiary(cut);
        if (diary is null) return null;
        var ledger = own.Select(f => new
        {
            f.formationId,
            f.label,
            f.kindLabel,
            locationLabel = Locations[f.locationId].Label,
            f.orderStatusLabel,
        }).ToArray();
        return new
        {
            contractVersion = 1,
            campaignId = cut.CampaignId,
            sessionGeneration = session.Generation,
            seat = session.Seat,
            audienceRevision = cut.AudienceRevision.ToString(CultureInfo.InvariantCulture),
            rulesetHash = cut.RulesetHash,
            position = new
            {
                positionId = cut.PositionId,
                turn = cut.GameTurn.ToString(CultureInfo.InvariantCulture),
                phaseId = cut.PhaseId,
                stageId = cut.StageId,
                decision = actions.Count > 0 ? "human" : "unsupported-phase",
            },
            locations = cut.Locations.Select(l => new
            {
                locationId = l.LocationId,
                label = Locations[l.LocationId].Label,
                x = Locations[l.LocationId].X,
                y = Locations[l.LocationId].Y,
            }).ToArray(),
            ownFormations = own,
            apparentContacts = cut.Contacts.Select(c => new
            {
                contactId = c.ContactId,
                label = "Unidentified presence",
                locationId = c.LocationId,
            }).ToArray(),
            legalActions = actions,
            formationLedger = ledger,
            warDiary = diary,
            nextDiaryCursor = cut.NextDiaryCursor.ToString(CultureInfo.InvariantCulture),
            actionSetToken = cut.ActionSetToken,
        };
    }

    private static bool SupportedSuccessor(TemporaryPrivateAudienceCut cut)
    {
        if (cut.StateVersion == 1)
            return cut.Diary.Length == 0 && cut.NextDiaryCursor == 0;
        if (!SuccessorPositions.TryGetValue(cut.StateVersion, out var expected)
            || cut.GameTurn != 1 || cut.DecisionStateKind != "normal"
            || cut.PositionId != expected.Position || cut.PhaseId != expected.Phase
            || cut.StageId != expected.Stage)
            return false;
        var kinds = cut.Actions.Select(action => action.Kind).Order(StringComparer.Ordinal)
            .ToArray();
        var expectedKinds = cut.Audience == CampaignActionAudience.Axis
            ? cut.StateVersion switch
            {
                4 => new[] { "act-first", "act-last" },
                10 => new[] { "complete-reserve-designation", "designate-reserve" },
                _ => [],
            }
            : [];
        if (!kinds.SequenceEqual(expectedKinds.Order(StringComparer.Ordinal)))
            return false;
        // No reserve action has yet been accepted in this bounded trace.
        // One owner-only initiative diary entry appears after the human step.
        var expectedCursor = cut.Audience == CampaignActionAudience.Axis
            && cut.StateVersion >= 5 ? 1UL : 0UL;
        return cut.NextDiaryCursor == expectedCursor
            && cut.Diary.Length == (int)expectedCursor;
    }

    private static object[]? ProjectDiary(TemporaryPrivateAudienceCut cut)
    {
        var entries = new List<object>();
        ulong previous = 0;
        foreach (var entry in cut.Diary)
        {
            if (!Guid.TryParseExact(entry.EntryId, "D", out _)
                || entry.AudienceCursor != ++previous)
                return null;
            var approved = LookupDiaryCopy(cut.SourcePin, DiaryCatalogVersion, "en-CA",
                "diary.initiativeOrder");
            if (approved is null || entry.Title != approved.Title
                || entry.Body != approved.Body || entry.FormationId is not null)
                return null;
            entries.Add(new
            {
                entryId = entry.EntryId,
                audienceCursor = entry.AudienceCursor.ToString(CultureInfo.InvariantCulture),
                title = entry.Title,
                body = entry.Body,
            });
        }
        return previous == cut.NextDiaryCursor ? entries.ToArray() : null;
    }

    private object? Action(TemporaryPrivateAction action,
        Dictionary<string, string> byElement, string campaign, CampaignActionAudience side,
        string generation)
    {
        string? formationId = null;
        string label;
        switch (action.Kind)
        {
            case "act-first" when action.ElementId is null && action.TargetLocationId is null
                && action.OpportunityId is null:
                label = "Act first";
                break;
            case "act-last" when action.ElementId is null && action.TargetLocationId is null
                && action.OpportunityId is null:
                label = "Act last";
                break;
            case "complete-reserve-designation"
                when action.ElementId is null && action.TargetLocationId is null
                && action.OpportunityId is null:
                label = "Complete reserve designation";
                break;
            case "designate-reserve" when action.ElementId is not null
                && action.TargetLocationId is null && action.OpportunityId is null
                && byElement.TryGetValue(action.ElementId, out var rawFormation):
                formationId = FormationId(campaign, side, generation, rawFormation);
                label = "Designate reserve";
                break;
            default:
                return null;
        }
        return formationId is null
            ? new
            {
                actionId = action.OutwardActionId,
                actionContractVersion = 1,
                kind = action.Kind,
                label
            } as object
            : new
            {
                actionId = action.OutwardActionId,
                actionContractVersion = 1,
                kind = action.Kind,
                label,
                formationId
            };
    }

    private string FormationId(string campaign, CampaignActionAudience side,
        string generation, string privateId)
    {
        var message = Encoding.UTF8.GetBytes(
            $"formation\0{campaign}\0{side}\0{generation}\0{privateId}");
        return new Guid(HMACSHA256.HashData(formationKey, message).AsSpan(0, 16)).ToString("D");
    }

    private static string? SideName(CampaignActionAudience audience) => audience switch
    {
        CampaignActionAudience.Axis => "axis",
        CampaignActionAudience.Commonwealth => "commonwealth",
        _ => null,
    };
}

internal sealed record TemporaryDiaryCatalogCopy(string Title, string Body,
    bool OwnFormationLink);
