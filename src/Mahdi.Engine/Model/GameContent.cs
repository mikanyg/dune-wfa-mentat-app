using System.Collections.Immutable;

namespace Mahdi.Engine.Model;

/// <summary>Static game data (dice, boards, cards) loaded from the embedded content JSON.</summary>
public sealed record GameContent(
    DiceContent Dice,
    ImmutableArray<SpiceMustFlowRow> SpiceMustFlow,
    SupremacyTrack Supremacy,
    ImmutableArray<TacticalCard> TacticalCards,
    ImmutableArray<LeaderDefinition> Leaders,
    ImmutableArray<BanDefinition> Bans)
{
    public SpiceMustFlowRow RowForStep(int step) => SpiceMustFlow.Single(r => r.Step == step);

    public int TopStep => SpiceMustFlow.Max(r => r.Step);

    public int BottomStep => SpiceMustFlow.Min(r => r.Step);

    public TacticalCard Card(string id) => TacticalCards.Single(c => c.Id == id);

    public LeaderDefinition Leader(LeaderId id) => Leaders.Single(l => l.Id == id);

    public BanDefinition Ban(ImperiumPower power) => Bans.Single(b => b.Power == power);
}

/// <param name="Faces">The six faces of one Harkonnen Action die.</param>
/// <param name="DiceCount">Total Harkonnen Action dice.</param>
/// <param name="SlotsPerResult">Dashboard slots per result; 3 spent dice of a result force a reroll.</param>
/// <param name="TruthtranceLimits">Spent-dice count at which Truthtrance can no longer pick a result.</param>
public sealed record DiceContent(
    ImmutableArray<DieFace> Faces,
    int DiceCount,
    int SlotsPerResult,
    ImmutableDictionary<DieFace, int> TruthtranceLimits);

/// <param name="Step">Imperium step; the highest step is the top row where markers start.</param>
/// <param name="DiceSetAside">Action dice set aside when this is the active row (cumulative).</param>
public sealed record SpiceMustFlowRow(int Step, int DiceSetAside, int Harvesters, int Ornithopters, int Carryalls);

/// <param name="Max">Supremacy needed for a Harkonnen victory.</param>
/// <param name="AtreidesBeneGesseritSteps">Steps that give the Atreides a Bene Gesserit token.</param>
public sealed record SupremacyTrack(int Max, ImmutableArray<int> AtreidesBeneGesseritSteps);

public sealed record TacticalCard(string Id, string Sietch, Sector Sector);

/// <param name="Face">The die result whose Action box holds the Leader card.</param>
/// <param name="EntryStep">Supremacy step that brings the Leader into play (for <see cref="LeaderEntry.SupremacyStep"/>).</param>
/// <param name="Replaces">Leader removed from the game when this one enters.</param>
/// <param name="RequiresOnBoard">The special action uses the Leader's own Legion.</param>
/// <param name="DeployFirst">Must be deployed before any other Named Leader.</param>
public sealed record LeaderDefinition(
    LeaderId Id,
    string Name,
    PlanningDeck Deck,
    DieFace Face,
    LeaderEntry Entry,
    int? EntryStep,
    LeaderId? Replaces,
    bool RequiresOnBoard,
    bool DeployFirst,
    bool GrantsBeneGesserit,
    string Special);

public sealed record BanDefinition(ImperiumPower Power, string Name, string Title, string Effect, string SoloEffect);
