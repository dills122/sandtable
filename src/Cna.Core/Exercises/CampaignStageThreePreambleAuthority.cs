using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Exercises;

/// <summary>Forward authority for the existing Stage 3 preamble events.</summary>
internal static class CampaignStageThreePreambleAuthority
{
    internal static ExerciseStepResult SubmitStageThreeInitiativeDeclaration(ExerciseSession session,
        CampaignActionAudience audience, string actionId, bool actFirst) =>
        Complete<StageEntrySecondAssaultPending>(session, current =>
        {
            // A retained post-combat pass alone is not enough: only the exact
            // Stage 3 declaration position may advance into the next stage.
            if (CampaignExercises.QueryCheckpoint(current).PositionId != "land.position.operation-3.initiative-declaration")
                throw new InvalidOperationException("Stage 3 Initiative Declaration is not available at this position.");
            if (current.CurrentHistory.LastOrDefault() is not CampaignNativePostCombatPassV42 predecessor
                || predecessor.StateVersion != current.CurrentContacts.AuthorityVersion)
                throw new InvalidOperationException("Initiative Declaration requires the latest completed terminal pass.");

            var side = audience switch
            {
                CampaignActionAudience.Axis => LandSide.Axis,
                CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
                _ => throw new InvalidOperationException("Initiative Declaration requires an owner."),
            };
            var holder = current.CurrentSnapshot.InitiativeHolder
                ?? throw new InvalidOperationException("Initiative Declaration requires a resolved holder.");
            if (side != holder)
                throw new InvalidOperationException("Only the initiative holder may declare.");

            var declared = CampaignNativeInitiativeDeclarationV49.Create(predecessor, holder, actFirst,
                current.CurrentSnapshot.Setup.InitialGameTurn, current.CurrentContacts.AuthorityVersion);
            var contacts = current.CurrentContacts.AdvanceInitiativeDeclaration(declared);
            var next = current.WithThirdStageInitiativeDeclaration(declared, contacts);
            var bytes = declared.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(declared.CampaignId, declared.PriorStateVersion,
                declared.StateVersion, declared.NextPositionId, audience, actionId);
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(receipt, bytes, bytes));
        });

    internal static ExerciseStepResult CompleteStageThreeOrganization(ExerciseSession session) =>
        Complete<ThirdStageWeatherPending>(session, static current =>
        {
            var resolved = CampaignNativeNoObligationOrganizationV51.Create(
                current.CurrentThirdStageWeather!, current.CurrentContacts.AuthorityVersion);
            var contacts = current.CurrentContacts.AdvanceNoObligationOrganization(resolved);
            var next = current.WithThirdStageNoObligationOrganization(resolved, contacts);
            return Accepted(next, resolved.CampaignId, resolved.PriorStateVersion, resolved.StateVersion,
                resolved.NextPositionId, resolved.SerializeCanonical());
        });

    internal static ExerciseStepResult CompleteStageThreeNavalConvoyArrival(ExerciseSession session) =>
        Complete<ThirdStageNoObligationOrganizationPending>(session, static current =>
        {
            var resolved = CampaignNativeNoObligationNavalConvoyArrivalV52.Create(
                current.CurrentThirdStageNoObligationOrganization!, current.Context, current.CurrentContacts.AuthorityVersion);
            var contacts = current.CurrentContacts.AdvanceNoObligationNavalConvoyArrival(resolved);
            var next = current.WithThirdStageNoObligationNavalConvoyArrival(resolved, contacts);
            return Accepted(next, resolved.CampaignId, resolved.PriorStateVersion, resolved.StateVersion,
                resolved.NextPositionId, resolved.SerializeCanonical());
        });

    internal static ExerciseStepResult CompleteStageThreeFleetAssignment(ExerciseSession session) =>
        Complete<ThirdStageNoObligationNavalConvoyArrivalPending>(session, static current =>
        {
            var resolved = CampaignNativeNoObligationFleetAssignmentV53.Create(
                current.CurrentThirdStageNoObligationNavalConvoyArrival!, current.CurrentContacts.AuthorityVersion);
            var contacts = current.CurrentContacts.AdvanceNoObligationFleetAssignment(resolved);
            var next = current.WithThirdStageNoObligationFleetAssignment(resolved, contacts);
            return Accepted(next, resolved.CampaignId, resolved.PriorStateVersion, resolved.StateVersion,
                resolved.NextPositionId, resolved.SerializeCanonical());
        });

    internal static ExerciseStepResult CompleteStageThreeFleetRepair(ExerciseSession session) =>
        Complete<ThirdStageNoObligationFleetAssignmentPending>(session, static current =>
        {
            var resolved = CampaignNativeNoObligationFleetRepairV54.Create(
                current.CurrentThirdStageNoObligationFleetAssignment!, current.CurrentContacts.AuthorityVersion);
            var contacts = current.CurrentContacts.AdvanceNoObligationFleetRepair(resolved);
            var next = current.WithThirdStageNoObligationFleetRepair(resolved, contacts);
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
