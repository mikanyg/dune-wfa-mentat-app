using Mahdi.Engine.Commands;
using Mahdi.Engine.Guidance;
using Mahdi.Engine.Model;

namespace Mahdi.Engine.Tests;

public class GuidanceTests
{
    private static GuidanceBuilder Builder => new(TestGame.Content);

    [Test]
    public void FallbackChainNamesTheTargetSietch()
    {
        var game = TestGame.InActionResolution(DiceMode.Physical);
        var target = TestGame.Content.Card(game.State.Tactical.TargetCardId!).Sietch;

        game.Execute(new EnterHarkonnenDie(DieFace.Strategy));
        var guidance = Builder.Turn(game.State);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(guidance.StepsLabel, Does.Contain("FIRST"));
            Assert.That(guidance.Steps, Has.Length.EqualTo(3));
            Assert.That(guidance.Steps[2].Text, Does.Contain(target));
        }
    }

    [Test]
    public void LeadershipRequiresALeader()
    {
        var game = TestGame.InActionResolution(DiceMode.Physical);

        game.Execute(new EnterHarkonnenDie(DieFace.Leadership));
        var guidance = Builder.Turn(game.State);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(guidance.Notes, Has.Some.Contains("only Legions containing at least 1 Leader"));
            Assert.That(guidance.Steps[0].Text, Does.Contain("Surprise Attack"));
        }
    }

    [Test]
    public void SpecialActionCarriesTheRegularActionAsAlternative()
    {
        var game = TestGame.InActionResolution(DiceMode.Physical);

        game.Execute(new EnterHarkonnenDie(DieFace.House));
        var guidance = Builder.Turn(game.State);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(guidance.Title, Does.Contain("Baron Harkonnen"));
            Assert.That(guidance.Headline, Does.Contain("Replace 3 Regular Units"));
            Assert.That(guidance.Alternative, Is.Not.Null);
            Assert.That(guidance.Alternative!.Steps[0].Text, Does.Contain("Replace 2 Regular Units"));
        }
    }

    [Test]
    public void BaronSpecialReplacesTheRegularHouseActionWithoutVehicles()
    {
        var game = TestGame.InActionResolution(DiceMode.Physical);

        game.Execute(new EnterHarkonnenDie(DieFace.House));
        var guidance = Builder.Turn(game.State);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(guidance.Headline, Does.Contain("No Vehicles"));
            Assert.That(guidance.Steps.Select(s => s.Text), Has.None.Contains("Harvester"));
            Assert.That(guidance.Alternative!.Steps[1].Text, Does.Contain("1 Harvester and 1 Ornithopter"));
        }
    }

    [Test]
    public void MentatNamesTheDeckOrder()
    {
        var game = TestGame.InActionResolution(DiceMode.Physical);
        game.Execute(new SetNextDeck(PlanningDeck.Corrino));

        game.Execute(new EnterHarkonnenDie(DieFace.Mentat));

        Assert.That(Builder.Turn(game.State).Headline, Does.Contain("first from Corrino Ally, the second from House Harkonnen"));
    }

    [Test]
    public void VehiclePlacementUsesTheActiveRow()
    {
        var game = TestGame.New();
        game.Execute(new CompleteSetup());
        game.Execute(new ConfirmRoundStart());

        var guidance = Builder.VehiclePlacement(game.State);

        Assert.That(guidance.Headline, Does.Contain("3 Harvesters, 1 Carryall and 2 Ornithopters"));
    }

    [Test]
    public void ActiveBansAppearInTheNotes()
    {
        var game = TestGame.InActionResolution(DiceMode.Physical);
        TestGame.SpendAllDice(game);
        game.Execute(new ConfirmDesertHazards());
        game.Execute(new HarvestSpice(0, 0));
        game.Execute(new EndRound());
        game.Execute(new ConfirmRoundStart());
        game.Execute(new ConfirmVehicles());

        game.Execute(new EnterHarkonnenDie(DieFace.Strategy));

        Assert.That(Builder.Turn(game.State).Notes, Has.Some.Contains("CHOAM Ban"));
    }
}
