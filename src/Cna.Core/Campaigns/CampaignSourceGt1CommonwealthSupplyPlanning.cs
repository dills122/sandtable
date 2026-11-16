using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Randomness;

namespace Cna.Core.Campaigns;

// GT1 planning for Commonwealth GT5. The availability result does not deliver supply.
internal sealed class CampaignSourceGt1CommonwealthSupplyPlanning
{
    private static readonly int[] UnitsByDie = [1, 1, 2, 2, 3, 3];
    private CampaignSourceGt1CommonwealthSupplyPlanning(int? die, int? units)
    { Die = die; Units = units; }
    internal int? Die { get; }
    internal int? Units { get; }
    internal static CampaignSourceGt1CommonwealthSupplyPlanning Create() => new(null, null);
    internal (CampaignSourceGt1CommonwealthSupplyPlanning Plan, RandomStreamState Random) Roll(RandomStreamState random)
    {
        if (Die is not null) throw new InvalidOperationException("Commonwealth GT5 supply was already rolled.");
        var result = SandtableRandom.RollD6(random);
        return (new(result.Value, UnitsByDie[result.Value - 1]), result.State);
    }
    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1);
            writer.WriteString("policyId", "graziani.gt1.commonwealth-gt5-supply-availability.v1");
            writer.WriteNumber("planningGameTurn", 1); writer.WriteNumber("targetGameTurn", 5);
            writer.WriteString("arrivalPeriod", "I"); writer.WriteString("status", "planned-not-delivered");
            writer.WriteString("sources", "land:32.43,32.45,32.47");
            if (Die is int die) writer.WriteNumber("die", die); else writer.WriteNull("die");
            if (Units is int units) writer.WriteNumber("supplyUnits", units); else writer.WriteNull("supplyUnits");
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
}
