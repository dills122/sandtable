using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Exercises;
using Cna.Core.Hosting;

namespace Cna.OrleansHost.Api;

/// <summary>
/// One pregame-source admission mechanic: a gate ("has this already applied/is it ready"),
/// the accept step, and the command that reproduces it canonically. Replaces what was ~30
/// near-identical 10-line method bodies (RequireOperator -&gt; Load -&gt; already-applied
/// check -&gt; Accept -&gt; PersistAccepted) each hand-written with its own kind constant and
/// error message -- see docs/work/plans/2026-11-23-codebase-overhaul-direction.md Section 4.
/// A parameterized class (not one subclass per mechanic) because every one of these ~30
/// mechanics is fully uniform with zero per-kind special case; Section 1's handler classes
/// use full types instead because those genuinely need a per-kind extension point.
/// </summary>
internal sealed class SourceAdmissionHandler
{
    internal SourceAdmissionHandler(string kind, string unavailableMessage,
        Func<ExerciseSession, bool> canAdmit,
        Func<ExerciseSession, ExerciseStepResult> accept,
        Func<ExerciseSession, ExerciseStepResult, CampaignActionSubmission> command)
    {
        Kind = kind;
        UnavailableMessage = unavailableMessage;
        this.canAdmit = canAdmit;
        this.accept = accept;
        this.command = command;
    }

    private readonly Func<ExerciseSession, bool> canAdmit;
    private readonly Func<ExerciseSession, ExerciseStepResult> accept;
    private readonly Func<ExerciseSession, ExerciseStepResult, CampaignActionSubmission> command;

    internal string Kind { get; }
    internal string UnavailableMessage { get; }
    internal bool CanAdmit(ExerciseSession session) => canAdmit(session);
    internal ExerciseStepResult Accept(ExerciseSession session) => accept(session);
    internal CampaignActionSubmission Command(ExerciseSession session, ExerciseStepResult accepted) =>
        command(session, accepted);
}

internal sealed partial class CampaignDurableAuthorityJournal
{
    private bool AdmitCore(string campaignId, string operatorSecret, SourceAdmissionHandler handler)
    {
        RequireWritable();
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(operatorSecret);
        var head = Load(connection, transaction, campaignId);
        if (head.Request.ContractVersion != CampaignPregameSourceCreation.RequestVersion)
            throw new InvalidOperationException("Campaign is not a source-backed setup.");
        if (!handler.CanAdmit(head.Session)) return false;
        var accepted = handler.Accept(head.Session);
        if (!accepted.IsAccepted)
            throw new InvalidOperationException(handler.UnavailableMessage);
        var command = handler.Command(head.Session, accepted);
        PersistAccepted(connection, transaction, campaignId, head,
            handler.Kind, null, null, null, null, command, accepted);
        transaction.Commit();
        return true;
    }

    private static readonly SourceAdmissionHandler admitSourceAxisSupplementHandler = new(
        CampaignHostAcceptedStep.SourceAxisSupplementKind,
        "Source supplement is not available at this position.",
        session => !CampaignExercises.IsSourceAxisSupplementAdmitted(session),
        session => CampaignExercises.AdmitSourceAxisSupplement(session),
        (session, accepted) => CampaignHostAdapter.SourceAxisSupplementCommand(session, accepted));
    private static readonly SourceAdmissionHandler admitSourceCommonwealthSupplementHandler = new(
        CampaignHostAcceptedStep.SourceCommonwealthSupplementKind,
        "Source supplement is not available at this position.",
        session => !CampaignExercises.IsSourceCommonwealthSupplementAdmitted(session),
        session => CampaignExercises.AdmitSourceCommonwealthSupplement(session),
        (session, accepted) => CampaignHostAdapter.SourceCommonwealthSupplementCommand(session, accepted));
    private static readonly SourceAdmissionHandler admitSourcePriorityMotorizationHandler = new(
        CampaignHostAcceptedStep.SourcePriorityMotorizationKind,
        "Source policy is not available at this position.",
        session => !CampaignExercises.IsSourcePriorityMotorizationAdmitted(session),
        session => CampaignExercises.AdmitSourcePriorityMotorization(session),
        (session, accepted) => CampaignHostAdapter.SourcePriorityMotorizationCommand(session, accepted));
    private static readonly SourceAdmissionHandler admitSourceAttachedInitialLoadsHandler = new(
        CampaignHostAcceptedStep.SourceAttachedInitialLoadsKind,
        "Source policy is not available at this position.",
        session => !CampaignExercises.IsSourceAttachedInitialLoadsAdmitted(session),
        session => CampaignExercises.AdmitSourceAttachedInitialLoads(session),
        (session, accepted) => CampaignHostAdapter.SourceAttachedInitialLoadsCommand(session, accepted));
    private static readonly SourceAdmissionHandler admitSourceInitialWrecksHandler = new(
        CampaignHostAcceptedStep.SourceInitialWrecksKind,
        "Source policy is not available at this position.",
        session => !CampaignExercises.IsSourceInitialWrecksAdmitted(session),
        session => CampaignExercises.AdmitSourceInitialWrecks(session),
        (session, accepted) => CampaignHostAdapter.SourceInitialWrecksCommand(session, accepted));
    private static readonly SourceAdmissionHandler admitSourceAxisFreeSetupHandler = new(
        CampaignHostAcceptedStep.SourceAxisFreeSetupKind,
        "Source policy is not available at this position.",
        session => !CampaignExercises.IsSourceAxisFreeSetupAdmitted(session),
        session => CampaignExercises.AdmitSourceAxisFreeSetup(session),
        (session, accepted) => CampaignHostAdapter.SourceAxisFreeSetupCommand(session, accepted));
    private static readonly SourceAdmissionHandler admitSourceAxisCorpsSetupHandler = new(
        CampaignHostAcceptedStep.SourceAxisCorpsSetupKind,
        "Source policy is not available at this position.",
        session => !CampaignExercises.IsSourceAxisCorpsSetupAdmitted(session),
        session => CampaignExercises.AdmitSourceAxisCorpsSetup(session),
        (session, accepted) => CampaignHostAdapter.SourceAxisCorpsSetupCommand(session, accepted));
    private static readonly SourceAdmissionHandler admitSourceMalettiHierarchyHandler = new(
        CampaignHostAcceptedStep.SourceMalettiHierarchyKind,
        "Source policy is not available at this position.",
        session => !CampaignExercises.IsSourceMalettiHierarchyAdmitted(session),
        session => CampaignExercises.AdmitSourceMalettiHierarchy(session),
        (session, accepted) => CampaignHostAdapter.SourceMalettiHierarchyCommand(session, accepted));
    private static readonly SourceAdmissionHandler admitSourceGt2CorrectionHandler = new(
        CampaignHostAcceptedStep.SourceGt2CorrectionKind,
        "Source policy is not available at this position.",
        session => !CampaignExercises.IsSourceGt2CorrectionAdmitted(session),
        session => CampaignExercises.AdmitSourceGt2Correction(session),
        (session, accepted) => CampaignHostAdapter.SourceGt2CorrectionCommand(session, accepted));
    private static readonly SourceAdmissionHandler admitSourceCwPriorityCatalogHandler = new(
        CampaignHostAcceptedStep.SourceCwPriorityCatalogKind,
        "Source policy is not available at this position.",
        session => !CampaignExercises.IsSourceCwPriorityCatalogAdmitted(session),
        session => CampaignExercises.AdmitSourceCwPriorityCatalog(session),
        (session, accepted) => CampaignHostAdapter.SourceCwPriorityCatalogCommand(session, accepted));
    private static readonly SourceAdmissionHandler admitSourceCompactCheckpointHandler = new(
        CampaignHostAcceptedStep.SourceCompactCheckpointKind,
        "Source policy is not available at this position.",
        session => !CampaignExercises.IsSourceCompactCheckpointAdmitted(session),
        session => CampaignExercises.AdmitSourceCompactCheckpoint(session),
        (session, accepted) => CampaignHostAdapter.SourceCompactCheckpointCommand(session, accepted));
    private static readonly SourceAdmissionHandler admitSourceFourthIndianWorldHandler = new(
        CampaignHostAcceptedStep.SourceFourthIndianWorldKind,
        "Source policy is not available at this position.",
        session => !CampaignExercises.IsSourceFourthIndianWorldAdmitted(session),
        session => CampaignExercises.AdmitSourceFourthIndianWorld(session),
        (session, accepted) => CampaignHostAdapter.SourceFourthIndianWorldCommand(session, accepted));
    private static readonly SourceAdmissionHandler admitSourceMatruhGroupedWorldHandler = new(
        CampaignHostAcceptedStep.SourceMatruhGroupedWorldKind,
        "Source policy is not available at this position.",
        session => !CampaignExercises.IsSourceMatruhGroupedWorldAdmitted(session),
        session => CampaignExercises.AdmitSourceMatruhGroupedWorld(session),
        (session, accepted) => CampaignHostAdapter.SourceMatruhGroupedWorldCommand(session, accepted));
    private static readonly SourceAdmissionHandler admitSourceMalettiGroupedWorldHandler = new(
        CampaignHostAcceptedStep.SourceMalettiGroupedWorldKind,
        "Source policy is not available at this position.",
        session => !CampaignExercises.IsSourceMalettiGroupedWorldAdmitted(session),
        session => CampaignExercises.AdmitSourceMalettiGroupedWorld(session),
        (session, accepted) => CampaignHostAdapter.SourceMalettiGroupedWorldCommand(session, accepted));
    private static readonly SourceAdmissionHandler admitSourceLibyanGroupedWorldHandler = new(
        CampaignHostAcceptedStep.SourceLibyanGroupedWorldKind,
        "Source policy is not available at this position.",
        session => !CampaignExercises.IsSourceLibyanGroupedWorldAdmitted(session),
        session => CampaignExercises.AdmitSourceLibyanGroupedWorld(session),
        (session, accepted) => CampaignHostAdapter.SourceLibyanGroupedWorldCommand(session, accepted));
    private static readonly SourceAdmissionHandler admitSourceItalianDivisionsGroupedWorldHandler = new(
        CampaignHostAcceptedStep.SourceItalianDivisionsGroupedWorldKind,
        "Source policy is not available at this position.",
        session => !CampaignExercises.IsSourceItalianDivisionsGroupedAdmitted(session),
        session => CampaignExercises.AdmitSourceItalianDivisionsGroupedWorld(session),
        (session, accepted) => CampaignHostAdapter.SourceItalianDivisionsGroupedWorldCommand(session, accepted));
    private static readonly SourceAdmissionHandler admitSourceSeventhArmoredGroupedWorldHandler = new(
        CampaignHostAcceptedStep.SourceSeventhArmoredGroupedWorldKind,
        "Source policy is not available at this position.",
        session => !CampaignExercises.IsSourceSeventhArmoredGroupedAdmitted(session),
        session => CampaignExercises.AdmitSourceSeventhArmoredGroupedWorld(session),
        (session, accepted) => CampaignHostAdapter.SourceSeventhArmoredGroupedWorldCommand(session, accepted));
    private static readonly SourceAdmissionHandler admitSourceSixteenthBrigadeGroupedWorldHandler = new(
        CampaignHostAcceptedStep.SourceSixteenthBrigadeGroupedWorldKind,
        "Source policy is not available at this position.",
        session => !CampaignExercises.IsSourceSixteenthBrigadeGroupedAdmitted(session),
        session => CampaignExercises.AdmitSourceSixteenthBrigadeGroupedWorld(session),
        (session, accepted) => CampaignHostAdapter.SourceSixteenthBrigadeGroupedWorldCommand(session, accepted));
    private static readonly SourceAdmissionHandler admitSourceCatanzaroGroupedWorldHandler = new(
        CampaignHostAcceptedStep.SourceCatanzaroGroupedWorldKind,
        "Source policy is not available at this position.",
        session => !CampaignExercises.IsSourceCatanzaroGroupedAdmitted(session),
        session => CampaignExercises.AdmitSourceCatanzaroGroupedWorld(session),
        (session, accepted) => CampaignHostAdapter.SourceCatanzaroGroupedWorldCommand(session, accepted));
    private static readonly SourceAdmissionHandler admitSourceMatruhFormationHandler = new(
        CampaignHostAcceptedStep.SourceMatruhFormationKind,
        "Source policy is not available at this position.",
        session => !CampaignExercises.IsSourceMatruhFormationAdmitted(session),
        session => CampaignExercises.AdmitSourceMatruhFormation(session),
        (session, accepted) => CampaignHostAdapter.SourceMatruhFormationCommand(session, accepted));
    private static readonly SourceAdmissionHandler admitSourceCwFreeGunsHandler = new(
        CampaignHostAcceptedStep.SourceCwFreeGunsKind,
        "Source policy is not available at this position.",
        session => !CampaignExercises.IsSourceCwFreeGunsAdmitted(session),
        session => CampaignExercises.AdmitSourceCwFreeGuns(session),
        (session, accepted) => CampaignHostAdapter.SourceCwFreeGunsCommand(session, accepted));
    private static readonly SourceAdmissionHandler admitSourceAxisPriorityMotorizationHandler = new(
        CampaignHostAcceptedStep.SourceAxisPriorityMotorizationKind,
        "Source policy is not available at this position.",
        session => !CampaignExercises.IsSourceAxisPriorityMotorizationAdmitted(session),
        session => CampaignExercises.AdmitSourceAxisPriorityMotorization(session),
        (session, accepted) => CampaignHostAdapter.SourceAxisPriorityMotorizationCommand(session, accepted));
    private static readonly SourceAdmissionHandler admitSourceAxisMinorGarrisonsHandler = new(
        CampaignHostAcceptedStep.SourceAxisMinorGarrisonsKind,
        "Source garrisons are not available at this position.",
        session => !CampaignExercises.IsSourceAxisMinorGarrisonsAdmitted(session),
        session => CampaignExercises.AdmitSourceAxisMinorGarrisons(session),
        (session, accepted) => CampaignHostAdapter.SourceAxisMinorGarrisonsCommand(session, accepted));
    private static readonly SourceAdmissionHandler admitSourceBattlefieldHandler = new(
        CampaignHostAcceptedStep.SourceBattlefieldKind,
        "Source battlefield is not available at this position.",
        session => !CampaignExercises.IsSourceBattlefieldAdmitted(session),
        session => CampaignExercises.AdmitSourceBattlefield(session),
        (session, accepted) => CampaignHostAdapter.SourceBattlefieldCommand(session, accepted));
    private static readonly SourceAdmissionHandler admitSourceCairoSetupHandler = new(
        CampaignHostAcceptedStep.SourceCairoSetupKind,
        "Cairo setup is not available at this position.",
        session => !CampaignExercises.IsSourceCairoSetupAdmitted(session),
        session => CampaignExercises.AdmitSourceCairoSetup(session),
        (session, accepted) => CampaignHostAdapter.SourceCairoSetupCommand(session, accepted));
    private static readonly SourceAdmissionHandler admitSourceCairoWorldHandler = new(
        CampaignHostAcceptedStep.SourceCairoWorldKind,
        "Cairo World requires all supported player placements.",
        session => !CampaignExercises.IsSourceCairoWorldAdmitted(session),
        session => CampaignExercises.AdmitSourceCairoWorld(session),
        (session, accepted) => CampaignHostAdapter.SourceCairoWorldCommand(session, accepted));
    private static readonly SourceAdmissionHandler promoteSourceMalettiHandler = new(
        CampaignHostAcceptedStep.SourceMalettiKind,
        "Maletti promotion is not available at this position.",
        session => CampaignExercises.CanPromoteSourceMaletti(session),
        session => CampaignExercises.PromoteSourceMaletti(session),
        (session, accepted) => CampaignHostAdapter.SourceMalettiCommand(session, accepted));
    private static readonly SourceAdmissionHandler promoteSourceIndependentHandler = new(
        CampaignHostAcceptedStep.SourceIndependentKind,
        "Independent force promotion is unavailable.",
        session => CampaignExercises.CanPromoteSourceIndependent(session),
        session => CampaignExercises.PromoteSourceIndependent(session),
        (session, accepted) => CampaignHostAdapter.SourceIndependentCommand(session, accepted));
    private static readonly SourceAdmissionHandler promoteSourceExpandedIndependentHandler = new(
        CampaignHostAcceptedStep.SourceExpandedIndependentKind,
        "Expanded independent force promotion is unavailable.",
        session => CampaignExercises.CanPromoteSourceExpandedIndependent(session),
        session => CampaignExercises.PromoteSourceExpandedIndependent(session),
        (session, accepted) => CampaignHostAdapter.SourceExpandedIndependentCommand(session, accepted));
    private static readonly SourceAdmissionHandler promoteSourceAxisFreeWorldHandler = new(
        CampaignHostAcceptedStep.SourceAxisFreeWorldKind,
        "Axis free-world promotion is unavailable.",
        session => CampaignExercises.CanPromoteSourceAxisFreeWorld(session),
        session => CampaignExercises.PromoteSourceAxisFreeWorld(session),
        (session, accepted) => CampaignHostAdapter.SourceAxisFreeWorldCommand(session, accepted));
    private static readonly SourceAdmissionHandler promoteSourceAxisCorpsWorldHandler = new(
        CampaignHostAcceptedStep.SourceAxisCorpsWorldKind,
        "Axis corps-world promotion is unavailable.",
        session => CampaignExercises.CanPromoteSourceAxisCorpsWorld(session),
        session => CampaignExercises.PromoteSourceAxisCorpsWorld(session),
        (session, accepted) => CampaignHostAdapter.SourceAxisCorpsWorldCommand(session, accepted));
    private static readonly SourceAdmissionHandler promoteSourceCertifiedMalettiWorldHandler = new(
        CampaignHostAcceptedStep.SourceCertifiedMalettiWorldKind,
        "certified Maletti world promotion is unavailable.",
        session => CampaignExercises.CanPromoteSourceCertifiedMalettiWorld(session),
        session => CampaignExercises.PromoteSourceCertifiedMalettiWorld(session),
        (session, accepted) => CampaignHostAdapter.SourceCertifiedMalettiWorldCommand(session, accepted));
    private static readonly SourceAdmissionHandler promoteSourceMatruhDetachedWorldHandler = new(
        CampaignHostAcceptedStep.SourceMatruhDetachedWorldKind,
        "Matruh detached world promotion is unavailable.",
        session => CampaignExercises.CanPromoteSourceMatruhDetachedWorld(session),
        session => CampaignExercises.PromoteSourceMatruhDetachedWorld(session),
        (session, accepted) => CampaignHostAdapter.SourceMatruhDetachedWorldCommand(session, accepted));
    private static readonly SourceAdmissionHandler promoteSourceCwPriorityWorldHandler = new(
        CampaignHostAcceptedStep.SourceCwPriorityWorldKind,
        "Commonwealth priority world promotion is unavailable.",
        session => CampaignExercises.CanPromoteSourceCwPriorityWorld(session),
        session => CampaignExercises.PromoteSourceCwPriorityWorld(session),
        (session, accepted) => CampaignHostAdapter.SourceCwPriorityWorldCommand(session, accepted));
    private static readonly SourceAdmissionHandler promoteSourceCwFreeGunsWorldHandler = new(
        CampaignHostAcceptedStep.SourceCwFreeGunsWorldKind,
        "Commonwealth priority world promotion is unavailable.",
        session => CampaignExercises.CanPromoteSourceCwFreeGunsWorld(session),
        session => CampaignExercises.PromoteSourceCwFreeGunsWorld(session),
        (session, accepted) => CampaignHostAdapter.SourceCwFreeGunsWorldCommand(session, accepted));
    private static readonly SourceAdmissionHandler admitSourceLocalMapHandler = new(
        CampaignHostAcceptedStep.SourceLocalMapKind,
        "Source map is unavailable at this position.",
        session => !CampaignExercises.IsSourceLocalMapAdmitted(session),
        session => CampaignExercises.AdmitSourceLocalMap(session),
        (session, accepted) => CampaignHostAdapter.SourceLocalMapCommand(session, accepted));
}
