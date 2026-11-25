using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// Versioned successor of the retained, source-backed pregame.  Setup is still
// in progress; there is no game clock until setup obligations are satisfied.
internal sealed partial record CampaignSourceAuthorityState
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
    internal const int FrontlineAttachedInitialLoadsEventContractVersion = 64;
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
          CampaignSourceLocalMapWorld? localMapWorld, long stateVersion, CampaignSourceExpandedIndependentWorld? expandedIndependentWorld = null, CampaignSourceAttachedInitialLoads? attachedInitialLoads = null, CampaignSourceInitialWrecks? initialWrecks = null, CampaignSourceAxisFreeArtillery? axisFreeArtillery = null, CampaignSourceAxisFreeArtilleryWorld? axisFreeArtilleryWorld = null, CampaignSourceAxisCorpsArtillery? axisCorpsArtillery = null, CampaignSourceAxisCorpsArtilleryWorld? axisCorpsArtilleryWorld = null, CampaignSourceMalettiHierarchy? malettiHierarchy = null, CampaignSourceCertifiedMalettiWorld? certifiedMalettiWorld = null, CampaignSourceMatruhFormation? matruhFormation = null, CampaignSourceMatruhDetachedWorld? matruhDetachedWorld = null, CampaignSourceGt2ConvoyCorrection? gt2Correction = null, CampaignSourceCwPriorityCatalog? cwPriorityCatalog = null, CampaignSourceCwPriorityWorld? cwPriorityWorld = null, bool compactCheckpoint = false, CampaignSourceUnattachedCargo? axisUnattachedCargo = null, CampaignSourceUnattachedCargo? commonwealthUnattachedCargo = null, CampaignSourceCwFreeGuns? cwFreeGuns = null, CampaignSourceCwFreeGunsWorld? cwFreeGunsWorld = null, CampaignGroupedWorld? groupedWorld = null, CampaignSourceSeptemberConvoyPlans? septemberPlans = null, int finalizedOwners = 0, bool finalizationVersion = false, int horseRulingVotes = 0, bool horseStageOneNoTraining = false, bool horseRulingVersion = false)
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
        SeptemberPlans = septemberPlans;
        AxisSupplement = axisSupplement;
        Autoblinda = autoblinda;
        CommonwealthSupplement = commonwealthSupplement;
        PriorityMotorization = priorityMotorization;
        AxisPriorityMotorization = axisPriorityMotorization;
        AxisGarrisons = axisGarrisons;
        Battlefield = battlefield;
        CairoSetup = cairoSetup;
        CairoWorld = cairoWorld;
        MalettiWorld = malettiWorld;
        IndependentWorld = independentWorld;
        LocalMapWorld = localMapWorld;
        ExpandedIndependentWorld = expandedIndependentWorld;
        AttachedInitialLoads = attachedInitialLoads;
        InitialWrecks = initialWrecks;
        AxisFreeArtillery = axisFreeArtillery;
        AxisFreeArtilleryWorld = axisFreeArtilleryWorld;
        AxisCorpsArtillery = axisCorpsArtillery;
        AxisCorpsArtilleryWorld = axisCorpsArtilleryWorld;
        MalettiHierarchy = malettiHierarchy;
        CertifiedMalettiWorld = certifiedMalettiWorld;
        MatruhFormation = matruhFormation;
        MatruhDetachedWorld = matruhDetachedWorld;
        Gt2Correction = gt2Correction;
        CwPriorityCatalog = cwPriorityCatalog;
        CwPriorityWorld = cwPriorityWorld;
        CompactCheckpoint = compactCheckpoint;
        AxisUnattachedCargo = axisUnattachedCargo;
        CommonwealthUnattachedCargo = commonwealthUnattachedCargo;
        CwFreeGuns = cwFreeGuns;
        CwFreeGunsWorld = cwFreeGunsWorld;
        // GroupedWorld must keep retaining the *current* active source World as
        // its Prior -- rebind it if whatever just changed (here, or via a later
        // `with` mutation; see Validated() below) moved World out from under it.
        GroupedWorld = groupedWorld is null || groupedWorld.Prior == World
            ? groupedWorld
            : RebindGroupedWorld(groupedWorld, World);
        StateVersion = stateVersion;
        FinalizedOwners = finalizedOwners;
        FinalizationVersion = finalizationVersion;
        HorseRulingVotes = horseRulingVotes;
        HorseStageOneNoTraining = horseStageOneNoTraining;
        HorseRulingVersion = horseRulingVersion;
        ValidateInvariants();
    }

    // Copies every field of `original` verbatim. This is what both the
    // compiler-generated clone (used by `with`) and any explicit copy go
    // through.
    //
    // Deliberately does NOT call ValidateInvariants() here, for two reasons,
    // the second found the hard way while verifying the first:
    //
    // 1) It wouldn't protect `with` anyway. A `with` expression works as
    //    "clone, THEN apply the member initializers" -- verified empirically
    //    with a throwaway record reproduction -- so a ValidateInvariants() call
    //    in this constructor only ever observes `original`'s own
    //    (already-valid) pre-mutation field values. It can never catch an
    //    invariant that the `with` block's own assignments introduce.
    // 2) It actively breaks legitimate multi-step `with` composition. Validated()
    //    below sometimes issues an intermediate `with` purely to patch
    //    GroupedWorld (see RebindGroupedWorld) before the final result is
    //    validated. If this constructor validated on every clone, that
    //    intermediate clone -- which still carries the OLD GroupedWorld next to
    //    the already-updated World-affecting fields -- would be validated while
    //    still transiently inconsistent, and the grouped-world checks in
    //    ValidateInvariants() would reject a perfectly legal transition (this
    //    is exactly the regression this comment replaced: PromoteCertifiedMalettiWorld
    //    started throwing where main's pre-refactor code did not).
    //
    // So the only thing that actually protects a `with`-built successor is each
    // mutation method calling ValidateInvariants() explicitly on the *final*
    // result, via the private `Validated()` helper -- see e.g.
    // SetOwnerFinalization below.
    private CampaignSourceAuthorityState(CampaignSourceAuthorityState original)
    {
        Pregame = original.Pregame;
        ConvoyPlan = original.ConvoyPlan;
        AxisSupplyWorld = original.AxisSupplyWorld;
        CommonwealthSupplyWorld = original.CommonwealthSupplyWorld;
        AxisInitialForces = original.AxisInitialForces;
        CommonwealthInitialForces = original.CommonwealthInitialForces;
        AxisMotorization = original.AxisMotorization;
        CommonwealthMotorization = original.CommonwealthMotorization;
        OpeningConvoyPlan = original.OpeningConvoyPlan;
        CommonwealthSupplyPlans = original.CommonwealthSupplyPlans;
        SeptemberPlans = original.SeptemberPlans;
        AxisSupplement = original.AxisSupplement;
        Autoblinda = original.Autoblinda;
        CommonwealthSupplement = original.CommonwealthSupplement;
        PriorityMotorization = original.PriorityMotorization;
        AxisPriorityMotorization = original.AxisPriorityMotorization;
        AxisGarrisons = original.AxisGarrisons;
        Battlefield = original.Battlefield;
        CairoSetup = original.CairoSetup;
        CairoWorld = original.CairoWorld;
        MalettiWorld = original.MalettiWorld;
        IndependentWorld = original.IndependentWorld;
        LocalMapWorld = original.LocalMapWorld;
        ExpandedIndependentWorld = original.ExpandedIndependentWorld;
        AttachedInitialLoads = original.AttachedInitialLoads;
        InitialWrecks = original.InitialWrecks;
        AxisFreeArtillery = original.AxisFreeArtillery;
        AxisFreeArtilleryWorld = original.AxisFreeArtilleryWorld;
        AxisCorpsArtillery = original.AxisCorpsArtillery;
        AxisCorpsArtilleryWorld = original.AxisCorpsArtilleryWorld;
        MalettiHierarchy = original.MalettiHierarchy;
        CertifiedMalettiWorld = original.CertifiedMalettiWorld;
        MatruhFormation = original.MatruhFormation;
        MatruhDetachedWorld = original.MatruhDetachedWorld;
        Gt2Correction = original.Gt2Correction;
        CwPriorityCatalog = original.CwPriorityCatalog;
        CwPriorityWorld = original.CwPriorityWorld;
        CompactCheckpoint = original.CompactCheckpoint;
        AxisUnattachedCargo = original.AxisUnattachedCargo;
        CommonwealthUnattachedCargo = original.CommonwealthUnattachedCargo;
        CwFreeGuns = original.CwFreeGuns;
        CwFreeGunsWorld = original.CwFreeGunsWorld;
        GroupedWorld = original.GroupedWorld;
        StateVersion = original.StateVersion;
        FinalizedOwners = original.FinalizedOwners;
        FinalizationVersion = original.FinalizationVersion;
        HorseRulingVotes = original.HorseRulingVotes;
        HorseStageOneNoTraining = original.HorseStageOneNoTraining;
        HorseRulingVersion = original.HorseRulingVersion;
    }

    // Mutation methods below build their successor with `this with { ... }` and
    // then call `.Validated()` on the result so the invariant checks that used to
    // run inside the full positional constructor call still run on every path
    // that can produce a new instance -- see ValidateInvariants() below for why
    // the copy constructor alone cannot provide this guarantee.
    private CampaignSourceAuthorityState Validated()
    {
        // A `with` mutation can change a field that feeds the computed `World`
        // property (CairoWorld, MalettiWorld, IndependentWorld, LocalMapWorld,
        // ExpandedIndependentWorld, the *ArtilleryWorld fields, CertifiedMaletti/
        // MatruhDetached/CwPriority/CwFreeGunsWorld) without touching GroupedWorld
        // itself. The primary constructor always re-derives GroupedWorld against
        // the freshly computed World on every reconstruction; `with` does not, so
        // this reproduces that same rebind-or-keep step here.
        var result = GroupedWorld is null || GroupedWorld.Prior == World
            ? this
            : this with { GroupedWorld = RebindGroupedWorld(GroupedWorld, World) };
        result.ValidateInvariants();
        return result;
    }

    private CampaignGroupedWorld RebindGroupedWorld(CampaignGroupedWorld groupedWorld, CampaignWorldSnapshotV8 world)
    {
        var scenarioStart = Pregame.Source.Session.Content.Definition.LegacyDefinition.Scenarios
            .Single(value => value.ScenarioId == world.ScenarioId).Start;
        return groupedWorld.Rebind(world, scenarioStart.GameTurn, scenarioStart.OperationStage);
    }

    // The same checks the primary constructor used to run inline, factored out so
    // both constructors -- and every `with`-based mutation method below -- can
    // reuse them. Order matches the original constructor exactly, so when more
    // than one invariant is violated at once, the same one throws first.
    private void ValidateInvariants()
    {
        if (SeptemberPlans is not null && (CommonwealthSupplyPlans is null || CommonwealthSupplyPlans.NextTargetGameTurn is not null))
            throw new ArgumentException("September planning requires completed Commonwealth plans.");
        if ((AxisSupplement is null) != (Autoblinda is null))
            throw new ArgumentException("Autoblinda setup requires the matching source supplement.");
        if (CommonwealthSupplement is not null && AxisSupplement is null)
            throw new ArgumentException("Commonwealth supplement requires the prior source profile.");
        if (PriorityMotorization is not null && CommonwealthSupplement is null)
            throw new ArgumentException("Priority motorization requires the prior source profile.");
        if (AxisPriorityMotorization is not null && PriorityMotorization is null)
            throw new ArgumentException("Axis priority motorization requires the prior source profile.");
        if (AxisGarrisons is not null && AxisPriorityMotorization is null)
            throw new ArgumentException("Axis garrisons require the prior source profile.");
        if (Battlefield is not null && AxisGarrisons is null)
            throw new ArgumentException("Source battlefield requires the prior profile.");
        if (CairoSetup is not null && Battlefield is null)
            throw new ArgumentException("Cairo setup requires the prior source battlefield.");
        if (CairoWorld is not null && CairoSetup is null)
            throw new ArgumentException("Cairo active world requires the prior source setup.");
        if (MalettiWorld is not null && CairoWorld is null)
            throw new ArgumentException("Maletti active world requires the prior Cairo world.");
        if (IndependentWorld is not null && CairoWorld is null)
            throw new ArgumentException("Independent world requires the prior Cairo world.");
        if (LocalMapWorld is not null && IndependentWorld is null)
            throw new ArgumentException("Local map requires the prior independent world.");
        if (ExpandedIndependentWorld is not null && LocalMapWorld is null)
            throw new ArgumentException("Expanded independent world requires the prior local map.");
        if (AttachedInitialLoads is not null && ExpandedIndependentWorld is null)
            throw new ArgumentException("Attached initial loads require the prior source world.");
        if (InitialWrecks is not null && AttachedInitialLoads is null)
            throw new ArgumentException("Initial wrecks require the prior setup profile.");
        if (AxisFreeArtillery is not null && InitialWrecks is null)
            throw new ArgumentException("Axis free setup requires prior source setup.");
        if (AxisFreeArtilleryWorld is not null && AxisFreeArtillery is null)
            throw new ArgumentException("Axis free world requires source setup.");
        if (AxisCorpsArtillery is not null && AxisFreeArtilleryWorld is null)
            throw new ArgumentException("Axis corps setup requires prior source world.");
        if (AxisCorpsArtilleryWorld is not null && AxisCorpsArtillery is null)
            throw new ArgumentException("Axis corps world requires source setup.");
        if (MalettiHierarchy is not null && AxisCorpsArtilleryWorld is null)
            throw new ArgumentException("Maletti hierarchy requires prior source world.");
        if (CertifiedMalettiWorld is not null && MalettiHierarchy is null)
            throw new ArgumentException("Certified Maletti world requires its hierarchy.");
        if (MatruhFormation is not null && MalettiHierarchy is null)
            throw new ArgumentException("Matruh formation requires prior source hierarchy.");
        if (MatruhDetachedWorld is not null && MatruhFormation is null)
            throw new ArgumentException("Matruh detached world requires the source formation.");
        if (CwPriorityWorld is not null && CwPriorityCatalog is null)
            throw new ArgumentException("Commonwealth priority world requires catalog.");
        // Scenario 60.31 deploys Axis forces outside Cairo and Helwan.
        // This setup-only invariant makes Australian setup sites public-safe:
        // no hidden Axis unit can occupy one of these sites before GT1.
        if (Battlefield is not null && Battlefield.Entries.Any(entry => entry.SideId == "axis"
            && entry.LocationId is not null && CampaignSourceSixthAustralianGroupedDefinition.Locations.Contains(entry.LocationId, StringComparer.Ordinal)))
            throw new InvalidOperationException("Axis source setup cannot occupy Cairo or Helwan.");
        if (CwFreeGunsWorld is not null && CwFreeGuns is null) throw new ArgumentException("CW free guns world requires setup.");
        if (FinalizedOwners is < 0 or > 3) throw new ArgumentOutOfRangeException("finalizedOwners");
        if (!FinalizationVersion && FinalizedOwners != 0) throw new ArgumentException("Owner mask requires finalization checkpoint version.");
        if (HorseRulingVotes is < 0 or > 3) throw new ArgumentOutOfRangeException("horseRulingVotes");
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
            if (IsSeventhArmoredGrouped)
            {
                CampaignSourceSeventhArmoredGroupedDefinition.Validate(this, GroupedWorld);
                groupedBattlefield.ValidateSeventhArmored(GroupedWorld);
            }
            if (IsSixteenthBrigadeGrouped)
            {
                CampaignSourceSixteenthBrigadeGroupedDefinition.Validate(this, GroupedWorld);
                groupedBattlefield.ValidateSixteenthBrigade(GroupedWorld);
            }
            if (IsCatanzaroGrouped)
            {
                CampaignSourceCatanzaroGroupedDefinition.Validate(this, GroupedWorld);
                groupedBattlefield.ValidateCatanzaro(GroupedWorld);
            }
            if (IsSixthAustralianGrouped)
            {
                CampaignSourceSixthAustralianGroupedDefinition.Validate(this, GroupedWorld);
                var australian = GroupedWorld.Groups.Single(value => value.GroupId == CampaignSourceSixthAustralianGroupedDefinition.GroupId);
                if (australian.Members.Any(member => !groupedBattlefield.Entries.Any(entry => entry.ElementId == member.ElementId
                    && entry.SideId == "commonwealth" && entry.LocationId == australian.LocationId && entry.AuthorityStatus == "source-australian-grouped")))
                    throw new InvalidOperationException("Australian battlefield index differs from grouped world.");
            }
            if (IsCcnnBatchGrouped)
            {
                CampaignSourceCcnnBatch.Validate(this, GroupedWorld);
                foreach (var group in GroupedWorld.Groups.Where(value => value.GroupId is CampaignSourceCcnnBatch.FirstId or CampaignSourceCcnnBatch.FourthId))
                    if (group.Members.Any(member => !groupedBattlefield.Entries.Any(entry => entry.ElementId == member.ElementId
                        && entry.SideId == "axis" && entry.LocationId == group.LocationId && entry.AuthorityStatus == "source-ccnn-grouped")))
                        throw new InvalidOperationException("CCNN battlefield index differs from grouped world.");
            }
            if (IsArescaGrouped)
            {
                CampaignSourceArescaGroupedDefinition.Validate(this, GroupedWorld);
                var aresca = GroupedWorld.Groups.Single(value => value.GroupId == CampaignSourceArescaGroupedDefinition.GroupId);
                if (aresca.Members.Any(member => !groupedBattlefield.Entries.Any(entry => entry.ElementId == member.ElementId
                    && entry.SideId == "axis" && entry.LocationId == aresca.LocationId && entry.AuthorityStatus == "source-aresca-grouped")))
                    throw new InvalidOperationException("Aresca battlefield index differs from grouped world.");
            }
        }
        if (GroupedWorld is not null && (CommonwealthMotorization.Remaining(CampaignSourceFourthIndianDefinition.PoolId) != 22
            || AttachedInitialLoads is null || AttachedInitialLoads.Reserved(CampaignSourceFourthIndianDefinition.PoolId) != 0))
            throw new InvalidOperationException("Fourth Indian motor reservations must share the retained source pool.");
    }

    internal CampaignPregameSourceState Pregame { get; private init; }
    internal CampaignCreationRequest Request => Pregame.Request;
    internal ContentSourceScheduledCombatPackArtifact Content => Pregame.Source.Session.Content;
    internal CampaignWorldSnapshotV8 World => CwFreeGunsWorld?.World ?? CwPriorityWorld?.World ?? MatruhDetachedWorld?.World ?? CertifiedMalettiWorld?.World ?? AxisCorpsArtilleryWorld?.World ?? AxisFreeArtilleryWorld?.World ?? ExpandedIndependentWorld?.World ?? LocalMapWorld?.World ?? IndependentWorld?.World ?? MalettiWorld?.World ?? CairoWorld?.World ?? Pregame.Source.Session.InitialWorld;
    internal RandomStreamState RandomState => SeptemberPlans?.RandomState ?? CommonwealthSupplyPlans?.RandomState ?? OpeningConvoyPlan.RandomState;
    internal CampaignSourceConvoyPlan ConvoyPlan { get; private init; }
    internal CampaignSourceAxisSupplyWorld AxisSupplyWorld { get; private init; }
    internal CampaignSourceCommonwealthSupplyWorld CommonwealthSupplyWorld { get; private init; }
    internal CampaignSourceAxisInitialForces AxisInitialForces { get; private init; }
    internal CampaignSourceCommonwealthInitialForces CommonwealthInitialForces { get; private init; }
    internal CampaignSourceMotorizationWorld AxisMotorization { get; private init; }
    internal CampaignSourceMotorizationWorld CommonwealthMotorization { get; private init; }
    internal CampaignSourceOpeningConvoyPlan OpeningConvoyPlan { get; private init; }
    internal CampaignSourceCommonwealthSupplyPlans? CommonwealthSupplyPlans { get; private init; }
    internal CampaignSourceSeptemberConvoyPlans? SeptemberPlans { get; private init; }
    internal int FinalizedOwners { get; private init; }
    internal bool FinalizationVersion { get; private init; }
    internal const string HorseRulingId = "graziani.central-india-horse.oa-note-c-untrained.v1";
    internal int HorseRulingVotes { get; private init; }
    internal bool HorseStageOneNoTraining { get; private init; }
    internal bool HorseRulingVersion { get; private init; }
    internal bool HasHorseRulingVote(CampaignActionAudience audience) => audience switch
    {
        CampaignActionAudience.Axis => (HorseRulingVotes & 1) != 0,
        CampaignActionAudience.Commonwealth => (HorseRulingVotes & 2) != 0,
        _ => false,
    };
    internal bool HorseStageOneResolved => HorseRulingVotes == 3 && HorseStageOneNoTraining;

    internal bool IsOwnerFinalized(CampaignActionAudience audience) => audience switch
    {
        CampaignActionAudience.Axis => (FinalizedOwners & 1) != 0,
        CampaignActionAudience.Commonwealth => (FinalizedOwners & 2) != 0,
        _ => false,
    };

    internal static string SetupFinalizationActionId(CampaignActionAudience audience, bool finish) =>
        CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes(
            $"graziani.setup.owner-finalization.v1\0{audience}\0{finish}"));

    internal CampaignSourceAuthorityState SetOwnerFinalization(CampaignActionAudience audience, bool finish)
    {
        var flag = audience switch
        {
            CampaignActionAudience.Axis => 1,
            CampaignActionAudience.Commonwealth => 2,
            _ => throw new InvalidOperationException("Only an owner may finalize setup."),
        };
        if (IsOwnerFinalized(audience) == finish)
            throw new InvalidOperationException("Setup finalization has already been recorded.");
        var next = finish ? FinalizedOwners | flag : FinalizedOwners & ~flag;
        return (this with { StateVersion = checked(StateVersion + 1), FinalizedOwners = next, FinalizationVersion = true }).Validated();
    }

    internal static string HorseSetupActionId(CampaignActionAudience audience, string kind) =>
        CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes($"graziani.horse.setup.v1\0{audience}\0{kind}"));

    internal CampaignSourceAuthorityState SetHorseSetupDecision(CampaignActionAudience audience, string kind)
    {
        if (audience is not (CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth) || IsOwnerFinalized(audience))
            throw new InvalidOperationException("Horse setup decision requires an editable owner.");
        var votes = HorseRulingVotes;
        var noTraining = HorseStageOneNoTraining;
        var bit = audience == CampaignActionAudience.Axis ? 1 : 2;
        switch (kind)
        {
            case "approve" when (votes & bit) == 0: votes |= bit; break;
            case "withdraw" when (votes & bit) != 0: votes &= ~bit; break;
            case "decline-stage1" when audience == CampaignActionAudience.Commonwealth && !noTraining: noTraining = true; break;
            case "reopen-stage1" when audience == CampaignActionAudience.Commonwealth && noTraining: noTraining = false; break;
            default: throw new InvalidOperationException("Horse setup decision is unavailable.");
        }
        return (this with { StateVersion = checked(StateVersion + 1), HorseRulingVotes = votes, HorseStageOneNoTraining = noTraining, HorseRulingVersion = true }).Validated();
    }

    internal byte[] SerializeHorseSetupEvent(CampaignSourceAuthorityState successor, CampaignActionAudience audience, string kind)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1);
            writer.WriteString("eventType", "source-horse-setup-decision"); writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion); writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteNumber("stateVersion", successor.StateVersion); writer.WriteString("actionId", HorseSetupActionId(audience, kind));
            writer.WriteString("actor", audience == CampaignActionAudience.Axis ? "axis" : "commonwealth");
            writer.WriteString("kind", kind); writer.WriteString("rulingId", HorseRulingId);
            writer.WriteString("checkpointHash", CampaignPregameSourceCreation.Digest(successor.SerializeCheckpoint())); writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal byte[] SerializeSetupFinalizationEvent(CampaignSourceAuthorityState successor,
        CampaignActionAudience audience, bool finish)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("eventType", "source-owner-setup-finalization-changed");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", SetupFinalizationActionId(audience, finish));
            writer.WriteString("actor", audience == CampaignActionAudience.Axis ? "axis" : "commonwealth");
            writer.WriteBoolean("finished", finish);
            writer.WriteNumber("stateVersion", successor.StateVersion);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal CampaignSourceAxisSupplementalForces? AxisSupplement { get; private init; }
    internal CampaignSourceAutoblindaSetup? Autoblinda { get; private init; }
    internal CampaignSourceCommonwealthSupplementalForces? CommonwealthSupplement { get; private init; }
    internal CampaignSourcePriorityMotorization? PriorityMotorization { get; private init; }
    internal CampaignSourceAxisPriorityMotorization? AxisPriorityMotorization { get; private init; }
    internal CampaignSourceAxisGarrisonForces? AxisGarrisons { get; private init; }
    internal CampaignSourceBattlefield? Battlefield { get; private init; }
    internal CampaignSourceCairoInitialSetup? CairoSetup { get; private init; }
    internal CampaignSourceCairoActiveWorld? CairoWorld { get; private init; }
    internal CampaignSourceMalettiActiveWorld? MalettiWorld { get; private init; }
    internal CampaignSourceIndependentActiveWorld? IndependentWorld { get; private init; }
    internal CampaignSourceLocalMapWorld? LocalMapWorld { get; private init; }
    internal CampaignSourceExpandedIndependentWorld? ExpandedIndependentWorld { get; private init; }
    internal CampaignSourceAttachedInitialLoads? AttachedInitialLoads { get; private init; }
    internal CampaignSourceInitialWrecks? InitialWrecks { get; private init; }
    internal CampaignSourceAxisFreeArtillery? AxisFreeArtillery { get; private init; }
    internal CampaignSourceAxisFreeArtilleryWorld? AxisFreeArtilleryWorld { get; private init; }
    internal CampaignSourceAxisCorpsArtillery? AxisCorpsArtillery { get; private init; }
    internal CampaignSourceAxisCorpsArtilleryWorld? AxisCorpsArtilleryWorld { get; private init; }
    internal CampaignSourceMalettiHierarchy? MalettiHierarchy { get; private init; }
    internal CampaignSourceCertifiedMalettiWorld? CertifiedMalettiWorld { get; private init; }
    internal CampaignSourceMatruhFormation? MatruhFormation { get; private init; }
    internal CampaignSourceMatruhDetachedWorld? MatruhDetachedWorld { get; private init; }
    internal CampaignSourceGt2ConvoyCorrection? Gt2Correction { get; private init; }
    internal CampaignSourceCwPriorityCatalog? CwPriorityCatalog { get; private init; }
    internal CampaignSourceCwPriorityWorld? CwPriorityWorld { get; private init; }
    internal bool CompactCheckpoint { get; private init; }
    internal const int FinalizedLegacyCheckpointContractVersion = 43;
    internal const int FinalizedCompactCheckpointContractVersion = 44;
    internal const int AustralianCompactCheckpointContractVersion = 46;
    internal const int CcnnCompactCheckpointContractVersion = 48;
    internal const int CombinedCompactCheckpointContractVersion = 50;
    internal const int ShellCorrectedLegacyCheckpointContractVersion = 51;
    internal const int ShellCorrectedCompactCheckpointContractVersion = 52;
    internal const int ShellCorrectedFinalizedLegacyCheckpointContractVersion = 53;
    internal const int ShellCorrectedFinalizedCompactCheckpointContractVersion = 54;
    internal const int ArescaCompactCheckpointContractVersion = 56;
    internal const int CombinedArescaCompactCheckpointContractVersion = 58;
    internal const int HorseRulingCheckpointContractVersion = 59;
    internal const int AxisMinorGarrisonsCheckpointContractVersion = 60;
    internal const int AxisGarrisonRemainderCheckpointContractVersion = 61;
    internal const int GiarabubMotorMachineGunCheckpointContractVersion = 62;
    internal const int CwFrontlineCheckpointContractVersion = 63;
    internal const int SourceTobrukBardiaGarrisonCheckpointContractVersion = 64;
    internal const int SourceBardiaGarrisonRemainderCheckpointContractVersion = 65;
    internal const int SourceTripoliTankRegimentCheckpointContractVersion = 66;
    internal const int SourceCwAttachedArtilleryCheckpointContractVersion = 67;
    // ProfileVersion / BaseProfileVersion / LegacyProfileVersion / UnderlyingLegacyProfileVersion
    // pick the on-wire checkpoint contract version (a persisted/replayed value) from an
    // ORDERED table of (predicate, version) rules, first match wins. This replaces what was
    // a long chain of nested ternaries; the rule order below is IDENTICAL to that chain's
    // evaluation order and must stay that way - do not reorder without re-running the
    // characterization coverage in GrazianiSourceAuthorityTests.
    private static int FirstMatch(CampaignSourceAuthorityState state,
        (Func<CampaignSourceAuthorityState, bool> When, int Version)[] rules, int fallback)
    {
        foreach (var rule in rules)
            if (rule.When(state)) return rule.Version;
        return fallback;
    }
    private static readonly (Func<CampaignSourceAuthorityState, bool> When, int Version)[] ProfileVersionRules =
    [
        (state => state.CwFreeGunsWorld?.CwAttachedArtilleryContent is not null, SourceCwAttachedArtilleryCheckpointContractVersion),
        (state => state.CwFreeGunsWorld?.TripoliTankRegimentContent is not null, SourceTripoliTankRegimentCheckpointContractVersion),
        (state => state.CwFreeGunsWorld?.BardiaGarrisonRemainderContent is not null, SourceBardiaGarrisonRemainderCheckpointContractVersion),
        (state => state.CwFreeGunsWorld?.TobrukBardiaGarrisonContent is not null, SourceTobrukBardiaGarrisonCheckpointContractVersion),
        (state => state.CwFreeGunsWorld?.CwFrontlineContent is not null, CwFrontlineCheckpointContractVersion),
        (state => state.CwFreeGunsWorld?.MotorMachineGunContent is not null, GiarabubMotorMachineGunCheckpointContractVersion),
        (state => state.CwFreeGunsWorld?.GarrisonRemainderContent is not null, AxisGarrisonRemainderCheckpointContractVersion),
        (state => state.CwFreeGunsWorld?.MinorGarrisonContent is not null, AxisMinorGarrisonsCheckpointContractVersion),
        (state => state.HorseRulingVersion, HorseRulingCheckpointContractVersion),
    ];
    internal int ProfileVersion => FirstMatch(this, ProfileVersionRules, BaseProfileVersion);
    private static readonly (Func<CampaignSourceAuthorityState, bool> When, int Version)[] BaseProfileVersionRules =
    [
        (state => state.FinalizationVersion && state.IsSeventhShellCorrected && state.CompactCheckpoint, ShellCorrectedFinalizedCompactCheckpointContractVersion),
        (state => state.FinalizationVersion && state.IsSeventhShellCorrected, ShellCorrectedFinalizedLegacyCheckpointContractVersion),
        (state => state.FinalizationVersion && state.CompactCheckpoint, FinalizedCompactCheckpointContractVersion),
        (state => state.FinalizationVersion, FinalizedLegacyCheckpointContractVersion),
        (state => state.CompactCheckpoint && state.IsSeventhShellCorrected, ShellCorrectedCompactCheckpointContractVersion),
        (state => state.CompactCheckpoint && state.IsArescaGrouped && state.IsSixthAustralianGrouped, CombinedArescaCompactCheckpointContractVersion),
        (state => state.CompactCheckpoint && state.IsArescaGrouped, ArescaCompactCheckpointContractVersion),
        (state => state.CompactCheckpoint && state.IsCcnnBatchGrouped && state.IsSixthAustralianGrouped, CombinedCompactCheckpointContractVersion),
        (state => state.CompactCheckpoint && state.IsCcnnBatchGrouped, CcnnCompactCheckpointContractVersion),
        (state => state.CompactCheckpoint && state.IsSixthAustralianGrouped, AustralianCompactCheckpointContractVersion),
        (state => state.CompactCheckpoint, CompactCheckpointContractVersion),
    ];
    private int BaseProfileVersion => FirstMatch(this, BaseProfileVersionRules, LegacyProfileVersion);
    internal CampaignSourceUnattachedCargo? AxisUnattachedCargo { get; private init; }
    internal CampaignSourceUnattachedCargo? CommonwealthUnattachedCargo { get; private init; }
    internal CampaignSourceCwFreeGuns? CwFreeGuns { get; private init; }
    internal CampaignSourceCwFreeGunsWorld? CwFreeGunsWorld { get; private init; }
    internal CampaignGroupedWorld? GroupedWorld { get; private init; }
    private static readonly (Func<CampaignSourceAuthorityState, bool> When, int Version)[] LegacyProfileVersionRules =
    [
        (state => state.IsSeventhShellCorrected, ShellCorrectedLegacyCheckpointContractVersion),
    ];
    private int LegacyProfileVersion => FirstMatch(this, LegacyProfileVersionRules, UnderlyingLegacyProfileVersion);
    private static readonly (Func<CampaignSourceAuthorityState, bool> When, int Version)[] UnderlyingLegacyProfileVersionRules =
    [
        (state => state.IsArescaGrouped && state.IsSixthAustralianGrouped, 57),
        (state => state.IsArescaGrouped, 55),
        (state => state.IsCcnnBatchGrouped && state.IsSixthAustralianGrouped, 49),
        (state => state.IsCcnnBatchGrouped, 47),
        (state => state.IsSixthAustralianGrouped, 45),
        (state => state.IsCatanzaroGrouped, 42),
        (state => state.IsSixteenthBrigadeGrouped, 41),
        (state => state.IsSeventhArmoredGrouped, 40),
        (state => state.IsItalianDivisionsGrouped, 39),
        (state => state.SeptemberPlans is not null, 38),
        (state => state.IsLibyanGrouped, 37),
        (state => state.IsMalettiGrouped, 36),
        (state => state.GroupedWorld?.Groups.Any(value => value.GroupId == CampaignSourceMatruhGroupedDefinition.GroupId) == true, 35),
        (state => state.GroupedWorld is not null, 34),
        (state => state.AxisUnattachedCargo is not null || state.CommonwealthUnattachedCargo is not null, UnattachedCargoContractVersion),
        (state => state.CwFreeGunsWorld is not null, CwFreeGunsWorldContractVersion),
        (state => state.CwFreeGuns is not null, CwFreeGunsContractVersion),
        (state => state.CwPriorityWorld is not null, CwPriorityWorldContractVersion),
        (state => state.CwPriorityCatalog is not null, CwPriorityCatalogContractVersion),
        (state => state.Gt2Correction is not null, Gt2CorrectionContractVersion),
        (state => state.MatruhDetachedWorld is not null, MatruhDetachedWorldContractVersion),
        (state => state.MatruhFormation is not null, MatruhFormationContractVersion),
        (state => state.CertifiedMalettiWorld is not null, CertifiedMalettiWorldContractVersion),
        (state => state.MalettiHierarchy is not null, MalettiHierarchyContractVersion),
        (state => state.AxisCorpsArtilleryWorld is not null, AxisCorpsWorldContractVersion),
        (state => state.AxisCorpsArtillery is not null, AxisCorpsSetupContractVersion),
        (state => state.AxisFreeArtilleryWorld is not null, AxisFreeWorldContractVersion),
        (state => state.AxisFreeArtillery is not null, AxisFreeSetupContractVersion),
        (state => state.InitialWrecks is not null, InitialWrecksContractVersion),
        (state => state.AttachedInitialLoads is not null, AttachedInitialLoadsContractVersion),
        (state => state.ExpandedIndependentWorld is not null, ExpandedIndependentWorldContractVersion),
        (state => state.LocalMapWorld is not null, LocalMapWorldContractVersion),
        (state => state.IndependentWorld is not null, IndependentWorldContractVersion),
        (state => state.MalettiWorld is not null, MalettiWorldContractVersion),
        (state => state.CairoWorld is not null, CairoWorldContractVersion),
        (state => state.CairoSetup is not null, CairoSetupContractVersion),
        (state => state.Battlefield is not null, BattlefieldContractVersion),
        (state => state.AxisGarrisons is not null, AxisGarrisonsContractVersion),
        (state => state.AxisPriorityMotorization is not null, AxisPriorityMotorizationContractVersion),
        (state => state.PriorityMotorization is not null, PriorityMotorizationContractVersion),
        (state => state.CommonwealthSupplement is not null, CommonwealthSupplementalContractVersion),
        (state => state.AxisSupplement is not null, SupplementalContractVersion),
    ];
    private int UnderlyingLegacyProfileVersion => FirstMatch(this, UnderlyingLegacyProfileVersionRules, ContractVersion);
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
    internal long StateVersion { get; private init; }
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
        (this with { AxisSupplyWorld = AxisSupplyWorld.Place(dumpId, locationId), StateVersion = checked(StateVersion + 1) }).Validated();

    internal CampaignSourceAuthorityState PlaceCommonwealthSupply(string placementId, string locationId) =>
        (this with { CommonwealthSupplyWorld = CommonwealthSupplyWorld.Place(placementId, locationId), StateVersion = checked(StateVersion + 1) }).Validated();

    internal CampaignSourceAuthorityState DecideDummy(bool axis, string dummyId, string? locationId) => axis
        ? (this with { AxisSupplyWorld = AxisSupplyWorld.DecideDummy(dummyId, locationId), StateVersion = checked(StateVersion + 1) }).Validated()
        : dummyId == "cw-dummy-1"
            ? (this with { CommonwealthSupplyWorld = CommonwealthSupplyWorld.DecideDummy(locationId), StateVersion = checked(StateVersion + 1) }).Validated()
            : throw new InvalidOperationException("Unknown Commonwealth dummy group.");

    internal static readonly string AdmitGiarabubMotorPlacementPolicyActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.giarabub-motor-placement-policy.admit.v2"));

    internal bool CanAdmitGiarabubMotorPlacementPolicy => !AxisMotorization.GiarabubPlacementAdmitted
        && AxisUnattachedCargo is null && FinalizedOwners == 0;

    internal CampaignSourceAuthorityState AdmitGiarabubMotorPlacementPolicy()
    {
        if (!CanAdmitGiarabubMotorPlacementPolicy)
            throw new InvalidOperationException("Giarabub motor placement policy is unavailable.");
        return (this with { AxisMotorization = AxisMotorization.AdmitGiarabubPlacement(), StateVersion = checked(StateVersion + 1) }).Validated();
    }

    internal byte[] SerializeGiarabubMotorPlacementPolicyEvent(CampaignSourceAuthorityState successor)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 2);
            writer.WriteString("eventType", "source-giarabub-motor-placement-policy-admitted");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", AdmitGiarabubMotorPlacementPolicyActionId);
            writer.WriteString("actor", "system");
            writer.WriteString("axisMotorizationHash", successor.AxisMotorization.Hash());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal CampaignSourceAuthorityState AdjustMotorization(bool axis, string poolId, string locationId, int delta) => axis
        ? (this with { AxisMotorization = AxisMotorization.Assign(poolId, locationId, delta), StateVersion = checked(StateVersion + 1) }).Validated()
        : (this with { CommonwealthMotorization = CommonwealthMotorization.Assign(poolId, locationId, delta), StateVersion = checked(StateVersion + 1) }).Validated();

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
        return (this with { OpeningConvoyPlan = plan, CommonwealthSupplyPlans = cwPlans, StateVersion = checked(StateVersion + 1) }).Validated();
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
        return (this with { CommonwealthSupplyPlans = next, StateVersion = checked(StateVersion + 1) }).Validated();
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
        return (this with { StateVersion = checked(StateVersion + 1), SeptemberPlans = next }).Validated();
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

    internal CampaignSourceAuthorityState AdmitAxisSupplement() => AdmitCore(this, StepByKind("Supplemental"));

    private CampaignSourceAuthorityState AdmitAxisSupplementCore()
    {
        var supplement = CampaignSourceAxisSupplementalForces.Create(Pregame, AxisInitialForces);
        return (this with { AxisSupplement = supplement, Autoblinda = CampaignSourceAutoblindaSetup.Create(supplement), CommonwealthSupplement = null, PriorityMotorization = null, AxisPriorityMotorization = null, AxisGarrisons = null, Battlefield = null, CairoSetup = null, CairoWorld = null, MalettiWorld = null, IndependentWorld = null, LocalMapWorld = null, StateVersion = checked(StateVersion + 1) }).Validated();
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
        return (this with { Autoblinda = next, StateVersion = checked(StateVersion + 1) }).Validated();
    }

    internal static readonly string AdmitCommonwealthSupplementActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.commonwealth-forces.supplement.v1"));

    internal CampaignSourceAuthorityState AdmitCommonwealthSupplement() => AdmitCore(this, StepByKind("CommonwealthSupplemental"));

    private CampaignSourceAuthorityState AdmitCommonwealthSupplementCore()
    {
        var supplement = CampaignSourceCommonwealthSupplementalForces.Create(Pregame, CommonwealthInitialForces);
        return (this with { CommonwealthSupplement = supplement, PriorityMotorization = null, AxisPriorityMotorization = null, AxisGarrisons = null, Battlefield = null, CairoSetup = null, CairoWorld = null, MalettiWorld = null, IndependentWorld = null, LocalMapWorld = null, StateVersion = checked(StateVersion + 1) }).Validated();
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

    internal CampaignSourceAuthorityState AdmitPriorityMotorization() => AdmitCore(this, StepByKind("PriorityMotorization"));

    private CampaignSourceAuthorityState AdmitPriorityMotorizationCore()
    {
        var priority = CampaignSourcePriorityMotorization.Create(CommonwealthSupplement!, CommonwealthMotorization);
        return (this with { PriorityMotorization = priority, AxisPriorityMotorization = null, AxisGarrisons = null, Battlefield = null, CairoSetup = null, CairoWorld = null, MalettiWorld = null, IndependentWorld = null, LocalMapWorld = null, StateVersion = checked(StateVersion + 1) }).Validated();
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
        return (this with { PriorityMotorization = next, StateVersion = checked(StateVersion + 1) }).Validated();
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

    internal CampaignSourceAuthorityState AdmitAxisPriorityMotorization() => AdmitCore(this, StepByKind("AxisPriorityMotorization"));

    private CampaignSourceAuthorityState AdmitAxisPriorityMotorizationCore()
    {
        var priority = CampaignSourceAxisPriorityMotorization.Create(AxisSupplement!, AxisMotorization);
        return (this with { AxisPriorityMotorization = priority, AxisGarrisons = null, Battlefield = null, CairoSetup = null, CairoWorld = null, MalettiWorld = null, IndependentWorld = null, LocalMapWorld = null, StateVersion = checked(StateVersion + 1) }).Validated();
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
            AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization, next, AxisGarrisons, Battlefield, CairoSetup, CairoWorld, MalettiWorld, IndependentWorld, LocalMapWorld, checked(StateVersion + 1), ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld, AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, grouped, septemberPlans: SeptemberPlans, finalizedOwners: FinalizedOwners, finalizationVersion: FinalizationVersion, horseRulingVotes: HorseRulingVotes, horseStageOneNoTraining: HorseStageOneNoTraining, horseRulingVersion: HorseRulingVersion);
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

    internal CampaignSourceAuthorityState AdmitAxisGarrisons() => AdmitCore(this, StepByKind("AxisGarrisons"));

    private CampaignSourceAuthorityState AdmitAxisGarrisonsCore()
    {
        var garrisons = CampaignSourceAxisGarrisonForces.Create(Pregame, AxisInitialForces, AxisSupplement!);
        return (this with { AxisGarrisons = garrisons, Battlefield = null, CairoSetup = null, CairoWorld = null, MalettiWorld = null, IndependentWorld = null, LocalMapWorld = null, StateVersion = checked(StateVersion + 1) }).Validated();
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

    internal CampaignSourceAuthorityState AdmitBattlefield() => AdmitCore(this, StepByKind("Battlefield"));

    private CampaignSourceAuthorityState AdmitBattlefieldCore()
    {
        var battlefield = CampaignSourceBattlefield.Create(this);
        return (this with { Battlefield = battlefield, CairoSetup = null, CairoWorld = null, MalettiWorld = null, IndependentWorld = null, LocalMapWorld = null, StateVersion = checked(StateVersion + 1) }).Validated();
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

    internal CampaignSourceAuthorityState AdmitCairoSetup() => AdmitCore(this, StepByKind("CairoSetup"));

    private CampaignSourceAuthorityState AdmitCairoSetupCore()
    {
        var cairo = CampaignSourceCairoInitialSetup.Create(Battlefield!);
        var battlefield = Battlefield!.AddCairoSetup(cairo);
        return (this with { Battlefield = battlefield, CairoSetup = cairo, StateVersion = checked(StateVersion + 1) }).Validated();
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
        return (this with { Battlefield = battlefield, CairoSetup = cairo, StateVersion = checked(StateVersion + 1) }).Validated();
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

    internal CampaignSourceAuthorityState AdmitCairoWorld() => AdmitCore(this, StepByKind("CairoWorld"));

    private CampaignSourceAuthorityState AdmitCairoWorldCore()
    {
        var world = CampaignSourceCairoActiveWorld.Create(this);
        return (this with { CairoWorld = world, MalettiWorld = null, IndependentWorld = null, LocalMapWorld = null, StateVersion = checked(StateVersion + 1) }).Validated();
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

    internal CampaignSourceAuthorityState PromoteMaletti() => AdmitCore(this, StepByKind("MalettiWorld"));

    private CampaignSourceAuthorityState PromoteMalettiCore()
    {
        var active = CampaignSourceMalettiActiveWorld.Create(this);
        var battlefield = Battlefield!.PromoteMaletti(AxisPriorityMotorization!, World, active.World);
        return (this with { Battlefield = battlefield, MalettiWorld = active, IndependentWorld = null, LocalMapWorld = null, StateVersion = checked(StateVersion + 1) }).Validated();
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

    internal CampaignSourceAuthorityState PromoteIndependent() => AdmitCore(this, StepByKind("IndependentWorld"));

    private CampaignSourceAuthorityState PromoteIndependentCore()
    {
        var active = CampaignSourceIndependentActiveWorld.Create(this);
        var battlefield = Battlefield!.PromoteSourceIndependent(CommonwealthSupplement!, AxisSupplement!,
            AxisPriorityMotorization!, World, active.World);
        return (this with { Battlefield = battlefield, IndependentWorld = active, LocalMapWorld = LocalMapWorld is null ? null : CampaignSourceLocalMapWorld.Rebind(LocalMapWorld, active), StateVersion = checked(StateVersion + 1) }).Validated();
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
    internal CampaignSourceAuthorityState AdmitCwFreeGuns() => AdmitCore(this, StepByKind("CwFreeGuns"));

    private CampaignSourceAuthorityState AdmitCwFreeGunsCore()
    {
        var setup = CampaignSourceCwFreeGuns.Create(this, Battlefield!);
        return WithCwFreeGuns(setup, Battlefield!.AddCwFreeGuns(setup), CwFreeGunsWorld);
    }
    internal CampaignSourceAuthorityState PlaceCwFreeGun(string id, string location)
    {
        var setup = CwFreeGuns ?? throw new InvalidOperationException("CW free guns not admitted.");
        var battlefield = Battlefield ?? throw new InvalidOperationException("Source battlefield missing.");
        return WithCwFreeGuns(setup.Place(id, location, battlefield), battlefield.PlaceCwFreeGun(setup, id, location), CwFreeGunsWorld);
    }
    internal bool CanPromoteCwFreeGunsWorld => CwFreeGuns is not null && Battlefield is not null && CwPriorityWorld is not null
        && CwFreeGuns.Placements.Any(placement => World.World.Elements.All(element => element.ElementId != placement.ElementId));
    internal CampaignSourceAuthorityState PromoteCwFreeGunsWorld() => AdmitCore(this, StepByKind("CwFreeGunsWorld"));

    private CampaignSourceAuthorityState PromoteCwFreeGunsWorldCore()
    {
        var active = CampaignSourceCwFreeGunsWorld.Create(this);
        return WithCwFreeGuns(CwFreeGuns!, Battlefield!.PromoteCwFreeGuns(CwFreeGuns!, World, active.World), active);
    }
    internal static readonly string ActivateAxisMinorGarrisonsActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.axis-minor-garrisons.activate.v1"));
    internal static readonly string ActivateAxisGarrisonRemainderActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.axis-garrison-remainder.activate.v1"));
    internal static readonly string ActivateGiarabubMotorMachineGunActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.giarabub-motor-mg.activate.v1"));
    internal static readonly string ActivateCwFrontlineActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.cw-frontline.activate.v1"));
    internal bool CanActivateCwFrontline => CwFreeGunsWorld is
    { MotorMachineGunContent: not null, CwFrontlineContent: null }
        && GroupedWorld is not null && CommonwealthSupplement is not null && Battlefield is not null
        && CompactCheckpoint && FinalizedOwners == 0;
    internal CampaignSourceAuthorityState ActivateCwFrontline()
    {
        if (!CanActivateCwFrontline) throw new InvalidOperationException("CW frontline activation is unavailable.");
        var active = CwFreeGunsWorld!.ActivateCwFrontline(CommonwealthSupplement!, Battlefield!, CommonwealthMotorization);
        return WithCwFreeGuns(CwFreeGuns!, Battlefield!.ActivateCwFrontline(World, active.World), active);
    }
    internal byte[] SerializeCwFrontlineEvent(CampaignSourceAuthorityState successor)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", CwFrontlineCheckpointContractVersion);
            writer.WriteString("eventType", "source-cw-frontline-activated");
            writer.WriteString("campaignId", Request.CampaignId); writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", ActivateCwFrontlineActionId); writer.WriteString("actor", "system");
            writer.WriteString("contentHash", successor.CwFreeGunsWorld!.CwFrontlineContent!.Identity.Hash);
            writer.WriteString("worldHash", successor.CwFreeGunsWorld.Hash());
            writer.WriteString("groupedWorldHash", successor.GroupedWorld!.Hash());
            writer.WriteString("battlefieldHash", successor.Battlefield!.Hash()); writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal bool CanActivateGiarabubMotorMachineGun => CwFreeGunsWorld is
    { GarrisonRemainderContent: not null, MotorMachineGunContent: null }
        && GroupedWorld is not null && AxisGarrisons is not null && Battlefield is not null
        && CompactCheckpoint && FinalizedOwners == 0;
    internal CampaignSourceAuthorityState ActivateGiarabubMotorMachineGun()
    {
        if (!CanActivateGiarabubMotorMachineGun) throw new InvalidOperationException("Motor MG setup activation is unavailable.");
        var active = CwFreeGunsWorld!.ActivateGiarabubMotorMachineGun(AxisGarrisons!, Battlefield!);
        return WithCwFreeGuns(CwFreeGuns!, Battlefield!.ActivateGiarabubMotorMachineGun(World, active.World), active);
    }
    internal byte[] SerializeGiarabubMotorMachineGunEvent(CampaignSourceAuthorityState successor)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", GiarabubMotorMachineGunCheckpointContractVersion);
            writer.WriteString("eventType", "source-giarabub-motor-machine-gun-activated");
            writer.WriteString("campaignId", Request.CampaignId); writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", ActivateGiarabubMotorMachineGunActionId); writer.WriteString("actor", "system");
            writer.WriteString("contentHash", successor.CwFreeGunsWorld!.MotorMachineGunContent!.Identity.Hash);
            writer.WriteString("worldHash", successor.CwFreeGunsWorld.Hash());
            writer.WriteString("groupedWorldHash", successor.GroupedWorld!.Hash());
            writer.WriteString("battlefieldHash", successor.Battlefield!.Hash()); writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal static readonly string GiarabubMotorMgRulingActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.giarabub-motor-mg-ruling.approve.v1"));
    // Axis-only, single acceptance (no Commonwealth vote: this element has no
    // Commonwealth-visible consequence modeled anywhere) of the finding
    // ContentSourceGiarabubMotorMachineGunArtifact already states at creation:
    // the source identifies historical motorization priority but no assigned
    // motor pool or effective current allowance for this element. Available
    // only once the content exists and only before Axis finalizes setup.
    internal bool CanAcceptGiarabubMotorMgRuling => CwFreeGunsWorld is { MotorMachineGunContent: not null, MotorMachineGunRulingAccepted: false }
        && !IsOwnerFinalized(CampaignActionAudience.Axis);
    internal CampaignSourceAuthorityState AcceptGiarabubMotorMgRuling()
    {
        if (!CanAcceptGiarabubMotorMgRuling) throw new InvalidOperationException("Motor MG ruling is unavailable.");
        var active = CwFreeGunsWorld!.AcceptMotorMachineGunRuling();
        return WithCwFreeGuns(CwFreeGuns!, Battlefield!, active);
    }
    internal byte[] SerializeGiarabubMotorMgRulingEvent(CampaignSourceAuthorityState successor)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", GiarabubMotorMachineGunCheckpointContractVersion);
            writer.WriteString("eventType", "source-giarabub-motor-mg-ruling-accepted");
            writer.WriteString("campaignId", Request.CampaignId); writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", GiarabubMotorMgRulingActionId); writer.WriteString("actor", "axis");
            writer.WriteString("rulingId", CampaignSourceCwFreeGunsWorld.MotorMachineGunRulingId);
            writer.WriteString("worldHash", successor.CwFreeGunsWorld!.Hash());
            writer.WriteNumber("stateVersion", successor.StateVersion); writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal bool CanActivateAxisGarrisonRemainder => CwFreeGunsWorld is
    { MinorGarrisonContent: not null, GarrisonRemainderContent: null }
        && GroupedWorld is not null && AxisGarrisons is not null && Battlefield is not null
        && CompactCheckpoint && FinalizedOwners == 0;
    internal CampaignSourceAuthorityState ActivateAxisGarrisonRemainder()
    {
        if (!CanActivateAxisGarrisonRemainder) throw new InvalidOperationException("Garrison remainder activation is unavailable.");
        var active = CwFreeGunsWorld!.ActivateGarrisonRemainder(AxisGarrisons!, Battlefield!);
        return WithCwFreeGuns(CwFreeGuns!, Battlefield!.ActivateAxisGarrisonRemainder(World, active.World), active);
    }
    internal byte[] SerializeAxisGarrisonRemainderEvent(CampaignSourceAuthorityState successor)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", AxisGarrisonRemainderCheckpointContractVersion);
            writer.WriteString("eventType", "source-axis-garrison-remainder-activated");
            writer.WriteString("campaignId", Request.CampaignId); writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", ActivateAxisGarrisonRemainderActionId); writer.WriteString("actor", "system");
            writer.WriteString("contentHash", successor.CwFreeGunsWorld!.GarrisonRemainderContent!.Identity.Hash);
            writer.WriteString("worldHash", successor.CwFreeGunsWorld.Hash());
            writer.WriteString("groupedWorldHash", successor.GroupedWorld!.Hash());
            writer.WriteString("battlefieldHash", successor.Battlefield!.Hash()); writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal bool CanActivateAxisMinorGarrisons => CwFreeGunsWorld is { MinorGarrisonContent: null }
        && GroupedWorld is not null && AxisGarrisons is not null && Battlefield is not null
        && CwFreeGuns is { PendingIds.Count: 0 } && !CanPromoteCwFreeGunsWorld && FinalizedOwners == 0;
    internal CampaignSourceAuthorityState ActivateAxisMinorGarrisons()
    {
        if (!CanActivateAxisMinorGarrisons) throw new InvalidOperationException("Minor garrison activation is unavailable.");
        var active = CwFreeGunsWorld!.ActivateMinorGarrisons(AxisGarrisons!, Battlefield!);
        return WithCwFreeGuns(CwFreeGuns!, Battlefield!.ActivateAxisMinorGarrisons(World, active.World), active);
    }
    internal byte[] SerializeAxisMinorGarrisonsEvent(CampaignSourceAuthorityState successor)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", AxisMinorGarrisonsCheckpointContractVersion);
            writer.WriteString("eventType", "source-axis-minor-garrisons-activated");
            writer.WriteString("campaignId", Request.CampaignId); writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", ActivateAxisMinorGarrisonsActionId); writer.WriteString("actor", "system");
            writer.WriteString("contentHash", successor.CwFreeGunsWorld!.MinorGarrisonContent!.Identity.Hash);
            writer.WriteString("worldHash", successor.CwFreeGunsWorld.Hash());
            writer.WriteString("groupedWorldHash", successor.GroupedWorld!.Hash());
            writer.WriteString("battlefieldHash", successor.Battlefield!.Hash()); writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal static readonly string AdmitSourceTobrukBardiaGarrisonActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.tobruk-bardia-garrison.admit.v1"));
    internal static readonly string ActivateSourceTobrukBardiaGarrisonActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.tobruk-bardia-garrison.activate.v1"));

    // Independent (ungrouped) static-defense counters, admitted the same way
    // as the Axis minor garrisons: a source-evidence Battlefield entry first,
    // then (once CW free guns exist, the file's established late-stage home
    // for further independent-element activation - see ActivateAxisMinorGarrisons
    // and its siblings below) materialization into the active World8.
    internal bool CanAdmitSourceTobrukBardiaGarrison => Battlefield is not null && AxisGarrisons is not null
        && ContentSourceTobrukBardiaGarrisonPackArtifact.AllIds.All(id => Battlefield.Entries
            .Any(value => value.ElementId == id && value.AuthorityStatus == "source-evidence")) && FinalizedOwners == 0;

    internal CampaignSourceAuthorityState AdmitSourceTobrukBardiaGarrison()
    {
        if (!CanAdmitSourceTobrukBardiaGarrison) throw new InvalidOperationException("Tobruk/Bardia garrison requires the prior source profile and no duplicate.");
        var battlefield = Battlefield!.AdmitSourceTobrukBardiaGarrison();
        return (this with { Battlefield = battlefield, StateVersion = checked(StateVersion + 1) }).Validated();
    }

    internal byte[] SerializeSourceTobrukBardiaGarrisonAdmission(CampaignSourceAuthorityState successor)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", SourceTobrukBardiaGarrisonCheckpointContractVersion);
            writer.WriteString("eventType", "source-tobruk-bardia-garrison-admitted");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", AdmitSourceTobrukBardiaGarrisonActionId);
            writer.WriteString("actor", "system");
            writer.WriteString("battlefieldHash", successor.Battlefield!.Hash());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal bool CanActivateSourceTobrukBardiaGarrison => CwFreeGunsWorld is { TobrukBardiaGarrisonContent: null }
        && Battlefield is not null && ContentSourceTobrukBardiaGarrisonPackArtifact.AllIds.All(id => Battlefield.Entries
            .Any(value => value.ElementId == id && value.AuthorityStatus == "source-tobruk-bardia-garrison-admitted")) && CompactCheckpoint && FinalizedOwners == 0;

    internal CampaignSourceAuthorityState ActivateSourceTobrukBardiaGarrison()
    {
        if (!CanActivateSourceTobrukBardiaGarrison) throw new InvalidOperationException("Tobruk/Bardia garrison activation is unavailable.");
        var active = CwFreeGunsWorld!.ActivateTobrukBardiaGarrison(Battlefield!);
        return WithCwFreeGuns(CwFreeGuns!, Battlefield!.ActivateSourceTobrukBardiaGarrison(World, active.World), active);
    }

    internal byte[] SerializeSourceTobrukBardiaGarrisonEvent(CampaignSourceAuthorityState successor)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", SourceTobrukBardiaGarrisonCheckpointContractVersion);
            writer.WriteString("eventType", "source-tobruk-bardia-garrison-activated");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", ActivateSourceTobrukBardiaGarrisonActionId);
            writer.WriteString("actor", "system");
            writer.WriteString("contentHash", successor.CwFreeGunsWorld!.TobrukBardiaGarrisonContent!.Identity.Hash);
            writer.WriteString("worldHash", successor.CwFreeGunsWorld.Hash());
            writer.WriteString("groupedWorldHash", successor.GroupedWorld!.Hash());
            writer.WriteString("battlefieldHash", successor.Battlefield!.Hash());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static readonly string AdmitSourceBardiaGarrisonRemainderActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.bardia-garrison-remainder.admit.v1"));
    internal static readonly string ActivateSourceBardiaGarrisonRemainderActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.bardia-garrison-remainder.activate.v1"));

    // The 5 Bardia elements ContentSourceTobrukBardiaGarrisonPackArtifact's
    // "part 1" pack deliberately left out (see its own comment); admitted the
    // same two-phase way once the Tobruk/Bardia pack itself is active.
    internal bool CanAdmitSourceBardiaGarrisonRemainder => Battlefield is not null
        && CwFreeGunsWorld is { TobrukBardiaGarrisonContent: not null }
        && ContentSourceBardiaGarrisonRemainderPackArtifact.AllIds.All(id => Battlefield.Entries
            .Any(value => value.ElementId == id && value.AuthorityStatus == "source-evidence")) && FinalizedOwners == 0;

    internal CampaignSourceAuthorityState AdmitSourceBardiaGarrisonRemainder()
    {
        if (!CanAdmitSourceBardiaGarrisonRemainder) throw new InvalidOperationException("Bardia garrison remainder requires the admitted Tobruk/Bardia pack and no duplicate.");
        var battlefield = Battlefield!.AdmitSourceBardiaGarrisonRemainder();
        return (this with { Battlefield = battlefield, StateVersion = checked(StateVersion + 1) }).Validated();
    }

    internal byte[] SerializeSourceBardiaGarrisonRemainderAdmission(CampaignSourceAuthorityState successor)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", SourceBardiaGarrisonRemainderCheckpointContractVersion);
            writer.WriteString("eventType", "source-bardia-garrison-remainder-admitted");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", AdmitSourceBardiaGarrisonRemainderActionId);
            writer.WriteString("actor", "system");
            writer.WriteString("battlefieldHash", successor.Battlefield!.Hash());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal bool CanActivateSourceBardiaGarrisonRemainder => CwFreeGunsWorld is { BardiaGarrisonRemainderContent: null }
        && Battlefield is not null && ContentSourceBardiaGarrisonRemainderPackArtifact.AllIds.All(id => Battlefield.Entries
            .Any(value => value.ElementId == id && value.AuthorityStatus == "source-bardia-garrison-remainder-admitted")) && CompactCheckpoint && FinalizedOwners == 0;

    internal CampaignSourceAuthorityState ActivateSourceBardiaGarrisonRemainder()
    {
        if (!CanActivateSourceBardiaGarrisonRemainder) throw new InvalidOperationException("Bardia garrison remainder activation is unavailable.");
        var active = CwFreeGunsWorld!.ActivateBardiaGarrisonRemainder(Battlefield!);
        return WithCwFreeGuns(CwFreeGuns!, Battlefield!.ActivateSourceBardiaGarrisonRemainder(World, active.World), active);
    }

    internal byte[] SerializeSourceBardiaGarrisonRemainderEvent(CampaignSourceAuthorityState successor)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", SourceBardiaGarrisonRemainderCheckpointContractVersion);
            writer.WriteString("eventType", "source-bardia-garrison-remainder-activated");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", ActivateSourceBardiaGarrisonRemainderActionId);
            writer.WriteString("actor", "system");
            writer.WriteString("contentHash", successor.CwFreeGunsWorld!.BardiaGarrisonRemainderContent!.Identity.Hash);
            writer.WriteString("worldHash", successor.CwFreeGunsWorld.Hash());
            writer.WriteString("groupedWorldHash", successor.GroupedWorld!.Hash());
            writer.WriteString("battlefieldHash", successor.Battlefield!.Hash());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static readonly string AdmitSourceTripoliTankRegimentActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.tripoli-tank-regiment.admit.v1"));
    internal static readonly string ActivateSourceTripoliTankRegimentActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.tripoli-tank-regiment.activate.v1"));

    // Independent Tripoli Tank Regiment counters (distinct from the grouped
    // Aresca regiment). Admitted the same two-phase way as Tobruk/Bardia;
    // does not require any other pack to be active first.
    internal bool CanAdmitSourceTripoliTankRegiment => Battlefield is not null && AxisSupplement is not null
        && ContentSourceTripoliTankRegimentPackArtifact.AllIds.All(id => Battlefield.Entries
            .Any(value => value.ElementId == id && value.AuthorityStatus == "source-evidence")) && FinalizedOwners == 0;

    internal CampaignSourceAuthorityState AdmitSourceTripoliTankRegiment()
    {
        if (!CanAdmitSourceTripoliTankRegiment) throw new InvalidOperationException("Tripoli tank regiment requires the prior source profile and no duplicate.");
        var battlefield = Battlefield!.AdmitSourceTripoliTankRegiment();
        return (this with { Battlefield = battlefield, StateVersion = checked(StateVersion + 1) }).Validated();
    }

    internal byte[] SerializeSourceTripoliTankRegimentAdmission(CampaignSourceAuthorityState successor)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", SourceTripoliTankRegimentCheckpointContractVersion);
            writer.WriteString("eventType", "source-tripoli-tank-regiment-admitted");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", AdmitSourceTripoliTankRegimentActionId);
            writer.WriteString("actor", "system");
            writer.WriteString("battlefieldHash", successor.Battlefield!.Hash());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal bool CanActivateSourceTripoliTankRegiment => CwFreeGunsWorld is { TripoliTankRegimentContent: null }
        && Battlefield is not null && ContentSourceTripoliTankRegimentPackArtifact.AllIds.All(id => Battlefield.Entries
            .Any(value => value.ElementId == id && value.AuthorityStatus == "source-tripoli-tank-regiment-admitted")) && CompactCheckpoint && FinalizedOwners == 0;

    internal CampaignSourceAuthorityState ActivateSourceTripoliTankRegiment()
    {
        if (!CanActivateSourceTripoliTankRegiment) throw new InvalidOperationException("Tripoli tank regiment activation is unavailable.");
        var active = CwFreeGunsWorld!.ActivateTripoliTankRegiment(Battlefield!);
        return WithCwFreeGuns(CwFreeGuns!, Battlefield!.ActivateSourceTripoliTankRegiment(World, active.World), active);
    }

    internal byte[] SerializeSourceTripoliTankRegimentEvent(CampaignSourceAuthorityState successor)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", SourceTripoliTankRegimentCheckpointContractVersion);
            writer.WriteString("eventType", "source-tripoli-tank-regiment-activated");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", ActivateSourceTripoliTankRegimentActionId);
            writer.WriteString("actor", "system");
            writer.WriteString("contentHash", successor.CwFreeGunsWorld!.TripoliTankRegimentContent!.Identity.Hash);
            writer.WriteString("worldHash", successor.CwFreeGunsWorld.Hash());
            writer.WriteString("groupedWorldHash", successor.GroupedWorld!.Hash());
            writer.WriteString("battlefieldHash", successor.Battlefield!.Hash());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal static readonly string AdmitSourceCwAttachedArtilleryActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.cw-attached-artillery.admit.v1"));
    internal static readonly string ActivateSourceCwAttachedArtilleryActionId = CampaignPregameSourceCreation.Digest(
        Encoding.UTF8.GetBytes("graziani.source.cw-attached-artillery.activate.v1"));

    // Independent Commonwealth artillery/antitank counters attached to
    // already-admitted independent infantry at the same source placements;
    // admitted the same two-phase way once the CW frontline pack is active.
    internal bool CanAdmitSourceCwAttachedArtillery => Battlefield is not null
        && CwFreeGunsWorld is { CwFrontlineContent: not null }
        && ContentSourceCwAttachedArtilleryPackArtifact.AllIds.All(id => Battlefield.Entries
            .Any(value => value.ElementId == id && value.AuthorityStatus == "source-evidence")) && FinalizedOwners == 0;

    internal CampaignSourceAuthorityState AdmitSourceCwAttachedArtillery()
    {
        if (!CanAdmitSourceCwAttachedArtillery) throw new InvalidOperationException("CW attached artillery requires the admitted CW frontline pack and no duplicate.");
        var battlefield = Battlefield!.AdmitSourceCwAttachedArtillery();
        return (this with { Battlefield = battlefield, StateVersion = checked(StateVersion + 1) }).Validated();
    }

    internal byte[] SerializeSourceCwAttachedArtilleryAdmission(CampaignSourceAuthorityState successor)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", SourceCwAttachedArtilleryCheckpointContractVersion);
            writer.WriteString("eventType", "source-cw-attached-artillery-admitted");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", AdmitSourceCwAttachedArtilleryActionId);
            writer.WriteString("actor", "system");
            writer.WriteString("battlefieldHash", successor.Battlefield!.Hash());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal bool CanActivateSourceCwAttachedArtillery => CwFreeGunsWorld is { CwAttachedArtilleryContent: null }
        && Battlefield is not null && ContentSourceCwAttachedArtilleryPackArtifact.AllIds.All(id => Battlefield.Entries
            .Any(value => value.ElementId == id && value.AuthorityStatus == "source-cw-attached-artillery-admitted")) && CompactCheckpoint && FinalizedOwners == 0;

    internal CampaignSourceAuthorityState ActivateSourceCwAttachedArtillery()
    {
        if (!CanActivateSourceCwAttachedArtillery) throw new InvalidOperationException("CW attached artillery activation is unavailable.");
        var active = CwFreeGunsWorld!.ActivateCwAttachedArtillery(Battlefield!);
        return WithCwFreeGuns(CwFreeGuns!, Battlefield!.ActivateSourceCwAttachedArtillery(World, active.World), active);
    }

    internal byte[] SerializeSourceCwAttachedArtilleryEvent(CampaignSourceAuthorityState successor)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", SourceCwAttachedArtilleryCheckpointContractVersion);
            writer.WriteString("eventType", "source-cw-attached-artillery-activated");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", ActivateSourceCwAttachedArtilleryActionId);
            writer.WriteString("actor", "system");
            writer.WriteString("contentHash", successor.CwFreeGunsWorld!.CwAttachedArtilleryContent!.Identity.Hash);
            writer.WriteString("worldHash", successor.CwFreeGunsWorld.Hash());
            writer.WriteString("groupedWorldHash", successor.GroupedWorld!.Hash());
            writer.WriteString("battlefieldHash", successor.Battlefield!.Hash());
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    private CampaignSourceAuthorityState WithCwFreeGuns(CampaignSourceCwFreeGuns setup, CampaignSourceBattlefield battlefield, CampaignSourceCwFreeGunsWorld? active) =>
        (this with { Battlefield = battlefield, StateVersion = checked(StateVersion + 1), CwFreeGuns = setup, CwFreeGunsWorld = active }).Validated();
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
    private CampaignSourceAuthorityState WithGroupedWorld(CampaignGroupedWorld world, CampaignSourceBattlefield? battlefield = null, int? finalizedOwners = null) =>
        new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces, CommonwealthInitialForces,
            AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans, AxisSupplement, Autoblinda,
            CommonwealthSupplement, PriorityMotorization, AxisPriorityMotorization, AxisGarrisons, battlefield ?? Battlefield, CairoSetup, CairoWorld,
            MalettiWorld, IndependentWorld, LocalMapWorld, checked(StateVersion + 1), ExpandedIndependentWorld, AttachedInitialLoads,
            InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld, AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy,
            CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint,
            AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, world, septemberPlans: SeptemberPlans, finalizedOwners: finalizedOwners ?? FinalizedOwners, finalizationVersion: FinalizationVersion, horseRulingVotes: HorseRulingVotes, horseStageOneNoTraining: HorseStageOneNoTraining, horseRulingVersion: HorseRulingVersion);
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
    internal static readonly string AdmitSeventhArmoredActionId = CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes("graziani.source.seventh-armored-grouped.admit.v1"));
    internal static readonly string ConfirmSeventhArmoredActionId = CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes("graziani.source.seventh-armored.confirm.v1"));
    internal bool IsSeventhArmoredGrouped => GroupedWorld?.Groups.Any(value => value.GroupId == CampaignSourceSeventhArmoredGroupedDefinition.GroupId) == true;
    internal bool IsSeventhShellCorrected => GroupedWorld?.Groups.Any(value => value.GroupId == CampaignSourceSeventhArmoredGroupedDefinition.GroupId
        && value.DispositionConfirmed && value.StackValue == 3) == true;
    internal bool CanCorrectSeventhShell => GroupedWorld?.Groups.Any(value => value.GroupId == CampaignSourceSeventhArmoredGroupedDefinition.GroupId
        && value.DispositionConfirmed && value.StackValue == 5) == true;
    internal static readonly string CorrectSeventhShellActionId = CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes("graziani.source.seventh-armored.shell-stack-correction.v1"));
    internal CampaignSourceAuthorityState CorrectSeventhShell()
    {
        if (!CanCorrectSeventhShell) throw new InvalidOperationException("Seventh Armored shell correction unavailable.");
        // Changed CW stacking requires that owner to re-finalize; Axis remains private and unchanged.
        return WithGroupedWorld(GroupedWorld!.CorrectSeventhArmoredShellStack(), finalizedOwners: FinalizedOwners & ~2);
    }
    internal byte[] SerializeSeventhShellCorrection(CampaignSourceAuthorityState successor)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1); writer.WriteString("eventType", "source-seventh-shell-stack-corrected");
            writer.WriteString("campaignId", Request.CampaignId); writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", CorrectSeventhShellActionId); writer.WriteString("actor", "system");
            writer.WriteString("source", "common-charts:9.4");
            writer.WriteString("sourceHash", "sha256:51aa5a5bfdaca3d23794da71a45a830d98b798d060097cde6418126d6bd63bb0");
            writer.WriteNumber("priorStack", 5); writer.WriteNumber("correctedStack", 3);
            writer.WriteBoolean("commonwealthFinalizationCleared", IsOwnerFinalized(CampaignActionAudience.Commonwealth));
            writer.WriteString("worldHash", successor.GroupedWorld!.Hash()); writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal bool CanAdmitSeventhArmored => IsItalianDivisionsGrouped && !IsSeventhArmoredGrouped && Battlefield is not null && CwFreeGunsWorld is not null;
    internal CampaignSourceAuthorityState AdmitSeventhArmoredGroupedWorld()
    {
        if (!CanAdmitSeventhArmored) throw new InvalidOperationException("Seventh Armored grouped world prerequisites are incomplete.");
        var fourth = ContentSourceFourthIndianPackArtifact.Create(CwFreeGunsWorld!.Content);
        var matruh = ContentSourceMatruhGroupedPackArtifact.Create(fourth);
        var maletti = ContentSourceMalettiGroupedPackArtifact.Create(matruh);
        var libyan = ContentSourceLibyanGroupedPackArtifact.Create(maletti);
        var italian = ContentSourceCireneMarmaricaGroupedPackArtifact.Create(libyan);
        var content = ContentSourceSeventhArmoredGroupedPackArtifact.Create(italian);
        _ = ContentSourceSeventhArmoredGroupedPackArtifact.Read(content.CanonicalBytes);
        var world = GroupedWorld!.AddFormation(content.Hash(), CampaignSourceSeventhArmoredGroupedDefinition.Create(this));
        return WithGroupedWorld(world, Battlefield!.ProposeSeventhArmored(world));
    }
    internal CampaignSourceAuthorityState ConfirmSeventhArmored()
    {
        var prior = GroupedWorld ?? throw new InvalidOperationException("Seventh Armored grouped world unavailable.");
        if (!IsSeventhArmoredGrouped) throw new InvalidOperationException("Seventh Armored grouped world unavailable.");
        var next = prior.ConfirmDisposition(CampaignSourceSeventhArmoredGroupedDefinition.GroupId, 5);
        return WithGroupedWorld(next, Battlefield!.ConfirmSeventhArmored(prior, next));
    }
    internal byte[] SerializeSeventhArmoredGroupedEvent(CampaignSourceAuthorityState successor, bool confirm)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 40);
            writer.WriteString("eventType", confirm ? "source-seventh-armored-disposition-confirmed" : "source-seventh-armored-grouped-world-admitted");
            writer.WriteString("campaignId", Request.CampaignId); writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", confirm ? ConfirmSeventhArmoredActionId : AdmitSeventhArmoredActionId);
            writer.WriteString("actor", confirm ? "commonwealth" : "system");
            writer.WriteString("worldHash", successor.GroupedWorld!.Hash()); writer.WriteString("contentHash", successor.GroupedWorld.ContentHash);
            writer.WriteString("battlefieldHash", successor.Battlefield!.Hash()); writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal static readonly string AdmitSixteenthBrigadeActionId = CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes("graziani.source.sixteenth-brigade-grouped.admit.v1"));
    internal static readonly string ConfirmSixteenthBrigadeActionId = CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes("graziani.source.sixteenth-brigade.confirm.v1"));
    internal bool IsSixteenthBrigadeGrouped => GroupedWorld?.Groups.Any(value => value.GroupId == CampaignSourceSixteenthBrigadeGroupedDefinition.GroupId) == true;
    internal bool CanAdmitSixteenthBrigade => IsSeventhArmoredGrouped && !IsSixteenthBrigadeGrouped && Battlefield is not null && CwFreeGunsWorld is not null;
    internal CampaignSourceAuthorityState AdmitSixteenthBrigadeGroupedWorld()
    {
        if (!CanAdmitSixteenthBrigade) throw new InvalidOperationException("Sixteenth Brigade grouped world prerequisites are incomplete.");
        var fourth = ContentSourceFourthIndianPackArtifact.Create(CwFreeGunsWorld!.Content);
        var matruh = ContentSourceMatruhGroupedPackArtifact.Create(fourth);
        var maletti = ContentSourceMalettiGroupedPackArtifact.Create(matruh);
        var libyan = ContentSourceLibyanGroupedPackArtifact.Create(maletti);
        var italian = ContentSourceCireneMarmaricaGroupedPackArtifact.Create(libyan);
        var seventh = ContentSourceSeventhArmoredGroupedPackArtifact.Create(italian);
        var content = ContentSourceSixteenthBrigadeGroupedPackArtifact.Create(seventh);
        _ = ContentSourceSixteenthBrigadeGroupedPackArtifact.Read(content.CanonicalBytes);
        var world = GroupedWorld!.AddFormation(content.Hash(), CampaignSourceSixteenthBrigadeGroupedDefinition.Create(this));
        return WithGroupedWorld(world, Battlefield!.ProposeSixteenthBrigade(world));
    }
    internal CampaignSourceAuthorityState ConfirmSixteenthBrigade()
    {
        var prior = GroupedWorld ?? throw new InvalidOperationException("Sixteenth Brigade grouped world unavailable.");
        if (!IsSixteenthBrigadeGrouped) throw new InvalidOperationException("Sixteenth Brigade grouped world unavailable.");
        var next = prior.ConfirmDisposition(CampaignSourceSixteenthBrigadeGroupedDefinition.GroupId, 2);
        return WithGroupedWorld(next, Battlefield!.ConfirmSixteenthBrigade(prior, next));
    }
    internal byte[] SerializeSixteenthBrigadeGroupedEvent(CampaignSourceAuthorityState successor, bool confirm)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 41);
            writer.WriteString("eventType", confirm ? "source-sixteenth-brigade-disposition-confirmed" : "source-sixteenth-brigade-grouped-world-admitted");
            writer.WriteString("campaignId", Request.CampaignId); writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", confirm ? ConfirmSixteenthBrigadeActionId : AdmitSixteenthBrigadeActionId);
            writer.WriteString("actor", confirm ? "commonwealth" : "system");
            writer.WriteString("worldHash", successor.GroupedWorld!.Hash()); writer.WriteString("contentHash", successor.GroupedWorld.ContentHash);
            writer.WriteString("battlefieldHash", successor.Battlefield!.Hash()); writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal static readonly string AdmitCatanzaroActionId = CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes("graziani.source.catanzaro-grouped.admit.v1"));
    internal static readonly string ConfirmCatanzaroActionId = CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes("graziani.source.catanzaro.confirm.v1"));
    internal bool IsCatanzaroGrouped => GroupedWorld?.Groups.Any(value => value.GroupId == CampaignSourceCatanzaroGroupedDefinition.GroupId) == true;
    internal bool IsSixthAustralianGrouped => GroupedWorld?.Groups.Any(value => value.GroupId == CampaignSourceSixthAustralianGroupedDefinition.GroupId) == true;
    internal bool IsCcnnBatchGrouped => GroupedWorld?.Groups.Any(value => value.GroupId == CampaignSourceCcnnBatch.FirstId) == true;
    internal bool IsArescaGrouped => GroupedWorld?.Groups.Any(value => value.GroupId == CampaignSourceArescaGroupedDefinition.GroupId) == true;
    internal bool CanAdmitAresca => IsCcnnBatchGrouped && !IsArescaGrouped && !IsOwnerFinalized(CampaignActionAudience.Axis)
        && AxisSupplement is not null && AxisMotorization.Remaining(CampaignSourceArescaGroupedDefinition.PoolId) == 10;
    internal static readonly string ArescaActionId = CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes("graziani.source.aresca.accept.v1"));
    internal CampaignSourceAuthorityState AdmitAresca()
    {
        if (!CanAdmitAresca) throw new InvalidOperationException("Aresca setup is unavailable.");
        var fourth = ContentSourceFourthIndianPackArtifact.Create(CwFreeGunsWorld!.Content);
        var matruh = ContentSourceMatruhGroupedPackArtifact.Create(fourth);
        var maletti = ContentSourceMalettiGroupedPackArtifact.Create(matruh);
        var libyan = ContentSourceLibyanGroupedPackArtifact.Create(maletti);
        var italian = ContentSourceCireneMarmaricaGroupedPackArtifact.Create(libyan);
        var seventh = ContentSourceSeventhArmoredGroupedPackArtifact.Create(italian);
        var sixteenth = ContentSourceSixteenthBrigadeGroupedPackArtifact.Create(seventh);
        var catanzaro = ContentSourceCatanzaroGroupedPackArtifact.Create(sixteenth);
        var content = ContentSourceCcnnBatchPackArtifact.Create(catanzaro, IsSixthAustralianGrouped, true);
        _ = ContentSourceCcnnBatchPackArtifact.Read(content.CanonicalBytes);
        var world = GroupedWorld!.AddFormation(content.Hash(), CampaignSourceArescaGroupedDefinition.Create(this));
        return WithGroupedWorld(world, Battlefield!.AdmitAresca(world));
    }
    internal byte[] SerializeAresca(CampaignSourceAuthorityState successor)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1);
            writer.WriteString("eventType", "source-aresca-admitted"); writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion); writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", ArescaActionId); writer.WriteString("actor", "axis");
            writer.WriteString("worldHash", successor.GroupedWorld!.Hash()); writer.WriteString("contentHash", successor.GroupedWorld.ContentHash);
            writer.WriteString("battlefieldHash", successor.Battlefield!.Hash()); writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal bool CanAdmitCcnnBatch => IsCatanzaroGrouped && !IsCcnnBatchGrouped && !IsOwnerFinalized(CampaignActionAudience.Axis)
        && AxisSupplement is not null && AxisMotorization.Remaining("axis-c4218-attached") == 35
        && AxisMotorization.Remaining("axis-c4507-attached") == 35;
    internal static readonly string CcnnBatchActionId = CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes("graziani.source.ccnn-batch.accept.v1"));
    internal CampaignSourceAuthorityState AdmitCcnnBatch()
    {
        if (!CanAdmitCcnnBatch) throw new InvalidOperationException("CCNN setup is unavailable.");
        var fourth = ContentSourceFourthIndianPackArtifact.Create(CwFreeGunsWorld!.Content);
        var matruh = ContentSourceMatruhGroupedPackArtifact.Create(fourth);
        var maletti = ContentSourceMalettiGroupedPackArtifact.Create(matruh);
        var libyan = ContentSourceLibyanGroupedPackArtifact.Create(maletti);
        var italian = ContentSourceCireneMarmaricaGroupedPackArtifact.Create(libyan);
        var seventh = ContentSourceSeventhArmoredGroupedPackArtifact.Create(italian);
        var sixteenth = ContentSourceSixteenthBrigadeGroupedPackArtifact.Create(seventh);
        var catanzaro = ContentSourceCatanzaroGroupedPackArtifact.Create(sixteenth);
        var content = ContentSourceCcnnBatchPackArtifact.Create(catanzaro, IsSixthAustralianGrouped);
        _ = ContentSourceCcnnBatchPackArtifact.Read(content.CanonicalBytes);
        var world = GroupedWorld!;
        foreach (var formation in CampaignSourceCcnnBatch.Create(this)) world = world.AddFormation(content.Hash(), formation);
        return WithGroupedWorld(world, Battlefield!.AdmitCcnnBatch(world));
    }
    internal byte[] SerializeCcnnBatch(CampaignSourceAuthorityState successor)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1);
            writer.WriteString("eventType", "source-ccnn-batch-admitted"); writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion); writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", CcnnBatchActionId); writer.WriteString("actor", "axis");
            writer.WriteString("worldHash", successor.GroupedWorld!.Hash()); writer.WriteString("contentHash", successor.GroupedWorld.ContentHash);
            writer.WriteString("battlefieldHash", successor.Battlefield!.Hash()); writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal bool CanPlaceSixthAustralian => IsCatanzaroGrouped && !IsSixthAustralianGrouped
        && !IsOwnerFinalized(CampaignActionAudience.Commonwealth);
    internal static string SixthAustralianActionId(string location) =>
        CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes("graziani.source.sixth-australian.place.v1:" + location));
    internal bool CanPlaceSixthAustralianAt(string location)
    {
        if (!CanPlaceSixthAustralian || !CampaignSourceSixthAustralianGroupedDefinition.Locations.Contains(location, StringComparer.Ordinal))
            return false;
        var world = GroupedWorld!;
        var battlefield = Battlefield!;
        var occupants = world.OccupantsAt(location);
        var representedIds = occupants.SelectMany(value => value.Formation?.Members.Select(member => member.ElementId)
            ?? (value.Detached is not null ? [value.Detached.Member.ElementId] : value.Independent!.BoundElementIds)).ToHashSet(StringComparer.Ordinal);
        return !battlefield.Entries.Any(value => value.LocationId == location
                && (value.SideId != "commonwealth" || !representedIds.Contains(value.ElementId)))
            && !occupants.Any(value => value.Formation is { StackValue: null }
                || value.Independent is { BoundElementIds.Count: not 1 }
                || (value.Independent is not null && !CampaignSourceCairoInitialSetup.Units.Any(unit => unit.ElementId == value.Independent.BoundElementIds[0])
                    && !CampaignSourceCwFreeGuns.Units.Any(unit => unit.ElementId == value.Independent.BoundElementIds[0])))
            && occupants.Sum(value => value.Formation?.StackValue ?? value.Detached?.StackValue ?? 1) + 5 <= (location == "e1430" ? 5 : 8);
    }
    internal CampaignSourceAuthorityState PlaceSixthAustralian(string location)
    {
        if (!CanPlaceSixthAustralianAt(location))
            throw new InvalidOperationException("Australian source placement requires known compatible stacking.");
        var world = GroupedWorld!;
        var battlefield = Battlefield!;
        var fourth = ContentSourceFourthIndianPackArtifact.Create(CwFreeGunsWorld!.Content);
        var matruh = ContentSourceMatruhGroupedPackArtifact.Create(fourth);
        var maletti = ContentSourceMalettiGroupedPackArtifact.Create(matruh);
        var libyan = ContentSourceLibyanGroupedPackArtifact.Create(maletti);
        var italian = ContentSourceCireneMarmaricaGroupedPackArtifact.Create(libyan);
        var seventh = ContentSourceSeventhArmoredGroupedPackArtifact.Create(italian);
        var sixteenth = ContentSourceSixteenthBrigadeGroupedPackArtifact.Create(seventh);
        var catanzaro = ContentSourceCatanzaroGroupedPackArtifact.Create(sixteenth);
        var hash = IsCcnnBatchGrouped ? ContentSourceCcnnBatchPackArtifact.Create(catanzaro, true, IsArescaGrouped).Hash()
            : ContentSourceSixthAustralianGroupedPackArtifact.Create(catanzaro).Hash();
        var next = world.AddFormation(hash, CampaignSourceSixthAustralianGroupedDefinition.Create(this, location));
        return WithGroupedWorld(next, battlefield.AddSixthAustralian(next, location));
    }
    internal byte[] SerializeSixthAustralianPlacement(CampaignSourceAuthorityState successor, string location)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1);
            writer.WriteString("eventType", "source-sixth-australian-placed");
            writer.WriteString("campaignId", Request.CampaignId); writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", SixthAustralianActionId(location)); writer.WriteString("actor", "commonwealth");
            writer.WriteString("locationId", location); writer.WriteString("worldHash", successor.GroupedWorld!.Hash());
            writer.WriteString("contentHash", successor.GroupedWorld.ContentHash);
            writer.WriteString("battlefieldHash", successor.Battlefield!.Hash()); writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal bool CanAdmitCatanzaro => IsSixteenthBrigadeGrouped && !IsCatanzaroGrouped && Battlefield is not null && CwFreeGunsWorld is not null;
    internal CampaignSourceAuthorityState AdmitCatanzaroGroupedWorld()
    {
        if (!CanAdmitCatanzaro) throw new InvalidOperationException("Catanzaro grouped world prerequisites are incomplete.");
        var fourth = ContentSourceFourthIndianPackArtifact.Create(CwFreeGunsWorld!.Content);
        var matruh = ContentSourceMatruhGroupedPackArtifact.Create(fourth);
        var maletti = ContentSourceMalettiGroupedPackArtifact.Create(matruh);
        var libyan = ContentSourceLibyanGroupedPackArtifact.Create(maletti);
        var italian = ContentSourceCireneMarmaricaGroupedPackArtifact.Create(libyan);
        var seventh = ContentSourceSeventhArmoredGroupedPackArtifact.Create(italian);
        var sixteenth = ContentSourceSixteenthBrigadeGroupedPackArtifact.Create(seventh);
        var content = ContentSourceCatanzaroGroupedPackArtifact.Create(sixteenth);
        _ = ContentSourceCatanzaroGroupedPackArtifact.Read(content.CanonicalBytes);
        var world = GroupedWorld!.AddFormation(content.Hash(), CampaignSourceCatanzaroGroupedDefinition.Create(this));
        return WithGroupedWorld(world, Battlefield!.ProposeCatanzaro(world));
    }
    internal CampaignSourceAuthorityState ConfirmCatanzaro()
    {
        var prior = GroupedWorld ?? throw new InvalidOperationException("Catanzaro grouped world unavailable.");
        if (!IsCatanzaroGrouped) throw new InvalidOperationException("Catanzaro grouped world unavailable.");
        var next = prior.ConfirmDisposition(CampaignSourceCatanzaroGroupedDefinition.GroupId, 5);
        return WithGroupedWorld(next, Battlefield!.ConfirmCatanzaro(prior, next));
    }
    internal byte[] SerializeCatanzaroGroupedEvent(CampaignSourceAuthorityState successor, bool confirm)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 42);
            writer.WriteString("eventType", confirm ? "source-catanzaro-disposition-confirmed" : "source-catanzaro-grouped-world-admitted");
            writer.WriteString("campaignId", Request.CampaignId); writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", confirm ? ConfirmCatanzaroActionId : AdmitCatanzaroActionId);
            writer.WriteString("actor", confirm ? "axis" : "system");
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
        return (this with { StateVersion = checked(StateVersion + 1), AxisUnattachedCargo = axis ? next : AxisUnattachedCargo, CommonwealthUnattachedCargo = axis ? CommonwealthUnattachedCargo : next }).Validated();
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
        return (this with { StateVersion = checked(StateVersion + 1), CompactCheckpoint = true }).Validated();
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
    internal CampaignSourceAuthorityState PromoteCwPriorityWorld() => AdmitCore(this, StepByKind("CwPriorityWorld"));

    private CampaignSourceAuthorityState PromoteCwPriorityWorldCore()
    {
        var active = CampaignSourceCwPriorityWorld.Create(this);
        var battlefield = Battlefield!.PromoteCwPriorityWorld(CommonwealthSupplement!, World, active.World);
        return (this with { Battlefield = battlefield, StateVersion = checked(StateVersion + 1), CwPriorityWorld = active }).Validated();
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
    internal CampaignSourceAuthorityState AdmitCwPriorityCatalog() => AdmitCore(this, StepByKind("CwPriorityCatalog"));

    private CampaignSourceAuthorityState AdmitCwPriorityCatalogCore() =>
        WithCwPriorityCatalog(CampaignSourceCwPriorityCatalog.Create(this), PriorityMotorization!);
    internal CampaignSourceAuthorityState AdjustCwPriorityCatalog(string id, int delta)
    {
        var catalog = CwPriorityCatalog ?? throw new InvalidOperationException("Commonwealth priority catalog not admitted.");
        var (next, priority) = catalog.Adjust(this, id, delta);
        return WithCwPriorityCatalog(next, priority);
    }
    private CampaignSourceAuthorityState WithCwPriorityCatalog(CampaignSourceCwPriorityCatalog catalog, CampaignSourcePriorityMotorization priority) =>
        (this with { PriorityMotorization = priority, StateVersion = checked(StateVersion + 1), CwPriorityCatalog = catalog }).Validated();
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
    internal CampaignSourceAuthorityState AdmitGt2Correction() => AdmitCore(this, StepByKind("Gt2Correction"));

    private CampaignSourceAuthorityState AdmitGt2CorrectionCore() =>
        WithGt2Correction(CampaignSourceGt2ConvoyCorrection.Create(ConvoyPlan));
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
        (this with { StateVersion = checked(StateVersion + 1), Gt2Correction = correction }).Validated();
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
    internal CampaignSourceAuthorityState PromoteMatruhDetachedWorld() => AdmitCore(this, StepByKind("MatruhDetachedWorld"));

    private CampaignSourceAuthorityState PromoteMatruhDetachedWorldCore()
    {
        MatruhFormation!.Validate(this);
        var active = CampaignSourceMatruhDetachedWorld.Create(this);
        var battlefield = Battlefield!.PromoteMatruhDetached(MatruhFormation, World, active.World);
        return (this with { Battlefield = battlefield, StateVersion = checked(StateVersion + 1), MatruhDetachedWorld = active, CwPriorityWorld = CwPriorityWorld is null || CwFreeGunsWorld is not null ? CwPriorityWorld : CampaignSourceCwPriorityWorld.Rebind(CwPriorityWorld, active), CwFreeGunsWorld = CwFreeGunsWorld is null ? null : CampaignSourceCwFreeGunsWorld.Rebind(CwFreeGunsWorld, active) }).Validated();
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
    internal CampaignSourceAuthorityState AdmitMatruhFormation() => AdmitCore(this, StepByKind("MatruhFormation"));

    private CampaignSourceAuthorityState AdmitMatruhFormationCore()
    {
        MalettiHierarchy!.Validate(this);
        var group = CampaignSourceMatruhFormation.Create(this);
        return WithMatruhFormation(group, Battlefield!.AddMatruhFormation(group));
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
            AxisCorpsArtillery, AxisCorpsArtilleryWorld, MalettiHierarchy, CertifiedMalettiWorld, group, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, groupedWorld ?? GroupedWorld, septemberPlans: SeptemberPlans, finalizedOwners: FinalizedOwners, finalizationVersion: FinalizationVersion, horseRulingVotes: HorseRulingVotes, horseStageOneNoTraining: HorseStageOneNoTraining, horseRulingVersion: HorseRulingVersion);
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
    internal CampaignSourceAuthorityState PromoteCertifiedMalettiWorld() => AdmitCore(this, StepByKind("CertifiedMalettiWorld"));

    private CampaignSourceAuthorityState PromoteCertifiedMalettiWorldCore()
    {
        MalettiHierarchy!.Validate(this);
        var active = CampaignSourceCertifiedMalettiWorld.Create(this);
        var battlefield = Battlefield!.PromoteCertifiedMaletti(MalettiHierarchy, AxisPriorityMotorization!, World, active.World);
        return (this with { Battlefield = battlefield, StateVersion = checked(StateVersion + 1), CertifiedMalettiWorld = active, MatruhDetachedWorld = MatruhDetachedWorld is null || CwPriorityWorld is not null ? MatruhDetachedWorld : CampaignSourceMatruhDetachedWorld.Rebind(MatruhDetachedWorld, active), CwPriorityWorld = CwPriorityWorld is null || CwFreeGunsWorld is not null ? CwPriorityWorld : CampaignSourceCwPriorityWorld.Rebind(CwPriorityWorld, active), CwFreeGunsWorld = CwFreeGunsWorld is null ? null : CampaignSourceCwFreeGunsWorld.Rebind(CwFreeGunsWorld, active) }).Validated();
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
    internal CampaignSourceAuthorityState AdmitMalettiHierarchy() => AdmitCore(this, StepByKind("MalettiHierarchy"));

    private CampaignSourceAuthorityState AdmitMalettiHierarchyCore() =>
        WithMalettiHierarchy(CampaignSourceMalettiHierarchy.Create(this), AxisPriorityMotorization!);
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
            ExpandedIndependentWorld, AttachedInitialLoads, InitialWrecks, AxisFreeArtillery, AxisFreeArtilleryWorld, AxisCorpsArtillery, AxisCorpsArtilleryWorld, hierarchy, CertifiedMalettiWorld, MatruhFormation, MatruhDetachedWorld, Gt2Correction, CwPriorityCatalog, CwPriorityWorld, CompactCheckpoint, AxisUnattachedCargo, CommonwealthUnattachedCargo, CwFreeGuns, CwFreeGunsWorld, grouped ?? GroupedWorld, septemberPlans: SeptemberPlans, finalizedOwners: FinalizedOwners, finalizationVersion: FinalizationVersion, horseRulingVotes: HorseRulingVotes, horseStageOneNoTraining: HorseStageOneNoTraining, horseRulingVersion: HorseRulingVersion);
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
    internal CampaignSourceAuthorityState PromoteAxisCorpsWorld() => AdmitCore(this, StepByKind("AxisCorpsWorld"));

    private CampaignSourceAuthorityState PromoteAxisCorpsWorldCore()
    {
        var active = CampaignSourceAxisCorpsArtilleryWorld.Create(this);
        var battlefield = Battlefield!.PromoteAxisCorpsArtillery(AxisCorpsArtillery!, World, active.World);
        return (this with { Battlefield = battlefield, StateVersion = checked(StateVersion + 1), AxisCorpsArtilleryWorld = active }).Validated();
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
    internal CampaignSourceAuthorityState AdmitAxisCorpsSetup() => AdmitCore(this, StepByKind("AxisCorpsSetup"));

    private CampaignSourceAuthorityState AdmitAxisCorpsSetupCore()
    {
        var setup = CampaignSourceAxisCorpsArtillery.Create(Pregame, Battlefield!);
        return WithAxisCorpsSetup(setup, Battlefield!.AddAxisCorpsArtillery(setup));
    }
    internal CampaignSourceAuthorityState PlaceAxisCorpsUnit(string id, string locationId)
    {
        var setup = AxisCorpsArtillery ?? throw new InvalidOperationException("Axis corps setup not admitted.");
        var battlefield = Battlefield ?? throw new InvalidOperationException("Source battlefield missing.");
        return WithAxisCorpsSetup(setup.Place(id, locationId, battlefield), battlefield.PlaceAxisCorpsArtillery(setup, id, locationId));
    }
    private CampaignSourceAuthorityState WithAxisCorpsSetup(CampaignSourceAxisCorpsArtillery setup, CampaignSourceBattlefield battlefield) =>
        (this with { Battlefield = battlefield, StateVersion = checked(StateVersion + 1), AxisCorpsArtillery = setup }).Validated();
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
    internal CampaignSourceAuthorityState PromoteAxisFreeWorld() => AdmitCore(this, StepByKind("AxisFreeWorld"));

    private CampaignSourceAuthorityState PromoteAxisFreeWorldCore()
    {
        var active = CampaignSourceAxisFreeArtilleryWorld.Create(this);
        var battlefield = Battlefield!.PromoteAxisFreeArtillery(AxisFreeArtillery!, World, active.World);
        return (this with { Battlefield = battlefield, StateVersion = checked(StateVersion + 1), AxisFreeArtilleryWorld = active }).Validated();
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
    internal CampaignSourceAuthorityState AdmitAxisFreeSetup() => AdmitCore(this, StepByKind("AxisFreeSetup"));

    private CampaignSourceAuthorityState AdmitAxisFreeSetupCore()
    {
        var setup = CampaignSourceAxisFreeArtillery.Create(Pregame, Battlefield!);
        return WithAxisFreeSetup(setup, Battlefield!.AddAxisFreeArtillery(setup));
    }
    internal CampaignSourceAuthorityState PlaceAxisFreeUnit(string id, string locationId)
    {
        var setup = AxisFreeArtillery ?? throw new InvalidOperationException("Axis free setup not admitted.");
        var battlefield = Battlefield ?? throw new InvalidOperationException("Source battlefield missing.");
        return WithAxisFreeSetup(setup.Place(id, locationId, battlefield), battlefield.PlaceAxisFreeArtillery(setup, id, locationId));
    }
    private CampaignSourceAuthorityState WithAxisFreeSetup(CampaignSourceAxisFreeArtillery setup, CampaignSourceBattlefield battlefield) =>
        (this with { Battlefield = battlefield, StateVersion = checked(StateVersion + 1), AxisFreeArtillery = setup }).Validated();
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
    internal CampaignSourceAuthorityState AdmitInitialWrecks() => AdmitCore(this, StepByKind("InitialWrecks"));

    private CampaignSourceAuthorityState AdmitInitialWrecksCore() =>
        WithInitialWrecks(CampaignSourceInitialWrecks.Create(Pregame));
    internal CampaignSourceAuthorityState PlaceInitialWreck(string id, string locationId) =>
        WithInitialWrecks((InitialWrecks ?? throw new InvalidOperationException("Initial wrecks not admitted.")).Place(id, locationId));
    private CampaignSourceAuthorityState WithInitialWrecks(CampaignSourceInitialWrecks wrecks) =>
        (this with { StateVersion = checked(StateVersion + 1), InitialWrecks = wrecks }).Validated();
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
    internal CampaignSourceAuthorityState AdmitAttachedInitialLoads() => AdmitCore(this, StepByKind("AttachedInitialLoads"));

    private CampaignSourceAuthorityState AdmitAttachedInitialLoadsCore() =>
        new(Pregame, ConvoyPlan, AxisSupplyWorld, CommonwealthSupplyWorld, AxisInitialForces,
            CommonwealthInitialForces, AxisMotorization, CommonwealthMotorization, OpeningConvoyPlan, CommonwealthSupplyPlans,
            AxisSupplement, Autoblinda, CommonwealthSupplement, PriorityMotorization!, AxisPriorityMotorization, AxisGarrisons,
            Battlefield, CairoSetup, CairoWorld, MalettiWorld, IndependentWorld, LocalMapWorld, checked(StateVersion + 1),
            ExpandedIndependentWorld!, CampaignSourceAttachedInitialLoads.Create(), septemberPlans: SeptemberPlans, finalizedOwners: FinalizedOwners, finalizationVersion: FinalizationVersion, horseRulingVotes: HorseRulingVotes, horseStageOneNoTraining: HorseStageOneNoTraining, horseRulingVersion: HorseRulingVersion);

    internal static string AttachedInitialLoadsActionId(string kind, string elementId) =>
        CampaignPregameSourceCreation.Digest(Encoding.UTF8.GetBytes("graziani.source.attached-initial-loads.choice.v1\0" + kind + "\0" + elementId));

    internal CampaignSourceAuthorityState ApplyAttachedInitialLoads(string kind, string elementId)
    {
        var loads = AttachedInitialLoads ?? throw new InvalidOperationException("Initial loads are not admitted.");
        if (!kind.StartsWith("frontline-", StringComparison.Ordinal)
            && !CampaignSourcePriorityMotorization.Targets.Any(value => value.ElementId == elementId))
            throw new InvalidOperationException("Initial load target is unavailable.");
        if (kind.StartsWith("frontline-", StringComparison.Ordinal)
            && (IsOwnerFinalized(CampaignActionAudience.Commonwealth) || CwFreeGunsWorld?.CwFrontlineContent is null
                || (kind != "frontline-reopen" && loads.FrontlineFinished)
                || (kind is not ("frontline-finish" or "frontline-reopen")
                    && !CampaignSourceAttachedInitialLoads.FrontlineTargets.Any(value => value.ElementId == elementId))
                || (kind is "frontline-finish" or "frontline-reopen") && elementId != "frontline"))
            throw new InvalidOperationException("Frontline load choice is unavailable.");
        var next = kind switch
        {
            "attach" => loads.Attach(elementId, PriorityMotorization!, CommonwealthMotorization),
            "detach" => loads.Detach(elementId),
            "ammo" or "fuel" => loads.Load(elementId, kind),
            "unload-ammo" => loads.ClearLoad(elementId, "ammo"),
            "unload-fuel" => loads.ClearLoad(elementId, "fuel"),
            "frontline-attach" => loads.AttachFrontline(elementId, CommonwealthMotorization),
            "frontline-detach" => loads.Detach(elementId),
            "frontline-ammo" => loads.Load(elementId, "ammo"),
            "frontline-fuel" => loads.Load(elementId, "fuel"),
            "frontline-unload-ammo" => loads.ClearLoad(elementId, "ammo"),
            "frontline-unload-fuel" => loads.ClearLoad(elementId, "fuel"),
            "frontline-finish" => loads.FinishFrontline(CommonwealthMotorization),
            "frontline-reopen" => loads.ReopenFrontline(),
            _ => throw new InvalidOperationException("Unknown initial load choice."),
        };
        return (this with { StateVersion = checked(StateVersion + 1), AttachedInitialLoads = next }).Validated();
    }

    internal byte[] SerializeAttachedInitialLoadsEvent(CampaignSourceAuthorityState successor, string? kind = null, string? elementId = null)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            var frontline = kind?.StartsWith("frontline-", StringComparison.Ordinal) == true;
            writer.WriteNumber("contractVersion", frontline ? FrontlineAttachedInitialLoadsEventContractVersion : AttachedInitialLoadsContractVersion);
            writer.WriteString("eventType", frontline ? "source-frontline-initial-loads-changed"
                : kind is null ? "source-attached-initial-loads-admitted" : "source-attached-initial-loads-changed");
            writer.WriteString("campaignId", Request.CampaignId);
            writer.WriteNumber("priorStateVersion", StateVersion);
            writer.WriteString("priorCheckpointHash", CampaignPregameSourceCreation.Digest(SerializeCheckpoint()));
            writer.WriteString("actionId", kind is null ? AdmitAttachedInitialLoadsActionId : AttachedInitialLoadsActionId(kind, elementId!));
            writer.WriteString("actor", kind is null ? "system" : "commonwealth");
            if (kind is not null) { writer.WriteString("kind", kind); writer.WriteString("elementId", elementId); }
            if (frontline) writer.WriteString("frontlinePolicyId", CampaignSourceAttachedInitialLoads.FrontlinePolicyId);
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

    internal CampaignSourceAuthorityState PromoteExpandedIndependent() => AdmitCore(this, StepByKind("ExpandedIndependentWorld"));

    private CampaignSourceAuthorityState PromoteExpandedIndependentCore()
    {
        var active = CampaignSourceExpandedIndependentWorld.Create(this);
        var battlefield = Battlefield!.PromoteSourceIndependentV2(CommonwealthSupplement!, AxisSupplement!,
            AxisPriorityMotorization!, World, active.World);
        return (this with { Battlefield = battlefield, StateVersion = checked(StateVersion + 1), ExpandedIndependentWorld = active }).Validated();
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

    internal CampaignSourceAuthorityState AdmitLocalMap() => AdmitCore(this, StepByKind("LocalMapWorld"));

    private CampaignSourceAuthorityState AdmitLocalMapCore()
    {
        var map = CampaignSourceLocalMapWorld.Create(this);
        return (this with { LocalMapWorld = map, StateVersion = checked(StateVersion + 1) }).Validated();
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

    internal byte[] SerializeCheckpoint()
    {
        var checkpoint = SerializeBeforeSourcePackCheckpoint();
        if (CwFreeGunsWorld is not { } world) return checkpoint;
        checkpoint = WrapSourcePackCheckpoint(checkpoint, SourceTobrukBardiaGarrisonCheckpointContractVersion, "tobrukBardiaGarrisonContentHash", world.TobrukBardiaGarrisonContent?.Identity.Hash);
        checkpoint = WrapSourcePackCheckpoint(checkpoint, SourceBardiaGarrisonRemainderCheckpointContractVersion, "bardiaGarrisonRemainderContentHash", world.BardiaGarrisonRemainderContent?.Identity.Hash);
        checkpoint = WrapSourcePackCheckpoint(checkpoint, SourceTripoliTankRegimentCheckpointContractVersion, "tripoliTankRegimentContentHash", world.TripoliTankRegimentContent?.Identity.Hash);
        return WrapSourcePackCheckpoint(checkpoint, SourceCwAttachedArtilleryCheckpointContractVersion, "cwAttachedArtilleryContentHash", world.CwAttachedArtilleryContent?.Identity.Hash);
    }

    private byte[] WrapSourcePackCheckpoint(byte[] baseline, int version, string hashName, string? contentHash)
    {
        if (contentHash is null) return baseline;
        if (!CompactCheckpoint) throw new InvalidOperationException("Source packs require compact checkpoint replay.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", version);
            writer.WriteString("campaignId", Request.CampaignId); writer.WriteNumber("stateVersion", StateVersion);
            writer.WriteString("status", "setup-in-progress"); writer.WriteBase64String("baseCheckpoint", baseline);
            writer.WriteString(hashName, contentHash);
            writer.WriteString("nativeWorldHash", CwFreeGunsWorld!.Hash()); writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    private byte[] SerializeBeforeSourcePackCheckpoint()
    {
        var baseline = SerializeBeforeCwFrontlineCheckpoint();
        if (CwFreeGunsWorld?.CwFrontlineContent is not { } content) return baseline;
        if (!CompactCheckpoint) throw new InvalidOperationException("CW frontline requires compact checkpoint replay.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", CwFrontlineCheckpointContractVersion);
            writer.WriteString("campaignId", Request.CampaignId); writer.WriteNumber("stateVersion", StateVersion);
            writer.WriteString("status", "setup-in-progress"); writer.WriteBase64String("baseCheckpoint", baseline);
            writer.WriteString("cwFrontlineContentHash", content.Identity.Hash);
            writer.WriteString("nativeWorldHash", CwFreeGunsWorld.Hash()); writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    private byte[] SerializeBeforeCwFrontlineCheckpoint()
    {
        var baseline = SerializeBeforeMotorMachineGunCheckpoint();
        if (CwFreeGunsWorld?.MotorMachineGunContent is not { } content) return baseline;
        if (!CompactCheckpoint) throw new InvalidOperationException("Motor MG requires compact checkpoint replay.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", GiarabubMotorMachineGunCheckpointContractVersion);
            writer.WriteString("campaignId", Request.CampaignId); writer.WriteNumber("stateVersion", StateVersion);
            writer.WriteString("status", "setup-in-progress"); writer.WriteBase64String("baseCheckpoint", baseline);
            writer.WriteString("motorMachineGunContentHash", content.Identity.Hash);
            writer.WriteString("nativeWorldHash", CwFreeGunsWorld.Hash()); writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    private byte[] SerializeBeforeMotorMachineGunCheckpoint()
    {
        var baseline = SerializeBeforeGarrisonRemainderCheckpoint();
        if (CwFreeGunsWorld?.GarrisonRemainderContent is not { } content) return baseline;
        if (!CompactCheckpoint) throw new InvalidOperationException("Garrison remainder requires compact checkpoint replay.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", AxisGarrisonRemainderCheckpointContractVersion);
            writer.WriteString("campaignId", Request.CampaignId); writer.WriteNumber("stateVersion", StateVersion);
            writer.WriteString("status", "setup-in-progress"); writer.WriteBase64String("baseCheckpoint", baseline);
            writer.WriteString("garrisonRemainderContentHash", content.Identity.Hash);
            writer.WriteString("nativeWorldHash", CwFreeGunsWorld.Hash()); writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    private byte[] SerializeBeforeGarrisonRemainderCheckpoint()
    {
        var baseline = SerializeBeforeMinorGarrisonsCheckpoint();
        if (CwFreeGunsWorld?.MinorGarrisonContent is not { } content) return baseline;
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", AxisMinorGarrisonsCheckpointContractVersion);
            writer.WriteString("campaignId", Request.CampaignId); writer.WriteNumber("stateVersion", StateVersion);
            writer.WriteString("status", "setup-in-progress"); writer.WriteBase64String("baseCheckpoint", baseline);
            writer.WriteString("minorGarrisonContentHash", content.Identity.Hash);
            // Compact checkpoints are reconstructed from the complete event
            // journal, which re-creates and validates this canonical content.
            // Embedding its entire predecessor chain defeats that contract.
            if (!CompactCheckpoint) writer.WriteBase64String("minorGarrisonContent", content.CanonicalBytes);
            writer.WriteString("nativeWorldHash", CwFreeGunsWorld.Hash()); writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    private byte[] SerializeBeforeMinorGarrisonsCheckpoint()
    {
        var baseline = CompactCheckpoint ? SerializeCompactCheckpoint() : SerializeLegacyCheckpoint();
        if (!HorseRulingVersion) return baseline;
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", HorseRulingCheckpointContractVersion);
            writer.WriteString("campaignId", Request.CampaignId); writer.WriteNumber("stateVersion", StateVersion);
            writer.WriteString("status", "setup-in-progress"); writer.WriteNumber("baseProfileVersion", BaseProfileVersion);
            writer.WriteBase64String("baseCheckpoint", baseline); writer.WriteString("rulingId", HorseRulingId);
            writer.WriteNumber("rulingVotes", HorseRulingVotes); writer.WriteBoolean("stageOneNoTraining", HorseStageOneNoTraining);
            writer.WriteNull("priorTrainingProgress"); writer.WriteNull("priorTrainingActive"); writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    private byte[] SerializeCompactCheckpoint()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", BaseProfileVersion);
            writer.WriteString("campaignId", Request.CampaignId); writer.WriteNumber("stateVersion", StateVersion);
            writer.WriteString("rulesetHash", Request.RulesetHash); writer.WriteString("setupHash", Request.SetupHash);
            writer.WriteString("authoritySetupHash", AuthoritySetupHash); writer.WriteString("positionId", SetupPositionId);
            writer.WriteString("status", "setup-in-progress"); writer.WriteNull("gameClock");
            if (FinalizationVersion) writer.WriteNumber("finalizedOwners", FinalizedOwners);
            writer.WriteString("reconstruction", "retained-genesis-and-complete-event-journal-required");
            writer.WriteNumber("legacyProjectionVersion", FinalizationVersion
                ? (IsSeventhShellCorrected ? ShellCorrectedFinalizedLegacyCheckpointContractVersion : FinalizedLegacyCheckpointContractVersion)
                : LegacyProfileVersion);
            writer.WriteString("legacyProjectionHash", DigestLegacyCheckpoint());
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

    internal byte[] SerializeLegacyCheckpoint()
    {
        using var stream = new MemoryStream();
        WriteLegacyCheckpoint(stream);
        return stream.ToArray();
    }

    internal string DigestLegacyCheckpoint()
    {
        using var hash = SHA256.Create();
        using var sink = new CryptoStream(Stream.Null, hash, CryptoStreamMode.Write, leaveOpen: true);
        WriteLegacyCheckpoint(sink);
        sink.FlushFinalBlock();
        return "sha256:" + Convert.ToHexStringLower(hash.Hash!);
    }

    private void WriteLegacyCheckpoint(Stream stream)
    {
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", FinalizationVersion
                ? (IsSeventhShellCorrected ? ShellCorrectedFinalizedLegacyCheckpointContractVersion : FinalizedLegacyCheckpointContractVersion)
                : LegacyProfileVersion);
            if (FinalizationVersion) writer.WriteNumber("baseProfileVersion", LegacyProfileVersion);
            if (IsSeventhShellCorrected)
            {
                writer.WriteNumber("uncorrectedProfileVersion", UnderlyingLegacyProfileVersion);
                writer.WriteString("seventhArmoredShellCorrection", "common-charts:9.4:division-shell-stack-3.v1");
                writer.WriteString("seventhArmoredShellCorrectionSourceHash", "sha256:51aa5a5bfdaca3d23794da71a45a830d98b798d060097cde6418126d6bd63bb0");
            }
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
            if (FinalizationVersion) writer.WriteNumber("finalizedOwners", FinalizedOwners);
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
                var seventhContent = IsSeventhArmoredGrouped ? ContentSourceSeventhArmoredGroupedPackArtifact.Create(italianContent!) : null;
                var sixteenthContent = IsSixteenthBrigadeGrouped ? ContentSourceSixteenthBrigadeGroupedPackArtifact.Create(seventhContent!) : null;
                var catanzaroContent = IsCatanzaroGrouped ? ContentSourceCatanzaroGroupedPackArtifact.Create(sixteenthContent!) : null;
                var australianContent = IsSixthAustralianGrouped && !IsCcnnBatchGrouped ? ContentSourceSixthAustralianGroupedPackArtifact.Create(catanzaroContent!) : null;
                var ccnnContent = IsCcnnBatchGrouped ? ContentSourceCcnnBatchPackArtifact.Create(catanzaroContent!, IsSixthAustralianGrouped, IsArescaGrouped) : null;
                var hash = ccnnContent?.Hash() ?? australianContent?.Hash() ?? catanzaroContent?.Hash() ?? sixteenthContent?.Hash() ?? seventhContent?.Hash() ?? italianContent?.Hash() ?? libyanContent?.Hash() ?? malettiContent?.Hash() ?? matruhContent?.Hash() ?? fourthContent.Hash();
                if (hash != GroupedWorld.ContentHash) throw new InvalidOperationException("Grouped source content identity differs.");
                writer.WriteString("groupedWorldContentHash", hash); writer.WriteBase64String("groupedWorldContent", ccnnContent?.CanonicalBytes ?? australianContent?.CanonicalBytes ?? catanzaroContent?.CanonicalBytes ?? sixteenthContent?.CanonicalBytes ?? seventhContent?.CanonicalBytes ?? italianContent?.CanonicalBytes ?? libyanContent?.CanonicalBytes ?? malettiContent?.CanonicalBytes ?? matruhContent?.CanonicalBytes ?? fourthContent.CanonicalBytes);
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
    }
}
