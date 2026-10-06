using System.Collections.Immutable;

namespace Mahdi.Engine.Model;

/// <summary>
/// Everything the app knows about the automa. Board geometry is never stored: the player owns the map.
/// </summary>
public sealed record GameState
{
    public required DiceMode DiceMode { get; init; }

    public Phase Phase { get; init; } = Phase.Setup;

    public int Round { get; init; }

    public int Supremacy { get; init; }

    public required ImmutableDictionary<ImperiumPower, int> Imperium { get; init; }

    public ImmutableHashSet<ImperiumPower> ActiveBans { get; init; } = [];

    public int SpiceReserve { get; init; }

    public required TacticalState Tactical { get; init; }

    public DiceState Dice { get; init; } = new();

    public int Reinforcements { get; init; }

    /// <summary>The deck the next Harkonnen Planning card is drawn from (the decks alternate).</summary>
    public PlanningDeck NextDeck { get; init; } = PlanningDeck.Harkonnen;

    public required ImmutableDictionary<LeaderId, LeaderState> Leaders { get; init; }

    /// <summary>A first Hawat's Scheming card lies next to the board.</summary>
    public bool HawatsSchemingOnTable { get; init; }

    public SpiceResult? LastSpiceResult { get; init; }

    public GameOutcome? Outcome { get; init; }

    /// <summary>Messages raised by the most recent command (Leader entries, Bans, reminders).</summary>
    public ImmutableArray<Notice> Notices { get; init; } = [];

    public int LowestImperiumStep => Imperium.Values.Min();

    public bool IsBanActive(ImperiumPower power) => ActiveBans.Contains(power);
}

/// <summary>
/// Tactical cards in play. The facedown deck is every card that is neither in play nor discarded,
/// and cards are drawn from it at random.
/// </summary>
/// <param name="Discard">Cards drawn and set aside this round (rejected or replaced).</param>
public sealed record TacticalState(
    ImmutableArray<string> Discard,
    string? HarvestCardId,
    string? TargetCardId,
    ImmutableHashSet<string> DestroyedSietches)
{
    public static TacticalState Empty { get; } = new([], null, null, []);
}

public sealed record DiceState
{
    /// <summary>Dice placed on The Spice Must Flow board this round.</summary>
    public int SetAside { get; init; }

    public int Unused { get; init; }

    public ImmutableArray<DieFace> UsedFaces { get; init; } = [];

    /// <summary>Number of Harkonnen turns taken this round.</summary>
    public int TurnsTaken { get; init; }

    /// <summary>The die result being resolved, if a Harkonnen turn is in progress.</summary>
    public PendingTurn? Pending { get; init; }

    /// <summary>Result chosen through Truthtrance for the next Harkonnen turn.</summary>
    public DieFace? ForcedFace { get; init; }

    public int UsedCount(DieFace face) => UsedFaces.Count(f => f == face);
}

/// <param name="Rerolled">Results that were rolled but blocked by 3 spent dice showing them.</param>
/// <param name="SpecialLeader">Named Leader whose special action applies to this result, if any.</param>
/// <param name="DeployLeader">Named Leader to deploy on a Deployment result (null means a Bashar).</param>
public sealed record PendingTurn(
    DieFace Face,
    ImmutableArray<DieFace> Rerolled,
    bool Forced,
    LeaderId? SpecialLeader,
    LeaderId? DeployLeader,
    PlanningDeck StartDeck);

public sealed record LeaderState(LeaderStatus Status, bool CardSpent, int TankPosition)
{
    public static LeaderState NotInPlay { get; } = new(LeaderStatus.NotInPlay, false, 0);

    public static LeaderState InReserve { get; } = new(LeaderStatus.InReserve, false, 0);

    public bool IsActive => Status is LeaderStatus.InReserve or LeaderStatus.OnBoard;
}

/// <summary>Outcome of the Spice Harvesting phase, per Imperium marker.</summary>
public sealed record SpiceResult(
    int Collected,
    int ReserveUsed,
    ImmutableArray<MarkerOutcome> Markers,
    bool SupremacyGained,
    int NewReserve,
    int Wasted);

public enum MarkerChange
{
    Raised,
    Kept,
    Dropped,
    StayedAtBottom,
}

public sealed record MarkerOutcome(ImperiumPower Power, int From, int To, int Spent, MarkerChange Change);

public sealed record Notice(NoticeKind Kind, string Text);
