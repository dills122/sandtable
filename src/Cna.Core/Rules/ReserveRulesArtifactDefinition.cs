namespace Cna.Core.Rules;

internal sealed record ReserveStatusTransitionDefinition(string From, string To);

internal sealed record ReserveRulesArtifactDefinition
{
    public ReserveRulesArtifactDefinition(
        int schemaVersion,
        string eligibleOwner,
        string assignmentTiming,
        string assignmentResult,
        int capabilityPointCost,
        ReserveStatusTransitionDefinition supportedTransition,
        IEnumerable<RuleReference> sources)
    {
        ArgumentNullException.ThrowIfNull(supportedTransition);
        ArgumentNullException.ThrowIfNull(sources);

        SchemaVersion = schemaVersion;
        EligibleOwner = eligibleOwner;
        AssignmentTiming = assignmentTiming;
        AssignmentResult = assignmentResult;
        CapabilityPointCost = capabilityPointCost;
        SupportedTransition = supportedTransition;
        Sources = Array.AsReadOnly(sources.ToArray());
    }

    public int SchemaVersion { get; }

    public string EligibleOwner { get; }

    public string AssignmentTiming { get; }

    public string AssignmentResult { get; }

    public int CapabilityPointCost { get; }

    public ReserveStatusTransitionDefinition SupportedTransition { get; }

    public IReadOnlyList<RuleReference> Sources { get; }

    public bool Equals(ReserveRulesArtifactDefinition? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && SchemaVersion == other.SchemaVersion
            && string.Equals(EligibleOwner, other.EligibleOwner, StringComparison.Ordinal)
            && string.Equals(AssignmentTiming, other.AssignmentTiming, StringComparison.Ordinal)
            && string.Equals(AssignmentResult, other.AssignmentResult, StringComparison.Ordinal)
            && CapabilityPointCost == other.CapabilityPointCost
            && SupportedTransition == other.SupportedTransition
            && Sources.SequenceEqual(other.Sources));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(SchemaVersion);
        hash.Add(EligibleOwner, StringComparer.Ordinal);
        hash.Add(AssignmentTiming, StringComparer.Ordinal);
        hash.Add(AssignmentResult, StringComparer.Ordinal);
        hash.Add(CapabilityPointCost);
        hash.Add(SupportedTransition);
        foreach (var source in Sources)
        {
            hash.Add(source);
        }
        return hash.ToHashCode();
    }
}

internal static class Cna1979Reserve
{
    public const int SchemaVersion = 1;
    public const string ArtifactId = "cna-1979.1.reserve-designation";
    public const string EmptySelectionRulingId =
        "cna-1979.1.ruling.empty-reserve-designation";

    internal const string EligibleOwner = "resolved-first-acting-side";
    internal const string AssignmentTiming = "reserve-designation";
    internal const string AssignmentResult = "reserve-i";
    internal const int CapabilityPointCost = 0;
    internal const string TransitionFrom = "none";
    internal const string TransitionTo = "reserve-i";

    internal static readonly IReadOnlyList<RuleReference> SourceReferences = Array.AsReadOnly(
        new RuleReference[]
        {
            new("spi-1979-land-rules", "18.11"),
            new("spi-1979-land-rules", "18.12"),
            new("spi-1979-land-rules", "18.15"),
            new("spi-1979-land-rules", "18.21"),
            new("spi-1979-land-rules", "18.26"),
            new("spi-1979-land-rules", "5.2.reserve-designation"),
        });

    public static ReserveRulesArtifactDefinition Definition { get; } = new(
        SchemaVersion,
        EligibleOwner,
        AssignmentTiming,
        AssignmentResult,
        CapabilityPointCost,
        new ReserveStatusTransitionDefinition(TransitionFrom, TransitionTo),
        SourceReferences);

    static Cna1979Reserve()
    {
        ReserveRulesArtifactCodec.Validate(Definition);
    }

    public static RulesetArtifact CreateArtifact()
    {
        var canonical = ReserveRulesArtifactCodec.SerializeCanonical(Definition);
        var contentHash = $"sha256:{Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(canonical)).ToLowerInvariant()}";
        return new RulesetArtifact(ArtifactId, contentHash, Definition.Sources);
    }

    public static Ruling CreateEmptySelectionRuling() => new(
        EmptySelectionRulingId,
        "cna-1979.1.conflict.reserve-designation-minimum",
        [
            "require-at-least-one-reserve-designation",
            "allow-empty-reserve-designation",
        ],
        "allow-empty-reserve-designation",
        ["RES-AC-002", "RES-AC-006", "RES-AC-009"],
        [
            new RuleReference("spi-1979-land-rules", "18.11"),
            new RuleReference("spi-1979-land-rules", "5.2.reserve-designation"),
        ]);
}
