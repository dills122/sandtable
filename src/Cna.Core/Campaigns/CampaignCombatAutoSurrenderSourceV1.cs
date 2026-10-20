using System.Security.Cryptography;
using System.Text.Json;

namespace Cna.Core.Campaigns;

/// <summary>
/// Reads only the owner-approved, frozen Content8/Rules/Setup8 source bytes.
/// This is not a CreationRequest context and cannot issue or replay a ledger.
/// </summary>
internal sealed record CampaignCombatAutoSurrenderSourceV1(
    string ContentHash, string RulesSourceHash, string SetupHash,
    string AttackerElementId, string DefenderElementId,
    int AttackerInitialAmmunition, int DefenderInitialAmmunition)
{
    private const string ContentDigest = "f7e25087a3f892304907e03640df258203eda6a2dee59fc669468200ef8d1595";
    private const string RulesDigest = "68ecae6ce2b688622842c3ed581c0cbd7e263a36c8e82c9b89e51ebb079b134d";
    private const string SetupDigest = "da46fb117545d5c3f2becddd1b7b6b458781d42591c4575e3f20a2b2c3b41134";
    private const string SetupHashValue = "sha256:253f6e302851d127a5cfaf735f377f9748091915a06e3b12f47acac1239bfa0d";

    internal static CampaignCombatAutoSurrenderSourceV1 Read(
        ReadOnlySpan<byte> contentBytes, ReadOnlySpan<byte> rulesBytes, ReadOnlySpan<byte> setupBytes)
    {
        using var content = ParsePinned(contentBytes, 11_217, ContentDigest, "Content8");
        using var rules = ParsePinned(rulesBytes, 1_173, RulesDigest, "Rules source");
        using var setup = ParsePinned(setupBytes, 1_930, SetupDigest, "Setup8");
        var c = content.RootElement;
        var r = rules.RootElement;
        var s = setup.RootElement;
        Require(c.GetProperty("schemaVersion").GetInt32() == 8 &&
                c.GetProperty("formatId").GetString() == "sandtable.content-json.v7" &&
                c.GetProperty("packId").GetString() == "rules-lab.content.close-assault-ammunitionless-defender.v1" &&
                c.GetProperty("capabilityProfileId").GetString() == "sandtable.capability.combat-auto-surrender-infantry.v1",
            "Content8 identity");
        Require(r.GetProperty("schemaVersion").GetInt32() == 2 &&
                r.GetProperty("trigger").GetProperty("attackKind").GetString() == "close-assault" &&
                r.GetProperty("trigger").GetProperty("acceptedSelectionRequired").GetBoolean() &&
                r.GetProperty("trigger").GetProperty("defenderRbaDeclineRequired").GetBoolean() &&
                r.GetProperty("trigger").GetProperty("settlementStatus").GetString() == "unsupported",
            "Rules source scope");
        Require(s.GetProperty("schemaVersion").GetInt32() == 8 &&
                s.GetProperty("setupHash").GetString() == SetupHashValue &&
                s.GetProperty("capabilityProfileId").GetString() == c.GetProperty("capabilityProfileId").GetString() &&
                s.GetProperty("content").GetProperty("hash").GetString() == "sha256:" + ContentDigest &&
                s.GetProperty("rulesSource").GetProperty("artifactId").GetString() ==
                    "cna-1979.1.combat-auto-surrender-source.v1" &&
                s.GetProperty("rulesSource").GetProperty("hash").GetString() == "sha256:" + RulesDigest,
            "Setup8 source binding");
        var placements = c.GetProperty("scenarios")[0].GetProperty("initialPlacements");
        Require(placements.GetArrayLength() == 2 &&
                c.GetProperty("ammunitionAccessInventory").GetProperty("scope").GetString() ==
                    "complete-initial-synthetic-inventory" &&
                c.GetProperty("ammunitionAccessInventory").GetProperty("externalSources").GetArrayLength() == 0 &&
                s.GetProperty("combatInitialization").GetProperty("cohesionLevel").GetInt32() == 0,
            "closed initial ammunition inventory");
        var attacker = placements[0];
        var defender = placements[1];
        var attackerAmmo = attacker.GetProperty("initialAmmunition").GetProperty("points").GetInt32();
        var defenderAmmo = defender.GetProperty("initialAmmunition").GetProperty("points").GetInt32();
        Require(attacker.GetProperty("elementId").GetString() == "axis-assault-battalion" &&
                defender.GetProperty("elementId").GetString() == "commonwealth-assault-battalion" &&
                attackerAmmo == 10 && defenderAmmo == 0,
            "approved one-orientation asymmetric ammunition");
        return new("sha256:" + ContentDigest, "sha256:" + RulesDigest, SetupHashValue,
            "axis-assault-battalion", "commonwealth-assault-battalion", attackerAmmo, defenderAmmo);
    }

    private static JsonDocument ParsePinned(ReadOnlySpan<byte> bytes, int length, string digest, string label)
    {
        if (bytes.Length != length || !CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(bytes), Convert.FromHexString(digest)))
            throw new JsonException($"{label} is not the frozen private source artifact.");
        return JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 32 });
    }

    private static void Require(bool condition, string label)
    {
        if (!condition) throw new JsonException($"Unsupported private automatic-surrender {label}.");
    }
}
