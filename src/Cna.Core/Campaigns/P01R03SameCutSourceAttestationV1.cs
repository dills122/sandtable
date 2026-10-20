using System.Security.Cryptography;
using System.Text.Json;

namespace Cna.Core.Campaigns;

/// <summary>Private source facts from one A1-authenticated R03 terminal cut. Never an action authority.</summary>
internal sealed class P01R03SameCutSourceAttestationV1
{
    private readonly byte[] canonicalBytes;
    private readonly bool admitted;

    internal P01R03SameCutSourceAttestationV1(byte[] bytes)
    {
        canonicalBytes = bytes.ToArray();
        admitted = false;
    }

    internal byte[] CanonicalBytes => canonicalBytes.ToArray();
    internal bool Admitted => admitted;
}

/// <summary>
/// Binds a retained terminal Control claim to A1's recomputed Control hash before reading private
/// World facts. No caller-provided fact, source selection, seat, or public reference is trusted.
/// </summary>
internal static class P01R03SameCutSourceAttestationCodecV1
{
    internal const int MaximumControlBytes = 65_536;
    internal const int MaximumAttestationBytes = 4_096;

    internal static P01R03SameCutSourceAttestationV1 Attest(ReadOnlySpan<byte> importBytes,
        ReadOnlySpan<byte> terminalControlClaim, CampaignCombatCreationContext trustedContext,
        Func<string, byte[]> trustedDependencies)
    {
        if (terminalControlClaim.Length is < 1 or > MaximumControlBytes)
            throw new JsonException("P01-A1S-001");

        // A1 independently selects the sole retained root and replays all eleven input ledgers.
        var checkpoint = P01R03ValidationCheckpointV1.Validate(importBytes, trustedContext, trustedDependencies);
        using var checkpointDocument = JsonDocument.Parse(checkpoint.CanonicalBytes);
        var checkpointRoot = checkpointDocument.RootElement;
        var controlHash = Hash(terminalControlClaim);
        if (checkpoint.Admitted || checkpointRoot.GetProperty("admitted").GetBoolean() ||
            checkpointRoot.GetProperty("r03ControlHash").GetString() != controlHash)
            throw new JsonException("P01-A1S-002");

        // A hash match to A1's recomputed Control pins every byte, including the private World.
        using var controlDocument = JsonDocument.Parse(terminalControlClaim.ToArray());
        var control = controlDocument.RootElement;
        var cycle = control.GetProperty("activeCycle");
        var position = control.GetProperty("sequencePosition");
        var world = control.GetProperty("world");
        var content = trustedContext.Setup.Artifact.Definition;
        Require(control.GetProperty("stateVersion").GetInt64() == checkpointRoot.GetProperty("terminalStateVersion").GetInt64() &&
            control.GetProperty("prefix").GetString() == checkpointRoot.GetProperty("terminalPrefix").GetString() &&
            control.GetProperty("cycleId").GetString() == checkpointRoot.GetProperty("activeCycleId").GetString() &&
            position.GetProperty("positionId").GetString() == checkpointRoot.GetProperty("positionId").GetString() &&
            cycle.GetProperty("campaignId").GetString() == checkpointRoot.GetProperty("campaignId").GetString() &&
            world.GetProperty("creationBinding").GetString() == checkpointRoot.GetProperty("creationBinding").GetString() &&
            cycle.GetProperty("rulesetHash").GetString() == trustedContext.RulesetHash &&
            cycle.GetProperty("setupHash").GetString() == trustedContext.Setup.SetupHash &&
            cycle.GetProperty("contentHash").GetString() == trustedContext.Setup.Artifact.Identity.Hash &&
            cycle.GetProperty("admittedPolicyBundleDigest").GetString() == trustedContext.Configuration.Hash &&
            cycle.GetProperty("ordinal").GetInt32() == 2 &&
            control.GetProperty("breakdownCompletionReceiptId").GetString() == checkpointRoot.GetProperty("terminalReceiptId").GetString());

        var definitions = content.Elements.ToDictionary(x => x.ElementId, StringComparer.Ordinal);
        var locations = content.Locations.Select(x => x.LocationId).ToHashSet(StringComparer.Ordinal);
        var elements = world.GetProperty("elements").EnumerateArray().ToArray();
        var representations = world.GetProperty("representations").EnumerateArray().ToArray();
        Require(elements.Length == definitions.Count && representations.Length == elements.Length);
        var worldElements = elements.ToDictionary(x => x.GetProperty("elementId").GetString()!, StringComparer.Ordinal);
        Require(worldElements.Count == definitions.Count && worldElements.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(definitions.Keys));
        var bound = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var representation in representations)
        {
            var ids = representation.GetProperty("boundElementIds").EnumerateArray().ToArray();
            Require(representation.GetProperty("bindingKind").GetString() == "independent-element" && ids.Length == 1);
            var elementId = ids[0].GetString()!;
            Require(worldElements.ContainsKey(elementId) && bound.TryAdd(elementId, representation));
        }
        Require(bound.Count == elements.Length);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("a1CheckpointHash", Hash(checkpoint.CanonicalBytes));
            writer.WriteString("terminalControlHash", controlHash);
            writer.WriteString("campaignId", cycle.GetProperty("campaignId").GetString());
            writer.WriteString("creationBinding", world.GetProperty("creationBinding").GetString());
            writer.WriteString("cycleId", control.GetProperty("cycleId").GetString());
            writer.WriteString("positionId", position.GetProperty("positionId").GetString());
            writer.WriteNumber("gameTurn", cycle.GetProperty("gameTurn").GetInt32());
            writer.WriteNumber("operationStage", cycle.GetProperty("operationStage").GetInt32());
            writer.WriteNumber("cycleOrdinal", cycle.GetProperty("ordinal").GetInt32());
            writer.WriteString("actingSide", cycle.GetProperty("actingSide").GetString());
            writer.WriteString("rulesetHash", trustedContext.RulesetHash);
            writer.WriteString("setupHash", trustedContext.Setup.SetupHash);
            writer.WriteString("contentHash", trustedContext.Setup.Artifact.Identity.Hash);
            writer.WriteString("configurationHash", trustedContext.Configuration.Hash);
            writer.WriteStartArray("sourceElements");
            foreach (var definition in content.Elements)
            {
                var element = worldElements[definition.ElementId];
                var representation = bound[definition.ElementId];
                var location = element.GetProperty("currentLocationId").GetString();
                Require(location is not null && locations.Contains(location) &&
                    representation.GetProperty("currentLocationId").GetString() == location);
                writer.WriteStartObject();
                writer.WriteString("elementId", definition.ElementId);
                writer.WriteString("sideId", definition.SideId);
                writer.WriteString("representationId", representation.GetProperty("representationId").GetString());
                writer.WriteString("locationId", location);
                writer.WriteStartObject("provenance");
                writer.WriteString("side", "trusted-content7.elements");
                writer.WriteString("representation", "authenticated-r03-control.world.representations");
                writer.WriteString("location", "authenticated-r03-control.world.elements.currentLocationId");
                writer.WriteEndObject();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteBoolean("admitted", false);
            writer.WriteEndObject();
        }
        var bytes = stream.ToArray();
        if (bytes.Length > MaximumAttestationBytes) throw new JsonException("P01-A1S-003");
        return new P01R03SameCutSourceAttestationV1(bytes);
    }

    internal static P01R03SameCutSourceAttestationV1 ReadAttestation(ReadOnlySpan<byte> claim,
        ReadOnlySpan<byte> importBytes, ReadOnlySpan<byte> terminalControlClaim,
        CampaignCombatCreationContext trustedContext, Func<string, byte[]> trustedDependencies)
    {
        if (claim.Length is < 1 or > MaximumAttestationBytes) throw new JsonException("P01-A1S-004");
        var recomputed = Attest(importBytes, terminalControlClaim, trustedContext, trustedDependencies);
        if (!claim.SequenceEqual(recomputed.CanonicalBytes)) throw new JsonException("P01-A1S-004");
        return recomputed;
    }

    private static void Require(bool condition)
    {
        if (!condition) throw new JsonException("P01-A1S-003");
    }

    private static string Hash(ReadOnlySpan<byte> bytes)
        => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));
}
