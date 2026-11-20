using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Cna.Core.Actions;

// Task 020B (DRAFT for owner review; not frozen). Byte-level verifier for the audience decision and
// outcome envelopes produced by CampaignCombatAudienceDecisions, checked against the draft schema in
// docs/specs/combat-audience-decision-draft-v1.schema.json. Covers Selection, Force Assignment,
// Reserve Release, Cycle Control, decline-only RBA (020A4) and captor Custody (020A3). Any other
// stage fails as uncovered.

/// <summary>Checks one decision or outcome byte string against the draft successor contract.</summary>
internal static partial class CampaignCombatAudienceDraftVerifier
{
    public const string ContractId = "combat-audience-decision-draft-v1";

    public static readonly IReadOnlyList<string> DecisionFields =
    [
        "contractVersion", "campaignId", "audience", "stage", "actingElementId",
        "audienceRevision", "visibleDigest", "visible", "offerRefs",
    ];

    /// <summary>Exact ordered top-level fields and contract version of each covered visible cut.</summary>
    public static readonly IReadOnlyDictionary<string, (int Version, string[] Fields)> VisibleCuts =
        new Dictionary<string, (int, string[])>(StringComparer.Ordinal)
        {
            [CampaignCombatAudienceDecisions.SelectionStage] = (2,
            [
                "contractVersion", "campaignId", "side", "actingFormationId", "contactAudienceRevision",
                "contactObservation", "locations", "edges", "ownElements", "assaultOffers", "finishOffer",
            ]),
            [CampaignCombatAudienceDecisions.ForceAssignmentStage] = (1,
            [
                "contractVersion", "campaignId", "side", "positionId", "role", "contactObservation",
                "ownElements", "sealOffer",
            ]),
            [CampaignCombatAudienceDecisions.ReserveReleaseStage] = (27,
            [
                "contractVersion", "campaignId", "side", "cycleOrdinal", "elementId", "reserveStatus",
                "choices",
            ]),
            [CampaignCombatAudienceDecisions.CycleControlStage] = (28,
            [
                "contractVersion", "campaignId", "side", "cycleOrdinal", "choices",
            ]),
            // 020A4: decline-only RBA codec (CampaignContactRbaDeclineOwnerVisibleCutV1). It has no
            // moveOffers, locations, edges or initiallyAdjacentToKnownEnemy field by design.
            [CampaignCombatAudienceDecisions.RbaDeclineStage] = (1,
            [
                "contractVersion", "campaignId", "side", "positionId", "defenderFormationId",
                "contactAudienceRevision", "contactObservation", "ownElements", "declineOffer",
            ]),
            // 020A3 captor Custody (CampaignContactCustodyOwnerVisibleCutV1). Its field set is the
            // T020-Q5 allowlist (ruled 2026-11-20). No side and no contact observation.
            [CampaignCombatAudienceDecisions.CustodyStage] = (1,
            [
                "contractVersion", "campaignId", "captorElementId", "captiveQuantity", "offers",
            ]),
        };

    /// <summary>Property names that must not appear anywhere in outward bytes.</summary>
    public static readonly IReadOnlyList<string> ForbiddenKeys =
    [
        "authorityVersion", "stateVersion", "actionId", "actionIds", "privateActionIds",
        "boundRepresentationId", "boundDefenderId", "headEventHash", "worldHash", "settlementHash",
        "randomState", "seed",
    ];

    public static readonly IReadOnlyList<string> AssaultOfferFields =
        ["kind", "actingElementId", "contactHandle", "apparentLocationId"];
    public static readonly IReadOnlyList<string> FinishOfferFields = ["kind", "actingElementId"];
    public static readonly IReadOnlyList<string> SealOfferFields =
        ["kind", "role", "elementId", "componentId", "committedToe"];
    public static readonly IReadOnlyList<string> DeclineOfferFields = ["kind", "elementId"];
    public static readonly IReadOnlyList<string> CustodyOfferFields = ["kind", "route"];
    public static readonly IReadOnlyList<string> CustodyOfferKinds = ["relocate-and-guard", "leave-unguarded"];

    public static readonly string RejectedOutcome = "{\"contractVersion\":1,\"outcome\":\"rejected\"}";

    [GeneratedRegex("^cmbref\\.v1\\.[0-9a-f]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex RefPattern();

    [GeneratedRegex("^sha256:[0-9a-f]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex DigestPattern();

    /// <summary>Returns the ordered distinct failure codes; empty means the bytes conform.</summary>
    public static IReadOnlyList<string> VerifyDecision(byte[] bytes)
    {
        var failures = new List<string>();
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(bytes);
        }
        catch (JsonException)
        {
            return ["malformed-json"];
        }
        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return ["decision-fields"];
            if (!Names(root).SequenceEqual(DecisionFields)) failures.Add("decision-fields");
            CheckForbidden(root, failures);
            if (!TryInt(root, "contractVersion", out var version)
                || version != CampaignCombatAudienceDecision.CurrentContractVersion)
                failures.Add("contract-version");
            var campaignId = Text(root, "campaignId");
            var audience = Text(root, "audience");
            var stage = Text(root, "stage");
            var actor = Text(root, "actingElementId");
            var digest = Text(root, "visibleDigest");
            if (string.IsNullOrWhiteSpace(campaignId)) failures.Add("campaign-id");
            CampaignActionAudience? audienceValue = audience switch
            {
                "axis" => CampaignActionAudience.Axis,
                "commonwealth" => CampaignActionAudience.Commonwealth,
                _ => null,
            };
            if (audienceValue is null) failures.Add("audience");
            if (stage is null || !VisibleCuts.ContainsKey(stage)) failures.Add("stage-uncovered");
            if (string.IsNullOrWhiteSpace(actor)) failures.Add("acting-subject");
            if (!TryLong(root, "audienceRevision", out var revision) || revision < 0)
                failures.Add("audience-revision");
            if (!root.TryGetProperty("visible", out var visible) || visible.ValueKind != JsonValueKind.Object)
            {
                failures.Add("visible-fields");
                return Distinct(failures);
            }
            var visibleBytes = Encoding.UTF8.GetBytes(visible.GetRawText());
            if (digest is null || !DigestPattern().IsMatch(digest)
                || digest != CampaignCombatAudienceDecisions.Digest(visibleBytes))
                failures.Add("visible-digest");
            if (!IsCompact(visibleBytes)) failures.Add("non-canonical");

            var refs = new List<string>();
            if (!root.TryGetProperty("offerRefs", out var refArray) || refArray.ValueKind != JsonValueKind.Array)
                failures.Add("offer-ref-format");
            else
            {
                foreach (var value in refArray.EnumerateArray())
                {
                    if (value.ValueKind != JsonValueKind.String || !RefPattern().IsMatch(value.GetString()!))
                        failures.Add("offer-ref-format");
                    else refs.Add(value.GetString()!);
                }
                if (refs.Count == 0 || refs.Distinct(StringComparer.Ordinal).Count() != refs.Count)
                    failures.Add("offer-count");
            }

            var offers = -1;
            if (stage is not null && VisibleCuts.TryGetValue(stage, out var cut) && audience is not null)
                offers = CheckVisible(stage, cut, visible, campaignId, audience, actor, revision, failures);
            if (offers >= 0 && offers != refs.Count) failures.Add("offer-count");

            // Refs derive only from decision-visible values; recompute each one.
            if (campaignId is not null && audienceValue is { } enumAudience && stage is not null
                && actor is not null && digest is not null)
            {
                for (var index = 0; index < refs.Count; index++)
                {
                    if (refs[index] != CampaignCombatAudienceDecisions.Reference(campaignId, enumAudience,
                            stage, actor, digest, index))
                        failures.Add("offer-ref-derivation");
                }
                // Exact canonical bytes: the production serializer must reproduce the input.
                if (failures.Count == 0)
                {
                    var rebuilt = new CampaignCombatAudienceDecision(campaignId, enumAudience, stage, actor,
                        revision, visibleBytes, refs).SerializeCanonical();
                    if (!rebuilt.AsSpan().SequenceEqual(bytes)) failures.Add("non-canonical");
                }
            }
        }
        return Distinct(failures);
    }

    /// <summary>
    /// Verifies an outward submission outcome. When a decision is supplied, an accepted ref must be one
    /// of its offered refs.
    /// </summary>
    public static IReadOnlyList<string> VerifyOutcome(byte[] bytes, byte[]? decision = null)
    {
        var text = Encoding.UTF8.GetString(bytes);
        if (text == RejectedOutcome) return [];
        const string prefix = "{\"contractVersion\":1,\"outcome\":\"accepted\",\"offerRef\":\"";
        if (!text.StartsWith(prefix, StringComparison.Ordinal) || !text.EndsWith("\"}", StringComparison.Ordinal))
            return ["outcome-shape"];
        var offerRef = text[prefix.Length..^2];
        if (!RefPattern().IsMatch(offerRef)) return ["offer-ref-format"];
        if (decision is null) return [];
        using var document = JsonDocument.Parse(decision);
        return document.RootElement.GetProperty("offerRefs").EnumerateArray()
            .Any(value => value.GetString() == offerRef) ? [] : ["offer-ref-unknown"];
    }

    private static int CheckVisible(string stage, (int Version, string[] Fields) cut, JsonElement visible,
        string? campaignId, string audience, string? actor, long revision, List<string> failures)
    {
        if (!Names(visible).SequenceEqual(cut.Fields)) failures.Add("visible-fields");
        if (!TryInt(visible, "contractVersion", out var version) || version != cut.Version)
            failures.Add("visible-version");
        if (Text(visible, "campaignId") != campaignId) failures.Add("visible-binding");
        switch (stage)
        {
            case CampaignCombatAudienceDecisions.SelectionStage:
                {
                    if (Text(visible, "side") != audience) failures.Add("visible-binding");
                    if (!TryLong(visible, "contactAudienceRevision", out var contactRevision)
                        || contactRevision != revision || revision < 1)
                        failures.Add("audience-revision");
                    var targets = CheckObservation(visible, campaignId, audience, revision, failures);
                    var count = 0;
                    if (visible.TryGetProperty("assaultOffers", out var assaults)
                        && assaults.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var offer in assaults.EnumerateArray())
                        {
                            count++;
                            if (offer.ValueKind != JsonValueKind.Object
                                || !Names(offer).SequenceEqual(AssaultOfferFields)
                                || Text(offer, "kind") != "select-contact-assault")
                            {
                                failures.Add("offer-shape");
                                continue;
                            }
                            if (Text(offer, "actingElementId") != actor) failures.Add("offer-binding");
                            // Stale or inaccessible contacts cannot be direct assault targets.
                            if (!targets.Contains((Text(offer, "contactHandle"), Text(offer, "apparentLocationId"))))
                                failures.Add("offer-target");
                        }
                    }
                    else failures.Add("offer-shape");
                    if (visible.TryGetProperty("finishOffer", out var finish)
                        && finish.ValueKind != JsonValueKind.Null)
                    {
                        count++;
                        if (finish.ValueKind != JsonValueKind.Object
                            || !Names(finish).SequenceEqual(FinishOfferFields)
                            || Text(finish, "kind") != "finish-combat-without-attack")
                            failures.Add("offer-shape");
                        else if (Text(finish, "actingElementId") != actor) failures.Add("offer-binding");
                    }
                    return count;
                }
            case CampaignCombatAudienceDecisions.ForceAssignmentStage:
                {
                    if (Text(visible, "side") != audience) failures.Add("visible-binding");
                    if (revision < 1) failures.Add("audience-revision");
                    CheckObservation(visible, campaignId, audience, revision, failures);
                    if (!visible.TryGetProperty("sealOffer", out var seal) || seal.ValueKind != JsonValueKind.Object
                        || !Names(seal).SequenceEqual(SealOfferFields)
                        || Text(seal, "kind") != "seal-combat-force-assignment")
                    {
                        failures.Add("offer-shape");
                        return 1;
                    }
                    if (Text(seal, "elementId") != actor || Text(seal, "role") != Text(visible, "role"))
                        failures.Add("offer-binding");
                    return 1;
                }
            case CampaignCombatAudienceDecisions.CustodyStage:
                {
                    // Allowlisted captor cut: revision 0 (no contact observation), the captor is the
                    // acting element, 1-10 captives, 1-2 distinct offers each with a 1-16 hex route.
                    // The cut carries no side, so the audience cannot be cross-checked from bytes.
                    if (revision != 0) failures.Add("audience-revision");
                    if (Text(visible, "captorElementId") != actor) failures.Add("visible-binding");
                    if (!TryInt(visible, "captiveQuantity", out var quantity) || quantity is < 1 or > 10)
                        failures.Add("visible-binding");
                    if (!visible.TryGetProperty("offers", out var offers) || offers.ValueKind != JsonValueKind.Array)
                    {
                        failures.Add("offer-shape");
                        return -1;
                    }
                    var kinds = new List<string?>();
                    foreach (var offer in offers.EnumerateArray())
                    {
                        if (offer.ValueKind != JsonValueKind.Object
                            || !Names(offer).SequenceEqual(CustodyOfferFields)
                            || !CustodyOfferKinds.Contains(Text(offer, "kind"), StringComparer.Ordinal)
                            || !offer.TryGetProperty("route", out var route) || route.ValueKind != JsonValueKind.Array
                            || route.GetArrayLength() is < 1 or > 16
                            || route.EnumerateArray().Any(value => value.ValueKind != JsonValueKind.String
                                || string.IsNullOrWhiteSpace(value.GetString())))
                            failures.Add("offer-shape");
                        kinds.Add(offer.ValueKind == JsonValueKind.Object ? Text(offer, "kind") : null);
                    }
                    if (kinds.Count is < 1 or > 2 || kinds.Distinct(StringComparer.Ordinal).Count() != kinds.Count)
                        failures.Add("offer-shape");
                    return kinds.Count;
                }
            case CampaignCombatAudienceDecisions.RbaDeclineStage:
                {
                    if (Text(visible, "side") != audience) failures.Add("visible-binding");
                    if (!TryLong(visible, "contactAudienceRevision", out var contactRevision)
                        || contactRevision != revision || revision < 1)
                        failures.Add("audience-revision");
                    CheckObservation(visible, campaignId, audience, revision, failures);
                    if (!visible.TryGetProperty("declineOffer", out var decline)
                        || decline.ValueKind != JsonValueKind.Object
                        || !Names(decline).SequenceEqual(DeclineOfferFields)
                        || Text(decline, "kind") != "decline-retreat-before-assault")
                    {
                        failures.Add("offer-shape");
                        return 1;
                    }
                    if (Text(decline, "elementId") != actor) failures.Add("offer-binding");
                    return 1;
                }
            case CampaignCombatAudienceDecisions.ReserveReleaseStage:
            case CampaignCombatAudienceDecisions.CycleControlStage:
                {
                    // These cuts carry no contact observation (revision 0; T020-Q4 accepted subset).
                    // Their side uses the V27/V28 enum spelling, recorded as observed.
                    if (Text(visible, "side") != (audience == "axis" ? "Axis" : "Commonwealth"))
                        failures.Add("visible-binding");
                    if (revision != 0) failures.Add("audience-revision");
                    if (stage == CampaignCombatAudienceDecisions.ReserveReleaseStage
                        ? Text(visible, "elementId") != actor
                        : actor != CampaignCombatAudienceDecisions.CycleControlSubject)
                        failures.Add(stage == CampaignCombatAudienceDecisions.ReserveReleaseStage
                            ? "visible-binding" : "acting-subject");
                    if (!visible.TryGetProperty("choices", out var choices) || choices.ValueKind != JsonValueKind.Array
                        || choices.EnumerateArray().Any(value => value.ValueKind != JsonValueKind.String))
                    {
                        failures.Add("offer-shape");
                        return -1;
                    }
                    var names = choices.EnumerateArray().Select(value => value.GetString()).ToArray();
                    if (names.Distinct(StringComparer.Ordinal).Count() != names.Length) failures.Add("offer-shape");
                    return names.Length;
                }
            default:
                return -1;
        }
    }

    private static HashSet<(string?, string?)> CheckObservation(JsonElement visible, string? campaignId,
        string audience, long revision, List<string> failures)
    {
        var targets = new HashSet<(string?, string?)>();
        if (!visible.TryGetProperty("contactObservation", out var observation)
            || observation.ValueKind != JsonValueKind.Object)
        {
            failures.Add("visible-fields");
            return targets;
        }
        if (Text(observation, "campaignId") != campaignId || Text(observation, "side") != audience)
            failures.Add("visible-binding");
        if (!TryLong(observation, "audienceRevision", out var observed) || observed != revision)
            failures.Add("audience-revision");
        var current = new HashSet<string?>(StringComparer.Ordinal);
        if (observation.TryGetProperty("contacts", out var contacts) && contacts.ValueKind == JsonValueKind.Array)
        {
            foreach (var contact in contacts.EnumerateArray())
            {
                if (contact.ValueKind == JsonValueKind.Object
                    && contact.TryGetProperty("isCurrent", out var isCurrent)
                    && isCurrent.ValueKind == JsonValueKind.True)
                    current.Add(Text(contact, "handle"));
            }
        }
        if (observation.TryGetProperty("currentTargets", out var listed) && listed.ValueKind == JsonValueKind.Array)
        {
            foreach (var target in listed.EnumerateArray())
            {
                var handle = target.ValueKind == JsonValueKind.Object ? Text(target, "contactHandle") : null;
                if (current.Contains(handle)) targets.Add((handle, Text(target, "apparentLocationId")));
            }
        }
        return targets;
    }

    private static void CheckForbidden(JsonElement element, List<string> failures)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (ForbiddenKeys.Contains(property.Name, StringComparer.Ordinal)) failures.Add("forbidden-field");
                    CheckForbidden(property.Value, failures);
                }
                break;
            case JsonValueKind.Array:
                foreach (var value in element.EnumerateArray()) CheckForbidden(value, failures);
                break;
        }
    }

    private static bool IsCompact(byte[] visibleBytes)
    {
        using var document = JsonDocument.Parse(visibleBytes);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) document.RootElement.WriteTo(writer);
        return stream.ToArray().AsSpan().SequenceEqual(visibleBytes);
    }

    private static IEnumerable<string> Names(JsonElement element) =>
        element.EnumerateObject().Select(value => value.Name);

    private static string? Text(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static bool TryInt(JsonElement element, string name, out int value)
    {
        value = 0;
        return element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.Number
            && property.TryGetInt32(out value);
    }

    private static bool TryLong(JsonElement element, string name, out long value)
    {
        value = 0;
        return element.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.Number
            && property.TryGetInt64(out value);
    }

    private static string[] Distinct(List<string> failures) =>
        failures.Distinct(StringComparer.Ordinal).ToArray();
}
