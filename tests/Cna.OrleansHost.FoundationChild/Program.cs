using Microsoft.Data.Sqlite;

if (args.Length != 5) return 2;
var (path, campaign, authority, request, expectedHex) =
    (args[0], args[1], args[2], args[3], args[4]);
using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
{
    DataSource = path,
    Mode = SqliteOpenMode.ReadOnly,
}.ToString());
connection.Open();
using var command = connection.CreateCommand();
command.CommandText = """
    SELECT r.sequence,r.record_bytes FROM probe_requests q
    JOIN probe_records r ON r.campaign_id=q.campaign_id AND r.sequence=q.sequence
    WHERE q.campaign_id=$campaign AND q.authority_id=$authority AND q.request_id=$request
    """;
command.Parameters.AddWithValue("$campaign", campaign);
command.Parameters.AddWithValue("$authority", authority);
command.Parameters.AddWithValue("$request", request);
using var reader = command.ExecuteReader();
if (!reader.Read() || Convert.ToHexString((byte[])reader[1]) != expectedHex) return 3;
Console.WriteLine(reader.GetInt64(0));
return reader.Read() ? 4 : 0;
