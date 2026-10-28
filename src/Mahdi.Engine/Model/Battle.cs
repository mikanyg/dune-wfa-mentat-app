namespace Mahdi.Engine.Model;

public enum Attacker
{
    Harkonnen,
    Atreides,
}

/// <summary>What is attacked. The Harkonnens attack Sietches and Atreides Legions; the Atreides attack
/// Harkonnen Legions and Harkonnen Settlements.</summary>
public enum BattleTarget
{
    Sietch,
    AtreidesLegion,
    HarkonnenLegion,
    Settlement,
}

/// <summary>Harkonnen Settlements; the rank adds Combat dice for the defender.</summary>
public enum SettlementKind
{
    PyonVillage,
    Carthag,
    Arrakeen,
}

/// <summary>How a battle ended.</summary>
public enum BattleOutcome
{
    /// <summary>The attacker stopped attacking (the Harkonnens cease at half Combat Power or less).</summary>
    AttackerCeased,

    /// <summary>The defender retreated (only the Atreides retreat; the Harkonnens never do).</summary>
    DefenderRetreated,

    DefenderDestroyed,
    AttackerDestroyed,
    BothDestroyed,

    /// <summary>Closed before any round was fought (started by mistake).</summary>
    Cancelled,
}

/// <summary>A battle in progress. Stored in the game state so it survives reloads and undo works per round.</summary>
/// <param name="Round">The Combat round being fought, starting at 1.</param>
/// <param name="SurpriseAttack">A Leadership attack: +1 Hit in the first round.</param>
/// <param name="CardsDiscarded">Reinforcements cards discarded so far in this battle.</param>
public sealed record BattleState(
    Attacker Attacker,
    BattleTarget Target,
    string? SietchCardId,
    SettlementKind? Settlement,
    bool SurpriseAttack,
    int Round,
    int CardsDiscarded);
