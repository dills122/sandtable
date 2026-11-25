using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;

namespace Cna.Core.Exercises;

/// <summary>Forward authority for the existing Stage 2 no-obligation preamble events.</summary>
internal static class CampaignStageEntryPreambleAuthority
{
    internal static ExerciseStepResult CompleteStageEntryOrganization(ExerciseSession session) =>
        Complete<StageEntryWeatherPending>(session, static current =>
        {
            var resolved = CampaignNativeNoObligationOrganizationV51.Create(
                current.CurrentStageEntryWeather!, current.CurrentContacts.AuthorityVersion);
            var contacts = current.CurrentContacts.AdvanceNoObligationOrganization(resolved);
            var next = current.WithNoObligationOrganization(resolved, contacts);
            return Accepted(next, resolved.CampaignId, resolved.PriorStateVersion, resolved.StateVersion,
                resolved.NextPositionId, resolved.SerializeCanonical());
        });

    internal static ExerciseStepResult CompleteStageEntryNavalConvoyArrival(ExerciseSession session) =>
        Complete<NoObligationOrganizationPending>(session, static current =>
        {
            var resolved = CampaignNativeNoObligationNavalConvoyArrivalV52.Create(
                current.CurrentNoObligationOrganization!, current.Context, current.CurrentContacts.AuthorityVersion);
            var contacts = current.CurrentContacts.AdvanceNoObligationNavalConvoyArrival(resolved);
            var next = current.WithNoObligationNavalConvoyArrival(resolved, contacts);
            return Accepted(next, resolved.CampaignId, resolved.PriorStateVersion, resolved.StateVersion,
                resolved.NextPositionId, resolved.SerializeCanonical());
        });

    internal static ExerciseStepResult CompleteStageEntryFleetAssignment(ExerciseSession session) =>
        Complete<NoObligationNavalConvoyArrivalPending>(session, static current =>
        {
            var resolved = CampaignNativeNoObligationFleetAssignmentV53.Create(
                current.CurrentNoObligationNavalConvoyArrival!, current.CurrentContacts.AuthorityVersion);
            var contacts = current.CurrentContacts.AdvanceNoObligationFleetAssignment(resolved);
            var next = current.WithNoObligationFleetAssignment(resolved, contacts);
            return Accepted(next, resolved.CampaignId, resolved.PriorStateVersion, resolved.StateVersion,
                resolved.NextPositionId, resolved.SerializeCanonical());
        });

    internal static ExerciseStepResult CompleteStageEntryFleetRepair(ExerciseSession session) =>
        Complete<NoObligationFleetAssignmentPending>(session, static current =>
        {
            var resolved = CampaignNativeNoObligationFleetRepairV54.Create(
                current.CurrentNoObligationFleetAssignment!, current.CurrentContacts.AuthorityVersion);
            var contacts = current.CurrentContacts.AdvanceNoObligationFleetRepair(resolved);
            var next = current.WithNoObligationFleetRepair(resolved, contacts);
            return Accepted(next, resolved.CampaignId, resolved.PriorStateVersion, resolved.StateVersion,
                resolved.NextPositionId, resolved.SerializeCanonical());
        });

    private static ExerciseStepResult Complete<TPending>(ExerciseSession session,
        Func<ExerciseSession, ExerciseStepResult> complete) where TPending : PendingDecision
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.Pending is not TPending)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);

        try
        {
            return complete(session);
        }
        catch (Exception error) when (error is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    private static ExerciseStepResult Accepted(ExerciseSession next, string campaignId, long priorStateVersion,
        long stateVersion, string nextPositionId, byte[] bytes)
    {
        var receipt = new CampaignActionAcceptanceReceipt(campaignId, priorStateVersion,
            stateVersion, nextPositionId, CampaignActionAudience.System,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)));
        return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(receipt, bytes, bytes));
    }
}
