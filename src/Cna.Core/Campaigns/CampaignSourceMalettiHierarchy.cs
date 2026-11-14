using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Campaigns;

/// <summary>Explicit, provisional source setup hierarchy for the C3617 stack.
/// Does not certify complete scenario setup or native grouped-formation movement.</summary>
internal sealed class CampaignSourceMalettiHierarchy
{
    internal const string PolicyId = "graziani.maletti-baseline-hierarchy.v1";
    private static readonly string[] MotorBattalions =
        ["1-1-libyan-motor-infantry", "3-1-libyan-motor-infantry", "5-1-libyan-motor-infantry"];
    private static readonly (string Id, string Parent, string? Component, int? Toe)[] Attachments =
    [
        ("1-libyan-motor-regiment-hq", "maletti-hq", null, null),
        ("1-1-libyan-motor-infantry", "1-libyan-motor-regiment-hq", "infantry", 5),
        ("3-1-libyan-motor-infantry", "1-libyan-motor-regiment-hq", "infantry", 5),
        ("5-1-libyan-motor-infantry", "1-libyan-motor-regiment-hq", "infantry", 5),
        ("5-libyan-regiment-hq", "maletti-hq", null, null),
        ("17-5-libyan-infantry", "5-libyan-regiment-hq", "infantry", 5),
        ("19-5-libyan-infantry", "5-libyan-regiment-hq", "infantry", 5),
        ("sahariano-regiment-hq", "maletti-hq", null, null),
        ("1-sahariano-camel", "sahariano-regiment-hq", "reconnaissance", 5),
        ("2-sahariano-camel", "sahariano-regiment-hq", "reconnaissance", 5),
        ("3-sahariano-camel", "sahariano-regiment-hq", "reconnaissance", 5),
        ("1-maletti-artillery", "maletti-hq", "75-27-gun", 3),
        ("2-maletti-artillery", "maletti-hq", "75-18-gun-howitzer", 2),
        ("ii-medium-tank", "maletti-hq", "m11-39", 7),
    ];
    private CampaignSourceMalettiHierarchy(string freeHash, string corpsHash, bool confirmed, string? detachedId)
    { FreeHash = freeHash; CorpsHash = corpsHash; Confirmed = confirmed; DetachedId = detachedId; }
    internal static IReadOnlyList<(string Id, string Parent, string? Component, int? Toe)> SourceAttachments => Array.AsReadOnly(Attachments.ToArray());
    internal string FreeHash { get; }
    internal string CorpsHash { get; }
    internal bool Confirmed { get; }
    internal string? DetachedId { get; }

    internal static CampaignSourceMalettiHierarchy Create(CampaignSourceAuthorityState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var free = state.AxisFreeArtillery ?? throw new InvalidOperationException("Source 1AR setup missing.");
        var corps = state.AxisCorpsArtillery ?? throw new InvalidOperationException("Source corps setup missing.");
        var result = new CampaignSourceMalettiHierarchy(free.Hash(), corps.Hash(), false, null);
        result.Validate(state);
        return result;
    }

    internal CampaignSourceMalettiHierarchy Confirm(CampaignSourceAuthorityState state)
    {
        Validate(state);
        if (Confirmed) throw new InvalidOperationException("Maletti baseline already selected.");
        return new(FreeHash, CorpsHash, true, null);
    }

    internal bool CanDetach(CampaignSourceAuthorityState state, string elementId)
    {
        Validate(state);
        return Confirmed && DetachedId is null && MotorBattalions.Contains(elementId, StringComparer.Ordinal);
    }

    internal CampaignSourceMalettiHierarchy Detach(CampaignSourceAuthorityState state, string elementId)
    {
        if (!CanDetach(state, elementId)) throw new InvalidOperationException("Maletti detachment unavailable.");
        return new(FreeHash, CorpsHash, true, elementId);
    }

    internal void Validate(CampaignSourceAuthorityState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var free = state.AxisFreeArtillery ?? throw new InvalidOperationException("Source 1AR setup missing.");
        var corps = state.AxisCorpsArtillery ?? throw new InvalidOperationException("Source corps setup missing.");
        var supplement = state.AxisSupplement ?? throw new InvalidOperationException("Axis supplement missing.");
        var battlefield = state.Battlefield ?? throw new InvalidOperationException("Source battlefield missing.");
        var priority = state.AxisPriorityMotorization ?? throw new InvalidOperationException("Axis priority setup missing.");
        if (free.Hash() != FreeHash || corps.Hash() != CorpsHash
            || supplement.Hash() != CampaignSourceAxisSupplementalForces.ContentHash
            || state.AxisFreeArtilleryWorld is null || state.AxisCorpsArtilleryWorld is null
            || free.PendingIds.Count != 0 || corps.PendingIds.Count != 0
            || free.Placements.Count != 4 || corps.Placements.Count != 2
            || free.Placements.Any(value => value.LocationId == "c3617")
            || corps.Placements.Any(value => value.LocationId == "c3617"))
            throw new InvalidOperationException("Free Axis source placements are incomplete or changed.");
        var active = state.World.World.Elements.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        if (free.Placements.Any(value => !active.Contains(value.ElementId)
                || !battlefield.Entries.Any(entry => entry.ElementId == value.ElementId && entry.LocationId == value.LocationId
                    && entry.AuthorityStatus == "source-1ar-active"))
            || corps.Placements.Any(value => !active.Contains(value.ElementId)
                || !battlefield.Entries.Any(entry => entry.ElementId == value.ElementId && entry.LocationId == value.LocationId
                    && entry.AuthorityStatus == "source-corps-artillery-active")))
            throw new InvalidOperationException("Free Axis placements do not match active source world.");
        var expected = Cna1979GrazianiSourceFacts.MalettiInitialChartCandidate.ElementIds.Order(StringComparer.Ordinal).ToArray();
        var actual = battlefield.Entries.Where(value => value.LocationId == "c3617").ToArray();
        if (expected.Length != 15 || !actual.Select(value => value.ElementId).Order(StringComparer.Ordinal).SequenceEqual(expected)
            || actual.Any(value => value.SideId != "axis"))
            throw new InvalidOperationException("Maletti source hex has missing or additional occupants.");
        if (Attachments.Length != 14 || Attachments.Select(value => value.Id).Distinct(StringComparer.Ordinal).Count() != 14
            || !Attachments.Select(value => value.Id).Append("maletti-hq").Order(StringComparer.Ordinal).SequenceEqual(expected)
            || supplement.Forces.Single(value => value.ElementId == "maletti-hq").InitialToes is not null
            || Attachments.Count(value => value.Parent == "1-libyan-motor-regiment-hq") != 3
            || Attachments.Count(value => value.Parent == "5-libyan-regiment-hq") != 2
            || Attachments.Count(value => value.Parent == "sahariano-regiment-hq") != 3
            || Attachments.Count(value => value.Parent == "maletti-hq") != 6)
            throw new InvalidOperationException("Maletti source hierarchy inventory differs from the pinned roster.");
        foreach (var (id, parent, component, toe) in Attachments)
        {
            var force = supplement.Forces.SingleOrDefault(value => value.ElementId == id);
            if (!expected.Contains(parent, StringComparer.Ordinal) || force is null || force.LocationId != "c3617"
                || (component is null ? force.InitialToes is not null
                    : force.InitialToes is null || force.InitialToes.Count != 1
                        || force.InitialToes[0].ComponentId != id + "." + component || force.InitialToes[0].Toe != toe))
                throw new InvalidOperationException("Maletti source hierarchy or known component strength differs.");
        }
        var priorityDetached = priority.DetachedTargets;
        if (DetachedId is null ? priorityDetached.Count != 0
            : priorityDetached.Count != 1 || priorityDetached[0] != DetachedId)
            throw new InvalidOperationException("Maletti hierarchy differs from retained detachment state.");
    }

    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("policyId", PolicyId);
            writer.WriteString("free1arHash", FreeHash); writer.WriteString("corpsHash", CorpsHash);
            writer.WriteString("scenarioHash", "sha256:fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66");
            writer.WriteString("axisChartsHash", "sha256:9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca");
            writer.WriteString("landRulesHash", "sha256:b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99");
            writer.WriteString("scenarioOverOaLxPolicy", "provisional-ii-m-only-maletti-lx-whole-bardia");
            writer.WriteString("repeated4ArPolicy", "provisional-one-unique-counter");
            writer.WriteString("xxviIdentity", "provisional-unique-artillery-counter");
            writer.WriteString("hierarchyPolicy", "owner-select-all-3-assigned-regiments-and-ii-m-attached");
            writer.WriteString("stackPolicy", "village-clear-limit6-full-division5-plus-one-battalion1");
            writer.WriteString("organizationRule", "19.32-three-regiments-each-max-three-battalions");
            writer.WriteString("attachmentRule", "19.5-two-extra-nonassigned-units-max-one-tank-ii-m-only");
            writer.WriteStartArray("attachments");
            foreach (var (id, parent, component, toe) in Attachments)
            {
                writer.WriteStartObject(); writer.WriteString("elementId", id); writer.WriteString("attachedParentId", parent);
                writer.WriteString("historicalAssignment", id == "ii-medium-tank"
                    ? "trivoli-regiment-libyan-tank-command" : "attached-parent");
                if (component is null) writer.WriteNull("knownComponentId"); else writer.WriteString("knownComponentId", id + "." + component);
                if (toe is null) writer.WriteNull("knownToe"); else writer.WriteNumber("knownToe", toe.Value);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteNumber("maxDetachedMotorBattalions", 1);
            writer.WriteBoolean("confirmed", Confirmed);
            if (DetachedId is null) writer.WriteNull("detachedId"); else writer.WriteString("detachedId", DetachedId);
            writer.WriteBoolean("scenarioSetupComplete", false);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
    internal string InitialHash() => new CampaignSourceMalettiHierarchy(FreeHash, CorpsHash, false, null).Hash();
}
