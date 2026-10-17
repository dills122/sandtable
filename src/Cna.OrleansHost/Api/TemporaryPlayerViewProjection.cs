using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Cna.Core.Actions;

namespace Cna.OrleansHost.Api;

// Development fixture presentation data. This is a separately versioned public layout,
// admitted only with the exact Core setup/content pins below; it is not authority state.
internal sealed class TemporaryPlayerViewProjection
{
    internal const string CatalogVersion = "breakdown-truck-maproom.v1";
    private const string SourcePin =
        "17f3e6047f34b5bf6f5f809055863b664a4bd82a8481db83ee83e0ae5cad3a2a|" +
        "breakdown-truck-lab|" +
        "sha256:e6631e81ad8f97e39fd9d7eec93bad7fe2b39db4d2d3059ed94a02dd4093e7a3|" +
        "sha256:646e76e69ecceb82216b37d84e950928099acd8a3cb04b51526d0fe631e512ee";

    private readonly byte[] formationKey = RandomNumberGenerator.GetBytes(32);

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

    internal object? Map(TemporaryPrivateAudienceCut? cut, LocalSession session)
    {
        if (cut is null || session.CampaignId != cut.CampaignId
            || session.Generation is null || !Guid.TryParse(session.Generation, out _)
            || session.Seat != SideName(cut.Audience)
            || cut.SourcePin != SourcePin || cut.ScenarioId != "breakdown-truck-lab"
            || cut.RulesetHash != SourcePin[..64] || cut.StateVersion != 1
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
            || cut.Actions.Any(a => !Guid.TryParse(a.OutwardActionId, out _))
            || !Guid.TryParse(cut.ActionSetToken, out _))
            return null;

        var formations = cut.OwnElements.GroupBy(e => e.FormationId, StringComparer.Ordinal)
            .ToArray();
        if (formations.Any(group => group.Select(e => e.LocationId)
            .Distinct(StringComparer.Ordinal).Skip(1).Any()
            || group.Select(e => e.ReserveStatus).Distinct(StringComparer.Ordinal).Skip(1).Any()))
            return null;
        var byElement = cut.OwnElements.ToDictionary(e => e.ElementId,
            e => e.FormationId, StringComparer.Ordinal);
        if (byElement.Count != cut.OwnElements.Length) return null;

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
            warDiary = Array.Empty<object>(),
            nextDiaryCursor = "0",
            actionSetToken = cut.ActionSetToken,
        };
    }

    private object? Action(TemporaryPrivateAction action,
        Dictionary<string, string> byElement, string campaign, CampaignActionAudience side,
        string generation)
    {
        string? formationId = null;
        string label;
        switch (action.Kind)
        {
            case "act-first" when action.ElementId is null && action.TargetLocationId is null:
                label = "Act first";
                break;
            case "act-last" when action.ElementId is null && action.TargetLocationId is null:
                label = "Act last";
                break;
            case "complete-reserve-designation"
                when action.ElementId is null && action.TargetLocationId is null:
                label = "Complete reserve designation";
                break;
            case "designate-reserve" when action.ElementId is not null
                && action.TargetLocationId is null
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
