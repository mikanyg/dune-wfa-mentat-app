using Mahdi.Engine.Commands;
using Mahdi.Engine.Guidance;
using Mahdi.Engine.Model;
using Mahdi.Engine.Persistence;
using Mahdi.Engine.Rules;

namespace Mahdi.Engine.Tests;

/// <summary>Player-driven mode: the player draws the Tactical cards and enters them.</summary>
public class PlayerDrivenTests
{
    private static GameContent Content => TestGame.Content;

    private static Game AtRoundStart()
    {
        var game = TestGame.New(DiceMode.Physical);
        game.Execute(new CompleteSetup());
        return game;
    }

    [Test]
    public void TheRoundStartsWithoutCardsUntilThePlayerEntersThem()
    {
        var game = AtRoundStart();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.State.Phase, Is.EqualTo(Phase.RoundStart));
            Assert.That(game.State.Tactical.HarvestCardId, Is.Null);
            Assert.That(TacticalDeck.AwaitingHarvest(game.State), Is.True);
            Assert.That(game.State.Reinforcements, Is.EqualTo(2));
            Assert.Throws<CommandRejectedException>(() => game.Execute(new ConfirmRoundStart()));
            Assert.Throws<CommandRejectedException>(() => game.Execute(new EnterTargetCard("gara-kulon")));
        }
    }

    [Test]
    public void EnteredCardsBecomeTheHarvestingSectorAndTarget()
    {
        var game = AtRoundStart();

        game.Execute(new EnterHarvestCard("habbanya-ridge"));
        Assert.That(TacticalDeck.AwaitingTarget(Content, game.State), Is.True);
        game.Execute(new EnterTargetCard("gara-kulon"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.State.Tactical.HarvestCardId, Is.EqualTo("habbanya-ridge"));
            Assert.That(game.State.Tactical.TargetCardId, Is.EqualTo("gara-kulon"));
            Assert.That(TacticalDeck.AwaitingTarget(Content, game.State), Is.False);
            Assert.That(game.State.Notices, Is.Empty);
        }

        game.Execute(new ConfirmRoundStart());
        Assert.That(game.State.Phase, Is.EqualTo(Phase.VehiclePlacement));
    }

    [Test]
    public void ATargetCardInTheHarvestingSectorIsDiscarded()
    {
        var game = AtRoundStart();
        game.Execute(new EnterHarvestCard("rock-outcroppings"));

        game.Execute(new EnterTargetCard("bight-of-the-cliff"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.State.Tactical.TargetCardId, Is.Null);
            Assert.That(game.State.Tactical.Discard, Is.EqualTo(new[] { "bight-of-the-cliff" }));
            Assert.That(TacticalDeck.AwaitingTarget(Content, game.State), Is.True);
        }

        // A discarded card or the Harvesting card cannot be entered again.
        using (Assert.EnterMultipleScope())
        {
            Assert.Throws<CommandRejectedException>(() => game.Execute(new EnterTargetCard("bight-of-the-cliff")));
            Assert.Throws<CommandRejectedException>(() => game.Execute(new EnterTargetCard("rock-outcroppings")));
        }

        game.Execute(new EnterTargetCard("tasmin-sink"));
        Assert.That(Builder().RoundStart(game.State).Notes, Has.Some.Contains("Bight-of-the-Cliff"));
    }

    [Test]
    public void EnteringTheHarvestingCardAgainStartsTheDrawOver()
    {
        var game = AtRoundStart();
        game.Execute(new EnterHarvestCard("rock-outcroppings"));
        game.Execute(new EnterTargetCard("bight-of-the-cliff"));
        game.Execute(new EnterTargetCard("tasmin-sink"));

        game.Execute(new EnterHarvestCard("windgap"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.State.Tactical.HarvestCardId, Is.EqualTo("windgap"));
            Assert.That(game.State.Tactical.TargetCardId, Is.Null);
            Assert.That(game.State.Tactical.Discard, Is.Empty);
        }
    }

    [Test]
    public void ADestroyedTargetIsDiscardedAndThePlayerDrawsANewOne()
    {
        var game = TestGame.InActionResolution(DiceMode.Physical);
        var target = game.State.Tactical.TargetCardId!;

        game.Execute(new DestroySietch(target, 2));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.State.Supremacy, Is.EqualTo(2));
            Assert.That(game.State.Tactical.TargetCardId, Is.Null);
            Assert.That(game.State.Tactical.Discard, Does.Contain(target));
            Assert.That(TacticalDeck.AwaitingTarget(Content, game.State), Is.True);
            Assert.Throws<CommandRejectedException>(() => game.Execute(new EnterHarkonnenDie(DieFace.House)));
        }

        var tactical = game.State.Tactical;
        var next = TacticalDeck.Remaining(Content, tactical)
            .First(id => TacticalDeck.IsEligibleTarget(Content, tactical, id, Content.Card(tactical.HarvestCardId!).Sector));
        game.Execute(new EnterTargetCard(next));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.State.Tactical.TargetCardId, Is.EqualTo(next));
            Assert.That(game.State.Notices.Select(n => n.Text), Has.Some.EqualTo($"New Target Sietch: {Content.Card(next).Sietch}."));
        }

        game.Execute(new EnterHarkonnenDie(DieFace.House));
        Assert.That(game.State.Dice.Pending, Is.Not.Null);
    }

    [Test]
    public void WithNoEligibleCardLeftTheRoundGoesOnWithoutATarget()
    {
        // Harvest Central (Windgap): Hobars Gap shares the Sector. Destroy every Outer Sietch but Habbanya Ridge,
        // make it the Target and destroy it too: no eligible card is left.
        var game = AtRoundStart();
        game.Execute(new EnterHarvestCard("windgap"));
        game.Execute(new EnterTargetCard("hobars-gap")); // same Sector: discarded
        game.Execute(new DestroySietch("rock-outcroppings", 1));
        game.Execute(new DestroySietch("bight-of-the-cliff", 1));
        game.Execute(new DestroySietch("sihaya-ridge", 1));
        game.Execute(new DestroySietch("gara-kulon", 1));
        game.Execute(new DestroySietch("tasmin-sink", 1));
        game.Execute(new EnterTargetCard("habbanya-ridge"));

        game.Execute(new DestroySietch("habbanya-ridge", 1));

        // Every remaining card is ineligible: Hobars Gap shares the Sector, the rest are destroyed.
        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.State.Tactical.TargetCardId, Is.Null);
            Assert.That(TacticalDeck.AwaitingTarget(Content, game.State), Is.False);
            Assert.That(game.State.Notices.Select(n => n.Text), Has.Some.Contains("No eligible Target Sietch"));
        }
    }

    [Test]
    public void SwitchingToAppDrivenDrawsTheMissingCards()
    {
        var game = AtRoundStart();

        game.Execute(new ChangeDiceMode(DiceMode.AppRolls));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.State.Tactical.HarvestCardId, Is.Not.Null);
            Assert.That(game.State.Tactical.TargetCardId, Is.Not.Null);
            Assert.That(Content.Card(game.State.Tactical.TargetCardId!).Sector,
                Is.Not.EqualTo(Content.Card(game.State.Tactical.HarvestCardId!).Sector));
        }

        game.Execute(new ConfirmRoundStart());
        Assert.That(game.State.Phase, Is.EqualTo(Phase.VehiclePlacement));
    }

    [Test]
    public void AppDrivenGamesRejectEnteredCards()
    {
        var game = TestGame.New(DiceMode.AppRolls);
        game.Execute(new CompleteSetup());

        using (Assert.EnterMultipleScope())
        {
            Assert.Throws<CommandRejectedException>(() => game.Execute(new EnterHarvestCard("windgap")));
            Assert.Throws<CommandRejectedException>(() => game.Execute(new EnterTargetCard("windgap")));
        }
    }

    [Test]
    public void EndOfRoundAsksThePlayerToShuffleTheCards()
    {
        var game = TestGame.InActionResolution(DiceMode.Physical);
        TestGame.SpendAllDice(game);
        game.Execute(new ConfirmDesertHazards());
        game.Execute(new HarvestSpice(0, 0));

        Assert.That(Builder().EndOfRound(game.State).Steps.Select(s => s.Text),
            Has.Some.EqualTo("Shuffle all 8 Tactical cards for the next round."));

        game.Execute(new EndRound());
        Assert.That(TacticalDeck.AwaitingHarvest(game.State), Is.True);
    }

    [Test]
    public void EnteredCardsSurviveSaveAndReplay()
    {
        var game = AtRoundStart();
        game.Execute(new EnterHarvestCard("rock-outcroppings"));
        game.Execute(new EnterTargetCard("bight-of-the-cliff"));
        game.Execute(new EnterTargetCard("tasmin-sink"));

        var restored = Game.FromSave(Content, SaveData.FromJson(game.ToSave().ToJson()));

        Assert.That(restored.State.Tactical, Is.EqualTo(game.State.Tactical).Using<TacticalState>(SameTactical));
    }

    private static bool SameTactical(TacticalState a, TacticalState b) =>
        a.HarvestCardId == b.HarvestCardId && a.TargetCardId == b.TargetCardId && a.Discard.SequenceEqual(b.Discard);

    private static GuidanceBuilder Builder() => new(Content);
}
