namespace Cna.Core.Content;

internal static class Cna1979BreakdownContentCatalog
{
    public static ContentPackV6Artifact Artifact { get; } = CreateArtifact("rules-lab.content.breakdown-truck.v1.json");

    public static ContentPackV6Artifact ReactionArtifact { get; } = CreateArtifact("rules-lab.content.breakdown-reaction.v1.json");

    private static ContentPackV6Artifact CreateArtifact(string resourceName)
    {
        using var source = typeof(Cna1979BreakdownContentCatalog).Assembly.GetManifestResourceStream(
            $"Cna.Core.Content.Fixtures.{resourceName}")
            ?? throw new InvalidOperationException("Certified Breakdown content resource is missing.");
        using var buffer = new MemoryStream();
        source.CopyTo(buffer);
        var parsed = ContentPackV6Serializer.Deserialize(buffer.ToArray());
        if (!parsed.IsSuccess)
            throw new InvalidOperationException($"Certified Breakdown content is invalid: {parsed.ErrorCode}: {parsed.Message}");
        return ContentPackV6Artifact.Create(parsed.Definition!);
    }
}
