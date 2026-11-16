using System.Security.Cryptography;
using System.Text.Json;

namespace Cna.Core.Content;

// Setup identity only. Source's 8+ marks historical motorization; the scenario
// does not identify an assigned motor pool or an effective current allowance.
internal sealed class ContentSourceGiarabubMotorMachineGunArtifact
{
    internal const string FormatId = "sandtable.source-giarabub-motor-mg.v1";
    internal const string ContentHash = "sha256:178af9ec5fc908935150c398df023fedc8f2e6aa6d2ee057f92ddf641e50bbdc";
    internal const string ElementId = "giarabub-motor-machine-gun";
    internal const string LocationId = "c1014";
    private readonly byte[] bytes;
    private ContentSourceGiarabubMotorMachineGunArtifact(ContentSourceAxisGarrisonRemainderPackArtifact basis, byte[] bytes)
    {
        Basis = basis; this.bytes = bytes.ToArray();
        Identity = new(FormatId, basis.Identity.PackId, basis.Identity.RulesetId,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }
    internal ContentSourceAxisGarrisonRemainderPackArtifact Basis { get; }
    internal ContentScheduledPackIdentity Identity { get; }
    internal byte[] CanonicalBytes => bytes.ToArray();
    internal static ContentSourceGiarabubMotorMachineGunArtifact Create(ContentSourceAxisGarrisonRemainderPackArtifact basis)
    {
        ArgumentNullException.ThrowIfNull(basis);
        // The predecessor has a private constructor and is immutable; both of
        // its factory paths enforce its pinned identity and canonical bytes.
        if (basis.Identity.Hash != ContentSourceAxisGarrisonRemainderPackArtifact.ContentHash)
            throw new InvalidOperationException("Motor MG requires canonical garrison predecessor.");
        var facts = Cna1979GrazianiSourceFacts.GiarabubCombatFacts.Single(value => value.ElementId == ElementId);
        var component = facts.Components.Single();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("formatId", FormatId);
            writer.WriteString("baseHash", basis.Identity.Hash); writer.WriteBase64String("baseContent", basis.CanonicalBytes);
            writer.WriteString("sources", "scenario:59.63a,60.31;axis-charts:oa-giarabub-p14,4.46b;air:counter-section-2-p40;land:4.23,9.16a,32.54");
            writer.WriteString("elementId", ElementId); writer.WriteString("sideId", "axis"); writer.WriteString("locationId", LocationId);
            writer.WriteNumber("toe", 1); writer.WriteNumber("printedStacking", 0); writer.WriteNumber("assignedLocationStacking", 0);
            writer.WriteString("printedCapabilityPoints", "8+"); writer.WriteNull("effectiveCapabilityPoints");
            writer.WriteNull("mobilityId"); writer.WriteNull("motorPoolId"); writer.WriteNull("assignedMotorPoints");
            writer.WriteString("historicalMotorizationPriority", "unresolved-no-scenario-pool");
            writer.WriteString("combatClassificationId", facts.CombatClassificationId);
            writer.WriteString("componentId", component.ComponentId); writer.WriteString("componentClassId", component.ComponentClassId);
            writer.WriteNumber("maximumToe", component.MaximumToe); writer.WriteNumber("defensiveCloseAssault", component.DefensiveCloseAssaultRating);
            if (component.OffensiveCloseAssaultRating is int offensive) writer.WriteNumber("offensiveCloseAssault", offensive); else writer.WriteNull("offensiveCloseAssault");
            writer.WriteNull("parentFormationId"); writer.WriteEndObject();
        }
        var result = new ContentSourceGiarabubMotorMachineGunArtifact(basis, stream.ToArray());
        if (result.Identity.Hash != ContentHash)
            throw new InvalidOperationException("Motor MG content differs from its pinned identity.");
        return result;
    }
    internal static ContentSourceGiarabubMotorMachineGunArtifact Read(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        using var document = JsonDocument.Parse(bytes);
        var basis = ContentSourceAxisGarrisonRemainderPackArtifact.Read(document.RootElement.GetProperty("baseContent").GetBytesFromBase64());
        var result = Create(basis);
        if (!bytes.AsSpan().SequenceEqual(result.bytes)) throw new InvalidOperationException("Motor MG content is not canonical.");
        return result;
    }
}
