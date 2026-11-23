using System.Net.Http.Json;
using System.Text.Json;
using OllamaSharp;
using OllamaSharp.Models.Chat;

namespace Cna.RecommendationConnector;

/// <summary>
/// Pure, read-only advisory logic for the v1 scope (Initiative Order on Stage 1 only).
/// Never writes to the campaign journal -- the only outward-API call this makes is the
/// opening query, the same POST /openings any real client would send. It never calls
/// /actions. Zero project reference to Cna.Core or Cna.OrleansHost -- this cannot reach
/// anything but what the real outward HTTP contract exposes.
/// </summary>
public sealed class InitiativeOrderAdvisor(HttpClient outwardApi, IOllamaApiClient ollama)
{
    // System.Text.Json's default is case-sensitive property matching, which silently
    // fails to bind (not throws) against a model's own JSON key casing -- confirmed
    // locally: recommendedActionId came back empty without this.
    private static readonly JsonSerializerOptions DeserializeOptions = new() { PropertyNameCaseInsensitive = true };

    // Static harness context, built once -- the rules never change per request (decision 4).
    // The two worked examples are load-bearing: without them, qwen2.5:1.5b defaulted to
    // act-first regardless of state (confirmed locally); with them, it correctly reasons
    // about force ratio and visibility instead.
    private const string SystemPrompt = """
        You are a tactical advisor for a 1979 North African desert wargame. You hold
        initiative and must pick exactly one legal action: act-first (commit your
        movement/combat before the enemy acts, trading information for tempo) or
        act-last (react to everything the enemy commits this turn, trading tempo for
        information). Do not default to one answer. Weigh the specific force ratio and
        visibility given in each case. Only reference facts that are explicitly present
        in the state given to you -- never invent a threat, reinforcement, or piece of
        information the state does not actually state.

        Example 1: Your forces outnumber the visible enemy garrison 3 to 1 at the
        contested position, and the enemy has no visible reserves nearby. Answer:
        act-first. Reason: you have a clear local advantage and no hidden threat to
        worry about, so press it before the enemy can reposition or reinforce.

        Example 2: Your forces are outnumbered 1 to 2 at the contested position, and
        you have no visibility into where the enemy reserves are. Answer: act-last.
        Reason: committing now with a weaker, uncertain position risks running into
        hidden strength; better to see what the enemy actually does before committing
        your own weaker force.

        Now apply this same reasoning to the new situation given.
        """;

    // Second pass: ground-check the first pass's own output against the literal state it
    // was given, rather than trust it. This is the verification step decision 5 calls for --
    // confirmed necessary by local testing: the model picked a defensible action once but
    // justified it with a fact (hidden enemy reinforcement) the input explicitly ruled out.
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

        // The "choices" array in the decoded visible cut carries the real semantic labels
        // (act-first/act-last); publicActionRefs are opaque submission tokens, positionally
        // parallel to it -- this connector never needs those refs itself (it never submits),
        // but the legal-action *set* it validates against must come from choices, not refs.
        var choices = opening.VisibleCutJson.GetProperty("choices").EnumerateArray()
            .Select(value => value.GetString()!).ToArray();

        if (choices.Length != 2 || !choices.Contains("act-first") || !choices.Contains("act-last"))
        {
            throw new InvalidOperationException(
                "This advisor's v1 scope is Initiative Order only -- the opening at "
                + $"'{stage}' did not offer exactly [act-first, act-last]. Got: "
                + string.Join(", ", choices));
        }

        var stateSuffix = BuildStateSuffix(opening.VisibleCutJson);
        var candidate = await GenerateCandidate(stateSuffix, cancellationToken);

        // Structural check: is the recommended id even one of the two real legal actions?
        // Schema-constrained decoding already guarantees this for this scope (the schema's
        // enum IS the legal-action set), but check anyway -- the schema is only as safe as
        // it being re-derived from the real opening every call, and this is the one line
        // standing between "guaranteed" and "assumed."
        if (!choices.Contains(candidate.RecommendedActionId))
        {
            throw new InvalidOperationException(
                $"Model recommended '{candidate.RecommendedActionId}', which is not in "
                + $"the real legal-action list for this request: {string.Join(", ", choices)}");
        }

        var verification = await VerifyCandidate(stateSuffix, candidate, cancellationToken);

        return new AdviceResult(candidate.RecommendedActionId, candidate.Rationale,
            verification.Grounded, verification.Note);
    }

    // POST /api/v2/campaigns/{campaignId}/openings -- the exact real outward-API shape
    // (OwnerHttpHarness.OpeningResponse's own body), not a shortcut. Credential travels in
    // the body, same as any real client; this never touches /actions.
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
        var refs = json.RootElement.GetProperty("publicActionRefs").EnumerateArray()
            .Select(value => value.GetString()!).ToArray();
        var revision = json.RootElement.GetProperty("revision").GetInt64();
        var visible = JsonDocument.Parse(Convert.FromBase64String(
            json.RootElement.GetProperty("visibleCutBase64").GetString()!));
        return new OpeningSnapshot(refs, revision, visible.RootElement.Clone());
    }

    private async Task<Candidate> GenerateCandidate(string stateSuffix, CancellationToken cancellationToken)
    {
        var chat = new Chat(ollama, SystemPrompt) { Think = false };
        var reply = "";
        await foreach (var token in chat.SendAsync(stateSuffix, tools: null,
            imagesAsBase64: null, format: OutputSchema, cancellationToken: cancellationToken))
            reply += token;
        return JsonSerializer.Deserialize<Candidate>(reply, DeserializeOptions)
            ?? throw new InvalidOperationException("Model returned no parseable candidate.");
    }

    private async Task<Verification> VerifyCandidate(
        string stateSuffix, Candidate candidate, CancellationToken cancellationToken)
    {
        var verifyPrompt = $"State given:\n{stateSuffix}\n\nRecommendation made: "
            + $"{candidate.RecommendedActionId}\nRationale given: {candidate.Rationale}";
        var chat = new Chat(ollama, VerificationSystemPrompt) { Think = false };
        var reply = "";
        await foreach (var token in chat.SendAsync(verifyPrompt, tools: null,
            imagesAsBase64: null, format: VerificationSchema, cancellationToken: cancellationToken))
            reply += token;
        return JsonSerializer.Deserialize<Verification>(reply, DeserializeOptions)
            ?? new Verification(false, "Verification pass returned no parseable result.");
    }

    // Tier-2 context (decision 3's open question #1, resolved pragmatically for this PoC):
    // the real visible-cut JSON, as-is, not yet a hand-curated natural-language template.
    // Honest scope note: this is the crude version -- real integration testing against the
    // actual messy JSON shape (not the hand-written scenario text used for prompt tuning)
    // is itself useful signal, flagged in the report, not hidden.
    private static string BuildStateSuffix(JsonElement visibleCutJson) =>
        "Visible state (JSON):\n" + visibleCutJson.GetRawText();

    private static readonly object OutputSchema = new
    {
        type = "object",
        properties = new
        {
            recommendedActionId = new { type = "string", @enum = new[] { "act-first", "act-last" } },
            rationale = new { type = "string" },
        },
        required = new[] { "recommendedActionId", "rationale" },
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

    private sealed record OpeningSnapshot(string[] PublicActionRefs, long Revision, JsonElement VisibleCutJson);
    private sealed record Candidate(string RecommendedActionId, string Rationale);
    private sealed record Verification(bool Grounded, string Note);
}

/// <summary>
/// This connector's own plain data shape for "how to authenticate as a seat" -- matches
/// the real outward-API wire contract field-for-field, but is independently defined here
/// (not a reference to Cna.OrleansHost.Api.CampaignSeatCredential) so this project keeps
/// zero dependency on Core/OrleansHost.
/// </summary>
public sealed record CampaignSeatCredential(
    string CampaignId, string Audience, long Epoch, string Secret, long ExpiresUnixMs);

public sealed record AdviceResult(string RecommendedActionId, string Rationale, bool Grounded, string VerificationNote);
