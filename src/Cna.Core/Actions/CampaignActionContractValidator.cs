using Cna.Core.Content;

namespace Cna.Core.Actions;

internal static class CampaignActionContractValidator
{
    public static bool IsValidSubmission(CampaignActionSubmission submission) =>
        submission.ContractVersion == CampaignActionSubmission.CurrentContractVersion
        && IsStableId(submission.CampaignId)
        && submission.ExpectedStateVersion >= 1
        && IsStableId(submission.ExpectedPositionId)
        && Enum.IsDefined(submission.Audience)
        && IsSha256(submission.ActionId);

    private static bool IsStableId(string? value)
    {
        try
        {
            _ = ContentContractGuards.RequireStableId(value!, nameof(value));
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool IsSha256(string? value)
    {
        try
        {
            _ = ContentContractGuards.RequireSha256(value!, nameof(value));
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
