using System.Collections.ObjectModel;

namespace Cna.Core.Campaigns;

internal sealed record CampaignScheduledWorld6PreambleEvent(int ContractVersion, string CampaignId,
    string SetupHash, string CreationHash, int Ordinal, long PreviousVersion, long ResultVersion,
    string PreviousPositionId, string SuccessorPositionId, string ReferenceEventHash);

internal sealed record CampaignScheduledWorld6PreambleVerification(int VerifiedCount, CampaignScheduledWorld6PreambleCheckpoint Checkpoint,
    CampaignScheduledWorld6PreambleEvents.VerifiedCut? CompleteCut);

// Internal successor preamble proof; not yet admitted by Chronicle or Host.
internal static class CampaignScheduledWorld6PreambleEvents
{
    internal sealed class VerifiedCut
    {
        private VerifiedCut(CampaignScheduledWorld6Preamble.Cut cut) => Cut = cut;
        internal CampaignScheduledWorld6Preamble.Cut Cut { get; }

        internal static CampaignScheduledWorld6PreambleVerification VerifyHistory(CampaignCreationRequest request,
            CampaignScheduledWorld6Created creation, IEnumerable<CampaignScheduledWorld6PreambleEvent> events)
        {
            ArgumentNullException.ThrowIfNull(events);
            var preamble = CampaignScheduledWorld6Preamble.Run(request, creation);
            var expected = Build(preamble);
            var count = 0;
            foreach (var value in events)
            {
                if (count >= expected.Count || value != expected[count])
                    throw new InvalidOperationException("Invalid scheduled World6 preamble history.");
                count++;
            }
            return new CampaignScheduledWorld6PreambleVerification(count, preamble.Cut.Checkpoints[count],
                count == expected.Count ? new VerifiedCut(preamble.Cut) : null);
        }
    }

    public static IReadOnlyList<CampaignScheduledWorld6PreambleEvent> Create(
        CampaignCreationRequest request, CampaignScheduledWorld6Created creation) =>
        Build(CampaignScheduledWorld6Preamble.Run(request, creation));

    public static CampaignScheduledWorld6PreambleVerification Verify(CampaignCreationRequest request,
        CampaignScheduledWorld6Created creation, IEnumerable<CampaignScheduledWorld6PreambleEvent> events)
        => VerifiedCut.VerifyHistory(request, creation, events);

    private static ReadOnlyCollection<CampaignScheduledWorld6PreambleEvent> Build(CampaignScheduledWorld6PreambleResult preamble)
    {
        var cut = preamble.Cut;
        var creationHash = CampaignScheduledWorld6CreatedCodec.CalculateHash(cut.Creation);
        var priorVersion = cut.Creation.StateVersion;
        var priorPosition = cut.Creation.Position.PositionId;
        var events = new List<CampaignScheduledWorld6PreambleEvent>();
        foreach (var step in cut.Trace)
        {
            events.Add(new CampaignScheduledWorld6PreambleEvent(1, cut.Request.CampaignId,
                CampaignScheduledWorld6Phase.SetupHash, creationHash, events.Count + 1,
                priorVersion, step.ResultVersion, priorPosition, step.PositionId, step.ReferenceEventHash));
            priorVersion = step.ResultVersion;
            priorPosition = step.PositionId;
        }
        return Array.AsReadOnly(events.ToArray());
    }
}
