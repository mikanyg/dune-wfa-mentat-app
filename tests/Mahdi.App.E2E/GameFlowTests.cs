using System.Text.RegularExpressions;

namespace Mahdi.App.E2E;

public sealed partial class GameFlowTests(string device) : DeviceTest(device)
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

            // The main action stays reachable without scrolling, also on the phone.
            await Expect(ById("complete-turn")).ToBeInViewportAsync();

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
        await Expect(ById("harvest-spice")).ToBeInViewportAsync();
        await Tap("harvest-spice");
        await ExpectPhaseAsync("EndOfRound");
        await Expect(ById("spice-summary")).ToHaveTextAsync("Spice: all markers kept");
        await Expect(ById("important-step")).ToContainTextAsync("from 0 to 1");

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
        await Expect(ById("turn-guidance")).Not.ToContainTextAsync("Ornithopter");

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
    public async Task AddingReinforcementsShowsTheDeckOrderAndKeepsTheNextDeck()
    {
        await StartGameAsync(seed: 20);
        await AdvanceToActionsAsync();

        await Tap("open-events");
        await Tap("ev-draw");
        await Expect(ById("draw-2")).ToContainTextAsync("Harkonnen, Corrino");
        await SnapshotAsync("add-reinforcements");
        await Tap("draw-2");

        await Expect(ById("reinforcements")).ToHaveAttributeAsync("data-count", "4");
        await Expect(ById("next-deck")).ToContainTextAsync("Harkonnen");
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

        await ReloadAsync();
        await ExpectPhaseAsync("ActionResolution");
        await Expect(ById("dice-unused")).ToHaveAttributeAsync("data-count", "6");

        await OpenAsync("");
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

        await ReloadAsync();
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

    [Test]
    public async Task CombatPowerLinkOnTheDeploymentCardOpensTheHelp()
    {
        await StartGameAsync(seed: 21, physicalDice: true);
        await AdvanceToActionsAsync();
        await Tap("face-Deployment");
        await Expect(ById("result-face")).ToHaveTextAsync("Deployment");
        await SnapshotAsync("deployment-combat-power-link");

        await ById("turn-guidance").GetByTestId("cp-link").First.ClickAsync();

        await Expect(ById("cp-sheet")).ToBeVisibleAsync();
        await Expect(ById("cp-example-total")).ToHaveTextAsync("7");
        await Expect(ById("cp-ties")).ToContainTextAsync("Sardaukar or Fedaykin Unit");
        await SnapshotAsync("combat-power-help");

        await Tap("cp-close");
        await Expect(ById("cp-sheet")).ToHaveCountAsync(0);
        await Expect(ById("result-card")).ToBeVisibleAsync();
    }

    [Test]
    public async Task CombatPowerHelpIsAvailableFromTheQuickActionsAndTheBattleSheet()
    {
        await StartGameAsync(seed: 22);
        await AdvanceToActionsAsync();

        await Tap("open-combat-power");
        await Expect(ById("cp-sheet")).ToBeVisibleAsync();
        await Tap("cp-close");
        await Expect(ById("cp-sheet")).ToHaveCountAsync(0);

        // A Harkonnen attack's battle notes link to the same help, which opens on top of the battle sheet.
        await Tap("open-battle");
        await Tap("battle-switch-attacker");
        await Tap("battle-target-AtreidesLegion");
        await Tap("battle-begin");
        await ById("battle-sheet").GetByTestId("cp-link").First.ClickAsync();
        await Expect(ById("cp-sheet")).ToBeVisibleAsync();
        await Tap("cp-close");
        await Expect(ById("battle-sheet")).ToBeVisibleAsync();
    }

    [Test]
    public async Task DeploymentLetsThePlayerChooseTheNamedLeader()
    {
        await StartGameAsync(seed: 23, physicalDice: true);
        await AdvanceToActionsAsync();

        // Beast Rabban must be deployed first, so there is nothing to choose.
        await Tap("face-Deployment");
        await Expect(ById("turn-guidance")).ToContainTextAsync("Beast Rabban");
        await Expect(ById("deploy-leader")).ToHaveCountAsync(0);
        await Tap("complete-turn");

        await Tap("face-Deployment");
        await Expect(ById("deploy-leader")).ToBeVisibleAsync();
        await Expect(ById("deploy-leader-BaronHarkonnen")).ToHaveAttributeAsync("aria-pressed", "true");
        await Tap("deploy-leader-CaptainAramsham");
        await Expect(ById("deploy-leader-CaptainAramsham")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(ById("deploy-leader-BaronHarkonnen")).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(ById("guidance-headline")).ToContainTextAsync("Captain Aramsham");
        await SnapshotAsync("deploy-leader-choice");
        await Tap("complete-turn");

        await OpenRailAsync();
        await Expect(ById("leader-CaptainAramsham")).ToContainTextAsync("on board");
        await Expect(ById("leader-BaronHarkonnen")).ToContainTextAsync("ready");
    }

    [Test]
    public async Task LeaderInReserveCanBeMarkedAsOnTheBoard()
    {
        await StartGameAsync(seed: 24, physicalDice: true);
        await AdvanceToActionsAsync();
        await OpenRailAsync();

        await ById("leader-BaronHarkonnen").GetByTestId("leader-chip").ClickAsync();
        await Tap("leader-on-board");
        await Expect(ById("leader-BaronHarkonnen")).ToContainTextAsync("on board");

        // Leaders on the board offer no "Placed on the board" action.
        await ById("leader-BaronHarkonnen").GetByTestId("leader-chip").ClickAsync();
        await Expect(ById("leader-on-board")).ToHaveCountAsync(0);
        await Expect(ById("leader-killed")).ToBeVisibleAsync();
        await SnapshotAsync("leader-on-board");

        await Tap("undo");
        await Expect(ById("leader-BaronHarkonnen")).ToContainTextAsync("ready");
    }

    private async Task OpenRailAsync()
    {
        if (IsPhone)
        {
            await Tap("rail-toggle");
        }
    }

    [Test]
    public async Task DesertHazardsExplainEachHazardAndLinkToTheHitsHelp()
    {
        await StartGameAsync(seed: 25);
        await AdvanceToActionsAsync();
        for (var turn = 1; turn <= 8; turn++)
        {
            await Tap("roll-die");
            await Tap("complete-turn");
        }

        await ExpectPhaseAsync("DesertHazards");
        await Expect(ById("hazards-section-Wormsigns")).ToContainTextAsync("This round's Harvesters went to the");
        await Expect(ById("hazards-section-Sandworms")).ToContainTextAsync("1 Carryall was placed this round");
        await Expect(ById("hazards-section-CoriolisStorms")).ToContainTextAsync("Roll 2 Combat dice");
        await Expect(ById("confirm-phase")).ToBeInViewportAsync();

        await ById("hazards-details-Wormsigns").Locator("summary").ClickAsync();
        await Expect(ById("hazards-details-Wormsigns")).ToContainTextAsync("Burrowing Sandworm");
        await SnapshotAsync("desert-hazards");

        await Tap("hz-link-Sandworm");
        await Expect(ById("hz-sheet")).ToHaveAttributeAsync("data-focus", "Sandworm");
        await Expect(ById("hz-attack-Sandworm")).ToHaveAttributeAsync("data-count", "4");
        await Expect(ById("hz-attack-Sandworm")).ToHaveClassAsync(FocusClass());
        await Expect(ById("hz-terrain-DeepDesert")).ToContainTextAsync("2");
        await Expect(ById("hz-face-Shield")).ToContainTextAsync("No effect");
        await Expect(ById("hz-example-total")).ToHaveTextAsync("3");
        await SnapshotAsync("desert-hazard-hits");
        await Tap("hz-close");
        await Expect(ById("hz-sheet")).ToHaveCountAsync(0);

        await Tap("hz-link-CoriolisStorm");
        await Expect(ById("hz-sheet")).ToHaveAttributeAsync("data-focus", "CoriolisStorm");
        await Expect(ById("hz-attack-CoriolisStorm")).ToHaveAttributeAsync("data-count", "2");
        await Expect(ById("hz-attack-CoriolisStorm")).ToHaveClassAsync(FocusClass());
        await Tap("hz-close");

        await Tap("confirm-phase");
        await ExpectPhaseAsync("SpiceHarvesting");
    }

    [GeneratedRegex(@"\bfocus\b")]
    private static partial Regex FocusClass();
    [Test]
    public async Task SpiceHarvestingCorrectsMarkersAndEndOfRoundIsItsOwnScreen()
    {
        await StartGameAsync(seed: 26);
        await AdvanceToActionsAsync();
        for (var turn = 1; turn <= 8; turn++)
        {
            await Tap("roll-die");
            await Tap("complete-turn");
        }

        await Tap("confirm-phase");
        await ExpectPhaseAsync("SpiceHarvesting");
        for (var i = 0; i < 5; i++)
        {
            await Tap("desert-plus");
        }

        // 5 spice with every marker on the top step: CHOAM and the Guild are kept, the Landsraad drops.
        await Expect(ById("spice-total")).ToHaveAttributeAsync("data-count", "5");
        await Expect(ById("spend-Landsraad")).ToHaveAttributeAsync("data-change", "Dropped");
        await Expect(ById("spend-reserve")).ToHaveAttributeAsync("data-value", "1");

        // A card lowered the Spacing Guild marker during the round: correct it, and the spending follows.
        await Tap("marker-SpacingGuild-4");
        await Expect(ById("marker-SpacingGuild")).ToHaveAttributeAsync("data-value", "4");
        await Expect(ById("marker-SpacingGuild-4")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(ById("marker-changed-SpacingGuild")).ToBeVisibleAsync();
        await Expect(ById("spend-SpacingGuild")).ToHaveAttributeAsync("data-change", "Raised");
        await Expect(ById("spend-SpacingGuild")).ToHaveAttributeAsync("data-value", "5");
        await Expect(ById("spend-reserve")).ToHaveAttributeAsync("data-value", "0");

        await ById("spice-rules").Locator("summary").ClickAsync();
        await Expect(ById("spice-rules")).ToContainTextAsync("lowest first");
        await Expect(ById("spice-rules")).Not.ToContainTextAsync("Stockpiling");
        await Expect(ById("harvest-spice")).ToBeInViewportAsync();
        await SnapshotAsync("spice-harvesting");

        await Tap("harvest-spice");
        await ExpectPhaseAsync("EndOfRound");
        await Expect(ById("spice-panel")).ToHaveCountAsync(0);
        await Expect(ById("spice-summary")).ToHaveTextAsync("Spice: Spacing Guild raised · Landsraad dropped · Landsraad Ban active");
        await Expect(ById("important-step")).ToContainTextAsync("Advance the Harkonnen Supremacy marker 1 step on the board: from 0 to 1");
        await Expect(ById("end-guidance")).ToContainTextAsync("Thufir Hawat enters play");
        await Expect(ById("confirm-phase")).ToBeInViewportAsync();
        await SnapshotAsync("end-of-round");

        await Tap("confirm-phase");
        await ExpectPhaseAsync("RoundStart");
        await Expect(ById("supremacy")).ToHaveAttributeAsync("data-value", "1");
    }
}
