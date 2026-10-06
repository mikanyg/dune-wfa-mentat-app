using Mahdi.Engine.Commands;
using Mahdi.Engine.Content;
using Mahdi.Engine.Model;

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
        game.Execute(new ConfirmRoundStart());
        game.Execute(new ConfirmVehicles());
        return game;
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
                game.Execute(new ConfirmRoundStart());
                game.Execute(new ConfirmVehicles());
            }

            SpendAllDice(game);
            game.Execute(new ConfirmDesertHazards());
            game.Execute(new HarvestSpice(0, 3));
            game.Execute(new EndRound());
        }
    }
}
