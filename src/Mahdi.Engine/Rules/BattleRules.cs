using System.Collections.Immutable;
using Mahdi.Engine.Model;

namespace Mahdi.Engine.Rules;

public static class BattleRules
{
    /// <summary>Combat dice are capped at 6 per side.</summary>
    public const int MaxCombatDice = 6;

    /// <summary>During a Harkonnen turn (a result is being resolved) the Harkonnens attack, otherwise the Atreides.</summary>
    public static Attacker DefaultAttacker(GameState state) =>
        state.Dice.Pending is null ? Attacker.Atreides : Attacker.Harkonnen;

    /// <summary>The Harkonnens attack Sietches and Atreides Legions; the Atreides attack Harkonnen Legions and Settlements.</summary>
    public static ImmutableArray<BattleTarget> Targets(Attacker attacker) => attacker == Attacker.Harkonnen
        ? [BattleTarget.Sietch, BattleTarget.AtreidesLegion]
        : [BattleTarget.HarkonnenLegion, BattleTarget.Settlement];

    public static int SettlementRank(SettlementKind settlement) => settlement switch
    {
        SettlementKind.PyonVillage => 1,
        SettlementKind.Carthag => 2,
        SettlementKind.Arrakeen => 3,
        _ => throw new ArgumentOutOfRangeException(nameof(settlement), settlement, null),
    };

    /// <summary>
    /// Harkonnen Combat dice before Planning cards: one per Unit (not Leaders), plus the Settlement rank when
    /// the Harkonnens defend a Settlement, at most 6.
    /// </summary>
    public static int HarkonnenDiceBeforeCards(BattleState battle, int units) =>
        Math.Min(MaxCombatDice, units + (battle.Settlement is { } settlement && battle.Attacker == Attacker.Atreides
            ? SettlementRank(settlement)
            : 0));

    /// <summary>Discard Reinforcements cards until the Harkonnens roll 6 Combat dice (none under the Landsraad Ban).</summary>
    public static int SuggestedDiscard(GameState state, int diceBeforeCards) =>
        state.IsBanActive(ImperiumPower.Landsraad)
            ? 0
            : Math.Min(Math.Max(0, MaxCombatDice - diceBeforeCards), state.Reinforcements);

    /// <summary>Ways a battle can end for this attacker. The Harkonnens never retreat, so only an Atreides defender can.</summary>
    public static ImmutableArray<BattleOutcome> Outcomes(Attacker attacker) => attacker == Attacker.Harkonnen
        ?
        [
            BattleOutcome.AttackerCeased,
            BattleOutcome.DefenderRetreated,
            BattleOutcome.DefenderDestroyed,
            BattleOutcome.AttackerDestroyed,
            BattleOutcome.BothDestroyed,
        ]
        :
        [
            BattleOutcome.AttackerCeased,
            BattleOutcome.DefenderDestroyed,
            BattleOutcome.AttackerDestroyed,
            BattleOutcome.BothDestroyed,
        ];

    public static bool AttackerWins(BattleOutcome outcome) =>
        outcome is BattleOutcome.DefenderRetreated or BattleOutcome.DefenderDestroyed;

    /// <summary>A victorious Harkonnen attack on a Sietch destroys it.</summary>
    public static bool SietchFalls(BattleState battle, BattleOutcome outcome) =>
        battle is { Attacker: Attacker.Harkonnen, Target: BattleTarget.Sietch } && AttackerWins(outcome);

    /// <summary>A victorious Atreides attack on a Harkonnen Settlement destroys it.</summary>
    public static bool SettlementFalls(BattleState battle, BattleOutcome outcome) =>
        battle is { Attacker: Attacker.Atreides, Target: BattleTarget.Settlement } && AttackerWins(outcome);
}
