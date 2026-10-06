using System.Collections.Immutable;
using Mahdi.Engine.Events;
using Mahdi.Engine.Model;

namespace Mahdi.Engine.Rules;

/// <summary>
/// The 8 Tactical cards: the first card drawn sets the Harvesting Sector, the second the Target Sietch.
/// A Target card must show a different Sector and a Sietch still in play.
/// </summary>
public static class TacticalDeck
{
    public static ImmutableArray<string> Remaining(GameContent content, TacticalState tactical) =>
    [
        .. content.TacticalCards
            .Select(c => c.Id)
            .Where(id => id != tactical.HarvestCardId && id != tactical.TargetCardId && !tactical.Discard.Contains(id)),
    ];

    public static bool IsEligibleTarget(GameContent content, TacticalState tactical, string cardId, Sector harvestSector) =>
        content.Card(cardId).Sector != harvestSector && !tactical.DestroyedSietches.Contains(cardId);

    /// <summary>Draws the Harvesting Sector and Target Sietch cards from a freshly shuffled deck.</summary>
    public static RoundStarted DrawRound(GameContent content, GameState state, IRandomSource random, int round)
    {
        var deck = content.TacticalCards.Select(c => c.Id).ToList();
        var harvest = Take(deck, random);
        var (target, rejected) = DrawTargetFrom(content, state.Tactical, deck, content.Card(harvest).Sector, random);
        return new RoundStarted(round, harvest, target, rejected);
    }

    /// <summary>Replaces a destroyed Target Sietch, reshuffling the discards if no eligible card is left.</summary>
    public static TargetSietchDrawn RedrawTarget(GameContent content, GameState state, IRandomSource random)
    {
        var tactical = state.Tactical;
        var harvestSector = content.Card(tactical.HarvestCardId!).Sector;
        var deck = Remaining(content, tactical).ToList();
        var reshuffled = false;

        if (!deck.Any(id => IsEligibleTarget(content, tactical, id, harvestSector)))
        {
            var reshuffledDeck = content.TacticalCards
                .Select(c => c.Id)
                .Where(id => id != tactical.HarvestCardId && id != tactical.TargetCardId)
                .ToList();

            if (!reshuffledDeck.Any(id => IsEligibleTarget(content, tactical, id, harvestSector)))
            {
                return new TargetSietchDrawn(null, [], false);
            }

            deck = reshuffledDeck;
            reshuffled = true;
        }

        var (target, rejected) = DrawTargetFrom(content, tactical, deck, harvestSector, random);
        return new TargetSietchDrawn(target, rejected, reshuffled);
    }

    private static (string? Target, ImmutableArray<string> Rejected) DrawTargetFrom(
        GameContent content, TacticalState tactical, List<string> deck, Sector harvestSector, IRandomSource random)
    {
        if (!deck.Any(id => IsEligibleTarget(content, tactical, id, harvestSector)))
        {
            return (null, []);
        }

        var rejected = ImmutableArray.CreateBuilder<string>();
        while (true)
        {
            var card = Take(deck, random);
            if (IsEligibleTarget(content, tactical, card, harvestSector))
            {
                return (card, rejected.ToImmutable());
            }

            rejected.Add(card);
        }
    }

    private static string Take(List<string> deck, IRandomSource random)
    {
        var index = random.Next(deck.Count);
        var card = deck[index];
        deck.RemoveAt(index);
        return card;
    }
}
