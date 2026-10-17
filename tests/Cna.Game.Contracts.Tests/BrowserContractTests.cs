using System.Text.Json;
using System.Text.RegularExpressions;

namespace Cna.Game.Contracts.Tests;

public sealed class BrowserContractTests
{
    private static readonly string[] SubmissionFields =
    [
        "actionContractVersion", "actionId", "contractVersion",
        "expectedPositionId", "expectedStateVersion", "requestId", "sessionGeneration"
    ];

    private static readonly JsonDocument Contract = JsonDocument.Parse(File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "openapi.v1.json")));

    [Fact]
    public void EveryLocalReferenceResolvesAndEveryOperationHasExplicitSecurity()
    {
        var root = Contract.RootElement;
        Assert.Equal("3.0.3", root.GetProperty("openapi").GetString());
        var schemas = root.GetProperty("components").GetProperty("schemas");
        foreach (var property in schemas.EnumerateObject())
            AssertReferences(property.Value, schemas);
        foreach (var path in root.GetProperty("paths").EnumerateObject())
            foreach (var operation in path.Value.EnumerateObject())
            {
                Assert.True(operation.Value.TryGetProperty("security", out _) ||
                    path.Name is "/api/v1/bootstrap" or "/api/v1/fixtures");
                AssertReferences(operation.Value, schemas);
            }
    }

    [Fact]
    public void CoherentViewRoundtripsWideRevisionsAndRejectsHiddenData()
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
            "Fixtures", "coherent-view.v1.json"));
        using var view = JsonDocument.Parse(bytes);
        AssertSchema(view.RootElement, "CoherentPlayerView");
        Assert.Equal("9223372036854775807",
            view.RootElement.GetProperty("stateVersion").GetString());
        Assert.Equal("9007199254740993",
            view.RootElement.GetProperty("audienceRevision").GetString());
        using var reparsed = JsonDocument.Parse(JsonSerializer.SerializeToUtf8Bytes(view.RootElement));
        Assert.Equal(view.RootElement.GetProperty("stateVersion").GetString(),
            reparsed.RootElement.GetProperty("stateVersion").GetString());

        var poisoned = JsonSerializer.Deserialize<Dictionary<string, object>>(bytes)!;
        poisoned["rngSeed"] = "secret";
        using var hidden = JsonDocument.Parse(JsonSerializer.Serialize(poisoned));
        Assert.Throws<InvalidDataException>(() => AssertSchema(hidden.RootElement,
            "CoherentPlayerView"));

        using var nestedHidden = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
                "Fixtures", "coherent-view.v1.json"))
            .Replace("\"formationId\":\"own-1\"", "\"formationId\":\"own-1\",\"hiddenEnemyId\":\"secret\"", StringComparison.Ordinal));
        Assert.Throws<InvalidDataException>(() => AssertSchema(nestedHidden.RootElement,
            "CoherentPlayerView"));

        using var numericRevision = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
                "Fixtures", "coherent-view.v1.json"))
            .Replace("\"9223372036854775807\"", "9223372036854775807", StringComparison.Ordinal));
        Assert.Throws<InvalidDataException>(() => AssertSchema(numericRevision.RootElement,
            "CoherentPlayerView"));
    }

    [Fact]
    public void SubmissionHasNoActorOrClientSuppliedOutcome()
    {
        var schemas = Contract.RootElement.GetProperty("components").GetProperty("schemas");
        var submission = schemas.GetProperty("SubmitActionRequest").GetProperty("properties");
        Assert.Equal(SubmissionFields,
            submission.EnumerateObject().Select(p => p.Name).OrderBy(p => p, StringComparer.Ordinal));
        Assert.False(submission.TryGetProperty("actor", out _));
        Assert.False(submission.TryGetProperty("audience", out _));
        Assert.False(submission.TryGetProperty("payload", out _));
    }

    private static void AssertReferences(JsonElement node, JsonElement schemas)
    {
        if (node.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in node.EnumerateObject())
            {
                if (property.Name == "$ref")
                {
                    var reference = property.Value.GetString()!;
                    Assert.StartsWith("#/components/schemas/", reference);
                    Assert.True(schemas.TryGetProperty(reference.Split('/')[^1], out _));
                }
                else AssertReferences(property.Value, schemas);
            }
        }
        else if (node.ValueKind == JsonValueKind.Array)
            foreach (var item in node.EnumerateArray()) AssertReferences(item, schemas);
    }

    private static void AssertSchema(JsonElement value, string name)
    {
        var schemas = Contract.RootElement.GetProperty("components").GetProperty("schemas");
        AssertNode(value, schemas.GetProperty(name), schemas);
    }

    private static void AssertNode(JsonElement value, JsonElement schema, JsonElement schemas)
    {
        if (schema.TryGetProperty("$ref", out var reference))
        {
            AssertNode(value, schemas.GetProperty(reference.GetString()!.Split('/')[^1]), schemas);
            return;
        }
        var type = schema.GetProperty("type").GetString();
        if (type == "object")
        {
            if (value.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Expected object.");
            var properties = schema.GetProperty("properties");
            foreach (var required in schema.GetProperty("required").EnumerateArray())
                if (!value.TryGetProperty(required.GetString()!, out _))
                    throw new InvalidDataException($"Missing {required.GetString()}.");
            foreach (var property in value.EnumerateObject())
            {
                if (!properties.TryGetProperty(property.Name, out var field))
                    throw new InvalidDataException($"Forbidden field {property.Name}.");
                AssertNode(property.Value, field, schemas);
            }
        }
        else if (type == "array")
        {
            if (value.ValueKind != JsonValueKind.Array) throw new InvalidDataException("Expected array.");
            foreach (var item in value.EnumerateArray())
                AssertNode(item, schema.GetProperty("items"), schemas);
        }
        else if (type == "string")
        {
            if (value.ValueKind != JsonValueKind.String) throw new InvalidDataException("Expected string.");
            if (schema.TryGetProperty("pattern", out var pattern) &&
                !Regex.IsMatch(value.GetString()!, pattern.GetString()!, RegexOptions.CultureInvariant))
                throw new InvalidDataException("Pattern mismatch.");
        }
        else if (type == "integer" && value.ValueKind != JsonValueKind.Number)
            throw new InvalidDataException("Expected integer.");
        else if (type == "number" && value.ValueKind != JsonValueKind.Number)
            throw new InvalidDataException("Expected number.");
        if (schema.TryGetProperty("enum", out var choices) &&
            !choices.EnumerateArray().Any(choice => choice.ToString() == value.ToString()))
            throw new InvalidDataException("Enum mismatch.");
    }
}
