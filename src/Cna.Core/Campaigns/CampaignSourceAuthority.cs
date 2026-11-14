using System.Text.Json;
using System.Text;
using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// Versioned successor of the retained, source-backed pregame.  Setup is still
// in progress; there is no game clock until setup obligations are satisfied.
internal sealed class CampaignSourceAuthorityState
{
    internal const int ContractVersion = 4;
    internal const int SupplementalContractVersion = 5;
    internal const int CommonwealthSupplementalContractVersion = 6;
    internal const int PriorityMotorizationContractVersion = 7;
    internal const int AxisPriorityMotorizationContractVersion = 8;
    internal const int AxisGarrisonsContractVersion = 9;
    internal const int BattlefieldContractVersion = 10;
    internal const int CairoSetupContractVersion = 11;
    internal const int CairoWorldContractVersion = 12;
    internal const int MalettiWorldContractVersion = 13;
    internal const int IndependentWorldContractVersion = 14;
    internal const int LocalMapWorldContractVersion = 15;
    internal const int ExpandedIndependentWorldContractVersion = 16;
    internal const int AttachedInitialLoadsContractVersion = 17;
    internal const int InitialWrecksContractVersion = 18;
    internal const int AxisFreeSetupContractVersion = 19;
    internal const int AxisFreeWorldContractVersion = 20;
    internal const int AxisCorpsSetupContractVersion = 21;
    internal const int AxisCorpsWorldContractVersion = 22;
    internal const int MalettiHierarchyContractVersion = 23;
    internal const int CertifiedMalettiWorldContractVersion = 24;
    internal const int MatruhFormationContractVersion = 25;
    internal const int MatruhDetachedWorldContractVersion = 26;
    internal const int Gt2CorrectionContractVersion = 27;
    internal const int CwPriorityCatalogContractVersion = 28;
    internal const int CwPriorityWorldContractVersion = 29;
    internal const int CompactCheckpointContractVersion = 30;
    internal const int CwFreeGunsContractVersion = 31;
    internal const int CwFreeGunsWorldContractVersion = 32;
    internal const int UnattachedCargoContractVersion = 33;
    internal static readonly string FinishActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.pregame.gt2-supply.finish.v1"));
    internal const string SetupPositionId = "land.graziani-source-setup";
    internal const string AuthorityPolicyId = "graziani.source-authority.setup.provisional-dummy-omission.v1";
    private static readonly string[] InitialLedgerHashes =
    [
        "sha256:2abf7916a4b15270296d00e1e3019e10aff53af0b532b49f6fb377b6123104b2",
        "sha256:65ac39b0e93183c5423761dd17adaaba709b80dd1611897f62e14b7eccdaa373",
        "sha256:0832f9947bd4a614d65f1331ad2a981db1855df681464a3cb4187861a743ebe4",
        "sha256:176c59cf523effa00c043d809d8a37914fce4b7d8bd0aa6abf46a4d6dab84a24",
        "sha256:7e41bb146a93294548b577c0681529e31b46fd731cb324e142a584a71e91a289",
        "sha256:512cffd255916eecdea793c45afd9667a4f381f4ac1a4e794302b01526015b03",
    ];

    private CampaignSourceAuthorityState(CampaignPregameSourceState pregame,
        CampaignSourceConvoyPlan convoyPlan, CampaignSourceAxisSupplyWorld axisSupplyWorld,
        CampaignSourceCommonwealthSupplyWorld commonwealthSupplyWorld,
        CampaignSourceAxisInitialForces axisInitialForces,
        CampaignSourceCommonwealthInitialForces commonwealthInitialForces,
        CampaignSourceMotorizationWorld axisMotorization, CampaignSourceMotorizationWorld commonwealthMotorization,
        CampaignSourceOpeningConvoyPlan openingConvoyPlan,
        CampaignSourceCommonwealthSupplyPlans? commonwealthSupplyPlans,
        CampaignSourceAxisSupplementalForces? axisSupplement, CampaignSourceAutoblindaSetup? autoblinda,
        CampaignSourceCommonwealthSupplementalForces? commonwealthSupplement,
        CampaignSourcePriorityMotorization? priorityMotorization,
        CampaignSourceAxisPriorityMotorization? axisPriorityMotorization,
        CampaignSourceAxisGarrisonForces? axisGarrisons, CampaignSourceBattlefield? battlefield,
        CampaignSourceCairoInitialSetup? cairoSetup, CampaignSourceCairoActiveWorld? cairoWorld,
        CampaignSourceMalettiActiveWorld? malettiWorld, CampaignSourceIndependentActiveWorld? independentWorld,
        CampaignSourceLocalMapWorld? localMapWorld, long stateVersion, CampaignSourceExpandedIndependentWorld? expandedIndependentWorld = null, CampaignSourceAttachedInitialLoads? attachedInitialLoads = null, CampaignSourceInitialWrecks? initialWrecks = null, CampaignSourceAxisFreeArtillery? axisFreeArtillery = null, CampaignSourceAxisFreeArtilleryWorld? axisFreeArtilleryWorld = null, CampaignSourceAxisCorpsArtillery? axisCorpsArtillery = null, CampaignSourceAxisCorpsArtilleryWorld? axisCorpsArtilleryWorld = null, CampaignSourceMalettiHierarchy? malettiHierarchy = null, CampaignSourceCertifiedMalettiWorld? certifiedMalettiWorld = null, CampaignSourceMatruhFormation? matruhFormation = null, CampaignSourceMatruhDetachedWorld? matruhDetachedWorld = null, CampaignSourceGt2ConvoyCorrection? gt2Correction = null, CampaignSourceCwPriorityCatalog? cwPriorityCatalog = null, CampaignSourceCwPriorityWorld? cwPriorityWorld = null, bool compactCheckpoint = false, CampaignSourceUnattachedCargo? axisUnattachedCargo = null, CampaignSourceUnattachedCargo? commonwealthUnattachedCargo = null, CampaignSourceCwFreeGuns? cwFreeGuns = null, CampaignSourceCwFreeGunsWorld? cwFreeGunsWorld = null, CampaignGroupedWorld? groupedWorld = null, CampaignSourceSeptemberConvoyPlans? septemberPlans = null)
    {
        Pregame = pregame;
        ConvoyPlan = convoyPlan;
        AxisSupplyWorld = axisSupplyWorld;
        CommonwealthSupplyWorld = commonwealthSupplyWorld;
        AxisInitialForces = axisInitialForces;
        CommonwealthInitialForces = commonwealthInitialForces;
        AxisMotorization = axisMotorization;
        CommonwealthMotorization = commonwealthMotorization;
        OpeningConvoyPlan = openingConvoyPlan;
        CommonwealthSupplyPlans = commonwealthSupplyPlans;
        if (septemberPlans is not null && (commonwealthSupplyPlans is null || commonwealthSupplyPlans.NextTargetGameTurn is not null))
            throw new ArgumentException("September planning requires completed Commonwealth plans.");
        SeptemberPlans = septemberPlans;
        AxisSupplement = axisSupplement;
        if ((axisSupplement is null) != (autoblinda is null))
            throw new ArgumentException("Autoblinda setup requires the matching source supplement.");
        Autoblinda = autoblinda;
        if (commonwealthSupplement is not null && axisSupplement is null)
            throw new ArgumentException("Commonwealth supplement requires the prior source profile.");
        CommonwealthSupplement = commonwealthSupplement;
        if (priorityMotorization is not null && commonwealthSupplement is null)
            throw new ArgumentException("Priority motorization requires the prior source profile.");
        PriorityMotorization = priorityMotorization;
        if (axisPriorityMotorization is not null && priorityMotorization is null)
            throw new ArgumentException("Axis priority motorization requires the prior source profile.");
        AxisPriorityMotorization = axisPriorityMotorization;
        if (axisGarrisons is not null && axisPriorityMotorization is null)
            throw new ArgumentException("Axis garrisons require the prior source profile.");
        AxisGarrisons = axisGarrisons;
        if (battlefield is not null && axisGarrisons is null)
            throw new ArgumentException("Source battlefield requires the prior profile.");
        Battlefield = battlefield;
        if (cairoSetup is not null && battlefield is null)
            throw new ArgumentException("Cairo setup requires the prior source battlefield.");
        CairoSetup = cairoSetup;
        if (cairoWorld is not null && cairoSetup is null)
            throw new ArgumentException("Cairo active world requires the prior source setup.");
        CairoWorld = cairoWorld;
        if (malettiWorld is not null && cairoWorld is null)
            throw new ArgumentException("Maletti active world requires the prior Cairo world.");
        MalettiWorld = malettiWorld;
        if (independentWorld is not null && cairoWorld is null)
            throw new ArgumentException("Independent world requires the prior Cairo world.");
        IndependentWorld = independentWorld;
        if (localMapWorld is not null && independentWorld is null)
            throw new ArgumentException("Local map requires the prior independent world.");
        LocalMapWorld = localMapWorld;
        if (expandedIndependentWorld is not null && localMapWorld is null)
            throw new ArgumentException("Expanded independent world requires the prior local map.");
        ExpandedIndependentWorld = expandedIndependentWorld;
        if (attachedInitialLoads is not null && expandedIndependentWorld is null)
            throw new ArgumentException("Attached initial loads require the prior source world.");
        AttachedInitialLoads = attachedInitialLoads;
        if (initialWrecks is not null && attachedInitialLoads is null)
            throw new ArgumentException("Initial wrecks require the prior setup profile.");
        InitialWrecks = initialWrecks;
        if (axisFreeArtillery is not null && initialWrecks is null)
            throw new ArgumentException("Axis free setup requires prior source setup.");
        AxisFreeArtillery = axisFreeArtillery;
        if (axisFreeArtilleryWorld is not null && axisFreeArtillery is null)
            throw new ArgumentException("Axis free world requires source setup.");
        AxisFreeArtilleryWorld = axisFreeArtilleryWorld;
        if (axisCorpsArtillery is not null && axisFreeArtilleryWorld is null)
            throw new ArgumentException("Axis corps setup requires prior source world.");
        if (axisCorpsArtilleryWorld is not null && axisCorpsArtillery is null)
            throw new ArgumentException("Axis corps world requires source setup.");
        AxisCorpsArtillery = axisCorpsArtillery;
        AxisCorpsArtilleryWorld = axisCorpsArtilleryWorld;
        if (malettiHierarchy is not null && axisCorpsArtilleryWorld is null)
            throw new ArgumentException("Maletti hierarchy requires prior source world.");
        MalettiHierarchy = malettiHierarchy;
        if (certifiedMalettiWorld is not null && malettiHierarchy is null)
            throw new ArgumentException("Certified Maletti world requires its hierarchy.");
        CertifiedMalettiWorld = certifiedMalettiWorld;
        if (matruhFormation is not null && malettiHierarchy is null)
            throw new ArgumentException("Matruh formation requires prior source hierarchy.");
        MatruhFormation = matruhFormation;
        if (matruhDetachedWorld is not null && matruhFormation is null)
            throw new ArgumentException("Matruh detached world requires the source formation.");
        MatruhDetachedWorld = matruhDetachedWorld;
        Gt2Correction = gt2Correction;
        if (cwPriorityWorld is not null && cwPriorityCatalog is null)
            throw new ArgumentException("Commonwealth priority world requires catalog.");
        CwPriorityCatalog = cwPriorityCatalog;
        CwPriorityWorld = cwPriorityWorld;
        CompactCheckpoint = compactCheckpoint;
        AxisUnattachedCargo = axisUnattachedCargo;
        CommonwealthUnattachedCargo = commonwealthUnattachedCargo;
        if (cwFreeGunsWorld is not null && cwFreeGuns is null) throw new ArgumentException("CW free guns world requires setup.");
        CwFreeGuns = cwFreeGuns;
        CwFreeGunsWorld = cwFreeGunsWorld;
        GroupedWorld = groupedWorld is null ? null : groupedWorld.Prior == World ? groupedWorld : groupedWorld.Rebind(World);
        StateVersion = stateVersion;
        // Later setup actions must not invalidate either retained, owner-selected
        // source stack proof. Unsupported new occupants fail closed here.
        MalettiHierarchy?.Validate(this);
        MatruhFormation?.Validate(this);
        CwPriorityCatalog?.Validate(this);
        AxisUnattachedCargo?.Validate(this);
        CommonwealthUnattachedCargo?.Validate(this);
        CwFreeGuns?.Validate(this);
        if (GroupedWorld is not null)
        {
            CampaignSourceFourthIndianDefinition.Validate(GroupedWorld);
            var groupedBattlefield = Battlefield ?? throw new InvalidOperationException("Grouped battlefield missing.");
            groupedBattlefield.ValidateFourthIndian(GroupedWorld);
            if (GroupedWorld.Groups.Any(value => value.GroupId == CampaignSourceMatruhGroupedDefinition.GroupId))
            {
                CampaignSourceMatruhGroupedDefinition.Validate(this, GroupedWorld);
                groupedBattlefield.ValidateGroupedMatruh(GroupedWorld, MatruhFormation!);
            }
            if (GroupedWorld.Groups.Any(value => value.GroupId == CampaignSourceMalettiGroupedDefinition.GroupId))
            {
                CampaignSourceMalettiGroupedDefinition.Validate(this, GroupedWorld);
                groupedBattlefield.ValidateGroupedMaletti(GroupedWorld, MalettiHierarchy!, AxisPriorityMotorization!);
            }
            if (IsLibyanGrouped)
            {
                CampaignSourceLibyanGroupedDefinition.Validate(this, GroupedWorld);
                groupedBattlefield.ValidateLibyanGroups(GroupedWorld);
            }
            if (IsItalianDivisionsGrouped)
            {
                CampaignSourceCireneMarmaricaGroupedDefinition.Validate(this, GroupedWorld);
                groupedBattlefield.ValidateItalianDivisions(GroupedWorld);
            }
        }
        if (GroupedWorld is not null && (CommonwealthMotorization.Remaining(CampaignSourceFourthIndianDefinition.PoolId) != 22
            || AttachedInitialLoads is null || AttachedInitialLoads.Reserved(CampaignSourceFourthIndianDefinition.PoolId) != 0))
            throw new InvalidOperationException("Fourth Indian motor reservations must share the retained source pool.");
    }

    internal CampaignPregameSourceState Pregame { get; }
    internal CampaignCreationRequest Request => Pregame.Request;
    internal ContentSourceScheduledCombatPackArtifact Content => Pregame.Source.Session.Content;
    internal CampaignWorldSnapshotV8 World => CwFreeGunsWorld?.World ?? CwPriorityWorld?.World ?? MatruhDetachedWorld?.World ?? CertifiedMalettiWorld?.World ?? AxisCorpsArtilleryWorld?.World ?? AxisFreeArtilleryWorld?.World ?? ExpandedIndependentWorld?.World ?? LocalMapWorld?.World ?? IndependentWorld?.World ?? MalettiWorld?.World ?? CairoWorld?.World ?? Pregame.Source.Session.InitialWorld;
    internal RandomStreamState RandomState => SeptemberPlans?.RandomState ?? CommonwealthSupplyPlans?.RandomState ?? OpeningConvoyPlan.RandomState;
    internal CampaignSourceConvoyPlan ConvoyPlan { get; }
    internal CampaignSourceAxisSupplyWorld AxisSupplyWorld { get; }
    internal CampaignSourceCommonwealthSupplyWorld CommonwealthSupplyWorld { get; }
    internal CampaignSourceAxisInitialForces AxisInitialForces { get; }
    internal CampaignSourceCommonwealthInitialForces CommonwealthInitialForces { get; }
    internal CampaignSourceMotorizationWorld AxisMotorization { get; }
    internal CampaignSourceMotorizationWorld CommonwealthMotorization { get; }
    internal CampaignSourceOpeningConvoyPlan OpeningConvoyPlan { get; }
    internal CampaignSourceCommonwealthSupplyPlans? CommonwealthSupplyPlans { get; }
    internal CampaignSourceSeptemberConvoyPlans? SeptemberPlans { get; }
    internal CampaignSourceAxisSupplementalForces? AxisSupplement { get; }
    internal CampaignSourceAutoblindaSetup? Autoblinda { get; }
    internal CampaignSourceCommonwealthSupplementalForces? CommonwealthSupplement { get; }
    internal CampaignSourcePriorityMotorization? PriorityMotorization { get; }
    internal CampaignSourceAxisPriorityMotorization? AxisPriorityMotorization { get; }
    internal CampaignSourceAxisGarrisonForces? AxisGarrisons { get; }
    internal CampaignSourceBattlefield? Battlefield { get; }
    internal CampaignSourceCairoInitialSetup? CairoSetup { get; }
    internal CampaignSourceCairoActiveWorld? CairoWorld { get; }
    internal CampaignSourceMalettiActiveWorld? MalettiWorld { get; }
    internal CampaignSourceIndependentActiveWorld? IndependentWorld { get; }
    internal CampaignSourceLocalMapWorld? LocalMapWorld { get; }
    internal CampaignSourceExpandedIndependentWorld? ExpandedIndependentWorld { get; }
    internal CampaignSourceAttachedInitialLoads? AttachedInitialLoads { get; }
    internal CampaignSourceInitialWrecks? InitialWrecks { get; }
    internal CampaignSourceAxisFreeArtillery? AxisFreeArtillery { get; }
    internal CampaignSourceAxisFreeArtilleryWorld? AxisFreeArtilleryWorld { get; }
    internal CampaignSourceAxisCorpsArtillery? AxisCorpsArtillery { get; }
    internal CampaignSourceAxisCorpsArtilleryWorld? AxisCorpsArtilleryWorld { get; }
    internal CampaignSourceMalettiHierarchy? MalettiHierarchy { get; }
    internal CampaignSourceCertifiedMalettiWorld? CertifiedMalettiWorld { get; }
    internal CampaignSourceMatruhFormation? MatruhFormation { get; }
    internal CampaignSourceMatruhDetachedWorld? MatruhDetachedWorld { get; }
    internal CampaignSourceGt2ConvoyCorrection? Gt2Correction { get; }
    internal CampaignSourceCwPriorityCatalog? CwPriorityCatalog { get; }
    internal CampaignSourceCwPriorityWorld? CwPriorityWorld { get; }
    internal bool CompactCheckpoint { get; }
    internal int ProfileVersion => CompactCheckpoint ? CompactCheckpointContractVersion : LegacyProfileVersion;
    internal CampaignSourceUnattachedCargo? AxisUnattachedCargo { get; }
    internal CampaignSourceUnattachedCargo? CommonwealthUnattachedCargo { get; }
    internal CampaignSourceCwFreeGuns? CwFreeGuns { get; }
    internal CampaignSourceCwFreeGunsWorld? CwFreeGunsWorld { get; }
    internal CampaignGroupedWorld? GroupedWorld { get; }
    private int LegacyProfileVersion => IsItalianDivisionsGrouped ? 39 : SeptemberPlans is not null ? 38 : IsLibyanGrouped ? 37 : IsMalettiGrouped ? 36 : GroupedWorld?.Groups.Any(value => value.GroupId == CampaignSourceMatruhGroupedDefinition.GroupId) == true ? 35 : GroupedWorld is not null ? 34 : AxisUnattachedCargo is not null || CommonwealthUnattachedCargo is not null ? UnattachedCargoContractVersion
        : CwFreeGunsWorld is not null ? CwFreeGunsWorldContractVersion
        : CwFreeGuns is not null ? CwFreeGunsContractVersion
        : CwPriorityWorld is not null ? CwPriorityWorldContractVersion
        : CwPriorityCatalog is not null ? CwPriorityCatalogContractVersion
        : Gt2Correction is not null ? Gt2CorrectionContractVersion
        : MatruhDetachedWorld is not null ? MatruhDetachedWorldContractVersion
        : MatruhFormation is not null ? MatruhFormationContractVersion
        : CertifiedMalettiWorld is not null ? CertifiedMalettiWorldContractVersion
        : MalettiHierarchy is not null ? MalettiHierarchyContractVersion
        : AxisCorpsArtilleryWorld is not null ? AxisCorpsWorldContractVersion
        : AxisCorpsArtillery is not null ? AxisCorpsSetupContractVersion
        : AxisFreeArtilleryWorld is not null ? AxisFreeWorldContractVersion
        : AxisFreeArtillery is not null ? AxisFreeSetupContractVersion
        : InitialWrecks is not null ? InitialWrecksContractVersion
        : AttachedInitialLoads is not null ? AttachedInitialLoadsContractVersion
        : ExpandedIndependentWorld is not null ? ExpandedIndependentWorldContractVersion
        : LocalMapWorld is not null ? LocalMapWorldContractVersion
        : IndependentWorld is not null ? IndependentWorldContractVersion
        : MalettiWorld is not null ? MalettiWorldContractVersion
        : CairoWorld is not null ? CairoWorldContractVersion
        : CairoSetup is not null ? CairoSetupContractVersion
        : Battlefield is not null ? BattlefieldContractVersion
        : AxisGarrisons is not null ? AxisGarrisonsContractVersion
        : AxisPriorityMotorization is not null ? AxisPriorityMotorizationContractVersion
        : PriorityMotorization is not null ? PriorityMotorizationContractVersion
        : CommonwealthSupplement is not null ? CommonwealthSupplementalContractVersion
        : AxisSupplement is null ? ContractVersion : SupplementalContractVersion;
    internal string SupplementalProfileHash => AxisSupplement is null
        ? throw new InvalidOperationException("Source supplement is not admitted.")
        : CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(AuthoritySetupHash + "\0"
            + AxisSupplement.Hash() + "\0" + CampaignSourceAutoblindaSetup.PolicyId + "\0"
            + CampaignSourceAutoblindaSetup.Create(AxisSupplement).Hash()));
    internal string CommonwealthSupplementalProfileHash => CommonwealthSupplement is null
        ? throw new InvalidOperationException("Commonwealth source supplement is not admitted.")
        : CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(SupplementalProfileHash + "\0"
            + CommonwealthSupplement.Hash()));
    internal string PriorityMotorizationProfileHash => PriorityMotorization is null || CommonwealthSupplement is null
        ? throw new InvalidOperationException("Priority motorization profile is not admitted.")
        : CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(CommonwealthSupplementalProfileHash + "\0"
            + CampaignSourcePriorityMotorization.PolicyId + "\0"
            + CampaignSourcePriorityMotorization.Create(CommonwealthSupplement, CommonwealthMotorization).Hash()));
    internal string AxisPriorityMotorizationProfileHash => AxisPriorityMotorization is null || AxisSupplement is null
        ? throw new InvalidOperationException("Axis priority motorization profile is not admitted.")
        : CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(PriorityMotorizationProfileHash + "\0"
            + CampaignSourceAxisPriorityMotorization.PolicyId + "\0"
            + CampaignSourceAxisPriorityMotorization.Create(AxisSupplement, AxisMotorization).Hash()));
    internal string AxisGarrisonsProfileHash => AxisGarrisons is null
        ? throw new InvalidOperationException("Axis garrisons are not admitted.")
        : CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(AxisPriorityMotorizationProfileHash + "\0" + AxisGarrisons.Hash()));
    internal string BattlefieldProfileHash => Battlefield is null
        ? throw new InvalidOperationException("Source battlefield is not admitted.")
        : CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(AxisGarrisonsProfileHash + "\0"
            + CampaignSourceBattlefield.PolicyId + "\0" + CampaignSourceBattlefield.Create(this).Hash()));
    internal string CairoSetupProfileHash => CairoSetup is null || Battlefield is null
        ? throw new InvalidOperationException("Cairo setup is not admitted.")
        : CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(BattlefieldProfileHash + "\0"
            + CampaignSourceCairoInitialSetup.PolicyId + "\0"
            + CampaignSourceCairoInitialSetup.Create(CampaignSourceBattlefield.Create(this)).Hash()));
    internal string CairoWorldProfileHash => CairoWorld is null
        ? throw new InvalidOperationException("Cairo active world is not admitted.")
        : CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(CairoSetupProfileHash + "\0"
            + CampaignSourceCairoActiveWorld.PolicyId + "\0" + CairoWorld.Content.Hash()));
    internal string MalettiWorldProfileHash => MalettiWorld is null
        ? throw new InvalidOperationException("Maletti active world is not admitted.")
        : CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(CairoWorldProfileHash + "\0"
            + CampaignSourceMalettiActiveWorld.PolicyId + "\0" + MalettiWorld.Content.Hash()));
    internal string IndependentWorldProfileHash => IndependentWorld is null
        ? throw new InvalidOperationException("Independent source world is not admitted.")
        : CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(CairoWorldProfileHash + "\0"
            + CampaignSourceIndependentActiveWorld.PolicyId + "\0" + IndependentWorld.Content.Hash()));
    internal string LocalMapWorldProfileHash => LocalMapWorld is null
        ? throw new InvalidOperationException("Source local map is not admitted.")
        : CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(IndependentWorldProfileHash + "\0"
            + CampaignSourceLocalMapWorld.PolicyId + "\0" + LocalMapWorld.Content.Hash()));
    internal string ExpandedIndependentWorldProfileHash => ExpandedIndependentWorld is null
        ? throw new InvalidOperationException("Expanded independent source world is not admitted.")
        : CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(LocalMapWorldProfileHash + "\0"
            + CampaignSourceExpandedIndependentWorld.PolicyId + "\0" + ExpandedIndependentWorld.Content.Hash()));
    internal string AttachedInitialLoadsProfileHash => AttachedInitialLoads is null
        ? throw new InvalidOperationException("Initial loads profile is not admitted.")
        : CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(ExpandedIndependentWorldProfileHash + "\0"
            + CampaignSourceAttachedInitialLoads.PolicyId + "\0" + CampaignSourceAttachedInitialLoads.Create().Hash()));
    internal string InitialWrecksProfileHash => InitialWrecks is null
        ? throw new InvalidOperationException("Initial wrecks profile is not admitted.")
        : CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(AttachedInitialLoadsProfileHash + "\0"
            + CampaignSourceInitialWrecks.PolicyId + "\0" + CampaignSourceInitialWrecks.Create(Pregame).Hash()));
    internal string AxisFreeSetupProfileHash => AxisFreeArtillery is null
        ? throw new InvalidOperationException("Axis free setup profile is not admitted.")
        : CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(InitialWrecksProfileHash + "\0"
            + CampaignSourceAxisFreeArtillery.PolicyId + "\0" + AxisFreeArtillery.InitialHash()));
    internal string AxisFreeWorldProfileHash => AxisFreeArtilleryWorld is null
        ? throw new InvalidOperationException("Axis free source world is not admitted.")
        : CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(AxisFreeSetupProfileHash + "\0"
            + CampaignSourceAxisFreeArtilleryWorld.PolicyId + "\0" + AxisFreeArtilleryWorld.Content.Hash()));
    internal string AxisCorpsSetupProfileHash => AxisCorpsArtillery is null
        ? throw new InvalidOperationException("Axis corps setup is not admitted.")
        : CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(AxisFreeWorldProfileHash + "\0"
            + CampaignSourceAxisCorpsArtillery.PolicyId + "\0" + AxisCorpsArtillery.InitialHash()));
    internal string AxisCorpsWorldProfileHash => AxisCorpsArtilleryWorld is null
        ? throw new InvalidOperationException("Axis corps source world is not admitted.")
        : CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(AxisCorpsSetupProfileHash + "\0"
            + CampaignSourceAxisCorpsArtilleryWorld.PolicyId + "\0" + AxisCorpsArtilleryWorld.Content.Hash()));
    internal string MalettiHierarchyProfileHash => MalettiHierarchy is null
        ? throw new InvalidOperationException("Maletti hierarchy is not admitted.")
        : CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(AxisCorpsWorldProfileHash + "\0"
            + CampaignSourceMalettiHierarchy.PolicyId + "\0" + MalettiHierarchy.InitialHash()));
    internal string CertifiedMalettiWorldProfileHash => CertifiedMalettiWorld is null
        ? throw new InvalidOperationException("Certified Maletti world is not admitted.")
        : CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(MalettiHierarchyProfileHash + "\0"
            + CampaignSourceCertifiedMalettiWorld.PolicyId + "\0" + CertifiedMalettiWorld.Content.Hash()));
    internal string MatruhFormationProfileHash => MatruhFormation is null
        ? throw new InvalidOperationException("Matruh source formation is not admitted.")
        : CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(MalettiHierarchyProfileHash + "\0"
            + CampaignSourceMatruhFormation.PolicyId + "\0" + MatruhFormation.InitialHash()));
    internal string MatruhDetachedWorldProfileHash => MatruhDetachedWorld is null
        ? throw new InvalidOperationException("Matruh detached world is not admitted.")
        : CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(MatruhFormationProfileHash + "\0"
            + CampaignSourceMatruhDetachedWorld.PolicyId + "\0" + MatruhDetachedWorld.Content.Hash()));
    internal string Gt2CorrectionProfileHash => Gt2Correction is null
        ? throw new InvalidOperationException("GT2 correction is not admitted.")
        : CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(AuthoritySetupHash + "\0"
            + CampaignSourceGt2ConvoyCorrection.PolicyId + "\0" + CampaignSourceGt2ConvoyCorrection.Create(ConvoyPlan).Hash()));
    internal string CwPriorityCatalogProfileHash => CwPriorityCatalog is null
        ? throw new InvalidOperationException("Commonwealth priority catalog is not admitted.")
        : CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(AuthoritySetupHash + "\0"
            + CampaignSourceCwPriorityCatalog.PolicyId + "\0" + CampaignSourceCwPriorityCatalog.InitialHash()));
    internal string CwPriorityWorldProfileHash => CwPriorityWorld is null
        ? throw new InvalidOperationException("Commonwealth priority world is not admitted.")
        : CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(CwPriorityCatalogProfileHash + "\0"
            + CampaignSourceCwPriorityWorld.PolicyId + "\0" + CwPriorityWorld.Content.Hash()));
    internal string UnattachedCargoProfileHash => AxisUnattachedCargo is null && CommonwealthUnattachedCargo is null
        ? throw new InvalidOperationException("Initial unattached cargo not admitted.")
        : CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(AuthoritySetupHash + "\0" + CampaignSourceUnattachedCargo.PolicyId
            + "\0" + CampaignSourceUnattachedCargo.InitialHash(LandSide.Axis) + "\0" + CampaignSourceUnattachedCargo.InitialHash(LandSide.Commonwealth)));
    internal string CwFreeGunsProfileHash => CwFreeGuns is null
        ? throw new InvalidOperationException("CW free guns not admitted.")
        : CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(CwPriorityWorldProfileHash + "\0" + CampaignSourceCwFreeGuns.PolicyId + "\0" + CwFreeGuns.InitialHash()));
    internal string CwFreeGunsWorldProfileHash => CwFreeGunsWorld is null
        ? throw new InvalidOperationException("CW free guns world not admitted.")
        : CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(CwFreeGunsProfileHash + "\0" + CampaignSourceCwFreeGunsWorld.PolicyId + "\0" + CwFreeGunsWorld.Content.Hash()));
    internal long StateVersion { get; }
    internal LandSequencePosition? Position { get; }
    internal string AuthoritySetupHash => CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(
        AuthorityPolicyId + "\0" + Request.SetupHash + "\0" + Content.Identity.Hash + "\0"
        + CampaignSourceAxisSupplyWorld.PolicyId + "\0" + CampaignSourceCommonwealthSupplyWorld.PolicyId + "\0"
        + CampaignSourceAxisInitialForces.PolicyId + "\0" + CampaignSourceCommonwealthInitialForces.PolicyId + "\0"
        + CampaignSourceAxisSupplyWorld.Create(Pregame).Hash() + "\0"
        + CampaignSourceCommonwealthSupplyWorld.Create(Pregame).Hash() + "\0"
        + CampaignSourceAxisInitialForces.Create(Pregame).Hash() + "\0"
        + CampaignSourceCommonwealthInitialForces.Create(Pregame).Hash() + "\0"
        + CampaignSourceMotorizationWorld.Create(Pregame, LandSide.Axis).Hash() + "\0"
        + CampaignSourceMotorizationWorld.Create(Pregame, LandSide.Commonwealth).Hash() + "\0"
        + CampaignSourceOpeningConvoyPlan.PolicyId + "\0"
        + "sha256:9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca" + "\0"
        + CampaignSourceCommonwealthSupplyPlans.PolicyId + "\0"
        + "sha256:f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981"));

    internal static CampaignSourceAuthorityState Finish(CampaignPregameSourceState pregame)
    {
        var state = new CampaignSourceAuthorityState(pregame, CampaignSourceConvoyPlan.Create(pregame), CampaignSourceAxisSupplyWorld.Create(pregame),
            CampaignSourceCommonwealthSupplyWorld.Create(pregame),
            CampaignSourceAxisInitialForces.Create(pregame),
            CampaignSourceCommonwealthInitialForces.Create(pregame),
            CampaignSourceMotorizationWorld.Create(pregame, LandSide.Axis),
            CampaignSourceMotorizationWorld.Create(pregame, LandSide.Commonwealth),
            CampaignSourceOpeningConvoyPlan.Create(pregame),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            checked(pregame.StateVersion + 1));
        string[] actual = [state.AxisSupplyWorld.Hash(), state.CommonwealthSupplyWorld.Hash(),
            state.AxisInitialForces.Hash(), state.CommonwealthInitialForces.Hash(),
            state.AxisMotorization.Hash(), state.CommonwealthMotorization.Hash()];
        if (!actual.SequenceEqual(InitialLedgerHashes, StringComparer.Ordinal))
            throw new InvalidOperationException("Version 4 source setup tables differ from their pinned identity.");
        return state;
    }

    internal CampaignSourceAuthorityState PlaceDump(string dumpId, string locationId) =>
        new(Pregame, ConvoyPlan, AxisSupplyWorld.Place(dumpId, locationId), CommonwealthSupplyWorld, AxisInitialForces, CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans, AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization, AxisPriorityMotorization, AxisGarrisons, Battlefield, CairoSetup, CairoWorld, MalettiWorld, IndependentWorld, LocalMapWorld, checked(StateVersion + 1), ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld, AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, GroupedWorld, septemberPlans: SeptemberPlans);

    internal CampaignSourceAuthorityState PlaceCommonwealthSupply(string placementId, string locationId) =>
        new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld.Place(placementId, locationId), AxisInitialForces, CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans, AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization, AxisPriorityMotorization, AxisGarrisons, Battlefield, CairoSetup, CairoWorld, MalettiWorld, IndependentWorld, LocalMapWorld, checked(StateVersion + 1), ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld, AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, GroupedWorld, septemberPlans: SeptemberPlans);

    internal CampaignSourceAuthorityState DecideDummy(bool axis, string dummyId, string? locationId) => axis
        ? new(Pregame, ConvoyPlan, AxisSupplyWorld.DecideDummy(dummyId, locationId), CommonwealthSupplyWorld,
            AxisInitialForces, CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans, AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization, AxisPriorityMotorization, AxisGarrisons, Battlefield, CairoSetup, CairoWorld, MalettiWorld, IndependentWorld, LocalMapWorld, checked(StateVersion + 1), ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld, AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, GroupedWorld, septemberPlans: SeptemberPlans)
        : dummyId == "cw-dummy-1"
            ? new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld.DecideDummy(locationId),
                AxisInitialForces, CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans, AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization, AxisPriorityMotorization, AxisGarrisons, Battlefield, CairoSetup, CairoWorld, MalettiWorld, IndependentWorld, LocalMapWorld, checked(StateVersion + 1), ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld, AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, GroupedWorld, septemberPlans: SeptemberPlans)
            : throw new InvalidOperationException("Unknown Commonwealth dummy group.");

    internal CampaignSourceAuthorityState AdjustMotorization(bool axis, string poolId, string locationId, int delta) => axis
        ? new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces, CommonwealthInitialForces,
            AxisMotorization.Assign(poolId, locationId, delta), CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans, AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization, AxisPriorityMotorization, AxisGarrisons, Battlefield, CairoSetup, CairoWorld, MalettiWorld, IndependentWorld, LocalMapWorld, checked(StateVersion + 1), ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld, AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, GroupedWorld, septemberPlans: SeptemberPlans)
        : new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces, CommonwealthInitialForces,
            AxisMotorization, CommonwealthMotorization.Assign(poolId, locationId, delta), OpeningConvoyPlan, CommonwealthSupplyPlans, AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization, AxisPriorityMotorization, AxisGarrisons, Battlefield, CairoSetup, CairoWorld, MalettiWorld, IndependentWorld, LocalMapWorld, checked(StateVersion + 1), ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld, AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, GroupedWorld, septemberPlans: SeptemberPlans);

    internal CampaignSourceAuthorityState ApplyOpeningConvoyChoice(string kind, int lane, int stage, int delta)
    {
        var plan = kind switch
        {
            "roll" when lane == 0 && stage == 0 && delta == 0 => OpeningConvoyPlan.Roll(),
            "adjust" => OpeningConvoyPlan.Adjust(lane, stage, delta),
            "finish" when lane == 0 && stage == 0 && delta == 0 => OpeningConvoyPlan.Finish(),
            _ => throw new InvalidOperationException("Invalid GT1 convoy choice."),
        };
        var cwPlans = plan.IsFinished ? CampaignSourceCommonwealthSupplyPlans.Create(plan) : null;
        return new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces,
            CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, plan, cwPlans, AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization, AxisPriorityMotorization, AxisGarrisons, Battlefield, CairoSetup, CairoWorld, MalettiWorld, IndependentWorld, LocalMapWorld, checked(StateVersion + 1), ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld, AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, GroupedWorld, septemberPlans: SeptemberPlans);
    }

    internal static string OpeningConvoyActionId(string kind, int lane, int stage, int delta) =>
        CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(
            "graziani.setup.gt1-supply.v1\0" + kind + "\0"
            + lane.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\0"
            + stage.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\0"
            + delta.ToString(System.Globalization.CultureInfo.InvariantCulture)));

    internal byte[] SerializeOpeningConvoyEvent(CampaignSourceAuthorityState successor,
        string kind, int lane, int stage, int delta)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "source-gt1-supply-planned");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", OpeningConvoyActionId(kind, lane, stage, delta));
            writer.WriteString("actor", "axis");
            writer.WriteString("choice", kind);
            writer.WriteNumber("lane", lane);
            writer.WriteNumber("operationStage", stage);
            writer.WriteNumber("delta", delta);
            writer.WriteNumber("randomCursorBefore", RandomState.NextByteCursor);
            writer.WriteNumber("randomCursorAfter", successor.RandomState.NextByteCursor);
            writer.WriteString("openingConvoyPlanHash", successor.OpeningConvoyPlan.Hash());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal CampaignSourceAuthorityState ApplyCommonwealthSupplyChoice(string kind, int targetTurn)
    {
        var plans = CommonwealthSupplyPlans ?? throw new InvalidOperationException("Finish Axis GT1 planning first.");
        var next = kind switch
        {
            "skip" when targetTurn == 1 => plans.SkipGt1(),
            "roll" => plans.Roll(targetTurn),
            _ => throw new InvalidOperationException("Invalid Commonwealth supply choice."),
        };
        return new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces,
            CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, next, AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization, AxisPriorityMotorization, AxisGarrisons, Battlefield, CairoSetup, CairoWorld, MalettiWorld, IndependentWorld, LocalMapWorld,
            checked(StateVersion + 1), ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld, AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, GroupedWorld, septemberPlans: SeptemberPlans);
    }

    internal CampaignSourceAuthorityState ApplySeptemberSupplyChoice(string kind, int targetTurn, int lane, int stage, int delta)
    {
        var next = kind switch
        {
            "begin" when targetTurn == 0 && lane == 0 && stage == 0 && delta == 0 && SeptemberPlans is null => CampaignSourceSeptemberConvoyPlans.Create(CommonwealthSupplyPlans ?? throw new InvalidOperationException("Complete Commonwealth planning first.")),
            "roll" when lane == 0 && stage == 0 && delta == 0 => (SeptemberPlans ?? throw new InvalidOperationException("Begin September planning first.")).Roll(targetTurn),
            "adjust" => (SeptemberPlans ?? throw new InvalidOperationException("Begin September planning first.")).Adjust(targetTurn, lane, stage, delta),
            "finish" when lane == 0 && stage == 0 && delta == 0 => (SeptemberPlans ?? throw new InvalidOperationException("Begin September planning first.")).Finish(targetTurn),
            _ => throw new InvalidOperationException("Invalid September supply choice."),
        };
        return new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces,
            CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans, AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization, AxisPriorityMotorization, AxisGarrisons, Battlefield, CairoSetup, CairoWorld, MalettiWorld, IndependentWorld, LocalMapWorld,
            checked(StateVersion + 1), ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld, AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, GroupedWorld, septemberPlans: next);
    }

    internal static string SeptemberSupplyActionId(string kind, int targetTurn, int lane, int stage, int delta) =>
        CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(FormattableString.Invariant($"graziani.setup.axis-september-supply.v1\0{kind}\0{targetTurn}\0{lane}\0{stage}\0{delta}")));

    internal byte[] SerializeSeptemberSupplyEvent(CampaignSourceAuthorityState successor, string kind, int targetTurn, int lane, int stage, int delta)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 38);
            writer.WriteString("eventType", "source-axis-september-supply-changed");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", SeptemberSupplyActionId(kind, targetTurn, lane, stage, delta));
            writer.WriteString("actor", "axis");
            writer.WriteString("choice", kind);
            writer.WriteNumber("targetGameTurn", targetTurn);
            writer.WriteNumber("lane", lane);
            writer.WriteNumber("operationStage", stage);
            writer.WriteNumber("delta", delta);
            writer.WriteNumber("randomCursorBefore", RandomState.NextByteCursor);
            writer.WriteNumber("randomCursorAfter", successor.RandomState.NextByteCursor);
            writer.WriteString("septemberSupplyPlansHash", successor.SeptemberPlans!.Hash());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static readonly string AdmitSupplementActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.axis-forces.supplement.v1"));

    internal CampaignSourceAuthorityState AdmitAxisSupplement()
    {
        if (AxisSupplement is not null) throw new InvalidOperationException("The Axis source supplement is already admitted.");
        var supplement = CampaignSourceAxisSupplementalForces.Create(Pregame, AxisInitialForces);
        return new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces,
            CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans,
            supplement, CampaignSourceAutoblindaSetup.Create(supplement), null, null, null, null, null, null, null, null, null, null, checked(StateVersion + 1), ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld, AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, GroupedWorld, septemberPlans: SeptemberPlans);
    }

    internal CampaignSourceAuthorityState ApplyAutoblindaChoice(string kind, string target)
    {
        var setup = Autoblinda ?? throw new InvalidOperationException("Axis source supplement is not admitted.");
        var next = kind switch
        {
            "replacements" when target.Length == 0 => setup.ChooseTobrukReplacements(),
            "attach" => setup.Attach(target),
            "reset" when target.Length == 0 => setup.Reset(),
            _ => throw new InvalidOperationException("Unsupported Autoblinda choice."),
        };
        return new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces,
            CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans,
            AxisSupplement, next, CommonwealthSupplement, PriorityMotorization, AxisPriorityMotorization, AxisGarrisons, Battlefield, CairoSetup, CairoWorld, MalettiWorld, IndependentWorld, LocalMapWorld, checked(StateVersion + 1), ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld, AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, GroupedWorld, septemberPlans: SeptemberPlans);
    }

    internal static readonly string AdmitCommonwealthSupplementActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.commonwealth-forces.supplement.v1"));

    internal CampaignSourceAuthorityState AdmitCommonwealthSupplement()
    {
        if (AxisSupplement is null || CommonwealthSupplement is not null)
            throw new InvalidOperationException("Commonwealth source supplement requires the prior profile and no duplicate.");
        var supplement = CampaignSourceCommonwealthSupplementalForces.Create(Pregame, CommonwealthInitialForces);
        return new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces,
            CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans,
            AxisSupplement, Autoblinda, supplement, null, null, null, null, null, null, null, null, null, checked(StateVersion + 1), ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld, AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, GroupedWorld, septemberPlans: SeptemberPlans);
    }

    internal byte[] SerializeCommonwealthSupplementEvent(CampaignSourceAuthorityState successor)
    {
        var supplement = successor.CommonwealthSupplement ?? throw new InvalidOperationException("Missing Commonwealth source supplement.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CommonwealthSupplementalContractVersion);
            writer.WriteString("eventType", "source-commonwealth-force-supplement-admitted");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", AdmitCommonwealthSupplementActionId);
            writer.WriteString("actor", "system");
            writer.WriteString("priorSupplementalProfileHash", SupplementalProfileHash);
            writer.WriteString("supplementHash", supplement.Hash());
            writer.WriteString("commonwealthSupplementalProfileHash", successor.CommonwealthSupplementalProfileHash);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static readonly string AdmitPriorityMotorizationActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.priority-motorization.admit.v1"));

    internal CampaignSourceAuthorityState AdmitPriorityMotorization()
    {
        if (CommonwealthSupplement is null || PriorityMotorization is not null)
            throw new InvalidOperationException("Priority motorization requires the prior source profile and no duplicate.");
        var priority = CampaignSourcePriorityMotorization.Create(CommonwealthSupplement, CommonwealthMotorization);
        return new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces,
            CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans,
            AxisSupplement, Autoblinda, CommonwealthSupplement, priority, null, null, null, null, null, null, null, null, checked(StateVersion + 1), ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld, AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, GroupedWorld, septemberPlans: SeptemberPlans);
    }

    internal byte[] SerializePriorityMotorizationAdmission(CampaignSourceAuthorityState successor)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", PriorityMotorizationContractVersion);
            writer.WriteString("eventType", "source-priority-motorization-admitted");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", AdmitPriorityMotorizationActionId);
            writer.WriteString("actor", "system");
            writer.WriteString("priorProfileHash", CommonwealthSupplementalProfileHash);
            writer.WriteString("priorityMotorizationHash", successor.PriorityMotorization!.Hash());
            writer.WriteString("priorityMotorizationProfileHash", successor.PriorityMotorizationProfileHash);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal CampaignSourceAuthorityState AdjustPriorityMotorization(string elementId, int delta)
    {
        var priority = PriorityMotorization ?? throw new InvalidOperationException("Priority motorization is not admitted.");
        if (AttachedInitialLoads is not null && !AttachedInitialLoads.CanApplyPriorityDelta(elementId, delta, priority, CommonwealthMotorization))
            throw new InvalidOperationException("Priority allocation conflicts with attached initial loads.");
        var next = priority.Adjust(elementId, delta, CommonwealthMotorization);
        return new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces,
            CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans,
            AxisSupplement, Autoblinda, CommonwealthSupplement, next, AxisPriorityMotorization, AxisGarrisons, Battlefield, CairoSetup, CairoWorld, MalettiWorld, IndependentWorld, LocalMapWorld, checked(StateVersion + 1), ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld, AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, GroupedWorld, septemberPlans: SeptemberPlans);
    }

    internal static string PriorityMotorizationActionId(string elementId, int delta) =>
        CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes("graziani.setup.priority-motorization.v1\0" + elementId
            + "\0" + delta.ToString(System.Globalization.CultureInfo.InvariantCulture)));

    internal byte[] SerializePriorityMotorizationEvent(CampaignSourceAuthorityState successor, string elementId, int delta)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", PriorityMotorizationContractVersion);
            writer.WriteString("eventType", "source-priority-motorization-adjusted");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", PriorityMotorizationActionId(elementId, delta));
            writer.WriteString("actor", "commonwealth");
            writer.WriteString("elementId", elementId);
            writer.WriteNumber("delta", delta);
            writer.WriteString("priorityMotorizationHash", successor.PriorityMotorization!.Hash());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static readonly string AdmitAxisPriorityActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.axis-priority-motorization.admit.v1"));

    internal CampaignSourceAuthorityState AdmitAxisPriorityMotorization()
    {
        if (PriorityMotorization is null || AxisPriorityMotorization is not null || AxisSupplement is null)
            throw new InvalidOperationException("Axis priority setup requires the prior source profile and no duplicate.");
        var priority = CampaignSourceAxisPriorityMotorization.Create(AxisSupplement, AxisMotorization);
        return new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces,
            CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans,
            AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization, priority, null, null, null, null, null, null, null, checked(StateVersion + 1), ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld, AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, GroupedWorld, septemberPlans: SeptemberPlans);
    }

    internal byte[] SerializeAxisPriorityAdmission(CampaignSourceAuthorityState successor)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", AxisPriorityMotorizationContractVersion);
            writer.WriteString("eventType", "source-axis-priority-motorization-admitted");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", AdmitAxisPriorityActionId);
            writer.WriteString("actor", "system");
            writer.WriteString("priorProfileHash", PriorityMotorizationProfileHash);
            writer.WriteString("axisPriorityHash", successor.AxisPriorityMotorization!.Hash());
            writer.WriteString("axisPriorityProfileHash", successor.AxisPriorityMotorizationProfileHash);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal CampaignSourceAuthorityState ApplyAxisPriorityChoice(string kind, string elementId, int delta)
    {
        var priority = AxisPriorityMotorization ?? throw new InvalidOperationException("Axis priority setup is not admitted.");
        if (kind == "undo-detachment" && World.World.Elements.Any(value => value.ElementId == elementId))
            throw new InvalidOperationException("An active independent unit cannot undo detachment without a world reattachment transition.");
        var next = kind switch
        {
            "detach" when delta == 0 => priority.Detach(elementId),
            "undo-detachment" when delta == 0 => priority.UndoDetachment(elementId),
            "adjust" => priority.Adjust(elementId, delta, AxisMotorization),
            _ => throw new InvalidOperationException("Unsupported Axis priority choice."),
        };
        var grouped = IsMalettiGrouped && kind == "adjust" ? GroupedWorld!.AssignMotorization(CampaignSourceMalettiGroupedDefinition.GroupId, elementId, delta) : GroupedWorld;
        return new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces,
            CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans,
            AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization, next, AxisGarrisons, Battlefield, CairoSetup, CairoWorld, MalettiWorld, IndependentWorld, LocalMapWorld, checked(StateVersion + 1), ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld, AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, grouped, septemberPlans: SeptemberPlans);
    }

    internal static string AxisPriorityActionId(string kind, string elementId, int delta) =>
        CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes("graziani.setup.axis-priority.v1\0" + kind + "\0"
            + elementId + "\0" + delta.ToString(System.Globalization.CultureInfo.InvariantCulture)));

    internal byte[] SerializeAxisPriorityEvent(CampaignSourceAuthorityState successor, string kind, string elementId, int delta)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", AxisPriorityMotorizationContractVersion);
            writer.WriteString("eventType", "source-axis-priority-motorization-changed");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", AxisPriorityActionId(kind, elementId, delta));
            writer.WriteString("actor", "axis");
            writer.WriteString("choice", kind);
            writer.WriteString("elementId", elementId);
            writer.WriteNumber("delta", delta);
            writer.WriteString("axisPriorityHash", successor.AxisPriorityMotorization!.Hash());
            if (successor.IsMalettiGrouped) writer.WriteString("groupedWorldHash", successor.GroupedWorld!.Hash());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static readonly string AdmitAxisGarrisonsActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.axis-garrisons.admit.v1"));

    internal CampaignSourceAuthorityState AdmitAxisGarrisons()
    {
        if (AxisPriorityMotorization is null || AxisGarrisons is not null || AxisSupplement is null)
            throw new InvalidOperationException("Axis garrisons require the prior source profile and no duplicate.");
        var garrisons = CampaignSourceAxisGarrisonForces.Create(Pregame, AxisInitialForces, AxisSupplement);
        return new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces,
            CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans,
            AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization, AxisPriorityMotorization, garrisons, null, null, null, null, null, null,
            checked(StateVersion + 1), ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld, AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, GroupedWorld, septemberPlans: SeptemberPlans);
    }

    internal byte[] SerializeAxisGarrisonsAdmission(CampaignSourceAuthorityState successor)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", AxisGarrisonsContractVersion);
            writer.WriteString("eventType", "source-axis-minor-garrisons-admitted");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", AdmitAxisGarrisonsActionId);
            writer.WriteString("actor", "system");
            writer.WriteString("priorProfileHash", AxisPriorityMotorizationProfileHash);
            writer.WriteString("axisGarrisonsHash", successor.AxisGarrisons!.Hash());
            writer.WriteString("axisGarrisonsProfileHash", successor.AxisGarrisonsProfileHash);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static readonly string AdmitBattlefieldActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.battlefield.admit.v1"));

    internal CampaignSourceAuthorityState AdmitBattlefield()
    {
        if (AxisGarrisons is null || Battlefield is not null)
            throw new InvalidOperationException("Source battlefield requires the prior profile and no duplicate.");
        var battlefield = CampaignSourceBattlefield.Create(this);
        return new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces,
            CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans,
            AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization, AxisPriorityMotorization, AxisGarrisons,
            battlefield, null, null, null, null, null, checked(StateVersion + 1), ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld, AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, GroupedWorld, septemberPlans: SeptemberPlans);
    }

    internal byte[] SerializeBattlefieldAdmission(CampaignSourceAuthorityState successor)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", BattlefieldContractVersion);
            writer.WriteString("eventType", "source-battlefield-admitted");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", AdmitBattlefieldActionId);
            writer.WriteString("actor", "system");
            writer.WriteString("priorProfileHash", AxisGarrisonsProfileHash);
            writer.WriteString("battlefieldHash", successor.Battlefield!.Hash());
            writer.WriteString("battlefieldProfileHash", successor.BattlefieldProfileHash);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static readonly string AdmitCairoSetupActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.cairo-setup.admit.v1"));

    internal CampaignSourceAuthorityState AdmitCairoSetup()
    {
        if (Battlefield is null || CairoSetup is not null)
            throw new InvalidOperationException("Cairo setup requires the prior battlefield and no duplicate.");
        var cairo = CampaignSourceCairoInitialSetup.Create(Battlefield);
        var battlefield = Battlefield.AddCairoSetup(cairo);
        return new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces,
            CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans,
            AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization, AxisPriorityMotorization, AxisGarrisons,
            battlefield, cairo, CairoWorld, MalettiWorld, IndependentWorld, LocalMapWorld, checked(StateVersion + 1), ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld, AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, GroupedWorld, septemberPlans: SeptemberPlans);
    }

    internal byte[] SerializeCairoSetupAdmission(CampaignSourceAuthorityState successor)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CairoSetupContractVersion);
            writer.WriteString("eventType", "source-cairo-setup-admitted");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", AdmitCairoSetupActionId);
            writer.WriteString("actor", "system");
            writer.WriteString("priorProfileHash", BattlefieldProfileHash);
            writer.WriteString("cairoSetupHash", successor.CairoSetup!.Hash());
            writer.WriteString("battlefieldHash", successor.Battlefield!.Hash());
            writer.WriteString("cairoSetupProfileHash", successor.CairoSetupProfileHash);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal CampaignSourceAuthorityState PlaceCairoUnit(string elementId, string locationId)
    {
        if (Battlefield is null || CairoSetup is null) throw new InvalidOperationException("Cairo setup is unavailable.");
        var battlefield = Battlefield.PlaceCairo(CairoSetup, elementId, locationId);
        var cairo = CairoSetup.Place(elementId, locationId);
        return new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces,
            CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans,
            AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization, AxisPriorityMotorization, AxisGarrisons,
            battlefield, cairo, CairoWorld, MalettiWorld, IndependentWorld, LocalMapWorld, checked(StateVersion + 1), ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld, AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, GroupedWorld, septemberPlans: SeptemberPlans);
    }

    internal static string CairoPlacementActionId(string elementId, string locationId) =>
        CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes("graziani.setup.cairo-placement.v1\0" + elementId + "\0" + locationId));

    internal byte[] SerializeCairoPlacementEvent(CampaignSourceAuthorityState successor, string elementId, string locationId)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CairoSetupContractVersion);
            writer.WriteString("eventType", "source-cairo-unit-placed");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", CairoPlacementActionId(elementId, locationId));
            writer.WriteString("actor", "commonwealth");
            writer.WriteString("elementId", elementId);
            writer.WriteString("locationId", locationId);
            writer.WriteString("cairoSetupHash", successor.CairoSetup!.Hash());
            writer.WriteString("battlefieldHash", successor.Battlefield!.Hash());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static readonly string AdmitCairoWorldActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.cairo-world.admit.v1"));

    internal CampaignSourceAuthorityState AdmitCairoWorld()
    {
        if (CairoSetup is null || CairoWorld is not null || CairoSetup.PendingIds.Count != 0)
            throw new InvalidOperationException("Cairo active world requires all source placements and no duplicate.");
        var world = CampaignSourceCairoActiveWorld.Create(this);
        return new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces,
            CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans,
            AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization, AxisPriorityMotorization, AxisGarrisons,
            Battlefield, CairoSetup, world, null, null, null, checked(StateVersion + 1), ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld, AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, GroupedWorld, septemberPlans: SeptemberPlans);
    }

    internal byte[] SerializeCairoWorldAdmission(CampaignSourceAuthorityState successor)
    {
        var active = successor.CairoWorld ?? throw new InvalidOperationException("Missing Cairo active world.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CairoWorldContractVersion);
            writer.WriteString("eventType", "source-cairo-world-admitted");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", AdmitCairoWorldActionId);
            writer.WriteString("actor", "system");
            writer.WriteString("priorProfileHash", CairoSetupProfileHash);
            writer.WriteString("activeContentHash", active.Content.Hash());
            writer.WriteString("activeWorldHash", active.Hash());
            writer.WriteString("cairoWorldProfileHash", successor.CairoWorldProfileHash);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static readonly string PromoteMalettiActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.maletti-world.promote.v1"));

    internal bool CanPromoteMaletti => IndependentWorld is null && CairoWorld is not null && Battlefield is not null && AxisPriorityMotorization is not null
        && AxisPriorityMotorization.DetachedTargets.Except(MalettiWorld?.ActivatedIds ?? [], StringComparer.Ordinal).Any();

    internal CampaignSourceAuthorityState PromoteMaletti()
    {
        if (!CanPromoteMaletti) throw new InvalidOperationException("No newly detached Maletti units are ready for promotion.");
        var active = CampaignSourceMalettiActiveWorld.Create(this);
        var battlefield = Battlefield!.PromoteMaletti(AxisPriorityMotorization!, World, active.World);
        return new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces,
            CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans,
            AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization, AxisPriorityMotorization, AxisGarrisons,
            battlefield, CairoSetup, CairoWorld, active, null, null, checked(StateVersion + 1), ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld, AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, GroupedWorld, septemberPlans: SeptemberPlans);
    }

    internal byte[] SerializeMalettiPromotion(CampaignSourceAuthorityState successor)
    {
        var active = successor.MalettiWorld ?? throw new InvalidOperationException("Missing Maletti active world.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", MalettiWorldContractVersion);
            writer.WriteString("eventType", "source-maletti-world-promoted");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", PromoteMalettiActionId);
            writer.WriteString("actor", "system");
            writer.WriteString("priorProfileHash", MalettiWorld is null ? CairoWorldProfileHash : MalettiWorldProfileHash);
            writer.WriteString("activeContentHash", active.Content.Hash());
            writer.WriteString("activeWorldHash", active.Hash());
            writer.WriteString("battlefieldHash", successor.Battlefield!.Hash());
            writer.WriteString("malettiWorldProfileHash", successor.MalettiWorldProfileHash);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static readonly string PromoteIndependentActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.independent-world.promote.v1"));

    internal bool CanPromoteIndependent => ExpandedIndependentWorld is null && CairoWorld is not null && Battlefield is not null
        && AxisPriorityMotorization is not null && AxisSupplement is not null && CommonwealthSupplement is not null
        && ContentSourceIndependentPackArtifact.Units.Any(unit =>
            (unit.SideId == "commonwealth" || AxisPriorityMotorization.DetachedTargets.Contains(unit.ElementId, StringComparer.Ordinal))
            && World.World.Elements.All(element => element.ElementId != unit.ElementId));

    internal CampaignSourceAuthorityState PromoteIndependent()
    {
        if (!CanPromoteIndependent) throw new InvalidOperationException("No source independent units are ready for promotion.");
        var active = CampaignSourceIndependentActiveWorld.Create(this);
        var battlefield = Battlefield!.PromoteSourceIndependent(CommonwealthSupplement!, AxisSupplement!,
            AxisPriorityMotorization!, World, active.World);
        return new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces,
            CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans,
            AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization, AxisPriorityMotorization, AxisGarrisons,
            battlefield, CairoSetup, CairoWorld, MalettiWorld, active,
            LocalMapWorld is null ? null : CampaignSourceLocalMapWorld.Rebind(LocalMapWorld, active), checked(StateVersion + 1), ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld, AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, GroupedWorld, septemberPlans: SeptemberPlans);
    }

    internal byte[] SerializeIndependentPromotion(CampaignSourceAuthorityState successor)
    {
        var active = successor.IndependentWorld ?? throw new InvalidOperationException("Missing independent active world.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", IndependentWorldContractVersion);
            writer.WriteString("eventType", "source-independent-world-promoted");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", PromoteIndependentActionId);
            writer.WriteString("actor", "system");
            writer.WriteString("priorProfileHash", LocalMapWorld is not null ? LocalMapWorldProfileHash
                : IndependentWorld is not null ? IndependentWorldProfileHash
                : MalettiWorld is not null ? MalettiWorldProfileHash : CairoWorldProfileHash);
            writer.WriteString("activeContentHash", active.Content.Hash());
            writer.WriteString("activeWorldHash", active.Hash());
            writer.WriteString("battlefieldHash", successor.Battlefield!.Hash());
            writer.WriteString("independentWorldProfileHash", successor.IndependentWorldProfileHash);
            if (successor.LocalMapWorld is not null)
            {
                writer.WriteString("localMapContentHash", successor.LocalMapWorld.Content.Hash());
                writer.WriteString("localMapWorldHash", successor.LocalMapWorld.Hash());
            }
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static readonly string AdmitLocalMapActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.local-map.admit.v1"));

    internal static readonly string AdmitAttachedInitialLoadsActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.attached-initial-loads.admit.v1"));

    internal static readonly string AdmitInitialWrecksActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.initial-wrecks.admit.v1"));

    internal static readonly string AdmitAxisFreeSetupActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.axis-free-setup.admit.v1"));
    internal static readonly string AdmitAxisCorpsSetupActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.axis-corps-setup.admit.v1"));
    internal static readonly string AdmitMalettiHierarchyActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.maletti-hierarchy.admit.v1"));
    internal static readonly string AdmitMatruhFormationActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.matruh-formation.admit.v1"));
    internal static readonly string AdmitGt2CorrectionActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.gt2-correction.admit.v1"));
    internal static readonly string AdmitCompactCheckpointActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.compact-checkpoint.admit.v1"));
    internal static string UnattachedCargoActionId(bool axis, string kind, string? location, int delta) =>
        CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(FormattableString.Invariant($"graziani.source.unattached-cargo.v1\0{axis}\0{kind}\0{location}\0{delta}")));
    internal static readonly string AdmitCwFreeGunsActionId = CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes("graziani.source.cw-free-guns.admit.v1"));
    internal static readonly string PromoteCwFreeGunsWorldActionId = CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes("graziani.source.cw-free-guns-world.promote.v1"));
    internal static string CwFreeGunPlacementActionId(string id, string location) => CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes("graziani.source.cw-free-guns.place.v1\0" + id + "\0" + location));
    internal CampaignSourceAuthorityState AdmitCwFreeGuns()
    {
        if (CwPriorityWorld is null || Battlefield is null || CwFreeGuns is not null) throw new InvalidOperationException("CW free guns require source world and no duplicate.");
        var setup = CampaignSourceCwFreeGuns.Create(this, Battlefield);
        return WithCwFreeGuns(setup, Battlefield.AddCwFreeGuns(setup), CwFreeGunsWorld);
    }
    internal CampaignSourceAuthorityState PlaceCwFreeGun(string id, string location)
    {
        var setup = CwFreeGuns ?? throw new InvalidOperationException("CW free guns not admitted.");
        var battlefield = Battlefield ?? throw new InvalidOperationException("Source battlefield missing.");
        return WithCwFreeGuns(setup.Place(id, location, battlefield), battlefield.PlaceCwFreeGun(setup, id, location), CwFreeGunsWorld);
    }
    internal bool CanPromoteCwFreeGunsWorld => CwFreeGuns is not null && Battlefield is not null && CwPriorityWorld is not null
        && CwFreeGuns.Placements.Any(placement => World.World.Elements.All(element => element.ElementId != placement.ElementId));
    internal CampaignSourceAuthorityState PromoteCwFreeGunsWorld()
    {
        if (!CanPromoteCwFreeGunsWorld) throw new InvalidOperationException("No CW free guns await activation.");
        var active = CampaignSourceCwFreeGunsWorld.Create(this);
        return WithCwFreeGuns(CwFreeGuns!, Battlefield!.PromoteCwFreeGuns(CwFreeGuns!, World, active.World), active);
    }
    private CampaignSourceAuthorityState WithCwFreeGuns(CampaignSourceCwFreeGuns setup, CampaignSourceBattlefield battlefield, CampaignSourceCwFreeGunsWorld? active) =>
        new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces, CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization,
            OpeningConvoyPlan, CommonwealthSupplyPlans, AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization, AxisPriorityMotorization,
            AxisGarrisons, battlefield, CairoSetup, CairoWorld, MalettiWorld, IndependentWorld, LocalMapWorld, checked(StateVersion + 1), ExpandedIndependentWorld,
            AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld, AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy,
            CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint,
            AxisUnattachedCargo, CommonwealthUnattachedCargo, setup, active, GroupedWorld, septemberPlans: SeptemberPlans);
    internal byte[] SerializeCwFreeGunsEvent(CampaignSourceAuthorityState successor, string? kind = null, string? id = null, string? location = null)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", kind == "promote" ? CwFreeGunsWorldContractVersion : CwFreeGunsContractVersion);
            writer.WriteString("eventType", kind switch { null => "source-cw-free-guns-admitted", "promote" => "source-cw-free-guns-world-promoted", _ => "source-cw-free-gun-placed" });
            writer.WriteString("campaignId", Request.CampaignId); writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", kind switch { null => AdmitCwFreeGunsActionId, "promote" => PromoteCwFreeGunsWorldActionId, _ => CwFreeGunPlacementActionId(id!, location!) });
            writer.WriteString("actor", kind == "place" ? "commonwealth" : "system");
            if (kind == "place") { writer.WriteString("elementId", id); writer.WriteString("locationId", location); }
            writer.WriteString("cwFreeGunsHash", successor.CwFreeGuns!.Hash()); writer.WriteString("battlefieldHash", successor.Battlefield!.Hash());
            writer.WriteString("cwFreeGunsProfileHash", successor.CwFreeGunsProfileHash);
            if (kind == "promote") { writer.WriteString("activeContentHash", successor.CwFreeGunsWorld!.Content.Hash()); writer.WriteString("activeWorldHash", successor.CwFreeGunsWorld.Hash()); writer.WriteString("cwFreeGunsWorldProfileHash", successor.CwFreeGunsWorldProfileHash); }
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal static readonly string AdmitFourthIndianActionId = CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes("graziani.source.fourth-indian-world.admit.v1"));
    internal static readonly string ConfirmFourthIndianActionId = CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes("graziani.source.fourth-indian-disposition.confirm.v1"));
    internal static string FourthIndianMotorActionId(string id, int delta) => CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(FormattableString.Invariant($"graziani.source.fourth-indian-motor.v1\0{id}\0{delta}")));
    internal bool CanAdmitFourthIndian => GroupedWorld is null && CwFreeGunsWorld is not null && CwFreeGuns is not null
        && CwFreeGuns.PendingIds.Count == 0 && !CanPromoteCwFreeGunsWorld && Battlefield is not null;
    internal CampaignSourceAuthorityState AdmitFourthIndianWorld()
    {
        if (!CanAdmitFourthIndian) throw new InvalidOperationException("Fourth Indian world prerequisites are incomplete.");
        _ = CampaignSourceFourthIndianDefinition.Create(this);
        var entries = Battlefield!.Entries.Where(value => value.LocationId == "d3615").ToArray();
        if (entries.Length != CampaignSourceFourthIndianDefinition.Members.Count
            || entries.Any(value => value.SideId != "commonwealth" || value.AuthorityStatus != "source-evidence"
                || CampaignSourceFourthIndianDefinition.Members.All(member => member.Id != value.ElementId)))
            throw new InvalidOperationException("Fourth Indian source occupancy is incomplete or conflicting.");
        var artifact = ContentSourceFourthIndianPackArtifact.Create(CwFreeGunsWorld!.Content);
        _ = ContentSourceFourthIndianPackArtifact.Read(artifact.CanonicalBytes);
        var initialization = World.Initialization;
        if (CampaignSourceFourthIndianDefinition.Members.Any(member => initialization.CapabilityPointsExpended > new CapabilityPointAmount(member.CapabilityPoints, 1)))
            throw new InvalidOperationException("Initial expenditure exceeds a source member allowance.");
        var members = CampaignSourceFourthIndianDefinition.Members.Select(member => new CampaignGroupedMember(
            member.Id, member.ParentId, member.Kind is "infantry" or "motor-infantry" ? member.Toe : null,
            0, member.Kind == "motor-infantry", member.Toe is null ? null : member.Kind, member.Toe, member.CapabilityPoints,
            initialization.CapabilityPointsExpended, initialization.CohesionLevel));
        var group = new CampaignGroupedFormation("4-indian-division", "commonwealth", "4-indian-division-hq", "d3615", null, 22, members, false);
        var world = CampaignGroupedWorld.Create(World, artifact.Hash(), [group]);
        var occupants = world.OccupantsAt("d3615");
        if (occupants.Count != 1 || occupants[0].Formation?.GroupId != "4-indian-division"
            || world.StackAt("commonwealth", "d3615", _ => null) is not null)
            throw new InvalidOperationException("Fourth Indian source occupancy or shell/stack status differs.");
        return WithGroupedWorld(world, Battlefield.PromoteFourthIndian(world));
    }
    internal CampaignSourceAuthorityState ConfirmFourthIndianDisposition()
    {
        var prior = GroupedWorld ?? throw new InvalidOperationException("Fourth Indian world is unavailable.");
        var battlefield = Battlefield ?? throw new InvalidOperationException("Fourth Indian battlefield is unavailable.");
        var occupants = prior.OccupantsAt("d3615");
        if (occupants.Count != 1 || occupants[0].Formation?.GroupId != "4-indian-division")
            throw new InvalidOperationException("Fourth Indian occupancy is unavailable.");
        var confirmed = prior.ConfirmDisposition("4-indian-division", 5);
        return WithGroupedWorld(confirmed, battlefield.ConfirmFourthIndian(prior, confirmed));
    }
    internal CampaignSourceAuthorityState AdjustFourthIndianMotor(string id, int delta)
    {
        var world = GroupedWorld ?? throw new InvalidOperationException("Fourth Indian world is unavailable.");
        if (delta is not (-1 or 1)) throw new InvalidOperationException("Unsupported motor adjustment.");
        return WithGroupedWorld(world.AssignMotorization("4-indian-division", id, delta));
    }
    private CampaignSourceAuthorityState WithGroupedWorld(CampaignGroupedWorld world, CampaignSourceBattlefield? battlefield = null) =>
        new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces, CommonwealthInitialForces,
            AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans, AxisSupplement, Autoblinda,
            CommonwealthSupplement, PriorityMotorization, AxisPriorityMotorization, AxisGarrisons, battlefield ?? Battlefield, CairoSetup, CairoWorld,
            MalettiWorld, IndependentWorld, LocalMapWorld, checked(StateVersion + 1), ExpandedIndependentWorld, AttachedInitialLoads,
            InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld, AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy,
            CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint,
            AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, world, septemberPlans: SeptemberPlans);
    internal byte[] SerializeFourthIndianEvent(CampaignSourceAuthorityState successor, string? id = null, int delta = 0, bool confirm = false)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 34);
            writer.WriteString("eventType", confirm ? "source-fourth-indian-disposition-confirmed" : id is null ? "source-fourth-indian-world-admitted" : "source-fourth-indian-motor-adjusted");
            writer.WriteString("campaignId", Request.CampaignId); writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", confirm ? ConfirmFourthIndianActionId : id is null ? AdmitFourthIndianActionId : FourthIndianMotorActionId(id, delta));
            writer.WriteString("actor", id is null && !confirm ? "system" : "commonwealth");
            if (id is not null) { writer.WriteString("elementId", id); writer.WriteNumber("delta", delta); }
            writer.WriteString("worldHash", successor.GroupedWorld!.Hash());
            writer.WriteString("contentHash", successor.GroupedWorld.ContentHash); writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal static readonly string AdmitMatruhGroupedActionId = CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes("graziani.source.matruh-grouped-world.admit.v1"));
    internal bool IsMatruhGrouped => GroupedWorld?.Groups.Any(value => value.GroupId == CampaignSourceMatruhGroupedDefinition.GroupId) == true;
    internal bool CanAdmitMatruhGrouped => GroupedWorld is not null && !IsMatruhGrouped && MatruhFormation is { Confirmed: true }
        && CwFreeGunsWorld is not null;
    internal CampaignSourceAuthorityState AdmitMatruhGroupedWorld()
    {
        if (!CanAdmitMatruhGrouped) throw new InvalidOperationException("Matruh grouped world prerequisites are incomplete.");
        var fourth = ContentSourceFourthIndianPackArtifact.Create(CwFreeGunsWorld!.Content);
        var content = ContentSourceMatruhGroupedPackArtifact.Create(fourth);
        _ = ContentSourceMatruhGroupedPackArtifact.Read(content.CanonicalBytes);
        var group = CampaignSourceMatruhGroupedDefinition.Create(this);
        var next = GroupedWorld!.AddFormation(content.Hash(), group, CampaignSourceMatruhGroupedDefinition.CreateDetached(this));
        (Battlefield ?? throw new InvalidOperationException("Source battlefield missing.")).ValidateGroupedMatruh(next, MatruhFormation!);
        return WithGroupedWorld(next);
    }
    internal byte[] SerializeMatruhGroupedEvent(CampaignSourceAuthorityState successor)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 35);
            writer.WriteString("eventType", "source-matruh-grouped-world-admitted"); writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion); writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", AdmitMatruhGroupedActionId); writer.WriteString("actor", "system");
            writer.WriteString("worldHash", successor.GroupedWorld!.Hash()); writer.WriteString("contentHash", successor.GroupedWorld.ContentHash);
            writer.WriteString("matruhFormationHash", successor.MatruhFormation!.Hash()); writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal static readonly string AdmitMalettiGroupedActionId = CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes("graziani.source.maletti-grouped-world.admit.v1"));
    internal bool IsMalettiGrouped => GroupedWorld?.Groups.Any(value => value.GroupId == CampaignSourceMalettiGroupedDefinition.GroupId) == true;
    internal bool CanAdmitMalettiGrouped => IsMatruhGrouped && !IsMalettiGrouped && MalettiHierarchy is { Confirmed: true }
        && AxisPriorityMotorization is not null && Battlefield is not null && CwFreeGunsWorld is not null;
    internal CampaignSourceAuthorityState AdmitMalettiGroupedWorld()
    {
        if (!CanAdmitMalettiGrouped) throw new InvalidOperationException("Maletti grouped world prerequisites are incomplete.");
        var fourth = ContentSourceFourthIndianPackArtifact.Create(CwFreeGunsWorld!.Content);
        var matruh = ContentSourceMatruhGroupedPackArtifact.Create(fourth);
        var content = ContentSourceMalettiGroupedPackArtifact.Create(matruh);
        _ = ContentSourceMalettiGroupedPackArtifact.Read(content.CanonicalBytes);
        var group = CampaignSourceMalettiGroupedDefinition.Create(this);
        var (detached, external) = CampaignSourceMalettiGroupedDefinition.CreateDetached(this);
        var next = GroupedWorld!.AddFormation(content.Hash(), group, detached, external);
        var battlefield = Battlefield!.PromoteGroupedMaletti(next, MalettiHierarchy!, AxisPriorityMotorization!);
        return WithGroupedWorld(next, battlefield);
    }
    internal byte[] SerializeMalettiGroupedEvent(CampaignSourceAuthorityState successor)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 36);
            writer.WriteString("eventType", "source-maletti-grouped-world-admitted"); writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion); writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", AdmitMalettiGroupedActionId); writer.WriteString("actor", "system");
            writer.WriteString("worldHash", successor.GroupedWorld!.Hash()); writer.WriteString("contentHash", successor.GroupedWorld.ContentHash);
            writer.WriteString("malettiHierarchyHash", successor.MalettiHierarchy!.Hash()); writer.WriteString("axisPriorityHash", successor.AxisPriorityMotorization!.Hash());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal static readonly string AdmitLibyanGroupedActionId = CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes("graziani.source.libyan-grouped-world.admit.v1"));
    internal static string ConfirmLibyanGroupActionId(string groupId) => CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes("graziani.source.libyan-disposition.confirm.v1\0" + groupId));
    internal bool IsLibyanGrouped => GroupedWorld?.Groups.Any(value => value.GroupId == "1-libyan-division") == true;
    internal bool CanAdmitLibyanGrouped => IsMalettiGrouped && !IsLibyanGrouped && Battlefield is not null && CwFreeGunsWorld is not null;
    internal CampaignSourceAuthorityState AdmitLibyanGroupedWorld()
    {
        if (!CanAdmitLibyanGrouped) throw new InvalidOperationException("Libyan grouped world prerequisites are incomplete.");
        var fourth = ContentSourceFourthIndianPackArtifact.Create(CwFreeGunsWorld!.Content);
        var matruh = ContentSourceMatruhGroupedPackArtifact.Create(fourth);
        var maletti = ContentSourceMalettiGroupedPackArtifact.Create(matruh);
        var content = ContentSourceLibyanGroupedPackArtifact.Create(maletti);
        _ = ContentSourceLibyanGroupedPackArtifact.Read(content.CanonicalBytes);
        var groups = CampaignSourceLibyanGroupedDefinition.Create(this);
        var world = GroupedWorld!;
        foreach (var group in groups) world = world.AddFormation(content.Hash(), group);
        var battlefield = Battlefield!.ProposeLibyanGroups(world);
        return WithGroupedWorld(world, battlefield);
    }
    internal CampaignSourceAuthorityState ConfirmLibyanGroup(string groupId)
    {
        var prior = GroupedWorld ?? throw new InvalidOperationException("Libyan grouped world unavailable.");
        if (!IsLibyanGrouped || groupId is not ("1-libyan-division" or "2-libyan-division"))
            throw new InvalidOperationException("Libyan group unavailable.");
        var next = prior.ConfirmDisposition(groupId, 5);
        var battlefield = (Battlefield ?? throw new InvalidOperationException("Source battlefield missing."))
            .ConfirmLibyanGroup(prior, next, groupId);
        return WithGroupedWorld(next, battlefield);
    }
    internal byte[] SerializeLibyanGroupedEvent(CampaignSourceAuthorityState successor, string? groupId = null)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 37);
            writer.WriteString("eventType", groupId is null ? "source-libyan-grouped-world-admitted" : "source-libyan-disposition-confirmed");
            writer.WriteString("campaignId", Request.CampaignId); writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", groupId is null ? AdmitLibyanGroupedActionId : ConfirmLibyanGroupActionId(groupId));
            writer.WriteString("actor", groupId is null ? "system" : "axis");
            if (groupId is not null) writer.WriteString("groupId", groupId);
            writer.WriteString("worldHash", successor.GroupedWorld!.Hash()); writer.WriteString("contentHash", successor.GroupedWorld.ContentHash);
            writer.WriteString("battlefieldHash", successor.Battlefield!.Hash()); writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal static readonly string AdmitItalianDivisionsActionId = CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes("graziani.source.italian-divisions-grouped.admit.v1"));
    internal static string ConfirmItalianDivisionActionId(string groupId) => CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes("graziani.source.italian-division.confirm.v1\0" + groupId));
    internal bool IsItalianDivisionsGrouped => GroupedWorld?.Groups.Any(value => value.GroupId == CampaignSourceCireneMarmaricaGroupedDefinition.CireneId) == true;
    internal bool CanAdmitItalianDivisionsGrouped => IsLibyanGrouped && !IsItalianDivisionsGrouped && Battlefield is not null && CwFreeGunsWorld is not null;
    internal CampaignSourceAuthorityState AdmitItalianDivisionsGroupedWorld()
    {
        if (!CanAdmitItalianDivisionsGrouped) throw new InvalidOperationException("Italian grouped world prerequisites are incomplete.");
        var fourth = ContentSourceFourthIndianPackArtifact.Create(CwFreeGunsWorld!.Content);
        var matruh = ContentSourceMatruhGroupedPackArtifact.Create(fourth);
        var maletti = ContentSourceMalettiGroupedPackArtifact.Create(matruh);
        var libyan = ContentSourceLibyanGroupedPackArtifact.Create(maletti);
        var content = ContentSourceCireneMarmaricaGroupedPackArtifact.Create(libyan);
        _ = ContentSourceCireneMarmaricaGroupedPackArtifact.Read(content.CanonicalBytes);
        var world = GroupedWorld!;
        foreach (var group in CampaignSourceCireneMarmaricaGroupedDefinition.Create(this)) world = world.AddFormation(content.Hash(), group);
        return WithGroupedWorld(world, Battlefield!.ProposeItalianDivisions(world));
    }
    internal CampaignSourceAuthorityState ConfirmItalianDivision(string groupId)
    {
        var prior = GroupedWorld ?? throw new InvalidOperationException("Italian grouped world unavailable.");
        if (!IsItalianDivisionsGrouped || groupId is not (CampaignSourceCireneMarmaricaGroupedDefinition.CireneId or CampaignSourceCireneMarmaricaGroupedDefinition.MarmaricaId))
            throw new InvalidOperationException("Italian source division unavailable.");
        var next = prior.ConfirmDisposition(groupId, 5);
        var battlefield = (Battlefield ?? throw new InvalidOperationException("Source battlefield missing."))
            .ConfirmItalianDivision(prior, next, groupId);
        return WithGroupedWorld(next, battlefield);
    }
    internal byte[] SerializeItalianDivisionsGroupedEvent(CampaignSourceAuthorityState successor, string? groupId = null)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 39);
            writer.WriteString("eventType", groupId is null ? "source-italian-divisions-grouped-world-admitted" : "source-italian-division-disposition-confirmed");
            writer.WriteString("campaignId", Request.CampaignId); writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", groupId is null ? AdmitItalianDivisionsActionId : ConfirmItalianDivisionActionId(groupId));
            writer.WriteString("actor", groupId is null ? "system" : "axis");
            if (groupId is not null) writer.WriteString("groupId", groupId);
            writer.WriteString("worldHash", successor.GroupedWorld!.Hash()); writer.WriteString("contentHash", successor.GroupedWorld.ContentHash);
            writer.WriteString("battlefieldHash", successor.Battlefield!.Hash()); writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal CampaignSourceAuthorityState ApplyUnattachedCargo(bool axis, string kind, string? location, int delta)
    {
        var current = axis ? AxisUnattachedCargo : CommonwealthUnattachedCargo;
        var next = kind switch
        {
            "begin" when current is null && location is null && delta == 0 => CampaignSourceUnattachedCargo.Create(this, axis ? LandSide.Axis : LandSide.Commonwealth),
            "adjust" when current is not null && location is not null => current.Adjust(this, location, delta),
            "finish" when current is not null && location is null && delta == 0 => current.Finish(this),
            _ => throw new InvalidOperationException("Unavailable initial unattached cargo choice."),
        };
        return new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces,
            CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans,
            AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization, AxisPriorityMotorization, AxisGarrisons,
            Battlefield, CairoSetup, CairoWorld, MalettiWorld, IndependentWorld, LocalMapWorld, checked(StateVersion + 1),
            ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld,
            AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld,
            Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint,
            axis ? next : AxisUnattachedCargo, axis ? CommonwealthUnattachedCargo : next, CwFreeGuns, CwFreeGunsWorld, GroupedWorld, septemberPlans: SeptemberPlans);
    }
    internal byte[] SerializeUnattachedCargoEvent(CampaignSourceAuthorityState successor, bool axis, string kind, string? location, int delta)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", UnattachedCargoContractVersion);
            writer.WriteString("eventType", "source-unattached-cargo-changed"); writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion); writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", UnattachedCargoActionId(axis, kind, location, delta)); writer.WriteString("actor", axis ? "axis" : "commonwealth");
            writer.WriteString("kind", kind); if (location is null) writer.WriteNull("locationId"); else writer.WriteString("locationId", location);
            writer.WriteNumber("delta", delta); writer.WriteString("unattachedCargoHash", (axis ? successor.AxisUnattachedCargo : successor.CommonwealthUnattachedCargo)!.Hash());
            writer.WriteString("unattachedCargoProfileHash", successor.UnattachedCargoProfileHash); writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal CampaignSourceAuthorityState AdmitCompactCheckpoint()
    {
        if (CompactCheckpoint) throw new InvalidOperationException("Compact checkpoint already admitted.");
        return new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces,
            CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans,
            AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization, AxisPriorityMotorization, AxisGarrisons,
            Battlefield, CairoSetup, CairoWorld, MalettiWorld, IndependentWorld, LocalMapWorld, checked(StateVersion + 1),
            ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld,
            AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld,
            Gt2Correction, CwPriorityCatalog, CwPriorityWorld, true, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, GroupedWorld, septemberPlans: SeptemberPlans);
    }
    internal byte[] SerializeCompactCheckpointEvent(CampaignSourceAuthorityState successor)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", CompactCheckpointContractVersion);
            writer.WriteString("eventType", "source-compact-checkpoint-admitted"); writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion); writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", AdmitCompactCheckpointActionId); writer.WriteString("actor", "system");
            writer.WriteString("fullProjectionHash", CampaignPregameSourceCreation.Digest(successor.SerializeLegacyCheckpoint()));
            writer.WriteString("compactCheckpointHash", CampaignPregameSourceCreation.Digest(successor.SerializeCheckpoint()));
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal static readonly string AdmitCwPriorityCatalogActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.cw-priority-catalog.admit.v1"));
    internal static readonly string PromoteCwPriorityWorldActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.cw-priority-world.promote.v1"));
    internal bool CanPromoteCwPriorityWorld => CwPriorityCatalog is not null && CwPriorityWorld is null
        && AxisCorpsArtilleryWorld is not null && Battlefield is not null;
    internal CampaignSourceAuthorityState PromoteCwPriorityWorld()
    {
        if (!CanPromoteCwPriorityWorld) throw new InvalidOperationException("Commonwealth source priority world not ready.");
        var active = CampaignSourceCwPriorityWorld.Create(this);
        var battlefield = Battlefield!.PromoteCwPriorityWorld(CommonwealthSupplement!, World, active.World);
        return new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces,
            CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans,
            AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization, AxisPriorityMotorization, AxisGarrisons,
            battlefield, CairoSetup, CairoWorld, MalettiWorld, IndependentWorld, LocalMapWorld, checked(StateVersion + 1),
            ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld,
            AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld,
            Gt2Correction, CwPriorityCatalog, active, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, GroupedWorld, septemberPlans: SeptemberPlans);
    }
    internal byte[] SerializeCwPriorityWorldEvent(CampaignSourceAuthorityState successor)
    {
        var active = successor.CwPriorityWorld ?? throw new InvalidOperationException("Commonwealth priority world missing.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", CwPriorityWorldContractVersion);
            writer.WriteString("eventType", "source-cw-priority-world-promoted"); writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion); writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", PromoteCwPriorityWorldActionId); writer.WriteString("actor", "system");
            writer.WriteString("priorProfileHash", CwPriorityCatalogProfileHash);
            writer.WriteString("activeContentHash", active.Content.Hash()); writer.WriteString("activeWorldHash", active.Hash());
            writer.WriteString("battlefieldHash", successor.Battlefield!.Hash()); writer.WriteString("cwPriorityWorldProfileHash", successor.CwPriorityWorldProfileHash);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal static string CwPriorityCatalogActionId(string id, int delta) =>
        CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(FormattableString.Invariant($"graziani.source.cw-priority-catalog.adjust.v1\0{id}\0{delta}")));
    internal CampaignSourceAuthorityState AdmitCwPriorityCatalog()
    {
        if (CwPriorityCatalog is not null) throw new InvalidOperationException("Commonwealth priority catalog already admitted.");
        return WithCwPriorityCatalog(CampaignSourceCwPriorityCatalog.Create(this), PriorityMotorization!);
    }
    internal CampaignSourceAuthorityState AdjustCwPriorityCatalog(string id, int delta)
    {
        var catalog = CwPriorityCatalog ?? throw new InvalidOperationException("Commonwealth priority catalog not admitted.");
        var (next, priority) = catalog.Adjust(this, id, delta);
        return WithCwPriorityCatalog(next, priority);
    }
    private CampaignSourceAuthorityState WithCwPriorityCatalog(CampaignSourceCwPriorityCatalog catalog, CampaignSourcePriorityMotorization priority) =>
        new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces,
            CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans,
            AxisSupplement, Autoblinda, CommonwealthSupplement, priority, AxisPriorityMotorization, AxisGarrisons,
            Battlefield, CairoSetup, CairoWorld, MalettiWorld, IndependentWorld, LocalMapWorld, checked(StateVersion + 1),
            ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld,
            AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld,
            Gt2Correction, catalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, GroupedWorld, septemberPlans: SeptemberPlans);
    internal byte[] SerializeCwPriorityCatalogEvent(CampaignSourceAuthorityState successor, string? id = null, int delta = 0)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", CwPriorityCatalogContractVersion);
            writer.WriteString("eventType", id is null ? "source-cw-priority-catalog-admitted" : "source-cw-priority-catalog-adjusted");
            writer.WriteString("campaignId", Request.CampaignId); writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", id is null ? AdmitCwPriorityCatalogActionId : CwPriorityCatalogActionId(id, delta));
            writer.WriteString("actor", id is null ? "system" : "commonwealth");
            if (id is not null) { writer.WriteString("elementId", id); writer.WriteNumber("delta", delta); }
            writer.WriteString("cwPriorityCatalogHash", successor.CwPriorityCatalog!.Hash());
            writer.WriteString("retainedPriorityHash", successor.PriorityMotorization!.Hash());
            writer.WriteString("cwPriorityCatalogProfileHash", successor.CwPriorityCatalogProfileHash);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal static string Gt2CorrectionActionId(string kind, int lane, int stage, int delta) =>
        CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(FormattableString.Invariant($"graziani.source.gt2-correction.choice.v1\0{kind}\0{lane}\0{stage}\0{delta}")));
    internal CampaignSourceAuthorityState AdmitGt2Correction()
    {
        if (Gt2Correction is not null) throw new InvalidOperationException("GT2 correction already admitted.");
        return WithGt2Correction(CampaignSourceGt2ConvoyCorrection.Create(ConvoyPlan));
    }
    internal CampaignSourceAuthorityState ApplyGt2Correction(string kind, int lane, int stage, int delta)
    {
        var correction = Gt2Correction ?? throw new InvalidOperationException("GT2 correction not admitted.");
        return WithGt2Correction(kind switch
        {
            "adjust" => correction.Adjust(lane, stage, delta),
            "finish" when lane == 0 && stage == 0 && delta == 0 => correction.Finish(),
            _ => throw new InvalidOperationException("Unknown GT2 correction choice."),
        });
    }
    private CampaignSourceAuthorityState WithGt2Correction(CampaignSourceGt2ConvoyCorrection correction) =>
        new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces,
            CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans,
            AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization, AxisPriorityMotorization, AxisGarrisons,
            Battlefield, CairoSetup, CairoWorld, MalettiWorld, IndependentWorld, LocalMapWorld, checked(StateVersion + 1),
            ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld,
            AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, GroupedWorld, septemberPlans: SeptemberPlans);
    internal byte[] SerializeGt2CorrectionEvent(CampaignSourceAuthorityState successor, string? kind = null, int lane = 0, int stage = 0, int delta = 0)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", Gt2CorrectionContractVersion);
            writer.WriteString("eventType", kind is null ? "source-gt2-correction-admitted" : "source-gt2-correction-changed");
            writer.WriteString("campaignId", Request.CampaignId); writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", kind is null ? AdmitGt2CorrectionActionId : Gt2CorrectionActionId(kind, lane, stage, delta));
            writer.WriteString("actor", kind is null ? "system" : "axis");
            if (kind is not null) { writer.WriteString("kind", kind); writer.WriteNumber("lane", lane); writer.WriteNumber("stage", stage); writer.WriteNumber("delta", delta); }
            writer.WriteString("gt2CorrectionHash", successor.Gt2Correction!.Hash());
            writer.WriteString("gt2CorrectionProfileHash", successor.Gt2CorrectionProfileHash);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal static readonly string PromoteMatruhDetachedWorldActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.matruh-detached-world.promote.v1"));
    internal bool CanPromoteMatruhDetachedWorld => MatruhFormation is { Confirmed: true, DetachedId: not null }
        && MatruhDetachedWorld is null && Battlefield is not null && AxisCorpsArtilleryWorld is not null;
    internal CampaignSourceAuthorityState PromoteMatruhDetachedWorld()
    {
        if (!CanPromoteMatruhDetachedWorld) throw new InvalidOperationException("Matruh detached unit is not ready.");
        MatruhFormation!.Validate(this);
        var active = CampaignSourceMatruhDetachedWorld.Create(this);
        var battlefield = Battlefield!.PromoteMatruhDetached(MatruhFormation, World, active.World);
        return new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces,
            CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans,
            AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization, AxisPriorityMotorization, AxisGarrisons,
            battlefield, CairoSetup, CairoWorld, MalettiWorld, IndependentWorld, LocalMapWorld, checked(StateVersion + 1),
            ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld,
            AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, active, Gt2Correction, CwPriorityCatalog,
            CwPriorityWorld is null || CwFreeGunsWorld is not null ? CwPriorityWorld : CampaignSourceCwPriorityWorld.Rebind(CwPriorityWorld, active),
            CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns,
            CwFreeGunsWorld is null ? null : CampaignSourceCwFreeGunsWorld.Rebind(CwFreeGunsWorld, active), GroupedWorld, septemberPlans: SeptemberPlans);
    }
    internal byte[] SerializeMatruhDetachedWorldEvent(CampaignSourceAuthorityState successor)
    {
        var active = successor.MatruhDetachedWorld ?? throw new InvalidOperationException("Matruh detached world missing.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", MatruhDetachedWorldContractVersion);
            writer.WriteString("eventType", "source-matruh-detached-world-promoted"); writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion); writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", PromoteMatruhDetachedWorldActionId); writer.WriteString("actor", "system");
            writer.WriteString("priorProfileHash", MatruhFormationProfileHash);
            writer.WriteString("activeContentHash", active.Content.Hash()); writer.WriteString("activeWorldHash", active.Hash());
            writer.WriteString("battlefieldHash", successor.Battlefield!.Hash()); writer.WriteString("matruhDetachedWorldProfileHash", successor.MatruhDetachedWorldProfileHash);
            if (successor.CwPriorityWorld is not null)
            {
                writer.WriteString("cwPriorityContentHash", successor.CwPriorityWorld.Content.Hash());
                writer.WriteString("cwPriorityWorldHash", successor.CwPriorityWorld.Hash());
            }
            if (successor.CwFreeGunsWorld is not null)
            { writer.WriteString("cwFreeGunsContentHash", successor.CwFreeGunsWorld.Content.Hash()); writer.WriteString("cwFreeGunsWorldHash", successor.CwFreeGunsWorld.Hash()); }
            if (successor.IsMatruhGrouped) writer.WriteString("groupedWorldHash", successor.GroupedWorld!.Hash());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal static string MatruhFormationActionId(string kind, string? id) =>
        CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes("graziani.source.matruh-formation.choice.v1\0" + kind + "\0" + id));
    internal CampaignSourceAuthorityState AdmitMatruhFormation()
    {
        if (MalettiHierarchy is not { Confirmed: true } || Battlefield is null || MatruhFormation is not null)
            throw new InvalidOperationException("Matruh formation requires the prior hierarchy and no duplicate.");
        MalettiHierarchy.Validate(this);
        var group = CampaignSourceMatruhFormation.Create(this);
        return WithMatruhFormation(group, Battlefield.AddMatruhFormation(group));
    }
    internal CampaignSourceAuthorityState ApplyMatruhFormation(string kind, string? id)
    {
        var group = MatruhFormation ?? throw new InvalidOperationException("Matruh formation not admitted.");
        var battlefield = Battlefield ?? throw new InvalidOperationException("Source battlefield missing.");
        var next = kind switch
        {
            "confirm" when id is null => group.Confirm(this),
            "detach" when id is not null => group.Detach(this, id),
            _ => throw new InvalidOperationException("Unknown Matruh formation choice."),
        };
        var nextBattlefield = kind == "confirm" ? battlefield.ConfirmMatruhFormation(group)
            : battlefield.DetachMatruhUnit(group, id!);
        var nextWorld = IsMatruhGrouped && kind == "detach"
            ? GroupedWorld!.DetachMember(CampaignSourceMatruhGroupedDefinition.GroupId, id!, 1, 0) : GroupedWorld;
        return WithMatruhFormation(next, nextBattlefield, nextWorld);
    }
    private CampaignSourceAuthorityState WithMatruhFormation(CampaignSourceMatruhFormation group, CampaignSourceBattlefield battlefield,
        CampaignGroupedWorld? groupedWorld = null) =>
        new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces,
            CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans,
            AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization, AxisPriorityMotorization, AxisGarrisons,
            battlefield, CairoSetup, CairoWorld, MalettiWorld, IndependentWorld, LocalMapWorld, checked(StateVersion + 1),
            ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld,
            AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, group, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, groupedWorld ?? GroupedWorld, septemberPlans: SeptemberPlans);
    internal byte[] SerializeMatruhFormationEvent(CampaignSourceAuthorityState successor, string? kind = null, string? id = null)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", MatruhFormationContractVersion);
            writer.WriteString("eventType", kind is null ? "source-matruh-formation-admitted" : "source-matruh-formation-changed");
            writer.WriteString("campaignId", Request.CampaignId); writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", kind is null ? AdmitMatruhFormationActionId : MatruhFormationActionId(kind, id));
            writer.WriteString("actor", kind is null ? "system" : "commonwealth");
            if (kind is not null) { writer.WriteString("kind", kind); if (id is null) writer.WriteNull("elementId"); else writer.WriteString("elementId", id); }
            writer.WriteString("matruhFormationHash", successor.MatruhFormation!.Hash());
            writer.WriteString("battlefieldHash", successor.Battlefield!.Hash());
            writer.WriteString("matruhFormationProfileHash", successor.MatruhFormationProfileHash);
            if (successor.IsMatruhGrouped) writer.WriteString("groupedWorldHash", successor.GroupedWorld!.Hash());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal static readonly string PromoteCertifiedMalettiWorldActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.certified-maletti-world.promote.v1"));
    internal bool CanPromoteCertifiedMalettiWorld => MalettiHierarchy is { Confirmed: true, DetachedId: not null }
        && CertifiedMalettiWorld is null && Battlefield is not null && AxisPriorityMotorization is not null;
    internal CampaignSourceAuthorityState PromoteCertifiedMalettiWorld()
    {
        if (!CanPromoteCertifiedMalettiWorld) throw new InvalidOperationException("Certified Maletti unit not ready.");
        MalettiHierarchy!.Validate(this);
        var active = CampaignSourceCertifiedMalettiWorld.Create(this);
        var battlefield = Battlefield!.PromoteCertifiedMaletti(MalettiHierarchy, AxisPriorityMotorization!, World, active.World);
        return new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces,
            CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans,
            AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization, AxisPriorityMotorization, AxisGarrisons,
            battlefield, CairoSetup, CairoWorld, MalettiWorld, IndependentWorld, LocalMapWorld, checked(StateVersion + 1),
            ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld, AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, active, MatruhFormation,
            MatruhDetachedWorld is null || CwPriorityWorld is not null ? MatruhDetachedWorld : CampaignSourceMatruhDetachedWorld.Rebind(MatruhDetachedWorld, active),
            Gt2Correction, CwPriorityCatalog, CwPriorityWorld is null || CwFreeGunsWorld is not null ? CwPriorityWorld : CampaignSourceCwPriorityWorld.Rebind(CwPriorityWorld, active),
            CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns,
            CwFreeGunsWorld is null ? null : CampaignSourceCwFreeGunsWorld.Rebind(CwFreeGunsWorld, active), GroupedWorld, septemberPlans: SeptemberPlans);
    }
    internal byte[] SerializeCertifiedMalettiWorldEvent(CampaignSourceAuthorityState successor)
    {
        var active = successor.CertifiedMalettiWorld ?? throw new InvalidOperationException("Certified Maletti world missing.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", CertifiedMalettiWorldContractVersion);
            writer.WriteString("eventType", "source-certified-maletti-world-promoted"); writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion); writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", PromoteCertifiedMalettiWorldActionId); writer.WriteString("actor", "system");
            writer.WriteString("priorProfileHash", MalettiHierarchyProfileHash);
            writer.WriteString("activeContentHash", active.Content.Hash()); writer.WriteString("activeWorldHash", active.Hash());
            writer.WriteString("battlefieldHash", successor.Battlefield!.Hash()); writer.WriteString("certifiedMalettiWorldProfileHash", successor.CertifiedMalettiWorldProfileHash);
            if (successor.MatruhDetachedWorld is not null)
            {
                writer.WriteString("matruhContentHash", successor.MatruhDetachedWorld.Content.Hash());
                writer.WriteString("matruhWorldHash", successor.MatruhDetachedWorld.Hash());
            }
            if (successor.CwPriorityWorld is not null)
            {
                writer.WriteString("cwPriorityContentHash", successor.CwPriorityWorld.Content.Hash());
                writer.WriteString("cwPriorityWorldHash", successor.CwPriorityWorld.Hash());
            }
            if (successor.CwFreeGunsWorld is not null)
            { writer.WriteString("cwFreeGunsContentHash", successor.CwFreeGunsWorld.Content.Hash()); writer.WriteString("cwFreeGunsWorldHash", successor.CwFreeGunsWorld.Hash()); }
            if (successor.IsMalettiGrouped) writer.WriteString("groupedWorldHash", successor.GroupedWorld!.Hash());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal static string MalettiHierarchyActionId(string kind, string? id) =>
        CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes("graziani.source.maletti-hierarchy.choice.v1\0" + kind + "\0" + id));
    internal CampaignSourceAuthorityState AdmitMalettiHierarchy()
    {
        if (AxisCorpsArtilleryWorld is null || MalettiHierarchy is not null)
            throw new InvalidOperationException("Maletti hierarchy requires prior world and no duplicate.");
        return WithMalettiHierarchy(CampaignSourceMalettiHierarchy.Create(this), AxisPriorityMotorization!);
    }
    internal CampaignSourceAuthorityState ApplyMalettiHierarchy(string kind, string? id)
    {
        var hierarchy = MalettiHierarchy ?? throw new InvalidOperationException("Maletti hierarchy not admitted.");
        var priority = AxisPriorityMotorization ?? throw new InvalidOperationException("Axis priority missing.");
        var next = kind switch
        {
            "confirm" when id is null => hierarchy.Confirm(this),
            "detach" when id is not null => hierarchy.Detach(this, id),
            _ => throw new InvalidOperationException("Unknown Maletti hierarchy choice."),
        };
        if (kind == "detach") priority = priority.Detach(id!);
        var grouped = IsMalettiGrouped && kind == "detach"
            ? GroupedWorld!.DetachMember(CampaignSourceMalettiGroupedDefinition.GroupId, id!, 1, 5) : GroupedWorld;
        var battlefield = IsMalettiGrouped && kind == "detach"
            ? Battlefield!.DetachGroupedMaletti(GroupedWorld!, grouped!, hierarchy, next, priority) : Battlefield;
        return WithMalettiHierarchy(next, priority, battlefield, grouped);
    }
    private CampaignSourceAuthorityState WithMalettiHierarchy(CampaignSourceMalettiHierarchy hierarchy, CampaignSourceAxisPriorityMotorization priority,
        CampaignSourceBattlefield? battlefield = null, CampaignGroupedWorld? grouped = null) =>
        new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces,
            CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans,
            AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization, priority, AxisGarrisons,
            battlefield ?? Battlefield, CairoSetup, CairoWorld, MalettiWorld, IndependentWorld, LocalMapWorld, checked(StateVersion + 1),
            ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld, AxisCorpsArtillery, AxisCorpsArtilleryWorld, hierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, grouped ?? GroupedWorld, septemberPlans: SeptemberPlans);
    internal byte[] SerializeMalettiHierarchyEvent(CampaignSourceAuthorityState successor, string? kind = null, string? id = null)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", MalettiHierarchyContractVersion);
            writer.WriteString("eventType", kind is null ? "source-maletti-hierarchy-admitted" : "source-maletti-hierarchy-changed");
            writer.WriteString("campaignId", Request.CampaignId); writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", kind is null ? AdmitMalettiHierarchyActionId : MalettiHierarchyActionId(kind, id));
            writer.WriteString("actor", kind is null ? "system" : "axis");
            if (kind is not null) { writer.WriteString("kind", kind); if (id is null) writer.WriteNull("elementId"); else writer.WriteString("elementId", id); }
            writer.WriteString("malettiHierarchyHash", successor.MalettiHierarchy!.Hash());
            writer.WriteString("axisPriorityHash", successor.AxisPriorityMotorization!.Hash());
            writer.WriteString("malettiHierarchyProfileHash", successor.MalettiHierarchyProfileHash);
            if (successor.IsMalettiGrouped) writer.WriteString("groupedWorldHash", successor.GroupedWorld!.Hash());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal static readonly string PromoteAxisCorpsWorldActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.axis-corps-world.promote.v1"));
    internal bool CanPromoteAxisCorpsWorld => AxisCorpsArtillery is not null && Battlefield is not null
        && AxisFreeArtilleryWorld is not null && AxisCorpsArtillery.Placements.Any(placement =>
            World.World.Elements.All(element => element.ElementId != placement.ElementId));
    internal CampaignSourceAuthorityState PromoteAxisCorpsWorld()
    {
        if (!CanPromoteAxisCorpsWorld) throw new InvalidOperationException("No placed Axis corps guns await activation.");
        var active = CampaignSourceAxisCorpsArtilleryWorld.Create(this);
        var battlefield = Battlefield!.PromoteAxisCorpsArtillery(AxisCorpsArtillery!, World, active.World);
        return new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces,
            CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans,
            AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization, AxisPriorityMotorization, AxisGarrisons,
            battlefield, CairoSetup, CairoWorld, MalettiWorld, IndependentWorld, LocalMapWorld, checked(StateVersion + 1),
            ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld, AxisCorpsArtillery, active, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, GroupedWorld, septemberPlans: SeptemberPlans);
    }
    internal byte[] SerializeAxisCorpsWorldEvent(CampaignSourceAuthorityState successor)
    {
        var active = successor.AxisCorpsArtilleryWorld ?? throw new InvalidOperationException("Axis corps source world missing.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", AxisCorpsWorldContractVersion);
            writer.WriteString("eventType", "source-axis-corps-world-promoted"); writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion); writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", PromoteAxisCorpsWorldActionId); writer.WriteString("actor", "system");
            writer.WriteString("priorProfileHash", AxisCorpsArtilleryWorld is null ? AxisCorpsSetupProfileHash : AxisCorpsWorldProfileHash);
            writer.WriteString("activeContentHash", active.Content.Hash()); writer.WriteString("activeWorldHash", active.Hash());
            writer.WriteString("battlefieldHash", successor.Battlefield!.Hash()); writer.WriteString("axisCorpsWorldProfileHash", successor.AxisCorpsWorldProfileHash);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal static string AxisCorpsPlacementActionId(string id, string locationId) =>
        CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes("graziani.source.axis-corps-setup.place.v1\0" + id + "\0" + locationId));
    internal CampaignSourceAuthorityState AdmitAxisCorpsSetup()
    {
        if (AxisFreeArtilleryWorld is null || Battlefield is null || AxisCorpsArtillery is not null
            || AxisFreeArtillery is null || AxisFreeArtillery.PendingIds.Count != 0 || CanPromoteAxisFreeWorld)
            throw new InvalidOperationException("Axis corps setup requires the previous world and no duplicate.");
        var setup = CampaignSourceAxisCorpsArtillery.Create(Pregame, Battlefield);
        return WithAxisCorpsSetup(setup, Battlefield.AddAxisCorpsArtillery(setup));
    }
    internal CampaignSourceAuthorityState PlaceAxisCorpsUnit(string id, string locationId)
    {
        var setup = AxisCorpsArtillery ?? throw new InvalidOperationException("Axis corps setup not admitted.");
        var battlefield = Battlefield ?? throw new InvalidOperationException("Source battlefield missing.");
        return WithAxisCorpsSetup(setup.Place(id, locationId, battlefield), battlefield.PlaceAxisCorpsArtillery(setup, id, locationId));
    }
    private CampaignSourceAuthorityState WithAxisCorpsSetup(CampaignSourceAxisCorpsArtillery setup, CampaignSourceBattlefield battlefield) =>
        new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces,
            CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans,
            AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization, AxisPriorityMotorization, AxisGarrisons,
            battlefield, CairoSetup, CairoWorld, MalettiWorld, IndependentWorld, LocalMapWorld, checked(StateVersion + 1),
            ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld, setup, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, GroupedWorld, septemberPlans: SeptemberPlans);
    internal byte[] SerializeAxisCorpsSetupEvent(CampaignSourceAuthorityState successor, string? id = null, string? locationId = null)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", AxisCorpsSetupContractVersion);
            writer.WriteString("eventType", id is null ? "source-axis-corps-setup-admitted" : "source-axis-corps-unit-placed");
            writer.WriteString("campaignId", Request.CampaignId); writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", id is null ? AdmitAxisCorpsSetupActionId : AxisCorpsPlacementActionId(id, locationId!));
            writer.WriteString("actor", id is null ? "system" : "axis");
            if (id is not null) { writer.WriteString("elementId", id); writer.WriteString("locationId", locationId); }
            writer.WriteString("axisCorpsSetupHash", successor.AxisCorpsArtillery!.Hash());
            writer.WriteString("battlefieldHash", successor.Battlefield!.Hash());
            writer.WriteString("axisCorpsSetupProfileHash", successor.AxisCorpsSetupProfileHash);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal static readonly string PromoteAxisFreeWorldActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.axis-free-world.promote.v1"));
    internal bool CanPromoteAxisFreeWorld => AxisFreeArtillery is not null && Battlefield is not null
        && ExpandedIndependentWorld is not null && AxisFreeArtillery.Placements.Any(placement =>
            World.World.Elements.All(element => element.ElementId != placement.ElementId));
    internal CampaignSourceAuthorityState PromoteAxisFreeWorld()
    {
        if (!CanPromoteAxisFreeWorld) throw new InvalidOperationException("No placed Axis source guns await activation.");
        var active = CampaignSourceAxisFreeArtilleryWorld.Create(this);
        var battlefield = Battlefield!.PromoteAxisFreeArtillery(AxisFreeArtillery!, World, active.World);
        return new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces,
            CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans,
            AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization, AxisPriorityMotorization, AxisGarrisons,
            battlefield, CairoSetup, CairoWorld, MalettiWorld, IndependentWorld, LocalMapWorld, checked(StateVersion + 1),
            ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, active, AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, GroupedWorld, septemberPlans: SeptemberPlans);
    }
    internal byte[] SerializeAxisFreeWorldEvent(CampaignSourceAuthorityState successor)
    {
        var active = successor.AxisFreeArtilleryWorld ?? throw new InvalidOperationException("Axis source world missing.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", AxisFreeWorldContractVersion);
            writer.WriteString("eventType", "source-axis-free-world-promoted"); writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion); writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", PromoteAxisFreeWorldActionId); writer.WriteString("actor", "system");
            writer.WriteString("priorProfileHash", AxisFreeArtilleryWorld is null ? AxisFreeSetupProfileHash : AxisFreeWorldProfileHash);
            writer.WriteString("activeContentHash", active.Content.Hash()); writer.WriteString("activeWorldHash", active.Hash());
            writer.WriteString("battlefieldHash", successor.Battlefield!.Hash()); writer.WriteString("axisFreeWorldProfileHash", successor.AxisFreeWorldProfileHash);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal static string AxisFreePlacementActionId(string id, string locationId) =>
        CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes("graziani.source.axis-free-setup.place.v1\0" + id + "\0" + locationId));
    internal CampaignSourceAuthorityState AdmitAxisFreeSetup()
    {
        if (InitialWrecks is null || Battlefield is null || AxisFreeArtillery is not null)
            throw new InvalidOperationException("Axis free setup requires prior source setup and no duplicate.");
        var setup = CampaignSourceAxisFreeArtillery.Create(Pregame, Battlefield);
        return WithAxisFreeSetup(setup, Battlefield.AddAxisFreeArtillery(setup));
    }
    internal CampaignSourceAuthorityState PlaceAxisFreeUnit(string id, string locationId)
    {
        var setup = AxisFreeArtillery ?? throw new InvalidOperationException("Axis free setup not admitted.");
        var battlefield = Battlefield ?? throw new InvalidOperationException("Source battlefield missing.");
        return WithAxisFreeSetup(setup.Place(id, locationId, battlefield), battlefield.PlaceAxisFreeArtillery(setup, id, locationId));
    }
    private CampaignSourceAuthorityState WithAxisFreeSetup(CampaignSourceAxisFreeArtillery setup, CampaignSourceBattlefield battlefield) =>
        new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces,
            CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans,
            AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization, AxisPriorityMotorization, AxisGarrisons,
            battlefield, CairoSetup, CairoWorld, MalettiWorld, IndependentWorld, LocalMapWorld, checked(StateVersion + 1),
            ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, setup, AxisFreeArtilleryWorld, AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, GroupedWorld, septemberPlans: SeptemberPlans);
    internal byte[] SerializeAxisFreeSetupEvent(CampaignSourceAuthorityState successor, string? id = null, string? locationId = null)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", AxisFreeSetupContractVersion);
            writer.WriteString("eventType", id is null ? "source-axis-free-setup-admitted" : "source-axis-free-unit-placed");
            writer.WriteString("campaignId", Request.CampaignId); writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", id is null ? AdmitAxisFreeSetupActionId : AxisFreePlacementActionId(id, locationId!));
            writer.WriteString("actor", id is null ? "system" : "axis");
            if (id is not null) { writer.WriteString("elementId", id); writer.WriteString("locationId", locationId); }
            writer.WriteString("axisFreeSetupHash", successor.AxisFreeArtillery!.Hash());
            writer.WriteString("battlefieldHash", successor.Battlefield!.Hash());
            writer.WriteString("axisFreeSetupProfileHash", successor.AxisFreeSetupProfileHash);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal static string InitialWreckPlacementActionId(string id, string locationId) =>
        CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes("graziani.source.initial-wrecks.place.v1\0" + id + "\0" + locationId));
    internal CampaignSourceAuthorityState AdmitInitialWrecks()
    {
        if (AttachedInitialLoads is null || InitialWrecks is not null) throw new InvalidOperationException("Initial wrecks require prior setup and no duplicate.");
        return WithInitialWrecks(CampaignSourceInitialWrecks.Create(Pregame));
    }
    internal CampaignSourceAuthorityState PlaceInitialWreck(string id, string locationId) =>
        WithInitialWrecks((InitialWrecks ?? throw new InvalidOperationException("Initial wrecks not admitted.")).Place(id, locationId));
    private CampaignSourceAuthorityState WithInitialWrecks(CampaignSourceInitialWrecks wrecks) =>
        new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces,
            CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans,
            AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization, AxisPriorityMotorization, AxisGarrisons,
            Battlefield, CairoSetup, CairoWorld, MalettiWorld, IndependentWorld, LocalMapWorld, checked(StateVersion + 1),
            ExpandedIndependentWorld, AttachedInitialLoads, wrecks, AxisFreeArtillery, AxisFreeArtilleryWorld, AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, GroupedWorld, septemberPlans: SeptemberPlans);
    internal byte[] SerializeInitialWreckEvent(CampaignSourceAuthorityState successor, string? id = null, string? locationId = null)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", InitialWrecksContractVersion);
            writer.WriteString("eventType", id is null ? "source-initial-wrecks-admitted" : "source-initial-wreck-placed");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", id is null ? AdmitInitialWrecksActionId : InitialWreckPlacementActionId(id, locationId!));
            writer.WriteString("actor", id is null ? "system" : "commonwealth");
            if (id is not null) { writer.WriteString("id", id); writer.WriteString("locationId", locationId); }
            writer.WriteString("initialWrecksHash", successor.InitialWrecks!.Hash());
            writer.WriteString("initialWrecksProfileHash", successor.InitialWrecksProfileHash);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal CampaignSourceAuthorityState AdmitAttachedInitialLoads()
    {
        if (ExpandedIndependentWorld is null || AttachedInitialLoads is not null || PriorityMotorization is null)
            throw new InvalidOperationException("Initial loads require the expanded source world and priority setup.");
        return new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces,
            CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans,
            AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization, AxisPriorityMotorization, AxisGarrisons,
            Battlefield, CairoSetup, CairoWorld, MalettiWorld, IndependentWorld, LocalMapWorld, checked(StateVersion + 1),
            ExpandedIndependentWorld, CampaignSourceAttachedInitialLoads.Create(), septemberPlans: SeptemberPlans);
    }

    internal static string AttachedInitialLoadsActionId(string kind, string elementId) =>
        CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes("graziani.source.attached-initial-loads.choice.v1\0" + kind + "\0" + elementId));

    internal CampaignSourceAuthorityState ApplyAttachedInitialLoads(string kind, string elementId)
    {
        var loads = AttachedInitialLoads ?? throw new InvalidOperationException("Initial loads are not admitted.");
        var next = kind switch
        {
            "attach" => loads.Attach(elementId, PriorityMotorization!, CommonwealthMotorization),
            "detach" => loads.Detach(elementId),
            "ammo" or "fuel" => loads.Load(elementId, kind),
            "unload-ammo" => loads.ClearLoad(elementId, "ammo"),
            "unload-fuel" => loads.ClearLoad(elementId, "fuel"),
            _ => throw new InvalidOperationException("Unknown initial load choice."),
        };
        return new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces,
            CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans,
            AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization, AxisPriorityMotorization, AxisGarrisons,
            Battlefield, CairoSetup, CairoWorld, MalettiWorld, IndependentWorld, LocalMapWorld, checked(StateVersion + 1),
            ExpandedIndependentWorld, next, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld, AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, GroupedWorld, septemberPlans: SeptemberPlans);
    }

    internal byte[] SerializeAttachedInitialLoadsEvent(CampaignSourceAuthorityState successor, string? kind = null, string? elementId = null)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", AttachedInitialLoadsContractVersion);
            writer.WriteString("eventType", kind is null ? "source-attached-initial-loads-admitted" : "source-attached-initial-loads-changed");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", kind is null ? AdmitAttachedInitialLoadsActionId : AttachedInitialLoadsActionId(kind, elementId!));
            writer.WriteString("actor", kind is null ? "system" : "commonwealth");
            if (kind is not null) { writer.WriteString("kind", kind); writer.WriteString("elementId", elementId); }
            writer.WriteString("attachedInitialLoadsHash", successor.AttachedInitialLoads!.Hash());
            writer.WriteString("attachedInitialLoadsProfileHash", successor.AttachedInitialLoadsProfileHash);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static readonly string PromoteExpandedIndependentActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.expanded-independent-world.promote.v1"));
    internal bool CanPromoteExpandedIndependent => LocalMapWorld is not null && Battlefield is not null
        && AxisPriorityMotorization is not null && AxisSupplement is not null && CommonwealthSupplement is not null
        && ContentSourceIndependentPackArtifact.Units.Concat(ContentSourceExpandedIndependentPackArtifact.AddedUnits)
            .Any(unit => (unit.SideId == "commonwealth" || AxisPriorityMotorization.DetachedTargets.Contains(unit.ElementId, StringComparer.Ordinal))
                && World.World.Elements.All(element => element.ElementId != unit.ElementId));

    internal CampaignSourceAuthorityState PromoteExpandedIndependent()
    {
        if (!CanPromoteExpandedIndependent) throw new InvalidOperationException("No expanded source units are ready for promotion.");
        var active = CampaignSourceExpandedIndependentWorld.Create(this);
        var battlefield = Battlefield!.PromoteSourceIndependentV2(CommonwealthSupplement!, AxisSupplement!,
            AxisPriorityMotorization!, World, active.World);
        return new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces,
            CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans,
            AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization, AxisPriorityMotorization, AxisGarrisons,
            battlefield, CairoSetup, CairoWorld, MalettiWorld, IndependentWorld, LocalMapWorld, checked(StateVersion + 1), active, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld, AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, GroupedWorld, septemberPlans: SeptemberPlans);
    }

    internal byte[] SerializeExpandedIndependentPromotion(CampaignSourceAuthorityState successor)
    {
        var active = successor.ExpandedIndependentWorld ?? throw new InvalidOperationException("Missing expanded source world.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ExpandedIndependentWorldContractVersion);
            writer.WriteString("eventType", "source-expanded-independent-world-promoted");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", PromoteExpandedIndependentActionId);
            writer.WriteString("actor", "system");
            writer.WriteString("priorProfileHash", ExpandedIndependentWorld is null ? LocalMapWorldProfileHash : ExpandedIndependentWorldProfileHash);
            writer.WriteString("activeContentHash", active.Content.Hash());
            writer.WriteString("activeWorldHash", active.Hash());
            writer.WriteString("battlefieldHash", successor.Battlefield!.Hash());
            writer.WriteString("expandedIndependentWorldProfileHash", successor.ExpandedIndependentWorldProfileHash);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal CampaignSourceAuthorityState AdmitLocalMap()
    {
        if (IndependentWorld is null || LocalMapWorld is not null)
            throw new InvalidOperationException("Local source map requires the prior independent world and no duplicate.");
        var map = CampaignSourceLocalMapWorld.Create(this);
        return new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces,
            CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans,
            AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization, AxisPriorityMotorization, AxisGarrisons,
            Battlefield, CairoSetup, CairoWorld, MalettiWorld, IndependentWorld, map, checked(StateVersion + 1), ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld, AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, GroupedWorld, septemberPlans: SeptemberPlans);
    }

    internal byte[] SerializeLocalMapAdmission(CampaignSourceAuthorityState successor)
    {
        var map = successor.LocalMapWorld ?? throw new InvalidOperationException("Missing local source map.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", LocalMapWorldContractVersion);
            writer.WriteString("eventType", "source-local-map-admitted");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", AdmitLocalMapActionId);
            writer.WriteString("actor", "system");
            writer.WriteString("priorProfileHash", IndependentWorldProfileHash);
            writer.WriteString("activeContentHash", map.Content.Hash());
            writer.WriteString("activeWorldHash", map.Hash());
            writer.WriteString("localMapWorldProfileHash", successor.LocalMapWorldProfileHash);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static string AutoblindaActionId(string kind, string target) =>
        CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes("graziani.setup.autoblinda.v1\0" + kind + "\0" + target));

    internal byte[] SerializeAutoblindaEvent(CampaignSourceAuthorityState successor, string kind, string target)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", SupplementalContractVersion);
            writer.WriteString("eventType", "source-autoblinda-choice");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", AutoblindaActionId(kind, target));
            writer.WriteString("actor", "axis");
            writer.WriteString("choice", kind);
            writer.WriteString("target", target);
            writer.WriteString("autoblindaSetupHash", successor.Autoblinda!.Hash());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal byte[] SerializeAxisSupplementEvent(CampaignSourceAuthorityState successor)
    {
        var supplement = successor.AxisSupplement ?? throw new InvalidOperationException("Missing Axis source supplement.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", SupplementalContractVersion);
            writer.WriteString("eventType", "source-axis-force-supplement-admitted");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", AdmitSupplementActionId);
            writer.WriteString("actor", "system");
            writer.WriteString("priorAuthoritySetupHash", AuthoritySetupHash);
            writer.WriteString("supplementHash", supplement.Hash());
            writer.WriteString("supplementalProfileHash", successor.SupplementalProfileHash);
            writer.WriteString("autoblindaSetupHash", successor.Autoblinda!.Hash());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static string CommonwealthSupplyPlanActionId(string kind, int targetTurn) =>
        CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(
            "graziani.setup.cw-supply.v1\0" + kind + "\0" + targetTurn.ToString(System.Globalization.CultureInfo.InvariantCulture)));

    internal byte[] SerializeCommonwealthSupplyPlanEvent(CampaignSourceAuthorityState successor, string kind, int targetTurn)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "source-commonwealth-supply-planned");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", CommonwealthSupplyPlanActionId(kind, targetTurn));
            writer.WriteString("actor", "commonwealth");
            writer.WriteString("choice", kind);
            writer.WriteNumber("targetGameTurn", targetTurn);
            writer.WriteNumber("randomCursorBefore", RandomState.NextByteCursor);
            writer.WriteNumber("randomCursorAfter", successor.RandomState.NextByteCursor);
            writer.WriteString("commonwealthSupplyPlansHash", successor.CommonwealthSupplyPlans!.Hash());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static string MotorizationActionId(bool axis, string poolId, string locationId, int delta) =>
        CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(
            "graziani.setup.motorization.v1\0" + (axis ? "axis" : "commonwealth") + "\0" + poolId + "\0" + locationId + "\0"
            + delta.ToString(System.Globalization.CultureInfo.InvariantCulture)));

    internal byte[] SerializeMotorizationEvent(CampaignSourceAuthorityState successor, bool axis, string poolId, string locationId, int delta)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "source-motorization-adjusted");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", MotorizationActionId(axis, poolId, locationId, delta));
            writer.WriteString("actor", axis ? "axis" : "commonwealth");
            writer.WriteString("poolId", poolId);
            writer.WriteString("locationId", locationId);
            writer.WriteNumber("delta", delta);
            writer.WriteString("motorizationHash", axis ? successor.AxisMotorization.Hash() : successor.CommonwealthMotorization.Hash());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static string DummyActionId(bool axis, string dummyId, string? locationId) =>
        CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(
            "graziani.setup.dummy-choice.v1\0" + (axis ? "axis" : "commonwealth") + "\0" + dummyId + "\0" + (locationId ?? "omit")));

    internal byte[] SerializeDummyEvent(CampaignSourceAuthorityState successor, bool axis, string dummyId, string? locationId)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "source-dummy-group-decided");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", DummyActionId(axis, dummyId, locationId));
            writer.WriteString("actor", axis ? "axis" : "commonwealth");
            writer.WriteString("dummyId", dummyId);
            if (locationId is null) writer.WriteNull("locationId");
            else writer.WriteString("locationId", locationId);
            writer.WriteString("supplyWorldHash", axis ? successor.AxisSupplyWorld.Hash() : successor.CommonwealthSupplyWorld.Hash());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static string CommonwealthSupplyActionId(string placementId, string locationId) =>
        CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(
            "graziani.commonwealth-setup.place-supply.v1\0" + placementId + "\0" + locationId));

    internal byte[] SerializeCommonwealthSupplyEvent(CampaignSourceAuthorityState successor, string placementId, string locationId)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "source-commonwealth-supply-placed");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", CommonwealthSupplyActionId(placementId, locationId));
            writer.WriteString("actor", "commonwealth");
            writer.WriteString("placementId", placementId);
            writer.WriteString("locationId", locationId);
            writer.WriteString("commonwealthSupplyWorldHash", successor.CommonwealthSupplyWorld.Hash());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static string DumpActionId(string dumpId, string locationId) =>
        CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(
            "graziani.axis-setup.place-dump.v1\0" + dumpId + "\0" + locationId));

    internal byte[] SerializeDumpEvent(CampaignSourceAuthorityState successor, string dumpId, string locationId)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "source-axis-supply-dump-placed");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", DumpActionId(dumpId, locationId));
            writer.WriteString("actor", "axis");
            writer.WriteString("dumpId", dumpId);
            writer.WriteString("locationId", locationId);
            writer.WriteString("axisSupplyWorldHash", successor.AxisSupplyWorld.Hash());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal byte[] SerializeEvent()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("eventType", "source-authority-started");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", Pregame.StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(Pregame.SerializeCheckpoint()));
            writer.WriteString("actionId", FinishActionId);
            writer.WriteString("actor", "axis");
            writer.WriteString("contentHash", Content.Identity.Hash);
            writer.WriteString("authorityPolicyId", AuthorityPolicyId);
            writer.WriteString("authoritySetupHash", AuthoritySetupHash);
            writer.WriteString("convoyPlanHash", ConvoyPlan.Hash());
            writer.WriteString("axisSupplyWorldHash", AxisSupplyWorld.Hash());
            writer.WriteString("commonwealthSupplyWorldHash", CommonwealthSupplyWorld.Hash());
            writer.WriteString("axisInitialForcesHash", AxisInitialForces.Hash());
            writer.WriteString("commonwealthInitialForcesHash", CommonwealthInitialForces.Hash());
            writer.WriteString("axisMotorizationHash", AxisMotorization.Hash());
            writer.WriteString("commonwealthMotorizationHash", CommonwealthMotorization.Hash());
            writer.WriteString("openingConvoyPlanHash", OpeningConvoyPlan.Hash());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal byte[] SerializeCheckpoint() => CompactCheckpoint ? SerializeCompactCheckpoint() : SerializeLegacyCheckpoint();

    private byte[] SerializeCompactCheckpoint()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", CompactCheckpointContractVersion);
            writer.WriteString("campaignId", Request.CampaignId); writer.WriteNumber("stateVersion", StateVersion);
            writer.WriteString("rulesetHash", Request.RulesetHash); writer.WriteString("setupHash", Request.SetupHash);
            writer.WriteString("authoritySetupHash", AuthoritySetupHash); writer.WriteString("positionId", SetupPositionId);
            writer.WriteString("status", "setup-in-progress"); writer.WriteNull("gameClock");
            writer.WriteString("reconstruction", "retained-genesis-and-complete-event-journal-required");
            writer.WriteNumber("legacyProjectionVersion", LegacyProfileVersion);
            writer.WriteString("legacyProjectionHash", CampaignPregameSourceCreation.Digest(SerializeLegacyCheckpoint()));
            writer.WriteString("activeContentHash", GroupedWorld?.ContentHash ?? World.ContentIdentity.Hash);
            writer.WriteString("activeWorldHash", GroupedWorld?.Hash() ?? CwFreeGunsWorld?.Hash() ?? CwPriorityWorld?.Hash() ?? MatruhDetachedWorld?.Hash()
                ?? CertifiedMalettiWorld?.Hash() ?? AxisCorpsArtilleryWorld?.Hash() ?? AxisFreeArtilleryWorld?.Hash()
                ?? ExpandedIndependentWorld?.Hash() ?? LocalMapWorld?.Hash() ?? IndependentWorld?.Hash()
                ?? MalettiWorld?.Hash() ?? CairoWorld?.Hash() ?? CampaignPregameSourceCreation.Digest(Pregame.Source.InitialSnapshotBytes));
            writer.WriteNumber("randomContractVersion", RandomState.ContractVersion);
            writer.WriteString("randomAlgorithmId", RandomState.AlgorithmId);
            writer.WriteNumber("randomSeed", RandomState.Seed); writer.WriteNumber("randomCursor", RandomState.NextByteCursor);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    private byte[] SerializeLegacyCheckpoint()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", LegacyProfileVersion);
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("stateVersion", StateVersion);
            writer.WriteString("rulesetHash", Request.RulesetHash);
            writer.WriteString("contentHash", Content.Identity.Hash);
            writer.WriteString("setupHash", Request.SetupHash);
            writer.WriteString("authorityPolicyId", AuthorityPolicyId);
            writer.WriteString("authoritySetupHash", AuthoritySetupHash);
            writer.WriteString("positionId", SetupPositionId);
            writer.WriteString("status", "setup-in-progress");
            writer.WriteNull("gameClock");
            writer.WriteBase64String("pregameCheckpointBase64", Pregame.SerializeCheckpoint());
            writer.WriteBase64String("convoyPlanBase64", ConvoyPlan.SerializeCanonical());
            writer.WriteBase64String("axisSupplyWorldBase64", AxisSupplyWorld.SerializeCanonical());
            writer.WriteBase64String("commonwealthSupplyWorldBase64", CommonwealthSupplyWorld.SerializeCanonical());
            writer.WriteBase64String("axisInitialForcesBase64", AxisInitialForces.SerializeCanonical());
            writer.WriteBase64String("commonwealthInitialForcesBase64", CommonwealthInitialForces.SerializeCanonical());
            writer.WriteBase64String("axisMotorizationBase64", AxisMotorization.SerializeCanonical());
            writer.WriteBase64String("commonwealthMotorizationBase64", CommonwealthMotorization.SerializeCanonical());
            writer.WriteBase64String("openingConvoyPlanBase64", OpeningConvoyPlan.SerializeCanonical());
            if (CommonwealthSupplyPlans is null) writer.WriteNull("commonwealthSupplyPlansBase64");
            else writer.WriteBase64String("commonwealthSupplyPlansBase64", CommonwealthSupplyPlans.SerializeCanonical());
            if (SeptemberPlans is not null) writer.WriteBase64String("septemberSupplyPlansBase64", SeptemberPlans.SerializeCanonical());
            writer.WriteBase64String("sourceSnapshotBase64", Pregame.Source.InitialSnapshotBytes);
            if (AxisSupplement is not null)
            {
                writer.WriteString("axisSupplementHash", AxisSupplement.Hash());
                writer.WriteBase64String("axisSupplementBase64", AxisSupplement.SerializeCanonical());
                writer.WriteString("supplementalProfileHash", SupplementalProfileHash);
                writer.WriteBase64String("autoblindaSetupBase64", Autoblinda!.SerializeCanonical());
            }
            if (CommonwealthSupplement is not null)
            {
                writer.WriteString("commonwealthSupplementHash", CommonwealthSupplement.Hash());
                writer.WriteBase64String("commonwealthSupplementBase64", CommonwealthSupplement.SerializeCanonical());
                writer.WriteString("commonwealthSupplementalProfileHash", CommonwealthSupplementalProfileHash);
            }
            if (PriorityMotorization is not null)
            {
                writer.WriteString("priorityMotorizationProfileHash", PriorityMotorizationProfileHash);
                writer.WriteBase64String("priorityMotorizationBase64", PriorityMotorization.SerializeCanonical());
            }
            if (AxisPriorityMotorization is not null)
            {
                writer.WriteString("axisPriorityProfileHash", AxisPriorityMotorizationProfileHash);
                writer.WriteBase64String("axisPriorityMotorizationBase64", AxisPriorityMotorization.SerializeCanonical());
            }
            if (AxisGarrisons is not null)
            {
                writer.WriteString("axisGarrisonsProfileHash", AxisGarrisonsProfileHash);
                writer.WriteBase64String("axisGarrisonsBase64", AxisGarrisons.SerializeCanonical());
            }
            if (Battlefield is not null)
            {
                writer.WriteString("battlefieldProfileHash", BattlefieldProfileHash);
                writer.WriteBase64String("battlefieldBase64", Battlefield.SerializeCanonical());
            }
            if (CairoSetup is not null)
            {
                writer.WriteString("cairoSetupProfileHash", CairoSetupProfileHash);
                writer.WriteBase64String("cairoSetupBase64", CairoSetup.SerializeCanonical());
            }
            if (CairoWorld is not null)
            {
                writer.WriteString("cairoWorldProfileHash", CairoWorldProfileHash);
                writer.WriteString("activeContentHash", CairoWorld.Content.Hash());
                writer.WriteBase64String("activeContentBase64", CairoWorld.Content.CanonicalBytes);
                writer.WriteBase64String("activeWorldBase64", CairoWorld.SerializeCanonical());
            }
            if (MalettiWorld is not null)
            {
                writer.WriteString("malettiWorldProfileHash", MalettiWorldProfileHash);
                writer.WriteString("malettiActiveContentHash", MalettiWorld.Content.Hash());
                writer.WriteBase64String("malettiActiveContentBase64", MalettiWorld.Content.CanonicalBytes);
                writer.WriteBase64String("malettiActiveWorldBase64", MalettiWorld.SerializeCanonical());
            }
            if (IndependentWorld is not null)
            {
                writer.WriteString("independentWorldProfileHash", IndependentWorldProfileHash);
                writer.WriteString("independentActiveContentHash", IndependentWorld.Content.Hash());
                writer.WriteBase64String("independentActiveContentBase64", IndependentWorld.Content.CanonicalBytes);
                writer.WriteBase64String("independentActiveWorldBase64", IndependentWorld.SerializeCanonical());
            }
            if (LocalMapWorld is not null)
            {
                writer.WriteString("localMapWorldProfileHash", LocalMapWorldProfileHash);
                writer.WriteString("localMapActiveContentHash", LocalMapWorld.Content.Hash());
                writer.WriteBase64String("localMapActiveContentBase64", LocalMapWorld.Content.CanonicalBytes);
                writer.WriteBase64String("localMapActiveWorldBase64", LocalMapWorld.SerializeCanonical());
            }
            if (ExpandedIndependentWorld is not null)
            {
                writer.WriteString("expandedIndependentWorldProfileHash", ExpandedIndependentWorldProfileHash);
                writer.WriteString("expandedIndependentActiveContentHash", ExpandedIndependentWorld.Content.Hash());
                writer.WriteBase64String("expandedIndependentActiveContentBase64", ExpandedIndependentWorld.Content.CanonicalBytes);
                writer.WriteBase64String("expandedIndependentActiveWorldBase64", ExpandedIndependentWorld.SerializeCanonical());
            }
            if (AttachedInitialLoads is not null)
            {
                writer.WriteString("attachedInitialLoadsProfileHash", AttachedInitialLoadsProfileHash);
                writer.WriteBase64String("attachedInitialLoadsBase64", AttachedInitialLoads.SerializeCanonical());
            }
            if (InitialWrecks is not null)
            {
                writer.WriteString("initialWrecksProfileHash", InitialWrecksProfileHash);
                writer.WriteBase64String("initialWrecksBase64", InitialWrecks.SerializeCanonical());
            }
            if (AxisFreeArtillery is not null)
            {
                writer.WriteString("axisFreeSetupProfileHash", AxisFreeSetupProfileHash);
                writer.WriteBase64String("axisFreeSetupBase64", AxisFreeArtillery.SerializeCanonical());
            }
            if (AxisFreeArtilleryWorld is not null)
            {
                writer.WriteString("axisFreeWorldProfileHash", AxisFreeWorldProfileHash);
                writer.WriteString("axisFreeWorldContentHash", AxisFreeArtilleryWorld.Content.Hash());
                writer.WriteBase64String("axisFreeWorldContentBase64", AxisFreeArtilleryWorld.Content.CanonicalBytes);
                writer.WriteBase64String("axisFreeWorldBase64", AxisFreeArtilleryWorld.SerializeCanonical());
            }
            if (AxisCorpsArtillery is not null)
            {
                writer.WriteString("axisCorpsSetupProfileHash", AxisCorpsSetupProfileHash);
                writer.WriteBase64String("axisCorpsSetupBase64", AxisCorpsArtillery.SerializeCanonical());
            }
            if (AxisCorpsArtilleryWorld is not null)
            {
                writer.WriteString("axisCorpsWorldProfileHash", AxisCorpsWorldProfileHash);
                writer.WriteString("axisCorpsWorldContentHash", AxisCorpsArtilleryWorld.Content.Hash());
                writer.WriteBase64String("axisCorpsWorldContentBase64", AxisCorpsArtilleryWorld.Content.CanonicalBytes);
                writer.WriteBase64String("axisCorpsWorldBase64", AxisCorpsArtilleryWorld.SerializeCanonical());
            }
            if (MalettiHierarchy is not null)
            {
                writer.WriteString("malettiHierarchyProfileHash", MalettiHierarchyProfileHash);
                writer.WriteBase64String("malettiHierarchyBase64", MalettiHierarchy.SerializeCanonical());
            }
            if (CertifiedMalettiWorld is not null)
            {
                writer.WriteString("certifiedMalettiWorldProfileHash", CertifiedMalettiWorldProfileHash);
                writer.WriteString("certifiedMalettiWorldContentHash", CertifiedMalettiWorld.Content.Hash());
                writer.WriteBase64String("certifiedMalettiWorldContentBase64", CertifiedMalettiWorld.Content.CanonicalBytes);
                writer.WriteBase64String("certifiedMalettiWorldBase64", CertifiedMalettiWorld.SerializeCanonical());
            }
            if (MatruhFormation is not null)
            {
                writer.WriteString("matruhFormationProfileHash", MatruhFormationProfileHash);
                writer.WriteBase64String("matruhFormationBase64", MatruhFormation.SerializeCanonical());
            }
            if (MatruhDetachedWorld is not null)
            {
                writer.WriteString("matruhDetachedWorldProfileHash", MatruhDetachedWorldProfileHash);
                writer.WriteString("matruhDetachedWorldContentHash", MatruhDetachedWorld.Content.Hash());
                writer.WriteBase64String("matruhDetachedWorldContentBase64", MatruhDetachedWorld.Content.CanonicalBytes);
                writer.WriteBase64String("matruhDetachedWorldBase64", MatruhDetachedWorld.SerializeCanonical());
            }
            if (Gt2Correction is not null)
            {
                writer.WriteString("gt2CorrectionProfileHash", Gt2CorrectionProfileHash);
                writer.WriteBase64String("gt2CorrectionBase64", Gt2Correction.SerializeCanonical());
            }
            if (CwPriorityCatalog is not null)
            {
                writer.WriteString("cwPriorityCatalogProfileHash", CwPriorityCatalogProfileHash);
                writer.WriteBase64String("cwPriorityCatalogBase64", CwPriorityCatalog.SerializeCanonical());
            }
            if (CwPriorityWorld is not null)
            {
                writer.WriteString("cwPriorityWorldProfileHash", CwPriorityWorldProfileHash);
                writer.WriteString("cwPriorityWorldContentHash", CwPriorityWorld.Content.Hash());
                writer.WriteBase64String("cwPriorityWorldContentBase64", CwPriorityWorld.Content.CanonicalBytes);
                writer.WriteBase64String("cwPriorityWorldBase64", CwPriorityWorld.SerializeCanonical());
            }
            if (AxisUnattachedCargo is not null || CommonwealthUnattachedCargo is not null)
            {
                writer.WriteString("unattachedCargoProfileHash", UnattachedCargoProfileHash);
                if (AxisUnattachedCargo is null) writer.WriteNull("axisUnattachedCargoBase64"); else writer.WriteBase64String("axisUnattachedCargoBase64", AxisUnattachedCargo.SerializeCanonical());
                if (CommonwealthUnattachedCargo is null) writer.WriteNull("commonwealthUnattachedCargoBase64"); else writer.WriteBase64String("commonwealthUnattachedCargoBase64", CommonwealthUnattachedCargo.SerializeCanonical());
            }
            if (GroupedWorld is not null)
            {
                var fourthContent = ContentSourceFourthIndianPackArtifact.Create(CwFreeGunsWorld!.Content);
                var matruhContent = IsMatruhGrouped ? ContentSourceMatruhGroupedPackArtifact.Create(fourthContent) : null;
                var malettiContent = IsMalettiGrouped ? ContentSourceMalettiGroupedPackArtifact.Create(matruhContent!) : null;
                var libyanContent = IsLibyanGrouped ? ContentSourceLibyanGroupedPackArtifact.Create(malettiContent!) : null;
                var italianContent = IsItalianDivisionsGrouped ? ContentSourceCireneMarmaricaGroupedPackArtifact.Create(libyanContent!) : null;
                var hash = italianContent?.Hash() ?? libyanContent?.Hash() ?? malettiContent?.Hash() ?? matruhContent?.Hash() ?? fourthContent.Hash();
                if (hash != GroupedWorld.ContentHash) throw new InvalidOperationException("Grouped source content identity differs.");
                writer.WriteString("groupedWorldContentHash", hash); writer.WriteBase64String("groupedWorldContent", italianContent?.CanonicalBytes ?? libyanContent?.CanonicalBytes ?? malettiContent?.CanonicalBytes ?? matruhContent?.CanonicalBytes ?? fourthContent.CanonicalBytes);
                writer.WriteBase64String("groupedWorld", GroupedWorld.SerializeCanonical());
            }
            if (CwFreeGuns is not null)
            {
                writer.WriteString("cwFreeGunsProfileHash", CwFreeGunsProfileHash); writer.WriteBase64String("cwFreeGunsBase64", CwFreeGuns.SerializeCanonical());
            }
            if (CwFreeGunsWorld is not null)
            {
                writer.WriteString("cwFreeGunsWorldProfileHash", CwFreeGunsWorldProfileHash); writer.WriteString("cwFreeGunsWorldContentHash", CwFreeGunsWorld.Content.Hash());
                writer.WriteBase64String("cwFreeGunsWorldContentBase64", CwFreeGunsWorld.Content.CanonicalBytes); writer.WriteBase64String("cwFreeGunsWorldBase64", CwFreeGunsWorld.SerializeCanonical());
            }
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
}
