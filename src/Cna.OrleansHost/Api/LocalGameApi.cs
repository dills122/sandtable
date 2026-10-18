using System.Net;
using System.Text.Json;
using Cna.Core.Actions;

namespace Cna.OrleansHost.Api;

internal static class LocalGameApi
{
    private const string Cookie = "sandtable_session";
    private const string CsrfHeader = "X-Sandtable-CSRF";

    internal static void MapLocalGameApi(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return;
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
            return Results.Json(new { contractVersion = 1, csrfToken = session.CsrfToken });
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
            var session = store.Find(context.Request.Cookies[Cookie]);
            if (session is null) return ApiError(StatusCodes.Status401Unauthorized, "unauthorized");
            if (!ValidCsrf(context, session)) return ApiError(StatusCodes.Status403Forbidden, "forbidden");
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
            var next = await store.RedeemAsync(session, campaignId, grantId);
            if (next is null) return ApiError(StatusCodes.Status403Forbidden, "forbidden");
            SetCookie(context, next.Token);
            return Results.Json(SessionInfo(next));
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
    }

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
