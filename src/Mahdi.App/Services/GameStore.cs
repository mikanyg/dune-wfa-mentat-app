using Mahdi.Engine.Persistence;
using Microsoft.JSInterop;

namespace Mahdi.App.Services;

/// <summary>Keeps the current game in the browser's localStorage (seed plus event history).</summary>
public sealed class GameStore(IJSRuntime js)
{
    public const string Key = "mahdi.save.v1";

    public async Task<SaveData?> LoadAsync()
    {
        var json = await js.InvokeAsync<string?>("mahdi.storageGet", Key);
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            var save = SaveData.FromJson(json);
            return save.Version == SaveData.CurrentVersion ? save : null;
        }
        catch (System.Text.Json.JsonException)
        {
            // A corrupt or incompatible save is ignored rather than breaking the app.
            return null;
        }
    }

    public async Task<bool> SaveAsync(SaveData save) =>
        await js.InvokeAsync<bool>("mahdi.storageSet", Key, save.ToJson());

    public async Task ClearAsync() => await js.InvokeVoidAsync("mahdi.storageRemove", Key);
}
