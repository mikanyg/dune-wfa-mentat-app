using System.Collections.Immutable;
using Mahdi.Engine.Model;

namespace Mahdi.Engine.Rules;

/// <summary>
/// Solo Spice Harvesting (rulebook p. 38): spend all spice, including the Reserve, first to keep every
/// Imperium marker from dropping (lowest first), then to raise the lowest ones. With all markers on the
/// top step and 7+ spice, the excess scores 1 Supremacy instead. No Stockpiling; Reserve only for spice
/// that cannot be spent.
/// </summary>
public static class SpiceRules
{
    public const int KeepCost = 2;
    public const int RaiseCost = 3;
    public const int SupremacyThreshold = 7;
    public const int MaxReserve = 1;

    public static int Collected(int desertHarvesters, int deepDesertHarvesters) =>
        desertHarvesters + (2 * deepDesertHarvesters);

    public static SpiceResult Resolve(GameContent content, ImmutableDictionary<ImperiumPower, int> imperium, int reserve, int collected)
    {
        var available = collected + reserve;
        var order = imperium.OrderBy(m => m.Value).ThenBy(m => m.Key).Select(m => m.Key).ToList();
        var spent = order.ToDictionary(p => p, _ => 0);
        var allAtTop = imperium.Values.All(step => step == content.TopStep);

        foreach (var power in order)
        {
            if (available < KeepCost)
            {
                break;
            }

            spent[power] = KeepCost;
            available -= KeepCost;
        }

        foreach (var power in order.Where(p => spent[p] == KeepCost && imperium[p] < content.TopStep))
        {
            if (available < RaiseCost - KeepCost)
            {
                break;
            }

            spent[power] = RaiseCost;
            available -= RaiseCost - KeepCost;
        }

        var supremacy = allAtTop && collected + reserve >= SupremacyThreshold;
        if (supremacy)
        {
            available = 0;
        }

        var markers = order.Select(power => Outcome(content, power, imperium[power], spent[power])).ToImmutableArray();
        var newReserve = Math.Min(MaxReserve, available);

        return new SpiceResult(collected, reserve, markers, supremacy, newReserve, available - newReserve);
    }

    private static MarkerOutcome Outcome(GameContent content, ImperiumPower power, int from, int spent) => spent switch
    {
        RaiseCost => new MarkerOutcome(power, from, from + 1, spent, MarkerChange.Raised),
        KeepCost => new MarkerOutcome(power, from, from, spent, MarkerChange.Kept),
        _ when from > content.BottomStep => new MarkerOutcome(power, from, from - 1, 0, MarkerChange.Dropped),
        _ => new MarkerOutcome(power, from, from, 0, MarkerChange.StayedAtBottom),
    };
}
