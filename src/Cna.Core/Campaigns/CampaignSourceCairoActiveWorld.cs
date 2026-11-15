using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>
/// Validated source-content successor and real World6 state for the four
/// player-placed Cairo counters. Movement, attachments and complete scenario
/// admission remain unsupported.
/// </summary>
internal sealed class CampaignSourceCairoActiveWorld
{
    internal const string PolicyId = "graziani.cairo-source-active-world.v1";
    internal const string SourceContentHash = "sha256:3a7fddd510854c81b408e07742dd837f7be416206117ca58ea79357357a11ddd";
    private CampaignSourceCairoActiveWorld(ContentSourceSetupPackArtifact content,
        CampaignWorldSnapshotV8 world, string setupHash, string battlefieldHash)
    { Content = content; World = world; SetupHash = setupHash; BattlefieldHash = battlefieldHash; }

    internal ContentSourceSetupPackArtifact Content { get; }
    internal CampaignWorldSnapshotV8 World { get; }
    internal string SetupHash { get; }
    internal string BattlefieldHash { get; }

    internal static CampaignSourceCairoActiveWorld Create(CampaignSourceAuthorityState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var setup = state.CairoSetup ?? throw new InvalidOperationException("Cairo setup is missing.");
        var battlefield = state.Battlefield ?? throw new InvalidOperationException("Source battlefield is missing.");
        if (setup.PendingIds.Count != 0 || setup.Placements.Count != CampaignSourceCairoInitialSetup.Units.Count)
            throw new InvalidOperationException("Every source Cairo setup counter must be placed first.");
        var original = state.Pregame.Source.Session.InitialWorld;
        var basis = state.Pregame.Source.Session.Content;
        if (original.ContentIdentity != basis.Identity || state.World.ContentIdentity != basis.Identity)
            throw new InvalidOperationException("Cairo activation requires the retained source World8 identity.");
        foreach (var placement in setup.Placements)
        {
            var entry = battlefield.Entries.SingleOrDefault(value => value.ElementId == placement.ElementId);
            if (entry is null || entry.SideId != "commonwealth" || entry.LocationId != placement.LocationId
                || entry.AuthorityStatus != "source-setup-placed")
                throw new InvalidOperationException("Cairo setup choices disagree with retained battlefield.");
        }
        var contentUnits = CampaignSourceCairoInitialSetup.Units.Select(unit => new ContentSourceSetupUnit(
            unit.ElementId, "commonwealth", unit.CapabilityPoints, unit.ComponentId,
            unit.ComponentClassId, unit.Toe, unit.Stacking, Origin(unit.ElementId))).ToArray();
        var content = ContentSourceSetupPackArtifact.Create(basis, contentUnits);
        if (content.Identity.Hash != SourceContentHash)
            throw new InvalidOperationException("Cairo source content differs from the pinned source profile.");
        // Content identity is independent of the player's chosen locations.
        var oldWorld = original.World;
        var existing = oldWorld.Elements.Select(value => value.ElementId).ToHashSet(StringComparer.Ordinal);
        if (contentUnits.Any(value => existing.Contains(value.ElementId))
            || contentUnits.Any(value => original.PendingArrivals.Any(group => group.ElementIds.Contains(value.ElementId, StringComparer.Ordinal))))
            throw new InvalidOperationException("Cairo active unit overlaps an existing or deferred unit.");
        var init = original.Initialization;
        if (contentUnits.Any(value => init.CapabilityPointsExpended > new CapabilityPointAmount(value.CapabilityPoints, 1)))
            throw new InvalidOperationException("Retained initial expenditure exceeds a Cairo unit's allowance.");
        var newElements = setup.Placements.Select(placement =>
        {
            var unit = content.Units.Single(value => value.ElementId == placement.ElementId);
            return new CampaignElementStateV5(unit.ElementId, placement.LocationId, init.ReserveStatus,
                new CampaignElementOperationalStateV5(1, 1, init.CapabilityPointsExpended, init.CohesionLevel, null, null),
                [new CampaignComponentToeState(unit.ComponentId, unit.Toe, unit.Origin)]);
        }).ToArray();
        var newRepresentations = setup.Placements.Select(placement => new CampaignMapRepresentationState(
            "cairo-setup." + placement.ElementId, placement.LocationId,
            CampaignMapRepresentationBindingKind.IndependentElement, [placement.ElementId])).ToArray();
        var world6 = new CampaignWorldSnapshotV6(CampaignWorldSnapshotV6.CurrentContractVersion,
            oldWorld.Elements.Concat(newElements), oldWorld.Representations.Concat(newRepresentations), oldWorld.BrokenVehicleLots);
        var world8 = new CampaignWorldSnapshotV8(content.Identity, original.ScenarioId, original.Initialization,
            world6, original.PendingArrivals);
        Validate(content, world8, original, setup);
        return new(content, world8, setup.Hash(), battlefield.Hash());
    }

    private static void Validate(ContentSourceSetupPackArtifact content, CampaignWorldSnapshotV8 world,
        CampaignWorldSnapshotV8 original, CampaignSourceCairoInitialSetup setup)
    {
        if (ContentSourceSetupPackArtifact.Read(content.CanonicalBytes).Identity != content.Identity
            || world.ContentIdentity != content.Identity || world.ScenarioId != original.ScenarioId
            || !world.PendingArrivals.SequenceEqual(original.PendingArrivals)
            || world.World.Elements.Count != original.World.Elements.Count + content.Units.Count
            || world.World.Representations.Count != original.World.Representations.Count + content.Units.Count
            || !original.World.Elements.All(value => world.World.Elements.Contains(value))
            || !original.World.Representations.All(value => world.World.Representations.Contains(value))
            || !world.World.BrokenVehicleLots.SequenceEqual(original.World.BrokenVehicleLots))
            throw new InvalidOperationException("Cairo source world does not preserve its retained predecessor.");
        foreach (var placement in setup.Placements)
        {
            var unit = content.Units.Single(value => value.ElementId == placement.ElementId);
            var element = world.World.Elements.Single(value => value.ElementId == placement.ElementId);
            var representation = world.World.Representations.Single(value => value.BoundElementIds.Contains(placement.ElementId, StringComparer.Ordinal));
            if (element.CurrentLocationId != placement.LocationId || representation.CurrentLocationId != placement.LocationId
                || representation.BindingKind != CampaignMapRepresentationBindingKind.IndependentElement
                || representation.BoundElementIds.Count != 1 || element.Components.Count != 1
                || element.Components[0].ComponentId != unit.ComponentId || element.Components[0].CurrentToe != unit.Toe)
                throw new InvalidOperationException("Cairo source world differs from placed source facts.");
        }
    }

    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("policyId", PolicyId);
            writer.WriteString("sourceSetupHash", SetupHash);
            writer.WriteString("sourceBattlefieldHash", BattlefieldHash);
            writer.WriteString("contentFormatId", Content.Identity.FormatId);
            writer.WriteString("contentHash", Content.Identity.Hash);
            writer.WriteBase64String("content", Content.CanonicalBytes);
            writer.WriteString("scenarioId", World.ScenarioId);
            writer.WriteBoolean("worldCoverageComplete", false);
            writer.WriteString("mobilityStatus", "unresolved-no-native-movement");
            writer.WriteString("attachmentStatus", "unresolved");
            CampaignV11CanonicalCodec.WriteWorld(writer, "world", World.World);
            writer.WriteStartArray("pendingArrivals");
            foreach (var arrival in World.PendingArrivals)
            {
                writer.WriteStartObject();
                writer.WriteString("groupId", arrival.GroupId);
                writer.WriteString("sideId", arrival.SideId);
                writer.WriteNumber("gameTurn", arrival.Arrival.GameTurn);
                writer.WriteNumber("operationStage", arrival.Arrival.OperationStage);
                writer.WriteString("entryAreaId", arrival.EntryAreaId);
                writer.WriteStartArray("elementIds");
                foreach (var id in arrival.ElementIds) writer.WriteStringValue(id);
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));

    internal static ContentOrigin Origin(string id)
    {
        var locator = id switch
        {
            "1-welch" => "oa-indian-p16",
            "8-field-artillery" => "oa-artillery-p12",
            "1-cheshire-machine-gun" or "2-kings-own-royal-regiment" => "oa-british-p11",
            _ => throw new InvalidOperationException("Unknown source Cairo counter."),
        };
        return new(ContentOriginKind.SourceDerived,
        [new RuleReference(Cna1979GrazianiSourceFacts.ScenariosSourceId, "60.41"),
            new RuleReference(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId, locator),
            new RuleReference(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId, "4.46a"),
            new RuleReference(Cna1979GrazianiSourceFacts.LandRulesSourceId, "4.45")]);
    }
}
