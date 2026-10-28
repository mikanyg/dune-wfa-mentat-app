using System.Collections.Immutable;
using Mahdi.Engine.Model;
using Mahdi.Engine.Rules;

namespace Mahdi.Engine.Guidance;

/// <summary>Player-facing texts for the battle sheet (rulebook p. 41 and the core battle rules).</summary>
public static class BattleGuidance
{
    public static string AttackerName(Attacker attacker) =>
        attacker == Attacker.Harkonnen ? "The Harkonnens" : "The Atreides";

    public static string TargetName(BattleTarget target) => target switch
    {
        BattleTarget.Sietch => "A Sietch",
        BattleTarget.AtreidesLegion => "An Atreides Legion",
        BattleTarget.HarkonnenLegion => "A Harkonnen Legion",
        BattleTarget.Settlement => "A Harkonnen Settlement",
        _ => target.ToString(),
    };

    public static string SettlementLabel(SettlementKind settlement) => settlement switch
    {
        SettlementKind.PyonVillage => "Pyon Village",
        SettlementKind.Carthag => "Carthag",
        SettlementKind.Arrakeen => "Arrakeen",
        _ => settlement.ToString(),
    };

    /// <summary>One line naming who attacks what, e.g. "The Harkonnens attack Gara Kulon".</summary>
    public static string Headline(GameContent content, BattleState battle)
    {
        var what = battle.Target switch
        {
            BattleTarget.Sietch when battle.SietchCardId is { } id => content.Card(id).Sietch,
            BattleTarget.Settlement when battle.Settlement is { } settlement => SettlementLabel(settlement),
            BattleTarget.AtreidesLegion => "an Atreides Legion",
            BattleTarget.HarkonnenLegion => "a Harkonnen Legion",
            _ => "the defenders",
        };

        return $"{AttackerName(battle.Attacker)} attack {what}";
    }

    public static string OutcomeLabel(Attacker attacker, BattleOutcome outcome) => (attacker, outcome) switch
    {
        (Attacker.Harkonnen, BattleOutcome.AttackerCeased) => "Harkonnens cease (Combat Power half or less)",
        (Attacker.Atreides, BattleOutcome.AttackerCeased) => "Atreides stop attacking",
        (_, BattleOutcome.DefenderRetreated) => "Atreides retreat",
        (Attacker.Harkonnen, BattleOutcome.DefenderDestroyed) => "Atreides defenders destroyed",
        (Attacker.Atreides, BattleOutcome.DefenderDestroyed) => "Harkonnen Legion destroyed",
        (Attacker.Harkonnen, BattleOutcome.AttackerDestroyed) => "Harkonnen Legion destroyed",
        (Attacker.Atreides, BattleOutcome.AttackerDestroyed) => "Atreides Legion destroyed",
        (_, BattleOutcome.BothDestroyed) => "Both Legions destroyed",
        _ => outcome.ToString(),
    };

    /// <summary>The rules that apply to this battle, shown on every round.</summary>
    public static ImmutableArray<string> RoundNotes(BattleState battle)
    {
        var notes = ImmutableArray.CreateBuilder<string>();

        if (battle.SurpriseAttack && battle.Round == 1)
        {
            notes.Add("Surprise Attack (Leadership): the Harkonnens score +1 Hit in this first round.");
        }

        if (battle.Attacker == Attacker.Harkonnen)
        {
            notes.Add("At the start of each round the Harkonnens cease attacking if their Combat Power is half or less of the defenders'.");
            notes.Add(battle.Target == BattleTarget.Sietch
                ? "Against a Sietch the Harkonnens do not take 1 Hit to continue the battle."
                : "The Harkonnens never retreat during a battle.");
            notes.Add("If the Atreides retreat, you choose where, but empty Areas come first.");
        }
        else
        {
            notes.Add("The Harkonnens never retreat during a battle.");
            if (battle.Settlement is { } settlement)
            {
                notes.Add($"{SettlementLabel(settlement)} adds its rank ({BattleRules.SettlementRank(settlement)}) to the Harkonnen Combat dice.");
                notes.Add("To continue the battle against a Settlement, the Atreides take 1 Hit.");
            }
        }

        return notes.ToImmutable();
    }
}
