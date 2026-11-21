using System.Net;
using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.OrleansHost.Campaigns;
using Orleans;

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
        if (app.Services.GetService<CampaignDurableAuthorityJournalStore>() is null)
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
        api.MapPost("/source", async (HttpContext context, CampaignDurableAuthorityJournalStore journal) =>
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
                await journal.CreateCampaignAsync(request, content, initialization);
                return Results.Json(new { contractVersion = 2, request.CampaignId, status = "initialization-only" },
                    statusCode: StatusCodes.Status201Created);
            }));
        // The pinned source excerpt is already part of the application. A client should
        // not need to construct its canonical content pack just to start this setup.
        api.MapPost("/pregame-source/graziani", async (HttpContext context, CampaignDurableAuthorityJournalStore journal) =>
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
                await journal.CreatePregameSourceCampaignAsync(request, content, initialization);
                return Results.Json(new
                {
                    contractVersion = 2,
                    request.CampaignId,
                    status = "incomplete",
                    policyStatus = "provisional"
                }, statusCode: StatusCodes.Status201Created);
            }));
        api.MapPost("/pregame-source", async (HttpContext context, CampaignDurableAuthorityJournalStore journal) =>
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
                await journal.CreatePregameSourceCampaignAsync(request, content, initialization);
                return Results.Json(new
                {
                    contractVersion = 2,
                    request.CampaignId,
                    status = "incomplete",
                    policyStatus = "provisional"
                }, statusCode: StatusCodes.Status201Created);
            }));
        api.MapPost("/{campaignId}/source-axis-supplement", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.AdmitSourceAxisSupplementAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-commonwealth-supplement", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.AdmitSourceCommonwealthSupplementAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-priority-motorization", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.AdmitSourcePriorityMotorizationAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-attached-initial-loads", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.AdmitSourceAttachedInitialLoadsAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-initial-wrecks", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.AdmitSourceInitialWrecksAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-axis-free-setup", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.AdmitSourceAxisFreeSetupAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-axis-corps-setup", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.AdmitSourceAxisCorpsSetupAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-maletti-hierarchy", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.AdmitSourceMalettiHierarchyAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-gt2-correction", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.AdmitSourceGt2CorrectionAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-cw-priority-catalog", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.AdmitSourceCwPriorityCatalogAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-compact-checkpoint", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.AdmitSourceCompactCheckpointAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-fourth-indian-world", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.AdmitSourceFourthIndianWorldAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-matruh-grouped-world", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.AdmitSourceMatruhGroupedWorldAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-maletti-grouped-world", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.AdmitSourceMalettiGroupedWorldAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-libyan-grouped-world", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.AdmitSourceLibyanGroupedWorldAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-italian-divisions-grouped-world", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.AdmitSourceItalianDivisionsGroupedWorldAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-seventh-armored-grouped-world", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.AdmitSourceSeventhArmoredGroupedWorldAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-sixteenth-brigade-grouped-world", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.AdmitSourceSixteenthBrigadeGroupedWorldAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-catanzaro-grouped-world", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.AdmitSourceCatanzaroGroupedWorldAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-turn/start", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var started = await journal.StartSourceTurnAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, started });
            }));
        api.MapPost("/{campaignId}/source-turn/initiative", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var resolved = await journal.ResolveSourceInitiativeAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, resolved });
            }));
        api.MapPost("/{campaignId}/source-turn/complete-gt1-scheduling", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var completed = await journal.CompleteSourceGt1SchedulingAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, completed });
            }));
        api.MapPost("/{campaignId}/source-turn/complete-gt1-organization", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var completed = await journal.CompleteSourceGt1OrganizationAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, completed });
            }));
        api.MapPost("/{campaignId}/source-turn/complete-gt1-stage1-arrivals", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var completed = await journal.CompleteSourceGt1StageOneArrivalsAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, completed });
            }));
        api.MapPost("/{campaignId}/source-turn/complete-gt1-stage1-fleet", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var completed = await journal.CompleteSourceGt1StageOneFleetAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, completed });
            }));
        api.MapPost("/{campaignId}/source-turn/complete-gt1-first-breakdown", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var completed = await journal.CompleteSourceGt1FirstBreakdownAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, completed });
            }));
        api.MapPost("/{campaignId}/source-turn/gt1-weather", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var resolved = await journal.ResolveSourceGt1WeatherAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, resolved });
            }));
        api.MapPost("/{campaignId}/source-turn/gt1-lane2", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var announced = await journal.AnnounceSourceGt1Lane2Async(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, announced });
            }));
        api.MapPost("/{campaignId}/source-turn/gt1-lane3", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var announced = await journal.AnnounceSourceGt1Lane3Async(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, announced });
            }));
        api.MapPost("/{campaignId}/source-seventh-shell-correction", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var corrected = await journal.CorrectSourceSeventhShellAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, corrected });
            }));
        api.MapPost("/{campaignId}/source-axis-minor-garrisons-activation", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var activated = await journal.ActivateSourceAxisMinorGarrisonsAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, activated });
            }));
        api.MapPost("/{campaignId}/source-axis-garrison-remainder-activation", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var activated = await journal.ActivateSourceAxisGarrisonRemainderAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, activated });
            }));
        api.MapPost("/{campaignId}/source-giarabub-motor-machine-gun-activation", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var activated = await journal.ActivateSourceGiarabubMotorMachineGunAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, activated });
            }));
        api.MapPost("/{campaignId}/source-cw-frontline-activation", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var activated = await journal.ActivateSourceCwFrontlineAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, activated });
            }));
        api.MapPost("/{campaignId}/source-giarabub-motor-placement-policy", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.AdmitSourceGiarabubMotorPlacementPolicyAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted });
            }));
        api.MapPost("/{campaignId}/source-turn/gt1-multiple-lanes", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var announced = await journal.AnnounceSourceGt1MultiLanesAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, announced });
            }));
        api.MapPost("/{campaignId}/source-matruh-formation", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.AdmitSourceMatruhFormationAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-cw-free-guns", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.AdmitSourceCwFreeGunsAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-axis-priority-motorization", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.AdmitSourceAxisPriorityMotorizationAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-axis-minor-garrisons", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.AdmitSourceAxisMinorGarrisonsAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-battlefield", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.AdmitSourceBattlefieldAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-cairo-setup", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.AdmitSourceCairoSetupAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-profiles", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.AdmitAvailableSourceProfilesAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-cairo-world", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.AdmitSourceCairoWorldAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-maletti-world", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.PromoteSourceMalettiAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-independent-world", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.PromoteSourceIndependentAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-expanded-independent-world", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.PromoteSourceExpandedIndependentAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-axis-free-world", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.PromoteSourceAxisFreeWorldAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-axis-corps-world", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.PromoteSourceAxisCorpsWorldAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-certified-maletti-world", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.PromoteSourceCertifiedMalettiWorldAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-matruh-detached-world", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.PromoteSourceMatruhDetachedWorldAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-cw-priority-world", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.PromoteSourceCwPriorityWorldAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-cw-free-guns-world", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.PromoteSourceCwFreeGunsWorldAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapPost("/{campaignId}/source-local-map", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var admitted = await journal.AdmitSourceLocalMapAsync(campaignId,
                    context.Request.Headers["X-Sandtable-Operator"].ToString());
                return Results.Json(new { contractVersion = 2, campaignId, admitted, status = "setup-incomplete" });
            }));
        api.MapGet("/{campaignId}/source-initialization", (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var source = await journal.ReadSourceVerifiedAsync(campaignId);
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
        api.MapPost("/", async (HttpContext context, CampaignDurableAuthorityJournalStore journal) =>
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
                await journal.CreateCampaignAsync(request, timing);
                return Results.Json(new { contractVersion = 2, campaignId = request.CampaignId },
                    statusCode: StatusCodes.Status201Created);
            }));
        api.MapPost("/{campaignId}/grants/{audience}",
            (HttpContext context, CampaignDurableAuthorityJournalStore journal,
                string campaignId, string audience) => ExecuteWithoutBody(async () =>
            {
                RequireOperator(context, journal);
                var grant = await journal.IssueSeatGrantAsync(campaignId, Audience(audience),
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
            CampaignDurableAuthorityJournalStore journal, string campaignId) =>
            await Execute(context, async () =>
            {
                using var document = await ReadBody(context);
                var root = document.RootElement;
                RequireShape(root, "contractVersion", "grant");
                RequireVersion(root);
                var grant = Grant(root.GetProperty("grant"));
                if (grant.CampaignId != campaignId) throw new UnauthorizedAccessException();
                var credential = await journal.RedeemSeatGrantAsync(grant);
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
        api.MapPost("/{campaignId}/source-counter-contacts", async (HttpContext context,
            CampaignDurableAuthorityJournalStore journal, string campaignId) =>
            await Execute(context, async () =>
            {
                using var document = await ReadBody(context);
                var root = document.RootElement;
                RequireShape(root, "contractVersion", "credential", "counterId");
                RequireVersion(root);
                var credential = Credential(root.GetProperty("credential"), campaignId);
                var observation = await journal.QuerySourceCounterContactsAsync(credential, String(root, "counterId"));
                return Results.Json(new { contractVersion = 2, campaignId, observationBase64 = Convert.ToBase64String(observation) });
            }));
        api.MapPost("/{campaignId}/openings", async (HttpContext context,
            IGrainFactory grainFactory, string campaignId) =>
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
                // Phase C3b of issue #439: opening queries write durable state (opening/action
                // references, the open round's high-water mark, and possibly a committed Core
                // timeout), so they run behind CampaignGrain's single-writer boundary too.
                var opening = await grainFactory.GetGrain<ICampaignGrain>(campaignId)
                    .QueryOwnerOpeningAsync(stage, credential, element);
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
            IGrainFactory grainFactory, string campaignId) =>
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
                // Phase C3a of issue #439: every stage below can open or close a combat round
                // (they all funnel through CampaignDurableAuthorityJournal.Submissions.cs's one
                // SubmitOwnerActionCore chokepoint), so this dispatch -- and the round-timeout
                // grain arming/disarming it triggers -- now runs behind CampaignGrain's single-
                // writer-per-campaign boundary instead of calling the store directly. See
                // ICampaignGrain's remarks for the full stage-to-store-method mapping (moved
                // there unchanged) and for which other store calls deliberately still bypass the
                // grain.
                var stageKnown = stage is "pregame-axis-supply" or "pregame-axis-supply-finish"
                    or "source-axis-dump" or "source-commonwealth-supply" or "source-dummy"
                    or "source-motorization" or "source-unattached-cargo" or "source-actions"
                    or "source-axis-gt1-supply" or "source-commonwealth-supply-plan"
                    or "source-autoblinda" or "source-priority-motorization"
                    or "source-attached-initial-loads" or "source-initial-wrecks"
                    or "source-axis-free-setup" or "source-axis-corps-setup"
                    or "source-maletti-hierarchy" or "source-gt2-correction"
                    or "source-cw-priority-catalog" or "source-matruh-formation"
                    or "source-cw-free-guns" or "source-axis-priority-motorization"
                    or "source-cairo-setup" or "movement" or "initiative-order" or "reserve-designation" or "reaction" or "combat-selection"
                    or "rba-decline" or "force-assignment"
                    or "post-assault-retreat" or "custody" or "reserve-release"
                    or "cycle-control" or "truck-convoy-pass" or "post-combat-pass"
                    or "second-reserve" or "second-movement" or "second-movement-completion"
                    or "repeat-movement" or "repeat-combat-selection";
                if (!stageKnown) throw new JsonException("Unknown owner stage.");
                var result = await grainFactory.GetGrain<ICampaignGrain>(campaignId)
                    .SubmitOwnerActionAsync(stage, credential, element, revision, reference);
                return result.IsAccepted
                    ? Results.Json(new
                    {
                        contractVersion = 2,
                        accepted = true,
                        openingRevision = revision,
                        publicActionRef = reference
                    })
                    : Error(StatusCodes.Status409Conflict, result.RejectionCode ?? "rejected");
            }));
    }

    private static bool SameOrigin(HttpContext context)
    {
        var fetchSite = context.Request.Headers["Sec-Fetch-Site"].ToString();
        var origin = context.Request.Headers.Origin.ToString();
        // Absence of both headers is not proof of same-origin - it is simply the
        // signature of a client that supplies neither. Require positive same-origin
        // evidence from at least one of them before trusting the rest of this check (#319).
        if (fetchSite.Length == 0 && origin.Length == 0) return false;
        if (fetchSite.Length > 0 && fetchSite != "same-origin") return false;
        return origin.Length == 0 || origin == $"{context.Request.Scheme}://{context.Request.Host}";
    }

    private static void RequireOperator(HttpContext context,
        CampaignDurableAuthorityJournalStore journal)
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

    private static async Task<IResult> ExecuteWithoutBody(Func<Task<IResult>> action)
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

    private static IResult Error(int status, string code) =>
        Results.Json(new { contractVersion = 2, code }, statusCode: status);
}
