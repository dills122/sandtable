using Microsoft.Data.Sqlite;

namespace Cna.OrleansHost.Api;

/// <summary>Schedules durable clock checks; Core remains sole adjudication authority.</summary>
internal sealed class CampaignCombatTimeoutService(CampaignDurableAuthorityJournal journal,
    TimeProvider clock, ILogger<CampaignCombatTimeoutService> logger) : BackgroundService
{
    private static readonly Action<ILogger, Exception?> LogEnumerationFailure =
        LoggerMessage.Define(LogLevel.Error, new EventId(1, "CombatEnumerationFailed"),
            "Unable to enumerate durable Combat deadlines.");
    private static readonly Action<ILogger, string, Exception?> LogCampaignFailure =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(2, "CombatCampaignFailed"),
            "Durable Combat deadline failed for campaign {CampaignId}.");
    private string rbaCursor = "";
    private string forceCursor = "";
    private string cycleCursor = "";
    private string reserveCursor = "";
    private string custodyCursor = "";
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
        SweepLane(ref rbaCursor, journal.CollectingRbaCampaigns, journal.TickRbaRound, cancellationToken);
        SweepLane(ref forceCursor, journal.CollectingForceCampaigns, journal.TickForceRound, cancellationToken);
        SweepLane(ref custodyCursor, journal.CollectingCustodyCampaigns, journal.TickCustodyRound, cancellationToken);
        SweepLane(ref reserveCursor, journal.CollectingReserveCampaigns, journal.TickReserveRound, cancellationToken);
        SweepLane(ref cycleCursor, journal.CollectingCycleCampaigns, journal.TickCycleRound, cancellationToken);
    }

    private void SweepLane(ref string cursor, Func<string, int, IReadOnlyList<string>> enumerate,
        Func<string, bool> tick, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<string> campaigns;
        try
        {
            campaigns = enumerate(cursor, BatchSize);
            if (campaigns.Count == 0 && cursor.Length > 0)
            {
                cursor = "";
                campaigns = enumerate(cursor, BatchSize);
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
            try { _ = tick(campaign); }
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
