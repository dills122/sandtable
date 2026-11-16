using System.Security.Cryptography;
using System.Text.Json;

namespace Cna.Core.Campaigns;

// Land 17.31-17.36 and Commonwealth OA p.13. Scenario 60.41 does not
// specify the Australian contingent's individual placement, current morale,
// instructor, or elapsed training. Null records unknown, not zero.
internal sealed class CampaignSourceTrainingLedger
{
    internal const string FormationId = "6th-australian-division";
    internal const string PolicyId = "graziani.sixth-australian.training.v1";
    internal const string ProvisionalBaselineId = "graziani.6-australian.initial-untrained-zero.v1";
    internal const int UntrainedMorale = -3;
    internal const int TrainedMorale = 1;
    private CampaignSourceTrainingLedger(int? morale, int? stages, int lastStage, string? ruling, bool interrupted, int baselineVotes = 0)
    {
        CurrentMorale = morale;
        QualifyingStages = stages;
        LastRecordedStage = lastStage;
        InitialRuling = ruling;
        Interrupted = interrupted;
        BaselineVotes = baselineVotes;
    }

    internal int? CurrentMorale { get; }
    internal int? QualifyingStages { get; }
    internal int LastRecordedStage { get; }
    internal string? InitialRuling { get; }
    internal bool Interrupted { get; }
    internal int BaselineVotes { get; }
    internal bool HasBaselineVote(bool axis) => (BaselineVotes & (axis ? 1 : 2)) != 0;
    internal bool HasEstablishedStartingState => CurrentMorale.HasValue && QualifyingStages.HasValue;
    internal static CampaignSourceTrainingLedger CreateSixthAustralian() => new(null, null, 0, null, false);

    // This is a declared ruling, not a claim that the printed scenario supplies
    // these missing values. It cannot be applied retroactively after a stage.
    internal CampaignSourceTrainingLedger EstablishStartingState(int morale, int stages, string ruling)
    {
        if (InitialRuling is not null || LastRecordedStage != 0 || morale is < UntrainedMorale or > TrainedMorale
            || stages is < 0 or > 5 || (morale == TrainedMorale && stages != 0)
            || string.IsNullOrWhiteSpace(ruling))
            throw new InvalidOperationException("Invalid or already established training starting state.");
        return new(morale, stages, 0, ruling, Interrupted, BaselineVotes);
    }

    // A scenario-default interpretation must be independently approved by
    // both owners. This is a provisional ruling, not historical evidence.
    // Before any stage is recorded either owner may withdraw its vote.
    internal CampaignSourceTrainingLedger SetBaselineVote(bool axis, bool approve)
    {
        if (LastRecordedStage != 0 || (InitialRuling is not null && InitialRuling != ProvisionalBaselineId)
            || HasBaselineVote(axis) == approve)
            throw new InvalidOperationException("Training baseline vote is unavailable.");
        var bit = axis ? 1 : 2;
        var votes = approve ? BaselineVotes | bit : BaselineVotes & ~bit;
        return votes == 3
            ? new(UntrainedMorale, 0, 0, ProvisionalBaselineId, Interrupted, votes)
            : new(null, null, 0, null, Interrupted, votes);
    }

    // Land 17.36 permits training to be interrupted. Unknown prior progress
    // and morale remain unknown; no qualifying stage is inferred.
    internal CampaignSourceTrainingLedger Interrupt()
    {
        if (Interrupted) throw new InvalidOperationException("Training is already interrupted.");
        return new(CurrentMorale, QualifyingStages, LastRecordedStage, InitialRuling, true, BaselineVotes);
    }

    // Caller must derive all facts from the authoritative effective world for
    // the full operation stage, never from the player's assertion alone.
    internal CampaignSourceTrainingLedger RecordStage(int stage, bool continued, bool eligibleArea,
        bool expendedCapabilityPoints, bool instructorPresentEntireStage,
        bool instructorTrained, bool instructorAtLeastHalfStrength, bool instructorTypeEligible)
    {
        if (!HasEstablishedStartingState)
            throw new InvalidOperationException("Training progress and morale are unknown; a starting ruling is required.");
        if (stage <= LastRecordedStage)
            throw new InvalidOperationException("Training stages must be recorded in increasing order.");
        var qualifies = !Interrupted && continued && eligibleArea && !expendedCapabilityPoints && instructorPresentEntireStage
            && instructorTrained && instructorAtLeastHalfStrength && instructorTypeEligible;
        var morale = CurrentMorale!.Value;
        var stages = QualifyingStages!.Value;
        if (qualifies && morale < TrainedMorale)
        {
            stages++;
            if (stages == 6) { morale++; stages = 0; }
        }
        return new(morale, stages, stage, InitialRuling, Interrupted, BaselineVotes);
    }

    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("policyId", PolicyId);
            writer.WriteString("formationId", FormationId);
            writer.WriteNumber("untrainedMorale", UntrainedMorale);
            writer.WriteNumber("trainedMorale", TrainedMorale);
            if (CurrentMorale is int morale) writer.WriteNumber("currentMorale", morale); else writer.WriteNull("currentMorale");
            if (QualifyingStages is int stages) writer.WriteNumber("qualifyingStages", stages); else writer.WriteNull("qualifyingStages");
            writer.WriteNumber("lastRecordedStage", LastRecordedStage);
            writer.WriteBoolean("interrupted", Interrupted);
            if (BaselineVotes != 0) writer.WriteNumber("baselineVotes", BaselineVotes);
            writer.WriteString("initialRuling", InitialRuling);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
}
