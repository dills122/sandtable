using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Cna.Game.Contracts.Tests;

public sealed class BrowserContractTests
{
    private static readonly JsonDocument Contract = JsonDocument.Parse(File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "openapi.v1.json")));
    private static readonly JsonDocument SignalR = JsonDocument.Parse(File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "signalr.v1.json")));
    private static readonly string[] SubmissionFields =
    [
        "actionContractVersion", "actionId", "actionSetToken", "contractVersion",
        "expectedPositionId", "requestId", "sessionGeneration"
    ];

    [Fact]
    public void EveryReferenceResolvesAndMutationsRequireCookieAndCsrfTogether()
    {
        var root = Contract.RootElement;
        Assert.Equal("3.0.3", root.GetProperty("openapi").GetString());
        Assert.Equal("signalr.v1.json", root.GetProperty("x-signalr-contract").GetString());
        Assert.Empty(root.GetProperty("security").EnumerateArray());
        var schemas = root.GetProperty("components").GetProperty("schemas");
        foreach (var property in schemas.EnumerateObject())
            AssertReferences(property.Value, schemas);
        foreach (var path in root.GetProperty("paths").EnumerateObject())
            foreach (var operation in path.Value.EnumerateObject())
            {
                AssertReferences(operation.Value, schemas);
                var isPublic = path.Name is "/api/v1/bootstrap" or "/api/v1/fixtures";
                if (isPublic)
                {
                    Assert.False(operation.Value.TryGetProperty("security", out _));
                    continue;
                }
                var alternatives = operation.Value.GetProperty("security").EnumerateArray().ToArray();
                var oneAndOnly = Assert.Single(alternatives);
                var required = oneAndOnly.EnumerateObject().Select(p => p.Name).OrderBy(p => p,
                    StringComparer.Ordinal).ToArray();
                var cookie = path.Name == "/api/v1/campaigns" ||
                    (path.Name.EndsWith("/seat", StringComparison.Ordinal) && operation.Name == "post")
                    ? "operatorCookie" : "seatCookie";
                var expected = operation.Name is "post" or "delete"
                    ? new[] { cookie, "csrfHeader" }.OrderBy(p => p, StringComparer.Ordinal).ToArray()
                    : [cookie];
                Assert.Equal(expected, required);
                foreach (var credential in oneAndOnly.EnumerateObject())
                    Assert.Empty(credential.Value.EnumerateArray());
            }
    }

    [Fact]
    public void LocalBootstrapAndSeatErrorResponsesAreTyped()
    {
        var paths = Contract.RootElement.GetProperty("paths");
        var bootstrap = paths.GetProperty("/api/v1/bootstrap").GetProperty("get");
        Assert.Contains("positive same-origin Fetch Metadata",
            bootstrap.GetProperty("description").GetString());
        var bootstrapResponses = bootstrap.GetProperty("responses");
        Assert.Equal(["200", "403", "503"], bootstrapResponses.EnumerateObject()
            .Select(response => response.Name).ToArray());
        AssertApiErrors(bootstrapResponses, "403", "503");

        var seatResponses = paths.GetProperty("/api/v1/campaigns/{campaignId}/seat")
            .GetProperty("post").GetProperty("responses");
        Assert.Equal(["200", "400", "401", "403", "409"], seatResponses
            .EnumerateObject().Select(response => response.Name).ToArray());
        AssertApiErrors(seatResponses, "400", "401", "403", "409");
    }

    private static void AssertApiErrors(JsonElement responses, params string[] statuses)
    {
        foreach (var status in statuses)
            Assert.Equal("#/components/schemas/ApiError", responses.GetProperty(status)
                .GetProperty("content").GetProperty("application/json")
                .GetProperty("schema").GetProperty("$ref").GetString());
    }

    [Fact]
    public void CreationRequiresPinnedHashesAndExactlyTwoDistinctSeatGrants()
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
            "Fixtures", "campaign-created.v1.json"));
        using var created = JsonDocument.Parse(bytes);
        AssertSchema(created.RootElement, "CampaignCreated");
        var grantSchema = Contract.RootElement.GetProperty("components").GetProperty("schemas")
            .GetProperty("SeatGrantSet");
        Assert.Equal(["axis", "commonwealth"], grantSchema.GetProperty("required")
            .EnumerateArray().Select(value => value.GetString()).ToArray());
        Assert.Equal(["axis", "commonwealth"], grantSchema.GetProperty("x-distinct-values")
            .EnumerateArray().Select(value => value.GetString()).ToArray());

        var invalid = JsonNode.Parse(bytes)!;
        invalid["seatGrants"] = new JsonObject();
        AssertRejects(invalid, "CampaignCreated");
        invalid = JsonNode.Parse(bytes)!;
        invalid["seatGrants"]!.AsObject().Remove("axis");
        AssertRejects(invalid, "CampaignCreated");
        invalid = JsonNode.Parse(bytes)!;
        invalid["seatGrants"]!["commonwealth"] = invalid["seatGrants"]!["axis"]!.GetValue<string>();
        AssertRejects(invalid, "CampaignCreated");
        invalid = JsonNode.Parse(bytes)!;
        invalid["seatGrants"]!["system"] = "d2448ca5-07f2-4dfe-a8fd-06d78b9ff40d";
        AssertRejects(invalid, "CampaignCreated");
        foreach (var field in new[] { "rulesetHash", "setupHash", "contentHash", "configurationHash" })
        {
            invalid = JsonNode.Parse(bytes)!;
            invalid[field] = "not-a-lowercase-sha256";
            AssertRejects(invalid, "CampaignCreated");
            invalid = JsonNode.Parse(bytes)!;
            invalid[field] = new string('A', 64);
            AssertRejects(invalid, "CampaignCreated");
        }
    }

    [Fact]
    public void CoherentViewCarriesOnlyAudienceRevisionAndOpaqueActionCapability()
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
            "Fixtures", "coherent-view.v1.json"));
        using var view = JsonDocument.Parse(bytes);
        AssertSchema(view.RootElement, "CoherentPlayerView");
        Assert.Equal("9007199254740993", view.RootElement.GetProperty("audienceRevision").GetString());
        Assert.Equal("9bf1a814-2e55-4f1e-8646-cf7219caa69a",
            view.RootElement.GetProperty("actionSetToken").GetString());
        Assert.False(view.RootElement.TryGetProperty("stateVersion", out _));
        using var reparsed = JsonDocument.Parse(JsonSerializer.SerializeToUtf8Bytes(view.RootElement));
        Assert.Equal(view.RootElement.GetProperty("audienceRevision").GetString(),
            reparsed.RootElement.GetProperty("audienceRevision").GetString());

        var hidden = JsonNode.Parse(bytes)!;
        hidden["rngSeed"] = "secret";
        AssertRejects(hidden, "CoherentPlayerView");
        hidden = JsonNode.Parse(bytes)!;
        hidden["ownFormations"]![0]!["hiddenEnemyId"] = "secret";
        AssertRejects(hidden, "CoherentPlayerView");
        hidden = JsonNode.Parse(bytes)!;
        hidden["audienceRevision"] = long.MaxValue;
        AssertRejects(hidden, "CoherentPlayerView");
        hidden = JsonNode.Parse(bytes)!;
        hidden["actionSetToken"] = "not-an-opaque-uuid";
        AssertRejects(hidden, "CoherentPlayerView");
        hidden = JsonNode.Parse(bytes)!;
        hidden["warDiary"]![0]!["body"] = new string('x', 1025);
        AssertRejects(hidden, "CoherentPlayerView");
    }

    [Fact]
    public void SubmissionDoesNotAcceptAuthorityRevisionOrCallerActor()
    {
        var schemas = Contract.RootElement.GetProperty("components").GetProperty("schemas");
        var submission = schemas.GetProperty("SubmitActionRequest").GetProperty("properties");
        Assert.Equal(SubmissionFields,
            submission.EnumerateObject().Select(p => p.Name).OrderBy(p => p, StringComparer.Ordinal));
        foreach (var forbidden in new[] { "actor", "audience", "payload", "stateVersion",
                     "expectedStateVersion", "acceptedStateVersion" })
            Assert.False(submission.TryGetProperty(forbidden, out _));
        var view = schemas.GetProperty("CoherentPlayerView").GetProperty("properties");
        Assert.False(view.TryGetProperty("stateVersion", out _));
        var legal = schemas.GetProperty("LegalAction").GetProperty("properties");
        Assert.Equal("uuid", legal.GetProperty("actionId").GetProperty("format").GetString());
    }

    [Fact]
    public void ReceiptVariantsRejectAcceptedFieldsOutsideAcceptance()
    {
        var prefix = "\"contractVersion\":1,\"campaignId\":\"campaign-1\",\"requestId\":\"8aa18f25-30c3-4c76-8f7b-576742b9a82f\"";
        AssertReceipt($"{{{prefix},\"status\":\"pending\",\"reason\":\"none\"}}", true);
        AssertReceipt($"{{{prefix},\"status\":\"accepted\",\"reason\":\"none\",\"audienceRevision\":\"9007199254740993\"}}", true);
        AssertReceipt($"{{{prefix},\"status\":\"rejected\",\"reason\":\"stale-view\"}}", true);
        AssertReceipt($"{{{prefix},\"status\":\"pending\",\"reason\":\"none\",\"audienceRevision\":\"1\"}}", false);
        AssertReceipt($"{{{prefix},\"status\":\"accepted\",\"reason\":\"none\"}}", false);
        AssertReceipt($"{{{prefix},\"status\":\"rejected\",\"reason\":\"none\"}}", false);
        AssertReceipt($"{{{prefix},\"status\":\"accepted\",\"reason\":\"none\",\"acceptedStateVersion\":\"9\",\"audienceRevision\":\"1\"}}", false);
        AssertReceipt($"{{{prefix},\"status\":\"rejected\",\"reason\":\"stale-view\",\"audienceRevision\":\"1\"}}", false);
    }

    [Fact]
    public void SubmissionHttpSuccessCarriesDiscriminatedReceiptAndLatestStatusRetry()
    {
        var root = Contract.RootElement;
        var schemas = root.GetProperty("components").GetProperty("schemas");
        var post = root.GetProperty("paths")
            .GetProperty("/api/v1/campaigns/{campaignId}/actions").GetProperty("post");
        var responses = post.GetProperty("responses");
        Assert.False(responses.TryGetProperty("202", out _));
        Assert.Equal("#/components/schemas/ActionReceipt",
            responses.GetProperty("200").GetProperty("content").GetProperty("application/json")
                .GetProperty("schema").GetProperty("$ref").GetString());
        Assert.Equal("#/components/schemas/ActionReceipt",
            root.GetProperty("paths")
                .GetProperty("/api/v1/campaigns/{campaignId}/receipts/{requestId}")
                .GetProperty("get").GetProperty("responses").GetProperty("200")
                .GetProperty("content").GetProperty("application/json").GetProperty("schema")
                .GetProperty("$ref").GetString());

        var retry = post.GetProperty("x-retry-semantics");
        Assert.Equal(["campaignId", "serverSeat", "requestId"],
            retry.GetProperty("identity").EnumerateArray()
                .Select(value => value.GetString()).ToArray());
        Assert.Equal("SubmitActionRequest", retry.GetProperty("canonicalPayload").GetString());
        Assert.Equal("latest-retained-status", retry.GetProperty("sameRequest").GetString());
        Assert.Equal("409", retry.GetProperty("changedSeatOrPayload").GetString());
        Assert.Equal(["pending->accepted", "pending->rejected"],
            retry.GetProperty("allowedTransitions").EnumerateArray()
                .Select(value => value.GetString()).ToArray());
        Assert.Equal(["accepted", "rejected"],
            retry.GetProperty("terminalStatuses").EnumerateArray()
                .Select(value => value.GetString()).ToArray());

        const string prefix = "\"contractVersion\":1,\"campaignId\":\"campaign-1\",\"requestId\":\"8aa18f25-30c3-4c76-8f7b-576742b9a82f\"";
        using var pending = JsonDocument.Parse($"{{{prefix},\"status\":\"pending\",\"reason\":\"none\"}}");
        using var accepted = JsonDocument.Parse($"{{{prefix},\"status\":\"accepted\",\"reason\":\"none\",\"audienceRevision\":\"1\"}}");
        using var rejected = JsonDocument.Parse($"{{{prefix},\"status\":\"rejected\",\"reason\":\"stale-view\"}}");
        AssertNode(pending.RootElement, responses.GetProperty("200").GetProperty("content")
            .GetProperty("application/json").GetProperty("schema"), schemas);
        AssertNode(accepted.RootElement, responses.GetProperty("200").GetProperty("content")
            .GetProperty("application/json").GetProperty("schema"), schemas);
        AssertNode(rejected.RootElement, responses.GetProperty("200").GetProperty("content")
            .GetProperty("application/json").GetProperty("schema"), schemas);
    }

    [Fact]
    public void SignalRContractBindsSubscriptionToServerSeatAndPermittedViewChanges()
    {
        var root = SignalR.RootElement;
        Assert.Equal(1, root.GetProperty("contractVersion").GetInt32());
        Assert.Equal("/hubs/v1/campaigns", root.GetProperty("hubPath").GetString());
        var auth = root.GetProperty("authentication");
        Assert.Equal("sandtable_session", auth.GetProperty("cookie").GetString());
        Assert.Equal("server-session", auth.GetProperty("seatSource").GetString());
        Assert.True(auth.GetProperty("rejectClientSide").GetBoolean());
        var stages = auth.GetProperty("authorizeOn").EnumerateArray().Select(p => p.GetString()).ToArray();
        Assert.Equal(["connect", "subscribe", "before-group-join"], stages);
        var subscription = root.GetProperty("subscription");
        Assert.Equal("Subscribe", subscription.GetProperty("method").GetString());
        Assert.DoesNotContain("side", subscription.GetProperty("argumentsSchema")
            .GetProperty("properties").EnumerateObject().Select(p => p.Name));
        var ev = root.GetProperty("serverEvent");
        Assert.Equal("viewChanged", ev.GetProperty("method").GetString());
        var fields = ev.GetProperty("payloadSchema").GetProperty("properties");
        Assert.False(fields.TryGetProperty("stateVersion", out _));
        Assert.False(fields.TryGetProperty("journalSequence", out _));
        Assert.Equal("sessionGeneration", ev.GetProperty("payloadSchema")
            .GetProperty("required")[2].GetString());
        var negotiate = auth.GetProperty("negotiate");
        Assert.Equal("POST", negotiate.GetProperty("method").GetString());
        Assert.Equal("X-Sandtable-CSRF", negotiate.GetProperty("csrfHeader").GetString());
        Assert.False(negotiate.GetProperty("skipNegotiation").GetBoolean());
        Assert.True(negotiate.GetProperty("rejectCrossOrigin").GetBoolean());

        var schemas = Contract.RootElement.GetProperty("components").GetProperty("schemas");
        using var hint = JsonDocument.Parse("""
            {"contractVersion":1,"campaignId":"campaign-1","sessionGeneration":"8aa18f25-30c3-4c76-8f7b-576742b9a82f","audienceRevision":"9007199254740993"}
            """);
        AssertNode(hint.RootElement, ev.GetProperty("payloadSchema"), schemas);
        var hidden = JsonNode.Parse(hint.RootElement.GetRawText())!;
        hidden["stateVersion"] = "42";
        using var hiddenDocument = JsonDocument.Parse(hidden.ToJsonString());
        Assert.Throws<InvalidDataException>(() => AssertNode(hiddenDocument.RootElement,
            ev.GetProperty("payloadSchema"), schemas));
        using var subscribe = JsonDocument.Parse("""
            {"contractVersion":1,"campaignId":"campaign-1","sessionGeneration":"8aa18f25-30c3-4c76-8f7b-576742b9a82f"}
            """);
        AssertNode(subscribe.RootElement, subscription.GetProperty("argumentsSchema"), schemas);
        var clientSide = JsonNode.Parse(subscribe.RootElement.GetRawText())!;
        clientSide["side"] = "commonwealth";
        using var clientSideDocument = JsonDocument.Parse(clientSide.ToJsonString());
        Assert.Throws<InvalidDataException>(() => AssertNode(clientSideDocument.RootElement,
            subscription.GetProperty("argumentsSchema"), schemas));
    }

    private static void AssertReceipt(string json, bool valid)
    {
        using var document = JsonDocument.Parse(json);
        if (valid) AssertSchema(document.RootElement, "ActionReceipt");
        else Assert.Throws<InvalidDataException>(() => AssertSchema(document.RootElement,
            "ActionReceipt"));
    }

    private static void AssertRejects(JsonNode node, string name)
    {
        using var document = JsonDocument.Parse(node.ToJsonString());
        Assert.Throws<InvalidDataException>(() => AssertSchema(document.RootElement, name));
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
        if (schema.TryGetProperty("oneOf", out var variants))
        {
            var matches = 0;
            foreach (var variant in variants.EnumerateArray())
            {
                try { AssertNode(value, variant, schemas); matches++; }
                catch (InvalidDataException) { }
            }
            if (matches != 1) throw new InvalidDataException("Exactly one variant required.");
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
                {
                    if (schema.TryGetProperty("additionalProperties", out var additional) &&
                        additional.ValueKind == JsonValueKind.False)
                        throw new InvalidDataException($"Forbidden field {property.Name}.");
                    continue;
                }
                AssertNode(property.Value, field, schemas);
            }
            if (schema.TryGetProperty("x-distinct-values", out var distinctFields))
            {
                var values = distinctFields.EnumerateArray()
                    .Select(field => value.GetProperty(field.GetString()!).GetString())
                    .ToArray();
                if (values.Distinct(StringComparer.Ordinal).Count() != values.Length)
                    throw new InvalidDataException("Contract fields must be distinct.");
            }
        }
        else if (type == "array")
        {
            if (value.ValueKind != JsonValueKind.Array) throw new InvalidDataException("Expected array.");
            if (schema.TryGetProperty("maxItems", out var maxItems) &&
                value.GetArrayLength() > maxItems.GetInt32())
                throw new InvalidDataException("Too many items.");
            foreach (var item in value.EnumerateArray())
                AssertNode(item, schema.GetProperty("items"), schemas);
        }
        else if (type == "string")
        {
            if (value.ValueKind != JsonValueKind.String) throw new InvalidDataException("Expected string.");
            var text = value.GetString()!;
            if (schema.TryGetProperty("pattern", out var pattern) &&
                !Regex.IsMatch(text, pattern.GetString()!, RegexOptions.CultureInvariant))
                throw new InvalidDataException("Pattern mismatch.");
            if (schema.TryGetProperty("format", out var format) &&
                format.GetString() == "uuid" && !Guid.TryParseExact(text, "D", out _))
                throw new InvalidDataException("Expected UUID.");
            if (schema.TryGetProperty("minLength", out var minLength) &&
                text.Length < minLength.GetInt32())
                throw new InvalidDataException("String too short.");
            if (schema.TryGetProperty("maxLength", out var maxLength) &&
                text.Length > maxLength.GetInt32())
                throw new InvalidDataException("String too long.");
        }
        else if (type == "integer")
        {
            if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out var integer))
                throw new InvalidDataException("Expected integer.");
            if (schema.TryGetProperty("minimum", out var minimum) && integer < minimum.GetInt64())
                throw new InvalidDataException("Integer below minimum.");
        }
        else if (type == "number" && value.ValueKind != JsonValueKind.Number)
            throw new InvalidDataException("Expected number.");
        else if (type == "boolean" && value.ValueKind is not
            (JsonValueKind.True or JsonValueKind.False))
            throw new InvalidDataException("Expected boolean.");
        if (schema.TryGetProperty("enum", out var choices) &&
            !choices.EnumerateArray().Any(choice => choice.ToString() == value.ToString()))
            throw new InvalidDataException("Enum mismatch.");
    }
}
