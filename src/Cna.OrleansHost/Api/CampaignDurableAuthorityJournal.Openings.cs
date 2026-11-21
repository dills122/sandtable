using System.Security.Cryptography;
using System.Text;
using Cna.Core.Hosting;
using Microsoft.Data.Sqlite;

namespace Cna.OrleansHost.Api;

internal sealed partial class CampaignDurableAuthorityJournal
{
    private const string InitiativeOrderOpeningKey = "land.initiative-order";
    private const string ReserveDesignationOpeningKey = "land.reserve-designation";
    private const string CycleControlOpeningKey = "land.cycle-control";
    private const string TruckConvoyPassOpeningKey = "land.truck-convoy-pass";
    private const string PostCombatPassOpeningKey = "land.post-combat-pass";
    private const string SecondReserveOpeningKey = "land.second-reserve";
    private const string SecondMovementCompletionOpeningKey = "land.second-movement-completion";
    private const string RepeatMovementOpeningKey = "land.repeat-movement";
    private const string PregameAxisSupplyOpeningKey = "land.pregame-axis-supply";
    private const string PregameAxisSupplyFinishOpeningKey = "land.pregame-axis-supply-finish";
    private const string SourceAxisDumpOpeningKey = "land.graziani-axis-dumps";
    private const string SourceCommonwealthSupplyOpeningKey = "land.graziani-commonwealth-supply";
    private const string SourceDummyOpeningKey = "land.graziani-dummy-groups";
    private const string SourceMotorizationOpeningKey = "land.graziani-motorization";
    private const string SourceUnattachedCargoOpeningKey = "land.graziani-unattached-cargo";
    private const string SourceActionsOpeningKey = "land.graziani-source-actions";
    private const string SourceAxisGt1SupplyOpeningKey = "land.graziani-axis-gt1-supply";
    private const string SourceCommonwealthSupplyPlanOpeningKey = "land.graziani-commonwealth-supply-plan";
    private const string SourceAutoblindaOpeningKey = "land.graziani-axis-autoblinda";
    private const string SourcePriorityMotorizationOpeningKey = "land.graziani-commonwealth-priority-motorization";
    private const string SourceAttachedInitialLoadsOpeningKey = "land.graziani-commonwealth-attached-initial-loads";
    private const string SourceInitialWrecksOpeningKey = "land.graziani-commonwealth-initial-wrecks";
    private const string SourceAxisFreeSetupOpeningKey = "land.graziani-axis-free-setup";
    private const string SourceAxisCorpsSetupOpeningKey = "land.graziani-axis-corps-setup";
    private const string SourceMalettiHierarchyOpeningKey = "land.graziani-axis-maletti-hierarchy";
    private const string SourceGt2CorrectionOpeningKey = "land.graziani-axis-gt2-correction";
    private const string SourceCwPriorityCatalogOpeningKey = "land.graziani-commonwealth-priority-catalog";
    private const string SourceMatruhFormationOpeningKey = "land.graziani-commonwealth-matruh-formation";
    private const string SourceCwFreeGunsOpeningKey = "land.graziani-commonwealth-free-guns";
    private const string SourceAxisPriorityOpeningKey = "land.graziani-axis-priority-motorization";
    private const string SourceCairoSetupOpeningKey = "land.graziani-commonwealth-cairo-setup";
    /// <summary>
    /// Project one normal owner opening from the committed Core head. Visible bytes and public
    /// references remain stable when only hidden authority changes; the private map is replaced
    /// transactionally with the current Core action IDs and authority fence.
    /// </summary>
    internal CampaignOwnerOpening QueryOwnerOpening(CampaignSeatCredential credential,
        string actingElementId) => QueryOwnerOpeningCore(credential, actingElementId,
            CampaignOwnerOpeningKind.Movement);

    /// <summary>
    /// The preamble's "who acts first" choice (land.phase.initiative-declaration): offered
    /// only to the side that won initiative. Release-blocking gap found during the
    /// Movement/ZOC/Reaction/Breakdown adversarial audit -- previously reachable only via
    /// the raw, non-HTTP CampaignDurableAuthorityJournal.Append/legal-action-set path used
    /// by tests and the Exercise Harness, never by a real player.
    /// </summary>
    internal CampaignOwnerOpening QueryInitiativeOrderOwnerOpening(CampaignSeatCredential credential) =>
        QueryOwnerOpeningCore(credential, InitiativeOrderOpeningKey, CampaignOwnerOpeningKind.InitiativeOrder);

    /// <summary>
    /// The first acting side's own Reserve Designation (land.phase.reserve-designation,
    /// Operation Stage 1): the same gap class as InitiativeOrder, found and reported during
    /// the Movement/ZOC/Reaction/Breakdown adversarial audit, confirmed by probe to be the
    /// very next preamble step after ActFirst/ActLast. Previously reachable only via the raw,
    /// non-HTTP CampaignDurableAuthorityJournal.Append/legal-action-set path, never by a real
    /// player.
    /// </summary>
    internal CampaignOwnerOpening QueryReserveDesignationOwnerOpening(CampaignSeatCredential credential) =>
        QueryOwnerOpeningCore(credential, ReserveDesignationOpeningKey, CampaignOwnerOpeningKind.ReserveDesignation);

    internal CampaignOwnerOpening QueryPregameAxisSupplyOpening(CampaignSeatCredential credential) =>
        QueryOwnerOpeningCore(credential, PregameAxisSupplyOpeningKey, CampaignOwnerOpeningKind.PregameAxisSupply);

    internal CampaignOwnerOpening QueryPregameAxisSupplyFinishOpening(CampaignSeatCredential credential) =>
        QueryOwnerOpeningCore(credential, PregameAxisSupplyFinishOpeningKey, CampaignOwnerOpeningKind.PregameAxisSupplyFinish);

    internal CampaignOwnerOpening QuerySourceAxisDumpOpening(CampaignSeatCredential credential) =>
        QueryOwnerOpeningCore(credential, SourceAxisDumpOpeningKey, CampaignOwnerOpeningKind.SourceAxisDump);

    internal CampaignOwnerOpening QuerySourceCommonwealthSupplyOpening(CampaignSeatCredential credential) =>
        QueryOwnerOpeningCore(credential, SourceCommonwealthSupplyOpeningKey, CampaignOwnerOpeningKind.SourceCommonwealthSupply);

    internal CampaignOwnerOpening QuerySourceDummyOpening(CampaignSeatCredential credential) =>
        QueryOwnerOpeningCore(credential, SourceDummyOpeningKey, CampaignOwnerOpeningKind.SourceDummy);

    internal CampaignOwnerOpening QuerySourceMotorizationOpening(CampaignSeatCredential credential) =>
        QueryOwnerOpeningCore(credential, SourceMotorizationOpeningKey, CampaignOwnerOpeningKind.SourceMotorization);

    internal CampaignOwnerOpening QuerySourceUnattachedCargoOpening(CampaignSeatCredential credential) =>
        QueryOwnerOpeningCore(credential, SourceUnattachedCargoOpeningKey, CampaignOwnerOpeningKind.SourceUnattachedCargo);

    internal CampaignOwnerOpening QuerySourceActionsOpening(CampaignSeatCredential credential) =>
        QueryOwnerOpeningCore(credential, SourceActionsOpeningKey, CampaignOwnerOpeningKind.SourceActions);

    internal CampaignOwnerOpening QuerySourceAxisGt1SupplyOpening(CampaignSeatCredential credential) =>
        QueryOwnerOpeningCore(credential, SourceAxisGt1SupplyOpeningKey, CampaignOwnerOpeningKind.SourceAxisGt1Supply);

    internal CampaignOwnerOpening QuerySourceCommonwealthSupplyPlanOpening(CampaignSeatCredential credential) =>
        QueryOwnerOpeningCore(credential, SourceCommonwealthSupplyPlanOpeningKey, CampaignOwnerOpeningKind.SourceCommonwealthSupplyPlan);

    internal CampaignOwnerOpening QuerySourceAutoblindaOpening(CampaignSeatCredential credential) =>
        QueryOwnerOpeningCore(credential, SourceAutoblindaOpeningKey, CampaignOwnerOpeningKind.SourceAutoblinda);

    internal CampaignOwnerOpening QuerySourcePriorityMotorizationOpening(CampaignSeatCredential credential) =>
        QueryOwnerOpeningCore(credential, SourcePriorityMotorizationOpeningKey, CampaignOwnerOpeningKind.SourcePriorityMotorization);

    internal CampaignOwnerOpening QuerySourceAttachedInitialLoadsOpening(CampaignSeatCredential credential) =>
        QueryOwnerOpeningCore(credential, SourceAttachedInitialLoadsOpeningKey, CampaignOwnerOpeningKind.SourceAttachedInitialLoads);

    internal CampaignOwnerOpening QuerySourceInitialWrecksOpening(CampaignSeatCredential credential) =>
        QueryOwnerOpeningCore(credential, SourceInitialWrecksOpeningKey, CampaignOwnerOpeningKind.SourceInitialWrecks);

    internal CampaignOwnerOpening QuerySourceAxisFreeSetupOpening(CampaignSeatCredential credential) =>
        QueryOwnerOpeningCore(credential, SourceAxisFreeSetupOpeningKey, CampaignOwnerOpeningKind.SourceAxisFreeSetup);

    internal CampaignOwnerOpening QuerySourceAxisCorpsSetupOpening(CampaignSeatCredential credential) =>
        QueryOwnerOpeningCore(credential, SourceAxisCorpsSetupOpeningKey, CampaignOwnerOpeningKind.SourceAxisCorpsSetup);

    internal CampaignOwnerOpening QuerySourceMalettiHierarchyOpening(CampaignSeatCredential credential) =>
        QueryOwnerOpeningCore(credential, SourceMalettiHierarchyOpeningKey, CampaignOwnerOpeningKind.SourceMalettiHierarchy);

    internal CampaignOwnerOpening QuerySourceGt2CorrectionOpening(CampaignSeatCredential credential) =>
        QueryOwnerOpeningCore(credential, SourceGt2CorrectionOpeningKey, CampaignOwnerOpeningKind.SourceGt2Correction);

    internal CampaignOwnerOpening QuerySourceCwPriorityCatalogOpening(CampaignSeatCredential credential) =>
        QueryOwnerOpeningCore(credential, SourceCwPriorityCatalogOpeningKey, CampaignOwnerOpeningKind.SourceCwPriorityCatalog);

    internal CampaignOwnerOpening QuerySourceMatruhFormationOpening(CampaignSeatCredential credential) =>
        QueryOwnerOpeningCore(credential, SourceMatruhFormationOpeningKey, CampaignOwnerOpeningKind.SourceMatruhFormation);

    internal CampaignOwnerOpening QuerySourceCwFreeGunsOpening(CampaignSeatCredential credential) =>
        QueryOwnerOpeningCore(credential, SourceCwFreeGunsOpeningKey, CampaignOwnerOpeningKind.SourceCwFreeGuns);

    internal CampaignOwnerOpening QuerySourceAxisPriorityOpening(CampaignSeatCredential credential) =>
        QueryOwnerOpeningCore(credential, SourceAxisPriorityOpeningKey, CampaignOwnerOpeningKind.SourceAxisPriority);

    internal CampaignOwnerOpening QuerySourceCairoSetupOpening(CampaignSeatCredential credential) =>
        QueryOwnerOpeningCore(credential, SourceCairoSetupOpeningKey, CampaignOwnerOpeningKind.SourceCairoSetup);

    internal CampaignOwnerOpening QueryCombatOwnerOpening(CampaignSeatCredential credential,
        string actingElementId) => QueryOwnerOpeningCore(credential, actingElementId,
            CampaignOwnerOpeningKind.CombatSelection);

    internal CampaignOwnerOpening QueryRbaOwnerOpening(CampaignSeatCredential credential,
        string defenderElementId) => QueryOwnerOpeningCore(credential, defenderElementId,
            CampaignOwnerOpeningKind.RetreatBeforeAssault);

    internal CampaignOwnerOpening QueryForceOwnerOpening(CampaignSeatCredential credential,
        string elementId) => QueryOwnerOpeningCore(credential, elementId,
            CampaignOwnerOpeningKind.ForceAssignment);

    internal CampaignOwnerOpening QueryPostAssaultRetreatOwnerOpening(
        CampaignSeatCredential credential, string defenderElementId) =>
        QueryOwnerOpeningCore(credential, defenderElementId,
            CampaignOwnerOpeningKind.PostAssaultRetreat);

    internal CampaignOwnerOpening QueryCustodyOwnerOpening(
        CampaignSeatCredential credential, string captorElementId) =>
        QueryOwnerOpeningCore(credential, captorElementId,
            CampaignOwnerOpeningKind.Custody);

    internal CampaignOwnerOpening QueryReserveReleaseOwnerOpening(
        CampaignSeatCredential credential, string reserveElementId) =>
        QueryOwnerOpeningCore(credential, reserveElementId,
            CampaignOwnerOpeningKind.ReserveRelease);

    internal CampaignOwnerOpening QueryCycleControlOwnerOpening(
        CampaignSeatCredential credential) =>
        QueryOwnerOpeningCore(credential, CycleControlOpeningKey,
            CampaignOwnerOpeningKind.CycleControl);

    internal CampaignOwnerOpening QueryTruckConvoyPassOwnerOpening(CampaignSeatCredential credential) =>
        QueryOwnerOpeningCore(credential, TruckConvoyPassOpeningKey, CampaignOwnerOpeningKind.TruckConvoyPass);

    internal CampaignOwnerOpening QueryPostCombatPassOwnerOpening(CampaignSeatCredential credential) =>
        QueryOwnerOpeningCore(credential, PostCombatPassOpeningKey, CampaignOwnerOpeningKind.PostCombatPass);

    internal CampaignOwnerOpening QuerySecondReserveOwnerOpening(CampaignSeatCredential credential) =>
        QueryOwnerOpeningCore(credential, SecondReserveOpeningKey, CampaignOwnerOpeningKind.SecondReserve);

    internal CampaignOwnerOpening QuerySecondMovementOwnerOpening(CampaignSeatCredential credential, string elementId) =>
        QueryOwnerOpeningCore(credential, elementId, CampaignOwnerOpeningKind.SecondMovement);

    internal CampaignOwnerOpening QuerySecondMovementCompletionOwnerOpening(CampaignSeatCredential credential) =>
        QueryOwnerOpeningCore(credential, SecondMovementCompletionOpeningKey, CampaignOwnerOpeningKind.SecondMovementCompletion);

    internal CampaignOwnerOpening QueryRepeatMovementOwnerOpening(
        CampaignSeatCredential credential) =>
        QueryOwnerOpeningCore(credential, RepeatMovementOpeningKey,
            CampaignOwnerOpeningKind.RepeatMovement);

    internal CampaignOwnerOpening QueryRepeatCombatOwnerOpening(
        CampaignSeatCredential credential, string actingElementId) =>
        QueryOwnerOpeningCore(credential, actingElementId,
            CampaignOwnerOpeningKind.RepeatCombatSelection);

    private CampaignOwnerOpening QueryOwnerOpeningCore(CampaignSeatCredential credential,
        string actingElementId, CampaignOwnerOpeningKind kind)
    {
        RequireWritable();
        ArgumentException.ThrowIfNullOrWhiteSpace(actingElementId);
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        var seat = AuthenticateSeat(connection, transaction, credential, out var verifiedHead)
            ?? throw new UnauthorizedAccessException("Seat credential rejected.");
        var authority = verifiedHead ?? Load(connection, transaction, seat.CampaignId);
        if (kind == CampaignOwnerOpeningKind.ForceAssignment && authority.Timing is null)
            throw new InvalidOperationException("Force Assignment needs a pinned campaign timing configuration.");
        byte[] visible;
        long authorityVersion;
        string[] actionIds;
        try
        {
            if (kind == CampaignOwnerOpeningKind.PregameAxisSupply)
            {
                var draft = CampaignHostAdapter.QueryPregameAxisSupplyOwnerDraft(authority.Session, seat.Audience);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.PregameAxisSupplyFinish)
            {
                var draft = CampaignHostAdapter.QueryPregameAxisSupplyFinishDraft(authority.Session, seat.Audience);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceAxisDump)
            {
                var draft = CampaignHostAdapter.QuerySourceAxisDumpDraft(authority.Session, seat.Audience);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceCommonwealthSupply)
            {
                var draft = CampaignHostAdapter.QuerySourceCommonwealthSupplyDraft(authority.Session, seat.Audience);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceDummy)
            {
                var draft = CampaignHostAdapter.QuerySourceDummyDraft(authority.Session, seat.Audience);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceMotorization)
            {
                var draft = CampaignHostAdapter.QuerySourceMotorizationDraft(authority.Session, seat.Audience);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceUnattachedCargo)
            {
                var draft = CampaignHostAdapter.QuerySourceUnattachedCargoDraft(authority.Session, seat.Audience);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceActions)
            {
                var draft = CampaignHostAdapter.QuerySourceActionsDraft(authority.Session, seat.Audience);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceAxisGt1Supply)
            {
                var draft = CampaignHostAdapter.QuerySourceAxisGt1SupplyDraft(authority.Session, seat.Audience);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceCommonwealthSupplyPlan)
            {
                var draft = CampaignHostAdapter.QuerySourceCommonwealthSupplyPlanDraft(authority.Session, seat.Audience);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceAutoblinda)
            {
                var draft = CampaignHostAdapter.QuerySourceAutoblindaDraft(authority.Session, seat.Audience);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourcePriorityMotorization)
            {
                var draft = CampaignHostAdapter.QuerySourcePriorityMotorizationDraft(authority.Session, seat.Audience);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceAttachedInitialLoads)
            {
                var draft = CampaignHostAdapter.QuerySourceAttachedInitialLoadsDraft(authority.Session, seat.Audience);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceInitialWrecks)
            {
                var draft = CampaignHostAdapter.QuerySourceInitialWrecksDraft(authority.Session, seat.Audience);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceAxisFreeSetup)
            {
                var draft = CampaignHostAdapter.QuerySourceAxisFreeSetupDraft(authority.Session, seat.Audience);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceAxisCorpsSetup)
            {
                var draft = CampaignHostAdapter.QuerySourceAxisCorpsSetupDraft(authority.Session, seat.Audience);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceMalettiHierarchy)
            {
                var draft = CampaignHostAdapter.QuerySourceMalettiHierarchyDraft(authority.Session, seat.Audience);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceGt2Correction)
            {
                var draft = CampaignHostAdapter.QuerySourceGt2CorrectionDraft(authority.Session, seat.Audience);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceCwPriorityCatalog)
            {
                var draft = CampaignHostAdapter.QuerySourceCwPriorityCatalogDraft(authority.Session, seat.Audience);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceMatruhFormation)
            {
                var draft = CampaignHostAdapter.QuerySourceMatruhFormationDraft(authority.Session, seat.Audience);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceCwFreeGuns)
            {
                var draft = CampaignHostAdapter.QuerySourceCwFreeGunsDraft(authority.Session, seat.Audience);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceAxisPriority)
            {
                var draft = CampaignHostAdapter.QuerySourceAxisPriorityDraft(authority.Session, seat.Audience);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SourceCairoSetup)
            {
                var draft = CampaignHostAdapter.QuerySourceCairoSetupDraft(authority.Session, seat.Audience);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.CombatSelection)
            {
                var draft = CampaignHostAdapter.QueryContactCombatOwnerDraft(authority.Session,
                    seat.Audience, actingElementId);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateCombatActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.ForceAssignment)
            {
                var draft = CampaignHostAdapter.QueryContactForceOwnerDraft(authority.Session,
                    seat.Audience, actingElementId);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.RetreatBeforeAssault)
            {
                var draft = CampaignHostAdapter.QueryContactRbaDeclineOwnerDraft(authority.Session,
                    seat.Audience, actingElementId);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.PostAssaultRetreat)
            {
                var draft = CampaignHostAdapter.QueryContactPostAssaultRetreatOwnerDraft(
                    authority.Session, seat.Audience, actingElementId);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.Custody)
            {
                var draft = CampaignHostAdapter.QueryContactCustodyOwnerDraft(
                    authority.Session, seat.Audience, actingElementId);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.ReserveRelease)
            {
                var draft = CampaignHostAdapter.QueryNativeReserveReleaseOwnerDraft(
                    authority.Session, seat.Audience, actingElementId);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.CycleControl)
            {
                var draft = CampaignHostAdapter.QueryNativeCycleControlOwnerDraft(
                    authority.Session, seat.Audience);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.TruckConvoyPass)
            {
                var draft = CampaignHostAdapter.QueryTruckConvoyPassOwnerDraft(authority.Session, seat.Audience);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.InitiativeOrder)
            {
                var draft = CampaignHostAdapter.QueryInitiativeOrderOwnerDraft(authority.Session, seat.Audience);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.ReserveDesignation)
            {
                var draft = CampaignHostAdapter.QueryReserveDesignationOwnerDraft(authority.Session, seat.Audience);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.PostCombatPass)
            {
                var draft = CampaignHostAdapter.QueryPostCombatPassOwnerDraft(authority.Session, seat.Audience);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SecondReserve)
            {
                var draft = CampaignHostAdapter.QuerySecondReserveOwnerDraft(authority.Session, seat.Audience);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SecondMovement)
            {
                var draft = CampaignHostAdapter.QuerySecondMovementOwnerDraft(authority.Session, seat.Audience, actingElementId);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.SecondMovementCompletion)
            {
                var draft = CampaignHostAdapter.QuerySecondMovementCompletionOwnerDraft(authority.Session, seat.Audience);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.RepeatMovement)
            {
                var draft = CampaignHostAdapter.QueryNativeRepeatMovementOwnerDraft(
                    authority.Session, seat.Audience);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateActionIds.ToArray();
            }
            else if (kind == CampaignOwnerOpeningKind.RepeatCombatSelection)
            {
                var draft = CampaignHostAdapter.QueryNativeRepeatCombatOwnerDraft(
                    authority.Session, seat.Audience, actingElementId);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateCombatActionIds.ToArray();
            }
            else
            {
                var draft = CampaignHostAdapter.QueryContactOwnerDraft(authority.Session,
                    seat.Audience, actingElementId);
                visible = draft.VisibleBytes;
                authorityVersion = draft.AuthorityVersion;
                actionIds = draft.PrivateMovementActions.OrderBy(value => value.OriginLocationId,
                    StringComparer.Ordinal).ThenBy(value => value.DestinationLocationId,
                    StringComparer.Ordinal).Select(value => value.ActionId).ToArray();
            }
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            throw new InvalidOperationException("No owner opening is available at this cut.");
        }
        if (kind == CampaignOwnerOpeningKind.RetreatBeforeAssault)
        {
            var round = ReadActiveRbaRound(connection, transaction, authority, seat.CampaignId);
            var gate = GateRbaRound(connection, transaction, round);
            if (gate.CancellationReason is { } reason)
            {
                CancelRbaRound(connection, transaction, authority, gate.Round, reason, gate.CancelledAt);
                transaction.Commit();
                throw new InvalidOperationException("RBA round is closed.");
            }
        }
        if (kind == CampaignOwnerOpeningKind.ForceAssignment)
        {
            var round = ReadActiveForceRound(connection, transaction, authority, seat.CampaignId);
            var gate = GateForceRound(connection, transaction, round);
            if (gate.CancellationReason is { } reason)
            {
                CancelForceRound(connection, transaction, authority, gate.Round,
                    reason, gate.CancelledAt);
                transaction.Commit();
                throw new InvalidOperationException("Force Assignment round is closed.");
            }
        }
        if (kind == CampaignOwnerOpeningKind.CycleControl)
        {
            var round = ReadActiveCycleRound(connection, transaction, authority, seat.CampaignId);
            var gate = GateCycleRound(connection, transaction, round);
            if (gate.CancellationReason is { } reason)
            {
                TimeoutCycleRound(connection, transaction, authority, gate.Round, reason, gate.CancelledAt);
                transaction.Commit();
                throw new InvalidOperationException("Cycle control round is closed.");
            }
        }
        if (kind == CampaignOwnerOpeningKind.CombatSelection
            || kind == CampaignOwnerOpeningKind.RepeatCombatSelection)
        {
            var round = ReadActiveSelectionRound(connection, transaction, authority, seat.CampaignId);
            var gate = GateSelectionRound(connection, transaction, round);
            if (gate.CancellationReason is { } reason)
            {
                TimeoutSelectionRound(connection, transaction, authority, gate.Round, reason, gate.CancelledAt);
                transaction.Commit();
                throw new InvalidOperationException("Selection round is closed.");
            }
        }
        if (kind == CampaignOwnerOpeningKind.PostAssaultRetreat)
        {
            var round = ReadActiveRetreatRound(connection, transaction, authority, seat.CampaignId);
            var gate = GateRetreatRound(connection, transaction, round);
            if (gate.CancellationReason is { } reason)
            {
                TimeoutRetreatRound(connection, transaction, authority, gate.Round, reason, gate.CancelledAt);
                transaction.Commit();
                throw new InvalidOperationException("Retreat round is closed.");
            }
        }
        if (kind == CampaignOwnerOpeningKind.Custody)
        {
            var round = ReadActiveCustodyRound(connection, transaction, authority, seat.CampaignId);
            var gate = GateCustodyRound(connection, transaction, round);
            if (gate.CancellationReason is { } reason)
            {
                TimeoutCustodyRound(connection, transaction, authority, gate.Round, reason, gate.CancelledAt);
                transaction.Commit();
                throw new InvalidOperationException("Custody round is closed.");
            }
        }
        if (kind == CampaignOwnerOpeningKind.ReserveRelease)
        {
            var round = ReadActiveReserveRound(connection, transaction, authority, seat.CampaignId);
            var gate = GateReserveRound(connection, transaction, round);
            if (gate.CancellationReason is { } reason)
            {
                TimeoutReserveRound(connection, transaction, authority, gate.Round, reason, gate.CancelledAt);
                transaction.Commit();
                throw new InvalidOperationException("Reserve Release round is closed.");
            }
        }
        RequireBounded(visible);
        var digest = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(visible));
        var side = Side(seat.Audience);
        long revision = 1;
        using (var prior = connection.CreateCommand())
        {
            prior.Transaction = transaction;
            prior.CommandText = """
                SELECT seat_epoch,revision,visible_digest,visible_bytes
                FROM owner_openings
                WHERE campaign_id=$id AND side=$side AND acting_element_id=$element
                """;
            BindOpeningKey(prior, seat.CampaignId, side, actingElementId);
            using var reader = prior.ExecuteReader();
            if (reader.Read())
            {
                var priorVisible = ReadBounded(reader, 3);
                var priorDigest = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(priorVisible));
                if (reader.GetString(2) != priorDigest)
                    throw new InvalidOperationException("Retained owner opening changed.");
                if (reader.GetInt64(0) == seat.Epoch && priorDigest == digest
                    && priorVisible.AsSpan().SequenceEqual(visible))
                    revision = reader.GetInt64(1);
                else
                    revision = checked(reader.GetInt64(1) + 1);
            }
        }
        var key = ReadReferenceKey(connection, transaction);
        var refs = actionIds.Select((_, index) => Reference(key, seat.CampaignId, side,
            actingElementId, seat.Epoch, revision, digest, index)).ToArray();
        using (var upsert = connection.CreateCommand())
        {
            upsert.Transaction = transaction;
            upsert.CommandText = """
                INSERT INTO owner_openings VALUES ($id,$side,$element,$epoch,$revision,
                  $digest,$visible,$authority,$count)
                ON CONFLICT(campaign_id,side,acting_element_id) DO UPDATE SET
                  seat_epoch=excluded.seat_epoch, revision=excluded.revision,
                  visible_digest=excluded.visible_digest, visible_bytes=excluded.visible_bytes,
                  authority_version=excluded.authority_version, action_count=excluded.action_count
                """;
            BindOpeningKey(upsert, seat.CampaignId, side, actingElementId);
            upsert.Parameters.AddWithValue("$epoch", seat.Epoch);
            upsert.Parameters.AddWithValue("$revision", revision);
            upsert.Parameters.AddWithValue("$digest", digest);
            upsert.Parameters.AddWithValue("$visible", visible);
            upsert.Parameters.AddWithValue("$authority", authorityVersion);
            upsert.Parameters.AddWithValue("$count", refs.Length);
            upsert.ExecuteNonQuery();
        }
        using (var delete = connection.CreateCommand())
        {
            delete.Transaction = transaction;
            delete.CommandText = """
                DELETE FROM owner_actions WHERE campaign_id=$id AND side=$side
                  AND acting_element_id=$element
                """;
            BindOpeningKey(delete, seat.CampaignId, side, actingElementId);
            delete.ExecuteNonQuery();
        }
        for (var index = 0; index < refs.Length; index++)
        {
            using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO owner_actions VALUES ($id,$side,$element,$ref,$ordinal,$action)
                """;
            BindOpeningKey(insert, seat.CampaignId, side, actingElementId);
            insert.Parameters.AddWithValue("$ref", refs[index]);
            insert.Parameters.AddWithValue("$ordinal", index);
            insert.Parameters.AddWithValue("$action", actionIds[index]);
            insert.ExecuteNonQuery();
        }
        transaction.Commit();
        return new CampaignOwnerOpening(seat.CampaignId, seat.Audience, actingElementId,
            revision, visible, refs);
    }

    private static void BindOpeningKey(SqliteCommand command, string campaignId,
        string side, string actingElementId)
    {
        command.Parameters.AddWithValue("$id", campaignId);
        command.Parameters.AddWithValue("$side", side);
        command.Parameters.AddWithValue("$element", actingElementId);
    }

    /// <summary>
    /// Reads the campaign-reference HMAC key from this campaign's own per-campaign file.
    /// Deliberately per-campaign, not host-identity state like <c>operator_capability</c> -- see
    /// <see cref="EnsureHostIdentity"/>'s remarks for why the two are not treated the same way:
    /// <see cref="Reference"/> always takes <c>campaignId</c> as an explicit input and is only ever
    /// checked within one campaign's own scope, so nothing requires this key to match across
    /// campaigns, and keeping it in the campaign's own file means it travels correctly with that
    /// file alone if it is ever copied/backed up elsewhere (a shared host-identity file would not).
    /// </summary>
    private static byte[] ReadReferenceKey(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var query = connection.CreateCommand();
        query.Transaction = transaction;
        query.CommandText = "SELECT secret FROM reference_key WHERE singleton=1";
        if (query.ExecuteScalar() is not byte[] key || key.Length != 32)
            throw new InvalidOperationException("Owner reference key is missing.");
        return key;
    }

    private static string Reference(byte[] key, string campaignId, string side,
        string actingElementId, long epoch, long revision, string visibleDigest, int ordinal)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write("campaign-owner-ref-v1");
            writer.Write(campaignId);
            writer.Write(side);
            writer.Write(actingElementId);
            writer.Write(epoch);
            writer.Write(revision);
            writer.Write(visibleDigest);
            writer.Write(ordinal);
        }
        return "act." + Convert.ToHexStringLower(HMACSHA256.HashData(key, stream.ToArray()));
    }
}

internal enum CampaignOwnerOpeningKind
{
    Movement,
    InitiativeOrder,
    ReserveDesignation,
    CombatSelection,
    RetreatBeforeAssault,
    ForceAssignment,
    PostAssaultRetreat,
    Custody,
    ReserveRelease,
    CycleControl,
    TruckConvoyPass,
    PostCombatPass,
    SecondReserve,
    SecondMovement,
    SecondMovementCompletion,
    RepeatMovement,
    RepeatCombatSelection,
    PregameAxisSupply,
    PregameAxisSupplyFinish,
    SourceAxisDump,
    SourceCommonwealthSupply,
    SourceDummy,
    SourceMotorization,
    SourceUnattachedCargo,
    SourceActions,
    SourceAxisGt1Supply,
    SourceCommonwealthSupplyPlan,
    SourceAutoblinda,
    SourcePriorityMotorization,
    SourceAttachedInitialLoads,
    SourceInitialWrecks,
    SourceAxisFreeSetup,
    SourceAxisCorpsSetup,
    SourceMalettiHierarchy,
    SourceGt2Correction,
    SourceCwPriorityCatalog,
    SourceMatruhFormation,
    SourceCwFreeGuns,
    SourceAxisPriority,
    SourceCairoSetup,
}

internal sealed class CampaignOwnerOpening
{
    private readonly byte[] visibleBytes;
    private readonly string[] publicActionRefs;

    internal CampaignOwnerOpening(string campaignId, Cna.Core.Actions.CampaignActionAudience audience,
        string actingElementId, long revision, byte[] visibleBytes,
        IReadOnlyList<string> publicActionRefs)
    {
        CampaignId = campaignId;
        Audience = audience;
        ActingElementId = actingElementId;
        Revision = revision;
        this.visibleBytes = visibleBytes.ToArray();
        this.publicActionRefs = publicActionRefs.ToArray();
    }

    public string CampaignId { get; }
    public Cna.Core.Actions.CampaignActionAudience Audience { get; }
    public string ActingElementId { get; }
    public long Revision { get; }
    public byte[] VisibleBytes => visibleBytes.ToArray();
    public IReadOnlyList<string> PublicActionRefs => Array.AsReadOnly(publicActionRefs.ToArray());
}
