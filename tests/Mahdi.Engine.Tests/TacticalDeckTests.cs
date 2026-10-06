using Mahdi.Engine.Model;
using Mahdi.Engine.Rules;

namespace Mahdi.Engine.Tests;

public class TacticalDeckTests
{
    private static GameContent Content => TestGame.Content;

    private static GameState Fresh() => GameReducer.Initial(Content, DiceMode.AppRolls);

    [Test]
    public void TargetIsNeverInTheHarvestingSector([Range(0, 40)] int seed)
    {
        var round = TacticalDeck.DrawRound(Content, Fresh(), new SeededRandomSource(seed, 0), 1);

        Assert.That(round.TargetCardId, Is.Not.Null);
        Assert.That(Content.Card(round.TargetCardId!).Sector, Is.Not.EqualTo(Content.Card(round.HarvestCardId).Sector));
        Assert.That(round.Rejected.Select(id => Content.Card(id).Sector), Is.All.EqualTo(Content.Card(round.HarvestCardId).Sector));
    }

    [Test]
    public void SameSectorCardIsDiscardedAndRedrawn()
    {
        // Deck order by index: rock-outcroppings (NW) is harvest, then bight-of-the-cliff (NW) is rejected.
        var round = TacticalDeck.DrawRound(Content, Fresh(), new ScriptedRandom(0, 0, 0), 1);

        Assert.That(round.HarvestCardId, Is.EqualTo("rock-outcroppings"));
        Assert.That(round.Rejected, Is.EqualTo(new[] { "bight-of-the-cliff" }));
        Assert.That(round.TargetCardId, Is.EqualTo("sihaya-ridge"));
    }

    [Test]
    public void DestroyedSietchIsNeverTheTarget()
    {
        var state = Fresh() with
        {
            Tactical = TacticalState.Empty with { DestroyedSietches = ["bight-of-the-cliff", "sihaya-ridge"] },
        };

        var round = TacticalDeck.DrawRound(Content, state, new ScriptedRandom(0, 0, 0, 0), 1);

        Assert.That(round.TargetCardId, Is.EqualTo("gara-kulon"));
        Assert.That(round.Rejected, Is.EqualTo(new[] { "bight-of-the-cliff", "sihaya-ridge" }));
    }

    [Test]
    public void NoTargetWhenEverySietchOutsideTheHarvestingSectorIsDestroyed()
    {
        var destroyed = Content.TacticalCards.Where(c => c.Sector != Sector.NorthWest).Select(c => c.Id).ToHashSet();
        var state = Fresh() with
        {
            Tactical = TacticalState.Empty with { DestroyedSietches = [.. destroyed] },
        };

        var round = TacticalDeck.DrawRound(Content, state, new ScriptedRandom(0), 1);

        Assert.That(round.TargetCardId, Is.Null);
    }

    [Test]
    public void RedrawReshufflesDiscardsWhenTheDeckHasNoEligibleCard()
    {
        // Harvest NW; every non-NW card except Gara Kulon has been discarded; Sihaya Ridge is the destroyed target.
        var discard = Content.TacticalCards
            .Where(c => c.Sector != Sector.NorthWest && c.Id is not "gara-kulon" and not "sihaya-ridge")
            .Select(c => c.Id)
            .ToList();
        discard.Add("gara-kulon");
        var state = Fresh() with
        {
            Tactical = new TacticalState([.. discard], "rock-outcroppings", "sihaya-ridge", ["sihaya-ridge"]),
        };

        var drawn = TacticalDeck.RedrawTarget(Content, state, new ScriptedRandom(1, 0));

        Assert.That(drawn.Reshuffled, Is.True);
        Assert.That(drawn.TargetCardId, Is.Not.Null.And.Not.EqualTo("sihaya-ridge"));
        Assert.That(Content.Card(drawn.TargetCardId!).Sector, Is.Not.EqualTo(Sector.NorthWest));
    }
}
