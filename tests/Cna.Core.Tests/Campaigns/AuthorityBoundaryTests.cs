using System.Reflection;
using Cna.Core.Campaigns;

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
            typeof(InitiativeResolver),
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
    public void OrleansHostIsTheOnlyProductionProjectThatReferencesCore()
    {
        var root = FindRepositoryRoot();
        var references = Directory.GetFiles(Path.Combine(root, "src"), "*.csproj",
                SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains("../Cna.Core/Cna.Core.csproj",
                StringComparison.Ordinal))
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["Cna.OrleansHost"], references);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Sandtable.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
