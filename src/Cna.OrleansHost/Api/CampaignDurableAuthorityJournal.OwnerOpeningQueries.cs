using System.Collections.Frozen;
using Cna.Core.Actions;
using Cna.Core.Exercises;
using Cna.Core.Hosting;

namespace Cna.OrleansHost.Api;

/// <summary>
/// One owner-opening kind's query result: the bytes visible to the acting side, the Core
/// authority version they were produced at, and the private action ids they bind to. Lets
/// QueryOwnerOpeningCore and SubmitOwnerActionCore's revalidation step (which re-derives the
/// identical draft to confirm nothing changed since the opening was queried) share one
/// dispatch table instead of each hand-maintaining its own copy of the same ~41-branch
/// kind-to-method mapping -- see docs/work/plans/2026-11-23-codebase-overhaul-direction.md
/// Section 1. The round-gating and post-acceptance combat-chain continuation logic that
/// surrounds these two call sites is deliberately NOT part of this registry: those remain
/// genuinely per-kind special-cased control flow (#394-adjacent, determinism-sensitive),
/// not a mechanical "call method X" mapping, so folding them in here would be forcing an
/// interface onto logic that doesn't actually fit it.
/// </summary>
internal sealed record OwnerOpeningDraft(byte[] VisibleBytes, long AuthorityVersion, string[] ActionIds);

internal sealed partial class CampaignDurableAuthorityJournal
{
    private static readonly FrozenDictionary<CampaignOwnerOpeningKind, Func<ExerciseSession, CampaignActionAudience, string, OwnerOpeningDraft>> openingQueries =
        new Dictionary<CampaignOwnerOpeningKind, Func<ExerciseSession, CampaignActionAudience, string, OwnerOpeningDraft>>
        {
            [CampaignOwnerOpeningKind.PregameAxisSupply] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QueryPregameAxisSupplyOwnerDraft(session, audience);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.PregameAxisSupplyFinish] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QueryPregameAxisSupplyFinishDraft(session, audience);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.SourceAxisDump] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QuerySourceAxisDumpDraft(session, audience);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.SourceCommonwealthSupply] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QuerySourceCommonwealthSupplyDraft(session, audience);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.SourceDummy] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QuerySourceDummyDraft(session, audience);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.SourceMotorization] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QuerySourceMotorizationDraft(session, audience);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.SourceUnattachedCargo] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QuerySourceUnattachedCargoDraft(session, audience);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.SourceActions] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QuerySourceActionsDraft(session, audience);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.SourceAxisGt1Supply] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QuerySourceAxisGt1SupplyDraft(session, audience);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.SourceCommonwealthSupplyPlan] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QuerySourceCommonwealthSupplyPlanDraft(session, audience);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.SourceAutoblinda] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QuerySourceAutoblindaDraft(session, audience);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.SourcePriorityMotorization] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QuerySourcePriorityMotorizationDraft(session, audience);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.SourceAttachedInitialLoads] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QuerySourceAttachedInitialLoadsDraft(session, audience);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.SourceInitialWrecks] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QuerySourceInitialWrecksDraft(session, audience);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.SourceAxisFreeSetup] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QuerySourceAxisFreeSetupDraft(session, audience);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.SourceAxisCorpsSetup] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QuerySourceAxisCorpsSetupDraft(session, audience);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.SourceMalettiHierarchy] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QuerySourceMalettiHierarchyDraft(session, audience);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.SourceGt2Correction] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QuerySourceGt2CorrectionDraft(session, audience);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.SourceCwPriorityCatalog] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QuerySourceCwPriorityCatalogDraft(session, audience);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.SourceMatruhFormation] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QuerySourceMatruhFormationDraft(session, audience);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.SourceCwFreeGuns] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QuerySourceCwFreeGunsDraft(session, audience);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.SourceAxisPriority] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QuerySourceAxisPriorityDraft(session, audience);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.SourceCairoSetup] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QuerySourceCairoSetupDraft(session, audience);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.CombatSelection] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QueryContactCombatOwnerDraft(session, audience, actingElementId);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateCombatActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.ForceAssignment] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QueryContactForceOwnerDraft(session, audience, actingElementId);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.RetreatBeforeAssault] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QueryContactRbaDeclineOwnerDraft(session, audience, actingElementId);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.PostAssaultRetreat] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QueryContactPostAssaultRetreatOwnerDraft(session, audience, actingElementId);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.Custody] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QueryContactCustodyOwnerDraft(session, audience, actingElementId);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.ReserveRelease] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QueryNativeReserveReleaseOwnerDraft(session, audience, actingElementId);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.CycleControl] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QueryNativeCycleControlOwnerDraft(session, audience);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.TruckConvoyPass] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QueryTruckConvoyPassOwnerDraft(session, audience);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.InitiativeOrder] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QueryInitiativeOrderOwnerDraft(session, audience);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.ReserveDesignation] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QueryReserveDesignationOwnerDraft(session, audience);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.Reaction] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QueryReactionOwnerDraft(session, audience);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.MovementSegmentCompletion] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QueryMovementSegmentCompletionOwnerDraft(session, audience);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.PostCombatPass] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QueryPostCombatPassOwnerDraft(session, audience);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.SecondReserve] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QuerySecondReserveOwnerDraft(session, audience);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.StageEntryInitiativeDeclaration] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QueryStageEntryInitiativeDeclarationOwnerDraft(session, audience);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.SecondMovement] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QuerySecondMovementOwnerDraft(session, audience, actingElementId);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.SecondMovementCompletion] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QuerySecondMovementCompletionOwnerDraft(session, audience);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.RepeatMovement] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QueryNativeRepeatMovementOwnerDraft(session, audience);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.RepeatCombatSelection] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QueryNativeRepeatCombatOwnerDraft(session, audience, actingElementId);
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, draft.PrivateCombatActionIds.ToArray());
            },
            [CampaignOwnerOpeningKind.Movement] = (session, audience, actingElementId) =>
            {
                var draft = CampaignHostAdapter.QueryContactOwnerDraft(session, audience, actingElementId);
                var actionIds = draft.PrivateMovementActions.OrderBy(value => value.OriginLocationId,
                    StringComparer.Ordinal).ThenBy(value => value.DestinationLocationId,
                    StringComparer.Ordinal).Select(value => value.ActionId).ToArray();
                return new OwnerOpeningDraft(draft.VisibleBytes, draft.AuthorityVersion, actionIds);
            },
        }.ToFrozenDictionary();
}
