using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Campaigns;

/// <summary>Admission outcome. Only Core mints it; the rejection carries no cause.</summary>
internal sealed class CampaignCombatCapabilityAdmissionResult
{
    private CampaignCombatCapabilityAdmissionResult(CampaignCombatCapabilityManifest? manifest) => Manifest = manifest;

    public bool IsAdmitted => Manifest is not null;
    /// <summary>The frozen closed manifest when admitted; null otherwise.</summary>
    public CampaignCombatCapabilityManifest? Manifest { get; }
    /// <summary>
    /// Outward reason. Every failure maps to the existing public creation reason
    /// <see cref="CampaignCreationRejectionReason.UnsupportedCapabilityProfile"/>, so an uncovered family,
    /// an unknown family or an unsupported configuration are indistinguishable to the caller.
    /// </summary>
    public CampaignCreationRejectionReason RejectionReason => IsAdmitted
        ? CampaignCreationRejectionReason.None
        : CampaignCreationRejectionReason.UnsupportedCapabilityProfile;

    internal static CampaignCombatCapabilityAdmissionResult Admitted(CampaignCombatCapabilityManifest manifest) =>
        new(manifest ?? throw new ArgumentNullException(nameof(manifest)));
    internal static readonly CampaignCombatCapabilityAdmissionResult Rejected = new(null);
}

// Task 021A2 (dormant, internal; no runtime caller). The activation gate the plan and fog spike require:
// a configuration is admitted only with a closed capability manifest; any uncovered or unknown family,
// or any configuration the manifest cannot be derived for, rejects before a Combat decision. Where it
// is called (campaign creation, Combat entry, or an explicit activation step) is T021-Q5 and is not
// decided here; restore revalidation (Q3) is 021A3. It derives the latest manifest version (v2 since
// 021A1b). See the scoping doc §9-§10 for which configurations that admits.
internal static class CampaignCombatCapabilityAdmission
{
    /// <summary>Shared bounded-profile invariant for the Barrage V14 and Anti Armor V20 handlers.</summary>
    internal const string NonInfantryExceedsBoundedProfile =
        "Unsupported: non-infantry combat component exceeds the bounded Combat profile.";

    public static CampaignCombatCapabilityAdmissionResult Admit(string rulesetHash,
        CampaignSetupSnapshotV6? setup, ContentPackV6Artifact? artifact)
    {
        if (setup is null || artifact is null) return CampaignCombatCapabilityAdmissionResult.Rejected;
        CampaignCombatCapabilityManifest manifest;
        try
        {
            manifest = CampaignCombatCapabilityManifest.Derive(rulesetHash, setup, artifact,
                CampaignCombatCapabilityManifest.LatestContractVersion);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return CampaignCombatCapabilityAdmissionResult.Rejected;
        }
        return Admit(manifest);
    }

    /// <summary>
    /// Admits a manifest only if it is closed and survives the strict register reader, so a manifest
    /// built outside derivation (unknown family, unregistered disposition) cannot pass.
    /// </summary>
    internal static CampaignCombatCapabilityAdmissionResult Admit(CampaignCombatCapabilityManifest? manifest)
    {
        if (manifest is null) return CampaignCombatCapabilityAdmissionResult.Rejected;
        try
        {
            var reread = CampaignCombatCapabilityManifestCodec.Read(manifest.SerializeCanonical());
            return reread.IsClosed
                ? CampaignCombatCapabilityAdmissionResult.Admitted(reread)
                : CampaignCombatCapabilityAdmissionResult.Rejected;
        }
        catch (JsonException)
        {
            return CampaignCombatCapabilityAdmissionResult.Rejected;
        }
    }
}
