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
    //
    // Important, confirmed-by-running-this correction to an earlier draft: the real
    // visible-cut for this opening is {contractVersion, campaignId, positionId, choices}
    // ONLY -- no force counts, no positions, no enemy visibility. Checked directly
    // against a real campaign (see docs/work/plans/2026-11-23-recommendation-connector-
    // design-discussion.md's "resolved" section) and confirmed no broader map/forces/
    // contacts query exists anywhere in the real outward API either -- this isn't a
    // context-curation gap to engineer around, it's what the real game currently
    // exposes at this exact rules-moment (nothing new has happened yet this turn).
    //
    // An earlier version of this prompt's few-shot examples implied force-ratio data
    // would always be present in the input. It won't be, for this opening, today. That
    // mismatch is exactly what caused qwen2.5:1.5b to fabricate specific numbers
    // ("your force of 7 vs their 2") out of nothing when given the real, minimal input --
    // caught by the verification pass (Grounded: false), not by this prompt. Fixed by
    // teaching the real shape of the input instead of a hypothetical richer one.
    private const string SystemPrompt = """
        You are a tactical advisor for a 1979 North African desert wargame. You hold
        initiative and must pick exactly one legal action: act-first (commit your
        movement/combat before the enemy acts, trading information for tempo) or
        act-last (react to everything the enemy commits this turn, trading tempo for
        information).

        Important: the state you are given for this decision will NOT include force
        counts, unit positions, or enemy visibility -- at this exact moment in a turn,
        nothing new has happened yet, so there is no situational detail to report. Do
        not invent any. If you find yourself wanting to cite a specific number or fact,
        stop -- it is not real.

        Given that, reason about the general structural tradeoff instead: act-first
        commits you before you can react to anything the opponent does, which is
        stronger when you expect to have the advantage and want to deny the opponent
        a chance to reposition; act-last costs you the tempo but means you commit with
        full information about what the opponent did this turn, which is safer when
        you have no way to judge your relative strength in advance. State plainly in
        your rationale that no situational force/position data was available for this
        decision, and that your recommendation is a general rule of thumb, not a
        claim about a specific advantage.

        Example: State gives only contractVersion, campaignId, positionId, and the two
        choices -- no force or position data. Answer: act-last. Reason: with no
        situational information available to judge relative strength, preserving the
        information advantage (seeing what the opponent commits to first) is the safer
        general default than committing blind for tempo you cannot confirm you need.
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

    // Resolution of the design doc's open question #1 (context tier): there is no richer
    // tier to curate into. Confirmed directly against a real campaign that this opening's
    // visible cut really is just {contractVersion, campaignId, positionId, choices}, and
    // that no broader map/forces/contacts query exists anywhere in the real outward API
    // today. Passing the real cut through as-is is therefore not a crude placeholder for
    // a future curated template -- it is the honest, complete picture, and the system
    // prompt above is written to match that rather than to imply more exists.
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
