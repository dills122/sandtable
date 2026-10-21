using System.Security.Cryptography;
using System.Text;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Visibility;

namespace Cna.Core.Campaigns;

// The captor's next normal-campaign decision. Routes and resource checks are
// derived from the accepted V24 World, never from an owner-supplied path.
internal sealed record CampaignNativeCombatCustodyChoiceV25(
    string Kind, string ActionId, IReadOnlyList<string> Route);

internal sealed record CampaignNativeCombatCustodyCutV25(
    string CampaignId, long AuthorityVersion, string SettlementHash,
    string CaptorElementId, string VictimElementId, string LotId,
    int Quantity, IReadOnlyList<CampaignNativeCombatCustodyChoiceV25> Choices)
{
    internal static CampaignNativeCombatCustodyCutV25? Project(
        CampaignNativeCombatSettlementV24 settlement,
        CampaignWorldSnapshotV6 world,
        CampaignContentContext context,
        CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(settlement);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(contacts);
        var artifact = context.ArtifactV6
            ?? throw new InvalidOperationException("Custody requires normal Content 6.");
        if (contacts.CampaignId != settlement.CampaignId
            || contacts.AuthorityVersion != settlement.StateVersion
            || !CampaignWorldV6Validator.IsValid(world, artifact, context.Scenario)
            || settlement.SettledWorldHash != CampaignNativeCombatPaidWorldV22.Hash(world))
            throw new InvalidOperationException("Custody requires the accepted settled World.");
        if (settlement.PendingCaptives.Count == 0) return null;
        if (settlement.PendingCaptives.Count != 1)
            throw new InvalidOperationException("Multiple captive lots need an ordered custody owner cut.");

        var lot = settlement.PendingCaptives[0];
        var captor = world.Elements.Single(value => value.ElementId == lot.CaptorElementId);
        var victim = world.Elements.Single(value => value.ElementId == lot.VictimElementId);
        var content = artifact.Definition.LegacyDefinition;
        var captorFacts = content.Elements.Single(value => value.ElementId == captor.ElementId);
        var victimFacts = content.Elements.Single(value => value.ElementId == victim.ElementId);
        if (captorFacts.SideId == victimFacts.SideId
            || !victim.Components.Any(value => value.ComponentId == lot.VictimComponentId)
            || lot.Quantity is < 1 or > 10)
            throw new InvalidOperationException("Custody lot does not bind opposing combatants.");
        var side = captorFacts.SideId switch
        {
            "axis" => LandSide.Axis,
            "commonwealth" => LandSide.Commonwealth,
            _ => throw new InvalidOperationException("Unknown captor side."),
        };
        var visibleVictim = contacts.CurrentTargetsFor(side,
            captorFacts.ParentFormationId).Any(target =>
            target.ApparentLocationId == victim.CurrentLocationId
            && world.Representations.Any(representation =>
                representation.BoundElementIds.Contains(victim.ElementId)
                && representation.RepresentationId == contacts.ResolvePrivateTarget(
                    side, captorFacts.ParentFormationId, target.ContactHandle)));
        if (!visibleVictim)
            throw new InvalidOperationException(
                "Custody route needs a current captor contact with the victim.");

        var settlementHash = Convert.ToHexStringLower(
            SHA256.HashData(settlement.SerializeCanonical()));
        var choices = new List<CampaignNativeCombatCustodyChoiceV25>(2);
        var guardRoute = ShortestRoute(content.Edges, lot.OriginLocationId,
            captor.CurrentLocationId);
        var donorToe = captor.Components.Single().CurrentToe;
        if (guardRoute is not null && guardRoute.Count - 1 <= 3
            && lot.Quantity <= 5
            && donorToe > 1
            && !guardRoute.Skip(1).Contains(victim.CurrentLocationId, StringComparer.Ordinal))
            choices.Add(Choice("relocate-and-guard", guardRoute));

        var escapeRoute = ShortestRoute(content.Edges, lot.OriginLocationId,
            victim.CurrentLocationId);
        if (escapeRoute is not null && escapeRoute.Count - 1 <= 4)
        {
            long cost = 0;
            var supported = true;
            foreach (var locationId in escapeRoute.Skip(1))
            {
                var terrain = content.Locations.Single(value =>
                    value.LocationId == locationId).TerrainId;
                var move = Cna1979Movement.LookupTerrain(terrain,
                    Cna1979Movement.NonMotorizedMobilityId);
                if (terrain != "land.terrain.clear" || !move.IsSupported
                    || move.Value.Cost.Denominator != 1)
                {
                    supported = false;
                    break;
                }
                cost = checked(cost + move.Value.Cost.Numerator);
            }
            if (supported && cost <= 8)
                choices.Add(Choice("leave-unguarded", escapeRoute));
        }

        if (choices.Count == 0)
            throw new InvalidOperationException("Captive lot has no source-legal disposition.");
        return new(settlement.CampaignId, settlement.StateVersion,
            "sha256:" + settlementHash, captor.ElementId, victim.ElementId,
            "combat.captive." + settlementHash, lot.Quantity,
            Array.AsReadOnly(choices.ToArray()));

        CampaignNativeCombatCustodyChoiceV25 Choice(
            string kind, IReadOnlyList<string> route)
        {
            // Stable across private settlement changes that leave the owner's
            // quantity, route and offer semantics unchanged. V25 replay still
            // binds the accepted choice to the exact settlement hash.
            var payload = Encoding.UTF8.GetBytes(string.Join('\0',
                [settlement.CampaignId, captor.ElementId,
                    lot.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    kind, .. route]));
            return new(kind, "sha256:" + Convert.ToHexStringLower(
                SHA256.HashData(payload)), route);
        }
    }

    private static System.Collections.ObjectModel.ReadOnlyCollection<string>? ShortestRoute(
        IReadOnlyList<ContentHexEdge> edges, string start, string end)
    {
        var graph = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var edge in edges)
        {
            Add(edge.FirstLocationId, edge.SecondLocationId);
            Add(edge.SecondLocationId, edge.FirstLocationId);
        }
        var queue = new Queue<string[]>();
        var seen = new HashSet<string>(StringComparer.Ordinal) { start };
        queue.Enqueue([start]);
        while (queue.TryDequeue(out var path))
        {
            if (path[^1] == end)
                return Array.AsReadOnly(path);
            if (!graph.TryGetValue(path[^1], out var neighbors)) continue;
            foreach (var next in neighbors.Order(StringComparer.Ordinal))
                if (seen.Add(next)) queue.Enqueue([.. path, next]);
        }
        return null;

        void Add(string from, string to)
        {
            if (!graph.TryGetValue(from, out var neighbors))
                graph[from] = neighbors = [];
            neighbors.Add(to);
        }
    }
}
