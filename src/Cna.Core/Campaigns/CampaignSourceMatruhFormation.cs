using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Campaigns;

internal sealed record CampaignSourceMatruhMember(string ElementId, string SourceCode, string AssignmentStatus);

/// <summary>Owner-selected source formation state; distinct from World6 independent counters.</summary>
internal sealed class CampaignSourceMatruhFormation
{
    internal const string PolicyId = "graziani.matruh-source-formation.v1";
    internal const string HeadquartersId = "matruh-garrison-hq";
    private static readonly IReadOnlyList<CampaignSourceMatruhMember> MembersData = Array.AsReadOnly(new[]
    {
        new CampaignSourceMatruhMember("1-essex", "m", "scenario-23-70-vs-oa-note-f-conflict"),
        new CampaignSourceMatruhMember("1-durham-light-infantry", "m", "scenario-23-70-vs-oa-note-f-conflict"),
        new CampaignSourceMatruhMember("1-south-staffordshires", "p", "unassigned-source"),
        new CampaignSourceMatruhMember("1-buffs", "p", "provisional-errata-assigned-to-matruh"),
        new CampaignSourceMatruhMember("1-hampshire", "p", "provisional-errata-assigned-to-matruh"),
    });
    private CampaignSourceMatruhFormation(string battlefieldHash, bool confirmed, string? detachedId)
    { BattlefieldHash = battlefieldHash; Confirmed = confirmed; DetachedId = detachedId; }
    internal static IReadOnlyList<CampaignSourceMatruhMember> Members => MembersData;
    internal static IReadOnlyList<string> UnitIds => Array.AsReadOnly(MembersData.Select(value => value.ElementId)
        .Append(HeadquartersId).Order(StringComparer.Ordinal).ToArray());
    internal string BattlefieldHash { get; }
    internal bool Confirmed { get; }
    internal string? DetachedId { get; }
    internal IReadOnlyList<string> AttachedIds => !Confirmed ? [] : Array.AsReadOnly(MembersData.Select(value => value.ElementId)
        .Where(id => id != DetachedId).Order(StringComparer.Ordinal).ToArray());

    internal static CampaignSourceMatruhFormation Create(CampaignSourceAuthorityState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var battlefield = state.Battlefield ?? throw new InvalidOperationException("Source battlefield missing.");
        if (state.Pregame.Source.Session.Content.Identity.Hash != CampaignPregameSourceCreation.SupportedContentHash
            || UnitIds.Any(id => battlefield.Entries.Any(entry => entry.ElementId == id)
                || battlefield.Pending.Any(group => group.ElementIds.Contains(id, StringComparer.Ordinal)))
            || battlefield.Entries.Any(entry => entry.LocationId == "d3714"))
            throw new InvalidOperationException("Matruh source formation overlaps retained battlefield.");
        return new(battlefield.Hash(), false, null);
    }
    internal CampaignSourceMatruhFormation Confirm(CampaignSourceAuthorityState state)
    {
        Validate(state);
        if (Confirmed) throw new InvalidOperationException("Matruh source baseline already selected.");
        return new(BattlefieldHash, true, null);
    }
    internal bool CanDetach(CampaignSourceAuthorityState state, string id)
    {
        Validate(state);
        return Confirmed && DetachedId is null && MembersData.Any(value => value.ElementId == id);
    }
    internal CampaignSourceMatruhFormation Detach(CampaignSourceAuthorityState state, string id)
    {
        if (!CanDetach(state, id)) throw new InvalidOperationException("Matruh detachment unavailable.");
        return new(BattlefieldHash, true, id);
    }
    internal void Validate(CampaignSourceAuthorityState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var battlefield = state.Battlefield ?? throw new InvalidOperationException("Source battlefield missing.");
        var entries = battlefield.Entries.Where(value => value.LocationId == "d3714").ToArray();
        if (entries.Length != 6 || !entries.Select(value => value.ElementId).Order(StringComparer.Ordinal).SequenceEqual(UnitIds)
            || entries.Any(value => value.SideId != "commonwealth"))
            throw new InvalidOperationException("Matruh source occupancy changed or has unsupported combat units.");
        foreach (var entry in entries)
        {
            var expected = !Confirmed ? "source-matruh-unconfirmed"
                : entry.ElementId == DetachedId ? "source-matruh-detached" : "source-matruh-grouped";
            if (entry.ElementId == DetachedId && entry.AuthorityStatus == "source-matruh-active") continue;
            if (entry.AuthorityStatus != expected)
                throw new InvalidOperationException("Matruh formation differs from retained battlefield.");
        }
    }
    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("policyId", PolicyId); writer.WriteString("initialBattlefieldHash", BattlefieldHash);
            writer.WriteString("scenarioHash", "sha256:fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66");
            writer.WriteString("commonwealthChartsHash", "sha256:f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981");
            writer.WriteString("landRulesHash", "sha256:b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99");
            writer.WriteString("errataHash", "sha256:5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb");
            writer.WriteString("assignmentRuling", "provisional-errata-buffs-hampshire-assigned-oa-no-assigned-exception");
            writer.WriteString("colocationRuling", "provisional-owner-select-buffs-hampshire-with-matruh");
            writer.WriteString("essexDurhamAssignment", "scenario-23-70-vs-oa-note-f-unresolved");
            writer.WriteString("headquartersId", HeadquartersId); writer.WriteString("headquartersCode", "a"); writer.WriteNumber("headquartersCpa", 30);
            writer.WriteString("locationId", "d3714"); writer.WriteString("garrisonStackRule", "9.16-garrison-counter-zero-at-matruh");
            writer.WriteString("movementRestriction", "no-voluntary-move-until-selby-or-stage-attacked-close-assault");
            writer.WriteString("conversion", "gt12-operation-stage2-selby"); writer.WriteNumber("maxAdditionalPotentialGunStack", 3);
            writer.WriteNumber("cityCombatStackLimit", 8); writer.WriteNumber("maxDetached", 1);
            writer.WriteBoolean("confirmed", Confirmed);
            if (DetachedId is null) writer.WriteNull("detachedId"); else writer.WriteString("detachedId", DetachedId);
            writer.WriteStartArray("members");
            foreach (var member in MembersData.OrderBy(value => value.ElementId, StringComparer.Ordinal))
            {
                writer.WriteStartObject(); writer.WriteString("elementId", member.ElementId); writer.WriteString("sourceCode", member.SourceCode);
                writer.WriteString("assignmentStatus", member.AssignmentStatus); writer.WriteString("componentId", member.ElementId + ".infantry");
                writer.WriteNumber("toe", 6); writer.WriteNumber("capabilityPoints", 10); writer.WriteNumber("individualStack", 1);
                writer.WriteString("attachmentStatus", !Confirmed ? "owner-choice-pending" : member.ElementId == DetachedId ? "detached" : "attached");
                writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteBoolean("scenarioSetupComplete", false); writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
    internal string InitialHash() => new CampaignSourceMatruhFormation(BattlefieldHash, false, null).Hash();
}
