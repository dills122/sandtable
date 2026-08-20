using Cna.Core.Rules;

namespace Cna.ExerciseRunner.Artifacts;

public sealed record BuildArtifactIdentity
{
    internal BuildArtifactIdentity(string name, long sizeBytes, string sha256)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name is "." or ".." || name.Contains('/') || name.Contains('\\'))
            throw new ArgumentException("Artifact identity names must be one stable segment.", nameof(name));
        ArgumentOutOfRangeException.ThrowIfNegative(sizeBytes);
        ReplayProofValidation.RequireSha256(sha256, nameof(sha256));
        Name = name;
        SizeBytes = sizeBytes;
        Sha256 = sha256;
    }

    public string Name { get; }
    public long SizeBytes { get; }
    public string Sha256 { get; }
}

public sealed class BuildIdentity
{
    public const int CurrentContractVersion = 1;
    public const string SchemeId = ArtifactSchema.BuildIdentitySchemaId;

    internal BuildIdentity(
        ExerciseBuildMode buildMode,
        string headCommit,
        string headTree,
        bool dirty,
        string porcelainSha256,
        string frameworkDescription,
        string osArchitecture,
        string processArchitecture,
        string rulesetHash,
        string configurationHash,
        string manifestHash,
        string seedSchemeId,
        bool baselineEligible,
        bool reproducible,
        IEnumerable<BuildArtifactIdentity> artifacts)
    {
        if (!Enum.IsDefined(buildMode)) throw new ArgumentOutOfRangeException(nameof(buildMode));
        RequireGitObjectId(headCommit, nameof(headCommit));
        RequireGitObjectId(headTree, nameof(headTree));
        ReplayProofValidation.RequireSha256(porcelainSha256, nameof(porcelainSha256));
        if (!Cna1979Ruleset.IsCanonicalHash(rulesetHash))
            throw new ArgumentException("The ruleset hash is unsupported.", nameof(rulesetHash));
        ReplayProofValidation.RequireSha256(configurationHash, nameof(configurationHash));
        ReplayProofValidation.RequireSha256(manifestHash, nameof(manifestHash));
        ArgumentException.ThrowIfNullOrWhiteSpace(frameworkDescription);
        ArgumentException.ThrowIfNullOrWhiteSpace(osArchitecture);
        ArgumentException.ThrowIfNullOrWhiteSpace(processArchitecture);
        if (!string.Equals(
                seedSchemeId,
                ExerciseSeedLedger.SchemeId,
                StringComparison.Ordinal))
            throw new ArgumentException("The seed scheme is unsupported.", nameof(seedSchemeId));
        var validPolicy = buildMode switch
        {
            ExerciseBuildMode.Baseline => !dirty && baselineEligible && reproducible,
            ExerciseBuildMode.Exploratory => !baselineEligible && (!dirty || !reproducible),
            _ => false,
        };
        if (!validPolicy)
            throw new ArgumentException("Build mode and eligibility flags are contradictory.");
        ArgumentNullException.ThrowIfNull(artifacts);
        var copy = artifacts.ToArray();
        if (copy.Length == 0
            || copy.Any(value => value is null)
            || copy.Select(value => value.Name).Distinct(StringComparer.Ordinal).Count()
                != copy.Length)
            throw new ArgumentException("Executed artifact identities must be nonempty and unique.", nameof(artifacts));

        ContractVersion = CurrentContractVersion;
        ContractSchemeId = SchemeId;
        BuildMode = buildMode;
        HeadCommit = headCommit;
        HeadTree = headTree;
        Dirty = dirty;
        PorcelainSha256 = porcelainSha256;
        FrameworkDescription = frameworkDescription;
        OsArchitecture = osArchitecture;
        ProcessArchitecture = processArchitecture;
        RulesetHash = rulesetHash;
        ConfigurationHash = configurationHash;
        ManifestHash = manifestHash;
        SeedSchemeId = seedSchemeId;
        BaselineEligible = baselineEligible;
        Reproducible = reproducible;
        Artifacts = Array.AsReadOnly(copy.OrderBy(value => value.Name, StringComparer.Ordinal).ToArray());
    }

    public int ContractVersion { get; }
    public string ContractSchemeId { get; }
    public ExerciseBuildMode BuildMode { get; }
    public string HeadCommit { get; }
    public string HeadTree { get; }
    public bool Dirty { get; }
    public string PorcelainSha256 { get; }
    public string FrameworkDescription { get; }
    public string OsArchitecture { get; }
    public string ProcessArchitecture { get; }
    public string RulesetHash { get; }
    public string ConfigurationHash { get; }
    public string ManifestHash { get; }
    public string SeedSchemeId { get; }
    public bool BaselineEligible { get; }
    public bool Reproducible { get; }
    public IReadOnlyList<BuildArtifactIdentity> Artifacts { get; }

    private static void RequireGitObjectId(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Length is not (40 or 64)
            || value.Any(character => character is not (>= '0' and <= '9')
                and not (>= 'a' and <= 'f')))
            throw new ArgumentException("A lowercase Git object ID is required.", parameterName);
    }
}
