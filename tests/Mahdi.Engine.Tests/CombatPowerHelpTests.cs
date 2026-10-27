using Mahdi.Engine.Guidance;

namespace Mahdi.Engine.Tests;

public class CombatPowerHelpTests
{
    private static CombatPowerHelp Help => CombatPowerHelp.Rules;

    [Test]
    public void CountingFollowsTheRulebook()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Help.Counting.Single(r => r.Label.Contains("Unit")).Points, Is.EqualTo(1));
            Assert.That(Help.Counting.Single(r => r.Label.Contains("Deployment token")).Points, Is.EqualTo(2));
            Assert.That(Help.Counting.Single(r => r.Label.Contains("Leader")).Points, Is.EqualTo(1));
        }
    }

    [Test]
    public void WorkedExampleAddsUp()
    {
        // 3 Regular + 1 Elite + 1 Deployment token + 1 Bashar = 3 + 1 + 2 + 1.
        Assert.That(Help.ExampleTotal, Is.EqualTo(7));
    }

    [Test]
    public void TieValuesRankTheUnits()
    {
        var points = Help.TieValues.ToDictionary(r => r.Label, r => r.Points);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(points["Generic Leader (Bashar, Naib)"], Is.EqualTo(1));
            Assert.That(points["Regular Unit"], Is.EqualTo(2));
            Assert.That(points["Named Leader"], Is.EqualTo(2));
            Assert.That(points["Elite Unit"], Is.EqualTo(3));
            Assert.That(points["Sardaukar or Fedaykin Unit"], Is.EqualTo(4));
        }
    }

    [Test]
    public void NotesCoverRankAndTheHalfRule()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Help.Notes, Has.Some.Contains("rank do not count"));
            Assert.That(Help.Notes, Has.Some.Contains("half or less"));
            Assert.That(Help.RuleReference, Does.Contain("39"));
        }
    }
}
