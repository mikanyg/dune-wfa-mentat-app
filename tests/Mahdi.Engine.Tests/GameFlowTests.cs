using Mahdi.Engine.Commands;
using Mahdi.Engine.Events;
using Mahdi.Engine.Model;
using Mahdi.Engine.Persistence;

namespace Mahdi.Engine.Tests;

public class GameFlowTests
{
    [Test]
    public void SetupStartsRoundOneWithTacticalCardsAndReinforcements()
    {
        var game = TestGame.New();

        game.Execute(new CompleteSetup());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.State.Phase, Is.EqualTo(Phase.RoundStart));
            Assert.That(game.State.Round, Is.EqualTo(1));
            Assert.That(game.State.Tactical.HarvestCardId, Is.Not.Null);
            Assert.That(game.State.Tactical.TargetCardId, Is.Not.Null);
            Assert.That(game.State.Reinforcements, Is.EqualTo(2));
        }
    }

    [Test]
    public void PhasesFollowTheRoundSequence()
    {
        var game = TestGame.InActionResolution();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.State.Phase, Is.EqualTo(Phase.ActionResolution));
            Assert.That(game.State.Dice.Unused, Is.EqualTo(8));
        }

        TestGame.SpendAllDice(game);
        Assert.That(game.State.Phase, Is.EqualTo(Phase.DesertHazards));

        game.Execute(new ConfirmDesertHazards());
        Assert.That(game.State.Phase, Is.EqualTo(Phase.SpiceHarvesting));

        game.Execute(new HarvestSpice(1, 1));
        Assert.That(game.State.Phase, Is.EqualTo(Phase.EndOfRound));

        game.Execute(new EndRound());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.State.Phase, Is.EqualTo(Phase.RoundStart));
            Assert.That(game.State.Round, Is.EqualTo(2));
            Assert.That(game.State.Supremacy, Is.EqualTo(1));
        }
    }

    [Test]
    public void ActionResolutionEndsWhenTheLastHarkonnenDieIsSpent()
    {
        var game = TestGame.InActionResolution();

        for (var i = 0; i < 7; i++)
        {
            game.Execute(new RollHarkonnenDie());
            game.Execute(new CompleteHarkonnenTurn());
        }

        Assert.That(game.State.Phase, Is.EqualTo(Phase.ActionResolution));

        game.Execute(new RollHarkonnenDie());
        game.Execute(new CompleteHarkonnenTurn());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.State.Phase, Is.EqualTo(Phase.DesertHazards));
            Assert.That(game.State.Notices.Select(n => n.Text), Has.Some.Contains("Action Resolution is over"));
        }
    }

    [Test]
    public void NoFaceIsEverUsedMoreThanThreeTimes([Range(1, 25)] int seed)
    {
        var game = TestGame.InActionResolution(seed: seed);

        TestGame.SpendAllDice(game);

        var used = game.History.SelectMany(b => b).OfType<HarkonnenDieResolved>().GroupBy(e => e.Face);
        Assert.That(used.Select(g => g.Count()), Is.All.LessThanOrEqualTo(3));
    }

    [Test]
    public void PhysicalModeRejectsABlockedFace()
    {
        var game = TestGame.InActionResolution(DiceMode.Physical);
        for (var i = 0; i < 3; i++)
        {
            game.Execute(new EnterHarkonnenDie(DieFace.Strategy));
            game.Execute(new CompleteHarkonnenTurn());
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.TryExecute(new EnterHarkonnenDie(DieFace.Strategy), out var error), Is.False);
            Assert.That(error, Does.Contain("roll the die again"));
        }
    }

    [Test]
    public void PhysicalModeCannotUseTheVirtualRoll()
    {
        var game = TestGame.InActionResolution(DiceMode.Physical);

        Assert.Throws<CommandRejectedException>(() => game.Execute(new RollHarkonnenDie()));
    }

    [Test]
    public void DiceModeCanBeChangedMidGame()
    {
        var game = TestGame.InActionResolution(DiceMode.Physical);

        game.Execute(new ChangeDiceMode(DiceMode.AppRolls));
        game.Execute(new RollHarkonnenDie());

        Assert.That(game.State.Dice.Pending, Is.Not.Null);
    }

    [Test]
    public void TruthtranceForcesTheNextResult()
    {
        var game = TestGame.InActionResolution(DiceMode.Physical);

        game.Execute(new PlayTruthtrance(DieFace.Mentat));
        Assert.Throws<CommandRejectedException>(() => game.Execute(new EnterHarkonnenDie(DieFace.House)));
        game.Execute(new EnterHarkonnenDie(DieFace.Mentat));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.State.Dice.Pending!.Face, Is.EqualTo(DieFace.Mentat));
            Assert.That(game.State.Dice.Pending.Forced, Is.True);
            Assert.That(game.State.Dice.ForcedFace, Is.Null);
        }
    }

    [Test]
    public void BeneGesseritTokenGivesSupremacyWhenNoDieIsSetAside()
    {
        // All markers start on the top step, so no die is on The Spice Must Flow board.
        var game = TestGame.InActionResolution();
        var unused = game.State.Dice.Unused;

        game.Execute(new GainBeneGesserit());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.State.Supremacy, Is.EqualTo(1));
            Assert.That(game.State.Dice.Unused, Is.EqualTo(unused));
        }
    }

    [Test]
    public void BeneGesseritTokenAddsAnUnusedDieWhenOneIsSetAside()
    {
        var game = TestGame.InActionResolution();
        TestGame.SpendAllDice(game);
        game.Execute(new ConfirmDesertHazards());
        game.Execute(new HarvestSpice(0, 0));
        game.Execute(new EndRound());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.State.Dice.SetAside, Is.EqualTo(1));
            Assert.That(game.State.Dice.Unused, Is.EqualTo(7));
        }

        game.Execute(new GainBeneGesserit());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.State.Dice.SetAside, Is.Zero);
            Assert.That(game.State.Dice.Unused, Is.EqualTo(8));
        }
    }

    [Test]
    public void DroppedMarkersActivateTheirBans()
    {
        var game = TestGame.InActionResolution();
        TestGame.SpendAllDice(game);
        game.Execute(new ConfirmDesertHazards());

        game.Execute(new HarvestSpice(2, 0));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.State.ActiveBans, Has.Count.EqualTo(2));
            Assert.That(game.State.Notices.Count(n => n.Text.Contains("Ban active")), Is.EqualTo(2));
        }
    }

    [Test]
    public void SpacingGuildBanBlocksReinforcementsFromVoluntaryReveals()
    {
        var game = TestGame.InActionResolution();
        game.Execute(new VoluntaryReveal());
        Assert.That(game.State.Reinforcements, Is.EqualTo(3));

        TestGame.SpendAllDice(game);
        game.Execute(new ConfirmDesertHazards());
        game.Execute(new HarvestSpice(0, 0));

        Assert.That(game.State.IsBanActive(ImperiumPower.SpacingGuild), Is.True);
        Assert.Throws<CommandRejectedException>(() => game.Execute(new VoluntaryReveal()));
    }

    [Test]
    public void LandsraadBanBlocksReinforcementDiscards()
    {
        var game = TestGame.InActionResolution();
        game.Execute(new DiscardReinforcements(1));
        Assert.That(game.State.Reinforcements, Is.EqualTo(1));

        TestGame.SpendAllDice(game);
        game.Execute(new ConfirmDesertHazards());
        game.Execute(new HarvestSpice(0, 0));

        Assert.Throws<CommandRejectedException>(() => game.Execute(new DiscardReinforcements(1)));
    }

    [Test]
    public void DrawingIntoReinforcementsDoesNotChangeTheNextDeck()
    {
        // The next deck follows the top of the Harkonnen discard pile, which a facedown draw does not change.
        var game = TestGame.InActionResolution();
        Assert.That(game.State.NextDeck, Is.EqualTo(PlanningDeck.Harkonnen));

        game.Execute(new DrawToReinforcements());
        game.Execute(new VoluntaryReveal(Count: 3));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.State.NextDeck, Is.EqualTo(PlanningDeck.Harkonnen));
            Assert.That(game.State.Reinforcements, Is.EqualTo(6));
        }
    }

    [Test]
    public void MultiCardDrawsAlternateDecksStartingFromTheNextDeck()
    {
        Assert.That(GameReducer.DrawOrder(PlanningDeck.Corrino, 3),
            Is.EqualTo(new[] { PlanningDeck.Corrino, PlanningDeck.Harkonnen, PlanningDeck.Corrino }));
    }

    [Test]
    public void DiscardingReinforcementsSetsTheNextDeckFromTheLastDiscardedCard()
    {
        var game = TestGame.InActionResolution();

        game.Execute(new DiscardReinforcements(2, LastDiscardedFrom: PlanningDeck.Harkonnen));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.State.NextDeck, Is.EqualTo(PlanningDeck.Corrino));
            Assert.That(game.State.Reinforcements, Is.Zero);
        }
    }

    [Test]
    public void DrawCountMustBeReasonable()
    {
        var game = TestGame.InActionResolution();

        Assert.Throws<CommandRejectedException>(() => game.Execute(new DrawToReinforcements(0)));
        Assert.Throws<CommandRejectedException>(() => game.Execute(new VoluntaryReveal(7)));
    }

    [Test]
    public void SavesWithTheLegacyReinforcementEventStillReplay()
    {
        var game = TestGame.InActionResolution(seed: 3);
        var legacy = game.ToSave();
        legacy = legacy with { History = legacy.History.Add([new ReinforcementAdded("Voluntary reveal")]) };

        var loaded = Game.FromSave(TestGame.Content, SaveData.FromJson(legacy.ToJson()));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(loaded.State.Reinforcements, Is.EqualTo(3));
            Assert.That(loaded.State.NextDeck, Is.EqualTo(PlanningDeck.Corrino));
        }
    }

    [Test]
    public void DestroyingTheTargetSietchDrawsANewTarget()
    {
        var game = TestGame.InActionResolution();
        var target = game.State.Tactical.TargetCardId!;

        game.Execute(new DestroySietch(target, 2));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.State.Supremacy, Is.EqualTo(2));
            Assert.That(game.State.Tactical.TargetCardId, Is.Not.EqualTo(target));
            Assert.That(game.State.Tactical.DestroyedSietches, Does.Contain(target));
        }
    }

    [Test]
    public void TenSupremacyEndsTheGameWithAHarkonnenVictory()
    {
        var game = TestGame.InActionResolution();

        foreach (var sietch in TestGame.Content.TacticalCards.Select(c => c.Id).Take(4))
        {
            if (game.State.Phase != Phase.GameOver)
            {
                game.Execute(new DestroySietch(sietch, 3));
            }
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.State.Supremacy, Is.EqualTo(10));
            Assert.That(game.State.Phase, Is.EqualTo(Phase.GameOver));
            Assert.That(game.State.Outcome, Is.EqualTo(GameOutcome.HarkonnenVictory));
        }
        Assert.Throws<CommandRejectedException>(() => game.Execute(new RollHarkonnenDie()));
    }

    [Test]
    public void AtreidesVictoryEndsTheGame()
    {
        var game = TestGame.InActionResolution();

        game.Execute(new DeclareAtreidesVictory());

        Assert.That(game.State.Outcome, Is.EqualTo(GameOutcome.AtreidesVictory));
    }

    [Test]
    public void UndoRevertsOneCommand()
    {
        var game = TestGame.InActionResolution();
        var before = game.State;

        game.Execute(new RollHarkonnenDie());
        game.Execute(new CompleteHarkonnenTurn());
        game.Undo();

        Assert.That(game.State.Dice.Pending, Is.Not.Null);
        game.Undo();
        Assert.That(game.State.Dice, Is.EqualTo(before.Dice));
    }

    [Test]
    public void UndoneRollRepeatsTheSameResult()
    {
        var game = TestGame.InActionResolution(seed: 7);
        game.Execute(new RollHarkonnenDie());
        var first = game.State.Dice.Pending!.Face;

        game.Undo();
        game.Execute(new RollHarkonnenDie());

        Assert.That(game.State.Dice.Pending!.Face, Is.EqualTo(first));
    }

    [Test]
    public void SaveAndLoadRestoresTheSameState()
    {
        var game = TestGame.New(seed: 99);
        TestGame.AdvanceToRound(game, 3);
        game.Execute(new ConfirmRoundStart());
        game.Execute(new ConfirmVehicles());
        game.Execute(new RollHarkonnenDie());

        var loaded = Game.FromSave(TestGame.Content, SaveData.FromJson(game.ToSave().ToJson()));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(loaded.State.Round, Is.EqualTo(game.State.Round));
            Assert.That(loaded.State.Supremacy, Is.EqualTo(game.State.Supremacy));
            Assert.That(loaded.State.Dice.Pending!.Face, Is.EqualTo(game.State.Dice.Pending!.Face));
            Assert.That(loaded.State.Dice.Pending.Rerolled, Is.EqualTo(game.State.Dice.Pending.Rerolled));
            Assert.That(loaded.State.Dice.UsedFaces, Is.EqualTo(game.State.Dice.UsedFaces));
            Assert.That(loaded.State.Tactical.TargetCardId, Is.EqualTo(game.State.Tactical.TargetCardId));
            Assert.That(loaded.State.Imperium, Is.EquivalentTo(game.State.Imperium));
            Assert.That(loaded.History, Has.Count.EqualTo(game.History.Count));
        }
    }

    [Test]
    public void SameSeedGivesTheSameGame()
    {
        var a = TestGame.New(seed: 5);
        var b = TestGame.New(seed: 5);

        TestGame.AdvanceToRound(a, 3);
        TestGame.AdvanceToRound(b, 3);

        Assert.That(a.ToSave().ToJson(), Is.EqualTo(b.ToSave().ToJson()));
    }
}
