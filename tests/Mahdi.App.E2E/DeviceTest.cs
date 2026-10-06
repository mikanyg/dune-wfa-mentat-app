using Microsoft.Playwright;

namespace Mahdi.App.E2E;

/// <summary>
/// Base class running every test on both target devices in landscape (WebKit, see e2e.runsettings).
/// </summary>
[TestFixture("iPad Pro 11 landscape")]
[TestFixture("iPhone 15 Pro landscape")]
[Parallelizable(ParallelScope.Self)]
public abstract class DeviceTest(string device) : PageTest
{
    protected string Device { get; } = device;

    protected bool IsPhone => Device.Contains("iPhone", StringComparison.Ordinal);

    public override BrowserNewContextOptions ContextOptions()
    {
        var options = new BrowserNewContextOptions(Playwright.Devices[Device])
        {
            BaseURL = AppServer.BaseUrl,
            ColorScheme = ColorScheme.Light,
        };
        return options;
    }

    protected ILocator ById(string testId) => Page.GetByTestId(testId);

    protected Task Tap(string testId) => ById(testId).First.ClickAsync();

    /// <summary>Starts a new game with a fixed seed and lands on the setup screen.</summary>
    protected async Task StartGameAsync(int seed, bool physicalDice = false)
    {
        await Page.GotoAsync($"new?seed={seed}");
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
        await Tap("confirm-phase");
        await ExpectPhaseAsync("VehiclePlacement");
        await Tap("confirm-phase");
        await ExpectPhaseAsync("ActionResolution");
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
        await Page.ScreenshotAsync(new() { Path = path });
        TestContext.AddTestAttachment(path, $"{Device}: {name}");
    }
}
