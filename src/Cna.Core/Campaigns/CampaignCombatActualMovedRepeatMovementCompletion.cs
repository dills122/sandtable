using System.Text;
using System.Text.Json;
using static Cna.Core.Campaigns.CampaignCombatActualMovedRepeatMovementCompletionCodec;
namespace Cna.Core.Campaigns;
internal static class CampaignCombatActualMovedRepeatMovementCompletion
{
    public static CombatActualMovedRepeatMovementResult ReplayTrustedSource(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request, IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release, IReadOnlyList<byte[]> control, IReadOnlyList<byte[]> movement, IReadOnlyList<byte[]> completion, Func<string, byte[]> dependencies)
    {
        var packet = Object(source, "Source"); SourceBounds(packet);
        Require(completion.Count == 0 && packet["lifecycleEventCanonicalUtf8"]!.AsArray().Count == 0, 7);
        var ledgers = new[] { prefix, lifecycle, breakdown, selection, round, paid, release, control, movement }.Select(l => Own(l, l.Count, "AncestorInput")).ToArray();
        var retained = SnapshotDependencies(dependencies); var parent = Encoding.ASCII.GetBytes(packet["movementSourceCanonicalUtf8"]!.GetValue<string>());
        var result = Inherited(() => CampaignCombatActualMovedRepeatMovement.ReplayTrustedSource(parent, request, ledgers[0], ledgers[1], ledgers[2], ledgers[3], ledgers[4], ledgers[5], ledgers[6], ledgers[7], ledgers[8], retained));
        Require(result.Control.GetProperty("phase").GetString() == "movement" && result.Control.GetProperty("activeCycle").GetProperty("ordinal").GetInt32() == 2 && result.Control.GetProperty("publicationReceiptId").ValueKind == JsonValueKind.String, 4);
        return result;
    }
    public static CombatActualMovedRepeatMovementResult ApplyTrustedSource(ReadOnlySpan<byte> source, CampaignCombatCreationRequest request, IReadOnlyList<byte[]> prefix, IReadOnlyList<byte[]> lifecycle, IReadOnlyList<byte[]> breakdown, IReadOnlyList<byte[]> selection, IReadOnlyList<byte[]> round, IReadOnlyList<byte[]> paid, IReadOnlyList<byte[]> release, IReadOnlyList<byte[]> control, IReadOnlyList<byte[]> movement, IReadOnlyList<byte[]> completion, ReadOnlySpan<byte> currentInput, Func<string, byte[]> dependencies, bool admissionEnabled = true)
    { _ = Copy(currentInput, "Input"); _ = ReplayTrustedSource(source, request, prefix, lifecycle, breakdown, selection, round, paid, release, control, movement, completion, dependencies); throw new JsonException("CMB-MRC-007"); }
}
