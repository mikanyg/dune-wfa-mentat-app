using Microsoft.Playwright;

namespace Mahdi.App.E2E;

/// <summary>
/// Base class for browser tests on a Playwright device profile (WebKit, see e2e.runsettings), with the shared helpers.
/// </summary>
public abstract class AppTest(string device) : PageTest
{
    protected string Device { get; } = device;

    protected bool IsPhone => Device.Contains("iPhone", StringComparison.Ordinal);

    /// <summary>
    /// A Playwright device profile, or "Desktop WIDTHxHEIGHT" for a desktop browser window of that size.
    /// </summary>
    public override BrowserNewContextOptions ContextOptions()
    {
        var options = Device.StartsWith(DesktopPrefix, StringComparison.Ordinal)
            ? new BrowserNewContextOptions { ViewportSize = DesktopViewport(Device[DesktopPrefix.Length..]) }
            : new BrowserNewContextOptions(Playwright.Devices[Device]);
        options.BaseURL = AppServer.BaseUrl;
        options.ColorScheme = ColorScheme.Light;
        return options;
    }

    private const string DesktopPrefix = "Desktop ";

    private static ViewportSize DesktopViewport(string size)
    {
        var parts = size.Split('x');
        return new ViewportSize { Width = int.Parse(parts[0]), Height = int.Parse(parts[1]) };
    }

    /// <summary>
    /// Downloading and starting the WebAssembly runtime can take a while on a busy CI runner, so startup
    /// gets a longer budget than ordinary steps. The header link renders once Blazor is running.
    /// </summary>
    private const float AppStartTimeoutMs = 120_000;

    /// <summary>Longer than the app's entry animations (sheets slide in for 200 ms).</summary>
    private const float SettleMs = 400;

    protected ILocator ById(string testId) => Page.GetByTestId(testId);

    /// <summary>Navigates to a page of the app and waits until Blazor has started.</summary>
    protected async Task OpenAsync(string url)
    {
        await Page.GotoAsync(url, new() { Timeout = AppStartTimeoutMs });
        await WaitForAppAsync();
    }

    /// <summary>Reloads the page and waits until Blazor has started again.</summary>
    protected async Task ReloadAsync()
    {
        await Page.ReloadAsync(new() { Timeout = AppStartTimeoutMs });
        await WaitForAppAsync();
    }

    private Task WaitForAppAsync() =>
        ById("home-link").WaitForAsync(new() { Timeout = AppStartTimeoutMs });

    protected Task Tap(string testId) => ById(testId).First.ClickAsync();

    /// <summary>Starts a new game with a fixed seed and lands on the setup screen.</summary>
    /// <param name="physicalDice">Player-driven mode: the player rolls the dice and draws the Tactical cards.</param>
    protected async Task StartGameAsync(int seed, bool physicalDice = false)
    {
        await OpenAsync($"new?seed={seed}");
        if (physicalDice)
        {
            await Tap("mode-physical");
        }

        await Tap("start-game");
        await ExpectPhaseAsync("Setup");
    }

    /// <summary>Setup, round start and vehicle placement: ends in Action Resolution.</summary>
    protected async Task AdvanceToActionsAsync()
    {
        await Tap("confirm-phase");
        await ExpectPhaseAsync("RoundStart");
        if (await ById("card-picker").CountAsync() > 0)
        {
            await EnterTacticalCardsAsync();
        }

        await Tap("confirm-phase");
        await ExpectPhaseAsync("VehiclePlacement");
        await Tap("confirm-phase");
        await ExpectPhaseAsync("ActionResolution");
    }

    /// <summary>Player-driven: taps the Harvesting card, then the Target card the player drew.</summary>
    protected async Task EnterTacticalCardsAsync(string harvest = "rock-outcroppings", string target = "sihaya-ridge")
    {
        await Expect(ById("card-picker")).ToHaveAttributeAsync("data-mode", "harvest");
        await Tap($"pick-{harvest}");
        await Expect(ById("card-picker")).ToHaveAttributeAsync("data-mode", "target");
        await Tap($"pick-{target}");
        await Expect(ById("card-picker")).ToHaveCountAsync(0);
    }

    protected Task ExpectPhaseAsync(string phase) =>
        Expect(ById("phase")).ToHaveAttributeAsync("data-phase", phase);

    protected async Task<int> AttributeAsync(string testId, string attribute) =>
        int.Parse(await ById(testId).GetAttributeAsync(attribute) ?? "-1");

    /// <summary>Saves a screenshot under the test output folder and attaches it to the test result.</summary>
    protected async Task SnapshotAsync(string name)
    {
        var folder = Path.Combine(TestContext.CurrentContext.WorkDirectory, "screenshots", Device.Replace(' ', '-'));
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"{name}.png");
        await Page.WaitForTimeoutAsync(SettleMs); // let sheet and notice animations finish
        await Page.ScreenshotAsync(new() { Path = path });
        TestContext.AddTestAttachment(path, $"{Device}: {name}");
    }
}
