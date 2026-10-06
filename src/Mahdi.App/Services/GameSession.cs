using Mahdi.Engine;
using Mahdi.Engine.Commands;
using Mahdi.Engine.Content;
using Mahdi.Engine.Guidance;
using Mahdi.Engine.Model;

namespace Mahdi.App.Services;

/// <summary>
/// The single source of truth for the UI: holds the current game, executes commands and
/// notifies components when the state changes.
/// </summary>
public sealed class GameSession
{
    public GameSession()
    {
        Content = ContentLoader.Default;
        Guidance = new GuidanceBuilder(Content);
    }

    public GameContent Content { get; }

    public GuidanceBuilder Guidance { get; }

    public Game? Game { get; private set; }

    public GameState? State => Game?.State;

    /// <summary>The message of the last rejected command, shown until the next successful one.</summary>
    public string? LastError { get; private set; }

    public event Action? Changed;

    public void StartNew(DiceMode mode, int seed)
    {
        Game = new Game(Content, mode, seed);
        LastError = null;
        Changed?.Invoke();
    }

    public void Attach(Game game)
    {
        Game = game;
        LastError = null;
        Changed?.Invoke();
    }

    public void Clear()
    {
        Game = null;
        LastError = null;
        Changed?.Invoke();
    }

    public bool Execute(GameCommand command)
    {
        if (Game is null)
        {
            return false;
        }

        var ok = Game.TryExecute(command, out var error);
        LastError = error;
        Changed?.Invoke();
        return ok;
    }

    public void Undo()
    {
        if (Game is { CanUndo: true })
        {
            Game.Undo();
            LastError = null;
            Changed?.Invoke();
        }
    }

    public void DismissError()
    {
        LastError = null;
        Changed?.Invoke();
    }
}
