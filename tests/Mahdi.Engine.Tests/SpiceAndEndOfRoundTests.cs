using Mahdi.Engine.Commands;
using Mahdi.Engine.Guidance;
using Mahdi.Engine.Model;

namespace Mahdi.Engine.Tests;

public class SpiceAndEndOfRoundTests
{
    private static GuidanceBuilder Builder => new(TestGame.Content);

    [Test]
    public void ImperiumMarkerCanBeCorrectedBeforeSpending()
    {
        var game = InSpiceHarvesting();

        game.Execute(new SetImperiumMarker(ImperiumPower.SpacingGuild, 4));
        Assert.That(game.State.Imperium[ImperiumPower.SpacingGuild], Is.EqualTo(4));

        // 5 spice: keep the Guild (lowest) and CHOAM, then raise the Guild with the last 1. Landsraad drops.
        game.Execute(new HarvestSpice(DesertHarvesters: 5, DeepDesertHarvesters: 0));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.State.Imperium[ImperiumPower.SpacingGuild], Is.EqualTo(5));
            Assert.That(game.State.Imperium[ImperiumPower.Choam], Is.EqualTo(5));
            Assert.That(game.State.Imperium[ImperiumPower.Landsraad], Is.EqualTo(4));
            Assert.That(game.State.ActiveBans, Is.EquivalentTo(new[] { ImperiumPower.Landsraad }));
        }
    }

    [Test]
    public void CorrectingAMarkerDoesNotActivateABan()
    {
        var game = InSpiceHarvesting();

        game.Execute(new SetImperiumMarker(ImperiumPower.Choam, TestGame.Content.BottomStep));

        Assert.That(game.State.ActiveBans, Is.Empty);
        game.Undo();
        Assert.That(game.State.Imperium[ImperiumPower.Choam], Is.EqualTo(TestGame.Content.TopStep));
    }

    [Test]
    public void MarkerCorrectionIsRejectedOutsideSpiceHarvestingOrOffTheBoard()
    {
        var game = TestGame.InActionResolution();
        Assert.Throws<CommandRejectedException>(() => game.Execute(new SetImperiumMarker(ImperiumPower.Choam, 4)));

        game = InSpiceHarvesting();
        using (Assert.EnterMultipleScope())
        {
            Assert.Throws<CommandRejectedException>(() => game.Execute(new SetImperiumMarker(ImperiumPower.Choam, 0)));
            Assert.Throws<CommandRejectedException>(() => game.Execute(new SetImperiumMarker(ImperiumPower.Choam, 6)));
            Assert.Throws<CommandRejectedException>(() => game.Execute(new SetImperiumMarker(ImperiumPower.Choam, TestGame.Content.TopStep)));
        }
    }

    [Test]
    public void SpiceSummaryNamesDroppedMarkersBansAndTheReserve()
    {
        var game = InSpiceHarvesting();
        game.Execute(new HarvestSpice(DesertHarvesters: 5, DeepDesertHarvesters: 0));

        var summary = GuidanceBuilder.SpiceSummary(game.State.LastSpiceResult!);

        Assert.That(summary, Is.EqualTo("Spice: Landsraad dropped · Landsraad Ban active · Reserve 1"));
    }

    [Test]
    public void SpiceSummaryMentionsTheSupremacyPointAndKeptMarkers()
    {
        var game = InSpiceHarvesting();
        game.Execute(new HarvestSpice(DesertHarvesters: 1, DeepDesertHarvesters: 4));

        Assert.That(GuidanceBuilder.SpiceSummary(game.State.LastSpiceResult!), Is.EqualTo("Spice: all markers kept · Supremacy +1"));
    }

    [Test]
    public void SpiceSummaryNamesRaisedMarkersAndMarkersOnTheBottomStep()
    {
        var game = InSpiceHarvesting();
        game.Execute(new SetImperiumMarker(ImperiumPower.Choam, 1));
        game.Execute(new SetImperiumMarker(ImperiumPower.SpacingGuild, 3));

        // 3 spice: keep CHOAM (lowest), then raise it with the last 1. The Guild drops; the Landsraad drops too.
        game.Execute(new HarvestSpice(DesertHarvesters: 3, DeepDesertHarvesters: 0));
        var result = game.State.LastSpiceResult!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(GuidanceBuilder.SpiceSummary(result), Does.StartWith("Spice: CHOAM raised · Spacing Guild dropped · Landsraad dropped"));
            Assert.That(GuidanceBuilder.SpendLabel(result.Markers[0]), Is.EqualTo("raise (3)"));
            Assert.That(GuidanceBuilder.BansAfter(result), Is.EquivalentTo(new[] { ImperiumPower.SpacingGuild, ImperiumPower.Landsraad }));
        }
    }

    [Test]
    public void SpendLabelsCoverEveryChange()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(GuidanceBuilder.SpendLabel(new(ImperiumPower.Choam, 5, 5, 2, MarkerChange.Kept)), Is.EqualTo("keep (2)"));
            Assert.That(GuidanceBuilder.SpendLabel(new(ImperiumPower.Choam, 5, 4, 0, MarkerChange.Dropped)), Is.EqualTo("drops"));
            Assert.That(GuidanceBuilder.SpendLabel(new(ImperiumPower.Choam, 1, 1, 0, MarkerChange.StayedAtBottom)), Is.EqualTo("stays on the bottom step"));
            Assert.That(GuidanceBuilder.SpiceSummary(new(0, 0, [new(ImperiumPower.Choam, 1, 1, 0, MarkerChange.StayedAtBottom)], false, 0, 0)),
                Is.EqualTo("Spice: CHOAM on the bottom step · CHOAM Ban active"));
        }
    }

    [Test]
    public void SpiceRulesLeaveOutWhatSoloReplaces()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(GuidanceBuilder.SpiceHarvestingRules, Has.Length.EqualTo(7));
            Assert.That(GuidanceBuilder.SpiceHarvestingRules, Has.None.Contains("Stockpiling"));
            Assert.That(GuidanceBuilder.SpiceHarvestingRules, Has.Some.Contains("lowest first"));
            Assert.That(GuidanceBuilder.SpiceHarvestingRules, Has.Some.Contains("all of them, FAQ"));
        }
    }

    [Test]
    public void EndOfRoundRemindsToAdvanceSupremacyAndNamesTheLeaderEntering()
    {
        var game = InSpiceHarvesting();
        game.Execute(new HarvestSpice(0, 0));

        var guidance = Builder.EndOfRound(game.State);
        var important = guidance.Steps.Where(s => s.Important).ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(guidance.Title, Is.EqualTo("End of Round 1"));
            Assert.That(important, Has.Count.EqualTo(1));
            Assert.That(important[0].Text, Does.Contain("Supremacy marker 1 step on the board: from 0 to 1"));
            Assert.That(guidance.Steps.Select(s => s.Text), Has.Some.Contains("Thufir Hawat enters play"));
            Assert.That(guidance.Steps.Select(s => s.Text), Has.Some.Contains("Harkonnen Named Leaders stay"));
        }
    }

    [TestCase(3, "Bene Gesserit token")]
    [TestCase(5, "Beast Rabban is removed from the game")]
    [TestCase(9, "the Harkonnens win the game")]
    public void EndOfRoundNamesWhatTheNextSupremacyStepBrings(int supremacy, string expected)
    {
        var game = InSpiceHarvesting();
        game.Execute(new HarvestSpice(0, 0));
        var state = game.State with { Supremacy = supremacy };

        Assert.That(Builder.EndOfRound(state).Steps.Select(s => s.Text), Has.Some.Contains(expected));
    }

    private static Game InSpiceHarvesting()
    {
        var game = TestGame.InActionResolution();
        TestGame.SpendAllDice(game);
        game.Execute(new ConfirmDesertHazards());
        return game;
    }
}
