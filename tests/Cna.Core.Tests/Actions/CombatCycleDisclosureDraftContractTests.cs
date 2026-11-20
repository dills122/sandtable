using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Actions;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Tests.Campaigns;

namespace Cna.Core.Tests.Actions;

/// <summary>
/// Frozen combat-audience-decision-v1 (CMB-CON-005 successor; owner sign-off 2026-11-20): schema, goldens,
/// rejection vectors and verifier for Selection, Force Assignment, Reserve Release, Cycle Control,
/// decline-only RBA and Custody. Forced/surprise and the non-combat-unit disposition need no
/// visible-byte rule under the 2026-11-20 rulings (T020-Q2/Q8).
/// </summary>
public sealed class CombatCycleDisclosureDraftContractTests
{
    private const string FixturePath = "docs/specs/fixtures/combat-audience-decision-v1.json";
    private const string SchemaPath = "docs/specs/combat-audience-decision-v1.schema.json";

    [Fact]
    public void FrozenSchemaMatchesTheVerifierRules()
    {
        using var schema = JsonDocument.Parse(CombatActualPaidAssaultTests.Dependency(SchemaPath));
        var root = schema.RootElement;
        Assert.Equal(CampaignCombatAudienceDraftVerifier.ContractId, root.GetProperty("contract").GetString());
        Assert.Equal(CampaignCombatAudienceDraftVerifier.DecisionFields, Strings(root.GetProperty("decisionFields")));
        Assert.Equal(CampaignCombatAudienceDraftVerifier.ForbiddenKeys, Strings(root.GetProperty("forbiddenKeys")));
        var stages = root.GetProperty("stages").EnumerateObject().ToArray();
        Assert.Equal(CampaignCombatAudienceDraftVerifier.VisibleCuts.Keys.Order(StringComparer.Ordinal),
            stages.Select(value => value.Name).Order(StringComparer.Ordinal));
        foreach (var stage in stages)
        {
            var (version, fields) = CampaignCombatAudienceDraftVerifier.VisibleCuts[stage.Name];
            Assert.Equal(version, stage.Value.GetProperty("visibleContractVersion").GetInt32());
            Assert.Equal(fields, Strings(stage.Value.GetProperty("visibleFields")));
        }
        // Unresolved stages are named as gaps, never as covered stages.
        var gaps = root.GetProperty("notCovered").EnumerateArray()
            .Where(value => value.TryGetProperty("stage", out _))
            .Select(value => value.GetProperty("stage").GetString()!).ToArray();
        Assert.Contains("post-assault-retreat", gaps);
        Assert.Empty(gaps.Intersect(CampaignCombatAudienceDraftVerifier.VisibleCuts.Keys));
        foreach (var document in new[] { root, Fixture().RootElement })
        {
            var status = document.GetProperty("status").GetString()!;
            Assert.StartsWith("Frozen v1 (CMB-CON-005 successor)", status, StringComparison.Ordinal);
            Assert.DoesNotContain("DRAFT", status, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void GoldensReproduceFromCurrentCoreAndConform()
    {
        using var fixture = Fixture();
        var live = LiveDecisions();
        var goldens = fixture.RootElement.GetProperty("goldens").EnumerateArray().ToArray();
        Assert.Equal(live.Keys.Order(StringComparer.Ordinal),
            goldens.Select(value => value.GetProperty("id").GetString()!).Order(StringComparer.Ordinal));
        Assert.Equal(CampaignCombatAudienceDraftVerifier.VisibleCuts.Keys.Order(StringComparer.Ordinal),
            goldens.Select(value => value.GetProperty("stage").GetString()!).Distinct().Order(StringComparer.Ordinal));
        foreach (var golden in goldens)
        {
            var bytes = GoldenBytes(golden);
            Assert.Equal(golden.GetProperty("decisionLength").GetInt32(), bytes.Length);
            Assert.Equal(golden.GetProperty("decisionSha256").GetString(), CampaignCombatAudienceDecisions.Digest(bytes));
            Assert.Equal(bytes, live[golden.GetProperty("id").GetString()!].SerializeCanonical());
            Assert.Empty(CampaignCombatAudienceDraftVerifier.VerifyDecision(bytes));
            // The tamper helper's re-serialization is byte-exact for every golden.
            Assert.Equal(bytes, Encoding.UTF8.GetBytes(JsonNode.Parse(bytes)!.ToJsonString()));
        }

        var envelopes = fixture.RootElement.GetProperty("envelopes");
        var rejected = Encoding.UTF8.GetBytes(envelopes.GetProperty("rejected").GetString()!);
        Assert.Equal(CampaignCombatAudienceSubmissionResult.Rejected.SerializeOutwardCanonical(), rejected);
        Assert.Empty(CampaignCombatAudienceDraftVerifier.VerifyOutcome(rejected));
        var accepted = Encoding.UTF8.GetBytes(envelopes.GetProperty("acceptedExample").GetProperty("utf8").GetString()!);
        var cycle = GoldenBytes(Golden(fixture, "cycle-control-axis"));
        Assert.Empty(CampaignCombatAudienceDraftVerifier.VerifyOutcome(accepted, cycle));
        AssertOnly("offer-ref-unknown", CampaignCombatAudienceDraftVerifier.VerifyOutcome(accepted,
            GoldenBytes(Golden(fixture, "reserve-release-axis"))));
        AssertOnly("outcome-shape", CampaignCombatAudienceDraftVerifier.VerifyOutcome(
            Encoding.UTF8.GetBytes("{\"contractVersion\":1,\"outcome\":\"rejected\",\"cause\":\"stale\"}")));
        AssertOnly("offer-ref-format", CampaignCombatAudienceDraftVerifier.VerifyOutcome(
            Encoding.UTF8.GetBytes("{\"contractVersion\":1,\"outcome\":\"accepted\",\"offerRef\":\"action-1\"}")));
    }

    [Fact]
    public void RejectionVectorsYieldTheGoldenRejectionEnvelopeForEveryCoveredStage()
    {
        using var fixture = Fixture();
        var rejected = Encoding.UTF8.GetBytes(fixture.RootElement.GetProperty("envelopes")
            .GetProperty("rejected").GetString()!);
        var vectors = fixture.RootElement.GetProperty("rejectionVectors");
        Assert.Equal(CampaignCombatAudienceDraftVerifier.VisibleCuts.Keys.Order(StringComparer.Ordinal),
            vectors.EnumerateObject().Select(value => value.Name).Order(StringComparer.Ordinal));

        var selection = CombatCycleDisclosureForceTests.AdvanceToCombat("campaign-con005-draft-selection", 12345);
        var force = CombatCycleDisclosureForceTests.AdvanceToForce("campaign-con005-draft-force", 12345);
        var reserve = CombatCycleDisclosureReserveCycleTests.AtReserveRelease("campaign-con005-draft-reserve", 6);
        var reserveDecision = CampaignCombatAudienceDecisions.QueryReserveRelease(reserve,
            CampaignActionAudience.Axis, "axis-infantry-second")!;
        var cycle = CombatCycleDisclosureReserveCycleTests.Complete(
            CampaignCombatAudienceDecisions.Submit(reserve, reserveDecision.Choose(0)).TrustedStep!.SuccessorSession!);
        var forceOther = CampaignCombatAudienceDecisions.QueryForceAssignment(force,
            CampaignActionAudience.Commonwealth, "commonwealth-infantry")!;
        var rba = CombatCycleDisclosureRbaDeclineTests.AtRba("campaign-con005-draft-rba", 12345);
        var custody = CombatCycleDisclosureCustodyTests.AtCustody("campaign-con005-draft-custody", 5, out _);
        (string Stage, ExerciseSession Session, CampaignCombatAudienceDecision Decision)[] cases =
        [
            ("custody", custody, CampaignCombatAudienceDecisions.QueryCustody(custody,
                CampaignActionAudience.Axis, "axis-infantry")!),
            ("rba-decline", rba, CampaignCombatAudienceDecisions.QueryRbaDecline(rba,
                CampaignActionAudience.Commonwealth, "commonwealth-infantry")!),
            ("combat-selection", selection, CampaignCombatAudienceDecisions.QuerySelection(selection,
                CampaignActionAudience.Axis, "axis-infantry")!),
            ("force-assignment", force, CampaignCombatAudienceDecisions.QueryForceAssignment(force,
                CampaignActionAudience.Axis, "axis-infantry")!),
            ("reserve-release", reserve, reserveDecision),
            ("cycle-control", cycle, CampaignCombatAudienceDecisions.QueryCycleControl(cycle,
                CampaignActionAudience.Axis)!),
        ];
        foreach (var (stage, session, decision) in cases)
        {
            var valid = decision.Choose(0);
            var count = 0;
            foreach (var vector in vectors.GetProperty(stage).EnumerateArray())
            {
                var id = vector.GetProperty("id").GetString()!;
                CampaignCombatAudienceSubmissionResult result;
                if (vector.TryGetProperty("replay", out _))
                {
                    var accepted = CampaignCombatAudienceDecisions.Submit(session, valid);
                    Assert.True(accepted.IsAccepted, $"{stage}/{id}");
                    result = CampaignCombatAudienceDecisions.Submit(accepted.TrustedStep!.SuccessorSession!, valid);
                }
                else if (vector.TryGetProperty("entry", out var entry))
                {
                    Assert.Equal("SubmitSelection", entry.GetString());
                    result = CampaignCombatAudienceDecisions.SubmitSelection(session, valid);
                }
                else result = CampaignCombatAudienceDecisions.Submit(session,
                    Mutate(valid, vector, session, forceOther));
                Assert.False(result.IsAccepted, $"{stage}/{id}");
                Assert.Null(result.TrustedStep);
                Assert.Equal(rejected, result.SerializeOutwardCanonical());
                count++;
            }
            Assert.True(count >= 8, stage);
            var ok = CampaignCombatAudienceDecisions.Submit(session, valid);
            Assert.True(ok.IsAccepted, stage);
            Assert.Empty(CampaignCombatAudienceDraftVerifier.VerifyOutcome(ok.SerializeOutwardCanonical(),
                decision.SerializeCanonical()));
        }
    }

    [Fact]
    public void VerifierCatchesTamperedSelectionCuts()
    {
        var golden = GoldenBytes(Golden(Fixture(), "selection-axis-first-cut"));
        AssertCommonTampers(golden, "contactObservation");
        AssertCaught(golden, "offer-target", value =>
        {
            Visible(value)["assaultOffers"]![0]!["contactHandle"] = "c-stale-00000099";
            Resign(value);
        });
        AssertCaught(golden, "offer-binding", value =>
        {
            Visible(value)["assaultOffers"]![0]!["actingElementId"] = "axis-other";
            Resign(value);
        });
        AssertCaught(golden, "offer-count", value =>
        {
            Visible(value)["finishOffer"] = null;
            Resign(value);
        });
        AssertCaught(golden, "audience-revision", value =>
        {
            Visible(value)["contactAudienceRevision"] = 99;
            Resign(value);
        });
    }

    [Fact]
    public void VerifierCatchesTamperedForceAssignmentCuts()
    {
        using var fixture = Fixture();
        foreach (var id in new[] { "force-assignment-axis", "force-assignment-commonwealth" })
        {
            var golden = GoldenBytes(Golden(fixture, id));
            AssertCommonTampers(golden, "contactObservation");
            AssertCaught(golden, "offer-binding", value =>
            {
                Visible(value)["sealOffer"]!["elementId"] = "someone-else";
                Resign(value);
            });
            AssertCaught(golden, "offer-shape", value =>
            {
                Visible(value)["sealOffer"]!["kind"] = "seal-anything";
                Resign(value);
            });
            AssertCaught(golden, "visible-binding", value =>
            {
                Visible(value)["side"] = value["audience"]!.GetValue<string>() == "axis" ? "commonwealth" : "axis";
                Resign(value);
            });
        }
    }

    [Fact]
    public void VerifierCatchesTamperedReserveReleaseCuts()
    {
        var golden = GoldenBytes(Golden(Fixture(), "reserve-release-axis"));
        AssertCommonTampers(golden, null);
        AssertCaught(golden, "visible-binding", value =>
        {
            Visible(value)["elementId"] = "axis-infantry";
            Resign(value);
        });
        // The legacy V27 enum spelling is no longer accepted (lowercase since the freeze).
        AssertCaught(golden, "visible-binding", value =>
        {
            Visible(value)["side"] = "Axis";
            Resign(value);
        });
        AssertCaught(golden, "visible-binding", value =>
        {
            Visible(value)["side"] = "commonwealth";
            Resign(value);
        });
        AssertCaught(golden, "offer-shape", value =>
        {
            Visible(value)["choices"] = new JsonArray("ReleaseI", "ReleaseI");
            Resign(value);
        });
    }

    [Fact]
    public void VerifierCatchesTamperedCycleControlCuts()
    {
        var golden = GoldenBytes(Golden(Fixture(), "cycle-control-axis"));
        AssertCommonTampers(golden, null);
        AssertCaught(golden, "acting-subject", value =>
        {
            value["actingElementId"] = "axis-infantry";
            Resign(value);
        });
        // The legacy V28 enum spelling is no longer accepted (lowercase since the freeze).
        AssertCaught(golden, "visible-binding", value =>
        {
            Visible(value)["side"] = "Axis";
            Resign(value);
        });
        AssertCaught(golden, "offer-count", value =>
        {
            Visible(value)["choices"] = new JsonArray("Repeat", "Finish", "Surrender");
            Resign(value, refCount: 2);
        });
    }

    [Fact]
    public void VerifierCatchesTamperedCustodyCuts()
    {
        var golden = GoldenBytes(Golden(Fixture(), "custody-axis"));
        AssertCommonTampers(golden, null);
        AssertCaught(golden, "visible-binding", value =>
        {
            Visible(value)["captorElementId"] = "axis-infantry-second";
            Resign(value);
        });
        AssertCaught(golden, "visible-binding", value =>
        {
            Visible(value)["captiveQuantity"] = 0;
            Resign(value);
        });
        AssertCaught(golden, "offer-shape", value =>
        {
            Visible(value)["offers"]![0]!["kind"] = "release-captives";
            Resign(value);
        });
        AssertCaught(golden, "offer-shape", value =>
        {
            Visible(value)["offers"]![1]!["kind"] = Visible(value)["offers"]![0]!["kind"]!.GetValue<string>();
            Resign(value);
        });
        AssertCaught(golden, "offer-shape", value =>
        {
            Visible(value)["offers"]![0]!["route"] = new JsonArray();
            Resign(value);
        });
        // Allowlist (T020-Q5): a victim or settlement field is a field-set violation, re-signed.
        AssertCaught(golden, "visible-fields", value =>
        {
            Visible(value)["victimElementId"] = "commonwealth-infantry";
            Resign(value);
        });
        AssertCaught(golden, "offer-shape", value =>
        {
            Visible(value)["offers"]![0]!["lotId"] = "lot-1";
            Resign(value);
        });
    }

    [Fact]
    public void VerifierCatchesTamperedRbaDeclineCuts()
    {
        var golden = GoldenBytes(Golden(Fixture(), "rba-decline-commonwealth"));
        AssertCommonTampers(golden, "contactObservation");
        AssertCaught(golden, "offer-binding", value =>
        {
            Visible(value)["declineOffer"]!["elementId"] = "commonwealth-truck";
            Resign(value);
        });
        AssertCaught(golden, "offer-shape", value =>
        {
            Visible(value)["declineOffer"]!["kind"] = "retreat-before-assault-move";
            Resign(value);
        });
        // A move offer smuggled into the decline-only codec is a field-set violation.
        AssertCaught(golden, "visible-fields", value =>
        {
            Visible(value)["moveOffers"] = new JsonArray();
            Resign(value);
        });
        AssertCaught(golden, "visible-binding", value =>
        {
            Visible(value)["side"] = "axis";
            Resign(value);
        });
        AssertCaught(golden, "audience-revision", value =>
        {
            Visible(value)["contactAudienceRevision"] = 99;
            Resign(value);
        });
    }

    // Tampers every covered round kind must reject. `nestedObject` names a visible sub-object used
    // to plant a private field without also changing the visible top-level field list.
    private static void AssertCommonTampers(byte[] golden, string? nestedObject)
    {
        Assert.Empty(CampaignCombatAudienceDraftVerifier.VerifyDecision(golden));
        AssertCaught(golden, "visible-digest", value => Visible(value)["campaignId"] = "drifted");
        AssertCaught(golden, "offer-ref-derivation", value =>
            value["offerRefs"]![0] = "cmbref.v1." + new string('0', 64));
        AssertCaught(golden, "offer-ref-format", value => value["offerRefs"]![0] = "core-action-0001");
        AssertCaught(golden, "offer-count", value => value["offerRefs"]!.AsArray().RemoveAt(0));
        AssertCaught(golden, "offer-count", value =>
            value["offerRefs"]!.AsArray().Add(value["offerRefs"]![0]!.GetValue<string>()));
        AssertCaught(golden, "decision-fields", value =>
        {
            var audience = value["audience"]!.GetValue<string>();
            value.Remove("audience");
            value.Add("audience", audience);
        });
        AssertCaught(golden, "forbidden-field", value => value["authorityVersion"] = 7);
        AssertCaught(golden, "decision-fields", value => value["authorityVersion"] = 7);
        AssertCaught(golden, "contract-version", value => value["contractVersion"] = 2);
        AssertCaught(golden, "audience", value => value["audience"] = "system");
        AssertCaught(golden, "stage-uncovered", value =>
        {
            value["stage"] = "post-assault-retreat";
            Resign(value);
        });
        AssertCaught(golden, "visible-binding", value =>
        {
            value["campaignId"] = "another-campaign";
            Resign(value);
        });
        AssertCaught(golden, "audience-revision", value =>
        {
            value["audienceRevision"] = value["audienceRevision"]!.GetValue<long>() + 1;
            Resign(value);
        });
        AssertCaught(golden, "visible-version", value =>
        {
            Visible(value)["contractVersion"] = Visible(value)["contractVersion"]!.GetValue<int>() + 1;
            Resign(value);
        });
        AssertCaught(golden, "visible-fields", value =>
        {
            // campaignId is the one top-level visible field every covered cut carries (Custody has no side).
            Visible(value).Remove("campaignId");
            Resign(value);
        });
        AssertCaught(golden, "forbidden-field", value =>
        {
            var target = nestedObject is null ? Visible(value) : Visible(value)[nestedObject]!.AsObject();
            target["stateVersion"] = 3;
            Resign(value);
        });
        var indented = Encoding.UTF8.GetBytes(JsonNode.Parse(golden)!.ToJsonString(
            new JsonSerializerOptions { WriteIndented = true }));
        Assert.Contains("non-canonical", Resigned(indented));
        AssertOnly("malformed-json", CampaignCombatAudienceDraftVerifier.VerifyDecision(golden[..^1]));
    }

    // Re-derives digest and refs over an indented decision so only the canonical-form check can fail.
    private static IReadOnlyList<string> Resigned(byte[] indented)
    {
        var text = Encoding.UTF8.GetString(indented);
        using var document = JsonDocument.Parse(indented);
        var root = document.RootElement;
        var visibleRaw = root.GetProperty("visible").GetRawText();
        var digest = CampaignCombatAudienceDecisions.Digest(Encoding.UTF8.GetBytes(visibleRaw));
        text = text.Replace(root.GetProperty("visibleDigest").GetString()!, digest, StringComparison.Ordinal);
        var refs = root.GetProperty("offerRefs").EnumerateArray().Select(value => value.GetString()!).ToArray();
        for (var index = 0; index < refs.Length; index++)
            text = text.Replace(refs[index], Ref(root, digest, index), StringComparison.Ordinal);
        return CampaignCombatAudienceDraftVerifier.VerifyDecision(Encoding.UTF8.GetBytes(text));
    }

    private static void AssertCaught(byte[] golden, string expected, Action<JsonObject> tamper)
    {
        var node = JsonNode.Parse(golden)!.AsObject();
        tamper(node);
        var bytes = Encoding.UTF8.GetBytes(node.ToJsonString());
        Assert.NotEqual(golden, bytes);
        var failures = CampaignCombatAudienceDraftVerifier.VerifyDecision(bytes);
        Assert.True(failures.Contains(expected), $"expected {expected}; got [{string.Join(", ", failures)}]");
    }

    private static void AssertOnly(string expected, IReadOnlyList<string> failures) =>
        Assert.Equal(expected, Assert.Single(failures));

    private static JsonObject Visible(JsonObject decision) => decision["visible"]!.AsObject();

    // Consistent re-signing: recompute digest and refs so deeper checks, not the fence, must catch it.
    private static void Resign(JsonObject decision, int? refCount = null)
    {
        var digest = CampaignCombatAudienceDecisions.Digest(Encoding.UTF8.GetBytes(Visible(decision).ToJsonString()));
        decision["visibleDigest"] = digest;
        using var document = JsonDocument.Parse(decision.ToJsonString());
        var count = refCount ?? decision["offerRefs"]!.AsArray().Count;
        decision["offerRefs"] = new JsonArray(Enumerable.Range(0, count)
            .Select(index => (JsonNode?)JsonValue.Create(Ref(document.RootElement, digest, index))).ToArray());
    }

    private static string Ref(JsonElement decision, string digest, int ordinal)
    {
        var audience = decision.GetProperty("audience").GetString() == "commonwealth"
            ? CampaignActionAudience.Commonwealth : CampaignActionAudience.Axis;
        return CampaignCombatAudienceDecisions.Reference(decision.GetProperty("campaignId").GetString()!,
            audience, decision.GetProperty("stage").GetString()!,
            decision.GetProperty("actingElementId").GetString()!, digest, ordinal);
    }

    private static CampaignCombatAudienceSubmission Mutate(CampaignCombatAudienceSubmission valid,
        JsonElement vector, ExerciseSession session, CampaignCombatAudienceDecision forceOther)
    {
        var field = vector.GetProperty("field").GetString();
        if (vector.TryGetProperty("derive", out var derive))
        {
            return derive.GetString() switch
            {
                "increment" => valid with { AudienceRevision = valid.AudienceRevision + 1 },
                "private-action-id-ordinal-0" => valid with
                {
                    OfferRef = CampaignHostAdapter.QueryContactCombatOwnerDraft(session,
                        valid.Audience, valid.ActingElementId).PrivateCombatActionIds[0],
                },
                "other-owner-ref" => valid with { OfferRef = forceOther.OfferRefs[0] },
                "rba-private-decline-action-id" => valid with
                {
                    OfferRef = CampaignExercises.QueryContactRbaCut(session).DeclineOffer.ActionId,
                },
                "rba-private-move-action-id" => valid with
                {
                    OfferRef = CampaignExercises.QueryContactRbaCut(session).MoveOffers[0].ActionId,
                },
                "custody-victim-element" => valid with
                {
                    ActingElementId = CampaignExercises.QueryNativeCombatCustodyCut(session)!.VictimElementId,
                },
                "other-owner-digest" => valid with { VisibleDigest = forceOther.VisibleDigest },
                var other => throw new InvalidOperationException($"Unknown derivation {other}."),
            };
        }
        var value = vector.GetProperty("value");
        return field switch
        {
            "offerRef" => valid with { OfferRef = value.GetString()! },
            "audience" => valid with
            {
                Audience = value.GetString() switch
                {
                    "axis" => CampaignActionAudience.Axis,
                    "commonwealth" => CampaignActionAudience.Commonwealth,
                    "system" => CampaignActionAudience.System,
                    var other => throw new InvalidOperationException($"Unknown audience {other}."),
                },
            },
            "actingElementId" => valid with { ActingElementId = value.GetString()! },
            "audienceRevision" => valid with { AudienceRevision = value.GetInt64() },
            "visibleDigest" => valid with { VisibleDigest = value.GetString()! },
            "stage" => valid with { Stage = value.GetString()! },
            "campaignId" => valid with { CampaignId = value.GetString()! },
            "contractVersion" => valid with { ContractVersion = value.GetInt32() },
            _ => throw new InvalidOperationException($"Unknown vector field {field}."),
        };
    }

    private static Dictionary<string, CampaignCombatAudienceDecision> LiveDecisions()
    {
        var selection = CombatCycleDisclosureForceTests.AdvanceToCombat("campaign-con005-draft-selection", 12345);
        var force = CombatCycleDisclosureForceTests.AdvanceToForce("campaign-con005-draft-force", 12345);
        var reserve = CombatCycleDisclosureReserveCycleTests.AtReserveRelease("campaign-con005-draft-reserve", 6);
        var reserveDecision = CampaignCombatAudienceDecisions.QueryReserveRelease(reserve,
            CampaignActionAudience.Axis, "axis-infantry-second")!;
        var cycle = CombatCycleDisclosureReserveCycleTests.Complete(
            CampaignCombatAudienceDecisions.Submit(reserve, reserveDecision.Choose(0)).TrustedStep!.SuccessorSession!);
        var rba = CombatCycleDisclosureRbaDeclineTests.AtRba("campaign-con005-draft-rba", 12345);
        var custody = CombatCycleDisclosureCustodyTests.AtCustody("campaign-con005-draft-custody", 5, out _);
        return new(StringComparer.Ordinal)
        {
            ["custody-axis"] = CampaignCombatAudienceDecisions.QueryCustody(custody,
                CampaignActionAudience.Axis, "axis-infantry")!,
            ["rba-decline-commonwealth"] = CampaignCombatAudienceDecisions.QueryRbaDecline(rba,
                CampaignActionAudience.Commonwealth, "commonwealth-infantry")!,
            ["selection-axis-first-cut"] = CampaignCombatAudienceDecisions.QuerySelection(selection,
                CampaignActionAudience.Axis, "axis-infantry")!,
            ["force-assignment-axis"] = CampaignCombatAudienceDecisions.QueryForceAssignment(force,
                CampaignActionAudience.Axis, "axis-infantry")!,
            ["force-assignment-commonwealth"] = CampaignCombatAudienceDecisions.QueryForceAssignment(force,
                CampaignActionAudience.Commonwealth, "commonwealth-infantry")!,
            ["reserve-release-axis"] = reserveDecision,
            ["cycle-control-axis"] = CampaignCombatAudienceDecisions.QueryCycleControl(cycle,
                CampaignActionAudience.Axis)!,
        };
    }

    private static JsonDocument Fixture() => JsonDocument.Parse(CombatActualPaidAssaultTests.Dependency(FixturePath));

    private static JsonElement Golden(JsonDocument fixture, string id) =>
        fixture.RootElement.GetProperty("goldens").EnumerateArray()
            .Single(value => value.GetProperty("id").GetString() == id);

    private static byte[] GoldenBytes(JsonElement golden) =>
        Encoding.UTF8.GetBytes(golden.GetProperty("decisionUtf8").GetString()!);

    private static string[] Strings(JsonElement array) =>
        array.EnumerateArray().Select(value => value.GetString()!).ToArray();
}
