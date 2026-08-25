using System.Reflection;
using Cna.Core.Campaigns;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class AuthorityBoundaryTests
{
    [Fact]
    public void RawAuthorityAndReplayPrimitivesAreNotPublic()
    {
        Type[] forbidden =
        [
            typeof(CampaignSnapshot),
            typeof(CampaignSetupSnapshot),
            typeof(CampaignWorldSnapshot),
            typeof(CampaignContentContext),
            typeof(CampaignCommand),
            typeof(CampaignEvent),
            typeof(CampaignEngine),
            typeof(CampaignProjector),
            typeof(CampaignEventSerializer),
            typeof(CampaignSnapshotSerializer),
            typeof(CampaignReplayPreparation),
            typeof(CampaignReplayHarness),
            typeof(CampaignCreationExecution),
            typeof(Cna.Core.Actions.CampaignActionExecution),
            typeof(InitiativeResolver),
            typeof(CampaignStageEntryPolicy),
            typeof(CampaignStageEntryPolicyCodec),
            typeof(FirstActingSideResolver),
            typeof(StageEntryEventFactory),
            typeof(StageEntryResolved),
            typeof(ResolveNoObligationOrganization),
            typeof(ResolveNoObligationNavalConvoyArrival),
            typeof(ResolveNoObligationFleetAssignment),
            typeof(ResolveNoObligationFleetRepair),
            typeof(NoObligationOrganizationResolved),
            typeof(NoObligationNavalConvoyArrivalResolved),
            typeof(NoObligationFleetAssignmentResolved),
            typeof(NoObligationFleetRepairResolved),
        ];

        Assert.All(forbidden, type =>
        {
            Assert.False(type.IsPublic, type.FullName);
            Assert.False(type.IsNestedPublic, type.FullName);
        });
    }

    [Fact]
    public void OnlyTestsReceiveCoreFriendAccess()
    {
        var friends = typeof(CampaignAuthorityHandle).Assembly
            .GetCustomAttributes<System.Runtime.CompilerServices.InternalsVisibleToAttribute>()
            .Select(value => value.AssemblyName)
            .ToArray();
        Assert.Equal(["Cna.Core.Tests"], friends);
    }

    [Fact]
    public void OnlyApprovedProductionProjectsReferenceCore()
    {
        var root = FindRepositoryRoot();
        var references = Directory.GetFiles(Path.Combine(root, "src"), "*.csproj",
                SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains("../Cna.Core/Cna.Core.csproj",
                StringComparison.Ordinal))
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["Cna.ExerciseRunner", "Cna.OrleansHost"], references);
    }

    [Fact]
    public void DecisionAndIntelligenceSourceDoNotNameStageEntryAuthority()
    {
        var root = FindRepositoryRoot();
        string[] projectDirectories =
        [
            "Cna.DecisionWorker",
            "Cna.Intelligence.Contracts",
            "Cna.Intelligence.Gateway",
        ];
        string[] forbiddenNames =
        [
            nameof(CampaignStageEntryPolicy),
            nameof(FirstActingSideResolver),
            nameof(StageEntryResolved),
            nameof(ResolveNoObligationOrganization),
            nameof(ResolveNoObligationNavalConvoyArrival),
            nameof(ResolveNoObligationFleetAssignment),
            nameof(ResolveNoObligationFleetRepair),
            nameof(NoObligationOrganizationResolved),
            nameof(NoObligationNavalConvoyArrivalResolved),
            nameof(NoObligationFleetAssignmentResolved),
            nameof(NoObligationFleetRepairResolved),
        ];

        var offenders = projectDirectories
            .SelectMany(project => Directory.GetFiles(
                Path.Combine(root, "src", project),
                "*.cs",
                SearchOption.AllDirectories))
            .Where(path => forbiddenNames.Any(name =>
                File.ReadAllText(path).Contains(name, StringComparison.Ordinal)))
            .ToArray();

        Assert.Empty(offenders);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Sandtable.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
