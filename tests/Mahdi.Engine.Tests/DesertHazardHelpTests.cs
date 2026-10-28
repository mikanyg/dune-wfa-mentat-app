using Mahdi.Engine.Guidance;
using Mahdi.Engine.Model;
using Mahdi.Engine.Rules;

namespace Mahdi.Engine.Tests;

public class DesertHazardHelpTests
{
    private static DesertHazardHelp Help => DesertHazardHelp.Rules;

    [TestCase(HazardAttack.CoriolisStorm, 2)]
    [TestCase(HazardAttack.Sandworm, 4)]
    [TestCase(HazardAttack.WildMaker, 6)]
    public void DiceRolledPerAttack(HazardAttack attack, int dice)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(HazardRules.Dice(attack), Is.EqualTo(dice));
            Assert.That(Help.Attacks.Single(a => a.Attack == attack).Dice, Is.EqualTo(dice));
        }
    }

    [TestCase(HazardTerrain.DeepDesert, 2, 1, 1, 3)]
    [TestCase(HazardTerrain.Desert, 1, 1, 1, 2)]
    [TestCase(HazardTerrain.MinorErgOrPlateau, 0, 1, 1, 1)]
    [TestCase(HazardTerrain.DeepDesert, 2, 0, 2, 4)]
    public void SwordsAreOneHitAndSpecialsDependOnTheTerrain(HazardTerrain terrain, int perSpecial, int swords, int specials, int hits)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(HazardRules.SpecialHits(terrain), Is.EqualTo(perSpecial));
            Assert.That(HazardRules.Hits(terrain, swords, specials), Is.EqualTo(hits));
            Assert.That(Help.SpecialHits.Single(t => t.Terrain == terrain).Hits, Is.EqualTo(perSpecial));
        }
    }

    [Test]
    public void EveryDieResultIsExplainedAndShieldsDoNothing()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Help.DieResults.Select(r => r.Face), Is.EquivalentTo(Enum.GetValues<CombatDieFace>()));
            Assert.That(Help.DieResults.Single(r => r.Face == CombatDieFace.Shield).Effect, Does.StartWith("No effect"));
        }
    }

    [Test]
    public void WorkedExampleAppliesOneStepPerHit()
    {
        using (Assert.EnterMultipleScope())
        {
            // 1 Sword + 1 Special in Deep Desert = 1 + 2.
            Assert.That(Help.ExampleHits, Is.EqualTo(3));
            Assert.That(Help.ExampleSteps, Has.Length.EqualTo(Help.ExampleHits));
            Assert.That(Help.HitPriority, Has.Length.EqualTo(4));
        }
    }

    [Test]
    public void ExposedAreasExcludeMountainsAndTheCentralPlateaus()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Help.ExposedAreas, Is.EquivalentTo(new[] { "Plateau", "Minor Erg", "Desert", "Deep Desert" }));
            Assert.That(Help.NotExposed, Has.Some.Contains("Mountains"));
            Assert.That(Help.NotExposed, Has.Some.Contains("5 central Plateau"));
            Assert.That(Help.RuleReference, Does.Contain("20").And.Contain("31"));
        }
    }

    [Test]
    public void OutOfRangeValuesAreRejected()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => HazardRules.Dice((HazardAttack)99));
            Assert.Throws<ArgumentOutOfRangeException>(() => HazardRules.SpecialHits((HazardTerrain)99));
        }
    }
}
