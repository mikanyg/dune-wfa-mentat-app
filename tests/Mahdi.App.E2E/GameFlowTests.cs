namespace Mahdi.App.E2E;

public sealed class GameFlowTests(string device) : DeviceTest(device)
{
    [Test]
    public async Task FullRoundWithTheAppRollingTheDice()
    {
        await StartGameAsync(seed: 11);
        await SnapshotAsync("01-setup");
        await Tap("confirm-phase");
        await ExpectPhaseAsync("RoundStart");
        await Expect(ById("target-card")).ToBeVisibleAsync();
        await SnapshotAsync("02-round-start");
        await Tap("confirm-phase");
        await ExpectPhaseAsync("VehiclePlacement");
        await Expect(ById("vehicle-guidance")).ToContainTextAsync("3 Harvesters, 1 Carryall and 2 Ornithopters");
        await Tap("confirm-phase");
        await ExpectPhaseAsync("ActionResolution");
        await SnapshotAsync("03-war-table");

        for (var turn = 1; turn <= 8; turn++)
        {
            await Tap("roll-die");
            await Expect(ById("result-card")).ToBeVisibleAsync();
            await Expect(ById("turn-guidance")).Not.ToBeEmptyAsync();
            if (turn == 1)
            {
                await SnapshotAsync("04-result");
            }

            await Tap("complete-turn");
        }

        // FAQ: the phase ends as soon as the Harkonnens spend their last die.
        await ExpectPhaseAsync("DesertHazards");
        await Expect(ById("notice").First).ToContainTextAsync("Action Resolution is over");

        await Tap("confirm-phase");
        await ExpectPhaseAsync("SpiceHarvesting");
        for (var i = 0; i < 3; i++)
        {
            await Tap("deep-plus");
        }

        await Expect(ById("spice-total")).ToContainTextAsync("6 spice");
        await SnapshotAsync("05-spice");
        await Tap("harvest-spice");
        await ExpectPhaseAsync("EndOfRound");
        await Expect(ById("spice-result")).ToContainTextAsync("stays on step 5");

        await Tap("confirm-phase");
        await ExpectPhaseAsync("RoundStart");
        await Expect(ById("supremacy")).ToHaveAttributeAsync("data-value", "1");
        await Expect(ById("phase")).ToContainTextAsync("Round 2");

        // Supremacy step 1 brings Thufir Hawat into play.
        await Expect(ById("leader-ThufirHawat")).ToHaveCountAsync(1);
        await Expect(Page.GetByText("Thufir Hawat enters play")).ToBeVisibleAsync();
    }

    [Test]
    public async Task PhysicalDiceModeGreysOutAResultWithThreeSpentDice()
    {
        await StartGameAsync(seed: 12, physicalDice: true);
        await AdvanceToActionsAsync();
        await Expect(ById("face-picker")).ToBeVisibleAsync();

        for (var i = 0; i < 3; i++)
        {
            await Tap("face-Strategy");
            await Expect(ById("result-face")).ToHaveTextAsync("Strategy");
            await Tap("complete-turn");
        }

        await Expect(ById("face-Strategy")).ToBeDisabledAsync();
        await Expect(ById("face-Strategy")).ToContainTextAsync("roll again");
        await Expect(ById("face-Mentat")).ToBeEnabledAsync();
        await SnapshotAsync("physical-blocked");
    }

    [Test]
    public async Task NamedLeaderSpecialActionCanFallBackToTheRegularAction()
    {
        await StartGameAsync(seed: 13, physicalDice: true);
        await AdvanceToActionsAsync();

        await Tap("face-House");
        await Expect(ById("result-title")).ToHaveTextAsync("House: Baron Harkonnen");
        await Expect(ById("turn-guidance")).ToContainTextAsync("Replace 3 Regular Units");

        await Tap("special-toggle");
        await Expect(ById("turn-guidance")).ToContainTextAsync("Replace 2 Regular Units");
        await Tap("complete-turn");

        // The special was not used, so the Baron's card is still ready for the next House result.
        await Tap("face-House");
        await Expect(ById("result-title")).ToHaveTextAsync("House: Baron Harkonnen");
    }

    [Test]
    public async Task DestroyingTheTargetSietchDrawsANewTarget()
    {
        await StartGameAsync(seed: 14);
        await AdvanceToActionsAsync();
        var target = await ById("target-sietch").TextContentAsync();

        await Tap("open-sietch");
        await Expect(ById("event-sheet")).ToBeVisibleAsync();
        await Page.Locator(".choice:has(.choice-tag)").ClickAsync();
        await SnapshotAsync("sietch-destroyed");
        await Tap("rank-2");

        await Expect(ById("event-sheet")).ToHaveCountAsync(0);
        await Expect(ById("supremacy")).ToHaveAttributeAsync("data-value", "2");
        await Expect(ById("target-sietch")).Not.ToHaveTextAsync(target!);
        await Expect(Page.GetByText("New Target Sietch")).ToBeVisibleAsync();
    }

    [Test]
    public async Task BattleHelperDiscardsReinforcementsUpToSixDice()
    {
        await StartGameAsync(seed: 15);
        await AdvanceToActionsAsync();
        await Expect(ById("reinforcements")).ToHaveAttributeAsync("data-count", "2");

        await Tap("open-battle");
        await Tap("combat-dice-5");
        await Expect(ById("combat-discard")).ToHaveAttributeAsync("data-count", "1");
        await SnapshotAsync("battle");
        await Tap("combat-apply");

        await Expect(ById("reinforcements")).ToHaveAttributeAsync("data-count", "1");
    }

    [Test]
    public async Task UndoRevertsTheLastStep()
    {
        await StartGameAsync(seed: 16);
        await AdvanceToActionsAsync();

        await Tap("roll-die");
        await Expect(ById("result-card")).ToBeVisibleAsync();
        await Tap("undo");

        await Expect(ById("result-card")).ToHaveCountAsync(0);
        await Expect(ById("roll-die")).ToBeVisibleAsync();
        await Expect(ById("dice-unused")).ToHaveAttributeAsync("data-count", "8");
    }

    [Test]
    public async Task ReloadingResumesTheSavedGame()
    {
        await StartGameAsync(seed: 17);
        await AdvanceToActionsAsync();
        for (var i = 0; i < 2; i++)
        {
            await Tap("roll-die");
            await Tap("complete-turn");
        }

        await Expect(ById("dice-unused")).ToHaveAttributeAsync("data-count", "6");

        await Page.ReloadAsync();
        await ExpectPhaseAsync("ActionResolution");
        await Expect(ById("dice-unused")).ToHaveAttributeAsync("data-count", "6");

        await Page.GotoAsync("");
        await Expect(ById("continue-game")).ToContainTextAsync("Round 1");
    }

    [Test]
    public async Task ThemeChoiceIsRememberedAcrossVisits()
    {
        await StartGameAsync(seed: 18);
        await AdvanceToActionsAsync();
        var html = Page.Locator("html");
        await Expect(html).ToHaveAttributeAsync("data-theme", "light");
        await SnapshotAsync("theme-light");

        await Tap("theme-toggle");
        await Expect(html).ToHaveAttributeAsync("data-theme", "dark");
        await SnapshotAsync("theme-dark");

        await Page.ReloadAsync();
        await ExpectPhaseAsync("ActionResolution");
        await Expect(html).ToHaveAttributeAsync("data-theme", "dark");
    }

    [Test]
    public async Task StateRailCollapsesToAStripOnThePhone()
    {
        await StartGameAsync(seed: 19);
        await AdvanceToActionsAsync();

        if (IsPhone)
        {
            await Expect(ById("target-sietch")).ToBeHiddenAsync();
            await Tap("rail-toggle");
            await Expect(ById("target-sietch")).ToBeVisibleAsync();
            await SnapshotAsync("rail-open");
        }
        else
        {
            await Expect(ById("rail-toggle")).ToBeHiddenAsync();
            await Expect(ById("target-sietch")).ToBeVisibleAsync();
        }
    }
}
