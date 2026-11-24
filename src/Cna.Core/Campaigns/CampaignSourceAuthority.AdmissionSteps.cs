using System.Text;

namespace Cna.Core.Campaigns;

// Slice 3a of the maintainability refactor direction (see
// docs/work/plans/2026-11-24-maintainability-refactor-direction.md, Section 2.2):
// an ordered hash-chain registry for the ProfileHash/AdmitXxx pairs in
// CampaignSourceAuthority.cs. This file is purely additive -- every existing
// AdmitXxx/XxxProfileHash member in CampaignSourceAuthority.cs is untouched;
// each step below drives the SAME hand-written method (via Accept) and
// reproduces the SAME hash formula (via ProfileHash), so there is no behavior
// change and nothing new that could drift out of sync. Slice 3b (a separate,
// later PR, gated on this one proving green) deletes the old members once the
// registry is proven byte-identical by a real test run, not a static
// comparison -- see CampaignSourceAuthorityAdmissionStepsTests for that proof,
// and this project's own reverted codec-fragment attempt (direction doc,
// Section 6's "Section 3" callout) for why a static comparison alone is not
// sufficient evidence.
//
// Not every ProfileHash chain entry is included here: CampaignSourceAuthorityState's
// UnattachedCargo mechanic (AxisUnattachedCargo/CommonwealthUnattachedCargo,
// admitted via the dual-sided, parameterized
// ApplyUnattachedCargo(bool axis, string kind, string? location, int delta))
// does not fit the single CanAdmit/Accept shape below -- it is two independent
// per-side mechanics sharing one ProfileHash, not a single admission step. Per
// the direction doc's own precedent for not force-fitting outliers (Section
// 2.3's Libyan/Matruh groups), it stays hand-written and is left out of this
// chain.
internal sealed partial record CampaignSourceAuthorityState
{
    // One step in the ordered hash chain. PriorProfileHash is a delegate, not
    // "the previous entry in this array", because the chain is a tree, not a
    // line: Gt2Correction and CwPriorityCatalog branch directly off
    // AuthoritySetupHash, and MalettiWorld/IndependentWorld both branch off
    // CairoWorldProfileHash, rather than each reading the immediately
    // preceding step. PolicyId is nullable because a couple of hand-written
    // properties (e.g. CommonwealthSupplementalProfileHash, AxisGarrisonsProfileHash)
    // omit the middle PolicyId segment entirely -- see ProfileHash() below.
    internal sealed record SourceAdmissionStep(
        string Kind,
        int ContractVersion,
        string? PolicyId,
        Func<CampaignSourceAuthorityState, string> PriorProfileHash,
        Func<CampaignSourceAuthorityState, string> ContentHash,
        Func<CampaignSourceAuthorityState, bool> CanAdmit,
        Func<CampaignSourceAuthorityState, CampaignSourceAuthorityState> Accept,
        string UnavailableMessage);

    // Replaces all hand-written "Digest(prior + "\0" + PolicyId + "\0" +
    // content)" XxxProfileHash property bodies with one shared formula. When a
    // step has no separate PolicyId segment (PolicyId is null), this reproduces
    // the 2-part "Digest(prior + "\0" + content)" shape those properties use
    // instead -- same discipline PR #525's AdmitCore/ProfileHash sharing used
    // for CampaignDurableAuthorityJournal.
    internal static string ProfileHash(SourceAdmissionStep step, CampaignSourceAuthorityState state) =>
        CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(
            step.PriorProfileHash(state) + "\0"
            + (step.PolicyId is null ? "" : step.PolicyId + "\0")
            + step.ContentHash(state)));

    // Replaces the hand-written AdmitXxx bodies' "if (!CanAdmitXxx) throw ...;
    // return ...;" shape with one shared driver. Accept still calls the
    // existing, unmodified AdmitXxx/PromoteXxx method for its step -- this is
    // purely a shared dispatch wrapper, not a reimplementation of any
    // mechanic's logic, so there is nothing new that could diverge from it.
    internal static CampaignSourceAuthorityState AdmitCore(CampaignSourceAuthorityState state, SourceAdmissionStep step)
    {
        if (!step.CanAdmit(state)) throw new InvalidOperationException(step.UnavailableMessage);
        return step.Accept(state);
    }

    // The ordered chain itself, in the same order the hand-written
    // XxxProfileHash properties appear in CampaignSourceAuthority.cs.
    internal static readonly SourceAdmissionStep[] AdmissionChain =
    [
        new("Supplemental", SupplementalContractVersion, null,
            state => state.AuthoritySetupHash,
            state => state.AxisSupplement!.Hash() + "\0" + CampaignSourceAutoblindaSetup.PolicyId + "\0"
                + CampaignSourceAutoblindaSetup.Create(state.AxisSupplement!).Hash(),
            state => state.AxisSupplement is null,
            state => state.AdmitAxisSupplementCore(),
            "The Axis source supplement is already admitted."),
        new("CommonwealthSupplemental", CommonwealthSupplementalContractVersion, null,
            state => state.SupplementalProfileHash,
            state => state.CommonwealthSupplement!.Hash(),
            state => state.AxisSupplement is not null && state.CommonwealthSupplement is null,
            state => state.AdmitCommonwealthSupplementCore(),
            "Commonwealth source supplement requires the prior profile and no duplicate."),
        new("PriorityMotorization", PriorityMotorizationContractVersion, CampaignSourcePriorityMotorization.PolicyId,
            state => state.CommonwealthSupplementalProfileHash,
            state => CampaignSourcePriorityMotorization.Create(state.CommonwealthSupplement!, state.CommonwealthMotorization).Hash(),
            state => state.CommonwealthSupplement is not null && state.PriorityMotorization is null,
            state => state.AdmitPriorityMotorizationCore(),
            "Priority motorization requires the prior source profile and no duplicate."),
        new("AxisPriorityMotorization", AxisPriorityMotorizationContractVersion, CampaignSourceAxisPriorityMotorization.PolicyId,
            state => state.PriorityMotorizationProfileHash,
            state => CampaignSourceAxisPriorityMotorization.Create(state.AxisSupplement!, state.AxisMotorization).Hash(),
            state => state.PriorityMotorization is not null && state.AxisPriorityMotorization is null && state.AxisSupplement is not null,
            state => state.AdmitAxisPriorityMotorizationCore(),
            "Axis priority setup requires the prior source profile and no duplicate."),
        new("AxisGarrisons", AxisGarrisonsContractVersion, null,
            state => state.AxisPriorityMotorizationProfileHash,
            state => state.AxisGarrisons!.Hash(),
            state => state.AxisPriorityMotorization is not null && state.AxisGarrisons is null && state.AxisSupplement is not null,
            state => state.AdmitAxisGarrisonsCore(),
            "Axis garrisons require the prior source profile and no duplicate."),
        new("Battlefield", BattlefieldContractVersion, CampaignSourceBattlefield.PolicyId,
            state => state.AxisGarrisonsProfileHash,
            state => CampaignSourceBattlefield.Create(state).Hash(),
            state => state.AxisGarrisons is not null && state.Battlefield is null,
            state => state.AdmitBattlefieldCore(),
            "Source battlefield requires the prior profile and no duplicate."),
        new("CairoSetup", CairoSetupContractVersion, CampaignSourceCairoInitialSetup.PolicyId,
            state => state.BattlefieldProfileHash,
            state => CampaignSourceCairoInitialSetup.Create(CampaignSourceBattlefield.Create(state)).Hash(),
            state => state.Battlefield is not null && state.CairoSetup is null,
            state => state.AdmitCairoSetupCore(),
            "Cairo setup requires the prior battlefield and no duplicate."),
        new("CairoWorld", CairoWorldContractVersion, CampaignSourceCairoActiveWorld.PolicyId,
            state => state.CairoSetupProfileHash,
            state => state.CairoWorld!.Content.Hash(),
            state => state.CairoSetup is not null && state.CairoWorld is null && state.CairoSetup.PendingIds.Count == 0,
            state => state.AdmitCairoWorldCore(),
            "Cairo active world requires all source placements and no duplicate."),
        new("MalettiWorld", MalettiWorldContractVersion, CampaignSourceMalettiActiveWorld.PolicyId,
            state => state.CairoWorldProfileHash,
            state => state.MalettiWorld!.Content.Hash(),
            state => state.CanPromoteMaletti,
            state => state.PromoteMalettiCore(),
            "No newly detached Maletti units are ready for promotion."),
        new("IndependentWorld", IndependentWorldContractVersion, CampaignSourceIndependentActiveWorld.PolicyId,
            state => state.CairoWorldProfileHash,
            state => state.IndependentWorld!.Content.Hash(),
            state => state.CanPromoteIndependent,
            state => state.PromoteIndependentCore(),
            "No source independent units are ready for promotion."),
        new("LocalMapWorld", LocalMapWorldContractVersion, CampaignSourceLocalMapWorld.PolicyId,
            state => state.IndependentWorldProfileHash,
            state => state.LocalMapWorld!.Content.Hash(),
            state => state.IndependentWorld is not null && state.LocalMapWorld is null,
            state => state.AdmitLocalMapCore(),
            "Local source map requires the prior independent world and no duplicate."),
        new("ExpandedIndependentWorld", ExpandedIndependentWorldContractVersion, CampaignSourceExpandedIndependentWorld.PolicyId,
            state => state.LocalMapWorldProfileHash,
            state => state.ExpandedIndependentWorld!.Content.Hash(),
            state => state.CanPromoteExpandedIndependent,
            state => state.PromoteExpandedIndependentCore(),
            "No expanded source units are ready for promotion."),
        new("AttachedInitialLoads", AttachedInitialLoadsContractVersion, CampaignSourceAttachedInitialLoads.PolicyId,
            state => state.ExpandedIndependentWorldProfileHash,
            state => CampaignSourceAttachedInitialLoads.Create().Hash(),
            state => state.ExpandedIndependentWorld is not null && state.AttachedInitialLoads is null && state.PriorityMotorization is not null,
            state => state.AdmitAttachedInitialLoadsCore(),
            "Initial loads require the expanded source world and priority setup."),
        new("InitialWrecks", InitialWrecksContractVersion, CampaignSourceInitialWrecks.PolicyId,
            state => state.AttachedInitialLoadsProfileHash,
            state => CampaignSourceInitialWrecks.Create(state.Pregame).Hash(),
            state => state.AttachedInitialLoads is not null && state.InitialWrecks is null,
            state => state.AdmitInitialWrecksCore(),
            "Initial wrecks require prior setup and no duplicate."),
        new("AxisFreeSetup", AxisFreeSetupContractVersion, CampaignSourceAxisFreeArtillery.PolicyId,
            state => state.InitialWrecksProfileHash,
            state => state.AxisFreeArtillery!.InitialHash(),
            state => state.InitialWrecks is not null && state.Battlefield is not null && state.AxisFreeArtillery is null,
            state => state.AdmitAxisFreeSetupCore(),
            "Axis free setup requires prior source setup and no duplicate."),
        new("AxisFreeWorld", AxisFreeWorldContractVersion, CampaignSourceAxisFreeArtilleryWorld.PolicyId,
            state => state.AxisFreeSetupProfileHash,
            state => state.AxisFreeArtilleryWorld!.Content.Hash(),
            state => state.CanPromoteAxisFreeWorld,
            state => state.PromoteAxisFreeWorldCore(),
            "No placed Axis source guns await activation."),
        new("AxisCorpsSetup", AxisCorpsSetupContractVersion, CampaignSourceAxisCorpsArtillery.PolicyId,
            state => state.AxisFreeWorldProfileHash,
            state => state.AxisCorpsArtillery!.InitialHash(),
            state => state.AxisFreeArtilleryWorld is not null && state.Battlefield is not null && state.AxisCorpsArtillery is null
                && state.AxisFreeArtillery is not null && state.AxisFreeArtillery.PendingIds.Count == 0 && !state.CanPromoteAxisFreeWorld,
            state => state.AdmitAxisCorpsSetupCore(),
            "Axis corps setup requires the previous world and no duplicate."),
        new("AxisCorpsWorld", AxisCorpsWorldContractVersion, CampaignSourceAxisCorpsArtilleryWorld.PolicyId,
            state => state.AxisCorpsSetupProfileHash,
            state => state.AxisCorpsArtilleryWorld!.Content.Hash(),
            state => state.CanPromoteAxisCorpsWorld,
            state => state.PromoteAxisCorpsWorldCore(),
            "No placed Axis corps guns await activation."),
        new("MalettiHierarchy", MalettiHierarchyContractVersion, CampaignSourceMalettiHierarchy.PolicyId,
            state => state.AxisCorpsWorldProfileHash,
            state => state.MalettiHierarchy!.InitialHash(),
            state => state.AxisCorpsArtilleryWorld is not null && state.MalettiHierarchy is null,
            state => state.AdmitMalettiHierarchyCore(),
            "Maletti hierarchy requires prior world and no duplicate."),
        new("CertifiedMalettiWorld", CertifiedMalettiWorldContractVersion, CampaignSourceCertifiedMalettiWorld.PolicyId,
            state => state.MalettiHierarchyProfileHash,
            state => state.CertifiedMalettiWorld!.Content.Hash(),
            state => state.CanPromoteCertifiedMalettiWorld,
            state => state.PromoteCertifiedMalettiWorldCore(),
            "Certified Maletti unit not ready."),
        new("MatruhFormation", MatruhFormationContractVersion, CampaignSourceMatruhFormation.PolicyId,
            state => state.MalettiHierarchyProfileHash,
            state => state.MatruhFormation!.InitialHash(),
            state => state.MalettiHierarchy is { Confirmed: true } && state.Battlefield is not null && state.MatruhFormation is null,
            state => state.AdmitMatruhFormationCore(),
            "Matruh formation requires the prior hierarchy and no duplicate."),
        new("MatruhDetachedWorld", MatruhDetachedWorldContractVersion, CampaignSourceMatruhDetachedWorld.PolicyId,
            state => state.MatruhFormationProfileHash,
            state => state.MatruhDetachedWorld!.Content.Hash(),
            state => state.CanPromoteMatruhDetachedWorld,
            state => state.PromoteMatruhDetachedWorldCore(),
            "Matruh detached unit is not ready."),
        new("Gt2Correction", Gt2CorrectionContractVersion, CampaignSourceGt2ConvoyCorrection.PolicyId,
            state => state.AuthoritySetupHash,
            state => CampaignSourceGt2ConvoyCorrection.Create(state.ConvoyPlan).Hash(),
            state => state.Gt2Correction is null,
            state => state.AdmitGt2CorrectionCore(),
            "GT2 correction already admitted."),
        new("CwPriorityCatalog", CwPriorityCatalogContractVersion, CampaignSourceCwPriorityCatalog.PolicyId,
            state => state.AuthoritySetupHash,
            _ => CampaignSourceCwPriorityCatalog.InitialHash(),
            state => state.CwPriorityCatalog is null,
            state => state.AdmitCwPriorityCatalogCore(),
            "Commonwealth priority catalog already admitted."),
        new("CwPriorityWorld", CwPriorityWorldContractVersion, CampaignSourceCwPriorityWorld.PolicyId,
            state => state.CwPriorityCatalogProfileHash,
            state => state.CwPriorityWorld!.Content.Hash(),
            state => state.CanPromoteCwPriorityWorld,
            state => state.PromoteCwPriorityWorldCore(),
            "Commonwealth source priority world not ready."),
        new("CwFreeGuns", CwFreeGunsContractVersion, CampaignSourceCwFreeGuns.PolicyId,
            state => state.CwPriorityWorldProfileHash,
            state => state.CwFreeGuns!.InitialHash(),
            state => state.CwPriorityWorld is not null && state.Battlefield is not null && state.CwFreeGuns is null,
            state => state.AdmitCwFreeGunsCore(),
            "CW free guns require source world and no duplicate."),
        new("CwFreeGunsWorld", CwFreeGunsWorldContractVersion, CampaignSourceCwFreeGunsWorld.PolicyId,
            state => state.CwFreeGunsProfileHash,
            state => state.CwFreeGunsWorld!.Content.Hash(),
            state => state.CanPromoteCwFreeGunsWorld,
            state => state.PromoteCwFreeGunsWorldCore(),
            "No CW free guns await activation."),
    ];

    internal static SourceAdmissionStep StepByKind(string kind) =>
        AdmissionChain.Single(step => step.Kind == kind);
}
