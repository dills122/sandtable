using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.OrleansHost.Api;
using Cna.OrleansHost.Campaigns;

namespace Cna.LoadHarness;

// All campaign interactions cross the actual v2 route boundary. Never log request or
// response bodies: they contain seat credentials and opaque owner references.
internal sealed class HttpDriver : ICampaignDriver, IDisposable
{
    private readonly HarnessConfig config;
    private readonly HttpClient client;
    private readonly string secret;
    private readonly HttpRunControl control;
    private readonly Action<string, string, string, string, double> record;
    private readonly string prefix;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    internal bool CampaignCreated { get; private set; }

    internal HttpDriver(HarnessConfig config, Uri address, string secret, HttpRunControl control,
        Action<string, string, string, string, double> record, string prefix)
    {
        this.config = config;
        this.secret = secret;
        this.control = control;
        this.record = record;
        this.prefix = prefix;
        client = new HttpClient(new SocketsHttpHandler { AllowAutoRedirect = false, UseProxy = false }) { BaseAddress = address };
        client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
    }

    public async Task<CampaignSeatCredential[]> CreateAsync(string id)
    {
        id = prefix + id;
        var (request, timing) = InProcessDriver.Creation(config, id);
        using var timingJson = JsonDocument.Parse(timing.SerializeCanonical());
        await Send("/api/v2/campaigns/", new
        {
            contractVersion = 2, request.CampaignId, request.RulesetHash, request.Seed,
            request.SetupId, request.SetupHash, request.ContentPackId, request.ContentHash,
            request.ScenarioId, timing = timingJson.RootElement,
        }, root => root.GetProperty("campaignId").GetString() == id ? true : throw new JsonException("Campaign mismatch."),
            "create", operatorCall: true, expected: HttpStatusCode.Created).ConfigureAwait(false);
        CampaignCreated = true;

        var seats = new List<CampaignSeatCredential>();
        foreach (var audience in new[] { CampaignActionAudience.Axis, CampaignActionAudience.Commonwealth })
        {
            var side = Side(audience);
            var grant = await Send(Path(id, "grants/" + side), new { },
                root => root.GetProperty("grant").Clone(), "grant", side, operatorCall: true).ConfigureAwait(false);
            seats.Add(await Send(Path(id, "seats"), new { contractVersion = 2, grant },
                root =>
                {
                    var value = root.GetProperty("credential");
                    RequireOwner(value, id, side);
                    return new CampaignSeatCredential(id, audience, value.GetProperty("epoch").GetInt64(),
                        value.GetProperty("secret").GetString() ?? throw new JsonException("Missing credential."),
                        value.GetProperty("expiresUnixMs").GetInt64());
                }, "redeem", side).ConfigureAwait(false));
        }
        return [.. seats];
    }

    public Task<CampaignOpenSeatResult> DiscoverAsync(CampaignSeatCredential seat) =>
        Send(Path(seat.CampaignId, "openings/open"),
            new { contractVersion = 2, credential = Credential(seat) }, root =>
            {
                RequireOwner(root, seat.CampaignId, Side(seat.Audience));
                var entries = root.GetProperty("open").Deserialize<CampaignOpenSeatEntry[]>(Json)
                    ?? throw new JsonException("Missing discovery entries.");
                return new CampaignOpenSeatResult(seat.CampaignId, seat.Audience, entries);
            }, "discover", Side(seat.Audience));

    public Task<CampaignGrainOpeningResult> OpenAsync(CampaignSeatCredential seat, string stage, string element, string operation) =>
        Send(Path(seat.CampaignId, "openings"),
            new { contractVersion = 2, credential = Credential(seat), stage, actingElementId = element }, root =>
            {
                RequireOwner(root, seat.CampaignId, Side(seat.Audience));
                if (root.GetProperty("stage").GetString() != stage
                    || root.GetProperty("actingElementId").GetString() != element)
                    throw new JsonException("Opening mismatch.");
                var references = root.GetProperty("publicActionRefs").Deserialize<string[]>(Json);
                if (references is null || references.Length == 0) throw new JsonException("Missing opening references.");
                return new CampaignGrainOpeningResult
                {
                    CampaignId = seat.CampaignId, Audience = seat.Audience, ActingElementId = element,
                    Revision = root.GetProperty("revision").GetInt64(),
                    VisibleBytes = root.GetProperty("visibleCutBase64").GetBytesFromBase64(),
                    PublicActionRefs = references,
                };
            }, operation, Side(seat.Audience), stage);

    public Task<Submission> SubmitAsync(CampaignSeatCredential seat, string stage, string element, long revision, string reference) =>
        Send(Path(seat.CampaignId, "actions"), new
        {
            contractVersion = 2, credential = Credential(seat), stage, actingElementId = element,
            openingRevision = revision, publicActionRef = reference,
        }, root =>
        {
            if (!root.GetProperty("accepted").GetBoolean()
                || root.GetProperty("openingRevision").GetInt64() != revision
                || root.GetProperty("publicActionRef").GetString() != reference)
                throw new JsonException("Invalid acceptance.");
            // v2 deliberately excludes authoritative state version from the public receipt.
            return new Submission(true, null, null);
        }, "submit", Side(seat.Audience), stage);

    internal Task<StateFingerprint?> VerifyAsync(string id) =>
        Send(Path(prefix + id, "verification"), null, root =>
        {
            if (!root.GetProperty("isVerified").GetBoolean()) return null;
            return new StateFingerprint(
                root.GetProperty("chainHeadHash").GetString() ?? throw new JsonException("Missing chain head."),
                root.GetProperty("reconstructionHash").GetString() ?? throw new JsonException("Missing reconstruction hash."),
                root.GetProperty("eventCount").GetInt64(), root.GetProperty("canonicalByteCount").GetInt64(),
                root.GetProperty("positionId").GetString() ?? throw new JsonException("Missing checkpoint position."),
                root.GetProperty("gameTurn").GetInt32());
        }, "verify", operatorCall: true);

    private async Task<T> Send<T>(string path, object? body, Func<JsonElement, T> parse, string operation,
        string audience = "", string stage = "", bool operatorCall = false, HttpStatusCode expected = HttpStatusCode.OK)
    {
        await control.EnterAsync(verification: operation == "verify").ConfigureAwait(false);
        var watch = Stopwatch.StartNew();
        var outcome = "error";
        try
        {
            using var request = new HttpRequestMessage(body is null ? HttpMethod.Get : HttpMethod.Post, path);
            if (body is not null) request.Content = JsonContent.Create(body, options: Json);
            if (operatorCall) request.Headers.Add("X-Sandtable-Operator", secret);
            using var response = await client.SendAsync(request, operation == "verify"
                ? control.VerificationCancellation : CancellationToken.None).ConfigureAwait(false);
            if (response.StatusCode != expected)
            {
                outcome = "http-" + (int)response.StatusCode;
                // Only the L1 Stage 2 discovery/fallback ladder expects unavailable openings.
                // B6's extended fixture requires every advertised owner offer to be available.
                if (response.StatusCode == HttpStatusCode.Conflict && config.Until == "stage2"
                    && operation is "open" or "probe")
                {
                    using var error = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false));
                    if (error.RootElement.GetProperty("code").GetString() == "unavailable")
                    {
                        outcome = "unavailable";
                        throw new OpeningUnavailableException();
                    }
                }
                if (response.StatusCode == HttpStatusCode.Conflict && config.Policy == "random-legal-full"
                    && operation == "submit")
                {
                    using var error = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false));
                    var code = error.RootElement.GetProperty("code").GetString();
                    if (code == "rule-not-implemented") outcome = "reaction-refused";
                    return (T)(object)new Submission(false, code, null);
                }
                throw new HttpRequestException("Unexpected HTTP status.", null, response.StatusCode);
            }
            using var document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false));
            if (body is not null && document.RootElement.GetProperty("contractVersion").GetInt32() != 2)
                throw new JsonException("Unexpected contract version.");
            var value = parse(document.RootElement);
            outcome = value is null && body is null ? "verification-failed" : "ok";
            return value;
        }
        finally
        {
            var elapsedMs = watch.Elapsed.TotalMilliseconds;
            record(audience, stage, operation, outcome, elapsedMs);
            control.Finish(operation, outcome, elapsedMs);
        }
    }

    private static void RequireOwner(JsonElement value, string id, string side)
    {
        if (value.GetProperty("campaignId").GetString() != id || value.GetProperty("audience").GetString() != side)
            throw new JsonException("Owner mismatch.");
    }

    private static string Path(string id, string suffix) => "/api/v2/campaigns/" + Uri.EscapeDataString(id) + "/" + suffix;
    private static string Side(CampaignActionAudience audience) => audience.ToString().ToLowerInvariant();
    private static object Credential(CampaignSeatCredential seat) => new
    {
        seat.CampaignId, audience = Side(seat.Audience), seat.Epoch, seat.Secret, seat.ExpiresUnixMs,
    };

    public void Dispose() => client.Dispose();
}

internal sealed class OpeningUnavailableException : InvalidOperationException;
