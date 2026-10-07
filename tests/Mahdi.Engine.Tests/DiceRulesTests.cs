using Mahdi.Engine.Model;
using Mahdi.Engine.Rules;

namespace Mahdi.Engine.Tests;

public class DiceRulesTests
{
    private static GameContent Content => TestGame.Content;

    // Face indexes on the die: 0 Leadership, 1 Strategy, 2 Strategy, 3 Deployment, 4 Mentat, 5 House.
    [Test]
    public void ResultIsBlockedOnlyWhenThreeSpentDiceShowIt()
    {
        var dice = new DiceState { UsedFaces = [DieFace.Strategy, DieFace.Strategy] };
        Assert.That(DiceRules.IsBlocked(Content, dice, DieFace.Strategy), Is.False);

        dice = dice with { UsedFaces = dice.UsedFaces.Add(DieFace.Strategy) };
        Assert.That(DiceRules.IsBlocked(Content, dice, DieFace.Strategy), Is.True);
    }

    [Test]
    public void RollRerollsBlockedResults()
    {
        var dice = new DiceState { UsedFaces = [DieFace.Strategy, DieFace.Strategy, DieFace.Strategy] };

        var (face, rerolled) = DiceRules.Roll(Content, dice, new ScriptedRandom(1, 2, 4));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(face, Is.EqualTo(DieFace.Mentat));
            Assert.That(rerolled, Is.EqualTo(new[] { DieFace.Strategy, DieFace.Strategy }));
        }
    }

    [Test]
    public void RollWithoutBlocksTakesTheFirstResult()
    {
        var (face, rerolled) = DiceRules.Roll(Content, new DiceState(), new ScriptedRandom(5));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(face, Is.EqualTo(DieFace.House));
            Assert.That(rerolled, Is.Empty);
        }
    }

    [TestCase(DieFace.Deployment, 1, true)]
    [TestCase(DieFace.Deployment, 2, false)]
    [TestCase(DieFace.House, 2, false)]
    [TestCase(DieFace.Strategy, 2, true)]
    [TestCase(DieFace.Strategy, 3, false)]
    public void TruthtranceLimitsFollowTheSoloText(DieFace face, int spent, bool allowed)
    {
        var dice = new DiceState { UsedFaces = [.. Enumerable.Repeat(face, spent)] };

        Assert.That(DiceRules.CanChooseWithTruthtrance(Content, dice, face), Is.EqualTo(allowed));
    }
}
