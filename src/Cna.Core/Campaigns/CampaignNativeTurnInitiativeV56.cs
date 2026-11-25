using System.Text.Json;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Campaigns;

// D-T2-1: only an explicit contested policy is supported after rollover.
// The accepted predecessor history supplies the policy and the prior cursor.
internal sealed record CampaignNativeTurnInitiativeV56(
    int ContractVersion, string CampaignId, long PriorStateVersion, string PriorEventHash,
    string PositionId, string NextPositionId, string WorldHash, int GameTurn, int NextGameTurn,
    LandSide Holder, IReadOnlyList<InitiativeRollRound> Rounds, AxisInitiativePresence AxisPresence,
    ulong PriorRandomCursor, ulong ResultRandomCursor)
{
    internal const int CurrentContractVersion = 56;
    internal long StateVersion => checked(PriorStateVersion + 1);

    internal static CampaignNativeTurnInitiativeV56 Create(
        CampaignNativeEndOfTurnV55 predecessor, InitiativePolicy policy,
        RandomStreamState priorRandomState, long contactVersion)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(priorRandomState);
        if (contactVersion != predecessor.StateVersion)
            throw new InvalidOperationException("Turn Initiative belongs to the current cut.");
        var position = Cna1979LandSequenceV4.CreateTurn(predecessor.NextGameTurn)[0];
        if (position.PhaseId != LandPhaseIds.InitiativeDetermination || predecessor.NextPositionId != position.PositionId)
            throw new InvalidOperationException("Turn Initiative requires the rollover predecessor.");
        var next = Cna1979LandSequenceV4.GetNext(position);
        if (next.PhaseId != LandPhaseIds.NavalConvoySchedule || next.GameTurn != position.GameTurn)
            throw new InvalidOperationException("Turn Initiative must advance to the convoy schedule.");
        var priorBytes = predecessor.SerializeCanonical();
        if (policy is not ContestedInitiative)
            throw new CampaignRuleNotImplementedException("Later-turn initiative requires a contested policy.");
        if (priorRandomState.ContractVersion != SandtableRandom.ContractVersion
            || priorRandomState.AlgorithmId != SandtableRandom.AlgorithmId)
            throw new InvalidOperationException("Turn Initiative requires a valid random cursor.");
        var resolution = InitiativeResolver.Resolve(position.GameTurn, policy, priorRandomState, []);
        var outcome = (ContestedInitiativeOutcome)resolution.Outcome;
        return new(CurrentContractVersion, predecessor.CampaignId, predecessor.StateVersion,
            TurnBoundaryCanonical.Hash(priorBytes), position.PositionId, next.PositionId, predecessor.WorldHash,
            position.GameTurn, next.GameTurn, outcome.Holder, outcome.Rounds, outcome.AxisPresence,
            priorRandomState.NextByteCursor, resolution.RandomState.NextByteCursor);
    }

    internal byte[] SerializeCanonical()
    {
        if (GameTurn is < 2 or > CampaignNativeEndOfTurnV55.LastRolloverTurn + 1 || NextGameTurn != GameTurn
            || PositionId != "land.position.initiative-determination" || NextPositionId != "land.position.naval-convoy.schedule"
            || !Enum.IsDefined(Holder) || !Enum.IsDefined(AxisPresence) || Rounds is null || Rounds.Count == 0
            || ResultRandomCursor <= PriorRandomCursor || ResultRandomCursor - PriorRandomCursor < 2UL * (uint)Rounds.Count)
            throw new JsonException("Invalid Turn Initiative.");
        var axisRating = Cna1979InitiativeRatings.GetAxis(AxisPresence).Rating;
        var commonwealthRating = Cna1979InitiativeRatings.GetCommonwealth(GameTurn).Rating;
        for (var index = 0; index < Rounds.Count; index++)
        {
            var round = Rounds[index];
            if (round is null || round.Round != index + 1 || round.AxisRating != axisRating
                || round.CommonwealthRating != commonwealthRating
                || (round.AxisTotal == round.CommonwealthTotal) != (index < Rounds.Count - 1))
                throw new JsonException("Invalid Turn Initiative round.");
        }
        var final = Rounds[^1];
        if (Holder != (final.AxisTotal > final.CommonwealthTotal ? LandSide.Axis : LandSide.Commonwealth))
            throw new JsonException("Invalid Turn Initiative holder.");

        return TurnBoundaryCanonical.Write(ContractVersion, CurrentContractVersion, "native-turn-initiative-determined",
            CampaignId, PriorStateVersion, PriorEventHash, PositionId, NextPositionId, WorldHash, GameTurn, writer =>
            {
                writer.WriteNumber("nextGameTurn", NextGameTurn);
                writer.WriteString("holder", CampaignSnapshotSerializer.FormatSide(Holder));
                writer.WriteStartArray("rounds");
                foreach (var round in Rounds)
                {
                    writer.WriteStartObject();
                    writer.WriteNumber("axisRoll", round.AxisDie);
                    writer.WriteNumber("axisRating", round.AxisRating);
                    writer.WriteNumber("axisTotal", round.AxisTotal);
                    writer.WriteNumber("commonwealthRoll", round.CommonwealthDie);
                    writer.WriteNumber("commonwealthRating", round.CommonwealthRating);
                    writer.WriteNumber("commonwealthTotal", round.CommonwealthTotal);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteString("axisPresence", AxisPresence.ToString());
                writer.WriteNumber("priorRandomCursor", PriorRandomCursor);
                writer.WriteNumber("resultRandomCursor", ResultRandomCursor);
            });
    }

    internal static CampaignNativeTurnInitiativeV56 Deserialize(ReadOnlyMemory<byte> bytes) =>
        TurnBoundaryCanonical.Read(bytes, "native-turn-initiative-determined",
            ["nextGameTurn", "holder", "rounds", "axisPresence", "priorRandomCursor", "resultRandomCursor"], root =>
            {
                var holder = root.GetProperty("holder").GetString() switch
                {
                    "axis" => LandSide.Axis,
                    "commonwealth" => LandSide.Commonwealth,
                    _ => throw new JsonException("Invalid side."),
                };
                var rounds = root.GetProperty("rounds").EnumerateArray().Select((round, index) =>
                {
                    CampaignSnapshotSerializer.RequireProperties(round, "axisRoll", "axisRating", "axisTotal",
                        "commonwealthRoll", "commonwealthRating", "commonwealthTotal");
                    return new InitiativeRollRound(index + 1, round.GetProperty("axisRoll").GetInt32(),
                        round.GetProperty("axisRating").GetInt32(), round.GetProperty("axisTotal").GetInt32(),
                        round.GetProperty("commonwealthRoll").GetInt32(), round.GetProperty("commonwealthRating").GetInt32(),
                        round.GetProperty("commonwealthTotal").GetInt32());
                }).ToArray();
                return new CampaignNativeTurnInitiativeV56(root.GetProperty("contractVersion").GetInt32(),
                    root.GetProperty("campaignId").GetString()!, root.GetProperty("priorStateVersion").GetInt64(),
                    root.GetProperty("priorEventHash").GetString()!, root.GetProperty("positionId").GetString()!,
                    root.GetProperty("nextPositionId").GetString()!, root.GetProperty("worldHash").GetString()!,
                    root.GetProperty("gameTurn").GetInt32(), root.GetProperty("nextGameTurn").GetInt32(),
                    holder, Array.AsReadOnly(rounds), Enum.Parse<AxisInitiativePresence>(root.GetProperty("axisPresence").GetString()!),
                    root.GetProperty("priorRandomCursor").GetUInt64(), root.GetProperty("resultRandomCursor").GetUInt64());
            }, value => value.SerializeCanonical());
}
