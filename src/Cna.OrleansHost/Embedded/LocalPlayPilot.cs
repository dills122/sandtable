using System.Net;
using System.Net.Sockets;
using Cna.Core.Campaigns;
using Cna.OrleansHost.Api;
using Microsoft.AspNetCore.Hosting;

namespace Cna.OrleansHost.Embedded;

/// <summary>Explicit, Development-only single-process pilot. Never configures remote Orleans or HTTP ingress.</summary>
internal sealed class LocalPlayPilot : IDisposable
{
    private readonly FileStream directoryLock;

    private LocalPlayPilot(FileStream directoryLock, int siloPort)
    {
        this.directoryLock = directoryLock;
        SiloPort = siloPort;
    }

    internal int SiloPort { get; }

    internal static LocalPlayPilot? Configure(WebApplicationBuilder builder)
    {
        if (builder.Configuration["SANDTABLE_LOCAL_PLAY"] != "1") return null;
        if (!builder.Environment.IsDevelopment())
            throw new InvalidOperationException("SANDTABLE_LOCAL_PLAY requires Development hosting; v2 gates are unchanged.");
        if (!string.IsNullOrWhiteSpace(builder.Configuration["SANDTABLE_POSTGRES_HOT_CONNECTION_STRING"])
            || !string.IsNullOrWhiteSpace(builder.Configuration["SANDTABLE_POSTGRES_ORLEANS_CONNECTION_STRING"])
            || !string.IsNullOrWhiteSpace(builder.Configuration["CampaignJournal:Path"]))
            throw new InvalidOperationException("Local play cannot be combined with Postgres or retired CampaignJournal:Path configuration.");
        if (builder.Configuration.GetSection("Kestrel:Endpoints").GetChildren().Any())
            throw new InvalidOperationException("Local play cannot be combined with configured Kestrel endpoints.");
        var directory = builder.Configuration["SANDTABLE_LOCAL_PLAY_DATA"];
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            throw new InvalidOperationException("Local play save directory does not exist. Create a private directory explicitly and set SANDTABLE_LOCAL_PLAY_DATA.");
        directory = Path.GetFullPath(directory);
        if (!OperatingSystem.IsWindows() && (File.GetUnixFileMode(directory)
            & (UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute
               | UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute)) != 0)
            throw new InvalidOperationException("Local play save directory must be private (chmod 700).");
        var secret = builder.Configuration["CampaignJournal:OperatorSecret"];
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("Local play requires CampaignJournal:OperatorSecret; retain it separately for restart.");

        FileStream directoryLock;
        try
        {
            // Never unlink the lock file: another process must contend for the same inode.
            // OS releases the handle even on a hard kill.
            directoryLock = new FileStream(Path.Combine(directory, ".local-play.lock"), FileMode.OpenOrCreate,
                FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException exception)
        {
            throw new InvalidOperationException("Local play save directory is already in use or its lock cannot be opened.", exception);
        }
        try
        {
            var journals = Directory.GetFiles(directory, "*" + CampaignDurableAuthorityJournal.CampaignFileExtension);
            if (journals.Length != 0 && !File.Exists(CampaignDurableAuthorityJournal.HostIdentityPathForDirectory(directory)))
                throw new InvalidOperationException("Local play host identity is missing; restore the full save directory before restarting.");
            // Fail startup for unrecognized/truncated saves rather than silently skipping them in re-arm.
            foreach (var journal in journals)
                _ = CampaignDurableAuthorityJournal.OpenExisting(journal, contentResolver: Cna1979SyntheticContentResolver.Instance);
            builder.Services.AddEmbeddedCampaignJournal(directory, secret, TimeProvider.System, Cna1979SyntheticContentResolver.Instance);
            if (!CampaignDurableAuthorityJournal.AuthenticateOperatorAt(directory, secret))
                throw new InvalidOperationException("Local play operator secret does not match the saved host identity.");
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.WebHost.UseSetting(WebHostDefaults.PreferHostingUrlsKey, "false");
            builder.WebHost.ConfigureKestrel(options =>
            {
                // Do not let a subsequent appsettings reload add another listener to this pilot.
                options.Configure(new ConfigurationBuilder().Build(), reloadOnChange: false);
                options.Listen(IPAddress.Loopback, 0);
            });
            // Orleans needs a nonzero primary silo endpoint. The OS chooses one here; the small
            // release/bind race fails startup normally if a competing process takes the port.
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            return new LocalPlayPilot(directoryLock, ((IPEndPoint)listener.LocalEndpoint).Port);
        }
        catch
        {
            directoryLock.Dispose();
            throw;
        }
    }

    public void Dispose() => directoryLock.Dispose();
}
