using System.Text.Json.Serialization;

namespace Cna.Core.Exercises;

/// <summary>Trusted Axis-only setup draft. Private action IDs are for the Host, never a player DTO.</summary>
public sealed class CampaignPregameAxisDraft
{
    private readonly byte[] visibleBytes;
    private readonly string[] privateActionIds;

    internal CampaignPregameAxisDraft(long authorityVersion, byte[] visibleBytes, IEnumerable<string> privateActionIds)
    {
        AuthorityVersion = authorityVersion;
        this.visibleBytes = visibleBytes.ToArray();
        this.privateActionIds = privateActionIds.ToArray();
    }
    [JsonIgnore] public long AuthorityVersion { get; }
    [JsonIgnore] public byte[] VisibleBytes => visibleBytes.ToArray();
    [JsonIgnore] public IReadOnlyList<string> PrivateActionIds => Array.AsReadOnly(privateActionIds.ToArray());
}
