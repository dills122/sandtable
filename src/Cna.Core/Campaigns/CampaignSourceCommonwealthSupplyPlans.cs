using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Randomness;

namespace Cna.Core.Campaigns;

/// <summary>
/// Source-backed prestart Commonwealth supply plans. Outcomes reserve future
/// arrivals; they do not place or deliver supply units or choose an OpStage.
/// </summary>
internal sealed class CampaignSourceCommonwealthSupplyPlans
{
    internal const string PolicyId = "graziani.pregame.cw-supply.period-i.after-axis.v1";
    private static readonly int[] UnitsByDie = [1, 1, 2, 2, 3, 3];
    private readonly ReadOnlyCollection<CampaignSourceCommonwealthSupplyPlanEntry> entries;

    private CampaignSourceCommonwealthSupplyPlans(RandomStreamState random,
        IEnumerable<CampaignSourceCommonwealthSupplyPlanEntry> entries)
    {
        RandomState = random;
        this.entries = Array.AsReadOnly(entries.OrderBy(value => value.TargetGameTurn).ToArray());
    }

    internal RandomStreamState RandomState { get; }
    internal IReadOnlyList<CampaignSourceCommonwealthSupplyPlanEntry> Entries => entries;
    internal int? NextTargetGameTurn => entries.FirstOrDefault(value => value.Status == "unrolled")?.TargetGameTurn;

    internal static CampaignSourceCommonwealthSupplyPlans Create(CampaignSourceOpeningConvoyPlan axisPlan)
    {
        ArgumentNullException.ThrowIfNull(axisPlan);
        if (!axisPlan.IsFinished)
            throw new InvalidOperationException("Finish the retained Axis GT1 plan before Commonwealth planning.");
        return new(axisPlan.RandomState, Enumerable.Range(1, 4)
            .Select(turn => new CampaignSourceCommonwealthSupplyPlanEntry(turn, "unrolled", null, null)));
    }

    internal CampaignSourceCommonwealthSupplyPlans SkipGt1()
    {
        if (NextTargetGameTurn != 1)
            throw new InvalidOperationException("The starting-turn planning choice is unavailable.");
        return Replace(new(1, "skipped-opening", null, null), RandomState);
    }

    internal CampaignSourceCommonwealthSupplyPlans Roll(int targetGameTurn)
    {
        if (NextTargetGameTurn != targetGameTurn)
            throw new InvalidOperationException("Commonwealth supply plans must be rolled in the bound prestart order.");
        var result = SandtableRandom.RollD6(RandomState);
        return Replace(new(targetGameTurn, "rolled", result.Value, UnitsByDie[result.Value - 1]), result.State);
    }

    private CampaignSourceCommonwealthSupplyPlans Replace(CampaignSourceCommonwealthSupplyPlanEntry entry,
        RandomStreamState random) => new(random, entries.Select(value =>
            value.TargetGameTurn == entry.TargetGameTurn ? entry : value));

    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("policyId", PolicyId);
            writer.WriteString("policyStatus", "provisional");
            writer.WriteString("rngOrder", "axis-gt2-axis-gt1-cw-gt1-to-gt4");
            writer.WriteString("startingTurnRule", "optional-explicit-skip-or-roll");
            writer.WriteString("tablePeriod", "i-by-arrival-turn");
            writer.WriteString("cwChartsSourceHash", "sha256:f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981");
            writer.WriteString("landRulesSourceHash", "sha256:b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99");
            writer.WriteString("scenarioSourceHash", "sha256:fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66");
            writer.WriteString("randomAlgorithm", RandomState.AlgorithmId);
            writer.WriteNumber("randomSeed", RandomState.Seed);
            writer.WriteNumber("randomCursor", RandomState.NextByteCursor);
            writer.WriteStartArray("sources");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.LandRulesSourceId + ":32.42-32.45");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId + ":32.47");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.ScenariosSourceId + ":60.22");
            writer.WriteEndArray();
            writer.WriteStartArray("plans");
            foreach (var entry in entries)
            {
                writer.WriteStartObject();
                writer.WriteNumber("targetGameTurn", entry.TargetGameTurn);
                writer.WriteString("status", entry.Status);
                if (entry.Die is { } die) writer.WriteNumber("die", die);
                else writer.WriteNull("die");
                if (entry.SupplyUnits is { } units) writer.WriteNumber("supplyUnits", units);
                else writer.WriteNull("supplyUnits");
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
}

internal sealed record CampaignSourceCommonwealthSupplyPlanEntry(
    int TargetGameTurn, string Status, int? Die, int? SupplyUnits);
