using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Execution;
using Cna.ExerciseRunner.Tests.Artifacts;

namespace Cna.ExerciseRunner.Tests.Execution;

public sealed class ExerciseConfigurationIdentityTests
{
    [Fact]
    public void VersionTwoFirstByIdConfigurationMatchesTheCanonicalGolden()
    {
        Assert.Equal(2, ExerciseConfigurationIdentity.CurrentContractVersion);
        Assert.Equal(
            "sandtable.exercise-controller-configuration.v2",
            ExerciseConfigurationIdentity.SchemeId);
        Assert.Equal(
            "sha256:38ed28be6562e5d5967d838b0d264c3b52bcae77a5e61d122a282b7b91c16f0b",
            ExerciseConfigurationIdentity.ComputeHash(
                ExerciseManifestCodecTests.Create()));
    }

    [Fact]
    public void SemanticReserveConfigurationMatchesTheCanonicalGolden()
    {
        var manifest = ExerciseManifestCodecTests.Create(controllerPolicy:
            ExerciseControllerPolicy.DesignateAllReservesThenFirstByActionId);

        Assert.Equal(
            "sha256:ce7818b1b6461861f859058cfe006a493605c06bd90f7ecab5eebb0554811e40",
            ExerciseConfigurationIdentity.ComputeHash(manifest));
    }

    [Fact]
    public void ControllerMatrixPoliciesHaveDistinctConfigurationIdentities()
    {
        string[] policyNames =
        [
            "ActFirstReserveNoneThenFirstByActionId",
            "ActFirstReserveOneThenFirstByActionId",
            "ActFirstReserveAllThenFirstByActionId",
            "ActLastReserveNoneThenFirstByActionId",
            "ActLastReserveOneThenFirstByActionId",
            "ActLastReserveAllThenFirstByActionId",
        ];

        var hashes = policyNames.Select(name => ExerciseConfigurationIdentity.ComputeHash(
            ExerciseManifestCodecTests.Create(
                controllerPolicy: Enum.Parse<ExerciseControllerPolicy>(name)))).ToArray();

        Assert.Equal(policyNames.Length, hashes.Distinct(StringComparer.Ordinal).Count());
        Assert.All(hashes, value => Assert.StartsWith("sha256:", value, StringComparison.Ordinal));
    }

    [Fact]
    public void BoundedMovementPoliciesHaveDistinctVersionTwoConfigurationIdentities()
    {
        string[] policyNames =
        [
            "ActFirstReserveNoneMoveEachOnceThenComplete",
            "ActFirstReserveOneMoveEachOnceThenComplete",
            "ActFirstReserveAllMoveEachOnceThenComplete",
            "ActLastReserveNoneMoveEachOnceThenComplete",
            "ActLastReserveOneMoveEachOnceThenComplete",
            "ActLastReserveAllMoveEachOnceThenComplete",
            "ActFirstReserveNoneMoveEachOnceByLowestCostThenComplete",
        ];

        var hashes = policyNames.Select(name => ExerciseConfigurationIdentity.ComputeHash(
            ExerciseManifestCodecTests.Create(
                controllerPolicy: Enum.Parse<ExerciseControllerPolicy>(name)))).ToArray();

        Assert.Equal(2, ExerciseConfigurationIdentity.CurrentContractVersion);
        Assert.Equal(
            "sandtable.exercise-controller-configuration.v2",
            ExerciseConfigurationIdentity.SchemeId);
        Assert.Equal(
            [
                "sha256:14e17e9c70df705c679901d2478dd39f69f44c75c81514ca30204c2c731ff094",
                "sha256:374ec522b356337748af50becc9f7b7e8308759aa249dd676470222ba2680dcb",
                "sha256:f6e6ad230de28b8b18a634c65d235afb16c4bd828b751b03584ee3bd1cf8b03d",
                "sha256:b4f855497b740d8091f506734587063cf45825f5bf6054fb7c29c4ee16efbae0",
                "sha256:73f644a20a1625fab920043c53e7271c2b4dbd2242c20560fc385c8bc8dae241",
                "sha256:0c83960b9c2a68b05f2122e97ac4a037430b0ea507b06dba123ce59d811cd9f9",
                "sha256:b7bc95b9754073f9814d93ebaa56f6223d394afb189083170ddc88d1f08733bb",
            ],
            hashes);
    }
}
