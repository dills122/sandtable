using Microsoft.Data.Sqlite;

namespace Cna.OrleansHost.Api;

/// <summary>Schedules durable clock checks; Core remains sole adjudication authority.</summary>
internal sealed class CampaignRbaTimeoutService(CampaignDurableAuthorityJournal journal,
    TimeProvider clock, ILogger<CampaignRbaTimeoutService> logger) : BackgroundService
{
    private static readonly Action<ILogger, Exception?> LogEnumerationFailure =
        LoggerMessage.Define(LogLevel.Error, new EventId(1, "RbaEnumerationFailed"),
            "Unable to enumerate durable RBA deadlines.");
    private static readonly Action<ILogger, string, Exception?> LogCampaignFailure =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(2, "RbaCampaignFailed"),
            "Durable RBA deadline failed for campaign {CampaignId}.");
    private string cursor = "";
    internal const int BatchSize = 16;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1), clock);
        while (!stoppingToken.IsCancellationRequested)
        {
            SweepOnce(stoppingToken);
            if (!await timer.WaitForNextTickAsync(stoppingToken)) break;
        }
    }

    internal void SweepOnce(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<string> campaigns;
        try
        {
            campaigns = journal.CollectingRbaCampaigns(cursor, BatchSize);
            if (campaigns.Count == 0 && cursor.Length > 0)
            {
                cursor = "";
                campaigns = journal.CollectingRbaCampaigns(cursor, BatchSize);
            }
        }
        catch (SqliteException exception)
        {
            LogEnumerationFailure(logger, exception);
            return;
        }
        foreach (var campaign in campaigns)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Advance even after corruption so one campaign cannot starve later deadlines.
            cursor = campaign;
            try { journal.TickRbaRound(campaign); }
            catch (Exception exception) when (exception is InvalidOperationException
                or ArgumentException or SqliteException or System.Text.Json.JsonException
                or FormatException or OverflowException or InvalidCastException
                or Cna.Core.Campaigns.InvalidCampaignHistoryException)
            {
                LogCampaignFailure(logger, campaign, exception);
            }
        }
    }
}
