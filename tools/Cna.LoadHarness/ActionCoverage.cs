using System.Text.Json;

namespace Cna.LoadHarness;

// Interpret only the owner-visible choices. Never write contact handles or opaque refs.
internal sealed record VisibleChoice(string Kind, decimal MoveCp)
{
    internal static VisibleChoice Read(JsonElement cut, string stage, int index, int count)
    {
        var choices = new List<JsonElement>();
        foreach (var property in new[] { "choices", "movementOffers", "assaultOffers", "moveOffers", "offers" })
            if (cut.TryGetProperty(property, out var array) && array.ValueKind == JsonValueKind.Array)
                choices.AddRange(array.EnumerateArray());
        foreach (var property in new[] { "finishOffer", "declineOffer", "refuseOffer", "sealOffer" })
            if (cut.TryGetProperty(property, out var offer) && offer.ValueKind == JsonValueKind.Object)
                choices.Add(offer);
        if (choices.Count != count) return new(stage, 0);
        var choice = choices[index];
        var kind = choice.ValueKind == JsonValueKind.String ? choice.GetString()!
            : choice.TryGetProperty("kind", out var name) ? name.GetString()! : stage;
        decimal cost = 0;
        if (choice.ValueKind == JsonValueKind.Object)
        {
            if (choice.TryGetProperty("totalCostNumerator", out var numerator))
                cost = numerator.GetDecimal() / choice.GetProperty("totalCostDenominator").GetDecimal();
            else if (choice.TryGetProperty("costBreakdown", out var breakdown)
                && breakdown.TryGetProperty("totalCost", out var total))
                cost = total.GetProperty("numerator").GetDecimal() / total.GetProperty("denominator").GetDecimal();
        }
        return new(kind, cost);
    }

    internal bool IsMove => Kind is "move-element" or "move-and-stop" or "move";
    internal bool IsAttack => Kind == "select-contact-assault";
    internal bool IsFinish => Kind.StartsWith("finish", StringComparison.OrdinalIgnoreCase)
        || Kind.StartsWith("complete", StringComparison.OrdinalIgnoreCase)
        || Kind.EndsWith("-completion", StringComparison.Ordinal) || Kind.EndsWith("-pass", StringComparison.Ordinal);
}

internal sealed class ActionCoverage
{
    internal int Moves { get; private set; }
    internal decimal MoveCpSpent { get; private set; }
    internal int Attacks { get; private set; }
    internal int ReactionRefused { get; set; }
    internal int Finishes { get; private set; }
    internal SortedDictionary<string, int> CombatSteps { get; } = new(StringComparer.Ordinal)
    {
        ["rba-decline"] = 0, ["force-assignment"] = 0, ["post-assault-retreat"] = 0,
        ["custody"] = 0, ["assault-selection"] = 0,
    };
    internal SortedDictionary<string, int> CombatChoiceKinds { get; } = new(StringComparer.Ordinal);
    internal SortedSet<string> Positions { get; } = new(StringComparer.Ordinal);
    internal SortedSet<string> JournalPositions { get; } = new(StringComparer.Ordinal);
    internal int JournalCampaigns { get; private set; }

    // Diagnostics only, after the last owner submission. Count explicit persisted
    // positions/edges rather than interpolating unobserved catalog positions.
    internal void RecordJournal(IEnumerable<byte[]> records)
    {
        var turn = 1;
        foreach (var record in records)
        {
            using var document = JsonDocument.Parse(record);
            var root = document.RootElement;
            if (root.TryGetProperty("gameTurn", out var gameTurn)) turn = gameTurn.GetInt32();
            var nextTurn = root.TryGetProperty("nextGameTurn", out var rollover) ? rollover.GetInt32() : turn;
            Record(root, turn, nextTurn);
            if (root.TryGetProperty("sequencePosition", out var sequence))
                Record(sequence, sequence.TryGetProperty("gameTurn", out var sequenceTurn) ? sequenceTurn.GetInt32() : nextTurn, nextTurn);
            if (root.TryGetProperty("steps", out var steps) && steps.ValueKind == JsonValueKind.Array)
                foreach (var step in steps.EnumerateArray()) Record(step, turn, nextTurn);
            turn = nextTurn;
        }
        JournalCampaigns = 1;

        void Record(JsonElement value, int currentTurn, int followingTurn)
        {
            foreach (var name in new[] { "positionId", "fromPositionId", "nextPositionId" })
                if (value.ValueKind == JsonValueKind.Object && value.TryGetProperty(name, out var position)
                    && position.ValueKind == JsonValueKind.String && position.GetString() is { } id
                    && id.StartsWith("land.position.", StringComparison.Ordinal))
                    JournalPositions.Add($"{(name == "nextPositionId" ? followingTurn : currentTurn)}:{id}");
        }
    }

    internal void Accept(string stage, VisibleChoice choice)
    {
        if (choice.IsMove) { Moves++; MoveCpSpent += choice.MoveCp; }
        if (choice.IsAttack) { Attacks++; CombatSteps["assault-selection"]++; }
        if (choice.IsFinish) Finishes++;
        if (CombatSteps.TryGetValue(stage, out var count)) CombatSteps[stage] = count + 1;
        if (choice.IsAttack || CombatSteps.ContainsKey(stage))
            CombatChoiceKinds[choice.Kind] = CombatChoiceKinds.GetValueOrDefault(choice.Kind) + 1;
    }

    internal void Add(ActionCoverage other)
    {
        Moves += other.Moves;
        MoveCpSpent += other.MoveCpSpent;
        Attacks += other.Attacks;
        ReactionRefused += other.ReactionRefused;
        Finishes += other.Finishes;
        foreach (var (kind, count) in other.CombatSteps) CombatSteps[kind] += count;
        foreach (var (kind, count) in other.CombatChoiceKinds)
            CombatChoiceKinds[kind] = CombatChoiceKinds.GetValueOrDefault(kind) + count;
        Positions.UnionWith(other.Positions);
        JournalPositions.UnionWith(other.JournalPositions);
        JournalCampaigns += other.JournalCampaigns;
    }

    internal object Snapshot() => new
    {
        Moves, MoveCpSpent, Attacks, CombatSteps, CombatChoiceKinds, reactionRefused = ReactionRefused, Finishes,
        positionsVisited = Positions.Count, positionIds = Positions,
        journalCampaigns = JournalCampaigns, journalPositionsVisited = JournalCampaigns > 0 ? JournalPositions.Count : (int?)null,
        journalPositionIds = JournalPositions,
        zeroCoverage = new[] { ("moves", Moves), ("attacks", Attacks) }.Where(x => x.Item2 == 0).Select(x => x.Item1),
    };
}
