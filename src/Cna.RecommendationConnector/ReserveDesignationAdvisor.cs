using System.Net.Http.Json;
using System.Text.Json;
using OllamaSharp;
using OllamaSharp.Models.Chat;

namespace Cna.RecommendationConnector;

/// <summary>
/// Pure, read-only advisory logic for Reserve Designation on Stage 1. Same zero-dependency
/// shape as InitiativeOrderAdvisor, but a structurally different decision: a variable-length
/// list of real choices (one "complete" option plus one "designate" option per eligible
/// element), not a fixed two-way enum. Confirmed directly against a real campaign: like
/// Initiative Order, this opening's visible cut carries no force/position data either --
/// {contractVersion, campaignId, positionId, choices}, where each choice is
/// {kind: "complete"|"designate", elementId: string?}.
/// </summary>
public sealed class ReserveDesignationAdvisor(HttpClient outwardApi, IOllamaApiClient ollama)
{
    private static readonly JsonSerializerOptions DeserializeOptions = new() { PropertyNameCaseInsensitive = true };

    // Same lesson as InitiativeOrderAdvisor's corrected prompt: do not imply richer state
    // (force counts, positions) will be present, because it won't be -- confirmed directly,
    // not assumed this time. The real decision here is purely about the tradeoff of holding
    // an element back versus committing everything now, independent of situational specifics
    // this opening's state doesn't carry.
    private const string SystemPrompt = """
        You are a tactical advisor for a 1979 North African desert wargame. The acting
        side must choose exactly one option from a numbered list: "complete" finishes
        reserve designation with nothing held back (every available element commits
        now), or "designate" holds one specific named element back in reserve instead.

        The state you are given will NOT include force counts, unit positions, or enemy
        visibility -- do not invent any. Reason about the general structural tradeoff:
        holding an element in reserve preserves flexibility to reinforce a weak point or
        exploit a success later in the turn, at the cost of not using that element's
        full strength now; committing everything via "complete" maximizes immediate
        strength but leaves nothing in hand if the situation changes. State plainly in
        your rationale that no situational force/position data was available, and that
        your recommendation is a general rule of thumb, not a claim about a specific
        advantage.

        You must answer with the exact 0-based index of your chosen option from the
        numbered list given to you, and a brief rationale.

        Example: options are [0: complete, 1: designate axis-infantry]. Answer: 1.
        Reason: with no situational data to suggest an immediate need for axis-infantry's
        full strength, the general default is to hold a resource in reserve rather than
        fully commit everything with nothing held back.

        Example: options are [0: complete, 1: designate cw-armor, 2: designate cw-artillery].
        Answer: 0. Reason: with multiple elements eligible and no situational data pointing
        at any one of them as the right one to hold back, there is no principled basis to
        single one out over the others -- committing everything (complete) avoids an
        arbitrary pick among equally-uninformed options. The number and names of the
        designate options vary call to call; always re-read the real list given and
        answer with its index, never assume these two examples are the only options.
        """;

    private const string VerificationSystemPrompt = """
        You are fact-checking a tactical recommendation against the exact state it was
        given. Read the rationale. If it references any fact, threat, or piece of
        information that is not actually present in the state text, say so. Otherwise
        confirm it is grounded.
        """;

    public async Task<AdviceResult> Advise(
        CampaignSeatCredential credential, string actor, string stage, CancellationToken cancellationToken)
    {
        var opening = await QueryOpening(credential, actor, stage, cancellationToken);

        if (opening.Choices.Length == 0)
            throw new InvalidOperationException($"The opening at '{stage}' offered no choices at all.");

        var optionsText = string.Join("\n", opening.Choices.Select((choice, index) =>
            $"{index}: {choice.Kind}" + (choice.ElementId is null ? "" : $" {choice.ElementId}")));
        var stateSuffix = "Visible state (JSON):\n" + opening.VisibleCutJson.GetRawText()
            + "\n\nOptions (choose by index):\n" + optionsText;

        var candidate = await GenerateCandidate(stateSuffix, opening.Choices.Length, cancellationToken);

        if (candidate.RecommendedChoiceIndex < 0 || candidate.RecommendedChoiceIndex >= opening.Choices.Length)
        {
            throw new InvalidOperationException(
                $"Model recommended index {candidate.RecommendedChoiceIndex}, out of range for "
                + $"{opening.Choices.Length} real options.");
        }

        var verification = await VerifyCandidate(stateSuffix, candidate, cancellationToken);
        var chosen = opening.Choices[candidate.RecommendedChoiceIndex];

        return new AdviceResult(
            chosen.Kind + (chosen.ElementId is null ? "" : $" {chosen.ElementId}"),
            candidate.Rationale, verification.Grounded, verification.Note);
    }

    private async Task<OpeningSnapshot> QueryOpening(
        CampaignSeatCredential credential, string actor, string stage, CancellationToken cancellationToken)
    {
        var body = new
        {
            contractVersion = 2,
            credential = new
            {
                campaignId = credential.CampaignId,
                audience = credential.Audience,
                epoch = credential.Epoch,
                secret = credential.Secret,
                expiresUnixMs = credential.ExpiresUnixMs,
            },
            stage,
            actingElementId = actor,
        };
        using var response = await outwardApi.PostAsJsonAsync(
            $"/api/v2/campaigns/{credential.CampaignId}/openings", body, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var json = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var visible = JsonDocument.Parse(Convert.FromBase64String(
            json.RootElement.GetProperty("visibleCutBase64").GetString()!));
        var choices = visible.RootElement.GetProperty("choices").EnumerateArray()
            .Select(value => new ReserveChoice(
                value.GetProperty("kind").GetString()!,
                value.TryGetProperty("elementId", out var elementId) && elementId.ValueKind == JsonValueKind.String
                    ? elementId.GetString()
                    : null))
            .ToArray();
        return new OpeningSnapshot(choices, visible.RootElement.Clone());
    }

    private async Task<Candidate> GenerateCandidate(
        string stateSuffix, int optionCount, CancellationToken cancellationToken)
    {
        var chat = new Chat(ollama, SystemPrompt) { Think = false };
        var reply = "";
        await foreach (var token in chat.SendAsync(stateSuffix, tools: null,
            imagesAsBase64: null, format: OutputSchema(optionCount), cancellationToken: cancellationToken))
            reply += token;
        return JsonSerializer.Deserialize<Candidate>(reply, DeserializeOptions)
            ?? throw new InvalidOperationException("Model returned no parseable candidate.");
    }

    private async Task<Verification> VerifyCandidate(
        string stateSuffix, Candidate candidate, CancellationToken cancellationToken)
    {
        var verifyPrompt = $"State given:\n{stateSuffix}\n\nRecommended index: "
            + $"{candidate.RecommendedChoiceIndex}\nRationale given: {candidate.Rationale}";
        var chat = new Chat(ollama, VerificationSystemPrompt) { Think = false };
        var reply = "";
        await foreach (var token in chat.SendAsync(verifyPrompt, tools: null,
            imagesAsBase64: null, format: VerificationSchema, cancellationToken: cancellationToken))
            reply += token;
        return JsonSerializer.Deserialize<Verification>(reply, DeserializeOptions)
            ?? new Verification(false, "Verification pass returned no parseable result.");
    }

    // The valid index range is request-specific (depends on how many real options exist
    // this call), so the schema's bound is rebuilt per request rather than a fixed enum --
    // the structural difference from InitiativeOrderAdvisor's fixed two-choice schema.
    private static object OutputSchema(int optionCount) => new
    {
        type = "object",
        properties = new
        {
            recommendedChoiceIndex = new { type = "integer", minimum = 0, maximum = optionCount - 1 },
            rationale = new { type = "string" },
        },
        required = new[] { "recommendedChoiceIndex", "rationale" },
    };

    private static readonly object VerificationSchema = new
    {
        type = "object",
        properties = new
        {
            grounded = new { type = "boolean" },
            note = new { type = "string" },
        },
        required = new[] { "grounded", "note" },
    };

    private sealed record ReserveChoice(string Kind, string? ElementId);
    private sealed record OpeningSnapshot(ReserveChoice[] Choices, JsonElement VisibleCutJson);
    private sealed record Candidate(int RecommendedChoiceIndex, string Rationale);
    private sealed record Verification(bool Grounded, string Note);
}
