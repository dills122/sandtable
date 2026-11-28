using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Campaigns;
using Microsoft.Data.Sqlite;

namespace Cna.LoadHarness;

// Standalone process-level pilot; synthetic catalog only, no container or database service.
internal static class LocalPlayProbe
{
    private const string Id = "local-play-probe";
    private const string Api = "/api/v2/campaigns/";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    internal static async Task RunAsync(string hostDll)
    {
        var directory = Directory.CreateTempSubdirectory("sandtable-local-probe-").FullName;
        var secret = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        var config = HarnessConfig.Parse(["--output", Path.Combine(directory, "unused"), "--until", "turn2-reserve", "--combat-gt1"]);
        var (creation, timing) = InProcessDriver.Creation(config, Id);
        timing = new CampaignDecisionTimingConfigurationV1(timing.ConfigId, timing.RulesetHash, timing.ContentHash,
            timing.Windows.Select(w => w.Kind == "selection" ? w with { DecisionBudgetMilliseconds = 45_000 } : w));
        using var timingJson = JsonDocument.Parse(timing.SerializeCanonical());
        var path = Path.Combine(directory, Id + ".sqlite");
        try
        {
            using var first = await Launch(hostDll, directory, secret);
            using var client = Client(first.Address);
            await Send(client, Api, new
            {
                contractVersion = 2,
                creation.CampaignId,
                creation.RulesetHash,
                creation.Seed,
                creation.SetupId,
                creation.SetupHash,
                creation.ContentPackId,
                creation.ContentHash,
                creation.ScenarioId,
                timing = timingJson.RootElement
            }, secret, HttpStatusCode.Created);
            var seats = new Dictionary<string, JsonElement>();
            var counts = new Dictionary<string, int> { ["axis"] = 0, ["commonwealth"] = 0 };
            foreach (var side in counts.Keys) seats[side] = await Seat(client, secret, side);
            long? deadline = null;
            for (var i = 0; i < 112; i++)
            {
                deadline = Deadline(path);
                if (counts.Values.Sum() >= 20 && counts.Values.All(n => n != 0) && deadline.HasValue) break;
                var openings = new List<(string Side, string Stage, string Element, JsonElement Cut)>();
                foreach (var (side, seat) in seats)
                {
                    var discover = await Send(client, Api + Id + "/openings/open", new { contractVersion = 2, credential = seat });
                    foreach (var entry in discover.GetProperty("open").EnumerateArray())
                    {
                        var stage = entry.GetProperty("openingStage").GetString()!;
                        if (entry.GetProperty("ruleNotImplemented").GetBoolean() || stage is "movement" or "second-movement" or "stage-entry-movement") continue;
                        foreach (var item in entry.GetProperty("actingElementIds").EnumerateArray())
                        {
                            var element = item.GetString()!;
                            var cut = await Send(client, Api + Id + "/openings", new { contractVersion = 2, credential = seat, stage, actingElementId = element });
                            openings.Add((side, entry.GetProperty("actionStage").GetString()!, element, cut));
                        }
                    }
                }
                Check(openings.Count > 0, "No owner opening before selection checkpoint.");
                var selected = openings.OrderBy(o => o.Element, StringComparer.Ordinal).Last();
                var refs = selected.Cut.GetProperty("publicActionRefs");
                var index = refs.GetArrayLength() - 1;
                if (selected.Stage is "reserve-designation" or "second-reserve" or "stage-entry-reserve")
                {
                    using var visible = JsonDocument.Parse(selected.Cut.GetProperty("visibleCutBase64").GetBytesFromBase64());
                    index = visible.RootElement.GetProperty("choices").EnumerateArray().Select((choice, offset) => (choice, offset))
                        .Single(x => x.choice.GetProperty("kind").GetString() == "complete").offset;
                }
                var accepted = await Send(client, Api + Id + "/actions", new
                {
                    contractVersion = 2,
                    credential = seats[selected.Side],
                    stage = selected.Stage,
                    actingElementId = selected.Element,
                    openingRevision = selected.Cut.GetProperty("revision"),
                    publicActionRef = refs[index]
                });
                Check(accepted.GetProperty("accepted").GetBoolean(), "Owner submission not accepted.");
                counts[selected.Side]++;
            }
            Check(deadline.HasValue && counts.Values.Sum() >= 20 && counts.Values.All(n => n > 0), "Selection checkpoint / owner step target not reached.");
            Check(TimeoutCount(path) == 0, "Selection timed out before restart.");
            var before = await Verify(client, secret);
            Console.WriteLine($"Accepted owner steps: axis={counts["axis"]}, commonwealth={counts["commonwealth"]}; collecting selection deadline retained.");
            await CheckGate(first.Address);
            await ExpectFailure(hostDll, directory, secret, "already in use");
            first.Kill();
            using var second = await Launch(hostDll, directory, secret);
            using var resumed = Client(second.Address);
            Check(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() < deadline, "Restart missed deadline; increase probe budget.");
            Check(JsonElement.DeepEquals(before, await Verify(resumed, secret)), "Verified fingerprint changed before deadline.");
            Check(Deadline(path) == deadline, "Deadline changed on restart.");
            foreach (var (side, seat) in seats)
            {
                await Send(resumed, Api + Id + "/openings/open", new { contractVersion = 2, credential = seat });
                var renewed = await Seat(resumed, secret, side);
                Check(renewed.GetProperty("epoch").GetInt64() == seat.GetProperty("epoch").GetInt64() + 1, "Reissue did not increment epoch.");
                await Send(resumed, Api + Id + "/openings/open", new { contractVersion = 2, credential = seat }, expected: HttpStatusCode.Unauthorized);
                await Send(resumed, Api + Id + "/openings/open", new { contractVersion = 2, credential = renewed });
            }
            Console.WriteLine("Hard restart: fingerprint and deadline unchanged; both credentials reused; operator reissue increments both epochs and revokes old credentials.");
            second.Kill();
            var gap = DateTimeOffset.FromUnixTimeMilliseconds(deadline!.Value) - DateTimeOffset.UtcNow + TimeSpan.FromSeconds(1);
            if (gap > TimeSpan.Zero) await Task.Delay(gap);
            using var third = await Launch(hostDll, directory, secret);
            // No HTTP or grain access before checking timeout: prove boot re-arm rather than lazy expiry.
            var limit = DateTimeOffset.UtcNow.AddSeconds(20);
            while (TimeoutCount(path) == 0 && DateTimeOffset.UtcNow < limit) await Task.Delay(100);
            Check(TimeoutCount(path) == 1, "Expected one selection timeout on boot.");
            using var expired = Client(third.Address);
            var after = await Verify(expired, secret);
            Check(after.GetProperty("eventCount").GetInt64() > before.GetProperty("eventCount").GetInt64(), "Expiry did not append verified state.");
            Console.WriteLine("Offline past deadline: exactly one selection timeout appended before any campaign request; replay verified.");
            third.Kill();
            await ExpectFailure(hostDll, directory, secret + "wrong", "does not match");
            var identity = Path.Combine(directory, "_host-identity.db");
            File.Move(identity, identity + ".backup");
            await ExpectFailure(hostDll, directory, secret, "host identity is missing");
            File.Move(identity + ".backup", identity);
            // Preserve the entire SQLite file family: a surviving WAL can contain a valid page 1.
            foreach (var file in Directory.GetFiles(directory, Id + ".sqlite*"))
                File.Move(file, file + ".backup");
            // A missing individual campaign is not a missing directory; the existing verification route returns 404.
            using (var missing = await Launch(hostDll, directory, secret))
            using (var missingClient = Client(missing.Address))
                await Send(missingClient, Api + Id + "/verification", null, secret, HttpStatusCode.NotFound);
            var corrupt = Path.Combine(directory, "corrupt.sqlite");
            File.WriteAllText(corrupt, "not a journal");
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(corrupt, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            await ExpectFailure(hostDll, directory, secret, "journal");
            await ExpectFailure(hostDll, Path.Combine(directory, "missing"), secret, "does not exist");
            await ExpectFailure(hostDll, directory, secret, "requires Development", production: true);
            await ExpectFailure(hostDll, directory, secret, "configured Kestrel endpoints", configuredEndpoint: true);
            Console.WriteLine("Duplicate process, wrong secret, missing host identity, corrupt journal, missing directory and Production and configured endpoints rejected; missing campaign verification returns 404; same-origin gate retained.");
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    private static async Task CheckGate(Uri address)
    {
        using var client = new HttpClient(new SocketsHttpHandler { AllowAutoRedirect = false, UseProxy = false }) { BaseAddress = address };
        using var absent = await client.GetAsync(Api + "opening-kinds");
        Check(absent.StatusCode == HttpStatusCode.Forbidden, "Missing same-origin evidence admitted.");
        client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "cross-site");
        using var crossSite = await client.GetAsync(Api + "opening-kinds");
        Check(crossSite.StatusCode == HttpStatusCode.Forbidden, "Cross-site request admitted.");
        client.DefaultRequestHeaders.Remove("Sec-Fetch-Site");
        client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
        client.DefaultRequestHeaders.Add("Origin", "http://127.0.0.1:1");
        using var mismatch = await client.GetAsync(Api + "opening-kinds");
        Check(mismatch.StatusCode == HttpStatusCode.Forbidden, "Mismatched origin admitted.");
    }

    private static HttpClient Client(Uri address)
    {
        var client = new HttpClient(new SocketsHttpHandler { AllowAutoRedirect = false, UseProxy = false }) { BaseAddress = address, Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
        return client;
    }

    private static async Task<JsonElement> Send(HttpClient client, string route, object? body, string? secret = null, HttpStatusCode expected = HttpStatusCode.OK)
    {
        using var request = new HttpRequestMessage(body is null ? HttpMethod.Get : HttpMethod.Post, route);
        if (body is not null) request.Content = JsonContent.Create(body, options: Json);
        if (secret is not null) request.Headers.Add("X-Sandtable-Operator", secret);
        using var response = await client.SendAsync(request);
        Check(response.StatusCode == expected, $"{route}: expected {(int)expected}, got {(int)response.StatusCode}.");
        if (expected is HttpStatusCode.NotFound or HttpStatusCode.Unauthorized) return default;
        using var doc = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync());
        return doc.RootElement.Clone();
    }

    private static async Task<JsonElement> Seat(HttpClient client, string secret, string side)
    {
        var grant = (await Send(client, Api + Id + "/grants/" + side, new { }, secret)).GetProperty("grant");
        return (await Send(client, Api + Id + "/seats", new { contractVersion = 2, grant })).GetProperty("credential");
    }

    private static async Task<JsonElement> Verify(HttpClient client, string secret)
    {
        var result = await Send(client, Api + Id + "/verification", null, secret);
        Check(result.GetProperty("isVerified").GetBoolean(), "Replay verification failed.");
        return result;
    }

    private static object? Scalar(string path, string sql)
    {
        using var db = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
        db.Open();
        using var command = db.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteScalar();
    }
    private static long? Deadline(string path) => Scalar(path, "SELECT deadline_ms FROM selection_rounds WHERE status='collecting'") as long?;
    private static long TimeoutCount(string path) => (long)Scalar(path, "SELECT count(*) FROM accepted_steps WHERE action_kind='native-combat-selection-timeout'")!;
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    private static Process Start(string dll, string directory, string secret, bool production = false, bool configuredEndpoint = false)
    {
        var fullPath = Path.GetFullPath(dll);
        var info = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(fullPath)!,
        };
        info.ArgumentList.Add(fullPath);
        info.Environment["SANDTABLE_LOCAL_PLAY"] = "1";
        info.Environment["SANDTABLE_LOCAL_PLAY_DATA"] = directory;
        info.Environment["CampaignJournal__OperatorSecret"] = secret;
        info.Environment["ASPNETCORE_ENVIRONMENT"] = production ? "Production" : "Development";
        info.Environment["DOTNET_ENVIRONMENT"] = production ? "Production" : "Development";
        info.Environment["Logging__LogLevel__Microsoft.Hosting.Lifetime"] = "Information";
        foreach (var key in new[] { "SANDTABLE_POSTGRES_HOT_CONNECTION_STRING", "SANDTABLE_POSTGRES_ORLEANS_CONNECTION_STRING", "CampaignJournal__Path", "OTEL_EXPORTER_OTLP_ENDPOINT" }) info.Environment.Remove(key);
        if (configuredEndpoint) info.Environment["Kestrel__Endpoints__Extra__Url"] = "http://127.0.0.1:0";
        return Process.Start(info)!;
    }

    private static async Task<Child> Launch(string dll, string directory, string secret)
    {
        var process = Start(dll, directory, secret);
        var child = new Child(process);
        try
        {
            var stderr = process.StandardError.ReadToEndAsync();
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            while (await process.StandardOutput.ReadLineAsync(deadline.Token) is { } line)
            {
                const string prefix = "Now listening on: ";
                var offset = line.IndexOf(prefix, StringComparison.Ordinal);
                if (offset < 0) continue;
                child.Address = new Uri(line[(offset + prefix.Length)..].Trim());
                Check(child.Address.Host == "127.0.0.1", "Host not bound to loopback.");
                _ = process.StandardOutput.ReadToEndAsync();
                return child;
            }
            throw new InvalidOperationException("Host failed to listen: " + await stderr);
        }
        catch { child.Dispose(); throw; }
    }

    private static async Task ExpectFailure(string dll, string directory, string secret, string expected, bool production = false, bool configuredEndpoint = false)
    {
        using var child = new Child(Start(dll, directory, secret, production, configuredEndpoint));
        var output = child.Process.StandardOutput.ReadToEndAsync();
        var error = child.Process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        try { await child.Process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException) { throw new InvalidOperationException("Host did not exit for expected startup failure: " + expected); }
        Check(child.Process.ExitCode != 0 && (await output + await error).Contains(expected, StringComparison.OrdinalIgnoreCase), "Expected startup failure: " + expected);
    }

    private sealed class Child(Process process) : IDisposable
    {
        internal Process Process => process;
        internal Uri Address { get; set; } = null!;
        internal void Kill() { if (!process.HasExited) process.Kill(entireProcessTree: true); process.WaitForExit(); }
        public void Dispose() { Kill(); process.Dispose(); }
    }
}
