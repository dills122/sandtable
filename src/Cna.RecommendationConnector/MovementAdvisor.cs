using System.Net.Http.Json;
using System.Text.Json;
using OllamaSharp;
using OllamaSharp.Models.Chat;

namespace Cna.RecommendationConnector;

/// <summary>
/// Pure, read-only advisory logic for Movement on Stage 1 -- the first decision type this
/// connector handles whose real visible cut is NOT the minimal {contractVersion, campaignId,
/// positionId, choices} shape Initiative Order and Reserve Designation turned out to have.
/// Confirmed directly against a real campaign: the "movement" opening's visible cut carries a
/// real terrain graph (locations/edges with terrain features), real contact observations
/// (apparent enemy presence), the acting side's own element state (position, mobility,
/// cohesion), and a list of real candidate moves (movementOffers, each with an origin,
/// destination, and real movement-point cost) -- the design doc's "no richer tier exists"
/// finding was scoped to the two decision types checked at the time, not to every decision
/// type; this one genuinely has tactical data to reason over.
/// </summary>
public sealed class MovementAdvisor(HttpClient outwardApi, IOllamaApiClient ollama)
{
    private static readonly JsonSerializerOptions DeserializeOptions = new() { PropertyNameCaseInsensitive = true };

    // Unlike Initiative Order/Reserve Designation, this decision has real facts to reason
    // over -- so the prompt teaches how to read them, not that none exist. Kept deliberately
    // narrow and conservative (one simple heuristic) rather than a rich doctrine, matching the
    // demonstrated fragility of a 1.5B model on anything more structured than a binary choice.
    private const string SystemPrompt = """
        You are a tactical advisor for a 1979 North African desert wargame. The acting side
        must choose exactly one candidate move for one element from a numbered list of real
        options. Each option gives the element's origin location, its destination location,
        and the real movement-point cost of that move.

        You are also given: the map (locations and the terrain features on the edges between
        them), any contacts currently observed (an apparent enemy presence at a location, with
        how confidently it is identified), and the acting element's own state (its current
        location, mobility type, and cohesion level).

        Use only the facts given -- never invent a contact, a cost, or a terrain feature that
        is not in the state text. When no contact is observed near any candidate destination,
        there is no situational reason to prefer one destination over another on safety
        grounds, so prefer the lowest-cost option. When a contact IS observed near a
        candidate's destination, treat moving toward it as the riskier choice relative to
        options that do not approach it, and only recommend moving toward it if no other option
        is offered. State plainly in your rationale which facts (if any) from the given contacts
        and terrain you relied on, and if none were relevant, say so and that you defaulted to
        lowest cost.

        Example: options are [0: axis-infantry from north to north-west, cost 1], with no
        contacts observed near north-west. Answer: 0. Reason: no contact is observed near the
        destination, and this is the only option offered, so there is no situational basis to
        prefer a different move -- taking the lowest (and only) available cost option.
        """;

    private const string VerificationSystemPrompt = """
        You are fact-checking a tactical recommendation against the exact state it was given.
        Read the rationale. If it references any contact, cost, terrain feature, or location
        that is not actually present in the state text, say so. Otherwise confirm it is
        grounded.
        """;

    public async Task<AdviceResult> Advise(
        CampaignSeatCredential credential, string actor, string stage, CancellationToken cancellationToken)
    {
        var opening = await QueryOpening(credential, actor, stage, cancellationToken);

        if (opening.Offers.Length == 0)
        {
            throw new InvalidOperationException(
                $"The movement opening for '{actor}' offered no candidate moves at all -- "
                + "there is nothing for this advisor to recommend.");
        }

        var stateSuffix = BuildStateSuffix(opening.VisibleCutJson);

        // A single offer is not a real decision (UX-001 is about not automating away a
        // genuine choice a human would make -- there is no choice here to automate away).
        // Skip the model entirely rather than have it "decide" something that was never in
        // question; this also means it cannot introduce reliability noise where none of the
        // earlier two decision types ever needed this guard (both always have >= 2 real
        // choices by construction).
        if (opening.Offers.Length == 1)
        {
            var only = opening.Offers[0];
            return new AdviceResult(DescribeOffer(only), "Only one candidate move was offered -- "
                + "there is no real choice here, so no model call was made.", true,
                "Not applicable -- nothing to verify when there is only one legal option.");
        }

        var candidate = await GenerateCandidate(stateSuffix, opening.Offers.Length, cancellationToken);

        if (candidate.RecommendedChoiceIndex < 0 || candidate.RecommendedChoiceIndex >= opening.Offers.Length)
        {
            throw new InvalidOperationException(
                $"Model recommended index {candidate.RecommendedChoiceIndex}, out of range for "
                + $"{opening.Offers.Length} real options.");
        }

        var verification = await VerifyCandidate(stateSuffix, candidate, cancellationToken);
        var chosen = opening.Offers[candidate.RecommendedChoiceIndex];

        return new AdviceResult(DescribeOffer(chosen), candidate.Rationale,
            verification.Grounded, verification.Note);
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
        var offers = visible.RootElement.GetProperty("movementOffers").EnumerateArray()
            .Select(value => new MovementOffer(
                value.GetProperty("elementId").GetString()!,
                value.GetProperty("originLocationId").GetString()!,
                value.GetProperty("destinationLocationId").GetString()!,
                value.GetProperty("totalCostNumerator").GetInt64(),
                value.GetProperty("totalCostDenominator").GetInt64()))
            .ToArray();
        return new OpeningSnapshot(offers, visible.RootElement.Clone());
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

    private static string DescribeOffer(MovementOffer offer) =>
        $"move {offer.ElementId} from {offer.OriginLocationId} to {offer.DestinationLocationId} "
        + $"(cost {offer.CostNumerator}/{offer.CostDenominator})";

    // Passed through as-is -- the real visible cut here genuinely has the facts this prompt
    // asks the model to reason over, unlike Initiative Order/Reserve Designation's minimal cut.
    private static string BuildStateSuffix(JsonElement visibleCutJson) =>
        "Visible state (JSON):\n" + visibleCutJson.GetRawText();

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

    private sealed record MovementOffer(
        string ElementId, string OriginLocationId, string DestinationLocationId,
        long CostNumerator, long CostDenominator);
    private sealed record OpeningSnapshot(MovementOffer[] Offers, JsonElement VisibleCutJson);
    private sealed record Candidate(int RecommendedChoiceIndex, string Rationale);
    private sealed record Verification(bool Grounded, string Note);
}
