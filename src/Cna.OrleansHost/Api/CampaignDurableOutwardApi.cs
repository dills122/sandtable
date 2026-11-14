using System.Net;
using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.OrleansHost.Api;

/// <summary>
/// Versioned local entry to the normal durable campaign authority. All owner operations go
/// through the retained seat and opening fences in CampaignDurableAuthorityJournal.
/// </summary>
internal static class CampaignDurableOutwardApi
{
    private const int MaxBodyBytes = 8192;
    private const int MaxSourceBodyBytes = 24 * 1024 * 1024;

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
                if (size is { IsReadOnly: false }) size.MaxRequestBodySize =
                    context.Request.Method == "POST" && (context.Request.Path == "/api/v2/campaigns/source"
                        || context.Request.Path == "/api/v2/campaigns/pregame-source")
                        ? MaxSourceBodyBytes : MaxBodyBytes;
                await next(context);
            }));

        var api = app.MapGroup("/api/v2/campaigns");
        api.MapPost("/source", async (HttpContext context, CampaignDurableAuthorityJournal journal) =>
            await Execute(context, async () =>
            {
                RequireOperator(context, journal);
                using var document = await ReadBody(context, MaxSourceBodyBytes);
                var root = document.RootElement;
                RequireShape(root, "contractVersion", "campaignId", "rulesetHash", "seed", "scenarioId",
                    "expectedContentHash", "contentBase64", "capabilityNumerator", "capabilityDenominator", "cohesion");
                RequireVersion(root);
                var content = ContentSourceScheduledCombatPackArtifact.Read(root.GetProperty("contentBase64").GetBytesFromBase64());
                if (content.Identity.Hash != String(root, "expectedContentHash"))
                    throw new JsonException("Source content hash differs from the requested identity.");
                var initialization = new CampaignSourceInitialization(new CapabilityPointAmount(
                    root.GetProperty("capabilityNumerator").GetInt64(), root.GetProperty("capabilityDenominator").GetInt32()),
                    root.GetProperty("cohesion").GetInt32());
                var scenarioId = String(root, "scenarioId");
                var request = new CampaignCreationRequest(CampaignSourceCreation.RequestVersion,
                    String(root, "campaignId"), String(root, "rulesetHash"), root.GetProperty("seed").GetUInt64(),
                    CampaignSourceCreation.SetupId, CampaignSourceCreation.CalculateSetupHash(content.Identity, scenarioId, initialization),
                    content.Identity.PackId, content.Identity.Hash, scenarioId);
                journal.CreateCampaign(request, content, initialization);
                return Results.Json(new { contractVersion = 2, request.CampaignId, status = "initialization-only" },
                    statusCode: StatusCodes.Status201Created);
            }));
        // The pinned source excerpt is already part of the application. A client should
        // not need to construct its canonical content pack just to start this setup.
        api.MapPost("/pregame-source/graziani", async (HttpContext context, CampaignDurableAuthorityJournal journal) =>
            await Execute(context, async () =>
            {
                RequireOperator(context, journal);
                using var document = await ReadBody(context);
                var root = document.RootElement;
                RequireShape(root, "contractVersion", "campaignId", "seed",
                    "capabilityNumerator", "capabilityDenominator", "cohesion");
                RequireVersion(root);
                var content = Cna1979GrazianiCairoSourceExcerpt.Build();
                var initialization = new CampaignSourceInitialization(new CapabilityPointAmount(
                    root.GetProperty("capabilityNumerator").GetInt64(), root.GetProperty("capabilityDenominator").GetInt32()),
                    root.GetProperty("cohesion").GetInt32());
                var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
                    String(root, "campaignId"), Cna1979Ruleset.Manifest.Hash, root.GetProperty("seed").GetUInt64(),
                    CampaignPregameSourceCreation.SetupId, CampaignPregameSourceCreation.CalculateSetupHash(content.Identity, initialization),
                    content.Identity.PackId, content.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
                journal.CreatePregameSourceCampaign(request, content, initialization);
                return Results.Json(new
                {
                    contractVersion = 2,
                    request.CampaignId,
                    status = "incomplete",
                    policyStatus = "provisional"
                }, statusCode: StatusCodes.Status201Created);
            }));
        api.MapPost("/pregame-source", async (HttpContext context, CampaignDurableAuthorityJournal journal) =>
            await Execute(context, async () =>
            {
                RequireOperator(context, journal);
                using var document = await ReadBody(context, MaxSourceBodyBytes);
                var root = document.RootElement;
                RequireShape(root, "contractVersion", "campaignId", "rulesetHash", "seed", "scenarioId",
                    "expectedContentHash", "contentBase64", "capabilityNumerator", "capabilityDenominator", "cohesion");
                RequireVersion(root);
                var content = ContentSourceScheduledCombatPackArtifact.Read(root.GetProperty("contentBase64").GetBytesFromBase64());
                if (content.Identity.Hash != String(root, "expectedContentHash"))
                    throw new JsonException("Source content hash differs from the requested identity.");
                var initialization = new CampaignSourceInitialization(new CapabilityPointAmount(
                    root.GetProperty("capabilityNumerator").GetInt64(), root.GetProperty("capabilityDenominator").GetInt32()),
                    root.GetProperty("cohesion").GetInt32());
                var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
                    String(root, "campaignId"), String(root, "rulesetHash"), root.GetProperty("seed").GetUInt64(),
                    CampaignPregameSourceCreation.SetupId, CampaignPregameSourceCreation.CalculateSetupHash(content.Identity, initialization),
                    content.Identity.PackId, content.Identity.Hash, String(root, "scenarioId"));
                journal.CreatePregameSourceCampaign(request, content, initialization);
                return Results.Json(new
                {
                    contractVersion = 2,
                    request.CampaignId,
                    status = "incomplete",
                    policyStatus = "provisional"
                }, statusCode: StatusCodes.Status201Created);
            }));
        api.MapPost("/{campaignId}/source-axis-supplement", (HttpContext context,
            CampaignDurableAuthorityJournal journal, string campaignId) => ExecuteSync(() =>
            {
                RequireOperator(context, journal);
                var admitted = journal.AdmitSourceAxisSupplement(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-commonwealth-supplement", (HttpContext context,
            CampaignDurableAuthorityJournal journal, string campaignId) => ExecuteSync(() =>
            {
                RequireOperator(context, journal);
                var admitted = journal.AdmitSourceCommonwealthSupplement(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-priority-motorization", (HttpContext context,
            CampaignDurableAuthorityJournal journal, string campaignId) => ExecuteSync(() =>
            {
                RequireOperator(context, journal);
                var admitted = journal.AdmitSourcePriorityMotorization(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-attached-initial-loads", (HttpContext context,
            CampaignDurableAuthorityJournal journal, string campaignId) => ExecuteSync(() =>
            {
                RequireOperator(context, journal);
                var admitted = journal.AdmitSourceAttachedInitialLoads(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-initial-wrecks", (HttpContext context,
            CampaignDurableAuthorityJournal journal, string campaignId) => ExecuteSync(() =>
            {
                RequireOperator(context, journal);
                var admitted = journal.AdmitSourceInitialWrecks(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-axis-free-setup", (HttpContext context,
            CampaignDurableAuthorityJournal journal, string campaignId) => ExecuteSync(() =>
            {
                RequireOperator(context, journal);
                var admitted = journal.AdmitSourceAxisFreeSetup(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-axis-corps-setup", (HttpContext context,
            CampaignDurableAuthorityJournal journal, string campaignId) => ExecuteSync(() =>
            {
                RequireOperator(context, journal);
                var admitted = journal.AdmitSourceAxisCorpsSetup(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-maletti-hierarchy", (HttpContext context,
            CampaignDurableAuthorityJournal journal, string campaignId) => ExecuteSync(() =>
            {
                RequireOperator(context, journal);
                var admitted = journal.AdmitSourceMalettiHierarchy(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-gt2-correction", (HttpContext context,
            CampaignDurableAuthorityJournal journal, string campaignId) => ExecuteSync(() =>
            {
                RequireOperator(context, journal);
                var admitted = journal.AdmitSourceGt2Correction(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-cw-priority-catalog", (HttpContext context,
            CampaignDurableAuthorityJournal journal, string campaignId) => ExecuteSync(() =>
            {
                RequireOperator(context, journal);
                var admitted = journal.AdmitSourceCwPriorityCatalog(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-compact-checkpoint", (HttpContext context,
            CampaignDurableAuthorityJournal journal, string campaignId) => ExecuteSync(() =>
            {
                RequireOperator(context, journal);
                var admitted = journal.AdmitSourceCompactCheckpoint(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-fourth-indian-world", (HttpContext context,
            CampaignDurableAuthorityJournal journal, string campaignId) => ExecuteSync(() =>
            {
                RequireOperator(context, journal);
                var admitted = journal.AdmitSourceFourthIndianWorld(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-matruh-grouped-world", (HttpContext context,
            CampaignDurableAuthorityJournal journal, string campaignId) => ExecuteSync(() =>
            {
                RequireOperator(context, journal);
                var admitted = journal.AdmitSourceMatruhGroupedWorld(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-maletti-grouped-world", (HttpContext context,
            CampaignDurableAuthorityJournal journal, string campaignId) => ExecuteSync(() =>
            {
                RequireOperator(context, journal);
                var admitted = journal.AdmitSourceMalettiGroupedWorld(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-libyan-grouped-world", (HttpContext context,
            CampaignDurableAuthorityJournal journal, string campaignId) => ExecuteSync(() =>
            {
                RequireOperator(context, journal);
                var admitted = journal.AdmitSourceLibyanGroupedWorld(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-matruh-formation", (HttpContext context,
            CampaignDurableAuthorityJournal journal, string campaignId) => ExecuteSync(() =>
            {
                RequireOperator(context, journal);
                var admitted = journal.AdmitSourceMatruhFormation(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-cw-free-guns", (HttpContext context,
            CampaignDurableAuthorityJournal journal, string campaignId) => ExecuteSync(() =>
            {
                RequireOperator(context, journal);
                var admitted = journal.AdmitSourceCwFreeGuns(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-axis-priority-motorization", (HttpContext context,
            CampaignDurableAuthorityJournal journal, string campaignId) => ExecuteSync(() =>
            {
                RequireOperator(context, journal);
                var admitted = journal.AdmitSourceAxisPriorityMotorization(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-axis-minor-garrisons", (HttpContext context,
            CampaignDurableAuthorityJournal journal, string campaignId) => ExecuteSync(() =>
            {
                RequireOperator(context, journal);
                var admitted = journal.AdmitSourceAxisMinorGarrisons(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-battlefield", (HttpContext context,
            CampaignDurableAuthorityJournal journal, string campaignId) => ExecuteSync(() =>
            {
                RequireOperator(context, journal);
                var admitted = journal.AdmitSourceBattlefield(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-cairo-setup", (HttpContext context,
            CampaignDurableAuthorityJournal journal, string campaignId) => ExecuteSync(() =>
            {
                RequireOperator(context, journal);
                var admitted = journal.AdmitSourceCairoSetup(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-profiles", (HttpContext context,
            CampaignDurableAuthorityJournal journal, string campaignId) => ExecuteSync(() =>
            {
                RequireOperator(context, journal);
                var admitted = journal.AdmitAvailableSourceProfiles(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-cairo-world", (HttpContext context,
            CampaignDurableAuthorityJournal journal, string campaignId) => ExecuteSync(() =>
            {
                RequireOperator(context, journal);
                var admitted = journal.AdmitSourceCairoWorld(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-maletti-world", (HttpContext context,
            CampaignDurableAuthorityJournal journal, string campaignId) => ExecuteSync(() =>
            {
                RequireOperator(context, journal);
                var admitted = journal.PromoteSourceMaletti(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-independent-world", (HttpContext context,
            CampaignDurableAuthorityJournal journal, string campaignId) => ExecuteSync(() =>
            {
                RequireOperator(context, journal);
                var admitted = journal.PromoteSourceIndependent(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-expanded-independent-world", (HttpContext context,
            CampaignDurableAuthorityJournal journal, string campaignId) => ExecuteSync(() =>
            {
                RequireOperator(context, journal);
                var admitted = journal.PromoteSourceExpandedIndependent(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-axis-free-world", (HttpContext context,
            CampaignDurableAuthorityJournal journal, string campaignId) => ExecuteSync(() =>
            {
                RequireOperator(context, journal);
                var admitted = journal.PromoteSourceAxisFreeWorld(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-axis-corps-world", (HttpContext context,
            CampaignDurableAuthorityJournal journal, string campaignId) => ExecuteSync(() =>
            {
                RequireOperator(context, journal);
                var admitted = journal.PromoteSourceAxisCorpsWorld(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-certified-maletti-world", (HttpContext context,
            CampaignDurableAuthorityJournal journal, string campaignId) => ExecuteSync(() =>
            {
                RequireOperator(context, journal);
                var admitted = journal.PromoteSourceCertifiedMalettiWorld(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-matruh-detached-world", (HttpContext context,
            CampaignDurableAuthorityJournal journal, string campaignId) => ExecuteSync(() =>
            {
                RequireOperator(context, journal);
                var admitted = journal.PromoteSourceMatruhDetachedWorld(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-cw-priority-world", (HttpContext context,
            CampaignDurableAuthorityJournal journal, string campaignId) => ExecuteSync(() =>
            {
                RequireOperator(context, journal);
                var admitted = journal.PromoteSourceCwPriorityWorld(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-cw-free-guns-world", (HttpContext context,
            CampaignDurableAuthorityJournal journal, string campaignId) => ExecuteSync(() =>
            {
                RequireOperator(context, journal);
                var admitted = journal.PromoteSourceCwFreeGunsWorld(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-local-map", (HttpContext context,
            CampaignDurableAuthorityJournal journal, string campaignId) => ExecuteSync(() =>
            {
                RequireOperator(context, journal);
                var admitted = journal.AdmitSourceLocalMap(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapGet("/{campaignId}/source-initialization", (HttpContext context,
            CampaignDurableAuthorityJournal journal, string campaignId) => ExecuteSync(() =>
            {
                RequireOperator(context, journal);
                var source = journal.ReadSourceVerified(campaignId);
                return Results.Json(new
                {
                    contractVersion = 2,
                    campaignId,
                    status = "initialization-only",
                    source.Request.ScenarioId,
                    source.Request.ContentHash,
                    pendingGroupCount = source.PendingArrivalGroupIds.Count
                });
            }));
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
                if (stage == "pregame-axis-supply" && element != "land.pregame-axis-supply")
                    throw new JsonException("Invalid pregame opening key.");
                if (stage == "pregame-axis-supply-finish" && element != "land.pregame-axis-supply-finish")
                    throw new JsonException("Invalid pregame finish opening key.");
                if (stage == "source-axis-dump" && element != "land.graziani-axis-dumps")
                    throw new JsonException("Invalid Axis dump opening key.");
                if (stage == "source-commonwealth-supply" && element != "land.graziani-commonwealth-supply")
                    throw new JsonException("Invalid Commonwealth supply opening key.");
                if (stage == "source-dummy" && element != "land.graziani-dummy-groups")
                    throw new JsonException("Invalid dummy opening key.");
                if (stage == "source-actions" && element != "land.graziani-source-actions")
                    throw new JsonException("Invalid source actions opening key.");
                if (stage == "source-unattached-cargo" && element != "land.graziani-unattached-cargo")
                    throw new JsonException("Invalid unattached cargo opening key.");
                if (stage == "source-motorization" && element != "land.graziani-motorization")
                    throw new JsonException("Invalid motorization opening key.");
                if (stage == "source-axis-gt1-supply" && element != "land.graziani-axis-gt1-supply")
                    throw new JsonException("Invalid Axis GT1 supply opening key.");
                if (stage == "source-commonwealth-supply-plan" && element != "land.graziani-commonwealth-supply-plan")
                    throw new JsonException("Invalid Commonwealth supply plan opening key.");
                if (stage == "source-autoblinda" && element != "land.graziani-axis-autoblinda")
                    throw new JsonException("Invalid Autoblinda opening key.");
                if (stage == "source-cw-free-guns" && element != "land.graziani-commonwealth-free-guns")
                    throw new JsonException("Invalid Commonwealth free guns opening key.");
                if (stage == "source-matruh-formation" && element != "land.graziani-commonwealth-matruh-formation")
                    throw new JsonException("Invalid Matruh formation opening key.");
                if (stage == "source-cw-priority-catalog" && element != "land.graziani-commonwealth-priority-catalog")
                    throw new JsonException("Invalid Commonwealth priority catalog opening key.");
                if (stage == "source-gt2-correction" && element != "land.graziani-axis-gt2-correction")
                    throw new JsonException("Invalid GT2 correction opening key.");
                if (stage == "source-maletti-hierarchy" && element != "land.graziani-axis-maletti-hierarchy")
                    throw new JsonException("Invalid Maletti hierarchy opening key.");
                if (stage == "source-axis-corps-setup" && element != "land.graziani-axis-corps-setup")
                    throw new JsonException("Invalid Axis corps setup opening key.");
                if (stage == "source-axis-free-setup" && element != "land.graziani-axis-free-setup")
                    throw new JsonException("Invalid Axis free setup opening key.");
                if (stage == "source-initial-wrecks" && element != "land.graziani-commonwealth-initial-wrecks")
                    throw new JsonException("Invalid initial wrecks opening key.");
                if (stage == "source-attached-initial-loads" && element != "land.graziani-commonwealth-attached-initial-loads")
                    throw new JsonException("Invalid attached initial loads opening key.");
                if (stage == "source-priority-motorization" && element != "land.graziani-commonwealth-priority-motorization")
                    throw new JsonException("Invalid priority motorization opening key.");
                if (stage == "source-axis-priority-motorization" && element != "land.graziani-axis-priority-motorization")
                    throw new JsonException("Invalid Axis priority motorization opening key.");
                if (stage == "source-cairo-setup" && element != "land.graziani-commonwealth-cairo-setup")
                    throw new JsonException("Invalid Cairo setup opening key.");
                var opening = stage switch
                {
                    "pregame-axis-supply" => journal.QueryPregameAxisSupplyOpening(credential),
                    "pregame-axis-supply-finish" => journal.QueryPregameAxisSupplyFinishOpening(credential),
                    "source-axis-dump" => journal.QuerySourceAxisDumpOpening(credential),
                    "source-commonwealth-supply" => journal.QuerySourceCommonwealthSupplyOpening(credential),
                    "source-dummy" => journal.QuerySourceDummyOpening(credential),
                    "source-motorization" => journal.QuerySourceMotorizationOpening(credential),
                    "source-unattached-cargo" => journal.QuerySourceUnattachedCargoOpening(credential),
                    "source-actions" => journal.QuerySourceActionsOpening(credential),
                    "source-axis-gt1-supply" => journal.QuerySourceAxisGt1SupplyOpening(credential),
                    "source-commonwealth-supply-plan" => journal.QuerySourceCommonwealthSupplyPlanOpening(credential),
                    "source-autoblinda" => journal.QuerySourceAutoblindaOpening(credential),
                    "source-priority-motorization" => journal.QuerySourcePriorityMotorizationOpening(credential),
                    "source-attached-initial-loads" => journal.QuerySourceAttachedInitialLoadsOpening(credential),
                    "source-initial-wrecks" => journal.QuerySourceInitialWrecksOpening(credential),
                    "source-axis-free-setup" => journal.QuerySourceAxisFreeSetupOpening(credential),
                    "source-axis-corps-setup" => journal.QuerySourceAxisCorpsSetupOpening(credential),
                    "source-maletti-hierarchy" => journal.QuerySourceMalettiHierarchyOpening(credential),
                    "source-gt2-correction" => journal.QuerySourceGt2CorrectionOpening(credential),
                    "source-cw-priority-catalog" => journal.QuerySourceCwPriorityCatalogOpening(credential),
                    "source-matruh-formation" => journal.QuerySourceMatruhFormationOpening(credential),
                    "source-cw-free-guns" => journal.QuerySourceCwFreeGunsOpening(credential),
                    "source-axis-priority-motorization" => journal.QuerySourceAxisPriorityOpening(credential),
                    "source-cairo-setup" => journal.QuerySourceCairoSetupOpening(credential),
                    "movement" => journal.QueryOwnerOpening(credential, element),
                    "combat-selection" => journal.QueryCombatOwnerOpening(credential, element),
                    "retreat-before-assault" => journal.QueryRbaOwnerOpening(credential, element),
                    "force-assignment" => journal.QueryForceOwnerOpening(credential, element),
                    "post-assault-retreat" => journal.QueryPostAssaultRetreatOwnerOpening(credential, element),
                    "custody" => journal.QueryCustodyOwnerOpening(credential, element),
                    "reserve-release" => journal.QueryReserveReleaseOwnerOpening(credential, element),
                    "cycle-control" => journal.QueryCycleControlOwnerOpening(credential),
                    "truck-convoy-pass" => journal.QueryTruckConvoyPassOwnerOpening(credential),
                    "post-combat-pass" => journal.QueryPostCombatPassOwnerOpening(credential),
                    "second-reserve" => journal.QuerySecondReserveOwnerOpening(credential),
                    "second-movement" => journal.QuerySecondMovementOwnerOpening(credential, element),
                    "second-movement-completion" => journal.QuerySecondMovementCompletionOwnerOpening(credential),
                    "repeat-movement" => journal.QueryRepeatMovementOwnerOpening(credential),
                    "repeat-combat-selection" => journal.QueryRepeatCombatOwnerOpening(
                        credential, element),
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
                if (stage == "pregame-axis-supply" && element != "land.pregame-axis-supply")
                    throw new JsonException("Invalid pregame opening key.");
                if (stage == "pregame-axis-supply-finish" && element != "land.pregame-axis-supply-finish")
                    throw new JsonException("Invalid pregame finish opening key.");
                if (stage == "source-axis-dump" && element != "land.graziani-axis-dumps")
                    throw new JsonException("Invalid Axis dump opening key.");
                if (stage == "source-commonwealth-supply" && element != "land.graziani-commonwealth-supply")
                    throw new JsonException("Invalid Commonwealth supply opening key.");
                if (stage == "source-dummy" && element != "land.graziani-dummy-groups")
                    throw new JsonException("Invalid dummy opening key.");
                if (stage == "source-actions" && element != "land.graziani-source-actions")
                    throw new JsonException("Invalid source actions opening key.");
                if (stage == "source-unattached-cargo" && element != "land.graziani-unattached-cargo")
                    throw new JsonException("Invalid unattached cargo opening key.");
                if (stage == "source-motorization" && element != "land.graziani-motorization")
                    throw new JsonException("Invalid motorization opening key.");
                if (stage == "source-axis-gt1-supply" && element != "land.graziani-axis-gt1-supply")
                    throw new JsonException("Invalid Axis GT1 supply opening key.");
                if (stage == "source-commonwealth-supply-plan" && element != "land.graziani-commonwealth-supply-plan")
                    throw new JsonException("Invalid Commonwealth supply plan opening key.");
                if (stage == "source-autoblinda" && element != "land.graziani-axis-autoblinda")
                    throw new JsonException("Invalid Autoblinda opening key.");
                if (stage == "source-cw-free-guns" && element != "land.graziani-commonwealth-free-guns")
                    throw new JsonException("Invalid Commonwealth free guns opening key.");
                if (stage == "source-matruh-formation" && element != "land.graziani-commonwealth-matruh-formation")
                    throw new JsonException("Invalid Matruh formation opening key.");
                if (stage == "source-cw-priority-catalog" && element != "land.graziani-commonwealth-priority-catalog")
                    throw new JsonException("Invalid Commonwealth priority catalog opening key.");
                if (stage == "source-gt2-correction" && element != "land.graziani-axis-gt2-correction")
                    throw new JsonException("Invalid GT2 correction opening key.");
                if (stage == "source-maletti-hierarchy" && element != "land.graziani-axis-maletti-hierarchy")
                    throw new JsonException("Invalid Maletti hierarchy opening key.");
                if (stage == "source-axis-corps-setup" && element != "land.graziani-axis-corps-setup")
                    throw new JsonException("Invalid Axis corps setup opening key.");
                if (stage == "source-axis-free-setup" && element != "land.graziani-axis-free-setup")
                    throw new JsonException("Invalid Axis free setup opening key.");
                if (stage == "source-initial-wrecks" && element != "land.graziani-commonwealth-initial-wrecks")
                    throw new JsonException("Invalid initial wrecks opening key.");
                if (stage == "source-attached-initial-loads" && element != "land.graziani-commonwealth-attached-initial-loads")
                    throw new JsonException("Invalid attached initial loads opening key.");
                if (stage == "source-priority-motorization" && element != "land.graziani-commonwealth-priority-motorization")
                    throw new JsonException("Invalid priority motorization opening key.");
                if (stage == "source-axis-priority-motorization" && element != "land.graziani-axis-priority-motorization")
                    throw new JsonException("Invalid Axis priority motorization opening key.");
                if (stage == "source-cairo-setup" && element != "land.graziani-commonwealth-cairo-setup")
                    throw new JsonException("Invalid Cairo setup opening key.");
                var result = stage switch
                {
                    "pregame-axis-supply" => journal.SubmitPregameAxisSupply(credential, revision, reference),
                    "pregame-axis-supply-finish" => journal.SubmitPregameAxisSupplyFinish(credential, revision, reference),
                    "source-axis-dump" => journal.SubmitSourceAxisDump(credential, revision, reference),
                    "source-commonwealth-supply" => journal.SubmitSourceCommonwealthSupply(credential, revision, reference),
                    "source-dummy" => journal.SubmitSourceDummy(credential, revision, reference),
                    "source-motorization" => journal.SubmitSourceMotorization(credential, revision, reference),
                    "source-unattached-cargo" => journal.SubmitSourceUnattachedCargo(credential, revision, reference),
                    "source-actions" => journal.SubmitSourceActions(credential, revision, reference),
                    "source-axis-gt1-supply" => journal.SubmitSourceAxisGt1Supply(credential, revision, reference),
                    "source-commonwealth-supply-plan" => journal.SubmitSourceCommonwealthSupplyPlan(credential, revision, reference),
                    "source-autoblinda" => journal.SubmitSourceAutoblinda(credential, revision, reference),
                    "source-priority-motorization" => journal.SubmitSourcePriorityMotorization(credential, revision, reference),
                    "source-attached-initial-loads" => journal.SubmitSourceAttachedInitialLoads(credential, revision, reference),
                    "source-initial-wrecks" => journal.PlaceSourceInitialWreck(credential, revision, reference),
                    "source-axis-free-setup" => journal.PlaceSourceAxisFreeUnit(credential, revision, reference),
                    "source-axis-corps-setup" => journal.PlaceSourceAxisCorpsUnit(credential, revision, reference),
                    "source-maletti-hierarchy" => journal.SubmitSourceMalettiHierarchy(credential, revision, reference),
                    "source-gt2-correction" => journal.SubmitSourceGt2Correction(credential, revision, reference),
                    "source-cw-priority-catalog" => journal.SubmitSourceCwPriorityCatalog(credential, revision, reference),
                    "source-matruh-formation" => journal.SubmitSourceMatruhFormation(credential, revision, reference),
                    "source-cw-free-guns" => journal.PlaceSourceCwFreeGun(credential, revision, reference),
                    "source-axis-priority-motorization" => journal.SubmitSourceAxisPriority(credential, revision, reference),
                    "source-cairo-setup" => journal.SubmitSourceCairoUnit(credential, revision, reference),
                    "movement" => journal.SubmitOwnerMovement(credential, element, revision, reference),
                    "combat-selection" => journal.SubmitOwnerCombatSelection(credential, element, revision, reference),
                    "rba-decline" => journal.SubmitOwnerRbaDecline(credential, element, revision, reference),
                    "rba-move" => journal.SubmitOwnerRbaMove(credential, element, revision, reference),
                    "force-assignment" => journal.SubmitOwnerForceSeal(credential, element, revision, reference),
                    "post-assault-retreat" => journal.SubmitOwnerPostAssaultRetreat(credential, element, revision, reference),
                    "custody" => journal.SubmitOwnerCustody(credential, element, revision, reference),
                    "reserve-release" => journal.SubmitOwnerReserveRelease(credential, element, revision, reference),
                    "cycle-control" => journal.SubmitOwnerCycleControl(credential, revision, reference),
                    "truck-convoy-pass" => journal.SubmitOwnerTruckConvoyPass(credential, revision, reference),
                    "post-combat-pass" => journal.SubmitOwnerPostCombatPass(credential, revision, reference),
                    "second-reserve" => journal.SubmitOwnerSecondReserve(credential, revision, reference),
                    "second-movement" => journal.SubmitOwnerSecondMovement(credential, element, revision, reference),
                    "second-movement-completion" => journal.SubmitOwnerSecondMovementCompletion(credential, revision, reference),
                    "repeat-movement" => journal.SubmitOwnerRepeatMovement(credential, revision, reference),
                    "repeat-combat-selection" => journal.SubmitOwnerRepeatCombat(
                        credential, element, revision, reference),
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

    private static async Task<JsonDocument> ReadBody(HttpContext context, int maximumBytes = MaxBodyBytes)
    {
        if (context.Request.ContentLength > maximumBytes) throw new JsonException();
        if (maximumBytes != MaxBodyBytes)
        {
            using var buffer = new MemoryStream();
            var chunk = new byte[81920];
            int read;
            while ((read = await context.Request.Body.ReadAsync(chunk, context.RequestAborted)) != 0)
            {
                if (buffer.Length + read > maximumBytes) throw new JsonException("Source request is too large.");
                buffer.Write(chunk, 0, read);
            }
            return JsonDocument.Parse(buffer.ToArray(), new JsonDocumentOptions { MaxDepth = 12 });
        }
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
        catch (InvalidContentPackException) { return Error(400, "invalid-request"); }
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
