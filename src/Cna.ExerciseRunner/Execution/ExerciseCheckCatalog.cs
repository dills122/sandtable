using Cna.Core.Actions;
using Cna.ExerciseRunner.Artifacts;

namespace Cna.ExerciseRunner.Execution;

public enum ExerciseCheckId
{
    AuthorityQueryValid,
    ActiveAudienceCardinality,
    SelectedActionMembership,
    AcceptedEventCardinality,
    CheckpointContinuity,
    TerminalBoundary,
    HistoryReconstruction,
    Readjudication,
}

public enum ExerciseCheckFailureCode
{
    None,
    AuthorityQueryRejected,
    AuthorityQueryCoordinateMismatch,
    NoActiveAudience,
    MultipleActiveAudiences,
    SelectedActionNotCurrent,
    ActionRejected,
    EventCardinalityMismatch,
    CampaignMismatch,
    RulesetMismatch,
    StateVersionDiscontinuity,
    PositionMismatch,
    TerminalBoundaryNotReached,
    ReconstructionMismatch,
    ReadjudicationMismatch,
}

public sealed record ExerciseCheckResult
{
    public const int CurrentContractVersion = 1;
    public const string SchemeId = "sandtable.exercise-checks.v1";

    private ExerciseCheckResult(
        ExerciseCheckId checkId,
        int? stepOrdinal,
        CampaignActionAudience? audience,
        ExerciseCheckFailureCode failureCode)
    {
        if (!Enum.IsDefined(checkId)) throw new ArgumentOutOfRangeException(nameof(checkId));
        if (stepOrdinal is < 0) throw new ArgumentOutOfRangeException(nameof(stepOrdinal));
        if (audience.HasValue && !Enum.IsDefined(audience.Value))
            throw new ArgumentOutOfRangeException(nameof(audience));
        if (!Enum.IsDefined(failureCode))
            throw new ArgumentOutOfRangeException(nameof(failureCode));
        ValidateScope(checkId, stepOrdinal, audience);
        ContractVersion = CurrentContractVersion;
        ContractSchemeId = SchemeId;
        CheckId = checkId;
        StepOrdinal = stepOrdinal;
        Audience = audience;
        FailureCode = failureCode;
    }

    public int ContractVersion { get; }
    public string ContractSchemeId { get; }
    public ExerciseCheckId CheckId { get; }
    public int? StepOrdinal { get; }
    public CampaignActionAudience? Audience { get; }
    public bool IsPassed => FailureCode == ExerciseCheckFailureCode.None;
    public ExerciseCheckFailureCode FailureCode { get; }

    public static ExerciseCheckResult Passed(
        ExerciseCheckId checkId,
        int? stepOrdinal,
        CampaignActionAudience? audience) =>
        new(checkId, stepOrdinal, audience, ExerciseCheckFailureCode.None);

    public static ExerciseCheckResult Failed(
        ExerciseCheckId checkId,
        int? stepOrdinal,
        CampaignActionAudience? audience,
        ExerciseCheckFailureCode failureCode)
    {
        if (failureCode == ExerciseCheckFailureCode.None)
            throw new ArgumentOutOfRangeException(nameof(failureCode));
        return new ExerciseCheckResult(checkId, stepOrdinal, audience, failureCode);
    }

    private static void ValidateScope(
        ExerciseCheckId checkId,
        int? stepOrdinal,
        CampaignActionAudience? audience)
    {
        var valid = checkId switch
        {
            ExerciseCheckId.AuthorityQueryValid => stepOrdinal.HasValue && audience.HasValue,
            ExerciseCheckId.ActiveAudienceCardinality => stepOrdinal.HasValue && !audience.HasValue,
            ExerciseCheckId.SelectedActionMembership
                or ExerciseCheckId.AcceptedEventCardinality
                or ExerciseCheckId.CheckpointContinuity =>
                stepOrdinal.HasValue && audience.HasValue,
            ExerciseCheckId.TerminalBoundary
                or ExerciseCheckId.HistoryReconstruction
                or ExerciseCheckId.Readjudication =>
                !stepOrdinal.HasValue && !audience.HasValue,
            _ => false,
        };
        if (!valid) throw new ArgumentException("The check scope is invalid for its catalog ID.");
    }
}

public sealed class ExerciseCheckResults
{
    public const int CurrentContractVersion = 1;
    public const string SchemeId = ExerciseCheckResult.SchemeId;

    internal ExerciseCheckResults(IEnumerable<ExerciseCheckResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        var copy = results.ToArray();
        ValidateOrder(copy);
        ContractVersion = CurrentContractVersion;
        ContractSchemeId = SchemeId;
        Results = Array.AsReadOnly(copy);
    }

    public int ContractVersion { get; }
    public string ContractSchemeId { get; }
    public IReadOnlyList<ExerciseCheckResult> Results { get; }

    public ExerciseCheckResults WithReadjudication(ReadjudicationProof proof)
    {
        ArgumentNullException.ThrowIfNull(proof);
        if (Results.Count == 0
            || Results[^1].CheckId != ExerciseCheckId.HistoryReconstruction
            || !Results[^1].IsPassed)
            throw new InvalidOperationException(
                "Re-adjudication can follow only a passed reconstruction check.");
        var result = proof.IsVerified
            ? ExerciseCheckResult.Passed(ExerciseCheckId.Readjudication, null, null)
            : ExerciseCheckResult.Failed(
                ExerciseCheckId.Readjudication,
                null,
                null,
                ExerciseCheckFailureCode.ReadjudicationMismatch);
        return new ExerciseCheckResults(Results.Append(result));
    }

    private static void ValidateOrder(ExerciseCheckResult[] results)
    {
        var previousStep = -1;
        var previousOrdinal = -1;
        var runOrdinal = -1;
        var runLevelStarted = false;
        for (var index = 0; index < results.Length; index++)
        {
            var result = results[index]
                ?? throw new ArgumentException("Check results cannot contain null.", nameof(results));
            if (index > 0
                && !results[index - 1].IsPassed
                && !(results[index - 1].StepOrdinal.HasValue
                    && result.CheckId == ExerciseCheckId.TerminalBoundary))
                throw new ArgumentException(
                    "No check may follow a required failed check.",
                    nameof(results));
            if (result.StepOrdinal.HasValue)
            {
                if (runLevelStarted)
                    throw new ArgumentException(
                        "Step checks cannot follow run checks.",
                        nameof(results));
                var step = result.StepOrdinal.Value;
                var ordinal = StepCatalogOrdinal(result);
                if (step < previousStep
                    || (step == previousStep && ordinal != previousOrdinal + 1)
                    || (step > previousStep && (step != previousStep + 1 || ordinal != 0))
                    || (step > previousStep && previousStep >= 0 && previousOrdinal != 6))
                    throw new ArgumentException("Step checks are out of catalog order.", nameof(results));
                previousStep = step;
                previousOrdinal = ordinal;
                continue;
            }

            runLevelStarted = true;
            var ordinalValue = RunCatalogOrdinal(result.CheckId);
            if (ordinalValue != runOrdinal + 1)
                throw new ArgumentException("Run checks are out of catalog order.", nameof(results));
            runOrdinal = ordinalValue;
        }
    }

    private static int StepCatalogOrdinal(ExerciseCheckResult result) => result.CheckId switch
    {
        ExerciseCheckId.AuthorityQueryValid => result.Audience switch
        {
            CampaignActionAudience.System => 0,
            CampaignActionAudience.Axis => 1,
            CampaignActionAudience.Commonwealth => 2,
            _ => throw new ArgumentException("Unknown audience order."),
        },
        ExerciseCheckId.ActiveAudienceCardinality => 3,
        ExerciseCheckId.SelectedActionMembership => 4,
        ExerciseCheckId.AcceptedEventCardinality => 5,
        ExerciseCheckId.CheckpointContinuity => 6,
        _ => throw new ArgumentException("A run check cannot have step scope."),
    };

    private static int RunCatalogOrdinal(ExerciseCheckId checkId) => checkId switch
    {
        ExerciseCheckId.TerminalBoundary => 0,
        ExerciseCheckId.HistoryReconstruction => 1,
        ExerciseCheckId.Readjudication => 2,
        _ => throw new ArgumentException("A step check cannot have run scope."),
    };
}
