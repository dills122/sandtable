using System.Text.Json;
using Cna.Core.Actions;
using Cna.OrleansHost.Api;

namespace Cna.OrleansHost.Tests;

public sealed class TemporaryPlayerViewProjectionTests
{
    private const string Campaign = "host-rsh-001";
    private const string Rules =
        "17f3e6047f34b5bf6f5f809055863b664a4bd82a8481db83ee83e0ae5cad3a2a";
    private const string Pin = Rules + "|breakdown-truck-lab|" +
        "sha256:e6631e81ad8f97e39fd9d7eec93bad7fe2b39db4d2d3059ed94a02dd4093e7a3|" +
        "sha256:646e76e69ecceb82216b37d84e950928099acd8a3cb04b51526d0fe631e512ee";
    private const string Generation = "11111111-1111-4111-8111-111111111111";
    private static readonly string[] LocationIds =
    [
        "center", "east", "north", "north-east", "north-west", "south",
        "south-east", "south-west", "west",
    ];

    [Fact]
    public void InitialPinnedCutProjectsExactSafeFieldsAndStableScopedIdentities()
    {
        var projector = new TemporaryPlayerViewProjection();
        var axis = Seat("axis", Generation);
        var cut = Cut(CampaignActionAudience.Axis);
        var first = JsonSerializer.Serialize(projector.Map(cut, axis));
        var repeated = JsonSerializer.Serialize(projector.Map(cut, axis));
        Assert.Equal(first, repeated);
        using var document = JsonDocument.Parse(first);
        var root = document.RootElement;
        Assert.Equal(1, root.GetProperty("contractVersion").GetInt32());
        Assert.Equal("axis", root.GetProperty("seat").GetString());
        Assert.Equal("1", root.GetProperty("audienceRevision").GetString());
        Assert.Equal("0", root.GetProperty("nextDiaryCursor").GetString());
        Assert.Empty(root.GetProperty("warDiary").EnumerateArray());
        Assert.Equal(9, root.GetProperty("locations").GetArrayLength());
        Assert.Single(root.GetProperty("ownFormations").EnumerateArray());
        Assert.Single(root.GetProperty("formationLedger").EnumerateArray());
        var formation = root.GetProperty("ownFormations")[0].GetProperty("formationId").GetString();
        Assert.Equal(formation,
            root.GetProperty("legalActions")[0].GetProperty("formationId").GetString());
        Assert.NotEqual("axis-infantry.formation", formation);
        Assert.DoesNotContain("private-representation", first);
        Assert.DoesNotContain("axis-infantry.formation", first);
        Assert.DoesNotContain("axis-infantry\"", first);
        Assert.DoesNotContain("core-action", first);
        Assert.DoesNotContain("stateVersion", first);
        Assert.DoesNotContain("sourcePin", first);
        Assert.DoesNotContain("reserveStatus", first);
        Assert.DoesNotContain("terrainId", first);
        Assert.NotEqual(formation, JsonDocument.Parse(JsonSerializer.Serialize(
            projector.Map(cut, Seat("axis", "22222222-2222-4222-8222-222222222222"))))
            .RootElement.GetProperty("ownFormations")[0]
            .GetProperty("formationId").GetString());
    }

    [Fact]
    public void MissingSourcesWrongSeatUnsupportedActionAndSuccessorFailClosed()
    {
        var projector = new TemporaryPlayerViewProjection();
        var cut = Cut(CampaignActionAudience.Axis);
        var axis = Seat("axis", Generation);
        Assert.Null(projector.Map(cut, Seat("commonwealth", Generation)));
        Assert.Null(projector.Map(cut, axis with { CampaignId = "other" }));
        Assert.Null(projector.Map(cut, axis with { Generation = "invalid" }));
        Assert.Null(projector.Map(cut with { SourcePin = "other" }, axis));
        Assert.Null(projector.Map(cut with { StateVersion = 2 }, axis));
        Assert.Null(projector.Map(cut with { DecisionStateKind = "reacting" }, axis));
        Assert.Null(projector.Map(cut with
        {
            Actions =
            [new TemporaryPrivateAction(Guid.NewGuid().ToString(), "core-secret",
                "unsupported-kind", null, null, null)]
        }, axis));
        Assert.Null(projector.Map(cut with { Locations = cut.Locations[..^1] }, axis));
        Assert.Null(projector.Map(cut with
        {
            OwnElements =
            [new TemporaryPrivateOwnElement("hidden-opponent", "commonwealth-truck.formation",
                "east", "None")]
        }, axis));
    }

    [Fact]
    public void PinnedEarlySuccessorProjectsOnlyCoherentOwnDiaryAndActionWindows()
    {
        var projector = new TemporaryPlayerViewProjection();
        var seat = Seat("axis", Generation);
        var initiativeEntry = new TemporaryPrivateDiaryEntry(Guid.NewGuid().ToString("D"), 1,
            "Initiative order recorded", "Your choice of acting order was recorded.", null);
        var initiative = Cut(CampaignActionAudience.Axis) with
        {
            StateVersion = 4,
            PositionId = "land.position.operation-1.initiative-declaration",
            PhaseId = "land.phase.initiative-declaration",
            StageId = "land.stage.operation",
            Actions =
            [
                new TemporaryPrivateAction(Guid.NewGuid().ToString("D"), "core-first",
                    "act-first", null, null, null),
                new TemporaryPrivateAction(Guid.NewGuid().ToString("D"), "core-last",
                    "act-last", null, null, null),
            ],
        };
        using var first = JsonDocument.Parse(JsonSerializer.Serialize(
            projector.Map(initiative, seat)));
        Assert.Equal(2, first.RootElement.GetProperty("legalActions").GetArrayLength());
        Assert.Equal("human", first.RootElement.GetProperty("position")
            .GetProperty("decision").GetString());
        Assert.Empty(first.RootElement.GetProperty("warDiary").EnumerateArray());

        var weather = initiative with
        {
            StateVersion = 5,
            PositionId = "land.position.operation-1.weather-determination",
            PhaseId = "land.phase.weather-determination",
            Actions = [],
            Diary = [initiativeEntry],
            NextDiaryCursor = 1,
        };
        var json = JsonSerializer.Serialize(projector.Map(weather, seat));
        using var view = JsonDocument.Parse(json);
        Assert.Equal("1", view.RootElement.GetProperty("nextDiaryCursor").GetString());
        Assert.Single(view.RootElement.GetProperty("warDiary").EnumerateArray());
        Assert.Equal("Initiative order recorded", view.RootElement
            .GetProperty("warDiary")[0].GetProperty("title").GetString());
        Assert.Empty(view.RootElement.GetProperty("legalActions").EnumerateArray());
        Assert.DoesNotContain("core-first", json);
        Assert.DoesNotContain(Pin, json);
        Assert.DoesNotContain("axis-infantry.formation", json);
        Assert.DoesNotContain("private-representation", json);
        Assert.Null(projector.Map(weather with { SourcePin = "other" }, seat));
        Assert.Null(projector.Map(weather with { PositionId = "other" }, seat));
        Assert.Null(projector.Map(weather with { PhaseId = "other" }, seat));
        Assert.Null(projector.Map(weather with { StateVersion = 11 }, seat));
        Assert.Null(projector.Map(weather with { NextDiaryCursor = 2 }, seat));
        Assert.Null(projector.Map(weather with
        {
            Diary = [initiativeEntry with { Body = "private destination" }],
        }, seat));
        Assert.Null(projector.Map(weather with
        {
            Diary = [initiativeEntry with { FormationId = "axis-infantry.formation" }],
        }, seat));
        Assert.Null(projector.Map(weather with
        {
            Actions = initiative.Actions,
        }, seat));
        Assert.Null(projector.Map(weather with
        {
            Contacts = [weather.Contacts[0], weather.Contacts[0]],
        }, seat));
        Assert.Null(projector.Map(weather, Seat("commonwealth", Generation)));
    }

    [Fact]
    public void ApprovedDiaryCatalogFreezesExactTextAndFormationPolicy()
    {
        (string Key, string Title, string Body, bool OwnFormationLink)[] cases =
        [
            ("diary.initiativeOrder", "Initiative order recorded",
                "Your choice of acting order was recorded.", false),
            ("diary.reserveDesignated", "Reserve assigned",
                "A formation you control was assigned to reserve.", true),
            ("diary.reserveCompleted", "Reserve choices complete",
                "Your reserve choices were completed.", false),
            ("diary.elementMoved", "Formation moved",
                "A formation you control moved.", true),
            ("diary.movementStopped", "Movement stopped",
                "Movement for one of your formations stopped.", false),
            ("diary.reactionMoved", "Reaction move recorded",
                "A formation you control made a reaction move.", true),
            ("diary.reactionCompleted", "Reaction complete",
                "Your reaction opportunity was completed.", false),
            ("diary.reactionDeclined", "Reaction declined",
                "You declined the reaction opportunity.", false),
            ("diary.movementCompleted", "Movement complete",
                "Your movement segment was completed.", false),
        ];
        Assert.Equal(9, cases.Length);
        foreach (var item in cases)
        {
            var copy = TemporaryPlayerViewProjection.LookupDiaryCopy(Pin,
                TemporaryPlayerViewProjection.DiaryCatalogVersion, "en-CA", item.Key);
            Assert.NotNull(copy);
            Assert.Equal(item.Title, copy.Title);
            Assert.Equal(item.Body, copy.Body);
            Assert.Equal(item.OwnFormationLink, copy.OwnFormationLink);
            Assert.InRange(copy.Title.Length, 1, 80);
            Assert.InRange(copy.Body.Length, 1, 160);
        }
    }

    [Fact]
    public void DiaryCatalogRejectsUnknownSourceVersionLocaleAndKey()
    {
        const string key = "diary.initiativeOrder";
        Assert.Null(TemporaryPlayerViewProjection.LookupDiaryCopy("other",
            TemporaryPlayerViewProjection.DiaryCatalogVersion, "en-CA", key));
        Assert.Null(TemporaryPlayerViewProjection.LookupDiaryCopy(Pin,
            TemporaryPlayerViewProjection.CatalogVersion, "en-CA", key));
        Assert.Null(TemporaryPlayerViewProjection.LookupDiaryCopy(Pin,
            TemporaryPlayerViewProjection.DiaryCatalogVersion, "fr-CA", key));
        Assert.Null(TemporaryPlayerViewProjection.LookupDiaryCopy(Pin,
            TemporaryPlayerViewProjection.DiaryCatalogVersion, "en-CA", "diary.unapproved"));
        Assert.Null(TemporaryPlayerViewProjection.LookupDiaryCopy(Pin,
            TemporaryPlayerViewProjection.DiaryCatalogVersion, "en-CA", key + " "));
    }

    private static LocalSession Seat(string side, string generation) =>
        new("cookie", "csrf", "operator", generation, Campaign, side);

    private static TemporaryPrivateAudienceCut Cut(CampaignActionAudience side) =>
        new(Campaign, 1, Rules, "breakdown-truck-lab", "position", 1,
            "phase", "stage", side, 1, Guid.NewGuid().ToString(),
            LocationIds.Select(id => new TemporaryPrivateLocation(id, "terrain")).ToArray(),
            [new TemporaryPrivateOwnElement("axis-infantry", "axis-infantry.formation",
                "north-west", "None")],
            [new TemporaryPrivateContact(Guid.NewGuid().ToString(),
                "private-representation", "south-east", false)],
            [new TemporaryPrivateAction(Guid.NewGuid().ToString(), "core-action",
                "designate-reserve", "axis-infantry", null, null)], Pin, "normal");
}
