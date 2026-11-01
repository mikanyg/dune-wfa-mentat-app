using Mahdi.Engine.Commands;
using Mahdi.Engine.Content;
using Mahdi.Engine.Model;
using Mahdi.Engine.Rules;

namespace Mahdi.Engine.Tests;

/// <summary>Returns scripted values (modulo the requested range), then repeats the last one.</summary>
internal sealed class ScriptedRandom(params int[] values) : IRandomSource
{
    private int index;

    public int Next(int maxExclusive)
    {
        var value = values.Length == 0 ? 0 : values[Math.Min(index, values.Length - 1)];
        index++;
        return value % maxExclusive;
    }
}

internal static class TestGame
{
    public static GameContent Content => ContentLoader.Default;

    public static Game New(DiceMode mode = DiceMode.AppRolls, int seed = 1) => new(Content, mode, seed);

    /// <summary>A game in Action Resolution of round 1.</summary>
    public static Game InActionResolution(DiceMode mode = DiceMode.AppRolls, int seed = 1)
    {
        var game = New(mode, seed);
        game.Execute(new CompleteSetup());
        if (mode == DiceMode.Physical)
        {
            EnterCardsAsTheAppWouldDraw(game, seed);
        }

        game.Execute(new ConfirmRoundStart());
        game.Execute(new ConfirmVehicles());
        return game;
    }

    /// <summary>
    /// Player-driven: enters the Tactical cards the app would have drawn with this seed (setup is command 0), so
    /// player-driven test games play on the same cards as app-driven ones.
    /// </summary>
    public static void EnterCardsAsTheAppWouldDraw(Game game, int seed)
    {
        var round = TacticalDeck.DrawRound(Content, GameReducer.Initial(Content, DiceMode.AppRolls), new SeededRandomSource(seed, 0), 1);
        game.Execute(new EnterHarvestCard(round.HarvestCardId!));
        foreach (var rejected in round.Rejected)
        {
            game.Execute(new EnterTargetCard(rejected));
        }

        game.Execute(new EnterTargetCard(round.TargetCardId!));
    }

    /// <summary>Plays out the rest of the Action Resolution phase with the given face (physical mode).</summary>
    public static void SpendAllDice(Game game)
    {
        while (game.State.Phase == Phase.ActionResolution)
        {
            if (game.State.DiceMode == DiceMode.AppRolls)
            {
                game.Execute(new RollHarkonnenDie());
            }
            else
            {
                var face = Enum.GetValues<DieFace>().First(f => game.State.Dice.UsedCount(f) < 3);
                game.Execute(new EnterHarkonnenDie(face));
            }

            game.Execute(new CompleteHarkonnenTurn());
        }
    }

    /// <summary>Plays rounds until the given round has started.</summary>
    public static void AdvanceToRound(Game game, int round)
    {
        if (game.State.Phase == Phase.Setup)
        {
            game.Execute(new CompleteSetup());
        }

        while (game.State.Round < round && game.State.Phase != Phase.GameOver)
        {
            if (game.State.Phase == Phase.RoundStart)
            {
                EnterCardsIfNeeded(game);
                game.Execute(new ConfirmRoundStart());
                game.Execute(new ConfirmVehicles());
            }

            SpendAllDice(game);
            game.Execute(new ConfirmDesertHazards());
            game.Execute(new HarvestSpice(0, 3));
            game.Execute(new EndRound());
        }

        EnterCardsIfNeeded(game);
    }

    /// <summary>Player-driven: enters a Harvesting card and the first eligible Target card if they are still missing.</summary>
    public static void EnterCardsIfNeeded(Game game)
    {
        if (TacticalDeck.AwaitingHarvest(game.State))
        {
            game.Execute(new EnterHarvestCard("rock-outcroppings"));
        }

        while (TacticalDeck.AwaitingTarget(Content, game.State))
        {
            var tactical = game.State.Tactical;
            var harvest = Content.Card(tactical.HarvestCardId!).Sector;
            var remaining = TacticalDeck.Remaining(Content, tactical);
            game.Execute(new EnterTargetCard(remaining.First(id => TacticalDeck.IsEligibleTarget(Content, tactical, id, harvest))));
        }
    }
}
