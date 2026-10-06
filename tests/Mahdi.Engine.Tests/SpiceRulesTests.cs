using System.Collections.Immutable;
using Mahdi.Engine.Model;
using Mahdi.Engine.Rules;

namespace Mahdi.Engine.Tests;

public class SpiceRulesTests
{
    private static GameContent Content => TestGame.Content;

    private static ImmutableDictionary<ImperiumPower, int> Markers(int choam, int guild, int landsraad) =>
        new Dictionary<ImperiumPower, int>
        {
            [ImperiumPower.Choam] = choam,
            [ImperiumPower.SpacingGuild] = guild,
            [ImperiumPower.Landsraad] = landsraad,
        }.ToImmutableDictionary();

    private static MarkerOutcome Marker(SpiceResult result, ImperiumPower power) => result.Markers.Single(m => m.Power == power);

    [Test]
    public void HarvesterValues()
    {
        Assert.That(SpiceRules.Collected(desertHarvesters: 2, deepDesertHarvesters: 3), Is.EqualTo(8));
    }

    [Test]
    public void NoSpiceDropsEveryMarker()
    {
        var result = SpiceRules.Resolve(Content, Markers(5, 5, 5), 0, 0);

        Assert.That(result.Markers.Select(m => m.Change), Is.All.EqualTo(MarkerChange.Dropped));
        Assert.That(result.Markers.Select(m => m.To), Is.All.EqualTo(4));
    }

    [Test]
    public void KeepingStartsFromTheLowestMarker()
    {
        var result = SpiceRules.Resolve(Content, Markers(5, 3, 4), 0, 4);

        Assert.That(Marker(result, ImperiumPower.SpacingGuild).Change, Is.EqualTo(MarkerChange.Kept));
        Assert.That(Marker(result, ImperiumPower.Landsraad).Change, Is.EqualTo(MarkerChange.Kept));
        Assert.That(Marker(result, ImperiumPower.Choam).Change, Is.EqualTo(MarkerChange.Dropped));
    }

    [Test]
    public void SpiceBeyondKeepingRaisesTheLowestMarkers()
    {
        var result = SpiceRules.Resolve(Content, Markers(5, 3, 4), 0, 8);

        Assert.That(Marker(result, ImperiumPower.SpacingGuild).To, Is.EqualTo(4));
        Assert.That(Marker(result, ImperiumPower.Landsraad).To, Is.EqualTo(5));
        Assert.That(Marker(result, ImperiumPower.Choam).Change, Is.EqualTo(MarkerChange.Kept));
        Assert.That(result.NewReserve, Is.Zero);
    }

    [Test]
    public void SevenSpiceWithEveryMarkerOnTopScoresSupremacy()
    {
        var result = SpiceRules.Resolve(Content, Markers(5, 5, 5), 0, 7);

        Assert.That(result.SupremacyGained, Is.True);
        Assert.That(result.Markers.Select(m => m.Change), Is.All.EqualTo(MarkerChange.Kept));
    }

    [Test]
    public void SixSpiceWithEveryMarkerOnTopOnlyKeeps()
    {
        var result = SpiceRules.Resolve(Content, Markers(5, 5, 5), 0, 6);

        Assert.That(result.SupremacyGained, Is.False);
        Assert.That(result.NewReserve, Is.Zero);
    }

    [Test]
    public void UnspendableSpiceGoesToTheReserveUpToOne()
    {
        var result = SpiceRules.Resolve(Content, Markers(5, 5, 5), 0, 5);

        Assert.That(result.NewReserve, Is.EqualTo(1));
        Assert.That(result.Wasted, Is.Zero);
        Assert.That(result.Markers.Count(m => m.Change == MarkerChange.Dropped), Is.EqualTo(1));
    }

    [Test]
    public void TheReserveIsSpentWithTheHarvest()
    {
        var result = SpiceRules.Resolve(Content, Markers(5, 5, 5), 1, 5);

        Assert.That(result.ReserveUsed, Is.EqualTo(1));
        Assert.That(result.Markers.Select(m => m.Change), Is.All.EqualTo(MarkerChange.Kept));
        Assert.That(result.NewReserve, Is.Zero);
    }

    [Test]
    public void MarkerOnTheBottomStepStaysThere()
    {
        var result = SpiceRules.Resolve(Content, Markers(1, 5, 5), 0, 0);

        Assert.That(Marker(result, ImperiumPower.Choam).Change, Is.EqualTo(MarkerChange.StayedAtBottom));
        Assert.That(Marker(result, ImperiumPower.Choam).To, Is.EqualTo(1));
    }
}
