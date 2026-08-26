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
}
