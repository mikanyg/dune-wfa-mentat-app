using Mahdi.Engine.Commands;
using Mahdi.Engine.Guidance;
using Mahdi.Engine.Model;
using Mahdi.Engine.Persistence;
using Mahdi.Engine.Rules;

namespace Mahdi.Engine.Tests;

public class BattleTests
{
    private static GameContent Content => TestGame.Content;

    /// <summary>A physical-dice game in Action Resolution with a Harkonnen turn in progress.</summary>
    private static Game DuringHarkonnenTurn(DieFace face = DieFace.Strategy)
    {
        var game = TestGame.InActionResolution(DiceMode.Physical);
        game.Execute(new EnterHarkonnenDie(face));
        return game;
    }

    private static string Target(Game game) => game.State.Tactical.TargetCardId!;

    [Test]
    public void TheAttackerFollowsWhoseTurnItIs()
    {
        var game = TestGame.InActionResolution(DiceMode.Physical);
        Assert.That(BattleRules.DefaultAttacker(game.State), Is.EqualTo(Attacker.Atreides));

        game.Execute(new EnterHarkonnenDie(DieFace.Strategy));
        Assert.That(BattleRules.DefaultAttacker(game.State), Is.EqualTo(Attacker.Harkonnen));
    }

    [Test]
    public void EachSideAttacksOnlyItsOwnTargets()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(BattleRules.Targets(Attacker.Harkonnen), Is.EquivalentTo(new[] { BattleTarget.Sietch, BattleTarget.AtreidesLegion }));
            Assert.That(BattleRules.Targets(Attacker.Atreides), Is.EquivalentTo(new[] { BattleTarget.HarkonnenLegion, BattleTarget.Settlement }));
        }

        var game = DuringHarkonnenTurn();
        Assert.Throws<CommandRejectedException>(() => game.Execute(new StartBattle(Attacker.Harkonnen, BattleTarget.Settlement, Settlement: SettlementKind.Carthag)));
    }

    [Test]
    public void StartingABattleValidatesTheTarget()
    {
        var game = DuringHarkonnenTurn();

        Assert.Throws<CommandRejectedException>(() => game.Execute(new StartBattle(Attacker.Harkonnen, BattleTarget.Sietch)));
        Assert.Throws<CommandRejectedException>(() => game.Execute(new StartBattle(Attacker.Atreides, BattleTarget.Settlement)));
        Assert.Throws<CommandRejectedException>(() => game.Execute(new StartBattle(Attacker.Atreides, BattleTarget.HarkonnenLegion, Settlement: SettlementKind.Carthag)));

        game.Execute(new StartBattle(Attacker.Harkonnen, BattleTarget.Sietch, Target(game)));
        Assert.Throws<CommandRejectedException>(() => game.Execute(new StartBattle(Attacker.Harkonnen, BattleTarget.AtreidesLegion)));
    }

    [Test]
    public void ALeadershipAttackIsASurpriseAttack()
    {
        var game = DuringHarkonnenTurn(DieFace.Leadership);

        game.Execute(new StartBattle(Attacker.Harkonnen, BattleTarget.AtreidesLegion));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.State.Battle!.SurpriseAttack, Is.True);
            Assert.That(BattleGuidance.RoundNotes(game.State.Battle), Has.Some.Contains("Surprise Attack"));
        }
    }

    [Test]
    public void RoundsAreCountedAndDiscardsSetTheNextDeck()
    {
        var game = DuringHarkonnenTurn();
        game.Execute(new DrawToReinforcements(2));
        game.Execute(new StartBattle(Attacker.Harkonnen, BattleTarget.AtreidesLegion));

        game.Execute(new FightBattleRound(2, PlanningDeck.Harkonnen));
        game.Execute(new FightBattleRound());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.State.Battle!.Round, Is.EqualTo(3));
            Assert.That(game.State.Battle.CardsDiscarded, Is.EqualTo(2));
            Assert.That(game.State.Reinforcements, Is.EqualTo(2));
            Assert.That(game.State.NextDeck, Is.EqualTo(PlanningDeck.Corrino));
        }
    }

    [Test]
    public void DiscardsNeedADeckAndRespectTheLandsraadBan()
    {
        var game = DuringHarkonnenTurn();
        game.Execute(new StartBattle(Attacker.Harkonnen, BattleTarget.AtreidesLegion));

        Assert.Throws<CommandRejectedException>(() => game.Execute(new FightBattleRound(1)));
        Assert.Throws<CommandRejectedException>(() => game.Execute(new FightBattleRound(5, PlanningDeck.Corrino)));

        var banned = game.State with { ActiveBans = [ImperiumPower.Landsraad] };
        Assert.That(BattleRules.SuggestedDiscard(banned, 2), Is.Zero);
    }

    [Test]
    public void AVictoriousHarkonnenAttackDestroysTheSietchAndDrawsANewTarget()
    {
        var game = DuringHarkonnenTurn();
        var target = Target(game);
        game.Execute(new StartBattle(Attacker.Harkonnen, BattleTarget.Sietch, target));
        game.Execute(new FightBattleRound());

        Assert.Throws<CommandRejectedException>(() => game.Execute(new EndBattle(BattleOutcome.DefenderDestroyed)));
        game.Execute(new EndBattle(BattleOutcome.DefenderDestroyed, SietchRank: 2));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.State.Battle, Is.Null);
            Assert.That(game.State.Supremacy, Is.EqualTo(2));
            Assert.That(game.State.Tactical.DestroyedSietches, Does.Contain(target));
            Assert.That(game.State.Tactical.TargetCardId, Is.Not.EqualTo(target));
            Assert.That(game.State.Notices.Select(n => n.Text), Has.Some.Contains("may advance"));
        }
    }

    [Test]
    public void ARetreatAlsoLosesTheSietch()
    {
        var game = DuringHarkonnenTurn();
        game.Execute(new StartBattle(Attacker.Harkonnen, BattleTarget.Sietch, Target(game)));

        game.Execute(new EndBattle(BattleOutcome.DefenderRetreated, SietchRank: 1));

        Assert.That(game.State.Supremacy, Is.EqualTo(1));
    }

    [TestCase(BattleOutcome.AttackerCeased)]
    [TestCase(BattleOutcome.AttackerDestroyed)]
    [TestCase(BattleOutcome.BothDestroyed)]
    public void TheSietchStandsUnlessTheHarkonnensWin(BattleOutcome outcome)
    {
        var game = DuringHarkonnenTurn();
        game.Execute(new StartBattle(Attacker.Harkonnen, BattleTarget.Sietch, Target(game)));

        Assert.Throws<CommandRejectedException>(() => game.Execute(new EndBattle(outcome, SietchRank: 2)));
        game.Execute(new EndBattle(outcome));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.State.Supremacy, Is.Zero);
            Assert.That(game.State.Tactical.DestroyedSietches, Is.Empty);
        }
    }

    [Test]
    public void KilledNamedLeadersGoToTheRegenerationTank()
    {
        var game = TestGame.InActionResolution(DiceMode.Physical);
        game.Execute(new EnterHarkonnenDie(DieFace.Deployment));
        game.Execute(new CompleteHarkonnenTurn());
        Assert.That(game.State.Leaders[LeaderId.BeastRabban].Status, Is.EqualTo(LeaderStatus.OnBoard));

        game.Execute(new StartBattle(Attacker.Atreides, BattleTarget.HarkonnenLegion));
        Assert.Throws<CommandRejectedException>(() => game.Execute(new EndBattle(BattleOutcome.DefenderDestroyed, KilledLeaders: [LeaderId.BaronHarkonnen])));
        game.Execute(new EndBattle(BattleOutcome.DefenderDestroyed, KilledLeaders: [LeaderId.BeastRabban]));

        Assert.That(game.State.Leaders[LeaderId.BeastRabban].Status, Is.EqualTo(LeaderStatus.InTank));
    }

    [Test]
    public void TheHarkonnensNeverRetreat()
    {
        var game = TestGame.InActionResolution();
        game.Execute(new StartBattle(Attacker.Atreides, BattleTarget.HarkonnenLegion));

        Assert.That(BattleRules.Outcomes(Attacker.Atreides), Does.Not.Contain(BattleOutcome.DefenderRetreated));
        Assert.Throws<CommandRejectedException>(() => game.Execute(new EndBattle(BattleOutcome.DefenderRetreated)));
        Assert.That(BattleGuidance.RoundNotes(game.State.Battle!), Has.Some.Contains("never retreat"));
    }

    [TestCase(SettlementKind.PyonVillage, 1)]
    [TestCase(SettlementKind.Carthag, 2)]
    [TestCase(SettlementKind.Arrakeen, 3)]
    public void SettlementRankAddsHarkonnenDice(SettlementKind settlement, int rank)
    {
        var battle = new BattleState(Attacker.Atreides, BattleTarget.Settlement, null, settlement, false, 1, 0);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(BattleRules.SettlementRank(settlement), Is.EqualTo(rank));
            Assert.That(BattleRules.HarkonnenDiceBeforeCards(battle, 3), Is.EqualTo(3 + rank));
            Assert.That(BattleRules.HarkonnenDiceBeforeCards(battle, 6), Is.EqualTo(6));
        }
    }

    [Test]
    public void AFallenSettlementRemindsTheAtreidesOfPrescience()
    {
        var game = TestGame.InActionResolution();
        game.Execute(new StartBattle(Attacker.Atreides, BattleTarget.Settlement, Settlement: SettlementKind.Carthag));

        game.Execute(new EndBattle(BattleOutcome.DefenderDestroyed));

        Assert.That(game.State.Notices.Select(n => n.Text), Has.Some.Contains("Carthag is destroyed").And.Some.Contains("rank (2)"));
    }

    [Test]
    public void ArrakeenFallingPromptsTheSecretObjectiveCheck()
    {
        var game = TestGame.InActionResolution();
        game.Execute(new StartBattle(Attacker.Atreides, BattleTarget.Settlement, Settlement: SettlementKind.Arrakeen));

        game.Execute(new EndBattle(BattleOutcome.DefenderDestroyed));

        Assert.That(game.State.Notices.Select(n => n.Text), Has.Some.Contains("Secret Objective"));
    }

    [Test]
    public void ABattleCanOnlyBeCancelledBeforeAnyRound()
    {
        var game = TestGame.InActionResolution();
        game.Execute(new StartBattle(Attacker.Atreides, BattleTarget.HarkonnenLegion));
        game.Execute(new CancelBattle());
        Assert.That(game.State.Battle, Is.Null);

        game.Execute(new StartBattle(Attacker.Atreides, BattleTarget.HarkonnenLegion));
        game.Execute(new FightBattleRound());
        Assert.Throws<CommandRejectedException>(() => game.Execute(new CancelBattle()));
    }

    [Test]
    public void UndoStepsBackOneRoundAndReloadKeepsTheBattle()
    {
        var game = DuringHarkonnenTurn();
        game.Execute(new StartBattle(Attacker.Harkonnen, BattleTarget.AtreidesLegion));
        game.Execute(new FightBattleRound());
        game.Execute(new FightBattleRound());

        game.Undo();
        Assert.That(game.State.Battle!.Round, Is.EqualTo(2));

        var loaded = Game.FromSave(Content, SaveData.FromJson(game.ToSave().ToJson()));
        Assert.That(loaded.State.Battle, Is.EqualTo(game.State.Battle));
    }

    [Test]
    public void GuidanceNamesTheBattleAndItsOutcomes()
    {
        var game = DuringHarkonnenTurn();
        var sietch = Content.Card(Target(game)).Sietch;
        game.Execute(new StartBattle(Attacker.Harkonnen, BattleTarget.Sietch, Target(game)));
        var battle = game.State.Battle!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(BattleGuidance.Headline(Content, battle), Is.EqualTo($"The Harkonnens attack {sietch}"));
            Assert.That(BattleGuidance.RoundNotes(battle), Has.Some.Contains("do not take 1 Hit"));
            Assert.That(BattleGuidance.OutcomeLabel(Attacker.Harkonnen, BattleOutcome.AttackerCeased), Does.Contain("half or less"));
            Assert.That(BattleGuidance.OutcomeLabel(Attacker.Atreides, BattleOutcome.AttackerCeased), Is.EqualTo("Atreides stop attacking"));
            Assert.That(BattleGuidance.TargetName(BattleTarget.Settlement), Is.EqualTo("A Harkonnen Settlement"));
            Assert.That(BattleGuidance.SettlementLabel(SettlementKind.PyonVillage), Is.EqualTo("Pyon Village"));
        }
    }
}
