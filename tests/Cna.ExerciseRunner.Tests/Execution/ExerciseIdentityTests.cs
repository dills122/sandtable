using System.Text;
using Cna.ExerciseRunner.Execution;

namespace Cna.ExerciseRunner.Tests.Execution;

public sealed class ExerciseIdentityTests
{
    [Fact]
    public void StandaloneUmpireSeedMatchesTheCanonicalGoldenVector()
    {
        var identity = ExerciseRunIdentity.Standalone("organization-boundary", 0);

        var result = ExerciseSeedDeriver.Derive(
            identity,
            ExerciseSeedDomain.Umpire,
            null);

        Assert.Equal(
            "{\"contractVersion\":1,\"schemeId\":\"sandtable.exercise-seeds.v1\",\"rootSeed\":0,\"maneuverId\":\"standalone.organization-boundary\",\"exerciseOrdinal\":0,\"pairKey\":null,\"domain\":\"umpire\",\"role\":null}",
            Encoding.UTF8.GetString(result.CanonicalMaterial));
        Assert.Equal(
            "sha256:cebcdf8544041b47d2d035c9717f54dd0a4ab26babaf73578f228f9021c43aed",
            result.Digest);
        Assert.Equal(14897027430899522375UL, result.DerivedSeed);
    }

    [Fact]
    public void MaximumSeedAndControllerRoleMatchTheCanonicalGoldenVector()
    {
        var identity = new ExerciseRunIdentity(
            ulong.MaxValue,
            "maneuver-alpha",
            7,
            "pair-a");

        var result = ExerciseSeedDeriver.Derive(
            identity,
            ExerciseSeedDomain.Controller,
            ExerciseSeedRole.Axis);

        Assert.Equal(
            "{\"contractVersion\":1,\"schemeId\":\"sandtable.exercise-seeds.v1\",\"rootSeed\":18446744073709551615,\"maneuverId\":\"maneuver-alpha\",\"exerciseOrdinal\":7,\"pairKey\":\"pair-a\",\"domain\":\"controller\",\"role\":\"axis\"}",
            Encoding.UTF8.GetString(result.CanonicalMaterial));
        Assert.Equal(
            "sha256:2567f87a476e9083dddf614dee40508f9b63ccb15a3ce00e019ee4f55da0b528",
            result.Digest);
        Assert.Equal(2695396106072658051UL, result.DerivedSeed);
    }

    [Fact]
    public void CampaignIdentityExcludesControllerAndVariantIdentity()
    {
        var identity = ExerciseRunIdentity.Standalone("organization-boundary", 0);

        var campaignId = ExerciseCampaignId.Derive(identity);

        Assert.Equal(
            "exercise-656e2c8b1412eb473fddbc86b6ab8230791528b8149580872c13ada219887b08",
            campaignId);
    }

    [Fact]
    public void DomainAndRoleContractsRejectInvalidCombinations()
    {
        var identity = ExerciseRunIdentity.Standalone("organization-boundary", 1);

        Assert.Throws<ArgumentException>(() => ExerciseSeedDeriver.Derive(
            identity,
            ExerciseSeedDomain.Umpire,
            ExerciseSeedRole.Axis));
        Assert.Throws<ArgumentException>(() => ExerciseSeedDeriver.Derive(
            identity,
            ExerciseSeedDomain.Controller,
            null));
    }
}
