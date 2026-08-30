using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.ExerciseRunner.Artifacts;

namespace Cna.ExerciseRunner.Execution;

internal static class PairedManeuverPairingEvidence
{
    internal static string HashCreationInputs(
        PairedManeuverManifest manifest,
        PairedManeuverPairManifest pair)
    {
        var identity = new ExerciseRunIdentity(
            manifest.RootSeed,
            manifest.ManeuverId,
            pair.Repetition,
            pair.PairKey);
        return ReplayEvidenceHasher.HashBytes(SerializeCreationInputs(
            ExerciseExecutor.CreateRequest(
                pair.MaterializeBaseline(manifest.RootSeed),
                identity)));
    }

    internal static byte[] SerializeCreationInputs(CampaignCreationRequest request)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("schemeId", "sandtable.exercise-pairing-inputs.v1");
            writer.WriteString("campaignId", request.CampaignId);
            writer.WriteString("rulesetHash", request.RulesetHash);
            writer.WriteNumber("seed", request.Seed);
            writer.WriteString("setupId", request.SetupId);
            writer.WriteString("setupHash", request.SetupHash);
            writer.WriteString("contentPackId", request.ContentPackId);
            writer.WriteString("contentHash", request.ContentHash);
            writer.WriteString("scenarioId", request.ScenarioId);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static PairedAcceptedActionDivergence FindDivergence(
        IReadOnlyList<PairedAcceptedActionIdentity> baseline,
        IReadOnlyList<PairedAcceptedActionIdentity> candidate)
    {
        var common = Math.Min(baseline.Count, candidate.Count);
        for (var ordinal = 0; ordinal < common; ordinal++)
        {
            if (baseline[ordinal].Audience != candidate[ordinal].Audience
                || !string.Equals(
                    baseline[ordinal].ActionId,
                    candidate[ordinal].ActionId,
                    StringComparison.Ordinal))
                return Divergence(ordinal, baseline[ordinal], candidate[ordinal]);
        }
        if (baseline.Count != candidate.Count)
            return Divergence(
                common,
                common < baseline.Count ? baseline[common] : null,
                common < candidate.Count ? candidate[common] : null);
        return new PairedAcceptedActionDivergence(
            PairedDivergenceKind.None,
            null,
            null,
            null,
            null,
            null);
    }

    private static PairedAcceptedActionDivergence Divergence(
        int ordinal,
        PairedAcceptedActionIdentity? baseline,
        PairedAcceptedActionIdentity? candidate) => new(
        PairedDivergenceKind.AcceptedAction,
        ordinal,
        baseline?.Audience,
        baseline?.ActionId,
        candidate?.Audience,
        candidate?.ActionId);
}
