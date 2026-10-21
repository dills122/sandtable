using Cna.Core.Exercises;
using Cna.Core.Hosting;

namespace Cna.OrleansHost.Api;

internal sealed partial class CampaignDurableAuthorityJournal
{
    private const int MaxContactRecords = 65_536;
    private const int MaxContactRecordBytes = 4_096;

    private static byte[] EncodeContactRecords(IReadOnlyList<byte[]> records)
    {
        ArgumentNullException.ThrowIfNull(records);
        if (records.Count > MaxContactRecords)
            throw new InvalidOperationException("Contact record count is invalid.");
        var encodedLength = (long)sizeof(int);
        foreach (var record in records)
        {
            if (record is not { Length: > 0 and <= MaxContactRecordBytes })
                throw new InvalidOperationException("Contact record length is invalid.");
            encodedLength += sizeof(int) + record.Length;
            if (encodedLength > MaxEvidenceBytes)
                throw new InvalidOperationException("Contact evidence length is invalid.");
        }
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(records.Count);
            foreach (var record in records)
            {
                writer.Write(record.Length);
                writer.Write(record);
            }
        }
        var encoded = stream.ToArray();
        RequireBounded(encoded);
        return encoded;
    }

    private static byte[][] DecodeContactRecords(byte[] encoded)
    {
        RequireBounded(encoded);
        using var stream = new MemoryStream(encoded, writable: false);
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        if (stream.Length < sizeof(int))
            throw new InvalidOperationException("Contact record framing is incomplete.");
        var count = reader.ReadInt32();
        if (count is < 0 or > MaxContactRecords)
            throw new InvalidOperationException("Contact record count is invalid.");
        var records = new byte[count][];
        for (var index = 0; index < count; index++)
        {
            if (stream.Length - stream.Position < sizeof(int))
                throw new InvalidOperationException("Contact record framing is incomplete.");
            var length = reader.ReadInt32();
            if (length is < 1 or > MaxContactRecordBytes
                || stream.Length - stream.Position < length)
                throw new InvalidOperationException("Contact record length is invalid.");
            records[index] = reader.ReadBytes(length);
        }
        if (stream.Position != stream.Length)
            throw new InvalidOperationException("Contact record framing has trailing bytes.");
        return records;
    }

    private static byte[][] AppendedContactRecords(ExerciseSession before,
        ExerciseSession after)
    {
        var previous = CampaignHostAdapter.QueryContactRecords(before);
        var current = CampaignHostAdapter.QueryContactRecords(after);
        if (current.Count < previous.Count
            || !current.Take(previous.Count).Zip(previous).All(pair =>
                pair.First.AsSpan().SequenceEqual(pair.Second)))
            throw new InvalidOperationException("Contact journal lost prior records.");
        return current.Skip(previous.Count).ToArray();
    }
}
