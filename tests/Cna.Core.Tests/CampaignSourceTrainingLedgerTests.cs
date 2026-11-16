using Cna.Core.Campaigns;

namespace Cna.Core.Tests;

public sealed class CampaignSourceTrainingLedgerTests
{
    [Fact]
    public void AustralianScenarioDoesNotInventUnstatedTrainingProgress()
    {
        var ledger = CampaignSourceTrainingLedger.CreateSixthAustralian();
        Assert.Null(ledger.CurrentMorale);
        Assert.Null(ledger.QualifyingStages);
        Assert.Throws<InvalidOperationException>(() => ledger.RecordStage(1, true, true, false, true, true, true, true));
        Assert.Throws<InvalidOperationException>(() => ledger.EstablishStartingState(-3, 6, "ruling"));
        Assert.Equal(ledger.Hash(), CampaignSourceTrainingLedger.CreateSixthAustralian().Hash());
    }

    [Fact]
    public void SixQualifyingStagesRaiseMoraleAndInterruptionDoesNotCount()
    {
        var initial = CampaignSourceTrainingLedger.CreateSixthAustralian().EstablishStartingState(-3, 2, "explicit scenario ruling");
        var current = initial;
        current = current.RecordStage(1, true, true, false, true, true, true, true);
        current = current.RecordStage(2, true, true, true, true, true, true, true);
        current = current.RecordStage(3, true, true, false, true, true, true, false);
        Assert.Equal(3, current.QualifyingStages);
        for (var stage = 4; stage <= 6; stage++)
            current = current.RecordStage(stage, true, true, false, true, true, true, true);
        Assert.Equal(-2, current.CurrentMorale);
        Assert.Equal(0, current.QualifyingStages);
        Assert.Equal(-3, initial.CurrentMorale);
        Assert.Throws<InvalidOperationException>(() => current.RecordStage(6, true, true, false, true, true, true, true));
        Assert.Throws<InvalidOperationException>(() => current.EstablishStartingState(-3, 0, "other"));
    }
}
