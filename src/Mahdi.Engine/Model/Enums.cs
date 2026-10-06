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

/// <summary>Who produces the Harkonnen die results.</summary>
public enum DiceMode
{
    /// <summary>The app rolls virtual dice for the Harkonnens.</summary>
    AppRolls,

    /// <summary>The player rolls the physical die and taps the face shown.</summary>
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
