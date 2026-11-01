using Mahdi.Engine.Model;

namespace Mahdi.Engine.Tests;

public class ContentTests
{
    private static GameContent Content => TestGame.Content;

    [Test]
    public void HarkonnenDieHasStrategyOnTwoFaces()
    {
        Assert.That(Content.Dice.Faces, Has.Length.EqualTo(6));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Content.Dice.Faces.Count(f => f == DieFace.Strategy), Is.EqualTo(2));
            Assert.That(Content.Dice.Faces.Distinct().Count(), Is.EqualTo(5));
            Assert.That(Content.Dice.DiceCount, Is.EqualTo(8));
        }
    }

    [TestCase(5, 0, 3, 2, 1)]
    [TestCase(4, 1, 4, 2, 1)]
    [TestCase(3, 2, 5, 3, 1)]
    [TestCase(2, 3, 5, 4, 2)]
    [TestCase(1, 4, 6, 4, 2)]
    public void SpiceMustFlowRowsMatchTheBoard(int step, int setAside, int harvesters, int ornithopters, int carryalls)
    {
        Assert.That(Content.RowForStep(step), Is.EqualTo(new SpiceMustFlowRow(step, setAside, harvesters, ornithopters, carryalls)));
    }

    [Test]
    public void TacticalDeckHasEightCardsWithTwoCentral()
    {
        Assert.That(Content.TacticalCards, Has.Length.EqualTo(8));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Content.TacticalCards.Count(c => c.Sector == Sector.Central), Is.EqualTo(2));
            Assert.That(Content.Card("gara-kulon").Sector, Is.EqualTo(Sector.NorthEast));
        }
    }

    [Test]
    public void EveryTacticalCardPinLiesInItsOwnSectorOnTheMap()
    {
        using (Assert.EnterMultipleScope())
        {
            foreach (var card in Content.TacticalCards)
            {
                Assert.That(card.Pin, Is.Not.Null, card.Id);
                Assert.That(BoardMap.SectorAt(card.Pin!), Is.EqualTo(card.Sector), card.Id);
            }
        }
    }

    [TestCase(30, 20, Sector.NorthWest)]
    [TestCase(90, 20, Sector.NorthEast)]
    [TestCase(30, 70, Sector.SouthWest)]
    [TestCase(90, 70, Sector.SouthEast)]
    [TestCase(60, 45, Sector.Central)]
    [TestCase(60, 19, Sector.Central)]
    [TestCase(60, 18, Sector.NorthEast)]
    public void BoardMapFindsTheSectorOfAPoint(int x, int y, Sector expected) =>
        Assert.That(BoardMap.SectorAt(new MapPoint(x, y)), Is.EqualTo(expected));

    [Test]
    public void EveryLeaderFaceIsUniqueAmongLeadersInPlayAtTheSameTime()
    {
        // Feyd replaces Rabban and Mohiam replaces Hawat, so one die result never has two candidates.
        var atStart = Content.Leaders.Where(l => l.Entry == LeaderEntry.AtStart).Select(l => l.Face);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(atStart, Is.Unique);
            Assert.That(Content.Leader(LeaderId.FeydRautha).Replaces, Is.EqualTo(LeaderId.BeastRabban));
        }
    }
}
