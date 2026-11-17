using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// Land 60-combat.md:22-24 ("Combat is compulsory, not optional: once every
// move and reaction of the segment is over, the phasing player must attack
// every enemy hex that projects a zone of control onto a friendly unit...")
// and 50-stacking-and-zoc.md:241-273 (ZOC combat requirement / holding off).
// This records, per phasing side per combat segment, which of the phasing
// side's own hexes are ZOC-obligated to be attacked, and whether each has
// since been cleared by an attack, an exemption, or a retreat. It never
// resolves an attack, judges an exemption, or performs a retreat itself -
// those systems are out of scope here (barrage/anti-armor/close-assault
// resolution is a separate, much larger system; retreat mechanics do not
// yet exist). Each clearing path is recorded only on the caller's
// assertion that it genuinely occurred.
internal enum CampaignSourceCombatObligationStatus { Open, Attacked, Exempted, Retreated }

// The two exemption grounds in 50-stacking-and-zoc.md:241-273. Today's
// domain model has no artillery/anti-tank/AA "unit-kind" tag (every grouped
// formation/detached member is classified a generic combat unit; artillery
// pieces like a 3.7" howitzer are merely a free-form ComponentType string
// inside an otherwise combat-classified formation - see
// CampaignSourceGroupedZoc) and no "pinned" status exists on any element.
// This ledger therefore cannot independently verify either ground; it only
// records which one the caller is asserting, for audit/journaling. See the
// obligation-ledger work report for this judgment call.
internal enum CampaignSourceCombatObligationExemptionReason { ArtilleryAntiTankAaOrNonCombatOnly, Pinned }

internal sealed record CampaignSourceCombatObligation(string LocationId,
    CampaignSourceCombatObligationStatus Status,
    CampaignSourceCombatObligationExemptionReason? ExemptionReason = null);

// Keyed, per the design sketch, by (phasing side, enemy hex location, game
// turn, operation stage) - the side/turn/stage are carried once at the
// ledger level (one ledger instance per phasing side's combat segment),
// and each obligation then only needs its own location id and status.
internal sealed class CampaignSourceCombatObligationLedger
{
    private CampaignSourceCombatObligationLedger(LandSide phasingSide, int gameTurn, int operationStage,
        IReadOnlyList<CampaignSourceCombatObligation> obligations)
    {
        PhasingSide = phasingSide;
        GameTurn = gameTurn;
        OperationStage = operationStage;
        Obligations = obligations;
    }

    internal LandSide PhasingSide { get; }
    internal int GameTurn { get; }
    internal int OperationStage { get; }
    internal IReadOnlyList<CampaignSourceCombatObligation> Obligations { get; }
    internal bool HasOpenObligations =>
        Obligations.Any(value => value.Status == CampaignSourceCombatObligationStatus.Open);

    // obligatedLocationIds is the enemy-ZOC-controlled set intersected with
    // the phasing side's own occupied hexes (see
    // CampaignSourceCombatObligations.DeriveObligatedLocationIds below).
    // Duplicates are collapsed and the set is ordered for determinism.
    internal static CampaignSourceCombatObligationLedger Create(LandSide phasingSide, int gameTurn, int operationStage,
        IEnumerable<string> obligatedLocationIds)
    {
        ArgumentNullException.ThrowIfNull(obligatedLocationIds);
        var distinct = obligatedLocationIds.Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal).ToArray();
        if (distinct.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Obligated location ids must be non-empty.", nameof(obligatedLocationIds));
        return new(phasingSide, gameTurn, operationStage, Array.AsReadOnly(distinct
            .Select(value => new CampaignSourceCombatObligation(value, CampaignSourceCombatObligationStatus.Open)).ToArray()));
    }

    internal CampaignSourceCombatObligationLedger ResolveAttack(string locationId) =>
        WithStatus(locationId, CampaignSourceCombatObligationStatus.Attacked, null);

    internal CampaignSourceCombatObligationLedger ApplyExemption(string locationId,
        CampaignSourceCombatObligationExemptionReason reason) =>
        WithStatus(locationId, CampaignSourceCombatObligationStatus.Exempted, reason);

    internal CampaignSourceCombatObligationLedger ResolveRetreat(string locationId) =>
        WithStatus(locationId, CampaignSourceCombatObligationStatus.Retreated, null);

    private CampaignSourceCombatObligationLedger WithStatus(string locationId,
        CampaignSourceCombatObligationStatus status, CampaignSourceCombatObligationExemptionReason? reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(locationId);
        var index = -1;
        for (var i = 0; i < Obligations.Count; i++)
        {
            if (string.Equals(Obligations[i].LocationId, locationId, StringComparison.Ordinal)) { index = i; break; }
        }
        if (index < 0 || Obligations[index].Status != CampaignSourceCombatObligationStatus.Open)
            throw new InvalidOperationException("No open combat obligation exists at that location.");
        var updated = Obligations.ToArray();
        updated[index] = new CampaignSourceCombatObligation(locationId, status, reason);
        return new(PhasingSide, GameTurn, OperationStage, Array.AsReadOnly(updated));
    }

    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("phasingSide", PhasingSide == LandSide.Axis ? "axis" : "commonwealth");
            writer.WriteNumber("gameTurn", GameTurn);
            writer.WriteNumber("operationStage", OperationStage);
            writer.WriteStartArray("obligations");
            foreach (var obligation in Obligations)
            {
                writer.WriteStartObject();
                writer.WriteString("locationId", obligation.LocationId);
                writer.WriteString("status", obligation.Status.ToString());
                writer.WriteString("exemptionReason", obligation.ExemptionReason?.ToString());
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
}

// Derives which of the phasing side's own hexes are combat-obligated at the
// start of its combat segment: the enemy-ZOC-controlled set (via
// CampaignSourceGroupedZoc.TryDeriveControlledLocationIds, called for the
// enemy side) intersected with hexes containing the phasing side's own
// grouped formations or detached members. Mirrors
// CampaignSourceGroupedZoc's own two-tier (full-world / grouped-world)
// shape so the grouped-world core stays independently testable without a
// full historical-map-bound world.
internal static class CampaignSourceCombatObligations
{
    internal static string SideId(LandSide side) => side switch
    {
        LandSide.Axis => "axis",
        LandSide.Commonwealth => "commonwealth",
        _ => throw new ArgumentOutOfRangeException(nameof(side)),
    };

    internal static IReadOnlyList<string>? TryDeriveObligatedLocationIds(CampaignSourcePlayableWorld world,
        string phasingSideId, string enemySideId)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentException.ThrowIfNullOrWhiteSpace(phasingSideId);
        ArgumentException.ThrowIfNullOrWhiteSpace(enemySideId);
        var controlled = CampaignSourceGroupedZoc.TryDeriveControlledLocationIds(world, enemySideId);
        if (controlled is null) return null;
        return DeriveObligatedLocationIds(controlled, world.GroupedWorld, phasingSideId);
    }

    internal static IReadOnlyList<string> DeriveObligatedLocationIds(IReadOnlyList<string> enemyControlledLocationIds,
        CampaignGroupedWorld grouped, string phasingSideId)
    {
        ArgumentNullException.ThrowIfNull(enemyControlledLocationIds);
        ArgumentNullException.ThrowIfNull(grouped);
        ArgumentException.ThrowIfNullOrWhiteSpace(phasingSideId);
        var friendly = FriendlyOccupiedLocationIds(grouped, phasingSideId);
        return Array.AsReadOnly(enemyControlledLocationIds.Where(friendly.Contains)
            .Distinct(StringComparer.Ordinal).ToArray());
    }

    // Grouped formations and detached members only: this ledger does not
    // derive obligations from independent (ungrouped) elements, matching
    // CampaignSourceGroupedZoc's own scope boundary (it does not derive ZOC
    // for independent elements either). See the obligation-ledger report.
    private static HashSet<string> FriendlyOccupiedLocationIds(CampaignGroupedWorld grouped, string sideId)
    {
        var locations = new HashSet<string>(StringComparer.Ordinal);
        foreach (var group in grouped.Groups.Where(value => value.SideId == sideId)) locations.Add(group.LocationId);
        foreach (var detached in grouped.Detached.Where(value => value.SideId == sideId)) locations.Add(detached.LocationId);
        return locations;
    }
}
