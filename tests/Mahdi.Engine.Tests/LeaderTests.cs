using System.Collections.Immutable;
using Mahdi.Engine.Commands;
using Mahdi.Engine.Model;
using Mahdi.Engine.Rules;

namespace Mahdi.Engine.Tests;

public class LeaderTests
{
    [Test]
    public void LeadersInPlayAtTheStart()
    {
        var state = TestGame.New().State;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(state.Leaders[LeaderId.BaronHarkonnen].Status, Is.EqualTo(LeaderStatus.InReserve));
            Assert.That(state.Leaders[LeaderId.BeastRabban].Status, Is.EqualTo(LeaderStatus.InReserve));
            Assert.That(state.Leaders[LeaderId.CaptainAramsham].Status, Is.EqualTo(LeaderStatus.InReserve));
            Assert.That(state.Leaders[LeaderId.ThufirHawat].Status, Is.EqualTo(LeaderStatus.NotInPlay));
            Assert.That(state.Leaders[LeaderId.FeydRautha].Status, Is.EqualTo(LeaderStatus.NotInPlay));
        }
    }

    [Test]
    public void SpecialActionReplacesTheRegularActionAndSpendsTheCard()
    {
        var game = TestGame.InActionResolution(DiceMode.Physical);

        game.Execute(new EnterHarkonnenDie(DieFace.House));
        Assert.That(game.State.Dice.Pending!.SpecialLeader, Is.EqualTo(LeaderId.BaronHarkonnen));
        game.Execute(new CompleteHarkonnenTurn());
        Assert.That(game.State.Leaders[LeaderId.BaronHarkonnen].CardSpent, Is.True);

        game.Execute(new EnterHarkonnenDie(DieFace.House));
        Assert.That(game.State.Dice.Pending!.SpecialLeader, Is.Null);
    }

    [Test]
    public void SpecialNotPossibleKeepsTheCard()
    {
        var game = TestGame.InActionResolution(DiceMode.Physical);

        game.Execute(new EnterHarkonnenDie(DieFace.House));
        game.Execute(new CompleteHarkonnenTurn(SpecialUsed: false));

        Assert.That(game.State.Leaders[LeaderId.BaronHarkonnen].CardSpent, Is.False);
    }

    [Test]
    public void RabbanSpecialNeedsRabbanOnTheBoard()
    {
        var game = TestGame.InActionResolution(DiceMode.Physical);

        game.Execute(new EnterHarkonnenDie(DieFace.Leadership));
        Assert.That(game.State.Dice.Pending!.SpecialLeader, Is.Null);
        game.Execute(new CompleteHarkonnenTurn());

        // Deployment puts Rabban on the board first (he deploys before other Named Leaders).
        game.Execute(new EnterHarkonnenDie(DieFace.Deployment));
        Assert.That(game.State.Dice.Pending!.DeployLeader, Is.EqualTo(LeaderId.BeastRabban));
        game.Execute(new CompleteHarkonnenTurn());
        Assert.That(game.State.Leaders[LeaderId.BeastRabban].Status, Is.EqualTo(LeaderStatus.OnBoard));

        game.Execute(new EnterHarkonnenDie(DieFace.Leadership));
        Assert.That(game.State.Dice.Pending!.SpecialLeader, Is.EqualTo(LeaderId.BeastRabban));
    }

    [Test]
    public void DeploymentFallsBackToABasharWhenNoNamedLeaderIsAvailable()
    {
        var game = TestGame.InActionResolution(DiceMode.Physical);
        var deployed = new List<LeaderId?>();

        for (var i = 0; i < 4; i++)
        {
            game.Execute(new EnterHarkonnenDie(i < 2 ? DieFace.Deployment : DieFace.Strategy));
            deployed.Add(game.State.Dice.Pending!.DeployLeader);
            game.Execute(new CompleteHarkonnenTurn());
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(deployed[0], Is.EqualTo(LeaderId.BeastRabban));
            Assert.That(deployed[1], Is.Not.Null.And.Not.EqualTo(LeaderId.BeastRabban));
        }

        var state = game.State with
        {
            Leaders = game.State.Leaders.ToDictionary(l => l.Key, l => l.Value.Status == LeaderStatus.InReserve
                ? l.Value with { Status = LeaderStatus.OnBoard }
                : l.Value).ToImmutableDictionary(),
        };
        Assert.That(LeaderRules.DeployCandidates(TestGame.Content, state), Is.Empty);
    }

    [Test]
    public void SpentCardsRefreshAtTheEndOfTheRound()
    {
        var game = TestGame.InActionResolution(DiceMode.Physical);
        game.Execute(new EnterHarkonnenDie(DieFace.House));
        game.Execute(new CompleteHarkonnenTurn());

        TestGame.AdvanceToRound(game, 2);

        Assert.That(game.State.Leaders[LeaderId.BaronHarkonnen].CardSpent, Is.False);
    }

    [Test]
    public void KilledLeaderReturnsAfterFiveHarkonnenTurns()
    {
        var game = TestGame.InActionResolution(DiceMode.Physical);
        game.Execute(new KillLeader(LeaderId.BaronHarkonnen));
        Assert.That(game.State.Leaders[LeaderId.BaronHarkonnen].Status, Is.EqualTo(LeaderStatus.InTank));

        for (var turn = 1; turn <= 5; turn++)
        {
            game.Execute(new EnterHarkonnenDie(turn % 2 == 0 ? DieFace.Mentat : DieFace.Leadership));
            Assert.That(game.State.Dice.Pending!.SpecialLeader, Is.Not.EqualTo(LeaderId.BaronHarkonnen));
            game.Execute(new CompleteHarkonnenTurn());

            var expected = turn < 5 ? LeaderStatus.InTank : LeaderStatus.InReserve;
            Assert.That(game.State.Leaders[LeaderId.BaronHarkonnen].Status, Is.EqualTo(expected), $"after turn {turn}");
        }

        Assert.That(game.State.Leaders[LeaderId.BaronHarkonnen].CardSpent, Is.False);
    }

    [Test]
    public void HawatEntersAtSupremacyOne()
    {
        var game = TestGame.New();

        TestGame.AdvanceToRound(game, 2);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.State.Supremacy, Is.EqualTo(1));
            Assert.That(game.State.Leaders[LeaderId.ThufirHawat].Status, Is.EqualTo(LeaderStatus.InReserve));
            Assert.That(game.State.Notices.Select(n => n.Text), Has.Some.Contains("Thufir Hawat enters play"));
        }
    }

    [Test]
    public void FeydRauthaEntersAtSupremacySixAndRemovesRabban()
    {
        var game = TestGame.InActionResolution();

        game.Execute(new DestroySietch("windgap", 3));
        game.Execute(new DestroySietch("hobars-gap", 3));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.State.Supremacy, Is.EqualTo(6));
            Assert.That(game.State.Leaders[LeaderId.FeydRautha].Status, Is.EqualTo(LeaderStatus.InReserve));
            Assert.That(game.State.Leaders[LeaderId.BeastRabban].Status, Is.EqualTo(LeaderStatus.Removed));
        }
    }

    [Test]
    public void AtreidesBeneGesseritReminderAtSupremacyFour()
    {
        var game = TestGame.InActionResolution();

        game.Execute(new DestroySietch("windgap", 3));
        game.Execute(new DestroySietch("hobars-gap", 1));

        Assert.That(game.State.Notices.Select(n => n.Text), Has.Some.Contains("Atreides take 1 Bene Gesserit token"));
    }

    [Test]
    public void MohiamEntersWhenHawatIsRemovedAndGrantsABeneGesseritToken()
    {
        var game = TestGame.New();
        TestGame.AdvanceToRound(game, 2);

        game.Execute(new RemoveLeaderFromGame(LeaderId.ThufirHawat));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.State.Leaders[LeaderId.GaiusHelenMohiam].Status, Is.EqualTo(LeaderStatus.InReserve));
            Assert.That(game.State.Notices.Select(n => n.Text), Has.Some.Contains("Bene Gesserit token"));
        }
    }

    [Test]
    public void ShaddamEntersWhenRageOvercameShaddamIsPlayed()
    {
        var game = TestGame.InActionResolution();

        game.Execute(new PlayRageOvercameShaddam());

        Assert.That(game.State.Leaders[LeaderId.ShaddamIV].Status, Is.EqualTo(LeaderStatus.InReserve));
        Assert.Throws<CommandRejectedException>(() => game.Execute(new PlayRageOvercameShaddam()));
    }

    [Test]
    public void HawatSpecialDrawsHarkonnenCardsSoTheNextDrawIsCorrino()
    {
        var game = TestGame.New(DiceMode.Physical);
        TestGame.AdvanceToRound(game, 2);
        game.Execute(new ConfirmRoundStart());
        game.Execute(new ConfirmVehicles());

        game.Execute(new EnterHarkonnenDie(DieFace.Mentat));
        Assert.That(game.State.Dice.Pending!.SpecialLeader, Is.EqualTo(LeaderId.ThufirHawat));
        game.Execute(new CompleteHarkonnenTurn());

        Assert.That(game.State.NextDeck, Is.EqualTo(PlanningDeck.Corrino));
    }
}
