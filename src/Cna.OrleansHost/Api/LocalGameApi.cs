using System.Net;
using System.Text.Json;

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
            var session = store.Bootstrap(context.Request.Cookies[Cookie]);
            SetCookie(context, session.Token);
            return Results.Json(new { contractVersion = 1, csrfToken = session.CsrfToken });
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
            var next = store.Redeem(session, campaignId, grantId);
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
        api.MapDelete("/campaigns/{campaignId}/seat", (HttpContext context,
            LocalSessionStore store, string campaignId) =>
        {
            var session = store.Find(context.Request.Cookies[Cookie]);
            if (session is null || session.CampaignId != campaignId || session.Seat is null)
                return ApiError(StatusCodes.Status401Unauthorized, "unauthorized");
            if (!ValidCsrf(context, session)) return ApiError(StatusCodes.Status403Forbidden, "forbidden");
            if (!store.Relinquish(session, campaignId))
                return ApiError(StatusCodes.Status401Unauthorized, "unauthorized");
            context.Response.Cookies.Delete(Cookie, CookieOptions(context));
            return Results.NoContent();
        });
        api.MapGet("/campaigns/{campaignId}/view", (HttpContext context,
            LocalSessionStore store, string campaignId) =>
        {
            var session = store.Find(context.Request.Cookies[Cookie]);
            if (session is null || session.CampaignId != campaignId || session.Seat is null)
                return ApiError(StatusCodes.Status403Forbidden, "forbidden");
            // MAP-07's legal-action bytes contain private Core IDs and stateVersion. A complete,
            // audience-safe MAP-04 projection is a prerequisite for returning this endpoint.
            return ApiError(StatusCodes.Status503ServiceUnavailable, "unavailable");
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
