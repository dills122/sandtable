using System.Security.Cryptography;
using System.Text;
using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Content;

// Candidate values only: no authoritative world, event, or persistence is produced.
internal sealed class CampaignDeferredWorldDraft
{
    private CampaignDeferredWorldDraft(CampaignElementState[] elements, CampaignMapRepresentationState[] representations)
    {
        Elements = Array.AsReadOnly(elements);
        Representations = Array.AsReadOnly(representations);
    }

    public IReadOnlyList<CampaignElementState> Elements { get; }
    public IReadOnlyList<CampaignMapRepresentationState> Representations { get; }

    public static CampaignDeferredWorldDraft Create(CampaignArrivalDraftIdentity identity,
        CampaignWorldSnapshot world, string groupId, LandSide actor, int turn, int stage, string destination)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(world);
        var group = identity.Groups.SingleOrDefault(candidate => candidate.GroupId == groupId)
            ?? throw new ArgumentException("Unknown arrival group.", nameof(groupId));
        if (group.Owner != actor || group.GameTurn != turn || group.OperationStage != stage
            || !identity.Destinations[groupId].Contains(destination, StringComparer.Ordinal))
            throw new ArgumentException("Arrival does not match its scheduled slot or destination.");

        var existingElements = world.Elements.Select(element => element.ElementId).ToHashSet(StringComparer.Ordinal);
        var existingRepresentations = world.Representations.Select(rep => rep.RepresentationId).ToHashSet(StringComparer.Ordinal);
        var existingBindings = world.Representations.SelectMany(rep => rep.BoundElementIds).ToHashSet(StringComparer.Ordinal);
        var definitions = identity.Pack.Elements.ToDictionary(element => element.ElementId, StringComparer.Ordinal);
        var elements = new List<CampaignElementState>();
        var representations = new List<CampaignMapRepresentationState>();
        foreach (var id in group.UnitIds)
        {
            var representationId = RepresentationId(identity.Hash, id);
            if (existingElements.Contains(id) || existingBindings.Contains(id) || existingRepresentations.Contains(representationId))
                throw new ArgumentException("Arrival elements and representations must not already exist in the world.", nameof(world));
            var cohort = definitions[id].BreakdownVehicleCohort;
            elements.Add(new CampaignElementState(id, destination, CampaignElementReserveStatus.None,
                new CampaignElementOperationalState(turn, stage, CapabilityPointAmount.Zero, 0,
                    cohort is null ? null : new CampaignVehicleBreakdownState(cohort.CohortId,
                        BreakdownPointAmount.Zero, BreakdownPointAmount.Zero, null, cohort.WorkingPointCount, 0))));
            representations.Add(new CampaignMapRepresentationState(representationId, destination,
                CampaignMapRepresentationBindingKind.IndependentElement, [id]));
        }
        return new CampaignDeferredWorldDraft(elements.ToArray(), representations.ToArray());
    }

    private static string RepresentationId(string identityHash, string elementId)
    {
        var input = Encoding.UTF8.GetBytes("sandtable.arrival-representation.v1\0" + identityHash + "\0" + elementId);
        return "arrival-representation." + Convert.ToHexStringLower(SHA256.HashData(input));
    }
}
