using System.Text.Json;

namespace Cna.Core.Actions;

/// <summary>A side-private source setup choice, bound to the exact authority revision.</summary>
public sealed record CampaignSourceSetupAction : CampaignActionCandidate
{
    private static readonly HashSet<string> AllowedFields = new(StringComparer.Ordinal)
    { "kind", "elementId", "locationId", "id", "groupId", "lane", "stage", "operationStage", "delta", "targetGameTurn", "target", "dumpId", "placementId", "dummyId", "poolId", "rulingId" };

    internal CampaignSourceSetupAction(long stateVersion, string family, string sourceActionId, JsonElement choice)
        : this(stateVersion, family, sourceActionId, CanonicalChoice(choice)) { }

    private CampaignSourceSetupAction(long stateVersion, string family, string sourceActionId, string canonicalChoice)
        : base("source-setup", Semantics(stateVersion, family, canonicalChoice))
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(stateVersion, 1);
        if (string.IsNullOrWhiteSpace(family) || (sourceActionId.Length != 0 && (!sourceActionId.StartsWith("sha256:", StringComparison.Ordinal) || sourceActionId.Length != 71)))
            throw new ArgumentException("Invalid source setup action identity.");
        StateVersion = stateVersion; Family = family; SourceActionId = sourceActionId; ChoiceJson = canonicalChoice;
    }
    internal long StateVersion { get; }
    public string Family { get; }
    internal string SourceActionId { get; }
    public string ChoiceJson { get; }

    public bool Equals(CampaignSourceSetupAction? other) => other is not null
        && StateVersion == other.StateVersion && Family == other.Family && ChoiceJson == other.ChoiceJson;
    public override int GetHashCode() => HashCode.Combine(StateVersion, Family, ChoiceJson);

    private static string CanonicalChoice(JsonElement choice)
    {
        if (choice.ValueKind != JsonValueKind.Object) throw new ArgumentException("Source choice must be an object.");
        var properties = choice.EnumerateObject().OrderBy(value => value.Name, StringComparer.Ordinal).ToArray();
        if (properties.Select(value => value.Name).Distinct(StringComparer.Ordinal).Count() != properties.Length
            || properties.Any(value => !AllowedFields.Contains(value.Name) || value.Value.ValueKind is not (JsonValueKind.String or JsonValueKind.Number or JsonValueKind.Null)))
            throw new ArgumentException("Source choice contains unsupported fields.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var property in properties) { writer.WritePropertyName(property.Name); property.Value.WriteTo(writer); }
            writer.WriteEndObject();
        }
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }
    private static byte[] Semantics(long stateVersion, string family, string choice)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1); writer.WriteString("kind", "source-setup");
            writer.WriteNumber("stateVersion", stateVersion); writer.WriteString("family", family);
            writer.WritePropertyName("choice"); writer.WriteRawValue(choice); writer.WriteEndObject();
        }
        return stream.ToArray();
    }
}
