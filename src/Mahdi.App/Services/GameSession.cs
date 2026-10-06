using Mahdi.Engine;
using Mahdi.Engine.Commands;
using Mahdi.Engine.Content;
using Mahdi.Engine.Guidance;
using Mahdi.Engine.Model;

namespace Mahdi.App.Services;

/// <summary>
/// The single source of truth for the UI: holds the current game, executes commands, saves after
/// every change and notifies components when the state changes.
/// </summary>
public sealed class GameSession(GameStore store)
{
    private Task? loading;

    public GameContent Content { get; } = ContentLoader.Default;

    public GuidanceBuilder Guidance { get; } = new(ContentLoader.Default);

    public Game? Game { get; private set; }

    public GameState? State => Game?.State;

    public bool IsLoaded { get; private set; }

    /// <summary>The message of the last rejected command, shown until the next successful one.</summary>
    public string? LastError { get; private set; }

    public event Action? Changed;

    /// <summary>Restores the saved game once per app start.</summary>
    public Task EnsureLoadedAsync() => loading ??= LoadAsync();

    public void StartNew(DiceMode mode, int seed)
    {
        Game = new Game(Content, mode, seed);
        LastError = null;
        Persist();
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
        if (ok)
        {
            Persist();
        }

        Changed?.Invoke();
        return ok;
    }

    public void Undo()
    {
        if (Game is { CanUndo: true })
        {
            Game.Undo();
            LastError = null;
            Persist();
            Changed?.Invoke();
        }
    }

    public void DismissError()
    {
        LastError = null;
        Changed?.Invoke();
    }

    private async Task LoadAsync()
    {
        try
        {
            if (Game is null && await store.LoadAsync() is { } save)
            {
                Game = Game.FromSave(Content, save);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A save that no longer replays (for example after a rules fix) must not block the app.
            Console.Error.WriteLine($"Could not restore the saved game: {ex.Message}");
            Game = null;
        }

        IsLoaded = true;
        Changed?.Invoke();
    }

    private async void Persist()
    {
        try
        {
            if (Game is not null)
            {
                await store.SaveAsync(Game.ToSave());
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Could not save the game: {ex.Message}");
        }
    }
}
