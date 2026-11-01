namespace Mahdi.Engine.Model;

/// <summary>The five results on a Harkonnen Action die (Strategy appears on two faces).</summary>
public enum DieFace
{
    Leadership,
    Strategy,
    Deployment,
    Mentat,
    House,
}

/// <summary>
/// Who drives the Harkonnen randomness. Shown as "App-driven" and "Player-driven"; the names are kept for saved games.
/// </summary>
public enum DiceMode
{
    /// <summary>App-driven: the app rolls the Harkonnen dice and draws the Tactical cards.</summary>
    AppRolls,

    /// <summary>Player-driven: the player rolls the dice and draws the Tactical cards, then taps what they got.</summary>
    Physical,
}

public enum Phase
{
    Setup,
    RoundStart,
    VehiclePlacement,
    ActionResolution,
    DesertHazards,
    SpiceHarvesting,
    EndOfRound,
    GameOver,
}

/// <summary>Board sectors. In the solo game the four Central Sectors count as one.</summary>
public enum Sector
{
    NorthWest,
    NorthEast,
    SouthWest,
    SouthEast,
    Central,
}

public enum ImperiumPower
{
    Choam,
    SpacingGuild,
    Landsraad,
}

public enum PlanningDeck
{
    Harkonnen,
    Corrino,
}

public enum LeaderId
{
    BaronHarkonnen,
    BeastRabban,
    FeydRautha,
    ThufirHawat,
    GaiusHelenMohiam,
    CaptainAramsham,
    ShaddamIV,
}

public enum LeaderStatus
{
    /// <summary>The Leader has not entered play yet.</summary>
    NotInPlay,

    /// <summary>In play, card on the dashboard, figure not on the board.</summary>
    InReserve,

    /// <summary>In play with the figure on the board.</summary>
    OnBoard,

    /// <summary>In the Regeneration Tank; inactive.</summary>
    InTank,

    /// <summary>Permanently removed from the game.</summary>
    Removed,
}

/// <summary>When a Leader enters play.</summary>
public enum LeaderEntry
{
    AtStart,
    SupremacyStep,
    WhenHawatRemoved,
    WhenRageOvercameShaddamPlayed,
}

public enum GameOutcome
{
    HarkonnenVictory,
    AtreidesVictory,
}

public enum NoticeKind
{
    Info,
    Important,
}
