using System.Net;
using System.Text.Json;
using Cna.Core.Actions;

namespace Cna.OrleansHost.Api;

internal static class LocalGameApi
{
    private const string Cookie = "sandtable_session";
    private const string CsrfHeader = "X-Sandtable-CSRF";

    /// <summary>
    /// Explicit deployment-posture config key. "Is the local game API enabled" and
    /// "is it safe to expose this surface at all" are deliberately two independent
    /// checks (see issue #366): <see cref="Microsoft.AspNetCore.Hosting.IHostEnvironment.IsDevelopment"/>
    /// is ASP.NET's own environment flag and is easy to leave on by accident in a real
    /// deployment, so it must not be the sole thing standing between this surface and a
    /// real network. This key does not inherit from ASPNETCORE_ENVIRONMENT in any way -
    /// it defaults to disabled and must be explicitly set to true (e.g. in
    /// appsettings.Development.json) for a deployment the operator has actually
    /// confirmed is a trusted local/loopback one.
    /// </summary>
    internal const string TrustedLocalDeploymentConfigKey = "LocalGameApi:TrustedLocalDeployment";

    internal static void MapLocalGameApi(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return;
        if (!app.Configuration.GetValue<bool>(TrustedLocalDeploymentConfigKey)) return;
        app.UseWhen(context => context.Request.Path.StartsWithSegments("/api/v1"), branch =>
            branch.Use(async (context, next) =>
            {
                if (!IsLoopback(context) || !IsSameOrigin(context))
                {
                    await Error(context, StatusCodes.Status403Forbidden, "forbidden");
                    return;
                }
                context.Response.Headers.CacheControl = "no-store";
                var bodyLimit = context.Features
                    .Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>();
                if (bodyLimit is { IsReadOnly: false }) bodyLimit.MaxRequestBodySize = 4096;
                await next(context);
            }));

        var api = app.MapGroup("/api/v1");
        api.MapGet("/bootstrap", (HttpContext context, LocalSessionStore store) =>
        {
            // A browser navigation with no Origin is not proof of same-origin script intent.
            // Modern same-origin fetches supply Sec-Fetch-Site; explicit Origin supports tools.
            if (string.IsNullOrEmpty(context.Request.Headers["Sec-Fetch-Site"])
                && string.IsNullOrEmpty(context.Request.Headers.Origin))
                return ApiError(StatusCodes.Status403Forbidden, "forbidden");
            var session = store.Bootstrap(context.Request.Cookies[Cookie]);
            if (session is null) return ApiError(StatusCodes.Status503ServiceUnavailable, "unavailable");
            SetCookie(context, session.Token);
            return Results.Json(new
            {
                contractVersion = 1,
                csrfToken = session.CsrfToken,
                // Same check this method's own early-return already gated on (both
                // IsDevelopment() and the explicit posture flag are implied true here,
                // since MapLocalGameApi would have returned before mounting this route
                // otherwise) - lets the browser learn whether this deployment would
                // actually admit an action submission, without duplicating the config key.
                actionSubmissionTrusted = app.Configuration.GetValue<bool>(TrustedLocalDeploymentConfigKey),
            });
        });
        api.MapGet("/fixtures", () => Results.Json(new[]
        {
            new
            {
                fixtureId = TemporaryCampaignGrain.FixtureCampaignId,
                displayName = "Breakdown truck lab",
                scenarioId = TemporaryCampaignGrain.ScenarioId,
                rulesetHash = TemporaryCampaignGrain.RulesetHash,
                setupHash = TemporaryCampaignGrain.SetupHash[7..],
                contentHash = TemporaryCampaignGrain.ContentHash[7..],
            },
        }));
        api.MapPost("/campaigns", async (HttpContext context, LocalSessionStore sessions,
            IServiceProvider services) =>
        {
            var session = sessions.Find(context.Request.Cookies[Cookie]);
            if (session is null || session.Seat is not null)
                return ApiError(StatusCodes.Status401Unauthorized, "unauthorized");
            if (!ValidCsrf(context, session))
                return ApiError(StatusCodes.Status403Forbidden, "forbidden");
            string? fixtureId;
            string? requestId;
            try
            {
                using var document = await JsonDocument.ParseAsync(context.Request.Body,
                    cancellationToken: context.RequestAborted);
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 3
                    || !root.TryGetProperty("contractVersion", out var version)
                    || version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out var v)
                    || v != 1 || !root.TryGetProperty("fixtureId", out var fixture)
                    || fixture.ValueKind != JsonValueKind.String
                    || !root.TryGetProperty("requestId", out var request)
                    || request.ValueKind != JsonValueKind.String)
                    return ApiError(StatusCodes.Status400BadRequest, "invalid-request");
                fixtureId = fixture.GetString();
                requestId = request.GetString();
            }
            catch (JsonException)
            {
                return ApiError(StatusCodes.Status400BadRequest, "invalid-request");
            }
            if (fixtureId != TemporaryCampaignGrain.FixtureCampaignId
                || requestId is null || !Guid.TryParseExact(requestId, "D", out var parsed)
                || parsed.ToString("D") != requestId)
                return ApiError(StatusCodes.Status400BadRequest, "invalid-request");
            var creations = services.GetService<LocalCampaignCreationStore>();
            var creator = services.GetService<ITemporaryCampaignCreator>();
            if (creations is null || creator is null)
                return ApiError(StatusCodes.Status503ServiceUnavailable, "unavailable");
            var result = await creations.CreateAsync(session, fixtureId, requestId,
                sessions, creator, context.RequestAborted);
            return result.Status switch
            {
                LocalCreationStatus.Created => Results.Json(result.Response,
                    statusCode: StatusCodes.Status201Created),
                LocalCreationStatus.Conflict => ApiError(StatusCodes.Status409Conflict, "conflict"),
                LocalCreationStatus.Unauthorized => ApiError(StatusCodes.Status401Unauthorized,
                    "unauthorized"),
                LocalCreationStatus.Invalid => ApiError(StatusCodes.Status400BadRequest,
                    "invalid-request"),
                _ => ApiError(StatusCodes.Status503ServiceUnavailable, "unavailable"),
            };
        });
        api.MapPost("/campaigns/{campaignId}/seat", async (HttpContext context,
            LocalSessionStore store, string campaignId) =>
        {
            var cookie = context.Request.Cookies[Cookie];
            var session = store.Find(cookie);
            // No cookie at all is never recoverable (there's nothing to look a prior
            // redemption up by), so this still fails fast exactly like before recovery
            // existed.
            if (session is null && cookie is null)
                return ApiError(StatusCodes.Status401Unauthorized, "unauthorized");
            if (session is not null && !ValidCsrf(context, session))
                return ApiError(StatusCodes.Status403Forbidden, "forbidden");
            string? grantId;
            try
            {
                using var document = await JsonDocument.ParseAsync(context.Request.Body,
                    cancellationToken: context.RequestAborted);
                var root = document.RootElement;
                grantId = root.ValueKind == JsonValueKind.Object
                    && root.EnumerateObject().Count() == 2
                    && root.TryGetProperty("contractVersion", out var version)
                    && version.ValueKind == JsonValueKind.Number && version.TryGetInt32(out var number)
                    && number == 1
                    && root.TryGetProperty("grantId", out var grant)
                    && grant.ValueKind == JsonValueKind.String
                    ? grant.GetString() : null;
            }
            catch (JsonException) { return ApiError(StatusCodes.Status400BadRequest, "invalid-request"); }
            if (!Guid.TryParse(grantId, out _))
                return ApiError(StatusCodes.Status400BadRequest, "invalid-request");

            if (session is not null)
            {
                var next = await store.RedeemAsync(session, campaignId, grantId,
                    context.RequestAborted);
                if (next is null) return ApiError(StatusCodes.Status403Forbidden, "forbidden");
                SetCookie(context, next.Token);
                return Results.Json(SessionInfo(next));
            }

            // The cookie is present but matches no active session. It may simply be stale
            // (unknown/expired), or it may be the OLD cookie from a redemption that already
            // committed on a previous call to this same route, whose response (the
            // Set-Cookie carrying the new token) never reached this caller - see
            // LocalSessionStore.RedeemAsync/RecoverRedemption for exactly what that does
            // and doesn't recover. Try recovery before giving up; a mismatch on any of
            // cookie/grantId/campaignId/CSRF - including an entirely unrelated caller who
            // never had this cookie - correctly fails it.
            var csrf = context.Request.Headers.TryGetValue(CsrfHeader, out var header)
                && header.Count == 1 ? header[0] : null;
            var recovered = store.RecoverRedemption(cookie!, campaignId, grantId, csrf);
            if (recovered is null) return ApiError(StatusCodes.Status401Unauthorized, "unauthorized");
            SetCookie(context, recovered.Token);
            return Results.Json(SessionInfo(recovered));
        });
        api.MapGet("/campaigns/{campaignId}/session", (HttpContext context,
            LocalSessionStore store, string campaignId) =>
        {
            var session = store.Find(context.Request.Cookies[Cookie]);
            if (session is null || session.CampaignId != campaignId || session.Seat is null)
                return ApiError(StatusCodes.Status401Unauthorized, "unauthorized");
            return Results.Json(SessionInfo(session));
        });
        api.MapDelete("/campaigns/{campaignId}/seat", async (HttpContext context,
            LocalSessionStore store, string campaignId) =>
        {
            var session = store.Find(context.Request.Cookies[Cookie]);
            if (session is null || session.CampaignId != campaignId || session.Seat is null)
                return ApiError(StatusCodes.Status401Unauthorized, "unauthorized");
            if (!ValidCsrf(context, session)) return ApiError(StatusCodes.Status403Forbidden, "forbidden");
            if (!await store.RelinquishAsync(session, campaignId))
                return ApiError(StatusCodes.Status401Unauthorized, "unauthorized");
            context.Response.Cookies.Delete(Cookie, CookieOptions(context));
            return Results.NoContent();
        });
        api.MapGet("/campaigns/{campaignId}/view", async (HttpContext context,
            LocalSessionStore store, IServiceProvider services, string campaignId) =>
        {
            var session = store.Find(context.Request.Cookies[Cookie]);
            if (session is null || session.CampaignId != campaignId || session.Seat is null)
                return ApiError(StatusCodes.Status403Forbidden, "forbidden");
            var side = session.Seat switch
            {
                "axis" => CampaignActionAudience.Axis,
                "commonwealth" => CampaignActionAudience.Commonwealth,
                _ => (CampaignActionAudience?)null,
            };
            var reader = services.GetService<ITemporaryPrivateCutReader>();
            var projection = services.GetService<TemporaryPlayerViewProjection>();
            if (side is null || reader is null || projection is null
                || session.Generation is null)
                return ApiError(StatusCodes.Status503ServiceUnavailable, "unavailable");
            var reply = await reader.ReadAsync(campaignId, side.Value, session.Generation);
            // A seat switch can revoke this generation while the grain read is in flight.
            if (store.Find(session.Token) != session)
                return ApiError(StatusCodes.Status403Forbidden, "forbidden");
            if (reply.Status != TemporaryCampaignStatus.Current)
                return ApiError(StatusCodes.Status503ServiceUnavailable, "unavailable");
            var view = projection.Map(reply.Cut, session);
            return view is null
                ? ApiError(StatusCodes.Status503ServiceUnavailable, "unavailable")
                : Results.Json(view);
        });
        api.MapPost("/campaigns/{campaignId}/actions", async (HttpContext context,
            LocalSessionStore store, TemporaryOutwardActionIngress ingress, string campaignId) =>
        {
            var session = store.Find(context.Request.Cookies[Cookie]);
            if (session is null) return ApiError(StatusCodes.Status403Forbidden, "forbidden");
            if (!ValidCsrf(context, session)) return ApiError(StatusCodes.Status403Forbidden, "forbidden");
            string? requestId;
            string? sessionGeneration;
            string? expectedPositionId;
            string? actionId;
            int actionContractVersion;
            string? actionSetToken;
            try
            {
                using var document = await JsonDocument.ParseAsync(context.Request.Body,
                    cancellationToken: context.RequestAborted);
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 7
                    || !root.TryGetProperty("contractVersion", out var version)
                    || version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out var v)
                    || v != 1
                    || !root.TryGetProperty("requestId", out var request)
                    || request.ValueKind != JsonValueKind.String
                    || !root.TryGetProperty("sessionGeneration", out var generation)
                    || generation.ValueKind != JsonValueKind.String
                    || !root.TryGetProperty("expectedPositionId", out var position)
                    || position.ValueKind != JsonValueKind.String
                    || !root.TryGetProperty("actionId", out var action)
                    || action.ValueKind != JsonValueKind.String
                    || !root.TryGetProperty("actionContractVersion", out var actionVersion)
                    || actionVersion.ValueKind != JsonValueKind.Number
                    || !actionVersion.TryGetInt32(out actionContractVersion)
                    || !root.TryGetProperty("actionSetToken", out var token)
                    || token.ValueKind != JsonValueKind.String)
                    return ApiError(StatusCodes.Status400BadRequest, "invalid-request");
                requestId = request.GetString();
                sessionGeneration = generation.GetString();
                expectedPositionId = position.GetString();
                actionId = action.GetString();
                actionSetToken = token.GetString();
            }
            catch (JsonException)
            {
                return ApiError(StatusCodes.Status400BadRequest, "invalid-request");
            }
            // The outward request's identity/binding (canonical GUID shape, session generation
            // match, campaign/seat match) is validated by TemporaryOutwardActionBinding inside
            // the ingress; a failure there surfaces back as Rejected("invalid-request") below.
            var outward = new TemporaryOutwardActionRequest(1, campaignId, requestId!,
                sessionGeneration!, expectedPositionId!, actionId!, actionContractVersion,
                actionSetToken!);
            var reply = await ingress.SubmitAsync(session.Token, outward);
            return reply.Status switch
            {
                TemporaryCampaignStatus.Accepted =>
                    Results.Json(AcceptedReceipt(campaignId, requestId!, reply.AudienceRevision!)),
                TemporaryCampaignStatus.Rejected when reply.Reason == "invalid-request" =>
                    ApiError(StatusCodes.Status400BadRequest, "invalid-request"),
                TemporaryCampaignStatus.Rejected when reply.Reason == "unauthorized" =>
                    ApiError(StatusCodes.Status403Forbidden, "forbidden"),
                // invalid-action / stale-view / unsupported-phase: the submission itself was
                // well formed and admitted, so the outcome is a 200 rejected receipt, not an
                // HTTP error - matching the openapi contract's discriminated ActionReceipt.
                TemporaryCampaignStatus.Rejected =>
                    Results.Json(RejectedReceipt(campaignId, requestId!, reply.Reason)),
                TemporaryCampaignStatus.Conflict =>
                    ApiError(StatusCodes.Status409Conflict, "conflict"),
                TemporaryCampaignStatus.Missing =>
                    ApiError(StatusCodes.Status503ServiceUnavailable, "unavailable"),
                _ => ApiError(StatusCodes.Status503ServiceUnavailable, "unavailable"),
            };
        });
        api.MapGet("/campaigns/{campaignId}/receipts/{requestId}", async (HttpContext context,
            TemporaryOutwardActionIngress ingress, string campaignId, string requestId) =>
        {
            var token = context.Request.Cookies[Cookie];
            if (token is null) return ApiError(StatusCodes.Status403Forbidden, "forbidden");
            var reply = await ingress.FindRequestAsync(token, campaignId, requestId);
            return reply.Status switch
            {
                TemporaryCampaignStatus.Accepted =>
                    Results.Json(AcceptedReceipt(campaignId, requestId, reply.AudienceRevision!)),
                TemporaryCampaignStatus.Rejected when reply.Reason == "invalid-request" =>
                    ApiError(StatusCodes.Status400BadRequest, "invalid-request"),
                TemporaryCampaignStatus.Rejected when reply.Reason == "unauthorized" =>
                    ApiError(StatusCodes.Status403Forbidden, "forbidden"),
                TemporaryCampaignStatus.Rejected =>
                    Results.Json(RejectedReceipt(campaignId, requestId, reply.Reason)),
                TemporaryCampaignStatus.Conflict =>
                    ApiError(StatusCodes.Status403Forbidden, "forbidden"),
                TemporaryCampaignStatus.Missing =>
                    ApiError(StatusCodes.Status404NotFound, "not-found"),
                _ => ApiError(StatusCodes.Status503ServiceUnavailable, "unavailable"),
            };
        });
    }

    private static object AcceptedReceipt(string campaignId, string requestId,
        string audienceRevision) => new
        {
            contractVersion = 1,
            campaignId,
            requestId,
            status = "accepted",
            reason = "none",
            audienceRevision,
        };

    private static object RejectedReceipt(string campaignId, string requestId,
        string reason) => new
        {
            contractVersion = 1,
            campaignId,
            requestId,
            status = "rejected",
            reason,
        };

    private static bool IsLoopback(HttpContext context)
    {
        var remote = context.Connection.RemoteIpAddress;
        if (remote is null || !IPAddress.IsLoopback(remote)) return false;
        var host = context.Request.Host.Host;
        return host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host == "127.0.0.1" || host == "[::1]" || host == "::1";
    }

    private static bool IsSameOrigin(HttpContext context)
    {
        var fetchSite = context.Request.Headers["Sec-Fetch-Site"].ToString();
        if (fetchSite is not ("" or "same-origin")) return false;
        var origin = context.Request.Headers.Origin.ToString();
        if (string.IsNullOrEmpty(origin)) return true;
        return Uri.TryCreate(origin, UriKind.Absolute, out var value)
            && value.Scheme == context.Request.Scheme
            && value.Authority.Equals(context.Request.Host.Value, StringComparison.OrdinalIgnoreCase)
            && value.AbsolutePath == "/" && value.Query.Length == 0 && value.Fragment.Length == 0;
    }

    private static bool ValidCsrf(HttpContext context, LocalSession session) =>
        context.Request.Headers.TryGetValue(CsrfHeader, out var header)
        && header.Count == 1 && header[0] == session.CsrfToken;

    private static void SetCookie(HttpContext context, string token) =>
        context.Response.Cookies.Append(Cookie, token, CookieOptions(context));

    private static CookieOptions CookieOptions(HttpContext context) => new()
    {
        HttpOnly = true,
        Secure = context.Request.IsHttps,
        SameSite = SameSiteMode.Strict,
        Path = "/api/v1",
    };

    private static object SessionInfo(LocalSession session) => new
    {
        contractVersion = 1,
        campaignId = session.CampaignId,
        seat = session.Seat,
        sessionGeneration = session.Generation,
        lifetime = "temporary-memory",
    };

    private static IResult ApiError(int status, string code) => Results.Json(new
    {
        contractVersion = 1,
        code,
        message = code,
    }, statusCode: status);

    private static Task Error(HttpContext context, int status, string code) =>
        ApiError(status, code).ExecuteAsync(context);

}
