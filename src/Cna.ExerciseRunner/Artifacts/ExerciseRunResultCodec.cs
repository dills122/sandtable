using System.Text.Json;

namespace Cna.ExerciseRunner.Artifacts;

public static class ExerciseRunResultCodec
{
    private static readonly string[] PropertyNames =
    [
        "contractVersion", "schemeId", "status", "outcome", "failure", "failureAssertion",
    ];

    public static byte[] Serialize(ExerciseRunResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", result.ContractVersion);
            writer.WriteString("schemeId", result.ContractSchemeId);
            switch (result.Completion)
            {
                case ExerciseSucceeded succeeded:
                    writer.WriteString("status", "succeeded");
                    WriteOutcome(writer, succeeded.Outcome);
                    writer.WriteNull("failure");
                    break;
                case ExerciseFailed failed:
                    writer.WriteString("status", "failed");
                    writer.WriteNull("outcome");
                    writer.WriteStartObject("failure");
                    writer.WriteString("category", ExerciseContractText.FormatFailure(failed.Failure.Category));
                    writer.WriteEndObject();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(result));
            }
            WriteFailureAssertion(writer, result.FailureAssertion);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    public static ExerciseRunResult Deserialize(ReadOnlyMemory<byte> canonicalJson)
    {
        try
        {
            using var document = JsonDocument.Parse(canonicalJson);
            var root = document.RootElement;
            StrictJson.RequireExactProperties(root, PropertyNames);
            if (root.GetProperty("contractVersion").GetInt32()
                != ExerciseRunResult.CurrentContractVersion)
                throw new JsonException("Unknown result contract version.");
            if (!string.Equals(
                    root.GetProperty("schemeId").GetString(),
                    ExerciseRunResult.SchemeId,
                    StringComparison.Ordinal))
                throw new JsonException("Unknown result scheme.");

            return root.GetProperty("status").GetString() switch
            {
                "succeeded" => ReadSucceeded(root),
                "failed" => ReadFailed(root),
                _ => throw new JsonException("Unknown result status."),
            };
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException
            or OverflowException
            or FormatException)
        {
            throw new JsonException("The Exercise result is invalid.", exception);
        }
    }

    private static ExerciseRunResult ReadSucceeded(JsonElement root)
    {
        RequireNull(root.GetProperty("failure"), "A successful result cannot contain a failure.");
        RequireNull(
            root.GetProperty("failureAssertion"),
            "A successful result cannot contain a failure assertion result.");
        var outcome = root.GetProperty("outcome");
        StrictJson.RequireExactProperties(outcome, ["kind", "positionId", "victor"]);
        return outcome.GetProperty("kind").GetString() switch
        {
            "boundary-reached" => ExerciseRunResult.Succeeded(new BoundaryReached(
                RequireStringAndNull(outcome, "positionId", "victor"))),
            "victory-reached" => ExerciseRunResult.Succeeded(new VictoryReached(
                RequireStringAndNull(outcome, "victor", "positionId"))),
            _ => throw new JsonException("Unknown terminal outcome."),
        };
    }

    private static ExerciseRunResult ReadFailed(JsonElement root)
    {
        RequireNull(root.GetProperty("outcome"), "A failed result cannot contain an outcome.");
        var failure = root.GetProperty("failure");
        StrictJson.RequireExactProperties(failure, ["category"]);
        var category = ExerciseContractText.ParseFailure(
            failure.GetProperty("category").GetString());
        var assertionElement = root.GetProperty("failureAssertion");
        if (assertionElement.ValueKind == JsonValueKind.Null)
            return ExerciseRunResult.Failed(category, null);
        StrictJson.RequireExactProperties(assertionElement, ["expectedCategory", "matches"]);
        var expected = ExerciseContractText.ParseFailure(
            assertionElement.GetProperty("expectedCategory").GetString());
        var result = ExerciseRunResult.Failed(category, expected);
        if (assertionElement.GetProperty("matches").GetBoolean()
            != result.FailureAssertion!.Matches)
            throw new JsonException("The failure assertion is contradictory.");
        return result;
    }

    private static void WriteOutcome(Utf8JsonWriter writer, ExerciseTerminalOutcome outcome)
    {
        writer.WriteStartObject("outcome");
        switch (outcome)
        {
            case BoundaryReached boundary:
                writer.WriteString("kind", "boundary-reached");
                writer.WriteString("positionId", boundary.PositionId);
                writer.WriteNull("victor");
                break;
            case VictoryReached victory:
                writer.WriteString("kind", "victory-reached");
                writer.WriteNull("positionId");
                writer.WriteString("victor", victory.Victor);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(outcome));
        }
        writer.WriteEndObject();
    }

    private static void WriteFailureAssertion(
        Utf8JsonWriter writer,
        ExerciseFailureAssertion? assertion)
    {
        if (assertion is null)
        {
            writer.WriteNull("failureAssertion");
            return;
        }
        writer.WriteStartObject("failureAssertion");
        writer.WriteString(
            "expectedCategory",
            ExerciseContractText.FormatFailure(assertion.ExpectedCategory));
        writer.WriteBoolean("matches", assertion.Matches);
        writer.WriteEndObject();
    }

    private static string RequireStringAndNull(
        JsonElement element,
        string stringProperty,
        string nullProperty)
    {
        RequireNull(element.GetProperty(nullProperty), $"{nullProperty} must be null.");
        return element.GetProperty(stringProperty).GetString()
            ?? throw new JsonException($"{stringProperty} must be a string.");
    }

    private static void RequireNull(JsonElement element, string message)
    {
        if (element.ValueKind != JsonValueKind.Null) throw new JsonException(message);
    }
}
