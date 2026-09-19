namespace Cna.Core.Content;

internal static class Cna1979CombatContentCatalog
{
    public static ContentPackV7Artifact Artifact { get; } = CreateArtifact();

    private static ContentPackV7Artifact CreateArtifact()
    {
        using var source = typeof(Cna1979CombatContentCatalog).Assembly.GetManifestResourceStream(
            "Cna.Core.Content.Fixtures.combat-content-v7.canonical.json")
            ?? throw new InvalidOperationException("Certified Combat content resource is missing.");
        using var buffer = new MemoryStream();
        source.CopyTo(buffer);
        var parsed = ContentPackV7Serializer.Deserialize(buffer.ToArray());
        if (!parsed.IsSuccess)
        {
            throw new InvalidOperationException(
                $"Certified Combat content is invalid: {parsed.ErrorCode} {parsed.ErrorPath}: {parsed.Message}");
        }
        return ContentPackV7Artifact.Create(parsed.Definition!);
    }
}
