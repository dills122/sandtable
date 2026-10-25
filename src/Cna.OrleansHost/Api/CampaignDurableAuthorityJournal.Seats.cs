using System.Security.Cryptography;
using System.Text;
using Cna.Core.Actions;
using Microsoft.Data.Sqlite;

namespace Cna.OrleansHost.Api;

internal sealed partial class CampaignDurableAuthorityJournal
{
    private static readonly TimeSpan GrantLifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan CredentialLifetime = TimeSpan.FromHours(2);

    internal bool AuthenticateOperator(string secret)
    {
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction();
        try
        {
            RequireOperator(connection, transaction, secret);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    internal CampaignSeatGrant IssueSeatGrant(string campaignId, CampaignActionAudience audience,
        string operatorSecret)
    {
        RequireWritable();
        var side = Side(audience);
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        RequireOperator(connection, transaction, operatorSecret);
        _ = Load(connection, transaction, campaignId);
        long epoch;
        using (var query = connection.CreateCommand())
        {
            query.Transaction = transaction;
            query.CommandText = "SELECT epoch FROM seats WHERE campaign_id=$id AND side=$side";
            query.Parameters.AddWithValue("$id", campaignId);
            query.Parameters.AddWithValue("$side", side);
            var prior = query.ExecuteScalar();
            epoch = prior is null ? 1 : checked((long)prior + 1);
        }
        var secret = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        var expires = checked(clock.GetUtcNow().ToUnixTimeMilliseconds()
            + (long)GrantLifetime.TotalMilliseconds);
        using (var upsert = connection.CreateCommand())
        {
            upsert.Transaction = transaction;
            upsert.CommandText = """
                INSERT INTO seats VALUES ($id,$side,$epoch,$grant,$expires,NULL,NULL)
                ON CONFLICT(campaign_id,side) DO UPDATE SET
                  epoch=excluded.epoch, grant_digest=excluded.grant_digest,
                  grant_expires_ms=excluded.grant_expires_ms,
                  credential_digest=NULL, credential_expires_ms=NULL
                """;
            upsert.Parameters.AddWithValue("$id", campaignId);
            upsert.Parameters.AddWithValue("$side", side);
            upsert.Parameters.AddWithValue("$epoch", epoch);
            upsert.Parameters.AddWithValue("$grant", CredentialDigest("seat-grant", secret));
            upsert.Parameters.AddWithValue("$expires", expires);
            if (upsert.ExecuteNonQuery() != 1)
                throw new InvalidOperationException("Seat grant did not commit.");
        }
        transaction.Commit();
        return new CampaignSeatGrant(campaignId, audience, epoch, secret, expires);
    }

    internal CampaignSeatCredential? RedeemSeatGrant(CampaignSeatGrant grant)
    {
        RequireWritable();
        ArgumentNullException.ThrowIfNull(grant);
        var digest = TryCredentialDigest("seat-grant", grant.Secret);
        if (digest is null) return null;
        var side = Side(grant.Audience);
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction(deferred: false);
        using (var query = connection.CreateCommand())
        {
            query.Transaction = transaction;
            query.CommandText = """
                SELECT epoch,grant_digest,grant_expires_ms
                FROM seats WHERE campaign_id=$id AND side=$side
                """;
            query.Parameters.AddWithValue("$id", grant.CampaignId);
            query.Parameters.AddWithValue("$side", side);
            using var reader = query.ExecuteReader();
            if (!reader.Read() || reader.GetInt64(0) != grant.Epoch
                || reader.IsDBNull(1) || reader.IsDBNull(2)
                || reader.GetInt64(2) <= clock.GetUtcNow().ToUnixTimeMilliseconds()
                || !SameDigest(reader.GetFieldValue<byte[]>(1), digest))
                return null;
        }
        var credentialSecret = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        var expires = checked(clock.GetUtcNow().ToUnixTimeMilliseconds()
            + (long)CredentialLifetime.TotalMilliseconds);
        using (var update = connection.CreateCommand())
        {
            update.Transaction = transaction;
            update.CommandText = """
                UPDATE seats SET grant_digest=NULL,grant_expires_ms=NULL,
                  credential_digest=$digest,credential_expires_ms=$expires
                WHERE campaign_id=$id AND side=$side AND epoch=$epoch
                """;
            update.Parameters.AddWithValue("$id", grant.CampaignId);
            update.Parameters.AddWithValue("$side", side);
            update.Parameters.AddWithValue("$epoch", grant.Epoch);
            update.Parameters.AddWithValue("$digest", CredentialDigest("seat-credential", credentialSecret));
            update.Parameters.AddWithValue("$expires", expires);
            if (update.ExecuteNonQuery() != 1)
                throw new InvalidOperationException("Seat credential did not commit.");
        }
        transaction.Commit();
        return new CampaignSeatCredential(grant.CampaignId, grant.Audience, grant.Epoch,
            credentialSecret, expires);
    }

    internal CampaignSeatAuthority? AuthenticateSeat(CampaignSeatCredential credential)
    {
        using var connection = Connect(create: false);
        using var transaction = connection.BeginTransaction();
        return AuthenticateSeat(connection, transaction, credential);
    }

    private CampaignSeatAuthority? AuthenticateSeat(SqliteConnection connection,
        SqliteTransaction transaction, CampaignSeatCredential credential)
    {
        ArgumentNullException.ThrowIfNull(credential);
        var digest = TryCredentialDigest("seat-credential", credential.Secret);
        if (digest is null) return null;
        var side = Side(credential.Audience);
        using var query = connection.CreateCommand();
        query.Transaction = transaction;
        query.CommandText = """
            SELECT epoch,credential_digest,credential_expires_ms
            FROM seats WHERE campaign_id=$id AND side=$side
            """;
        query.Parameters.AddWithValue("$id", credential.CampaignId);
        query.Parameters.AddWithValue("$side", side);
        using var reader = query.ExecuteReader();
        if (!reader.Read() || reader.GetInt64(0) != credential.Epoch
            || reader.IsDBNull(1) || reader.IsDBNull(2)
            || reader.GetInt64(2) <= clock.GetUtcNow().ToUnixTimeMilliseconds()
            || !SameDigest(reader.GetFieldValue<byte[]>(1), digest))
            return null;
        return new CampaignSeatAuthority(credential.CampaignId, credential.Audience,
            credential.Epoch);
    }

    private static void RequireOperator(SqliteConnection connection,
        SqliteTransaction transaction, string secret)
    {
        var digest = TryCredentialDigest("operator", secret);
        if (digest is null) throw new UnauthorizedAccessException("Operator capability rejected.");
        using var query = connection.CreateCommand();
        query.Transaction = transaction;
        query.CommandText = "SELECT digest FROM operator_capability WHERE singleton=1";
        if (query.ExecuteScalar() is not byte[] stored || !SameDigest(stored, digest))
            throw new UnauthorizedAccessException("Operator capability rejected.");
    }

    private static string Side(CampaignActionAudience audience) => audience switch
    {
        CampaignActionAudience.Axis => "axis",
        CampaignActionAudience.Commonwealth => "commonwealth",
        _ => throw new ArgumentOutOfRangeException(nameof(audience)),
    };

    private static byte[] CredentialDigest(string domain, string secret) =>
        TryCredentialDigest(domain, secret)
        ?? throw new ArgumentException("Capability must be 32 random bytes encoded as hex.", nameof(secret));

    private static byte[]? TryCredentialDigest(string domain, string secret)
    {
        if (secret is null || secret.Length != 64 || !secret.All(value =>
            value is >= '0' and <= '9' or >= 'a' and <= 'f'))
            return null;
        var bytes = Convert.FromHexString(secret);
        return SHA256.HashData(Encoding.UTF8.GetBytes(domain).Concat(bytes).ToArray());
    }

    private static bool SameDigest(byte[] stored, byte[] candidate) =>
        stored.Length == 32 && candidate.Length == 32
        && CryptographicOperations.FixedTimeEquals(stored, candidate);
}

internal sealed record CampaignSeatGrant(string CampaignId, CampaignActionAudience Audience,
    long Epoch, string Secret, long ExpiresUnixMs)
{
    public override string ToString() => $"CampaignSeatGrant({CampaignId},{Audience},{Epoch})";
}

internal sealed record CampaignSeatCredential(string CampaignId, CampaignActionAudience Audience,
    long Epoch, string Secret, long ExpiresUnixMs)
{
    public override string ToString() => $"CampaignSeatCredential({CampaignId},{Audience},{Epoch})";
}

internal sealed record CampaignSeatAuthority(string CampaignId, CampaignActionAudience Audience,
    long Epoch);
