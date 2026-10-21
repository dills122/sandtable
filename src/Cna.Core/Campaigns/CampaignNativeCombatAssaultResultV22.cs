using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// The normal campaign owns this result. Its dice and chart selection are derived
// from the retained authority cursor, never supplied by an owner submission.
internal sealed class CampaignNativeCombatAssaultResultV22
{
    internal const int CurrentContractVersion = 22;

    private CampaignNativeCombatAssaultResultV22(
        CampaignCombatSelectionFrame selected,
        CampaignNativeCombatAntiArmorV20 antiArmor,
        CampaignNativeCombatAssaultInputs inputs,
        RandomStreamState before,
        RandomStreamState after,
        IReadOnlyList<CombatResolutionDraw> draws,
        int attackerMoraleCoordinate,
        int defenderMoraleCoordinate,
        CampaignCombatResultFacts facts)
        : this(selected.CampaignId, selected.Decision.ActionId,
            antiArmor.StateVersion, antiArmor.NextPositionId,
            Hash(antiArmor.SerializeCanonical()), inputs.ContentHash,
            inputs.SourceHash, inputs.RulesInputHash, inputs.PriorAmmunitionHash,
            inputs.AfterAmmunition.Hash, inputs.PriorWorldHash,
            inputs.PaidWorldHash,
            inputs.Attacker.ElementId, inputs.Defender.ElementId,
            inputs.Attacker.AmmunitionBefore - inputs.Attacker.AmmunitionAfter,
            inputs.Defender.AmmunitionBefore - inputs.Defender.AmmunitionAfter,
            before, after, draws,
            attackerMoraleCoordinate, defenderMoraleCoordinate, facts)
    { }

    private CampaignNativeCombatAssaultResultV22(string campaignId,
        string decisionActionId, long priorStateVersion, string positionId,
        string priorAntiArmorHash, string contentHash, string sourceHash,
        string rulesInputHash, string priorAmmunitionHash,
        string afterAmmunitionHash, string priorWorldHash, string paidWorldHash,
        string attackerElementId,
        string defenderElementId, int attackerAmmunitionSpent,
        int defenderAmmunitionSpent, RandomStreamState before,
        RandomStreamState after, IReadOnlyList<CombatResolutionDraw> draws,
        int attackerMoraleCoordinate, int defenderMoraleCoordinate,
        CampaignCombatResultFacts facts)
    {
        CampaignId = campaignId;
        DecisionActionId = decisionActionId;
        PriorStateVersion = priorStateVersion;
        PositionId = positionId;
        PriorAntiArmorHash = priorAntiArmorHash;
        ContentHash = contentHash;
        SourceHash = sourceHash;
        RulesInputHash = rulesInputHash;
        PriorAmmunitionHash = priorAmmunitionHash;
        AfterAmmunitionHash = afterAmmunitionHash;
        PriorWorldHash = priorWorldHash;
        PaidWorldHash = paidWorldHash;
        AttackerElementId = attackerElementId;
        DefenderElementId = defenderElementId;
        AttackerAmmunitionSpent = attackerAmmunitionSpent;
        DefenderAmmunitionSpent = defenderAmmunitionSpent;
        BeforeRandomState = before;
        AfterRandomState = after;
        Draws = Array.AsReadOnly(draws.ToArray());
        AttackerMoraleCoordinate = attackerMoraleCoordinate;
        DefenderMoraleCoordinate = defenderMoraleCoordinate;
        Facts = facts;
    }

    internal string CampaignId { get; }
    internal string DecisionActionId { get; }
    internal long PriorStateVersion { get; }
    internal long StateVersion => checked(PriorStateVersion + 1);
    internal string PositionId { get; }
    internal string NextPositionId => PositionId;
    internal string PriorAntiArmorHash { get; }
    internal string ContentHash { get; }
    internal string SourceHash { get; }
    internal string RulesInputHash { get; }
    internal string PriorAmmunitionHash { get; }
    internal string AfterAmmunitionHash { get; }
    internal string PriorWorldHash { get; }
    internal string PaidWorldHash { get; }
    internal string AttackerElementId { get; }
    internal string DefenderElementId { get; }
    internal int AttackerAmmunitionSpent { get; }
    internal int DefenderAmmunitionSpent { get; }
    internal RandomStreamState BeforeRandomState { get; }
    internal RandomStreamState AfterRandomState { get; }
    internal IReadOnlyList<CombatResolutionDraw> Draws { get; }
    internal int AttackerMoraleCoordinate { get; }
    internal int DefenderMoraleCoordinate { get; }
    internal CampaignCombatResultFacts Facts { get; }

    internal static CampaignNativeCombatAssaultResultV22 Create(
        CampaignCombatSelectionFrame selected,
        CampaignNativeCombatAntiArmorV20 antiArmor,
        CampaignNativeCombatAssaultInputs inputs,
        RandomStreamState before)
    {
        ArgumentNullException.ThrowIfNull(selected);
        ArgumentNullException.ThrowIfNull(antiArmor);
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(before);
        if (antiArmor.CampaignId != selected.CampaignId
            || antiArmor.DecisionActionId != selected.Decision.ActionId
            || antiArmor.Disposition != "no-effect"
            || inputs.AntiArmorHash != Hash(antiArmor.SerializeCanonical())
            || inputs.RulesInputHash != Cna1979CombatAdjudication.CreateArtifact().ContentHash)
            throw new InvalidOperationException("Assault result lacks selected normal authority.");

        var current = before;
        var draws = new List<CombatResolutionDraw>();
        var dice = new List<int>();
        void Draw(string purpose)
        {
            var start = current.NextByteCursor;
            var consumed = new List<int>();
            while (true)
            {
                if (consumed.Count == 512)
                    throw new InvalidOperationException("Combat rejection trace exceeds its bound.");
                var next = SandtableRandom.NextByte(current);
                current = next.State;
                consumed.Add(next.Value);
                if (next.Value >= SandtableRandom.D6AcceptBelow) continue;
                var die = next.Value % 6 + 1;
                dice.Add(die);
                draws.Add(new CombatResolutionDraw(purpose, start,
                    current.NextByteCursor, die, consumed));
                return;
            }
        }

        var definition = Cna1979CombatAdjudication.Definition;
        foreach (var purpose in definition.Procedure.OrderedPurposes) Draw(purpose);
        var attackerMorale = 10 * dice[0] + dice[1];
        var defenderMorale = 10 * dice[2] + dice[3];
        var attackerCoordinate = 10 * dice[4] + dice[5];
        var defenderCoordinate = 10 * dice[6] + dice[7];
        var differential = Cna1979CombatAdjudication.CalculateDifferential(
            attackerMorale, defenderMorale);
        var effects = definition.Effects[differential + 2];
        var captured = effects.AttackerCaptureSums.Contains(dice[4] + dice[5])
            ? "attacker" : effects.DefenderCaptureSums.Contains(dice[6] + dice[7])
                ? "defender" : null;
        if (captured is not null) Draw(captured + ".capture.share");
        int? captureDie = captured is null ? null : dice[^1];
        var selectedResult = Cna1979CombatAdjudication.Resolve(attackerMorale,
            defenderMorale, attackerCoordinate, defenderCoordinate,
            captureDie: captureDie);
        var facts = new CampaignCombatResultFacts(selectedResult.Differential,
            attackerCoordinate, defenderCoordinate, captureDie,
            selectedResult.AttackerLossPercent, selectedResult.DefenderBaseLossPercent,
            selectedResult.RawEngaged, selectedResult.DefenderRetreatHexes,
            captured, selectedResult.CaptureSharePercent ?? 0);
        return new CampaignNativeCombatAssaultResultV22(selected, antiArmor, inputs,
            before, current, draws, attackerMorale, defenderMorale, facts);
    }

    internal static CampaignNativeCombatAssaultResultV22 Replay(
        CampaignCombatSelectionFrame selected,
        CampaignNativeCombatAntiArmorV20 antiArmor,
        CampaignNativeCombatAssaultInputs inputs,
        RandomStreamState before,
        ReadOnlyMemory<byte> canonicalEvent)
    {
        var expected = Create(selected, antiArmor, inputs, before);
        if (!canonicalEvent.Span.SequenceEqual(expected.SerializeCanonical()))
            throw new InvalidCampaignHistoryException(
                "Combat result differs from retained authority and ordered RNG.");
        return expected;
    }

    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CurrentContractVersion);
            writer.WriteString("eventType", "combat-assault-resolved");
            writer.WriteString("campaignId", CampaignId);
            writer.WriteNumber("priorStateVersion", PriorStateVersion);
            writer.WriteString("decisionActionId", DecisionActionId);
            writer.WriteString("positionId", PositionId);
            writer.WriteString("priorAntiArmorHash", PriorAntiArmorHash);
            writer.WriteString("contentHash", ContentHash);
            writer.WriteString("sourceHash", SourceHash);
            writer.WriteString("rulesInputHash", RulesInputHash);
            writer.WriteString("priorAmmunitionHash", PriorAmmunitionHash);
            writer.WriteString("afterAmmunitionHash", AfterAmmunitionHash);
            writer.WriteString("priorWorldHash", PriorWorldHash);
            writer.WriteString("paidWorldHash", PaidWorldHash);
            writer.WriteString("attackerElementId", AttackerElementId);
            writer.WriteString("defenderElementId", DefenderElementId);
            writer.WriteNumber("attackerAmmunitionSpent", AttackerAmmunitionSpent);
            writer.WriteNumber("defenderAmmunitionSpent", DefenderAmmunitionSpent);
            writer.WritePropertyName("beforeRandomState");
            WriteRandom(writer, BeforeRandomState);
            writer.WritePropertyName("afterRandomState");
            WriteRandom(writer, AfterRandomState);
            writer.WriteStartArray("draws");
            foreach (var draw in Draws)
            {
                writer.WriteStartObject();
                writer.WriteString("purpose", draw.Purpose);
                writer.WriteNumber("beforeCursor", draw.BeforeCursor);
                writer.WriteNumber("afterCursor", draw.AfterCursor);
                writer.WriteNumber("die", draw.Die);
                writer.WriteStartArray("consumed");
                foreach (var value in draw.Consumed) writer.WriteNumberValue(value);
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteNumber("attackerMoraleCoordinate", AttackerMoraleCoordinate);
            writer.WriteNumber("defenderMoraleCoordinate", DefenderMoraleCoordinate);
            writer.WriteStartObject("facts");
            writer.WriteNumber("differential", Facts.Differential);
            writer.WriteNumber("attackerCoordinate", Facts.AttackerCoordinate);
            writer.WriteNumber("defenderCoordinate", Facts.DefenderCoordinate);
            if (Facts.CaptureDie is int captureDie)
                writer.WriteNumber("captureDie", captureDie);
            else writer.WriteNull("captureDie");
            writer.WriteNumber("attackerPercent", Facts.AttackerPercent);
            writer.WriteNumber("defenderPercent", Facts.DefenderPercent);
            writer.WriteBoolean("rawEngaged", Facts.RawEngaged);
            writer.WriteNumber("requiredRetreat", Facts.RequiredRetreat);
            if (Facts.CapturedRole is null) writer.WriteNull("capturedRole");
            else writer.WriteString("capturedRole", Facts.CapturedRole);
            writer.WriteNumber("captureShare", Facts.CaptureShare);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal byte[] SerializeStateCanonical(ReadOnlySpan<byte> priorState)
    {
        if (priorState.IsEmpty)
            throw new ArgumentException("Combat result requires retained Anti Armor authority.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CurrentContractVersion);
            writer.WriteString("campaignId", CampaignId);
            writer.WriteNumber("stateVersion", StateVersion);
            writer.WriteString("positionId", NextPositionId);
            writer.WriteString("priorStateHash", Hash(priorState));
            writer.WritePropertyName("result");
            writer.WriteRawValue(SerializeCanonical());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static CampaignNativeCombatAssaultResultV22 Deserialize(
        ReadOnlyMemory<byte> canonicalEvent)
    {
        try
        {
            using var document = JsonDocument.Parse(canonicalEvent);
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root, "contractVersion", "eventType",
                "campaignId", "priorStateVersion", "decisionActionId", "positionId",
                "priorAntiArmorHash", "contentHash", "sourceHash", "rulesInputHash",
                "priorAmmunitionHash", "afterAmmunitionHash", "priorWorldHash",
                "paidWorldHash", "attackerElementId",
                "defenderElementId", "attackerAmmunitionSpent",
                "defenderAmmunitionSpent", "beforeRandomState",
                "afterRandomState", "draws", "attackerMoraleCoordinate",
                "defenderMoraleCoordinate", "facts");
            if (root.GetProperty("contractVersion").GetInt32() != CurrentContractVersion
                || root.GetProperty("eventType").GetString() != "combat-assault-resolved")
                throw new JsonException("Unknown native assault result.");
            var facts = root.GetProperty("facts");
            var parsed = new CampaignNativeCombatAssaultResultV22(
                root.GetProperty("campaignId").GetString()!,
                root.GetProperty("decisionActionId").GetString()!,
                root.GetProperty("priorStateVersion").GetInt64(),
                root.GetProperty("positionId").GetString()!,
                root.GetProperty("priorAntiArmorHash").GetString()!,
                root.GetProperty("contentHash").GetString()!,
                root.GetProperty("sourceHash").GetString()!,
                root.GetProperty("rulesInputHash").GetString()!,
                root.GetProperty("priorAmmunitionHash").GetString()!,
                root.GetProperty("afterAmmunitionHash").GetString()!,
                root.GetProperty("priorWorldHash").GetString()!,
                root.GetProperty("paidWorldHash").GetString()!,
                root.GetProperty("attackerElementId").GetString()!,
                root.GetProperty("defenderElementId").GetString()!,
                root.GetProperty("attackerAmmunitionSpent").GetInt32(),
                root.GetProperty("defenderAmmunitionSpent").GetInt32(),
                ParseRandom(root.GetProperty("beforeRandomState")),
                ParseRandom(root.GetProperty("afterRandomState")),
                root.GetProperty("draws").EnumerateArray().Select(draw =>
                    new CombatResolutionDraw(draw.GetProperty("purpose").GetString()!,
                        draw.GetProperty("beforeCursor").GetUInt64(),
                        draw.GetProperty("afterCursor").GetUInt64(),
                        draw.GetProperty("die").GetInt32(),
                        draw.GetProperty("consumed").EnumerateArray()
                            .Select(value => value.GetInt32()))).ToArray(),
                root.GetProperty("attackerMoraleCoordinate").GetInt32(),
                root.GetProperty("defenderMoraleCoordinate").GetInt32(),
                new CampaignCombatResultFacts(
                    facts.GetProperty("differential").GetInt32(),
                    facts.GetProperty("attackerCoordinate").GetInt32(),
                    facts.GetProperty("defenderCoordinate").GetInt32(),
                    facts.GetProperty("captureDie").ValueKind == JsonValueKind.Null
                        ? null : facts.GetProperty("captureDie").GetInt32(),
                    facts.GetProperty("attackerPercent").GetInt32(),
                    facts.GetProperty("defenderPercent").GetInt32(),
                    facts.GetProperty("rawEngaged").GetBoolean(),
                    facts.GetProperty("requiredRetreat").GetInt32(),
                    facts.GetProperty("capturedRole").GetString(),
                    facts.GetProperty("captureShare").GetInt32()));
            if (!canonicalEvent.Span.SequenceEqual(parsed.SerializeCanonical()))
                throw new JsonException("Noncanonical native assault result.");
            return parsed;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException
            or KeyNotFoundException or OverflowException)
        {
            throw new JsonException("Invalid native assault result.", exception);
        }
    }

    private static RandomStreamState ParseRandom(JsonElement value) => new(
        value.GetProperty("contractVersion").GetInt32(),
        value.GetProperty("algorithmId").GetString()!,
        value.GetProperty("seed").GetUInt64(),
        value.GetProperty("nextByteCursor").GetUInt64());

    private static void WriteRandom(Utf8JsonWriter writer, RandomStreamState state)
    {
        writer.WriteStartObject();
        writer.WriteNumber("contractVersion", state.ContractVersion);
        writer.WriteString("algorithmId", state.AlgorithmId);
        writer.WriteNumber("seed", state.Seed);
        writer.WriteNumber("nextByteCursor", state.NextByteCursor);
        writer.WriteEndObject();
    }

    private static string Hash(ReadOnlySpan<byte> bytes) =>
        "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));
}
