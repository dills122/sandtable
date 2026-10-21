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
        CampaignNativeCombatDecisionStateV12 selected,
        CampaignNativeCombatAntiArmorV20 antiArmor,
        CampaignNativeCombatAssaultInputs inputs,
        RandomStreamState before,
        RandomStreamState after,
        IReadOnlyList<CombatResolutionDraw> draws,
        int attackerMoraleCoordinate,
        int defenderMoraleCoordinate,
        CampaignCombatResultFacts facts)
    {
        CampaignId = selected.CampaignId;
        DecisionActionId = selected.Decision.ActionId;
        PriorStateVersion = antiArmor.StateVersion;
        PriorAntiArmorHash = Hash(antiArmor.SerializeCanonical());
        ContentHash = inputs.ContentHash;
        SourceHash = inputs.SourceHash;
        RulesInputHash = inputs.RulesInputHash;
        PriorAmmunitionHash = inputs.PriorAmmunitionHash;
        AfterAmmunitionHash = inputs.AfterAmmunition.Hash;
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
    internal string PriorAntiArmorHash { get; }
    internal string ContentHash { get; }
    internal string SourceHash { get; }
    internal string RulesInputHash { get; }
    internal string PriorAmmunitionHash { get; }
    internal string AfterAmmunitionHash { get; }
    internal RandomStreamState BeforeRandomState { get; }
    internal RandomStreamState AfterRandomState { get; }
    internal IReadOnlyList<CombatResolutionDraw> Draws { get; }
    internal int AttackerMoraleCoordinate { get; }
    internal int DefenderMoraleCoordinate { get; }
    internal CampaignCombatResultFacts Facts { get; }

    internal static CampaignNativeCombatAssaultResultV22 Create(
        CampaignNativeCombatDecisionStateV12 selected,
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
        CampaignNativeCombatDecisionStateV12 selected,
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
            writer.WriteString("priorAntiArmorHash", PriorAntiArmorHash);
            writer.WriteString("contentHash", ContentHash);
            writer.WriteString("sourceHash", SourceHash);
            writer.WriteString("rulesInputHash", RulesInputHash);
            writer.WriteString("priorAmmunitionHash", PriorAmmunitionHash);
            writer.WriteString("afterAmmunitionHash", AfterAmmunitionHash);
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
