namespace Mahdi.App.E2E;

/// <summary>Battles fought over several Combat rounds and their outcomes, for both attacking sides.</summary>
public sealed partial class BattleFlowTests(string device) : DeviceTest(device)
{
    [GeneratedRegex("in-tank")]
    private static partial Regex InTankClass();

    /// <summary>Starts a Harkonnen turn with the given result (physical dice) and opens the battle sheet.</summary>
    private async Task OpenHarkonnenBattleAsync(int seed, string face = "Strategy")
    {
        await StartGameAsync(seed, physicalDice: true);
        await AdvanceToActionsAsync();
        await Tap($"face-{face}");
        await Expect(ById("result-card")).ToBeVisibleAsync();
        await Tap("open-battle");
        await Expect(ById("battle-attacker")).ToHaveAttributeAsync("data-attacker", "Harkonnen");
    }

    /// <summary>Opens the battle sheet during the Atreides turn (no Harkonnen result showing).</summary>
    private async Task OpenAtreidesBattleAsync(int seed)
    {
        await StartGameAsync(seed, physicalDice: true);
        await AdvanceToActionsAsync();
        await Tap("open-battle");
        await Expect(ById("battle-attacker")).ToHaveAttributeAsync("data-attacker", "Atreides");
    }

    private async Task FightRoundAsync(int units, int discard, string? lastDeck = null)
    {
        await Tap($"battle-units-{units}");
        var current = int.Parse(await ById("battle-discard").GetAttributeAsync("data-count") ?? "0");
        for (; current > discard; current--)
        {
            await Tap("battle-discard-minus");
        }

        await Expect(ById("battle-discard")).ToHaveAttributeAsync("data-count", discard.ToString());
        if (lastDeck is not null)
        {
            await Tap($"battle-last-{lastDeck}");
        }
    }

    private Task ExpectRoundAsync(int round) =>
        Expect(ById("battle-title")).ToHaveTextAsync($"Battle · Round {round}");

    [Test]
    public async Task HarkonnenAttackOnASietchOverThreeRoundsDestroysIt()
    {
        await OpenHarkonnenBattleAsync(seed: 31);
        await Expect(ById("battle-target-Sietch")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(ById("battle-begin")).ToBeEnabledAsync(); // the Target Sietch is preselected
        await SnapshotAsync("battle-start-harkonnen");
        await Tap("battle-begin");

        // Round 1: 4 Units, the app suggests discarding 2 to reach 6 dice; discard only 1.
        await ExpectRoundAsync(1);
        await Tap("battle-units-4");
        await Expect(ById("battle-discard")).ToHaveAttributeAsync("data-count", "2");
        await Tap("battle-discard-minus");
        await Expect(ById("battle-next-round")).ToBeDisabledAsync(); // the last discarded deck is still missing
        await Tap("battle-last-Harkonnen");
        await SnapshotAsync("battle-round");
        await Tap("battle-next-round");

        // Round 2: 5 Units, discard the last card.
        await ExpectRoundAsync(2);
        await FightRoundAsync(units: 5, discard: 1, lastDeck: "Corrino");
        await Tap("battle-next-round");

        // Round 3: no cards left; the Atreides defenders are destroyed.
        await ExpectRoundAsync(3);
        await Tap("battle-units-6");
        await Expect(ById("battle-discard")).ToHaveAttributeAsync("data-count", "0");
        await Tap("battle-outcome-DefenderDestroyed");
        await Expect(ById("battle-result")).ToContainTextAsync("after 3 rounds");
        await Expect(ById("battle-confirm")).ToBeDisabledAsync(); // the Sietch rank is required
        await Tap("battle-sietch-rank-2");
        await SnapshotAsync("battle-result");
        await Tap("battle-confirm");

        await Expect(ById("battle-sheet")).ToHaveCountAsync(0);
        await Expect(ById("battle-bar")).ToHaveCountAsync(0);
        await Expect(ById("supremacy")).ToHaveAttributeAsync("data-value", "2");
        await Expect(ById("reinforcements")).ToHaveAttributeAsync("data-count", "0");
        await Expect(ById("next-deck")).ToContainTextAsync("Harkonnen"); // last discard was Corrino
        await Expect(Page.GetByText("New Target Sietch")).ToBeVisibleAsync();
    }

    [TestCase("AttackerCeased", "0")]
    [TestCase("DefenderRetreated", "1")]
    [TestCase("AttackerDestroyed", "0")]
    [TestCase("BothDestroyed", "0")]
    public async Task HarkonnenAttackOutcomes(string outcome, string supremacy)
    {
        await OpenHarkonnenBattleAsync(seed: 32);
        await Tap("battle-begin");
        await FightRoundAsync(units: 3, discard: 0);
        await Tap("battle-next-round");
        await ExpectRoundAsync(2);

        await Tap($"battle-outcome-{outcome}");
        if (outcome == "DefenderRetreated")
        {
            await Tap("battle-sietch-rank-1"); // the Atreides gave up the Sietch
        }
        else
        {
            await Expect(ById("battle-sietch-rank-1")).ToHaveCountAsync(0);
        }

        await Tap("battle-confirm");

        await Expect(ById("battle-sheet")).ToHaveCountAsync(0);
        await Expect(ById("supremacy")).ToHaveAttributeAsync("data-value", supremacy);
    }

    [Test]
    public async Task AtreidesAttackOnCarthagOverTwoRoundsDestroysIt()
    {
        await OpenAtreidesBattleAsync(seed: 33);
        await Expect(ById("battle-target-Sietch")).ToHaveCountAsync(0); // the Atreides never attack Sietches
        await Tap("battle-target-Settlement");
        await Expect(ById("battle-begin")).ToBeDisabledAsync();
        await Tap("battle-settlement-Carthag");
        await SnapshotAsync("battle-start-atreides");
        await Tap("battle-begin");

        // Carthag's rank 2 is added to the Harkonnen dice: 3 Units give 5 dice, so discard 1.
        await Tap("battle-units-3");
        await Expect(ById("battle-dice")).ToHaveAttributeAsync("data-count", "5");
        await Expect(ById("battle-discard")).ToHaveAttributeAsync("data-count", "1");
        await Tap("battle-last-Harkonnen");
        await Expect(ById("battle-outcome-DefenderRetreated")).ToHaveCountAsync(0); // the Harkonnens never retreat
        await Tap("battle-next-round");

        await ExpectRoundAsync(2);
        await FightRoundAsync(units: 2, discard: 1, lastDeck: "Corrino");
        await Tap("battle-outcome-DefenderDestroyed");
        await Expect(ById("battle-settlement-falls")).ToContainTextAsync("Carthag is destroyed");
        await Tap("battle-confirm");

        await Expect(ById("battle-sheet")).ToHaveCountAsync(0);
        await Expect(Page.GetByText("Carthag is destroyed")).ToBeVisibleAsync();
        await Expect(ById("reinforcements")).ToHaveAttributeAsync("data-count", "0");
        await Expect(ById("supremacy")).ToHaveAttributeAsync("data-value", "0");
    }

    [TestCase("AttackerCeased")]
    [TestCase("AttackerDestroyed")]
    [TestCase("BothDestroyed")]
    public async Task AtreidesAttackOutcomesOnALegion(string outcome)
    {
        await OpenAtreidesBattleAsync(seed: 34);
        await Tap("battle-target-HarkonnenLegion");
        await Tap("battle-begin");
        await FightRoundAsync(units: 4, discard: 0);
        await Tap("battle-next-round");

        await Tap($"battle-outcome-{outcome}");
        await Expect(ById("battle-settlement-falls")).ToHaveCountAsync(0);
        await Tap("battle-confirm");

        await Expect(ById("battle-sheet")).ToHaveCountAsync(0);
        await Expect(ById("supremacy")).ToHaveAttributeAsync("data-value", "0");
    }

    [Test]
    public async Task AKilledNamedLeaderGoesToTheRegenerationTank()
    {
        // Deploy Beast Rabban first, then the Atreides destroy his Legion.
        await StartGameAsync(seed: 35, physicalDice: true);
        await AdvanceToActionsAsync();
        await Tap("face-Deployment");
        await Tap("complete-turn");

        await Tap("open-battle");
        await Tap("battle-target-HarkonnenLegion");
        await Tap("battle-begin");
        await FightRoundAsync(units: 3, discard: 0);
        await Tap("battle-outcome-DefenderDestroyed");
        await Tap("battle-killed-BeastRabban");
        await Tap("battle-confirm");

        await Expect(ById("leader-BeastRabban")).ToHaveClassAsync(InTankClass());
    }

    [Test]
    public async Task ArrakeenFallingPromptsTheSecretObjectiveCheck()
    {
        await OpenAtreidesBattleAsync(seed: 36);
        await Tap("battle-target-Settlement");
        await Tap("battle-settlement-Arrakeen");
        await Tap("battle-begin");
        await FightRoundAsync(units: 2, discard: 0);
        await Tap("battle-outcome-DefenderDestroyed");

        await Expect(ById("battle-settlement-falls")).ToContainTextAsync("Secret Objective");
        await Tap("battle-confirm");
        await Expect(Page.GetByText("Arrakeen has fallen")).ToBeVisibleAsync();
    }

    [Test]
    public async Task TheAttackerCanBeSwitchedForCardEffects()
    {
        await OpenHarkonnenBattleAsync(seed: 37);
        await Expect(ById("battle-target-Settlement")).ToHaveCountAsync(0);

        await Tap("battle-switch-attacker");

        await Expect(ById("battle-attacker")).ToHaveAttributeAsync("data-attacker", "Atreides");
        await Expect(ById("battle-target-Settlement")).ToBeVisibleAsync();
        await Expect(ById("battle-target-Sietch")).ToHaveCountAsync(0);
    }

    [Test]
    public async Task AnOpenBattleSurvivesMinimisingReloadingAndUndo()
    {
        await OpenHarkonnenBattleAsync(seed: 38);
        await Tap("battle-target-AtreidesLegion");
        await Tap("battle-begin");
        await FightRoundAsync(units: 4, discard: 0);
        await Tap("battle-next-round");
        await ExpectRoundAsync(2);

        await Tap("battle-minimise");
        await Expect(ById("battle-bar")).ToContainTextAsync("round 2");
        await SnapshotAsync("battle-bar");

        await ReloadAsync();
        await Expect(ById("battle-bar")).ToContainTextAsync("round 2");

        await Tap("undo"); // steps back one round
        await Expect(ById("battle-bar")).ToContainTextAsync("round 1");

        await Tap("battle-bar");
        await ExpectRoundAsync(1);
    }

    [Test]
    public async Task ABattleStartedByMistakeCanBeCancelled()
    {
        await OpenAtreidesBattleAsync(seed: 39);
        await Tap("battle-begin");
        await ExpectRoundAsync(1);

        await Tap("battle-cancel");

        await Expect(ById("battle-attacker")).ToBeVisibleAsync(); // back on the start screen
        await Tap("battle-close");
        await Expect(ById("battle-bar")).ToHaveCountAsync(0);
    }
}
