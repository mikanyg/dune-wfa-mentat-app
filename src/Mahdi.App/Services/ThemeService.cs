using Microsoft.JSInterop;

namespace Mahdi.App.Services;

public enum ThemePreference
{
    System,
    Light,
    Dark,
}

/// <summary>Light "Desert Parchment" or dark "Arrakis Night", following the system unless overridden.</summary>
public sealed class ThemeService(IJSRuntime js)
{
    public ThemePreference Preference { get; private set; } = ThemePreference.System;

    public bool IsDark { get; private set; }

    public event Action? Changed;

    public async Task InitializeAsync()
    {
        var stored = await js.InvokeAsync<string>("mahdi.getThemePreference");
        Preference = Enum.TryParse<ThemePreference>(stored, ignoreCase: true, out var preference) ? preference : ThemePreference.System;
        IsDark = await js.InvokeAsync<string>("mahdi.initTheme") == "dark";
        Changed?.Invoke();
    }

    /// <summary>Flips between light and dark, storing an explicit preference.</summary>
    public Task ToggleAsync() => SetAsync(IsDark ? ThemePreference.Light : ThemePreference.Dark);

    public async Task SetAsync(ThemePreference preference)
    {
        Preference = preference;
        IsDark = await js.InvokeAsync<string>("mahdi.setThemePreference", preference.ToString().ToLowerInvariant()) == "dark";
        Changed?.Invoke();
    }
}
