using Cna.Core.Hosting;

namespace Cna.Core.Tests.Hosting;

// #580 PR2: the eight Tobruk/Bardia/Tripoli/CW-artillery source-pack steps are
// Host-visible accepted-step kinds with distinct, stable contract versions.
public sealed class CampaignHostSourcePackStepKindTests
{
    private static readonly (string Kind, string WireName, int Version)[] Kinds =
    [
        (CampaignHostAcceptedStep.SourceTobrukBardiaGarrisonAdmissionKind, "source-tobruk-bardia-garrison-admission", CampaignHostAcceptedStep.SourceTobrukBardiaGarrisonAdmissionContractVersion),
        (CampaignHostAcceptedStep.SourceTobrukBardiaGarrisonActivationKind, "source-tobruk-bardia-garrison-activation", CampaignHostAcceptedStep.SourceTobrukBardiaGarrisonActivationContractVersion),
        (CampaignHostAcceptedStep.SourceBardiaGarrisonRemainderAdmissionKind, "source-bardia-garrison-remainder-admission", CampaignHostAcceptedStep.SourceBardiaGarrisonRemainderAdmissionContractVersion),
        (CampaignHostAcceptedStep.SourceBardiaGarrisonRemainderActivationKind, "source-bardia-garrison-remainder-activation", CampaignHostAcceptedStep.SourceBardiaGarrisonRemainderActivationContractVersion),
        (CampaignHostAcceptedStep.SourceTripoliTankRegimentAdmissionKind, "source-tripoli-tank-regiment-admission", CampaignHostAcceptedStep.SourceTripoliTankRegimentAdmissionContractVersion),
        (CampaignHostAcceptedStep.SourceTripoliTankRegimentActivationKind, "source-tripoli-tank-regiment-activation", CampaignHostAcceptedStep.SourceTripoliTankRegimentActivationContractVersion),
        (CampaignHostAcceptedStep.SourceCwAttachedArtilleryAdmissionKind, "source-cw-attached-artillery-admission", CampaignHostAcceptedStep.SourceCwAttachedArtilleryAdmissionContractVersion),
        (CampaignHostAcceptedStep.SourceCwAttachedArtilleryActivationKind, "source-cw-attached-artillery-activation", CampaignHostAcceptedStep.SourceCwAttachedArtilleryActivationContractVersion),
    ];

    [Fact]
    public void EverySourcePackKindIsAnAcceptedStepWithItsOwnDistinctContractVersion()
    {
        byte[] bytes = [1];
        foreach (var (kind, wireName, version) in Kinds)
        {
            Assert.Equal(wireName, kind);
            var step = new CampaignHostAcceptedStep(bytes, [bytes], bytes, bytes, kind);
            Assert.Equal(kind, step.Kind);
            Assert.Equal(version, step.ContractVersion);
        }
        Assert.Equal(8, Kinds.Select(value => value.Version).Distinct().Count());
        Assert.Equal(8, Kinds.Select(value => value.Kind).Distinct(StringComparer.Ordinal).Count());
    }
}
