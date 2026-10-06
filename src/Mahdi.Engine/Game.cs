using System.Collections.Immutable;
using Mahdi.Engine.Commands;
using Mahdi.Engine.Events;
using Mahdi.Engine.Model;
using Mahdi.Engine.Persistence;
using Mahdi.Engine.Rules;

namespace Mahdi.Engine;

/// <summary>
/// A game in progress: the seed, the initial dice mode and the event history grouped per command.
/// State is always the replay of the history, which makes undo a matter of dropping the last command.
/// </summary>
public sealed class Game
{
    private readonly List<ImmutableArray<GameEvent>> history = [];

    public Game(GameContent content, DiceMode diceMode, int seed)
    {
        Content = content;
        InitialDiceMode = diceMode;
        Seed = seed;
        State = GameReducer.Initial(content, diceMode);
    }

    public GameContent Content { get; }

    public DiceMode InitialDiceMode { get; }

    public int Seed { get; }

    public GameState State { get; private set; }

    public IReadOnlyList<ImmutableArray<GameEvent>> History => history;

    public bool CanUndo => history.Count > 0;

    public ImmutableArray<GameEvent> Execute(GameCommand command)
    {
        var start = State with { Notices = [] };
        var events = CommandHandler.Handle(Content, start, command, new SeededRandomSource(Seed, history.Count));
        history.Add(events);
        State = events.Aggregate(start, (state, e) => GameReducer.Apply(Content, state, e));
        return events;
    }

    /// <summary>Executes the command, returning the rejection message instead of throwing.</summary>
    public bool TryExecute(GameCommand command, out string? error)
    {
        try
        {
            Execute(command);
            error = null;
            return true;
        }
        catch (CommandRejectedException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public void Undo()
    {
        if (!CanUndo)
        {
            return;
        }

        history.RemoveAt(history.Count - 1);
        State = Replay(Content, InitialDiceMode, history);
    }

    public SaveData ToSave() => new(SaveData.CurrentVersion, InitialDiceMode, Seed, [.. history]);

    public static Game FromSave(GameContent content, SaveData save)
    {
        var game = new Game(content, save.DiceMode, save.Seed);
        game.history.AddRange(save.History);
        game.State = Replay(content, save.DiceMode, game.history);
        return game;
    }

    private static GameState Replay(GameContent content, DiceMode mode, IEnumerable<ImmutableArray<GameEvent>> batches) =>
        batches.Aggregate(
            GameReducer.Initial(content, mode),
            (state, batch) => batch.Aggregate(state with { Notices = [] }, (s, e) => GameReducer.Apply(content, s, e)));
}
