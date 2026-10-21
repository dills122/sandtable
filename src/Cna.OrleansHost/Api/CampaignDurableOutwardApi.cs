using System.Net;
using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;

namespace Cna.OrleansHost.Api;

/// <summary>
/// Versioned local entry to the normal durable campaign authority. All owner operations go
/// through the retained seat and opening fences in CampaignDurableAuthorityJournal.
/// </summary>
internal static class CampaignDurableOutwardApi
{
    private const int MaxBodyBytes = 8192;

    internal static void MapDurableCampaignApi(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            throw new InvalidOperationException("Durable campaign HTTP requires Development hosting.");
        if (app.Services.GetService<CampaignDurableAuthorityJournal>() is null)
            throw new InvalidOperationException("Durable campaign journal is not configured.");

        app.UseWhen(context => context.Request.Path.StartsWithSegments("/api/v2/campaigns"),
            branch => branch.Use(async (context, next) =>
            {
                context.Response.Headers.CacheControl = "no-store";
                if (context.Connection.RemoteIpAddress is not { } remote
                    || !IPAddress.IsLoopback(remote)
                    || !SameOrigin(context))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return;
                }
                var size = context.Features.Get<Microsoft.AspNetCore.Http.Features
                    .IHttpMaxRequestBodySizeFeature>();
                if (size is { IsReadOnly: false }) size.MaxRequestBodySize = MaxBodyBytes;
                await next(context);
            }));

        var api = app.MapGroup("/api/v2/campaigns");
        api.MapPost("/", async (HttpContext context, CampaignDurableAuthorityJournal journal) =>
            await Execute(context, async () =>
            {
                RequireOperator(context, journal);
                using var document = await ReadBody(context);
                var root = document.RootElement;
                RequireShape(root, "contractVersion", "campaignId", "rulesetHash", "seed",
                    "setupId", "setupHash", "contentPackId", "contentHash", "scenarioId",
                    "timing");
                RequireVersion(root);
                var request = new CampaignCreationRequest(
                    CampaignCreationRequest.CurrentContractVersion,
                    String(root, "campaignId"), String(root, "rulesetHash"),
                    root.GetProperty("seed").GetUInt64(), String(root, "setupId"),
                    String(root, "setupHash"), String(root, "contentPackId"),
                    String(root, "contentHash"), String(root, "scenarioId"));
                var timing = CampaignDecisionTimingConfigurationV1.DeserializeCanonical(
                    Encoding.UTF8.GetBytes(root.GetProperty("timing").GetRawText()));
                journal.CreateCampaign(request, timing);
                return Results.Json(new { contractVersion = 2, campaignId = request.CampaignId },
                    statusCode: StatusCodes.Status201Created);
            }));
        api.MapPost("/{campaignId}/grants/{audience}",
            (HttpContext context, CampaignDurableAuthorityJournal journal,
                string campaignId, string audience) => ExecuteSync(() =>
            {
                RequireOperator(context, journal);
                var grant = journal.IssueSeatGrant(campaignId, Audience(audience),
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new
                {
                    contractVersion = 2,
                    grant = new
                    {
                        grant.CampaignId,
                        audience = Side(grant.Audience),
                        grant.Epoch,
                        grant.Secret,
                        grant.ExpiresUnixMs,
                    }
                });
            }));
        api.MapPost("/{campaignId}/seats", async (HttpContext context,
            CampaignDurableAuthorityJournal journal, string campaignId) =>
            await Execute(context, async () =>
            {
                using var document = await ReadBody(context);
                var root = document.RootElement;
                RequireShape(root, "contractVersion", "grant");
                RequireVersion(root);
                var grant = Grant(root.GetProperty("grant"));
                if (grant.CampaignId != campaignId) throw new UnauthorizedAccessException();
                var credential = journal.RedeemSeatGrant(grant);
                if (credential is null) throw new UnauthorizedAccessException();
                return Results.Json(new
                {
                    contractVersion = 2,
                    credential = new
                    {
                        credential.CampaignId,
                        audience = Side(credential.Audience),
                        credential.Epoch,
                        credential.Secret,
                        credential.ExpiresUnixMs,
                    }
                });
            }));
        api.MapPost("/{campaignId}/openings", async (HttpContext context,
            CampaignDurableAuthorityJournal journal, string campaignId) =>
            await Execute(context, async () =>
            {
                using var document = await ReadBody(context);
                var root = document.RootElement;
                RequireShape(root, "contractVersion", "credential", "stage", "actingElementId");
                RequireVersion(root);
                var credential = Credential(root.GetProperty("credential"), campaignId);
                var stage = String(root, "stage");
                var element = String(root, "actingElementId");
                var opening = stage switch
                {
                    "movement" => journal.QueryOwnerOpening(credential, element),
                    "combat-selection" => journal.QueryCombatOwnerOpening(credential, element),
                    "retreat-before-assault" => journal.QueryRbaOwnerOpening(credential, element),
                    "force-assignment" => journal.QueryForceOwnerOpening(credential, element),
                    "post-assault-retreat" => journal.QueryPostAssaultRetreatOwnerOpening(credential, element),
                    "custody" => journal.QueryCustodyOwnerOpening(credential, element),
                    "reserve-release" => journal.QueryReserveReleaseOwnerOpening(credential, element),
                    "cycle-control" => journal.QueryCycleControlOwnerOpening(credential),
                    "repeat-movement" => journal.QueryRepeatMovementOwnerOpening(credential),
                    _ => throw new JsonException("Unknown owner stage."),
                };
                return Results.Json(new
                {
                    contractVersion = 2,
                    opening.CampaignId,
                    audience = Side(opening.Audience),
                    stage,
                    opening.ActingElementId,
                    opening.Revision,
                    visibleCutBase64 = Convert.ToBase64String(opening.VisibleBytes),
                    opening.PublicActionRefs
                });
            }));
        api.MapPost("/{campaignId}/actions", async (HttpContext context,
            CampaignDurableAuthorityJournal journal, string campaignId) =>
            await Execute(context, async () =>
            {
                using var document = await ReadBody(context);
                var root = document.RootElement;
                RequireShape(root, "contractVersion", "credential", "stage", "actingElementId",
                    "openingRevision", "publicActionRef");
                RequireVersion(root);
                var credential = Credential(root.GetProperty("credential"), campaignId);
                var stage = String(root, "stage");
                var element = String(root, "actingElementId");
                var revision = root.GetProperty("openingRevision").GetInt64();
                var reference = String(root, "publicActionRef");
                var result = stage switch
                {
                    "movement" => journal.SubmitOwnerMovement(credential, element, revision, reference),
                    "combat-selection" => journal.SubmitOwnerCombatSelection(credential, element, revision, reference),
                    "rba-decline" => journal.SubmitOwnerRbaDecline(credential, element, revision, reference),
                    "rba-move" => journal.SubmitOwnerRbaMove(credential, element, revision, reference),
                    "force-assignment" => journal.SubmitOwnerForceSeal(credential, element, revision, reference),
                    "post-assault-retreat" => journal.SubmitOwnerPostAssaultRetreat(credential, element, revision, reference),
                    "custody" => journal.SubmitOwnerCustody(credential, element, revision, reference),
                    "reserve-release" => journal.SubmitOwnerReserveRelease(credential, element, revision, reference),
                    "cycle-control" => journal.SubmitOwnerCycleControl(credential, revision, reference),
                    "repeat-movement" => journal.SubmitOwnerRepeatMovement(credential, revision, reference),
                    _ => throw new JsonException("Unknown owner stage."),
                };
                return result.IsAccepted
                    ? Results.Json(new
                    {
                        contractVersion = 2,
                        accepted = true,
                        openingRevision = revision,
                        publicActionRef = reference
                    })
                    : Error(StatusCodes.Status409Conflict, "rejected");
            }));
    }

    private static bool SameOrigin(HttpContext context)
    {
        var fetchSite = context.Request.Headers["Sec-Fetch-Site"].ToString();
        if (fetchSite.Length > 0 && fetchSite != "same-origin") return false;
        var origin = context.Request.Headers.Origin.ToString();
        return origin.Length == 0 || origin == $"{context.Request.Scheme}://{context.Request.Host}";
    }

    private static void RequireOperator(HttpContext context,
        CampaignDurableAuthorityJournal journal)
    {
        if (!journal.AuthenticateOperator(
            context.Request.Headers["X-Sandtable-Operator"].ToString()))
            throw new UnauthorizedAccessException();
    }

    private static async Task<JsonDocument> ReadBody(HttpContext context)
    {
        if (context.Request.ContentLength is > MaxBodyBytes) throw new JsonException();
        return await JsonDocument.ParseAsync(context.Request.Body,
            new JsonDocumentOptions { MaxDepth = 12 }, context.RequestAborted);
    }

    private static void RequireShape(JsonElement value, params string[] properties)
    {
        if (value.ValueKind != JsonValueKind.Object
            || !value.EnumerateObject().Select(item => item.Name)
                .Order(StringComparer.Ordinal)
                .SequenceEqual(properties.Order(StringComparer.Ordinal), StringComparer.Ordinal))
            throw new JsonException("Invalid request shape.");
    }

    private static void RequireVersion(JsonElement root)
    {
        if (root.GetProperty("contractVersion").GetInt32() != 2) throw new JsonException();
    }

    private static string String(JsonElement root, string property)
    {
        if (root.GetProperty(property).ValueKind != JsonValueKind.String)
            throw new JsonException();
        var value = root.GetProperty(property).GetString();
        if (string.IsNullOrWhiteSpace(value) || value.Length > 256) throw new JsonException();
        return value;
    }

    private static CampaignActionAudience Audience(string value) => value switch
    {
        "axis" => CampaignActionAudience.Axis,
        "commonwealth" => CampaignActionAudience.Commonwealth,
        _ => throw new JsonException("Invalid audience."),
    };

    private static string Side(CampaignActionAudience audience) => audience switch
    {
        CampaignActionAudience.Axis => "axis",
        CampaignActionAudience.Commonwealth => "commonwealth",
        _ => throw new JsonException("Invalid audience."),
    };

    private static CampaignSeatGrant Grant(JsonElement value)
    {
        RequireShape(value, "campaignId", "audience", "epoch", "secret", "expiresUnixMs");
        return new CampaignSeatGrant(String(value, "campaignId"),
            Audience(String(value, "audience")), value.GetProperty("epoch").GetInt64(),
            String(value, "secret"), value.GetProperty("expiresUnixMs").GetInt64());
    }

    private static CampaignSeatCredential Credential(JsonElement value, string campaignId)
    {
        RequireShape(value, "campaignId", "audience", "epoch", "secret", "expiresUnixMs");
        if (String(value, "campaignId") != campaignId) throw new UnauthorizedAccessException();
        return new CampaignSeatCredential(campaignId,
            Audience(String(value, "audience")), value.GetProperty("epoch").GetInt64(),
            String(value, "secret"), value.GetProperty("expiresUnixMs").GetInt64());
    }

    private static async Task<IResult> Execute(HttpContext context, Func<Task<IResult>> action)
    {
        try { return await action(); }
        catch (UnauthorizedAccessException) { return Error(401, "unauthorized"); }
        catch (JsonException) { return Error(400, "invalid-request"); }
        catch (KeyNotFoundException) { return Error(400, "invalid-request"); }
        catch (FormatException) { return Error(400, "invalid-request"); }
        catch (InvalidOperationException) { return Error(409, "unavailable"); }
        catch (ArgumentException) { return Error(400, "invalid-request"); }
        catch (OverflowException) { return Error(400, "invalid-request"); }
    }

    private static IResult ExecuteSync(Func<IResult> action)
    {
        try { return action(); }
        catch (UnauthorizedAccessException) { return Error(401, "unauthorized"); }
        catch (JsonException) { return Error(400, "invalid-request"); }
        catch (KeyNotFoundException) { return Error(400, "invalid-request"); }
        catch (FormatException) { return Error(400, "invalid-request"); }
        catch (InvalidOperationException) { return Error(409, "unavailable"); }
        catch (ArgumentException) { return Error(400, "invalid-request"); }
        catch (OverflowException) { return Error(400, "invalid-request"); }
    }

    private static IResult Error(int status, string code) =>
        Results.Json(new { contractVersion = 2, code }, statusCode: status);
}
