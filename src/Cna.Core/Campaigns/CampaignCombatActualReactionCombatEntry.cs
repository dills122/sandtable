using System.Text.Json;
namespace Cna.Core.Campaigns;
internal sealed class CombatActualReactionCombatEntryResult
{
    public byte[] SourceBytes => throw new NotSupportedException();
    public byte[] CommandBytes => throw new NotSupportedException();
    public JsonElement Control => throw new NotSupportedException();
    public CampaignCombatCandidate? Candidate => throw new NotSupportedException();
    public CampaignWorldSnapshotV7 World => throw new NotSupportedException();
}
internal static class CampaignCombatActualReactionCombatEntry
{
    public static CombatActualReactionCombatEntryResult ReplayTrustedSource(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request, IReadOnlyList<byte[]> prefixInputs, IReadOnlyList<byte[]> entryInputs)
        => throw new NotSupportedException("Actual Reaction closure to Combat entry is not implemented.");
    public static CombatActualReactionCombatEntryResult ApplyTrustedSource(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request, IReadOnlyList<byte[]> prefixInputs, IReadOnlyList<byte[]> entryInputs, ReadOnlySpan<byte> currentInput)
        => throw new NotSupportedException("Actual Reaction closure to Combat entry is not implemented.");
}
