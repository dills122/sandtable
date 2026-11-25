using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

/// <summary>
/// EVT-001 tripwire for issue #583. Each enum or bool whose <c>ToString()</c> / <c>Enum.Parse</c>
/// text reaches canonical event bytes or an action-id preimage is pinned here by exact string, so a
/// rename fails loudly instead of silently invalidating stored events or changing action IDs.
/// These values are NOT a style choice: do not "fix" them to match another casing convention.
/// </summary>
public sealed class CanonicalByteEnumNamePinTests
{
    // CampaignSourceAuthority.SetupFinalizationActionId: "{audience}" and "{finish}" are interpolated.
    [Fact]
    public void ActionAudienceAndBoolTextFeedSetupFinalizationPreimage()
    {
        Assert.Equal("Axis", CampaignActionAudience.Axis.ToString());
        Assert.Equal("Commonwealth", CampaignActionAudience.Commonwealth.ToString());
        Assert.Equal("System", CampaignActionAudience.System.ToString());
        Assert.Equal("True", true.ToString());
        Assert.Equal("False", false.ToString());
    }

    [Theory]
    [InlineData(CampaignActionAudience.Axis, true, "sha256:cb7330eaed4aee073fec0f4d6aa91597638d1341c027550f3b7bc4cd7b22b4d0")]
    [InlineData(CampaignActionAudience.Axis, false, "sha256:3d55cb7eb4f5e3854614d6d8ecf39a240b8ed301bc3b0d07719647983bf0639e")]
    [InlineData(CampaignActionAudience.Commonwealth, true, "sha256:30ea414a16542776c5a154c662ee1f8bb1a823fb978bbc69102ae56525b0bd40")]
    [InlineData(CampaignActionAudience.Commonwealth, false, "sha256:4b011d8f6299c5a6c835a3aa089b38fd712b7e3f34ae641f4423ffd8b2dd1337")]
    public void SetupFinalizationActionIdsAreByteStable(
        CampaignActionAudience audience, bool finish, string expected) =>
        Assert.Equal(expected, CampaignSourceAuthorityState.SetupFinalizationActionId(audience, finish));

    // Reserve Release V27 (event "choice"/"actingSide", owner-cut action-id preimage and option list).
    [Fact]
    public void ReserveReleaseChoiceNamesAreFrozen()
    {
        Assert.Equal(["ReleaseI", "ConvertToII", "ReleaseII", "RetainII"],
            Enum.GetNames<CampaignReserveReleaseChoice>());
        Assert.Equal(CampaignReserveReleaseChoice.ConvertToII,
            Enum.Parse<CampaignReserveReleaseChoice>("ConvertToII"));
        Assert.Equal(["None", "ReserveI", "ReserveII"], Enum.GetNames<CampaignElementReserveStatus>());
    }

    // Cycle Control V28 ("choice" in event bytes and owner-cut action-id preimage).
    [Fact]
    public void CycleControlChoiceNamesAreFrozen()
    {
        Assert.Equal(["Repeat", "Finish"], Enum.GetNames<CampaignNativeCycleControlChoiceV28>());
        Assert.Equal(CampaignNativeCycleControlChoiceV28.Finish,
            Enum.Parse<CampaignNativeCycleControlChoiceV28>("Finish"));
    }

    // Side encodings: PascalCase Enum.ToString() (V27/V28 actingSide, V37 defenderSide, owner-cut
    // preimages with lowercaseSide:false) versus lowercase FormatSide (V50, owner-cut visible bytes).
    [Fact]
    public void SideEncodingsDifferByCodecAndBothAreFrozen()
    {
        Assert.Equal(["Axis", "Commonwealth"], Enum.GetNames<LandSide>());
        Assert.Equal("Axis", LandSide.Axis.ToString());
        Assert.Equal("Commonwealth", LandSide.Commonwealth.ToString());
        Assert.Equal("axis", CampaignSnapshotSerializer.FormatSide(LandSide.Axis));
        Assert.Equal("commonwealth", CampaignSnapshotSerializer.FormatSide(LandSide.Commonwealth));
        // V37 Enum.Parse<LandSide>(..., ignoreCase: false) must reject the lowercase codec form.
        Assert.Throws<ArgumentException>(() => Enum.Parse<LandSide>("axis", false));
    }

    // Weather Determination V50 writes Season/Kind/Scope/AffectedAreas via Enum.ToString().
    [Fact]
    public void WeatherEnumNamesFeedingV50AreFrozen()
    {
        Assert.Equal(["Fall", "Winter", "Spring", "Summer"], Enum.GetNames<WeatherSeason>());
        Assert.Equal(["Normal", "Hot", "Sandstorm", "Rainstorm"], Enum.GetNames<WeatherKind>());
        Assert.Equal(["None", "Global", "ListedAreas"], Enum.GetNames<WeatherScope>());
        Assert.Equal(["A", "B", "C", "D", "E"], Enum.GetNames<WeatherArea>());
    }
}
