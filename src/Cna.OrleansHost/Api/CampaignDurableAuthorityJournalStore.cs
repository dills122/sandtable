using System.Collections.Concurrent;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Exercises;
using Cna.Core.Hosting;

namespace Cna.OrleansHost.Api;

/// <summary>
/// Production-side routing layer over <see cref="CampaignDurableAuthorityJournal"/> for the
/// design doc's &#167;3 file-per-campaign hot-tier topology
/// (<c>docs/work/plans/2026-11-18-data-layer-design-1000-campaign-ceiling.md</c>).
/// </summary>
/// <remarks>
/// <para><b>Why this exists.</b> Before this change, one <see cref="CampaignDurableAuthorityJournal"/>
/// instance -- opened once at host startup against one fixed path -- served every campaign on the
/// host for its entire lifetime, because every campaign shared the one file that instance wrapped.
/// <see cref="CampaignDurableAuthorityJournal"/> itself is deliberately left as a per-file handle
/// (own <c>path</c>, own <c>schemaVersion</c>, own connections) rather than being taught to juggle
/// many files internally -- see its own remarks and <see cref="JournalFor"/> below. Something still
/// has to decide, for an arbitrary incoming campaign id, which file (lazily created on first use)
/// that campaign's journal instance is, and cache it for the life of the host; that is this class's
/// entire job. It is <see cref="CampaignDurableOutwardApi"/>'s injected dependency now, in place of
/// a bare <see cref="CampaignDurableAuthorityJournal"/>, and otherwise forwards every entry point
/// <see cref="CampaignDurableAuthorityJournal"/> exposes to that API unchanged -- see the generated
/// forwarding block below.</para>
/// <para><b>Why <see cref="CampaignDurableAuthorityJournal"/> itself did not need a directory-aware
/// rewrite.</b> Nearly every one of its methods already took the campaign's identity as a plain
/// parameter (directly, or via a credential/grant/request that carries a <c>CampaignId</c>) purely
/// because the SQL inside needed it -- not because the instance was multi-campaign-aware. That made
/// the actual topology change small and contained: <see cref="CampaignDurableAuthorityJournal.CreateNew"/>
/// and <c>OpenExisting</c> keep their existing single-file signatures untouched (this class is what
/// computes the per-campaign path and passes it in), its crash-safety and migration machinery runs
/// completely unmodified per file, and the one piece of genuinely cross-campaign behavior left
/// (the seven <c>CollectingXCampaigns</c> sweep-enumeration entry points) is handled by
/// <see cref="CampaignDurableAuthorityJournal.CollectingAcrossSiblingFiles"/> -- any single
/// instance can answer that correctly by looking at its own directory, so <see
/// cref="CampaignCombatTimeoutService"/> needs no changes at all and keeps depending directly on a
/// <see cref="CampaignDurableAuthorityJournal"/> (see the hand-written <c>CollectingXCampaigns</c>
/// wrappers below for how this class answers the same question with an empty cache).</para>
/// <para><b>Host identity.</b> <c>operator_capability</c>/<c>reference_key</c> moved out of the
/// per-campaign schema into one small shared file per directory (see
/// <see cref="CampaignDurableAuthorityJournal.EnsureHostIdentity"/>'s remarks for why they are
/// host-wide identity, not per-campaign data) -- this constructor ensures it exists eagerly, before
/// the host ever serves a request, mirroring how today's single shared file was always fully
/// initialized before <c>Program.cs</c> finished building the host.</para>
/// <para><b>Deliberately out of scope here</b> (see the PR description for the full list): the
/// hot/cold archival split (&#167;4), replacing the sweep with per-grain reminders (&#167;7), and
/// snapshotting (&#167;5, separately documented as blocked). This class only resolves "which file"
/// for the topology that already exists today, campaign-by-campaign.</para>
/// </remarks>
internal sealed class CampaignDurableAuthorityJournalStore
{
    private readonly string? directory;
    private readonly string? operatorSecret;
    private readonly TimeProvider clock;
    private readonly IContentPackV6Resolver? contentResolver;
    private readonly CampaignDurableAuthorityJournal? fixedJournal;
    private readonly ConcurrentDictionary<string, Lazy<CampaignDurableAuthorityJournal>> journals = new(StringComparer.Ordinal);

    internal CampaignDurableAuthorityJournalStore(string directory, string operatorSecret,
        TimeProvider? clock = null, IContentPackV6Resolver? contentResolver = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentException.ThrowIfNullOrWhiteSpace(operatorSecret);
        this.directory = Path.GetFullPath(directory);
        this.operatorSecret = operatorSecret;
        this.clock = clock ?? TimeProvider.System;
        this.contentResolver = contentResolver;
        if (!Directory.Exists(this.directory))
        {
            Directory.CreateDirectory(this.directory);
            // Match CreateNew's own directory-creation path (CampaignDurableAuthorityJournal.cs):
            // a directory created with the process's default umask can leave group/other read
            // access, which RequirePrivateDirectory (run by EnsureHostIdentity below, and by every
            // per-campaign CreateNew/OpenExisting this store performs) refuses.
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(this.directory, UnixFileMode.UserRead | UnixFileMode.UserWrite
                    | UnixFileMode.UserExecute);
        }
        CampaignDurableAuthorityJournal.EnsureHostIdentity(
            CampaignDurableAuthorityJournal.HostIdentityPathForDirectory(this.directory), operatorSecret);
    }

    private CampaignDurableAuthorityJournalStore(CampaignDurableAuthorityJournal fixedJournal)
    {
        this.fixedJournal = fixedJournal;
        clock = TimeProvider.System;
    }

    /// <summary>
    /// Test-only convenience: every campaign id routes to the one already-constructed <paramref
    /// name="journal"/>, matching how this project's HTTP-layer tests built their setup before this
    /// change (one shared <see cref="CampaignDurableAuthorityJournal"/> registered directly in DI) --
    /// see the PR description for exactly which test files use this and why no other test change was
    /// needed to keep them exercising real per-campaign-file production code otherwise.
    /// </summary>
    internal static CampaignDurableAuthorityJournalStore ForSingleCampaign(
        CampaignDurableAuthorityJournal journal) => new(journal);

    /// <summary>
    /// Resolves (lazily creating or opening, and caching for the life of this store) the journal
    /// instance for <paramref name="campaignId"/>. <paramref name="createIfMissing"/> distinguishes
    /// "this call is itself creating the campaign" (its file may not exist yet) from every other
    /// call (the campaign must already have a file, or this throws exactly like today's single-file
    /// <see cref="CampaignDurableAuthorityJournal.OpenExisting"/> does for an unknown campaign).
    /// </summary>
    internal CampaignDurableAuthorityJournal JournalFor(string campaignId, bool createIfMissing)
    {
        if (fixedJournal is not null) return fixedJournal;
        if (directory is null) throw new InvalidOperationException("Store has no backing directory.");
        if (journals.TryGetValue(campaignId, out var cached)) return cached.Value;
        var path = JournalPath(directory, campaignId);
        if (!createIfMissing && !File.Exists(path))
            throw new InvalidOperationException($"Campaign '{campaignId}' has no durable journal.");
        var lazy = journals.GetOrAdd(campaignId, _ => new Lazy<CampaignDurableAuthorityJournal>(
            () => File.Exists(path)
                ? CampaignDurableAuthorityJournal.OpenExisting(path, clock, contentResolver)
                : CampaignDurableAuthorityJournal.CreateNew(path, operatorSecret!, clock, contentResolver),
            LazyThreadSafetyMode.ExecutionAndPublication));
        return lazy.Value;
    }

    private static string JournalPath(string directory, string campaignId) =>
        Path.Combine(directory, RequireSafeCampaignId(campaignId) + CampaignDurableAuthorityJournal.CampaignFileExtension);

    /// <summary>
    /// Defense-in-depth against path traversal/injection before <paramref name="campaignId"/> ever
    /// becomes part of a file path. <see cref="CampaignCreationRequest.CampaignId"/> is already
    /// validated upstream to the same character class this enforces (<c>Cna.Core</c>'s
    /// <c>ContentContractGuards.RequireStableId</c>, not reusable here across the assembly
    /// boundary -- it is <see langword="internal"/> to <c>Cna.Core</c>), but a campaign id reaching
    /// this store via an outward-API route parameter (e.g. <c>/{campaignId}/source-axis-supplement</c>)
    /// is bound straight from the URL and never passes through that check, so this store cannot
    /// assume it already holds a safe value.
    /// </summary>
    private static string RequireSafeCampaignId(string campaignId)
    {
        static bool IsAsciiLetterOrDigit(char c) => c is >= 'a' and <= 'z' or >= '0' and <= '9';
        ArgumentException.ThrowIfNullOrWhiteSpace(campaignId, nameof(campaignId));
        if (campaignId.Length > 200 || !IsAsciiLetterOrDigit(campaignId[0]) || !IsAsciiLetterOrDigit(campaignId[^1]))
            throw new ArgumentException(
                "Campaign id must be 1-200 characters, beginning and ending with a lowercase ASCII letter or digit.",
                nameof(campaignId));
        var previousWasSeparator = false;
        foreach (var character in campaignId)
        {
            if (IsAsciiLetterOrDigit(character)) { previousWasSeparator = false; continue; }
            if (character is '-' or '_' && !previousWasSeparator) { previousWasSeparator = true; continue; }
            throw new ArgumentException(
                "Campaign id must use lowercase ASCII letters, digits, and nonadjacent hyphen or underscore separators.",
                nameof(campaignId));
        }
        return campaignId;
    }

    /// <summary>
    /// Host-level operator authentication, usable even when this store has never yet resolved a
    /// single campaign -- see <see cref="CampaignDurableAuthorityJournal.AuthenticateOperatorAt"/>'s
    /// remarks (operator capability is host-identity state keyed by directory, not by campaign).
    /// </summary>
    internal bool AuthenticateOperator(string secret) => fixedJournal is not null
        ? fixedJournal.AuthenticateOperator(secret)
        : CampaignDurableAuthorityJournal.AuthenticateOperatorAt(
            directory ?? throw new InvalidOperationException("Store has no backing directory."), secret);

    /// <summary>
    /// Any already-resolved journal instance can answer a <c>CollectingXCampaigns</c> query
    /// correctly on its own (<see cref="CampaignDurableAuthorityJournal.CollectingAcrossSiblingFiles"/>
    /// looks at its own directory, not just its own file) -- so this store only needs to hand back
    /// *some* instance, preferring one it has already opened over opening a fresh one, and returning
    /// null only when the directory holds no campaign at all yet.
    /// </summary>
    private CampaignDurableAuthorityJournal? AnyJournal()
    {
        if (fixedJournal is not null) return fixedJournal;
        foreach (var lazy in journals.Values)
            if (lazy.IsValueCreated) return lazy.Value;
        if (directory is null) return null;
        string[] siblings;
        try { siblings = Directory.GetFiles(directory, "*" + CampaignDurableAuthorityJournal.CampaignFileExtension); }
        catch (DirectoryNotFoundException) { return null; }
        if (siblings.Length == 0) return null;
        try { return CampaignDurableAuthorityJournal.OpenExisting(siblings[0], clock, contentResolver); }
        catch (InvalidOperationException) { return null; }
    }

    internal IReadOnlyList<string> CollectingSelectionCampaigns(string after, int limit) =>
        AnyJournal()?.CollectingSelectionCampaigns(after, limit) ?? [];

    internal IReadOnlyList<string> CollectingRbaCampaigns(string after, int limit) =>
        AnyJournal()?.CollectingRbaCampaigns(after, limit) ?? [];

    internal IReadOnlyList<string> CollectingForceCampaigns(string after, int limit) =>
        AnyJournal()?.CollectingForceCampaigns(after, limit) ?? [];

    internal IReadOnlyList<string> CollectingRetreatCampaigns(string after, int limit) =>
        AnyJournal()?.CollectingRetreatCampaigns(after, limit) ?? [];

    internal IReadOnlyList<string> CollectingCustodyCampaigns(string after, int limit) =>
        AnyJournal()?.CollectingCustodyCampaigns(after, limit) ?? [];

    internal IReadOnlyList<string> CollectingReserveCampaigns(string after, int limit) =>
        AnyJournal()?.CollectingReserveCampaigns(after, limit) ?? [];

    internal IReadOnlyList<string> CollectingCycleCampaigns(string after, int limit) =>
        AnyJournal()?.CollectingCycleCampaigns(after, limit) ?? [];

    // <auto-generated-by-hand>
    // Everything below is a one-to-one forwarding surface onto JournalFor(campaignId), mirroring
    // every internal entry point CampaignDurableAuthorityJournal exposes to CampaignDurableOutwardApi.
    // Generated mechanically from each method's own signature (never hand-typed, to avoid a
    // transcription mistake across ~150 near-identical forwarders) -- see the PR description for
    // the generator. Keep it that way: if CampaignDurableAuthorityJournal's outward-facing surface
    // changes, regenerate this block rather than hand-editing it.
    internal bool TickCustodyRound(string campaignId) =>
        JournalFor(campaignId, createIfMissing: false).TickCustodyRound(campaignId);

    internal bool TickCycleRound(string campaignId) =>
        JournalFor(campaignId, createIfMissing: false).TickCycleRound(campaignId);

    internal bool TickForceRound(string campaignId) =>
        JournalFor(campaignId, createIfMissing: false).TickForceRound(campaignId);

    internal CampaignOwnerOpening QueryOwnerOpening(CampaignSeatCredential credential, string actingElementId) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QueryOwnerOpening(credential, actingElementId);

    internal CampaignOwnerOpening QueryPregameAxisSupplyOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QueryPregameAxisSupplyOpening(credential);

    internal CampaignOwnerOpening QueryPregameAxisSupplyFinishOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QueryPregameAxisSupplyFinishOpening(credential);

    internal CampaignOwnerOpening QuerySourceAxisDumpOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceAxisDumpOpening(credential);

    internal CampaignOwnerOpening QuerySourceCommonwealthSupplyOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceCommonwealthSupplyOpening(credential);

    internal CampaignOwnerOpening QuerySourceDummyOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceDummyOpening(credential);

    internal CampaignOwnerOpening QuerySourceMotorizationOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceMotorizationOpening(credential);

    internal CampaignOwnerOpening QuerySourceUnattachedCargoOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceUnattachedCargoOpening(credential);

    internal CampaignOwnerOpening QuerySourceActionsOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceActionsOpening(credential);

    internal CampaignOwnerOpening QuerySourceAxisGt1SupplyOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceAxisGt1SupplyOpening(credential);

    internal CampaignOwnerOpening QuerySourceCommonwealthSupplyPlanOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceCommonwealthSupplyPlanOpening(credential);

    internal CampaignOwnerOpening QuerySourceAutoblindaOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceAutoblindaOpening(credential);

    internal CampaignOwnerOpening QuerySourcePriorityMotorizationOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourcePriorityMotorizationOpening(credential);

    internal CampaignOwnerOpening QuerySourceAttachedInitialLoadsOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceAttachedInitialLoadsOpening(credential);

    internal CampaignOwnerOpening QuerySourceInitialWrecksOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceInitialWrecksOpening(credential);

    internal CampaignOwnerOpening QuerySourceAxisFreeSetupOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceAxisFreeSetupOpening(credential);

    internal CampaignOwnerOpening QuerySourceAxisCorpsSetupOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceAxisCorpsSetupOpening(credential);

    internal CampaignOwnerOpening QuerySourceMalettiHierarchyOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceMalettiHierarchyOpening(credential);

    internal CampaignOwnerOpening QuerySourceGt2CorrectionOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceGt2CorrectionOpening(credential);

    internal CampaignOwnerOpening QuerySourceCwPriorityCatalogOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceCwPriorityCatalogOpening(credential);

    internal CampaignOwnerOpening QuerySourceMatruhFormationOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceMatruhFormationOpening(credential);

    internal CampaignOwnerOpening QuerySourceCwFreeGunsOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceCwFreeGunsOpening(credential);

    internal CampaignOwnerOpening QuerySourceAxisPriorityOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceAxisPriorityOpening(credential);

    internal CampaignOwnerOpening QuerySourceCairoSetupOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceCairoSetupOpening(credential);

    internal CampaignOwnerOpening QueryCombatOwnerOpening(CampaignSeatCredential credential, string actingElementId) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QueryCombatOwnerOpening(credential, actingElementId);

    internal CampaignOwnerOpening QueryRbaOwnerOpening(CampaignSeatCredential credential, string defenderElementId) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QueryRbaOwnerOpening(credential, defenderElementId);

    internal CampaignOwnerOpening QueryForceOwnerOpening(CampaignSeatCredential credential, string elementId) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QueryForceOwnerOpening(credential, elementId);

    internal CampaignOwnerOpening QueryPostAssaultRetreatOwnerOpening(CampaignSeatCredential credential, string defenderElementId) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QueryPostAssaultRetreatOwnerOpening(credential, defenderElementId);

    internal CampaignOwnerOpening QueryCustodyOwnerOpening(CampaignSeatCredential credential, string captorElementId) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QueryCustodyOwnerOpening(credential, captorElementId);

    internal CampaignOwnerOpening QueryReserveReleaseOwnerOpening(CampaignSeatCredential credential, string reserveElementId) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QueryReserveReleaseOwnerOpening(credential, reserveElementId);

    internal CampaignOwnerOpening QueryCycleControlOwnerOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QueryCycleControlOwnerOpening(credential);

    internal CampaignOwnerOpening QueryTruckConvoyPassOwnerOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QueryTruckConvoyPassOwnerOpening(credential);

    internal CampaignOwnerOpening QueryPostCombatPassOwnerOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QueryPostCombatPassOwnerOpening(credential);

    internal CampaignOwnerOpening QuerySecondReserveOwnerOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySecondReserveOwnerOpening(credential);

    internal CampaignOwnerOpening QuerySecondMovementOwnerOpening(CampaignSeatCredential credential, string elementId) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySecondMovementOwnerOpening(credential, elementId);

    internal CampaignOwnerOpening QuerySecondMovementCompletionOwnerOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySecondMovementCompletionOwnerOpening(credential);

    internal CampaignOwnerOpening QueryRepeatMovementOwnerOpening(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QueryRepeatMovementOwnerOpening(credential);

    internal CampaignOwnerOpening QueryRepeatCombatOwnerOpening(CampaignSeatCredential credential, string actingElementId) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QueryRepeatCombatOwnerOpening(credential, actingElementId);

    internal bool TickRbaRound(string campaignId) =>
        JournalFor(campaignId, createIfMissing: false).TickRbaRound(campaignId);

    internal bool TickReserveRound(string campaignId) =>
        JournalFor(campaignId, createIfMissing: false).TickReserveRound(campaignId);

    internal bool TickRetreatRound(string campaignId) =>
        JournalFor(campaignId, createIfMissing: false).TickRetreatRound(campaignId);

    internal void CreateScheduledWorld6(CampaignCreationRequest request) =>
        JournalFor(request.CampaignId, createIfMissing: true).CreateScheduledWorld6(request);

    internal IReadOnlyList<ScheduledWorld6Offer> QueryScheduledWorld6(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QueryScheduledWorld6(credential);

    internal byte[]? SubmitScheduledWorld6(CampaignSeatCredential credential, string publicReference) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitScheduledWorld6(credential, publicReference);

    internal CampaignSeatGrant IssueSeatGrant(string campaignId, CampaignActionAudience audience, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).IssueSeatGrant(campaignId, audience, operatorSecret);

    internal CampaignSeatCredential? RedeemSeatGrant(CampaignSeatGrant grant) =>
        JournalFor(grant.CampaignId, createIfMissing: false).RedeemSeatGrant(grant);

    internal CampaignSeatAuthority? AuthenticateSeat(CampaignSeatCredential credential) =>
        JournalFor(credential.CampaignId, createIfMissing: false).AuthenticateSeat(credential);

    internal byte[] QuerySourceCounterContacts(CampaignSeatCredential credential, string counterId) =>
        JournalFor(credential.CampaignId, createIfMissing: false).QuerySourceCounterContacts(credential, counterId);

    internal bool TickSelectionRound(string campaignId) =>
        JournalFor(campaignId, createIfMissing: false).TickSelectionRound(campaignId);

    internal CampaignOwnerSubmissionResult SubmitOwnerMovement(CampaignSeatCredential credential, string actingElementId, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitOwnerMovement(credential, actingElementId, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitPregameAxisSupply(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitPregameAxisSupply(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitPregameAxisSupplyFinish(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitPregameAxisSupplyFinish(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitSourceAxisDump(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitSourceAxisDump(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitSourceCommonwealthSupply(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitSourceCommonwealthSupply(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitSourceDummy(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitSourceDummy(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitSourceMotorization(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitSourceMotorization(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitSourceUnattachedCargo(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitSourceUnattachedCargo(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitSourceActions(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitSourceActions(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitSourceAxisGt1Supply(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitSourceAxisGt1Supply(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitSourceCommonwealthSupplyPlan(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitSourceCommonwealthSupplyPlan(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitSourceAutoblinda(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitSourceAutoblinda(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitSourcePriorityMotorization(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitSourcePriorityMotorization(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitSourceAttachedInitialLoads(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitSourceAttachedInitialLoads(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult PlaceSourceInitialWreck(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).PlaceSourceInitialWreck(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult PlaceSourceAxisFreeUnit(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).PlaceSourceAxisFreeUnit(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult PlaceSourceAxisCorpsUnit(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).PlaceSourceAxisCorpsUnit(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitSourceMalettiHierarchy(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitSourceMalettiHierarchy(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitSourceGt2Correction(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitSourceGt2Correction(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitSourceCwPriorityCatalog(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitSourceCwPriorityCatalog(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitSourceMatruhFormation(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitSourceMatruhFormation(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult PlaceSourceCwFreeGun(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).PlaceSourceCwFreeGun(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitSourceAxisPriority(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitSourceAxisPriority(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitSourceCairoUnit(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitSourceCairoUnit(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitOwnerCombatSelection(CampaignSeatCredential credential, string actingElementId, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitOwnerCombatSelection(credential, actingElementId, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitOwnerRbaDecline(CampaignSeatCredential credential, string defenderElementId, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitOwnerRbaDecline(credential, defenderElementId, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitOwnerRbaMove(CampaignSeatCredential credential, string defenderElementId, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitOwnerRbaMove(credential, defenderElementId, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitOwnerForceSeal(CampaignSeatCredential credential, string elementId, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitOwnerForceSeal(credential, elementId, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitOwnerPostAssaultRetreat(CampaignSeatCredential credential, string defenderElementId, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitOwnerPostAssaultRetreat(credential, defenderElementId, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitOwnerCustody(CampaignSeatCredential credential, string captorElementId, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitOwnerCustody(credential, captorElementId, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitOwnerReserveRelease(CampaignSeatCredential credential, string reserveElementId, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitOwnerReserveRelease(credential, reserveElementId, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitOwnerCycleControl(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitOwnerCycleControl(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitOwnerTruckConvoyPass(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitOwnerTruckConvoyPass(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitOwnerPostCombatPass(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitOwnerPostCombatPass(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitOwnerSecondReserve(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitOwnerSecondReserve(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitOwnerSecondMovement(CampaignSeatCredential credential, string elementId, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitOwnerSecondMovement(credential, elementId, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitOwnerSecondMovementCompletion(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitOwnerSecondMovementCompletion(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitOwnerRepeatMovement(CampaignSeatCredential credential, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitOwnerRepeatMovement(credential, openingRevision, publicActionRef);

    internal CampaignOwnerSubmissionResult SubmitOwnerRepeatCombat(CampaignSeatCredential credential, string actingElementId, long openingRevision, string publicActionRef) =>
        JournalFor(credential.CampaignId, createIfMissing: false).SubmitOwnerRepeatCombat(credential, actingElementId, openingRevision, publicActionRef);

    internal ExerciseSession CreateCampaign(CampaignCreationRequest request, CampaignDecisionTimingConfigurationV1? timing = null) =>
        JournalFor(request.CampaignId, createIfMissing: true).CreateCampaign(request, timing);

    internal CampaignSourceSession CreateCampaign(CampaignCreationRequest request, ContentSourceScheduledCombatPackArtifact content, CampaignSourceInitialization initialization) =>
        JournalFor(request.CampaignId, createIfMissing: true).CreateCampaign(request, content, initialization);

    internal ExerciseSession CreatePregameSourceCampaign(CampaignCreationRequest request, ContentSourceScheduledCombatPackArtifact content, CampaignSourceInitialization initialization) =>
        JournalFor(request.CampaignId, createIfMissing: true).CreatePregameSourceCampaign(request, content, initialization);

    internal ExerciseSession ReadVerified(string campaignId) =>
        JournalFor(campaignId, createIfMissing: false).ReadVerified(campaignId);

    internal CampaignSourceSession ReadSourceVerified(string campaignId) =>
        JournalFor(campaignId, createIfMissing: false).ReadSourceVerified(campaignId);

    internal ExerciseStepResult Append(string campaignId, CampaignActionSubmission submission) =>
        JournalFor(campaignId, createIfMissing: false).Append(campaignId, submission);

    internal bool AdmitSourceAxisSupplement(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceAxisSupplement(campaignId, operatorSecret);

    internal bool AdmitSourceCommonwealthSupplement(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceCommonwealthSupplement(campaignId, operatorSecret);

    internal bool AdmitSourcePriorityMotorization(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourcePriorityMotorization(campaignId, operatorSecret);

    internal bool AdmitSourceAttachedInitialLoads(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceAttachedInitialLoads(campaignId, operatorSecret);

    internal bool AdmitSourceInitialWrecks(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceInitialWrecks(campaignId, operatorSecret);

    internal bool AdmitSourceAxisFreeSetup(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceAxisFreeSetup(campaignId, operatorSecret);

    internal bool AdmitSourceAxisCorpsSetup(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceAxisCorpsSetup(campaignId, operatorSecret);

    internal bool AdmitSourceMalettiHierarchy(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceMalettiHierarchy(campaignId, operatorSecret);

    internal bool AdmitSourceGt2Correction(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceGt2Correction(campaignId, operatorSecret);

    internal bool AdmitSourceCwPriorityCatalog(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceCwPriorityCatalog(campaignId, operatorSecret);

    internal bool AdmitSourceCompactCheckpoint(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceCompactCheckpoint(campaignId, operatorSecret);

    internal bool AdmitSourceFourthIndianWorld(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceFourthIndianWorld(campaignId, operatorSecret);

    internal bool AdmitSourceMatruhGroupedWorld(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceMatruhGroupedWorld(campaignId, operatorSecret);

    internal bool AdmitSourceMalettiGroupedWorld(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceMalettiGroupedWorld(campaignId, operatorSecret);

    internal bool AdmitSourceLibyanGroupedWorld(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceLibyanGroupedWorld(campaignId, operatorSecret);

    internal bool AdmitSourceItalianDivisionsGroupedWorld(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceItalianDivisionsGroupedWorld(campaignId, operatorSecret);

    internal bool AdmitSourceSeventhArmoredGroupedWorld(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceSeventhArmoredGroupedWorld(campaignId, operatorSecret);

    internal bool AdmitSourceSixteenthBrigadeGroupedWorld(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceSixteenthBrigadeGroupedWorld(campaignId, operatorSecret);

    internal bool AdmitSourceCatanzaroGroupedWorld(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceCatanzaroGroupedWorld(campaignId, operatorSecret);

    internal bool StartSourceTurn(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).StartSourceTurn(campaignId, operatorSecret);

    internal bool ResolveSourceInitiative(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).ResolveSourceInitiative(campaignId, operatorSecret);

    internal bool CompleteSourceGt1Scheduling(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).CompleteSourceGt1Scheduling(campaignId, operatorSecret);

    internal bool CompleteSourceGt1Organization(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).CompleteSourceGt1Organization(campaignId, operatorSecret);

    internal bool CompleteSourceGt1StageOneArrivals(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).CompleteSourceGt1StageOneArrivals(campaignId, operatorSecret);

    internal bool CompleteSourceGt1StageOneFleet(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).CompleteSourceGt1StageOneFleet(campaignId, operatorSecret);

    internal bool CompleteSourceGt1FirstBreakdown(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).CompleteSourceGt1FirstBreakdown(campaignId, operatorSecret);

    internal bool ResolveSourceGt1Weather(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).ResolveSourceGt1Weather(campaignId, operatorSecret);

    internal bool AnnounceSourceGt1Lane2(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AnnounceSourceGt1Lane2(campaignId, operatorSecret);

    internal bool AnnounceSourceGt1Lane3(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AnnounceSourceGt1Lane3(campaignId, operatorSecret);

    internal bool CorrectSourceSeventhShell(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).CorrectSourceSeventhShell(campaignId, operatorSecret);

    internal bool ActivateSourceAxisMinorGarrisons(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).ActivateSourceAxisMinorGarrisons(campaignId, operatorSecret);

    internal bool ActivateSourceAxisGarrisonRemainder(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).ActivateSourceAxisGarrisonRemainder(campaignId, operatorSecret);

    internal bool ActivateSourceGiarabubMotorMachineGun(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).ActivateSourceGiarabubMotorMachineGun(campaignId, operatorSecret);

    internal bool ActivateSourceCwFrontline(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).ActivateSourceCwFrontline(campaignId, operatorSecret);

    internal bool AdmitSourceGiarabubMotorPlacementPolicy(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceGiarabubMotorPlacementPolicy(campaignId, operatorSecret);

    internal bool AnnounceSourceGt1MultiLanes(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AnnounceSourceGt1MultiLanes(campaignId, operatorSecret);

    internal bool AdmitSourceMatruhFormation(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceMatruhFormation(campaignId, operatorSecret);

    internal bool AdmitSourceCwFreeGuns(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceCwFreeGuns(campaignId, operatorSecret);

    internal bool AdmitSourceAxisPriorityMotorization(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceAxisPriorityMotorization(campaignId, operatorSecret);

    internal bool AdmitSourceAxisMinorGarrisons(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceAxisMinorGarrisons(campaignId, operatorSecret);

    internal bool AdmitSourceBattlefield(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceBattlefield(campaignId, operatorSecret);

    internal bool AdmitSourceCairoSetup(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceCairoSetup(campaignId, operatorSecret);

    internal bool AdmitSourceCairoWorld(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceCairoWorld(campaignId, operatorSecret);

    internal bool PromoteSourceMaletti(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).PromoteSourceMaletti(campaignId, operatorSecret);

    internal bool PromoteSourceIndependent(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).PromoteSourceIndependent(campaignId, operatorSecret);

    internal bool PromoteSourceExpandedIndependent(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).PromoteSourceExpandedIndependent(campaignId, operatorSecret);

    internal bool PromoteSourceAxisFreeWorld(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).PromoteSourceAxisFreeWorld(campaignId, operatorSecret);

    internal bool PromoteSourceAxisCorpsWorld(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).PromoteSourceAxisCorpsWorld(campaignId, operatorSecret);

    internal bool PromoteSourceCertifiedMalettiWorld(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).PromoteSourceCertifiedMalettiWorld(campaignId, operatorSecret);

    internal bool PromoteSourceMatruhDetachedWorld(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).PromoteSourceMatruhDetachedWorld(campaignId, operatorSecret);

    internal bool PromoteSourceCwPriorityWorld(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).PromoteSourceCwPriorityWorld(campaignId, operatorSecret);

    internal bool PromoteSourceCwFreeGunsWorld(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).PromoteSourceCwFreeGunsWorld(campaignId, operatorSecret);

    internal bool AdmitSourceLocalMap(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitSourceLocalMap(campaignId, operatorSecret);

    internal int AdmitAvailableSourceProfiles(string campaignId, string operatorSecret) =>
        JournalFor(campaignId, createIfMissing: false).AdmitAvailableSourceProfiles(campaignId, operatorSecret);
}
